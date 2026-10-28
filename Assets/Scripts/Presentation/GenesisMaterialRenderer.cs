using Aevoria.World;
using UnityEngine;

namespace Aevoria.Presentation
{
    // Shared, compact render data. Never writes geography, meshes or save state.
    public sealed class GenesisMaterialRenderer:MonoBehaviour
    {
        public const int Bands=4;
        private readonly Vector4[] recipes=new Vector4[SurfaceMaterialRecipes.Capacity*SurfaceMaterialRecipe.Rows];
        private readonly Vector4[] cells=new Vector4[SurfaceMaterialRecipes.Capacity*Bands],fractions=new Vector4[SurfaceMaterialRecipes.Capacity*Bands];
        private Texture2D geology,habitats;
        private bool initialized;
        public void Initialize(ProceduralGeography world)
        {
            if(AevoriaBootstrap.HasFlag("-aevoria014LegacyMaterials")){Shader.SetGlobalFloat("_GenesisReady",0);return;}
            for(int i=0;i<SurfaceMaterialRecipes.All.Count;i++)
            {
                float[] packed=SurfaceMaterialRecipes.All[i].Pack();
                for(int row=0;row<SurfaceMaterialRecipe.Rows;row++)recipes[i*SurfaceMaterialRecipe.Rows+row]=new Vector4(packed[row*4],packed[row*4+1],packed[row*4+2],packed[row*4+3]);
            }
            Shader.SetGlobalVectorArray("_GenesisRecipes",recipes);
            Shader.SetGlobalInteger("_GenesisSeed",unchecked((int)SurfaceMaterialRecipes.WorldSeed(world.Configuration.Seed)));
            Shader.SetGlobalFloat("_GenesisRecipeVersion",SurfaceMaterialRecipes.Version);
            const int n=129;var values=new Color[n*n];var habitatValues=new Color[n*n];var config=world.Configuration;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                double east=(x/(double)(n-1)-.5)*config.WidthMeters,north=(z/(double)(n-1)-.5)*config.DepthMeters;
                var g=world.Geology.Sample(east,north);values[z*n+x]=new Color(g.DirectionX,g.DirectionZ,g.Uplift,g.Rift);
                var sample=world.Sample(new GeoPoint(east,north));var habitat=BiomeHabitat.For(sample.Biome);
                bool mediterranean=sample.Biome==Biome.Mediterranean||sample.Biome==Biome.MediterraneanWoodland||sample.Biome==Biome.MediterraneanScrub;
                habitatValues[z*n+x]=new Color((float)habitat.TreeDensity,mediterranean?1:0,sample.Biome==Biome.SaltFlat?1:0,(float)world.Hydrology.MoistureInfluence(east,north));
            }
            if(geology!=null)Destroy(geology);
            geology=new Texture2D(n,n,TextureFormat.RGBAHalf,false,true){name="Genesis geological strike and province inputs",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            geology.SetPixels(values);geology.Apply(false,true);Shader.SetGlobalTexture("_GenesisGeology",geology);
            if(habitats!=null)Destroy(habitats);
            habitats=new Texture2D(n,n,TextureFormat.RGBAHalf,false,true){name="Genesis canopy, Mediterranean, salt basin and riparian masks",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            habitats.SetPixels(habitatValues);habitats.Apply(false,true);Shader.SetGlobalTexture("_GenesisHabitats",habitats);
            if(!initialized){WorldOrigin.Shifted+=OriginShifted;initialized=true;}
            PublishOrigin();Shader.SetGlobalFloat("_GenesisReady",1);
        }
        private void OriginShifted(Vector3 shift)=>PublishOrigin();
        private void PublishOrigin()
        {
            var origin=WorldOrigin.Offset;
            for(int i=0;i<SurfaceMaterialRecipes.All.Count;i++)
            {
                var recipe=SurfaceMaterialRecipes.All[i];
                for(int band=0;band<Bands;band++)
                {
                    float frequency=band==0?recipe.MacroFrequency:band==1?recipe.MesoFrequency:band==2?recipe.MicroFrequency:recipe.StrataFrequency;
                    var x=new SurfaceLatticeOrigin(origin.x,frequency);var y=new SurfaceLatticeOrigin(origin.y,frequency);var z=new SurfaceLatticeOrigin(origin.z,frequency);int at=i*Bands+band;
                    cells[at]=new Vector4(x.Cell,y.Cell,z.Cell,0);fractions[at]=new Vector4(x.Fraction,y.Fraction,z.Fraction,0);
                }
            }
            Shader.SetGlobalVectorArray("_GenesisCells",cells);Shader.SetGlobalVectorArray("_GenesisFractions",fractions);
        }
        private void OnDestroy()
        {
            if(initialized)WorldOrigin.Shifted-=OriginShifted;
            if(geology!=null){if(Shader.GetGlobalTexture("_GenesisGeology")==geology){Shader.SetGlobalFloat("_GenesisReady",0);Shader.SetGlobalTexture("_GenesisGeology",Texture2D.blackTexture);}Destroy(geology);}
            if(habitats!=null){if(Shader.GetGlobalTexture("_GenesisHabitats")==habitats)Shader.SetGlobalTexture("_GenesisHabitats",Texture2D.blackTexture);Destroy(habitats);}
        }
    }
}
