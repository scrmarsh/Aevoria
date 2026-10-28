import fs from 'node:fs';
import path from 'node:path';
const root=path.resolve(import.meta.dirname,'..');
function append(file,marker,text){const p=path.join(root,file),before=fs.readFileSync(p);if(before.toString('utf8').includes(marker))return;fs.writeFileSync(p,Buffer.concat([before,Buffer.from('\n\n'+text.trim()+'\n','utf8')]));}
append('CHANGELOG.md','### Continued restoration — playtesting expansion',`
### Continued restoration — playtesting expansion

- **Status:** Additional v0.0.8 recovery in development following Sean's October 23 playtesting. This is the same milestone. The [expanded implementation record](Docs/ALPHA_v0.0.8_ADDENDUM.md) separates completed checks from pending visual and performance acceptance.
- **Added:** Independent cloud and weather display controls; graphics presets that adjust real rendering budgets; cancellable visible-area preload with installed-resource progress; versioned, checksummed disposable terrain caching; ground-view mountain scenery; configurable double-Space exploration flight and a separate minimum God-mode movement speed.
- **Changed:** Generator 7 and hydrology 5 identify the triangular gradient terrain basis, distributed thermal transport, removal of artificial continent-bisector sea cuts and bounded lowland meander. The relief control remains 16,000 m. Earlier worlds and matching historical builds are preserved.
- **Fixed:** Supported high-altitude and Colossal-edge traveler positions rejected by old fixed bounds; generic save diagnostics; optional camera safety; near clipping based on orbit distance beside slopes; and nested procedural material derivatives that varied between terrain layers. Additional checks cover installed fallback surfaces during loading.
- **Improved:** Smooth exponential zoom targets, responsive ground-relative panning, pitched-viewport regional planning, background streaming controls and cache reuse. Large cumulus volumes replace the low speckled sheet; their hierarchy and shadows share stable world-space formation data. The underlying weather, ecology, wetness, resources and inventory retain their own authority.
- **Testing and limitations:** Existing evidence remains intact. Intermediate rendering failures and changed-identity comparisons are disclosed. Final native cloud/terrain appearance, flight and preload behavior, all-size frame pacing and the updated Windows build still require recorded verification. Source implementation is not a declaration of visual completion.
`);
append('Docs/ROADMAP.md','## Alpha v0.0.8 playtesting expansion',`
## Alpha v0.0.8 playtesting expansion

The current recovery expands under the same application version. Generator 7/geology 3/hydrology 5 supersede the initial generator-6 delivery for new worlds; archive 5 and climate 3 remain. [Expanded implementation and validation status](ALPHA_v0.0.8_ADDENDUM.md) records critical save/camera fixes, physical and material grid diagnosis, streaming/preload/cache work, flight, cloud hierarchy and horizon scenery. Close every remaining native visual, physical-input and performance gate before acceptance; no civilization scope advances. Previous prose and evidence remain historical records of their corresponding builds.
`);
append('Docs/WORLD_RESTORATION.md','## October 23 playtesting expansion',`
## October 23 playtesting expansion

The architecture above describes the initial generator-6 delivery. The [current expansion](ALPHA_v0.0.8_ADDENDUM.md) supersedes its Cartesian value-noise basis, axial thermal transport, fixed two-worker policy, derivative-based material filtering, optional God-mode safety and cloud sheet. Current source uses generator 7/geology 3/hydrology 5. Its tagged regional preload, checksummed disposable cache, one-to-four background workers, actual clearance checks, flight and bounded cloud-volume hierarchy are described with their limits in that record. Native acceptance is still pending; earlier passed evidence is not silently reused as proof of changed behavior.
`);
for(const [source,destination]of [['CHANGELOG.md','Assets/Resources/History/Changelog.txt'],['Docs/THE_CHRONICLES_OF_AEVORIA.md','Assets/Resources/History/Chronicles.txt']])fs.copyFileSync(path.join(root,source),path.join(root,destination));
console.log('Appended the v0.0.8 playtesting record without rewriting prior milestone prose.');
