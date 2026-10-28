using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Aevoria.World
{
    public static class WorldGenerationIdentity
    {
        public static IReadOnlyDictionary<string,int> ModuleVersions {get;}=new System.Collections.ObjectModel.ReadOnlyDictionary<string,int>(new Dictionary<string,int>(StringComparer.Ordinal)
        {
            [WorldLayerIds.Continents]=ContinentalField.Version,[WorldLayerIds.Geology]=GeologyMap.Version,
            [WorldLayerIds.Tectonics]=TectonicStructureField.Version,
            [WorldLayerIds.Coasts]=CoastalLandformField.Version,
            [WorldLayerIds.Elevation]=BaseElevationField.Version,[WorldLayerIds.Mountains]=MountainClusterField.Version,
            [WorldLayerIds.Surface]=ComposedSurfaceField.Version,[WorldLayerIds.Climate]=ClimateMap.Version,
            [WorldLayerIds.Hydrology]=HydrologyMap.Version,[WorldLayerIds.Caves]=CaveNetwork.Version,
            [WorldLayerIds.Moisture]=MoistureField.Version,[WorldLayerIds.Biomes]=BiomeField.Version,
            [WorldLayerIds.Materials]=WorldMaterialField.Version,[WorldLayerIds.Ecosystem]=EcosystemField.Version,[WorldLayerIds.Civilizations]=CivilizationSiteField.Version
        });
        public static IReadOnlyDictionary<string,int> For(GenerationSettings settings)
        {
            if(settings.AtlasRevision==0)return ModuleVersions;
            var versions=new Dictionary<string,int>(StringComparer.Ordinal);foreach(var pair in ModuleVersions)versions.Add(pair.Key,pair.Value);
            versions.Add(WorldLayerIds.RegionalLandforms,RegionalLandformField.Version);
            return new System.Collections.ObjectModel.ReadOnlyDictionary<string,int>(versions);
        }
    }
    public static class WorldLayerIds
    {
        public const string Continents="continents",Geology="geology",Tectonics="tectonics",Coasts="coasts",Elevation="elevation",Mountains="mountains",Surface="surface",Climate="climate",Hydrology="hydrology",Moisture="moisture",Biomes="biomes",Caves="caves",Materials="materials",Ecosystem="ecosystem",Civilizations="civilizations",Assembly="assembly";
        public const string RegionalLandforms="regional-landforms";
    }
    public sealed class WorldModuleContract
    {
        public string Id{get;}public int Version{get;}public Type OutputType{get;}
        public IReadOnlyList<string> Dependencies{get;}
        public WorldModuleContract(string id,int version,Type outputType,params string[] dependencies)
        {
            if(string.IsNullOrWhiteSpace(id)||version<1||outputType==null||dependencies==null||dependencies.Any(string.IsNullOrWhiteSpace)||dependencies.Distinct(StringComparer.Ordinal).Count()!=dependencies.Length)throw new ArgumentException("Invalid world-module contract.");
            Id=id;Version=version;OutputType=outputType;Dependencies=Array.AsReadOnly((string[])dependencies.Clone());
        }
    }
    public interface IWorldGenerationModule
    {
        WorldModuleContract Contract{get;}
        object Generate(WorldGenerationContext context);
    }
    public abstract class WorldGenerationModule<T>:IWorldGenerationModule where T:class
    {
        public WorldModuleContract Contract{get;}
        protected WorldGenerationModule(string id,int version,params string[] dependencies){Contract=new WorldModuleContract(id,version,typeof(T),dependencies);}
        protected abstract T Build(WorldGenerationContext context);
        public object Generate(WorldGenerationContext context)=>Build(context);
    }
    internal sealed class FunctionalWorldModule<T>:WorldGenerationModule<T> where T:class
    {
        private readonly Func<WorldGenerationContext,T> build;
        public FunctionalWorldModule(string id,int version,Func<WorldGenerationContext,T> build,params string[] dependencies):base(id,version,dependencies){this.build=build;}
        protected override T Build(WorldGenerationContext context)=>build(context);
    }
    public sealed class WorldLayerArtifact
    {
        public WorldModuleContract Contract{get;}public ulong Seed{get;}private readonly object value;
        internal WorldLayerArtifact(WorldModuleContract contract,ulong seed,object value){Contract=contract;Seed=seed;this.value=value;}
        public T Read<T>()where T:class=>value as T??throw new InvalidOperationException("Layer "+Contract.Id+" is not "+typeof(T).FullName+".");
    }
    public sealed class WorldLayerSet
    {
        private readonly Dictionary<string,WorldLayerArtifact> layers=new Dictionary<string,WorldLayerArtifact>(StringComparer.Ordinal);
        public IReadOnlyList<string> Ids=>Array.AsReadOnly(layers.Keys.OrderBy(id=>id,StringComparer.Ordinal).ToArray());
        public WorldLayerArtifact Artifact(string id)=>layers.TryGetValue(id,out var layer)?layer:throw new InvalidOperationException("World layer is not ready: "+id);
        public T Read<T>(string id)where T:class=>Artifact(id).Read<T>();
        internal void Publish(WorldModuleContract contract,ulong seed,object value)
        {
            if(value==null||!contract.OutputType.IsInstanceOfType(value))throw new InvalidOperationException("Module "+contract.Id+" returned an invalid "+contract.OutputType.FullName+" product.");
            if(layers.ContainsKey(contract.Id))throw new InvalidOperationException("A world layer cannot be published twice: "+contract.Id);
            layers.Add(contract.Id,new WorldLayerArtifact(contract,seed,value));
        }
    }
    public static class WorldGenerationSeeds
    {
        public static double Site(ulong seed,int x,int z,uint channel)
        {unchecked{uint h=(uint)(seed^(seed>>32))^(uint)x*374761393u^(uint)z*668265263u^channel*2246822519u;h=(h^(h>>13))*1274126177u;h^=h>>16;return h/(double)uint.MaxValue;}}
        public static ulong Derive(string masterSeed,string module,int version)
        {
            if(string.IsNullOrWhiteSpace(masterSeed)||string.IsNullOrWhiteSpace(module)||version<1)throw new ArgumentException("A master seed and versioned module are required.");
            using(var hash=SHA256.Create())
            {
                byte[] bytes=hash.ComputeHash(Encoding.UTF8.GetBytes(masterSeed+"/world-module/"+module+"/"+version.ToString(CultureInfo.InvariantCulture)));ulong value=0;
                for(int i=0;i<8;i++)value|=(ulong)bytes[i]<<(i*8);return value==0?1:value;
            }
        }
    }
    public struct WorldGenerationRandom
    {
        private ulong state;
        public WorldGenerationRandom(ulong seed){if(seed==0)throw new ArgumentOutOfRangeException(nameof(seed));state=seed;}
        public double Next01(){ulong x=state;x^=x>>12;x^=x<<25;x^=x>>27;state=x;return ((x*2685821657736338717UL)>>11)*(1.0/(1UL<<53));}
    }
    public sealed class WorldGenerationContext
    {
        public WorldConfiguration Configuration{get;}public ClimateSettings ClimateSettings{get;}public GenerationDiagnostics Diagnostics{get;}
        public CancellationToken Cancellation{get;}public ulong ModuleSeed{get;private set;}
        private readonly WorldLayerSet products;private readonly double deadlineSeconds,startedSeconds;private WorldModuleContract active;
        internal WorldGenerationContext(WorldConfiguration config,ClimateSettings climate,WorldLayerSet products,GenerationDiagnostics diagnostics,CancellationToken cancellation,TimeSpan maximumTime)
        {Configuration=config;ClimateSettings=climate;this.products=products;Diagnostics=diagnostics;Cancellation=cancellation;deadlineSeconds=maximumTime.TotalSeconds;startedSeconds=diagnostics.ElapsedSeconds;}
        public T Input<T>(string id)where T:class
        {if(active==null||!active.Dependencies.Contains(id))throw new InvalidOperationException("Module "+active?.Id+" read an undeclared dependency: "+id);return products.Read<T>(id);}
        public WorldGenerationRandom CreateRandom()=>new WorldGenerationRandom(ModuleSeed);
        public void Checkpoint(string detail=null,long completed=0,long total=0)
        {
            Cancellation.ThrowIfCancellationRequested();
            if(Diagnostics.ElapsedSeconds-startedSeconds>deadlineSeconds)throw new TimeoutException("Generation exceeded its budget in "+active?.Id+": "+Diagnostics.Snapshot().Detail);
            Diagnostics.Pulse(detail,completed,total);
        }
        internal void Enter(WorldModuleContract contract){active=contract;ModuleSeed=WorldGenerationSeeds.Derive(Configuration.Seed,contract.Id,contract.Version);Diagnostics.Begin(contract.Id,contract.Version);Checkpoint();}
    }
    public sealed class WorldGenerationPipeline
    {
        private readonly IWorldGenerationModule[] ordered;
        public IReadOnlyList<WorldModuleContract> Contracts=>Array.AsReadOnly(ordered.Select(module=>module.Contract).ToArray());
        public WorldGenerationPipeline(IEnumerable<IWorldGenerationModule> modules)
        {
            if(modules==null)throw new ArgumentNullException(nameof(modules));var input=modules.ToArray();
            if(input.Any(m=>m?.Contract==null))throw new ArgumentException("Every generation module requires a contract.");
            var byId=new Dictionary<string,IWorldGenerationModule>(StringComparer.Ordinal);
            foreach(var module in input)if(byId.ContainsKey(module.Contract.Id))throw new ArgumentException("Duplicate world module: "+module.Contract.Id);else byId.Add(module.Contract.Id,module);
            foreach(var module in input)foreach(string dependency in module.Contract.Dependencies)if(!byId.ContainsKey(dependency))throw new ArgumentException("Missing dependency "+dependency+" for "+module.Contract.Id);
            var remaining=new SortedSet<string>(byId.Keys,StringComparer.Ordinal);var ready=new HashSet<string>(StringComparer.Ordinal);var sequence=new List<IWorldGenerationModule>();
            while(remaining.Count>0)
            {
                string next=remaining.FirstOrDefault(id=>byId[id].Contract.Dependencies.All(ready.Contains));
                if(next==null)throw new ArgumentException("World-generation dependencies contain a cycle: "+string.Join(", ",remaining));
                sequence.Add(byId[next]);ready.Add(next);remaining.Remove(next);
            }
            ordered=sequence.ToArray();
        }
        public WorldLayerSet Generate(WorldConfiguration config,ClimateSettings climate=null,GenerationDiagnostics diagnostics=null,CancellationToken cancellation=default,TimeSpan? maximumTime=null)
        {
            if(config==null)throw new ArgumentNullException(nameof(config));var limit=maximumTime??TimeSpan.FromMinutes(10);if(limit<=TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(maximumTime));
            var output=new WorldLayerSet();var trace=diagnostics??new GenerationDiagnostics();var context=new WorldGenerationContext(config,climate??new ClimateSettings(),output,trace,cancellation,limit);
            try
            {
                foreach(var module in ordered){context.Enter(module.Contract);object product=module.Generate(context);context.Checkpoint();output.Publish(module.Contract,context.ModuleSeed,product);trace.CompleteModule();}
                trace.Finish(GenerationOutcome.Completed);return output;
            }
            catch(OperationCanceledException){trace.Finish(GenerationOutcome.Cancelled);throw;}
            catch(Exception ex){trace.Finish(GenerationOutcome.Failed,ex.Message);throw;}
        }
    }
}
