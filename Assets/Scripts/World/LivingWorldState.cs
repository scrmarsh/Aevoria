using System;
using System.Collections.Generic;

namespace Aevoria.World
{
    public enum ResourceKind { Wood,Stone,Soil,Sand }
    public enum ToolKind { Hands,Shovel,Pickaxe,Axe,Stick,Crate }
    public enum NaturalKind { Branch,Log,Boulder,Shrub,Flowers,DryPlant }
    [Serializable] public sealed class ResourceChange {public string Id;public double Remaining;public long Tick;}
    [Serializable] public sealed class DigCut {public double X,Y,Z,Radius;public ResourceKind Material;}
    [Serializable] public sealed class ExcavationSite {public int X,Z;public double Top,Bottom;public List<DigCut> Cuts=new List<DigCut>();}
    [Serializable] public sealed class PlacedObject {public long Id;public double X,Y,Z,Yaw;}
    [Serializable] public sealed class TravelerState
    {
        public string CharacterPreset=CharacterProfiles.Explorer;
        public bool HasPosition,Exploring,FirstPerson,ReverseThirdPerson,Crouched,Flying,Airborne;
        // Optional archive fields: absent in earlier saves and default to rest.
        public double VelocityX,VelocityY,VelocityZ;
        public double X,Y,Z,Yaw,Pitch;public ToolKind Tool=ToolKind.Shovel;
        public int Wood,Stone,Soil,Sand;
    }
    [Serializable] public sealed class LivingWorldSnapshot
    {
        public int Version=3;public string Seed;
        public SeasonalSnapshot Seasons;public AtmosphericSnapshot Atmosphere;
        public TravelerState Traveler=new TravelerState();
        public List<ResourceChange> Resources=new List<ResourceChange>();
        public List<ExcavationSite> Excavations=new List<ExcavationSite>();
        public List<PlacedObject> Objects=new List<PlacedObject>();
        public long NextObjectId=1;
    }
    public readonly struct NaturalSite
    {
        public readonly string Id;public readonly NaturalKind Kind;public readonly GeoPoint Position;
        public readonly float Height,Scale,Yaw;public readonly ResourceKind Resource;public readonly double Quantity;
        public NaturalSite(string id,NaturalKind kind,GeoPoint p,float height,float scale,float yaw,ResourceKind resource,double quantity)
        {Id=id;Kind=kind;Position=p;Height=height;Scale=scale;Yaw=yaw;Resource=resource;Quantity=quantity;}
    }
    // Sparse changes outlive visual chunks. Recoverable resources use world time
    // in a pure availability query; visiting a site does not advance its growth.
    public sealed class LivingWorldState
    {
        public const int MaximumResources=8192,MaximumObjects=128;
        // Includes supported 16 km mountain relief and exploration flight.
        // Horizontal bounds come from the world, including Colossal's 1,024 km half-width.
        public const double MaximumTravelerAltitude=50000;
        private readonly ProceduralGeography geo;private readonly EcologyModel ecology;
        private readonly LivingWorldSnapshot state;
        private readonly Dictionary<string,ResourceChange> changes=new Dictionary<string,ResourceChange>();
        private readonly Dictionary<long,ResourceChange> treeChanges=new Dictionary<long,ResourceChange>();
        private readonly Dictionary<(int,int),ExcavationSite> editSites=new Dictionary<(int,int),ExcavationSite>();
        private readonly Dictionary<(int,int,int),List<DigCut>> editChunks=new Dictionary<(int,int,int),List<DigCut>>();
        public bool HasResourceChanges=>changes.Count>0;
        public SeasonalModel Seasons {get;}
        public RegionalAtmosphere Atmosphere {get;}
        public TravelerState Traveler=>state.Traveler;
        public int Revision {get;private set;}
        public int ExcavationRevision {get;private set;}
        public IReadOnlyList<ExcavationSite> Excavations=>state.Excavations;
        public IReadOnlyList<PlacedObject> Objects=>state.Objects;
        public LivingWorldState(ProceduralGeography geo,EcologyModel ecology,long tick,LivingWorldSnapshot saved=null)
        {
            this.geo=geo;this.ecology=ecology;
            if(saved!=null){Validate(saved,geo.Configuration,tick,ecology.Resolution);state=Clone(saved);}
            else state=new LivingWorldSnapshot{Seed=geo.Configuration.Seed};
            Atmosphere=new RegionalAtmosphere(geo,ecology,tick,state.Atmosphere);Seasons=new SeasonalModel(ecology,geo.Configuration.Seed,tick,state.Seasons,Atmosphere);
            foreach(var r in state.Resources){changes.Add(r.Id,r);IndexTree(r);}
            foreach(var site in state.Excavations){editSites.Add((site.X,site.Z),site);foreach(var cut in site.Cuts)IndexCut(cut);}
        }
        public static void Validate(LivingWorldSnapshot s,WorldConfiguration config,long tick,int resolution)
        {
            if(s==null||s.Version!=3)throw new NotSupportedException("Unsupported living-world state.");
            if(s.Seed!=config.Seed||s.Resources==null||s.Resources.Count>MaximumResources||s.Excavations==null||s.Objects==null||s.Objects.Count>MaximumObjects||s.Traveler==null||s.NextObjectId<1)throw new ArgumentException("Invalid sparse world state.");
            SeasonalModel.Validate(s.Seasons,config.Seed,resolution,tick);RegionalAtmosphere.Validate(s.Atmosphere,config.Seed,resolution,tick);var ids=new HashSet<string>();
            foreach(var r in s.Resources)if(r==null||string.IsNullOrEmpty(r.Id)||r.Id.Length>80||!ids.Add(r.Id)||!Finite(r.Remaining)||r.Remaining<0||r.Remaining>10000||r.Tick<0||r.Tick>tick)throw new ArgumentException("Invalid resource modification.");
            var cells=new HashSet<string>();foreach(var site in s.Excavations)
            {
                if(site==null||!cells.Add(site.X+":"+site.Z)||site.Cuts==null||site.Cuts.Count==0||!config.Contains(new GeoPoint((double)site.X*32,(double)site.Z*32))||!Finite(site.Top)||!Finite(site.Bottom)||Math.Abs(site.Top)>MaximumTravelerAltitude||Math.Abs(site.Bottom)>MaximumTravelerAltitude||site.Top<site.Bottom)throw new ArgumentException("Invalid excavation site.");
                foreach(var cut in site.Cuts)if(cut==null||!Finite(cut.X)||!Finite(cut.Y)||!Finite(cut.Z)||!Finite(cut.Radius)||cut.Radius<.1||cut.Radius>4||Math.Abs(cut.X-site.X*32)>16.001||Math.Abs(cut.Z-site.Z*32)>16.001||cut.Y-cut.Radius<site.Bottom-.001||cut.Y+cut.Radius>site.Top+.001||!config.Contains(new GeoPoint(cut.X,cut.Z))||!Enum.IsDefined(typeof(ResourceKind),cut.Material)||cut.Material==ResourceKind.Wood)throw new ArgumentException("Unsupported excavation bounds.");
            }
            var p=s.Traveler;
            if(!CharacterProfiles.Valid(p.CharacterPreset))throw new ArgumentException("Traveler has an unsupported character preset.");
            if(!Finite(p.X)||!Finite(p.Y)||!Finite(p.Z)||!Finite(p.Yaw)||!Finite(p.Pitch))throw new ArgumentException("Traveler position or view contains a non-finite number. Use Recover to nearby surface before saving.");
            if(!Finite(p.VelocityX)||!Finite(p.VelocityY)||!Finite(p.VelocityZ)||Math.Abs(p.VelocityX)>2000||Math.Abs(p.VelocityY)>2000||Math.Abs(p.VelocityZ)>2000)throw new ArgumentException("Traveler velocity exceeds the supported motion range.");
            if(p.HasPosition&&!config.Contains(new GeoPoint(p.X,p.Z)))throw new ArgumentException($"Traveler is outside this world's {config.WidthMeters/1000:0} × {config.DepthMeters/1000:0} km boundary. Return inside the world before saving.");
            if(Math.Abs(p.Y)>MaximumTravelerAltitude)throw new ArgumentException("Traveler altitude exceeds the supported 50 km exploration range. Return toward the surface before saving.");
            if(p.Wood<0||p.Stone<0||p.Soil<0||p.Sand<0||p.Wood>1000000||p.Stone>1000000||p.Soil>1000000||p.Sand>1000000)throw new ArgumentException("Inventory quantities must be between zero and 1,000,000. The previous archive has been preserved.");
            if(!Enum.IsDefined(typeof(ToolKind),p.Tool))throw new ArgumentException("Traveler has an unsupported selected tool. Select a supported tool before saving.");
            var objectIds=new HashSet<long>();foreach(var o in s.Objects)if(o==null||!objectIds.Add(o.Id)||o.Id<1||o.Id>=s.NextObjectId||!Finite(o.X)||!Finite(o.Y)||Math.Abs(o.Y)>MaximumTravelerAltitude||!Finite(o.Z)||!Finite(o.Yaw)||!config.Contains(new GeoPoint(o.X,o.Z)))throw new ArgumentException("Invalid placed object.");
        }
        private static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
        public static double Hash(int x,int z,int channel)
        {unchecked {uint h=(uint)x*374761393U+(uint)z*668265263U+(uint)channel*2246822519U;h=(h^(h>>13))*1274126177U;return (h^(h>>16))/4294967296.0;}}
        public bool TryNatural(int x,int z,out NaturalSite site)
        {
            site=default;var p=new GeoPoint((x+.15+Hash(x,z,41)*.7)*32,(z+.15+Hash(x,z,42)*.7)*32);
            if(!geo.Configuration.Contains(p))return false;var c=geo.Sample(p);int region=ecology.RegionAt(p);
            if(c.ElevationMeters<geo.SeaLevel+3||region<0||geo.Hydrology.IsLakeWater(p.X,p.Y,c.ElevationMeters)||geo.Hydrology.IsRiverWater(p.X,p.Y,c.ElevationMeters))return false;
            double roll=Hash(x,z,43),forest=ecology.Forest(region),slope=Math.Abs(geo.ElevationAt(p.X+8,p.Y)-geo.ElevationAt(p.X-8,p.Y))/16;
            NaturalKind kind;float scale;double quantity;
            if(roll<.004&&(slope>.18||c.ElevationMeters>170||c.Biome==Biome.Desert)){kind=NaturalKind.Boulder;scale=(float)(2.5+Hash(x,z,45)*4);quantity=60;}
            else if(roll<forest*.22+ecology.Burn(region)*.12){kind=Hash(x,z,47)<.6?NaturalKind.Log:NaturalKind.Branch;scale=(float)(.6+Hash(x,z,45)*1.1);quantity=kind==NaturalKind.Log?24:5;}
            else if(c.Biome==Biome.Desert&&c.TemperatureCelsius>18&&roll<.03+.22*Math.Max(0,Math.Min(1,(geo.FieldNoise(p.X,p.Y,350,1810)+.35)*1.8))){kind=NaturalKind.DryPlant;scale=(float)(.7+Hash(x,z,45));quantity=2;}
            else if(c.Moisture>.35&&roll<.12&&slope<.5){kind=roll<.035?NaturalKind.Flowers:NaturalKind.Shrub;scale=(float)(.7+Hash(x,z,45)*.8);quantity=2;}
            else return false;
            site=new NaturalSite("natural:"+x+":"+z,kind,p,(float)LocalSolidField.RenderedGround(geo,p.X,p.Y),scale,(float)(Hash(x,z,46)*360),kind==NaturalKind.Boulder?ResourceKind.Stone:ResourceKind.Wood,quantity);return true;
        }
        public double Available(string id,double capacity,long tick,bool regrows)
        {if(!changes.TryGetValue(id,out var r))return capacity;return Math.Min(capacity,r.Remaining+(regrows?Math.Max(0,tick-r.Tick)/1440.0/12*capacity:0));}
        private void IndexTree(ResourceChange r){if(!r.Id.StartsWith("tree:",StringComparison.Ordinal))return;var parts=r.Id.Split(':');if(parts.Length==3&&int.TryParse(parts[1],out int x)&&int.TryParse(parts[2],out int z))treeChanges[((long)x<<32)|(uint)z]=r;}
        public bool TreeDepleted(double x,double z,long tick)=>treeChanges.TryGetValue(((long)(int)Math.Floor(x/16)<<32)|(uint)(int)Math.Floor(z/16),out var r)&&r.Remaining+Math.Max(0,tick-r.Tick)/1440.0/12*30<1;
        public int Harvest(string id,ResourceKind kind,double capacity,ToolKind required,long tick,int amount=5,bool regrows=false)
        {
            if(string.IsNullOrEmpty(id)||id.Length>80||!Enum.IsDefined(typeof(ResourceKind),kind)||!Enum.IsDefined(typeof(ToolKind),required)||!Finite(capacity)||capacity<=0||capacity>10000||tick<0||amount<1||amount>100||Traveler.Tool!=required&&required!=ToolKind.Hands)return 0;
            double available=Available(id,capacity,tick,regrows);int taken=(int)Math.Min(amount,Math.Floor(available));if(taken<=0)return 0;
            if(!changes.TryGetValue(id,out var r)){if(changes.Count>=MaximumResources)return 0;r=new ResourceChange{Id=id};state.Resources.Add(r);changes.Add(id,r);}
            r.Remaining=available-taken;r.Tick=tick;IndexTree(r);Credit(kind,taken);Revision++;return taken;
        }
        public void Credit(ResourceKind kind,int amount)
        {if(amount<0||amount>1000)throw new ArgumentOutOfRangeException(nameof(amount));if(kind==ResourceKind.Wood)Traveler.Wood=Math.Min(1000000,Traveler.Wood+amount);else if(kind==ResourceKind.Stone)Traveler.Stone=Math.Min(1000000,Traveler.Stone+amount);else if(kind==ResourceKind.Soil)Traveler.Soil=Math.Min(1000000,Traveler.Soil+amount);else Traveler.Sand=Math.Min(1000000,Traveler.Sand+amount);}
        public bool Dig(double x,double y,double z,ResourceKind material,out string reason)
        {
            if(!CanDig(x,y,z,material,out reason))return false;
            int sx=(int)Math.Round(x/32),sz=(int)Math.Round(z/32);double radius=ExcavationRadius(material);
            if(!editSites.TryGetValue((sx,sz),out var site)){site=new ExcavationSite{X=sx,Z=sz,Top=y+radius,Bottom=y-radius};state.Excavations.Add(site);editSites.Add((sx,sz),site);}
            var cut=new DigCut{X=x,Y=y,Z=z,Radius=radius,Material=material};site.Cuts.Add(cut);site.Top=Math.Max(site.Top,y+radius);site.Bottom=Math.Min(site.Bottom,y-radius);IndexCut(cut);
            Credit(material,1);Revision++;ExcavationRevision++;Excavated?.Invoke(cut);return true;
        }
        public event Action<DigCut> Excavated;
        public static double ExcavationRadius(ResourceKind material)=>material==ResourceKind.Stone?.75:.95;
        public bool CanDig(double x,double y,double z,ResourceKind material,out string reason)
        {
            reason="";if(!Finite(x)||!Finite(y)||!Finite(z)||!geo.Configuration.Contains(new GeoPoint(x,z))||Math.Abs(y)>MaximumTravelerAltitude-4){reason="Outside the supported world extent";return false;}
            if(!Enum.IsDefined(typeof(ResourceKind),material)||material==ResourceKind.Wood){reason="Aim at soil, sand or rock";return false;}
            if(material==ResourceKind.Stone?Traveler.Tool!=ToolKind.Pickaxe:Traveler.Tool!=ToolKind.Shovel){reason="Use the appropriate shovel or pickaxe";return false;}
            if(SolidDensity(x,y,z)<-ExcavationRadius(material)*.65){reason="Aim at remaining solid ground";return false;}return true;
        }
        private void IndexCut(DigCut cut)
        {
            double radius=cut.Radius+.2;
            for(int z=(int)Math.Floor((cut.Z-radius)/16);z<=(int)Math.Floor((cut.Z+radius)/16);z++)for(int y=(int)Math.Floor((cut.Y-radius)/16);y<=(int)Math.Floor((cut.Y+radius)/16);y++)for(int x=(int)Math.Floor((cut.X-radius)/16);x<=(int)Math.Floor((cut.X+radius)/16);x++)
            {var key=(x,y,z);if(!editChunks.TryGetValue(key,out var list)){list=new List<DigCut>();editChunks.Add(key,list);}list.Add(cut);}
        }
        public IReadOnlyList<DigCut> EditsForChunk(int x,int y,int z)=>editChunks.TryGetValue((x,y,z),out var cuts)?cuts.ToArray():Array.Empty<DigCut>();
        public double SolidDensity(double x,double y,double z)
        {
            double solid=Math.Min(LocalSolidField.RenderedGround(geo,x,z)-y,geo.Caves.Distance(x,y,z));
            if(editChunks.TryGetValue(((int)Math.Floor(x/16),(int)Math.Floor(y/16),(int)Math.Floor(z/16)),out var cuts))foreach(var c in cuts){double dx=x-c.X,dy=y-c.Y,dz=z-c.Z;solid=Math.Min(solid,Math.Sqrt(dx*dx+dy*dy+dz*dz)-c.Radius);}return solid;
        }
        public bool Place(double x,double y,double z,double yaw)
        {if(Traveler.Wood<3||state.Objects.Count>=MaximumObjects||!geo.Configuration.Contains(new GeoPoint(x,z))||!Finite(y)||Math.Abs(y)>MaximumTravelerAltitude||!Finite(yaw))return false;state.Objects.Add(new PlacedObject{Id=state.NextObjectId++,X=x,Y=y,Z=z,Yaw=yaw});Traveler.Wood-=3;Revision++;return true;}
        public LivingWorldSnapshot Capture(){var copy=Clone(state);copy.Seasons=Seasons.Capture();copy.Atmosphere=Atmosphere.Capture();return copy;}
        public static LivingWorldSnapshot Clone(LivingWorldSnapshot s)
        {
            var p=s.Traveler;var copy=new LivingWorldSnapshot{Version=s.Version,Seed=s.Seed,Seasons=s.Seasons?.Copy(),Atmosphere=s.Atmosphere?.Copy(),NextObjectId=s.NextObjectId,Traveler=new TravelerState{CharacterPreset=p.CharacterPreset,HasPosition=p.HasPosition,Exploring=p.Exploring,FirstPerson=p.FirstPerson,ReverseThirdPerson=p.ReverseThirdPerson,Crouched=p.Crouched,Flying=p.Flying,Airborne=p.Airborne,VelocityX=p.VelocityX,VelocityY=p.VelocityY,VelocityZ=p.VelocityZ,X=p.X,Y=p.Y,Z=p.Z,Yaw=p.Yaw,Pitch=p.Pitch,Tool=p.Tool,Wood=p.Wood,Stone=p.Stone,Soil=p.Soil,Sand=p.Sand}};
            foreach(var r in s.Resources)copy.Resources.Add(new ResourceChange{Id=r.Id,Remaining=r.Remaining,Tick=r.Tick});
            foreach(var s0 in s.Excavations){var s1=new ExcavationSite{X=s0.X,Z=s0.Z,Top=s0.Top,Bottom=s0.Bottom};foreach(var c in s0.Cuts)s1.Cuts.Add(new DigCut{X=c.X,Y=c.Y,Z=c.Z,Radius=c.Radius,Material=c.Material});copy.Excavations.Add(s1);}
            foreach(var o in s.Objects)copy.Objects.Add(new PlacedObject{Id=o.Id,X=o.X,Y=o.Y,Z=o.Z,Yaw=o.Yaw});return copy;
        }
    }
}
