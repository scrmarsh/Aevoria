import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const root=path.resolve('.'),evidence=path.join(root,'Docs/Validation/Alpha-v0.0.10');
const json=file=>JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex').toUpperCase();
const validation=json(path.join(evidence,'validation-summary.json'));
if(validation.native.length!==5||validation.native.some(r=>r.errors||!r.persistence||!r.cleanup)||validation.play.failed||validation.terrain.failed)throw Error('Complete recorded validation before preservation');
const finalManifest=json(path.join(evidence,'build-source-final-delivery.json'));
const accepted=json(path.join(evidence,'build-source-audit-background-fixed.json'));
const finalSmoke=json(path.join(evidence,'NativeFinalDelivery/result.json'));
if(finalSmoke.errors||!finalSmoke.persistence||!finalSmoke.cleanup)throw Error('Final delivery native smoke must pass');
const source=path.join(root,'Archives/Alpha-v0.0.10-Source'),delivery=path.join(root,'Builds/Alpha-v0.0.10');
if(fs.existsSync(source)||fs.existsSync(path.join(evidence,'delivery-provenance.json')))throw Error('Preservation already exists; never overwrite a milestone');
if(hash(path.join(root,'.git/index'))!=='00024D9632812793FAFCE6418D7392644E0ABEB315014E843AE1A0740F416583')throw Error('Existing staging changed');
const acceptedFiles=new Map(accepted.files.map(item=>[item.file,item.sha256]));
const codeDifferences=finalManifest.files.filter(item=>/^Assets\/Scripts\//.test(item.file)&&item.file.endsWith('.cs')&&acceptedFiles.get(item.file)!==item.sha256).map(item=>item.file);
if(codeDifferences.some(file=>file!=='Assets/Scripts/Presentation/ApplicationMenu.cs'))throw Error('Review untested gameplay source changes before packaging: '+codeDifferences.join(', '));
for(const name of ['Aevoria.World.dll','Aevoria.Presentation.dll']){
  const key=name.includes('World')?'worldSha256':'presentationSha256';
  if(hash(path.join(delivery,'Aevoria_Data/Managed',name))!==finalManifest[key])throw Error('Delivery differs from its built-source manifest');
}
for(const file of ['CHANGELOG.md','Docs/THE_CHRONICLES_OF_AEVORIA.md']){
  const prior=fs.readFileSync(path.join(root,'Archives/Alpha-v0.0.9-Source',file));
  if(!fs.readFileSync(path.join(root,file)).subarray(0,prior.length).equals(prior))throw Error('Historical prose changed');
}
for(const [document,resource] of [['CHANGELOG.md','Assets/Resources/History/Changelog.txt'],['Docs/THE_CHRONICLES_OF_AEVORIA.md','Assets/Resources/History/Chronicles.txt']])if(hash(path.join(root,document))!==hash(path.join(finalManifest.validationProject,resource)))throw Error('In-game history differs from the final development record');
// Verify the entire preserved v0.0.9 source and original runtime files.
const earlier=json(path.join(root,'Docs/Validation/Alpha-v0.0.9/delivery-provenance.json'));
const generatedCacheDrift=[];
for(const item of earlier.sourceFiles){const actual=hash(path.join(earlier.sourceArchive,item.file));if(actual!==item.sha256){if(!/(^|\/)obj\//.test(item.file))throw Error('Preserved v0.0.9 authored source changed: '+item.file);generatedCacheDrift.push({file:item.file,expected:item.sha256,actual});}}
for(const item of earlier.binaries)if(hash(path.join(root,'Builds/Alpha-v0.0.9',item.file))!==item.sha256)throw Error('Preserved v0.0.9 binary changed: '+item.file);
for(const folder of ['Assets','Packages','ProjectSettings'])fs.cpSync(path.join(finalManifest.validationProject,folder),path.join(source,folder),{recursive:true});
for(const folder of ['Docs','Tools','Tests','SourceAssets'])fs.cpSync(path.join(root,folder),path.join(source,folder),{recursive:true,filter:file=>!file.startsWith(path.join(root,'Docs/Validation'))&&!/[\\/](bin|obj)([\\/]|$)/.test(file)});
for(const name of ['requirements.txt','UserReferences'])fs.cpSync(path.join(evidence,name),path.join(source,'Docs/Validation/Alpha-v0.0.10',name),{recursive:true});
for(const file of ['README.md','CHANGELOG.md'])fs.copyFileSync(path.join(root,file),path.join(source,file));
fs.copyFileSync(path.join(root,'Docs/ALPHA_v0.0.10_TEST_RESULTS.md'),path.join(delivery,'development-report.md'));
fs.copyFileSync(path.join(evidence,'validation-summary.json'),path.join(delivery,'validation-summary.json'));
function inventory(directory,prefix=''){
  return fs.readdirSync(directory,{withFileTypes:true}).flatMap(entry=>entry.isDirectory()?inventory(path.join(directory,entry.name),prefix+entry.name+'/'):[{file:prefix+entry.name,sha256:hash(path.join(directory,entry.name))}]);
}
const output={preservedUtc:new Date().toISOString(),status:'Alpha Development package; documented art/performance/physical/soak gates remain open',build:finalManifest.build,executable:path.join(delivery,'Aevoria.exe'),sourceArchive:source,sourceFiles:inventory(source),binaries:inventory(delivery),acceptedEvidence:'NativeSizeMatrix-BackgroundFixed and NativeCharacters-BackgroundFixed',finalSmoke:'NativeFinalDelivery',acceptedSourceManifest:'build-source-audit-background-fixed.json',finalSourceManifest:'build-source-final-delivery.json',codeDifferencesFromMatrix:codeDifferences,sourceDifferenceReason:'Only the About version footer and runtime development-history text were updated after the accepted size/character audits. Final native smoke repeats creation, a live year, save/load and cleanup.',worldAssemblyMatchesAcceptedMatrix:finalManifest.worldSha256===accepted.worldSha256,gitIndexSha256:hash('.git/index'),verifiedPreserved009AuthoredFiles:earlier.sourceFiles.filter(item=>!/(^|\/)(bin|obj)\//.test(item.file)).length,matchingOriginal009InventoryFiles:earlier.sourceFiles.length-generatedCacheDrift.length,generatedCacheDrift,verifiedPreserved009Binaries:earlier.binaries.length,knownLimits:'Docs/ALPHA_v0.0.10_TEST_RESULTS.md'};
fs.writeFileSync(path.join(evidence,'delivery-provenance.json'),JSON.stringify(output,null,2)+'\n');
fs.writeFileSync(path.join(delivery,'delivery-provenance.json'),JSON.stringify(output,null,2)+'\n');
fs.writeFileSync(path.join(source,'PRESERVATION.json'),JSON.stringify({...output,sourceFiles:undefined,binaries:undefined},null,2)+'\n');
console.log(`Preserved ${output.sourceFiles.length} source/document/asset files and ${output.binaries.length} delivery files; verified v0.0.9 authored source and ${earlier.binaries.length} runtime files unchanged; ${generatedCacheDrift.length} generated-cache differences recorded.`);
