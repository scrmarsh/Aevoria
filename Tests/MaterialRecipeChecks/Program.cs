using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aevoria.World;
using Aevoria.Persistence;

if(args.Length>2&&args[1]=="--archive")
{
    byte[] original=File.ReadAllBytes(args[2]);var record=ArchiveCodec.Unpack(original);
    if(record.ApplicationVersion!="0.0.13")throw new Exception("Expected actual pre-Genesis native save");
    byte[] gameplayBefore=ArchiveCodec.Encode(record);record.Validate();
    var world=record.Rebuild(progress:s=>Console.WriteLine("STAGE "+s));
    string fingerprint=WorldRecord.GeographyFingerprint(world);
    if(fingerprint!=record.Fingerprint||!original.SequenceEqual(File.ReadAllBytes(args[2]))||!gameplayBefore.SequenceEqual(ArchiveCodec.Encode(record)))throw new Exception("Existing archive, gameplay state or geography changed");
    File.WriteAllText(args[0],JsonSerializer.Serialize(new{result="Passed",sourceApplication=record.ApplicationVersion,currentApplication=Aevoria.Core.ApplicationVersion.Number,schema=record.Schema,generator=record.Generator,fingerprint,sourceBytesUnchanged=true,recordGameplayStateUnchanged=true,scope="Read/rebuild of actual native pre-Genesis world; original archive never written"},new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("PASS Actual v13 native archive loads/rebuilds with unchanged fingerprint and gameplay state");return;
}

int checks=0;
void Check(bool value,string reason){if(!value)throw new Exception(reason);checks++;Console.WriteLine("PASS "+reason);}
void Reject(Action action,string reason){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,reason);}
Check(SurfaceMaterialRecipes.All.Select(x=>x.Family).Distinct().Count()==Enum.GetValues<SurfaceFamily>().Length,"Every declared family has exactly one recipe");
Check(SurfaceMaterialRecipes.All.Count<=SurfaceMaterialRecipes.Capacity,"Library fits fixed GPU capacity");
foreach(var recipe in SurfaceMaterialRecipes.All)
{
    var data=recipe.Pack();
    Check(data.Length==32&&data.All(float.IsFinite),"Finite versioned GPU contract: "+recipe.Family);
    uint restored=(uint)data[22]|((uint)data[23]<<16);
    Check(restored==recipe.Seed&&(int)data[28]==(int)recipe.Family&&(int)data[30]==SurfaceMaterialRecipe.FormatVersion,"Lossless seed and identity packing: "+recipe.Family);
    foreach(float frequency in new[]{recipe.MacroFrequency,recipe.MesoFrequency,recipe.MicroFrequency,recipe.StrataFrequency})
    foreach(double position in new[]{-1638000.25,-1024.001,-.001,0,1023.999,1638000.25})
    {
        // Same point represented on either side of a 1024m floating-origin
        // shift. Bound phase error rather than trusting global float positions.
        double anchor=Math.Floor(position/1024)*1024;
        double Phase(double origin){var split=new SurfaceLatticeOrigin(origin,frequency);return split.Cell+(double)(float)((float)(position-origin)*frequency+split.Fraction);}
        if(Math.Abs(Phase(anchor)-Phase(anchor+1024))>.001||Math.Abs(Phase(anchor)-position*frequency)>.001)throw new Exception("Floating-origin material phase discontinuity: "+recipe.Family);
    }
}
checks++;Console.WriteLine("PASS Material lattice remains continuous across rebases, including negative and continental coordinates");
Check(SurfaceMaterialRecipes.WorldSeed("aevoria-001")==SurfaceMaterialRecipes.WorldSeed("aevoria-001")&&SurfaceMaterialRecipes.WorldSeed("aevoria-001")!=SurfaceMaterialRecipes.WorldSeed("aevoria-002"),"Stable material seed with independent worlds");
var tint=new SurfaceTint(.2f,.3f,.4f);
Reject(()=>new SurfaceMaterialRecipe(SurfaceFamily.Limestone,1,tint,tint,micro:float.NaN),"Nonfinite recipes rejected");
Reject(()=>new SurfaceMaterialRecipe(SurfaceFamily.Limestone,1,tint,tint,octaves:5),"Unbounded octave work rejected");
Reject(()=>new SurfaceMaterialRecipe(SurfaceFamily.Limestone,1,tint,tint,wet:-1),"Invalid response masks rejected");
Reject(()=>new SurfaceLatticeOrigin(double.PositiveInfinity,.2f),"Nonfinite world coordinates rejected");
Check(!SurfaceMaterialRecipes.All[(int)SurfaceFamily.Salt].Suitable(20,.8f)&&!SurfaceMaterialRecipes.All[(int)SurfaceFamily.Snow].Suitable(30,.6f),"Declared salt and snow climate restrictions remain explicit");
Check(WorldGenerationIdentity.ModuleVersions.Count==15&&WorldGenerationIdentity.ModuleVersions[WorldLayerIds.Climate]==6&&WorldGenerationIdentity.ModuleVersions[WorldLayerIds.Materials]==1,"Render recipes do not change the existing saved geography module identity");
File.WriteAllText(args[0],JsonSerializer.Serialize(new{checks,recipeVersion=SurfaceMaterialRecipes.Version,identity=SurfaceMaterialRecipes.Identity(),packedRecipeBytes=SurfaceMaterialRecipes.All.Count*32*4,uniformRecipeCapacityBytes=SurfaceMaterialRecipes.Capacity*32*4,scope="Recipe data, numerical origin continuity and unchanged geography identity; native shader review is separate"},new JsonSerializerOptions{WriteIndented=true}));
