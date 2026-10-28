using System.IO;
using UnityEditor;
using UnityEngine;

namespace Aevoria.Editor
{
    public static class Phase0Assets
    {
        public static void Create()
        {
            const string path = "Assets/Resources/PlaceholderGround.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Directory.CreateDirectory("Assets/Resources");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException("URP Lit shader is unavailable in the Editor.");
            var material = new Material(shader);
            material.SetColor("_BaseColor", new Color(0.23f, 0.42f, 0.27f, 1));
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            Debug.Log("Created Phase 0 URP placeholder material.");
        }
    }
}