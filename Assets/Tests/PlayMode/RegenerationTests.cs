using System.Collections;
using Aevoria.World;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Aevoria.Tests
{
    public sealed class RegenerationTests
    {
        [UnityTest] public IEnumerator EvictedLodOnlyResultDoesNotInstallAMissingForestBatch()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();
            if(app==null) app=new GameObject("late job test").AddComponent<AevoriaBootstrap>();
            app.Regenerate("late-result",WorldSize.Small,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));
            float deadline=Time.realtimeSinceStartup+30;
            while(!app.Terrain.IsReady && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.IsTrue(app.Terrain.IsReady);
            // Inject a completed LOD-only result whose original tile has gone,
            // reproducing fast leave-and-return independently of worker timing.
            var type=typeof(WorldRenderer);var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var key=new ChunkKey(10,10);int before=app.Terrain.ActiveChunks;
            var generate=type.GetMethod("GeneratePayload",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            var payload=generate.Invoke(null,new object[]{app.Terrain.Geography,key,1,false,System.Threading.CancellationToken.None});
            var task=typeof(System.Threading.Tasks.Task).GetMethod("FromResult").MakeGenericMethod(payload.GetType()).Invoke(null,new[]{payload});
            var jobType=type.GetNestedType("Job",System.Reflection.BindingFlags.NonPublic);var job=System.Activator.CreateInstance(jobType,true);
            jobType.GetField("Key").SetValue(job,key);jobType.GetField("Lod").SetValue(job,1);jobType.GetField("Task").SetValue(job,task);
            jobType.GetField("Epoch").SetValue(job,type.GetField("epoch",flags).GetValue(app.Terrain));
            var wanted=(System.Collections.Generic.Dictionary<ChunkKey,int>)type.GetField("desired",flags).GetValue(app.Terrain);wanted[key]=1;
            ((System.Collections.IList)type.GetField("candidates",flags).GetValue(app.Terrain)).Clear();
            ((System.Collections.IList)type.GetField("jobs",flags).GetValue(app.Terrain)).Add(job);
            type.GetField("refreshAt",flags).SetValue(app.Terrain,float.PositiveInfinity);
            type.GetMethod("Update",flags).Invoke(app.Terrain,null);
            Assert.AreEqual(before,app.Terrain.ActiveChunks);Assert.AreEqual(0,app.Terrain.PendingChunks);
            wanted.Remove(key);type.GetField("refreshAt",flags).SetValue(app.Terrain,0f);
        }
        [UnityTest] public IEnumerator RapidLodChangesKeepFallbackAndBoundTemporaryMeshes()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();
            if(app==null) app=new GameObject("LOD stress test").AddComponent<AevoriaBootstrap>();
            app.Regenerate("lod-stress",WorldSize.Small,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));
            float deadline=Time.realtimeSinceStartup+30;
            while(!app.Terrain.IsReady && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.IsTrue(app.Terrain.IsReady);
            for(int i=0;i<16;i++)
            {
                app.Controller.SetView(new GeoPoint(i*150-1200,i*70-500),i*13,45,i%2==0?90:18000);
                yield return new WaitForSecondsRealtime(0.12f);
                Assert.AreEqual(3,app.Terrain.BaseMeshes,"Fallback must survive local churn");
                Assert.LessOrEqual(app.Terrain.Regional.StagingMeshes,1,"Only one regional install may be staged beside its visible fallback");
                Assert.LessOrEqual(app.Terrain.OwnedMeshes,app.Terrain.ActiveChunks*2+3+app.Terrain.DecorativeMeshes+app.Terrain.Regional.StagingMeshes,"At most one outgoing mesh per tile, plus the explicitly counted regional install");
                Assert.LessOrEqual(app.Terrain.PendingChunks,2);
            }
            app.Controller.SetView(new GeoPoint(0,0),25,45,900);
            deadline=Time.realtimeSinceStartup+30;
            while(!app.Terrain.IsReady && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.IsTrue(app.Terrain.IsReady,"LOD work must settle after rapid travel");
            Assert.AreEqual(app.Terrain.ActiveChunks+3+app.Terrain.DecorativeMeshes,app.Terrain.OwnedMeshes);
        }
        [UnityTest] public IEnumerator ClosingDuringRegionalUploadReleasesStagedNativeResources()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();if(app==null)app=new GameObject("regional cancellation test").AddComponent<AevoriaBootstrap>();
            app.ReturnToMenu();yield return null;app.Regenerate("staged-upload-008",WorldSize.Small,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));app.Controller.ExternalDriver=true;
            float deadline=Time.realtimeSinceStartup+60;while(!app.Terrain.IsReady&&Time.realtimeSinceStartup<deadline)yield return null;Assert.IsTrue(app.Terrain.IsReady);
            app.Terrain.Regional.Quality=2;app.Controller.SetView(new GeoPoint(1000,2000),25,45,2500);
            deadline=Time.realtimeSinceStartup+30;while(app.Terrain.Regional.StagingMeshes==0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.AreEqual(1,app.Terrain.Regional.StagingMeshes);
            app.ReturnToMenu();yield return null;yield return null;
            foreach(var mesh in Resources.FindObjectsOfTypeAll<Mesh>())Assert.IsFalse(mesh.name.StartsWith("Regional terrain clipmap"),"Closing mid-install must destroy active and staged regional meshes");
            Assert.AreEqual(0,Shader.GetGlobalFloat("_AevoriaRegionalReady"));
        }
        [UnityTest,Timeout(600000)] public IEnumerator FastTravelOutsideWorldRetainsSceneryAndBoundsResources()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();
            if(app==null) app=new GameObject("travel test").AddComponent<AevoriaBootstrap>();
            app.Regenerate("fast-large",WorldSize.Large,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));
            app.Controller.SetView(new GeoPoint(0,0),0,55,32000);
            float deadline=Time.realtimeSinceStartup+30;
            while(!app.Terrain.IsReady && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.IsTrue(app.Terrain.IsReady);Assert.AreEqual(3,app.Terrain.BaseMeshes);
            app.Controller.SetView(new GeoPoint(10000000,10000000),70,25,2,-50);
            yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual(0,app.Terrain.ActiveChunks);Assert.AreEqual(0,app.Terrain.Vegetation.CachedTrees);
            Assert.AreEqual(3+app.Terrain.DecorativeMeshes,app.Terrain.OwnedMeshes);Assert.GreaterOrEqual(app.Controller.Clearance,2.99f);
        }
        [UnityTest,Timeout(600000)] public IEnumerator RegenerationKeepsClockAndReleasesMeshes()
        {
            var app=Object.FindFirstObjectByType<AevoriaBootstrap>();
            if(app==null) app=new GameObject("Aevoria test").AddComponent<AevoriaBootstrap>();
            var clock=app.Clock; clock.Start();
            for(int generation=0;generation<3;generation++)
            {
                app.Regenerate("regeneration-"+generation,WorldSize.Small,new GenerationSettings(renderRadius:2,nearLodRadius:0,mediumLodRadius:1));
                float deadline=Time.realtimeSinceStartup+30;
                while(!app.Terrain.IsReady && Time.realtimeSinceStartup<deadline) yield return null;
                Assert.IsTrue(app.Terrain.IsReady,"Chunk generation did not settle");
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.AreSame(clock,app.Clock); Assert.Greater(clock.State.Tick,0);
                Assert.AreEqual(app.Terrain.ActiveChunks+app.Terrain.BaseMeshes+app.Terrain.DecorativeMeshes,app.Terrain.OwnedMeshes);
                Assert.LessOrEqual(app.Terrain.ActiveChunks,25);
                int owned=0; foreach(var filter in app.Terrain.GetComponentsInChildren<MeshFilter>(true)) if(filter.sharedMesh!=null) owned++;
                Assert.AreEqual(app.Terrain.OwnedMeshes,owned);
                int live=0; foreach(var mesh in Resources.FindObjectsOfTypeAll<Mesh>()) if(mesh.name.StartsWith("Terrain ")) live++;
                Assert.AreEqual(app.Terrain.ActiveChunks,live,"Replaced chunk meshes must be destroyed");
                int vegetationMeshes=0;foreach(var mesh in Resources.FindObjectsOfTypeAll<Mesh>()) if(mesh.name.StartsWith("Instanced ")||mesh.name=="Closed rocky outcrop") vegetationMeshes++;
                Assert.AreEqual(WorldVegetation.OwnedMeshCount,vegetationMeshes,"Vegetation meshes must be reused across generations");
                var cam=Object.FindFirstObjectByType<MapCamera>();
                // Clearance is now an opt-in mode; preserve its original regression check.
                cam.TerrainCollision=true; cam.Advance(1);
                Assert.GreaterOrEqual(cam.transform.position.y,app.Terrain.Geography.Sample(new GeoPoint(cam.transform.position.x,cam.transform.position.z)).ElevationMeters+2.9f);
                cam.TerrainCollision=false;
            }
        }
    }
}
