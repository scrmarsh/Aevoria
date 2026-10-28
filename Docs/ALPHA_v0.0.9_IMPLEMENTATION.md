# Alpha v0.0.9 — The Natural World Update

Status: implemented development milestone under final Windows validation. No stable release or human visual acceptance is inferred from automated results.

## Integrated implementation — October 24, 2026

The sections below retain the development chronology. Their earlier version numbers and unfinished-feature statements describe those checkpoints, not the integrated source. The current contract is application 0.0.9 / generator 9 / geology 4 / hydrology 9 / inland-water layout 5 / climate 3 / caves 1 / archive 6 / living state 3. The theoretical relief setting remains 16,000 m.

The integrated source implements broad geological roots, plateau provinces and coastal scarps, signed bathymetry, fine outlet routing and closed-basin water budgets, joined river surfaces and terminal pools, climate-supported snow/ice, and physical cloud identity independent of zoom. Terrain, water, collision, materials and origin rebasing consult common geographic coordinates. Fine material normals no longer use the unstable calculation observed in the early snow/excavation investigation.

Underground systems now use a deterministic three-dimensional cave graph and a common solid field for terrain, natural air and sparse spherical cuts. Nearby 16 m cubes publish geometry and baked collision atomically; a bounded ownership atlas removes only the replaced portion of the heightfield. A separate, render-only 32 m cube view continues distant cave walls within the current geological system. The old sixteen-site, thirty-two-cut, ten-metre and reserved-border excavation restrictions are removed. Material/tool validation, a four-metre reach and finite world bounds remain deliberate gameplay constraints. See [underground architecture](UNDERGROUND_WORLD.md).

The Roman explorer is an actual weighted SkinnedMeshRenderer with a skeletal hierarchy, continuous limb surfaces, rigid armor, helmet, crimson cloth and cape. Idle, walk, run, jump, flight, freefall, landing and digging have distinct bone animation. Animation does not move the physics root. This is procedural stylized art; it is not a production photorealistic humanoid or a customization interface.

T preserves the explorer pose and airborne momentum across Top-Down and Explore. F5 cycles first-person, rear third-person and reverse third-person. Collision constrains the camera; obstructed transitions fade between safe views. Creation/loading show actual operation stages and immutable current-world height/climate previews. Cancellation and scene cleanup use the existing lifecycle.

The final integrated Windows candidate has passed actual entrance traversal, cave walking, valid/invalid wall targeting, mining, underground archive reload and all eight character states. Editor, Play Mode and portable results, exact build provenance, the five-size native benchmark, compatibility and remaining limits belong to the [validation report](ALPHA_v0.0.9_TEST_RESULTS.md). Historical failed runs remain available. No creator archive or previous build is overwritten.

Known limitations include coarse distant narrow river banks and shorelines, angular local river-mouth transitions and visible gaps between water and mixed-detail terrain in some settled captures, stylized procedural character/environment art, approximate erosion/climate/cloud physics, bounded detailed cave presentation, and outstanding physical-keyboard, subjective visual and extended-soak acceptance. River-mouth visual continuity is not fully resolved. Lake counts increased with the finer drainage graph; this update does not claim a universal reduction. Final frame-time results must retain their spikes rather than imply a universal 60 FPS guarantee.

## Phase 0: baseline and dependencies

The source, settings, history, tools and existing validation documents are preserved in `Temp/Alpha009Baseline`. The delivered v0.0.8 Expanded executable and its recorded hashes remain the comparison build. Its complete evidence remains in `Docs/Validation/Alpha-v0.0.8/Addendum`. The v0.0.9 request is preserved beside this milestone's evidence. Existing archives and the Git index are not migration targets.

Baseline evidence includes the existing geometry/normal/albedo diagnostics, five-size native traces at 2560 × 1440, and motion checks. These are historical measurements, not v0.0.9 results. New regression captures must use matching poses, configuration and diagnostic modes. New motion traces must exercise the actual controller and camera at large coordinates.

Source audit identifies these mechanisms to investigate:

