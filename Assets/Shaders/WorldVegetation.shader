Shader "Aevoria/Vegetation"
{
    Properties { _NearCover("Near understory",Float)=0 _VegetationFade("LOD fade",Float)=0 _VegetationFar("Far mesh",Float)=0 _VegetationVisibility("Visibility",Float)=1 _Foliage("Foliage lighting",Float)=1 _EcologyAffects("Ecological foliage",Float)=1 _TreeCoverage("Tree coverage",Float)=1 _VegetationDensity("Render density",Float)=1 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        HLSLINCLUDE
        float TreeSiteHash(float2 world){uint2 cell=(uint2)(int2)floor(world/16);uint h=cell.x*374761393u+cell.y*668265263u+303u*2246822519u;h=(h^(h>>13))*1274126177u;return (h^(h>>16))*(1.0/4294967296.0);}
        ENDHLSL
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma target 4.5
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldOrigin.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WorldWeatherFields.hlsl"
            #include "SettlementFields.hlsl"
            float4 _AevoriaHaze;
            TEXTURE2D(_AevoriaEcology); SAMPLER(sampler_AevoriaEcology);
            float4 _AevoriaEcologyRect; float _AevoriaEcologyReady;
            TEXTURE2D(_AevoriaSeasonal);SAMPLER(sampler_AevoriaSeasonal);float _AevoriaSeasonalReady;
            float HashLeaf(float2 p) {return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float LeafNoise(float2 p) {float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(HashLeaf(a),HashLeaf(a+float2(1,0)),f.x),lerp(HashLeaf(a+float2(0,1)),HashLeaf(a+1),f.x),f.y);}
            CBUFFER_START(UnityPerMaterial)
            float _NearCover,_VegetationFade,_VegetationFar,_VegetationVisibility,_Foliage,_EcologyAffects,_TreeCoverage,_VegetationDensity;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 world:TEXCOORD1;float4 color:COLOR;float fog:TEXCOORD2;float3 origin:TEXCOORD3;};
            V Vert(A i)
            {
                UNITY_SETUP_INSTANCE_ID(i); V o;
                float3 physical=TransformObjectToWorld(i.positionOS.xyz);
                physical.x+=sin(_Time.y*1.2+(physical.z+_AevoriaOrigin.z)*0.1)*saturate(i.positionOS.y/12)*0.09*_Foliage;
                o.world=physical+_AevoriaOrigin;
                o.origin=GeographicObjectPosition(float3(0,0,0));o.positionCS=TransformWorldToHClip(physical);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.color=i.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 Frag(V i):SV_Target
            {
                float noise=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(0.06711056,0.00583715))));
                clip(_VegetationVisibility-noise);clip(lerp(1,saturate((68-distance(i.origin.xz,GeographicCameraPosition().xz))/14),_NearCover)-noise);
                ClipSettledVegetation(i.origin.xz,_Foliage);
                float4 ecology=SAMPLE_TEXTURE2D(_AevoriaEcology,sampler_AevoriaEcology,saturate((i.origin.xz-_AevoriaEcologyRect.xy)/max(_AevoriaEcologyRect.zw,1)));
                if(_EcologyAffects*_AevoriaEcologyReady>0.5 && _Foliage>0.5)
                {float coverage=lerp(ecology.g*2,ecology.r,_TreeCoverage);clip(saturate(coverage*(1-ecology.b)*_VegetationDensity)-lerp(HashLeaf(floor(i.origin.xz)),TreeSiteHash(i.origin.xz),_TreeCoverage));}
                if(_VegetationFar>0.5) clip(_VegetationFade-noise);else clip(noise-_VegetationFade);
                Light light=GetMainLight(TransformWorldToShadowCoord(i.world-_AevoriaOrigin));
                half3 n=normalize(i.normalWS);float diffuse=saturate(dot(n,light.direction)*0.65+0.35);
                float leaf=LeafNoise(i.world.xz*2.4+i.world.y*0.8)-0.5;
                float footprint=max(length(ddx(i.world)),length(ddy(i.world)));
                float detail=(1-smoothstep(80,350,distance(GeographicCameraPosition(),i.world)))*(1-smoothstep(0.06,0.3,footprint))*_Foliage;
                half3 color=i.color.rgb*(1+leaf*0.12*detail)*(SampleSH(n)+light.color*diffuse*lerp(0.65,1,light.shadowAttenuation));
                color+=i.color.rgb*light.color*saturate(dot(-n,light.direction))*0.13*_Foliage;
                color*=RegionalCloudShadow(i.world,light.direction);
                color=lerp(color,color*half3(0.42,0.30,0.18),ecology.b*_EcologyAffects*_AevoriaEcologyReady);
                float4 seasonal=SAMPLE_TEXTURE2D(_AevoriaSeasonal,sampler_AevoriaSeasonal,saturate((i.world.xz-_AevoriaEcologyRect.xy)/max(_AevoriaEcologyRect.zw,1)));
                float snow=saturate(seasonal.r*.8)*_AevoriaSeasonalReady*_EcologyAffects;
                float crown=smoothstep(.12,.78,n.y)*smoothstep(.1,.8,LeafNoise(i.world.xz*.9+i.world.y));
                color=lerp(color,half3(.71,.79,.83)*(SampleSH(n)+light.color*.6),snow*crown*.86);
                float haze=WorldHeightHaze(GeographicCameraPosition(),i.world,_AevoriaClimateSea,_AevoriaHaze.w);
                return half4(lerp(color,_AevoriaHaze.rgb,haze),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldOrigin.hlsl"
            #include "SettlementFields.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _NearCover,_VegetationFade,_VegetationFar,_VegetationVisibility,_Foliage,_EcologyAffects,_TreeCoverage,_VegetationDensity;
            CBUFFER_END
            TEXTURE2D(_AevoriaEcology);SAMPLER(sampler_AevoriaEcology);float4 _AevoriaEcologyRect;float _AevoriaEcologyReady;
            struct DA{float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct DV{float4 positionCS:SV_POSITION;float3 origin:TEXCOORD0;};
            DV DepthVert(DA i)
            {UNITY_SETUP_INSTANCE_ID(i);DV o;float3 p=TransformObjectToWorld(i.positionOS.xyz);p.x+=sin(_Time.y*1.2+(p.z+_AevoriaOrigin.z)*.1)*saturate(i.positionOS.y/12)*.09*_Foliage;o.positionCS=TransformWorldToHClip(p);o.origin=GeographicObjectPosition(float3(0,0,0));return o;}
            half4 DepthFrag(DV i):SV_Target
            {
                float noise=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(.06711056,.00583715))));clip(_VegetationVisibility-noise);clip(lerp(1,saturate((68-distance(i.origin.xz,GeographicCameraPosition().xz))/14),_NearCover)-noise);ClipSettledVegetation(i.origin.xz,_Foliage);
                if(_VegetationFar>.5)clip(_VegetationFade-noise);else clip(noise-_VegetationFade);
                if(_EcologyAffects*_AevoriaEcologyReady>.5&&_Foliage>.5)
                {float4 e=SAMPLE_TEXTURE2D(_AevoriaEcology,sampler_AevoriaEcology,saturate((i.origin.xz-_AevoriaEcologyRect.xy)/max(_AevoriaEcologyRect.zw,1)));float hash=lerp(frac(sin(dot(floor(i.origin.xz),float2(127.1,311.7)))*43758.5453),TreeSiteHash(i.origin.xz),_TreeCoverage);clip(saturate(lerp(e.g*2,e.r,_TreeCoverage)*(1-e.b)*_VegetationDensity)-hash);}
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ColorMask 0 Cull Off
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma target 4.5
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldOrigin.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "SettlementFields.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _NearCover,_VegetationFade,_VegetationFar,_VegetationVisibility,_Foliage,_EcologyAffects,_TreeCoverage,_VegetationDensity;
            CBUFFER_END
            TEXTURE2D(_AevoriaEcology); SAMPLER(sampler_AevoriaEcology);
            float4 _AevoriaEcologyRect;float _AevoriaEcologyReady;float3 _LightDirection;
            struct SA {float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct SV {float4 positionCS:SV_POSITION;float3 origin:TEXCOORD0;};
            SV ShadowVert(SA i)
            {
                UNITY_SETUP_INSTANCE_ID(i);SV o;float3 p=TransformObjectToWorld(i.positionOS.xyz);
                p.x+=sin(_Time.y*1.2+(p.z+_AevoriaOrigin.z)*.1)*saturate(i.positionOS.y/12)*.09*_Foliage;
                o.positionCS=TransformWorldToHClip(ApplyShadowBias(p,TransformObjectToWorldNormal(i.normalOS),_LightDirection));
                #if UNITY_REVERSED_Z
                o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #else
                o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                o.origin=GeographicObjectPosition(float3(0,0,0));return o;
            }
            half4 ShadowFrag(SV i):SV_Target
            {
                float noise=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(.06711056,.00583715))));clip(_VegetationVisibility-noise);clip(lerp(1,saturate((68-distance(i.origin.xz,GeographicCameraPosition().xz))/14),_NearCover)-noise);
                ClipSettledVegetation(i.origin.xz,_Foliage);
                if(_VegetationFar>.5)clip(_VegetationFade-noise);else clip(noise-_VegetationFade);
                if(_EcologyAffects*_AevoriaEcologyReady>.5&&_Foliage>.5)
                {float4 e=SAMPLE_TEXTURE2D(_AevoriaEcology,sampler_AevoriaEcology,saturate((i.origin.xz-_AevoriaEcologyRect.xy)/max(_AevoriaEcologyRect.zw,1)));float hash=lerp(frac(sin(dot(floor(i.origin.xz),float2(127.1,311.7)))*43758.5453),TreeSiteHash(i.origin.xz),_TreeCoverage);clip(saturate(lerp(e.g*2,e.r,_TreeCoverage)*(1-e.b)*_VegetationDensity)-hash);}
                return 0;
            }
            ENDHLSL
        }
    }
}
