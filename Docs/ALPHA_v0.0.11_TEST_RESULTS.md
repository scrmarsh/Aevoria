# Alpha v0.0.11 validation and reproduction

Alpha Development package, October25-26,2026UTC. Unity6000.3.27f1, Windows x64, NVIDIA RTX3060Ti. This report records executed checks and their limits; it does not declare a stable public release.

## Results

| Check | Executed outcome |
| --- | --- |
| Windows build50 | Clean compile/shader/runtime-log checks; default Direct3D11 |
| Editor full129 | Originally124PASS/5failures; all five repaired and passed focused reruns |
| Current ecology3/civilization suite |24/24PASS; cold-forest biomass, annual continuation/reload, society determinism and resident contracts |
| PlayMode current full15 |14PASS/one300s integration timeout |
| Colossal flight rerun |1/1PASS in245.23s with600s budget, unchanged movement/origin/precision assertions |
| Native controls and restart | Live capture conflicts/cancel/remap, actual movement/wheel/flight/Escape, preference restart and isolated defaults PASS; two exits0 |
| Native settlements | Paid housing/growth/paths, moving residents, actual tool-ray injury/death, exact population/resource effect and reload persistence PASS; exit0 |
| Native cave on final runtime | Grounded mouth entry, interior movement, mining, exact pose/cut reload and character actions PASS; exit0 |
| Native final terrain/materials | Overview,23.9km mountain, ordinary terrain/plateau, desert/forests, river/lake/coast, nine zoom levels and moving frames PASS; exit0 |
| Current Colossal pure generation | Completed in220.81worker seconds, all13module timings recorded,28.89km refined peak |
| Preservation |401authored v0.0.9 files/182runtime files and468v0.0.10 source files/184runtime files verified; Chronicles and Git index hashes unchanged |

The full Editor and PlayMode results are multiple runs with focused repairs, not a claim that every test passed together in a single current run. Older runtime evidence is qualified below.

Raw results: [Editor24](Validation/Alpha-v0.0.11/editor-ecology3-society-24.xml), [Editor heavy four](Validation/Alpha-v0.0.11/editor-heavy-four-rerun.xml), [PlayMode full](Validation/Alpha-v0.0.11/playmode-current-full.xml), [Colossal rerun](Validation/Alpha-v0.0.11/playmode-colossal-final.xml), [preservation](Validation/Alpha-v0.0.11/final-preservation-check.json), [native gallery](ALPHA_v0.0.11_NATIVE_GALLERY.md).

## Reproducible worlds

| Purpose | Seed | Size | Mountain relief | Other settings |
| --- | --- | --- | --- | --- |
| Final geographic/water review |shaping-continents|Miniature|16000m|Continents, default climate |
| Controls, settlement residents |shaping-continents|Miniature|3600m|Continents, render radius2/near0/medium1 |
| Cold conifer comparison |shaping-continents|Miniature|3600m|Continents, temperature offset-3C, rainfall1.3, latitude43 |
| Walking cave |shaping-continents|Miniature|Fixture defaults|Entrance(-31574.294,770.045,2734.754); additional seeds aevoria-001 and831904722 passed earlier walking fixtures |
| Current Colossal diagnostic |aevoria-001|Colossal|16000m|Continents, default climate |

Final Miniature physical fingerprint is `B76894E7FEC356BB6AF67BDC805F16C9FFB758468C1B215A6E3BD9625F8DF9E9`. It is unchanged by rock, cloud, ecology and final water-material changes. Current Colossal fingerprint is `5410041C463F6ACCCB669529C0A07985C5D98661C87D3F348F03C489FB82917C`. Explicit seeds retain exact text; blank seeds resolve once at creation and are stored in the world.

Run from the repository root with a fresh evidence directory:

```powershell
./Tools/RunAlpha011InputReview.ps1 -BuildDirectory Builds/Alpha-v0.0.11 -OutputDirectory Temp/MyInputReview
./Tools/RunAlpha011SettlementReview.ps1 -BuildDirectory Builds/Alpha-v0.0.11 -OutputDirectory Temp/MySettlementReview
./Tools/RunAlpha009UndergroundAudit.ps1 -BuildDirectory Builds/Alpha-v0.0.11 -OutputDirectory Temp/MyCaveReview -Seed shaping-continents
./Tools/RunAlpha011Prototype.ps1 -BuildDirectory Builds/Alpha-v0.0.11 -OutputDirectory Temp/MyTerrainReview -Review Full -Relief 16000 -Zoom
```

Native fixtures use isolated D:/AevoriaValidation save roots; ordinary play uses the application's normal saves. `Full` terrain review does not execute the prototype-only physics-contact branch. Cave, traveler and mountain-flight checks independently exercise collision.

## Performance scope

[Current Colossal result](Validation/Alpha-v0.0.11/Current-Colossal-16000/result.json), [per-module timing](Validation/Alpha-v0.0.11/Current-Colossal-16000/module-timings.json), and heartbeats record actual current source. Worker generation took220.808s; hydrology accounted for219.331s, with real basin-refinement progress. Peak private memory was1761.02MiB and peak working memory1433.59MiB in this pure.NET process. Concurrent Unity work ran on the machine. These values are neither Unity-player memory nor an isolated baseline comparison. The older unknown-seed Colossal stall was not reproduced and its cause is not established.

The default Direct3D11 native terrain run recorded360uncaptured moving-zoom frames: median9.8456ms, p9512.5173ms, max43.5114ms, zero above50ms. Concurrent Unity work was active. PNG-capture timings are excluded; these are engine-loop timings, not independently measured display presentation. Final material retake records another raw trace beside its screenshots. Earlier CPU-copy cleanup reduced a Colossal native peak working set from4251.28 to3474.74MiB with equal fingerprints; private committed memory did not return to the older baseline. No total-allocation or universal frame-rate claim is made.

## Compatibility and limits

New worlds use generator10, schema8, geology6, climate5, hydrology12, water layout8, caves2, ecology3 and civilization2. Incompatible archives are rejected explicitly. Older worlds require preserved matching v0.0.9/v0.0.10 players; no save is deleted or silently migrated. Aggregate population/resources/history and long continuation remain deterministic under matching models. Resident physical positions are saved presentation-driven state; authority comparisons exclude only those coordinates, while native checks separately verify pose/health/death persistence.

Windows Application Error records exposed D3D12Core access violations during shutdown. Earlier completion tokens establish particular gameplay/render observations but do not prove clean process exit. The current default Direct3D11 reviews record exit0. The sampler retains the exact process handle and native validation rejects missing/nonzero exit status. Driver-wide compatibility is not established.

Remaining alpha limitations: stylized procedural assets and facial likeness; hard material boundaries and some confluence transparency; bounded representative NPCs with simple walking, rather than individual economies or advanced AI; a console foundation without shipped gameplay commands; no universal seamless-LOD or long-soak guarantee. Several earlier cave seeds establish traversal, but their Direct3D12 shutdown status was not accepted. The current final-runtime cave fixture closes that clean-exit gap for one seed.
