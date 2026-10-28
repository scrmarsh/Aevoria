using System;
namespace Aevoria.World
{
    // Triangular gradient basis. Unlike tensor value noise, it does not force
    // both spatial derivatives to zero along every Cartesian lattice line.
    public static class GradientField
    {
        public static double Sample(double x,double z,uint seed,uint channel)
        {
            const double f=.36602540378443864676,g=.21132486540518711775;
            double skew=(x+z)*f;int i=(int)Math.Floor(x+skew),j=(int)Math.Floor(z+skew);double unskew=(i+j)*g;
            double x0=x-(i-unskew),z0=z-(j-unskew);int ix=x0>z0?1:0,iz=1-ix;
            double Corner(int a,int b,double px,double pz)
            {
                double t=.5-px*px-pz*pz;if(t<=0)return 0;
                uint h;unchecked{h=seed^(uint)a*374761393u^(uint)b*668265263u^channel*2246822519u;h=(h^(h>>13))*1274126177u;h^=h>>16;}
                double dot;switch(h&7){case 0:dot=px+pz;break;case 1:dot=px-pz;break;case 2:dot=-px+pz;break;case 3:dot=-px-pz;break;case 4:dot=px;break;case 5:dot=-px;break;case 6:dot=pz;break;default:dot=-pz;break;}
                t*=t;return t*t*dot;
            }
            return Math.Max(-1,Math.Min(1,70*(Corner(i,j,x0,z0)+Corner(i+ix,j+iz,x0-ix+g,z0-iz+g)+Corner(i+1,j+1,x0-1+2*g,z0-1+2*g))));
        }
    }
}
