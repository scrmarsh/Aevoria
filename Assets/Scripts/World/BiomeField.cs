using System;

namespace Aevoria.World
{
    public sealed class MoistureField
    {
        public const int Version=1;
        private readonly ClimateMap climate;private readonly HydrologyMap hydrology;
        public MoistureField(ClimateMap climate,HydrologyMap hydrology){this.climate=climate;this.hydrology=hydrology;}
        public float WaterAt(double east,double north)=>hydrology.MoistureInfluence(east,north);
        internal HydrologyMap Hydrology=>hydrology;
        public float At(double east,double north)=>Math.Max(0,Math.Min(1,climate.RainfallAt(east,north)+WaterAt(east,north)*.06f));
    }
    public sealed class BiomeField
    {
        public const int Version=2;
        private readonly ClimateMap climate;private readonly MoistureField moisture;private readonly GeologyMap geology;private readonly float sea;
        private readonly WorldConfiguration configuration;
        private readonly RegionalLandformField regional;
        public BiomeField(ClimateMap climate,MoistureField moisture,GeologyMap geology,float sea,WorldConfiguration configuration=null,RegionalLandformField regional=null){this.climate=climate;this.moisture=moisture;this.geology=geology;this.sea=sea;this.configuration=configuration;this.regional=regional;}
        public GeographicSample Sample(double east,double north,float elevation)
        {
            float h=elevation-sea,m=moisture.At(east,north),temperature=climate.TemperatureAt(east,north,elevation),water=moisture.WaterAt(east,north);
            float fertility=h<0?0:Math.Max(0,Math.Min(1,m*(1-Math.Abs(temperature-18)/50)*(1-Math.Max(0,h-500)/9000)+geology.Soil(east,north)*.12f));
            var biome=Classify(h,temperature,m,water,climate.Continentality(east,north));
            if(configuration?.Terrain.Profile==GeographyProfile.Atlas)
                biome=ClassifyAtlas(east,north,h,temperature,m,water,biome);
            return new GeographicSample(elevation,m,temperature,fertility,biome);
        }
        private Biome ClassifyAtlas(double east,double north,float h,float temperature,float m,float water,Biome fallback)
        {
            if(h<0)return Biome.Ocean;
            float soil=geology.Soil(east,north);
            if(h<12)
            {
                var terrain=moisture.Hydrology;
                double dx=(terrain.ElevationAt(east+16,north)-terrain.ElevationAt(east-16,north))/32;
                double dz=(terrain.ElevationAt(east,north+16)-terrain.ElevationAt(east,north-16))/32;
                return dx*dx+dz*dz>.0144?Biome.RockyCoast:Biome.SandyCoast;
            }
            if(fallback==Biome.Snow||fallback==Biome.Tundra)return fallback;
            if(fallback==Biome.ColdDesert&&north>0)return Biome.Tundra;
            if(fallback==Biome.Alpine)return soil<.45f?Biome.AlpineScree:Biome.Alpine;
            // Local floodplain habitats override the regional dry climate.
            if(water>.30f&&temperature>4)return m<.32f?Biome.Oasis:h<160&&m>.65f?Biome.Wetland:Biome.RiparianForest;
            if(regional!=null&&m<.28f&&temperature>10&&regional.SaltBasinAt(east,north)>.55f)return Biome.SaltFlat;
            if(m<.22f&&temperature>12)
            {
                int node=moisture.Hydrology.Nearest(east,north);
                var outcome=moisture.Hydrology.OutcomeAt(node);
                bool dryBasin=h<220&&(outcome==HydrologyMap.BasinOutcome.ClosedWaterBudget||outcome==HydrologyMap.BasinOutcome.SmallFootprint);
                if(dryBasin&&soil>.2f)return Biome.SaltFlat;
                return soil<.48f?Biome.RockyDesert:Biome.Desert;
            }
            if(m<.32f)return Biome.Steppe;
            if(temperature>12&&temperature<26&&m<.53f)
                return m>.40f?Biome.MediterraneanWoodland:Biome.MediterraneanScrub;
            return fallback;
        }
        internal static GeographicSample Provisional(ClimateMap climate,GeologyMap geology,double east,double north,float elevation,float sea)
        {
            var c=climate.Sample(east,north,elevation);float h=elevation-sea;
            float fertility=h<0?0:Math.Max(0,Math.Min(1,c.Moisture*(1-Math.Abs(c.TemperatureCelsius-18)/50)*(1-Math.Max(0,h-500)/9000)+geology.Soil(east,north)*.12f));
            return new GeographicSample(elevation,c.Moisture,c.TemperatureCelsius,fertility,Classify(h,c.TemperatureCelsius,c.Moisture,0,c.Continentality));
        }
        internal static Biome Classify(float h,float temp,float m,float water,float continental)
            =>h<0?Biome.Ocean:h<5?Biome.Coast:temp< -13?Biome.Snow:h>1800&&temp<6?Biome.Alpine:temp< -6?(m<.2?Biome.ColdDesert:Biome.Tundra):temp<8&&m>.35?Biome.BorealForest:temp<0?(m<.2?Biome.ColdDesert:Biome.Tundra):
                m<.22?(temp>12?Biome.Desert:Biome.ColdDesert):temp>24&&m>.7?Biome.TropicalForest:temp>22&&m>.45?Biome.TropicalSeasonalForest:temp>20&&m<.5?Biome.Savanna:
                m>.8&&water>.2&&h<200?Biome.Wetland:m<.36?(temp>12&&temp<24&&continental<.65?Biome.Mediterranean:Biome.Shrubland):m>.58?(temp<14?Biome.TemperateConiferousForest:Biome.Forest):Biome.Grassland;
    }
}
