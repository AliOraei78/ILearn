"""Compile and execute repository-authored C# 14 examples, never learner code."""
import concurrent.futures
import json
import os
import re
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LESSONS = json.loads((ROOT / "ILearn/Content/csharp-v2.json").read_text(encoding="utf-8"))
DOTNET = shutil.which("dotnet")
assert DOTNET
sdk_line = next(line for line in reversed(subprocess.check_output([DOTNET, "--list-sdks"], text=True).splitlines()) if line.startswith("10."))
VERSION, sdk_directory = sdk_line.split(" ", 1)
SDK = Path(sdk_directory.strip("[]")) / VERSION
PACKS = SDK.parent.parent / "packs"
latest = lambda folder: max((p for p in folder.iterdir() if p.name.startswith("10.")), key=lambda p: tuple(map(int, p.name.split("."))))
CORE = latest(PACKS / "Microsoft.NETCore.App.Ref")
ASP = latest(PACKS / "Microsoft.AspNetCore.App.Ref")
OUTPUT = ROOT / "verification/obj/csharp-v2-examples"
OUTPUT.mkdir(parents=True, exist_ok=True)
RSP = OUTPUT / "references.rsp"
RSP.write_text("\n".join(f'/reference:"{p}"' for p in [*(CORE / "ref/net10.0").glob("*.dll"), *(ASP / "ref/net10.0").glob("*.dll")]), encoding="utf-8")
USINGS = "\n".join("global using " + name + ";" for name in ["System", "System.IO", "System.Linq", "System.Threading", "System.Threading.Tasks", "System.Collections.Generic"]) + "\n"

REFERENCE_OUTPUTS = {
1:"Ready\nC#",2:"10",3:"9,4,10,False",4:"37.5\n4\n4.5\noverflow\n-2147483648",5:"Item: Book\nPrice: 12.50\n0,1",6:"False\nTrue\nTrue\nFalse\nFalse",
7:"Invalid,Invalid\nFree,Free\nPaid,Paid",8:"0,0\n10,10\n1",9:"-3,7\nEmpty",10:"6,6\n4\n0,0\n6,6\n6",
11:"20\n5",12:"True,3\nFalse,0\nFalse,0",13:"Missing\n0\n7",14:"Invalid\nInvalid\nInvalid\n0\n12.50",15:"6\nformat\ncleanup",16:"7\nCalc",17:"red|BLUE\nTrue\n red , BLUE ,, ",18:"True,False\nTrue,True\nFalse,False",19:"-1\n0\n2",20:"Unknown\n12",
21:"3,0",22:"0\nrejected",23:"Pen\ninvalid",24:"8",25:"False\n5\nTrue\n0",26:"True\nTrue\nTrue",27:"Base+Vip\nBase\nHidden",28:"Report:memory\nReport:fake",29:"count",30:"1",31:"4 passed",32:"passed",
33:"7",34:"5,0\n9",35:"1,3,9,7",36:"3\n0",37:"True,1\nFalse",38:"missing\npositive\ntext\npair\nother\nother",39:"False,True",40:"4.0",41:"7\nrange",42:"3\nlate failure",43:"model passed",
44:"3\nnone",45:"ok",46:"A,a,b\npassed",47:"5\n4.0\n0",48:"6\n2\n3",49:"0,1,2\n8",50:"passed",51:"A\nB",52:"4",53:"True\nFalse",54:"empty\nformat\nrange\n3",55:"1",
56:"1,8\nrange",57:"1\ndisposed",58:"2\n3",59:"2\nFalse\n2",60:"A:1\nB:2\nC:3",61:"1\n7,1",62:"6\n9\n5",63:"4,2",64:"A:7\nB:0",65:"2,6,1",66:"False,True",67:"A:5\n1",
68:"True",69:"6,5,5\nTrue\nFalse",70:"سلام",71:"5\n1\nTrue",72:"5",73:"passed",74:"7,InvalidOperationException",75:"memory:2\nfile:2",76:"6",77:"net10.0",78:"6",79:"5\n5",
80:"7\nobserved",81:"2,1\n2",82:"4\ncancelled",83:"1",84:"1\n1",85:"3,2,1",86:"10",87:"100\n1",88:"0\n1\n1",89:"2,1,7",90:"6",91:"2,4,6,8",92:"request\nrequest",93:"cancelled\nchild,parent",94:"18\npreserved\ncancelled",
95:"True\nTrue",96:"3",97:"1\n0",98:"0\n1",99:"9",100:"42",101:"6",102:"ABC",103:r"equivalent\nbaseline ticks: \d+; bytes: \d+\nspan ticks: \d+; bytes: \d+",104:"7\n6",105:"42\ninvalid\ninvalid\n2147483647",106:"item_name",107:"True\nTrue",108:"1,2",109:"10",110:"4\n1",111:"True",112:'{"Value":3}',113:"32",114:"invalid\n42\ninvalid\ninvalid",
115:"0.0\n9.0",116:"Value=3\nvalue,3",117:"old\n1",118:"Cancelled\ninvalid transition",119:"5,2",120:"passed",121:"1,1\n0,1",122:"5",123:"7\npreserved\ncancelled"
}


