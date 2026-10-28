using System;
using System.Collections.Generic;
namespace Aevoria.World
{
    public sealed partial class CivilizationModel
    {
        // These stable identities represent a bounded subset of the aggregate
        // population. Streaming creates views, never additional inhabitants.
        public const int ResidentRosterLimit=32;
        private void SynchronizeResidents(SettlementState settlement)
        {
            int target=Math.Min(ResidentRosterLimit,(int)Math.Floor(settlement.Population));
            if(settlement.Residents.Count>target)settlement.Residents.RemoveRange(target,settlement.Residents.Count-target);
            while(settlement.Residents.Count<target)
            {
                long number=settlement.NextResidentNumber++;
                settlement.Residents.Add(new ResidentState{Id=CivilIdentity.Id(settlement.Id,"resident/"+number),Number=number});
            }
        }
        public bool DamageResident(string settlementId,string residentId,double damage)
        {
            if(!CivilIdentity.Finite(damage)||damage<=0||damage>1000)throw new ArgumentOutOfRangeException(nameof(damage));
            var settlement=Settlement(settlementId);var resident=settlement?.Residents.Find(r=>r.Id==residentId);if(resident==null)return false;
            resident.Health=Math.Max(0,resident.Health-damage);Revision++;
            if(resident.Health>0)return true;
            double before=settlement.Population;settlement.Residents.Remove(resident);settlement.Population=Math.Max(0,before-1);settlement.LastDeaths+=before-settlement.Population;settlement.ResidentDeaths++;
            UpdateTotals();Emit(SocialEventKind.ResidentKilled,Civilization(settlement.CivilizationId),settlement,"A resident was killed in "+settlement.Name,"Resident "+resident.Id+" was killed by the traveler. The recorded settlement and civilization population decreased by one.",before,settlement.Population);
            return true;
        }
        private static void ValidateResidents(SettlementState settlement,WorldConfiguration config)
        {
            if(settlement.Residents==null||settlement.NextResidentNumber<0||settlement.NextResidentNumber>1000000000||settlement.ResidentDeaths<0||settlement.ResidentDeaths>settlement.NextResidentNumber||settlement.Residents.Count>ResidentRosterLimit||settlement.Residents.Count>Math.Floor(settlement.Population))throw new ArgumentException("Invalid settlement resident roster.");
            var ids=new HashSet<string>();foreach(var resident in settlement.Residents)
            {
                if(resident==null||resident.Number<0||resident.Number>=settlement.NextResidentNumber||resident.Id!=CivilIdentity.Id(settlement.Id,"resident/"+resident.Number)||!ids.Add(resident.Id)||!CivilIdentity.Finite(resident.Health)||resident.Health<=0||resident.Health>100)throw new ArgumentException("Invalid resident identity or health.");
                if(resident.HasPosition&&(!CivilIdentity.Finite(resident.X)||!CivilIdentity.Finite(resident.Y)||!CivilIdentity.Finite(resident.Z)||Math.Abs(resident.Y)>LivingWorldState.MaximumTravelerAltitude||!config.Contains(new GeoPoint(resident.X,resident.Z))))throw new ArgumentException("Invalid resident location.");
            }
        }
        private static void ValidateHousing(CivicBuilding building)
        {
            if(building.Variant<0||building.Variant>2||building.HousingCapacity!=(building.Kind==CivicBuildingKind.Dwelling?(building.Variant==0?6:building.Variant==1?8:12):0))throw new ArgumentException("Invalid building variation or housing capacity.");
        }
    }
}
