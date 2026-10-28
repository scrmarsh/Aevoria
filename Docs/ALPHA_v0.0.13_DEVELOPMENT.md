# Alpha v0.0.13 — Mediterranean Update development ledger

This is active development, not a finished release. Character/face revisions are
deferred. Preserve the v12 character meshes, rigs, animation clips and equipment.

## Phase 0: baseline and architecture audit

`Validation/Alpha-v0.0.13/baseline.json` records the repository's unborn HEAD,
branch, index, protected character files and complete v12 runtime/source manifest
verification. The183-file v12 runtime and1863-file source checkpoint remain at
their original paths. No Git commit is fabricated. Requirements are preserved
byte-for-byte beside the baseline. Old saves are never used directly for tests.

Current save schema8 records generator10 and module versions/seeds. Loading checks
those identities. The new geography requires an explicit generator boundary;
old worlds must remain playable in their preserved v12 player, not be silently
regenerated. No automatic world migration is planned.

Architecture at the preserved v12 baseline:

| Product | Current input/use | Problem or retained contract |
|---|---|---|
| ContinentalField | Seed/settings; coast calibration | Land fraction and coast distance are authoritative |
| GeologyMap | Continents; soft plate stress fields | Computed, but ordinary elevation and mountain placement ignore it |
| BaseElevationField | Continents | Mostly coastal ramp; small masked hills and one plateau |
| MountainClusterField | Continents | Sparse independent formations; exponential radial peak profile creates cone/needle silhouettes |
| ComposedSurfaceField | Ordinary elevation + mountains | Single physical surface shared downstream |
| ClimateMap | Composed surface | Symmetric absolute latitude gives cold regions at both ends |
| HydrologyMap/InlandWaterLayout | Climate/surface | Preserve tributary connectivity, drainage conditioning, authoritative water ownership |
| MoistureField/BiomeField | Climate, hydrology, geology | Add geographical regional transitions in the climate phase |
| CaveNetwork | Settled terrain/geology/climate | Preserve real holes, collision, excavation and saved state |
| WorldMaterialField | Settled surface/geology/coast/biomes | Improve distant filtering and regional materials later |
| EcosystemField | Terrain/biomes/materials | Keep deterministic global placement cells; replace sparse generic assets/distribution |
| CivilizationSiteField | Final geography/ecosystem/caves | Preserve population/resource constraints; terrain-aware road/layout work later |

Rendering uses WorldRenderer, RegionalTerrain, DistantMountainHorizon,
WorldVegetation, WorldWaterFeatures, WorldCloudLayer and WorldAtmosphere. Their
streaming/frustum/cloud problems require native diagnostics; a sky overlay is
not an acceptable repair. ApplicationMenu/CreationPreview own the current menus
and generation atlas. Save & Quit must await a successful save before unloading.

Preserved landscape inspirations at `Validation/Phase11/References` were
inspected: connected ridged valleys, rugged coastal cliffs/forested hills and
continent-scale relief. No claim is made that an additional original topographic
or Mediterranean hillside photograph has been located yet. Reference fidelity
will be distinguished from inferred geological structure.

Known-seed native before-runs use the **unchanged v12 executable**, fresh isolated
saves, `shaping-continents` at ordinary relief and `aevoria-001` at16000m relief.
Raw images, view manifests, actual exit status and process telemetry are retained.
Existing v12 style/character limits remain recorded in the v12 reports.

## Implementation gates

0. Preserve and inspect native baselines.
1. Connect plate/fault structure to actual uplift, massifs, fold belts, ridges,
   valleys and broad supporting mountains. Validate deterministic surfaces,
   cancellation, continuity and Windows silhouettes before proceeding.
2. Improve continental separation, ocean basins, Mediterranean coasts/fjords.
3. Preserve tributaries while refining drainage, pond density, banks and water.
4. Cold +Z north / warm -Z south with ocean moderation, rain shadows and regional
   climate. Version changed generation explicitly.
5. Coherent northern/European/Mediterranean/Asian/desert landforms and soils.
6. Branch/foliage conifers, Mediterranean vegetation and varied ground instances.
7. Diagnose streaming holes/cloud visibility/horizon; improve materials/heat haze.
8. Terrain-following smooth roads and varied settlement layouts, retaining economy.
9. Compass, crisp real-stage atlas, unclipped scaled UI, four-item Escape menu,
   safe Save & Quit, hidden Chronicles and actual new gameplay background.
10. Multi-seed/all-size/extreme native integration, persistence and character
    regression, measured generation/performance, visual review, source/build archive.

Never mark a gate passed from automated checks alone where a visual gate applies.
Preserve failed prototypes, exact reproduction settings and unresolved defects.

## Current implementation

Phase1 has passed its basic structural/native review after corrections to branch
attachments, ridge support and extreme coastal slopes. Final art remains open.
Phase2 is under native review; phases3 through10 remain to be implemented.

Generator11 now has15 explicit products. TectonicStructureField1 supplies actual
uplift/rift/fault structure to BaseElevation6 and MountainCluster7. Continental4
reserves seas before calibration; CoastalLandform1 physically carves supported
northern fjords and southern coves before ComposedSurface3 is published. Every
downstream product reads that shared surface. BaseElevation6 uses smooth inland
subsidence instead of a1mm hard floor, and preserves varied shelves/ocean basins.

Ocean shading is being verified against the same overview/regional physical
triangle fields, replacing a legacy depth shortcut that cannot represent the
new coast shapes. This is necessary for the coast phase's native acceptance.
Failed native prototypes and their exact binary identities are retained under
Validation/Alpha-v0.0.13. GEOGRAPHY_PROGRESS.md and the active checkpoint explain
the failures, corrections and remaining gates. No official13 build is published.
