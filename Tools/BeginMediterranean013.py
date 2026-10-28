"""Record the immutable v12 baseline before editing Mediterranean systems."""
from pathlib import Path
import sys,json,hashlib,datetime,subprocess,shutil
root=Path(sys.argv[1]).resolve();out=root/'Docs/Validation/Alpha-v0.0.13';out.mkdir(parents=True,exist_ok=True)
def sha(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest().upper()
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def git(*args):
    p=subprocess.run(['git','-c','safe.directory='+root.as_posix(),*args],cwd=root,capture_output=True,text=True)
    return dict(exitCode=p.returncode,stdout=p.stdout,stderr=p.stderr)
previous=read(root/'Docs/Validation/Alpha-v0.0.12/delivery-provenance.json')
runtime=root/'Builds/Alpha-v0.0.12';source=root/'Archives/Alpha-v0.0.12-Source'
assert all(sha(runtime/f['file'])==f['sha256'] for f in previous['binaries']), 'v12 runtime preservation failure'
assert all(sha(source/f['file'])==f['sha256'] for f in previous['sourceFiles']), 'v12 source preservation failure'
status=git('status','--porcelain=v1','--untracked-files=all');(out/'repository-status.txt').write_text(status['stdout'],encoding='utf-8')
head=git('rev-parse','--verify','HEAD');branch=git('symbolic-ref','--short','HEAD')
protected=[]
for folder in ['SourceAssets/Characters','SourceAssets/Equipment','Assets/Resources/Characters','Assets/Resources/Equipment']:
    protected += [p for p in (root/folder).rglob('*') if p.is_file()]
for name in ['HumanoidPresentation.cs','HumanoidPresentation.Authored.cs','HumanoidPresentation.Motion012.cs','AuthoredEquipment012.cs','EquipmentGrip012.cs','RomanSoldier.cs','CharacterDialogue.cs','ExperimentalExplorer.cs']:
    protected.append(root/'Assets/Scripts/Presentation'/name)
protected += [root/'Assets/Scripts/World/CharacterProfiles.cs',root/'Assets/Shaders/CharacterSurface.shader']
record=dict(startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),head=head,branch=branch,repositoryStatus='repository-status.txt',indexSha256=sha(root/'.git/index'),startingVersion='0.0.12',generatorVersion=10,saveSchema=8,priorRuntime=str(runtime),priorSource=str(source),verifiedRuntimeFiles=len(previous['binaries']),verifiedSourceFiles=len(previous['sourceFiles']),priorValidation='Docs/Validation/Alpha-v0.0.12/delivery-provenance.json',worldSha256=previous['worldSha256'],presentationSha256=previous['presentationSha256'],chroniclesSha256=sha(root/'Docs/THE_CHRONICLES_OF_AEVORIA.md'),protectedCharacters=[dict(file=p.relative_to(root).as_posix(),sha256=sha(p)) for p in sorted(set(protected))],worldSources=[dict(file=p.relative_to(root).as_posix(),sha256=sha(p)) for p in sorted((root/'Assets/Scripts/World').glob('*.cs'))])
(out/'baseline.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
shutil.copy2(Path(sys.argv[2]),out/'requirements.txt')
print('V12_PRESERVED runtimeFiles='+str(record['verifiedRuntimeFiles'])+' sourceFiles='+str(record['verifiedSourceFiles'])+' HEAD='+('unborn' if head['exitCode'] else head['stdout'].strip()))
