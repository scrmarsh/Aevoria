import fs from 'node:fs';
import crypto from 'node:crypto';
const base='Docs/Validation/Alpha-v0.0.8/Addendum',run=base+'/Rendered-delivery';
const read=p=>{const b=fs.readFileSync(p);return (b[0]===255&&b[1]===254?b.toString('utf16le'):b.toString('utf8')).replace(/^\uFEFF/,'');};
const json=p=>JSON.parse(read(p)),hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase();
const audit=json(run+'/audit.json'),extra=json(run+'/addendum.json'),views=json(run+'/views.json').views,launch=json(run+'/audit-launch.json');
const finalRun=base+'/Horizon-final',finalLaunch=json(finalRun+'/audit-launch.json'),horizon=json(finalRun+'/horizon.json');
if(!['overviewFade','firstPerson','thirdPerson','cleanup'].every(k=>horizon[k])||!read(finalRun+'/player.log').includes('AEVORIA008_HORIZON_PASS')||/Screen position out of view frustum|Exception:|Shader error in/.test(read(finalRun+'/player.log')))throw Error('Final horizon/native mode validation failed');
const playerLog=read(run+'/player.log');if(!playerLog.includes('AEVORIA008_AUDIT_PASS')||/Audit008 failed|Exception:|Shader error in/.test(playerLog))throw Error('Native player did not finish cleanly');
if(playerLog.includes('Screen position out of view frustum'))throw Error('Native frustum warning remains unresolved');
const binary='Builds/Alpha-v0.0.8-Expanded';
const deliveredBuild=json(binary+'/build-info.json');if(deliveredBuild.builtUtc!==finalLaunch.build.builtUtc||deliveredBuild.generatorVersion!==finalLaunch.build.generatorVersion)throw Error('Delivered build metadata differs from final native audit');
const finalSource=json(base+'/build-source-manifest.json').files,fullSource=json(base+'/build-source-manifest-full-run.json').files;
for(const entry of finalSource)if(hash(entry.file)!==entry.sha256)throw Error('Runtime source changed after build snapshot: '+entry.file);
const sourceDelta=finalSource.filter(e=>fullSource.find(f=>f.file===e.file)?.sha256!==e.sha256).map(e=>e.file).sort();
if(JSON.stringify(sourceDelta)!==JSON.stringify(['Assets/Scripts/Presentation/Alpha008Audit.cs','Assets/Scripts/Presentation/DistantMountainHorizon.cs','Assets/Shaders/WorldHorizon.shader']))throw Error('Final delta exceeds the documented horizon corrections and audit-only branch');
const semantic=json(base+'/world-assembly-comparison.json.log'),nativeEquivalence=json(base+'/world-native-equivalence.json');
if(semantic.methods!==334||semantic.differences.length||!nativeEquivalence.equal||nativeEquivalence.full.fingerprint!==nativeEquivalence.final.fingerprint)throw Error('World source/compiled-method/native fingerprint equivalence not established');
for(const [dll,key] of [['Aevoria.World.dll','worldAssemblySha256'],['Aevoria.Presentation.dll','presentationAssemblySha256']])if(hash(binary+'/Aevoria_Data/Managed/'+dll)!==finalLaunch[key])throw Error('Final native audit does not match delivered assembly: '+dll);
for(const k of ['continuity','cleanup','cancellation','exploration','excavation'])if(!audit[k])throw Error('Incomplete native lifecycle: '+k);
for(const k of ['preload','cancellation','zoom','toggles','flight','invalidSaveProtected','highAltitudeSave'])if(!extra[k])throw Error('Incomplete addendum check: '+k);
if(extra.minClearance<2.9||extra.beforeRenderChecks<1||audit.generations.length!==5||extra.preloadSeconds?.length!==5||extra.highSavedAltitude<=10000||!extra.rejectedSaveHashBefore||extra.rejectedSaveHashBefore!==extra.rejectedSaveHashAfter)throw Error('Incomplete camera/save/all-size evidence');
const cases=new Set(),testRows=[];
for(const [file,label] of [['editor-delivery.xml','Full Editor'],['editor-precision.xml','Final projection follow-up'],['play-publication.xml','Repeated full Play Mode'],['play-precision.xml','Projection flight/preload follow-up'],['play-horizon.xml','Final horizon/flight follow-up']]){
 const xml=read(base+'/'+file),tag=xml.match(/<test-run\s[^>]+>/)?.[0]??'',attr=k=>new RegExp(`\\b${k}="([^"]*)"`).exec(tag)?.[1];
 if(attr('result')!=='Passed'||+attr('failed')!==0)throw Error('Unresolved suite: '+file);
 testRows.push(`| ${label} | ${attr('passed')}/${attr('total')} | [XML](Validation/Alpha-v0.0.8/Addendum/${file}) |`);
 if(file.startsWith('editor'))for(const m of xml.matchAll(/<test-case\b[^>]*\bfullname="([^"]+)"/g))cases.add(m[1]);
}
const portable=(read(base+'/portable-delivery.log').match(/^PASS:/gm)||[]).length;if(portable!==115)throw Error('Unexpected portable result');
const models=read(base+'/Models/geography.csv').trim().split(/\r?\n/).length-1,ecology=read(base+'/Retained-ecology/ecology-long-run.csv').trim().split(/\r?\n/).length-1;
if(models!==10||ecology!==24)throw Error('Incomplete retained planetary/ecology cases');
let frameCount=0,outliers=0;const frameRows=[];
for(const m of audit.measurements){
 if(m.width!==2560||m.height!==1440||m.frames!==m.rendered)throw Error('Invalid native resolution/render counts: '+m.scenario);
 const lines=read(run+'/'+m.scenario+'-frames.csv').trim().split(/\r?\n/),head=lines.shift().split(','),column=head.indexOf('engineLoopMs');
 if(column<0||lines.length!==m.frames)throw Error('Missing frame interval column/rows');const bad=lines.filter(l=>+l.split(',')[column]>1000/60).length;
 frameCount+=m.frames;outliers+=bad;frameRows.push(`| ${m.scenario} | ${m.frames} | ${m.meanMs.toFixed(2)} | ${m.p95Ms.toFixed(2)} | ${m.p99Ms.toFixed(2)} | ${m.maxMs.toFixed(2)} | ${bad} | ${m.cpuMs.toFixed(2)} | ${m.gpuMs<0?'N/A':m.gpuMs.toFixed(2)} |`);
}
const memory=json(run+'/memory-maxima.json'),memoryText=memory.map(m=>`${m.Property}: ${m.Maximum<0?'N/A':Number(m.Maximum).toFixed(1)+' MiB'}`).join('; ');
const creatorPeak=Number(/ground ([\d.]+)/.exec(read(base+'/creator-model-probe.log'))?.[1]);if(!(creatorPeak>10000))throw Error('Creator altitude regression not exercised');
const f=n=>Number(n).toFixed(2),maxTrees=Math.max(...views.map(v=>v.trees)),maxLatency=Math.max(...views.map(v=>v.readySeconds));
const generationRows=audit.generations.map((g,i)=>{const forest=views.find(v=>v.scene==='05-'+g.size+'-forest'),preload=views.find(v=>v.scene==='addendum-'+g.size+'-preloaded-viewport');return `| ${g.size} | ${128*2**i} | ${f(g.seconds)} | ${f(forest?.readySeconds)} | ${preload?.trees??'N/A'} | ${f(extra.preloadSeconds[i])} |`;});
const escape=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const themes=[
 ['Flat grassland','matrix-grassland-120','matrix-grassland-120','Flat selected grassland; inspect the native PNG for texture repetition.'],
 ['Alpine relief','08-Alpine','matrix-heavy-mountains','High-relief mountain inspection; the older Alpine biome cell has different relief and framing.'],
 ['Mountain range and valleys','06-Colossal-mountain-system','06-Colossal-mountain-system','Expanded Colossal uses Sean’s exact seed and relief settings.'],
 ['Ground-level distant mountains','matrix-forest-inside','../Horizon-final/ground-after-overview-180','Final build, entering Explore from a planetary orbit; decorative distant layers are scenery.'],
 ['Forest vegetation','matrix-forest-850','matrix-forest-850','Counts describe submitted sites; shader distance and clipping affect visible trees.'],
 ['Desert detail','matrix-desert-120','matrix-desert-120','Dry scrub and cactus suitability varies with climate; natural open ground remains.'],
 ['River bends','10-river','10-river','The same bounded fitted paths drive banks and water; coarse-world sampling remains visible.'],
 ['Confluence','13-confluence','13-confluence','Compare actual joining water and valley geometry.'],
 ['Overview-to-ground zoom','03-Colossal-overview','addendum-Colossal-zoom-sequence-144','Five sequence frames plus the final ground view and CSV preserve the transition.'],
 ['Explore flight','28-explore-first-person','addendum-flight-third-person','Real controller flight, first/third-person captures and airborne archive check.'],
 ['Preloaded viewport','05-Medium-forest','addendum-Medium-preloaded-viewport','High Quality; progress waits for installed local and tagged regional resources.'],
 ['Cloud rendering enabled','matrix-cloud-overview','addendum-Colossal-cumulus-on','Normal overview uses five macro formations, with a bounded near/overview transition.'],
 ['Cloud rendering disabled','03-Colossal-overview','addendum-Colossal-clouds-off-weather-on','Underlying simulation continues while cloud geometry and shadows are hidden.'],
 ['Weather independently disabled','22-rain-cloud-shadow-wetness','addendum-precipitation-off-clouds-on','Known rainy forecast time; precipitation hidden while cloud rendering remains on.'],
 ['Large cumulus and shadows','matrix-cloud-regional','addendum-Medium-cumulus-on','Volume density and projected shadows share the same world-space formations.'],
 ['Boundary horizon','32-world-edge-east','addendum-boundary-horizon','Ground-level boundary scenery fades out for planetary overview.'],
];
for(const [,before,after]of themes)if(!fs.existsSync(run+'/'+after+'.png'))throw Error('Missing required theme: '+after);
const captures=fs.readdirSync(run).filter(p=>p.endsWith('.png'));for(const capture of captures){const b=fs.readFileSync(run+'/'+capture);if(b.readUInt32BE(16)!==2560||b.readUInt32BE(20)!==1440)throw Error('Wrong native capture resolution: '+capture);}
const oldReferences=json(base+'/../comparison-manifest.json').rows,oldTheme=['Grassland','Snow','Mountains',null,'Forest','Desert','Rivers','Rivers','Overall country',null,'Far detail','Clouds',null,null,'Clouds',null];
const manifest={equivalentCoordinates:false,themes:themes.map(([theme,before,after,note],i)=>{const previous=oldReferences.find(r=>r.theme===oldTheme[i]);return {theme,before:fs.existsSync(base+'/../Rendered-final/'+before+'.png')?'../Rendered-final/'+before+'.png':null,after:'Rendered-delivery/'+after+'.png',v006:previous?.v006?'../'+previous.v006:null,v007:previous?.v007?'../'+previous.v007:null,note};})};
fs.writeFileSync(base+'/comparison-manifest.json',JSON.stringify(manifest,null,2));
const card=(url,label)=>url?`<a href="${escape(url)}"><img loading="lazy" src="${escape(url)}" alt="${escape(label)}"><span>${escape(label)}</span></a>`:'<p>No equivalent historical frame available.</p>';
const sections=manifest.themes.map(t=>`<section><h2>${escape(t.theme)}</h2><p>${escape(t.note)}</p><p>Older representative references: ${[['v0.0.6',t.v006],['v0.0.7',t.v007]].filter(([,p])=>p).map(([label,p])=>`<a href="${escape(p)}">${label}</a>`).join(' · ')||'no equivalent capture available'}. Geography, scale and pose differ.</p><div class="pair">${card(t.before,'Initial v0.0.8, generator 6')}${card(t.after,'Expanded v0.0.8, generator 7')}</div></section>`).join('\n');
const references=fs.readdirSync(base+'/UserReferences').filter(p=>p.endsWith('.png')).map(p=>card('UserReferences/'+p,p)).join('');
fs.writeFileSync(base+'/Comparison.html',`<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Aevoria v0.0.8 expanded native evidence</title><style>body{background:#14201c;color:#e4eadc;font:16px/1.5 system-ui;margin:2rem auto;max-width:1500px;padding:0 1rem}a{color:#c7dfbd}img{width:100%;height:auto}section{margin:3rem 0}.pair{display:grid;grid-template-columns:1fr 1fr;gap:1rem}span{display:block}h1,h2{font-weight:500}@media(max-width:750px){.pair{grid-template-columns:1fr}}</style><h1>The Restoration of the World — continued</h1><p>Actual standalone 2560 × 1440 captures. Click for original pixels. Geography, camera, weather and settings differ between generations. These are representative comparisons, not controlled identical-coordinate A/B images. Numerical passes do not establish reference-level art or Sean’s acceptance.</p><p>The save-error message visible in some Medium captures is from the deliberate invalid-inventory regression. The inventory is restored immediately; later valid save/reload checks pass. It is retained as honest test evidence.</p><p><a href="../../../ALPHA_v0.0.8_ADDENDUM_TEST_RESULTS.md">Validation report</a> · <a href="../Comparison.html">Earlier v0.0.6/v0.0.7 comparison gallery</a> · <a href="Rendered-delivery/views.json">Camera/settings metadata</a></p>${sections}<section><h2>Sean’s original defect references</h2><div class="pair">${references}</div></section>`);
const report=`# Alpha v0.0.8 — continued restoration validation

October 23, 2026. **Development build; Sean’s final visual acceptance remains open.** The [implementation record](ALPHA_v0.0.8_ADDENDUM.md) expands the same milestone. The initial generator-6 [report](ALPHA_v0.0.8_TEST_RESULTS.md) and all earlier evidence remain historical records.

## Delivery and compatibility

Windows x64: [Aevoria.exe](../Builds/Alpha-v0.0.8-Expanded/Aevoria.exe). Application 0.0.8; generator 7, geology 3, hydrology 5, climate 3, archive 5. The theoretical mountain control remains 16,000 m. Earlier worlds require their preserved matching build; no creator archive was edited or silently regenerated. Original generator-6 v0.0.8 remains in its separate build folder.

Final build UTC: ${finalLaunch.build.builtUtc}. [Final build/native provenance](Validation/Alpha-v0.0.8/Addendum/Horizon-final/audit-launch.json). Delivered World SHA-256: ${finalLaunch.worldAssemblySha256}; Presentation: ${finalLaunch.presentationAssemblySha256}.

The complete five-size audit and benchmark below ran on the projection-corrected build from ${launch.build.builtUtc}, Presentation SHA-256 ${launch.presentationAssemblySha256}. The final build adds two scenery corrections: Explore ignores the disabled God camera's retained orbit distance, and the distant scenery pass runs after the skybox while retaining depth testing and writing. This avoids the observed skybox overwrite of distant geometry. Its other runtime source delta is an opt-in audit branch. [Source manifests](Validation/Alpha-v0.0.8/Addendum/build-source-manifest.json) are compared against the [full-run snapshot](Validation/Alpha-v0.0.8/Addendum/build-source-manifest-full-run.json); other product sources, other shaders and history assets remain identical. Rebuilding in a fresh Unity cache changed the World DLL hash despite unchanged source: [compiled comparison](Validation/Alpha-v0.0.8/Addendum/world-assembly-comparison.json.log) found no differences in 334 method bodies, type/field/member definitions and signatures (1,026 entries), and [the native Miniature fingerprint](Validation/Alpha-v0.0.8/Addendum/world-native-equivalence.json) matches exactly. These checks establish the recorded comparison, not a claim that the DLL files are byte-identical. A targeted native overview-to-first/third-person check, four cardinal captures, cleanup check and repeated real traveler/flight Play Mode test validate the final product delta. The full benchmark is retained under its own exact binary hash rather than relabeled as a rerun of the final horizon build.

## Executed checks

| Suite | Passed | Evidence |
| --- | --- | --- |
${testRows.join('\n')}
| Portable | ${portable} | [Log](Validation/Alpha-v0.0.8/Addendum/portable-delivery.log) |
| Repeated planetary configurations | ${models} | [Models and fingerprints](Validation/Alpha-v0.0.8/Addendum/Models/geography.csv) |
| Ecology checkpoints | ${ecology} | [1/10/100/1000-year populations](Validation/Alpha-v0.0.8/Addendum/Retained-ecology/ecology-long-run.csv) |

The full Editor suite and final projection follow-up cover **${cases.size} distinct cases**. The full suite and full Play Mode run precede the final clip corrections; nine focused Editor checks, two flight/preload Play Mode checks and the new complete standalone run follow them. World-generation source is unchanged by those camera corrections. Planetary cases cover two seeds across all five sizes with matching repeat fingerprints, finite fields and no dry channel terminals. The Archipelago regression retains at least three substantial islands. All four simulated populations persist in six long-run cases. The ridge-axis regression first failed on the previous native candidate with an exactly reversed direction (dot product -1). The clip regression first failed with a 5,734,400 m ground-view far plane. Tightening far clipping alone retained warnings at Colossal coordinates; a position-aware near-plane guard then passed the complete Colossal diagnostic without warnings. The short forest-motion A/B probe did not reproduce the warning in either mode and is not presented as proof of causation. Intermediate package, Windows cache-path, corrupt-cache exception and island-domain failures remain under the addendum directory; their failed results are not included as passes.

## Native standalone behavior and visual evidence

[Sixteen-theme gallery](Validation/Alpha-v0.0.8/Addendum/Comparison.html), [native audit](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/audit.json), [expanded checks](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/addendum.json), [camera/settings metadata](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/views.json).

All five sizes created and reloaded with mutable continuation intact. The save-error message visible in some Medium captures is deliberately injected by the invalid-inventory test; the inventory is restored immediately and the later valid save/reload succeeds. Generation cancellation, presentation cleanup, exploration and completed solid excavation passed. Installed preload completion, preload cancellation, logarithmic zoom, independent display controls, double-Space flight, airborne archive round-trip, rejected invalid inventory preserving the previous archive byte-for-byte, and a creator-case save above 10,000 m passed. The actual creator configuration has a ${f(creatorPeak)} m model peak; its last valid user archive cannot reveal the unknown transient failed pose.

Zoom recorded ${extra.zoomSamples} continuous samples, with minimum installed/authoritative clearance **${f(extra.minClearance)} m** and near clipping below clearance. Alternating rapid wheel checks also passed. The player executed ${extra.beforeRenderChecks} publication/settling safety checks before rendering. Cache hits accumulated: ${extra.cacheHits}. The five installed-preload re-request times are ${extra.preloadSeconds.map(f).join(", ")} s (size order Miniature through Colossal). Those requests occur after High Quality readiness, so they measure warm re-publication, not a cold first load. The saved high-mountain traveler elevation is ${f(extra.highSavedAltitude)} m. Rejected-save hashes before and after: ${extra.rejectedSaveHashBefore} / ${extra.rejectedSaveHashAfter}. Peak submitted tree count: ${maxTrees}. Maximum observed requested-mesh readiness: ${f(maxLatency)} s; this is the harness’s settled-resource criterion, not an input-latency measure or a promise of maximum detail at every pixel.

| Size | Width km | Create to active s | Selected forest settle s | Preloaded submitted trees | Warm preload re-publication s |
| --- | --- | --- | --- | --- | --- |
${generationRows.join('\n')}

## Frame pacing and resources

Hardware: ${audit.cpu}; ${audit.gpu}. **${frameCount} measured engine-loop intervals** each have a matching main-camera render callback at 2560 × 1440; **${outliers} exceed 16.667 ms**. These are Unity engine-loop intervals, not an external display-present capture. CPU aggregates may include parallel work; unavailable GPU timing must remain N/A. Process/WDDM maxima: ${memoryText}. WDDM allocations are process counters, not whole-board residency guarantees. Terrain triangle metadata excludes water and vegetation; the scenery triangle budget is included. Cached-data RAM limits exclude active meshes, native copies, world models and GPU allocations.

| Scenario | Frames | Mean ms | P95 ms | P99 ms | Max ms | >16.667 ms | Aggregate CPU ms/frame | GPU mean ms |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
${frameRows.join('\n')}

[Frame summary](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/summary.csv), [all outliers](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/outliers.csv), [process samples](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/process-memory.csv). Generation and blocking readiness windows are outside timed traversal segments and are reported separately in audit/views metadata. No universal hitch-free or 60 FPS guarantee is asserted.

## Practical limits and remaining acceptance

This is a bounded procedural alpha renderer. Geological/hydrological rasters, distant coast and pond triangulation, procedural materials, vegetation models, cloud art and scenery remain finite approximations. Some river surfaces remain faint against pale beds or coarsely sampled terrain; curved drainage and downhill tests do not establish complete visual water coverage. Some high-relief silhouettes remain stylized. Cloud volumes ray march a bounded set of formations and use approximate projected shadows; they are not a physical meteorology solver. Distant mountains are decorative, non-colliding scenery. The whole planetary surface is not resident at metre detail. Disk cache storage currently has no global pruning budget. This evidence supports the recorded fixes and behavior, not a claim that every reference-image gap, shoreline/LOD artifact, physical-input or presented-frame acceptance criterion is closed. Sean’s comparison of the unchanged native captures remains part of acceptance.

The original defect PNGs, diagnostic shader modes, failed/intermediate candidates and matching earlier player snapshots remain available. Changelog prose and previous Chronicle books were preserved; Book VIII was extended once, without a duplicate chapter. No civilization scope was added.
`;
fs.writeFileSync('Docs/ALPHA_v0.0.8_ADDENDUM_TEST_RESULTS.md',report);
console.log(JSON.stringify({portable,editorDistinct:cases.size,models,ecology,frameCount,outliers,extra,memory},null,2));
