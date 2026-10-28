using System;
using System.Linq;
using Aevoria.World;
using Aevoria.Persistence;
using Aevoria.Simulation;
using NUnit.Framework;

namespace Aevoria.Tests
{
    public sealed class EcologyTests
    {
        private ProceduralGeography geo;
        [Test] public void ColdMoistBorealForestHasPersistentEcologicalBiomass()
        {
            var cold=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents)),new ClimateSettings(-3,1.3f,43));
            var model=new EcologyModel(cold);var candidates=Enumerable.Range(0,model.Count).Where(i=>model.EnvironmentAt(i).Biome==Biome.BorealForest&&model.EnvironmentAt(i).Temperature<0&&model.EnvironmentAt(i).Temperature>=-6&&model.EnvironmentAt(i).Moisture>.5).ToArray();
            Assert.IsNotEmpty(candidates);Assert.IsTrue(candidates.Any(i=>model.Forest(i)>.05),"Cold forest classification must survive the ecological rendering authority");
            model.AdvanceTo(1440,12);Assert.IsTrue(candidates.Any(i=>model.Forest(i)>.05));var restored=new EcologyModel(cold,model.Capture());Assert.AreEqual(Hash(model),Hash(restored));
        }
        [OneTimeSetUp] public void Setup()=>geo=new ProceduralGeography(GenerationSettings.Preset("ecosystem-005",WorldSize.Small));
        private static string Hash(EcologyModel e)=>ArchiveCodec.Hash(ArchiveCodec.Encode(e.Capture()));
        [Test] public void InitializationAndCatchupAreDeterministic()
        {var a=new EcologyModel(geo);var b=new EcologyModel(geo);a.AdvanceTo(1440,12);for(int k=1;k<=12;k++)b.AdvanceTo(k*120);Assert.AreEqual(Hash(a),Hash(b));}
        [Test] public void MigrationConservesAllSpecies()
        {var e=new EcologyModel(geo);var before=Enumerable.Range(0,4).Select(e.TotalPopulation).ToArray();e.Migrate();for(int s=0;s<4;s++)Assert.AreEqual(before[s],e.TotalPopulation(s),1e-8);}
        [Test] public void OceanRegionsHaveNoLandConnections()
        {var e=new EcologyModel(geo);for(int i=0;i<e.Count;i++)if(!e.EnvironmentAt(i).Land)Assert.AreEqual(0,e.NeighborCount(i));}
        [Test] public void SnapshotAndInspectionCannotMutateAuthority()
        {var e=new EcologyModel(geo);string hash=Hash(e);e.Capture().Regions[0].Animals[0]=999;e.Region(0).Plants[0]=999;Assert.AreEqual(hash,Hash(e));}
        [Test] public void DamagedAndFutureEcologyAreRejected()
        {var s=new EcologyModel(geo).Capture();s.Regions[0].Moisture=double.NaN;Assert.Throws<ArgumentException>(()=>new EcologyModel(geo,s));s.Version=99;Assert.Throws<NotSupportedException>(()=>new EcologyModel(geo,s));}
        [Test] public void DisturbancesEndAndPersistRecovery()
        {var e=new EcologyModel(geo);int i=Enumerable.Range(0,e.Count).First(n=>e.EnvironmentAt(n).Land&&e.Moisture(n)<.6&&e.Biomass(n)>.15);Assert.IsTrue(e.StartEvent(i,DisturbanceKind.Fire,.9,1));e.AdvanceTo(120);Assert.AreEqual(DisturbanceKind.None,e.ActiveEvent(i));Assert.Greater(e.Burn(i),.8);Assert.IsFalse(e.StartEvent(Enumerable.Range(0,e.Count).First(n=>!e.EnvironmentAt(n).Land),DisturbanceKind.Fire,.9,1));}
        [Test] public void SavingActiveEventsContinuesExactly()
        {var e=new EcologyModel(geo);int i=Enumerable.Range(0,e.Count).First(n=>e.EnvironmentAt(n).Land);e.StartEvent(i,DisturbanceKind.Drought,.8,6);e.AdvanceTo(120);var clock=new SimulationClock(WorldState.Restore(geo.Configuration.Seed,120,24,100,1));var r=WorldRecord.Capture(null,"Test",null,geo,clock,"0.0.5",e);var copy=new EcologyModel(geo,ArchiveCodec.Unpack(ArchiveCodec.Pack(r)).Ecology);e.AdvanceTo(14400,120);copy.AdvanceTo(14400,120);Assert.AreEqual(Hash(e),Hash(copy));}
        [Test] public void HistoryIsBoundedAndIdsNeverRepeat()
        {var e=new EcologyModel(geo,0,16);e.AdvanceTo(14400,120);var s=e.Capture();EcologyModel.ValidateSnapshot(s,geo.Configuration.Seed,e.Resolution);Assert.LessOrEqual(s.History.Count(ev=>ev.EndTick>0),16);Assert.AreEqual(s.History.Count,s.History.Select(ev=>ev.Id).Distinct().Count());}
        [Test] public void SeasonsRespectHemispheresAndTropics()
        {var tropical=new ProceduralGeography(GenerationSettings.Preset("hemispheres",WorldSize.Small),new ClimateSettings(latitude:0));var e=new EcologyModel(tropical);Assert.AreNotEqual(e.Temperature(0,360)-e.EnvironmentAt(0).Temperature>0,e.Temperature(e.Count-1,360)-e.EnvironmentAt(e.Count-1).Temperature>0);StringAssert.Contains("season",e.Season(e.Count/2,360));}
        [Test] public void MissingEcologyAndOldSchemaCannotSilentlyReset()
        {var c=new SimulationClock(new WorldState(geo.Configuration.Seed));var r=WorldRecord.Capture(null,"World",null,geo,c,"0.0.5");r.Schema=2;Assert.Throws<NotSupportedException>(()=>r.Validate());r.Schema=3;r.Ecology=null;Assert.Throws<NotSupportedException>(()=>r.Validate());}
    }
}
