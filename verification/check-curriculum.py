"""Compile and run authored lesson examples with the installed .NET SDK, without NuGet.

Only repository content is compiled here; learner submissions are never read.
"""
import json
import re
import shutil
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
content = root / "ILearn/Content"
lessons = json.loads((content / "lessons.json").read_text(encoding="utf-8"))
revisions = json.loads((content / "revisions.json").read_text(encoding="utf-8"))
activities = {}
for lesson in lessons:
    for activity in lesson["Activities"]:
        key = lesson["Id"] + "/" + activity["Id"]
        activities[key] = activity | revisions.get(key, {})

dotnet = shutil.which("dotnet")
assert dotnet, ".NET SDK is required"
sdks = subprocess.check_output([dotnet, "--list-sdks"], text=True).splitlines()
sdk = next(line for line in reversed(sdks) if line.startswith("10."))
version, directory = sdk.split(" ", 1)
sdk_root = Path(directory.strip("[]"))
compiler = sdk_root / version / "Roslyn/bincore/csc.dll"
packs = sdk_root.parent / "packs/Microsoft.NETCore.App.Ref"
pack = max((p for p in packs.iterdir() if p.name.startswith("10.")), key=lambda p: tuple(map(int, p.name.split("."))))
output = root / "verification/obj/curriculum-examples"
output.mkdir(parents=True, exist_ok=True)
references = output / "references.rsp"
references.write_text("\n".join(f'/reference:"{p}"' for p in (pack / "ref/net10.0").glob("*.dll")), encoding="utf-8")
usings = "global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\n"
type_start = re.compile(r"(?m)^(?:(?:public|internal|abstract|sealed|readonly|static|partial)\s+)*(?:class|struct|interface|record)\b")
checks = []


def add_statements(source, statements):
    match = type_start.search(source)
    index = match.start() if match else len(source)
    return source[:index] + "\n" + statements + "\n" + source[index:]


def check(name, source, expected=None, error=None):
    case = output / re.sub(r"[^a-zA-Z0-9-]", "-", name)
    case.mkdir(exist_ok=True)
    program = case / "Program.cs"
    program.write_text(usings + source, encoding="utf-8")
    assembly = case / "Example.dll"
    target = "exe" if expected is not None else "library"
    result = subprocess.run([dotnet, str(compiler), "/nologo", "/noconfig", "/langversion:13", "/nullable:enable",
                             f"/target:{target}", f"/out:{assembly}", "@" + str(references), str(program)],
                            capture_output=True, text=True, timeout=30)
    if error:
        assert result.returncode != 0 and error in result.stdout, (name, result.stdout, result.stderr)
    else:
        assert result.returncode == 0, (name, result.stdout, result.stderr)
        if expected is not None:
            (case / "Example.runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
                "tfm": "net10.0", "framework": {"name": "Microsoft.NETCore.App", "version": "10.0.0"}}}), encoding="utf-8")
            run = subprocess.run([dotnet, str(assembly)], capture_output=True, text=True, timeout=10)
            assert run.returncode == 0, (name, run.stdout, run.stderr)
            assert run.stdout.strip().splitlines() == expected, (name, run.stdout, expected)
    checks.append(name)
    print("PASS " + name, flush=True)


