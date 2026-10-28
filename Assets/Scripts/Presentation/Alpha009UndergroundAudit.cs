using System;
using System.Collections;
using System.IO;
using Aevoria.World;
using UnityEngine;
namespace Aevoria.Presentation
{
    // Explicit opt-in Windows-player evidence. Never uses the creator's library.
    public sealed class Alpha009UndergroundAudit:MonoBehaviour
    {
        private AevoriaBootstrap app;private string output;
        private static void Require(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
        private IEnumerator Start()
        {
            Require(AevoriaBootstrap.Argument("-aevoriaTestRoot")!=null,"Underground audit needs isolated saves");output=AevoriaBootstrap.Argument("-aevoriaOutput");Require(!string.IsNullOrEmpty(output),"Explicit evidence directory required");Directory.CreateDirectory(output);Application.logMessageReceived+=Logged;Application.runInBackground=true;
            app=GetComponent<AevoriaBootstrap>();app.ExternalControl=true;yield return null;
            string seed=AevoriaBootstrap.Argument("-aevoria011CaveSeed")??"shaping-continents";app.CreateWorld("Underground native review",seed,WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents,renderRadius:2,nearLodRadius:0,mediumLodRadius:1),new ClimateSettings());
            float end=Time.realtimeSinceStartup+600;GenerationPreview lastPreview=null;while(app.Busy&&Time.realtimeSinceStartup<end){var preview=app.PreparationPreview;if(preview!=null&&!ReferenceEquals(lastPreview,preview)){Require(preview.Seed==seed,"Loading preview belongs to requested world");lastPreview=preview;yield return Capture("loading-stage-"+app.PreparationStage);}yield return null;}Require(app.WorldActive&&!app.Busy,"World created");app.Clock.Pause();app.Controller.ExternalDriver=true;app.Explorer.AcceptInput=false;
            var settings=app.Settings.Copy();settings.AutosaveSeconds=0;settings.MotionBlur=false;settings.RenderClouds=false;app.ApplySettings(settings,false);
            var geo=app.Terrain.Geography;
            if(AevoriaBootstrap.HasFlag("-aevoria009CharacterOnly")){bool entered=false;GeoPoint location=default;for(int i=0;i<app.Ecology.Count&&!entered;i++)if(app.Ecology.EnvironmentAt(i).Land){location=app.Ecology.EnvironmentAt(i).Center;entered=app.Explorer.EnterAt(location);}Require(entered,"Character surface spawn");yield return CharacterReview(location);File.WriteAllText(Path.Combine(output,"character-result.txt"),"PASS: native character and camera review completed");app.ReturnToMenu();yield return null;Application.Quit();yield break;}
            CaveSystem system=null;for(int z=-16;z<=16&&system==null;z++)for(int x=-16;x<=16;x++){var candidate=geo.Caves.SystemAt(x,z);if(candidate.Active&&candidate.HillsideEntrance){system=candidate;break;}}Require(system!=null,"Hillside cave exists");
            app.Controller.SetView(new GeoPoint(system.Entrance.X,system.Entrance.Z),25,45,120);yield return Ready();yield return Capture("hillside-entrance");
            CavePassage portal=default;bool hasPortal=false;foreach(var p in system.Passages)if(Math.Abs(p.Radius-3.2)<.001&&Math.Abs(p.B.X-system.Entrance.X)<.001&&Math.Abs(p.B.Z-system.Entrance.Z)<.001){portal=p;hasPortal=true;break;}Require(hasPortal,"External hillside portal found");
            var traveler=app.Living.Traveler;traveler.HasPosition=true;traveler.Flying=true;traveler.X=portal.A.X;traveler.Y=portal.A.Y-.7;traveler.Z=portal.A.Z;traveler.VelocityX=traveler.VelocityY=traveler.VelocityZ=0;traveler.FirstPerson=true;traveler.Yaw=Math.Atan2(system.Entrance.X-portal.A.X,system.Entrance.Z-portal.A.Z)*Mathf.Rad2Deg;traveler.Pitch=5;traveler.Tool=ToolKind.Hands;Require(app.Explorer.EnterAt(new GeoPoint(traveler.X,traveler.Z),true),"Place capsule outside entrance");yield return Ready();yield return Capture("entrance-outside-player");var walkingSettings=app.Settings.Copy();walkingSettings.FlightSpeed=5;app.ApplySettings(walkingSettings,false);
            SolidPoint inside=default;foreach(var p in system.Passages)if(Math.Abs(p.A.X-system.Entrance.X)<.001&&Math.Abs(p.A.Z-system.Entrance.Z)<.001&&Math.Abs(p.Radius-3)<.001){inside=p.B;break;}
            foreach(var waypoint in new[]{system.Entrance,inside})
            {
                bool reached=false;for(int frame=0;frame<900;frame++){var position=app.Explorer.GeographicPosition;double dx=waypoint.X-position.X,dz=waypoint.Z-position.Z,dy=waypoint.Y-.7-position.Y;if(dx*dx+dz*dz+dy*dy<.25){reached=true;break;}traveler.Yaw=Math.Atan2(dx,dz)*Mathf.Rad2Deg;traveler.Pitch=dy<-.3?15:0;app.Explorer.StepFlight(dx*dx+dz*dz>.04?Vector2.up:Vector2.zero,dy>.2,dy<-.2,false,1/60f);yield return null;}Require(reached,"Actual controller traverses hillside entrance");
            }
            traveler.X=portal.A.X;traveler.Y=portal.A.Y-.7;traveler.Z=portal.A.Z;traveler.Flying=true;traveler.VelocityX=traveler.VelocityY=traveler.VelocityZ=0;
            Require(app.Explorer.EnterAt(new GeoPoint(traveler.X,traveler.Z),true),"Restore outside pose for grounded entrance traversal");yield return Ready();Require(app.Explorer.SetFlight(false),"Disable propulsion for walking entrance");
            using(var walking=new StreamWriter(Path.Combine(output,"entrance-walk.csv")))
            {
                walking.WriteLine("frame,x,y,z,grounded");
                foreach(var waypoint in new[]{system.Entrance,inside})
                {
                    bool reached=false;for(int frame=0;frame<900;frame++){var position=app.Explorer.GeographicPosition;double dx=waypoint.X-position.X,dz=waypoint.Z-position.Z;if(dx*dx+dz*dz<.64){reached=true;break;}traveler.Yaw=Math.Atan2(dx,dz)*Mathf.Rad2Deg;traveler.Pitch=8;app.Explorer.StepMovement(Vector2.up,false,false,false,1/60f);yield return null;position=app.Explorer.GeographicPosition;walking.WriteLine(FormattableString.Invariant($"{frame},{position.X:R},{position.Y:R},{position.Z:R},{app.Explorer.Grounded}"));}Require(reached,"Grounded traveler walks through the actual hillside entrance");
                }
            }
            yield return Capture("entrance-walk-inside");
            Require(LocalSolidField.RenderedGround(geo,traveler.X,traveler.Z)-traveler.Y>2.4,"Traveler moved from outside into geological volume");yield return Capture("entrance-inside-player");traveler.Flying=false;traveler.VelocityX=traveler.VelocityY=traveler.VelocityZ=0;
            var passage=system.Passages[0];var point=SolidPoint.Mix(passage.A,passage.B,.5);double low=point.Y,high=point.Y;while(app.Living.SolidDensity(point.X,low,point.Z)<0&&point.Y-low<100)low-=.5;Require(point.Y-low<100,"Find cave floor");for(int i=0;i<30;i++){double mid=(low+high)*.5;if(app.Living.SolidDensity(point.X,mid,point.Z)>=0)low=mid;else high=mid;}
            var state=app.Living.Traveler;state.HasPosition=true;state.X=point.X;state.Y=high+.10;state.Z=point.Z;state.Yaw=Math.Atan2(passage.B.X-passage.A.X,passage.B.Z-passage.A.Z)*Mathf.Rad2Deg;state.Pitch=0;state.Tool=ToolKind.Pickaxe;state.FirstPerson=true;
            Require(app.Explorer.EnterAt(new GeoPoint(state.X,state.Z),true),"Restore real underground pose");yield return Ready();
            using(var csv=new StreamWriter(Path.Combine(output,"cave-motion.csv")))
            {
                csv.WriteLine("frame,engineMs,x,y,z,grounded,chunks,vertices");float minimum=float.MaxValue,maximum=float.MinValue;
                for(int i=0;i<240;i++){app.Explorer.StepMovement(i<120?Vector2.zero:Vector2.up,false,false,false,1/60f);yield return null;var p=app.Explorer.GeographicPosition;if(i<120){minimum=Mathf.Min(minimum,(float)p.Y);maximum=Mathf.Max(maximum,(float)p.Y);}csv.WriteLine(FormattableString.Invariant($"{i},{Time.unscaledDeltaTime*1000:R},{p.X:R},{p.Y:R},{p.Z:R},{app.Explorer.Grounded},{app.Excavations.ResidentChunks},{app.Excavations.InstalledVertices}"));if(i==90||i==180)yield return Capture("cave-walk-"+i);}
                Require(maximum-minimum<.08f,"Standing cave support is stable");
            }
            yield return Ready();double selectedYaw=state.Yaw,selectedPitch=0;float nearest=4;bool found=false;
            foreach(float pitch in new[]{0f,30f,60f,-30f})for(int a=0;a<36;a++){state.Yaw=a*10;state.Pitch=pitch;if(Physics.Raycast(app.Explorer.AimRay,out var candidate,nearest,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&app.Explorer.IsTerrainCollider(candidate.collider)&&app.Explorer.DigMaterial(candidate)==ResourceKind.Stone){found=true;nearest=candidate.distance;selectedYaw=state.Yaw;selectedPitch=pitch;}}
            Require(found,"Nearby stone wall within mining reach");state.Yaw=selectedYaw;state.Pitch=selectedPitch;yield return null;
            var target=GetComponent<ExcavationTarget>();target.Refresh();Require(target.Visible&&target.Valid,"Valid surface-conforming target visible");var centre=target.Centre;double radius=target.Radius;Require(Math.Abs(radius-LivingWorldState.ExcavationRadius(ResourceKind.Stone))<.001,"Displayed radius matches stone excavation");yield return Capture("wall-target");
            state.Tool=ToolKind.Shovel;target.Refresh();Require(target.Visible&&!target.Valid,"Wrong tool gives invalid wall target");yield return Capture("wall-invalid-target");state.Tool=ToolKind.Pickaxe;
            Require(app.Explorer.DigAt(centre,ResourceKind.Stone),"Remove real cave wall");yield return Ready();Require(app.Living.SolidDensity(centre.X,centre.Y,centre.Z)<-.4,"Removed centre is empty");yield return Capture("wall-excavated");
            var saved=app.Explorer.GeographicPosition;app.SaveWorld(newName:"Native underground persistence");end=Time.realtimeSinceStartup+120;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Require(app.Current!=null,"Save completed");string id=app.Current.Id;app.ReturnToMenu();yield return null;yield return null;app.LoadWorld(id);end=Time.realtimeSinceStartup+600;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Require(app.WorldActive&&!app.Busy,"Reload completed");app.Explorer.AcceptInput=false;app.Controller.ExternalDriver=true;yield return Ready();var loaded=app.Explorer.GeographicPosition;Require(Math.Abs(saved.X-loaded.X)<.01&&Math.Abs(saved.Y-loaded.Y)<.01&&Math.Abs(saved.Z-loaded.Z)<.01,"Exact underground pose retained");Require(app.Living.SolidDensity(centre.X,centre.Y,centre.Z)<-.4,"Wall cut retained");yield return Capture("underground-reloaded");
            yield return CharacterReview(new GeoPoint(system.Entrance.X,system.Entrance.Z));
            File.WriteAllText(Path.Combine(output,"result.txt"),FormattableString.Invariant($"PASS\nGenerator {ProceduralGeography.GeneratorVersion}\nEntrance {system.Entrance.X:R},{system.Entrance.Y:R},{system.Entrance.Z:R}\nSaved {saved.X:R},{saved.Y:R},{saved.Z:R}\nTarget radius {radius:R}\n"));Debug.Log("AEVORIA009_UNDERGROUND_REVIEW_COMPLETE");app.ReturnToMenu();yield return null;Application.Quit();
        }
        private IEnumerator CharacterReview(GeoPoint location)
        {
            app.Explorer.Exit();Require(app.Explorer.EnterAt(location),"Surface character review spawn");yield return Ready();app.Explorer.SetFirstPerson(false);app.Living.Traveler.ReverseThirdPerson=true;for(int i=0;i<90;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}yield return Capture("roman-front-idle");app.Living.Traveler.ReverseThirdPerson=false;yield return null;yield return Capture("roman-rear-idle");
            for(int i=0;i<40;i++){app.Explorer.StepMovement(Vector2.up,false,false,false,1/60f);yield return null;if(i==20)yield return Capture("roman-walking");}for(int i=0;i<30;i++){app.Explorer.StepMovement(Vector2.up,true,false,false,1/60f);yield return null;if(i==15)yield return Capture("roman-running");}yield return Ready();app.Explorer.StepMovement(Vector2.zero,false,false,true,1/60f);for(int i=0;i<15;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}yield return Capture("roman-jumping");Require(app.Explorer.SetFlight(true),"Flight character review");for(int i=0;i<50;i++){app.Explorer.StepFlight(Vector2.zero,true,false,false,1/60f);yield return null;}app.Living.Traveler.ReverseThirdPerson=true;yield return null;yield return Capture("roman-flying");Require(app.Explorer.SetFlight(false),"Freefall character review");for(int i=0;i<20;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}yield return Capture("roman-freefall");
            for(int i=0;i<900&&!app.Explorer.Grounded;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}Require(app.Explorer.Grounded,"Character lands on actual support");for(int i=0;i<8;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}yield return Capture("roman-landing");app.Living.Traveler.Pitch=65;yield return null;Require(Physics.Raycast(app.Explorer.AimRay,out var ground,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&app.Explorer.IsTerrainCollider(ground.collider),"Character aims at actual excavation surface");var kind=app.Explorer.DigMaterial(ground);app.Living.Traveler.Tool=kind==ResourceKind.Stone?ToolKind.Pickaxe:ToolKind.Shovel;Require(app.Explorer.DigAt(new SolidPoint((double)ground.point.x+WorldOrigin.Offset.x,ground.point.y,(double)ground.point.z+WorldOrigin.Offset.z),kind),"Character performs a real excavation");for(int i=0;i<10;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}yield return Capture("roman-digging");yield return Ready();
        }
        private IEnumerator Ready(){app.Excavations.RequestNearTraveler();yield return null;float end=Time.realtimeSinceStartup+180;while((!app.Terrain.IsReady||!app.Explorer.Ready||!app.Excavations.Ready)&&Time.realtimeSinceStartup<end)yield return null;Require(app.Terrain.IsReady&&app.Explorer.Ready&&app.Excavations.Ready,"World collision, cave geometry and rendering settled");yield return new WaitForSecondsRealtime(.5f);}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private void Logged(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;File.AppendAllText(Path.Combine(output,"failures.txt"),message+"\n"+stack+"\n");Application.Quit(4);}
        private void OnDestroy(){Application.logMessageReceived-=Logged;}
    }
}
