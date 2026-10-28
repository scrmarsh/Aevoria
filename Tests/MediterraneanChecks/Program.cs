using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Aevoria.World;

if(args.Contains("--coasts")){CoastChecks.Run(args[0]);return;}
if(args.Contains("--fjord-fixture")){CoastChecks.FindFjord(args[0]);return;}
if(args.Contains("--datum"))
{
    string master="aevoria-001";var c=GenerationSettings.Preset(master,WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents));
    var land=new ContinentalField(c,WorldGenerationSeeds.Derive(master,WorldLayerIds.Continents,ContinentalField.Version));var geology=new GeologyMap(c,land,WorldGenerationSeeds.Derive(master,WorldLayerIds.Geology,GeologyMap.Version));var faults=new TectonicStructureField(c,land,geology,WorldGenerationSeeds.Derive(master,WorldLayerIds.Tectonics,TectonicStructureField.Version));var ground=new BaseElevationField(c,land,WorldGenerationSeeds.Derive(master,WorldLayerIds.Elevation,BaseElevationField.Version),faults);
    var points=new List<object>();int dry=0,clamped=0;
    for(int z=0;z<257;z++)for(int x=0;x<257;x++){double e=(x/256.0-.5)*c.WidthMeters,n=(z/256.0-.5)*c.DepthMeters;if(land.CoastDistanceAt(e,n)<=1000)continue;dry++;float h=ground.ElevationAt(e,n);if(h!=.001f)continue;clamped++;if(clamped%64==0)points.Add(new{x=e,z=n,height=h,faultRelief=faults.PhysicalReliefAt(e,n)});}
    File.WriteAllText(args[0],JsonSerializer.Serialize(new{seed=master,dry,clamped,points},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"DATUM dry{dry} clamped{clamped}");return;
}