- Flight deactivation calls `EnterAt`, which explicitly relocates to terrain. Replace this with an airborne gravity transition and preserve momentum.
- Controller height and centre are reassigned every movement frame. Collider recreation during motion can destabilize grounding; update dimensions only on stance changes.
- Excavation publishes a replacement solid collider while the explorer's old heightfield still covers the patch. Remove overlapping support in the same publication transaction, including an already-running collision preparation job.
- Collision vertices and player movement are stored at absolute float world coordinates. At Colossal coordinates this is centimetre-scale quantization. Local collision coordinates alone do not solve rendering or player precision; a coordinated solution needs motion validation before acceptance.
- Third-person camera lag is applied independently of player motion. Rendering motion must use the same physical position and pose, without hiding physical instability.
- Terrain shader uses screen-space stochastic LOD discard in its shadow caster, and procedural normal perturbation on excavation surfaces. Diagnose each independently against flat lighting, normals, albedo, shadows disabled and a stationary surface before selecting a correction.
- Geology still subtracts powered narrow noise valleys and ridges from a coarse raster. Diagnose their contribution separately from hydrology and shader patterns. Broader mountain bases, plateau provinces and plains must precede dependent drainage reconstruction.
- Cloud presentation crossfades between different local and continental systems as orbit range changes. World anchoring within each set does not make these the same clouds. Keep physical systems independent of zoom.
- Mining remains limited to sixteen dry, gentle 32 m patches, thirty-two cuts and ten metres of depth. This architecture cannot satisfy connected underground traversal without a new streamed volumetric ownership model.

## Ordered work

1. Critical stability: freefall, stance/grounding, collider publication, precision and motion diagnostics, snow/excavation shader diagnosis. Run focused Editor, Play Mode and standalone motion checks before dependent systems.
2. Terrain: version new geography; remove narrow noise incision, widen geological mountain provinces, add geological plateaus and coastal escarpments, climate-specific plains, continuous drainage and shoreline sampling. Compare multiple seeds across five sizes.
3. Environment: climate-driven snow, pale ice, shallow/deep water, bathymetry, and zoom-independent weather clouds. Keep display controls independent of simulation.
4. Underground: deterministic three-dimensional cave fields and chunk streaming, shared solid/excavation ownership, persisted edits, target ring, accessible entries and stable collision. Do not substitute a heightfield trench for caves.
5. Character/camera: proportioned rigged legionary, separate visual animation states, position-preserving mode transitions, T and three F5 perspectives, text-entry suppression.
6. Delivery: real completed-work progress and current-world preview, final native gameplay/visual review, comparable performance traces, compatibility notes, unchanged historical books plus a new current Chronicle entry, separate Windows build and an explicit remaining-defect list.

Generation, archive and presentation cache versions must change when their contracts change. Prior archives remain with matching older builds. Each stage records exact source/build provenance; previous results are never relabeled as current verification.

## Stability checkpoint — October 23, 2026

Implemented: freefall with momentum and an explicit separate recovery action; optional persisted airborne velocity; guarded stance changes; atomic excavation/heightfield collision ownership; removal of wind deformation from rigid travelers/tools/rocks; a shared horizontal scene origin for physics, rendering, instanced objects, clouds and geographic camera rays; local excavation vertices; precise Top-Down focus accumulation; rebase-aware particles and post-process history; stable, bounded fine material normals; depth-derived exposed soil and rock strata.

This is a checkpoint, not full milestone acceptance. Generator 7/geology 3/hydrology 5/climate 3/archive 5 remain while physical geography is unchanged. The new traveler fields are optional and absent older fields default to rest. The v0.0.8 Expanded generator-7 archive format remains readable; incompatible older generators remain rejected. No creator archive has been modified.

Executed evidence so far:

- 117 portable checks passed in `Validation/Alpha-v0.0.9/portable-stability.log`; a later focused solid-depth check passed against the new material attribute implementation in `portable-solid-depth.log`.
- The first full Editor run passed 88 of 89 cases and exposed stale bundle-version metadata. That source setting was corrected. Subsequent focused checks include the version, projection, small Top-Down movement, terrain safety and rebase contracts; failed intermediate runs remain preserved.
- Four focused Play Mode cases passed, including actual freefall landing, excavation publication/cleanup, standing beside a replacement cut, and maximum-speed flight at 800 km east / 700 km south across origin changes. `play-current-stability.xml` records the current four-case pass.
- `Native-Colossal-resume-fixed` runs the candidate-5 Windows player with isolated D: saves. Its live controller retained the airborne pose and momentum through save/load, landed on real collision, and completed first/third-person motion scenarios with no runtime errors. This is programmatic motion through gameplay code, not physical-keyboard acceptance or final performance acceptance.
- The original surface probe separated geometry, mesh normals, perturbed normals, albedo and direct shadows. Excavation speckling survived disabling direct shadows and appeared in the perturbed-normal diagnostic while mesh normals stayed smooth. The bounded, origin-split normal calculation removed that observed speckling in the repeated native probe, and exposed depth now selects soil/strata materials. The current-origin snow/natural-object isolation follow-up is still under review.

