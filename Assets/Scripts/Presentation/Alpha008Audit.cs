using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Aevoria.Core;
using Aevoria.Persistence;
using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    public sealed class Alpha008Audit : MonoBehaviour
    {
        [Serializable]public sealed class Measurement{public string scenario;public int frames,rendered,width,height;public double meanMs,p95Ms,p99Ms,maxMs,cpuMs,gpuMs,managedMiB,unityMiB;}
        [Serializable]public sealed class Generation{public string size,seed;public double seconds;public List<string> stages=new List<string>();}
        [Serializable]public sealed class ViewEvidence{public string scene,biome,seed;public Vector3 cameraPosition,cameraEuler;public long tick;public ApplicationSettings settings;public float x,z,ground,cameraHeight,distance,regionalSpacing;public int trees,details,submergedRocks,chunks;public double readySeconds,solidColliderMs,solidInstallMs,solidPreparationMs,solidBakeWaitMs;public int solidVertices,solidTriangles;public long triangles;}
        private readonly List<ViewEvidence> views=new List<ViewEvidence>();private double lastReady;
        [Serializable]private sealed class AddendumResult{public bool preload,cancellation,zoom,toggles,flight,invalidSaveProtected,highAltitudeSave;public float minClearance=999999;public int zoomSamples,cacheHits,beforeRenderChecks;public double highSavedAltitude;public string rejectedSaveHashBefore,rejectedSaveHashAfter;public List<float> preloadSeconds=new List<float>();}
        private readonly AddendumResult addendum=new AddendumResult();
        private bool probeLegacyNear;private string projectionProbePhase="full";private int frustumWarnings;
        [Serializable]private sealed class FrustumState{public string phase;public Vector3 position;public float near,far,distance,clearance,viewProjectionDeterminant;}
        [Serializable]private sealed class HorizonResult{public bool overviewFade,firstPerson,thirdPerson,cleanup;}
        [Serializable]public sealed class Results{public string version=ApplicationVersion.Number,cpu,gpu;public bool continuity,cleanup,cancellation,exploration,excavation;public List<Generation> generations=new List<Generation>();public List<Measurement> measurements=new List<Measurement>();public List<string> limitations=new List<string>();public float wetBefore,wetRain,wetDryStart,wetDry;}
        private AevoriaBootstrap app;private Results result;private string output;private int rendered;private bool smoke;private Generation generation;
        private void Rendered(ScriptableRenderContext context,Camera camera){if(camera.CompareTag("MainCamera"))rendered++;}
        private IEnumerator Start()
        {
            if(AevoriaBootstrap.Argument("-aevoriaTestRoot")==null){Debug.LogError("Audit008 requires isolated save root");Application.Quit(4);yield break;}
            app=GetComponent<AevoriaBootstrap>();output=AevoriaBootstrap.Argument("-aevoriaOutput");Directory.CreateDirectory(output);smoke=AevoriaBootstrap.HasFlag("-aevoria008Smoke");result=new Results{cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName};Application.runInBackground=true;Application.SetStackTraceLogType(LogType.Error,StackTraceLogType.Full);Application.SetStackTraceLogType(LogType.Warning,StackTraceLogType.Full);Application.logMessageReceived+=Failure;RenderPipelineManager.endCameraRendering+=Rendered;
            yield return new WaitForSecondsRealtime(1);yield return Capture("01-menu");app.Menu.Show(ApplicationPage.Create);yield return new WaitForSecondsRealtime(2);yield return Capture("02-creation");
            if(AevoriaBootstrap.HasFlag("-aevoria008HorizonProbe"))
            {
                var checks=new HorizonResult();app.CreateWorld("Ground horizon validation","shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();Require(app.WorldActive,"horizon world");app.Clock.Pause();app.Controller.ExternalDriver=true;app.ApplySettings(new ApplicationSettings{AutosaveSeconds=0,CloudQuality=0,RenderClouds=false,FogEnabled=false});app.Explorer.AcceptInput=false;
                bool entered=false;for(int i=0;i<app.Ecology.Count&&!entered;i++)if(app.Ecology.EnvironmentAt(i).Land)entered=app.Explorer.EnterAt(app.Ecology.EnvironmentAt(i).Center);Require(entered,"horizon traveler spawn");float end=Time.realtimeSinceStartup+60;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Require(app.Explorer.Ready,"horizon collision");var position=app.Explorer.Position;app.Explorer.Exit();
                app.Controller.SetView(new GeoPoint(position.x,position.z),25,72,160000);yield return Ready();yield return new WaitForSecondsRealtime(.3f);var horizon=FindFirstObjectByType<DistantMountainHorizon>();Require(horizon!=null,"scenery object");foreach(var layer in horizon.GetComponentsInChildren<MeshRenderer>())Require(!layer.enabled,"overview hides decorative scenery");checks.overviewFade=true;yield return Capture("overview-scenery-hidden");
                Require(app.Explorer.EnterAt(new GeoPoint(position.x,position.z)),"return to traveler from overview");end=Time.realtimeSinceStartup+60;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Require(app.Explorer.Ready,"ground horizon collision");app.Explorer.SetFirstPerson(true);yield return new WaitForSecondsRealtime(1.5f);foreach(var layer in horizon.GetComponentsInChildren<MeshRenderer>())Require(layer.enabled&&layer.sharedMaterial.GetFloat("_Opacity")>.9f,"ground scenery ignores retained God orbit");checks.firstPerson=true;
                foreach(int yaw in new[]{0,90,180,270}){app.Living.Traveler.Yaw=yaw;app.Living.Traveler.Pitch=0;yield return new WaitForSecondsRealtime(.4f);yield return Capture("ground-after-overview-"+yaw);}
                app.Explorer.SetFirstPerson(false);yield return new WaitForSecondsRealtime(.5f);yield return Capture("ground-after-overview-third-person");checks.thirdPerson=true;app.Explorer.Exit();app.ReturnToMenu();yield return null;yield return null;checks.cleanup=FindObjectsByType<DistantMountainHorizon>(FindObjectsSortMode.None).Length==0;Require(checks.cleanup,"horizon cleanup");File.WriteAllText(Path.Combine(output,"horizon.json"),JsonUtility.ToJson(checks,true));Debug.Log("AEVORIA008_HORIZON_PASS");Application.Quit();yield break;
            }
            if(AevoriaBootstrap.HasFlag("-aevoria008FrustumProbe"))
            {
                app.CreateWorld("Projection diagnostic","aevoria-001",WorldSize.Colossal,new GenerationSettings(mountains:12686.6143f,landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();Require(app.WorldActive,"projection probe world");app.Clock.Pause();app.Controller.ExternalDriver=true;var settings=new ApplicationSettings{AutosaveSeconds=0,CloudQuality=0,FogEnabled=false};app.ApplySettings(settings);int forest=Best(i=>app.Ecology.Forest(i));Application.onBeforeRender+=ProbeClipOverride;
                foreach(bool legacy in new[]{true,false})
                {
                    probeLegacyNear=legacy;projectionProbePhase=legacy?"legacy-0.05m":"precision-near";Focus(forest,750);yield return Ready();int startWarnings=frustumWarnings;
                    for(int i=0;i<1800;i++){float t=i/60f;app.Controller.MoveHorizontal(new Vector2(Mathf.Sin(t*.2f),Mathf.Cos(t*.2f))*1000/60);app.Controller.Advance(1/60f);yield return null;}
                    yield return Capture("probe-"+projectionProbePhase);File.AppendAllText(Path.Combine(output,"projection-counts.txt"),projectionProbePhase+": "+(frustumWarnings-startWarnings)+" warnings\n");
                }
                probeLegacyNear=false;Application.onBeforeRender-=ProbeClipOverride;app.ReturnToMenu();yield return null;Debug.Log("AEVORIA008_FRUSTUM_PROBE_COMPLETE");Application.Quit();yield break;
            }
            if(AevoriaBootstrap.HasFlag("-aevoria008AddendumProbe"))
            {
                app.CreateWorld("Addendum diagnostic","shaping-continents",WorldSize.Medium,new GenerationSettings(landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();Require(app.WorldActive,"diagnostic world");app.Clock.Pause();app.Controller.ExternalDriver=true;
                var settings=new ApplicationSettings{CloudQuality=0,RenderClouds=false,FogEnabled=false};app.ApplySettings(settings);
                int plain=Find(i=>app.Ecology.EnvironmentAt(i).Land&&app.Ecology.EnvironmentAt(i).Biome==Biome.Grassland);var hydro=app.Terrain.Geography.Hydrology;int peak=0;for(int i=1;i<hydro.Ground.Length;i++)if(hydro.Ground[i]>hydro.Ground[peak])peak=i;
                foreach(string terrain in new[]{"grassland","mountains"})
                {
                    if(terrain=="grassland")Focus(plain,850);else app.Controller.SetView(new GeoPoint(hydro.X(peak),hydro.Y(peak)),25,44,7000);
                    yield return Ready();for(int mode=0;mode<=5;mode++){Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",mode);yield return Capture("diagnostic-"+terrain+"-"+mode);}Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",0);
                }
                app.ReturnToMenu();yield return null;Debug.Log("AEVORIA008_ADDENDUM_PROBE_COMPLETE");Application.Quit();yield break;
            }
            if(AevoriaBootstrap.HasFlag("-aevoria008ArtifactProbe"))
            {
                app.CreateWorld("Surface artifact probe","shaping-continents",WorldSize.Medium,new GenerationSettings(landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();Require(app.WorldActive,"artifact probe world");app.Clock.Pause();app.Controller.ExternalDriver=true;
                var settings=new ApplicationSettings{AutosaveSeconds=0};settings.CloudQuality=0;app.ApplySettings(settings);app.Controller.SetView(new GeoPoint(-62000,230000),25,44,1500);yield return Ready();yield return Capture("probe-1-default");
                var water=app.Waters.GetComponentsInChildren<MeshRenderer>();foreach(var renderer in water)renderer.forceRenderingOff=true;yield return Capture("probe-2-no-inland-water");foreach(var renderer in water)renderer.forceRenderingOff=false;
                app.Terrain.Vegetation.Visible=false;app.Details.enabled=false;yield return Capture("probe-3-no-vegetation");app.Terrain.Vegetation.Visible=true;app.Details.enabled=true;
                settings.TerrainDetail=3;settings.MountainDetail=3;app.ApplySettings(settings);yield return Ready();yield return Capture("probe-4-high-detail");
                var locals=new List<Mesh>();var originalNormals=new List<Vector3[]>();foreach(var renderer in app.Terrain.GetComponentsInChildren<MeshRenderer>()){var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);if(renderer.sharedMaterial==null||renderer.sharedMaterial.shader.name!="Aevoria/Terrain"||block.GetFloat("_Backdrop")>.5f)continue;var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;if(mesh==null)continue;locals.Add(mesh);originalNormals.Add(mesh.normals);var flat=new Vector3[mesh.vertexCount];for(int i=0;i<flat.Length;i++)flat[i]=Vector3.up;mesh.normals=flat;}yield return Capture("probe-6-flat-local-normals");for(int i=0;i<locals.Count;i++)locals[i].normals=originalNormals[i];
                foreach(var mesh in locals){var bank=new List<Vector4>(mesh.vertexCount);for(int i=0;i<mesh.vertexCount;i++)bank.Add(new Vector4(100,100,0,0));mesh.SetUVs(0,bank);}yield return Capture("probe-7-no-local-bank-shading");
                var hidden=new List<MeshRenderer>();var properties=new MaterialPropertyBlock();foreach(var renderer in app.Terrain.GetComponentsInChildren<MeshRenderer>()){renderer.GetPropertyBlock(properties);if(renderer.sharedMaterial!=null&&renderer.sharedMaterial.shader.name=="Aevoria/Terrain"&&properties.GetFloat("_Backdrop")<.5f){renderer.forceRenderingOff=true;hidden.Add(renderer);}}yield return Capture("probe-5-regional-only");foreach(var renderer in hidden)renderer.forceRenderingOff=false;
                app.ReturnToMenu();yield return null;Debug.Log("AEVORIA008_ARTIFACT_PROBE_COMPLETE");Application.Quit();yield break;
            }
            if(AevoriaBootstrap.HasFlag("-aevoria008Review"))
            {
                app.CreateWorld("Restoration visual review","shaping-continents",WorldSize.Medium,new GenerationSettings(landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();Require(app.WorldActive,"review world");app.Clock.Pause();app.Controller.ExternalDriver=true;var s=app.Settings.Copy();s.CloudQuality=0;app.ApplySettings(s);
                int forest=Best(i=>app.Ecology.Forest(i)),plain=Find(i=>app.Ecology.EnvironmentAt(i).Land&&app.Ecology.EnvironmentAt(i).Biome==Biome.Grassland);yield return RestorationMatrix(forest,plain);yield return AdditionalViews();app.ReturnToMenu();yield return null;File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(result,true));Debug.Log("AEVORIA008_REVIEW_COMPLETE");Application.Quit();yield break;
            }
            if(AevoriaBootstrap.HasFlag("-aevoria008WeatherOnly"))
            {
                app.CreateWorld("Regional weather validation","shaping-continents",WorldSize.Medium,new GenerationSettings(landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();Require(app.WorldActive,"weather world");app.Clock.Pause();app.Controller.ExternalDriver=true;yield return WeatherSequence(Best(i=>app.Ecology.Forest(i)));Require(result.wetRain>result.wetBefore&&result.wetDry<result.wetDryStart&&result.wetDry>0,"observed gradual drying");
                string before=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));app.SaveWorld();yield return Operation();string id=app.Current.Id;app.ReturnToMenu();yield return new WaitForSecondsRealtime(.3f);app.LoadWorld(id);yield return Operation();app.Clock.Pause();result.continuity=before==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));Require(result.continuity,"weather continuation");app.ReturnToMenu();yield return null;File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(result,true));Debug.Log("AEVORIA008_WEATHER_PASS");Application.Quit();yield break;
            }
            var sizes=AevoriaBootstrap.HasFlag("-aevoria008ColossalOnly")?new[]{WorldSize.Colossal}:smoke?new[]{WorldSize.Miniature}:new[]{WorldSize.Miniature,WorldSize.Small,WorldSize.Medium,WorldSize.Large,WorldSize.Colossal};
            foreach(var size in sizes)
            {
                string sizeName=size==WorldSize.Large?"Large":size.ToString();
                app.ReturnToMenu();yield return new WaitForSecondsRealtime(.3f);generation=new Generation{size=size==WorldSize.Large?"Large":size.ToString(),seed="shaping-continents"};float start=Time.realtimeSinceStartup;
                bool creatorCase=AevoriaBootstrap.HasFlag("-aevoria008Addendum")&&size==WorldSize.Colossal;if(creatorCase)generation.seed="aevoria-001";
                app.CreateWorld("Restored worlds · "+sizeName,generation.seed,size,new GenerationSettings(mountains:creatorCase?12686.6143f:3600,landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();Require(app.WorldActive,"creation "+size);generation.seconds=Time.realtimeSinceStartup-start;result.generations.Add(generation);generation=null;
                app.Clock.Pause();app.Controller.ExternalDriver=true;var settings=new ApplicationSettings{AutosaveSeconds=0};settings.CloudQuality=0;settings.ShowFps=true;settings.ShowFrameTime=true;app.ApplySettings(settings);
                app.Controller.SetView(new GeoPoint(0,0),0,72,(float)app.Terrain.Geography.Configuration.WidthMeters*1.08f);yield return Ready();yield return Capture("03-"+sizeName+"-overview");if(!smoke)yield return Measure(sizeName+"-overview",5);
                app.Navigation.Toggle();yield return new WaitForSecondsRealtime(2);yield return Capture("04-"+sizeName+"-map");app.Navigation.Toggle();
                int forest=Best(i=>app.Ecology.Forest(i)),plain=Find(i=>app.Ecology.EnvironmentAt(i).Land&&app.Ecology.EnvironmentAt(i).Biome==Biome.Grassland);
                Focus(forest,350);yield return Ready();yield return Capture("05-"+sizeName+"-forest");if(!smoke)yield return Measure(sizeName+"-forest",5);
                int peak=0;var h=app.Terrain.Geography.Hydrology;for(int i=1;i<h.Ground.Length;i++)if(h.Ground[i]>h.Ground[peak])peak=i;
                app.Controller.SetView(new GeoPoint(h.X(peak),h.Y(peak)),25,55,20000);yield return Ready();yield return Capture("06-"+sizeName+"-mountain-system");
                if(AevoriaBootstrap.HasFlag("-aevoria008Addendum"))yield return AddendumViews(sizeName,forest,plain,peak);
                if(size==WorldSize.Medium||smoke)
                {
                    if(plain>=0){Focus(plain,500);yield return Ready();yield return Capture("07-grassland");}
                    foreach(var biome in new[]{Biome.Desert,Biome.TropicalForest,Biome.Tundra,Biome.Alpine,Biome.BorealForest,Biome.Snow}){int region=biome==Biome.Desert?BestDesert():Find(i=>app.Ecology.EnvironmentAt(i).Land&&app.Ecology.EnvironmentAt(i).Biome==biome);if(region<0){result.limitations.Add("No "+biome+" inspection region in "+size);continue;}Focus(region,biome==Biome.Alpine?3000:800);yield return Ready();yield return Capture("08-"+biome);}
                    var lakes=app.Waters.Layout.Lakes.Vertices;if(lakes.Count>0){var lake=lakes[lakes.Count/2];app.Controller.SetView(new GeoPoint(lake.X,lake.Z),25,42,1800);yield return Ready();yield return Capture("09-lake-shore");}
                    int river=-1,source=-1,mouth=-1,confluence=-1;var incoming=new int[h.Ground.Length];for(int i=0;i<h.Ground.Length;i++)if(h.IsChannelCandidate(i)){river=i;incoming[h.Receiver[i]]++;if(h.Ground[h.Receiver[i]]<=app.Terrain.Geography.SeaLevel)mouth=i;if(h.Accumulation[i]<50)source=i;}for(int i=0;i<incoming.Length;i++)if(incoming[i]>1&&h.IsChannelCandidate(i)){confluence=i;break;}
                    foreach(var item in new[]{new KeyValuePair<string,int>("10-river",river),new KeyValuePair<string,int>("11-source",source),new KeyValuePair<string,int>("12-mouth",mouth),new KeyValuePair<string,int>("13-confluence",confluence)})if(item.Value>=0){app.Controller.SetView(new GeoPoint(h.X(item.Value),h.Y(item.Value)),25,44,item.Key.Contains("mouth")?5000:1500);yield return Ready();yield return Capture(item.Key);if(item.Key=="10-river"){var sun=UnityEngine.Object.FindFirstObjectByType<Light>();if(sun!=null){var shadows=sun.shadows;sun.shadows=LightShadows.None;yield return null;yield return Capture("10-river-no-direct-shadows-diagnostic");sun.shadows=shadows;yield return null;}}}
                    app.DiagnosticsVisible=true;yield return new WaitForSecondsRealtime(.5f);yield return Capture("14-f3");app.DiagnosticsVisible=false;var ecology=GetComponent<EcologyPresentation>();ecology.Inspect=true;foreach(int layer in new[]{3,4,5}){ecology.MapLayer=layer;yield return new WaitForSecondsRealtime(.6f);yield return Capture("15-f4-"+layer);}ecology.Inspect=false;
                    app.Menu.Show(ApplicationPage.Pause);yield return Capture("16-pause");app.Menu.Show(ApplicationPage.Graphics);yield return Capture("17-graphics");app.Menu.Show(ApplicationPage.World);
                    var fog=app.Settings.Copy();fog.FogEnabled=false;app.ApplySettings(fog);yield return Capture("18-fog-off");fog.FogEnabled=true;app.ApplySettings(fog);yield return Capture("19-fog-on");
                    if(!smoke){yield return AdditionalViews();yield return RestorationMatrix(forest,plain);Focus(forest,600);yield return Ready();yield return Measure("rapid-zoom",8,false,true);yield return WeatherSequence(forest);app.Clock.SetSpeed(1);app.Clock.Start();yield return Measure("Medium-running-simulation",30);app.Clock.Pause();}
                }
                string ecologyHash=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture())),living=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));app.SaveWorld();yield return Operation();string id=app.Current.Id;app.ReturnToMenu();yield return new WaitForSecondsRealtime(.3f);app.LoadWorld(id);yield return Operation();Require(app.WorldActive,"reload "+size);app.Clock.Pause();app.Controller.ExternalDriver=true;
                bool same=ecologyHash==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture()))&&living==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));if(result.generations.Count==1)result.continuity=same;else result.continuity&=same;Require(same,"saved continuation "+size);yield return Capture("20-"+sizeName+"-reloaded");
                if(!smoke&&size==WorldSize.Colossal)
                {Focus(Best(i=>app.Ecology.Forest(i)),750);yield return Ready();yield return Measure("Colossal-moving",180,true);var high=app.Settings.Copy();high.ApplyGraphicsPreset(2);high.TerrainDetail=3;high.TerrainDistance=10;high.DistantDetail=2;high.VegetationDistance=8000;high.VegetationDensity=1.5f;high.CloudQuality=2;high.WaterQuality=2;high.ShadowDistance=4000;app.ApplySettings(high);yield return Ready();yield return Capture("21-high-quality");yield return Measure("Colossal-high",15,true);}
            }
            app.ReturnToMenu();yield return new WaitForSecondsRealtime(.5f);result.cleanup=FindObjectsByType<WorldRenderer>(FindObjectsSortMode.None).Length==0&&FindObjectsByType<WorldCloudLayer>(FindObjectsSortMode.None).Length==0&&FindObjectsByType<RegionalTerrain>(FindObjectsSortMode.None).Length==0;
            if(!smoke){app.CreateWorld("Cancellation validation","cancel-008",WorldSize.Colossal,new GenerationSettings(),new ClimateSettings());yield return null;app.CancelPreparation();yield return Operation();result.cancellation=!app.WorldActive;Require(result.cancellation,"generation cancellation");}
            app.Menu.SelectedChapter=7;app.Menu.Show(ApplicationPage.Chronicles);yield return Capture("30-book-viii");app.Menu.Show(ApplicationPage.Changelog);yield return Capture("31-changelog");app.Menu.Show(ApplicationPage.Home);
            File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(result,true));Require(result.continuity&&result.cleanup,"continuity and cleanup");Debug.Log("AEVORIA008_AUDIT_PASS");Application.Quit();
        }
        private IEnumerator AdditionalViews()
        {
            var geo=app.Terrain.Geography;var h=geo.Hydrology;int entry=-1,outlet=-1,shallow=-1,deep=-1;
            for(int i=0;i<h.Ground.Length;i++)
            {if(h.Ground[i]<geo.SeaLevel-2000&&deep<0)deep=i;if(h.Ground[i]<geo.SeaLevel-2&&h.Ground[i]>geo.SeaLevel-45&&geo.Sample(new GeoPoint(h.X(i),h.Y(i))).TemperatureCelsius>22)shallow=i;if(h.IsChannelCandidate(i)){int r=h.Receiver[i];bool lake=geo.InlandSurfaces.LakeAt(h.X(i),h.Y(i),out _),next=geo.InlandSurfaces.LakeAt(h.X(r),h.Y(r),out _);if(!lake&&next)entry=i;if(lake&&!next&&h.Ground[r]>geo.SeaLevel)outlet=i;}}
            foreach(var item in new[]{new KeyValuePair<string,int>("24-river-lake-entry",entry),new KeyValuePair<string,int>("25-lake-outlet",outlet),new KeyValuePair<string,int>("26-tropical-shallows",shallow),new KeyValuePair<string,int>("27-deep-ocean",deep)})
            {if(item.Value<0){result.limitations.Add("No eligible "+item.Key+" site in Medium");continue;}int i=item.Value;app.Controller.SetView(new GeoPoint(h.X(i),h.Y(i)),25,45,item.Key.Contains("ocean")?15000:2000);yield return Ready();yield return Capture(item.Key);if(item.Key.Contains("shallows"))yield return Measure("ocean-near-far-transition",8,false,true);}
            int clear=Find(i=>app.Ecology.EnvironmentAt(i).Land&&app.Ecology.WeatherAt(i,app.Clock.State.Tick).Cloud<.2);if(clear>=0){var s=app.Settings.Copy();s.CloudQuality=1;app.ApplySettings(s);Focus(clear,2500);yield return Ready();yield return Capture("28-clear-weather");}
            int foothill=Find(i=>app.Ecology.EnvironmentAt(i).Land&&app.Ecology.EnvironmentAt(i).Height>500&&app.Ecology.EnvironmentAt(i).Height<1800);
            if(foothill>=0){Focus(foothill,150);yield return Ready();app.Explorer.AcceptInput=false;bool entered=app.Explorer.EnterAt(app.Ecology.EnvironmentAt(foothill).Center);if(entered){float until=Time.realtimeSinceStartup+30;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<until)yield return null;result.exploration=app.Explorer.Ready;app.Explorer.SetFirstPerson(true);yield return new WaitForSecondsRealtime(1);yield return Capture("29-ground-foothills");app.Explorer.SetFirstPerson(false);yield return Capture("29-third-person");app.Explorer.Exit();app.Explorer.AcceptInput=true;}else result.limitations.Add("No safe ground spawn at selected foothill");}
            // Probe a cloned model first so presentation can settle at a known
            // supported dry patch before measuring the real tool/edit install.
            var probe=new LivingWorldState(geo,app.Ecology,app.Clock.State.Tick,app.Living.Capture());probe.Traveler.Tool=ToolKind.Shovel;GeoPoint digPoint=default;bool supported=false;
            for(int i=0;i<app.Ecology.Count&&!supported;i++)if(app.Ecology.EnvironmentAt(i).Land)
            {var p=app.Ecology.EnvironmentAt(i).Center;double x=Math.Round(p.X/32)*32,z=Math.Round(p.Y/32)*32;float ground=geo.ElevationAt(x,z);if(geo.Hydrology.IsLakeWater(x,z,ground)||geo.Hydrology.IsRiverWater(x,z,ground))continue;if(probe.Dig(x,ground,z,ResourceKind.Soil,out _)){digPoint=new GeoPoint(x,z);supported=true;}}
            if(supported)
            {
                app.Controller.SetView(digPoint,25,55,30);yield return Ready();yield return Capture("29-solid-before");app.Living.Traveler.Tool=ToolKind.Shovel;result.excavation=app.Explorer.DigAt(new Vector3((float)digPoint.X,geo.ElevationAt(digPoint.X,digPoint.Y),(float)digPoint.Y),ResourceKind.Soil);
                if(result.excavation){yield return Measure("experimental-excavation-install",6);result.excavation=app.Excavations.Ready&&app.Excavations.InstalledRevision>0;yield return Capture("29-solid-excavation");}
            }
            if(!result.excavation)result.limitations.Add("No completed native excavation install in the sampled dry patches");
        }
        private IEnumerator AddendumViews(string sizeName,int forest,int plain,int peak)
        {
            var geo=app.Terrain.Geography;var h=geo.Hydrology;var original=app.Settings.Copy();var clear=original.Copy();clear.AutosaveSeconds=0;clear.FogEnabled=false;clear.CloudQuality=1;clear.RenderClouds=false;clear.RenderWeatherEffects=true;app.ApplySettings(clear);
            var preloadQuality=clear.Copy();preloadQuality.ApplyGraphicsPreset(1);app.ApplySettings(preloadQuality);Focus(forest,850);yield return Ready();float preloadStart=Time.realtimeSinceStartup;app.Terrain.PreloadVisibleArea();float end=preloadStart+180;while(app.Terrain.Preloading&&Time.realtimeSinceStartup<end)yield return null;Require(app.Terrain.PreloadProgress==1,"preload installed "+sizeName);addendum.preloadSeconds.Add(Time.realtimeSinceStartup-preloadStart);addendum.preload=true;yield return Capture("addendum-"+sizeName+"-preloaded-viewport");
            app.Terrain.PreloadVisibleArea();app.Terrain.CancelPreload();Require(!app.Terrain.Preloading,"preload cancellation");addendum.cancellation=true;
            var points=new List<GeoPoint>{new GeoPoint(h.X(peak),h.Y(peak)),new GeoPoint(app.Controller.Focus.x,app.Controller.Focus.z)};if(plain>=0)points.Add(app.Ecology.EnvironmentAt(plain).Center);
            int river=-1;for(int i=0;i<h.Ground.Length;i++)if(h.IsRiver(i)){river=i;break;}if(river>=0)points.Add(new GeoPoint(h.X(river),h.Y(river)));
            using(var writer=new StreamWriter(Path.Combine(output,"zoom-"+sizeName+".csv")))
            {
                writer.WriteLine("site,frame,orbitDistance,x,y,z,clearance,nearClip");int site=0;
                foreach(var point in points)
                {
                    float overview=(float)geo.Configuration.WidthMeters*1.05f;app.Controller.SetView(point,25,55,overview);
                    for(int i=0;i<180;i++)
                    {
                        float t=i/179f,distance=Mathf.Exp(Mathf.Lerp(Mathf.Log(overview),Mathf.Log(2),t));app.Controller.SetOrbitTargets(25,55,distance);app.Controller.Advance(1/60f);yield return null;
                        var p=Camera.main.transform.position;float clearance=app.Controller.Clearance;Require(clearance>=2.9f&&!float.IsNaN(p.y),"zoom terrain safety");Require(Camera.main.nearClipPlane<clearance,"near plane safety");addendum.minClearance=Mathf.Min(addendum.minClearance,clearance);addendum.zoomSamples++;
                        writer.WriteLine(FormattableString.Invariant($"{site},{i},{app.Controller.Distance:R},{p.x:R},{p.y:R},{p.z:R},{clearance:R},{Camera.main.nearClipPlane:R}"));
                        if(site==0&&i%36==0)yield return Capture("addendum-"+sizeName+"-zoom-sequence-"+i.ToString("000"));
                    }
                    yield return Capture("addendum-"+sizeName+"-zoom-ground-"+site);for(int i=0;i<80;i++){app.Controller.ZoomNotches(i%2==0?-5:5);app.Controller.Advance(1/60f);yield return null;Require(app.Controller.Clearance>=2.9f,"rapid wheel safety");}site++;
                }
            }
            addendum.zoom=true;
            app.Controller.SetView(new GeoPoint(0,0),0,72,(float)geo.Configuration.WidthMeters*1.08f);yield return Ready();
            string authority=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));clear.RenderClouds=true;clear.RenderWeatherEffects=true;clear.CloudDensity=2;app.ApplySettings(clear);yield return new WaitForSecondsRealtime(1);yield return Capture("addendum-"+sizeName+"-cumulus-on");
            clear.RenderWeatherEffects=false;app.ApplySettings(clear);yield return Capture("addendum-"+sizeName+"-weather-off-clouds-on");clear.RenderClouds=false;clear.RenderWeatherEffects=true;app.ApplySettings(clear);yield return Capture("addendum-"+sizeName+"-clouds-off-weather-on");Require(authority==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture())),"display toggles preserve world state");addendum.toggles=true;
            if(sizeName=="Medium")
            {
                string archivePath=Path.Combine(app.Repository.Root,app.Current.Id,"world.xml"),previous=ArchiveCodec.Hash(File.ReadAllBytes(archivePath));int stock=app.Living.Traveler.Wood;app.Living.Traveler.Wood=-1;app.SaveWorld();yield return Operation();app.Living.Traveler.Wood=stock;
                addendum.rejectedSaveHashBefore=previous;addendum.rejectedSaveHashAfter=ArchiveCodec.Hash(File.ReadAllBytes(archivePath));Require(app.WorldActive&&app.Message.Contains("Inventory")&&previous==addendum.rejectedSaveHashAfter,"invalid native save preserves last archive");addendum.invalidSaveProtected=true;yield return Capture("addendum-save-rejected-with-specific-diagnostic");
                app.Explorer.AcceptInput=false;bool entered=false;for(int i=0;i<app.Ecology.Count&&!entered;i++)if(app.Ecology.EnvironmentAt(i).Land)entered=app.Explorer.EnterAt(app.Ecology.EnvironmentAt(i).Center);
                Require(entered,"flight spawn");end=Time.realtimeSinceStartup+60;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Require(app.Explorer.Ready,"flight collision ready");
                app.Explorer.RegisterSpaceTap(100);Require(app.Explorer.RegisterSpaceTap(100.2f),"double Space flight");for(int i=0;i<240;i++){app.Explorer.StepFlight(Vector2.up,true,false,true,1/60f);yield return null;}app.Explorer.SetFirstPerson(true);yield return Capture("addendum-flight-first-person");app.Explorer.SetFirstPerson(false);yield return Capture("addendum-flight-third-person");
                var archive=WorldRecord.Capture(null,"Airborne validation",null,geo,app.Clock,"0.0.8",app.Ecology,app.Living);archive.Validate();Require(ArchiveCodec.Unpack(ArchiveCodec.Pack(archive)).Living.Traveler.Flying,"airborne archive");addendum.flight=true;
                app.Explorer.Exit();app.Explorer.AcceptInput=true;app.Controller.SetView(new GeoPoint(geo.Configuration.WidthMeters*.5-100,0),90,0,10);yield return Ready();yield return Capture("addendum-boundary-horizon");
            }
            if(sizeName=="Colossal")
            {
                var traveler=app.Living.Traveler;double x=traveler.X,y=traveler.Y,z=traveler.Z;bool hadPosition=traveler.HasPosition;
                traveler.X=h.X(peak);traveler.Z=h.Y(peak);traveler.Y=geo.ElevationAt(traveler.X,traveler.Z)+4;traveler.HasPosition=true;Require(traveler.Y>10000,"creator relief reproduces old traveler altitude rejection");
                app.SaveWorld(duplicate:true,newName:"High mountain save regression");yield return Operation();Require(app.Message.StartsWith("World saved"),"high mountain native save");var saved=ArchiveCodec.Unpack(File.ReadAllBytes(Path.Combine(app.Repository.Root,app.Current.Id,"world.xml")));Require(saved.Living.Traveler.Y==traveler.Y,"high mountain pose retained");addendum.highSavedAltitude=saved.Living.Traveler.Y;addendum.highAltitudeSave=true;
                traveler.X=x;traveler.Y=y;traveler.Z=z;traveler.HasPosition=hadPosition;
            }
            addendum.cacheHits+=app.Terrain.CacheHits;addendum.beforeRenderChecks=app.Controller.SafetyChecksBeforeRender;Require(addendum.beforeRenderChecks>0,"standalone before-render camera safety executed");app.ApplySettings(original);File.WriteAllText(Path.Combine(output,"addendum.json"),JsonUtility.ToJson(addendum,true));
        }
        private IEnumerator RestorationMatrix(int forest,int plain)
        {
            var geo=app.Terrain.Geography;
            foreach(var group in new[]{new KeyValuePair<string,int>("grassland",plain),new KeyValuePair<string,int>("forest",forest),new KeyValuePair<string,int>("desert",BestDesert())})
            {
                if(group.Value<0){result.limitations.Add("Matrix has no "+group.Key+" site");continue;}
                foreach(float distance in new[]{120f,850f,5000f}){Focus(group.Value,distance);yield return Ready();yield return new WaitForSecondsRealtime(1.5f);yield return Capture("matrix-"+group.Key+"-"+distance.ToString("0"));}
            }
            var lakes=geo.InlandSurfaces.Lakes.Vertices;int deepest=0;for(int i=1;i<lakes.Count;i++)if(lakes[i].Depth>lakes[deepest].Depth)deepest=i;
            if(lakes.Count>0){var p=lakes[deepest];app.Controller.SetView(new GeoPoint(p.X,p.Z),25,48,900,p.Level);yield return Ready();yield return Capture("matrix-deep-lake");app.Controller.SetView(new GeoPoint(p.X,p.Z),25,48,8000,p.Level);yield return Ready();yield return Capture("matrix-deep-lake-context");}
            var areas=new Dictionary<float,(double area,WaterPoint point)>();
            for(int i=0;i<lakes.Count;i+=3){var a=lakes[i];var b=lakes[i+1];var c=lakes[i+2];double area=Math.Abs((b.X-a.X)*(c.Z-a.Z)-(b.Z-a.Z)*(c.X-a.X))*.5;areas.TryGetValue(a.Level,out var item);areas[a.Level]=(item.area+area,a.Depth>item.point.Depth?a:item.point);}
            double smallest=double.MaxValue;WaterPoint pond=default;foreach(var item in areas.Values)if(item.area>1200&&item.area<smallest){smallest=item.area;pond=item.point;}
            if(smallest<double.MaxValue){app.Controller.SetView(new GeoPoint(pond.X,pond.Z),25,48,Mathf.Clamp((float)Math.Sqrt(smallest)*3,150,2500),pond.Level);yield return Ready();yield return Capture("matrix-small-pond");}
            // A real coastal point is found by bracketing a land/ocean analysis
            // edge, then solving the physical height field for a 1.5 m seabed.
            var h=geo.Hydrology;GeoPoint coast=default;bool found=false;
            for(int i=0;i<h.Ground.Length&&!found;i++)if(h.Ground[i]>geo.SeaLevel&&h.Receiver[i]>=0&&h.Ground[h.Receiver[i]]<geo.SeaLevel)
            {int r=h.Receiver[i];double ax=h.X(i),az=h.Y(i),bx=h.X(r),bz=h.Y(r);for(int k=0;k<28;k++){double mx=(ax+bx)*.5,mz=(az+bz)*.5;if(geo.ElevationAt(mx,mz)>geo.SeaLevel-1.5){ax=mx;az=mz;}else{bx=mx;bz=mz;}}coast=new GeoPoint((ax+bx)*.5,(az+bz)*.5);found=true;}
            if(found)
            {
                var saved=app.Settings.Copy();foreach(int quality in new[]{0,1,2}){var s=saved.Copy();s.WaterTransparency=quality;app.ApplySettings(s);app.Controller.SetView(coast,25,55,90,geo.SeaLevel);yield return Ready();yield return new WaitForSecondsRealtime(1.5f);yield return Capture("matrix-shallows-transparency-"+quality);}app.ApplySettings(saved);
                app.Controller.SetView(coast,25,42,1800,geo.SeaLevel);yield return Ready();yield return Capture("matrix-coast-regional");yield return Measure("coastal-streaming",12,true);
            }
            Focus(forest,120);yield return Ready();app.Explorer.AcceptInput=false;if(app.Explorer.EnterAt(new GeoPoint(app.Controller.Focus.x,app.Controller.Focus.z))){float limit=Time.realtimeSinceStartup+30;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<limit)yield return null;app.Explorer.SetFirstPerson(true);yield return new WaitForSecondsRealtime(1);yield return Capture("matrix-forest-inside");app.Explorer.Exit();}app.Explorer.AcceptInput=true;
            var cloud=app.Settings.Copy();cloud.CloudQuality=2;app.ApplySettings(cloud);Focus(forest,1500);yield return Ready();yield return Capture("matrix-cloud-medium");Focus(forest,120);yield return Ready();var under=app.Controller.Focus;Camera.main.transform.SetPositionAndRotation(new Vector3(under.x,geo.ElevationAt(under.x,under.z)+40,under.z),Quaternion.LookRotation(new Vector3(.3f,.45f,1)));yield return new WaitForSecondsRealtime(2);yield return Capture("matrix-cloud-below");Focus(forest,30000);yield return Ready();yield return Capture("matrix-cloud-overview");
            foreach(var point in new[]{new GeoPoint(geo.Configuration.WidthMeters*.49,0),new GeoPoint(-geo.Configuration.WidthMeters*.49,0),new GeoPoint(0,geo.Configuration.DepthMeters*.49),new GeoPoint(0,-geo.Configuration.DepthMeters*.49)})
            {app.Controller.SetView(point,25,55,15000,geo.SeaLevel);yield return Ready();yield return Capture("matrix-edge-"+views.Count);}
            var high=app.Settings.Copy();high.ApplyGraphicsPreset(2);high.TerrainDetail=3;high.TerrainDistance=10;high.DistantDetail=2;high.VegetationDistance=8000;high.VegetationDensity=1.5f;high.CloudQuality=2;high.WaterQuality=2;high.ShadowDistance=4000;app.ApplySettings(high);
            Focus(forest,1800);yield return Ready();yield return Capture("matrix-heavy-forest");yield return Measure("high-quality-forest",20);
            int peak=0;for(int i=1;i<h.Ground.Length;i++)if(h.Ground[i]>h.Ground[peak])peak=i;app.Controller.SetView(new GeoPoint(h.X(peak),h.Y(peak)),25,42,7000);yield return Ready();yield return Capture("matrix-heavy-mountains");yield return Measure("high-quality-mountains",20,true);
        }
        private IEnumerator WeatherSequence(int region)
        {
            var p=app.Ecology.EnvironmentAt(region).Center;var geo=app.Terrain.Geography;long start=app.Clock.State.Tick;long rain=-1,dry=-1;
            for(long tick=start;tick<start+1440;tick+=4)if(RegionalAtmosphere.Forecast(geo,tick,p).Precipitation>.5){rain=tick;break;}
            if(rain<0){result.limitations.Add("No sufficiently strong rain window at selected forest");yield break;}
            var settings=app.Settings.Copy();settings.CloudQuality=1;app.ApplySettings(settings);app.Controller.SetView(p,25,45,10000);result.wetBefore=app.Living.Atmosphere.Wetness(region);yield return Advance(Math.Max(start,rain-16));yield return new WaitForSecondsRealtime(2);yield return Capture("22-approaching-storm");
            if(AevoriaBootstrap.HasFlag("-aevoria008Addendum"))
            {
                yield return Advance(rain);app.Controller.SetView(p,25,-15,600);yield return Ready();var controls=app.Settings.Copy();controls.RenderClouds=true;controls.RenderWeatherEffects=true;app.ApplySettings(controls);yield return new WaitForSecondsRealtime(1);yield return Capture("addendum-precipitation-and-clouds-on");
                string state=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));controls.RenderWeatherEffects=false;app.ApplySettings(controls);yield return Capture("addendum-precipitation-off-clouds-on");controls.RenderWeatherEffects=true;controls.RenderClouds=false;app.ApplySettings(controls);yield return new WaitForSecondsRealtime(1);yield return Capture("addendum-precipitation-on-clouds-off");Require(state==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture())),"independent storm display preserves authority");app.ApplySettings(settings);app.Controller.SetView(p,25,45,10000);yield return Ready();
            }
            yield return Advance(rain+16);yield return new WaitForSecondsRealtime(2);result.wetRain=app.Living.Atmosphere.Wetness(region);yield return Capture("22-rain-cloud-shadow-wetness");app.Controller.SetView(p,25,35,350);yield return Ready();yield return Capture("22-below-storm");yield return Measure("regional-rain",10);
            for(long tick=app.Clock.State.Tick+4;tick<app.Clock.State.Tick+2880;tick+=4){bool clear=true;for(int d=0;d<=80;d+=4)if(RegionalAtmosphere.Forecast(geo,tick+d,p).Precipitation>.005){clear=false;break;}if(clear){dry=tick;break;}}
            if(dry>=0){yield return Advance(dry);result.wetDryStart=app.Living.Atmosphere.Wetness(region);yield return new WaitForSecondsRealtime(2);yield return Capture("23-drying-start");yield return Advance(dry+32);yield return new WaitForSecondsRealtime(2);result.wetDry=app.Living.Atmosphere.Wetness(region);yield return Capture("23-drying-ground");}
        }
        private IEnumerator Advance(long target){app.Clock.SetSpeed(1);while(app.Clock.State.Tick<target){app.Clock.Start();app.Clock.Update(.25,1);app.Clock.Pause();app.Ecology.AdvanceTo(app.Clock.State.Tick,2);app.Living.Atmosphere.AdvanceTo(app.Clock.State.Tick,2);app.Living.Seasons.AdvanceTo(app.Clock.State.Tick,2);yield return null;}}
        private int Find(Func<int,bool> predicate){for(int i=0;i<app.Ecology.Count;i++)if(predicate(i))return i;return -1;}
        private int BestDesert()
        {int best=-1;double score=double.MinValue;for(int i=0;i<app.Ecology.Count;i++){var e=app.Ecology.EnvironmentAt(i);if(!e.Land||e.Biome!=Biome.Desert)continue;double value=(e.Temperature>18?3:0)+(e.Height>150&&e.Height<650?2:0)-e.Moisture*5;if(value>score){score=value;best=i;}}return best;}
        private int Best(Func<int,double> value){int best=Find(i=>app.Ecology.EnvironmentAt(i).Land);for(int i=0;i<app.Ecology.Count;i++)if(app.Ecology.EnvironmentAt(i).Land&&value(i)>value(best))best=i;return best;}
        private ProceduralGeography forestGeo;private int forestRegion=-1;private GeoPoint forestPoint;
        private void Focus(int region,float distance)
        {
            var geo=app.Terrain.Geography;
            if(forestGeo!=geo)
            {
                forestGeo=geo;forestRegion=Best(i=>app.Ecology.Forest(i));forestPoint=app.Ecology.EnvironmentAt(forestRegion).Center;double best=-1;
                // Select a real stand within the ecological authority rather
                // than assuming its coarse region centre is forested terrain.
                for(int i=0;i<app.Ecology.Count;i++)if(app.Ecology.EnvironmentAt(i).Land&&app.Ecology.Forest(i)>.3)
                {
                    var p=app.Ecology.EnvironmentAt(i).Center;int cx=(int)Math.Floor(p.X/16),cz=(int)Math.Floor(p.Y/16);int count=0;
                    for(int z=-4;z<=4;z++)for(int x=-4;x<=4;x++)if(VegetationDistribution.TryTree(geo,cx+x*4,cz+z*4,out _))count++;
                    double score=count*app.Ecology.Forest(i);if(score>best){best=score;forestPoint=p;}
                }
            }
            app.Controller.SetView(region==forestRegion?forestPoint:app.Ecology.EnvironmentAt(region).Center,25,45,distance);
        }
        private IEnumerator Operation(){float end=Time.realtimeSinceStartup+300;string stage=null;while(app.Busy&&Time.realtimeSinceStartup<end){if(generation!=null&&stage!=app.Progress){stage=app.Progress;generation.stages.Add(stage);}yield return null;}Require(!app.Busy,"operation timeout");}
        private IEnumerator Ready(){float start=Time.realtimeSinceStartup,end=start+150;yield return new WaitForSecondsRealtime(.35f);while(!app.Terrain.IsReady&&Time.realtimeSinceStartup<end)yield return null;Require(app.Terrain.IsReady,"streaming ready");lastReady=Time.realtimeSinceStartup-start;yield return new WaitForSecondsRealtime(.6f);}
        [Serializable]private sealed class ViewList{public List<ViewEvidence> views;}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));if(app.WorldActive){if(name.Contains("overview"))Require(Shader.GetGlobalVector("_AevoriaRegionalGrid").x<=Math.Max(8,app.Terrain.Geography.Configuration.WidthMeters*1.6/WorldRenderer.OverviewResolution),"regional overview spacing");var p=app.Controller.Focus;views.Add(new ViewEvidence{scene=name,solidPreparationMs=app.Excavations.LastPreparationMilliseconds,solidBakeWaitMs=app.Excavations.LastBakeWaitMilliseconds,solidColliderMs=app.Excavations.LastColliderMilliseconds,solidInstallMs=app.Excavations.LastInstallMilliseconds,solidVertices=app.Excavations.InstalledVertices,solidTriangles=app.Excavations.InstalledTriangles,seed=app.Terrain.Geography.Configuration.Seed,tick=app.Clock.State.Tick,settings=app.Settings.Copy(),cameraPosition=Camera.main.transform.position,cameraEuler=Camera.main.transform.eulerAngles,x=p.x,z=p.z,ground=app.Terrain.Geography.ElevationAt(p.x,p.z),cameraHeight=Camera.main.transform.position.y,distance=app.Controller.Distance,trees=app.Terrain.Vegetation.VisibleTrees,regionalSpacing=Shader.GetGlobalVector("_AevoriaRegionalGrid").x,details=app.Details.VisibleSites,submergedRocks=app.Details.SubmergedRocks,biome=app.Terrain.Geography.Sample(new GeoPoint(p.x,p.z)).Biome.ToString(),chunks=app.Terrain.ActiveChunks,readySeconds=lastReady,triangles=app.Terrain.ResidentTriangles});File.WriteAllText(Path.Combine(output,"views.json"),JsonUtility.ToJson(new ViewList{views=views},true));}yield return null;}
        private void Require(bool value,string name){if(!value){File.WriteAllText(Path.Combine(output,"failure.txt"),name+": "+app.Message);Debug.LogError("Audit008 failed "+name);Application.Quit(9);throw new InvalidOperationException(name);}}
        private void ProbeClipOverride(){if(probeLegacyNear&&Camera.main!=null)Camera.main.nearClipPlane=.05f;}
        private void Failure(string text,string stack,LogType type){if(text.Contains("Screen position out of view frustum")&&Camera.main!=null){frustumWarnings++;var camera=Camera.main;File.AppendAllText(Path.Combine(output,"frustum-state.jsonl"),JsonUtility.ToJson(new FrustumState{phase=projectionProbePhase,position=camera.transform.position,near=camera.nearClipPlane,far=camera.farClipPlane,distance=app.Controller.Distance,clearance=app.Controller.Clearance,viewProjectionDeterminant=(camera.projectionMatrix*camera.worldToCameraMatrix).determinant})+"\n");}if(type==LogType.Exception){File.WriteAllText(Path.Combine(output,"exception.txt"),text+"\n"+stack);Application.Quit(10);}}
        private IEnumerator Measure(string scenario,float seconds,bool move=false,bool zoom=false)
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;using var trace=new PerformanceTrace();trace.Reset();var values=new List<float>(100000);yield return null;yield return null;int start=rendered;var process=System.Diagnostics.Process.GetCurrentProcess();double cpu=process.TotalProcessorTime.TotalMilliseconds,gpu=0;int gpuFrames=0;var timing=new FrameTiming[1];float end=Time.realtimeSinceStartup+seconds;
            while(Time.realtimeSinceStartup<end){yield return null;if(move){app.Controller.MoveHorizontal(new Vector2(Mathf.Sin(Time.time*.2f),Mathf.Cos(Time.time*.2f))*1000*Time.unscaledDeltaTime);app.Controller.Advance(Time.unscaledDeltaTime);}if(zoom)app.Controller.SetView(new GeoPoint(app.Controller.Focus.x,app.Controller.Focus.z),25,55,Mathf.Exp(Mathf.Lerp(Mathf.Log(20),Mathf.Log((float)app.Terrain.Geography.Configuration.WidthMeters),(Mathf.Sin(Time.time*1.5f)+1)*.5f)));float ms=Time.unscaledDeltaTime*1000;values.Add(ms);trace.Record(ms,app.Terrain.LastUpdateMilliseconds,app.Terrain.OwnedMeshes);FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){gpu+=timing[0].gpuFrameTime;gpuFrames++;}}
            double cpuMs=(process.TotalProcessorTime.TotalMilliseconds-cpu)/values.Count,sum=0;values.Sort();foreach(float value in values)sum+=value;result.measurements.Add(new Measurement{scenario=scenario,frames=values.Count,rendered=rendered-start,width=Screen.width,height=Screen.height,meanMs=sum/values.Count,p95Ms=values[(int)(values.Count*.95)],p99Ms=values[(int)(values.Count*.99)],maxMs=values[values.Count-1],cpuMs=cpuMs,gpuMs=gpuFrames>0?gpu/gpuFrames:-1,managedMiB=GC.GetTotalMemory(false)/1048576.0,unityMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0});trace.Write(Path.Combine(output,scenario+"-frames.csv"));File.WriteAllText(Path.Combine(output,"progress.json"),JsonUtility.ToJson(result,true));
        }
        private void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;Application.logMessageReceived-=Failure;Application.onBeforeRender-=ProbeClipOverride;}
    }
}
