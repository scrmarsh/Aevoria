using System;
using System.Collections.Generic;
using System.Threading;

namespace Aevoria.World
{
    // Optional Atlas revision1 product. Regional climate reads the unmodified
    // geological surface; drainage then reads this immutable physical surface.
    // Historical profiles/revision0 never construct or sample this product.
    public sealed class RegionalLandformField
    {
        public const int Version=1;
        private readonly struct Basin
        {
            public readonly double X,Z,Radius,Level;
            public Basin(double x,double z,double radius,double level){X=x;Z=z;Radius=radius;Level=level;}
        }
        private readonly List<Basin> basins=new List<Basin>();
        private readonly WorldConfiguration configuration;
        private readonly ClimateMap climate;
        private readonly ContinentalField continents;
        private readonly GeologyMap geology;
        private readonly uint seed;
        public int SaltBasinCount=>basins.Count;
        public RegionalLandformField(WorldConfiguration config,IWorldHeightField original,ClimateMap climate,ContinentalField continents,GeologyMap geology,ulong moduleSeed,CancellationToken token=default)
        {
            configuration=config;this.climate=climate;this.continents=continents;this.geology=geology;seed=(uint)(moduleSeed^(moduleSeed>>32));
            if(config.Terrain.Profile!=GeographyProfile.Atlas||config.Terrain.AtlasRevision!=1)throw new ArgumentException("Regional landforms require their explicit Atlas terrain revision.");
            var random=new WorldGenerationRandom(moduleSeed);int desired=(int)Math.Min(12,2+config.WidthMeters/256000);
            for(int attempt=0;attempt<192&&basins.Count<desired;attempt++)
            {
                token.ThrowIfCancellationRequested();double x=(random.Next01()-.5)*config.WidthMeters*.8,z=(-.42+random.Next01()*.29)*config.DepthMeters;
                double radius=Math.Max(1200,Math.Min(16000,2000*Math.Sqrt(config.WidthMeters/128000)))*(.7+random.Next01()*.6);
                float h=original.ElevationAt(x,z);if(h<config.Terrain.SeaLevel+30||h>450||continents.CoastDistanceAt(x,z)<radius*1.3||climate.RainfallAt(x,z)>.24||climate.TemperatureAt(x,z,h)<12)continue;
                double dx=(original.ElevationAt(x+500,z)-original.ElevationAt(x-500,z))/1000,dz=(original.ElevationAt(x,z+500)-original.ElevationAt(x,z-500))/1000;
                if(dx*dx+dz*dz>.0016)continue;
                bool separate=true;foreach(var basin in basins)if(Math.Pow(x-basin.X,2)+Math.Pow(z-basin.Z,2)<Math.Pow(radius+basin.Radius,2)){separate=false;break;}
                if(separate)basins.Add(new Basin(x,z,radius,h-7-random.Next01()*7));
            }
        }
        private double Noise(double x,double z,double scale,uint channel)=>GradientField.Sample(x/scale,z/scale,seed,channel);
        private double BasinWeight(Basin basin,double east,double north)
        {
            double x=east-basin.X,z=north-basin.Z;if(Math.Abs(x)>basin.Radius*1.3||Math.Abs(z)>basin.Radius*1.3)return 0;
            double irregular=1+Noise(east,north,basin.Radius*.7,1901)*.18;
            double radius=Math.Sqrt(x*x+z*z)/basin.Radius/irregular;
            return 1-ContinentalField.Smooth(.53,1,radius);
        }
        public float SaltBasinAt(double east,double north)
        {double weight=0;foreach(var basin in basins)weight=Math.Max(weight,BasinWeight(basin,east,north));return (float)weight;}
        public float DuneOffsetAt(double east,double north,float height)
        {
            float rain=climate.RainfallAt(east,north),temperature=climate.TemperatureAt(east,north,height);
            double dry=(1-ContinentalField.Smooth(.18,.30,rain))*ContinentalField.Smooth(12,22,temperature);
            if(dry<.001||height<configuration.Terrain.SeaLevel+12||height>1400)return 0;
            double support=ContinentalField.Smooth(12,80,height-configuration.Terrain.SeaLevel)*(1-ContinentalField.Smooth(700,1400,height));
            double sand=ContinentalField.Smooth(.28,.65,geology.Soil(east,north));
            double fetch=ContinentalField.Smooth(100,1600,continents.CoastDistanceAt(east,north));
            double province=ContinentalField.Smooth(-.25,.35,Noise(east,north,6500,1910));
            double mask=dry*support*sand*fetch*province*(1-SaltBasinAt(east,north));if(mask<.001)return 0;
            double warp=Noise(east,north,1900,1911)*210+Noise(east,north,670,1912)*55;
            double phase=(east*.8+north*.6+warp)/300+Noise(east,north,4200,1913)*.75,u=phase-Math.Floor(phase);
            // Long windward slopes and shorter slip faces, with irregular crest
            // spacing and amplitude. This is physical relief before drainage.
            double crest=u<.76?ContinentalField.Smooth(0,.76,u):1-ContinentalField.Smooth(.76,1,u);
            double amplitude=24+Noise(east,north,2700,1914)*16;
            return (float)((crest-.35)*amplitude*mask);
        }
        public float Shape(double east,double north,float height)
        {
            double ground=height;
            foreach(var basin in basins)
            {
                double weight=BasinWeight(basin,east,north);
                weight*=1-ContinentalField.Smooth(80,200,Math.Abs(height-basin.Level));
                double floor=basin.Level+Noise(east,north,350,1920)*.18;
                ground+=(floor-ground)*weight;
            }
            return (float)(ground+DuneOffsetAt(east,north,(float)ground));
        }
    }
}
