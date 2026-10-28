# Active Mediterranean update checkpoint

## Latest state supersedes early phase0 notes below

### ACTIVE CONTINUATION 2026-10-26 — supersedes paused state below

### Latest completed milestone: phase4 basic gate passed

Climate5-Climate-Colossal-aevoria-001 completed exit0 at21:12:04UTC, full terrain
review marker, no runtime failures. Actual generation completed403.2s. Overview,
Forest-close, BorealForest-close and Tundra images inspected. Southern desert,
central broadleaf forest, northern boreal and far-northern tundra are placed in
the intended order. Miniature's equivalent views were inspected and its Full run
also exited0. Current build Temp/Alpha013Climate5/Aevoria.exe. Source/WorldDLL
unchanged fromClimate4; PresentationDLL
AB9822371AD36D07409B19514FBF106902C0D57BFAED50359969D587A217C79C.
Evidence/limits recorded in phase4-native-acceptance.json. External peak working
set4486.24MiB; pure basin profile overlapped part of the native run, so this is
diagnostic evidence rather than an uncontended performance benchmark.

No task-specific players, validation Editors or checks remain running. Unity Hub
serve remains untouched. No commits/staging/releases/archives made. Development
is not marked paused; this is a saved continuation milestone. Next work is phase5
regional landforms/materials and phase6 vegetation. Phases5..10 remain unfinished.
Do not count existing lollipop/cone trees, sparse cover or current flat desert
materials as completed remaster art. Keep all protected v12 character work intact.

Phase5 design considerations (not implemented): dunes must alter the shared
physical height source before hydrology so collision, water and rendering agree.
Use dry/warm climate, sediment, slope and prevailing wind; salt flats need real
dry depositional basins, not white noise patches. Preserve immutable climate
dependencies and explicit typed/module-version identity. Do not silently mutate
the published pre-drainage surface. Consider a separate small-scale landform
product after broad climate, before hydrology, with all downstream queries using
that same product; evaluate its effect on the climate/terrain contract before
choosing the final implementation. No new landform class or module exists yet.

Earlier Colossal audit: Climate4 hit its legacy five-minute harness deadline at
drainage pass55, exit4. Heartbeats continued; no generator stall was established.
Failure is preserved in Climate4-Full-Colossal-aevoria-001. Audit timeout now660s
(pipeline10-minute budget plus publication headroom), and runner supports
Review Climate to omit unrelated mountain/water/coast captures. Climate5 compiled
clean: climate-prototype5-build.log. Climate5-Climate-Colossal-aevoria-001 launched.
A separate pure basin convergence profile completed successfully:
phase4-basin-profile-Colossal.log/json,66 settlement passes,22675 retained lakes,
2063 shallow incisions,357.5947s excluding water geometry. It demonstrates finite
settlement for the current default Colossal seed; native geometry/render review
still pending. These are diagnostic/visual runs, not an
uncontended performance benchmark. Production climate/terrain unchanged since4.

Newest phase4 revision: the initial linear temperature curve produced almost no
central broadleaf forest in terrain profiles. Current Climate6 uses
33 - .0077*latitude^2 before ocean/altitude/settings adjustments. The expanded
51-check suite passes (phase4-climate-checks3.json), including warm central
lowlands. phase4-terrain2-Miniature/Colossal.json profile this revision.
Full integration2 passes (complete log phase4-integration2.log), save/rebuild
fingerprint preserved and cancellation0.0046s. Climate3 build compiled clean
(climate-prototype3-build.log). Climate2 Full native review of the initial linear
curve completed; it is calibration evidence, not current-curve acceptance.
Its first-match biome capture selected a bare cliff for boreal. Audit now scores
woodland by forest potential/slope, tundra by northing, and adds woodland closeups.
Climate4 build clean; native Full Miniature aevoria-001 completed exit0 at
20:54:46UTC, complete terrain marker, no failures. Overview, Forest-close,
BorealForest-close and Tundra inspected: warm south, temperate woodland, northern
boreal habitat, far-northern tundra. Vegetation meshes/understory still need the
planned remaster; these captures validate climate placement, not final art.
Colossal Full current build running in Climate4-Full-Colossal-aevoria-001,
player559908 sampler551928 (verify process identity before action). Colossal
native climate acceptance remains open.
WorldDLL B225FBD5CE44261F8FDD1C621C45D9EFED0DA1008970A56D1DD54D78B1C89170;
PresentationDLL 1CD49099B6637B9E5D44B9904BC0377AC7CC71B081D34243354A2BD292DAFE28.
Climate2 calibration exited0 with complete terrain marker. Current-climate paired
basin profiles:735 lakes187 incisions vs922 with preserved Hydro12/Layout8,
20.3% reduction (phase4-basin-profile-current/baseline.json). Added matching cold
shallow pond fixture; expanded drainage regression2 passed19 checks, including
the shallow warm pond draining while its cold counterpart remains. Both JSON/log
are preserved. All-size/extreme native tests and phases5..10 still unfinished.
Preservation-climate1.json verifies all101 character files and Chronicles unchanged.

