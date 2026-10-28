using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace Aevoria.Tests
{
    public sealed class RomanSoldierTests
    {
        [Test]public void LegionaryHasWeightedSkinAndIndependentPhysicsRoot()
        {
            var root=new GameObject("Legionary rig validation");root.transform.position=new Vector3(12,40,19);var visual=new GameObject("Visual skeleton");visual.transform.SetParent(root.transform,false);var soldier=visual.AddComponent<RomanSoldier>();
            try
            {
                soldier.Initialize();var skin=soldier.Skin;Assert.GreaterOrEqual(skin.bones.Length,17);Assert.AreEqual(skin.bones.Length,skin.sharedMesh.bindposes.Length);Assert.Greater(skin.sharedMesh.vertexCount,3000);Assert.AreEqual(skin.sharedMesh.vertexCount,skin.sharedMesh.boneWeights.Length);
                foreach(var weight in skin.sharedMesh.boneWeights){Assert.AreEqual(1,weight.weight0+weight.weight1+weight.weight2+weight.weight3,.00001);Assert.Less(weight.boneIndex0,skin.bones.Length);Assert.Less(weight.boneIndex1,skin.bones.Length);}
                var positions=skin.sharedMesh.vertices;var normals=skin.sharedMesh.normals;int limbSamples=0;for(int i=0;i<positions.Length;i++){var p=positions[i];if(p.y>.56f&&p.y<.75f&&Mathf.Abs(p.x)>.17f&&Mathf.Abs(p.z)<.015f){Assert.Greater(normals[i].x*(p.x-Mathf.Sign(p.x)*.105f),0,"Leg surfaces face outward, rather than exposing the back of the skin");limbSamples++;}}Assert.Greater(limbSamples,0);
                var position=root.transform.position;var rotation=root.transform.rotation;
                foreach(var state in new[]{"Idle","Walking","Running","Jumping","Flying","Freefall","Digging"})
                {
                    bool moving=state=="Walking"||state=="Running",ground=state=="Idle"||moving;for(int i=0;i<30;i++)soldier.Animate(moving?1:0,state=="Running",ground,state=="Flying",state=="Jumping"?5:-5,state=="Digging",false,1/60f);Assert.AreEqual(state,soldier.AnimationState);Assert.AreEqual(position,root.transform.position);Assert.AreEqual(rotation,root.transform.rotation);
                }
                soldier.Animate(0,false,true,false,0,false,false,1/60f);Assert.AreEqual("Landing",soldier.AnimationState);Assert.AreEqual(position,root.transform.position);Assert.IsTrue(soldier.RightHand.IsChildOf(visual.transform));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
