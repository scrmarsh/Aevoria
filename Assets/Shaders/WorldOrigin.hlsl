#ifndef AEVORIA_ORIGIN
#define AEVORIA_ORIGIN
float3 _AevoriaOrigin;
float3 GeographicObjectPosition(float3 p){return TransformObjectToWorld(p)+_AevoriaOrigin;}
float4 GeographicToClip(float3 p){return TransformWorldToHClip(p-_AevoriaOrigin);}
float3 GeographicCameraPosition(){return _WorldSpaceCameraPos+_AevoriaOrigin;}
#endif
