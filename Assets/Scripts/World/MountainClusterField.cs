using System;
using System.Collections.Generic;
using System.Threading;
namespace Aevoria.World
{
    public enum MountainFormationKind { Massif,FoldBelt,Coastal,Eroded,Extreme }
    public sealed class MountainPeak
    {
        public readonly double X,Z,Height,Radius,Angle,Stretch,CliffStrength,Curvature;public readonly int RidgeCount;
        internal MountainPeak(double x,double z,double height,double radius,double angle,double stretch,int ridges,double cliff,double curvature){X=x;Z=z;Height=height;Radius=radius;Angle=angle;Stretch=stretch;RidgeCount=ridges;CliffStrength=cliff;Curvature=curvature;}
    }
    public sealed class MountainRidge
    {
        public readonly double Width;public IReadOnlyList<FaultPoint> Points{get;}public IReadOnlyList<double> Heights{get;}
        internal MountainRidge(FaultPoint[] points,double[] heights,double width){Points=Array.AsReadOnly(points);Heights=Array.AsReadOnly(heights);Width=width;}
        public double HeightAt(double x,double z)
        {
            double best=double.MaxValue,height=0;
            for(int i=1;i<Points.Count;i++)
            {
                var a=Points[i-1];var b=Points[i];double dx=b.X-a.X,dz=b.Z-a.Z,length=dx*dx+dz*dz,t=Math.Max(0,Math.Min(1,((x-a.X)*dx+(z-a.Z)*dz)/length)),ex=x-a.X-t*dx,ez=z-a.Z-t*dz,d=ex*ex+ez*ez;
                if(d>=best)continue;best=d;height=Heights[i-1]+(Heights[i]-Heights[i-1])*t;
            }
            return best>Width*Width*12?0:height*Math.Exp(-Math.Pow(best/(Width*Width),.65));
        }
    }
    public sealed class MountainCluster
    {
        public readonly string Id;public readonly double X,Z,Radius,Height,Angle,Age;public readonly MountainFormationKind Kind;
        public IReadOnlyList<MountainPeak> Peaks{get;}public IReadOnlyList<MountainRidge> Ridges{get;}
        internal MountainCluster(string id,double x,double z,double radius,double height,double angle,double age,MountainFormationKind kind,MountainPeak[] peaks,MountainRidge[] ridges){Id=id;X=x;Z=z;Radius=radius;Height=height;Angle=angle;Age=age;Kind=kind;Peaks=Array.AsReadOnly(peaks);Ridges=Array.AsReadOnly(ridges);}
    }
    // Broad supporting rock masses with connected ridge graphs and saddles.
    // There is no radial exponential cone or angular sine-spoke peak profile.
    public sealed class MountainClusterField
    {
        public const int Version=7;private readonly ContinentalField continents;private readonly uint seed;private readonly List<MountainCluster> clusters=new List<MountainCluster>();
        public IReadOnlyList<MountainCluster> Clusters=>clusters.AsReadOnly();public bool PrototypeOnly{get;}
        private double Noise(double x,double z,double scale,uint channel)=>GradientField.Sample(x/scale,z/scale,seed,channel);
        public MountainClusterField(WorldConfiguration config,ContinentalField continents,ulong moduleSeed,CancellationToken token=default,TectonicStructureField tectonics=null)
        {
            this.continents=continents??throw new ArgumentNullException(nameof(continents));seed=(uint)(moduleSeed^(moduleSeed>>32));PrototypeOnly=config.Terrain.MountainPrototypeOnly;if(config.Terrain.Mountains<=0)return;
            // Standalone tools receive the same physical tectonic input as the
            // pipeline, rather than silently using a different mountain model.
            if(tectonics==null){var geology=new GeologyMap(config,continents,WorldGenerationSeeds.Derive(config.Seed,WorldLayerIds.Geology,GeologyMap.Version),token);tectonics=new TectonicStructureField(config,continents,geology,WorldGenerationSeeds.Derive(config.Seed,WorldLayerIds.Tectonics,TectonicStructureField.Version),token);}
            var sites=new List<(double x,double z,double score,int key)>();
            for(int row=1;row<65;row++)
            {
                token.ThrowIfCancellationRequested();
                for(int column=1;column<65;column++)
                {
                    int key=row*65+column;var local=new WorldGenerationRandom(WorldGenerationSeeds.Derive(config.Seed,"mountains/site/"+key,Version));
                    double x=((column+(local.Next01()-.5)*.6)/65-.5)*config.WidthMeters,z=((row+(local.Next01()-.5)*.6)/65-.5)*config.DepthMeters,coast=continents.CoastDistanceAt(x,z);if(coast<4500)continue;
                    var structure=tectonics.Sample(x,z);double score=structure.BeltStrength*.62+Math.Min(1,coast/22000)*.25+local.Next01()*.13;
                    if(config.Terrain.Profile==GeographyProfile.Atlas)
                    {
                        double bend=Noise(x,z,config.WidthMeters*.18,897)*.045;
                        double northDistance=Math.Abs(z/config.DepthMeters-.265-bend),southDistance=Math.Abs(z/config.DepthMeters+.015-bend);
                        double belt=Math.Exp(-Math.Pow(Math.Min(northDistance,southDistance)/.055,2));
                        score=belt*.85+structure.BeltStrength*.10+local.Next01()*.05;
                        if(belt<.35)continue;
                    }
                    sites.Add((x,z,score,key));
                }
            }
            sites.Sort((a,b)=>{int n=b.score.CompareTo(a.score);return n!=0?n:a.key.CompareTo(b.key);});int target=PrototypeOnly?1:Math.Max(2,Math.Min(32,(int)Math.Round(config.WidthMeters/100000)));
            if(!PrototypeOnly&&config.Terrain.Profile==GeographyProfile.Atlas)target=Math.Max(6,target);
            foreach(var site in sites)
            {
                if(clusters.Count>=target)break;token.ThrowIfCancellationRequested();bool separated=true;
                foreach(var other in clusters){double dx=site.x-other.X,dz=site.z-other.Z;if(dx*dx+dz*dz<other.Radius*other.Radius*1.65){separated=false;break;}}
                if(!separated)continue;var random=new WorldGenerationRandom(WorldGenerationSeeds.Derive(config.Seed,"mountains/formation/"+site.key,Version));Add(config,tectonics,site.x,site.z,site.key,ref random);
            }
        }
        private void Add(WorldConfiguration config,TectonicStructureField tectonics,double x,double z,int key,ref WorldGenerationRandom random)
        {
            var structure=tectonics.Sample(x,z);var fault=tectonics.ClosestFault(x,z);double age=fault?.Age??structure.Age,angle=Math.Atan2(structure.StrikeZ,structure.StrikeX),height=config.Terrain.Mountains*(1.25+random.Next01()*.30);
            if(random.Next01()>.97)height=config.Terrain.Mountains*2.02;
            double radius=Math.Max(14000,Math.Min(config.WidthMeters*.45,12000+height*3.1));
            if(config.Terrain.Profile==GeographyProfile.Atlas)radius=Math.Max(radius,Math.Min(config.WidthMeters,config.DepthMeters)*.065);
            MountainFormationKind kind=height>18000?MountainFormationKind.Extreme:age>.73?MountainFormationKind.Eroded:continents.CoastDistanceAt(x,z)<radius*.45?MountainFormationKind.Coastal:fault?.Kind==BoundaryKind.Convergent?MountainFormationKind.FoldBelt:MountainFormationKind.Massif;
            if(kind==MountainFormationKind.Eroded){height*=.78;radius*=1.08;}
            int satellites=3+(int)(random.Next01()*2);var peaks=new MountainPeak[satellites+1];var ridges=new List<MountainRidge>();
            peaks[0]=new MountainPeak(x,z,height,radius*.30,angle,kind==MountainFormationKind.FoldBelt?1.35:1.05,4,.4,0);
            for(int i=0;i<satellites;i++)
            {
                double direction=angle+i*Math.PI*2/satellites+(random.Next01()-.5)*.55,distance=radius*(.40+random.Next01()*.31);
                double px=x+Math.Cos(direction)*distance,pz=z+Math.Sin(direction)*distance;
                if(kind==MountainFormationKind.FoldBelt){double along=(i-(satellites-1)*.5)*radius*.43;px=x+Math.Cos(angle)*along-Math.Sin(angle)*(random.Next01()-.5)*radius*.35;pz=z+Math.Sin(angle)*along+Math.Cos(angle)*(random.Next01()-.5)*radius*.35;}
                peaks[i+1]=new MountainPeak(px,pz,height*(.40+random.Next01()*.29),radius*(.18+random.Next01()*.08),direction,1.10,3,.25,0);
                Ridge(ridges,x,z,height,px,pz,peaks[i+1].Height,Math.Max(radius*(.075+age*.025),height*.60),radius*.08,ref random);
                var parent=ridges[ridges.Count-1];
                // Subordinate spurs branch off each main ridge; the spaces
                // between these connected structures form nested valleys.
                for(int branch=0;branch<3;branch++)for(int side=-1;side<=1;side+=2)
                {
                    double t=.27+branch*.22+(random.Next01()-.5)*.08,position=t*(parent.Points.Count-1);int segment=Math.Min(parent.Points.Count-2,(int)position);double local=position-segment;var a=parent.Points[segment];var b=parent.Points[segment+1];
                    double bx=a.X+(b.X-a.X)*local,bz=a.Z+(b.Z-a.Z)*local,tx=b.X-a.X,tz=b.Z-a.Z,l=Math.Sqrt(tx*tx+tz*tz);if(l<1)continue;tx/=l;tz/=l;
                    double rootHeight=parent.Heights[segment]+(parent.Heights[segment+1]-parent.Heights[segment])*local,reach=radius*(.15+random.Next01()*.19),ex=bx+tx*reach*.4-tz*side*reach,ez=bz+tz*reach*.4+tx*side*reach;
                    Ridge(ridges,bx,bz,rootHeight,ex,ez,rootHeight*(.15+random.Next01()*.13),Math.Max(radius*(.035+random.Next01()*.012),rootHeight*.70),radius*.035,ref random);
                }
            }
            clusters.Add(new MountainCluster("formation013/"+key,x,z,radius,height,angle,age,kind,peaks,ridges.ToArray()));
        }
        private static void Ridge(List<MountainRidge> ridges,double ax,double az,double ah,double bx,double bz,double bh,double width,double bow,ref WorldGenerationRandom random)
        {
            double dx=bx-ax,dz=bz-az,length=Math.Sqrt(dx*dx+dz*dz);if(length<1)return;double nx=-dz/length,nz=dx/length,curve=(random.Next01()-.5)*2*bow;var points=new FaultPoint[7];var heights=new double[7];
            for(int i=0;i<points.Length;i++){double t=i/(double)(points.Length-1),offset=Math.Sin(t*Math.PI)*curve;points[i]=new FaultPoint(ax+dx*t+nx*offset,az+dz*t+nz*offset);heights[i]=(ah+(bh-ah)*t)*(1-.19*Math.Sin(t*Math.PI));}
            ridges.Add(new MountainRidge(points,heights,width));
        }
        private static double Join(double a,double b,double softness){double d=Math.Max(0,softness-Math.Abs(a-b));return Math.Max(a,b)+d*d/(softness*4);}
        public float ContributionAt(double east,double north)
        {
            double coast=continents.CoastDistanceAt(east,north);if(coast<=0)return 0;double result=0;
            foreach(var cluster in clusters)
            {
                double dx=east-cluster.X,dz=north-cluster.Z,r=Math.Sqrt(dx*dx+dz*dz)/cluster.Radius;if(r>1.65)continue;
                double warp=cluster.Radius*.018,wx=east+Noise(east,north,cluster.Radius*.22,811)*warp,wz=north+Noise(east,north,cluster.Radius*.27,812)*warp;
                double structure=0;
                foreach(var peak in cluster.Peaks){double ca=Math.Cos(peak.Angle),sa=Math.Sin(peak.Angle),px=wx-peak.X,pz=wz-peak.Z,u=(px*ca+pz*sa)/(peak.Radius*peak.Stretch),v=(-px*sa+pz*ca)/(peak.Radius/peak.Stretch);structure=Join(structure,peak.Height*Math.Exp(-2.2*(u*u+v*v)),cluster.Height*.012);}
                foreach(var ridge in cluster.Ridges)structure=Join(structure,ridge.HeightAt(wx,wz),cluster.Height*.012);
                double platform=cluster.Height*.17*(1-ContinentalField.Smooth(.03,1.45,r));
                double weathering=1-.07*Math.Abs(Noise(east,north,cluster.Radius*.13,813));
                double rock=Noise(east,north,Math.Max(650,cluster.Radius*.045),814)*Math.Min(75,structure*.015)+Noise(east,north,210,815)*Math.Min(7,structure*.002);
                double height=(platform+structure*.83)*weathering+rock;
                // Resistant beds form small physical ledges, aligned with the
                // local fold strike, instead of a grain-only rock texture.
                double across=-dx*Math.Sin(cluster.Angle)+dz*Math.Cos(cluster.Angle),bed=(1-cluster.Age)*Math.Min(45,height*.008);
                height+=(ContinentalField.Smooth(-.2,.25,Math.Sin(height/180+across/3600))-.5)*bed;
                // A fixed narrow coast mask turned extreme formations into
                // kilometre-high curtains. Their supporting coastal slopes
                // need the same horizontal scale as the local rock height.
                double coastalSupport=ContinentalField.Smooth(0,Math.Max(2800,height*1.25),coast);
                result=Math.Max(result,height*coastalSupport*(1-ContinentalField.Smooth(1.35,1.65,r)));
            }
            return (float)Math.Max(0,result);
        }
    }
}