User resumed. Phase3 representative native water review passed with Drainage6:
close/reverse, zoom14000 and motion120 inspected, no overlap triangles or clipping
gaps. Process exited0. Final all-size/confluence review remains open. Audit now
waits for current streaming pose and captures still frames synchronously.
Failed nearest-owner clipping (Drainage4) removed. Rivers composite once using
reserved URP user stencil bit8; other water materials retain no-op stencil.
Per-pixel channel depth remains. Geography DLL unchanged from Drainage2 through6.

ClimateMap6 implemented: default latitude20..75, clamped0..88 after user offset;
cold+Z/hot-Z across every size. Ocean moderation and subtropical/temperate moisture
forcing feed physical wind advection. Gradual humidity recycling replaces the
hard floor that erased dry-interior rain shadows. Focused five-size suite passed46
checks: phase4-climate-checks1.json. Warm drainage fixture offset now+10 to preserve
the intended condition; phase4-drainage-regression1.json passes15 checks.
Full persistence integration passed including fingerprint rebuild and cancellation
0.0012s (phase4-integration1.json). Temp/Alpha013Climate1 built successfully;
log climate-prototype1-build.log. Climate2 build adds Mediterranean/Boreal/Tundra
native inspection centres. Native acceptance pending. Physical terrain profiles
phase4-terrain-Miniature/Colossal.json show southern deserts, central Mediterranean
and grassland/conifers, northern boreal/tundra. These profiles are provisional
biomes before hydrology, not screenshots. No official13 release. Phases5..10 unfinished.

Latest reviewed water build Temp/Alpha013Drainage6. Evidence
Drainage6-Water-aevoria-001. WorldDLL
08D9A2F75EF53376B85D8961FB2067D9BE39BFAE8E3EDED3E37B90B46CB2BAC8;
PresentationDLL F60D1AA5CFFF130FCC8C55521BC3CA91A7A9D47C0993D60F05F68DA4FD05FC59.

### Historical pause — superseded by continuation above

User explicitly requested safe stop to preserve5-hour usage. No further work
until resumed. Entry point Docs/AEVORIA_RESUME.md contains current status,
verified evidence, exact next steps and unresolved native defects. All code and
evidence saved; no commit/staging. No Aevoria players, validation Editors or
task-specific dotnet checks remained running at stop. Unity Hub serve untouched.

LATEST BUILD: Temp/Alpha013Drainage2/Aevoria.exe compiledclean, NOT LAUNCHED.
Log drainage-prototype2-build.log preserved. WorldDLL
08D9A2F75EF53376B85D8961FB2067D9BE39BFAE8E3EDED3E37B90B46CB2BAC8
PresentationDLL C2EE4CDF8AB17B35F72A358290C234138EE235942F467693C2DEEB2B6D104D4D.
No official13 published.

Phase3 native1 FullMiniatureaevoria-001 andRareSmall52 both exit0. Actualsummit
search found30281.41m, canonicalcolliderprobes highest30281.516m;54probes max
error.005078125m. Actual-summit image inspected. Highest30kmrequirement now
has actualnativecontact evidence, not just a metadata-centre assumption.

