"""Compile playable workshop answers in their starter and check non-C# artifacts."""

from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import runpy
import shlex
import subprocess
from xml.etree import ElementTree


HERE = Path(__file__).resolve().parent
CONTENT = HERE.parent / "ILearn/Content"
ARTIFACTS = HERE.parent / "artifacts/teaching-audit"
STARTER = "/* پاسخ این مرحله */"
FRAGMENTS = {"code-room/R008/complete-even-condition": "/* شرط */"}


def read(name):
    return json.loads((CONTENT / name).read_text(encoding="utf-8-sig"))


def references():
    revisions = read("revisions.json")
    for filename in ("csharp-v2.json", "lessons.json"):
        for lesson in read(filename):
            for original in lesson["Activities"]:
                if original["Kind"] not in ("workshop", "explain"):
                    continue
                key = f"{lesson['Id']}/{original['Id']}"
                activity = original | revisions.get(key, {})
                if activity.get("SolutionCode"):
                    yield key, activity
    for item in read("code-room.json"):
        activity = item["Activity"]
        if item["Subject"] == "csharp" and activity.get("SolutionCode"):
            yield f"code-room/{item['LessonId']}/{activity['Id']}", activity


def check_command_answer(key, solution):
    commands = [shlex.split(line) for line in solution.splitlines() if line.strip()]
    if key == "R122/workshop":
        expected = [
            ("restore", "src/Delivery.slnx", ["--locked-mode"]),
            ("build", "src/Delivery.slnx", ["-c", "Release", "--no-restore"]),
            ("test", "src/Delivery.slnx", ["-c", "Release", "--no-build", "--no-restore"]),
            ("format", "src/Delivery.slnx", ["--verify-no-changes", "--no-restore"]),
        ]
    else:
        expected = [
            ("pack", "Library.csproj", ["-c", "Release", "--no-build", "--no-restore", "-o", "artifacts/packages"]),
            ("publish", "Runner.csproj", ["-c", "Release", "-r", "linux-x64", "--self-contained", "false",
                                        "--no-restore", "-o", "artifacts/linux-x64"]),
        ]
    assert len(commands) == len(expected)
    for command, (operation, project, arguments) in zip(commands, expected):
        assert command[:3] == ["dotnet", operation, project], command
        assert command[3:] == arguments, command


def main():
    compiler = runpy.run_path(str(HERE / "check-csharp-v2.py"))
    sdk = compiler["SDK"]
    core = compiler["CORE"]
    test_output = HERE.parent / "ILearn.Tests/bin/Release/net10.0"
    additional = [
        *(test_output / name for name in ("xunit.assert.dll", "xunit.core.dll", "xunit.abstractions.dll")),
        *(sdk / "Roslyn/bincore").glob("Microsoft.CodeAnalysis*.dll"),
    ]
    missing = [file for file in additional if not file.is_file()]
    if missing:
        raise FileNotFoundError(f"Build ILearn.Tests in Release before checking the workshop references. Missing {missing}")
    command = [
        compiler["DOTNET"], str(sdk / "Roslyn/bincore/csc.dll"), "/nologo", "/noconfig",
        "/langversion:14", "/nullable:enable", "/unsafe", "@" + str(compiler["RSP"]),
        *(f"/reference:{file}" for file in additional),
    ]

    def check(entry):
        key, activity = entry
        solution = activity["SolutionCode"]
        if key == "R077/workshop":
            project = ElementTree.fromstring(solution)
            assert project.tag == "Project" and project.get("Sdk") == "Microsoft.NET.Sdk"
            assert set(project.findtext("PropertyGroup/TargetFrameworks", "").split(";")) == {"net10.0", "netstandard2.1"}
            assert project.findtext("PropertyGroup/Nullable") == "enable"
            assert project.findtext("PropertyGroup/OutputType", "Library") == "Library"
            return "project XML"
        if key in {"R122/workshop", "R122/workshop-02"}:
            check_command_answer(key, solution)
            return "command structure"

        starter = activity.get("Code", "")
        marker = FRAGMENTS.get(key, STARTER)
        if marker in starter:
            assert starter.count(marker) == 1
            source = starter.replace(marker, solution, 1)
        else:
            assert key not in FRAGMENTS
            source = solution
        folder = HERE / "obj/workshop-references" / key.replace("/", "-")
        folder.mkdir(parents=True, exist_ok=True)
        file = folder / "Program.cs"
        file.write_text(compiler["USINGS"] + source, encoding="utf-8")
        args = [*command, f"/out:{folder / 'Answer.dll'}", str(file)]
        if "JsonSerializerContext" in source:
            args.append(f"/analyzer:{core / 'analyzers/dotnet/cs/System.Text.Json.SourceGeneration.dll'}")
        result = subprocess.run([*args, "/target:library"], capture_output=True, text=True, encoding="utf-8", timeout=45)
        if "error CS8805" in result.stdout:
            result = subprocess.run([*args, "/target:exe"], capture_output=True, text=True, encoding="utf-8", timeout=45)
        assert result.returncode == 0, result.stdout + result.stderr
        return "C# compilation"

    results = []
    entries = list(references())
    with ThreadPoolExecutor(max_workers=4) as pool:
        futures = [pool.submit(check, entry) for entry in entries]
        for entry, future in zip(entries, futures):
            key, _ = entry
            try:
                results.append({"id": key, "check": future.result()})
            except Exception as error:
                results.append({"id": key, "error": str(error) or type(error).__name__})
                print(f"FAIL {key} {error}", flush=True)
    failures = sum("error" in result for result in results)
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    (ARTIFACTS / "workshop-reference-results.json").write_text(
        json.dumps({"total": len(results), "passed": len(results) - failures, "results": results}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"{len(results) - failures}/{len(results)} reference answers checked")
    return bool(failures)


if __name__ == "__main__":
    raise SystemExit(main())
