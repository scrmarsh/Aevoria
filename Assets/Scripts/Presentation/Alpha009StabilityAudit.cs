using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aevoria.Presentation
{
    // Opt-in motion evidence; never writes into the normal world library.
    public sealed class Alpha009StabilityAudit:MonoBehaviour
    {
        private AevoriaBootstrap app;private string output;private int renders,errors;private StreamWriter motion;
        private void Rendered(ScriptableRenderContext context,Camera view){if(view.CompareTag("MainCamera"))renders++;}
        private void Logged(string condition,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)errors++;}
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        private IEnumerator Start()
        {
            Require(AevoriaBootstrap.Argument("-aevoriaTestRoot")!=null,"Audit requires isolated saves");
            output=AevoriaBootstrap.Argument("-aevoriaOutput");Require(!string.IsNullOrEmpty(output),"Audit requires output");Directory.CreateDirectory(output);
            app=GetComponent<AevoriaBootstrap>();app.ExternalControl=true;Application.runInBackground=true;
            RenderPipelineManager.endCameraRendering+=Rendered;Application.logMessageReceived+=Logged;
            yield return null;
            var size=AevoriaBootstrap.HasFlag("-aevoria009Colossal")?WorldSize.Colossal:WorldSize.Small;
            bool surfaces=AevoriaBootstrap.HasFlag("-aevoria009SurfaceProbe");
            app.CreateWorld("Natural world stability","ecosystem-005",size,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1),surfaces?new ClimateSettings(-20,1,65):new ClimateSettings());
            float end=Time.realtimeSinceStartup+240;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;
            Require(app.WorldActive&&!app.Busy,"World creation");app.Clock.Pause();app.Controller.ExternalDriver=true;
            var settings=app.Settings.Copy();settings.AutosaveSeconds=0;settings.CloudQuality=0;settings.RenderClouds=false;app.ApplySettings(settings,false);app.Explorer.AcceptInput=false;
            if(surfaces){yield return Surfaces();yield break;}
            bool entered=false;for(int i=0;i<app.Ecology.Count&&!entered;i++)if(app.Ecology.EnvironmentAt(i).Land)entered=app.Explorer.EnterAt(app.Ecology.EnvironmentAt(i).Center);
            Require(entered,"Safe supported spawn");var initialSpawn=new GeoPoint(app.Living.Traveler.X,app.Living.Traveler.Z);end=Time.realtimeSinceStartup+90;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Require(app.Explorer.Ready,"Installed collision");
            motion=new StreamWriter(Path.Combine(output,"motion.csv"));motion.WriteLine("scenario,frame,engineMs,x,y,z,cameraLocalX,cameraLocalY,cameraLocalZ,verticalSpeed,flying,renderCallbacks,originX,originZ");
            foreach(bool first in new[]{true,false})
            {
                app.Explorer.SetFirstPerson(first);string prefix=first?"first":"third";
                for(int i=0;i<180;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;Record(prefix+"-standing",i);}
                for(int i=0;i<180;i++){app.Explorer.StepMovement(Vector2.up,false,false,false,1/60f);yield return null;Record(prefix+"-walking",i);}
                Require(app.Explorer.SetFlight(true),"Enable flight");settings.FlightSpeed=500;app.ApplySettings(settings,false);
                for(int i=0;i<240;i++){app.Explorer.StepFlight(new Vector2(Mathf.Sin(i*.03f),Mathf.Cos(i*.03f)),i<90,false,true,1/60f);yield return null;Record(prefix+"-maximum-flight",i);}
                Vector3 airborne=app.Explorer.Position;Require(app.Explorer.SetFlight(false),"Disable flight");Require(app.Explorer.Position==airborne,"Freefall retains exact position");
                for(int i=0;i<180;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;Record(prefix+"-freefall",i);}
                Require(app.Explorer.VerticalSpeed<0,"Gravity applies after powered ascent momentum");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,prefix+"-freefall.png"));yield return new WaitForEndOfFrame();
                if(first)
                {
                    var pose=app.Explorer.GeographicPosition;float speed=app.Explorer.VerticalSpeed;app.SaveWorld();end=Time.realtimeSinceStartup+90;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;
                    string id=app.Current.Id;app.ReturnToMenu();yield return null;yield return null;app.LoadWorld(id);end=Time.realtimeSinceStartup+240;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;
                    Require(app.WorldActive&&!app.Busy,"Airborne archive reload");app.Clock.Pause();app.Controller.ExternalDriver=true;app.Explorer.AcceptInput=false;
                    var restored=app.Explorer.GeographicPosition;Require(Math.Abs(restored.X-pose.X)<.002&&Math.Abs(restored.Y-pose.Y)<.002&&Math.Abs(restored.Z-pose.Z)<.002,"Exact airborne pose round-trip");Require(Math.Abs(app.Explorer.VerticalSpeed-speed)<.001,"Airborne momentum round-trip");
                }
                for(int i=0;i<1200&&!app.Explorer.Grounded;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;Record(prefix+"-landing",i);}
                Require(app.Explorer.Grounded,"Freefall lands on real collision");
                // Recovery is explicit, separate from disabling flight.
                app.Explorer.Exit();Require(app.Explorer.EnterAt(initialSpawn),"Explicit recovery for next scenario");
                end=Time.realtimeSinceStartup+90;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;
            }
            motion.Dispose();motion=null;Require(errors==0,"No runtime errors");Require(renders>1000,"Actual main-camera rendering");
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"phase\":\"1-partial\",\"freefall\":true,\"renders\":"+renders+",\"errors\":"+errors+",\"remaining\":\"Large-coordinate precision, snow and excavation visual diagnosis; not phase acceptance\"}");
            app.ReturnToMenu();yield return null;Debug.Log("AEVORIA009_STABILITY_AUDIT_PASS");Application.Quit();
        }
        private void Record(string scenario,int frame)
        {
            var p=app.Explorer.GeographicPosition;Vector3 c=Camera.main.transform.position;
            motion.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:F5},{3:R},{4:R},{5:R},{6:R},{7:R},{8:R},{9:R},{10},{11},{12:R},{13:R}",scenario,frame,Time.unscaledDeltaTime*1000,p.X,p.Y,p.Z,c.x,c.y,c.z,app.Explorer.VerticalSpeed,app.Explorer.Flying,renders,WorldOrigin.Offset.x,WorldOrigin.Offset.z));
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;
        }
        private IEnumerator Settle()
        {
            float end=Time.realtimeSinceStartup+90;while(!app.Terrain.IsReady&&Time.realtimeSinceStartup<end)yield return null;
            Require(app.Terrain.IsReady,"Surface probe terrain readiness");yield return new WaitForSecondsRealtime(2);
        }
        private IEnumerator Surfaces()
        {
            var geo=app.Terrain.Geography;var hydro=geo.Hydrology;int peak=0;for(int i=1;i<hydro.Ground.Length;i++)if(hydro.Ground[i]>hydro.Ground[peak])peak=i;
            app.Controller.SetView(new GeoPoint(hydro.X(peak),hydro.Y(peak)),25,55,900);yield return Settle();
            foreach(int mode in new[]{0,1,2,3,6}){Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",mode);yield return Capture("snow-diagnostic-"+mode);}Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",0);
            var sun=RenderSettings.sun;var shadows=sun.shadows;sun.shadows=LightShadows.None;yield return Capture("snow-without-direct-shadows");sun.shadows=shadows;
            app.Terrain.Vegetation.Visible=false;app.Details.enabled=false;yield return Capture("snow-without-natural-objects");
            for(int i=0;i<12;i++){app.Controller.SetView(new GeoPoint(hydro.X(peak),hydro.Y(peak)),25+i*.75f,55,900);yield return Capture("snow-motion-"+i.ToString("D2"));}
            app.Terrain.Vegetation.Visible=true;app.Details.enabled=true;
            bool dug=false;double x=0,z=0;app.Living.Traveler.Tool=ToolKind.Shovel;
            for(int i=0;i<app.Ecology.Count&&!dug;i++){var p=app.Ecology.EnvironmentAt(i).Center;x=Math.Round(p.X/32)*32;z=Math.Round(p.Y/32)*32;float h=geo.ElevationAt(x,z);if(hydro.IsLakeWater(x,z,h)||hydro.IsRiverWater(x,z,h))continue;dug=app.Living.Dig(x,h,z,ResourceKind.Soil,out _);}
            Require(dug,"Surface probe excavation site");
            for(int i=0;i<10;i++){double dx=Math.Sin(i*2.4)*.7,dz=Math.Cos(i*2.4)*.7;app.Living.Dig(x+dx,geo.ElevationAt(x+dx,z+dz)-.3,z+dz,ResourceKind.Soil,out _);}
            app.Controller.SetView(new GeoPoint(x,z),25,65,8);yield return Settle();float end=Time.realtimeSinceStartup+90;while(!app.Excavations.Ready&&Time.realtimeSinceStartup<end)yield return null;Require(app.Excavations.InstalledRevision>0,"Installed volumetric surface");
            foreach(int mode in new[]{0,1,2,3,6}){Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",mode);yield return Capture("excavation-diagnostic-"+mode);}Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",0);
            sun.shadows=LightShadows.None;yield return Capture("excavation-without-direct-shadows");sun.shadows=shadows;
            for(int i=0;i<12;i++){app.Controller.SetView(new GeoPoint(x,z),25+i*.75f,65,8);yield return Capture("excavation-motion-"+i.ToString("D2"));}
            Require(errors==0,"No surface probe runtime errors");File.WriteAllText(Path.Combine(output,"surfaces.json"),"{\"snowPeakX\":"+hydro.X(peak).ToString(CultureInfo.InvariantCulture)+",\"snowPeakZ\":"+hydro.Y(peak).ToString(CultureInfo.InvariantCulture)+",\"excavationX\":"+x.ToString(CultureInfo.InvariantCulture)+",\"excavationZ\":"+z.ToString(CultureInfo.InvariantCulture)+",\"renders\":"+renders+"}");
            app.ReturnToMenu();yield return null;Debug.Log("AEVORIA009_SURFACE_PROBE_COMPLETE");Application.Quit();
        }
        private void OnDestroy(){motion?.Dispose();RenderPipelineManager.endCameraRendering-=Rendered;Application.logMessageReceived-=Logged;}
    }
}
