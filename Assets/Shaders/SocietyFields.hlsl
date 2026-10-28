#ifndef AEVORIA_SOCIETY_FIELDS
#define AEVORIA_SOCIETY_FIELDS
TEXTURE2D(_AevoriaSocietyOverlay);SAMPLER(sampler_AevoriaSocietyOverlay);
float4 _AevoriaSocietyRect;float _AevoriaSocietyEnabled;
float3 SocietyAlbedo(float3 base,float2 geographic)
{
    if(_AevoriaSocietyEnabled<.5)return base;
    float2 uv=(geographic-_AevoriaSocietyRect.xy)/max(_AevoriaSocietyRect.zw,1);
    if(any(uv<0)||any(uv>1))return base;
    float4 overlay=SAMPLE_TEXTURE2D_LOD(_AevoriaSocietyOverlay,sampler_AevoriaSocietyOverlay,uv,0);
    return lerp(base,overlay.rgb,overlay.a);
}
#endif
