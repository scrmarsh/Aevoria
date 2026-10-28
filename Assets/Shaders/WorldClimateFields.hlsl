#ifndef AEVORIA_CLIMATE_FIELDS
#define AEVORIA_CLIMATE_FIELDS
// Keep this physical altitude profile in sync with ClimateMap.AltitudeCooling.
float WorldAltitudeCooling(float elevation)
{
    float h=max(0,elevation);return min(h,11000)*.0065-max(0,min(h,32000)-20000)*.001;
}
float WorldAltitudeTemperature(float sampledTemperature,float sampledHeight,float height,float sea)
{
    return sampledTemperature+WorldAltitudeCooling(sampledHeight-sea)-WorldAltitudeCooling(height-sea);
}
#endif
