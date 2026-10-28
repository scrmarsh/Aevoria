# Aevoria Alpha v0.0.4 — implementation and validation record

Recorded October 22, 2026, America/Chicago. **Development build; implementation and acceptance remain distinct. No release is declared.** Unity 6000.3.27f1 and URP 17.3.0 are retained. Application 0.0.4, generator 4 and archive schema 2 have separate identities.

## Audit and preservation

The existing Alpha v0.0.3 implementation, validation report, history, roadmap, simulation, persistence scaffold, terrain/shaders/camera, tests and benchmark routes were audited before edits. Pre-update source, project settings, packages, docs, tests and tools were copied into ignored Temp/Alpha004Baseline. Its index hash records the existing staged state. No commit, reset or staging operation was used. The repository still has no commit ID to report. Aevoria-old was not modified or used as a terrain source. Earlier reports, builds, screenshots, traces and reference images remain historical.

Sean explicitly requested the next milestone and a theoretical mountain relief ceiling of four times the previous 800 m control: **3,200 m**. Both creation and the retained developer control use a common validated limit. Relief adds to the underlying landform; it is not an absolute summit-altitude cap. His preference for the changelog's wording and style overrides conflicting editorial instructions. Earlier entries remain intact, with a new entry appended in the same format.

## Implemented, changed and fixed

| Area | Current implementation | Practical limit |
| --- | --- | --- |
| Application | Normal startup opens Home without generating terrain. Create, Load, actual Recents, Settings, Changelog, Chronicles, About and Quit are connected. The backdrop is a static earlier world capture; it needs no live world. A compact world bar supplies time/pause/save/menu; Escape opens the paused menu, F3 toggles existing diagnostics. | Automated navigation invokes real controllers, not physical mouse/keyboard actions. |
| World management | Validated names/seeds, random seeds, three size presets, relief/roughness/sea/land and climate options. Initial archive is written before world entry; loading stays visible until initial terrain is ready. GUID identities permit duplicate names. Search, sort, details, rename and confirmed deletion operate on real archive metadata. | Presets retain 16.384/32.768/65.536 km dimensions. No arbitrary-dimension editor or thumbnail browser is claimed. |
| Persistent state | Checksummed XML archives preserve identity, dates, seed/configuration, generation/environment versions, fingerprint, tick, food/population prototype, RNG, pause/speed/backlog and derived calendar. Immutable geography is reconstructed only under matching identities and fingerprint. | No earlier complete world format existed. Legacy schema-1 SaveEnvelope remains a scaffold, not a full migration source. |
| Safe transactions | Worker serialization and disk writes are serialized through one repository. Flush + atomic replace retains a previous complete backup. Interrupted temp files are ignored. Recovery preserves the damaged primary; Save As gives recovery a fresh identity. Future/unsupported saves reject safely. | World and metadata are separate transactions: a crash between them can leave stale metadata. Authoritative payload is validated on load. There is one previous complete backup, not a retention history. |
| Settings | Separate validated settings file; Apply/Cancel/Defaults, quality, directional shadows, FXAA, VSync/cap, blur, camera options and autosave. Display changes have Keep/Revert and 15-second rollback. | Physical display rollback/alt-tab and complete ergonomic acceptance remain pending. No unconnected audio controls are shown. |
| Square-water diagnosis | The continental field used max(abs(normalized x), abs(normalized z)), a Chebyshev square. Independent outer scenery also switched terrain algorithms at the finite rectangle, creating straight decorative land edges. Generator 4 uses a warped radial falloff, and outer scenery continues the same field across the simulation edge, with stronger radial falloff beyond the shaped coast. | Independent decorative islands are removed. Landforms may continue into explicitly non-simulated scenery. No fog mask or boundary-wall disguise is used. |
| Relief | Warped ridge belts gain more varied profiles and broader lowlands; culling bounds support higher relief. Calibrated land coverage and global-coordinate authority remain. | This is procedural noise/ridge geography, not tectonic or erosion simulation. Reference-level silhouettes are not achieved everywhere. |
| Drainage | Priority-flood receivers, deterministic flat ordering, flow accumulation, basin IDs/water levels and spill routes on one 129×129 world grid. Downstream width grows with accumulation; curved paths and carving share the same authority. Ocean level floods a fixed reference-datum landform. | Resolution is coarse, especially on Large. Endorheic/evaporative balance, physically integrated discharge and erosion remain unimplemented. |
| Inland surfaces | Aggregate river, clipped basin-lake and conditional steep-drop ribbon meshes; no object per river segment. River cross-sections transition into banks; placement excludes open lakes and river corridors. | Fine terrain LOD still approximates narrow channels. Confluences, lake edges, water/terrain intersections and steep-drop ribbons need further visual acceptance. A physical waterfall solver is not claimed. |
| Climate and biomes | Latitude/elevation temperature, prevailing wind moisture, ocean recharge, uplift and leeward drying; interpolated drainage moisture and fertility. Climate changes ground color/snow and tree eligibility/type. Biome labels include grassland, forest, boreal/tropical forest, savanna, shrubland, desert, tundra/alpine, wetland and snow. | Classification is a foundation, not vegetation ecology. All biomes are not promised in every default seed; configured warm/arid contrast is captured. |
| Calendar/weather | Four existing ticks/day, 360 days/year, four 90-day seasons. Deterministic local weather follows tick, world field, climate and season. Sky/light/fog and one pooled local rain/snow system supply feedback. | Local visual precipitation is not conserved watershed water, a planetary atmosphere or cloud-fluid simulation. |
| Developer flow | Existing benchmark flags remain; explicit direct startup and seed override work. The menu audit requires an isolated test root. Presentation/world services clean up on menu return and reuse one atmosphere service. | Resource counts and short probes do not prove every long-session leak or driver condition absent. |
| Development history | Canonical changelog and Chronicles export into runtime readers with version/chapter browsing. Book IV contains an in-progress ledger and preserves its earlier prophecy as an earlier record. About distinguishes current foundations from the long-term autonomous-history vision. | The development Chronicles remain separate from a future simulated-world History Book. |

