using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Aevoria.World;
using Aevoria.Persistence;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    // One bounded regional clipmap bridges local 8 m chunks and the planetary
    // overview. Its true triangle field is also the near-tile join target.
    public sealed class RegionalTerrain : MonoBehaviour
    {
        private sealed class Data {public Vector3[] Vertices,Normals;public Vector4[] Banks;public Color[] NormalPixels;public Bounds Bounds;public int[] Indices;public float[] Heights;public Vector2 Origin;public int N,PreloadVersion;public float Step;}
        private ProceduralGeography geo;private Camera view;private CancellationTokenSource cancel;private Task<Data> worker;private GameObject surface;private Mesh mesh;private Texture2D heights,normals;private Vector2 requested=new Vector2(float.MaxValue,float.MaxValue);private int requestedQuality=-1;private float requestedStep;
        private Mesh stagingMesh;private Texture2D stagingHeights,stagingNormals;private bool installing;private int requestedN;private bool coverVisibleFootprint;
        public TerrainPresentationCache Cache {get;set;}
        public Action SurfaceInstalled {get;set;}
        private int preloadVersion;private bool preloadQueued;
        public int CompletedPreloadVersion {get;private set;}
        public int RequestPreload(){preloadQueued=true;return ++preloadVersion;}
        public void CancelPreload()=>preloadQueued=false;
        private Data installed;
        public bool TryTriangle(float x,float z,out Vector3 a,out Vector3 b,out Vector3 c,out Vector3 weights)
        {
            a=b=c=weights=Vector3.zero;if(installed==null)return false;var d=installed;float gx=(x-d.Origin.x)/d.Step,gz=(z-d.Origin.y)/d.Step;
            if(gx<0||gz<0||gx>d.N||gz>d.N)return false;int ix=Mathf.Min(d.N-1,(int)gx),iz=Mathf.Min(d.N-1,(int)gz),i=iz*(d.N+1)+ix;float u=gx-ix,v=gz-iz;
            if(u+v<=1){a=d.Vertices[i];b=d.Vertices[i+1];c=d.Vertices[i+d.N+1];weights=new Vector3(1-u-v,u,v);}
            else{a=d.Vertices[i+1];b=d.Vertices[i+d.N+1];c=d.Vertices[i+d.N+2];weights=new Vector3(1-v,1-u,u+v-1);}return true;
        }
        public float OverviewBlend(float x,float z)
        {if(installed==null)return 1;var d=installed;float edge=Mathf.Min(x-d.Origin.x,z-d.Origin.y,d.Origin.x+d.N*d.Step-x,d.Origin.y+d.N*d.Step-z);return 1-Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/(d.Step*8)));}
        public bool TryHeight(float x,float z,out float height)
        {
            height=0;if(installed==null)return false;var d=installed;float gx=(x-d.Origin.x)/d.Step,gz=(z-d.Origin.y)/d.Step;
            if(gx<0||gz<0||gx>d.N||gz>d.N)return false;
            int ix=Mathf.Min(d.N-1,(int)gx),iz=Mathf.Min(d.N-1,(int)gz),i=iz*(d.N+1)+ix;float a=gx-ix,b=gz-iz;
            height=a+b<=1?d.Vertices[i].y*(1-a-b)+d.Vertices[i+1].y*a+d.Vertices[i+d.N+1].y*b:d.Vertices[i+1].y*(1-b)+d.Vertices[i+d.N+1].y*(1-a)+d.Vertices[i+d.N+2].y*(a+b-1);
            // Edge morph can include the overview; caller retains that bound.
            return true;
        }
        public int Quality {get;set;}=1;public bool Ready=>mesh!=null&&worker==null&&!installing;public int OwnedMeshes=>mesh==null?0:1;public int StagingMeshes=>stagingMesh==null?0:1;public long Triangles=>mesh==null?0:(long)mesh.GetIndexCount(0)/3;
        public void Initialize(ProceduralGeography geography,Camera camera)
        {Cleanup();geo=geography;view=camera;cancel=new CancellationTokenSource();requested=new Vector2(float.MaxValue,float.MaxValue);requestedQuality=-1;coverVisibleFootprint=!AevoriaBootstrap.HasFlag("-aevoria011LegacyRegionalCoverage");Request();}
        private void Update()
        {
            if(geo==null)return;
            if(worker!=null&&worker.IsCompleted)
            {if(worker.IsFaulted)Debug.LogException(worker.Exception);else if(!worker.IsCanceled){installing=true;StartCoroutine(Install(worker.Result));}worker=null;}
            if(worker==null&&!installing)Request();
        }
        private void Request()
        {
            var controller=view.GetComponent<MapCamera>();var focus=controller!=null?controller.Focus:WorldOrigin.Geographic(view.transform);
            int n=Quality==0?256:Quality==1?512:1024;
            float range=Vector3.Distance(WorldOrigin.Geographic(view.transform),focus);
            float span=Mathf.Max(4096,range*Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f)*Mathf.Max(1,view.aspect)*3);
            // Include the finite visible landscape, rather than assuming the
            // orbit focus is also the centre of a pitched camera's footprint.
            var ground=new Plane(Vector3.up,focus);Vector2 low=new Vector2(focus.x,focus.z),high=low;
            float localReach=Mathf.Max(4096,range*1.5f);
            for(int y=0;y<3;y++)for(int x=0;x<3;x++){var ray=CameraRays.Viewport(view,new Vector2(x*.5f,y*.5f));if(ground.Raycast(ray,out float hit)&&hit<view.farClipPlane){var p=ray.GetPoint(hit);float half=(float)geo.Configuration.WidthMeters*.5f;var q=new Vector2(Mathf.Clamp(p.x,Mathf.Max(-half,focus.x-localReach),Mathf.Min(half,focus.x+localReach)),Mathf.Clamp(p.z,Mathf.Max(-half,focus.z-localReach),Mathf.Min(half,focus.z+localReach)));low=Vector2.Min(low,q);high=Vector2.Max(high,q);}}
            span=Mathf.Max(span,Mathf.Max(high.x-low.x,high.y-low.y)*1.15f);
            float step=Mathf.Clamp(Mathf.Pow(2,Mathf.Ceil(Mathf.Log(span/n,2))),8,16384);
            // At planetary zoom the regional layer must never replace the
            // overview with coarser geometry. Its bounded footprint can shrink;
            // the more distant overview remains visible outside that footprint.
            float overviewStep=(float)(geo.Configuration.WidthMeters*1.6/WorldRenderer.OverviewResolution);
            float maximum=Mathf.Max(8,Mathf.Pow(2,Mathf.Floor(Mathf.Log(overviewStep,2))));step=Mathf.Min(step,maximum);
            // Grazing horizon rays must not turn the near clipmap into a
            // continent-sized coarse mesh. The overview owns that far field;
            // preserve a physical near sampling limit for mountain contacts.
            if(range<30000)step=Mathf.Min(step,32);
            if(coverVisibleFootprint&&range<30000)
            {
                // Preserve the visible footprint when limiting physical sample
                // spacing. Capping step alone shrank the mesh to 16 km and
                // exposed kilometre-scale overview triangles across lakes.
                int maximumN=Quality==0?1024:2048;
                n=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(span/step)),n,maximumN);
                if(n*step<span)step=Mathf.Min(maximum,Mathf.Pow(2,Mathf.Ceil(Mathf.Log(span/n,2))));
            }
            float snap=n*step/8;var middle=(low+high)*.5f;var center=new Vector2(Mathf.Round(middle.x/snap)*snap,Mathf.Round(middle.y/snap)*snap);
            if(center==requested&&requestedQuality==Quality&&requestedStep==step&&requestedN==n&&!preloadQueued)return;
            int version=preloadQueued?preloadVersion:0;preloadQueued=false;
            requested=center;requestedQuality=Quality;requestedStep=step;requestedN=n;var geography=geo;var token=cancel.Token;var cache=Cache;worker=Task.Run(()=>{var data=GenerateCached(cache,geography,center,n,step,token);data.PreloadVersion=version;return data;},token);
        }
        private static Data GenerateCached(TerrainPresentationCache cache,ProceduralGeography geo,Vector2 center,int n,float step,CancellationToken token)
        {
            string key=FormattableString.Invariant($"regional-2:{center.x:R}:{center.y:R}:{n}:{step:R}");float[] samples=null;int count=(n+1)*(n+1);
            if(cache!=null&&cache.TryRead(key,out var bytes)&&bytes.Length==count*4){samples=new float[count];Buffer.BlockCopy(bytes,0,samples,0,bytes.Length);foreach(float value in samples)if(float.IsNaN(value)||float.IsInfinity(value)){samples=null;break;}}
            var data=GenerateCore(geo,center,n,step,token,samples);
            if(cache!=null&&samples==null){token.ThrowIfCancellationRequested();try{var encoded=new byte[count*4];Buffer.BlockCopy(data.Heights,0,encoded,0,encoded.Length);cache.Write(key,encoded);}catch(Exception ex)when(ex is IOException||ex is UnauthorizedAccessException){}}
            return data;
        }
        private static Data Generate(ProceduralGeography geo,Vector2 center,int n,float step,CancellationToken token)
            =>GenerateCore(geo,center,n,step,token,null);
        private static Data GenerateCore(ProceduralGeography geo,Vector2 center,int n,float step,CancellationToken token,float[] cached)
        {
            int row=n+1;var data=new Data{N=n,Step=step,Origin=center-Vector2.one*(n*step*.5f),Vertices=new Vector3[row*row],Normals=new Vector3[row*row],Banks=new Vector4[row*row],NormalPixels=new Color[row*row],Heights=new float[row*row],Indices=new int[n*n*6]};
            for(int z=0;z<=n;z++){token.ThrowIfCancellationRequested();for(int x=0;x<=n;x++){double east=data.Origin.x+x*step,north=data.Origin.y+z*step;int i=z*row+x;float h;if(cached!=null)h=cached[i];else h=geo.Configuration.Contains(new GeoPoint(east,north))?geo.ElevationAt(east,north):geo.SceneryElevationAt(east,north);data.Heights[i]=h;data.Vertices[i]=new Vector3((float)east,h,(float)north);data.Banks[i]=WorldRenderer.BankSample(geo,east,north,h);}}
            // Normals describe the sampled mesh, rather than unrelated 2 m
            // terrain frequencies that cannot exist on these triangles.
            float low=float.MaxValue,high=float.MinValue;
            for(int z=0;z<=n;z++){token.ThrowIfCancellationRequested();for(int x=0;x<=n;x++)
            {int left=Mathf.Max(0,x-1),right=Mathf.Min(n,x+1),down=Mathf.Max(0,z-1),up=Mathf.Min(n,z+1);int at=z*row+x;var normal=new Vector3((data.Heights[z*row+left]-data.Heights[z*row+right])/((right-left)*step),1,(data.Heights[down*row+x]-data.Heights[up*row+x])/((up-down)*step)).normalized;data.Normals[at]=normal;data.NormalPixels[at]=new Color(normal.x,normal.y,normal.z,0);low=Math.Min(low,data.Heights[at]);high=Math.Max(high,data.Heights[at]);}}
            data.Bounds=new Bounds(new Vector3(center.x,(low+high)*.5f,center.y),new Vector3(n*step,Math.Max(.1f,high-low),n*step));
            int indexAt=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*row+x;data.Indices[indexAt++]=a;data.Indices[indexAt++]=a+row;data.Indices[indexAt++]=a+1;data.Indices[indexAt++]=a+1;data.Indices[indexAt++]=a+row;data.Indices[indexAt++]=a+row+1;}return data;
        }
        private IEnumerator Install(Data data)
        {
            // Stage native copies over several frames, keeping the old fallback
            // visible. Publish the replacement and matching fields atomically.
            {using var work=Aevoria.Core.WorkCounter.Upload.Measure(Time.frameCount);stagingMesh=new Mesh{name="Regional terrain clipmap",indexFormat=IndexFormat.UInt32};stagingMesh.vertices=data.Vertices;}yield return null;
            {using var work=Aevoria.Core.WorkCounter.Upload.Measure(Time.frameCount);stagingMesh.normals=data.Normals;}yield return null;
            {using var work=Aevoria.Core.WorkCounter.Upload.Measure(Time.frameCount);stagingMesh.SetUVs(0,data.Banks);}yield return null;
            {using var work=Aevoria.Core.WorkCounter.Upload.Measure(Time.frameCount);stagingMesh.SetTriangles(data.Indices,0,false);stagingMesh.bounds=data.Bounds;}yield return null;
            {using var work=Aevoria.Core.WorkCounter.Upload.Measure(Time.frameCount);stagingHeights=new Texture2D(data.N+1,data.N+1,TextureFormat.RFloat,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};stagingHeights.SetPixelData(data.Heights,0);stagingHeights.Apply(false,true);}yield return null;
            {using var work=Aevoria.Core.WorkCounter.Upload.Measure(Time.frameCount);stagingNormals=new Texture2D(data.N+1,data.N+1,TextureFormat.RGBAFloat,false,true){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};stagingNormals.SetPixelData(data.NormalPixels,0);stagingNormals.Apply(false,true);}yield return null;
            if(surface==null){surface=new GameObject("Regional terrain");surface.transform.SetParent(transform,false);surface.AddComponent<MeshFilter>();var renderer=surface.AddComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("WorldTerrain");renderer.shadowCastingMode=ShadowCastingMode.Off;var properties=new MaterialPropertyBlock();properties.SetFloat("_Backdrop",2);properties.SetFloat("_Fade",1);renderer.SetPropertyBlock(properties);}
            surface.GetComponent<MeshFilter>().sharedMesh=stagingMesh;if(mesh!=null)Destroy(mesh);mesh=stagingMesh;stagingMesh=null;
            if(heights!=null)Destroy(heights);if(normals!=null)Destroy(normals);heights=stagingHeights;normals=stagingNormals;stagingHeights=stagingNormals=null;
            Shader.SetGlobalTexture("_AevoriaRegionalHeight",heights);Shader.SetGlobalTexture("_AevoriaRegionalNormals",normals);Shader.SetGlobalVector("_AevoriaRegionalRect",new Vector4(data.Origin.x,data.Origin.y,data.N*data.Step,data.N*data.Step));Shader.SetGlobalVector("_AevoriaRegionalGrid",new Vector4(data.Step,data.Step,data.N,data.N));Shader.SetGlobalFloat("_AevoriaRegionalReady",1);
            // Queries retain the canonical triangle positions. Upload-only
            // normals, bank attributes, indices and texture copies need no
            // second CPU lifetime after publication.
            mesh.UploadMeshData(true);
            data.Normals=null;data.Banks=null;data.NormalPixels=null;data.Indices=null;data.Heights=null;installed=data;
            if(data.PreloadVersion>0)CompletedPreloadVersion=data.PreloadVersion;
            SurfaceInstalled?.Invoke();
            installing=false;
        }
        private void Cleanup()
        {installed=null;preloadQueued=false;preloadVersion=0;CompletedPreloadVersion=0;StopAllCoroutines();installing=false;if(stagingMesh!=null)Destroy(stagingMesh);if(stagingHeights!=null)Destroy(stagingHeights);if(stagingNormals!=null)Destroy(stagingNormals);stagingMesh=null;stagingHeights=stagingNormals=null;cancel?.Cancel();cancel?.Dispose();if(worker!=null)worker.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);worker=null;if(Shader.GetGlobalTexture("_AevoriaRegionalHeight")==heights){Shader.SetGlobalFloat("_AevoriaRegionalReady",0);Shader.SetGlobalTexture("_AevoriaRegionalHeight",Texture2D.blackTexture);}if(surface!=null){surface.SetActive(false);Destroy(surface);}if(mesh!=null)Destroy(mesh);if(heights!=null)Destroy(heights);if(normals!=null)Destroy(normals);surface=null;mesh=null;heights=normals=null;}
        private void OnDestroy()=>Cleanup();
    }
}
