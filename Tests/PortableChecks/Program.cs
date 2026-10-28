using System;
using Aevoria.Simulation;
using Aevoria.World;
using Aevoria.Persistence;

static void Check(bool value, string name)
{
    if (!value) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
}

if(Array.IndexOf(Environment.GetCommandLineArgs(),"--recovery-diagnostic")>=0){
var g=new ProceduralGeography(GenerationSettings.Preset("restoration-008",WorldSize.Miniature));var h=g.Hydrology;int total=0,bad=0,raw=0;for(int i=0;i<h.Ground.Length;i++)if(h.IsRiver(i)){h.PathPoint(i,HydrologyMap.PathSteps/2,out double x,out double z);if(g.InlandSurfaces.LakeAt(x,z,out _))continue;float surface=h.PathLevel(i,.5),ground=g.ElevationAt(x,z),b=g.BaseElevationAt(x,z);total++;if(surface-ground>HydrologyMap.ChannelDepth(h.RiverWidth(i),1)+.1f){bad++;if(b<surface-6)raw++;if(bad<=15)Console.WriteLine($"DEEP {i} width {h.RiverWidth(i):F1} surface {surface:F1} carved {ground:F1} raw {b:F1} lakeNode {h.Lake[i]} recvLake {h.Lake[h.Receiver[i]]}");}}Console.WriteLine($"CHANNELS {total} deep {bad}, raw depression {raw}, shores {g.InlandSurfaces.ShoreSamples}, lake triangles {g.InlandSurfaces.Lakes.Indices.Count/3}");return;}
if(Array.IndexOf(Environment.GetCommandLineArgs(),"--civilization-matrix")>=0){CivilizationChecks.RunMatrix();return;}
if(Array.IndexOf(Environment.GetCommandLineArgs(),"--civilization-long")>=0){CivilizationChecks.RunLong();return;}
if(Array.IndexOf(Environment.GetCommandLineArgs(),"--civilization-only")>=0){CivilizationChecks.Run();return;}
if(Array.IndexOf(Environment.GetCommandLineArgs(),"--planet-only")>=0){PlanetaryChecks.Run();return;}
if(Array.IndexOf(Environment.GetCommandLineArgs(),"--ecology-only")>=0){EcologyChecks.Run();return;}
if(Array.IndexOf(Environment.GetCommandLineArgs(),"--cave-only")>=0){CaveChecks.Run();return;}
if (Array.IndexOf(Environment.GetCommandLineArgs(),"--living-only" ) >= 0) { LivingChecks.Run(); return; }
var first = new SimulationClock(new WorldState("same-seed"));
var second = new SimulationClock(new WorldState("same-seed"));
first.Start();
second.Start();
first.Update(2.5);
first.Update(0);
for (int i = 0; i < 10; i++) second.Update(0.25);
Check(first.State.Tick == second.State.Tick && first.State.Food == second.State.Food &&
      first.State.Population == second.State.Population &&
      first.State.RandomState == second.State.RandomState, "determinism across frame cadence");
