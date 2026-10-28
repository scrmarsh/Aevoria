Shader "Aevoria/Character Surface"
{
    Properties
    {
        _BaseMap("Base",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _HeadMap("Authored head albedo",2D)="white"{}
        _SurfaceClass("Equipment",Float)=0
        _HideHead("First person head clipping",Float)=0
        [HideInInspector]_Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_HeadMap);SAMPLER(sampler_HeadMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor,_BaseMap_ST;float _SurfaceClass,_HideHead;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;float2 uv:TEXCOORD0;float2 region:TEXCOORD1;float2 faceUV:TEXCOORD2;};
            struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;half4 color:COLOR;float2 uv:TEXCOORD2;float3 local:TEXCOORD3;float head:TEXCOORD4;float reference:TEXCOORD5;float2 faceUV:TEXCOORD6;};
            V Vert(A i){V o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.color=i.color;o.uv=i.uv;o.local=i.positionOS.xyz;o.head=i.region.x;o.reference=i.region.y;o.faceUV=i.faceUV;return o;}
            half4 Frag(V i):SV_Target
            {
                clip(.5-_HideHead*i.head);
                float metallic=saturate(i.color.a),footprint=max(length(ddx(i.local)),length(ddy(i.local)));
                float grain=sin(i.uv.x*310+sin(i.uv.y*35)*2)*sin(i.uv.y*870),detail=1-smoothstep(.0008,.009,footprint);
                float wood=_SurfaceClass*(1-smoothstep(.1,.45,metallic))*smoothstep(.025,.13,i.color.r-i.color.b);
                float cloth=(1-_SurfaceClass)*(1-smoothstep(.1,.4,metallic))*smoothstep(1.1,1.4,i.color.b/max(.01,i.color.r));
                float flesh=(1-_SurfaceClass)*(1-metallic)*smoothstep(.06,.18,i.color.r-i.color.b);
                float woven=(sin(i.uv.x*1800)*sin(i.uv.y*1800))*(1-smoothstep(.0002,.003,footprint))*cloth;
                half3 referenceAlbedo=SAMPLE_TEXTURE2D(_HeadMap,sampler_HeadMap,i.faceUV).rgb;
                // Reduce illumination already present in the source photograph
                // so the game's dynamic shadows remain the dominant lighting.
                float referenceLight=max(.02,dot(referenceAlbedo,half3(.2126,.7152,.0722)));
                referenceAlbedo*=lerp(1,pow(referenceLight,.72)/referenceLight,.85);
                referenceAlbedo=lerp(referenceAlbedo,i.color.rgb,.18);
                half3 base=lerp(i.color.rgb,referenceAlbedo,saturate(i.reference)*(1-_SurfaceClass));
                half3 albedo=base*_BaseColor.rgb*(1+grain*lerp(.025,.006,flesh)*detail+wood*grain*.12*detail+woven*.025);
                InputData input=(InputData)0;input.positionWS=i.positionWS;input.normalWS=normalize(i.normalWS);input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);input.shadowCoord=TransformWorldToShadowCoord(i.positionWS);input.bakedGI=SampleSH(input.normalWS);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);input.shadowMask=1;
                SurfaceData surface=(SurfaceData)0;surface.albedo=albedo;surface.metallic=metallic;surface.smoothness=lerp(lerp(.22,.32,flesh),.62,metallic)-grain*.025*detail;surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.alpha=1;return UniversalFragmentPBR(input,surface);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor,_BaseMap_ST;float _SurfaceClass,_HideHead;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float2 region:TEXCOORD1;};
            struct V{float4 positionCS:SV_POSITION;float head:TEXCOORD0;};
            V DepthVert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.head=i.region.x;return o;}
            half DepthFrag(V i):SV_Target{clip(.5-_HideHead*i.head);return i.positionCS.z;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags {"LightMode"="DepthNormals"}
            ZWrite On
            HLSLPROGRAM
            #pragma vertex NormalVert
            #pragma fragment NormalFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor,_BaseMap_ST;float _SurfaceClass,_HideHead;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 region:TEXCOORD1;};
            struct V{float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float head:TEXCOORD1;};
            V NormalVert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.head=i.region.x;return o;}
            half4 NormalFrag(V i):SV_Target
            {
                clip(.5-_HideHead*i.head);float3 n=normalize(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct=PackNormalOctQuadEncode(n);return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
                #else
                return half4(n,0);
                #endif
            }
            ENDHLSL
        }
    }
}
