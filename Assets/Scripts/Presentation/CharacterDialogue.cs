using System;
using Aevoria.World;
using UnityEngine;

namespace Aevoria.Presentation
{
    // Presentation-only stream: it never reads or changes the society RNG.
    public sealed class CharacterDialogue:MonoBehaviour
    {
        private AevoriaBootstrap app;private ExperimentalExplorer explorer;private readonly System.Random random=new System.Random(7371);
        private int last=-1;private float next,started=-1000,contextAfter,biomeAt;private Biome? biome;private bool falling;
        public string LastLine=>last<0?"":CharacterProfiles.DoctorDialogue[last];
        public void Initialize(AevoriaBootstrap value,ExperimentalExplorer traveler){app=value;explorer=traveler;Schedule();}
        private bool Active=>app?.Living?.Traveler.Exploring==true&&app.Living.Traveler.CharacterPreset==CharacterProfiles.Doctor&&app.Settings.CharacterDialogue&&app.Menu.Page==ApplicationPage.World;
        private void Schedule()=>next=Time.unscaledTime+45+(float)random.NextDouble()*75;
        public void Speak(int preferred=-1)
        {
            if(!Active||Time.unscaledTime<contextAfter)return;int line=preferred;
            if(line<0||line>=CharacterProfiles.DoctorDialogue.Length||line==last){line=random.Next(CharacterProfiles.DoctorDialogue.Length-1);if(line>=last&&last>=0)line++;}
            last=line;started=Time.unscaledTime;contextAfter=started+20;Schedule();
        }
        private void Update()
        {
            if(!Active){started=-1000;falling=false;biome=null;return;}
            if(Time.unscaledTime>=next)Speak();bool now=explorer.Falling;if(now&&!falling)Speak(7);falling=now;
            if(Time.unscaledTime>=biomeAt){biomeAt=Time.unscaledTime+2;var p=explorer.GeographicPosition;var current=app.Terrain.Geography.Sample(new GeoPoint(p.X,p.Z)).Biome;if(biome.HasValue&&current!=biome.Value)Speak(1);biome=current;}
        }
        private void OnGUI()
        {
            if(!Active||GameInput.TextEntry||explorer.InventoryOpen||app.Navigation?.Open==true)return;float elapsed=Time.unscaledTime-started,duration=app.Settings.DialogueDuration;if(elapsed<0||elapsed>=duration)return;
            float fade=Mathf.Min(Mathf.Clamp01(elapsed/.3f),Mathf.Clamp01((duration-elapsed)/.6f));var matrix=GUI.matrix;var color=GUI.color;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));GUI.color=new Color(1,1,1,fade);
            GUI.Box(new Rect(340,548,600,68),"");var style=new GUIStyle(GUI.skin.label){fontSize=18,alignment=TextAnchor.MiddleCenter,wordWrap=true};GUI.Label(new Rect(354,554,572,56),LastLine,style);GUI.color=color;GUI.matrix=matrix;
        }
    }
}
