"""One-time, fail-if-present source checkpoint before the approved atlas work."""
from pathlib import Path
import datetime, hashlib, json, shutil

root = Path(__file__).resolve().parents[1]
target = root / 'Archives/Alpha-v0.0.14-Development-BeforeAtlas'
if target.exists():
    raise SystemExit('Pre-atlas checkpoint already exists; preserve it.')
rows = []
for folder in ('Assets', 'Packages', 'ProjectSettings', 'Tools', 'Tests'):
    for path in sorted((root / folder).rglob('*')):
        if not path.is_file() or any(p in ('bin', 'obj', '__pycache__') for p in path.relative_to(root).parts):
            continue
        relative = path.relative_to(root)
        copy = target / relative
        copy.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, copy)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        assert hashlib.sha256(copy.read_bytes()).hexdigest() == digest
        rows.append(dict(file=relative.as_posix(), sha256=digest, bytes=path.stat().st_size))
evidence = root / 'Docs/Validation/Alpha-v0.0.14'
reference = evidence / 'References/Aevoria_Fantasy_Atlas_Overhead.png'
reference.parent.mkdir(parents=True, exist_ok=True)
shutil.copy2(Path(r'C:\Users\Sean Marsh\Downloads\Aevoria_ Fantasy Atlas Overhead.png'), reference)
result = dict(capturedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
              archive=str(target), files=rows, referenceSha256=hashlib.sha256(reference.read_bytes()).hexdigest())
(evidence / 'pre-atlas-source-manifest.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
print(f'Preserved {len(rows)} source files and the supplied atlas reference.')
