# Aevoria public versioning

The authoritative application definition is [ApplicationVersion.cs](../Assets/Scripts/Core/ApplicationVersion.cs). Runtime HUD and diagnostics use its display name. The Windows builder derives its default output directory, PlayerSettings bundle version and build-info.json from the same definition. The serialized ProjectSettings value is a Unity-required mirror, checked by an editor regression test; change it alongside the definition when the developer selects a new target.

The current target is **Aevoria Alpha v0.0.11**, status **Development**. A build succeeding does not declare a release or advance a version. Build metadata records UTC build time, not a release date. No Git commit exists in this repository at this audit; metadata leaves the commit unknown rather than inventing an identifier.

The Unity Windows bootstrap executable's PE ProductVersion/FileVersion reports the engine build on this player. Application identity is the Unity bundle version and generated build-info.json, both driven by ApplicationVersion; engine metadata is not relabeled as the game release.

Alpha uses v0.0.1, v0.0.2, v0.0.3 and subsequent developer-selected updates. Beta begins at v0.1.0 when the essential game is fully playable. Official release begins at v1.0.0. Those transitions require milestone decisions, not an automatic numeric rollover. Intermediate Alpha updates may change the provisional roadmap allocations.

Application versions, Unity engine versions and procedural generator identity are independent. Alpha v0.0.8 retains Unity 6000.3.27f1 and archive schema 5, now uses geography generator 7, geology 3, hydrology 5 and climate 3 after the October 23 playtesting expansion (the initial v0.0.8 delivery used generator 6), and preserves the five exact 128–2,048 km dimensions; changed geography must never silently replace an older saved identity. Full archives reconstruct derived fields only with matching generator/environment identities and a verified fingerprint. See PERSISTENT_WORLDS.md.

The Phase 0 foundation and Phase 1 world rendering are organized retrospectively as Alpha v0.0.1 and v0.0.2. These labels were not public releases at the time. Phase 1.1 evidence contributes to the current v0.0.3 history. Preserve its filenames and raw measurements. New evidence belongs in Validation/Alpha-v0.0.11; never overwrite prior milestone records.

CHANGELOG.md is the factual software record. THE_CHRONICLES_OF_AEVORIA.md is its literary companion. Docs/Chronicles/chapters.json supplies stable chapter IDs, version association, status, theme colors, body anchors and evidence references for the development archive. The Markdown remains the readable source and is exported into the runtime chapter reader; prose never serves as authoritative simulation data.

The Chronicles describe development. The future simulated-world History Book will describe events inside individual generated worlds. Neither the narrator They nor development milestones are ordinary characters/events in a world's simulation.


Alpha v0.0.9 uses generator 9 / geology 4 / hydrology 9 / inland-water layout 5 / climate 3 / cave generator 1, archive schema 6 and living state 3. The public version remains independent of these model identities. Historical schema-5 worlds require matching preserved builds; no archive is silently regenerated. The integrated milestone has completed Editor, Play Mode and five-size native checks; its [report](ALPHA_v0.0.9_TEST_RESULTS.md) retains performance and human-acceptance limits. Alpha Development status remains. Sean's requested v0.0.10 is a subsequent development phase, not a retrospective relabeling of this build.
