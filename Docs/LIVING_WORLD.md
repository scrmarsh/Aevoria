# Living World implementation and bounds

Alpha v0.0.6 is a Development build. Application 0.0.6, archive 4, generator 4, ecology 1, inland surface layout 1 and seasonal state 1 are independent identities. Earlier generator geometry and ecology progression are retained. The 3,200 m mountain relief control is preserved.

## Terrain and water

All three terrain LODs contain the full 64-segment boundary, with identical world heights and a two-metre normal stencil. Stitched interior fans replace vertical skirts. Coverage joins use the same sampled triangles as the actual 768-cell overview mesh; adjacent chunks use their shared coverage minimum. Forward, depth and shadow passes share surface displacement and installed excavation masks. A new global field is published with its owner and cleared only by that owner.

The old hydrology grid remains authoritative generator-4 drainage. Presentation refines connected candidate basins, rejects leaking and tiny footprints, and applies a rainfall/catchment versus evaporation proxy. One basin has one level. Piecewise triangle ownership is shared by lake construction, river clipping and map generation. Adaptive clipping resolves lake-entry triangles; a final unresolved tiny boundary sliver belongs to the lake. This is not a hydraulic mass-balance simulation. Legacy ecological water proximity continues to use the original drainage model, so a rejected visual lake can still influence coarse habitat estimates.

Banks use continuous slope, elevation, moisture and channel proximity with wet gravel/sand coloring. Existing carved channel geometry is preserved. Physical sediment bars, full erosion, hydraulic backwater and detailed waterfalls remain development limits. Ocean depth colors blend scene and geographic depth continuously. Ocean-only waves and surf are restrained; river metadata carries direction and energy. Ice has lower reflectance, muted blue-gray color, crack detail and reduced motion.

## Seasons and ecosystems

Regional snow depth and ice thickness advance at seven-day intervals independently of camera presence. Cold supply and warm melt use climate/moisture proxies, not actual daily precipitation conservation. The saved regional arrays and epoch continue deterministically. New worlds and explicit old-world upgrades begin with zero recorded seasonal memory; permanent mountain snow is separate. A small presentation texture interpolates saved quantities. Upward-facing portions of crowns and ground details accept partial snow; green remains underneath.

Existing monthly ecology, four wildlife groups, food limitation, movement, disturbances and F4 inspection remain. Terrain-only LOD work retains vegetation placement. Detailed tree selection now measures stand distance at actual ground height. A 32 m deterministic natural-site grid yields sparse fallen timber, branches, shrubs, flowers, dry plants and rare large boulders. At most 512 nearby details are presented. Harvest changes are sparse IDs, capped at 8,192; availability queries never reset resources on streaming. Eligible renewable wood recovers over twelve world years. Inventory is authoritative and saved. Crates cost three harvested wood; at most 128 persistent objects exist.

## Controls

God camera: WASD/arrows, middle drag and optional edge scroll have independent settings. Input is composed and clamped, gated over UI, outside the window and on lost focus. RMB orbits, wheel zooms, Space/Ctrl moves vertically, Shift accelerates, R resets and F focuses. Pitch permits looking straight up and default collision/follow are off. Recenter changes only the view. Six visible speed buttons use 0.25, 0.5, 1, 2, 4 and 8 times the existing simulation clock.

Explore toggles a same-world prototype traveler. WASD walks, Ctrl sprints, Shift crouches, Space jumps, V changes first/third person, E opens inventory, M opens the map, Tab releases the cursor, 1–6 choose starter tools, LMB uses a tool, RMB places a crate. Starter tools and animation are experiments, not crafting or combat systems. Near collision covers a 64 m neighborhood and installed excavations. Deep water and world edges block prototype walking. Third-person camera collision and safe entry use bounded local checks. Procedural tree and natural-detail physical colliders remain incomplete; terrain and placed crates have colliders. Return-to-traveler and surface recovery controls remain available.

