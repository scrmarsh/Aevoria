using System;
using System.Reflection;
using System.IO;
using Aevoria.World;
using Aevoria.Presentation;
using Aevoria.Persistence;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Aevoria.Tests
{
    public sealed class RestorationAddendumTests
    {
        [SetUp]public void BeginWithNeutralOrigin()=>WorldOrigin.Reset();
        [TearDown]public void RestoreNeutralOrigin()=>WorldOrigin.Reset();
        [Test]public void GroundViewKeepsHorizonWithoutUsingPlanetaryFarClip()
        {
            var obj=new GameObject("View-dependent far clipping");var camera=obj.AddComponent<Camera>();var controller=obj.AddComponent<MapCamera>();
            try{
                controller.Initialize(new PlaceholderGeography(GenerationSettings.Preset("clip",WorldSize.Colossal)));controller.SetView(new GeoPoint(900000,0),25,55,2);
                Assert.That(camera.farClipPlane,Is.InRange(150000f,220000f),"Ground views retain the 140 km scenery without a multi-million-metre far/near ratio");
                Assert.That(camera.nearClipPlane,Is.InRange(.04f,.1f),"A local scene origin restores nearby depth precision at Colossal geographic coordinates");
                foreach(var point in new[]{Vector2.zero,Vector2.one,new Vector2(.5f,.5f)}){var ray=camera.ViewportPointToRay(point);Assert.IsFalse(float.IsNaN(ray.direction.x));Assert.Greater(ray.direction.sqrMagnitude,.99f);}
                controller.SetView(new GeoPoint(0,0),0,72,2200000);Assert.Greater(camera.farClipPlane,4000000);Assert.Less(camera.nearClipPlane,controller.Clearance);
            }finally{Object.DestroyImmediate(obj);WorldOrigin.Reset();}
        }
        [Test]public void NavigationRaysMatchCameraAndRemainStableAtPlanetaryClipRatios()
        {
            var obj=new GameObject("Planetary projection");var camera=obj.AddComponent<Camera>();camera.aspect=16f/9;camera.fieldOfView=60;camera.nearClipPlane=.3f;camera.farClipPlane=1000;obj.transform.SetPositionAndRotation(new Vector3(10,20,30),Quaternion.Euler(55,25,0));
            try{
                foreach(bool physical in new[]{false,true})
                {
                    camera.usePhysicalProperties=physical;if(physical)camera.lensShift=new Vector2(.1f,-.05f);
                    foreach(var point in new[]{Vector2.zero,Vector2.one,new Vector2(.5f,.5f),new Vector2(.1f,.8f)})
                    {
                        camera.nearClipPlane=.3f;camera.farClipPlane=1000;var expected=camera.ViewportPointToRay(point);var actual=CameraRays.Viewport(camera,point);
                        Assert.Less(Vector3.Distance(expected.direction,actual.direction),.00001f);Assert.Less(Vector3.Distance(expected.origin,actual.origin),.001f);
                        camera.nearClipPlane=.05f;camera.farClipPlane=8000000;actual=CameraRays.Viewport(camera,point);Assert.Less(Vector3.Distance(expected.direction,actual.direction),.00001f);Assert.IsFalse(float.IsNaN(actual.origin.x));
                        var screen=CameraRays.Screen(camera,camera.pixelRect.position+Vector2.Scale(point,camera.pixelRect.size));Assert.Less(Vector3.Distance(screen.direction,actual.direction),.00001f);
                    }
                }
            }finally{Object.DestroyImmediate(obj);}
        }
        [Test]public void PublishedTerrainRaisesCameraBeforeRenderingAndSettlesWithoutLosingClearance()
        {
            var obj=new GameObject("Published surface safety");obj.AddComponent<Camera>();var cam=obj.AddComponent<MapCamera>();float surface=0;
            try{
                cam.Initialize(new PlaceholderGeography(GenerationSettings.Preset("publication",WorldSize.Miniature)));cam.PresentationHeight=(x,z)=>surface;cam.SetView(new GeoPoint(0,0),0,45,2);
                surface=1000;cam.NotifyPresentationChanged();typeof(MapCamera).GetMethod("BeforeRenderSafety",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(cam,null);Assert.GreaterOrEqual(cam.transform.position.y,1003);
                surface=0;for(int i=0;i<240;i++){cam.Advance(1/60f);Assert.GreaterOrEqual(cam.Clearance,2.99f);}Assert.Less(cam.transform.position.y,4);
            }finally{Object.DestroyImmediate(obj);}
        }
        [Test,Timeout(300000)]public void ArchipelagoKeepsSubstantialIslandsAfterErosionAndCoverageCalibration()
        {
            var world=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Archipelago)));const int n=257;
            var land=new bool[n*n];var seen=new bool[land.Length];int total=0,islands=0;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){land[z*n+x]=world.ElevationAt((x+.5)/n*128000-64000,(z+.5)/n*128000-64000)>0;if(land[z*n+x])total++;}
            for(int i=0;i<land.Length;i++)if(land[i]&&!seen[i])
            {
                var queue=new System.Collections.Generic.Queue<int>();queue.Enqueue(i);seen[i]=true;int size=0;
                while(queue.Count>0){int k=queue.Dequeue(),x=k%n,z=k/n;size++;foreach(int next in new[]{x>0?k-1:-1,x<n-1?k+1:-1,z>0?k-n:-1,z<n-1?k+n:-1})if(next>=0&&land[next]&&!seen[next]){seen[next]=true;queue.Enqueue(next);}}
                if(size>100)islands++;
            }
            Assert.GreaterOrEqual(islands,3,"Restoring continental interiors must preserve actual island geography in Archipelago mode");Assert.That(total/(double)land.Length,Is.InRange(.48,.58));
        }
        [Test]public void GradientTerrainDoesNotForceFlatCartesianLatticeLines()
        {
            int active=0;for(int i=1;i<=100;i++){double x=i,z=i*.371,e=.00001;double derivative=(GradientField.Sample(x+e,z,123,12)-GradientField.Sample(x-e,z,123,12))/(2*e);if(Math.Abs(derivative)>.05)active++;Assert.That(GradientField.Sample(x,z,123,12),Is.InRange(-1d,1d));}
            Assert.Greater(active,90,"Value-noise lattice shelves must not be a terrain constraint");
        }
        [Test]public void CacheSurvivesRestartAndRejectsCorruptionAndDifferentIdentities()
        {
            string root=Path.Combine(Path.GetTempPath(),"AevoriaTerrainCacheTests-"+Guid.NewGuid().ToString("N"));
            try{
                var bytes=new byte[]{1,2,3,4};var cache=new TerrainPresentationCache(root,"generator7-hydrology5-render2",0);cache.Write("tile",bytes);
                var restarted=new TerrainPresentationCache(root,"generator7-hydrology5-render2",0);Assert.IsTrue(restarted.TryRead("tile",out var read));CollectionAssert.AreEqual(bytes,read);
                Assert.IsFalse(new TerrainPresentationCache(root,"different-generator",0).TryRead("tile",out _));
                var corrupt=File.ReadAllBytes(cache.PathFor("tile"));corrupt[corrupt.Length-1]^=0xff;File.WriteAllBytes(cache.PathFor("tile"),corrupt);Assert.IsFalse(restarted.TryRead("tile",out _));
                restarted.Write("tile",bytes);Assert.IsTrue(restarted.TryRead("tile",out read));CollectionAssert.AreEqual(bytes,read);
                Assert.AreEqual(0,restarted.ResidentBytes);Assert.AreEqual(0,Directory.GetFiles(Path.GetDirectoryName(cache.PathFor("tile")),"*.tmp").Length);
            }finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }
        [Test]public void PresetsAndIndependentDisplayFlagsSurviveSettingsSerialization()
        {
            var settings=new ApplicationSettings{RenderClouds=false,RenderWeatherEffects=true};settings.ApplyGraphicsPreset(2);settings.Validate();var copy=ArchiveCodec.Decode<ApplicationSettings>(ArchiveCodec.Encode(settings));
            Assert.IsFalse(copy.RenderClouds);Assert.IsTrue(copy.RenderWeatherEffects);Assert.AreEqual(4,copy.BackgroundStreamingBudget);Assert.AreEqual(2,copy.TerrainTextureQuality);
            var balanced=new ApplicationSettings();balanced.ApplyGraphicsPreset(0);Assert.Greater(copy.VegetationDistance,balanced.VegetationDistance);Assert.Greater(copy.TerrainDistance,balanced.TerrainDistance);
        }
        [Test] public void LogarithmicZoomIsReversibleAndProtectedDuringRapidInput()
        {
            var obj=new GameObject("Zoom safety");obj.AddComponent<Camera>();var cam=obj.AddComponent<MapCamera>();
            try {
                cam.Initialize(new PlaceholderGeography(GenerationSettings.Preset("camera",WorldSize.Colossal)));
                cam.SetView(new GeoPoint(1000,1000),34,-60,50000,-1000);
                for(int i=0;i<100;i++){cam.ZoomNotches(i<50?1:-1);cam.Advance(.016f);Assert.GreaterOrEqual(cam.Clearance,2.99f);Assert.Less(cam.GetComponent<Camera>().nearClipPlane,cam.Clearance);}
                cam.Initialize(null);cam.ZoomNotches(1);cam.ZoomNotches(-1);for(int i=0;i<120;i++)cam.Advance(.016f);Assert.AreEqual(900,cam.Distance,.01f);
                typeof(MapCamera).GetField("targetFocus",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(cam,new Vector3(float.NaN,0,0));cam.Advance(.016f);Assert.IsFalse(float.IsNaN(cam.transform.position.x));Assert.Greater(cam.Clearance,0);
            } finally {Object.DestroyImmediate(obj);}
        }
        [Test] public void CameraStaysAboveInstalledFallbackBeforeFineTerrainIsReady()
        {
            var obj=new GameObject("Fallback safety");obj.AddComponent<Camera>();var cam=obj.AddComponent<MapCamera>();
            try {cam.Initialize(new PlaceholderGeography(GenerationSettings.Preset("camera",WorldSize.Miniature)));cam.PresentationHeight=(x,z)=>18000;cam.SetView(new GeoPoint(0,0),0,20,2);Assert.GreaterOrEqual(cam.transform.position.y,18003);Assert.LessOrEqual(cam.GetComponent<Camera>().nearClipPlane,.25f);}finally{Object.DestroyImmediate(obj);}
        }
    }
}
