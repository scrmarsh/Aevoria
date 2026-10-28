using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aevoria.Core;
using Aevoria.Persistence;
using Aevoria.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aevoria.Presentation
{
    public enum ApplicationPage {Home,Create,Load,Settings,Changelog,Chronicles,About,Loading,World,Pause,Controls,Graphics,Audio,Characters,Bindings,Overlays,Display,WorldTools,Advancement}
    public sealed class ApplicationMenu : MonoBehaviour
    {
        private AevoriaBootstrap app;
        private GameAction? capturingBinding;private Vector2 bindingScroll;private int captureFrame;
        public GameAction? CapturingBinding=>capturingBinding;public string BindingNotice=>localError;
        private string lastNotice;private float noticeUntil;
        private Texture2D backdrop,loadingPreview;private GenerationPreview loadingSnapshot;
        private Texture2D pauseBackdrop;private bool capturingPause;
        public bool PauseBackdropReady=>pauseBackdrop!=null&&!capturingPause;
        private GUIStyle loadingTitle,title,heading,body,muted,button,field,small,toggle,hudText;
        private Font font;
        private Texture2D normalButton,hoverButton,activeButton,inputBackground;
        private readonly Color gold=new Color(0.79f,0.68f,0.43f),ink=new Color(0.035f,0.065f,0.10f),paper=new Color(0.88f,0.90f,0.89f);
        public ApplicationPage Page {get; private set;}=ApplicationPage.Home;
        public List<SaveListing> Saves {get; private set;}=new List<SaveListing>();
        public int SelectedChapter {get;set;}=9;
        private ApplicationSettings draft;
        private CreationPreview preview;
        private Vector2 previewScroll;
        private static readonly double[] speeds={.25,.5,1,2,4,8,16,32,64,128};
        public bool DisplayPreviewActive=>confirmingDisplay;
        private string name="A new beginning",seed="",search="",rename="",selectedId,deleteId,localError="";
        private int size,sort,landmass;private Vector2 graphicsScroll;private float rollingFrame;private string coordinates="",fps="";
        private float mountains=3600,roughness=0.65f,land=.54f,sea,temperature,rainfall=1,latitude;
        private Vector2 scroll,documentScroll;
        private string changelog,chronicles;
        private string[] books;
        private string[] changeEntries;
        private string[] changeLabels;
        private int changeVersion;
        private string cachedDocument,cachedDisplay;
        private float cachedHeight;
        private ApplicationSettings previousSettings;
        private int previousWidth,previousHeight;
        private FullScreenMode previousMode;
        private float displayDeadline;
        private bool confirmingDisplay;
        public static bool PointerBlocked {get;private set;}
        private string worldSummary="";
        private float summaryAt;
        private bool scanning,mutating;
        private ApplicationPage settingsReturn=ApplicationPage.Home;
        public void Initialize(AevoriaBootstrap bootstrap)
        {
            app=bootstrap;backdrop=Resources.Load<Texture2D>("UI/MenuLandscape");
            preview=gameObject.AddComponent<CreationPreview>();
            changelog=Resources.Load<TextAsset>("History/Changelog")?.text??"Development record unavailable.";
            chronicles=Resources.Load<TextAsset>("History/Chronicles")?.text??"Chronicles unavailable.";
            books=Regex.Split(chronicles,@"(?im)(?=^#{1,2} CHAPTER )");RefreshSaves();
            var entries=Regex.Split(changelog,@"(?m)(?=^## Alpha )");
            changeEntries=new string[Math.Max(1,entries.Length-1)];
            for(int i=0;i<changeEntries.Length;i++) changeEntries[i]=entries[i==0?entries.Length-1:i];
            changeLabels=new string[changeEntries.Length];for(int i=0;i<changeEntries.Length;i++)changeLabels[i]=Regex.Match(changeEntries[i],@"Alpha (v[0-9.]+)").Groups[1].Value;
        }
        public void Show(ApplicationPage page)
        {
            // The development Chronicle data is retained, but its presentation
            // is temporarily hidden. Old UI callers open the factual changelog.
            if(page==ApplicationPage.Chronicles)page=ApplicationPage.Changelog;
            var previous=Page;
            if(confirmingDisplay && page!=ApplicationPage.Settings) RevertDisplay();
            if(page==ApplicationPage.Settings) {settingsReturn=app.WorldActive?ApplicationPage.Pause:ApplicationPage.Home;draft=app.Settings.Copy();}
            if(page==ApplicationPage.Graphics||page==ApplicationPage.Audio||page==ApplicationPage.Controls||page==ApplicationPage.Display){settingsReturn=previous==ApplicationPage.Settings?ApplicationPage.Settings:app.WorldActive?ApplicationPage.Pause:ApplicationPage.Home;draft=app.Settings.Copy();}
            if(page==ApplicationPage.Bindings){settingsReturn=ApplicationPage.Settings;if(previous!=ApplicationPage.Controls||draft==null)draft=app.Settings.Copy();}
            capturingBinding=null;Page=page;scroll=Vector2.zero;documentScroll=Vector2.zero;localError="";GUIUtility.keyboardControl=0;
            if(page==ApplicationPage.Load || page==ApplicationPage.Home) RefreshSaves();
            if(page==ApplicationPage.Pause&&previous==ApplicationPage.World&&app.WorldActive&&!capturingPause)StartCoroutine(CapturePauseBackdrop());
        }
        private IEnumerator CapturePauseBackdrop()
        {
            capturingPause=true;
            // With the world paused, one clean camera frame becomes the menu
            // background. No old promotional image or generated substitute.
            yield return null;yield return new WaitForEndOfFrame();
            if(Page==ApplicationPage.Pause&&app.WorldActive)
            {
                var captured=ScreenCapture.CaptureScreenshotAsTexture();
                if(captured!=null){if(pauseBackdrop!=null)Destroy(pauseBackdrop);pauseBackdrop=captured;}
            }
            capturingPause=false;
        }
        public void SaveAndQuitToMainMenu()
        {if(app.WorldActive&&!app.Busy)app.SaveWorld(returnToMenu:true);}
        private ApplicationPage ParentPage()
        {
            if(Page==ApplicationPage.Graphics||Page==ApplicationPage.Audio||Page==ApplicationPage.Controls||Page==ApplicationPage.Bindings||Page==ApplicationPage.Display||Page==ApplicationPage.WorldTools)return ApplicationPage.Settings;
            if(Page==ApplicationPage.Characters||Page==ApplicationPage.Overlays)return app.WorldActive?ApplicationPage.WorldTools:ApplicationPage.Settings;
            return app.WorldActive?ApplicationPage.Pause:ApplicationPage.Home;
        }
        public async void RefreshSaves()
        {
            if(scanning || app==null) return;scanning=true;
            try {Saves=await app.Repository.ListAsync();}catch(Exception ex){localError=ex.Message;}finally{scanning=false;}
        }
        private void Update()
        {
            if(app.Message!=lastNotice){lastNotice=app.Message;noticeUntil=Time.unscaledTime+4;if(lastNotice?.IndexOf("failed",StringComparison.OrdinalIgnoreCase)>=0)noticeUntil=float.PositiveInfinity;}
            var pointer=Mouse.current?.position.ReadValue()??Vector2.zero;
            PointerBlocked=Page!=ApplicationPage.World;rollingFrame=Mathf.Lerp(rollingFrame<=0?Time.unscaledDeltaTime:rollingFrame,Time.unscaledDeltaTime,.08f);
            if(Page==ApplicationPage.World && Time.unscaledTime>=summaryAt)
            {
                var date=new Aevoria.Simulation.WorldCalendar(app.Clock.State.Tick);
                worldSummary=$"Year {date.Year} · {date.SeasonName} · {app.Clock.Speed:0.##}× · {(app.Clock.Running?"Running":"Paused")}";
                var p=app.Living?.Traveler.Exploring==true?app.Explorer.Position:WorldOrigin.Geographic(Camera.main.transform);coordinates=$"X {UnitFormatting.Length(p.x,app.Settings.Units)}  Y {UnitFormatting.Length(p.y,app.Settings.Units)}\nZ {UnitFormatting.Length(p.z,app.Settings.Units)}";fps=(app.Settings.ShowFps?$"{1/Mathf.Max(.00001f,rollingFrame):0} FPS ":"")+(app.Settings.ShowFrameTime?$"{rollingFrame*1000:0.0} ms":"");summaryAt=Time.unscaledTime+0.25f;
            }
            if(confirmingDisplay && Time.realtimeSinceStartup>=displayDeadline) RevertDisplay();
            if(Page==ApplicationPage.Bindings&&capturingBinding.HasValue){CaptureBinding();return;}
            if(Application.isFocused&&!app.ExternalControl&&Keyboard.current?.escapeKey.wasPressedThisFrame==true && !app.Busy&&!GameInput.Blocked)
            {if(confirmingDisplay) RevertDisplay();else if(Page==ApplicationPage.World){if(app.Navigation?.Open==true)app.Navigation.Toggle();else if(app.Explorer?.InventoryOpen==true)app.Explorer.CloseInventory();else Show(ApplicationPage.Pause);}else if(Page==ApplicationPage.Pause) Show(ApplicationPage.World);else if(Page!=ApplicationPage.Home && Page!=ApplicationPage.Loading) Show(ParentPage());}
        }
        private void Styles()
        {
            if(title!=null) return;
            font=Font.CreateDynamicFontFromOSFont("Segoe UI",20);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=17,wordWrap=true,normal={textColor=paper}};
            muted=new GUIStyle(body){fontSize=14,normal={textColor=new Color(0.62f,0.70f,0.75f)}};
            small=new GUIStyle(muted){fontSize=12};toggle=new GUIStyle(GUI.skin.toggle){font=font,fontSize=17,normal={textColor=paper},onNormal={textColor=paper}};
            hudText=new GUIStyle(small){fontSize=13,normal={textColor=Color.white}};
            heading=new GUIStyle(body){fontSize=27,normal={textColor=gold}};
            title=new GUIStyle(heading){fontSize=61,fontStyle=FontStyle.Normal};loadingTitle=new GUIStyle(title){fontSize=38,wordWrap=false};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=17,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(17,12,4,4),normal={textColor=paper},hover={textColor=gold},active={textColor=Color.white}};
            field=new GUIStyle(GUI.skin.textField){font=font,fontSize=17,padding=new RectOffset(8,8,5,5)};
            normalButton=Swatch(new Color(0.085f,0.14f,0.19f,0.96f));hoverButton=Swatch(new Color(0.14f,0.23f,0.29f));activeButton=Swatch(new Color(0.27f,0.30f,0.28f));inputBackground=Swatch(new Color(0.025f,0.05f,0.075f));
            button.normal.background=normalButton;button.hover.background=hoverButton;button.active.background=activeButton;button.onNormal.background=activeButton;button.onHover.background=hoverButton;button.onActive.background=activeButton;button.border=new RectOffset();
            field.normal.background=inputBackground;field.focused.background=normalButton;field.hover.background=inputBackground;field.border=new RectOffset();field.normal.textColor=paper;field.focused.textColor=Color.white;
        }
        private static Texture2D Swatch(Color color){var texture=new Texture2D(1,1){hideFlags=HideFlags.HideAndDontSave};texture.SetPixel(0,0,color);texture.Apply();return texture;}
        private void Label(float x,float y,float w,float h,string text,GUIStyle style=null)=>GUI.Label(new Rect(x,y,w,h),text,style??body);
        private bool Button(float x,float y,float w,string label,float h=43)=>GUI.Button(new Rect(x,y,w,h),label,button);
        private void HudLabel(Rect rect,string value)
        {var color=GUI.color;GUI.color=Color.white;hudText.normal.textColor=Color.black;GUI.Label(new Rect(rect.x+1,rect.y+1,rect.width,rect.height),value,hudText);hudText.normal.textColor=Color.white;GUI.Label(rect,value,hudText);GUI.color=color;}
        private void Panel(Rect rect,Color color) {var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        private void OnGUI()
        {
            if(app==null||capturingPause) return;Styles();
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            if(Page==ApplicationPage.World)
            {
                HudLabel(new Rect(20,12,410,48),worldSummary);
                HudLabel(new Rect(920,16,340,48),coordinates);HudLabel(new Rect(920,63,340,24),fps);
                
                if(app.Terrain?.PreloadStatus.Length>0){Label(25,595,1000,40,app.Terrain.PreloadStatus,small);if(app.Terrain.Preloading&&Button(1020,595,235,"Cancel preload",30))app.Terrain.CancelPreload();}
                if(!string.IsNullOrEmpty(app.Message)&&Time.unscaledTime<noticeUntil)Label(425,682,840,27,app.Message,muted);                GUI.matrix=matrix;return;
            }
            Panel(new Rect(0,0,1280,720),ink);
            if(Page==ApplicationPage.Loading)LoadingBackground();
            else if(app.WorldActive&&pauseBackdrop!=null)GUI.DrawTexture(new Rect(0,0,1280,720),pauseBackdrop,ScaleMode.ScaleAndCrop);
            else if(backdrop!=null) GUI.DrawTextureWithTexCoords(new Rect(0,0,1280,720),backdrop,new Rect(0.16f,0.04f,0.84f,0.91f));
            Panel(new Rect(0,0,1280,720),new Color(0.015f,0.035f,0.065f,Page==ApplicationPage.Loading?.35f:.72f));
            Panel(new Rect(42,42,3,610),gold);
            Label(65,34,700,36,"A E V O R I A",heading);Label(938,43,300,24,"ALPHA  v"+ApplicationVersion.Number+"  ·  DEVELOPMENT",small);
            Panel(new Rect(65,85,1150,1),new Color(0.79f,0.68f,0.43f,0.35f));
            heading.normal.textColor=gold;
            if(Page!=ApplicationPage.Home&&Page!=ApplicationPage.Loading&&Page!=ApplicationPage.Pause&&Button(1095,96,120,"← Back",34))Show(ParentPage());
            switch(Page)
            {
                case ApplicationPage.Home: Home();break;
                case ApplicationPage.Create: Create();break;
                case ApplicationPage.Load: Load();break;
                case ApplicationPage.Settings: Settings();break;
                case ApplicationPage.Display: DisplaySettings();break;
                case ApplicationPage.WorldTools: WorldTools();break;
                case ApplicationPage.Advancement: Label(90,150,900,60,"Advancement",heading);Label(90,245,900,60,"Coming Soon.",body);break;
                case ApplicationPage.Controls: Controls();break;
                case ApplicationPage.Graphics: Graphics();break;case ApplicationPage.Audio: Audio();break;
                case ApplicationPage.Changelog:
                    changeVersion=GUI.SelectionGrid(new Rect(90,154,1100,38),changeVersion,changeLabels,changeLabels.Length,button);
                    Document("Changelog",changeEntries[Math.Min(changeVersion,changeEntries.Length-1)]);break;
                case ApplicationPage.Chronicles: Chronicles();break;
                case ApplicationPage.About: About();break;
                case ApplicationPage.Characters: Characters();break;
                case ApplicationPage.Bindings: Bindings();break;case ApplicationPage.Overlays: Overlays();break;
                case ApplicationPage.Loading: Loading();break;
                case ApplicationPage.Pause: Pause();break;
            }
            Label(65,675,900,28,string.IsNullOrEmpty(localError)?app.Message:localError,muted);
            Label(995,680,225,24,"Created by Sean Marsh",small);
            GUI.matrix=matrix;
        }
        private void Home()
        {
            Label(85,131,650,82,"The living world",title);
            Label(90,224,610,54,"A living world begins with a place to endure.",body);
            if(Button(90,309,330,"Create new world")) Show(ApplicationPage.Create);
            if(Button(90,361,330,"Load world")) Show(ApplicationPage.Load);
            if(Button(90,413,330,"Settings")) Show(ApplicationPage.Settings);
            if(Button(90,465,330,"Changelog")) Show(ApplicationPage.Changelog);
            if(Button(90,517,160,"About")) Show(ApplicationPage.About);
            if(Button(260,517,160,"Quit to desktop")) app.Quit();
            Panel(new Rect(740,220,460,385),new Color(0.03f,0.06f,0.10f,0.82f));Label(765,240,410,40,"Recent worlds",heading);
            if(scanning) Label(765,305,400,50,"Reading your world library…",muted);
            else if(Saves.Count==0) {Label(765,310,390,60,"Your first world is still unwritten.",body);Label(765,392,390,75,"Create a world to give it a name, a seed, and a place in this archive.",muted);}
            else for(int i=0;i<Math.Min(3,Saves.Count);i++)
            {var item=Saves[i];float y=290+i*100;if(Button(760,y,420,item.Metadata?.Name??"Damaged archive",36)) app.LoadWorld(item.Id);Label(773,y+36,400,22,$"Year {item.Metadata?.Year} · played {Date(item.Metadata?.LastPlayedUtc)}",small);Label(773,y+57,400,22,$"Seed {item.Metadata?.Seed} · {Dimensions(item.Metadata?.Width??0,item.Metadata?.Depth??0)}",small);Label(773,y+78,400,22,item.Status,small);}
        }
        private void Create()
        {
            Label(90,105,900,50,"Create a new world",heading);
            Label(90,163,450,26,"World name");name=GUI.TextField(new Rect(90,192,450,36),name,80,field);
            Label(90,244,450,26,"Seed");seed=GUI.TextField(new Rect(90,274,450,36),seed,128,field);
            if(string.IsNullOrEmpty(seed))Label(102,279,425,28,"Leave blank for random seed",muted);
            Label(90,329,460,27,"World size");var sizes=new[]{"Miniature","Small","Medium","Large","Colossal"};for(int i=0;i<sizes.Length;i++)sizes[i]+=" "+UnitFormatting.Distance(128000*Math.Pow(2,i),app.Settings.Units,"N0");size=GUI.SelectionGrid(new Rect(90,363,450,64),size,sizes,3,button);
            Label(90,427,450,23,"Landmass type",small);landmass=GUI.SelectionGrid(new Rect(90,451,450,48),landmass,new[]{"Random","Single","Continents","Supercontinent","Archipelago","Mixed"},3,button);
            if(preview.Texture!=null)GUI.DrawTexture(new Rect(90,510,160,100),preview.Texture,ScaleMode.ScaleToFit);
            previewScroll=GUI.BeginScrollView(new Rect(268,508,277,102),previewScroll,new Rect(0,0,215,330));Label(0,0,215,330,(preview.Pending?"Sampling preview…\n":"")+preview.Details,small);GUI.EndScrollView();
            GUI.enabled=!app.Busy;
            if(Button(90,618,450,"Create world",45))
            {try{app.CreateWorld(name,seed,(WorldSize)size,new GenerationSettings(seaLevel:sea,mountains:mountains,roughness:roughness,targetLandFraction:land,landmass:(LandmassType)landmass,profile:GeographyProfile.Atlas),new ClimateSettings(temperature,rainfall,latitude));}catch(Exception ex){localError=ex.Message;}}
            GUI.enabled=true;
            mountains=Slider(650,165,"Mountain relief",mountains,0,GenerationSettings.MaximumMountainRelief,"0 m");
            roughness=Slider(650,226,"Surface roughness",roughness,0,1,"0.00");
            land=Slider(650,287,"Target land coverage",land,0.3f,0.85f,"P0");
            sea=Slider(650,348,"Sea level",sea,-100,100,"0 m");
            temperature=Slider(650,409,"Temperature offset",temperature,-25,25,"0 °C");
            rainfall=Slider(650,470,"Rainfall multiplier",rainfall,0.2f,2,"0.00×");
            latitude=Slider(650,531,"Central latitude",latitude,-75,75,"0°");
            Label(650,604,520,43,UnitFormatting.Length(16000,app.Settings.Units)+" is the relief parameter ceiling; ground elevation also includes the underlying landform.",small);
            if(!string.IsNullOrWhiteSpace(seed))preview.Configure(seed,(WorldSize)size,new GenerationSettings(seaLevel:sea,mountains:mountains,roughness:roughness,targetLandFraction:land,landmass:(LandmassType)landmass,profile:GeographyProfile.Atlas),new ClimateSettings(temperature,rainfall,latitude),app.Settings.Units);else preview.Clear();
            if(!string.IsNullOrEmpty(GUI.tooltip))Label(650,648,520,30,GUI.tooltip,small);
        }
        private string Dimensions(double width,double depth)=>UnitFormatting.Distance(width,app.Settings.Units)+" × "+UnitFormatting.Distance(depth,app.Settings.Units);
        private string FormatMeasurement(string label,float value,string format)
        {if(format.Contains("m/s"))return UnitFormatting.Speed(value,app.Settings.Units);if(format.EndsWith(" m"))return UnitFormatting.Length(value,app.Settings.Units,format.Substring(0,format.Length-2));if(label.Contains("Temperature"))return UnitFormatting.TemperatureDifference(value,app.Settings.Units);return value.ToString(format);}
        private float Slider(float x,float y,string label,float value,float min,float max,string format)
        {string tip=label.Contains("Mountain")?"Relief above the underlying landform; higher ranges constrain routes.":label.Contains("Temperature")?"Adds degrees Celsius to the generated climate; cold conditions favor snow.":label.Contains("Rainfall")?"Scales climate moisture; dry conditions limit habitat and freshwater.":label.Contains("land")?"Calibrates continental coverage before sea-level flooding.":label.Contains("Sea")?"Floods existing landforms without changing their elevations.":label.Contains("rough")?"Changes small terrain variations, not world dimensions.":"Adjust "+label.ToLowerInvariant();GUI.Label(new Rect(x,y,520,26),new GUIContent(label+"   "+FormatMeasurement(label,value,format),tip),body);return GUI.HorizontalSlider(new Rect(x,y+34,520,18),value,min,max);}
        private void Load()
        {
            Label(90,105,800,50,"World archive",heading);
            search=GUI.TextField(new Rect(90,163,520,36),search,128,field);if(Button(623,163,110,"Refresh",36)) RefreshSaves();
            sort=GUI.SelectionGrid(new Rect(749,163,430,36),sort,new[]{"Recent","Name","Year"},3,button);
            var rows=new List<SaveListing>(Saves);
            if(sort==1) rows.Sort((a,b)=>string.Compare(a.Metadata?.Name,b.Metadata?.Name,StringComparison.OrdinalIgnoreCase));
            if(sort==2) rows.Sort((a,b)=>(b.Metadata?.Year??0).CompareTo(a.Metadata?.Year??0));
            rows.RemoveAll(r=>!string.IsNullOrEmpty(search) && (r.Metadata?.Name??r.Id).IndexOf(search,StringComparison.OrdinalIgnoreCase)<0 && (r.Metadata?.Seed??"").IndexOf(search,StringComparison.OrdinalIgnoreCase)<0);
            scroll=GUI.BeginScrollView(new Rect(90,220,670,412),scroll,new Rect(0,0,644,Math.Max(390,rows.Count*83)));
            if(rows.Count==0) Label(12,15,610,90,scanning?"Reading archive metadata…":"No matching saved worlds. Create a world to begin.",muted);
            for(int i=0;i<rows.Count;i++)
            {
                var row=rows[i];if(Button(0,i*83,633,row.Metadata?.Name??"Unreadable metadata",43)) {selectedId=row.Id;rename=row.Metadata?.Name??"Recovered world";deleteId=null;}
                Label(12,i*83+45,610,34,$"Year {row.Metadata?.Year} · {Date(row.Metadata?.SavedUtc)} · {row.Status}",small);
            }
            GUI.EndScrollView();
            var selected=Saves.Find(s=>s.Id==selectedId);
            if(selected==null) {Label(810,244,370,150,"Select a world to inspect its identity, load it, rename it, or remove it from disk.",muted);return;}
            var m=selected.Metadata;
            Label(802,226,375,190,$"{m?.Name??"Damaged metadata"}\nSeed: {m?.Seed??"Unknown"}\nYear {m?.Year}, day {m?.Day}\n{Dimensions(m?.Width??0,m?.Depth??0)}\nApp {m?.ApplicationVersion} · generator {m?.Generator}\n{selected.Status}\nBackup: {(selected.HasBackup?"available":"none")}",body);
            Label(802,416,380,29,selected.Id,small);
            GUI.enabled=!app.Busy&&!mutating;
            if(m?.Schema==3&&m.Generator==ProceduralGeography.GeneratorVersion){if(Button(802,452,375,"Upgrade copy · preserve original")){localError="Preparing a validated upgrade copy…";Mutate(async()=>{var copy=await app.Repository.UpgradeCopyAsync(selected.Id);app.LoadWorld(copy.Id);});}}
            else if(Button(802,452,375,"Load world / try recovery")) app.LoadWorld(selected.Id);
            rename=GUI.TextField(new Rect(802,504,245,35),rename,80,field);
            if(Button(1055,504,122,"Rename",35)) Mutate(()=>app.RenameAsync(selected.Id,rename));
            if(deleteId==selected.Id)
            {if(Button(802,552,182,"Confirm delete")) Mutate(()=>app.Repository.DeleteAsync(selected.Id));if(Button(995,552,182,"Keep world")) deleteId=null;Label(802,604,380,40,"Deletion removes this archive and its backup.",small);}
            else if(Button(802,552,375,"Delete world…")) deleteId=selected.Id;
            GUI.enabled=true;
        }
        private async void Mutate(Func<Task> action)
        {mutating=true;try{await action();deleteId=null;RefreshSaves();}catch(Exception ex){localError=ex.Message;}finally{mutating=false;}}
        private void Settings()
        {
            Label(90,115,950,50,"Settings",heading);
            if(Button(90,220,500,"Graphics",52))Show(ApplicationPage.Graphics);
            if(Button(90,292,500,"Audio",52))Show(ApplicationPage.Audio);
            if(Button(90,364,500,"Controls and camera",52))Show(ApplicationPage.Controls);
            if(Button(660,220,500,"Key bindings",52))Show(ApplicationPage.Bindings);
            if(Button(660,292,500,"Display, units and autosave",52))Show(ApplicationPage.Display);
            if(app.WorldActive&&Button(660,364,500,"World and traveler tools",52))Show(ApplicationPage.WorldTools);
        }
        private void DisplaySettings()
        {
            if(draft==null) draft=app.Settings.Copy();Label(90,105,540,50,"Display preferences",heading);
            draft.VSync=GUI.Toggle(new Rect(90,175,460,30),draft.VSync==1,"Vertical sync",toggle)?1:0;
            draft.FrameCap=Mathf.RoundToInt(Slider(90,225,"Engine frame limit",draft.FrameCap,30,360,"0"));
            Label(90,290,500,28,"Display units");var units=(UnitSystem)GUI.SelectionGrid(new Rect(90,333,520,45),(int)draft.Units,new[]{"Metric","Imperial"},2,button);if(units!=draft.Units){draft.Units=units;var current=app.Settings.Copy();current.Units=units;app.ApplySettings(current);}
            draft.Shadows=GUI.Toggle(new Rect(90,403,500,30),draft.Shadows,"Terrain and vegetation shadows",toggle);
            draft.Antialiasing=GUI.Toggle(new Rect(90,447,500,30),draft.Antialiasing,"FXAA antialiasing",toggle);
            draft.MotionBlur=GUI.Toggle(new Rect(90,491,500,30),draft.MotionBlur,"Subtle camera motion blur",toggle);
            draft.TerrainFollowing=GUI.Toggle(new Rect(660,175,510,30),draft.TerrainFollowing,"Follow terrain elevation",toggle);
            draft.TerrainCollision=GUI.Toggle(new Rect(660,219,510,30),draft.TerrainCollision,"Additional camera slope clearance (surface safety always on)",toggle);
            draft.ZoomSensitivity=Slider(660,275,"Zoom sensitivity",draft.ZoomSensitivity,0.2f,2.5f,"0.0×");
            draft.AutosaveSeconds=Mathf.RoundToInt(Slider(660,354,"Autosave interval (0 disables)",draft.AutosaveSeconds,0,600,"0 s"));
            Label(660,427,500,27,"Display mode");
            draft.DisplayMode=GUI.SelectionGrid(new Rect(660,461,520,34),draft.DisplayMode,new[]{"Window","Borderless","Exclusive"},3,button);
            Label(90,536,500,25,"Resolution · display changes revert after 15 seconds",small);
            if(Button(660,508,160,"1600 × 900",34)){draft.DisplayWidth=1600;draft.DisplayHeight=900;}
            if(Button(837,508,160,"1920 × 1080",34)){draft.DisplayWidth=1920;draft.DisplayHeight=1080;}
            if(Button(1014,508,166,"2560 × 1440",34)){draft.DisplayWidth=2560;draft.DisplayHeight=1440;}
            Label(660,548,520,24,draft.DisplayWidth==0?"Resolution: current display":$"Selected: {draft.DisplayWidth} × {draft.DisplayHeight}",small);
            if(confirmingDisplay)
            {
                Label(90,580,500,55,$"Keep this display? Reverting in {Mathf.CeilToInt(displayDeadline-Time.realtimeSinceStartup)} seconds.");
                if(Button(660,590,240,"Keep changes")){try{app.ApplySettings(draft);confirmingDisplay=false;Show(settingsReturn);}catch(Exception ex){localError=ex.Message;RevertDisplay();}}
                if(Button(920,590,240,"Revert")) RevertDisplay();
            }
            else
            {
                if(Button(90,590,240,"Apply")) ApplyDraft();
                if(Button(345,590,240,"Cancel")) Show(settingsReturn);
                if(Button(660,590,240,"Restore defaults")) draft=new ApplicationSettings();
            }
        }
        private void ApplyDraft()
        {
            try
            {
                if(draft.DisplayMode!=app.Settings.DisplayMode || draft.DisplayWidth>0&&(draft.DisplayWidth!=Screen.width||draft.DisplayHeight!=Screen.height))
                {
                    previousSettings=app.Settings.Copy();previousWidth=Screen.width;previousHeight=Screen.height;previousMode=Screen.fullScreenMode;
                    if(draft.DisplayWidth==0){draft.DisplayWidth=Screen.width;draft.DisplayHeight=Screen.height;}
                    app.ApplySettings(draft,false);confirmingDisplay=true;displayDeadline=Time.realtimeSinceStartup+15;
                }
                else {app.ApplySettings(draft);Show(settingsReturn);}
            }
            catch(Exception ex){localError=ex.Message;}
        }
        private void Controls()
        {
            if(draft==null)draft=app.Settings.Copy();Label(90,112,900,45,"Controls and camera",heading);
            draft.KeyboardMovement=GUI.Toggle(new Rect(90,180,500,30),draft.KeyboardMovement,"Keyboard movement",toggle);
            draft.MiddlePanning=GUI.Toggle(new Rect(90,225,500,30),draft.MiddlePanning,"Middle-mouse drag pan",toggle);
            draft.EdgeScrolling=GUI.Toggle(new Rect(90,270,500,30),draft.EdgeScrolling,"Mouse edge scrolling",toggle);
            draft.MovementSensitivity=Slider(90,330,"Movement sensitivity",draft.MovementSensitivity,.2f,4,"0.0×");
            draft.ZoomSensitivity=Slider(90,410,"Zoom sensitivity",draft.ZoomSensitivity,.2f,2.5f,"0.0×");
            draft.EdgeZone=Mathf.RoundToInt(Slider(660,180,"Edge activation zone",draft.EdgeZone,4,80,"0 px"));
            draft.EdgeSpeed=Slider(660,260,"Edge movement speed",draft.EdgeSpeed,.2f,3,"0.0×");
            draft.HeadBob=Slider(660,340,"First-person walking bob",draft.HeadBob,0,.12f,"0.000 m");
            draft.FlightSpeed=Slider(90,490,"Exploration flight speed",draft.FlightSpeed,.5f,5000,"0 m/s");
            draft.DoubleTapWindow=Slider(90,560,"Double-Space interval",draft.DoubleTapWindow,.15f,.6f,"0.00 s");
            draft.MinimumPanSpeed=Slider(660,565,"Minimum Top-Down movement speed",draft.MinimumPanSpeed,10,500,"0 m/s");
            if(Button(660,430,510,"Keyboard and mouse bindings"))Show(ApplicationPage.Bindings);Label(660,484,510,60,"Double-tap jump to toggle flight. The mouse wheel selects tools on foot and adjusts speed in flight.",small);
            if(Button(90,610,450,"Apply controls")){try{app.ApplySettings(draft);Show(settingsReturn);}catch(Exception ex){localError=ex.Message;}}
            if(Button(660,610,450,"Cancel"))Show(settingsReturn);
        }
        private void Bindings()
        {
            if(draft==null)draft=app.Settings.Copy();Label(90,112,950,45,"Keyboard and mouse bindings",heading);
            Label(90,164,1090,44,capturingBinding.HasValue?"Press a keyboard key or mouse button. Escape cancels.":localError.Length>0?localError:"Choose an action. Inputs may be shared between World and Explore contexts.",small);
            var actions=(GameAction[])Enum.GetValues(typeof(GameAction));bindingScroll=GUI.BeginScrollView(new Rect(90,212,1090,362),bindingScroll,new Rect(0,0,1040,actions.Length*40));
            for(int i=0;i<actions.Length;i++){var action=actions[i];Label(0,i*40,540,33,InputBindings.Label(action)+" · "+InputBindings.ContextLabel(action),body);if(Button(610,i*40,360,capturingBinding==action?"Waiting…":InputBindings.Input(draft.Bindings,action),33))BeginBindingCapture(action);}
            GUI.EndScrollView();
            if(Button(90,610,300,"Save bindings"))ApplyBindings();
            if(Button(410,610,300,"Restore defaults"))RestoreBindingDefaults();
            if(Button(730,610,300,"Cancel"))Show(settingsReturn);
        }
        public void BeginBindingCapture(GameAction action)
        {if(draft==null)draft=app.Settings.Copy();if(Page!=ApplicationPage.Bindings)Show(ApplicationPage.Bindings);capturingBinding=action;captureFrame=Time.frameCount;localError="";}
        public void ApplyBindings()
        {try{app.ApplySettings(draft);Show(settingsReturn);}catch(Exception ex){localError=ex.Message;}}
        public void RestoreBindingDefaults()
        {if(draft==null)draft=app.Settings.Copy();draft.Bindings.Clear();capturingBinding=null;localError="Defaults restored. Save to apply.";}
        private void CaptureBinding()
        {
            if(Time.frameCount<=captureFrame)return;var keyboard=Keyboard.current;
            if(keyboard?.escapeKey.wasPressedThisFrame==true){capturingBinding=null;GameInput.ConsumedFrame=Time.frameCount;return;}
            string input=null;if(keyboard!=null)foreach(var key in keyboard.allKeys)if(key.wasPressedThisFrame){input=key.keyCode.ToString();break;}
            var mouse=Mouse.current;if(mouse?.leftButton.wasPressedThisFrame==true)input="MouseLeft";else if(mouse?.rightButton.wasPressedThisFrame==true)input="MouseRight";else if(mouse?.middleButton.wasPressedThisFrame==true)input="MouseMiddle";else if(mouse?.backButton.wasPressedThisFrame==true)input="MouseBack";else if(mouse?.forwardButton.wasPressedThisFrame==true)input="MouseForward";
            if(input==null)return;var action=capturingBinding.Value;localError=InputBindings.Conflict(draft.Bindings,action,input)??"";if(localError.Length>0){capturingBinding=null;return;}
            draft.Bindings.RemoveAll(b=>b.Action==action);draft.Bindings.Add(new KeyBinding(action,input));capturingBinding=null;GameInput.ConsumedFrame=Time.frameCount;
        }
        private void Overlays()
        {
            Label(90,112,1000,45,"World and simulation views",heading);
            if(Button(90,210,450,"Civilizations")){Show(ApplicationPage.World);app.SocietyView.Open=true;}
            if(Button(90,265,450,"World history")){Show(ApplicationPage.World);app.SocietyView.ShowHistory();}
            int row=0;foreach(SocietyOverlayMode mode in Enum.GetValues(typeof(SocietyOverlayMode))){if(Button(660,210+row++*55,450,mode.ToString())){app.SocietyView.SetOverlay(mode);Show(ApplicationPage.World);}}
            if(Button(90,320,450,"Ecosystem maps")){Show(ApplicationPage.World);var ecology=GetComponent<EcologyPresentation>();if(ecology!=null)ecology.Inspect=true;}
        }
        private void RevertDisplay()
        {
            if(!confirmingDisplay) return;confirmingDisplay=false;app.ApplySettings(previousSettings,false);app.DisplayControl.Request(previousWidth,previousHeight,previousMode);draft=previousSettings.Copy();
        }
        private void Document(string label,string text)
        {
            Label(90,108,850,45,label,heading);
            CacheDocument(text);
            documentScroll=GUI.BeginScrollView(new Rect(90,211,1110,432),documentScroll,new Rect(0,0,1070,cachedHeight));Label(0,0,1060,cachedHeight,cachedDisplay);GUI.EndScrollView();
        }
        private void Chronicles()
        {
            heading.normal.textColor=SelectedChapter==0?gold:SelectedChapter==1?new Color(0.58f,0.64f,0.49f):SelectedChapter==2?new Color(0.32f,0.69f,0.64f):SelectedChapter==4?new Color(0.48f,0.72f,0.40f):new Color(0.57f,0.65f,0.87f);
            var chapters=Regex.Matches(chronicles,@"(?im)^#{1,2}\s+chapter\s+([ivxlcdm]+)\s*-\s*(.+)$");var labels=new string[chapters.Count];for(int i=0;i<labels.Length;i++)labels[i]=chapters[i].Groups[1].Value.ToUpperInvariant()+" · "+chapters[i].Groups[2].Value.Trim();
            SelectedChapter=Mathf.Clamp(SelectedChapter,0,Mathf.Max(0,labels.Length-1));SelectedChapter=GUI.SelectionGrid(new Rect(90,150,1100,50),SelectedChapter,labels,5,button);
            string text=books!=null&&books.Length>SelectedChapter+1?books[SelectedChapter+1]:chronicles;
            Label(90,105,870,40,"The Chronicles of Aevoria",heading);
            Label(90,207,1100,25,SelectedChapter>=3?"IN DEVELOPMENT · implementation and open acceptance recorded separately":"DEVELOPMENT HISTORY · retrospective record · no public release date asserted",muted);
            CacheDocument(text);
            documentScroll=GUI.BeginScrollView(new Rect(90,245,1110,395),documentScroll,new Rect(0,0,1070,cachedHeight));Label(0,0,1060,cachedHeight,cachedDisplay);GUI.EndScrollView();
        }
        private void CacheDocument(string text)
        {
            if(cachedDocument==text) return;cachedDocument=text;
            cachedDisplay=Regex.Replace(text,@"\[([^\]]+)\]\([^)]+\)","$1").Replace("**","").Replace("`","");
            cachedDisplay=Regex.Replace(cachedDisplay,@"(?m)^#{1,6}\s+","").Replace("*","");
            cachedHeight=body.CalcHeight(new GUIContent(cachedDisplay),1060)+30;documentScroll=Vector2.zero;
        }
        private void About()
        {
            Label(90,115,900,50,"A world, given time",heading);
            Label(90,204,1010,300,"Aevoria is a persistent procedural world and civilization simulation created by Sean Marsh. Geography, environment and the lives of its inhabitants shape each world's history.\n\nObserve a world from above, then explore the same terrain as a traveler. Settlements, regional ecology and weather, caves, excavation and saved world changes form the current foundation. Advanced diplomacy, dynasties and large-scale warfare remain future systems.\n\nProject Genesis is developing procedural terrain materials and natural regional environments across five world sizes. Historical worlds retain their recorded geography. Built with Unity 6.3 LTS and Universal Render Pipeline. Development assistance: Codex.",body);
            Label(90,558,1080,70,$"{ApplicationVersion.DisplayName} · {ApplicationVersion.Status}\nGenerator {ProceduralGeography.GeneratorVersion} · archive schema {WorldRecord.CurrentSchema}. Development milestones and open acceptance are recorded in the changelog.",muted);
        }
        private void Pause()
        {
            Label(90,112,1000,80,app.Current?.Name??"Aevoria",heading);
            Label(90,188,1000,30,"Paused",muted);
            bool enabled=GUI.enabled;GUI.enabled=enabled&&!app.Busy;
            if(Button(90,240,450,"Resume",52))Show(ApplicationPage.World);
            if(Button(90,310,450,"Settings",52))Show(ApplicationPage.Settings);
            if(Button(90,380,450,"Advancement",52))Show(ApplicationPage.Advancement);
            if(Button(90,450,450,"Save & Quit",52))SaveAndQuitToMainMenu();
            GUI.enabled=enabled;Label(90,525,600,50,"Save your world and return to the main menu.",muted);
        }
        private void WorldTools()
        {
            Label(90,115,1000,50,"World and traveler tools",heading);
            if(!app.WorldActive){Label(90,210,900,60,"Open a world to use these tools.",body);return;}
            if(Button(90,215,340,"Save world",40))app.SaveWorld();
            if(Button(90,267,340,"Save As · new archive",40)){string value=app.Current?.Name??"World";app.SaveWorld(duplicate:true,newName:value.Substring(0,Math.Min(75,value.Length))+" copy");}
            if(Button(90,319,340,"Load world",40))Show(ApplicationPage.Load);
            if(Button(90,371,340,"Character appearance",40))Show(ApplicationPage.Characters);
            if(Button(90,423,340,"World map",40)){Show(ApplicationPage.World);app.ToggleMap?.Invoke();}
            if(Button(90,475,340,app.Living.Traveler.Exploring?"World Mode":"Explore Mode",40)){Show(ApplicationPage.World);app.ToggleExploration?.Invoke();}
            if(Button(90,527,340,"Preload visible area",40)){app.Terrain.PreloadVisibleArea();Show(ApplicationPage.World);}
            if(Button(455,215,340,"Recenter world",40)){app.Controller.RecenterWorld();Show(ApplicationPage.World);}
            if(app.Living.Traveler.HasPosition&&Button(455,267,340,"Return to traveler",40)){app.Explorer.ReturnToPlayer();Show(ApplicationPage.World);}
            if(Button(455,319,340,"Civilizations and history",40)){Show(ApplicationPage.World);app.SocietyView.Open=true;}
            if(Button(455,371,340,"World overlays",40))Show(ApplicationPage.Overlays);
            if(Button(455,423,340,"Changelog",40))Show(ApplicationPage.Changelog);
            Label(835,215,340,40,"Simulation time",body);
            if(Button(835,267,340,app.Clock.Running?"Pause simulation":"Resume simulation",40)){if(app.Clock.Running)app.Clock.Pause();else app.Clock.Start();}
            for(int i=0;i<speeds.Length;i++)if(Button(835+(i%3)*115,330+(i/3)*45,108,speeds[i].ToString("0.##")+"×",36))app.Clock.SetSpeed(speeds[i]);
        }
        private void Characters()
        {
            Label(90,115,950,45,"Traveler appearance",heading);
            if(!app.WorldActive){Label(90,190,900,60,"Open a world to select your traveler.");return;}
            Label(90,180,1050,45,"Choose an appearance for this world's traveler. Movement and world simulation use the same rules.",body);
            if(Button(90,250,320,"Explorer",48))app.Explorer.SetAppearance(CharacterProfiles.Explorer);
            if(Button(430,250,320,"Roman soldier",48))app.Explorer.SetAppearance(CharacterProfiles.Roman);
            if(Button(770,250,320,"Doctor",48))app.Explorer.SetAppearance(CharacterProfiles.Doctor);
            Label(90,325,950,35,"Selected: "+app.Living.Traveler.CharacterPreset,body);
            Label(90,390,1000,50,"Your selected appearance is saved with this traveler.",body);
            bool enabled=GUI.Toggle(new Rect(90,535,500,30),app.Settings.CharacterDialogue,"Character dialogue subtitles",toggle);
            float duration=Slider(660,525,"Subtitle duration",app.Settings.DialogueDuration,3,15,"0.0 s");
            if(enabled!=app.Settings.CharacterDialogue||Math.Abs(duration-app.Settings.DialogueDuration)>.01f){var settings=app.Settings.Copy();settings.CharacterDialogue=enabled;settings.DialogueDuration=duration;app.ApplySettings(settings);}
            if(Button(90,615,320,"Return to world",38))Show(ApplicationPage.World);
        }
        private void Graphics()
        {
            if(draft==null)draft=app.Settings.Copy();Label(90,110,900,45,"Graphics and atmosphere",heading);
            if(Button(660,145,165,"Balanced",32))draft.ApplyGraphicsPreset(0);if(Button(835,145,165,"High Quality",32))draft.ApplyGraphicsPreset(1);if(Button(1010,145,170,"Ultra",32))draft.ApplyGraphicsPreset(2);
            graphicsScroll=GUI.BeginScrollView(new Rect(90,195,1100,395),graphicsScroll,new Rect(0,0,1060,1200));
            draft.TerrainDetail=Mathf.RoundToInt(Slider(0,10,"Terrain detail",draft.TerrainDetail,0,3,"0"));
            draft.TerrainDistance=Mathf.RoundToInt(Slider(0,85,"Local terrain view radius",draft.TerrainDistance,2,10,"0 chunks"));
            draft.MountainDetail=Mathf.RoundToInt(Slider(0,160,"Mountain geometry",draft.MountainDetail,0,3,"0"));
            draft.DistantDetail=Mathf.RoundToInt(Slider(0,235,"Regional mesh detail (adapts to view)",draft.DistantDetail,0,2,"0"));
            draft.VegetationDistance=Slider(0,310,"Vegetation render distance",draft.VegetationDistance,200,8000,"0 m");
            draft.VegetationDensity=Slider(0,385,"Vegetation density",draft.VegetationDensity,.1f,2,"0.0×");
            draft.WaterQuality=Mathf.RoundToInt(Slider(0,460,"Water detail",draft.WaterQuality,0,2,"0"));
            draft.WaterTransparency=Mathf.RoundToInt(Slider(0,710,"Water transparency detail",draft.WaterTransparency,0,2,"0"));
            draft.LodTransition=Mathf.RoundToInt(Slider(550,620,"LOD transition quality",draft.LodTransition,0,2,"0"));
            draft.ShadowDistance=Slider(0,535,"Shadow distance",draft.ShadowDistance,100,10000,"0 m");
            draft.ShowFps=GUI.Toggle(new Rect(0,620,480,28),draft.ShowFps,"Compact FPS counter",toggle);draft.ShowFrameTime=GUI.Toggle(new Rect(0,660,480,28),draft.ShowFrameTime,"Compact frame time",toggle);
            draft.FogEnabled=GUI.Toggle(new Rect(550,10,490,28),draft.FogEnabled,"Atmospheric fog enabled",toggle);
            draft.FogDensity=Slider(550,65,"Fog density",draft.FogDensity,0,3,"0.0×");draft.FogDistance=Slider(550,140,"Fog distance",draft.FogDistance,1000,100000,"0 m");draft.HazeStrength=Slider(550,215,"Atmospheric haze",draft.HazeStrength,0,3,"0.0×");
            draft.CloudQuality=Mathf.RoundToInt(Slider(550,290,"Cloud volume quality (0 disables)",draft.CloudQuality,0,2,"0"));
            draft.DistantScenery=Slider(550,365,"Distant scenery contrast",draft.DistantScenery,0,2,"0.0×");
            draft.RenderClouds=GUI.Toggle(new Rect(550,760,490,28),draft.RenderClouds,"Render clouds and cloud shadows",toggle);
            draft.RenderWeatherEffects=GUI.Toggle(new Rect(550,805,490,28),draft.RenderWeatherEffects,"Render weather effects",toggle);
            draft.CloudDensity=GUI.SelectionGrid(new Rect(550,855,490,70),draft.CloudDensity,new[]{"Off","Sparse","Normal","Overcast"},2,button);
            draft.TerrainTextureQuality=Mathf.RoundToInt(Slider(0,800,"Terrain texture quality",draft.TerrainTextureQuality,0,2,"0"));
            draft.StreamingAggressiveness=Mathf.RoundToInt(Slider(0,880,"Streaming lookahead",draft.StreamingAggressiveness,0,2,"0"));
            draft.BackgroundStreamingBudget=Mathf.RoundToInt(Slider(0,960,"Background terrain workers",draft.BackgroundStreamingBudget,1,4,"0"));
            draft.PresentationCacheMiB=Mathf.RoundToInt(Slider(550,965,"Presentation cache RAM budget",draft.PresentationCacheMiB,64,1024,"0 MiB"));
            Label(0,1060,1040,90,"Preload Visible Area is available in World and traveler tools. It reports installed terrain and vegetation and can be cancelled. Moving the camera cancels that snapshot. Regional and planetary terrain cover distant pixels; full-resolution planetary geometry is not held in GPU memory.",muted);
            GUI.Label(new Rect(550,450,480,150),"Higher distances and density cost GPU time. Local, regional and planetary meshes remain bounded. Fog off removes atmospheric extinction. Weather simulation continues when cloud rendering is disabled.",body);
            GUI.EndScrollView();if(Button(90,615,480,"Apply graphics")){app.ApplySettings(draft);Show(settingsReturn);}if(Button(660,615,480,"Cancel"))Show(settingsReturn);
        }
        private void LoadingBackground()
        {
            var snapshot=app.PreparationPreview;if(snapshot==null){loadingSnapshot=null;if(loadingPreview!=null){Destroy(loadingPreview);loadingPreview=null;}return;}
            if(!ReferenceEquals(snapshot,loadingSnapshot))
            {
                loadingSnapshot=snapshot;if(loadingPreview==null)loadingPreview=new Texture2D(GenerationPreview.Resolution,GenerationPreview.Resolution,TextureFormat.RGBA32,false){name="Current world generation preview",filterMode=FilterMode.Bilinear};
                var pixels=new Color[snapshot.Elevation.Length];int side=GenerationPreview.Resolution;
                for(int z=0;z<side;z++)for(int x=0;x<side;x++)
                {
                    int i=z*side+x;float h=snapshot.Elevation[i]-snapshot.SeaLevel;Color color;
                    if(h<0)color=Color.Lerp(new Color(.035f,.18f,.26f),new Color(.06f,.42f,.47f),Mathf.Exp(h/250));
                    else {float moisture=snapshot.Moisture==null?.5f:snapshot.Moisture[i];color=Color.Lerp(new Color(.58f,.45f,.23f),new Color(.16f,.35f,.23f),moisture);color=Color.Lerp(color,new Color(.47f,.47f,.43f),Mathf.SmoothStep(0,1,h/Mathf.Max(1000,snapshot.Relief)));if(snapshot.Temperature!=null&&snapshot.Temperature[i]<-2)color=Color.Lerp(color,new Color(.78f,.85f,.86f),Mathf.InverseLerp(-2,-15,snapshot.Temperature[i]));float dx=snapshot.Elevation[z*side+Mathf.Max(0,x-1)]-snapshot.Elevation[z*side+Mathf.Min(side-1,x+1)],dz=snapshot.Elevation[Mathf.Max(0,z-1)*side+x]-snapshot.Elevation[Mathf.Min(side-1,z+1)*side+x];color*=Mathf.Clamp(.9f+(dx+dz)/Mathf.Max(500,snapshot.Relief)*.7f,.55f,1.2f);}
                    if(h>=0&&snapshot.Biomes!=null)
                    {
                        var biome=snapshot.Biomes[i];if(biome==Biome.Desert||biome==Biome.ColdDesert)color=Color.Lerp(color,new Color(.63f,.50f,.28f),.7f);
                        if(biome==Biome.Tundra)color=Color.Lerp(color,new Color(.41f,.45f,.32f),.6f);if(biome==Biome.Snow)color=new Color(.8f,.86f,.9f);
                    }
                    if(snapshot.Forest!=null&&h>=0)color=Color.Lerp(color,new Color(.055f,.21f,.095f),snapshot.Forest[i]*.75f);
                    if(snapshot.Water!=null)color=Color.Lerp(color,new Color(.09f,.52f,.67f),snapshot.Water[i]*.9f);
                    pixels[i]=color;
                }
                if(snapshot.Settlements!=null)foreach(var p in snapshot.Settlements){int x=Mathf.Clamp(Mathf.RoundToInt((float)(p.X/snapshot.Width+.5)*(side-1)),0,side-1),z=Mathf.Clamp(Mathf.RoundToInt((float)(p.Y/snapshot.Depth+.5)*(side-1)),0,side-1);for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(x+dx>=0&&z+dz>=0&&x+dx<side&&z+dz<side)pixels[(z+dz)*side+x+dx]=new Color(.96f,.74f,.3f);}
                loadingPreview.SetPixels(pixels);loadingPreview.Apply(false);
            }
            GUI.DrawTexture(new Rect(545,105,665,545),loadingPreview,ScaleMode.ScaleToFit);
        }
        private void Loading()
        {
            Panel(new Rect(90,150,600,435),new Color(.015f,.035f,.055f,.86f));Label(120,185,560,65,app.LoadingExistingWorld?"A world returns":"A world takes shape",loadingTitle);Label(125,265,540,35,app.LoadingExistingWorld?"Loading an existing world":"Creating a new world",heading);Label(125,322,530,80,app.Progress,body);
            int count=app.PreparationStageCount,stage=Mathf.Clamp(app.PreparationStage,0,count-1);Label(125,405,530,30,$"Stage {stage+1} of {count}",muted);float spacing=520f/count;for(int i=0;i<count;i++)Panel(new Rect(125+i*spacing,445,spacing-5,7),i<stage?gold:i==stage?new Color(.8f,.68f,.43f,.55f):new Color(.25f,.3f,.32f,.8f));
            var diagnostics=app.PreparationDiagnostics;
            string activity=diagnostics==null?"Preparing world data":diagnostics.Outcome==Aevoria.World.GenerationOutcome.Running?$"{diagnostics.ElapsedSeconds:F0}s elapsed | last work report {diagnostics.SecondsSinceHeartbeat:F1}s ago\n{diagnostics.Detail}":$"Geography: {diagnostics.Outcome} in {diagnostics.ElapsedSeconds:F0}s";
            Label(125,470,530,55,activity,small);if(Button(125,535,450,"Cancel preparation"))app.CancelPreparation();if(loadingSnapshot!=null)Label(715,613,490,50,loadingSnapshot.Stage+"\nSeed: "+loadingSnapshot.Seed,small);
        }
        private void Audio()
        {if(draft==null)draft=app.Settings.Copy();Label(90,115,900,45,"Audio",heading);draft.MasterVolume=Slider(90,220,"Master volume",draft.MasterVolume,0,1,"P0");Label(90,330,1000,80,"Audio settings foundation. The current prototype does not include a soundtrack or a complete environmental audio library.",body);if(Button(90,590,450,"Apply")){app.ApplySettings(draft);Show(settingsReturn);}}        private static string Date(string text)=>DateTime.TryParse(text,out var value)?value.ToLocalTime().ToString("MMM d, yyyy · HH:mm"):"Unknown date";
        private void OnDestroy() {if(font!=null) Destroy(font);foreach(var texture in new[]{normalButton,hoverButton,activeButton,inputBackground,loadingPreview,pauseBackdrop}) if(texture!=null) Destroy(texture);}
    }
}

