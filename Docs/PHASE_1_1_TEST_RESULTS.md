# Phase 1.1 implementation and validation

Validated October 21, 2026 (America/Chicago), Unity 6000.3.27f1, Windows standalone, generator version 3. This is a working environment refinement build. The implementation and automated checks are complete for this pass; full visual acceptance against the supplied references remains open.

## Deliverables and source protection

Player: [Builds/Phase11/Aevoria.exe](../Builds/Phase11/Aevoria.exe). Keep its adjacent Data, DLL and runtime folders together. The previous Phase 1 build and report remain available. No civilization features or old terrain-generator migration were introduced. Aevoria-old was not modified. No commit, reset, staging change or publication was performed. The pre-refinement source snapshot is in ignored Temp/Phase11Baseline.

The existing validation tool copies this project into the full NTFS path under LOCALAPPDATA/Temp/AevoriaPhase1Validation. It does not initialize another project. The previous abbreviated TEMP-path serialization failure is guarded against. Player captures and timings below come from the rendered Windows player, not a hidden Update-only process.

## Implemented behavior

| Area | Result and evidence |
| --- | --- |
| Continental geography | Deterministic warped continental fields, quantile-calibrated 67.5% default land, variable coasts and an ocean perimeter. Nine seed/preset measurements below. |
| World dimensions | Small 16.384 km, Medium 32.768 km, Large 65.536 km square. 512 m chunks; 8 m highest-detail terrain spacing. |
| Relief | Regional mountain masks preserve broad lowlands; ridges, plateaus, foothills, valleys and channel-shaped cuts add localized roughness. Artistic fields, without physical erosion. |
| Materials | Procedural triplanar grass/soil/sand/rock/snow blending, fine surface grain, directional weathering and rock fractures. Distance-faded detail. |
| Water | Depth-colored transparent shallows, deep blue offshore, animated restrained ripples/highlights, subtle shore foam and haze. No fluid simulation or real reflections/refraction. |
| Horizon | World-space decorative outer terrain, extended ocean, sky and altitude-dependent haze. Scenery is separate from authoritative geography; HUD identifies outside-world focus. |
| Camera | Free horizontal travel preserves altitude; terrain following/collision are optional and off by default. Can enter/bypass terrain. Smooth adjustable zoom, 2 m to 32 km, retains orbit/pan/reset/focus controls. |
| Wheel correction | Installed Input System normalizes wheel notches to +/-1. The previous 120-unit assumption suppressed zoom. Default single notch now changes target 900 m to 504 m; legacy Windows range is explicitly handled. |
| LOD/streaming | Projected cell-size LOD, hysteresis, visible/predicted priority, two cancellable workers, pooled tiles, shared border samples, skirts and 0.45 s complementary fades. Coarse 256-cell overview supplies fallback. |
| Atmosphere | Warm directional light, procedural sky, distance/altitude haze and modest exposure/color adjustments. Optional URP camera motion blur, off by default. |
| Vegetation | Deterministic 16 m jittered placement cells produce dense groves, sparse woods and grass tufts. Slope/water/elevation filtering, worker placement, GPU instancing, frustum/distance limits and near/far meshes; no per-tree GameObjects. |
| Existing systems | Simulation state/clock, deterministic RNG, save-envelope validation, regeneration, speed/pause controls and HUD/text/window navigation guards retained. Regeneration ownership tests pass. |

Generation identity includes version, seed, dimensions and settings. Height queries do not depend on chunk creation order or simulation RNG. Sea-level changes classify the same elevation field rather than raising terrain. Full persisted-world version migration remains future work.

## Automated checks and build

| Check | Result | Evidence |
| --- | --- | --- |
| Portable .NET checks | 23 passed | [Raw results](Validation/Phase11/portable-results.txt) |
| Unity EditMode | 18/18 passed | [XML](Validation/Phase11/edit-results.xml) |
| Unity PlayMode | 2/2 passed | [XML](Validation/Phase11/play-results.xml) |
| Windows player | Succeeded, 102,547,465 reported bytes, 74.61 s | [Build summary](Validation/Phase11/build-summary.txt) |

