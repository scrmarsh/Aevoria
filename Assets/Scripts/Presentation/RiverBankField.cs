using System;
using System.Collections.Generic;
using Aevoria.World;
using UnityEngine;

namespace Aevoria.Presentation
{
    // A spatial index of the authoritative monotone river curves. Materials
    // must query geographic distance, not interpolate bank flags over a mesh.
    public sealed class RiverBankField : MonoBehaviour
    {
        private const int N=1024;
        private struct Curve {public Vector4 A,B,C,D,E;}
        private struct Range {public int Start,Count;}
        private ComputeBuffer curves,ranges,indices;
        private static RiverBankField owner;
        public void Initialize(ProceduralGeography geo)
        {
            Release();var h=geo.Hydrology;var data=new List<Curve>();var bins=new List<int>[N*N];
            double width=geo.Configuration.WidthMeters,depth=geo.Configuration.DepthMeters;
            for(int node=0;node<h.Ground.Length;node++)if(h.IsRiver(node))
            {
                int count=h.PathSamples(node);float width0=h.RiverWidthAt(node,0),width1=h.RiverWidthAt(node,1),half=Math.Max(width0,width1)*.5f;int receiver=h.Receiver[node];
                double dx=h.X(receiver)-h.X(node),dz=h.Y(receiver)-h.Y(node);
                float grade=(float)(Math.Max(0,h.Water[node]-h.Water[receiver])/Math.Max(1,Math.Sqrt(dx*dx+dz*dz)));
                float dry=geo.InlandSurfaces.IsSeasonallyDryChannel(node)?1:0;
                for(int p=0;p<count;p++)
                {
                    h.PathPoint(node,p,out double ax,out double az);h.PathPoint(node,p+1,out double bx,out double bz);
                    h.PathPoint(node,Math.Max(0,p-1),out double px,out double pz);h.PathPoint(node,Math.Min(count,p+2),out double nx,out double nz);
                    int id=data.Count;data.Add(new Curve{A=new Vector4((float)ax,(float)az,h.PathLevel(node,p/(double)count),h.PathLevel(node,(p+1)/(double)count)),B=new Vector4((float)bx,(float)bz,half,grade),C=new Vector4((float)px,(float)pz,(float)nx,(float)nz),D=new Vector4(h.PathLevel(node,Math.Max(0,p-1)/(double)count),h.PathLevel(node,Math.Min(count,p+2)/(double)count),node+1,dry),E=new Vector4(width0,width1,p/(float)count,(p+1)/(float)count)});
                    double radius=half+32;
                    int x0=Math.Max(0,(int)Math.Floor((Math.Min(ax,bx)-radius)/width*N+N*.5)),x1=Math.Min(N-1,(int)Math.Floor((Math.Max(ax,bx)+radius)/width*N+N*.5));
                    int z0=Math.Max(0,(int)Math.Floor((Math.Min(az,bz)-radius)/depth*N+N*.5)),z1=Math.Min(N-1,(int)Math.Floor((Math.Max(az,bz)+radius)/depth*N+N*.5));
                    for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){int cell=z*N+x;if(bins[cell]==null)bins[cell]=new List<int>();bins[cell].Add(id);}
                }
            }
            var offsets=new Range[N*N];var flat=new List<int>();for(int i=0;i<bins.Length;i++){offsets[i]=new Range{Start=flat.Count,Count=bins[i]?.Count??0};if(bins[i]!=null)flat.AddRange(bins[i]);}
            // Bind valid buffers even for a world without rivers.
            if(data.Count==0)data.Add(default);if(flat.Count==0)flat.Add(0);
            curves=new ComputeBuffer(data.Count,80);curves.SetData(data);ranges=new ComputeBuffer(offsets.Length,8);ranges.SetData(offsets);indices=new ComputeBuffer(flat.Count,4);indices.SetData(flat);
            Shader.SetGlobalBuffer("_AevoriaRiverCurves",curves);Shader.SetGlobalBuffer("_AevoriaRiverRanges",ranges);Shader.SetGlobalBuffer("_AevoriaRiverIndices",indices);Shader.SetGlobalFloat("_AevoriaRiverBanksReady",1);owner=this;
        }
        private void Release()
        {
            if(owner==this)
            {
                Shader.SetGlobalFloat("_AevoriaRiverBanksReady",0);
                Shader.SetGlobalBuffer("_AevoriaRiverCurves",(ComputeBuffer)null);Shader.SetGlobalBuffer("_AevoriaRiverRanges",(ComputeBuffer)null);Shader.SetGlobalBuffer("_AevoriaRiverIndices",(ComputeBuffer)null);owner=null;
            }
            curves?.Release();ranges?.Release();indices?.Release();curves=ranges=indices=null;
        }
        private void OnDestroy()=>Release();
    }
}
