# v0.0.12 completed checkpoint and development history

## Completed delivery state — supersedes historical notes below

Packaged Windows player: Builds/Alpha-v0.0.12/Aevoria.exe, candidate12 binaries.
Full actual packaged-player character audit passed, exit0/errors0, all3 characters,
all tools and perspectives, jump/flight/freefall/landing, hill movement, save/reload.
Native-Final-Package contains the final images/CSV and9 sampled replay MP4s.
Doctor neck width extends below collar into torso with torso-anchored base weights;
native standing/crouch/motion and both forced LOD crouch captures inspected.
User accepted head; its shape retained during neck change. Tools brown wood and
steel shading checked; upright carry/grip/cutting plane reviewed. Body/face/style
limits recorded honestly in Docs/ALPHA_v0.0.12_IMPLEMENTATION.md.

Final focused Editor11/11 PASS: character-final-eleven.xml; physics/viewmode Play
test PASS; actual native UI/mining60stone/save depletion regression9 PASS/exit0;
copied v11-save compatibility8 PASS/exit0. World.dll exact v11 hash, World source
and dialogue unchanged. v11 complete runtime/source inventory differences0.
Existing Chronicles/index unchanged. No agents, commits, external publication.

LOD export bone order issue fixed by runtime bone-name remap; added actual crouch
bake bounds consistency test and forced LOD native images. Source70508vertices,
110074triangles,37bones,2UV; LOD144009tri/LOD213188tri. All authored sources remain
editable and independent Blender re-export supports actual artist changes.

Reports, before/after source/native gallery and reproduction tools completed.
Source checkpoint Archives/Alpha-v0.0.12-Source and delivery-provenance.json are
preserved during final packaging. Official previous builds/archives stay intact.
No further required code changes; retain known alpha art/animation limits.

---

## Latest state (supersedes older notes below)

### Latest user steering and candidate11 (supersedes candidate9 notes)

User: **head looks fine; neck separates from body; extend neck width into torso**.
Acknowledged keeping head shape, widened base into upper chest under collar and
blend weights into torso. Generator cutoff5.95 (was6.30), flare radiusX.054+.057
and Z.049+.028 below1.60, geometry unchanged above1.645. Neck weights below1.62
blend CHEST->NECK across1.510..1.605; above blend neck->head. Previous base was
neck-weighted only, causing separation when bending. New10th Editor test checks
wide attachment below1.535 and >.75 chest weight. Source/LODs11 regenerated:
near70508vertices/110074tri, LOD144009tri, LOD213188tri,37bones/2UV sets.

Candidate9 native FULL PASS/exit0 and gameplay regression PASS/exit0 including
actual60stone/UI/saved depletion. Actual native9 face reviewed; user accepts head.
Native9 revealed tools gray because missing UV channels allowed head-texture blend
on equipment. Fixed by explicit zero UV2/UV3 in equipment meshes AND shader gate
reference*(1-_SurfaceClass). Face albedo illumination reduced in shader, normal
3D lighting retained. Portrait audit now unequips normal tool and disables only
audit dialogue subtitles, farther .85m face distance for clear images. Product
dialogue unchanged. CHANGELOG/README now v12, need finish linked reports/artifacts.

Candidate10 compiled clean (candidate-build10.log), not run because new neck
user steering superseded it. Currently candidate11 build session tool output
(AEVORIA_BUILD_PATH Temp/Alpha012Candidate11) and10 focused tests running.
New -aevoriaDoctorReview flag + runner -DoctorOnly allow fast focused neck review;
default remains all3 characters. Next: launch candidate11 DoctorOnly immediately,
inspect front/side/crouch/neck connection and brown wood/shaded steel. Need final
all3 capture run/packaging, reports/comparisons/source preservation afterward.

Candidate9 sessions53945/66208 completed. Last no active Blender. Buildcache Unity
active for11, don't copy source/metadata into cache until completed. Root new
reference PNG.meta already copied. No terrain/World/source identity modifications.

---

- User explicitly reported sideways tools mid-task. EquipmentGrip012 now uses
  upright carry15deg, pickaxe axial90/axe-90, actual wrist roll Doctor-90 vs
  other+90 canceled in mount, first-person viewmodel compensation. Fingers now
  flex85/95deg and thumb opposition AFTER editable clips; build8 close-up shows
  actual wrap around shaft. Crouch adds60deg carry compensation so head clears
  Doctor's face; build8 crouch-side reviewed and good relative to build7.
- Native-Character-Compatibility8 PASS/exit0: loads copied actual v11 saved world
  95517f88fed14a0fad9456a98a73cf05 from isolated fixture, performs full three-char
  selection/tool/camera/jump/flight/freefall/landing, uphill/downhill Doctor and
  actual save/reload. Same fingerprint and identical World.dll v11 SHA.
