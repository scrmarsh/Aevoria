using System;
using System.IO;
using System.Threading.Tasks;
using Aevoria.Core;
using Aevoria.Persistence;
using Aevoria.Simulation;
using Aevoria.UI;
using Aevoria.World;
using UnityEngine;

namespace Aevoria.Presentation
{
    public sealed class AevoriaBootstrap : MonoBehaviour
    {
        private SimulationClock clock;
        private WorldRenderer terrain;
        private MapCamera controller;
        private Camera view;
        private DeveloperHud hud;
        private WorldEnvironment environment;
        private WorldWaterFeatures water;
        private EcologyPresentation ecosystems;
        public EcologyModel Ecology {get;private set;}
        public LivingWorldState Living {get;private set;}
        public CivilizationModel Society {get;private set;}
        public SettlementPresentation Settlements {get;private set;}
        public SocietyObservation SocietyView {get;private set;}
        public ExperimentalExplorer Explorer {get;private set;}
        public WorldNavigation Navigation {get;private set;}
        public ExcavationPresentation Excavations {get;private set;}
        public WorldNaturalDetails Details {get;private set;}
        public WorldWaterFeatures Waters=>water;
        private Task operation;private System.Threading.CancellationTokenSource preparation;
        public void CancelPreparation(){if(Busy)preparation?.Cancel();}
        private float autosaveAt;
        private bool quitApproved;
        private bool showDiagnostics;public bool DiagnosticsVisible {get=>showDiagnostics;set=>showDiagnostics=value;}
        private bool settingsApplied;private UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset qualityPipeline;private UnityEngine.Rendering.RenderPipelineAsset previousQualityPipeline;
        public DisplayController DisplayControl {get;private set;}
        public Action ToggleExploration,ToggleMap;
        public bool ExternalControl {get;set;}
        public MapCamera Controller => controller;
        public WorldAtmosphere Atmosphere { get; private set; }
        public WorldRenderer Terrain => terrain;
        public SimulationClock Clock => clock;
        public ApplicationMenu Menu { get; private set; }
        public WorldRepository Repository { get; private set; }
        public WorldRecord Current { get; private set; }
        public ApplicationSettings Settings { get; private set; }=new ApplicationSettings();
        public bool WorldActive=>terrain!=null;
        public bool Busy=>operation!=null && !operation.IsCompleted;
        public string Message { get; private set; }="";
        public string Progress { get; private set; }="";
        private GenerationPreview preparationPreview;
        private readonly System.Collections.Concurrent.ConcurrentQueue<GenerationPreview> preparedPreviews=new System.Collections.Concurrent.ConcurrentQueue<GenerationPreview>();private float nextPreviewFrame;
        private GenerationDiagnostics preparationDiagnostics;
        private float nextPreparationReport;
        public GenerationDiagnosticSnapshot PreparationDiagnostics=>preparationDiagnostics?.Snapshot();
        public GenerationPreview PreparationPreview=>System.Threading.Volatile.Read(ref preparationPreview);
        public bool LoadingExistingWorld{get;private set;}public int PreparationStage{get;private set;}
        private int preparationModuleCount=WorldGenerationIdentity.ModuleVersions.Count;
        public int PreparationStageCount=>preparationModuleCount+4;
        private void PreviewPrepared(GenerationPreview preview)
        {preparedPreviews.Enqueue(preview);var trace=PreparationDiagnostics;if(trace?.Outcome==GenerationOutcome.Running)PreparationStage=Math.Max(0,trace.RecentStages.Count-1);}
        private void GeographyProgress(string stage)
        {
            Debug.Log("AEVORIA_GENERATION_STAGE "+stage);
            Progress=stage;PreparationStage=Math.Max(0,(PreparationDiagnostics?.RecentStages.Count??1)-1);
        }
        public double LastOperationMilliseconds { get; private set; }
        private string settingsPath;
        public static bool HasFlag(string flag)=>Array.IndexOf(Environment.GetCommandLineArgs(),flag)>=0;
        public static string Argument(string name,string fallback=null)
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateIfMissing()
        {if(FindFirstObjectByType<AevoriaBootstrap>()==null) new GameObject("Aevoria").AddComponent<AevoriaBootstrap>();}
        private void Awake()
        {
            WorldOrigin.Reset();
            clock=new SimulationClock(new WorldState("phase1-001"));
            view=Camera.main;if(view==null) {view=new GameObject("Map camera").AddComponent<Camera>();view.tag="MainCamera";}
            controller=view.GetComponent<MapCamera>()??view.gameObject.AddComponent<MapCamera>();controller.enabled=false;
            view.nearClipPlane=0.08f;view.farClipPlane=180000;view.clearFlags=CameraClearFlags.Skybox;view.backgroundColor=new Color(0.55f,0.68f,0.78f);
            var sun=FindFirstObjectByType<Light>();if(sun==null) sun=new GameObject("Sun").AddComponent<Light>();
            sun.type=LightType.Directional;sun.intensity=1.3f;sun.color=new Color(1,0.96f,0.88f);sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(34,-32,0);RenderSettings.sun=sun;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(0.45f,0.59f,0.76f);RenderSettings.ambientEquatorColor=new Color(0.28f,0.33f,0.35f);RenderSettings.ambientGroundColor=new Color(0.15f,0.18f,0.14f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(0.57f,0.68f,0.76f);
            Atmosphere=gameObject.AddComponent<WorldAtmosphere>();Atmosphere.Initialize(view);
            DisplayControl=gameObject.AddComponent<DisplayController>();
            hud=gameObject.AddComponent<DeveloperHud>();hud.Clock=clock;hud.Visible=false;hud.Regenerate=Regenerate;
            hud.CameraOptions=(follow,collision,zoom,blur)=>{controller.TerrainFollowing=follow;controller.TerrainCollision=collision;controller.ZoomSensitivity=zoom;Atmosphere.MotionBlurEnabled=blur;};
            hud.Diagnostics=Diagnostics;
            string root=Argument("-aevoriaTestRoot",Application.persistentDataPath);
            Repository=new WorldRepository(Path.Combine(root,"Worlds"));settingsPath=Path.Combine(root,"settings.xml");
            bool benchmark=HasFlag("-aevoriaBenchmark003")||HasFlag("-aevoriaBenchmark11")||HasFlag("-aevoriaBenchmark");
            showDiagnostics=benchmark||HasFlag("-aevoriaDirect");
            if(!benchmark) LoadSettings();
            Menu=gameObject.AddComponent<ApplicationMenu>();Menu.Initialize(this);gameObject.AddComponent<CommandConsole>().Initialize(this);
            Application.wantsToQuit+=WantsToQuit;
            if(benchmark || HasFlag("-aevoriaDirect"))
            {
                Regenerate(Argument("-aevoriaSeed","aevoria-001"),WorldSize.Medium,new GenerationSettings());
                Menu.Show(ApplicationPage.World);
                if(HasFlag("-aevoriaBenchmark003")||HasFlag("-aevoriaBenchmark11")) gameObject.AddComponent<RefinementBenchmark>();
                else if(HasFlag("-aevoriaBenchmark")) gameObject.AddComponent<WorldBenchmark>();
            }
            if(HasFlag("-aevoriaAudit004")) gameObject.AddComponent<Alpha004Audit>();
            if(HasFlag("-aevoriaAudit005")) gameObject.AddComponent<Alpha005Audit>();
            if(HasFlag("-aevoriaAudit006")) gameObject.AddComponent<Alpha006Audit>();
            if(HasFlag("-aevoriaAudit007"))gameObject.AddComponent<Alpha007Audit>();
            if(HasFlag("-aevoriaAudit008"))gameObject.AddComponent<Alpha008Audit>();
            if(HasFlag("-aevoriaAudit009Stability"))gameObject.AddComponent<Alpha009StabilityAudit>();
            if(HasFlag("-aevoriaAudit009Terrain"))gameObject.AddComponent<Alpha009TerrainAudit>();
            if(HasFlag("-aevoriaAudit009Final"))gameObject.AddComponent<Alpha009FinalAudit>();
            if(HasFlag("-aevoriaAudit009Underground"))gameObject.AddComponent<Alpha009UndergroundAudit>();
            if(HasFlag("-aevoriaAudit009Environment"))gameObject.AddComponent<Alpha009EnvironmentAudit>();
            if(HasFlag("-aevoriaAudit010Foundation"))gameObject.AddComponent<Alpha010FoundationAudit>();
            if(HasFlag("-aevoriaAudit010Characters"))gameObject.AddComponent<Alpha010CharacterAudit>();
            if(HasFlag("-aevoriaAudit011Loading"))gameObject.AddComponent<Alpha011LoadingAudit>();
            if(HasFlag("-aevoriaAudit011Settlements"))gameObject.AddComponent<Alpha011SettlementAudit>();
            if(HasFlag("-aevoriaAudit011Inputs"))gameObject.AddComponent<Alpha011InputAudit>();
            if(HasFlag("-aevoriaAudit012Characters"))gameObject.AddComponent<Alpha012CharacterAudit>();
        }
        private string Diagnostics()
        {
            if(terrain==null)return "No world loaded";var geo=terrain.Geography;var c=geo.Configuration;var position=Living?.Traveler.Exploring==true?Explorer.Position:WorldOrigin.Geographic(view.transform);var point=new GeoPoint(position.x,position.z);bool inside=c.Contains(point);var sample=geo.Sample(point);var weather=RegionalAtmosphere.Forecast(geo,clock.State.Tick,point);int region=Ecology?.RegionAt(point)??-1;
            double heading=Living?.Traveler.Exploring==true?Living.Traveler.Yaw:view.transform.eulerAngles.y;
            string facing=new[]{"North","East","South","West"}[(int)Math.Floor((((heading%360)+360)%360+45)/90)%4];
            var timings=new UnityEngine.FrameTiming[1];UnityEngine.FrameTimingManager.CaptureFrameTimings();bool timing=UnityEngine.FrameTimingManager.GetLatestTimings(1,timings)>0;
            string gpu=timing&&timings[0].gpuFrameTime>0?$"{timings[0].gpuFrameTime:0.00} ms":"unavailable",cpu=timing&&timings[0].cpuFrameTime>0?$"{timings[0].cpuFrameTime:0.00} ms":"unavailable";
            return $"Seed {c.Seed}\nGenerator {ProceduralGeography.GeneratorVersion} · archive {WorldRecord.CurrentSchema} · ecology {EcologyModel.Version}\n{UnitFormatting.Distance(c.WidthMeters,Settings.Units)} × {UnitFormatting.Distance(c.DepthMeters,Settings.Units)}\nLandmass {geo.Geology.Arrangement}\nX {UnitFormatting.Length(position.x,Settings.Units)} E · Z {UnitFormatting.Length(position.z,Settings.Units)} N\nAltitude {UnitFormatting.Length(position.y-geo.SeaLevel,Settings.Units)}\nBiome {(inside?sample.Biome.ToString():"Scenery outside simulation")}\nFacing {facing}\nTemperature {(inside?UnitFormatting.Temperature(weather.Temperature,Settings.Units):"unavailable")}\nMoisture {sample.Moisture:P0}\nRain intensity {weather.Precipitation:P0} · cloud {weather.Cloud:P0}\nGround wetness {(region>=0?Living.Atmosphere.Wetness(region).ToString("P0"):"unavailable")}\nWind {UnitFormatting.Speed(weather.WindX,Settings.Units)} / {UnitFormatting.Speed(weather.WindZ,Settings.Units)}\nPressure {weather.Pressure:0} hPa proxy\nCPU frame {cpu} · GPU {gpu}\nChunks {terrain.ActiveChunks} · jobs {terrain.PendingChunks}\n{terrain.LodSummary}\nRegional triangles {terrain.Regional?.Triangles:N0}\nResident triangles {terrain.ResidentTriangles:N0}\nVisible tree candidates {terrain.Vegetation.VisibleTrees:N0}\nStreaming CPU {terrain.LastUpdateMilliseconds:0.00} ms\nManaged {GC.GetTotalMemory(false)/1048576.0:0.0} MiB\nUnity {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0:0.0} MiB\nEcology {Ecology?.Count} regions · month {Ecology?.Steps}\nWeather and seasonal tick {Living?.Atmosphere.Tick}/{Living?.Seasons.Tick}\nGeology {geo.CalibrationMilliseconds:0} ms · climate {geo.ClimateMilliseconds:0} ms\nDrainage {geo.HydrologyMilliseconds:0} ms · lakes {geo.InlandSurfaces.LakeCount}";
        }        private void Install(ProceduralGeography geography,EcologyModel ecology=null,LivingWorldState living=null,CivilizationModel society=null)
        {
            Ecology=ecology??new EcologyModel(geography,clock.State.Tick);
            Living=living??new LivingWorldState(geography,Ecology,clock.State.Tick);
            Society=society??new CivilizationModel(geography,clock.State.Tick,archive:Repository.History);UpdateSocietyTotals();
            Atmosphere.WorldExtent=(float)geography.Configuration.WidthMeters;
            if(terrain==null) terrain=new GameObject("Procedural geography").AddComponent<WorldRenderer>();
            controller.enabled=true;controller.ExternalDriver=ExternalControl;controller.PresentationHeight=null;controller.Initialize(geography);terrain.Regenerate(geography,view);
            if(water==null){water=new GameObject("Inland waters").AddComponent<WorldWaterFeatures>();water.transform.position=-WorldOrigin.Offset;}water.Initialize(geography);
            if(environment==null) environment=gameObject.AddComponent<WorldEnvironment>();environment.Initialize(geography,clock,view,Atmosphere,Living.Seasons,Ecology,Living.Atmosphere);
            if(ecosystems==null) ecosystems=gameObject.AddComponent<EcologyPresentation>();ecosystems.Initialize(this,geography,view);
            ApplyWorldQuality();
            terrain.Vegetation.Living=Living;terrain.Vegetation.WorldTick=()=>clock.State.Tick;
            if(Excavations==null)Excavations=gameObject.AddComponent<ExcavationPresentation>();Excavations.Initialize(this);
            if(Details==null)Details=gameObject.AddComponent<WorldNaturalDetails>();Details.Initialize(this);
            if(Explorer==null)Explorer=gameObject.AddComponent<ExperimentalExplorer>();Explorer.Initialize(this);Explorer.AcceptInput=!ExternalControl;
            if(Navigation==null)Navigation=gameObject.AddComponent<WorldNavigation>();Navigation.Initialize(this);
            if(Settlements!=null)Destroy(Settlements);Settlements=gameObject.AddComponent<SettlementPresentation>();Settlements.Initialize(this);
            if(SocietyView!=null)Destroy(SocietyView);SocietyView=gameObject.AddComponent<SocietyObservation>();SocietyView.Initialize(this);
            hud.Clock=clock;hud.Visible=showDiagnostics;autosaveAt=Time.unscaledTime+Settings.AutosaveSeconds;
        }
        public void Regenerate(string seed,WorldSize size,GenerationSettings settings)
        {Install(new ProceduralGeography(GenerationSettings.Preset(seed,size,settings)));terrain.ViewRadius=terrain.Geography.Configuration.Terrain.RenderRadius;Current=null;Menu?.Show(ApplicationPage.World);}
        public void CreateWorld(string name,string seed,WorldSize size,GenerationSettings settings,ClimateSettings climate)
        {
            if(Busy) return;WorldRecord.ValidateName(name);var config=GenerationSettings.Preset(WorldSeed.Resolve(seed),size,settings);
            operation=CreateAsync(name,config,climate);
        }
        private async Task CreateAsync(string name,WorldConfiguration config,ClimateSettings climate)
        {
            preparationModuleCount=WorldGenerationIdentity.For(config.Terrain).Count;
            bool previousBackground=Application.runInBackground;Application.runInBackground=true;
            preparationDiagnostics=new GenerationDiagnostics();
            preparation?.Dispose();preparation=new System.Threading.CancellationTokenSource();
            LoadingExistingWorld=false;PreparationStage=0;while(preparedPreviews.TryDequeue(out _)){}System.Threading.Interlocked.Exchange(ref preparationPreview,null);Menu.Show(ApplicationPage.Loading);Progress="Building geography, drainage and climate";Message="";var watch=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var geography=await Task.Run(()=>new ProceduralGeography(config,climate,GeographyProgress,preparation.Token,PreviewPrepared,preparationDiagnostics));
                var nextClock=new SimulationClock(new WorldState(config.Seed));nextClock.Start();
                PreparationStage=preparationModuleCount;Progress="Preparing bounded ecological regions";var nextEcology=await Task.Run(()=>new EcologyModel(geography));var nextLiving=new LivingWorldState(geography,nextEcology,0);
                PreparationStage=preparationModuleCount+1;Progress="Founding civilizations on suitable land";var nextSociety=await Task.Run(()=>new CivilizationModel(geography,archive:Repository.History,token:preparation.Token));
                PreviewPrepared(GenerationPreview.ForSettlements(geography,nextSociety));
                var record=WorldRecord.Capture(null,name,null,geography,nextClock,ApplicationVersion.Number,nextEcology,nextLiving,nextSociety);
                preparation.Token.ThrowIfCancellationRequested();PreparationStage=preparationModuleCount+2;Progress="Writing the first world archive";await Repository.SaveAsync(record,false);preparation.Token.ThrowIfCancellationRequested();
                clock=nextClock;Current=record;Install(geography,nextEcology,nextLiving,nextSociety);await WaitForPresentation();Menu.Show(ApplicationPage.World);Menu.RefreshSaves();
            }
            catch(OperationCanceledException){Message="Generation cancelled; existing archives were preserved.";ReturnToMenu();}
            catch(Exception ex) {Message=ex.Message;Menu.Show(ApplicationPage.Create);}
            finally {Application.runInBackground=previousBackground;LastOperationMilliseconds=watch.Elapsed.TotalMilliseconds;Progress="";}
        }
        public void LoadWorld(string id)
        {if(!Busy) operation=LoadAsync(id);}
        private async Task LoadAsync(string id)
        {
            bool previousBackground=Application.runInBackground;Application.runInBackground=true;
            preparationDiagnostics=new GenerationDiagnostics();
            preparation?.Dispose();preparation=new System.Threading.CancellationTokenSource();
            LoadingExistingWorld=true;PreparationStage=0;while(preparedPreviews.TryDequeue(out _)){}System.Threading.Interlocked.Exchange(ref preparationPreview,null);Menu.Show(ApplicationPage.Loading);Progress="Reading and validating the world archive";Message="";var watch=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var loaded=await Repository.LoadAsync(id);preparationModuleCount=WorldGenerationIdentity.For(loaded.Record.Configuration().Terrain).Count;Progress="Reconstructing verified geography and environment";
                var geography=await Task.Run(()=>loaded.Record.Rebuild(progress:GeographyProgress,token:preparation.Token,preview:PreviewPrepared,diagnostics:preparationDiagnostics));
                var nextClock=loaded.Record.RestoreClock();PreparationStage=preparationModuleCount;Progress="Restoring ecological regions and seasonal memory";var nextEcology=await Task.Run(()=>new EcologyModel(geography,loaded.Record.Ecology));var nextLiving=new LivingWorldState(geography,nextEcology,nextClock.State.Tick,loaded.Record.Living);
                PreparationStage=preparationModuleCount+1;Progress="Restoring civilizations, territories and history";var nextSociety=await Task.Run(()=>new CivilizationModel(geography,loaded.Record.Society,Repository.History));PreviewPrepared(GenerationPreview.ForSettlements(geography,nextSociety));preparation.Token.ThrowIfCancellationRequested();clock=nextClock;Current=loaded.Record;Install(geography,nextEcology,nextLiving,nextSociety);await WaitForPresentation();
                if(loaded.RecoveredBackup) Message="Recovered the previous complete backup. Save As creates a new safe archive; the damaged original is preserved.";
                else {Current.LastPlayedUtc=DateTime.UtcNow.ToString("O");await Repository.SaveAsync(Current);}
                Menu.Show(ApplicationPage.World);Menu.RefreshSaves();
            }
            catch(OperationCanceledException){ReturnToMenu();Message="Loading cancelled; archive preserved.";}
            catch(Exception ex) {ReturnToMenu();Message=ex.Message;Menu.Show(ApplicationPage.Load);}
            finally {Application.runInBackground=previousBackground;LastOperationMilliseconds=watch.Elapsed.TotalMilliseconds;Progress="";}
        }
        public void SaveWorld(bool duplicate=false,bool returnToMenu=false,bool quit=false,string newName=null)
        {if(!Busy && WorldActive) operation=SaveAsync(duplicate,returnToMenu,quit,newName);}
        private async Task WaitForPresentation()
        {
            PreparationStage=preparationModuleCount+3;Progress="Preparing the initial terrain and vegetation";var timer=System.Diagnostics.Stopwatch.StartNew();
            while(terrain!=null && (!terrain.IsReady||water!=null&&!water.IsReady||Excavations!=null&&!Excavations.Ready||Explorer!=null&&!Explorer.Ready))
            {preparation?.Token.ThrowIfCancellationRequested();if(timer.Elapsed.TotalSeconds>120) throw new TimeoutException("Initial terrain did not become ready. The world archive is preserved.");await Task.Delay(16);}
        }
        private async Task SaveAsync(bool duplicate,bool returnToMenu,bool quit,string newName)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();Message="Saving world…";autosaveAt=Time.unscaledTime+Math.Max(30,Settings.AutosaveSeconds);
            try
            {
                long saveTick=clock.State.Tick;
                while(!ModelsReadyAt(saveTick))
                {
                    AdvanceWorldModels(saveTick,2);Message="Synchronizing world history and environment before saving";
                    await Task.Yield();
                    if(!WorldActive)throw new OperationCanceledException("The world closed before save synchronization completed.");
                }
                UpdateSocietyTotals();
                var record=WorldRecord.Capture(duplicate?null:Current?.Id,newName??Current?.Name??"Untitled world",duplicate?null:Current?.CreatedUtc,terrain.Geography,clock,ApplicationVersion.Number,Ecology,Living,Society);
                if(Current!=null) record.Extensions=Current.Extensions;
                await Repository.SaveAsync(record,!duplicate);Current=record;Message="World saved · "+record.Name;Menu.RefreshSaves();
                autosaveAt=Time.unscaledTime+Settings.AutosaveSeconds;
                if(returnToMenu) ReturnToMenu();if(quit) {quitApproved=true;Application.Quit();}
            }
            catch(Exception ex) {Message="Save failed: "+ex.Message+" Your current world is still open.";}
            finally {LastOperationMilliseconds=watch.Elapsed.TotalMilliseconds;}
        }
        public void ReturnToMenu()
        {
            if(terrain!=null) {terrain.gameObject.SetActive(false);Destroy(terrain.gameObject);terrain=null;}
            if(water!=null) {water.gameObject.SetActive(false);Destroy(water.gameObject);water=null;}
            if(environment!=null) {environment.enabled=false;Destroy(environment);environment=null;}
            if(ecosystems!=null) {ecosystems.enabled=false;Destroy(ecosystems);ecosystems=null;}Ecology=null;Living=null;Society=null;
            foreach(var service in new MonoBehaviour[]{Explorer,Navigation,Excavations,Details,Settlements,SocietyView})if(service!=null){service.enabled=false;Destroy(service);}Explorer=null;Navigation=null;Excavations=null;Details=null;Settlements=null;SocietyView=null;ToggleMap=null;ToggleExploration=null;MapCamera.OverlayBlocked=false;
            WorldOrigin.Reset();controller.enabled=false;hud.Visible=false;Current=null;Menu.Show(ApplicationPage.Home);Menu.RefreshSaves();
        }
        public async Task RenameAsync(string id,string name)
        {
            WorldRecord.ValidateName(name);var loaded=await Repository.LoadAsync(id);if(loaded.RecoveredBackup) throw new IOException("Recover with Save As before renaming a damaged archive.");
            loaded.Record.Name=name.Trim();await Repository.SaveAsync(loaded.Record);if(Current?.Id==id) Current.Name=name.Trim();Menu.RefreshSaves();
        }
        private void LoadSettings()
        {
            try {if(File.Exists(settingsPath)) {var saved=ArchiveCodec.Decode<ApplicationSettings>(File.ReadAllBytes(settingsPath));saved.Validate();Settings=saved;}ApplySettings(Settings,false);}
            catch(Exception ex) {Message="Settings could not be loaded; defaults are active. "+ex.Message;ApplySettings(new ApplicationSettings(),false);}
        }
        public void ApplySettings(ApplicationSettings settings,bool persist=true)
        {
            settings.Validate();GameInput.Validate(settings);bool displayChanged=!settingsApplied||settings.DisplayWidth!=Settings.DisplayWidth||settings.DisplayHeight!=Settings.DisplayHeight||settings.DisplayMode!=Settings.DisplayMode;
            if(persist) ArchiveCodec.AtomicWrite(settingsPath,ArchiveCodec.Encode(settings));Settings=settings.Copy();GameInput.Settings=Settings;settingsApplied=true;
            QualitySettings.SetQualityLevel(Mathf.Min(Settings.Quality,QualitySettings.names.Length-1),true);
            QualitySettings.vSyncCount=Settings.VSync;Application.targetFrameRate=Settings.FrameCap;QualitySettings.shadows=Settings.Shadows?ShadowQuality.All:ShadowQuality.Disable;
            if(RenderSettings.sun!=null) RenderSettings.sun.shadows=Settings.Shadows?LightShadows.Soft:LightShadows.None;
            if(displayChanged&&Settings.DisplayWidth>0) DisplayControl.Request(Settings.DisplayWidth,Settings.DisplayHeight,Settings.DisplayMode==0?FullScreenMode.Windowed:Settings.DisplayMode==1?FullScreenMode.FullScreenWindow:FullScreenMode.ExclusiveFullScreen);
            view.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().antialiasing=Settings.Antialiasing?UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing:UnityEngine.Rendering.Universal.AntialiasingMode.None;
            controller.TerrainFollowing=Settings.TerrainFollowing;controller.TerrainCollision=Settings.TerrainCollision;controller.ZoomSensitivity=Settings.ZoomSensitivity;Atmosphere.MotionBlurEnabled=Settings.MotionBlur;
            controller.KeyboardMovement=Settings.KeyboardMovement;controller.MiddlePanning=Settings.MiddlePanning;controller.EdgeScrolling=Settings.EdgeScrolling;controller.EdgeZone=Settings.EdgeZone;controller.EdgeSpeed=Settings.EdgeSpeed;controller.MovementSensitivity=Settings.MovementSensitivity;controller.MinimumMovementSpeed=Settings.MinimumPanSpeed;
            ApplyWorldQuality();
            autosaveAt=Time.unscaledTime+Settings.AutosaveSeconds;
        }
        private void ApplyWorldQuality()
        {
            if(terrain!=null){terrain.StreamingAggressiveness=Settings.StreamingAggressiveness;terrain.BackgroundBudget=Settings.BackgroundStreamingBudget;terrain.SetCacheMemoryBudget(Settings.PresentationCacheMiB);}
            if(terrain!=null){terrain.ViewRadius=Settings.TerrainDistance;terrain.DetailQuality=Settings.TerrainDetail;terrain.MountainQuality=Settings.MountainDetail;if(terrain.Regional!=null)terrain.Regional.Quality=Settings.DistantDetail;if(terrain.Vegetation!=null){terrain.Vegetation.ViewDistance=Settings.VegetationDistance;terrain.Vegetation.Density=Settings.VegetationDensity;}}
            Atmosphere.FogEnabled=Settings.FogEnabled;Atmosphere.FogDensity=Settings.FogDensity;Atmosphere.FogDistance=Settings.FogDistance;Atmosphere.HazeStrength=Settings.HazeStrength;
            Shader.SetGlobalFloat("_AevoriaWaterQuality",Settings.WaterQuality);Shader.SetGlobalFloat("_AevoriaWaterTransparency",Settings.WaterTransparency);Shader.SetGlobalFloat("_AevoriaLodTransition",Settings.LodTransition);Shader.SetGlobalFloat("_AevoriaCloudQuality",Settings.CloudQuality);Shader.SetGlobalFloat("_AevoriaDistantScenery",Settings.DistantScenery);AudioListener.volume=Settings.MasterVolume;
            Shader.SetGlobalFloat("_AevoriaTerrainTextureQuality",Settings.TerrainTextureQuality);
            environment?.ApplyPresentation(Settings.RenderClouds,Settings.RenderWeatherEffects,Settings.CloudDensity);
            if(qualityPipeline==null){previousQualityPipeline=QualitySettings.renderPipeline;var active=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;if(active!=null)qualityPipeline=Instantiate(active);}
            if(qualityPipeline!=null){qualityPipeline.shadowDistance=Settings.ShadowDistance;QualitySettings.renderPipeline=qualityPipeline;}
        }        public void Quit() {if(WorldActive) SaveWorld(quit:true);else {quitApproved=true;Application.Quit();}}
        private bool WantsToQuit()
        {
            // Isolated native audits own their failure exit code. Intercepting
            // Quit(4) with a successful save-and-quit used to turn a failed audit
            // into OS exit0; its failure ledger still rejected the run.
            bool isolatedAudit=Argument("-aevoriaTestRoot")!=null&&(HasFlag("-aevoriaAudit009Stability")||HasFlag("-aevoriaAudit009Terrain")||HasFlag("-aevoriaAudit009Final")||HasFlag("-aevoriaAudit009Underground")||HasFlag("-aevoriaAudit009Environment"));
            if(Application.isBatchMode||quitApproved||!WorldActive||isolatedAudit||HasFlag("-aevoriaBenchmark003")||HasFlag("-aevoriaBenchmark11")||HasFlag("-aevoriaBenchmark")||HasFlag("-aevoriaAudit004")||HasFlag("-aevoriaAudit005")||HasFlag("-aevoriaAudit006")||HasFlag("-aevoriaAudit007")||HasFlag("-aevoriaAudit008"))return true;
            SaveWorld(quit:true);return false;
        }
        private void OnDestroy() {preparation?.Cancel();preparation?.Dispose();Application.wantsToQuit-=WantsToQuit;if(qualityPipeline!=null){if(QualitySettings.renderPipeline==qualityPipeline)QualitySettings.renderPipeline=previousQualityPipeline;Destroy(qualityPipeline);}}
        private void Update()
        {
            if(Busy&&Time.unscaledTime>=nextPreviewFrame&&preparedPreviews.TryDequeue(out var nextPreview)){System.Threading.Interlocked.Exchange(ref preparationPreview,nextPreview);nextPreviewFrame=Time.unscaledTime+.25f;}
            if(Busy&&preparationDiagnostics!=null&&Time.unscaledTime>=nextPreparationReport)
            {
                nextPreparationReport=Time.unscaledTime+5;var trace=preparationDiagnostics.Snapshot();
                Debug.Log($"AEVORIA_GENERATION_HEARTBEAT elapsed={trace.ElapsedSeconds:F1}s module={trace.Module} outcome={trace.Outcome} silent={trace.SecondsSinceHeartbeat:F1}s work={trace.Completed}/{trace.Total} managedMiB={GC.GetTotalMemory(false)/1048576.0:F1} detail={trace.Detail}");
            }
            if(GameInput.Pressed(Aevoria.Core.GameAction.Fullscreen)&&Menu?.DisplayPreviewActive!=true) DisplayControl.Toggle(this);
            if(!WorldActive) return;
            bool playing=Menu==null || Menu.Page==ApplicationPage.World;
            if(GameInput.Pressed(Aevoria.Core.GameAction.Diagnostics)) showDiagnostics=!showDiagnostics;
            controller.enabled=playing&&Living?.Traveler.Exploring!=true;hud.Visible=playing&&showDiagnostics;
            if(playing&&!Busy&&!ExternalControl&&Application.isFocused&&!Aevoria.UI.DeveloperHud.IsEditingText&&GameInput.Pressed(Aevoria.Core.GameAction.Explore))ToggleExploration?.Invoke();
            if(playing&&!Busy) {clock.Update(Time.unscaledDeltaTime,16);try{AdvanceWorldModels(clock.State.Tick,4);}catch(Exception ex){clock.Pause();Message="World simulation paused: "+ex.Message;}}
            if(Current!=null && Settings.AutosaveSeconds>0 && Time.unscaledTime>=autosaveAt && !Busy) SaveWorld();
        }
        private void UpdateSocietyTotals(){if(Society!=null)clock.State.UseAggregatePopulation((int)Math.Min(int.MaxValue,Society.TotalPopulation),(int)Math.Min(int.MaxValue,Society.TotalFood));}
        private bool ModelsReadyAt(long tick)=>Living.Atmosphere.Tick>tick-RegionalAtmosphere.StepTicks&&Living.Seasons.Tick>tick-SeasonalModel.StepTicks&&Ecology.Tick>tick-EcologyModel.TicksPerMonth&&Society.Tick>tick-CivilizationModel.TicksPerMonth;
        private void AdvanceWorldModels(long tick,int dailyBudget)
        {
            // Interleave matching daily steps so seasonal accumulation can reuse
            // the atmosphere's exact forecast cache instead of recomputing it.
            for(int i=0;i<dailyBudget;i++)
            {
                long next=Math.Min(Living.Atmosphere.Tick,Living.Seasons.Tick)+SeasonalModel.StepTicks;if(next>tick)break;
                if(Living.Atmosphere.Tick<next)Living.Atmosphere.AdvanceTo(next,1);
                if(Living.Seasons.Tick<next)Living.Seasons.AdvanceTo(next,1);
            }
            Ecology.AdvanceTo(tick,Math.Max(1,dailyBudget));Society.AdvanceTo(tick,Math.Max(1,dailyBudget));UpdateSocietyTotals();
        }
    }
}

