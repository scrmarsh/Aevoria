Shader "Aevoria/Roman Soldier"
{
    Properties { _Skin("Skin",Color)=(.64,.43,.29,1) }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 color:COLOR;};
            V Vert(A i){V o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.color=i.color;return o;}
            half4 Frag(V i):SV_Target
            {
                InputData data=(InputData)0;data.positionWS=i.positionWS;data.normalWS=normalize(i.normalWS);data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);data.bakedGI=SampleSH(data.normalWS);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=half4(1,1,1,1);
                SurfaceData surface=(SurfaceData)0;surface.albedo=i.color.rgb;surface.metallic=i.color.a;surface.smoothness=lerp(.25,.48,i.color.a);surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.alpha=1;return UniversalFragmentPBR(data,surface);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
