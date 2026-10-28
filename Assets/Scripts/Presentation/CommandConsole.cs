using System;
using System.Collections.Generic;
using Aevoria.Core;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Aevoria.Presentation
{
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string,Func<string[],string>> commands=new Dictionary<string,Func<string[],string>>(StringComparer.OrdinalIgnoreCase);
        public void Register(string name,Func<string[],string> command)
        {if(string.IsNullOrWhiteSpace(name)||name.IndexOf(' ')>=0||command==null)throw new ArgumentException("A command needs a single name and handler.");commands.Add(name,command);}
        public string Dispatch(string input)
        {var parts=(input??"").Trim().TrimStart('/').Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);if(parts.Length==0)return "Commands are not yet available.";if(!commands.TryGetValue(parts[0],out var command))return "Unknown command. Gameplay commands are not yet available.";var args=new string[parts.Length-1];Array.Copy(parts,1,args,0,args.Length);return command(args);}
    }
    [DefaultExecutionOrder(-1000)]
    public sealed class CommandConsole:MonoBehaviour
    {
        private AevoriaBootstrap app;private string input="",response="";private bool focus;
        public bool Open{get;private set;}public readonly CommandRegistry Registry=new CommandRegistry();
        public void Initialize(AevoriaBootstrap value){app=value;}
        public void Close(){Open=false;GameInput.TextEntry=false;GameInput.ConsumedFrame=Time.frameCount;GUIUtility.keyboardControl=0;}
        private void Update()
        {
            if(app?.Menu?.Page!=ApplicationPage.World){if(Open)Close();return;}
            if(Open){GameInput.TextEntry=true;if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)Close();return;}
            if(!Aevoria.UI.DeveloperHud.IsEditingText&&GUIUtility.keyboardControl==0&&app.Navigation?.Open!=true&&app.Explorer?.InventoryOpen!=true&&GameInput.Pressed(GameAction.Console))
            {Open=true;GameInput.TextEntry=true;input="";response="";focus=true;}
        }
        private void OnGUI()
        {
            if(!Open)return;var matrix=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            GUI.Box(new Rect(25,542,720,103),"");GUI.Label(new Rect(40,551,680,24),response.Length==0?"Command entry":response);
            GUI.SetNextControlName("AevoriaCommandEntry");input=GUI.TextField(new Rect(40,586,685,36),input,256);
            if(focus&&Event.current.type==EventType.Repaint){GUI.FocusControl("AevoriaCommandEntry");focus=false;}
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Return){try{response=Registry.Dispatch(input);}catch(Exception ex){response="Command failed: "+ex.Message;}input="";Event.current.Use();}
            GUI.matrix=matrix;
        }
        private void OnDestroy(){GameInput.TextEntry=false;}
    }
}
