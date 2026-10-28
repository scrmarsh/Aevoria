using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Aevoria.World;
using Aevoria.Persistence;
using Aevoria.Presentation;
using Aevoria.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Aevoria.Tests
{
    public sealed class LivingWorldTests
    {
        private ProceduralGeography geo;private EcologyModel eco;
        [OneTimeSetUp]public void Setup(){geo=new ProceduralGeography(GenerationSettings.Preset("ecosystem-005",WorldSize.Small));eco=new EcologyModel(geo);}
        [TestCase(WorldSize.Miniature),TestCase(WorldSize.Small),TestCase(WorldSize.Medium),TestCase(WorldSize.Large),TestCase(WorldSize.Colossal),Timeout(600000)]public void EveryLodUsesTheSameCompleteBoundaryWithoutSubmergedSkirts(WorldSize size)
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset("edge-006",size));
            var generate=typeof(WorldRenderer).GetMethod("GeneratePayload",BindingFlags.Static|BindingFlags.NonPublic);
            for(int l=0;l<3;l++)for(int r=0;r<3;r++)
            {
                object a=generate.Invoke(null,new object[]{geo,new ChunkKey(-1,0),l,false,CancellationToken.None}),b=generate.Invoke(null,new object[]{geo,new ChunkKey(0,0),r,false,CancellationToken.None});
                var av=(Vector3[])a.GetType().GetField("Vertices").GetValue(a);var bv=(Vector3[])b.GetType().GetField("Vertices").GetValue(b);
                int sa=((64>>l)+1)*((64>>l)+1),sb=((64>>r)+1)*((64>>r)+1);
                var an=(Vector3[])a.GetType().GetField("Normals").GetValue(a);var bn=(Vector3[])b.GetType().GetField("Normals").GetValue(b);
                for(int i=0;i<=64;i++){Assert.AreEqual(av[sa+65+i],bv[sb+195+64-i]);Assert.AreEqual(an[sa+65+i],bn[sb+195+64-i]);}
                foreach(var p in av)Assert.AreEqual(geo.ElevationAt(p.x,p.z),p.y);
                var indices=(int[])a.GetType().GetField("Indices").GetValue(a);double area=0;
                for(int i=0;i<indices.Length;i+=3){var u=av[indices[i+1]]-av[indices[i]];var v=av[indices[i+2]]-av[indices[i]];double y=Vector3.Cross(u,v).y;Assert.GreaterOrEqual(y,0);area+=y*.5;}
                Assert.AreEqual(512*512,area,.1);
            }
        }
        [Test]public void FivePlanetaryPresetsHaveExactDimensions()
        {var sizes=new[]{WorldSize.Miniature,WorldSize.Small,WorldSize.Medium,WorldSize.Large,WorldSize.Colossal};for(int i=0;i<sizes.Length;i++)Assert.AreEqual(128000*(1<<i),GenerationSettings.Preset("s",sizes[i]).WidthMeters);}
        [Test]public void LakeMeshesHaveUniformLevelsAndRiversYieldTheirOpenWater()
        {
            var layout=new InlandWaterLayout(geo);Assert.Greater(layout.LakeCount,0);
            int overlap=0;for(int i=0;i<layout.Rivers.Vertices.Count;i+=3){var a=layout.Rivers.Vertices[i];var b=layout.Rivers.Vertices[i+1];var c=layout.Rivers.Vertices[i+2];if(layout.LakeAt((a.X+b.X+c.X)/3,(a.Z+b.Z+c.Z)/3,out _))overlap++;}
            Assert.AreEqual(0,overlap,"River triangle centroids must not occupy lake-owned surface");
            for(int i=0;i<layout.Lakes.Vertices.Count;i+=3){Assert.AreEqual(layout.Lakes.Vertices[i].Level,layout.Lakes.Vertices[i+1].Level);Assert.AreEqual(layout.Lakes.Vertices[i].Level,layout.Lakes.Vertices[i+2].Level);}
        }
        [Test]public void SnowAccumulatesWithoutAnInstantWhiteStateAndContinuesAfterLoad()
        {
            var coldGeo=new ProceduralGeography(GenerationSettings.Preset("winter",WorldSize.Small),new ClimateSettings(-20,1,65));var e=new EcologyModel(coldGeo);var a=new SeasonalModel(e,"winter",0);
            a.AdvanceTo(28);Assert.IsTrue(Enumerable.Range(0,e.Count).Any(i=>a.Snow(i)>0&&a.Snow(i)<.15f&&a.Ice(i)>0));
            var b=new SeasonalModel(e,"winter",28,a.Capture());a.AdvanceTo(280,100);b.AdvanceTo(280,100);CollectionAssert.AreEqual(a.Capture().Snow,b.Capture().Snow);
        }
        [Test]public void WarmGroundThawsGradually()
        {var a=new SeasonalModel(eco,geo.Configuration.Seed,0);var s=a.Capture();for(int i=0;i<s.Snow.Length;i++)s.Snow[i]=1;var thaw=new SeasonalModel(eco,geo.Configuration.Seed,0,s);int land=Enumerable.Range(0,eco.Count).First(i=>eco.EnvironmentAt(i).Land&&eco.Temperature(i,28)>5);thaw.AdvanceTo(28);Assert.Greater(thaw.Snow(land),0);Assert.Less(thaw.Snow(land),1);}
        [Test]public void DepletionSurvivesAResourceModelRebuild()
        {var world=new LivingWorldState(geo,eco,0);Assert.AreEqual(5,world.Harvest("natural:1:2",ResourceKind.Wood,5,ToolKind.Hands,0));Assert.AreEqual(0,world.Available("natural:1:2",5,0,false));var copy=new LivingWorldState(geo,eco,0,world.Capture());Assert.AreEqual(0,copy.Available("natural:1:2",5,0,false));Assert.AreEqual(5,copy.Traveler.Wood);}
        [Test]public void SolidSubtractionHasUndergroundVolumeInsteadOfAnEmptyHeightMap()
        {var site=new ExcavationSite{X=0,Z=0,Top=100,Bottom=-100};double h=LocalSolidField.RenderedGround(geo,0,0);site.Cuts.Add(new DigCut{X=0,Y=h-2,Z=0,Radius=.8});var field=new LocalSolidField(geo,site);Assert.Greater(field.Density(0,h-5,0),0);Assert.Less(field.Density(0,h-2,0),0);Assert.Greater(field.Density(0,h-.5,0),0);}
        [Test]public void PreviousGenerationUpgradeIsRejectedWithoutChangingArchive()
        {
            string root=Path.Combine(Path.GetTempPath(),"Aevoria006-"+Guid.NewGuid().ToString("N"));try
            {var record=WorldRecord.Capture(null,"Legacy ecology",null,geo,new SimulationClock(new WorldState(geo.Configuration.Seed)),"0.0.5",eco);record.Schema=4;record.Generator=4;var bytes=ArchiveCodec.Pack(record);Directory.CreateDirectory(Path.Combine(root,record.Id));string original=Path.Combine(root,record.Id,"world.xml");File.WriteAllBytes(original,bytes);
                Assert.Throws<NotSupportedException>(()=>new WorldRepository(root).UpgradeCopyAsync(record.Id).GetAwaiter().GetResult());CollectionAssert.AreEqual(bytes,File.ReadAllBytes(original));Assert.AreEqual(1,Directory.GetDirectories(root).Length);}
            finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }
    }
}
