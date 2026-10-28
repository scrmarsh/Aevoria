using Aevoria.World;
using UnityEngine;
namespace Aevoria.Presentation
{
    public static class EnvironmentPalette
    {
        public static Color BiomeColor(Biome biome)
        {
            switch(biome)
            {
                case Biome.MediterraneanWoodland:return new Color(.32f,.43f,.18f);
                case Biome.MediterraneanScrub:return new Color(.57f,.55f,.30f);
                case Biome.RiparianForest:return new Color(.22f,.46f,.16f);
                case Biome.RockyCoast:return new Color(.61f,.60f,.49f);
                case Biome.SandyCoast:return new Color(.88f,.79f,.53f);
                case Biome.Steppe:return new Color(.66f,.64f,.36f);
                case Biome.RockyDesert:return new Color(.61f,.45f,.29f);
                case Biome.SaltFlat:return new Color(.91f,.89f,.80f);
                case Biome.Oasis:return new Color(.30f,.52f,.19f);
                case Biome.Alpine:return new Color(.48f,.57f,.30f);
                case Biome.AlpineScree:return new Color(.56f,.55f,.49f);
            }
            switch(biome){case Biome.Ocean:return new Color(.025f,.10f,.20f);case Biome.Coast:return new Color(.70f,.64f,.40f);case Biome.Desert:return new Color(.85f,.57f,.19f);case Biome.ColdDesert:return new Color(.57f,.49f,.37f);case Biome.Grassland:return new Color(.43f,.61f,.22f);case Biome.Forest:return new Color(.13f,.37f,.13f);case Biome.TemperateConiferousForest:return new Color(.16f,.35f,.24f);case Biome.TropicalForest:return new Color(.025f,.28f,.13f);case Biome.TropicalSeasonalForest:return new Color(.16f,.47f,.13f);case Biome.BorealForest:return new Color(.23f,.39f,.34f);case Biome.Savanna:return new Color(.66f,.62f,.24f);case Biome.Shrubland:return new Color(.48f,.47f,.25f);case Biome.Mediterranean:return new Color(.62f,.55f,.29f);case Biome.Wetland:return new Color(.15f,.46f,.46f);case Biome.Tundra:return new Color(.56f,.57f,.43f);case Biome.Snow:return new Color(.86f,.91f,.94f);default:return new Color(.48f,.48f,.48f);}
        }
        public static Color Temperature(float c){float t=Mathf.InverseLerp(-40,40,c);return t<.5f?Color.Lerp(new Color(.08f,.22f,.75f),new Color(.88f,.93f,.91f),t*2):Color.Lerp(new Color(.88f,.93f,.91f),new Color(.82f,.09f,.025f),(t-.5f)*2);}
    }
}