Excavation subtracts ellipsoids from a real positive-solid scalar field and reconstructs a quarter-metre marching-tetrahedra surface with a collider. It can retain solid ground below an opening and a roof above a cavity. The distant terrain remains a height field. Supports at most 16 separate 32 m patches, 32 cuts per patch, 10 m below original surface; each patch requires dry, modest-relief ground and a four-metre border guard. One worker builds at a time. Unsupported border crossing is refused, rather than opening an uncapped hole. This is a bounded proof, not unrestricted caves or a voxel continent. Collider cooking and mesh installation can still cause frame spikes.

## Scale, maps and creation

Original Small/Medium/Large are 16.384/32.768/65.536 km. Huge is exactly 128 km and Colossal exactly 512 km, both using 512 m chunks. Global hydrology remains 129-square, climate 65-square and ecological regions at most 64-square. Overview is 768-square with limited near tiles. Geography/ecology preparation runs in background stages; old jobs are cancelled or their results discarded. No region or distant tree requires an individual GameObject. Colossal is a coarse-global-first mode; detailed local hierarchical climate and drainage are not implemented.

M opens a map with actual geographic sampling, lakes, rivers, elevation/biome shading, camera/player/selected/resource markers, zoom and pan. It resamples 256-square viewport tiles at each zoom. Minimap supports circle, square, rectangle, corner, size, opacity, zoom, orientation and marker preferences. Map biome snow is climatic, not a live seasonal snowpack layer. No kingdoms or settlements are fabricated.

Creation preview debounces a 96-square geographic sample of current seed and settings. Statistics and civilization suitability are approximate environmental heuristics, not generated civilization forecasts. The final world uses the same actual configuration, with normal higher-resolution fields.

## Display and persistence

F11 requests borderless fullscreen at the current window monitor's native dimensions; leaving fullscreen restores remembered window dimensions. Display changes use a 15-second real-time Keep/Revert flow. Unrelated navigation/settings do not request display changes. Physical multi-monitor/Alt-Tab/OS-focus acceptance remains a separate manual gate. Engine API references: [current monitor](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen-mainWindowDisplayInfo.html), [borderless mode](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/fullscreenmode/fullscreenwindow).

Archive schema 4 includes seasonal memory, inventory, traveler pose/view/crouch/mode, resource changes, excavation cuts and placed objects. Clock and ecology keep their earlier validated state. Camera and map preferences live in settings.xml. Schema 3 cannot be silently loaded as schema 4: Load Worlds offers Upgrade copy. It verifies the original checksum/geography, preserves ecology exactly, creates a new ID and seasonal epoch, and leaves the old directory untouched. Earlier unsupported schema identities remain rejected. Tests use isolated roots; they never open normal user worlds.

## Evidence and future work

See [validation report](ALPHA_v0.0.6_TEST_RESULTS.md) for exact tests, native captures, raw frame outliers and open gates. Earlier Chronicles are preserved; Book VI is development narrative rather than simulated civilization history. Production art, sediment transport, unrestricted caves, refined large-world local hydrology, natural-object collision completeness and physical acceptance remain work ahead.

## Alpha v0.0.7 continuation

The current architecture and changed identities are recorded in [PLANETARY_WORLD.md](PLANETARY_WORLD.md). New schema-5 worlds retain resources, traveler state, exploration and seasonal memory and add integrated regional wetness. Generator 5 changes the geography and invalidates earlier generation compatibility; the schema-3 upgrade described above belongs to the historical v0.0.6 build and is unavailable in v0.0.7. Original archives remain intact.

## Alpha v0.0.8 continuation

The restoration update retains archive schema 5 and prior living/environment snapshot identities, while physical terrain and water repairs deliberately advance generator 6, geology 2, hydrology 3 and inland surface layout 2. Historical saves remain intact and require their matching build. Optional transparency/transition graphics fields retain settings schema 1. See [recovery architecture](WORLD_RESTORATION.md) and [executed validation](ALPHA_v0.0.8_TEST_RESULTS.md).
