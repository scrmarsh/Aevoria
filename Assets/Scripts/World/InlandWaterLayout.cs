using System;
using System.Collections.Generic;
using System.Threading;

namespace Aevoria.World
{
    public readonly struct WaterPoint
    {
        public readonly double X,Z;
        public readonly float Level,SignedDepth,FlowX,FlowZ,Energy;
        public readonly int ChannelOwner;
        public float Depth=>Math.Max(.01f,SignedDepth);
        public WaterPoint(double x,double z,float level,float depth,float fx=0,float fz=0,float energy=0,int channelOwner=-1)
        {X=x;Z=z;Level=level;SignedDepth=depth;FlowX=fx;FlowZ=fz;Energy=energy;ChannelOwner=channelOwner;}
    }
    public sealed class WaterGeometry
    {
        public readonly List<WaterPoint> Vertices=new List<WaterPoint>();
        public readonly List<int> Indices=new List<int>();
        public void Triangle(WaterPoint a,WaterPoint b,WaterPoint c)
        {int n=Vertices.Count;Vertices.Add(a);Vertices.Add(b);Vertices.Add(c);Indices.Add(n);Indices.Add(n+2);Indices.Add(n+1);}
    }
    // Each lake has one elevation and a connected, terrain-bounded footprint.
    // A candidate that escapes its refinement domain is rejected, never cut off
    // at that rectangle. All rivers use these same ownership queries.
    public sealed class InlandWaterLayout
    {
        public readonly struct LakeBasinSummary
        {
            public double AreaSquareKilometres{get;}public double MeanDepth{get;}public double MaximumDepth{get;}public float Temperature{get;}
            public LakeBasinSummary(double area,double mean,double maximum,float temperature){AreaSquareKilometres=area;MeanDepth=mean;MaximumDepth=maximum;Temperature=temperature;}
        }
        private readonly List<LakeBasinSummary> lakeSummaries=new List<LakeBasinSummary>();
        public IReadOnlyList<LakeBasinSummary> LakeSummaries=>lakeSummaries.AsReadOnly();
        public readonly struct ClosedBasinLevel
        {
            public readonly int PotentialBasin;public readonly float Level;public readonly int[] Nodes;
            public ClosedBasinLevel(int basin,float level,int[] nodes){PotentialBasin=basin;Level=level;Nodes=nodes;}
        }
        private readonly List<ClosedBasinLevel> closedBudgets=new List<ClosedBasinLevel>();
        public IReadOnlyList<ClosedBasinLevel> ClosedBudgets=>closedBudgets;
        public const int Version=9;
        private readonly HashSet<int> terminalPools=new HashSet<int>();
        public bool HasTerminalPool(int node)=>terminalPools.Contains(node);
        public bool IsSeasonallyDryChannel(int node)=>hydrology.CatchmentSquareKilometres(node)<hydrology.SeasonalCatchmentSquareKilometres&&hydrology.Climate.RainfallAt(hydrology.X(node),hydrology.Y(node))<.15f;
        private sealed class ShoreCell
        {
            public int N;public float[] Field;
            public float Sample(double x,double z)
            {double gx=x*N,gz=z*N;int ix=Math.Min(N-1,(int)gx),iz=Math.Min(N-1,(int)gz);double tx=gx-ix,tz=gz-iz;int a=iz*(N+1)+ix;return (float)(tx>=tz?Field[a]*(1-tx)+Field[a+1]*(tx-tz)+Field[a+N+2]*tz:Field[a]*(1-tz)+Field[a+N+2]*tx+Field[a+N+1]*(tz-tx));}
        }
        private sealed class Basin
        {
            public int Id,NX,NZ;public double X,Z,Step;public float Level;public float[] Field;public bool[] Checked;
            public readonly Dictionary<int,ShoreCell> Shores=new Dictionary<int,ShoreCell>();
            public float Sample(double x,double z)
            {
                double gx=(x-X)/Step,gz=(z-Z)/Step;if(gx<0||gz<0||gx>=NX-1||gz>=NZ-1)return -10000;
                int ix=(int)gx,iz=(int)gz;double tx=gx-ix,tz=gz-iz;int a=iz*NX+ix;
                if(Shores.TryGetValue(a,out var shore))return shore.Sample(tx,tz);
                // Match the actual shoreline triangle, rather than bilinearly
                // inventing a different coastline in the ownership query.
                return (float)(tx>=tz?Field[a]*(1-tx)+Field[a+1]*(tx-tz)+Field[a+NX+1]*tz:Field[a]*(1-tz)+Field[a+NX+1]*tx+Field[a+NX]*(tz-tx));
            }
        }
        private readonly HydrologyMap hydrology;
        private readonly Action<string,long,long> report;
        private Dictionary<(double,double),float> terrainSamples;
        private float BaseHeight(double x,double z)
        {
            if(terrainSamples==null)return hydrology.BaseElevationAt(x,z);
            var key=(x,z);if(terrainSamples.TryGetValue(key,out float height))return height;
            height=hydrology.BaseElevationAt(x,z);if(terrainSamples.Count<2000000)terrainSamples.Add(key,height);return height;
        }
        internal void ReleasePreparationCache()=>terrainSamples=null;
        private readonly List<Basin> basins=new List<Basin>();
        private readonly Dictionary<int,string> rejections=new Dictionary<int,string>();
        public string RejectionReason(int id)=>rejections.TryGetValue(id,out var reason)?reason:"not-owned";
        private readonly List<Basin>[] lookup=new List<Basin>[HydrologyMap.Resolution*HydrologyMap.Resolution];
        public readonly WaterGeometry Lakes=new WaterGeometry(),Rivers=new WaterGeometry(),Falls=new WaterGeometry();
        public int Waterfalls {get;private set;}
        public int LakeCount=>basins.Count;
        public int RejectedBasins {get;private set;}
        public double RefinementMeters {get;}
        public InlandWaterLayout(ProceduralGeography geography,CancellationToken token=default,bool buildGeometry=true,Dictionary<(double,double),float> terrainSamples=null,Action<string,long,long> report=null):this(geography.Hydrology,token,buildGeometry,terrainSamples,report){}
        public InlandWaterLayout(HydrologyMap hydrology,CancellationToken token=default,bool buildGeometry=true,Dictionary<(double,double),float> terrainSamples=null,Action<string,long,long> report=null)
        {
            this.hydrology=hydrology??throw new ArgumentNullException(nameof(hydrology));this.report=report;this.terrainSamples=terrainSamples;var h=hydrology;
            RefinementMeters=Math.Max(8,hydrology.Configuration.WidthMeters/4096);
            var groups=new List<int>[h.LakeCount];for(int i=0;i<h.Lake.Length;i++)if(h.Lake[i]>=0){int id=h.Lake[i];if(groups[id]==null)groups[id]=new List<int>();groups[id].Add(i);}
            for(int id=0;id<groups.Length;id++)
            {
                token.ThrowIfCancellationRequested();report?.Invoke("Refining lake basin",id,groups.Length);var nodes=groups[id];if(nodes==null)continue;
                if(LakeAt(h.X(nodes[0]),h.Y(nodes[0]),out _)){rejections[id]="covered-by-another-basin";RejectedBasins++;continue;}
                float level=h.Water[nodes[0]];if(level<=hydrology.Configuration.Terrain.SeaLevel+.05f){rejections[id]="sea-level";RejectedBasins++;continue;}
                float temperature=hydrology.Climate.TemperatureAt(h.X(nodes[0]),h.Y(nodes[0]),hydrology.ElevationAt(h.X(nodes[0]),h.Y(nodes[0])));
                double minX=double.MaxValue,minZ=minX,maxX=double.MinValue,maxZ=maxX;
                foreach(int i in nodes){minX=Math.Min(minX,h.X(i));maxX=Math.Max(maxX,h.X(i));minZ=Math.Min(minZ,h.Y(i));maxZ=Math.Max(maxZ,h.Y(i));}
                // A coarse reservoir may split into many independent fine
                // basins. Sharing its old ID let one basin's expansion enlarge
                // all the others until their seeds and walls were undersampled.
                int padding=h.BasinPadding(h.BasinAnchor(nodes));
                minX=Math.Max(-hydrology.Configuration.WidthMeters/2,minX-h.StepX*2*padding);maxX=Math.Min(hydrology.Configuration.WidthMeters/2,maxX+h.StepX*2*padding);
                minZ=Math.Max(-hydrology.Configuration.DepthMeters/2,minZ-h.StepY*2*padding);maxZ=Math.Min(hydrology.Configuration.DepthMeters/2,maxZ+h.StepY*2*padding);
                double step=Math.Max(RefinementMeters,Math.Max(maxX-minX,maxZ-minZ)/384);
                minX=Math.Floor(minX/step)*step;minZ=Math.Floor(minZ/step)*step;
                int nx=(int)Math.Ceiling((maxX-minX)/step)+1,nz=(int)Math.Ceiling((maxZ-minZ)/step)+1;
                var basin=new Basin{Id=id,X=minX,Z=minZ,Step=step,NX=nx,NZ=nz,Level=level,Field=new float[nx*nz]};
                var seen=new bool[nx*nz];var queue=new Queue<int>();
                basin.Checked=seen;
                foreach(int i in nodes)
                {int x=(int)Math.Round((h.X(i)-minX)/step),z=(int)Math.Round((h.Y(i)-minZ)/step),p=z*nx+x;if(!seen[p]){seen[p]=true;queue.Enqueue(p);}}
                bool escaped=false;int wetCount=0,visitedCount=0;double wetDepth=0,maximumDepth=0;
                while(queue.Count>0)
                {
                    if((visitedCount++&255)==0){token.ThrowIfCancellationRequested();report?.Invoke("Flooding lake basin "+id,visitedCount-1,nx*nz);}
                    int i=queue.Dequeue(),x=i%nx,z=i/nx;
                    float field=level-.02f-BaseHeight(minX+x*step,minZ+z*step);basin.Field[i]=field;
                    if(field<=0)continue;wetCount++;wetDepth+=field;maximumDepth=Math.Max(maximumDepth,field);
                    if(x==0||z==0||x==nx-1||z==nz-1){escaped=true;break;}
                    for(int k=0;k<4;k++){int j=k==0?i-1:k==1?i+1:k==2?i-nx:i+nx;if(!seen[j]){seen[j]=true;queue.Enqueue(j);}}
                }
                // Annual catchment runoff and lake precipitation balance an
                // explicit evaporation proxy. A dry climate lowers a closed
                // lake below its geological sill; it does not dig an outlet
                // through that sill or leave its river hanging at the old level.
                double rainCatchment=0;
                foreach(int node in nodes)
                {
                    int downstream=h.Receiver[node];if(downstream>=0&&h.Lake[downstream]==id)continue;
                    int guard=0;while(downstream>=0&&h.Lake[downstream]<0&&h.Water[downstream]>=level-.02f&&guard++<h.Ground.Length){if((guard&255)==0)token.ThrowIfCancellationRequested();downstream=h.Receiver[downstream];}
                    if(downstream>=0&&h.Lake[downstream]==id)continue;
                    rainCatchment+=h.Discharge[node];
                }
                double annualRain=hydrology.Climate.RainfallAt(h.X(nodes[0]),h.Y(nodes[0]))*hydrology.Climate.Settings.Rainfall*1.2;
                double evaporation=.25+Math.Max(0,temperature)*.025;
                double sustainableArea=rainCatchment*hydrology.Climate.Settings.Rainfall*1.2*.4/Math.Max(.02,evaporation-annualRain*.6);
                if(wetCount>0&&sustainableArea*1000000<wetCount*step*step)
                {
                    var floors=new List<float>();for(int k=0;k<basin.Field.Length;k++)if(seen[k]&&basin.Field[k]>0)floors.Add(level-.02f-basin.Field[k]);floors.Sort();
                    int at=Math.Max(0,Math.Min(floors.Count-1,(int)(sustainableArea*1000000/(step*step))));float equilibrium=Math.Min(level,floors[at]+.025f);
                    if(equilibrium<level-.05f){closedBudgets.Add(new ClosedBasinLevel(h.PotentialLake[nodes[0]],equilibrium,nodes.ToArray()));rejections[id]="water-budget";RejectedBasins++;continue;}
                }
                // Reject tiny unsupported puddles and depressions whose fine
                // terrain reveals an outlet below the old coarse spill level.
                double areaSquareKilometres=wetCount*step*step/1000000;
                bool strongThroughflow=rainCatchment>Math.Max(1,areaSquareKilometres*10);
                bool shallowIsolated=maximumDepth<10&&temperature>5&&!strongThroughflow;
                double minimumArea=shallowIsolated?Math.Max(200000,RefinementMeters*RefinementMeters*8):Math.Max(1200,RefinementMeters*RefinementMeters*4);
                if(shallowIsolated&&wetCount*step*step<minimumArea&&!h.CanErodeSmallBasin(nodes))minimumArea=Math.Max(1200,RefinementMeters*RefinementMeters*4);
                if(h.Configuration.Terrain.Profile==GeographyProfile.Atlas)
                {
                    // Major seas belong to the continental coast field. Inland
                    // lakes need a substantial basin at the scale of the world;
                    // cold/deep glacial basins receive a smaller allowance.
                    double worldArea=h.Configuration.WidthMeters*h.Configuration.DepthMeters;
                    double regionalMinimum=Math.Max(4000000,worldArea*.00015*Math.Max(1,h.Configuration.WidthMeters/128000));
                    if(temperature<3&&maximumDepth>12||maximumDepth>35)regionalMinimum*=.25;
                    minimumArea=Math.Max(minimumArea,regionalMinimum);
                }
                if(escaped||wetCount*step*step<minimumArea){rejections[id]=escaped?"fine-outlet":"small-footprint";RejectedBasins++;continue;}
                for(int i=0;i<basin.Field.Length;i++)if(!seen[i])basin.Field[i]=-.02f;
                basins.Add(basin);
                lakeSummaries.Add(new LakeBasinSummary(wetCount*step*step/1000000,wetDepth/Math.Max(1,wetCount),maximumDepth,temperature));
                int ix0=Math.Max(0,(int)((minX+hydrology.Configuration.WidthMeters/2)/h.StepX)-1),ix1=Math.Min(HydrologyMap.Resolution-1,(int)((minX+(nx-1)*step+hydrology.Configuration.WidthMeters/2)/h.StepX)+1);
                int iz0=Math.Max(0,(int)((minZ+hydrology.Configuration.DepthMeters/2)/h.StepY)-1),iz1=Math.Min(HydrologyMap.Resolution-1,(int)((minZ+(nz-1)*step+hydrology.Configuration.DepthMeters/2)/h.StepY)+1);
                for(int z=iz0;z<=iz1;z++)for(int x=ix0;x<=ix1;x++){int i=z*HydrologyMap.Resolution+x;if(lookup[i]==null)lookup[i]=new List<Basin>();lookup[i].Add(basin);}
            }
            hydrology.BindSurface(this);
            if(buildGeometry)PrepareGeometry(token);
        }
        private bool geometryBuilt;
        public void PrepareGeometry(CancellationToken token=default)
        {
            if(geometryBuilt)return;
            // Basin acceptance and spill routing use the coarse connected flood
            // footprint. Build the detailed, shared geometry/query shoreline
            // only after those basin decisions have settled.
            RefineShores(token);hydrology.BindSurface(this);
            for(int i=0;i<basins.Count;i++){token.ThrowIfCancellationRequested();report?.Invoke("Triangulating lake basins",i,basins.Count);BuildLake(basins[i],token);}
            BuildRivers(token);BuildFalls(token);geometryBuilt=true;
        }
        public int ShoreSamples {get;private set;}
        private void RefineShores(CancellationToken token)
        {
            var boundaries=new List<(Basin basin,int x,int z)>();
            foreach(var b in basins)for(int z=0;z<b.NZ-1;z++)for(int x=0;x<b.NX-1;x++)
            {if(x==0){token.ThrowIfCancellationRequested();report?.Invoke("Locating refined lake shores",z,b.NZ-1);}int i=z*b.NX+x;float a=b.Field[i],c=b.Field[i+1],d=b.Field[i+b.NX],e=b.Field[i+b.NX+1];if(Math.Max(Math.Max(a,c),Math.Max(d,e))>0&&Math.Min(Math.Min(a,c),Math.Min(d,e))<=0)boundaries.Add((b,x,z));}
            // Shared geometry/query refinement, bounded independently of world
            // area. Interiors stay coarse; only the physical shoreline gets a
            // denser field. Fine triangles and ownership use identical values.
            int common=Math.Max(1,Math.Min(128,(int)Math.Sqrt(16000000.0/Math.Max(1,boundaries.Count))-1));
            foreach(var entry in boundaries)
            {
                token.ThrowIfCancellationRequested();var b=entry.basin;int n=Math.Max(1,Math.Min(common,(int)Math.Ceiling(b.Step/12)));if(n==1)continue;
                var shore=new ShoreCell{N=n,Field=new float[(n+1)*(n+1)]};
                for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)shore.Field[z*(n+1)+x]=b.Level-.02f-BaseHeight(b.X+(entry.x+x/(double)n)*b.Step,b.Z+(entry.z+z/(double)n)*b.Step);
                b.Shores.Add(entry.z*b.NX+entry.x,shore);ShoreSamples+=shore.Field.Length;
            }
        }
        private void BuildFalls(CancellationToken token)
        {
            var h=hydrology;
            for(int i=0;i<h.Ground.Length&&Waterfalls<128;i++)if(h.IsChannelCandidate(i))
            {
                token.ThrowIfCancellationRequested();int r=h.Receiver[i];double dx=h.X(r)-h.X(i),dz=h.Y(r)-h.Y(i),length=Math.Sqrt(dx*dx+dz*dz);float drop=h.Water[i]-h.Water[r];
                if(drop<=30||drop<=length*.7||length<1)continue;
                h.PathPoint(i,7,out double x,out double z);float width=h.RiverWidth(i)*.5f;
                if(LakeAt(x,z,out _)||LakeAt(x+dz/length*width,z-dx/length*width,out _)||LakeAt(x-dz/length*width,z+dx/length*width,out _))continue;
                float bottom=Math.Max(h.Water[r],hydrology.ElevationAt(x,z)+.1f),top=h.Water[i];if(top-bottom<3)continue;
                var a=new WaterPoint(x-dz/length*width,z+dx/length*width,top,2,(float)(dx/length),(float)(dz/length),1);
                var b=new WaterPoint(x+dz/length*width,z-dx/length*width,top,2,(float)(dx/length),(float)(dz/length),1);
                var c=new WaterPoint(b.X,b.Z,bottom,2,b.FlowX,b.FlowZ,1);var d=new WaterPoint(a.X,a.Z,bottom,2,a.FlowX,a.FlowZ,1);
                Falls.Triangle(a,b,c);Falls.Triangle(a,c,d);Waterfalls++;
            }
        }
        public bool AcceptsBasin(int id){foreach(var b in basins)if(b.Id==id)return true;return false;}
        public bool LakeAt(double x,double z,out float level)
        {
            level=0;var candidates=lookup[hydrology.Nearest(x,z)];if(candidates==null)return false;
            foreach(var b in candidates)if(b.Sample(x,z)>0){level=b.Level;return true;}return false;
        }
        public float LakeField(double x,double z)
        {float field=-10000;var candidates=lookup[hydrology.Nearest(x,z)];if(candidates!=null)foreach(var b in candidates)field=Math.Max(field,b.Sample(x,z));return field;}
        public bool LakeBank(double x,double z,out float level)
        {level=0;var candidates=lookup[hydrology.Nearest(x,z)];if(candidates==null)return false;foreach(var b in candidates){int ix=(int)Math.Round((x-b.X)/b.Step),iz=(int)Math.Round((z-b.Z)/b.Step);if(ix>=0&&iz>=0&&ix<b.NX&&iz<b.NZ&&b.Checked[iz*b.NX+ix]){level=b.Level;return true;}}return false;}
        public bool RiverVisible(double x,double z,float level)=>!LakeAt(x,z,out _)&&level>hydrology.Configuration.Terrain.SeaLevel+.03f&&hydrology.ElevationAt(x,z)<level;
        private WaterPoint Point(double x,double z,float level,float fx=0,float fz=0,float energy=0,int owner=-1)=>new WaterPoint(x,z,level+.035f,level-hydrology.Carve(x,z,BaseHeight(x,z)),fx,fz,energy,owner);
        private WaterPoint LakePoint(Basin b,double x,double z)=>new WaterPoint(x,z,b.Level+.035f,b.Sample(x,z)+.02f);
        private void BuildLake(Basin b,CancellationToken token)
        {
            for(int z=0;z<b.NZ-1;z+=8)for(int x=0;x<b.NX-1;x+=8)
                BuildLakePatch(b,x,z,Math.Min(8,b.NX-1-x),Math.Min(8,b.NZ-1-z),token);
        }
        private void BuildLakePatch(Basin b,int x,int z,int width,int depth,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();bool interior=true,dry=true;
            for(int row=z;row<=z+depth;row++)for(int column=x;column<=x+width;column++)
            {float field=b.Field[row*b.NX+column];interior&=field>2;dry&=field<=0;}
            for(int row=z;row<z+depth;row++)for(int column=x;column<x+width;column++)
                if(b.Shores.ContainsKey(row*b.NX+column)){interior=false;dry=false;}
            if(dry)return;
            if(!interior&&(width>1||depth>1))
            {
                int w=width>1?width/2:width,d=depth>1?depth/2:depth;
                BuildLakePatch(b,x,z,w,d,token);
                if(width>w)BuildLakePatch(b,x+w,z,width-w,d,token);
                if(depth>d)BuildLakePatch(b,x,z+d,w,depth-d,token);
                if(width>w&&depth>d)BuildLakePatch(b,x+w,z+d,width-w,depth-d,token);return;
            }
            int at=z*b.NX+x;
            if(b.Shores.TryGetValue(at,out var shore)){BuildShore(b,shore,x,z,0,0,shore.N,shore.N);return;}
            // Merge only completely wet, shore-free planar interiors. The
            // canonical shoreline/query field is unchanged at its full detail.
            var a=LakePoint(b,b.X+x*b.Step,b.Z+z*b.Step);var c=LakePoint(b,a.X+width*b.Step,a.Z+depth*b.Step);var right=LakePoint(b,c.X,a.Z);var up=LakePoint(b,a.X,c.Z);
            Clip(Lakes,a,right,c,b.Field[at],b.Field[at+width],b.Field[(z+depth)*b.NX+x+width]);
            Clip(Lakes,a,c,up,b.Field[at],b.Field[(z+depth)*b.NX+x+width],b.Field[(z+depth)*b.NX+x]);
        }
        private void BuildShore(Basin b,ShoreCell shore,int cellX,int cellZ,int x,int z,int width,int depth)
        {
            int row=shore.N+1;bool wet=true,dry=true;
            for(int iz=z;iz<=z+depth;iz++)for(int ix=x;ix<=x+width;ix++){float f=shore.Field[iz*row+ix];wet&=f>=0;dry&=f<=0;}
            if(dry)return;
            if(!wet&&(width>1||depth>1))
            {
                int w=width>1?width/2:width,d=depth>1?depth/2:depth;
                BuildShore(b,shore,cellX,cellZ,x,z,w,d);
                if(width>w)BuildShore(b,shore,cellX,cellZ,x+w,z,width-w,d);
                if(depth>d)BuildShore(b,shore,cellX,cellZ,x,z+d,w,depth-d);
                if(width>w&&depth>d)BuildShore(b,shore,cellX,cellZ,x+w,z+d,width-w,depth-d);return;
            }
            double step=b.Step/shore.N;var a=LakePoint(b,b.X+cellX*b.Step+x*step,b.Z+cellZ*b.Step+z*step);var c=LakePoint(b,a.X+width*step,a.Z+depth*step);var right=LakePoint(b,c.X,a.Z);var up=LakePoint(b,a.X,c.Z);
            Clip(Lakes,a,right,c,shore.Field[z*row+x],shore.Field[z*row+x+width],shore.Field[(z+depth)*row+x+width]);Clip(Lakes,a,c,up,shore.Field[z*row+x],shore.Field[(z+depth)*row+x+width],shore.Field[(z+depth)*row+x]);
        }
        private void BuildRivers(CancellationToken token)
        {
            var h=hydrology;var terminals=new SortedDictionary<int,int>();
            for(int i=0;i<h.Ground.Length;i++)if(h.IsChannelCandidate(i))
            {
                if(IsSeasonallyDryChannel(i))continue;
                token.ThrowIfCancellationRequested();report?.Invoke("Triangulating connected river segments",i,h.Ground.Length);int receiver=h.Receiver[i];float width=Math.Max(h.RiverWidthAt(i,0),h.RiverWidthAt(i,1))*.5f;
                if(h.Receiver[receiver]<0&&h.Ground[receiver]>hydrology.Configuration.Terrain.SeaLevel&&!LakeAt(h.X(receiver),h.Y(receiver),out _))
                {
                    var outcome=h.OutcomeAt(receiver);
                    if((outcome==HydrologyMap.BasinOutcome.ClosedWaterBudget||outcome==HydrologyMap.BasinOutcome.SmallFootprint)&&(!terminals.TryGetValue(receiver,out int incoming)||h.Discharge[i]>h.Discharge[incoming]))terminals[receiver]=i;
                }
                float fall=Math.Max(0,h.Water[i]-h.Water[receiver]),energy=(float)Math.Min(1,fall/Math.Max(1,h.StepX)*8);
                var centre=new List<(double x,double z,double t)>();
                for(int p=0;p<h.PathSamples(i);p++)
                {
                    h.PathPoint(i,p,out double ax,out double az);h.PathPoint(i,p+1,out double bx,out double bz);
                    double length=Math.Sqrt((bx-ax)*(bx-ax)+(bz-az)*(bz-az));
                    int subdivisions=Math.Max(1,(int)Math.Ceiling(length/Math.Max(8,RefinementMeters)));
                    for(int k=0;k<subdivisions;k++){double f=k/(double)subdivisions;centre.Add((ax+(bx-ax)*f,az+(bz-az)*f,(p+f)/h.PathSamples(i)));}
                }
                h.PathPoint(i,h.PathSamples(i),out double endX,out double endZ);centre.Add((endX,endZ,1));
                centre=SimplifyRiverCentre(centre,i,Math.Max(.25,Math.Min(1,width*.01)),token);
                // One shared cross-section at every centreline sample. Separate
                // segment normals left triangular gaps at every bend and also
                // repeated most expensive canonical depth queries several times.
                int across=Math.Max(4,2*(int)Math.Ceiling(width/Math.Max(8,RefinementMeters)));
                var previous=new WaterPoint[across+1];var row=new WaterPoint[across+1];bool hasPrevious=false;
                for(int k=0;k<centre.Count;k++)
                {
                    if((k&31)==0){token.ThrowIfCancellationRequested();report?.Invoke("River cross-sections at node "+i,k,centre.Count);}
                    var point=centre[k];var before=centre[Math.Max(0,k-1)];var after=centre[Math.Min(centre.Count-1,k+1)];
                    double dx=after.x-before.x,dz=after.z-before.z;
                    if(k==0||k==centre.Count-1){h.JunctionDirection(k==0?i:receiver,out double joinX,out double joinZ);if(joinX*joinX+joinZ*joinZ>1e-12){dx=joinX;dz=joinZ;}}
                    double length=Math.Sqrt(dx*dx+dz*dz);if(length<1e-6)continue;
                    float fx=(float)(dx/length),fz=(float)(dz/length),level=h.PathLevel(i,point.t);
                    float sectionWidth=h.RiverWidthAt(i,point.t)*.5f;
                    for(int strip=0;strip<=across;strip++){double offset=-sectionWidth+2*sectionWidth*strip/across;row[strip]=Point(point.x+fz*offset,point.z-fx*offset,level,fx,fz,energy,i);}
                    if(hasPrevious)for(int strip=0;strip<across;strip++){ClipRiver(previous[strip],previous[strip+1],row[strip+1]);ClipRiver(previous[strip],row[strip+1],row[strip]);}
                    var swap=previous;previous=row;row=swap;hasPrevious=true;
                }
            }
            foreach(var pair in terminals)
            {
                token.ThrowIfCancellationRequested();int root=pair.Key,branch=pair.Value,steps=h.PathSamples(branch);
                h.PathPoint(branch,steps-1,out double beforeX,out double beforeZ);h.PathPoint(branch,steps,out double x,out double z);
                double dx=x-beforeX,dz=z-beforeZ;h.JunctionDirection(root,out double joinX,out double joinZ);if(joinX*joinX+joinZ*joinZ>1e-12){dx=joinX;dz=joinZ;}
                double length=Math.Sqrt(dx*dx+dz*dz);if(length<1e-6)continue;dx/=length;dz/=length;
                double radius=h.RiverWidth(branch)*.5;int segments=Math.Max(12,Math.Min(64,(int)Math.Ceiling(Math.PI*radius/4)));
                var centre=Point(x,z,h.Water[root],(float)dx,(float)dz,owner:branch);int first=Rivers.Vertices.Count;
                // The carved channel already has a round end in its distance
                // field. Complete the downstream half of that same wet bed;
                // a ribbon's straight final cross-section left it uncovered.
                for(int k=0;k<segments;k++)
                {
                    double a=-Math.PI*.5+k*Math.PI/segments,b=-Math.PI*.5+(k+1)*Math.PI/segments;
                    var p=Point(x+radius*(dx*Math.Cos(a)+dz*Math.Sin(a)),z+radius*(dz*Math.Cos(a)-dx*Math.Sin(a)),h.Water[root],owner:branch);
                    var q=Point(x+radius*(dx*Math.Cos(b)+dz*Math.Sin(b)),z+radius*(dz*Math.Cos(b)-dx*Math.Sin(b)),h.Water[root],owner:branch);
                    ClipRiver(centre,p,q);
                }
                if(Rivers.Vertices.Count>first)terminalPools.Add(root);
            }
        }
        private List<(double x,double z,double t)> SimplifyRiverCentre(List<(double x,double z,double t)> points,int node,double tolerance,CancellationToken token)
        {
            if(points.Count<3)return points;var keep=new bool[points.Count];keep[0]=keep[points.Count-1]=true;
            var pending=new Stack<(int first,int last)>();pending.Push((0,points.Count-1));var h=hydrology;
            while(pending.Count>0)
            {
                token.ThrowIfCancellationRequested();var span=pending.Pop();if(span.last-span.first<2)continue;
                var a=points[span.first];var b=points[span.last];double levelA=h.PathLevel(node,a.t),levelB=h.PathLevel(node,b.t),worst=1;int split=-1;
                for(int j=span.first+1;j<span.last;j++)
                {
                    var p=points[j];double t=(p.t-a.t)/(b.t-a.t),dx=p.x-(a.x+(b.x-a.x)*t),dz=p.z-(a.z+(b.z-a.z)*t);
                    double error=Math.Max(Math.Sqrt(dx*dx+dz*dz)/tolerance,Math.Abs(h.PathLevel(node,p.t)-(levelA+(levelB-levelA)*t))/.02);
                    if(error>worst){worst=error;split=j;}
                }
                if(split<0&&(b.x-a.x)*(b.x-a.x)+(b.z-a.z)*(b.z-a.z)>128*128)split=(span.first+span.last)/2;
                if(split<0)continue;keep[split]=true;pending.Push((span.first,split));pending.Push((split,span.last));
            }
            var output=new List<(double x,double z,double t)>();for(int i=0;i<points.Count;i++)if(keep[i])output.Add(points[i]);return output;
        }
        private float RiverField(WaterPoint p)=>Math.Min(Math.Min(-LakeField(p.X,p.Z),p.SignedDepth+.025f),p.Level-hydrology.Configuration.Terrain.SeaLevel-.04f);
        private void ClipRiver(WaterPoint a,WaterPoint b,WaterPoint c,int refinement=0)
        {
            int first=Rivers.Vertices.Count;Clip(Rivers,a,b,c,RiverField(a),RiverField(b),RiverField(c));bool overlap=false;
            for(int i=first;i<Rivers.Vertices.Count;i+=3)
            {var p=Rivers.Vertices[i];var q=Rivers.Vertices[i+1];var r=Rivers.Vertices[i+2];if(LakeAt((p.X+q.X+r.X)/3,(p.Z+q.Z+r.Z)/3,out _)){overlap=true;break;}}
            if(!overlap)return;
            Rivers.Indices.RemoveRange(first,Rivers.Indices.Count-first);Rivers.Vertices.RemoveRange(first,Rivers.Vertices.Count-first);
            // Resolve triangles crossing a piecewise shoreline field boundary.
            // At the final sub-metre boundary sliver the lake owns the surface.
            if(refinement>=5)return;
            WaterPoint Mid(WaterPoint p,WaterPoint q){var m=Mix(p,q,.5f);return Point(m.X,m.Z,m.Level-.035f,m.FlowX,m.FlowZ,m.Energy,m.ChannelOwner);}
            var ab=Mid(a,b);var bc=Mid(b,c);var ca=Mid(c,a);
            ClipRiver(a,ab,ca,refinement+1);ClipRiver(ab,b,bc,refinement+1);ClipRiver(ca,bc,c,refinement+1);ClipRiver(ab,bc,ca,refinement+1);
        }
        private static WaterPoint Mix(WaterPoint a,WaterPoint b,float t)=>new WaterPoint(a.X+(b.X-a.X)*t,a.Z+(b.Z-a.Z)*t,a.Level+(b.Level-a.Level)*t,a.SignedDepth+(b.SignedDepth-a.SignedDepth)*t,a.FlowX+(b.FlowX-a.FlowX)*t,a.FlowZ+(b.FlowZ-a.FlowZ)*t,a.Energy+(b.Energy-a.Energy)*t,a.ChannelOwner);
        private static void Clip(WaterGeometry mesh,WaterPoint a,WaterPoint b,WaterPoint c,float da,float db,float dc)
        {
            // Stack-sized cases avoid a List/array allocation for every triangle.
            if(da>=0&&db>=0&&dc>=0){mesh.Triangle(a,b,c);return;}if(da<0&&db<0&&dc<0)return;
            if(da<0&&db>=0&&dc>=0){var ab=Mix(a,b,da/(da-db));var ac=Mix(a,c,da/(da-dc));mesh.Triangle(ab,b,c);mesh.Triangle(ab,c,ac);}
            else if(db<0&&da>=0&&dc>=0)Clip(mesh,b,c,a,db,dc,da);
            else if(dc<0&&da>=0&&db>=0)Clip(mesh,c,a,b,dc,da,db);
            else if(da>=0)mesh.Triangle(a,Mix(a,b,da/(da-db)),Mix(a,c,da/(da-dc)));
            else if(db>=0)Clip(mesh,b,c,a,db,dc,da);else Clip(mesh,c,a,b,dc,da,db);
        }
    }
}
