namespace Aevoria.World
{
    // Tree slots: 0 spruce, 1 pine, 2 fir, 3 oak, 4 birch, 5 beech,
    // 6 olive/evergreen oak, 7 cypress, 8 umbrella pine, 9 palm,
    // 10 willow, 11 juniper. Cover: grass, fern, scrub, reeds,
    // meadow flowers, moss/lichen, salt shrubs, beach grass.
    public readonly struct BiomeHabitat
    {
        public readonly int TreeA,TreeB,Cover;public readonly double TreeDensity,CoverDensity;
        public BiomeHabitat(int a,int b,int cover,double trees,double ground)
        {TreeA=a;TreeB=b;Cover=cover;TreeDensity=trees;CoverDensity=ground;}
        public static BiomeHabitat For(Biome biome)
        {
            switch(biome)
            {
                case Biome.Ocean:case Biome.Snow:return new BiomeHabitat(0,0,5,0,0);
                case Biome.Tundra:case Biome.ColdDesert:return new BiomeHabitat(11,11,5,0,.35);
                case Biome.Alpine:return new BiomeHabitat(11,11,4,0,.6);
                case Biome.AlpineScree:return new BiomeHabitat(11,11,5,0,.09);
                case Biome.BorealForest:return new BiomeHabitat(0,4,5,1,.7);
                case Biome.TemperateConiferousForest:return new BiomeHabitat(1,2,1,1,.75);
                case Biome.Forest:return new BiomeHabitat(3,5,1,1,1);
                case Biome.Grassland:return new BiomeHabitat(3,4,4,.07,1);
                case Biome.Mediterranean:case Biome.MediterraneanWoodland:return new BiomeHabitat(6,8,2,.48,.65);
                case Biome.MediterraneanScrub:case Biome.Shrubland:return new BiomeHabitat(7,11,2,.12,.7);
                case Biome.RiparianForest:return new BiomeHabitat(10,4,3,.9,1);
                case Biome.Wetland:return new BiomeHabitat(10,10,3,.16,1);
                case Biome.RockyCoast:return new BiomeHabitat(8,11,6,.035,.2);
                case Biome.SandyCoast:case Biome.Coast:return new BiomeHabitat(8,8,7,0,.45);
                case Biome.Steppe:case Biome.Savanna:return new BiomeHabitat(11,11,0,.015,.5);
                case Biome.RockyDesert:return new BiomeHabitat(11,11,2,0,.05);
                case Biome.Desert:return new BiomeHabitat(9,9,2,0,.015);
                case Biome.SaltFlat:return new BiomeHabitat(11,11,6,0,.015);
                case Biome.Oasis:return new BiomeHabitat(9,10,3,.65,1);
                default:return new BiomeHabitat(3,5,1,.8,.8);
            }
        }
    }
}
