# Performance strategy - Aevoria Alpha v0.0.3

Target hardware: i7-8700K, RTX 3060 Ti 8 GB, 64 GB RAM, Windows 11, 2560 x 1440. Representative navigation should exceed 60 FPS with acceptable frame pacing. High engine-loop averages alone do not establish stable display presentation. No artificial storage limit applies.

## Working set and confirmed costs

Local detailed meshes remain bounded by render radius/frustum priorities. Two cancellable workers produce data; main-thread uploads are limited to one per frame. LOD replacements wait for existing fades. Immutable topology is shared, unused color payloads removed, and triangle counts use GetIndexCount instead of copying Mesh.triangles. LOD-only work retains vegetation placement. Current/outgoing/base mesh ownership remains explicit; deeper settings-scaled skirts reduce overview/local gaps. A 768-cell overview supplies global fallback at increased GPU/memory cost.

Vegetation uses 15 shared meshes, per-chunk instance matrices, regional variants, frustum/distance limits and complementary near/far dither. Counts include overlapping submitted LOD geometry. No per-tree GameObjects exist. HUD strings refresh at 4 Hz and fixed rectangles replace per-event GUILayout trees. IMGUI and other runtime work can still allocate.

## Reproduce

1. Run dotnet run --project Tests/PortableChecks/PortableChecks.csproj.
2. Run powershell -File Tools/ValidatePhase1.ps1 -Action edit, then play, then build sequentially. The retained tool uses the existing full-path LOCALAPPDATA validation copy and rejects TypeDB assembly lookup failures. It copies the existing project; no second project is initialized.
3. Build output defaults to Builds/Alpha-v0.0.3/Aevoria.exe from ApplicationVersion; build-info.json records target identity, engine, generator and UTC build time. This is not a release declaration.
4. Copy the complete player directory back to the workspace. Launch visibly with -screen-width 2560 -screen-height 1440 -screen-fullscreen 1 -window-mode exclusive -aevoriaBenchmark003 -aevoriaTrace -aevoriaOutput <absolute-directory> -logFile <path>. Wait for all Editor/build compilation to finish first. Keep the player unminimized. The final-code windowed attempt rendered at 1705 x 958 despite its flags; verify actual reported dimensions in every record and require rendered-frame counts to closely track sampled frames. Flags alone do not prove rendering resolution.
5. Analyze CSV frames with node Tools/AnalyzeFrameTrace.mjs <directory>. Keep raw data, marker availability, every outlier, log and screenshots. Store subsequent runs separately.

Fourteen scenarios cover overview, mountains, grassland, coast, water, horizon, forest, continuous movement, rapid zoom, Large overview, bounded Large streaming, matched blur off/on, and low-angle exploration. Initial local readiness precedes two warm-up seconds. Static intervals measure 8 s, movement/blur 12 s, rapid zoom/Large streaming 16 s. Movement does not pause sampling for streaming readiness. Large motion stays inside authority on a 2.5 km-radius loop at 1,600 m/s. Main-camera render callbacks guard against Update-only hidden-window results. Screenshot/CSV writes occur after measurement.

## Profiling evidence and limits

The pre-optimization instrumented baseline preserves all 13 prior paths. Its historical coast/water maxima did not reproduce, but Large streaming recorded 31.84/16.87 ms and blur-off 17.58 ms outliers. The largest had no nearby GC marker; others coincided with a marker within an adjacent frame. These observations rule out claiming a single proven GC cause. Baseline frame-zero collection deltas include earlier warm-up and must not be summed as interval collection counts; use the per-scenario metric sampled at interval start. The final harness resets trace counters at sample start.

Unity profiler recorders capture GC.Collect and available render/present waits; unavailable counters are explicitly marked. HUD, vegetation and upload have profiler markers plus opt-in Stopwatch scopes. A positive-allocation self-test guards GC.GetAllocatedBytesForCurrentThread because a runtime may return zero without supporting the measurement. Unsupported byte counts are -1, never evidence of zero allocation. Scope timings refer to the previous completed frame; engine/GPU counters can lag. Nearby events imply correlation, not causation. Explicit scope instrumentation and tracing add overhead.

