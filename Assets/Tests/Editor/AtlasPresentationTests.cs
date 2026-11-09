using System;
using System.Reflection;
using Aevoria.World;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Aevoria.Tests
{
    public sealed class AtlasPresentationTests
    {
        [Test] public void GroundSkyReviewRejectsAnOverheadRoof()
        {
            var cameraObject=new GameObject("Sky review camera");var roof=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var camera=cameraObject.AddComponent<Camera>();cameraObject.transform.SetPositionAndRotation(new Vector3(50000,1000,50000),Quaternion.Euler(-18,60,0));camera.aspect=16f/9;
                roof.transform.position=cameraObject.transform.position+Vector3.up*3;roof.transform.localScale=new Vector3(80,1,80);Physics.SyncTransforms();
                var check=typeof(Alpha009TerrainAudit).GetMethod("SkyViewClear",BindingFlags.NonPublic|BindingFlags.Static);var args=new object[]{camera,0};
                Assert.IsFalse((bool)check.Invoke(null,args),"A terrain-supported pose under a roof must not pass sky review");Assert.Greater((int)args[1],0);
                roof.transform.position+=Vector3.right*500;Physics.SyncTransforms();args[1]=0;
                Assert.IsTrue((bool)check.Invoke(null,args),"An open outdoor upper view must pass");Assert.AreEqual(0,(int)args[1]);
            }
            finally{UnityEngine.Object.DestroyImmediate(roof);UnityEngine.Object.DestroyImmediate(cameraObject);}
        }
        [Test] public void AllPanelShapesContainTheEntireWorld()
        {
            foreach(WorldSize size in new[]{WorldSize.Miniature,WorldSize.Small,WorldSize.Medium,WorldSize.Large,WorldSize.Colossal})
            foreach(float aspect in new[]{1f,.65f,520f/870})foreach(bool circle in new[]{false,true})
            {
                var c=GenerationSettings.Preset("atlas-fit",size);double span=WorldNavigation.WorldSpan(c,aspect,circle);
                Assert.GreaterOrEqual(span,c.WidthMeters);Assert.GreaterOrEqual(span*aspect,c.DepthMeters);
                if(circle&&aspect==1)Assert.LessOrEqual(Math.Sqrt(Math.Pow(c.WidthMeters/span/2,2)+Math.Pow(c.DepthMeters/span/2,2)),.5);
            }
        }
        [Test] public void PlayerAndVillageMarkersHaveTheRequestedSilhouettes()
        {
            var build=typeof(WorldNavigation).GetMethod("MarkerTexture",BindingFlags.NonPublic|BindingFlags.Static);
            foreach(bool house in new[]{false,true})
            {
                var icon=(Texture2D)build.Invoke(null,new object[]{house});
                try
                {
                    // The texture is intentionally unreadable after upload;
                    // verify the GPU representation using a readback.
                    var target=RenderTexture.GetTemporary(24,24,0,RenderTextureFormat.ARGB32);
                    var old=RenderTexture.active;var copy=new Texture2D(24,24,TextureFormat.RGBA32,false);
                    try
                    {
                        Graphics.Blit(icon,target);RenderTexture.active=target;copy.ReadPixels(new Rect(0,0,24,24),0,0);copy.Apply();
                        var center=copy.GetPixel(12,10);Assert.Greater(center.a,.9);Assert.Less(copy.GetPixel(0,0).a,.1);
                        if(house){Assert.Greater(center.r,center.g);Assert.Greater(center.g,center.b);Assert.Less(copy.GetPixel(3,20).a,.1);}
                        else{Assert.Greater(center.r,.95);Assert.Greater(center.g,.95);Assert.Greater(center.b,.95);Assert.Less(copy.GetPixel(2,2).a,.1);}
                    }
                    finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(copy);}
                }
                finally{UnityEngine.Object.DestroyImmediate(icon);}
            }
        }
        [Test] public void NewHabitatMeshesAreFiniteAndBounded()
        {
            foreach(string method in new[]{"Tree","GroundCover"})
            {
                var build=typeof(WorldVegetation).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static);
                int count=method=="Tree"?WorldVegetation.TreeVariants:WorldVegetation.CoverVariants;
                for(int kind=0;kind<count;kind++)
                {
                    var mesh=(Mesh)build.Invoke(null,method=="Tree"?new object[]{kind,true}:new object[]{kind});
                    try
                    {
                        Assert.Greater(mesh.vertexCount,0);Assert.Greater(mesh.bounds.size.y,.02);
                        foreach(var p in mesh.vertices)Assert.IsTrue(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z));
                        foreach(int i in mesh.triangles)Assert.That(i,Is.InRange(0,mesh.vertexCount-1));
                    }
                    finally{UnityEngine.Object.DestroyImmediate(mesh);}
                }
            }
        }
    }
}
