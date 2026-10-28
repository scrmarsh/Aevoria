using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using Aevoria.World;
using Aevoria.Persistence;
using UnityEngine;
namespace Aevoria.Presentation
{
    [DefaultExecutionOrder(1000)]
    public sealed class Alpha012CharacterAudit:MonoBehaviour
    {
        private AevoriaBootstrap app;private string output;private bool cameraOverride,faceView;private float azimuth;private int errors,gripView;
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        private void Failure(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;errors++;File.AppendAllText(Path.Combine(output,"failures.txt"),message+"\n"+stack+"\n");Application.Quit(12);}
        private IEnumerator Start()
        {
            Require(AevoriaBootstrap.Argument("-aevoriaTestRoot")!=null,"Character review requires isolated saves");output=AevoriaBootstrap.Argument("-aevoriaOutput");Directory.CreateDirectory(output);Application.logMessageReceived+=Failure;Application.runInBackground=true;
            app=GetComponent<AevoriaBootstrap>();app.ExternalControl=true;yield return null;app.ReturnToMenu();yield return null;
            string compatibility=AevoriaBootstrap.Argument("-aevoriaCompatibilityWorld");
            if(compatibility==null)app.CreateWorld("Character remaster review","shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents,renderRadius:2,nearLodRadius:0,mediumLodRadius:1),new ClimateSettings());else app.LoadWorld(compatibility);
            yield return Operation();Require(app.WorldActive,"Created or loaded actual world");app.Clock.Pause();app.Explorer.AcceptInput=false;
            var dialogue=app.GetComponent<CharacterDialogue>();if(dialogue!=null)dialogue.enabled=false;
            var settings=app.Settings.Copy();settings.AutosaveSeconds=0;settings.RenderClouds=false;settings.RenderWeatherEffects=false;settings.MotionBlur=false;settings.VSync=0;app.ApplySettings(settings,false);
            string geography=WorldRecord.GeographyFingerprint(app.Terrain.Geography);bool entered=false;var geo=app.Terrain.Geography;
            for(int region=0;region<app.Ecology.Count&&!entered;region++)
            {
                var candidate=app.Ecology.EnvironmentAt(region);if(!candidate.Land||candidate.Height<30||candidate.Height>600||candidate.Biome!=Biome.Grassland)continue;bool dry=true;
                for(int z=-24;z<=24&&dry;z+=8)for(int x=-24;x<=24&&dry;x+=8){double east=candidate.Center.X+x,north=candidate.Center.Y+z;float h=geo.ElevationAt(east,north);if(geo.InlandSurfaces.LakeAt(east,north,out _)||geo.Hydrology.IsRiverWater(east,north,h)||h<geo.SeaLevel+8)dry=false;}
                if(dry)entered=app.Explorer.EnterAt(candidate.Center);
            }
            Require(entered,"Entered a dry gameplay review area");yield return Ready();yield return Settle();
            // Face the existing daylight, without modifying scene illumination.
            var sunward=RenderSettings.sun==null?Vector3.forward:-RenderSettings.sun.transform.forward;
            app.Living.Traveler.Yaw=Mathf.Atan2(sunward.x,sunward.z)*Mathf.Rad2Deg;
            File.WriteAllText(Path.Combine(output,"lighting.txt"),"Existing sun direction="+sunward+"; grassland gameplay site; no additional review lights.");
            var appearances=AevoriaBootstrap.HasFlag("-aevoriaDoctorReview")?new[]{CharacterProfiles.Doctor}:new[]{CharacterProfiles.Explorer,CharacterProfiles.Roman,CharacterProfiles.Doctor};
            foreach(string appearance in appearances)
            {
                Require(app.Explorer.SetAppearance(appearance),"Normal character selection: "+appearance);app.Living.Traveler.Tool=ToolKind.Pickaxe;yield return Settle();var rig=app.Explorer.Character;
                Require(rig.ToolHand==(appearance==CharacterProfiles.Doctor?rig.LeftHand:rig.RightHand),"Character-specific tool hand");Require(rig.ToolHand.Find("Equipped hand tool")!=null,"Actual equipped mesh uses the configured hand");
                if(appearance==CharacterProfiles.Doctor)Require(rig.Cane!=null&&rig.Cane.IsChildOf(rig.RightHand),"Doctor cane is in right hand");else Require(rig.Cane==null,"Other characters do not inherit the cane");
                cameraOverride=true;faceView=false;azimuth=0;yield return Capture(appearance+"-idle-front");azimuth=90;yield return Capture(appearance+"-idle-side");azimuth=180;yield return Capture(appearance+"-idle-back");
                if(appearance==CharacterProfiles.Doctor){app.Living.Traveler.Tool=ToolKind.Hands;yield return Settle();faceView=true;azimuth=0;yield return Capture("doctor-face-front");azimuth=90;yield return Capture("doctor-face-profile");faceView=false;app.Living.Traveler.Tool=ToolKind.Pickaxe;yield return Settle();azimuth=30;gripView=1;yield return Capture("doctor-left-tool-grip");gripView=2;yield return Capture("doctor-right-cane-grip");gripView=0;azimuth=0;}
                yield return Motion(appearance+"-walk",false,false,3);yield return Motion(appearance+"-run",true,false,2);
                yield return Motion(appearance+"-crouch",false,true,2);yield return Capture(appearance+"-crouch-side",true);
                if(appearance==CharacterProfiles.Doctor){var lod=rig.GetComponent<LODGroup>();Require(lod!=null,"Doctor distance meshes installed");for(int level=1;level<=2;level++){lod.ForceLOD(level);yield return Capture("doctor-crouch-lod"+level,true);}lod.ForceLOD(-1);}
                foreach(ToolKind tool in new[]{ToolKind.Shovel,ToolKind.Pickaxe,ToolKind.Axe,ToolKind.Stick,ToolKind.Crate})
                {
                    app.Living.Traveler.Tool=tool;yield return Settle();azimuth=35;Time.captureFramerate=60;app.Explorer.Interact(false);
                    using(var timing=new StreamWriter(Path.Combine(output,appearance+"-"+tool+"-strike.csv")))
                    {
                        timing.WriteLine("frame,actionProgress,state");
                        for(int sample=0;sample<28;sample++){yield return null;yield return new WaitForEndOfFrame();FinitePose();timing.WriteLine(FormattableString.Invariant($"{sample},{rig.ToolActionProgress:R},{rig.AnimationState}"));if(sample%2==0)ScreenCapture.CaptureScreenshot(Path.Combine(output,appearance+"-"+tool+"-strike-"+sample.ToString("D2")+".png"));}
                    }
                    Time.captureFramerate=0;
                    cameraOverride=false;app.Explorer.SetFirstPerson(true);yield return Capture(appearance+"-"+tool+"-first-person");app.Explorer.SetFirstPerson(false);cameraOverride=true;
                }
                app.Living.Traveler.Tool=ToolKind.Pickaxe;
                cameraOverride=false;Camera.main.fieldOfView=60;app.Living.Traveler.ReverseThirdPerson=false;app.Explorer.SetFirstPerson(false);yield return Capture(appearance+"-third-person");app.Living.Traveler.ReverseThirdPerson=true;yield return Capture(appearance+"-reverse-third-person");app.Explorer.SetFirstPerson(true);yield return Capture(appearance+"-first-person");app.Explorer.SetFirstPerson(false);
                yield return AirMovement(appearance);
            }
            yield return SlopeMovement();
            Require(geography==WorldRecord.GeographyFingerprint(app.Terrain.Geography),"Character changes preserve geography");Require(app.Living.Traveler.CharacterPreset==CharacterProfiles.Doctor,"Doctor selected without unlock");
            app.SaveWorld();yield return Operation();string id=app.Current.Id;app.ReturnToMenu();yield return null;yield return null;app.LoadWorld(id);yield return Operation();app.Clock.Pause();app.Explorer.AcceptInput=false;yield return Ready();
            Require(app.Living.Traveler.CharacterPreset==CharacterProfiles.Doctor&&app.Explorer.Character.Cane.IsChildOf(app.Explorer.Character.RightHand)&&app.Explorer.Character.ToolHand==app.Explorer.Character.LeftHand,"Actual reload retains Doctor equipment configuration");Require(geography==WorldRecord.GeographyFingerprint(app.Terrain.Geography),"Reload preserves geographic fingerprint");
            cameraOverride=true;faceView=false;azimuth=0;yield return Capture("doctor-reloaded");
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: actual character selection/equipment, crouch and locomotion capture, all tools, three camera perspectives, actual save/reload and unchanged geography. Visual quality requires separate image/sequence review. Errors="+errors+"\nFingerprint="+geography);
            cameraOverride=false;app.ReturnToMenu();yield return null;yield return null;Debug.Log("AEVORIA012_CHARACTER_REVIEW_COMPLETE");Application.Quit();
        }
        private IEnumerator AirMovement(string appearance)
        {
            cameraOverride=true;faceView=false;azimuth=90;yield return Settle();float start=app.Explorer.Position.y,peak=start;
            for(int i=0;i<55;i++){app.Explorer.StepMovement(Vector2.zero,false,false,i==0,Mathf.Min(.05f,Time.unscaledDeltaTime));yield return null;FinitePose();peak=Mathf.Max(peak,app.Explorer.Position.y);}
            Require(peak>start+.2f,"Actual jump: "+appearance);yield return Settle();Require(app.Explorer.SetFlight(true),"Flight enabled: "+appearance);
            for(int i=0;i<45;i++){app.Explorer.StepFlight(Vector2.zero,true,false,false,1/60f);yield return null;FinitePose();}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,appearance+"-flight.png"));Require(app.Explorer.SetFlight(false),"Return to gravity: "+appearance);
            bool falling=false;float end=Time.realtimeSinceStartup+30;
            while(!app.Explorer.Grounded&&Time.realtimeSinceStartup<end){app.Explorer.StepMovement(Vector2.zero,false,false,false,Mathf.Min(.05f,Time.unscaledDeltaTime));yield return null;FinitePose();falling|=app.Explorer.Character.AnimationState=="Freefall";}
            Require(falling&&app.Explorer.Grounded,"Freefall and landing: "+appearance);yield return Settle();
        }
        private IEnumerator SlopeMovement()
        {
            var geo=app.Terrain.Geography;bool entered=false;float rise=0;
            for(int region=0;region<app.Ecology.Count&&!entered;region++)
            {
                var e=app.Ecology.EnvironmentAt(region);if(!e.Land||e.Height<100||e.Height>1200)continue;double x=e.Center.X,z=e.Center.Y;
                float dx=(float)((LocalSolidField.RenderedGround(geo,x+3,z)-LocalSolidField.RenderedGround(geo,x-3,z))/6),dz=(float)((LocalSolidField.RenderedGround(geo,x,z+3)-LocalSolidField.RenderedGround(geo,x,z-3))/6);rise=Mathf.Sqrt(dx*dx+dz*dz);
                if(rise<.08f||rise>.30f||geo.InlandSurfaces.LakeAt(x,z,out _)||geo.Hydrology.IsRiverWater(x,z,(float)e.Height))continue;
                entered=app.Explorer.EnterAt(e.Center);if(entered)app.Living.Traveler.Yaw=Mathf.Atan2(dx,dz)*Mathf.Rad2Deg;
            }
            Require(entered,"Found real sloped gameplay terrain");yield return Ready();yield return Settle();File.WriteAllText(Path.Combine(output,"slope.txt"),"Actual terrain gradient="+rise.ToString("R",CultureInfo.InvariantCulture));
            yield return Motion("doctor-uphill-walk",false,false,4);app.Living.Traveler.Yaw+=180;yield return Motion("doctor-downhill-walk",false,false,4);
        }
        private IEnumerator Motion(string name,bool running,bool crouch,float seconds)
        {
            cameraOverride=true;faceView=false;float end=Time.realtimeSinceStartup+seconds;int frame=0;using(var csv=new StreamWriter(Path.Combine(output,name+".csv")))
            {
                csv.WriteLine("frame,x,y,z,leftFootY,rightFootY,caneError,state");
                while(Time.realtimeSinceStartup<end)
                {
                    azimuth=name.EndsWith("walk")?Mathf.Repeat(frame*.5f,190):90;
                    app.Explorer.StepMovement(Vector2.up,running,crouch,false,Mathf.Min(.05f,Time.unscaledDeltaTime));yield return null;
                    FinitePose();var rig=app.Explorer.Character;var left=rig.Skin.bones.First(b=>b.name=="Left ankle");var right=rig.Skin.bones.First(b=>b.name=="Right ankle");var p=app.Explorer.Position;
                    csv.WriteLine(FormattableString.Invariant($"{frame},{p.x:R},{p.y:R},{p.z:R},{left.position.y:R},{right.position.y:R},{rig.CaneContactError:R},{rig.AnimationState}"));
                    if(frame%12==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+"-"+frame.ToString("D4")+".png"));}frame++;
                }
            }
            yield return Settle();
        }
        private void FinitePose(){foreach(var bone in app.Explorer.Character.Skin.bones){var p=bone.position;var q=bone.rotation;Require(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)&&float.IsFinite(q.x)&&float.IsFinite(q.y)&&float.IsFinite(q.z)&&float.IsFinite(q.w),"Finite animated pose: "+bone.name);}}
        private IEnumerator Capture(string name,bool crouch=false){float end=Time.realtimeSinceStartup+.35f;while(Time.realtimeSinceStartup<end){app.Explorer.StepMovement(Vector2.zero,false,crouch,false,Mathf.Min(.05f,Time.unscaledDeltaTime));yield return null;}yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private IEnumerator Settle(){for(int i=0;i<45;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,Mathf.Min(.05f,Time.unscaledDeltaTime));yield return null;}}
        private IEnumerator Operation(){float end=Time.realtimeSinceStartup+900;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Require(!app.Busy,"World operation completed");}
        private IEnumerator Ready(){float end=Time.realtimeSinceStartup+180;while((!app.Terrain.IsReady||!app.Explorer.Ready)&&Time.realtimeSinceStartup<end)yield return null;Require(app.Terrain.IsReady&&app.Explorer.Ready,"Gameplay terrain/collision ready");}
        private void LateUpdate()
        {
            if(!cameraOverride||app?.Explorer?.Character==null||!app.WorldActive)return;var rig=app.Explorer.Character;var root=rig.transform;var camera=Camera.main;Vector3 target=gripView>0?(gripView==1?rig.ToolHand:rig.RightHand).position+Vector3.down*.025f:faceView?rig.Skin.bones.First(b=>b.name=="Head").position+Vector3.up*.025f:root.position+Vector3.up*.98f;
            Vector3 outward=Quaternion.AngleAxis(azimuth,Vector3.up)*root.forward;camera.transform.position=target+outward*(gripView>0?.45f:faceView?.85f:3.1f)+Vector3.up*(faceView||gripView>0?0:.08f);camera.transform.LookAt(target);camera.fieldOfView=gripView>0?34:faceView?29:45;camera.nearClipPlane=.025f;
        }
        private void OnDestroy(){Application.logMessageReceived-=Failure;}
    }
}
