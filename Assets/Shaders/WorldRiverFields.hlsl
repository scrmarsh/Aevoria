#ifndef AEVORIA_RIVER_FIELDS
#define AEVORIA_RIVER_FIELDS
struct RiverCurve {float4 a,b,c,d,e;};
StructuredBuffer<RiverCurve> _AevoriaRiverCurves;
StructuredBuffer<int2> _AevoriaRiverRanges;
StructuredBuffer<int> _AevoriaRiverIndices;
float _AevoriaRiverBanksReady;
float2 RiverTangent(float2 a,float2 b){return float2(a.x*b.x<=0?0:2*a.x*b.x/(a.x+b.x),a.y*b.y<=0?0:2*a.y*b.y/(a.y+b.y));}
// Result is signed bank distance, surface level, valid. The same cubic
// coefficients and bounded closest-point solve are used by HydrologyMap.
float3 WorldRiverBank(float2 world,float4 rectangle,out float3 channel)
{
    channel=0;
    float2 uv=(world-rectangle.xy)/rectangle.zw;if(_AevoriaRiverBanksReady<.5||any(uv<0)||any(uv>=1))return float3(100,0,0);
    int2 cell=(int2)floor(uv*1024),range=_AevoriaRiverRanges[cell.y*1024+cell.x];float nearest=1e30,bank=100,level=0;
    [loop]for(int k=0;k<range.y;k++)
    {
        RiverCurve r=_AevoriaRiverCurves[_AevoriaRiverIndices[range.x+k]];float2 a=r.a.xy,b=r.b.xy,delta=b-a;
        float2 box=max(max(min(a,b)-world,world-max(a,b)),0);if(dot(box,box)>min(nearest,(r.b.z+32)*(r.b.z+32)))continue;
        float2 m0=RiverTangent(a-r.c.xy,delta),m1=RiverTangent(delta,r.c.zw-b),cubic=2*a-2*b+m0+m1,quadratic=-3*a+3*b-2*m0-m1;
        float t=saturate(dot(world-a,delta)/max(.0001,dot(delta,delta)));
        [unroll]for(int iteration=0;iteration<8;iteration++)
        {
            float2 q=((cubic*t+quadratic)*t+m0)*t+a-world,v=(3*cubic*t+2*quadratic)*t+m0;
            float denominator=dot(v,v)+dot(q,6*cubic*t+2*quadratic);if(denominator<=1e-12)break;
            t=saturate(t-clamp(dot(q,v)/denominator,-.5,.5));
        }
        float2 q=((cubic*t+quadratic)*t+m0)*t+a-world;float d=dot(q,q),d0=dot(a-world,a-world),d1=dot(b-world,b-world);
        if(d0<d){d=d0;t=0;}if(d1<d){d=d1;t=1;}
        if(d<nearest)
        {
            float pathT=lerp(r.e.z,r.e.w,t);float width=lerp(r.e.x,r.e.y,pathT*pathT*(3-2*pathT));
            float halfWidth=width*.5,bedDepth=clamp(.6+sqrt(max(0,width))*.24+min(1,r.b.w)*.4,.5,18);
            nearest=d;bank=sqrt(d)-halfWidth;channel=float3(halfWidth,bedDepth,r.d.w>.5?0:r.d.z);
            float2 tangent=RiverTangent(float2(r.a.z-r.d.x,r.a.w-r.a.z),float2(r.a.w-r.a.z,r.d.y-r.a.w));float t2=t*t,t3=t2*t;level=(2*t3-3*t2+1)*r.a.z+(t3-2*t2+t)*tangent.x+(-2*t3+3*t2)*r.a.w+(t3-t2)*tangent.y;
        }
    }
    return float3(bank,level,nearest<1e29?1:0);
}
#endif
