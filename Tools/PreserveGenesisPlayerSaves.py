from pathlib import Path
import hashlib, json, shutil
root=Path(__file__).resolve().parents[1]
source=Path.home()/'AppData/LocalLow/Aevoria/Aevoria'
target=root/'Archives/PreGenesis-PlayerSaves'
if target.exists(): raise SystemExit('Existing preserved save snapshot retained; no overwrite.')
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
entries=[]
if source.exists():
    for p in sorted(source.rglob('*')):
        if not p.is_file() or not (p.relative_to(source).parts[0]=='Worlds' or p.name=='settings.xml'): continue
        rel=p.relative_to(source);out=target/rel;out.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,out)
        before=digest(p);assert digest(out)==before
        entries.append(dict(file=rel.as_posix(),bytes=p.stat().st_size,sha256=before))
target.mkdir(parents=True,exist_ok=True)
(target/'preservation-manifest.json').write_text(json.dumps(dict(source=str(source),files=entries),indent=2),encoding='utf-8')
print(json.dumps(dict(preservedFiles=len(entries),bytes=sum(x['bytes'] for x in entries),destination=str(target))))
