using System;
namespace Aevoria.World
{
    [Serializable] public sealed class AtmosphericSnapshot
    {public int Version=1,Resolution;public string Seed;public long Tick,EpochTick;public float[] Wetness;public AtmosphericSnapshot Copy()=>new AtmosphericSnapshot{Version=Version,Resolution=Resolution,Seed=Seed,Tick=Tick,EpochTick=EpochTick,Wetness=(float[])Wetness.Clone()};}
    public readonly struct RegionalWeather
    {
        public readonly float Cloud,Precipitation,Temperature,WindX,WindZ,Pressure;public readonly WeatherKind Kind;
        public RegionalWeather(float cloud,float precipitation,float temperature,float wx,float wz,float pressure)
        {Cloud=cloud;Precipitation=precipitation;Temperature=temperature;WindX=wx;WindZ=wz;Pressure=pressure;Kind=precipitation>.02?(temperature<0?WeatherKind.Snow:precipitation>.7?WeatherKind.Storm:WeatherKind.Rain):cloud>.25?WeatherKind.Cloud:WeatherKind.Clear;}
    }
    // Fronts are deterministic functions of geography and world time. Only the
    // integrated surface wetness needs a mutable, versioned save field.
    public sealed class RegionalAtmosphere
    {
        public const int StepTicks=4;private readonly ProceduralGeography geo;private readonly EcologyModel ecology;private readonly AtmosphericSnapshot state;private RegionalWeather[] cache;private long cacheTick=-1;
        public RegionalAtmosphere(ProceduralGeography geo,EcologyModel ecology,long tick,AtmosphericSnapshot saved=null)
        {this.geo=geo;this.ecology=ecology;if(saved==null)state=new AtmosphericSnapshot{Seed=geo.Configuration.Seed,Resolution=ecology.Resolution,Tick=tick,EpochTick=tick,Wetness=new float[ecology.Count]};else{Validate(saved,geo.Configuration.Seed,ecology.Resolution,tick);state=saved.Copy();}}
        public static RegionalWeather Forecast(ProceduralGeography geo,long tick,GeoPoint p)
        {
            var climate=geo.Climate;float latitude=climate.LatitudeAt(p.Y),moisture=climate.RainfallAt(p.X,p.Y),continental=climate.Continentality(p.X,p.Y);double day=tick/4.0;
            float wx=Math.Abs(latitude)>=30&&Math.Abs(latitude)<60?1:-1,wz=(float)Math.Sin(latitude*Math.PI/90)*.24f;double wavelength=Math.Min(220000,Math.Max(24000,geo.Configuration.WidthMeters*.12));
            double x=p.X-wx*day*800,z=p.Y-wz*day*800;
            float front=(float)(.5+.5*geo.FieldNoise(x,z,wavelength,1601)),detail=(float)geo.FieldNoise(x+day*170,z-day*90,wavelength*.35,1602);
            float cloud=Clamp((front-.24f)*1.4f+moisture*.35f+detail*.12f);float rain=Clamp((cloud-.64f)*3.2f)*Math.Min(1,moisture*1.5f);
            float height=geo.BaseElevationAt(p.X,p.Y),temperature=climate.TemperatureAt(p.X,p.Y,height);
            temperature+=(float)Math.Sin(day/360*Math.PI*2)*(latitude<0?-1:1)*Math.Min(24,Math.Abs(latitude)*.28f)*(.5f+continental*.5f);
            return new RegionalWeather(cloud,rain,temperature,wx*(3+rain*8),wz*(3+rain*8),1013+(front-.5f)*24);
        }
        public RegionalWeather AtRegion(int region,long tick)=>cacheTick==tick?cache[region]:Forecast(geo,tick,ecology.EnvironmentAt(region).Center);
        public float Wetness(int region)=>state.Wetness[region];public long Tick=>state.Tick;
        public int AdvanceTo(long tick,int maximumSteps=8)
        {
            if(tick<state.Tick||maximumSteps<=0)throw new ArgumentOutOfRangeException(nameof(tick));int steps=0;
            while(state.Tick<=tick-StepTicks&&steps<maximumSteps){state.Tick+=StepTicks;steps++;for(int i=0;i<state.Wetness.Length;i++){var e=ecology.EnvironmentAt(i);var weather=Forecast(geo,state.Tick,e.Center);if(cache==null)cache=new RegionalWeather[state.Wetness.Length];cache[i]=weather;float supply=weather.Temperature>0?weather.Precipitation*.18f:0,evaporation=(.006f+Math.Max(0,weather.Temperature)*.0009f)*(1-weather.Cloud*.6f)*(1+(Math.Abs(weather.WindX)+Math.Abs(weather.WindZ))*.025f)*(float)(1-e.Moisture*.6);state.Wetness[i]=Clamp(state.Wetness[i]+supply-evaporation);}cacheTick=state.Tick;}
            return steps;
        }
        public AtmosphericSnapshot Capture()=>state.Copy();
        public static void Validate(AtmosphericSnapshot s,string seed,int n,long tick)
        {if(s==null||s.Version!=1||s.Seed!=seed||s.Resolution!=n||s.Wetness==null||s.Wetness.Length!=n*n||s.EpochTick<0||s.Tick<s.EpochTick||s.Tick>tick||tick-s.Tick>=StepTicks||(s.Tick-s.EpochTick)%StepTicks!=0)throw new ArgumentException("Invalid atmospheric timeline");foreach(float v in s.Wetness)if(float.IsNaN(v)||float.IsInfinity(v)||v<0||v>1)throw new ArgumentException("Invalid surface wetness");}
        private static float Clamp(float value)=>Math.Max(0,Math.Min(1,value));
    }
}
