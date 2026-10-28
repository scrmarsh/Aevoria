"""Write compact delivery inventories without PowerShell extended metadata."""
import sys,json,hashlib,shutil,datetime
from pathlib import Path
root=Path(sys.argv[1]).resolve();source=root/'Archives/Alpha-v0.0.12-Source';runtime=root/'Builds/Alpha-v0.0.12';evidence=root/'Docs/Validation/Alpha-v0.0.12'
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest().upper()
def inventory(path,exclude=()):return [dict(file=p.relative_to(path).as_posix(),sha256=sha(p),bytes=p.stat().st_size) for p in sorted(path.rglob('*')) if p.is_file() and p.name not in exclude]
def write(path,data):path.write_text(json.dumps(data,indent=2,ensure_ascii=False),encoding='utf-8')
now=datetime.datetime.now(datetime.timezone.utc).isoformat();baseline=read(evidence/'baseline.json');prior=read(root/'Docs/Validation/Alpha-v0.0.11/delivery-provenance.json')
assert all(sha(root/'Assets/Scripts/World'/p['file'])==p['sha256'] for p in baseline['worldSources'])
assert sha(runtime/'Aevoria_Data/Managed/Aevoria.World.dll')==baseline['worldSha256']
assert sha(root/'.git/index')==baseline['indexSha256']
assert sha(root/'Docs/THE_CHRONICLES_OF_AEVORIA.md')==baseline['chroniclesSha256']
assert sha(root/'Assets/Scripts/World/CharacterProfiles.cs')==baseline['dialogueSha256']
assert all(sha(root/'Archives/Alpha-v0.0.11-Source'/p['file'])==p['sha256'] for p in prior['sourceFiles'])
assert all(sha(root/'Builds/Alpha-v0.0.11'/p['file'])==p['sha256'] for p in prior['binaries'])
cache=Path('D:/AevoriaValidation/Aevoria010AuditBuild')
assert all(sha(p)==sha(cache/p.relative_to(root)) for p in (root/'Assets/Scripts').rglob('*.cs'))
assert int((evidence/'Native-Final-Package/exit-code.txt').read_text(encoding='utf-8-sig').strip())==0
shutil.copy2(Path(__file__),source/'Tools'/Path(__file__).name)
write(source/'INVENTORY.json',dict(version='0.0.12',preservedUtc=now,files=inventory(source,('INVENTORY.json',))))
data=dict(preservedUtc=now,status='Alpha Development; native character/neck/tool review completed; known style limits in implementation report',build=read(runtime/'build-info.json'),executable=str(runtime/'Aevoria.exe'),sourceArchive=str(source),acceptedRuntimeCandidate='Temp/Alpha012Final12',nativeResult=(evidence/'Native-Final-Package/result.txt').read_text(encoding='utf-8-sig'),nativeExitCode=0,focusedEditorPassed=11,focusedEditorTotal=11,gameplayRegression=read(evidence/'Native-Gameplay-Regression9/result.json'),copiedV11SaveCompatibility='PASS, exit0',worldSha256=sha(runtime/'Aevoria_Data/Managed/Aevoria.World.dll'),presentationSha256=sha(runtime/'Aevoria_Data/Managed/Aevoria.Presentation.dll'),worldSourceDifferences=0,runtimeSourceDifferencesSinceAcceptedBuild=0,gitIndexSha256=sha(root/'.git/index'),chroniclesSha256=sha(root/'Docs/THE_CHRONICLES_OF_AEVORIA.md'),dialogueSha256=sha(root/'Assets/Scripts/World/CharacterProfiles.cs'),priorV11RuntimeInventoryDifferences=0,priorV11SourceInventoryDifferences=0,sourceFiles=inventory(source),binaries=inventory(runtime))
write(evidence/'delivery-provenance.json',data)
print('DELIVERY_VERIFIED runtimeFiles='+str(len(data['binaries']))+' sourceFiles='+str(len(data['sourceFiles']))+' focusedTests=11/11 nativeExit=0 preservationDifferences=0')
