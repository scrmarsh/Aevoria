using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Aevoria.World
{
    public enum SurfaceFamily { Limestone,Sandstone,Granite,Metamorphic,Sedimentary,RiverStone,Soil,Grassland,Mediterranean,ForestFloor,Sand,Salt,Snow,Ice }
    public enum SurfaceNoise { Gradient,Ridged,Cellular }
    public readonly struct SurfaceTint
    {
        public readonly float R,G,B;
        public SurfaceTint(float r,float g,float b)
        {if(!Valid(r)||!Valid(g)||!Valid(b))throw new ArgumentOutOfRangeException(nameof(r));R=r;G=g;B=b;}
        private static bool Valid(float x)=>WorldConfiguration.Finite(x)&&x>=0&&x<=1;
    }
    // Rendering recipes never participate in the saved geography module graph.
    // Float packing is a versioned CPU/GPU contract, independent of Unity types.
    public sealed class SurfaceMaterialRecipe
    {
        public const int FormatVersion=1,Rows=8,Floats=Rows*4;
        public SurfaceFamily Family{get;}public uint Seed{get;}public int Version{get;}
        public SurfaceTint Dark{get;}public SurfaceTint Light{get;}
        public float MacroFrequency{get;}public float MesoFrequency{get;}public float MicroFrequency{get;}
        public float Roughness{get;}public float NormalStrength{get;}public float WarpMeters{get;}
        public float StrataFrequency{get;}public float StrataDip{get;}public float FractureWidth{get;}public float Weathering{get;}
        public float MoistureResponse{get;}public float SnowRetention{get;}public float MossAffinity{get;}public float TemperatureResponse{get;}
        public float MinimumTemperature{get;}public float MaximumTemperature{get;}public float MinimumMoisture{get;}public float MaximumMoisture{get;}
        public SurfaceNoise Noise{get;}public int Octaves{get;}
        public SurfaceMaterialRecipe(SurfaceFamily family,uint seed,SurfaceTint dark,SurfaceTint light,
            float roughness=.8f,float normal=.12f,float macro=.0005f,float meso=.035f,float micro=1.7f,
            float warp=5,float strata=.11f,float dip=.12f,float cracks=.05f,float weathering=.45f,
            float wet=.3f,float snow=.7f,float moss=.3f,float thermal=.2f,SurfaceNoise noise=SurfaceNoise.Gradient,
            int octaves=3,int version=1,float minimumTemperature=-90,float maximumTemperature=60,float minimumMoisture=0,float maximumMoisture=1)
        {
            float[] values={roughness,normal,macro,meso,micro,warp,strata,dip,cracks,weathering,wet,snow,moss,thermal,minimumTemperature,maximumTemperature,minimumMoisture,maximumMoisture};
            foreach(float v in values)if(!WorldConfiguration.Finite(v))throw new ArgumentException("Nonfinite material recipe.");
            foreach(float v in new[]{weathering,wet,snow,moss,thermal})if(v<0||v>1)throw new ArgumentException("Material response must be in [0,1].");
            if(!Enum.IsDefined(typeof(SurfaceFamily),family)||!Enum.IsDefined(typeof(SurfaceNoise),noise)||version<1||octaves<1||octaves>4||roughness<.04f||roughness>1||normal<0||normal>1||macro<=0||meso<=macro||micro<=meso||micro>8||warp<0||warp>100||strata<=0||cracks<=0||cracks>.3||minimumTemperature>maximumTemperature||minimumMoisture<0||maximumMoisture>1||minimumMoisture>maximumMoisture)throw new ArgumentException("Invalid material recipe.");
            Family=family;Seed=seed;Dark=dark;Light=light;Roughness=roughness;NormalStrength=normal;MacroFrequency=macro;MesoFrequency=meso;MicroFrequency=micro;WarpMeters=warp;StrataFrequency=strata;StrataDip=dip;FractureWidth=cracks;Weathering=weathering;MoistureResponse=wet;SnowRetention=snow;MossAffinity=moss;TemperatureResponse=thermal;Noise=noise;Octaves=octaves;Version=version;MinimumTemperature=minimumTemperature;MaximumTemperature=maximumTemperature;MinimumMoisture=minimumMoisture;MaximumMoisture=maximumMoisture;
        }
        public float[] Pack()=>new[]{Dark.R,Dark.G,Dark.B,Roughness, Light.R,Light.G,Light.B,NormalStrength,
            MacroFrequency,MesoFrequency,MicroFrequency,WarpMeters, StrataFrequency,StrataDip,FractureWidth,Weathering,
            MoistureResponse,SnowRetention,MossAffinity,TemperatureResponse, (float)Noise,Octaves,(float)(Seed&65535),(float)(Seed>>16),
            MinimumTemperature,MaximumTemperature,MinimumMoisture,MaximumMoisture, (float)Family,Version,FormatVersion,0};
        public bool Suitable(float temperature,float moisture)=>temperature>=MinimumTemperature&&temperature<=MaximumTemperature&&moisture>=MinimumMoisture&&moisture<=MaximumMoisture;
    }
    public static class SurfaceMaterialRecipes
    {
        public const int Version=6,Capacity=16;
        public static IReadOnlyList<SurfaceMaterialRecipe> All{get;}=Array.AsReadOnly(new[]{
            new SurfaceMaterialRecipe(SurfaceFamily.Limestone,101,new SurfaceTint(.38f,.36f,.29f),new SurfaceTint(.84f,.81f,.71f),meso:.06f,micro:6,warp:14,strata:.12f,normal:.32f,noise:SurfaceNoise.Cellular),
            new SurfaceMaterialRecipe(SurfaceFamily.Sandstone,211,new SurfaceTint(.29f,.15f,.08f),new SurfaceTint(.66f,.43f,.24f),strata:.22f,dip:.06f,normal:.17f),
            new SurfaceMaterialRecipe(SurfaceFamily.Granite,307,new SurfaceTint(.19f,.22f,.23f),new SurfaceTint(.50f,.53f,.52f),strata:.025f,weathering:.3f,noise:SurfaceNoise.Ridged),
            new SurfaceMaterialRecipe(SurfaceFamily.Metamorphic,401,new SurfaceTint(.075f,.095f,.10f),new SurfaceTint(.29f,.32f,.33f),strata:.3f,dip:.4f),
            new SurfaceMaterialRecipe(SurfaceFamily.Sedimentary,503,new SurfaceTint(.24f,.20f,.15f),new SurfaceTint(.56f,.48f,.36f),strata:.18f,normal:.16f),
            new SurfaceMaterialRecipe(SurfaceFamily.RiverStone,601,new SurfaceTint(.16f,.18f,.17f),new SurfaceTint(.40f,.42f,.37f),roughness:.52f,normal:.06f,wet:.55f),
            new SurfaceMaterialRecipe(SurfaceFamily.Soil,701,new SurfaceTint(.07f,.045f,.025f),new SurfaceTint(.27f,.18f,.09f),normal:.09f,wet:.55f),
            new SurfaceMaterialRecipe(SurfaceFamily.Grassland,809,new SurfaceTint(.065f,.13f,.035f),new SurfaceTint(.27f,.32f,.10f),normal:.055f,moss:.65f,minimumTemperature:-5,minimumMoisture:.2f),
            new SurfaceMaterialRecipe(SurfaceFamily.Mediterranean,907,new SurfaceTint(.085f,.15f,.04f),new SurfaceTint(.26f,.33f,.11f),normal:.08f,moss:.4f,version:2,minimumTemperature:8,maximumMoisture:.65f),
            new SurfaceMaterialRecipe(SurfaceFamily.ForestFloor,1009,new SurfaceTint(.035f,.045f,.02f),new SurfaceTint(.18f,.15f,.07f),normal:.075f,wet:.55f,moss:.85f,minimumMoisture:.25f),
            new SurfaceMaterialRecipe(SurfaceFamily.Sand,1103,new SurfaceTint(.43f,.31f,.16f),new SurfaceTint(.73f,.60f,.36f),roughness:.88f,normal:.045f,strata:.45f,moss:0),
            new SurfaceMaterialRecipe(SurfaceFamily.Salt,1201,new SurfaceTint(.55f,.53f,.44f),new SurfaceTint(.87f,.85f,.73f),normal:.055f,macro:.002f,meso:.8f,micro:6,warp:.2f,noise:SurfaceNoise.Cellular,moss:0,version:2,maximumMoisture:.2f),
            new SurfaceMaterialRecipe(SurfaceFamily.Snow,1301,new SurfaceTint(.60f,.70f,.78f),new SurfaceTint(.92f,.94f,.95f),roughness:.72f,normal:.025f,moss:0,maximumTemperature:3),
            new SurfaceMaterialRecipe(SurfaceFamily.Ice,1409,new SurfaceTint(.12f,.25f,.30f),new SurfaceTint(.54f,.70f,.75f),roughness:.18f,normal:.03f,moss:0,maximumTemperature:0)
        });
        public static uint WorldSeed(string seed){ulong value=WorldGenerationSeeds.Derive(seed,"surface-render-recipes",Version);return (uint)(value^(value>>32));}
        public static string Identity()
        {
            using(var data=new MemoryStream())using(var writer=new BinaryWriter(data))
            {writer.Write(Version);writer.Write(SurfaceMaterialRecipe.FormatVersion);foreach(var recipe in All)foreach(float value in recipe.Pack())writer.Write(value);writer.Flush();using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(data.ToArray())).Replace("-","");}
        }
    }
    public readonly struct SurfaceLatticeOrigin
    {
        public readonly int Cell;public readonly float Fraction;
        public SurfaceLatticeOrigin(double position,float frequency)
        {
            if(!WorldConfiguration.Finite(position)||!WorldConfiguration.Finite(frequency)||frequency<=0)throw new ArgumentException("Invalid material coordinate.");
            double scaled=position*frequency,cell=Math.Floor(scaled);
            if(cell< -16777216||cell>16777216)throw new ArgumentOutOfRangeException(nameof(position),"Material lattice exceeds exact GPU integer storage.");
            Cell=(int)cell;Fraction=(float)(scaled-cell);
        }
    }
}
