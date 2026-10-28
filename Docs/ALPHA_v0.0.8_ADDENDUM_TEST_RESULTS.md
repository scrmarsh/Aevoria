# Alpha v0.0.8 — continued restoration validation

October 23, 2026. **Development build; Sean’s final visual acceptance remains open.** The [implementation record](ALPHA_v0.0.8_ADDENDUM.md) expands the same milestone. The initial generator-6 [report](ALPHA_v0.0.8_TEST_RESULTS.md) and all earlier evidence remain historical records.

## Delivery and compatibility

Windows x64: [Aevoria.exe](../Builds/Alpha-v0.0.8-Expanded/Aevoria.exe). Application 0.0.8; generator 7, geology 3, hydrology 5, climate 3, archive 5. The theoretical mountain control remains 16,000 m. Earlier worlds require their preserved matching build; no creator archive was edited or silently regenerated. Original generator-6 v0.0.8 remains in its separate build folder.

Final build UTC: 2026-10-23T23:24:18.6437479Z. [Final build/native provenance](Validation/Alpha-v0.0.8/Addendum/Horizon-final/audit-launch.json). Delivered World SHA-256: F599B636472A2A9027223A85FD584CE66AC88C86F1FECD18E1C1DF8AF0345248; Presentation: 19401D1F8313F4E00D2B69609B07104C85C63078E8E738E9EE086DD6E4E8FB2F.

The complete five-size audit and benchmark below ran on the projection-corrected build from 2026-10-23T21:38:27.8259846Z, Presentation SHA-256 381A9EA18FE3238F8ED7115009DB65D388D125F75B31369BFEBF64AB1B7E547C. The final build adds two scenery corrections: Explore ignores the disabled God camera's retained orbit distance, and the distant scenery pass runs after the skybox while retaining depth testing and writing. This avoids the observed skybox overwrite of distant geometry. Its other runtime source delta is an opt-in audit branch. [Source manifests](Validation/Alpha-v0.0.8/Addendum/build-source-manifest.json) are compared against the [full-run snapshot](Validation/Alpha-v0.0.8/Addendum/build-source-manifest-full-run.json); other product sources, other shaders and history assets remain identical. Rebuilding in a fresh Unity cache changed the World DLL hash despite unchanged source: [compiled comparison](Validation/Alpha-v0.0.8/Addendum/world-assembly-comparison.json.log) found no differences in 334 method bodies, type/field/member definitions and signatures (1,026 entries), and [the native Miniature fingerprint](Validation/Alpha-v0.0.8/Addendum/world-native-equivalence.json) matches exactly. These checks establish the recorded comparison, not a claim that the DLL files are byte-identical. A targeted native overview-to-first/third-person check, four cardinal captures, cleanup check and repeated real traveler/flight Play Mode test validate the final product delta. The full benchmark is retained under its own exact binary hash rather than relabeled as a rerun of the final horizon build.

## Executed checks

| Suite | Passed | Evidence |
| --- | --- | --- |
| Full Editor | 87/87 | [XML](Validation/Alpha-v0.0.8/Addendum/editor-delivery.xml) |
| Final projection follow-up | 9/9 | [XML](Validation/Alpha-v0.0.8/Addendum/editor-precision.xml) |
| Repeated full Play Mode | 10/10 | [XML](Validation/Alpha-v0.0.8/Addendum/play-publication.xml) |
| Projection flight/preload follow-up | 2/2 | [XML](Validation/Alpha-v0.0.8/Addendum/play-precision.xml) |
| Final horizon/flight follow-up | 1/1 | [XML](Validation/Alpha-v0.0.8/Addendum/play-horizon.xml) |
| Portable | 115 | [Log](Validation/Alpha-v0.0.8/Addendum/portable-delivery.log) |
| Repeated planetary configurations | 10 | [Models and fingerprints](Validation/Alpha-v0.0.8/Addendum/Models/geography.csv) |
| Ecology checkpoints | 24 | [1/10/100/1000-year populations](Validation/Alpha-v0.0.8/Addendum/Retained-ecology/ecology-long-run.csv) |