var timer=Stopwatch.StartNew();int checks=0;
void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
var results=new List<object>();
foreach(string master in new[]{"shaping-continents","aevoria-001","mediterranean-013"})
{
    // Tall-formation fixtures need enough inland area for their supporting
    // slopes; a newly widened Miniature sea can legitimately constrain relief.
    var config=GenerationSettings.Preset(master,WorldSize.Small,new GenerationSettings(mountains:16000,mountainPrototypeOnly:true,landmass:LandmassType.Continents));
    var continents=new ContinentalField(config,WorldGenerationSeeds.Derive(master,WorldLayerIds.Continents,ContinentalField.Version));
    var geology=new GeologyMap(config,continents,WorldGenerationSeeds.Derive(master,WorldLayerIds.Geology,GeologyMap.Version));
    ulong seed=WorldGenerationSeeds.Derive(master,WorldLayerIds.Tectonics,TectonicStructureField.Version);
    var structure=new TectonicStructureField(config,continents,geology,seed);var repeat=new TectonicStructureField(config,continents,geology,seed);
    Check(structure.Faults.Count>2,"Neighbouring plate regions produce real structural traces: "+master);
    Check(structure.Faults.All(f=>f.Points.Count>=9&&f.Width>0&&double.IsFinite(f.Displacement)),"Fault shapes/width/displacement are finite");
    Check(structure.Faults.Zip(repeat.Faults,(a,b)=>a.Id==b.Id&&a.Points.Zip(b.Points,(p,q)=>p.X==q.X&&p.Z==q.Z).All(x=>x)).All(x=>x),"Structural traces reproduce exactly");
    var ordinary=new BaseElevationField(config,continents,WorldGenerationSeeds.Derive(master,WorldLayerIds.Elevation,BaseElevationField.Version),structure);
    var alteredStructure=new TectonicStructureField(config,continents,geology,seed+1);
    var alteredOrdinary=new BaseElevationField(config,continents,WorldGenerationSeeds.Derive(master,WorldLayerIds.Elevation,BaseElevationField.Version),alteredStructure);double structuralRelief=0;
    foreach(var trace in structure.Faults)foreach(var p in trace.Points)structuralRelief=Math.Max(structuralRelief,Math.Abs(ordinary.ElevationAt(p.X,p.Z)-alteredOrdinary.ElevationAt(p.X,p.Z)));
    Check(structuralRelief>1,"Fault geometry changes the authoritative physical elevation, not a decorative overlay");
    int inlandSamples=0,nearDatum=0;
    for(int z=-20;z<=20;z++)for(int x=-20;x<=20;x++){double e=x/42.0*config.WidthMeters,n=z/42.0*config.DepthMeters;if(continents.CoastDistanceAt(e,n)<3000)continue;inlandSamples++;if(Math.Abs(ordinary.ElevationAt(e,n))<.01)nearDatum++;}
    Check(inlandSamples>100&&nearDatum<inlandSamples*.001,"Subsided continental interiors retain relief instead of a sea-datum sheet");
    var mountains=new MountainClusterField(config,continents,WorldGenerationSeeds.Derive(master,WorldLayerIds.Mountains,MountainClusterField.Version),tectonics:structure);
    var surface=new ComposedSurfaceField(config,ordinary,mountains);Check(mountains.Clusters.Count==1,"Isolated formation fixture retains one reviewable massif");var formation=mountains.Clusters[0];
    Check(formation.Peaks.Count>=4&&formation.Ridges.Count>=9,"Dominant/subordinate summits have connected primary and secondary ridges");
    Check(formation.Ridges.All(r=>formation.Ridges.Any(parent=>parent!=r&&PathDistanceSquared(parent,r.Points[0])<.0001)),"Every spur root lies on its actual curved parent network");
    Check(formation.Radius>=formation.Height*1.5,"Extreme relief has a broad supporting geographic mass");
    double highest=0,positive=0,largestStep=0,asymmetry=0,largestCoastalRatio=0;int samples=0;
    for(int z=-24;z<=24;z++)for(int x=-24;x<=24;x++)
    {
        double east=formation.X+x/24.0*formation.Radius,north=formation.Z+z/24.0*formation.Radius;double h=mountains.ContributionAt(east,north),again=mountains.ContributionAt(east,north);
        if(!double.IsFinite(h)||h<0||h!=again)throw new Exception("Invalid/non-deterministic mountain surface");
        highest=Math.Max(highest,h);positive+=h>formation.Height*.1?1:0;samples++;
        double coast=continents.CoastDistanceAt(east,north);if(coast>0)largestCoastalRatio=Math.Max(largestCoastalRatio,h/coast);
        largestStep=Math.Max(largestStep,Math.Abs(h-mountains.ContributionAt(east+.01,north)));asymmetry+=Math.Abs(h-mountains.ContributionAt(formation.X-x/24.0*formation.Radius,north));
        if(Math.Abs(surface.ElevationAt(east,north)-(ordinary.ElevationAt(east,north)+h))>.02)throw new Exception("Physical surface composition mismatch");
    }
    Check(highest>18000&&highest<surface.MaximumElevation,"Extreme mountain remains physical, tall and inside declared surface bounds");
    Check(positive/samples>.12,"Broad elevated support occupies actual terrain");
    Check(largestStep<.25,"Analytic mountain surface remains continuous across centimetre probes");
    Check(largestCoastalRatio<=.901,"Extreme coastal relief has supporting slopes instead of a fixed-width vertical curtain");
    Check(asymmetry/samples>formation.Height*.015,"Branching formation is not a symmetric radial cone");
    using(var cancel=new CancellationTokenSource())
    {
        bool interrupted=false;try{new TectonicStructureField(config,continents,geology,seed,cancel.Token,(detail,done,total)=>{if(done==8)cancel.Cancel();});}catch(OperationCanceledException){interrupted=true;}
        Check(interrupted,"Fault-field preparation cancels during measured work");
    }
    results.Add(new{seed=master,size=WorldSize.Small.ToString(),faults=structure.Faults.Count,kind=formation.Kind.ToString(),summits=formation.Peaks.Count,ridges=formation.Ridges.Count,radius=formation.Radius,highest,supportFraction=positive/samples,largestCentimetreStep=largestStep});
}
Console.WriteLine("PHASE013_GEOLOGY_CHECKS "+checks+" passed in "+timer.Elapsed.TotalSeconds.ToString("F2")+"s");
object rareFixture=null;
if(args.Contains("--rare"))
{
    for(int attempt=0;attempt<192;attempt++)
    {
        string master="rare-relief-013-"+attempt;var config=GenerationSettings.Preset(master,WorldSize.Small,new GenerationSettings(mountains:16000,mountainPrototypeOnly:true,landmass:LandmassType.Continents));
        var continents=new ContinentalField(config,WorldGenerationSeeds.Derive(master,WorldLayerIds.Continents,ContinentalField.Version));var geology=new GeologyMap(config,continents,WorldGenerationSeeds.Derive(master,WorldLayerIds.Geology,GeologyMap.Version));var tectonics=new TectonicStructureField(config,continents,geology,WorldGenerationSeeds.Derive(master,WorldLayerIds.Tectonics,TectonicStructureField.Version));var mountains=new MountainClusterField(config,continents,WorldGenerationSeeds.Derive(master,WorldLayerIds.Mountains,MountainClusterField.Version),tectonics:tectonics);
        if(mountains.Clusters.Count==0||mountains.Clusters[0].Height<32000)continue;
        var formation=mountains.Clusters[0];var ordinary=new BaseElevationField(config,continents,WorldGenerationSeeds.Derive(master,WorldLayerIds.Elevation,BaseElevationField.Version),tectonics);var surface=new ComposedSurfaceField(config,ordinary,mountains);double highest=0;
        for(int z=-15;z<=15;z++)for(int x=-15;x<=15;x++)highest=Math.Max(highest,surface.ElevationAt(formation.X+x*formation.Radius/150,formation.Z+z*formation.Radius/150));
        // An extreme formation intersecting the coast must keep its support
        // rather than retain an artificially stretched summit. Find a truly
        // inland fixture whose physical height still meets the requirement.
        if(highest<=30000)continue;
        Check(highest<surface.MaximumElevation,"Rare extreme formation exceeds30km on the authoritative terrain: "+master);rareFixture=new{seed=master,size=WorldSize.Small.ToString(),height=highest,formation.Radius};break;
    }
    Check(rareFixture!=null,"A reproducible rare30km fixture is preserved");
}
if(args.Length>0)File.WriteAllText(args[0],JsonSerializer.Serialize(new{generator=ProceduralGeography.GeneratorVersion,modules=WorldGenerationIdentity.ModuleVersions,checks,seconds=timer.Elapsed.TotalSeconds,results,rareFixture},new JsonSerializerOptions{WriteIndented=true}));

static double PathDistanceSquared(MountainRidge ridge,FaultPoint point)
{
    double best=double.MaxValue;
    for(int i=1;i<ridge.Points.Count;i++){var a=ridge.Points[i-1];var b=ridge.Points[i];double dx=b.X-a.X,dz=b.Z-a.Z,t=Math.Max(0,Math.Min(1,((point.X-a.X)*dx+(point.Z-a.Z)*dz)/(dx*dx+dz*dz))),ex=point.X-a.X-t*dx,ez=point.Z-a.Z-t*dz;best=Math.Min(best,ex*ex+ez*ez);}return best;
}
