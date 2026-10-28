Shader "Aevoria/Clouds"
{
    Properties{_CloudSystemIndex("Weather system",Float)=0}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+50" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Front ZTest LEqual
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldOrigin.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "WorldWeatherFields.hlsl"
            float4 _AevoriaHaze;
            float _AevoriaGodView;
            CBUFFER_START(UnityPerMaterial)
            float _CloudSystemIndex;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;};struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
            V Vert(A i){V o;o.world=GeographicObjectPosition(i.positionOS.xyz);o.positionCS=GeographicToClip(o.world);return o;}
            half4 Frag(V i):SV_Target
            {
                clip(_AevoriaRenderClouds-.5);clip(_AevoriaCloudQuality-.1);int index=(int)_CloudSystemIndex;
                float4 c=_AevoriaCloudSystems[index],s=_AevoriaCloudShapes[index];clip(s.y-.001);
                float3 origin=GeographicCameraPosition(),ray=normalize(i.world-origin),extent=float3(c.w,s.x,c.w)*1.65;
                float3 inverse=rcp(ray+float3(.000001,.000001,.000001)),a=(c.xyz-extent-origin)*inverse,b=(c.xyz+extent-origin)*inverse;
                float3 near=min(a,b),far=max(a,b);float entry=max(0,max(near.x,max(near.y,near.z))),exit=min(far.x,min(far.y,far.z));
                float depth=SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS)),eye=LinearEyeDepth(depth,_ZBufferParams);
                exit=min(exit,eye/max(.001,dot(ray,-UNITY_MATRIX_V[2].xyz)));clip(exit-entry);
                int steps=_AevoriaCloudQuality>1.5?40:24;float stride=(exit-entry)/steps,transmission=1;float3 color=0;Light sun=GetMainLight();
                [loop]for(int j=0;j<steps;j++)
                {
                    float3 p=origin+ray*(entry+(j+.5)*stride);float density=CumulusDensity(p,index),opacity=1-exp(-density*stride/max(100,c.w*.065));
                    float upper=saturate((p.y-c.y)/max(s.x,1)*.5+.5);
                    float light=exp(-CumulusDensity(p+sun.direction*min(c.w,s.x)*.25,index)*.9-CumulusDensity(p+sun.direction*min(c.w,s.x)*.65,index)*.4);
                    float3 cloud=lerp(float3(.28,.35,.44),float3(1,.99,.95),saturate(.15+upper*.45+light*.4))*(1-s.z*.30);
                    color+=transmission*opacity*cloud;transmission*=1-opacity;if(transmission<.015)break;
                }
                float alpha=1-transmission;clip(alpha-.002);color/=max(.001,alpha);
                // God-mode inspection remains readable while the camera passes
                // inside a cloud; entering a volume must not resemble a frozen
                // gray-screen camera. Formation positions remain unchanged.
                alpha=min(alpha,lerp(1,.35,smoothstep(.01,.3,CumulusDensity(origin,index))*_AevoriaGodView));
                return half4(lerp(color,_AevoriaHaze.rgb,WorldHeightHaze(origin,origin+ray*((entry+exit)*.5),_AevoriaClimateSea,_AevoriaHaze.w)),alpha);
            }
            ENDHLSL
        }
    }
}
