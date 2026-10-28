#ifndef AEVORIA_SETTLEMENT_FIELDS
#define AEVORIA_SETTLEMENT_FIELDS
TEXTURE2D(_AevoriaSettledFootprints);SAMPLER(sampler_AevoriaSettledFootprints);
float4 _AevoriaSettledRect;float _AevoriaSettledReady;
void ClipSettledVegetation(float2 geographic,float foliage)
{
    if(_AevoriaSettledReady<.5||foliage<.5)return;
    float2 uv=(geographic-_AevoriaSettledRect.xy)/max(_AevoriaSettledRect.zw,1);
    if(all(uv>=0)&&all(uv<=1))clip(.5-SAMPLE_TEXTURE2D_LOD(_AevoriaSettledFootprints,sampler_AevoriaSettledFootprints,uv,0).r);
}
#endif
