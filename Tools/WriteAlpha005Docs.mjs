import fs from 'node:fs';
const append=(file,marker,text)=>{if(!fs.readFileSync(file,'utf8').includes(marker))fs.appendFileSync(file,'\n\n'+text+'\n');};
append('CHANGELOG.md','## Alpha v0.0.5',`## Alpha v0.0.5 - Living Ecosystems

- **Status:** Development implementation; final acceptance remains open. No public release declared.
- **Validation date:** October 22, 2026. Measurements, screenshots and remaining gates belong in the [current report](Docs/ALPHA_v0.0.5_TEST_RESULTS.md).
- **Added:** Regional ecological authority independent of rendered chunks; six vegetation pools; four wildlife feeding groups and immutable species traits; conservative migration across a geographic graph; deterministic drought, fire, flood, cold and heat; persistent recovery and bounded structured environmental history; F4 inspection and six regional maps; bounded instanced animal silhouettes; Book V, The First Stirring.
- **Changed:** World archives use schema 3 with ecology version 1. Compatible worlds retain plants, populations, events, burn scars, stand age, history, monthly progression and random state. Recent worlds show actual saved year, last played, seed and size. Older incompatible archives remain visible and untouched; an explicit migration is still required.
- **Improved:** Forest candidate sites retain stable identities as regional coverage changes. Seasonal moisture, browsing, grazing, mortality and recruitment affect habitat. Fire leaves damage that outlasts its active event. Animals remain a local view of populations that continue beyond the camera.
- **Fixed:** Save As respects the world-name length limit. Ecology resources and global shader ownership are released when a world closes. Changelog labels follow their actual entry headings. The creator's 3,200 m mountain-relief ceiling and earlier changelog wording remain intact.
- **Performance:** Monthly aggregate ecology is separated from render frames and 5 Hz local wildlife behavior. Regions, animals, history and draw resources have explicit bounds. Accelerated tests retain raw long-run statistics; actual rendered-player frame traces retain outliers and unavailable counter labels.
- **Testing:** Earlier portable and Unity suites are retained. Added ecological determinism, migration conservation, disturbance/recovery, invalid-state, persistence-continuation, view-immutability and cleanup coverage. Six seed/size combinations exercise 1, 10, 100 and 1,000 years. Final totals and standalone outcomes are linked above, rather than inferred from a successful build.
- **Known issues and limitations:** Regional ecosystems are coarse aggregate models, with implicit seed-bank recruitment and simplified food webs. Animals are procedural placeholders without production animation. Floods affect ecology without dynamic flood-water geometry; fires show persistent damage without flame/smoke effects. Schema-2 migration UI, physical acceptance, reference-level foliage, exact coastal habitat areas, detailed pathfinding and multi-hour/presented-frame/VRAM acceptance remain open. No peoples, settlements or civilizations are introduced.
- **Git references:** None available; repository has no commits. Existing staging is preserved.`);
append('Docs/THE_CHRONICLES_OF_AEVORIA.md','# CHAPTER V - THE FIRST STIRRING',`# CHAPTER V - THE FIRST STIRRING

### Book V · Alpha v0.0.5 · Development account · acceptance remains open

*Moss green, warm earth, and the amber of a clearing after rain.*

The valley had become very good at remaining a valley.

They returned to it after an absence and found the same trees in the same places, making the same claim upon the light. The grass had suffered no hunger. Nothing had eaten it. Nothing had failed to return.

There was comfort in this arrangement, but comfort was a poor witness for life.

They began with appetite. Grass could increase under favorable skies, and deer could diminish it. Woodland offered another meal. The small creatures required less room and made more of their opportunities. A predator arrived with a claim that could be met only by the living inventory of its neighbors.

The first difficulty was arithmetic. A hungry animal could not eat a negative meadow. A pack could not grow without limit because a calculation had forgotten winter. They placed boundaries around the quantities, then allowed the quantities to move within them. A boundary could make the account coherent; it could not, by itself, make the account true.

So the years were allowed to pass.

A clearing acquired more grass than it had begun with. A forest lost some of its earlier abundance. Across longer intervals, different regions found different balances. They kept the totals, including the declines. Permanence would have been a suspicious achievement for a wilderness.

Movement required another kind of restraint. Two favorable places on opposite shores were not neighbors merely because a table put them close together. They asked the ground between them whether passage was possible. Water and high stone could refuse. A river could make the journey harder without declaring the whole bank uninhabitable.

The animals visible to the observer were a small vocabulary of bodies and legs. They did not contain all the animals in the world. That distinction mattered most when the observer turned away. The valley continued its account in the absence of an audience.

Then a dry month persisted beyond its welcome.

Growth weakened. A fire spent what the forest had accumulated. When the active event ended, the ground did not immediately forgive it. The scar remained in the saved world; the canopy had to recover through the slower terms of its own existence. They found this delay more persuasive than a spectacular flame would have been. The flame itself remained unfinished.

Memory had acquired consequences.

They closed an account and opened it again. The next uncertain choice had to follow the last one, rather than begin a new performance under an old name. Compatible worlds carried their populations and wounds together. Older accounts were left intact where a safe translation had not yet been written. An admission of unreadiness was preferable to an invented past.

**A place became alive when returning to it no longer guaranteed finding it unchanged.**

Nobody had named the clearing. Nobody had claimed the river or mistaken a boundary for a right. Those were future difficulties. For now there was growth, hunger, movement, damage, and the unfinished possibility of recovery.

They left room in the record for what the next observation might contradict.

*Implemented ledger:* regional vegetation and wildlife, monthly seasonal updates, food limitation, conservative migration, condition-dependent disturbances, persistent recovery, bounded environmental events, compatible save continuation, local silhouette rendering and ecological inspection. [Architecture and exact limits](ECOLOGY.md). [Validation and surviving measurements](ALPHA_v0.0.5_TEST_RESULTS.md).

*Open acceptance and prophecy:* production animal animation, detailed plant dispersal, dynamic flood surfaces, flame/smoke effects, old-world migration, full reference fidelity and long-session acceptance remain unfinished. The first people have not arrived. No release date is asserted.`);
const manifestFile='Docs/Chronicles/chapters.json';const manifest=JSON.parse(fs.readFileSync(manifestFile,'utf8'));
if(!manifest.chapters.some(c=>c.version==='0.0.5')){manifest.chapters.push({id:'first-stirring',version:'0.0.5',stage:'Alpha',title:'The First Stirring',status:'development-with-open-acceptance',releaseDate:null,anchor:'chapter-v---the-first-stirring',theme:{moss:'#507A48',earth:'#826340',amber:'#D4AC62'},evidence:['../ALPHA_v0.0.5_TEST_RESULTS.md','../ECOLOGY.md']});fs.writeFileSync(manifestFile,JSON.stringify(manifest,null,2)+'\n');}
for(const [file,title,text] of [
['Docs/ARCHITECTURE.md','Alpha v0.0.5 ecology','Pure regional ecology now lives in the World assembly, with no Unity or Presentation dependency. Persistence schema 3 owns a versioned deep snapshot; Presentation supplies bounded instanced wildlife, state textures and inspection. See [ecology contracts](ECOLOGY.md). Generator 4 and earlier assembly boundaries are retained.'],
['Docs/PERSISTENT_WORLDS.md','Alpha v0.0.5 compatibility','Current archives use schema 3 and require ecology version 1, validated against seed, region resolution and calendar. Compatible saves restore full ecology and deterministic continuation. Alpha v0.0.4 schema 2 requires an explicit migration; no migration UI ships, so load rejects without changing the original. Metadata remains visible. Atomic writes, backup recovery and future-schema protection continue. See [ecology persistence](ECOLOGY.md).'],
['Docs/PERFORMANCE_STRATEGY.md','Alpha v0.0.5 ecology budgets','Standard worlds use 256/1,024/4,096 ecological cells, twelve monthly steps per year, at most 128 local wildlife proxies and four shared wildlife meshes. Migration uses reusable buffers; region state is independent of terrain streaming. Visual behavior runs at 5 Hz; state texture interpolation at 4 Hz; map contents refresh at most once per second unless the monthly revision changes. History bounds retain completed records plus active events. Raw long-run CSV, standalone traces and unavailable counters are preserved in the current validation directory.'],
['Docs/ROADMAP.md','Alpha v0.0.5 implementation addendum','Sean requested Living Ecosystems. Regional ecology, wildlife, migration, disturbances, environmental memory and inspection are implemented in development; [current report](ALPHA_v0.0.5_TEST_RESULTS.md) records validation and remaining gates. Application 0.0.5, generator 4, archive schema 3 and ecology 1 are separate identities. Before Alpha v0.0.6, prioritize a safe schema-2 migration path, performance outliers, visual/physical acceptance and long-session reliability. Future social systems should consume stable region IDs, fertile habitat, water, forest, wildlife and hazard data; no social simulation ships in this update.']
])append(file,'## '+title,'## '+title+'\n\n'+text);
console.log('Alpha005 documentation appended; earlier changelog and Chronicle text retained.');
