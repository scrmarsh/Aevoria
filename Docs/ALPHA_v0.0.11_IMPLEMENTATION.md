# Alpha v0.0.11 implementation ledger

Status: Alpha Development. Implementation and native acceptance are delivered as an Alpha Development package; the limitations below remain. The preserved request is [requirements.txt](Validation/Alpha-v0.0.11/requirements.txt). Official v0.0.9/v0.0.10 builds, source archives, user saves and all ten literary Chronicles are preserved. No Chapter XI was added.

## Implemented behavior

Generator10 / world schema8 persists thirteen independently identified typed products, derived seeds and the mountain prototype profile. See the [actual dependency graph](ALPHA_v0.0.11_ARCHITECTURE.md). Ordinary gentle terrain and plateau provinces are separate from sparse mountain clusters. High-relief settings produce real20-30km peaks; the current native Miniature sample reaches23,890m. Raw climate is independent of downstream moisture and drainage. Hydrology12 combines acyclic priority-flood spill routes with steepest downhill receivers, seeded meanders, connected lake surfaces and coastal mouth carving. Layout8 publishes final geometry after drainage settles. Caves2, materials, ecosystem placement and founding sites consume typed upstream products.

Regional rendering covers the visible footprint and releases CPU-only uploaded data while retaining canonical collision. Cave mouths and local solid excavation share canonical terrain triangles. Closed boulder/outcrop meshes have matching collision; depletion removes geometry and collision and persists. Rock uses triplanar material detail. World-anchored weather cells keep physical cloud identity through zoom; hardware depth rejects clouds behind foreground vegetation.

Loading shows ten immutable129-square completed-layer snapshots with actual founding/restored settlement markers. Progress derives from13module stages plus4assembly stages. Background creation/loading restores the prior preference; cancellation leaves archives intact. Blank/whitespace seeds resolve to a cryptographic numeric seed only at creation; explicit seeds remain exact.

Saved controls include metric/imperial readouts, context-aware keyboard/mouse bindings, conflicts, Escape cancellation, compact HUD and tool hotbar. Walking is4.5m/s, sprinting9m/s. The grounded wheel selects tools; in flight it scales speed without changing tools. Flight disables walking stairs/slope climbing; freefall retains pose/momentum. Explore enters at map focus, with explicit saved-pose restoration separate. Doctor is an ordinary appearance. Slash opens a focused console with registration/dispatch and text-entry gating; no gameplay command set is shipped yet.

Civilization2 uses real6/8/12-person dwelling capacity, paid founding supplies/construction, varied civic meshes and matching collision, connected terrain-following paths, and bounded moving humanoid residents. Identities, health, physical poses and cumulative deaths persist. A lethal traveler tool ray removes one identity and reduces authoritative population exactly once without modifying resources. Residents represent a bounded subset with simple path walking, rather than a full individual economy. Ecology3 aligns eligible cold moist boreal biomass with biome/tree eligibility; tundra/snow/desert exclusions remain.

Windows builds explicitly use Direct3D11. Windows records identified D3D12Core shutdown access violations on the validated driver. Native validation now checks actual process exit, in addition to gameplay completion and render logs. This does not establish the cause of the original unknown-seed Colossal incident.

## Executed acceptance

