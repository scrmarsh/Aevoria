using Aevoria.World;
using NUnit.Framework;
using UnityEngine;

namespace Aevoria.Tests
{
    public sealed class GenesisMaterialGpuTests
    {
        [Test]
        public void RenderedMaterialFamiliesRemainFiniteAndContinuousAcrossRebases()
        {
            var shader=Shader.Find("Hidden/Aevoria/GenesisNoiseProbe");Assert.That(shader!=null&&shader.isSupported);
            var material=new Material(shader);var target=new RenderTexture(16,16,0,RenderTextureFormat.ARGBFloat);target.Create();
            var readback=new Texture2D(16,16,TextureFormat.RGBAFloat,false,true);var previous=RenderTexture.active;
            var savedCells=Shader.GetGlobalVectorArray("_GenesisCells");var savedFractions=Shader.GetGlobalVectorArray("_GenesisFractions");
            var savedRecipes=Shader.GetGlobalVectorArray("_GenesisRecipes");int savedSeed=Shader.GetGlobalInteger("_GenesisSeed");
            try
            {
                var recipes=new Vector4[128];
                foreach(var recipe in SurfaceMaterialRecipes.All)
                {var packed=recipe.Pack();for(int row=0;row<8;row++)recipes[(int)recipe.Family*8+row]=new Vector4(packed[row*4],packed[row*4+1],packed[row*4+2],packed[row*4+3]);}
                Shader.SetGlobalVectorArray("_GenesisRecipes",recipes);Shader.SetGlobalInteger("_GenesisSeed",12345);material.SetInt("_ProbeMode",1);
                foreach(var recipe in SurfaceMaterialRecipes.All)foreach(double position in new[]{-1638000.37,1638000.37})
                {
                    Color[] Read(double anchor)
                    {
                        var cells=new Vector4[64];var fractions=new Vector4[64];
                        for(int band=0;band<4;band++)
                        {
                            float frequency=band==0?recipe.MacroFrequency:band==1?recipe.MesoFrequency:band==2?recipe.MicroFrequency:recipe.StrataFrequency;
                            var x=new SurfaceLatticeOrigin(anchor,frequency);var z=new SurfaceLatticeOrigin(-anchor,frequency);int at=(int)recipe.Family*4+band;
                            cells[at]=new Vector4(x.Cell,0,z.Cell,0);fractions[at]=new Vector4(x.Fraction,0,z.Fraction,0);
                        }
                        Shader.SetGlobalVectorArray("_GenesisCells",cells);Shader.SetGlobalVectorArray("_GenesisFractions",fractions);
                        material.SetVector("_ProbeLocal",new Vector4((float)(position-anchor),0,(float)(-position+anchor),0));material.SetInt("_ProbeFamily",(int)recipe.Family);
                        Graphics.Blit(Texture2D.blackTexture,target,material);RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,16,16),0,0);readback.Apply();return readback.GetPixels();
                    }
                    double origin=System.Math.Floor(position/1024)*1024;var before=Read(origin);var after=Read(origin+1024);float variation=0;
                    Assert.That(UnityEditor.ShaderUtil.ShaderHasError(shader),Is.False,"Material probe compiled successfully");
                    for(int pixel=0;pixel<before.Length;pixel++)for(int channel=0;channel<4;channel++)
                    {
                        float value=before[pixel][channel];Assert.That(float.IsNaN(value)||float.IsInfinity(value),Is.False,recipe.Family.ToString());
                        Assert.That(after[pixel][channel],Is.EqualTo(value).Within(.005f),"Rendered rebase continuity: "+recipe.Family+" / "+channel);
                        if(channel<3){Assert.That(value,Is.InRange(0f,1f));variation=Mathf.Max(variation,Mathf.Abs(value-before[0][channel]));}
                    }
                    Assert.That(variation,Is.GreaterThan(.0001f),"Spatially varying rendered family: "+recipe.Family);
                }
            }
            finally
            {
                if(savedCells!=null&&savedCells.Length>0)Shader.SetGlobalVectorArray("_GenesisCells",savedCells);
                if(savedFractions!=null&&savedFractions.Length>0)Shader.SetGlobalVectorArray("_GenesisFractions",savedFractions);
                if(savedRecipes!=null&&savedRecipes.Length>0)Shader.SetGlobalVectorArray("_GenesisRecipes",savedRecipes);Shader.SetGlobalInteger("_GenesisSeed",savedSeed);
                RenderTexture.active=previous;Object.DestroyImmediate(readback);target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(material);
            }
        }
        [TestCase(-2)][TestCase(0)][TestCase(2)]
        public void ActualGpuNoiseIsContinuousAcrossLargePositiveAndNegativeOriginShifts(int octave)
        {
            var shader=Shader.Find("Hidden/Aevoria/GenesisNoiseProbe");Assert.That(shader!=null&&shader.isSupported,"Supported native GPU probe shader");
            var material=new Material(shader);var target=new RenderTexture(16,16,0,RenderTextureFormat.ARGBFloat);target.Create();
            material.SetInt("_ProbeMode",0);
            var readback=new Texture2D(16,16,TextureFormat.RGBAFloat,false,true);var previous=RenderTexture.active;
            var savedCells=Shader.GetGlobalVectorArray("_GenesisCells");var savedFractions=Shader.GetGlobalVectorArray("_GenesisFractions");
            try
            {
                bool varied=false;
                foreach(float frequency in new[]{.0005f,.06f,2.6f,6f})foreach(double position in new[]{-1638000.37,-.37,1638000.37})for(int projection=0;projection<4;projection++)
                {
                    Color[] Read(double anchor)
                    {
                        var cells=new Vector4[64];var fractions=new Vector4[64];
                        var x=new SurfaceLatticeOrigin(anchor,frequency);var z=new SurfaceLatticeOrigin(-anchor,frequency);
                        cells[1]=new Vector4(x.Cell,0,z.Cell,0);fractions[1]=new Vector4(x.Fraction,0,z.Fraction,0);
                        Shader.SetGlobalVectorArray("_GenesisCells",cells);Shader.SetGlobalVectorArray("_GenesisFractions",fractions);
                        material.SetVector("_ProbeLocal",new Vector4((float)(position-anchor),0,(float)(-position+anchor),0));
                        material.SetFloat("_ProbeFrequency",frequency);material.SetInt("_ProbeOctave",octave);material.SetInt("_ProbeProjection",projection);
                        Graphics.Blit(Texture2D.blackTexture,target,material);RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,16,16),0,0);readback.Apply();return readback.GetPixels();
                    }
                    double origin=System.Math.Floor(position/1024)*1024;var before=Read(origin);var after=Read(origin+1024);
                    float minimum=1,maximum=0;
                    for(int pixel=0;pixel<before.Length;pixel++)
                    {
                        Assert.That(before[pixel].r,Is.InRange(0f,1f));minimum=Mathf.Min(minimum,before[pixel].r);maximum=Mathf.Max(maximum,before[pixel].r);
                        Assert.That(after[pixel].r,Is.EqualTo(before[pixel].r).Within(.002f),"GPU phase changed: "+position+"/"+frequency+"/"+projection);
                    }
                    varied|=maximum-minimum>.005f;
                }
                Assert.That(varied,"Readback contains varying procedural samples, not an empty/constant render target");
            }
            finally
            {
                if(savedCells!=null&&savedCells.Length>0)Shader.SetGlobalVectorArray("_GenesisCells",savedCells);if(savedFractions!=null&&savedFractions.Length>0)Shader.SetGlobalVectorArray("_GenesisFractions",savedFractions);
                RenderTexture.active=previous;Object.DestroyImmediate(readback);target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(material);
            }
        }
    }
}
