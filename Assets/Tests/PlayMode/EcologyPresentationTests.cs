using System.Collections;
using Aevoria.Presentation;
using Aevoria.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Aevoria.Tests
{
    public sealed class EcologyPresentationTests
    {
        [UnityTest] public IEnumerator WildlifeAndInspectionDoNotChangeAuthorityAndReleaseResources()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();if(app==null)app=new GameObject("Ecology test").AddComponent<AevoriaBootstrap>();
            app.ReturnToMenu();yield return null;yield return null;
            app.Regenerate("ecosystem-005",WorldSize.Small,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));app.Clock.Pause();
            var eco=app.Ecology;int region=0;for(int i=1;i<eco.Count;i++)if(eco.Population(i,0)>eco.Population(region,0))region=i;
            app.Controller.ExternalDriver=true;app.Controller.SetView(eco.EnvironmentAt(region).Center,25,45,250);
            var visuals=app.GetComponent<EcologyPresentation>();visuals.Inspect=true;
            var populations=new double[4];for(int s=0;s<4;s++)populations[s]=eco.TotalPopulation(s);long tick=eco.Tick;
            yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual(region,visuals.SelectedRegion);Assert.LessOrEqual(visuals.VisibleAnimals,EcologyPresentation.AnimalLimit);
            for(int s=0;s<4;s++)Assert.AreEqual(populations[s],eco.TotalPopulation(s));Assert.AreEqual(tick,eco.Tick);
            Assert.AreEqual(1,Shader.GetGlobalFloat("_AevoriaEcologyReady"));
            app.ReturnToMenu();yield return null;yield return null;
            Assert.IsNull(app.Ecology);Assert.AreEqual(0,Object.FindObjectsByType<EcologyPresentation>(FindObjectsSortMode.None).Length);Assert.AreEqual(0,Shader.GetGlobalFloat("_AevoriaEcologyReady"));
            foreach(var mesh in Resources.FindObjectsOfTypeAll<Mesh>())Assert.IsFalse(mesh.name.StartsWith("Procedural Meadow deer")||mesh.name.StartsWith("Procedural Woodland deer")||mesh.name=="Procedural Hare"||mesh.name=="Procedural Wolf");
        }
    }
}
