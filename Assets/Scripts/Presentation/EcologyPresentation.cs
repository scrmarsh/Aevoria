using System;
using System.Collections.Generic;
using Aevoria.World;
using Aevoria.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Aevoria.Presentation
{
    // A bounded, disposable view of regional authority. No population mutation.
    public sealed class EcologyPresentation : MonoBehaviour
    {
        public const int AnimalLimit=128;
        private struct Animal {public int Region,Species,Id;public Vector3 Position,Home;public float Phase,Yaw;}
        private readonly Animal[] animals=new Animal[AnimalLimit];
        private readonly Animal[] previousAnimals=new Animal[AnimalLimit];
        private readonly Matrix4x4[][] matrices={new Matrix4x4[AnimalLimit],new Matrix4x4[AnimalLimit],new Matrix4x4[AnimalLimit],new Matrix4x4[AnimalLimit]};
        private readonly int[] counts=new int[4];
        private readonly Mesh[] meshes=new Mesh[4];
        private AevoriaBootstrap app;
        private ProceduralGeography geo;
        private Camera view;
        private Material material;
        private Texture2D ecologyTexture,mapTexture;
        private Color[] colors,mapColors;
        private float refreshAt,behaviorAt,mapAt;
        private int selected=-1,mapMode;
        private long mapStep=-1;
        private string summary="",history="";
        private Vector2 scroll;private bool showEvents;
        public int MapLayer {get=>mapMode;set{mapMode=Mathf.Clamp(value,0,5);mapStep=-1;}}
        private float historyHeight=150;
        private GUIStyle textStyle;
        private static readonly string[] modes={"Vegetation","Wildlife","Stress","Biome","Temperature","Moisture"};
        public bool Inspect {get;set;}
        private int animalCount;
        public int VisibleAnimals=>app!=null&&app.Controller.Distance<=3500&&app.Menu.Page==ApplicationPage.World?animalCount:0;
        public int SelectedRegion=>selected;
        public bool TryAnimalPosition(int species,out GeoPoint point)
        {for(int i=0;i<animalCount;i++)if(animals[i].Species==species){point=new GeoPoint(animals[i].Position.x,animals[i].Position.z);return true;}point=default;return false;}
        public static bool PointerBlocked {get;private set;}
        public void Initialize(AevoriaBootstrap bootstrap,ProceduralGeography geography,Camera camera)
        {
            app=bootstrap;geo=geography;view=camera;animalCount=0;selected=-1;refreshAt=behaviorAt=mapAt=0;mapStep=-1;
            if(material==null) {material=new Material(Resources.Load<Material>("WorldVegetation"));material.name="Wildlife procedural silhouettes";material.SetFloat("_Foliage",0);material.SetFloat("_EcologyAffects",0);}
            for(int s=0;s<4;s++)if(meshes[s]==null)meshes[s]=MakeAnimal(s);
            if(ecologyTexture!=null)Destroy(ecologyTexture);if(mapTexture!=null)Destroy(mapTexture);
            int n=app.Ecology.Resolution;ecologyTexture=new Texture2D(n,n,TextureFormat.RGBA32,false,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear,name="Regional ecology"};
            mapTexture=new Texture2D(n,n,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};colors=new Color[n*n];mapColors=new Color[n*n];
            UpdateTextures(true);Shader.SetGlobalTexture("_AevoriaEcology",ecologyTexture);Shader.SetGlobalFloat("_AevoriaEcologyReady",1);
            Shader.SetGlobalVector("_AevoriaEcologyRect",new Vector4((float)(-geo.Configuration.WidthMeters/2),(float)(-geo.Configuration.DepthMeters/2),(float)geo.Configuration.WidthMeters,(float)geo.Configuration.DepthMeters));
        }
        private static double Unit(int region,int slot,int channel)
        {unchecked {uint h=(uint)(region*747796405+slot*2891336453L+channel*277803737L);h=(h^(h>>16))*2246822519U;h=(h^(h>>13))*3266489917U;return (h^(h>>16))/4294967296.0;}}
        private bool DryLand(Vector3 p,int region)
        {if(app.Ecology.RegionAt(new GeoPoint(p.x,p.z))!=region)return false;float h=geo.ElevationAt(p.x,p.z);return h>geo.SeaLevel+3&&!geo.Hydrology.IsLakeWater(p.x,p.z,h)&&!geo.Hydrology.IsRiverWater(p.x,p.z,h)&&Math.Abs(h-geo.ElevationAt(p.x+3,p.z))<2.5;}
        private void SelectAnimals()
        {
            int oldCount=animalCount;Array.Copy(animals,previousAnimals,oldCount);
            animalCount=0;if(app.Controller.Distance>3500)return;
            var focus=app.Controller.Focus;int current=app.Ecology.RegionAt(new GeoPoint(focus.x,focus.z));if(current<0)return;
            double side=geo.Configuration.WidthMeters/app.Ecology.Resolution;
            for(int ring=0;ring<2&&animalCount<AnimalLimit;ring++)
            {
                int regions=ring==0?1:app.Ecology.NeighborCount(current);
                for(int edge=0;edge<regions&&animalCount<AnimalLimit;edge++)
                {
                    int region=ring==0?current:app.Ecology.Neighbor(current,edge);var environment=app.Ecology.EnvironmentAt(region);if(!environment.Land)continue;
                    for(int s=0;s<4&&animalCount<AnimalLimit;s++)
                    {
                        double population=app.Ecology.Population(region,s);
                        // A sparse predator proxy may represent a fractional regional
                        // density; it is never an additional authoritative animal.
                        int count=Math.Min(s==3?8:40,s==3&&population>=.1?Math.Max(1,(int)Math.Floor(population)):(int)Math.Floor(population));
                        for(int j=0;j<count&&animalCount<AnimalLimit;j++)
                        {
                            // Stable regional groups and identities; independent of camera position.
                            float x=(float)(environment.Center.X+(Unit(region,s,3)-.5)*side*.6+(Unit(region,j+s*71,8)-.5)*100);
                            float z=(float)(environment.Center.Y+(Unit(region,s,4)-.5)*side*.6+(Unit(region,j+s*71,9)-.5)*100);
                            var p=new Vector3(x,0,z);if(!DryLand(p,region)||Vector3.Distance(new Vector3(focus.x,0,focus.z),p)>1800)continue;
                            p.y=geo.ElevationAt(x,z);var animal=new Animal {Region=region,Species=s,Id=j,Position=p,Home=p,Phase=(float)(Unit(region,j+s*71,11)*6.28),Yaw=(float)(Unit(region,j,12)*360)};
                            for(int old=0;old<oldCount;old++)if(previousAnimals[old].Region==region&&previousAnimals[old].Species==s&&previousAnimals[old].Id==j){animal=previousAnimals[old];break;}animals[animalCount++]=animal;
                        }
                    }
                }
            }
        }
        private int lastRegion=-2;
        private long animalsStep=-1;
        private bool detailed;
        private void Update()
        {
            if(app==null||app.Ecology==null)return;
            bool playing=app.Menu.Page==ApplicationPage.World;
            if(playing&&GameInput.Pressed(GameAction.Ecosystems))Inspect=!Inspect;
            var pointer=Mouse.current?.position.ReadValue()??Vector2.zero;
            PointerBlocked=playing&&Inspect&&new Rect(15,95,1000,610).Contains(new Vector2(pointer.x/Screen.width*1280,(Screen.height-pointer.y)/Screen.height*720));
            if(!playing)return;
            if(Time.unscaledTime>=refreshAt)
            {
                refreshAt=Time.unscaledTime+.25f;UpdateTextures(false);
                int region=app.Ecology.RegionAt(new GeoPoint(app.Controller.Focus.x,app.Controller.Focus.z));selected=region;
                bool nextDetailed=app.Controller.Distance<=3500;
                if(region!=lastRegion||animalsStep!=app.Ecology.Steps||detailed!=nextDetailed){SelectAnimals();lastRegion=region;animalsStep=app.Ecology.Steps;detailed=nextDetailed;}
                if(Inspect&&region>=0)RefreshSummary(region);
            }
            if(Time.unscaledTime>=behaviorAt)
            {
                behaviorAt=Time.unscaledTime+.2f;
                for(int i=0;i<animalCount;i++)
                {
                    var a=animals[i];float t=Time.time*.12f+a.Phase;
                    Vector3 target=a.Home+new Vector3(Mathf.Sin(t)*24,0,Mathf.Cos(t*.83f)*24);
                    // Local pursuit / avoidance affects silhouettes only.
                    for(int j=0;j<animalCount;j++)if(animals[j].Species!=a.Species&&((a.Species==3&&animals[j].Species<3)||(a.Species<3&&animals[j].Species==3)))
                    {Vector3 d=animals[j].Position-a.Position;if(d.sqrMagnitude<1600){target=a.Position+(a.Species==3?d:-d);break;}}
                    Vector3 delta=target-a.Position;delta.y=0;bool resting=Mathf.Sin(Time.time*.18f+a.Phase)>.65;
                    var next=a.Position+Vector3.ClampMagnitude(delta,resting?0:a.Species==3?.65f:.38f);
                    if(DryLand(next,a.Region)){next.y=geo.ElevationAt(next.x,next.z);a.Position=next;if(delta.sqrMagnitude>.01)a.Yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;}
                    animals[i]=a;
                }
            }
        }
        private void LateUpdate()
        {
            if(app==null||app.Ecology==null||app.Menu.Page!=ApplicationPage.World||app.Controller.Distance>3500)return;
            Array.Clear(counts,0,counts.Length);
            for(int i=0;i<animalCount;i++) {var a=animals[i];float scale=a.Species==2?.7f:a.Species==3?1.1f:1.5f;matrices[a.Species][counts[a.Species]++]=Matrix4x4.TRS(a.Position,Quaternion.Euler(0,a.Yaw,0),Vector3.one*scale);}
            for(int s=0;s<4;s++)if(counts[s]>0)WorldOrigin.DrawInstances(new RenderParams(material){camera=view,worldBounds=new Bounds(app.Controller.Focus,Vector3.one*8000),shadowCastingMode=ShadowCastingMode.On,receiveShadows=true},meshes[s],0,matrices[s],counts[s]);
        }
        private void UpdateTextures(bool initial)
        {
            var eco=app.Ecology;
            for(int i=0;i<colors.Length;i++)
            {var target=new Color((float)Math.Min(1,eco.Forest(i)*5),(float)eco.Biomass(i),(float)eco.Burn(i),(float)eco.Moisture(i));colors[i]=initial?target:Color.Lerp(colors[i],target,.08f);}
            ecologyTexture.SetPixels(colors);ecologyTexture.Apply(false);
            if(mapStep==eco.Steps&&Time.unscaledTime<mapAt)return;mapStep=eco.Steps;mapAt=Time.unscaledTime+1;
            for(int i=0;i<mapColors.Length;i++)
            {
                var e=eco.EnvironmentAt(i);float density=(float)((eco.Population(i,0)+eco.Population(i,1)+eco.Population(i,2))/Math.Max(.001,e.Area)/80),v=mapMode==0?(float)eco.Biomass(i):mapMode==1?density:mapMode==2?(float)eco.Stress(i):(float)eco.Moisture(i);
                mapColors[i]=mapMode==4?EnvironmentPalette.Temperature((float)eco.Temperature(i,app.Clock.State.Tick)):mapMode==3?EnvironmentPalette.BiomeColor(e.Biome):!e.Land?EnvironmentPalette.BiomeColor(Biome.Ocean):mapMode==5?Color.Lerp(new Color(.76f,.55f,.29f),new Color(.05f,.30f,.72f),v):mapMode==2?(v<.5f?Color.Lerp(new Color(.15f,.55f,.22f),Color.yellow,v*2):Color.Lerp(Color.yellow,new Color(.85f,.08f,.04f),(v-.5f)*2)):Color.Lerp(new Color(.70f,.70f,.47f),new Color(.035f,.36f,.13f),Mathf.Clamp01(v));
            }            mapTexture.SetPixels(mapColors);mapTexture.Apply(false);
        }
        private void RefreshSummary(int i)
        {
            var eco=app.Ecology;var e=eco.EnvironmentAt(i);var r=eco.Region(i);var date=new Aevoria.Simulation.WorldCalendar(app.Clock.State.Tick);
            summary=$"REGION {i} · {e.Biome}\nYear {date.Year}, day {date.Day} · {eco.Season(i,app.Clock.State.Tick)}\nTemperature {UnitFormatting.Temperature(eco.Temperature(i,app.Clock.State.Tick),app.Settings.Units)} · moisture {r.Moisture:P0}\nBiomass {eco.Biomass(i):P0} · forest {eco.Forest(i):P0}\nGrass {r.Plants[0]:P0} · shrubs {r.Plants[1]+r.Plants[4]:P0}\nForest age {r.ForestAge:0.0} y · burn scar {r.Burn:P0}\nMeadow deer {r.Animals[0]:0.0} · woodland deer {r.Animals[1]:0.0}\nHares {r.Animals[2]:0.0} · wolves {r.Animals[3]:0.0}\nHabitat {eco.HabitatSuitability(i,0):P0} · stress {r.Stress:P0}\nDisturbance {r.Event} · severity {r.Severity:P0}\nEcology {eco.Count} regions · month {eco.Steps}\nLast step {eco.LastStepMilliseconds:0.00} ms · visible animals {VisibleAnimals}\nFocus selects region · ecology updates every 30 days";
            history="";for(int h=eco.HistoryCount-1;h>=Math.Max(0,eco.HistoryCount-6);h--){var ev=eco.HistoryAt(h);history+=$"#{ev.Id} · year {new Aevoria.Simulation.WorldCalendar(ev.Tick).Year} · region {ev.Region} · {ev.Kind}\n{ev.Outcome} · biomass loss {ev.BiomassLost:P0}\n";}
            historyHeight=Math.Max(150,Math.Min(6,eco.HistoryCount)*54);
        }
        private void OnGUI()
        {
            if(app==null||app.Menu.Page!=ApplicationPage.World||app.Navigation?.Open==true||app.Explorer?.InventoryOpen==true)return;
            if(!Inspect)return;var previous=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            GUI.Box(new Rect(15,95,1000,610),"");if(textStyle==null)textStyle=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
            GUI.Label(new Rect(35,103,700,26),"F4 · ecosystem and climate inspection",textStyle);
            int next=GUI.SelectionGrid(new Rect(35,137,950,44),mapMode,modes,6);if(next!=mapMode)MapLayer=next;
            GUI.DrawTexture(new Rect(35,195,440,440),mapTexture);
            if(selected>=0){int n=app.Ecology.Resolution;GUI.Box(new Rect(35+(selected%n+.5f)*440/n-4,195+(n-1-selected/n+.5f)*440/n-4,8,8),"");}
            GUI.Label(new Rect(500,195,490,280),selected<0?"Focus inside the world to inspect a region.":summary,textStyle);
            if(GUI.Button(new Rect(500,480,235,30),"Layer legend"))showEvents=false;if(GUI.Button(new Rect(750,480,235,30),"Environmental events"))showEvents=true;
            if(showEvents){scroll=GUI.BeginScrollView(new Rect(500,520,490,145),scroll,new Rect(0,0,460,historyHeight));GUI.Label(new Rect(0,0,455,historyHeight),history,textStyle);GUI.EndScrollView();}
            else if(mapMode==3){scroll=GUI.BeginScrollView(new Rect(500,520,490,145),scroll,new Rect(0,0,460,18*23));foreach(Biome biome in Enum.GetValues(typeof(Biome))){int index=(int)biome;var color=GUI.color;GUI.color=EnvironmentPalette.BiomeColor(biome);GUI.DrawTexture(new Rect(0,index*23,20,18),Texture2D.whiteTexture);GUI.color=color;GUI.Label(new Rect(30,index*23,400,22),biome.ToString(),textStyle);}GUI.EndScrollView();}
            else GUI.Label(new Rect(500,530,480,125),mapMode==4?$"Temperature\nBlue: {UnitFormatting.Temperature(-40,app.Settings.Units,"0")} · pale: {UnitFormatting.Temperature(0,app.Settings.Units,"0")} · red: {UnitFormatting.Temperature(40,app.Settings.Units,"0")}\nActual regional seasonal temperature.":mapMode==5?"Moisture · 0–100%\nSand: dry · blue: wet":mapMode==2?"Habitat stress · 0–100%\nGreen: low · yellow: moderate · red: high":mapMode==1?$"Wildlife · terrestrial animals\nPale: 0 · dark green: {UnitFormatting.Density(80,app.Settings.Units)}+":"Vegetation biomass · 0–100%\nPale: sparse · dark green: dense",textStyle);
            GUI.Label(new Rect(35,648,450,35),"North ↑ · focus selects a region · values persist off camera",textStyle);GUI.matrix=previous;
        }        private static Mesh MakeAnimal(int species)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
            Color fur=species==2?new Color(.36f,.29f,.20f):species==3?new Color(.28f,.30f,.29f):new Color(.43f,.28f,.14f);
            Box(vertices,triangles,colors,new Vector3(0,.85f,0),new Vector3(.5f,.55f,1.1f),fur);
            Box(vertices,triangles,colors,new Vector3(0,species==3?.95f:1.25f,.65f),new Vector3(.32f,.38f,.45f),fur*1.12f);
            for(int k=0;k<4;k++)Box(vertices,triangles,colors,new Vector3((k%2==0?-.18f:.18f),.35f,k<2?-.38f:.38f),new Vector3(.1f,.7f,.12f),fur*.65f);
            if(species==2)for(int k=0;k<2;k++)Box(vertices,triangles,colors,new Vector3(k==0?-.11f:.11f,1.7f,.65f),new Vector3(.09f,.65f,.12f),fur);
            else if(species==3){Box(vertices,triangles,colors,new Vector3(0,1,.95f),new Vector3(.22f,.18f,.45f),fur*.8f);Box(vertices,triangles,colors,new Vector3(0,.85f,-.85f),new Vector3(.16f,.16f,.7f),fur);}
            else for(int k=0;k<2;k++){Box(vertices,triangles,colors,new Vector3(k==0?-.2f:.2f,1.7f,.6f),new Vector3(.05f,.55f,.05f),new Color(.6f,.5f,.32f));Box(vertices,triangles,colors,new Vector3(k==0?-.27f:.27f,1.85f,.6f),new Vector3(.2f,.04f,.05f),new Color(.6f,.5f,.32f));}
            var mesh=new Mesh{name="Procedural "+EcologyModel.Species(species).Name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void Box(List<Vector3> v,List<int> t,List<Color> c,Vector3 center,Vector3 size,Color color)
        {
            int start=v.Count;for(int k=0;k<8;k++){v.Add(center+Vector3.Scale(new Vector3((k&1)==0?-.5f:.5f,(k&2)==0?-.5f:.5f,(k&4)==0?-.5f:.5f),size));c.Add(color);}
            int[] indices={0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};foreach(int i in indices)t.Add(start+i);
        }
        private void OnDisable(){PointerBlocked=false;}
        private void OnDestroy(){if(Shader.GetGlobalTexture("_AevoriaEcology")==ecologyTexture){Shader.SetGlobalFloat("_AevoriaEcologyReady",0);Shader.SetGlobalTexture("_AevoriaEcology",Texture2D.blackTexture);}if(material!=null)Destroy(material);foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);if(ecologyTexture!=null)Destroy(ecologyTexture);if(mapTexture!=null)Destroy(mapTexture);}
    }
}
