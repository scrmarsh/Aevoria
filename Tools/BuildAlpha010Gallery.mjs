import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const root = path.resolve('Docs/Validation/Alpha-v0.0.10');
const escape = value => String(value).replace(/[&<>"']/g, c => ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;' }[c]));
const groups = [];
function group(title, description, directory, names) {
  const images = names.map(([file, caption]) => ({ file: `${directory}/${file}.png`, caption })).filter(item => fs.existsSync(path.join(root, item.file)));
  if (images.length) groups.push({ title, description, images });
}
group('Thirty-year society foundation', 'Actual 2560 × 1440 Windows captures. This earlier foundation candidate predates the later character and save-validation refinements.', 'NativeFoundation-RouteFix', [
  ['small-settlement','Geographically founded multi-building camp'],['settlement-inspection','Real settlement population and inventories'],['civilization-statistics','Civilization inspection'],['multiple-civilizations-political','Political influence and neutral land'],['founding-history','Factual founding history'],['settlement-after-thirty-years','Settlement after thirty live calendar years'],['development-history','Recorded development history'],['reloaded-civilization','Reloaded society']
]);
group('Playable character validation', 'Grounded native application captures from the background-fixed audit candidate. The doctor is a procedural approximation; facial likeness, cane contact and gait polish remain open art acceptance.', 'NativeCharacters-BackgroundFixed', [
  ['explorer-front','Default Explorer'],['roman-front','Optional Roman appearance'],['doctor-front','Unlocked doctor, left-hand cane'],['doctor-walking','Asymmetric right-leg walking presentation'],['doctor-flight','Flight presentation'],['doctor-freefall','Gravity and freefall presentation'],['doctor-subtitle','Offline character subtitle'],['doctor-reloaded','Selected doctor after actual archive reload']
]);
for (const size of ['Miniature','Small','Medium','Large','Colossal']) {
  const directory = `NativeSizeMatrix-BackgroundFixed/${size}`;
  if (!fs.existsSync(path.join(root, directory, 'result.json'))) continue;
  const result = JSON.parse(fs.readFileSync(path.join(root, directory, 'result.json'), 'utf8').replace(/^\uFEFF/, ''));
  group(`${size} native society`, `${result.initialCivilizations} actual civilizations; ${result.simulatedMonths} live months; persistence ${result.persistence ? 'passed' : 'failed'}; cleanup ${result.cleanup ? 'passed' : 'failed'}. Frame data was measured under concurrent validation workloads.`, directory, [
    ['multiple-civilizations-political','Geographic political overview'],['small-settlement','Actual founding structures'],['settlement-after-12-months','Settlement after one live year'],['reloaded-civilization','Reloaded authority and presentation']
  ]);
}
if(fs.existsSync(path.join(root,'NativeFinalDelivery/result.json')))group('Exact final development executable','Final package creation, live-year progression, exact save/reload and cleanup smoke test.','NativeFinalDelivery',[
  ['small-settlement','Final executable founding camp'],['multiple-civilizations-political','Final executable political overview'],['settlement-after-12-months','Final executable after twelve live months'],['reloaded-civilization','Final executable reload']
]);
const items = groups.flatMap(g => g.images).map(item => ({...item, sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,item.file))).digest('hex').toUpperCase()}));
fs.writeFileSync(path.join(root, 'gallery-images.json'), JSON.stringify({generatedUtc:new Date().toISOString(),nativeCaptures:true,images:items},null,2));
const html = `<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Aevoria 0.0.10 native evidence</title><style>body{margin:0;background:#111a20;color:#e5e8e6;font:16px/1.6 system-ui,sans-serif}main{max-width:1500px;margin:auto;padding:32px}h1,h2{color:#d8c391}p{max-width:1000px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(390px,1fr));gap:20px}figure{margin:0;background:#1c2a32;border:1px solid #3e4c52}img{display:block;width:100%;height:auto}figcaption{padding:12px}a{color:#a9d9e0}section{margin:42px 0}.note{padding:16px;background:#24333a;border-left:3px solid #b69c67}@media(max-width:500px){main{padding:16px}.grid{grid-template-columns:1fr}}</style><main><h1>Aevoria Alpha v0.0.10 · native evidence</h1><p class="note">These are actual Windows application renders. Open an image for its original resolution. Numerical audit success does not declare reference-level art, physical-input acceptance or a public release. Earlier failed and superseded observations remain preserved in their original folders.</p>${groups.map(g=>`<section><h2>${escape(g.title)}</h2><p>${escape(g.description)}</p><div class="grid">${g.images.map(i=>`<figure><a href="${encodeURI(i.file)}"><img loading="lazy" src="${encodeURI(i.file)}" alt="${escape(i.caption)}"></a><figcaption>${escape(i.caption)}</figcaption></figure>`).join('')}</div></section>`).join('')}<p><a href="DEVELOPMENT_CHECKPOINT.md">Executed checkpoint</a> · <a href="gallery-images.json">Image hashes</a></p></main></html>`;
fs.writeFileSync(path.join(root,'Comparison.html'),html);
console.log(`Created native gallery with ${items.length} actual captures in ${groups.length} groups.`);
