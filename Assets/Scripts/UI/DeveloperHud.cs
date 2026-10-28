using System;
using Aevoria.Core;
using Aevoria.Simulation;
using Aevoria.World;
using UnityEngine;
using Unity.Profiling;

namespace Aevoria.UI
{
    // Fixed rectangles avoid GUILayout's per-event layout tree allocations.
    // Dynamic strings refresh at 4 Hz; input remains responsive every event.
    public sealed class DeveloperHud : MonoBehaviour
    {
        private static readonly ProfilerMarker HudMarker=new ProfilerMarker("Aevoria.HUD");
        public static bool IsEditingText { get; private set; }
        private static Rect Area=>new Rect(12,12,350,Mathf.Min(850,Screen.height-55));
        private static bool pointerAreaActive;
        public static bool IsPointerOverHud(Vector2 p)=>pointerAreaActive && Area.Contains(new Vector2(p.x,Screen.height-p.y));
        public SimulationClock Clock { get; set; }
        public Action<string,WorldSize,GenerationSettings> Regenerate { get; set; }
        public Func<string> Diagnostics { get; set; }
        public Action<bool,bool,float,bool> CameraOptions { get; set; }
        public bool ControlsEnabled { get; set; }=true;
        public bool Visible { get; set; }=true;
        private readonly string[] labels=new string[9];
        private static readonly string[] Presets={"Small","Medium","Large"};
        private static readonly string[] SpeedLabels={"0.25","0.5","1","2","4","8"};
        private static readonly float[] Speeds={0.25f,0.5f,1,2,4,8};
        private string seed="aevoria-001",error="",cachedDiagnostics="Preparing world";
        private int preset=1;
        private float mountains=600,roughness=0.65f,sea,land=0.675f,zoom=1.1f,frameMs,diagnosticsAt;
        private bool following,collision,blur;
        private Vector2 scroll;
        private void Update()
        {
            pointerAreaActive=Visible;
            if(!Visible) IsEditingText=false;
            frameMs=Mathf.Lerp(frameMs==0?Time.unscaledDeltaTime*1000:frameMs,Time.unscaledDeltaTime*1000,0.1f);
            if(Clock==null || Time.unscaledTime<diagnosticsAt) return;
            cachedDiagnostics=Diagnostics?.Invoke()??"Preparing world";diagnosticsAt=Time.unscaledTime+0.25f;
            var calendar=new WorldCalendar(Clock.State.Tick);
            labels[0]=$"{(Clock.Running?"Running":"Paused")} / year {calendar.Year}, day {calendar.Day} / {calendar.SeasonName}";
            labels[1]=$"Population {Clock.State.Population} / food {Clock.State.Food} / {Clock.Speed:0.##}x";
            labels[2]=$"Engine loop {1000/frameMs:0.0} FPS / {frameMs:0.00} ms";
            labels[3]=$"Tick {Clock.LastTickMilliseconds:0.###} ms / debt {Clock.BacklogSeconds:0.00}s";
            labels[4]=$"Mountains: {mountains:0} m";labels[5]=$"Roughness: {roughness:0.00}";
            labels[6]=$"Sea level: {sea:0} m";labels[7]=$"Target land: {land:P0}";labels[8]=$"Zoom sensitivity: {zoom:0.0}x";
        }
        private void OnDisable() {IsEditingText=false;pointerAreaActive=false;}
        private void OnGUI()
        {
            using var measured=HudMarker.Auto();
            using var work=WorkCounter.Hud.Measure(Time.frameCount);
            if(Clock==null || !Visible) return;
            IsEditingText=false;GUI.Box(Area,GUIContent.none);GUI.BeginGroup(Area);
            scroll=GUI.BeginScrollView(new Rect(5,5,340,Area.height-10),scroll,new Rect(0,0,320,920));
            GUI.Label(new Rect(0,0,320,24),ApplicationVersion.DisplayName+" · F3 diagnostics");
            GUI.Label(new Rect(0,27,320,24),labels[2]??"");GUI.Label(new Rect(0,51,320,24),labels[0]??"");
            GUI.Label(new Rect(0,80,320,760),cachedDiagnostics);
            GUI.Label(new Rect(0,855,320,55),"F4: ecosystem maps · Escape: game menu\nEngine-loop timing; display presentation unmeasured.");
            GUI.EndScrollView();GUI.EndGroup();        }
    }
}
