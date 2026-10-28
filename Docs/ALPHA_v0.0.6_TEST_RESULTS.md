# Alpha v0.0.6 — The Living World: validation record

October 22, 2026. **Development build, not a release declaration.** Windows player: [Aevoria.exe](../Builds/Alpha-v0.0.6/Aevoria.exe), with its adjacent runtime folders. [Implementation and exact bounds](LIVING_WORLD.md). [Physical/manual acceptance](ALPHA_v0.0.6_MANUAL_ACCEPTANCE.md).

**Final rendered audit passed:** 56776 sampled main-camera-rendered frames at 2560 × 1440, 41 captures, all native continuity/cleanup/walk/jump/crouch/dig gates true. There were 41 frames above 16.6667 ms; the longest was 92.1544 ms during experimental excavation installation/travel. This hitch remains an explicit performance limitation.

## Delivered and tested

| Area | Evidence and limits |
|---|---|
| Shared terrain boundaries | All nine LOD pairs across all five world sizes share complete edge positions and normals. Upward triangle winding and complete chunk area are checked. Opaque skirts are removed. The same overview triangle field joins the near perimeter; this does not establish perfect visual acceptance everywhere. |
| Inland water | Connected refined basins use one level, reject leaks/tiny unsupported puddles and apply a supply/evaporation proxy. The test seed accepts 16 and rejects 18 candidates. River triangle centroids do not occupy lake-owned surface. Lake and river construction share shoreline ownership. This is a surface refinement over generator-4 drainage, not hydraulic conservation. |
| Snow and ice | Seven-day deterministic saved accumulation/melt, cadence-independent continuation, gradual warm thaw and validation of seasonal arrays pass. Permanent mountain snow remains separate. Ice and crown/prop snow are presentation driven by these saved quantities. |
| Resources and local solids | Stable sparse depletion, tool restrictions, time-based standing-tree recovery, funded placement and archive round trips pass. Excavation retains real solid below an opening and can represent a roof above a cavity. Quarter-metre reconstruction passes finite/index checks and paired, oppositely oriented interior-edge checks at the rim. |
| Exploration | Real CharacterController terrain contact and jumping, crouch state, first/third-person state, exit/return pose and map input blocking pass in PlayMode. The rendered harness drives the same movement method used by live input. This verifies the physical capsule without claiming physical keyboard/mouse acceptance. |
| Persistence | Schema 4 captures living state. Explicit schema-3 upgrade creates a new ID, preserves original archive bytes and all ecology, and verifies the original geography fingerprint. Existing ecology and random continuation remain covered. No user archive was used by automated tests. |
| Scale | Original presets remain 16.384/32.768/65.536 km; new presets are exactly 128/512 km. Global fields and presentation remain bounded. Colossal uses coarse global drainage/climate and local streaming; detailed hierarchical local hydrology is deferred. |
| UI | Six actual clock speeds, independent movement settings, upward god-camera pitch, recenter, full map, minimap preferences, debounced creation preview, copied migration UI and F11 display controller are implemented. Native captures exercise presentation; physical input and monitor behavior remain manual gates. |

## Automated results

- **111 portable checks passed:** [complete output](Validation/Alpha-v0.0.6/portable-final.txt). Includes the retained 90, 21 living-world checks, and both ordinary seeds at Small/Medium/Large through 1,000 ecological years. The added rim test specifically checks that interior mesh edges occur twice with opposing directions.
- **54 EditMode tests passed:** [XML](Validation/Alpha-v0.0.6/Automated/edit-results.xml), [log](Validation/Alpha-v0.0.6/Automated/edit.log). The original camera regression now expects the deliberately expanded pitch interval, −90° to 89.7°; it was not removed to suppress a failure.
- **7 PlayMode tests passed:** [XML](Validation/Alpha-v0.0.6/Automated/play-results.xml), [log](Validation/Alpha-v0.0.6/Automated/play.log). Includes retained regeneration/late-job/cleanup tests and the new real terrain-contact/jump/view/overlay scenario.
- **Windows x64 build succeeded:** [build log](Validation/Alpha-v0.0.6/Automated/build.log), [build metadata](../Builds/Alpha-v0.0.6/build-info.json). Unity 6000.3.27f1, URP 17.3.0; application 0.0.6, generator 4, archive 4, ecology 1.

The final player uses the expanded local Unity validation cache, not an abbreviated TEMP alias. Logs are checked for shader/runtime exceptions and TypeDB assembly mismatch. Test archives live under isolated temporary or explicitly named `Temp/Alpha006…` roots.

## Findings, fixes and retained failures

