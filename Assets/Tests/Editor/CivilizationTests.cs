using System;
using System.IO;
using System.Linq;
using System.Globalization;
using Aevoria.Persistence;
using Aevoria.World;
using NUnit.Framework;

namespace Aevoria.Tests
{
    public sealed class CivilizationTests
    {
        private ProceduralGeography geography;
        private CivilizationSnapshot initial;
        private CivilizationHistoryArchive archive;
        [OneTimeSetUp] public void Prepare()
        {
            geography=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents)));
            archive=new CivilizationHistoryArchive(Path.Combine(Path.GetTempPath(),"Aevoria-SocietyEditor-"+Guid.NewGuid().ToString("N")));
            initial=new CivilizationModel(geography,settings:new CivilizationSettings{CandidateResolution=32,RequestedCivilizations=4},archive:archive).Capture();
        }
        [Test] public void FounderIdentitiesAndFootprintsUseRealGeography()
        {
            Assert.AreEqual(4,initial.Civilizations.Count);Assert.AreEqual(4,initial.Civilizations.Select(c=>c.Id).Distinct().Count());
            foreach(var s in initial.Settlements){Assert.IsTrue(initial.Civilizations.Any(c=>c.Id==s.CivilizationId&&c.CapitalSettlementId==s.Id));Assert.IsTrue(CivilizationGeography.Footprint(geography,s.X,s.Z,28,out _));Assert.GreaterOrEqual(s.Buildings.Count,3);foreach(var b in s.Buildings)Assert.IsTrue(CivilizationGeography.Footprint(geography,b.X,b.Z,4,out _));}
            Assert.IsFalse(CivilizationGeography.Footprint(geography,geography.Configuration.WidthMeters,0,4,out _));
        }
        [Test] public void ResidentsPersistHealthDeathsAndStableIdentitiesWithoutChangingResourceStores()
        {
            var model=new CivilizationModel(geography,initial,archive);var settlement=model.Settlements[0];var resident=settlement.Residents[0];string dead=resident.Id;
            double population=model.TotalPopulation,local=settlement.Population;string stores=ArchiveCodec.Hash(ArchiveCodec.Encode(settlement.Resources));
            Assert.IsTrue(model.DamageResident(settlement.Id,dead,25));Assert.AreEqual(75,resident.Health);Assert.AreEqual(population,model.TotalPopulation);
            var injured=new CivilizationModel(geography,ArchiveCodec.Decode<CivilizationSnapshot>(ArchiveCodec.Encode(model.Capture())),archive);
            Assert.AreEqual(75,injured.Settlements[0].Residents.First(r=>r.Id==dead).Health,"Nonlethal damage survives the real archive codec");
            Assert.IsTrue(injured.DamageResident(settlement.Id,dead,75));Assert.AreEqual(local-1,injured.Settlements[0].Population);Assert.AreEqual(population-1,injured.TotalPopulation);
            Assert.AreEqual(stores,ArchiveCodec.Hash(ArchiveCodec.Encode(injured.Settlements[0].Resources)),"Presentation deaths do not invent or consume civic resources");
            Assert.IsFalse(injured.DamageResident(settlement.Id,dead,100),"A stale streamed collider cannot kill the same identity twice");
            var saved=ArchiveCodec.Decode<CivilizationSnapshot>(ArchiveCodec.Encode(injured.Capture()));var restored=new CivilizationModel(geography,saved,archive);
            Assert.IsFalse(restored.Settlements[0].Residents.Any(r=>r.Id==dead));Assert.AreEqual(1,restored.Settlements[0].ResidentDeaths);
            restored.AdvanceTo(restored.Tick+CivilizationModel.TicksPerMonth);Assert.IsFalse(restored.Settlements[0].Residents.Any(r=>r.Id==dead),"New representatives use monotonically new identities");
            Assert.LessOrEqual(restored.Settlements[0].Residents.Count,Math.Min(CivilizationModel.ResidentRosterLimit,(int)restored.Settlements[0].Population));
            Assert.IsTrue(restored.RecentHistory.Any(e=>e.Kind==SocialEventKind.ResidentKilled&&e.Before-e.After==1));
            var bad=restored.Capture();bad.Settlements[0].Residents[0].Health=101;Assert.Throws<ArgumentException>(()=>new CivilizationModel(geography,bad,archive));
        }
        [Test] public void FoundingHousingHasRealCapacityAndOldSocietiesRequireTheirOriginalVersion()
        {
            foreach(var settlement in initial.Settlements)
            {
                int houses=settlement.Buildings.Count(b=>b.Kind==CivicBuildingKind.Dwelling);Assert.Greater(houses,5,"A founding party receives more than a few oversized proxy homes");
                foreach(var home in settlement.Buildings.Where(b=>b.Kind==CivicBuildingKind.Dwelling))Assert.That(home.HousingCapacity,Is.InRange(6,12));
                Assert.GreaterOrEqual(CivilizationModel.HousingCapacity(settlement),settlement.Population*.8,"Geographically buildable founding sites house most of their actual population");
                Assert.IsTrue(settlement.Buildings.Any(b=>b.Kind==CivicBuildingKind.Farm));
            }
            var old=initial.Copy();old.Version=1;StringAssert.Contains("original release",Assert.Throws<ArgumentException>(()=>new CivilizationModel(geography,old,archive)).Message);
        }
        [Test] public void SettlementPathsConnectRealDoorsWithoutCrossingBuildingsOrFreshwater()
        {
            var settlement=initial.Settlements[0];var network=SettlementPaths.Build(geography,settlement);var again=SettlementPaths.Build(geography,settlement);
            Assert.GreaterOrEqual(network.Paths.Count,settlement.Buildings.Count/2,"Most homes join the walkable village network");Assert.AreEqual(network.Paths.Count,again.Paths.Count);
            var connected=new System.Collections.Generic.HashSet<string>{settlement.Buildings[0].Id};
            for(int path=0;path<network.Paths.Count;path++)
            {
                var route=network.Paths[path];Assert.IsTrue(connected.Contains(route.From));Assert.IsTrue(connected.Add(route.To));Assert.AreEqual(route.Points.Count,again.Paths[path].Points.Count);
                var from=CivicGeometry.Door(settlement.Buildings.First(b=>b.Id==route.From));var to=CivicGeometry.Door(settlement.Buildings.First(b=>b.Id==route.To));
                Assert.AreEqual(from.X,route.Points[0].X);Assert.AreEqual(from.Y,route.Points[0].Y);Assert.AreEqual(to.X,route.Points[route.Points.Count-1].X);Assert.AreEqual(to.Y,route.Points[route.Points.Count-1].Y);
                for(int segment=0;segment<route.Points.Count;segment++)
                {
                    var point=route.Points[segment];Assert.AreEqual(point.X,again.Paths[path].Points[segment].X);Assert.AreEqual(point.Y,again.Paths[path].Points[segment].Y);
                    foreach(var building in settlement.Buildings)Assert.IsFalse(CivicGeometry.Contains(building,point.X,point.Y,.9),"The full road half-width clears the building footprint");
                    if(segment==0)continue;var before=route.Points[segment-1];int count=Math.Max(1,(int)Math.Ceiling(SettlementPaths.Distance(before,point)/2));
                    for(int sample=0;sample<=count;sample++){double t=sample/(double)count,x=before.X+(point.X-before.X)*t,z=before.Y+(point.Y-before.Y)*t;float h=geography.ElevationAt(x,z);Assert.Greater(h,geography.SeaLevel+2);Assert.IsFalse(geography.Hydrology.IsLakeWater(x,z,h));Assert.IsFalse(geography.Hydrology.IsRiverWater(x,z,h));foreach(var building in settlement.Buildings)Assert.IsFalse(CivicGeometry.Contains(building,x,z,.2),"Rounded resident route retains building clearance");}
                }
            }
            using(var cancel=new System.Threading.CancellationTokenSource()){cancel.Cancel();Assert.Throws<OperationCanceledException>(()=>SettlementPaths.Build(geography,settlement,cancel.Token));}
        }
        [Test] public void MonthlyEconomyIsDeterministicAcrossBudgetsAndRestore()
        {
            var a=new CivilizationModel(geography,initial,archive);var b=new CivilizationModel(geography,initial,archive);long tick=120*12*80;a.AdvanceTo(tick,int.MaxValue);while(b.Tick<tick)b.AdvanceTo(tick,3);
            Assert.AreEqual(ArchiveCodec.Hash(ArchiveCodec.Encode(a.Capture())),ArchiveCodec.Hash(ArchiveCodec.Encode(b.Capture())));Assert.IsTrue(a.Settlements.Any(s=>s.Stage>SettlementStage.Camp));
            var c=new CivilizationModel(geography,a.Capture(),archive);a.AdvanceTo(tick+120*12,int.MaxValue);c.AdvanceTo(tick+120*12,int.MaxValue);Assert.AreEqual(ArchiveCodec.Hash(ArchiveCodec.Encode(a.Capture())),ArchiveCodec.Hash(ArchiveCodec.Encode(c.Capture())));
        }
        [Test] public void ScarcityHasConsequencesAndInvalidResourcesCannotLoad()
        {
            var saved=initial.Copy();var s=saved.Settlements[0];s.Fertility=s.Freshwater=s.ClimateSuitability=0;s.Resources=new CivicInventory();double before=s.Population;var poor=new CivilizationModel(geography,saved,archive);poor.AdvanceTo(120*12,int.MaxValue);Assert.Less(poor.Settlements[0].Population,before);Assert.IsTrue(poor.Settlements[0].InCrisis);Assert.IsTrue(poor.RecentHistory.Any(e=>e.Kind==SocialEventKind.ResourceCrisis));
            saved.Settlements[0].Resources.Stone=-1;Assert.Throws<ArgumentException>(()=>new CivilizationModel(geography,saved,archive));
        }
        [Test] public void ConfigurationAndHistoricalReferencesAreValidated()
        {
            Assert.Throws<ArgumentException>(()=>new CivilizationSettings{RequestedCivilizations=65}.Validate());var broken=initial.Copy();broken.Territory[0]=100;Assert.Throws<ArgumentException>(()=>new CivilizationModel(geography,broken,archive));broken=initial.Copy();broken.RecentHistory[0].Sequence=99;Assert.Throws<ArgumentException>(()=>new CivilizationModel(geography,broken,archive));
        }
        [TestCase(0),TestCase(1),TestCase(2),TestCase(3),TestCase(4)]
        public void CorruptConstructionAndEconomyCannotEnterTheWorld(int corruption)
        {
            var broken=initial.Copy();var s=broken.Settlements[0];
            switch(corruption){case 0:s.Capacity=double.NaN;break;case 1:s.Buildings[0].Kind=(CivicBuildingKind)999;break;case 2:s.Buildings.Add(s.Buildings[0].Copy());break;case 3:s.LastExpansionTick=broken.Tick+1;break;case 4:broken.Civilizations[0].Settlements.Clear();break;}
            Assert.Throws<ArgumentException>(()=>new CivilizationModel(geography,broken,archive));
        }
        [Test] public void EventRecordsAreIndependentOfNumericLocale()
        {
            var original=CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;var a=new CivilizationModel(geography,initial,archive);a.AdvanceTo(120*12*80,int.MaxValue);var hash=ArchiveCodec.Hash(ArchiveCodec.Encode(a.Capture()));
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");var b=new CivilizationModel(geography,initial,archive);b.AdvanceTo(120*12*80,int.MaxValue);Assert.AreEqual(hash,ArchiveCodec.Hash(ArchiveCodec.Encode(b.Capture())));
            }
            finally{CultureInfo.CurrentCulture=original;}
        }
    }
}
