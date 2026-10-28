import fs from 'node:fs';
import crypto from 'node:crypto';
const root='Docs/Validation/Alpha-v0.0.8',run=root+'/Rendered-final';
const read=p=>{const b=fs.readFileSync(p);return (b[0]===255&&b[1]===254?b.toString('utf16le'):b.toString('utf8')).replace(/^\uFEFF/,'');},json=p=>JSON.parse(read(p));
const audit=json(run+'/audit.json'),views=json(run+'/views.json').views,build=json('Builds/Alpha-v0.0.8/build-info.json');
const launch=json(run+'/audit-launch.json');
for(const [assembly,expected] of [['Aevoria.World.dll',launch.worldAssemblySha256],['Aevoria.Presentation.dll',launch.presentationAssemblySha256]])if(crypto.createHash('sha256').update(fs.readFileSync('Builds/Alpha-v0.0.8/Aevoria_Data/Managed/'+assembly)).digest('hex').toUpperCase()!==expected)throw Error('Delivered assembly differs from the native audit: '+assembly);
const xmlSummary=p=>{const s=read(p),tag=s.match(/<test-run\s[^>]+>/)[0],attr=k=>new RegExp(`\\b${k}="([^"]*)"`).exec(tag)?.[1];return {result:attr('result'),total:+attr('total'),passed:+attr('passed'),failed:+attr('failed')};};
const edit=xmlSummary(root+'/phase1-edit.xml'),play=xmlSummary(root+'/phase1-play.xml');
const retry=xmlSummary(root+'/phase1-edit-timeout-retry.xml');
const timeoutName='Aevoria.Tests.RefinementTests.ContinentalCoverageAndLargeCoordinatesAreDeterministic';
const failures=[...read(root+'/phase1-edit.xml').matchAll(/<test-case\b[^>]*result="Failed"[\s\S]*?<\/test-case>/g)];
if(play.failed||play.passed!==play.total||edit.failed!==1||failures.length!==1||!failures[0][0].includes(timeoutName)||!failures[0][0].includes('Timeout value of 180000 ms')||retry.failed||retry.total!==1||retry.passed!==1||!read(root+'/phase1-edit-timeout-retry.xml').includes(timeoutName))throw new Error('Unresolved automated failure; do not publish passing coverage');
const completedEditorCases=edit.passed+retry.passed;
const portable=(read(root+'/portable-final.log').match(/^PASS:/gm)||[]).length;
const living=(read(root+'/living-final.log').match(/^PASS:/gm)||[]).length;
if(portable!==111||living!==21)throw Error('Portable/living log counts differ from executed suites; check failures and encoding');
const number=n=>Number(n).toFixed(2),evidence=p=>`[${p}](${p})`;
let totalFrames=0,outliers=0,table=[],worst=[];
for(const m of audit.measurements){
 const csv=read(run+'/'+m.scenario+'-frames.csv').trim().split(/\r?\n/),header=csv.shift().split(','),records=csv.map(l=>Object.fromEntries(l.split(',').map((v,i)=>[header[i],Number(v)])));
 const bad=records.filter(r=>r.engineLoopMs>1000/60);outliers+=bad.length;totalFrames+=m.frames;
 table.push(`| ${m.scenario} | ${m.frames} | ${number(m.meanMs)} | ${number(m.p95Ms)} | ${number(m.p99Ms)} | ${number(m.maxMs)} | ${bad.length} | ${number(m.cpuMs)} | ${m.gpuMs<0?'N/A':number(m.gpuMs)} |`);
 worst.push(...bad.map(r=>({scenario:m.scenario,...r})));
 if(m.width!==2560||m.height!==1440||m.rendered!==m.frames)throw new Error('Native resolution/render confirmation failed: '+m.scenario);
}
worst.sort((a,b)=>b.engineLoopMs-a.engineLoopMs);fs.writeFileSync(run+'/all-outliers.json',JSON.stringify(worst,null,2));
const solid=views.find(v=>v.scene==='29-solid-excavation');
if(!solid||!['continuity','cleanup','cancellation','exploration','excavation'].every(k=>audit[k]))throw Error('Final native lifecycle evidence incomplete');
const latency=views.filter(v=>v.readySeconds>0).map(v=>v.readySeconds).sort((a,b)=>a-b),peakTrees=Math.max(...views.map(v=>v.trees)),peakRocks=Math.max(...views.map(v=>v.submergedRocks||0));
const memory=fs.existsSync(run+'/memory-maxima.json')?json(run+'/memory-maxima.json'):[];
const memoryText=memory.map(m=>`${m.Property}: ${m.Maximum<0?'N/A':number(m.Maximum)+' MiB'}`).join('; ');
const prefix=[];for(const [file,base] of [['CHANGELOG.md','Temp/Alpha008Baseline/CHANGELOG.md'],['Docs/THE_CHRONICLES_OF_AEVORIA.md','Temp/Alpha008Baseline/THE_CHRONICLES_OF_AEVORIA.md']]){
 const before=fs.readFileSync(base),after=fs.readFileSync(file);if(!after.subarray(0,before.length).equals(before))throw new Error('History prefix changed');prefix.push(`${file}: ${before.length} original bytes preserved (SHA-256 ${crypto.createHash('sha256').update(before).digest('hex')})`);
}
const rows=[
 ['Pixelated ground','Confirmed bilinear slope changes, fixed regional spacing and undersampled local normal derivatives','GeologyMap.Read; RegionalTerrain','Cubic shared tangents, adaptive sampling and local/regional mesh-scale normals with common border normals','Interpolation/normal tests; grass/forest near–far PNGs','Finite planetary raster and metre-scale Colossal precision remain'],
 ['Square terrain patches','Confirmed fixed rectangle joins, ecology moisture override and independent climate wind rows','WorldTerrainFields; WorldTerrain.Frag','Spacing-relative joins, continuous climatic materials and crosswind moisture exchange','Join/lifecycle tests; full native views','Residual geometry/LOD patches require visual review'],
 ['Centre-only high detail','Confirmed focus-square candidate discovery','WorldRenderer.RefreshDesired','Frustum ground-footprint and lookahead within tile budget','Moving traces and native edge views','Settled time covers requested meshes, not guaranteed highest detail at every pixel'],
 ['Grainy surfaces','Confirmed unfiltered high-frequency procedural bands','WorldTerrain.Frag','Per-band footprint filtering in world coordinates','Near/middle/far grass PNGs','Procedural material art remains limited'],
 ['Orange grass','Confirmed broad moisture/season overrides and warm tint','WorldTerrain.Frag','Climatic green/dry/forest palette; local lapse correction','Biome/season captures and retained weather tests','Weather and lighting still change appearance'],
 ['Missing vegetation','Confirmed height bounds, overhead distance and carved-bank water detection','WorldVegetation; HydrologyMap.IsRiverWater','Actual bounds, overhead visibility and true wetted-width eligibility','Determinism/above-water tests; overhead/inside forest frames','Submitted candidate counts include later shader clipping'],
 ['Empty deserts','Confirmed low density, row-order selection and short visibility','LivingWorldState.TryNatural; WorldNaturalDetails','Patchy hot-desert sites; nearest rings; scrub/cactus/dry grass','Desert near/middle/far and native cached-site counts','Cold/dry/open terrain remains deliberately sparse'],
 ['Deep narrow rivers','Confirmed unconstrained breach plus endpoint-only water profiles','HydrologyMap.BuildPaths/FitPathLevels/Carve','Physical path lows, downhill spill propagation, smooth centreline/level queries and graded valleys','Actual profile/depth tests and river frames','Natural depressions, confluences and distant sampling remain limited'],
 ['Hairline rivers','Confirmed physical width lost under coarse terrain triangles','HydrologyMap.RiverWidth; RegionalTerrain','Discharge widths and finer regional geometry; shared actual water','Source/river/mouth/confluence captures','Distant narrow channels can still be undersampled; no decorative overlay'],
 ['Blocky banks','Confirmed coarse samples and inconsistent derivative scale','WorldRenderer; RegionalTerrain','Denser regional field and coherent bank queries','LOD boundary tests and river traversal','Bank geometry is still finite resolution'],
 ['Polygon lakes','Confirmed world-scaled triangular shore field','InlandWaterLayout.RefineShores/BuildShore','Shared refined boundary with bounded interior merging','Uniform-level/ownership/budget tests; pond/deep-lake views','One-million-sample budget limits very large-world close shores'],
 ['Dark ponds','Confirmed shared navy palette and strong absorption','WorldWater.Frag','Freshwater palette and continuous depth absorption','Small pond and deep-lake captures','Turbidity, ice and lighting remain approximate'],
 ['Black ocean','Confirmed deep RGB approached near black','WorldWater.Frag','Brighter gradual deep ocean range','Deep ocean/coast captures','No full physical scattering model'],
 ['Opaque-looking shallows','Confirmed depth switch and insufficient bed treatment','WorldWater.Frag; WorldRenderer.BankSample','Actual rendered-depth transparency; lake ownership for bed shading; submerged rocks','Transparency 0/1/2 PNGs, source depth path and rock counts','No refraction or diving; inspect actual PNGs for artistic quality'],
 ['River lines across lakes','Suspected visible mismatch despite centroid ownership passes','InlandWaterLayout.ClipRiver/LakeAt','Shared refined lake field and matching path water levels','Retained overlap tests and inlet/outlet frames','Tests do not prove every shore pixel is correct'],
 ['White mountain blobs','Confirmed broad temperature/snow mask with aliased breakup','WorldTerrain.Frag; WorldEnvironment','Elevation-reference lapse, filtered exposure and steeper-face rock','Mountain/snow captures and regional normal tests','Snowy highland shape/reference fidelity remains open'],
 ['Vertical mountain walls','Confirmed coarse derivatives, strong short relief and narrow deep carving','GeologyMap.Shape/Elevation; HydrologyMap.Carve','Continuous slopes, bounded transport, crest/valley relief and broad drainage grading','Actual heights/profiles, multiple sizes and mountain PNGs','No claim of full geological realism'],
 ['Noisy square clouds','Confirmed rectangular draped blanket and unrelated static detail','WorldCloudLayer; WorldWeatherFields; WorldClouds','Soft individual forecast formations, world-day drift and shared shadows','Below/medium/overview and storm captures','Layered sheet art remains visibly non-volumetric'],
 ['Exterior brown wedges','Confirmed exterior sampler used fixed -1 instead of ocean-relative depth; coarse edge triangles suspected contributor','ProceduralGeography.ComputeElevation; WorldTerrainFields','Submerged ocean-relative exterior and consistent terrain-pass clipping','Exterior tests, clear whole-world overviews and four cloud-obscured close edge captures','Planar world boundary remains'],
 ['Overview/local discontinuity','Confirmed dissimilar sampling, normals and fixed joins','WorldRenderer; RegionalTerrain; WorldTerrainFields','Adaptive fields, matching sample-scale normals, staged atomic publication','Moving/zoom traces, lifetime tests and recorded settle latency','Not a proof of universal seamless detail']
];
const causeTable=rows.map(r=>'| '+r.join(' | ')+' |').join('\n');
const report=`# Alpha v0.0.8 — The Restoration of the World: validation

October 22-23, 2026. **Development recovery; final visual acceptance remains open. No public release declared.** The update fixes specific source mechanisms while retaining the planetary and living-world architecture. It does not establish that all reference-image gaps are closed.

## 1. Deliverable and identities

Windows x64: **M:\\Aevoria\\Builds\\Alpha-v0.0.8\\Aevoria.exe**. [Build metadata](../Builds/Alpha-v0.0.8/build-info.json). Unity 6000.3.27f1 / URP 17.3.0. Application 0.0.8; generator 6; geology 2; hydrology 4; inland layout 2; climate 3. Archive schema 5, ecology/forecast 2, living/seasonal 2 and atmospheric snapshot 1 remain. Settings/calendar schema remain 1. Physical height/profile/smooth-centreline changes and crosswind moisture transport justify the explicit geography/climate identities; rendering changes alone do not. Earlier saves and v0.0.6/v0.0.7 executable folders remain intact and require their matching generator.

## 2. Audit and source preservation

[Pre-correction analysis](ALPHA_v0.0.8_REGRESSION_ANALYSIS.md) was written before broad changes. Actual v0.0.6 source exists under Temp/Alpha007Baseline, and v0.0.7 was copied to Temp/Alpha008Baseline before editing. They are workspace snapshots, not authenticated release commits. Both historical players were launched with isolated v0.0.8 reference save roots. [Architecture and practical bounds](WORLD_RESTORATION.md) documents the final implementation.

## 3. Twenty reported regressions

“Confirmed” identifies a source mechanism; “suspected” retains attribution uncertainty. Automated passes and source repairs do not independently certify visual resolution.

| Symptom | Cause | Files/functions | Correction | Executed evidence | Remaining limit |
| --- | --- | --- | --- | --- | --- |
${causeTable}

## 4. What was recovered and preserved

Recovered elements of the older presentation include more legible grass/material color, visible coherent forest stands, brighter water and recognizable shallow beds. v0.0.7's five exact 128/256/512/1024/2048 km presets, connected uplift belts, bounded deterministic fields, climate, regional weather, saved wetness, ecology, seasonal memory, compact HUD, menus, maps and exploration remain. The theoretical relief control remains 16,000 m. This report does not claim v0.0.8 exceeds v0.0.6 in every scene.

## 5. Before/after visual evidence

[Twelve-theme comparison gallery](Validation/Alpha-v0.0.8/Comparison.html), [comparison manifest](Validation/Alpha-v0.0.8/comparison-manifest.json), [native camera/instance/latency metadata](Validation/Alpha-v0.0.8/Rendered-final/views.json). Images use representative compositions across changed generators, not identical-coordinate A/B tests. Some earlier “overview” labels were default local views; no equivalent v0.0.6 planetary/cloud-layer view is invented. Weather, framing, world dimensions and depth can differ. Open original 2560 × 1440 PNGs to avoid preview-resize artifacts.

Earlier complete recovery runs remain under Rendered-final-pre-continuity, Rendered-final-pre-climate and Rendered-before-local-normal. Initial rejected recovery captures remain under Smoke-initial, Review-initial and Review-refined; historical v0.0.6/v0.0.7 evidence is unchanged. The final native acceptance matrix includes grass/forest/desert at multiple ranges, forest ground view, mountain systems, streams/rivers/inlets/outlets, pond/deep lake/coast/deep ocean, transparency quality levels, cloud scales/storms, four edges and all five world sizes. A category's capture is evidence to inspect, not an automatic artistic pass.

## 6. Executed automated verification

| Suite | Executed result | Evidence |
| --- | --- | --- |
| Retained portable suite | ${portable} PASS | [Full log](Validation/Alpha-v0.0.8/portable-final.log) |
| Retained living/resource follow-up | ${living} PASS | [Log](Validation/Alpha-v0.0.8/living-final.log) |
| Unity EditMode - complete final suite | ${edit.passed}/${edit.total}; one runner timeout | [Unmodified full-run XML](Validation/Alpha-v0.0.8/phase1-edit.xml) |
| Six-world coverage/reconstruction case, isolated retry with 600 s limit | ${retry.passed}/${retry.total}; ${completedEditorCases} distinct Editor cases covered across both runs | [Retry XML](Validation/Alpha-v0.0.8/phase1-edit-timeout-retry.xml) |
| Unity PlayMode | ${play.passed}/${play.total} | [XML](Validation/Alpha-v0.0.8/phase1-play.xml) |
| Two seeds × five sizes, repeated construction | Ten repeatable fingerprints; finite fields, downhill receivers and zero dry terminals | [Model log](Validation/Alpha-v0.0.8/planetary-final.log), [CSV](Validation/Alpha-v0.0.8/Models/geography.csv) |
| Retained ecology | Two seeds × three sizes through 1, 10, 100 and 1000 years | [CSV](Validation/Alpha-v0.0.8/Retained-ecology/ecology-long-run.csv) |
| Native lifecycle | continuity=${audit.continuity}, cleanup=${audit.cleanup}, cancellation=${audit.cancellation}, exploration=${audit.exploration}, excavation=${audit.excavation} | [Audit](Validation/Alpha-v0.0.8/Rendered-final/audit.json) |
| History | Eight chapter associations and unchanged prior prose bytes | [Validation](Validation/Alpha-v0.0.8/chronicles-validation.txt) |

The complete EditMode run exceeded its default 180 s timeout in the case that constructs six full worlds. It took approximately 239 s under the concurrent model/portable workload, with no numeric assertion failure reported. Only that case's runner limit changed to 600 s; all land-coverage, bounds and repeated-height assertions remain unchanged. Its isolated retry above executes the whole case. The full-run failure remains visible and is not rewritten as a passing full run. The increased generator cost remains a performance limitation, measured separately in the native run.

All original assertions remain. The traveler fixture now selects an actual supported dry capsule spawn before exercising collision, jump, crouch and view modes; ecological land alone does not guarantee a safe traveler spawn. Explicit identity mirrors were updated. A sparse 100-square vegetation probe aliased narrow forest bands; its coverage increased ninefold while retaining all determinism, above-water and numeric count assertions. The tile churn test separately counts and caps one staged regional replacement while retaining one outgoing mesh per tile and the same settled ownership total. New tests cover interpolation continuity/no invented peaks, shoreline level/ownership/budget, actual channel depths and downhill profiles, ocean-relative exterior, regional normals/cancellation, crosswind coastal moisture transport, path-distance continuity across former search-window boundaries, local mesh-slope normals, graphics limits, closing during staged installation, baked excavation collision and closing during a replacement bake. Initial 71/73 and 73/74 failures, land-coverage failure and a transient staging compile error remain under Regres­sions; successful later results do not erase them.

## 7. Actual 1440p native measurements

${audit.cpu}; ${audit.gpu}. ${totalFrames.toLocaleString('en-US')} measured frames, with one MainCamera render callback per counted frame, all at 2560 × 1440. VSync off and unlimited frame cap during measurement. Each size starts with default graphics except cloud quality zero for unobscured terrain evidence; explicit high-quality scenarios enable additional load separately. Values are Unity engine-loop intervals with camera-render confirmation, **not independently measured display/presented FPS**. No intentional agent compiler or portable workload ran during this final measurement run; pre-existing user applications remained open.

| Scenario | Frames | Mean ms | p95 ms | p99 ms | Max ms | >16.667 ms | CPU ms/frame | GPU ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
${table.join('\n')}

${outliers} captured intervals exceed the 60 FPS frame budget. [All outliers](Validation/Alpha-v0.0.8/Rendered-final/all-outliers.json) retain every captured event, with raw per-scenario frame CSVs alongside them. CPU is aggregate process CPU per frame, not a main-thread-only counter. GPU timing and allocation counters use N/A when unsupported; negative sentinel values are not zero work. Profiler/counter lag makes correlation insufficient to prove cause. Regional native copies and excavation installation now participate in upload scopes; vegetation counters do not necessarily include every natural-detail operation.

## 8. Streaming, memory and headroom

Recorded settle time median ${number(latency[Math.floor(latency.length*.5)]??0)} s; maximum ${number(latency.at(-1)??0)} s. These timings wait for requested terrain meshes/LOD transitions, including the regional replacement; they do not establish highest local detail at every viewport pixel. Native images remain the full-viewport quality check. The same-camera diagnostic captures under Surface-probe and Surface-probe-bank-normal ruled out direct shadows, inland-water drawing, vegetation and bank attributes as the source of repeated dark river-bank dots. Flat normals removed the pattern and were used only as a diagnostic. Local field probes subsequently confirmed a physical fan in carved heights. Exhaustive polyline search alone was insufficient; the final smooth-centreline query removes the repeated fan in the numerical slope raster. [Before local field](Validation/Alpha-v0.0.8/local-height-probe-before.png), [after local field](Validation/Alpha-v0.0.8/local-height-probe-after.png). The local-normal correction also preserves the shared two-metre border stencil and joins it to mesh-spacing interior slopes over two rows; flat normals were used only to diagnose the artifact. Near selection retains its explicit tile budget, two workers and one local upload per frame. Regional work retains one active mesh plus one counted staged install, with coherent publication after native copies across frames.

Observed process-memory sample maxima: ${memoryText||'N/A — sampler output missing'}. WDDM process dedicated/shared allocation is not total-board VRAM or a guarantee of peak residency. Peak submitted forest candidates in recorded views: ${peakTrees}; shader ecological/resource/density clipping follows submission. High-quality forest and mountain scenarios are separate actual workloads; their names do not imply a single all-effects worst case.

## 9. Water and submerged geometry

Transparency levels 0/1/2 compare model, eye-depth and reconstructed world-depth paths. Actual submerged terrain remains visible through blended water; up to 32 eligible shallow rocks reuse bounded presentation geometry. Recorded maximum cached submerged rocks: ${peakRocks}. This is not refraction, a marine population or an underwater food web. Shoreline samples are shared with ownership, but large-world budget/LOD limits still apply. Narrow distant channels and visible inlet/outlet detail remain manual-review priorities.

## 10. Vegetation, deserts and clouds

Native overhead/inside forest views supplement the unchanged ecological authority and expanded placement regression sampling. Desert sites use seeded patchy hot-desert eligibility and present dry scrub, grass or cactus; open desert remains open. Cloud quality zero now disables both geometry and shadows, avoiding residual patterned shadows on a supposedly clear ocean. Higher settings have soft individual formation masks with the same forecast field, local latitude wind and shared shadow function. They remain layered sheet art, not volumetric clouds; their artistic quality is not certified by weather-consistency tests.

## 11. Excavation and retained gameplay

Worker indexing reduced the representative solid reconstruction from roughly 400,752 duplicated vertices to ${solid.solidVertices.toLocaleString('en-US')} vertices and ${solid.solidTriangles.toLocaleString('en-US')} triangles on the final terrain sample; geometry changed, so these counts are not an identical-coordinate benchmark. Winding/rim and persistent-edit tests remain. The preserved pre-climate run measured a 49.73 ms frame, 48.35 ms native installation and 43.28 ms synchronous collider assignment, confirming the dominant cost there. The final implementation pre-bakes one immutable mesh in a Unity worker job and publishes visual mesh, collision and terrain mask together after readiness. Closing completes owned work before releasing its mesh. Final measured mesh preparation: ${number(solid.solidPreparationMs)} ms; queued bake-to-ready wait: ${number(solid.solidBakeWaitMs)} ms; main-thread publication: ${number(solid.solidInstallMs)} ms, including collider assignment ${number(solid.solidColliderMs)} ms. Bake wait includes scheduling and frame polling and is not isolated bake CPU time. The excavation frame window above is the actual final hitch check. Camera/movement, F11/display rollback, F3/F4/maps, traveler tools/inventory, seasons/time and save continuation retain their regression coverage. Scripted exercise does not establish every physical keyboard/mouse interaction.

## 12. Generation and planetary limits

Native ready seconds include construction, environmental state, initial archive write and presentation readiness: ${audit.generations.map(g=>`${g.size} ${number(g.seconds)} s`).join('; ')}. These are individual observations, not statistical guarantees. Model-only construction timings in the CSV are a different workload. The exact Miniature/Continents rainfall probe confirmed row-based moisture transport caused the persistent horizontal stripes; raw and carved height rasters lacked those stripes. Climate 3 exchanges moisture between neighboring wind lanes before precipitation and blends prevailing direction through latitude transitions. [Before raster](Validation/Alpha-v0.0.8/climate-height-probe-before.csv), [after raster](Validation/Alpha-v0.0.8/climate-height-probe.csv), [before figure](Validation/Alpha-v0.0.8/field-probe.png), [after figure](Validation/Alpha-v0.0.8/field-probe-after.png). The native overview remains the visual confirmation. Compact-support channel weights also reach zero at conservative candidate boundaries, removing lookup-cell jumps without a visual-only blur. Geography remains planar; tectonics/erosion/climate/drainage are bounded approximations. Archipelago labels do not guarantee separated islands; sampled large worlds can still be dominated by one connected land body. No civilization, settlement or social simulation is added.

## 13. Records preserved

${prefix.map(p=>'- '+p).join('\n')}

Book VIII and its metadata are appended. Changelog tone follows Sean's preference. Roadmap/version/persistence/architecture records identify current changes while retaining prior evidence. No commit or staging mutation was performed. [Native launch and assembly fingerprints](Validation/Alpha-v0.0.8/Rendered-final/audit-launch.json) identify the delivered player used by the final run. Build metadata: ${JSON.stringify(build)}.

## 14. Remaining acceptance

Sean's subjective visual approval remains open. The native review still shows broad artificial snow-covered highlands, visibly angular small-pond shores, residual faint bank joins and layered elliptical cloud sheets. Close edge captures are substantially cloud-obscured; the clear whole-world overviews supplement the exterior sampler/clipping tests, without proving every local edge pixel. These are partial improvements, not closed defects. Review mountain/snow forms, residual rectangular/triangular material or geometry transitions, distant narrow water, detailed shores, desert composition and layered cloud appearance in the native player. Review measured outliers, long-session behavior and externally presented frame pacing before declaring smooth 60 FPS universally. Historical builds and unsuccessful iterations remain available for comparison. Automated passing results do not close these gates.
`;
fs.writeFileSync('Docs/ALPHA_v0.0.8_TEST_RESULTS.md',report.replace('Regres­sions','Regressions'));
console.log(`PASS: report records ${portable} portable, ${completedEditorCases} Editor cases across full run and disclosed timeout retry, ${play.passed} PlayMode, ${totalFrames} rendered-confirmed frames and ${outliers} outliers`);
