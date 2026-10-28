# Atlas12/13 review — 2026-10-28, development only

Atlas12 native Full and Interface, Atlas13 Interface completion/log/manifest checks
pass. Inspected all4 Atlas12 compass headings (needle correct), pause (F3 leak
removed), Atlas13 Display/Graphics (Back overlap removed), Atlas12 Mediterranean/
boreal/salt ground, strongest physical dune close, village paths overhead/oblique,
clouds at2000/20000/60000m. Cloud identity through zoom, toggles preserving weather,
and time-driven motion are proven by assertions/CSV. Production Atlas revision1
reload retains the earlier fingerprint. All36 regional collider probes pass.

The world still does not meet the reference/art target. Observed issues include
straw-like herb/moss prototypes, ball-like crowns, flat/scalloped cloud silhouettes,
graphic polygonal salt crust, subtle physical dunes, river stripes and joins,
and unresolved cliff faceting. Atlas14 implements targeted habitat/cloud/water
refinements; its clean build is not native visual acceptance. Review fresh images.

# Atlas9 review — 2026-10-27, development only

Actual Atlas9 captures inspected: reloaded overhead; Mediterranean, boreal,
desert and salt ground views; old v13 cliff and diagnostic14; Atlas0 river close.
Revision1 retains its generated geography through a real production reload and
36 regional collider probes pass. The overhead canopy lattice remains removed.
Ground sand ripples and litter are present, but obvious terrain facets, sparse
understory, simple crowns, river bands and angular junctions remain unaccepted.
Salt ground is too plain. Forest/Mediterranean ground lacks convincing variation
and vegetation detail. The reference remains the target, not a claimed match.

Atlas9-Atlas0-Water fails readiness at its reverse view and has no completed
manifest. Do not infer success from its exit0. Atlas9-Legacy-Materials and
Atlas9-Atlas1-Full pass native completion/log/manifest checks. Concurrent timings
are diagnostic only, not final performance benchmarks.

# Resumed Genesis / Atlas review — 2026-10-27

Current work remains development, not release acceptance. The supplied reference
is present under References/Aevoria_Fantasy_Atlas_Overhead.png; missing-reference
statements in historical sections below are superseded.

- Atlas6 built cleanly after recovering an interrupted Bee cache. Native full
  Miniature review exited0; completion, logs and capture manifest passed.
  Actual top-down/world-map captures were inspected. The repeated forest-dot
  lattice is removed. Coarse climate boundaries, sparse detail and the gap to
  the reference remain visible; this is not final overhead art acceptance.
- Atlas7-Lighting-Diagnosis loaded the real v13 fixture through the production
  loader and exited0. Fingerprint remains
  003C68619D0CB7A35F76845CF6B35AD15CD8E3C95C4DDADBF166BC22495AC180.
  Matched diagnostics7/8/9/10/11/12/13 were inspected. Raw-mesh diffuse12 already
  contains the triangular shading patches, whereas albedo/roughness/footprint
  do not explain them. The new Atlas8 candidate reconstructs shading normals
  from the existing regional normal field with cubic filtering; positions,
  colliders and saves are not changed. Its visual result is still pending.
- Atlas7-Water-Miniature loaded an isolated Atlas6 save and exited0. Native
  major-river, close and3500m images were inspected. Straight routing, mouth
  appearance, overlapping-channel/width transitions and aliasing are NOT
  visually accepted. Source inspection found GPU RiverBankField still packed
  constant node widths after physical widths had become interpolated. The
  corrected GPU endpoint widths/depths and filtered wave phases are newer than
  Atlas8. CPU/GPU Atlas bank tests and another native candidate remain required.
- All14 material families now have shader implementations in source, with
  geography-derived habitat masks and physically based blending. Render library4,
  identity C7E7B45DB26160EA1A7F13A250F620998BFBF1284683A7A2A520B95521F73D1A.
  This is a first implementation awaiting native acceptance, not14 finished
  materials.38 recipe checks and7 GPU/presentation tests pass. First GPU attempt
  caught a reserved HLSL identifier; its rejected logs are retained. Corrected
  tests explicitly reject shader-error/constant-color readbacks.
- Native Atlas7 runs overlapped an Editor check/another audit. Their short frame
  samples are diagnostic data, not sustained or uncontended performance evidence.

See the newest Docs/AEVORIA_RESUME.md section for exact build/source differences.

# Historical Project Genesis prototype review

This is a development review, not v0.0.14 release acceptance. Phase0 preservation
and the material foundation have technical evidence. The limestone prototype
remains under visual review. Regional families, geometry, audio and final tests
are unfinished. The overhead concept reference has not been identified.

Latest native build: `Temp/Alpha014Rock8/Aevoria.exe`, application0.0.14 Development,
render library3, generator11/schema8. It loaded an isolated real v13 native save
through the production loader and exited0. Original archive/user files are unchanged.

| View | Actual native screenshot |
|---|---|
| Same-pose legacy cliff | [Legacy](Rock8-LoadedV13-Materials-Miniature-aevoria-001/Miniature-materials-cliff-legacy.png) |
| Same-pose Genesis cliff | [Genesis](Rock8-LoadedV13-Materials-Miniature-aevoria-001/Miniature-materials-cliff-genesis.png) |
| Landscape scale | [600m](Rock8-LoadedV13-Materials-Miniature-aevoria-001/Miniature-materials-cliff-600.png) |
| Medium mountain | [Mountain](Rock8-LoadedV13-Materials-Miniature-aevoria-001/Miniature-materials-mountain-medium.png) |
| Rotated cliff | [Rotation](Rock8-LoadedV13-Materials-Miniature-aevoria-001/Miniature-materials-cliff-rotated.png) |
| Panned cliff | [Pan](Rock8-LoadedV13-Materials-Miniature-aevoria-001/Miniature-materials-cliff-pan.png) |

Earlier trials were rejected for flatness, a paved/cellular appearance, repeating
wavy bedding, then distant camouflage-like blotches and crack speckles. Current
code separates cellular joints from continuous rock relief, uses non-periodic
layer mineral values, and filters fractures by their physical width. Inspected
close,600m,medium mountain and rotated views show those corrections. Grain remains
present up close and fades at larger distances.

Remaining concerns: rotated close views expose faceting/triangular surface changes.
Compare matched legacy/normal diagnostics at that rotation to identify geometric
versus material contribution. The48m pan does not guarantee a chunk-edge crossing;
add explicit512m border tests. Larger worlds, lighting changes, motion/shimmer and
geographic blending remain open. This does not establish universal anti-tiling or
final rock-art acceptance.

In short stabilized same-pose windows, engine-loop medians were9.33ms legacy and
11.46ms Genesis. These are not isolated CPU/GPU timestamps or sustained movement
benchmarks; raw CSV and summary.json retain the limits and frame tails. Final
performance profiling and optimization remain required.

The complete development player is160094744 bytes versus160017592 baseline,
an increase of77152 bytes. Packed recipe definitions occupy1792 bytes; that is
not total shader/code/input memory. The baseline already used procedural terrain;
no texture-library storage reduction is claimed. See Alpha014Rock8-size.json.
