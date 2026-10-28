# Aevoria Alpha v0.0.3 - validation and stability record

Recorded October 22, 2026, America/Chicago. **Development target; no release or unconditional stability declaration.** This report distinguishes implemented changes, measured results and open acceptance. Generator identity remains 3, Unity remains 6000.3.27f1, URP remains 17.3.0.

## Audit and preservation

The active M:\Aevoria Unity project, staged/unstaged work, latest Phase 1.1 report, architecture, shaders, materials, tests, benchmark and roadmap were inspected before edits. Git has an unborn master branch with no commits; no commit IDs or release dates were invented. No commit/reset/staging command was used. The original index hash was retained for final verification. Pre-update source/docs/tests/tooling were copied to ignored Temp/Alpha003Baseline. Builds/Phase1, Builds/Phase11 and their evidence remain historical. Aevoria-old was not modified or used as a terrain source.

Existing systems already supplied v3 geography, calibrated continents, free camera, transparent shallows, instancing, projected LOD and asynchronous ownership. This update refines them; it does not replace simulation architecture, add packages or migrate the old generator. Climate, rivers, hydrology, erosion simulation, ecosystems, peoples and simulated-world history are not begun.

## Changes in this iteration

| Area | Implemented change | Practical limit |
| --- | --- | --- |
| Application identity | Aevoria.Core/ApplicationVersion supplies number, stage, development status and HUD display name. Builder mirrors bundle version and emits build-info.json. Editor regression checks consistency. | Developer controls release/milestone decisions. Generator identity is separate. |
| Distant terrain | Overview increases from 256 to 768 cells per side, with 1,179,648 triangles. Outer decorative relief gains smaller ridges. High-altitude haze is clearer. | More overview geometry costs GPU time/memory. Authority remains finite and unchanged; distant silhouettes remain approximations. |
| Rock and ground | Irregular bedding replaces periodic horizontal strata; triplanar grain is filtered by footprint/distance. Grass/soil transitions and fine color vary; instanced outcrops add local rocky detail. | Height algorithm remains unchanged. Mountain-wall shapes and procedural repetition are not fully solved. Outcrops are decorative, with slope alignment and no collider authority. |
| Shore/water | Beach shading width varies by position/slope; steep shore surfaces expose rock. Relative eye depth, transparent soft edges, shorter-range foam and FXAA reduce edge artifacts. | Detailed terrain is still 8 m spacing. No hydrology, real wave geometry, refraction or planar reflections. |
| Vegetation | Six regional tree variants with independent height/width/yaw, retained near/far crown centres, two ground-cover shapes, smooth forest-density ramps and dithered LOD/distance transitions. | Procedural faceted assets remain below reference foliage quality. Shadow/dither/distance transitions need hands-on moving review. |
| Streaming | One native mesh/placement upload per frame; do not interrupt an active LOD fade. Reuse vegetation on LOD-only work, shared immutable index topology, no unused color arrays or triangle-count copies. Outgoing meshes do not cast duplicate shadows. Deeper settings-scaled skirts cover overview/local relief disagreement. | Crossfades/skirts remain, without geometric morphing or stitched edges. Resource tests do not establish seamless appearance everywhere. |
| HUD | Fixed rectangles replace GUILayout; strings refresh at 4 Hz; arrays/labels are reused. Existing buttons, seed guard, panel bounds and camera options remain. | IMGUI and other runtime systems can still allocate. No zero-allocation claim is made. |
| Development record | CHANGELOG, original Chronicles, chapter metadata, public roadmap/version policy and manual checklist. Earlier labels are retrospective; Chapter IV is a planned prophecy. | Metadata is for a future reader; no animated Chronicle UI or simulated History Book ships. |

The authoritatively generated land remains exactly the v3 field used previously: presets are 16.384/32.768/65.536 km square. The nine seed/preset coverage checks still target 65-70%. The current portable results retain the exact independent coverage readings; no new tectonics, climate or erosion is claimed.

## Verification

