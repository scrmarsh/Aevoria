# Alpha v0.0.7 planetary architecture

This document records the v0.0.7 baseline. Current v0.0.8 sampling, rendering, water-profile and identity changes are documented in [WORLD_RESTORATION.md](WORLD_RESTORATION.md); the original architecture and evidence remain here.

Development implementation; visual and performance acceptance is tracked in [the validation report](ALPHA_v0.0.7_TEST_RESULTS.md). Original builds and archives remain available. No release is declared.

## Independent identities

Application 0.0.7; geography generator 5; archive schema 5; geology 1; hydrology 2; climate 2; ecology 2; regional forecast 2; integrated atmospheric state 1; living state 2; seasonal state 2. Calendar and application-settings schema remain 1. Inland surface layout remains an implementation subordinate to hydrology 2. Legacy Huge is a source compatibility alias for Large used by historical diagnostic classes; the player offers exactly five presets.

## Bounded hierarchy and dimensions

| Preset | Width and depth | 512 m chunks per axis |
| --- | ---: | ---: |
| Miniature | 128 km | 250 |
| Small | 256 km | 500 |
| Medium | 512 km | 1,000 |
| Large | 1,024 km | 2,000 |
| Colossal | 2,048 km | 4,000 |

The world is a finite planar representation of planetary geography, not a spherical mesh or a globe with wraparound. Global geology and drainage use 257 x 257 fields, climate 129 x 129, ecology/weather/wetness/seasonal memory at most 64 x 64. Geology's nine float fields occupy 2,377,764 bytes regardless of preset. Plates grow from 9 to 49. Landmass chains, physical noise scales, latitude span and ocean distance vary with size: larger presets are not merely scaled copies of one height map.

The renderer retains a 768 x 768-cell planetary overview and ocean backdrop, one moving regional clipmap (32/64/128 km), and a bounded neighborhood of 8 m local terrain. Inland water is partitioned into at most 256 spatial batches per class for frustum culling. A fixed 64 x 64 coverage atlas follows that neighborhood instead of allocating a 4,000 x 4,000 CPU color field for Colossal. Shared triangle height/normal textures join local boundaries to regional geometry and regional boundaries to the overview. Native arrays, meshes, textures and cancellation tokens are owned and released by their components. One regional job and at most two local jobs may be outstanding. Regional installation still uploads a whole clipmap and is a possible frame-time outlier.

## Continents, mountains and erosion

Seeded chains of oriented continental lobes, coordinate warping, inter-chain ocean troughs and multi-scale coastal variation form selectable Single, Continents, Supercontinent, Archipelago and Mixed layouts; Random resolves deterministically. An irregular ocean envelope confines analysis without cutting coastlines along a square edge. Quantile calibration targets the requested land fraction; erosion and ocean-cell interpolation cause modest differences in final measured coverage.

Jittered plates carry seeded motion vectors. Relative normal and tangential motion across nearest-plate boundaries produces convergent uplift, divergent rifts and transform stress; low-amplitude ancient relief and localized volcanism supplement connected belts. The height field combines broad stable terrain, foothills and folded detail in physical coordinates. The mountain control allows 0 to 16,000 m of theoretical relief; it is not an assertion that every preset or seed reaches 16,000 m, nor an absolute sea-level summit cap. Basal elevation contributes separately. This is a tectonic-inspired static construction, not a plate evolution simulator.

Eight conservative thermal transport passes move excess material downhill. Four regional incision/deposition passes move material to lower neighbors. Coordinate detail adds folded ridges, incised texture and actual dune height where climatic rainfall is low. This practical erosion proxy does not solve long-term sediment mass balance, full hydraulic erosion or geological time.

## Climate, biomes and ecology

Latitude span grows from 30 degrees to at most 160 degrees across the map; the creation latitude offsets it. Ocean-distance flood fill estimates continentality. Latitude circulation bands advect ocean moisture across elevations, adding windward precipitation and leeward drying. Temperature includes latitude and a 6.5 C/km elevation lapse. Biome classification distinguishes tropical rain/seasonal forest, deciduous and temperate coniferous forest, boreal, grassland, savanna, shrubland, Mediterranean, hot/cold desert, wetland, tundra, alpine and polar snow, with ocean/coast categories. The circulation solver is row-based and remains an approximation, not a coupled fluid atmosphere.

Vegetation uses repeatable 16 m candidate cells with coherent stand noise, slope, water and climatic eligibility. Plains and savannas receive lower tree density. Hot forests cannot acquire cold conifers through random selection; cacti require hot desert. Existing four aggregate animal feeding groups, resource depletion and inventory remain authoritative. Migration uses a canonical undirected edge with both endpoints and at most about 64 interior samples at preset scale. The coarsest worlds can miss barriers narrower than that regional sampling; camera movement never creates ecological populations.