Checks cover deterministic geography, signed/large coordinates, bounds, finite settings, sea-level classification, border agreement including mixed LODs, tree repeatability/suitability, camera altitude independence and underground travel, optional collision, zoom sensitivity/settling, normalized and legacy wheel input, existing simulation/persistence behavior, regeneration and mesh/vegetation resource cleanup. Fast Large-world travel outside authority also verifies fallback ownership and eventual chunk/tree eviction. Automated synthetic input is not physical-device acceptance.

Source Assets/Packages/ProjectSettings were checked against the validated copy. Unity serialized three shader-default properties into WorldTerrain.mat; that material was synchronized back to source. No other source/cache differences remained at verification.

## Land coverage

Independent 129 x 129 cell-center elevation samples at sea level zero, rather than reusing the generator's 97 x 97 calibration samples:

| Seed | Small | Medium | Large |
| --- | ---: | ---: | ---: |
| aevoria-001 | 67.49% | 67.62% | 67.34% |
| aevoria-002 | 67.30% | 67.58% | 67.76% |
| archipelago-37 | 67.32% | 67.45% | 67.71% |

All nine meet the 65-70% default target. A separate configurable-coverage test verifies that 40% and 80% targets alter coast topology. The historical grid diagnostic includes perimeter samples and is not the unbiased area estimate in this table. These finite samples estimate area; they are not exact polygon integration or proof that every possible seed meets the band.

## Standalone benchmark method

2560 x 1440, uncapped VSync-off visible Windows player, Intel i7-8700K / NVIDIA RTX 3060 Ti. Editor/build compilation finished before the accepted run. Main-camera rendering callbacks were required in every interval. The earlier Preview directory was used for visual iteration and is excluded from final results.

The probe waits for initial local view readiness, excludes two seconds of warm-up, then measures 8 s static, 12 s motion, or 16 s rapid-zoom/Large streaming intervals. Motion sampling continues while chunks load. Continuous paths travel at 550 m/s; rapid zoom spans targets 35-15,000 m while traveling 350 m/s; Large streaming travels 1,600 m/s on a bounded 2.5 km-radius loop starting at least 6 km inside the world. Blur off/on use matched paths on the regenerated Large world. Screenshots are requested after each measured interval. Regeneration also has automated PlayMode coverage.

The first fast Large path drifted outside authority; its 547 FPS average is rejected as an active-terrain streaming result. [Audit evidence](Validation/Phase11/PathAudit/README.md) preserves that path. The benchmark-only path was corrected, the player rebuilt, and all 13 scenarios rerun. The table and parent-directory raw data use only that corrected run. Editor/PlayMode results cover the game behavior; the subsequent benchmark-path edit was compiled and exercised in the final player.

The raw records include frame percentiles, rendered frame counts, GPU timing, normalized process CPU, Main Thread marker, stream-update maximum, terrain/vegetation triangle counts, visible trees, worker CPU, initial readiness, allocated memory, managed snapshots, Gen0 collections and last simulation tick duration. Worker time includes placement and accumulates over the world lifetime; it is not wall-clock whole-world generation time. Readiness is the first local ready state after regeneration, not per-camera-stage latency or every detailed chunk in the world. Main Thread includes idle/synchronization; it is not exclusive CPU script work. The field named peakResidentTerrainTriangles tracks current detailed meshes; temporary outgoing fade copies and the separately reported 131,072-triangle overview are excluded. All geometry counts exclude extra shadow/depth passes. Allocated memory excludes dedicated VRAM. These are engine frame-loop measurements, not presented-frame/PresentMon data.