| Check | Verified result | Evidence |
| --- | --- | --- |
| Portable .NET | 24 checks passed | [Raw output](Validation/Alpha-v0.0.3/portable-results.txt) |
| Unity EditMode | 24/24 passed | [XML](Validation/Alpha-v0.0.3/edit-results.xml) |
| Unity PlayMode | 4/4 passed | [XML](Validation/Alpha-v0.0.3/play-results.xml) |
| Windows x64 | Succeeded; 102,567,263 reported bytes; 74.87 s | [Build summary](Validation/Alpha-v0.0.3/build-summary.txt) |
| Version metadata | Application/build target 0.0.3, Alpha, Development; generator 3 | [Generated metadata](Validation/Alpha-v0.0.3/build-info.json) |
| Chronicle manifest | Four unique chapters; anchors, themes, evidence paths and current version association passed | [Validator output](Validation/Alpha-v0.0.3/chronicles-validation.txt) |

Final validation logs contained no UnityException, NullReferenceException, MissingReferenceException, shader compilation error or TypeDB assembly lookup failure. Source Assets/Packages/ProjectSettings hashes match the validated copy. The saved Git index hash and hashes of the authoritative geography, camera controller and historical Phase 1.1 report are unchanged. No Unity asset metadata is missing. Final [preservation checks](Validation/Alpha-v0.0.3/preservation-checks.json) retain the source/cache and index results.

The original simulation, clock/RNG, save-envelope, sea-level, deterministic height, camera input and mesh-border tests remain. New checks cover application/build version agreement, deterministic bounded tree variants, valid finite near/far meshes, cancellation, retained LOD placement and rapid LOD resource bounds. Regeneration checks retain the same clock and verify that all 15 shared vegetation/outcrop meshes are reused rather than accumulated.

Intermediate log review found an Editor bootstrap UnityException from creating a MaterialPropertyBlock in a MonoBehaviour field initializer, despite passing test totals. Creation was moved into ResetWorld; a component-construction regression was added. The validation script now also rejects runtime/shader exception logs and non-passing XML independently of process exit code. A separate PlayMode regression injects a completed LOD-only result after its tile was evicted, ensuring it is rejected/rescheduled rather than installed with a missing vegetation batch.

Physical acceptance remains unperformed: see [the concise checklist](ALPHA_v0.0.3_MANUAL_ACCEPTANCE.md). Synthetic input and scripted paths do not establish physical mouse feel, alt-tab behavior or human UI ergonomics.

## Performance investigation

Before optimization, an instrumented copy of the preceding implementation was built and rendered at 2560 x 1440 on the i7-8700K / RTX 3060 Ti. Its 13 original scenarios and every CSV frame are preserved separately in [Baseline](Validation/Alpha-v0.0.3/Baseline/). No compilation overlapped accepted timing intervals.

The earlier historical coast 41.70 ms and water 22.51 ms maxima did not recur in that new baseline (8.63/6.15 ms). They remain valid historical outliers. The instrumented baseline instead recorded a 31.84 ms Large-streaming frame, a 16.87 ms frame there, and a 17.58 ms blur-off frame. The largest had no nearby GC marker; one of the other Large spikes and the blur-off spike had a GC marker within one adjacent frame. On the 31.84 ms sample the tracked renderer Update was 0.133 ms, present-wait marker 0.002 ms and GC marker zero. The trace does not explain the remainder. OS scheduling, engine/render-thread stalls and driver behavior remain unresolved; no unsupported cause is assigned.

Confirmed avoidable costs were removed through source inspection: Mesh.triangles returns a copied index array when only its count was needed; terrain jobs rebuilt identical topology and unused colors; LOD changes recomputed installed vegetation; the HUD built layout objects/arrays and formatted strings on GUI events. Final results measure the combined refinement, not an isolated attribution of every cost or a controlled visual-workload speedup.

The trace uses Unity GC.Collect and available wait markers, main-thread scope timers around HUD/vegetation/upload, frame-loop timing and GPU timing. Profiler counters not exposed in the release player are marked unavailable. A positive-allocation self-test guards the runtime byte counter: zero from an unsupported API must not be presented as zero allocation. Scopes cover the previous completed frame and GPU timing may lag; adjacency is correlation, not proof of causation. Raw marker availability is retained beside every CSV.

The early visual Preview is retained under Builds/Alpha-v0.0.3/Preview. It exposed aliased foliage grain and slope-floating outcrops; those were filtered/aligned before the final build. Its byte readings were not capability-checked and are excluded as allocation evidence. Final records explicitly mark unsupported counters. The preview also motivated increased overview fidelity and deeper skirts after a distant seam was visible in the low-angle view.

