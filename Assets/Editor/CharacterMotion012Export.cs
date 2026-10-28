using System;
using System.IO;
using Aevoria.World;
using Aevoria.Presentation;
using UnityEditor;
using UnityEngine;
namespace Aevoria.Editor
{
    // Seed editable Unity clips from the revised rig. Existing artist edits are
    // preserved on every ordinary build; explicitly rebake from the menu only.
    public static class CharacterMotion012Export
    {
        private const string Folder="Assets/Resources/Characters/Motion012";
        public static void EnsureAssets(){if(!File.Exists(Folder+"/Walk.anim"))Bake();}
        [MenuItem("Aevoria/Characters/Rebake Doctor 012 animation clips")]
        public static void Bake()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            foreach(string state in new[]{"Idle","Walk","Run","Crouch","CrouchWalk","Mining","Jump","Freefall","Flight"})
            {
                var root=new GameObject("Doctor clip authoring");
                try
                {
                    var rig=root.AddComponent<HumanoidPresentation>();rig.Initialize(CharacterAppearance.Doctor);rig.SampleEditableClips=false;rig.ToolEquipped=true;
                    bool crouch=state.StartsWith("Crouch"),run=state=="Run",moving=state=="Walk"||run||state=="CrouchWalk",mining=state=="Mining",air=state=="Jump"||state=="Freefall"||state=="Flight";
                    float duration=mining?.4f:run?.5f:moving?(crouch?.65f/1.65f:1.18f/1.65f):2;
                    void Pose(float dt){rig.Animate(moving?1:0,run,!air,state=="Flight",state=="Jump"?4:-4,mining,crouch,dt);}
                    for(int frame=0;frame<120;frame++)Pose(1/60f);rig.ResetMotionPhase();
                    int samples=Mathf.CeilToInt(duration*60);var transforms=rig.Skin.bones;var curves=new AnimationCurve[transforms.Length,4];
                    for(int bone=0;bone<transforms.Length;bone++)for(int axis=0;axis<4;axis++)curves[bone,axis]=new AnimationCurve();
                    for(int sample=0;sample<=samples;sample++)
                    {
                        float time=sample*duration/samples;rig.ToolActionProgress=time/duration;Pose(sample==0?0:duration/samples);
                        for(int bone=0;bone<transforms.Length;bone++){var q=transforms[bone].localRotation;for(int axis=0;axis<4;axis++)curves[bone,axis].AddKey(time,q[axis]);}
                    }
                    var clip=new AnimationClip{name=state,legacy=true,frameRate=60,wrapMode=mining||air?WrapMode.ClampForever:WrapMode.Loop};
                    for(int bone=0;bone<transforms.Length;bone++)for(int axis=0;axis<4;axis++)clip.SetCurve(AnimationUtility.CalculateTransformPath(transforms[bone],root.transform),typeof(Transform),"localRotation."+"xyzw"[axis],curves[bone,axis]);
                    clip.EnsureQuaternionContinuity();string path=Folder+"/"+state+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if(existing==null)AssetDatabase.CreateAsset(clip,path);else{EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);EditorUtility.SetDirty(existing);}
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
    }
}
