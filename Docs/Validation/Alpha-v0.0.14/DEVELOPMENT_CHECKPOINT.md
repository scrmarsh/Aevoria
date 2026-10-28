# ACTIVE RESUMED WORK — 2026-10-27

User explicitly resumed. The pause below is historical and does not govern current
work. Read the newest section of Docs/AEVORIA_RESUME.md for live process/build state.
Atlas10 built cleanly; source is newer. Atlas9 revision1 production reload and
regional physical-contact audit passed; visual acceptance remains open. Atlas9
revision0 Water timed out in Ready and is rejected, even though its OS exit was0.
UI, roads and near-field cover require fresh native review. Official release is v12.

# HISTORICAL PAUSE - ATLAS CHECKPOINT, 2026-10-27

User said **pause**. Work is paused. Do not build, test, implement or package
anything else until the user resumes. Atlas5 build was deliberately stopped at
the user's request, including its owned compiler/worker process tree. Its -1
exit is an intentional interruption, not a diagnosed code/build failure. Unity
Hub was excluded. All native audits had already exited normally.

## Exact next starting point

Read `Docs/ATLAS_REFINEMENT.md` and this section first. Latest complete executable:
**Temp/Alpha014Atlas4/Aevoria.exe**, clean build command exited0. It has NOT had a
native audit. Latest native-reviewed executable: **Temp/Alpha014Atlas3/Aevoria.exe**;
`Atlas3-Native-Miniature/process-exit.json` reports exit0. Atlas1 and Atlas2 native
audits also completed0. No official v14 release or new Builds distribution exists.
Latest official release is v12. No staging or commits.

**Source/build difference:** source contains one last, UNBUILT production fix:
WorldEnvironment's distant canopy alpha now uses expected habitat cover rather
than undersampling the 370m individual-grove distribution on a ~1km texture grid.
Atlas3 screenshots revealed an artificial lattice of dark dots. Atlas4 still has
that old canopy representation. The corresponding shader comment was clarified.
Atlas5 was intended to compile this fix but was interrupted. Do not run/ship the
partial Temp/Alpha014Atlas5 directory. Rebuild a fresh Atlas6 or later candidate.
The final test-program width assertions are also newer than early audit builds;
they live outside Assets and do not affect the player.

## Implemented approved atlas scope

- Preserved 581 pre-change source files with hashes in
  Archives/Alpha-v0.0.14-Development-BeforeAtlas. Preserved actual supplied atlas
  PNG under Docs/Validation/Alpha-v0.0.14/References. See pre-atlas-source-manifest.json.
- Explicit persisted GeographyProfile.Legacy=0 / Atlas=1. Missing historical XML
  profile stays Legacy. New-world creation and its preview select Atlas. Existing
  worlds keep original terrain; historical fingerprint bytes are unchanged.
- Continental field creates one elongated, bay-shaped major sea and sometimes a
  secondary basin, with an eastern strait and irregular sea islands. Atlas Random
  uses a supported supercontinent; explicit landmass choices remain available.
- Northern and central mountain placement belts, broader lowland drainage divides,
  and mountain footprints that scale up for large worlds.
- Lake area/depth qualification now scales more strongly on large worlds. No global
  lake-count cap was introduced. First Colossal filter failed the <100 check;
  revised area floor produced 5 lakes in the tested Colossal seed.
- Atlas removes per-segment random sinusoidal river wiggle. Latest source/Atlas4
  also interpolates river widths into their downstream junctions; physical carving,
  segment lookup, bank queries and water mesh cross-sections use the same width.
- Ten appended biome IDs: MediterraneanWoodland, MediterraneanScrub, RiparianForest,
  RockyCoast, SandyCoast, Steppe, RockyDesert, SaltFlat, Oasis, AlpineScree.
  Existing tundra/snow/alpine/forest/meadow categories retained. Coastal classification
  uses actual slope; coastal soil alone classified every coast as rocky.
- Habitat catalog controls tree/ground-cover density and ecology. Added olive-like,
  cypress, umbrella pine, palm, willow and juniper prototypes; added scrub/reeds/
  flowers/moss/salt shrubs/beach grass. Total 12 tree and 8 ground-cover variants.
