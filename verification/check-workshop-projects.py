"""Build copied workshop files in isolated output folders; never reads learner submissions."""
import json
import subprocess
import sys
import uuid
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")
ROOT = Path(__file__).resolve().parents[1]
LESSONS = json.loads((ROOT / "ILearn/Content/csharp-v2.json").read_text(encoding="utf-8"))
OUTPUT = ROOT / "verification/obj" / ("workshop-projects-" + uuid.uuid4().hex[:10])
OUTPUT.mkdir(parents=True, exist_ok=True)
results = []


def command(case, arguments, expected=None, error=None):
    done = subprocess.run(arguments, cwd=case, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180)
    if error:
        assert done.returncode != 0 and error in done.stdout + done.stderr, (case.name, arguments, done.stdout, done.stderr)
    else:
        assert done.returncode == 0, (case.name, arguments, done.stdout, done.stderr)
        if expected is not None:
            assert done.stdout.strip().endswith(expected), (case.name, arguments, done.stdout, expected)
    results.append({"unit":case.name,"command":arguments,"expectedDiagnostic":error,"output":done.stdout.strip()[-3500:]})
    print("PASS " + case.name + " " + " ".join(arguments[:4]), flush=True)


for lesson in LESSONS:
    number = int(lesson["Id"][1:])
    files = lesson["Activities"][-1].get("ReferenceFiles", {})
    if not files or (len(sys.argv) > 1 and lesson["Id"] not in sys.argv[1:]):
        continue
    case = OUTPUT / lesson["Id"]
    case.mkdir(exist_ok=True)
    for name, source in files.items():
        destination = (case / name).resolve()
        assert destination.is_relative_to(case.resolve()), "Reference file leaves its project"
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(source, encoding="utf-8")
    if number == 16:
        command(case, ["dotnet", "run", "hello.cs"], "file-based")
    feature_outputs = {40:"6,True\n-2147483648\noverflow",44:"List\nTrue",49:"True,12\nFalse,0",52:"4,1",99:"6\n6",100:"2\n42",106:"Int32\nvalue > 0",108:"N=3\nlong"}
    if number in feature_outputs:
        command(case, ["dotnet", "run", "--project", "Features.csproj"], feature_outputs[number])
        if number == 99:
            invalid = case / "Escape.cs"
            invalid.write_text(files["Escape.cs.txt"], encoding="utf-8")
            try:
                command(case, ["dotnet", "build", "Features.csproj", "--no-restore"], error="CS8352")
            finally:
                invalid.unlink()
    if number == 31:
        command(case, ["dotnet","test","Wallet.Tests.csproj","--verbosity","minimal"])
    if number in (74,119):
        command(case, ["dotnet","run","--project","Workshop.csproj","-c","Release"], "7,InvalidOperationException" if number == 74 else "5,2")
    if number in (109,110):
        command(case,["dotnet","run","--project","UnsafeWorkshop.csproj","-c","Release"],"10" if number==109 else "4\n1")
    if number == 77:
        command(case, ["dotnet","restore","Consumer/Consumer.csproj"])
        command(case, ["dotnet","restore","Consumer/Consumer.csproj","--locked-mode"])
        command(case, ["dotnet","run","--project","Consumer","--no-restore"], "5")
    if number == 107:
        command(case, ["dotnet","run","--project","Consumer"], "Hello")
        invalid = case / "Consumer/Bad.cs"
        invalid.write_text("[GenerateHello] class Bad {}", encoding="utf-8")
        try:
            command(case, ["dotnet","build","Consumer/Consumer.csproj","--no-restore"], error="IL001")
        finally:
            invalid.unlink()
    if number == 112:
        command(case, ["dotnet","run","-c","Release"], '{"Value":3}')
        command(case, ["dotnet","publish","-c","Release","--no-restore","--self-contained","false","-o","artifacts/jit"])
        command(case, ["dotnet","artifacts/jit/Publish.dll"], '{"Value":3}')
    if number in (94,123):
        expected = "18" if number == 94 else "7"
        command(case, ["dotnet","run","--project","Worker","--","input.json","output.json"], expected)
        values=json.loads((case/"output.json").read_text(encoding="utf-8"))
        assert values == ([4,6,8] if number == 94 else [0,3,4])
        if number == 123:
            command(case, ["dotnet","test","Tests/Tests.csproj","--verbosity","minimal"])
    if number == 122:
        assert "Runner/packages.lock.json" in files and "Tests/packages.lock.json" in files
        command(case, ["pwsh","-NoProfile","-File","verify.ps1"])

(ROOT/"verification/workshop-projects.json").write_text(json.dumps({"outputDirectory":str(OUTPUT),"checks":results,"passed":len(results)},ensure_ascii=False,indent=2),encoding="utf-8")
print(f"Passed {len(results)} project checks")
