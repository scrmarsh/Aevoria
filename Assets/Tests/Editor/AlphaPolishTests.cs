using System;
using System.Reflection;
using System.Threading;
using Aevoria.Core;
using Aevoria.World;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Aevoria.Tests
{
    public sealed class AlphaPolishTests
    {
        [Test] public void VegetationComponentConstructionDoesNotCreateNativeRenderingState()
        {
            var obj=new GameObject("vegetation initialization");
            try {Assert.IsNotNull(obj.AddComponent<WorldVegetation>());}
            finally {UnityEngine.Object.DestroyImmediate(obj);}
        }
        [Test] public void ApplicationVersionAndBuildMetadataAgree()
        {
            Assert.IsTrue(Version.TryParse(ApplicationVersion.Number,out var version));
            Assert.AreEqual(0,version.Major);Assert.AreEqual(0,version.Minor);
            Assert.AreEqual(ApplicationVersion.Number,PlayerSettings.bundleVersion);
            Assert.AreEqual("Aevoria "+ApplicationVersion.Stage+" v"+ApplicationVersion.Number,ApplicationVersion.DisplayName);
            Assert.AreEqual(10,ProceduralGeography.GeneratorVersion,"World Refinement geography has an explicit incompatible generator identity");
        }
        [Test] public void VegetationVariantsRemainRepeatableAndBounded()
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset("forest",WorldSize.Medium));
            var seen=new bool[WorldVegetation.TreeVariants];int count=0;
            for(int z=-150;z<150;z++) for(int x=-150;x<150;x++)
            {
                if(!VegetationDistribution.TryTree(geo,(int)(x/300.0*geo.Configuration.WidthMeters/16),(int)(z/300.0*geo.Configuration.DepthMeters/16),out var site)) continue;
                Assert.That(site.Variant,Is.InRange(0,5));Assert.That(site.WidthScale,Is.InRange(0.72f,1.30f));
                Assert.That(site.Scale,Is.InRange(0.8f,1.9f));Assert.AreEqual(site.Conifer,site.Variant<3);
                Assert.IsTrue(VegetationDistribution.TryTree(geo,(int)(x/300.0*geo.Configuration.WidthMeters/16),(int)(z/300.0*geo.Configuration.DepthMeters/16),out var repeat));Assert.AreEqual(site.Variant,repeat.Variant);
                Assert.AreEqual(site.WidthScale,repeat.WidthScale);seen[site.Variant]=true;count++;
            }
            Assert.Greater(count,100);Assert.GreaterOrEqual(Array.FindAll(seen,v=>v).Length,4);
        }
        [Test] public void EveryTreeLodHasValidFiniteGeometry()
        {
            var build=typeof(WorldVegetation).GetMethod("Tree",BindingFlags.Static|BindingFlags.NonPublic);
            for(int kind=0;kind<WorldVegetation.TreeVariants;kind++) foreach(bool detail in new[]{false,true})
            {
                var mesh=(Mesh)build.Invoke(null,new object[]{kind,detail});
                try {Assert.Greater(mesh.vertexCount,30);foreach(var p in mesh.vertices) Assert.IsFalse(float.IsNaN(p.x)||float.IsInfinity(p.y));foreach(int index in mesh.triangles) Assert.That(index,Is.InRange(0,mesh.vertexCount-1));Assert.Greater(mesh.bounds.size.y,kind==11?2:5);}
                finally {UnityEngine.Object.DestroyImmediate(mesh);}
            }
        }
        [Test] public void LodOnlyJobsDoNotRebuildForestPlacements()
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset("reuse",WorldSize.Small));
            var method=typeof(WorldRenderer).GetMethod("GeneratePayload",BindingFlags.Static|BindingFlags.NonPublic);
            var payload=method.Invoke(null,new object[]{geo,new ChunkKey(0,0),1,false,CancellationToken.None});
            var placement=(VegetationChunk)payload.GetType().GetField("Vegetation").GetValue(payload);
            Assert.IsNull(placement.Trees,"LOD-only work must retain the installed placement batch");
        }
        [Test] public void CancelledForestGenerationStopsBeforeTraversal()
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset("cancel",WorldSize.Small));
            using(var cancellation=new CancellationTokenSource()) {cancellation.Cancel();Assert.Throws<OperationCanceledException>(()=>VegetationDistribution.CreateChunk(geo,new ChunkKey(0,0),cancellation.Token));}
        }
    }
}

