using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using Aevoria.World;

var config=GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(mountains:16000,landmass:LandmassType.Continents));
var continents=new ContinentalField(config,WorldGenerationSeeds.Derive(config.Seed,WorldLayerIds.Continents,ContinentalField.Version));
var ordinary=new BaseElevationField(config,continents,WorldGenerationSeeds.Derive(config.Seed,WorldLayerIds.Elevation,BaseElevationField.Version));
var mountains=new MountainClusterField(config,continents,WorldGenerationSeeds.Derive(config.Seed,WorldLayerIds.Mountains,MountainClusterField.Version));
var surface=new ComposedSurfaceField(config,ordinary,mountains);
var climate=new ClimateMap(config,surface,new ClimateSettings());
var slopes=new List<double>();using var bytes=new MemoryStream();using var writer=new BinaryWriter(bytes);
for(int z=1;z<129;z++)for(int x=1;x<129;x++)
{
    double east=(x/129.0-.5)*config.WidthMeters,north=(z/129.0-.5)*config.DepthMeters;
    writer.Write(continents.CoastDistanceAt(east,north));writer.Write(ordinary.ElevationAt(east,north));writer.Write(surface.ElevationAt(east,north));
    var c=climate.Sample(east,north,surface.ElevationAt(east,north));writer.Write(c.TemperatureCelsius);writer.Write(c.Moisture);
    if(continents.CoastDistanceAt(east,north)<2000)continue;
    double dx=(ordinary.ElevationAt(east+32,north)-ordinary.ElevationAt(east-32,north))/64,dz=(ordinary.ElevationAt(east,north+32)-ordinary.ElevationAt(east,north-32))/64;
    slopes.Add(Math.Sqrt(dx*dx+dz*dz));
}
slopes.Sort();writer.Flush();
Console.WriteLine(JsonSerializer.Serialize(new{sha256=Convert.ToHexString(SHA256.HashData(bytes.ToArray())),samples=slopes.Count,median=slopes[slopes.Count/2],p95=slopes[(int)(slopes.Count*.95)]}));
