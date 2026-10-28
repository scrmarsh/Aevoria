using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Jobs;
namespace Aevoria.Presentation
{
    public sealed class ExcavationPresentation : MonoBehaviour
    {
        private readonly struct Key:IEquatable<Key>
        {
            public readonly int X,Y,Z;public Key(int x,int y,int z){X=x;Y=y;Z=z;}
            public bool Equals(Key k)=>X==k.X&&Y==k.Y&&Z==k.Z;
            public override bool Equals(object value)=>value is Key k&&Equals(k);
            public override int GetHashCode()=>unchecked((X*397^Y)*397^Z);
        }
        private sealed class Patch{public GameObject Object;public Mesh Mesh;public int Vertices,Triangles;}
        private sealed class Request{public Key Key;public int Version;public VolumetricSolidField Field;public SolidGeometry Geometry;public bool Owns;public Mesh Mesh;public JobHandle Bake;public bool Baked;}
        private sealed class Batch{public List<Request> Requests;public double WorkerMilliseconds;public int Staged;public long BakeStarted;}
        private readonly Dictionary<Key,Patch> active=new Dictionary<Key,Patch>();
        private readonly Dictionary<Key,int> known=new Dictionary<Key,int>(),versions=new Dictionary<Key,int>();
        private readonly Dictionary<Key,VolumetricSolidField> fields=new Dictionary<Key,VolumetricSolidField>();
        private readonly HashSet<Key> desired=new HashSet<Key>();private readonly List<Key> remove=new List<Key>();
        private DistantCavePresentation horizon;private AevoriaBootstrap app;private LivingWorldState living;private Material material;private CancellationTokenSource cancellation;
        private Task<Batch> worker;private Batch staged;private Key centre;private bool hasCentre,lastSkip;
        private Texture3D coverage;private readonly byte[] mask=new byte[343];private static ExcavationPresentation maskOwner;
        private const int CoverageSide=7,Radius=2;
        private const MeshColliderCookingOptions Cooking=MeshColliderCookingOptions.CookForFasterSimulation|MeshColliderCookingOptions.EnableMeshCleaning|MeshColliderCookingOptions.WeldColocatedVertices|MeshColliderCookingOptions.UseFastMidphase;
        private struct BakeJob:IJob{public int MeshId;public void Execute()=>Physics.BakeMesh(MeshId,false,Cooking);}
        public int InstalledRevision{get;private set;}
        public int InstalledVertices{get;private set;}public int InstalledTriangles{get;private set;}
        public int ResidentChunks=>active.Count;
        public double LastColliderMilliseconds{get;private set;}public double LastInstallMilliseconds{get;private set;}
        public double LastPreparationMilliseconds{get;private set;}public double LastBakeWaitMilliseconds{get;private set;}
        public bool IsBaking=>staged!=null;
        public bool Ready{get{if(worker!=null||staged!=null||horizon!=null&&!horizon.Ready)return false;foreach(var key in desired)if(!known.TryGetValue(key,out int done)||done<Version(key))return false;return true;}}
        public event Action GroundOwnershipChanged;
        private int Version(Key key)=>versions.TryGetValue(key,out int value)?value:0;
        public void Initialize(AevoriaBootstrap value)
        {
            if(living!=null)living.Excavated-=Edited;cancellation?.Cancel();cancellation?.Dispose();if(worker!=null)Observe(worker);worker=null;ReleaseStaged();
            foreach(var patch in active.Values)Release(patch);active.Clear();known.Clear();fields.Clear();versions.Clear();desired.Clear();hasCentre=false;InstalledRevision=InstalledVertices=InstalledTriangles=0;
            app=value;if(horizon==null){horizon=gameObject.AddComponent<DistantCavePresentation>();horizon.Initialize(app);}living=app.Living;living.Excavated+=Edited;cancellation=new CancellationTokenSource();
            if(material==null){material=new Material(Resources.Load<Material>("WorldTerrain"));material.SetFloat("_EditablePatch",1);}
            if(coverage==null)coverage=new Texture3D(CoverageSide,CoverageSide,CoverageSide,TextureFormat.R8,false){name="Installed solid volume ownership",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            PublishMask();
        }
        private static void Observe(Task task)=>task.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
        public bool OwnsCollider(Collider collider)
        {
            if(!(collider is MeshCollider mesh))return false;var p=WorldOrigin.Geographic(collider.transform);var key=new Key(Mathf.RoundToInt(p.x/16),Mathf.RoundToInt(p.y/16),Mathf.RoundToInt(p.z/16));return active.TryGetValue(key,out var patch)&&patch.Object==collider.gameObject&&patch.Mesh!=null&&mesh.sharedMesh==patch.Mesh;
        }
        public bool OwnsChunk(int x,int y,int z)=>active.ContainsKey(new Key(x,y,z));
        public bool OwnsPoint(double x,double y,double z)=>OwnsChunk((int)Math.Floor(x/16),(int)Math.Floor(y/16),(int)Math.Floor(z/16));
        public bool OwnsGround(double x,double z)=>app?.Terrain?.Geography!=null&&OwnsPoint(x,LocalSolidField.RenderedGround(app.Terrain.Geography,x,z),z);
        public void RequestNearTraveler()=>RefreshDesired();
        public bool TryInstalledDensity(double x,double y,double z,out double density)
        {if(fields.TryGetValue(new Key((int)Math.Floor(x/16),(int)Math.Floor(y/16),(int)Math.Floor(z/16)),out var field)){density=field.Density(x,y,z);return true;}density=0;return false;}
        private bool InCoverage(Key k)=>hasCentre&&Math.Abs(k.X-centre.X)<=3&&Math.Abs(k.Y-centre.Y)<=3&&Math.Abs(k.Z-centre.Z)<=3;
        private void Edited(DigCut cut)
        {
            double r=cut.Radius+.2;
            for(int z=(int)Math.Floor((cut.Z-r)/16);z<=(int)Math.Floor((cut.Z+r)/16);z++)for(int y=(int)Math.Floor((cut.Y-r)/16);y<=(int)Math.Floor((cut.Y+r)/16);y++)for(int x=(int)Math.Floor((cut.X-r)/16);x<=(int)Math.Floor((cut.X+r)/16);x++)
            {var key=new Key(x,y,z);if(InCoverage(key)||active.ContainsKey(key)){versions[key]=living.ExcavationRevision;desired.Add(key);}}
        }
        private void RefreshDesired()
        {
            var state=living.Traveler;bool exploring=state.Exploring;var focus=app.Controller.Focus;
            double x=exploring?state.X:focus.x,y=exploring?state.Y:focus.y,z=exploring?state.Z:focus.z;
            bool skip=!exploring&&app.Controller.Distance>1500||exploring&&state.Flying&&y-LocalSolidField.RenderedGround(app.Terrain.Geography,x,z)>96;
            var next=new Key((int)Math.Floor(x/16),(int)Math.Floor(y/16),(int)Math.Floor(z/16));if(hasCentre&&next.Equals(centre)&&lastSkip==skip)return;
            centre=next;hasCentre=true;lastSkip=skip;desired.Clear();
            if(!skip)for(int dz=-Radius;dz<=Radius;dz++)for(int dy=-Radius;dy<=Radius;dy++)for(int dx=-Radius;dx<=Radius;dx++)desired.Add(new Key(centre.X+dx,centre.Y+dy,centre.Z+dz));
            remove.Clear();foreach(var pair in active)if(skip||!InCoverage(pair.Key))remove.Add(pair.Key);
            bool changed=remove.Count>0;foreach(var key in remove){Release(active[key]);active.Remove(key);}
            remove.Clear();foreach(var key in known.Keys)if(skip||!InCoverage(key))remove.Add(key);foreach(var key in remove){known.Remove(key);fields.Remove(key);versions.Remove(key);}
            PublishMask();if(changed)OwnershipChanged();
        }
        private void Update()
        {
            if(living==null||app?.Terrain?.Geography==null)return;RefreshDesired();
            if(worker!=null&&worker.IsCompleted)
            {
                if(worker.IsFaulted)Debug.LogException(worker.Exception);else if(!worker.IsCanceled){staged=worker.Result;LastPreparationMilliseconds=staged.WorkerMilliseconds;}
                worker=null;
            }
            if(staged!=null)
            {
                // Stage one native mesh per rendered frame. All replacements
                // remain invisible until every collider in the batch is ready.
                while(staged.Staged<staged.Requests.Count)
                {
                    var request=staged.Requests[staged.Staged++];if(!InCoverage(request.Key)||lastSkip||!request.Owns||request.Geometry.Indices.Count==0)continue;
                    using var work=Aevoria.Core.WorkCounter.Upload.Measure(Time.frameCount);PrepareMesh(request);if(staged.BakeStarted==0)staged.BakeStarted=System.Diagnostics.Stopwatch.GetTimestamp();break;
                }
                if(staged.Staged<staged.Requests.Count)return;
                foreach(var request in staged.Requests)if(request.Baked&&!request.Bake.IsCompleted)return;
                Commit();
            }
            if(worker!=null||staged!=null||lastSkip)return;
            var requests=new List<Request>();foreach(var key in desired)if(!known.TryGetValue(key,out int done)||done<Version(key))requests.Add(new Request{Key=key,Version=Version(key),Field=new VolumetricSolidField(app.Terrain.Geography,app.Terrain.Geography.Caves,living.EditsForChunk(key.X,key.Y,key.Z))});
            if(requests.Count==0)return;var token=cancellation.Token;
            // Deterministic installation order also keeps nearest support first
            // in preparation/debug evidence; a batch still publishes atomically.
            requests.Sort((a,b)=>{int da=Math.Abs(a.Key.X-centre.X)+Math.Abs(a.Key.Y-centre.Y)+Math.Abs(a.Key.Z-centre.Z),db=Math.Abs(b.Key.X-centre.X)+Math.Abs(b.Key.Y-centre.Y)+Math.Abs(b.Key.Z-centre.Z);int cmp=da.CompareTo(db);if(cmp!=0)return cmp;cmp=a.Key.Z.CompareTo(b.Key.Z);if(cmp!=0)return cmp;cmp=a.Key.Y.CompareTo(b.Key.Y);return cmp!=0?cmp:a.Key.X.CompareTo(b.Key.X);});
            worker=Task.Run(()=>{var timer=System.Diagnostics.Stopwatch.StartNew();Parallel.ForEach(requests,new ParallelOptions{MaxDegreeOfParallelism=2,CancellationToken=token},r=>{token.ThrowIfCancellationRequested();r.Owns=r.Field.MayContainSurface(r.Key.X,r.Key.Y,r.Key.Z);if(r.Owns)r.Geometry=r.Field.Build(r.Key.X,r.Key.Y,r.Key.Z,token);});return new Batch{Requests=requests,WorkerMilliseconds=timer.Elapsed.TotalMilliseconds};},token);
        }
        private void PrepareMesh(Request request)
        {
            var data=request.Geometry;var vertices=new List<Vector3>(data.Vertices.Count);var normals=new List<Vector3>(data.Normals.Count);var depth=new List<Vector2>(data.SurfaceDepth.Count);var key=request.Key;
            foreach(var p in data.Vertices)vertices.Add(new Vector3((float)(p.X-key.X*16.0),(float)(p.Y-key.Y*16.0),(float)(p.Z-key.Z*16.0)));foreach(var p in data.Normals)normals.Add(new Vector3((float)p.X,(float)p.Y,(float)p.Z));foreach(float d in data.SurfaceDepth)depth.Add(new Vector2(d,0));
            var mesh=request.Mesh=new Mesh{name="Streamed solid volume",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(2,depth);mesh.SetTriangles(data.Indices,0);mesh.RecalculateBounds();
            request.Bake=new BakeJob{MeshId=mesh.GetInstanceID()}.Schedule();request.Baked=true;JobHandle.ScheduleBatchedJobs();
        }
        private void Commit()
        {
            using var work=Aevoria.Core.WorkCounter.Upload.Measure(Time.frameCount);long began=System.Diagnostics.Stopwatch.GetTimestamp();LastColliderMilliseconds=0;bool changed=false;var oldMeshes=new List<Mesh>();
            foreach(var request in staged.Requests)
            {
                if(request.Baked)request.Bake.Complete();var key=request.Key;if(!InCoverage(key)||lastSkip){if(request.Mesh!=null)Destroy(request.Mesh);continue;}
                known[key]=request.Version;fields[key]=request.Field;
                if(!request.Owns){if(active.TryGetValue(key,out var previous)){Release(previous);active.Remove(key);changed=true;}continue;}
                if(!active.TryGetValue(key,out var patch)){patch=new Patch();active.Add(key,patch);}if(patch.Mesh!=null)oldMeshes.Add(patch.Mesh);
                patch.Mesh=request.Mesh;patch.Vertices=request.Geometry.Vertices.Count;patch.Triangles=request.Geometry.Indices.Count/3;
                if(patch.Object==null&&request.Mesh!=null){patch.Object=new GameObject("Solid volume "+key.X+","+key.Y+","+key.Z);patch.Object.transform.SetParent(transform,false);patch.Object.AddComponent<MeshCollider>().cookingOptions=Cooking;patch.Object.AddComponent<MeshFilter>();patch.Object.AddComponent<MeshRenderer>().sharedMaterial=material;}
                if(patch.Object!=null)
                {
                    patch.Object.transform.position=WorldOrigin.Relative(new Vector3(key.X*16,key.Y*16,key.Z*16));long colliderAt=System.Diagnostics.Stopwatch.GetTimestamp();patch.Object.GetComponent<MeshCollider>().sharedMesh=request.Mesh;LastColliderMilliseconds+=(System.Diagnostics.Stopwatch.GetTimestamp()-colliderAt)*1000.0/System.Diagnostics.Stopwatch.Frequency;patch.Object.GetComponent<MeshFilter>().sharedMesh=request.Mesh;patch.Object.SetActive(request.Mesh!=null);
                }
                changed=true;
            }
            LastBakeWaitMilliseconds=staged.BakeStarted==0?0:(System.Diagnostics.Stopwatch.GetTimestamp()-staged.BakeStarted)*1000.0/System.Diagnostics.Stopwatch.Frequency;staged=null;
            if(changed){PublishMask();OwnershipChanged();}Physics.SyncTransforms();foreach(var mesh in oldMeshes)Destroy(mesh);LastInstallMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-began)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        }
        private void OwnershipChanged(){InstalledRevision++;InstalledVertices=InstalledTriangles=0;foreach(var p in active.Values){InstalledVertices+=p.Vertices;InstalledTriangles+=p.Triangles;}GroundOwnershipChanged?.Invoke();}
        private void PublishMask()
        {
            if(coverage==null)return;Array.Clear(mask,0,mask.Length);foreach(var key in active.Keys){int x=key.X-centre.X+3,y=key.Y-centre.Y+3,z=key.Z-centre.Z+3;if(x>=0&&y>=0&&z>=0&&x<7&&y<7&&z<7)mask[(z*7+y)*7+x]=255;}
            coverage.SetPixelData(mask,0);coverage.Apply(false);Shader.SetGlobalTexture("_AevoriaSolidCoverage",coverage);Shader.SetGlobalVector("_AevoriaSolidGrid",new Vector4((centre.X-3)*16,(centre.Y-3)*16,(centre.Z-3)*16,16));Shader.SetGlobalFloat("_AevoriaSolidCells",7);Shader.SetGlobalFloat("_AevoriaSolidReady",1);Shader.SetGlobalFloat("_AevoriaDigCount",0);maskOwner=this;
        }
        private static void Release(Patch patch){if(patch.Object!=null){patch.Object.SetActive(false);Destroy(patch.Object);}if(patch.Mesh!=null)Destroy(patch.Mesh);}
        private void ReleaseStaged(){if(staged==null)return;foreach(var r in staged.Requests){if(r.Baked)r.Bake.Complete();if(r.Mesh!=null)Destroy(r.Mesh);}staged=null;}
        private void OnDestroy(){if(horizon!=null)Destroy(horizon);if(living!=null)living.Excavated-=Edited;cancellation?.Cancel();cancellation?.Dispose();if(worker!=null)Observe(worker);ReleaseStaged();foreach(var p in active.Values)Release(p);if(material!=null)Destroy(material);if(coverage!=null)Destroy(coverage);if(maskOwner==this){Shader.SetGlobalFloat("_AevoriaSolidReady",0);maskOwner=null;}}
    }
}