- Native-Character-Wrist7 also PASS/exit0; inspect8 for newer hand/crouch behavior.
- Added proper editable Blender distance LODs and runtime LODGroup with3 skins;
  shared skeleton/material/head-masking, owned meshes released. New sizes near
  109812triangles, LOD1 43904, LOD2 13157; source/weights/UVs retained.
- Added actual UV-mapped head albedo from USER PROVIDED reference, copied as
  Assets/Resources/Characters/DoctorReference.png (1448x1086). This is normal skin
  texture ON the actual anatomical mesh, NOT screen overlay. Front projection
  mask ReferenceSkin fades on side normals, blend toward original skin at neck.
  HouseReferenceUV stored in Blender, JSON faceUV/skin, runtime uv3/uv2.y and
  CharacterSurface shader _HeadMap. Actual standalone edited-.blend exporter
  supports both UV sets, mask, colors, Metallic, normals and rest weights.
- Blender-Reference9 diagnostic renders showed clear reference resemblance;
  double geometric eyebrows removed in subsequent9c; sparse modeled beard400
  hairs supplements textured stubble. Neck radial taper below1.645 fixes bare
  shoulder flare. Hair1800 tangent strands with gray temples. Source9c +LODs9
  generated and exported. Near audit70370vertices/37bones/2UV/109812triangles.
- Source authoring normal reads initially O(n²) due RNA attribute invalidation;
  stopped only verified Blender helper448368 and fixed by caching normals BEFORE
  writing attributes. Generation9b/9c completed. No active Blender expected.
- Source metadata/materials copied from idle build cache: CharacterSurface and
  EquipmentSurface mats and21 new .meta files. This fixed PlayMode material-null
  fixture failure; preserved failure XML. Rerun traveler collision/viewmode/flight
  PlayMode test PASS in traveler-physics-and-viewmodes.xml.
- Focused9 tests (character+Roman+three actual rig/equipment orientation cases)
  pass including latest textured mesh; character-reference-nine.xml.
- Build9 compiled clean. Logs prototype-build9.log. Currently running:
  session53945 Native-Character-Reference9 (new albedo/full visual audit);
  session66208 Native-Gameplay-Regression9 (established Alpha010 UI+actual60stone
  mining+depletion+inventory/save/reload checks). Both actual visible players.
  Review their logs/results/exit and images before claiming acceptance.
- Root Documentation source pipeline updated, equipment README and CC0 source
  PROVENANCE.md. Need update provenance to mention actual reference skin albedo.
  Root README/CHANGELOG still not v12 release. No official v12 build/archive yet.
- World source hashes/index/Chronicles rechecked unchanged. World.dll of8 equals
  exact v11 SHA79E35E...C101928A. No agents, staging/commits or Chronicles XII.

NEXT: inspect Native9 face/front/profile/grips/crouch/all tools+perspectives and
moving sequences. Check both native sessions; fix genuine failure. Then preserve
before/after actual screenshots, make short sampled replay MP4s with ffmpeg
(installed WinGet path available Get-Command), write factual implementation,
test/visual acceptance and limitations/changelog. Verify source/binary World
invariants and older build inventories. Copy latest new reference texture .meta
from idle cache. Package Alpha-v0.0.12 Windows and preserved source checkpoint
only after visual gates. Source archive can omit earlier huge prototype captures
while originals remain in root; include accepted evidence and reference/before.
No need to rerun previously passing checks absent new code/failure.

Potential remaining quality issues: hands are still stylized at extreme zoom,
cloth not photorealistic, static cynical expression (no facial speech rig), hair
strands not salon-quality. Need actual Native9 likeness review, don't infer it
from Blender diagnostic or automated PASS. Current code/assets after9 compile
only documentation/audit-json2UV correction changed, no new runtime code.

---

User's full prompt: requirements.txt in this directory. Latest explicit steering:
**tools are sideways**. Corrected carry shaft from fixed 90 degrees to 15 degrees,
per-tool axial alignment (pickaxe90/axe-90), independent first-person compensation
and windup/strike/recovery in EquipmentGrip012.cs. Native Grip5 shows upright
carry and pick head in swing plane. Need latest evenly sampled swing review.

Preserve v0.0.11 official Builds/Archives, user saves, all Chronicles (no XII).
No git staging/reset/commit. No agents authorized. World C# sources untouched.
Baseline hashes in baseline.json. Current version 0.0.12 Development.

