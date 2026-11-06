"""Inspect source/history and release outputs without printing secret values. Standard library only."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import subprocess
import tarfile
import zipfile

parser = argparse.ArgumentParser()
parser.add_argument('--web-dir', type=Path, required=True)
parser.add_argument('--apk', type=Path, required=True)
parser.add_argument('--report', type=Path, required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
secrets = []
env_path = root / '.env'
if env_path.exists():
    for line in env_path.read_text(encoding='utf-8-sig').splitlines():
        if '=' not in line:
            continue
        key, value = line.split('=', 1)
        if re.search(r'PASSWORD|API_KEY|CODE_KEY', key):
            value = value.strip().strip('\"\'')
            if len(value) >= 12:
                secrets.append((key, value.encode()))
password_file = root / 'ILearn.Android/keys/password.txt'
if password_file.exists():
    secrets.append(('APK_SIGNING_PASSWORD', password_file.read_bytes().strip()))
account_file = root / 'ILearn/App_Data/test-account.json'
if account_file.exists():
    account = json.loads(account_file.read_text(encoding='utf-8-sig'))
    if isinstance(account.get('password'), str):
        secrets.append(('LOCAL_TEST_ACCOUNT_PASSWORD', account['password'].encode()))

findings, counts = [], {'source': 0, 'history': 0, 'web': 0, 'apk': 0}
patterns = [
    ('OPENROUTER_KEY', re.compile(rb'sk-or-v1-[0-9a-fA-F]{32,}')),
    ('GITHUB_TOKEN', re.compile(rb'ghp_[A-Za-z0-9]{36}')),
    ('PRIVATE_KEY', re.compile(rb'(?m)^-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----\r?$')),
]

def inspect(name, data, scope):
    counts[scope] += 1
    for label, value in secrets:
        if value and value in data:
            findings.append({'scope': scope, 'path': name, 'kind': label})
    for label, pattern in patterns:
        if pattern.search(data):
            findings.append({'scope': scope, 'path': name, 'kind': label})

def forbidden(name):
    path = Path(name)
    return 'app_data' in {part.lower() for part in path.parts} or path.name.lower() in {'.env', 'password.txt'} or path.suffix.lower() in {'.p12', '.pfx', '.jks', '.keystore', '.pem', '.key'}

excluded = {'.git', '.tools', 'artifacts', 'bin', 'obj', 'node_modules', 'App_Data', 'keys', '__pycache__'}
for directory, folders, files in os.walk(root):
    folders[:] = [name for name in folders if name not in excluded]
    for name in files:
        path = Path(directory) / name
        if name == '.env':
            continue
        inspect(path.relative_to(root).as_posix(), path.read_bytes(), 'source')

revisions = subprocess.check_output(['git', '-C', str(root), 'rev-list', '--all'], text=True).splitlines()
for revision in revisions:
    process = subprocess.Popen(['git', '-C', str(root), 'archive', revision], stdout=subprocess.PIPE)
    with tarfile.open(fileobj=process.stdout, mode='r|') as archive:
        for entry in archive:
            if entry.isfile():
                inspect(revision[:12] + '/' + entry.name, archive.extractfile(entry).read(), 'history')
    if process.wait() != 0:
        raise RuntimeError('History archive failed')

assert args.web_dir.is_dir() and (args.web_dir / 'ILearn.dll').is_file(), 'Fresh web publish is required'
for path in args.web_dir.rglob('*'):
    if path.is_file():
        name = path.relative_to(args.web_dir).as_posix()
        if forbidden(name):
            findings.append({'scope': 'web', 'path': name, 'kind': 'PRIVATE_FILE'})
        inspect(name, path.read_bytes(), 'web')

with zipfile.ZipFile(args.apk) as archive:
    for entry in archive.infolist():
        if entry.is_dir():
            continue
        if forbidden(entry.filename):
            findings.append({'scope': 'apk', 'path': entry.filename, 'kind': 'PRIVATE_FILE'})
        inspect(entry.filename, archive.read(entry), 'apk')

tools = Path(os.environ['LOCALAPPDATA']) / 'Android/Sdk/build-tools/35.0.0'
def android(command, *arguments):
    result = subprocess.run([str(tools / command), *map(str, arguments)], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8', errors='replace')
    if result.returncode:
        raise RuntimeError('Android artifact verification failed: ' + command)
    return result.stdout

signature = android('apksigner.bat', 'verify', '--verbose', args.apk)
assert 'Verified using v3 scheme (APK Signature Scheme v3): true' in signature
badging = android('aapt2.exe', 'dump', 'badging', args.apk)
assert 'application-debuggable' not in badging, 'Release APK must not be debuggable'
assert re.findall(r"uses-permission: name='([^']+)'", badging) == ['android.permission.INTERNET']
manifest = android('aapt2.exe', 'dump', 'xmltree', '--file', 'AndroidManifest.xml', args.apk)
assert re.search(r'android:allowBackup[^\n]*=false\b', manifest)
assert re.search(r'android:usesCleartextTraffic[^\n]*=false\b', manifest)
assert 'android:dataExtractionRules' in manifest
extraction = android('aapt2.exe', 'dump', 'xmltree', '--file', 'res/xml/data_extraction_rules.xml', args.apk)
assert 'cloud-backup' in extraction and 'device-transfer' in extraction and extraction.count('E: exclude') == 10

report = {'passed': not findings, 'filesScanned': counts, 'gitRevisions': len(revisions), 'findings': findings,
          'apkSha256': hashlib.sha256(args.apk.read_bytes()).hexdigest(),
          'apk': {'signatureV3': True, 'debuggable': False, 'cleartext': False, 'backupAndTransferExcluded': True, 'permissions': ['INTERNET']}}
args.report.parent.mkdir(parents=True, exist_ok=True)
args.report.write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps({'passed': report['passed'], 'filesScanned': counts, 'gitRevisions': len(revisions), 'findings': findings}))
raise SystemExit(0 if report['passed'] else 1)
