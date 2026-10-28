using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aevoria.World
{
    public enum PlantKind { Grass, Shrub, Deciduous, Evergreen, DryScrub, Wetland }
    public enum DisturbanceKind { None, Drought, Fire, Flood, Cold, Heat }
    public sealed class SpeciesTraits
    {
        public readonly string Id, Name;
        public readonly double BirthRate, DeathRate, Density, Migration, ForestPreference, MinimumTemperature;
        public readonly bool Predator;
        public SpeciesTraits(string id,string name,double birth,double death,double density,double migration,double forest,double minimum,bool predator=false)
        {Id=id;Name=name;BirthRate=birth;DeathRate=death;Density=density;Migration=migration;ForestPreference=forest;MinimumTemperature=minimum;Predator=predator;}
    }
    [Serializable] public sealed class EcologyRegionState
    {
        public double[] Plants=new double[6], Animals=new double[4];
        public double Moisture, Stress, Burn, ForestAge, Biodiversity;
        public DisturbanceKind Event;
        public int EventMonths, Cooldown;
        public long EventId;
        public double Severity;
        public EcologyRegionState Copy()=>new EcologyRegionState {Plants=(double[])Plants.Clone(),Animals=(double[])Animals.Clone(),Moisture=Moisture,Stress=Stress,Burn=Burn,ForestAge=ForestAge,Biodiversity=Biodiversity,Event=Event,EventMonths=EventMonths,Cooldown=Cooldown,EventId=EventId,Severity=Severity};
    }
    [Serializable] public sealed class EnvironmentalEvent
    {
        public long Id, Tick, EndTick;
        public int Region;
        public DisturbanceKind Kind;
        public double Severity, BiomassLost, AnimalsLost;
        public string Outcome;
        public EnvironmentalEvent Copy()=>(EnvironmentalEvent)MemberwiseClone();
    }
    [Serializable] public sealed class EcologySnapshot
    {
        public int Version=EcologyModel.Version, Resolution, HistoryLimit=512;
        public string Seed;
        public long Tick, EpochTick, Step, NextEventId=1;
        public ulong RandomState;
        public EcologyRegionState[] Regions;
        public List<EnvironmentalEvent> History=new List<EnvironmentalEvent>();
    }
    public readonly struct EcologyEnvironment
    {
        public readonly int Id;
        public readonly GeoPoint Center;
        public readonly Biome Biome;
        public readonly double Height, Temperature, Moisture, Fertility, Water, Latitude, Area,SeasonAmplitude;
        public readonly bool Land;
        public EcologyEnvironment(int id,GeoPoint center,GeographicSample sample,ProceduralGeography geo,double area)
        {Id=id;Center=center;Biome=sample.Biome;Height=sample.ElevationMeters;Temperature=sample.TemperatureCelsius;Moisture=sample.Moisture;Fertility=sample.Fertility;
            Water=geo.Hydrology.MoistureInfluence(center.X,center.Y);Latitude=geo.Climate.LatitudeAt(center.Y);Area=area;SeasonAmplitude=CryosphereClimate.SeasonAmplitude(Latitude,geo.Climate.Continentality(center.X,center.Y));
            Land=Height>geo.SeaLevel+2&&!geo.Hydrology.IsLakeWater(center.X,center.Y,(float)Height);}
    }
    // Pure regional authority. One bounded monthly substep; no dependency on cameras,
    // terrain chunks, Unity objects, wall time, or visual animal lifetimes.
    public sealed class EcologyModel
    {
        public const int Version=3, TicksPerMonth=120, SpeciesCount=4;
        public static readonly SpeciesTraits Grazer=new SpeciesTraits("grazer","Meadow deer",.075,.022,38,.07,.15,-12);
        public static readonly SpeciesTraits Browser=new SpeciesTraits("browser","Woodland deer",.065,.020,25,.055,.85,-16);
        public static readonly SpeciesTraits Hare=new SpeciesTraits("hare","Hare",.12,.045,70,.085,.35,-20);
        public static readonly SpeciesTraits Wolf=new SpeciesTraits("wolf","Wolf",.045,.025,2.5,.10,.45,-24,true);
        public static SpeciesTraits Species(int i)=>i==0?Grazer:i==1?Browser:i==2?Hare:i==3?Wolf:throw new ArgumentOutOfRangeException(nameof(i));
        private readonly ProceduralGeography geography;
        private readonly EcologyEnvironment[] environment;
        private readonly int[][] neighbors;
        private readonly double[][] permeability;
        private readonly double[] transfers, capacities;
        private readonly EcologySnapshot state;
        private long seasonTick=-1;
        private double seasonWave;
        public int Resolution=>state.Resolution;
        public int Count=>environment.Length;
        public long Tick=>state.Tick;
        public long Steps=>state.Step;
        public long NextEventId=>state.NextEventId;
        public double LastStepMilliseconds {get;private set;}
        public double LastBirths {get;private set;}
        public double LastDeaths {get;private set;}
        public double LastMigrationError {get;private set;}
        public int HistoryCount=>state.History.Count;
        public EnvironmentalEvent HistoryAt(int i)=>state.History[i].Copy();
        public EcologyEnvironment EnvironmentAt(int i)=>environment[i];
        public EcologyRegionState Region(int i)=>state.Regions[i].Copy();
        public double Plant(int i,int plant)=>state.Regions[i].Plants[plant];
        public double Population(int i,int species)=>state.Regions[i].Animals[species];
        public double Forest(int i)=>Forest(state.Regions[i]);
        public double Biomass(int i)=>Biomass(state.Regions[i]);
        public double Burn(int i)=>state.Regions[i].Burn;
        public double Moisture(int i)=>state.Regions[i].Moisture;
        public double Stress(int i)=>state.Regions[i].Stress;
        public DisturbanceKind ActiveEvent(int i)=>state.Regions[i].Event;
        public double TotalPopulation(int species) {double total=0;foreach(var r in state.Regions) total+=r.Animals[species];return total;}
        public int RegionAt(GeoPoint p)
        {if(!geography.Configuration.Contains(p)) return -1;var c=geography.Configuration;int x=Math.Min(Resolution-1,(int)((p.X/c.WidthMeters+.5)*Resolution)),y=Math.Min(Resolution-1,(int)((p.Y/c.DepthMeters+.5)*Resolution));return y*Resolution+x;}
        public int NeighborCount(int i)=>neighbors[i].Length;
        public int Neighbor(int i,int edge)=>neighbors[i][edge];
        public double Permeability(int i,int edge)=>permeability[i][edge];
        public double Temperature(int i,long tick)
        {var e=environment[i];if(seasonTick!=tick){seasonTick=tick;seasonWave=Math.Sin((tick/4.0%360)/360*2*Math.PI);}return e.Temperature+seasonWave*(e.Latitude<0?-1:1)*Math.Min(12,Math.Abs(e.Latitude)*.25);}
        public RegionalWeather WeatherAt(int i,long tick)=>RegionalAtmosphere.Forecast(geography,tick,environment[i].Center);
        public string Season(int i,long tick)
        {if(Math.Abs(environment[i].Latitude)<10) return Math.Sin(tick/4.0/360*2*Math.PI)>0?"Wet season":"Dry season";int s=(int)(tick/360%4);if(environment[i].Latitude<0) s=(s+2)%4;return s==0?"Spring":s==1?"Summer":s==2?"Autumn":"Winter";}
        public EcologyModel(ProceduralGeography geo,long tick=0,int historyLimit=512):this(geo,null,tick,historyLimit) {}
        public EcologyModel(ProceduralGeography geo,EcologySnapshot saved):this(geo,saved,0,512) {}
        private EcologyModel(ProceduralGeography geo,EcologySnapshot saved,long tick,int historyLimit)
        {
            geography=geo??throw new ArgumentNullException(nameof(geo));if(tick<0||historyLimit<16||historyLimit>4096) throw new ArgumentException("Invalid ecology epoch or history limit.");
            int n=Math.Max(8,Math.Min(64,(int)Math.Ceiling(Math.Max(geo.Configuration.WidthMeters,geo.Configuration.DepthMeters)/1024)));
            if(saved!=null) ValidateSnapshot(saved,geo.Configuration.Seed,n);
            state=saved==null?new EcologySnapshot {Seed=geo.Configuration.Seed,Resolution=n,Tick=tick,EpochTick=tick,HistoryLimit=historyLimit,Regions=new EcologyRegionState[n*n],RandomState=SeedHash(geo.Configuration.Seed+"/ecosystem/"+geo.Ecosystem.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture))}:Copy(saved);
            environment=new EcologyEnvironment[n*n];neighbors=new int[n*n][];permeability=new double[n*n][];transfers=new double[n*n*4];capacities=new double[n*n*4];
            var c=geo.Configuration;double area=c.WidthMeters*c.DepthMeters/n/n/1000000;
            for(int i=0;i<Count;i++)
            {
                var p=new GeoPoint(((i%n+.5)/n-.5)*c.WidthMeters,((i/n+.5)/n-.5)*c.DepthMeters);
                environment[i]=new EcologyEnvironment(i,p,geo.Sample(p),geo,area);
                if(saved==null)
                {
                    var r=state.Regions[i]=new EcologyRegionState {Moisture=environment[i].Moisture,ForestAge=20};
                    if(environment[i].Land)
                    {
                        for(int k=0;k<6;k++) r.Plants[k]=Target(i,k)*.75;
                        for(int s=0;s<4;s++) r.Animals[s]=Capacity(i,s)*.55;
                    }
                }
            }
            for(int i=0;i<Count;i++)
            {
                var list=new List<int>();var costs=new List<double>();int x=i%n,y=i/n;
                if(x>0) Edge(i,i-1,list,costs);if(x<n-1) Edge(i,i+1,list,costs);if(y>0) Edge(i,i-n,list,costs);if(y<n-1) Edge(i,i+n,list,costs);
                neighbors[i]=list.ToArray();permeability[i]=costs.ToArray();
                if(saved!=null&&!environment[i].Land&&(Biomass(state.Regions[i])>0||Sum(state.Regions[i].Animals)>0)) throw new ArgumentException("Ocean region contains terrestrial ecology.");
            }
        }
        private void Edge(int i,int j,List<int> list,List<double> costs)
        {
            var a=environment[Math.Min(i,j)];var b=environment[Math.Max(i,j)];if(!a.Land||!b.Land) return;
            double pass=1,last=a.Height;bool river=false;
            // Sample a canonical undirected edge, including both endpoints. Bounded
            // spacing is 32 m to 500 m over the five planetary presets. Endpoints alone
            // would incorrectly connect animals across inlets and lake basins.
            double sampleSpacing=Math.Max(32,geography.Configuration.WidthMeters/4096);
            int samples=Math.Max(2,(int)Math.Ceiling(Math.Sqrt(Math.Pow(a.Center.X-b.Center.X,2)+Math.Pow(a.Center.Y-b.Center.Y,2))/sampleSpacing));
            for(int k=0;k<=samples;k++)
            {double f=(double)k/samples,x=a.Center.X+(b.Center.X-a.Center.X)*f,y=a.Center.Y+(b.Center.Y-a.Center.Y)*f;float h=geography.ElevationAt(x,y);
                if(h<=geography.SeaLevel+1||geography.Hydrology.IsLakeWater(x,y,h)||h-geography.SeaLevel>1800) return;
                if(Math.Abs(h-last)/sampleSpacing>.6) pass*=.9;last=h;river|=geography.Hydrology.IsRiverWater(x,y,h);}
            list.Add(j);costs.Add(Math.Max(.02,pass)*(river?.3:1));
        }
        private static ulong SeedHash(string seed) {ulong h=14695981039346656037;foreach(char ch in seed) {h^=ch;h*=1099511628211;}return h==0?1:h;}
        private double Random() {ulong x=state.RandomState;x^=x>>12;x^=x<<25;x^=x>>27;state.RandomState=x;return ((x*2685821657736338717UL)>>11)*(1.0/9007199254740992.0);}
        private static double Clamp(double n)=>Math.Max(0,Math.Min(1,n));
        private static double Sum(double[] a) {double s=0;foreach(double v in a)s+=v;return s;}
        private static double Forest(EcologyRegionState r)=>r.Plants[2]+r.Plants[3];
        private static double Biomass(EcologyRegionState r)=>Sum(r.Plants);
        private double Target(int i,int kind)
        {
            var e=environment[i];if(!e.Land) return 0;
            double coldGrowth=e.Biome==Biome.BorealForest?Clamp((e.Temperature+14)/16):Clamp((e.Temperature+5)/16);
            double forest=Clamp((e.Moisture-.3)*1.8)*coldGrowth*.68;
            forest*=e.Biome==Biome.Grassland?.18:e.Biome==Biome.Savanna?.25:e.Biome==Biome.Desert||e.Biome==Biome.ColdDesert||e.Biome==Biome.Tundra||e.Biome==Biome.Snow||e.Biome==Biome.Alpine?0:1;
            double evergreen=Clamp((18-e.Temperature)/20);
            double wet=Clamp((e.Moisture-.72)*3)*e.Water*.2,dry=Clamp((.45-e.Moisture)*2)*.22;
            double productivity=.22+.65*e.Fertility;
            if(geography.Configuration.Terrain.Profile==GeographyProfile.Atlas)
            {
                var habitat=BiomeHabitat.For(e.Biome);forest=habitat.TreeDensity*coldGrowth*.68;
                evergreen=e.Biome==Biome.MediterraneanWoodland||e.Biome==Biome.MediterraneanScrub?.85:e.Biome==Biome.Oasis?.6:evergreen;
                wet=habitat.Cover==3?.22:0;dry=habitat.Cover==2?.22:0;productivity*=habitat.CoverDensity;
            }
            return kind==0?(1-forest-wet-dry)*productivity*.68:kind==1?.1*productivity:kind==2?forest*(1-evergreen)*productivity:kind==3?forest*evergreen*productivity:kind==4?dry*productivity:wet*productivity;
        }
        public double Capacity(int i,int species)
        {
            var e=environment[i];if(!e.Land) return 0;var r=state.Regions[i];var t=Species(species);
            if(t.Predator) return Math.Min(e.Area*t.Density,(r.Animals[0]+r.Animals[1]+r.Animals[2]*.25)/35);
            double food=species==0?r.Plants[0]+r.Plants[4]:species==1?Forest(r)+r.Plants[1]:Biomass(r)*.7;
            return e.Area*t.Density*food*Clamp((Temperature(i,state.Tick)-t.MinimumTemperature+8)/20)*(.45+.55*r.Moisture)*(1-r.Burn*.6);
        }
        public double HabitatSuitability(int i,int species)=>Clamp(Capacity(i,species)/Math.Max(.001,environment[i].Area*Species(species).Density));
        public void HarvestForest(int region,double fraction)
        {if(region<0||region>=Count||!Finite(fraction)||fraction<0||fraction>.01)throw new ArgumentOutOfRangeException(nameof(fraction));var r=state.Regions[region];double forest=Forest(r);if(forest<=0)return;double remaining=Math.Max(0,forest-fraction)/forest;r.Plants[2]*=remaining;r.Plants[3]*=remaining;}
        public int AdvanceTo(long tick,int maxSteps=4)
        {
            if(tick<state.Tick||maxSteps<=0) throw new ArgumentOutOfRangeException(nameof(tick));int steps=0;
            while(state.Tick+TicksPerMonth<=tick&&steps<maxSteps) {StepMonth();steps++;}return steps;
        }
        private void StepMonth()
        {
            long start=Stopwatch.GetTimestamp();state.Tick+=TicksPerMonth;state.Step++;LastBirths=LastDeaths=0;
            for(int i=0;i<Count;i++)
            {
                var e=environment[i];if(!e.Land) continue;var r=state.Regions[i];
                double before=Biomass(r),oldForest=Forest(r),animalsBefore=Sum(r.Animals),oldStress=r.Stress,temp=Temperature(i,state.Tick);
                // Correlated seasonal water, not an unrelated random climate each frame.
                double season=Math.Sin(state.Tick/4.0/360*2*Math.PI)*(e.Latitude<0?-1:1);
                double rain=Clamp(e.Moisture*.8+WeatherAt(i,state.Tick).Precipitation*.2+season*(Math.Abs(e.Latitude)<10?.10:-.04));
                r.Moisture=Clamp(r.Moisture*.65+rain*.35);
                if(r.Cooldown>0)r.Cooldown--;
                if(r.Event==DisturbanceKind.None&&r.Cooldown==0)
                {
                    double roll=Random();
                    if(r.Moisture<.32&&temp>12&&roll<.055) StartEvent(i,DisturbanceKind.Drought,.45+Random()*.45,3);
                    else if(r.Moisture<.48&&temp>18&&before>.22&&roll<.006) StartEvent(i,DisturbanceKind.Fire,.45+Random()*.5,1);
                    else if(r.Moisture>.69&&e.Water>.3&&roll<.018) StartEvent(i,DisturbanceKind.Flood,.35+Random()*.5,2);
                    else if(temp< -8&&roll<.025) StartEvent(i,DisturbanceKind.Cold,.3+Random()*.4,2);
                    else if(temp>33&&roll<.02) StartEvent(i,DisturbanceKind.Heat,.3+Random()*.4,2);
                }
                if(r.Event==DisturbanceKind.Drought||r.Event==DisturbanceKind.Heat) r.Moisture*=1-r.Severity*.35;
                if(r.Event==DisturbanceKind.Flood) r.Moisture=Clamp(r.Moisture+.2);
                double growth=Clamp((temp+4)/22)*Clamp((43-temp)/16)*Clamp(r.Moisture*1.8),scar=r.Burn;
                for(int k=0;k<6;k++)
                {
                    double target=Target(i,k)*(1-scar*.8),rate=k==2||k==3?.012:.22;
                    double value=r.Plants[k];value+=(target-value)*rate*growth;
                    double senescence=k==2||k==3?Math.Max(0,r.ForestAge-120)/480*.004:0;
                    value-=value*(.001+senescence+(1-growth)*.015+(r.Event==DisturbanceKind.Fire?r.Severity*.7:r.Event==DisturbanceKind.Flood?.06*r.Severity:0));
                    r.Plants[k]=Clamp(value);
                }
                r.ForestAge=Math.Min(600,r.ForestAge+1.0/12);r.ForestAge*=1-(r.Event==DisturbanceKind.Fire?r.Severity*.8:0);
                if(Forest(r)>oldForest)r.ForestAge*=oldForest/Forest(r); // recruits dilute mean stand age
                r.Burn=Clamp(r.Event==DisturbanceKind.Fire?Math.Max(r.Burn,r.Severity):r.Burn*Math.Exp(-1.0/60));
                for(int s=0;s<4;s++)
                {
                    var traits=Species(s);double population=r.Animals[s],capacity=Capacity(i,s),ratio=population/Math.Max(.01,capacity);
                    double births=population*traits.BirthRate*Clamp(1-ratio)*(.55+.45*growth);
                    double deaths=population*Math.Min(.65,traits.DeathRate+Math.Max(0,ratio-1)*.06+(r.Event==DisturbanceKind.Fire?.12*r.Severity:0));
                    r.Animals[s]=Math.Max(0,population+births-deaths);LastBirths+=births;LastDeaths+=deaths;
                }
                // Actual kills are bounded by available prey, counted once as deaths.
                double prey=r.Animals[0]+r.Animals[1]+r.Animals[2]*.25,kills=Math.Min(prey*.035,r.Animals[3]*.65);
                if(prey>0) for(int s=0;s<3;s++) {double loss=r.Animals[s]*(kills/prey)*(s==2?.25:1);r.Animals[s]-=loss;LastDeaths+=loss;}
                double grazing=(r.Animals[0]+r.Animals[2]*.12)/Math.Max(.01,e.Area)*.00007,browsing=r.Animals[1]/Math.Max(.01,e.Area)*.00006;
                r.Plants[0]=Math.Max(0,r.Plants[0]-grazing);r.Plants[2]=Math.Max(0,r.Plants[2]-browsing*.5);r.Plants[3]=Math.Max(0,r.Plants[3]-browsing*.5);
                r.Stress=Clamp(1-growth+scar*.35);int present=0;for(int s=0;s<4;s++)if(r.Animals[s]>.1)present++;r.Biodiversity=present/4.0;
                if(r.Event!=DisturbanceKind.None)
                {
                    for(int h=state.History.Count-1;h>=0;h--)if(state.History[h].Id==r.EventId) {var ev=state.History[h];ev.BiomassLost+=Math.Max(0,before-Biomass(r));ev.AnimalsLost+=Math.Max(0,animalsBefore-Sum(r.Animals));break;}
                    if(--r.EventMonths<=0) EndEvent(i);
                }
                if(oldStress<.8&&r.Stress>=.8) Log(i,DisturbanceKind.None,r.Stress,"Severe habitat stress",state.Tick);
                else if(oldStress>.65&&r.Stress<.4) Log(i,DisturbanceKind.None,1-r.Stress,"Habitat recovering",state.Tick);
                if(before>=.12&&Biomass(r)<.12) Log(i,DisturbanceKind.None,1-Biomass(r),"Vegetation collapse below 12% biomass",state.Tick);
                else if(before<.25&&Biomass(r)>=.25) Log(i,DisturbanceKind.None,Biomass(r),"Vegetation recovered above 25% biomass",state.Tick);
                if(animalsBefore/e.Area<1&&Sum(r.Animals)/e.Area>=2) Log(i,DisturbanceKind.None,.5,"Wildlife recovered above two animals per square kilometre",state.Tick);
            }
            // Second pass: buffered transfers conserve populations independently of
            // births/deaths and cannot cascade across multiple regions in one month.
            Migrate();LastStepMilliseconds=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
        }
        public void Migrate()
        {
            Array.Clear(transfers,0,transfers.Length);double before=0;
            for(int i=0;i<Count;i++)for(int s=0;s<4;s++) {capacities[i*4+s]=Capacity(i,s);before+=state.Regions[i].Animals[s];}
            for(int i=0;i<Count;i++)for(int s=0;s<4;s++)
            {
                double pop=state.Regions[i].Animals[s];if(pop<=0)continue;int best=-1;double score=0;
                double here=capacities[i*4+s]/(pop+1)-state.Regions[i].Stress;
                for(int j=0;j<neighbors[i].Length;j++) {int to=neighbors[i][j];double candidate=(capacities[to*4+s]/(state.Regions[to].Animals[s]+1)-state.Regions[to].Stress-here)*permeability[i][j];if(candidate>score){score=candidate;best=j;}}
                if(best>=0) {int to=neighbors[i][best];double moving=pop*Species(s).Migration*Clamp(score)*permeability[i][best];transfers[i*4+s]-=moving;transfers[to*4+s]+=moving;}
            }
            double after=0;for(int i=0;i<Count;i++)for(int s=0;s<4;s++) {state.Regions[i].Animals[s]+=transfers[i*4+s];after+=state.Regions[i].Animals[s];}LastMigrationError=after-before;
        }
        // Controlled external disturbances are also useful for deterministic probes.
        // Preconditions prevent fire/flood from being painted onto arbitrary oceans.
        public bool StartEvent(int i,DisturbanceKind kind,double severity,int months)
        {
            if(i<0||i>=Count||!Enum.IsDefined(typeof(DisturbanceKind),kind)||kind==DisturbanceKind.None||!Finite(severity)||severity<=0||severity>1||months<1||months>24) throw new ArgumentException("Invalid disturbance.");
            var r=state.Regions[i];var e=environment[i];if(!e.Land||r.Event!=DisturbanceKind.None||kind==DisturbanceKind.Fire&&(Biomass(r)<.15||r.Moisture>.6)||kind==DisturbanceKind.Flood&&e.Water<.2) return false;
            r.Event=kind;r.EventMonths=months;r.Severity=severity;r.EventId=Log(i,kind,severity,"Active",0);return true;
        }
        private void EndEvent(int i)
        {
            var r=state.Regions[i];bool fire=r.Event==DisturbanceKind.Fire;
            for(int h=state.History.Count-1;h>=0;h--)if(state.History[h].Id==r.EventId){state.History[h].EndTick=state.Tick;state.History[h].Outcome="Ended; recovery continues";break;}
            r.Event=DisturbanceKind.None;r.EventMonths=0;r.Severity=0;r.Cooldown=6;
            TrimHistory();
            if(fire) for(int k=0;k<neighbors[i].Length;k++) {int n=neighbors[i][k];if(state.Regions[n].Moisture<.48&&Random()<.12*permeability[i][k]) StartEvent(n,DisturbanceKind.Fire,.45,1);}
        }
        private long Log(int region,DisturbanceKind kind,double severity,string outcome,long end)
        {
            long id=state.NextEventId++;state.History.Add(new EnvironmentalEvent {Id=id,Tick=state.Tick,EndTick=end,Region=region,Kind=kind,Severity=severity,Outcome=outcome});
            // Active records remain; at most one per region. Completed history bounded.
            TrimHistory();return id;
        }
        private void TrimHistory()
        {
            int completed=0;foreach(var ev in state.History)if(ev.EndTick>0)completed++;
            for(int h=0;completed>state.HistoryLimit&&h<state.History.Count;) {if(state.History[h].EndTick>0){state.History.RemoveAt(h);completed--;}else h++;}
        }
        public EcologySnapshot Capture()=>Copy(state);
        private static EcologySnapshot Copy(EcologySnapshot s)
        {var copy=new EcologySnapshot {Version=s.Version,Resolution=s.Resolution,Seed=s.Seed,Tick=s.Tick,EpochTick=s.EpochTick,Step=s.Step,NextEventId=s.NextEventId,RandomState=s.RandomState,HistoryLimit=s.HistoryLimit,Regions=new EcologyRegionState[s.Regions.Length]};for(int i=0;i<s.Regions.Length;i++)copy.Regions[i]=s.Regions[i].Copy();foreach(var e in s.History)copy.History.Add(e.Copy());return copy;}
        private static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
        public static void ValidateSnapshot(EcologySnapshot s,string seed,int resolution)
        {
            if(s==null||s.Version!=Version)throw new NotSupportedException("Unsupported ecology version; original archive preserved.");
            if(s.Seed!=seed||s.Resolution!=resolution||s.Regions==null||s.Regions.Length!=resolution*resolution||s.RandomState==0||s.Tick<0||s.EpochTick<0||s.Tick<s.EpochTick||s.Step<0||(s.Tick-s.EpochTick)/TicksPerMonth!=s.Step||(s.Tick-s.EpochTick)%TicksPerMonth!=0||s.HistoryLimit<16||s.HistoryLimit>4096||s.History==null||s.History.Count>s.HistoryLimit+s.Regions.Length||s.NextEventId<1)throw new ArgumentException("Invalid ecology identity or progression.");
            foreach(var r in s.Regions)
            {
                if(r==null||r.Plants==null||r.Plants.Length!=6||r.Animals==null||r.Animals.Length!=4||!Enum.IsDefined(typeof(DisturbanceKind),r.Event)||r.EventMonths<0||r.EventMonths>24||r.Cooldown<0||r.Cooldown>6||!Finite(r.ForestAge)||r.ForestAge<0||r.ForestAge>600)throw new ArgumentException("Invalid ecology region.");
                foreach(double v in r.Plants)if(!Finite(v)||v<0||v>1)throw new ArgumentException("Invalid vegetation.");
                foreach(double v in r.Animals)if(!Finite(v)||v<0||v>1e9)throw new ArgumentException("Invalid population.");
                if(Biomass(r)>1.000001||!Unit(r.Moisture)||!Unit(r.Stress)||!Unit(r.Burn)||!Unit(r.Severity)||!Unit(r.Biodiversity)||r.Event==DisturbanceKind.None&&(r.EventMonths!=0||r.Severity!=0)||r.Event!=DisturbanceKind.None&&(r.EventMonths==0||r.EventId<1||r.EventId>=s.NextEventId))throw new ArgumentException("Invalid ecological bounds.");
            }
            var ids=new HashSet<long>();int completed=0;foreach(var e in s.History)
            {if(e==null||!ids.Add(e.Id)||e.Id<1||e.Id>=s.NextEventId||e.Region<0||e.Region>=s.Regions.Length||e.Tick<s.EpochTick||e.Tick>s.Tick||e.EndTick<0||e.EndTick>0&&(e.EndTick<e.Tick||e.EndTick>s.Tick)||!Enum.IsDefined(typeof(DisturbanceKind),e.Kind)||!Unit(e.Severity)||!Finite(e.BiomassLost)||e.BiomassLost<0||!Finite(e.AnimalsLost)||e.AnimalsLost<0||string.IsNullOrEmpty(e.Outcome)||e.Outcome.Length>200)throw new ArgumentException("Invalid environmental history.");if(e.EndTick>0)completed++;}
            if(completed>s.HistoryLimit)throw new ArgumentException("History retention exceeded.");
            for(int i=0;i<s.Regions.Length;i++)if(s.Regions[i].Event!=DisturbanceKind.None) {bool found=false;foreach(var e in s.History)if(e.Id==s.Regions[i].EventId&&e.Region==i&&e.Kind==s.Regions[i].Event&&e.EndTick==0)found=true;if(!found)throw new ArgumentException("Active event missing history.");}
        }
        private static bool Unit(double v)=>Finite(v)&&v>=0&&v<=1;
    }
}
