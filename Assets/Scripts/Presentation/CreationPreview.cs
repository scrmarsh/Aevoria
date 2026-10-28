using System;
using System.Threading.Tasks;
using Aevoria.World;
using Aevoria.Core;
using UnityEngine;
namespace Aevoria.Presentation
{
    public sealed class CreationPreview : MonoBehaviour
    {
        private sealed class Result {public Color[] Pixels;public string Metric,Imperial;public string Describe(UnitSystem units)=>units==UnitSystem.Imperial?Imperial:Metric;}
        private Result latest;private UnitSystem units;
        private Task<Result> worker;private Func<Result> request;private string key;private float deadline;
        private int revision,workingRevision;
        private System.Threading.CancellationTokenSource cancellation;
        public Texture2D Texture {get;private set;}
        public string Details {get;private set;}="A new numeric seed will be chosen when you create the world.";
        public bool Pending=>worker!=null||request!=null;
        public void Clear()
        {if(key==null&&request==null&&Texture==null)return;revision++;key=null;request=null;latest=null;cancellation?.Cancel();if(Texture!=null){Destroy(Texture);Texture=null;}Details="A new numeric seed will be chosen when you create the world.";}
        public void Configure(string seed,WorldSize size,GenerationSettings settings,ClimateSettings climate,UnitSystem units=UnitSystem.Metric)
        {
            this.units=units;if(latest!=null)Details=latest.Describe(units);
            string next=seed+"|"+settings.Profile+"|"+settings.AtlasRevision+"|"+settings.Landmass+"|"+size+"|"+settings.Mountains+"|"+settings.Roughness+"|"+settings.SeaLevel+"|"+settings.TargetLandFraction+"|"+climate.TemperatureOffset+"|"+climate.Rainfall+"|"+climate.Latitude;
            if(next==key)return;key=next;revision++;deadline=Time.unscaledTime+.35f;cancellation?.Cancel();cancellation?.Dispose();cancellation=new System.Threading.CancellationTokenSource();var token=cancellation.Token;
            request=()=>Generate(seed,size,settings,climate,token);
        }
        private void Update()
        {
            if(worker!=null&&worker.IsCompleted)
            {if(worker.IsFaulted){var error=worker.Exception.GetBaseException();if(!(error is OperationCanceledException)&&workingRevision==revision)Details="Preview unavailable: "+error.Message;}else if(!worker.IsCanceled&&workingRevision==revision){var result=worker.Result;if(Texture==null)Texture=new Texture2D(96,96,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};Texture.SetPixels(result.Pixels);Texture.Apply(false);latest=result;Details=result.Describe(units);}worker=null;}
            if(worker==null&&request!=null&&Time.unscaledTime>=deadline){var action=request;request=null;workingRevision=revision;worker=Task.Run(action);}
        }
        private static Result Generate(string seed,WorldSize size,GenerationSettings settings,ClimateSettings climate,System.Threading.CancellationToken token)
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset(seed,size,settings),climate,token:token);var pixels=new Color[96*96];double min=100,max=-100,moisture=0,fertility=0;int land=0,forest=0,dry=0,fresh=0;
            for(int z=0;z<96;z++)for(int x=0;x<96;x++)
            {
                var p=new GeoPoint(((x+.5)/96-.5)*geo.Configuration.WidthMeters,((z+.5)/96-.5)*geo.Configuration.DepthMeters);var s=geo.Sample(p);min=Math.Min(min,s.TemperatureCelsius);max=Math.Max(max,s.TemperatureCelsius);
                Color color;if(s.ElevationMeters<geo.SeaLevel)color=Color.Lerp(new Color(.03f,.10f,.20f),new Color(.12f,.42f,.48f),Mathf.Clamp01(1+(s.ElevationMeters-geo.SeaLevel)/160));
                else{land++;moisture+=s.Moisture;fertility+=s.Fertility;if(s.Moisture>.55)forest++;if(s.Moisture<.3)dry++;if(geo.Hydrology.IsLakeWater(p.X,p.Y,s.ElevationMeters)||geo.Hydrology.IsRiverWater(p.X,p.Y,s.ElevationMeters))fresh++;
                    color=Color.Lerp(new Color(.48f,.35f,.15f),new Color(.13f,.36f,.19f),s.Moisture);color=Color.Lerp(color,new Color(.36f,.37f,.35f),Mathf.Clamp01((s.ElevationMeters-80)/Mathf.Max(200,settings.Mountains)));if(s.TemperatureCelsius<0)color=Color.Lerp(color,new Color(.72f,.81f,.86f),Mathf.Clamp01(-s.TemperatureCelsius/12));}
                pixels[z*96+x]=color;
            }
            double landFraction=land/9216.0,wet=moisture/Math.Max(1,land),fertile=fertility/Math.Max(1,land);
            string Describe(UnitSystem units)=>$"Representative sample · actual settings\n{geo.Geology.Arrangement} · {UnitFormatting.Distance(geo.Configuration.WidthMeters,units)}\nLand {landFraction:P0} · water {1-landFraction:P0}\nTemperature {UnitFormatting.Temperature(min,units,"0")} to {UnitFormatting.Temperature(max,units,"0")}\nHumid land {(double)forest/Math.Max(1,land):P0} · arid {(double)dry/Math.Max(1,land):P0}\nFreshwater sample {(double)fresh/Math.Max(1,land):P1}\nHabitat potential {fertile:P0}\nWood: {(wet>.5?"favored":"scarce")} · stone: relief-dependent\nSettlement forecast: {(wet<.35?"dryness is a barrier":settings.Mountains>1800?"mountains restrict routes":"fertile lowlands may help")}\nHeuristic only; no civilizations simulated.\n{(size>=WorldSize.Huge?"Longer preparation; coarse regional detail.":"Local detail streams as you explore.")}";
            return new Result{Pixels=pixels,Metric=Describe(UnitSystem.Metric),Imperial=Describe(UnitSystem.Imperial)};
        }
        private void OnDestroy(){cancellation?.Cancel();cancellation?.Dispose();if(Texture!=null)Destroy(Texture);if(worker!=null)worker.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);}
    }
}
