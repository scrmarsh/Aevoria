using Aevoria.World;
using Aevoria.Persistence;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

void Check(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS: "+message);}
Check(ArchiveCodec.Decode<WorldRecord>(Encoding.UTF8.GetBytes("<WorldRecord/>" )).GeographyProfile==GeographyProfile.Legacy,"missing saved XML profile keeps historical generation");
Check(ArchiveCodec.Decode<WorldRecord>(Encoding.UTF8.GetBytes("<WorldRecord><GeographyProfile>Atlas</GeographyProfile></WorldRecord>" )).AtlasRevision==0,"missing Atlas revision keeps historical Atlas geography");
var revisionRecord=ArchiveCodec.Decode<WorldRecord>(ArchiveCodec.Encode(new WorldRecord{GeographyProfile=GeographyProfile.Atlas,AtlasRevision=1}));
Check(revisionRecord.GeographyProfile==GeographyProfile.Atlas&&revisionRecord.AtlasRevision==1,"Atlas terrain revision survives XML serialization independently of render recipes");
Check(WorldGenerationIdentity.For(new GenerationSettings()).Count==15&&WorldGenerationIdentity.For(new GenerationSettings(profile:GeographyProfile.Atlas)).Count==15&&WorldGenerationIdentity.For(new GenerationSettings(profile:GeographyProfile.Atlas,atlasRevision:1)).Count==16,"only the explicitly revised Atlas profile adds a generation product");
var report=new List<object>();
foreach(var seed in new[]{"aevoria-001","atlas-coasts-2","atlas-coasts-3"})
foreach(float coverage in new[]{.54f,.85f})
{
    var c=GenerationSettings.Preset(seed,WorldSize.Miniature,new GenerationSettings(targetLandFraction:coverage,profile:GeographyProfile.Atlas));
    ulong moduleSeed=WorldGenerationSeeds.Derive(seed,WorldLayerIds.Continents,ContinentalField.Version);
    var coasts=new ContinentalField(c,moduleSeed);int land=0,seaCore=0;
    for(int z=0;z<97;z++)for(int x=0;x<97;x++)
    {
        double east=((x+.5)/97-.5)*c.WidthMeters,north=((z+.5)/97-.5)*c.DepthMeters;
        double distance=coasts.CoastDistanceAt(east,north);if(distance>0)land++;
        if(AtlasGeography.SeaMargin(c,moduleSeed,east,north)<-1000){CheckCore(distance);seaCore++;}
    }
    double fraction=land/(97.0*97);
    Check(Math.Abs(fraction-coverage)<.025,$"{seed} coverage {coverage}: actual {fraction:F3}");
    Check(seaCore>20,$"{seed}: major sea has a substantial physical basin");
    report.Add(new{seed,coverage,fraction,seaCore});
}
if(args.Contains("--full"))
{
    var size=args.Contains("--colossal")?WorldSize.Colossal:WorldSize.Miniature;
    int sizeArg=Array.IndexOf(args,"--size");if(sizeArg>=0)size=Enum.Parse<WorldSize>(args[sizeArg+1]);
    int seedArg=Array.IndexOf(args,"--seed");string seed=seedArg>=0?args[seedArg+1]:"aevoria-001";
    int reliefArg=Array.IndexOf(args,"--relief");float relief=reliefArg>=0?float.Parse(args[reliefArg+1],System.Globalization.CultureInfo.InvariantCulture):3600;
    int revision=args.Contains("--genesis")?1:0;
    var c=GenerationSettings.Preset(seed,size,new GenerationSettings(profile:GeographyProfile.Atlas,atlasRevision:revision,mountains:relief));
    var geo=new ProceduralGeography(c,new ClimateSettings(),stage=>Console.WriteLine(stage));
    var biomes=new Dictionary<string,int>();
    for(int z=0;z<129;z++)for(int x=0;x<129;x++)
    {
        var sample=geo.Sample(new GeoPoint((x/128.0-.5)*c.WidthMeters,(z/128.0-.5)*c.DepthMeters));
        string name=sample.Biome.ToString();biomes[name]=biomes.GetValueOrDefault(name)+1;
    }
    Console.WriteLine(JsonSerializer.Serialize(new{size=size.ToString(),revision,seed,relief,lakes=geo.InlandSurfaces.LakeCount,biomes,fingerprint=WorldRecord.GeographyFingerprint(geo)}));
    if(revision>0)
    {
        Check(geo.Layers.Ids.Count==16&&geo.Surface.Regional!=null,"regional landforms are an explicit immutable drainage input");
        Check(geo.Climate.LatitudeAt(c.DepthMeters*.5)<70,"new northern lowlands use a subarctic Scandinavian latitude instead of a polar coast");
        int dunes=0;float largestDune=0;
        for(int z=0;z<129;z++)for(int x=0;x<129;x++)
        {
            double east=(x/128.0-.5)*c.WidthMeters,north=(z/128.0-.5)*c.DepthMeters;
            float height=geo.Surface.ElevationAt(east,north),offset=Math.Abs(geo.Surface.Regional.DuneOffsetAt(east,north,height));
            if(offset>.5f)dunes++;largestDune=Math.Max(largestDune,offset);
        }
        Console.WriteLine(JsonSerializer.Serialize(new{duneSamples=dunes,largestDune,saltBasins=geo.Surface.Regional.SaltBasinCount}));
        Check(dunes>20&&largestDune>2,"desert dunes have substantial physical relief before drainage, not only a material pattern");
        Check(geo.Surface.Regional.SaltBasinCount>0,"arid salt basins have explicit supported geographic footprints");
    }
    Check(geo.InlandSurfaces.LakeCount<100,"atlas geography avoids hundreds of random lakes");
    Check(geo.Mountains.Clusters.Count>=2,"atlas geography retains multiple mountain provinces");
    var h=geo.Hydrology;int channels=0,uphill=0,broken=0,uncovered=0,widthGaps=0;
    for(int i=0;i<h.Ground.Length;i++)if(h.IsChannelCandidate(i))
    {
        channels++;float previous=h.PathLevel(i,0);
        for(int p=1;p<=h.PathSamples(i);p++)
        {
            float level=h.PathLevel(i,p/(double)h.PathSamples(i));if(level>previous+.025f)uphill++;previous=level;
        }
        h.PathPoint(i,h.PathSamples(i),out double ex,out double ez);int receiver=h.Receiver[i];
        if(Math.Abs(ex-h.X(receiver))>.25||Math.Abs(ez-h.Y(receiver))>.25)broken++;
        if(Math.Abs(h.RiverWidthAt(i,1)-h.RiverWidthAt(receiver,0))>.01)widthGaps++;
        int midpoint=h.PathSamples(i)/2;h.PathPoint(i,midpoint,out double x,out double z);float middle=h.PathLevel(i,midpoint/(double)h.PathSamples(i));
        if(h.IsRiver(i)&&!geo.InlandSurfaces.IsSeasonallyDryChannel(i)&&middle>geo.SeaLevel+.2&&!geo.InlandSurfaces.LakeAt(x,z,out _)&&geo.ElevationAt(x,z)>middle+.025)uncovered++;
    }
    Console.WriteLine(JsonSerializer.Serialize(new{channels,uphill,broken,uncovered,widthGaps}));
    Check(channels>0&&uphill==0,"river water levels remain downhill through every sampled path");
    Check(broken==0,"tributaries join their downstream drainage nodes without position gaps");
    Check(widthGaps==0,"river cross-sections share the downstream width at every junction");
    Check(uncovered==0,"visible river centre lines have carved terrain beneath their water surface");
    Console.WriteLine(JsonSerializer.Serialize(new{lakes=geo.InlandSurfaces.LakeCount,biomes,fingerprint=WorldRecord.GeographyFingerprint(geo)}));
}
Console.WriteLine(JsonSerializer.Serialize(report));
void CheckCore(double distance){if(distance>0)throw new Exception("Sea core became land during coast calibration");}
