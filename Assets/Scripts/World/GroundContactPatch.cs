namespace Aevoria.World
{
    // Shared by explorer support and native physical-contact validation.
    public static class GroundContactPatch
    {
        public const int Resolution=64;
        public static double SurfaceHeight(IWorldHeightField surface,double size,double x,double z)
        {
            double gx=System.Math.Floor(x/size)*size,gz=System.Math.Floor(z/size)*size,tx=(x-gx)/size,tz=(z-gz)/size;
            double a=surface.ElevationAt(gx,gz),b=surface.ElevationAt(gx+size,gz),c=surface.ElevationAt(gx,gz+size),d=surface.ElevationAt(gx+size,gz+size);
            return tx+tz<=1?a+(b-a)*tx+(c-a)*tz:d+(c-d)*(1-tx)+(b-d)*(1-tz);
        }
        public static SolidPoint[] Sample(ProceduralGeography geography,double centreX,double centreZ)
        {
            int row=Resolution+1;var source=new SolidPoint[row*row];
            for(int z=0;z<=Resolution;z++)for(int x=0;x<=Resolution;x++)
            {double east=centreX+x-Resolution*.5,north=centreZ+z-Resolution*.5;source[z*row+x]=new SolidPoint(east,LocalSolidField.RenderedGround(geography,east,north),north);}
            return source;
        }
    }
}