- Regional seeded climate variation and gentler Atlas rainout reduce ruler-straight
  bands and improve temperate/boreal coverage. Large-world northern tundra is still
  overrepresented relative to the artwork; no final biome-balance acceptance.
- Blended habitat surface colors, reduced overhead haze, bathymetric map colors,
  and distant canopy representation (last anti-alias fix unbuilt, see above).
- Map/minimap remain full-world, fixed north-up, independent of camera movement,
  rotation or zoom. Circle fit contains world corners. Removed map pan/zoom controls.
  Main camera retains normal controls. Map raster512px, minimap256px, cached once.
- Brown house village markers and white circular player marker. Player draws last
  to remain visible over resource dots. Marker textures and shape changes tested.
- F3 explicitly displays Biome and Facing; Explorer uses traveler yaw, World uses
  camera yaw. Native captures inspected: Explorer West and World South.

## Evidence actually completed

- atlas-presentation-tests.xml / atlas-presentation-editor.log: 3 Unity tests PASS
  (all world sizes/panel fit, GPU marker silhouettes/colors, finite habitat meshes).
- atlas-generation-final-miniature.log: latest geography code, 8 inland lakes,
  9463 channel paths; uphill=0, broken endpoint=0, uncovered centreline=0,
  widthGaps=0. All four river assertions PASS, coastal checks 3 seeds x 2 coverages
  PASS, missing XML generation profile remains Legacy.
- atlas-generation-colossal2.log: 5 lakes, 373 channels, uphill=0, broken=0,
  uncovered=0. This run predates width interpolation and its added width-gap assertion;
  repeat at largest scale after resuming. Earlier failed run retained as evidence.
- Atlas2-LegacySave-Compatibility: real v13 save loaded via production loader into
  an isolated copy, original fingerprint retained exactly:
  003C68619D0CB7A35F76845CF6B35AD15CD8E3C95C4DDADBF166BC22495AC180.
  Native exit0, views.json, water screenshots (major-river.png inspected).
- atlas-legacy-current.json, atlas-legacy-preserved.json, atlas-legacy-final.json:
  16,384 historical coast/base/surface/climate samples match preserved source digest
  028C46800C6FB03F8B022C1EDF170A8FBF5398C5FC728ED39296722385A085D5.
  Existing slope check failure reproduced exactly in preserved source:
  n7855, median.035573073, p95.134948211 (test requires <.05). Not introduced here.
- Native Atlas1,2,3 full Miniature audits all exit0. Actual images inspected:
  top-down, world map, map after camera movement, F3, Mediterranean, oasis, boreal.
  Atlas3 shows improved sea shape / readable white marker / fixed map; canopy dots
  were rejected visually and motivated the last unbuilt fix.
- Protected 101 accepted character files and Chronicles hash checked unchanged.
  Original user saves and official/historical archives were not edited. All native
  work uses fresh isolated D:/AevoriaValidation/Prototype011-* roots.

## Next steps when explicitly resumed

1. Confirm no old validation Editor is running, then build fresh Atlas6+ from the
   current source. Atlas5 was interrupted; CleanBuildCache remains required.
2. Run native Full Atlas Miniature review; inspect overhead canopy/blends and map
   (white player circle / brown houses / fixed extent / headings). Use -Atlas with
   Tools/RunAlpha011Prototype.ps1. Do not just trust exit0/screenshots existing.
3. Run Atlas water review using -Atlas -Review Water. Latest audit skips its full
   atlas branch for WaterOnly so the real river diagnostics can run. Reuse a saved
   world by explicitly copying its GUID fixture through AEVORIA_GENESIS_BASELINE_WORLD;
   Atlas fixtures require application version14. Never write to original player saves.
4. Verify production Atlas save reload and repeat native historical v13 fixture load
   on the final candidate. .NET-vs-Unity fingerprint incompatibility is an older
   known issue; actual production Mono load is authoritative.
5. Repeat Colossal latest-width tests, then high-relief / alternate-seed coverage.
   Native all-size/16000m acceptance is still open. Map raster still needs review
   for seasonally dry-channel styling and smoothness at final map resolution.