Check(first.State.Tick == 10, "fixed tick count");
first.Pause();
first.Update(20);
Check(first.State.Tick == 10, "pause halts ticks");
first.Resume();
first.SetSpeed(2);
first.Update(0.5);
Check(first.State.Tick == 14, "resume and speed");
var config = new WorldConfiguration("s", 2, 2, 64, 1);
var chunk = config.ChunkAt(new GeoPoint(-0.1, 64));
Check(chunk.X == -1 && chunk.Y == 1, "stable signed chunk coordinates");
var geo = new PlaceholderGeography(config);
Check(geo.Sample(new GeoPoint(0, 0)).ElevationMeters == 0, "geography independent of renderer");
SaveValidation.Validate(new SaveEnvelope { Seed = "s", Tick = 1, Population = 2, Food = 3, RandomState = "4" });
bool rejected = false;
try { SaveValidation.Validate(new SaveEnvelope { SchemaVersion = 99, Seed = "s", RandomState = "4" }); }
catch (ArgumentException) { rejected = true; }
Check(rejected, "versioned save validation rejects unknown schema");
Console.WriteLine("All portable checks passed.");
var geographyA = new ProceduralGeography(GenerationSettings.Preset("aevoria-001", WorldSize.Medium));
var geographyB = new ProceduralGeography(GenerationSettings.Preset("aevoria-001", WorldSize.Medium));
var geographyC = new ProceduralGeography(GenerationSettings.Preset("aevoria-002", WorldSize.Medium));
var timer = System.Diagnostics.Stopwatch.StartNew();
int land = 0, water = 0, changed = 0;
float min = float.MaxValue, max = float.MinValue;
for (int y = -2048; y <= 2048; y += 32)
for (int x = -2048; x <= 2048; x += 32)
{
    double east=x/4096.0*geographyA.Configuration.WidthMeters, north=y/4096.0*geographyA.Configuration.DepthMeters;
    float a = geographyA.ElevationAt(east,north);
    if (a != geographyB.ElevationAt(east,north)) throw new Exception("Nondeterminism");
    if (a != geographyC.ElevationAt(east,north)) changed++;
    if (float.IsNaN(a) || float.IsInfinity(a)) throw new Exception("Invalid elevation");
    if (a < geographyA.SeaLevel) water++; else land++;
    min = Math.Min(min,a); max = Math.Max(max,a);
}
Check(land>100 && water>100 && changed>1000 && max-min>150,"distinct coherent land and ocean fields / finite deterministic samples");
Console.WriteLine($"Geography grid: {land} land / {water} ocean; elevation {min:F2}..{max:F2}m; {timer.Elapsed.TotalMilliseconds:F2}ms for 49,923 samples (portable .NET, not Unity FPS)");
var originA = geographyA.Configuration.ChunkOrigin(new ChunkKey(-1,0));
var originB = geographyA.Configuration.ChunkOrigin(new ChunkKey(0,0));
Check(originA.X + geographyA.Configuration.ChunkSizeMeters == originB.X,"adjacent global coordinates match exactly");
for (int y=0;y<=64;y++)
{
    var point = new GeoPoint(0,y*4);
    if(geographyA.Sample(point).ElevationMeters!=geographyB.Sample(point).ElevationMeters) throw new Exception("Chunk order mismatch");
}
Check(geographyA.Sample(new GeoPoint(geographyA.Configuration.WidthMeters/2,geographyA.Configuration.DepthMeters/2)).Biome==Biome.Ocean,"sea classification and bounded ocean perimeter");
rejected=false;
try { new WorldConfiguration("bad",2,2,63,double.NaN); } catch(ArgumentException) { rejected=true; }
Check(rejected,"configuration rejects nonfinite size and invalid LOD subdivisions");
rejected=false;
try { new GenerationSettings(roughness:float.NaN); } catch(ArgumentException) { rejected=true; }
Check(rejected,"terrain configuration rejects NaN");
Check(GenerationSettings.Preset("s",WorldSize.Small).WidthMeters < GenerationSettings.Preset("s",WorldSize.Medium).WidthMeters && GenerationSettings.Preset("s",WorldSize.Medium).WidthMeters < GenerationSettings.Preset("s",WorldSize.Large).WidthMeters,"world size presets");
Console.WriteLine("All Phase 1 portable checks passed.");
foreach(var worldSeed in new[]{"aevoria-001","aevoria-002","archipelago-37"})
foreach(var size in new[]{WorldSize.Small,WorldSize.Medium,WorldSize.Large})
{
    var world=new ProceduralGeography(GenerationSettings.Preset(worldSeed,size));
    float fraction=world.MeasureLandFraction();
    Check(fraction>=0.48f && fraction<=0.58f,$"authoritative land coverage {worldSeed}/{size}: {fraction:P2}");
}
var wetter=new ProceduralGeography(GenerationSettings.Preset("land-control",WorldSize.Medium,new GenerationSettings(targetLandFraction:0.4f)));
var drier=new ProceduralGeography(GenerationSettings.Preset("land-control",WorldSize.Medium,new GenerationSettings(targetLandFraction:0.8f)));
Check(wetter.MeasureLandFraction()<0.45 && drier.MeasureLandFraction()>0.75,"configurable continental coverage changes coast topology");
Check(Version.TryParse(Aevoria.Core.ApplicationVersion.Number,out var publicVersion) && publicVersion.Major==0 && publicVersion.Minor==0 && Aevoria.Core.ApplicationVersion.Stage=="Alpha","public Alpha version is separate from geography version");

