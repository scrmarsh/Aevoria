# Current development checkpoint — 2026-10-28

Source is now versioned at https://github.com/scrmarsh/Aevoria on `main`.
Initial import: `c11ae4c`. Bulk captures, generated players, historical local
archives and player-save backups remain local; see `GITHUB_WORKFLOW.md`.

Latest clean candidate: `Temp/Alpha014Atlas12/Aevoria.exe`. Its regional Full
and Interface native audits are running against isolated copies of saved worlds.
Atlas12 initially caught a compass local-variable compiler error; corrected
source built successfully. Rejected build log retained separately.

Completed checks:

- Eight Unity editor tests pass: all14 material-family GPU rebase/finite checks,
  existing numerical GPU origin checks, map fitting and marker silhouettes,
  finite habitat meshes, deterministic walkable resident paths.
- Recipe library6:38 CPU checks,1792 packed bytes; salt crust has a physical
  near-field polygon scale. New recipe identity:
  `2187999AF19E016FDF2DD025A872AAD39ABF8C6E7328F711A5151B2F4988187F`.
- Atlas9 revision1: native Full including36 collider samples and production
  save/reload passed. Atlas10: four-option pause and production Save & Quit to
  Home plus reload passed. Its inspected compass/pause captures exposed needle
  clipping and one-frame F3 leakage; Atlas12 contains fixes awaiting inspection.
- Atlas11 matched native cliff captures with ambient occlusion enabled/disabled
  pass runtime/manifest checks. Facets remain visible with AO disabled. No visual
  acceptance or confirmed faceting fix is claimed.
- Preservation check:101 protected character files, Chronicles and126 original
  player save/settings files unchanged.

Near-field cosmetic plants, fern leaflets/crown leaves, physically checked road
curves, continuous road cross-sections, stable river wave phase and expanded
cloud/dune/road audits are implemented. Actual appearance, sustained performance,
remaining water/terrain issues, native all-size/high-relief review, subsequent
audio investigation and release packaging remain open. Official milestone is v12.

# Historical Genesis ledger - read current handoff first

Atlas changes are now implemented in development. The supplied concept image was
found and preserved. Earlier unchanged-geography/missing-image claims below are
historical. Current status and pending source/build differences:
`Docs/CODEX_ACCOUNT_HANDOFF_PROMPT.md` and `Docs/AEVORIA_RESUME.md`.

# Project Genesis development ledger

Actual baseline: v0.0.13 Development, generator11/schema8, with fifteen existing
geography products. Latest completed official release: v0.0.12. The unfinished
Mediterranean features are explicitly tracked in the v14 checkpoint.

Preservation and measured baseline are in Validation/Alpha-v0.0.14. Pre-change
source and player snapshots are labeled Development-BeforeGenesis. The actual
player save/settings snapshot is Archives/PreGenesis-PlayerSaves (109 files,
236062055 bytes), copied and hash-verified without editing originals.

The baseline already generates terrain appearance mathematically. This work
improves the procedural model; it does not replace a large existing terrain
texture library. Baseline build160017592 bytes; ZIP52781046 bytes includes retained
debug support and is not a shipping-package measurement. Final sizes and savings
must be measured after validation.

Phase0 fresh native Full Miniature aevoria-001 passed,46 captures, generation111.7s.
Its medium mountain view was inspected. Short engine frame-loop medians were
20.50ms mountain,9.21ms plain and9.01ms river at2560x1440. These are not isolated
CPU/GPU timings or sustained movement benchmarks. External peak process working
set2294.11MiB; dedicated GPU process memory995.96MiB. Raw evidence/summary retained.
Hardware: RTX3060Ti8192MiB, i7-8700K, approximately64GiB physical memory.

Phase1 implementation: fourteen immutable render-recipe definitions with explicit
format/version, independent world-derived material seed, color endpoints, scales,
octaves/noise family, domain warp, strata/fractures, roughness/normal strength and
environmental affinities. Packed definitions1792 bytes; GPU recipe capacity2048
bytes. This is definition data only, not total shader/material/texture memory.
The recipe shader receives four split lattice origins per family and a small
129-square geological strike/uplift/rift input field. No per-location full-detail
image is generated or stored in a world save.

The recipe system is independent of WorldMaterialField's gameplay resource rules.
No geography module version, generation seed, physical height, receiver graph,
collision, excavation or persistence schema has been modified. Recipe changes
must receive a rendering version bump. The preserved player/source retain the
old presentation for historical reproducibility. Persisting an optional rendering
profile in save metadata still needs evaluation during compatibility validation;
the current build applies the bundled recipe version to unchanged geography.

Phase2 prototype wires limestone into existing terrain rock influences, with
triplanar seeded gradient/cellular noise, warped fracture networks, oriented
bedding, filtered micro-grain, surface-gradient normals and URP physically based
rock lighting. Other recipe families are definitions only at this stage.
No anti-tiling or visual acceptance is claimed until native captures are reviewed.
Audit mode Materials captures real high/medium mountains and an exposed cliff at
600/140/45m ranges, legacy/Genesis at the same pose, rotation and a48m pan.

Initial compile/harness defects were corrected. Current Rock8 build/native review
both pass, including production loading of a real v13 saved-world copy with an
unchanged fingerprint.38 recipe/origin checks,3 actual GPU origin tests and full
geography/save/rebuild integration pass.101 protected character files,82 original
geography source/meta files,109 actual player save/settings files and Chronicles
remain unchanged (preservation-and-native-compatibility-Rock8.json).

Current rendering library3 uses non-periodic sediment-layer mineral values and
physical-width fracture filtering. Earlier flat/paved/wavy/spotted trials are
retained. The wide mountain no longer shows the earlier camouflage/speckles;
rotated close views still expose faceting that must be diagnosed before visual
acceptance. See Validation/Alpha-v0.0.14/VISUAL_REVIEW.md. Other regional families
remain definitions. Geometry/audio/final validation and release remain unfinished.
The160094744-byte development build is77152 bytes larger than baseline, with
1792 bytes of packed recipe definitions plus separately measured shader/input
data. Final performance/size acceptance remains open.

The earlier generated overhead concept image has not been identified in this
project. Existing gameplay captures are evidence, not a substitute concept image.
