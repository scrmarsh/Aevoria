using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Aevoria.World;
using Aevoria.Persistence;

if(args.Length<3||args.Length>4)throw new ArgumentException("seed size output-directory [landmass] are required; evidence is never overwritten");
string seed=args[0],output=Path.GetFullPath(args[2]);var size=Enum.Parse<WorldSize>(args[1]);
if(Directory.Exists(output))throw new IOException("Choose a fresh output directory");Directory.CreateDirectory(output);
var sourceFiles=Directory.GetFiles("Assets/Scripts/World","*.cs").Append("Assets/Scripts/Core/ApplicationVersion.cs").ToDictionary(p=>p,p=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(p))));
File.WriteAllText(Path.Combine(output,"source.json"),JsonSerializer.Serialize(sourceFiles,new JsonSerializerOptions{WriteIndented=true}));
var requested=args.Length==4?Enum.Parse<LandmassType>(args[3]):LandmassType.Continents;
var watch=Stopwatch.StartNew();var geo=new ProceduralGeography(GenerationSettings.Preset(seed,size,new GenerationSettings(landmass:requested)),progress:stage=>Console.Error.WriteLine(DateTime.UtcNow.ToString("O")+" "+stage));
double generated=watch.Elapsed.TotalSeconds;const int n=129;double width=geo.Configuration.WidthMeters;var plainSlope=new List<double>();var mountains=new List<double>();var samples=new List<object>();float low=float.MaxValue,high=float.MinValue;int land=0,mountainArea=0;
using(var csv=new StreamWriter(Path.Combine(output,"fields.csv")))
{
    csv.WriteLine("x,z,elevation,rawElevation,uplift,biome,slope64m,slope1km");
    for(int z=0;z<n;z++)for(int x=0;x<n;x++)
    {
        double east=((x+.5)/n-.5)*width,north=((z+.5)/n-.5)*width;var sample=geo.Sample(new GeoPoint(east,north));float h=sample.ElevationMeters;var geology=geo.Geology.Sample(east,north);
        double Slope(double radius)=>Math.Sqrt(Math.Pow((geo.ElevationAt(east+radius,north)-geo.ElevationAt(east-radius,north))/(radius*2),2)+Math.Pow((geo.ElevationAt(east,north+radius)-geo.ElevationAt(east,north-radius))/(radius*2),2));
        double local=Slope(32),regional=Slope(500);low=Math.Min(low,h);high=Math.Max(high,h);if(h>geo.SeaLevel)land++;
        if(sample.Biome==Biome.Grassland&&h>geo.SeaLevel+5)plainSlope.Add(local);
        if(h>geo.SeaLevel+1000&&geology.Uplift>.1){mountainArea++;mountains.Add(h);}
        csv.WriteLine(FormattableString.Invariant($"{east:R},{north:R},{h:R},{geo.BaseElevationAt(east,north):R},{geology.Uplift:R},{sample.Biome},{local:R},{regional:R}"));
    }
}
double Percentile(List<double> values,double fraction){values.Sort();return values.Count==0?0:values[(int)((values.Count-1)*fraction)];}
var discontinuities=new List<object>();var hydro=geo.Hydrology;
for(int z=2;z<HydrologyMap.Resolution-2;z+=3)for(int x=2;x<HydrologyMap.Resolution-2;x+=5)
{
    int i=z*HydrologyMap.Resolution+x;
    foreach(bool eastWest in new[]{true,false})
    {
        double e=hydro.X(i)+hydro.StepX*(eastWest?.5:.17),k=hydro.Y(i)+hydro.StepY*(eastWest?.13:.5),dx=eastWest?.02:0,dz=eastWest?0:.02;
        double a=geo.ElevationAt(e-dx,k-dz),b=geo.ElevationAt(e+dx,k+dz),rawA=geo.BaseElevationAt(e-dx,k-dz),rawB=geo.BaseElevationAt(e+dx,k+dz);
        if(Math.Abs(a-b)>.5)discontinuities.Add(new{e,k,eastWest,delta=b-a,rawDelta=rawB-rawA,incisionDelta=(b-rawB)-(a-rawA)});
    }
}
File.WriteAllText(Path.Combine(output,"continuity.json"),JsonSerializer.Serialize(discontinuities,new JsonSerializerOptions{WriteIndented=true}));
var incision=new List<double>();
for(int i=0;i<hydro.Ground.Length;i++)if(hydro.IsRiver(i))for(int p=0;p<=hydro.PathSamples(i);p+=4){hydro.PathPoint(i,p,out double x,out double z);incision.Add(Math.Max(0,geo.BaseElevationAt(x,z)-geo.ElevationAt(x,z)));}
var suspended=new List<double>();var gaps=new List<(double gap,object details)>();
for(int i=0;i<hydro.Ground.Length;i++)if(hydro.IsRiver(i))for(int p=0;p<=hydro.PathSamples(i);p+=4){hydro.PathPoint(i,p,out double x,out double z);if(hydro.PathLevel(i,p/(double)hydro.PathSamples(i))>geo.SeaLevel+.04f&&!geo.InlandSurfaces.LakeAt(x,z,out _)){double level=hydro.PathLevel(i,p/(double)hydro.PathSamples(i)),gap=Math.Max(0,level-HydrologyMap.ChannelDepth(hydro.RiverWidth(i))-geo.ElevationAt(x,z));suspended.Add(gap);if(gap>10){int r=hydro.Receiver[i];gaps.Add((gap,new{x,z,i,p,gap,level,raw=geo.BaseElevationAt(x,z),ground=geo.ElevationAt(x,z),sourceGround=hydro.Ground[i],targetGround=hydro.Ground[r],sourceWater=hydro.Water[i],targetWater=hydro.Water[r],potentialLake=hydro.PotentialLake[i],rejection=hydro.OutcomeAt(i).ToString(),sourceLake=hydro.Lake[i],targetLake=hydro.Lake[r],width=hydro.RiverWidth(i)}));}}}
File.WriteAllText(Path.Combine(output,"suspended.json"),JsonSerializer.Serialize(new{samples=suspended.Count,median=Percentile(suspended,.5),p95=Percentile(suspended,.95),maximum=suspended.Count==0?0:suspended.Max(),over10m=suspended.Count(v=>v>10),worst=gaps.OrderByDescending(v=>v.gap).Take(30).Select(v=>v.details)},new JsonSerializerOptions{WriteIndented=true}));
File.WriteAllText(Path.Combine(output,"incision.json"),JsonSerializer.Serialize(new{samples=incision.Count,median=Percentile(incision,.5),p95=Percentile(incision,.95),maximum=incision.Count==0?0:incision.Max(),over100m=incision.Count(v=>v>100),over500m=incision.Count(v=>v>500)},new JsonSerializerOptions{WriteIndented=true}));
var result=new{generator=ProceduralGeography.GeneratorVersion,geology=GeologyMap.Version,hydrology=HydrologyMap.Version,seed,size=size.ToString(),widthMeters=width,generationSeconds=generated,datum=geo.Geology.DatumAdjustment,coastThreshold=geo.CoastThreshold,minimum=low,maximum=high,landFraction=land/(double)(n*n),mountainFractionOfLand=mountainArea/(double)Math.Max(1,land),grasslandSamples=plainSlope.Count,grasslandMedianSlope=Percentile(plainSlope,.5),grasslandP95Slope=Percentile(plainSlope,.95),grasslandGentleFraction=plainSlope.Count==0?0:plainSlope.Count(v=>v<.05)/(double)plainSlope.Count,lakes=geo.InlandSurfaces.LakeCount,riverSegments=geo.Hydrology.RiverSegments,fingerprint=WorldRecord.GeographyFingerprint(geo)};
string json=JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true});File.WriteAllText(Path.Combine(output,"summary.json"),json);Console.WriteLine(json);
