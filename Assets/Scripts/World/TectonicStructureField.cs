using System;
using System.Collections.Generic;
using System.Threading;
namespace Aevoria.World
{
    public readonly struct FaultPoint
    {
        public readonly double X,Z;public FaultPoint(double x,double z){X=x;Z=z;}
    }
    public sealed class FaultTrace
    {
        public readonly string Id;public readonly BoundaryKind Kind;public readonly double Width,Displacement,Age;
        public IReadOnlyList<FaultPoint> Points{get;}
        internal FaultTrace(string id,BoundaryKind kind,double width,double displacement,double age,FaultPoint[] points){Id=id;Kind=kind;Width=width;Displacement=displacement;Age=age;Points=Array.AsReadOnly(points);}
        public double DistanceSquared(double x,double z,out double tangentX,out double tangentZ,out double signed)
        {
            double best=double.MaxValue;tangentX=1;tangentZ=0;signed=0;
            for(int i=1;i<Points.Count;i++)
            {
                var a=Points[i-1];var b=Points[i];double dx=b.X-a.X,dz=b.Z-a.Z,length=dx*dx+dz*dz,t=Math.Max(0,Math.Min(1,((x-a.X)*dx+(z-a.Z)*dz)/length)),ex=x-a.X-t*dx,ez=z-a.Z-t*dz,d=ex*ex+ez*ez;
                if(d>=best)continue;best=d;double l=Math.Sqrt(length);tangentX=dx/l;tangentZ=dz/l;signed=ex*(-tangentZ)+ez*tangentX;
            }
            return best;
        }
    }
    public readonly struct TectonicSample
    {
        public readonly float Uplift,Rift,Escarpment,BeltStrength,Age,StrikeX,StrikeZ;
        public TectonicSample(float up,float rift,float escarpment,float belt,float age,float sx,float sz){Uplift=up;Rift=rift;Escarpment=escarpment;BeltStrength=belt;Age=age;StrikeX=sx;StrikeZ=sz;}
    }
    // A tectonic-style structural proxy, not a simulated geological chronology.
    // The same immutable field drives ordinary land, massifs and later soils.
    public sealed class TectonicStructureField
    {
        public const int Version=1,Resolution=257;
        private readonly double width,depth;private readonly uint seed;
        private readonly List<FaultTrace> faults=new List<FaultTrace>();public IReadOnlyList<FaultTrace> Faults{get;}
        private readonly float[] uplift=new float[Resolution*Resolution],rift=new float[Resolution*Resolution],escarpment=new float[Resolution*Resolution],belt=new float[Resolution*Resolution],age=new float[Resolution*Resolution],strikeX=new float[Resolution*Resolution],strikeZ=new float[Resolution*Resolution];
        public long FieldBytes=>(long)Resolution*Resolution*7*sizeof(float);
        private double Noise(double x,double z,double scale,uint channel)=>GradientField.Sample(x/scale,z/scale,seed,channel);
        public TectonicStructureField(WorldConfiguration config,ContinentalField continents,GeologyMap geology,ulong moduleSeed,CancellationToken token=default,Action<string,long,long> checkpoint=null)
        {
            if(config==null||continents==null||geology==null)throw new ArgumentNullException();width=config.WidthMeters;depth=config.DepthMeters;seed=(uint)(moduleSeed^(moduleSeed>>32));var plates=geology.Plates;
            double spacing=Math.Sqrt(width*depth/plates.Count);
            for(int a=0;a<plates.Count;a++)for(int b=a+1;b<plates.Count;b++)
            {
                token.ThrowIfCancellationRequested();var pa=plates[a];var pb=plates[b];double nx=pb.X-pa.X,nz=pb.Z-pa.Z,d=Math.Sqrt(nx*nx+nz*nz);if(d<1e-9||d>1.65/Math.Sqrt(plates.Count))continue;
                double mx=(pa.X+pb.X)*.5,mz=(pa.Z+pb.Z)*.5;bool neighbours=true;
                for(int c=0;c<plates.Count;c++)if(c!=a&&c!=b){double ex=mx-plates[c].X,ez=mz-plates[c].Z;if(ex*ex+ez*ez<d*d*.245){neighbours=false;break;}}
                if(!neighbours)continue;nx/=d;nz/=d;double stress=(pa.VX-pb.VX)*nx+(pa.VZ-pb.VZ)*nz;
                var random=new WorldGenerationRandom(WorldGenerationSeeds.Derive(config.Seed,"tectonics/fault/"+a+"/"+b,Version));double old=random.Next01();
                BoundaryKind kind=stress>.20?BoundaryKind.Convergent:stress<-.30?BoundaryKind.Divergent:old>.60?BoundaryKind.Ancient:BoundaryKind.Transform;
                double w=spacing*(.07+random.Next01()*.05),height=(Math.Min(900,config.Terrain.Elevation*.8+config.Terrain.Mountains*.025))*(.4+Math.Abs(stress)*.6),length=spacing*(.65+random.Next01()*.35),phase=random.Next01()*Math.PI*2;
                var points=new FaultPoint[17];
                for(int i=0;i<points.Length;i++)
                {
                    double t=(i/(double)(points.Length-1)-.5)*2,along=t*length*.5,curve=Math.Sin(t*2.2+phase)*w*.8+Noise(mx*width+nz*along,mz*depth-nx*along,spacing*.7,701)*w;
                    points[i]=new FaultPoint(mx*width+nz*along+nx*curve,mz*depth-nx*along+nz*curve);
                }
                faults.Add(new FaultTrace(moduleSeed.ToString("X16")+"/"+a+"-"+b,kind,w,height,old,points));
            }
            Faults=faults.AsReadOnly();
            for(int z=0;z<Resolution;z++)
            {
                token.ThrowIfCancellationRequested();checkpoint?.Invoke("Conditioning fault blocks and fold provinces",z,Resolution);
                for(int x=0;x<Resolution;x++)
                {
                    double east=(x/(double)(Resolution-1)-.5)*width,north=(z/(double)(Resolution-1)-.5)*depth,up=0,down=0,step=0,strong=0,oldSum=0,dirX=0,dirZ=0,weights=0;
                    foreach(var fault in faults)
                    {
                        double distance=fault.DistanceSquared(east,north,out double tx,out double tz,out double side),weight=Math.Exp(-distance/(fault.Width*fault.Width*2));if(weight<.001)continue;
                        double compression=fault.Kind==BoundaryKind.Convergent?1:fault.Kind==BoundaryKind.Ancient?.65:.20;
                        up+=fault.Displacement*weight*compression;down+=fault.Kind==BoundaryKind.Divergent?fault.Displacement*weight*.8:0;
                        step+=fault.Kind==BoundaryKind.Transform?fault.Displacement*.24*Math.Tanh(side/(fault.Width*.18))*weight:0;
                        strong=Math.Max(strong,weight*compression);dirX+=tx*weight*compression;dirZ+=tz*weight*compression;oldSum+=fault.Age*weight;weights+=weight;
                    }
                    double inland=ContinentalField.Smooth(0,3000,continents.CoastDistanceAt(east,north));int index=z*Resolution+x;
                    uplift[index]=(float)(up*inland);rift[index]=(float)(down*inland);escarpment[index]=(float)(step*inland);belt[index]=(float)strong;age[index]=(float)(weights>0?oldSum/weights:.8);double norm=Math.Sqrt(dirX*dirX+dirZ*dirZ);strikeX[index]=(float)(norm>1e-8?dirX/norm:1);strikeZ[index]=(float)(norm>1e-8?dirZ/norm:0);
                }
            }
            checkpoint?.Invoke("Fault provinces ready",Resolution,Resolution);
        }
        private float Read(float[] field,double x,double z)=>ContinuousGrid.Sample(field,Resolution,(x/width+.5)*(Resolution-1),(z/depth+.5)*(Resolution-1));
        public TectonicSample Sample(double x,double z)=>new TectonicSample(Read(uplift,x,z),Read(rift,x,z),Read(escarpment,x,z),Read(belt,x,z),Read(age,x,z),Read(strikeX,x,z),Read(strikeZ,x,z));
        public double PhysicalReliefAt(double x,double z)=>Read(uplift,x,z)-Read(rift,x,z)+Read(escarpment,x,z);
        public FaultTrace ClosestFault(double x,double z)
        {FaultTrace selected=null;double best=double.MaxValue;foreach(var fault in faults){double d=fault.DistanceSquared(x,z,out _,out _,out _);if(d<best){best=d;selected=fault;}}return selected;}
    }
}
