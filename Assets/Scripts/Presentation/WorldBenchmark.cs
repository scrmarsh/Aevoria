using System;
using System.Collections.Generic;
using System.IO;
using Aevoria.World;
using UnityEngine;

namespace Aevoria.Presentation
{
    // Opt-in standalone probe; warm-up is excluded. No results are inferred from HUD FPS.
    public sealed class WorldBenchmark : MonoBehaviour
    {
        private readonly List<float> samples = new List<float>(4096);
        private AevoriaBootstrap app;
        private float started, readyAt;
        private int gcStart, stage;
        private bool transitioning;
        private double tickTotal, tickMax;
        private string output;
        private UnityEngine.FrameTiming[] timings = new UnityEngine.FrameTiming[1];
        private double gpuTotal;
        private System.Diagnostics.Process process;
        private double cpuStart;
        private int gpuSamples;
        private int renderedFrames, renderedAtSampleStart;
        private void OnCameraRendered(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (camera.CompareTag("MainCamera")) renderedFrames++;
        }
        private void Start()
        {
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += OnCameraRendered;
            process = System.Diagnostics.Process.GetCurrentProcess();
            app = GetComponent<AevoriaBootstrap>(); started = Time.realtimeSinceStartup;
            output = Path.Combine(Application.persistentDataPath,"Phase1Benchmark");
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args,"-aevoriaOutput"); if(index>=0 && index+1<args.Length) output=args[index+1];
            Directory.CreateDirectory(output);
            Application.runInBackground=true;
            QualitySettings.vSyncCount=0; Application.targetFrameRate=-1;
            app.Clock.Start();
        }
        private void Update()
        {
            if(transitioning) return;
            if(Time.realtimeSinceStartup-started>180) { Debug.LogError("Benchmark generation timeout"); Application.Quit(2); return; }
            if(!app.Terrain.IsReady) return;
            if(readyAt==0) { readyAt=Time.realtimeSinceStartup; gcStart=GC.CollectionCount(0); }
            float elapsed=Time.realtimeSinceStartup-readyAt;
            if(elapsed<3) return;
            if(samples.Count==0) { cpuStart=process.TotalProcessorTime.TotalMilliseconds; gcStart=GC.CollectionCount(0); renderedAtSampleStart=renderedFrames; }
            samples.Add(Time.unscaledDeltaTime*1000);
            tickTotal+=app.Clock.LastTickMilliseconds; tickMax=Math.Max(tickMax,app.Clock.LastTickMilliseconds);
            FrameTimingManager.CaptureFrameTimings();
            if(FrameTimingManager.GetLatestTimings(1,timings)>0 && timings[0].gpuFrameTime>0) { gpuTotal+=timings[0].gpuFrameTime; gpuSamples++; }
            if(elapsed<13) return;
            if(renderedFrames<=renderedAtSampleStart)
            {
                Debug.LogError("Benchmark rejected: no camera frames rendered. Keep the player window visible and unminimized.");
                enabled=false; Application.Quit(3); return;
            }
            samples.Sort(); double total=0; foreach(float ms in samples) total+=ms;
            double cpuPercent=(process.TotalProcessorTime.TotalMilliseconds-cpuStart)/total/System.Environment.ProcessorCount*100;
            string report=$"cpuPercentNormalized={cpuPercent:F2}\nseed={app.Terrain.Geography.Configuration.Seed}\nresolution={Screen.width}x{Screen.height}\nframes={samples.Count}\navgFrameMs={total/samples.Count:F3}\navgFPS={1000*samples.Count/total:F2}\np50Ms={Percentile(0.5f):F3}\np95Ms={Percentile(0.95f):F3}\np99Ms={Percentile(0.99f):F3}\nmaxMs={samples[samples.Count-1]:F3}\nchunks={app.Terrain.ActiveChunks}\ntriangles={app.Terrain.Triangles}\nmeshCpuMs={app.Terrain.MeshMilliseconds:F2}\ninitialViewReadyMs={app.Terrain.ReadyMilliseconds:F2}\nmanagedBytes={GC.GetTotalMemory(false)}\ngen0Collections={GC.CollectionCount(0)-gcStart}\nlastTickAverageMs={tickTotal/samples.Count:F4}\nlastTickMaxMs={tickMax:F4}\ngpuMs={(gpuSamples>0?(gpuTotal/gpuSamples).ToString("F3"):"unavailable")}\nCPU={SystemInfo.processorType}\nGPU={SystemInfo.graphicsDeviceName}\nUnity={Application.unityVersion}\n";
            File.WriteAllText(Path.Combine(output,$"stage{stage}.txt"),report); Debug.Log("PHASE1_BENCHMARK\n"+report);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,$"stage{stage}.png"));
            stage++;
            if(stage>=3) { enabled=false; Invoke(nameof(Finish),2); return; }
            transitioning=true; Invoke(nameof(Advance),0.25f);
        }
        private void Advance()
        {
            // Allow screenshot capture to finish before releasing meshes.
            app.Regenerate(stage==1?"aevoria-002":"archipelago-37",stage==1?WorldSize.Small:WorldSize.Medium,new GenerationSettings());
            var camera=FindFirstObjectByType<MapCamera>();
            camera.SetView(new GeoPoint(0,0),stage==1?35:0,stage==1?40:65,stage==1?350:1800);
            transitioning=false; samples.Clear(); readyAt=0; tickTotal=tickMax=gpuTotal=0; gpuSamples=0;
        }
        private float Percentile(float p) => samples[Mathf.Clamp((int)(samples.Count*p),0,samples.Count-1)];
        private void OnDestroy()
        {
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= OnCameraRendered;
            process?.Dispose();
        }
        private void Finish() { Application.Quit(); }
    }
}