def compile_and_run(lesson, reference=False):
    number = int(lesson["Id"][1:])
    name = lesson["Id"] + ("-reference" if reference else "-example")
    source = lesson["Activities"][-1]["SolutionCode"] if reference else lesson["Example"]
    case = OUTPUT / name
    case.mkdir(exist_ok=True)
    extra_references = []
    if reference and number == 107:
        for filename in ["Microsoft.CodeAnalysis.dll", "Microsoft.CodeAnalysis.CSharp.dll"]:
            original = SDK / "Roslyn/bincore" / filename
            extra_references.append(f"/reference:{original}")
            shutil.copy2(original, case / filename)
        harness = '''
var references=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(path=>MetadataReference.CreateFromFile(path));
var compilation=CSharpCompilation.Create("GeneratedTest",[CSharpSyntaxTree.ParseText("[GenerateHello] partial class Sample {}")],references:references,options:new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
GeneratorDriver driver=CSharpGeneratorDriver.Create(new HelloGenerator().AsSourceGenerator());
driver.RunGeneratorsAndUpdateCompilation(compilation,out var updated,out var diagnostics);
if(diagnostics.Any(d=>d.Severity==DiagnosticSeverity.Error)||updated.GetDiagnostics().Any(d=>d.Severity==DiagnosticSeverity.Error)) throw new Exception("generator diagnostic");
Console.WriteLine(updated.GetTypeByMetadataName("Sample")?.GetMembers("Hello").Length==1);
var bad=CSharpCompilation.Create("BadTest",[CSharpSyntaxTree.ParseText("[GenerateHello] class Bad {}")],references:references,options:new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
GeneratorDriver badDriver=CSharpGeneratorDriver.Create(new HelloGenerator().AsSourceGenerator());
badDriver.RunGeneratorsAndUpdateCompilation(bad,out _,out var rejected);
Console.WriteLine(rejected.Any(d=>d.Id=="IL001"&&d.Severity==DiagnosticSeverity.Error));
'''
        source = source.replace("[Generator]", harness + "[Generator]", 1)
    if reference and number == 122:
        source = "Console.WriteLine(Smoke.Run());\n" + source
    program = case / "Program.cs"
    program.write_text(USINGS + source, encoding="utf-8")
    assembly = case / "Example.dll"
    analyzers = []
    if "JsonSerializerContext" in source:
        analyzers = [f"/analyzer:{CORE / 'analyzers/dotnet/cs/System.Text.Json.SourceGeneration.dll'}"]
    command = [DOTNET, str(SDK / "Roslyn/bincore/csc.dll"), "/nologo", "/noconfig", "/langversion:14", "/nullable:enable", "/unsafe", "/target:exe", f"/out:{assembly}", "@" + str(RSP), *extra_references, *analyzers, str(program)]
    built = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", timeout=45)
    assert built.returncode == 0, (name, built.stdout, built.stderr)
    (case / "Example.runtimeconfig.json").write_text(json.dumps({"runtimeOptions":{"tfm":"net10.0","frameworks":[{"name":"Microsoft.NETCore.App","version":"10.0.0"},{"name":"Microsoft.AspNetCore.App","version":"10.0.0"}]}}), encoding="utf-8")
    arguments = ["--count", "3"] if reference and number == 78 else []
    stdin = "bad\nsum\nexit\n" if reference and number == 20 else ""
    environment = dict(os.environ, DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="0")
    run = subprocess.run([DOTNET, str(assembly), *arguments], input=stdin, capture_output=True, text=True, encoding="utf-8", env=environment, timeout=15)
    assert run.returncode == 0, (name, run.returncode, run.stdout, run.stderr)
    expected = REFERENCE_OUTPUTS[number] if reference else lesson["ExampleOutput"]
    if reference and number == 103:
        assert re.fullmatch(expected,run.stdout.strip().replace("\r\n","\n")), (name,run.stdout)
    else:
        assert run.stdout.strip().splitlines() == expected.strip().splitlines(), (name, "expected", expected, "actual", run.stdout)
    return dict(name=name, warnings=built.stdout.strip())


if __name__ == "__main__":
    results, errors = [], []
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        futures = {pool.submit(compile_and_run, lesson, reference): (lesson["Id"], reference) for lesson in LESSONS for reference in (False, True)}
        for future in concurrent.futures.as_completed(futures):
            try:
                results.append(future.result())
            except Exception as error:
                errors.append(str(error))
                print("FAIL " + str(error), flush=True)
            if (len(results) + len(errors)) % 25 == 0:
                print(f"Checked {len(results)+len(errors)}/246; passed {len(results)}", flush=True)
    report = dict(sdk=VERSION, languageVersion="14", passed=len(results), errors=errors, checks=results)
    (ROOT / "verification/csharp-v2-examples.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(dict(passed=len(results), failed=len(errors))))
    raise SystemExit(bool(errors))
