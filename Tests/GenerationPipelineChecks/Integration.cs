using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevoria.World;
using Aevoria.Persistence;
using Aevoria.Simulation;

static class Integration
{
    static void Check(bool condition,string message){if(!condition)throw new Exception("FAILED: "+message);Console.WriteLine("PASS: "+message);}
    static void Reject(Action action,string message)
    {bool rejected=false;try{action();}catch(Exception ex)when(ex is System.IO.InvalidDataException||ex is NotSupportedException){rejected=true;}Check(rejected,message);}
    public static void Run()
    {
        var config=GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(mountains:3600,landmass:LandmassType.Continents,mountainPrototypeOnly:true));
        GenerationPreview climatePreview=null;var trace=new GenerationDiagnostics();var geo=new ProceduralGeography(config,progress:s=>Console.WriteLine("STAGE: "+s),preview:p=>{if(p.Stage=="Climate regions")climatePreview=p;},diagnostics:trace);
        Check(trace.Snapshot().Outcome==GenerationOutcome.Completed&&trace.Snapshot().RecentStages.Count==WorldGenerationIdentity.ModuleVersions.Count,"all geographic modules complete before the generation trace reports success");
        Check(geo.Layers.Ids.Count==WorldGenerationIdentity.ModuleVersions.Count&&geo.Layers.Ids.All(id=>geo.Layers.Artifact(id).Contract.Version==WorldGenerationIdentity.ModuleVersions[id]),"completed products match the persistent version manifest");
        Check(geo.Materials!=null&&geo.Ecosystem!=null&&geo.CivilizationSites!=null&&geo.CivilizationSites.InitialSites.Count>0,"materials, ecosystems and suitable settlement sites are actual completed generation products");
        var duplicateEcosystem=new EcosystemField(config,geo.Hydrology,geo.Hydrology,geo.Biomes,geo.Materials,geo.Ecosystem.Seed);int trees=0;bool samePlacements=true,materialDepth=true;
        for(int z=0;z<64;z++)for(int x=0;x<64;x++)
        {
            int xx=(int)((x/63.0-.5)*config.WidthMeters/16),zz=(int)((z/63.0-.5)*config.DepthMeters/16);bool firstTree=geo.Ecosystem.TryTree(xx,zz,out var one),secondTree=duplicateEcosystem.TryTree(xx,zz,out var two);samePlacements&=firstTree==secondTree&&(!firstTree||one.East==two.East&&one.North==two.North&&one.Variant==two.Variant);if(firstTree)trees++;
            double east=xx*16,north=zz*16;var material=geo.Materials.Sample(east,north);materialDepth&=material.SoilDepth>0&&material.SoilDepth<4&&geo.Materials.GroundMaterialAt(east,geo.ElevationAt(east,north)-20,north)==ResourceKind.Stone;
        }
        Check(trees>0&&samePlacements,"independent ecosystem fields with the same module seed reproduce real tree placements");
        Check(materialDepth,"geological soil depths are bounded and deep excavation consistently resolves to rock");
        Check(geo.CivilizationSites.InitialSites.All(s=>geo.CivilizationSites.Footprint(s.X,s.Z,28,out _)),"every initial civilization site has a dry, supported physical footprint");
        var hydro=geo.Hydrology;var ranks=Enumerable.Repeat(-1,hydro.Ground.Length).ToArray();bool acyclic=true;
        for(int i=0;i<hydro.FloodOrder.Length;i++){int node=hydro.FloodOrder[i];if(node<0||node>=ranks.Length||ranks[node]>=0){acyclic=false;break;}ranks[node]=i;}
        long totalCatchment=0;
        for(int i=0;i<ranks.Length;i++){int receiver=hydro.Receiver[i];if(receiver<0)totalCatchment+=hydro.Accumulation[i];else acyclic&=ranks[receiver]<ranks[i];}
        Check(acyclic,"completed receiver graph has one topological position per cell and no drainage cycles");
        Check(totalCatchment==hydro.Ground.Length,"terminal catchments conserve every contributing drainage cell exactly once");
        var rivers=geo.InlandSurfaces.Rivers;bool ownership=rivers.Vertices.Count>0;
        for(int i=0;i<rivers.Indices.Count;i+=3)
        {
            int owner=rivers.Vertices[rivers.Indices[i]].ChannelOwner;
            ownership&=owner>=0&&owner<geo.Hydrology.Ground.Length&&geo.Hydrology.IsChannelCandidate(owner)&&rivers.Vertices[rivers.Indices[i+1]].ChannelOwner==owner&&rivers.Vertices[rivers.Indices[i+2]].ChannelOwner==owner;
        }
        Check(ownership,"every clipped river triangle retains one valid categorical channel owner");
        Check(geo.InlandSurfaces.Lakes.Vertices.All(p=>p.ChannelOwner==-1)&&geo.InlandSurfaces.Falls.Vertices.All(p=>p.ChannelOwner==-1),"lake and waterfall geometry remain outside river-junction ownership clipping");
        Check(climatePreview!=null&&Enumerable.Range(0,climatePreview.Elevation.Length).All(i=>
        {
            double east=(i%GenerationPreview.Resolution/(double)(GenerationPreview.Resolution-1)-.5)*config.WidthMeters,north=(i/GenerationPreview.Resolution/(double)(GenerationPreview.Resolution-1)-.5)*config.DepthMeters;
            var sample=geo.Climate.Sample(east,north,climatePreview.Elevation[i]);
            return sample.Moisture==climatePreview.Moisture[i]&&sample.TemperatureCelsius==climatePreview.Temperature[i];
        }),"raw climate samples stay unchanged after downstream drainage and biomes finish");
        var clock=new SimulationClock(new WorldState(config.Seed));var record=WorldRecord.Capture(null,"v11 integration fixture",null,geo,clock,"0.0.11");
        var encoded=ArchiveCodec.Encode(record);var roundtrip=ArchiveCodec.Decode<WorldRecord>(encoded);roundtrip.Validate();
        Check(roundtrip.Schema==8&&roundtrip.Modules.Count==WorldGenerationIdentity.ModuleVersions.Count&&roundtrip.MountainPrototypeOnly&&roundtrip.Configuration().Terrain.MountainPrototypeOnly,"archive round-trip preserves schema, generation profile and every module seed/version");
        var altered=ArchiveCodec.Decode<WorldRecord>(encoded);altered.Modules[0].Version++;Reject(()=>altered.Validate(),"changed module versions are rejected before regeneration");
        altered=ArchiveCodec.Decode<WorldRecord>(encoded);altered.Modules[0].Seed="0000000000000000";Reject(()=>altered.Validate(),"changed module seed identity is rejected before regeneration");
        altered=ArchiveCodec.Decode<WorldRecord>(encoded);altered.Modules.RemoveAt(0);Reject(()=>altered.Validate(),"incomplete module manifests are rejected before regeneration");
        var rebuilt=roundtrip.Rebuild();Check(WorldRecord.GeographyFingerprint(rebuilt)==record.Fingerprint,"save/rebuild preserves the canonical geography fingerprint");
        Check(Enumerable.Range(0,30).All(i=>
        {
            var p=new GeoPoint((i/29.0-.5)*config.WidthMeters,0);float height=geo.ElevationAt(p.X,p.Y);
            var a=geo.Sample(p);var b=geo.Biomes.Sample(p.X,p.Y,height);
            return a.Biome==b.Biome&&a.TemperatureCelsius==b.TemperatureCelsius&&a.Moisture==b.Moisture;
        }),"geography queries use the completed biome and moisture products");
        using(var cancel=new CancellationTokenSource())
        {
            var cancellationTrace=new GenerationDiagnostics();var task=Task.Run(()=>new ProceduralGeography(config,token:cancel.Token,diagnostics:cancellationTrace));
            var deadline=Stopwatch.StartNew();bool requested=false;long requestedAt=0;
            while(!task.IsCompleted&&deadline.Elapsed.TotalSeconds<60)
            {
                var snapshot=cancellationTrace.Snapshot();
                if(snapshot.Detail.StartsWith("Flooding lake basin",StringComparison.Ordinal)){requested=true;requestedAt=Stopwatch.GetTimestamp();cancel.Cancel();break;}
                Thread.Sleep(1);
            }
            if(!requested)cancel.Cancel();
            var completion=Task.WhenAny(task,Task.Delay(5000)).GetAwaiter().GetResult();Check(requested&&completion==task,"cancellation requested inside a fine basin flood completes within five seconds");
            bool cancelled=false;try{task.GetAwaiter().GetResult();}catch(OperationCanceledException){cancelled=true;}
            double seconds=(Stopwatch.GetTimestamp()-requestedAt)/(double)Stopwatch.Frequency;
            Check(cancelled&&cancellationTrace.Snapshot().Outcome==GenerationOutcome.Cancelled,"deep-flood cancellation retains an explicit terminal diagnostic outcome");
            Console.WriteLine($"DEEP BASIN CANCELLATION LATENCY {seconds:F4}s (includes 1 ms polling and test cleanup)");
        }
        Console.WriteLine("GEOGRAPHIC INTEGRATION CHECKS COMPLETE");
    }
}
static class WaterDigest
{
    public static void Run(bool colossal)
    {
        string seed=colossal?"aevoria-001":"shaping-continents";var watch=Stopwatch.StartNew();
        var config=GenerationSettings.Preset(seed,colossal?WorldSize.Colossal:WorldSize.Miniature,new GenerationSettings(mountains:colossal?16000:3600,landmass:LandmassType.Continents));
        var geo=new ProceduralGeography(config,progress:s=>Console.WriteLine("STAGE: "+s));double generationSeconds=watch.Elapsed.TotalSeconds;
        using var hash=System.Security.Cryptography.SHA256.Create();
        using var crypto=new System.Security.Cryptography.CryptoStream(System.IO.Stream.Null,hash,System.Security.Cryptography.CryptoStreamMode.Write);
        using var buffer=new System.IO.BufferedStream(crypto,65536);using var writer=new System.IO.BinaryWriter(buffer,System.Text.Encoding.UTF8,true);
        var h=geo.Hydrology;writer.Write(h.Ground.Length);
        for(int i=0;i<h.Ground.Length;i++){writer.Write(h.Ground[i]);writer.Write(h.Water[i]);writer.Write(h.Discharge[i]);writer.Write(h.Receiver[i]);writer.Write(h.Accumulation[i]);writer.Write(h.Lake[i]);}
        foreach(var mesh in new[]{geo.InlandSurfaces.Lakes,geo.InlandSurfaces.Rivers,geo.InlandSurfaces.Falls})
        {
            writer.Write(mesh.Vertices.Count);writer.Write(mesh.Indices.Count);
            foreach(var p in mesh.Vertices){writer.Write(p.X);writer.Write(p.Z);writer.Write(p.Level);writer.Write(p.SignedDepth);writer.Write(p.FlowX);writer.Write(p.FlowZ);writer.Write(p.Energy);writer.Write(p.ChannelOwner);}
            foreach(int index in mesh.Indices)writer.Write(index);
        }
        writer.Flush();buffer.Flush();crypto.FlushFinalBlock();
        Console.WriteLine("WATER_DIGEST "+Convert.ToHexString(hash.Hash));Console.WriteLine("GEOGRAPHY_FINGERPRINT "+WorldRecord.GeographyFingerprint(geo));
        Console.WriteLine(FormattableString.Invariant($"GENERATION_SECONDS {generationSeconds:F4}"));Console.WriteLine("SHORE_SAMPLES "+geo.InlandSurfaces.ShoreSamples);
    }
}
