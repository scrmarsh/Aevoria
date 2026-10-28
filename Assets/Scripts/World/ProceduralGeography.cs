using System;

namespace Aevoria.World
{
    public enum WorldSize { Miniature,Small,Medium,Large,Colossal,Huge=Large }
    public enum TerrainRegion { Ocean, Coast, Plain, Hills, Plateau, Mountain, Snow }
    // Persisted separately: absent in historical saves means the original algorithm.
    public enum GeographyProfile { Legacy=0, Atlas=1 }

    // Immutable settings are part of generator identity.
    public sealed class GenerationSettings
    {
        public const float MaximumMountainRelief = 16000;
        public GeographyProfile Profile {get;}
        public int AtlasRevision {get;}
        public LandmassType Landmass {get;}
        public float SeaLevel { get; }
        public float ContinentalScale { get; }
        public float Elevation { get; }
        public float Mountains { get; }
        public float Roughness { get; }
        public float CoastVariation { get; }
        public float TargetLandFraction { get; }
        public bool MountainPrototypeOnly {get;}
        public int RenderRadius { get; }
        public int NearLodRadius { get; }
        public int MediumLodRadius { get; }
        public GenerationSettings(float seaLevel = 0, float continentalScale = 9000, float elevation = 240,
            float mountains = 3600, float roughness = 0.65f, float coastVariation = 0.7f,
            int renderRadius = 6, int nearLodRadius = 1, int mediumLodRadius = 3, float targetLandFraction = 0.54f, LandmassType landmass=LandmassType.Random,bool mountainPrototypeOnly=false,GeographyProfile profile=GeographyProfile.Legacy,int atlasRevision=0)
        {
            if (!In(seaLevel, -500, 500) || !In(continentalScale, 200, 20000) || !In(elevation, 20, 1000) ||
                !In(mountains, 0, MaximumMountainRelief) || !In(roughness, 0, 1) || !In(coastVariation, 0, 1) ||
                !In(targetLandFraction,0.3f,0.85f) || renderRadius < 2 || renderRadius > 10 || nearLodRadius < 0 || mediumLodRadius <= nearLodRadius || mediumLodRadius >= renderRadius)
                throw new ArgumentException("Invalid terrain settings.");
            if(!Enum.IsDefined(typeof(LandmassType),landmass))throw new ArgumentOutOfRangeException(nameof(landmass));Landmass=landmass;
            SeaLevel = seaLevel; ContinentalScale = continentalScale; Elevation = elevation;
            Mountains = mountains; Roughness = roughness; CoastVariation = coastVariation;
            RenderRadius = renderRadius; NearLodRadius = nearLodRadius; MediumLodRadius = mediumLodRadius;
            TargetLandFraction = targetLandFraction;
            MountainPrototypeOnly=mountainPrototypeOnly;
            if(!Enum.IsDefined(typeof(GeographyProfile),profile))throw new ArgumentOutOfRangeException(nameof(profile));Profile=profile;
            if(atlasRevision<0||atlasRevision>1||atlasRevision>0&&profile!=GeographyProfile.Atlas)throw new ArgumentOutOfRangeException(nameof(atlasRevision),"Unsupported Atlas terrain revision; existing worlds must retain their recorded revision.");AtlasRevision=atlasRevision;
        }
        private static bool In(float n, float low, float high) => WorldConfiguration.Finite(n) && n >= low && n <= high;
        public static WorldConfiguration Preset(string seed, WorldSize size, GenerationSettings settings = null)
        {
            if(!Enum.IsDefined(typeof(WorldSize),size))throw new ArgumentOutOfRangeException(nameof(size));
            int chunks = size==WorldSize.Miniature?250:size==WorldSize.Small?500:size==WorldSize.Medium?1000:size==WorldSize.Large?2000:4000;
            return new WorldConfiguration(seed, chunks, chunks, 64, 8, settings ?? new GenerationSettings());
        }
    }

