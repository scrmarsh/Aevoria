using System;
using System.Collections.Generic;
using System.Threading;

namespace Aevoria.World
{
    public enum CoastalInletKind { Cove, Fjord }
    public sealed class CoastalInlet
    {
        public CoastalInletKind Kind{get;}public IReadOnlyList<FaultPoint> Points{get;}public double Width{get;}public double Depth{get;}
        internal CoastalInlet(CoastalInletKind kind,FaultPoint[] points,double width,double depth){Kind=kind;Points=Array.AsReadOnly(points);Width=width;Depth=depth;}
        internal double Cut(double x,double z,double original)
        {
            double best=double.MaxValue,along=0;
            for(int i=1;i<Points.Count;i++)
            {
                var a=Points[i-1];var b=Points[i];double dx=b.X-a.X,dz=b.Z-a.Z,l=dx*dx+dz*dz;if(l<1)continue;
                double t=Math.Max(0,Math.Min(1,((x-a.X)*dx+(z-a.Z)*dz)/l)),ex=x-a.X-t*dx,ez=z-a.Z-t*dz,d=ex*ex+ez*ez;
                if(d<best){best=d;along=(i-1+t)/(Points.Count-1);}
            }
            double width=Width*(1-.45*ContinentalField.Smooth(.4,1,along)),distance=Math.Sqrt(best);
            if(distance>=width*2.2)return original;
            // Overdeepened U-shaped floor with a rising glacial head. The
            // offshore end joins real bathymetry, rather than a water overlay.
            double bed=-Depth*(.65+.35*Math.Sin(along*Math.PI));
            double head=ContinentalField.Smooth(.78,1,along),floor=bed+(Math.Max(original,0)-bed)*head;
            double inner=width*(Kind==CoastalInletKind.Cove?1.15:.32);
            double weight=1-ContinentalField.Smooth(inner,width*2.2,distance);
            return original-Math.Max(0,original-floor)*weight;
        }
    }
    public sealed class CoastalLandformField
    {
        public const int Version=1;private const double Bin=12000;
        private readonly ContinentalField continents;private readonly uint seed;private readonly Dictionary<(int,int),List<CoastalInlet>> bins=new Dictionary<(int,int),List<CoastalInlet>>();
        private readonly List<CoastalInlet> inlets=new List<CoastalInlet>();public IReadOnlyList<CoastalInlet> Inlets=>inlets.AsReadOnly();
        public CoastalLandformField(WorldConfiguration config,ContinentalField continents,BaseElevationField ordinary,MountainClusterField mountains,TectonicStructureField tectonics,ulong moduleSeed,CancellationToken token=default,Action<string,long,long> report=null)
        {
            this.continents=continents;seed=(uint)(moduleSeed^(moduleSeed>>32));double step=Math.Min(config.WidthMeters,config.DepthMeters)/64;
            double Height(double x,double z)=>ordinary.ElevationAt(x,z)+mountains.ContributionAt(x,z);
            int maxFjords=Math.Max(3,Math.Min(48,(int)(config.WidthMeters/75000))),fjords=0,coves=0;
            for(int row=1;row<64;row++)
            {
                token.ThrowIfCancellationRequested();report?.Invoke("Coastal valleys and submerged shelves",row,64);
                for(int column=1;column<64;column++)
                {
                    double x=(column/64.0-.5)*config.WidthMeters,z=(row/64.0-.5)*config.DepthMeters,d=continents.CoastDistanceAt(x,z);if(Math.Abs(d)>step*.75)continue;
                    var random=new WorldGenerationRandom(WorldGenerationSeeds.Derive(config.Seed,"coast/site/"+(row*64+column),Version));if(random.Next01()<.78)continue;
                    double probe=Math.Max(150,step*.15),gx=continents.CoastDistanceAt(x+probe,z)-continents.CoastDistanceAt(x-probe,z),gz=continents.CoastDistanceAt(x,z+probe)-continents.CoastDistanceAt(x,z-probe),length=Math.Sqrt(gx*gx+gz*gz);if(length<.01)continue;gx/=length;gz/=length;
                    // Project to the actual coast, then start the channel in
                    // the sea so neither its water nor its bed has a sealed mouth.
                    for(int iteration=0;iteration<5;iteration++){double coast=continents.CoastDistanceAt(x,z);x-=gx*coast;z-=gz*coast;}
                    bool cold=z>config.DepthMeters*.18;
                    double inland=Height(x+gx*9000,z+gz*9000),mountainSupport=mountains.ContributionAt(x+gx*9000,z+gz*9000);
                    bool glacial=cold&&inland>500&&mountainSupport>450&&fjords<maxFjords;
                    if(!glacial&&(cold||z>config.DepthMeters*.12||coves>=maxFjords*2))continue;
                    double reach=glacial?Math.Min(config.WidthMeters*.16,9000+random.Next01()*18000):600+random.Next01()*800,width=glacial?400+random.Next01()*650:250+random.Next01()*300;
                    bool separate=true;foreach(var other in inlets){double dx=x-other.Points[1].X,dz=z-other.Points[1].Z;if(dx*dx+dz*dz<reach*reach*.45){separate=false;break;}}if(!separate)continue;
                    var points=new FaultPoint[13];points[0]=new FaultPoint(x-gx*width*3,z-gz*width*3);points[1]=new FaultPoint(x,z);
                    double px=x,pz=z,segment=reach/11,tx=gx,tz=gz;
                    for(int i=2;i<points.Length;i++)
                    {
                        // Select an ascending valley corridor from the actual
                        // physical mass, with a small bias along geological strike.
                        var structure=tectonics.Sample(px,pz);double best=double.MaxValue,bx=gx,bz=gz;
                        for(int turn=-2;turn<=2;turn++)
                        {
                            double angle=Math.Atan2(tz,tx)+turn*.22,dx=Math.Cos(angle),dz=Math.Sin(angle);if(dx*gx+dz*gz<.35)continue;
                            double nextX=px+dx*segment,nextZ=pz+dz*segment,coast=continents.CoastDistanceAt(nextX,nextZ);
                            double expected=(i-1)*segment*.55;
                            double h=Height(nextX,nextZ),alignment=Math.Abs(dx*structure.StrikeX+dz*structure.StrikeZ),score=h-alignment*80+Math.Abs(turn)*20+Math.Max(0,expected-coast)*3;
                            if(score<best){best=score;bx=dx;bz=dz;}
                        }
                        tx=bx;tz=bz;px+=tx*segment;pz+=tz*segment;points[i]=new FaultPoint(px,pz);
                    }
                    if(!glacial)
                    {
                        double bend=(random.Next01()-.5)*width;
                        for(int i=2;i<points.Length;i++){double offset=Math.Sin((i-1)/11.0*Math.PI)*bend;points[i]=new FaultPoint(points[i].X-gz*offset,points[i].Z+gx*offset);}
                    }
                    // Do not label a second bay crossing or a flat shoreline
                    // trench as a fjord. Its interior must occupy actual land
                    // and have supporting mountain walls beside the valley.
                    bool supported=true;
                    for(int i=2;i<=9;i++)if(continents.CoastDistanceAt(points[i].X,points[i].Z)<Math.Min(width*.35,(i-1)*segment*.3)){supported=false;break;}
                    if(!supported)continue;
                    if(glacial)
                    {
                        var mid=points[6];var before=points[5];var after=points[7];double vx=after.X-before.X,vz=after.Z-before.Z,l=Math.Sqrt(vx*vx+vz*vz);vx/=l;vz/=l;
                        double left=mountains.ContributionAt(mid.X-vz*width*2.5,mid.Z+vx*width*2.5),right=mountains.ContributionAt(mid.X+vz*width*2.5,mid.Z-vx*width*2.5);
                        if(Math.Min(left,right)<120||Math.Max(left,right)<450)continue;
                    }
                    var inlet=new CoastalInlet(glacial?CoastalInletKind.Fjord:CoastalInletKind.Cove,points,width,glacial?180+random.Next01()*650:8+random.Next01()*25);inlets.Add(inlet);if(glacial)fjords++;else coves++;
                    double minX=double.MaxValue,minZ=minX,maxX=double.MinValue,maxZ=maxX;foreach(var p in points){minX=Math.Min(minX,p.X);maxX=Math.Max(maxX,p.X);minZ=Math.Min(minZ,p.Z);maxZ=Math.Max(maxZ,p.Z);}
                    for(int bz=(int)Math.Floor((minZ-width*2.2)/Bin);bz<=(int)Math.Floor((maxZ+width*2.2)/Bin);bz++)for(int bx=(int)Math.Floor((minX-width*2.2)/Bin);bx<=(int)Math.Floor((maxX+width*2.2)/Bin);bx++){if(!bins.TryGetValue((bx,bz),out var list))bins[(bx,bz)]=list=new List<CoastalInlet>();list.Add(inlet);}
                }
            }
        }
        public float Shape(double x,double z,float height)
        {
            double d=continents.CoastDistanceAt(x,z),result=height;
            if(d>0&&d<1600)
            {
                double province=ContinentalField.Smooth(.05,.45,GradientField.Sample(x/11000,z/11000,seed,881));
                result+=province*85*ContinentalField.Smooth(0,65,d)*(1-ContinentalField.Smooth(180,1600,d));
            }
            if(bins.TryGetValue(((int)Math.Floor(x/Bin),(int)Math.Floor(z/Bin)),out var features))foreach(var inlet in features)result=Math.Min(result,inlet.Cut(x,z,result));
            return (float)result;
        }
    }
}
