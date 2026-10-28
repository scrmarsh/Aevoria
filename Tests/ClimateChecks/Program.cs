using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Aevoria.World;

if(args.Length>1&&args[1]=="--terrain")
{
    var size=Enum.Parse<WorldSize>(args[2]);string seed=args.Length>3?args[3]:"aevoria-001";
    var c=GenerationSettings.Preset(seed,size,new GenerationSettings(landmass:LandmassType.Continents));
    var land=new ContinentalField(c,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Continents,ContinentalField.Version));
    var geology=new GeologyMap(c,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Geology,GeologyMap.Version));
    var faults=new TectonicStructureField(c,land,geology,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Tectonics,TectonicStructureField.Version));
    var ordinary=new BaseElevationField(c,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Elevation,BaseElevationField.Version),faults);
    var mountains=new MountainClusterField(c,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Mountains,MountainClusterField.Version),tectonics:faults);
    var coast=new CoastalLandformField(c,land,ordinary,mountains,faults,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Coasts,CoastalLandformField.Version));
    var surface=new ComposedSurfaceField(c,ordinary,mountains,coast);var climate=new ClimateMap(c,surface,new ClimateSettings());
    var zones=new List<object>();
    for(int zone=0;zone<5;zone++)
    {
        int count=0;double temperature=0,moisture=0,altitude=0;var biomes=new Dictionary<string,int>();
        for(int z=0;z<25;z++)for(int x=0;x<129;x++)
        {
            double ex=(x/128.0-.5)*c.WidthMeters,nz=((zone+(z+.5)/25)/5-.5)*c.DepthMeters;
            float h=surface.ElevationAt(ex,nz);if(h<=c.Terrain.SeaLevel+5)continue;
            var sample=BiomeField.Provisional(climate,geology,ex,nz,h,c.Terrain.SeaLevel);string biome=sample.Biome.ToString();
            biomes[biome]=biomes.GetValueOrDefault(biome)+1;count++;temperature+=sample.TemperatureCelsius;moisture+=sample.Moisture;altitude+=h;
        }
        zones.Add(new{southToNorthZone=zone,landSamples=count,meanTemperature=temperature/Math.Max(1,count),meanMoisture=moisture/Math.Max(1,count),meanElevation=altitude/Math.Max(1,count),biomes});
    }
    File.WriteAllText(args[0],JsonSerializer.Serialize(new{seed,size=size.ToString(),climate=ClimateMap.Version,scope="Physical composed surface and provisional biomes, before hydrology",zones},new JsonSerializerOptions{WriteIndented=true}));return;
}

int checks=0;var results=new List<object>();
void Check(bool condition,string reason){if(!condition)throw new Exception(reason);checks++;Console.WriteLine("PASS "+reason);}
foreach(var size in new[]{WorldSize.Miniature,WorldSize.Small,WorldSize.Medium,WorldSize.Large,WorldSize.Colossal})
{
    var c=GenerationSettings.Preset("hemisphere-013",size);var flat=new Landscape(c,0);var ridge=new Landscape(c,2500);
    var climate=new ClimateMap(c,flat,new ClimateSettings());var mountain=new ClimateMap(c,ridge,new ClimateSettings());
    double south=-c.DepthMeters*.45,north=c.DepthMeters*.45,inland=c.WidthMeters*.3,coast=-c.WidthMeters*.29;
    float hot=climate.TemperatureAt(inland,south,100),cold=climate.TemperatureAt(inland,north,100);
    Check(hot>22&&cold< -5&&hot-cold>30,"Warm south and cold north at equal elevation: "+size);
    float temperate=climate.TemperatureAt(inland,0,100);
    Check(temperate>14&&temperate<20,"Central lowlands support warm temperate woodland: "+size);
    float previous=float.MaxValue;
    for(int z=0;z<=100;z++){float t=climate.TemperatureAt(inland,(z/100.0-.5)*c.DepthMeters,100);CheckFinite(t);if(t>previous+.01f)throw new Exception("A second cold southern pole or reversed gradient");previous=t;}
    checks++;Check(mountain.TemperatureAt(0,south,10000)<0,"Southern high mountains can remain cold");
    Check(climate.TemperatureAt(coast,north,100)>cold&&climate.TemperatureAt(coast,south,100)<hot,"Ocean moderates both cold and hot coastal regions");
    double lee=c.WidthMeters*.08,windward=-lee;
    Check(mountain.RainfallAt(lee,0)<climate.RainfallAt(lee,0)-.02f,"Physical mountain barrier creates a leeward rain shadow");
    Check(mountain.RainfallAt(windward,0)>climate.RainfallAt(windward,0),"Windward terrain receives lifted moisture");
    Check(climate.RainfallAt(inland,south)<climate.RainfallAt(inland,0),"Southern subtropical lowlands are drier than temperate regions");
    foreach(float offset in new[]{-75f,75f})
    {
        var shifted=new ClimateMap(c,flat,new ClimateSettings(latitude:offset));
        Check(shifted.TemperatureAt(inland,north,100)<=shifted.TemperatureAt(inland,south,100),"Latitude setting retains one hemisphere's orientation");
    }
    results.Add(new{size=size.ToString(),southTemperature=hot,northTemperature=cold,southRain=climate.RainfallAt(inland,south),temperateRain=climate.RainfallAt(inland,0),leewardRain=mountain.RainfallAt(lee,0)});
}
Check(BiomeField.Classify(100,30,.85f,0,.3f)==Biome.TropicalForest,"Hot wet ground is not forced into desert");
File.WriteAllText(args[0],JsonSerializer.Serialize(new{climate=ClimateMap.Version,checks,results},new JsonSerializerOptions{WriteIndented=true}));
static void CheckFinite(float x){if(float.IsNaN(x)||float.IsInfinity(x))throw new Exception("Nonfinite climate field");}
sealed class Landscape:IWorldHeightField
{
    private readonly WorldConfiguration c;private readonly double relief;public Landscape(WorldConfiguration c,double relief){this.c=c;this.relief=relief;}
    public float ElevationAt(double x,double z)=>x< -c.WidthMeters*.3?-100:(float)(100+relief*Math.Exp(-Math.Pow(x/(c.WidthMeters*.025),2)));
}
