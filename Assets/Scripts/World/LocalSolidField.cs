using System;
using System.Collections.Generic;
using System.Threading;
namespace Aevoria.World
{
    public readonly struct SolidPoint
    {
        public readonly double X,Y,Z;
        public SolidPoint(double x,double y,double z){X=x;Y=y;Z=z;}
        public static SolidPoint Mix(SolidPoint a,SolidPoint b,double t)=>new SolidPoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,a.Z+(b.Z-a.Z)*t);
    }
    public sealed class SolidGeometry
    {
        public readonly List<SolidPoint> Vertices=new List<SolidPoint>(),Normals=new List<SolidPoint>();
        public readonly List<int> Indices=new List<int>();
        public readonly List<float> SurfaceDepth=new List<float>();
    }
    // A real positive-solid scalar field, with spherical subtraction below its
    // surface. It can represent a bounded cavity; it is not a lowered height map.
    public sealed class LocalSolidField
    {
        private readonly ProceduralGeography geo;private readonly ExcavationSite site;
        private double[] cachedHeights;private double cacheX,cacheZ;private int cacheN;private double cacheScale;
        public LocalSolidField(ProceduralGeography geo,ExcavationSite site){this.geo=geo;this.site=site;}
        public static double RenderedGround(ProceduralGeography geo,double x,double z)
        {
            return GroundContactPatch.SurfaceHeight(geo,geo.Configuration.CellSizeMeters,x,z);
        }
        public double Density(double x,double y,double z)
        {
            double solid=(cachedHeights==null?RenderedGround(geo,x,z):CachedHeight(x,z))-y;
            foreach(var c in site.Cuts){double dx=x-c.X,dy=(y-c.Y)*1.12,dz=z-c.Z;solid=Math.Min(solid,Math.Sqrt(dx*dx+dy*dy+dz*dz)-c.Radius);}
            return solid;
        }
        private double DensityAtHeight(double x,double y,double z,double height)
        {double solid=height-y;foreach(var c in site.Cuts){double dx=x-c.X,dy=(y-c.Y)*1.12,dz=z-c.Z;solid=Math.Min(solid,Math.Sqrt(dx*dx+dy*dy+dz*dz)-c.Radius);}return solid;}
        private double CachedHeight(double x,double z)
        {
            double gx=Math.Max(0,Math.Min(cacheN,(x-cacheX)*cacheScale)),gz=Math.Max(0,Math.Min(cacheN,(z-cacheZ)*cacheScale));int ix=Math.Min(cacheN-1,(int)gx),iz=Math.Min(cacheN-1,(int)gz);double tx=gx-ix,tz=gz-iz;int row=cacheN+1,i=iz*row+ix;
            double a=cachedHeights[i],b=cachedHeights[i+1],c=cachedHeights[i+row],d=cachedHeights[i+row+1];return tx+tz<=1?a+(b-a)*tx+(c-a)*tz:d+(c-d)*(1-tx)+(b-d)*(1-tz);
        }
        public SolidGeometry Build(CancellationToken token=default)
        {
            const int n=128;const double step=.25;int ny=(int)Math.Ceiling((site.Top-site.Bottom)/step);
            double ox=site.X*32-16,oz=site.Z*32-16;var heights=new double[(n+1)*(n+1)];
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)heights[z*(n+1)+x]=RenderedGround(geo,ox+x*step,oz+z*step);
            cachedHeights=heights;cacheX=ox;cacheZ=oz;cacheN=n;cacheScale=1/step;
            var mesh=new SolidGeometry();var points=new SolidPoint[8];var density=new double[8];var tet=new int[,]{{0,1,3,7},{0,3,2,7},{0,2,6,7},{0,6,4,7},{0,4,5,7},{0,5,1,7}};
            var positive=new int[4];var negative=new int[4];
            for(int z=0;z<n;z++)
            {token.ThrowIfCancellationRequested();for(int x=0;x<n;x++)
                {
                    double min=double.MaxValue,max=double.MinValue;
                    for(int k=0;k<4;k++){double h=heights[(z+(k>>1))*(n+1)+x+(k&1)];min=Math.Min(min,h);max=Math.Max(max,h);}
                    foreach(var cut in site.Cuts)if(Math.Abs(cut.X-(ox+(x+.5)*step))<cut.Radius+step&&Math.Abs(cut.Z-(oz+(z+.5)*step))<cut.Radius+step)min=Math.Min(min,cut.Y-cut.Radius-1);
                    int lo=Math.Max(0,(int)Math.Floor((min-site.Bottom-1)/step)),hi=Math.Min(ny-1,(int)Math.Ceiling((max-site.Bottom+1)/step));
                    for(int y=lo;y<=hi;y++)
                    {
                        for(int k=0;k<8;k++){double px=ox+(x+(k&1))*step,py=site.Bottom+(y+((k>>1)&1))*step,pz=oz+(z+(k>>2))*step;points[k]=new SolidPoint(px,py,pz);density[k]=DensityAtHeight(px,py,pz,heights[(z+(k>>2))*(n+1)+x+(k&1)]);}
                        for(int t=0;t<6;t++)
                        {
                            int pc=0,nc=0;for(int k=0;k<4;k++){int v=tet[t,k];if(density[v]>=0)positive[pc++]=v;else negative[nc++]=v;}
                            if(pc==0||pc==4)continue;
                            // Winding follows the interpolated tetrahedron, not
                            // a nonlinear vertex gradient at a sharp cut rim.
                            double dx=0,dy=0,dz=0;for(int k=0;k<nc;k++){var q=points[negative[k]];dx+=q.X/nc;dy+=q.Y/nc;dz+=q.Z/nc;}for(int k=0;k<pc;k++){var q=points[positive[k]];dx-=q.X/pc;dy-=q.Y/pc;dz-=q.Z/pc;}var outward=new SolidPoint(dx,dy,dz);
                            if(pc==1){int a=positive[0];Triangle(mesh,Intersect(points,density,a,negative[0]),Intersect(points,density,a,negative[1]),Intersect(points,density,a,negative[2]),outward);}
                            else if(pc==3){int a=negative[0];Triangle(mesh,Intersect(points,density,a,positive[0]),Intersect(points,density,a,positive[1]),Intersect(points,density,a,positive[2]),outward);}
                            else{int a=positive[0],b=positive[1],c=negative[0],d=negative[1];var ac=Intersect(points,density,a,c);var ad=Intersect(points,density,a,d);var bc=Intersect(points,density,b,c);var bd=Intersect(points,density,b,d);Triangle(mesh,ac,ad,bd,outward);Triangle(mesh,ac,bd,bc,outward);}
                        }
                    }
                }}
            return Index(mesh);
        }
        // Exact shared vertices are generated repeatedly by neighbouring
        // tetrahedra. Index on the worker, before native upload/collider cooking.
        // Quantisation is only a lookup key: the first original position and
        // analytic normal are retained, and triangle order/winding are unchanged.
        private static SolidGeometry Index(SolidGeometry source)
        {
            var result=new SolidGeometry();var vertices=new Dictionary<(long,long,long),int>();
            foreach(int original in source.Indices)
            {
                var p=source.Vertices[original];var key=((long)Math.Round(p.X*1000000),(long)Math.Round(p.Y*1000000),(long)Math.Round(p.Z*1000000));
                if(!vertices.TryGetValue(key,out int index))
                {index=result.Vertices.Count;vertices.Add(key,index);result.Vertices.Add(p);result.Normals.Add(source.Normals[original]);result.SurfaceDepth.Add(source.SurfaceDepth[original]);}
                result.Indices.Add(index);
            }
            return result;
        }
        private static SolidPoint Intersect(SolidPoint[] p,double[] d,int a,int b)=>SolidPoint.Mix(p[a],p[b],d[a]/(d[a]-d[b]));
        private SolidPoint Normal(SolidPoint p)
        {const double e=.04;double x=Density(p.X-e,p.Y,p.Z)-Density(p.X+e,p.Y,p.Z),y=Density(p.X,p.Y-e,p.Z)-Density(p.X,p.Y+e,p.Z),z=Density(p.X,p.Y,p.Z-e)-Density(p.X,p.Y,p.Z+e),length=Math.Sqrt(x*x+y*y+z*z);return length>1e-10?new SolidPoint(x/length,y/length,z/length):new SolidPoint(0,1,0);}
        private void Triangle(SolidGeometry mesh,SolidPoint a,SolidPoint b,SolidPoint c,SolidPoint outward)
        {
            var na=Normal(a);var nb=Normal(b);var nc=Normal(c);double ux=b.X-a.X,uy=b.Y-a.Y,uz=b.Z-a.Z,vx=c.X-a.X,vy=c.Y-a.Y,vz=c.Z-a.Z;
            double cx=uy*vz-uz*vy,cy=uz*vx-ux*vz,cz=ux*vy-uy*vx;if(cx*cx+cy*cy+cz*cz<1e-20)return;
            bool reverse=cx*outward.X+cy*outward.Y+cz*outward.Z<0;
            int i=mesh.Vertices.Count;mesh.Vertices.Add(a);mesh.Vertices.Add(b);mesh.Vertices.Add(c);mesh.Normals.Add(na);mesh.Normals.Add(nb);mesh.Normals.Add(nc);mesh.SurfaceDepth.Add((float)System.Math.Max(0,CachedHeight(a.X,a.Z)-a.Y));mesh.SurfaceDepth.Add((float)System.Math.Max(0,CachedHeight(b.X,b.Z)-b.Y));mesh.SurfaceDepth.Add((float)System.Math.Max(0,CachedHeight(c.X,c.Z)-c.Y));mesh.Indices.Add(i);mesh.Indices.Add(i+(reverse?2:1));mesh.Indices.Add(i+(reverse?1:2));
        }
    }
}
