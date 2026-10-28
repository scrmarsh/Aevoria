# Aevoria

Current source: **Alpha v0.0.14 — Project Genesis / Atlas, in development**.
The latest completed milestone remains **v0.0.12**. v0.0.14 has open visual,
performance and release gates; see [active checkpoint](Docs/AEVORIA_RESUME.md)
and [development status](Docs/ALPHA_v0.0.14_DEVELOPMENT.md).

Open this repository in **Unity 6000.3.27f1**; Unity resolves the pinned packages
from `Packages/manifest.json` and `Packages/packages-lock.json`. The Windows
validation workflow is [Tools/ValidatePhase1.ps1](Tools/ValidatePhase1.ps1).
Portable test projects are under `Tests/`. Native audits require explicitly
isolated save roots; never run them against existing player saves.

Git tracks the current project, original source assets, tests, tools and written
reports. Generated players, Unity caches, local historical snapshots, player-save
backups and bulk validation captures remain local. Historical links below refer
to those local artifacts and are retained as development history.

Current development package: **Alpha v0.0.12 - The Character Remaster Hotfix**. [Windows executable](Builds/Alpha-v0.0.12/Aevoria.exe) - keep the runtime folders beside it. [Implementation and limits](Docs/ALPHA_v0.0.12_IMPLEMENTATION.md), [validation](Docs/ALPHA_v0.0.12_TEST_RESULTS.md), [native comparisons](Docs/ALPHA_v0.0.12_NATIVE_GALLERY.md), and [source checkpoint](Archives/Alpha-v0.0.12-Source). The [v0.0.11 build](Builds/Alpha-v0.0.11/Aevoria.exe) and [source checkpoint](Archives/Alpha-v0.0.11-Source) remain preserved.

Typed world generation, real high-relief mountains, hydrology, walking caves, cold forests, persistent boulders, units/bindings, normal Doctor selection and paid settlements with moving residents are implemented. Windows defaults to Direct3D11 after verified D3D12 shutdown faults. Stylized art, some material/confluence transitions, simple representative NPC behavior and soak/driver-wide acceptance remain alpha limits. This is an Alpha Development package, not a public stable release.

**Doctor:** open a world, press Escape, choose **Character appearance**, then **Doctor** and **Return to world**. No unlock phrase is required. See [current controls](Docs/CAMERA_CONTROLS.md).

New worlds use generator10/schema8, ecology3 and civilization2. Open old worlds in their preserved matching builds. [v0.0.10](Builds/Alpha-v0.0.10/Aevoria.exe) and [v0.0.9](Builds/Alpha-v0.0.9/Aevoria.exe), source archives and all ten Chronicles are preserved; no saves are migrated or deleted.
Preserved natural-world milestone: **Aevoria Alpha v0.0.9 — The Natural World Update**, Development. Unity 6000.3.27f1, URP 17.3.0, Windows x64. Application version authority is [ApplicationVersion.cs](Assets/Scripts/Core/ApplicationVersion.cs). Current new worlds use generator 9, geology 4, hydrology 9, inland-water layout 5, climate 3 and cave generator 1; archives use schema 6 and living state 3.

- [Current implementation and acceptance gates](Docs/ALPHA_v0.0.9_IMPLEMENTATION.md), [development checkpoint](Docs/Validation/Alpha-v0.0.9/DEVELOPMENT_CHECKPOINT.md), and [underground architecture](Docs/UNDERGROUND_WORLD.md)
- Previous milestone: [Expanded implementation](Docs/ALPHA_v0.0.8_ADDENDUM.md), [validation and benchmarks](Docs/ALPHA_v0.0.8_ADDENDUM_TEST_RESULTS.md), and [sixteen-theme native gallery](Docs/Validation/Alpha-v0.0.8/Addendum/Comparison.html)
- [Technical changelog](CHANGELOG.md)
- [The Chronicles of Aevoria](Docs/THE_CHRONICLES_OF_AEVORIA.md)
- [Roadmap](Docs/ROADMAP.md) and [version policy](Docs/VERSIONING.md)
- [Controls](Docs/CAMERA_CONTROLS.md) and [physical acceptance](Docs/ALPHA_v0.0.3_MANUAL_ACCEPTANCE.md)
- [Architecture](Docs/ARCHITECTURE.md) and [performance workflow](Docs/PERFORMANCE_STRATEGY.md)

