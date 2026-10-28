# Alpha v0.0.7 — The Shaping of Worlds: validation

October 22, 2026. **Development build; full acceptance remains open.** Automated consistency and lifecycle gates passed. The visual review still rejects reference-level continental/mountain fidelity and fully resolved inland-water shorelines. This report distinguishes those outcomes. No public release is declared.

Windows x64: **M:\Aevoria\Builds\Alpha-v0.0.7\Aevoria.exe**. [Build metadata](../Builds/Alpha-v0.0.7/build-info.json). Unity 6000.3.27f1 / URP 17.3.0. Application 0.0.7, geography 5 and archive 5 are independent identities; ecology, climate, hydrology and forecast are 2, living/seasonal state 2, integrated atmospheric state 1. Calendar/settings schema remain 1. Old save directories remain intact and require their matching historical build.

## Implemented algorithms and practical bounds

The [architecture](PLANETARY_WORLD.md) documents seeded continental lobe chains and ocean gaps; jittered plate-motion stress and connected uplift belts; conservative thermal transport and regional incision/deposition proxies; latitude/lapse/ocean-distance climate with windward rain and leeward drying; biome eligibility; rainfall-weighted priority-flood drainage; refined connected one-level lakes; rejected-basin breaching; continuous outlet paths and shared lake/river ownership. These are deterministic practical approximations, not full geological, hydraulic or meteorological simulations.

Five exact 128–2,048 km presets share bounded 257-square geology/drainage, 129-square climate and at most 64-square ecological/weather memory. Plate count and physical scales change with size. Rendering combines the planetary overview, one regional clipmap and local 8 m chunks. Coverage is a moving 64-square atlas. Inland water uses bounded spatial batches for culling. No full-resolution Colossal world allocation is made.

Saved daily wetness and snow/ice use the same regional forecast as precipitation and clouds. The ocean uses continuous bathymetry with nearby scene-depth absorption. Fog has independent controls and immediate analytic altitude attenuation. Escape centralizes actions; HUD retains time, coordinates and optional independent performance counters. F3/F4 remain optional. Map, minimap, combined movement, F11, ecology, tools, inventory, first/third-person exploration and bounded solid excavation are retained. The traveler-return action now lives in Escape.

## Executed automated checks

| Check | Result | Evidence |
| --- | --- | --- |
| Portable suite | 111 PASS | [Log](Validation/Alpha-v0.0.7/portable.log) |
| Unity EditMode | 63/63 passed, including nine new planetary tests | [XML](Validation/Alpha-v0.0.7/phase1-edit.xml) |
| Unity PlayMode | 7/7 passed | [XML](Validation/Alpha-v0.0.7/phase1-play.xml) |
| Two seeds × five sizes, repeated construction | All ten fingerprints repeated exactly; parallel reverse-location samples matched; finite fields; downhill receivers; zero dry river endings | [Model log](Validation/Alpha-v0.0.7/planetary.log), [statistics](Validation/Alpha-v0.0.7/Models/geography.csv) |
| Retained ecology evolution | Two seeds × three sizes through 1, 10, 100 and 1,000 years; all four groups finite and persistent | [CSV](Validation/Alpha-v0.0.7/Retained-ecology/ecology-long-run.csv) |
| Visible player | Five creations/reloads; exact ecological/living continuation, cancellation and cleanup; exploration and excavation true | [Audit](Validation/Alpha-v0.0.7/Rendered-polished/audit.json) |
| Weather continuation | Rain wets ground, eight clear days dry it gradually, full living snapshot survives save/load | [Separate weather proof](Validation/Alpha-v0.0.7/Weather-proof/audit.json) |
| Final Windows build | Succeeded; validation rejects shader/runtime errors and serialization assembly warnings | [Build log](Validation/Alpha-v0.0.7/phase1-build-final.log) |

The tests were retained with explicit new contracts: generator 5 and new preset dimensions; 16,000 m relief setting bounds; three base terrain meshes; precipitation-dependent daily snow; incompatible old archives rejected without modification. Sea-level tests now preserve the continental skeleton rather than requiring identical final carved/dune heights, because sea level affects climate and outlet incision in this generator. Tree-placement tests sample the whole world instead of assuming every seed has all forest variants near its origin. New tests cover ordered progress, cancellation, bounded hierarchy, climate gradients, actual downstream outlets, regional cloud/rain coherence, saved wetness cadence and invalid state/settings. The final shading/menu-only polish was compiled and exercised again in the rendered run. These execution results do not establish physical-input acceptance.

