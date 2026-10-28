using System;
using System.Collections.Generic;

namespace Aevoria.World
{
    public sealed partial class CivilizationModel
    {
        private void MonthlyStep()
        {
            foreach(var s in state.Settlements)
            {
                double previous=s.Population,yearPhase=(state.Tick/4.0%360)/360*2*Math.PI;
                double latitude=geography.Climate.LatitudeAt(s.Z),winter=Math.Sin(yearPhase)*(latitude<0?-1:1);
                double seasonal=CivilIdentity.Clamp(1+winter*Math.Min(.35,Math.Abs(latitude)/180),.55,1.25);
                double localLimit=350+s.Fertility*4200+s.Freshwater*2400;
                int dwellings=0;foreach(var b in s.Buildings)if(b.Kind==CivicBuildingKind.Dwelling)dwellings++;
                double stageCapacity=s.Stage==SettlementStage.Camp?180:s.Stage==SettlementStage.Village?900:s.Stage==SettlementStage.Town?3500:12000;
                double housingCapacity=HousingCapacity(s);s.Capacity=Math.Max(40,Math.Min(localLimit,Math.Min(stageCapacity,housingCapacity)));
                double labor=previous*.55,pressure=CivilIdentity.Clamp(1-previous/(localLimit*1.5),.2,1);
                double food=priorProduction(labor,s.Fertility,s.Freshwater,s.ClimateSuitability,seasonal,pressure);
                s.Resources.Food=Math.Min(previous*18+100,s.Resources.Food+food);
                double demand=previous,consumed=Math.Min(demand,s.Resources.Food);s.Resources.Food-=consumed;s.FoodSatisfaction=demand<.000001?1:consumed/demand;
                s.Resources.Timber=Math.Min(1000000,s.Resources.Timber+labor*s.TimberPotential*.035);
                s.Resources.Stone=Math.Min(1000000,s.Resources.Stone+labor*s.StonePotential*.025);
                s.Resources.Ore=Math.Min(1000000,s.Resources.Ore+labor*s.OrePotential*.008*((int)s.Stage+1));
                double supplies=Math.Min(Math.Max(0,20+(int)s.Stage*20-s.Resources.Supplies),Math.Min(labor*.008,Math.Min(s.Resources.Timber*.08,s.Resources.Stone*.15)));s.Resources.Timber-=supplies*.5;s.Resources.Stone-=supplies*.25;s.Resources.Supplies+=supplies;
                double reserve=CivilIdentity.Clamp(s.Resources.Food/Math.Max(1,previous*2));
                s.LastBirths=previous*.004*s.ClimateSuitability*s.FoodSatisfaction*reserve*CivilIdentity.Clamp(1-previous/s.Capacity);
                s.LastDeaths=previous*(.00055+(1-s.FoodSatisfaction)*.05+CivilIdentity.Clamp(previous/s.Capacity-1)*.004);
                s.Population=CivilIdentity.Clamp(previous+s.LastBirths-s.LastDeaths,0,1000000);s.GrowthRate=previous<.001?0:(s.Population-previous)/previous;
                s.Prosperity=CivilIdentity.Clamp(s.Prosperity*.9+(s.FoodSatisfaction*.6+reserve*.4)*.1);
                s.ScarcityMonths=s.FoodSatisfaction<.9?s.ScarcityMonths+1:Math.Max(0,s.ScarcityMonths-1);
                var civ=Civilization(s.CivilizationId);
                if(s.ScarcityMonths>=2&&!s.InCrisis){s.InCrisis=true;Emit(SocialEventKind.ResourceCrisis,civ,s,"Food crisis in "+s.Name,FormattableString.Invariant($"Food covered {s.FoodSatisfaction:P0} of monthly needs; unsupported population is declining."),previous,s.Population);}
                if(s.InCrisis&&s.ScarcityMonths==0&&reserve>.5){s.InCrisis=false;Emit(SocialEventKind.Recovery,civ,s,s.Name+" recovers","Food supply and reserves recovered after the shortage.",0,s.Resources.Food);}
                long milestone=(long)(s.Population/100);if(milestone>s.LastMilestone){Emit(SocialEventKind.PopulationMilestone,civ,s,s.Name+" reaches "+milestone*100,FormattableString.Invariant($"Population reached {s.Population:0} through supported aggregate growth."),previous,s.Population);s.LastMilestone=milestone;}
                if(state.Months%12==0&&s.GrowthRate<-.005)Emit(SocialEventKind.Decline,civ,s,s.Name+" in decline","Population declined under the recorded food/capacity conditions.",previous,s.Population);
                if(!s.InCrisis&&reserve>.5)
                {
                    double threshold=s.Stage==SettlementStage.Camp?120:s.Stage==SettlementStage.Village?500:s.Stage==SettlementStage.Town?2000:double.MaxValue;
                    double cost=1+(int)s.Stage;
                    if(s.Population>=threshold&&state.Tick-s.LastDevelopmentTick>=TicksPerMonth*6&&Spend(s.Resources,0,45*cost,24*cost,(int)s.Stage>=1?8*cost:0,10*cost))
                    {
                        var before=s.Stage;s.Stage++;s.LastDevelopmentTick=state.Tick;Emit(SocialEventKind.Development,civ,s,s.Name+" becomes a "+s.Stage,$"Population and paid timber, stone, ore and supply investment supported development from {before} to {s.Stage}.",(int)before,(int)s.Stage);
                        if(s.Stage>=SettlementStage.Town&&civ.Government==PoliticalForm.TribalCouncil){civ.Government=PoliticalForm.CityState;Emit(SocialEventKind.PoliticalTransition,civ,s,civ.Name+" forms a city-state","A developed urban settlement now anchors the polity; leadership remains the recorded founding council.",(int)PoliticalForm.TribalCouncil,(int)PoliticalForm.CityState);}
                    }
                    if(s.Population>s.Capacity*.7&&housingCapacity<Math.Min(localLimit,stageCapacity)&&s.Buildings.Count<248)Construct(s,CivicBuildingKind.Dwelling,false);
                    if(state.Months%6==0)
                    {
                        int services=0;foreach(var building in s.Buildings)if(building.Kind!=CivicBuildingKind.Dwelling)services++;
                        if(services<6+(int)s.Stage*2&&s.Buildings.Count<248)
                        {
                            var options=new[]{CivicBuildingKind.Farm,CivicBuildingKind.Workshop,CivicBuildingKind.Storehouse,CivicBuildingKind.Market};
                            var kind=options[(services-2+options.Length)%options.Length];Construct(s,kind,false);
                        }
                    }
                }
            }
            if(state.Months%12==0){MigrationAndExchange();ExpandSettlements();UpdateTotals();RebuildTerritory(true);UpdateRelationships();}
            foreach(var settlement in state.Settlements)SynchronizeResidents(settlement);
        }
        private static double priorProduction(double labor,double fertility,double water,double climate,double seasonal,double pressure)=>labor*2.45*(.55+fertility*.65+water*.3)*CivilIdentity.Clamp(climate,.25,1)*seasonal*pressure;
        private static bool Spend(CivicInventory r,double food,double timber,double stone,double ore,double supplies)
        {
            if(r.Food+1e-9<food||r.Timber+1e-9<timber||r.Stone+1e-9<stone||r.Ore+1e-9<ore||r.Supplies+1e-9<supplies)return false;
            r.Food=Math.Max(0,r.Food-food);r.Timber=Math.Max(0,r.Timber-timber);r.Stone=Math.Max(0,r.Stone-stone);r.Ore=Math.Max(0,r.Ore-ore);r.Supplies=Math.Max(0,r.Supplies-supplies);return true;
        }
        public static int HousingCapacity(SettlementState settlement){int capacity=0;foreach(var building in settlement.Buildings)capacity+=building.HousingCapacity;return capacity;}
        private bool Construct(SettlementState s,CivicBuildingKind kind,bool founding)
        {
            int ordinal=s.Buildings.Count,variant=ordinal%3;double timber=kind==CivicBuildingKind.Dwelling?4:kind==CivicBuildingKind.MeetingHall?8:6,stone=timber*.5,supplies=kind==CivicBuildingKind.MeetingHall?2:1;
            if(s.Resources.Timber<timber||s.Resources.Stone<stone||s.Resources.Supplies<supplies)return false;
            double x=0,z=0,y=0;bool found=false;
            for(int attempt=0;attempt<24;attempt++)
            {
                int slot=ordinal+attempt;double angle=slot*2.399963229728653,radius=slot==0?0:9+Math.Sqrt(slot)*10;
                x=s.X+Math.Cos(angle)*radius;z=s.Z+Math.Sin(angle)*radius;bool separated=true;foreach(var b in s.Buildings)if(Distance(x,z,b.X,b.Z)<13){separated=false;break;}
                if(separated&&CivilizationGeography.Footprint(geography,x,z,6,out y)){found=true;break;}
            }
            if(!found||!Spend(s.Resources,0,timber,stone,0,supplies))return false;
            s.Buildings.Add(new CivicBuilding{Id=CivilIdentity.Id(s.Id,"building/"+ordinal),Kind=kind,Variant=variant,HousingCapacity=kind==CivicBuildingKind.Dwelling?(variant==0?6:variant==1?8:12):0,X=x,Y=y,Z=z,Yaw=(ordinal*137.5)%360});Revision++;return true;
        }
        private readonly Dictionary<string,bool> connections=new Dictionary<string,bool>();
        public bool Connected(SettlementState a,SettlementState b)
        {
            string key=string.CompareOrdinal(a.Id,b.Id)<0?a.Id+"/"+b.Id:b.Id+"/"+a.Id;if(connections.TryGetValue(key,out bool cached))return cached;
            bool okay=OverlandConnection(a,b);connections[key]=okay;return okay;
        }
        private bool OverlandConnection(SettlementState a,SettlementState b)
        {
            double distance=Distance(a.X,a.Z,b.X,b.Z);bool okay=distance<=160000;
            int steps=Math.Max(8,Math.Min(160,(int)Math.Ceiling(distance/1000)));
            for(int i=1;i<steps&&okay;i++){double x=a.X+(b.X-a.X)*i/steps,z=a.Z+(b.Z-a.Z)*i/steps;float h=geography.ElevationAt(x,z);okay=h>geography.SeaLevel+2&&h<2600&&!geography.InlandSurfaces.LakeAt(x,z,out _);}
            return okay;
        }
        public bool TransferFood(string from,string to,double amount)
        {
            var a=Settlement(from);var b=Settlement(to);if(a==null||b==null||a==b||a.CivilizationId!=b.CivilizationId||!CivilIdentity.Finite(amount)||amount<=0||a.Resources.Food<amount||b.Resources.Food+amount>1e12||!Connected(a,b))return false;
            a.Resources.Food-=amount;b.Resources.Food+=amount;Revision++;return true;
        }
        private void MigrationAndExchange()
        {
            foreach(var a in state.Settlements)
            {
                SettlementState best=null;double score=0;
                foreach(var b in state.Settlements)if(a!=b&&a.CivilizationId==b.CivilizationId&&b.FoodSatisfaction>.98&&b.Population<b.Capacity*.85&&Connected(a,b))
                {double candidate=(b.Prosperity-a.Prosperity)+Math.Max(0,a.Population/a.Capacity-.85);if(candidate>score+.05){best=b;score=candidate;}}
                if(best!=null&&a.Population>30){double amount=Math.Min(a.Population*.012,Math.Max(0,best.Capacity*.85-best.Population));double before=a.Population;a.Population-=amount;best.Population+=amount;Emit(SocialEventKind.Migration,Civilization(a.CivilizationId),a,"Migration from "+a.Name,FormattableString.Invariant($"{amount:0.0} people moved to {best.Name}; the transfer conserves total population."),before,a.Population);}
                foreach(var b in state.Settlements)if(a!=b&&a.CivilizationId==b.CivilizationId&&a.Resources.Food>a.Population*4&&b.Resources.Food<b.Population&&Connected(a,b)){double amount=Math.Min(a.Resources.Food-a.Population*4,b.Population*2-b.Resources.Food);TransferFood(a.Id,b.Id,amount);}
            }
        }
        private void ExpandSettlements()
        {
            var founders=new List<SettlementState>(state.Settlements);
            foreach(var parent in founders)
            {
                var civ=Civilization(parent.CivilizationId);if(civ.Settlements.Count>=state.Settings.MaximumSettlementsPerCivilization||parent.Stage<SettlementStage.Village||parent.Population<420||parent.InCrisis||state.Tick-parent.LastExpansionTick<120*24||parent.Resources.Food<parent.Population*4||parent.Resources.Timber<160||parent.Resources.Stone<80||parent.Resources.Supplies<30)continue;
                if(candidateSites==null)candidateSites=CivilizationGeography.Candidates(geography,state.Settings);
                foreach(var site in candidateSites)
                {
                    double distance=Distance(parent.X,parent.Z,site.X,site.Z);if(distance<4000||distance>120000)continue;bool clear=true;foreach(var s in state.Settlements)if(Distance(s.X,s.Z,site.X,site.Z)<Math.Max(2500,geography.Configuration.WidthMeters/1000)){clear=false;break;}if(!clear)continue;
                    int owner=OwnerAt(site.X,site.Z),own=state.Civilizations.IndexOf(civ);if(owner>=0&&owner!=own)continue;
                    // Candidate locations share the future child's identity. A failed
                    // route to one location must not poison every subsequent candidate.
                    var probe=new SettlementState{X=site.X,Z=site.Z};if(!OverlandConnection(parent,probe))continue;
                    // All founding stock comes out of the parent. The child pays
                    // construction from that transferred inventory.
                    var stores=new CivicInventory{Food=180,Timber=60,Stone=30,Supplies=12};if(!Spend(parent.Resources,stores.Food,stores.Timber,stores.Stone,0,stores.Supplies))break;
                    parent.Population-=60;parent.LastExpansionTick=state.Tick;Found(civ,site,60,civ.Settlements.Count,stores);break;
                }
            }
        }
    }
}
