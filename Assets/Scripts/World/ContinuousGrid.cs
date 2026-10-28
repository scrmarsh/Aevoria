using System;
namespace Aevoria.World
{
    // Shape-preserving cubic interpolation. Shared node tangents remove the
    // bilinear raster's slope jumps without introducing overshooting summits.
    public static class ContinuousGrid
    {
        public static double Curve(double a,double b,double c,double d,double t)
        {
            double delta=c-b;
            double m0=Tangent(b-a,delta),m1=Tangent(delta,d-c),t2=t*t,t3=t2*t;
            return (2*t3-3*t2+1)*b+(t3-2*t2+t)*m0+(-2*t3+3*t2)*c+(t3-t2)*m1;
        }
        private static double Tangent(double a,double b)=>a*b<=0?0:2*a*b/(a+b);
        public static float Sample(float[] field,int n,double x,double z)
        {
            x=Math.Max(0,Math.Min(n-1,x));z=Math.Max(0,Math.Min(n-1,z));
            int ix=Math.Min(n-2,(int)x),iz=Math.Min(n-2,(int)z);double tx=x-ix,tz=z-iz;
            double Row(int row)
            {int at=Math.Max(0,Math.Min(n-1,row))*n;return Curve(field[at+Math.Max(0,ix-1)],field[at+ix],field[at+ix+1],field[at+Math.Min(n-1,ix+2)],tx);}
            return (float)Curve(Row(iz-1),Row(iz),Row(iz+1),Row(iz+2),tz);
        }
    }
}