- Editor: full129 run originally124passes/five failures; all five were repaired and passed focused reruns. Four long integrations retain assertions with the existing600s budget. Current ecology3/civilization suite24/24 PASS (editor-ecology3-society-24.xml); earlier society/character/preferences21/21 PASS. These are multiple runs, not a new single clean full Editor suite.
- PlayMode: initial15 ran10passes; focused reruns supplied2passes then3/3 clean. Current full15 ran14passes/one300s integration timeout; the isolated Colossal rerun passed in245.23s with the same600s large-world budget and unchanged assertions. The flight failure exposed a real automatic slope-climb defect and is now fixed. Other repairs update obsolete Doctor, decorative-horizon, saved-entry and mesh-name expectations; no geographic/resource assertions were removed.
- Native-Inputs-WindowsD3D11: actual default Direct3D11, live binding conflict/cancel/remap, real camera movement, grounded/flying wheel contexts, double-tap/freefall, Escape/cursor behavior and real-process preference restart PASS. Both exits0; no graphics override.
- Native-Settlement-WindowsD3D11: actual Windows housing/paid growth/paths, moving humanoids, tool-ray injury, injury reload, death/population/resource authority and no resurrection after reload PASS. Verified process exit0. The earlier fixture moved24/24 residents over1m in8s, mean8.624m.
- Native-Boreal-Ecology3: actual conifer stand at X-61000 Z15000, temperature-5.1185C, moisture.6729,403canonical conifers in320m square, errors0/exit0. Before/after views show that ecology2 incorrectly clipped nearly all trees. Queued tree count is not a per-pixel surviving-tree measure.
- Native-WalkingCave-aevoria001, -831904722 and -shaping: actual grounded mouth entry, underground mining and pose/cut reload PASS. These predate the final Windows backend change and establish gameplay/visual behavior, not verified clean process exits under Direct3D12.
- Native-Full-WindowsD3D11-16000: overview, actual23.9km mountain, ordinary terrain, desert/forests, river/lake/coast and nine river zoom ranges completed with errors0 and verified exit0. Physical fingerprint B76894E7FEC356BB6AF67BDC805F16C9FFB758468C1B215A6E3BD9625F8DF9E9 matches pre-material-refinement builds. Reviewed views found pale banks/bright lake water; build50 retake passed clean exit and reviewed views with narrower wet banks and deeper blue lake columns. Physical fingerprint is unchanged. The uncaptured360-frame co-run measured9.8456ms median,12.5173ms p95,43.5114ms max, zero above50ms while Unity work ran concurrently; this is not an isolated benchmark.
- Native-LeanCoverage-Colossal-16000 retained its exact baseline fingerprint and repaired lake coverage. Peak working set3474.74MiB versus4251.28MiB before CPU-copy cleanup. Private committed memory remained above the older baseline; total allocation equality is not claimed.
- Native-Unfocused-Lifecycle passed actual background create/load/cancel and preference/archive preservation. The unknown original Colossal stall seed is unavailable; the incident has not been reproduced.

## Limits and delivery

Procedural art remains stylized. Some hard material boundaries and confluence transparency remain alpha limitations. Distant bank/lake refinements have native before/after views; some confluence transparency remains. The command console and resident AI have the bounded scope described above. Long soak, universal LOD seamlessness and driver-wide compatibility are not established. Older generator/model archives require matching preserved players and are never silently migrated.

Delivered [Windows player](../Builds/Alpha-v0.0.11/Aevoria.exe) and [source checkpoint](../Archives/Alpha-v0.0.11-Source). See [validation/reproduction](ALPHA_v0.0.11_TEST_RESULTS.md), [native gallery](ALPHA_v0.0.11_NATIVE_GALLERY.md), and [preservation check](Validation/Alpha-v0.0.11/final-preservation-check.json). The packaged binary matches build50 byte for byte; native package controls and real process restart passed with both exits0. Current-runtime cave traversal/mining/reload passed with exit0. Temp builds remain candidates.
## Historical refinement notes

The dated entries below describe earlier intermediate states. The current implementation and evidence above supersede their version and completion statements.

## River refinement follow-up, October 24 evening

A geographic GPU index now evaluates canonical monotone river curves for bank materials. Native `Native-Water-SpatialBanks-Miniature` visibly removes the broad triangular bank-color fans; it does not fix all water seams. GPU-vs-CPU closest-point distance and water-level checks pass at more than 100 sampled coordinates. `river-bed-gpu-and-terminal-checks.xml` passes three focused Editor cases, including closed-basin coverage and the new coast-crossing continuity check.

The physical mouth probe (`mouth-physical-field.csv`, recorded before hydrology 11) found channel carving stopped at datum zero, leaving a roughly two-metre underwater step. Hydrology 11 carries carving into shallow coastal water; the focused continuity test passes. Native `Native-Water-Estuary-Miniature` shows an extended mouth but still has a hard river/ocean optical boundary. The following single-column shader build suppresses ocean draws underneath an above-sea-level wet river; visual acceptance is pending.

A nearest-branch transparency ownership experiment (`Native-Water-Ownership-Miniature`) created a narrow visible gap. Its clipping has been removed. Water layout 7 retains valid categorical owner metadata through clipping for future work; metadata contract checks pass. No claim of solved confluence transparency is made.

The latest authoring change adds optional native water zoom captures at nine ranges, 180–48,000 m, followed by a measured 180-frame moving zoom. These are actual player captures, not generated images. That harness is being built; multi-altitude acceptance remains open.
