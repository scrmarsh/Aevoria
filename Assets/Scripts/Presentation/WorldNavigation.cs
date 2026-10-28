using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevoria.World;
using Aevoria.Core;
using Aevoria.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Aevoria.Presentation
{
    public enum WorldMarkerKind { Waypoint,Player,Camera,Resource,Environment }
    public readonly struct WorldMarker
    {public readonly string Id;public readonly WorldMarkerKind Kind;public readonly GeoPoint Position;public WorldMarker(string id,WorldMarkerKind kind,GeoPoint p){Id=id;Kind=kind;Position=p;}}
    public sealed class WorldNavigation : MonoBehaviour
    {
        private sealed class Atlas {public Color[] Colors;public double X,Z,Span,Angle;public float Aspect;public bool Circle;}
        private AevoriaBootstrap app;private Texture2D map,mini;private Task<Atlas> mapWorker,miniWorker;private Atlas mapView,miniView;
        private bool dirty=true;private Texture2D playerIcon,houseIcon;
        private Texture2D compassFace,compassNeedle;private GUIStyle compassLabel;
        private float compassYaw=float.NaN;private readonly Color[] compassPixels=new Color[64*64];
        private ApplicationSettings draft;private GeoPoint? selected;private GUIStyle label;
        public bool Open {get;private set;}
        public bool AtlasReady=>mapView!=null&&miniView!=null;
        public bool ShowsWholeWorld=>AtlasReady&&mapView.X==0&&mapView.Z==0&&miniView.X==0&&miniView.Z==0&&mapView.Angle==0&&miniView.Angle==0;
        public readonly List<WorldMarker> Markers=new List<WorldMarker>();
        private static readonly Rect MapRect=new Rect(45,145,870,520);
        public void Initialize(AevoriaBootstrap value)
        {app=value;Open=false;dirty=true;draft=app.Settings.Copy();app.ToggleMap=Toggle;Markers.Clear();foreach(var task in new[]{mapWorker,miniWorker})if(task!=null)task.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);mapWorker=miniWorker=null;if(map!=null)Destroy(map);if(mini!=null)Destroy(mini);map=mini=null;mapView=miniView=null;EnsureIcons();}
        public void Toggle(){Open=!Open;if(Open){draft=app.Settings.Copy();}MapCamera.OverlayBlocked=Open||app.Explorer?.InventoryOpen==true;}
        public void SelectLocation(GeoPoint p){selected=p;Markers.RemoveAll(m=>m.Kind==WorldMarkerKind.Waypoint);Markers.Add(new WorldMarker("selected",WorldMarkerKind.Waypoint,p));}
        private void Update()
        {
            if(app?.Living==null)return;
            bool playing=app.Menu.Page==ApplicationPage.World&&!GameInput.Blocked;
            bool acceptingInput=playing&&Application.isFocused&&!app.ExternalControl;
            if(acceptingInput&&GameInput.Pressed(GameAction.Map))Toggle();
            MapCamera.OverlayBlocked=playing&&(Open||app.Explorer?.InventoryOpen==true);
            var mouse=Mouse.current;var position=mouse?.position.ReadValue()??Vector2.zero;var point=new Vector2(position.x/Screen.width*1280,(Screen.height-position.y)/Screen.height*720);
            if(Open&&acceptingInput&&mouse!=null)
            {
                if(MapRect.Contains(point))
                {
                    if(mouse.leftButton.wasPressedThisFrame){double span=mapView?.Span??FullSpan(),x=mapView?.X??0,z=mapView?.Z??0;SelectLocation(new GeoPoint(x+(point.x-MapRect.center.x)/MapRect.width*span,z-(point.y-MapRect.center.y)/MapRect.height*span*MapRect.height/MapRect.width));}
                }
            }
            Complete(ref mapWorker,ref map,ref mapView);Complete(ref miniWorker,ref mini,ref miniView);
            var geo=app.Terrain.Geography;var layout=app.Waters?.Layout;
            if(Open&&playing&&dirty&&mapWorker==null&&layout!=null)
            {double x=0,z=0,span=FullSpan();float aspect=MapRect.height/MapRect.width;mapWorker=Task.Run(()=>Generate(geo,layout,x,z,span,aspect,0,false,512));dirty=false;}
            if((app.Settings.MinimapEnabled||Open)&&playing&&miniWorker==null&&miniView==null&&layout!=null)
            {
                var settings=Open?draft:app.Settings;
                float aspect=settings.MinimapShape==2?.65f:1;bool circle=settings.MinimapShape==0;
                double x=0,z=0,span=WorldSpan(geo.Configuration,aspect,circle),angle=0;
                miniWorker=Task.Run(()=>Generate(geo,layout,x,z,span,aspect,angle,circle));
            }
        }
        private double FullSpan()=>WorldSpan(app.Terrain.Geography.Configuration,MapRect.height/MapRect.width,false);
        // Circular panels must fit the world's corners, not clip them at the rim.
        public static double WorldSpan(WorldConfiguration c,float aspect,bool circle)
            =>1.03*(circle?Math.Sqrt(c.WidthMeters*c.WidthMeters+c.DepthMeters*c.DepthMeters/(aspect*aspect)):Math.Max(c.WidthMeters,c.DepthMeters/aspect));
        private static void Complete(ref Task<Atlas> task,ref Texture2D texture,ref Atlas view)
        {if(task==null||!task.IsCompleted)return;if(task.IsFaulted)Debug.LogException(task.Exception);else{view=task.Result;int n=(int)Math.Sqrt(view.Colors.Length);if(texture!=null&&texture.width!=n){Destroy(texture);texture=null;}if(texture==null)texture=new Texture2D(n,n,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};texture.SetPixels(view.Colors);texture.Apply(false);}task=null;}
        private static Atlas Generate(ProceduralGeography geo,InlandWaterLayout water,double x,double z,double span,float aspect,double angle,bool circle,int n=256)
        {
            var pixels=new Color[n*n];var heights=new float[n*n];double a=angle*Math.PI/180,cos=Math.Cos(a),sin=Math.Sin(a);
            for(int y=0;y<n;y++)for(int column=0;column<n;column++)
            {
                double dx=((column+.5)/n-.5)*span,dz=((y+.5)/n-.5)*span*aspect,east=x+dx*cos+dz*sin,north=z-dx*sin+dz*cos;var p=new GeoPoint(east,north);var s=geo.Sample(p);int i=y*n+column;heights[i]=s.ElevationMeters;
                Color color=EnvironmentPalette.BiomeColor(s.Biome);
                if(s.ElevationMeters<geo.SeaLevel)color=Color.Lerp(new Color(.04f,.57f,.62f),new Color(.018f,.13f,.28f),1-Mathf.Exp((s.ElevationMeters-geo.SeaLevel)/140));
                else if(water.LakeAt(east,north,out _))color=new Color(.06f,.37f,.47f);
                if(!geo.Configuration.Contains(p))color=new Color(.025f,.075f,.13f);
                if(circle&&((column+.5-n*.5)*(column+.5-n*.5)+(y+.5-n*.5)*(y+.5-n*.5)>n*n*.25))color.a=0;pixels[i]=color;
            }
            for(int y=1;y<n-1;y++)for(int column=1;column<n-1;column++)
            {
                int i=y*n+column;if(heights[i]<geo.SeaLevel)continue;
                float dx=(heights[i+1]-heights[i-1])/(float)(2*span/n),dz=(heights[i+n]-heights[i-n])/(float)(2*span*aspect/n);
                float light=Mathf.Clamp(1+(-dx+dz)*.32f,.64f,1.18f);var color=pixels[i];
                pixels[i]=new Color(color.r*light,color.g*light,color.b*light,color.a);
            }
            // Rasterize the actual global flow paths; no invented civilization layer.
            var h=geo.Hydrology;var wetCoverage=new float[n*n];var dryCoverage=new float[n*n];
            for(int i=0;i<h.Ground.Length;i++)if(h.IsChannelCandidate(i)&&h.CatchmentSquareKilometres(i)>=geo.Configuration.WidthMeters*geo.Configuration.DepthMeters/1000000*.004)for(int p=0;p<h.PathSamples(i);p++)
            {
                h.PathPoint(i,p,out double ax,out double az);h.PathPoint(i,p+1,out double bx,out double bz);
                double dx=ax-x,dz=az-z,ex=bx-x,ez=bz-z;
                var start=new Vector2((float)((dx*cos-dz*sin)/span*n+n*.5),(float)((dx*sin+dz*cos)/(span*aspect)*n+n*.5));
                var end=new Vector2((float)((ex*cos-ez*sin)/span*n+n*.5),(float)((ex*sin+ez*cos)/(span*aspect)*n+n*.5));
                bool dry=water.IsSeasonallyDryChannel(i);
                float radius=dry?.30f:Mathf.Clamp((float)h.RiverWidthAt(i,(p+.5)/h.PathSamples(i))*n/(float)span*.5f,.35f,2.5f);
                int left=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(start.x,end.x)-radius-1)),right=Mathf.Min(n-1,Mathf.CeilToInt(Mathf.Max(start.x,end.x)+radius+1));
                int bottom=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(start.y,end.y)-radius-1)),top=Mathf.Min(n-1,Mathf.CeilToInt(Mathf.Max(start.y,end.y)+radius+1));
                var delta=end-start;float length=delta.sqrMagnitude;
                for(int py=bottom;py<=top;py++)for(int px=left;px<=right;px++)
                {
                    int at=py*n+px;if(pixels[at].a<=0||heights[at]<geo.SeaLevel)continue;
                    var centre=new Vector2(px+.5f,py+.5f);float f=length<.0001f?0:Mathf.Clamp01(Vector2.Dot(centre-start,delta)/length);
                    float coverage=Mathf.Clamp01(radius+.5f-Vector2.Distance(centre,start+delta*f));
                    if(coverage<=0||water.LakeAt(ax+(bx-ax)*f,az+(bz-az)*f,out _))continue;
                    if(dry)dryCoverage[at]=Mathf.Max(dryCoverage[at],coverage);else wetCoverage[at]=Mathf.Max(wetCoverage[at],coverage);
                }
            }
            // Composite each union once. Applying fractional alpha per path
            // sample made overlapping samples become opaque dots along rivers.
            for(int i=0;i<pixels.Length;i++)
            {
                pixels[i]=Color.Lerp(pixels[i],new Color(.55f,.39f,.22f),dryCoverage[i]*.7f);
                pixels[i]=Color.Lerp(pixels[i],new Color(.06f,.29f,.43f),wetCoverage[i]);
            }
            return new Atlas{Colors=pixels,X=x,Z=z,Span=span,Aspect=aspect,Angle=angle,Circle=circle};
        }
        private void OnGUI()
        {
            if(app?.Living==null||app.Menu.Page!=ApplicationPage.World)return;var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));if(label==null)label=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};
            if(Open)
            {
                GUI.Box(new Rect(15,110,1250,600)," ");GUI.Label(new Rect(45,115,800,30),"WORLD MAP / NORTH UP / FULL WORLD",label);
                if(map!=null)DrawMap(MapRect,map,mapView,true,1);
                GUI.Label(new Rect(935,150,300,30),"Map and minimap",label);
                GUI.Label(new Rect(935,189,290,32),"Full world / fixed north-up",label);
                GUI.Label(new Rect(935,213,290,22),"Rivers: blue / dry channels: tan",label);
                draft.MinimapEnabled=GUI.Toggle(new Rect(935,234,290,28),draft.MinimapEnabled,"Show minimap in the world");
                draft.MinimapShape=GUI.SelectionGrid(new Rect(935,270,290,30),draft.MinimapShape,new[]{"Circle","Square","Rectangle"},3);
                draft.MinimapCorner=GUI.SelectionGrid(new Rect(935,310,290,54),draft.MinimapCorner,new[]{"Top left","Top right","Bottom left","Bottom right"},2);
                GUI.Label(new Rect(935,372,290,24),"Size / opacity",label);draft.MinimapSize=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(935,402,290,18),draft.MinimapSize,100,300));draft.MinimapOpacity=GUI.HorizontalSlider(new Rect(935,428,290,18),draft.MinimapOpacity,.2f,1);
                draft.MapResources=GUI.Toggle(new Rect(935,508,290,26),draft.MapResources,"Resource markers");draft.MapWaypoints=GUI.Toggle(new Rect(935,536,290,26),draft.MapWaypoints,"Waypoint markers");
                if(GUI.Button(new Rect(935,578,290,34),"Apply map preferences")){draft.MinimapZoom=1;draft.MinimapHeadingUp=false;app.ApplySettings(draft);InvalidateMini();}
                if(GUI.Button(new Rect(935,622,290,34),"Close · M"))Toggle();
                GUI.Label(new Rect(45,673,1150,26),selected.HasValue?$"Selected {selected.Value.X:0} E, {selected.Value.Y:0} N":"Click to place a waypoint / white circle: player / brown house: village",label);
            }
            else if(app.Settings.MinimapEnabled&&mini!=null)
            {
                var settings=app.Settings;float width=settings.MinimapSize,height=width*(settings.MinimapShape==2?.65f:1);bool right=(settings.MinimapCorner&1)!=0,bottom=settings.MinimapCorner>=2;
                var rect=new Rect(right?1260-width:20,bottom?690-height:115,width,height);DrawMap(rect,mini,miniView,false,settings.MinimapOpacity);
                GUI.Label(new Rect(rect.x,rect.yMax-25,rect.width,25),settings.MinimapHeadingUp?"View ↑ · M map":"N ↑ · M map",label);
            }
            if(!Open&&!app.Living.Traveler.Exploring)DrawCompass();
            GUI.matrix=old;
        }
        private void DrawCompass()
        {
            if(compassFace==null){compassFace=CompassTexture(false);compassNeedle=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Rotating world compass needle",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};}
            if(compassLabel==null)compassLabel=new GUIStyle(GUI.skin.label){fontSize=12,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.96f,.87f,.69f)}};
            float x=app.Settings.MinimapEnabled&&app.Settings.MinimapCorner==3?1260-app.Settings.MinimapSize-42:1228;
            var centre=new Vector2(x,642);var rect=new Rect(centre.x-32,centre.y-32,64,64);
            GUI.DrawTexture(rect,compassFace);float yaw=Camera.main.transform.eulerAngles.y;
            // Rotate within the small bitmap. GUIUtility's pivot rotation and
            // the resolution-scaled HUD matrix otherwise clipped the needle.
            if(float.IsNaN(compassYaw)||Mathf.Abs(Mathf.DeltaAngle(compassYaw,yaw))>.25f)
            {
                compassYaw=yaw;float radians=yaw*Mathf.Deg2Rad,sin=Mathf.Sin(radians),cos=Mathf.Cos(radians);
                for(int py=0;py<64;py++)for(int px=0;px<64;px++)
                {
                    float x=px+.5f-32,y=py+.5f-32,across=x*cos+y*sin,along=-x*sin+y*cos;
                    float width=(25-Mathf.Abs(along))*.19f,alpha=Mathf.Abs(along)<25?Mathf.Clamp01(width-Mathf.Abs(across)+.5f):0;
                    compassPixels[py*64+px]=along>=0?new Color(.83f,.21f,.13f,alpha):new Color(.69f,.77f,.79f,alpha);
                }
                compassNeedle.SetPixels(compassPixels);compassNeedle.Apply(false,false);
            }
            GUI.DrawTexture(rect,compassNeedle);
            float angle=-yaw*Mathf.Deg2Rad;var north=centre+new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle))*24;
            GUI.Label(new Rect(north.x-8,north.y-9,16,18),"N",compassLabel);
        }
        private static Texture2D CompassTexture(bool needle)
        {
            const int n=64;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){name=needle?"World compass needle":"World compass ring",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float dx=x+.5f-n*.5f,dy=y+.5f-n*.5f,r=Mathf.Sqrt(dx*dx+dy*dy);
                if(needle)
                {
                    float width=(25-Mathf.Abs(dy))*.19f,alpha=Mathf.Clamp01(width-Mathf.Abs(dx)+.5f);
                    if(Mathf.Abs(dy)<25)colors[y*n+x]=dy>=0?new Color(.83f,.21f,.13f,alpha):new Color(.69f,.77f,.79f,alpha);
                }
                else if(r<32)
                {
                    bool tick=r>26&&(Mathf.Abs(dx)<.8f||Mathf.Abs(dy)<.8f||r>29&&(Mathf.Abs(dx-dy)<.7f||Mathf.Abs(dx+dy)<.7f));
                    var color=r>30||tick?new Color(.67f,.70f,.66f,.9f):new Color(.025f,.07f,.095f,.86f);color.a*=Mathf.Clamp01(32-r);colors[y*n+x]=color;
                }
            }
            texture.SetPixels(colors);texture.Apply(false,true);return texture;
        }
        private void DrawMap(Rect rect,Texture2D texture,Atlas view,bool full,float opacity)
        {
            if(view==null)return;var color=GUI.color;GUI.color=new Color(1,1,1,opacity);GUI.DrawTexture(rect,texture);GUI.color=color;
            var focus=app.Controller.Focus;
            var player=app.Living.Traveler;if(app.Society!=null)foreach(var village in app.Society.Settlements)Icon(rect,view,new GeoPoint(village.X,village.Z),houseIcon,full?11:8);
            if(app.Settings.MapWaypoints)foreach(var marker in Markers)Dot(rect,view,marker.Position,new Color(.8f,.45f,1),6);
            if(app.Settings.MapResources&&app.Details!=null)foreach(var site in app.Details.Sites)if(site.Kind<=NaturalKind.Boulder)Dot(rect,view,site.Position,new Color(.85f,.65f,.23f),2);
            if(player.HasPosition)Icon(rect,view,new GeoPoint(player.X,player.Z),playerIcon,full?8:6);
            if(full)GUI.Label(new Rect(rect.x+10,rect.y+10,600,25),$"Camera {UnitFormatting.Length(focus.x,app.Settings.Units)} E / {UnitFormatting.Length(focus.z,app.Settings.Units)} N · heading {Camera.main.transform.eulerAngles.y:0}°",label);
        }
        private static void Dot(Rect rect,Atlas view,GeoPoint p,Color color,float radius)
        {double a=view.Angle*Math.PI/180,dx=p.X-view.X,dz=p.Y-view.Z,u=(dx*Math.Cos(a)-dz*Math.Sin(a))/view.Span+.5,v=(dx*Math.Sin(a)+dz*Math.Cos(a))/(view.Span*view.Aspect)+.5;if(u<0||u>1||v<0||v>1||view.Circle&&((u-.5)*(u-.5)+(v-.5)*(v-.5)>.25))return;var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(rect.x+(float)u*rect.width-radius/2,rect.y+(float)(1-v)*rect.height-radius/2,radius,radius),Texture2D.whiteTexture);GUI.color=old;}
        private void EnsureIcons()
        {
            if(playerIcon!=null)return;
            playerIcon=MarkerTexture(false);houseIcon=MarkerTexture(true);
        }
        private void InvalidateMini()
        {
            // A completed old shape must not overwrite the newly applied one.
            if(miniWorker!=null)miniWorker.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
            miniWorker=null;miniView=null;
        }
        private static Texture2D MarkerTexture(bool house)
        {
            const int n=24;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var colors=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float dx=x+.5f-n*.5f,dy=y+.5f-n*.5f;
                if(!house){float coverage=Mathf.Clamp01(10.5f-Mathf.Sqrt(dx*dx+dy*dy));colors[y*n+x]=new Color(1,1,1,coverage);}
                else if((y>=3&&y<=13&&x>=5&&x<=18)||(y>=13&&y<=21&&Mathf.Abs(dx)<=22-y))colors[y*n+x]=new Color(.38f,.22f,.10f,1);
            }
            texture.SetPixels(colors);texture.Apply(false,true);return texture;
        }
        private static void Icon(Rect rect,Atlas view,GeoPoint p,Texture2D icon,float size)
        {
            double u=(p.X-view.X)/view.Span+.5,v=(p.Y-view.Z)/(view.Span*view.Aspect)+.5;
            if(u<0||u>1||v<0||v>1)return;
            GUI.DrawTexture(new Rect(rect.x+(float)u*rect.width-size/2,rect.y+(float)(1-v)*rect.height-size/2,size,size),icon);
        }
        private void OnDestroy(){if(playerIcon!=null)Destroy(playerIcon);if(houseIcon!=null)Destroy(houseIcon);if(compassFace!=null)Destroy(compassFace);if(compassNeedle!=null)Destroy(compassNeedle);if(map!=null)Destroy(map);if(mini!=null)Destroy(mini);foreach(var task in new[]{mapWorker,miniWorker})if(task!=null)task.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);}
    }
}
