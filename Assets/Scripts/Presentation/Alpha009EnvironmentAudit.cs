using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Aevoria.World;
using Aevoria.Persistence;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    // Opt-in native evidence using real world generation, rendering and clock.
    public sealed class Alpha009EnvironmentAudit:MonoBehaviour
    {
        [Serializable]private sealed class CloudView{public string climate;public float range;public long tick;public Vector4[] systems,shapes;}
        [Serializable]private sealed class Evidence{public int generator=ProceduralGeography.GeneratorVersion,hydrology=HydrologyMap.Version;public List<CloudView> cloudViews=new List<CloudView>();public List<string> notes=new List<string>();public int renders,errors;}
        private AevoriaBootstrap app;private string output,label;private readonly Evidence evidence=new Evidence();
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        private void Rendered(ScriptableRenderContext context,Camera camera){if(camera.CompareTag("MainCamera"))evidence.renders++;}
        private void Logged(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;evidence.errors++;File.AppendAllText(Path.Combine(output,"failures.txt"),message+"\n"+stack+"\n");Application.Quit(4);}
        private IEnumerator Start()
        {
            Require(AevoriaBootstrap.Argument("-aevoriaTestRoot")!=null,"Environment audit requires isolated saves");output=AevoriaBootstrap.Argument("-aevoriaOutput");Require(!string.IsNullOrEmpty(output),"Explicit evidence output");Directory.CreateDirectory(output);
            app=GetComponent<AevoriaBootstrap>();app.ExternalControl=true;Application.runInBackground=true;RenderPipelineManager.endCameraRendering+=Rendered;Application.logMessageReceived+=Logged;yield return null;
            if(AevoriaBootstrap.HasFlag("-aevoria011BorealOnly")){yield return Boreal();Application.Quit();yield break;}
            foreach(bool cold in new[]{false,true})
            {
                app.ReturnToMenu();yield return null;yield return null;label=cold?"cold":"tropical";
                app.CreateWorld("Natural environment "+label,"shaping-continents",WorldSize.Miniature,new GenerationSettings(mountains:12686.6143f,landmass:LandmassType.Continents),cold?new ClimateSettings(-15,1,55):new ClimateSettings());
                float end=Time.realtimeSinceStartup+600,nextStatus=0;
                while(app.Busy&&Time.realtimeSinceStartup<end){if(Time.realtimeSinceStartup>=nextStatus){File.WriteAllText(Path.Combine(output,"live-status.txt"),label+": "+app.Progress);nextStatus=Time.realtimeSinceStartup+1;}yield return null;}
                Require(app.WorldActive&&!app.Busy,"Created "+label+" environment");app.Clock.Pause();app.Controller.ExternalDriver=true;
                var settings=app.Settings.Copy();settings.AutosaveSeconds=0;settings.CloudQuality=2;settings.CloudDensity=3;settings.RenderClouds=true;settings.RenderWeatherEffects=false;settings.MotionBlur=false;settings.WaterQuality=2;settings.WaterTransparency=2;app.ApplySettings(settings,false);
                var geo=app.Terrain.Geography;var h=geo.Hydrology;int peak=0;for(int i=1;i<h.Ground.Length;i++)if(h.Ground[i]>h.Ground[peak])peak=i;var summit=new GeoPoint(h.X(peak),h.Y(peak));
                // Clear snow/material review is independent of cloud display.
                settings.RenderClouds=false;app.ApplySettings(settings,false);app.Controller.SetView(summit,25,42,3500);yield return Ready();yield return Capture("snow-range");
                using(var csv=new StreamWriter(Path.Combine(output,label+"-snow-motion.csv")))
                {
                    csv.WriteLine("frame,engineMs,cameraX,cameraY,cameraZ,yaw");
                    for(int i=0;i<120;i++){app.Controller.SetView(summit,25+i*.04f,42,3500);yield return null;var p=WorldOrigin.Geographic(Camera.main.transform);csv.WriteLine(FormattableString.Invariant($"{i},{Time.unscaledDeltaTime*1000:R},{p.x:R},{p.y:R},{p.z:R},{Camera.main.transform.eulerAngles.y:R}"));if(i%20==0)yield return Capture("snow-motion-"+i);}
                }
                var lakes=geo.InlandSurfaces.Lakes.Vertices;int frozen=-1;float coldest=float.MaxValue;
                for(int i=0;i<lakes.Count;i+=3){var p=lakes[i];var climate=geo.Climate.Sample(p.X,p.Z,p.Level);float warmest=climate.TemperatureCelsius+CryosphereClimate.SeasonAmplitude(geo.Climate.LatitudeAt(p.Z),geo.Climate.Continentality(p.X,p.Z));if(warmest<coldest){coldest=warmest;frozen=i;}}
                if(frozen>=0){var p=lakes[frozen];app.Controller.SetView(new GeoPoint(p.X,p.Z),25,50,1800);yield return Ready();yield return Capture("lake");evidence.notes.Add(label+": inspected lake warmest-season air temperature "+coldest.ToString("F2",CultureInfo.InvariantCulture)+" C");}
                int warm=-1;for(int i=0;i<app.Ecology.Count;i++){var e=app.Ecology.EnvironmentAt(i);if(e.Height>1000&&e.Temperature>5){warm=i;break;}}
                if(warm>=0){app.Controller.SetView(app.Ecology.EnvironmentAt(warm).Center,25,50,1800);yield return Ready();yield return Capture("warm-highland");}else evidence.notes.Add(label+": no warm highland inspection centre");
                settings.RenderClouds=true;app.ApplySettings(settings,false);Vector4[] fixedSystems=null,fixedShapes=null;
                foreach(float range in new[]{2000,20000,60000,150000})
                {
                    var lowland=app.Society.Settlements[0];app.Controller.SetView(new GeoPoint(lowland.X,lowland.Z),25,55,range);yield return Ready();yield return Capture("cloud-zoom-"+range);
                    var systems=Shader.GetGlobalVectorArray("_AevoriaCloudSystems");var shapes=Shader.GetGlobalVectorArray("_AevoriaCloudShapes");
                    if(fixedSystems==null){fixedSystems=systems;fixedShapes=shapes;}else for(int i=0;i<systems.Length;i++){Require(systems[i]==fixedSystems[i],"Cloud physical identity through zoom");Require(shapes[i]==fixedShapes[i],"Cloud density through zoom");}
                    evidence.cloudViews.Add(new CloudView{climate=label,range=app.Controller.Distance,tick=app.Clock.State.Tick,systems=systems,shapes=shapes});
                }
                string weather=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Atmosphere.Capture()));settings.RenderClouds=false;settings.RenderWeatherEffects=true;app.ApplySettings(settings,false);yield return Capture("weather-only");Require(weather==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Atmosphere.Capture())),"Cloud display does not alter weather state");
                settings.RenderClouds=true;settings.RenderWeatherEffects=false;app.ApplySettings(settings,false);yield return Capture("clouds-only");Require(weather==ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Atmosphere.Capture())),"Weather display does not alter weather state");
                app.Clock.SetSpeed(8);app.Clock.Start();
                using(var csv=new StreamWriter(Path.Combine(output,label+"-cloud-motion.csv")))
                {
                    csv.WriteLine("frame,tick,presentedTick,engineMs,cloudX,cloudY,cloudZ,coverage,visibleSystems");var clouds=app.GetComponentInChildren<WorldCloudLayer>();
                    for(int i=0;i<180;i++){yield return null;var systems=Shader.GetGlobalVectorArray("_AevoriaCloudSystems");var shapes=Shader.GetGlobalVectorArray("_AevoriaCloudShapes");csv.WriteLine(FormattableString.Invariant($"{i},{app.Clock.State.Tick},{clouds.PresentedTick:R},{Time.unscaledDeltaTime*1000:R},{systems[0].x:R},{systems[0].y:R},{systems[0].z:R},{shapes[0].y:R},{app.GetComponent<WorldEnvironment>().VisibleCloudSystems}"));if(i%60==0)yield return Capture("cloud-motion-"+i);}
                }
                app.Clock.Pause();File.WriteAllText(Path.Combine(output,"environment.json"),JsonUtility.ToJson(evidence,true));
            }
            Require(evidence.errors==0&&evidence.renders>500,"Native environment rendered without runtime errors");app.ReturnToMenu();yield return null;File.WriteAllText(Path.Combine(output,"live-status.txt"),"Completed environment review");Debug.Log("AEVORIA009_ENVIRONMENT_REVIEW_COMPLETE");Application.Quit();
        }
        private IEnumerator Boreal()
        {
            label="boreal";app.ReturnToMenu();yield return null;app.CreateWorld("Cold forest review","shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents,renderRadius:3,nearLodRadius:0,mediumLodRadius:1),new ClimateSettings(-3,1.3f,43));
            float end=Time.realtimeSinceStartup+600;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Require(app.WorldActive&&!app.Busy,"Cold forest world created");app.Clock.Pause();
            var settings=app.Settings.Copy();settings.RenderClouds=false;settings.RenderWeatherEffects=false;settings.AutosaveSeconds=0;app.ApplySettings(settings,false);
            var geo=app.Terrain.Geography;GeoPoint selected=default;int best=0;GeographicSample climate=default;
            for(int i=0;i<app.Ecology.Count;i++)
            {
                var region=app.Ecology.EnvironmentAt(i);var sample=geo.Sample(region.Center);if(sample.Biome!=Biome.BorealForest||sample.TemperatureCelsius>=2||sample.TemperatureCelsius< -6||app.Ecology.Forest(i)<.05)continue;
                int trees=0,cx=(int)Math.Floor(region.Center.X/16),cz=(int)Math.Floor(region.Center.Y/16);
                for(int z=cz-10;z<=cz+10;z++)for(int x=cx-10;x<=cx+10;x++)if(geo.Ecosystem.TryTree(x,z,out var tree)&&tree.Conifer)trees++;
                if(trees>best){best=trees;selected=region.Center;climate=sample;}
            }
            Require(best>=20,"Actual cold moist boreal region supports a conifer stand");app.Controller.SetView(selected,25,45,400);yield return Ready();yield return new WaitForSecondsRealtime(2);Require(app.Terrain.Vegetation.VisibleTrees>0,"Actual cold forest trees are rendered");yield return Capture("stand");
            File.WriteAllText(Path.Combine(output,"boreal-result.txt"),FormattableString.Invariant($"PASS: cold boreal stand at {selected.X:R},{selected.Y:R}; temperatureC={climate.TemperatureCelsius:R}; moisture={climate.Moisture:R}; conifersIn320mSquare={best}; renderedTrees={app.Terrain.Vegetation.VisibleTrees}; errors={evidence.errors}"));
            Require(evidence.errors==0,"Cold forest review has no runtime errors");app.ReturnToMenu();yield return null;Debug.Log("AEVORIA011_BOREAL_REVIEW_COMPLETE");
        }
        private IEnumerator Ready(){float end=Time.realtimeSinceStartup+180;while(!app.Terrain.IsReady&&Time.realtimeSinceStartup<end)yield return null;Require(app.Terrain.IsReady,"Environment view settled");yield return new WaitForSecondsRealtime(.5f);}
        private IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,label+"-"+name+".png"));yield return null;}
        private void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;Application.logMessageReceived-=Logged;}
    }
}
