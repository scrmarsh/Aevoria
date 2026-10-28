using System.Linq;
using Aevoria.World;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace Aevoria.Tests
{
    public sealed class NaturalWorldEnvironmentTests
    {
        private ProceduralGeography geo;
        [OneTimeSetUp]public void Generate()=>geo=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents)));
        [Test]public void PhysicalCloudSystemsSurviveZoomAndDisplayChanges()
        {
            float quality=Shader.GetGlobalFloat("_AevoriaCloudQuality");var root=new GameObject("Physical cloud test");var cameraObject=new GameObject("Cloud zoom camera");cameraObject.AddComponent<Camera>();var navigation=cameraObject.AddComponent<MapCamera>();
            try
            {
                Shader.SetGlobalFloat("_AevoriaCloudQuality",2);var clouds=root.AddComponent<WorldCloudLayer>();clouds.Initialize(geo);clouds.Density=3;
                navigation.Initialize(geo);navigation.SetView(new GeoPoint(0,0),25,55,2000);clouds.Present(0);
                var expected=Shader.GetGlobalVectorArray("_AevoriaCloudSystems");var shapes=Shader.GetGlobalVectorArray("_AevoriaCloudShapes");Assert.IsTrue(shapes.Any(s=>s.y>0));
                foreach(float distance in new[]{20000,60000,150000}){navigation.SetView(new GeoPoint(0,0),25,55,distance);clouds.Present(0);CollectionAssert.AreEqual(expected,Shader.GetGlobalVectorArray("_AevoriaCloudSystems"));CollectionAssert.AreEqual(shapes,Shader.GetGlobalVectorArray("_AevoriaCloudShapes"));}
                clouds.RenderClouds=false;clouds.Present(0);CollectionAssert.AreEqual(expected,Shader.GetGlobalVectorArray("_AevoriaCloudSystems"));clouds.RenderClouds=true;clouds.Present(0);CollectionAssert.AreEqual(shapes,Shader.GetGlobalVectorArray("_AevoriaCloudShapes"));
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(cameraObject);WorldOrigin.Reset();Shader.SetGlobalFloat("_AevoriaCloudQuality",quality);}
        }
        [Test]public void WindAdvectionContinuesWithinOneSimulationTick()
        {
            var root=new GameObject("Subtick cloud motion");float quality=Shader.GetGlobalFloat("_AevoriaCloudQuality");
            try
            {
                Shader.SetGlobalFloat("_AevoriaCloudQuality",2);var clouds=root.AddComponent<WorldCloudLayer>();clouds.Initialize(geo);clouds.Present(40);var a=Shader.GetGlobalVectorArray("_AevoriaCloudSystems");clouds.Present(40.25);var b=Shader.GetGlobalVectorArray("_AevoriaCloudSystems");clouds.Present(40.5);var c=Shader.GetGlobalVectorArray("_AevoriaCloudSystems");
                for(int i=0;i<a.Length;i++){float first=b[i].x-a[i].x,second=c[i].x-b[i].x;Assert.That(Mathf.Abs(first),Is.InRange(49.9f,50.1f));Assert.AreEqual(first,second,.01f);Assert.AreEqual(a[i].w,b[i].w);}
            }
            finally{Object.DestroyImmediate(root);Shader.SetGlobalFloat("_AevoriaCloudQuality",quality);}
        }
    }
}
