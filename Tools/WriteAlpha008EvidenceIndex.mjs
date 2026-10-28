import fs from 'node:fs';
import crypto from 'node:crypto';
const base='Docs/Validation/Alpha-v0.0.8/Addendum';
const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase();
const local=process.env.LOCALAPPDATA.replaceAll('\\','/');
const pairs=[
 ['Initial generator-6 delivery','../Rendered-final','Builds/Alpha-v0.0.8'],
 ['Pre-ridge-correction full candidate','Rendered-before-ridge-axis','Temp/Alpha008BeforeRidgeAxis/Build'],
 ['Stable ridge-axis diagnostic','Diagnostics-ridge-axis','Temp/Alpha008RidgeAxisOnly/Build'],
 ['Continuous plate-boundary diagnostic','Diagnostics-continuous-belts',local+'/Temp/Aevoria008AddendumValidation/Builds/Alpha-v0.0.8-ContinuousBelts'],
 ['Full candidate before view-dependent far clip','Rendered-before-view-clip','Temp/Alpha008BeforeViewClip/Build'],
 ['Full candidate before position-aware near clip','Rendered-before-precision-clip','Temp/Alpha008BeforePrecisionClip/Build'],
 ['Non-reproducing short projection A/B diagnostic','Projection-precision-probe',local+'/Temp/Aevoria008AddendumValidation/Builds/Alpha-v0.0.8-PrecisionProbe'],
 ['Complete Colossal projection diagnostic','Colossal-precision-diagnostic',local+'/Temp/Aevoria008AddendumValidation/Builds/Alpha-v0.0.8-PrecisionDiagnostic'],
 ['Complete projection-corrected five-size audit','Rendered-delivery','Temp/Alpha008FullProjectionValidated/Build'],
 ['Horizon before skybox ordering correction','Horizon-before-sky-order','Temp/Alpha008HorizonBeforeSkyOrder/Build'],
 ['Final horizon-corrected delivery','Horizon-final','Builds/Alpha-v0.0.8-Expanded'],
];
const runs=pairs.map(([label,evidence,player])=>{
 const launch=json(base+'/'+evidence+'/audit-launch.json');
 for(const [name,key] of [['Aevoria.World.dll','worldAssemblySha256'],['Aevoria.Presentation.dll','presentationAssemblySha256']])if(hash(player+'/Aevoria_Data/Managed/'+name)!==launch[key])throw Error('Preserved player does not match '+label+': '+name);
 return {label,evidence,player,worldAssemblySha256:launch.worldAssemblySha256,presentationAssemblySha256:launch.presentationAssemblySha256};
});
const indexSha256=hash('.git/index');if(indexSha256!=='00024D9632812793FAFCE6418D7392644E0ABEB315014E843AE1A0740F416583')throw Error('Git index differs from the initial observation; do not reset it');
fs.writeFileSync(base+'/evidence-index.json',JSON.stringify({note:'Unreleased intermediate candidates retain their own matching executable snapshots; they are not relabeled as final validation.',indexSha256,runs},null,2));
console.log(`Verified ${runs.length} native evidence/player pairs and the unchanged Git index.`);
