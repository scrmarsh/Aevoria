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
    public sealed class Alpha009FinalAudit:MonoBehaviour
    {
        [Serializable]private sealed class Measurement{public string size,seed,scenario;public int frames,renders,width,height,trees,solidChunks;public double meanMs,p95Ms,p99Ms,maxMs,cpuMs,gpuMs,unityMiB,managedMiB;}
        [Serializable]private sealed class WorldResult{public string size,seed;public double createSeconds,loadSeconds;public bool persistence;}
        [Serializable]private sealed class Results{public string version=ApplicationVersion.Number,cpu,gpu,graphicsPreset="High Quality";public int generator=ProceduralGeography.GeneratorVersion;public bool uncapped=true;public ApplicationSettings graphics;public int errors,renders;public bool cancellation,cleanup;public List<WorldResult> worlds=new List<WorldResult>();public List<Measurement> measurements=new List<Measurement>();}
        private struct Motion{public double X,Y,Z;public Vector3 Camera,Origin;public float Dt,Vertical;public bool Flying,Grounded;}
        private readonly Results results=new Results();private AevoriaBootstrap app;private string output,sizeName,seed;private System.Diagnostics.Process process;
        private void Require(bool value,string message){if(value)return;File.WriteAllText(Path.Combine(output,"failure.txt"),message+"\n"+app.Message);throw new InvalidOperationException(message);}
        private void Rendered(ScriptableRenderContext context,Camera camera){if(camera.CompareTag("MainCamera"))results.renders++;}
        private void Logged(string message,string stack,LogType type){if(type!=LogType.Exception&&type!=LogType.Error)return;results.errors++;File.AppendAllText(Path.Combine(output,"exceptions.txt"),message+"\n"+stack+"\n");Application.Quit(8);}
        private IEnumerator Start()
        {
            Require(AevoriaBootstrap.Argument("-aevoriaTestRoot")!=null,"Final audit requires isolated saves");output=AevoriaBootstrap.Argument("-aevoriaOutput");Directory.CreateDirectory(output);app=GetComponent<AevoriaBootstrap>();app.ExternalControl=true;Application.runInBackground=true;process=System.Diagnostics.Process.GetCurrentProcess();results.cpu=SystemInfo.processorType;results.gpu=SystemInfo.graphicsDeviceName;Application.logMessageReceived+=Logged;RenderPipelineManager.endCameraRendering+=Rendered;yield return null;
            foreach(var size in new[]{WorldSize.Miniature,WorldSize.Small,WorldSize.Medium,WorldSize.Large,WorldSize.Colossal})
            {
                app.ReturnToMenu();yield return null;yield return null;sizeName=size.ToString();seed=size==WorldSize.Small||size==WorldSize.Colossal?"aevoria-001":"shaping-continents";File.WriteAllText(Path.Combine(output,"live-status.txt"),sizeName+": creating");float start=Time.realtimeSinceStartup;
                app.CreateWorld("Natural world acceptance "+sizeName,seed,size,new GenerationSettings(mountains:size==WorldSize.Colossal?12686.6143f:3600,landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();Require(app.WorldActive,"World created "+sizeName);var world=new WorldResult{size=sizeName,seed=seed,createSeconds=Time.realtimeSinceStartup-start};results.worlds.Add(world);app.Clock.Pause();app.Controller.ExternalDriver=true;app.Explorer.AcceptInput=false;
                var high=new ApplicationSettings{AutosaveSeconds=0};high.ApplyGraphicsPreset(1);high.MotionBlur=false;high.ShowFps=true;high.ShowFrameTime=true;app.ApplySettings(high,false);results.graphics=high.Copy();QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                var geo=app.Terrain.Geography;var hydro=geo.Hydrology;app.Controller.SetView(new GeoPoint(0,0),25,65,(float)geo.Configuration.WidthMeters*1.05f);yield return Ready();yield return Capture("overview");yield return Measure("overview",5);
                int forest=0;double best=-1;for(int i=0;i<app.Ecology.Count;i++)if(app.Ecology.EnvironmentAt(i).Land&&app.Ecology.Forest(i)>best){best=app.Ecology.Forest(i);forest=i;}var stand=app.Ecology.EnvironmentAt(forest).Center;app.Controller.SetView(stand,25,42,450);yield return Ready();yield return Capture("forest");yield return Measure("forest",6);
                int peak=0;for(int i=1;i<hydro.Ground.Length;i++)if(hydro.Ground[i]>hydro.Ground[peak])peak=i;app.Controller.SetView(new GeoPoint(hydro.X(peak),hydro.Y(peak)),25,45,6000);yield return Ready();yield return Capture("mountain");yield return Measure("mountain-orbit",6,1);
                int river=-1;double discharge=-1;for(int i=0;i<hydro.Ground.Length;i++)if(hydro.IsRiver(i)&&hydro.Discharge[i]>discharge){hydro.PathPoint(i,(hydro.PathSamples(i)-1)/2,out double x,out double z);double height=geo.ElevationAt(x,z);if(height<=geo.SeaLevel+4||geo.InlandSurfaces.LakeAt(x,z,out _))continue;discharge=hydro.Discharge[i];river=i;}
                if(river>=0){hydro.PathPoint(river,(hydro.PathSamples(river)-1)/2,out double x,out double z);app.Controller.SetView(new GeoPoint(x,z),25,45,1000);yield return Ready();yield return Capture("river");yield return Measure("river",5);}
                app.Navigation.Toggle();yield return null;yield return Capture("map");app.Navigation.Toggle();
                bool entered=false;for(int i=0;i<app.Ecology.Count&&!entered;i++)if(app.Ecology.EnvironmentAt(i).Land)entered=app.Explorer.EnterAt(app.Ecology.EnvironmentAt(i).Center);Require(entered,"Dry supported spawn "+sizeName);yield return Ready();app.Explorer.SetFirstPerson(false);app.Living.Traveler.ReverseThirdPerson=true;yield return null;yield return Capture("roman-front");yield return Measure("standing",4);app.Living.Traveler.ReverseThirdPerson=false;yield return Measure("walking",5,2);yield return Measure("running",4,3);
                var origin=app.Explorer.GeographicPosition;app.Explorer.Exit();Require(app.Explorer.EnterAt(new GeoPoint(origin.X,origin.Z)),"Explicit recovery before flight");yield return Ready();Require(app.Explorer.SetFlight(true),"Enable flight");high.FlightSpeed=500;app.ApplySettings(high,false);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;yield return Measure("maximum-flight",6,4);var pose=app.Explorer.GeographicPosition;Require(app.Explorer.SetFlight(false),"Disable flight");Require(pose.X==app.Explorer.GeographicPosition.X&&pose.Y==app.Explorer.GeographicPosition.Y&&pose.Z==app.Explorer.GeographicPosition.Z,"No freefall teleport");yield return Measure("freefall",5,5);Require(app.Explorer.VerticalSpeed<-.5f,"Gravity produces actual descending freefall");yield return Capture("freefall");
                app.Explorer.Exit();app.Controller.SetView(stand,25,45,700);yield return Ready();if(size==WorldSize.Colossal)yield return Measure("sustained-streaming",180,6);
                string ecology=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture())),living=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));app.SaveWorld();yield return Operation();Require(app.Current!=null,"Saved "+sizeName);string id=app.Current.Id;app.ReturnToMenu();yield return null;yield return null;start=Time.realtimeSinceStartup;app.LoadWorld(id);yield return Operation();world.loadSeconds=Time.realtimeSinceStartup-start;Require(app.WorldActive,"Loaded "+sizeName);app.Clock.Pause();app.Controller.ExternalDriver=true;app.Explorer.AcceptInput=false;world.persistence=ecology==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Ecology.Capture()))&&living==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));Require(world.persistence,"Authority survives full reconstruction "+sizeName);yield return Capture("reloaded");WriteResults();
            }
            app.ReturnToMenu();yield return null;yield return null;results.cleanup=FindObjectsByType<WorldRenderer>(FindObjectsSortMode.None).Length==0&&FindObjectsByType<DistantCavePresentation>(FindObjectsSortMode.None).Length==0&&FindObjectsByType<ExcavationPresentation>(FindObjectsSortMode.None).Length==0;Require(results.cleanup,"Presentation cleanup");app.CreateWorld("Cancellation acceptance","cancel-natural-009",WorldSize.Colossal,new GenerationSettings(),new ClimateSettings());yield return null;app.CancelPreparation();yield return Operation();results.cancellation=!app.WorldActive;Require(results.cancellation,"Cancellation preserves existing archives");app.Menu.SelectedChapter=8;app.Menu.Show(ApplicationPage.Chronicles);yield return Capture("book-ix");app.Menu.Show(ApplicationPage.Changelog);yield return Capture("changelog");WriteResults();Require(results.errors==0,"No runtime errors");Debug.Log("AEVORIA009_FINAL_AUDIT_COMPLETE");Application.Quit();
        }
        private IEnumerator Operation(){float end=Time.realtimeSinceStartup+900;while(app.Busy&&Time.realtimeSinceStartup<end){File.WriteAllText(Path.Combine(output,"live-status.txt"),sizeName+": "+app.Progress);yield return new WaitForSecondsRealtime(.25f);}Require(!app.Busy,"Operation completed");}
        private IEnumerator Ready()
        {
            // SetView is consumed by LateUpdate. Before that update, IsReady can
            // still describe the previous camera's desired tiles. Allow both
            // camera publication and streaming discovery before trusting it.
            yield return null;yield return null;
            float end=Time.realtimeSinceStartup+240;int settled=0;
            while(settled<3&&Time.realtimeSinceStartup<end)
            {
                settled=app.Terrain.IsReady&&app.Waters.IsReady&&app.Explorer.Ready?settled+1:0;
                yield return null;
            }
            Require(settled>=3,"Presentation settled "+sizeName);
            yield return new WaitForSecondsRealtime(.5f);
            Require(app.Terrain.IsReady&&app.Waters.IsReady&&app.Explorer.Ready,"Presentation remains settled "+sizeName);
        }
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,sizeName+"-"+name+".png"));yield return null;}
        private IEnumerator Measure(string scenario,float seconds,int movement=0)
        {
            File.WriteAllText(Path.Combine(output,"live-status.txt"),sizeName+": measuring "+scenario);using var trace=new PerformanceTrace();trace.Reset();var frames=new List<float>(30000);var motion=new List<Motion>(30000);yield return null;yield return null;trace.Reset();var timing=new FrameTiming[1];double cpu=process.TotalProcessorTime.TotalMilliseconds,gpu=0;int gpuFrames=0,renders=results.renders;float end=Time.realtimeSinceStartup+seconds;var focus=app.Controller.Focus;float yaw=Camera.main.transform.eulerAngles.y;
            while(Time.realtimeSinceStartup<end)
            {
                float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);if(movement==1){yaw+=dt*5;app.Controller.SetView(new GeoPoint(focus.x,focus.z),yaw,45,6000);}else if(movement==2||movement==3)app.Explorer.StepMovement(Vector2.up,movement==3,false,false,dt);else if(movement==4)app.Explorer.StepFlight(Vector2.up,Time.realtimeSinceStartup<end-seconds*.65f,false,true,dt);else if(movement==5)app.Explorer.StepMovement(Vector2.zero,false,false,false,dt);else if(movement==6){app.Controller.MoveHorizontal(new Vector2(Mathf.Sin(Time.time*.2f),Mathf.Cos(Time.time*.2f))*300*dt);app.Controller.Advance(dt);}
                yield return null;float ms=Time.unscaledDeltaTime*1000;frames.Add(ms);if(movement>1||app.Living.Traveler.Exploring){var p=app.Explorer.GeographicPosition;motion.Add(new Motion{X=p.X,Y=p.Y,Z=p.Z,Camera=Camera.main.transform.position,Origin=WorldOrigin.Offset,Dt=dt,Vertical=app.Explorer.VerticalSpeed,Flying=app.Explorer.Flying,Grounded=app.Explorer.Grounded});}trace.Record(ms,app.Terrain.LastUpdateMilliseconds,app.Terrain.OwnedMeshes);FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){gpu+=timing[0].gpuFrameTime;gpuFrames++;}
            }
            double sum=0;foreach(float value in frames)sum+=value;frames.Sort();Require(frames.Count>30,"Real rendered benchmark frames");results.measurements.Add(new Measurement{size=sizeName,seed=seed,scenario=scenario,frames=frames.Count,renders=results.renders-renders,width=Screen.width,height=Screen.height,trees=app.Terrain.Vegetation.VisibleTrees,solidChunks=app.Excavations.ResidentChunks,meanMs=sum/frames.Count,p95Ms=frames[(int)(frames.Count*.95)],p99Ms=frames[(int)(frames.Count*.99)],maxMs=frames[frames.Count-1],cpuMs=(process.TotalProcessorTime.TotalMilliseconds-cpu)/frames.Count,gpuMs=gpuFrames>0?gpu/gpuFrames:-1,unityMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0,managedMiB=GC.GetTotalMemory(false)/1048576.0});trace.Write(Path.Combine(output,sizeName+"-"+scenario+"-frames.csv"));if(motion.Count>0){using var csv=new StreamWriter(Path.Combine(output,sizeName+"-"+scenario+"-motion.csv"));csv.WriteLine("frame,stepSeconds,x,y,z,cameraLocalX,cameraLocalY,cameraLocalZ,originX,originZ,verticalSpeed,flying,grounded");for(int i=0;i<motion.Count;i++){var p=motion[i];csv.WriteLine(FormattableString.Invariant($"{i},{p.Dt:R},{p.X:R},{p.Y:R},{p.Z:R},{p.Camera.x:R},{p.Camera.y:R},{p.Camera.z:R},{p.Origin.x:R},{p.Origin.z:R},{p.Vertical:R},{p.Flying},{p.Grounded}"));}}WriteResults();
        }
        private void WriteResults()=>File.WriteAllText(Path.Combine(output,"audit.json"),JsonUtility.ToJson(results,true));
        private void OnDestroy(){Application.logMessageReceived-=Logged;RenderPipelineManager.endCameraRendering-=Rendered;process?.Dispose();}
    }
}
