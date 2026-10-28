using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using Aevoria.Persistence;
using Aevoria.World;
using Aevoria.Simulation;

internal static class CivilizationChecks
{
    private static void Check(bool okay,string name){if(!okay)throw new Exception("FAILED: "+name);Console.WriteLine("PASS: "+name);}
    public static void RunMatrix()
    {
        string root=Path.Combine(Environment.GetEnvironmentVariable("AEVORIA_SOCIAL_TEST_ROOT")??Path.GetTempPath(),"SocietyMatrix-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        foreach(WorldSize size in Enum.GetValues(typeof(WorldSize)))
        {
            var timer=Stopwatch.StartNew();var geo=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",size,new GenerationSettings(landmass:LandmassType.Continents)));
            var repository=new WorldRepository(Path.Combine(root,size.ToString()));var model=new CivilizationModel(geo,archive:repository.History);double generationSeconds=timer.Elapsed.TotalSeconds;
            Check(model.Civilizations.Count>=2,size+" has multiple geographically valid civilizations");
            foreach(var s in model.Settlements)Check(CivilizationGeography.Footprint(geo,s.X,s.Z,28,out _),size+" founding footprint is supported and dry");
            timer.Restart();model.AdvanceTo(1440L*100,int.MaxValue);var saved=model.Capture();CivilizationModel.Validate(saved,geo.Configuration,model.Tick);double simulationSeconds=timer.Elapsed.TotalSeconds;
            var restored=new CivilizationModel(geo,saved,repository.History);model.AdvanceTo(1440L*101,int.MaxValue);restored.AdvanceTo(1440L*101,int.MaxValue);
            Check(ArchiveCodec.Hash(ArchiveCodec.Encode(model.Capture()))==ArchiveCodec.Hash(ArchiveCodec.Encode(restored.Capture())),size+" restores and continues identically after a century");
            Check(model.Settlements.Count<=model.Civilizations.Count*12&&model.RecentHistory.Count<=512,size+" respects settlement and active-history bounds");
            File.WriteAllBytes(Path.Combine(root,size+"-century.xml"),ArchiveCodec.Encode(saved));Console.WriteLine(FormattableString.Invariant($"MATRIX {size}: civilizations {model.Civilizations.Count}, settlements {model.Settlements.Count}, population {model.TotalPopulation:F1}, generation {generationSeconds:F2}s, century simulation {simulationSeconds:F2}s; latest monthly step {model.LastStepMilliseconds:F3}ms"));
        }
        Console.WriteLine("MATRIX EVIDENCE "+root);
    }
    public static void RunLong()
    {
        string root=Path.Combine(Environment.GetEnvironmentVariable("AEVORIA_SOCIAL_TEST_ROOT")??Path.GetTempPath(),"SocietyLong-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);var repository=new WorldRepository(Path.Combine(root,"Worlds"));var geo=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents)));var model=new CivilizationModel(geo,archive:repository.History);var steps=new List<double>();
        for(int year=1;year<=1000;year++){model.AdvanceTo(year*1440L,int.MaxValue);steps.Add(model.LastStepMilliseconds);if(year%100==0){var saved=model.Capture();CivilizationModel.Validate(saved,geo.Configuration,model.Tick);Console.WriteLine($"CENTURY {year}: people {model.TotalPopulation:F1}, settlements {model.Settlements.Count}, camps {model.Settlements.Count(s=>s.Stage==SettlementStage.Camp)}, villages {model.Settlements.Count(s=>s.Stage==SettlementStage.Village)}, towns {model.Settlements.Count(s=>s.Stage>=SettlementStage.Town)}, events {saved.NextEventSequence-1}, pages {saved.HistoryPages.Count}");File.WriteAllBytes(Path.Combine(root,"year-"+year+".xml"),ArchiveCodec.Encode(saved));}}
        Check(model.Settlements.Any(s=>s.Stage>=SettlementStage.Town),"a supported long-running society develops real towns");Check(model.Settlements.Count>model.Civilizations.Count,"supported expansion establishes additional populated settlements");Check(model.Settlements.All(s=>s.Population>=0&&s.Population<=s.Capacity*1.05),"long-running populations remain nonnegative and capacity constrained");Check(model.RecentHistory.Count<=512,"long-duration history retains a bounded active window");
        var restore=new CivilizationModel(geo,model.Capture(),repository.History);model.AdvanceTo(1440L*1001,int.MaxValue);restore.AdvanceTo(1440L*1001,int.MaxValue);Check(ArchiveCodec.Hash(ArchiveCodec.Encode(model.Capture()))==ArchiveCodec.Hash(ArchiveCodec.Encode(restore.Capture())),"millennium restoration and continuation remain deterministic");
        var source=model.Settlements.First(s=>s.Resources.Food>1&&model.Settlements.Any(t=>t!=s&&t.CivilizationId==s.CivilizationId&&model.Connected(s,t)));var target=model.Settlements.First(s=>s!=source&&s.CivilizationId==source.CivilizationId&&model.Connected(source,s));double beforeFood=model.TotalFood,beforePeople=model.TotalPopulation;
        Check(model.TransferFood(source.Id,target.Id,.5)&&Math.Abs(model.TotalFood-beforeFood)<1e-6&&model.TotalPopulation==beforePeople,"real expanded settlements transfer food conservatively without creating people");
        var foreign=model.Settlements.First(s=>s.CivilizationId!=source.CivilizationId);Check(!model.TransferFood(source.Id,foreign.Id,.5)&&!model.TransferFood(source.Id,source.Id,.5)&&!model.TransferFood(source.Id,target.Id,double.NaN)&&Math.Abs(model.TotalFood-beforeFood)<1e-6,"cross-polity, self and nonfinite transfers are refused without changing stores");
        Console.WriteLine($"SOCIETY LONG: {model.Settlements.Count} settlements, {model.TotalPopulation:F1} people, max sampled annual-boundary monthly step {steps.Max():F3}ms. Concurrent Unity validation may influence timing. Evidence {root}");
    }
    public static void Run()
    {
        string root=Path.Combine(Environment.GetEnvironmentVariable("AEVORIA_SOCIAL_TEST_ROOT")??Path.GetTempPath(),"Society-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);var repository=new WorldRepository(Path.Combine(root,"Worlds"));var archive=repository.History;
        var timer=Stopwatch.StartNew();var geo=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents)));
        var settings=new CivilizationSettings{RequestedCivilizations=4,CandidateResolution=32};var a=new CivilizationModel(geo,settings:settings,archive:archive);var b=new CivilizationModel(geo,settings:settings,archive:archive);
        Check(a.Civilizations.Count==4&&a.Settlements.Count==4,"configured civilizations establish multiple independent settlements");
        Check(a.Civilizations.Select(c=>c.Id).Distinct().Count()==4&&a.Settlements.Select(s=>s.Id).Distinct().Count()==4,"civilization and settlement IDs are unique");
        Check(ArchiveCodec.Hash(ArchiveCodec.Encode(a.Capture()))==ArchiveCodec.Hash(ArchiveCodec.Encode(b.Capture())),"society generation, names, colors, inventories and territories reproduce exactly");
        foreach(var s in a.Settlements){Check(CivilizationGeography.Footprint(geo,s.X,s.Z,28,out _),"settlement "+s.Name+" has dry supported geography");foreach(var building in s.Buildings)Check(CivilizationGeography.Footprint(geo,building.X,building.Z,4,out _),"building "+building.Id+" has a real non-floating construction footprint");}
        Check(a.Settlements.All(s=>s.Buildings.Count>=3),"founding stores fund recognizable multi-building camps");
        Check(a.RecentHistory.Count==8&&a.RecentHistory.All(e=>e.Tick==0),"founding histories describe actual dated civilizations and settlements");
        long until=120*12*80;a.AdvanceTo(until,int.MaxValue);while(b.Tick<until)b.AdvanceTo(Math.Min(until,b.Tick+120*7),3);
        Check(ArchiveCodec.Hash(ArchiveCodec.Encode(a.Capture()))==ArchiveCodec.Hash(ArchiveCodec.Encode(b.Capture())),"monthly economy, population, territory and history are independent of catch-up cadence");
        var originalCulture=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");
            var localized=new CivilizationModel(geo,settings:settings,archive:archive);localized.AdvanceTo(until,int.MaxValue);
            Check(ArchiveCodec.Hash(ArchiveCodec.Encode(a.Capture()))==ArchiveCodec.Hash(ArchiveCodec.Encode(localized.Capture())),"saved simulation and factual event text remain identical across numeric locales");
        }
        finally{CultureInfo.CurrentCulture=originalCulture;}
        Console.WriteLine(string.Join("\n",a.Settlements.Select(s=>$"SETTLEMENT {s.Name}: {s.Stage}, {s.Population:F2} people, food {s.Resources.Food:F2}, timber {s.Resources.Timber:F2}, stone {s.Resources.Stone:F2}, supplies {s.Resources.Supplies:F2}")));
        Check(a.Settlements.Any(s=>s.Stage>SettlementStage.Camp)&&a.RecentHistory.Any(e=>e.Kind==SocialEventKind.Development),"supported settlements pay for a real development transition over time");
        var snapshot=a.Capture();CivilizationModel.Validate(snapshot,geo.Configuration,a.Tick);var restored=new CivilizationModel(geo,snapshot,archive);Check(ArchiveCodec.Hash(ArchiveCodec.Encode(restored.Capture()))==ArchiveCodec.Hash(ArchiveCodec.Encode(snapshot)),"society restoration changes no population, stores, boundaries or events");
        Action<CivilizationSnapshot>[] corruptions={
            v=>v.Settlements[0].Capacity=double.NaN,
            v=>v.Settlements[0].GrowthRate=double.PositiveInfinity,
            v=>v.Settlements[0].LastExpansionTick=v.Tick+1,
            v=>v.Settlements[0].Buildings[0].X=double.NaN,
            v=>v.Settlements[0].Buildings[0].Kind=(CivicBuildingKind)999,
            v=>v.Settlements[0].Buildings.Add(v.Settlements[0].Buildings[0].Copy()),
            v=>v.Civilizations[0].Government=(PoliticalForm)999,
            v=>v.Civilizations[0].Settlements.Remove(v.Settlements[0].Id),
            v=>v.RecentHistory[0].OtherCivilizationId="missing-polity",
            v=>v.RecentHistory[0].Kind=(SocialEventKind)999
        };
        foreach(var corrupt in corruptions){var damaged=snapshot.Copy();corrupt(damaged);bool invalidSnapshot=false;try{new CivilizationModel(geo,damaged,archive);}catch(ArgumentException){invalidSnapshot=true;}Check(invalidSnapshot,"corrupted economy, construction, ownership or history is rejected before installation");}
        a.AdvanceTo(until+120*24,int.MaxValue);restored.AdvanceTo(until+120*24,int.MaxValue);Check(ArchiveCodec.Hash(ArchiveCodec.Encode(a.Capture()))==ArchiveCodec.Hash(ArchiveCodec.Encode(restored.Capture())),"continued society after reload matches uninterrupted simulation");
        var shortage=b.Capture();var s0=shortage.Settlements[0];double initial=s0.Population;s0.Resources=new CivicInventory();s0.Fertility=s0.Freshwater=s0.ClimateSuitability=0;s0.FoodSatisfaction=0;
        var poor=new CivilizationModel(geo,shortage,archive);poor.AdvanceTo(poor.Tick+120*12,int.MaxValue);Check(poor.Settlements[0].Population<initial&&poor.Settlements[0].InCrisis,"food shortage causes a real recorded crisis and population decline");
        Check(poor.Settlements.All(s=>s.Population>=0&&s.Resources.Food>=0&&s.Resources.Timber>=0&&s.Resources.Stone>=0),"scarcity cannot produce negative population or inventories");
        var invalid=a.Capture();invalid.Settlements[0].Resources.Food=-1;bool rejected=false;try{new CivilizationModel(geo,invalid,archive);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid persistent inventories are rejected before restoration");
        var events=new List<SocialHistoryEvent>();for(int i=1;i<=256;i++)events.Add(new SocialHistoryEvent{Sequence=i,Tick=i,Id=CivilIdentity.Id("test","event/"+i),Title="A fact",Description="A measured transition"});var page=archive.Store(events);var duplicate=archive.Store(events);
        Check(page.Key==duplicate.Key&&archive.Read(page).Count==256,"immutable historical pages deduplicate and reload exactly");
        bool traversal=false;try{archive.Read(new SocialHistoryPage{Key="../outside.xml",Sha256="bad",First=1,Last=1,Count=1});}catch(InvalidDataException){traversal=true;}Check(traversal,"history archive rejects path traversal and malformed references");
        var paged=a.Capture();for(int i=0;i<900;i++){long sequence=paged.NextEventSequence++;paged.RecentHistory.Add(new SocialHistoryEvent{Id=CivilIdentity.Id("pager-test","event/"+sequence),Sequence=sequence,Tick=paged.Tick,Kind=SocialEventKind.PopulationMilestone,CivilizationId=paged.Civilizations[0].Id,Title="Pager test fixture",Description="Synthetic test event for bounded historical storage."});}
        var archived=new CivilizationModel(geo,paged,archive);var bounded=archived.Capture();Check(bounded.RecentHistory.Count<=CivilizationModel.RecentHistoryLimit&&bounded.HistoryPages.Count>=2,"society bounds its active event window and durably archives older pages");
        Check(archived.ReadHistoryPage(0)[0].Sequence==1,"older civilization history remains available on demand");
        var clock=new SimulationClock(WorldState.Restore(geo.Configuration.Seed,archived.Tick,(int)archived.TotalPopulation,(int)archived.TotalFood,12345));var record=WorldRecord.Capture(null,"Society persistence",null,geo,clock,"0.0.10",society:archived);repository.SaveAsync(record,false).GetAwaiter().GetResult();var loaded=repository.LoadAsync(record.Id).GetAwaiter().GetResult();
        Check(ArchiveCodec.Hash(ArchiveCodec.Encode(loaded.Record.Society))==ArchiveCodec.Hash(ArchiveCodec.Encode(bounded)),"actual versioned world archive retains society and paged historical references");
        var mismatched=ArchiveCodec.Decode<WorldRecord>(ArchiveCodec.Encode(record));mismatched.Population++;bool mismatchRejected=false;try{mismatched.Validate();}catch(InvalidDataException){mismatchRejected=true;}Check(mismatchRejected,"archive summary totals must equal authoritative society accounts");
        var worldCopy=ArchiveCodec.Decode<WorldRecord>(ArchiveCodec.Encode(record));worldCopy.Id=Guid.NewGuid().ToString("N");repository.SaveAsync(worldCopy,false).GetAwaiter().GetResult();Check(repository.LoadAsync(worldCopy.Id).GetAwaiter().GetResult().Record.Society.HistoryPages[0].Key==bounded.HistoryPages[0].Key,"duplicated worlds retain immutable shared history without rewriting events");
        var external=loaded.Record.RestoreClock();int pop=external.State.Population,food=external.State.Food;external.Start();external.Update(2);Check(external.State.Population==pop&&external.State.Food==food,"society-owned clock totals do not receive prototype food or random births");
        Console.WriteLine($"SOCIETY elapsed {timer.Elapsed.TotalSeconds:F2}s; {a.Civilizations.Count} civilizations, {a.Settlements.Count} settlements, population {a.TotalPopulation:F2}, months {a.Months}, latest step {a.LastStepMilliseconds:F3}ms, history {a.RecentHistory.Count} recent / {a.HistoryPages.Count} immutable pages. Evidence retained: {root}");
    }
}
