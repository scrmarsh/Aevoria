Shader "Aevoria/Water"
{
    Properties
    {
        _InlandWater("Inland water",Float)=0
        [HideInInspector] _WaterStencilComp("Water stencil comparison",Float)=8
        [HideInInspector] _WaterStencilPass("Water stencil operation",Float)=0
        [HideInInspector] _WaterStencilWriteMask("Water stencil write mask",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            // Bit3 belongs to URP's user stencil range. River ribbons use it
            // to composite their union once, including overlaps at junctions.
            Stencil { Ref 8 ReadMask 8 WriteMask [_WaterStencilWriteMask] Comp [_WaterStencilComp] Pass [_WaterStencilPass] }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma target 4.5
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldOrigin.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "WorldWeatherFields.hlsl"
            #include "WorldCoastFields.hlsl"
            #include "WorldRiverFields.hlsl"
            TEXTURE2D(_AevoriaHeight); SAMPLER(sampler_AevoriaHeight);
            TEXTURE2D(_AevoriaOverviewHeight);float4 _AevoriaOverviewGrid;float _AevoriaOverviewReady,_AevoriaWaterQuality,_AevoriaWaterTransparency;
            TEXTURE2D(_AevoriaRegionalHeight);float4 _AevoriaRegionalRect,_AevoriaRegionalGrid;float _AevoriaRegionalReady;
            TEXTURE2D(_AevoriaClimate);SAMPLER(sampler_AevoriaClimate);
            TEXTURE2D(_AevoriaSeasonal); SAMPLER(sampler_AevoriaSeasonal);
            float4 _AevoriaWorldRect,_AevoriaHaze,_AevoriaEcologyRect;float _AevoriaSeaLevel,_AevoriaSeasonalReady;
            float _AevoriaWaterDiagnostic;
            CBUFFER_START(UnityPerMaterial)
            float _InlandWater;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;float4 water:TEXCOORD0;float2 flow:TEXCOORD1;};
            struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float4 water:TEXCOORD1;float2 flow:TEXCOORD2;float level:TEXCOORD3;};
            V Vert(A i)
            {
                V o;o.world=GeographicObjectPosition(i.positionOS.xyz);o.water=i.water;o.flow=i.flow;o.level=o.world.y-.035*_InlandWater;
                if(_InlandWater>.5&&i.water.w>.5)o.world.y-=.035*(1-smoothstep(.2,1,o.level-_AevoriaSeaLevel));
                o.positionCS=GeographicToClip(o.world);return o;
            }
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p){float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(c),Hash(c+float2(1,0)),f.x),lerp(Hash(c+float2(0,1)),Hash(c+1),f.x),f.y);}
            float OceanBottom(float2 world)
            {
                // Read the physical rendered bottom. The old coast-distance
                // shortcut could neither represent variable bathymetry nor
                // resolve carved fjord/cove mouths inside the original land mask.
                float2 g=(world-_AevoriaOverviewGrid.xy)/max(_AevoriaOverviewGrid.zw,1);int2 c=clamp((int2)floor(g),0,767);float2 f=saturate(g-c);float4 w=f.x+f.y<=1?float4(1-f.x-f.y,f.x,f.y,0):float4(0,1-f.y,1-f.x,f.x+f.y-1);
                float h=LOAD_TEXTURE2D(_AevoriaOverviewHeight,c).r*w.x+LOAD_TEXTURE2D(_AevoriaOverviewHeight,c+int2(1,0)).r*w.y+LOAD_TEXTURE2D(_AevoriaOverviewHeight,c+int2(0,1)).r*w.z+LOAD_TEXTURE2D(_AevoriaOverviewHeight,c+1).r*w.w;
                if(_AevoriaRegionalReady>.5&&all(world>=_AevoriaRegionalRect.xy)&&all(world<=_AevoriaRegionalRect.xy+_AevoriaRegionalRect.zw))
                {
                    float2 rg=(world-_AevoriaRegionalRect.xy)/_AevoriaRegionalGrid.xy;int2 rc=clamp((int2)floor(rg),0,(int)_AevoriaRegionalGrid.z-1);float2 rf=saturate(rg-rc);
                    float4 rw=rf.x+rf.y<=1?float4(1-rf.x-rf.y,rf.x,rf.y,0):float4(0,1-rf.y,1-rf.x,rf.x+rf.y-1);
                    float rh=LOAD_TEXTURE2D(_AevoriaRegionalHeight,rc).r*rw.x+LOAD_TEXTURE2D(_AevoriaRegionalHeight,rc+int2(1,0)).r*rw.y+LOAD_TEXTURE2D(_AevoriaRegionalHeight,rc+int2(0,1)).r*rw.z+LOAD_TEXTURE2D(_AevoriaRegionalHeight,rc+1).r*rw.w;
                    float2 edge=min(world-_AevoriaRegionalRect.xy,_AevoriaRegionalRect.xy+_AevoriaRegionalRect.zw-world);h=lerp(h,rh,smoothstep(0,_AevoriaRegionalGrid.x*8,min(edge.x,edge.y)));
                }
                float2 outside=max(max(-g,g-768),0);return h-length(outside*_AevoriaOverviewGrid.zw)*.025;
            }            half4 Frag(V i):SV_Target
            {
                float2 uv=(i.world.xz-_AevoriaWorldRect.xy)/_AevoriaWorldRect.zw;
                float dataHeight=OceanBottom(i.world.xz);bool riverMouth=false;float estuary=0,oceanHandoff=0,riverColumn=-1;
                if(_InlandWater<.5)
                {
                    float3 channel;float3 bank=WorldRiverBank(i.world.xz,_AevoriaWorldRect,channel);
                    if(bank.z>.5&&bank.x<-.05&&channel.z>.5)oceanHandoff=smoothstep(.005,.2,bank.y-_AevoriaSeaLevel);
                    if(oceanHandoff>=1)clip(-1);
                    riverMouth=bank.z>.5&&bank.x<=0&&bank.y<=_AevoriaSeaLevel+.5;
                    estuary=bank.z*(1-smoothstep(0,32,max(0,bank.x)))*(1-smoothstep(50,400,max(0,-WorldCoastDistance(i.world.xz,_AevoriaWorldRect))))*(1-smoothstep(0,.5,bank.y-_AevoriaSeaLevel));
                    if(riverMouth)
                    {
                        // The coarse overview can miss a narrow carved mouth.
                        // Inside the wetted half-width this is the exact nearest
                        // channel bed used by HydrologyMap.Carve.
                        float ratio=saturate((bank.x+channel.x)/max(.001,channel.x));
                        float bed=bank.y-channel.y+(channel.y-.12)*ratio*ratio;
                        dataHeight=min(dataHeight,bed);
                    }
                }
                else if(i.water.w>.5)
                {
                    // Interpolated vertex depth exposes the ribbon's triangles
                    // where bank curvature changes. Evaluate the same smooth
                    // channel cross-section used by physical river carving.
                    float3 channel;float3 bank=WorldRiverBank(i.world.xz,_AevoriaWorldRect,channel);
                    if(bank.z>.5&&bank.x<=0&&channel.y>0)
                    {
                        float ratio=saturate((bank.x+channel.x)/max(.001,channel.x));
                        float bed=bank.y-channel.y+(channel.y-.12)*ratio*ratio;
                        // A crossing ribbon's interpolated level belongs to
                        // that mesh owner. Use the canonical nearest channel
                        // level for optics so junction triangles cannot change
                        // the apparent depth; geometry still owns occlusion.
                        riverColumn=max(0,bank.y+.035-bed);
                    }
                }
                float inlandOptics=max(_InlandWater,estuary);
                float depth=_InlandWater>.5?i.water.x:max(0,i.world.y-dataHeight),canonicalDepth=depth;
                if(riverColumn>=0){depth=riverColumn;canonicalDepth=riverColumn;}
                float3 toCamera=normalize(GeographicCameraPosition()-i.world);float range=distance(GeographicCameraPosition(),i.world);
                float raw=SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS));
                #if UNITY_REVERSED_Z
                bool valid=raw>0&&raw<=1;
                #else
                bool valid=raw>=0&&raw<1;raw=lerp(UNITY_NEAR_CLIP_VALUE,1,raw);
                #endif
                // Inland optical depth is owned by the canonical bed field.
                // A coarse terrain triangle can sit just below the water and
                // must not make a deep river optically disappear at that LOD.
                // Hardware depth testing still handles opaque occlusion.
                if(valid&&_AevoriaWaterTransparency>.5&&_InlandWater<.5&&!riverMouth)
                {
                    float sceneEye=LinearEyeDepth(raw,_ZBufferParams),waterEye=-TransformWorldToView(i.world-_AevoriaOrigin).z;
                    float difference=max(0,sceneEye-waterEye);
                    // Recover the submerged ray's vertical displacement directly.
                    // Inverting a near/far projection and subtracting world Y
                    // loses shallow depth precision and reveals LOD-shaped seams.
                    float verticalPerEye=abs(GeographicCameraPosition().y-i.world.y)/max(.001,waterEye);
                    depth=difference*(_AevoriaWaterTransparency>1.5?verticalPerEye:max(.02,abs(toCamera.y)));
                }
                if(_AevoriaWaterDiagnostic>.5&&_AevoriaWaterDiagnostic<2.5){if(_AevoriaWaterDiagnostic<1.5)return half4(.02,.45,.85,1);return half4(saturate(depth/5),saturate(i.water.x/5),valid?1:0,1);}
                // Resolve shallow transmission against visible geometry. Once
                // the vertical water column is optically deep, screen-space
                // silhouettes must not punch transparent holes into it.
                depth=lerp(depth,canonicalDepth,smoothstep(8,30,canonicalDepth));
                float4 season=SAMPLE_TEXTURE2D(_AevoriaSeasonal,sampler_AevoriaSeasonal,saturate((i.world.xz-_AevoriaEcologyRect.xy)/max(_AevoriaEcologyRect.zw,1)));
                float river=i.water.z*_InlandWater,energy=i.water.y;
                float4 climate=SAMPLE_TEXTURE2D(_AevoriaClimate,sampler_AevoriaClimate,(saturate(uv)*128+.5)/129);
                float waterMeanTemperature=WorldAltitudeTemperature(climate.r,climate.a,i.world.y,_AevoriaSeaLevel);
                float permanentIce=1-smoothstep(-6,-1,waterMeanTemperature+climate.b);
                float ice=smoothstep(.08,.35,max(season.g*_AevoriaSeasonalReady,permanentIce)*lerp(1,.12+(.45-energy)*.5,river))*_InlandWater;
                float4 localWeather=RegionalWeatherField(i.world);float detailFade=(1-smoothstep(10000,80000,range))*(1-smoothstep(20,150,max(length(ddx(i.world)),length(ddy(i.world)))))*lerp(.35,1,saturate(_AevoriaWaterQuality));
                // Multiplying geographic coordinates by interpolated flow
                // rotates a wave's entire origin at every ribbon vertex. It
                // produced dense diagonal bands at bends and mesh boundaries.
                // Keep both wave phases anchored in the same world lattice;
                // flow steers their amplitudes and temporal advection only.
                float warp=sin(i.world.x*.017+i.world.z*.031)*1.4;
                float2 riverPhase=float2(i.world.x*.34+warp,i.world.z*.31-warp);
                float2 riverFilter=1-smoothstep(.8,3,fwidth(riverPhase));
                float2 flowWeight=abs(i.flow)+.2;flowWeight/=flowWeight.x+flowWeight.y;
                // Blend fixed-frequency forward/backward waves. Multiplying
                // elapsed time by a varying flow vector also accumulated dense
                // phase bands at bends as the session became longer.
                float2 flowingWave=lerp(sin(riverPhase+_Time.y*.9),sin(riverPhase-_Time.y*.9),saturate(i.flow*.5+.5));
                float riverWave=dot(flowingWave*riverFilter,flowWeight);
                float pixelMetres=max(length(ddx(i.world)),length(ddy(i.world)));
                float oceanFilter=1-smoothstep(.8,3,pixelMetres*.12);
                float wave=lerp(sin(i.world.x*.095+sin(i.world.z*.02)*3+_Time.y*.65)*sin(i.world.z*.071-_Time.y*.4)*oceanFilter,riverWave,river)*detailFade*(1-ice*.98);
                float normalFilter=1-smoothstep(.8,3,pixelMetres*.08);
                float3 n=normalize(float3(sin(i.world.x*.075+_Time.y)*.014*detailFade*normalFilter*(1-ice),1,cos(i.world.z*.08-_Time.y)*.014*detailFade*normalFilter*(1-ice)));
                float warmClear=smoothstep(20,28,waterMeanTemperature)*(1-inlandOptics);
                half3 pale=lerp(half3(.09,.32,.35),half3(.08,.53,.48),warmClear),cyan=lerp(half3(.035,.24,.29),half3(.025,.38,.43),warmClear),blue=half3(.025,.15,.29),navy=half3(.016,.085,.20);
                float riverTint=max(river,estuary);
                pale=lerp(pale,lerp(half3(.075,.24,.28),half3(.085,.24,.22),riverTint),inlandOptics);
                cyan=lerp(cyan,lerp(half3(.02,.145,.21),half3(.055,.17,.155),riverTint),inlandOptics);
                // Water colour follows the physical column beneath this point.
                // Screen depth follows a different XZ point along the view ray;
                // using it for colour projected distant seabed silhouettes onto
                // the ocean as moving, sharp blue boundaries. Keep that optical
                // depth for transmission into the actually rendered bottom.
                half3 color=lerp(pale,cyan,1-exp(-canonicalDepth*.065));color=lerp(color,blue,(1-exp(-canonicalDepth*.018))*(1-inlandOptics*.25));color=lerp(color,navy,(1-exp(-canonicalDepth*.003))*(1-inlandOptics*.35));
                float suspendedSediment=riverTint*(.12+energy*.2+localWeather.g*.25);
                color=lerp(color,half3(.12,.17,.11),saturate(suspendedSediment*.3));
                color+=wave*.006;Light light=GetMainLight();float fresnel=pow(1-saturate(dot(n,toCamera)),4);
                color=lerp(color,half3(.25,.37,.47),fresnel*.22*(1-ice));color+=light.color*pow(saturate(dot(n,normalize(toCamera+light.direction))),100)*.08*(1-ice*.9);
                float surf=(1-smoothstep(.25,2,depth))*(.5+.5*sin(i.world.x*.12+i.world.z*.07-_Time.y*1.3));
                float rapid=river*energy*saturate(wave*.5+.2)*(1-smoothstep(3,7,depth));
                color=lerp(color,half3(.65,.77,.76),max(surf*(1-_InlandWater)*.35,rapid*.18)*(1-ice));
                float iceDetail=lerp(Noise(i.world.xz*.004),.5,smoothstep(25,120,max(length(ddx(i.world)),length(ddy(i.world)))));
                half3 frozen=lerp(half3(.58,.76,.84),half3(.78,.88,.93),iceDetail);
                float drift=smoothstep(.42,.72,Noise(i.world.xz*.0012+Noise(i.world.xz*.0003)*2));
                frozen=lerp(frozen,half3(.90,.94,.97),saturate(season.r*.5)*drift);
                frozen+=light.color*pow(saturate(dot(n,normalize(toCamera+light.direction))),60)*.06;
                color=lerp(color,frozen,ice);
                color*=RegionalCloudShadow(i.world,light.direction);color+=sin(i.world.x*.00013+i.world.z*.00017)*.002*(1-ice);
                float turbidity=suspendedSediment;float absorption=lerp(.10,.055,warmClear);absorption=lerp(absorption,.45,inlandOptics);
                // Transmission and the reflected surface contribute together.
                // Absorption alone made metre-deep inland water look dry from
                // above, where the Fresnel-only reflection tends toward zero.
                float reflection=lerp(.02+fresnel*.16,.24+fresnel*.12,inlandOptics);
                float alpha=lerp(1-(1-reflection)*exp(-depth*(absorption+turbidity*.10)),.97,ice);alpha*=smoothstep(0,max(.02,fwidth(depth)),depth);
                // Complementary coverage joins the elevated river to the ocean
                // without a hard cross-section or double absorption. Account
                // for the river's over-compositing when reducing ocean alpha.
                if(_InlandWater>.5&&i.water.w>.5)alpha*=smoothstep(.005,.2,i.level-_AevoriaSeaLevel);
                else if(_InlandWater<.5)alpha*=saturate((1-oceanHandoff)/max(.001,1-alpha*oceanHandoff));
                if(_AevoriaWaterDiagnostic>2.5&&_AevoriaWaterDiagnostic<3.5)return half4(alpha,1-exp(-i.water.x*.1),saturate(raw*10000),1);
                if(_AevoriaWaterDiagnostic>3.5&&_AevoriaWaterDiagnostic<4.5)return half4(saturate(depth/500),saturate(canonicalDepth/500),valid?1:0,1);
                float haze=WorldHeightHaze(GeographicCameraPosition(),i.world,_AevoriaSeaLevel,_AevoriaHaze.w);return half4(lerp(color,_AevoriaHaze.rgb,haze),_AevoriaWaterDiagnostic>4.5?1:alpha);
            }
            ENDHLSL
        }
    }
}
