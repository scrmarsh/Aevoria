# Alpha v0.0.5 — Living Ecosystems

Development implementation; graphical, physical-input and long-session acceptance must be read with the current test report. Geography generator 4 and the 3,200 m mountain-relief ceiling are unchanged.

## Authority and time

`World/EcologyModel.cs` is pure C#. Regions are stable row-major cells, independent of terrain chunks and camera visibility. The analysis resolution is ceil(longest world dimension / 1,024 m), clamped to 8–64. The standard Small/Medium/Large worlds use 256/1,024/4,096 regions. Cell centers sample authoritative carved geography, climate and hydrological water proximity. Ocean and lake-center regions have no terrestrial populations. This is a coarse habitat approximation; mixed coastal cells are not fully area-integrated.

One ecological step represents 30 days / 120 existing simulation ticks. Twelve steps form the existing 360-day year. `AdvanceTo` retains unfinished monthly time, takes a bounded step budget, and never uses rendered frame counts or wall time. Normal play calls it after the clock; normal clock budgets cannot leave a whole month unprocessed. Dedicated long-run checks explicitly allow larger batches. Initial ecology may begin at a nonzero calendar epoch; this represents initialization then, not invented prior history.

Temperature varies with latitude, elevation, calendar and hemisphere. Near the equator the inspector describes wet/dry seasons. Moisture blends the existing climate baseline, seasonal variation, a saved deterministic random stream and disturbance effects. This is an ecological water-availability model, not simulated rainfall transport or aquifer storage.

## Plants, animals and movement

Six bounded biomass pools represent grass, shrubs, deciduous trees, evergreens, dry scrub and wetland vegetation. Climate-dependent targets represent habitat potential. Seasonal recruitment from a persistent implicit regional seed bank, background mortality, stand age, senescence, browsing, grazing and disturbance losses change actual biomass. Recruitment dilutes mean stand age. Forest recovery and burn-scar decay span years. This aggregate seed-bank model does not track individual trees, explicit seed dispersal or age cohorts.

Four immutable trait records supply stable species identities, names, reproduction/mortality rates, density, movement rates and temperature limits: meadow deer, woodland deer, hares and wolves. Food links distinguish grazing, browsing, mixed small herbivores and prey-dependent predators. Density-dependent births, food limitation, mortality and bounded predation act before migration. Values represent fractional animal equivalents in regional populations. There is no forced population floor. The current four feeding groups and snapshot dimensions are versioned together; adding species requires a deliberate ecology schema change, not just an extra visual model.

Migration uses buffered transfers and stable iteration. Each region/species transfers at most its trait movement fraction to one more suitable adjacent habitat. Births, deaths and migration residuals are tracked separately. Four-neighbor graph edges sample terrain at intervals no greater than 32 m; sea, lake water and terrain above 1,800 m block passage. Steep changes reduce permeability; rivers impose a 0.3 multiplier. These are coarse barriers and crossing costs, not per-species swimming, exact coastline topology or navigation meshes. Visible animals additionally reject local water and steep ground.

## Disturbance and memory

Drought, fire, flood, cold and heat have stable identifiers, duration, severity and persistent regional state. Natural starts depend on moisture, temperature, fuel and water proximity plus the saved random stream. Fire consumes biomass, reduces stand age, leaves a decaying scar and can spread across admissible neighboring edges. Flood increases local water availability and damages vegetation. Cold reduces seasonal productivity; heat/drought reduce water availability. The external `StartEvent` route is a checked deterministic probe interface; it does not bypass terrestrial/fuel/flood-water prerequisites.

The structured history records starts, ends, observed biomass/population loss during events, severe habitat stress and threshold-based habitat collapse/recovery. It does not generate fictional prose. Completed events retain the newest configurable 16–4,096 records (512 by default), plus records needed by currently active events. IDs keep increasing after eviction. Consequence values are observed net losses during an active event, not a counterfactual attribution of every loss exclusively to that disaster. State/history/RNG are all saved.

## Presentation and inspection

F4 or **Inspect ecosystems** opens the regional inspector. Camera focus selects a region. It shows the actual calendar, local season, biome, temperature, moisture, biomass, forest, stand age, burn scar, populations, habitat suitability, stress and active disturbance. Six small regional maps show vegetation, wildlife, stress, biome, temperature and moisture. North is up. Maps are sampled at a bounded cadence; they are not a full-screen strategic map or a new geography authority.

A small linear RGBA ecology texture drives regional forest density, ground productivity, moisture and persistent burn discoloration. Target colors blend at 4 Hz over several seconds. Fixed candidate tree sites include a reserve for recruitment; the same world-space site hash selects survivors as coverage changes. Trees do not randomly move when regions update. These are regional cover proxies, not one tree per unit of biomass. Discrete individual-tree appearance and coarse region boundaries can still be noticeable. Terrain shaders, foliage and foliage shadows share the ecological mask. Existing geology is not reshaped.

At most 128 procedural animal silhouettes are drawn through four shared instanced meshes; there is no GameObject per animal or region. Nearby aggregate populations determine available proxies. Simple resting, wandering/herding, local avoidance and pursuit run at 5 Hz; drawing remains independent. Stable regional identities preserve local positions when a region refreshes. Water/steep-ground rejection restricts motion. These are placeholder deer/hare/wolf silhouettes, not production animation, tracked individual lives, water-seeking AI or complete predator combat. Distant populations continue without visible agents. Visual behavior cannot change authoritative populations.

There are no flame, smoke, storm-damage or dynamic flood-water meshes in this version. Fire damage and recovery are visible through canopy and ground state; flood is currently an ecological effect and inspector entry. The benchmark deliberately labels controlled disasters as probes. Do not mistake them for captures of naturally occurring disasters.

## Save compatibility and lifecycle

Archive schema **3** requires ecology version **1**. Compatible loads restore all plant/population arrays, moisture, stand age, burn scars, event durations/cooldowns/identifiers, history, RNG, epoch and monthly progression. The ecology/calendar relationship and numeric bounds are validated before world installation. Rebuilt geography must still match its fingerprint. Snapshot and inspection copies do not expose mutable authority.

Schema 2 / Alpha v0.0.4 archives require an explicit migration and are **rejected without modification**. No migration UI is supplied in this update. Their metadata remains visible with “Migration required”; the old 0.0.4 player remains available. This intentionally preserves old worlds instead of silently inventing or resetting their ecosystems. Newer schemas/unsupported ecology versions also reject. Existing checksum, atomic replacement, backup recovery and metadata-only recents remain in place. No normal save directory is used by automated audits.

Returning to the menu disposes the ecology view, four wildlife meshes, owned material and two textures, removes shader ownership, and releases the authoritative model. Regeneration starts a new ecosystem while retaining the existing developer-route clock behavior. Normal creation/load uses its own saved clock. No Alpha v0.0.6 social simulation is introduced.

## Validation boundaries

Portable tests cover deterministic cadence, conservation, barriers, growth, food limitation, drought, fire/recovery, save continuation, invalid data and long runs. Unity tests cover model integration, view immutability and cleanup alongside the earlier suites. Actual standalone measurements and screenshots belong in `ALPHA_v0.0.5_TEST_RESULTS.md`. Neither passing arithmetic nor a build proves visual realism, thousands of years of individual animal behavior, multi-hour reliability or physical input acceptance.
