using System;

namespace Aevoria.World
{
    // Versioned by GeographyProfile.Atlas. Coordinates describe regions of a
    // generated world; all metric consumers use the same physical coastline.
    public static class AtlasGeography
    {
        public static double SeaMargin(WorldConfiguration c,ulong seed,double east,double north)
        {
            var random=new WorldGenerationRandom(seed);double shift=(random.Next01()-.5)*.08;
            double angle=(random.Next01()-.5)*.55,secondary=random.Next01();
            double x=east/c.WidthMeters,z=north/c.DepthMeters;
            double warp=GradientField.Sample(x*12,z*12,(uint)seed,981)*.018;
            double squeeze=1-.65*ContinentalField.Smooth(.62,.85,c.Terrain.TargetLandFraction);
            double coastalWarp=GradientField.Sample(x*7,z*7,(uint)seed,982)*.035;
            double dx=x-shift+coastalWarp,dz=z+.105+warp,ca=Math.Cos(angle),sa=Math.Sin(angle);
            double u=dx*ca+dz*sa,v=-dx*sa+dz*ca;
            double bayWidth=1+.22*Math.Sin(u*29+shift*24)+GradientField.Sample(x*19,z*19,(uint)seed,983)*.18;
            double primary=(Math.Sqrt(u*u/(.245*.245)+v*v/(.072*.072*squeeze*squeeze*bayWidth*bayWidth))-1)*.072*squeeze;
            // A narrow strait opens the major basin toward the eastern ocean.
            double strait=Math.Max(Math.Abs(z+.105+Math.Sin(x*12+shift)*.018)-.012*squeeze,.12-x);
            double margin=Math.Min(primary,strait);
            if(secondary>.48)
            {
                double sx=x+.19,sz=z-.075-warp;
                double second=(Math.Sqrt(sx*sx/(.095*.095)+sz*sz/(.043*.043*squeeze*squeeze))-1)*.043*squeeze;
                margin=Math.Min(margin,second);
            }
            // A few supported islands interrupt the sea without fragmenting
            // it into hundreds of disconnected water patches.
            for(int i=0;i<3;i++)
            {
                double along=(random.Next01()-.5)*.32,across=(random.Next01()-.5)*.062;
                double ix=shift+along*ca-across*sa,iz=-.105+along*sa+across*ca;
                double a=.004+Math.Pow(random.Next01(),2)*.014,b=.003+random.Next01()*.008;
                double islandNoise=GradientField.Sample(x*140,z*140,(uint)seed,(uint)(984+i))*.18;
                double island=(1+islandNoise-Math.Sqrt((x-ix)*(x-ix)/(a*a)+(z-iz)*(z-iz)/(b*b)))*Math.Min(a,b);
                margin=Math.Max(margin,island);
            }
            return margin*Math.Min(c.WidthMeters,c.DepthMeters);
        }
    }
}
