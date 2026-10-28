using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    public partial class HumanoidPresentation
    {
        [Serializable]private sealed class AuthoredBone{public string name;public int parent;public Vector3 p;}
        [Serializable]private sealed class AuthoredVertex{public Vector3 p,n;public Vector2 uv,faceUV;public Color c;public int a,b;public float w,skin;}
        [Serializable]private sealed class AuthoredCharacter{public int version;public string name;public AuthoredBone[] bones;public AuthoredVertex[] vertices;public int[] triangles;}
        private readonly List<int> leftFingers=new List<int>(),rightFingers=new List<int>();
        private Material caneMaterial;private bool firstPerson;private MaterialPropertyBlock characterProperties;
        private readonly List<SkinnedMeshRenderer> distanceSkins=new List<SkinnedMeshRenderer>();private readonly List<Mesh> distanceMeshes=new List<Mesh>();
        public Transform ToolHand=>appearance==Aevoria.World.CharacterAppearance.Doctor?LeftHand:RightHand;
        public bool ToolEquipped{get;set;}
        public void SetFirstPerson(bool value){firstPerson=value;if(Skin==null||!Skin.sharedMaterial.HasProperty("_HideHead"))return;if(characterProperties==null)characterProperties=new MaterialPropertyBlock();Skin.GetPropertyBlock(characterProperties);characterProperties.SetFloat("_HideHead",value?1:0);Skin.SetPropertyBlock(characterProperties);foreach(var skin in distanceSkins)skin.SetPropertyBlock(characterProperties);}
        private void LoadAuthoredDoctor()
        {
            var asset=Resources.Load<TextAsset>("Characters/Doctor012");if(asset==null)throw new InvalidOperationException("Authored Doctor012 mesh is required; rebuild editable Blender source exports.");
            var data=JsonUtility.FromJson<AuthoredCharacter>(asset.text);if(data.version!=12||data.bones.Length>pose.Length||data.vertices.Length<10000)throw new InvalidOperationException("Invalid Doctor012 authored mesh contract.");
            for(int i=0;i<data.bones.Length;i++){var b=data.bones[i];Bone(b.name,b.parent,b.p);if(b.name.StartsWith("Left finger "))leftFingers.Add(i);if(b.name.StartsWith("Right finger "))rightFingers.Add(i);}
            int Find(string name){for(int i=0;i<data.bones.Length;i++)if(data.bones[i].name==name)return i;throw new InvalidOperationException("Missing anatomical bone: "+name);}
            hips=Find("Pelvis");chest=Find("Spine and rib cage");neck=Find("Neck");head=Find("Head");leftThigh=Find("Left thigh");leftShin=Find("Left knee");leftFoot=Find("Left ankle");rightThigh=Find("Right thigh");rightShin=Find("Right knee");rightFoot=Find("Right ankle");leftUpper=Find("Left shoulder");leftLower=Find("Left elbow");leftHand=Find("Left wrist");rightUpper=Find("Right shoulder");rightLower=Find("Right elbow");rightHand=Find("Right wrist");cape=Find("Short cape");
            var points=new Vector3[data.vertices.Length];var normals=new Vector3[points.Length];var uv=new Vector2[points.Length];var projections=new Vector2[points.Length];var regions=new Vector2[points.Length];var colors=new Color[points.Length];var skin=new BoneWeight[points.Length];
            for(int i=0;i<points.Length;i++){var v=data.vertices[i];points[i]=v.p;normals[i]=v.n;uv[i]=v.uv;projections[i]=v.faceUV;regions[i]=new Vector2(v.a==head||v.a==neck?1:0,v.skin);colors[i]=v.c;skin[i]=new BoneWeight{boneIndex0=v.a,weight0=1-v.w,boneIndex1=v.b,weight1=v.w};}
            mesh=new Mesh{name="Doctor012 authored anatomical skin",indexFormat=points.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.vertices=points;mesh.normals=normals;mesh.uv=uv;mesh.uv2=regions;mesh.uv3=projections;mesh.colors=colors;mesh.triangles=data.triangles;mesh.boneWeights=skin;
            var matrices=new Matrix4x4[bones.Count];for(int i=0;i<matrices.Length;i++)matrices[i]=bones[i].worldToLocalMatrix*transform.localToWorldMatrix;mesh.bindposes=matrices;mesh.RecalculateBounds();
            material=new Material(Resources.Load<Material>("CharacterSurface")??Resources.Load<Material>("RomanSoldier"));if(material.HasProperty("_HeadMap"))material.SetTexture("_HeadMap",Resources.Load<Texture2D>("Characters/DoctorReference"));Skin=gameObject.AddComponent<SkinnedMeshRenderer>();Skin.sharedMesh=mesh;Skin.sharedMaterial=material;Skin.bones=bones.ToArray();Skin.rootBone=bones[hips];Skin.localBounds=new Bounds(new Vector3(0,1,0),new Vector3(2.4f,2.8f,2.4f));
            LoadDistanceSkins();
        }
        private void LoadDistanceSkins()
        {
            for(int level=1;level<=2;level++)
            {
                var text=Resources.Load<TextAsset>("Characters/Doctor012_LOD"+level);if(text==null)continue;var data=JsonUtility.FromJson<AuthoredCharacter>(text.text);
                if(data.bones.Length!=bones.Count)throw new InvalidOperationException("Doctor LOD skeleton mismatch");
                // Blender serializes bones in hierarchy order. Remap names to
                // the runtime bind order rather than treating indices as stable.
                var boneMap=new int[data.bones.Length];for(int i=0;i<boneMap.Length;i++){boneMap[i]=bones.FindIndex(b=>b.name==data.bones[i].name);if(boneMap[i]<0)throw new InvalidOperationException("Doctor LOD bone missing: "+data.bones[i].name);}
                var points=new Vector3[data.vertices.Length];var normals=new Vector3[points.Length];var uv=new Vector2[points.Length];var projections=new Vector2[points.Length];var regions=new Vector2[points.Length];var colors=new Color[points.Length];var weights=new BoneWeight[points.Length];
                for(int i=0;i<points.Length;i++){var v=data.vertices[i];int a=boneMap[v.a],b=boneMap[v.b];points[i]=v.p;normals[i]=v.n;uv[i]=v.uv;projections[i]=v.faceUV;colors[i]=v.c;regions[i]=new Vector2(a==head||a==neck?1:0,v.skin);weights[i]=new BoneWeight{boneIndex0=a,weight0=1-v.w,boneIndex1=b,weight1=v.w};}
                var lod=new Mesh{name="Doctor012 distance LOD "+level,indexFormat=points.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};lod.vertices=points;lod.normals=normals;lod.uv=uv;lod.uv2=regions;lod.uv3=projections;lod.colors=colors;lod.boneWeights=weights;lod.bindposes=mesh.bindposes;lod.triangles=data.triangles;lod.RecalculateBounds();distanceMeshes.Add(lod);
                var child=new GameObject("Doctor distance LOD "+level);child.layer=gameObject.layer;child.transform.SetParent(transform,false);var skin=child.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=lod;skin.sharedMaterial=material;skin.bones=Skin.bones;skin.rootBone=Skin.rootBone;skin.localBounds=Skin.localBounds;distanceSkins.Add(skin);
            }
            if(distanceSkins.Count==2){var group=gameObject.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.12f,new Renderer[]{Skin}),new LOD(.045f,new Renderer[]{distanceSkins[0]}),new LOD(.003f,new Renderer[]{distanceSkins[1]})});group.RecalculateBounds();}
        }
        private void ReleaseDistanceMeshes(){foreach(var lod in distanceMeshes){if(Application.isPlaying)Destroy(lod);else DestroyImmediate(lod);}distanceMeshes.Clear();}
    }
}