## Final rendered-player measurements

Uncapped visible Windows x64 player, VSync off, 2560 x 1440. Fourteen scenarios include all previous paths plus low-angle exploration. Initial local readiness and two seconds warm-up are excluded; motion continues sampling during streaming. Static intervals are 8 s, motion/blur 12 s, rapid zoom/Large streaming 16 s. Large streaming uses the corrected bounded 2.5 km-radius loop at 1,600 m/s; other movement and zoom paths match the preceding harness. A render callback is required in each interval. Screenshots/CSV writing occur after timing. Trace overhead is included.

The first final-code windowed run reported 1705 x 958 despite the requested command-line size. It is retained separately as a resolution diagnostic, not accepted as 1440p evidence. The same binary was rerun with exclusive fullscreen requested; acceptance uses the actual per-scenario reported resolution, not the requested flags. This display-mode difference from the earlier windowed baseline is a comparison limitation. The Unity bootstrap's PE file version remains the engine version; application/bundle identity is 0.0.3 in generated metadata and every final scenario record.

| Scenario | Engine-loop FPS mean | p95 ms | p99 ms | Max ms | GPU mean ms | Peak chunks | Visible trees peak |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| overview | 286.3 | 3.80 | 3.93 | 5.15 | 3.354 | 169 | 0 |
| mountains | 235.4 | 4.52 | 4.69 | 5.41 | 4.084 | 95 | 12876 |
| grassland | 263.6 | 4.33 | 5.20 | 6.87 | 3.558 | 71 | 8510 |
| coast | 200.8 | 5.25 | 5.47 | 5.99 | 4.849 | 111 | 13273 |
| water | 249.6 | 4.33 | 4.98 | 6.60 | 3.834 | 85 | 11601 |
| horizon | 175.9 | 5.99 | 6.42 | 7.44 | 5.542 | 128 | 14235 |
| forest | 141.3 | 7.49 | 7.94 | 8.91 | 6.903 | 68 | 18425 |
| continuous | 135.8 | 8.58 | 8.98 | 9.30 | 7.194 | 87 | 26205 |
| rapid-zoom | 189.5 | 8.31 | 9.67 | 10.52 | 5.115 | 169 | 41129 |
| large-overview | 305.5 | 3.59 | 3.72 | 4.35 | 3.127 | 169 | 0 |
| large-streaming | 190.0 | 9.26 | 10.05 | 11.27 | 5.108 | 87 | 34327 |
| blur-off | 221.7 | 5.38 | 6.62 | 7.70 | 4.351 | 83 | 14409 |
| blur-on | 215.2 | 5.63 | 6.68 | 7.40 | 4.492 | 83 | 14409 |
| low-angle | 173.7 | 6.42 | 7.79 | 8.79 | 5.506 | 69 | 19782 |

All **29,226 sampled frames** across 14 scenarios rendered at actual 2560 x 1440; rendered counts were within one frame of sampled counts. Application and bundle versions were both 0.0.3. No sampled frame exceeded 16.67 ms. The longest was **11.27 ms** during Large streaming. Continuous travel averaged **135.8 FPS**, rapid zoom **189.5 FPS**, dense forest **141.3 FPS**, and Large streaming **190.0 FPS**. The final coast/water maxima were **5.99/6.60 ms**. This is evidence of good short-run engine pacing, not proof that the historical pauses are permanently eliminated.

The additional geometry/shading has a real cost: averages are lower than the preceding instrumented baseline. Overview geometry is nine times the previous triangle count, crowns are more detailed, near/far overlap is counted/rendered, and FXAA is enabled. The runs also differ in display mode. This is not presented as an overall FPS speedup. Final GPU means ranged from 3.13 to 7.19 ms, versus the simpler preceding workload. The representative 60 FPS budget retained headroom in this run.

| Scenario | Baseline collections | Final collections | Baseline per 1,000 frames | Final per 1,000 frames |
| --- | ---: | ---: | ---: | ---: |
| Coast | 52 | 26 | 22.69 | 16.17 |
| Forest | 41 | 16 | 22.79 | 14.13 |
| Continuous travel | 125 | 59 | 48.15 | 36.17 |
| Rapid zoom | 279 | 183 | 60.00 | 60.30 |
| Large streaming | 303 | 160 | 62.55 | 52.60 |

