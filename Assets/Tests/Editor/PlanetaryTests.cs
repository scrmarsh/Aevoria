using System;
using System.Linq;
using System.Threading;
using Aevoria.World;
using Aevoria.Persistence;
using NUnit.Framework;
namespace Aevoria.Tests
{
    public sealed class PlanetaryTests
    {
        private ProceduralGeography geo;private EcologyModel eco;
        [OneTimeSetUp] public void Setup(){geo=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Medium));eco=new EcologyModel(geo);}
        [Test] public void CancelledPlanetaryConstructionStopsBeforeAllocatingTheWorld()
        {using(var c=new CancellationTokenSource()){c.Cancel();Assert.Throws<OperationCanceledException>(()=>new ProceduralGeography(GenerationSettings.Preset("cancel",WorldSize.Colossal),token:c.Token));}}
        [Test] public void GenerationReportsRealOrderedStages()
        {var stages=new System.Collections.Generic.List<string>();new ProceduralGeography(GenerationSettings.Preset("stages",WorldSize.Miniature),progress:stages.Add);var fixedStages=stages.Where(stage=>!stage.StartsWith("Settling drainage pass")).ToArray();var expected=new[]{"Continents","Tectonic","Mountain","Latitude","Drainage","Refined","Triangulating","Biome","Surface materials","Vegetation habitats","Settlement sites"};Assert.AreEqual(expected.Length,fixedStages.Length);for(int i=0;i<expected.Length;i++)StringAssert.StartsWith(expected[i],fixedStages[i]);int pass=0;foreach(var stage in stages.Where(stage=>stage.StartsWith("Settling drainage pass")))StringAssert.StartsWith("Settling drainage pass "+(++pass)+": ",stage);}
        [Test,Timeout(600000)] public void HierarchyStaysBoundedWhilePlateCountGrows()
        {var small=new ProceduralGeography(GenerationSettings.Preset("bounds",WorldSize.Miniature));var large=new ProceduralGeography(GenerationSettings.Preset("bounds",WorldSize.Colossal));Assert.AreEqual(small.Geology.FieldBytes,large.Geology.FieldBytes);Assert.Less(large.Geology.FieldBytes,4000000,"Plateau and physical shoreline-gradient fields remain bounded independently of world area");Assert.Greater(large.Geology.Plates.Count,small.Geology.Plates.Count);Assert.AreEqual(small.Hydrology.Ground.Length,large.Hydrology.Ground.Length);Assert.AreEqual(64,new EcologyModel(large).Resolution);}
        [Test] public void EveryChannelHasADownhillContinuationOrAcceptedWaterOutlet()
        {var h=geo.Hydrology;for(int i=0;i<h.Ground.Length;i++){int r=h.Receiver[i];if(r<0)continue;Assert.GreaterOrEqual(h.Water[i]+.001f,h.Water[r]);if(h.IsRiver(i))Assert.IsTrue(h.IsRiver(r)||h.Ground[r]<=geo.SeaLevel||geo.InlandSurfaces.LakeAt(h.X(r),h.Y(r),out _)||geo.InlandSurfaces.HasTerminalPool(r)&&h.IsRiverWater(h.X(r),h.Y(r),geo.ElevationAt(h.X(r),h.Y(r)))||geo.InlandSurfaces.IsSeasonallyDryChannel(i),"Dry outlet at "+i);}}
        [Test] public void ClimateHasBroadLatitudinalAndElevationGradients()
        {double d=geo.Configuration.DepthMeters;Assert.Greater(geo.Climate.Sample(0,0,0).TemperatureCelsius,geo.Climate.Sample(0,d*.45,0).TemperatureCelsius+20);Assert.AreEqual(13,geo.Climate.Sample(0,0,0).TemperatureCelsius-geo.Climate.Sample(0,0,2000).TemperatureCelsius,.01);}
        [Test] public void GeologicalRegolithPotentialIsBoundedAndVariesAcrossLand()
        {float low=1,high=0;for(int z=0;z<65;z++)for(int x=0;x<65;x++){double e=(x/64.0-.5)*geo.Configuration.WidthMeters,n=(z/64.0-.5)*geo.Configuration.DepthMeters;float soil=geo.Geology.Soil(e,n);Assert.That(soil,Is.InRange(0,1));if(geo.Continents.CoastDistanceAt(e,n)>2000){low=Math.Min(low,soil);high=Math.Max(high,soil);}}Assert.Greater(high-low,.05,"Static weathered regolith is a material field; discarded legacy height erosion is not reported as executed work.");}
        [Test] public void RegionalCloudsAndRainAreCoherentAndSpatiallyDistinct()
        {var a=RegionalAtmosphere.Forecast(geo,200,new GeoPoint(0,0));var b=RegionalAtmosphere.Forecast(geo,200,new GeoPoint(1,1));Assert.AreEqual(a.Cloud,b.Cloud,.001);Assert.AreEqual(a.Precipitation,b.Precipitation,.002);bool different=false;for(int i=0;i<eco.Count;i+=13){var w=eco.WeatherAt(i,200);Assert.IsTrue(w.Precipitation<=0||w.Cloud>.64);different|=Math.Abs(w.Cloud-a.Cloud)>.1;}Assert.IsTrue(different);}
        [Test] public void SavedWetnessContinuesExactlyAcrossDailyCatchupCadences()
        {var a=new RegionalAtmosphere(geo,eco,0);a.AdvanceTo(40,20);var b=new RegionalAtmosphere(geo,eco,40,a.Capture());a.AdvanceTo(120,30);for(int tick=44;tick<=120;tick+=4)b.AdvanceTo(tick,1);CollectionAssert.AreEqual(a.Capture().Wetness,b.Capture().Wetness);Assert.Greater(a.Capture().Wetness.Max(),0);}
        [Test] public void WetGroundDriesGraduallyAndRejectsNonfiniteState()
        {var a=new RegionalAtmosphere(geo,eco,0);var saved=a.Capture();for(int i=0;i<saved.Wetness.Length;i++)saved.Wetness[i]=1;var wet=new RegionalAtmosphere(geo,eco,0,saved);wet.AdvanceTo(4);Assert.IsTrue(wet.Capture().Wetness.Any(v=>v>0&&v<1));saved.Wetness[0]=float.NaN;Assert.Throws<ArgumentException>(()=>new RegionalAtmosphere(geo,eco,0,saved));}
        [Test] public void IndependentGraphicsSettingsValidateAndRoundTrip()
        {var s=new ApplicationSettings{TerrainDetail=3,TerrainDistance=10,DistantDetail=2,WaterQuality=0,CloudQuality=2,FogEnabled=false,FogDensity=0,VegetationDensity=1.7f,ShadowDistance=4000};var copy=ArchiveCodec.Decode<ApplicationSettings>(ArchiveCodec.Encode(s));copy.Validate();Assert.IsFalse(copy.FogEnabled);Assert.AreEqual(0,copy.WaterQuality);Assert.AreEqual(2,copy.CloudQuality);copy.VegetationDensity=float.NaN;Assert.Throws<ArgumentException>(()=>copy.Validate());}
    }
}
