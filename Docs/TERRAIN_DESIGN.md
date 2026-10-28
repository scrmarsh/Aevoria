# Terrain design - generator version 3 / Alpha v0.0.3

The authoritative geography remains pure C#, independent of Unity objects and the simulation RNG. One unit is one metre. Geographic coordinates are double-precision east/north; Unity renders east/elevation/north. Signed chunk indices use floor division. The original WPF generator was not used.

## Scale and continental distribution

Small is 16,384 m square (32 x 32 chunks), Medium is 32,768 m (64 x 64), and Large is 65,536 m (128 x 128). Chunks are 512 m wide, with 64 base cells at 8 m spacing. Presets change authoritative extent; they do not instantiate all detailed chunks.

GenerationSettings.TargetLandFraction defaults to 0.675 and supports 0.30-0.85. A deterministic 97 x 97 stratified sample of the warped continental field selects its quantile threshold. This changes coherent continental masks and coast topology before regional relief is added. It does not lift every final height. An independent 129 x 129 elevation sample measured 67.30-67.76% land over three seeds and all three presets. Calibration is for sea level zero; raising the sea floods the same elevations and intentionally changes coverage.

Continental scale defaults to 9 km. Low-frequency continental fields, modest domain warping and finer coast modulation create connected mainland and offshore fragments. An ocean margin keeps authoritative boundary geometry below sea level. Regional fields at kilometre scales leave extensive quiet lowlands, with localized mountain belts and plateaus. Mountain ridgelines, variable profiles and channel-shaped cuts modulate relief; roughly 95 m rock fields add localized geometric roughness. GPU surface detail operates at metre and sub-metre scales. These are artistic, erosion-inspired fields, not physical tectonics, erosion or hydrology.

Generator identity is version 3 plus seed, dimensions and all settings. Previous version previews intentionally change. No geography persistence or migration has shipped; the save-envelope validator is unchanged. Queries remain deterministic regardless of chunk order. Cross-platform bitwise parity is not promised.

## Streaming and LOD

WorldRenderer keeps a bounded local working set. A 768 x 768-cell overview covers the world and a decorative outer belt, supplying fallback during rapid travel. The outer belt uses a separate presentation height function with broad islands and smaller ridged relief, excluded from authoritative geography and vegetation placement. The HUD distinguishes outside-world scenery from playable geography. Authoritative v3 heights and dimensions are unchanged in this pass.

Two cancellable background jobs generate immutable mesh payloads. Visible chunks receive priority, with a small surrounding reserve and predicted camera movement. Projected cell size selects 64/32/16-cell meshes, with hysteresis to reduce oscillation. The desired set is refreshed at 0.3 second intervals, rather than rebuilding terrain each frame. Tile objects are pooled; replaced meshes are released. Regeneration invalidates pending generations and preserves the simulation clock and state.

Shared global height/normal queries match at chunk boundaries and common mixed-LOD vertices. Skirts cover remaining edge differences. Replaced LODs use a 0.45 second complementary screen-door crossfade; a coverage texture applies the same fade to the coarse fallback and its depth pass. The overview does not cast duplicate shadows. Crossfading reduces popping but is not geometric morphing or edge stitching. Detailed coverage is bounded, and silhouette/detail changes can still be visible during very rapid travel.

## Terrain surfaces, water and horizon

The URP terrain shader blends grass, dry soil, wet/dry sand, gray/weathered rock and high snow using height, slope and procedural macro variation. Three world-coordinate projections provide fine surface grain and rock fracture modulation without UV stretching. Elongated weathering fields and surface-gradient normal perturbation add directional rock detail. Detail fades with distance. No external texture or model assets were downloaded; these are procedural shader surfaces, not scanned PBR materials.

Water is transparent near shore and approaches deep blue offshore. It reads the geographic height texture as a fallback and reconstructs the visible geographic surface from URP depth for local shallows. Underwater ground remains visible; a simple depth band supplies subtle foam. Animated, distance-faded normal ripples, restrained highlights and a sky-colored Fresnel approximation replace the previous opaque glossy plane. There is no fluid simulation, screen-space/planar reflection, refraction or real wave geometry.

