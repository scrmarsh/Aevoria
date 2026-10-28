using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    // One geological system around the traveler, never the entire planet.
    // Render-only 1 m surfaces continue beyond the 0.5 m collision stream.
    // Edited cubes retain the exact 0.5 m field so the shell cannot seal cuts.
    public sealed class DistantCavePresentation:MonoBehaviour
    {
        private sealed class Request{public int X,Y,Z;public VolumetricSolidField Field;public bool Edited;public SolidGeometry Geometry;}
        private AevoriaBootstrap app;private CancellationTokenSource cancellation;private Task<List<Request>> worker;private List<Request> prepared;private int cursor,revision=-1,cellX=int.MinValue,cellZ;
        private readonly List<GameObject> active=new List<GameObject>(),staging=new List<GameObject>();private readonly List<Mesh> activeMeshes=new List<Mesh>(),stagingMeshes=new List<Mesh>();private Material material;private bool wanted;
        public bool Ready=>!wanted||worker==null&&prepared==null;public int Chunks=>active.Count;
        public void Initialize(AevoriaBootstrap application){app=application;cancellation=new CancellationTokenSource();material=new Material(Resources.Load<Material>("WorldTerrain"));material.SetFloat("_EditablePatch",1);}
        private void Update()
        {
            if(app?.Living==null)return;var s=app.Living.Traveler;var geo=app.Terrain.Geography;int x=(int)Math.Floor(s.X/CaveNetwork.CellSize),z=(int)Math.Floor(s.Z/CaveNetwork.CellSize);var system=geo.Caves.SystemAt(x,z);
            double depth=LocalSolidField.RenderedGround(geo,s.X,s.Z)-s.Y;wanted=s.Exploring&&(depth>1||system.Active&&Math.Pow(s.X-system.Entrance.X,2)+Math.Pow(s.Z-system.Entrance.Z,2)<6400);
            if(!wanted){foreach(var obj in active)if(obj!=null)obj.SetActive(false);return;}foreach(var obj in active)if(obj!=null)obj.SetActive(true);
            if(worker!=null&&worker.IsCompleted){if(worker.IsFaulted)Debug.LogException(worker.Exception);else if(!worker.IsCanceled){prepared=worker.Result;cursor=0;}worker=null;}
            if(prepared!=null)
            {
                if(cursor<prepared.Count){Upload(prepared[cursor++]);return;}
                Release(active,activeMeshes);active.AddRange(staging);activeMeshes.AddRange(stagingMeshes);staging.Clear();stagingMeshes.Clear();foreach(var obj in active)obj.SetActive(true);prepared=null;
            }
            if(worker!=null||revision==app.Living.ExcavationRevision&&x==cellX&&z==cellZ)return;revision=app.Living.ExcavationRevision;cellX=x;cellZ=z;
            var keys=new HashSet<(int X,int Y,int Z)>();
            void Bounds(double ax,double ay,double az,double bx,double by,double bz){for(int k=(int)Math.Floor(az/32);k<=(int)Math.Floor(bz/32);k++)for(int j=(int)Math.Floor(ay/32);j<=(int)Math.Floor(by/32);j++)for(int i=(int)Math.Floor(ax/32);i<=(int)Math.Floor(bx/32);i++)keys.Add((i,j,k));}
            foreach(var p in system.Passages){double r=p.Radius+1,ry=r/p.VerticalScale;Bounds(Math.Min(p.A.X,p.B.X)-r,Math.Min(p.A.Y,p.B.Y)-ry,Math.Min(p.A.Z,p.B.Z)-r,Math.Max(p.A.X,p.B.X)+r,Math.Max(p.A.Y,p.B.Y)+ry,Math.Max(p.A.Z,p.B.Z)+r);}
            foreach(var p in system.Chambers)Bounds(p.Centre.X-p.Radii.X-1,p.Centre.Y-p.Radii.Y-1,p.Centre.Z-p.Radii.Z-1,p.Centre.X+p.Radii.X+1,p.Centre.Y+p.Radii.Y+1,p.Centre.Z+p.Radii.Z+1);
            foreach(var site in app.Living.Excavations)foreach(var cut in site.Cuts)if(Math.Abs(cut.X-s.X)<768&&Math.Abs(cut.Z-s.Z)<768)Bounds(cut.X-cut.Radius,cut.Y-cut.Radius,cut.Z-cut.Radius,cut.X+cut.Radius,cut.Y+cut.Radius,cut.Z+cut.Radius);
            var requests=new List<Request>();foreach(var key in keys)
            {
                var edits=new List<DigCut>();var unique=new HashSet<DigCut>();for(int dz=0;dz<2;dz++)for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)foreach(var cut in app.Living.EditsForChunk(key.X*2+dx,key.Y*2+dy,key.Z*2+dz))if(unique.Add(cut))edits.Add(cut);
                requests.Add(new Request{X=key.X,Y=key.Y,Z=key.Z,Edited=edits.Count>0,Field=new VolumetricSolidField(geo,geo.Caves,edits)});
            }
            requests.Sort((a,b)=>{int c=a.Z.CompareTo(b.Z);if(c!=0)return c;c=a.Y.CompareTo(b.Y);return c!=0?c:a.X.CompareTo(b.X);});var token=cancellation.Token;
            worker=Task.Run(()=>{Parallel.ForEach(requests,new ParallelOptions{MaxDegreeOfParallelism=2,CancellationToken=token},r=>r.Geometry=r.Field.BuildResolution(r.X,r.Y,r.Z,32,r.Edited?64:32,token));requests.RemoveAll(r=>r.Geometry.Indices.Count==0);return requests;},token);
        }
        private void Upload(Request request)
        {
            var data=request.Geometry;var vertices=new List<Vector3>();var normals=new List<Vector3>();var depth=new List<Vector2>();float inset=request.Edited?.025f:.18f;
            for(int i=0;i<data.Vertices.Count;i++){var p=data.Vertices[i];var n=data.Normals[i];vertices.Add(new Vector3((float)(p.X-request.X*32.0-n.X*inset),(float)(p.Y-request.Y*32.0-n.Y*inset),(float)(p.Z-request.Z*32.0-n.Z*inset)));normals.Add(new Vector3((float)n.X,(float)n.Y,(float)n.Z));depth.Add(new Vector2(data.SurfaceDepth[i],0));}
            var triangles=new List<int>();for(int i=0;i<data.Indices.Count;i+=3){int a=data.Indices[i],b=data.Indices[i+1],c=data.Indices[i+2];if(data.SurfaceDepth[a]<.2||data.SurfaceDepth[b]<.2||data.SurfaceDepth[c]<.2)continue;triangles.Add(a);triangles.Add(b);triangles.Add(c);}if(triangles.Count==0)return;
            var mesh=new Mesh{name="Distant cave geological shell",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(2,depth);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();var obj=new GameObject("Distant cave "+request.X+","+request.Y+","+request.Z);obj.transform.SetParent(transform,false);obj.transform.position=WorldOrigin.Relative(new Vector3(request.X*32,request.Y*32,request.Z*32));obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;obj.SetActive(false);staging.Add(obj);stagingMeshes.Add(mesh);
        }
        private static void Release(List<GameObject> objects,List<Mesh> meshes){foreach(var obj in objects)if(obj!=null)Destroy(obj);foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);objects.Clear();meshes.Clear();}
        private void OnDestroy(){cancellation?.Cancel();if(worker!=null)worker.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);cancellation?.Dispose();Release(active,activeMeshes);Release(staging,stagingMeshes);if(material!=null)Destroy(material);}
    }
}