The reported straight boundaries had concrete implementation causes: per-grid-cell lake level selection, river strips continuing through lake-owned water, opaque vertical terrain skirts, different overview/detail normal stencils, and an ocean depth switch at a hard camera distance. Terrain LODs now share a complete fine boundary and normal stencil; forward/depth/shadow passes share the relevant displacement and masks. Near-field coverage updates change dirty pixels rather than walking the full coverage array every frame.

A separate forest defect measured LOD distance from a 4,800 m-high culling enclosure. It selected distant tree silhouettes close to the ground. LOD selection now uses actual stand height; detailed conifers contain branches and crown clusters. Terrain tile priority also measures actual surface height.

The first new water regression failed on **22 river triangle centroids** within lakes. The failure is retained in [initial test results](Validation/Alpha-v0.0.6/Initial/edit-results.xml); adaptive shoreline clipping removed those overlaps. The initial rendered audit then stopped because its cold-world forest filter had no matching habitat. Its logs, partial frame traces and captures remain in [Rendered-initial](Validation/Alpha-v0.0.6/Rendered-initial/). Its digging search was also too local to find a supported low-relief patch; the revised probe searches the tested supported area.

A completed pre-refinement run is preserved in [Rendered-final](Validation/Alpha-v0.0.6/Rendered-final/). It passed native continuity/cleanup/walk/jump/crouch/dig gates, but inspection found a ragged dig rim and a distracting pixel pattern on snowy ice. Tetrahedron winding now uses the interpolated solid-to-air direction, degenerate triangles are omitted, reconstruction uses 0.25 m cells and the rim topology is tested. Ice uses continuous broad snow drifts with filtered cracks. These changes are covered by the final rendered run below; earlier evidence has not been replaced.

[Original defect images](Validation/Alpha-v0.0.6/References/README.txt) and [concept references](Validation/Phase11/References/) remain unchanged. Original defect seeds/camera coordinates were unavailable, so this is fault-class reproduction rather than a claim of identical scene reconstruction.

## Benchmark method

Native, visible Windows rendering at actual 2560 × 1440 on the i7-8700K / RTX 3060 Ti / 64 GB target. Each final measurement disables VSync and the frame cap; two warm frames follow instrumentation setup. Camera render callbacks count actual main-camera renders. Engine-loop duration is not externally measured display presentation latency or a universal FPS guarantee.

Raw CSVs retain every sampled frame and all outliers. A preallocated trace records streaming CPU scope, available GC markers, collection deltas, HUD/vegetation/upload scopes and mesh counts. Unsupported allocation or wait counters are **−1**, not zero. FrameTimingManager supplies available GPU timings; process CPU time is summed across threads and divided by frames, not a main-thread critical-path measurement. Correlated markers can lag and do not establish cause.

Screenshots and CSV writes are normally outside measurement windows. The excavation installation/travel window deliberately includes streaming and local mesh/collider installation. The final moving sample lasts 180 seconds. The whole run also repeats Small save/load and verifies Huge/Colossal reloads. A lightweight host sampler records process RAM/private memory and Windows per-process dedicated/shared GPU counters approximately every two seconds; these are sampled maxima, not instantaneous physical VRAM residency peaks.

Use [AnalyzeAlpha006.ps1](../Tools/AnalyzeAlpha006.ps1) with the run directory to regenerate summaries and the unfiltered >16.6667 ms outlier list. The final run directory is [Rendered-release](Validation/Alpha-v0.0.6/Rendered-release/); that name identifies the final packaged development binary and does not declare a public release.

## Remaining acceptance and deferred work

**Implemented, requiring manual acceptance:** visual seam-free travel at arbitrary locations/seasons and every camera transition; combined physical input, focus/UI/window gating; F11/multi-monitor/Alt-Tab/15-second Keep/Revert; map usability and live setting changes; long-session memory stability; subjective landscape/forest/ice quality. [Manual checklist](ALPHA_v0.0.6_MANUAL_ACCEPTANCE.md).

**Experimental:** the 1.8 m stick traveler, starter tools/actions, local collision and excavation, resource gathering, inventory and test crates. Excavation permits 16 × 32 m patches, 32 cuts per patch, ten metres below original surface, dry modest-relief ground and a guarded border. Unsupported cross-patch editing is refused. No unrestricted cave world is claimed. Terrain and crates have colliders; natural-object collision remains incomplete. Tool debris is bounded; production character/tool animation is unfinished.

