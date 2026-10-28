# Persistent worlds and application flow

Alpha v0.0.4 development, October 22, 2026. Application version, generator identity and archive schema are independent: 0.0.4 / 4 / 2. The old schema-1 SaveEnvelope remains a validation scaffold; it was never a complete world archive.

Normal startup opens Home. Create validates a name, seed and immutable configuration, generates geography/environment on a worker, captures a fresh simulation, writes its initial archive, then installs presentation. Load validates checksum, supported identities and mutable state before reconstructing geography; a sampled geography/climate fingerprint must match. Unsupported archives stay on disk and require an explicit future migration. No generator-3 migration has been invented.

The repository root is Application.persistentDataPath/Worlds. Unity supplies the writable user-data location; no save depends on the executable's folder. Each world has a GUID directory containing world.xml, metadata.xml and their previous .bak files. Temporary .tmp files are flushed before atomic replacement. Serialization uses bounded XML with DTDs and external resolution disabled; the world payload has a SHA-256 checksum. Metadata is intentionally separate so listing does not deserialize worlds. A stale metadata file after a crash may show an older date/name; loading validates authoritative world.xml. Corrupt metadata, missing payloads and unsupported versions remain listed. A Ready label explicitly says validation occurs on load.

Archives contain name, GUID, seed, creation/save/last-play dates, app/schema/generator/hydrology/climate/calendar/weather versions, dimensions, terrain/climate configuration, fingerprint, tick, population/food prototype state, RNG, speed, running state, retained clock debt, year/day and extension sections. Environment state is derived deterministically from tick/configuration/version, rather than a second mutable random stream. Extension sections are reserved storage, not implemented civilization features.

Save operations serialize through one repository semaphore; serialization and disk writes run on a worker. Current simulation snapshots are captured on the main thread. A failed save leaves the world open. Save As assigns a fresh GUID, including after recovery; duplicate names are permitted. Recovery reads the previous complete backup without overwriting a damaged primary. Saving over a corrupt or unsupported primary is rejected. Delete requires an explicit in-menu confirmation and removes that identity's folder including backups. Autosave is configurable, resets its retry interval after failures, and never blocks normal menu startup.

Returning to Home disables then destroys terrain, water meshes and local environment ownership. Atmosphere and the main camera are retained once. Normal menus do not generate terrain. Escape opens a paused world menu; simulation resumes only when the world page is visible, retaining its previous running/paused setting. Save + return and Save + exit wait for successful writes. Window close also requests a save while a world is open; a failed save leaves the application available to recover.

Settings live in settings.xml beside Worlds. Apply validates and persists independently from world archives; Cancel discards the draft. Resolution and window/fullscreen changes receive a 15-second keep/revert interval; Escape, navigation away, or timeout revert unconfirmed changes. Quality, directional-light shadows, FXAA, VSync/frame cap, motion blur, camera clearance/follow/zoom, and autosave are connected. No audio slider is shown because no corresponding audio system exists.

Developer flags:

- -aevoriaDirect opens a developer world; -aevoriaSeed VALUE chooses its seed.
- -aevoriaBenchmark003, -aevoriaBenchmark11 and -aevoriaBenchmark retain the existing benchmark routes.
- -aevoriaAudit004 runs actual menu-state captures and save/load/rendered scenarios. It refuses mutations without -aevoriaTestRoot PATH.
- -aevoriaTestRoot PATH redirects both settings and archives into an isolated test root. -aevoriaOutput PATH selects evidence output for probes.

Automated menu captures navigate real page controllers and actual saved test worlds. They do not establish physical click, scroll, text-entry, display rollback, alt-tab, or camera ergonomics acceptance.


## Alpha v0.0.5 compatibility

Current archives use schema 3 and require ecology version 1, validated against seed, region resolution and calendar. Compatible saves restore full ecology and deterministic continuation. Alpha v0.0.4 schema 2 requires an explicit migration; no migration UI ships, so load rejects without changing the original. Metadata remains visible. Atomic writes, backup recovery and future-schema protection continue. See [ecology persistence](ECOLOGY.md).

## Alpha v0.0.6 living state

Current archives use schema 4. The full clock/ecology state is joined by version-1 seasonal arrays, traveler pose/view/crouch/mode, inventory, sparse resource changes, bounded excavation cuts and placed objects. Generator 4 and ecology 1 are unchanged. Compatible reconstruction verifies fingerprints and strict limits before creating native objects. Schema 3 requires Upgrade copy: validate checksum and geography, clone ecological state, assign a new archive identity and begin seasonal memory at the recorded tick. The original directory is never modified. Earlier unsupported schemas remain rejected. See [living-world contracts](LIVING_WORLD.md) and [current verification](ALPHA_v0.0.6_TEST_RESULTS.md).

## Alpha v0.0.7 planetary generation

Current archives use schema 5 with generator 5, hydrology/climate/ecology/forecast 2, living/seasonal state 2 and atmospheric state 1. All five new size presets persist exact dimensions and Landmass Type. Daily integrated wetness is saved alongside daily snow/ice. Fingerprints and full mutable continuation are validated before native installation. Old generations are listed as incompatible and require their historical build; Upgrade copy now rejects without writing any files. Create/Load cancellation leaves existing archives intact and releases partially installed presentation.

Escape now centralizes ordinary actions. Graphics include independent terrain, vegetation, water, shadow, cloud, fog and haze controls. Master audio volume is available as a foundation; no music or ambient sound library is claimed. -aevoriaAudit007 runs the current isolated rendered validation; -aevoria007Smoke reduces it to one size without benchmark windows. Each harness flag starts only its own versioned harness. See [current architecture](PLANETARY_WORLD.md) and [executed evidence](ALPHA_v0.0.7_TEST_RESULTS.md).

## Alpha v0.0.8 continuation

The restoration update retains archive schema 5 and prior living/environment snapshot identities, while physical terrain and water repairs deliberately advance generator 6, geology 2, hydrology 4, climate 3 and inland surface layout 2. Historical saves remain intact and require their matching build. Optional transparency/transition graphics fields retain settings schema 1. See [recovery architecture](WORLD_RESTORATION.md) and [executed validation](ALPHA_v0.0.8_TEST_RESULTS.md).


## Alpha v0.0.8 playtesting expansion

The expanded build uses generator 7, geology 3, hydrology 5 and climate 3. Archive schema 5 remains; the traveler optionally persists exploration flight. Save validation uses each world's actual horizontal bounds and a 50 km supported altitude envelope, with separate finite-position and inventory diagnostics. Validation precedes atomic replacement. Earlier archives remain untouched and require their matching historical build; the initial generator-6 v0.0.8 executable is preserved separately.

The versioned terrain presentation cache is disposable. It contains immutable base products and potential placements, with identity checks, checksums and atomic writes. Runtime depletion, excavation, ecological state and inventory are applied separately and are never recovered from this cache. Corrupt cache products regenerate. See [expanded implementation](ALPHA_v0.0.8_ADDENDUM.md).
