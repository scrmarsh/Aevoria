using System;
using System.Collections.Generic;
namespace Aevoria.Core
{
    [Flags] public enum BindingContext {World=1,Explore=2,Gameplay=3}
    public enum GameAction
    {
        Forward,Backward,Left,Right,Sprint,Jump,Crouch,Inventory,Map,Explore,Perspective,Cursor,
        Tool1,Tool2,Tool3,Tool4,Tool5,Tool6,Primary,Secondary,Pan,RotateLeft,RotateRight,ZoomIn,ZoomOut,Recenter,Focus,
        Diagnostics,Ecosystems,Console,WorldRise,WorldLower,Orbit,Fullscreen
    }
    [Serializable] public sealed class KeyBinding
    {
        public GameAction Action;public string Input;
        public KeyBinding(){} public KeyBinding(GameAction action,string input){Action=action;Input=input;}
    }
    public static class InputBindings
    {
        private static readonly string[] defaultInputs={"W","S","A","D","LeftCtrl","Space","LeftShift","E","M","T","F5","Tab","Digit1","Digit2","Digit3","Digit4","Digit5","Digit6","MouseLeft","MouseRight","MouseMiddle","Q","E","PageUp","PageDown","R","F","F3","F4","Slash","Space","LeftShift","MouseRight","F11"};
        private static readonly string[] labels={"Move forward","Move backward","Move left","Move right","Sprint / flight boost","Jump / fly upward","Crouch / fly downward","Inventory","World map","Enter / leave Explore Mode","Camera perspective","Release / capture cursor","Tool slot 1","Tool slot 2","Tool slot 3","Tool slot 4","Tool slot 5","Tool slot 6","Use tool / mine","Place item","Drag camera","Rotate camera left","Rotate camera right","Zoom in","Zoom out","Reset camera orientation","Focus at pointer","Developer diagnostics","Ecosystem maps","Command console","Raise world camera","Lower world camera","Orbit camera","Toggle fullscreen"};
        public static string Label(GameAction action)=>labels[(int)action];
        public static string ContextLabel(GameAction action)=>Context(action)==BindingContext.Gameplay?"World + Explore":Context(action)==BindingContext.World?"World":"Explore";
        public static BindingContext Context(GameAction action)
        {
            switch(action)
            {
                case GameAction.Jump:case GameAction.Crouch:case GameAction.Inventory:case GameAction.Perspective:case GameAction.Cursor:
                case GameAction.Tool1:case GameAction.Tool2:case GameAction.Tool3:case GameAction.Tool4:case GameAction.Tool5:case GameAction.Tool6:case GameAction.Primary:case GameAction.Secondary:return BindingContext.Explore;
                case GameAction.WorldRise:case GameAction.WorldLower:case GameAction.Orbit:case GameAction.Pan:case GameAction.RotateLeft:case GameAction.RotateRight:case GameAction.ZoomIn:case GameAction.ZoomOut:case GameAction.Recenter:case GameAction.Focus:case GameAction.Ecosystems:return BindingContext.World;
                default:return BindingContext.Gameplay;
            }
        }
        public static List<KeyBinding> Defaults()
        {
            var result=new List<KeyBinding>();foreach(GameAction action in Enum.GetValues(typeof(GameAction)))result.Add(new KeyBinding(action,defaultInputs[(int)action]));return result;
        }
        public static string Input(List<KeyBinding> bindings,GameAction action)
        {if(bindings!=null)foreach(var binding in bindings)if(binding.Action==action)return binding.Input;return defaultInputs[(int)action];}
        public static string Conflict(List<KeyBinding> bindings,GameAction action,string input)
        {
            if(string.Equals(input,"Escape",StringComparison.OrdinalIgnoreCase))return "Escape is reserved for closing menus and cancelling input capture.";
            foreach(GameAction other in Enum.GetValues(typeof(GameAction)))if(other!=action&&(Context(other)&Context(action))!=0&&string.Equals(Input(bindings,other),input,StringComparison.OrdinalIgnoreCase))return input+" is already assigned to "+Label(other)+" in the same context.";
            return null;
        }
        public static List<KeyBinding> Copy(List<KeyBinding> bindings)
        {var result=new List<KeyBinding>();if(bindings!=null)foreach(var b in bindings)result.Add(new KeyBinding(b.Action,b.Input));return result;}
        public static void Validate(List<KeyBinding> bindings)
        {
            if(bindings==null)return;var seen=new HashSet<GameAction>();
            foreach(var b in bindings){if(b==null||!Enum.IsDefined(typeof(GameAction),b.Action)||!seen.Add(b.Action)||string.IsNullOrWhiteSpace(b.Input)||b.Input.Length>32||Conflict(bindings,b.Action,b.Input)!=null)throw new ArgumentException("Invalid or conflicting input bindings.");}
        }
    }
}