6. Further visual work is still required to match the reference: northern snow/forest
   balance, dense natural vegetation silhouettes/ground cover, physical dunes/salt
   basins, mountain/river/coast detail and organic biome blending. New vegetation is
   explicitly prototype quality. Atlas3 distant forest-dot artifact is NOT accepted.
7. Earlier Genesis material faceting investigation remains open; only limestone has
   actual new material shading. Full v14/old v13 briefs remain unfinished; do not
   relabel this as a completed release. After visual acceptance, package a clearly
   named development build without replacing official v12 or the older snapshots.

Build workflow:
$env:TEMP='M:/Aevoria/Temp'; $env:TMP=$env:TEMP
$env:AEVORIA_BUILD_PATH='M:/Aevoria/Temp/Alpha014Atlas6/Aevoria.exe'
& ./Tools/ValidatePhase1.ps1 -Action build -ValidationRoot D:/AevoriaValidation -ValidationProject Aevoria010AuditBuild

Do not copy into a validation cache while its Editor is running. Keep Unity Hub
serve untouched. Some PowerShell-redirection logs are UTF16; Python should detect
BOM when reading them. Source and new docs are UTF8. Never restore a save snapshot
just because the user has played and its hash changed.


# Historical checkpoints below

# ACTIVE ? approved atlas work, 2026-10-27

User resumed and approved the image-based geography, biome, map and F3 changes.
Read `Docs/ATLAS_REFINEMENT.md` first for current scope and evidence. Earlier
pause notes below are historical, not an instruction to stop. Pre-atlas source
was preserved before implementation. First candidate validation is in progress.

# Project Genesis development checkpoint

## Latest implementation state

### PAUSED BY USER — 2026-10-27, authoritative handoff

User requested: "pause when safe - create a log so you can know what to pick up
on tomorrow". Work is paused. Do not start another build or implementation until
the user resumes. Current audit had already completed successfully; no task
players, validation Editors or checks remain running. Unity Hub serve is untouched.

Latest completed build: **Temp/Alpha014Rock10/Aevoria.exe**. Clean build log:
rock-prototype10-build.log. Native run **Rock10-ShadowDiagnosis-Miniature** loaded
an isolated v13 baseline save and exited0 at2026-10-27T06:56:52UTC. Rock9 native
normal diagnosis also exited0. Both have summary.json, views.json, launch.json,
actual screenshots, external memory telemetry and process-exit.json. No official
v14 release. No commits/staging. Generator11/schema8/all15 saved geography modules
and render library3 remain unchanged. Characters, actual user saves and older
builds/archives were not edited during this continuation.

Changes in this continuation:
- WorldTerrain.shader now computes camera/filter and PBR view vectors from
  relative positions, avoiding large geographic-coordinate subtraction.
- Material audit now records matched rotated legacy, geometric-normal,
  interpolated-normal, albedo and Genesis perturbed-normal diagnostics.
- Added actual512m chunk-border crossings (focus at boundary-4m and boundary+4m).
- Added temporary sun-shadow-off and bias tests; original light settings restore
  before subsequent captures. No production shadow/bias fix has been applied.

Evidence inspected from Rock9:
- materials-rotated-legacy-diagnostic-2: interpolated mesh normals look smooth.
- materials-rotated-genesis-diagnostic-6: perturbed normals also look smooth.
- materials-rotated-genesis-diagnostic-3: albedo does not show the large triangular
  patches seen in the lit view.
- materials-rotated-legacy and materials-chunk-right: issue reproduced at matched
  angle/boundary. No obvious square color assignment seam in that checked view.
This points investigation toward lighting, but does NOT establish a root cause.

Rock10 materials-rotated-no-shadows.png was inspected while saving this handoff:
the triangular patches remain visible. Do NOT claim shadow bias fixes the issue.
Verify which light/URP term the toggle actually affects before interpreting it.
materials-rotated-shadow-bias-2.png exists but has NOT been visually inspected.
The requested pause interrupted further diagnosis; no more implementation was done.

