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
    // Opt-in actual Windows gameplay audit. Normal startup never runs this.
    public sealed class Alpha010FoundationAudit:MonoBehaviour
    {
        [Serializable] private sealed class Results
        {
            public string version=ApplicationVersion.Number,cpu,gpu;public int initialCivilizations,initialSettlements,finalSettlements,renders,errors,width,height;
            public double initialPopulation,finalPopulation,createSeconds,saveSeconds,loadSeconds,acceleratedMeanMs,acceleratedP95Ms,acceleratedMaxMs,clockBacklogSeconds;
            public string worldSize;
            public long simulatedMonths;public bool persistence,cleanup;public ApplicationSettings graphics;
        }
        private AevoriaBootstrap app;private string output;private readonly Results result=new Results();
        private void Require(bool condition,string message){if(condition)return;File.WriteAllText(Path.Combine(output,"failure.txt"),message+"\n"+app.Message);throw new InvalidOperationException(message);}
        private void Logged(string message,string stack,LogType kind){if(kind!=LogType.Error&&kind!=LogType.Exception)return;result.errors++;File.AppendAllText(Path.Combine(output,"exceptions.txt"),message+"\n"+stack+"\n");Application.Quit(7);}
        private void Rendered(ScriptableRenderContext context,Camera camera){if(camera.CompareTag("MainCamera"))result.renders++;}
        private IEnumerator Start()
        {
            Application.runInBackground=true;
            output=AevoriaBootstrap.Argument("-aevoriaOutput");Directory.CreateDirectory(output);Application.logMessageReceived+=Logged;RenderPipelineManager.endCameraRendering+=Rendered;app=FindFirstObjectByType<AevoriaBootstrap>();result.cpu=SystemInfo.processorType;result.gpu=SystemInfo.graphicsDeviceName;result.width=Screen.width;result.height=Screen.height;
            WorldSize size=WorldSize.Miniature;string requestedSize=AevoriaBootstrap.Argument("-aevoriaAuditSize");if(!string.IsNullOrEmpty(requestedSize))Require(Enum.TryParse(requestedSize,out size)&&Enum.IsDefined(typeof(WorldSize),size),"Valid audit world size");int targetMonths=360;string requestedMonths=AevoriaBootstrap.Argument("-aevoriaAuditMonths");if(!string.IsNullOrEmpty(requestedMonths))Require(int.TryParse(requestedMonths,out targetMonths)&&targetMonths>=12&&targetMonths<=1200,"Valid audit month count");result.worldSize=size.ToString();
            yield return null;app.ReturnToMenu();yield return null;yield return null;float start=Time.realtimeSinceStartup;app.CreateWorld("Living world native foundation","shaping-continents",size,new GenerationSettings(landmass:LandmassType.Continents),new ClimateSettings());yield return Operation();result.createSeconds=Time.realtimeSinceStartup-start;Require(app.WorldActive,"Create actual populated world");app.Clock.Pause();app.ExternalControl=true;app.Controller.ExternalDriver=true;app.Explorer.AcceptInput=false;
            var high=new ApplicationSettings{AutosaveSeconds=0};high.ApplyGraphicsPreset(1);high.RenderClouds=false;high.RenderWeatherEffects=false;high.MotionBlur=false;high.ShowFps=true;high.ShowFrameTime=true;app.ApplySettings(high,false);result.graphics=high.Copy();
            result.initialCivilizations=app.Society.Civilizations.Count;result.initialSettlements=app.Society.Settlements.Count;result.initialPopulation=app.Society.TotalPopulation;Require(result.initialCivilizations>=2,"Multiple real civilizations");
            File.WriteAllBytes(Path.Combine(output,"society-before.xml"),ArchiveCodec.Encode(app.Society.Capture()));
            var selected=app.Society.Settlements[0];app.SocietyView.FocusSettlement(selected.Id);app.SocietyView.Open=false;yield return Ready();Require(app.Settlements.VisibleBuildings>=3,"Rendered multi-building camp");yield return Capture("small-settlement");app.SocietyView.SelectSettlement(selected.Id);yield return Capture("settlement-inspection");app.SocietyView.SelectCivilization(selected.CivilizationId);yield return Capture("civilization-statistics");
            app.Controller.SetView(new GeoPoint(0,0),25,65,(float)app.Terrain.Geography.Configuration.WidthMeters*1.05f);app.SocietyView.SetOverlay(SocietyOverlayMode.Political);app.SocietyView.Open=false;yield return Ready();yield return Capture("multiple-civilizations-political");app.SocietyView.ShowHistory();yield return Capture("founding-history");
            app.SocietyView.SetOverlay(SocietyOverlayMode.Off);app.SocietyView.FocusSettlement(selected.Id);yield return Ready();app.Clock.SetSpeed(128);app.Clock.Resume();var frames=new List<float>(20000);float end=Time.realtimeSinceStartup+900,nextStatus=0;
            using(var trace=File.CreateText(Path.Combine(output,"accelerated-frames.csv")))
            {
                trace.WriteLine("frame,frameMs,societyStepMs,settlementUpdateMs,societyMonths,clockTick,clockBacklogSeconds");
                while(app.Society.Months<targetMonths&&Time.realtimeSinceStartup<end)
                {
                    if(Time.unscaledTime>=nextStatus){nextStatus=Time.unscaledTime+1;File.WriteAllText(Path.Combine(output,"live-status.txt"),$"Simulating month {app.Society.Months}/{targetMonths}; population {app.Society.TotalPopulation:0.0}; clock debt {app.Clock.BacklogSeconds:0.00}s");}
                    frames.Add(Time.unscaledDeltaTime*1000);trace.WriteLine(FormattableString.Invariant($"{Time.frameCount},{Time.unscaledDeltaTime*1000:R},{app.Society.LastStepMilliseconds:R},{app.Settlements.LastUpdateMilliseconds:R},{app.Society.Months},{app.Clock.State.Tick},{app.Clock.BacklogSeconds:R}"));yield return null;
                }
            }
            app.Clock.Pause();Require(app.Society.Months>=targetMonths,"Requested calendar months progressed through the live clock");result.clockBacklogSeconds=app.Clock.BacklogSeconds;double sum=0;foreach(float frame in frames)sum+=frame;frames.Sort();result.acceleratedMeanMs=sum/frames.Count;result.acceleratedP95Ms=frames[(int)(frames.Count*.95)];result.acceleratedMaxMs=frames[frames.Count-1];result.simulatedMonths=app.Society.Months;result.finalPopulation=app.Society.TotalPopulation;result.finalSettlements=app.Society.Settlements.Count;
            Require(Math.Abs(result.finalPopulation-result.initialPopulation)>.1,"Population changed through actual simulated time");File.WriteAllBytes(Path.Combine(output,"society-after.xml"),ArchiveCodec.Encode(app.Society.Capture()));app.SocietyView.FocusSettlement(selected.Id);yield return Ready();yield return Capture("settlement-after-"+targetMonths+"-months");app.SocietyView.ShowHistory();yield return Capture("development-history");
            string before=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Society.Capture()));start=Time.realtimeSinceStartup;app.SaveWorld();yield return Operation();result.saveSeconds=Time.realtimeSinceStartup-start;Require(app.Current!=null,"Society archive saved");string id=app.Current.Id;app.ReturnToMenu();yield return null;yield return null;result.cleanup=app.Society==null&&FindObjectsByType<SettlementPresentation>(FindObjectsSortMode.None).Length==0&&FindObjectsByType<SocietyObservation>(FindObjectsSortMode.None).Length==0;Require(result.cleanup,"Society presentation cleanup");
            start=Time.realtimeSinceStartup;app.LoadWorld(id);yield return Operation();result.loadSeconds=Time.realtimeSinceStartup-start;Require(app.WorldActive,"Society archive loaded");app.Clock.Pause();app.Controller.ExternalDriver=true;app.Explorer.AcceptInput=false;result.persistence=before==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Society.Capture()));Require(result.persistence,"Population, stores, territory, colors and history survive reconstruction");app.SocietyView.SetOverlay(SocietyOverlayMode.Political);app.SocietyView.FocusSettlement(selected.Id);yield return Ready();yield return Capture("reloaded-civilization");
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(output,"result.txt"),"PASS — civilization foundation only; characters and final all-size acceptance are subsequent work.\n");Debug.Log("AEVORIA010_FOUNDATION_AUDIT_COMPLETE");Application.Quit();
        }
        private IEnumerator Operation(){float end=Time.realtimeSinceStartup+900;while(app.Busy&&Time.realtimeSinceStartup<end){File.WriteAllText(Path.Combine(output,"live-status.txt"),app.Progress.Length>0?app.Progress:app.Message);yield return null;}Require(!app.Busy,"World operation completed");}
        private IEnumerator Ready(){yield return null;yield return null;float end=Time.realtimeSinceStartup+240;int good=0;while(good<3&&Time.realtimeSinceStartup<end){good=app.Terrain.IsReady&&app.Waters.IsReady&&app.Explorer.Ready?good+1:0;yield return null;}Require(good>=3,"Current camera presentation settled");yield return new WaitForSecondsRealtime(.4f);}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private void OnDestroy(){Application.logMessageReceived-=Logged;RenderPipelineManager.endCameraRendering-=Rendered;}
    }
}
