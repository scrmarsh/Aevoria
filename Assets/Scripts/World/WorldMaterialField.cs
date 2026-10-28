using System;
namespace Aevoria.World
{
    public readonly struct SurfaceMaterialSample
    {
        public readonly float SoilDepth,Regolith,RockHardness,Sand;
        public SurfaceMaterialSample(float soil,float regolith,float hardness,float sand){SoilDepth=soil;Regolith=regolith;RockHardness=hardness;Sand=sand;}
    }
    public sealed class WorldMaterialField
    {
        public const int Version=1;
        private readonly WorldConfiguration configuration;private readonly IWorldHeightField terrain;private readonly GeologyMap geology;private readonly ContinentalField continents;private readonly BiomeField biomes;private readonly uint seed;
        public WorldMaterialField(WorldConfiguration configuration,IWorldHeightField terrain,GeologyMap geology,ContinentalField continents,BiomeField biomes,ulong moduleSeed)
        {this.configuration=configuration;this.terrain=terrain;this.geology=geology;this.continents=continents;this.biomes=biomes;seed=(uint)(moduleSeed^(moduleSeed>>32));}
        public float SoilDepthAt(double x,double z)
            =>(float)(.25+geology.Soil(x,z)*2.75+GradientField.Sample(x/700,z/700,seed,911)*.12);
        public SurfaceMaterialSample Sample(double x,double z)
        {
            float h=terrain.ElevationAt(x,z);return Sample(x,z,biomes.Sample(x,z,h));
        }
        public SurfaceMaterialSample Sample(double x,double z,GeographicSample b)
        {
            float h=b.ElevationMeters,regolith=geology.Soil(x,z);
            float sand=b.Biome==Biome.Desert||b.Biome==Biome.ColdDesert?1:continents.CoastDistanceAt(x,z)<100&&h<configuration.Terrain.SeaLevel+12?1:0;
            return new SurfaceMaterialSample(SoilDepthAt(x,z),regolith,.8f+(1-regolith)*1.4f,sand);
        }
        public ResourceKind GroundMaterialAt(double x,double y,double z,float normalY=1)
        {
            var material=Sample(x,z);double ground=GroundContactPatch.SurfaceHeight(terrain,configuration.CellSizeMeters,x,z);
            if(normalY<.45f||ground-y>material.SoilDepth)return ResourceKind.Stone;
            return material.Sand>.5f?ResourceKind.Sand:ResourceKind.Soil;
        }
    }
}
