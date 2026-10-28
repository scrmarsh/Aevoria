using System;
namespace Aevoria.Core
{
    public enum UnitSystem{Metric,Imperial}
    public static class UnitFormatting
    {
        public static string Area(double squareKilometres,UnitSystem units)=>(squareKilometres/(units==UnitSystem.Imperial?2.589988110336:1)).ToString("N0")+(units==UnitSystem.Imperial?" mi²":" km²");
        public static double LengthValue(double metres,UnitSystem units)=>units==UnitSystem.Imperial?metres/0.3048:metres;
        public static double TemperatureValue(double celsius,UnitSystem units)=>units==UnitSystem.Imperial?celsius*1.8+32:celsius;
        public static double SpeedValue(double metresPerSecond,UnitSystem units)=>metresPerSecond*(units==UnitSystem.Imperial?2.2369362920544:3.6);
        public static string Length(double metres,UnitSystem units,string format="N0")=>LengthValue(metres,units).ToString(format)+(units==UnitSystem.Imperial?" ft":" m");
        public static string Distance(double metres,UnitSystem units,string format="N1")=>Math.Abs(metres)<1000?Length(metres,units,format):(metres/(units==UnitSystem.Imperial?1609.344:1000)).ToString(format)+(units==UnitSystem.Imperial?" mi":" km");
        public static string Temperature(double celsius,UnitSystem units,string format="0.0")=>TemperatureValue(celsius,units).ToString(format)+(units==UnitSystem.Imperial?" °F":" °C");
        public static string TemperatureDifference(double degrees,UnitSystem units)=> (degrees*(units==UnitSystem.Imperial?1.8:1)).ToString("0.0")+(units==UnitSystem.Imperial?" °F":" °C");
        public static string Speed(double metresPerSecond,UnitSystem units)=>SpeedValue(metresPerSecond,units).ToString("0.0")+(units==UnitSystem.Imperial?" mph":" km/h");
        public static string Density(double perSquareKilometre,UnitSystem units)=> (perSquareKilometre*(units==UnitSystem.Imperial?2.589988110336:1)).ToString("0.0")+(units==UnitSystem.Imperial?" /mi²":" /km²");
    }
}