Earlier failures are retained under [Regressions](Validation/Alpha-v0.0.7/Regressions), [initial smoke](Validation/Alpha-v0.0.7/Smoke-initial) and [rejected harness run](Validation/Alpha-v0.0.7/Smoke-refined). Fixes include canonical migration-edge sampling with both endpoints, Unity fake-null component handling, regeneration's explicit chunk limit, and isolated audit registration.

## Measured generation times

Visible native player, seed shaping-continents with explicit Continents, default climate and 3,600 m relief. These seconds include global construction, ecology, initial archive write and initial presentation readiness; no hidden timer simulates progress. Single observations on this machine, not statistical guarantees.

| Preset | Dimensions | Native ready seconds |
| --- | --- | ---: |
| Miniature | 128 × 128 km | 9.26 |
| Small | 256 × 256 km | 9.90 |
| Medium | 512 × 512 km | 8.39 |
| Large | 1024 × 1024 km | 8.74 |
| Colossal | 2048 × 2048 km | 6.98 |

Model-only .NET timings below are different workloads and were collected during development with other validation activity; they are not native generation benchmarks. All rows use a 129-square sampling raster.

| Seed / size | Land | Components / largest share | Biomes | Temperature C | Height m | Accepted lakes | Dry endings |
| --- | ---: | --- | ---: | --- | --- | ---: | ---: |
| aevoria-007 / Miniature | 51.8% | 4 / 43.4% | 13 | -1 to 29 | -4689 to 4329 | 106 | 0 |
| aevoria-007 / Small | 50.8% | 6 / 29.4% | 13 | -8 to 30 | -4643 to 4284 | 208 | 0 |
| aevoria-007 / Medium | 50.7% | 6 / 36.8% | 15 | -24 to 29 | -4983 to 4257 | 287 | 0 |
| aevoria-007 / Large | 50.3% | 17 / 31.8% | 15 | -36 to 29 | -4998 to 4033 | 283 | 0 |
| aevoria-007 / Colossal | 51.6% | 9 / 99.0% | 15 | -39 to 30 | -5009 to 4332 | 235 | 0 |
| shaping-continents / Miniature | 52.5% | 1 / 100.0% | 13 | -0 to 30 | -4935 to 4012 | 88 | 0 |
| shaping-continents / Small | 52.5% | 1 / 100.0% | 12 | -6 to 30 | -4556 to 3651 | 137 | 0 |
| shaping-continents / Medium | 52.4% | 1 / 100.0% | 15 | -28 to 29 | -4957 to 4326 | 176 | 0 |
| shaping-continents / Large | 52.6% | 1 / 100.0% | 15 | -42 to 30 | -4877 to 4325 | 238 | 0 |
| shaping-continents / Colossal | 52.4% | 2 / 100.0% | 15 | -35 to 29 | -4883 to 4378 | 183 | 0 |

The CSV also retains connected mountain-cell components, biome patch sizes, climate standard deviations, lake-area distributions and fingerprints. These are proxies, not proof of geological realism. The median biome patch is one sampled cell: small fragments remain despite broad major zones. The Colossal archipelago sample is dominated by one land component; layout labels do not guarantee an ideal artistic topology. The 16,000 m control is a validated theoretical relief limit; the native default-setting captures do not prove a 16 km summit.

## Actual 2560 × 1440 rendered measurements

Intel(R) Core(TM) i7-8700K CPU @ 3.70GHz; NVIDIA GeForce RTX 3060 Ti; target host 64 GB RAM. 17 windows, **83,503 measured frames**, with a corresponding MainCamera render callback for each counted frame. VSync off and unlimited frame cap during measurement. These are Unity engine-loop intervals with rendered-camera confirmation, **not independently measured display/presented FPS**. No Unity compiler, portable long-run suite or other intentional heavy validation ran concurrently. The light process-memory sampler ran throughout. Normal player defaults retain their configured cap.

