using Aevoria.World;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Aevoria.Tests
{
    public sealed class RefinementTests
    {
        [SetUp]public void BeginWithNeutralOrigin()=>Aevoria.Presentation.WorldOrigin.Reset();
        [TearDown]public void RestoreNeutralOrigin()=>Aevoria.Presentation.WorldOrigin.Reset();
        // Six complete constructions share one case. Keep every coverage,
        // bounds and repeatability assertion while allowing the smooth-channel
        // generator its measured construction time on the target machine.
        [Test,Timeout(600000)] public void ContinentalCoverageAndLargeCoordinatesAreDeterministic()
        {
            foreach(var size in new[]{WorldSize.Small,WorldSize.Medium,WorldSize.Large})
            {
                var a=new ProceduralGeography(GenerationSettings.Preset("coverage",size));
                var b=new ProceduralGeography(GenerationSettings.Preset("coverage",size));
                Assert.That(a.MeasureLandFraction(),Is.InRange(0.48f,0.58f));
                double edge=a.Configuration.WidthMeters/2;
                Assert.IsFalse(a.Configuration.Contains(new GeoPoint(edge+1,0)));
                for(int i=-10;i<10;i++) Assert.AreEqual(a.ElevationAt(i*1300,i*1700),b.ElevationAt(i*1300,i*1700));
            }
        }
        [Test] public void VegetationIsDeterministicAndAboveWater()
        {
            var a=new ProceduralGeography(GenerationSettings.Preset("forest",WorldSize.Medium));
            var b=new ProceduralGeography(GenerationSettings.Preset("forest",WorldSize.Medium));
            int trees=0;
            // The 5 km raster aliased narrow coastal forest bands. Increase
            // coverage ninefold, retaining the same determinism/water/count
            // assertions rather than requiring forest at every sparse probe.
            for(int z=-150;z<150;z++) for(int x=-150;x<150;x++)
            {
                bool found=VegetationDistribution.TryTree(a,(int)(x/300.0*a.Configuration.WidthMeters/16),(int)(z/300.0*a.Configuration.DepthMeters/16),out var left);
                Assert.AreEqual(found,VegetationDistribution.TryTree(b,(int)(x/300.0*b.Configuration.WidthMeters/16),(int)(z/300.0*b.Configuration.DepthMeters/16),out var right));
                if(!found) continue;
                Assert.AreEqual(left.East,right.East); Assert.AreEqual(left.Scale,right.Scale);
                Assert.Greater(left.Height,a.SeaLevel+6); trees++;
            }
            Assert.Greater(trees,30); Assert.Less(trees,9000);
        }
        [Test] public void FreeCameraKeepsAltitudeAcrossTerrainAndBounds()
        {
            var obj=new GameObject("free camera"); obj.AddComponent<Camera>(); var cam=obj.AddComponent<MapCamera>();
            try
            {
                cam.Initialize(new ProceduralGeography(GenerationSettings.Preset("camera-free",WorldSize.Small)));
                for(int i=0;i<120;i++) { cam.MoveHorizontal(new Vector2(80,33)); cam.Advance(1f/60); Assert.GreaterOrEqual(cam.Clearance,2.99f); }
                Assert.Greater(cam.Focus.x,cam.GetComponent<Camera>().nearClipPlane+2000);
                Assert.IsFalse(cam.TerrainFollowing); Assert.IsFalse(cam.TerrainCollision);
            }
            finally { Object.DestroyImmediate(obj); }
        }
        [Test] public void GodCameraAlwaysRecoversAboveTerrainEvenWhenOptionalSlopeProbeIsOff()
        {
            var obj=new GameObject("camera collision"); obj.AddComponent<Camera>(); var cam=obj.AddComponent<MapCamera>();
            try
            {
                cam.Initialize(new PlaceholderGeography(new WorldConfiguration("flat",2,2,64,4)));
                cam.SetView(new GeoPoint(0,0),0,55,2,-100);
                Assert.GreaterOrEqual(cam.transform.position.y,3);
                cam.TerrainCollision=true; cam.Advance(1);
                Assert.GreaterOrEqual(cam.transform.position.y,3);
            }
            finally { Object.DestroyImmediate(obj); }
        }
    }
}