// Alpha v0.0.4: state continuity, real filesystem transactions and environmental topology.
Check(GenerationSettings.MaximumMountainRelief==16000 && new GenerationSettings(mountains:16000).Mountains==16000,"creator's 16000 m mountain relief ceiling");
rejected=false;try{new GenerationSettings(mountains:16001);}catch(ArgumentException){rejected=true;}Check(rejected,"reject mountain relief above 16000 m");
var calendar004=new WorldCalendar(1440*3+4*101);
Check(calendar004.Year==4&&calendar004.Day==102&&calendar004.Season==1,"calendar derives years and seasons from the existing tick cadence");
var hydro004=geographyA.Hydrology;var rank004=new int[hydro004.Ground.Length];
for(int n=0;n<rank004.Length;n++) rank004[hydro004.FloodOrder[n]]=n;
for(int n=0;n<rank004.Length;n++) {int r=hydro004.Receiver[n];if(r>=0&&(rank004[r]>=rank004[n]||hydro004.Water[r]>hydro004.Water[n]||hydro004.Accumulation[r]<hydro004.Accumulation[n])) throw new Exception("Drainage topology failure");}
Check(hydro004.RiverSegments>0&&hydro004.LakeCount>0,"acyclic downhill receiver network, accumulating rivers and basin lakes");
for(int n=0;n<rank004.Length;n++) if(hydro004.Receiver[n]!=geographyB.Hydrology.Receiver[n]||hydro004.Water[n]!=geographyB.Hydrology.Water[n]) throw new Exception("Hydrology nondeterminism");
Check(true,"hydrology deterministic across complete reconstructions");
var warm004=new ProceduralGeography(GenerationSettings.Preset("climate",WorldSize.Small),new ClimateSettings(temperatureOffset:20));
var cold004=new ProceduralGeography(GenerationSettings.Preset("climate",WorldSize.Small),new ClimateSettings(temperatureOffset:-20));
Check(warm004.Sample(new GeoPoint(0,0)).TemperatureCelsius>cold004.Sample(new GeoPoint(0,0)).TemperatureCelsius+39,"climate settings affect authoritative samples");
Check(new WorldWeather(geographyA,999,new GeoPoint(0,0)).Kind==new WorldWeather(geographyB,999,new GeoPoint(0,0)).Kind,"weather reconstructs deterministically from world and tick");
var source004=new SimulationClock(new WorldState("aevoria-001"));source004.Start();source004.SetSpeed(2);source004.Update(500);while(source004.BacklogSeconds>=SimulationClock.SecondsPerTick) source004.Update(0);source004.Pause();
// Current archives use society-owned totals; prototype RNG behavior is tested
// separately above. Match the application's ownership handoff before capture.
var society004=new CivilizationModel(geographyA,source004.State.Tick);source004.State.UseAggregatePopulation((int)society004.TotalPopulation,(int)society004.TotalFood);
var record004=WorldRecord.Capture(null,"A shared name",null,geographyA,source004,Aevoria.Core.ApplicationVersion.Number,society:society004);
Check(record004.Year>1,"persistent worlds capture an elapsed simulation year");
var restored004=record004.RestoreClock();
Check(restored004.State.Tick==source004.State.Tick&&restored004.State.RandomState==source004.State.RandomState&&restored004.BacklogSeconds==source004.BacklogSeconds&&restored004.Speed==2&&!restored004.Running,"clock, RNG, speed, pause and retained backlog restore exactly");
source004.Resume();restored004.Resume();for(int n=0;n<100;n++){source004.Update(0.1);restored004.Update(0.1);}
Check(source004.State.Food==restored004.State.Food&&source004.State.Population==restored004.State.Population&&source004.State.RandomState==restored004.State.RandomState,"restored simulation continues the same random stream");
string root004=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"Aevoria-Portable-"+Guid.NewGuid().ToString("N"));
var repository004=new WorldRepository(root004);
try
{
    Check((await repository004.ListAsync()).Count==0,"new isolated world library has no fabricated recents");
    await repository004.SaveAsync(record004,false);var loaded004=await repository004.LoadAsync(record004.Id);var rebuilt004=loaded004.Record.Rebuild();
    Check(loaded004.Record.Id==record004.Id&&WorldRecord.GeographyFingerprint(rebuilt004)==record004.Fingerprint,"disk round trip preserves world identity and reconstructed fingerprint");
    var duplicate004=WorldRecord.Capture(null,"A shared name",null,geographyA,source004,Aevoria.Core.ApplicationVersion.Number);await repository004.SaveAsync(duplicate004,false);
    Check((await repository004.ListAsync()).Count==2&&duplicate004.Id!=record004.Id,"identical names use distinct archive identities");
    record004.Name="Renamed world";await repository004.SaveAsync(record004);string path004=System.IO.Path.Combine(root004,record004.Id,"world.xml");
    System.IO.File.WriteAllText(path004+".tmp","interrupted incomplete write");
    Check((await repository004.LoadAsync(record004.Id)).Record.Name=="Renamed world","interrupted temporary writes do not replace complete saves");
    System.IO.File.WriteAllText(path004,"damaged archive");var backup004=await repository004.LoadAsync(record004.Id);
    Check(backup004.RecoveredBackup&&backup004.Record.Name=="A shared name"&&System.IO.File.ReadAllText(path004)=="damaged archive","backup recovery preserves the damaged original for review");
    rejected=false;try{await repository004.SaveAsync(record004);}catch(Exception){rejected=true;}Check(rejected,"saving cannot overwrite an unsupported or damaged existing archive");
    duplicate004.Schema=999;System.IO.File.WriteAllBytes(System.IO.Path.Combine(root004,duplicate004.Id,"world.xml"),ArchiveCodec.Pack(duplicate004));
    rejected=false;try{await repository004.LoadAsync(duplicate004.Id);}catch(NotSupportedException){rejected=true;}Check(rejected,"future schema rejected without falling back to an old backup");
    Check((await repository004.ListAsync()).Count==2,"damaged or unsupported worlds remain visible in metadata enumeration");
    duplicate004.Schema=WorldRecord.CurrentSchema;duplicate004.Generator=3;rejected=false;try{duplicate004.Rebuild();}catch(NotSupportedException){rejected=true;}Check(rejected,"generator v3 worlds require an explicit migration instead of silent v4 regeneration");
    await repository004.DeleteAsync(record004.Id);Check((await repository004.ListAsync()).Count==1,"deleting one world preserves other identities");
    rejected=false;try{await repository004.DeleteAsync("../elsewhere");}catch(ArgumentException){rejected=true;}Check(rejected,"archive operations reject path traversal");
    var settings004=new ApplicationSettings {MotionBlur=true,AutosaveSeconds=300};var roundSettings004=ArchiveCodec.Decode<ApplicationSettings>(ArchiveCodec.Encode(settings004));roundSettings004.Validate();
    Check(roundSettings004.MotionBlur&&roundSettings004.AutosaveSeconds==300,"global settings serialize independently from world archives");
}
finally {System.IO.Directory.Delete(root004,true);}
Console.WriteLine("All Alpha v0.0.4 portable checks passed.");

var checksum004=ArchiveCodec.Decode<ArchiveContainer>(ArchiveCodec.Pack(record004));checksum004.Sha256="00";
rejected=false;try{ArchiveCodec.Unpack(ArchiveCodec.Encode(checksum004));}catch(System.IO.InvalidDataException){rejected=true;}Check(rejected,"checksum rejects a modified payload or digest");
EcologyChecks.Run();

LivingChecks.Run();
