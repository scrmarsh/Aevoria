# Copy this entire document into the new Codex account

2026-10-28 amendment: the user authorized ongoing commits and pushes to
https://github.com/scrmarsh/Aevoria. Follow Docs/GITHUB_WORKFLOW.md; this supersedes
historical instructions against staging/committing. Read the newest resume section
for the current source, candidate build and pending native validation.

You are taking over Aevoria in M:/Aevoria on Windows/PowerShell. Finish the
unfinished v0.0.14 Project Genesis update, including the subsequently approved
atlas geography/biome/map requirements. This pasted prompt is my instruction to
resume; the previous account paused at my request. Do not ask for the same approval
again. Conserve credits: use one agent, batch related edits, read the current
handoff first, and avoid repeating expensive builds/tests without a specific reason.
Do not start a new redesign or reimplement completed work.

## Read these local files in this order

1. Docs/AEVORIA_RESUME.md -- newest pause section is authoritative.
2. Docs/ATLAS_REFINEMENT.md -- approved later atlas requirements; its older
   in-progress bullets are superseded by the newest resume section.
3. Docs/Validation/Alpha-v0.0.14/requirements.txt -- complete original v14 brief.
4. Docs/Validation/Alpha-v0.0.14/DEVELOPMENT_CHECKPOINT.md -- latest atlas handoff,
   followed by historical Genesis diagnosis and earlier checkpoints.
5. Docs/Validation/Alpha-v0.0.14/VISUAL_REVIEW.md and
   Docs/ALPHA_v0.0.14_DEVELOPMENT.md -- historical material findings. Claims here
   that geography was unchanged or the concept was missing describe the earlier
   baseline, NOT the current atlas source.
6. Docs/Validation/Alpha-v0.0.13/requirements.txt and its checkpoint only as needed
   for outstanding Mediterranean work, not to repeat completed phases.

The actual supplied reference is now preserved at:
Docs/Validation/Alpha-v0.0.14/References/Aevoria_Fantasy_Atlas_Overhead.png
Original: C:/Users/Sean Marsh/Downloads/Aevoria_ Fantasy Atlas Overhead.png

## Full scope and approved art direction

The reference shows an irregular continent: connected snowy northern mountain
ranges, boreal forest below, rich central mixed forest/meadows/river valleys,
a separate central/southern mountain belt, Mediterranean woodland/scrub and
limestone coasts, southern sand/rock deserts, pale salt basins and green desert
river corridors. Ocean is navy with turquoise shallows and varied coasts, bays,
straits, peninsulas and islands. Use this as art direction for varied procedural
seeds and explorable terrain, not a static map image or one fixed seed/template.

User specifically approved: one or two major Mediterranean-like seas; far fewer
random ponds/lakes; occasional justified large/glacial/basin lakes; smooth connected
terrain-led rivers and tributaries, consistent banks/widths/confluences/mouths,
mountain-confined channels and lowland meanders without random decorative bends.
Introduce every visible habitat with its own appropriate trees/shrubs/ground cover
and biological features. F3 must show current biome and cardinal facing direction.
Both map panels must always show the whole playable world, fixed north-up,
independent of player/main-camera motion/rotation/zoom. Player is a small white
circle; villages are small brown houses. Keep main-camera controls usable.

The full v14 brief additionally requires high-quality procedural materials,
non-repeating macro/meso/micro detail, geographic material intelligence, integrated
geometry, visual polish, performance/size measurement, compact environmental audio
investigation and complete native validation before a release. Read the brief for
its exact requirements; do not silently reduce the task to only the atlas patch.

## Earlier Genesis work already completed

- Pre-Genesis source/player and actual save/settings snapshots were preserved.
- Baseline profiling and native visual evidence exist. Hardware RTX3060Ti8GiB,
  i7-8700K, ~64GiB RAM. Baseline was already procedural; do NOT claim texture-library
  storage savings that never existed. Latest official release remains v0.0.12.
- Source/app is 0.0.14 Development, Unity6000.3.27f1. Generator11, archive schema8,
  fifteen geography modules; Hydro13, InlandWaterLayout9, Climate6. Atlas adds a
  separately persisted profile without changing historical Legacy generation.
- Fourteen immutable material recipe definitions, independent render seed,
  split-lattice origins and geological input texture are implemented.
- Render recipe library3/format1 identity:
  599397CCA1BCBEB5578AD971770CE9B9E7E7E109C45B2942615D7D2FCF3E644A