| Scenario | Mean FPS | p95 ms | p99 ms | Max ms | GPU mean ms | Peak chunks | Peak visible trees |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| overview | 427.8 | 3.25 | 4.09 | 7.55 | 1.875 | 169 | 0 |
| mountains | 319.2 | 4.29 | 5.18 | 16.27 | 2.674 | 95 | 14223 |
| grassland | 396.8 | 3.22 | 3.77 | 7.58 | 2.238 | 71 | 10379 |
| coast | 285.6 | 4.81 | 9.21 | 41.70 | 3.083 | 111 | 11653 |
| water | 362.4 | 3.31 | 4.14 | 22.51 | 2.500 | 85 | 11448 |
| horizon | 313.0 | 3.79 | 4.51 | 12.57 | 2.961 | 128 | 12255 |
| forest | 243.0 | 5.06 | 5.97 | 7.22 | 3.864 | 68 | 19972 |
| continuous | 237.2 | 5.31 | 6.21 | 15.64 | 3.913 | 87 | 27977 |
| rapid-zoom | 309.3 | 4.81 | 5.49 | 8.32 | 2.868 | 163 | 37709 |
| large-overview | 500.8 | 2.51 | 3.03 | 5.68 | 1.790 | 169 | 0 |
| large-streaming | 338.7 | 4.60 | 5.48 | 9.02 | 2.512 | 88 | 22925 |
| blur-off | 343.2 | 3.97 | 4.47 | 7.27 | 2.700 | 81 | 13440 |
| blur-on | 331.8 | 4.14 | 4.59 | 6.39 | 2.808 | 81 | 13440 |

All 13 intervals rendered at 2560 x 1440. Mean rates ranged from 237.2 FPS during continuous travel to 500.8 FPS in the Large overview. The corrected active-terrain Large loop averaged 338.7 FPS, with 5.48 ms p99 and 9.02 ms maximum. These short paths exceed the 60 FPS average target; they do not establish perfect frame pacing.

The final coast interval had a 41.70 ms maximum and 9.21 ms p99; water had a 22.51 ms maximum. Those outliers exceed the 16.67 ms budget despite high averages. Their causes are unresolved: renderer Update in those intervals peaked at 1.46/0.93 ms, which does not establish whether the spikes came from GC, engine/driver synchronization, or other system work. No outlier was discarded. Profile event-level traces before claiming stable 60 FPS in every frame.

Across scenarios, normalized process CPU was 18.1-34.0% of the 12 logical processors, and the Main Thread marker averaged 2.00-4.21 ms including wait time. Renderer Update peaked at 3.34 ms in rapid zoom. Unity allocated-memory peaks were approximately 120-149 MiB, excluding VRAM; managed snapshots and raw byte counts are retained. Gen0 counts were 46-336 per interval, including 297 during rapid zoom and 336 during Large streaming. This allocation churn is an explicit optimization target despite the measured frame rates. First locally ready states after regeneration were 2.585 s for Medium and 1.477 s for Large; the coarse initial workloads differ, so this is not evidence that Large generates faster overall.

Motion blur changed matched-path mean rate from 343.2 to 331.8 FPS (about 3.3% lower); GPU mean increased from 2.700 to 2.808 ms in this single sequential comparison. It is optional and off by default. Peak visible trees reached 37,709 during rapid zoom. Last simulation ticks were 0-0.0002 ms at timer precision for the existing tiny aggregate model; that is not a forecast of future simulation cost. Full [raw records](Validation/Phase11/metrics.json), per-scenario text files, [player log](Validation/Phase11/player.log) and screenshots accompany this report.

## Captures and comparison

These seven final player images were visually inspected. Additional moving/Large/blur captures are retained with the raw measurements.

| View | Capture | Observation |
| --- | --- | --- |
| Continental overview | [Overview](Validation/Phase11/00-overview.png) | Broad connected land, localized relief and coastal inlets; distant overview silhouettes remain coarse. |
| Mountains | [Mountains](Validation/Phase11/01-mountains.png) | Rocky ridges/cliffs and green valleys; fracturing is procedural and repeated bands remain recognizable. |
| Grassland | [Grassland](Validation/Phase11/02-grassland.png) | Broad quiet lowlands, surface grain, sparse trees and distant groves; near ground lacks reference-quality plant variety. |
| Coastline | [Coast](Validation/Phase11/03-coast.png) | Curved beach, dense woodland patches and turquoise-to-blue water. Beach width is relatively uniform. |
| Water | [Shallows](Validation/Phase11/04-water.png) | Underwater ground and depth transition visible; low-angle shoreline stair stepping and distant thin bright artifacts need refinement. |
| Horizon | [Horizon](Validation/Phase11/05-horizon.png) | Atmospheric distance layers and scenery beyond local chunks; coarse silhouettes and a flat ocean/sky transition remain evident. |
| Forest | [Forest](Validation/Phase11/06-forest.png) | Dense instanced crowns/trunks; repeated faceted canopies are clearly provisional. |

