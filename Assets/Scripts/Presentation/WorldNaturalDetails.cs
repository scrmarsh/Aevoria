using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    public sealed class WorldNaturalDetails : MonoBehaviour
    {
        private const int Limit=512,Kinds=8;private readonly Mesh[] meshes=new Mesh[Kinds];private readonly Matrix4x4[][] matrices=new Matrix4x4[Kinds][];private readonly int[] counts=new int[Kinds];
        private AevoriaBootstrap app;private Material material;private Task<List<NaturalSite>> pending;private float refresh;private int revision=-1;private List<NaturalSite> sites=new List<NaturalSite>();
        public int VisibleSites {get;private set;}
        private readonly Matrix4x4[] submerged=new Matrix4x4[32];private int submergedCount;private MaterialPropertyBlock submergedProperties;
        public int SubmergedRocks=>submergedCount;
        private Mesh crateMesh;private readonly Dictionary<long,GameObject> placed=new Dictionary<long,GameObject>();
        private readonly Dictionary<string,GameObject> boulders=new Dictionary<string,GameObject>();private float nextBoulderRefresh;
        public int BoulderColliders=>boulders.Count;
        public bool TryMineBoulder(Collider collider,out int amount)
        {amount=0;if(collider==null||!boulders.TryGetValue(collider.gameObject.name,out var body)||body!=collider.gameObject)return false;string id=collider.gameObject.name;amount=app.Living.Harvest(id,ResourceKind.Stone,60,ToolKind.Pickaxe,app.Clock.State.Tick);if(app.Living.Available(id,60,app.Clock.State.Tick,false)<1){body.SetActive(false);Destroy(body);boulders.Remove(id);}return true;}
        private static Mesh BoulderMesh(){var shape=new WorldShapes();shape.Boulder(new Vector3(0,.42f,0),new Vector3(1.2f,.65f,.9f),new Color(.29f,.30f,.28f),7);shape.Boulder(new Vector3(.65f,.25f,.4f),new Vector3(.7f,.5f,.65f),new Color(.34f,.33f,.29f),19);return shape.Build("Closed mineable boulder");}
        public IReadOnlyList<NaturalSite> Sites=>sites;
        public void Initialize(AevoriaBootstrap value)
        {
            foreach(var obj in boulders.Values){obj.SetActive(false);Destroy(obj);}boulders.Clear();nextBoulderRefresh=0;
            app=value;refresh=0;revision=-1;sites.Clear();submergedCount=0;submergedProperties=new MaterialPropertyBlock();submergedProperties.SetFloat("_EcologyAffects",0);if(pending!=null)pending.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);pending=null;
            foreach(var obj in placed.Values)Destroy(obj);placed.Clear();
            if(material==null){material=new Material(Resources.Load<Material>("WorldVegetation"));material.SetFloat("_Foliage",0);material.SetFloat("_EcologyAffects",1);}
            if(crateMesh==null){var crate=new WorldShapes();crate.Box(new Vector3(0,.3f,0),Vector3.one*.6f,new Color(.35f,.23f,.12f));for(int j=0;j<3;j++)crate.Box(new Vector3(-.21f+j*.21f,.3f,-.305f),new Vector3(.025f,.57f,.025f),new Color(.18f,.12f,.07f));crateMesh=crate.Build("Persistent test crate");}
            if(meshes[0]==null)for(int k=0;k<Kinds;k++)
            {
                var shape=new WorldShapes();var wood=new Color(.20f,.12f,.065f);var green=new Color(.12f,.24f,.07f);
                if(k==0){shape.Rod(new Vector3(-.7f,.08f,0),new Vector3(.8f,.12f,.1f),.045f,wood);shape.Rod(new Vector3(.2f,.1f,0),new Vector3(.5f,.2f,.4f),.026f,wood);}
                if(k==1){shape.Rod(new Vector3(-2,.25f,0),new Vector3(2,.25f,.15f),.28f,wood,10);shape.Rod(new Vector3(.2f,.3f,0),new Vector3(.6f,.65f,.8f),.1f,wood);shape.Crown(new Vector3(-1.8f,.23f,0),new Vector3(.19f,.23f,.25f),new Color(.42f,.31f,.15f));}
                if(k==3){shape.Rod(Vector3.zero,new Vector3(0,.6f,0),.07f,wood);for(int j=0;j<5;j++)shape.Crown(new Vector3(Mathf.Sin(j*2.4f)*.45f,.55f+j*.07f,Mathf.Cos(j*2.4f)*.4f),new Vector3(.5f,.4f,.45f),green,j);}
                if(k==4)for(int j=0;j<7;j++){var p=new Vector3(Mathf.Sin(j*2.4f)*.6f,.45f+j*.025f,Mathf.Cos(j*2.4f)*.6f);shape.Rod(new Vector3(p.x,0,p.z),p,.015f,green,4);shape.Crown(p,new Vector3(.07f,.035f,.07f),j%2==0?new Color(.68f,.54f,.14f):new Color(.42f,.22f,.52f),j);}
                if(k==5){shape.Rod(Vector3.zero,new Vector3(0,1.6f,0),.14f,new Color(.18f,.29f,.14f));shape.Rod(new Vector3(0,.7f,0),new Vector3(.45f,1.05f,0),.09f,green);shape.Rod(new Vector3(.45f,1.05f,0),new Vector3(.45f,1.45f,0),.09f,green);}
                if(k==6)for(int j=0;j<6;j++){var tip=new Vector3(Mathf.Sin(j*2.4f)*.65f,.45f+j*.035f,Mathf.Cos(j*2.4f)*.6f);shape.Rod(Vector3.zero,tip,.025f,wood,4);shape.Crown(tip,new Vector3(.32f,.19f,.25f),new Color(.24f,.25f,.12f),j);}
                if(k==7)for(int j=0;j<9;j++)shape.Rod(new Vector3(Mathf.Sin(j*2.4f)*.16f,0,Mathf.Cos(j*2.4f)*.16f),new Vector3(Mathf.Sin(j*2.4f)*.4f,.32f+j*.025f,Mathf.Cos(j*2.4f)*.4f),.014f,new Color(.35f,.31f,.17f),3);
                meshes[k]=k==2?BoulderMesh():shape.Build("Natural detail "+k);matrices[k]=new Matrix4x4[Limit];
            }
        }
        private void Update()
        {
            if(app==null||app.Living==null)return;
            if(Time.unscaledTime>=nextBoulderRefresh){nextBoulderRefresh=Time.unscaledTime+.25f;RefreshBoulders();}
            if(pending!=null&&pending.IsCompleted)
            {if(pending.IsFaulted)Debug.LogException(pending.Exception);else{sites=pending.Result;Rebuild();}pending=null;}
            if(revision!=app.Living.Revision){Rebuild();revision=app.Living.Revision;}
            if(pending==null&&Time.unscaledTime>=refresh&&app.Controller.Distance<=6000)
            {
                refresh=Time.unscaledTime+1;var focus=app.Controller.Focus;int cx=(int)Math.Floor(focus.x/32),cz=(int)Math.Floor(focus.z/32);var living=app.Living;
                pending=Task.Run(()=>{var result=new List<NaturalSite>(Limit);for(int r=0;r<=40&&result.Count<Limit;r++)for(int z=-r;z<=r&&result.Count<Limit;z++)for(int x=-r;x<=r&&result.Count<Limit;x++){if(Math.Max(Math.Abs(x),Math.Abs(z))!=r)continue;if(living.TryNatural(cx+x,cz+z,out var site))result.Add(site);}return result;});
            }
        }
        private void RefreshBoulders()
        {
            var wanted=new HashSet<string>();var centre=app.Living.Traveler.Exploring?new Vector3((float)app.Living.Traveler.X,(float)app.Living.Traveler.Y,(float)app.Living.Traveler.Z):app.Controller.Focus;
            void Install(string id,Matrix4x4 matrix,Mesh mesh)
            {
                if(mesh==null||wanted.Count>=64||app.Living.Available(id,60,app.Clock.State.Tick,false)<1)return;wanted.Add(id);
                if(!boulders.TryGetValue(id,out var obj)){obj=new GameObject(id);obj.transform.SetParent(transform,false);obj.AddComponent<MeshCollider>().sharedMesh=mesh;boulders.Add(id,obj);}
                obj.transform.SetPositionAndRotation(WorldOrigin.Relative(new Vector3(matrix.m03,matrix.m13,matrix.m23)),matrix.rotation);obj.transform.localScale=matrix.lossyScale;
            }
            if(app.Living.Traveler.Exploring||app.Controller.Distance<1500)
            {
                var geo=app.Terrain.Geography;
                foreach(var site in sites)if(site.Kind==NaturalKind.Boulder&&Vector3.Distance(centre,new Vector3((float)site.Position.X,site.Height,(float)site.Position.Y))<256)
                {double x=site.Position.X,z=site.Position.Y;float h=(float)LocalSolidField.RenderedGround(geo,x,z);var n=new Vector3(geo.ElevationAt(x-2,z)-geo.ElevationAt(x+2,z),4,geo.ElevationAt(x,z-2)-geo.ElevationAt(x,z+2)).normalized;Install(site.Id,Matrix4x4.TRS(new Vector3((float)x,h,(float)z),Quaternion.FromToRotation(Vector3.up,n)*Quaternion.Euler(0,site.Yaw,0),Vector3.one*site.Scale),meshes[2]);}
                if(app.Terrain.Vegetation!=null)foreach(var rock in app.Terrain.Vegetation.NearbyRocks(centre,256))Install(rock.Id,rock.Transform,app.Terrain.Vegetation.RockMesh);
            }
            var remove=new List<string>();foreach(var pair in boulders)if(!wanted.Contains(pair.Key)){pair.Value.SetActive(false);Destroy(pair.Value);remove.Add(pair.Key);}foreach(string id in remove)boulders.Remove(id);
        }
        private void Rebuild()
        {
            Array.Clear(counts,0,Kinds);VisibleSites=0;if(app.Living==null)return;
            foreach(var item in app.Living.Objects)if(!placed.ContainsKey(item.Id)){var obj=new GameObject("Placed test crate "+item.Id);obj.transform.SetParent(transform,false);obj.transform.SetPositionAndRotation(WorldOrigin.Relative(new Vector3((float)item.X,(float)item.Y,(float)item.Z)),Quaternion.Euler(0,(float)item.Yaw,0));obj.AddComponent<MeshFilter>().sharedMesh=crateMesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;var collider=obj.AddComponent<BoxCollider>();collider.center=Vector3.up*.3f;collider.size=Vector3.one*.6f;placed.Add(item.Id,obj);}
            foreach(var site in sites)
            {
                if(app.Living.Available(site.Id,site.Quantity,app.Clock.State.Tick,false)<1)continue;int kind=(int)site.Kind;if(kind==5)kind=site.Yaw<190?6:site.Yaw>315?7:5;if(counts[kind]>=Limit)continue;var geo=app.Terrain.Geography;double x=site.Position.X,z=site.Position.Y;
                float h=(float)LocalSolidField.RenderedGround(geo,x,z);var normal=new Vector3(geo.ElevationAt(x-2,z)-geo.ElevationAt(x+2,z),4,geo.ElevationAt(x,z-2)-geo.ElevationAt(x,z+2)).normalized;
                matrices[kind][counts[kind]++]=Matrix4x4.TRS(new Vector3((float)x,h,(float)z),Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,site.Yaw,0),Vector3.one*site.Scale);VisibleSites++;
            }
            submergedCount=0;var geography=app.Terrain.Geography;var focus=app.Controller.Focus;
            if(geography.ElevationAt(focus.x,focus.z)<geography.SeaLevel+20)
            {
                int cx=(int)Math.Floor(focus.x/16),cz=(int)Math.Floor(focus.z/16);
                for(int r=0;r<=16&&submergedCount<submerged.Length;r++)for(int z=-r;z<=r&&submergedCount<submerged.Length;z++)for(int x=-r;x<=r&&submergedCount<submerged.Length;x++)
                {
                    if(Math.Max(Math.Abs(x),Math.Abs(z))!=r)continue;int sx=cx+x,sz=cz+z;double roll=(geography.FieldNoise(sx*71,sz*73,1,1801)+1)*.5;if(roll>.12)continue;
                    double east=(sx+.5)*16,north=(sz+.5)*16;if(!geography.Configuration.Contains(new GeoPoint(east,north)))continue;float ground=(float)LocalSolidField.RenderedGround(geography,east,north),depth=geography.SeaLevel-ground;if(depth<.4||depth>12)continue;
                    var habitat=geography.AquaticAt(east,north);if(habitat==AquaticHabitat.None||habitat==AquaticHabitat.DeepOcean||habitat==AquaticHabitat.FreshwaterPlants)continue;
                    float scale=Mathf.Min(.6f+(float)roll*8,depth*.35f);submerged[submergedCount++]=Matrix4x4.TRS(new Vector3((float)east,ground-.15f,(float)north),Quaternion.Euler(0,(float)roll*3000,0),Vector3.one*scale);
                }
            }
        }
        private void LateUpdate()
        {
            if(app==null||app.Living==null||app.Menu.Page!=ApplicationPage.World||app.Controller.Distance>6000)return;
            for(int k=0;k<Kinds;k++)if(counts[k]>0)WorldOrigin.DrawInstances(new RenderParams(material){worldBounds=new Bounds(app.Controller.Focus,Vector3.one*6000),shadowCastingMode=ShadowCastingMode.On,receiveShadows=true},meshes[k],0,matrices[k],counts[k]);
            if(submergedCount>0)WorldOrigin.DrawInstances(new RenderParams(material){worldBounds=new Bounds(app.Controller.Focus,Vector3.one*1200),shadowCastingMode=ShadowCastingMode.Off,receiveShadows=true,matProps=submergedProperties},meshes[2],0,submerged,submergedCount);
        }
        private void OnDestroy(){if(pending!=null)pending.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);foreach(var obj in boulders.Values)if(obj!=null)Destroy(obj);foreach(var obj in placed.Values)if(obj!=null)Destroy(obj);foreach(var m in meshes)if(m!=null)Destroy(m);if(crateMesh!=null)Destroy(crateMesh);if(material!=null)Destroy(material);}
    }
}
