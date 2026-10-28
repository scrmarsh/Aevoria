using System.Collections;
using Aevoria.Presentation;
using Aevoria.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Aevoria.Tests
{
    public sealed class NaturalWorldStabilityTests
    {
        [UnityTest,Timeout(300000)]
        public IEnumerator FastHorizontalFlightStopsAtACliffInsteadOfSnappingOntoItsTop()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();if(app==null)app=new GameObject("cliff flight stability").AddComponent<AevoriaBootstrap>();
            app.ReturnToMenu();yield return null;yield return null;app.Regenerate("restoration-008",WorldSize.Miniature,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));
            app.Clock.Pause();app.Controller.ExternalDriver=true;app.Explorer.AcceptInput=false;var geo=app.Terrain.Geography;var hydro=geo.Hydrology;
            bool found=false;double sx=0,sz=0,dx=0,dz=0;float flightHeight=0;
            // Refined lowland coasts are gentle. Find an actual rising mountain
            // face instead of depending on the removed global coastal scarp.
            foreach(var cluster in geo.Mountains.Clusters)
            {
                for(int angle=0;angle<32&&!found;angle++)for(double radius=cluster.Radius;radius>600&&!found;radius-=300)
                {
                    double a=angle*System.Math.PI/16,x=cluster.X+System.Math.Cos(a)*radius,z=cluster.Z+System.Math.Sin(a)*radius;
                    double east=-System.Math.Cos(a),north=-System.Math.Sin(a);if(!geo.Configuration.Contains(new GeoPoint(x,z)))continue;
                    float low=(float)LocalSolidField.RenderedGround(geo,x,z),high=(float)LocalSolidField.RenderedGround(geo,x+east*300,z+north*300);
                    if(low<geo.SeaLevel+10||high-low<120)continue;
                    sx=x;sz=z;dx=east;dz=north;flightHeight=low+30;found=true;
                }
                if(found)break;
            }
            Assert.IsTrue(found,"Find a canonical mountain face rising above horizontal flight altitude");var state=app.Living.Traveler;state.HasPosition=true;state.Flying=true;state.X=sx;state.Z=sz;state.Y=flightHeight;state.Yaw=Mathf.Atan2((float)dx,(float)dz)*Mathf.Rad2Deg;
            Assert.IsTrue(app.Explorer.EnterAt(new GeoPoint(sx,sz),true));float end=Time.realtimeSinceStartup+90;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.Explorer.Ready);
            var settings=app.Settings.Copy();settings.FlightSpeed=500;app.ApplySettings(settings,false);float initial=app.Explorer.Position.y;
            for(int i=0;i<120;i++)
            {
                app.Explorer.StepFlight(Vector2.up,false,false,true,1/60f);Assert.That(app.Explorer.Position.y,Is.EqualTo(initial).Within(.5f),"Horizontal flight must collide rather than teleport upwards onto a scarp");yield return null;
            }
            double travelled=System.Math.Sqrt(System.Math.Pow(app.Explorer.GeographicPosition.X-sx,2)+System.Math.Pow(app.Explorer.GeographicPosition.Z-sz,2));Assert.That(travelled,Is.InRange(1,350),"Flight must advance and stop before crossing the higher terrain");
            app.ReturnToMenu();yield return null;yield return null;
        }
        [UnityTest,Timeout(600000)]
        public IEnumerator MaximumFlightAcrossOriginChangesPreservesSmallMotionAndMomentum()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();if(app==null)app=new GameObject("far coordinate flight").AddComponent<AevoriaBootstrap>();
            app.ReturnToMenu();yield return null;yield return null;
            app.Regenerate("ecosystem-005",WorldSize.Colossal,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));
            app.Clock.Pause();app.Controller.ExternalDriver=true;app.Explorer.AcceptInput=false;
            Assert.AreEqual(-WorldOrigin.Offset,app.Waters.transform.position,"Geographic meshes created after camera initialization inherit its origin");
            var state=app.Living.Traveler;state.HasPosition=true;state.Flying=true;state.X=800000;state.Z=-700000;state.Y=25000;state.Yaw=90;
            Assert.IsTrue(app.Explorer.EnterAt(new GeoPoint(state.X,state.Z),true));
            float end=Time.realtimeSinceStartup+90;while(!app.Explorer.Ready&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.Explorer.Ready);
            var settings=app.Settings.Copy();settings.FlightSpeed=500;app.ApplySettings(settings,false);
            int revision=WorldOrigin.Revision;double previous=app.Explorer.GeographicPosition.X;float expectedSpeed=0;
            for(int i=0;i<300;i++)
            {
                expectedSpeed=Mathf.Lerp(expectedSpeed,1500,1-Mathf.Exp(-8/60f));app.Explorer.StepFlight(Vector2.up,false,false,true,1/60f);
                double current=app.Explorer.GeographicPosition.X;
                Assert.That(current-previous,Is.EqualTo(expectedSpeed/60).Within(.003),"Physical displacement must remain continuous through rebases");previous=current;
                yield return null;
            }
            Assert.Greater(WorldOrigin.Revision,revision);Assert.Less(Mathf.Abs(Camera.main.transform.position.x),2100);
            var airborne=app.Explorer.GeographicPosition;Assert.IsTrue(app.Explorer.SetFlight(false));
            Assert.AreEqual(airborne.X,app.Explorer.GeographicPosition.X);Assert.AreEqual(airborne.Y,app.Explorer.GeographicPosition.Y);
            app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);Assert.Greater(app.Explorer.GeographicPosition.X,airborne.X+20,"Horizontal flight momentum carries into freefall");
            Assert.Less(app.Explorer.VerticalSpeed,0);Assert.AreEqual(app.Explorer.GeographicPosition.X,state.X);
            app.ReturnToMenu();yield return null;yield return null;Assert.AreEqual(Vector3.zero,WorldOrigin.Offset);
        }
        [UnityTest,Timeout(300000)]
        public IEnumerator ExcavationTransfersGroundOwnershipWithoutOverlappingSupport()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();
            if(app==null)app=new GameObject("natural world stability").AddComponent<AevoriaBootstrap>();
            app.ReturnToMenu();yield return null;yield return null;
            app.Regenerate("restoration-008",WorldSize.Miniature,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));
            app.Clock.Pause();app.Controller.ExternalDriver=true;app.Explorer.AcceptInput=false;
            var geo=app.Terrain.Geography;bool dug=false;double x=0,z=0;
            for(int i=0;i<app.Ecology.Count&&!dug;i++)
            {
                var p=app.Ecology.EnvironmentAt(i).Center;x=System.Math.Round(p.X/32)*32;z=System.Math.Round(p.Y/32)*32;
                float h=geo.ElevationAt(x,z);if(h<geo.SeaLevel+8||geo.Hydrology.IsLakeWater(x,z,h)||geo.Hydrology.IsRiverWater(x,z,h))continue;
                dug=app.Living.Dig(x,h,z,ResourceKind.Soil,out _);
            }
            Assert.IsTrue(dug);Assert.IsTrue(app.Explorer.EnterAt(new GeoPoint(x+4,z+4)));
            float end=Time.realtimeSinceStartup+90;
            while((!app.Explorer.Ready||app.Excavations.InstalledRevision==0)&&Time.realtimeSinceStartup<end)yield return null;
            Assert.IsTrue(app.Explorer.Ready,$"Pose {app.Explorer.Position}, cut {x},{z}, chunks {app.Excavations.ResidentChunks}, bake {app.Excavations.IsBaking}, worker {app.Excavations.LastPreparationMilliseconds}");Assert.Greater(app.Excavations.InstalledRevision,0);
            var ground=GameObject.Find("Bounded exploration collision").GetComponent<MeshCollider>();
            var ray=new Ray(WorldOrigin.Relative(new Vector3((float)x+4,geo.ElevationAt(x+4,z+4)+25,(float)z+4)),Vector3.down);
            Assert.IsFalse(ground.Raycast(ray,out _,60),"The old heightfield must not cover an installed volumetric patch");
            MeshCollider solid=null;foreach(var candidate in app.Excavations.GetComponentsInChildren<MeshCollider>())if(candidate.Raycast(ray,out _,60)){solid=candidate;break;}Assert.IsNotNull(solid,"The streamed solid collider must support the same location");
            for(int i=0;i<120;i++){app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);yield return null;}
            float low=app.Explorer.Position.y,high=low;
            Assert.IsTrue(app.Living.Dig(x+2,geo.ElevationAt(x+2,z),z,ResourceKind.Soil,out _));
            int revision=app.Excavations.InstalledRevision;end=Time.realtimeSinceStartup+60;
            while((app.Excavations.InstalledRevision==revision||!app.Explorer.Ready)&&Time.realtimeSinceStartup<end)
            {
                app.Explorer.StepMovement(Vector2.zero,false,false,false,1/60f);
                low=Mathf.Min(low,app.Explorer.Position.y);high=Mathf.Max(high,app.Explorer.Position.y);yield return null;
            }
            Assert.Greater(app.Excavations.InstalledRevision,revision);Assert.Less(high-low,.08f,"Standing beside a cut must not shake during collider replacement");
            Assert.IsFalse(ground.Raycast(ray,out _,60));app.ReturnToMenu();yield return null;yield return null;
        }
    }
}