Compared with the [Phase 1 captures](PHASE_1_TEST_RESULTS.md), this pass adds actual forest coverage, more detailed procedural rock/ground shading, transparent depth-colored water, larger continental land distribution and persistent fallback scenery. Phase 1 FPS figures used different static workloads and cannot be treated as a controlled before/after speedup.

The three user-supplied references are preserved for direct review:

- [Valley reference](Validation/Phase11/References/01-valley.png): long mountain walls, dense woods, green valley and mist guided range/forest composition. This build has those ingredients, but less natural geology, canopy structure and fine vegetation. Settlements are outside this phase.
- [Coast reference](Validation/Phase11/References/02-coast.png): pale fractured rock, clustered forest, broad grassland and translucent shallows guided surfaces and placement. The build improves in that direction but does not match the reference's rock geometry, layered foliage, shore clutter, lighting or material fidelity. Waterfalls/rivers are not implemented.
- [Continental reference](Validation/Phase11/References/03-continent.png): localized ranges next to expansive plains and natural inlets guided scale/distribution. Drainage networks, river mouths and erosion detail remain absent. No claim of photorealistic or reference-image parity is made.

## Remaining acceptance gates and next work

1. Physical mouse/keyboard, focus loss/alt-tab, text entry and HUD interaction in Editor and player require hands-on acceptance. Synthetic tests cover behavior but do not establish device ergonomics.
2. Review continuous motion visually, especially extreme zoom, mixed-LOD silhouettes and vegetation near/far transitions. Crossfades/skirt tests and timed paths do not prove absence of visible popping everywhere. No geometric morphing or edge stitching is implemented; overview/local differences can remain visible.
3. Improve distant overview resolution/adaptation, coastline mesh detail, rock shape variety, beach-width variation, tree/grass assets, foliage shading and water-edge artifacts. No missing reference fidelity is hidden by the performance results. Horizon implementation is functional; cinematic visual acceptance remains partial.
4. Profile managed allocations/Gen0 activity, regeneration latency, VRAM and long sessions. The bounded working set and cleanup checks are evidence of ownership, not a long-soak memory guarantee. The tiny current aggregate simulation is not a civilization performance workload.
5. Prefer the environment polish and physical-control/LOD review above before civilization strategy work. Physical erosion, hydrology, ecology, scanned materials, volumetric clouds and full world persistence remain outside this implementation pass.

Against the brief's definition of done, land coverage, deterministic generation, free-camera behavior, faster zoom, basic forests, successful tests/build and reported benchmarks have direct evidence. Materials/water/horizon/relief are implemented and improved but remain short of the reference realism target. Smooth LOD and absence of major popping require further visual acceptance. Phase 1.1 is therefore delivered as a validated implementation pass with these open gates, not unconditional completion of all 23 criteria.

## Files created or changed in this pass

| Area | Files |
| --- | --- |
| Pure geography | Assets/Scripts/World/Geography.cs, ProceduralGeography.cs; new VegetationDistribution.cs |
| Camera/presentation | MapCamera.cs, WorldRenderer.cs, AevoriaBootstrap.cs, Aevoria.Presentation.asmdef; new WorldVegetation.cs, WorldAtmosphere.cs, RefinementBenchmark.cs |
| HUD | Assets/Scripts/UI/DeveloperHud.cs |
| Rendering assets | WorldTerrain.shader/material; new WorldWater.shader, WorldVegetation.shader/material, Resources/WorldEffects.asset; WorldWater.mat |
| Validation | Assets/Editor/Phase1Validation.cs, Editor/TerrainTests.cs; new Editor/RefinementTests.cs; PlayMode/RegenerationTests.cs; Tests/PortableChecks/Program.cs |
| Documentation | TERRAIN_DESIGN, CAMERA_CONTROLS, ARCHITECTURE, PERFORMANCE_STRATEGY, MIGRATION_STATUS, ROADMAP, DEVELOPMENT_WORKFLOW, this report and Validation/Phase11 evidence |

New Unity assets include metadata. Existing Phase 1 validation/path fixes and diagnostics are retained. Materials and five vegetation meshes are reused; old terrain meshes, textures, tasks and runtime-owned volume components have explicit cleanup paths. No external asset packs were downloaded.


