using System;
using System.Collections.Generic;
using System.Reflection;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace Aevoria.Tests
{
    public sealed class BoulderPresentationTests
    {
        [Test]public void BouldersAndOutcropsAreClosedOutwardMeshesWithSolidRaycastCollision()
        {
            foreach(var factory in new[]{typeof(WorldNaturalDetails).GetMethod("BoulderMesh",BindingFlags.NonPublic|BindingFlags.Static),typeof(WorldVegetation).GetMethod("Rock",BindingFlags.NonPublic|BindingFlags.Static)})
            {
                var mesh=(Mesh)factory.Invoke(null,null);var obj=new GameObject("Closed boulder collision contract");
                try
                {
                    var vertices=mesh.vertices;var indices=mesh.triangles;var edges=new Dictionary<(int,int),int>();double volume=0;
                    void Edge(int a,int b){var key=a<b?(a,b):(b,a);edges.TryGetValue(key,out int count);edges[key]=count+1;}
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        int a=indices[i],b=indices[i+1],c=indices[i+2];Assert.Greater(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).sqrMagnitude,.000001f,"No collapsed pole or seam triangles");
                        Edge(a,b);Edge(b,c);Edge(c,a);volume+=Vector3.Dot(vertices[a],Vector3.Cross(vertices[b],vertices[c]))/6.0;
                    }
                    foreach(int count in edges.Values)Assert.AreEqual(2,count,"Every mesh edge belongs to exactly two faces");Assert.Greater(volume,0,"Triangle winding encloses positive solid volume");
                    var collider=obj.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();float reach=mesh.bounds.extents.magnitude*3;
                    foreach(var direction in new[]{Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                    {Assert.IsTrue(collider.Raycast(new Ray(mesh.bounds.center+direction*reach,-direction),out var hit,reach*2),"Solid collision from "+direction);Assert.Greater(Vector3.Dot(hit.normal,direction),0,"Collider normal faces outside the rock");}
                }
                finally{UnityEngine.Object.DestroyImmediate(obj);UnityEngine.Object.DestroyImmediate(mesh);}
            }
        }
    }
}
