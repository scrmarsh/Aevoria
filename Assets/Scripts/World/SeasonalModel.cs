using System;
namespace Aevoria.World
{
    [Serializable] public sealed class SeasonalSnapshot
    {
        public int Version=2,Resolution;public string Seed;public long Tick,EpochTick;
        public float[] Snow,Ice;
        public SeasonalSnapshot Copy()=>new SeasonalSnapshot{Version=Version,Resolution=Resolution,Seed=Seed,Tick=Tick,EpochTick=EpochTick,Snow=(float[])Snow.Clone(),Ice=(float[])Ice.Clone()};
    }
    // Daily accumulation intervals, independent of visible regions.
    public sealed class SeasonalModel
    {
        public const int StepTicks=4;
        private readonly RegionalAtmosphere atmosphere;private readonly EcologyModel ecology;private readonly SeasonalSnapshot state;
        public long Tick=>state.Tick;public int Count=>state.Snow.Length;
        public float Snow(int region)=>state.Snow[region];public float Ice(int region)=>state.Ice[region];
        public SeasonalModel(EcologyModel ecology,string seed,long tick,SeasonalSnapshot saved=null,RegionalAtmosphere atmosphere=null)
        {
            this.ecology=ecology;this.atmosphere=atmosphere;
            if(saved!=null){Validate(saved,seed,ecology.Resolution,tick);state=saved.Copy();}
            else state=new SeasonalSnapshot{Seed=seed,Resolution=ecology.Resolution,Tick=tick,EpochTick=tick,Snow=new float[ecology.Count],Ice=new float[ecology.Count]};
        }
        public static void Validate(SeasonalSnapshot s,string seed,int n,long tick)
        {
            if(s==null||s.Version!=2)throw new NotSupportedException("Unsupported seasonal state.");
            if(s.Seed!=seed||s.Resolution!=n||s.Snow==null||s.Ice==null||s.Snow.Length!=n*n||s.Ice.Length!=n*n||s.EpochTick<0||s.Tick<s.EpochTick||s.Tick>tick||tick-s.Tick>=StepTicks||(s.Tick-s.EpochTick)%StepTicks!=0)throw new ArgumentException("Invalid snow/ice timeline.");
            for(int i=0;i<s.Snow.Length;i++)if(!Finite(s.Snow[i])||s.Snow[i]<0||s.Snow[i]>2||!Finite(s.Ice[i])||s.Ice[i]<0||s.Ice[i]>1)throw new ArgumentException("Invalid snow or ice quantity.");
        }
        private static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        public int AdvanceTo(long tick,int maximumSteps=8)
        {
            if(tick<state.Tick||maximumSteps<=0)throw new ArgumentOutOfRangeException(nameof(tick));int steps=0;
            while(state.Tick<=tick-StepTicks&&steps<maximumSteps)
            {
                state.Tick+=StepTicks;steps++;
                for(int i=0;i<Count;i++)
                {
                    var e=ecology.EnvironmentAt(i);var weather=atmosphere?.AtRegion(i,state.Tick)??ecology.WeatherAt(i,state.Tick);double temperature=weather.Temperature;
                    // Daily normalized supply proxy, not millimetres of precipitation.
                    double precipitation=weather.Precipitation;
                    double snowfall=temperature<1?precipitation*.018*Math.Min(1,(1-temperature)/5):0;
                    double melt=Math.Max(0,temperature)*.002+(temperature> -1?.0006:0);
                    state.Snow[i]=(float)Math.Max(0,Math.Min(2,state.Snow[i]+snowfall-melt));
                    state.Ice[i]=(float)Math.Max(0,Math.Min(1,state.Ice[i]+Math.Max(0,-temperature-1)*.002-Math.Max(0,temperature+1)*.003));
                }
            }
            return steps;
        }
        public float PermanentSnow(int region)
        {var e=ecology.EnvironmentAt(region);return CryosphereClimate.PerennialSnow(e.Temperature,e.SeasonAmplitude,e.Moisture);}
        public SeasonalSnapshot Capture()=>state.Copy();
    }
}
