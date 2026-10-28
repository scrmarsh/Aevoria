using System;
using System.Collections.Generic;
using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aevoria.Presentation
{
    public sealed class SettlementPresentation:MonoBehaviour
    {
        private AevoriaBootstrap app;private Material material;
        private static SettlementPresentation fieldOwner;
        private readonly Mesh[] meshes=new Mesh[13];
        private readonly Matrix4x4[][] matrices=new Matrix4x4[13][];
        private readonly int[] counts=new int[13];
        private readonly Dictionary<string,GameObject> collision=new Dictionary<string,GameObject>();
        private readonly HashSet<string> wanted=new HashSet<string>();private readonly List<string> remove=new List<string>();
        private Texture2D occupation;private readonly Color32[] occupied=new Color32[256*256];
        private int revision=-1,originRevision=-1;private Vector3 lastFocus;private float refresh,lastRange=-1;
        private GameObject streetRoot;public SettlementPathPresentation Paths{get;private set;}public SettlementResidents Residents{get;private set;}
        public int VisibleBuildings{get;private set;}public int VisibleSettlements{get;private set;}public int ColliderCount=>collision.Count;
        public double LastUpdateMilliseconds{get;private set;}
        public void Initialize(AevoriaBootstrap value)
        {
            app=value;fieldOwner=this;material=new Material(Resources.Load<Material>("WorldVegetation")){name="Settlement plaster, timber and tile",enableInstancing=true};material.SetFloat("_Foliage",0);material.SetFloat("_EcologyAffects",0);
            streetRoot=new GameObject("Settlement streets and residents");streetRoot.transform.SetParent(transform,false);Paths=streetRoot.AddComponent<SettlementPathPresentation>();Paths.Initialize(value);Residents=streetRoot.AddComponent<SettlementResidents>();Residents.Initialize(value,Paths);
            for(int i=0;i<meshes.Length;i++){matrices[i]=new Matrix4x4[1023];meshes[i]=Build(i);}
            occupation=new Texture2D(256,256,TextureFormat.RGBA32,false,true){name="Occupied settlement construction footprints",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            revision=-1;refresh=0;
        }
        private static Mesh Build(int kind)
        {
            var s=new WorldShapes();Color plaster=new Color(.72f,.65f,.49f),wood=new Color(.24f,.13f,.065f),tile=new Color(.36f,.16f,.10f),stone=new Color(.39f,.39f,.34f),dark=new Color(.075f,.095f,.10f);
            if(kind>=7&&kind<=10){int buildings=kind==7?12:kind==8?24:kind==9?40:60;for(int i=0;i<buildings;i++){float angle=i*2.4f,r=5+Mathf.Sqrt(i)*6;var p=new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r);s.Box(p+Vector3.up*2,new Vector3(5,4,5),plaster);Roof(s,p,3,3,4,5.8f,tile);}return s.Build("Distant "+(SettlementStage)(kind-7));}
            if(kind==6)
            {
                s.Box(new Vector3(0,.06f,0),new Vector3(8,.12f,6),stone*.8f);
                for(int x=-1;x<=1;x+=2){var p=new Vector3(x*2.4f,0,0);for(int z=-1;z<=1;z+=2)s.Rod(p+new Vector3(-1,0,z*2.2f),p+new Vector3(-1,2.6f,z*2.2f),.1f,wood);s.Box(p+Vector3.up*.9f,new Vector3(1.7f,1.5f,4),wood);Roof(s,p,1.2f,2.4f,2.5f,3.1f,new Color(.56f,.43f,.24f));for(int z=0;z<3;z++)s.Box(p+new Vector3(0,1.8f,-1.3f+z*1.2f),new Vector3(.6f,.35f,.7f),new Color(.45f,.28f,.11f));}
                return s.Build("Covered market stalls");
            }
            int variant=kind==11?1:kind==12?2:0;if(kind>=11)kind=0;
            if(kind==3){s.Box(new Vector3(0,.08f,0),new Vector3(7,.16f,6),new Color(.26f,.19f,.09f));for(int i=-3;i<=3;i++)s.Box(new Vector3(i,.24f,0),new Vector3(.3f,.26f,5.7f),new Color(.37f,.42f,.15f));s.Rod(new Vector3(-3.5f,0,-3),new Vector3(-3.5f,1.3f,-3),.08f,wood);return s.Build("Cultivated settlement plot");}
            float width=kind==2?7:kind==1?6:kind==0?5+variant:5,height=kind==2?4:kind==1?3.6f:kind==0&&variant==2?5.6f:3,depth=kind==2?6:kind==0?5+variant:5;
            plaster=Color.Lerp(plaster,new Color(.54f,.46f,.33f),variant*.16f);tile=Color.Lerp(tile,new Color(.27f,.22f,.17f),variant*.23f);
            s.Box(new Vector3(0,-.55f,0),new Vector3(width+.35f,1.5f,depth+.35f),stone);
            s.Box(new Vector3(0,height*.5f+.15f,0),new Vector3(width,height,depth),plaster);
            Roof(s,Vector3.zero,width*.5f+.45f,depth*.5f+.45f,height+.2f,height+2.15f,tile);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)s.Box(new Vector3(x*(width*.5f+.025f),height*.5f+.1f,z*depth*.5f),new Vector3(.16f,height+.2f,.16f),wood);
            s.Box(new Vector3(0,1.12f,depth*.5f+.04f),new Vector3(1.2f,2.05f,.11f),wood);
            s.Box(new Vector3(.35f,1.05f,depth*.5f+.11f),new Vector3(.08f,.08f,.08f),new Color(.68f,.51f,.2f));
            for(int side=-1;side<=1;side+=2){s.Box(new Vector3(side*(width*.5f+.035f),height*.58f,0),new Vector3(.1f,.85f,1.1f),wood);s.Box(new Vector3(side*(width*.5f+.095f),height*.58f,0),new Vector3(.02f,.6f,.8f),dark);}
            s.Box(new Vector3(-width*.27f,height+1,0),new Vector3(.6f,2.1f,.65f),stone);
            if(kind==1||kind==5)for(int i=0;i<3;i++)s.Box(new Vector3(-1+i, .5f,depth*.5f+1),new Vector3(.8f,1,.8f),wood);
            if(kind==4){s.Box(new Vector3(width*.5f+1,.6f,0),new Vector3(1.5f,1.2f,2),wood);s.Rod(new Vector3(width*.5f+1,1.3f,-.5f),new Vector3(width*.5f+1,1.3f,.5f),.3f,stone);}
            if(kind==0&&variant==1){s.Box(new Vector3(width*.45f,1.2f,-1.3f),new Vector3(2.6f,2.4f,3),plaster);Roof(s,new Vector3(width*.45f,0,-1.3f),1.5f,1.8f,2.4f,3.5f,tile);}
            if(kind==0&&variant==2)for(int x=-1;x<=1;x+=2){s.Box(new Vector3(x*1.8f,4.2f,depth*.5f+.08f),new Vector3(.9f,1.1f,.14f),dark);s.Box(new Vector3(x*1.8f,4.2f,depth*.5f+.17f),new Vector3(.08f,1.2f,.08f),wood);}
            if(kind==2){for(int x=-1;x<=1;x+=2)s.Rod(new Vector3(x*2.6f,0,4.8f),new Vector3(x*2.6f,3.4f,4.8f),.16f,wood);Roof(s,new Vector3(0,0,4),3.2f,1.4f,3.2f,4.1f,tile);}
            return s.Build(((CivicBuildingKind)kind)+" variation "+variant);
        }
        private static void Roof(WorldShapes s,Vector3 p,float w,float d,float low,float high,Color color)
        {
            var a=p+new Vector3(-w,low,-d);var b=p+new Vector3(w,low,-d);var c=p+new Vector3(w,low,d);var e=p+new Vector3(-w,low,d);var f=p+new Vector3(0,high,-d);var g=p+new Vector3(0,high,d);
            s.Triangle(a,e,f,color);s.Triangle(e,g,f,color);s.Triangle(f,g,b,color*.9f);s.Triangle(g,c,b,color*.9f);s.Triangle(a,f,b,color*.7f);s.Triangle(e,c,g,color*.7f);
        }
        private void Update()
        {
            if(app?.Society==null)return;var focus=app.Living.Traveler.Exploring?new Vector3((float)app.Living.Traveler.X,(float)app.Living.Traveler.Y,(float)app.Living.Traveler.Z):app.Controller.Focus;
            if(revision!=app.Society.Revision||originRevision!=WorldOrigin.Revision||Time.unscaledTime>=refresh&&((focus-lastFocus).sqrMagnitude>64||Mathf.Abs(app.Controller.Distance-lastRange)>Mathf.Max(50,lastRange*.2f))){Refresh(focus);refresh=Time.unscaledTime+.35f;}
        }
        private void Refresh(Vector3 focus)
        {
            long at=System.Diagnostics.Stopwatch.GetTimestamp();Array.Clear(counts,0,counts.Length);Array.Clear(occupied,0,occupied.Length);VisibleBuildings=VisibleSettlements=0;wanted.Clear();
            float maskX=Mathf.Floor(focus.x/512)*512-4096,maskZ=Mathf.Floor(focus.z/512)*512-4096;
            foreach(var s in app.Society.Settlements)
            {
                double distance=Math.Sqrt((s.X-focus.x)*(s.X-focus.x)+(s.Z-focus.z)*(s.Z-focus.z));if(distance>22000||app.Controller.Distance>120000&&!app.Living.Traveler.Exploring)continue;VisibleSettlements++;
                if(distance>2500||!app.Living.Traveler.Exploring&&app.Controller.Distance>4500){Add(7+(int)s.Stage,s.X,s.Y,s.Z,0);continue;}
                foreach(var b in s.Buildings)
                {
                    int meshIndex=b.Kind==CivicBuildingKind.Dwelling&&b.Variant>0?10+b.Variant:(int)b.Kind;Add(meshIndex,b.X,b.Y,b.Z,b.Yaw);VisibleBuildings++;
                    int px=(int)((b.X-maskX)/8192*256),pz=(int)((b.Z-maskZ)/8192*256);for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)if(px+x>=0&&px+x<256&&pz+z>=0&&pz+z<256)occupied[(pz+z)*256+px+x]=new Color32(255,255,255,255);
                    if(distance>350||b.Kind==CivicBuildingKind.Farm)continue;wanted.Add(b.Id);
                    if(!collision.TryGetValue(b.Id,out var obj)){obj=new GameObject("Settlement structure "+b.Id);obj.transform.SetParent(transform,false);obj.AddComponent<MeshCollider>().sharedMesh=meshes[meshIndex];collision.Add(b.Id,obj);}
                    obj.transform.SetPositionAndRotation(Relative(b.X,b.Y,b.Z),Quaternion.Euler(0,(float)b.Yaw,0));
                }
            }
            remove.Clear();foreach(var pair in collision)if(!wanted.Contains(pair.Key))remove.Add(pair.Key);foreach(string id in remove){Destroy(collision[id]);collision.Remove(id);}
            occupation.SetPixels32(occupied);occupation.Apply(false,false);Shader.SetGlobalTexture("_AevoriaSettledFootprints",occupation);Shader.SetGlobalVector("_AevoriaSettledRect",new Vector4(maskX,maskZ,8192,8192));Shader.SetGlobalFloat("_AevoriaSettledReady",1);
            revision=app.Society.Revision;originRevision=WorldOrigin.Revision;lastFocus=focus;lastRange=app.Controller.Distance;LastUpdateMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-at)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        }
        private static Vector3 Relative(double x,double y,double z)=>new Vector3((float)(x-WorldOrigin.Offset.x),(float)y,(float)(z-WorldOrigin.Offset.z));
        private void Add(int kind,double x,double y,double z,double yaw){if(counts[kind]<1023)matrices[kind][counts[kind]++]=Matrix4x4.TRS(Relative(x,y,z),Quaternion.Euler(0,(float)yaw,0),Vector3.one);}
        private void LateUpdate()
        {
            if(app?.Society==null)return;if(originRevision!=WorldOrigin.Revision)Refresh(lastFocus);
            var bounds=new Bounds(Relative(lastFocus.x,lastFocus.y,lastFocus.z),new Vector3(50000,40000,50000));
            for(int i=0;i<meshes.Length;i++)if(counts[i]>0)Graphics.RenderMeshInstanced(new RenderParams(material){worldBounds=bounds,shadowCastingMode=ShadowCastingMode.On,receiveShadows=true},meshes[i],0,matrices[i],counts[i]);
        }
        private void OnDestroy(){if(streetRoot!=null){streetRoot.SetActive(false);Destroy(streetRoot);}if(fieldOwner==this){Shader.SetGlobalFloat("_AevoriaSettledReady",0);fieldOwner=null;}foreach(var obj in collision.Values)if(obj!=null)Destroy(obj);foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);if(occupation!=null)Destroy(occupation);}
    }
}
