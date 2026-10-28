# Architecture

Current target: [Aevoria Alpha v0.0.4](VERSIONING.md). Aevoria.Core owns ApplicationVersion, shared by HUD and builder independently of generator version 4. It also contains opt-in allocation/timing scopes for trace capture. Application status remains Development.

- Aevoria.Simulation: preserved aggregate WorldState, seeded xorshift64* stream and fixed-step SimulationClock, independent of frame rendering.
- Aevoria.World: immutable metric configurations, double coordinates/signed chunk keys, IGeography, versioned ProceduralGeography and stateless VegetationDistribution. No Unity dependency.
- Aevoria.Persistence: preserved save-envelope validator plus checksummed, atomic, versioned world archives and global settings.
- Aevoria.Presentation: bootstrap, free/optional-follow MapCamera, cancellable WorldRenderer, GPU-instanced WorldVegetation, WorldAtmosphere and opt-in benchmark drivers.
- Aevoria.UI: simulation/developer controls, generation settings, optional camera modes, zoom sensitivity and motion blur. Callbacks avoid a dependency on Presentation.

Bootstrap constructs geography, creates presentation services, and keeps one simulation clock across regeneration. The renderer owns detailed, outgoing-fade, overview and ocean meshes plus coverage/height textures. Vegetation owns 15 shared procedural meshes (six tree variants at two LODs, two ground-cover forms and one outcrop) and chunk instance batches. Atmosphere owns cloned Volume components and a cloned sky material. Regeneration cancels jobs and invalidates results; Unity uploads/destroys resources on the main thread. Immutable topology arrays are shared by subdivision count. LOD-only jobs retain vegetation placements; uploads are limited to one per frame, and fades finish before another replacement.

A coarse geographic overview and separate outer scenery give the finite world an open visual horizon. Outside scenery is never queried as authoritative geography and has no simulated population or resources. The camera queries geography only for deliberate focus/regeneration or opt-in terrain following/collision.

The source remains on M:\Aevoria. Unity validation uses the existing local AevoriaPhase1Validation cache with expanded paths, avoiding exFAT cache problems and the short-path TypeDB startup bug. Git staging/history and Aevoria-old are preserved. Earlier Phase evidence is historical; ALPHA_v0.0.4_TEST_RESULTS.md records current validation. BuildWindows writes build-info.json from ApplicationVersion and mirrors its numeric version into PlayerSettings.

CHANGELOG.md is the factual development record. Docs/THE_CHRONICLES_OF_AEVORIA.md and Docs/Chronicles/chapters.json hold the literary development archive and reader metadata. No runtime Chronicle reader, simulated-world History Book, rivers, climate or civilization systems are introduced here.

## Alpha v0.0.4 ownership and persistence

See [persistent worlds](PERSISTENT_WORLDS.md). Presentation now references the existing Persistence assembly; UI has no reverse dependency on Presentation. ApplicationMenu/AevoriaBootstrap own application navigation and world lifetimes. WorldArchive, HydrologyMap, ClimateMap and WorldCalendar contain no Unity dependencies. WorldEnvironment converts pure samples into a global climate texture and pooled local weather; WorldWaterFeatures owns aggregate inland-water meshes. Simulation remains authoritative for elapsed time. Earlier architecture notes above describe their original milestones.


## Alpha v0.0.5 ecology

Pure regional ecology now lives in the World assembly, with no Unity or Presentation dependency. Persistence schema 3 owns a versioned deep snapshot; Presentation supplies bounded instanced wildlife, state textures and inspection. See [ecology contracts](ECOLOGY.md). Generator 4 and earlier assembly boundaries are retained.

## Alpha v0.0.6 living world

The pure World assembly adds connected inland surface layout, deterministic regional snow/ice memory, sparse resources/traveler/inventory/placement and a local solid scalar field with marching tetrahedra. Persistence schema 4 owns their snapshots and explicit schema-3 copied upgrade. Presentation owns all Unity meshes, colliders, materials, particles, background mesh requests, map textures and input. Global geography remains bounded; only nearby terrain/collision/details are installed. All jobs are cancelled or observed on world replacement; model authority never depends on visible chunks. See [implementation and limits](LIVING_WORLD.md).
