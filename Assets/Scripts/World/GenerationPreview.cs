using System;
namespace Aevoria.World
{
    // Immutable snapshots published only after their source stage completes.
    // The display never samples arrays being changed by the generator worker.
    public sealed class GenerationPreview
    {
        public const int Resolution=129;
        public readonly string Seed,Stage;public readonly float SeaLevel,Relief;public readonly float[] Elevation,Temperature,Moisture;
        public readonly float[] Water,Forest;public readonly Biome[] Biomes;public readonly GeoPoint[] Settlements;
        public readonly double Width,Depth;
        private GenerationPreview(ProceduralGeography geo,string stage,bool detailed,CivilizationModel society=null)
        {
            Seed=geo.Configuration.Seed;Stage=stage;SeaLevel=geo.SeaLevel;Relief=geo.Configuration.Terrain.Mountains;Width=geo.Configuration.WidthMeters;Depth=geo.Configuration.DepthMeters;Elevation=new float[Resolution*Resolution];if(geo.Climate!=null){Temperature=new float[Elevation.Length];Moisture=new float[Elevation.Length];}
            if(geo.InlandSurfaces!=null)Water=new float[Elevation.Length];if(geo.Biomes!=null)Biomes=new Biome[Elevation.Length];if(geo.Ecosystem!=null)Forest=new float[Elevation.Length];
            IWorldHeightField surface=geo.Surface??(IWorldHeightField)geo.OrdinaryTerrain;
            for(int z=0;z<Resolution;z++)for(int x=0;x<Resolution;x++)
            {
                double east=(x/(double)(Resolution-1)-.5)*Width,north=(z/(double)(Resolution-1)-.5)*Depth;int i=z*Resolution+x;float height=surface==null?(geo.Continents.CoastDistanceAt(east,north)>0?SeaLevel+1:SeaLevel-20):detailed?geo.ElevationAt(east,north):surface.ElevationAt(east,north);Elevation[i]=height;
                if(Temperature!=null){var c=geo.Climate.Sample(east,north,height);Temperature[i]=c.TemperatureCelsius;Moisture[i]=c.Moisture;}
                if(Water!=null&&geo.InlandSurfaces.LakeAt(east,north,out _))Water[i]=1;
                if(Biomes!=null)Biomes[i]=geo.Biomes.Sample(east,north,height).Biome;
                if(Forest!=null)Forest[i]=(float)geo.Ecosystem.ForestPotential(east,north);
            }
            if(Water!=null)
            {
                var h=geo.Hydrology;
                for(int node=0;node<h.Ground.Length;node++)if(h.IsRiver(node)&&!geo.InlandSurfaces.IsSeasonallyDryChannel(node))
                {
                    h.PathPoint(node,0,out double ax,out double az);
                    // Subpixel streams stay subtle at world scale. A fixed
                    // minimum opacity made narrow channels resemble kilometre
                    // wide flooded corridors in the assembled preview.
                    float strength=(float)Math.Min(.65,Math.Max(.025,Math.Sqrt(h.RiverWidth(node)/(Width/(Resolution-1)))*.5));
                    for(int p=4;p<h.PathSamples(node)+4;p+=4){h.PathPoint(node,Math.Min(p,h.PathSamples(node)),out double bx,out double bz);RasterRiver(ax,az,bx,bz,strength);ax=bx;az=bz;}
                }
            }
            if(society!=null){Settlements=new GeoPoint[society.Settlements.Count];for(int i=0;i<Settlements.Length;i++)Settlements[i]=new GeoPoint(society.Settlements[i].X,society.Settlements[i].Z);}
        }
        private void RasterRiver(double ax,double az,double bx,double bz,float strength)
        {
            double x0=(ax/Width+.5)*(Resolution-1),z0=(az/Depth+.5)*(Resolution-1),x1=(bx/Width+.5)*(Resolution-1),z1=(bz/Depth+.5)*(Resolution-1);int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Abs(x1-x0),Math.Abs(z1-z0))*2));
            for(int i=0;i<=steps;i++){double t=i/(double)steps;int x=Math.Max(0,Math.Min(Resolution-1,(int)Math.Round(x0+(x1-x0)*t))),z=Math.Max(0,Math.Min(Resolution-1,(int)Math.Round(z0+(z1-z0)*t)));Water[z*Resolution+x]=Math.Max(Water[z*Resolution+x],strength);}
        }
        public static GenerationPreview ForSettlements(ProceduralGeography geo,CivilizationModel society)=>new GenerationPreview(geo,"Founded settlements",true,society);
        internal static void Publish(Action<GenerationPreview> receive,ProceduralGeography geo,string stage,bool detailed=false){if(receive!=null)receive(new GenerationPreview(geo,stage,detailed));}
    }
}