The expanded Windows player is [Builds/Alpha-v0.0.8-Expanded/Aevoria.exe](Builds/Alpha-v0.0.8-Expanded/Aevoria.exe). Keep its runtime folders beside it. Earlier worlds require their matching historical executable; the initial generator-6 v0.0.8 build remains in Builds/Alpha-v0.0.8. Create a new world for expanded geography. A successful build is not a stable-release declaration. Prior evidence and players are preserved. Aevoria-old is read-only.

The integrated natural-world player is [Builds/Alpha-v0.0.9/Aevoria.exe](Builds/Alpha-v0.0.9/Aevoria.exe). Keep its runtime folders beside it. [Executed validation, performance and known limits](Docs/ALPHA_v0.0.9_TEST_RESULTS.md) and the [native comparison gallery](Docs/Validation/Alpha-v0.0.9/Comparison.html) record the preserved milestone. Historical candidates remain separate. This Alpha Development build does not declare stable-release or subjective visual acceptance. The subsequently requested v0.0.10 phase begins only after this package and its source are preserved.

In a world, Escape opens the menu. Graphics includes real quality presets, independent cloud/weather controls and streaming/cache budgets. Preload Visible Area waits for installed terrain and vegetation and can be cancelled. Top-Down zoom is exponential, with ground clearance enforced. Explore: double Space toggles flight; WASD moves, Space rises, Shift descends, Ctrl accelerates, F5 cycles first-person, third-person and reverse third-person; T switches modes while retaining the traveler. V remains a compatibility shortcut. Mountain relief supports up to 16,000 m. The sections below retain earlier milestone descriptions and their original compatibility limits.

## Alpha v0.0.6 living world

Refined lake/river ownership, shared terrain boundaries, persistent seasonal snow and ice, larger worlds, maps and independent controls join the existing ecology. Experimental ground exploration includes starter tools, saved resources and bounded solid excavation. [Implementation and exact bounds](Docs/LIVING_WORLD.md). Archives use schema 4; schema-3 worlds upgrade only through a verified new copy. M opens the map, Explore enters the traveler, V switches views and F11 toggles borderless fullscreen. Book VI is appended without changing the earlier Chronicles.

## Alpha v0.0.5 living ecosystems

Regional vegetation, wildlife, migration, disturbances and persistent environmental memory now evolve independently of visible chunks. Press F4 in a world to inspect its actual ecology and regional maps. Normal menus and the developer direct route remain. [Ecology contracts and limits](Docs/ECOLOGY.md) distinguish implemented aggregate simulation from unfinished animation, disaster effects and acceptance. Schema-2 worlds remain untouched and require an explicit migration; this update does not supply a migration UI. Current saves use schema 3 / ecology 1. [Physical and long-session acceptance](Docs/ALPHA_v0.0.5_MANUAL_ACCEPTANCE.md) remains open.

## Alpha v0.0.4 application and environment update

The current development target is Alpha v0.0.4. See [persistent-world architecture](Docs/PERSISTENT_WORLDS.md) and [validation record](Docs/ALPHA_v0.0.4_TEST_RESULTS.md). Normal startup now opens the main menu. World menu: Escape; create/load/save controls use real user-data archives. Developer direct startup remains available with `-aevoriaDirect`.

Sean's mountain relief parameter now allows 0–3,200 m. This is a theoretical relief setting, not an absolute summit-elevation cap. Generator 4 deliberately changes world geography; it must not silently replace older identities. Prior builds and reports remain in their existing folders. The technical changelog retains its established wording and format.
