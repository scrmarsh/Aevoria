"""Preserve the actual pre-Genesis development state without relabeling it a release."""
from pathlib import Path
import collections, datetime, hashlib, json, shutil, subprocess, zipfile

root = Path(__file__).resolve().parents[1]
evidence = root / 'Docs/Validation/Alpha-v0.0.14'
archive = root / 'Archives/Alpha-v0.0.13-Development-BeforeGenesis'
player = root / 'Builds/Alpha-v0.0.13-Development-BeforeGenesis'
build = root / 'Temp/Alpha013Climate5'
if archive.exists() or player.exists() or (evidence / 'baseline.json').exists():
    raise SystemExit('Baseline destinations already exist; preserve them and inspect baseline.json.')
evidence.mkdir(parents=True, exist_ok=True)
def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024*1024), b''): h.update(block)
    return h.hexdigest().upper()
def manifest(base):
    return [dict(file=p.relative_to(base).as_posix(), bytes=p.stat().st_size, sha256=digest(p))
            for p in sorted(base.rglob('*')) if p.is_file()]
source_files = []
for folder in ('Assets','Packages','ProjectSettings','Tools','Tests'):
    for p in sorted((root / folder).rglob('*')):
        if p.is_file() and not any(x in ('bin','obj','__pycache__') for x in p.relative_to(root / folder).parts):
            source_files.append(p)
for relative in ('CHANGELOG.md','README.md','Docs/AEVORIA_RESUME.md','Docs/THE_CHRONICLES_OF_AEVORIA.md',
                 'Docs/ALPHA_v0.0.13_DEVELOPMENT.md','Docs/Validation/Alpha-v0.0.13/DEVELOPMENT_CHECKPOINT.md',
                 'Docs/Validation/Alpha-v0.0.13/phase4-native-acceptance.json'):
    p = root / relative
    if p.exists(): source_files.append(p)
for p in source_files:
    target = archive / p.relative_to(root)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(p, target)
shutil.copytree(build, player)
source_manifest = manifest(archive)
player_manifest = manifest(player)
assert all(digest(root / item['file']) == item['sha256'] for item in source_manifest)
assert all(digest(build / item['file']) == item['sha256'] for item in player_manifest)
baseline12 = json.loads((root / 'Docs/Validation/Alpha-v0.0.13/baseline.json').read_text(encoding='utf-8-sig'))
changed = [x['file'] for x in baseline12['protectedCharacters'] if digest(root / x['file']) != x['sha256']]
assert not changed, changed
assert digest(root / 'Docs/THE_CHRONICLES_OF_AEVORIA.md') == baseline12['chroniclesSha256']
archive_zip = evidence / 'baseline-development-player.zip'
with zipfile.ZipFile(archive_zip, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for item in player_manifest: z.write(player / item['file'], item['file'])
asset_types = collections.defaultdict(lambda: dict(files=0, bytes=0))
for p in (root / 'Assets').rglob('*'):
    if p.is_file():
        row = asset_types[p.suffix.lower() or '<none>']; row['files'] += 1; row['bytes'] += p.stat().st_size
saves = root / 'UserData'
save_manifest = manifest(saves) if saves.exists() else []
for name, data in [('source-manifest.json',source_manifest),('player-manifest.json',player_manifest),
                   ('asset-usage.json',asset_types),('workspace-save-manifest.json',save_manifest)]:
    (evidence / name).write_text(json.dumps(data,indent=2),encoding='utf-8')
result = dict(capturedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
    sourceVersion='0.0.13 Development, unfinished Mediterranean update',latestCompletedRelease='0.0.12',
    generator=11,archiveSchema=8,sourceArchive=str(archive),developmentPlayer=str(player),
    sourceFiles=len(source_manifest),playerFiles=len(player_manifest),
    installedDevelopmentBytes=sum(x['bytes'] for x in player_manifest),
    compressedDevelopmentZipBytes=archive_zip.stat().st_size,
    compressedScope='Exact complete development build, including retained debug-support files; not a shipping release',
    protectedCharacterFiles=len(baseline12['protectedCharacters']),changedCharacters=changed,chroniclesUnchanged=True,
    indexSha256=digest(root / '.git/index'),
    baselineScreenshots=['Docs/Validation/Alpha-v0.0.13/Climate4-Full-Miniature-aevoria-001',
                         'Docs/Validation/Alpha-v0.0.13/Climate5-Climate-Colossal-aevoria-001'],
    performance='Existing diagnostic evidence retained; fresh uncontended native baseline is required',
    missingReference='Previously generated overhead concept map has not been identified in the project',
    saves='Workspace UserData inventoried if present; all other player save roots left untouched')
(evidence / 'baseline.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
