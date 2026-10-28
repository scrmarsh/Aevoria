using System.Collections.Generic;
using System.Linq;
using Aevoria.World;
using Aevoria.Persistence;
using Aevoria.Simulation;
using NUnit.Framework;
namespace Aevoria.Tests
{
    public sealed class GenerationPreviewTests
    {
        [Test]public void CompletedStageSnapshotsMatchCurrentWorldAndVerifiedArchive()
        {
            var snapshots=new List<GenerationPreview>();float[] first=null;
            var geo=new ProceduralGeography(GenerationSettings.Preset("loading-preview-009",WorldSize.Miniature),preview:p=>{snapshots.Add(p);if(first==null)first=(float[])p.Elevation.Clone();});
            Assert.AreEqual(10,snapshots.Count);Assert.AreEqual("Continental outlines",snapshots[0].Stage);Assert.IsNull(snapshots[0].Temperature);Assert.IsNotNull(snapshots.Single(p=>p.Stage=="Climate regions").Temperature);CollectionAssert.AreEqual(first,snapshots[0].Elevation,"Later generation must not mutate an already published preview");
            foreach(var snapshot in snapshots){Assert.AreEqual(geo.Configuration.Seed,snapshot.Seed);Assert.AreEqual(GenerationPreview.Resolution*GenerationPreview.Resolution,snapshot.Elevation.Length);Assert.IsTrue(snapshot.Elevation.All(float.IsFinite));}
            int n=GenerationPreview.Resolution;Assert.AreEqual(geo.ElevationAt(0,0),snapshots.Last().Elevation[n/2*n+n/2]);
            Assert.IsTrue(snapshots.Single(p=>p.Stage=="Connected drainage and water").Water.Any(w=>w>0));Assert.IsNotNull(snapshots.Single(p=>p.Stage=="Biome regions").Biomes);Assert.IsNotNull(snapshots.Single(p=>p.Stage=="Vegetation habitats").Forest);
            var society=new CivilizationModel(geo);var founded=GenerationPreview.ForSettlements(geo,society);Assert.AreEqual(society.Settlements.Count,founded.Settlements.Length);Assert.IsTrue(founded.Settlements.Select(p=>(p.X,p.Y)).SequenceEqual(society.Settlements.Select(s=>(s.X,s.Z))));
            var record=WorldRecord.Capture(null,"Preview reconstruction",null,geo,new SimulationClock(new WorldState(geo.Configuration.Seed)),"0.0.11");var rebuilt=record.Rebuild(preview:p=>Assert.AreEqual(geo.Configuration.Seed,p.Seed));Assert.AreEqual(record.Fingerprint,WorldRecord.GeographyFingerprint(rebuilt));
            var saved=new LivingWorldSnapshot();saved.Traveler.ReverseThirdPerson=true;Assert.IsTrue(LivingWorldState.Clone(ArchiveCodec.Decode<LivingWorldSnapshot>(ArchiveCodec.Encode(saved))).Traveler.ReverseThirdPerson,"Optional reverse perspective survives serialization and deep cloning");
        }
    }
}
