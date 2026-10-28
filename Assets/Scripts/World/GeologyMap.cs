using System;
using System.Collections.Generic;
using System.Threading;
namespace Aevoria.World
{
    public enum LandmassType { Random,SingleContinent,Continents,Supercontinent,Archipelago,Mixed }
    public enum BoundaryKind { Stable,Convergent,Divergent,Transform,Ancient,Volcanic }
    public enum AquaticHabitat { None,DeepOcean,SandySeabed,RockyCoast,Kelp,Seagrass,CoralReef,Estuary,FreshwaterPlants }
    public readonly struct GeologicalSample
    {
        public readonly float Continental,Uplift,Rift,Regolith,DirectionX,DirectionZ,Plateau;public readonly BoundaryKind Kind;
        public GeologicalSample(float continental,float uplift,float rift,float regolith,float dx,float dz,float shear=0,float volcanic=0,float plateau=0)
        {Continental=continental;Uplift=uplift;Rift=rift;Regolith=regolith;DirectionX=dx;DirectionZ=dz;Plateau=plateau;Kind=volcanic>.15?BoundaryKind.Volcanic:uplift>.3?BoundaryKind.Convergent:rift>.3?BoundaryKind.Divergent:shear>.3?BoundaryKind.Transform:uplift>.03?BoundaryKind.Ancient:BoundaryKind.Stable;}
    }
    // Immutable continental/tectonic fields. Elevation, erosion and climate
    // belong to their own products; this map never queries those downstream.
    public sealed class GeologyMap
    {
        public const int Version=6,Resolution=257;
        private readonly uint moduleSeed;private readonly double width,depth;
        private readonly float[] continent=new float[Resolution*Resolution],uplift=new float[Resolution*Resolution],rift=new float[Resolution*Resolution],directionX=new float[Resolution*Resolution],directionZ=new float[Resolution*Resolution],regolith=new float[Resolution*Resolution],shear=new float[Resolution*Resolution],volcanism=new float[Resolution*Resolution];
        private readonly Plate[] plates;
        public readonly struct Plate {public readonly double X,Z,VX,VZ;public Plate(double x,double z,double vx,double vz){X=x;Z=z;VX=vx;VZ=vz;}}
        public IReadOnlyList<Plate> Plates {get;}public LandmassType Arrangement {get;}
        public long FieldBytes=>(long)Resolution*Resolution*8*4;
        private double Noise(double east,double north,double wavelength,uint channel)=>GradientField.Sample(east/wavelength,north/wavelength,moduleSeed,channel);
        private double Rand(int index,int channel)=>(Noise(index*1703+channel*139,index*307-channel*251,191,(uint)(channel+1000))+1)*.5;
        public GeologyMap(WorldConfiguration configuration,ContinentalField continents,ulong seed,CancellationToken token=default)
        {
            if(configuration==null||continents==null)throw new ArgumentNullException();
            moduleSeed=(uint)(seed^(seed>>32));width=configuration.WidthMeters;depth=configuration.DepthMeters;Arrangement=continents.Arrangement;
            int scaleCount=Math.Max(0,(int)Math.Round(Math.Log(width/128000,2)));
            int plateSide=3+scaleCount;plates=new Plate[plateSide*plateSide];for(int z=0;z<plateSide;z++)for(int x=0;x<plateSide;x++){int i=z*plateSide+x;double a=Rand(i,20)*Math.PI*2;plates[i]=new Plate((x+.2+Rand(i,21)*.6)/plateSide-.5,(z+.2+Rand(i,22)*.6)/plateSide-.5,Math.Cos(a),Math.Sin(a));}
            var distances=new double[plates.Length];var probabilities=new double[plates.Length];double softness=.026/plateSide;
            for(int z=0;z<Resolution;z++){token.ThrowIfCancellationRequested();for(int x=0;x<Resolution;x++)
            {
                double nx=x/(double)(Resolution-1)-.5,nz=z/(double)(Resolution-1)-.5,e=nx*width,n=nz*depth;
                double value=continents.RawAt(e,n);
                double minimum=double.MaxValue;
                for(int p=0;p<plates.Length;p++){double dx=nx-plates[p].X,dz=nz-plates[p].Z;distances[p]=dx*dx+dz*dz;minimum=Math.Min(minimum,distances[p]);}
                double total=0;for(int p=0;p<plates.Length;p++){probabilities[p]=Math.Exp(-(distances[p]-minimum)/softness);total+=probabilities[p];}
                for(int p=0;p<plates.Length;p++)probabilities[p]/=total;
                // Soft plate memberships keep both strength and ridge phase
                // continuous through nearest-pair changes and triple junctions.
                // Each pair has a stable undirected axis, and its own ridge is
                // blended before rasterization instead of switching a cosine's
                // direction abruptly at a Voronoi boundary.
                double up=0,down=0,slip=0,axisX=0,axisZ=0,axisWeight=0;
                for(int a=0;a<plates.Length;a++)if(probabilities[a]>1e-6)
                for(int b=a+1;b<plates.Length;b++)if(probabilities[b]>1e-6)
                {
                    double vx=plates[b].X-plates[a].X,vz=plates[b].Z-plates[a].Z,len=Math.Sqrt(vx*vx+vz*vz);vx/=len;vz/=len;
                    double weight=4*probabilities[a]*probabilities[b],stress=(plates[a].VX-plates[b].VX)*vx+(plates[a].VZ-plates[b].VZ)*vz;
                    double rise=weight*Math.Max(0,stress)*.6;up+=rise;down+=weight*Math.Max(0,-stress)*.5;
                    slip+=weight*Math.Abs((plates[a].VX-plates[b].VX)*(-vz)+(plates[a].VZ-plates[b].VZ)*vx)*.5;
                    axisX+=weight*vx;axisZ+=weight*vz;axisWeight+=weight;
                }
                int i=z*Resolution+x;continent[i]=(float)value;uplift[i]=(float)up;rift[i]=(float)down;shear[i]=(float)slip;
                if(axisWeight>0){directionX[i]=(float)(axisX/axisWeight);directionZ[i]=(float)(axisZ/axisWeight);}
            }}
            Plates=Array.AsReadOnly(plates);
            for(int z=0;z<Resolution;z++)
            {
                token.ThrowIfCancellationRequested();
                for(int x=0;x<Resolution;x++)
                {
                    int i=z*Resolution+x;double east=(x/(double)(Resolution-1)-.5)*width,north=(z/(double)(Resolution-1)-.5)*depth;
                    double inland=ContinentalField.Smooth(0,2000,continents.CoastDistanceAt(east,north));
                    // A static weathered-regolith potential, not a claim of
                    // executed sediment transport in the discarded height shaper.
                    double stability=1-ContinentalField.Smooth(.05,.8,uplift[i]+rift[i]);
                    regolith[i]=(float)(inland*(.12+.6*stability)*(.8+.2*Noise(east,north,Math.Min(width*.05,24000),110)));
                    volcanism[i]=(float)(Math.Pow(Math.Max(0,Noise(east,north,width*.07,111)-.65)*2.85,3)*.55);
                }
            }
        }
        public float ContinentalAt(double x,double z)=>Read(continent,x,z);
        public GeologicalSample Sample(double x,double z)=>new GeologicalSample(Read(continent,x,z),Read(uplift,x,z),Read(rift,x,z),Read(regolith,x,z),Read(directionX,x,z),Read(directionZ,x,z),Read(shear,x,z),Read(volcanism,x,z));
        public float Soil(double x,double z)=>Math.Max(0,Math.Min(1,Read(regolith,x,z)));
        private float Read(float[] values,double x,double z)=>ContinuousGrid.Sample(values,Resolution,(x/width+.5)*(Resolution-1),(z/depth+.5)*(Resolution-1));
    }
}
