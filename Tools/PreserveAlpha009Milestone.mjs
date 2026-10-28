import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const root=path.resolve('.'),evidence=path.join(root,'Docs/Validation/Alpha-v0.0.9');
const summary=JSON.parse(fs.readFileSync(path.join(evidence,'validation-summary.json'),'utf8'));
if(!summary.native?.completed||summary.native.evidence!=='Final-native-settled')throw Error('Corrected final native run must pass before preservation');
const source=path.join(root,'Archives/Alpha-v0.0.9-Source');
if(fs.existsSync(source))throw Error('Source milestone already exists; never overwrite');
const manifest=JSON.parse(fs.readFileSync(path.join(evidence,'build-source-settled-candidate.json'),'utf8'));
const delivery=path.join(root,'Builds/Alpha-v0.0.9');
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase();
if(hash(path.join(root,'.git/index'))!=='00024D9632812793FAFCE6418D7392644E0ABEB315014E843AE1A0740F416583')throw Error('Creator staging changed; inspect before preservation');
for(const [file,expected] of [['Aevoria.World.dll',manifest.worldSha256],['Aevoria.Presentation.dll',manifest.presentationSha256]])if(hash(path.join(delivery,'Aevoria_Data/Managed',file))!==expected)throw Error('Delivery differs from accepted candidate: '+file);
for(const file of ['CHANGELOG.md','Docs/THE_CHRONICLES_OF_AEVORIA.md']){const earlier=fs.readFileSync(path.join(root,'Temp/Alpha009Baseline',file));if(!fs.readFileSync(path.join(root,file)).subarray(0,earlier.length).equals(earlier))throw Error('Historical prefix changed: '+file);}
for(const folder of ['Assets','Packages','ProjectSettings'])fs.cpSync(path.join(manifest.validationProject,folder),path.join(source,folder),{recursive:true});
for(const folder of ['Tools','Tests','Docs'])fs.cpSync(path.join(root,folder),path.join(source,folder),{recursive:true,filter:p=>!p.startsWith(path.join(root,'Docs/Validation'))});
for(const file of ['README.md','CHANGELOG.md'])fs.copyFileSync(path.join(root,file),path.join(source,file));
const list=[];function walk(dir,prefix=''){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){const relative=prefix+entry.name;if(entry.isDirectory())walk(path.join(dir,entry.name),relative+'/');else list.push({file:relative,sha256:hash(path.join(dir,entry.name))});}}
walk(source);
const binaries=[];function buildWalk(dir,prefix=''){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){const relative=prefix+entry.name;if(entry.isDirectory())buildWalk(path.join(dir,entry.name),relative+'/');else binaries.push({file:relative,sha256:hash(path.join(dir,entry.name))});}}
buildWalk(delivery);
const output={preservedUtc:new Date().toISOString(),status:'Preserved Alpha Development milestone with documented visual/performance/acceptance limits',build:manifest.build,executable:path.join(delivery,'Aevoria.exe'),sourceArchive:source,sourceFiles:list,binaries,acceptedEvidence:summary.native.evidence,acceptedSourceManifest:'build-source-settled-candidate.json',caveEvidence:'Final-underground-native',caveSourceDifference:'Only the opt-in final-audit readiness helper and in-game history differ from FinalCandidate3; gameplay source and World assembly are unchanged.',gitIndexSha256:hash(path.join(root,'.git/index')),knownLimits:'Docs/ALPHA_v0.0.9_TEST_RESULTS.md'};
fs.writeFileSync(path.join(evidence,'delivery-provenance.json'),JSON.stringify(output,null,2)+'\n');
fs.writeFileSync(path.join(source,'PRESERVATION.json'),JSON.stringify({...output,sourceFiles:undefined,binaries:undefined},null,2)+'\n');
fs.copyFileSync(path.join(root,'Docs/ALPHA_v0.0.9_TEST_RESULTS.md'),path.join(delivery,'development-report.md'));
fs.copyFileSync(path.join(evidence,'validation-summary.json'),path.join(delivery,'validation-summary.json'));
fs.copyFileSync(path.join(evidence,'delivery-provenance.json'),path.join(delivery,'delivery-provenance.json'));
console.log(`Preserved ${list.length} source/document/tool files and ${binaries.length} accepted build files. Existing staging and historical prefixes verified.`);
