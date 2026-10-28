using System;
using System.Collections.Generic;
namespace Aevoria.World
{
    // Input triangles come from a one-metre grid aligned with the 16 m volume
    // columns. Clip vertically, rather than removing unrelated ground above a
    // cave or leaving overlapping triangles across a cube's horizontal face.
    public static class HeightfieldSupportClipper
    {
        public static void AppendOutside(SolidPoint a,SolidPoint b,SolidPoint c,Func<int,int,int,bool> owned,List<SolidPoint> vertices,List<int> indices)
        {
            int x=(int)Math.Floor((a.X+b.X+c.X)/48),z=(int)Math.Floor((a.Z+b.Z+c.Z)/48);
            int first=(int)Math.Floor(Math.Min(a.Y,Math.Min(b.Y,c.Y))/16),last=(int)Math.Floor(Math.Max(a.Y,Math.Max(b.Y,c.Y))/16);
            Span<SolidPoint> input=stackalloc SolidPoint[8];Span<SolidPoint> output=stackalloc SolidPoint[8];
            for(int y=first;y<=last;y++)
            {
                if(owned(x,y,z))continue;input[0]=a;input[1]=b;input[2]=c;int count=ClipY(input.Slice(0,3),output,y*16,true);count=ClipY(output.Slice(0,count),input,(y+1)*16,false);
                for(int i=1;i<count-1;i++)
                {
                    var p=input[0];var q=input[i];var r=input[i+1];double ax=q.X-p.X,ay=q.Y-p.Y,az=q.Z-p.Z,bx=r.X-p.X,by=r.Y-p.Y,bz=r.Z-p.Z;
                    double nx=ay*bz-az*by,ny=az*bx-ax*bz,nz=ax*by-ay*bx;if(nx*nx+ny*ny+nz*nz<1e-18)continue;int n=vertices.Count;vertices.Add(p);vertices.Add(q);vertices.Add(r);indices.Add(n);indices.Add(n+1);indices.Add(n+2);
                }
            }
        }
        private static int ClipY(ReadOnlySpan<SolidPoint> source,Span<SolidPoint> target,double plane,bool above)
        {
            int count=0;if(source.Length==0)return 0;
            for(int i=0;i<source.Length;i++)
            {
                var a=source[i];var b=source[(i+1)%source.Length];double da=above?a.Y-plane:plane-a.Y,db=above?b.Y-plane:plane-b.Y;
                if(da>=0)target[count++]=a;if((da<0&&db>=0)||(da>=0&&db<0))target[count++]=SolidPoint.Mix(a,b,da/(da-db));
            }
            return count;
        }
    }
}
