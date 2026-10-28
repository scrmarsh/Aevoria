using System;
namespace Aevoria.World
{
    public readonly struct CivicFootprint
    {
        public readonly double HalfWidth,HalfDepth;
        public CivicFootprint(double width,double depth){HalfWidth=width;HalfDepth=depth;}
    }
    public static class CivicGeometry
    {
        // Includes eaves, attached work areas, porches and storage crates.
        public static CivicFootprint Footprint(CivicBuilding building)
        {
            switch(building.Kind)
            {
                case CivicBuildingKind.Dwelling:return building.Variant==1?new CivicFootprint(4.3,3.5):building.Variant==2?new CivicFootprint(4,4):new CivicFootprint(3,3);
                case CivicBuildingKind.Granary:return new CivicFootprint(3.5,4);
                case CivicBuildingKind.MeetingHall:return new CivicFootprint(4,5.5);
                case CivicBuildingKind.Farm:return new CivicFootprint(3.6,3.1);
                case CivicBuildingKind.Workshop:return new CivicFootprint(4.4,3);
                case CivicBuildingKind.Storehouse:return new CivicFootprint(3,4);
                default:return new CivicFootprint(4.1,3.1);
            }
        }
        public static GeoPoint Door(CivicBuilding building)
        {double angle=building.Yaw*Math.PI/180,distance=Footprint(building).HalfDepth+1.6;return new GeoPoint(building.X+Math.Sin(angle)*distance,building.Z+Math.Cos(angle)*distance);}
        public static bool Contains(CivicBuilding building,double x,double z,double margin=0)
        {var box=Footprint(building);double angle=building.Yaw*Math.PI/180,dx=x-building.X,dz=z-building.Z,c=Math.Cos(angle),s=Math.Sin(angle);return Math.Abs(dx*c-dz*s)<box.HalfWidth+margin&&Math.Abs(dx*s+dz*c)<box.HalfDepth+margin;}
    }
}
