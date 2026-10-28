using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aevoria.Presentation
{
    // Geographic data stays in absolute metres. Unity transforms and physics
    // use nearby coordinates; meshes containing geographic vertices inherit
    // the shifted scene root. Rebasing never changes a saved world position.
    public static class WorldOrigin
    {
        public static Vector3 Offset {get;private set;}
        public static int Revision {get;private set;}
        public static int LastShiftFrame {get;private set;}=-10;
        public static event System.Action<Vector3> Shifted;
        private sealed class Instances {public int Revision=-1;public Matrix4x4[] Matrices;}
        private static readonly ConditionalWeakTable<Matrix4x4[],Instances> instances=new ConditionalWeakTable<Matrix4x4[],Instances>();
        private static ParticleSystem.Particle[] particles=new ParticleSystem.Particle[1024];
        private static readonly Vector4[] noiseOrigins=new Vector4[10];
        public static Vector3 Geographic(Vector3 local)=>local+Offset;
        public static Vector3 Relative(Vector3 geographic)=>geographic-Offset;
        public static Vector3 Geographic(Transform value)=>Geographic(value.position);
        public static Bounds Relative(Bounds geographic)=>new Bounds(Relative(geographic.center),geographic.size);
        public static void Follow(Vector3 geographic)
        {
            var local=Relative(geographic);if(Mathf.Abs(local.x)<2048&&Mathf.Abs(local.z)<2048)return;
            Set(new Vector3(Mathf.Floor(geographic.x/1024)*1024,0,Mathf.Floor(geographic.z/1024)*1024));
        }
        public static void Reset(){Set(Vector3.zero);PublishShaderOrigin();}
        private static void NoiseOrigin(int index,double x,double z)
        {
            const float skew=.3660254038f;double sx=x+(x+z)*skew,sz=z+(x+z)*skew;
            double ix=System.Math.Floor(sx),iz=System.Math.Floor(sz);
            noiseOrigins[index]=new Vector4((float)ix,(float)iz,(float)(sx-ix),(float)(sz-iz));
        }
        private static void PublishShaderOrigin()
        {
            double x=Offset.x,z=Offset.z;
            NoiseOrigin(0,x*.011f,z*.011f);NoiseOrigin(1,z*.15f,0);NoiseOrigin(2,x*.13f,z*.13f);NoiseOrigin(3,x*.15f,0);
            NoiseOrigin(4,z*.65f,0);NoiseOrigin(5,x*.65f,z*.65f);NoiseOrigin(6,x*.65f,0);
            NoiseOrigin(7,z*3.1f,0);NoiseOrigin(8,x*3.1f,z*3.1f);NoiseOrigin(9,x*3.1f,0);
            Shader.SetGlobalVector("_AevoriaOrigin",Offset);Shader.SetGlobalVectorArray("_AevoriaNoiseOrigins",noiseOrigins);
        }
        private static void Set(Vector3 next)
        {
            Vector3 shift=next-Offset;if(shift==Vector3.zero)return;
            // Root translations move each object once, including inactive
            // collision, pooled tiles, weather bounds and the equipped tool.
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);if(!scene.isLoaded)continue;
                foreach(var root in scene.GetRootGameObjects())root.transform.position-=shift;
            }
            foreach(var system in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(system.main.simulationSpace!=ParticleSystemSimulationSpace.World)continue;
                if(particles.Length<system.main.maxParticles)System.Array.Resize(ref particles,system.main.maxParticles);
                int count=system.GetParticles(particles);for(int i=0;i<count;i++)particles[i].position-=shift;system.SetParticles(particles,count);
            }
            Offset=next;Revision++;LastShiftFrame=Time.frameCount;PublishShaderOrigin();Shifted?.Invoke(shift);Physics.SyncTransforms();
            foreach(var camera in Camera.allCameras)
                if(camera.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var data))data.resetHistory=true;
        }
        public static void DrawInstances(RenderParams parameters,Mesh mesh,int submesh,Matrix4x4[] source,int count=-1,int start=0)
        {
            var entry=instances.GetValue(source,_=>new Instances{Matrices=new Matrix4x4[source.Length]});
            // Wildlife and resource matrices can change without an origin
            // change. Reuse storage but translate the current submitted range.
            int end=count<0?source.Length:Mathf.Min(source.Length,start+count);
            for(int i=start;i<end;i++){var matrix=source[i];matrix.m03-=Offset.x;matrix.m23-=Offset.z;entry.Matrices[i]=matrix;}
            parameters.worldBounds=Relative(parameters.worldBounds);
            Graphics.RenderMeshInstanced(parameters,mesh,submesh,entry.Matrices,count,start);
        }
    }
}
