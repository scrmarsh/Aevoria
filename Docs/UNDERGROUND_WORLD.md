# Streamed underground world — v0.0.9 implementation contract

## Implemented architecture and validation

The future-tense contract below is retained as the original design. It is now implemented in the integrated generator-9 development source.

`CaveNetwork` lazily builds deterministic 2,048 m geological cells, retaining at most 128 immutable systems. Each generated graph has two winding loops at different depths, connecting ramps, branches, chambers, shafts and terrain-probed entrances; eligible systems also expose a ravine. A 16 m spatial index bounds signed-distance queries. Eviction and reconstruction do not change the field.

`VolumetricSolidField` takes the minimum of positive-solid triangular terrain, natural-cave distance and the outside of persisted spherical cuts. Indexed marching tetrahedra samples common boundary coordinates. Nearby 16 m chunks use 0.5 m cells, shared normals and depth attributes. `ExcavationPresentation` prepares with at most two workers, stages native meshes and collision baking, retains old support until replacement is ready, and publishes the complete batch with its ownership atlas. `HeightfieldSupportClipper` removes only installed vertical slabs. Collider ownership checks the actual installed mesh and object identity; a child object beneath the application root is not automatically terrain.

`DistantCavePresentation` supplies render-only 32 m cubes at 1 m resolution, using 0.5 m for edited cubes. It covers the current cave system rather than the entire planet. A slight inset avoids overlap flicker with the detailed near field. It currently rebuilds the bounded far batch after edits; incremental dirty-cube rebuilding remains an optimization opportunity. Collision remains the near field's responsibility. Movement waits for installed support instead of entering unknown volumes.

Sparse edits are indexed in three dimensions. There is no fixed site count, cut count or ten-metre depth limit. Finite world bounds and a ±50 km supported vertical edit range remain. The pick uses a 0.75 m sphere; soil/sand tools use 0.95 m. The visible 64-segment target ring projects the same aim, centre, material and radius onto the actual solid surface, with cyan/red validity feedback and a four-metre reach. Warm carried illumination supports underground visibility.

Archive schema 6 / living state 3 / cave generator 1 retain cuts and underground explorer position, perspective and airborne velocity. Older incompatible archives remain untouched and require their matching historical builds. No silent regeneration occurs.

Portable checks cover graph loops, vertical extent, real openings, concurrent deterministic reconstruction, bounded caching, matching mesh boundaries/normals, cancellation, clipping, many sites/cuts and deep archive round trips. Unity Play Mode verifies installed ownership and replacement collision with the real controller. The current Windows audit enters a hillside portal, walks a real passage, targets/mines a wall, and reloads underground state. Exact evidence and limitations are in [the current validation report](ALPHA_v0.0.9_TEST_RESULTS.md). These are real rendered-game observations driven through gameplay code; physical human input and full-world cave coverage are not claimed.

## Original design contract

The terrain/environment checkpoint builds remain preserved before this work. The underground phase is unfinished until its real controller, mesh/collision ownership, persistence and native traversal checks pass.

Natural caves will be deterministic, lazily generated three-dimensional systems tied to geological provinces. Each system must contain winding tubes, connected loops at different depths, branches, chambers, shafts and a real opening through the surface. A spatial index will bound nearby field queries; no Colossal-wide detailed cave meshes will be generated.

One positive-solid field will combine the existing triangular terrain surface, natural cave air and persisted spherical excavation cuts. Streamed 16 m cubes will use the same field for visual geometry, normals and collision. Installed cube ownership will be published atomically with its collider, and a bounded three-dimensional GPU ownership texture will remove only the replaced portion of the heightfield. Underground cubes must not remove unrelated ground above them. Neighbor cubes must sample identical boundary coordinates.

Excavation edits will remain sparse authoritative data, indexed in three dimensions for local rebuilding. Remove the sixteen-patch, thirty-two-cut, gentle-slope, reserved-border and ten-metre-depth restrictions. Affected chunks must retain their previous collision until a coherent replacement is ready. The targeting circle must use the actual hit, normal and excavation radius, including walls and ceilings. Save/load must reconstruct identical edited solids and retain underground player poses.

Generation/archive/living-state versions must explicitly identify incompatible underground contracts. Existing worlds remain untouched with their historical builds. Initial cave field tests precede integration; field-only tests are not evidence of traversable caves. Native acceptance must enter and traverse real meshed passages, excavate across chunk boundaries and at multiple depths, save/reload, and repeat movement/stability checks.

Final review also retains the open distant narrow-bank sampling issue. No major requirement may be silently dropped while introducing underground features.
