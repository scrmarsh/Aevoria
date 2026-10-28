using System;
using System.IO;
using System.Linq;
using Aevoria.Presentation;
using Aevoria.World;
using UnityEditor;
using UnityEngine;

namespace Aevoria.Editor
{
    public static class CharacterAssetExport
    {
        [Serializable] private sealed class BoneData {public string name;public int parent;public Vector3 position;}
        [Serializable] private sealed class SkinData {public string preset;public Vector3[] vertices,caneVertices;public int[] triangles,caneTriangles;public Color[] colors;public BoneWeight[] weights;public BoneData[] bones;}
        public static void Export()
        {
            string folder=Environment.GetEnvironmentVariable("AEVORIA_CHARACTER_SOURCE");if(string.IsNullOrEmpty(folder))throw new InvalidOperationException("Set AEVORIA_CHARACTER_SOURCE to the editable-source output folder.");Directory.CreateDirectory(folder);
            foreach(CharacterAppearance preset in Enum.GetValues(typeof(CharacterAppearance)))
            {
                var root=new GameObject("Export "+preset);
                try
                {
                    var rig=root.AddComponent<HumanoidPresentation>();rig.Initialize(preset);var mesh=rig.Skin.sharedMesh;var bones=rig.Skin.bones;
                    var data=new SkinData{preset=preset.ToString(),vertices=mesh.vertices,triangles=mesh.triangles,colors=mesh.colors,weights=mesh.boneWeights,bones=bones.Select(b=>new BoneData{name=b.name,parent=Array.IndexOf(bones,b.parent),position=root.transform.InverseTransformPoint(b.position)}).ToArray()};
                    if(rig.Cane!=null){var cane=rig.Cane.GetComponent<MeshFilter>().sharedMesh;data.caneVertices=cane.vertices.Select(v=>root.transform.InverseTransformPoint(rig.Cane.TransformPoint(v))).ToArray();data.caneTriangles=cane.triangles;}
                    File.WriteAllText(Path.Combine(folder,preset+"-skin.json"),JsonUtility.ToJson(data));
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();
        }
    }
}