    public sealed class ProceduralGeography : IGeography,IWorldHeightField
    {
        public InlandWaterLayout InlandSurfaces {get;private set;}
        public const int GeneratorVersion = 11;
        public ContinentalField Continents {get;private set;}
        public BaseElevationField OrdinaryTerrain {get;private set;}
        public MountainClusterField Mountains {get;private set;}
        public ComposedSurfaceField Surface {get;private set;}
        public MoistureField Moisture {get;private set;}
        public BiomeField Biomes {get;private set;}
        public WorldMaterialField Materials {get;private set;}
        public EcosystemField Ecosystem {get;private set;}
        public CivilizationSiteField CivilizationSites {get;private set;}
        public WorldLayerSet Layers {get;private set;}
        public GenerationDiagnostics Diagnostics {get;}=new GenerationDiagnostics();
        public CaveNetwork Caves {get;private set;}
        public GeologyMap Geology {get;private set;}
        public TectonicStructureField Tectonics {get;private set;}
        public CoastalLandformField Coasts {get;private set;}
        public double CoastThreshold=>coastThreshold;
        public HydrologyMap Hydrology { get; private set; }
        public ClimateMap Climate { get; private set; }
        public WorldConfiguration Configuration { get; }
        public float SeaLevel => Configuration.Terrain.SeaLevel;
        private readonly uint seed;
        private double coastThreshold;
        public float CalibrationLandFraction { get; private set; }
        public double CalibrationMilliseconds {get;private set;}
        public double HydrologyMilliseconds {get;private set;}
        public double ClimateMilliseconds {get;private set;}
        public ProceduralGeography(WorldConfiguration configuration, ClimateSettings climate = null,Action<string> progress=null,System.Threading.CancellationToken token=default,Action<GenerationPreview> preview=null,GenerationDiagnostics diagnostics=null)
        {
            Diagnostics=diagnostics??new GenerationDiagnostics();Configuration=configuration??throw new ArgumentNullException(nameof(configuration));var timer=System.Diagnostics.Stopwatch.StartNew();
            unchecked{seed=2166136261;foreach(char c in configuration.Seed){seed^=c;seed*=16777619;}}
            bool regionalLandforms=configuration.Terrain.AtlasRevision>0;
            string drainageSurface=regionalLandforms?WorldLayerIds.RegionalLandforms:WorldLayerIds.Surface;
            var terrainModules=new IWorldGenerationModule[]
            {
                new FunctionalWorldModule<ContinentalField>(WorldLayerIds.Continents,ContinentalField.Version,c=>
                {
                    progress?.Invoke("Continents and coast calibration");
                    Continents=new ContinentalField(configuration,c.ModuleSeed,token,c.Checkpoint);
                    coastThreshold=Continents.CoastThreshold;CalibrationLandFraction=(float)Continents.CalibratedLandFraction;GenerationPreview.Publish(preview,this,"Continental outlines");return Continents;
                }),
                new FunctionalWorldModule<GeologyMap>(WorldLayerIds.Geology,GeologyMap.Version,c=>
                {
                    progress?.Invoke("Tectonic plate structure");return Geology=new GeologyMap(configuration,c.Input<ContinentalField>(WorldLayerIds.Continents),c.ModuleSeed,token);
                },WorldLayerIds.Continents),
                new FunctionalWorldModule<TectonicStructureField>(WorldLayerIds.Tectonics,TectonicStructureField.Version,c=>
                {
                    progress?.Invoke("Curved faults, fold provinces and uplifted blocks");Tectonics=new TectonicStructureField(configuration,c.Input<ContinentalField>(WorldLayerIds.Continents),c.Input<GeologyMap>(WorldLayerIds.Geology),c.ModuleSeed,token,c.Checkpoint);return Tectonics;
                },WorldLayerIds.Continents,WorldLayerIds.Geology),
                new FunctionalWorldModule<BaseElevationField>(WorldLayerIds.Elevation,BaseElevationField.Version,c=>
                {OrdinaryTerrain=new BaseElevationField(configuration,c.Input<ContinentalField>(WorldLayerIds.Continents),c.ModuleSeed,c.Input<TectonicStructureField>(WorldLayerIds.Tectonics));GenerationPreview.Publish(preview,this,"Fault blocks and rolling elevation");return OrdinaryTerrain;},WorldLayerIds.Continents,WorldLayerIds.Tectonics),
                new FunctionalWorldModule<MountainClusterField>(WorldLayerIds.Mountains,MountainClusterField.Version,c=>
                {
                    progress?.Invoke(configuration.Terrain.MountainPrototypeOnly?"Structured mountain formation prototype":"Fault-aligned massifs, fold belts and branching ridges");return Mountains=new MountainClusterField(configuration,c.Input<ContinentalField>(WorldLayerIds.Continents),c.ModuleSeed,token,c.Input<TectonicStructureField>(WorldLayerIds.Tectonics));
                },WorldLayerIds.Continents,WorldLayerIds.Tectonics),
                new FunctionalWorldModule<CoastalLandformField>(WorldLayerIds.Coasts,CoastalLandformField.Version,c=>
                {
                    progress?.Invoke("Glacial fjords, sheltered coves and rocky headlands");
                    return Coasts=new CoastalLandformField(configuration,c.Input<ContinentalField>(WorldLayerIds.Continents),c.Input<BaseElevationField>(WorldLayerIds.Elevation),c.Input<MountainClusterField>(WorldLayerIds.Mountains),c.Input<TectonicStructureField>(WorldLayerIds.Tectonics),c.ModuleSeed,token,c.Checkpoint);
                },WorldLayerIds.Continents,WorldLayerIds.Elevation,WorldLayerIds.Mountains,WorldLayerIds.Tectonics),
                new FunctionalWorldModule<ComposedSurfaceField>(WorldLayerIds.Surface,ComposedSurfaceField.Version,c=>
                {
                    Surface=new ComposedSurfaceField(configuration,c.Input<BaseElevationField>(WorldLayerIds.Elevation),c.Input<MountainClusterField>(WorldLayerIds.Mountains),c.Input<CoastalLandformField>(WorldLayerIds.Coasts));
                    CalibrationMilliseconds=timer.Elapsed.TotalMilliseconds;GenerationPreview.Publish(preview,this,"Geological elevation");return Surface;
                },WorldLayerIds.Elevation,WorldLayerIds.Mountains,WorldLayerIds.Coasts),
                new FunctionalWorldModule<ClimateMap>(WorldLayerIds.Climate,ClimateMap.Version,c=>
                {
                    c.Input<ComposedSurfaceField>(WorldLayerIds.Surface);timer.Restart();progress?.Invoke("Latitude, ocean winds and rain shadows");
                    Climate=new ClimateMap(configuration,c.Input<ComposedSurfaceField>(WorldLayerIds.Surface),c.ClimateSettings);ClimateMilliseconds=timer.Elapsed.TotalMilliseconds;GenerationPreview.Publish(preview,this,"Climate regions");return Climate;
                },WorldLayerIds.Surface),
                new FunctionalWorldModule<HydrologyMap>(WorldLayerIds.Hydrology,HydrologyMap.Version,c=>
                {
                    c.Input<ClimateMap>(WorldLayerIds.Climate);c.Input<ComposedSurfaceField>(drainageSurface);timer.Restart();
                    progress?.Invoke("Drainage basins, rivers and lake ownership");Hydrology=new HydrologyMap(configuration,c.Input<ComposedSurfaceField>(drainageSurface),c.Input<ClimateMap>(WorldLayerIds.Climate),token,c.Checkpoint,c.ModuleSeed);
                    var waterSamples=new System.Collections.Generic.Dictionary<(double,double),float>();
                    progress?.Invoke("Refined lake shores and river ownership");InlandSurfaces=new InlandWaterLayout(Hydrology,token,false,waterSamples,c.Checkpoint);
                    var drainageStates=new System.Collections.Generic.HashSet<ulong>();int drainagePass=0;
                    while(true)
                    {
                        c.Checkpoint("Settling drainage pass "+drainagePass);
                        if(!drainageStates.Add(Hydrology.StateIdentity()))throw new InvalidOperationException("Drainage refinement repeated an unresolved state at pass "+drainagePass+".");
                        int changed=Hydrology.BreachRejectedBasins(InlandSurfaces);if(changed==0)break;
                        string detail="Settling drainage pass "+(++drainagePass)+": "+changed+" basins";progress?.Invoke(detail);c.Checkpoint(detail);
                        InlandSurfaces=new InlandWaterLayout(Hydrology,token,false,waterSamples,c.Checkpoint);
                    }
                    Hydrology.BindSurface(InlandSurfaces);progress?.Invoke("Triangulating settled water surfaces");c.Checkpoint("Triangulating settled water surfaces");
                    InlandSurfaces.PrepareGeometry(token);InlandSurfaces.ReleasePreparationCache();HydrologyMilliseconds=timer.Elapsed.TotalMilliseconds;GenerationPreview.Publish(preview,this,"Connected drainage and water",true);return Hydrology;
                },WorldLayerIds.Climate,drainageSurface),
                new FunctionalWorldModule<MoistureField>(WorldLayerIds.Moisture,MoistureField.Version,c=>
                    Moisture=new MoistureField(c.Input<ClimateMap>(WorldLayerIds.Climate),c.Input<HydrologyMap>(WorldLayerIds.Hydrology)),WorldLayerIds.Climate,WorldLayerIds.Hydrology),
                new FunctionalWorldModule<BiomeField>(WorldLayerIds.Biomes,BiomeField.Version,c=>
                {
                    Biomes=new BiomeField(c.Input<ClimateMap>(WorldLayerIds.Climate),c.Input<MoistureField>(WorldLayerIds.Moisture),c.Input<GeologyMap>(WorldLayerIds.Geology),SeaLevel,configuration,c.Input<ComposedSurfaceField>(drainageSurface).Regional);
                    progress?.Invoke("Biome regions and aquatic habitats");for(int z=0;z<65;z++){c.Checkpoint("Sampling terrain habitats",z,65);for(int x=0;x<65;x++)Sample(new GeoPoint((x/64.0-.5)*configuration.WidthMeters,(z/64.0-.5)*configuration.DepthMeters));}
                    GenerationPreview.Publish(preview,this,"Biome regions",true);return Biomes;
                },WorldLayerIds.Climate,WorldLayerIds.Moisture,WorldLayerIds.Geology,drainageSurface),
                new FunctionalWorldModule<CaveNetwork>(WorldLayerIds.Caves,CaveNetwork.Version,c=>
                {
                    // The final preview also requires the completed biome product.
                    c.Input<BiomeField>(WorldLayerIds.Biomes);
                    Caves=new CaveNetwork(configuration,c.Input<HydrologyMap>(WorldLayerIds.Hydrology),c.Input<GeologyMap>(WorldLayerIds.Geology),c.Input<BaseElevationField>(WorldLayerIds.Elevation),c.Input<ClimateMap>(WorldLayerIds.Climate),c.ModuleSeed);
                    GenerationPreview.Publish(preview,this,"Settled terrain and drainage",true);return Caves;
                },WorldLayerIds.Hydrology,WorldLayerIds.Geology,WorldLayerIds.Elevation,WorldLayerIds.Climate,WorldLayerIds.Biomes),
                new FunctionalWorldModule<WorldMaterialField>(WorldLayerIds.Materials,WorldMaterialField.Version,c=>
                {
                    progress?.Invoke("Surface materials and geological soils");Materials=new WorldMaterialField(configuration,c.Input<HydrologyMap>(WorldLayerIds.Hydrology),c.Input<GeologyMap>(WorldLayerIds.Geology),c.Input<ContinentalField>(WorldLayerIds.Continents),c.Input<BiomeField>(WorldLayerIds.Biomes),c.ModuleSeed);GenerationPreview.Publish(preview,this,"Surface materials",true);return Materials;
                },WorldLayerIds.Hydrology,WorldLayerIds.Geology,WorldLayerIds.Continents,WorldLayerIds.Biomes),
                new FunctionalWorldModule<EcosystemField>(WorldLayerIds.Ecosystem,EcosystemField.Version,c=>
                {
                    progress?.Invoke("Vegetation habitats and forest distributions");Ecosystem=new EcosystemField(configuration,c.Input<HydrologyMap>(WorldLayerIds.Hydrology),c.Input<HydrologyMap>(WorldLayerIds.Hydrology),c.Input<BiomeField>(WorldLayerIds.Biomes),c.Input<WorldMaterialField>(WorldLayerIds.Materials),c.ModuleSeed);GenerationPreview.Publish(preview,this,"Vegetation habitats",true);return Ecosystem;
                },WorldLayerIds.Hydrology,WorldLayerIds.Biomes,WorldLayerIds.Materials),
                new FunctionalWorldModule<CivilizationSiteField>(WorldLayerIds.Civilizations,CivilizationSiteField.Version,c=>
                {
                    progress?.Invoke("Settlement sites and geographic resources");CivilizationSites=new CivilizationSiteField(configuration,c.Input<HydrologyMap>(WorldLayerIds.Hydrology),c.Input<HydrologyMap>(WorldLayerIds.Hydrology),c.Input<HydrologyMap>(WorldLayerIds.Hydrology).CanonicalSurfaces,c.Input<CaveNetwork>(WorldLayerIds.Caves),c.Input<BiomeField>(WorldLayerIds.Biomes),c.Input<EcosystemField>(WorldLayerIds.Ecosystem),c.Input<WorldMaterialField>(WorldLayerIds.Materials),c.ModuleSeed);
                    CivilizationSites.Prepare(token,c.Checkpoint);GenerationPreview.Publish(preview,this,"Habitats and settlement geography",true);return CivilizationSites;
                },WorldLayerIds.Hydrology,WorldLayerIds.Caves,WorldLayerIds.Biomes,WorldLayerIds.Ecosystem,WorldLayerIds.Materials)
            };
            // Downstream maps retain the facade during migration, with declared
            // inputs and publication only after their mutable preparation ends.
            var modules=new System.Collections.Generic.List<IWorldGenerationModule>(terrainModules);
            if(regionalLandforms)modules.Add(new FunctionalWorldModule<ComposedSurfaceField>(WorldLayerIds.RegionalLandforms,RegionalLandformField.Version,c=>
            {
                progress?.Invoke("Wind-shaped dunes and arid salt basins");
                var original=c.Input<ComposedSurfaceField>(WorldLayerIds.Surface);
                var regional=new RegionalLandformField(configuration,original,c.Input<ClimateMap>(WorldLayerIds.Climate),c.Input<ContinentalField>(WorldLayerIds.Continents),c.Input<GeologyMap>(WorldLayerIds.Geology),c.ModuleSeed,token);
                Surface=new ComposedSurfaceField(configuration,original.Base,original.Mountains,original.Coast,regional);
                GenerationPreview.Publish(preview,this,"Regional desert landforms");return Surface;
            },WorldLayerIds.Surface,WorldLayerIds.Climate,WorldLayerIds.Continents,WorldLayerIds.Geology));
            Layers=new WorldGenerationPipeline(modules).Generate(configuration,climate,Diagnostics,token);
        }
        private double Lattice(int x, int y, uint channel)
        {
            unchecked
            {
                uint h = seed ^ (uint)x * 374761393u ^ (uint)y * 668265263u ^ channel * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
                return h / (double)uint.MaxValue * 2 - 1;
            }
        }
        public double FieldNoise(double east,double north,double wavelength,uint channel)
            => GradientField.Sample(east/wavelength,north/wavelength,seed,channel);
        public double SiteRandom(int x,int z,uint channel)=>(Lattice(x,z,channel)+1)*.5;

