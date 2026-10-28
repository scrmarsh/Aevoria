# Mediterranean geography development evidence

This is an unfinished development update. No v0.0.13 release has been published.

## Latest continuation: drainage and northern hemisphere climate

Drainage6 representative native water review exited0. Close/reverse views and
zoom14000/motion120 captures were inspected without overlap triangles or clipping
gaps. Streaming-ready waits and synchronous still captures corrected misleading
old-pose audit images; river stencil compositing removes repeated transparency.
All-size/confluence acceptance remains part of final validation.

Climate6 replaces symmetric absolute latitude with20..75 degrees south-to-north
across every supported size. Physical ocean distance moderates temperature; wind
advection, uplift and rainout shape moisture. Gradual atmospheric recycling allows
rain shadows in dry interiors. Five-size focused climate suite passes46 checks;
drainage regression15 checks and full geography/save/rebuild/cancel integration
pass. Cancellation latency0.0012s. See phase4-climate-checks1.json,
phase4-drainage-regression1.json and phase4-integration1.json.

Physical terrain profiles for Miniature and Colossal seed aevoria-001 show southern
desert, central Mediterranean/grassland/conifers and northern boreal/tundra.
These are provisional pre-hydrology samples. The later, warmer-central curve
passes51 focused checks and19 drainage checks (including cold shallow retention),
plus save/rebuild integration2. Current native Miniature/Colossal reviews both
exited0; overview/temperate forest/boreal/tundra images were inspected. This passes
the basic phase4 climate placement gate, recorded in phase4-native-acceptance.json.
Final visual remaster, all-size/extreme gameplay validation and release remain open.

Colossal aevoria-001 settled in66 passes, with native generation completing403.2s.
The old five-minute audit cutoff was too short for this progressing run. The
corrected audit uses660s against the pipeline's ten-minute generation budget.
The separate pure convergence profile completed357.6s excluding geometry.

The v0.0.12 runtime and source manifests were verified before development. The
protected character files were compared again during this continuation and
remain unchanged. Existing worlds are not used as test fixtures.

## Mountains

The shared physical surface now uses curved plate faults and uplift, connected
massifs, multiple summits, saddles and subordinate ridges. Native prototype2
exposed detached fin-shaped branches; prototype3 attached their roots to the
actual curved parent ridges and widened their support.

The extreme Miniature prototype3 then exposed a separate fixed-width coastal
cutoff. Prototype4 scales coastal support with local mountain height. Its native
views show slopes replacing the narrow curtains. Forty-five collider probes
passed in each of the two latest fixtures. Maximum discrepancies were:

| Prototype4 fixture | Maximum discrepancy |
|---|---:|
| Miniature, `rare-relief-013-4`, relief16000 | See retained contact CSV |
| Small, `rare-relief-013-76`, relief16000 | 0.004466 m |

The Small fixture reaches approximately 32,477 m on the physical terrain. It was
generated with ContinentalField3/BaseElevation4, before the coastline phase.
The old Miniature rare fixture no longer reaches33km after correcting its
supporting slopes. The failed search is retained rather than relabelled a pass.

`phase1-structural-checks8.json` records44 successful checks including the rare
fixture. `phase2-geology-regression.json` records42 successful base checks after
the first coastline implementation. Final materials, vegetation and all-size
mountain presentation remain open.

## Continents and coasts

ContinentalField4 reserves physical ocean corridors before calibrating land
coverage. It adapts their maximum width to the actual space available for the
requested land fraction. The finite perimeter remains ocean.

CoastalLandformField1 is an explicit pipeline product before ComposedSurface3.
It carves actual submerged inlet beds; climate, drainage, collision and scenery
all consume the same shaped surface. BaseElevation5 adds varying coastal shelves
and broad ocean basins. The complete manifest now contains15 products.

The first native coast captures failed placement review: some inlets crossed
flat coastal land or existing bays. Placement now requires actual mountain
support for fjords, progress inland and supported valley walls. Flat northern
coasts retain their natural shoreline. A second native build is under review.
The first overview also exposed insufficient ocean width; the second build has
wider reserved separation.

`phase2-coast-checks6.json` covers Miniature/Colossal, four arrangements and
30/54/85% requested coverage, plus three Small inlet fixtures, physical open
mouths, deterministic composition, mountain support and cancellation.
`phase2-integration1.log` passed the shared-pipeline, settlement support,
drainage acyclicity, catchment conservation, river ownership, archive identity,
save/rebuild fingerprint and deep-flood cancellation checks on coast prototype1.

Coast prototype2 still requires native acceptance. Dense ponds, terrain grain,
vegetation, climate, rendering coverage, roads and menus are later open phases.
Clouds are deliberately disabled by the terrain geometry audit; these captures
cannot establish the state of cloud rendering.
