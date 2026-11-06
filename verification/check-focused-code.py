"""Compile C# reference snippets, including the support types explicitly supplied by each task."""
import concurrent.futures
import json
import os
from pathlib import Path
import subprocess
import sys

sys.stdout.reconfigure(encoding='utf-8')
root = Path(__file__).resolve().parent.parent
scratch = root / '.tools' / 'focused-code'
scratch.mkdir(parents=True, exist_ok=True)
dotnet = Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'dotnet'
version = lambda p: tuple(int(n) for n in p.name.split('.')[:3])
sdk = max((dotnet / 'sdk').iterdir(), key=version)
compiler = sdk / 'Roslyn/bincore/csc.dll'
references = list((max((dotnet / 'packs/Microsoft.NETCore.App.Ref').iterdir(), key=version) / 'ref/net10.0').glob('*.dll'))
references += list((max((dotnet / 'packs/Microsoft.AspNetCore.App.Ref').iterdir(), key=version) / 'ref/net10.0').glob('*.dll'))
references += list((root / 'ILearn.Tests/bin/Debug/net10.0').glob('xunit*.dll'))
references += list((sdk / 'Roslyn/bincore').glob('Microsoft.CodeAnalysis*.dll'))
generator = max((dotnet / 'packs/Microsoft.NETCore.App.Ref').iterdir(), key=version) / 'analyzers/dotnet/cs/System.Text.Json.SourceGeneration.dll'
response = scratch / 'references.rsp'
response.write_text('\n'.join(f'/reference:"{p}"' for p in references), encoding='utf-8')
prelude = '''using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
'''

def check(item):
    key, code = item
    source = scratch / (key.replace('/', '_') + '.cs')
    source.write_text(prelude + code, encoding='utf-8')
    arguments = [str(dotnet / 'dotnet.exe'), str(compiler), '/nologo', '/noconfig', '/nostdlib+', '/nullable:enable', '/langversion:14', '/unsafe+', f'/analyzer:{generator}', f'@{response}', f'/out:{source.with_suffix(".dll")}', str(source)]
    result = subprocess.run(arguments, capture_output=True, encoding='utf-8')
    if result.returncode and 'error CS5001:' in result.stdout:
        result = subprocess.run(arguments + ['/target:library'], capture_output=True, encoding='utf-8')
    errors = [line.replace(str(source), key) for line in (result.stdout + result.stderr).splitlines() if 'error ' in line]
    return {'key': key, 'errors': errors}


items = []
for filename in ['csharp-v2.json', 'lessons.json']:
    for lesson in json.loads((root / 'ILearn/Content' / filename).read_text(encoding='utf-8')):
        for a in lesson['Activities']:
            code = a.get('SolutionCode', '')
            if code and a['Kind'] == 'workshop' and not code.startswith(('<Project', 'dotnet ')):
                context = a.get('Code', '')
                items.append((lesson['Id'] + '/' + a['Id'], context.replace('/* پاسخ این مرحله */', code) if '/* پاسخ این مرحله */' in context else code))

with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    results = list(pool.map(check, items))
failures = [result for result in results if result['errors']]
(root / 'verification/focused-code-report.json').write_text(json.dumps({'checked': len(results), 'failures': failures}, ensure_ascii=False, indent=2), encoding='utf-8')
for failure in failures:
    print(failure['key'], '\n'.join(failure['errors'][:2]))
print(f'{len(results) - len(failures)}/{len(results)} C# references compile')
sys.exit(bool(failures))
