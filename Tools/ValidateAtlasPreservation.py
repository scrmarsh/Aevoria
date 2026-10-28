"""Read-only preservation checks for Atlas, where geography edits are intentional.

Snapshot current player data on resume; compare against that snapshot afterwards.
Older save snapshots are references, never a reason to restore player files.
"""
from pathlib import Path
import argparse
import hashlib
import json

root = Path(__file__).resolve().parents[1]
evidence = root / 'Docs/Validation/Alpha-v0.0.14'
parser = argparse.ArgumentParser()
parser.add_argument('--snapshot', action='store_true')
parser.add_argument('--output', required=True)
args = parser.parse_args()

def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest().upper()

baseline = json.loads((root / 'Docs/Validation/Alpha-v0.0.13/baseline.json').read_text(encoding='utf-8-sig'))
player_root = Path(json.loads((root / 'Archives/PreGenesis-PlayerSaves/preservation-manifest.json').read_text())['source'])
snapshot = evidence / 'atlas-resume-player-files.json'
player_files = list((player_root / 'Worlds').rglob('*'))
player_files += [player_root / name for name in ('settings.xml', 'settings.xml.bak')]
current = {p.relative_to(player_root).as_posix(): digest(p) for p in player_files if p.is_file()}
if args.snapshot:
    with snapshot.open('x', encoding='utf-8') as stream:
        json.dump({'source': str(player_root), 'files': current}, stream, indent=2)
saved = json.loads(snapshot.read_text(encoding='utf-8'))['files']
changed = [p for p, value in saved.items() if current.get(p) != value]
added = sorted(set(current) - set(saved))
characters = [p['file'] for p in baseline['protectedCharacters'] if digest(root / p['file']) != p['sha256']]
chronicles = digest(root / 'Docs/THE_CHRONICLES_OF_AEVORIA.md') == baseline['chroniclesSha256']
report = dict(protectedCharacterFiles=len(baseline['protectedCharacters']), changedCharacters=characters,
              chroniclesUnchanged=chronicles, playerFilesCompared=len(saved), changedPlayerFiles=changed,
              addedPlayerFiles=added, playerRoot=str(player_root),
              scope='Saved worlds and settings only; disposable PresentationCache, Unity caches and logs are excluded. Intentional Atlas geography changes are outside this assertion.')
report['result'] = 'Passed' if not characters and chronicles and not changed and not added else 'ReviewRequired'
with (evidence / args.output).open('x', encoding='utf-8') as stream:
    json.dump(report, stream, indent=2)
print(json.dumps(report, indent=2))
if report['result'] != 'Passed':
    raise SystemExit(1)
