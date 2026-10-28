using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Aevoria.UI;
using Aevoria.Core;
using Aevoria.World;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aevoria.Presentation
{
    // Opt-in scripted camera path. Motion samples include streaming, uploads and LOD changes.
    public sealed class RefinementBenchmark : MonoBehaviour
    {
        private readonly string[] names={"overview","mountains","grassland","coast","water","horizon","forest","continuous","rapid-zoom","large-overview","large-streaming","blur-off","blur-on","low-angle"};
        private readonly List<float> frames=new List<float>(16000);
        private readonly FrameTiming[] timing=new FrameTiming[1];
        private AevoriaBootstrap app;
        private MapCamera camera;
        private System.Diagnostics.Process process;
        private ProfilerRecorder mainThread;
        private PerformanceTrace trace;
        private string output;
        private GeoPoint mountain,plain,coast,forest;
        private float coastYaw,stageStart,readyAt,sampleStart,coverage;
        private int stage,gcStart,rendered,renderedStart,peakChunks,peakTrees,gpuCount;
        private bool transitioning;
        private double cpuStart,gpuSum,mainSum,streamMax;
        private long memoryPeak;
        private long peakTerrainTriangles,peakVegetationTriangles;
        private void Start()
        {
            app=GetComponent<AevoriaBootstrap>(); camera=app.Controller; camera.ExternalDriver=true;
            GetComponent<DeveloperHud>().ControlsEnabled=false;
            output=Path.Combine(Application.persistentDataPath,ApplicationVersion.Stage+"-v"+ApplicationVersion.Number+"-Benchmark");
            var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-aevoriaOutput"); if(i>=0 && i+1<args.Length) output=args[i+1];
            Directory.CreateDirectory(output); process=System.Diagnostics.Process.GetCurrentProcess();
            try {mainThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);} catch(Exception e) {Debug.LogWarning("Main thread recorder unavailable: "+e.Message);}
            RenderPipelineManager.endCameraRendering+=Rendered;
            if(Array.IndexOf(args,"-aevoriaTrace")>=0) trace=new PerformanceTrace();
            Application.runInBackground=true; QualitySettings.vSyncCount=0; Application.targetFrameRate=-1; app.Clock.Start();
            FindLocations(); BeginStage();
        }
        private void Rendered(ScriptableRenderContext context,Camera view) {if(view.CompareTag("MainCamera")) rendered++;}
        private void FindLocations()
        {
            var geo=app.Terrain.Geography; var config=geo.Configuration;
            float highest=float.MinValue,shore=float.MaxValue,plainScore=float.MaxValue,forestScore=float.MaxValue;
            for(int z=0;z<65;z++) for(int x=0;x<65;x++)
            {
                double east=(x+0.5)/65*config.WidthMeters-config.WidthMeters/2,north=(z+0.5)/65*config.DepthMeters-config.DepthMeters/2;
                float h=geo.ElevationAt(east,north);
                var p=new GeoPoint(east,north);
                if(h>highest) {highest=h;mountain=p;}
                if(Math.Abs(h-3)<shore) {shore=Math.Abs(h-3);coast=p;}
                if(h>15 && h<140)
                {
                    float dx=geo.ElevationAt(east+16,north)-geo.ElevationAt(east-16,north);
                    float dz=geo.ElevationAt(east,north+16)-geo.ElevationAt(east,north-16);
                    float score=Math.Abs(dx)+Math.Abs(dz);
                    if(score<plainScore) {plainScore=score;plain=p;}
                    double forestField=geo.FieldNoise(east,north,1600,310)*0.65+geo.FieldNoise(east,north,370,311)*0.35;
                    if(forestField>0.18 && VegetationDistribution.TryTree(geo,(int)Math.Floor(east/VegetationDistribution.CellMeters),(int)Math.Floor(north/VegetationDistribution.CellMeters),out var site) && score<forestScore)
                    {forestScore=score; forest=new GeoPoint(site.East,site.North);}
                }
            }
            float gx=geo.ElevationAt(coast.X+150,coast.Y)-geo.ElevationAt(coast.X-150,coast.Y);
            float gz=geo.ElevationAt(coast.X,coast.Y+150)-geo.ElevationAt(coast.X,coast.Y-150);
            coastYaw=Mathf.Atan2(gx,gz)*Mathf.Rad2Deg;
            coverage=geo.MeasureLandFraction();
        }
        private void BeginStage()
        {
            if(stage==9) {app.Regenerate("large-11",WorldSize.Large,new GenerationSettings());FindLocations();}
            app.Atmosphere.MotionBlurEnabled=stage==12;
            camera.TerrainFollowing=camera.TerrainCollision=false;
            switch(stage)
            {
                case 0: camera.SetView(new GeoPoint(0,0),15,70,20000);break;
                case 1: camera.SetView(mountain,35,34,1400);break;
                case 2: camera.SetView(plain,25,28,230);break;
                case 3: camera.SetView(coast,coastYaw,42,1600);break;
                case 4: camera.SetView(coast,coastYaw,27,520);break;
                case 5: camera.SetView(coast,coastYaw+140,20,3000);break;
                case 6: camera.SetView(forest,25,23,150);break;
                case 9: camera.SetView(new GeoPoint(0,0),30,65,30000);break;
                case 10:
                    float limit=(float)app.Terrain.Geography.Configuration.WidthMeters/2-6000;
                    camera.SetView(new GeoPoint(Mathf.Clamp((float)forest.X,-limit,limit),Mathf.Clamp((float)forest.Y,-limit,limit)),20,38,650);
                    break;
                case 13: camera.SetView(coast,coastYaw,20,110);break;
                default: camera.SetView(forest,20,38,650);break;
            }
            stageStart=Time.realtimeSinceStartup; readyAt=sampleStart=0; frames.Clear();
            trace?.Reset();
            gpuSum=mainSum=streamMax=0; gpuCount=peakChunks=peakTrees=0; memoryPeak=peakTerrainTriangles=peakVegetationTriangles=0; transitioning=false;
        }
        private bool IsMotion=>stage==7 || stage==8 || stage==10 || stage==11 || stage==12;
        private void Update()
        {
            if(transitioning) return;
            float now=Time.realtimeSinceStartup;
            if(now-stageStart>120) {Debug.LogError("World benchmark readiness timeout: "+names[stage]);Application.Quit(2);enabled=false;return;}
            if(readyAt==0) {if(!app.Terrain.IsReady) return;readyAt=now;}
            float elapsed=now-readyAt;
            if(IsMotion)
            {
                float speed=stage==10?1600:stage==8?350:550;
                // The fast Large path closes a 2.5 km-radius loop instead of
                // drifting out of authority during its 18 second interval.
                float angle=elapsed*(stage==10?0.64f:0.11f);
                camera.MoveHorizontal(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*speed*Time.unscaledDeltaTime);
                float zoom=stage==8?Mathf.Exp(Mathf.Lerp(Mathf.Log(35),Mathf.Log(15000),(Mathf.Sin(elapsed*1.4f)+1)/2)):650;
                camera.SetOrbitTargets(20+elapsed*5,stage==8?48:38,zoom); camera.Advance(Time.unscaledDeltaTime);
            }
            if(elapsed<2) return;
            if(sampleStart==0) {sampleStart=now;cpuStart=process.TotalProcessorTime.TotalMilliseconds;gcStart=GC.CollectionCount(0);renderedStart=rendered;trace?.Reset();}
            frames.Add(Time.unscaledDeltaTime*1000); FrameTimingManager.CaptureFrameTimings();
            trace?.Record(Time.unscaledDeltaTime*1000,app.Terrain.LastUpdateMilliseconds,app.Terrain.OwnedMeshes);
            if(FrameTimingManager.GetLatestTimings(1,timing)>0 && timing[0].gpuFrameTime>0) {gpuSum+=timing[0].gpuFrameTime;gpuCount++;}
            if(mainThread.Valid) mainSum+=mainThread.LastValue/1000000.0;
            peakChunks=Math.Max(peakChunks,app.Terrain.ActiveChunks); peakTrees=Math.Max(peakTrees,app.Terrain.Vegetation.VisibleTrees);
            peakTerrainTriangles=Math.Max(peakTerrainTriangles,app.Terrain.ResidentTriangles);peakVegetationTriangles=Math.Max(peakVegetationTriangles,app.Terrain.Vegetation.VisibleTriangles);
            streamMax=Math.Max(streamMax,app.Terrain.LastUpdateMilliseconds); memoryPeak=Math.Max(memoryPeak,UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
            float duration=stage==8 || stage==10?16:IsMotion?12:8;
            if(now-sampleStart<duration) return;
            if(rendered<=renderedStart) {Debug.LogError("World benchmark rejected: no rendered camera frames");Application.Quit(3);enabled=false;return;}
            frames.Sort(); double total=0; foreach(float ms in frames) total+=ms;
            double cpu=(process.TotalProcessorTime.TotalMilliseconds-cpuStart)/total/Environment.ProcessorCount*100;
            var sb=new StringBuilder();
            void Metric(string key,object value) {sb.Append(key).Append('=').Append(Convert.ToString(value,CultureInfo.InvariantCulture)).Append('\n');}
            Metric("scenario",names[stage]);Metric("generatorVersion",ProceduralGeography.GeneratorVersion);Metric("seed",app.Terrain.Geography.Configuration.Seed);
            Metric("applicationVersion",ApplicationVersion.Number);Metric("developmentStatus",ApplicationVersion.Status);Metric("displayName",ApplicationVersion.DisplayName);
            Metric("bundleVersion",Application.version);
            Metric("worldMeters",app.Terrain.Geography.Configuration.WidthMeters);Metric("landFraction",coverage);Metric("resolution",Screen.width+"x"+Screen.height);
            Metric("frames",frames.Count);Metric("renderedFrames",rendered-renderedStart);Metric("avgFPS",1000*frames.Count/total);Metric("avgFrameMs",total/frames.Count);
            Metric("p50Ms",frames[frames.Count/2]);Metric("p95Ms",frames[(int)(frames.Count*.95)]);Metric("p99Ms",frames[(int)(frames.Count*.99)]);Metric("maxMs",frames[frames.Count-1]);
            Metric("mainThreadMarkerMeanMs",mainThread.Valid?(mainSum/frames.Count).ToString("F3",CultureInfo.InvariantCulture):"unavailable");
            Metric("gpuMeanMs",gpuCount>0?(gpuSum/gpuCount).ToString("F3",CultureInfo.InvariantCulture):"unavailable");Metric("cpuPercentNormalized",cpu);
            Metric("peakChunks",peakChunks);Metric("terrainTriangles",app.Terrain.Triangles);Metric("overviewTriangles",WorldRenderer.OverviewTriangleCount);Metric("peakVisibleTrees",peakTrees);
            Metric("peakResidentTerrainTriangles",peakTerrainTriangles);Metric("peakInstancedVegetationTriangles",peakVegetationTriangles);
            Metric("residentTerrainIncludesOverviewAndFades",true);
            Metric("meshCpuMs",app.Terrain.MeshMilliseconds);Metric("viewReadyMs",app.Terrain.ReadyMilliseconds);Metric("maxStreamingUpdateMs",streamMax);
            Metric("unityAllocatedPeakBytes",memoryPeak);Metric("managedBytes",GC.GetTotalMemory(false));Metric("gen0Collections",GC.CollectionCount(0)-gcStart);
            Metric("lastSimulationTickMs",app.Clock.LastTickMilliseconds);Metric("motionBlur",app.Atmosphere.MotionBlurEnabled);
            Metric("CPU",SystemInfo.processorType);Metric("GPU",SystemInfo.graphicsDeviceName);Metric("Unity",Application.unityVersion);
            string stem=stage.ToString("00")+"-"+names[stage];File.WriteAllText(Path.Combine(output,stem+".txt"),sb.ToString());
            trace?.Write(Path.Combine(output,stem+"-frames.csv"));
            ScreenCapture.CaptureScreenshot(Path.Combine(output,stem+".png"));Debug.Log(ApplicationVersion.DisplayName+" BENCHMARK\n"+sb);
            transitioning=true;Invoke(nameof(Next),0.3f);
        }
        private void Next() {stage++;if(stage>=names.Length) {Application.Quit();enabled=false;}else BeginStage();}
        private void OnDestroy() {RenderPipelineManager.endCameraRendering-=Rendered;if(mainThread.Valid) mainThread.Dispose();trace?.Dispose();process?.Dispose();}
    }
}
