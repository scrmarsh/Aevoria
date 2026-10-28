"""Verify protected working files and Git blobs against their accepted hashes."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--index', action='store_true', help='Check staged blobs instead of HEAD')
args = parser.parse_args()
manifest = json.loads((root / 'Docs/PROTECTED_ASSETS.json').read_text(encoding='utf-8'))
files = manifest['files']
changed_working, changed_git, missing = [], [], []
process = subprocess.Popen(
    ['git', '-c', 'safe.directory=' + root.as_posix(), 'cat-file', '--batch'],
    cwd=root, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
)
try:
    for item in files:
        path, expected = item['file'], item['sha256']
        source = root / path
        if not source.is_file() or hashlib.sha256(source.read_bytes()).hexdigest().upper() != expected:
            changed_working.append(path)
        query = (':' if args.index else 'HEAD:') + path
        process.stdin.write((query + '\n').encode('utf-8'))
        process.stdin.flush()
        header = process.stdout.readline().decode('utf-8').rstrip('\n')
        if header.endswith(' missing'):
            missing.append(path)
            continue
        _, kind, size = header.split()
        if kind != 'blob':
            raise RuntimeError('Expected a Git blob for ' + path)
        remaining = int(size)
        digest = hashlib.sha256()
        while remaining:
            chunk = process.stdout.read(min(remaining, 1024 * 1024))
            if not chunk:
                raise RuntimeError('Incomplete Git blob for ' + path)
            digest.update(chunk)
            remaining -= len(chunk)
        if process.stdout.read(1) != b'\n':
            raise RuntimeError('Invalid Git batch boundary')
        if digest.hexdigest().upper() != expected:
            changed_git.append(path)
finally:
    process.stdin.close()
    process.stdout.close()
    code = process.wait()
report = dict(reference='index' if args.index else 'HEAD', checked=len(files),
              changedWorkingFiles=changed_working, changedGitBlobs=changed_git,
              missingGitBlobs=missing)
passed = code == 0 and not (changed_working or changed_git or missing)
report['result'] = 'Passed' if passed else 'ReviewRequired'
print(json.dumps(report, indent=2))
raise SystemExit(0 if passed else 1)
