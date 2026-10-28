import fs from 'node:fs';
function appendOnce(file,heading,body,baseline){
  const old=fs.readFileSync(baseline),current=fs.readFileSync(file);
  if(!current.subarray(0,old.length).equals(old))throw new Error('Historical prefix changed: '+file);
  if(!current.toString('utf8').includes(heading))fs.appendFileSync(file,'\r\n'+body.replace(/\r?\n/g,'\r\n'));
  if(!fs.readFileSync(file).subarray(0,old.length).equals(old))throw new Error('Append damaged history');
}
appendOnce('CHANGELOG.md','## Alpha v0.0.8 - The Restoration of the World',`
## Alpha v0.0.8 - The Restoration of the World

- **Status:** Development recovery update; implementation, executed validation and final visual acceptance remain separate. No public release declared.
- **Validation date:** October 22, 2026. The [current report](Docs/ALPHA_v0.0.8_TEST_RESULTS.md) records actual evidence, rejected iterations and open gates; the [pre-correction audit](Docs/ALPHA_v0.0.8_REGRESSION_ANALYSIS.md) records the regression mechanisms.
- **Added:** Shape-preserving continuous geological interpolation; view-dependent regional sampling; projected-viewport local tile discovery; bounded shared shoreline refinement; geometry-based water transparency quality; independent LOD transition quality; desert scrub and dry-grass presentation; Book VIII, The Restoration of the World.
- **Changed:** Geography generator 6, geology 2, hydrology 4, climate 3 and inland surface layout 2 identify the deliberate physical terrain and water-profile repairs. Archive schema remains 5; ecology, forecast and saved living/seasonal/atmospheric identities are retained. Existing worlds and the v0.0.6/v0.0.7 players remain intact and require their matching generator. The five exact world dimensions and theoretical 16,000 m mountain-relief ceiling continue.
- **Improved:** Footprint-filtered world-space materials; climatic grass and desert palettes; elevation-aware temperature and exposed-rock snow; local and regional triangle-scale normals, common mixed-LOD border normals and proportional terrain joins; freshwater and ocean depth ranges; visible submerged terrain; soft individual forecast-driven cloud formations with shared moving shadows; vegetation bounds derived from actual instances and useful overhead visibility.
- **Fixed:** Coarse ecology moisture no longer paints a terrain grid. Climate moisture exchanges between adjacent wind lanes rather than leaving independent horizontal coastal streaks; prevailing direction blends through latitude transitions. River water profiles consult actual depressions and carry spill levels downstream; local beds depend on width, while broader drainage corridors grade into valleys. Bounded smooth centrelines and monotone water-level interpolation replace corner fans in broad carved valleys. Carved dry banks no longer count as river water and suppress riparian plants. Lake geometry and ownership share the refined boundary field. Exterior ground is submerged relative to the configured sea level, with consistent clipping across terrain passes. Natural details are selected outward from the observer instead of taking the first rows of a square.
- **Performance:** Regional meshes, tile counts, shoreline samples, natural details and worker ownership remain bounded. Excavation vertices are indexed on the worker before native mesh and collider installation; its upload now participates in measured work counters. A bounded worker job pre-bakes collision before atomic mesh/mask publication, keeping the old patch active until replacement readiness. Actual native frame traces, outliers, memory and remaining installation costs belong to the current report; lower vertex counts alone do not prove hitch removal.
- **Testing:** Earlier checks are retained, with explicit new geography identity and added interpolation, shoreline ownership/budget, actual channel profile, downhill continuity, regional normals/cancellation, exterior seabed and graphics-range tests. Historical captures and unsuccessful recovery iterations remain available. Automated consistency does not replace visual review or physical-input acceptance.
- **Known issues and limitations:** Bounded planar geology, climate and drainage remain approximations. Procedural vegetation and layered cloud art remain development assets; no full hydraulic erosion, volumetric meteorology, marine food web or unrestricted underground world is introduced. Final reference fidelity, physical controls, presented-frame behavior and Sean's subjective visual approval remain explicit acceptance gates.
- **Git references:** None available; repository has no commits. Existing staging and historical build folders are preserved.
`, 'Temp/Alpha008Baseline/CHANGELOG.md');

