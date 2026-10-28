using System;
using System.IO;
using Aevoria.Core;
using Aevoria.World;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aevoria.Editor
{
    public static class Phase1Validation
    {
        [MenuItem("Aevoria/Prepare Phase 1 materials")]
        public static void PrepareAssets()
        {
            Directory.CreateDirectory("Assets/Resources/History");
            if(File.Exists("CHANGELOG.md")) File.Copy("CHANGELOG.md","Assets/Resources/History/Changelog.txt",true);
            if(File.Exists("Docs/THE_CHRONICLES_OF_AEVORIA.md")) File.Copy("Docs/THE_CHRONICLES_OF_AEVORIA.md","Assets/Resources/History/Chronicles.txt",true);
            Create("WorldRain", "Universal Render Pipeline/Particles/Unlit", Color.white);
            Create("WorldTerrain", "Aevoria/Terrain", Color.white);
            Create("WorldWater", "Aevoria/Water", new Color(0.035f,0.22f,0.29f));
            Create("WorldVegetation", "Aevoria/Vegetation", Color.white);
            Create("WorldHorizon", "Aevoria/Horizon", Color.white);
            Create("CharacterSurface","Aevoria/Character Surface",Color.white);
            Create("EquipmentSurface","Aevoria/Character Surface",Color.white);
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/EquipmentSurface.mat").SetFloat("_SurfaceClass",1);
            const string effectsPath="Assets/Resources/WorldEffects.asset";
            if(AssetDatabase.LoadAssetAtPath<VolumeProfile>(effectsPath)==null)
            {
                var profile=ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile,effectsPath);
                var blur=profile.Add<MotionBlur>(true); blur.intensity.Override(0.13f); blur.mode.Override(MotionBlurMode.CameraOnly);
                var tone=profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
                var color=profile.Add<ColorAdjustments>(true); color.postExposure.Override(0.2f); color.saturation.Override(4); color.contrast.Override(7);
                foreach(var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
            CharacterMotion012Export.EnsureAssets();
        }
        private static void Create(string name,string shaderName,Color color)
        {
            string path="Assets/Resources/"+name+".mat";
            var shader=Shader.Find(shaderName); if(shader==null) throw new InvalidOperationException(shaderName+" unavailable");
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing!=null) { existing.shader=shader; existing.enableInstancing=name=="WorldVegetation"; EditorUtility.SetDirty(existing); return; }
            var material=new Material(shader); if(material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
            material.enableInstancing=name=="WorldVegetation";
            if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",name=="WorldWater"?0.88f:0.2f);
            if(material.HasProperty("_Metallic")) material.SetFloat("_Metallic",name=="WorldWater"?0.25f:0);
            AssetDatabase.CreateAsset(material,path);
        }
        public static void BuildWindows()
        {
            // D3D12 on the validated Windows driver crashes in D3D12Core during
            // process shutdown. Ship the backend with clean native exit evidence.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            PrepareAssets();
            PlayerSettings.bundleVersion=ApplicationVersion.Number;
            string output=Environment.GetEnvironmentVariable("AEVORIA_BUILD_PATH");
            if(string.IsNullOrEmpty(output)) output="Builds/"+ApplicationVersion.Stage+"-v"+ApplicationVersion.Number+"/Aevoria.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{"Assets/Scenes/SampleScene.unity"},
                locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.CleanBuildCache });
            Debug.Log($"PHASE1_BUILD {report.summary.result} {report.summary.totalSize} bytes {report.summary.totalTime}");
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-info.json"),JsonUtility.ToJson(new BuildInfo {
                version=ApplicationVersion.Number,stage=ApplicationVersion.Stage,status=ApplicationVersion.Status,
                displayName=ApplicationVersion.DisplayName,generatorVersion=ProceduralGeography.GeneratorVersion,
                unity=Application.unityVersion,builtUtc=DateTime.UtcNow.ToString("O"),commit=null},true));
        }
        [Serializable] private sealed class BuildInfo {public string version,stage,status,displayName,unity,builtUtc,commit;public int generatorVersion;}
    }
}
