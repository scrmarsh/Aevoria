import fs from 'node:fs';
import path from 'node:path';
const project=path.resolve(process.argv[2]??'.');
const manifestFile=path.join(project,'Docs/Chronicles/chapters.json');
const base=path.dirname(manifestFile), manifest=JSON.parse(fs.readFileSync(manifestFile,'utf8'));
if(manifest.schemaVersion!==1 || manifest.archive!=='development-history' || manifest.narrator!=='They') throw new Error('Invalid archive identity');
const resolve=p=>path.resolve(base,p);
const body=fs.readFileSync(resolve(manifest.bodyFile),'utf8');
const anchors=new Set([...body.matchAll(/^#+\s+(.+)$/gm)].map(m=>m[1].trim().toLowerCase().replace(/[^\w\s-]/g,'').replace(/\s/g,'-')));
const definition=fs.readFileSync(resolve(manifest.versionDefinition),'utf8');
const version=/const string Number="([^"]+)"/.exec(definition)?.[1];
const ids=new Set();
for(const chapter of manifest.chapters) {
  if(ids.has(chapter.id)) throw new Error('Duplicate chapter ID: '+chapter.id);ids.add(chapter.id);
  if(!/^\d+\.\d+\.\d+$/.test(chapter.version) || !anchors.has(chapter.anchor)) throw new Error('Invalid version/anchor: '+chapter.id);
  if(!Object.values(chapter.theme).every(c=>/^#[\dA-F]{6}$/i.test(c))) throw new Error('Invalid theme: '+chapter.id);
  if(chapter.status==='planned-prophecy' && (chapter.releaseDate!==null || chapter.evidence.length)) throw new Error('Prophecy presented as released history');
  for(const evidence of chapter.evidence) if(!fs.existsSync(resolve(evidence))) throw new Error('Missing evidence: '+evidence);
}
if(version==='0.0.11') {
  if(manifest.chapters.some(c=>c.version===version)) throw new Error('v0.0.11 must use conventional development documentation; do not extend the literary Chronicles');
  if(!fs.existsSync(path.join(project,'Docs/ALPHA_v0.0.11_IMPLEMENTATION.md'))) throw new Error('Missing v0.0.11 implementation ledger');
} else if(!manifest.chapters.some(c=>c.version===version && c.status==='development-with-open-acceptance')) throw new Error('Current development chapter does not match version definition');
console.log(`PASS: ${ids.size} unique chapters; retained body anchors, themes and evidence valid; v${version} development documentation present`);
