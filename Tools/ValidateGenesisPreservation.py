from pathlib import Path
import hashlib,json,sys
root=Path(__file__).resolve().parents[1];evidence=root/'Docs/Validation/Alpha-v0.0.14'
def digest(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest().upper()
baseline=json.loads((root/'Docs/Validation/Alpha-v0.0.13/baseline.json').read_text(encoding='utf-8-sig'))
characters=[x['file'] for x in baseline['protectedCharacters'] if digest(root/x['file'])!=x['sha256']]
originals=json.loads((evidence/'source-manifest.json').read_text())
geography=[x for x in originals if x['file'].startswith('Assets/Scripts/World/')]
changed_geography=[x['file'] for x in geography if digest(root/x['file'])!=x['sha256']]
saved=json.loads((root/'Archives/PreGenesis-PlayerSaves/preservation-manifest.json').read_text())
changed_saves=[x['file'] for x in saved['files'] if digest(Path(saved['source'])/x['file'])!=x['sha256']]
chronicles=digest(root/'Docs/THE_CHRONICLES_OF_AEVORIA.md')==baseline['chroniclesSha256']
assert not characters and not changed_geography and not changed_saves and chronicles
native=evidence/(sys.argv[1] if len(sys.argv)>1 else 'Rock8-LoadedV13-Materials-Miniature-aevoria-001')
fixture=json.loads((native/'baseline-save-fixture.json').read_text(encoding='utf-8-sig'))
source_archive_unchanged=digest(Path(fixture['source'])/'world.xml')==fixture['sha256']
views=json.loads((native/'views.json').read_text());exit_code=json.loads((native/'process-exit.json').read_text(encoding='utf-8-sig'))['exitCode']
assert source_archive_unchanged and exit_code==0 and views['loadedSaveApplicationVersion']=='0.0.13'
assert all(x['fingerprint']==views['loadedSaveFingerprint'] for x in views['views'])
report=dict(result='Passed',protectedCharacterFiles=len(baseline['protectedCharacters']),changedCharacters=characters,
    originalWorldSourceFiles=len(geography),changedOriginalWorldSources=changed_geography,
    actualPlayerSaveAndSettingsFiles=len(saved['files']),changedPlayerFiles=changed_saves,chroniclesUnchanged=chronicles,
    nativeBaselineArchiveOriginalUnchanged=source_archive_unchanged,nativeLoadedSaveVersion=views['loadedSaveApplicationVersion'],
    nativePlayerVersion=views['version'],nativeLoadExitCode=exit_code,canonicalFingerprint=views['loadedSaveFingerprint'],
    renderRecipeVersion=views['surfaceRecipeVersion'],renderRecipeIdentity=views['surfaceRecipeIdentity'],
    scope='Actual native production load of isolated pre-Genesis baseline save; original user files hash-verified. Broader mining/cave/gameplay acceptance remains pending.')
(evidence/('preservation-and-native-compatibility-'+native.name.split('-')[0]+'.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
