import fs from 'node:fs';
const root='Docs/Validation/Alpha-v0.0.8/';
const read=p=>fs.readFileSync(root+p,'utf8').trim().split(/\r?\n/).slice(1).map(s=>s.split(',').map(Number));
const before=read('climate-height-probe-before.csv'),after=read('climate-height-probe.csv');
function curvature(rows){let north=0,east=0,count=0;for(let z=1;z<128;z++)for(let x=1;x<128;x++){const i=z*129+x;if(rows[i][4]<=5)continue;north+=Math.abs(rows[i-129][6]-2*rows[i][6]+rows[i+129][6]);east+=Math.abs(rows[i-1][6]-2*rows[i][6]+rows[i+1][6]);count++;}return {landSamples:count,meanNorthSecondDifference:north/count,meanEastSecondDifference:east/count};}
const result={seed:'shaping-continents',size:'Miniature',landmass:'Continents',climateVersions:[2,3],spacingMeters:1000,before:curvature(before),after:curvature(after),interpretation:'Second differences describe this rainfall raster only; they do not establish visual acceptance or universal climate quality.'};
fs.writeFileSync(root+'climate-probe-comparison.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
