using System;
using System.Collections.Generic;
using System.Threading;
namespace Aevoria.World
{
    public sealed class SettlementPath
    {
        public readonly string From,To;public readonly IReadOnlyList<GeoPoint> Points;
        internal SettlementPath(string from,string to,List<GeoPoint> points){From=from;To=to;Points=points.AsReadOnly();}
    }
    public sealed class SettlementPathNetwork
    {
        public readonly string SettlementId;public readonly IReadOnlyList<SettlementPath> Paths;public readonly IReadOnlyList<GeoPoint> Destinations;
        internal SettlementPathNetwork(string id,List<SettlementPath> paths,List<GeoPoint> destinations){SettlementId=id;Paths=paths.AsReadOnly();Destinations=destinations.AsReadOnly();}
    }
    public static class SettlementPaths
    {
        private readonly struct Entry{public readonly int Node;public readonly double Priority;public Entry(int node,double priority){Node=node;Priority=priority;}}
        private sealed class Heap
        {
            private readonly List<Entry> entries=new List<Entry>();public int Count=>entries.Count;
            public void Add(int node,double priority){var entry=new Entry(node,priority);int i=entries.Count;entries.Add(entry);while(i>0){int parent=(i-1)/2;if(entries[parent].Priority<=priority)break;entries[i]=entries[parent];i=parent;}entries[i]=entry;}
            public int Pop(){int result=entries[0].Node,last=entries.Count-1;var tail=entries[last];entries.RemoveAt(last);if(last==0)return result;int i=0;while(i*2+1<last){int child=i*2+1;if(child+1<last&&entries[child+1].Priority<entries[child].Priority)child++;if(entries[child].Priority>=tail.Priority)break;entries[i]=entries[child];i=child;}entries[i]=tail;return result;}
        }
        private sealed class Grid
        {
            public readonly int N;private const double Step=4;private readonly double west,south;private readonly ProceduralGeography geo;private readonly IReadOnlyList<CivicBuilding> buildings;private readonly byte[] cells;private readonly float[] heights;private readonly CancellationToken token;
            public Grid(ProceduralGeography geography,SettlementState settlement,CancellationToken cancellation)
            {
                geo=geography;buildings=settlement.Buildings;token=cancellation;double extent=64;foreach(var b in buildings)extent=Math.Max(extent,Math.Max(Math.Abs(b.X-settlement.X),Math.Abs(b.Z-settlement.Z))+20);
                extent=Math.Min(256,Math.Ceiling(extent/Step)*Step);N=(int)(extent*2/Step)+1;west=settlement.X-extent;south=settlement.Z-extent;cells=new byte[N*N];heights=new float[cells.Length];
            }
            public GeoPoint Point(int node)=>new GeoPoint(west+node%N*Step,south+node/N*Step);
            private bool Clear(double x,double z)
            {foreach(var building in buildings)if(CivicGeometry.Contains(building,x,z,.95))return false;return true;}
            public bool Walkable(int node)
            {
                if(node<0||node>=cells.Length)return false;if(cells[node]!=0)return cells[node]==2;token.ThrowIfCancellationRequested();var p=Point(node);cells[node]=1;
                if(!geo.Configuration.Contains(p)||!Clear(p.X,p.Y))return false;float h=geo.ElevationAt(p.X,p.Y);heights[node]=h;
                if(h<geo.SeaLevel+2||geo.Hydrology.IsLakeWater(p.X,p.Y,h)||geo.Hydrology.IsRiverWater(p.X,p.Y,h))return false;
                if(Math.Abs(geo.ElevationAt(p.X+2,p.Y)-geo.ElevationAt(p.X-2,p.Y))>1.7||Math.Abs(geo.ElevationAt(p.X,p.Y+2)-geo.ElevationAt(p.X,p.Y-2))>1.7)return false;
                cells[node]=2;return true;
            }
            public bool Segment(GeoPoint a,GeoPoint b)
            {
                int count=Math.Max(1,(int)Math.Ceiling(Distance(a,b)/1.5));float previous=geo.ElevationAt(a.X,a.Y);
                for(int i=0;i<=count;i++){token.ThrowIfCancellationRequested();double t=i/(double)count,x=a.X+(b.X-a.X)*t,z=a.Y+(b.Y-a.Y)*t;if(!Clear(x,z)||!geo.Configuration.Contains(new GeoPoint(x,z)))return false;float h=geo.ElevationAt(x,z);if(h<geo.SeaLevel+2||Math.Abs(h-previous)>1||geo.Hydrology.IsLakeWater(x,z,h)||geo.Hydrology.IsRiverWater(x,z,h))return false;previous=h;}return true;
            }
            private int Snap(GeoPoint point)
            {
                int cx=(int)Math.Round((point.X-west)/Step),cz=(int)Math.Round((point.Y-south)/Step);double best=double.MaxValue;int chosen=-1;
                for(int radius=0;radius<=3;radius++)for(int z=cz-radius;z<=cz+radius;z++)for(int x=cx-radius;x<=cx+radius;x++)
                {if(x<0||z<0||x>=N||z>=N)continue;int node=z*N+x;if(!Walkable(node))continue;double distance=Distance(point,Point(node));if(distance<best&&Segment(point,Point(node))){best=distance;chosen=node;}}
                return chosen;
            }
            public List<GeoPoint> Route(GeoPoint start,GeoPoint finish)
            {
                if(Segment(start,finish))return new List<GeoPoint>{start,finish};int a=Snap(start),b=Snap(finish);if(a<0||b<0)return null;
                var costs=new double[cells.Length];var parent=new int[cells.Length];var closed=new bool[cells.Length];for(int i=0;i<costs.Length;i++){costs[i]=double.PositiveInfinity;parent[i]=-1;}
                var heap=new Heap();costs[a]=0;heap.Add(a,Distance(Point(a),Point(b)));int work=0;
                while(heap.Count>0)
                {
                    if((++work&63)==0)token.ThrowIfCancellationRequested();int node=heap.Pop();if(closed[node])continue;closed[node]=true;if(node==b)break;int x=node%N,z=node/N;
                    for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                    {
                        if(dx==0&&dz==0||x+dx<0||z+dz<0||x+dx>=N||z+dz>=N)continue;int next=(z+dz)*N+x+dx;if(closed[next]||!Walkable(next))continue;
                        if(dx!=0&&dz!=0&&(!Walkable(z*N+x+dx)||!Walkable((z+dz)*N+x)))continue;
                        double step=Step*(dx!=0&&dz!=0?Math.Sqrt(2):1),rise=Math.Abs(heights[node]-heights[next]);if(rise>step*.42)continue;double cost=costs[node]+step+rise*2;
                        if(cost>=costs[next])continue;costs[next]=cost;parent[next]=node;heap.Add(next,cost+Distance(Point(next),Point(b)));
                    }
                }
                if(!closed[b])return null;var points=new List<GeoPoint>();for(int at=b;at>=0;at=parent[at]){points.Add(Point(at));if(at==a)break;}points.Reverse();points.Insert(0,start);points.Add(finish);
                // Every displayed segment is rechecked at physical sampling scale.
                for(int i=1;i<points.Count;i++)if(!Segment(points[i-1],points[i]))return null;return Refine(points);
            }
            private List<GeoPoint> Refine(List<GeoPoint> source)
            {
                // The visible path and resident route share these exact points.
                // First remove grid stair-steps using physical line of sight.
                var pulled=new List<GeoPoint>{source[0]};int at=0;
                while(at<source.Count-1)
                {
                    token.ThrowIfCancellationRequested();int next=source.Count-1;
                    while(next>at+1&&!Segment(source[at],source[next]))next--;
                    pulled.Add(source[next]);at=next;
                }
                var rounded=new List<GeoPoint>{pulled[0]};
                for(int i=1;i<pulled.Count-1;i++)
                {
                    var before=pulled[i-1];var corner=pulled[i];var after=pulled[i+1];
                    double incoming=Distance(before,corner),outgoing=Distance(corner,after);
                    double radius=Math.Min(4,Math.Min(incoming,outgoing)*.25);
                    if(radius<.3){rounded.Add(corner);continue;}
                    var entry=Mix(corner,before,radius/incoming);var exit=Mix(corner,after,radius/outgoing);
                    int count=Math.Max(4,(int)Math.Ceiling(radius*2/.75));var curve=new List<GeoPoint>{entry};
                    for(int step=1;step<=count;step++){double t=step/(double)count;curve.Add(Mix(Mix(entry,corner,t),Mix(corner,exit,t),t));}
                    bool safe=Segment(rounded[rounded.Count-1],entry)&&Segment(exit,after);
                    for(int step=1;safe&&step<curve.Count;step++)safe=Segment(curve[step-1],curve[step]);
                    if(safe)rounded.AddRange(curve);else rounded.Add(corner);
                }
                rounded.Add(pulled[pulled.Count-1]);
                for(int i=1;i<rounded.Count;i++)if(!Segment(rounded[i-1],rounded[i]))return pulled;
                return rounded;
            }
            private static GeoPoint Mix(GeoPoint a,GeoPoint b,double t)=>new GeoPoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);
        }
        public static double Distance(GeoPoint a,GeoPoint b)=>Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
        public static SettlementPathNetwork Build(ProceduralGeography geography,SettlementState settlement,CancellationToken token=default)
        {
            token.ThrowIfCancellationRequested();
            var buildings=settlement.Buildings;var paths=new List<SettlementPath>();var destinations=new List<GeoPoint>();if(buildings.Count==0)return new SettlementPathNetwork(settlement.Id,paths,destinations);
            var grid=new Grid(geography,settlement,token);var doors=new GeoPoint[buildings.Count];var connected=new bool[buildings.Count];var rejected=new bool[buildings.Count];for(int i=0;i<doors.Length;i++)doors[i]=CivicGeometry.Door(buildings[i]);connected[0]=true;destinations.Add(doors[0]);
            for(int iteration=1;iteration<buildings.Count;iteration++)
            {
                token.ThrowIfCancellationRequested();int from=-1,to=-1;double best=double.MaxValue;
                for(int a=0;a<doors.Length;a++)if(connected[a])for(int b=0;b<doors.Length;b++)if(!connected[b]&&!rejected[b]){double distance=Distance(doors[a],doors[b]);if(distance<best){best=distance;from=a;to=b;}}
                if(to<0)break;var route=grid.Route(doors[from],doors[to]);
                if(route!=null){connected[to]=true;paths.Add(new SettlementPath(buildings[from].Id,buildings[to].Id,route));destinations.Add(doors[to]);}else rejected[to]=true;
            }
            return new SettlementPathNetwork(settlement.Id,paths,destinations);
        }
    }
}