No civilizations, settlements, kingdoms, politics, wars, dynasties or detailed historical characters were added. The original aggregate population/food test prototype remains because preserving simulation state requires preserving its existing data.

## Automated verification and persistence

- **47 portable checks pass**, extending the retained 24 checks. Evidence: [portable output](Validation/Alpha-v0.0.4/portable-results.txt).
- **32 EditMode tests pass**, extending the retained 24: [XML](Validation/Alpha-v0.0.4/edit-results.xml).
- **Five PlayMode tests pass**, retaining the four original streaming/regeneration checks and adding menu navigation/repeated world cleanup: [XML](Validation/Alpha-v0.0.4/play-results.xml).
- Chronicle IDs, anchors, themes, evidence and current-version association pass: [validator output](Validation/Alpha-v0.0.4/chronicles-validation.txt).
- Windows x64 standalone builds successfully. The final summary and app/engine/generator identity are retained in [build log](Validation/Alpha-v0.0.4/build.log) and [metadata](Validation/Alpha-v0.0.4/build-info.json).

Persistence checks cover exact clock/RNG/speed/pause/backlog restoration and subsequent deterministic continuation; an elapsed simulation year; disk identity/fingerprint round trips; identical-name isolation; empty libraries; interrupted temp writes; checksums; recovery without primary overwrite; refusal to overwrite damage; future-schema rejection without fallback; generator-3 rejection rather than silent v4 regeneration; metadata-only enumeration; independent settings serialization; deletion isolation; and path-traversal rejection. Drainage tests check receiver acyclicity, nonascending filled surfaces, accumulating flow, repeatable reconstruction, curved channel/terrain agreement and continuity of the geographic field across tested scenery boundaries. Existing sea-level flooding and mixed-LOD border tests remain.