Lower interval totals partly reflect fewer rendered frames. In particular, rapid zoom's collections per frame did **not** improve; allocation churn remains an open optimization target. The retained gen0Collections field is GC.CollectionCount(0), treated here as an observed runtime counter, not proof of generational collector behavior. No allocation-byte reduction is claimed: the positive 4,096-byte probe failed to produce a valid byte increment, so byte fields are **unavailable/-1**. GC Allocated In Frame and the render-thread wait counter were also unavailable; GC.Collect and present-wait were available.

During continuous travel, explicit main-thread scopes averaged **0.516 ms HUD**, **0.530 ms vegetation**, and **0.025 ms upload**, with maxima of 3.705/1.596/1.947 ms. Large-streaming upload maximum was 2.537 ms; overall renderer Update peaked at 3.125 ms there. These scopes do not include every engine/driver wait and have no matching baseline scope coverage, so no exclusive causal speedup is inferred. Full scope distributions and every frame are in [trace-analysis.json](Validation/Alpha-v0.0.3/trace-analysis.json) and its CSVs.

Normalized process CPU ranged 11.6-24.7% of 12 logical processors. Unity allocated-memory peak was **163.5 MiB**, excluding VRAM; current/fading/overview terrain peaked at **2,236,674 triangles** and submitted instanced vegetation/cover/outcrops at **23,795,387 triangles**, excluding shadow/depth passes. Peak visible trees reached **41,129**. First local ready times were 3.237 s Medium and 3.335 s Large. Blur off/on averaged 221.7/215.2 FPS, with GPU means 4.351/4.492 ms, about 2.9% lower mean FPS in this one sequential comparison. Blur remains off by default.

The final [metrics](Validation/Alpha-v0.0.3/metrics.json), [player log](Validation/Alpha-v0.0.3/player.log), per-scenario records, CSVs, counter capabilities and 14 captures are retained. Historical outliers and baseline traces were not altered or filtered out.

These are **engine-loop** measurements, not independently measured display presentation. No PresentMon, dedicated-VRAM capture or long-soak result is claimed. Main Thread includes idle/synchronization. Resident terrain counts now include overview/ocean and outgoing fade copies; the older Phase 1.1 field excluded those, so direct triangle-count comparison would be misleading. Instanced geometry counts include submitted LOD overlap and cover/outcrops but exclude extra shadow/depth passes. Memory excludes dedicated VRAM. meshCpuMs accumulates accepted detailed mesh/placement jobs, excluding overview generation, discarded work and uploads; it is not total worker CPU or wall-clock whole-world generation. Normalized process CPU covers the process more broadly. Readiness is the first local ready state after regeneration, not every camera-stage latency. The tiny aggregate simulation is not a civilization load test.

## Screenshots and reference comparison

All eight requested final 1440p views were visually inspected:

| View | Capture | Verified improvement and remaining limitation |
| --- | --- | --- |
| Continental overview | [00-overview](Validation/Alpha-v0.0.3/00-overview.png) | Clearer relief and less haze washout; artificial belt/plateau repetition remains in the unchanged height field. |
| Rocky ranges | [01-mountains](Validation/Alpha-v0.0.3/01-mountains.png) | Less regular surface banding and slope-aligned outcrops; broad rounded walls still need geological shape work. |
| Grassland close-up | [02-grassland](Validation/Alpha-v0.0.3/02-grassland.png) | Readable fine grain and gentler soil color patches; sparse near ground cover is provisional. |
| Coastline | [03-coast](Validation/Alpha-v0.0.3/03-coast.png) | Varied sand/grass transition and forest edge; beaches still often read as a smooth broad band. |
| Shallow water | [04-water](Validation/Alpha-v0.0.3/04-water.png) | Transparent depth transition, restrained glare and softer edge; 8 m shore geometry still visibly steps at low angles. |
| Distant horizon | [05-horizon](Validation/Alpha-v0.0.3/05-horizon.png) | Finer distant ranges and atmospheric layers; silhouettes and the ocean/sky line remain simplified. |
| Dense forest | [06-forest](Validation/Alpha-v0.0.3/06-forest.png) | Mixed crown silhouettes, trunk tones and scales; the preview's repetitive grain pattern is reduced. Faceted, rounded procedural crowns remain obvious. |
| Low-angle terrain | [13-low-angle](Validation/Alpha-v0.0.3/13-low-angle.png) | Close sand/forest inspection and finer background; a small bright sliver persists below the far-right slope. The larger preview seam is reduced, not proven fully resolved. |

