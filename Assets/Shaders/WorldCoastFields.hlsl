#ifndef AEVORIA_COAST_FIELDS
#define AEVORIA_COAST_FIELDS
TEXTURE2D(_AevoriaCoastDistance);float _AevoriaCoastReady;
float CoastTangent(float a,float b){return a*b<=0?0:2*a*b/(a+b);}
float CoastCurve(float a,float b,float c,float d,float t)
{
    float delta=c-b,m0=CoastTangent(b-a,delta),m1=CoastTangent(delta,d-c),t2=t*t,t3=t2*t;
    return (2*t3-3*t2+1)*b+(t3-2*t2+t)*m0+(-2*t3+3*t2)*c+(t3-t2)*m1;
}
float CoastRow(int row,int column,float t)
{
    row=clamp(row,0,256);
    return CoastCurve(LOAD_TEXTURE2D(_AevoriaCoastDistance,int2(max(0,column-1),row)).r,LOAD_TEXTURE2D(_AevoriaCoastDistance,int2(column,row)).r,LOAD_TEXTURE2D(_AevoriaCoastDistance,int2(column+1,row)).r,LOAD_TEXTURE2D(_AevoriaCoastDistance,int2(min(256,column+2),row)).r,t);
}
float WorldCoastDistance(float2 world,float4 rectangle)
{
    float2 grid=saturate((world-rectangle.xy)/rectangle.zw)*256;int2 cell=min((int2)floor(grid),255);float2 t=grid-cell;
    return CoastCurve(CoastRow(cell.y-1,cell.x,t.x),CoastRow(cell.y,cell.x,t.x),CoastRow(cell.y+1,cell.x,t.x),CoastRow(cell.y+2,cell.x,t.x),t.y);
}
float ReferenceOceanFloor(float coast)
{
    float d=max(0,-coast);return -160*(1-exp(-d/4200))-4200*(1-exp(-max(0,d-12000)/28000));
}
#endif
