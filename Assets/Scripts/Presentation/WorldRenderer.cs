using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aevoria.World;
using Aevoria.Core;
using Aevoria.Persistence;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Debug=UnityEngine.Debug;
using Unity.Profiling;

namespace Aevoria.Presentation
{
    public sealed class WorldRenderer : MonoBehaviour
    {
        private static readonly ProfilerMarker UploadMarker=new ProfilerMarker("Aevoria.Upload");
        public const int OverviewResolution=768;
        public const int OverviewTriangleCount=OverviewResolution*OverviewResolution*2;
        private sealed class Tile
        {
            public GameObject Object;
            public MeshFilter Filter,OutgoingFilter;
            public MeshRenderer Renderer,OutgoingRenderer;
            public Mesh Mesh,Outgoing;
            public int Lod;
            public Payload Surface,PreviousSurface;
            public float Fade;
            public readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();
        }
        private sealed class Payload
        {
            public VegetationChunk Vegetation;
            public Vector3[] Vertices,Normals;
            public Color[] Colors=Array.Empty<Color>();
            public Vector4[] Banks;
            public int[] Indices;
            public double Milliseconds;
        }
        private sealed class Job
        {
            public ChunkKey Key;
            public int Lod,Epoch;
            public Task<Payload> Task;
        }
        private struct Candidate { public ChunkKey Key; public int Lod; public float Score; }
        private sealed class Overview
        {
            public Payload Terrain;
            public float[] Heights;
        }
        private readonly Dictionary<ChunkKey,Tile> active=new Dictionary<ChunkKey,Tile>();
        private readonly Dictionary<ChunkKey,int> desired=new Dictionary<ChunkKey,int>();
        private readonly List<Candidate> candidates=new List<Candidate>();
        private readonly List<ChunkKey> remove=new List<ChunkKey>();
        private readonly Stack<Tile> pool=new Stack<Tile>();
        private readonly List<Job> jobs=new List<Job>();
        private static readonly ConcurrentDictionary<int,int[]> topology=new ConcurrentDictionary<int,int[]>();
        private readonly Plane[] planes=new Plane[6];
        private CancellationTokenSource cancellation;
        private Task<Overview> overviewJob;
        private InlandWaterLayout bankSource;
        private int epoch;
        private float refreshAt;
        private Vector3 lastPosition;
        private Material terrainMaterial,waterMaterial;
        private Camera view;
        private GameObject ocean,overviewObject;
        private Mesh oceanMesh,overviewMesh;
        private Texture2D overviewHeightField,overviewNormalField;
        private Texture2D coverage,heightTexture;
        private float[] overviewHeights;
        private const int CoverageResolution=64;private int coverageX=-32,coverageZ=-32;
        private Color[] coveragePixels;
        private bool coverageDirty;
        private WorldVegetation vegetation;public RegionalTerrain Regional {get;private set;}
        public int ViewRadius {get;set;}=6;public int DetailQuality {get;set;}=1;public int MountainQuality {get;set;}=1;
        public int StreamingAggressiveness {get;set;}=1;public int BackgroundBudget {get;set;}=2;
        private TerrainPresentationCache diskCache;
        public int CacheHits=>diskCache?.Hits??0;public int CacheMisses=>diskCache?.Misses??0;
        public void SetCacheMemoryBudget(int mebibytes)=>diskCache?.SetMemoryBudget(mebibytes);
        public bool Preloading {get;private set;}public string PreloadStatus {get;private set;}="";
        private Vector3 preloadPosition;private Quaternion preloadRotation;private int preloadTarget,preloadRegionalVersion;
        public float PreloadProgress {get;private set;}
        public void PreloadVisibleArea()
        {
            if(Geography==null||view==null)return;
            RefreshDesired(Vector3.zero);Preloading=true;preloadPosition=WorldOrigin.Geographic(view.transform);preloadRotation=view.transform.rotation;preloadTarget=desired.Count+1;PreloadProgress=0;PreloadStatus="Preparing visible terrain and vegetation";
            preloadRegionalVersion=Regional.RequestPreload();
        }
        public void CancelPreload(){if(!Preloading)return;Preloading=false;Regional?.CancelPreload();PreloadStatus="Preload cancelled; installed terrain remains available";refreshAt=0;}
        private readonly Stopwatch generation=new Stopwatch();
        public ProceduralGeography Geography { get; private set; }
        public int ActiveChunks=>active.Count;
        public string LodSummary {get{int a=0,b=0,c=0;foreach(var tile in active.Values){if(tile.Lod==0)a++;else if(tile.Lod==1)b++;else c++;}return $"LOD0 {a} · LOD1 {b} · LOD2 {c}";}}
        public int BaseMeshes=>(oceanMesh==null?0:1)+(overviewMesh==null?0:1)+(Regional?.OwnedMeshes??0);
        public int DecorativeMeshes=>GetComponentInChildren<DistantMountainHorizon>()==null?0:3;
        public int OwnedMeshes { get { int count=BaseMeshes+DecorativeMeshes+(Regional?.StagingMeshes??0); foreach(var tile in active.Values) count+=1+(tile.Outgoing==null?0:1); return count; } }
        public long Triangles { get; private set; }
        public long ResidentTriangles { get {long total=Triangles+DecorativeMeshes*1024L+(Regional?.Triangles??0)+(overviewMesh==null?0:OverviewTriangleCount)+(oceanMesh==null?0:2);foreach(var tile in active.Values) if(tile.Outgoing!=null) total+=(long)tile.Outgoing.GetIndexCount(0)/3;return total;} }
        public double MeshMilliseconds { get; private set; }
        public double ReadyMilliseconds { get; private set; }
        public double LastUpdateMilliseconds { get; private set; }
        public bool IsReady=>overviewMesh!=null && Regional!=null&&Regional.Ready&& jobs.Count==0 && AllReady();
        public int PendingChunks=>jobs.Count;
        public WorldVegetation Vegetation=>vegetation;

