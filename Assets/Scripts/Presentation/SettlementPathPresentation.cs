using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    public sealed class SettlementPathPresentation:MonoBehaviour
    {
        private sealed class Entry
        {public int Buildings;public double X,Z;public GameObject Object;public Mesh Mesh;public SettlementPathNetwork Network;public long Used;}
        private sealed class Prepared
        {public string Id;public int Buildings;public double X,Z;public SettlementPathNetwork Network;public Vector3[] Vertices;public int[] Indices;public Color[] Colors;}
        private readonly Dictionary<string,Entry> networks=new Dictionary<string,Entry>();private Task<Prepared> worker;private CancellationTokenSource cancellation;
        private AevoriaBootstrap app;private Material material;private float next;private long sequence;
        public int NetworkCount=>networks.Count;public int VisiblePaths{get;private set;}
        public void Initialize(AevoriaBootstrap value)
        {app=value;cancellation=new CancellationTokenSource();material=new Material(Resources.Load<Material>("WorldVegetation")){name="Settlement earth paths"};material.SetFloat("_Foliage",0);material.SetFloat("_EcologyAffects",0);}
        public SettlementPathNetwork Network(string settlement)
        {if(!networks.TryGetValue(settlement,out var entry))return null;entry.Used=++sequence;return entry.Network;}
        private void Update()
        {
            if(app?.Society==null)return;
            if(worker!=null&&worker.IsCompleted)
            {
                if(worker.IsFaulted)Debug.LogWarning("Settlement paths unavailable: "+worker.Exception.GetBaseException().Message);
                else if(!worker.IsCanceled)Publish(worker.Result);worker=null;
            }
            var focus=app.Living.Traveler.Exploring?app.Explorer.Position:app.Controller.Focus;bool show=app.Menu.Page==ApplicationPage.World&&(app.Living.Traveler.Exploring||app.Controller.Distance<6000);VisiblePaths=0;
            foreach(var entry in networks.Values){bool active=show&&Distance(focus,entry.X,entry.Z)<2400;entry.Object.SetActive(active);entry.Object.transform.position=new Vector3((float)(entry.X-WorldOrigin.Offset.x),0,(float)(entry.Z-WorldOrigin.Offset.z));if(active)VisiblePaths+=entry.Network.Paths.Count;}
            if(!show||worker!=null||Time.unscaledTime<next)return;next=Time.unscaledTime+.4f;
            SettlementState nearest=null;double best=1800;
            foreach(var settlement in app.Society.Settlements)
            {double distance=Distance(focus,settlement.X,settlement.Z);if(distance>=best||networks.TryGetValue(settlement.Id,out var known)&&known.Buildings==settlement.Buildings.Count)continue;best=distance;nearest=settlement;}
            if(nearest==null)return;var snapshot=nearest.Copy();var geo=app.Terrain.Geography;var token=cancellation.Token;worker=Task.Run(()=>Prepare(geo,snapshot,token),token);
        }
        private static double Distance(Vector3 point,double x,double z)=>Math.Sqrt((point.x-x)*(point.x-x)+(point.z-z)*(point.z-z));
        private static Prepared Prepare(ProceduralGeography geo,SettlementState settlement,CancellationToken token)
        {
            var network=SettlementPaths.Build(geo,settlement,token);var vertices=new List<Vector3>();var indices=new List<int>();var colors=new List<Color>();
            foreach(var path in network.Paths)
            {
                var samples=new List<GeoPoint>{path.Points[0]};
                for(int segment=1;segment<path.Points.Count;segment++)
                {
                    var a=path.Points[segment-1];var b=path.Points[segment];double distance=SettlementPaths.Distance(a,b);if(distance<.01)continue;int pieces=Math.Max(1,(int)Math.Ceiling(distance/1.5));
                    for(int piece=1;piece<=pieces;piece++){double t=piece/(double)pieces;samples.Add(new GeoPoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t));}
                }
                int first=vertices.Count;
                for(int i=0;i<samples.Count;i++)
                {
                    token.ThrowIfCancellationRequested();var p=samples[i];var before=samples[Math.Max(0,i-1)];var after=samples[Math.Min(samples.Count-1,i+1)];
                    double length=SettlementPaths.Distance(before,after);if(length<1e-6)length=1;
                    double dx=(after.X-before.X)/length*.85,dz=(after.Y-before.Y)/length*.85;
                    void Vertex(double x,double z){vertices.Add(new Vector3((float)(x-settlement.X),(float)LocalSolidField.RenderedGround(geo,x,z)+.045f,(float)(z-settlement.Z)));colors.Add(new Color(.29f,.23f,.145f));}
                    // Adjacent pieces use the same cross-section, closing the
                    // gaps between the old independently oriented road quads.
                    Vertex(p.X-dz,p.Y+dx);Vertex(p.X+dz,p.Y-dx);
                    if(i==0)continue;int a=first+(i-1)*2,b=first+i*2;
                    indices.Add(a);indices.Add(b);indices.Add(a+1);indices.Add(a+1);indices.Add(b);indices.Add(b+1);
                }
            }
            return new Prepared{Id=settlement.Id,Buildings=settlement.Buildings.Count,X=settlement.X,Z=settlement.Z,Network=network,Vertices=vertices.ToArray(),Indices=indices.ToArray(),Colors=colors.ToArray()};
        }
        private void Publish(Prepared data)
        {
            if(networks.TryGetValue(data.Id,out var old)){Destroy(old.Object);Destroy(old.Mesh);networks.Remove(data.Id);}
            while(networks.Count>=4)
            {string oldest=null;long used=long.MaxValue;foreach(var pair in networks)if(pair.Value.Used<used){used=pair.Value.Used;oldest=pair.Key;}var entry=networks[oldest];Destroy(entry.Object);Destroy(entry.Mesh);networks.Remove(oldest);}
            var mesh=new Mesh{name="Connected settlement paths",indexFormat=IndexFormat.UInt32};mesh.vertices=data.Vertices;mesh.triangles=data.Indices;mesh.colors=data.Colors;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.UploadMeshData(true);
            var obj=new GameObject("Paths · "+data.Id);obj.transform.SetParent(transform,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            networks.Add(data.Id,new Entry{Buildings=data.Buildings,X=data.X,Z=data.Z,Object=obj,Mesh=mesh,Network=data.Network,Used=++sequence});
        }
        private void OnDestroy()
        {cancellation?.Cancel();cancellation?.Dispose();if(worker!=null)worker.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);foreach(var entry in networks.Values){if(entry.Object!=null)Destroy(entry.Object);if(entry.Mesh!=null)Destroy(entry.Mesh);}if(material!=null)Destroy(material);}
    }
}
