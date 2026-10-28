from pathlib import Path
import json,sys
root=Path(__file__).resolve().parents[1];build=(root/sys.argv[1]).resolve();evidence=root/'Docs/Validation/Alpha-v0.0.14'
baseline=json.loads((evidence/'baseline.json').read_text())
files=[p for p in build.rglob('*') if p.is_file()]
installed=sum(p.stat().st_size for p in files)
recipe_path=(root/sys.argv[2]).resolve() if len(sys.argv)>2 else evidence/'material-recipe-checks5.json'
recipe=json.loads(recipe_path.read_text())
sources=['Assets/Shaders/GenesisMaterials.hlsl','Assets/Scripts/World/SurfaceMaterialRecipes.cs','Assets/Scripts/Presentation/GenesisMaterialRenderer.cs']
report=dict(build=str(build),status='Development prototype, not a release',installedBytes=installed,
    fileCount=len(files),baselineInstalledBytes=baseline['installedDevelopmentBytes'],
    installedDeltaBytes=installed-baseline['installedDevelopmentBytes'],
    sizeScope='Complete development build with retained debug support, compared using the same directory-size method',
    recipeVersion=recipe['recipeVersion'],recipeIdentity=recipe['identity'],packedDefinitionsBytes=recipe['packedRecipeBytes'],
    gpuRecipeCapacityBytes=recipe['uniformRecipeCapacityBytes'],gpuSplitOriginCapacityBytes=16*4*2*16,
    recipeReport=str(recipe_path),geologicalInputTextureBytes=129*129*8,habitatInputTextureBytes=129*129*8,
    sourceBytes={p:(root/p).stat().st_size for p in sources},
    notes=['Packed definition bytes are not total code/shader/asset memory.',
           'Baseline terrain already used procedural math; no texture-library storage savings claimed.',
           'No full generated material image maps are written to world saves.',
           'Final compressed release, shader attribution, cache and sustained graphics-memory measurements remain pending.'])
(evidence/(build.name+'-size.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