        public void Regenerate(ProceduralGeography geography,Camera camera)
        {
            epoch++;
            if(cancellation!=null) { cancellation.Cancel(); cancellation.Dispose(); }
            cancellation=new CancellationTokenSource();
            foreach(var job in jobs) Observe(job.Task);
            jobs.Clear(); if(overviewJob!=null) Observe(overviewJob);
            ClearTiles();
            Geography=geography; view=camera; refreshAt=0;ViewRadius=geography.Configuration.Terrain.RenderRadius;
            Preloading=false;PreloadStatus="";
            string root=AevoriaBootstrap.Argument("-aevoriaTestRoot",Application.persistentDataPath);
            string identity="terrain-payload-3|"+WorldRecord.GeographyFingerprint(geography)+"|"+GeologyMap.Version+"|"+InlandWaterLayout.Version;
            diskCache=new TerrainPresentationCache(Path.Combine(root,"PresentationCache"),identity,64);
            Regional=GetComponent<RegionalTerrain>()??gameObject.AddComponent<RegionalTerrain>();Regional.Cache=diskCache;Regional.Initialize(geography,camera);
            // Distant silhouettes come from canonical overview/regional terrain.
            // Remove the legacy camera-relative decorative mountain curtains.
            var horizon=GetComponentInChildren<DistantMountainHorizon>();if(horizon!=null){horizon.gameObject.SetActive(false);Destroy(horizon.gameObject);}
            var navigation=camera.GetComponent<MapCamera>();if(navigation!=null)navigation.PresentationHeight=CameraSurfaceHeight;
            Regional.SurfaceInstalled=()=>view?.GetComponent<MapCamera>()?.NotifyPresentationChanged();
            overviewHeights=null;
            bankSource=null;
            terrainMaterial=Resources.Load<Material>("WorldTerrain"); waterMaterial=Resources.Load<Material>("WorldWater");
            if(terrainMaterial==null || waterMaterial==null) throw new InvalidOperationException("Tracked world materials missing");
            if(coverage!=null) Destroy(coverage); if(heightTexture!=null) Destroy(heightTexture);
            coverage=new Texture2D(CoverageResolution,CoverageResolution,TextureFormat.RGBA32,false,true) { filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp };
            coveragePixels=new Color[coverage.width*coverage.height]; coverage.SetPixels(coveragePixels); coverage.Apply(false);
            Shader.SetGlobalFloat("_AevoriaOverviewReady",0);
            if(overviewHeightField!=null)Destroy(overviewHeightField);if(overviewNormalField!=null)Destroy(overviewNormalField);
            Shader.SetGlobalTexture("_AevoriaCoverage",coverage);coverageX=coverageZ=-32;PublishCoverageRect();
            var config=geography.Configuration;
            Shader.SetGlobalVector("_AevoriaWorldRect",new Vector4((float)-config.WidthMeters/2,(float)-config.DepthMeters/2,(float)config.WidthMeters,(float)config.DepthMeters));
            Shader.SetGlobalFloat("_AevoriaSeaLevel",geography.SeaLevel);
            Shader.SetGlobalFloat("_AevoriaBoundarySeaLevel",geography.SeaLevel);
            MeshMilliseconds=ReadyMilliseconds=0; generation.Restart();
            if(ocean!=null) Destroy(ocean); if(oceanMesh!=null) Destroy(oceanMesh);
            if(overviewObject!=null) Destroy(overviewObject); if(overviewMesh!=null) Destroy(overviewMesh);
            overviewMesh=null;
            ocean=new GameObject("Ocean horizon"); ocean.transform.SetParent(transform,false);
            float side=Mathf.Max(300000,(float)Math.Max(config.WidthMeters,config.DepthMeters)*2),y=geography.SeaLevel;
            oceanMesh=new Mesh { name="Ocean horizon" };
            oceanMesh.vertices=new[]{new Vector3(-side,y,-side),new Vector3(side,y,-side),new Vector3(-side,y,side),new Vector3(side,y,side)};
            oceanMesh.triangles=new[]{0,2,1,2,3,1}; oceanMesh.RecalculateNormals(); oceanMesh.RecalculateBounds();
            ocean.AddComponent<MeshFilter>().sharedMesh=oceanMesh; ocean.AddComponent<MeshRenderer>().sharedMaterial=waterMaterial;
            vegetation=GetComponent<WorldVegetation>()??gameObject.AddComponent<WorldVegetation>(); vegetation.ResetWorld(geography,camera);
            var token=cancellation.Token;
            overviewJob=Task.Run(()=>GenerateOverview(geography,token),token);
        }
        private static void Observe(Task task) { task.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted); }
        private void Update()
        {
            if(Geography==null || view==null) return;
            if(Geography.InlandSurfaces!=null&&bankSource!=Geography.InlandSurfaces){bankSource=Geography.InlandSurfaces;foreach(var tile in active.Values)tile.Lod=-1;}
            long began=Stopwatch.GetTimestamp();
            if(overviewJob!=null && overviewJob.IsCompleted)
            {
                if(overviewJob.IsFaulted) Debug.LogException(overviewJob.Exception);
                else if(!overviewJob.IsCanceled)
                {
                    var data=overviewJob.Result;
                    overviewObject=new GameObject("Geographic overview and decorative outer scenery"); overviewObject.transform.SetParent(transform,false);
                    overviewMesh=MakeMesh(data.Terrain,"World overview / decorative outside bounds");
                    overviewObject.AddComponent<MeshFilter>().sharedMesh=overviewMesh;
                    var renderer=overviewObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=terrainMaterial;
                    renderer.shadowCastingMode=ShadowCastingMode.Off;
                    var props=new MaterialPropertyBlock(); props.SetFloat("_Backdrop",1); props.SetFloat("_Fade",1); renderer.SetPropertyBlock(props);
                    heightTexture=new Texture2D(257,257,TextureFormat.RFloat,false,true) { filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
                    heightTexture.SetPixelData(data.Heights,0); heightTexture.Apply(false,true);
                    Shader.SetGlobalTexture("_AevoriaHeight",heightTexture);
                    int grid=OverviewResolution+1;var field=new float[grid*grid];var normals=new Color[field.Length];
                    for(int i=0;i<field.Length;i++){field[i]=data.Terrain.Vertices[i].y;var n=data.Terrain.Normals[i];normals[i]=new Color(n.x,n.y,n.z,1);}
                    overviewHeights=field;
                    overviewHeightField=new Texture2D(grid,grid,TextureFormat.RFloat,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};overviewHeightField.SetPixelData(field,0);overviewHeightField.Apply(false,true);
                    overviewNormalField=new Texture2D(grid,grid,TextureFormat.RGBAHalf,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};overviewNormalField.SetPixels(normals);overviewNormalField.Apply(false,true);
                    Shader.SetGlobalTexture("_AevoriaOverviewHeight",overviewHeightField);Shader.SetGlobalTexture("_AevoriaOverviewNormals",overviewNormalField);
                    Shader.SetGlobalVector("_AevoriaOverviewGrid",new Vector4((float)(-Geography.Configuration.WidthMeters*.8),(float)(-Geography.Configuration.DepthMeters*.8),(float)(Geography.Configuration.WidthMeters*1.6/OverviewResolution),(float)(Geography.Configuration.DepthMeters*1.6/OverviewResolution)));Shader.SetGlobalFloat("_AevoriaOverviewReady",1);
                    view.GetComponent<MapCamera>()?.NotifyPresentationChanged();
                }
                overviewJob=null;
            }
            Vector3 position=WorldOrigin.Geographic(view.transform);
            if(Preloading&&(Vector3.Distance(position,preloadPosition)>128||Quaternion.Angle(view.transform.rotation,preloadRotation)>5))CancelPreload();
            if(!Preloading&&Time.unscaledTime>=refreshAt)
            {
                RefreshDesired(position-lastPosition); lastPosition=position; refreshAt=Time.unscaledTime+(StreamingAggressiveness==0?.45f:StreamingAggressiveness==2?.18f:.3f);
            }
            for(int i=jobs.Count-1;i>=0;i--)
            {
                var job=jobs[i]; if(!job.Task.IsCompleted) continue;
                if(job.Task.IsFaulted) Debug.LogException(job.Task.Exception);
                else if(!job.Task.IsCanceled && job.Epoch==epoch && desired.TryGetValue(job.Key,out int lod) && lod==job.Lod)
                {
                    // A LOD-only payload can finish after its old tile was
                    // evicted and the camera returned. Reschedule full placement
                    // rather than installing a new tile with a missing batch.
                    if(!active.ContainsKey(job.Key) && job.Task.Result.Vegetation.Trees==null) {jobs.RemoveAt(i);continue;}
                    if(active.TryGetValue(job.Key,out var replacing) && replacing.Fade<1) continue;
                    Upload(job.Key,lod,job.Task.Result);
                    jobs.RemoveAt(i);
                    break; // One native mesh/instance upload per frame bounds bursts.
                }
                jobs.RemoveAt(i);
            }
            if(overviewMesh!=null)
            foreach(var candidate in candidates)
            {
                if(jobs.Count>=Mathf.Clamp(BackgroundBudget,1,4)) break;
                if(active.TryGetValue(candidate.Key,out var tile) && (tile.Lod==candidate.Lod || tile.Fade<1)) continue;
                bool pending=false; foreach(var job in jobs) if(job.Key.Equals(candidate.Key)) {pending=true;break;}
                if(pending) continue;
                var key=candidate.Key; int lod=candidate.Lod; var geo=Geography; var token=cancellation.Token;
                bool placement=tile==null;
                var cache=diskCache;
                jobs.Add(new Job { Key=key,Lod=lod,Epoch=epoch,Task=Task.Run(()=>CachedPayload(cache,geo,key,lod,placement,token),token) });
            }
            foreach(var pair in active)
            {
                var tile=pair.Value;
                if(tile.Fade<1)
                {
                    tile.Fade=Mathf.Min(1,tile.Fade+Time.unscaledDeltaTime/Mathf.Lerp(.2f,.7f,Shader.GetGlobalFloat("_AevoriaLodTransition")*.5f));
                    ApplyFade(tile,tile.Renderer,false); if(tile.Outgoing!=null) ApplyFade(tile,tile.OutgoingRenderer,true);
                    SetCoverage(pair.Key,tile.Outgoing==null?tile.Fade:1);
                }
                if(tile.Fade>=1 && tile.Outgoing!=null)
                {
                    Destroy(tile.Outgoing); tile.Outgoing=null; tile.OutgoingFilter.sharedMesh=null; tile.OutgoingRenderer.enabled=false;
                    tile.PreviousSurface=null;
                }
            }
            if(coverageDirty) { coverage.Apply(false); coverageDirty=false; }
            if(Preloading)
            {
                int ready=Regional!=null&&Regional.Ready&&Regional.CompletedPreloadVersion>=preloadRegionalVersion?1:0;foreach(var pair in desired)if(active.TryGetValue(pair.Key,out var tile)&&tile.Lod==pair.Value&&tile.Fade>=1)ready++;
                PreloadProgress=preloadTarget==0?0:ready/(float)preloadTarget;PreloadStatus=$"Visible area: {ready}/{preloadTarget} terrain products installed (vegetation included)";
                if(ready==preloadTarget){Preloading=false;PreloadStatus="Visible area ready at selected detail · installed geometry and vegetation";}
            }
            if(ReadyMilliseconds==0 && IsReady) { ReadyMilliseconds=generation.Elapsed.TotalMilliseconds; generation.Stop(); }
            LastUpdateMilliseconds=(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency;
        }
        private bool AllReady()
        {
            if(desired.Count==0) return overviewMesh!=null;
            foreach(var pair in desired) if(!active.TryGetValue(pair.Key,out var tile) || tile.Lod!=pair.Value || tile.Fade<1) return false;
            return true;
        }
        private void RefreshDesired(Vector3 movement)
        {
            desired.Clear(); candidates.Clear();
            var c=Geography.Configuration;
            var controller=view.GetComponent<MapCamera>(); Vector3 focus=controller==null?WorldOrigin.Geographic(view.transform):controller.Focus;
            // Keep a bounded tile budget, but discover candidates across the
            // projected viewport instead of a small square at its focus.
            var center=c.ChunkAt(new GeoPoint(focus.x,focus.z)); int radius=Mathf.Clamp(ViewRadius,2,10);
            if(Math.Abs(center.X-(coverageX+32))>12||Math.Abs(center.Y-(coverageZ+32))>12){coverageX=center.X-32;coverageZ=center.Y-32;Array.Clear(coveragePixels,0,coveragePixels.Length);coverage.SetPixels(coveragePixels);PublishCoverageRect();foreach(var pair in active)SetCoverage(pair.Key,pair.Value.Outgoing==null?pair.Value.Fade:1);coverageDirty=true;}
            GeometryUtility.CalculateFrustumPlanes(view,planes);
            float prediction=StreamingAggressiveness==0?1:StreamingAggressiveness==1?3:6;
            Vector3 lookahead=Vector3.ClampMagnitude(movement*prediction,1500*prediction),predicted=WorldOrigin.Geographic(view.transform)+lookahead;
            double chunkSide=c.ChunkSizeMeters;int minX=-radius,maxX=radius,minY=-radius,maxY=radius;
            var ground=new Plane(Vector3.up,focus);
            for(int vy=0;vy<3;vy++)for(int vx=0;vx<3;vx++)
            {var ray=CameraRays.Viewport(view,new Vector2(vx*.5f,vy*.5f));if(ground.Raycast(ray,out float enter)&&enter<view.farClipPlane)
                {var point=ray.GetPoint(enter)+lookahead;int x=(int)Math.Floor(point.x/chunkSide)-center.X,y=(int)Math.Floor(point.z/chunkSide)-center.Y;minX=Math.Min(minX,x-1);maxX=Math.Max(maxX,x+1);minY=Math.Min(minY,y-1);maxY=Math.Max(maxY,y+1);}}
            minX=Math.Max(-30,minX);maxX=Math.Min(30,maxX);minY=Math.Max(-30,minY);maxY=Math.Min(30,maxY);
            for(int y=minY;y<=maxY;y++) for(int x=minX;x<=maxX;x++)
            {
                var key=new ChunkKey(center.X+x,center.Y+y); double side=c.ChunkSizeMeters;
                if(key.X*side>=c.WidthMeters/2 || (key.X+1)*side<=-c.WidthMeters/2 || key.Y*side>=c.DepthMeters/2 || (key.Y+1)*side<=-c.DepthMeters/2) continue;
                var p=new Vector3((float)((key.X+0.5)*side),(c.Terrain.Mountains+c.Terrain.Elevation)*0.5f,(float)((key.Y+0.5)*side));
                float ceiling=(float)(Geography.Surface?.MaximumElevation??(c.Terrain.Mountains*2+c.Terrain.Elevation+2000));
                const float floor=-6000;
                var bounds=new Bounds(new Vector3(p.x,(ceiling+floor)*.5f,p.z),new Vector3((float)side,ceiling-floor,(float)side));
                bool visible=GeometryUtility.TestPlanesAABB(planes,bounds);
                if(!visible && Math.Max(Math.Abs(x),Math.Abs(y))>2) continue;
                var surfaceCenter=new Vector3(p.x,Geography.ElevationAt(p.x,p.z),p.z);
                float distance=Mathf.Max(20,Vector3.Distance(predicted,surfaceCenter));
                float pixels=(float)c.CellSizeMeters*Screen.height/(2*Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad/2)*distance)*Mathf.Lerp(.7f,1.7f,DetailQuality/3f)*(surfaceCenter.y>1000?Mathf.Lerp(.8f,1.6f,MountainQuality/3f):1);
                if(pixels<.25f)continue; // Regional/planetary geometry already resolves these pixels.
                int lod=pixels>2.2f?0:pixels>0.95f?1:2;
                if(active.TryGetValue(key,out var tile))
                {
                    if(tile.Lod==0 && pixels>1.8f) lod=0;
                    if(tile.Lod==1 && pixels>0.8f && pixels<2.6f) lod=1;
                    if(tile.Lod==2 && pixels<1.1f) lod=2;
                }
                candidates.Add(new Candidate {Key=key,Lod=lod,Score=distance+(visible?0:100000)});
            }
            candidates.Sort((a,b)=>a.Score.CompareTo(b.Score));
            int budget=(radius*2+1)*(radius*2+1);if(candidates.Count>budget)candidates.RemoveRange(budget,candidates.Count-budget);
            foreach(var candidate in candidates)desired[candidate.Key]=candidate.Lod;
            remove.Clear();
            foreach(var pair in active) if(!desired.ContainsKey(pair.Key)) remove.Add(pair.Key);
            foreach(var key in remove) { SetCoverage(key,0); vegetation.Remove(key); Release(active[key]); active.Remove(key); }
        }
        private static Mesh MakeMesh(Payload data,string name)
        {
            var mesh=new Mesh {name=name,indexFormat=IndexFormat.UInt32};
            mesh.vertices=data.Vertices; mesh.normals=data.Normals; mesh.colors=data.Colors;if(data.Banks!=null)mesh.SetUVs(0,data.Banks);mesh.triangles=data.Indices; mesh.RecalculateBounds();mesh.UploadMeshData(true); return mesh;
        }
        private void Upload(ChunkKey key,int lod,Payload data)
        {
            using var measured=UploadMarker.Auto();
            using var work=WorkCounter.Upload.Measure(Time.frameCount);
            if(!active.TryGetValue(key,out var tile)) { tile=pool.Count>0?pool.Pop():CreateTile(); active.Add(key,tile); }
            if(tile.Outgoing!=null) Destroy(tile.Outgoing);
            if(tile.Mesh!=null)
            {
                tile.PreviousSurface=tile.Surface;
                Triangles-=(long)tile.Mesh.GetIndexCount(0)/3; tile.Outgoing=tile.Mesh; tile.OutgoingFilter.sharedMesh=tile.Outgoing; tile.OutgoingRenderer.enabled=true;
            }
            else vegetation.Add(key,data.Vegetation);
            tile.Surface=data;
            tile.Mesh=MakeMesh(data,$"Terrain {key.X},{key.Y} LOD{lod}"); tile.Filter.sharedMesh=tile.Mesh; tile.Lod=lod; tile.Fade=0;
            float side=(float)Geography.Configuration.ChunkSizeMeters;tile.Properties.SetVector("_TileRect",new Vector4(key.X*side,key.Y*side,side,side));
            tile.Object.SetActive(true); Triangles+=data.Indices.Length/3; MeshMilliseconds+=data.Milliseconds;
            ApplyFade(tile,tile.Renderer,false); if(tile.Outgoing!=null) ApplyFade(tile,tile.OutgoingRenderer,true);
            SetCoverage(key,tile.Outgoing==null?0:1);
        }
        private void ApplyFade(Tile tile,MeshRenderer renderer,bool outgoing)
        {
            tile.Properties.SetFloat("_Fade",tile.Fade); tile.Properties.SetFloat("_Outgoing",outgoing?1:0); tile.Properties.SetFloat("_Backdrop",0); renderer.SetPropertyBlock(tile.Properties);
        }
        private void SetCoverage(ChunkKey key,float value)
        {
            int x=key.X-coverageX,y=key.Y-coverageZ;
            if(x<0 || y<0 || x>=coverage.width || y>=coverage.height) return;
            var color=new Color(value,0,0,0);if(coveragePixels[y*coverage.width+x]==color)return;
            coveragePixels[y*coverage.width+x]=color;coverage.SetPixel(x,y,color);coverageDirty=true;
            view.GetComponent<MapCamera>()?.NotifyPresentationChanged();
        }
        private void PublishCoverageRect(){float side=(float)Geography.Configuration.ChunkSizeMeters;Shader.SetGlobalVector("_AevoriaCoverageRect",new Vector4(coverageX*side,coverageZ*side,CoverageResolution*side,CoverageResolution*side));}
        private Tile CreateTile()
        {
            var obj=new GameObject("Terrain chunk"); obj.transform.SetParent(transform,false);
            var old=new GameObject("LOD crossfade"); old.transform.SetParent(obj.transform,false);
            var tile=new Tile {Object=obj,Filter=obj.AddComponent<MeshFilter>(),Renderer=obj.AddComponent<MeshRenderer>(),OutgoingFilter=old.AddComponent<MeshFilter>(),OutgoingRenderer=old.AddComponent<MeshRenderer>()};
            tile.Renderer.sharedMaterial=tile.OutgoingRenderer.sharedMaterial=terrainMaterial;
            tile.OutgoingRenderer.shadowCastingMode=ShadowCastingMode.Off; tile.OutgoingRenderer.enabled=false; return tile;
        }
        private void Release(Tile tile)
        {
            tile.Surface=tile.PreviousSurface=null;
            if(tile.Mesh!=null) { Triangles-=(long)tile.Mesh.GetIndexCount(0)/3; Destroy(tile.Mesh); tile.Mesh=null; }
            if(tile.Outgoing!=null) { Destroy(tile.Outgoing); tile.Outgoing=null; }
            tile.Filter.sharedMesh=tile.OutgoingFilter.sharedMesh=null; tile.OutgoingRenderer.enabled=false; tile.Object.SetActive(false); pool.Push(tile);
        }
        private void ClearTiles()
        {
            foreach(var tile in active.Values) Release(tile); active.Clear(); desired.Clear(); candidates.Clear(); Triangles=0;
        }
        private void OnDestroy()
        {
            var navigation=view==null?null:view.GetComponent<MapCamera>();if(navigation!=null&&navigation.PresentationHeight==(Func<float,float,float>)CameraSurfaceHeight)navigation.PresentationHeight=null;
            if(cancellation!=null) { cancellation.Cancel(); cancellation.Dispose(); }
            foreach(var job in jobs) Observe(job.Task); if(overviewJob!=null) Observe(overviewJob);
            ClearTiles(); foreach(var tile in pool) if(tile.Object!=null) Destroy(tile.Object); pool.Clear();
            if(oceanMesh!=null) Destroy(oceanMesh); if(overviewMesh!=null) Destroy(overviewMesh);
            if(coverage!=null) Destroy(coverage); if(heightTexture!=null) Destroy(heightTexture);
            if(Shader.GetGlobalTexture("_AevoriaOverviewHeight")==overviewHeightField)Shader.SetGlobalFloat("_AevoriaOverviewReady",0);
            if(overviewHeightField!=null)Destroy(overviewHeightField);if(overviewNormalField!=null)Destroy(overviewNormalField);
        }
        private float OverviewSurfaceHeight(float x,float z)
        {
            if(overviewHeights==null)return Geography.ElevationAt(x,z);var c=Geography.Configuration;
            float gx=Mathf.Clamp((float)((x/c.WidthMeters+.8)/1.6*OverviewResolution),0,OverviewResolution),gz=Mathf.Clamp((float)((z/c.DepthMeters+.8)/1.6*OverviewResolution),0,OverviewResolution);
            int ix=Mathf.Min(OverviewResolution-1,(int)gx),iz=Mathf.Min(OverviewResolution-1,(int)gz),row=OverviewResolution+1,i=iz*row+ix;float u=gx-ix,v=gz-iz;
            return u+v<=1?overviewHeights[i]*(1-u-v)+overviewHeights[i+1]*u+overviewHeights[i+row]*v:overviewHeights[i+1]*(1-v)+overviewHeights[i+row]*(1-u)+overviewHeights[i+row+1]*(u+v-1);
        }
        private float FallbackSurfaceHeight(float x,float z)
        {
            if(Regional==null||!Regional.TryTriangle(x,z,out var a,out var b,out var c,out var w))return OverviewSurfaceHeight(x,z);
            float Height(Vector3 v)=>Mathf.Lerp(v.y,OverviewSurfaceHeight(v.x,v.z),Regional.OverviewBlend(v.x,v.z));
            return Height(a)*w.x+Height(b)*w.y+Height(c)*w.z;
        }
        private float CameraSurfaceHeight(float x,float z)
        {
            var c=Geography.Configuration;float actual=Geography.ElevationAt(x,z),fallback=FallbackSurfaceHeight(x,z);var key=c.ChunkAt(new GeoPoint(x,z));
            if(!active.TryGetValue(key,out var tile)||tile.Surface==null)return Mathf.Max(actual,fallback);
            float Coverage(ChunkKey k)=>active.TryGetValue(k,out var t)?t.Outgoing!=null?1:t.Fade:0;
            float own=Coverage(key),side=(float)c.ChunkSizeMeters,width=Mathf.Min(64,side*.25f);
            float left=1-Mathf.Min(own,Coverage(new ChunkKey(key.X-1,key.Y))),right=1-Mathf.Min(own,Coverage(new ChunkKey(key.X+1,key.Y))),down=1-Mathf.Min(own,Coverage(new ChunkKey(key.X,key.Y-1))),up=1-Mathf.Min(own,Coverage(new ChunkKey(key.X,key.Y+1)));
            float VertexHeight(Vector3 v)
            {
                float lx=v.x-key.X*side,lz=v.z-key.Y*side,blend=Mathf.Max(Mathf.Max(left*Mathf.Clamp01(1-lx/width),right*Mathf.Clamp01(1-(side-lx)/width)),Mathf.Max(down*Mathf.Clamp01(1-lz/width),up*Mathf.Clamp01(1-(side-lz)/width)));
                return blend>0?Mathf.Lerp(v.y,FallbackSurfaceHeight(v.x,v.z),blend):v.y;
            }
            float Sample(Payload p)
            {
                int n=(int)Math.Round(Math.Sqrt(p.Vertices.Length-(c.CellsPerChunk+1)*4))-1,row=n+1;float gx=Mathf.Clamp((x-key.X*side)/side*n,0,n),gz=Mathf.Clamp((z-key.Y*side)/side*n,0,n);
                int ix=Mathf.Min(n-1,(int)gx),iz=Mathf.Min(n-1,(int)gz),i=iz*row+ix;float u=gx-ix,v=gz-iz;
                if(ix>0&&iz>0&&ix<n-1&&iz<n-1)
                    return u+v<=1?VertexHeight(p.Vertices[i])*(1-u-v)+VertexHeight(p.Vertices[i+1])*u+VertexHeight(p.Vertices[i+row])*v:VertexHeight(p.Vertices[i+1])*(1-v)+VertexHeight(p.Vertices[i+row])*(1-u)+VertexHeight(p.Vertices[i+row+1])*(u+v-1);
                // The exterior stitch uses finest-edge vertices, not the coarse
                // square's usual two triangles. Query those installed triangles.
                float high=float.NegativeInfinity;
                for(int t=(n-2)*(n-2)*6;t<p.Indices.Length;t+=3)
                {
                    var a=p.Vertices[p.Indices[t]];var b=p.Vertices[p.Indices[t+1]];var d=p.Vertices[p.Indices[t+2]];
                    if(x<Mathf.Min(a.x,Mathf.Min(b.x,d.x))||x>Mathf.Max(a.x,Mathf.Max(b.x,d.x))||z<Mathf.Min(a.z,Mathf.Min(b.z,d.z))||z>Mathf.Max(a.z,Mathf.Max(b.z,d.z)))continue;
                    float denominator=(b.z-d.z)*(a.x-d.x)+(d.x-b.x)*(a.z-d.z);if(Mathf.Abs(denominator)<.00001f)continue;
                    float wa=((b.z-d.z)*(x-d.x)+(d.x-b.x)*(z-d.z))/denominator,wb=((d.z-a.z)*(x-d.x)+(a.x-d.x)*(z-d.z))/denominator,wc=1-wa-wb;
                    if(wa<-.0001f||wb<-.0001f||wc<-.0001f)continue;
                    high=Mathf.Max(high,VertexHeight(a)*wa+VertexHeight(b)*wb+VertexHeight(d)*wc);
                }
                return float.IsNegativeInfinity(high)?actual:high;
            }
            float floor=Mathf.Max(actual,Sample(tile.Surface));if(tile.PreviousSurface!=null)floor=Mathf.Max(floor,Sample(tile.PreviousSurface));if(own<1)floor=Mathf.Max(floor,fallback);return floor;
        }
        private static Overview GenerateOverview(ProceduralGeography geo,CancellationToken token)
        {
            const int n=OverviewResolution; int row=n+1;
            var p=new Payload {Vertices=new Vector3[row*row],Normals=new Vector3[row*row],Banks=new Vector4[row*row],Indices=new int[n*n*6]};
            double width=geo.Configuration.WidthMeters*1.6,depth=geo.Configuration.DepthMeters*1.6;
            for(int z=0;z<=n;z++)
            {
                token.ThrowIfCancellationRequested();
                for(int x=0;x<=n;x++)
                {
                    double east=(double)x/n*width-width/2,north=(double)z/n*depth-depth/2;
                    float h=SceneryHeight(geo,east,north);
                    int i=z*row+x; p.Vertices[i]=new Vector3((float)east,h,(float)north);p.Banks[i]=BankSample(geo,east,north,h);
                }
            }
            for(int z=0;z<=n;z++){token.ThrowIfCancellationRequested();for(int x=0;x<=n;x++){int left=Math.Max(0,x-1),right=Math.Min(n,x+1),down=Math.Max(0,z-1),up=Math.Min(n,z+1);p.Normals[z*row+x]=new Vector3((p.Vertices[z*row+left].y-p.Vertices[z*row+right].y)/(float)((right-left)*width/n),1,(p.Vertices[down*row+x].y-p.Vertices[up*row+x].y)/(float)((up-down)*depth/n)).normalized;}}
            int t=0; for(int z=0;z<n;z++) for(int x=0;x<n;x++) {int a=z*row+x; Add(p.Indices,ref t,a,a+row,a+1); Add(p.Indices,ref t,a+1,a+row,a+row+1);}
            var heights=new float[257*257];
            for(int z=0;z<=256;z++) { token.ThrowIfCancellationRequested(); for(int x=0;x<=256;x++) heights[z*257+x]=geo.ElevationAt((double)x/256*geo.Configuration.WidthMeters-geo.Configuration.WidthMeters/2,(double)z/256*geo.Configuration.DepthMeters-geo.Configuration.DepthMeters/2); }
            return new Overview {Terrain=p,Heights=heights};
        }
        private static float SceneryHeight(ProceduralGeography geo,double x,double z)
        {
            if(geo.Configuration.Contains(new GeoPoint(x,z))) return geo.ElevationAt(x,z);
            // Carry the same field across the simulation rectangle. A hard
            // ocean override clipped high-relief land when it reached an edge.
            return geo.SceneryElevationAt(x,z);
        }
        private static Payload Generate(ProceduralGeography geo, ChunkKey key, int lod, CancellationToken token)
            =>GeneratePayload(geo,key,lod,true,token);
        private static Payload CachedPayload(TerrainPresentationCache cache,ProceduralGeography geo,ChunkKey key,int lod,bool placement,CancellationToken token)
        {
            string product=$"{key.X}:{key.Y}:{lod}:{placement}";
            if(cache.TryRead(product,out var bytes))try
            {
                token.ThrowIfCancellationRequested();using(var reader=new BinaryReader(new MemoryStream(bytes)))
                {
                    int n=geo.Configuration.CellsPerChunk>>lod,count=(n+1)*(n+1)+(geo.Configuration.CellsPerChunk+1)*4;
                    if(reader.ReadInt32()!=count)throw new InvalidDataException("Cached mesh count");
                    var p=new Payload{Vertices=new Vector3[count],Normals=new Vector3[count],Banks=new Vector4[count],Indices=topology.GetOrAdd((geo.Configuration.CellsPerChunk<<8)|lod,CreateTopology)};
                    for(int i=0;i<count;i++){p.Vertices[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());p.Normals[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());p.Banks[i]=new Vector4(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());}
                    VegetationSite[] Sites(){int length=reader.ReadInt32();if(length<0||length>8192)throw new InvalidDataException("Cached vegetation count");var sites=new VegetationSite[length];for(int i=0;i<length;i++)sites[i]=new VegetationSite(reader.ReadDouble(),reader.ReadDouble(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadBoolean(),reader.ReadInt32(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());return sites;}
                    if(placement)p.Vegetation=new VegetationChunk(Sites(),Sites(),Sites());
                    if(reader.BaseStream.Position!=reader.BaseStream.Length)throw new InvalidDataException("Cache trailing bytes");return p;
                }
            }catch(Exception ex)when(ex is IOException||ex is InvalidDataException){} // Reconstruct malformed products; world data is untouched.
            var generated=GeneratePayload(geo,key,lod,placement,token);token.ThrowIfCancellationRequested();
            try
            {
                using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
                {
                    writer.Write(generated.Vertices.Length);for(int i=0;i<generated.Vertices.Length;i++){var v=generated.Vertices[i];var normal=generated.Normals[i];var b=generated.Banks[i];writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);writer.Write(normal.x);writer.Write(normal.y);writer.Write(normal.z);writer.Write(b.x);writer.Write(b.y);writer.Write(b.z);writer.Write(b.w);}
                    void Sites(VegetationSite[] sites){writer.Write(sites.Length);foreach(var s in sites){writer.Write(s.East);writer.Write(s.North);writer.Write(s.Height);writer.Write(s.Scale);writer.Write(s.Yaw);writer.Write(s.Conifer);writer.Write(s.Variant);writer.Write(s.WidthScale);writer.Write(s.SlopeEast);writer.Write(s.SlopeNorth);}}
                    if(placement){Sites(generated.Vegetation.Trees);Sites(generated.Vegetation.Grass);Sites(generated.Vegetation.Rocks);}writer.Flush();cache.Write(product,stream.ToArray());
                }
            }catch(Exception ex)when(ex is IOException||ex is UnauthorizedAccessException){} // Cache is optional, including on a full/unavailable disk.
            return generated;
        }
        private static Payload GeneratePayload(ProceduralGeography geo, ChunkKey key, int lod,bool placement, CancellationToken token)
        {
            var watch = Stopwatch.StartNew();
            var c = geo.Configuration; int n = c.CellsPerChunk >> lod, row = n + 1;
            int surface = row * row, border = (c.CellsPerChunk+1) * 4;
            var p = new Payload { Vertices = new Vector3[surface + border], Normals = new Vector3[surface + border],
                Indices = topology.GetOrAdd((c.CellsPerChunk<<8)|lod,CreateTopology),Banks=new Vector4[surface+border] };
            double step = c.ChunkSizeMeters / n;
            for (int y = 0; y <= n; y++)
            {
                token.ThrowIfCancellationRequested();
                for (int x = 0; x <= n; x++)
                {
                    double east = key.X * c.ChunkSizeMeters + x * step, north = key.Y * c.ChunkSizeMeters + y * step;
                    float h = geo.ElevationAt(east, north);
                    int i = y * row + x; p.Vertices[i] = new Vector3((float)east, h, (float)north);
                    p.Banks[i]=BankSample(geo,east,north,h);
                }
            }
            for(int y=0;y<=n;y++)
            {
                token.ThrowIfCancellationRequested();
                for(int x=0;x<=n;x++)
                {
                    int i=y*row+x,edge=Math.Min(Math.Min(x,n-x),Math.Min(y,n-y));
                    if(edge>=2){p.Normals[i]=Vector3.Cross(p.Vertices[i+row]-p.Vertices[i-row],p.Vertices[i+1]-p.Vertices[i-1]).normalized;continue;}
                    double east=key.X*c.ChunkSizeMeters+x*step,north=key.Y*c.ChunkSizeMeters+y*step,normalStep=edge==0?2:2+(step-2)*.5;
                    p.Normals[i]=new Vector3(geo.ElevationAt(east-normalStep,north)-geo.ElevationAt(east+normalStep,north),(float)(normalStep*2),geo.ElevationAt(east,north-normalStep)-geo.ElevationAt(east,north+normalStep)).normalized;
                }
            }
            // Every LOD owns the same finest-resolution boundary profile.
            // A stitched surface ring replaces submerged opaque skirt walls.
            int fine=c.CellsPerChunk;
            for(int edge=0;edge<4;edge++)for(int i=0;i<=fine;i++)
            {
                double u=(double)i/fine,ex=edge==0?u:edge==1?1:edge==2?1-u:0,ez=edge==0?0:edge==1?u:edge==2?1:1-u;
                double east=(key.X+ex)*c.ChunkSizeMeters,north=(key.Y+ez)*c.ChunkSizeMeters;
                int dst=surface+edge*(fine+1)+i;
                p.Vertices[dst]=new Vector3((float)east,geo.ElevationAt(east,north),(float)north);
                p.Normals[dst]=new Vector3(geo.ElevationAt(east-2,north)-geo.ElevationAt(east+2,north),4,geo.ElevationAt(east,north-2)-geo.ElevationAt(east,north+2)).normalized;
                p.Banks[dst]=BankSample(geo,east,north,p.Vertices[dst].y);
            }
            if(placement) p.Vegetation=VegetationDistribution.CreateChunk(geo,key,token);
            p.Milliseconds = watch.Elapsed.TotalMilliseconds; return p;
        }
        private static int[] CreateTopology(int key)
        {
            int fine=key>>8,lod=key&255,n=fine>>lod,row=n+1,surface=row*row,ratio=1<<lod;
            var indices=new List<int>((n*n+fine*4)*6);
            for(int y=1;y<n-1;y++)for(int x=1;x<n-1;x++){int a=y*row+x;indices.AddRange(new[]{a,a+row,a+1,a+1,a+row,a+row+1});}
            for(int edge=0;edge<4;edge++)for(int i=0;i<n;i++)
            {
                int ca=Math.Max(1,Math.Min(n-1,i)),cb=Math.Max(1,Math.Min(n-1,i+1));
                int a=edge==0?row+ca:edge==1?ca*row+n-1:edge==2?(n-1)*row+n-ca:(n-ca)*row+1;
                int b=edge==0?row+cb:edge==1?cb*row+n-1:edge==2?(n-1)*row+n-cb:(n-cb)*row+1;
                int outer=surface+edge*(fine+1)+i*ratio;
                for(int j=0;j<ratio;j++){indices.Add(a);indices.Add(outer+j+1);indices.Add(outer+j);}
                if(a!=b){indices.Add(a);indices.Add(b);indices.Add(outer+ratio);}
            }
            return indices.ToArray();
        }
        internal static Vector4 BankSample(ProceduralGeography geo,double x,double z,float height)
        {
            if(geo.InlandSurfaces!=null&&geo.InlandSurfaces.LakeAt(x,z,out float lake)&&Mathf.Abs(height-lake)<8)return new Vector4(0,height-lake,1,1);
            if(geo.Hydrology.ChannelSample(x,z,out float level,out float distance,out _)&&Mathf.Abs(height-level)<12)return new Vector4(distance,height-level,1,0);
            return new Vector4(100,100,0,0);
        }
        private static void Add(int[] a, ref int i, int x, int y, int z) { a[i++] = x; a[i++] = y; a[i++] = z; }
        private static Color SurfaceColor(float height, float slope, double x, double z)
        {
            Color sand = new Color(0.65f,0.57f,0.38f), grass = new Color(0.24f,0.37f,0.18f);
            Color soil = new Color(0.36f,0.33f,0.21f), rock = new Color(0.40f,0.42f,0.40f), snow = new Color(0.88f,0.92f,0.94f);
            Color color = Color.Lerp(sand, grass, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(3, 18, height)));
            color = Color.Lerp(color, soil, Mathf.SmoothStep(0,1,Mathf.InverseLerp(40,130,height)) * 0.4f);
            color = Color.Lerp(color, rock, Mathf.Max(Mathf.SmoothStep(0,1,Mathf.InverseLerp(0.12f,0.42f,slope)), Mathf.InverseLerp(150,270,height)));
            color = Color.Lerp(color, snow, Mathf.SmoothStep(0,1,Mathf.InverseLerp(285,360,height)) * (1 - Mathf.InverseLerp(0.25f,0.6f,slope)));
            if (height < 0) color = Color.Lerp(new Color(0.12f,0.22f,0.22f), sand, Mathf.InverseLerp(-65,0,height));
            float macro = (float)(Math.Sin(x * 0.019 + Math.Sin(z * 0.007)) * Math.Cos(z * 0.017) * 0.045);
            return color * (1 + macro);
        }
    }
}