The full Editor suite and final projection follow-up cover **88 distinct cases**. The full suite and full Play Mode run precede the final clip corrections; nine focused Editor checks, two flight/preload Play Mode checks and the new complete standalone run follow them. World-generation source is unchanged by those camera corrections. Planetary cases cover two seeds across all five sizes with matching repeat fingerprints, finite fields and no dry channel terminals. The Archipelago regression retains at least three substantial islands. All four simulated populations persist in six long-run cases. The ridge-axis regression first failed on the previous native candidate with an exactly reversed direction (dot product -1). The clip regression first failed with a 5,734,400 m ground-view far plane. Tightening far clipping alone retained warnings at Colossal coordinates; a position-aware near-plane guard then passed the complete Colossal diagnostic without warnings. The short forest-motion A/B probe did not reproduce the warning in either mode and is not presented as proof of causation. Intermediate package, Windows cache-path, corrupt-cache exception and island-domain failures remain under the addendum directory; their failed results are not included as passes.

## Native standalone behavior and visual evidence

[Sixteen-theme gallery](Validation/Alpha-v0.0.8/Addendum/Comparison.html), [native audit](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/audit.json), [expanded checks](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/addendum.json), [camera/settings metadata](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/views.json).

All five sizes created and reloaded with mutable continuation intact. The save-error message visible in some Medium captures is deliberately injected by the invalid-inventory test; the inventory is restored immediately and the later valid save/reload succeeds. Generation cancellation, presentation cleanup, exploration and completed solid excavation passed. Installed preload completion, preload cancellation, logarithmic zoom, independent display controls, double-Space flight, airborne archive round-trip, rejected invalid inventory preserving the previous archive byte-for-byte, and a creator-case save above 10,000 m passed. The actual creator configuration has a 11934.37 m model peak; its last valid user archive cannot reveal the unknown transient failed pose.

Zoom recorded 3600 continuous samples, with minimum installed/authoritative clearance **3.00 m** and near clipping below clearance. Alternating rapid wheel checks also passed. The player executed 32576 publication/settling safety checks before rendering. Cache hits accumulated: 1207. The five installed-preload re-request times are 0.22, 0.19, 0.20, 0.22, 0.23 s (size order Miniature through Colossal). Those requests occur after High Quality readiness, so they measure warm re-publication, not a cold first load. The saved high-mountain traveler elevation is 11938.37 m. Rejected-save hashes before and after: D49270B59FD7A1D7EA63F5CF5A77FDDCBC4C583023B05C6E136917AE721E9C93 / D49270B59FD7A1D7EA63F5CF5A77FDDCBC4C583023B05C6E136917AE721E9C93. Peak submitted tree count: 76025. Maximum observed requested-mesh readiness: 16.00 s; this is the harness’s settled-resource criterion, not an input-latency measure or a promise of maximum detail at every pixel.

| Size | Width km | Create to active s | Selected forest settle s | Preloaded submitted trees | Warm preload re-publication s |
| --- | --- | --- | --- | --- | --- |
| Miniature | 128 | 74.06 | 3.45 | 2476 | 0.22 |
| Small | 256 | 52.90 | 1.72 | 3049 | 0.19 |
| Medium | 512 | 57.79 | 2.73 | 27702 | 0.20 |
| Large | 1024 | 75.25 | 2.40 | 21201 | 0.22 |
| Colossal | 2048 | 39.44 | 2.68 | 18700 | 0.23 |

## Frame pacing and resources

Hardware: Intel(R) Core(TM) i7-8700K CPU @ 3.70GHz; NVIDIA GeForce RTX 3060 Ti. **39075 measured engine-loop intervals** each have a matching main-camera render callback at 2560 × 1440; **2152 exceed 16.667 ms**. These are Unity engine-loop intervals, not an external display-present capture. CPU aggregates may include parallel work; unavailable GPU timing must remain N/A. Process/WDDM maxima: workingSetMiB: 5621.9 MiB; privateMiB: 6502.2 MiB; dedicatedGpuMiB: 856.3 MiB; sharedGpuMiB: 294.3 MiB. WDDM allocations are process counters, not whole-board residency guarantees. Terrain triangle metadata excludes water and vegetation; the scenery triangle budget is included. Cached-data RAM limits exclude active meshes, native copies, world models and GPU allocations.

