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
    public sealed class Alpha005Audit : MonoBehaviour
    {
        [Serializable] public sealed class Measurement
        {public string scenario;public int frames,rendered,width,height,animals,regions,collections;public long tick,steps;public double meanMs,p95Ms,p99Ms,maxMs,gpuMs,cpuMs,ecologyMs,managedMiB,unityMiB,elapsedSeconds,yearsPerSecond;public double deer,browsers,hares,wolves;}
        [Serializable] public sealed class Results
        {public string version=ApplicationVersion.Number,cpu,gpu;public double createMs,saveMs,loadMs;public bool saveEquivalent,continuationEquivalent,cleanup;public List<Measurement> measurements=new List<Measurement>();}
        private AevoriaBootstrap app;private string output;private int rendered;private Results results;
        private void Rendered(ScriptableRenderContext context,Camera camera){if(camera.CompareTag("MainCamera"))rendered++;}
        private IEnumerator Start()
        {
            app=GetComponent<AevoriaBootstrap>();
            if(AevoriaBootstrap.Argument("-aevoriaTestRoot")==null){Debug.LogError("Audit005 requires isolated save root");Application.Quit(4);yield break;}
            output=AevoriaBootstrap.Argument("-aevoriaOutput",Path.Combine(Application.persistentDataPath,"Audit005"));Directory.CreateDirectory(output);
            results=new Results {cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName};Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            RenderPipelineManager.endCameraRendering+=Rendered;
            yield return new WaitForSecondsRealtime(1);yield return Measure("menu",3);yield return Capture("00-main-menu");
            app.Menu.Show(ApplicationPage.Changelog);yield return Capture("01-changelog");app.Menu.Show(ApplicationPage.Chronicles);yield return Capture("02-book-v");
            app.CreateWorld("Living ecosystems · validation","ecosystem-005",WorldSize.Small,new GenerationSettings(),new ClimateSettings());yield return Operation();
            if(!app.WorldActive){Debug.LogError("Create failed: "+app.Message);Application.Quit(5);yield break;}results.createMs=app.LastOperationMilliseconds;app.Clock.Pause();app.Controller.ExternalDriver=true;
            var eco=app.Ecology;int grass=Find(i=>eco.EnvironmentAt(i).Land&&eco.Forest(i)<.05&&eco.Biomass(i)>.15);
            int forest=Find(i=>eco.EnvironmentAt(i).Land&&eco.Forest(i)>.11);
            int wildlife=FindMax(i=>eco.Population(i,0)+eco.Population(i,1)+eco.Population(i,2));
            int predators=FindMax(i=>eco.Population(i,3));
            GetComponent<EcologyPresentation>().Inspect=true;
            foreach(var pair in new[]{new KeyValuePair<string,int>("grassland",grass),new KeyValuePair<string,int>("forest",forest),new KeyValuePair<string,int>("wildlife-heavy",wildlife),new KeyValuePair<string,int>("predator-prey",predators)})
            {Focus(pair.Value,270);yield return Ready();
                if(pair.Key=="wildlife-heavy"||pair.Key=="predator-prey")
                {var visual=GetComponent<EcologyPresentation>();int species=pair.Key=="predator-prey"?3:1;if(visual.TryAnimalPosition(species,out var point)){app.Controller.SetView(point,30,28,35);yield return Ready();}}
                yield return Measure(pair.Key,5);yield return Capture(pair.Key);}
            int dry=Find(i=>eco.EnvironmentAt(i).Land&&eco.Moisture(i)<.6&&eco.Biomass(i)>.15);
            eco.StartEvent(dry,DisturbanceKind.Drought,.9,6);app.Clock.Start();app.Clock.Update(30,120);app.Clock.Pause();eco.AdvanceTo(app.Clock.State.Tick,12);
            Focus(dry,600);yield return Ready();yield return Measure("drought",5);yield return Capture("drought");
            int fire=Find(i=>eco.EnvironmentAt(i).Land&&eco.ActiveEvent(i)==DisturbanceKind.None&&eco.Moisture(i)<.6&&eco.Biomass(i)>.15);
            if(!eco.StartEvent(fire,DisturbanceKind.Fire,.95,2)){Debug.LogError("Fire probe preconditions unavailable");Application.Quit(8);yield break;}
            app.Clock.Start();app.Clock.Update(30,120);app.Clock.Pause();eco.AdvanceTo(app.Clock.State.Tick,12);Focus(fire,650);yield return Ready();yield return Measure("fire",5);yield return Capture("fire");
            int flood=Find(i=>eco.EnvironmentAt(i).Land&&eco.EnvironmentAt(i).Water>.2&&eco.ActiveEvent(i)==DisturbanceKind.None);
            if(!eco.StartEvent(flood,DisturbanceKind.Flood,.8,3)){Debug.LogError("Flood probe preconditions unavailable");Application.Quit(8);yield break;}
            app.Clock.Start();app.Clock.Update(30,120);app.Clock.Pause();eco.AdvanceTo(app.Clock.State.Tick,12);
            Focus(flood,700);yield return Ready();yield return Measure("flood",5);yield return Capture("flood");
            Focus(fire,650);yield return Ready();yield return Capture("fire-scar-after-event");
            app.Controller.SetView(new GeoPoint(0,0),20,70,22000);yield return Ready();yield return Measure("overview",5);yield return Capture("overview");
            Focus(forest,1000);yield return Ready();yield return Measure("rapid-travel",10,true);yield return Measure("rapid-zoom",8,false,true);
            app.Clock.Start();yield return Measure("accelerated",10,false,false,true);app.Clock.Pause();
            string saved=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture()));long tick=app.Clock.State.Tick;
            app.SaveWorld();yield return Operation();results.saveMs=app.LastOperationMilliseconds;string id=app.Current.Id;
            var expected=new EcologyModel(app.Terrain.Geography,app.Ecology.Capture());expected.AdvanceTo(tick+14400,120);
            app.ReturnToMenu();yield return new WaitForSecondsRealtime(.5f);yield return Capture("recents");app.LoadWorld(id);yield return Operation();results.loadMs=app.LastOperationMilliseconds;
            results.saveEquivalent=app.WorldActive&&saved==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture()))&&tick==app.Clock.State.Tick;
            var continued=new EcologyModel(app.Terrain.Geography,app.Ecology.Capture());continued.AdvanceTo(tick+14400,120);results.continuationEquivalent=ArchiveCodec.Hash(ArchiveCodec.Encode(expected.Capture()))==ArchiveCodec.Hash(ArchiveCodec.Encode(continued.Capture()));
            app.Clock.Pause();app.Controller.ExternalDriver=true;yield return Measure("moving-soak",60,true);
            app.ReturnToMenu();yield return new WaitForSecondsRealtime(.5f);
            app.CreateWorld("Large ecosystems · validation","aevoria-001",WorldSize.Large,new GenerationSettings(),new ClimateSettings());yield return Operation();app.Clock.Pause();app.Controller.ExternalDriver=true;yield return Ready();yield return Measure("large-world",8,true);yield return Capture("large-world");
            app.Clock.Start();yield return Measure("large-accelerated",8,false,false,true);app.Clock.Pause();
            app.ReturnToMenu();yield return new WaitForSecondsRealtime(1);yield return Measure("menu-after-unload",3);
            results.cleanup=FindObjectsByType<EcologyPresentation>(FindObjectsSortMode.None).Length==0&&FindObjectsByType<WorldRenderer>(FindObjectsSortMode.None).Length==0&&app.Ecology==null;
            File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(results,true));
            if(!results.saveEquivalent||!results.continuationEquivalent||!results.cleanup){Debug.LogError("Audit005 continuity or cleanup failed");Application.Quit(9);yield break;}
            Debug.Log("AEVORIA005_AUDIT_PASS");Application.Quit();
        }
        private int Find(Func<int,bool> predicate){for(int i=0;i<app.Ecology.Count;i++)if(predicate(i))return i;throw new InvalidOperationException("Ecology audit habitat not found");}
        private int FindMax(Func<int,double> value){int best=0;for(int i=1;i<app.Ecology.Count;i++)if(value(i)>value(best))best=i;return best;}
        private void Focus(int region,float distance){app.Controller.SetView(app.Ecology.EnvironmentAt(region).Center,25,40,distance);}
        private IEnumerator Operation(){float deadline=Time.realtimeSinceStartup+150;while(app.Busy&&Time.realtimeSinceStartup<deadline)yield return null;if(app.Busy)throw new TimeoutException("Audit operation timed out");}
        private IEnumerator Ready(){float deadline=Time.realtimeSinceStartup+120;while(!app.Terrain.IsReady&&Time.realtimeSinceStartup<deadline)yield return null;yield return new WaitForSecondsRealtime(1);}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private IEnumerator Measure(string scenario,float seconds,bool moving=false,bool zoom=false,bool accelerated=false)
        {
            var values=new List<float>(12000);using var trace=new PerformanceTrace();trace.Reset();yield return null;yield return null;
            int startRendered=rendered,gc=GC.CollectionCount(0);float end=Time.realtimeSinceStartup+seconds;
            long initialTick=app.Clock.State.Tick;float began=Time.realtimeSinceStartup;
            var timing=new FrameTiming[1];double gpu=0;int gpuCount=0;double ecologyMax=0;
            var process=System.Diagnostics.Process.GetCurrentProcess();double cpuStart=process.TotalProcessorTime.TotalMilliseconds;
            while(Time.realtimeSinceStartup<end)
            {
                yield return null;
                if(moving){app.Controller.MoveHorizontal(new Vector2(Mathf.Cos(Time.time*.3f),Mathf.Sin(Time.time*.3f))*1200*Time.unscaledDeltaTime);app.Controller.Advance(Time.unscaledDeltaTime);}
                if(zoom){app.Controller.SetView(app.Ecology.EnvironmentAt(Math.Max(0,GetComponent<EcologyPresentation>().SelectedRegion)).Center,25,45,Mathf.Lerp(120,12000,(Mathf.Sin(Time.time*1.7f)+1)*.5f));}
                if(accelerated){app.Clock.Update(30,120);app.Ecology.AdvanceTo(app.Clock.State.Tick,2);ecologyMax=Math.Max(ecologyMax,app.Ecology.LastStepMilliseconds);}
                float ms=Time.unscaledDeltaTime*1000;values.Add(ms);trace.Record(ms,app.Terrain?.LastUpdateMilliseconds??0,app.Terrain?.OwnedMeshes??0);
                FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){gpu+=timing[0].gpuFrameTime;gpuCount++;}
            }
            values.Sort();double sum=0;foreach(float v in values)sum+=v;var eco=app.Ecology;
            results.measurements.Add(new Measurement {scenario=scenario,frames=values.Count,rendered=rendered-startRendered,width=Screen.width,height=Screen.height,meanMs=sum/values.Count,p95Ms=values[(int)(values.Count*.95)],p99Ms=values[(int)(values.Count*.99)],maxMs=values[values.Count-1],gpuMs=gpuCount>0?gpu/gpuCount:-1,cpuMs=(process.TotalProcessorTime.TotalMilliseconds-cpuStart)/values.Count,ecologyMs=ecologyMax>0?ecologyMax:eco?.LastStepMilliseconds??0,managedMiB=GC.GetTotalMemory(false)/1048576.0,unityMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0,collections=GC.CollectionCount(0)-gc,animals=GetComponent<EcologyPresentation>()?.VisibleAnimals??0,regions=eco?.Count??0,tick=app.Clock.State.Tick,steps=eco?.Steps??0,deer=eco?.TotalPopulation(0)??0,browsers=eco?.TotalPopulation(1)??0,hares=eco?.TotalPopulation(2)??0,wolves=eco?.TotalPopulation(3)??0});
            trace.Write(Path.Combine(output,scenario+"-frames.csv"));
            var measurement=results.measurements[results.measurements.Count-1];measurement.elapsedSeconds=Time.realtimeSinceStartup-began;measurement.yearsPerSecond=(app.Clock.State.Tick-initialTick)/1440.0/measurement.elapsedSeconds;
        }
        private void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;}
    }
}
