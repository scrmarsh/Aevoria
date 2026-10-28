# Alpha v0.0.10 — executed validation

October 24, 2026. Alpha Development implementation, with explicit open acceptance. [Implemented contracts](ALPHA_v0.0.10_IMPLEMENTATION.md), [native gallery](Validation/Alpha-v0.0.10/Comparison.html) and [machine-readable evidence](Validation/Alpha-v0.0.10/validation-summary.json). No public release is declared.

## Outcome and limits

Civilization/settlement authority, paid growth, population/resources, geographic influence, real history pages, schema-7 persistence and observation have executed evidence. The optional doctor is a functioning rigged procedural character with a persistent unlock, selected appearance and exact offline dialogue; **facial likeness remains below the supplied reference**. Cane contact, foot sliding and subjective art/physical-input acceptance remain open. The natural-world river-bank/mixed-LOD defects and performance tails are carried forward. Detailed citizens, markets, dynasties and warfare remain future behavior.

## Automated validation

| Run | Executed result | Time |
| --- | --- | --- |
| Full Editor regression | 119/120 passed; one three-world determinism case exceeded its inherited 180 s timeout | 2761.47 s |
| Unchanged terrain determinism case, focused retry | 1/1 passed | 146.83 s |
| Full Play Mode regression | 15/15 passed | 1374.85 s |
| Refined character/retained Roman Editor checks | 5/5 passed | 0.35 s |
| Society hardening Editor checks | 10/10 passed | 50.57 s |
| Corrected full portable regression | 118 PASS records; process exited 0; final water regression 811 accepted / 0 rejected | See raw log |

The full Editor invocation remains recorded as failed by timeout. Its unchanged focused retry completes the **120 distinct Editor cases across recorded runs**; overlapping focused cases are not added to invent a larger total. No assertion was relaxed to suppress a terrain mismatch. The retained full Play suite covers society/character save/load, movement/origin/camera, ecology, menu and underground behavior. Scripts and APIs do not establish physical keyboard/mouse acceptance.

Logs/XML: [full Editor](Validation/Alpha-v0.0.10/editor-full-regression.xml), [focused terrain retry](Validation/Alpha-v0.0.10/editor-terrain-determinism-retry.xml), [full Play](Validation/Alpha-v0.0.10/play-full-regression.xml), [portable](Validation/Alpha-v0.0.10/portable-full-ownership-fixed.log), [society/archive totals](Validation/Alpha-v0.0.10/society-current-isolated.log), [refined characters](Validation/Alpha-v0.0.10/editor-character-refined.xml). The first portable run exposed a historical fixture still using prototype ownership; the corrected fixture performs the application's explicit aggregate handoff. Earlier prototype RNG tests remain. Concurrent portable apphost rebuild and native launcher/background/PID/enum-alias failures are preserved separately; they are not passing application evidence.

## Long-duration society and archive evidence

The current pure-authority millennium run grows from 4 to 38 settlements, retains 46 immutable history pages and continues identically through year 1,001 after restoration. Actual expanded settlements conserve food during allowed transfers and reject cross-polity/self/nonfinite transfers without changing stores. Maximum sampled annual-boundary monthly step was 38.963 ms under concurrent validation; archival I/O is outside that step timer. [Executed run](Validation/Alpha-v0.0.10/society-millennium-conservation.log).

Century restoration/continuation passes all five distinct sizes. The enum also retains Huge as an alias of Large; the portable enumeration exercised that alias twice. Colossal creates 20 suitable spaced civilizations against a target of 24, rather than overriding placement constraints. Locale, malformed economy/construction/ownership, negative inventory, immutable pages/path validation, old-page retrieval, duplicate-page retention and authoritative archive totals are covered. Synthetic pager fixtures are labeled test records; they are not simulation history.

## Actual 1440p Windows size matrix

Each actual size creates a world, renders settlement/inspection/political/history views, progresses twelve live calendar months, saves, reloads identical society authority and cleans up presentation with zero runtime errors. Saved metadata verifies 128/256/512/1,024/2,048 km dimensions, and assembly hashes tie each result to its candidate. Large's raw Mono enum name is Huge; summary data normalizes the retained alias.

Hardware: Intel(R) Core(TM) i7-8700K CPU @ 3.70GHz; NVIDIA GeForce RTX 3060 Ti; 64 GB installed RAM. High-quality terrain preset; clouds/weather display and motion blur disabled. These short accelerated traces ran alongside validation workloads and are **not exclusive hardware benchmarks** or independently measured display FPS.

