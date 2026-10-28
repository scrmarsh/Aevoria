using System;
using System.Collections.Generic;
using System.Threading;

namespace Aevoria.World
{
    public interface IWorldHeightField {float ElevationAt(double east,double north);}
    public sealed class ContinentalField
    {
        public const int Version=4,Resolution=257;
        private readonly WorldConfiguration config;private readonly uint seed;private readonly float[] field=new float[Resolution*Resolution],gradient=new float[Resolution*Resolution];
        private readonly List<Lobe> lobes=new List<Lobe>();
        private readonly struct Lobe{public readonly double X,Z,A,B,Angle;public readonly int Group;public Lobe(double x,double z,double a,double b,double angle,int group){X=x;Z=z;A=a;B=b;Angle=angle;Group=group;}}
        public LandmassType Arrangement{get;}public double CoastThreshold{get;}public double CalibratedLandFraction{get;}
        public long FieldBytes=>(long)this.field.Length*12;
        private readonly float[] coastDistance=new float[Resolution*Resolution];
        private double Noise(double x,double z,double scale,uint channel)=>GradientField.Sample(x/scale,z/scale,seed,channel);
        private double Rand(int index,int channel)=>(Noise(index*1703+channel*139,index*307-channel*251,191,(uint)(channel+1000))+1)*.5;
        private double BoundaryMargin(double east,double north)
        {
            double x=east/(config.WidthMeters*.485),z=north/(config.DepthMeters*.485),angle=Math.Atan2(z,x);
            // A rounded finite coast at normal coverage, with more usable
            // interior at deliberately high land fractions. Always leave sea
            // between the physical coast and the finite world's edge.
            double power=2.8+7*Smooth(.60,.85,config.Terrain.TargetLandFraction);
            double radius=Math.Pow(Math.Pow(Math.Abs(x),power)+Math.Pow(Math.Abs(z),power),1/power),coast=1+.007*Math.Sin(angle*3+seed%31)+.004*Math.Sin(angle*5+seed%17);
            return (coast-radius)*Math.Min(config.WidthMeters,config.DepthMeters)*.485;
        }
        public ContinentalField(WorldConfiguration config,ulong moduleSeed,CancellationToken token=default,Action<string,long,long> report=null)
        {
            this.config=config??throw new ArgumentNullException(nameof(config));seed=(uint)(moduleSeed^(moduleSeed>>32));
            bool atlas=config.Terrain.Profile==GeographyProfile.Atlas;
            Arrangement=config.Terrain.Landmass==LandmassType.Random?(atlas?LandmassType.Supercontinent:(LandmassType)(1+(int)(Rand(1,1)*4.999))):config.Terrain.Landmass;
            int scaleCount=Math.Max(0,(int)Math.Round(Math.Log(config.WidthMeters/128000,2)));
            int chains=Arrangement==LandmassType.SingleContinent||Arrangement==LandmassType.Supercontinent?1:Arrangement==LandmassType.Continents?2+scaleCount:Arrangement==LandmassType.Archipelago?6+scaleCount*3:2+scaleCount;
            for(int chain=0;chain<chains;chain++)
            {
                bool small=Arrangement==LandmassType.Archipelago;double cx=(Rand(chain,3)-.5)*.95,cz=(Rand(chain,4)-.5)*.85,angle=Rand(chain,5)*Math.PI*2;
                if(chains==1){cx=cz=0;}else if(!small){double a=chain*2.399+Rand(chain,30);cx=Math.Cos(a)*.30;cz=Math.Sin(a)*.27;}
                int count=small?3:6;double length=small?.09:chains==1?.60:.28,thickness=small?.027:Arrangement==LandmassType.Supercontinent?.24:chains==1?.17:.13;
                for(int i=0;i<count;i++){double along=(i/(double)(count-1)-.5)*length,bend=Math.Sin(i*1.3+chain)*thickness*.8;lobes.Add(new Lobe(cx+Math.Cos(angle)*along-Math.Sin(angle)*bend,cz+Math.Sin(angle)*along+Math.Cos(angle)*bend,thickness*(.7+Rand(i+chain*19,6)*.6),thickness*(.55+Rand(i+chain*17,7)*.5),angle+Rand(i,8)*.8,chain));}
            }
            if(Arrangement==LandmassType.Supercontinent||Arrangement==LandmassType.Mixed)for(int i=0;i<8;i++){double a=i*.79+Rand(i,9)*.3;lobes.Add(new Lobe(Math.Cos(a)*.43,Math.Sin(a)*.36,.025+Rand(i,10)*.028,.02+Rand(i,11)*.03,a,chains+i));}
            double width=config.WidthMeters,depth=config.DepthMeters,coastScale=Math.Max(.4,Math.Min(2.4,config.Terrain.ContinentalScale/9000));
            var seaMargin=new float[Resolution*Resolution];
            double oceanHalfWidth=Math.Min(width,depth)*.13*(1-.990*Smooth(.54,.85,config.Terrain.TargetLandFraction));
            for(int z=0;z<Resolution;z++)
            {
                token.ThrowIfCancellationRequested();report?.Invoke("Continental mask",z,Resolution);
                for(int x=0;x<Resolution;x++)
                {
                    double nx=x/(double)(Resolution-1)-.5,nz=z/(double)(Resolution-1)-.5,e=nx*width,n=nz*depth;
                    double wx=nx+Noise(e,n,width*.19*coastScale,100)*.045,wz=nz+Noise(e,n,depth*.19*coastScale,101)*.045,value=double.NegativeInfinity,next=double.NegativeInfinity;int group=-1;
                    foreach(var l in lobes){double dx=wx-l.X,dz=wz-l.Z,ca=Math.Cos(l.Angle),sa=Math.Sin(l.Angle),u=(dx*ca+dz*sa)/l.A,v=(-dx*sa+dz*ca)/l.B,d=(1-Math.Sqrt(u*u+v*v))*l.A/.13;if(d>value){if(group!=l.Group)next=value;value=d;group=l.Group;}else if(l.Group!=group)next=Math.Max(next,d);}
                    double margin=double.PositiveInfinity;if(Arrangement==LandmassType.Archipelago&&group>=0&&!double.IsNegativeInfinity(next)){margin=(value-next-.14)*4;value+=2;}
                    // Keep separate continental provinces separated even when
                    // coverage calibration lowers the shoreline threshold.
                    // The same broad domain warp bends these ocean corridors;
                    // their width is geographical, not a river-width setting.
                    double gap=Math.Min(width,depth)*4;
                    if((Arrangement==LandmassType.Continents||Arrangement==LandmassType.Mixed)&&!double.IsNegativeInfinity(next))gap=(value-next)*Math.Min(width,depth)*.13;
                    seaMargin[z*Resolution+x]=(float)gap;
                    value+=(Noise(e,n,Math.Min(120000,width*.09)*coastScale,103)*.35+Noise(e,n,Math.Min(24000,width*.025)*coastScale,104)*.12)*config.Terrain.CoastVariation/.7;
                    double angle=Math.Atan2(nz,nx),radius=.47+.035*Math.Sin(angle*3+Rand(17,40)*6)+.025*Math.Sin(angle*7+Rand(18,40)*6);
                    if(Arrangement==LandmassType.Archipelago)value=Math.Min(Math.Min(value,margin),(radius-Math.Sqrt(nx*nx+nz*nz))*10);else value-=Smooth(.84,1.10,Math.Sqrt(nx*nx+nz*nz)/radius)*1.2;
                    field[z*Resolution+x]=(float)value;
                }
            }
            // More continental provinces require more shared sea boundary.
            // Reserve the widest requested corridors that still leave space
            // for the chosen land coverage and organic shoreline calibration.
            int requested=(int)Math.Round(config.Terrain.TargetLandFraction*97*97);
            var gaps=new List<float>();for(int z=0;z<97;z++)for(int x=0;x<97;x++)
            {double e=((x+.5)/97-.5)*width,n=((z+.5)/97-.5)*depth;if(BoundaryMargin(e,n)>0)gaps.Add(Read(seaMargin,e,n));}gaps.Sort();
            int reserve=Math.Min((int)(.035*97*97),Math.Max(1,(gaps.Count-requested)/2));
            int gapCut=Math.Max(0,gaps.Count-requested-reserve-1);oceanHalfWidth=Math.Min(oceanHalfWidth,Math.Max(0,gaps[gapCut]));
            for(int i=0;i<seaMargin.Length;i++)seaMargin[i]-=(float)oceanHalfWidth;
            if(atlas)for(int z=0;z<Resolution;z++)for(int x=0;x<Resolution;x++)
            {
                double east=(x/(double)(Resolution-1)-.5)*width,north=(z/(double)(Resolution-1)-.5)*depth;
                int i=z*Resolution+x;seaMargin[i]=(float)Math.Min(seaMargin[i],AtlasGeography.SeaMargin(config,moduleSeed,east,north));
            }
            var values=new List<float>();for(int z=0;z<97;z++)for(int x=0;x<97;x++)
            {double e=((x+.5)/97-.5)*width,n=((z+.5)/97-.5)*depth;if(BoundaryMargin(e,n)>0&&Read(seaMargin,e,n)>0)values.Add(RawAt(e,n));}values.Sort();
            int cut=values.Count-requested;
            if(cut<1)throw new InvalidOperationException("Requested land coverage exceeds the finite world's coast domain.");
            CoastThreshold=(values[cut]+values[cut-1])*.5;CalibratedLandFraction=requested/(97.0*97);
            // Reserve genuine ocean before computing the physical distance
            // field. Land must not reach a rectangular renderer cut-off.
            for(int z=0;z<Resolution;z++)for(int x=0;x<Resolution;x++)
            {double e=(x/(double)(Resolution-1)-.5)*width,n=(z/(double)(Resolution-1)-.5)*depth;int at=z*Resolution+x;field[at]=(float)Math.Min(field[at],CoastThreshold+Math.Min(BoundaryMargin(e,n),seaMargin[at])/Math.Min(width,depth)*4);}
            for(int z=0;z<Resolution;z++)for(int x=0;x<Resolution;x++)
            {
                int left=Math.Max(0,x-1),right=Math.Min(Resolution-1,x+1),down=Math.Max(0,z-1),up=Math.Min(Resolution-1,z+1);
                double dx=(field[z*Resolution+right]-field[z*Resolution+left])/((right-left)*width/(Resolution-1)),dz=(field[up*Resolution+x]-field[down*Resolution+x])/((up-down)*depth/(Resolution-1));gradient[z*Resolution+x]=(float)Math.Max(1e-9,Math.Sqrt(dx*dx+dz*dz));
            }
            // Subcell coast intersections seed a chamfer distance transform.
            // Dividing the mask by its local gradient across an entire inland
            // region blows up near stationary points and invents steep hills.
            double stepX=width/(Resolution-1),stepZ=depth/(Resolution-1);
            for(int z=0;z<Resolution;z++)for(int x=0;x<Resolution;x++)
            {
                int at=z*Resolution+x;double value=field[at]-CoastThreshold,best=double.PositiveInfinity;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    int nx=x+dx,nz=z+dz;if(nx<0||nz<0||nx>=Resolution||nz>=Resolution||dx==0&&dz==0)continue;
                    double other=field[nz*Resolution+nx]-CoastThreshold;
                    if(value*other<=0)best=Math.Min(best,Math.Sqrt(dx*dx*stepX*stepX+dz*dz*stepZ*stepZ)*Math.Abs(value)/(Math.Abs(value)+Math.Abs(other)+1e-20));
                }
                coastDistance[at]=(float)best;
            }
            for(int pass=0;pass<2;pass++)
            {
                for(int row=0;row<Resolution;row++)
                {
                    token.ThrowIfCancellationRequested();int z=pass==0?row:Resolution-1-row;
                    for(int column=0;column<Resolution;column++)
                    {
                        int x=pass==0?column:Resolution-1-column,at=z*Resolution+x;
                        for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                        {
                            if(pass==0?(dz>0||dz==0&&dx>=0):(dz<0||dz==0&&dx<=0))continue;
                            int nx=x+dx,nz=z+dz;if(nx<0||nz<0||nx>=Resolution||nz>=Resolution)continue;
                            coastDistance[at]=Math.Min(coastDistance[at],coastDistance[nz*Resolution+nx]+(float)Math.Sqrt(dx*dx*stepX*stepX+dz*dz*stepZ*stepZ));
                        }
                    }
                }
            }
            for(int i=0;i<coastDistance.Length;i++)if(field[i]<CoastThreshold)coastDistance[i]=-coastDistance[i];
            report?.Invoke("Continental mask calibrated",Resolution,Resolution);
        }
        private float Read(float[] values,double east,double north)=>ContinuousGrid.Sample(values,Resolution,(east/config.WidthMeters+.5)*(Resolution-1),(north/config.DepthMeters+.5)*(Resolution-1));
        public float RawAt(double east,double north)=>Read(field,east,north);
        public double CoastDistanceAt(double east,double north)=>Read(coastDistance,east,north);
        internal static double Smooth(double low,double high,double value){double t=Math.Max(0,Math.Min(1,(value-low)/(high-low)));return t*t*(3-2*t);}
    }
}