| Scenario | Frames | Mean ms | P95 ms | P99 ms | Max ms | >16.667 ms | Aggregate CPU ms/frame | GPU mean ms |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Miniature-overview | 714 | 6.99 | 7.79 | 15.46 | 20.88 | 2 | 4.38 | 6.60 |
| Miniature-forest | 822 | 6.08 | 7.02 | 8.08 | 23.69 | 5 | 8.72 | 5.79 |
| Small-overview | 742 | 6.73 | 7.67 | 9.36 | 16.48 | 0 | 4.59 | 6.47 |
| Small-forest | 694 | 7.20 | 7.89 | 8.97 | 9.87 | 0 | 7.97 | 6.99 |
| Medium-overview | 734 | 6.82 | 7.72 | 14.81 | 20.36 | 2 | 4.79 | 6.44 |
| Medium-forest | 552 | 9.07 | 10.17 | 12.61 | 13.21 | 0 | 13.98 | 8.81 |
| ocean-near-far-transition | 1639 | 4.88 | 7.14 | 8.20 | 11.80 | 0 | 9.09 | 4.63 |
| experimental-excavation-install | 1171 | 5.13 | 6.49 | 7.72 | 25.51 | 6 | 7.42 | 4.84 |
| coastal-streaming | 1845 | 6.51 | 7.62 | 9.78 | 22.97 | 6 | 20.16 | 6.22 |
| high-quality-forest | 1122 | 17.83 | 18.86 | 19.85 | 20.71 | 1120 | 26.61 | 17.69 |
| high-quality-mountains | 948 | 21.10 | 25.22 | 26.15 | 28.11 | 891 | 40.53 | 20.95 |
| rapid-zoom | 829 | 9.66 | 16.78 | 20.81 | 28.02 | 42 | 18.68 | 9.32 |
| regional-rain | 723 | 13.83 | 14.54 | 15.04 | 15.35 | 0 | 18.80 | 13.69 |
| Medium-running-simulation | 2181 | 13.76 | 14.55 | 15.08 | 18.36 | 1 | 17.85 | 13.61 |
| Large-overview | 692 | 7.23 | 7.75 | 23.42 | 29.73 | 17 | 5.08 | 6.61 |
| Large-forest | 563 | 8.89 | 9.64 | 11.68 | 13.64 | 0 | 11.35 | 8.67 |
| Colossal-overview | 770 | 6.51 | 7.30 | 7.93 | 15.62 | 0 | 5.19 | 6.31 |
| Colossal-forest | 680 | 7.36 | 8.26 | 9.99 | 11.63 | 0 | 10.11 | 7.11 |
| Colossal-moving | 20494 | 8.78 | 11.27 | 13.42 | 21.73 | 18 | 17.47 | 8.40 |
| Colossal-high | 1160 | 12.94 | 15.40 | 20.46 | 28.31 | 42 | 38.48 | 12.46 |

[Frame summary](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/summary.csv), [all outliers](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/outliers.csv), [process samples](Validation/Alpha-v0.0.8/Addendum/Rendered-delivery/process-memory.csv). Generation and blocking readiness windows are outside timed traversal segments and are reported separately in audit/views metadata. No universal hitch-free or 60 FPS guarantee is asserted.

## Practical limits and remaining acceptance

This is a bounded procedural alpha renderer. Geological/hydrological rasters, distant coast and pond triangulation, procedural materials, vegetation models, cloud art and scenery remain finite approximations. Some river surfaces remain faint against pale beds or coarsely sampled terrain; curved drainage and downhill tests do not establish complete visual water coverage. Some high-relief silhouettes remain stylized. Cloud volumes ray march a bounded set of formations and use approximate projected shadows; they are not a physical meteorology solver. Distant mountains are decorative, non-colliding scenery. The whole planetary surface is not resident at metre detail. Disk cache storage currently has no global pruning budget. This evidence supports the recorded fixes and behavior, not a claim that every reference-image gap, shoreline/LOD artifact, physical-input or presented-frame acceptance criterion is closed. Sean’s comparison of the unchanged native captures remains part of acceptance.

The original defect PNGs, diagnostic shader modes, failed/intermediate candidates and matching earlier player snapshots remain available. Changelog prose and previous Chronicle books were preserved; Book VIII was extended once, without a duplicate chapter. No civilization scope was added.