| Size | Width km | Civs | Population before → after | Create / save / load s | Accelerated mean / p95 / max ms | Peak process private / dedicated GPU MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Miniature | 128 | 4 | 427.3 → 431.2 | 82.06 / 0.90 / 80.88 | 52.59 / 76.50 / 86.27 | 4501 / 962 |
| Small | 256 | 6 | 664.9 → 670.0 | 59.71 / 0.79 / 56.09 | 42.02 / 68.30 / 72.12 | 4142 / 961 |
| Medium | 512 | 10 | 1239.0 → 1245.1 | 54.55 / 0.63 / 55.54 | 40.85 / 72.79 / 93.43 | 3364 / 834 |
| Large | 1024 | 16 | 1766.9 → 1707.4 | 44.81 / 0.71 / 42.32 | 48.55 / 88.30 / 125.13 | 3569 / 834 |
| Colossal | 2048 | 20 | 2331.5 → 2181.9 | 44.60 / 0.84 / 57.43 | 37.54 / 59.55 / 70.64 | 3950 / 835 |

Full-lifecycle camera-render counts include creation/loading/menu periods; accelerated frame statistics use the separate raw CSV windows (Miniature: 91; Small: 92; Medium: 93; Large: 92; Colossal: 94 samples). WDDM process counters sample memory approximately every three seconds. Missing GPU counters would remain unavailable rather than becoming zero usage. Authority/settlement timing columns in CSVs are the most recent completed diagnostic step, not a promise that each frame performed that work.

128× is a requested speed: retained debt remains, and this workload does not meet a universal 60 FPS target. The earlier native thirty-year Miniature run changed population 427.27 → 621.10, passed exact reload/cleanup and recorded 14,955 camera renders with zero runtime errors. Its accelerated mean/p95/max was 41.324 / 60.350 / 108.892 ms. It used an earlier foundation candidate and a different measurement window; no controlled speedup is inferred. [Original result](Validation/Alpha-v0.0.10/NativeFoundation-RouteFix/result.json).

## Character and visual evidence

The current native character audit passes default Explorer, optional Roman, phrase-unlocked Doctor, walking/flight/freefall, unchanged society, selected-character reload and dialogue cleanup with zero runtime errors. [Result](Validation/Alpha-v0.0.10/NativeCharacters-BackgroundFixed/result.json). All 30 dialogue lines were mechanically compared with the archived request and match exactly. The left-hand cane is a real attached mesh; the right-leg asymmetry changes presentation, not physics. [Editable Blender sources](../SourceAssets/Characters/README.md).

Grounded doctor/front and walking, Large political overview, Medium settlement inspection, initial foundation camp and political views were inspected. The first coarse face/airborne audit views remain preserved. The refined face is cleaner three-dimensional geometry, but exact recognizable likeness is not established. Aggregate production uses geographic catchment potential; individual rendered trees, granaries and farm plots are not a detailed economic-agent simulation. Political grids and overland probes are coarse approximations.

## Provenance and preservation

The background-fixed native candidate is [Builds/Alpha-v0.0.10-AuditCandidate](../Builds/Alpha-v0.0.10-AuditCandidate/Aevoria.exe). [Exact built-source manifest](Validation/Alpha-v0.0.10/build-source-audit-background-fixed.json) records 363 source/configuration files, a separate snapshot, assembly hashes and zero subsequent authoring differences at capture. Final packaging provenance is recorded separately when present. Existing v0.0.9 builds/source and earlier failed observations remain preserved. The original changelog and Chronicle byte prefixes match the v0.0.9 source archive. Final verification found ten generated obj cache files with restore-time drift in that earlier archive; authored source and runtime binaries still match. [Recorded differences](Validation/Alpha-v0.0.10/earlier-generated-drift-audit.json) preserve expected/current hashes. No older authored files were replaced, and the new source archive excludes bin/obj caches. Book X and ten-chapter metadata validate. No Git staging/commit/reset occurred; index SHA-256 is 00024D9632812793FAFCE6418D7392644E0ABEB315014E843AE1A0740F416583.

Long continuous soaks, subjective/reference art acceptance, physical controls and externally presented-frame measurements remain open. Successful compilation and these tests do not close those gates.

## Final development package

[Windows executable](../Builds/Alpha-v0.0.10/Aevoria.exe), with its adjacent runtime folders; [source archive](../Archives/Alpha-v0.0.10-Source); [delivery provenance](Validation/Alpha-v0.0.10/delivery-provenance.json). The exact final executable repeats creation, twelve live months, save/reload authority equality and cleanup with zero runtime errors. [Final smoke result](Validation/Alpha-v0.0.10/NativeFinalDelivery/result.json).

The final World assembly is byte-identical to the five-size native candidate. The only gameplay-source-file difference after those audits is the About footer identity; runtime Changelog/Chronicle text now includes executed results. [Final built-source manifest](Validation/Alpha-v0.0.10/build-source-final-delivery.json) preserves that exact source. No stable-release, reference-likeness or physical/soak acceptance is inferred.
