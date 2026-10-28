using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    internal static class AuthoredEquipment012
    {
        [Serializable]private sealed class Vertex{public Vector3 p,n;public Vector2 uv;public Color c;}
        [Serializable]private sealed class Asset{public int version;public string name;public Vertex[] vertices;public int[] triangles;}
        private static readonly string[] names={"","Shovel012","Pickaxe012","Axe012","Stick012","Crate012"};
        public static Mesh LoadTool(int slot){if(slot<1||slot>=names.Length)throw new ArgumentOutOfRangeException(nameof(slot));return Load(names[slot]);}
        public static Mesh Load(string name)
        {
            var text=Resources.Load<TextAsset>("Equipment/"+name);if(text==null)throw new InvalidOperationException("Authored equipment is missing: "+name);
            var data=JsonUtility.FromJson<Asset>(text.text);if(data.version!=12||data.vertices.Length<50||data.triangles.Length%3!=0)throw new InvalidOperationException("Invalid authored equipment: "+name);
            var points=new Vector3[data.vertices.Length];var normals=new Vector3[points.Length];var uv=new Vector2[points.Length];var colors=new Color[points.Length];for(int i=0;i<points.Length;i++){var v=data.vertices[i];points[i]=v.p;normals[i]=v.n;uv[i]=v.uv;colors[i]=v.c;}
            var mesh=new Mesh{name=name+" authored equipment",indexFormat=points.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.vertices=points;mesh.normals=normals;mesh.uv=uv;mesh.uv2=new Vector2[points.Length];mesh.uv3=new Vector2[points.Length];mesh.colors=colors;mesh.triangles=data.triangles;mesh.RecalculateBounds();return mesh;
        }
    }
}
