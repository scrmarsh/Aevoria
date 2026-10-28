using System.Collections;
using System.IO;
using Aevoria.Presentation;
using Aevoria.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Aevoria.Tests
{
    public sealed class MenuLifecycleTests
    {
        [UnityTest] public IEnumerator MenuNavigationAndRepeatedWorldCleanupKeepSingleServices()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();if(app==null) app=new GameObject("menu test").AddComponent<AevoriaBootstrap>();
            app.ReturnToMenu();yield return null;yield return null;
            Assert.AreEqual(ApplicationPage.Home,app.Menu.Page);Assert.IsNull(app.Terrain);Assert.IsFalse(app.Controller.enabled);
            foreach(var page in new[]{ApplicationPage.Create,ApplicationPage.Load,ApplicationPage.Settings,ApplicationPage.Changelog,ApplicationPage.Chronicles,ApplicationPage.About}) {app.Menu.Show(page);yield return null;Assert.AreEqual(page,app.Menu.Page);Assert.IsNull(app.Terrain);}
            for(int n=0;n<2;n++)
            {
                app.Regenerate("menu-cleanup-"+n,WorldSize.Small,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));yield return null;
                Assert.AreEqual(1,Object.FindObjectsByType<WorldRenderer>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(1,Object.FindObjectsByType<WorldWaterFeatures>(FindObjectsSortMode.None).Length);
                app.ReturnToMenu();yield return null;yield return null;
                Assert.AreEqual(0,Object.FindObjectsByType<WorldRenderer>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(0,Object.FindObjectsByType<WorldWaterFeatures>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(0,Object.FindObjectsByType<WorldEnvironment>(FindObjectsSortMode.None).Length);
            }
            Assert.AreEqual(1,Object.FindObjectsByType<WorldAtmosphere>(FindObjectsSortMode.None).Length);
        }
    }
}
