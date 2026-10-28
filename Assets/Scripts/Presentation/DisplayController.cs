using System.Collections;
using UnityEngine;
namespace Aevoria.Presentation
{
    public sealed class DisplayController : MonoBehaviour
    {
        private Coroutine pending;private int windowWidth=1600,windowHeight=900;
        public void Request(int width,int height,FullScreenMode mode)
        {
            if(Screen.fullScreenMode==FullScreenMode.Windowed&&mode!=FullScreenMode.Windowed){windowWidth=Screen.width;windowHeight=Screen.height;}
            if(pending!=null)StopCoroutine(pending);Screen.SetResolution(width,height,mode);pending=StartCoroutine(VerifyWindow(width,height,mode));
        }
        private IEnumerator VerifyWindow(int width,int height,FullScreenMode mode)
        {
            // Resolution changes are deferred by Unity. Reapply the remembered
            // client dimensions after leaving fullscreen if the first transition
            // retained the old fullscreen dimensions.
            for(int i=0;i<4;i++){yield return null;if(mode==FullScreenMode.Windowed&&(Screen.width!=width||Screen.height!=height))Screen.SetResolution(width,height,mode);}
            pending=null;
        }
        public void Toggle(AevoriaBootstrap app)
        {
            var settings=app.Settings.Copy();
            if(Screen.fullScreenMode==FullScreenMode.Windowed)
            {windowWidth=Screen.width;windowHeight=Screen.height;var display=Screen.mainWindowDisplayInfo;settings.DisplayMode=1;settings.DisplayWidth=Mathf.Max(800,display.width);settings.DisplayHeight=Mathf.Max(600,display.height);}
            else{settings.DisplayMode=0;settings.DisplayWidth=Mathf.Max(800,windowWidth);settings.DisplayHeight=Mathf.Max(600,windowHeight);}
            app.ApplySettings(settings);
        }
    }
}