| Scenario | Frames | Mean ms | p95 ms | p99 ms | Max ms | >16.667 ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Miniature-overview | 1413 | 3.54 | 3.81 | 5.15 | 10.38 | 0 |
| Miniature-forest | 921 | 5.43 | 5.65 | 7.16 | 7.85 | 0 |
| Small-overview | 1410 | 3.55 | 3.86 | 5.04 | 7.24 | 0 |
| Small-forest | 1549 | 3.23 | 3.86 | 5.03 | 5.81 | 0 |
| Medium-overview | 1377 | 3.63 | 3.89 | 5.09 | 8.84 | 0 |
| Medium-forest | 487 | 10.27 | 10.52 | 10.74 | 11.70 | 0 |
| ocean-near-far-transition | 2241 | 3.57 | 5.16 | 5.88 | 8.52 | 0 |
| experimental-excavation-install | 2228 | 2.69 | 2.94 | 5.29 | 64.66 | 1 |
| rapid-zoom | 1721 | 4.65 | 10.29 | 11.03 | 12.34 | 0 |
| regional-rain | 2810 | 3.56 | 3.81 | 4.04 | 8.58 | 0 |
| Medium-running-simulation | 8390 | 3.58 | 3.84 | 4.38 | 11.35 | 0 |
| Large-overview | 1381 | 3.62 | 3.88 | 5.11 | 7.45 | 0 |
| Large-forest | 1296 | 3.86 | 4.63 | 5.84 | 6.48 | 0 |
| Colossal-overview | 1385 | 3.61 | 3.88 | 5.22 | 10.65 | 0 |
| Colossal-forest | 896 | 5.58 | 5.85 | 8.10 | 11.93 | 0 |
| Colossal-moving | 50565 | 3.56 | 5.44 | 6.56 | 12.15 | 0 |
| Colossal-high | 3433 | 4.37 | 5.94 | 7.40 | 16.50 | 0 |

**1 frames exceeded 16.667 ms; none were removed.** [Every outlier](Validation/Alpha-v0.0.7/Rendered-polished/outliers.csv), [summary](Validation/Alpha-v0.0.7/Rendered-polished/summary.csv), [raw frame traces and counter availability](Validation/Alpha-v0.0.7/Rendered-polished). Aggregate process CPU time per frame and GPU FrameTimingManager values are retained in audit.json; aggregate CPU time is not main-thread wall time.

Peak sampled process/counter values (MiB): workingSetMiB 2034.3; privateMiB 2522.8; dedicatedGpuMiB 532.3; sharedGpuMiB 260.7. [Samples](Validation/Alpha-v0.0.7/Rendered-polished/process-memory.csv). WDDM per-process GPU memory counters are not an independent board-wide VRAM measurement. Managed and Unity allocation snapshots are in each scenario. Current-thread and frame allocation counters failed availability/self-test and are **N/A/-1**, not zero. GC/present markers can lag; no universal cause is assigned to outliers from correlation alone.

The experimental excavation scenario remains a frame-pacing risk. The earlier v0.0.6 92.15 ms hitch was reviewed and remains historical evidence; a smaller maximum in this run is not a controlled proof of a fix. High-quality moving captures include ocean and coast, so that window does not establish heavy high-quality mountain/forest performance. The three-minute Colossal path is a looping navigation trajectory around a coastal region, not a traversal of the whole planet. Generation, loading, screenshot encoding and menu transitions are outside the gameplay measurement windows. Hour-scale soaks, all-biome high-quality stress and externally presented-frame acceptance remain open.

## Regional wetness proof

The first full run's drying label was misleading: more rain occurred between the rain sample and the later clear interval, so comparing those two values could not prove drying. Its original evidence remains untouched. A corrected isolated run searched a continuous clear forecast window and sampled wetness at its actual start and eight days later. Rain: **0.000 → 0.886**. Clear interval: **0.991 → 0.931**, remaining above zero. The living snapshot hash survived save/load exactly. [Clear-interval start](Validation/Alpha-v0.0.7/Weather-proof/23-drying-start.png), [eight-day result](Validation/Alpha-v0.0.7/Weather-proof/23-drying-ground.png). Model correctness does not establish convincing cloud art or perceptual wetness contrast in every material.

## Visual review and remaining acceptance

