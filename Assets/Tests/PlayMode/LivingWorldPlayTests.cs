using System.Collections;
using Aevoria.World;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Aevoria.Tests
{
    public sealed class LivingWorldPlayTests
    {
        [UnityTest]public IEnumerator ExcavationPublishesBakedCollisionAndCanCloseDuringReplacement()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();if(app==null)app=new GameObject("excavation test").AddComponent<AevoriaBootstrap>();
            app.ReturnToMenu();yield return null;yield return null;app.Regenerate("restoration-008",WorldSize.Miniature,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));app.Clock.Pause();app.Controller.ExternalDriver=true;
            var geo=app.Terrain.Geography;app.Living.Traveler.Tool=ToolKind.Shovel;bool dug=false;double x=0,z=0;
            for(int i=0;i<app.Ecology.Count&&!dug;i++){var p=app.Ecology.EnvironmentAt(i).Center;x=System.Math.Round(p.X/32)*32;z=System.Math.Round(p.Y/32)*32;float h=geo.ElevationAt(x,z);if(h<geo.SeaLevel+8||geo.Hydrology.IsLakeWater(x,z,h)||geo.Hydrology.IsRiverWater(x,z,h))continue;dug=app.Living.Dig(x,h,z,ResourceKind.Soil,out _);}
            Assert.IsTrue(dug,"Find a supported dry excavation before testing collision");app.Controller.SetView(new GeoPoint(x,z),25,55,30);
            float end=Time.realtimeSinceStartup+90;while(app.Excavations.InstalledRevision==0&&Time.realtimeSinceStartup<end)yield return null;
            Assert.Greater(app.Excavations.InstalledRevision,0,$"Focus {app.Controller.Focus}, cut {x},{z}, chunks {app.Excavations.ResidentChunks}, bake {app.Excavations.IsBaking}, worker {app.Excavations.LastPreparationMilliseconds}");Assert.IsTrue(app.Excavations.Ready);Assert.IsTrue(app.Excavations.OwnsGround(x,z));
            MeshCollider collider=null;RaycastHit hit=default;var ray=new Ray(WorldOrigin.Relative(new Vector3((float)x+4,geo.ElevationAt(x+4,z+4)+20,(float)z+4)),Vector3.down);foreach(var candidate in app.Excavations.GetComponentsInChildren<MeshCollider>())if(candidate.Raycast(ray,out hit,50)){collider=candidate;break;}Assert.IsNotNull(collider);Assert.IsNotNull(collider.sharedMesh);Assert.Greater(hit.normal.y,.5f);
            Assert.IsTrue(app.Explorer.IsTerrainCollider(collider));var obstacle=new GameObject("Object collider ownership regression");obstacle.transform.SetParent(app.transform,false);obstacle.transform.position=Vector3.up*30000;var objectCollider=obstacle.AddComponent<BoxCollider>();Assert.IsFalse(app.Explorer.IsTerrainCollider(objectCollider),"An object under the same application root is not geological terrain");Object.Destroy(obstacle);
            var installed=collider.sharedMesh;Assert.IsTrue(app.Living.Dig(x+2,geo.ElevationAt(x+2,z),z,ResourceKind.Soil,out _));
            end=Time.realtimeSinceStartup+90;while(!app.Excavations.IsBaking&&Time.realtimeSinceStartup<end)yield return null;
            Assert.IsTrue(app.Excavations.IsBaking);Assert.AreSame(installed,collider.sharedMesh,"Old collision stays live until the replacement bake completes");
            app.ReturnToMenu();yield return null;yield return null;Assert.AreEqual(0,Object.FindObjectsByType<ExcavationPresentation>(FindObjectsSortMode.None).Length);Assert.AreEqual(0,Shader.GetGlobalFloat("_AevoriaDigCount"));
        }
        [UnityTest] public IEnumerator TravelerUsesTerrainCollisionAndRetainsPoseAcrossViewModes()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();if(app==null)app=new GameObject("living test").AddComponent<AevoriaBootstrap>();
            app.ReturnToMenu();yield return null;yield return null;app.Regenerate("ecosystem-005",WorldSize.Small,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));app.Clock.Pause();app.Controller.ExternalDriver=true;
            float end=Time.realtimeSinceStartup+60;while((!app.Terrain.IsReady||!app.Waters.IsReady)&&Time.realtimeSinceStartup<end)yield return null;
            // Ecological land begins two metres above sea level; the traveler
            // requires a safe, dry four-metre margin and a clear capsule. Pick
            // an actual supported spawn before testing its collision/pose.
            bool entered=false;for(int region=0;region<app.Ecology.Count&&!entered;region++){var e=app.Ecology.EnvironmentAt(region);if(e.Land&&e.Height>app.Terrain.Geography.SeaLevel+8)entered=app.Explorer.EnterAt(e.Center);}
            Assert.IsTrue(entered,"The world must contain a supported dry traveler spawn");app.Explorer.AcceptInput=false;end=Time.realtimeSinceStartup+30;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.Explorer.Ready);
            for(int i=0;i<60;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}
            var start=app.Explorer.Position;double terrain=LocalSolidField.RenderedGround(app.Terrain.Geography,start.x,start.z);Assert.That(start.y,Is.InRange((float)terrain-.15f,(float)terrain+.3f));
            app.Controller.SetView(new GeoPoint(start.x,start.z),25,72,300000);app.Explorer.SetFirstPerson(true);yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsNull(Object.FindFirstObjectByType<DistantMountainHorizon>(),"Distant scenery must use canonical terrain rather than decorative mountain curtains");Assert.AreEqual(3,app.Terrain.BaseMeshes,"Canonical fallback scenery remains available in Explore");Assert.IsNotNull(app.Terrain.Regional);
            app.Explorer.StepMovement(Vector2.zero,false,true,false,.02f);Assert.IsTrue(app.Living.Traveler.Crouched);app.Explorer.SetFirstPerson(true);yield return null;Assert.IsTrue(app.Living.Traveler.FirstPerson);
            app.Explorer.StepMovement(Vector2.zero,false,false,true,.02f);for(int i=0;i<10;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,.02f);yield return null;}Assert.Greater(app.Explorer.Position.y,start.y+.1f,"Jump must move the real capsule upward");
            Assert.IsFalse(app.Explorer.RegisterSpaceTap(100));Assert.IsTrue(app.Explorer.RegisterSpaceTap(100.2f));Assert.IsTrue(app.Explorer.Flying);
            float flightStart=app.Explorer.Position.y;int wood=app.Living.Traveler.Wood;
            for(int i=0;i<90;i++){app.Explorer.StepFlight(Vector2.zero,true,false,false,1/60f);yield return null;}
            Assert.Greater(app.Explorer.Position.y,flightStart+10);float hover=app.Explorer.Position.y;for(int i=0;i<90;i++){app.Explorer.StepFlight(Vector2.zero,false,false,false,1/60f);yield return null;}Assert.GreaterOrEqual(app.Explorer.Position.y,hover-.05f);Assert.AreEqual(wood,app.Living.Traveler.Wood);
            Assert.IsTrue(app.Living.Capture().Traveler.Flying);
            var suspended=app.Explorer.GeographicPosition;double vx=app.Living.Traveler.VelocityX,vy=app.Living.Traveler.VelocityY,vz=app.Living.Traveler.VelocityZ;app.Explorer.Toggle();Assert.IsFalse(app.Living.Traveler.Exploring);Assert.IsTrue(app.Explorer.EnterAt(new GeoPoint(suspended.X,suspended.Z),true));end=Time.realtimeSinceStartup+90;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.Explorer.Ready);Assert.AreEqual(suspended.X,app.Explorer.GeographicPosition.X,.001);Assert.AreEqual(suspended.Y,app.Explorer.GeographicPosition.Y,.001);Assert.AreEqual(suspended.Z,app.Explorer.GeographicPosition.Z,.001);Assert.IsTrue(app.Explorer.Flying);Assert.AreEqual(vx,app.Living.Traveler.VelocityX,.001);Assert.AreEqual(vy,app.Living.Traveler.VelocityY,.001);Assert.AreEqual(vz,app.Living.Traveler.VelocityZ,.001);
            app.Explorer.SetFirstPerson(true);app.Explorer.CyclePerspective();Assert.IsFalse(app.Living.Traveler.FirstPerson);Assert.IsFalse(app.Living.Traveler.ReverseThirdPerson);app.Explorer.CyclePerspective();Assert.IsTrue(app.Living.Traveler.ReverseThirdPerson);app.Explorer.CyclePerspective();Assert.IsTrue(app.Living.Traveler.FirstPerson);Assert.IsFalse(app.Living.Traveler.ReverseThirdPerson);
            var airborne=app.Explorer.Position;Assert.IsTrue(app.Explorer.SetFlight(false));Assert.IsFalse(app.Explorer.Flying);
            Assert.AreEqual(airborne,app.Explorer.Position,"Disabling flight preserves the airborne capsule exactly");
            for(int i=0;i<30;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}
            Assert.Less(app.Explorer.Position.y,airborne.y-1,"Gravity must cause real freefall");
            Assert.Greater(app.Explorer.Position.y,start.y+3,"Freefall must not snap to the surface");
            Assert.IsTrue(app.Explorer.Falling);Assert.Less(app.Explorer.VerticalSpeed,-5);
            for(int i=0;i<240&&!app.Explorer.Grounded;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}
            Assert.IsTrue(app.Explorer.Grounded,"Freefall must land on real terrain collision");
            Assert.That(app.Explorer.Position.y,Is.EqualTo(start.y).Within(.15f));Assert.IsFalse(app.Living.Traveler.Airborne);
            end=Time.realtimeSinceStartup+30;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.Explorer.Ready);
            app.Explorer.SetFirstPerson(false);app.Explorer.Exit();Assert.IsFalse(app.Living.Traveler.Exploring);Assert.IsTrue(app.Living.Traveler.HasPosition);Assert.AreEqual(app.Explorer.GeographicPosition.X,app.Living.Traveler.X);
            app.Navigation.Toggle();yield return null;Assert.IsTrue(MapCamera.OverlayBlocked);app.Navigation.Toggle();yield return null;Assert.IsFalse(MapCamera.OverlayBlocked);
            app.ReturnToMenu();yield return null;yield return null;Assert.IsNull(app.Living);Assert.AreEqual(0,Object.FindObjectsByType<ExperimentalExplorer>(FindObjectsSortMode.None).Length);Assert.AreEqual(0,Object.FindObjectsByType<WorldNavigation>(FindObjectsSortMode.None).Length);
        }
    }
}
