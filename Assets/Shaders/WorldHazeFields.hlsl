#ifndef AEVORIA_HAZE_FIELDS
#define AEVORIA_HAZE_FIELDS
float WorldHeightHaze(float3 camera,float3 surfacePosition,float sea,float density)
{
    float a=max(0,camera.y-sea),b=max(0,surfacePosition.y-sea),delta=a-b;
    float average=abs(delta)<1?exp(-a/8000):8000*(exp(-b/8000)-exp(-a/8000))/delta;
    return 1-exp(-distance(camera,surfacePosition)*max(0,density)*max(0,average));
}
#endif
