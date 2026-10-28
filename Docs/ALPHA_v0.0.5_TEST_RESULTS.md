# Alpha v0.0.5 — Living Ecosystems validation

Development implementation, October 22, 2026. No public release declared. This extends the real v0.0.4 build; earlier physical, reference-image, LOD and long-session acceptance remains open.

## Delivered behavior

Pure regional ecology advances monthly independently of camera visibility: six vegetation pools, four wildlife feeding groups, seasonal water/productivity, recruitment, mortality, stand age, conservative migration, food limitation and predation. Drought, fire, flood, cold and heat have persistent duration, effects and recovery. A bounded structured history records actual environmental outcomes. Compatible saves restore the entire ecological state and random progression. F4 inspection exposes live values and six regional maps. At most 128 local procedural animal silhouettes use four instanced meshes. Forest masking, moisture and persistent burn discoloration reflect ecological state. No human societies are introduced.

[Ecology architecture and exact limits](ECOLOGY.md) · [Changelog](../CHANGELOG.md) · [Book V — The First Stirring](THE_CHRONICLES_OF_AEVORIA.md#chapter-v---the-first-stirring). Earlier changelog and Chronicle bytes remain a complete prefix of their updated files. The creator’s 3,200 m relief ceiling and generator 4 are preserved.

## Automated validation

- 90 portable checks passed, including the retained foundation suite and 24 long-run checkpoints. [Raw output](Validation/Alpha-v0.0.5/portable-results.txt).
- Unity EditMode: 42/42 passed. Unity PlayMode: 6/6 passed. [Edit results](Validation/Alpha-v0.0.5/edit-results.xml) · [Play results](Validation/Alpha-v0.0.5/play-results.xml).
- Windows x64 standalone build succeeded using Unity 6000.3.27f1 / URP 17.3.0 and the existing validation copy. Unity reported 75.289 s; the complete delivered build folder contains 109,917,846 bytes. [Build log](Validation/Alpha-v0.0.5/build.log) · [Build identity](Validation/Alpha-v0.0.5/build-info.json).
- Tests cover deterministic initialization/catch-up, conservation, intervening water/mountain barriers, births, food scarcity, predation, drought productivity, fire/forest mortality and regrowth, invalid/future state rejection, bounded event history, hemisphere/tropical seasons, full snapshot restoration, continued RNG/event equivalence, inspection immutability and visual-resource cleanup. Existing camera/streaming/persistence tests remain.

## Long-term aggregate simulation

Two seeds at each standard world size reached 1, 10, 100 and 1,000 years. All four species persisted in these six ordinary-climate runs; numeric validation passed at every checkpoint. This is not a promise against local extinction or an ecological calibration against real-world data. The model has no artificial population floor.

| Seed | Size | Regions | Years | Years/s | Mean forest | Mean biomass | Deer / browsers / hares / wolves | Managed MiB | Last step ms |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: |
| ecosystem-005 | Small | 256 | 1000 | 320.08 | 5.85% | 32.26% | 560.36 / 170.39 / 1015.69 / 6.96 | 96.85 | 0.24 |
| aevoria-001 | Small | 256 | 1000 | 322.72 | 5.58% | 32.60% | 552.94 / 162.38 / 986.96 / 6.57 | 99.60 | 0.23 |
| ecosystem-005 | Medium | 1024 | 1000 | 82.40 | 6.50% | 32.45% | 1944.97 / 673.23 / 3684.43 / 25.30 | 38.25 | 0.86 |
| aevoria-001 | Medium | 1024 | 1000 | 64.92 | 6.96% | 33.37% | 1910.12 / 692.74 / 3702.11 / 26.00 | 36.96 | 1.37 |
| ecosystem-005 | Large | 4096 | 1000 | 16.09 | 6.36% | 33.35% | 7778.12 / 2547.93 / 14457.98 / 101.33 | 35.33 | 3.50 |
| aevoria-001 | Large | 4096 | 1000 | 15.19 | 6.06% | 32.32% | 7513.19 / 2469.12 / 13952.40 / 95.71 | 43.01 | 3.40 |

Values are end-of-run samples from portable .NET, not Unity player performance or peak memory. Timing can be affected by other validation work on the machine. Forest biomass generally declined from initial conditions toward a lower equilibrium, rather than remaining constant. Moisture, vegetation, events and population totals at every checkpoint remain in the [raw CSV](Validation/Alpha-v0.0.5/ecology-long-run.csv). The retention-64 stress runs retain completed history plus currently active events; monotonic event IDs continue beyond eviction. Long simulated time does not establish multi-hour wall-clock reliability.

## Actual rendered standalone measurements

Intel(R) Core(TM) i7-8700K CPU @ 3.70GHz; NVIDIA GeForce RTX 3060 Ti. Every scenario reports actual 2560 × 1440 output and actual main-camera rendered counts. These are uncapped engine-loop intervals, not an independently measured presentation rate. Measured frames: 32,694. Raw traces retain every measured interval, including 1 intervals above 16.67 ms.

| Scenario | Frames / rendered | Mean ms | Engine FPS | p95 ms | p99 ms | Max ms | GPU ms | Ecology step ms | Proxies submitted | Unity MiB | GC0 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| menu | 1753 / 1753 | 1.71 | 584.08 | 2.21 | 2.60 | 3.73 | 1.08 | no step | 0 | 93.93 | 10 |
| grassland | 1383 / 1383 | 3.62 | 276.44 | 4.49 | 5.06 | 5.44 | 3.12 | no step | 29 | 153.98 | 14 |
| forest | 1132 / 1132 | 4.42 | 226.29 | 5.54 | 6.31 | 8.07 | 3.79 | no step | 47 | 152.13 | 11 |
| wildlife-heavy | 1286 / 1286 | 3.89 | 257.20 | 4.67 | 4.96 | 5.42 | 3.61 | no step | 35 | 152.58 | 10 |
| predator-prey | 1495 / 1495 | 3.35 | 298.93 | 4.12 | 4.58 | 6.53 | 2.97 | no step | 35 | 153.68 | 11 |
| drought | 1073 / 1073 | 4.66 | 214.41 | 5.69 | 6.67 | 7.76 | 4.13 | 0.67 | 37 | 155.56 | 17 |
| fire | 690 / 690 | 7.25 | 137.89 | 8.33 | 8.87 | 9.73 | 6.95 | 0.36 | 63 | 155.97 | 10 |
| flood | 783 / 783 | 6.39 | 156.58 | 7.38 | 7.91 | 8.55 | 6.06 | 0.92 | 45 | 158.70 | 12 |
| overview | 1348 / 1348 | 3.71 | 269.46 | 4.42 | 4.73 | 5.13 | 3.46 | 0.92 | 0 | 145.38 | 18 |
| rapid-travel | 1773 / 1773 | 5.64 | 177.29 | 9.93 | 11.28 | 13.20 | 5.18 | 0.92 | 49 | 156.70 | 47 |
| rapid-zoom | 1697 / 1697 | 4.71 | 212.11 | 6.05 | 6.83 | 8.13 | 4.43 | 0.92 | 49 | 153.04 | 42 |
| accelerated | 1948 / 1948 | 5.14 | 194.72 | 6.04 | 6.63 | 55.65 | 4.71 | 2.46 | 53 | 160.42 | 27 |
| moving-soak | 11809 / 11809 | 5.08 | 196.81 | 6.24 | 6.82 | 8.32 | 4.67 | no step | 37 | 157.74 | 142 |
| large-world | 1756 / 1756 | 4.56 | 219.41 | 5.40 | 5.73 | 6.81 | 4.21 | no step | 19 | 162.00 | 15 |
| large-accelerated | 1048 / 1048 | 7.63 | 131.14 | 11.32 | 14.03 | 16.08 | 3.73 | 13.85 | 21 | 162.34 | 7 |
| menu-after-unload | 1720 / 1720 | 1.74 | 573.28 | 2.26 | 2.56 | 3.16 | 1.16 | no step | 0 | 111.12 | 7 |

The accelerated scenario advances the existing clock through actual ticks, with aggregate ecological steps, while the camera remains rendered. Local animal behavior does not replay every simulated moment. Drought/fire/flood are explicitly injected through checked model preconditions for repeatable coverage; their images are not evidence of a naturally occurring disaster. “Fire” shows active regional damage without flame/smoke; “flood” shows its ecological state without dynamic inundation geometry. “Grassland” is a selected open habitat, not a fabricated empty render scene. Moving/zoom/soak paths are scripted, not physical-input acceptance.

The final audit includes a 60-second moving soak and repeated unload/load; this does not satisfy multi-hour reliability. CPU process time, managed memory, populations, tick/month progression and screen/render counts are in [audit.json](Validation/Alpha-v0.0.5/Standalone/audit.json). Unsupported profiling/allocation counters remain marked unavailable in adjacent counter files; no zero-allocation claim is made. Per-application VRAM, independently presented FPS and unavailable GPU/working-set counters are not inferred from total adapter memory.

A host PowerShell sampler observed maximum working set 866.77 MiB and private bytes 1281.71 MiB over 155 samples at approximately 1 Hz. This is an observed sampled maximum, not an instantaneous process peak; sampling began after launch. [Raw host memory](Validation/Alpha-v0.0.5/Standalone/host-memory.csv).

accelerated: 16.16 simulated years per real second over 10.05 s in the rendered player.
large-accelerated: 10.88 simulated years per real second over 8.03 s in the rendered player.

[Raw frame traces and counter declarations](Validation/Alpha-v0.0.5/Standalone) · [Complete machine-readable summary and outliers](Validation/Alpha-v0.0.5/summary.json). Any GC marker near a long frame is correlation, not a proven cause. Harness initialization warms two frames before measurement; transitions outside each sample window are not included in the reported scenario distributions.

The complete [initial standalone run](Validation/Alpha-v0.0.5/InitialStandalone) is also retained. It sampled 31,104 frames with two accelerated-time outliers at 28.78 and 24.10 ms, neither accompanied by a GC collection in that row. Subsequent changes improved close wildlife framing, sparse predator proxies, Book V color and scorched-ground contrast, and added Large accelerated coverage. The earlier outliers are not discarded or explained away by a faster later run.

## Persistence and resources

Standalone create 6956.86 ms; save 94.22 ms; load 7004.83 ms. Create/load include initial presentation readiness, not just serialization. Full ecological snapshot equivalence after actual disk reload: **true**. Continued ten-year ecology equivalence after reload: **true**. Final world/visual service cleanup: **true**.

All automated player archives use a required isolated test root. Schema 3 / ecology 1 saves are compatible within this implementation. Schema 2 worlds require an explicit migration and are rejected without modification; there is no migration UI. Unsupported/future archives cannot silently reset forests or overwrite the original. Metadata-only enumeration keeps these worlds visible. Existing atomic replacement, backup preservation and checksum tests continue to pass. No claim of safe cross-version migration is made.

## Visual review and remaining gates

14 actual standalone PNG captures are retained, with dimensions recorded in the summary. Review findings are recorded separately below.

- [Wildlife close-up](Validation/Alpha-v0.0.5/Standalone/wildlife-heavy.png) shows an antlered procedural deer in its habitat. Shapes are recognizable placeholders; limbs do not have a production gait. Some proxies remain occluded or off screen. The “visible animals” diagnostic counts submitted nearby proxies, not animals independently counted in visible pixels. Sparse wolves may represent fractional regional density; proxies never add to authoritative populations.
- [Fire scar after the event](Validation/Alpha-v0.0.5/Standalone/fire-scar-after-event.png) shows the active event has ended while the burn scar and depressed biomass remain. Ground discoloration is visible. Coverage is a blended regional proxy, with coarse boundaries and individual tree thresholds still requiring refinement.
- [Book V](Validation/Alpha-v0.0.5/Standalone/02-book-v.png) displays The First Stirring with its own green accent; earlier chapters remain selectable. [Recents](Validation/Alpha-v0.0.5/Standalone/recents.png) displays the actual saved year, last played, seed and size.
- The wider habitat captures still expose faceted crowns and coarse channel/lake ribbons. The predator/prey close-up does not establish a clean, unobstructed wolf animation shot. Flood imagery does not demonstrate dynamic water inundation. These visual gates remain open.
- Final acceleration outlier: 55.65 ms at trace frame 88, with a 1.30 ms GC marker and one GC0 collection. Terrain mesh ownership changes nearby; upload scope is zero in the outlier row and approximately 0.15 ms afterward. Neither these observations nor the 2.46 ms maximum recorded Small ecology step explains the entire interval. The cause remains unresolved; consistent 60-FPS frame pacing is not accepted as complete.
- No managed runtime exceptions, TypeDB assembly-index failures or unreleased-resource notices appear in the final player log. This single process exit is not a proof against leaks during longer use.

Open implementation/acceptance: production animal silhouettes and animation; explicit plant dispersal/cohorts; exact coastal habitat areas; per-species navigation/water-seeking; dynamic flood water and flame/smoke effects; safe legacy migration UI; full concept-reference fidelity; physical controls/display rollback/OS-close acceptance; multi-hour reliability; per-application VRAM and independently measured presentation. Regional cover changes use stable candidates and a blended state texture, but individual-tree thresholds and coarse ecological boundaries can still be visible. Long-run ordinary-climate stability is demonstrated for the tested cases, not every user configuration. Earlier shoreline, terrain/LOD and coarse hydrology limits remain in the v0.0.4 report.

## Source and next milestone

New: World/EcologyModel.cs; Presentation/EcologyPresentation.cs and Alpha005Audit.cs; Editor/EcologyTests.cs; PlayMode/EcologyPresentationTests.cs; portable EcologyChecks.cs; ecology documentation and evidence tools. Updated: archive schema/snapshots, bootstrap lifecycle/time/save wiring, menu/recents/history reader, camera UI guard, vegetation reserve sites and shared foliage/terrain shaders. Application version/settings mirror are 0.0.5 Development. No package or engine upgrade, new Unity project, legacy WPF changes, commits or staging edits were made.

Before v0.0.6, resolve significant performance/visual findings, add the deliberate old-world migration route and complete physical/long-session acceptance. Future peoples may consume stable ecological region identifiers, fertility, water, vegetation, wildlife, climate and risk; peoples, tribes, settlements and civilization systems remain future work.

[Open manual acceptance](ALPHA_v0.0.5_MANUAL_ACCEPTANCE.md) · [Source/cache hashes and preservation checks](Validation/Alpha-v0.0.5/source-preservation.json). The delivered executable is Builds/Alpha-v0.0.5/Aevoria.exe with its complete adjacent runtime folders. The normal player was reopened at the main menu after the audit; [normal startup log](Validation/Alpha-v0.0.5/normal-startup.log) has no managed startup or shader exceptions.

Preservation: index SHA256 00024D9632812793FAFCE6418D7392644E0ABEB315014E843AE1A0740F416583; matches baseline true. Earlier changelog prefix intact true; earlier Chronicle prefix intact true.
