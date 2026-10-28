#ifndef AEVORIA_GENESIS_MATERIALS
#define AEVORIA_GENESIS_MATERIALS
// Eight float4 rows per immutable recipe; four split-frequency origins each.
float4 _GenesisRecipes[128],_GenesisCells[64],_GenesisFractions[64];
uint _GenesisSeed;float _GenesisReady,_GenesisRecipeVersion;
TEXTURE2D(_GenesisGeology);SAMPLER(sampler_GenesisGeology);
TEXTURE2D(_GenesisHabitats);SAMPLER(sampler_GenesisHabitats);
uint GenesisHash(int3 p,uint seed)
{uint h=(uint)p.x*374761393u^(uint)p.y*2246822519u^(uint)p.z*668265263u^seed;h=(h^(h>>13))*1274126177u;return h^(h>>16);}
float GenesisCorner(int3 cell,float3 offset,uint seed)
{
    uint h=GenesisHash(cell,seed);float3 gradient=float3((h&1)?1:-1,(h&2)?1:-1,(h&4)?1:-1)*.57735027;
    return dot(gradient,offset);
}
void GenesisCoordinates(float3 local,int index,float frequency,out int3 origin,out float3 fractional)
{
    // Multiplying an entire ~2km relative position by a fine frequency loses
    // fractional phase before an octave is evaluated. Split local metres too;
    // this keeps exact integer-frequency products out of the small residual.
    float3 metres=floor(local),coarse=metres*frequency;
    origin=(int3)_GenesisCells[index].xyz+(int3)floor(coarse);
    fractional=(local-metres)*frequency+frac(coarse)+_GenesisFractions[index].xyz;
}
float GenesisNoise(float3 local,int id,int band,float frequency,int octave,uint seed,int projection)
{
    int index=id*4+band;float factor=exp2((float)octave);
    float3 residual;int3 rawOrigin;GenesisCoordinates(local,index,frequency,rawOrigin,residual);
    float3 q=residual*factor;int3 origin;
    if(octave<0)
    {
        int divisor=1<<(-octave);origin=rawOrigin/divisor;
        q+=(rawOrigin-origin*divisor)*factor;
    }
    else origin=rawOrigin*(1<<octave);
    // Triplanar projections discard the normal axis before hashing. Both the
    // integer lattice and its fractional origin use the same surface plane.
    if(projection==0){q.x=0;origin.x=0;}else if(projection==1){q.y=0;origin.y=0;}else if(projection==2){q.z=0;origin.z=0;}
    int3 cell=(int3)floor(q)+origin;float3 f=frac(q),u=f*f*f*(f*(f*6-15)+10);
    float a=lerp(GenesisCorner(cell,f,seed),GenesisCorner(cell+int3(1,0,0),f-float3(1,0,0),seed),u.x);
    float b=lerp(GenesisCorner(cell+int3(0,1,0),f-float3(0,1,0),seed),GenesisCorner(cell+int3(1,1,0),f-float3(1,1,0),seed),u.x);
    float c=lerp(GenesisCorner(cell+int3(0,0,1),f-float3(0,0,1),seed),GenesisCorner(cell+int3(1,0,1),f-float3(1,0,1),seed),u.x);
    float d=lerp(GenesisCorner(cell+int3(0,1,1),f-float3(0,1,1),seed),GenesisCorner(cell+int3(1,1,1),f-1,seed),u.x);
    return saturate(.5+lerp(lerp(a,b,u.y),lerp(c,d,u.y),u.z));
}
float GenesisTriplanar(float3 local,float3 weights,int id,int band,float frequency,int octave,uint seed,float footprint)
{
    float fade=1-smoothstep(.22,.8,footprint*frequency*(1<<octave));
    if(fade<=.001)return .5;
    float value=0;
    [loop]for(int axis=0;axis<3;axis++)if(weights[axis]>.005)value+=weights[axis]*GenesisNoise(local,id,band,frequency,octave,seed,axis);
    return lerp(.5,value,fade);
}
float GenesisCellular(float3 local,float3 weights,int id,float frequency,uint seed,float footprint)
{
    float fade=1-smoothstep(.22,.8,footprint*frequency);if(fade<=.001)return .5;
    float value=0;int index=id*4+1;
    [loop]for(int axis=0;axis<3;axis++)if(weights[axis]>.005)
    {
        float3 q;int3 origin;GenesisCoordinates(local,index,frequency,origin,q);
        float2 projected=axis==0?q.yz:axis==1?q.xz:q.xy;
        int2 baseCell=(int2)floor(projected),global=baseCell+(axis==0?origin.yz:axis==1?origin.xz:origin.xy);
        float2 f=frac(projected);float nearest=8,second=8;
        [loop]for(int z=-1;z<=1;z++)[loop]for(int x=-1;x<=1;x++)
        {
            int2 offset=int2(x,z);uint hash=GenesisHash(int3(global+offset,axis),seed);
            float2 featurePosition=float2(hash&65535,hash>>16)/65535.0;
            float2 delta=offset+featurePosition-f;float d=dot(delta,delta);
            if(d<nearest){second=nearest;nearest=d;}else second=min(second,d);
        }
        value+=weights[axis]*saturate((sqrt(second)-sqrt(nearest))*1.8);
    }
    return lerp(.5,value,fade);
}
struct GenesisSurface{float3 albedo;float height,roughness,occlusion;};
GenesisSurface GenesisRockFamily(float3 local,float3 n,float footprint,float4 geology,float moisture,float temperature,int id)
{
    float4 dark=_GenesisRecipes[id*8],light=_GenesisRecipes[id*8+1],scales=_GenesisRecipes[id*8+2];
    float4 structure=_GenesisRecipes[id*8+3],response=_GenesisRecipes[id*8+4],control=_GenesisRecipes[id*8+5];
    uint seed=_GenesisSeed^((uint)control.z|((uint)control.w<<16));
    float3 weights=pow(abs(n),4);weights/=max(.0001,dot(weights,1));
    float macro=GenesisNoise(local,id,0,scales.x,0,seed,3);
    float3 warp=float3(GenesisNoise(local,id,1,scales.y,-2,seed^0x9e3779b9u,3),GenesisNoise(local,id,1,scales.y,-2,seed^0x7f4a7c15u,3),GenesisNoise(local,id,1,scales.y,-2,seed^0x94d049bbu,3))-.5;
    float3 warped=local+warp*scales.w;
    float meso=0,total=0,amplitude=1,cellEdge=.5;
    [unroll]for(int octave=0;octave<4;octave++)
    {
        if(octave>=(int)control.y)break;
        float value=GenesisTriplanar(warped,weights,id,1,scales.y,octave,seed^0x85ebca6bu,footprint);
        if(octave==0)cellEdge=control.x>1.5?GenesisCellular(warped,weights,id,scales.y,seed^0x85ebca6bu,footprint):value;
        if(control.x>.5&&control.x<1.5)value=1-abs(value*2-1);
        meso+=value*amplitude;total+=amplitude;amplitude*=.5;
    }
    meso/=max(.001,total);
    float3 strike=normalize(float3(geology.x,0,geology.y)+float3(.001,0,.001));
    float3 beddingPlane=float3(-strike.z*structure.y,1,strike.x*structure.y);
    int strataIndex=id*4+3;
    float highLayer=dot(_GenesisCells[strataIndex].xyz,beddingPlane);
    float strataPhase=frac(highLayer)+dot(warped*structure.x+_GenesisFractions[strataIndex].xyz,beddingPlane)+(meso-.5)*.6;
    int layer=(int)floor(highLayer)+(int)floor(strataPhase);
    float layerFraction=frac(strataPhase);
    float strataFade=1-smoothstep(.15,.65,footprint*structure.x);
    // Sediment layers have stable, independent mineral values. A sine here
    // produced evenly repeating wavy stripes at medium camera distances.
    float layerA=(GenesisHash(int3(layer,701,0),seed)&65535u)/65535.0;
    float layerB=(GenesisHash(int3(layer+1,701,0),seed)&65535u)/65535.0;
    float bedding=lerp(layerA,layerB,smoothstep(.08,.92,layerFraction));
    bedding=lerp(.5,bedding,strataFade);
    // Broad fracture networks follow a warped coherent field; no repeated
    // source texture or square assignment tile is involved.
    float fissure=1-smoothstep(structure.z,structure.z*2.4,control.x>1.5?cellEdge:abs(meso-.5));
    // Joints occur in weathered exposures. Cellular distance must not become
    // the continuous height/color surface: that made entire cliffs look paved.
    fissure*=smoothstep(-.10,.08,warp.x);
    fissure*=1-smoothstep(.25,.9,footprint*scales.y);
    float jointMetres=structure.z/max(.0001,scales.y*1.8);
    fissure*=1-smoothstep(jointMetres*.5,jointMetres*2,footprint);
    float micro=GenesisTriplanar(warped,weights,id,2,scales.z,0,seed^0xc2b2ae35u,footprint);
    GenesisSurface result;
    float mesoAppearance=1-smoothstep(.025,.18,footprint*scales.y);
    float weathered=lerp(.5,smoothstep(-.10,.12,warp.y),mesoAppearance);
    float layerStrength=id==2?.015:id==3?.25:id==1?.32:id==4?.22:.08;
    if(id==5){layerStrength=.025;fissure*=.12;}
    float tone=saturate(.38+macro*.35+weathered*.10+(bedding-.5)*layerStrength);
    result.albedo=lerp(dark.rgb,light.rgb,tone)*(1-fissure*(.25+structure.w*.25));
    float calcite=smoothstep(.82,.98,bedding)*(.08+weathered*.18)*(id==0?1:.15);
    result.albedo=lerp(result.albedo,light.rgb,calcite);
    result.albedo*=1+(micro-.5)*.17;
    float damp=saturate((moisture-.35)*.65)*response.x;
    result.albedo*=1-damp*.20;
    result.height=((meso-.5)*.28+(bedding-.5)*.12-fissure*.045+(micro-.5)*.05)*light.a;
    result.roughness=clamp(dark.a+(micro-.5)*.12-damp*.25,.08,1);
    result.occlusion=1-fissure*.28;
    return result;
}
GenesisSurface GenesisRock(float3 local,float3 n,float footprint,float4 geology,float moisture,float temperature)
{return GenesisRockFamily(local,n,footprint,geology,moisture,temperature,0);}
GenesisSurface GenesisBlend(GenesisSurface a,GenesisSurface b,float weight)
{
    GenesisSurface r;r.albedo=lerp(a.albedo,b.albedo,weight);r.height=lerp(a.height,b.height,weight);
    r.roughness=lerp(a.roughness,b.roughness,weight);r.occlusion=lerp(a.occlusion,b.occlusion,weight);return r;
}
// Small litter elements have independent positions, rotations and shapes in a
// hashed world lattice. The lattice is only an acceleration structure: every
// neighbouring cell contributes across its borders, so there is no square stamp.
float2 GenesisLitter(float3 local,int id,float frequency,uint seed,float footprint,float needles)
{
    float fade=1-smoothstep(.035,.18,footprint);if(fade<.001)return 0;
    int3 origin;float3 q;GenesisCoordinates(local,id*4+2,frequency,origin,q);
    int2 cell=(int2)floor(q.xz);float2 f=frac(q.xz);float leaf=0,vein=0;
    [loop]for(int z=-1;z<=1;z++)[loop]for(int x=-1;x<=1;x++)
    {
        int2 offset=int2(x,z);uint h=GenesisHash(int3(origin.xz+cell+offset,97),seed);
        float2 leafSite=float2(h&65535u,h>>16)/65535.0;
        float angle=(h&1023u)*.006135923;float2 d=f-offset-leafSite;
        float2 rotated=float2(d.x*cos(angle)+d.y*sin(angle),-d.x*sin(angle)+d.y*cos(angle));
        float lengthScale=lerp(.16,.34,(h&255u)/255.0),width=lerp(.11,.018,needles);
        float shape=length(rotated/float2(lengthScale,width));float mask=1-smoothstep(.72,1,shape);
        leaf=max(leaf,mask);vein=max(vein,mask*(1-smoothstep(.005,.018,abs(rotated.y))));
    }
    return float2(leaf,vein)*fade;
}
GenesisSurface GenesisGround(float3 local,float footprint,float moisture,float temperature,int id)
{
    float4 dark=_GenesisRecipes[id*8],light=_GenesisRecipes[id*8+1],scales=_GenesisRecipes[id*8+2];
    float4 structure=_GenesisRecipes[id*8+3],response=_GenesisRecipes[id*8+4],control=_GenesisRecipes[id*8+5];
    uint seed=_GenesisSeed^((uint)control.z|((uint)control.w<<16));
    float macro=GenesisNoise(local,id,0,scales.x,0,seed,3);
    float broad=GenesisNoise(local,id,1,scales.y,-2,seed^0x9e3779b9u,3);
    broad=lerp(broad,.5,smoothstep(.2,.8,footprint*scales.y*.25));
    float3 warped=local+float3(broad-.5,0,macro-.5)*scales.w;
    float meso=GenesisNoise(warped,id,1,scales.y,0,seed^0x85ebca6bu,3);
    meso=lerp(meso,.5,smoothstep(.2,.8,footprint*scales.y));
    float micro=GenesisNoise(warped,id,2,scales.z,0,seed^0xc2b2ae35u,3);
    micro=lerp(micro,.5,smoothstep(.18,.7,footprint*scales.z));
    float tone=saturate(.2+macro*.5+meso*.35);
    GenesisSurface r;r.albedo=lerp(dark.rgb,light.rgb,tone);r.height=(meso-.5)*.08+(micro-.5)*.025;
    r.roughness=dark.a;r.occlusion=1;
    if(id==6) // Damp soil, small mineral grains and erosion patches.
    {
        float stone=smoothstep(.60,.70,micro)*(1-smoothstep(.04,.20,footprint));
        r.albedo=lerp(r.albedo,float3(.31,.30,.25),stone*.55);
        r.height+=stone*.02;r.roughness=lerp(.92,.58,moisture);
    }
    if(id==7||id==8) // Coherent turf and dry herb patches, with exposed earth.
    {
        float dry=smoothstep(.40,.65,meso)*(1-smoothstep(.25,.7,moisture));
        float earth=smoothstep(.63,.76,meso)*(1-smoothstep(.55,.9,moisture));
        r.albedo=lerp(r.albedo,id==8?float3(.31,.29,.12):float3(.30,.27,.105),dry*.48);
        r.albedo=lerp(r.albedo,float3(.16,.105,.055)*(.85+macro*.3),earth*.55);
        r.height=(micro-.5)*.035;r.roughness=.94;
    }
    if(id==9) // Distinct needle/leaf litter and moisture-dependent moss.
    {
        float2 litter=GenesisLitter(warped,id,scales.z,seed,footprint,1-smoothstep(5,13,temperature));
        float moss=smoothstep(.35,.66,meso)*smoothstep(.32,.75,moisture)*response.z;
        r.albedo=lerp(r.albedo,float3(.075,.155,.035)*(.8+macro*.5),moss);
        r.albedo=lerp(r.albedo,float3(.22,.13,.045)*(.8+meso*.4),litter.x*.72);
        r.albedo*=1-litter.y*.22;r.height+=litter.x*.009;r.roughness=.95;
    }
    if(id==10) // Wind-aligned, warped sand ripples. Integer phase stays bounded.
    {
        int3 cell;float3 residual;GenesisCoordinates(warped,id*4+2,scales.z,cell,residual);
        float phase=dot((float2)(cell.xz%5)+residual.xz,float2(.8,.6))+(meso-.5)*1.8;
        float ripple=sin(phase*6.2831853);
        float visible=1-smoothstep(.12,.5,footprint*scales.z);
        r.albedo*=1+ripple*.07*visible;r.height=ripple*.018*visible+(meso-.5)*.025;
    }
    if(id==11) // Efflorescent salt crust with irregular polygonal contraction.
    {
        float edge=GenesisCellular(warped,float3(0,1,0),id,scales.y,seed,footprint);
        float crack=(1-smoothstep(.025,.065,edge))*(1-smoothstep(.15,.6,footprint));
        r.albedo*=1-crack*.32;r.height-=crack*.018;r.occlusion=1-crack*.12;
    }
    if(id==12) // Fine snow crystals disappear before they can shimmer overhead.
    {
        r.albedo=lerp(dark.rgb,light.rgb,.65+meso*.25);
        r.albedo*=1+(micro-.5)*.04;r.height=(micro-.5)*.012;r.roughness=.76;
    }
    if(id==13) // Translucent-looking glacial mineral bands without alpha holes.
    {
        float vein=1-smoothstep(.015,.06,abs(meso-.5));
        r.albedo=lerp(r.albedo,float3(.68,.80,.83),vein*.3);r.height=(micro-.5)*.006;r.roughness=.22+meso*.12;
    }
    float wet=saturate((moisture-.35)*.8)*response.x;
    r.albedo*=1-wet*.24;r.roughness=max(.12,r.roughness-wet*.25);
    r.height*=light.a;return r;
}
float3 GenesisPerturb(float3 n,float3 local,float height)
{
    float3 dx=ddx(local),dy=ddy(local),a=cross(dy,n),b=cross(n,dx);float determinant=dot(dx,a);
    float3 gradient=(ddx(height)*a+ddy(height)*b)*sign(determinant)/max(abs(determinant),max(.00001,length(dx)*length(dy)*.08));
    gradient*=min(1,.38/max(.00001,length(gradient)));
    return normalize(n-gradient);
}
#endif
