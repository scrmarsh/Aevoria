# Alpha v0.0.10 — The Living World Update

Development implementation. Validation and remaining acceptance are recorded in [the checkpoint](Validation/Alpha-v0.0.10/DEVELOPMENT_CHECKPOINT.md); candidate executables are distinct from the final development package. The preserved v0.0.9 executable and source remain immutable.

## Society authority

`CivilizationModel` is a pure World-module authority. Founding uses deterministic geographic candidates, dry buffered foundations, elevation/slope/climate/fertility/freshwater suitability and cave-opening rejection. Targets scale through 4, 6, 10, 16 and 24 civilizations, but actual suitable spaced land may support fewer. Stable scoped IDs, culture, names, leadership foundations, colors and political identity survive restoration. Rendering and camera motion do not determine the simulation.

The model advances at 120-tick monthly boundaries. Four ticks form a day; twelve 30-day months form a year. Food production and consumption, finite habitat/housing capacity, births/deaths, shortage crises/recovery, paid buildings and paid development follow this calendar. Timber, stone, ore, supplies and food are aggregate catchment accounts. They do not claim to extract every rendered tree or player-accessible vein, and do not share the traveler's sparse inventory.

Annual overland migration conserves population, and same-polity food transfers conserve food. A colony transfers 60 people and its founding stock from a supported parent; the new settlement pays for its buildings from those stores. Candidate route checks bypass the cache of established settlements, so a failed candidate cannot poison later routes sharing the prospective child's ID. Routes are sampled straight overland connections; this is not a shipping or road network.

Geographic influence uses a bounded 32–128 cell-per-axis authority, neutral cells, terrain/temperature/elevation costs and sampled land connectivity. Annual settlement strength changes expand or contract influence. This grid is deliberately coarse, especially on Colossal worlds. Political ownership is not allocated independently by rendered chunks. Border length and resource pressure produce documented relationship trends. Town development can transition a tribal council to a city-state. Kingdoms, armies, wars, citizens, markets and dynasties have extension data only; no behavior is implied by those types.

## Observation and structures

Top-Down offers political, mean polity population-density and geographic resource-potential overlays, settlement markers, selection panels, civilization totals and filtered paged history. Resource layers show production potential, not remaining individual deposits. Select a settlement to focus its location. Controls in the society overview expose pause and 1×/8×/32×/128× requested time speeds.

Procedural dwellings, halls, farms, granaries, storage and workshops derive from actual paid building records. Nearby structures have collision, geographic rebasing and a bounded display-clearance vegetation mask; distant settlements use simplified geometry. The mask is visual clearance, not simulated harvesting. Canonical terrain and caves determine placement; towns do not flatten terrain to force an invalid foundation.

## History and archives

Schema 7 requires version-1 society alongside generator 9 and living state 3. Schema-6 worlds remain with the matching preserved v0.0.9 build; this update does not silently populate or regenerate old archives. Snapshot validation rejects malformed economic values, construction records, ownership, chronology and event references before installation.

Actual transitions emit monotonic dated events. The active window is capped at 512 events; older events are stored in immutable, checksummed 256-event XML pages and loaded on demand. Duplicate worlds and backups retain their page references. Restoration emits no duplicate founding events. Persisted factual numeric text uses invariant formatting. The Chronicle development narrative and a world's simulated history are separate records.

Clock debt is retained. Save synchronization advances bounded batches before capture and pauses further clock advancement while busy. A requested speed is not a throughput guarantee: the first native 128× foundation run accumulated debt and averaged 41.3 ms per measured accelerated frame. No environmental days are silently skipped to disguise that limit.

## Character presentation

Movement/collision/excavation remain in `ExperimentalExplorer`. `HumanoidPresentation` supplies weighted appearance and animation. `CharacterProfiles` defines stable Explorer, Roman and Doctor presets; `RomanSoldier` remains a compatibility adapter. New travelers default to Explorer. Escape → Character appearance selects a preset. Enter `EVERYBODY LIES` there to persistently unlock the optional doctor and display **A Mysterious Doctor Has Appeared**. The selected appearance is saved per traveler; the unlock and subtitle settings are application preferences.

The doctor is authored three-dimensional procedural geometry with a weighted skeleton, dark jacket/trousers, blue shirt and eyes, hair/stubble and a separate left-hand cane. His right-leg stride is reduced and his torso favors the left side without altering movement speed or physics. Existing idle/walk/run/jump/dig/crouch/fly/freefall/landing presentation remains. No voice is cloned. Thirty exact requested lines are available offline as fading subtitles, scheduled roughly every 45–120 active seconds with contextual biome/fall/mining remarks and repetition/cooldown guards. Menus, inventory and maps suppress subtitle overlap.

The exact meshes, skeletons, weights and colors are preserved in [editable Blender sources](../SourceAssets/Characters/README.md). They are procedural approximations of the supplied reference, not a photographic reconstruction. Native likeness, cane contact, gait/foot sliding and subjective art acceptance must be assessed from actual captures; successful rig tests alone do not establish those qualities.

## Preserved limits

The preexisting river-mouth/bank and mixed terrain/water-detail defects, procedural natural-world art quality, frame-budget tails, physical-input acceptance and extended soak remain open. This milestone adds aggregate societies and observation foundations; it does not claim detailed individual lives, full economic markets, armies, historical leaders or universal 60 FPS at accelerated time.
