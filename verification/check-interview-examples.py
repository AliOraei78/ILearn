"""Compile the actual authored snippets and verify their output or intentional runtime error."""
import json
import subprocess
import tempfile
import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
bank = json.loads((root / 'ILearn/Content/interviews.json').read_text(encoding='utf-8'))[0]
expected = {'C003': 'Mina', 'C017': '2', 'C023': 'B\nA', 'C033': 'ArrayTypeMismatchException', 'C042': '2,3,4', 'C053': '3\n3\n3'}
samples = [question['Activity'] for question in bank['Questions'] if question['Activity']['Code']]
assert {sample['Id'] for sample in samples} == set(expected)
reports = []
work = Path(tempfile.mkdtemp(prefix='interview-examples-', suffix='.tmp', dir=root / 'verification'))
for sample in samples:
    folder = work / sample['Id']
    folder.mkdir(parents=True, exist_ok=True)
    project = folder / 'Sample.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>', encoding='utf-8')
    (folder / 'Program.cs').write_text(sample['Code'], encoding='utf-8')
    build = subprocess.run(['dotnet', 'build', str(project), '--nologo', '-v:q'], cwd=root, capture_output=True, text=True, timeout=90)
    assert build.returncode == 0, sample['Id'] + ': ' + build.stdout + build.stderr
    result = subprocess.run(['dotnet', str(folder / 'bin/Debug/net10.0/Sample.dll')], cwd=root, capture_output=True, text=True, timeout=20)
    if sample['Id'] == 'C033':
        assert result.returncode != 0 and expected[sample['Id']] in result.stderr, sample['Id']
    else:
        assert result.returncode == 0 and result.stdout.strip() == expected[sample['Id']], (sample['Id'], result.stdout, result.stderr)
    reports.append(dict(id=sample['Id'], expected=expected[sample['Id']], passed=True))
(root / 'verification/interview-examples.json').write_text(json.dumps(reports, indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(compiledExamples=len(reports), passed=True)))
assert work.resolve().parent == (root / 'verification').resolve()
shutil.rmtree(work)
