# Development workflow

Open this project with Unity 6.3 LTS Editor 6000.3.27f1. It uses the Universal 3D URP template and URP 17.3.0. The source is at M:\Aevoria; its Unity Library cache and validation build were generated in a local temporary copy. M:\Aevoria-OLD remains read-only. Blender 5.2 is installed for later reproducible assets; Phase 0 uses no Blender files.

Use local desktop and laptop working copies. Commit Assets with meta files, Packages, ProjectSettings, Docs and tests. The Unity gitignore excludes Library, Temp, logs, builds and generated IDE projects. Synchronize with a private Git remote or another safe backup; none has been created. Avoid importing from a slow USB drive. Each machine should regenerate its local Library cache using the same Editor and package versions.

Portable checks: dotnet run --project Tests/PortableChecks/PortableChecks.csproj. For manual verification open Assets/Scenes/SampleScene.unity and press Play. WASD/arrows pan, Q/E rotate, wheel or PgUp/PgDn zoom, and middle drag pan.
For automated validation on an exFAT removable M: drive, copy Assets, Packages and ProjectSettings to a local temporary directory, then run Unity there; do not store its generated Library in the portable source tree. The Phase 0 validation copy and player build were kept under the local Temp directory.

Manual camera acceptance: in Play mode or a standalone player, tap Q then E and check camera yaw changes and returns; tap PgUp/PgDn and confirm terrain scale changes; tap each arrow key and confirm the camera X/Z readout moves. Then hold WASD and use the wheel/middle drag. Windows automation key injection did not reliably exercise these native input paths during Phase 0 validation.

## Phase 1 workflow

Phase 1 source replaces bootstrap's placeholder with procedural geography and chunk streaming. Open the same SampleScene. Current controls and physical-input acceptance checklist are in CAMERA_CONTROLS.md; current automated/build/runtime evidence is in PHASE_1_TEST_RESULTS.md.

`Tools/ValidatePhase1.ps1 -Action prepare|edit|play|build` is a repeatable Windows validation entrypoint (run one action at a time). It copies Assets/Packages/ProjectSettings to a dedicated AevoriaPhase1Validation local temporary project copy and launches Unity 6000.3.27f1 with a hidden batch window, explicit test XML/log paths and process exit-code propagation. The prepare action can create missing material assets in that validation copy; checked-in source already includes them. Do not edit source in the cache. Player builds and Library/Logs remain excluded from Git.

The optional `-aevoriaBenchmark` player argument runs three static-view seed/preset scenarios with automatic regeneration and writes evidence. See PERFORMANCE_STRATEGY.md for flags and measurement caveats. Running the player normally leaves simulation start/pause under HUD control and has no automatic exit.

Use the full local project path. On this machine `$env:TEMP` contains the abbreviated `SEANMA~1` user path; passing that alias to Unity caused `TypeDB: Assembly index ... was not found` warnings and successful builds whose players crashed loading URP assets. The validation script now derives its path from `$env:LOCALAPPDATA\Temp` and rejects builds with those database warnings. Fresh imports and CleanBuildCache did not fix the abbreviated-path builds; rebuilding with the expanded path removed the warnings and startup failure. Keep the benchmark player visible and unminimized: a hidden player can run Update without rendering. The probe rejects a sample interval without main-camera render callbacks. Run timing captures after the Editor/build process finishes.

## Phase 1.1 current workflow

Current controls, generator version 3 and 16/32/64 km presets are described in CAMERA_CONTROLS.md and TERRAIN_DESIGN.md. The existing Tools/ValidatePhase1.ps1 entrypoint is retained; current builds are written to the validation copy's Builds/Phase11 directory. Run the new opt-in -aevoriaBenchmark11 argument for the 13-scenario motion/forest/large-world/blur suite. PHASE_1_1_TEST_RESULTS.md is the current evidence record; the Phase 1 report remains historical. Resources/WorldEffects.asset retains the optional post-processing variants without disabling project-wide shader stripping.

## Current public target: Alpha v0.0.3

ApplicationVersion in Aevoria.Core is authoritative; build success does not increment or release it. Earlier Phase instructions remain historical. The retained ValidatePhase1.ps1 edit/play/build entrypoints now produce the default Builds/Alpha-v0.0.3 player and generated build-info.json. Use -aevoriaBenchmark003 -aevoriaTrace with an explicit output directory for current evidence. Read VERSIONING.md, ALPHA_v0.0.3_TEST_RESULTS.md and the physical acceptance checklist. Analyze traces with node Tools/AnalyzeFrameTrace.mjs. Preserve previous reports and raw measurements; do not commit/reset/change staging without explicit instruction.

## Current public target: Alpha v0.0.4

Build output is Builds/Alpha-v0.0.4/Aevoria.exe. Run edit/play/build through the retained validation script; it now copies canonical history sources for runtime readers. Use -aevoriaAudit004 -aevoriaTestRoot with a unique Temp path and -aevoriaOutput for actual UI/save-load/environment captures. Never point probes at the normal world library. Retained benchmark flags still work. F3 toggles the developer HUD; Escape opens the world menu. Manual display/camera acceptance and a longer soak remain separate from automated checks.
