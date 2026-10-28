using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevoria.World;
using Aevoria.Persistence;
using Aevoria.Simulation;
static class CaveChecks
{
    public static void Run()
    {
        var geo=new ProceduralGeography(GenerationSettings.Preset("shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents)));
        var network=new CaveNetwork(geo);var repeat=new CaveNetwork(geo);CaveSystem system=null;int selectedX=0,selectedZ=0;
        for(int z=-16;z<=16&&system==null;z++)for(int x=-16;x<=16;x++){var candidate=network.SystemAt(x,z);if(!candidate.Active||!candidate.HillsideEntrance)continue;system=candidate;selectedX=x;selectedZ=z;break;}
        if(system==null)throw new Exception("No supported natural cave system");
        var seen=new HashSet<int>{0};var queue=new Queue<int>();queue.Enqueue(0);
        while(queue.Count>0){int node=queue.Dequeue();foreach(var edge in system.Connections){int other=edge.A==node?edge.B:edge.B==node?edge.A:-1;if(other>=0&&seen.Add(other))queue.Enqueue(other);}}
        if(seen.Count!=system.Nodes.Count||system.Connections.Count-system.Nodes.Count+1<3)throw new Exception("Cave loops are not actually connected");
        Console.WriteLine("PASS: connected cave graph contains cycles between different depths");
        double low=system.Nodes.Min(p=>p.Y),high=system.Nodes.Max(p=>p.Y);if(high-low<60||system.Shafts<2||system.Chambers.Count<4)throw new Exception("Missing three-dimensional cave structure");
        foreach(var passage in system.Passages){var p=SolidPoint.Mix(passage.A,passage.B,.5);if(system.Distance(p.X,p.Y,p.Z)>=0)throw new Exception("Passage centre is solid");}
        Console.WriteLine("PASS: passages, caverns and shafts have real signed underground air volume");
        var entry=system.Entrance;double surface=LocalSolidField.RenderedGround(geo,entry.X,entry.Z);if(system.Distance(entry.X,surface,entry.Z)>=0)throw new Exception("Cave does not open through actual terrain");
        Console.WriteLine("PASS: hillside entry cuts through the actual triangular ground surface");
        string Identity(CaveSystem s)=>string.Join(";",s.Passages.Select(p=>FormattableString.Invariant($"{p.A.X:R}/{p.A.Y:R}/{p.A.Z:R}:{p.B.X:R}/{p.B.Y:R}/{p.B.Z:R}:{p.Radius:R}:{p.VerticalScale:R}")));
        string identity=Identity(system);for(int z=-96;z<96;z++)network.SystemAt(z,21);
        if(network.CachedSystems>CaveNetwork.CacheCapacity||Identity(network.SystemAt(selectedX,selectedZ))!=identity||Identity(repeat.SystemAt(selectedX,selectedZ))!=identity)throw new Exception("Cave regeneration depends on cache visitation");
        Parallel.For(0,128,i=>{var actual=repeat.SystemAt(selectedX,selectedZ);var p=actual.Nodes[i%actual.Nodes.Count];if(actual.Distance(p.X,p.Y,p.Z)!=system.Distance(p.X,p.Y,p.Z))throw new Exception("Threaded cave query differs");});
        Console.WriteLine("PASS: immutable cave systems agree after independent and concurrent reconstruction");
        var room=system.Chambers[0];int cx=(int)Math.Floor(room.Centre.X/16),cy=(int)Math.Floor((room.Centre.Y-room.Radii.Y)/16),cz=(int)Math.Floor(room.Centre.Z/16);
        var timer=System.Diagnostics.Stopwatch.StartNew();var natural=new VolumetricSolidField(geo,network).Build(cx,cy,cz);double naturalMs=timer.Elapsed.TotalMilliseconds;
        if(natural.Indices.Count<30||natural.Normals.All(n=>n.Y<.5)||natural.SurfaceDepth.Min()<1)throw new Exception("Missing real underground cave floor mesh");
        Console.WriteLine($"PASS: meshed natural cave floor has {natural.Vertices.Count} vertices / {natural.Indices.Count/3} triangles ({naturalMs:F2} ms worker preparation)");
        double boundary=(cx+1)*16.0,cutZ=cz*16+8,cutY=LocalSolidField.RenderedGround(geo,boundary,cutZ)-20;int cutChunkY=(int)Math.Floor(cutY/16);
        var edits=new[]{new DigCut{X=boundary,Y=cutY,Z=cutZ,Radius=1.2,Material=ResourceKind.Stone}};var solidField=new VolumetricSolidField(geo,null,edits);
        if(solidField.Density(boundary,cutY,cutZ)>=0||solidField.Density(boundary,cutY+3,cutZ)<=0)throw new Exception("Excavation is not a closed three-dimensional cavity");
        var left=solidField.Build(cx,cutChunkY,cz);var right=solidField.Build(cx+1,cutChunkY,cz);
        Dictionary<(long,long),SolidPoint> Boundary(SolidGeometry mesh){var result=new Dictionary<(long,long),SolidPoint>();for(int i=0;i<mesh.Vertices.Count;i++){var p=mesh.Vertices[i];if(Math.Abs(p.X-boundary)<1e-7)result[((long)Math.Round(p.Y*1000000),(long)Math.Round(p.Z*1000000))]=mesh.Normals[i];}return result;}
        var a=Boundary(left);var b=Boundary(right);if(a.Count<8||a.Count!=b.Count||a.Keys.Any(k=>!b.ContainsKey(k)))throw new Exception("Excavation chunk boundary does not share exact surface vertices");
        foreach(var pair in a){var n=b[pair.Key];double difference=Math.Abs(pair.Value.X-n.X)+Math.Abs(pair.Value.Y-n.Y)+Math.Abs(pair.Value.Z-n.Z);if(difference>1e-6)throw new Exception("Shared excavation normals disagree");}
        if(left.Normals.All(n=>n.Y>-.5)||left.Normals.All(n=>n.Y<.5))throw new Exception("Excavation mesh lacks roof or floor");
        Console.WriteLine("PASS: persistent spherical subtraction has a real roof/floor and identical mesh boundaries/normals across chunks");
        using(var cancelled=new System.Threading.CancellationTokenSource()){cancelled.Cancel();bool rejected=false;try{solidField.Build(cx,cy,cz,cancelled.Token);}catch(OperationCanceledException){rejected=true;}if(!rejected)throw new Exception("Volume preparation cannot cancel");}
        Console.WriteLine("PASS: streamed volume preparation observes cancellation");
        var clipped=new List<SolidPoint>();var clipIndices=new List<int>();
        HeightfieldSupportClipper.AppendOutside(new SolidPoint(0,15,0),new SolidPoint(1,17,0),new SolidPoint(0,17,1),(x,y,z)=>y==0,clipped,clipIndices);
        double Area(List<SolidPoint> points){double area=0;for(int i=0;i<points.Count;i+=3){var p=points[i];var q=points[i+1];var r=points[i+2];double ax=q.X-p.X,ay=q.Y-p.Y,az=q.Z-p.Z,bx=r.X-p.X,by=r.Y-p.Y,bz=r.Z-p.Z,nx=ay*bz-az*by,ny=az*bx-ax*bz,nz=ax*by-ay*bx;area+=Math.Sqrt(nx*nx+ny*ny+nz*nz)*.5;}return area;}
        if(clipped.Any(p=>p.Y<16-1e-9)||Math.Abs(Area(clipped)-1.125)>1e-8)throw new Exception("Support does not clip exactly to installed vertical ownership");
        clipped.Clear();clipIndices.Clear();HeightfieldSupportClipper.AppendOutside(new SolidPoint(0,100,0),new SolidPoint(1,100,0),new SolidPoint(0,100,1),(x,y,z)=>y==0,clipped,clipIndices);
        if(Math.Abs(Area(clipped)-.5)>1e-9)throw new Exception("Underground ownership removed ground above it");
        Console.WriteLine("PASS: support clipping removes only owned volume and preserves unrelated ground above caves");
        string output=Environment.GetEnvironmentVariable("AEVORIA_VALIDATION_OUTPUT")??throw new Exception("Explicit v0.0.9 evidence directory required");Directory.CreateDirectory(output);
        var ecology=new EcologyModel(geo);var living=new LivingWorldState(geo,ecology,0);living.Traveler.Tool=ToolKind.Pickaxe;
        double editX=527.5,editZ=527.5;while(geo.Caves.At(editX,editZ).Active)editX+=2048;double originalHeight=LocalSolidField.RenderedGround(geo,editX,editZ);
        for(int i=0;i<48;i++)if(!living.Dig(editX,originalHeight-40-i,editZ,ResourceKind.Stone,out string reason))throw new Exception("Deep shaft rejected: "+reason);
        for(int i=0;i<20;i++){double px=editX+64*(i+1),pz=editZ+64,py=LocalSolidField.RenderedGround(geo,px,pz)-500;if(!living.Dig(px,py,pz,ResourceKind.Stone,out string reason))throw new Exception("Sparse distant excavation rejected: "+reason);}
        var clock=new SimulationClock(new WorldState(geo.Configuration.Seed));var record=WorldRecord.Capture(null,"Underground persistence",null,geo,clock,"0.0.9",ecology,living);var repository=new WorldRepository(Path.Combine(output,"Worlds"));repository.SaveAsync(record,false).GetAwaiter().GetResult();var loaded=repository.LoadAsync(record.Id).GetAwaiter().GetResult();var restored=new LivingWorldState(geo,ecology,0,loaded.Record.Living);
        for(int i=0;i<48;i++){double py=originalHeight-40-i;if(living.SolidDensity(editX,py,editZ)!=restored.SolidDensity(editX,py,editZ)||restored.SolidDensity(editX,py,editZ)>=0)throw new Exception("Deep edited solid changed across archive reload");}
        if(restored.Excavations.Count<21||restored.Excavations.Max(s=>s.Cuts.Count)<48)throw new Exception("Old excavation budget remains");
        Console.WriteLine("PASS: more than 16 sites and 32 cuts, patch borders and deep edits survive a real archive round trip");
        File.WriteAllText(Path.Combine(output,"cave-field.json"),System.Text.Json.JsonSerializer.Serialize(new{seed=geo.Configuration.Seed,cellX=selectedX,cellZ=selectedZ,nodes=system.Nodes.Count,connections=system.Connections.Count,cycles=system.Connections.Count-system.Nodes.Count+1,passages=system.Passages.Count,chambers=system.Chambers.Count,shafts=system.Shafts,ravines=system.Ravines,verticalSpan=high-low,entryX=entry.X,entryY=entry.Y,entryZ=entry.Z,cachedSystems=network.CachedSystems,naturalVertices=natural.Vertices.Count,naturalTriangles=natural.Indices.Count/3,naturalPreparationMs=naturalMs,matchedEditBoundaryVertices=a.Count},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
    }
}
