using System.Collections.Generic;
using Aevoria.World;
using UnityEngine;
namespace Aevoria.Presentation
{
    public sealed class ExcavationTarget:MonoBehaviour
    {
        private const int Segments=64;
        private AevoriaBootstrap app;private ExperimentalExplorer explorer;private GameObject visual;private Mesh mesh;
        private readonly Vector3[] points=new Vector3[Segments],normals=new Vector3[Segments];private readonly bool[] contour=new bool[Segments];
        private readonly List<Vector3> vertices=new List<Vector3>();private readonly List<Color> colors=new List<Color>();private readonly List<int> indices=new List<int>();
        private Vector3 lastPoint,lastNormal;private int lastRevision=-1;private bool lastValid;private ToolKind lastTool;private double lastRadius;
        public bool Valid{get;private set;}public bool Visible=>visual!=null&&visual.activeSelf;public double Radius{get;private set;}public SolidPoint Centre{get;private set;}
        public void Initialize(AevoriaBootstrap application,ExperimentalExplorer actor)
        {
            app=application;explorer=actor;lastRevision=-1;
            if(visual==null){visual=new GameObject("Surface excavation radius");visual.transform.SetParent(transform,false);mesh=new Mesh{name="Surface-conforming excavation indicator"};mesh.MarkDynamic();visual.AddComponent<MeshFilter>().sharedMesh=mesh;visual.AddComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("ExcavationTarget");visual.SetActive(false);}
        }
        private void LateUpdate()=>Refresh();
        private bool Project(Vector3 guess,Vector3 normal,float reach,out RaycastHit hit)
            =>Physics.Raycast(new Ray(guess+normal*reach,-normal),out hit,reach*2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&explorer.IsTerrainCollider(hit.collider);
        public void Refresh()
        {
            if(app?.Living==null||explorer==null||!explorer.AimingAvailable||app.Living.Traveler.Tool!=ToolKind.Shovel&&app.Living.Traveler.Tool!=ToolKind.Pickaxe||!Physics.Raycast(explorer.AimRay,out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){if(visual!=null)visual.SetActive(false);return;}
            var material=explorer.DigMaterial(hit);Radius=LivingWorldState.ExcavationRadius(material);Centre=new SolidPoint((double)hit.point.x+WorldOrigin.Offset.x,hit.point.y,(double)hit.point.z+WorldOrigin.Offset.z);
            Valid=explorer.IsTerrainCollider(hit.collider)&&app.Living.CanDig(Centre.X,Centre.Y,Centre.Z,material,out _);var tool=app.Living.Traveler.Tool;
            if(visual.activeSelf&&Vector3.SqrMagnitude(lastPoint-hit.point)<.000009f&&Vector3.Dot(lastNormal,hit.normal)>.9999f&&lastRevision==app.Living.ExcavationRevision&&lastValid==Valid&&lastTool==tool&&lastRadius==Radius)return;
            lastPoint=hit.point;lastNormal=hit.normal;lastRevision=app.Living.ExcavationRevision;lastValid=Valid;lastTool=tool;lastRadius=Radius;visual.transform.position=hit.point;visual.SetActive(true);
            Vector3 tangent=Vector3.Cross(hit.normal,Mathf.Abs(hit.normal.y)<.8f?Vector3.up:Vector3.right).normalized,bitangent=Vector3.Cross(hit.normal,tangent);float radius=(float)Radius,reach=Mathf.Max(1.2f,radius*1.3f);
            for(int i=0;i<Segments;i++)
            {
                float angle=i*Mathf.PI*2/Segments;Vector3 direction=tangent*Mathf.Cos(angle)+bitangent*Mathf.Sin(angle);contour[i]=false;
                if(!Project(hit.point+direction*radius,hit.normal,reach,out var surface))continue;
                float low=0,high=radius;
                // Intersection of the real surface and the actual excavation
                // sphere, rather than a floating projected tangent-plane circle.
                for(int step=0;step<8;step++){float mid=(low+high)*.5f;if(!Project(hit.point+direction*mid,hit.normal,reach,out var probe)){high=mid;continue;}surface=probe;if(Vector3.SqrMagnitude(probe.point-hit.point)>radius*radius)high=mid;else low=mid;}
                if(!Project(hit.point+direction*((low+high)*.5f),hit.normal,reach,out surface))continue;points[i]=surface.point-hit.point;normals[i]=surface.normal;contour[i]=true;
            }
            vertices.Clear();colors.Clear();indices.Clear();Stroke(.038f,new Color(.015f,.025f,.03f,.90f),.014f);Stroke(.018f,Valid?new Color(.05f,.95f,1,.98f):new Color(1,.20f,.05f,.98f),.016f);
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
        }
        private void Stroke(float width,Color color,float offset)
        {
            for(int i=0;i<Segments;i++)
            {
                int j=(i+1)%Segments;if(!contour[i]||!contour[j])continue;Vector3 direction=(points[j]-points[i]).normalized,a=Vector3.Cross(normals[i],direction).normalized*width*.5f,b=Vector3.Cross(normals[j],direction).normalized*width*.5f;
                int n=vertices.Count;vertices.Add(points[i]+normals[i]*offset-a);vertices.Add(points[i]+normals[i]*offset+a);vertices.Add(points[j]+normals[j]*offset+b);vertices.Add(points[j]+normals[j]*offset-b);for(int k=0;k<4;k++)colors.Add(color);indices.Add(n);indices.Add(n+1);indices.Add(n+2);indices.Add(n);indices.Add(n+2);indices.Add(n+3);
            }
        }
        private void OnDestroy(){if(visual!=null)Destroy(visual);if(mesh!=null)Destroy(mesh);}
    }
}
