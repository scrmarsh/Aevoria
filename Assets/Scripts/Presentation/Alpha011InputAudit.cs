using System;
using System.Collections;
using System.IO;
using Aevoria.Core;
using Aevoria.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Aevoria.Presentation
{
    public sealed class Alpha011InputAudit:MonoBehaviour
    {
        private AevoriaBootstrap app;private string output;
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        private void Logged(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;File.AppendAllText(Path.Combine(output,"input-failures.txt"),message+"\n"+stack+"\n");Application.Quit(9);}
        private IEnumerator Keys(params Key[] keys){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));yield return null;yield return null;}
        private IEnumerator Wheel(float y){InputSystem.QueueStateEvent(Mouse.current,new MouseState{scroll=new Vector2(0,y)});yield return null;yield return null;}
        private IEnumerator Start()
        {
            Require(AevoriaBootstrap.Argument("-aevoriaTestRoot")!=null,"Input review requires isolated settings and saves");output=AevoriaBootstrap.Argument("-aevoriaOutput");Directory.CreateDirectory(output);Application.logMessageReceived+=Logged;Application.runInBackground=true;app=GetComponent<AevoriaBootstrap>();app.ExternalControl=true;yield return null;
            if(AevoriaBootstrap.HasFlag("-aevoria011InputReload"))
            {
                Require(InputBindings.Input(app.Settings.Bindings,GameAction.Forward)=="I","Remapped key survives a real process restart");Require(app.Settings.Units==UnitSystem.Imperial,"Measurement preference survives a real process restart");
                app.Menu.Show(ApplicationPage.Controls);app.Menu.Show(ApplicationPage.Bindings);app.Menu.RestoreBindingDefaults();app.Menu.ApplyBindings();Require(InputBindings.Input(app.Settings.Bindings,GameAction.Forward)=="W","Restore Defaults restores the real movement binding");Require(app.Settings.Units==UnitSystem.Imperial,"Binding defaults preserve unrelated preferences");
                File.WriteAllText(Path.Combine(output,"input-restart-result.txt"),"PASS: actual process restart restores custom binding and Imperial preference; Restore Defaults restores bindings only.");Debug.Log("AEVORIA011_INPUT_REVIEW_COMPLETE");Application.Quit();yield break;
            }
            app.ReturnToMenu();yield return null;app.CreateWorld("Input context review","shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents,renderRadius:2,nearLodRadius:0,mediumLodRadius:1),new ClimateSettings());yield return Operation();app.Clock.Pause();Require(app.WorldActive,"Input world created");var geography=app.Terrain.Geography;
            app.Menu.Show(ApplicationPage.Controls);app.Menu.Show(ApplicationPage.Bindings);app.Menu.BeginBindingCapture(GameAction.Forward);yield return null;yield return null;yield return Keys(Key.S);yield return Keys();
            Require(app.Menu.CapturingBinding==null&&app.Menu.BindingNotice.Contains("already assigned"),"Actual capture detects overlapping input conflicts");Require(InputBindings.Input(app.Settings.Bindings,GameAction.Forward)=="W","A conflict leaves saved controls intact");yield return Capture("binding-conflict");
            app.Menu.BeginBindingCapture(GameAction.Forward);yield return null;yield return null;yield return Keys(Key.Escape);yield return Keys();Require(app.Menu.CapturingBinding==null&&app.Menu.Page==ApplicationPage.Bindings,"Escape cancels capture while preserving the menu");
            app.Menu.BeginBindingCapture(GameAction.Forward);yield return null;yield return null;yield return Keys(Key.I);yield return Keys();app.Menu.ApplyBindings();Require(InputBindings.Input(app.Settings.Bindings,GameAction.Forward)=="I","Captured key is applied to live controls");app.Menu.Show(ApplicationPage.World);yield return null;
            app.Controller.ExternalDriver=false;Require(Application.isFocused,"Native input review window must have focus");var before=app.Controller.Focus;yield return Keys(Key.I);float until=Time.realtimeSinceStartup+.5f;while(Time.realtimeSinceStartup<until)yield return null;yield return Keys();Require(Vector3.Distance(before,app.Controller.Focus)>1,"Remapped key moves the actual world camera");app.Controller.ExternalDriver=true;
            var settings=app.Settings.Copy();settings.Units=UnitSystem.Imperial;app.ApplySettings(settings);Require(ReferenceEquals(geography,app.Terrain.Geography),"Preference changes do not rebuild geographic authority");
            var settlement=app.Society.Settlements[0];app.Explorer.AcceptInput=false;Require(app.Explorer.EnterAt(new GeoPoint(settlement.X+100,settlement.Z+100)),"Enter actual exploration");yield return Ready();
            for(int frame=0;frame<100;frame++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}Require(app.Explorer.Grounded,"Actual traveler is grounded before wheel test");app.Explorer.AcceptInput=true;app.Living.Traveler.Tool=ToolKind.Hands;
            yield return Wheel(-120);Require(app.Living.Traveler.Tool==ToolKind.Shovel,"Grounded wheel changes the actual hotbar selection");yield return Capture("grounded-hotbar");
            yield return Keys(Key.Space);yield return Keys();yield return Keys(Key.Space);yield return Keys();Require(app.Explorer.Flying,"Actual double jump input enters flight");
            float speed=app.Settings.FlightSpeed;var tool=app.Living.Traveler.Tool;yield return Wheel(120);Require(app.Settings.FlightSpeed>speed&&app.Living.Traveler.Tool==tool,"Flying wheel scales speed without changing tools");yield return Capture("flight-speed");
            var airborne=app.Explorer.Position;yield return Keys(Key.Space);yield return Keys();yield return Keys(Key.Space);yield return Keys();Require(!app.Explorer.Flying&&Vector3.Distance(airborne,app.Explorer.Position)<5,"Double jump returns to gravity without teleporting");yield return Capture("freefall");
            yield return Keys(Key.Escape);yield return Keys();Require(app.Menu.Page==ApplicationPage.Pause&&Cursor.lockState==CursorLockMode.None,"Escape opens pause and releases cursor capture");yield return Keys(Key.Escape);yield return Keys();Require(app.Menu.Page==ApplicationPage.World,"Escape resumes gameplay");
            app.SaveWorld();yield return Operation();File.WriteAllText(Path.Combine(output,"input-stage-one.txt"),"PASS: live capture conflict/cancel, remapped camera movement, unchanged geography, actual grounded/flying wheel contexts, double-tap flight/freefall, Escape and cursor release. Preferences saved for restart.");app.ReturnToMenu();yield return null;Application.Quit();
        }
        private IEnumerator Operation(){float end=Time.realtimeSinceStartup+900;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Require(!app.Busy,"Input world operation completed");}
        private IEnumerator Ready(){float end=Time.realtimeSinceStartup+180;while((!app.Terrain.IsReady||!app.Explorer.Ready)&&Time.realtimeSinceStartup<end)yield return null;Require(app.Terrain.IsReady&&app.Explorer.Ready,"Exploration collision ready");}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private void OnDestroy(){Application.logMessageReceived-=Logged;}
    }
}
