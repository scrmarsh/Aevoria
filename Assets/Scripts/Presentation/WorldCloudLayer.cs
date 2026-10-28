using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    public sealed class WorldCloudLayer : MonoBehaviour
    {
        public const int SystemLimit=16;
        private readonly Vector4[] systems=new Vector4[SystemLimit],shapes=new Vector4[SystemLimit];
        private static readonly Vector2Int[] cells={new Vector2Int(0,0),new Vector2Int(-1,0),new Vector2Int(0,-1),new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(-1,-1),new Vector2Int(1,1),new Vector2Int(-1,1),new Vector2Int(1,-1),new Vector2Int(-2,0),new Vector2Int(0,-2),new Vector2Int(2,0),new Vector2Int(0,2),new Vector2Int(-2,-1),new Vector2Int(1,-2),new Vector2Int(2,1)};
        private readonly GameObject[] objects=new GameObject[SystemLimit];
        private Mesh mesh;private Material material;private Texture3D cloudNoise;private ProceduralGeography geo;private float radius,thickness;
        public bool RenderClouds {get;set;}=true;public int Density {get;set;}=2;
        public int VisibleSystems {get;private set;}
        public double PresentedTick {get;private set;}
        public void Initialize(ProceduralGeography geography)
        {
            geo=geography;radius=6500;thickness=1800;
            if(mesh==null){var box=new WorldShapes();box.Box(Vector3.zero,Vector3.one*2,Color.white);mesh=box.Build("Shared cloud volume bounds");}
            if(material==null)material=new Material(Resources.Load<Material>("WorldClouds"));
            if(cloudNoise==null)
            {
                const int n=32;var values=new byte[n*n*n];uint state=481923;
                for(int i=0;i<values.Length;i++){state^=state<<13;state^=state>>17;state^=state<<5;values[i]=(byte)(state>>24);}
                cloudNoise=new Texture3D(n,n,n,TextureFormat.R8,true){name="Coherent cloud erosion",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Repeat};cloudNoise.SetPixelData(values,0);cloudNoise.Apply(true,true);
            }
            Shader.SetGlobalTexture("_AevoriaCloudNoise",cloudNoise);
            for(int i=0;i<SystemLimit;i++)
            {
                if(objects[i]==null){var obj=objects[i]=new GameObject("Cumulus system "+(i+1));obj.transform.SetParent(transform,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;var block=new MaterialPropertyBlock();block.SetFloat("_CloudSystemIndex",i);renderer.SetPropertyBlock(block);}
            }
        }
        public void Present(double tick)
        {
            if(geo==null)return;PresentedTick=tick;VisibleSystems=0;int count=Density==1?6:Density==2?10:Density==3?16:0;bool show=RenderClouds&&Density>0&&Shader.GetGlobalFloat("_AevoriaCloudQuality")>.1f;
            var view=Camera.main;var navigation=view==null?null:view.GetComponent<MapCamera>();
            Shader.SetGlobalFloat("_AevoriaGodView",navigation!=null&&navigation.enabled?1:0);
            // Select nearby cells from one world-anchored weather lattice. Zoom
            // never changes a formation's shape or location; distant cells only
            // stream when the observation point moves across the physical grid.
            var focus=navigation!=null&&navigation.enabled?navigation.Focus:view!=null?WorldOrigin.Geographic(view.transform):Vector3.zero;
            const double spacing=20000;double day=tick/4.0,driftX=day*600,driftZ=day*140;
            int centreX=(int)System.Math.Floor((focus.x-driftX)/spacing),centreZ=(int)System.Math.Floor((focus.z-driftZ)/spacing);
            for(int i=0;i<SystemLimit;i++)
            {
                int cellX=centreX+cells[i].x,cellZ=centreZ+cells[i].y;
                float variation=(float)geo.SiteRandom(cellX,cellZ,713),wide=radius*(.8f+variation*.4f),tall=thickness*(.8f+variation*.4f),phase=(float)geo.SiteRandom(cellX,cellZ,714)*6.28f;
                var anchor=new Vector2((float)((cellX+.35+geo.SiteRandom(cellX,cellZ,715)*.3)*spacing+driftX),(float)((cellZ+.35+geo.SiteRandom(cellX,cellZ,716)*.3)*spacing+driftZ));
                float highest=0;for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)highest=Mathf.Max(highest,geo.BaseElevationAt(anchor.x+x*wide,anchor.y+z*wide));
                float high=Mathf.Max(3500,highest+1500)+tall;
                long whole=(long)System.Math.Floor(tick);float fraction=(float)(tick-whole);var point=new GeoPoint(anchor.x,anchor.y);
                var weather=RegionalAtmosphere.Forecast(geo,whole,point);var next=RegionalAtmosphere.Forecast(geo,whole+1,point);
                float edge=Mathf.Min((float)(geo.Configuration.WidthMeters*.5)-Mathf.Abs(anchor.x),(float)(geo.Configuration.DepthMeters*.5)-Mathf.Abs(anchor.y));float lifetime=Mathf.SmoothStep(0,1,Mathf.Clamp01((edge+wide)/(wide*1.7f)));
                systems[i]=new Vector4(anchor.x,high,anchor.y,wide);float coverage=show&&i<count?Mathf.Clamp01((Mathf.Lerp(weather.Cloud,next.Cloud,fraction)-.12f)/.75f)*lifetime:0;
                shapes[i]=new Vector4(tall,coverage,Mathf.Lerp(weather.Precipitation,next.Precipitation,fraction),phase);objects[i].transform.position=WorldOrigin.Relative(new Vector3(systems[i].x,systems[i].y,systems[i].z));objects[i].transform.localScale=new Vector3(wide*1.65f,tall*1.65f,wide*1.65f);objects[i].GetComponent<MeshRenderer>().enabled=coverage>.001f;if(coverage>.001f)VisibleSystems++;
            }
            Shader.SetGlobalVectorArray("_AevoriaCloudSystems",systems);Shader.SetGlobalVectorArray("_AevoriaCloudShapes",shapes);Shader.SetGlobalFloat("_AevoriaRenderClouds",show?1:0);
        }
        private void OnDestroy(){Shader.SetGlobalFloat("_AevoriaRenderClouds",0);foreach(var obj in objects)if(obj!=null)Destroy(obj);if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);if(cloudNoise!=null)Destroy(cloudNoise);}
    }
}