appendOnce('Docs/THE_CHRONICLES_OF_AEVORIA.md','## CHAPTER VIII - THE RESTORATION OF THE WORLD',`
## CHAPTER VIII - THE RESTORATION OF THE WORLD

*Alpha v0.0.8 — Development, with open acceptance*

They returned to a country already named.

Its mountains belonged to ranges. Its winds remembered the sea. Water possessed receivers, and the smallest visible stand of trees belonged to an account that continued beyond the observer's sight. These relationships had taken work to establish. Yet the country, when seen in the light, had become less convincing than the one it followed.

They set the older drawings beside the new.

The comparison offered no comfort to a ledger proud of its additions. A forest could have more reasons to exist and still be harder to see. A river could reach its appointed outlet and still resemble a wound. A greater distance could be supported by the instrument while the ground beneath it dissolved into squares.

**An explanation does not excuse an appearance. It must eventually survive one.**

They began with the joins.

Between one sampled height and the next, the ground had changed its inclination too abruptly. They gave those meetings a shared tangent. The broad arrangement remained: lands divided by water, belts rising from the old pressures beneath them. The correction concerned the passage between places, the small continuity by which a landscape persuades an eye that it has not been assembled from separate promises.

The observer's attention was allowed to occupy more than the centre of the drawing. Detail sought the visible country. Nearer ground received a finer account; farther ground kept a bounded one. The cost of that choice remained measurable. They did not call an expensive drawing smooth merely because its still image was pleasing.

Then They followed a river below its appointed surface.

The line above it had been drawn between distant measurements. Between those measurements lay depressions the line had failed to consult. Water could seem to travel through the air; elsewhere, a narrow cut inherited the depth of a much larger descent. They made the line answer to those intervening lows and carry their consequences downstream. Banks were given room to rise into valleys. The smallest bed was no longer asked to contain the entire error of a coarse map.

Dry ground beside the river was returned to the living account. It had been excluded because it bore the mark of carving, as though every altered bank must still be water. A tree could now stand near a channel without being mistaken for something submerged.

At the lakes, the boundary and its description were brought closer together. One surface kept one level. The finer edge belonged both to the visible water and to the query that decided where that water existed. At the sea, color ceased its hurried descent toward black. The shallow bed remained geometry, seen through the surface rather than replaced by a pale assurance that something lay below.

They attended to the quieter absences as well. Scrub and dry stems found their bounded places among the desert's open spaces. Forests were no longer judged through a box too low to contain their heights. Snow consulted the slope and the temperature of the ground it covered; exposed stone was permitted to interrupt the white.

Above these corrections, cloud broke into softer formations. Its shape and shadow consulted the same weather, and its movement the same passing days. It remained an approximation. They left that word in the account.

The earlier books were not rewritten to make the return appear inevitable. Earlier worlds kept their instruments. New ground received a new identity. Even a successful repair was required to say what it had changed.

No people were summoned to distract from the unfinished land. No history inside a world was borrowed to complete the history of its making.

They left the drawings where another observer could compare them. Some judgments would belong to measurements. Some would belong to the person who had first asked this country to exist. Restoration had begun with looking again; it would not be completed by refusing another look.

*Implemented ledger:* continuous geological sampling; revised mountain/valley and channel profiles; smooth river centreline and water-level sampling; adaptive regional rendering and viewport selection; filtered materials and restored palettes; shared shoreline refinement; depth-sensitive transparent water; corrected vegetation visibility and dry-bank eligibility; bounded desert detail; forecast-driven cloud formations; crosswind moisture exchange; indexed excavation presentation with worker-prepared collision; independent transparency and transition controls. [Recovery architecture](WORLD_RESTORATION.md). [Executed validation and remaining gates](ALPHA_v0.0.8_TEST_RESULTS.md).

*Open acceptance and prophecy:* reference-level art, remaining shoreline/channel/LOD imperfections, physical-input and presented-frame acceptance, full erosion and fluid transport, volumetric weather, marine populations, unrestricted underground traversal and all civilization systems remain unfinished. The earlier books and their uncertainties are preserved.
`, 'Temp/Alpha008Baseline/THE_CHRONICLES_OF_AEVORIA.md');

const manifestPath='Docs/Chronicles/chapters.json',manifest=JSON.parse(fs.readFileSync(manifestPath,'utf8'));
if(!manifest.chapters.some(c=>c.version==='0.0.8'))manifest.chapters.push({id:'restoration-world',version:'0.0.8',stage:'Alpha',title:'The Restoration of the World',status:'development-with-open-acceptance',releaseDate:null,anchor:'chapter-viii---the-restoration-of-the-world',theme:{meadow:'#729154',water:'#468C9C',stone:'#9C9D91'},evidence:['../ALPHA_v0.0.8_TEST_RESULTS.md','../WORLD_RESTORATION.md']});
fs.writeFileSync(manifestPath,JSON.stringify(manifest,null,2)+'\n');
console.log('PASS: prior Changelog and Chronicle bytes preserved; Book VIII appended');
