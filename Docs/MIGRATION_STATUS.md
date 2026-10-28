# Migration status

Phase 1.1 extends the current Unity project with generator version 3, 16/32/64 km world presets, calibrated continental coverage, localized rugged ranges, broad lowlands, procedural terrain surfaces, shallow transparent water, an overview/decorative horizon, projected LOD priorities and crossfades, instanced forests and optional camera motion blur.

Camera terrain following/collision are now opt-in. Default horizontal navigation preserves elevation and permits travel through terrain and beyond simulated bounds. Zoom is faster, sensitivity is adjustable, and HUD/focus guards remain.

Preserved: deterministic simulation RNG, aggregate WorldState, independent fixed-step clock, Start/Pause/Resume/speed controls, save-envelope validator, foundation tests, Unity/URP/Input System versions and the Windows build path. Geography regeneration does not reset simulation. Aevoria-old was neither modified nor used as a terrain source. Existing staging/history were preserved; no commit, reset or publication was performed.

Geography/vegetation placement are data-only; rendered scenery is separate from simulation. Climate, fertility, hydrology, ecology, settlements, civilizations and full geography persistence remain unimplemented. Procedural materials/trees are a new visual foundation, not scanned assets or a physical geological model.

PHASE_1_1_TEST_RESULTS.md is the current acceptance record, including comparison with all three supplied reference images. PHASE_1_TEST_RESULTS.md preserves the preceding milestone and short-path URP startup investigation. Physical camera acceptance, remaining reference-fidelity gaps, complete motion/LOD review and long-duration hardware profiling remain explicit.

Current target is Aevoria Alpha v0.0.3 (Development), refining the above foundation with varied procedural assets, improved overview/shore shading, allocation/streaming changes and permanent development history. Public versions replace Phase names in the HUD; historical reports keep their original names. Current evidence is ALPHA_v0.0.3_TEST_RESULTS.md. The release definition, changelog and Chronicles do not change generator identity or add future hydrology/ecology/civilization systems. Alpha v0.0.4 remains planned.

## Alpha v0.0.4 current status

The active target is now Alpha v0.0.4. Generator 4, archive schema 2, actual application menus/persistent worlds and early environmental foundations extend the existing Unity implementation. WPF/Aevoria-old remains untouched. Earlier migration notes above remain historical. Current results and unresolved gaps are in ALPHA_v0.0.4_TEST_RESULTS.md; the v0.0.3 physical/visual follow-up remains carried forward.

## Alpha v0.0.5 current status

The active development target is Living Ecosystems. Generator 4 is retained; archives use schema 3 and ecology 1. Earlier schema-2 worlds remain intact and are rejected pending explicit migration. No migration UI ships. Regional ecosystems and bounded wildlife presentation extend the existing Unity project. Current evidence and open gates are in ALPHA_v0.0.5_TEST_RESULTS.md; the WPF project remains read-only.