IMPORTANT SOURCE/BUILD DIFFERENCE: after Rock10's source copy began, two additional
audit-only captures were added to Alpha009TerrainAudit.cs: shadowBias=.3 with
normalBias=.5, then shadowBias=.3 with normalBias=1. These source lines have NOT
been compiled or run. Rock10 contains only no-shadows and depthBias1/normalBias2
comparisons. A future build will include the two milder tests. Do not confuse the
unbuilt source additions with current executable evidence.

Tomorrow's next steps:
1. Read this section, then inspect Rock10's bias2 image and compare its actual lit
   frame/no-shadow frame. Check main-light identity and any remaining shadowing.
2. Isolate PBR contributions at the exact problematic rotated pose: diffuse,
   specular, ambient/GI and shadow attenuation. Raw normals and albedo already
   look smooth; do not start changing terrain heights/colliders or protected rigs.
3. Identify and fix the responsible rendering term, build a fresh Rock11 or later
   candidate, repeat matched diagnostics plus chunk-border/rotation captures.
4. Keep phase2 visual acceptance OPEN until the faceting is resolved. Then proceed
   to anti-tiling/blending/regional materials, geometry/polish/performance/audio and
   full native acceptance. Fourteen material families are still definitions only.

Use Tools/RunAlpha011Prototype.ps1 -Review Materials with a fresh output directory.
For production save loading, set AEVORIA_GENESIS_BASELINE_WORLD to the explicit
test fixture GUID directory under:
D:/AevoriaValidation/Prototype011-9029804acdf7489980aaf2a655fa7aea/Worlds.
The runner copies it into fresh isolated saves before opening it. Never point
test writes at original user saves. User gameplay since earlier snapshots may
legitimately change their saves; never restore old snapshots to force hash matches.
Build through Tools/ValidatePhase1.ps1 with explicit Temp candidate output,
TEMP/TMP=M:/Aevoria/Temp, cache D:/AevoriaValidation/Aevoria010AuditBuild, only when
that cache's Editor is stopped. Do not spawn agents without user authorization.

### Historical Rock8 native milestone — superseded by pause handoff above

Current app0.0.14 Development; generator11/schema8/all15 geography products remain
unchanged. Render library3, format1, recipe identity
599397CCA1BCBEB5578AD971770CE9B9E7E7E109C45B2942615D7D2FCF3E644A.
Current build Temp/Alpha014Rock8/Aevoria.exe (clean build log rock-prototype8-build.log).
WorldDLL81A967159BF77D23C838659E09A6C28C724248060F4DFA3F15CD897AC3D6492B;
PresentationDLL064170729A24EA28491557711CD9A5DF767F328EAFC2A0C6D289D36E8C2F65B2.

Rock8-LoadedV13-Materials-Miniature-aevoria-001 loaded an isolated real v13 save,
completed native audit and exited0 at02:59:26UTC. Nine captures. Close cliff,
600m,medium mountain and rotation inspected. Repeating wavy bands/camouflage
patches/subpixel crack speckles from earlier trials were corrected. PHASE2 VISUAL
ACCEPTANCE IS STILL OPEN: rotated close views expose noticeable faceting and
triangular surface changes, requiring matched legacy/normal diagnostics. The
current look is a prototype; do not mark it production-ready. VISUAL_REVIEW.md
links exact captures and records remaining checks. No new audio/geometry/regional
family shading is implemented; fourteen families are recipe definitions only.

material-recipe-checks5.json passes38 checks. material-gpu-origin-tests3.xml passes
all3 expanded actual GPU cases at unchanged tolerance after local-metre splitting.
material-geography-integration1.log passed fingerprint/rebuild/cancel (0.0010s).
preservation-and-native-compatibility-Rock8.json verifies101 protected characters,
82 original World source/meta files,109 actual player save/settings files and
Chronicles unchanged. Actual native saved-world fingerprint remains
003C68619D0CB7A35F76845CF6B35AD15CD8E3C95C4DDADBF166BC22495AC180.

Complete development build160094744 bytes, baseline160017592 (+77152). Packed
definition data1792 bytes; source/shader/input memory measured separately in
Alpha014Rock8-size.json. No texture savings claimed. Rock8 short engine-loop
windows: cliff legacy median9.33ms, Genesis11.46ms; GPU process-memory peak995.57MiB.
These are diagnostic windows, not final CPU/GPU or motion benchmarks.

