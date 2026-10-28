using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Aevoria.World
{
    public enum SettlementStage { Camp,Village,Town,RegionalCenter }
    public enum PoliticalForm { TribalCouncil,CityState,Kingdom,Republic,Empire }
    public enum DiplomaticDisposition { Hostile,Neutral,Friendly }
    public enum CivicBuildingKind { Dwelling,Granary,MeetingHall,Farm,Workshop,Storehouse,Market }
    public enum SocialEventKind { CivilizationFounded,SettlementFounded,Development,PopulationMilestone,Decline,ResourceCrisis,Recovery,TerritoryExpansion,TerritoryContraction,PoliticalTransition,RelationshipChanged,Migration,ResidentKilled }

    [Serializable] public sealed class CivilizationSettings
    {
        // Zero selects the world-size-scaled count. Tests may request a fixed cap.
        public int RequestedCivilizations,CandidateResolution=96,MaximumSettlementsPerCivilization=12;
        public double MinimumSpacingFraction=.12;
        public void Validate(){if(RequestedCivilizations<0||RequestedCivilizations>64||CandidateResolution<16||CandidateResolution>192||MaximumSettlementsPerCivilization<1||MaximumSettlementsPerCivilization>32||!CivilIdentity.Finite(MinimumSpacingFraction)||MinimumSpacingFraction<.01||MinimumSpacingFraction>.4)throw new ArgumentException("Invalid civilization generation configuration.");}
        public CivilizationSettings Copy()=>(CivilizationSettings)MemberwiseClone();
    }
    [Serializable] public sealed class CivicInventory
    {
        public double Food,Timber,Stone,Ore,Supplies;
        public CivicInventory Copy()=>(CivicInventory)MemberwiseClone();
        public void Validate(){foreach(double n in new[]{Food,Timber,Stone,Ore,Supplies})if(!CivilIdentity.Finite(n)||n<0||n>1e12)throw new ArgumentException("Invalid settlement resource inventory.");}
    }
    [Serializable] public sealed class CivilizationState
    {
        public string Id,Name,CultureId,CultureName,LanguageFamily,PoliticalId,CapitalSettlementId,LeaderId,LeaderName;
        public double OriginX,OriginZ,Red,Green,Blue,Population,ControlledSquareKilometres,Development,Cohesion=1;
        public PoliticalForm Government=PoliticalForm.TribalCouncil;
        public long FoundedTick;
        public List<string> Settlements=new List<string>();
        public CivilizationState Copy(){var c=(CivilizationState)MemberwiseClone();c.Settlements=new List<string>(Settlements);return c;}
    }
    [Serializable] public sealed class SettlementState
    {
        public string Id,Name,CivilizationId;
        public double X,Y,Z,Population,Capacity,Fertility,Freshwater,TimberPotential,StonePotential,OrePotential,ClimateSuitability;
        public double GrowthRate,LastBirths,LastDeaths,FoodSatisfaction=1,Prosperity=1,ScarcityMonths;
        public SettlementStage Stage;
        public long FoundedTick,LastDevelopmentTick,LastExpansionTick,LastMilestone;
        public bool InCrisis;
        public CivicInventory Resources=new CivicInventory();
        public List<CivicBuilding> Buildings=new List<CivicBuilding>();
        public long NextResidentNumber,ResidentDeaths;
        public List<ResidentState> Residents=new List<ResidentState>();
        public SettlementState Copy(){var s=(SettlementState)MemberwiseClone();s.Resources=Resources.Copy();s.Buildings=new List<CivicBuilding>();foreach(var b in Buildings)s.Buildings.Add(b.Copy());s.Residents=new List<ResidentState>();foreach(var r in Residents)s.Residents.Add(r.Copy());return s;}
    }
    [Serializable] public sealed class ResidentState
    {
        public string Id;public long Number;public double Health=100,X,Y,Z;public bool HasPosition;
        public ResidentState Copy()=>(ResidentState)MemberwiseClone();
    }
    [Serializable] public sealed class CivicBuilding
    {
        public string Id;public CivicBuildingKind Kind;public double X,Y,Z,Yaw;
        public int Variant,HousingCapacity;
        public CivicBuilding Copy()=>(CivicBuilding)MemberwiseClone();
    }
    [Serializable] public sealed class DiplomaticRelationship
    {
        public string A,B;public double Value,SharedBorderKilometres,Competition;public DiplomaticDisposition Disposition;
        public DiplomaticRelationship Copy()=>(DiplomaticRelationship)MemberwiseClone();
    }
    [Serializable] public sealed class SocialHistoryEvent
    {
        public long Sequence,Tick;public string Id,Title,Description,CivilizationId,SettlementId,OtherCivilizationId;
        public SocialEventKind Kind;public double X,Z,Before,After;
        public SocialHistoryEvent Copy()=>(SocialHistoryEvent)MemberwiseClone();
    }
    [Serializable] public sealed class SocialHistoryPage
    {
        public string Key,Sha256;public long First,Last;public int Count;
        public SocialHistoryPage Copy()=>(SocialHistoryPage)MemberwiseClone();
    }
    public interface ICivilizationHistoryArchive
    {
        SocialHistoryPage Store(IReadOnlyList<SocialHistoryEvent> events);
        IReadOnlyList<SocialHistoryEvent> Read(SocialHistoryPage page);
    }
    [Serializable] public sealed class CivilizationSnapshot
    {
        public int Version=CivilizationModel.Version,TerritoryResolution;
        public string Seed;public long Tick,EpochTick,Months,NextEventSequence=1;
        public ulong RandomState;
        public CivilizationSettings Settings=new CivilizationSettings();
        public List<CivilizationState> Civilizations=new List<CivilizationState>();
        public List<SettlementState> Settlements=new List<SettlementState>();
        public List<DiplomaticRelationship> Relationships=new List<DiplomaticRelationship>();
        public List<SocialHistoryEvent> RecentHistory=new List<SocialHistoryEvent>();
        public List<SocialHistoryPage> HistoryPages=new List<SocialHistoryPage>();
        // -1 is neutral; other values index the stable civilization list.
        public int[] Territory;
        public CivilizationSnapshot Copy()
        {
            var s=(CivilizationSnapshot)MemberwiseClone();s.Settings=Settings.Copy();s.Territory=Territory==null?null:(int[])Territory.Clone();
            s.Civilizations=new List<CivilizationState>();foreach(var c in Civilizations)s.Civilizations.Add(c.Copy());
            s.Settlements=new List<SettlementState>();foreach(var p in Settlements)s.Settlements.Add(p.Copy());
            s.Relationships=new List<DiplomaticRelationship>();foreach(var r in Relationships)s.Relationships.Add(r.Copy());
            s.RecentHistory=new List<SocialHistoryEvent>();foreach(var e in RecentHistory)s.RecentHistory.Add(e.Copy());
            s.HistoryPages=new List<SocialHistoryPage>();foreach(var p in HistoryPages)s.HistoryPages.Add(p.Copy());return s;
        }
    }
    // Extension contracts only. No individual lives, trade market or army behavior
    // is inferred from the existence of these records.
    [Serializable] public sealed class CitizenIdentity {public string Id,Name,HouseholdId,SettlementId,Occupation,LeaderOf;public long BirthTick;public List<string> Parents=new List<string>();}
    [Serializable] public sealed class TradeConnection {public string Id,OriginSettlementId,DestinationSettlementId;public double GeographicCost;}
    [Serializable] public sealed class ArmyFoundation {public string Id,CivilizationId,TargetId,CampaignId;public double X,Z,Strength;public List<GeoCoordinate> MovementOrders=new List<GeoCoordinate>();}
    [Serializable] public sealed class GeoCoordinate {public double X,Z;}

    public static class CivilIdentity
    {
        public static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
        public static double Clamp(double n,double low=0,double high=1)=>Math.Max(low,Math.Min(high,n));
        public static ulong Seed(string seed){ulong h=14695981039346656037UL;foreach(char c in seed){h^=c;h*=1099511628211UL;}return h==0?1:h;}
        public static double Next(ref ulong state){ulong x=state;x^=x>>12;x^=x<<25;x^=x>>27;state=x==0?1:x;return ((state*2685821657736338717UL)>>11)*(1.0/(1UL<<53));}
        public static string Id(string scope,string key){using(var sha=SHA256.Create()){byte[] bytes=sha.ComputeHash(Encoding.UTF8.GetBytes(scope+"/society-1/"+key));var guid=new byte[16];Array.Copy(bytes,guid,16);return new Guid(guid).ToString("N");}}
        public static string Scope(ProceduralGeography g)=>g.Configuration.Seed+"/"+g.Configuration.WidthMeters.ToString("R",CultureInfo.InvariantCulture)+"/"+g.Configuration.DepthMeters.ToString("R",CultureInfo.InvariantCulture)+"/civilization-module/"+g.CivilizationSites.Seed.ToString(CultureInfo.InvariantCulture);
    }
}
