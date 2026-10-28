using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using Aevoria.World;
using Aevoria.Persistence;
using Aevoria.Simulation;

static class EcologyChecks
{
    static void Require(bool ok,string name){if(!ok)throw new Exception("FAILED ecology: "+name);Console.WriteLine("PASS: "+name);}
    static string Hash(EcologyModel e)=>ArchiveCodec.Hash(ArchiveCodec.Encode(e.Capture()));
    public static void Run()
    {
        var geo=new ProceduralGeography(GenerationSettings.Preset("ecosystem-005",WorldSize.Small));var a=new EcologyModel(geo);var b=new EcologyModel(geo);
        Require(Hash(a)==Hash(b),"deterministic ecology initialization independent of render chunks");
        // Recovery requires a climate-supported forest, rather than the first marginal
        // stand encountered after a geography revision. Keep drought/fire state controlled.
        int land=Enumerable.Range(0,a.Count).First(i=>a.EnvironmentAt(i).Land&&a.Forest(i)>.15&&a.EnvironmentAt(i).Moisture>.6&&a.EnvironmentAt(i).Temperature>8&&a.EnvironmentAt(i).Temperature<28);
        double before=a.TotalPopulation(0)+a.TotalPopulation(1)+a.TotalPopulation(2)+a.TotalPopulation(3);a.Migrate();double after=a.TotalPopulation(0)+a.TotalPopulation(1)+a.TotalPopulation(2)+a.TotalPopulation(3);
        Require(Math.Abs(after-before)<1e-8&&Math.Abs(a.LastMigrationError)<1e-8,"buffered migration conserves total population");
        bool barriers=true;for(int i=0;i<a.Count;i++)for(int j=0;j<a.NeighborCount(i);j++){int n=a.Neighbor(i,j);barriers&=a.EnvironmentAt(i).Land&&a.EnvironmentAt(n).Land;bool reverse=false;for(int k=0;k<a.NeighborCount(n);k++)reverse|=a.Neighbor(n,k)==i;barriers&=reverse;}
        Require(barriers,"terrestrial adjacency has reciprocal land endpoints and excludes oceans");
        bool spans=true;for(int i=0;i<a.Count;i++)for(int j=0;j<a.NeighborCount(i);j++)
        {var from=a.EnvironmentAt(i).Center;var to=a.EnvironmentAt(a.Neighbor(i,j)).Center;for(int k=1;k<=32;k++){double f=k/32.0,x=from.X+(to.X-from.X)*f,y=from.Y+(to.Y-from.Y)*f;float h=geo.ElevationAt(x,y);spans&=h>geo.SeaLevel&&!geo.Hydrology.IsLakeWater(x,y,h)&&h-geo.SeaLevel<=1800;}}
        Require(spans,"migration edges sample intervening coastline, lakes and mountain barriers");
        a=new EcologyModel(geo);a.AdvanceTo(1440,12);for(int k=0;k<12;k++)b.AdvanceTo((k+1)*120,1);
        Require(Hash(a)==Hash(b),"monthly ecology is independent of frame and catch-up cadence");
        var snapshot=a.Capture();snapshot.Regions[land].Plants[0]=double.NaN;bool rejected=false;try{new EcologyModel(geo,snapshot);}catch(ArgumentException){rejected=true;}
        Require(rejected,"nonfinite ecological saves are rejected");
        snapshot=a.Capture();snapshot.Version=99;rejected=false;try{new EcologyModel(geo,snapshot);}catch(NotSupportedException){rejected=true;}
        Require(rejected,"future ecology versions are rejected");
        var original=a.Capture();var isolated=a.Region(land);isolated.Animals[0]=1e8;Require(Hash(a)==ArchiveCodec.Hash(ArchiveCodec.Encode(original)),"inspection returns isolated values without mutating authority");
        // Controlled moisture-only test leaves all other starting state identical.
        var initial=new EcologyModel(geo).Capture();var dry=new EcologyModel(geo,initial);var normal=new EcologyModel(geo,initial);
        Require(dry.StartEvent(land,DisturbanceKind.Drought,.95,6),"drought starts in a terrestrial habitat");dry.AdvanceTo(720,6);normal.AdvanceTo(720,6);
        Require(dry.Moisture(land)<normal.Moisture(land)&&dry.Biomass(land)<normal.Biomass(land),"drought reduces water availability and vegetation productivity");
        var fireSeed=normal.Capture();fireSeed.Regions[land].Moisture=.3;var fire=new EcologyModel(geo,fireSeed);double forest=fire.Forest(land);
        Require(fire.StartEvent(land,DisturbanceKind.Fire,.9,1),"fire starts only with combustible terrestrial biomass");fire.AdvanceTo(840,1);
        Require(fire.ActiveEvent(land)==DisturbanceKind.None&&fire.Burn(land)>.8&&fire.Forest(land)<forest,"fire ends, kills forest, and leaves a persistent scar");
        double damaged=fire.Forest(land);fire.AdvanceTo(840+1440*20,1000);
        Console.WriteLine(FormattableString.Invariant($"Ecology recovery habitat {land}: moisture {a.EnvironmentAt(land).Moisture:R}, temperature {a.EnvironmentAt(land).Temperature:R}, initial {forest:R}, damaged {damaged:R}, recovered {fire.Forest(land):R}, burn {fire.Burn(land):R}"));
        Require(fire.Burn(land)<.2&&fire.Forest(land)>damaged,"burn scar decays and forest regrows over years");
        var extinct=initial.Regions[land];extinct.Animals[0]=200;extinct.Plants[0]=0;extinct.Plants[4]=0;var limited=new EcologyModel(geo,initial);limited.AdvanceTo(120,1);
        Require(limited.Population(land,0)<200&&limited.LastDeaths>0,"food scarcity causes mortality without negative populations");
        var young=new EcologyModel(geo).Capture();for(int i=0;i<young.Regions.Length;i++)for(int k=0;k<3;k++)young.Regions[i].Animals[k]*=.01;
        var breeding=new EcologyModel(geo,young);breeding.AdvanceTo(120,1);Require(breeding.LastBirths>0,"suitable under-capacity wildlife reproduces");
        var noWolves=new EcologyModel(geo).Capture();var withWolves=new EcologyModel(geo,noWolves);foreach(var r in noWolves.Regions)r.Animals[3]=0;var noPredation=new EcologyModel(geo,noWolves);withWolves.AdvanceTo(120,1);noPredation.AdvanceTo(120,1);
        Require(withWolves.TotalPopulation(0)<noPredation.TotalPopulation(0)&&withWolves.LastDeaths>noPredation.LastDeaths,"predator foodweb removes available prey and counts mortality");
        var clock=new SimulationClock(WorldState.Restore(geo.Configuration.Seed,a.Tick,24,100,123));var record=WorldRecord.Capture(null,"Ecology round trip",null,geo,clock,"0.0.5",a);
        var loaded=ArchiveCodec.Unpack(ArchiveCodec.Pack(record));var restored=new EcologyModel(geo,loaded.Ecology);
        Require(Hash(a)==Hash(restored)&&loaded.Year==2,"archive round trip restores every ecological value and calendar");
        a.AdvanceTo(14400,120);restored.AdvanceTo(14400,120);Require(Hash(a)==Hash(restored),"restored ecology continues exactly through ten years including events and RNG");
        record.Schema=2;rejected=false;try{ArchiveCodec.Unpack(ArchiveCodec.Pack(record));}catch(NotSupportedException){rejected=true;}Require(rejected,"legacy schema requires migration and cannot silently reset forests");
        string output=Path.Combine(Environment.GetEnvironmentVariable("AEVORIA_VALIDATION_OUTPUT")??Path.Combine("Docs","Validation","Alpha-v0.0.8"),"Retained-ecology");Directory.CreateDirectory(output);
        using var csv=new StreamWriter(Path.Combine(output,"ecology-long-run.csv"));csv.WriteLine("seed,size,regions,years,elapsedMs,yearsPerSecond,managedMiB,grass,forest,biomass,moisture,deer,browsers,hares,wolves,retainedEvents,nextEventId,lastStepMs");
        foreach(var size in new[]{WorldSize.Small,WorldSize.Medium,WorldSize.Large})foreach(string seed in new[]{"ecosystem-005","aevoria-001"})
        {
            var terrain=new ProceduralGeography(GenerationSettings.Preset(seed,size));var eco=new EcologyModel(terrain,0,64);var timer=Stopwatch.StartNew();
            foreach(int years in new[]{1,10,100,1000})
            {
                eco.AdvanceTo((long)years*1440,int.MaxValue);var s=eco.Capture();EcologyModel.ValidateSnapshot(s,seed,eco.Resolution);int terrestrial=0;double grass=0,f=0,bio=0,m=0;
                for(int i=0;i<eco.Count;i++)if(eco.EnvironmentAt(i).Land){terrestrial++;grass+=eco.Plant(i,0);f+=eco.Forest(i);bio+=eco.Biomass(i);m+=eco.Moisture(i);}
                Require(Enumerable.Range(0,4).All(k=>eco.TotalPopulation(k)>.01&&double.IsFinite(eco.TotalPopulation(k))),$"{seed}/{size}: {years} years finite, bounded, all four species persist");
                csv.WriteLine(string.Join(",",new object[]{seed,size,eco.Count,years,timer.Elapsed.TotalMilliseconds.ToString("F3",CultureInfo.InvariantCulture),(years/timer.Elapsed.TotalSeconds).ToString("F3",CultureInfo.InvariantCulture),GC.GetTotalMemory(false)/1048576.0,grass/terrestrial,f/terrestrial,bio/terrestrial,m/terrestrial,eco.TotalPopulation(0),eco.TotalPopulation(1),eco.TotalPopulation(2),eco.TotalPopulation(3),eco.HistoryCount,eco.NextEventId,eco.LastStepMilliseconds}));csv.Flush();
            }
        }
    }
}
