using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Aevoria.World
{
    // Pure aggregate authority. Geographic doubles and calendar ticks are the
    // only coordinates/time used here; cameras and frame cadence are irrelevant.
    public sealed partial class CivilizationModel
    {
        public const int Version=2,TicksPerMonth=120,RecentHistoryLimit=512,HistoryPageSize=256;
        private readonly ProceduralGeography geography;
        private readonly CivilizationSnapshot state;
        private readonly string scope;
        private readonly ICivilizationHistoryArchive historyArchive;
        private List<SettlementSite> candidateSites;
        public IReadOnlyList<CivilizationState> Civilizations=>state.Civilizations;
        public IReadOnlyList<SettlementState> Settlements=>state.Settlements;
        public IReadOnlyList<DiplomaticRelationship> Relationships=>state.Relationships;
        public IReadOnlyList<SocialHistoryEvent> RecentHistory=>state.RecentHistory;
        public IReadOnlyList<SocialHistoryPage> HistoryPages=>state.HistoryPages;
        public long Tick=>state.Tick;
        public long Months=>state.Months;
        public int Revision{get;private set;}
        public double LastStepMilliseconds{get;private set;}
        public int TerritoryResolution=>state.TerritoryResolution;
        public double TotalPopulation{get{double n=0;foreach(var s in state.Settlements)n+=s.Population;return n;}}
        public double TotalFood{get{double n=0;foreach(var s in state.Settlements)n+=s.Resources.Food;return n;}}
        public CivilizationModel(ProceduralGeography geo,long tick=0,CivilizationSettings settings=null,ICivilizationHistoryArchive archive=null,CancellationToken token=default)
        {
            geography=geo??throw new ArgumentNullException(nameof(geo));if(tick<0)throw new ArgumentOutOfRangeException(nameof(tick));scope=CivilIdentity.Scope(geo);historyArchive=archive;
            settings=settings?.Copy()??new CivilizationSettings();settings.Validate();state=new CivilizationSnapshot{Seed=geo.Configuration.Seed,Tick=tick,EpochTick=tick,Settings=settings,RandomState=CivilIdentity.Seed(scope+"/simulation"),TerritoryResolution=Math.Max(32,Math.Min(128,(int)Math.Ceiling(geo.Configuration.WidthMeters/8000)))};
            candidateSites=CivilizationGeography.Candidates(geo,settings,token);
            int target=settings.RequestedCivilizations;if(target==0){double w=geo.Configuration.WidthMeters;target=w<=128000?4:w<=256000?6:w<=512000?10:w<=1024000?16:24;}
            double spacing=geo.Configuration.WidthMeters*settings.MinimumSpacingFraction;
            foreach(var site in candidateSites)
            {
                token.ThrowIfCancellationRequested();if(state.Civilizations.Count>=target)break;bool clear=true;
                foreach(var c in state.Civilizations)if(Distance(site.X,site.Z,c.OriginX,c.OriginZ)<spacing){clear=false;break;}if(!clear)continue;
                int i=state.Civilizations.Count;ulong names=CivilIdentity.Seed(scope+"/names/"+i);string name=Name(ref names,false)+" "+(i%3==0?"League":i%3==1?"Clans":"People");
                double hue=(.13+i*.6180339887498949)%1;Color(hue,out double r,out double g,out double b);
                var civ=new CivilizationState{Id=CivilIdentity.Id(scope,"civilization/"+i),PoliticalId=CivilIdentity.Id(scope,"polity/"+i),CultureId=CivilIdentity.Id(scope,"culture/"+i),Name=name,CultureName=Name(ref names,false),LanguageFamily="Family "+(i%7+1),OriginX=site.X,OriginZ=site.Z,Red=r,Green=g,Blue=b,FoundedTick=tick,LeaderId=CivilIdentity.Id(scope,"leader/"+i),LeaderName=Name(ref names,false)+" "+Name(ref names,false)};
                state.Civilizations.Add(civ);Emit(SocialEventKind.CivilizationFounded,civ,null,name+" founded",name+" established an independent tribal council.",0,0);
                var settlement=Found(civ,site,85+CivilIdentity.Next(ref state.RandomState)*60,0);civ.CapitalSettlementId=settlement.Id;
            }
            for(int a=0;a<state.Civilizations.Count;a++)for(int b=a+1;b<state.Civilizations.Count;b++)state.Relationships.Add(new DiplomaticRelationship{A=state.Civilizations[a].Id,B=state.Civilizations[b].Id});
            PrepareTerritory(token);UpdateTotals();RebuildTerritory(false);Validate(state,geo.Configuration,tick);
        }
        public CivilizationModel(ProceduralGeography geo,CivilizationSnapshot saved,ICivilizationHistoryArchive archive=null)
        {
            geography=geo??throw new ArgumentNullException(nameof(geo));Validate(saved,geo.Configuration,saved?.Tick??0);state=saved.Copy();scope=CivilIdentity.Scope(geo);historyArchive=archive;PrepareTerritory(default);
            // Loading is restoration, never generation; no founding events emitted.
        }
        public CivilizationSnapshot Capture(){FlushHistory();return state.Copy();}
        public int AdvanceTo(long tick,int maximumMonths=8)
        {
            if(tick<state.Tick||maximumMonths<1)throw new ArgumentException("Invalid society timeline or budget.");int count=0;
            while(state.Tick+TicksPerMonth<=tick&&count<maximumMonths)
            {
                FlushHistory();long at=Stopwatch.GetTimestamp();state.Tick+=TicksPerMonth;state.Months++;MonthlyStep();UpdateTotals();Revision++;LastStepMilliseconds=(Stopwatch.GetTimestamp()-at)*1000.0/Stopwatch.Frequency;count++;
            }
            return count;
        }
        private SettlementState Found(CivilizationState civ,SettlementSite site,double people,int ordinal,CivicInventory transferred=null)
        {
            ulong names=CivilIdentity.Seed(civ.Id+"/settlement/"+ordinal);var s=new SettlementState{Id=CivilIdentity.Id(civ.Id,"settlement/"+ordinal),Name=Name(ref names,true),CivilizationId=civ.Id,X=site.X,Y=site.Y,Z=site.Z,Population=people,Capacity=180,Fertility=site.Fertility,Freshwater=site.Freshwater,TimberPotential=site.Timber,StonePotential=site.Stone,OrePotential=site.Ore,ClimateSuitability=site.Climate,FoundedTick=state.Tick,LastDevelopmentTick=state.Tick,LastExpansionTick=state.Tick,LastMilestone=(long)(people/100),Resources=new CivicInventory{Food=people*(1.5+site.Fertility),Timber=20+site.Timber*45+people*.5,Stone=12+site.Stone*30+people*.25,Ore=site.Ore*5,Supplies=16+people*.15}};
            if(transferred!=null)s.Resources=transferred.Copy();
            // Founders bring geographically supported stores and build only what
            // those stores can pay for. This bootstrap is recorded, not daily income.
            state.Settlements.Add(s);civ.Settlements.Add(s.Id);Construct(s,CivicBuildingKind.MeetingHall,true);Construct(s,CivicBuildingKind.Granary,true);Construct(s,CivicBuildingKind.Farm,true);while(HousingCapacity(s)<Math.Ceiling(s.Population)&&s.Buildings.Count<128)if(!Construct(s,CivicBuildingKind.Dwelling,true))break;s.Capacity=Math.Max(40,HousingCapacity(s));
            SynchronizeResidents(s);
            Emit(SocialEventKind.SettlementFounded,civ,s,s.Name+" established",FormattableString.Invariant($"{s.Name} was founded with {people:0} inhabitants and {(transferred==null?"carried timber, stone and supplies proportional to the founding party, plus geographically supported local stores.":"stores transferred from its parent settlement.")}"),0,people);return s;
        }
        private void UpdateTotals()
        {
            foreach(var c in state.Civilizations){c.Population=0;c.Development=0;foreach(var s in state.Settlements)if(s.CivilizationId==c.Id){c.Population+=s.Population;c.Development+=(int)s.Stage;}c.Development/=Math.Max(1,c.Settlements.Count);}
        }
        public CivilizationState Civilization(string id)=>state.Civilizations.Find(c=>c.Id==id);
        public SettlementState Settlement(string id)=>state.Settlements.Find(s=>s.Id==id);
        private void Emit(SocialEventKind kind,CivilizationState c,SettlementState s,string title,string description,double before,double after,string other=null)
        {
            long sequence=state.NextEventSequence++;state.RecentHistory.Add(new SocialHistoryEvent{Sequence=sequence,Tick=state.Tick,Id=CivilIdentity.Id(scope,"event/"+sequence),Kind=kind,CivilizationId=c?.Id,SettlementId=s?.Id,OtherCivilizationId=other,Title=title,Description=description,X=s?.X??c?.OriginX??0,Z=s?.Z??c?.OriginZ??0,Before=before,After=after});
        }
        public void FlushHistory()
        {
            while(state.RecentHistory.Count>RecentHistoryLimit)
            {
                if(historyArchive==null)throw new InvalidOperationException("A durable society history archive is required before further simulation. No historical events have been discarded.");
                var page=state.RecentHistory.GetRange(0,HistoryPageSize);var saved=historyArchive.Store(page);if(saved==null||saved.Count!=HistoryPageSize||saved.First!=page[0].Sequence||saved.Last!=page[page.Count-1].Sequence)throw new InvalidOperationException("History archive did not confirm the complete page.");
                state.HistoryPages.Add(saved.Copy());state.RecentHistory.RemoveRange(0,HistoryPageSize);
            }
        }
        public IReadOnlyList<SocialHistoryEvent> ReadHistoryPage(int index){if(historyArchive==null)throw new InvalidOperationException("History archive unavailable.");return historyArchive.Read(state.HistoryPages[index]);}
        internal static double Distance(double ax,double az,double bx,double bz)=>Math.Sqrt((ax-bx)*(ax-bx)+(az-bz)*(az-bz));
        private static string Name(ref ulong random,bool place)
        {
            string[] start={"Aer","Bel","Cor","Dor","Eli","Falen","Gal","Haven","Iri","Jor","Kel","Lys","Mar","Nor","Or","Palen","Quel","Riv","Ser","Tor","Ul","Val","Wen","Ys","Zar"},middle={"a","e","i","o","u","ae","en","or","il","an"},end=place?new[]{"ford","haven","stead","mere","brook","hill","vale","wick","ton","rest","bridge","holm"}:new[]{"ian","ari","en","eth","or","ai","on","el","ir","os","yn","as"};
            return start[(int)(CivilIdentity.Next(ref random)*start.Length)]+middle[(int)(CivilIdentity.Next(ref random)*middle.Length)]+end[(int)(CivilIdentity.Next(ref random)*end.Length)];
        }
        private static void Color(double hue,out double r,out double g,out double b){double h=hue*6,f=h-Math.Floor(h),p=.22,q=.82-(.82-p)*f,t=p+(.82-p)*f;switch((int)h%6){case 0:r=.82;g=t;b=p;break;case 1:r=q;g=.82;b=p;break;case 2:r=p;g=.82;b=t;break;case 3:r=p;g=q;b=.82;break;case 4:r=t;g=p;b=.82;break;default:r=.82;g=p;b=q;break;}}
        public static void Validate(CivilizationSnapshot s,WorldConfiguration config,long clockTick)
        {
            if(s!=null&&s.Version!=Version)throw new ArgumentException("This saved society uses simulation version "+s.Version+"; this build requires "+Version+". Open it with its original release or create a new world.");
            if(s==null||s.Version!=Version||s.Seed!=config.Seed||s.Tick<0||s.Tick>clockTick||s.EpochTick<0||s.Tick<s.EpochTick||s.Tick-s.EpochTick!=s.Months*TicksPerMonth||s.RandomState==0||s.Settings==null||s.TerritoryResolution<32||s.TerritoryResolution>128||s.Territory==null||s.Territory.Length!=s.TerritoryResolution*s.TerritoryResolution||s.Civilizations==null||s.Civilizations.Count>64||s.Settlements==null||s.Relationships==null||s.RecentHistory==null||s.HistoryPages==null||s.NextEventSequence<1)throw new ArgumentException("Invalid civilization snapshot.");
            s.Settings.Validate();if(s.Settlements.Count>s.Civilizations.Count*s.Settings.MaximumSettlementsPerCivilization)throw new ArgumentException("Too many settlements.");
            if(s.Months<0||s.Months>(s.Tick-s.EpochTick)/TicksPerMonth)throw new ArgumentException("Invalid society month count.");
            var buildingIds=new HashSet<string>();
            foreach(var c in s.Civilizations)
            {
                if(c==null||!Enum.IsDefined(typeof(PoliticalForm),c.Government)||c.FoundedTick<s.EpochTick||c.FoundedTick>s.Tick||!CivilIdentity.Finite(c.ControlledSquareKilometres)||c.ControlledSquareKilometres<0||!CivilIdentity.Finite(c.Development)||c.Development<0||c.Development>3||!CivilIdentity.Finite(c.Cohesion)||c.Cohesion<0||c.Cohesion>1)throw new ArgumentException("Invalid political state.");
            }
            foreach(var p in s.Settlements)
            {
                if(p==null||p.Buildings==null||p.FoundedTick<s.EpochTick||p.LastDevelopmentTick<p.FoundedTick||p.LastDevelopmentTick>s.Tick||p.LastExpansionTick<p.FoundedTick||p.LastExpansionTick>s.Tick||p.LastMilestone<0||!CivilIdentity.Finite(p.Capacity)||p.Capacity<=0||p.Capacity>1000000||!CivilIdentity.Finite(p.GrowthRate)||!CivilIdentity.Finite(p.LastBirths)||p.LastBirths<0||!CivilIdentity.Finite(p.LastDeaths)||p.LastDeaths<0||!CivilIdentity.Finite(p.Prosperity)||p.Prosperity<0||p.Prosperity>1||!CivilIdentity.Finite(p.ScarcityMonths)||p.ScarcityMonths<0)throw new ArgumentException("Invalid settlement economy or timeline.");
                foreach(var b in p.Buildings){if(b==null||!Guid.TryParseExact(b.Id,"N",out _)||!buildingIds.Add(b.Id)||!Enum.IsDefined(typeof(CivicBuildingKind),b.Kind)||!config.Contains(new GeoPoint(b.X,b.Z))||!CivilIdentity.Finite(b.Y)||!CivilIdentity.Finite(b.Yaw))throw new ArgumentException("Invalid construction record.");ValidateHousing(b);}
                int links=0;foreach(var c in s.Civilizations)if(c.Settlements!=null)foreach(string id in c.Settlements)if(id==p.Id&&c.Id==p.CivilizationId)links++;
                if(links!=1)throw new ArgumentException("Settlement must belong to exactly one polity.");
            }
            var civs=new HashSet<string>();var places=new HashSet<string>();foreach(var c in s.Civilizations){if(c==null||!Guid.TryParseExact(c.Id,"N",out _)||!civs.Add(c.Id)||string.IsNullOrWhiteSpace(c.Name)||c.Settlements==null||!config.Contains(new GeoPoint(c.OriginX,c.OriginZ))||!CivilIdentity.Finite(c.Population)||c.Population<0||!CivilIdentity.Finite(c.Red)||c.Red<0||c.Red>1||!CivilIdentity.Finite(c.Green)||c.Green<0||c.Green>1||!CivilIdentity.Finite(c.Blue)||c.Blue<0||c.Blue>1)throw new ArgumentException("Invalid civilization identity.");}
            foreach(var p in s.Settlements){if(p==null||!places.Add(p.Id)||!Guid.TryParseExact(p.Id,"N",out _)||!civs.Contains(p.CivilizationId)||string.IsNullOrWhiteSpace(p.Name)||!CivilIdentity.Finite(p.Population)||p.Population<0||p.Population>1000000||!CivilIdentity.Finite(p.Y)||!config.Contains(new GeoPoint(p.X,p.Z))||p.Resources==null||p.Buildings==null||p.Buildings.Count>256||!Enum.IsDefined(typeof(SettlementStage),p.Stage)||p.FoundedTick>s.Tick)throw new ArgumentException("Invalid settlement state.");p.Resources.Validate();ValidateResidents(p,config);foreach(double n in new[]{p.Fertility,p.Freshwater,p.TimberPotential,p.StonePotential,p.OrePotential,p.ClimateSuitability,p.FoodSatisfaction})if(!CivilIdentity.Finite(n)||n<0||n>1)throw new ArgumentException("Invalid settlement habitat.");}
            foreach(var c in s.Civilizations){var links=new HashSet<string>();double pop=0;foreach(string id in c.Settlements){var p=s.Settlements.Find(v=>v.Id==id);if(!links.Add(id)||p==null||p.CivilizationId!=c.Id)throw new ArgumentException("Invalid settlement ownership.");pop+=p.Population;}if(!links.Contains(c.CapitalSettlementId)||Math.Abs(pop-c.Population)>.00001)throw new ArgumentException("Invalid capital or population total.");}
            foreach(int owner in s.Territory)if(owner< -1||owner>=s.Civilizations.Count)throw new ArgumentException("Invalid territorial owner.");
            var pairs=new HashSet<string>();foreach(var r in s.Relationships)if(r==null||r.A==r.B||!civs.Contains(r.A)||!civs.Contains(r.B)||!pairs.Add(string.CompareOrdinal(r.A,r.B)<0?r.A+"/"+r.B:r.B+"/"+r.A)||!CivilIdentity.Finite(r.Value)||r.Value< -100||r.Value>100)throw new ArgumentException("Invalid political relationship.");
            foreach(var r in s.Relationships)if(!Enum.IsDefined(typeof(DiplomaticDisposition),r.Disposition)||!CivilIdentity.Finite(r.SharedBorderKilometres)||r.SharedBorderKilometres<0||!CivilIdentity.Finite(r.Competition)||r.Competition<0)throw new ArgumentException("Invalid relationship pressure.");
            foreach(var e in s.RecentHistory)if(e==null||!Enum.IsDefined(typeof(SocialEventKind),e.Kind)||!CivilIdentity.Finite(e.X)||!CivilIdentity.Finite(e.Z)||e.OtherCivilizationId!=null&&!civs.Contains(e.OtherCivilizationId))throw new ArgumentException("Invalid historical participants or location.");
            long next=1;foreach(var p in s.HistoryPages){if(p==null||p.First!=next||p.Last<p.First||p.Count!=p.Last-p.First+1||string.IsNullOrEmpty(p.Key)||string.IsNullOrEmpty(p.Sha256))throw new ArgumentException("Invalid historical page sequence.");next=p.Last+1;}
            foreach(var e in s.RecentHistory){if(e==null||e.Sequence!=next++||e.Tick<s.EpochTick||e.Tick>s.Tick||!Guid.TryParseExact(e.Id,"N",out _)||string.IsNullOrEmpty(e.Title)||string.IsNullOrEmpty(e.Description)||e.CivilizationId!=null&&!civs.Contains(e.CivilizationId)||e.SettlementId!=null&&!places.Contains(e.SettlementId)||!CivilIdentity.Finite(e.Before)||!CivilIdentity.Finite(e.After))throw new ArgumentException("Invalid factual history.");}if(next!=s.NextEventSequence)throw new ArgumentException("Historical sequence does not match the snapshot.");
        }
    }
}
