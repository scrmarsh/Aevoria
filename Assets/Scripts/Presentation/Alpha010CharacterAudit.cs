using System;
using System.Collections;
using System.IO;
using Aevoria.Persistence;
using Aevoria.World;
using Aevoria.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Aevoria.Presentation
{
    public sealed class Alpha010CharacterAudit:MonoBehaviour
    {
        private AevoriaBootstrap app;private string output;private int errors;
        private void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        private void Error(string message,string stack,LogType type){if(type!=LogType.Exception&&type!=LogType.Error)return;errors++;File.AppendAllText(Path.Combine(output,"exceptions.txt"),message+"\n"+stack);Application.Quit(8);}
        private IEnumerator Start()
        {
            Application.runInBackground=true;
            output=AevoriaBootstrap.Argument("-aevoriaOutput");Directory.CreateDirectory(output);Application.logMessageReceived+=Error;app=FindFirstObjectByType<AevoriaBootstrap>();yield return null;app.ReturnToMenu();yield return null;yield return null;
            app.CreateWorld("Character playable audit","shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents,renderRadius:2,nearLodRadius:0,mediumLodRadius:1),new ClimateSettings());yield return Operation();Require(app.WorldActive,"Created actual world");app.Clock.Pause();app.ExternalControl=true;app.Explorer.AcceptInput=false;
            var first=app.Society.Settlements[0];string before=SocietyAuthorityHash();Require(app.Explorer.EnterAt(new GeoPoint(first.X+100,first.Z+100)),"Entered real exploration");float timeout=Time.realtimeSinceStartup+180;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<timeout)yield return null;Require(app.Explorer.Ready,"Exploration colliders ready");
            for(int i=0;i<90;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}Require(app.Explorer.Grounded,"Traveler settled on actual collision before portraits");
            app.Living.Traveler.FirstPerson=false;app.Living.Traveler.ReverseThirdPerson=true;app.Living.Traveler.Pitch=0;app.Living.Traveler.Tool=ToolKind.Hands;
            foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){var direction=-light.transform.forward;app.Living.Traveler.Yaw=Math.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;break;}
            Require(app.Explorer.SetAppearance(CharacterProfiles.Explorer),"Explorer selection");yield return Capture("explorer-front");Require(app.Explorer.SetAppearance(CharacterProfiles.Roman),"Roman selection");yield return Capture("roman-front");
            Require(!app.Settings.DoctorUnlocked,"Fresh settings need no unlock flag");Require(app.Explorer.SetAppearance(CharacterProfiles.Doctor),"Doctor is available through normal selection");yield return Capture("doctor-front");
            for(int i=0;i<90;i++){app.Explorer.StepMovement(Vector2.up,false,false,false,1/60f);yield return null;}yield return Capture("doctor-walking",Vector2.up);
            Require(app.Explorer.SetFlight(true),"Doctor enters flight");for(int i=0;i<90;i++){app.Explorer.StepFlight(Vector2.zero,true,false,false,1/60f);yield return null;}yield return Capture("doctor-flight");Require(app.Explorer.SetFlight(false),"Doctor returns to gravity");for(int i=0;i<100;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}yield return Capture("doctor-freefall");
            timeout=Time.realtimeSinceStartup+60;while(!app.Explorer.Grounded&&Time.realtimeSinceStartup<timeout){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}
            var dialogue=app.GetComponent<CharacterDialogue>();dialogue.Speak(0);yield return Capture("doctor-subtitle");Require(before==SocietyAuthorityHash(),"Character changes leave society unchanged");
            app.SaveWorld();yield return Operation();string id=app.Current.Id;app.ReturnToMenu();yield return null;yield return null;Require(FindObjectsByType<CharacterDialogue>(FindObjectsSortMode.None).Length==0,"Character dialogue cleanup");app.LoadWorld(id);yield return Operation();app.Clock.Pause();Require(app.Living.Traveler.CharacterPreset==CharacterProfiles.Doctor&&app.Explorer.Character.Appearance==CharacterAppearance.Doctor,"Doctor survives actual archive reload");Require(!app.Settings.DoctorUnlocked,"Doctor selection requires no unlock flag after reload");yield return Capture("doctor-reloaded");
            yield return InterfaceInteraction();yield return BoulderInteraction();
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":true,\"runtimeErrors\":"+errors+",\"societyUnchanged\":true,\"selectedCharacterReloaded\":true,\"dialogueCleanedUp\":true,\"boulderMiningReloaded\":true}");Application.Quit();
        }
        private string SocietyAuthorityHash()
        {
            var snapshot=app.Society.Capture();foreach(var settlement in snapshot.Settlements)foreach(var resident in settlement.Residents){resident.HasPosition=false;resident.X=resident.Y=resident.Z=0;}
            return ArchiveCodec.Hash(ArchiveCodec.Encode(snapshot));
        }
        private IEnumerator KeyFrame(params Key[] keys)
        {InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));yield return null;yield return null;}
        private IEnumerator InterfaceInteraction()
        {
            app.Menu.Show(ApplicationPage.World);app.Explorer.AcceptInput=true;var before=app.Explorer.Position;var console=app.GetComponent<CommandConsole>();
            yield return KeyFrame(Key.Slash);yield return KeyFrame();Require(console.Open&&GameInput.Blocked,"Slash opens focused console and blocks gameplay");
            for(int i=0;i<12;i++)yield return KeyFrame(Key.W);yield return KeyFrame();Require(Vector3.Distance(before,app.Explorer.Position)<.05f,"Text entry blocks physical traveler movement");
            yield return Capture("command-console");yield return KeyFrame(Key.Escape);yield return KeyFrame();Require(!console.Open&&app.Menu.Page==ApplicationPage.World,"Escape closes command entry without opening pause");
            app.Explorer.AcceptInput=false;var settings=app.Settings.Copy();var world=app.Terrain.Geography;settings.Units=UnitSystem.Imperial;app.ApplySettings(settings);yield return Capture("imperial-hud");Require(ReferenceEquals(world,app.Terrain.Geography),"Changing units leaves generated geography intact");
            app.Menu.Show(ApplicationPage.Pause);yield return Capture("escape-menu");app.Menu.Show(ApplicationPage.Bindings);yield return Capture("key-bindings");app.Menu.Show(ApplicationPage.Characters);yield return Capture("doctor-normal-selection");app.Menu.Show(ApplicationPage.World);
            settings.Units=UnitSystem.Metric;app.ApplySettings(settings);File.WriteAllText(Path.Combine(output,"interface-result.txt"),"PASS: native slash entry, movement suppression, Escape close, live imperial settings without regeneration, normal Doctor menu. Binding persistence/conflicts separately verified by Editor tests.");
        }
        private IEnumerator BoulderInteraction()
        {
            app.ExternalControl=true;app.Explorer.AcceptInput=false;app.Explorer.Exit();var geo=app.Terrain.Geography;NaturalSite selected=default;bool found=false;
            foreach(var cluster in geo.Mountains.Clusters)
            {
                int cx=(int)(cluster.X/32),cz=(int)(cluster.Z/32);
                for(int z=cz-128;z<=cz+128&&!found;z++)for(int x=cx-128;x<=cx+128&&!found;x++)
                    if(LivingWorldState.Hash(x,z,43)<.004&&app.Living.TryNatural(x,z,out var site)&&site.Kind==NaturalKind.Boulder){selected=site;found=true;}
                if(found)break;
            }
            Require(found,"An actual natural boulder exists");app.Controller.SetView(selected.Position,25,45,90);
            float timeout=Time.realtimeSinceStartup+180;MeshCollider rock=null;
            while(rock==null&&Time.realtimeSinceStartup<timeout){foreach(var collider in FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))if(collider.name==selected.Id){rock=collider;break;}yield return null;}
            Require(rock!=null,"Visible boulder has matching physical collision");Physics.SyncTransforms();var bounds=rock.bounds;bool aimed=false;
            for(int side=0;side<8&&!aimed;side++)
            {
                float angle=side*Mathf.PI/4;var outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var eye=bounds.center+outward*(Mathf.Max(bounds.extents.x,bounds.extents.z)+1.2f);
                var geographic=WorldOrigin.Geographic(eye);var state=app.Living.Traveler;state.HasPosition=true;state.Flying=true;state.X=geographic.x;state.Z=geographic.z;state.Y=Math.Max(geo.ElevationAt(state.X,state.Z)+.4,eye.y-1.65);
                if(!app.Explorer.EnterAt(new GeoPoint(state.X,state.Z),true))continue;timeout=Time.realtimeSinceStartup+180;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<timeout)yield return null;Require(app.Explorer.Ready,"Boulder approach collision ready");
                var aim=bounds.center-WorldOrigin.Relative(app.Explorer.Position+Vector3.up*1.65f);state.Yaw=Math.Atan2(aim.x,aim.z)*Mathf.Rad2Deg;state.Pitch=-Math.Atan2(aim.y,new Vector2(aim.x,aim.z).magnitude)*Mathf.Rad2Deg;
                Physics.SyncTransforms();aimed=Physics.Raycast(app.Explorer.AimRay,out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider==rock;
            }
            Require(aimed,"Actual traveler aim hits the visible boulder within interaction range");app.Living.Traveler.FirstPerson=true;app.Living.Traveler.ReverseThirdPerson=false;yield return Capture("boulder-before-mining");int stoneBefore=app.Living.Traveler.Stone;
            for(int strike=0;strike<20&&app.Living.Available(selected.Id,60,app.Clock.State.Tick,false)>0;strike++){app.Living.Traveler.Tool=ToolKind.Pickaxe;app.Explorer.Interact(false);yield return null;}
            Require(app.Living.Available(selected.Id,60,app.Clock.State.Tick,false)==0,"Actual Interact depletes the rock");Require(app.Living.Traveler.Stone-stoneBefore==60,"All mined stone enters the saved inventory");
            yield return Capture("boulder-depleted");app.SaveWorld();yield return Operation();string id=app.Current.Id;app.ReturnToMenu();yield return null;yield return null;app.LoadWorld(id);yield return Operation();app.Clock.Pause();app.Explorer.AcceptInput=false;
            Require(app.Living.Available(selected.Id,60,app.Clock.State.Tick,false)==0,"Boulder depletion persists across actual reload");Require(app.Living.Traveler.Stone-stoneBefore==60,"Mined inventory persists across reload");
            for(int i=0;i<60;i++)yield return null;foreach(var collider in FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))Require(collider.name!=selected.Id,"Depleted boulder collision does not respawn");yield return Capture("boulder-reloaded");
            File.WriteAllText(Path.Combine(output,"boulder-result.txt"),"PASS: visible collider, actual traveler ray/Interact, 60 stone, depletion, actual save/reload, no collider respawn.");
        }
        private int operationIndex;
        private IEnumerator Operation()
        {
            int operation=++operationIndex,frame=0;string previous=null;float timeout=Time.realtimeSinceStartup+900;
            while(app.Busy&&Time.realtimeSinceStartup<timeout)
            {
                var preview=app.PreparationPreview;if(preview!=null&&preview.Stage!=previous&&app.Menu.Page==ApplicationPage.Loading)
                {previous=preview.Stage;string name="loading-"+operation+"-"+(++frame).ToString("00");File.AppendAllText(Path.Combine(output,"loading-stages.txt"),name+" "+preview.Stage+"\n");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
                yield return null;
            }
            Require(!app.Busy,"World operation completed");
        }
        private IEnumerator Capture(string name,Vector2 movement=default){float end=Time.realtimeSinceStartup+.6f;while(Time.realtimeSinceStartup<end){if(app.Explorer.Flying)app.Explorer.StepFlight(Vector2.zero,false,false,false,Mathf.Min(.05f,Time.unscaledDeltaTime));else app.Explorer.StepMovement(movement,false,false,false,Mathf.Min(.05f,Time.unscaledDeltaTime));yield return null;}yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private void OnDestroy(){Application.logMessageReceived-=Error;}
    }
}
