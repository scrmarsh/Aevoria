using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aevoria.World;

internal static class CoastChecks
{
    public static void FindFjord(string output)
    {
        for(int attempt=0;attempt<64;attempt++)
        {
            string seed="fjord-coast-013-"+attempt;var config=GenerationSettings.Preset(seed,WorldSize.Small,new GenerationSettings(landmass:LandmassType.Continents));
            var land=new ContinentalField(config,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Continents,ContinentalField.Version));
            var geology=new GeologyMap(config,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Geology,GeologyMap.Version));
            var tectonics=new TectonicStructureField(config,land,geology,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Tectonics,TectonicStructureField.Version));
            var ordinary=new BaseElevationField(config,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Elevation,BaseElevationField.Version),tectonics);
            var mountains=new MountainClusterField(config,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Mountains,MountainClusterField.Version),tectonics:tectonics);
            var coast=new CoastalLandformField(config,land,ordinary,mountains,tectonics,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Coasts,CoastalLandformField.Version));
            int fjords=coast.Inlets.Count(i=>i.Kind==CoastalInletKind.Fjord);Console.WriteLine(seed+": "+fjords+" qualifying fjords");
            if(fjords==0)continue;
            File.WriteAllText(output,JsonSerializer.Serialize(new{seed,size="Small",relief=3600,fjords,continents=ContinentalField.Version,coasts=CoastalLandformField.Version,baseElevation=BaseElevationField.Version},new JsonSerializerOptions{WriteIndented=true}));return;
        }
        throw new Exception("No supported northern mountain coast fixture among64 seeds");
    }
    public static void Run(string output)
    {
        var results=new List<object>();int checks=0;
        foreach(var size in new[]{WorldSize.Miniature,WorldSize.Colossal})
        foreach(var arrangement in new[]{LandmassType.SingleContinent,LandmassType.Continents,LandmassType.Archipelago,LandmassType.Mixed})
        foreach(float target in new[]{.30f,.54f,.85f})
        {
            string seed="shaping-continents";
            var config=GenerationSettings.Preset(seed,size,new GenerationSettings(targetLandFraction:target,landmass:arrangement));
            var field=new ContinentalField(config,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Continents,ContinentalField.Version));
            const int n=257;var land=new bool[n*n];int occupied=0;double closestEdge=double.MaxValue;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                double e=((x+.5)/n-.5)*config.WidthMeters,north=((z+.5)/n-.5)*config.DepthMeters,d=field.CoastDistanceAt(e,north);
                if(!double.IsFinite(d))throw new Exception("Nonfinite coast distance: "+arrangement);
                if(d>0){land[z*n+x]=true;occupied++;closestEdge=Math.Min(closestEdge,Math.Min(Math.Min(x,n-1-x),Math.Min(z,n-1-z)));}
            }
            double fraction=occupied/(double)(n*n);
            if(Math.Abs(fraction-target)>.018)throw new Exception($"Coverage {size}/{arrangement}/{target}: {fraction}");checks++;
            if(closestEdge<1)throw new Exception("Land touches finite world boundary");checks++;
            var components=new List<int>();var seen=new bool[land.Length];
            for(int i=0;i<land.Length;i++)if(land[i]&&!seen[i])
            {
                int area=0;var q=new Queue<int>();q.Enqueue(i);seen[i]=true;
                while(q.Count>0){int p=q.Dequeue(),x=p%n,z=p/n;area++;foreach(int j in new[]{x>0?p-1:-1,x<n-1?p+1:-1,z>0?p-n:-1,z<n-1?p+n:-1})if(j>=0&&land[j]&&!seen[j]){seen[j]=true;q.Enqueue(j);}}
                components.Add(area);
            }
            components.Sort((a,b)=>b.CompareTo(a));
            if(arrangement==LandmassType.Continents&&target==.54f&&components.Count(a=>a>n*n*.03)<2)throw new Exception("Separate continental provinces have merged");
            checks++;results.Add(new{size=size.ToString(),arrangement=arrangement.ToString(),target,actual=fraction,edgeCells=closestEdge,landComponents=components.Count,majorAreas=components.Take(8).Select(a=>a/(double)(n*n)).ToArray()});
            Console.WriteLine($"PASS {size} {arrangement} target{target:F2} actual{fraction:F4} components{components.Count}");
        }
        var inletResults=new List<object>();int totalFjords=0;
        foreach(string seed in new[]{"shaping-continents","aevoria-001","mediterranean-013","fjord-coast-013-7"})
        {
            var config=GenerationSettings.Preset(seed,WorldSize.Small,new GenerationSettings(landmass:LandmassType.Continents));
            var land=new ContinentalField(config,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Continents,ContinentalField.Version));
            var geology=new GeologyMap(config,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Geology,GeologyMap.Version));
            var tectonics=new TectonicStructureField(config,land,geology,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Tectonics,TectonicStructureField.Version));
            var ordinary=new BaseElevationField(config,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Elevation,BaseElevationField.Version),tectonics);
            var mountains=new MountainClusterField(config,land,WorldGenerationSeeds.Derive(seed,WorldLayerIds.Mountains,MountainClusterField.Version),tectonics:tectonics);
            ulong moduleSeed=WorldGenerationSeeds.Derive(seed,WorldLayerIds.Coasts,CoastalLandformField.Version);
            var coast=new CoastalLandformField(config,land,ordinary,mountains,tectonics,moduleSeed);
            var repeat=new CoastalLandformField(config,land,ordinary,mountains,tectonics,moduleSeed);
            var surface=new ComposedSurfaceField(config,ordinary,mountains,coast);
            using(var cancel=new System.Threading.CancellationTokenSource())
            {
                bool stopped=false;try{new CoastalLandformField(config,land,ordinary,mountains,tectonics,moduleSeed,cancel.Token,(detail,done,total)=>{if(done==8)cancel.Cancel();});}catch(OperationCanceledException){stopped=true;}
                if(!stopped)throw new Exception("Coastal preparation did not cancel during work");checks++;
            }
            if(coast.Inlets.Count!=repeat.Inlets.Count)throw new Exception("Coastal features are nondeterministic");checks++;
            int fjords=0;
            foreach(var inlet in coast.Inlets)
            {
                if(inlet.Kind==CoastalInletKind.Fjord)
                {
                    fjords++;if(inlet.Points[1].Z<config.DepthMeters*.18)throw new Exception("Fjord outside northern glacial zone");checks++;
                    var mid=inlet.Points[6];var before=inlet.Points[5];var after=inlet.Points[7];double dx=after.X-before.X,dz=after.Z-before.Z,l=Math.Sqrt(dx*dx+dz*dz);dx/=l;dz/=l;
                    double left=mountains.ContributionAt(mid.X-dz*inlet.Width*2.5,mid.Z+dx*inlet.Width*2.5),right=mountains.ContributionAt(mid.X+dz*inlet.Width*2.5,mid.Z-dx*inlet.Width*2.5);
                    if(Math.Min(left,right)<120||Math.Max(left,right)<450)throw new Exception("Fjord is a trench through flat coastal land");checks++;
                }
                var p=inlet.Points[5];double original=ordinary.ElevationAt(p.X,p.Z)+mountains.ContributionAt(p.X,p.Z),cut=surface.ElevationAt(p.X,p.Z);
                if(cut>=0||cut>original+.001)throw new Exception("Inlet floor is not physically submerged");checks++;
                if(inlet.Kind==CoastalInletKind.Cove)
                {
                    var a=inlet.Points[4];var b=inlet.Points[6];double dx=b.X-a.X,dz=b.Z-a.Z,l=Math.Sqrt(dx*dx+dz*dz);dx/=l;dz/=l;
                    if(surface.ElevationAt(p.X-dz*inlet.Width*.75,p.Z+dx*inlet.Width*.75)>=0||surface.ElevationAt(p.X+dz*inlet.Width*.75,p.Z-dx*inlet.Width*.75)>=0)throw new Exception("Cove lacks a broad submerged bay floor");checks++;
                }
                for(int i=0;i<8;i++)
                {
                    var a=inlet.Points[i];var b=inlet.Points[i+1];
                    for(int t=0;t<=4;t++){double x=a.X+(b.X-a.X)*t/4,z=a.Z+(b.Z-a.Z)*t/4;if(surface.ElevationAt(x,z)>=0)throw new Exception("Inlet has a sealed mouth or dry gap");}
                }
                checks++;
                if(surface.ElevationAt(p.X,p.Z)!=coast.Shape(p.X,p.Z,(float)original)||coast.Shape(p.X,p.Z,(float)original)!=repeat.Shape(p.X,p.Z,(float)original))throw new Exception("Authoritative coastal composition is not deterministic");checks++;
            }
            totalFjords+=fjords;inletResults.Add(new{seed,inlets=coast.Inlets.Count,fjords});Console.WriteLine($"PASS Physical coastal beds {seed}: {coast.Inlets.Count} inlets, {fjords} fjords");
        }
        if(totalFjords==0)throw new Exception("No northern mountainous fjord fixture found");checks++;
        File.WriteAllText(output,JsonSerializer.Serialize(new{generator=ProceduralGeography.GeneratorVersion,modules=WorldGenerationIdentity.ModuleVersions,checks,results,inletResults},new JsonSerializerOptions{WriteIndented=true}));
    }
}