- Limestone prototype shading exists: triplanar gradient/cellular noise, warped
  joints, oriented non-periodic strata, filtered grain, perturbed normals,
  roughness and URP PBR. The other thirteen new material families are definitions
  only; existing legacy surface shading is still in use around the prototype.
- 38 recipe/origin checks and 3 actual GPU large-coordinate/rebase checks passed
  earlier. Existing full geography/save integration passed before Atlas.
- The rock visual gate is still OPEN: rotated close views have triangular facets.
  Raw mesh normals, perturbed normals and albedo diagnostics looked smooth.
  Turning shadows off in Rock10 did not eliminate the facets; no root cause or
  production shadow-bias fix was established. Do not claim it was solved.
- Latest earlier material executable Temp/Alpha014Rock10/Aevoria.exe; source has
  two extra mild shadow-bias audit captures that were not in Rock10, but later
  Atlas builds include the audit source. The checkpoint records details.

## Work not started / not completed

- Full new Genesis shaders for the thirteen remaining material families are not
  implemented. Rock is still a limestone prototype; final visual acceptance open.
- Comprehensive anti-tiling/seam/material-transition acceptance remains open,
  although the foundation and some noise/filtering/blending code already exist.
- Full regional grassland/Mediterranean/forest floor/desert/snow/ice treatments,
  physical dunes, convincing salt basins and integrated small terrain geometry
  remain mostly unimplemented or prototype-only. Do not relabel biome IDs/colors
  or new simple foliage meshes as completion of these requirements.
- Final vegetation/ground-cover remaster, organic biome blends, stronger northern
  snow/forest balance, river/coast/mountain polish and full atlas visual match are
  not accepted. Large worlds currently overrepresent tundra compared with the art.
- Procedural environmental audio prototype/comparison has not started; do it only
  after stable visuals, as the brief requests. Preserve existing audio.
- Final sustained performance optimization, final installation/storage figures,
  all-size/all-relief native acceptance, packaging, release archive and final
  release report have not been completed. No official v14 release exists.
- Older v13 Mediterranean road/UI/cloud/environment polish and final acceptance
  still need reconciliation against its own checkpoint/brief. Some related atlas
  work is now started; inspect before deciding what is missing.

## Non-negotiable preservation / workflow

- Never stage, commit, reset or clean the repository without explicit instruction.
  HEAD is unborn; preserve existing index. Git may need per-command
  `git -c safe.directory=M:/Aevoria ...`; do not change global Git config casually.
- Preserve all 101 accepted character files (Doctor head/neck, grips, tool orientation,
  crouch/limp work), Chronicles, actual player saves/settings, and old official builds.
  Do not redesign House or revisit accepted characters as part of atlas work.
- Existing native v12/generator10 worlds require their historical build; preserve
  originals and do not silently migrate them. Legacy generator11/v13 saves are
  supported and have passed real production load into isolated copies.
- Never restore save snapshots to force a hash match if the user has since played.
- Original snapshots: Archives/Alpha-v0.0.13-Development-BeforeGenesis,
  Builds/Alpha-v0.0.13-Development-BeforeGenesis, Archives/PreGenesis-PlayerSaves.
  New pre-atlas source snapshot: Archives/Alpha-v0.0.14-Development-BeforeAtlas.
- No official archive/build may be overwritten. Development candidates currently
  live under Temp; do not publish an unfinished candidate as a completed release.
- Do not copy into a validation cache while its Editor is running. Use fresh
  evidence destinations. Inspect actual images after successful native runs.
- Keep Unity Hub serve untouched. Last inventory after pausing showed ONLY Hub
  PID127548, no task game or validation Editor running. Recheck, don't assume PIDs.
- Unity path: C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe
  Python: C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe
- Full clean Windows build workflow and exact native runner commands are below.
  Use isolated save roots. Native D3D11 is baseline; D3D12 shutdown previously crashed.
- Time-stamped logs from PowerShell redirection may be UTF16. Source/docs UTF8.
- Some old tests have stale assumptions. Preserve real failures and diagnose;
  do not loosen assertions merely to make tests green. Known legacy slope failure
  and .NET-vs-Unity archive-fingerprint issue are explained below.

## Detailed current implementation, tests and exact stopping point

Everything below is the saved checkpoint from the paused account. Its pause is
historical once I paste this entire prompt as a resume instruction. It lists the
unbuilt last fix, the latest completed vs native-reviewed candidates, evidence,
and required next steps. Start by rebuilding the pending source, not by replaying
all prior experiments. Bundle any justified fixes before another full build.

# PAUSED BY USER - ATLAS CHECKPOINT, 2026-10-27

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

