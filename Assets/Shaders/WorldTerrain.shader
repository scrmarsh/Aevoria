Shader "Aevoria/Terrain"
{
    Properties { _DetailScale("Detail scale",Float)=0.3 _Fade("Streaming fade",Float)=1 _Outgoing("Outgoing LOD",Float)=0 _Backdrop("Overview",Float)=0 _EditablePatch("Solid excavation",Float)=0 _TileRect("Tile bounds",Vector)=(0,0,0,0) }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma target 4.5
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldOrigin.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _DetailScale,_Fade,_Outgoing,_Backdrop,_EditablePatch;float4 _TileRect;
            CBUFFER_END
            TEXTURE2D(_AevoriaCoverage); SAMPLER(sampler_AevoriaCoverage);
            TEXTURE2D(_AevoriaClimate); SAMPLER(sampler_AevoriaClimate);
            float _AevoriaClimateReady,_AevoriaSeasonTemperature;
            TEXTURE2D(_AevoriaMaterials);SAMPLER(sampler_AevoriaMaterials);float _AevoriaMaterialsReady;
            float _AevoriaTerrainDiagnostic,_AevoriaTerrainTextureQuality;
            TEXTURE2D(_AevoriaAtlasBiomes);SAMPLER(sampler_AevoriaAtlasBiomes);float _AevoriaAtlasReady;
            float4 _AevoriaNoiseOrigins[10];
            TEXTURE2D(_AevoriaEcology); SAMPLER(sampler_AevoriaEcology);
            float _AevoriaEcologyReady,_AevoriaDistantScenery;
            float4 _AevoriaEcologyRect;
            float4 _AevoriaWorldRect,_AevoriaHaze; float _AevoriaSeaLevel;
            TEXTURE2D(_AevoriaSeasonal); SAMPLER(sampler_AevoriaSeasonal);float _AevoriaSeasonalReady;
            #include "WorldTerrainFields.hlsl"
            #include "SocietyFields.hlsl"
            #include "WorldWeatherFields.hlsl"
            #include "WorldHazeFields.hlsl"
            #include "WorldCoastFields.hlsl"
            #include "WorldRiverFields.hlsl"
            #include "GenesisMaterials.hlsl"
            struct A {float4 positionOS:POSITION; float3 normalOS:NORMAL;float4 bank:TEXCOORD0;float2 depth:TEXCOORD2;};
            struct V {float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; float fog:TEXCOORD2;float4 bank:TEXCOORD3;float3 relativeWS:TEXCOORD4;float depth:TEXCOORD5;};
            float Hash(float2 p) {float3 p3=frac(float3(p.xyx)*0.1031); p3+=dot(p3,p3.yzx+33.33); return frac((p3.x+p3.y)*p3.z);}
            float GradientCorner(int2 cell,float2 p){uint h=(uint)cell.x*374761393u^(uint)cell.y*668265263u;h=(h^(h>>13))*1274126177u;h^=h>>16;uint k=h&7;float2 gradient=k<4?float2((k&1)!=0?-1:1,(k&2)!=0?-1:1):k<6?float2(k==4?1:-1,0):float2(0,k==6?1:-1);float t=max(0,.5-dot(p,p));return t*t*t*t*dot(gradient,p);}
            float Noise(float2 p){float2 cell=floor(p+(p.x+p.y)*.3660254038);float2 q=p-cell+(cell.x+cell.y)*.2113248654;int2 next=q.x>q.y?int2(1,0):int2(0,1);return saturate(.5+35*(GradientCorner((int2)cell,q)+GradientCorner((int2)cell+next,q-next+.2113248654)+GradientCorner((int2)cell+1,q-1+.4226497308)));}
            // Split the geographic lattice origin before forming fine noise
            // coordinates. Adding a million-metre origin to a subpixel float
            // position first destroys its derivatives at excavation scale.
            float StableNoise(float2 local,int band)
            {
                float4 origin=_AevoriaNoiseOrigins[band];float2 skew=local+(local.x+local.y)*.3660254038+origin.zw;
                int2 cell=(int2)origin.xy+(int2)floor(skew);float2 f=frac(skew),q=f-(f.x+f.y)*.2113248654;
                int2 next=q.x>q.y?int2(1,0):int2(0,1);
                return saturate(.5+35*(GradientCorner(cell,q)+GradientCorner(cell+next,q-next+.2113248654)+GradientCorner(cell+1,q-1+.4226497308)));
            }
            static float terrainPixelMeters;
            float FilteredNoise(float2 p,float frequency){return lerp(Noise(p),.5,smoothstep(.25,.9,terrainPixelMeters*frequency));}
            V Vert(A i) { V o;o.bank=i.bank;o.depth=i.depth.x;float3 physical=TransformObjectToWorld(i.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.positionWS=MorphWorldSurface(physical+_AevoriaOrigin,o.normalWS,_Backdrop);physical.y=o.positionWS.y;o.relativeWS=physical;o.positionCS=TransformWorldToHClip(physical);o.fog=ComputeFogFactor(o.positionCS.z);return o; }
            half4 Frag(V i):SV_Target
            {
                ClipExcavations(i.positionWS,_EditablePatch);
                float screenNoise=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(0.06711056,0.00583715))));
                if(_Backdrop>0.5) ClipWorldCoverage(i.positionWS,_Backdrop,screenNoise);
                else { if(_Outgoing>0.5) clip(screenNoise-_Fade); else clip(_Fade-screenNoise); }
                float3 p=i.positionWS;float3 surfaceNormal=normalize(i.normalWS);
                if(_GenesisReady>.5)surfaceNormal=WorldShadingNormal(p,surfaceNormal,i.depth*_EditablePatch);
                float3 n=surfaceNormal;float3 weights=pow(abs(n),4); weights/=dot(weights,1);
                // A common physical filter footprint across mesh LODs. Nested
                // screen derivatives of procedural noise are undefined and made
                // otherwise flat local tiles acquire rectangular colour changes.
                float3 toView=GetCameraPositionWS()-i.relativeWS;
                float grazing=max(.08,abs(dot(normalize(toView),n)));
                terrainPixelMeters=max(.001,2*length(toView)/max(1,_ScreenParams.y*abs(UNITY_MATRIX_P._m11)))/grazing*lerp(1.6,.7,saturate(_AevoriaTerrainTextureQuality*.5));
                float macro=FilteredNoise(p.xz*.0004,.0004)*.35+FilteredNoise(p.xz*0.0022,.0022)*.65, patch=FilteredNoise(float2(p.x*0.021+p.z*0.014,p.z*0.021-p.x*0.014)+FilteredNoise(p.xz*0.002,.002)*3,.026);
                float detail=FilteredNoise(p.zy*0.65,.65)*weights.x+FilteredNoise(p.xz*0.65,.65)*weights.y+FilteredNoise(p.xy*0.65,.65)*weights.z;
                float grain=FilteredNoise(p.zy*3.1,3.1)*weights.x+FilteredNoise(p.xz*3.1,3.1)*weights.y+FilteredNoise(p.xy*3.1,3.1)*weights.z;
                float warp=FilteredNoise(p.xz*0.011,.011)*3;
                float weather=FilteredNoise(float2(p.z*0.15+warp,p.y*0.025),.16)*weights.x+FilteredNoise(p.xz*0.13,.13)*weights.y+FilteredNoise(float2(p.x*0.15+warp,p.y*0.025),.16)*weights.z;
                float verticalFracture=1-smoothstep(0.025,0.08,abs(weather-0.5));
                float fractures=abs(detail-0.48); float fracture=1-smoothstep(0.018,0.06,fractures);
                float bedding=FilteredNoise(float2(p.x*0.011+p.y*0.045,p.z*0.014+p.y*0.018),.05);
                float distanceFade=1-smoothstep(250,4000,distance(GeographicCameraPosition(),p));
                float footprint=max(length(ddx(p)),length(ddy(p)));
                float fineFade=distanceFade*(1-smoothstep(0.2,1.5,footprint));
                float h=p.y-_AevoriaSeaLevel,slope=1-n.y;
                half3 grass=lerp(half3(0.075,0.15,0.035),half3(0.23,0.32,0.09),macro);
                float2 climateUV=saturate((p.xz-_AevoriaWorldRect.xy)/_AevoriaWorldRect.zw);
                float4 climate=SAMPLE_TEXTURE2D(_AevoriaClimate,sampler_AevoriaClimate,(climateUV*128+.5)/129);
                float4 material=lerp(float4(1.5,.5,1.5,0),SAMPLE_TEXTURE2D(_AevoriaMaterials,sampler_AevoriaMaterials,(climateUV*128+.5)/129),_AevoriaMaterialsReady);
                float meanTemperature=WorldAltitudeTemperature(climate.r,climate.a,p.y,_AevoriaSeaLevel);
                float temperature=lerp(18-WorldAltitudeCooling(h),meanTemperature,_AevoriaClimateReady)+_AevoriaSeasonTemperature;
                temperature=lerp(temperature,RegionalSurfaceTemperature(p),_AevoriaWeatherReady);
                float moisture=lerp(0.6,climate.g,_AevoriaClimateReady);
                float rockWeight=max(smoothstep(.045,.25,slope),smoothstep(1000,2500,h)*.85*(1-smoothstep(0,10,temperature)));
                float4 genesisHabitat=SAMPLE_TEXTURE2D(_GenesisHabitats,sampler_GenesisHabitats,(climateUV*128+.5)/129);
                float4 ecology=SAMPLE_TEXTURE2D(_AevoriaEcology,sampler_AevoriaEcology,saturate((p.xz-_AevoriaEcologyRect.xy)/_AevoriaEcologyRect.zw));
                float inside=all((p.xz-_AevoriaEcologyRect.xy)>=0)&&all((p.xz-_AevoriaEcologyRect.xy)<=_AevoriaEcologyRect.zw);
                float ecoReady=_AevoriaEcologyReady*inside;
                // Climate supplies continuous material zones; ecological cell
                // moisture remains simulation state, not a terrain colour grid.
                grass=lerp(half3(.21,.29,.085),half3(.075,.205,.065),smoothstep(.30,.85,moisture));
                grass*=.88+macro*.30;
                grass=lerp(grass,half3(.23,.255,.16),1-smoothstep(-8,6,temperature));
                grass=lerp(grass,half3(.105,.18,.065),ecology.r*ecoReady*.25);
                grass=lerp(grass,half3(0.18,0.18,0.08),smoothstep(0.64,0.90,patch)*0.18);
                float blades=FilteredNoise(p.xz*float2(3.7,0.42)+detail,3.7);
                grass*=1+(detail-0.5)*0.18*distanceFade+(grain-0.5)*0.10*fineFade+(blades-0.5)*0.10*fineFade;
                // Rock varies in its own surface planes. Projecting the grass
                // patch field onto XZ stretched its high-contrast cells down
                // steep mountain faces into a woven-looking pattern.
                float rockMacro=FilteredNoise(p.zy*.0015,.0015)*weights.x+FilteredNoise(p.xz*.0015,.0015)*weights.y+FilteredNoise(p.xy*.0015,.0015)*weights.z;
                float rockMeso=FilteredNoise(p.zy*.018,.018)*weights.x+FilteredNoise(p.xz*.018,.018)*weights.y+FilteredNoise(p.xy*.018,.018)*weights.z;
                half3 rock=lerp(half3(.27,.285,.27),half3(.42,.41,.37),rockMacro*.85+bedding*.15);
                rock*=1+(rockMeso-.5)*.08*distanceFade;
                rock*=lerp(.9,1.05,saturate((material.b-.8)/1.4));
                rock*=1-fracture*0.08*distanceFade;
                rock*=1+(grain-0.5)*0.28*fineFade;
                rock*=1-verticalFracture*(0.08+FilteredNoise(p.xz*0.07,.07)*0.12)*distanceFade*(1-smoothstep(2,12,footprint));
                GenesisSurface genesis=(GenesisSurface)0,genesisSand=(GenesisSurface)0;
                if(_GenesisReady>.5)
                {
                    float4 geological=SAMPLE_TEXTURE2D(_GenesisGeology,sampler_GenesisGeology,(climateUV*128+.5)/129);
                    GenesisSurface stone=(GenesisSurface)0;stone.albedo=rock;stone.roughness=.8;stone.occlusion=1;
                    if(rockWeight>.001)
                    {
                    stone=GenesisRock(i.relativeWS,n,terrainPixelMeters,geological,moisture,temperature);
                    float granite=(1-smoothstep(2,12,temperature))*smoothstep(.3,.8,material.b/2);
                    float sandstone=(1-smoothstep(.22,.38,moisture))*smoothstep(8,20,temperature);
                    float metamorphic=smoothstep(.65,.92,geological.w)*smoothstep(.4,.8,geological.z);
                    float sediment=(1-smoothstep(.5,.9,geological.z))*smoothstep(.5,1.2,material.g)*.45;
                    if(granite>.01)stone=GenesisBlend(stone,GenesisRockFamily(i.relativeWS,n,terrainPixelMeters,geological,moisture,temperature,2),granite);
                    if(sandstone>.01)stone=GenesisBlend(stone,GenesisRockFamily(i.relativeWS,n,terrainPixelMeters,geological,moisture,temperature,1),sandstone);
                    if(metamorphic>.01)stone=GenesisBlend(stone,GenesisRockFamily(i.relativeWS,n,terrainPixelMeters,geological,moisture,temperature,3),metamorphic);
                    if(sediment>.01)stone=GenesisBlend(stone,GenesisRockFamily(i.relativeWS,n,terrainPixelMeters,geological,moisture,temperature,4),sediment);
                    }
                    rock=stone.albedo;
                    GenesisSurface ground=(GenesisSurface)0;ground.albedo=grass;ground.roughness=.94;ground.occlusion=1;
                    if(rockWeight<.999)
                    {
                    ground=GenesisGround(i.relativeWS,terrainPixelMeters,moisture,temperature,7);
                    if(genesisHabitat.g>.01)ground=GenesisBlend(ground,GenesisGround(i.relativeWS,terrainPixelMeters,moisture,temperature,8),genesisHabitat.g);
                    float forestFloor=smoothstep(.2,.85,genesisHabitat.r)*(1-smoothstep(.20,.45,slope));
                    if(forestFloor>.01)ground=GenesisBlend(ground,GenesisGround(i.relativeWS,terrainPixelMeters,moisture,temperature,9),forestFloor);
                    }
                    grass=ground.albedo;
                    genesis=GenesisBlend(ground,stone,rockWeight);
                    genesisSand=ground;
                    if(rockWeight<.999)genesisSand=GenesisGround(i.relativeWS,terrainPixelMeters,moisture,temperature,10);
                }
                half3 sand=lerp(half3(0.35,0.30,0.18),half3(0.72,0.64,0.42),smoothstep(0,5,h));
                sand*=1+(grain-0.5)*0.18*fineFade;
                if(_GenesisReady>.5)sand=genesisSand.albedo;
                half3 base=lerp(grass,rock,rockWeight);
                float desert=(1-smoothstep(.17,.29,moisture))*smoothstep(7,18,temperature);half3 desertSand=lerp(half3(.48,.37,.19),half3(.78,.65,.38),macro);base=lerp(base,desertSand,desert*(1-rockWeight*.75));
                if(_GenesisReady>.5)
                {
                    float sandCover=desert*(1-rockWeight)*smoothstep(.1,.65,material.g);
                    base=lerp(lerp(grass,rock,rockWeight),sand,sandCover);genesis=GenesisBlend(genesis,genesisSand,sandCover);
                    if(genesisHabitat.b>.01)
                    {
                        GenesisSurface salt=GenesisGround(i.relativeWS,terrainPixelMeters,moisture,temperature,11);
                        float crust=genesisHabitat.b*(1-rockWeight);base=lerp(base,salt.albedo,crust);genesis=GenesisBlend(genesis,salt,crust);
                    }
                }
                half4 habitat=SAMPLE_TEXTURE2D(_AevoriaAtlasBiomes,sampler_AevoriaAtlasBiomes,(climateUV*128+.5)/129);
                base=lerp(base,habitat.rgb*(.88+macro*.24),_AevoriaAtlasReady*(1-rockWeight)*lerp(.8,.18,_GenesisReady));
                // Aggregate supported habitat cover once individual
                // instanced crowns are smaller than a pixel. No square LOD tile
                // supplies its own forest color or seed.
                float canopy=FilteredNoise(p.xz*.0031,.0031);
                half3 canopyColor=lerp(half3(.045,.115,.04),half3(.09,.18,.055),canopy);
                float forest=smoothstep(.06,.55,habitat.a)*(1-rockWeight)*_AevoriaAtlasReady*smoothstep(2,14,terrainPixelMeters);
                base=lerp(base,canopyColor,forest*.85);
                base=lerp(base,half3(0.028,0.021,0.016)*(0.7+patch*0.6),ecology.b*ecoReady*(1-rockWeight));
                float beachWidth=lerp(4,24,FilteredNoise(p.xz*0.0031,.0031))*lerp(1,0.22,smoothstep(0.08,0.32,slope));
                half3 shore=lerp(sand,rock,smoothstep(0.12,0.42,slope));
                float coastal=1-smoothstep(.5,beachWidth,h);
                if(_AevoriaCoastReady>.5&&abs(_AevoriaSeaLevel)<.01)
                {
                    float coast=WorldCoastDistance(p.xz,_AevoriaWorldRect),width=lerp(40,110,FilteredNoise(p.xz*.0031,.0031));
                    coastal=(1-smoothstep(3,width,coast))*(1-smoothstep(3,12,h));
                }
                base=lerp(base,shore,coastal);
                if(_GenesisReady>.5)genesis=GenesisBlend(genesis,genesisSand,coastal*(1-smoothstep(.12,.42,slope)));
                float3 channel;float3 riverBank=WorldRiverBank(p.xz,_AevoriaWorldRect,channel);
                float bankHeight=p.y-riverBank.y;
                float bankWidth=clamp(channel.x*.12,1.5,14);
                float bankFootprint=max(length(ddx(p.xz)),length(ddy(p.xz)));
                float bankCoverage=saturate((channel.x*2+bankWidth*2)/max(.01,bankFootprint));
                float bank=riverBank.z*(1-smoothstep(.3,6,abs(bankHeight)))*(1-smoothstep(.5,bankWidth,max(0,riverBank.x)))*bankCoverage;
                half3 gravel=lerp(sand*.82,rock,FilteredNoise(p.xz*.9,.9)*.65+smoothstep(.12,.45,slope)*.35);
                gravel*=lerp(.48,.85,smoothstep(.1,4,bankHeight));base=lerp(base,gravel,bank*.88);
                if(_GenesisReady>.5&&bank>.01)
                {
                    float4 geological=SAMPLE_TEXTURE2D(_GenesisGeology,sampler_GenesisGeology,(climateUV*128+.5)/129);
                    GenesisSurface pebbles=GenesisRockFamily(i.relativeWS,n,terrainPixelMeters,geological,max(moisture,.75),temperature,5);
                    base=lerp(base,pebbles.albedo,bank*.75);genesis=GenesisBlend(genesis,pebbles,bank*.75);
                }
                float4 seasonal=SAMPLE_TEXTURE2D(_AevoriaSeasonal,sampler_AevoriaSeasonal,saturate((p.xz-_AevoriaEcologyRect.xy)/max(_AevoriaEcologyRect.zw,1)));
                float permanent=(1-smoothstep(-5,2,meanTemperature+climate.b))*smoothstep(.08,.40,moisture)*_AevoriaClimateReady;
                float snowCover=max(permanent,seasonal.r*_AevoriaSeasonalReady);
                float exposure=FilteredNoise(p.xz*.0007,.0007)*.18+FilteredNoise(p.xz*.006,.006)*.10;
                float patchy=smoothstep(.12+exposure,.75+exposure,snowCover);
                float exposed=smoothstep(.035,.20,i.depth)*_EditablePatch;
                half3 soil=lerp(half3(.23,.145,.075),half3(.36,.26,.15),bedding);
                if(_GenesisReady>.5&&exposed>.01)
                {
                    GenesisSurface earth=GenesisGround(i.relativeWS,terrainPixelMeters,moisture,temperature,6);
                    soil=earth.albedo;genesis=GenesisBlend(genesis,earth,exposed*(1-rockWeight));
                }
                soil*=lerp(.85,1.05,saturate(material.g));
                half3 strata=lerp(soil,rock,smoothstep(material.r*.7,material.r+1.3,i.depth));
                base=lerp(base,strata,exposed);patchy*=1-exposed;
                base=lerp(base,half3(.78,.85,.89),patchy*(1-smoothstep(.12,.55,slope)));
                if(_GenesisReady>.5&&patchy>.01)
                {
                    GenesisSurface snow=GenesisGround(i.relativeWS,terrainPixelMeters,moisture,temperature,12);
                    float ice=permanent*genesisHabitat.a*(1-smoothstep(-8,-2,temperature))*(1-smoothstep(.02,.12,slope));
                    if(ice>.01)snow=GenesisBlend(snow,GenesisGround(i.relativeWS,terrainPixelMeters,moisture,temperature,13),ice);
                    float accumulation=patchy*(1-smoothstep(.12,.55,slope));
                    base=lerp(base,snow.albedo,accumulation);genesis=GenesisBlend(genesis,snow,accumulation);
                }
                float submerged=max(1-smoothstep(-.3,.2,h),riverBank.z*(1-smoothstep(-.3,.2,bankHeight))*(1-smoothstep(0,2,riverBank.x)));
                base=lerp(base,lerp(sand,rock,smoothstep(.12,.45,slope))*(.9+patch*.2),submerged);
                float3 local=i.relativeWS;float localWarp=StableNoise(local.xz*.011,0)*3;
                float stableWeather=StableNoise(float2(local.z*.15+localWarp,local.y*.025),1)*weights.x+StableNoise(local.xz*.13,2)*weights.y+StableNoise(float2(local.x*.15+localWarp,local.y*.025),3)*weights.z;
                float stableDetail=StableNoise(local.zy*.65,4)*weights.x+StableNoise(local.xz*.65,5)*weights.y+StableNoise(local.xy*.65,6)*weights.z;
                float stableGrain=StableNoise(local.zy*3.1,7)*weights.x+StableNoise(local.xz*3.1,8)*weights.y+StableNoise(local.xy*3.1,9)*weights.z;
                // Filter normal detail too. Unfiltered subpixel gradients
                // survived the albedo filter and produced distant speckling.
                stableWeather=lerp(stableWeather,.5,smoothstep(.25,.9,terrainPixelMeters*.16));
                stableDetail=lerp(stableDetail,.5,smoothstep(.25,.9,terrainPixelMeters*.65));
                stableGrain=lerp(stableGrain,.5,smoothstep(.25,.9,terrainPixelMeters*3.1));
                float rough=stableWeather*.6+stableDetail*.25+stableGrain*.15;
                float3 dpdx=ddx(local),dpdy=ddy(local),r1=cross(dpdy,n),r2=cross(n,dpdx);
                float determinant=dot(dpdx,r1);
                float3 gradient=ddx(rough)*r1+ddy(rough)*r2;
                float3 bump=gradient*(sign(determinant)/max(abs(determinant),max(.00001,length(dpdx)*length(dpdy)*.1)));
                bump*=min(1,.22/max(.00001,length(bump)));
                n=normalize(n-bump*rockWeight*distanceFade*(1-patchy*.85));
                if(_GenesisReady>.5)n=GenesisPerturb(surfaceNormal,i.relativeWS,genesis.height);
                float4 weatherState=RegionalWeatherField(p);base*=1-weatherState.b*lerp(.10,.23,rockWeight);
                if(_AevoriaTerrainDiagnostic>.5)
                {
                    if(_AevoriaTerrainDiagnostic<1.5){float3 geometric=normalize(cross(ddy(p),ddx(p)));if(geometric.y<0)geometric=-geometric;return half4((.15+.8*saturate(dot(geometric,normalize(float3(.4,1,.3))))).xxx,1);}
                    if(_AevoriaTerrainDiagnostic<2.5)return half4(normalize(i.normalWS)*.5+.5,1);
                    if(_AevoriaTerrainDiagnostic<3.5)return half4(base,1);
                    if(_AevoriaTerrainDiagnostic<4.5)return half4(saturate(h/16000).xxx,1);
                    if(_AevoriaTerrainDiagnostic<5.5)return half4(rockWeight,desert,patchy,1);
                    if(_AevoriaTerrainDiagnostic<6.5)return half4(n*.5+.5,1);
                    // Matched-pose lighting isolation, independent of shadows,
                    // fog and the final blend. Keep normal-map and BRDF failures
                    // distinguishable from geographic colour/mesh defects.
                    Light diagnosticLight=GetMainLight();
                    if(_AevoriaTerrainDiagnostic<7.5)return half4((.15+.8*saturate(dot(n,diagnosticLight.direction))).xxx,1);
                    if(_AevoriaTerrainDiagnostic<8.5)return half4(SampleSH(n),1);
                    if(_AevoriaTerrainDiagnostic<9.5)
                    {
                        BRDFData diagnosticBRDF;half diagnosticAlpha=1;
                        InitializeBRDFData(base,0,half3(.04,.04,.04),1-genesis.roughness,diagnosticAlpha,diagnosticBRDF);
                        return half4((DirectBRDFSpecular(diagnosticBRDF,n,diagnosticLight.direction,normalize(toView))*.04).xxx,1);
                    }
                    if(_AevoriaTerrainDiagnostic<10.5)return half4(genesis.roughness.xxx,1);
                    if(_AevoriaTerrainDiagnostic<11.5)return half4(saturate(terrainPixelMeters).xxx,1);
                    if(_AevoriaTerrainDiagnostic<12.5)return half4((.15+.8*saturate(dot(normalize(i.normalWS),diagnosticLight.direction))).xxx,1);
                    if(_AevoriaTerrainDiagnostic<13.5)return half4(saturate(abs(n-normalize(i.normalWS))*8),1);
                    if(_AevoriaTerrainDiagnostic<14.5)return half4((.15+.8*saturate(dot(surfaceNormal,diagnosticLight.direction))).xxx,1);
                    if(_AevoriaTerrainDiagnostic<15.5)return half4(saturate(i.depth),_EditablePatch,0,1);
                    float2 diagnosticGrid=(p.xz-_AevoriaRegionalRect.xy)/_AevoriaRegionalGrid.xy;
                    float diagnosticEdge=min(min(diagnosticGrid.x,diagnosticGrid.y),min(_AevoriaRegionalGrid.z-diagnosticGrid.x,_AevoriaRegionalGrid.w-diagnosticGrid.y));
                    if(_AevoriaTerrainDiagnostic<16.5)return half4(_AevoriaRegionalReady,smoothstep(2,6,diagnosticEdge),1-smoothstep(.04,.3,i.depth*_EditablePatch),1);
                    if(_AevoriaTerrainDiagnostic<17.5)return half4(surfaceNormal*.5+.5,1);
                    return half4(1,1,1,1);
                }
                base=SocietyAlbedo(base,p.xz);
                Light light=GetMainLight(TransformWorldToShadowCoord(p-_AevoriaOrigin));
                half diffuse=saturate(dot(n,light.direction));
                float enclosed=smoothstep(2,8,i.depth)*saturate(_EditablePatch);
                float cloud=RegionalCloudShadow(p,light.direction);half3 col=base*(SampleSH(n)*(1-weatherState.r*.12)*lerp(1,.15,enclosed)+light.color*diffuse*lerp(lerp(.55,1,light.shadowAttenuation),light.shadowAttenuation,enclosed)*cloud);
                InputData inputData=(InputData)0;inputData.positionWS=i.relativeWS;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                uint lightCount=GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light localLight=GetAdditionalLight(lightIndex,i.relativeWS,half4(1,1,1,1));
                    col+=base*localLight.color*saturate(dot(n,localLight.direction))*localLight.distanceAttenuation*localLight.shadowAttenuation;
                LIGHT_LOOP_END
                if(_GenesisReady>.5)
                {
                    inputData.normalWS=n;inputData.viewDirectionWS=normalize(toView);
                    inputData.shadowCoord=TransformWorldToShadowCoord(i.relativeWS);
                    inputData.bakedGI=SampleSH(n)*(1-weatherState.r*.12)*lerp(1,.15,enclosed);
                    inputData.shadowMask=half4(1,1,1,1);
                    SurfaceData surface=(SurfaceData)0;surface.albedo=base;surface.alpha=1;
                    surface.specular=half3(.04,.04,.04);surface.smoothness=1-genesis.roughness;
                    surface.normalTS=half3(0,0,1);surface.occlusion=genesis.occlusion;
                    half3 physicallyBased=UniversalFragmentPBR(inputData,surface).rgb;
                    col=physicallyBased*lerp(1,cloud,.8);
                }
                col+=weatherState.b*rockWeight*pow(saturate(dot(n,normalize(light.direction+normalize(GeographicCameraPosition()-p)))),64)*.12*cloud;
                col=lerp(col,lerp(_AevoriaHaze.rgb*.5,col,_AevoriaDistantScenery),smoothstep(6000,60000,distance(GeographicCameraPosition(),p)));
                float haze=WorldHeightHaze(GeographicCameraPosition(),p,_AevoriaSeaLevel,_AevoriaHaze.w);
                return half4(lerp(col,_AevoriaHaze.rgb,haze),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldOrigin.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _DetailScale,_Fade,_Outgoing,_Backdrop,_EditablePatch;float4 _TileRect;
            CBUFFER_END
            TEXTURE2D(_AevoriaCoverage);SAMPLER(sampler_AevoriaCoverage);float4 _AevoriaWorldRect;float3 _LightDirection;
            #include "WorldTerrainFields.hlsl"
            #include "WorldWeatherFields.hlsl"
            struct SA {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct SV {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
            SV ShadowVert(SA i)
            {SV o;float3 n=TransformObjectToWorldNormal(i.normalOS),physical=TransformObjectToWorld(i.positionOS.xyz);o.world=MorphWorldSurface(physical+_AevoriaOrigin,n,_Backdrop);physical.y=o.world.y;o.positionCS=TransformWorldToHClip(ApplyShadowBias(physical,n,_LightDirection));
                #if UNITY_REVERSED_Z
                o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #else
                o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;}
            half4 ShadowFrag(SV i):SV_Target
            {ClipExcavations(i.world,_EditablePatch);float noise=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(.06711056,.00583715))));
                if(_Backdrop>.5)ClipWorldCoverage(i.world,_Backdrop,noise);
                else if(_Outgoing>.5)clip(noise-_Fade);else clip(_Fade-noise);return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma target 4.5
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldOrigin.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _DetailScale,_Fade,_Outgoing,_Backdrop,_EditablePatch;float4 _TileRect;
            CBUFFER_END
            TEXTURE2D(_AevoriaCoverage); SAMPLER(sampler_AevoriaCoverage); float4 _AevoriaWorldRect;
            #include "WorldTerrainFields.hlsl"
            #include "WorldWeatherFields.hlsl"
            struct DA {float4 positionOS:POSITION;};
            struct DV {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
            DV DepthVert(DA i) {DV o;float3 n=float3(0,1,0),physical=TransformObjectToWorld(i.positionOS.xyz);o.world=MorphWorldSurface(physical+_AevoriaOrigin,n,_Backdrop);physical.y=o.world.y;o.positionCS=TransformWorldToHClip(physical);return o;}
            half DepthFrag(DV i):SV_Target
            {
                ClipExcavations(i.world,_EditablePatch);
                float noise=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(0.06711056,0.00583715))));
                if(_Backdrop>0.5)ClipWorldCoverage(i.world,_Backdrop,noise);
                else {if(_Outgoing>0.5) clip(noise-_Fade);else clip(_Fade-noise);}
                return i.positionCS.z;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags {"LightMode"="DepthNormals"}
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma target 4.5
            #pragma fragment DepthFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldOrigin.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _DetailScale,_Fade,_Outgoing,_Backdrop,_EditablePatch;float4 _TileRect;
            CBUFFER_END
            TEXTURE2D(_AevoriaCoverage); SAMPLER(sampler_AevoriaCoverage); float4 _AevoriaWorldRect;
            float _GenesisReady;
            #include "WorldTerrainFields.hlsl"
            #include "WorldWeatherFields.hlsl"
            struct DA {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 depth:TEXCOORD2;};
            struct DV {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normalWS:TEXCOORD1;float depth:TEXCOORD2;};
            DV DepthVert(DA i) {DV o;o.depth=i.depth.x*_EditablePatch;float3 n=TransformObjectToWorldNormal(i.normalOS),physical=TransformObjectToWorld(i.positionOS.xyz);o.world=MorphWorldSurface(physical+_AevoriaOrigin,n,_Backdrop);o.normalWS=n;physical.y=o.world.y;o.positionCS=TransformWorldToHClip(physical);return o;}
            half4 DepthFrag(DV i):SV_Target
            {
                ClipExcavations(i.world,_EditablePatch);
                float noise=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(0.06711056,0.00583715))));
                if(_Backdrop>0.5)ClipWorldCoverage(i.world,_Backdrop,noise);
                else {if(_Outgoing>0.5) clip(noise-_Fade);else clip(_Fade-noise);}
                float3 normal=normalize(i.normalWS);
                if(_GenesisReady>.5)normal=WorldShadingNormal(i.world,normal,i.depth);
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct=saturate(PackNormalOctQuadEncode(normal)*.5+.5);
                return half4(PackFloat2To888(oct),0);
                #else
                return half4(normal,0);
                #endif
            }
            ENDHLSL
        }
    }
}
