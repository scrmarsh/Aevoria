Shader "Aevoria/Horizon"
{
    Properties{_Color("Distant atmosphere",Color)=(.3,.4,.5,1) _Opacity("Visibility",Float)=1}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-100"}
        Pass
        {
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldOrigin.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;float _Opacity;
            CBUFFER_END
            struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
            V Vert(float4 p:POSITION){V o;o.world=GeographicObjectPosition(p.xyz);o.positionCS=GeographicToClip(o.world);return o;}
            half4 Frag(V i):SV_Target{float noise=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(.06711056,.00583715))));clip(_Opacity-noise);float fade=saturate((i.world.y+2000)/16000);return half4(lerp(half3(.48,.62,.75),_Color.rgb,fade*.6+.15),1);}
            ENDHLSL
        }
    }
}
