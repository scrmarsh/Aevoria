#ifndef AEVORIA_TERRAIN_FIELDS
#define AEVORIA_TERRAIN_FIELDS
TEXTURE2D(_AevoriaOverviewHeight);TEXTURE2D(_AevoriaOverviewNormals);
float4 _AevoriaCoverageRect;
float4 _AevoriaOverviewGrid;float _AevoriaOverviewReady;
TEXTURE2D(_AevoriaRegionalHeight);TEXTURE2D(_AevoriaRegionalNormals);
SAMPLER(sampler_AevoriaRegionalNormals);
float4 _AevoriaRegionalRect,_AevoriaRegionalGrid;float _AevoriaRegionalReady;
float _AevoriaDigCount;float4 _AevoriaDigRects[16];
TEXTURE3D(_AevoriaSolidCoverage);float4 _AevoriaSolidGrid;float _AevoriaSolidReady,_AevoriaSolidCells;
float _AevoriaBoundarySeaLevel;
float3 WorldShadingNormal(float3 p,float3 meshNormal,float excavationDepth)
{
    if(_AevoriaRegionalReady<.5||excavationDepth>.3)return meshNormal;
    float2 g=(p.xz-_AevoriaRegionalRect.xy)/_AevoriaRegionalGrid.xy;
    float edge=min(min(g.x,g.y),min(_AevoriaRegionalGrid.z-g.x,_AevoriaRegionalGrid.w-g.y));
    if(edge<=2)return meshNormal;
    // Cubic B-spline reconstruction of the existing physical mesh-normal field.
    // Four bilinear taps remove the diagonal interpolation creases without
    // moving a vertex, changing a collider, or inventing a new height field.
    float2 f=frac(g),f2=f*f,f3=f2*f;
    float2 w0=(1-3*f+3*f2-f3)/6,w1=(4-6*f2+3*f3)/6;
    float2 w2=(1+3*f+3*f2-3*f3)/6,w3=f3/6;
    float2 a=w0+w1,b=w2+w3,cell=floor(g);
    float2 low=(cell-1+w1/a+.5)/(_AevoriaRegionalGrid.zw+1);
    float2 high=(cell+1+w3/b+.5)/(_AevoriaRegionalGrid.zw+1);
    float3 s00=SAMPLE_TEXTURE2D_LOD(_AevoriaRegionalNormals,sampler_AevoriaRegionalNormals,low,0).xyz;
    float3 s10=SAMPLE_TEXTURE2D_LOD(_AevoriaRegionalNormals,sampler_AevoriaRegionalNormals,float2(high.x,low.y),0).xyz;
    float3 s01=SAMPLE_TEXTURE2D_LOD(_AevoriaRegionalNormals,sampler_AevoriaRegionalNormals,float2(low.x,high.y),0).xyz;
    float3 s11=SAMPLE_TEXTURE2D_LOD(_AevoriaRegionalNormals,sampler_AevoriaRegionalNormals,high,0).xyz;
    float3 reconstructed=(s00*a.x+s10*b.x)*a.y+(s01*a.x+s11*b.x)*b.y;
    return normalize(lerp(meshNormal,normalize(reconstructed),smoothstep(2,6,edge)*(1-smoothstep(.04,.3,excavationDepth))));
}
float4 OverviewWeights(float2 world,out int2 cell)
{
    float2 g=(world-_AevoriaOverviewGrid.xy)/max(_AevoriaOverviewGrid.zw,1);cell=clamp((int2)floor(g),0,767);float2 f=saturate(g-cell);
    return f.x+f.y<=1?float4(1-f.x-f.y,f.x,f.y,0):float4(0,1-f.y,1-f.x,f.x+f.y-1);
}
float3 MorphWorldSurface(float3 p,inout float3 normal,float backdrop)
{
    if(_AevoriaOverviewReady<.5)return p;
    if(backdrop>1.5)
    {
        float2 edge=min(p.xz-_AevoriaRegionalRect.xy,_AevoriaRegionalRect.xy+_AevoriaRegionalRect.zw-p.xz);float blend=1-smoothstep(0,_AevoriaRegionalGrid.x*8,min(edge.x,edge.y));
        int2 c;float4 w=OverviewWeights(p.xz,c);float h=LOAD_TEXTURE2D(_AevoriaOverviewHeight,c).r*w.x+LOAD_TEXTURE2D(_AevoriaOverviewHeight,c+int2(1,0)).r*w.y+LOAD_TEXTURE2D(_AevoriaOverviewHeight,c+int2(0,1)).r*w.z+LOAD_TEXTURE2D(_AevoriaOverviewHeight,c+1).r*w.w;
        float3 n=LOAD_TEXTURE2D(_AevoriaOverviewNormals,c).xyz*w.x+LOAD_TEXTURE2D(_AevoriaOverviewNormals,c+int2(1,0)).xyz*w.y+LOAD_TEXTURE2D(_AevoriaOverviewNormals,c+int2(0,1)).xyz*w.z+LOAD_TEXTURE2D(_AevoriaOverviewNormals,c+1).xyz*w.w;
        p.y=lerp(p.y,h,blend);normal=normalize(lerp(normal,n,blend));return p;
    }
    if(backdrop>.5||_TileRect.z<=0)return p;
    float2 size=_TileRect.zw,dimensions=_AevoriaCoverageRect.zw/size;
    float2 cell=floor((_TileRect.xy-_AevoriaCoverageRect.xy)/size+.01),local=saturate((p.xz-_TileRect.xy)/size);
    float left=SAMPLE_TEXTURE2D_LOD(_AevoriaCoverage,sampler_AevoriaCoverage,(cell+float2(-.5,.5))/dimensions,0).r;
    float right=SAMPLE_TEXTURE2D_LOD(_AevoriaCoverage,sampler_AevoriaCoverage,(cell+float2(1.5,.5))/dimensions,0).r;
    float down=SAMPLE_TEXTURE2D_LOD(_AevoriaCoverage,sampler_AevoriaCoverage,(cell+float2(.5,-.5))/dimensions,0).r;
    float up=SAMPLE_TEXTURE2D_LOD(_AevoriaCoverage,sampler_AevoriaCoverage,(cell+float2(.5,1.5))/dimensions,0).r;
    float own=SAMPLE_TEXTURE2D_LOD(_AevoriaCoverage,sampler_AevoriaCoverage,(cell+.5)/dimensions,0).r;
    left=min(left,own);right=min(right,own);down=min(down,own);up=min(up,own);
    float width=min(64,min(size.x,size.y)*.25);
    float blend=max(max((1-left)*saturate(1-local.x*size.x/width),(1-right)*saturate(1-(1-local.x)*size.x/width)),max((1-down)*saturate(1-local.y*size.y/width),(1-up)*saturate(1-(1-local.y)*size.y/width)));
    if(blend<=0)return p;
    int2 c;float4 w=OverviewWeights(p.xz,c);
    float h=LOAD_TEXTURE2D(_AevoriaOverviewHeight,c).r*w.x+LOAD_TEXTURE2D(_AevoriaOverviewHeight,c+int2(1,0)).r*w.y+LOAD_TEXTURE2D(_AevoriaOverviewHeight,c+int2(0,1)).r*w.z+LOAD_TEXTURE2D(_AevoriaOverviewHeight,c+1).r*w.w;
    float3 n=LOAD_TEXTURE2D(_AevoriaOverviewNormals,c).xyz*w.x+LOAD_TEXTURE2D(_AevoriaOverviewNormals,c+int2(1,0)).xyz*w.y+LOAD_TEXTURE2D(_AevoriaOverviewNormals,c+int2(0,1)).xyz*w.z+LOAD_TEXTURE2D(_AevoriaOverviewNormals,c+1).xyz*w.w;
    if(_AevoriaRegionalReady>.5&&all(p.xz>=_AevoriaRegionalRect.xy)&&all(p.xz<=_AevoriaRegionalRect.xy+_AevoriaRegionalRect.zw))
    {
        float2 g=(p.xz-_AevoriaRegionalRect.xy)/_AevoriaRegionalGrid.xy;int2 r=clamp((int2)floor(g),0,(int)_AevoriaRegionalGrid.z-1);float2 f=saturate(g-r);float4 rw=f.x+f.y<=1?float4(1-f.x-f.y,f.x,f.y,0):float4(0,1-f.y,1-f.x,f.x+f.y-1);
        float rh=LOAD_TEXTURE2D(_AevoriaRegionalHeight,r).r*rw.x+LOAD_TEXTURE2D(_AevoriaRegionalHeight,r+int2(1,0)).r*rw.y+LOAD_TEXTURE2D(_AevoriaRegionalHeight,r+int2(0,1)).r*rw.z+LOAD_TEXTURE2D(_AevoriaRegionalHeight,r+1).r*rw.w;
        float3 rn=LOAD_TEXTURE2D(_AevoriaRegionalNormals,r).xyz*rw.x+LOAD_TEXTURE2D(_AevoriaRegionalNormals,r+int2(1,0)).xyz*rw.y+LOAD_TEXTURE2D(_AevoriaRegionalNormals,r+int2(0,1)).xyz*rw.z+LOAD_TEXTURE2D(_AevoriaRegionalNormals,r+1).xyz*rw.w;
        float2 edge=min(p.xz-_AevoriaRegionalRect.xy,_AevoriaRegionalRect.xy+_AevoriaRegionalRect.zw-p.xz);float regional=smoothstep(0,_AevoriaRegionalGrid.x*8,min(edge.x,edge.y));h=lerp(h,rh,regional);n=normalize(lerp(n,rn,regional));
    }
    p.y=lerp(p.y,h,blend);normal=normalize(lerp(normal,n,blend));return p;
}
void ClipWorldCoverage(float3 p,float backdrop,float screenNoise)
{
    float2 uv=(p.xz-_AevoriaCoverageRect.xy)/_AevoriaCoverageRect.zw;
    if(all(uv>=0)&&all(uv<=1))clip(screenNoise-SAMPLE_TEXTURE2D(_AevoriaCoverage,sampler_AevoriaCoverage,uv).r);
    if(backdrop<1.5&&_AevoriaRegionalReady>.5&&all(p.xz>=_AevoriaRegionalRect.xy)&&all(p.xz<=_AevoriaRegionalRect.xy+_AevoriaRegionalRect.zw))clip(-1);
}
void ClipExcavations(float3 p,float editable)
{
    if((any(p.xz<_AevoriaWorldRect.xy)||any(p.xz>_AevoriaWorldRect.xy+_AevoriaWorldRect.zw))&&p.y>_AevoriaBoundarySeaLevel-.03)clip(-1);
    if(editable>.5)return;
    if(_AevoriaSolidReady>.5)
    {
        int3 cell=(int3)floor((p-_AevoriaSolidGrid.xyz)/_AevoriaSolidGrid.w);
        if(all(cell>=0)&&all(cell<(int)_AevoriaSolidCells)&&LOAD_TEXTURE3D(_AevoriaSolidCoverage,cell).r>.5)clip(-1);
    }
}
#endif