expected_references = {
    "S01/workshop": ["Ready"], "S02/workshop": ["5", "2"],
    "S03/workshop": ["True", "12", "False", "0"],
    "S04/invariant": ["6"], "S04/boundary": ["4", "7", "9"], "S04/workshop": ["2", "0"],
    "S05/keys": [], "S05/explain2": ["8", "4", "Key"], "S06/explain2": ["A"],
    "S07/explain2": ["Base+Vip"], "S08/assessment": ["7"],
    "S09/ref-key": ["5"], "S09/workshop": ["2,3", "6,2"],
    "S10/with-key": ["10", "20"], "S10/workshop": ["10", "20", "True", "True"],
}
for key, activity in activities.items():
    source = activity.get("SolutionCode", "")
    if not source:
        continue
    expected = expected_references.get(key)
    if key == "S06/surgery":
        source = "public class Example { " + source + " }"
    if key == "S05/workshop":
        source = add_statements(source, """
Console.WriteLine(TryCalculateTotal(10m, 2, out decimal total));
Console.WriteLine(total);
Console.WriteLine(TryCalculateTotal(0m, 1, out total));
Console.WriteLine(total);
Console.WriteLine(TryCalculateTotal(-1m, 1, out total));
Console.WriteLine(total);
Console.WriteLine(TryCalculateTotal(1m, 0, out total));
Console.WriteLine(total);
""")
        expected = ["True", "20", "True", "0", "False", "0", "False", "0"]
    if key == "S06/workshop":
        source = add_statements(source, """
var order = new Order("A"); order.AddItem(10m, 2); order.AddItem(0m, 1);
Console.WriteLine(order.Total);
try { order.AddItem(-1m, 1); } catch (ArgumentOutOfRangeException) { Console.WriteLine("negative"); }
try { order.AddItem(1m, 0); } catch (ArgumentOutOfRangeException) { Console.WriteLine("zero"); }
try { _ = new Order(""); } catch (ArgumentException) { Console.WriteLine("empty"); }
""")
        expected = ["20", "negative", "zero", "empty"]
    if key == "S07/workshop":
        source = add_statements(source, """
DiscountPolicy regular = new RegularDiscount(), vip = new VipDiscount();
Console.WriteLine(regular.Apply(100m).ToString("G29"));
Console.WriteLine(vip.Apply(100m).ToString("G29"));
Console.WriteLine(vip.Apply(0m).ToString("G29"));
try { vip.Apply(-1m); } catch (ArgumentOutOfRangeException) { Console.WriteLine("negative"); }
""")
        expected = ["100", "90", "0", "negative"]
    if key == "S08/workshop":
        source = add_statements(source, "Console.WriteLine(Reports.Example());")
        expected = ["report"]
    check(key + " reference", source, expected)

for key, expected in {
    "S02/copies": ["4", "9", "9", "9"], "S07/trace": ["Vip", "Base", "Vip"],
    "S09/value-copy": ["2", "8", "8", "8"], "S10/equality": ["False", "True", "False", "True"],
}.items():
    activity = activities[key]
    expressions = activity["Rows"]
    check(key + " outputs", add_statements(activity["Code"], "\n".join(f"Console.WriteLine({e});" for e in expressions)), expected)

for key, expected in {
    "S03/boxed": ["7", "InvalidCastException", "7", "9"],
    "S03/overflow": ["OverflowException", "-2147483648", "OverflowException", "44"],
}.items():
    activity = activities[key]
    statements = "\n".join(f"try {{ Console.WriteLine({e}); }} catch (Exception e) {{ Console.WriteLine(e.GetType().Name); }}" for e in activity["Rows"])
    check(key + " independent rows", add_statements(activity["Code"], statements), expected)

for key, expected, statements in [
    ("S04/switch", ["Invalid", "End", "Danger", "Play"], "Console.WriteLine(Stage(-1)); Console.WriteLine(Stage(0)); Console.WriteLine(Stage(2)); Console.WriteLine(Stage(3));"),
    ("S04/loop", ["8"], "Console.WriteLine(sum);"),
    ("S04/explain2", ["Small", "Big"], "Console.WriteLine(Gate(3)); Console.WriteLine(Gate(8));"),
    ("S05/predict", ["7"], ""), ("S02/alias", ["1"], ""), ("S02/explain2", ["Hi"], ""),
    ("S09/explain1", ["9"], ""), ("S10/shallow", ["Map"], ""), ("S10/array-equality", ["False"], ""),
]:
    check(key + " authored code", add_statements(activities[key]["Code"], statements), expected)

check("private access rejected", add_statements(activities["S06/compiler"]["Code"], "var amount = new Order().Price;"), error="CS0122")
check("readonly assignment rejected", add_statements(activities["S09/readonly"]["Code"], "Position p = new(2); p.X = 5;"), error="CS0200")
check("readonly replacement allowed", add_statements(activities["S09/readonly"]["Code"], "Position p = new(2); p = new Position(5); Console.WriteLine(p.X);"), ["5"])
check("as nonnullable rejected", activities["S03/type-lab"]["Code"] + "\nvar number = value as int;", error="CS0077")
check("as and is observations", activities["S03/type-lab"]["Code"] + "\nConsole.WriteLine(value is string); Console.WriteLine(value as string); Console.WriteLine(value as int? is null);", ["True", "map", "True"])
check("constant checked overflow rejected", "Console.WriteLine(checked((byte)300));", error="CS0221")

report = {"passed": len(checks), "checks": checks, "sdk": version, "languageVersion": "13", "errors": []}
(root / "verification/curriculum-examples.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps({"passed": len(checks), "errors": []}))