Unity6000.3.27f1, Blender5.2.2 installed in Program Files. Use isolated caches
D:/AevoriaValidation/Aevoria010AuditBuild (build) and Aevoria009NativeValidation
(edit). Never copy into cache while its Unity runs. Hub serve PID127548 is NOT
the Editor, leave it alone. Set TEMP/TMP=M:/Aevoria/Temp. Tools/ValidatePhase1.ps1
copies root into cache, launches hidden Unity. AEVORIA_BUILD_PATH selects output.
Build D3D11 required; v11 had D3D12 shutdown crash. Native review players visible.
No native desktop control needed; runner captures actual Windows backbuffer.

Assets:
- Tools/BuildCharacterRemaster012.py authors actual editable Doctor/tool meshes,
  UV/normals/weights/materials, .blend under SourceAssets/*/Remaster012 and
  runtime JSON Assets/Resources/{Characters,Equipment}. Regenerates source!
- Tools/ExportCharacterRemaster012.py independently exports manually edited
  .blend files without rebuilding geometry; verified clean exit0 all7 meshes.
- Anatomical head based on CC0 MakeHuman asset data (not program code), pinned
  commit a8bc2d54ff0ac92e78ff71431b1023eda42bf482. Licenses in source/MakeHuman.
  Reference: Docs/Validation/Alpha-v0.0.10/UserReferences/doctor-turnaround.png.
  Current head still NOT accepted as Hugh Laurie likeness.
- Corrected winding: conversion (x,-z,y) determinant+1 so preserve triangles!
  Normals alignment test passes. Original prototype1 was inverted and rejected.
- Right cane, Doctor left tool; other characters right tool, no cane.
- HumanoidPresentation partials: authored JSON, Motion012 limb/contact IK,
  crouch pelvis lowered/back, shoulder counterlean, right-leg asymmetric gait.
- First-person actual Doctor body, masked head, eye-relative left-arm IK.
- Equipment PBR shader vertex RGB/albedo alpha/metallic, woodgrain/fabric.
- Editable Unity animation clips in Assets/Resources/Characters/Motion012;
  sampled as base pose, live feet/cane and FP IK afterward. Editor seed script
  CharacterMotion012Export preserves existing clips unless explicitly rebaked.
- Latest generator change (refinement7 running): anatomical neck extended below
  collar with neck/head weights, smoother gray hair tangent strands, real short
  beard hairs, older70/young30 face and forehead creases. Not yet built/reviewed.

Evidence/tests:
- Native-Character-First: automated PASS, visually rejected inverted normals.
- Native-Character-NormalsFixed: PASS/exit0, too dark under forest for face.
- Native-Character-Daylight4: PASS/exit0, dry grassland faces actual sun.
  Improved crouch and FP hand visible, but tools still sideways in this build.
- Native-Character-Grip5: PASS/exit0, upright tools, detailed PBR, left/right
  config, all3 characters cameras/save/load, same world fingerprint.
  Still bad neck/head seam (latest source corrects); sparse hair (latest changes).
  Old .045s screenshot sampling missed swing. New audit uses captureFramerate60,
  28 frames, screenshot every2 and CSV of actual actionProgress/state, plus each
  tool first-person image. Need build6+ and review these sequences.
- character-crouch-and-ik.xml5/5; character-and-roman.xml6/6 PASS.
- Latest Editor retest session31345 running, with copied editable clips.
- Native Grip5 run session9440 completed; build5 session5791 completed.
- Last Blender refinement7 session in tool output, verify completion.
- Latest source after build5 not yet compiled: audit capture timing, beard/neck/
  hair authoring, no change to equipment orientation after build5.

Fingerprint consistently34E3461F1673AB6A4BCEF5655B18DACE61DD68F6ACCB82C36AC12D8200F201E2.

Remaining: evenly sampled actual swing review/repair; full latest native visual
review, camera/all tool orientation, mature recognizable head/hair acceptance;
slope/turn walk sequence and deeper mining/inventory/flight/jump regression;
copied v11-save compatibility; useful LODs; final editable asset/license docs;
copy generated .meta/materials from idle cache to root; final before/after native
gallery/video and honest validation report. No final v12 release/archive yet.
Do not call visual acceptance merely because the audit prints PASS.

Prior v11 isolated save fixture (copy Worlds into NEW isolated root, never edit):
D:/AevoriaValidation/Inputs011-d7a9f3f337594b958e4badd8d882eaa0
Worlds/95517f88fed14a0fad9456a98a73cf05. Launch metadata under
Docs/Validation/Alpha-v0.0.11/Native-Final-Package/launch.json.

Existing Alpha010CharacterAudit performs actual UI interaction +60 stone mining,
flight/freefall and save/reload. Its eyeheight mining fixture assumes1.65, update
fixture to Doctor EyePosition if needed; don't weaken real product assertions.