The actual player audit creates real isolated archives, saves, unloads and reloads them, checking the saved tick before continuing. It captures real menu states, a default world and a separately configured 3,200 m warm/arid world. It refuses to run its write workflow without -aevoriaTestRoot. Probe archives stay under ignored Temp; they do not populate the player's normal library.

Exploratory captures under InitialAudit, FinalAudit, AcceptedAudit and LatestAudit document earlier iterations, including the removed outer scenery seam and an intermediate river mesh. They are retained as development evidence. **DeliveryAudit is the final rendered audit**, and FinalBenchmark contains the final broad direct-world suite. The term accepted here identifies usable measurement conditions, not milestone or physical acceptance.

## Remaining scope and acceptance

This is a working feature build with open gates, not an unconditional implementation-complete declaration. The [manual checklist](ALPHA_v0.0.4_MANUAL_ACCEPTANCE.md) covers physical controls, display rollback, long names, archive interaction, window close/save failure behavior, moving LOD appearance, seasonal/weather review and longer sessions. Scripted page navigation and synthetic camera tests do not establish those outcomes.

Detailed terrain retains 8 m base spacing; adaptive local shoreline refinement was not implemented. Procedural faceted vegetation remains below the supplied references. The earlier low-angle bright sliver, shadow/dither/fade transitions, skirts and every streaming silhouette require hands-on moving review. Smoother river paths do not remove coarse-grid drainage limitations or prove seamless water against every terrain LOD. Closed-basin water balance, detailed erosion and higher fidelity lake/river outlet geometry remain open. Steep-drop ribbons are conditional and need dedicated natural-waterfall capture/acceptance.

Normal UI settings persistence is implemented; serialization is tested. Full physical control, display fallback, autosave-failure and operating-system close behavior still need manual acceptance. Atomic filesystem tests inject damaged/interrupted files, not a physical power cut. The pinned engine/build is the tested reconstruction environment; fingerprints intentionally reject a changed reconstruction rather than promise cross-engine bitwise compatibility.

No presented-frame capture or live VRAM-usage measurement is available. Startup adapter capacity/budget is not live VRAM use. Process working-set reads that return zero are marked unavailable, not treated as zero memory. Allocation counters failing the positive-allocation self-test stay unavailable. A one-minute moving sample is a short soak; multi-hour stability remains unverified.

## Priorities before Alpha v0.0.5

1. Complete the physical/menu/display/save-failure checklist and close the carried-forward moving camera, coast, horizon and LOD acceptance gaps.
2. Improve local shoreline/channel geometry and confluence/outlet rendering; evaluate drainage resolution, closed-basin balance and rain-weighted discharge before extending ecological consequences.
3. Harden metadata recovery and backup UX, add explicit migration design when a real prior complete format needs support, and test autosave/close/failure cases under longer sessions.
4. Establish live memory/presentation monitoring and longer repeat-load/travel soaks. Any later Living Ecosystems milestone should begin with bounded, testable environmental interactions; civilizations remain outside this update.

[Technical changelog](../CHANGELOG.md) · [Book IV and preserved Chronicles](THE_CHRONICLES_OF_AEVORIA.md) · [Archive/menu contract](PERSISTENT_WORLDS.md) · [Roadmap](ROADMAP.md)

## Final rendered evidence and measurements

Final run metrics and screenshot links follow.

The earlier AcceptedAudit travel/soak outliers were both sample zero, immediately after recorder construction/CSV transitions; no later sample in either interval exceeded 16.67 ms. Scope and GC markers did not explain them. The final audit warms two frames after measurement setup so recorder construction is excluded from world timings. Earlier raw 30.69/53.86 ms samples remain preserved and are not silently removed from their traces. This is an instrumentation-boundary finding, not proof that every possible frame-pacing issue is solved.

Rain/snow and winter capture states use explicit visual-preview overrides to exercise feedback. They do not prove those exact conditions naturally occurred at that location/date; deterministic scheduled weather is separately tested. The 3,200 m setting also exposed exaggerated spires in the exploratory maximum-relief capture. The final generator widens ranges with relief above the unchanged 600 m default to preserve more comparable slopes; the final maximum-setting view is rechecked below.


