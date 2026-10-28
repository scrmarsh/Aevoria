# Aevoria roadmap

Public versions follow [VERSIONING.md](VERSIONING.md). Future allocations are provisional; intermediate updates may be added. A successful build does not promote a milestone or declare a release.

| Milestone | Title | Scope | Status |
| --- | --- | --- | --- |
| Alpha v0.0.1 | The First Thought | Independent clock/RNG, geography interfaces, persistence validation and Unity foundation | Retrospective label; no historical release date established |
| Alpha v0.0.2 | The Geometry of Existence | Initial terrain, water, camera, chunks and working Windows player | Retrospective label for verified Phase 1 |
| Alpha v0.0.3 | The Correction of Imperfection | Expanded land, world realism, vegetation, shallows, atmosphere, camera, streaming/performance investigation and permanent development records | Prior implemented milestone; [open gates carried forward](ALPHA_v0.0.3_TEST_RESULTS.md) |
| Alpha v0.0.4 | The Emergence of Order | Menus, persistent worlds, drainage, climate, biomes, seasons and weather foundations | Prior implementation; [open acceptance](ALPHA_v0.0.4_TEST_RESULTS.md). Erosion remains future work |
| Alpha v0.0.5 | Living Ecosystems | Wildlife, vegetation ecology, environmental interactions and natural change | Prior implementation; [validation](ALPHA_v0.0.5_TEST_RESULTS.md) |
| Alpha v0.0.6 | The Living World | Seasonal memory, refined inland water, resources, maps and experimental exploration | Prior implementation; open acceptance retained |
| Alpha v0.0.7 | The Shaping of Worlds | Planetary geography, climate, drainage, regional weather, bounded rendering and compact UI | Development; [executed validation and open gates](ALPHA_v0.0.7_TEST_RESULTS.md) |
| Alpha v0.0.8 | The Restoration of the World | Terrain, water, vegetation, cloud and rendering recovery | Development; [validation and open acceptance](ALPHA_v0.0.8_TEST_RESULTS.md) |
| Alpha v0.0.9 | The Natural World Update | Terrain/environment, caves, excavation, weighted traveler and stable geographic exploration | Preserved development milestone; [validation](ALPHA_v0.0.9_TEST_RESULTS.md) |
| Alpha v0.0.10 | The Living World Update | Aggregate civilizations, paid settlements, population/resources, territory/history and reusable character presets | Development implementation; [actual scope and limits](ALPHA_v0.0.10_IMPLEMENTATION.md) |
| Later alpha | Histories in the making | Individual lives, dynasties, markets, roads/shipping, advanced diplomacy and armies | Future behavior; extension data alone does not implement these systems |
| Beta v0.1.0 | Playable core | Essential game fully playable; balancing, polishing and stabilization remain | Milestone decision, not automatic increment |
| Release v1.0.0 | Official release | Finished release meeting its acceptance criteria | Milestone decision |

The original gate before v0.0.4 was to finish or explicitly accept deferral of v0.0.3's physical controls, visual/LOD acceptance and unresolved frame-pacing limitations. Close the documented reference-image gaps. Profile allocations, long sessions and VRAM/presented-frame behavior where tools are available. Sean has now requested the next milestone; open acceptance is carried forward in the current report.

Durable world archives now use versioned geography/settings persistence and tested round trips. Application identity remains independent from generator 4 and schema 2. Existing save-envelope validation is preserved.

The Chronicles of Aevoria record software development. A future in-game History Book records simulated-world events. Their data, narrators and milestones stay separate. A future Chronicle reader may use the chapter metadata; no animated reader ships here.

Historical context remains in PHASE_1_TEST_RESULTS.md and PHASE_1_1_TEST_RESULTS.md, with original evidence paths preserved. The WPF project remains read-only and its terrain generator excluded.
## Alpha v0.0.4 implementation addendum

Current development advances to The Emergence of Order at Sean's request. The v0.0.3 physical-control, visual/LOD and long-session acceptance gaps remain open and are carried forward. Main menu, persistent world archives, hydrology, climate, biomes, seasons and weather foundations are now implemented; [current verification and limits](ALPHA_v0.0.4_TEST_RESULTS.md) distinguish code from accepted behavior. Erosion simulation remains future work. No civilization scope is advanced.

Application 0.0.4, generator 4 and archive schema 2 are separate identities. Persistent worlds use the documented [atomic archive and reconstruction contract](PERSISTENT_WORLDS.md). The original schema-1 validation scaffold and earlier evidence remain intact.


## Alpha v0.0.5 implementation addendum

Sean requested Living Ecosystems. Regional ecology, wildlife, migration, disturbances, environmental memory and inspection are implemented in development; [current report](ALPHA_v0.0.5_TEST_RESULTS.md) records validation and remaining gates. Application 0.0.5, generator 4, archive schema 3 and ecology 1 are separate identities. Before Alpha v0.0.6, prioritize a safe schema-2 migration path, performance outliers, visual/physical acceptance and long-session reliability. Future social systems should consume stable region IDs, fertile habitat, water, forest, wildlife and hazard data; no social simulation ships in this update.

