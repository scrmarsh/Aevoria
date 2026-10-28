using System;
namespace Aevoria.World
{
    // Climate support for perennial snow and ice. Seasonal accumulation remains
    // the persisted, time-integrated SeasonalModel; elevation enters through the
    // climate's lapse-rate temperature rather than a universal altitude stamp.
    public static class CryosphereClimate
    {
        private static double Smooth(double a,double b,double x)
        {double t=Math.Max(0,Math.Min(1,(x-a)/(b-a)));return t*t*(3-2*t);}
        public static float SeasonAmplitude(double latitude,double continentality)
            =>(float)(Math.Min(24,Math.Abs(latitude)*.28)*(.5+Math.Max(0,Math.Min(1,continentality))*.5));
        public static float PerennialSnow(double meanTemperature,double amplitude,double precipitation)
            =>(float)((1-Smooth(-5,2,meanTemperature+amplitude))*Smooth(.08,.40,precipitation));
        public static float PerennialIce(double meanTemperature,double amplitude)
            =>(float)(1-Smooth(-6,-1,meanTemperature+amplitude));
    }
}
