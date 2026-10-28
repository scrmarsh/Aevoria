using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aevoria.World;

if(args.Contains("--basins"))
{
    string seed=args.Length>2?args[2]:"aevoria-001";var size=args.Length>3?Enum.Parse<WorldSize>(args[3]):WorldSize.Miniature;var config=GenerationSettings.Preset(seed,size,new GenerationSettings(landmass:LandmassType.Continents));var generation=Stopwatch.StartNew();
    var land=new ContinentalField(config,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Continents,ContinentalField.Version));var geology=new GeologyMap(config,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Geology,GeologyMap.Version));var faults=new TectonicStructureField(config,land,geology,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Tectonics,TectonicStructureField.Version));var ordinary=new BaseElevationField(config,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Elevation,BaseElevationField.Version),faults);var mountains=new MountainClusterField(config,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Mountains,MountainClusterField.Version),tectonics:faults);var coast=new CoastalLandformField(config,land,ordinary,mountains,faults,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Coasts,CoastalLandformField.Version));var surface=new ComposedSurfaceField(config,ordinary,mountains,coast);var climate=new ClimateMap(config,surface,new ClimateSettings());var h=new HydrologyMap(config,surface,climate);var cache=new Dictionary<(double,double),float>();var water=new InlandWaterLayout(h,buildGeometry:false,terrainSamples:cache);var states=new HashSet<ulong>();int passes=0;
    while(true){if(!states.Add(h.StateIdentity()))throw new Exception("Repeated basin state");int changed=h.BreachRejectedBasins(water);if(changed==0)break;Console.WriteLine("Settling "+(++passes));water=new InlandWaterLayout(h,buildGeometry:false,terrainSamples:cache);}
    object summaries=typeof(InlandWaterLayout).GetProperty("LakeSummaries")?.GetValue(water);int eroded=(int)(typeof(HydrologyMap).GetProperty("ErodedSmallBasins")?.GetValue(h)??0);
    File.WriteAllText(args[0],JsonSerializer.Serialize(new{seed,size=size.ToString(),climate=ClimateMap.Version,hydrology=HydrologyMap.Version,lakes=water.LakeCount,eroded,passes,seconds=generation.Elapsed.TotalSeconds,summaries},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("BASINS "+water.LakeCount+" ERODED "+eroded);return;
}

if(args.Contains("--world"))
{
    string seed=args.Length>2?args[2]:"aevoria-001";var generation=Stopwatch.StartNew();var config=GenerationSettings.Preset(seed,WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents));
    var geo=new ProceduralGeography(config,progress:s=>Console.WriteLine("STAGE "+s));var h=geo.Hydrology;
    var result=new{seed,hydrology=HydrologyMap.Version,layout=InlandWaterLayout.Version,acceptedLakes=geo.InlandSurfaces.LakeCount,potentialBasins=h.PotentialLake.Max()+1,eroded=(int)(typeof(HydrologyMap).GetProperty("ErodedSmallBasins")?.GetValue(h)??0),riverSegments=h.RiverSegments,lakeTriangles=geo.InlandSurfaces.Lakes.Indices.Count/3,riverTriangles=geo.InlandSurfaces.Rivers.Indices.Count/3,seconds=generation.Elapsed.TotalSeconds};
    File.WriteAllText(args[0],JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(result));return;
}

var results=new List<object>();int checks=0;var watch=Stopwatch.StartNew();
void Check(bool value,string reason){if(!value)throw new Exception(reason);checks++;Console.WriteLine("PASS "+reason);}
foreach(var fixture in new[]{(name:"small-shallow",radius:55.0,depth:6.0,slope:.02),(name:"small-cold",radius:55.0,depth:6.0,slope:.02),(name:"small-deep",radius:55.0,depth:17.0,slope:.02),(name:"large-lake",radius:300.0,depth:10.0,slope:.005)})
{
    var config=new WorldConfiguration("erosion-013-"+fixture.name,8,8,64,8,new GenerationSettings(mountains:0));
    var surface=new Bowl(fixture.radius,fixture.depth,fixture.slope);var climate=new ClimateMap(config,surface,new ClimateSettings(temperatureOffset:fixture.name=="small-cold"?-15:10,rainfall:2));
    var h=new HydrologyMap(config,surface,climate);int initial=h.LakeCount;var original=(int[])h.Receiver.Clone();var states=new HashSet<ulong>();int passes=0;
    var water=new InlandWaterLayout(h,buildGeometry:false);
    while(true){Check(states.Add(h.StateIdentity()),"Drainage settling progresses: "+fixture.name+" pass"+passes);int changed=h.BreachRejectedBasins(water);if(changed==0)break;passes++;water=new InlandWaterLayout(h,buildGeometry:false);}
    long conserved=0;bool ordered=true;var rank=new int[h.Ground.Length];for(int i=0;i<rank.Length;i++)rank[h.FloodOrder[i]]=i;
    for(int i=0;i<h.Ground.Length;i++){int r=h.Receiver[i];if(r<0)conserved+=h.Accumulation[i];else ordered&=rank[r]<rank[i]&&h.Water[r]<=h.Water[i]+.001f;}
    Check(ordered,"Every receiver is acyclic and its water level is downstream: "+fixture.name);
    Check(conserved==h.Ground.Length,"Catchments conserve every cell: "+fixture.name);
    int eroded=(int)(typeof(HydrologyMap).GetProperty("ErodedSmallBasins")?.GetValue(h)??0),changedReceivers=original.Where((r,i)=>r!=h.Receiver[i]).Count();
    if(HydrologyMap.Version>=13&&fixture.name=="small-shallow")
    {
        Check(initial>0&&water.LakeCount==0&&eroded>0,"A shallow small pond drains through actual sill erosion");
        Check(changedReceivers==0,"Shallow-basin erosion preserves every original tributary receiver");
        Check(GroundContactPatch.SurfaceHeight(h,8,-64,0)<surface.ElevationAt(-64,0)-1,"The former sill is incised on the physical contact surface");
    }
    if(fixture.name!="small-shallow")Check(water.LakeCount>0,"A supported cold, deep or larger lake is preserved: "+fixture.name);
    results.Add(new{fixture.name,initialLakes=initial,finalLakes=water.LakeCount,eroded,changedReceivers,riverSegments=h.RiverSegments,passes});
    Console.WriteLine($"RESULT {fixture.name}: {initial}->{water.LakeCount} lakes, {eroded} eroded, {changedReceivers} changed receivers");
}
File.WriteAllText(args[0],JsonSerializer.Serialize(new{hydrology=HydrologyMap.Version,layout=InlandWaterLayout.Version,checks,seconds=watch.Elapsed.TotalSeconds,results},new JsonSerializerOptions{WriteIndented=true}));
sealed class Bowl:IWorldHeightField
{
    private readonly double radius,depth,slope;public Bowl(double radius,double depth,double slope){this.radius=radius;this.depth=depth;this.slope=slope;}
    public float ElevationAt(double x,double z)=>(float)(20+x*slope-depth*Math.Exp(-(x*x+z*z)/(radius*radius)));
}
