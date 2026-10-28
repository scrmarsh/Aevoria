using System;
using System.Collections.Generic;
using System.Threading;
namespace Aevoria.World
{
    // One authoritative positive-solid field for terrain, natural cave air and
    // persisted excavation. A bounded cube is only a sampled representation.
    public sealed class VolumetricSolidField
    {
        public const int ChunkSize=16,Cells=32;public const double Step=.5;
        private readonly ProceduralGeography geo;private readonly CaveNetwork caves;private readonly DigCut[] cuts;
        private readonly Dictionary<(int,int,int),List<int>> cutBuckets=new Dictionary<(int,int,int),List<int>>();
        private double[] terrain;private double originX,originZ;private int terrainSide=5,terrainExtent=16;
        public VolumetricSolidField(ProceduralGeography geography,CaveNetwork network,IReadOnlyList<DigCut> edits=null)
        {
            geo=geography;caves=network;cuts=new DigCut[edits?.Count??0];
            for(int i=0;i<cuts.Length;i++)
            {
                var c=edits[i];cuts[i]=new DigCut{X=c.X,Y=c.Y,Z=c.Z,Radius=c.Radius,Material=c.Material};double r=c.Radius+.2;
                for(int z=(int)Math.Floor((c.Z-r)/4);z<=(int)Math.Floor((c.Z+r)/4);z++)for(int y=(int)Math.Floor((c.Y-r)/4);y<=(int)Math.Floor((c.Y+r)/4);y++)for(int x=(int)Math.Floor((c.X-r)/4);x<=(int)Math.Floor((c.X+r)/4);x++)
                {var key=(x,y,z);if(!cutBuckets.TryGetValue(key,out var list)){list=new List<int>();cutBuckets.Add(key,list);}list.Add(i);}
            }
        }
        private void PrepareTerrain(int x,int z,int extent=16)
        {
            originX=x*(double)extent;originZ=z*(double)extent;terrainExtent=extent;terrainSide=extent/8+3;terrain=new double[terrainSide*terrainSide];
            // These are the exact vertices of the existing 8 m triangular
            // surface. Reuse them instead of repeating geology per voxel.
            for(int iz=0;iz<terrainSide;iz++)for(int ix=0;ix<terrainSide;ix++)terrain[iz*terrainSide+ix]=geo.ElevationAt(originX+(ix-1)*8,originZ+(iz-1)*8);
        }
        public double GroundHeight(double x,double z)
        {
            if(terrain==null||x<originX-8||z<originZ-8||x>originX+terrainExtent+8||z>originZ+terrainExtent+8)return LocalSolidField.RenderedGround(geo,x,z);
            double gx=(x-originX+8)/8,gz=(z-originZ+8)/8;int ix=Math.Min(terrainSide-2,(int)gx),iz=Math.Min(terrainSide-2,(int)gz);double a=gx-ix,b=gz-iz;int i=iz*terrainSide+ix;
            return a+b<=1?terrain[i]*(1-a-b)+terrain[i+1]*a+terrain[i+terrainSide]*b:terrain[i+1]*(1-b)+terrain[i+terrainSide]*(1-a)+terrain[i+terrainSide+1]*(a+b-1);
        }
        public double Density(double x,double y,double z)
        {
            double density=GroundHeight(x,z)-y;if(caves!=null)density=Math.Min(density,caves.Distance(x,y,z));
            if(cutBuckets.TryGetValue(((int)Math.Floor(x/4),(int)Math.Floor(y/4),(int)Math.Floor(z/4)),out var candidates))foreach(int i in candidates)
            {var c=cuts[i];double dx=x-c.X,dy=y-c.Y,dz=z-c.Z;density=Math.Min(density,Math.Sqrt(dx*dx+dy*dy+dz*dz)-c.Radius);}
            return density;
        }
        public bool MayContainSurface(int x,int y,int z)
        {
            PrepareTerrain(x,z);double low=double.MaxValue,high=double.MinValue;
            for(int iz=1;iz<=3;iz++)for(int ix=1;ix<=3;ix++){low=Math.Min(low,terrain[iz*5+ix]);high=Math.Max(high,terrain[iz*5+ix]);}
            if(y*16<=high+.01&&(y+1)*16>=low-.01)return true;if(caves?.IntersectsChunk(x,y,z)==true)return true;
            foreach(var c in cuts){double dx=Math.Max(0,Math.Max(x*16-c.X,c.X-(x+1)*16)),dy=Math.Max(0,Math.Max(y*16-c.Y,c.Y-(y+1)*16)),dz=Math.Max(0,Math.Max(z*16-c.Z,c.Z-(z+1)*16));if(dx*dx+dy*dy+dz*dz<=(c.Radius+.2)*(c.Radius+.2))return true;}return false;
        }
        private SolidPoint Normal(SolidPoint p)
        {const double e=.06;double x=Density(p.X-e,p.Y,p.Z)-Density(p.X+e,p.Y,p.Z),y=Density(p.X,p.Y-e,p.Z)-Density(p.X,p.Y+e,p.Z),z=Density(p.X,p.Y,p.Z-e)-Density(p.X,p.Y,p.Z+e),length=Math.Sqrt(x*x+y*y+z*z);return length>1e-12?new SolidPoint(x/length,y/length,z/length):new SolidPoint(0,1,0);}
        public SolidGeometry Build(int chunkX,int chunkY,int chunkZ,CancellationToken token=default)=>BuildResolution(chunkX,chunkY,chunkZ,16,32,token);
        public SolidGeometry BuildResolution(int chunkX,int chunkY,int chunkZ,int extent,int cells,CancellationToken token=default)
        {
            if(extent<8||extent%8!=0||cells<4||cells>128)throw new ArgumentOutOfRangeException();
            double step=extent/(double)cells;
            PrepareTerrain(chunkX,chunkZ,extent);int n=cells,row=n+1,plane=row*row;double ox=chunkX*(double)extent,oy=chunkY*(double)extent,oz=chunkZ*(double)extent;var field=new double[row*row*row];
            for(int z=0;z<=n;z++){token.ThrowIfCancellationRequested();for(int y=0;y<=n;y++)for(int x=0;x<=n;x++)field[z*plane+y*row+x]=Density(ox+x*step,oy+y*step,oz+z*step);}
            var mesh=new SolidGeometry();var vertices=new Dictionary<(long,long,long),int>();var points=new SolidPoint[8];var d=new double[8];var positive=new int[4];var negative=new int[4];
            var tetrahedra=new int[,]{{0,1,3,7},{0,3,2,7},{0,2,6,7},{0,6,4,7},{0,4,5,7},{0,5,1,7}};
            int Vertex(SolidPoint p)
            {
                var key=((long)Math.Round((p.X-ox)*1000000),(long)Math.Round((p.Y-oy)*1000000),(long)Math.Round((p.Z-oz)*1000000));
                if(vertices.TryGetValue(key,out int id))return id;id=mesh.Vertices.Count;vertices.Add(key,id);mesh.Vertices.Add(p);mesh.Normals.Add(Normal(p));mesh.SurfaceDepth.Add((float)Math.Max(0,GroundHeight(p.X,p.Z)-p.Y));return id;
            }
            SolidPoint Intersect(int a,int b)=>SolidPoint.Mix(points[a],points[b],d[a]/(d[a]-d[b]));
            void Triangle(SolidPoint a,SolidPoint b,SolidPoint c,SolidPoint outward)
            {
                double ux=b.X-a.X,uy=b.Y-a.Y,uz=b.Z-a.Z,vx=c.X-a.X,vy=c.Y-a.Y,vz=c.Z-a.Z,cx=uy*vz-uz*vy,cy=uz*vx-ux*vz,cz=ux*vy-uy*vx;
                if(cx*cx+cy*cy+cz*cz<1e-18)return;int ia=Vertex(a),ib=Vertex(b),ic=Vertex(c);bool reverse=cx*outward.X+cy*outward.Y+cz*outward.Z<0;mesh.Indices.Add(ia);mesh.Indices.Add(reverse?ic:ib);mesh.Indices.Add(reverse?ib:ic);
            }
            for(int z=0;z<n;z++)
            {token.ThrowIfCancellationRequested();for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    int solid=0;for(int k=0;k<8;k++){int px=x+(k&1),py=y+((k>>1)&1),pz=z+(k>>2);points[k]=new SolidPoint(ox+px*step,oy+py*step,oz+pz*step);d[k]=field[pz*plane+py*row+px];if(d[k]>=0)solid++;}if(solid==0||solid==8)continue;
                    for(int t=0;t<6;t++)
                    {
                        int pc=0,nc=0;for(int k=0;k<4;k++){int v=tetrahedra[t,k];if(d[v]>=0)positive[pc++]=v;else negative[nc++]=v;}if(pc==0||pc==4)continue;
                        double dx=0,dy=0,dz=0;for(int k=0;k<nc;k++){var p=points[negative[k]];dx+=p.X/nc;dy+=p.Y/nc;dz+=p.Z/nc;}for(int k=0;k<pc;k++){var p=points[positive[k]];dx-=p.X/pc;dy-=p.Y/pc;dz-=p.Z/pc;}var outward=new SolidPoint(dx,dy,dz);
                        if(pc==1){int a=positive[0];Triangle(Intersect(a,negative[0]),Intersect(a,negative[1]),Intersect(a,negative[2]),outward);}
                        else if(pc==3){int a=negative[0];Triangle(Intersect(a,positive[0]),Intersect(a,positive[1]),Intersect(a,positive[2]),outward);}
                        else{int a=positive[0],b=positive[1],c=negative[0],e=negative[1];var ac=Intersect(a,c);var ae=Intersect(a,e);var bc=Intersect(b,c);var be=Intersect(b,e);Triangle(ac,ae,be,outward);Triangle(ac,be,bc,outward);}
                    }
                }}
            return mesh;
        }
    }
}