        public float MeasureLandFraction(int resolution=129)
        {
            if(resolution<8 || resolution>1024) throw new ArgumentOutOfRangeException(nameof(resolution));
            int land=0;
            for(int z=0;z<resolution;z++) for(int x=0;x<resolution;x++)
                if(ElevationAt((x+0.5)/resolution*Configuration.WidthMeters-Configuration.WidthMeters/2,
                    (z+0.5)/resolution*Configuration.DepthMeters-Configuration.DepthMeters/2)>=SeaLevel) land++;
            return (float)land/(resolution*resolution);
        }

        public float ElevationAt(double east, double north)
        {
            float height = BaseElevationAt(east,north);
            return Hydrology == null ? height : Hydrology.Carve(east,north,height);
        }

        public float BaseElevationAt(double east, double north)
            => ComputeElevation(east,north,true);
        public float SceneryElevationAt(double east,double north)
            => ComputeElevation(east,north,false);
        private float ComputeElevation(double east,double north,bool bounded)
        {
            if(!WorldConfiguration.Finite(east)||!WorldConfiguration.Finite(north))throw new ArgumentException("Coordinates must be finite.");
            if(!Configuration.Contains(new GeoPoint(east,north)))
            {
                double e=Math.Max(-Configuration.WidthMeters/2,Math.Min(Configuration.WidthMeters/2,east)),n=Math.Max(-Configuration.DepthMeters/2,Math.Min(Configuration.DepthMeters/2,north));
                double distance=Math.Sqrt((east-e)*(east-e)+(north-n)*(north-n));return (float)Math.Min(SeaLevel-50-distance*.04,Surface.ElevationAt(e,n)-distance*.025);
            }
            return Surface.ElevationAt(east,north);
        }        public TerrainRegion RegionAt(GeoPoint point)
        {
            float h = ElevationAt(point.X, point.Y) - SeaLevel;
            if (h < 0) return TerrainRegion.Ocean;
            if (h < 8) return TerrainRegion.Coast;
            if (Climate.Sample(point.X,point.Y,h+SeaLevel).TemperatureCelsius < -3) return TerrainRegion.Snow;
            if (h > 170) return TerrainRegion.Mountain;
            if (h > 110) return TerrainRegion.Plateau;
            return h > 45 ? TerrainRegion.Hills : TerrainRegion.Plain;
        }
        public GeographicSample Sample(GeoPoint point)
        {
            float elevation = ElevationAt(point.X, point.Y);
            return Biomes!=null?Biomes.Sample(point.X,point.Y,elevation):BiomeField.Provisional(Climate,Geology,point.X,point.Y,elevation,SeaLevel);
        }
        public AquaticHabitat AquaticAt(double x,double z)
        {
            float h=ElevationAt(x,z),depth=SeaLevel-h,temperature=Climate.TemperatureAt(x,z,h);
            if(depth<=0)return Hydrology?.IsLakeWater(x,z,h)==true?AquaticHabitat.FreshwaterPlants:AquaticHabitat.None;
            if(depth>250)return AquaticHabitat.DeepOcean;bool rock=Geology.Sample(x,z).Uplift>.2;
            if(depth<20&&(Hydrology?.MoistureInfluence(x,z)??0)>.4)return AquaticHabitat.Estuary;
            if(depth<45&&temperature>22&&!rock)return AquaticHabitat.CoralReef;
            if(depth>4&&depth<50&&temperature>3&&temperature<19&&rock)return AquaticHabitat.Kelp;
            if(depth<20&&temperature>12)return AquaticHabitat.Seagrass;return rock?AquaticHabitat.RockyCoast:AquaticHabitat.SandySeabed;
        }
    }
}
