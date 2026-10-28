using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevoria.World;

if(args.Contains("--integration")){Integration.Run();return;}
if(args.Contains("--water-digest")){WaterDigest.Run(args.Contains("--colossal"));return;}
if(args.Contains("--mouth-fixture"))
{
    var world=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(mountains:3600,landmass:LandmassType.Continents)));var h=world.Hydrology;
    Console.WriteLine("node,sample,x,z,water,ground,rendered8m,coast,bank");int node=417723;
    for(int branch=0;branch<8&&node>=0&&h.Receiver[node]>=0;branch++,node=h.Receiver[node])
    for(int p=0;p<=h.PathSamples(node);p++)
    {
        h.PathPoint(node,p,out double x,out double z);h.ChannelSample(x,z,out _,out float bank,out _);
        Console.WriteLine(FormattableString.Invariant($"{node},{p},{x:F4},{z:F4},{h.PathLevel(node,p/(double)h.PathSamples(node)):F5},{world.ElevationAt(x,z):F5},{LocalSolidField.RenderedGround(world,x,z):F5},{world.Continents.CoastDistanceAt(x,z):F3},{bank:F4}"));
    }
    return;
}
if(args.Contains("--terminal-fixture"))
{
    foreach(string fixture in new[]{"aevoria-007","shaping-continents","closed-water-budget-011"})
    {
        var climate=fixture=="closed-water-budget-011"?new ClimateSettings(temperatureOffset:18,rainfall:.4f,latitude:25):new ClimateSettings();
        var world=new ProceduralGeography(GenerationSettings.Preset(fixture,WorldSize.Small),climate,progress:s=>Console.WriteLine(fixture+": "+s));var h=world.Hydrology;
        var branches=Enumerable.Range(0,h.Ground.Length).Where(i=>h.IsRiver(i)&&h.Receiver[i]>=0&&world.InlandSurfaces.HasTerminalPool(h.Receiver[i])).OrderByDescending(i=>h.Discharge[i]).ToArray();
        Console.WriteLine("TERMINAL FIXTURE "+fixture+": "+branches.Length+" wet closed branches; first="+branches.FirstOrDefault(-1));
        if(branches.Length>0)break;
    }
    return;
}
var config=GenerationSettings.Preset("module-contract-fixture",WorldSize.Miniature);
void Check(bool condition,string message){if(!condition)throw new Exception("FAILED: "+message);Console.WriteLine("PASS: "+message);}
void Reject<T>(Action action,string message)where T:Exception{bool rejected=false;try{action();}catch(T){rejected=true;}Check(rejected,message);}
Check(BiomeField.Classify(100,-4,.7f,0,.2f)==Biome.BorealForest&&BiomeField.Classify(100,-12,.7f,0,.2f)==Biome.Tundra&&BiomeField.Classify(100,-20,.7f,0,.2f)==Biome.Snow,"moist cold lowlands support boreal forest while colder tundra and permanent snow remain distinct");
var routingLevels=new float[]{0,9,9,9.5f,10,2,9,8,9};var routes=Enumerable.Repeat(-1,9).ToArray();routes[4]=0;
DrainageRouting.RefineDownhillReceivers(routingLevels,routes,3,3,1,1);
Check(routes[4]==5,"downhill drainage follows the physically steeper neighbour instead of flood-discovery order");
routes[4]=0;DrainageRouting.RefineDownhillReceivers(routingLevels,routes,3,3,2,1);
Check(routes[4]==0,"downhill drainage accounts for physical axis spacing and diagonal distance");
routes[4]=3;DrainageRouting.RefineDownhillReceivers(Enumerable.Repeat(10f,9).ToArray(),routes,3,3,1,1);
Check(routes[4]==3&&routes[0]==-1,"filled flats retain their established acyclic spill route and ocean outlets remain terminal");
var a=new Module("a",1,c=>new Payload((ulong)(c.CreateRandom().Next01()*long.MaxValue)));
var b=new Module("b",1,c=>new Payload(c.Input<Payload>("a").Value^(ulong)(c.CreateRandom().Next01()*long.MaxValue)),"a");
var preview=new Module("preview",1,c=>{var r=c.CreateRandom();for(int i=0;i<1000;i++)r.Next01();return new Payload(17);});
var first=new WorldGenerationPipeline(new[]{b,preview,a}).Generate(config);
var second=new WorldGenerationPipeline(new[]{a,b,preview}).Generate(config);
Check(first.Read<Payload>("a").Value==second.Read<Payload>("a").Value&&first.Read<Payload>("b").Value==second.Read<Payload>("b").Value,"registration order cannot change dependent products or seeds");
var previewChanged=new Module("preview",9,c=>{var r=c.CreateRandom();for(int i=0;i<17000;i++)r.Next01();return new Payload(99);});
var unrelated=new WorldGenerationPipeline(new[]{previewChanged,b,a}).Generate(config);
Check(unrelated.Read<Payload>("b").Value==first.Read<Payload>("b").Value,"unrelated module version and random consumption cannot perturb another module");
var aChanged=new Module("a",2,c=>new Payload((ulong)(c.CreateRandom().Next01()*long.MaxValue)));
var propagated=new WorldGenerationPipeline(new[]{aChanged,b,preview}).Generate(config);
Check(propagated.Read<Payload>("a").Value!=first.Read<Payload>("a").Value&&propagated.Read<Payload>("b").Value!=first.Read<Payload>("b").Value&&propagated.Read<Payload>("preview").Value==first.Read<Payload>("preview").Value,"a versioned input change propagates only through its declared consumers");
Reject<ArgumentException>(()=>new WorldGenerationPipeline(new[]{a,a}),"duplicate module producers are rejected before execution");
Reject<ArgumentException>(()=>new WorldGenerationPipeline(new[]{b}),"missing dependencies are rejected before execution");
Reject<ArgumentException>(()=>new WorldGenerationPipeline(new[]{new Module("a",1,c=>new Payload(0),"b"),new Module("b",1,c=>new Payload(0),"a")}),"cyclic dependencies are rejected before execution");
Reject<InvalidOperationException>(()=>new WorldGenerationPipeline(new[]{a,new Module("z",1,c=>c.Input<Payload>("a"))}).Generate(config),"a computed layer cannot be read as an undeclared dependency");
Reject<InvalidOperationException>(()=>new WorldGenerationPipeline(new[]{a,new Module("z",1,c=>new Payload((ulong)c.Input<string>("a").Length),"a")}).Generate(config),"typed inputs reject an incompatible product");
Reject<InvalidOperationException>(()=>new WorldGenerationPipeline(new[]{new Module("null",1,c=>null)}).Generate(config),"null products cannot be published as complete layers");
using(var cancel=new CancellationTokenSource())
{
    var trace=new GenerationDiagnostics();var interrupted=new Module("cancel",1,c=>{cancel.Cancel();c.Checkpoint("Cancellation requested within module",1,2);return new Payload(0);});
    Reject<OperationCanceledException>(()=>new WorldGenerationPipeline(new[]{interrupted}).Generate(config,diagnostics:trace,cancellation:cancel.Token),"cancellation interrupts module work before output publication");
    Check(trace.Snapshot().Outcome==GenerationOutcome.Cancelled&&trace.Snapshot().RecentStages.Count==1,"cancelled generation keeps a bounded diagnostic record and terminal outcome");
}
var timedTrace=new GenerationDiagnostics();
Reject<TimeoutException>(()=>new WorldGenerationPipeline(new[]{new Module("slow",1,c=>{Thread.Sleep(20);c.Checkpoint("Intentional test workload");return new Payload(0);})}).Generate(config,diagnostics:timedTrace,maximumTime:TimeSpan.FromMilliseconds(5)),"cooperative deadlines produce a stage-specific failure instead of silent success");
Check(timedTrace.Snapshot().Outcome==GenerationOutcome.Failed&&timedTrace.Snapshot().Error.Contains("slow"),"deadline diagnostics identify the responsible module");
var bounded=new GenerationDiagnostics();for(int i=0;i<400;i++){bounded.Begin("stage-"+i);bounded.Pulse("Measured work",3,4);bounded.CompleteModule();}bounded.Finish(GenerationOutcome.Completed);
var captured=bounded.Snapshot();Check(captured.RecentStages.Count==GenerationDiagnostics.RecentStageLimit&&captured.RecentStages[0].Module=="stage-144","recent diagnostic history remains bounded without discarding the latest stages");
bounded.Begin("concurrent");Parallel.For(0,1000,i=>bounded.Pulse("Worker heartbeat",i,1000));bounded.CompleteModule();bounded.Finish(GenerationOutcome.Completed);
Check(captured.RecentStages.Count==256&&captured.Outcome==GenerationOutcome.Completed&&bounded.Snapshot().Outcome==GenerationOutcome.Completed,"published diagnostic snapshots remain immutable while workers report progress");
Check(WorldGenerationSeeds.Derive("000123","mountains",1)!=WorldGenerationSeeds.Derive("123","mountains",1),"legacy textual and zero-prefixed master seeds retain distinct identities");
var terrainConfig=GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(mountains:16000,landmass:LandmassType.Continents));
var continents=new ContinentalField(terrainConfig,WorldGenerationSeeds.Derive(terrainConfig.Seed,WorldLayerIds.Continents,ContinentalField.Version));
var ordinary=new BaseElevationField(terrainConfig,continents,WorldGenerationSeeds.Derive(terrainConfig.Seed,WorldLayerIds.Elevation,BaseElevationField.Version));
var formation=new MountainClusterField(terrainConfig,continents,WorldGenerationSeeds.Derive(terrainConfig.Seed,WorldLayerIds.Mountains,MountainClusterField.Version));
var surface=new ComposedSurfaceField(terrainConfig,ordinary,formation);
var caveGeology=new GeologyMap(terrainConfig,continents,WorldGenerationSeeds.Derive(terrainConfig.Seed,WorldLayerIds.Geology,GeologyMap.Version));
var caveClimate=new ClimateMap(terrainConfig,surface,new ClimateSettings());ulong caveSeed=WorldGenerationSeeds.Derive(terrainConfig.Seed,WorldLayerIds.Caves,CaveNetwork.Version);
var caves=new CaveNetwork(terrainConfig,surface,caveGeology,ordinary,caveClimate,caveSeed);int caveX=0,caveZ=0;CaveSystem chosen=null;
for(int z=-12;z<=12&&chosen==null;z++)for(int x=-12;x<=12;x++){var candidate=caves.SystemAt(x,z);if(candidate.Active){chosen=candidate;caveX=x;caveZ=z;break;}}
Check(chosen!=null&&chosen.Chambers.Count>0&&chosen.Connections.Count>0,"typed cave inputs generate real passages and chambers without constructing a geography facade");
bool SameCave(CaveSystem x,CaveSystem y)=>x.Entrance.Equals(y.Entrance)&&x.Passages.Count==y.Passages.Count&&x.Passages.Zip(y.Passages,(p,q)=>p.A.Equals(q.A)&&p.B.Equals(q.B)&&p.Radius==q.Radius&&p.VerticalScale==q.VerticalScale).All(equal=>equal);
var repeatCaves=new CaveNetwork(terrainConfig,surface,caveGeology,ordinary,caveClimate,caveSeed);
Check(SameCave(chosen,repeatCaves.SystemAt(caveX,caveZ)),"identical typed cave inputs and module seed reproduce exact physical passages");
for(int i=0;i<CaveNetwork.CacheCapacity+20;i++)caves.SystemAt(i-74,20);
Check(caves.CachedSystems<=CaveNetwork.CacheCapacity&&SameCave(chosen,caves.SystemAt(caveX,caveZ)),"bounded cave cache eviction cannot change regenerated physical geometry");
var changedCaves=new CaveNetwork(terrainConfig,surface,caveGeology,ordinary,caveClimate,caveSeed+1);
Check(!SameCave(chosen,changedCaves.SystemAt(caveX,caveZ)),"changing the cave module seed changes only its independently generated cave product");
var slopes=new System.Collections.Generic.List<double>();
for(int z=1;z<129;z++)for(int x=1;x<129;x++)
{
    double east=(x/129.0-.5)*terrainConfig.WidthMeters,north=(z/129.0-.5)*terrainConfig.DepthMeters;
    if(continents.CoastDistanceAt(east,north)<2000)continue;
    double dx=(ordinary.ElevationAt(east+32,north)-ordinary.ElevationAt(east-32,north))/64,dz=(ordinary.ElevationAt(east,north+32)-ordinary.ElevationAt(east,north-32))/64;
    slopes.Add(Math.Sqrt(dx*dx+dz*dz));
}
slopes.Sort();Console.WriteLine($"ORDINARY TERRAIN slopes: n={slopes.Count}, median={slopes[slopes.Count/2]:F5}, p95={slopes[(int)(slopes.Count*.95)]:F5}");
Check(slopes.Count>1000&&slopes[(int)(slopes.Count*.95)]<.05,"ordinary inland terrain keeps 95 percent of sampled 64 m slopes below five percent");
Check(formation.Clusters.Count==1&&formation.Clusters[0].Peaks.Count>=4&&formation.Clusters[0].Peaks.Count<=5,"prototype contains one dominant summit and three or four satellites");
var cluster=formation.Clusters[0];float summit=surface.ElevationAt(cluster.X,cluster.Z);
Check(summit>20000&&summit<32000&&summit<surface.MaximumElevation,"high-relief canonical terrain supports a bounded 20 to 32 km summit");
Check(Enumerable.Range(0,51).All(i=>ClimateMap.AltitudeCooling(i*1000)<80),"climate lapse profile stays bounded through the flight altitude range");
var zeroConfig=GenerationSettings.Preset(terrainConfig.Seed,WorldSize.Miniature,new GenerationSettings(mountains:0));
var zeroMountains=new MountainClusterField(zeroConfig,continents,WorldGenerationSeeds.Derive(terrainConfig.Seed,WorldLayerIds.Mountains,MountainClusterField.Version));
Check(zeroMountains.Clusters.Count==0&&zeroMountains.ContributionAt(cluster.X,cluster.Z)==0,"zero mountain relief adds no residual smooth-maximum mound");
Check(Enumerable.Range(0,129).All(i=>
{
    double x=(i/128.0-.5)*terrainConfig.WidthMeters,z=(i/128.0-.5)*terrainConfig.DepthMeters;
    return surface.ElevationAt(x,-terrainConfig.DepthMeters*.5)<0&&surface.ElevationAt(x,terrainConfig.DepthMeters*.5)<0&&surface.ElevationAt(-terrainConfig.WidthMeters*.5,z)<0&&surface.ElevationAt(terrainConfig.WidthMeters*.5,z)<0;
}),"the finite world perimeter is real ocean rather than clipped elevated land");
Console.WriteLine("GENERATION CONTRACT AND TERRAIN CHECKS COMPLETE");
foreach(var worldSize in new[]{WorldSize.Small,WorldSize.Medium,WorldSize.Large,WorldSize.Colossal})
{
    var fullConfig=GenerationSettings.Preset("distribution-fixture",worldSize,new GenerationSettings(mountains:16000,landmass:LandmassType.Continents));
    var land=new ContinentalField(fullConfig,WorldGenerationSeeds.Derive(fullConfig.Seed,WorldLayerIds.Continents,ContinentalField.Version));
    ulong mountainSeed=WorldGenerationSeeds.Derive(fullConfig.Seed,WorldLayerIds.Mountains,MountainClusterField.Version);
    var full=new MountainClusterField(fullConfig,land,mountainSeed);var prototypeConfig=GenerationSettings.Preset(fullConfig.Seed,worldSize,new GenerationSettings(mountains:16000,landmass:LandmassType.Continents,mountainPrototypeOnly:true));
    var prototype=new MountainClusterField(prototypeConfig,land,mountainSeed);
    Console.WriteLine($"DISTRIBUTION {worldSize}: {full.Clusters.Count} clusters");
    Check(full.Clusters.Count>=1&&full.Clusters.Count<=fullConfig.WidthMeters/128000&&prototype.Clusters.Count==1&&full.Clusters[0].Id==prototype.Clusters[0].Id&&full.Clusters[0].Height==prototype.Clusters[0].Height,"global placement preserves the independently reviewable primary formation: "+worldSize);
    Check(full.Clusters.Select(c=>c.Id).Distinct().Count()==full.Clusters.Count,"cluster identities stay unique across global placement: "+worldSize);
    if(worldSize==WorldSize.Colossal)Check(full.Clusters.Count>=8&&full.Clusters.Select(c=>c.Height).Distinct().Count()>4,"Colossal worlds contain multiple sparse formations with distinct physical relief");
}

sealed class Payload{public readonly ulong Value;public Payload(ulong value){Value=value;}}
sealed class Module:WorldGenerationModule<Payload>
{
    private readonly Func<WorldGenerationContext,Payload> build;
    public Module(string id,int version,Func<WorldGenerationContext,Payload> build,params string[] dependencies):base(id,version,dependencies){this.build=build;}
    protected override Payload Build(WorldGenerationContext context)=>build(context);
}
