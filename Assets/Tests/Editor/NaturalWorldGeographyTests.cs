using System;
using System.Collections.Generic;
using System.Linq;
using Aevoria.World;
using NUnit.Framework;
using Aevoria.Presentation;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Aevoria.Tests
{
    public sealed class NaturalWorldGeographyTests
    {
        private ProceduralGeography geo;
        [OneTimeSetUp]public void Generate()=>geo=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents)));
        [Test]public void CoastCalibrationDoesNotLiftStablePlainsByKilometres()
        {
            // Coast calibration changes horizontal coverage, never the physical
            // surface datum. Sample the actual ordinary-elevation product.
            var plains=new List<float>();
            for(int z=0;z<97;z++)for(int x=0;x<97;x++)
            {
                double e=((x+.5)/97-.5)*128000,n=((z+.5)/97-.5)*128000;float h=geo.OrdinaryTerrain.ElevationAt(e,n);
                if(h>10)plains.Add(h);
            }
            Assert.Greater(plains.Count,100);plains.Sort();Assert.Less(plains[plains.Count/2],400,"Stable plains retain low physical elevations rather than inheriting a global coastal datum correction");
        }
        [Test]public void RepresentativeGrasslandIsSubstantiallyGentlerThanTheMazeBaseline()
        {
            var slopes=new List<double>();for(int z=0;z<129;z++)for(int x=0;x<129;x++)
            {
                double e=((x+.5)/129-.5)*128000,n=((z+.5)/129-.5)*128000;var s=geo.Sample(new GeoPoint(e,n));if(s.Biome!=Biome.Grassland||s.ElevationMeters<5)continue;
                double dx=(geo.ElevationAt(e+32,n)-geo.ElevationAt(e-32,n))/64,dz=(geo.ElevationAt(e,n+32)-geo.ElevationAt(e,n-32))/64;slopes.Add(Math.Sqrt(dx*dx+dz*dz));
            }
            Assert.Greater(slopes.Count,100);slopes.Sort();Assert.Less(slopes[slopes.Count/2],.67938*.25,"Median relief across all grassland, including mountain grassland, improves at least 75% against the recorded maze baseline");Assert.Greater(slopes.Count(s=>s<.05)/(double)slopes.Count,.35,"The recorded generator-7 reference had only 7.3% gentle grassland samples");
        }
        [Test]public void LowlandGrasslandsContainExtensiveGentlePlains()
        {
            var slopes=new List<double>();for(int z=0;z<129;z++)for(int x=0;x<129;x++)
            {
                double e=((x+.5)/129-.5)*128000,n=((z+.5)/129-.5)*128000;var s=geo.Sample(new GeoPoint(e,n));
                if(s.Biome!=Biome.Grassland||s.ElevationMeters<5||s.ElevationMeters>300||geo.Geology.Sample(e,n).Uplift>.04)continue;
                double dx=(geo.ElevationAt(e+32,n)-geo.ElevationAt(e-32,n))/64,dz=(geo.ElevationAt(e,n+32)-geo.ElevationAt(e,n-32))/64;slopes.Add(Math.Sqrt(dx*dx+dz*dz));
            }
            slopes.Sort();Assert.Greater(slopes.Count,100);Assert.Less(slopes[slopes.Count/2],.05);Assert.Greater(slopes.Count(s=>s<.05)/(double)slopes.Count,.65);
        }
        [Test]public void FormerDryBasinCannotCarryARiverAtItsDiscardedSpillHeight()
        {
            const double x=-2847.5087890625,z=16285.0087890625;
            if(geo.InlandSurfaces.LakeAt(x,z,out float lake)){Assert.Less(lake,700,"The former rejected spill was 744 m above a roughly 558 m floor");return;}
            if(geo.Hydrology.ChannelSample(x,z,out float level,out _,out float width))Assert.Less(level-geo.ElevationAt(x,z),HydrologyMap.ChannelDepth(width)+3,"A discarded reservoir cannot leave a suspended river above its former floor");
        }
        [Test]public void ClosedRiverTerminalHasAWetRoundedSurfaceBeyondItsFinalCrossSection()
        {
            var small=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Small));var h=small.Hydrology;
            // Select by topology rather than a generator-specific raster index.
            // This seed contains a real closed basin in generator 10.
            var terminals=Enumerable.Range(0,h.Ground.Length).Where(i=>h.IsRiver(i)&&h.Receiver[i]>=0&&small.InlandSurfaces.HasTerminalPool(h.Receiver[i])).OrderByDescending(i=>h.Discharge[i]).ToArray();
            Assert.IsNotEmpty(terminals,"The fixture must exercise a real wet closed river terminal");
            int branch=terminals[0],root=h.Receiver[branch];Assert.IsTrue(small.InlandSurfaces.HasTerminalPool(root));
            int steps=h.PathSamples(branch);h.PathPoint(branch,steps-1,out double ax,out double az);h.PathPoint(branch,steps,out double x,out double z);
            double dx=x-ax,dz=z-az,length=Math.Sqrt(dx*dx+dz*dz),radius=h.RiverWidth(branch)*.5;dx/=length;dz/=length;int beyond=0;
            var mesh=small.InlandSurfaces.Rivers;
            for(int i=0;i<mesh.Vertices.Count;i+=3)
            {
                var a=mesh.Vertices[i];var b=mesh.Vertices[i+1];var c=mesh.Vertices[i+2];double cx=(a.X+b.X+c.X)/3,cz=(a.Z+b.Z+c.Z)/3;
                if((cx-x)*dx+(cz-z)*dz<radius*.15||Math.Sqrt((cx-x)*(cx-x)+(cz-z)*(cz-z))>radius*.95)continue;
                Assert.Less(small.ElevationAt(cx,cz),(a.Level+b.Level+c.Level)/3+.04);beyond++;
            }
            Assert.Greater(beyond,3,"Real water triangles cover the rounded channel bed downstream of its final row");
        }
        [TestCase(false)][TestCase(true)]public void GpuRiverBanksMatchCanonicalCurvesAcrossMeshIndependentCoordinates(bool atlas)
        {
            var reviewGeo=atlas?new ProceduralGeography(GenerationSettings.Preset("aevoria-001",WorldSize.Miniature,new GenerationSettings(profile:GeographyProfile.Atlas))):geo;
            var points=new List<Vector2>();var h=reviewGeo.Hydrology;
            var nodes=Enumerable.Range(0,h.Ground.Length).Where(h.IsRiver).OrderByDescending(node=>h.RiverWidthAt(node,1)-h.RiverWidthAt(node,0));
            if(atlas)Assert.That(nodes.Any(node=>h.RiverWidthAt(node,1)-h.RiverWidthAt(node,0)>1),"Fixture exercises widening channels, not only historical constant widths");
            foreach(int node in nodes)
            {
                if(points.Count>=192)break;
                foreach(double fraction in new[]{.1,.5,.9})
                {
                    int at=(int)Math.Round(h.PathSamples(node)*fraction);h.PathPoint(node,at,out double x,out double z);
                    float half=h.RiverWidthAt(node,at/(double)h.PathSamples(node))*.5f;
                    foreach(float offset in new[]{0f,half+3.5f,-half-3.5f})
                    {
                        var p=new Vector2((float)x+offset,(float)z);
                        if(h.ChannelSample(p.x,p.y,out _,out float bank,out _)&&Math.Abs(bank)<25)points.Add(p);
                    }
                }
            }
            Assert.Greater(points.Count,100);var obj=new GameObject("Canonical river bank GPU verification");ComputeBuffer input=null,output=null;
            try
            {
                var field=obj.AddComponent<RiverBankField>();field.Initialize(reviewGeo);
                var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Tests/Editor/RiverBankProbe.compute");Assert.IsNotNull(shader);int kernel=shader.FindKernel("Probe");
                foreach(var binding in new[]{("curves","_AevoriaRiverCurves"),("ranges","_AevoriaRiverRanges"),("indices","_AevoriaRiverIndices")})shader.SetBuffer(kernel,binding.Item2,(ComputeBuffer)typeof(RiverBankField).GetField(binding.Item1,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(field));
                input=new ComputeBuffer(points.Count,8);input.SetData(points);output=new ComputeBuffer(points.Count,12);shader.SetBuffer(kernel,"Points",input);shader.SetBuffer(kernel,"Results",output);
                var c=reviewGeo.Configuration;shader.SetVector("Rectangle",new Vector4((float)-c.WidthMeters*.5f,(float)-c.DepthMeters*.5f,(float)c.WidthMeters,(float)c.DepthMeters));shader.SetFloat("_AevoriaRiverBanksReady",1);shader.SetInt("PointCount",points.Count);shader.Dispatch(kernel,(points.Count+63)/64,1,1);
                var results=new Vector3[points.Count];output.GetData(results);
                for(int i=0;i<points.Count;i++){h.ChannelSample(points[i].x,points[i].y,out float level,out float bank,out _);Assert.AreEqual(1,results[i].z,"Indexed curve missing at "+points[i]);Assert.AreEqual(bank,results[i].x,.15,"Signed bank distance at "+points[i]);Assert.AreEqual(level,results[i].y,.05,"Canonical water level at "+points[i]);}
            }
            finally{input?.Release();output?.Release();UnityEngine.Object.DestroyImmediate(obj);}
        }
        [Test]public void ElevatedPlateauCoresExtendAcrossKilometres()
        {
            const int n=129;var core=new bool[n*n];int count=0;float low=float.MaxValue,high=float.MinValue;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                double e=((x+.5)/n-.5)*128000,k=((z+.5)/n-.5)*128000;float h=geo.ElevationAt(e,k);
                core[z*n+x]=h>300&&geo.OrdinaryTerrain.PlateauAt(e,k)>.95;if(core[z*n+x])count++;low=Math.Min(low,h);high=Math.Max(high,h);
            }
            Assert.Greater(count,20);int largest=0;var visited=new bool[core.Length];
            for(int i=0;i<core.Length;i++)if(core[i]&&!visited[i])
            {
                var queue=new Queue<int>();queue.Enqueue(i);visited[i]=true;int area=0;
                while(queue.Count>0){int j=queue.Dequeue(),x=j%n,z=j/n;area++;foreach(int next in new[]{x>0?j-1:-1,x<n-1?j+1:-1,z>0?j-n:-1,z<n-1?j+n:-1})if(next>=0&&core[next]&&!visited[next]){visited[next]=true;queue.Enqueue(next);}}
                largest=Math.Max(largest,area);
            }
            Assert.Greater(largest,16,"Connected elevated caprock provinces must cover many square kilometres");Assert.Less(low,-100);Assert.Greater(high,1500);
        }
        [Test]public void RiverChannelBedContinuesThroughThePhysicalCoast()
        {
            var h=geo.Hydrology;bool found=false;
            for(int node=0;node<h.Ground.Length&&!found;node++)if(h.IsRiver(node)&&h.RiverWidth(node)>20)
            for(int p=0;p<h.PathSamples(node)&&!found;p++)
            {
                h.PathPoint(node,p,out double ax,out double az);h.PathPoint(node,p+1,out double bx,out double bz);
                if(geo.Continents.CoastDistanceAt(ax,az)<=0||geo.Continents.CoastDistanceAt(bx,bz)>=0)continue;
                for(int j=0;j<28;j++){double x=(ax+bx)*.5,z=(az+bz)*.5;if(geo.Continents.CoastDistanceAt(x,z)>0){ax=x;az=z;}else{bx=x;bz=z;}}
                float land=geo.ElevationAt(ax,az),ocean=geo.ElevationAt(bx,bz);
                Assert.Less(land,-.25,"A real channel remains below the sea at its mouth");Assert.Less(ocean,-.25,"Crossing zero must not restore an uncarved ocean-floor sill");Assert.AreEqual(land,ocean,.03,"Canonical channel bed must be continuous across the physical coast");found=true;
            }
            Assert.IsTrue(found,"The fixture must contain a channel crossing the physical coast");
        }
        [Test]public void PhysicalCoastCrossesZeroWithoutChangingBathymetricSign()
        {
            var h=geo.Hydrology;int inspected=0;
            for(int i=1;i<h.Ground.Length&&inspected<30;i++)if(i%HydrologyMap.Resolution>0&&h.Ground[i]>0&&h.Ground[i-1]<0)
            {
                double land=h.X(i),ocean=h.X(i-1),z=h.Y(i);
                for(int step=0;step<24;step++){double x=(land+ocean)*.5;if(geo.Continents.CoastDistanceAt(x,z)>0)land=x;else ocean=x;}
                Assert.Greater(geo.BaseElevationAt(land,z),0);Assert.Less(geo.BaseElevationAt(ocean,z),0);Assert.Less(Math.Abs(geo.BaseElevationAt(land,z)-geo.BaseElevationAt(ocean,z)),.1);inspected++;
            }
            Assert.Greater(inspected,10);
        }
    }
}