## Alpha v0.0.6 implementation addendum

The Living World update adds refined inland surfaces and common terrain boundaries, deterministic seasonal memory, sparse resources, maps and experimental ground exploration. Huge and Colossal are exact 128 km and 512 km coarse-global-first worlds, with bounded local presentation. Application 0.0.6, archive 4, generator 4 and ecology 1 remain separate identities. Schema-3 migration creates a verified new copy; older unsupported archives remain untouched. See [implementation bounds](LIVING_WORLD.md) and [current verification](ALPHA_v0.0.6_TEST_RESULTS.md). Priorities ahead remain physical/visual acceptance, frame-time outliers, detailed local drainage, natural-object collision, production art, erosion/sediment and unrestricted underground support. No social simulation is advanced.

## Alpha v0.0.7 implementation addendum

Current identities and practical algorithms are documented in [PLANETARY_WORLD.md](PLANETARY_WORLD.md). Before declaring acceptance, prioritize continental/mountain visual credibility, distant channel and shoreline detail, physical controls, native frame-time outliers and the experimental excavation hitch. Salt-lake typing, full erosion/sediment mass balance, spherical topology, volumetric meteorology, marine populations and all civilizations remain deferred. Historical identity statements above describe their respective milestones; the current archive/gen identity is 5/5.

## Alpha v0.0.8 implementation addendum

Targeted restoration preserves planetary geography and living systems. Generator 6 identifies revised physical sampling and drainage; archive 5 and prior saved-state identities remain. See [recovery architecture](WORLD_RESTORATION.md) and [executed validation](ALPHA_v0.0.8_TEST_RESULTS.md). Close remaining visual, physical-input, streaming and presented-frame acceptance gates before declaring the recovery accepted. No civilization scope advances.


## Alpha v0.0.8 playtesting expansion

The current recovery expands under the same application version. Generator 7/geology 3/hydrology 5 supersede the initial generator-6 delivery for new worlds; archive 5 and climate 3 remain. [Expanded implementation and validation status](ALPHA_v0.0.8_ADDENDUM.md) records critical save/camera fixes, physical and material grid diagnosis, streaming/preload/cache work, flight, cloud hierarchy and horizon scenery. Close every remaining native visual, physical-input and performance gate before acceptance; no civilization scope advances. Previous prose and evidence remain historical records of their corresponding builds.


## Alpha v0.0.9 development

The Natural World Update begins with stability and coordinate precision. The ordered [implementation plan](ALPHA_v0.0.9_IMPLEMENTATION.md) gates terrain reconstruction, environment, underground architecture, character/cameras and final delivery on verified foundations. None of these phases is declared complete by the new version label. Older builds, books, archives and validation evidence remain preserved.

### Integrated v0.0.9 milestone and next phase

The integrated natural-world source now implements reconstructed geography/drainage, climate-supported snow and physical clouds, streamed three-dimensional caves and sparse mining, stable geographic controller/collision ownership, a weighted Roman rig, three perspectives and actual loading previews. [Executed validation and remaining limits](ALPHA_v0.0.9_TEST_RESULTS.md) distinguish the preserved Windows milestone from stable-release or subjective visual acceptance. Priorities carried forward include distant narrow banks/shoreline sampling, dense-vegetation frame cost, streaming outliers, procedural-art fidelity, physical-input acceptance and extended soaks.

Sean has requested **Alpha v0.0.10 — The Living World Update** as the subsequent phase. Its priority is geographically valid civilizations and settlements, aggregate population and resource-constrained growth, persistent territory/relationships/history and observable Top-Down tools. A reusable character framework and secondary secret doctor follow the social simulation. Individual citizen lives, dynasties, complete markets, advanced diplomacy and simulated warfare remain long-term systems. Begin only after preserving the validated v0.0.9 Windows build and final report. The original desktop repository remains read-only.


## Alpha v0.0.10 implementation continuation

The v0.0.9 package and source were preserved before society work. Civilizations, paid geographic settlements, monthly aggregate economies/population, conservative overland exchange and colonies, coarse influence/relationships, factual paged history and Top-Down inspection now have executed foundation evidence. Reusable Explorer/Roman/Doctor presentation, selected appearance, secret unlock and offline text dialogue follow that authority. See [implementation and limitations](ALPHA_v0.0.10_IMPLEMENTATION.md) and [current executed checkpoint](Validation/Alpha-v0.0.10/DEVELOPMENT_CHECKPOINT.md). Native refined-character review and full regressions remain distinct from a public release.

Prioritize high-speed environmental throughput and frame tails, supported construction/colonies across more seeds, coastal political-grid fidelity, doctor likeness/cane contact/foot sliding, physical controls and extended saves/soaks. Detailed citizens, historical leadership succession, markets, road/shipping networks, advanced diplomacy, armies and warfare remain future behavior. Preserve explicit archive compatibility and immutable event pages as those systems grow.