**Deferred/limited:** production art and concept-image fidelity; visibly repetitive procedural tree crowns and coarse close-ground detail; complete hydraulic balance, backwater, erosion and physical sediment bars; detailed waterfalls; local hierarchical climate/drainage on 512 km worlds; unrestricted underground traversal; civilizations, politics, warfare and simulated social history. Lake refinement can reject a visual lake while legacy ecological habitat still uses the old coarse water-proximity field. Largest-world overview/detail transitions still require visual acceptance. The map shows climate/biome snow rather than live seasonal snowpack. A multi-minute scripted run does not replace an hour-long physical soak or externally measured frame presentation.

## Preservation

Earlier Chronicle bytes were checked against `Temp/Alpha006Baseline/THE_CHRONICLES_OF_AEVORIA.md` and remain identical as the prefix of the updated document. Book VI is appended; changelog retains its established style and prior entries. Git staging remains untouched; index SHA-256 is `00024D9632812793FAFCE6418D7392644E0ABEB315014E843AE1A0740F416583`. The repository has no commits, so none is invented. Aevoria-old and historical builds/evidence remain unchanged. Normal user saves were not opened or overwritten by validation.

## Final rendered measurements

[Audit JSON](Validation/Alpha-v0.0.6/Rendered-release/audit.json), [summary CSV](Validation/Alpha-v0.0.6/Rendered-release/summary.csv), [all outliers](Validation/Alpha-v0.0.6/Rendered-release/outliers.csv), [player log](Validation/Alpha-v0.0.6/Rendered-release/player.log), [process memory](Validation/Alpha-v0.0.6/Rendered-release/process-memory.csv).

| Scenario | Frames | Mean ms | p99 ms | Max ms | CPU ms/frame* | GPU ms | >16.67 ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| overview | 1379 | 3.63 | 5.33 | 15.94 | 7.78 | 3.20 | 0 |
| forest | 695 | 7.20 | 9.56 | 19.13 | 15.13 | 6.95 | 2 |
| water | 1180 | 4.24 | 6.18 | 13.12 | 8.85 | 3.73 | 0 |
| exploration | 1578 | 3.17 | 5.17 | 19.37 | 9.60 | 2.36 | 1 |
| excavation-install-and-travel | 1042 | 3.84 | 5.54 | 92.15 | 10.42 | 3.40 | 1 |
| moving-soak | 42422 | 4.24 | 6.36 | 22.57 | 14.45 | 3.74 | 28 |
| accelerated | 2827 | 4.25 | 6.18 | 21.52 | 14.08 | 3.74 | 1 |
| Huge-overview | 1476 | 4.07 | 5.65 | 15.78 | 9.37 | 3.66 | 0 |
| Huge-local | 1309 | 6.12 | 16.41 | 20.46 | 19.90 | 5.67 | 7 |
| Colossal-overview | 1402 | 4.28 | 6.20 | 14.62 | 9.93 | 3.70 | 0 |
| Colossal-local | 1466 | 5.46 | 10.90 | 19.25 | 11.17 | 4.86 | 1 |

*Aggregate process CPU across threads, not main-thread frame duration. Main-camera render counts equal sample counts in every window. The 180-second moving window contains 42,422 frames. Small is reloaded three times; Huge and Colossal each save/reload successfully. Initial creation took 8.46 seconds for Huge and 9.31 seconds for Colossal in this run.

Sampled process maxima: working set **1,747.922 MiB**, private memory **2,340.902 MiB**, dedicated GPU counter **600.859 MiB**, shared GPU counter **330.258 MiB**. These include preparation, multiple worlds and unloading; they are not isolated scene peaks or proof of indefinite memory stability.

The largest frame is excavation-install-and-travel frame 242, **92.1544 ms**. Its associated GC marker is 1.1882 ms, collection delta 0, terrain streaming scope 0.3596 ms and terrain upload scope 0. The local solid mesh/collider install is not isolated by a dedicated scope, so collider cooking is a plausible contributor, not a proven complete explanation. Experimental excavation is not performance-stable. The longest ordinary moving frame is 22.5692 ms; it has no coincident GC marker or collection delta in this trace. The previous v0.0.5 accelerated 55.6533 ms outlier did not recur here; its root cause remains unresolved. No single GC, terrain or driver explanation is asserted.

Inspected final captures include the menu/Book VI, forest, lake ownership/shore, experimental traveler and excavation, frozen lake, map/minimap, seam movement sequence and largest-world local view. The dig rim is smoother and the ice pixel grid is removed, while close-ground and tree assets remain visibly procedural. Broad ice drifts/cracks, coarse shoreline facets and largest-world transitions still need visual acceptance. See [all captures](Validation/Alpha-v0.0.6/Rendered-release/).