The final run retains 62 actual player PNGs. Filename labels describe intended inspection sites; a label alone is not a pass. The earlier [review sheets](Validation/Alpha-v0.0.7/ReviewSheets) retain the defects that prompted the final forest-floor, bank-color and label polish. Representative final images were inspected directly.

| Requested view | Evidence | Finding |
| --- | --- | --- |
| 1. Medium overview | [03-Medium-overview](Validation/Alpha-v0.0.7/Rendered-polished/03-Medium-overview.png) | Limited: broad zones and multiple land bodies; outer envelope and long straits still look constructed. |
| 2. Colossal overview | [03-Colossal-overview](Validation/Alpha-v0.0.7/Rendered-polished/03-Colossal-overview.png) | Limited: bounded construction; default 3,600 m relief reads softly at this planetary scale. |
| 3. Continental coasts | [12-mouth](Validation/Alpha-v0.0.7/Rendered-polished/12-mouth.png) | Limited: no former rectangular deep-ocean color fallback; coastal geometry remains coarse in places. |
| 4. Major mountain system overhead | [06-Medium-mountain-system](Validation/Alpha-v0.0.7/Rendered-polished/06-Medium-mountain-system.png) | Limited: connected uplift/snow belt, below reference-level rugged silhouette. |
| 5. Ground foothills | [29-ground-foothills](Validation/Alpha-v0.0.7/Rendered-polished/29-ground-foothills.png) | Ground exploration works; selected safe spawn is plain-like and does not establish dramatic foothill composition. |
| 6. Desert dunes | [08-Desert](Validation/Alpha-v0.0.7/Rendered-polished/08-Desert.png) | Golden dry terrain and actual dune height exist; broad dune appearance remains weak. |
| 7. Forest | [05-Medium-forest](Validation/Alpha-v0.0.7/Rendered-polished/05-Medium-forest.png) | Coherent stands present; final shading removes the old automatic gray floor. Tree art remains simplified. |
| 8. Grassland | [07-grassland](Validation/Alpha-v0.0.7/Rendered-polished/07-grassland.png) | Open green ground with low tree eligibility; surface variation remains repetitive. |
| 9. Tundra | [08-Tundra](Validation/Alpha-v0.0.7/Rendered-polished/08-Tundra.png) | Cold region eligibility checked; visual distinction from grassland is incomplete. |
| 10. Alpine snow | [08-Alpine](Validation/Alpha-v0.0.7/Rendered-polished/08-Alpine.png) | Snow-bearing high ground present; patch/filter artifacts and terrain silhouette need acceptance. |
| 11. River source | [11-source](Validation/Alpha-v0.0.7/Rendered-polished/11-source.png) | Shared channel authority; distant geometry may hide or flatten narrow water. |
| 12. Confluence | [13-confluence](Validation/Alpha-v0.0.7/Rendered-polished/13-confluence.png) | Model accumulation and continuation pass; visual branch/bank joins remain limited. |
| 13. River entering lake | [24-river-lake-entry](Validation/Alpha-v0.0.7/Rendered-polished/24-river-lake-entry.png) | Ownership passes; block-like shore/bank appearance remains an open visual gate. |
| 14. Lake shoreline | [09-lake-shore](Validation/Alpha-v0.0.7/Rendered-polished/09-lake-shore.png) | One-level transparent water; refined footprint still visibly polygonal in other lake captures. |
| 15. Lake outlet | [25-lake-outlet](Validation/Alpha-v0.0.7/Rendered-polished/25-lake-outlet.png) | Downstream continuation passes; shoreline raster and coarse-terrain disagreement remain visible. |
| 16. River to ocean | [12-mouth](Validation/Alpha-v0.0.7/Rendered-polished/12-mouth.png) | Model reaches sea; mouth geometry/color transition needs further work. |
| 17. Tropical shallows | [26-tropical-shallows](Validation/Alpha-v0.0.7/Rendered-polished/26-tropical-shallows.png) | Warm-shallow eligibility implemented; this view does not prove reference-level clear turquoise water. |
| 18. Deep ocean | [27-deep-ocean](Validation/Alpha-v0.0.7/Rendered-polished/27-deep-ocean.png) | Dark navy ocean; waves are shader ripples, not physical wave geometry. |
| 19. Ocean near/far transition | [26-tropical-shallows](Validation/Alpha-v0.0.7/Rendered-polished/26-tropical-shallows.png) | Rendered zoom trace retained; inspected views show no former rectangular color step, full transition acceptance open. |
| 20. Clear weather | [28-clear-weather](Validation/Alpha-v0.0.7/Rendered-polished/28-clear-weather.png) | Dry forecast location captured; clear-sky appearance remains basic. |
| 21. Approaching storm | [22-approaching-storm](Validation/Alpha-v0.0.7/Rendered-polished/22-approaching-storm.png) | Forecast/front state changes; downward camera can sit below the cloud deck, so this is not convincing storm-approach proof. |
| 22. Cloud shadow | [22-rain-cloud-shadow-wetness](Validation/Alpha-v0.0.7/Rendered-polished/22-rain-cloud-shadow-wetness.png) | Regional projected attenuation implemented; photographic/volumetric cloud realism is incomplete. |
| 23. Wet terrain | [22-rain-cloud-shadow-wetness](Validation/Alpha-v0.0.7/Rendered-polished/22-rain-cloud-shadow-wetness.png) | Saved wetness increases; material contrast needs visual acceptance. |
| 24. Drying after rain | [23-drying-ground](Validation/Alpha-v0.0.7/Rendered-polished/23-drying-ground.png) | Corrected continuous-clear-window proof is linked above; gradual model drying passes. |
| 25. Fog on/off | [18-fog-off](Validation/Alpha-v0.0.7/Rendered-polished/18-fog-off.png) | Independent toggle works with explicit fog/haze zero when off; compare 19-fog-on. |
| 26. Rapid altitude zoom | [19-fog-on](Validation/Alpha-v0.0.7/Rendered-polished/19-fog-on.png) | Rapid-zoom frames measured; analytic density removes old altitude lag, but no full-frame video proves absence of every flash. |
| 27. Terrain/detail streaming | [21-high-quality](Validation/Alpha-v0.0.7/Rendered-polished/21-high-quality.png) | Bounded ownership passes; high-quality view is ocean-dominated and does not close mountain-transition acceptance. |
| 28. F3 | [14-f3](Validation/Alpha-v0.0.7/Rendered-polished/14-f3.png) | Optional actual diagnostics; unsupported counters show N/A. |
| 29. F4 biome/temperature | [15-f4-4](Validation/Alpha-v0.0.7/Rendered-polished/15-f4-4.png) | Actual blue-to-red temperature and biome/moisture legends captured; biome colors in 15-f4-3. |
| 30. HUD / Escape | [16-pause](Validation/Alpha-v0.0.7/Rendered-polished/16-pause.png) | Compact HUD and structured actions captured; physical clicks/key handling still need Sean’s acceptance. |

