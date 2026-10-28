using System.Collections;
using Aevoria.World;
using Aevoria.Persistence;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Aevoria.Tests
{
    public sealed class RestorationAddendumPlayTests
    {
        [UnityTest,Timeout(300000)]public IEnumerator InstalledPreloadCancellationCacheReuseAndIndependentAtmosphere()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();if(app==null)app=new GameObject("addendum play test").AddComponent<AevoriaBootstrap>();
            app.ReturnToMenu();yield return null;app.Regenerate("addendum-preload",WorldSize.Miniature,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));app.Clock.Pause();app.Controller.ExternalDriver=true;
            float end=Time.realtimeSinceStartup+90;while(!app.Terrain.IsReady&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.Terrain.IsReady);
            app.Terrain.PreloadVisibleArea();Assert.IsTrue(app.Terrain.Preloading);Assert.Less(app.Terrain.PreloadProgress,1);app.Terrain.CancelPreload();Assert.IsFalse(app.Terrain.Preloading);Assert.AreEqual(3,app.Terrain.BaseMeshes);
            app.Terrain.PreloadVisibleArea();end=Time.realtimeSinceStartup+90;while(app.Terrain.Preloading&&Time.realtimeSinceStartup<end)yield return null;Assert.IsFalse(app.Terrain.Preloading);Assert.AreEqual(1,app.Terrain.PreloadProgress);Assert.IsTrue(app.Terrain.IsReady);
            var environment=Object.FindFirstObjectByType<WorldEnvironment>();var clouds=Object.FindFirstObjectByType<WorldCloudLayer>();var settings=app.Settings.Copy();settings.CloudQuality=1;settings.RenderClouds=true;settings.RenderWeatherEffects=false;settings.CloudDensity=2;
            string before=ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture()));app.ApplySettings(settings,false);yield return null;Assert.IsFalse(environment.RenderWeatherEffects);Assert.Greater(clouds.VisibleSystems,0);
            settings.RenderClouds=false;settings.RenderWeatherEffects=true;app.ApplySettings(settings,false);yield return null;Assert.IsTrue(environment.RenderWeatherEffects);Assert.AreEqual(0,clouds.VisibleSystems);Assert.AreEqual(0,Shader.GetGlobalFloat("_AevoriaRenderClouds"));Assert.AreEqual(before,ArchiveCodec.Hash(ArchiveCodec.Encode(app.Living.Capture())));
            string fingerprint=WorldRecord.GeographyFingerprint(app.Terrain.Geography);app.Regenerate("addendum-preload",WorldSize.Miniature,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));app.Clock.Pause();end=Time.realtimeSinceStartup+90;while(!app.Terrain.IsReady&&Time.realtimeSinceStartup<end)yield return null;
            Assert.IsTrue(app.Terrain.IsReady);Assert.Greater(app.Terrain.CacheHits,0);Assert.AreEqual(fingerprint,WorldRecord.GeographyFingerprint(app.Terrain.Geography));
            app.ReturnToMenu();yield return null;yield return null;Assert.AreEqual(0,Object.FindObjectsByType<DistantMountainHorizon>(FindObjectsSortMode.None).Length);Assert.AreEqual(0,Object.FindObjectsByType<WorldCloudLayer>(FindObjectsSortMode.None).Length);
        }
    }
}