Mean/percentile/max engine-loop times, GPU timing, Main Thread marker, process CPU, chunks, terrain/instance triangles, worker CPU, first readiness, stream Update maximum, memory, managed snapshots, GC counts and simulation tick duration are retained. Main Thread includes waits; it is not exclusive script CPU. Memory excludes dedicated VRAM. meshCpuMs counts accepted detailed jobs, excluding overview generation, discarded jobs and uploads; it is not total CPU or wall-clock whole-world generation. Readiness is the first locally ready state after regeneration. Resident terrain now includes overview/ocean/outgoing meshes; the older field excluded them, so compare definitions before counts. Geometry excludes extra shadow/depth passes. The tiny aggregate simulation is not a future civilization workload.

No PresentMon/presented-frame, dedicated VRAM or long-soak result is claimed. The current validation report records remaining outliers and stability gates. A faster/lower-GC run with changed overview/forest detail is not a controlled visual-workload speedup. Physical control and continuous LOD appearance require separate acceptance. Motion blur stays optional and off by default.

See ALPHA_v0.0.3_TEST_RESULTS.md for accepted final measurements. Prior Phase reports and raw evidence remain unchanged.

## Alpha v0.0.4 measurement additions

Normal-menu and direct-world routes are separate workloads. The opt-in Alpha004Audit requires an isolated root and records actual 1440p menu, creation/save/load, drainage, climate/weather, reload, 60-second motion, maximum-relief and menu-cleanup intervals. Its CSVs retain frame outliers/counter availability; audit.json records engine-loop averages/p95/max, rendered count/resolution, process CPU, GPU timing, managed/tracked Unity memory, operation latencies and live service counts. A nonpositive process working-set reading is unavailable (-1), not a zero-memory result. VRAM budget/capacity reported at startup is not live VRAM usage. Keep the historical 14-scenario trace suite for broad travel, rapid zoom, forest, Large and blur comparisons; generation changed, so results are not an isolated optimization experiment. A one-minute motion sample is a short soak, not evidence of multi-hour stability.


## Alpha v0.0.5 ecology budgets

Standard worlds use 256/1,024/4,096 ecological cells, twelve monthly steps per year, at most 128 local wildlife proxies and four shared wildlife meshes. Migration uses reusable buffers; region state is independent of terrain streaming. Visual behavior runs at 5 Hz; state texture interpolation at 4 Hz; map contents refresh at most once per second unless the monthly revision changes. History bounds retain completed records plus active events. Raw long-run CSV, standalone traces and unavailable counters are preserved in the current validation directory.


## Alpha v0.0.8 expanded presentation budgets

The expanded renderer uses one to four background generation workers and at most one local native tile upload per frame. Installed regional publication remains staged; outgoing terrain stays visible until replacement. Frustum samples and configurable lookahead prioritize visible products, while subpixel local cells use regional/overview coverage. High Quality and Ultra adjust real geometry, material filtering, vegetation, shadows, cache RAM and worker budgets. The whole planetary world never remains at maximum detail.

Visible-area preload counts installed local geometry/vegetation plus a tagged regional publication, not queued tasks. Cancellation retains installed resources. The disposable checksummed disk cache stores immutable base products; an independent byte LRU controls cached payload RAM, excluding active meshes, native objects, world models and GPU allocations. Persistent disk storage currently has no global pruning policy. Boundary normals retain common-field samples; interior normals reuse the installed grid's neighbor heights.

Cumulus presentation has sixteen reusable bounds at maximum near/overview overlap, a shared cube mesh and bounded ray marching. Normal overview presents five large systems. Three distant scenery layers add 3,072 triangles. Report terrain triangle counters separately from water, vegetation and extra render passes.

The expanded audit uses isolated saves at 2560 x 1440, retains per-frame engine-loop CSVs and main-camera render confirmation, and samples process working/private memory plus WDDM process dedicated/shared GPU allocations. These counters do not establish display-present pacing or whole-board residency. Generation, readiness and timed traversal are separate measurements. See the [expanded implementation and evidence](ALPHA_v0.0.8_ADDENDUM.md).