The ocean extends 600 km across, beyond the far clip. Real world-space decorative terrain, an existing procedural sky, and height-dependent distance haze provide an open horizon with parallax. There are no perimeter walls or camera-facing backdrop images. Outside scenery carries no simulated resources, population or vegetation authority. Extremely distant free travel eventually reaches ocean-only scenery.

## Vegetation

VegetationDistribution deterministically jitters global 16 m cells. Regional forest fields produce dense groves, sparse woodlands and occasional isolated trees. Sea level, elevation and slope reject unsuitable locations. Placement runs in cancellable terrain workers; the main thread uploads cached instance matrices. This is a provisional artistic suitability model, not rainfall/soil/ecology simulation.

WorldVegetation caches placements per active terrain chunk and reuses 15 generated meshes: six tree silhouettes at two LODs, meadow tufts, fern-like cover and rocky outcrops. Regional fields influence variant mixes; deterministic height/width/yaw variation reduces repetition. Smooth density ramps soften forest edges. Near/far trees retain crown centres and use complementary dither from 1,000 to 1,550 m; visibility fades from 3,100 to 3,800 m. Frustum limits bound cost. Cover and outcrops have shorter fades. There is no GameObject per tree, plant or rock. Wind, foliage grain and wrapped light remain approximations; this is no photoreal asset library or ecology model. Outcrops are decorative and do not change authoritative heights/collision queries.

Alpha polish removes repeated horizontal rock strata, adds irregular bedding and filters fine grain by pixel footprint. Beach material width varies by macro geography and slope; steep shores expose rock. Water uses relative eye depth with a soft edge and distance-limited foam. FXAA and clearer high-altitude haze reduce aliasing and excessive washout. Detailed geometry still has 8 m spacing: shading does not create sub-metre shoreline topology.

## Alpha v0.0.4 additions

Generator 4 replaces Chebyshev boundary falloff with a warped radial coast field and changes ridge profiles. Mountain relief validates through the shared 3,200 m ceiling. Existing preset dimensions, target-coverage calibration, cell spacing, terrain LOD topology and camera units remain. Hydrology v1 priority-flood routes a 129×129 world grid from ocean/boundary sinks, then accumulates upstream area. Receivers precede children in flood order; flats have deterministic tie ordering. Basins have a common filled-water surface and spill routes. Rivers widen with accumulation and carve a shared bed queried by terrain, normals and camera. Ocean level floods the same reference-datum landform rather than changing carved elevations.

Inland water uses three owned global meshes at most: channels, lakes and steep-drop ribbons. Lake triangles are clipped against sampled terrain, rather than painting rectangles. This is a coarse analysis model; there is no hydraulic erosion, meander evolution, discharge integration or physical fluid solver. Closed-basin evaporation/endorheic balance remains a limitation.

Climate v1 uses latitude, elevation lapse, prevailing westerly air moisture, ocean recharge, uplift and leeward drying. Interpolated moisture includes drainage proximity and provides biome/fertility data. Surface color, snow and instanced tree eligibility respond to that data. The texture is interpolated; biome labels remain classifications. Calendar v1 retains four ticks/day and 360 days/year; four 90-day seasons affect temperature and weather visuals. Weather v1 is deterministic from world field/tick and local climate, with a single local precipitation pool. Local precipitation is visual feedback, not hydrological mass conservation. New immutable fields are reconstructed only under matching version identities and verified fingerprints.

Scenery continuity follow-up: the high-relief review exposed a case where authoritative land reaches a simulation edge. The renderer now extends the same continental/elevation field into scenery rather than forcing ocean at that rectangle. Extra radial falloff carries the outer field back into ocean. Simulation queries remain bounded; optional camera clearance follows visible scenery outside the simulation. This fixes the field switch without expanding simulated dimensions. Boundary regression checks now compare heights across the seam rather than assuming every edge sample must be below sea level.
