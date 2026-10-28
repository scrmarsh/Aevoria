#ifndef AEVORIA_WEATHER_FIELDS
#define AEVORIA_WEATHER_FIELDS
#include "WorldClimateFields.hlsl"
#include "WorldHazeFields.hlsl"
TEXTURE2D(_AevoriaWeather);SAMPLER(sampler_AevoriaWeather);float4 _AevoriaWeatherRect,_AevoriaCloudDrift;float _AevoriaWeatherReady,_AevoriaClimateSea;
TEXTURE2D(_AevoriaWeatherGround);SAMPLER(sampler_AevoriaWeatherGround);
float4 _AevoriaCloudWeather;float _AevoriaCloudQuality,_AevoriaRenderClouds;
float4 _AevoriaCloudSystems[16],_AevoriaCloudShapes[16];
TEXTURE3D(_AevoriaCloudNoise);SAMPLER(sampler_AevoriaCloudNoise);
float4 RegionalWeatherField(float3 p){return SAMPLE_TEXTURE2D(_AevoriaWeather,sampler_AevoriaWeather,saturate((p.xz-_AevoriaWeatherRect.xy)/max(_AevoriaWeatherRect.zw,1)))*_AevoriaWeatherReady;}
float RegionalSurfaceTemperature(float3 p){float reference=SAMPLE_TEXTURE2D(_AevoriaWeatherGround,sampler_AevoriaWeatherGround,saturate((p.xz-_AevoriaWeatherRect.xy)/max(_AevoriaWeatherRect.zw,1))).r;return WorldAltitudeTemperature(RegionalWeatherField(p).a,reference,p.y,_AevoriaClimateSea);}
float CumulusDensity(float3 p,int system)
{
    float4 c=_AevoriaCloudSystems[system],s=_AevoriaCloudShapes[system];if(s.y<=0)return 0;
    float3 q=(p-c.xyz)/float3(c.w,s.x,c.w);if(any(abs(q)>1.65))return 0;
    float d=length(q/float3(.72,.62,.58));
    [unroll]for(int j=0;j<6;j++)
    {
        float a=j*2.39996+s.w,variation=.5+.5*sin(j*17.13+s.w*3.7);
        float3 offset=float3(cos(a)*(.25+variation*.48),-.02+sin(j*1.7+s.w)*.24,sin(a)*(.22+variation*.46));
        float3 lobe=float3(.38+variation*.23,.43+variation*.25,.35+variation*.22);
        d=min(d,length((q-offset)/lobe));
    }
    // Coherent 3D erosion belongs to the advected volume. It breaks repeated
    // cookie-shaped silhouettes without introducing a camera-facing noise sheet.
    float3 phase=float3(s.w*.037,s.w*.071,s.w*.113);
    float broad=SAMPLE_TEXTURE3D_LOD(_AevoriaCloudNoise,sampler_AevoriaCloudNoise,q*.19+phase,0).r;
    float fine=SAMPLE_TEXTURE3D_LOD(_AevoriaCloudNoise,sampler_AevoriaCloudNoise,q*.57+phase,0).r;
    d+=(broad-.5)*.62+(fine-.5)*.24;
    return (1-smoothstep(.54,1.04,d))*s.y;
}
float RegionalCloudShadow(float3 p,float3 sun)
{
    if(_AevoriaRenderClouds<.5||_AevoriaCloudQuality<.1||sun.y<.05)return 1;float density=0;
    [loop]for(int i=0;i<16;i++){float4 c=_AevoriaCloudSystems[i];if(_AevoriaCloudShapes[i].y<=0||p.y>c.y+_AevoriaCloudShapes[i].x)continue;float3 above=p+sun*((c.y-p.y)/max(.1,sun.y));density=max(density,CumulusDensity(above,i)*(.2+_AevoriaCloudShapes[i].z*.25));}
    return 1-density;
}
#endif
