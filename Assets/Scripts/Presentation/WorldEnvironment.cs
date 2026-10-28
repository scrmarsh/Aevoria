using Aevoria.Simulation;
using Aevoria.World;
using UnityEngine;

namespace Aevoria.Presentation
{
    public sealed class WorldEnvironment : MonoBehaviour
    {
        private ProceduralGeography geo;
        private SimulationClock clock;
        private Camera view;
        private WorldAtmosphere atmosphere;
        private ParticleSystem precipitation;
        private Texture2D climate,weatherGround,coastTexture,materialTexture,atlasBiomeTexture;
        private Texture2D seasonalTexture;private Color[] seasonalPixels;
        private SeasonalModel seasons;private EcologyModel ecology;private RegionalAtmosphere weatherModel;private Texture2D weatherTexture;private Color[] weatherPixels;private WorldCloudLayer clouds;
        public RegionalWeather LocalWeather {get;private set;}
        private float refresh;
        public WeatherKind? PreviewWeather {get;set;}
        public int? PreviewSeason {get;set;}
        public WeatherKind Weather {get;private set;}
        public bool RenderWeatherEffects {get;private set;}=true;
        public int VisibleCloudSystems=>clouds?.VisibleSystems??0;
        public void ApplyPresentation(bool renderClouds,bool weatherEffects,int density)
        {
            RenderWeatherEffects=weatherEffects;
            if(clouds!=null){clouds.RenderClouds=renderClouds;clouds.Density=density;clouds.Present(CloudTime);}
            if(!weatherEffects&&precipitation!=null)precipitation.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            refresh=0;
        }
        public void Initialize(ProceduralGeography geography,SimulationClock simulation,Camera camera,WorldAtmosphere worldAtmosphere,SeasonalModel seasonal=null,EcologyModel ecological=null,RegionalAtmosphere regionalWeather=null)
        {
            geo=geography;clock=simulation;view=camera;atmosphere=worldAtmosphere;
            (GetComponent<GenesisMaterialRenderer>()??gameObject.AddComponent<GenesisMaterialRenderer>()).Initialize(geo);
            (GetComponent<RiverBankField>()??gameObject.AddComponent<RiverBankField>()).Initialize(geo);
            seasons=seasonal;ecology=ecological;weatherModel=regionalWeather;
            if(coastTexture!=null)Destroy(coastTexture);
            const int coastN=ContinentalField.Resolution;var coastValues=new float[coastN*coastN];
            for(int z=0;z<coastN;z++)for(int x=0;x<coastN;x++)coastValues[z*coastN+x]=(float)geo.Continents.CoastDistanceAt((x/(double)(coastN-1)-.5)*geo.Configuration.WidthMeters,(z/(double)(coastN-1)-.5)*geo.Configuration.DepthMeters);
            coastTexture=new Texture2D(coastN,coastN,TextureFormat.RFloat,false,true){name="Canonical signed coast distance",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};coastTexture.SetPixelData(coastValues,0);coastTexture.Apply(false,true);Shader.SetGlobalTexture("_AevoriaCoastDistance",coastTexture);Shader.SetGlobalFloat("_AevoriaCoastReady",1);
            if(seasonalTexture!=null)Destroy(seasonalTexture);
            if(seasons!=null){int size=ecology.Resolution;seasonalTexture=new Texture2D(size,size,TextureFormat.RGBAHalf,false,true){name="Persistent snowpack and ice",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};seasonalPixels=new Color[size*size];UpdateSeasonal(true);Shader.SetGlobalTexture("_AevoriaSeasonal",seasonalTexture);Shader.SetGlobalFloat("_AevoriaSeasonalReady",1);}
            if(climate!=null) Destroy(climate);
            const int n=ClimateMap.Resolution;climate=new Texture2D(n,n,TextureFormat.RGBAHalf,false,true){name="World climate",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};var colors=new Color[n*n];var c=geo.Configuration;
            // Raw Celsius preserves climates colder than -30 C. The blue
            // channel carries the actual regional annual temperature amplitude.
            for(int z=0;z<n;z++) for(int x=0;x<n;x++) {var point=new GeoPoint((x/(double)(n-1)-0.5)*c.WidthMeters,(z/(double)(n-1)-0.5)*c.DepthMeters);var s=geo.Sample(point);colors[z*n+x]=new Color(s.TemperatureCelsius,s.Moisture,CryosphereClimate.SeasonAmplitude(geo.Climate.LatitudeAt(point.Y),geo.Climate.Continentality(point.X,point.Y)),s.ElevationMeters);}
            climate.SetPixels(colors);climate.Apply(false,true);Shader.SetGlobalFloat("_AevoriaClimateSea",geo.SeaLevel);Shader.SetGlobalTexture("_AevoriaClimate",climate);Shader.SetGlobalFloat("_AevoriaClimateReady",1);
            if(materialTexture!=null)Destroy(materialTexture);materialTexture=new Texture2D(n,n,TextureFormat.RGBAHalf,false,true){name="Authoritative soil and geological materials",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var materialColors=new Color[n*n];for(int z=0;z<n;z++)for(int x=0;x<n;x++){double east=(x/(double)(n-1)-.5)*c.WidthMeters,north=(z/(double)(n-1)-.5)*c.DepthMeters;var sample=geo.Materials.Sample(east,north);materialColors[z*n+x]=new Color(sample.SoilDepth,sample.Regolith,sample.RockHardness,0);}
            materialTexture.SetPixels(materialColors);materialTexture.Apply(false,true);Shader.SetGlobalTexture("_AevoriaMaterials",materialTexture);Shader.SetGlobalFloat("_AevoriaMaterialsReady",1);
            if(atmosphere!=null)atmosphere.AtlasPresentation=c.Terrain.Profile==GeographyProfile.Atlas;
            Shader.SetGlobalFloat("_AevoriaAtlasReady",0);
            if(atlasBiomeTexture!=null)Destroy(atlasBiomeTexture);
            if(c.Terrain.Profile==GeographyProfile.Atlas)
            {
                atlasBiomeTexture=new Texture2D(n,n,TextureFormat.RGBAHalf,false,true){name="Atlas habitat transitions",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
                var habitatColors=new Color[n*n];
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    var point=new GeoPoint((x/(double)(n-1)-.5)*c.WidthMeters,(z/(double)(n-1)-.5)*c.DepthMeters);
                    var habitat=geo.Sample(point).Biome;var color=EnvironmentPalette.BiomeColor(habitat)*.65f;
                    // A kilometre-scale texel represents expected canopy cover.
                    // Sampling individual groves below its Nyquist limit aliases
                    // the 370 m distribution into a visible lattice of dark dots.
                    color.a=(float)(BiomeHabitat.For(habitat).TreeDensity*.4);habitatColors[z*n+x]=color;
                }
                atlasBiomeTexture.SetPixels(habitatColors);atlasBiomeTexture.Apply(false,true);
                Shader.SetGlobalTexture("_AevoriaAtlasBiomes",atlasBiomeTexture);Shader.SetGlobalFloat("_AevoriaAtlasReady",1);
            }
            if(precipitation==null)
            {
                var obj=new GameObject("Local precipitation");obj.transform.SetParent(transform,false);precipitation=obj.AddComponent<ParticleSystem>();precipitation.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=precipitation.main;main.maxParticles=1000;main.startLifetime=2;main.startSpeed=0;main.startSize=0.08f;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var shape=precipitation.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(40,4,40);
                var renderer=precipitation.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Resources.Load<Material>("WorldRain");renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=3;
                precipitation.useAutoRandomSeed=false;precipitation.randomSeed=4819;
            }
            if(weatherTexture!=null)Destroy(weatherTexture);int weatherSize=ecology?.Resolution??32;weatherTexture=new Texture2D(weatherSize,weatherSize,TextureFormat.RGBAHalf,false,true){name="Regional cloud, precipitation and wetness",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};weatherPixels=new Color[weatherSize*weatherSize];UpdateWeatherTexture(true);Shader.SetGlobalTexture("_AevoriaWeather",weatherTexture);Shader.SetGlobalVector("_AevoriaWeatherRect",new Vector4((float)-c.WidthMeters/2,(float)-c.DepthMeters/2,(float)c.WidthMeters,(float)c.DepthMeters));Shader.SetGlobalFloat("_AevoriaWeatherReady",1);
            if(clouds==null){var obj=new GameObject("Regional weather clouds");obj.transform.SetParent(transform,false);clouds=obj.AddComponent<WorldCloudLayer>();}clouds.Initialize(geo);refresh=0;
            if(weatherGround!=null)Destroy(weatherGround);weatherGround=new Texture2D(weatherSize,weatherSize,TextureFormat.RFloat,false,true){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var referenceHeights=new float[weatherSize*weatherSize];for(int i=0;i<referenceHeights.Length;i++){var point=ecology!=null?ecology.EnvironmentAt(i).Center:new GeoPoint(((i%weatherSize+.5)/weatherSize-.5)*c.WidthMeters,((i/weatherSize+.5)/weatherSize-.5)*c.DepthMeters);referenceHeights[i]=geo.BaseElevationAt(point.X,point.Y);}weatherGround.SetPixelData(referenceHeights,0);weatherGround.Apply(false,true);Shader.SetGlobalTexture("_AevoriaWeatherGround",weatherGround);
        }
        private void Update()
        {
            if(geo==null || Time.unscaledTime<refresh) return;refresh=Time.unscaledTime+0.25f;
            var focus=view.GetComponent<MapCamera>().Focus;LocalWeather=RegionalAtmosphere.Forecast(geo,clock.State.Tick,new GeoPoint(focus.x,focus.z));Weather=PreviewWeather??LocalWeather.Kind;UpdateWeatherTexture(false);
            float latitude=geo.Climate.LatitudeAt(focus.z),wind=Mathf.Abs(latitude)>=30&&Mathf.Abs(latitude)<60?1:-1;
            // Same world-day advection direction and speed as the forecast.
            double day=clock.State.Tick/4.0;Shader.SetGlobalVector("_AevoriaCloudDrift",new Vector4((float)(wind*day*800),(float)(System.Math.Sin(latitude*System.Math.PI/90)*.24*day*800),0,0));
            Shader.SetGlobalVector("_AevoriaCloudWeather",new Vector4(geo.Climate.Settings.Latitude,(float)(System.Math.Min(160,System.Math.Max(30,geo.Configuration.DepthMeters/128000*30))/geo.Configuration.DepthMeters),(float)day,0));
            int season=PreviewSeason??new WorldCalendar(clock.State.Tick).Season;
            float seasonTemperature=season==1?7:season==3?-9:0;
            Shader.SetGlobalFloat("_AevoriaSeasonTemperature",seasonTemperature);
            if(seasons!=null)UpdateSeasonal(false);
            atmosphere.WeatherDarkness=clouds.RenderClouds?LocalWeather.Cloud*.25f:0;atmosphere.WeatherFog=RenderWeatherEffects?1+LocalWeather.Precipitation*.6f:1;
            if(RenderSettings.sun!=null){RenderSettings.sun.intensity=1.3f;RenderSettings.sun.color=new Color(1,.96f,.88f);}            bool snow=Weather==WeatherKind.Snow,wet=snow||Weather==WeatherKind.Rain||Weather==WeatherKind.Storm;
            precipitation.transform.position=view.transform.position+new Vector3(0,12,0);
            var main=precipitation.main;main.startSize=snow?0.13f:0.035f;main.startColor=snow?Color.white:new Color(0.65f,0.78f,0.88f,0.65f);
            var velocity=precipitation.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.y=snow?-4:-24;velocity.x=LocalWeather.WindX;velocity.z=LocalWeather.WindZ;
            var emission=precipitation.emission;float aboveGround=view.transform.position.y-geo.ElevationAt(focus.x,focus.z);emission.rateOverTime=wet&&aboveGround<2000?(PreviewWeather.HasValue?200:LocalWeather.Precipitation*500):0;
            var renderer=precipitation.GetComponent<ParticleSystemRenderer>();renderer.renderMode=snow?ParticleSystemRenderMode.Billboard:ParticleSystemRenderMode.Stretch;
            if(wet&&RenderWeatherEffects&&!precipitation.isPlaying) precipitation.Play();if((!wet||!RenderWeatherEffects)&&precipitation.isPlaying) precipitation.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private double CloudTime=>clock.State.Tick+System.Math.Min(1,clock.BacklogSeconds/SimulationClock.SecondsPerTick);
        private void LateUpdate(){if(geo!=null&&clouds!=null)clouds.Present(CloudTime);}
        private void OnDestroy()
        {
            if(Shader.GetGlobalTexture("_AevoriaMaterials")==materialTexture){Shader.SetGlobalFloat("_AevoriaMaterialsReady",0);Shader.SetGlobalTexture("_AevoriaMaterials",Texture2D.blackTexture);}if(materialTexture!=null)Destroy(materialTexture);
            if(Shader.GetGlobalTexture("_AevoriaSeasonal")==seasonalTexture){Shader.SetGlobalFloat("_AevoriaSeasonalReady",0);Shader.SetGlobalTexture("_AevoriaSeasonal",Texture2D.blackTexture);}if(seasonalTexture!=null)Destroy(seasonalTexture);
            bool ownsClimate=Shader.GetGlobalTexture("_AevoriaClimate")==climate;
            if(Shader.GetGlobalTexture("_AevoriaAtlasBiomes")==atlasBiomeTexture){Shader.SetGlobalFloat("_AevoriaAtlasReady",0);Shader.SetGlobalTexture("_AevoriaAtlasBiomes",Texture2D.blackTexture);}if(atlasBiomeTexture!=null)Destroy(atlasBiomeTexture);
            if(ownsClimate){Shader.SetGlobalFloat("_AevoriaClimateReady",0);Shader.SetGlobalFloat("_AevoriaSeasonTemperature",0);Shader.SetGlobalTexture("_AevoriaClimate",Texture2D.blackTexture);}
            if(Shader.GetGlobalTexture("_AevoriaCoastDistance")==coastTexture){Shader.SetGlobalFloat("_AevoriaCoastReady",0);Shader.SetGlobalTexture("_AevoriaCoastDistance",Texture2D.blackTexture);}if(coastTexture!=null)Destroy(coastTexture);
            if(Shader.GetGlobalTexture("_AevoriaWeather")==weatherTexture){Shader.SetGlobalFloat("_AevoriaWeatherReady",0);Shader.SetGlobalTexture("_AevoriaWeather",Texture2D.blackTexture);}if(weatherTexture!=null)Destroy(weatherTexture);if(clouds!=null)Destroy(clouds.gameObject);
            if(weatherGround!=null)Destroy(weatherGround);if(climate!=null) Destroy(climate);if(precipitation!=null) Destroy(precipitation.gameObject);
            if(ownsClimate&&atmosphere!=null)atmosphere.AtlasPresentation=false;
            if(ownsClimate){if(atmosphere!=null){atmosphere.WeatherDarkness=0;atmosphere.WeatherFog=1;}if(RenderSettings.sun!=null)RenderSettings.sun.intensity=1.3f;}
        }
        private void UpdateWeatherTexture(bool initial)
        {
            int n=weatherTexture.width;long tick=clock.State.Tick-clock.State.Tick%4;var c=geo.Configuration;
            for(int i=0;i<weatherPixels.Length;i++){var point=ecology!=null?ecology.EnvironmentAt(i).Center:new GeoPoint(((i%n+.5)/n-.5)*c.WidthMeters,((i/n+.5)/n-.5)*c.DepthMeters);var weather=weatherModel!=null?weatherModel.AtRegion(i,tick):RegionalAtmosphere.Forecast(geo,tick,point);var target=new Color(weather.Cloud,weather.Precipitation,weatherModel?.Wetness(i)??0,weather.Temperature);weatherPixels[i]=initial?target:Color.Lerp(weatherPixels[i],target,.35f);}weatherTexture.SetPixels(weatherPixels);weatherTexture.Apply(false);
        }        private void UpdateSeasonal(bool immediate)
        {for(int i=0;i<seasonalPixels.Length;i++){var target=new Color(Mathf.Clamp01(seasons.Snow(i)/1.2f),seasons.Ice(i),(float)(ecology.Temperature(i,clock.State.Tick)+40)/80,seasons.PermanentSnow(i));seasonalPixels[i]=immediate?target:Color.Lerp(seasonalPixels[i],target,.15f);}seasonalTexture.SetPixels(seasonalPixels);seasonalTexture.Apply(false);}
    }
}
