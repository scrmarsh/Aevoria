Shader "Hidden/Aevoria/GenesisNoiseProbe"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline"}
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Shaders/GenesisMaterials.hlsl"
            float4 _ProbeLocal;float _ProbeFrequency;int _ProbeOctave,_ProbeProjection,_ProbeMode,_ProbeFamily;
            struct A{float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
            V Vert(A input){V o;o.position=TransformObjectToHClip(input.vertex.xyz);o.uv=input.uv;return o;}
            float4 Frag(V input):SV_Target
            {
                float3 p=_ProbeLocal.xyz+float3(input.uv.x*17,3,input.uv.y*13);
                if(_ProbeMode==1)
                {
                    GenesisSurface surface;
                    if(_ProbeFamily<6)surface=GenesisRockFamily(p,float3(0,1,0),.02,float4(.8,.6,.7,.2),.5,12,_ProbeFamily);
                    else surface=GenesisGround(p,.02,.5,12,_ProbeFamily);
                    return float4(surface.albedo,surface.height);
                }
                float value=GenesisNoise(p,0,1,_ProbeFrequency,_ProbeOctave,0x1234abcdu,_ProbeProjection);
                return float4(value,value,value,1);
            }
            ENDHLSL
        }
    }
}
