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
    // Opt-in rendered evidence. It never opens the normal save repository.
    public sealed class Alpha006Audit : MonoBehaviour
    {
        [Serializable] public sealed class Run {public string version=ApplicationVersion.Number,cpu,gpu;public bool continuation,cleanup,walk,jump,crouch,dig;public List<Sample> samples=new List<Sample>();public List<string> notes=new List<string>();}
        [Serializable] public sealed class Sample {public string scenario;public int frames,rendered,width,height,meshes,regions;public double meanMs,p95Ms,p99Ms,maxMs,cpuMs,gpuMs,managedMiB,unityMiB;}
        private AevoriaBootstrap app;private string output;private Run result;private int rendered;
        private void Rendered(ScriptableRenderContext c,Camera camera){if(camera.CompareTag("MainCamera"))rendered++;}
        private IEnumerator Start()
        {
            if(AevoriaBootstrap.Argument("-aevoriaTestRoot")==null){Debug.LogError("Audit006 requires isolated save root");Application.Quit(4);yield break;}
            app=GetComponent<AevoriaBootstrap>();output=AevoriaBootstrap.Argument("-aevoriaOutput");Directory.CreateDirectory(output);
            result=new Run{cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName};Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;RenderPipelineManager.endCameraRendering+=Rendered;Application.logMessageReceived+=LogFailure;
            yield return new WaitForSecondsRealtime(1);yield return Capture("01-menu");app.Menu.Show(ApplicationPage.Changelog);yield return Capture("02-changelog");app.Menu.Show(ApplicationPage.Chronicles);yield return Capture("03-book-vi");app.Menu.Show(ApplicationPage.Create);yield return new WaitForSecondsRealtime(2);yield return Capture("04-creation-preview");
            app.CreateWorld("Living world validation","ecosystem-005",WorldSize.Small,new GenerationSettings(),new ClimateSettings());yield return Operation();Require(app.WorldActive,"creation");app.Clock.Pause();app.Controller.ExternalDriver=true;
            yield return Ready();yield return Capture("05-overview");yield return Measure("overview",5);
            int forest=Find(i=>app.Ecology.EnvironmentAt(i).Land&&app.Ecology.Forest(i)>.1),grass=Find(i=>app.Ecology.EnvironmentAt(i).Land&&app.Ecology.Forest(i)<.05);
            Focus(forest,240);yield return Ready();yield return Capture("06-forest");yield return Measure("forest",5);
            Focus(grass,170);yield return Ready();yield return Capture("07-grassland");
            var lake=app.Waters.Layout.Lakes.Vertices;Require(lake.Count>0,"lake available");var lp=lake[lake.Count/2];app.Controller.SetView(new GeoPoint(lp.X,lp.Z),25,45,650);yield return Ready();yield return Capture("08-lake-shore");yield return Measure("water",5);
            var river=app.Waters.Layout.Rivers.Vertices;var rp=river[river.Count/3];app.Controller.SetView(new GeoPoint(rp.X,rp.Z),25,40,350);yield return Ready();yield return Capture("09-river-banks");
            for(int i=0;i<6;i++){app.Controller.SetView(new GeoPoint(lp.X+i*110,lp.Z+i*65),20,48,900+i*220);yield return new WaitForSecondsRealtime(.6f);yield return Capture("seam-sequence-"+i);}
            Focus(grass,70);yield return Ready();app.Menu.Show(ApplicationPage.Controls);yield return Capture("10-independent-controls");app.Menu.Show(ApplicationPage.World);
            app.Navigation.Toggle();yield return new WaitForSecondsRealtime(3);yield return Capture("11-full-map");app.Navigation.Toggle();
            for(int shape=0;shape<3;shape++){var settings=app.Settings.Copy();settings.MinimapEnabled=true;settings.MinimapShape=shape;app.ApplySettings(settings);yield return new WaitForSecondsRealtime(3);yield return Capture("12-minimap-"+shape);}
            Require(app.Explorer.EnterAt(app.Ecology.EnvironmentAt(grass).Center),"safe exploration entry");app.Explorer.AcceptInput=false;yield return Ready();
            for(int i=0;i<45;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}
            app.Explorer.SetFirstPerson(false);yield return Capture("13-third-person");var before=app.Explorer.Position;
            for(int i=0;i<60;i++){app.Explorer.StepMovement(Vector2.up,false,false,false,1/60f);yield return null;}
            result.walk=Vector3.Distance(before,app.Explorer.Position)>1;
            app.Explorer.StepMovement(Vector2.zero,false,true,false,.02f);result.crouch=app.Living.Traveler.Crouched;yield return Capture("14-crouch");
            app.Explorer.StepMovement(Vector2.zero,false,false,true,.02f);float initialY=app.Explorer.Position.y;for(int i=0;i<10;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,.02f);yield return null;}result.jump=app.Explorer.Position.y>initialY+.1f;yield return Capture("15-jump");
            for(int i=0;i<60;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,.02f);yield return null;}
            app.Explorer.SetFirstPerson(true);yield return Capture("16-first-person");yield return Measure("exploration",5);
            var geo=app.Terrain.Geography;bool dug=false;for(int z=-30;z<=30&&!dug;z++)for(int x=-30;x<=30&&!dug;x++){double px=x*32,pz=z*32;app.Living.Traveler.Tool=ToolKind.Shovel;dug=app.Explorer.DigAt(new Vector3((float)px,geo.ElevationAt(px,pz),(float)pz),ResourceKind.Soil);if(dug)break;}
            result.dig=dug;app.Explorer.Exit();if(dug){var edit=app.Living.Excavations[0];app.Controller.SetView(new GeoPoint(edit.X*32,edit.Z*32),20,55,9);yield return Measure("excavation-install-and-travel",4);yield return Ready();yield return Capture("17-solid-excavation");}
            // Credit actual existing natural wood through the authoritative harvesting API.
            bool harvested=false;for(int z=-80;z<80&&!harvested;z++)for(int x=-80;x<80&&!harvested;x++)if(app.Living.TryNatural(x,z,out var site)&&site.Resource==ResourceKind.Wood&&(site.Kind==NaturalKind.Log||site.Kind==NaturalKind.Branch)){app.Living.Traveler.Tool=site.Kind==NaturalKind.Log?ToolKind.Axe:ToolKind.Hands;harvested=app.Living.Harvest(site.Id,site.Resource,site.Quantity,app.Living.Traveler.Tool,app.Clock.State.Tick)>0;}
            var pp=app.Explorer.Position;if(harvested)app.Living.Place(pp.x+2,LocalSolidField.RenderedGround(geo,pp.x+2,pp.z),pp.z,0);
            app.Controller.SetView(new GeoPoint(pp.x,pp.z),30,30,12);yield return Ready();yield return Capture("18-resource-funded-placement");
            string living=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture())),eco=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture()));app.SaveWorld();yield return Operation();string id=app.Current.Id;app.ReturnToMenu();yield return new WaitForSecondsRealtime(.5f);app.LoadWorld(id);yield return Operation();app.Clock.Pause();app.Controller.ExternalDriver=true;
            result.continuation=living==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()))&&eco==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture()));yield return Capture("19-reloaded-world");
            for(int repeat=0;repeat<2;repeat++){string saved=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));app.SaveWorld();yield return Operation();app.ReturnToMenu();yield return new WaitForSecondsRealtime(.3f);app.LoadWorld(id);yield return Operation();app.Clock.Pause();app.Controller.ExternalDriver=true;result.continuation&=saved==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));Require(result.continuation,"repeated reload continuity");}
            yield return Measure("moving-soak",180,true);app.Clock.Start();app.Clock.SetSpeed(8);yield return Measure("accelerated",12,true);app.Clock.Pause();
            app.ReturnToMenu();yield return new WaitForSecondsRealtime(.5f);
            app.CreateWorld("Seasonal validation","ecosystem-005",WorldSize.Small,new GenerationSettings(),new ClimateSettings(-12,1.2f,60));yield return Operation();Require(app.WorldActive,"seasonal creation");app.Clock.Pause();app.Controller.ExternalDriver=true;Focus(Find(i=>app.Ecology.EnvironmentAt(i).Land),300);yield return Ready();yield return Capture("20-season-start");
            for(int season=0;season<4;season++){Advance(360);yield return new WaitForSecondsRealtime(2);yield return Capture("21-season-"+season);}
            var lakes=app.Waters.Layout.Lakes.Vertices;if(lakes.Count>0){var p=lakes[lakes.Count/2];app.Controller.SetView(new GeoPoint(p.X,p.Z),20,45,650);yield return Ready();yield return Capture("22-cold-lake");}
            foreach(var size in new[]{WorldSize.Huge,WorldSize.Colossal}){app.ReturnToMenu();yield return new WaitForSecondsRealtime(.5f);float start=Time.realtimeSinceStartup;app.CreateWorld(size+" validation","aevoria-001",size,new GenerationSettings(),new ClimateSettings());yield return Operation();Require(app.WorldActive,size+" creation");result.notes.Add(size+" create seconds "+(Time.realtimeSinceStartup-start));app.Clock.Pause();app.Controller.ExternalDriver=true;yield return Ready();yield return Capture("23-"+size+"-overview");yield return Measure(size+"-overview",6);app.Navigation.Toggle();yield return new WaitForSecondsRealtime(3);yield return Capture("24-"+size+"-map");app.Navigation.Toggle();Focus(Find(i=>app.Ecology.EnvironmentAt(i).Land),500);yield return Ready();yield return Capture("25-"+size+"-local");yield return Measure(size+"-local",8,true);string saved=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture()));app.SaveWorld();yield return Operation();string worldId=app.Current.Id;app.ReturnToMenu();yield return new WaitForSecondsRealtime(.3f);app.LoadWorld(worldId);yield return Operation();app.Clock.Pause();app.Controller.ExternalDriver=true;Require(saved==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture())),size+" reload");yield return Capture("26-"+size+"-reloaded");}
            app.ReturnToMenu();yield return new WaitForSecondsRealtime(1);result.cleanup=FindObjectsByType<WorldRenderer>(FindObjectsSortMode.None).Length==0&&FindObjectsByType<ExperimentalExplorer>(FindObjectsSortMode.None).Length==0;File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(result,true));Require(result.continuation&&result.cleanup&&result.walk&&result.jump&&result.crouch&&result.dig,"continuity, cleanup and physical controls");Debug.Log("AEVORIA006_AUDIT_PASS");Application.Quit();
        }
        private void Advance(int ticks){app.Clock.Start();app.Clock.Update(ticks*.25,ticks);app.Clock.Pause();app.Ecology.AdvanceTo(app.Clock.State.Tick,120);app.Living.Seasons.AdvanceTo(app.Clock.State.Tick,120);}
        private void Require(bool ok,string name){if(!ok){File.WriteAllText(Path.Combine(output,"failure.txt"),name+": "+app.Message);Debug.LogError("Audit006 failed: "+name);Application.Quit(9);throw new InvalidOperationException(name);}}
        private int Find(Func<int,bool> p){for(int i=0;i<app.Ecology.Count;i++)if(p(i))return i;throw new InvalidOperationException("Habitat unavailable");}
        private void Focus(int region,float distance)=>app.Controller.SetView(app.Ecology.EnvironmentAt(region).Center,25,40,distance);
        private IEnumerator Operation(){float end=Time.realtimeSinceStartup+240;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Require(!app.Busy,"operation timeout");}
        private IEnumerator Ready(){float end=Time.realtimeSinceStartup+120;while((!app.Terrain.IsReady||!app.Excavations.Ready||!app.Explorer.Ready)&&Time.realtimeSinceStartup<end)yield return null;Require(app.Terrain.IsReady&&app.Explorer.Ready,"presentation ready");yield return new WaitForSecondsRealtime(1);}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private IEnumerator Measure(string name,float seconds,bool move=false)
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;using var trace=new PerformanceTrace();trace.Reset();yield return null;yield return null;var values=new List<float>(100000);int start=rendered;var process=System.Diagnostics.Process.GetCurrentProcess();double cpu=process.TotalProcessorTime.TotalMilliseconds,gpu=0;int gpuFrames=0;var timing=new FrameTiming[1];float end=Time.realtimeSinceStartup+seconds;
            while(Time.realtimeSinceStartup<end){yield return null;if(move){app.Controller.MoveHorizontal(new Vector2(Mathf.Sin(Time.time*.3f),Mathf.Cos(Time.time*.3f))*900*Time.unscaledDeltaTime);app.Controller.Advance(Time.unscaledDeltaTime);}float ms=Time.unscaledDeltaTime*1000;values.Add(ms);trace.Record(ms,app.Terrain.LastUpdateMilliseconds,app.Terrain.OwnedMeshes);FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){gpu+=timing[0].gpuFrameTime;gpuFrames++;}}
            double cpuMs=(process.TotalProcessorTime.TotalMilliseconds-cpu)/values.Count,sum=0;values.Sort();foreach(var v in values)sum+=v;result.samples.Add(new Sample{scenario=name,frames=values.Count,rendered=rendered-start,width=Screen.width,height=Screen.height,meanMs=sum/values.Count,p95Ms=values[(int)(values.Count*.95)],p99Ms=values[(int)(values.Count*.99)],maxMs=values[values.Count-1],cpuMs=cpuMs,gpuMs=gpuFrames==0?-1:gpu/gpuFrames,managedMiB=GC.GetTotalMemory(false)/1048576.0,unityMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0,regions=app.Ecology.Count,meshes=app.Terrain.OwnedMeshes});trace.Write(Path.Combine(output,name+"-frames.csv"));File.WriteAllText(Path.Combine(output,"progress.json"),JsonUtility.ToJson(result,true));
        }
        private void LogFailure(string text,string stack,LogType type){if(type==LogType.Exception){File.WriteAllText(Path.Combine(output,"exception.txt"),text+"\n"+stack);Application.Quit(10);}}
        private void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;Application.logMessageReceived-=LogFailure;}
    }
}
