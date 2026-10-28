using System.Collections.Generic;
using System.Threading.Tasks;
using Aevoria.World;
using Aevoria.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

namespace Aevoria.Presentation
{
    public sealed class WorldVegetation : MonoBehaviour
    {
        private static readonly ProfilerMarker DrawMarker=new ProfilerMarker("Aevoria.Vegetation");
        public float ViewDistance {get;set;}=1800;public float Density {get;set;}=1;
        public const int TreeVariants=12,CoverVariants=8;
        public const int OwnedMeshCount=TreeVariants*2+CoverVariants+1;
        private sealed class Batch {public Matrix4x4[][] Trees,Cover,Filtered;public Matrix4x4[] Rocks,FilteredRocks;public Bounds Bounds;public Vector3 GroundCenter;public int Count,Revision=-1;public long Year=-1;}
        public Mesh RockMesh=>rock;
        public static string RockId(Matrix4x4 m)=>"outcrop:"+Mathf.FloorToInt(m.m03/16)+":"+Mathf.FloorToInt(m.m23/16);
        public IEnumerable<(string Id,Matrix4x4 Transform)> NearbyRocks(Vector3 centre,float range)
        {foreach(var batch in batches.Values)foreach(var m in batch.Rocks)if((new Vector3(m.m03,m.m13,m.m23)-centre).sqrMagnitude<range*range)yield return (RockId(m),m);}
        public LivingWorldState Living {get;set;}
        public System.Func<long> WorldTick {get;set;}
        private readonly Dictionary<ChunkKey,Batch> batches=new Dictionary<ChunkKey,Batch>();
        private readonly Plane[] planes=new Plane[6];
        private readonly Mesh[] near=new Mesh[TreeVariants],far=new Mesh[TreeVariants],cover=new Mesh[CoverVariants];
        private MaterialPropertyBlock properties;
        private Mesh rock;
        private Material material;
        private ProceduralGeography geography;
        private Camera view;
        private sealed class Understory {public Vector2Int Cell;public Matrix4x4[][] Groups;public Bounds Bounds;}
        private Task<Understory> understoryWorker;private Understory understory;private int understoryEdits=-1;private Vector2Int understoryRequest=new Vector2Int(int.MinValue,int.MinValue);
        public int VisibleTrees {get;private set;}
        public int CachedTrees {get;private set;}
        public bool NearCoverReady=>understory!=null&&understoryWorker==null&&understory.Cell==understoryRequest;
        public int VisibleUnderstorySites {get;private set;}
        public long VisibleTriangles {get;private set;}
        public bool Visible {get;set;}=true;
        public void ResetWorld(ProceduralGeography geo,Camera camera)
        {
            if(properties==null) properties=new MaterialPropertyBlock();
            geography=geo;view=camera;batches.Clear();CachedTrees=0;understory=null;understoryEdits=-1;understoryRequest=new Vector2Int(int.MinValue,int.MinValue);
            if(understoryWorker!=null)understoryWorker.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);understoryWorker=null;
            material=Resources.Load<Material>("WorldVegetation");
            if(near[0]==null) {for(int i=0;i<TreeVariants;i++) {near[i]=Tree(i,true);far[i]=Tree(i,false);}for(int i=0;i<CoverVariants;i++)cover[i]=GroundCover(i);rock=Rock();}
        }
        public void Add(ChunkKey key,VegetationChunk placement)
        {
            if(batches.ContainsKey(key)) return;
            var groups=new List<Matrix4x4>[TreeVariants+CoverVariants];for(int i=0;i<groups.Length;i++) groups[i]=new List<Matrix4x4>();
            foreach(var site in placement.Trees) groups[site.Variant%TreeVariants].Add(Matrix4x4.TRS(new Vector3((float)site.East,site.Height,(float)site.North),Quaternion.Euler(0,site.Yaw,0),new Vector3(site.Scale*site.WidthScale,site.Scale,site.Scale*site.WidthScale)));
            foreach(var site in placement.Grass) groups[TreeVariants+site.Variant%CoverVariants].Add(Matrix4x4.TRS(new Vector3((float)site.East,site.Height,(float)site.North),Quaternion.Euler(0,site.Yaw,0),Vector3.one*site.Scale));
            var rocks=new Matrix4x4[placement.Rocks.Length];for(int i=0;i<rocks.Length;i++) {var s=placement.Rocks[i];var slope=Quaternion.FromToRotation(Vector3.up,new Vector3(-s.SlopeEast,1,-s.SlopeNorth).normalized);rocks[i]=Matrix4x4.TRS(new Vector3((float)s.East,s.Height,(float)s.North),slope*Quaternion.Euler(0,s.Yaw,0),Vector3.one*s.Scale);}
            double side=geography.Configuration.ChunkSizeMeters;
            double height=0;foreach(var tree in placement.Trees)height+=tree.Height;height=placement.Trees.Length>0?height/placement.Trees.Length:geography.ElevationAt((key.X+.5)*side,(key.Y+.5)*side);
            float low=(float)height,high=low;
            foreach(var site in placement.Trees){low=Mathf.Min(low,site.Height);high=Mathf.Max(high,site.Height+site.Scale*20);}
            foreach(var site in placement.Grass){low=Mathf.Min(low,site.Height);high=Mathf.Max(high,site.Height+4);}
            foreach(var site in placement.Rocks){low=Mathf.Min(low,site.Height-5);high=Mathf.Max(high,site.Height+10);}
            var batch=new Batch {Trees=new Matrix4x4[TreeVariants][],Cover=new Matrix4x4[CoverVariants][],Rocks=rocks,Count=placement.Trees.Length,GroundCenter=new Vector3((float)((key.X+.5)*side),(float)height,(float)((key.Y+.5)*side)),
                Bounds=new Bounds(new Vector3((float)((key.X+0.5)*side),(low+high)*.5f,(float)((key.Y+0.5)*side)),new Vector3((float)side+80,Mathf.Max(40,high-low+20),(float)side+80))};
            for(int i=0;i<TreeVariants;i++) batch.Trees[i]=groups[i].ToArray();for(int i=0;i<CoverVariants;i++) batch.Cover[i]=groups[TreeVariants+i].ToArray();
            batches[key]=batch;CachedTrees+=batch.Count;
        }
        public void Remove(ChunkKey key) {if(batches.TryGetValue(key,out var batch)) {CachedTrees-=batch.Count;batches.Remove(key);}}
        private void LateUpdate()
        {
            using var measured=DrawMarker.Auto();
            using var work=WorkCounter.Vegetation.Measure(Time.frameCount);
            VisibleTrees=0;VisibleTriangles=0;VisibleUnderstorySites=0;if(!Visible || view==null || material==null || !SystemInfo.supportsInstancing) return;
            GeometryUtility.CalculateFrustumPlanes(view,planes);
            properties.SetFloat("_NearCover",0);
            int resourceBudget=1;long tick=WorldTick?.Invoke()??0;
            foreach(var batch in batches.Values)
            {
                float distance=Vector3.Distance(WorldOrigin.Geographic(view.transform),batch.GroundCenter);
                float groundDistance=Vector2.Distance(new Vector2(WorldOrigin.Geographic(view.transform).x,WorldOrigin.Geographic(view.transform).z),new Vector2(batch.GroundCenter.x,batch.GroundCenter.z));
                float altitude=Mathf.Abs(WorldOrigin.Geographic(view.transform).y-batch.GroundCenter.y);
                float visibleDistance=Mathf.Max(ViewDistance,Mathf.Min(ViewDistance*2,altitude*1.5f));
                if(groundDistance>visibleDistance || !GeometryUtility.TestPlanesAABB(planes,WorldOrigin.Relative(batch.Bounds))) continue;
                if(Living!=null&&Living.HasResourceChanges&&resourceBudget>0&&(batch.Revision!=Living.Revision||batch.Year!=tick/1440))
                {batch.Filtered=new Matrix4x4[TreeVariants][];for(int s=0;s<TreeVariants;s++){var original=batch.Trees[s];int count=0;foreach(var m in original)if(!Living.TreeDepleted(m.m03,m.m23,tick))count++;if(count==original.Length)batch.Filtered[s]=original;else{var filtered=new Matrix4x4[count];int k=0;foreach(var m in original)if(!Living.TreeDepleted(m.m03,m.m23,tick))filtered[k++]=m;batch.Filtered[s]=filtered;}}var liveRocks=new List<Matrix4x4>();foreach(var m in batch.Rocks)if(Living.Available(RockId(m),60,tick,false)>=1)liveRocks.Add(m);batch.FilteredRocks=liveRocks.ToArray();batch.Revision=Living.Revision;batch.Year=tick/1440;resourceBudget--;}
                var trees=batch.Filtered??batch.Trees;
                float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1000,1550,distance));
                float visibility=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(visibleDistance*.8f,visibleDistance,groundDistance));
                var parameters=new RenderParams(material) {camera=view,worldBounds=batch.Bounds,matProps=properties,receiveShadows=true};
                properties.SetFloat("_VegetationVisibility",visibility);
                properties.SetFloat("_Foliage",1);
                properties.SetFloat("_TreeCoverage",1);properties.SetFloat("_VegetationDensity",Density);
                if(blend<1)
                {
                    properties.SetFloat("_VegetationFade",blend);properties.SetFloat("_VegetationFar",0);parameters.shadowCastingMode=ShadowCastingMode.On;
                    for(int i=0;i<TreeVariants;i++) Draw(parameters,near[i],trees[i]);
                }
                if(blend>0)
                {
                    properties.SetFloat("_VegetationFade",blend);properties.SetFloat("_VegetationFar",1);parameters.shadowCastingMode=ShadowCastingMode.Off;
                    for(int i=0;i<TreeVariants;i++) Draw(parameters,far[i],trees[i]);
                }
                VisibleTrees+=batch.Count;
                if(distance<500)
                {
                    properties.SetFloat("_TreeCoverage",0);
                    properties.SetFloat("_VegetationFade",0);properties.SetFloat("_VegetationFar",0);properties.SetFloat("_VegetationVisibility",1-Mathf.InverseLerp(350,500,distance));
                    parameters.shadowCastingMode=ShadowCastingMode.Off;for(int i=0;i<CoverVariants;i++) Draw(parameters,cover[i],batch.Cover[i]);
                }
                if(distance<2300)
                {
                    properties.SetFloat("_Foliage",0);properties.SetFloat("_VegetationFade",0);properties.SetFloat("_VegetationFar",0);
                    properties.SetFloat("_VegetationVisibility",1-Mathf.InverseLerp(1700,2300,distance));parameters.shadowCastingMode=ShadowCastingMode.On;
                    Draw(parameters,rock,batch.FilteredRocks??batch.Rocks);
                }
            }
            DrawUnderstory();
        }
        private void DrawUnderstory()
        {
            if(understoryWorker!=null&&understoryWorker.IsCompleted)
            {if(understoryWorker.IsFaulted)Debug.LogWarning("Understory unavailable: "+understoryWorker.Exception.GetBaseException().Message);else {understory=understoryWorker.Result;understoryEdits=-1;}understoryWorker=null;}
            var eye=WorldOrigin.Geographic(view.transform);float height=geography.ElevationAt(eye.x,eye.z);
            if(eye.y-height>180||eye.y-height< -2)return;
            var cell=new Vector2Int(Mathf.FloorToInt(eye.x/32),Mathf.FloorToInt(eye.z/32));
            if(understoryWorker==null&&cell!=understoryRequest)
            {understoryRequest=cell;var geo=geography;understoryWorker=Task.Run(()=>PrepareUnderstory(geo,cell));}
            if(understory==null||Vector2.Distance(new Vector2(eye.x,eye.z),new Vector2((understory.Cell.x+.5f)*32,(understory.Cell.y+.5f)*32))>120)return;
            if(Living!=null&&understoryEdits!=Living.ExcavationRevision)
            {
                for(int i=0;i<CoverVariants;i++){var kept=new List<Matrix4x4>();foreach(var m in understory.Groups[i])if(Living.SolidDensity(m.m03,m.m13-.06,m.m23)>=0)kept.Add(m);understory.Groups[i]=kept.ToArray();}
                understoryEdits=Living.ExcavationRevision;
            }
            properties.SetFloat("_NearCover",1);properties.SetFloat("_Foliage",1);properties.SetFloat("_TreeCoverage",0);properties.SetFloat("_VegetationFade",0);properties.SetFloat("_VegetationFar",0);properties.SetFloat("_VegetationVisibility",1);
            var parameters=new RenderParams(material){camera=view,worldBounds=understory.Bounds,matProps=properties,receiveShadows=true,shadowCastingMode=ShadowCastingMode.Off};
            for(int i=0;i<CoverVariants;i++){Draw(parameters,cover[i],understory.Groups[i]);VisibleUnderstorySites+=understory.Groups[i].Length;}
        }
        private static Understory PrepareUnderstory(ProceduralGeography geo,Vector2Int cell)
        {
            const int side=48;const double spacing=3;var groups=new List<Matrix4x4>[CoverVariants];for(int i=0;i<groups.Length;i++)groups[i]=new List<Matrix4x4>();
            int cx=(int)System.Math.Floor((cell.x+.5)*32/spacing),cz=(int)System.Math.Floor((cell.y+.5)*32/spacing);float low=float.MaxValue,high=float.MinValue;
            for(int z=cz-side/2;z<cz+side/2;z++)for(int x=cx-side/2;x<cx+side/2;x++)
            {
                double roll=geo.SiteRandom(x,z,1901);double east=(x+.12+geo.SiteRandom(x,z,1902)*.76)*spacing,north=(z+.12+geo.SiteRandom(x,z,1903)*.76)*spacing;
                var p=new GeoPoint(east,north);if(!geo.Configuration.Contains(p))continue;var sample=geo.Sample(p);var habitat=BiomeHabitat.For(sample.Biome);
                // Cosmetic near-field plants share geography and habitat, but
                // create no harvestable sites or saved resource identities.
                if(roll>habitat.CoverDensity*.78||sample.ElevationMeters<geo.SeaLevel+1||geo.Hydrology.IsLakeWater(east,north,sample.ElevationMeters)||geo.Hydrology.IsRiverWater(east,north,sample.ElevationMeters))continue;
                float dx=geo.ElevationAt(east-4,north)-geo.ElevationAt(east+4,north),dz=geo.ElevationAt(east,north-4)-geo.ElevationAt(east,north+4);
                if(dx*dx+dz*dz>36)continue;float h=(float)LocalSolidField.RenderedGround(geo,east,north);low=Mathf.Min(low,h);high=Mathf.Max(high,h);
                float scale=.55f+(float)geo.SiteRandom(x,z,1904)*.75f;var normal=new Vector3(dx,8,dz).normalized;
                groups[habitat.Cover].Add(Matrix4x4.TRS(new Vector3((float)east,h,(float)north),Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,(float)geo.SiteRandom(x,z,1905)*360,0),Vector3.one*scale));
            }
            if(low==float.MaxValue)low=high=0;var result=new Understory{Cell=cell,Groups=new Matrix4x4[CoverVariants][],Bounds=new Bounds(new Vector3((cell.x+.5f)*32,(low+high)*.5f,(cell.y+.5f)*32),new Vector3(side*(float)spacing+32,high-low+8,side*(float)spacing+32))};
            for(int i=0;i<groups.Length;i++)result.Groups[i]=groups[i].ToArray();return result;
        }
        private void Draw(RenderParams parameters,Mesh mesh,Matrix4x4[] matrices)
        {
            for(int start=0;start<matrices.Length;start+=1023) WorldOrigin.DrawInstances(parameters,mesh,0,matrices,Mathf.Min(1023,matrices.Length-start),start);
            VisibleTriangles+=(long)mesh.GetIndexCount(0)/3*matrices.Length;
        }
        private static Mesh Tree(int kind,bool detailed)
        {
            var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
            bool conifer=kind<3;int sides=detailed?10:6;float height=kind==1?15:kind==4?13:11;
            var bark=kind==4?new Color(0.48f,0.44f,0.34f):new Color(0.16f,0.105f,0.065f);
            Cone(v,c,t,sides,Vector3.zero,new Vector3(0.2f*(kind-2),height,0),0.34f,0.09f,bark);
            if(kind>=6)
            {
                if(kind==7) // Narrow cypress crown.
                    Cone(v,c,t,sides,new Vector3(0,2,0),new Vector3(.2f,15,0),1.7f,.05f,new Color(.12f,.24f,.10f));
                else if(kind==9) // Palms have radial, drooping fronds.
                {
                    for(int j=0;j<10;j++)
                    {
                        float angle=j*Mathf.PI*2/10;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                        var tip=direction*4.8f+Vector3.up*(height-1);var middle=direction*2.2f+Vector3.up*(height+1.2f);
                        Cone(v,c,t,4,Vector3.up*height,middle,.18f,.09f,new Color(.25f,.38f,.12f));
                        for(int k=0;k<5;k++)
                        {
                            float f=(k+1)/5f;var point=Vector3.Lerp(middle,tip,f);var side=Vector3.Cross(direction,Vector3.up)*(1-f*.7f);
                            int start=v.Count;v.Add(point-side);v.Add(point+direction*.7f-Vector3.up*.6f);v.Add(point+side);
                            t.Add(start);t.Add(start+1);t.Add(start+2);for(int q=0;q<3;q++)c.Add(new Color(.22f,.36f,.10f));
                        }
                    }
                }
                else
                {
                    int count=kind==8?7:kind==10?11:6;
                    for(int j=0;j<count;j++)
                    {
                        float angle=j*2.39996f,radius=kind==8?3.5f:kind==11?1.3f:2.8f;
                        var center=new Vector3(Mathf.Cos(angle)*radius,kind==11?3.5f+j%2:kind==6?6.5f+j%3:height-1+j%2,Mathf.Sin(angle)*radius);
                        Cone(v,c,t,5,new Vector3(0,kind==11?1.5f:4,0),center,.13f,.035f,bark);
                        Crown(v,c,t,center,kind==11?1.5f:2.4f,detailed?5:3,sides,kind*19+j);
                        if(kind==10)for(int k=0;k<3;k++)Cone(v,c,t,4,center+new Vector3((k-1)*.8f,0,0),center+new Vector3((k-1)*1.3f,-4,0),.3f,.06f,new Color(.19f,.34f,.13f));
                    }
                }
                if(kind==6||kind==11)for(int i=0;i<v.Count;i++){var point=v[i];point.y*=kind==6?.65f:.45f;v[i]=point;}
                return Make(v,c,t,"Instanced habitat tree "+kind+(detailed?" near":" far"));
            }
            if(conifer)
            {
                int layers=kind==2?9:8;
                for(int layer=0;layer<layers;layer++)
                {
                    float f=(float)layer/layers,y=2.4f+f*(height-3),radius=(kind==1?2.7f:3.7f)*Mathf.Pow(1-f,.8f);
                    var needles=new Color(.035f+kind*.007f,.105f+f*.035f,.060f-kind*.009f);
                    if(!detailed) Cone(v,c,t,sides,new Vector3(0,y-.45f,0),new Vector3(0,y+2.8f,0),radius,.025f,needles);
                    else for(int branch=0;branch<6;branch++)
                    {
                        float angle=layer*2.399f+branch*Mathf.PI/3+kind*.8f;
                        var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                        var side=Vector3.Cross(direction,Vector3.up);
                        var root=new Vector3(0,y+.38f,0);
                        var tip=root+direction*radius-Vector3.up*(.45f+radius*.10f);
                        Cone(v,c,t,4,root,tip,.045f*(1-f)+.01f,.008f,bark);
                        // Flat needle-bearing boughs give spruce/fir a tapering,
                        // broken silhouette. Rounded broadleaf crown blobs made
                        // even northern forests read as generic deciduous trees.
                        for(int spray=0;spray<4;spray++)
                        {
                            float along=(spray+1)/4.5f;
                            var centre=Vector3.Lerp(root,tip,along);
                            float width=radius*(1-along)*.48f+.10f;
                            NeedleFan(v,c,t,centre,tip+direction*.18f,side,width,needles*(.88f+spray*.05f));
                        }
                    }
                }
                Cone(v,c,t,6,new Vector3(0,height-1.8f,0),Vector3.up*(height+.35f),.5f,.015f,new Color(.05f,.14f,.075f));
            }
            else
            {
                int clusters=kind==3?13:kind==4?9:11;
                for(int i=0;i<clusters;i++)
                {
                    float a=i*2.39996f+kind*0.7f,r=1.1f+Mathf.Repeat(i*0.73f,2.5f),y=6.5f+Mathf.Repeat(i*1.31f,5);
                    if(kind==4) {r*=0.6f;y+=1.3f;}
                    var center=new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);
                    if(detailed && i%3==0) Cone(v,c,t,5,new Vector3(0,y-3,0),center,0.12f,0.035f,bark);
                    Crown(v,c,t,center,kind==4?1.7f:2.0f+(i%3)*0.25f,detailed?5:3,sides,kind*19+i);
                }
            }
            // A shared lean and asymmetric crowns vary silhouettes; both LODs
            // retain the same crown centres so transitions preserve structure.
            for(int i=0;i<v.Count;i++) {var p=v[i];p.x+=Mathf.Sin(p.y*0.11f+kind)*p.y*0.035f;v[i]=p;}
            return Make(v,c,t,"Instanced tree "+kind+(detailed?" near":" far"));
        }
        private static void NeedleFan(List<Vector3> v,List<Color> c,List<int> triangles,Vector3 root,Vector3 tip,Vector3 side,float width,Color tint)
        {
            int start=v.Count;var middle=Vector3.Lerp(root,tip,.38f);
            v.Add(root);v.Add(middle+side*width-Vector3.up*.18f);v.Add(tip);
            v.Add(middle-side*width-Vector3.up*.18f);v.Add(middle+Vector3.up*.23f);
            foreach(int index in new[]{0,1,4,1,2,4,2,3,4,3,0,4})triangles.Add(start+index);
            for(int i=0;i<5;i++)c.Add(tint*(i==4?1.13f:.91f));
        }
        private static void Crown(List<Vector3> v,List<Color> c,List<int> triangles,Vector3 center,float radius,int rings,int sides,int seed)
        {
            int start=v.Count;
            for(int r=0;r<=rings;r++) for(int i=0;i<=sides;i++)
            {
                float f=(float)r/rings,a=(float)i/sides*Mathf.PI*2;
                float irregular=1+Mathf.Sin(a*3+seed)*0.13f+Mathf.Cos(a*5+f*8+seed)*0.09f;
                float radial=Mathf.Sin(f*Mathf.PI)*radius*irregular;
                v.Add(center+new Vector3(Mathf.Cos(a)*radial,Mathf.Cos(f*Mathf.PI)*radius*(0.9f+(seed%3)*0.16f),Mathf.Sin(a)*radial));
                float light=0.75f+(1-f)*0.28f;float hue=(seed%5)*0.006f;
                c.Add(new Color(0.06f+hue,0.16f+hue*1.3f,0.032f+hue*0.4f)*light);
            }
            for(int r=0;r<rings;r++) for(int i=0;i<sides;i++) {int a=start+r*(sides+1)+i,b=a+sides+1;triangles.Add(a);triangles.Add(a+1);triangles.Add(b);triangles.Add(a+1);triangles.Add(b+1);triangles.Add(b);}
            if(rings>=5)for(int j=0;j<32;j++)
            {
                float y=1-2*(j+.5f)/32,azimuth=j*2.39996f+seed,radial=Mathf.Sqrt(1-y*y);
                var outward=new Vector3(Mathf.Cos(azimuth)*radial,y,Mathf.Sin(azimuth)*radial);var along=Vector3.Cross(outward,Vector3.up).normalized;
                if(along.sqrMagnitude<.01f)along=Vector3.right;var root=center+outward*(radius*.95f);
                LeafBlade(v,c,triangles,root-along*.2f,root+along*(.32f+(j%3)*.05f),outward,.13f,new Color(.105f,.23f,.055f)*(j%3==0?1.18f:.9f));
            }
        }
        private static void LeafBlade(List<Vector3> vertices,List<Color> colors,List<int> triangles,Vector3 root,Vector3 tip,Vector3 normal,float width,Color color)
        {
            var across=Vector3.Cross(tip-root,normal).normalized*width;var centre=Vector3.Lerp(root,tip,.48f);int first=vertices.Count;
            vertices.Add(root);vertices.Add(centre+across);vertices.Add(tip);vertices.Add(centre-across);vertices.Add(centre+normal*width*.3f);
            foreach(int index in new[]{0,1,4,1,2,4,2,3,4,3,0,4})triangles.Add(first+index);
            for(int i=0;i<5;i++)colors.Add(color*(i==4?1.12f:1));
        }
        private static void Cone(List<Vector3> v,List<Color> c,List<int> triangles,int sides,Vector3 bottom,Vector3 top,float radius,float tip,Color color)
        {
            int start=v.Count;var rotation=Quaternion.FromToRotation(Vector3.up,(top-bottom).normalized);
            for(int ring=0;ring<2;ring++) for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides;float rad=(ring==0?radius:tip)*(1+Mathf.Sin(i*17.1f+bottom.y)*0.11f);
                v.Add((ring==0?bottom:top)+rotation*new Vector3(Mathf.Cos(a)*rad,0,Mathf.Sin(a)*rad));c.Add(color*(0.9f+(i%3)*0.06f));
            }
            for(int i=0;i<sides;i++) {int a=start+i,b=start+(i+1)%sides;triangles.Add(a);triangles.Add(a+sides);triangles.Add(b);triangles.Add(b);triangles.Add(a+sides);triangles.Add(b+sides);}
        }
        private static Mesh GroundCover(int kind)
        {
            if(kind==1)
            {
                var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
                for(int frond=0;frond<6;frond++)
                {
                    float angle=frond*2.39996f;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var cross=Vector3.Cross(direction,Vector3.up);
                    var root=Vector3.up*.03f;var tip=direction*(.55f+(frond%2)*.18f)+Vector3.up*(.32f+(frond%3)*.07f);
                    Cone(vertices,colors,triangles,3,root,tip,.012f,.003f,new Color(.18f,.29f,.045f));
                    for(int leaflet=1;leaflet<=5;leaflet++)for(int sign=-1;sign<=1;sign+=2)
                    {
                        float leafT=leaflet/6f;var p=Vector3.Lerp(root,tip,leafT)+Vector3.up*Mathf.Sin(leafT*Mathf.PI)*.18f;
                        var end=p+cross*sign*(.21f*(1-leafT)+.045f)+direction*.07f;
                        LeafBlade(vertices,colors,triangles,p,end,Vector3.up,.035f*(1-leafT)+.012f,new Color(.13f,.29f,.055f)*(1+leafT*.25f));
                    }
                }
                return Make(vertices,colors,triangles,"Instanced fern fronds and paired leaflets");
            }
            var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
            if(kind==2||kind==6)
            {
                // Mediterranean herbs and salt-tolerant shrubs have woody
                // branching and small leaves, rather than giant straw blades.
                var leaf=kind==2?new Color(.20f,.29f,.11f):new Color(.40f,.46f,.28f);
                for(int stem=0;stem<9;stem++)
                {
                    float angle=stem*2.39996f;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    var root=direction*.035f;var tip=direction*(.22f+(stem%3)*.065f)+Vector3.up*(.25f+(stem%4)*.085f);
                    Cone(v,c,t,4,root,tip,.014f,.003f,new Color(.23f,.18f,.09f));
                    for(int node=1;node<=4;node++)
                    {
                        float f=node/5f;var p=Vector3.Lerp(root,tip,f);var side=Vector3.Cross(direction,Vector3.up)*(node%2==0?1:-1);
                        var shoot=p+side*(.10f+(1-f)*.12f)+Vector3.up*.075f;
                        Cone(v,c,t,3,p,shoot,.006f,.001f,new Color(.25f,.22f,.12f));
                        LeafBlade(v,c,t,shoot-side*.07f,shoot+side*.08f,Vector3.up,.035f,leaf*(.85f+f*.3f));
                        LeafBlade(v,c,t,p,p+direction*.10f+Vector3.up*.08f,side,.025f,leaf);
                    }
                }
            }
            else if(kind==3)
            {
                for(int stem=0;stem<7;stem++)
                {
                    float angle=stem*2.39996f;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var root=direction*.22f;float height=1.2f+(stem%3)*.25f;
                    Cone(v,c,t,4,root,root+Vector3.up*height,.016f,.008f,new Color(.20f,.29f,.09f));
                    Cone(v,c,t,5,root+Vector3.up*(height-.12f),root+Vector3.up*(height+.08f),.035f,.022f,new Color(.24f,.16f,.07f));
                    for(int blade=0;blade<3;blade++){var p=root+Vector3.up*(.2f+blade*.24f);LeafBlade(v,c,t,p,p+direction*.45f+Vector3.up*.35f,Vector3.Cross(direction,Vector3.up),.04f,new Color(.17f,.29f,.07f));}
                }
            }
            else if(kind==5)
            {
                for(int patch=0;patch<32;patch++)
                {
                    float angle=patch*2.39996f,radius=.55f*Mathf.Sqrt((patch+.5f)/32);var root=new Vector3(Mathf.Cos(angle)*radius,.012f,Mathf.Sin(angle)*radius);
                    var tip=root+new Vector3(Mathf.Cos(angle+.7f),0,Mathf.Sin(angle+.7f))*.10f+Vector3.up*(.025f+(patch%3)*.008f);
                    LeafBlade(v,c,t,root,tip,Vector3.up,.035f,new Color(.15f,.25f,.055f)*(1+(patch%3)*.12f));
                }
            }
            else
            {
                int blades=kind==7?12:10;
                for(int blade=0;blade<blades;blade++)
                {
                    float angle=blade*2.39996f;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var across=Vector3.Cross(direction,Vector3.up);
                    float height=(kind==7?.55f:.24f)+(blade%4)*(kind==7?.09f:.045f),width=kind==7?.018f:.015f;
                    var root=direction*.035f;int first=v.Count;
                    for(int section=0;section<=3;section++)
                    {
                        float f=section/3f;var p=root+direction*(f*f*.28f)+Vector3.up*(Mathf.Sin(f*1.25f)*height);
                        v.Add(p-across*width*(1-f));v.Add(p+across*width*(1-f));var color=Color.Lerp(new Color(.12f,.23f,.035f),kind==7?new Color(.47f,.48f,.24f):new Color(.24f,.34f,.075f),f);
                        c.Add(color);c.Add(color*1.06f);if(section==0)continue;int previous=first+(section-1)*2,current=first+section*2;
                        t.Add(previous);t.Add(current);t.Add(previous+1);t.Add(previous+1);t.Add(current);t.Add(current+1);
                    }
                }
                if(kind==4)for(int flower=0;flower<5;flower++)
                {
                    float angle=flower*2.39996f;var root=new Vector3(Mathf.Cos(angle)*.28f,0,Mathf.Sin(angle)*.28f);var head=root+Vector3.up*(.32f+(flower%3)*.07f);
                    Cone(v,c,t,3,root,head,.007f,.003f,new Color(.16f,.27f,.045f));
                    for(int petal=0;petal<5;petal++){float a=petal*Mathf.PI*2/5;var tip=head+new Vector3(Mathf.Cos(a),.13f,Mathf.Sin(a))*.065f;LeafBlade(v,c,t,head,tip,Vector3.up,.023f,flower%2==0?new Color(.77f,.59f,.13f):new Color(.54f,.31f,.65f));}
                }
            }
            return Make(v,c,t,"Instanced habitat ground cover "+kind);
        }
        private static Mesh Rock()
        {
            var shape=new WorldShapes();shape.Boulder(new Vector3(-1,.6f,0),new Vector3(3.25f,1.65f,2.6f),new Color(.26f,.27f,.25f),31);shape.Boulder(new Vector3(2,.3f,1),new Vector3(2.25f,1.17f,1.8f),new Color(.3f,.3f,.27f),13);return shape.Build("Closed rocky outcrop");
        }
        private static Mesh Make(List<Vector3> v,List<Color> c,List<int> t,string name) {var mesh=new Mesh {name=name};mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        private void OnDestroy() {if(understoryWorker!=null)understoryWorker.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);for(int i=0;i<TreeVariants;i++) {if(near[i]!=null) Destroy(near[i]);if(far[i]!=null) Destroy(far[i]);}foreach(var mesh in cover) if(mesh!=null) Destroy(mesh);if(rock!=null) Destroy(rock);}
    }
}

