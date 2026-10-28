using System;
using System.Collections.Generic;
using Aevoria.Simulation;
using Aevoria.World;
using Aevoria.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aevoria.Presentation
{
    public enum SocietyOverlayMode { Off,Political,Population,Resources }
    public sealed class SocietyObservation:MonoBehaviour
    {
        private AevoriaBootstrap app;private Texture2D overlay;private Color32[] pixels;private int revision=-1,resource;
        private static SocietyObservation owner;
        private SocietyOverlayMode mode;
        private string civilizationId,settlementId;private Vector2 scroll;private bool history;
        private int historyPage=-1;private IReadOnlyList<SocialHistoryEvent> loadedPage;
        private GUIStyle title,body,small;private string error="";
        private static readonly Rect PanelRect=new Rect(14,124,370,535),ButtonRect=new Rect(14,86,160,29);
        private static readonly string[] resourceNames={"Food-growing potential","Timber potential","Stone potential","Geological ore potential"};
        public static bool PointerBlocked{get;private set;}
        public bool Open{get;set;}
        public bool ShowMarkers{get;set;}=true;
        public SocietyOverlayMode OverlayMode=>mode;
        public string SelectedCivilizationId=>civilizationId;
        public string SelectedSettlementId=>settlementId;
        public int PaintedRevision{get;private set;}=-1;
        public void Initialize(AevoriaBootstrap value)
        {
            app=value;owner=this;int n=app.Society.TerritoryResolution;overlay=new Texture2D(n,n,TextureFormat.RGBA32,false,true){name="Authoritative civilization overlay",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};pixels=new Color32[n*n];Paint();
        }
        public void SetOverlay(SocietyOverlayMode value){mode=value;revision=-1;Paint();}
        public void SelectCivilization(string id){if(app.Society.Civilization(id)==null)return;civilizationId=id;settlementId=null;Open=true;history=false;scroll=Vector2.zero;}
        public void SelectSettlement(string id){var s=app.Society.Settlement(id);if(s==null)return;civilizationId=s.CivilizationId;settlementId=id;Open=true;history=false;scroll=Vector2.zero;}
        public void FocusSettlement(string id){var s=app.Society.Settlement(id);if(s==null)return;SelectSettlement(id);app.Controller.SetView(new GeoPoint(s.X,s.Z),25,45,300);}
        public void ShowHistory(){Open=true;history=true;historyPage=-1;loadedPage=null;scroll=Vector2.zero;}
        private bool Active=>app?.Society!=null&&app.Menu.Page==ApplicationPage.World&&app.Living.Traveler.Exploring==false&&app.Navigation?.Open!=true;
        private void Update()
        {
            if(!Active){PointerBlocked=false;Shader.SetGlobalFloat("_AevoriaSocietyEnabled",0);return;}
            var mouse=Mouse.current;Vector2 p=mouse==null?Vector2.zero:mouse.position.ReadValue();p=new Vector2(p.x/Screen.width*1280,(Screen.height-p.y)/Screen.height*720);
            PointerBlocked=Open&&(ButtonRect.Contains(p)||PanelRect.Contains(p)||new Rect(399,119,500,29).Contains(p));
            if(revision!=app.Society.Revision)Paint();Shader.SetGlobalFloat("_AevoriaSocietyEnabled",mode==SocietyOverlayMode.Off?0:1);
            if(Application.isFocused&&!app.ExternalControl&&!GameInput.Blocked&&mouse?.leftButton.wasPressedThisFrame==true&&!PointerBlocked&&!ApplicationMenu.PointerBlocked&&!EcologyPresentation.PointerBlocked)
            {
                var ray=CameraRays.Screen(Camera.main,mouse.position.ReadValue());if(PickTerrain(ray,out var hit))
                {int selected=app.Society.OwnerAt(hit.X,hit.Y);if(selected>=0)SelectCivilization(app.Society.Civilizations[selected].Id);}
            }
        }
        private bool PickTerrain(Ray ray,out GeoPoint hit)
        {
            hit=default;if(ray.direction.y>=-.001f)return false;var geo=app.Terrain.Geography;
            double end=Math.Min(Camera.main.farClipPlane,(geo.SeaLevel-12000-ray.origin.y)/ray.direction.y),previous=0;
            for(int step=1;step<=128;step++)
            {
                double t=end*step/128,x=ray.origin.x+ray.direction.x*t,z=ray.origin.z+ray.direction.z*t;
                if(!geo.Configuration.Contains(new GeoPoint(x,z))){previous=t;continue;}
                if(ray.origin.y+ray.direction.y*t<=geo.ElevationAt(x,z))
                {
                    double low=previous,high=t;for(int i=0;i<24;i++){double mid=(low+high)*.5,east=ray.origin.x+ray.direction.x*mid,north=ray.origin.z+ray.direction.z*mid;if(ray.origin.y+ray.direction.y*mid>geo.ElevationAt(east,north))low=mid;else high=mid;}double at=(low+high)*.5;hit=new GeoPoint(ray.origin.x+ray.direction.x*at,ray.origin.z+ray.direction.z*at);return geo.Configuration.Contains(hit);
                }
                previous=t;
            }
            return false;
        }
        private void Paint()
        {
            if(app?.Society==null||overlay==null)return;int n=app.Society.TerritoryResolution;
            for(int i=0;i<pixels.Length;i++)
            {
                int own=app.Society.OwnerCell(i);Color color=Color.clear;
                if(mode==SocietyOverlayMode.Political&&own>=0){var c=app.Society.Civilizations[own];color=new Color((float)c.Red,(float)c.Green,(float)c.Blue,.39f);int x=i%n,z=i/n;bool border=x==0||z==0||x==n-1||z==n-1||app.Society.OwnerCell(i-1)!=own||app.Society.OwnerCell(i+1)!=own||app.Society.OwnerCell(i-n)!=own||app.Society.OwnerCell(i+n)!=own;if(border)color.a=.58f;}
                else if(mode==SocietyOverlayMode.Population&&own>=0){float amount=(float)CivilIdentity.Clamp(Math.Log(1+app.Society.CellPopulationDensity(i))/Math.Log(101));color=Color.Lerp(new Color(.28f,.52f,.28f),new Color(.9f,.26f,.1f),amount);color.a=.45f;}
                else if(mode==SocietyOverlayMode.Resources){float amount=(float)app.Society.CellResourcePotential(i,resource);color=Color.Lerp(new Color(.23f,.32f,.3f),new Color(.89f,.65f,.2f),amount);color.a=amount>0?.45f:0;}
                pixels[i]=color;
            }
            overlay.SetPixels32(pixels);overlay.Apply(false,false);Shader.SetGlobalTexture("_AevoriaSocietyOverlay",overlay);float width=(float)app.Terrain.Geography.Configuration.WidthMeters,depth=(float)app.Terrain.Geography.Configuration.DepthMeters;Shader.SetGlobalVector("_AevoriaSocietyRect",new Vector4(-width*.5f,-depth*.5f,width,depth));revision=app.Society.Revision;PaintedRevision=revision;
        }
        private void Styles(){if(title!=null)return;title=new GUIStyle(GUI.skin.label){fontSize=19,fontStyle=FontStyle.Bold,wordWrap=true};body=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};small=new GUIStyle(GUI.skin.label){fontSize=12,wordWrap=true};}
        private static void Back(Rect r){var old=GUI.color;GUI.color=new Color(.025f,.045f,.055f,.94f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        private void OnGUI()
        {
            if(!Active)return;Styles();var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            if(Open&&GUI.Button(ButtonRect,"Close")){Open=!Open;scroll=Vector2.zero;}
            if(Open&&mode==SocietyOverlayMode.Population)GUI.Label(new Rect(400,119,500,25),app.Settings.Units==UnitSystem.Imperial?"Population per mi² — mean across each polity's controlled cells":"Population per km² — mean across each polity's controlled cells",small);
            if(Open&&mode==SocietyOverlayMode.Resources&&GUI.Button(new Rect(400,119,330,25),resourceNames[resource]+" · change")){resource=(resource+1)%4;Paint();}
            if(ShowMarkers)Markers();if(Open){Back(PanelRect);if(history)HistoryPanel();else if(settlementId!=null)SettlementPanel();else if(civilizationId!=null)CivilizationPanel();else OverviewPanel();}GUI.matrix=old;
        }
        private void Markers()
        {
            foreach(var s in app.Society.Settlements)
            {
                Vector3 local=new Vector3((float)(s.X-WorldOrigin.Offset.x),(float)s.Y+10,(float)(s.Z-WorldOrigin.Offset.z));var p=Camera.main.WorldToScreenPoint(local);if(p.z<=0)continue;var point=new Vector2(p.x/Screen.width*1280,(Screen.height-p.y)/Screen.height*720);if(point.x<0||point.x>1280||point.y<120||point.y>675||Open&&PanelRect.Contains(point))continue;var c=app.Society.Civilization(s.CivilizationId);var old=GUI.backgroundColor;GUI.backgroundColor=new Color((float)c.Red,(float)c.Green,(float)c.Blue);if(GUI.Button(new Rect(point.x-8,point.y-8,18,18),"◆"))SelectSettlement(s.Id);GUI.backgroundColor=old;if(app.Controller.Distance<100000)GUI.Label(new Rect(point.x+13,point.y-10,200,23),s.Name,small);
            }
        }
        private void Header(string text){GUI.Label(new Rect(28,137,340,50),text,title);if(GUI.Button(new Rect(28,193,160,27),"All civilizations")){civilizationId=settlementId=null;history=false;scroll=Vector2.zero;}if(GUI.Button(new Rect(196,193,170,27),"History"))ShowHistory();}
        private void OverviewPanel()
        {
            Header("The inhabited world");GUI.Label(new Rect(28,233,338,44),$"{app.Society.Civilizations.Count} civilizations · {app.Society.Settlements.Count} settlements\nPopulation {app.Society.TotalPopulation:N0}",body);
            scroll=GUI.BeginScrollView(new Rect(26,286,344,300),scroll,new Rect(0,0,320,app.Society.Civilizations.Count*43));int i=0;foreach(var c in app.Society.Civilizations){var old=GUI.color;GUI.color=new Color((float)c.Red,(float)c.Green,(float)c.Blue);GUI.DrawTexture(new Rect(1,i*43+8,10,24),Texture2D.whiteTexture);GUI.color=old;if(GUI.Button(new Rect(20,i++*43,294,37),c.Name+" · "+c.Population.ToString("N0")))SelectCivilization(c.Id);}GUI.EndScrollView();
            GUI.Label(new Rect(28,593,338,25),"Time · "+app.Clock.Speed.ToString("0.##")+"×"+(app.Clock.Running?"":" · paused"),body);GUI.enabled=!app.Busy;
            double[] speeds={1,8,32,128};for(int k=0;k<4;k++)if(GUI.Button(new Rect(28+k*67,622,62,25),speeds[k]+"×")){app.Clock.SetSpeed(speeds[k]);app.Clock.Resume();}if(GUI.Button(new Rect(300,622,66,25),"Pause"))app.Clock.Pause();GUI.enabled=true;
        }
        private void CivilizationPanel()
        {
            var c=app.Society.Civilization(civilizationId);if(c==null){civilizationId=null;return;}Header(c.Name);double food=0,timber=0,stone=0,ore=0;foreach(string id in c.Settlements){var s=app.Society.Settlement(id);food+=s.Resources.Food;timber+=s.Resources.Timber;stone+=s.Resources.Stone;ore+=s.Resources.Ore;}
            GUI.Label(new Rect(28,233,338,220),$"Population {c.Population:N0}\nControlled area {UnitFormatting.Area(c.ControlledSquareKilometres,app.Settings.Units)}\nDevelopment {c.Development:0.00} · cohesion {c.Cohesion:P0}\nGovernment {c.Government}\nFounding council · {c.LeaderName}\nCulture {c.CultureName}\nFood stores {food:N0} person-months\nTimber {timber:N0} · stone {stone:N0} · ore {ore:N0}",body);
            scroll=GUI.BeginScrollView(new Rect(26,461,344,187),scroll,new Rect(0,0,320,c.Settlements.Count*42));int i=0;foreach(string id in c.Settlements){var s=app.Society.Settlement(id);if(GUI.Button(new Rect(2,i++*42,312,35),s.Name+" · "+s.Stage+" · "+s.Population.ToString("N0")))SelectSettlement(id);}GUI.EndScrollView();
        }
        private void SettlementPanel()
        {
            var s=app.Society.Settlement(settlementId);if(s==null){settlementId=null;return;}Header(s.Name);var date=new WorldCalendar(s.FoundedTick);GUI.Label(new Rect(28,233,338,320),$"{app.Society.Civilization(s.CivilizationId).Name}\n{s.Stage} · {s.Buildings.Count} structures\nPopulation {s.Population:N1} / capacity {s.Capacity:N0}\nMonthly growth {s.GrowthRate:P2}\nFood needs met {s.FoodSatisfaction:P0}\nFood stores {s.Resources.Food:N0} person-months\nTimber {s.Resources.Timber:N1} · stone {s.Resources.Stone:N1}\nOre {s.Resources.Ore:N1} · supplies {s.Resources.Supplies:N1}\nFounded Year {date.Year}, day {date.Day}\nEast {s.X:N0} m · North {s.Z:N0} m\nElevation {s.Y:N1} m",body);
            if(GUI.Button(new Rect(28,568,338,32),"View this settlement"))FocusSettlement(s.Id);if(GUI.Button(new Rect(28,610,338,32),"Inspect civilization"))SelectCivilization(s.CivilizationId);
        }
        private void HistoryPanel()
        {
            Header(settlementId!=null?app.Society.Settlement(settlementId)?.Name+" · history":civilizationId!=null?app.Society.Civilization(civilizationId)?.Name+" · history":"World history");
            int pages=app.Society.HistoryPages.Count;if(GUI.Button(new Rect(28,233,105,27),"Older")&&pages>0){historyPage=historyPage<0?pages-1:Math.Max(0,historyPage-1);LoadPage();}if(GUI.Button(new Rect(143,233,105,27),"Newer")){historyPage=historyPage<0?-1:historyPage+1>=pages?-1:historyPage+1;LoadPage();}if(GUI.Button(new Rect(258,233,108,27),"Recent")){historyPage=-1;loadedPage=null;scroll=Vector2.zero;}
            var source=historyPage<0?app.Society.RecentHistory:loadedPage;var visible=new List<SocialHistoryEvent>();if(source!=null)for(int i=source.Count-1;i>=0;i--){var e=source[i];if(settlementId!=null&&e.SettlementId!=settlementId||settlementId==null&&civilizationId!=null&&e.CivilizationId!=civilizationId&&e.OtherCivilizationId!=civilizationId)continue;visible.Add(e);}
            GUI.Label(new Rect(28,266,338,24),historyPage<0?"Recent records":"Archived page "+(historyPage+1)+" of "+pages,small);scroll=GUI.BeginScrollView(new Rect(26,296,344,344),scroll,new Rect(0,0,320,Math.Max(40,visible.Count*106)));int start=Math.Max(0,(int)scroll.y/106-1),last=Math.Min(visible.Count,start+6);for(int i=start;i<last;i++){var e=visible[i];var date=new WorldCalendar(e.Tick);GUI.Label(new Rect(3,i*106,309,32),$"Year {date.Year}, day {date.Day} · {e.Title}",body);GUI.Label(new Rect(3,i*106+35,309,68),e.Description,small);}if(visible.Count==0)GUI.Label(new Rect(3,4,309,60),"No matching events on this page.",body);GUI.EndScrollView();if(error.Length>0)GUI.Label(new Rect(28,641,338,20),error,small);
        }
        private void LoadPage(){try{loadedPage=historyPage<0?null:app.Society.ReadHistoryPage(historyPage);error="";scroll=Vector2.zero;}catch(Exception ex){loadedPage=null;error=ex.Message;}}
        private void OnDestroy(){if(owner==this){PointerBlocked=false;Shader.SetGlobalFloat("_AevoriaSocietyEnabled",0);owner=null;}if(overlay!=null)Destroy(overlay);}
    }
}
