# Phase 1 validation — 2026-10-21 (America/Chicago)

Implementation is in M:\Aevoria. Aevoria-old was not modified or used for terrain. Git staging/history were preserved. The pre-existing camera/bootstrap/HUD edits were extended; baseline copies are retained in the ignored Temp/Phase1Baseline directory.

## Automated evidence

- **Portable: 13/13 checks passed**, including all original seven. Checks cover clock cadence/pause/speed, seed behaviour, finite elevation, land/ocean coverage, signed/shared coordinates, configuration validation and presets. The final generator version 2 run evaluated 49,923 elevation samples in **84.85 ms** under .NET 10; it is not Unity generation or rendering performance. The 129x129 default-seed grid had 6,929 land and 9,712 ocean points, with elevations **−401.10 to 386.84 m**.
- **EditMode: 10/10 tests passed**, including all three original foundation tests. New coverage includes reverse-order seed sampling, finite/classified elevation, invalid settings, actual generated mesh border heights/normals and index bounds at all three LODs, unchanged elevation when sea level rises, camera-relative keyboard/Shift/reset/deceleration, synthetic right-drag tilt/yaw, both pitch stops, zoom limits, middle drag and HUD blocking.
- **PlayMode: 1/1 test passed**, exercising three successive generations. It verifies retained simulation clock identity/tick progression, bounded active chunks, filter ownership, live mesh counts after destruction and sampled camera clearance.
- Unity 6000.3.27f1 compiled the assemblies. Final test XML is in [Validation/phase1-edit.xml](Validation/phase1-edit.xml) and [Validation/phase1-play.xml](Validation/phase1-play.xml).

Input tests use Unity's isolated InputTestFixture and synthetic queued device events. They do not establish physical mouse or keyboard acceptance. Early harness failures (Editor focus discarding events and asserting a clock tick before its 250ms interval) were corrected; the reported totals are the passing reruns. After the version 2 landform change, the sea-level test was expanded from one centre transect to a full-map grid: the previous transect no longer crossed land between its two waterlines. The test still requires unchanged elevations and a positive flooded-land count.

## Windows build and runtime investigation

Optimized Windows x64/Mono builds succeeded under Unity 6000.3.27f1 and URP 17.3.0. Early builds failed before Aevoria bootstrap, reporting URP asset/renderer serialization-layout mismatches and a native allocation failure. The failure is retained in [Validation/startup-failure.log](Validation/startup-failure.log). **The startup failure is now resolved.** The final scripted clean build reported **102,395,785 bytes in 79.09 seconds**, and the copied player at `M:\Aevoria\Builds\Phase1\Aevoria.exe` starts successfully.

The validation script used `$env:TEMP`, which expands to `C:\Users\SEANMA~1\AppData\Local\Temp` on this machine. Unity's build logs then reported `TypeDB: Assembly index ... was not found` for assemblies whose expanded paths contain `Sean Marsh`. Both fresh imports and CleanBuildCache reproduced the failure. A command-line build using the alias also failed; rebuilding the same validation copy with the full path removed the TypeDB warnings and player startup errors. The script now uses `$env:LOCALAPPDATA\Temp` and rejects builds containing those TypeDB warnings. Subsequent scripted builds using the full path succeeded; the final player was copied back to the source workspace. A separate Editor compilation out-of-memory crash during the earlier investigation did not recur in the successful builds.

Unity's [clean build guidance](https://docs.unity.com/en-us/engine/6000.0/manual/building-and-publishing/build-clean-build) distinguishes build caches from imported assets/shader caches. BuildWindows now uses CleanBuildCache; the validation script uses the dedicated temporary AevoriaPhase1Validation copy and leaves earlier caches intact.

## Standalone performance and visual acceptance

The opt-in WorldBenchmark runs at 2560x1440 with three seeds/two sizes/three orbit distances, warm-up exclusion, screenshots, frame percentiles, process CPU, GPU timing where available, memory, GC and geography/mesh readiness statistics. Hidden-window probes were discarded: Unity ran Update without rendering, produced implausibly high loop rates and failed screenshot capture. The benchmark now requires main-camera render callbacks during the sample interval. A hidden-window test of the probe correctly logged `Benchmark rejected: no camera frames rendered` and produced no stage results. Only the final visible-window run after build completion is accepted below.

Final optimized Windows player, **generator version 2, 2560x1440**, VSync/frame cap disabled, simulation running; **Intel i7-8700K / RTX 3060 Ti**, Unity 6000.3.27f1. Each stationary view excludes three seconds of warm-up and measures ten seconds. The benchmark completed all three stages and exited normally without serialization, shader or screenshot errors.

| Seed / preset / orbit | Average FPS | Mean frame ms | p95 ms | p99 ms | Max ms | GPU mean ms | View ready ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| aevoria-001 / Medium / 900 m | 432.52 | 2.312 | 4.032 | 5.305 | 7.248 | 1.125 | 2377.53 |
| aevoria-002 / Small / 350 m | 469.89 | 2.128 | 3.756 | 4.864 | 7.802 | 0.888 | 844.25 |
| archipelago-37 / Medium / 1800 m | 466.33 | 2.144 | 3.755 | 4.930 | 8.906 | 0.881 | 1254.05 |

