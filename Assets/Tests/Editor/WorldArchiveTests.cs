using System;
using System.IO;
using System.Threading.Tasks;
using Aevoria.Core;
using Aevoria.Persistence;
using Aevoria.Simulation;
using Aevoria.World;
using NUnit.Framework;

namespace Aevoria.Tests
{
    public sealed class WorldArchiveTests
    {
        private string root;
        [SetUp] public void Setup(){root=Path.Combine(Path.GetTempPath(),"Aevoria-EditTests-"+Guid.NewGuid().ToString("N"));}
        [TearDown] public void Cleanup(){if(Directory.Exists(root)) Directory.Delete(root,true);}
        private static WorldRecord Record()
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset("archive",WorldSize.Small));var clock=new SimulationClock(new WorldState("archive"));clock.Start();clock.SetSpeed(4);clock.Update(3.125);clock.Pause();
            return WorldRecord.Capture(null,"A world",null,geo,clock,ApplicationVersion.Number);
        }
        [Test] public void ArchiveRoundTripRetainsClockAndFingerprint()
        {
            var record=Record();var repo=new WorldRepository(root);repo.SaveAsync(record,false).GetAwaiter().GetResult();var loaded=repo.LoadAsync(record.Id).GetAwaiter().GetResult();
            Assert.AreEqual(record.Tick,loaded.Record.Tick);Assert.AreEqual(record.Backlog,loaded.Record.RestoreClock().BacklogSeconds);Assert.AreEqual(record.RandomState,loaded.Record.RandomState);
            Assert.AreEqual(record.Fingerprint,WorldRecord.GeographyFingerprint(loaded.Record.Rebuild()));Assert.IsFalse(loaded.Record.RestoreClock().Running);
        }
        [Test] public void RecoveryPreservesCorruptPrimaryAndIgnoresIncompleteTemp()
        {
            var record=Record();var repo=new WorldRepository(root);repo.SaveAsync(record).GetAwaiter().GetResult();record.Name="Second";repo.SaveAsync(record).GetAwaiter().GetResult();
            string path=Path.Combine(root,record.Id,"world.xml");File.WriteAllText(path+".tmp","incomplete");Assert.AreEqual("Second",(repo.LoadAsync(record.Id).GetAwaiter().GetResult()).Record.Name);
            File.WriteAllText(path,"damaged");var loaded=repo.LoadAsync(record.Id).GetAwaiter().GetResult();Assert.IsTrue(loaded.RecoveredBackup);Assert.AreEqual("A world",loaded.Record.Name);Assert.AreEqual("damaged",File.ReadAllText(path));
            Assert.Throws<InvalidOperationException>(()=>repo.SaveAsync(record).GetAwaiter().GetResult());
        }
        [Test] public void FutureAndGeneratorIncompatibleSavesAreNeverSilentlyRegenerated()
        {
            var record=Record();var repo=new WorldRepository(root);repo.SaveAsync(record).GetAwaiter().GetResult();record.Schema=999;
            File.WriteAllBytes(Path.Combine(root,record.Id,"world.xml"),ArchiveCodec.Pack(record));Assert.Throws<NotSupportedException>(()=>repo.LoadAsync(record.Id).GetAwaiter().GetResult());
            Assert.AreEqual(1,(repo.ListAsync().GetAwaiter().GetResult()).Count);record.Schema=WorldRecord.CurrentSchema;record.Generator=3;Assert.Throws<NotSupportedException>(()=>record.Rebuild());
        }
        [Test] public void DuplicateNamesRemainDistinctAndMetadataEnumerationDoesNotReadWorldPayloads()
        {
            var a=Record();var b=Record();var repo=new WorldRepository(root);repo.SaveAsync(a).GetAwaiter().GetResult();repo.SaveAsync(b).GetAwaiter().GetResult();File.WriteAllText(Path.Combine(root,a.Id,"world.xml"),"invalid payload");
            var listings=repo.ListAsync().GetAwaiter().GetResult();Assert.AreEqual(2,listings.Count);Assert.AreNotEqual(a.Id,b.Id);Assert.AreEqual(a.Name,b.Name);
            Assert.Throws<ArgumentException>(()=>repo.DeleteAsync("../outside").GetAwaiter().GetResult());repo.DeleteAsync(a.Id).GetAwaiter().GetResult();Assert.AreEqual(1,(repo.ListAsync().GetAwaiter().GetResult()).Count);
        }
        [Test] public void MountainCeilingAndCalendarAreExplicitAndValidated()
        {
            Assert.AreEqual(16000,new GenerationSettings(mountains:16000).Mountains);Assert.Throws<ArgumentException>(()=>new GenerationSettings(mountains:16001));
            var calendar=new WorldCalendar(1440*2+4*181);Assert.AreEqual(3,calendar.Year);Assert.AreEqual(182,calendar.Day);Assert.AreEqual(2,calendar.Season);
        }
        [Test] public void DrainageHasNoCyclesAndFlowNeverClimbsItsFilledSurface()
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset("hydrology",WorldSize.Small));var h=geo.Hydrology;var rank=new int[h.Ground.Length];
            for(int n=0;n<rank.Length;n++) rank[h.FloodOrder[n]]=n;
            for(int i=0;i<rank.Length;i++){int r=h.Receiver[i];if(r<0) continue;Assert.Less(rank[r],rank[i]);Assert.LessOrEqual(h.Water[r],h.Water[i]);Assert.GreaterOrEqual(h.Accumulation[r],h.Accumulation[i]);}
            Assert.Greater(h.RiverSegments,0);Assert.Greater(h.LakeCount,0);
        }
        [Test] public void ClimateAndWeatherReconstructAndSettingsValidate()
        {
            var a=new ProceduralGeography(GenerationSettings.Preset("environment",WorldSize.Small));var b=new ProceduralGeography(a.Configuration);
            Assert.AreEqual(a.Sample(new GeoPoint(500,500)).Moisture,b.Sample(new GeoPoint(500,500)).Moisture);
            Assert.AreEqual(new WorldWeather(a,300,new GeoPoint(500,500)).Kind,new WorldWeather(b,300,new GeoPoint(500,500)).Kind);
            Assert.Throws<ArgumentException>(()=>new ClimateSettings(rainfall:float.NaN));Assert.Throws<ArgumentException>(()=>new ApplicationSettings {ZoomSensitivity=float.NaN}.Validate());
        }
        [Test] public void RiverBendsShareTheirCarvedSurfaceAndSceneryHasNoRectangularAlgorithmEdge()
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset("river-borders",WorldSize.Small));var h=geo.Hydrology;int checkedPoints=0;
            for(int i=0;i<h.Ground.Length;i+=11) if(h.IsRiver(i))
            {
                int r=h.Receiver[i];for(int p=0;p<=h.PathSamples(i);p++)
                {
                    h.PathPoint(i,p,out double x,out double y);float surface=h.PathLevel(i,p/(double)h.PathSamples(i));
                    Assert.LessOrEqual(geo.ElevationAt(x,y),surface+0.02f);checkedPoints++;
                }
            }
            Assert.Greater(checkedPoints,20);
            var scenery=typeof(Aevoria.Presentation.WorldRenderer).GetMethod("SceneryHeight",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            for(int i=0;i<=16;i++)
            {
                double coordinate=(i/16.0-0.5)*geo.Configuration.WidthMeters;
                double edge=geo.Configuration.WidthMeters/2;
                Assert.AreEqual(geo.BaseElevationAt(edge,coordinate),(float)scenery.Invoke(null,new object[]{geo,edge+0.01,coordinate}),3,"Scenery must continue the field at an eastern simulation edge");
                edge=geo.Configuration.DepthMeters/2;
                Assert.AreEqual(geo.BaseElevationAt(coordinate,edge),(float)scenery.Invoke(null,new object[]{geo,coordinate,edge+0.01}),3,"Scenery must continue the field at a northern simulation edge");
            }
        }
    }
}

