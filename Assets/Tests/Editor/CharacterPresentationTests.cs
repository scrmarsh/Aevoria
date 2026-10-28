using System.Linq;
using Aevoria.Presentation;
using Aevoria.World;
using NUnit.Framework;
using UnityEngine;

namespace Aevoria.Tests
{
    public sealed class CharacterPresentationTests
    {
        [TestCase(CharacterAppearance.Explorer),TestCase(CharacterAppearance.Roman),TestCase(CharacterAppearance.Doctor)]
        public void PresetsHaveWeightedGeometryAndNeverMoveThePhysicsRoot(CharacterAppearance preset)
        {
            var root=new GameObject("Character test root");root.transform.position=new Vector3(22,71,-18);var visual=new GameObject("Appearance");visual.transform.SetParent(root.transform,false);
            try
            {
                var rig=visual.AddComponent<HumanoidPresentation>();rig.Initialize(preset);Assert.Greater(rig.Skin.sharedMesh.vertexCount,3000);Assert.AreEqual(rig.Skin.sharedMesh.vertexCount,rig.Skin.sharedMesh.boneWeights.Length);Assert.GreaterOrEqual(rig.Skin.bones.Length,17);
                if(preset==CharacterAppearance.Doctor){Assert.IsNotNull(rig.Cane);Assert.IsTrue(rig.Cane.IsChildOf(rig.RightHand));Assert.IsFalse(rig.Cane.IsChildOf(rig.LeftHand));Assert.AreSame(rig.LeftHand,rig.ToolHand);Assert.AreEqual(rig.Skin.sharedMesh.vertexCount,rig.Skin.sharedMesh.uv.Length);var geometry=rig.Skin.sharedMesh;var points=geometry.vertices;var normals=geometry.normals;var triangles=geometry.triangles;int aligned=0,total=0;for(int n=0;n<triangles.Length;n+=3){int a=triangles[n],b=triangles[n+1],c=triangles[n+2];var face=Vector3.Cross(points[b]-points[a],points[c]-points[a]);if(face.sqrMagnitude<1e-16f)continue;total++;if(Vector3.Dot(face,normals[a]+normals[b]+normals[c])>0)aligned++;}Assert.Greater(aligned/(float)total,.97f,"Visible mesh winding and shading normals must agree");}else{Assert.IsNull(rig.Cane);Assert.AreSame(rig.RightHand,rig.ToolHand);}
                Vector3 before=root.transform.position;var rotation=root.transform.rotation;
                for(int i=0;i<120;i++)rig.Animate(1,i>60,true,false,0,false,false,1/60f);
                rig.Animate(0,false,false,false,-15,false,false,1/60f);Assert.AreEqual("Freefall",rig.AnimationState);Assert.AreEqual(before,root.transform.position);Assert.AreEqual(rotation,root.transform.rotation);
                Assert.IsTrue(rig.Skin.sharedMesh.boneWeights.All(w=>Mathf.Abs(w.weight0+w.weight1-1)<.0001f));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void SecretAndDialoguePoolFollowTheRequestedContracts()
        {
            Assert.IsTrue(CharacterProfiles.UnlocksDoctor("EVERYBODY LIES"));Assert.IsFalse(CharacterProfiles.UnlocksDoctor("everybody"));Assert.AreEqual(30,CharacterProfiles.DoctorDialogue.Length);Assert.AreEqual(30,CharacterProfiles.DoctorDialogue.Distinct().Count());Assert.AreEqual("Everybody lies.",CharacterProfiles.DoctorDialogue[0]);Assert.AreEqual("The world doesn't owe you a happy ending.",CharacterProfiles.DoctorDialogue[29]);
            var living=new LivingWorldSnapshot();Assert.AreEqual(CharacterProfiles.Explorer,living.Traveler.CharacterPreset);living.Traveler.CharacterPreset=CharacterProfiles.Doctor;Assert.AreEqual(CharacterProfiles.Doctor,LivingWorldState.Clone(living).Traveler.CharacterPreset);
        }
        [Test] public void DoctorCrouchKeepsFeetAndCaneOnTheGroundWithoutMovingPhysicsRoot()
        {
            var root=new GameObject("Doctor stance contact");var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(8,1,8);
            try
            {
                var rig=root.AddComponent<HumanoidPresentation>();rig.Initialize(CharacterAppearance.Doctor);rig.GroundContactEnabled=true;Physics.SyncTransforms();
                for(int i=0;i<90;i++)rig.Animate(0,false,true,false,0,false,true,1/60f);
                var baked=new Mesh();try{rig.Skin.BakeMesh(baked);Assert.Less(baked.bounds.max.y,1.34f,"Crouched anatomy must fit its physical stance");Assert.Greater(baked.bounds.min.y,-.035f,"Shoes must not collapse through the ground");}finally{Object.DestroyImmediate(baked);}
                Assert.Less(rig.CaneContactError,.045f,"Cane ferrule must meet the actual support plane");Assert.AreEqual(Vector3.zero,root.transform.position);
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(floor);}
        }
        [TestCase(CharacterAppearance.Doctor),TestCase(CharacterAppearance.Roman),TestCase(CharacterAppearance.Explorer)]
        public void EquippedToolsCarryUprightAndCutInTheForwardSwingPlane(CharacterAppearance preset)
        {
            var root=new GameObject("Actual rig grip orientation");
            try
            {
                var rig=root.AddComponent<HumanoidPresentation>();rig.Initialize(preset);rig.ToolEquipped=true;
                for(int i=0;i<90;i++)rig.Animate(0,false,true,false,0,false,false,1/60f);
                bool left=preset==CharacterAppearance.Doctor;
                foreach(var tool in new[]{ToolKind.Pickaxe,ToolKind.Axe,ToolKind.Shovel,ToolKind.Stick})
                {
                    var grip=EquipmentGrip012.Rotation(tool,left,false,false,0);var shaft=rig.ToolHand.TransformDirection(grip*Vector3.up);
                    Assert.Greater(Vector3.Dot(shaft,Vector3.up),.85f,"Carry shaft must point up: "+tool);
                    if(tool==ToolKind.Pickaxe||tool==ToolKind.Axe){var edge=rig.ToolHand.TransformDirection(grip*Vector3.right);Assert.Less(Mathf.Abs(edge.x),.20f,"Cutting head belongs in forward swing plane: "+tool);}
                    var viewShaft=EquipmentGrip012.ViewModelRotation(tool,false,0)*Vector3.up;Assert.Less(Mathf.Abs(viewShaft.x),.20f,"Viewmodel shaft must not lie sideways");Assert.Greater(viewShaft.z,.75f);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void DoctorNeckWidensBelowTheCollarAndIsAnchoredToTheTorso()
        {
            var root=new GameObject("Neck torso continuity");
            try
            {
                var rig=root.AddComponent<HumanoidPresentation>();rig.Initialize(CharacterAppearance.Doctor);var skin=rig.Skin.sharedMesh;var p=skin.vertices;var c=skin.colors;var w=skin.boneWeights;int chest=System.Array.FindIndex(rig.Skin.bones,b=>b.name=="Spine and rib cage");int anchors=0;float width=0;
                for(int i=0;i<p.Length;i++)if(p[i].y>1.48f&&p[i].y<1.535f&&c[i].r>.4f&&c[i].b<c[i].r*.8f)
                {
                    float support=(w[i].boneIndex0==chest?w[i].weight0:0)+(w[i].boneIndex1==chest?w[i].weight1:0);Assert.Greater(support,.75f,"Lower neck must follow the torso through crouch and turning");anchors++;width=Mathf.Max(width,Mathf.Abs(p[i].x));
                }
                Assert.Greater(anchors,10,"Skin must extend below the shirt collar into the torso");Assert.Greater(width,.085f,"The attachment must flare into the upper chest");
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void DoctorDistanceSkinsFollowTheSameCrouchedSkeleton()
        {
            var root=new GameObject("LOD pose consistency");
            try
            {
                var rig=root.AddComponent<HumanoidPresentation>();rig.Initialize(CharacterAppearance.Doctor);var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();Assert.AreEqual(3,skins.Length);
                for(int i=0;i<90;i++)rig.Animate(0,false,true,false,0,false,true,1/60f);
                Bounds near=default;int priorTriangles=int.MaxValue;
                foreach(var skin in skins)
                {
                    var baked=new Mesh();try{skin.BakeMesh(baked);if(skin==rig.Skin)near=baked.bounds;else{Assert.Less(Vector3.Distance(near.center,baked.bounds.center),.12f,"LOD skin weights must map by bone name");Assert.Less(Mathf.Abs(near.max.y-baked.bounds.max.y),.12f);Assert.Less(Mathf.Abs(near.min.y-baked.bounds.min.y),.08f);}Assert.Less(skin.sharedMesh.triangles.Length,priorTriangles);priorTriangles=skin.sharedMesh.triangles.Length;}finally{Object.DestroyImmediate(baked);}
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
