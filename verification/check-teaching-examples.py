"""Verify C# examples and the first console project in the teaching sessions."""

from concurrent.futures import ThreadPoolExecutor, as_completed
import json
from pathlib import Path
import re
import runpy
import subprocess
import sys
from tempfile import TemporaryDirectory


HERE = Path(__file__).resolve().parent
FENCE = re.compile(r"^```([^\n]*)\n(.*?)^```[ \t]*$", re.MULTILINE | re.DOTALL)


def examples(teaching):
    for lesson_id, sessions in teaching.items():
        for session_number, session in enumerate(sessions, 1):
            blocks = FENCE.findall(session["Text"])
            for index, (tag, source) in enumerate(blocks):
                label = f"{lesson_id} / session {session_number} / block {index + 1}"
                if tag == "csharp":
                    assert index + 1 < len(blocks) and blocks[index + 1][0] == "output", (label, "missing output fence")
                    yield label, source, blocks[index + 1][1], None
                elif tag.startswith("csharp-error"):
                    diagnostic = re.fullmatch(r"csharp-error (CS\d{4})", tag)
                    assert diagnostic, (label, "expected csharp-error CSxxxx")
                    yield label, source, "", diagnostic[1]


def check_first_project(teaching, dotnet):
    first = [dict(FENCE.findall(session["Text"])) for session in teaching["R001"]]
    assert first[0]["shell"].splitlines() == ["mkdir GameLab", "cd GameLab", "pwd"]
    assert first[1]["shell"].splitlines() == [
        "dotnet --list-sdks", "dotnet new console -n FirstGame -f net10.0", "cd FirstGame",
    ]
    assert first[3]["shell"].splitlines() == ["dotnet build", "dotnet run"]

    output_root = (HERE / "obj").resolve()
    output_root.mkdir(exist_ok=True)
    with TemporaryDirectory(prefix="first-game-", dir=output_root) as directory:
        workspace = Path(directory) / "GameLab"
        workspace.mkdir()

        def run(cwd, *args, diagnostic=None):
            result = subprocess.run(
                [dotnet, *args], cwd=cwd, capture_output=True, text=True,
                encoding="utf-8", timeout=60,
            )
            if diagnostic:
                output = result.stdout + result.stderr
                assert result.returncode and f"error {diagnostic}" in output, result
                if diagnostic == "CS0103":
                    assert "Program.cs(1,1): error CS0103" in output, result
            else:
                assert result.returncode == 0, result
            return result.stdout.strip()

        assert any(line.startswith("10.") for line in run(workspace, "--list-sdks").splitlines())
        run(workspace, "new", "console", "-n", "FirstGame", "-f", "net10.0")
        project = workspace / "FirstGame"
        assert "<TargetFramework>net10.0</TargetFramework>" in (project / "FirstGame.csproj").read_text()
        program = project / "Program.cs"
        assert program.is_file()
        program.write_text(first[2]["csharp"], encoding="utf-8")
        run(project, "build")
        assert run(project, "run") == first[2]["output"].strip()

        if "R002" in teaching:
            for _, source, expected, diagnostic in examples({"R002": teaching["R002"]}):
                program.write_text(source, encoding="utf-8")
                run(project, "build", diagnostic=diagnostic)
                if not diagnostic:
                    assert run(project, "run") == expected.strip()


def main():
    teaching = json.loads((HERE.parent / "ILearn/Content/teaching.json").read_text(encoding="utf-8"))
    if requested := sys.argv[1:]:
        teaching = {lesson_id: teaching[lesson_id] for lesson_id in requested}
    compiler = runpy.run_path(str(HERE / "check-csharp-v2.py"))
    compile_and_run = compiler["compile_and_run"]
    failures = 0
    with ThreadPoolExecutor(max_workers=4) as pool:
        pending = {}
        for number, (label, source, output, diagnostic) in enumerate(examples(teaching), 1):
            example = {"Id": f"T{number:03}", "Example": source, "ExampleOutput": output}
            pending[pool.submit(compile_and_run, example, expected_error=diagnostic)] = label
        for job in as_completed(pending):
            try:
                job.result()
            except Exception as error:
                failures += 1
                print(f"FAIL {pending[job]} {error}")
    print(f"{len(pending) - failures}/{len(pending)} teaching examples passed")
    if "R001" in teaching:
        try:
            check_first_project(teaching, compiler["DOTNET"])
            print("PASS first console project")
        except (AssertionError, OSError, subprocess.TimeoutExpired) as error:
            print(f"FAIL first console project: {error}")
            failures += 1
    return bool(failures)


if __name__ == "__main__":
    raise SystemExit(main())
