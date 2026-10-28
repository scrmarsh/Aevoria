using System;
using System.Collections.Generic;
using System.Threading;

namespace Aevoria.World
{
    public readonly struct VegetationSite
    {
        public readonly double East,North;
        public readonly float Height,Scale,Yaw,WidthScale,SlopeEast,SlopeNorth;
        public readonly bool Conifer;
        public readonly int Variant;
        public VegetationSite(double east,double north,float height,float scale,float yaw,bool conifer,int variant=-1,float widthScale=1,float slopeEast=0,float slopeNorth=0)
        { East=east; North=north; Height=height; Scale=scale; Yaw=yaw; Conifer=conifer;Variant=variant<0?(conifer?0:3):variant;WidthScale=widthScale;SlopeEast=slopeEast;SlopeNorth=slopeNorth; }
    }

    // Stateless global cells make placement independent of streaming/LOD order.
    public sealed class EcosystemField
    {
        public const int Version=1;
        public ulong Seed{get;}
        private readonly WorldConfiguration configuration;private readonly IWorldHeightField surface;private readonly HydrologyMap hydrology;private readonly BiomeField biomes;private readonly WorldMaterialField materials;private readonly uint noiseSeed;
        public EcosystemField(WorldConfiguration configuration,IWorldHeightField surface,HydrologyMap hydrology,BiomeField biomes,WorldMaterialField materials,ulong seed)
        {this.configuration=configuration;this.surface=surface;this.hydrology=hydrology;this.biomes=biomes;this.materials=materials;Seed=seed;noiseSeed=(uint)(seed^(seed>>32));}
        private GeographicSample Sample(GeoPoint p)=>biomes.Sample(p.X,p.Y,surface.ElevationAt(p.X,p.Y));
        private double FieldNoise(double x,double z,double scale,uint channel)=>GradientField.Sample(x/scale,z/scale,noiseSeed,channel);
        public double ForestPotential(double x,double z)
        {
            if(configuration.Terrain.Profile==GeographyProfile.Atlas)
            {
                var habitat=Sample(new GeoPoint(x,z));var biology=BiomeHabitat.For(habitat.Biome);
                if(biology.TreeDensity==0||hydrology.IsLakeWater(x,z,habitat.ElevationMeters)||hydrology.IsRiverWater(x,z,habitat.ElevationMeters))return 0;
                double stand=FieldNoise(x,z,1600,310)*.65+FieldNoise(x,z,370,311)*.35;
                return biology.TreeDensity*(.04+.9*Smooth(-.15,.30,stand));
            }
            var s=Sample(new GeoPoint(x,z));if(s.ElevationMeters<configuration.Terrain.SeaLevel+7||s.TemperatureCelsius< -6||s.Moisture<.26||s.Biome==Biome.Alpine||s.Biome==Biome.Snow||s.Biome==Biome.Tundra||s.Biome==Biome.Desert||s.Biome==Biome.ColdDesert||hydrology.IsLakeWater(x,z,s.ElevationMeters)||hydrology.IsRiverWater(x,z,s.ElevationMeters))return 0;
            double forest=FieldNoise(x,z,1600,310)*.65+FieldNoise(x,z,370,311)*.35;
            double density=.008+.14*Smooth(-.16,.10,forest)+.78*Smooth(.12,.32,forest);
            return density*Math.Min(1,Math.Max(0,(s.Moisture-.22)*2.4))*(s.Biome==Biome.Grassland?.09:s.Biome==Biome.Savanna?.18:s.Biome==Biome.Shrubland||s.Biome==Biome.Mediterranean?.22:1)*Math.Min(1,materials.SoilDepthAt(x,z)/.5);
        }
        public const int CellMeters=16;
        private double Unit(int x,int z,uint channel)=>WorldGenerationSeeds.Site(Seed,x*71+17,z*71-39,channel);
        public bool TryTree(int x,int z,out VegetationSite site,bool potential=false)
        {
            double east=(x+0.15+Unit(x,z,301)*0.7)*CellMeters;
            double north=(z+0.15+Unit(x,z,302)*0.7)*CellMeters;
            site=default;
            if(!configuration.Contains(new GeoPoint(east,north))) return false;
            float height=surface.ElevationAt(east,north);
            var climate=Sample(new GeoPoint(east,north));
            bool atlas=configuration.Terrain.Profile==GeographyProfile.Atlas;
            if(atlas?BiomeHabitat.For(climate.Biome).TreeDensity==0:height<configuration.Terrain.SeaLevel+7 || climate.TemperatureCelsius< -6 || climate.Moisture<0.26f || climate.Biome==Biome.Alpine||climate.Biome==Biome.Desert||climate.Biome==Biome.ColdDesert||climate.Biome==Biome.Snow||climate.Biome==Biome.Tundra) return false;
            if(hydrology.IsLakeWater(east,north,height)) return false;
            if(hydrology.IsRiverWater(east,north,height)) return false;
            double forest=FieldNoise(east,north,1600,310)*0.65+FieldNoise(east,north,370,311)*0.35;
            double density=ForestPotential(east,north);
            if(potential) density=Math.Min(1,density*1.15+.025*Smooth(-.02,.15,forest)); // reserve sites stay in coherent stands
            if(Unit(x,z,303)>density) return false;
            double dx=(surface.ElevationAt(east+4,north)-surface.ElevationAt(east-4,north))/8;
            double dz=(surface.ElevationAt(east,north+4)-surface.ElevationAt(east,north-4))/8;
            if(dx*dx+dz*dz>0.45) return false;
            bool conifer=climate.Biome==Biome.BorealForest||climate.Biome==Biome.TemperateConiferousForest||(climate.TemperatureCelsius<12&&climate.Moisture>.35f);
            double regional=FieldNoise(east,north,4200,318)*0.5+0.5;
            int variant=Math.Min(2,(int)((regional*0.65+Unit(x,z,319)*0.35)*3))+(conifer?0:3);
            if(atlas){var biology=BiomeHabitat.For(climate.Biome);variant=Unit(x,z,319)<.65?biology.TreeA:biology.TreeB;conifer=variant<3||variant==7||variant==8||variant==11;}
            site=new VegetationSite(east,north,height,(float)(0.8+Unit(x,z,304)*1.1),
                (float)(Unit(x,z,305)*360),conifer,variant,(float)(0.72+Unit(x,z,320)*0.58));
            return true;
        }
        private static double Smooth(double lo,double hi,double v) {double t=Math.Max(0,Math.Min(1,(v-lo)/(hi-lo)));return t*t*(3-2*t);}
        public VegetationChunk CreateChunk(ChunkKey key,CancellationToken token)
        {
            var trees=new List<VegetationSite>();var grass=new List<VegetationSite>();var rocks=new List<VegetationSite>();
            double side=configuration.ChunkSizeMeters;
            int startX=(int)Math.Floor(key.X*side/CellMeters),startZ=(int)Math.Floor(key.Y*side/CellMeters);
            int endX=(int)Math.Ceiling((key.X+1)*side/CellMeters),endZ=(int)Math.Ceiling((key.Y+1)*side/CellMeters);
            int stride=Math.Max(1,(int)Math.Ceiling((endX-startX)/32.0));
            for(int z=startZ;z<endZ;z+=stride)
            {
                token.ThrowIfCancellationRequested();
                for(int x=startX;x<endX;x+=stride)
                {
                    if(TryTree(x,z,out var site,true))
                    {
                        if(configuration.ChunkAt(new GeoPoint(site.East,site.North)).Equals(key)) trees.Add(site);
                    }
                    else if(((x+z)&3)==0)
                    {
                        double east=(x+0.5)*CellMeters,north=(z+0.5)*CellMeters;
                        if(!configuration.Contains(new GeoPoint(east,north))) continue;
                        float h=surface.ElevationAt(east,north);
                        var habitat=Sample(new GeoPoint(east,north));
                        if(configuration.Terrain.Profile==GeographyProfile.Atlas)
                        {
                            var biology=BiomeHabitat.For(habitat.Biome);
                            if(biology.CoverDensity>Unit(x,z,321)&&!hydrology.IsLakeWater(east,north,h)&&!hydrology.IsRiverWater(east,north,h)&&configuration.ChunkAt(new GeoPoint(east,north)).Equals(key))
                                grass.Add(new VegetationSite(east,north,h,(float)(.65+Unit(x,z,322)*.7),(x*53+z*17)%360,false,biology.Cover));
                            continue;
                        }
                        if(h>configuration.Terrain.SeaLevel+10 && habitat.TemperatureCelsius>0&&habitat.Moisture>.24&&habitat.Biome!=Biome.Desert&& configuration.ChunkAt(new GeoPoint(east,north)).Equals(key))
                            grass.Add(new VegetationSite(east,north,h,(float)(0.65+Unit(x,z,321)*1.1),(x*53+z*17)%360,false,(x^z)&1));
                        else if(h>configuration.Terrain.SeaLevel+140 && Unit(x,z,324)>0.985 && FieldNoise(east,north,500,327)>-0.1)
                        {
                            double dx=surface.ElevationAt(east+8,north)-surface.ElevationAt(east-8,north),dz=surface.ElevationAt(east,north+8)-surface.ElevationAt(east,north-8);
                            if(dx*dx+dz*dz>18 && configuration.ChunkAt(new GeoPoint(east,north)).Equals(key))
                                rocks.Add(new VegetationSite(east,north,h-1.4f,(float)(1.2+Unit(x,z,325)*2.4),(float)(Unit(x,z,326)*360),false,slopeEast:(float)(dx/16),slopeNorth:(float)(dz/16)));
                        }
                    }
                }
            }
            return new VegetationChunk(trees.ToArray(),grass.ToArray(),rocks.ToArray());
        }
    }
    public static class VegetationDistribution
    {
        public const int CellMeters=EcosystemField.CellMeters;
        public static bool TryTree(ProceduralGeography geo,int x,int z,out VegetationSite site,bool potential=false)=>geo.Ecosystem.TryTree(x,z,out site,potential);
        public static VegetationChunk CreateChunk(ProceduralGeography geo,ChunkKey key,CancellationToken token)=>geo.Ecosystem.CreateChunk(key,token);
    }
    public readonly struct VegetationChunk
    {
        public readonly VegetationSite[] Trees,Grass,Rocks;
        public VegetationChunk(VegetationSite[] trees,VegetationSite[] grass,VegetationSite[] rocks=null) {Trees=trees;Grass=grass;Rocks=rocks??Array.Empty<VegetationSite>();}
    }
}