Skirts, matched border samples, deferred replacements and resource tests reduce specific transition risks. These still images do not verify every moment of the moving paths. Continuous visual LOD/vegetation acceptance and the remaining far-slope artifact remain open. No claim of universally invisible seams or popping is made.

References are the unchanged [valley](Validation/Phase11/References/01-valley.png), [coast](Validation/Phase11/References/02-coast.png) and [continental](Validation/Phase11/References/03-continent.png) images supplied by the user. The new views can be compared with the [preceding Phase 1.1 gallery](PHASE_1_1_TEST_RESULTS.md). Refined crowns, clearer terrain, varied shoreline shading and finer overview improve the composition. Photorealistic foliage, natural geological silhouettes, detailed shore clutter, sky/cloud richness and drainage detail remain below those references. No rivers/waterfalls, settlements or ecosystems depicted by the references are silently treated as implemented.

## Stability decision and remaining work

**Runnable and validated as an Alpha development build; not ready for an unconditional stable-release declaration.** Automated behavior/resource checks and all short rendered scenarios passed. This run stayed within the representative 60 FPS engine budget, but physical controls, full moving-LOD review, the remaining bright sliver/reference-fidelity gaps, long-soak/VRAM/presented-frame evidence and the root cause of intermittent historical outliers are not closed. No release was declared, no deferral was silently accepted, and Alpha v0.0.4 was not begun.

1. Perform the physical camera/UI checklist in Editor and player, including focus loss, alt-tab and seed text entry.
2. Review continuous terrain/forest transitions as motion, including rapid inverse zoom, low-angle seams, dither/shadow changes and coarse fallback. Automated border/resource tests and still captures do not prove universally seamless LOD.
3. Profile unresolved long frames with event-level engine/OS/GPU traces where available. GC counts and nearby markers do not explain every pause. Allocation byte counters may be unavailable; do not infer zero churn from them.
4. Improve remaining reference-fidelity gaps: plausible mountain-wall forms, finer coastline topology, plant/leaf assets, ground-cover density and more natural horizon/sky. Keep authoritative versioning deliberate if the height algorithm is changed later.
5. Measure long sessions, regeneration stress and VRAM/presented-frame behavior before a stronger stability claim. Do not start v0.0.4 until these gates are completed or explicitly accepted for deferral.

## Files and reproduction

New/changed implementation: Assets/Scripts/Core (version and opt-in scope counters); DeveloperHud; WorldRenderer; WorldVegetation; VegetationDistribution; WorldAtmosphere; terrain/water/vegetation shaders and serialized material defaults; RefinementBenchmark; AevoriaBootstrap benchmark routing; presentation/UI/test assembly references; Phase1Validation builder; ProjectSettings bundle-version mirror. The pure geography algorithm, simulation, save envelope, camera controller, engine and package versions are preserved.

Tests/tooling: AlphaPolishTests, RegenerationTests, portable version check and project include, PerformanceTrace and Tools/AnalyzeFrameTrace.mjs. Documentation: README, CHANGELOG, VERSIONING, Chronicles/metadata, ROADMAP, ARCHITECTURE, PERFORMANCE_STRATEGY, TERRAIN_DESIGN, CAMERA_CONTROLS, MIGRATION_STATUS, DEVELOPMENT_WORKFLOW, manual acceptance and this report. New Unity assets include metadata.

Run portable checks, then Tools/ValidatePhase1.ps1 with edit/play/build sequentially. The historical entrypoint remains for compatibility; default output is now Builds/Alpha-v0.0.3 from ApplicationVersion. Copy the validated player back and launch visibly with -aevoriaBenchmark003 -aevoriaTrace -aevoriaOutput <absolute-directory>, 2560 x 1440 windowed. Wait for Editor/build completion before accepting timings. Analyze with node Tools/AnalyzeFrameTrace.mjs <directory>. Preserve every raw frame and keep new runs separate from historical evidence.

