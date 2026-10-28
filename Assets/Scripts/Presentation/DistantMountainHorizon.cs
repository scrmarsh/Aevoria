using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    // Camera-relative angular scenery, never part of geography/collision/saves.
    public sealed class DistantMountainHorizon : MonoBehaviour
    {
        private ProceduralGeography geo;private Camera view;private readonly Mesh[] meshes=new Mesh[3];private readonly Material[] materials=new Material[3];private readonly GameObject[] layers=new GameObject[3];
        public void Initialize(ProceduralGeography geography,Camera camera)
        {
            geo=geography;view=camera;
            for(int layer=0;layer<3;layer++)
            {
                if(meshes[layer]!=null)Destroy(meshes[layer]);const int segments=512;var v=new Vector3[(segments+1)*2];var t=new int[segments*6];
                float radius=80000+layer*30000,phase=(float)geo.SiteRandom(layer,452,1232)*Mathf.PI*2;
                for(int i=0;i<=segments;i++)
                {
                    float a=i*Mathf.PI*2/segments,h=2500+Mathf.Pow(Mathf.Abs(Mathf.Sin(a*7+phase)*.55f+Mathf.Sin(a*19+phase)*.3f+Mathf.Sin(a*43)*.15f),1.6f)*7000;
                    v[i*2]=new Vector3(Mathf.Cos(a)*radius,geo.SeaLevel-25000,Mathf.Sin(a)*radius);v[i*2+1]=new Vector3(v[i*2].x,geo.SeaLevel+h-layer*300,v[i*2].z);
                    if(i<segments){int j=i*6,k=i*2;t[j]=k;t[j+1]=k+1;t[j+2]=k+2;t[j+3]=k+1;t[j+4]=k+3;t[j+5]=k+2;}
                }
                var mesh=meshes[layer]=new Mesh{name="Decorative distant mountain silhouette"};mesh.vertices=v;mesh.triangles=t;mesh.RecalculateBounds();
                if(layers[layer]==null){var obj=layers[layer]=new GameObject("Distant scenery layer "+layer);obj.transform.SetParent(transform,false);obj.AddComponent<MeshFilter>();var renderer=obj.AddComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;materials[layer]=new Material(Resources.Load<Material>("WorldHorizon"));renderer.sharedMaterial=materials[layer];}
                layers[layer].GetComponent<MeshFilter>().sharedMesh=mesh;materials[layer].SetColor("_Color",Color.Lerp(new Color(.20f,.29f,.35f),new Color(.45f,.57f,.66f),layer*.4f));
            }
        }
        private void LateUpdate()
        {
            if(view==null||geo==null)return;transform.position=new Vector3(view.transform.position.x,0,view.transform.position.z);
            float clearance=view.transform.position.y-geo.ElevationAt(WorldOrigin.Geographic(view.transform).x,WorldOrigin.Geographic(view.transform).z),fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2500,25000,clearance));
            var map=view.GetComponent<MapCamera>();if(map!=null&&map.isActiveAndEnabled)fade*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(20000,100000,map.Distance));
            for(int i=0;i<3;i++){layers[i].GetComponent<MeshRenderer>().enabled=fade>.001;materials[i].SetFloat("_Opacity",fade);}
        }
        private void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)if(material!=null)Destroy(material);}
    }
}
