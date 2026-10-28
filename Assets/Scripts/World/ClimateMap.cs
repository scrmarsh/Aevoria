using System;

namespace Aevoria.World
{
    public readonly struct ClimateSample
    {
        public readonly float TemperatureCelsius,Moisture,Continentality;
        public ClimateSample(float temperature,float moisture,float continentality){TemperatureCelsius=temperature;Moisture=moisture;Continentality=continentality;}
    }
    public sealed class ClimateSettings
    {
        public float TemperatureOffset { get; }
        public float Rainfall { get; }
        public float Latitude { get; }
        public ClimateSettings(float temperatureOffset=0,float rainfall=1,float latitude=0)
        {
            if(!WorldConfiguration.Finite(temperatureOffset)||temperatureOffset< -25||temperatureOffset>25 ||
                !WorldConfiguration.Finite(rainfall)||rainfall<0.2||rainfall>2 || !WorldConfiguration.Finite(latitude)||latitude< -75||latitude>75)
                throw new ArgumentException("Invalid climate settings.");
            TemperatureOffset=temperatureOffset;Rainfall=rainfall;Latitude=latitude;
        }
    }
    public sealed class ClimateMap
    {
        public const int Version=6,Resolution=129;
        public ClimateSettings Settings {get;}private const int N=Resolution;
        private readonly float[] moisture=new float[N*N],oceanDistance=new float[N*N];private readonly WorldConfiguration configuration;private readonly float sea;
        private readonly uint regionalSeed;
        public ClimateMap(WorldConfiguration config,IWorldHeightField surface,ClimateSettings settings)
        {
            configuration=config??throw new ArgumentNullException(nameof(config));if(surface==null)throw new ArgumentNullException(nameof(surface));sea=config.Terrain.SeaLevel;Settings=settings??throw new ArgumentNullException(nameof(settings));var c=config;var heights=new float[N*N];var queue=new System.Collections.Generic.Queue<int>();
            for(int z=0;z<N;z++)for(int x=0;x<N;x++){int i=z*N+x;heights[i]=surface.ElevationAt((x/(double)(N-1)-.5)*c.WidthMeters,(z/(double)(N-1)-.5)*c.DepthMeters)-sea;oceanDistance[i]=float.MaxValue;if(heights[i]<0){oceanDistance[i]=0;queue.Enqueue(i);}}
            float step=(float)(c.WidthMeters/(N-1));while(queue.Count>0){int i=queue.Dequeue(),x=i%N,z=i/N;for(int k=0;k<4;k++){int j=k==0?(x>0?i-1:-1):k==1?(x<N-1?i+1:-1):k==2?(z>0?i-N:-1):(z<N-1?i+N:-1);if(j>=0&&oceanDistance[j]>oceanDistance[i]+step){oceanDistance[j]=oceanDistance[i]+step;queue.Enqueue(j);}}}
            regionalSeed=(uint)WorldGenerationSeeds.Derive(config.Seed,WorldLayerIds.Climate,Version);
            var forcing=new float[N];var eastWeight=new float[N];
            for(int z=0;z<N;z++)
            {
                double lat=LatitudeAt((z/(double)(N-1)-.5)*c.DepthMeters);
                // Subtropical subsidence dries the south; prevailing winds bring
                // moisture to temperate terrain before physical uplift and rainout.
                forcing[z]=(float)Math.Max(.035,.16+.52*Math.Exp(-Math.Pow((lat-49)/18,2))-.19*Math.Exp(-Math.Pow((lat-25)/11,2)));
                eastWeight[z]=Smooth(28,32,lat)*(1-Smooth(58,62,lat));
            }
            bool atlas=config.Terrain.Profile==GeographyProfile.Atlas;
            var eastRain=Advect(heights,forcing,N,step,true,atlas);var westRain=Advect(heights,forcing,N,step,false,atlas);
            for(int z=0;z<N;z++)for(int x=0;x<N;x++){int i=z*N+x;moisture[i]=Clamp((eastRain[i]*eastWeight[z]+westRain[i]*(1-eastWeight[z]))*settings.Rainfall);}
        }
        // Transport exchanges water between adjacent wind lanes before precipitation.
        // This is part of the climate model, not a blur of the final biome/material field.
        private static float[] Advect(float[] heights,float[] forcing,int n,float step,bool east,bool atlas=false)
        {
            var rain=new float[n*n];var previous=new double[n];var next=new double[n];
            for(int z=0;z<n;z++)previous[z]=.85;
            for(int k=0;k<n;k++)
            {
                int x=east?k:n-1-k,upwind=east?x-1:x+1;
                for(int z=0;z<n;z++)
                {
                    int below=Math.Max(0,z-1),above=Math.Min(n-1,z+1),i=z*n+x;
                    double water=.6*previous[z]+.2*previous[below]+.2*previous[above];
                    double prior=k==0?0:.6*Math.Max(0,heights[z*n+upwind])+.2*Math.Max(0,heights[below*n+upwind])+.2*Math.Max(0,heights[above*n+upwind]);
                    double h=Math.Max(0,heights[i]),lift=Math.Max(0,h-prior),shadow=Math.Max(0,prior-h);
                    if(heights[i]<0)water=.98;
                    rain[i]=(float)(forcing[z]*water+Math.Min(.4,lift*.0002)-Math.Min(.25,shadow*.00015));
                    // Background recycling recovers gradually with distance.
                    // A hard humidity floor erased rain shadows as soon as a
                    // dry continental air mass crossed the next grid cell.
                    next[z]=(.08+(water-.08)*Math.Exp(-step/250000))*Math.Exp(-lift*(atlas?.00016:.00045));
                }
                var swap=previous;previous=next;next=swap;
            }
            return rain;
        }
        private static float Smooth(double low,double high,double value){double t=Math.Max(0,Math.Min(1,(value-low)/(high-low)));return (float)(t*t*(3-2*t));}
        // Stop extrapolating the surface lapse rate above the troposphere.
        public static float AltitudeCooling(float elevation)
        {
            float h=Math.Max(0,elevation);return Math.Min(h,11000)*.0065f-Math.Max(0,Math.Min(h,32000)-20000)*.001f;
        }
        public float LatitudeAt(double north)
        {
            // Every supported map covers one northern hemisphere. World size
            // changes physical distances, not the direction or number of poles.
            double position=Math.Max(0,Math.Min(1,north/configuration.DepthMeters+.5));
            double extent=configuration.Terrain.AtlasRevision>0?47:55;
            return (float)Math.Max(0,Math.Min(88,20+extent*position+Settings.Latitude));
        }
        public float RainfallAt(double x,double z)
        {
            float rain=Read(moisture,x,z);
            if(configuration.Terrain.Profile!=GeographyProfile.Atlas)return rain;
            return Clamp(rain+(float)(GradientField.Sample(x/(configuration.WidthMeters*.12),z/(configuration.DepthMeters*.12),regionalSeed,991)*.07));
        }
        public float Continentality(double x,double z)=>Math.Min(1,Read(oceanDistance,x,z)/250000);
        public float TemperatureAt(double east,double north,float elevation)
        {
            float lat=LatitudeAt(north),continental=Continentality(east,north);
            float maritime=(2.4f*Smooth(42,65,lat)-1.4f*(1-Smooth(28,44,lat)))*(1-continental);
            // Insolation declines faster toward the pole. A linear latitude
            // lapse made the middle of the map boreal at modest elevations,
            // leaving almost no warm temperate woodland between desert and taiga.
            float regional=configuration.Terrain.Profile==GeographyProfile.Atlas?(float)GradientField.Sample(east/(configuration.WidthMeters*.16),north/(configuration.DepthMeters*.14),regionalSeed,992)*1.8f:0;
            float annual=33-lat*lat*.0077f+Settings.TemperatureOffset-AltitudeCooling(elevation-sea)+maritime;
            return configuration.Terrain.Profile==GeographyProfile.Atlas?annual+regional:annual;
        }
        public ClimateSample Sample(double east,double north,float height)
        {
            // This product never reads downstream drainage or biome products.
            return new ClimateSample(TemperatureAt(east,north,height),RainfallAt(east,north),Continentality(east,north));
        }
        private float Read(float[] field,double x,double z)
        {var c=configuration;double gx=Math.Max(0,Math.Min(N-1,(x/c.WidthMeters+.5)*(N-1))),gz=Math.Max(0,Math.Min(N-1,(z/c.DepthMeters+.5)*(N-1)));int ix=Math.Min(N-2,(int)gx),iz=Math.Min(N-2,(int)gz);float a=(float)(gx-ix),b=(float)(gz-iz);int i=iz*N+ix;return (field[i]*(1-a)+field[i+1]*a)*(1-b)+(field[i+N]*(1-a)+field[i+N+1]*a)*b;}
        private static float Clamp(float f)=>Math.Max(0,Math.Min(1,f));
    }
    public enum WeatherKind { Clear, Cloud, Rain, Storm, Fog, Snow }
    public readonly struct WorldWeather
    {
        public const int Version=2;public readonly WeatherKind Kind;public readonly float Temperature;
        public WorldWeather(ProceduralGeography geography,long tick,GeoPoint point)
        {var weather=RegionalAtmosphere.Forecast(geography,tick,point);Kind=weather.Kind;Temperature=weather.Temperature;}
    }
}
