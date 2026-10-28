using System;
using System.IO;
using System.Linq;
using Aevoria.World;
using Aevoria.Persistence;
using Aevoria.Simulation;
static class LivingChecks
{
    static void Check(bool result,string name){if(!result)throw new Exception("FAILED: "+name);Console.WriteLine("PASS: "+name);}
    public static void Run()
    {
        var geo=new ProceduralGeography(GenerationSettings.Preset("ecosystem-005",WorldSize.Small));var eco=new EcologyModel(geo);
        Check(GenerationSettings.Preset("s",WorldSize.Miniature).WidthMeters==128000&&GenerationSettings.Preset("s",WorldSize.Colossal).WidthMeters==2048000,"exact 128 km and 2048 km presets preserve the 512 m chunk size");
        var water=new InlandWaterLayout(geo);Check(water.LakeCount>0&&water.LakeCount==geo.InlandSurfaces.LakeCount&&water.LakeCount<=geo.Hydrology.LakeCount,"accepted connected lake ownership reconstructs identically after rejected basins are breached");
        bool clear=true;for(int i=0;i<water.Rivers.Vertices.Count;i+=3){var a=water.Rivers.Vertices[i];var b=water.Rivers.Vertices[i+1];var c=water.Rivers.Vertices[i+2];clear&=!water.LakeAt((a.X+b.X+c.X)/3,(a.Z+b.Z+c.Z)/3,out _);}Check(clear,"river geometry does not own open lake surface");
        var cold=new ProceduralGeography(GenerationSettings.Preset("snow-save",WorldSize.Small),new ClimateSettings(-15,1,65));var coldEco=new EcologyModel(cold);var snow=new SeasonalModel(coldEco,"snow-save",0);snow.AdvanceTo(28);
        Check(Enumerable.Range(0,coldEco.Count).Any(i=>snow.Snow(i)>0&&snow.Snow(i)<.15&&snow.Ice(i)>0),"snow and ice accumulate progressively from a cold timeline");
        var copy=new SeasonalModel(coldEco,"snow-save",28,snow.Capture());snow.AdvanceTo(1120,1000);for(int i=8;i<=280;i++)copy.AdvanceTo(i*4,1);Check(ArchiveCodec.Hash(ArchiveCodec.Encode(snow.Capture()))==ArchiveCodec.Hash(ArchiveCodec.Encode(copy.Capture())),"snow and ice continue identically across saved state and different update cadence");
        var warm=new SeasonalModel(eco,geo.Configuration.Seed,0).Capture();for(int i=0;i<warm.Snow.Length;i++){warm.Snow[i]=1;warm.Ice[i]=.8f;}var thaw=new SeasonalModel(eco,geo.Configuration.Seed,0,warm);int temperate=Enumerable.Range(0,eco.Count).First(i=>eco.Temperature(i,28)>8);thaw.AdvanceTo(28);Check(thaw.Snow(temperate)>0&&thaw.Snow(temperate)<1&&thaw.Ice(temperate)<.8,"warmth melts existing snow and thaws ice over elapsed time");
        var world=new LivingWorldState(geo,eco,0);Check(world.Harvest("branch:test",ResourceKind.Wood,5,ToolKind.Hands,0)==5&&world.Traveler.Wood==5,"harvest credits only available resources");
        Check(world.Harvest("branch:test",ResourceKind.Wood,5,ToolKind.Hands,0)==0,"depleted resources cannot respawn through repeated queries");
        Check(world.Harvest("tree:0:0",ResourceKind.Wood,30,ToolKind.Axe,0)==0,"standing wood enforces its harvesting tool");world.Traveler.Tool=ToolKind.Axe;world.Harvest("tree:0:0",ResourceKind.Wood,30,ToolKind.Axe,0,30,true);Check(world.TreeDepleted(1,1,0)&&!world.TreeDepleted(1,1,1440*12),"standing resource depletion and time-based recovery are independent of streaming");
        world.Credit(ResourceKind.Wood,3);Check(world.Place(64,geo.ElevationAt(64,64),64,0)&&world.Objects.Count==1,"resource-funded test placement creates persistent object state");
        bool dug=false;string reason="";world.Traveler.Tool=ToolKind.Shovel;
        for(int z=-30;z<=30&&!dug;z++)for(int x=-30;x<=30&&!dug;x++){double px=x*32,pz=z*32,h=LocalSolidField.RenderedGround(geo,px,pz);if(world.SolidDensity(px,h-3,pz)<1)continue;dug=world.Dig(px,h,pz,ResourceKind.Soil,out reason);}
        Check(dug,"accessible solid excavation has a functional site");var edit=world.Excavations[0];var cut=edit.Cuts[0];var field=new VolumetricSolidField(geo,geo.Caves,edit.Cuts);
        Check(field.Density(cut.X,cut.Y,cut.Z)<0&&field.Density(cut.X,cut.Y-3,cut.Z)>0,"excavation subtracts underground volume while retaining solid ground below");
        var mesh=new SolidGeometry();for(int z=(int)Math.Floor((cut.Z-3)/16);z<=(int)Math.Floor((cut.Z+3)/16);z++)for(int y=(int)Math.Floor((cut.Y-4)/16);y<=(int)Math.Floor((cut.Y+4)/16);y++)for(int x=(int)Math.Floor((cut.X-3)/16);x<=(int)Math.Floor((cut.X+3)/16);x++){var part=new VolumetricSolidField(geo,geo.Caves,edit.Cuts).Build(x,y,z);int offset=mesh.Vertices.Count;mesh.Vertices.AddRange(part.Vertices);mesh.Normals.AddRange(part.Normals);mesh.SurfaceDepth.AddRange(part.SurfaceDepth);mesh.Indices.AddRange(part.Indices.Select(i=>i+offset));}Check(mesh.Vertices.Count>0&&mesh.Indices.All(i=>i>=0&&i<mesh.Vertices.Count)&&mesh.Vertices.All(p=>double.IsFinite(p.X)&&double.IsFinite(p.Y)&&double.IsFinite(p.Z)),"organic solid reconstruction produces finite bounded triangles");
        Check(mesh.SurfaceDepth.Count==mesh.Vertices.Count&&mesh.SurfaceDepth.All(v=>float.IsFinite(v)&&v>=0)&&mesh.SurfaceDepth.Any(v=>v>.3f),"exposed subsurface depth follows the actual solid mesh for soil and strata materials");
        Check(ClosedRim(mesh,cut),"excavated rim has paired interior edges with opposite winding");
        Check(world.Dig(edit.X*32+15,cut.Y,edit.Z*32,ResourceKind.Soil,out _),"excavation continues across former patch borders with persistent sparse cuts");
        var restored=new LivingWorldState(geo,eco,0,world.Capture());Check(restored.Excavations.Count==1&&restored.Excavations[0].Cuts.Count==2&&restored.Objects.Count==1&&restored.Available("branch:test",5,0,false)==0&&restored.Traveler.Wood==world.Traveler.Wood,"resources, inventory, edits and placed objects survive an authoritative snapshot");
        var clock=new SimulationClock(new WorldState(geo.Configuration.Seed));var archive=WorldRecord.Capture(null,"Living archive",null,geo,clock,"0.0.6",eco,world);var record=ArchiveCodec.Unpack(ArchiveCodec.Pack(archive));Check(ArchiveCodec.Hash(ArchiveCodec.Encode(archive.Living))==ArchiveCodec.Hash(ArchiveCodec.Encode(record.Living)),"full archive retains sparse world edits and seasonal state");
        var invalid=world.Capture();invalid.Traveler.Wood=-1;bool rejected=false;try{LivingWorldState.Validate(invalid,geo.Configuration,0,eco.Resolution);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid inventory and persistent state are rejected before native installation");
        var falling=world.Capture();falling.Traveler.Airborne=true;falling.Traveler.VelocityX=240.125;falling.Traveler.VelocityY=-73.5;
        var fallingCopy=LivingWorldState.Clone(ArchiveCodec.Decode<LivingWorldSnapshot>(ArchiveCodec.Encode(falling)));
        LivingWorldState.Validate(fallingCopy,geo.Configuration,0,eco.Resolution);
        Check(fallingCopy.Traveler.Airborne&&fallingCopy.Traveler.VelocityX==240.125&&fallingCopy.Traveler.VelocityY==-73.5,"freefall momentum survives archive serialization and deep snapshot cloning");
        fallingCopy.Traveler.VelocityZ=double.NaN;rejected=false;try{LivingWorldState.Validate(fallingCopy,geo.Configuration,0,eco.Resolution);}catch(ArgumentException){rejected=true;}
        Check(rejected,"non-finite airborne momentum is rejected before saving");
        var high=world.Capture();high.Traveler.HasPosition=true;high.Traveler.Y=16001;LivingWorldState.Validate(high,geo.Configuration,0,eco.Resolution);Check(true,"traveler above ten kilometres remains saveable with supported mountain relief");
        high.Traveler.X=1023000;var colossal=GenerationSettings.Preset(geo.Configuration.Seed,WorldSize.Colossal);LivingWorldState.Validate(high,colossal,0,eco.Resolution);Check(true,"Colossal outer 24 km is inside the archive's world-derived traveler bounds");
        high.Traveler.X=1024001;rejected=false;try{LivingWorldState.Validate(high,colossal,0,eco.Resolution);}catch(ArgumentException ex){rejected=ex.Message.Contains("boundary");}Check(rejected,"out-of-world traveler fails with an actionable boundary diagnostic");
        high.Traveler.X=0;high.Traveler.Y=double.NaN;rejected=false;try{LivingWorldState.Validate(high,colossal,0,eco.Resolution);}catch(ArgumentException ex){rejected=ex.Message.Contains("non-finite");}Check(rejected,"non-finite traveler remains invalid with a distinct diagnostic");
        string root=Path.Combine(Path.GetTempPath(),"Aevoria006-Portable-"+Guid.NewGuid().ToString("N"));try
        {
            archive.Schema=4;archive.Generator=4;var bytes=ArchiveCodec.Pack(archive);string folder=Path.Combine(root,archive.Id);Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"world.xml"),bytes);
            bool incompatible=false;try{new WorldRepository(root).LoadAsync(archive.Id).GetAwaiter().GetResult();}catch(NotSupportedException){incompatible=true;}
            Check(incompatible&&bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(folder,"world.xml"))),"previous generator archives are rejected and original bytes preserved");
            rejected=false;try{new WorldRepository(root).UpgradeCopyAsync(archive.Id).GetAwaiter().GetResult();}catch(NotSupportedException){rejected=true;}Check(rejected&&Directory.GetDirectories(root).Length==1,"unsupported geography cannot be silently upgraded into a new world");
        }finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        Console.WriteLine($"Water regression: {water.LakeCount} accepted, {water.RejectedBasins} rejected; {water.Rivers.Indices.Count/3} river triangles; {water.Lakes.Indices.Count/3} lake triangles; solid vertices {mesh.Vertices.Count}.");
    }
    static bool ClosedRim(SolidGeometry mesh,DigCut cut)
    {
        var edges=new System.Collections.Generic.Dictionary<string,(int count,int direction)>();
        string Key(SolidPoint p)=>Math.Round(p.X*1000000)+":"+Math.Round(p.Y*1000000)+":"+Math.Round(p.Z*1000000);
        double Distance(SolidPoint p)=>Math.Max(Math.Abs(p.X-cut.X),Math.Abs(p.Z-cut.Z));
        for(int i=0;i<mesh.Indices.Count;i+=3)
        {
            var a=mesh.Vertices[mesh.Indices[i]];var b=mesh.Vertices[mesh.Indices[i+1]];var c=mesh.Vertices[mesh.Indices[i+2]];
            if(Distance(a)>3&&Distance(b)>3&&Distance(c)>3)continue;
            Edge(a,b);Edge(b,c);Edge(c,a);
        }
        void Edge(SolidPoint a,SolidPoint b)
        {
            if(Distance(a)>2||Distance(b)>2)return;
            string x=Key(a),y=Key(b);if(x==y)return;bool forward=string.CompareOrdinal(x,y)<0;string key=forward?x+"/"+y:y+"/"+x;
            edges.TryGetValue(key,out var e);edges[key]=(e.count+1,e.direction+(forward?1:-1));
        }
        return edges.Count>0&&edges.Values.All(e=>e.count==2&&e.direction==0);
    }
}
