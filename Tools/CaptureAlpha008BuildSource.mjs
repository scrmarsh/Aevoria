import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const cache=process.argv[2];if(!cache)throw Error('Pass the isolated Unity project used for the build');
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase();
const files=[];
function walk(directory){for(const entry of fs.readdirSync(directory,{withFileTypes:true})){const file=directory+'/'+entry.name;if(entry.isDirectory())walk(file);else if(/\.(cs|asmdef|hlsl|shader|txt|json)$/.test(file))files.push(file);}}
for(const dir of ['Assets/Scripts','Assets/Shaders','Assets/Resources/History'])walk(dir);
files.push('Packages/manifest.json','ProjectSettings/ProjectVersion.txt');
const manifest={capturedUtc:new Date().toISOString(),files:files.sort().map(file=>{const sha256=hash(file);if(hash(path.join(cache,file))!==sha256)throw Error('Built project source differs: '+file);return {file,sha256};})};
fs.writeFileSync('Docs/Validation/Alpha-v0.0.8/Addendum/build-source-manifest.json',JSON.stringify(manifest,null,2));
for(const file of ['CHANGELOG.md','Docs/THE_CHRONICLES_OF_AEVORIA.md']){const before=fs.readFileSync('Temp/Alpha008BeforeAddendum/'+file),after=fs.readFileSync(file);if(!after.subarray(0,before.length).equals(before))throw Error('Pre-addendum history changed: '+file);}
const referenceDir='Docs/Validation/Alpha-v0.0.8/Addendum/UserReferences';
const references=fs.readdirSync(referenceDir).filter(p=>p.endsWith('.png')).map(name=>{const sha256=hash(referenceDir+'/'+name);if(hash('C:/Users/Sean Marsh/Pictures/Screenshots/'+name)!==sha256)throw Error('Defect reference altered: '+name);return {name,sha256};});
fs.writeFileSync(referenceDir+'/sha256.json',JSON.stringify(references,null,2));
console.log(`Verified ${manifest.files.length} runtime source/configuration files, unchanged history prefixes, and ${references.length} original defect PNGs.`);
