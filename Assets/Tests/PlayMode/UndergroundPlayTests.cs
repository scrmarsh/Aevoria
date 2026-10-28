using System;
using System.Collections;
using Aevoria.World;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Aevoria.Tests
{
    public sealed class UndergroundPlayTests
    {
        [UnityTest,Timeout(600000)]public IEnumerator RealCaveCollisionMiningAndUndergroundPoseSurviveSaveLoad()
        {
            Assert.IsNotNull(AevoriaBootstrap.Argument("-aevoriaTestRoot"),"Run this persistence test with Tools/ValidatePhase1.ps1 so creator archives stay isolated");
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();if(app==null)app=new GameObject("Underground traversal regression").AddComponent<AevoriaBootstrap>();app.ExternalControl=true;app.ReturnToMenu();yield return null;yield return null;
            app.Regenerate("shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents,renderRadius:2,nearLodRadius:0,mediumLodRadius:1));app.Clock.Pause();app.Controller.ExternalDriver=true;app.Explorer.AcceptInput=false;
            var geo=app.Terrain.Geography;CaveSystem system=null;for(int z=-16;z<=16&&system==null;z++)for(int x=-16;x<=16;x++){var candidate=geo.Caves.SystemAt(x,z);if(candidate.Active&&candidate.HillsideEntrance){system=candidate;break;}}Assert.IsNotNull(system);
            var passage=system.Passages[0];var point=SolidPoint.Mix(passage.A,passage.B,.5);double low=point.Y,high=point.Y;
            while(app.Living.SolidDensity(point.X,low,point.Z)<0&&point.Y-low<100)low-=.5;Assert.Less(point.Y-low,100);
            for(int i=0;i<30;i++){double mid=(low+high)*.5;if(app.Living.SolidDensity(point.X,mid,point.Z)>=0)low=mid;else high=mid;}
            var state=app.Living.Traveler;state.HasPosition=true;state.X=point.X;state.Y=high+.10;state.Z=point.Z;state.Yaw=Math.Atan2(passage.B.X-passage.A.X,passage.B.Z-passage.A.Z)*Mathf.Rad2Deg;state.Tool=ToolKind.Pickaxe;
            Assert.Less(state.Y,LocalSolidField.RenderedGround(geo,state.X,state.Z)-5);double restoredY=state.Y;
            Assert.IsTrue(app.Explorer.EnterAt(new GeoPoint(state.X,state.Z),true));Assert.AreEqual(restoredY,app.Explorer.GeographicPosition.Y,.001,"Restoration must retain an underground pose rather than recover to the surface");
            float end=Time.realtimeSinceStartup+120;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.Explorer.Ready,"Streamed cave support becomes ready");Assert.Greater(app.Excavations.ResidentChunks,0);
            for(int i=0;i<90;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}Assert.IsTrue(app.Explorer.Grounded,"Actual meshed cave floor supports the controller");
            float minimum=app.Explorer.Position.y,maximum=minimum;for(int i=0;i<90;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;minimum=Mathf.Min(minimum,app.Explorer.Position.y);maximum=Mathf.Max(maximum,app.Explorer.Position.y);}Assert.Less(maximum-minimum,.08f,"Underground standing is stable");
            var initial=app.Explorer.GeographicPosition;for(int i=0;i<90;i++){app.Explorer.StepMovement(Vector2.up,false,false,false,1/60f);yield return null;}var walked=app.Explorer.GeographicPosition;Assert.Greater(Math.Sqrt(Math.Pow(walked.X-initial.X,2)+Math.Pow(walked.Z-initial.Z,2)),1,"The capsule can walk inside the real passage");
            end=Time.realtimeSinceStartup+120;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.Explorer.Ready);
            Vector3 eye=WorldOrigin.Relative(new Vector3((float)walked.X,(float)walked.Y+1.5f,(float)walked.Z));Vector3 side=Quaternion.Euler(0,(float)state.Yaw+90,0)*Vector3.forward;
            Assert.IsTrue(Physics.Raycast(new Ray(eye,side),out var wall,6,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),"Aim at an actual cave wall");Vector3 target=WorldOrigin.Geographic(wall.point);float previousDistance=wall.distance;
            Assert.IsTrue(app.Explorer.DigAt(target,ResourceKind.Stone));int revision=app.Excavations.InstalledRevision;end=Time.realtimeSinceStartup+120;
            while((!app.Excavations.Ready||app.Excavations.InstalledRevision==revision)&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.Excavations.Ready);Assert.Greater(app.Excavations.InstalledRevision,revision);
            Assert.IsTrue(Physics.Raycast(new Ray(eye,side),out var cut,8,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore));Assert.Greater(cut.distance,previousDistance+.3,"Mining removes actual cave-wall collision");
            var saved=app.Explorer.GeographicPosition;app.SaveWorld(newName:"Underground regression");end=Time.realtimeSinceStartup+60;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Assert.IsNotNull(app.Current,app.Message);string id=app.Current.Id;
            app.ReturnToMenu();yield return null;yield return null;app.LoadWorld(id);end=Time.realtimeSinceStartup+240;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.WorldActive,app.Message);
            end=Time.realtimeSinceStartup+120;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.Explorer.Ready);var loaded=app.Explorer.GeographicPosition;
            Assert.AreEqual(saved.X,loaded.X,.01);Assert.AreEqual(saved.Y,loaded.Y,.01);Assert.AreEqual(saved.Z,loaded.Z,.01);Assert.Less(app.Living.SolidDensity(target.x,target.y,target.z),-.4,"The mined cave wall survives a new-world reconstruction");
            app.ReturnToMenu();yield return null;yield return null;
        }
    }
}
