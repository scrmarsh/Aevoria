using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aevoria.World;
using Aevoria.Persistence;

if(args.Length<4)throw new ArgumentException("seed size mountain-relief output-directory [timeout-seconds] [cancel-stage-substring]");
string seed=args[0],output=Path.GetFullPath(args[3]);
var size=Enum.Parse<WorldSize>(args[1]);float relief=float.Parse(args[2],CultureInfo.InvariantCulture);
int timeout=args.Length>4?int.Parse(args[4],CultureInfo.InvariantCulture):600;
string cancelStage=args.Length>5?args[5]:null;
if(Directory.Exists(output))throw new IOException("Choose a fresh evidence directory");Directory.CreateDirectory(output);
var options=new JsonSerializerOptions{WriteIndented=true};
var config=GenerationSettings.Preset(seed,size,new GenerationSettings(mountains:relief,landmass:LandmassType.Continents));
var watch=Stopwatch.StartNew();var stageWatch=Stopwatch.StartNew();var gate=new object();
string current="Starting";var stages=new List<object>();var process=Process.GetCurrentProcess();
double peakPrivate=0,peakWorking=0;long cancelAt=-1;bool done=false;
using var cancellation=new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
using var progressFile=new StreamWriter(Path.Combine(output,"progress.jsonl")){AutoFlush=true};
using var heartbeatFile=new StreamWriter(Path.Combine(output,"heartbeat.jsonl")){AutoFlush=true};
void Progress(string stage)
{
    lock(gate)
    {
        stages.Add(new{stage=current,milliseconds=stageWatch.Elapsed.TotalMilliseconds});current=stage;stageWatch.Restart();
        progressFile.WriteLine(JsonSerializer.Serialize(new{elapsedSeconds=watch.Elapsed.TotalSeconds,stage}));
        Console.WriteLine(FormattableString.Invariant($"{watch.Elapsed.TotalSeconds:F3}s {stage}"));
        if(cancelStage!=null&&stage.Contains(cancelStage,StringComparison.OrdinalIgnoreCase)&&cancelAt<0){cancelAt=Stopwatch.GetTimestamp();cancellation.Cancel();}
    }
}
var heartbeat=Task.Run(async()=>
{
    while(!Volatile.Read(ref done))
    {
        process.Refresh();double privateMiB=process.PrivateMemorySize64/1048576.0,workingMiB=process.WorkingSet64/1048576.0;
        peakPrivate=Math.Max(peakPrivate,privateMiB);peakWorking=Math.Max(peakWorking,workingMiB);
        lock(gate)heartbeatFile.WriteLine(JsonSerializer.Serialize(new{elapsedSeconds=watch.Elapsed.TotalSeconds,stage=current,stageSeconds=stageWatch.Elapsed.TotalSeconds,cpuSeconds=process.TotalProcessorTime.TotalSeconds,privateMiB,workingMiB,managedMiB=GC.GetTotalMemory(false)/1048576.0}));
        await Task.Delay(1000);
    }
});
Exception failure=null;ProceduralGeography geo=null;double workerSeconds=0;long workerFinished=0;
try
{
    var generation=Task.Run(()=>new ProceduralGeography(config,progress:Progress,token:cancellation.Token));
    if(await Task.WhenAny(generation,Task.Delay(TimeSpan.FromSeconds(timeout+15)))!=generation)
    {
        File.WriteAllText(Path.Combine(output,"unresponsive-cancellation.json"),JsonSerializer.Serialize(new{elapsedSeconds=watch.Elapsed.TotalSeconds,stage=current,status="Generation failed to honor its deadline/cancellation; owned probe exits without claiming completion"},options));
        Environment.Exit(4);
    }
    geo=await generation;
}
catch(Exception ex){failure=ex;}
finally{workerSeconds=watch.Elapsed.TotalSeconds;workerFinished=Stopwatch.GetTimestamp();Volatile.Write(ref done,true);await heartbeat;lock(gate)stages.Add(new{stage=current,milliseconds=stageWatch.Elapsed.TotalMilliseconds});}
double generationSeconds=watch.Elapsed.TotalSeconds;
if(failure!=null)
{
    var result=new{generator=ProceduralGeography.GeneratorVersion,seed,size=(int)size,widthMeters=config.WidthMeters,mountainRelief=relief,status=failure is OperationCanceledException?"Cancelled":"Failed",workerSeconds,generationSeconds,cancellationLatencyMilliseconds=cancelAt<0?(double?)null:(workerFinished-cancelAt)*1000.0/Stopwatch.Frequency,peakPrivateMiB=peakPrivate,peakWorkingMiB=peakWorking,stages,exception=failure.ToString()};
    File.WriteAllText(Path.Combine(output,"result.json"),JsonSerializer.Serialize(result,options));Environment.ExitCode=failure is OperationCanceledException&&cancelStage!=null?0:2;return;
}
const int resolution=129;var slopes=new List<double>();var peaks=new List<(double height,double x,double z)>();int land=0;
using(var fields=new StreamWriter(Path.Combine(output,"fields.csv")))
{
    fields.WriteLine("x,z,elevation,baseElevation,temperature,moisture,biome,slope64m");
    for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
    {
        double east=((x+.5)/resolution-.5)*config.WidthMeters,north=((z+.5)/resolution-.5)*config.DepthMeters;var sample=geo.Sample(new GeoPoint(east,north));
        double dx=(geo.ElevationAt(east+32,north)-geo.ElevationAt(east-32,north))/64,dz=(geo.ElevationAt(east,north+32)-geo.ElevationAt(east,north-32))/64,slope=Math.Sqrt(dx*dx+dz*dz);
        if(sample.ElevationMeters>geo.SeaLevel){land++;if(sample.Biome==Biome.Grassland)slopes.Add(slope);peaks.Add((sample.ElevationMeters,east,north));}
        fields.WriteLine(FormattableString.Invariant($"{east:R},{north:R},{sample.ElevationMeters:R},{geo.BaseElevationAt(east,north):R},{sample.TemperatureCelsius:R},{sample.Moisture:R},{sample.Biome},{slope:R}"));
    }
}
var best=peaks.OrderByDescending(p=>p.height).Take(32).ToArray();double step=config.WidthMeters/resolution;
for(int pass=0;pass<4;pass++)
{
    var refined=new List<(double height,double x,double z)>();foreach(var peak in best)for(int z=-2;z<=2;z++)for(int x=-2;x<=2;x++){double east=peak.x+x*step*.25,north=peak.z+z*step*.25;if(config.Contains(new GeoPoint(east,north)))refined.Add((geo.ElevationAt(east,north),east,north));}best=refined.OrderByDescending(p=>p.height).Take(32).ToArray();step*=.25;
}
slopes.Sort();double Percentile(double p)=>slopes.Count==0?0:slopes[(int)((slopes.Count-1)*p)];
object Read(object value,string member)=>value?.GetType().GetProperty(member)?.GetValue(value)??value?.GetType().GetField(member)?.GetValue(value);
var modules=new List<object>();object layers=Read(geo,"Layers");
if(Read(layers,"Ids") is System.Collections.IEnumerable ids)foreach(string id in ids)
{
    object artifact=layers.GetType().GetMethod("Artifact").Invoke(layers,new object[]{id}),contract=Read(artifact,"Contract");
    modules.Add(new{id,version=Convert.ToInt32(Read(contract,"Version")),seed=Convert.ToUInt64(Read(artifact,"Seed")).ToString("X16",CultureInfo.InvariantCulture)});
}
var formations=new List<object>();
object diagnostics=Read(geo,"Diagnostics"),diagnosticSnapshot=diagnostics?.GetType().GetMethod("Snapshot")?.Invoke(diagnostics,null);
if(Read(diagnosticSnapshot,"RecentStages") is System.Collections.IEnumerable records)
{
    var timings=records.Cast<object>().Select(record=>new{module=Read(record,"Module"),version=Read(record,"Version"),seconds=Read(record,"Seconds"),completed=Read(record,"Completed"),total=Read(record,"Total"),detail=Read(record,"Detail")}).ToArray();
    File.WriteAllText(Path.Combine(output,"module-timings.json"),JsonSerializer.Serialize(timings,options));
}
if(Read(Read(geo,"Mountains"),"Clusters") is System.Collections.IEnumerable clusters)foreach(object cluster in clusters)
{
    double x=Convert.ToDouble(Read(cluster,"X")),z=Convert.ToDouble(Read(cluster,"Z")),radius=Convert.ToDouble(Read(cluster,"Radius"));var ring=new double[32];
    for(int i=0;i<ring.Length;i++){double angle=i*Math.PI*2/ring.Length;ring[i]=geo.ElevationAt(x+Math.Cos(angle)*radius*1.6,z+Math.Sin(angle)*radius*1.6);}Array.Sort(ring);
    double median=(ring[15]+ring[16])*.5,central=geo.ElevationAt(x,z);
    formations.Add(new{id=Read(cluster,"Id"),x,z,radius,centralElevationMeters=central,foothillRingRadiusMeters=radius*1.6,foothillRingMedianMeters=median,localReliefAboveMedianFoothillRingMeters=central-median,localReliefMethod="32 azimuth samples; local relief, not a global key-col topographic prominence calculation",peaks=(Read(cluster,"Peaks") as System.Collections.IEnumerable)?.Cast<object>().Select(p=>new{x=Convert.ToDouble(Read(p,"X")),z=Convert.ToDouble(Read(p,"Z")),actualElevationMeters=geo.ElevationAt(Convert.ToDouble(Read(p,"X")),Convert.ToDouble(Read(p,"Z")))}).ToArray()});
}
var summary=new{probeVersion=2,application=Aevoria.Core.ApplicationVersion.Number,generator=ProceduralGeography.GeneratorVersion,geology=GeologyMap.Version,climate=ClimateMap.Version,hydrology=HydrologyMap.Version,waterLayout=InlandWaterLayout.Version,modules,mountainPrototypeOnly=Read(config.Terrain,"MountainPrototypeOnly"),formations,seed,size=(int)size,widthMeters=config.WidthMeters,mountainRelief=relief,status="Completed",workerSeconds,generationSeconds,timingScope="workerSeconds excludes heartbeat shutdown; generationSeconds retains the original probe's up-to-one-second shutdown overhead",peakPrivateMiB=peakPrivate,peakWorkingMiB=peakWorking,landFraction=land/(double)(resolution*resolution),grasslandSamples=slopes.Count,grasslandSlopeMedian=Percentile(.5),grasslandSlopeP95=Percentile(.95),grasslandGentleFraction=slopes.Count==0?0:slopes.Count(s=>s<.05)/(double)slopes.Count,sampledPeak=best.Length==0?null:new{height=best[0].height,x=best[0].x,z=best[0].z},rivers=geo.Hydrology.RiverSegments,lakes=geo.InlandSurfaces.LakeCount,riverTriangles=geo.InlandSurfaces.Rivers.Indices.Count/3,lakeTriangles=geo.InlandSurfaces.Lakes.Indices.Count/3,fingerprint=WorldRecord.GeographyFingerprint(geo),stages};
File.WriteAllText(Path.Combine(output,"result.json"),JsonSerializer.Serialize(summary,options));Console.WriteLine(JsonSerializer.Serialize(summary,options));
