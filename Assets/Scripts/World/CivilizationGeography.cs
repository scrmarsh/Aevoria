using System;
using System.Collections.Generic;
using System.Threading;

namespace Aevoria.World
{
    public readonly struct SettlementSite
    {
        public readonly double X,Y,Z,Score,Fertility,Freshwater,Timber,Stone,Ore,Climate,Slope;
        public SettlementSite(double x,double y,double z,double score,double fertility,double water,double timber,double stone,double ore,double climate,double slope)
        {X=x;Y=y;Z=z;Score=score;Fertility=fertility;Freshwater=water;Timber=timber;Stone=stone;Ore=ore;Climate=climate;Slope=slope;}
    }
    public sealed class CivilizationSiteField
    {
        public const int Version=1;
        public ulong Seed{get;}
        private readonly WorldConfiguration configuration;private readonly IWorldHeightField surface;private readonly HydrologyMap hydrology;private readonly InlandWaterLayout layout;private readonly CaveNetwork caves;private readonly BiomeField biomes;private readonly EcosystemField ecosystem;private readonly WorldMaterialField materials;
        private IReadOnlyList<SettlementSite> initialSites;
        public IReadOnlyList<SettlementSite> InitialSites=>initialSites??Array.Empty<SettlementSite>();
        public CivilizationSiteField(WorldConfiguration configuration,IWorldHeightField surface,HydrologyMap hydrology,InlandWaterLayout water,CaveNetwork caves,BiomeField biomes,EcosystemField ecosystem,WorldMaterialField materials,ulong seed)
        {this.configuration=configuration;this.surface=surface;this.hydrology=hydrology;layout=water;this.caves=caves;this.biomes=biomes;this.ecosystem=ecosystem;this.materials=materials;Seed=seed;}
        private GeographicSample Sample(GeoPoint p)=>biomes.Sample(p.X,p.Y,surface.ElevationAt(p.X,p.Y));
        private double Unit(int x,int z,uint channel)=>WorldGenerationSeeds.Site(Seed,x,z,channel);
        public void Prepare(CancellationToken token,Action<string,long,long> report)
        {initialSites=Array.AsReadOnly(Generate(new CivilizationSettings(),token,report).ToArray());}
        public bool Footprint(double x,double z,double radius,out double height)
        {
            height=surface.ElevationAt(x,z);if(!configuration.Contains(new GeoPoint(x-radius,z-radius))||!configuration.Contains(new GeoPoint(x+radius,z+radius)))return false;
            double minimum=height,maximum=height;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {
                double east=x+dx*radius,north=z+dz*radius;float h=surface.ElevationAt(east,north);
                if(h<configuration.Terrain.SeaLevel+8||layout.LakeAt(east,north,out float level)&&h<level+3||hydrology.IsRiverWater(east,north,h))return false;
                if(hydrology.ChannelSample(east,north,out _,out float bank,out _)&&bank<Math.Max(12,radius*.5))return false;
                // A building must have material beneath its foundation, including
                // natural entrances. Deep caves below a sound roof are allowed.
                if(caves.Distance(east,h-1,north)<1)return false;
                minimum=Math.Min(minimum,h);maximum=Math.Max(maximum,h);
            }
            return maximum-minimum<=Math.Max(1.5,radius*.28);
        }
        public bool TrySite(double x,double z,out SettlementSite site)
        {
            site=default;var sample=Sample(new GeoPoint(x,z));
            if(sample.ElevationMeters<=configuration.Terrain.SeaLevel+12||sample.ElevationMeters>2400||sample.TemperatureCelsius< -3||sample.TemperatureCelsius>36||sample.Fertility<.12||sample.Biome==Biome.Snow||sample.Biome==Biome.Alpine||sample.Biome==Biome.Ocean)return false;
            double h=sample.ElevationMeters,dx=Math.Abs(surface.ElevationAt(x+32,z)-surface.ElevationAt(x-32,z))/64,dz=Math.Abs(surface.ElevationAt(x,z+32)-surface.ElevationAt(x,z-32))/64,slope=Math.Sqrt(dx*dx+dz*dz);
            if(slope>.16||!Footprint(x,z,28,out h))return false;
            // Resource potential is sampled from actual local geography. This is
            // an aggregate catchment, separate from sparse traveler inventory.
            double fertile=0,moisture=0,trees=0,rock=0;int valid=0;
            for(int iz=-1;iz<=1;iz++)for(int ix=-1;ix<=1;ix++)
            {
                double east=x+ix*600,north=z+iz*600;if(!configuration.Contains(new GeoPoint(east,north)))continue;var s=Sample(new GeoPoint(east,north));if(s.ElevationMeters<=configuration.Terrain.SeaLevel||layout.LakeAt(east,north,out _))continue;
                fertile+=s.Fertility;moisture+=s.Moisture;trees+=ecosystem.ForestPotential(east,north);var material=materials.Sample(east,north);rock+=CivilIdentity.Clamp((1-material.Regolith)*.7+CivilIdentity.Clamp((s.ElevationMeters-100)/1800.0+.12)*.3);valid++;
            }
            if(valid<3)return false;fertile/=valid;moisture/=valid;trees/=valid;rock/=valid;
            var hydro=hydrology;int cell=hydro.Nearest(x,z),cx=cell%HydrologyMap.Resolution,cz=cell/HydrologyMap.Resolution;double fresh=0;
            for(int oz=-3;oz<=3;oz++)for(int ox=-3;ox<=3;ox++)
            {
                int nx=cx+ox,nz=cz+oz;if(nx<0||nz<0||nx>=HydrologyMap.Resolution||nz>=HydrologyMap.Resolution)continue;int i=nz*HydrologyMap.Resolution+nx;
                if(!hydro.IsRiver(i)&&!layout.LakeAt(hydro.X(i),hydro.Y(i),out _))continue;
                double distance=Math.Sqrt(Math.Pow(hydro.X(i)-x,2)+Math.Pow(hydro.Y(i)-z,2));fresh=Math.Max(fresh,1/(1+distance/1200));
            }
            // Rainwater supports modest settlements without a mapped river;
            // it is not presented as a freshwater deposit or river connection.
            double water=Math.Max(fresh,moisture*.45),climate=CivilIdentity.Clamp(1-Math.Abs(sample.TemperatureCelsius-17)/28.0);
            double score=fertile*.32+water*.23+climate*.22+trees*.09+(1-slope/.16)*.14;
            double ore=rock*CivilIdentity.Clamp(.2+Unit((int)Math.Floor(x/2048),(int)Math.Floor(z/2048),972)*.8);
            site=new SettlementSite(x,h,z,score,fertile,water,trees,rock,ore,climate,slope);return score>.36;
        }
        public List<SettlementSite> Candidates(CivilizationSettings settings,CancellationToken token=default)
        {settings.Validate();token.ThrowIfCancellationRequested();return initialSites!=null&&settings.CandidateResolution==96?new List<SettlementSite>(initialSites):Generate(settings,token,null);}
        private List<SettlementSite> Generate(CivilizationSettings settings,CancellationToken token,Action<string,long,long> report)
        {
            settings.Validate();var sites=new List<SettlementSite>();ulong random=Seed;int n=settings.CandidateResolution;double width=configuration.WidthMeters,depth=configuration.DepthMeters;
            for(int z=0;z<n;z++){token.ThrowIfCancellationRequested();report?.Invoke("Evaluating settlement geography",z,n);for(int x=0;x<n;x++){double east=((x+.2+CivilIdentity.Next(ref random)*.6)/n-.5)*width,north=((z+.2+CivilIdentity.Next(ref random)*.6)/n-.5)*depth;if(TrySite(east,north,out var site))sites.Add(site);}}
            sites.Sort((a,b)=>{int score=b.Score.CompareTo(a.Score);if(score!=0)return score;int x=a.X.CompareTo(b.X);return x!=0?x:a.Z.CompareTo(b.Z);});return sites;
        }
    }
    public static class CivilizationGeography
    {
        public static bool Footprint(ProceduralGeography geo,double x,double z,double radius,out double height)=>geo.CivilizationSites.Footprint(x,z,radius,out height);
        public static bool TrySite(ProceduralGeography geo,double x,double z,out SettlementSite site)=>geo.CivilizationSites.TrySite(x,z,out site);
        public static List<SettlementSite> Candidates(ProceduralGeography geo,CivilizationSettings settings,CancellationToken token=default)=>geo.CivilizationSites.Candidates(settings,token);
    }
}
