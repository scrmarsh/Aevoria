using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    public sealed class WorldWaterFeatures : MonoBehaviour
    {
        private readonly List<Mesh> owned=new List<Mesh>();
        private Material material,riverMaterial;
        private Task<InlandWaterLayout> pending;
        private CancellationTokenSource cancellation;
        private ProceduralGeography geography;
        public InlandWaterLayout Layout {get;private set;}
        public int Waterfalls {get;private set;}
        public bool IsReady=>pending==null;
        public void Initialize(ProceduralGeography geo)
        {
            geography=geo;
            if(cancellation!=null){cancellation.Cancel();cancellation.Dispose();}cancellation=new CancellationTokenSource();
            Layout=null;Waterfalls=0;
            if(material==null){material=new Material(Resources.Load<Material>("WorldWater"));material.SetFloat("_InlandWater",1);}
            if(riverMaterial==null)
            {
                riverMaterial=new Material(material){name="Single coverage river water"};
                riverMaterial.SetInt("_WaterStencilComp",(int)CompareFunction.NotEqual);
                riverMaterial.SetInt("_WaterStencilPass",(int)StencilOp.Replace);
                riverMaterial.SetInt("_WaterStencilWriteMask",8);
            }
            foreach(var mesh in owned)Destroy(mesh);owned.Clear();
            for(int i=transform.childCount-1;i>=0;i--){var child=transform.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
            if(pending!=null)Observe(pending);
            pending=Task.FromResult(geo.InlandSurfaces??throw new System.InvalidOperationException("Canonical water surfaces must finish generation before presentation."));
        }
        private static void Observe(Task task)=>task.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
        private void Update()
        {
            if(pending==null||!pending.IsCompleted)return;
            if(pending.IsFaulted)Debug.LogException(pending.Exception);
            else if(!pending.IsCanceled){Layout=pending.Result;Waterfalls=Layout.Waterfalls;Install("Lakes",Layout.Lakes,0);Install("Rivers",Layout.Rivers,1);Install("Waterfalls",Layout.Falls,1);}
            pending=null;
        }
        private void Install(string name,WaterGeometry data,float river)
        {
            if(data.Indices.Count==0)return;
            // Bounded spatial batches let the renderer cull remote basins and
            // channel triangles instead of submitting a planet-wide water mesh.
            var groups=new Dictionary<int,List<int>>();var c=geography.Configuration;
            for(int i=0;i<data.Indices.Count;i+=3)
            {var a=data.Vertices[data.Indices[i]];var b=data.Vertices[data.Indices[i+1]];var d=data.Vertices[data.Indices[i+2]];int x=Mathf.Clamp((int)(((a.X+b.X+d.X)/3/c.WidthMeters+.5)*16),0,15),z=Mathf.Clamp((int)(((a.Z+b.Z+d.Z)/3/c.DepthMeters+.5)*16),0,15),key=z*16+x;if(!groups.TryGetValue(key,out var list)){list=new List<int>();groups.Add(key,list);}list.Add(data.Indices[i]);list.Add(data.Indices[i+1]);list.Add(data.Indices[i+2]);}
            foreach(var group in groups)
            {
                var source=group.Value;var vertices=new Vector3[source.Count];var metadata=new Vector4[source.Count];var direction=new Vector2[source.Count];var indices=new int[source.Count];
                for(int i=0;i<source.Count;i++){var p=data.Vertices[source[i]];vertices[i]=new Vector3((float)p.X,p.Level,(float)p.Z);metadata[i]=new Vector4(p.Depth,p.Energy,river,p.ChannelOwner+1);direction[i]=new Vector2(p.FlowX,p.FlowZ);indices[i]=i;}
                var mesh=new Mesh{name="Inland water "+name+" "+group.Key,indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.SetUVs(0,metadata);mesh.SetUVs(1,direction);mesh.triangles=indices;mesh.RecalculateBounds();mesh.UploadMeshData(true);owned.Add(mesh);
                var obj=new GameObject(name+" "+group.Key);obj.transform.SetParent(transform,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=name=="Rivers"?riverMaterial:material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        private void OnDestroy(){if(cancellation!=null){cancellation.Cancel();cancellation.Dispose();}if(pending!=null)Observe(pending);foreach(var mesh in owned)if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);if(riverMaterial!=null)Destroy(riverMaterial);}
    }
}
