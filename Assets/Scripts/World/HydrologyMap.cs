using System;
using System.Collections.Generic;

namespace Aevoria.World
{
    // Priority-flood drainage on a world grid. Receivers always precede their
    // children in flood order, including flats: no cycles or per-chunk authority.
    public sealed class HydrologyMap:IWorldHeightField
    {
        public const int Version=13;
        public const int Resolution=1025;
        public readonly float[] Ground, Water,Discharge;
        public WorldConfiguration Configuration{get;}
        public ClimateMap Climate{get;}
        public InlandWaterLayout CanonicalSurfaces{get;private set;}
        private readonly IWorldHeightField source;private readonly double cellArea;private readonly System.Threading.CancellationToken cancellation;
        private readonly Action<string,long,long> report;
        public readonly int[] Receiver, Accumulation, Lake;
        public readonly int[] PotentialLake;
        public readonly Dictionary<int,string> RejectedBasinReasons=new Dictionary<int,string>();
        private readonly byte[] basinOutcome;
        public enum BasinOutcome:byte { None,ClosedWaterBudget,FineOutlet,SmallFootprint,Covered,SeaLevel,RefinedOutlet,ErodedSmallBasin }
        public int ErodedSmallBasins{get;private set;}
        public BasinOutcome OutcomeAt(int i)=>(BasinOutcome)basinOutcome[i];
        public readonly int[] FloodOrder;
        public int RiverSegments { get; private set; }
        public int LakeCount { get; private set; }
        public double StepX { get; }
        public double StepY { get; }
        private readonly double width, depth;
        private readonly float sea;
        private readonly bool[] riverNeighborhood;private bool[] acceptedWater,acceptedLakes;
        private readonly List<int>[] segmentLookup=new List<int>[Resolution*Resolution];
        public const int PathSteps=32;
        private float[] pathX,pathY,pathLevel;
        private int[] pathOffset,pathSamples;
        public int PathSamples(int i)=>pathSamples!=null&&pathSamples[i]>0?pathSamples[i]:PathSteps;
        private readonly Dictionary<int,List<GeoPoint>> routedPaths=new Dictionary<int,List<GeoPoint>>();
        private readonly uint pathSeed;
        private readonly float[] runoffSource;
        private readonly Dictionary<int,int> basinPadding=new Dictionary<int,int>();
        public int BasinPadding(int potential)=>basinPadding.TryGetValue(potential,out int padding)?padding:1;
        public int BasinAnchor(IReadOnlyList<int> nodes)
        {int lowest=nodes[0];foreach(int node in nodes)if(Ground[node]<Ground[lowest]||Ground[node]==Ground[lowest]&&node<lowest)lowest=node;return lowest;}
        public HydrologyMap(ProceduralGeography geography,System.Threading.CancellationToken token=default,Action<string,long,long> report=null,ulong? moduleSeed=null)
            :this(geography?.Configuration??throw new ArgumentNullException(nameof(geography)),geography.Surface,geography.Climate,token,report,moduleSeed){}
        public HydrologyMap(WorldConfiguration configuration,IWorldHeightField source,ClimateMap climate,System.Threading.CancellationToken token=default,Action<string,long,long> report=null,ulong? moduleSeed=null)
        {
            Configuration=configuration??throw new ArgumentNullException(nameof(configuration));this.source=source??throw new ArgumentNullException(nameof(source));Climate=climate??throw new ArgumentNullException(nameof(climate));cancellation=token;this.report=report;
            ulong resolvedSeed=moduleSeed??WorldGenerationSeeds.Derive(configuration.Seed,WorldLayerIds.Hydrology,Version);pathSeed=(uint)(resolvedSeed^(resolvedSeed>>32));
            width=configuration.WidthMeters; depth=configuration.DepthMeters;
            // Drainage carves the landform at its reference datum. Changing the
            // ocean level floods those same heights; it must not reshape them.
            StepX=width/(Resolution-1); StepY=depth/(Resolution-1); sea=0;
            cellArea=StepX*StepY/1000000;
            int count=Resolution*Resolution;
            basinOutcome=new byte[count];Ground=new float[count]; Water=new float[count];Discharge=new float[count]; Receiver=new int[count];
            Accumulation=new int[count]; Lake=new int[count]; FloodOrder=new int[count];runoffSource=new float[count];
            riverNeighborhood=new bool[count]; var visited=new bool[count]; var heap=new MinHeap();

            for(int z=0;z<Resolution;z++) for(int x=0;x<Resolution;x++)
            {
                if(x==0){cancellation.ThrowIfCancellationRequested();report?.Invoke("Sampling drainage elevation and rainfall",z,Resolution);}
                int i=z*Resolution+x; Ground[i]=BaseElevationAt(X(i),Y(i));
                Receiver[i]=-1; Accumulation[i]=1; Lake[i]=-1;Discharge[i]=runoffSource[i]=(float)(cellArea*Climate.RainfallAt(X(i),Y(i)));
                if(Ground[i]<=sea || x==0 || z==0 || x==Resolution-1 || z==Resolution-1)
                {visited[i]=true; Water[i]=Math.Max(sea,Ground[i]); heap.Push(i,Water[i]);}
            }
            int order=0;
            while(heap.Count>0)
            {
                if((order&255)==0){cancellation.ThrowIfCancellationRequested();if((order&4095)==0)report?.Invoke("Routing connected drainage basins",order,count);}
                int i=heap.Pop(); FloodOrder[order++]=i;
                int x=i%Resolution,z=i/Resolution;
                for(int dz=-1;dz<=1;dz++) for(int dx=-1;dx<=1;dx++)
                {
                    int nx=x+dx,nz=z+dz;
                    if(nx<0 || nx>=Resolution || nz<0 || nz>=Resolution) continue;
                    int j=nz*Resolution+nx; if(visited[j]) continue;
                    visited[j]=true; Receiver[j]=i; Water[j]=Math.Max(Ground[j],Water[i]); heap.Push(j,Water[j]);
                }
            }
            DrainageRouting.RefineDownhillReceivers(Water,Receiver,Resolution,Resolution,StepX,StepY,token,report);
            for(int n=count-1;n>=0;n--) {int i=FloodOrder[n], r=Receiver[i]; if(r>=0){Accumulation[r]+=Accumulation[i];Discharge[r]+=Discharge[i];}}
            LabelLakes();
            PotentialLake=(int[])Lake.Clone();
            BuildPaths();
        }
        private void LabelLakes()
        {
            int count=Ground.Length;LakeCount=0;for(int i=0;i<count;i++)Lake[i]=-1;
            var queue=new Queue<int>();
            for(int i=0;i<count;i++)
            {
                if(Ground[i]<=sea || Water[i]-Ground[i]<2 || Lake[i]>=0) continue;
                int id=LakeCount++; Lake[i]=id; queue.Enqueue(i);
                while(queue.Count>0)
                {
                    int j=queue.Dequeue(),x=j%Resolution,z=j/Resolution;
                    for(int dz=-1;dz<=1;dz++) for(int dx=-1;dx<=1;dx++)
                    {
                        int nx=x+dx,nz=z+dz; if(nx<0 || nz<0 || nx>=Resolution || nz>=Resolution) continue;
                        int k=nz*Resolution+nx;
                        if(Lake[k]<0 && Ground[k]>sea && Water[k]-Ground[k]>=.02f && Math.Abs(Water[k]-Water[i])<0.01f)
                        {Lake[k]=id;queue.Enqueue(k);}
                    }
                }
            }
        }
        private void AllocatePaths()
        {
            int count=Ground.Length;if(pathOffset==null||pathOffset.Length!=count){pathOffset=new int[count];pathSamples=new int[count];}int length=0;
            for(int i=0;i<count;i++)
            {
                pathOffset[i]=-1;pathSamples[i]=0;if(!IsChannelCandidate(i))continue;
                pathOffset[i]=length;pathSamples[i]=routedPaths.TryGetValue(i,out var route)?Math.Max(PathSteps,(route.Count-1)*3):PathSteps;length+=pathSamples[i]+1;
            }
            if(pathX==null||pathX.Length!=length){pathX=new float[length];pathY=new float[length];pathLevel=new float[length];}
        }
        private void BuildPaths()
        {
            AllocatePaths();int count=Ground.Length;RiverSegments=0;Array.Clear(riverNeighborhood,0,riverNeighborhood.Length);Array.Clear(pathX,0,pathX.Length);Array.Clear(pathY,0,pathY.Length);
            if(dominantUpstream==null)dominantUpstream=new int[count];var upstream=dominantUpstream;for(int i=0;i<count;i++) upstream[i]=-1;
            for(int i=0;i<count;i++){int r=Receiver[i];if(r>=0&&(upstream[r]<0||Accumulation[i]>Accumulation[upstream[r]])) upstream[r]=i;}
            for(int i=0;i<count;i++) if(IsChannelCandidate(i))
            {
                cancellation.ThrowIfCancellationRequested();
                report?.Invoke("Preparing connected river paths",i,count);
                RiverSegments++; int x=i%Resolution,z=i/Resolution;
                int r=Receiver[i];double vx=X(r)-X(i),vy=Y(r)-Y(i),length=Math.Sqrt(vx*vx+vy*vy);
                double phase=GradientField.Sample(X(i)/700,Y(i)/700,pathSeed,771)*Math.PI;
                double grade=Math.Max(0,(Ground[i]-Ground[r])/length);
                double lowland=1-Math.Max(0,Math.Min(1,(grade-.005)/.055));
                double bend=Math.Min(length*.08,RiverWidth(i)*1.5)*lowland;
                // Atlas rivers inherit their bends from the connected drainage
                // valley. Do not add a fresh sinusoidal wiggle per grid segment.
                if(Configuration.Terrain.Profile==GeographyProfile.Atlas)bend=0;
                int previous=upstream[i],next=Receiver[r];
                double tx0=previous<0?vx:X(r)-X(previous),ty0=previous<0?vy:Y(r)-Y(previous);
                double tx1=next<0?vx:X(next)-X(i),ty1=next<0?vy:Y(next)-Y(i);
                double len0=Math.Sqrt(tx0*tx0+ty0*ty0),len1=Math.Sqrt(tx1*tx1+ty1*ty1);
                tx0=tx0/len0*length;ty0=ty0/len0*length;tx1=tx1/len1*length;ty1=ty1/len1*length;
                // Keep longitudinal progress positive through coarse routing
                // corners. Meander is lateral and has zero endpoint derivative.
                if(tx0*vx+ty0*vy<length*length*.2){tx0=vx*.8+tx0*.2;ty0=vy*.8+ty0*.2;}
                if(tx1*vx+ty1*vy<length*length*.2){tx1=vx*.8+tx1*.2;ty1=vy*.8+ty1*.2;}
                for(int p=0;p<=PathSamples(i);p++)
                {
                    double t=(double)p/PathSamples(i),envelope=Math.Sin(t*Math.PI),offset=envelope*envelope*Math.Sin(t*Math.PI*2+phase)*bend;
                    double t2=t*t,t3=t2*t,a=2*t3-3*t2+1,b=t3-2*t2+t,c=-2*t3+3*t2,d=t3-t2;
                    int j=pathOffset[i]+p;pathX[j]=(float)(a*X(i)+b*tx0+c*X(r)+d*tx1-vy/length*offset);pathY[j]=(float)(a*Y(i)+b*ty0+c*Y(r)+d*ty1+vx/length*offset);
                }
                if(routedPaths.TryGetValue(i,out var route))for(int p=0;p<=PathSamples(i);p++)
                {double at=p/(double)PathSamples(i)*(route.Count-1);int index=Math.Min(route.Count-2,(int)at);double f=at-index;var point=RoundedOutlet(route,index,f);int target=pathOffset[i]+p;pathX[target]=(float)point.X;pathY[target]=(float)point.Y;}
                for(int dz=-1;dz<=1;dz++) for(int dx=-1;dx<=1;dx++)
                {int nx=x+dx,nz=z+dz;if(nx>=0&&nx<Resolution&&nz>=0&&nz<Resolution) riverNeighborhood[nz*Resolution+nx]=true;}
            }
            FitPathLevels();BuildSegmentLookup();
        }
        private static GeoPoint RoundedOutlet(IReadOnlyList<GeoPoint> route,int index,double t)
        {
            var a=route[index];var b=route[index+1];var before=route[Math.Max(0,index-1)];var after=route[Math.Min(route.Count-1,index+2)];
            double vx=b.X-a.X,vz=b.Y-a.Y,length=Math.Sqrt(vx*vx+vz*vz);if(length<.000001)return a;
            double tx0=b.X-before.X,tz0=b.Y-before.Y,tx1=after.X-a.X,tz1=after.Y-a.Y,l0=Math.Sqrt(tx0*tx0+tz0*tz0),l1=Math.Sqrt(tx1*tx1+tz1*tz1);
            if(l0<.000001||tx0*vx+tz0*vz<length*l0*.2){tx0=vx;tz0=vz;l0=length;}
            if(l1<.000001||tx1*vx+tz1*vz<length*l1*.2){tx1=vx;tz1=vz;l1=length;}
            tx0*=length*.65/l0;tz0*=length*.65/l0;tx1*=length*.65/l1;tz1*=length*.65/l1;
            double t2=t*t,t3=t2*t,h0=2*t3-3*t2+1,h1=t3-2*t2+t,h2=-2*t3+3*t2,h3=t3-t2;
            return new GeoPoint(h0*a.X+h1*tx0+h2*b.X+h3*tx1,h0*a.Y+h1*tz0+h2*b.Y+h3*tz1);
        }
        private int[] dominantUpstream;
        public void JunctionDirection(int node,out double dx,out double dz)
        {
            double ax=X(node),az=Y(node),bx=ax,bz=az;int incoming=dominantUpstream[node];
            if(incoming>=0&&pathOffset[incoming]>=0)PathPoint(incoming,Math.Max(0,PathSamples(incoming)-1),out ax,out az);
            if(pathOffset[node]>=0)PathPoint(node,Math.Min(1,PathSamples(node)),out bx,out bz);
            dx=bx-ax;dz=bz-az;
        }
        private double ValleyRadius(int i)=>Math.Max((Configuration.Terrain.Profile==GeographyProfile.Atlas?Math.Max(RiverWidthAt(i,0),RiverWidthAt(i,1)):RiverWidth(i))*3,Math.Max(StepX,StepY)*.9);
        private void BuildSegmentLookup()
        {
            Array.Clear(segmentLookup,0,segmentLookup.Length);
            for(int i=0;i<Ground.Length;i++)if(IsChannelCandidate(i))
            {
                cancellation.ThrowIfCancellationRequested();double minX=double.MaxValue,minY=minX,maxX=double.MinValue,maxY=maxX;
                for(int p=0;p<=PathSamples(i);p++){PathPoint(i,p,out double x,out double y);minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                double radius=ValleyRadius(i);int x0=Math.Max(0,(int)Math.Floor((minX-radius+width/2)/StepX-.5)),x1=Math.Min(Resolution-1,(int)Math.Ceiling((maxX+radius+width/2)/StepX+.5));
                int y0=Math.Max(0,(int)Math.Floor((minY-radius+depth/2)/StepY-.5)),y1=Math.Min(Resolution-1,(int)Math.Ceiling((maxY+radius+depth/2)/StepY+.5));
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){int cell=y*Resolution+x;if(segmentLookup[cell]==null)segmentLookup[cell]=new List<int>();segmentLookup[cell].Add(i);}
            }
        }
        private void FitPathLevels()
        {
            for(int i=0;i<Ground.Length;i++)if(IsChannelCandidate(i))
            {
                int r=Receiver[i];float previous=Water[i];
                if(routedPaths.ContainsKey(i))
                {
                    float downstream=Water[r];for(int p=PathSamples(i);p>=0;p--){int at=pathOffset[i]+p;if(p<PathSamples(i))downstream=Math.Max(downstream,BaseElevationAt(pathX[at],pathY[at])-.02f);pathLevel[at]=Math.Min(Water[i],downstream);}pathLevel[pathOffset[i]]=Water[i];continue;
                }
                for(int p=0;p<=PathSamples(i);p++)
                {int at=pathOffset[i]+p;float level=Water[i]+(Water[r]-Water[i])*(p/(float)PathSamples(i));if(p>0&&p<PathSamples(i)&&Lake[i]<0&&Lake[r]<0)level=Math.Max(Water[r],Math.Min(level,BaseElevationAt(pathX[at],pathY[at])+.03f));previous=Math.Min(previous,level);pathLevel[at]=previous;}
            }
        }
        public float PathLevel(int i,double t)
        {if(pathOffset[i]<0)return Water[i];int samples=PathSamples(i);double p=Math.Max(0,Math.Min(samples,t*samples));int at=Math.Min(samples-1,(int)p);double f=p-at;int first=pathOffset[i],offset=first+at;return (float)ContinuousGrid.Curve(pathLevel[first+Math.Max(0,at-1)],pathLevel[offset],pathLevel[offset+1],pathLevel[first+Math.Min(samples,at+2)],f);}
        // Fine terrain can reveal an outlet hidden by the analysis grid. Reject
        // that reservoir, breach its receiver corridor, and rebuild the same
        // authoritative paths instead of ending the displayed river there.
        private int SettleClosedBudgets(InlandWaterLayout surfaces)
        {
            int changed=0;
            foreach(var decision in surfaces.ClosedBudgets)
            {
                cancellation.ThrowIfCancellationRequested();
                var nodes=decision.Nodes;if(nodes.Length==0)continue;
                int sink=nodes[0];foreach(int i in nodes)if(Ground[i]<Ground[sink]||(Ground[i]==Ground[sink]&&i<sink))sink=i;
                if(Water[sink]<=decision.Level+.05f)continue;
                changed++;RejectedBasinReasons[decision.PotentialBasin]="closed-water-budget";foreach(int node in nodes)basinOutcome[node]=(byte)BasinOutcome.ClosedWaterBudget;
                ReorientBasin(nodes,sink,decision.Level,-1);
            }
            if(changed==0)return 0;
            RebuildFlowOrder();LabelLakes();BuildPaths();return changed;
        }
        private void ReorientBasin(IReadOnlyList<int> nodes,int sink,float level,int target)
        {
            foreach(int node in nodes)routedPaths.Remove(node);
            var membership=new HashSet<int>(nodes);var seen=new HashSet<int>();var heap=new MinHeap();
            Receiver[sink]=target;Water[sink]=Math.Max(Ground[sink],level);seen.Add(sink);heap.Push(sink,Water[sink]);
            while(heap.Count>0)
            {
                int i=heap.Pop();if((seen.Count&255)==0)cancellation.ThrowIfCancellationRequested();
                foreach(int j in Neighbours(i%Resolution,i/Resolution))if(membership.Contains(j)&&seen.Add(j))
                {Receiver[j]=i;Water[j]=Math.Max(Ground[j],Water[i]);heap.Push(j,Water[j]);}
            }
            if(seen.Count!=nodes.Count)throw new InvalidOperationException("Refined drainage basin is disconnected.");
        }
        private readonly Dictionary<int,string> outletSearchHistory=new Dictionary<int,string>();
        public bool CanErodeSmallBasin(IReadOnlyList<int> nodes)=>PlanShallowIncision(nodes)!=null;
        private Dictionary<int,float> PlanShallowIncision(IReadOnlyList<int> nodes)
        {
            int source=BasinAnchor(nodes);float level=Ground[source]+.02f;
            // Only shallow natural sills are eligible. Deep closed basins keep
            // their water-budget treatment instead of receiving an invented
            // gorge. Receivers remain unchanged, preserving tributary topology.
            if(Water[source]-level>12)return null;
            var levels=new Dictionary<int,float>();var pending=new Queue<int>();
            foreach(int node in nodes){if(Ground[node]-level>12)return null;levels[node]=Math.Min(Water[node],level);pending.Enqueue(node);}
            int work=0;
            // A flat basin can have several original outlets. Propagate the
            // incision down every affected receiver branch before committing;
            // lowering only one route leaves uphill junctions on the others.
            while(pending.Count>0)
            {
                if((work++&255)==0)cancellation.ThrowIfCancellationRequested();
                int node=pending.Dequeue(),next=Receiver[node];if(next<0)continue;
                double dx=X(next)-X(node),dz=Y(next)-Y(node);float desired=levels[node]-(float)(Math.Sqrt(dx*dx+dz*dz)*.00002);
                float old=levels.TryGetValue(next,out float planned)?planned:Water[next];if(old<=desired)continue;
                // A higher receiving lake sets the base level. Do not drain a
                // separate natural reservoir to remove a small upstream pond.
                if(Ground[next]<=sea||Ground[next]-desired>12||Lake[next]>=0&&Lake[next]!=Lake[source])return null;
                levels[next]=desired;pending.Enqueue(next);
            }
            return levels;
        }
        private bool ErodeSmallBasin(List<int> nodes)
        {
            var levels=PlanShallowIncision(nodes);if(levels==null)return false;
            foreach(var pair in levels)Water[pair.Key]=pair.Value;
            ErodedSmallBasins++;return true;
        }
        private bool RefineOutlet(List<int> nodes)
        {
            int source=nodes[0];double minX=X(source),maxX=minX,minZ=Y(source),maxZ=minZ;float old=Water[source];
            var membership=new HashSet<int>(nodes);
            foreach(int i in nodes){if(Ground[i]<Ground[source])source=i;minX=Math.Min(minX,X(i));maxX=Math.Max(maxX,X(i));minZ=Math.Min(minZ,Y(i));maxZ=Math.Max(maxZ,Y(i));}
            double margin=Math.Max(1000,Math.Max(StepX,StepY)*4*BasinPadding(source));
            minX=Math.Max(-width/2,minX-margin);maxX=Math.Min(width/2,maxX+margin);minZ=Math.Max(-depth/2,minZ-margin);maxZ=Math.Min(depth/2,maxZ+margin);
            double step=Math.Max(Math.Min(StepX,StepY)/4,Math.Max(maxX-minX,maxZ-minZ)/768);
            int nx=(int)Math.Ceiling((maxX-minX)/step)+1,nz=(int)Math.Ceiling((maxZ-minZ)/step)+1,count=nx*nz;
            var floors=new float[count];var cost=new float[count];var parent=new int[count];
            for(int i=0;i<count;i++){floors[i]=float.NaN;cost[i]=float.PositiveInfinity;parent[i]=-1;}
            GeoPoint Point(int i)=>new GeoPoint(Math.Min(maxX,minX+i%nx*step),Math.Min(maxZ,minZ+i/nx*step));
            float Floor(int i){if(float.IsNaN(floors[i])){var p=Point(i);floors[i]=BaseElevationAt(p.X,p.Y);}return floors[i];}
            float Bridge(GeoPoint a,GeoPoint b){float high=float.MinValue;for(int j=0;j<=4;j++)high=Math.Max(high,BaseElevationAt(a.X+(b.X-a.X)*j/4,a.Y+(b.Y-a.Y)*j/4));return high;}
            var safeTargets=new Dictionary<int,bool>();
            bool SafeTarget(int target)
            {
                if(safeTargets.TryGetValue(target,out bool safe))return safe;
                int i=target,guard=0;while(i>=0&&guard++<Ground.Length){if(membership.Contains(i)){safeTargets[target]=false;return false;}if(safeTargets.TryGetValue(i,out safe)){safeTargets[target]=safe;return safe;}i=Receiver[i];}
                return safeTargets[target]=i<0;
            }
            int sx=Math.Max(0,Math.Min(nx-1,(int)Math.Round((X(source)-minX)/step))),sz=Math.Max(0,Math.Min(nz-1,(int)Math.Round((Y(source)-minZ)/step))),start=sz*nx+sx;
            cost[start]=Math.Max(Ground[source],Bridge(new GeoPoint(X(source),Y(source)),Point(start)));var heap=new MinHeap();heap.Push(start,cost[start]);
            float best=old-.05f;int finish=-1,targetNode=-1,processed=0;
            while(heap.Count>0)
            {
                int i=heap.Pop();if((processed++&255)==0)cancellation.ThrowIfCancellationRequested();if(cost[i]>=best)break;
                var point=Point(i);int target=Nearest(point.X,point.Y);
                if(!membership.Contains(target)&&Water[target]<best&&SafeTarget(target))
                {
                    float spill=Math.Max(cost[i],Math.Max(Water[target],Bridge(point,new GeoPoint(X(target),Y(target)))));
                    if(spill<best){best=spill;finish=i;targetNode=target;}
                }
                int x=i%nx,z=i/nx;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    int xx=x+dx,zz=z+dz;if((dx==0&&dz==0)||xx<0||xx>=nx||zz<0||zz>=nz)continue;int j=zz*nx+xx;if(!float.IsPositiveInfinity(cost[j]))continue;
                    cost[j]=Math.Max(cost[i],Floor(j));if(cost[j]>=best)continue;parent[j]=i;heap.Push(j,cost[j]);
                }
            }
            if(finish<0){outletSearchHistory.TryGetValue(source,out string history);outletSearchHistory[source]=history+" [pad="+BasinPadding(source)+", step="+step+", visited="+processed+", start="+cost[start]+", limit="+old+"]";return false;}
            var route=new List<GeoPoint>();for(int i=finish;i>=0;i=parent[i])route.Add(Point(i));route.Reverse();
            route.Insert(0,new GeoPoint(X(source),Y(source)));route.Add(new GeoPoint(X(targetNode),Y(targetNode)));
            for(int j=route.Count-1;j>0;j--)if(Math.Abs(route[j].X-route[j-1].X)+Math.Abs(route[j].Y-route[j-1].Y)<.0001)route.RemoveAt(j);
            float level=Math.Max(Water[targetNode],best-.02f);ReorientBasin(nodes,source,level,targetNode);routedPaths[source]=route;return true;
        }
        public ulong StateIdentity()
        {
            unchecked{ulong hash=14695981039346656037UL;for(int i=0;i<Ground.Length;i++){hash=(hash^(uint)Receiver[i])*1099511628211UL;hash=(hash^(uint)Lake[i])*1099511628211UL;hash=(hash^(uint)BitConverter.SingleToInt32Bits(Water[i]))*1099511628211UL;}foreach(var item in basinPadding){hash=(hash^(uint)item.Key)*1099511628211UL;hash=(hash^(uint)item.Value)*1099511628211UL;}return hash;}
        }
        private IEnumerable<int> Neighbours(int x,int z)
        {
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {int nx=x+dx,nz=z+dz;if((dx!=0||dz!=0)&&nx>=0&&nx<Resolution&&nz>=0&&nz<Resolution)yield return nz*Resolution+nx;}
        }
        private void RebuildFlowOrder()
        {
            int count=Ground.Length;var head=new int[count];var next=new int[count];var queue=new int[count];int begin=0,end=0;
            Array.Copy(runoffSource,Discharge,count);
            for(int i=0;i<count;i++){if((i&1023)==0)cancellation.ThrowIfCancellationRequested();head[i]=-1;next[i]=-1;Accumulation[i]=1;}
            for(int i=0;i<count;i++){int r=Receiver[i];if(r<0)queue[end++]=i;else{next[i]=head[r];head[r]=i;}}
            while(begin<end){int i=queue[begin++];FloodOrder[begin-1]=i;for(int j=head[i];j>=0;j=next[j])queue[end++]=j;}
            if(end!=count)throw new InvalidOperationException("Drainage receivers contain a cycle.");
            for(int n=count-1;n>=0;n--){int i=FloodOrder[n],r=Receiver[i];if(r>=0){Accumulation[r]+=Accumulation[i];Discharge[r]+=Discharge[i];}}
        }
        public int BreachRejectedBasins(InlandWaterLayout surfaces)
        {
            int settled=SettleClosedBudgets(surfaces);if(settled>0)return settled;
            var members=new List<int>[LakeCount];for(int i=0;i<Lake.Length;i++)if(Lake[i]>=0){int id=Lake[i];if(members[id]==null)members[id]=new List<int>();members[id].Add(i);}
            int changed=0;bool flowChanged=false,labelsChanged=false;
            for(int id=0;id<members.Length;id++)
            {
                var nodes=members[id];if(nodes==null||surfaces.AcceptsBasin(id))continue;cancellation.ThrowIfCancellationRequested();
                string reason=surfaces.RejectionReason(id);int potential=PotentialLake[nodes[0]];RejectedBasinReasons[potential]=reason;
                BasinOutcome outcome;
                if(reason=="fine-outlet")
                {
                    if(RefineOutlet(nodes)){flowChanged=true;outcome=BasinOutcome.RefinedOutlet;}
                    else
                    {
                        int anchor=BasinAnchor(nodes),padding=BasinPadding(anchor),next=Math.Min(Resolution*2,padding*2);
                        if(next==padding)throw new InvalidOperationException("A lake outlet remains unresolved at the world boundary: basin "+id+", potential "+potential+", nodes "+nodes.Count+", lowest "+anchor+" at "+X(anchor)+","+Y(anchor)+", ground "+Ground[anchor]+", water "+Water[anchor]+". Search:"+outletSearchHistory[anchor]);
                        basinPadding[anchor]=next;outcome=BasinOutcome.FineOutlet;
                    }
                }
                else if(reason=="small-footprint")
                {
                    if(ErodeSmallBasin(nodes)){flowChanged=true;outcome=BasinOutcome.ErodedSmallBasin;}
                    else{int sink=nodes[0];foreach(int node in nodes)if(Ground[node]<Ground[sink])sink=node;ReorientBasin(nodes,sink,Ground[sink],-1);flowChanged=true;outcome=BasinOutcome.SmallFootprint;}
                }
                else if(reason=="covered-by-another-basin"||reason=="sea-level")
                {foreach(int node in nodes)Lake[node]=-1;labelsChanged=true;outcome=reason=="sea-level"?BasinOutcome.SeaLevel:BasinOutcome.Covered;}
                else throw new InvalidOperationException("Unresolved drainage basin: "+reason);
                foreach(int node in nodes)basinOutcome[node]=(byte)outcome;changed++;
            }
            if(flowChanged){RebuildFlowOrder();LabelLakes();BuildPaths();}else if(labelsChanged)BuildPaths();
            return changed;
        }
        public double X(int i) => (i%Resolution)*StepX-width/2;
        public double Y(int i) => (i/Resolution)*StepY-depth/2;
        public bool IsChannelCandidate(int i)=>Receiver[i]>=0&&Ground[i]>sea&&Discharge[i]>=width*depth/(256*256*1000000.0)*12;
        public bool IsRiver(int i)=>IsChannelCandidate(i)&&(acceptedLakes!=null?!acceptedLakes[i]:Lake[i]<0);
        public float RiverWidth(int i) => (float)Math.Min(2200,3+Math.Sqrt(Discharge[i])*2.5);
        public float RiverWidthAt(int i,double t)
        {
            float start=RiverWidth(i);int receiver=Receiver[i];
            if(Configuration.Terrain.Profile!=GeographyProfile.Atlas||receiver<0)return start;
            double f=Math.Max(0,Math.Min(1,t));f=f*f*(3-2*f);
            return (float)(start+(Math.Max(start,RiverWidth(receiver))-start)*f);
        }
        public double CatchmentSquareKilometres(int i)=>Accumulation[i]*cellArea;
        public double SeasonalCatchmentSquareKilometres=>width*depth/(256*1000000.0);
        public static float ChannelDepth(float width,float slope=0)=>(float)Math.Max(.5,Math.Min(18,.6+Math.Sqrt(Math.Max(0,width))*.24+Math.Min(1,slope)*.4));
        public void PathPoint(int i,int step,out double x,out double y)
        {if(pathOffset[i]<0){x=X(i);y=Y(i);return;}int p=pathOffset[i]+step;x=pathX[p];y=pathY[p];}
        public int Nearest(double x,double y)
        {
            int ix=Math.Max(0,Math.Min(Resolution-1,(int)Math.Round((x+width/2)/StepX)));
            int iz=Math.Max(0,Math.Min(Resolution-1,(int)Math.Round((y+depth/2)/StepY)));
            return iz*Resolution+ix;
        }
        public float MoistureInfluence(double x,double y)
        {
            double gx=Math.Max(0,Math.Min(Resolution-1,(x+width/2)/StepX)),gz=Math.Max(0,Math.Min(Resolution-1,(y+depth/2)/StepY));
            int ix=Math.Min(Resolution-2,(int)gx),iz=Math.Min(Resolution-2,(int)gz);
            double tx=gx-ix,tz=gz-iz;
            int a=iz*Resolution+ix,b=a+1,c=a+Resolution,d=c+1;
            double wa=Wet(a)?1:0,wb=Wet(b)?1:0,wc=Wet(c)?1:0,wd=Wet(d)?1:0;
            return (float)((wa+(wb-wa)*tx)*(1-tz)+(wc+(wd-wc)*tx)*tz);
        }
        private bool Wet(int i)=>acceptedWater!=null?acceptedWater[i]:Lake[i]>=0||IsRiver(i);
        public void BindSurface(InlandWaterLayout layout){CanonicalSurfaces=layout;acceptedWater=new bool[Ground.Length];acceptedLakes=new bool[Ground.Length];RiverSegments=0;for(int i=0;i<Ground.Length;i++){acceptedLakes[i]=layout.LakeAt(X(i),Y(i),out _);acceptedWater[i]=acceptedLakes[i]||IsChannelCandidate(i);if(IsRiver(i))RiverSegments++;}}
        public bool IsLakeWater(double x,double y,float ground)
        {
            if(CanonicalSurfaces!=null)return CanonicalSurfaces.LakeAt(x,y,out _);
            int n=Nearest(x,y),cx=n%Resolution,cz=n/Resolution;
            for(int dz=-1;dz<=1;dz++) for(int dx=-1;dx<=1;dx++)
            {int ix=cx+dx,iz=cz+dz;if(ix<0||ix>=Resolution||iz<0||iz>=Resolution) continue;int i=iz*Resolution+ix;if(Lake[i]>=0&&ground<Water[i]&&Math.Abs(x-X(i))<=StepX&&Math.Abs(y-Y(i))<=StepY) return true;}
            return false;
        }
        private double DistanceToPath(int i,double x,double y,out double pathT)
        {
            int first=pathOffset[i];
            // A valid curve point provides an inexpensive initial upper bound.
            // Starting at infinity needlessly refines every successively nearer
            // interval before reaching the useful part of a long river segment.
            int receiver=Receiver[i];double fx=X(receiver)-X(i),fy=Y(receiver)-Y(i);
            pathT=Math.Max(0,Math.Min(1,((x-X(i))*fx+(y-Y(i))*fy)/(fx*fx+fy*fy)));
            double scaled=pathT*PathSamples(i);int seed=Math.Min(PathSamples(i)-1,(int)scaled);double seedU=scaled-seed;
            Coefficients(pathX[first+Math.Max(0,seed-1)],pathX[first+seed],pathX[first+seed+1],pathX[first+Math.Min(PathSamples(i),seed+2)],out double seedXA,out double seedXB,out double seedXC);
            Coefficients(pathY[first+Math.Max(0,seed-1)],pathY[first+seed],pathY[first+seed+1],pathY[first+Math.Min(PathSamples(i),seed+2)],out double seedYA,out double seedYB,out double seedYC);
            double seedX=((seedXA*seedU+seedXB)*seedU+seedXC)*seedU+pathX[first+seed]-x,seedY=((seedYA*seedU+seedYB)*seedU+seedYC)*seedU+pathY[first+seed]-y;
            double closest=seedX*seedX+seedY*seedY;
            // The monotone Hermite curve stays inside each endpoint AABB.
            // Reject only boxes farther than an already evaluated curve point.
            // A smooth centreline avoids the wide Voronoi fans of polyline corners.
            for(int p=0;p<PathSamples(i);p++)
            {
                PathPoint(i,p,out double ax,out double ay);PathPoint(i,p+1,out double bx,out double by);
                double dx=Math.Max(0,Math.Max(Math.Min(ax,bx)-x,x-Math.Max(ax,bx))),dy=Math.Max(0,Math.Max(Math.Min(ay,by)-y,y-Math.Max(ay,by)));
                if(dx*dx+dy*dy>closest)continue;
                int before=first+Math.Max(0,p-1),after=first+Math.Min(PathSamples(i),p+2);
                Coefficients(pathX[before],ax,bx,pathX[after],out double xa,out double xb,out double xc);
                Coefficients(pathY[before],ay,by,pathY[after],out double ya,out double yb,out double yc);
                double sx=bx-ax,sy=by-ay,u=Math.Max(0,Math.Min(1,((x-ax)*sx+(y-ay)*sy)/(sx*sx+sy*sy)));
                for(int iteration=0;iteration<8;iteration++)
                {
                    double px=((xa*u+xb)*u+xc)*u+ax-x,py=((ya*u+yb)*u+yc)*u+ay-y;
                    double vx=(3*xa*u+2*xb)*u+xc,vy=(3*ya*u+2*yb)*u+yc;
                    double denominator=vx*vx+vy*vy+px*(6*xa*u+2*xb)+py*(6*ya*u+2*yb);
                    if(denominator<=1e-12)break;
                    double next=Math.Max(0,Math.Min(1,u-Math.Max(-.5,Math.Min(.5,(px*vx+py*vy)/denominator))));
                    if(Math.Abs(next-u)<1e-10){u=next;break;}u=next;
                }
                double ex=((xa*u+xb)*u+xc)*u+ax-x,ey=((ya*u+yb)*u+yc)*u+ay-y,d=ex*ex+ey*ey;
                if(d<closest){closest=d;pathT=(p+u)/PathSamples(i);}
                d=(ax-x)*(ax-x)+(ay-y)*(ay-y);if(d<closest){closest=d;pathT=p/(double)PathSamples(i);}
                d=(bx-x)*(bx-x)+(by-y)*(by-y);if(d<closest){closest=d;pathT=(p+1)/(double)PathSamples(i);}
            }
            return Math.Sqrt(closest);
        }
        private static double Tangent(double a,double b)=>a*b<=0?0:2*a*b/(a+b);
        private static void Coefficients(double a,double b,double c,double d,out double cubic,out double quadratic,out double linear)
        {linear=Tangent(b-a,c-b);double end=Tangent(c-b,d-c);cubic=2*b-2*c+linear+end;quadratic=-3*b+3*c-2*linear-end;}
        public float Carve(double x,double y,float height)
        {
            // Carry the channel into shallow coastal water. Stopping exactly at
            // datum zero left a metre-scale submerged sill at river mouths.
            if(x< -width/2 || x>width/2 || y< -depth/2 || y>depth/2) return height;
            var candidates=segmentLookup[Nearest(x,y)];if(candidates==null)return height;
            double nearest=double.MaxValue,nearestBed=height,nearestHalf=1,weights=.015,weighted=height*.015;
            foreach(int i in candidates)
            {
                int r=Receiver[i]; double ax=X(i),ay=Y(i),vx=X(r)-ax,vy=Y(r)-ay;
                double dist=DistanceToPath(i,x,y,out double t);
                float widthMeters=RiverWidthAt(i,t),half=widthMeters*0.5f;
                double radius=ValleyRadius(i);if(dist>=radius)continue;
                double surface=PathLevel(i,t);
                double slope=Math.Max(0,Water[i]-Water[r])/Math.Max(1,Math.Sqrt(vx*vx+vy*vy));
                double channelDepth=ChannelDepth(widthMeters,(float)slope);
                // The drainage corridor grades into a broad valley, instead of
                // forcing an arbitrarily deep breach into five channel widths.
                // Local bank rise is bounded by a 0.35 grade; the channel itself
                // has a width-dependent shallow bed. All LODs query this field.
                double beyond=Math.Max(0,dist-half),rounding=Math.Max(12,widthMeters*1.5);
                double valleyRise=.35*(Math.Sqrt(beyond*beyond+rounding*rounding)-rounding);
                double bed=dist<=half?surface-channelDepth+(channelDepth-.12)*Math.Pow(dist/half,2):surface-.12+valleyRise;
                // Compact support is essential: every contribution reaches the
                // unchanged terrain with a zero derivative before its spatial
                // lookup cell can disappear. Normalised weights alone leaked
                // whole drainage rows across the old nearest-cell boundary.
                double u=dist/radius,kernel=Math.Pow(1-u*u,3),scale=Math.Max(half*3,radius*.2),q=dist/scale;
                double weight=kernel/(1+q*q*q*q);weighted+=Math.Min(height,bed)*weight;weights+=weight;
                if(dist<nearest){nearest=dist;nearestBed=bed;nearestHalf=half;}
            }
            if(nearest==double.MaxValue)return height;
            double mix=Math.Max(0,Math.Min(1,(nearest/nearestHalf-1)/2));mix=mix*mix*(3-2*mix);
            return (float)Math.Min(height,nearestBed*(1-mix)+weighted/weights*mix);
        }
        public float BaseElevationAt(double east,double north)
        {
            if(!WorldConfiguration.Finite(east)||!WorldConfiguration.Finite(north))throw new ArgumentException("Coordinates must be finite.");
            if(Configuration.Contains(new GeoPoint(east,north)))return source.ElevationAt(east,north);
            double e=Math.Max(-width*.5,Math.Min(width*.5,east)),n=Math.Max(-depth*.5,Math.Min(depth*.5,north)),distance=Math.Sqrt((east-e)*(east-e)+(north-n)*(north-n));
            return (float)Math.Min(Configuration.Terrain.SeaLevel-50-distance*.04,source.ElevationAt(e,n)-distance*.025);
        }
        public float ElevationAt(double east,double north)=>Carve(east,north,BaseElevationAt(east,north));
        public bool IsRiverWater(double x,double y,float height)
        {
            // A graded valley is dry habitat outside the actual wetted width.
            // Detecting any carved ground incorrectly removed riparian trees,
            // shrubs and resources across the entire conditioned corridor.
            return ChannelSample(x,y,out float surface,out float bank,out _)&&bank<0&&height<surface+.03f&&surface>sea+.03f;
        }
        public bool ChannelSample(double x,double y,out float surface,out float bankDistance,out float widthMeters)
        {
            surface=bankDistance=widthMeters=0;var candidates=segmentLookup[Nearest(x,y)];if(candidates==null)return false;double best=double.MaxValue;
            foreach(int i in candidates)
            {
                double d=DistanceToPath(i,x,y,out double t);if(d<best){best=d;widthMeters=RiverWidthAt(i,t);bankDistance=(float)d-widthMeters*.5f;surface=PathLevel(i,t);}
            }
            return best<widthMeters*3;
        }
        private sealed class MinHeap
        {
            private readonly List<int> ids=new List<int>(); private readonly List<float> keys=new List<float>();
            public int Count=>ids.Count;
            public void Push(int id,float key)
            {
                int p=ids.Count; ids.Add(id);keys.Add(key);
                while(p>0) {int parent=(p-1)/2; if(keys[parent]<key || (keys[parent]==key && ids[parent]<id)) break;
                    ids[p]=ids[parent];keys[p]=keys[parent];p=parent;}
                ids[p]=id;keys[p]=key;
            }
            public int Pop()
            {
                int result=ids[0],last=ids.Count-1,id=ids[last];float key=keys[last];ids.RemoveAt(last);keys.RemoveAt(last);
                if(last==0) return result; int p=0;
                while(p*2+1<last)
                {
                    int c=p*2+1;if(c+1<last && (keys[c+1]<keys[c] || (keys[c+1]==keys[c]&&ids[c+1]<ids[c]))) c++;
                    if(key<keys[c] || (key==keys[c] && id<ids[c])) break;
                    ids[p]=ids[c];keys[p]=keys[c];p=c;
                }
                ids[p]=id;keys[p]=key;return result;
            }
        }
    }
}