### Final build and accepted workloads

Windows x64 build succeeds. See build.log for exact bytes/time and build-info.json for app/engine/generator identity. Rendered workloads used an i7-8700K and RTX 3060 Ti, VSync off, actual 2560 × 1440. Every audit sample rendered; direct probes rendered at least frame-count minus one. These are engine-loop metrics, not independently measured presentation. Fullscreen probes were visible; no compilation overlapped accepted intervals.

The final audit measured 6201.04 ms creation and 5924.06 ms load, including initial terrain readiness; save was 31.84 ms. Pure setup measured 13.73 ms calibration, 43.27 ms hydrology and 12.46 ms climate. The actual player reload retained tick 10.

| Audit workload | Frames | Mean ms | p95 ms | Raw max ms | GPU mean ms | Tracked Unity MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| menu-idle | 2507 | 1.20 | 1.47 | 2.83 | 0.93 | 104.88 |
| world-overview | 1112 | 3.60 | 3.85 | 4.22 | 3.37 | 150.37 |
| river | 1030 | 3.89 | 4.31 | 4.88 | 3.63 | 164.46 |
| lake | 1093 | 3.66 | 4.19 | 4.86 | 3.39 | 166.83 |
| biomes | 1095 | 3.65 | 3.95 | 4.35 | 3.43 | 156.73 |
| rain | 1075 | 3.72 | 3.98 | 4.26 | 3.50 | 156.83 |
| winter | 1077 | 3.72 | 3.98 | 4.41 | 3.49 | 156.83 |
| reloaded-world | 1335 | 3.00 | 3.27 | 4.51 | 2.76 | 163.04 |
| moving-world | 2429 | 4.12 | 5.13 | 5.72 | 3.81 | 162.24 |
| short-soak | 14969 | 4.01 | 5.33 | 6.30 | 3.71 | 162.61 |
| mountain-3200 | 1206 | 3.32 | 3.58 | 4.08 | 3.09 | 157.38 |
| menu-after-world | 2457 | 1.22 | 1.51 | 2.46 | 0.95 | 111.88 |

| Direct benchmark | Frames | Engine FPS | p95 ms | Raw max ms |
| --- | ---: | ---: | ---: | ---: |
| overview | 2254 | 281.51 | 3.80 | 5.53 |
| mountains | 2093 | 261.46 | 4.09 | 5.14 |
| grassland | 3025 | 377.93 | 2.88 | 4.32 |
| coast | 2146 | 268.08 | 3.99 | 4.54 |
| water | 2427 | 303.18 | 3.55 | 4.12 |
| horizon | 2025 | 252.94 | 4.21 | 4.87 |
| forest | 2650 | 331.09 | 3.29 | 4.88 |
| continuous | 4662 | 388.34 | 3.56 | 4.63 |
| rapid-zoom | 5657 | 353.42 | 3.91 | 5.48 |
| large-overview | 2409 | 300.99 | 3.57 | 4.17 |
| large-streaming | 3666 | 229.01 | 5.07 | 6.00 |
| blur-off | 2858 | 238.04 | 4.55 | 5.11 |
| blur-on | 2846 | 237.05 | 4.58 | 5.23 |
| low-angle | 2518 | 314.61 | 3.44 | 4.20 |

The two routes retain 72621 sampled frames and 32 actual 1440p screenshots. Timing scopes, outliers, counter availability and collections remain in each folder's CSVs and trace-analysis.json. Process working-set and live VRAM usage remain unavailable; tracked Unity memory is not total process or GPU memory. No zero-allocation claim is made. Prior measurements used different geography and trajectories, so these results do not establish an isolated speedup.

Menu return retained 0 terrain, 0 inland-water and 0 environment services. Tracked menu memory after world use was 111.88 MiB; loaded resources/engine pools need not return to startup byte counts. Repeated ownership tests and this short soak do not establish multi-hour stability.

