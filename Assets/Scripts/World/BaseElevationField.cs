using System;

namespace Aevoria.World
{
    public sealed class BaseElevationField:IWorldHeightField
    {
        public const int Version=6;private readonly ContinentalField continents;private readonly GenerationSettings settings;private readonly uint seed;private readonly double plateauX,plateauZ,plateauCore,plateauMargin;private readonly bool hasPlateau;public TectonicStructureField Structures{get;}
        public BaseElevationField(WorldConfiguration config,ContinentalField continents,ulong moduleSeed,TectonicStructureField tectonics=null)
        {
            settings=config.Terrain;this.continents=continents??throw new ArgumentNullException(nameof(continents));seed=(uint)(moduleSeed^(moduleSeed>>32));
            if(tectonics==null){var geology=new GeologyMap(config,continents,WorldGenerationSeeds.Derive(config.Seed,WorldLayerIds.Geology,GeologyMap.Version));tectonics=new TectonicStructureField(config,continents,geology,WorldGenerationSeeds.Derive(config.Seed,WorldLayerIds.Tectonics,TectonicStructureField.Version));}Structures=tectonics;
            plateauCore=Math.Max(4000,settings.Elevation*15);plateauMargin=Math.Max(8000,settings.Elevation*36);var random=new WorldGenerationRandom(moduleSeed);double best=-1;
            for(int z=1;z<33;z++)for(int x=1;x<33;x++)
            {
                double east=(x/33.0-.5)*config.WidthMeters,north=(z/33.0-.5)*config.DepthMeters,score=random.Next01();
                if(score>best&&continents.CoastDistanceAt(east,north)>plateauCore+plateauMargin){best=score;plateauX=east;plateauZ=north;hasPlateau=true;}
            }
        }
        private double Noise(double x,double z,double scale,uint channel)=>GradientField.Sample(x/scale,z/scale,seed,channel);
        public float PlateauAt(double east,double north)
        {if(!hasPlateau)return 0;double x=east-plateauX,z=north-plateauZ,r=Math.Sqrt(x*x+z*z);return (float)(1-ContinentalField.Smooth(plateauCore,plateauCore+plateauMargin,r));}
        public float ElevationAt(double east,double north)
        {
            double coast=continents.CoastDistanceAt(east,north);
            // The physical datum stays fixed when the ocean setting changes;
            // sea level floods this surface instead of translating the ground.
            if(coast<=0)
            {
                double d=-coast,shelfWidth=4200+Noise(east,north,32000,321)*1800;
                double shelfDepth=160+Noise(east,north,22000,322)*45,basinDepth=4200+Noise(east,north,110000,323)*1400;
                return (float)(-shelfDepth*(1-Math.Exp(-d/shelfWidth))-basinDepth*(1-Math.Exp(-Math.Max(0,d-shelfWidth*2.5)/28000)));
            }
            double land=1-Math.Exp(-coast/6500),plain=settings.Elevation*.65*land;
            double hillMask=.35+.65*ContinentalField.Smooth(-.35,.55,Noise(east,north,8700,301));
            double hills=(Noise(east,north,3200,302)*.65+Noise(east,north,1100,308)*.35)*(35+settings.Elevation*.32)*hillMask;
            double detail=Noise(east,north,180,303)*1.1+Noise(east,north,55,304)*.20;
            // Broad low-relief drainage divides keep ordinary land gentle while
            // giving rainfall real valleys to converge into. A coast-distance
            // ramp alone sends neighbouring streams down parallel fall lines.
            double divides=settings.Elevation*.28*Noise(east,north,5500,307);
            if(settings.Profile==GeographyProfile.Atlas)
            {
                // Broad drainage divides feed regional valleys. Small hills
                // decorate that relief instead of creating a pond in each cell.
                hills*=.22;detail*=.35;
                divides=settings.Elevation*.48*Noise(east,north,settings.ContinentalScale*2.5,307);
            }
            double ordinary=plain+land*settings.Roughness*(hills+detail+divides),cap=settings.Elevation*1.45+Noise(east,north,12000,306)*5;
            // Resistant upland provinces have broad tilted cores and gentle
            // margins. They are composed before mountains, so caprock never
            // flattens a major summit or clips it to one elevation.
            double blocks=Structures.PhysicalReliefAt(east,north);
            double terrain=ordinary+(cap-ordinary)*PlateauAt(east,north),relief=blocks*land;
            // Continental subsidence relaxes against the supporting crust.
            // A hard sea-datum clamp erased entire rift provinces into an
            // identical 1 mm sheet, producing flat land and ocean z-fighting.
            // Keep the depression continuous and retain its original relief.
            if(relief<0){double support=Math.Max(12,terrain*.75);relief=-support*(1-Math.Exp(relief/support));}
            return (float)(terrain+relief);
        }
    }
    public sealed class ComposedSurfaceField:IWorldHeightField
    {
        public const int Version=3;public BaseElevationField Base{get;}public MountainClusterField Mountains{get;}public CoastalLandformField Coast{get;}
        public RegionalLandformField Regional{get;}
        public double MaximumElevation{get;}
        public ComposedSurfaceField(WorldConfiguration config,BaseElevationField ordinary,MountainClusterField mountains,CoastalLandformField coast=null,RegionalLandformField regional=null){Base=ordinary;Mountains=mountains;Coast=coast;Regional=regional;MaximumElevation=config.Terrain.Elevation+config.Terrain.Mountains*2.05+1000;}
        public float ElevationAt(double east,double north){float height=Base.ElevationAt(east,north)+Mountains.ContributionAt(east,north);height=Coast==null?height:Coast.Shape(east,north,height);return Regional==null?height:Regional.Shape(east,north,height);}
    }
}
