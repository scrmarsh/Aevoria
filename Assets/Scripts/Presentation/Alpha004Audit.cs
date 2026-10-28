using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Aevoria.Core;
using Aevoria.Simulation;
using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aevoria.Presentation
{
    // Explicit opt-in audit. UI captures use actual menu states and real saves
    // inside the caller-supplied isolated root. No physical-input claim.
    public sealed class Alpha004Audit : MonoBehaviour
    {
        [Serializable] public sealed class Measurement {public string scenario;public int frames,rendered,width,height,terrainServices,waterServices,environmentServices;public double averageMs,p95Ms,maxMs,managedMiB,workingMiB,trackedUnityMiB,operationMs,gpuMs,cpuProcessMs;public int collections;}
        [Serializable] public sealed class Result {public string application=ApplicationVersion.Number,gpu,cpu,root;public double createMs,saveMs,loadMs,calibrationMs,hydrologyMs,climateMs;public long restoredTick;public List<Measurement> measurements=new List<Measurement>();}
        private AevoriaBootstrap app;private string output;private int rendered;private Result result;
        private void Rendered(ScriptableRenderContext context,Camera camera){if(camera.CompareTag("MainCamera")) rendered++;}
        private IEnumerator Start()
        {
            app=GetComponent<AevoriaBootstrap>();
            if(AevoriaBootstrap.Argument("-aevoriaTestRoot")==null) {Debug.LogError("Alpha004 audit requires -aevoriaTestRoot; refusing to write normal saves.");Application.Quit(4);yield break;}
            output=AevoriaBootstrap.Argument("-aevoriaOutput",Path.Combine(Application.persistentDataPath,"Audit004"));Directory.CreateDirectory(output);
            result=new Result {gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,root=app.Repository.Root};
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;RenderPipelineManager.endCameraRendering+=Rendered;
            yield return new WaitForSecondsRealtime(1);
            app.Menu.Show(ApplicationPage.Home);yield return Capture("00-menu-empty");
            foreach(var page in new[]{ApplicationPage.Create,ApplicationPage.Load,ApplicationPage.Settings,ApplicationPage.Changelog,ApplicationPage.Chronicles,ApplicationPage.About})
            {app.Menu.Show(page);yield return Capture("01-"+page);}
            app.Menu.Show(ApplicationPage.Home);yield return Measure("menu-idle",3);
            app.CreateWorld("Validation world · Order","aevoria-001",WorldSize.Medium,new GenerationSettings(),new ClimateSettings());
            yield return WaitOperation();result.createMs=app.LastOperationMilliseconds;
            if(!app.WorldActive) {Debug.LogError("Audit create failed: "+app.Message);Application.Quit(5);yield break;}
            result.calibrationMs=app.Terrain.Geography.CalibrationMilliseconds;result.hydrologyMs=app.Terrain.Geography.HydrologyMilliseconds;result.climateMs=app.Terrain.Geography.ClimateMilliseconds;
            yield return WaitReady();app.Clock.Pause();app.Controller.ExternalDriver=true;
            app.Controller.SetView(new GeoPoint(0,0),15,70,22000);yield return new WaitForSecondsRealtime(1);yield return Measure("world-overview",4);yield return Capture("08-overview");
            var hydro=app.Terrain.Geography.Hydrology;int river=-1,lake=-1;
            for(int i=0;i<hydro.Ground.Length;i++){if(hydro.IsRiver(i)&&(river<0||hydro.Accumulation[i]>hydro.Accumulation[river])) river=i;if(hydro.Lake[i]>=0&&(lake<0||hydro.Water[i]-hydro.Ground[i]>hydro.Water[lake]-hydro.Ground[lake])) lake=i;}
            if(river>=0){app.Controller.SetView(new GeoPoint(hydro.X(river),hydro.Y(river)),35,40,1000);yield return WaitReady();yield return Measure("river",4);yield return Capture("09-river");}
            if(lake>=0){app.Controller.SetView(new GeoPoint(hydro.X(lake),hydro.Y(lake)),25,48,1600);yield return WaitReady();yield return Measure("lake",4);yield return Capture("10-lake");}
            app.Controller.SetView(new GeoPoint(0,0),25,45,8000);yield return WaitReady();yield return Measure("biomes",4);yield return Capture("11-biomes");
            var environment=GetComponent<WorldEnvironment>();environment.PreviewWeather=WeatherKind.Rain;yield return new WaitForSecondsRealtime(1);yield return Measure("rain",4);yield return Capture("12-rain");
            environment.PreviewSeason=3;environment.PreviewWeather=WeatherKind.Snow;yield return new WaitForSecondsRealtime(1);yield return Measure("winter",4);yield return Capture("13-winter");environment.PreviewWeather=null;environment.PreviewSeason=null;
            app.Controller.SetView(new GeoPoint(app.Terrain.Geography.Configuration.WidthMeters*0.42,0),25,42,11000);yield return WaitReady();yield return Capture("14-perimeter");
            app.Clock.SetSpeed(2);app.Clock.Start();app.Clock.Update(7.125);app.Clock.Pause();long tick=app.Clock.State.Tick;
            app.Menu.Show(ApplicationPage.Pause);app.SaveWorld();yield return WaitOperation();result.saveMs=app.LastOperationMilliseconds;
            string id=app.Current.Id;app.ReturnToMenu();yield return new WaitForSecondsRealtime(0.5f);yield return Capture("15-menu-recents");
            app.Menu.Show(ApplicationPage.Load);yield return Capture("16-load-real-save");app.LoadWorld(id);yield return WaitOperation();result.loadMs=app.LastOperationMilliseconds;
            if(!app.WorldActive||app.Clock.State.Tick!=tick) {Debug.LogError("Audit save/load continuity failed");Application.Quit(6);yield break;}
            result.restoredTick=app.Clock.State.Tick;yield return WaitReady();app.Controller.ExternalDriver=true;
            yield return Measure("reloaded-world",4);
            yield return Measure("moving-world",10,true);
            yield return Measure("short-soak",60,true);
            app.SaveWorld(returnToMenu:true);yield return WaitOperation();
            app.CreateWorld("Arid highlands · validation","climate-contrast",WorldSize.Small,new GenerationSettings(mountains:3200),new ClimateSettings(temperatureOffset:12,rainfall:0.22f,latitude:12));
            yield return WaitOperation();yield return WaitReady();app.Clock.Pause();app.Controller.ExternalDriver=true;
            var tall=app.Terrain.Geography;int highest=0;for(int i=0;i<tall.Hydrology.Ground.Length;i++) if(tall.Hydrology.Ground[i]>tall.Hydrology.Ground[highest]) highest=i;
            app.Controller.SetView(new GeoPoint(tall.Hydrology.X(highest),tall.Hydrology.Y(highest)),35,34,1800);yield return WaitReady();yield return Measure("mountain-3200",4);yield return Capture("17-mountain-3200");
            app.Controller.SetView(new GeoPoint(0,0),20,65,13000);yield return WaitReady();yield return Capture("18-arid-biomes");
            app.ReturnToMenu();yield return new WaitForSecondsRealtime(1);yield return Measure("menu-after-world",3);
            File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(result,true));Debug.Log("AEVORIA004_AUDIT_PASS");Application.Quit();
        }
        private IEnumerator WaitOperation(){float end=Time.realtimeSinceStartup+120;while(app.Busy&&Time.realtimeSinceStartup<end) yield return null;if(app.Busy) {Debug.LogError("Audit operation timed out");Application.Quit(7);enabled=false;}}
        private IEnumerator WaitReady(){float end=Time.realtimeSinceStartup+120;while(app.WorldActive&&!app.Terrain.IsReady&&Time.realtimeSinceStartup<end) yield return null;yield return new WaitForSecondsRealtime(0.5f);}
        private IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(0.35f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private IEnumerator Measure(string scenario,float seconds,bool moving=false)
        {
            var frames=new List<float>(3000);int startRendered=rendered,gc=GC.CollectionCount(0);float end=Time.realtimeSinceStartup+seconds;
            var timing=new FrameTiming[1];double gpu=0;int gpuCount=0;var process=System.Diagnostics.Process.GetCurrentProcess();double cpuStart=process.TotalProcessorTime.TotalMilliseconds;
            using var trace=new PerformanceTrace();trace.Reset();
            // Recorder/list setup and the preceding CSV write belong to the
            // harness transition. Keep the earlier raw run, but do not time
            // profiler construction as if it were world travel.
            yield return null;yield return null;
            startRendered=rendered;gc=GC.CollectionCount(0);end=Time.realtimeSinceStartup+seconds;cpuStart=process.TotalProcessorTime.TotalMilliseconds;
            while(Time.realtimeSinceStartup<end)
            {
                yield return null;
                if(moving){float angle=(Time.realtimeSinceStartup-end)*0.2f;app.Controller.MoveHorizontal(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*550*Time.unscaledDeltaTime);app.Controller.Advance(Time.unscaledDeltaTime);}
                frames.Add(Time.unscaledDeltaTime*1000);trace.Record(Time.unscaledDeltaTime*1000,app.Terrain?.LastUpdateMilliseconds??0,app.Terrain?.OwnedMeshes??0);
                FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){gpu+=timing[0].gpuFrameTime;gpuCount++;}
            }
            frames.Sort();double total=0;foreach(float f in frames) total+=f;
            result.measurements.Add(new Measurement {scenario=scenario,frames=frames.Count,rendered=rendered-startRendered,width=Screen.width,height=Screen.height,averageMs=total/Math.Max(1,frames.Count),p95Ms=frames[(int)(frames.Count*0.95)],maxMs=frames[frames.Count-1],managedMiB=GC.GetTotalMemory(false)/1048576.0,workingMiB=process.WorkingSet64>0?process.WorkingSet64/1048576.0:-1,trackedUnityMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0,collections=GC.CollectionCount(0)-gc,operationMs=app.LastOperationMilliseconds,gpuMs=gpuCount>0?gpu/gpuCount:-1,cpuProcessMs=(process.TotalProcessorTime.TotalMilliseconds-cpuStart)/Math.Max(1,frames.Count),terrainServices=FindObjectsByType<WorldRenderer>(FindObjectsSortMode.None).Length,waterServices=FindObjectsByType<WorldWaterFeatures>(FindObjectsSortMode.None).Length,environmentServices=FindObjectsByType<WorldEnvironment>(FindObjectsSortMode.None).Length});
            trace.Write(Path.Combine(output,scenario+"-frames.csv"));
        }
        private void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;}
    }
}