Next work: diagnose faceting at matched rotations, use relative camera vectors,
explicitly cross512m chunk boundaries, inspect all camera distances and lighting
before accepting phase2/3. Then implement actual regional shading, geometry,
polish, optimization, audio and full native validation. Persisted optional render
recipe metadata still needs evaluation; current presentation uses bundled profile
on unchanged geography. Standalone .NET/Unity fingerprint mismatch is retained as
cross-runtime diagnostic evidence; native production load passes.

No official v14 release/source delivery archive exists. No commits/staging or
older-file overwrite. Do not resume old v13 phase5 accidentally: new v14 brief is
the active scope, with missing v13 features explicitly recorded as carry-over.

All task-created audits/Editors/checks have finished. A separate Aevoria process
was observed at final inventory (PID551128, preserved pre-Genesis player path,
without audit arguments). It was not launched by the current audit runner and
has been left untouched. Do not kill it as a stale test process. Unity Hub serve
also remains untouched. Development is a saved milestone, not marked paused.

Rock6 production-load/material review exited0, native pre-Genesis fingerprint
retained. Close grain/sparse joints improved, but600m view exposed repeating wavy
bedding: phase2 visual gate STILL OPEN. Replaced periodic sine bedding with
hashed, non-repeating sediment-layer mineral values. Rendering library version
bumped to2; geography versions remain unchanged. Rock7 build underway; recipe
checks4 and subsequent native review pending. Do not advance to regional families
or call the current limestone accepted until this medium-distance defect is gone.

LATEST: actual v13 save production load passed in Rock4b (native exit0) with the
same canonical fingerprint and original archive hash unchanged. Full preservation
report preservation-and-native-compatibility.json verifies101 character files,
82 original geography source/meta files,109 actual player save/settings files and
Chronicles unchanged. This is native compatibility for the baseline fixture;
broader cave/mining/gameplay acceptance is still pending. The standalone .NET
cross-runtime mismatch remains retained diagnostic evidence, not a native failure.

Rock4b sparse-joint surface removed the paved look, but its mineral color/grain
still needs work. Current Rock6 source has brighter chalk/calcite variation and
6 cycles/metre filtered grain. Recipe identity
221ACA28972C278B75FA9AA408089885E1E349A8E92EFEAC9E36755FD996798F;
material-recipe-checks3.json passes38 checks. Expanded actual GPU test caught a
0.002012 noise jump with6-frequency/octave2, above unchanged0.002 tolerance.
Fixed GenesisCoordinates to split local integer metres before scaling as well
as splitting the geographic origin. All3 expanded GPU cases now pass at the
original tolerance (material-gpu-origin-tests3.xml/log). Build Rock6 underway;
Rock5 compiled but was not launched because this numerical test needed fixing.
Do not call phase2 visual gate passed yet. Phase1 recipe/binding/math foundation
has data/GPU/integration evidence; full material response/family shading is later.

Next anti-tiling audit should explicitly straddle a512m chunk edge; the current
48m pan does not guarantee one. Also use relative camera vectors for PBR/filter
footprints on the largest worlds rather than subtracting huge geographic floats.
Those follow-up improvements are not implemented in the current Rock6 build.

Newest: Rock3 native exit0, but its medium-distance cellular relief looked like
cobblestone and is NOT accepted. Current Rock4b removes cells from base color and
height; only sparse, domain-warped joints use the cellular field. Recipe identity
C4D8BC45DB97416FEF06C069773E218CFD961A1A32FBB2EE13BD9ABEED8ED925.
38 pure recipe tests pass again (material-recipe-checks2.json). Actual GPU readback
tests pass3 cases (material-gpu-origin-tests.xml/log), each exercises frequency,
projection, large positive/negative origin and1024m rebase, including octave-2 warp.
Rock4b builds clean (rock-prototype4b-build.log). Actual native production-loader
review started in Rock4b-LoadedV13-Materials-Miniature-aevoria-001, loading an
isolated copy of the pre-Genesis v13 archive. Both compatibility and visual
acceptance remain pending. Initial Rock4 build had a test-harness API compile
error, corrected by reading the copied archive and invoking production LoadWorld.
No user save was opened for writing; original baseline archive hash recorded.

