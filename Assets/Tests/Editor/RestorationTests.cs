using System;
using System.Reflection;
using System.Threading;
using Aevoria.World;
using Aevoria.Persistence;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace Aevoria.Tests
{
    public sealed class RestorationTests
    {
        private ProceduralGeography geo;
        [OneTimeSetUp]public void Setup()=>geo=new ProceduralGeography(GenerationSettings.Preset("restoration-008",WorldSize.Miniature));
        [Test]public void RegionalGroundQueriesUsePublishedTrianglesAfterUploadArraysAreReleased()
        {
            var generate=typeof(RegionalTerrain).GetMethod("Generate",BindingFlags.NonPublic|BindingFlags.Static);
            var data=generate.Invoke(null,new object[]{geo,Vector2.zero,16,32f,CancellationToken.None});data.GetType().GetField("Heights").SetValue(data,null);
            var obj=new GameObject("Published regional query contract");
            try
            {
                var regional=obj.AddComponent<RegionalTerrain>();typeof(RegionalTerrain).GetField("installed",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(regional,data);
                for(int i=0;i<32;i++)
                {
                    float x=-240+i*14.31f,z=-235+i*13.21f;
                    Assert.IsTrue(regional.TryTriangle(x,z,out var a,out var b,out var c,out var weights));Assert.IsTrue(regional.TryHeight(x,z,out float height));
                    Assert.AreEqual(a.y*weights.x+b.y*weights.y+c.y*weights.z,height,.0001,"Collision and join queries retain the exact published triangle field");
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(obj);}
        }
        [Test]public void MountainRidgeAxisDoesNotReverseAcrossTheSamePlateBoundary()
        {
            var plates=geo.Geology.Plates;double width=geo.Configuration.WidthMeters,epsilon=width/(GeologyMap.Resolution-1)*2;int checkedBoundaries=0;
            bool NearestPair(double x,double z,int a,int b)
            {
                double da=Math.Pow(x/width-plates[a].X,2)+Math.Pow(z/width-plates[a].Z,2),db=Math.Pow(x/width-plates[b].X,2)+Math.Pow(z/width-plates[b].Z,2);
                for(int k=0;k<plates.Count;k++)if(k!=a&&k!=b&&Math.Pow(x/width-plates[k].X,2)+Math.Pow(z/width-plates[k].Z,2)<Math.Max(da,db)+.002)return false;
                return true;
            }
            for(int a=0;a<plates.Count;a++)for(int b=a+1;b<plates.Count;b++)
            {
                double dx=plates[b].X-plates[a].X,dz=plates[b].Z-plates[a].Z,length=Math.Sqrt(dx*dx+dz*dz),x=(plates[a].X+plates[b].X)*width*.5,z=(plates[a].Z+plates[b].Z)*width*.5;
                double ax=x-dx/length*epsilon,az=z-dz/length*epsilon,bx=x+dx/length*epsilon,bz=z+dz/length*epsilon;
                if(!NearestPair(ax,az,a,b)||!NearestPair(bx,bz,a,b))continue;
                var left=geo.Geology.Sample(ax,az);var right=geo.Geology.Sample(bx,bz);
                Assert.Greater(Vector2.Dot(new Vector2(left.DirectionX,left.DirectionZ).normalized,new Vector2(right.DirectionX,right.DirectionZ).normalized),.5f,"A continuous mountain belt must not acquire opposite ridge phases when nearest plates exchange order");checkedBoundaries++;
            }
            Assert.Greater(checkedBoundaries,2,"Exercise actual neighboring tectonic boundaries");
        }
        [Test]public void CoastalMoistureExchangesBetweenWindLanes()
        {
            const int n=33;var land=new float[n*n];var coastal=new float[n*n];var forcing=new float[n];
            for(int i=0;i<land.Length;i++)land[i]=coastal[i]=100;for(int z=0;z<n;z++)forcing[z]=.6f;
            coastal[(n/2)*n]=-1;
            var advect=typeof(ClimateMap).GetMethod("Advect",BindingFlags.Static|BindingFlags.NonPublic);
            var dry=(float[])advect.Invoke(null,new object[]{land,forcing,n,1000f,true});
            var wet=(float[])advect.Invoke(null,new object[]{coastal,forcing,n,1000f,true});
            var repeat=(float[])advect.Invoke(null,new object[]{coastal,forcing,n,1000f,true});
            Assert.Greater(wet[(n/2+1)*n+10],dry[(n/2+1)*n+10]+.001f,"A coastal source must supply neighboring wind lanes inland");
            Assert.Greater(wet[(n/2-1)*n+10],dry[(n/2-1)*n+10]+.001f);
            CollectionAssert.AreEqual(wet,repeat);
            foreach(float value in wet)Assert.That(value,Is.InRange(0f,1f));
        }
        [Test]public void CubicRetainsNodesAndDoesNotInventPeaks()
        {
            foreach(var row in new[]{new[]{0d,4d,7d,20d},new[]{8d,4d,7d,2d},new[]{2d,2d,2d,2d}})
            for(int i=0;i<=1000;i++){double h=ContinuousGrid.Curve(row[0],row[1],row[2],row[3],i/1000d);Assert.That(h,Is.InRange(Math.Min(row[1],row[2])-1e-12,Math.Max(row[1],row[2])+1e-12));}
            Assert.AreEqual(4,ContinuousGrid.Curve(0,4,7,20,0));Assert.AreEqual(7,ContinuousGrid.Curve(0,4,7,20,1));
        }
        [Test]public void AdjacentHeightCellsShareTheirSlope()
        {
            const double e=1e-6;double left=(7-ContinuousGrid.Curve(0,4,7,20,1-e))/e,right=(ContinuousGrid.Curve(4,7,20,25,e)-7)/e;
            Assert.AreEqual(left,right,.0001);
        }
        [Test]public void GridInterpolationIsContinuousInBothAxesAndFiniteAtEdges()
        {
            const int n=7;var f=new float[n*n];for(int z=0;z<n;z++)for(int x=0;x<n;x++)f[z*n+x]=x*x+z*z*.3f;
            foreach(double z in new[]{-1d,0,2.7,6,8})foreach(double x in new[]{-1d,0,2.7,6,8})Assert.IsFalse(float.IsNaN(ContinuousGrid.Sample(f,n,x,z)));
            Assert.AreEqual(ContinuousGrid.Sample(f,n,3-1e-5,2.3),ContinuousGrid.Sample(f,n,3+1e-5,2.3),.001);
            Assert.AreEqual(ContinuousGrid.Sample(f,n,2.3,3-1e-5),ContinuousGrid.Sample(f,n,2.3,3+1e-5),.001);
        }
        [Test]public void ExteriorSeabedStaysBelowTheConfiguredOcean()
        {
            double w=geo.Configuration.WidthMeters/2,d=geo.Configuration.DepthMeters/2;
            foreach(var p in new[]{new GeoPoint(w+1,0),new GeoPoint(-w-1,0),new GeoPoint(0,d+1),new GeoPoint(0,-d-1),new GeoPoint(w*4,d*4)})Assert.LessOrEqual(geo.SceneryElevationAt(p.X,p.Y),geo.SeaLevel-50);
        }
        [Test]public void RefinedShoreTrianglesRetainOneLevelAndOneOwner()
        {
            var layout=geo.InlandSurfaces;Assert.Greater(layout.ShoreSamples,0);Assert.LessOrEqual(layout.ShoreSamples,1000000);
            int count=0;for(int i=0;i<layout.Lakes.Vertices.Count;i+=3)
            {var a=layout.Lakes.Vertices[i];var b=layout.Lakes.Vertices[i+1];var c=layout.Lakes.Vertices[i+2];Assert.AreEqual(a.Level,b.Level);Assert.AreEqual(a.Level,c.Level);Assert.IsTrue(layout.LakeAt((a.X+b.X+c.X)/3,(a.Z+b.Z+c.Z)/3,out _));count++;}Assert.Greater(count,10);
        }
        [Test]public void ActualChannelCentresAreWetAndLocalBedsAreShallow()
        {
            var h=geo.Hydrology;int inspected=0,shallow=0;
            for(int i=0;i<h.Ground.Length;i++)if(h.IsRiver(i))
            {h.PathPoint(i,h.PathSamples(i)/2,out double x,out double z);if(geo.InlandSurfaces.LakeAt(x,z,out _))continue;float surface=h.PathLevel(i,.5),ground=geo.ElevationAt(x,z);Assert.Less(ground,surface+.05f);if(surface-ground<=HydrologyMap.ChannelDepth(h.RiverWidth(i),1)+.1f)shallow++;inspected++;}
            Assert.Greater(inspected,10);Assert.Greater(shallow,inspected*.8,"Most centres must remain within their actual shallow channel profile; retain confluence overlap exceptions");
        }
        [Test]public void LargerDischargeNeverNarrowsAChannel()
        {
            var h=geo.Hydrology;int a=0,b=1;for(int i=0;i<h.Discharge.Length;i++){if(h.Discharge[i]<h.Discharge[a])a=i;if(h.Discharge[i]>h.Discharge[b])b=i;}
            Assert.GreaterOrEqual(h.RiverWidth(b),h.RiverWidth(a));Assert.LessOrEqual(h.RiverWidth(b),2200);Assert.LessOrEqual(HydrologyMap.ChannelDepth(h.RiverWidth(b),1),18);
        }
        [Test]public void PathDistanceRemainsLipschitzAcrossFormerSearchWindowBoundaries()
        {
            var h=geo.Hydrology;var distance=typeof(HydrologyMap).GetMethod("DistanceToPath",BindingFlags.NonPublic|BindingFlags.Instance);int checkedPaths=0;
            for(int i=0;i<h.Ground.Length;i+=17)if(h.IsChannelCandidate(i))
            {
                int r=h.Receiver[i];double vx=h.X(r)-h.X(i),vz=h.Y(r)-h.Y(i),length=Math.Sqrt(vx*vx+vz*vz),ex=vx/length,ez=vz/length;
                foreach(double offset in new[]{-h.StepX*.8,h.StepX*.8})for(int k=1;k<HydrologyMap.PathSteps;k+=3)
                {
                    double x=h.X(i)+vx*k/HydrologyMap.PathSteps-ez*offset,z=h.Y(i)+vz*k/HydrologyMap.PathSteps+ex*offset;
                    double a=(double)distance.Invoke(h,new object[]{i,x-ex*.02,z-ez*.02,0d}),b=(double)distance.Invoke(h,new object[]{i,x+ex*.02,z+ez*.02,0d});
                    Assert.LessOrEqual(Math.Abs(a-b),.040001,"Distance to a fixed river path cannot jump when a search-window index changes");
                }
                checkedPaths++;
            }
            Assert.Greater(checkedPaths,5);
        }
        [Test]public void FineWaterProfilesAreDownhillAndMeetTheirReceiver()
        {
            var h=geo.Hydrology;for(int i=0;i<h.Ground.Length;i++)if(h.IsChannelCandidate(i))
            {float previous=h.PathLevel(i,0);Assert.AreEqual(h.Water[i],previous,.001);for(int p=1;p<=HydrologyMap.PathSteps;p++){float next=h.PathLevel(i,p/(double)HydrologyMap.PathSteps);Assert.LessOrEqual(next,previous+.001);previous=next;}Assert.AreEqual(h.Water[h.Receiver[i]],previous,.001);}
        }
        [Test]public void RegionalRiverBedRetainsBankMaterialOutsideDetailedTiles()
        {
            var h=geo.Hydrology;int river=-1;double x=0,z=0;
            for(int i=0;i<h.Ground.Length;i++)if(h.IsRiver(i))
            {
                h.PathPoint(i,h.PathSamples(i)/2,out double px,out double pz);
                if(h.PathLevel(i,.5)<=geo.SeaLevel+2||geo.InlandSurfaces.LakeAt(px,pz,out _))continue;
                river=i;x=px;z=pz;break;
            }
            Assert.GreaterOrEqual(river,0);var method=typeof(RegionalTerrain).GetMethod("Generate",BindingFlags.NonPublic|BindingFlags.Static);
            var data=method.Invoke(null,new object[]{geo,new Vector2((float)x,(float)z),16,8f,CancellationToken.None});
            var banks=(Vector4[])data.GetType().GetField("Banks").GetValue(data);Assert.AreEqual(17*17,banks.Length);
            var centre=banks[8*17+8];Assert.AreEqual(1,centre.z,"The regional fallback must retain the river-bed material after detailed tiles end");Assert.AreEqual(0,centre.w);Assert.Less(centre.y,.1f);
        }
        [Test]public void DrainageDoesNotJumpAtSpatialLookupCellBoundaries()
        {
            var h=geo.Hydrology;
            for(int z=2;z<HydrologyMap.Resolution-2;z+=3)for(int x=2;x<HydrologyMap.Resolution-2;x+=5)
            {
                int i=z*HydrologyMap.Resolution+x;double east=h.X(i)+h.StepX*.5,north=h.Y(i)+h.StepY*.13;
                double left=geo.ElevationAt(east-.02,north)-geo.BaseElevationAt(east-.02,north),right=geo.ElevationAt(east+.02,north)-geo.BaseElevationAt(east+.02,north);
                Assert.Less(Math.Abs(left-right),.5,"East-west drainage lookup must not add a wall to the underlying geology");
                east=h.X(i)+h.StepX*.17;north=h.Y(i)+h.StepY*.5;
                double down=geo.ElevationAt(east,north-.02)-geo.BaseElevationAt(east,north-.02),up=geo.ElevationAt(east,north+.02)-geo.BaseElevationAt(east,north+.02);
                Assert.Less(Math.Abs(down-up),.5,"North-south drainage lookup must not add rows to the underlying geology");
            }
        }
        [Test]public void RegionalNormalsDescribeItsActualTriangleScale()
        {
            var method=typeof(RegionalTerrain).GetMethod("Generate",BindingFlags.NonPublic|BindingFlags.Static);var data=method.Invoke(null,new object[]{geo,Vector2.zero,16,32f,CancellationToken.None});var t=data.GetType();var heights=(float[])t.GetField("Heights").GetValue(data);var normals=(Vector3[])t.GetField("Normals").GetValue(data);
            var expected=new Vector3((heights[8*17+7]-heights[8*17+9])/64,1,(heights[7*17+8]-heights[9*17+8])/64).normalized;Assert.Less((normals[8*17+8]-expected).magnitude,.00001);
        }
        [Test]public void LocalInteriorNormalsDescribeTheSampledMeshAtEveryLod()
        {
            var method=typeof(WorldRenderer).GetMethod("GeneratePayload",BindingFlags.NonPublic|BindingFlags.Static);
            for(int lod=0;lod<=2;lod++)
            {
                var data=method.Invoke(null,new object[]{geo,new ChunkKey(5,3),lod,false,CancellationToken.None});var type=data.GetType();var vertices=(Vector3[])type.GetField("Vertices").GetValue(data);var normals=(Vector3[])type.GetField("Normals").GetValue(data);
                int n=geo.Configuration.CellsPerChunk>>lod,row=n+1;
                for(int z=3;z<n-2;z+=3)for(int x=3;x<n-2;x+=3){int at=z*row+x;var east=vertices[at+1]-vertices[at-1];var north=vertices[at+row]-vertices[at-row];var expected=Vector3.Cross(north,east).normalized;Assert.Less((normals[at]-expected).magnitude,.001,"Lighting must describe the sampled surface rather than an unsampled fine stencil");}
            }
        }
        [Test]public void RegionalWorkHonoursCancellation()
        {
            using var c=new CancellationTokenSource();c.Cancel();var method=typeof(RegionalTerrain).GetMethod("Generate",BindingFlags.NonPublic|BindingFlags.Static);var ex=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{geo,Vector2.zero,16,32f,c.Token}));Assert.IsInstanceOf<OperationCanceledException>(ex.InnerException);
        }
        [Test]public void GraphicsControlsRejectUnsupportedTransparencyAndTransitions()
        {
            var settings=new ApplicationSettings();settings.Validate();settings.WaterTransparency=3;Assert.Throws<ArgumentException>(()=>settings.Validate());settings.WaterTransparency=2;settings.LodTransition=-1;Assert.Throws<ArgumentException>(()=>settings.Validate());
        }
    }
}