## Water authority and depth

Priority flood produces an acyclic receiver network and rainfall-weighted discharge. Curved continuous channel paths drive carving, surface geometry and water queries. Connected refined basins share one level and undergo a supply/evaporation plausibility check. Rejected coarse reservoirs are breached along their receiver corridor, then downstream levels are relaxed to restore monotonic drainage. River outlet channels are retained through lake nodes; accepted lake footprints own open water and clip river triangles. Iterative refinement remains a bounded approximation, not a full local watershed solve at every metre.

Lake footprints are sampled no coarser than world-width/4,096 subject to a 384-cell per-basin span bound and an 8 m floor. Consequently Colossal lakes remain coarse. Larger-world narrow rivers can be hidden by distant terrain approximations until local geometry arrives. These are open visual gates, not evidence of fully resolved river/coast rendering at all scales.

Seabed construction includes shelves, deep basins, rift-related ridges and convergent deepening. The ocean shader uses continuous overview bathymetry outside local depth coverage, eliminating the former rectangular constant-depth fallback. Scene depth controls absorption/transparency nearby. Deep water is navy; turquoise is conditioned on shallow warm ocean. Waves are visual normal/ripple effects, not physical wave geometry. Freshwater vegetation, seabed sand/rock, kelp, seagrass, coral and estuary classifications are deterministic habitat foundations only. Salt-lake/inland-sea classification, tidal systems and marine populations remain future work.

## Regional weather and saved memory

Advected seeded fronts sample location, world day, climate moisture and local lapse temperature. Cloud coverage controls precipitation eligibility; snow requires cold precipitation. A bounded cloud deck and projected terrain/vegetation/water cloud attenuation use the same regional weather texture. Camera-local particles show the corresponding precipitation near the ground. Particle trajectories are visual and intentionally not authoritative deterministic simulation.

Surface wetness integrates daily rain supply and gradual temperature/cloud/wind/humidity evaporation into a saved versioned field. Seasonal snow and ice integrate the same daily forecast. Cached regional forecasts are shared during catchup. Visual wind speed and pressure are model proxies; front advection is not a physically calibrated meteorological simulation. Clouds are a mesh deck with procedural shading, not volumetric clouds. No lightning or full synoptic simulation is claimed.

## Presentation and preservation

Escape contains simulation/save/load/settings/graphics/audio/controls/map/exploration/history actions. HUD retains world, calendar, run state, speed and coordinates with optional independent FPS and frame time. F3 is optional diagnostics with unsupported counters marked N/A. F4 uses actual regional layers and separate legend/events tabs. M, minimap settings, F11, combined camera movement, first/third-person experimental exploration, tools, inventory and sparse solid excavation are retained. Audio currently controls master volume as a foundation; no music library is introduced.

Terrain detail/distance, mountains, regional distance, vegetation distance/density, water, shadows, cloud detail, atmosphere, fog and haze have separate controls. Fog-off disables fog and custom haze; analytic altitude attenuation avoids density lag during rapid zoom. Graphics quality changes preserve model authority.

Archives retain atomic temporary-file replacement, checksum and backup behavior. New worlds save all configuration and geography fingerprint plus ecological, seasonal, atmospheric and sparse player state. Previous generator/schema identities are rejected and listed as incompatible; no automatic upgrade rewrites old geography. The historical upgrade API now rejects with an explanatory message. Validation uses isolated save roots and never operates on user archives. Original Chronicle chapters and changelog sections are preserved byte-for-byte before appending the new records.


## Alpha v0.0.8 expanded geological forcing

The expanded build uses generator 7, geology 3, hydrology 5 and climate 3. It preserves the five exact world dimensions and the theoretical 16,000 m relief control. New terrain uses a triangular gradient basis, conservative distributed thermal transport and a soft summit envelope. Continental interiors no longer receive the old artificial bisector sea canals; Archipelago mode instead constructs separated warped island domains before coverage calibration.

Tectonic uplift, rift and shear use smooth distance-based plate memberships. Materially contributing plate pairs blend their physical forcing and stable ridge phases before rasterization. The hard nearest/second-nearest switch previously reversed ridge orientation at a boundary and stepped forcing when a neighbor changed. One added bounded float field brings the 257-square geological fields to 2,641,960 bytes, independent of world dimensions. This remains a geological proxy, not a full plate or erosion simulation. Continuous raster reconstruction, shared fitted downhill channels and deterministic fingerprint checks remain authoritative. See [expanded implementation](ALPHA_v0.0.8_ADDENDUM.md) for streaming, camera, save, cache and atmosphere changes.