App/source0.0.14 Development; generator11/schema8/all15 geography module versions
unchanged. Phase0 source/player preservation verified. Actual default player saves
and settings also preserved,109 files236062055 bytes, in
Archives/PreGenesis-PlayerSaves. Fresh native baseline Full Miniature passed exit0;
46 captures,111.7s generation. summary.json records short frame-window and external
memory measurements. Hardware RTX3060Ti8192MiB/i7-8700K/approximately64GiB RAM.
Existing terrain was already mathematical; no texture-storage savings claimed.

Phase1 foundations: SurfaceMaterialRecipes.cs (14 immutable definitions, independent
render seed/version,32-float GPU contract), GenesisMaterialRenderer.cs (four split
lattice origins per family and129-square geological strike field),
GenesisMaterials.hlsl.38 recipe/origin checks passed. Full geography/save/rebuild
integration passed, cancellation0.0010s. This covers same-runtime data integration.

Phase2 Rock2 build/native exit0, nine captures, but VISUAL GATE FAILED: cliff too
flat and cellular cracks too regular. Retained same-pose legacy/Genesis images.
Native old/new generation fingerprints are exactly equal:
003C68619D0CB7A35F76845CF6B35AD15CD8E3C95C4DDADBF166BC22495AC180.
Rock3 changes limestone scales/normal response and uses meso-domain vector warp,
including negative octave origin carry; compiled clean. Rock3 Materials native
review launched; image acceptance remains open. Other families are definitions,
not finished regional shading. No new terrain geometry or audio implemented.

Compatibility caution: standalone .NET attempt to rebuild a real Unity-created
v13 archive failed fingerprint comparison (native-v13-save-compatibility1.log).
It does not write the source archive. Native pre/post generators agree; evaluate
the suspected runtime difference using the actual Unity loader. Audit source now
supports -aevoria014LoadBaseline. Runner env AEVORIA_GENESIS_BASELINE_WORLD copies
an explicitly selected saved-world GUID directory into fresh isolated saves,
then invokes production LoadWorld; original archive hash is recorded. This audit
change is AFTER Rock3 compilation and needs the next build. Baseline fixture is
under D:/AevoriaValidation/Prototype011-9029804acdf7489980aaf2a655fa7aea/Worlds.
Do not point the loader at original user saves. All user/protected content retained.

The supplied v0.0.14 brief supersedes the active update scope. The actual starting
state is unfinished v0.0.13 development (geography/climate phases0..4 basic gates),
not a completed Mediterranean release. Latest completed official release: v0.0.12.
Carry-over omissions include remastered vegetation, physical dunes/salt flats,
regional materials, rendering/UI/roads and final all-size gameplay acceptance.
Do not claim those exist because the new brief describes them as a baseline.

Phase0 preservation: baseline.json, source/player manifests and asset-usage.json.
Exact pre-Genesis source is Archives/Alpha-v0.0.13-Development-BeforeGenesis;
player is Builds/Alpha-v0.0.13-Development-BeforeGenesis. These are explicitly
development snapshots. All101 protected character files match the v12 baseline;
Chronicles and Git index unchanged. No older builds, archives or saves modified.
Baseline complete development player160017592 bytes; exact ZIP52781046 bytes.
Fresh native baseline Full Miniature aevoria-001 started; inspect captures and
measurements before claiming phase0 performance/visual baseline complete.

The earlier generated overhead concept map has not been identified in the project.
Existing terrain screenshots and Phase11 landscape references remain available;
they are not claimed as the missing concept image.

Implementation direction: versioned render recipes independent of saved geography
modules. Preserve generator11/schema8/module versions and canonical fingerprint.
Use a seed derived from the saved master seed plus an explicit rendering recipe
version. Keep geological/biome inputs continuous and keep high-frequency sampling
relative to the floating origin with split integer/fractional lattice coordinates.
Start with actual limestone/rock on the existing physical mountain, then validate
anti-tiling and regional families. Geometry and audio phases follow visual gates.
No release is authorized as ready until the brief's acceptance gates pass.