## Explicitly incomplete or deferred

Reference-level continental outlines, mountain silhouettes, broad biome art, lake/coast edge fidelity, narrow distant-channel presentation, convincing storms and ground-level composition remain **limited**, not accepted. Pixel/triangle patterns, coarse geometry transitions and simplified procedural trees remain visible. The all-world biome median patch and archipelago statistics above prevent a claim of uniformly coherent geography. The largest terrain/river analysis cells are kilometres wide; the regional clipmap improves presentation without becoming a full fine regional drainage solver.

Full plate evolution, water/sediment mass balance, salt/fresh/endoreic/inland-sea water typing, physical waves/tides, volumetric atmospheric fluid simulation, marine populations/gameplay, unrestricted underground traversal, production character animation and all civilization/history gameplay remain deferred. Aquatic habitat classifications and master volume are foundations; no marine foodweb or audio library is claimed. Geography is finite and planar with no spherical wraparound or floating-origin system.

Required manual acceptance: physical WASD/middle drag/edge-scroll combinations and camera ergonomics; Escape and inventory conflicts; F11, alt-tab, multi-monitor and 15-second display rollback; text entry/scrolling; first/third-person walking, jump, crouch, sprint and tools; color/depth and terrain transitions at several seeds; wetness/cloud appearance; and long-session frame pacing. The app launches at its Home menu for review.

Earlier Chronicle prose and changelog entries were preserved before appending Book VII and the new development record. The narrator remains the unnamed They. Git staging was not changed; no commit or public release was invented. Aevoria-old was not modified.