| Stage | Chunks / triangles | Mesh CPU ms | Process CPU, normalized | Managed bytes | Gen0 collections |
| --- | ---: | ---: | ---: | ---: | ---: |
| 0 | 169 / 247,296 | 1015.53 | 20.71% | 7,245,824 | 117 |
| 1 | 64 / 180,096 | 757.22 | 20.62% | 7,897,088 | 101 |
| 2 | 169 / 247,296 | 1055.25 | 20.80% | 7,995,392 | 112 |

Raw stage records include frame counts, p50 and sampled last-tick costs: [stage 0](Validation/Standalone/stage0.txt), [stage 1](Validation/Standalone/stage1.txt), [stage 2](Validation/Standalone/stage2.txt). Last-tick render-sample means were 0.0001 ms; maxima were 0.0002–0.0003 ms for this minimal aggregate simulation. These samples repeat the most recent tick duration and are not an independent tick latency distribution. Managed byte snapshots exclude native/GPU memory; Gen0 counts expose the HUD/sample allocation cost. Ready time covers the current streamed view, not an eagerly generated whole world. The final portable run is reported separately above.

Inspected final captures: [900 m](Validation/Standalone/stage0.png), [350 m](Validation/Standalone/stage1.png), [1800 m](Validation/Standalone/stage2.png). Terrain, water and HUD render with distinct layouts across seeds, with no pink missing-shader fallback. The [version 1 overview](Validation/Standalone/generator1-overview.png) exposed pinched/spiky mountain bands. Version 2 uses broader smooth range profiles, lower domain warping and larger terrain wavelengths; the final captures show rounder connected ranges, quieter lowlands and reduced needle-like peaks. Sand/grass/rock blending still lacks fine texture, water is flat/opaque and the finite water/map boundary is visible at wider views. Moving LOD transitions and full visual acceptance remain open.

Static engine frame-loop timings exceed the 60 FPS target in these three views. They do **not** establish displayed/presented FPS, sustained movement/Large-preset performance, physical input acceptance or future civilization simulation performance. No Editor/build process was running for the final measurement. See [player log](Validation/Standalone/player.log), [hidden-window rejection](Validation/Standalone/hidden-window-rejected.log) and [build summary](Validation/build-summary.txt).

## Changes by file

| Area | Files created or modified |
| --- | --- |
| Geography | Assets/Scripts/World/Geography.cs; new ProceduralGeography.cs |
| Presentation | AevoriaBootstrap.cs, MapCamera.cs; new WorldRenderer.cs and WorldBenchmark.cs |
| HUD | Assets/Scripts/UI/DeveloperHud.cs and Aevoria.UI.asmdef |
| Visual assets | new Assets/Shaders/WorldTerrain.shader; Resources/WorldTerrain.mat and WorldWater.mat |
| Build tooling | new Assets/Editor/Phase1Validation.cs and Tools/ValidatePhase1.ps1 |
| Configuration | ProjectSettings/ProjectSettings.asset: Frame Timing Stats enabled |
| Tests | original FoundationTests preserved; new Editor/TerrainTests.cs, extended test asmdef, new PlayMode assembly/RegenerationTests.cs; extended Tests/PortableChecks/Program.cs |
| Documentation | TERRAIN_DESIGN, CAMERA_CONTROLS, ARCHITECTURE, PERFORMANCE_STRATEGY, MIGRATION_STATUS, ROADMAP, DEVELOPMENT_WORKFLOW and this report; validation evidence |

All new Unity assets/folders include .meta files. Shared materials are retained through Resources, and meshes are generated/released by a single renderer owner. Placeholder assets/types remain for foundation compatibility but bootstrap uses the new world.

## Implemented features and remaining gates

The new versioned generator provides warped continents/coasts, connected mountain bands, regional hills/plains/plateaus and softened valley corridors. Geography is independent of GameObjects/global random state. Small/Medium/Large presets, bounded immutable settings, signed chunks, shared normals/heights, cancellable generation, pooling, three skirt LODs, sea-level water, elevation/slope surface blending, warm lighting/haze, regeneration and diagnostics are implemented.

Camera changes add right-drag yaw/tilt, smooth zoom, ray-plane middle pan, altitude-scaled accelerated WASD/Shift, reset/focus, terrain/ocean clearance and bounds. HUD/text/window guards suppress accidental navigation and require button release after focus changes. The baseline lacked right-drag/tilt entirely; wheel and middle-drag existed, so their original physical failure is not assigned an unproven cause.

- [x] Final player startup and standalone terrain/water material compatibility; see standalone evidence below.
- [ ] Physical controls in Editor and Windows player, including window exit/alt-tab and text-field focus. See CAMERA_CONTROLS.md.
- [ ] Full visual acceptance: three final seed/scale captures inspected and range silhouettes improved; surface detail is provisional and moving mixed-LOD transitions require review.
- [ ] 1440p presented-frame measurement and moving-camera/Large preset stress test on the target hardware.

Known implementation limits: fixed pivot-distance LOD rather than projected-error/altitude LOD; skirts instead of stitching/morphing; finite render-radius coverage at maximum zoom; inexpensive opaque water without refraction/foam; artistic field shaping rather than geological/erosion/hydrology simulation; provisional climate/fertility channels; approximate F terrain intersection; analytic clearance rather than a swept collision volume; IMGUI diagnostics allocate. No civilizations or vegetation were added. Save-envelope validation remains unchanged; full versioned world persistence is still future work.

Recommended next milestone: refine terrain silhouettes and surfaces, verify physical camera controls, then review moving LOD transitions and profile continuous movement/Large-world coverage before adding further simulation.