IMPORTANT NATIVE1 WATER VISUAL FAILURES: inspected major-river-close, major-river
and lake images: triangular darkerwedges along banks; substantial river looks
dry outsidefocus; rectangular terrain/water transitions near mouth; ragged
bank/LOD effects. ExactcauseUNPROVEN. Candidate terrainLOD occlusion vs river
transparent overlap (WorldWaterFeatures emits duplicate channel-owner geometry;
WorldWater.shader lacks per-owner clipping, but don't assume overlap is root).
Must resolve before passingphase3. NewDrainage2 may change density/shores but
was not yet rendered. Next runfreshFullMiniatureaevoria-001 onDrainage2 and
useopaque/depth/alpha/no-watercaptures todiagnose.

POND REDUCTION: initial16k m2shallowfilter DID NOTchange representativeworld:
phase3-world-aevoria-001.json and baseline both926lakes, eroded0. Measured actual
basinprofiles: medianarea.207km2, meandepth2.537m, maxdepth6m; mostpotslarger
thaninitialthreshold. Current filter targets warm(>5C), shallow(maxdepth<10m),
isolated(no strongthroughflow) basins below.2km2, keepingoldcriteria fordeep,
cold,large,streamfed and physicallyunbreachable ones. Strongthroughflow runoff
threshold max(1km2,area*10). PublicCanErodeSmallBasin plans incision BEFORE
rejecting: guard against lowering a different labelledreceivinglake, sea level
and >12m sill incision. Preserve blockedbasins ratherthan force-drying them.
Safe profile3:737lakes,189eroded (20.4%reduction) vs926baseline. Profile2
unprotected705is superseded; don't use its24%reduction claim. Read-only
LakeSummaries exposearea/mean/maxdepth/temp forvalidation.

Shore refinement budget1m->16m samples andmaxsubdiv16->128 forlargerworld
shore smoothness; needsperformance/all-sizevalidation later. Currenttests
sill-erosion5 PASS15, baseline5 PASS11; largepositivefixture corrected to
radius300m/slope.005 tobe a physicallylarger lake undernew .2km2criterion.
phase3-integration2 FULLPASS, cancel.0010s, aftereffectivepond/protectionchanges
beforehigherShorebudget (Miniature shoretarget already12m so mostlyunchanged).
New Tests/DrainageChecks --basins mode builds exactlypregeometrytypedterrain/
climate/drainage/layout toprofilefast; --world mode buildsfullgeography.
All baseline hydro/layout reads compile readonly fromArchives/Alpha-v0.0.12-Source.

Next phases4..10 stillunimplemented. Protectedcharacter101filesunchanged,
Chroniclesunchanged. Full brief preservedrequirements.txt. Resume fromthis
section and Docs/AEVORIA_RESUME.md; older checkpointsections are historical.


### LATEST: phase2 basic gate passed; phase3 drainagereview building

NativeCoasts7 Smallfjord7 exit0; broadcovefloor/bay inspected in normal+opaque
views. The longinlandpond leftofcoveisUNRELATEDexistinglake; don'tconfusewith
carvedcove! Actualcoveisviewportcentre (focusp5) and opens tosea right.
RenderprobeCSV points0..9allnegative, regionalmatchescanonicalwithin~.03m,
headpoint10error~.116m stillsubmerged, nooverviewblend. Baymouthconnected.
Phase2basicgeometry/optics gatePASSED; finalart/climate/materials/vegstillopen.
BothnativeCoasts6exit0, stripesgone confirmed, Coasts7exit0. Purecoastchecks11
PASS (includesbroadfloor) andgeologyregression4PASS45. Preservation101character
filesunchanged/Chroniclesunchanged reportpreservation-continuation.json.

LATESTRARE: purephase2-rare-regression PASS47, Smallrare-relief-013-52 actual
max30221.699m onisolatedcomposedsurface. NativeCoasts7-Rare-Small-52exit0,
45colliderprobes maxerror.0074975586m; centreheight25466m! Actualhighestmoves
inlandduetocoastalsupport, so centre isn't30km. DO NOTclaimnative30probePASSED.
Auditnow searchesactualterrainmaximum (49x49 then4refinements), writes
actual-summit.csv, capturesprototype-actual-summit, andadds9colliderprobes there.
BuildDrainage1includes auditupdate. Re-runSmallrare52PrototypeContacts toprove30.

PHASE3 IMPLEMENTED ENGINE CHANGES (notnativeaccepted): HydrologyMap13,
InlandWaterLayout9. Shallow smalllake minimumfootprint rises (16k m2 or8refcells)
whiledeep/largerbasinsretainoldcriterion. Rejectedshallowbasinscanphysically
erodesillsup to12m, lowerwateralongALL originalreceiverbrancheswithsmallgrade;
noReceiverchangesintheseincisions. Planlevelsfirstthencommit; cancellation256
workunits. Deep/geologicallyclosedbasinskeepwaterbudget/oldfallback.
ErodedSmallBasins counter +BasinOutcome.ErodedSmallBasin appended.
Fine-outlet routedpathsnowHermitecurves w/3samplesperrouteedge ratherthanlinear
corners; normalriverpaths/riverownership remainexistingarchitecture.

NEW Tests/DrainageChecks compares currentcodevsreadonlypreservedv12hydro12/layout8
via -p:BaselineHydrology=true -p:IntermediateOutputPath=obj/Baseline/
-p:OutputPath=bin/Baseline/. Same syntheticterrain/climate both versions.
Baseline3: shallow1->1/deep1->1/large1->1. Newerosion4: shallow1->0,1eroded,
ZEROchangedreceivers, actualGroundContactPatchsillincised>1m; deep/large1->1.
Alltopology/order/watermonotonicity/catchmentconservation PASS. Earlierfixtures
1 testsinterrupted during unnecessarygeometry (4km domaincreatesdensechannels);
removedPrepareGeometry becausegraphtestneedsnone. Deepfixture20minitially
belowsea caused bothbaseline/newtolacklake; correcteddepth17m, failuresretained.
phase3-integration1.log FULLPASS inclsave-rebuild,cancel.

Build Temp/Alpha013Drainage1 currently session59142, cacheAevoria010AuditBuild,
lognearfinishing. NEXT preserve drainage-prototype1-build.log thennativeFull
Miniatureshaping-continents or aevoria-001 (compare pondsdensity/riverbanks),
Water reviewSmallconfluences, plusSmallrare52PrototypeContactsactualsummit.
Do not advancephase4 untilbasicdrainagephysical+nativegate. Climate5stillabs
latitude/symmetrical; phases4..10 otherwiseuntouched. Noofficial13 release.


### LATEST: Coasts7 focused review running

Coasts6 bothnative exit0; overview VISUALLY confirmed stripedpatches GONE.
Rootcause proved: BaseElevation5hardclamped6275/32843inlandprobes to1mm.
BaseElevation6uses smooth subsidence against continental support and weights
tectonicrelief tocoast; afterdiagnostic0clampedpoints. Waterregionaldepthfix
compiledandnativefjordwaterworks. Coves stillpinchedatmouth (native6opaque
also triangularwetpatches). Coasts7widensflatcovebed core1.15Width vs.32Width
retainedforfjords. Purechecks11 PASS, includesbroadsubmergedcovewidthcriterion.
Audit7writes inlet-rendered-Kind-index.csv aftereachview Ready withcanonical,
regional, overviewblend andpresentedCameraFloorviaexistingcontrollerfunction.
Use this ifoptics/meshstillmismatch. NativeCoasts7-Small-fjord-7 launched; inspect
launch.json forPIDs; build Temp/Alpha013Coasts7. Build7logpreservedclean.
Coasts6World9041F088A77CE9BC2EC06E3EEF296AF80E3F0BDE30E2A94B118AF721629B6048;
Presentation596B796D...A23EA33. Build7sameworldexceptcovebedwidening, auditdiff.
Integration2completePASS currentsharedphysicsgen inclsave-rebuild and0.0014s
cancel. Geologyregression4 PASS45 includesdatumfloorregression; movedtallfixtures
toSmall becausewiderMiniatureseas legitimatelylimitavailable support, preserved
failedMiniature regression2 ratherthanweakeningcoastalsupportguard. Rare30km
latestfixtureonlyoldContinental3/Base4; needlatestfinalfixtureinphase10.
Coastfixture7nowvalidBase6 too (checks10/11). Bothphase6climate/vegnotdone!
No phase3+engineeditsyet. NEXT aftercove7visual+heightgate -> drainagephase3.
Protectedcharacterhashesagainunchanged; reportpreservation-continuation.json.


### LATEST: Coasts5 optical repair building

Coasts2 native both exit0; fjord6 actual mountainous walls acceptedbasicshape,
cove0 lookedlikecanal. Coasts3shorter/wider/curvedcoves nativeSmallaevoria-001
exit0; imagecove0 lookedsealed/broken despite pureconnectednegativebedchecks.
DIAGNOSED water rendering path: WorldWater.OceanBottom earlyreturned OLD
ReferenceOceanFloor(coast) for oldoceans, otherwise COARSEoverviewheight. This
ignored BOTH new variable bathymetry and narrowphysicalcarvedinlets.
Shader now reads same overview+regionaltriangleheights and matching edgeblend,
removes old coast-depthshortcut. NEED verify native, don'tassume fixed.
Auditnow captures opaque+depthdiagnostic eachinlet toverifyopticaljoins.
Build5session (see latesttools) Temp/Alpha013Coasts5; copiedsourceaftershaderfix.
No Unitycompile resultyet. Onfinish preservecoasts-prototype5-build.log and run
Coast Smallseedfjord-coast-013-7 +FullMiniatureaevoria-001.

Coasts4 is physicalsourcecurrent exceptshader/audit changes. Wideneddesired
oceanseparation further .13 worldwidth adaptivelycapped byeligiblecoveragedomain.
NativeCoasts4 Miniatureaevoria-001 player529144 exited0; Smallfjord7 player528580
exited0. Needinspect Coasts4 overview/Fjord andcoveimages.
Pure phase2-coast-checks8 failed no3oldfixtureshadqualifyingfjordsafterwiderseas;
DO NOTweakenplacement. Searched physicalfixture -> Smallfjord-coast-013-7 has2
valid northernmountainfjords, metadata phase2-fjord-fixture.json. Addedfourthseed
toCoastChecks; phase2-coast-checks9 PASS163. Codecorrectlyleaves3flatcoastfixtures
withoutfjords. Coasts4 worldDLL1556FC7C74BC82B9536C3D7C8DC1107A023F5B278A6A0254FEE18845E500EC36.
Coasts3worldD901FA92...44E20D7. Coasts4 PresentationD79362B9...79EEA84.
TectonicStructureField.cs.meta nowexplicitstableGUID. Charactercodeunchanged.
Phase2 still open; NO phase3+ edits yet. Denseponds, jaggedrendercoast/holes,
climatebothpoles, materialsgrain, vegetation/UI/roads allremaining.


### Latest: phase2 native1 failed visual placement, Coasts2 build

NativeCoasts1 Small shaping-continents and FullMiniature aevoria-001 BOTHexit0.
First fjord/cove images inspected: FAILED fjord placement: trenches crossing flat
coastal land/re-entering existing sea. FIX source now requires450mactualmountain
support9km inland; follows increasing continental interior distance while
seeking low geological valleys; rejects paths crossing pre-existing sea; checks
physical mountain support on BOTH sides of midvalley. Purechecks3+5PASS.
Native overview1 had onlynarrow ocean corridor. WIDENED desired separation from
.014 to.055 world width; calculates max permitted width from actual eligible
domain quantiles so highlandfraction/manyprovincialworlds keepvalidcoverage.
Checks4 caught Colossal54%overconstrained; fixedadaptivewidth; checks5PASS.
First native1 still preserved, do not claim final coast acceptance.
Coasts2 building session13375 toTemp/Alpha013Coasts2 with stricterfjords/widersea,
cacheAevoria010AuditBuild. NEXT native Coast Small aevoria-001 (3fjords fixture),
Full Miniature aevoria-001 for same-seed overviewcomparison.
Integration1 completedPASS inclreceiveracyclic/catchmentconservation/ownership/
save-rebuildfingerprint/cancel0.0016s; geologyregressionPASS42 afterphase2.
CoastChecks adds cancellation and both-bank mountain-support assertions after
checks5; run before gate. No phase3+ code edits yet.


### Latest: phase2 coast implementation and review build

Phase1 coastal support reviewed native4: broad slopes replace fixed narrow
curtains, run exit0. Small rare seed rare-relief-013-76 physical32476.54m,
checks8 PASS44, nativeSmall exit0 and oblique reviewed. Miniature rarely has
enough inland support for30km after fixing coastal slopes; failed search7
preserved. Exact rare fixture8 uses ContinentalField3/Base4/build4, NOT latest.

Phase2 IN PROGRESS (no phase3+ edits): ContinentalField4 now reserves curved
seas between continental provinces BEFORE land calibration; rounded world coast
with high-coverage domain adaptation. 24 size/arrangement/coverage combinations
PASS coverage, open boundary and separated default continents. NEW typed
CoastalLandformField1 after Mountains/Base/Tectonics, before ComposedSurface3:
northern uplifted mountain coast inlets carve physical submerged U valleys;
southern coves/headlands. Spatial bins avoid scanning all features per sample.
BaseElevation5 adds variable shelf width/depth and broad deep basins. Pipeline
manifest now15 products, all downstream sample the SAME final shaped surface.
Pure CoastChecks total166 PASS,9 inlets/3fjords each of3 Small seed fixtures,
connected physically submerged mouth/beds and exact shape determinism.
Alpha009TerrainAudit now writes coastal-landforms.csv and captures up to2 each
fjord/cove in Coast/Full review. Build Temp/Alpha013Coasts1 running session95961
cacheD:/AevoriaValidation/Aevoria010AuditBuild. Do not copy into active cache.
Integration --integration running session (see latest tool IDs/log) output
phase2-integration1.log. NEXT native Coast+Full to inspect actual fjords/coasts,
new overview. Do not pass phase2 from tests alone.


### Continuation: prototype4 coastal support (latest)

Prototype3 ordinary ridges now connect cleanly; native runs both exit0. Rare33km
Miniature still FAILED visually: enormous coastal curtains. Fixed cause: fixed
2.8km land mask compressed extreme mountains into a narrow shore strip. Local
mountain height now scales the supporting coastal taper (height*1.25).
Pure checks5 passed39; checks7 adds coastal ratio bound <=.901 (42 base pass),
rare fixture search still running session70386 (old rare4 now reduced physically).
Do not assume old rare4 still33km. Search skips insufficient actual peaks.
Protected character hashes verified unchanged. Prototype3 collision45rays max
error0.00668945312645519m, both native runs exit0.
Build4 clean complete: Temp/Alpha013Geology4; log geology-prototype4-build.log.
WorldDLL7D9F1F88F8864877C95E341C0C8E77E44D2B1F5B19417F0A6AA9410D014081E3.
Native Geology4-CoastalSupport running player439340/sampler512192, rare4 Miniature
16000 Prototype Motion Contacts. Inspect completion, oblique/overview, collision
and orbit; if no curtains then phase2. No phase2 code changes yet.


### User continued; latest prototype3 work

User said 'continue'; preserve original full13 objective, not a request to stop.
Prototype2 native runs completed exit0, reviewed ordinary and extreme images:
**visual failure: detached fin-like spurs**. Debugged roots were computed on a
straight centre-to-satellite line, missing the actual curved parent ridge; widths
were too narrow for height. Root heights also did not use exact parent samples.

LATEST SOURCE FIX (prototype3 build session43155, Temp/Alpha013Geology3):
- Derive each child root/tangent/height from actual parent polyline interpolation.
- Ridge widths scale with height (.60*mainH/.70*rootH) plus footprint minimums.
- Added explicit test that each ridge root is on another curve in the network.
- Structural tests session48098; inspect output/phase1-structural-checks4.json.
- Rare physical30km fixture FOUND: seed rare-relief-013-4, Miniature, Continents,
  prototypeOnly, relief16000. Prototype2 source sampled height33282.390625m.
  Test optional --rare searches <=192 master seeds, records actual found fixture.
- Prototype2 WorldDLL7346563F...F768727. First prototype WorldDLLA21F8B71...37F1277.
- Geology1 physical contact test completed clean exit0; mountain-physics.csv
  has canonicalHeight/colliderHeight/errorMeters; inspect max (assert <.08m).
- No phases2+ edits yet; don't advance until basic mountain shape acceptable.

NEXT: wait build3 and tests; preserve buildlog, launch ordinary and extreme native
review same runner to fresh directories. Inspect shapes/contact/motion. If shapes
now coherent, advance phase2 without pretending final global art is accepted.

Phase2 DESIGN NOTES ONLY (not implemented): ContinentalFieldv3 currently calibrates
coverage by dropping raw threshold until unrelated lobes join; Continents lacks
the archipelago group-separation constraint. For actual oceans reserve a physical
curved separation margin between continental groups BEFORE coast distance build,
calibrate coverage over eligible land-domain points, then cap raw field at
threshold+seaMargin. Ensure default54% land remains attainable and all supported
land fractions30..85% work (adapt boundary-domain shape/sea width for high land).
Use curved/noise-warped structural ocean corridors, not straight rectangular cuts.
Maintain perimeter ocean. Substantial oceans distinguish continents from rivers.
Fjords likely need a typed CoastalLandformField after base/mountains/tectonics,
before final ComposedSurface, carving cold+northern+mountainous coastal valleys;
all climate/hydrology/render/collision then read final shared surface. Don't mutate
already-published Continents fields downstream. Preserve no-overlay rule.

---

Phase0 COMPLETE: preserved v12, both before native runs exit0 and images inspected.
Before exact version12 generator10 World hash79E35... sameofficial release.
Extreme before shows pyramids; full before has huge tiny-lake fields/flat terrain.

Phase1 IN PROGRESS:
- Added TectonicStructureField.cs (v1), typed14th module 'tectonics', curved faults
  derived from neighbouring moving plate regions, rasterized uplift/rift/fault
  blocks/strike/age. Both BaseElevationFieldv4 and MountainClusterFieldv7 consume
  this SAME input. Shared ComposedSurfaceFieldv2 remains single physical surface.
- Generator11 explicitly breaks old10 geography; save schema8 unchanged (no XML
  format change yet). Old-world validation already refuses unsupported identities
  before backup recovery; originals preserved. Core application version now13.
- Replaced mountain cones with broad supporting platforms, dominant+3/4 summits,
  curved primary ridge/saddle graphs and subordinate branches. Fault-aligned fold
  belts, massifs/coastal/older/extreme types. Ordinary physical rolling hills and
  tectonic blocks also improved. OldModel only inpreservedv12 source archive.
- Tests/MediterraneanChecks new pure .NET10 checks compile Core/World/etc; latest
  phase1-structural-checks3.json/log36 PASS across3seeds in4.95s: determinism,
  finitefault geometry, >1m tectonics-input effect onactualsurface, broad extreme
  support, connected ridges, 18km+ heights withinbounds, surfacecomposition,
  centimetre continuity, nonradial profiles, cancellation during preparation.
- First real Windows prototype Temp/Alpha013Geology1 compiled clean; log
  geology-prototype1-build.log. Native Geology1-Extreme-aevoria-001 exit0; reviewed
  overview/oblique/steep and motion exists. Geology1-Ordinary-shaping-continents
  full run exit0; mountain-range reviewed. Connected ranges are visible, but still
  overly smooth/wall-like: NOT final mountain visual acceptance.
- Geology1-PhysicalContacts actual mountain canonical triangle/collider ray test
  exit0 (27+samples atpeaks? readcontactsCSV). Inspect CSV maxerror; <.08m asserted.
- AFTER prototype1, refined source to sharper ridge cross sections, six attached
  subordinate spurs per main ridge with matching root heights and physical rock
  ledges. Rare summit multiplier2.02 supports30km; added zero-length guard.
  BaseElevation onlyreads3 needed tectonic arrays (instead7) toreduce querycost.
- Prototype2 build currently running (session23425) to Temp/Alpha013Geology2,
  D:/AevoriaValidation/Aevoria010AuditBuild. Do not copy intoactivecache.
  Next save log and launch same ordinary/extreme native fixtures toreview shapes.
- Optional --rare search added to MediterraneanChecks Program, NOT run yet:
  dotnet run --project Tests/MediterraneanChecks -- <output.json> --rare
  Finds a reproducible real master seed with physical >30km relief, max192tries.
- First typed build usesnewMountainVersion7 beforethe rare2.02+branch guard changes;
  prototype2 includes latest sharpergeometry. Prototype saves isolated, noofficial13.

No character modeling/rig/tool/player code changed. Character hashes protected in
baseline, but still need periodic comparison. No agents, commit, staging or
updates toofficial older builds/archives/Chronicles. Remaining phases2..10 untouched.
Do NOT call release complete. Need getphase1 shapes reasonable before advancing.
Important known terrainissues still present: gianttinypondfields (phase3), old
symmetricclimate (phase4), brown/grainy terrain/low vegetation/cloud+horizon+upper
screen streaming issues (phases5..7), oldroads/UI (8..9). Preserve riverlinks.

---

User supplied full v13 prompt in requirements.txt. Full scope in
Docs/ALPHA_v0.0.13_DEVELOPMENT.md; implement phases0..10 and persist to completion.
No user permission/face review gate. **Do not edit character modeling/rig/tool
code** except a necessary regression fix. No agents authorized (developer rule).
No commits/staging; HEAD is unborn. Preserve official9/10/11/12 builds/archives,
Chronicles data and user saves. Hide Chronicles only in UI in phase9; no new book.

Current phase0: baseline.json verifies183 v12 runtime files and1863 archived
source files, captures git status/unborn HEAD/index and character hashes. Previous
full v12 artifacts already preserved and verified; do not overwrite them.

Actual v12 native before runs started via RunAlpha011Prototype.ps1:
- Before012-Miniature-shaping-continents: Full ordinary3600, zoom/regional coverage;
  player308712, sampler468340, isolatedD save root in launch.json.
- Before012-Extreme-aevoria-001: isolated mountain prototype16000, moving views;
  player468668, sampler470584. Process already appears exited; inspect exit/views.
Both use preserved Builds/Alpha-v0.0.12. Sampler writes process-exit.json. Do not
change source geography until phase0 captures are complete/inspected.

No v13 runtime code changes yet. Only baseline tool and development docs added.
Current runtime version remains12; generator10/schema8. Plan explicit generator11
boundary (schema bump only if serialized structure changes); old worlds rejected
clearly and retained, usable in v12. Record module version/seed changes.

Audit discovery: GeologyMap computes soft tectonic fields, but mountains and base
elevation have no geology input! MountainClusterFieldv6 combines radial
exp(-1.8*radius...) peaks, causing needles/cones despite wide foot supports.
BaseElevationFieldv3 mostly coastal ramp +small hillMask. Phase1 should introduce
a typed tectonic structure product consumed by BOTH physical elevation/mountains,
curved fault/fold structures, geological massif/ridge graphs and valleys. Keep
one authoritative surface for downstream hydrology/render/collision.

Current module graph in ProceduralGeography.cs13 typed products, version manifest
WorldGenerationPipeline.cs. Geology6/Continents? /Elevation3/Mountains6/Surface1/
Climate5/Hydrology12. Climate uses abs(latitude), cold both ends. Native/player
render/streaming bugs to investigate in phase7. River connectivity is valued;
don't discard existing drainage system. Caves real/persistent; preserve.

Preserved inspirations viewed: Docs/Validation/Phase11/References/{01-valley,
02-coast,03-continent}. No original additional topographic/hillside photo located
yet, don't falsely claim it. Continue from existing reference/structural needs.

Environment: M:/Aevoria PowerShell; Unity6000.3.27f1 Program Files; Blender5.2.
Bundled Python at C:/Program Files/Blender Foundation/Blender5.2/5.2/python/bin/
python.exe (actual installation folder has space 'Blender 5.2'). Use Python for
JSON/hash work: PS5 Get-Content Raw strings can expand extended metadata under
ConvertTo-Json and exhaust memory. Existing v12 manifest writer shows robust way.

Use isolated Unity caches via Tools/ValidatePhase1.ps1, TEMP/TMP=M:/Aevoria/Temp,
D:/AevoriaValidation/Aevoria010AuditBuild or Aevoria009NativeValidation; never copy
into an active Editor cache. Hidden Unity/helpers, actual players visible.
Hub serve PID127548 is not Editor: never kill it; verify PID/path before actions.
AEVORIA_BUILD_PATH selects fresh Temp/Alpha013... executable path. D3D11 baseline.
Read relevant code with bounded rg, no Windows glob like Tools/Run* as rg path.

Next: finish/inspect phase0 before images/exit; start phase1 coupled tectonics and
mountain prototype. Focus meaningful progress, tests and real native silhouettes.
