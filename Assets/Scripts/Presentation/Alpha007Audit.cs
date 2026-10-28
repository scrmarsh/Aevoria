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
    public sealed class Alpha007Audit : MonoBehaviour
    {
        [Serializable]public sealed class Measurement{public string scenario;public int frames,rendered,width,height;public double meanMs,p95Ms,p99Ms,maxMs,cpuMs,gpuMs,managedMiB,unityMiB;}
        [Serializable]public sealed class Generation{public string size,seed;public double seconds;public List<string> stages=new List<string>();}
        [Serializable]public sealed class Results{public string version=ApplicationVersion.Number,cpu,gpu;public bool continuity,cleanup,cancellation,exploration,excavation;public List<Generation> generations=new List<Generation>();public List<Measurement> measurements=new List<Measurement>();public List<string> limitations=new List<string>();public float wetBefore,wetRain,wetDryStart,wetDry;}
        private AevoriaBootstrap app;private Results result;private string output;private int rendered;private bool smoke;private Generation generation;
        private void Rendered(ScriptableRenderContext context,Camera camera){if(camera.CompareTag("MainCamera"))rendered++;}
        private IEnumerator Start()
        {
            if(AevoriaBootstrap.Argument("-aevoriaTestRoot")==null){Debug.LogError("Audit007 requires isolated save root");Application.Quit(4);yield break;}
            app=GetComponent<AevoriaBootstrap>();output=AevoriaBootstrap.Argument("-aevoriaOutput");Directory.CreateDirectory(output);smoke=AevoriaBootstrap.HasFlag("-aevoria007Smoke");result=new Results{cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName};Application.runInBackground=true;Application.logMessageReceived+=Failure;RenderPipelineManager.endCameraRendering+=Rendered;
            yield return new WaitForSecondsRealtime(1);yield return Capture("01-menu");app.Menu.Show(ApplicationPage.Create);yield return new WaitForSecondsRealtime(2);yield return Capture("02-creation");
            if(AevoriaBootstrap.HasFlag("-aevoria007WeatherOnly"))
            {
                app.CreateWorld("Regional weather validation","shaping-continents",WorldSize.Medium,new GenerationSettings(landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();Require(app.WorldActive,"weather world");app.Clock.Pause();app.Controller.ExternalDriver=true;yield return WeatherSequence(Best(i=>app.Ecology.Forest(i)));Require(result.wetRain>result.wetBefore&&result.wetDry<result.wetDryStart&&result.wetDry>0,"observed gradual drying");
                string before=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));app.SaveWorld();yield return Operation();string id=app.Current.Id;app.ReturnToMenu();yield return new WaitForSecondsRealtime(.3f);app.LoadWorld(id);yield return Operation();app.Clock.Pause();result.continuity=before==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));Require(result.continuity,"weather continuation");app.ReturnToMenu();yield return null;File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(result,true));Debug.Log("AEVORIA007_WEATHER_PASS");Application.Quit();yield break;
            }
            var sizes=smoke?new[]{WorldSize.Miniature}:new[]{WorldSize.Miniature,WorldSize.Small,WorldSize.Medium,WorldSize.Large,WorldSize.Colossal};
            foreach(var size in sizes)
            {
                string sizeName=size==WorldSize.Large?"Large":size.ToString();
                app.ReturnToMenu();yield return new WaitForSecondsRealtime(.3f);generation=new Generation{size=size==WorldSize.Large?"Large":size.ToString(),seed="shaping-continents"};float start=Time.realtimeSinceStartup;
                app.CreateWorld("Shaping worlds · "+sizeName,generation.seed,size,new GenerationSettings(landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();Require(app.WorldActive,"creation "+size);generation.seconds=Time.realtimeSinceStartup-start;result.generations.Add(generation);generation=null;
                app.Clock.Pause();app.Controller.ExternalDriver=true;var settings=app.Settings.Copy();settings.CloudQuality=0;settings.ShowFps=true;settings.ShowFrameTime=true;app.ApplySettings(settings);
                app.Controller.SetView(new GeoPoint(0,0),0,72,(float)app.Terrain.Geography.Configuration.WidthMeters*1.08f);yield return Ready();yield return Capture("03-"+sizeName+"-overview");if(!smoke)yield return Measure(sizeName+"-overview",5);
                app.Navigation.Toggle();yield return new WaitForSecondsRealtime(2);yield return Capture("04-"+sizeName+"-map");app.Navigation.Toggle();
                int forest=Best(i=>app.Ecology.Forest(i)),plain=Find(i=>app.Ecology.EnvironmentAt(i).Land&&app.Ecology.EnvironmentAt(i).Biome==Biome.Grassland);
                Focus(forest,350);yield return Ready();yield return Capture("05-"+sizeName+"-forest");if(!smoke)yield return Measure(sizeName+"-forest",5);
                int peak=0;var h=app.Terrain.Geography.Hydrology;for(int i=1;i<h.Ground.Length;i++)if(h.Ground[i]>h.Ground[peak])peak=i;
                app.Controller.SetView(new GeoPoint(h.X(peak),h.Y(peak)),25,55,20000);yield return Ready();yield return Capture("06-"+sizeName+"-mountain-system");
                if(size==WorldSize.Medium||smoke)
                {
                    if(plain>=0){Focus(plain,500);yield return Ready();yield return Capture("07-grassland");}
                    foreach(var biome in new[]{Biome.Desert,Biome.TropicalForest,Biome.Tundra,Biome.Alpine,Biome.BorealForest,Biome.Snow}){int region=Find(i=>app.Ecology.EnvironmentAt(i).Land&&app.Ecology.EnvironmentAt(i).Biome==biome);if(region<0){result.limitations.Add("No "+biome+" inspection region in "+size);continue;}Focus(region,biome==Biome.Alpine?3000:800);yield return Ready();yield return Capture("08-"+biome);}
                    var lakes=app.Waters.Layout.Lakes.Vertices;if(lakes.Count>0){var lake=lakes[lakes.Count/2];app.Controller.SetView(new GeoPoint(lake.X,lake.Z),25,42,1800);yield return Ready();yield return Capture("09-lake-shore");}
                    int river=-1,source=-1,mouth=-1,confluence=-1;var incoming=new int[h.Ground.Length];for(int i=0;i<h.Ground.Length;i++)if(h.IsChannelCandidate(i)){river=i;incoming[h.Receiver[i]]++;if(h.Ground[h.Receiver[i]]<=app.Terrain.Geography.SeaLevel)mouth=i;if(h.Accumulation[i]<50)source=i;}for(int i=0;i<incoming.Length;i++)if(incoming[i]>1&&h.IsChannelCandidate(i)){confluence=i;break;}
                    foreach(var item in new[]{new KeyValuePair<string,int>("10-river",river),new KeyValuePair<string,int>("11-source",source),new KeyValuePair<string,int>("12-mouth",mouth),new KeyValuePair<string,int>("13-confluence",confluence)})if(item.Value>=0){app.Controller.SetView(new GeoPoint(h.X(item.Value),h.Y(item.Value)),25,44,item.Key.Contains("mouth")?5000:1500);yield return Ready();yield return Capture(item.Key);}
                    app.DiagnosticsVisible=true;yield return new WaitForSecondsRealtime(.5f);yield return Capture("14-f3");app.DiagnosticsVisible=false;var ecology=GetComponent<EcologyPresentation>();ecology.Inspect=true;foreach(int layer in new[]{3,4,5}){ecology.MapLayer=layer;yield return new WaitForSecondsRealtime(.6f);yield return Capture("15-f4-"+layer);}ecology.Inspect=false;
                    app.Menu.Show(ApplicationPage.Pause);yield return Capture("16-pause");app.Menu.Show(ApplicationPage.Graphics);yield return Capture("17-graphics");app.Menu.Show(ApplicationPage.World);
                    var fog=app.Settings.Copy();fog.FogEnabled=false;app.ApplySettings(fog);yield return Capture("18-fog-off");fog.FogEnabled=true;app.ApplySettings(fog);yield return Capture("19-fog-on");
                    if(!smoke){yield return AdditionalViews();Focus(forest,600);yield return Ready();yield return Measure("rapid-zoom",8,false,true);yield return WeatherSequence(forest);app.Clock.SetSpeed(1);app.Clock.Start();yield return Measure("Medium-running-simulation",30);app.Clock.Pause();}
                }
                string ecologyHash=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture())),living=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));app.SaveWorld();yield return Operation();string id=app.Current.Id;app.ReturnToMenu();yield return new WaitForSecondsRealtime(.3f);app.LoadWorld(id);yield return Operation();Require(app.WorldActive,"reload "+size);app.Clock.Pause();app.Controller.ExternalDriver=true;
                bool same=ecologyHash==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture()))&&living==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));if(result.generations.Count==1)result.continuity=same;else result.continuity&=same;Require(same,"saved continuation "+size);yield return Capture("20-"+sizeName+"-reloaded");
                if(!smoke&&size==WorldSize.Colossal)
                {Focus(Best(i=>app.Ecology.Forest(i)),750);yield return Ready();yield return Measure("Colossal-moving",180,true);var high=app.Settings.Copy();high.TerrainDetail=3;high.TerrainDistance=10;high.DistantDetail=2;high.VegetationDistance=8000;high.VegetationDensity=1.5f;high.CloudQuality=2;high.WaterQuality=2;high.ShadowDistance=4000;app.ApplySettings(high);yield return Ready();yield return Capture("21-high-quality");yield return Measure("Colossal-high",15,true);}
            }
            app.ReturnToMenu();yield return new WaitForSecondsRealtime(.5f);result.cleanup=FindObjectsByType<WorldRenderer>(FindObjectsSortMode.None).Length==0&&FindObjectsByType<WorldCloudLayer>(FindObjectsSortMode.None).Length==0&&FindObjectsByType<RegionalTerrain>(FindObjectsSortMode.None).Length==0;
            if(!smoke){app.CreateWorld("Cancellation validation","cancel-007",WorldSize.Colossal,new GenerationSettings(),new ClimateSettings());yield return null;app.CancelPreparation();yield return Operation();result.cancellation=!app.WorldActive;Require(result.cancellation,"generation cancellation");}
            app.Menu.SelectedChapter=6;app.Menu.Show(ApplicationPage.Chronicles);yield return Capture("30-book-vii");app.Menu.Show(ApplicationPage.Changelog);yield return Capture("31-changelog");app.Menu.Show(ApplicationPage.Home);
            File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(result,true));Require(result.continuity&&result.cleanup,"continuity and cleanup");Debug.Log("AEVORIA007_AUDIT_PASS");Application.Quit();
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
            if(result.exploration){var position=app.Explorer.Position;app.Living.Traveler.Tool=ToolKind.Shovel;for(int z=-8;z<=8&&!result.excavation;z+=2)for(int x=-8;x<=8&&!result.excavation;x+=2){float east=position.x+x,north=position.z+z;result.excavation=app.Explorer.DigAt(new Vector3(east,geo.ElevationAt(east,north),north),ResourceKind.Soil);}if(result.excavation){var cut=app.Living.Excavations[0].Cuts[0];app.Controller.SetView(new GeoPoint(cut.X,cut.Z),25,55,10);yield return Measure("experimental-excavation-install",6);yield return Capture("29-solid-excavation");}else result.limitations.Add("No supported excavation point in the selected local patch");}
        }
        private IEnumerator WeatherSequence(int region)
        {
            var p=app.Ecology.EnvironmentAt(region).Center;var geo=app.Terrain.Geography;long start=app.Clock.State.Tick;long rain=-1,dry=-1;
            for(long tick=start;tick<start+1440;tick+=4)if(RegionalAtmosphere.Forecast(geo,tick,p).Precipitation>.5){rain=tick;break;}
            if(rain<0){result.limitations.Add("No sufficiently strong rain window at selected forest");yield break;}
            var settings=app.Settings.Copy();settings.CloudQuality=1;app.ApplySettings(settings);Focus(region,3000);result.wetBefore=app.Living.Atmosphere.Wetness(region);yield return Advance(Math.Max(start,rain-16));yield return new WaitForSecondsRealtime(2);yield return Capture("22-approaching-storm");yield return Advance(rain+16);yield return new WaitForSecondsRealtime(2);result.wetRain=app.Living.Atmosphere.Wetness(region);yield return Capture("22-rain-cloud-shadow-wetness");yield return Measure("regional-rain",10);
            for(long tick=app.Clock.State.Tick+4;tick<app.Clock.State.Tick+2880;tick+=4){bool clear=true;for(int d=0;d<=80;d+=4)if(RegionalAtmosphere.Forecast(geo,tick+d,p).Precipitation>.005){clear=false;break;}if(clear){dry=tick;break;}}
            if(dry>=0){yield return Advance(dry);result.wetDryStart=app.Living.Atmosphere.Wetness(region);yield return new WaitForSecondsRealtime(2);yield return Capture("23-drying-start");yield return Advance(dry+32);yield return new WaitForSecondsRealtime(2);result.wetDry=app.Living.Atmosphere.Wetness(region);yield return Capture("23-drying-ground");}
        }
        private IEnumerator Advance(long target){app.Clock.SetSpeed(1);while(app.Clock.State.Tick<target){app.Clock.Start();app.Clock.Update(.25,1);app.Clock.Pause();app.Ecology.AdvanceTo(app.Clock.State.Tick,2);app.Living.Atmosphere.AdvanceTo(app.Clock.State.Tick,2);app.Living.Seasons.AdvanceTo(app.Clock.State.Tick,2);yield return null;}}
        private int Find(Func<int,bool> predicate){for(int i=0;i<app.Ecology.Count;i++)if(predicate(i))return i;return -1;}
        private int Best(Func<int,double> value){int best=Find(i=>app.Ecology.EnvironmentAt(i).Land);for(int i=0;i<app.Ecology.Count;i++)if(app.Ecology.EnvironmentAt(i).Land&&value(i)>value(best))best=i;return best;}
        private void Focus(int region,float distance)=>app.Controller.SetView(app.Ecology.EnvironmentAt(region).Center,25,45,distance);
        private IEnumerator Operation(){float end=Time.realtimeSinceStartup+300;string stage=null;while(app.Busy&&Time.realtimeSinceStartup<end){if(generation!=null&&stage!=app.Progress){stage=app.Progress;generation.stages.Add(stage);}yield return null;}Require(!app.Busy,"operation timeout");}
        private IEnumerator Ready(){float end=Time.realtimeSinceStartup+150;yield return null;while(!app.Terrain.IsReady&&Time.realtimeSinceStartup<end)yield return null;Require(app.Terrain.IsReady,"streaming ready");yield return new WaitForSecondsRealtime(.6f);}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private void Require(bool value,string name){if(!value){File.WriteAllText(Path.Combine(output,"failure.txt"),name+": "+app.Message);Debug.LogError("Audit007 failed "+name);Application.Quit(9);throw new InvalidOperationException(name);}}
        private void Failure(string text,string stack,LogType type){if(type==LogType.Exception){File.WriteAllText(Path.Combine(output,"exception.txt"),text+"\n"+stack);Application.Quit(10);}}
        private IEnumerator Measure(string scenario,float seconds,bool move=false,bool zoom=false)
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;using var trace=new PerformanceTrace();trace.Reset();var values=new List<float>(100000);yield return null;yield return null;int start=rendered;var process=System.Diagnostics.Process.GetCurrentProcess();double cpu=process.TotalProcessorTime.TotalMilliseconds,gpu=0;int gpuFrames=0;var timing=new FrameTiming[1];float end=Time.realtimeSinceStartup+seconds;
            while(Time.realtimeSinceStartup<end){yield return null;if(move){app.Controller.MoveHorizontal(new Vector2(Mathf.Sin(Time.time*.2f),Mathf.Cos(Time.time*.2f))*1000*Time.unscaledDeltaTime);app.Controller.Advance(Time.unscaledDeltaTime);}if(zoom)app.Controller.SetView(new GeoPoint(app.Controller.Focus.x,app.Controller.Focus.z),25,55,Mathf.Exp(Mathf.Lerp(Mathf.Log(20),Mathf.Log((float)app.Terrain.Geography.Configuration.WidthMeters),(Mathf.Sin(Time.time*1.5f)+1)*.5f)));float ms=Time.unscaledDeltaTime*1000;values.Add(ms);trace.Record(ms,app.Terrain.LastUpdateMilliseconds,app.Terrain.OwnedMeshes);FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){gpu+=timing[0].gpuFrameTime;gpuFrames++;}}
            double cpuMs=(process.TotalProcessorTime.TotalMilliseconds-cpu)/values.Count,sum=0;values.Sort();foreach(float value in values)sum+=value;result.measurements.Add(new Measurement{scenario=scenario,frames=values.Count,rendered=rendered-start,width=Screen.width,height=Screen.height,meanMs=sum/values.Count,p95Ms=values[(int)(values.Count*.95)],p99Ms=values[(int)(values.Count*.99)],maxMs=values[values.Count-1],cpuMs=cpuMs,gpuMs=gpuFrames>0?gpu/gpuFrames:-1,managedMiB=GC.GetTotalMemory(false)/1048576.0,unityMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0});trace.Write(Path.Combine(output,scenario+"-frames.csv"));File.WriteAllText(Path.Combine(output,"progress.json"),JsonUtility.ToJson(result,true));
        }
        private void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;Application.logMessageReceived-=Failure;}
    }
}