### Screenshot review

Menus: [Home](Validation/Alpha-v0.0.4/DeliveryAudit/00-menu-empty.png), [Create](Validation/Alpha-v0.0.4/DeliveryAudit/01-Create.png), [Load](Validation/Alpha-v0.0.4/DeliveryAudit/16-load-real-save.png), [Recents](Validation/Alpha-v0.0.4/DeliveryAudit/15-menu-recents.png), [Settings](Validation/Alpha-v0.0.4/DeliveryAudit/01-Settings.png), [Changelog](Validation/Alpha-v0.0.4/DeliveryAudit/01-Changelog.png), [Chronicles](Validation/Alpha-v0.0.4/DeliveryAudit/01-Chronicles.png), [About](Validation/Alpha-v0.0.4/DeliveryAudit/01-About.png).

World: [Overview](Validation/Alpha-v0.0.4/DeliveryAudit/08-overview.png), [Perimeter](Validation/Alpha-v0.0.4/DeliveryAudit/14-perimeter.png), [River](Validation/Alpha-v0.0.4/DeliveryAudit/09-river.png), [Lake](Validation/Alpha-v0.0.4/DeliveryAudit/10-lake.png), [Rain](Validation/Alpha-v0.0.4/DeliveryAudit/12-rain.png), [Winter preview](Validation/Alpha-v0.0.4/DeliveryAudit/13-winter.png), [3,200 m setting](Validation/Alpha-v0.0.4/DeliveryAudit/17-mountain-3200.png), [Arid climate](Validation/Alpha-v0.0.4/DeliveryAudit/18-arid-biomes.png), [Forest](Validation/Alpha-v0.0.4/FinalBenchmark/06-forest.png), [Low coast](Validation/Alpha-v0.0.4/FinalBenchmark/13-low-angle.png), [Horizon](Validation/Alpha-v0.0.4/FinalBenchmark/05-horizon.png).

Visual review confirms the rectangular outer-land seam is absent in the tested overview/perimeter, menu controls and real recents are readable, and climate/weather feedback is visible. Rivers/lakes and faceted foliage remain a simplified foundation. Moving LOD, high-relief shapes, narrow-water intersections and low-angle artifacts retain manual acceptance; these screenshots do not close those gates. The original references remain under Validation/Phase11/References.

### Preservation and compatibility confirmation

Git index, package manifest/lock and earlier reports match their pre-update hashes. Earlier changelog wording remains a complete unchanged prefix. metrics-summary.json retains these checks and every screenshot dimension. Generator-3 or unsupported archive identities reject without migration or overwrite; no compatibility is inferred from the public app number alone. Exploratory v4 test archives can also reject a changed fingerprint during development; they are isolated from real saves.

### Final review notes

The delivered build reports 109,772,271 bytes and 64.66 seconds in the final Windows build summary. All final source Assets, Packages and ProjectSettings match the validated build copy; Unity's removal of a BOM/trailing blank line in ProjectSettings was mirrored without changing settings. The final audit and final direct suite sampled 72,621 frames; their longest recorded engine-loop interval was 6.30 ms. These runs do not establish universal frame pacing or presented-frame stability.

Both final player logs contain a D3D12 shutdown notice for an unreleased 15 MiB evictable resource. No managed/runtime/shader exception or TypeDB mismatch was found, and returning to the menu removes all owned world services, but the graphics shutdown notice remains an open native-lifetime diagnostic. It is not asserted to be a proved in-session application leak or a fixed engine issue.

NVIDIA's system utility is available, but WDDM per-process memory readings report N/A. A post-probe whole-adapter reading was about 6.1 GiB with other desktop applications active; this is neither Aevoria VRAM nor its benchmark peak. The aggregate query is retained separately and does not replace missing per-application measurements.

Name-limit edge case: Save As suggests a name with a copy suffix. A name already at the 80-character limit can fail that validation; shorten the world name before duplicating it. Full long-name ergonomics, display rollback and physical archive interactions remain on the manual checklist.
