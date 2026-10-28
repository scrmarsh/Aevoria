# Migration audit

Source: M:\Aevoria-OLD\Aevoria.sln and src/Aevoria.Engine (net10.0-windows, WPF). Destination: M:\Aevoria, initially empty. The source tree has pre-existing modified and untracked files; it was read only. Old saves and generator versions remain untouched.

| System | Class | Decision |
| --- | --- | --- |
| ArcadeRandom.cs xorshift64* core | A | Ported the state transition and double conversion as DeterministicRandom. Removed .NET 10 helpers and clock-based seeding. Saved-world RNG parity is not yet claimed. |
| LivingWorld.Models.cs and GrandModels.cs world metadata | B | Retain seed, coordinates and chunk concepts in a new independent geography model; old WPF and JSON types were not copied. |
| LivingWorld.Nations.cs, NationModels.cs, NationSimulation.cs | B | Stable IDs, ownership facts and optional politics snapshots are valuable; extract from BugManager, WPF Point, terrain and old schema in later phases. |
| LivingWorld.Civilization.cs, CivilizationModels.cs, Cultures.cs | B | Civic facts, settlement state and succession merit a later port with behavior tests. Current files combine WPF geometry, JSON and partial managers. |
| Aevoria.Economy.cs and EconomyModels.cs | B | Treasury invariants and validation merit extraction from civic manager and window code. |
| ColonyWars and ArcadeWorld combat/population | B/C | Review algorithms by feature; current implementation is coupled to old mutable manager and WPF geometry. No Phase 0 gameplay parity. |
| LivingWorld.Store.cs, GrandStore.cs, ColonyWars.Persistence.cs | C | Rebuild versioned persistence. Old JSON worlds cannot load in Phase 0. |
| LivingWorld.Chunks.cs, rendering and BattlefieldWindow files | C | WPF dispatcher, bitmap and controls replaced by Unity presentation. |
| LivingWorld.Terrain.cs, Continents.cs, GrandTerrain.cs | D | Exclude old terrain generator by user direction. |
| Packaging and exploratory diagnostics | D | Historical reference only. |

The old engine DLL cannot be imported directly into Unity because it targets .NET 10 Windows and WPF. Reuse decisions concern reviewed source or algorithms, not binary compatibility.