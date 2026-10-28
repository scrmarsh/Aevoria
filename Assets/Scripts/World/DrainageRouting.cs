using System;
using System.Threading;
namespace Aevoria.World
{
    public static class DrainageRouting
    {
        // Priority flooding establishes spill elevations and an acyclic route
        // across flats. On a real downhill slope, flow follows its steepest
        // descent, rather than the first neighbour reached by the flood queue.
        public static void RefineDownhillReceivers(float[] levels,int[] receivers,int columns,int rows,double stepX,double stepZ,CancellationToken token=default,Action<string,long,long> report=null)
        {
            if(levels==null||receivers==null||columns<2||rows<2||levels.Length!=columns*rows||receivers.Length!=levels.Length||stepX<=0||stepZ<=0)throw new ArgumentException("Invalid drainage raster");
            for(int z=0;z<rows;z++)
            {
                token.ThrowIfCancellationRequested();report?.Invoke("Following steepest downhill drainage",z,rows);
                for(int x=0;x<columns;x++)
                {
                    int node=z*columns+x,best=receivers[node];if(best<0)continue;
                    double bx=(best%columns-x)*stepX,bz=(best/columns-z)*stepZ;
                    double grade=(levels[node]-levels[best])/Math.Max(.0001,Math.Sqrt(bx*bx+bz*bz));
                    for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                    {
                        int xx=x+dx,zz=z+dz;if((dx==0&&dz==0)||xx<0||zz<0||xx>=columns||zz>=rows)continue;
                        int candidate=zz*columns+xx;if(levels[candidate]>=levels[node])continue;
                        double drop=(levels[node]-levels[candidate])/Math.Sqrt(dx*dx*stepX*stepX+dz*dz*stepZ*stepZ);
                        if(drop>grade+Math.Max(1e-10,Math.Abs(grade)*1e-6)){best=candidate;grade=drop;}
                    }
                    receivers[node]=best;
                }
            }
        }
    }
}
