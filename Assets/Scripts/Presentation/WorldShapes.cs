using System.Collections.Generic;
using UnityEngine;
namespace Aevoria.Presentation
{
    internal sealed class WorldShapes
    {
        private readonly List<Vector3> v=new List<Vector3>();private readonly List<Color> c=new List<Color>();private readonly List<int> t=new List<int>();
        public void Triangle(Vector3 a,Vector3 b,Vector3 d,Color color){int start=v.Count;v.Add(a);v.Add(b);v.Add(d);c.Add(color);c.Add(color);c.Add(color);t.Add(start);t.Add(start+1);t.Add(start+2);}
        public void Box(Vector3 center,Vector3 size,Color color)
        {int start=v.Count;for(int i=0;i<8;i++){v.Add(center+Vector3.Scale(new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f),size));c.Add(color);}int[] a={0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};foreach(int i in a)t.Add(start+i);}
        public void Rod(Vector3 a,Vector3 b,float radius,Color color,int sides=7)
        {int start=v.Count;var q=Quaternion.FromToRotation(Vector3.up,b-a);for(int ring=0;ring<2;ring++)for(int i=0;i<sides;i++){float angle=i*Mathf.PI*2/sides;v.Add((ring==0?a:b)+q*new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));c.Add(color*(.85f+(i%3)*.08f));}for(int i=0;i<sides;i++){int x=start+i,y=start+(i+1)%sides;t.Add(x);t.Add(x+sides);t.Add(y);t.Add(y);t.Add(x+sides);t.Add(y+sides);}}
        public void Crown(Vector3 center,Vector3 scale,Color color,int seed=0)
        {const int rings=5,sides=9;int start=v.Count;for(int r=0;r<=rings;r++)for(int i=0;i<=sides;i++){float f=r/(float)rings,a=i*Mathf.PI*2/sides,noise=1+Mathf.Sin(i*2.31f+r*3.7f+seed)*.16f;v.Add(center+Vector3.Scale(new Vector3(Mathf.Cos(a)*Mathf.Sin(f*Mathf.PI),Mathf.Cos(f*Mathf.PI),Mathf.Sin(a)*Mathf.Sin(f*Mathf.PI))*noise,scale));c.Add(color*(.75f+(1-f)*.35f));}for(int r=0;r<rings;r++)for(int i=0;i<sides;i++){int a=start+r*(sides+1)+i,b=a+sides+1;t.Add(a);t.Add(a+1);t.Add(b);t.Add(a+1);t.Add(b+1);t.Add(b);}}
        public void Boulder(Vector3 center,Vector3 scale,Color color,int seed=0,int rings=6,int sides=12)
        {
            int top=v.Count;v.Add(center+Vector3.Scale(Vector3.up,scale));c.Add(color*1.08f);
            for(int r=1;r<rings;r++)for(int i=0;i<sides;i++)
            {
                float latitude=r*Mathf.PI/rings,angle=i*Mathf.PI*2/sides;
                float radial=1+(Mathf.Sin(angle*3+seed+latitude*2)*.10f+Mathf.Cos(angle*5-seed)*.045f)*Mathf.Sin(latitude);
                v.Add(center+Vector3.Scale(new Vector3(Mathf.Cos(angle)*Mathf.Sin(latitude)*radial,Mathf.Cos(latitude),Mathf.Sin(angle)*Mathf.Sin(latitude)*radial),scale));c.Add(color*(.92f+.12f*Mathf.Cos(latitude)));
            }
            int bottom=v.Count;v.Add(center-Vector3.Scale(Vector3.up,scale));c.Add(color*.82f);
            for(int i=0;i<sides;i++){int next=(i+1)%sides;t.Add(top);t.Add(top+1+next);t.Add(top+1+i);}
            for(int r=0;r<rings-2;r++)for(int i=0;i<sides;i++)
            {int a=top+1+r*sides+i,next=top+1+r*sides+(i+1)%sides,b=a+sides;t.Add(a);t.Add(next);t.Add(b);t.Add(next);t.Add(next+sides);t.Add(b);}
            for(int i=0;i<sides;i++){int a=top+1+(rings-2)*sides+i,next=top+1+(rings-2)*sides+(i+1)%sides;t.Add(a);t.Add(next);t.Add(bottom);}
        }
        public Mesh Build(string name){var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
    }
}
