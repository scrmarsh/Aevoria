using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Aevoria.Simulation;
using Aevoria.World;

namespace Aevoria.Persistence
{
    [Serializable] public sealed class WorldModuleRecord
    {
        public string Id;public int Version;public string Seed;
    }
    [Serializable] public sealed class WorldRecord
    {
        public const int CurrentSchema=8;
        public int Schema=CurrentSchema, Generator=ProceduralGeography.GeneratorVersion, Hydrology=HydrologyMap.Version,
            Climate=ClimateMap.Version, Calendar=WorldCalendar.Version, Weather=WorldWeather.Version,Caves=CaveNetwork.Version;
        public string Id, Name, Seed, CreatedUtc, SavedUtc, LastPlayedUtc, ApplicationVersion, Fingerprint;
        public int ChunksX, ChunksY, Cells, Population, Food, Day;
        public double CellSize, Speed, Backlog;
        public long Tick, Year;
        public bool Running;
        public bool MountainPrototypeOnly;
        public GeographyProfile GeographyProfile;
        public int AtlasRevision;
        public string RandomState;
        public float Sea, Scale, Elevation, Mountains, Roughness, CoastVariation, Land, Temperature, Rainfall, Latitude;
        public int RenderRadius, NearRadius, MediumRadius,Landmass;
        public List<ExtensionSection> Extensions=new List<ExtensionSection>();
        public List<WorldModuleRecord> Modules=new List<WorldModuleRecord>();
        public int WaterLayout=InlandWaterLayout.Version;
        public EcologySnapshot Ecology;
        public LivingWorldSnapshot Living;
        public CivilizationSnapshot Society;
        public static WorldRecord Capture(string id,string name,string created,ProceduralGeography geo,SimulationClock clock,string version,EcologyModel ecology=null,LivingWorldState living=null,CivilizationModel society=null)
        {
            ValidateName(name);var c=geo.Configuration;var s=c.Terrain;var climate=geo.Climate.Settings;var calendar=new WorldCalendar(clock.State.Tick);
            var ecologicalState=ecology??new EcologyModel(geo,clock.State.Tick);
            var social=society??new CivilizationModel(geo,clock.State.Tick);
            var modules=new List<WorldModuleRecord>();
            foreach(string layer in geo.Layers.Ids){var artifact=geo.Layers.Artifact(layer);modules.Add(new WorldModuleRecord{Id=layer,Version=artifact.Contract.Version,Seed=artifact.Seed.ToString("X16",CultureInfo.InvariantCulture)});}
            return new WorldRecord {Modules=modules,Id=id??Guid.NewGuid().ToString("N"),Name=name.Trim(),Seed=c.Seed,
                CreatedUtc=created??DateTime.UtcNow.ToString("O"),SavedUtc=DateTime.UtcNow.ToString("O"),LastPlayedUtc=DateTime.UtcNow.ToString("O"),ApplicationVersion=version,
                ChunksX=c.ChunksX,ChunksY=c.ChunksY,Cells=c.CellsPerChunk,CellSize=c.CellSizeMeters,
                Population=(int)Math.Min(int.MaxValue,social.TotalPopulation),Food=(int)Math.Min(int.MaxValue,social.TotalFood),Tick=clock.State.Tick,RandomState=clock.State.RandomState.ToString(CultureInfo.InvariantCulture),
                Speed=clock.Speed,Backlog=clock.BacklogSeconds,Running=clock.Running,Year=calendar.Year,Day=calendar.Day,
                GeographyProfile=s.Profile,AtlasRevision=s.AtlasRevision,MountainPrototypeOnly=s.MountainPrototypeOnly,Sea=s.SeaLevel,Scale=s.ContinentalScale,Elevation=s.Elevation,Mountains=s.Mountains,Roughness=s.Roughness,CoastVariation=s.CoastVariation,Land=s.TargetLandFraction,
                Landmass=(int)s.Landmass,RenderRadius=s.RenderRadius,NearRadius=s.NearLodRadius,MediumRadius=s.MediumLodRadius,Temperature=climate.TemperatureOffset,Rainfall=climate.Rainfall,Latitude=climate.Latitude,
                Fingerprint=GeographyFingerprint(geo),Ecology=ecologicalState.Capture(),Living=(living??new LivingWorldState(geo,ecologicalState,clock.State.Tick)).Capture(),Society=social.Capture()};
        }
        public static void ValidateName(string name) {if(string.IsNullOrWhiteSpace(name)||name.Trim().Length>80||name.IndexOfAny(new[]{'\r','\n','\t'})>=0) throw new ArgumentException("Use a world name of 1–80 characters.");}
        public WorldConfiguration Configuration() => new WorldConfiguration(Seed,ChunksX,ChunksY,Cells,CellSize,
            new GenerationSettings(Sea,Scale,Elevation,Mountains,Roughness,CoastVariation,RenderRadius,NearRadius,MediumRadius,Land,(LandmassType)Landmass,MountainPrototypeOnly,GeographyProfile,AtlasRevision));
        public SimulationClock RestoreClock()
        {
            var clock=new SimulationClock(WorldState.Restore(Seed,Tick,Population,Food,ulong.Parse(RandomState,CultureInfo.InvariantCulture)));
            if(Society!=null)clock.State.UseAggregatePopulation(Population,Food);
            clock.RestoreControlState(Speed,Running,Backlog);return clock;
        }
        public void Validate(bool allowLegacy=false)
        {
            bool legacy=Schema==3&&allowLegacy;
            if(Schema!=CurrentSchema&&!legacy) throw new NotSupportedException(Schema>CurrentSchema?"Save was written by a newer schema.":"Save needs an explicit schema migration. Earlier worlds require their matching historical build; files are preserved.");
            if(Generator!=ProceduralGeography.GeneratorVersion || Hydrology!=HydrologyMap.Version || Climate!=ClimateMap.Version || Calendar!=WorldCalendar.Version || Weather!=WorldWeather.Version || Caves!=CaveNetwork.Version)
                throw new NotSupportedException("This world's generator or environment needs an explicit migration. The original save is preserved.");
            if(!legacy)
            {
                var expected=WorldGenerationIdentity.For(Configuration().Terrain);var found=new HashSet<string>(StringComparer.Ordinal);
                if(Modules==null||Modules.Count!=expected.Count||WaterLayout!=InlandWaterLayout.Version)throw new NotSupportedException("World module manifest requires an explicit migration; original preserved.");
                foreach(var module in Modules)
                    if(module==null||module.Id==null||!found.Add(module.Id)||!expected.TryGetValue(module.Id,out int version)||module.Version!=version||module.Seed!=WorldGenerationSeeds.Derive(Seed,module.Id,version).ToString("X16",CultureInfo.InvariantCulture))
                        throw new InvalidDataException("World module version or seed identity mismatch; original preserved.");
            }
            if(!Guid.TryParseExact(Id,"N",out _)) throw new InvalidDataException("Invalid world identity.");
            ValidateName(Name);Configuration();new ClimateSettings(Temperature,Rainfall,Latitude);RestoreClock();
            int regions=Math.Max(8,Math.Min(64,(int)Math.Ceiling(Math.Max(ChunksX*Cells*CellSize,ChunksY*Cells*CellSize)/1024)));
            EcologyModel.ValidateSnapshot(Ecology,Seed,regions);
            if(Ecology.Tick>Tick || Tick-Ecology.Tick>=EcologyModel.TicksPerMonth) throw new InvalidDataException("Ecology and calendar are not synchronized.");
            if(!legacy) LivingWorldState.Validate(Living,Configuration(),Tick,regions);
            if(!legacy){CivilizationModel.Validate(Society,Configuration(),Tick);if(Tick-Society.Tick>=CivilizationModel.TicksPerMonth)throw new InvalidDataException("Society and calendar are not synchronized.");}
            if(!legacy){double people=0,stores=0;foreach(var place in Society.Settlements){people+=place.Population;stores+=place.Resources.Food;}if(Population!=(int)Math.Min(int.MaxValue,people)||Food!=(int)Math.Min(int.MaxValue,stores))throw new InvalidDataException("Archive totals disagree with authoritative society accounts.");}
            var calendar=new WorldCalendar(Tick);
            if(calendar.Year!=Year || calendar.Day!=Day || string.IsNullOrEmpty(Fingerprint) || !DateTime.TryParse(CreatedUtc,null,DateTimeStyles.RoundtripKind,out _) || !DateTime.TryParse(SavedUtc,null,DateTimeStyles.RoundtripKind,out _))
                throw new InvalidDataException("Invalid calendar, dates or geography fingerprint.");
        }
        public ProceduralGeography Rebuild(bool allowLegacy=false,Action<string> progress=null,CancellationToken token=default,Action<GenerationPreview> preview=null,GenerationDiagnostics diagnostics=null)
        {
            Validate(allowLegacy);var geo=new ProceduralGeography(Configuration(),new ClimateSettings(Temperature,Rainfall,Latitude),progress,token,preview,diagnostics);
            if(GeographyFingerprint(geo)!=Fingerprint) throw new InvalidDataException("Geography fingerprint mismatch; the save was not changed.");return geo;
        }
        public static string GeographyFingerprint(ProceduralGeography geo)
        {
            using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream))
            {
                writer.Write(ProceduralGeography.GeneratorVersion);writer.Write(HydrologyMap.Version);writer.Write(ClimateMap.Version);writer.Write(CaveNetwork.Version);
                writer.Write(InlandWaterLayout.Version);
                foreach(string id in geo.Layers.Ids){var layer=geo.Layers.Artifact(id);writer.Write(id);writer.Write(layer.Contract.Version);writer.Write(layer.Seed);}
                var c=geo.Configuration;writer.Write(c.Seed);writer.Write(c.WidthMeters);writer.Write(c.DepthMeters);writer.Write(c.Terrain.MountainPrototypeOnly);
                // Preserve the exact historical fingerprint byte stream.
                if(c.Terrain.Profile!=World.GeographyProfile.Legacy){writer.Write("geography-profile");writer.Write((int)c.Terrain.Profile);}
                if(c.Terrain.AtlasRevision>0){writer.Write("atlas-terrain-revision");writer.Write(c.Terrain.AtlasRevision);}
                for(int z=0;z<17;z++) for(int x=0;x<17;x++)
                {
                    var p=new GeoPoint((x/16.0-0.5)*c.WidthMeters,(z/16.0-0.5)*c.DepthMeters);var s=geo.Sample(p);
                    writer.Write(s.ElevationMeters);writer.Write(s.Moisture);writer.Write(s.TemperatureCelsius);writer.Write((byte)s.Biome);
                }
                writer.Flush();return ArchiveCodec.Hash(stream.ToArray());
            }
        }
    }
    [Serializable] public sealed class ExtensionSection {public string Key; public int Version; public string Payload;}
    [Serializable] public sealed class ArchiveContainer {public int Schema=1;public string Sha256;public string Payload;}
    [Serializable] public sealed class WorldMetadata
    {
        public string Id,Name,Seed,SavedUtc,LastPlayedUtc,CreatedUtc,ApplicationVersion;
        public long Year,Tick; public int Day,Schema,Generator; public double Width,Depth;
        public static WorldMetadata From(WorldRecord r)=>new WorldMetadata {Id=r.Id,Name=r.Name,Seed=r.Seed,SavedUtc=r.SavedUtc,LastPlayedUtc=r.LastPlayedUtc,CreatedUtc=r.CreatedUtc,
            ApplicationVersion=r.ApplicationVersion,Year=r.Year,Day=r.Day,Tick=r.Tick,Schema=r.Schema,Generator=r.Generator,Width=r.ChunksX*r.Cells*r.CellSize,Depth=r.ChunksY*r.Cells*r.CellSize};
    }
    public sealed class SaveListing {public string Id,Status;public WorldMetadata Metadata;public bool HasBackup;}
    public sealed class LoadedWorld {public WorldRecord Record;public bool RecoveredBackup;}
    public static class ArchiveCodec
    {
        public static string Hash(byte[] data) {using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","");}
        public static byte[] Encode<T>(T value)
        {
            using(var stream=new MemoryStream()) {new XmlSerializer(typeof(T)).Serialize(stream,value);return stream.ToArray();}
        }
        public static T Decode<T>(byte[] bytes)
        {
            using(var stream=new MemoryStream(bytes)) using(var reader=XmlReader.Create(stream,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=0}))
                return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
        }
        public static byte[] Pack(WorldRecord value)
        {var payload=Encode(value);return Encode(new ArchiveContainer {Sha256=Hash(payload),Payload=Convert.ToBase64String(payload)});}
        public static WorldRecord Unpack(byte[] bytes,bool allowLegacy=false)
        {
            var container=Decode<ArchiveContainer>(bytes);if(container.Schema!=1) throw new NotSupportedException("Unsupported archive container.");
            var payload=Convert.FromBase64String(container.Payload);if(Hash(payload)!=container.Sha256) throw new InvalidDataException("Archive checksum mismatch.");
            var record=Decode<WorldRecord>(payload);record.Validate(allowLegacy);return record;
        }
        public static void AtomicWrite(string path,byte[] bytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temporary=path+".tmp";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)) {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
            // No delete-and-move fallback: a failed replace must leave the last
            // complete save intact. A stray temp file is never treated as a save.
            if(File.Exists(path)) File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
        }
    }
    public sealed class WorldRepository
    {
        public string Root { get; }
        public CivilizationHistoryArchive History{get;}
        private readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
        public WorldRepository(string root) {Root=Path.GetFullPath(root??throw new ArgumentNullException(nameof(root)));History=new CivilizationHistoryArchive(Path.Combine(Root,"SocietyHistory"));}
        private string Folder(string id)
        {if(!Guid.TryParseExact(id,"N",out _)) throw new ArgumentException("Invalid world ID.");return Path.Combine(Root,id);}
        public async Task SaveAsync(WorldRecord record,bool allowReplace=true)
        {
            record.Validate();string folder=Folder(record.Id);
            await gate.WaitAsync().ConfigureAwait(false);
            try {await Task.Run(()=> {
                History.Verify(record.Society.HistoryPages);
                byte[] archive=ArchiveCodec.Pack(record),metadata=ArchiveCodec.Encode(WorldMetadata.From(record));
                string path=Path.Combine(folder,"world.xml");
                if(File.Exists(path)) {if(!allowReplace) throw new IOException("This world ID already exists.");ArchiveCodec.Unpack(File.ReadAllBytes(path));}
                ArchiveCodec.AtomicWrite(path,archive);ArchiveCodec.AtomicWrite(Path.Combine(folder,"metadata.xml"),metadata);
            }).ConfigureAwait(false);} finally {gate.Release();}
        }
        public async Task<LoadedWorld> LoadAsync(string id)
        {
            string path=Path.Combine(Folder(id),"world.xml");await gate.WaitAsync().ConfigureAwait(false);
            try {return await Task.Run(()=> {
                try {var record=ArchiveCodec.Unpack(File.ReadAllBytes(path));if(record.Id!=id) throw new InvalidDataException("Save identity mismatch.");History.Verify(record.Society.HistoryPages);return new LoadedWorld {Record=record};}
                catch(NotSupportedException) {throw;}
                catch(Exception primary) when(primary is IOException || primary is InvalidOperationException || primary is ArgumentException || primary is FormatException)
                {
                    if(!File.Exists(path+".bak")) throw new InvalidDataException("Save is damaged and has no complete backup.",primary);
                    var backup=ArchiveCodec.Unpack(File.ReadAllBytes(path+".bak"));if(backup.Id!=id) throw new InvalidDataException("Backup identity mismatch.");
                    History.Verify(backup.Society.HistoryPages);
                    return new LoadedWorld {Record=backup,RecoveredBackup=true};
                }
            }).ConfigureAwait(false);} finally {gate.Release();}
        }
        public Task<WorldRecord> UpgradeCopyAsync(string id)
        {Folder(id);throw new NotSupportedException("This archive requires its original Aevoria build. Alpha v0.0.8 uses a new generator; original files are preserved. Create a new world to use planetary generation.");}
        public async Task<List<SaveListing>> ListAsync()
        {
            await gate.WaitAsync().ConfigureAwait(false);
            try {return await Task.Run(()=> {
                var list=new List<SaveListing>();if(!Directory.Exists(Root)) return list;
                foreach(string folder in Directory.GetDirectories(Root))
                {
                    string id=Path.GetFileName(folder);if(!Guid.TryParseExact(id,"N",out _)) continue;
                    var item=new SaveListing {Id=id,HasBackup=File.Exists(Path.Combine(folder,"world.xml.bak"))};
                    try {item.Metadata=ArchiveCodec.Decode<WorldMetadata>(File.ReadAllBytes(Path.Combine(folder,"metadata.xml")));
                        if(item.Metadata.Id!=id) throw new InvalidDataException("Metadata identity mismatch.");
                        item.Status=!File.Exists(Path.Combine(folder,"world.xml"))?"Missing world data":item.Metadata.Schema>WorldRecord.CurrentSchema?"Newer save schema":
                            item.Metadata.Schema!=WorldRecord.CurrentSchema || item.Metadata.Generator!=ProceduralGeography.GeneratorVersion?"Incompatible generation — original preserved":"Ready (validated on load)";}
                    catch(Exception ex) when(ex is IOException || ex is InvalidOperationException) {item.Status="Metadata damaged — try backup";}
                    list.Add(item);
                }
                list.Sort((a,b)=>string.CompareOrdinal(b.Metadata?.LastPlayedUtc??b.Metadata?.SavedUtc,a.Metadata?.LastPlayedUtc??a.Metadata?.SavedUtc));return list;
            }).ConfigureAwait(false);} finally {gate.Release();}
        }
        public async Task DeleteAsync(string id)
        {
            await gate.WaitAsync().ConfigureAwait(false);
            try {await Task.Run(()=> {string folder=Folder(id);if(Directory.Exists(folder)) Directory.Delete(folder,true);}).ConfigureAwait(false);}finally {gate.Release();}
        }
    }
    [Serializable] public sealed class ApplicationSettings
    {
        public Aevoria.Core.UnitSystem Units;
        public List<Aevoria.Core.KeyBinding> Bindings=new List<Aevoria.Core.KeyBinding>();
        public bool DoctorUnlocked,CharacterDialogue=true;
        public float DialogueDuration=6;
        public int Schema=1,FrameCap=120,VSync=0,Quality=2,AutosaveSeconds=120;
        public int DisplayWidth,DisplayHeight,DisplayMode;
        public bool MotionBlur,TerrainFollowing,TerrainCollision,Shadows=true,Antialiasing=true;
        public float ZoomSensitivity=1.1f;
        public float FlightSpeed=25,DoubleTapWindow=.3f,MinimumPanSpeed=60;
        public bool RenderClouds=true,RenderWeatherEffects=true;
        public int CloudDensity=2,TerrainTextureQuality=1,StreamingAggressiveness=1,BackgroundStreamingBudget=2;
        public int PresentationCacheMiB=128;
        public void ApplyGraphicsPreset(int preset)
        {
            preset=Math.Max(0,Math.Min(2,preset));TerrainDetail=preset+1;TerrainDistance=6+preset*2;MountainDetail=preset+1;DistantDetail=preset==0?1:2;
            TerrainTextureQuality=preset;VegetationDistance=3200+preset*2000;VegetationDensity=1+preset*.25f;LodTransition=preset;StreamingAggressiveness=preset;BackgroundStreamingBudget=preset+2;PresentationCacheMiB=128<<preset;CloudQuality=preset==2?2:1;ShadowDistance=1500+preset*1500;
        }
        public int TerrainDetail=1,TerrainDistance=6,MountainDetail=1,DistantDetail=1,WaterQuality=1,CloudQuality=1,WaterTransparency=2,LodTransition=1;
        public float VegetationDistance=3200,VegetationDensity=1,ShadowDistance=1500,FogDensity=1,FogDistance=20000,HazeStrength=1,DistantScenery=1,MasterVolume=1;
        public bool FogEnabled=true,ShowFps,ShowFrameTime;
        public bool KeyboardMovement=true,MiddlePanning=true,EdgeScrolling,MinimapEnabled,MinimapHeadingUp,MapResources=true,MapWaypoints=true;
        public int EdgeZone=16,MinimapShape,MinimapCorner=1,MinimapSize=180;
        public float MovementSensitivity=1,EdgeSpeed=1,MinimapOpacity=.85f,MinimapZoom=4,HeadBob=.025f;
        public void Validate()
        {Aevoria.Core.InputBindings.Validate(Bindings);if(!Enum.IsDefined(typeof(Aevoria.Core.UnitSystem),Units))throw new ArgumentException("Invalid measurement units.");if(!Finite(DialogueDuration,3,15))throw new ArgumentException("Dialogue duration must be between 3 and 15 seconds.");if(!Finite(MinimumPanSpeed,10,500)||!Finite(FlightSpeed,.5f,5000)||!Finite(DoubleTapWindow,.15f,.6f)||CloudDensity<0||CloudDensity>3||TerrainTextureQuality<0||TerrainTextureQuality>2||StreamingAggressiveness<0||StreamingAggressiveness>2||BackgroundStreamingBudget<1||BackgroundStreamingBudget>4||PresentationCacheMiB<64||PresentationCacheMiB>1024||WaterTransparency<0||WaterTransparency>2||LodTransition<0||LodTransition>2||TerrainDetail<0||TerrainDetail>3||TerrainDistance<2||TerrainDistance>10||MountainDetail<0||MountainDetail>3||DistantDetail<0||DistantDetail>2||WaterQuality<0||WaterQuality>2||CloudQuality<0||CloudQuality>2||!Finite(VegetationDistance,200,8000)||!Finite(VegetationDensity,.1f,2)||!Finite(ShadowDistance,100,10000)||!Finite(FogDensity,0,3)||!Finite(FogDistance,1000,100000)||!Finite(HazeStrength,0,3)||!Finite(DistantScenery,0,2)||!Finite(MasterVolume,0,1)||Schema!=1||FrameCap<30||FrameCap>360||VSync<0||VSync>1||Quality<0||Quality>5||AutosaveSeconds<0||AutosaveSeconds>3600||float.IsNaN(ZoomSensitivity)||ZoomSensitivity<0.2f||ZoomSensitivity>2.5f||DisplayMode<0||DisplayMode>2|| !((DisplayWidth==0&&DisplayHeight==0)||(DisplayWidth>=800&&DisplayWidth<=7680&&DisplayHeight>=600&&DisplayHeight<=4320))||EdgeZone<4||EdgeZone>80||!Finite(MovementSensitivity,.2f,4)||!Finite(EdgeSpeed,.2f,3)||MinimapShape<0||MinimapShape>2||MinimapCorner<0||MinimapCorner>3||MinimapSize<100||MinimapSize>300||!Finite(MinimapOpacity,.2f,1)||!Finite(MinimapZoom,1,32)||!Finite(HeadBob,0,.12f)) throw new ArgumentException("Invalid application settings.");}
        private static bool Finite(float n,float min,float max)=>!float.IsNaN(n)&&!float.IsInfinity(n)&&n>=min&&n<=max;
        public ApplicationSettings Copy() {var copy=(ApplicationSettings)MemberwiseClone();copy.Bindings=Aevoria.Core.InputBindings.Copy(Bindings);return copy;}
    }
}
