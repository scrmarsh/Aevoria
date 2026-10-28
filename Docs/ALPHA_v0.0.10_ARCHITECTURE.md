# Alpha v0.0.10 — architecture audit and implementation plan

October 24, 2026. The v0.0.9 development build, accepted native evidence, source archive and report were preserved before this phase began. See `Validation/Alpha-v0.0.9/delivery-provenance.json`. Its open river-mouth/LOD visual defect, frame-budget outliers and physical-input/soak acceptance remain open; they are not completed features of this phase.

## Existing foundations

| System | Actual state | Use in this phase |
| --- | --- | --- |
| Geography | Functional deterministic generator 9; signed terrain, climate, biomes, hydrology, caves | Read canonical elevation, water and footprint validity; preserve generated geography identity |
| Ecology | Functional bounded regional monthly aggregate authority; fertility, forests, wildlife, environmental events | Geographic production potential and environmental pressure; no citizen-per-animal analogy |
| Resources | Functional sparse traveler harvesting and inventory; Wood/Stone/Soil/Sand | Preserve player edits; add documented aggregate settlement catchments and separate food/timber/stone/ore accounts |
| Clock | Functional fixed 0.25-second tick, retained debt, pause and 0.25–8× speed; four ticks/day, 360 days/year | Schedule society at calendar-aligned monthly steps with bounded work and deterministic catch-up |
| Population/Food | Illustrative WorldState prototype: 24 people, automatic food and random births; no geography or settlements | Replace the player-facing authority with society totals; retain old serialization/test compatibility explicitly |
| Persistence | Functional atomic verified XML archives, backups and reconstruction fingerprints; schema 6 | Add a separately versioned society snapshot and paged history. Never silently seed societies into an old archive |
| Randomness | Saved xorshift stream for prototype; deterministic geographic hashing | Separate society generation/simulation streams from visuals and camera updates |
| Origin/rendering | Functional shared horizontal rebasing and bounded terrain/objects/caves | Store settlement positions as geographic doubles; render only nearby/visible structures with LOD |
| Top-Down/Explore | Functional T, three F5 views, ground/flight/freefall, tools | Add observation controls without changing movement physics |
| Roman | Actual weighted generic skeleton with procedural mesh; appearance and locomotion still in one presentation component | Extract reusable appearance/presentation interfaces after the society foundation works |

Read-only study of `M:\Aevoria-OLD\src\Aevoria.Engine\LivingWorld.CivilizationModels.cs`, `LivingWorld.Civilization.cs` and `Aevoria.EconomyModels.cs` found reusable ideas: deterministic scoped identities, factual event sequences, bounded recent history plus persistent journal, explicit inventories, and construction investment accounted once. Those implementations depend on WPF coordinates and older nation/unit systems. They will not be copied wholesale; no legacy rendering, terrain, saves or repository writes are authorized here.

## Planned contracts

1. Pure World-module society data: stable civilization/settlement/political/event identities, geographic candidate evaluation, cultures/colors, aggregate population and inventories. Candidate generation scales with the five existing world presets and habitable area. Reject water, steep/unsupported footprints and cave openings using canonical queries, not terrain colours.
2. Calendar-aligned monthly production/consumption/births/deaths, finite local capacity, paid construction and stage changes. Migration/limited transfers must conserve quantities. Settlement production is an aggregate catchment model, not a claim that every tree or ore vein is individually harvested. Avoid sharing the player's sparse inventory.
3. Geographic territory on a bounded grid with terrain/water travel costs, multi-source influence and neutral cells. Update from actual settlement strength and geographical connectivity. Political/population/resource overlays read this authority; chunk edges do not own independent political state.
4. Factual dated history with stable monotonic sequence, bounded recent records and immutable archive pages loaded on demand. Snapshot/journal restoration must not emit founding events again. Preserve older pages through duplicate/backup/save operations.
5. Versioned persistence, accelerated deterministic tests, five-size profiling and real native observation before claiming the simulation implemented. Long-term citizens, markets, armies and dynasties receive extension data only; they remain deferred behavior.
6. Distance-aware procedural settlement structures and selection panels; explicit policies for building footprints and development. Terrain/caves/excavation remain authoritative and are not flattened to force a town into an unsafe site.
7. Reusable character presets, then the secondary doctor using the supplied turnaround. The doctor alone has a left-hand cane/right-leg limp and text dialogue. Switching appearance cannot alter society or geographic state. Investigate available editable modeling tools before claiming reference likeness; no flat-image substitute or cloned voice.

## Current phase status

This document preserves the initial audit and planned contracts. Society, settlements, territory, paged history, observation and reusable character presentation now have implemented source and executed validation. See [actual implementation and limits](ALPHA_v0.0.10_IMPLEMENTATION.md) and [validation results](ALPHA_v0.0.10_TEST_RESULTS.md). The complete request and supplied reference are preserved in `Validation/Alpha-v0.0.10/`. Facial likeness, physical controls, high-speed frame-budget tails and extended-soak acceptance remain open.
