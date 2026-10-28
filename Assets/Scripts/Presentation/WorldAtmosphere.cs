using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aevoria.Presentation
{
    public sealed class WorldAtmosphere : MonoBehaviour
    {
        private VolumeProfile profile;
        private MotionBlur blur;
        private Camera view;
        private Material sky;
        private Material originalSky;
        public bool MotionBlurEnabled { get; set; } public float WeatherDarkness {get;set;} public float WeatherFog {get;set;}=1;
        public bool FogEnabled {get;set;}=true;public float FogDensity {get;set;}=1;public float FogDistance {get;set;}=20000;public float HazeStrength {get;set;}=1;private float smoothDensity,smoothDarkness;
        public float WorldExtent {get;set;}=65536;
        public bool AtlasPresentation {get;set;}
        public void Initialize(Camera camera)
        {
            view=camera;
            var data=view.GetUniversalAdditionalCameraData(); data.requiresDepthTexture=true; data.renderPostProcessing=true;
            data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
            var volume=gameObject.AddComponent<Volume>(); volume.isGlobal=true; volume.priority=20;
            var template=Resources.Load<VolumeProfile>("WorldEffects");
            profile=ScriptableObject.CreateInstance<VolumeProfile>();
            if(template!=null) foreach(var component in template.components) profile.components.Add(Instantiate(component));
            volume.sharedProfile=profile;
            if(!profile.TryGet(out blur)) blur=profile.Add<MotionBlur>(true); blur.mode.Override(MotionBlurMode.CameraOnly);
            blur.quality.Override(MotionBlurQuality.Low); blur.intensity.Override(0); blur.clamp.Override(0.015f);
            if(!profile.TryGet<Tonemapping>(out var tone)) tone=profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<ColorAdjustments>(out var color)) color=profile.Add<ColorAdjustments>(true); color.postExposure.Override(0.2f); color.saturation.Override(4); color.contrast.Override(7);
            if(RenderSettings.skybox!=null)
            {
                originalSky=RenderSettings.skybox; sky=new Material(originalSky); RenderSettings.skybox=sky;
                if(sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint",new Color(0.5f,0.5f,0.5f));
                if(sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor",new Color(.48f,.62f,.75f));
                if(sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure",1.15f);
            }
        }
        private void LateUpdate()
        {
            if(view==null) return;
            float altitude=Mathf.Max(0,view.transform.position.y);
            float target=FogEnabled?FogDensity/Mathf.Max(1000,FogDistance)*HazeStrength*.65f*WeatherFog:0;
            float amount=1-Mathf.Exp(-4*Time.unscaledDeltaTime);smoothDensity=Mathf.Lerp(smoothDensity,target,amount);smoothDarkness=Mathf.Lerp(smoothDarkness,WeatherDarkness,amount);
            float extinction=smoothDensity/Mathf.Pow(1+altitude/8000,1.7f);
            RenderSettings.fog=FogEnabled;RenderSettings.fogDensity=FogEnabled?extinction:0;
            if(sky!=null&&sky.HasProperty("_Exposure"))sky.SetFloat("_Exposure",1.15f-smoothDarkness*.35f);
            float overhead=AtlasPresentation?Mathf.SmoothStep(0,1,Mathf.InverseLerp(8000,40000,altitude))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,.95f,Vector3.Dot(view.transform.forward,Vector3.down))):0;
            Shader.SetGlobalVector("_AevoriaHaze",new Vector4(.48f,.62f,.75f,FogEnabled?smoothDensity*Mathf.Lerp(1,.07f,overhead):0));
            // A rebase changes the coordinate basis, not camera velocity. URP
            // camera-only blur needs two rendered histories in the new basis.
            blur.intensity.value=MotionBlurEnabled&&Time.frameCount>WorldOrigin.LastShiftFrame+1?0.13f:0;
        }
        private void OnDestroy() { if(profile!=null) { foreach(var component in profile.components) if(component!=null) Destroy(component); Destroy(profile); } if(sky!=null) {if(RenderSettings.skybox==sky) RenderSettings.skybox=originalSky; Destroy(sky);} }
    }
}

