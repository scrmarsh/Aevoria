using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Aevoria.Core;
using Aevoria.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aevoria.Presentation
{
    public sealed class Alpha009TerrainAudit:MonoBehaviour
    {
        [Serializable]private sealed class View
        {
            public string size,name,seed,fingerprint,biome,mode;public Vector3 geographicCamera,euler;public float focusX,focusZ,ground,range,waterWidth,plateau;
            public int width,height,trees,understorySites,riverNode=-1;public long terrainTriangles;
        }
        [Serializable]private sealed class Evidence
        {
            public string loadedSaveApplicationVersion,loadedSaveFingerprint;
            public string atlasSavedWorldId,atlasReloadFingerprint;
            public int surfaceRecipeVersion=SurfaceMaterialRecipes.Version;
            public int atlasRevision,generationProducts;
            public string surfaceRecipeIdentity=SurfaceMaterialRecipes.Identity();
            public string version=ApplicationVersion.Number;public int generator=ProceduralGeography.GeneratorVersion,geology=GeologyMap.Version,hydrology=HydrologyMap.Version;
            public List<View> views=new List<View>();public List<string> limitations=new List<string>();
        }
        private AevoriaBootstrap app;private string output,sizeName,worldFingerprint;private int renders,errors,inspectedRiver=-1;private readonly Evidence evidence=new Evidence();
        private void Rendered(ScriptableRenderContext context,Camera camera){if(camera.CompareTag("MainCamera"))renders++;}
        private void Failure(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;File.AppendAllText(Path.Combine(output,"failures.txt"),message+"\n"+stack+"\n");Application.Quit(4);}}
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        private IEnumerator Start()
        {
            Require(AevoriaBootstrap.Argument("-aevoriaTestRoot")!=null,"Terrain audit requires isolated saves");output=AevoriaBootstrap.Argument("-aevoriaOutput");Require(!string.IsNullOrEmpty(output),"Explicit evidence output");Directory.CreateDirectory(output);
            app=GetComponent<AevoriaBootstrap>();app.ExternalControl=true;Application.runInBackground=true;
            RenderPipelineManager.endCameraRendering+=Rendered;Application.logMessageReceived+=Failure;yield return null;
            foreach(string shaderName in new[]{"Aevoria/Terrain","Aevoria/Water","Aevoria/Vegetation","Aevoria/Clouds"})
            {var shader=Shader.Find(shaderName);Require(shader!=null&&shader.isSupported,"Native shader support: "+shaderName);}
            var sizes=AevoriaBootstrap.HasFlag("-aevoria009MiniOnly")?new[]{WorldSize.Miniature}:new[]{WorldSize.Miniature,WorldSize.Colossal};
            if(Enum.TryParse(AevoriaBootstrap.Argument("-aevoria009Size"),out WorldSize requestedSize)){Require(Enum.IsDefined(typeof(WorldSize),requestedSize),"Supported audit size");sizes=new[]{requestedSize};}
            string seed=AevoriaBootstrap.Argument("-aevoria009Seed")??"shaping-continents";
            foreach(var size in sizes)
            {
                app.ReturnToMenu();yield return null;yield return null;sizeName=size.ToString();
                float relief=AevoriaBootstrap.HasFlag("-aevoria009HighRelief")?12686.6143f:3600;
                if(float.TryParse(AevoriaBootstrap.Argument("-aevoria011Relief"),NumberStyles.Float,CultureInfo.InvariantCulture,out float explicitRelief))relief=explicitRelief;
                string savedId=AevoriaBootstrap.Argument("-aevoria014LoadBaseline");
                int atlasRevision=int.TryParse(AevoriaBootstrap.Argument("-aevoria014AtlasRevision"),out int requestedRevision)?requestedRevision:0;
                if(!string.IsNullOrEmpty(savedId))
                {
                    Require(Guid.TryParseExact(savedId,"N",out _),"Saved-world fixture identity");
                    var saved=Aevoria.Persistence.ArchiveCodec.Unpack(File.ReadAllBytes(Path.Combine(app.Repository.Root,savedId,"world.xml")));
                    Require(saved.ApplicationVersion==(AevoriaBootstrap.HasFlag("-aevoria014Atlas")?"0.0.14":"0.0.13")&&saved.Seed==seed,"Explicit saved-world fixture matches the requested review");
                    Require(saved.AtlasRevision==atlasRevision,"Fixture retains the explicitly requested Atlas terrain revision");
                    evidence.loadedSaveApplicationVersion=saved.ApplicationVersion;evidence.loadedSaveFingerprint=saved.Fingerprint;
                    app.LoadWorld(savedId);
                }
                else app.CreateWorld("Natural terrain review "+sizeName,seed,size,new GenerationSettings(mountains:relief,landmass:AevoriaBootstrap.HasFlag("-aevoria014Atlas")?LandmassType.Random:LandmassType.Continents,mountainPrototypeOnly:AevoriaBootstrap.HasFlag("-aevoria011MountainPrototype"),profile:AevoriaBootstrap.HasFlag("-aevoria014Atlas")?GeographyProfile.Atlas:GeographyProfile.Legacy,atlasRevision:atlasRevision),new ClimateSettings());
                // The generation pipeline allows ten minutes and reports live
                // progress/cancellation. Give publication a minute of headroom;
                // the audit must not misreport a progressing large world as hung.
                float end=Time.realtimeSinceStartup+660;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Require(app.WorldActive&&!app.Busy,"Created terrain world "+sizeName);app.Clock.Pause();app.Controller.ExternalDriver=true;
                var settings=app.Settings.Copy();settings.AutosaveSeconds=0;settings.CloudQuality=0;settings.RenderClouds=false;settings.RenderWeatherEffects=false;settings.MotionBlur=false;app.ApplySettings(settings,false);
                var geo=app.Terrain.Geography;evidence.atlasRevision=geo.Configuration.Terrain.AtlasRevision;evidence.generationProducts=geo.Layers.Ids.Count;worldFingerprint=Aevoria.Persistence.WorldRecord.GeographyFingerprint(geo);var hydro=geo.Hydrology;int peak=0,river=-1;for(int i=0;i<hydro.Ground.Length;i++)
                {
                    if(hydro.Ground[i]>hydro.Ground[peak])peak=i;
                    if(!hydro.IsRiver(i)||river>=0&&hydro.Discharge[i]<=hydro.Discharge[river]||geo.InlandSurfaces.IsSeasonallyDryChannel(i))continue;
                    hydro.PathPoint(i,hydro.PathSamples(i)/2,out double x,out double z);
                    if(geo.BaseElevationAt(x,z)>geo.SeaLevel+2&&hydro.PathLevel(i,.5)>geo.SeaLevel+.1f&&!geo.InlandSurfaces.LakeAt(x,z,out _))river=i;
                }
                if(AevoriaBootstrap.HasFlag("-aevoria014InterfaceOnly"))
                {
                    yield return InterfaceReview(peak);File.WriteAllText(Path.Combine(output,"views.json"),JsonUtility.ToJson(evidence,true));
                    Require(errors==0,"Interface review without runtime errors");app.ReturnToMenu();yield return null;Debug.Log("AEVORIA014_INTERFACE_REVIEW_COMPLETE");Application.Quit();yield break;
                }
                if(AevoriaBootstrap.HasFlag("-aevoria014Atlas")&&!AevoriaBootstrap.HasFlag("-aevoria009WaterOnly")&&!AevoriaBootstrap.HasFlag("-aevoria014MaterialsOnly"))
                {
                    Require(geo.Configuration.Terrain.Profile==GeographyProfile.Atlas,"Atlas profile generated");
                    if(app.Society.Settlements.Count>0)
                    {
                        var village=app.Society.Settlements[0];Require(app.Explorer.EnterAt(new GeoPoint(village.X+1500,village.Z)),"Atlas explorer spawn");
                        app.Living.Traveler.Yaw=270;app.DiagnosticsVisible=true;yield return null;yield return Capture("atlas-explorer-f3-west");app.DiagnosticsVisible=false;
                        app.Explorer.Exit();
                    }
                    app.Controller.SetView(new GeoPoint(0,0),0,89.5f,(float)geo.Configuration.WidthMeters*1.1f);yield return Ready();yield return Capture("atlas-top-down");
                    app.Settings.MinimapEnabled=true;app.Navigation.Toggle();
                    float deadline=Time.realtimeSinceStartup+120;while(!app.Navigation.AtlasReady&&Time.realtimeSinceStartup<deadline)yield return null;
                    Require(app.Navigation.ShowsWholeWorld,"Both map panels have fixed world extent and north-up orientation");
                    yield return Capture("atlas-world-map");
                    app.Controller.SetView(new GeoPoint(geo.Configuration.WidthMeters*.1,geo.Configuration.DepthMeters*.1),135,50,2200);yield return Ready();
                    Require(app.Navigation.ShowsWholeWorld,"Map panels remain fixed after camera movement and rotation");yield return Capture("atlas-map-after-camera-move");
                    app.Navigation.Toggle();app.DiagnosticsVisible=true;yield return Capture("atlas-f3-facing");app.DiagnosticsVisible=false;
                    foreach(var biome in new[]{Biome.MediterraneanWoodland,Biome.Oasis,Biome.RiparianForest,Biome.BorealForest,Biome.Forest,Biome.Desert,Biome.SaltFlat,Biome.Snow,Biome.RockyCoast})
                    {
                        int region=Best((p,h,s)=>h<=5||s.Biome!=biome?-1:1/(.01+Slope(p,128)));
                        if(region<0){evidence.limitations.Add("Atlas: no sampled "+biome);continue;}
                        Focus(region,1600,55);yield return Ready();yield return Capture("atlas-"+biome);
                        Focus(region,100,25);yield return Ready();yield return Capture("atlas-"+biome+"-close");
                        if(biome==Biome.MediterraneanWoodland||biome==Biome.BorealForest||biome==Biome.Desert||biome==Biome.SaltFlat||biome==Biome.Snow)
                        {
                            var centre=app.Ecology.EnvironmentAt(region).Center;
                            if(app.Explorer.EnterAt(centre))
                            {
                                app.Explorer.SetFirstPerson(true);app.Living.Traveler.Yaw=25;app.Living.Traveler.Pitch=28;
                                yield return Ready();float groundDeadline=Time.realtimeSinceStartup+120;
                                while(!app.Explorer.Ready&&Time.realtimeSinceStartup<groundDeadline)yield return null;
                                Require(app.Explorer.Ready,"Actual ground-level Explorer geometry ready");
                                float coverDeadline=Time.realtimeSinceStartup+60;while(!app.Terrain.Vegetation.NearCoverReady&&Time.realtimeSinceStartup<coverDeadline)yield return null;
                                Require(app.Terrain.Vegetation.NearCoverReady,"Ground-level cosmetic understory preparation completed");
                                yield return Capture("atlas-"+biome+"-ground");app.Explorer.Exit();
                            }
                            else evidence.limitations.Add("Atlas: no safe Explorer spawn near "+biome);
                        }
                    }
                    yield return RegionalPresentation();
                    if(AevoriaBootstrap.HasFlag("-aevoria011Contacts"))yield return RegionalContacts(peak);
                    // Exercise the real save/load path only inside the explicitly
                    // isolated audit root. Creation alone does not establish that
                    // an Atlas profile survives a production save and reload.
                    evidence.atlasSavedWorldId=app.Current.Id;
                    app.SaveWorld(newName:"Atlas production reload fixture");float saveDeadline=Time.realtimeSinceStartup+120;
                    while(app.Busy&&Time.realtimeSinceStartup<saveDeadline)yield return null;
                    Require(!app.Busy,"Atlas production save completed within audit budget");
                    var savedAtlas=Aevoria.Persistence.ArchiveCodec.Unpack(File.ReadAllBytes(Path.Combine(app.Repository.Root,evidence.atlasSavedWorldId,"world.xml")));
                    Require(savedAtlas.Name=="Atlas production reload fixture"&&savedAtlas.GeographyProfile==GeographyProfile.Atlas&&savedAtlas.AtlasRevision==atlasRevision&&savedAtlas.Fingerprint==worldFingerprint,"Production save wrote the new fixture name, Atlas profile/revision and geography fingerprint");
                    app.ReturnToMenu();yield return null;app.LoadWorld(evidence.atlasSavedWorldId);
                    float reloadDeadline=Time.realtimeSinceStartup+660;
                    while(app.Busy&&Time.realtimeSinceStartup<reloadDeadline)yield return null;
                    Require(app.WorldActive&&!app.Busy,"Atlas production reload completed");
                    evidence.atlasReloadFingerprint=Aevoria.Persistence.WorldRecord.GeographyFingerprint(app.Terrain.Geography);
                    Require(app.Terrain.Geography.Configuration.Terrain.Profile==GeographyProfile.Atlas&&app.Terrain.Geography.Configuration.Terrain.AtlasRevision==atlasRevision&&evidence.atlasReloadFingerprint==worldFingerprint,"Atlas production reload preserves profile/revision and canonical geography");
                    app.Clock.Pause();app.Controller.ExternalDriver=true;
                    app.Controller.SetView(new GeoPoint(0,0),0,89.5f,(float)app.Terrain.Geography.Configuration.WidthMeters*1.1f);yield return Ready();yield return Capture("atlas-reloaded-top-down");
                    File.WriteAllText(Path.Combine(output,"views.json"),JsonUtility.ToJson(evidence,true));
                    Require(errors==0,"Atlas native review without runtime errors");app.ReturnToMenu();yield return null;Debug.Log("AEVORIA014_ATLAS_REVIEW_COMPLETE");Application.Quit();yield break;
                }
                if(AevoriaBootstrap.HasFlag("-aevoria011MountainPrototype"))
                {
                    Require(geo.Mountains.Clusters.Count==1,"One canonical mountain prototype");var formation=geo.Mountains.Clusters[0];
                    var centre=new GeoPoint(formation.X,formation.Z);
                    app.Controller.SetView(centre,25,65,(float)formation.Radius*3.2f);yield return Ready();yield return Capture("prototype-overview");
                    app.Controller.SetView(centre,25,28,(float)formation.Radius*2.4f);yield return Ready();yield return Capture("prototype-oblique");
                    var foot=new GeoPoint(formation.X+formation.Radius*.65,formation.Z);
                    app.Controller.SetView(foot,270,24,5000);yield return Ready();yield return Capture("prototype-foothill");
                    var steep=new GeoPoint(formation.X+formation.Radius*.15,formation.Z);
                    app.Controller.SetView(steep,270,20,1800);yield return Ready();yield return Capture("prototype-steep");
                    app.Controller.SetView(centre,205,25,(float)formation.Radius*5);yield return Ready();yield return Capture("prototype-distant");
                    // Coastal support can shift the physical summit away from
                    // the authored formation centre. Review and probe the real
                    // maximum rather than assuming that metadata is its height.
                    var highest=centre;float highestGround=geo.ElevationAt(highest.X,highest.Y);double spacing=formation.Radius/24;
                    for(int z=-24;z<=24;z++)for(int x=-24;x<=24;x++){double ex=formation.X+x*spacing,nz=formation.Z+z*spacing;float ground=geo.ElevationAt(ex,nz);if(ground>highestGround){highestGround=ground;highest=new GeoPoint(ex,nz);}}
                    for(int pass=0;pass<4;pass++){var around=highest;spacing*=.25;for(int z=-4;z<=4;z++)for(int x=-4;x<=4;x++){double ex=around.X+x*spacing,nz=around.Y+z*spacing;float ground=geo.ElevationAt(ex,nz);if(ground>highestGround){highestGround=ground;highest=new GeoPoint(ex,nz);}}}
                    File.WriteAllText(Path.Combine(output,"actual-summit.csv"),"x,z,physicalHeight\n"+string.Format(CultureInfo.InvariantCulture,"{0},{1},{2}\n",highest.X,highest.Y,highestGround));
                    app.Controller.SetView(highest,25,42,Mathf.Max(3500,highestGround*.35f));yield return Ready();yield return Capture("prototype-actual-summit");
                    if(AevoriaBootstrap.HasFlag("-aevoria011Motion"))yield return PrototypeMotion(formation);
                    if(AevoriaBootstrap.HasFlag("-aevoria011Contacts"))yield return PrototypeContacts(formation,highest);
                    File.WriteAllText(Path.Combine(output,"views.json"),JsonUtility.ToJson(evidence,true));
                    Require(errors==0,"Prototype native captures without runtime errors");app.ReturnToMenu();yield return null;Debug.Log("AEVORIA011_MOUNTAIN_PROTOTYPE_COMPLETE");Application.Quit();yield break;
                }
                if(int.TryParse(AevoriaBootstrap.Argument("-aevoria009RiverNode"),out int requestedRiver)){Require(requestedRiver>=0&&requestedRiver<hydro.Ground.Length,"Valid requested drainage node");river=requestedRiver;if(!hydro.IsChannelCandidate(river))evidence.limitations.Add(sizeName+": historical drainage node "+river+" is no longer an active channel; capture retains its coordinates.");}
                if(AevoriaBootstrap.HasFlag("-aevoria009CoastOnly")){yield return Coast();File.WriteAllText(Path.Combine(output,"views.json"),JsonUtility.ToJson(evidence,true));app.ReturnToMenu();yield return null;Debug.Log("AEVORIA009_COAST_REVIEW_COMPLETE");Application.Quit();yield break;}
                if(AevoriaBootstrap.HasFlag("-aevoria009WaterOnly")){yield return River(river);File.WriteAllText(Path.Combine(output,"views.json"),JsonUtility.ToJson(evidence,true));Require(errors==0,"Water review without runtime errors");app.ReturnToMenu();yield return null;Debug.Log("AEVORIA009_WATER_REVIEW_COMPLETE");Application.Quit();yield break;}
                if(AevoriaBootstrap.HasFlag("-aevoria014MaterialsOnly"))
                {
                    yield return Materials(peak);File.WriteAllText(Path.Combine(output,"views.json"),JsonUtility.ToJson(evidence,true));
                    Require(errors==0,"Material review without runtime errors");app.ReturnToMenu();yield return null;Debug.Log("AEVORIA014_MATERIAL_REVIEW_COMPLETE");Application.Quit();yield break;
                }
                app.Controller.SetView(new GeoPoint(0,0),0,65,(float)geo.Configuration.WidthMeters*1.05f);yield return Ready();yield return Capture("overview");
                bool climateOnly=AevoriaBootstrap.HasFlag("-aevoria013ClimateOnly");
                if(!climateOnly)
                {
                var mountain=new GeoPoint(hydro.X(peak),hydro.Y(peak));app.Controller.SetView(mountain,25,45,22000);yield return Ready();yield return Capture("mountain-range");yield return Measure("mountain-range");
                app.Controller.SetView(mountain,25,42,3500);yield return Ready();foreach(int diagnostic in new[]{0,1,2,3}){Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",diagnostic);yield return Capture("mountain-diagnostic-"+diagnostic);}Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",0);
                int plain=Best((p,h,s)=>h>5&&s.Biome==Biome.Grassland?1/(.001+Slope(p,256)):-1);
                if(plain<0)plain=Best((p,h,s)=>h>10&&h<900?1/(.001+Slope(p,256)):-1);
                if(plain>=0){Focus(plain,1800,50);yield return Ready();yield return Capture("plain");yield return Measure("plain");}
                int table=Best((p,h,s)=>h>150?geo.OrdinaryTerrain.PlateauAt(p.X,p.Y)/(.01+Slope(p,500)):-1);
                if(table>=0){Focus(table,7000,48);yield return Ready();yield return Capture("plateau-region");Focus(table,1800,50);yield return Ready();yield return Capture("plateau-surface");}
                }
                foreach(var biome in new[]{Biome.Desert,Biome.Mediterranean,Biome.Forest,Biome.TemperateConiferousForest,Biome.BorealForest,Biome.Tundra})
                {
                    bool woodland=biome==Biome.Forest||biome==Biome.TemperateConiferousForest||biome==Biome.BorealForest;
                    // A constant score selected the first matching raster cell,
                    // often a bare cliff or isolated altitude-cooled southern
                    // patch. Inspect supported stands and northern tundra.
                    int region=Best((p,h,s)=>h<=5||s.Biome!=biome?-1:
                        woodland?(.01+geo.Ecosystem.ForestPotential(p.X,p.Y))/(1+Slope(p,128)*6):
                        biome==Biome.Tundra?.5+p.Y/geo.Configuration.DepthMeters+.2/(1+h*.001):1/(.01+Slope(p,128)));
                    if(region<0){evidence.limitations.Add(sizeName+": no "+biome+" inspection centre");continue;}
                    Focus(region,2400,50);yield return Ready();yield return Capture(biome.ToString());
                    if(woodland){Focus(region,180,25);yield return Ready();yield return Capture(biome+"-close");}
                }
                if(!climateOnly)
                {
                if(river>=0)yield return River(river);
                var lakes=geo.InlandSurfaces.Lakes.Vertices;if(lakes.Count>0){int deepest=0;for(int i=1;i<lakes.Count;i++)if(lakes[i].Depth>lakes[deepest].Depth)deepest=i;var p=lakes[deepest];app.Controller.SetView(new GeoPoint(p.X,p.Z),25,50,2500);yield return Ready();yield return Capture("lake");}
                yield return Coast();
                }
                File.WriteAllText(Path.Combine(output,"views.json"),JsonUtility.ToJson(evidence,true));
            }
            Require(errors==0&&renders>(AevoriaBootstrap.HasFlag("-aevoria013ClimateOnly")?100:1000),"Rendered terrain audit without runtime errors");app.ReturnToMenu();yield return null;Debug.Log("AEVORIA009_TERRAIN_REVIEW_COMPLETE");Application.Quit();
        }
        private IEnumerator River(int river)
        {Require(river>=0,"A drainage inspection node exists");inspectedRiver=river;var hydro=app.Terrain.Geography.Hydrology;hydro.PathPoint(river,hydro.PathSamples(river)/2,out double x,out double z);app.Controller.SetView(new GeoPoint(x,z),25,50,Mathf.Max(1800,hydro.RiverWidth(river)*5));yield return Ready();yield return Capture("major-river",hydro.RiverWidth(river));yield return Measure("major-river");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",1);yield return Capture("major-river-opaque-diagnostic");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",2);yield return Capture("major-river-depth-diagnostic");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",3);yield return Capture("major-river-alpha-diagnostic");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",0);yield return Capture("major-river-restored");foreach(var renderer in app.Waters.GetComponentsInChildren<MeshRenderer>())renderer.forceRenderingOff=true;yield return Capture("major-river-without-water");var ocean=app.Terrain.transform.Find("Ocean horizon").GetComponent<MeshRenderer>();ocean.forceRenderingOff=true;yield return Capture("major-river-without-any-water");ocean.forceRenderingOff=false;foreach(var renderer in app.Waters.GetComponentsInChildren<MeshRenderer>())renderer.forceRenderingOff=false;app.Controller.SetView(new GeoPoint(x,z),25,30,180);yield return Ready();yield return Capture("major-river-close");app.Controller.SetView(new GeoPoint(x,z),205,40,400);yield return Ready();yield return Capture("major-river-reverse");if(AevoriaBootstrap.HasFlag("-aevoria011WaterZoom"))yield return RiverZoom(new GeoPoint(x,z));inspectedRiver=-1;}
        private IEnumerator Materials(int peak)
        {
            var g=app.Terrain.Geography;var h=g.Hydrology;var mountain=new GeoPoint(h.X(peak),h.Y(peak));
            app.Controller.SetView(mountain,25,45,22000);yield return Ready();yield return Capture("materials-mountain-high");yield return Measure("materials-mountain-high");
            app.Controller.SetView(mountain,25,42,3500);yield return Ready();yield return Capture("materials-mountain-medium");
            var cliff=mountain;double best=-1;
            for(int z=-12;z<=12;z++)for(int x=-12;x<=12;x++)
            {
                var p=new GeoPoint(mountain.X+x*600,mountain.Y+z*600);if(!g.Configuration.Contains(p))continue;
                float height=g.ElevationAt(p.X,p.Y)-g.SeaLevel;double slope=Slope(p,32);
                if(height<300||height>3500||slope<.3||slope>2)continue;
                double score=Math.Min(1,slope)/(1+height/3000);if(score>best){best=score;cliff=p;}
            }
            Require(best>0,"Physical exposed mountain cliff for material inspection");
            double dx=g.ElevationAt(cliff.X+32,cliff.Y)-g.ElevationAt(cliff.X-32,cliff.Y),dz=g.ElevationAt(cliff.X,cliff.Y+32)-g.ElevationAt(cliff.X,cliff.Y-32);
            float yaw=(float)(Math.Atan2(dx,dz)*180/Math.PI);
            foreach(float range in new[]{600f,140f,45f})
            {app.Controller.SetView(cliff,yaw,22,range);yield return Ready();yield return Capture("materials-cliff-"+range.ToString("F0",CultureInfo.InvariantCulture));}
            Shader.SetGlobalFloat("_GenesisReady",0);yield return Capture("materials-cliff-legacy");yield return Measure("materials-cliff-legacy");
            Shader.SetGlobalFloat("_GenesisReady",1);yield return Capture("materials-cliff-genesis");yield return Measure("materials-cliff-genesis");
            app.Controller.SetView(cliff,yaw+45,22,45);yield return Ready();yield return Capture("materials-cliff-rotated");
            Shader.SetGlobalFloat("_GenesisReady",0);yield return Capture("materials-rotated-legacy");
            foreach(int diagnostic in new[]{1,2,3}){Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",diagnostic);yield return Capture("materials-rotated-legacy-diagnostic-"+diagnostic);}
            Shader.SetGlobalFloat("_GenesisReady",1);
            File.WriteAllText(Path.Combine(output,"normal-field.txt"),"ready="+Shader.GetGlobalFloat("_AevoriaRegionalReady")+"\nrect="+Shader.GetGlobalVector("_AevoriaRegionalRect").ToString("F4")+"\ngrid="+Shader.GetGlobalVector("_AevoriaRegionalGrid").ToString("F4")+"\norigin="+Shader.GetGlobalVector("_AevoriaOrigin").ToString("F4"));
            foreach(int diagnostic in new[]{3,6,7,8,9,10,11,12,13,14,15,16,17}){Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",diagnostic);yield return Capture("materials-rotated-genesis-diagnostic-"+diagnostic);}
            Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",0);
            var ambientFeatures=new List<UnityEngine.Rendering.Universal.ScriptableRendererFeature>();
            foreach(var rendererData in Resources.FindObjectsOfTypeAll<UnityEngine.Rendering.Universal.ScriptableRendererData>())
                foreach(var feature in rendererData.rendererFeatures)if(feature!=null&&feature.isActive&&feature.GetType().Name.IndexOf("AmbientOcclusion",StringComparison.OrdinalIgnoreCase)>=0)ambientFeatures.Add(feature);
            File.WriteAllText(Path.Combine(output,"ambient-features.txt"),"Active ambient-occlusion features: "+ambientFeatures.Count);
            Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",18);yield return Capture("materials-constant-white");
            try
            {
                foreach(var feature in ambientFeatures)feature.SetActive(false);yield return null;
                yield return Capture("materials-constant-white-without-ao");
                Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",14);yield return Capture("materials-normal-without-ao");
                Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",0);yield return Capture("materials-lit-without-ao");
            }
            finally{foreach(var feature in ambientFeatures)feature.SetActive(true);Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",0);}
            var sun=RenderSettings.sun;
            if(sun!=null)
            {
                var shadows=sun.shadows;float depthBias=sun.shadowBias,normalBias=sun.shadowNormalBias;
                var sunData=sun.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();bool pipelineBias=sunData!=null&&sunData.usePipelineSettings;
                sun.shadows=LightShadows.None;yield return null;yield return Capture("materials-rotated-no-shadows");
                sun.shadows=shadows;if(sunData!=null)sunData.usePipelineSettings=false;
                sun.shadowBias=1;sun.shadowNormalBias=2;yield return null;yield return Capture("materials-rotated-shadow-bias-2");
                sun.shadowBias=.3f;sun.shadowNormalBias=.5f;yield return null;yield return Capture("materials-rotated-shadow-depth-03");
                sun.shadowBias=.3f;sun.shadowNormalBias=1;yield return null;yield return Capture("materials-rotated-shadow-normal-1");
                sun.shadowBias=depthBias;sun.shadowNormalBias=normalBias;if(sunData!=null)sunData.usePipelineSettings=pipelineBias;
            }
            app.Controller.SetView(new GeoPoint(cliff.X+48,cliff.Y+48),yaw,22,45);yield return Ready();yield return Capture("materials-cliff-pan");
            double boundary=Math.Floor(cliff.X/g.Configuration.ChunkSizeMeters)*g.Configuration.ChunkSizeMeters;
            foreach(double side in new[]{-4.0,4.0})
            {app.Controller.SetView(new GeoPoint(boundary+side,cliff.Y),yaw,22,45);yield return Ready();yield return Capture(side<0?"materials-chunk-left":"materials-chunk-right");}
        }
        private IEnumerator InterfaceReview(int peak)
        {
            var h=app.Terrain.Geography.Hydrology;var focus=new GeoPoint(h.X(peak),h.Y(peak));
            foreach(float yaw in new[]{0f,90f,180f,270f})
            {app.Controller.SetView(focus,yaw,42,3500);yield return Ready();app.DiagnosticsVisible=true;yield return Capture("interface-compass-"+yaw);}
            app.DiagnosticsVisible=false;app.Menu.Show(ApplicationPage.Pause);
            float deadline=Time.realtimeSinceStartup+10;while(!app.Menu.PauseBackdropReady&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(app.Menu.PauseBackdropReady,"Pause captures the current world frame");yield return Capture("interface-pause");
            app.Menu.Show(ApplicationPage.Settings);yield return Capture("interface-settings");
            foreach(var page in new[]{ApplicationPage.Graphics,ApplicationPage.Audio,ApplicationPage.Controls,ApplicationPage.Bindings,ApplicationPage.Display,ApplicationPage.WorldTools})
            {app.Menu.Show(ApplicationPage.Settings);app.Menu.Show(page);yield return Capture("interface-"+page);}
            app.Menu.Show(ApplicationPage.Advancement);yield return Capture("interface-advancement");
            app.Menu.Show(ApplicationPage.Pause);string id=app.Current.Id;
            app.Menu.SaveAndQuitToMainMenu();deadline=Time.realtimeSinceStartup+120;
            while(app.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(!app.Busy&&!app.WorldActive&&app.Menu.Page==ApplicationPage.Home,"Save & Quit saved successfully and returned to the main menu while the player remains running");
            var saved=Aevoria.Persistence.ArchiveCodec.Unpack(File.ReadAllBytes(Path.Combine(app.Repository.Root,id,"world.xml")));
            Require(saved.Id==id&&saved.Fingerprint==worldFingerprint,"Save & Quit persisted the current world's canonical identity");
            yield return new WaitForEndOfFrame();var home=ScreenCapture.CaptureScreenshotAsTexture();Require(home!=null,"Main menu after Save & Quit");File.WriteAllBytes(Path.Combine(output,sizeName+"-interface-saved-home.png"),home.EncodeToPNG());Destroy(home);
            app.LoadWorld(id);deadline=Time.realtimeSinceStartup+660;while(app.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(app.WorldActive&&!app.Busy&&Aevoria.Persistence.WorldRecord.GeographyFingerprint(app.Terrain.Geography)==worldFingerprint,"Reload after Save & Quit preserves the world");
            app.Clock.Pause();app.Controller.ExternalDriver=true;app.Controller.SetView(focus,270,42,3500);yield return Ready();yield return Capture("interface-reloaded");
        }
        private IEnumerator RiverZoom(GeoPoint centre)
        {
            app.Controller.SetView(centre,25,55,180);yield return Ready();
            using(var csv=new StreamWriter(Path.Combine(output,"water-zoom-without-capture.csv")))
            {
                csv.WriteLine("frame,range,terrainReady,engineLoopMs,residentTriangles");
                for(int frame=0;frame<360;frame++)
                {
                    float range=180*Mathf.Pow(14000f/180,frame/359f);app.Controller.SetView(centre,25,55,range);yield return null;
                    csv.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4}",frame,range,app.Terrain.IsReady,Time.unscaledDeltaTime*1000,app.Terrain.ResidentTriangles));
                }
            }
            foreach(float range in new[]{180f,400,900,1800,3500,7000,14000,28000,48000})
            {
                app.Controller.SetView(centre,25,55,range);yield return Ready();
                yield return Capture("river-zoom-"+range.ToString("F0",CultureInfo.InvariantCulture));
            }
            string directory=Path.Combine(output,"water-motion");Directory.CreateDirectory(directory);
            using(var csv=new StreamWriter(Path.Combine(directory,"frames.csv")))
            {
                csv.WriteLine("frame,range,cameraX,cameraY,cameraZ,terrainReady,engineLoopMs,residentTriangles");
                for(int frame=0;frame<180;frame++)
                {
                    float range=180*Mathf.Pow(14000f/180,frame/179f);app.Controller.SetView(centre,25,55,range);
                    yield return new WaitForEndOfFrame();var p=WorldOrigin.Geographic(Camera.main.transform);
                    csv.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7}",frame,range,p.x,p.y,p.z,app.Terrain.IsReady,Time.unscaledDeltaTime*1000,app.Terrain.ResidentTriangles));
                    if(frame%6==0)ScreenCapture.CaptureScreenshot(Path.Combine(directory,"zoom-"+frame.ToString("D3")+".png"));yield return null;
                }
            }
        }
        private IEnumerator PrototypeMotion(MountainCluster formation)
        {
            var centre=new GeoPoint(formation.X,formation.Z);string directory=Path.Combine(output,"motion");Directory.CreateDirectory(directory);
            using(var csv=new StreamWriter(Path.Combine(directory,"frames.csv")))
            {
                csv.WriteLine("frame,yaw,cameraX,cameraY,cameraZ,terrainReady,engineLoopMs,residentTriangles");
                for(int frame=0;frame<180;frame++)
                {
                    float yaw=25+frame*.5f;app.Controller.SetView(centre,yaw,22,(float)formation.Radius*1.5f);
                    yield return new WaitForEndOfFrame();var p=WorldOrigin.Geographic(Camera.main.transform);
                    csv.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7}",frame,yaw,p.x,p.y,p.z,app.Terrain.IsReady,Time.unscaledDeltaTime*1000,app.Terrain.ResidentTriangles));
                    if(frame%6==0)ScreenCapture.CaptureScreenshot(Path.Combine(directory,"orbit-"+frame.ToString("D3")+".png"));
                    yield return null;
                }
            }
        }
        private IEnumerator PrototypeContacts(MountainCluster formation,GeoPoint? actualSummit=null)
        {
            var geo=app.Terrain.Geography;
            using(var csv=new StreamWriter(Path.Combine(output,"mountain-physics.csv")))
            {
                csv.WriteLine("peak,sample,x,z,canonicalHeight,colliderHeight,errorMeters");
                for(int peak=0;peak<formation.Peaks.Count+(actualSummit.HasValue?1:0);peak++)
                {
                    double sx=peak<formation.Peaks.Count?formation.Peaks[peak].X:actualSummit.Value.X,sz=peak<formation.Peaks.Count?formation.Peaks[peak].Z:actualSummit.Value.Y;
                    double centreX=System.Math.Floor(sx/16)*16,centreZ=System.Math.Floor(sz/16)*16;
                    var source=GroundContactPatch.Sample(geo,centreX,centreZ);int n=GroundContactPatch.Resolution,row=n+1;
                    var vertices=new Vector3[source.Length];for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3((float)(source[i].X-centreX),(float)source[i].Y,(float)(source[i].Z-centreZ));
                    var indices=new int[n*n*6];int t=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*row+x;indices[t++]=a;indices[t++]=a+row;indices[t++]=a+1;indices[t++]=a+1;indices[t++]=a+row;indices[t++]=a+row+1;}
                    var mesh=new Mesh{name="Native mountain contact probe"};mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateBounds();
                    var probe=new GameObject("Mountain contact probe");probe.layer=31;probe.transform.position=WorldOrigin.Relative(new Vector3((float)centreX,0,(float)centreZ));probe.AddComponent<MeshCollider>().sharedMesh=mesh;Physics.SyncTransforms();
                    for(int sample=0;sample<9;sample++)
                    {
                        double x=centreX-12+(sample%3)*10+.37,z=centreZ-12+(sample/3)*10+.63,height=LocalSolidField.RenderedGround(geo,x,z);
                        var origin=WorldOrigin.Relative(new Vector3((float)x,(float)height+50,(float)z));
                        Require(Physics.Raycast(origin,Vector3.down,out var hit,100,1<<31,QueryTriggerInteraction.Ignore),"Native contact exists at canonical mountain altitude");
                        double error=System.Math.Abs(hit.point.y-height);csv.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6}",peak,sample,x,z,height,hit.point.y,error));
                        Require(error<.08,"Mountain physics agrees with canonical finest terrain within 8 cm");
                    }
                    Destroy(probe);Destroy(mesh);yield return null;
                }
            }
        }
        private IEnumerator RegionalPresentation()
        {
            var geo=app.Terrain.Geography;
            if(geo.Surface.Regional!=null)
            {
                int dunes=Best((p,h,s)=>s.Biome!=Biome.Desert?-1:Math.Abs(geo.Surface.Regional.DuneOffsetAt(p.X,p.Y,h)));
                if(dunes>=0)
                {
                    var p=app.Ecology.EnvironmentAt(dunes).Center;
                    foreach(float range in new[]{1800f,350f}){app.Controller.SetView(p,25,18,range);yield return Ready();yield return Capture("atlas-physical-dunes-"+range);}
                }
            }
            if(app.Society.Settlements.Count==0){evidence.limitations.Add("Regional presentation: no settlement for paths/cloud inspection");yield break;}
            var village=app.Society.Settlements[0];var centre=new GeoPoint(village.X,village.Z);
            app.Controller.SetView(centre,25,65,450);yield return Ready();float deadline=Time.realtimeSinceStartup+120;
            while(app.Settlements.Paths.Network(village.Id)==null&&Time.realtimeSinceStartup<deadline)yield return null;
            var network=app.Settlements.Paths.Network(village.Id);Require(network!=null,"Native connected resident path network prepared");
            using(var csv=new StreamWriter(Path.Combine(output,"resident-paths.csv")))
            {
                csv.WriteLine("from,to,point,x,z");foreach(var path in network.Paths)for(int i=0;i<path.Points.Count;i++)csv.WriteLine(FormattableString.Invariant($"{path.From},{path.To},{i},{path.Points[i].X:R},{path.Points[i].Y:R}"));
            }
            yield return Capture("atlas-village-paths-overhead");app.Controller.SetView(centre,110,30,180);yield return Ready();yield return Capture("atlas-village-paths-oblique");
            var original=app.Settings.Copy();var settings=original.Copy();settings.CloudQuality=2;settings.CloudDensity=3;settings.RenderClouds=true;settings.RenderWeatherEffects=false;app.ApplySettings(settings,false);
            string weather=Aevoria.Persistence.ArchiveCodec.Hash(Aevoria.Persistence.ArchiveCodec.Encode(app.Living.Atmosphere.Capture()));
            Vector4[] anchors=null,shapes=null;
            foreach(float range in new[]{2000f,20000f,60000f})
            {
                app.Controller.SetView(centre,25,55,range);yield return Ready();yield return Capture("atlas-clouds-"+range);
                var current=Shader.GetGlobalVectorArray("_AevoriaCloudSystems");var currentShapes=Shader.GetGlobalVectorArray("_AevoriaCloudShapes");
                if(anchors==null){anchors=current;shapes=currentShapes;}else for(int i=0;i<current.Length;i++){Require(anchors[i]==current[i]&&shapes[i]==currentShapes[i],"Cloud formation identity and density remain fixed through zoom while paused");}
            }
            var environment=app.GetComponent<WorldEnvironment>();if(environment.VisibleCloudSystems==0)evidence.limitations.Add("Actual paused weather has no visible cloud system at the inspection centre");
            settings.RenderClouds=false;app.ApplySettings(settings,false);yield return Capture("atlas-clouds-disabled");
            Require(environment.VisibleCloudSystems==0,"Cloud rendering toggle disables visible systems");
            Require(weather==Aevoria.Persistence.ArchiveCodec.Hash(Aevoria.Persistence.ArchiveCodec.Encode(app.Living.Atmosphere.Capture())),"Cloud rendering changes preserve the simulated weather state");
            settings.RenderClouds=true;app.ApplySettings(settings,false);yield return Capture("atlas-clouds-restored");app.Clock.SetSpeed(8);app.Clock.Start();
            using(var csv=new StreamWriter(Path.Combine(output,"atlas-cloud-motion.csv")))
            {
                csv.WriteLine("frame,tick,presentedTick,x,y,z,coverage,visibleSystems");var clouds=app.GetComponentInChildren<WorldCloudLayer>();
                for(int frame=0;frame<120;frame++){yield return null;var positions=Shader.GetGlobalVectorArray("_AevoriaCloudSystems");var coverage=Shader.GetGlobalVectorArray("_AevoriaCloudShapes");csv.WriteLine(FormattableString.Invariant($"{frame},{app.Clock.State.Tick},{clouds.PresentedTick:R},{positions[0].x:R},{positions[0].y:R},{positions[0].z:R},{coverage[0].y:R},{environment.VisibleCloudSystems}"));}
            }
            app.Clock.Pause();app.Clock.SetSpeed(1);app.ApplySettings(original,false);
        }
        private IEnumerator RegionalContacts(int peak)
        {
            var geo=app.Terrain.Geography;var h=geo.Hydrology;
            var centres=new List<(string Kind,GeoPoint Point)>{("mountain",new GeoPoint(h.X(peak),h.Y(peak)))};
            foreach(var biome in new[]{Biome.Desert,Biome.SaltFlat,Biome.MediterraneanWoodland})
            {
                int region=Best((p,height,s)=>height<=5||s.Biome!=biome?-1:biome==Biome.Desert&&geo.Surface.Regional!=null?Math.Abs(geo.Surface.Regional.DuneOffsetAt(p.X,p.Y,height)):1/(.01+Slope(p,32)));
                if(region>=0)centres.Add((biome.ToString(),app.Ecology.EnvironmentAt(region).Center));
            }
            using(var csv=new StreamWriter(Path.Combine(output,"regional-physics.csv")))
            {
                csv.WriteLine("kind,sample,x,z,canonicalHeight,colliderHeight,errorMeters");
                foreach(var centre in centres)
                {
                    double x0=Math.Floor(centre.Point.X/16)*16,z0=Math.Floor(centre.Point.Y/16)*16;
                    var source=GroundContactPatch.Sample(geo,x0,z0);int n=GroundContactPatch.Resolution,row=n+1;
                    var vertices=new Vector3[source.Length];for(int i=0;i<source.Length;i++)vertices[i]=new Vector3((float)(source[i].X-x0),(float)source[i].Y,(float)(source[i].Z-z0));
                    var indices=new int[n*n*6];int at=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*row+x;indices[at++]=a;indices[at++]=a+row;indices[at++]=a+1;indices[at++]=a+1;indices[at++]=a+row;indices[at++]=a+row+1;}
                    var mesh=new Mesh{name="Canonical regional physics probe"};mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateBounds();
                    var probe=new GameObject("Regional contact probe");probe.layer=31;probe.transform.position=WorldOrigin.Relative(new Vector3((float)x0,0,(float)z0));probe.AddComponent<MeshCollider>().sharedMesh=mesh;Physics.SyncTransforms();
                    for(int sample=0;sample<9;sample++)
                    {
                        double x=x0-12+sample%3*10+.37,z=z0-12+sample/3*10+.63,height=LocalSolidField.RenderedGround(geo,x,z);
                        Require(Physics.Raycast(WorldOrigin.Relative(new Vector3((float)x,(float)height+50,(float)z)),Vector3.down,out var hit,100,1<<31,QueryTriggerInteraction.Ignore),"Regional physical terrain exists: "+centre.Kind);
                        double error=Math.Abs(hit.point.y-height);csv.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6}",centre.Kind,sample,x,z,height,hit.point.y,error));
                        Require(error<.08,"Regional native physics agrees with the authoritative terrain within8cm");
                    }
                    Destroy(probe);Destroy(mesh);yield return null;
                }
            }
        }
        private double Slope(GeoPoint p,double step)
        {var g=app.Terrain.Geography;double dx=(g.ElevationAt(p.X+step,p.Y)-g.ElevationAt(p.X-step,p.Y))/(step*2),dz=(g.ElevationAt(p.X,p.Y+step)-g.ElevationAt(p.X,p.Y-step))/(step*2);return Math.Sqrt(dx*dx+dz*dz);}
        private int Best(Func<GeoPoint,float,GeographicSample,double> score)
        {int best=-1;double maximum=-1;var g=app.Terrain.Geography;for(int i=0;i<app.Ecology.Count;i++){var p=app.Ecology.EnvironmentAt(i).Center;var s=g.Sample(p);double value=score(p,s.ElevationMeters,s);if(value>maximum){maximum=value;best=i;}}return best;}
        private void Focus(int region,float distance,float pitch)
        {app.Controller.SetView(app.Ecology.EnvironmentAt(region).Center,25,pitch,distance);}
        private IEnumerator Ready()
        {
            // SetView can run before either streaming Update has observed the
            // new pose. The previous view's ready flag is not sufficient.
            yield return null;yield return null;
            float end=Time.realtimeSinceStartup+120,stable=-1;
            while(Time.realtimeSinceStartup<end)
            {
                if(app.Terrain.IsReady&&app.Waters.IsReady)
                {if(stable<0)stable=Time.realtimeSinceStartup;if(Time.realtimeSinceStartup-stable>=1)yield break;}
                else stable=-1;
                yield return null;
            }
            Require(false,"Terrain and water remained settled for review");
        }
        private IEnumerator Capture(string name,float waterWidth=0)
        {
            yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();var camera=Camera.main;var p=app.Controller.Focus;var g=app.Terrain.Geography;
            evidence.views.Add(new View{size=sizeName,name=name,seed=g.Configuration.Seed,fingerprint=worldFingerprint,biome=g.Sample(new GeoPoint(p.x,p.z)).Biome.ToString(),mode=app.Living.Traveler.Exploring?"Explore":"World",riverNode=inspectedRiver,geographicCamera=WorldOrigin.Geographic(camera.transform),euler=camera.transform.eulerAngles,focusX=p.x,focusZ=p.z,ground=g.ElevationAt(p.x,p.z),range=app.Controller.Distance,waterWidth=waterWidth,plateau=g.OrdinaryTerrain.PlateauAt(p.x,p.z),width=Screen.width,height=Screen.height,trees=app.Terrain.Vegetation.VisibleTrees,understorySites=app.Terrain.Vegetation.VisibleUnderstorySites,terrainTriangles=app.Terrain.ResidentTriangles});
            // Read this completed frame before the next diagnostic toggles
            // renderers or shader globals. Deferred screenshots can otherwise
            // record a different state than the manifest describes.
            var captured=ScreenCapture.CaptureScreenshotAsTexture();Require(captured!=null,"Completed native frame capture");
            File.WriteAllBytes(Path.Combine(output,sizeName+"-"+name+".png"),captured.EncodeToPNG());Destroy(captured);yield return null;
        }
        private IEnumerator Measure(string name)
        {
            using(var csv=new StreamWriter(Path.Combine(output,sizeName+"-"+name+"-frames.csv")))
            {
                csv.WriteLine("frame,engineLoopMs,renderCallbacks,width,height,workingSetMiB,privateMiB");float end=Time.realtimeSinceStartup+3;int frame=0,previous=renders;
                while(Time.realtimeSinceStartup<end){yield return null;var process=System.Diagnostics.Process.GetCurrentProcess();csv.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1:F5},{2},{3},{4},{5:F3},{6:F3}",frame++,Time.unscaledDeltaTime*1000,renders-previous,Screen.width,Screen.height,process.WorkingSet64/1048576.0,process.PrivateMemorySize64/1048576.0));process.Dispose();previous=renders;}
            }
        }
        private IEnumerator Coast()
        {
            var g=app.Terrain.Geography;var h=g.Hydrology;GeoPoint selected=default;double best=-1;float yaw=0;
            if(g.Coasts!=null)
            {
                int fjords=0,coves=0;
                using(var csv=new StreamWriter(Path.Combine(output,"coastal-landforms.csv")))
                {
                    csv.WriteLine("kind,index,point,x,z,physicalHeight,width,depth");
                    for(int index=0;index<g.Coasts.Inlets.Count;index++)
                    {
                        var inlet=g.Coasts.Inlets[index];for(int p=0;p<inlet.Points.Count;p++){var a=inlet.Points[p];csv.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7}",inlet.Kind,index,p,a.X,a.Z,g.ElevationAt(a.X,a.Z),inlet.Width,inlet.Depth));}
                        bool fjord=inlet.Kind==CoastalInletKind.Fjord;if(fjord?fjords>=2:coves>=2)continue;if(fjord)fjords++;else coves++;
                        var focus=inlet.Points[5];var mouth=inlet.Points[1];var head=inlet.Points[inlet.Points.Count-1];
                        float along=(float)(Math.Atan2(head.X-mouth.X,head.Z-mouth.Z)*180/Math.PI),range=fjord?Mathf.Max(8000,(float)inlet.Width*18):Mathf.Max(2400,(float)inlet.Width*10);
                        app.Controller.SetView(new GeoPoint(focus.X,focus.Z),along+65,48,range);yield return Ready();yield return Capture("coastal-"+inlet.Kind+"-"+index);
                        using(var rendered=new StreamWriter(Path.Combine(output,"inlet-rendered-"+inlet.Kind+"-"+index+".csv")))
                        {
                            rendered.WriteLine("point,x,z,canonical,regional,overviewBlend,presentedCameraFloor");
                            for(int p=0;p<inlet.Points.Count;p++){var a=inlet.Points[p];float rx=(float)a.X,rz=(float)a.Z;bool found=app.Terrain.Regional.TryHeight(rx,rz,out float height);rendered.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6}",p,a.X,a.Z,g.ElevationAt(a.X,a.Z),found?height:float.NaN,app.Terrain.Regional.OverviewBlend(rx,rz),app.Controller.PresentationHeight(rx,rz)));}
                        }
                        Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",1);yield return Capture("coastal-"+inlet.Kind+"-"+index+"-opaque");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",2);yield return Capture("coastal-"+inlet.Kind+"-"+index+"-depth");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",0);
                    }
                }
            }
            for(int i=0;i<h.Ground.Length;i+=3)
            {
                if(h.Ground[i]<=0)continue;int x=i%HydrologyMap.Resolution,z=i/HydrologyMap.Resolution;
                foreach(int j in new[]{x>0?i-1:-1,x<HydrologyMap.Resolution-1?i+1:-1,z>0?i-HydrologyMap.Resolution:-1,z<HydrologyMap.Resolution-1?i+HydrologyMap.Resolution:-1})
                {
                    if(j<0||h.Ground[j]>=0)continue;double ax=h.X(i),az=h.Y(i),bx=h.X(j),bz=h.Y(j),dx=ax-bx,dz=az-bz,length=Math.Sqrt(dx*dx+dz*dz);dx/=length;dz/=length;
                    for(int step=0;step<12;step++){double mx=(ax+bx)*.5,mz=(az+bz)*.5;if(g.ElevationAt(mx,mz)>0){ax=mx;az=mz;}else{bx=mx;bz=mz;}}
                    double height=g.ElevationAt(ax+dx*100,az+dz*100);if(height<=best)continue;best=height;selected=new GeoPoint(ax+dx*180,az+dz*180);yaw=(float)(Math.Atan2(dx,dz)*180/Math.PI);
                }
            }
            if(best<0){evidence.limitations.Add(sizeName+": no coastal inspection pair");yield break;}
            app.Controller.SetView(selected,yaw,35,1800);yield return Ready();yield return Capture("coastal-cliff");
            Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",1);yield return Capture("coast-opaque-water");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",2);yield return Capture("coast-water-depth");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",0);
            Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",3);yield return Capture("coast-water-alpha");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",4);yield return Capture("coast-depth-500m");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",5);yield return Capture("coast-shading-opaque");Shader.SetGlobalFloat("_AevoriaWaterDiagnostic",0);
            float transparency=Shader.GetGlobalFloat("_AevoriaWaterTransparency");Shader.SetGlobalFloat("_AevoriaWaterTransparency",0);yield return Capture("coast-canonical-depth");Shader.SetGlobalFloat("_AevoriaWaterTransparency",transparency);
            var ocean=app.Terrain.transform.Find("Ocean horizon").GetComponent<MeshRenderer>();ocean.forceRenderingOff=true;yield return Capture("coast-without-ocean");Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",1);yield return Capture("coast-geometry-without-ocean");Shader.SetGlobalFloat("_AevoriaTerrainDiagnostic",0);ocean.forceRenderingOff=false;
        }
        private void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;Application.logMessageReceived-=Failure;}
    }
}
