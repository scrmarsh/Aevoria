using System;
using Aevoria.Core;
using Aevoria.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
namespace Aevoria.Presentation
{
    public static class GameInput
    {
        public static ApplicationSettings Settings;
        public static bool TextEntry;public static int ConsumedFrame=-1;
        public static bool Blocked=>TextEntry||ConsumedFrame==Time.frameCount;
        public static void Validate(ApplicationSettings settings)
        {foreach(var binding in settings.Bindings){string input=binding.Input;if(input=="MouseLeft"||input=="MouseRight"||input=="MouseMiddle"||input=="MouseBack"||input=="MouseForward")continue;if(!Enum.TryParse(input,out Key key)||key==Key.None||key==Key.Escape)throw new ArgumentException("Unsupported input binding: "+input);}}
        private static ButtonControl Control(GameAction action)
        {
            string input=InputBindings.Input(Settings?.Bindings,action);var mouse=Mouse.current;
            switch(input){case "MouseLeft":return mouse?.leftButton;case "MouseRight":return mouse?.rightButton;case "MouseMiddle":return mouse?.middleButton;case "MouseBack":return mouse?.backButton;case "MouseForward":return mouse?.forwardButton;}
            return Enum.TryParse(input,out Key key)&&key!=Key.None?Keyboard.current?[key]:null;
        }
        public static bool Held(GameAction action)=>!Blocked&&Control(action)?.isPressed==true;
        public static bool Pressed(GameAction action)=>!Blocked&&Control(action)?.wasPressedThisFrame==true;
        public static float WheelNotches()
        {float value=Mouse.current?.scroll.ReadValue().y??0;if(Mathf.Abs(value)>10)value/=120;return Mathf.Clamp(value,-8,8);}
    }
}
