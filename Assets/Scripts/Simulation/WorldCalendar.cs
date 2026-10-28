using System;
namespace Aevoria.Simulation
{
    public readonly struct WorldCalendar
    {
        public const int Version=1, TicksPerDay=4, DaysPerYear=360;
        public readonly long Year;
        public readonly int Day, Season;
        public WorldCalendar(long tick)
        {
            if(tick<0) throw new ArgumentOutOfRangeException(nameof(tick));
            long days=tick/TicksPerDay; Year=1+days/DaysPerYear; Day=1+(int)(days%DaysPerYear); Season=(Day-1)/90;
        }
        public string SeasonName => new[]{"Spring","Summer","Autumn","Winter"}[Season];
    }
}