Candidate-5 source is preserved in `Temp/Alpha009Candidate5Source`; exact binary hashes are in each native `launch.json`. Earlier native candidates remain separate. The first C: build import failed from physical disk exhaustion; new Unity builds use `Tools/ValidatePhase1.ps1 -ValidationRoot D:\AevoriaValidation`. No older cache, executable or evidence was deleted to work around it.

The current-origin surface review found the remaining dark objects to be rendered natural rocks: they disappear when those objects are disabled. The unstable fine material normal speckling is removed in the repeated native excavation captures. Phase 2 is now in progress; its changes require renewed movement and rendering regression checks.

## Terrain reconstruction checkpoint

The development geography now uses generator 8, geology 4 and hydrology 6. These are incompatible physical changes; old archives remain with their matching older builds. The final v0.0.9 compatibility contract is still under development.

Removed the narrow powered-noise incision and the kilometre-scale vertical coast correction. Coast position now controls a signed physical shore profile, preserving negative seabeds. Broader tectonic roots, resistant plateau provinces and distinct stable lowland shaping have been introduced. The initial Miniature comparison reduced median sampled grassland slope from 0.679 to 0.064; later plateau changes are being measured separately and must not inherit that result. All figures are seed/configuration-specific, not universal guarantees.

Actual native geometry review exposed two further issues. Plateau shaping could lower pre-existing summits into closed bowls; the current cap now raises its support rather than cutting those summits down. Rivers have real surface geometry, but their ordinary rendering was nearly transparent. The SSAO setup requests a depth/normal prepass, while the custom terrain supplied only a depth-only pass. A matching terrain depth/normal pass and centre-inclusive river mesh sampling are under native verification. The water diagnostic images remain preserved alongside normal rendering and rendering with the inland water disabled.

The new `play-cliff-flight.xml` passes a real-controller coastal-scarp regression: maximum horizontal flight stops without being lifted onto the cliff. The previous flight floor clamp has been replaced with a sweep through the canonical heightfield triangles. This is in addition to the earlier far-coordinate motion checks, not a substitute for renewed native traversal.

A pass-aware drainage routing experiment was rejected. On the fixed Miniature probe it increased maximum sampled channel incision from about 352 m to 1,182 m because the subsequent fine-basin rejection still lowered receiver corridors. Its source and measurements remain preserved; it is not in the current implementation. Drainage, lake placement, actual shoreline geometry and high-relief visual acceptance remain open. The first ten-config planetary run passed downhill and wet-endpoint checks for an earlier development candidate; those results are not final-source acceptance.

The C: Editor validation cache again lost package contents and failed with missing URP/Input System references. That is a failed run, not a product test pass. Current builds and repeat validation use the healthy D: isolated project. No earlier cache, build, save or evidence was removed.

Underground caves, expanded mining, the Roman soldier, new camera controls, world loading progress and final all-size delivery remain unimplemented. No final v0.0.9 executable has been declared accepted.

## Verified terrain and environment checkpoints — October 24

The current physical contract is generator 8 / geology 4 / hydrology 9 / climate 3, with inland-water layout 5 and archive 5. Drainage no longer lowers entire downstream corridors when a fine basin is rejected. Real fine outlets and explicit closed-basin water budgets settle to a converged receiver graph. Expansion belongs to each current basin; sharing an earlier coarse basin ID had caused a reproducible Colossal generation failure, now covered by renewed flight validation. Rounded terminal water geometry completes the wet channel bed. Terrain depth/normal publication, stable water-column colour and matching bank attributes across detailed, regional and overview meshes correct the observed rendering defects. Distant narrow banks remain an open visual-quality issue.

The terrain checkpoint passed 32 focused Editor cases, three renewed real-controller Play Mode scenarios and ten repeated seed/size reconstruction configurations. The environment checkpoint passed six focused Editor cases. Two native tropical/cold high-relief environment reviews completed without runtime errors, retaining cloud physical identity and density through paused zoom, checking display independence, and recording snow/camera and running-clock cloud motion. Exact provenance, failures, measurements and limitations are in the [development checkpoint](Validation/Alpha-v0.0.9/DEVELOPMENT_CHECKPOINT.md).

Snow support now uses warmest-season climate and precipitation rather than a universal altitude cutoff. Seasonal snow and ice continue their existing saved timelines. Perennial cold water receives pale ice. Cloud formations remain the same physical systems through zoom and advect continuously in fractional simulation time; coherent volume erosion and internal shading replace uniformly rounded silhouettes. The terrain and environment Windows candidates and their source snapshots remain preserved before underground development. These checkpoints do not complete the milestone or satisfy its remaining cave, mining, character, camera, loading and final delivery requirements.
