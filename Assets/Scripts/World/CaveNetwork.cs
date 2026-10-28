using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
namespace Aevoria.World
{
    public readonly struct CavePassage
    {
        public readonly SolidPoint A,B;public readonly double Radius,VerticalScale;
        public CavePassage(SolidPoint a,SolidPoint b,double radius,double verticalScale=1){A=a;B=b;Radius=radius;VerticalScale=verticalScale;}
        public double Distance(double x,double y,double z)
        {
            double ax=x-A.X,ay=(y-A.Y)*VerticalScale,az=z-A.Z,bx=B.X-A.X,by=(B.Y-A.Y)*VerticalScale,bz=B.Z-A.Z;
            double length=bx*bx+by*by+bz*bz,t=length<1e-15?0:Math.Max(0,Math.Min(1,(ax*bx+ay*by+az*bz)/length));
            ax-=bx*t;ay-=by*t;az-=bz*t;return Math.Sqrt(ax*ax+ay*ay+az*az)-Radius;
        }
    }
    public readonly struct CaveChamber
    {
        public readonly SolidPoint Centre,Radii;
        public CaveChamber(SolidPoint centre,SolidPoint radii){Centre=centre;Radii=radii;}
        public double Distance(double x,double y,double z)
        {double dx=(x-Centre.X)/Radii.X,dy=(y-Centre.Y)/Radii.Y,dz=(z-Centre.Z)/Radii.Z;return (Math.Sqrt(dx*dx+dy*dy+dz*dz)-1)*Math.Min(Radii.X,Math.Min(Radii.Y,Radii.Z));}
    }
    public readonly struct CaveConnection
    {
        public readonly int A,B;public CaveConnection(int a,int b){A=a;B=b;}
    }
    public sealed class CaveSystem
    {
        public const double BucketSize=16;
        public readonly SolidPoint Entrance;public readonly bool HillsideEntrance;public readonly int Shafts,Ravines;
        public readonly IReadOnlyList<CavePassage> Passages;public readonly IReadOnlyList<CaveChamber> Chambers;
        public readonly IReadOnlyList<SolidPoint> Nodes;public readonly IReadOnlyList<CaveConnection> Connections;
        private readonly Dictionary<(int,int,int),List<int>> buckets=new Dictionary<(int,int,int),List<int>>();
        public bool Active=>Passages.Count>0;
        internal CaveSystem(SolidPoint entrance,bool hillside,int shafts,int ravines,List<CavePassage> passages,List<CaveChamber> chambers,List<SolidPoint> nodes,List<CaveConnection> connections)
        {
            Entrance=entrance;HillsideEntrance=hillside;Shafts=shafts;Ravines=ravines;Passages=passages.AsReadOnly();Chambers=chambers.AsReadOnly();Nodes=nodes.AsReadOnly();Connections=connections.AsReadOnly();
            for(int i=0;i<passages.Count;i++){var p=passages[i];double r=p.Radius+.8,ry=r/p.VerticalScale;Index(i,Math.Min(p.A.X,p.B.X)-r,Math.Min(p.A.Y,p.B.Y)-ry,Math.Min(p.A.Z,p.B.Z)-r,Math.Max(p.A.X,p.B.X)+r,Math.Max(p.A.Y,p.B.Y)+ry,Math.Max(p.A.Z,p.B.Z)+r);}
            for(int i=0;i<chambers.Count;i++){var p=chambers[i];double scale=1+.8/Math.Min(p.Radii.X,Math.Min(p.Radii.Y,p.Radii.Z));Index(-i-1,p.Centre.X-p.Radii.X*scale,p.Centre.Y-p.Radii.Y*scale,p.Centre.Z-p.Radii.Z*scale,p.Centre.X+p.Radii.X*scale,p.Centre.Y+p.Radii.Y*scale,p.Centre.Z+p.Radii.Z*scale);}
        }
        private void Index(int id,double minX,double minY,double minZ,double maxX,double maxY,double maxZ)
        {
            for(int z=(int)Math.Floor(minZ/BucketSize);z<=(int)Math.Floor(maxZ/BucketSize);z++)for(int y=(int)Math.Floor(minY/BucketSize);y<=(int)Math.Floor(maxY/BucketSize);y++)for(int x=(int)Math.Floor(minX/BucketSize);x<=(int)Math.Floor(maxX/BucketSize);x++)
            {var key=(x,y,z);if(!buckets.TryGetValue(key,out var list)){list=new List<int>();buckets.Add(key,list);}list.Add(id);}
        }
        public bool IntersectsChunk(int x,int y,int z)=>buckets.ContainsKey((x,y,z));
        public double Distance(double x,double y,double z)
        {
            if(!buckets.TryGetValue(((int)Math.Floor(x/BucketSize),(int)Math.Floor(y/BucketSize),(int)Math.Floor(z/BucketSize)),out var candidates))return 1000000;
            double distance=1000000;
            foreach(int i in candidates){double d=i>=0?Passages[i].Distance(x,y,z):Chambers[-i-1].Distance(x,y,z);double h=Math.Max(0,.6-Math.Abs(distance-d))/.6;distance=Math.Min(distance,d)-h*h*.15;}
            return distance;
        }
        internal static CaveSystem Empty()=>new CaveSystem(default,false,0,0,new List<CavePassage>(),new List<CaveChamber>(),new List<SolidPoint>(),new List<CaveConnection>());
    }
    // Lazy immutable geological systems. Eviction changes only preparation cost;
    // no physical state, random stream or terrain depends on visitation order.
    public sealed class CaveNetwork
    {
        public const int Version=2,CellSize=2048,CacheCapacity=128;
        private readonly WorldConfiguration configuration;private readonly IWorldHeightField terrain;private readonly GeologyMap geological;private readonly BaseElevationField ordinary;private readonly ClimateMap climate;private readonly uint moduleSeed;
        private readonly ConcurrentDictionary<(int,int),CaveSystem> cache=new ConcurrentDictionary<(int,int),CaveSystem>();
        private readonly ConcurrentQueue<(int,int)> order=new ConcurrentQueue<(int,int)>();
        public int CachedSystems=>cache.Count;
        public CaveNetwork(ProceduralGeography geography):this(geography?.Configuration??throw new ArgumentNullException(nameof(geography)),geography.Hydrology,geography.Geology,geography.OrdinaryTerrain,geography.Climate,WorldGenerationSeeds.Derive(geography.Configuration.Seed,WorldLayerIds.Caves,Version)){}
        public CaveNetwork(WorldConfiguration configuration,IWorldHeightField terrain,GeologyMap geology,BaseElevationField ordinary,ClimateMap climate,ulong seed)
        {this.configuration=configuration??throw new ArgumentNullException(nameof(configuration));this.terrain=terrain??throw new ArgumentNullException(nameof(terrain));geological=geology??throw new ArgumentNullException(nameof(geology));this.ordinary=ordinary??throw new ArgumentNullException(nameof(ordinary));this.climate=climate??throw new ArgumentNullException(nameof(climate));moduleSeed=(uint)(seed^(seed>>32));}
        private double Ground(double x,double z)=>GroundContactPatch.SurfaceHeight(terrain,configuration.CellSizeMeters,x,z);
        private double SiteRandom(int x,int z,uint channel)
        {unchecked{uint h=moduleSeed^(uint)x*374761393u^(uint)z*668265263u^channel*2246822519u;h=(h^(h>>13))*1274126177u;h^=h>>16;return h/(double)uint.MaxValue;}}
        public CaveSystem SystemAt(int cellX,int cellZ)
        {
            var key=(cellX,cellZ);if(cache.TryGetValue(key,out var existing))return existing;var built=Generate(cellX,cellZ);
            if(cache.TryAdd(key,built))order.Enqueue(key);else if(cache.TryGetValue(key,out existing))return existing;
            while(cache.Count>CacheCapacity&&order.TryDequeue(out var old))cache.TryRemove(old,out _);return built;
        }
        public CaveSystem At(double x,double z)=>SystemAt((int)Math.Floor(x/CellSize),(int)Math.Floor(z/CellSize));
        public double Distance(double x,double y,double z)=>At(x,z).Distance(x,y,z);
        public bool IntersectsChunk(int x,int y,int z)=>At((x+.5)*16,(z+.5)*16).IntersectsChunk(x,y,z);
        private CaveSystem Generate(int cx,int cz)
        {
            double R(int channel)=>SiteRandom(cx,cz,(uint)(3400+channel));
            double x=(cx+.5)*CellSize+(R(1)-.5)*300,z=(cz+.5)*CellSize+(R(2)-.5)*300;
            // All generated branches stay inside their site's cell. Boundary
            // cells without a full geological footprint are left uncut.
            var config=configuration;if(!config.Contains(new GeoPoint(x-600,z-600))||!config.Contains(new GeoPoint(x+600,z+600)))return CaveSystem.Empty();
            double surface=Ground(x,z);if(surface<config.Terrain.SeaLevel+40)return CaveSystem.Empty();var geology=geological.Sample(x,z);float plateau=ordinary.PlateauAt(x,z);
            double chance=.18+plateau*.35+Math.Min(.2,geology.Uplift*.3)+climate.RainfallAt(x,z)*.12;
            if(R(3)>chance)return CaveSystem.Empty();
            var nodes=new List<SolidPoint>();var links=new List<CaveConnection>();var passages=new List<CavePassage>();var rooms=new List<CaveChamber>();
            double turn=R(4)*Math.PI*2,radius=120+R(5)*100,lowest=surface;
            for(int i=0;i<16;i++){double angle=turn+i*Math.PI/8,px=x+Math.Cos(angle)*(radius+80),pz=z+Math.Sin(angle)*(radius+80);lowest=Math.Min(lowest,Ground(px,pz));}
            if(lowest<configuration.Terrain.SeaLevel+25)return CaveSystem.Empty();double top=lowest-24-R(6)*20;
            for(int layer=0;layer<2;layer++)for(int i=0;i<8;i++)
            {
                double angle=turn+i*Math.PI/4+layer*.21,r=radius*(.82+.30*SiteRandom(cx*19+i,cz*23+layer,3420));
                nodes.Add(new SolidPoint(x+Math.Cos(angle)*r,top-layer*(45+R(7)*35)+Math.Sin(angle*2+layer)*8,z+Math.Sin(angle)*r));
            }
            SolidPoint Curve(SolidPoint a,SolidPoint b,SolidPoint c,SolidPoint d,double t)
            {
                double F(double p,double q,double r,double s)=>.5*((2*q)+(-p+r)*t+(2*p-5*q+4*r-s)*t*t+(-p+3*q-3*r+s)*t*t*t);
                return new SolidPoint(F(a.X,b.X,c.X,d.X),F(a.Y,b.Y,c.Y,d.Y),F(a.Z,b.Z,c.Z,d.Z));
            }
            void Tube(SolidPoint a,SolidPoint b,SolidPoint c,SolidPoint d,double width)
            {var previous=b;for(int i=1;i<=12;i++){var next=Curve(a,b,c,d,i/12.0);passages.Add(new CavePassage(previous,next,width*(1+.10*Math.Sin(i*.7+turn))));previous=next;}}
            for(int layer=0;layer<2;layer++)for(int i=0;i<8;i++)
            {int offset=layer*8,a=offset+(i+7)%8,b=offset+i,c=offset+(i+1)%8,d=offset+(i+2)%8;Tube(nodes[a],nodes[b],nodes[c],nodes[d],2.4+R(10+i)*1.4);links.Add(new CaveConnection(b,c));}
            // Two long ramps connect the loops; the extra links also create
            // cycles between depths rather than stacking disconnected mazes.
            foreach(int i in new[]{1,5}){int j=8+(i+3)%8;Tube(nodes[(i+7)%8],nodes[i],nodes[j],nodes[8+(j-8+1)%8],3.0);links.Add(new CaveConnection(i,j));}
            for(int i=0;i<4;i++)
            {
                int start=i*3%16;var a=nodes[start];double angle=turn+i*1.83;
                var end=new SolidPoint(a.X+Math.Cos(angle)*(50+R(30+i)*55),a.Y-8-R(40+i)*20,a.Z+Math.Sin(angle)*(50+R(50+i)*55));
                Tube(a,a,end,end,2.2+R(60+i));int id=nodes.Count;nodes.Add(end);links.Add(new CaveConnection(start,id));rooms.Add(new CaveChamber(end,new SolidPoint(9+R(70+i)*12,5+R(80+i)*5,10+R(90+i)*15)));
            }
            // Vertical shafts are real air volumes with rock on every side.
            for(int i=0;i<2;i++){var a=nodes[i*4];var b=new SolidPoint(a.X+3,a.Y-38-R(100+i)*25,a.Z+2);passages.Add(new CavePassage(a,b,2.1));int id=nodes.Count;nodes.Add(b);links.Add(new CaveConnection(i*4,id));}
            double best=double.NegativeInfinity,ex=x,ez=z,eh=surface,gx=1,gz=0;
            for(int i=0;i<32;i++)
            {
                double angle=turn+i*Math.PI/16,px=x+Math.Cos(angle)*350,pz=z+Math.Sin(angle)*350,h=Ground(px,pz);
                double dx=(Ground(px+4,pz)-Ground(px-4,pz))/8,dz=(Ground(px,pz+4)-Ground(px,pz-4))/8,slope=Math.Sqrt(dx*dx+dz*dz);
                double score=Math.Min(1,slope)*5-Math.Abs(h-(top+35))*.02;if(h<configuration.Terrain.SeaLevel+12||score<=best)continue;best=score;ex=px;ez=pz;eh=h;gx=dx;gz=dz;
            }
            double gradient=Math.Sqrt(gx*gx+gz*gz);bool hillside=gradient>.18;gx=gradient>.0001?gx/gradient:Math.Cos(turn);gz=gradient>.0001?gz/gradient:Math.Sin(turn);
            var entrance=new SolidPoint(ex,eh-.8,ez);var outside=new SolidPoint(ex-gx*5,eh+.3,ez-gz*5);var inside=new SolidPoint(ex+gx*18,eh-5,ez+gz*18);
            passages.Add(new CavePassage(outside,entrance,3.2));passages.Add(new CavePassage(entrance,inside,3.0));
            int nearest=0;double distance=double.MaxValue;for(int i=0;i<8;i++){double d=(nodes[i].X-ex)*(nodes[i].X-ex)+(nodes[i].Z-ez)*(nodes[i].Z-ez);if(d<distance){distance=d;nearest=i;}}
            var target=nodes[nearest];var control=new SolidPoint((inside.X+target.X)*.5,inside.Y+(target.Y-inside.Y)*.45,(inside.Z+target.Z)*.5);
            Tube(entrance,inside,control,target,3.0);Tube(inside,control,target,nodes[(nearest+1)%8],3.0);int portal=nodes.Count;nodes.Add(entrance);links.Add(new CaveConnection(portal,nearest));
            int ravines=0;if(hillside&&(geology.Uplift>.12||plateau>.3))
            {
                var previous=new SolidPoint(ex,eh-5,ez);for(int i=1;i<=12;i++){double t=i/12.0,px=ex-gz*t*90+gx*Math.Sin(t*Math.PI*2)*8,pz=ez+gx*t*90+gz*Math.Sin(t*Math.PI*2)*8,ph=Ground(px,pz)-5;var next=new SolidPoint(px,ph,pz);passages.Add(new CavePassage(previous,next,4.5,.35));previous=next;}ravines=1;
            }
            return new CaveSystem(entrance,hillside,2,ravines,passages,rooms,nodes,links);
        }
    }
}
