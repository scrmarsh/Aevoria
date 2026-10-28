using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aevoria.Persistence;
using Aevoria.World;
using UnityEngine;
namespace Aevoria.Presentation
{
    public sealed class Alpha011SettlementAudit:MonoBehaviour
    {
        private AevoriaBootstrap app;private string output;private int errors;
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        private void Logged(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;File.AppendAllText(Path.Combine(output,"failures.txt"),message+"\n"+stack+"\n");Application.Quit(7);}
        private IEnumerator Start()
        {
            Require(AevoriaBootstrap.Argument("-aevoriaTestRoot")!=null,"Isolated settlement saves required");output=AevoriaBootstrap.Argument("-aevoriaOutput");Directory.CreateDirectory(output);Application.runInBackground=true;Application.logMessageReceived+=Logged;
            app=GetComponent<AevoriaBootstrap>();app.ExternalControl=true;yield return null;app.ReturnToMenu();yield return null;
            app.CreateWorld("Settlement interaction review","shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents,renderRadius:2,nearLodRadius:0,mediumLodRadius:1),new ClimateSettings());yield return Operation();Require(app.WorldActive,"Created settlement world");app.Clock.Pause();app.Controller.ExternalDriver=true;
            var settings=app.Settings.Copy();settings.AutosaveSeconds=0;settings.MotionBlur=false;app.ApplySettings(settings,false);var settlement=app.Society.Settlements[0];string settlementId=settlement.Id;
            app.Controller.SetView(new GeoPoint(settlement.X,settlement.Z),25,48,160);yield return Ready(settlementId);yield return Capture("founding-village");
            Require(settlement.Buildings.Count(b=>b.Kind==CivicBuildingKind.Dwelling)>5,"Founders have actual homes rather than oversized population proxies");Require(app.Settlements.Paths.VisiblePaths>0,"Real connected paths are rendered");
            using(var trace=new StreamWriter(Path.Combine(output,"housing.txt")))trace.WriteLine("population="+settlement.Population+" homes="+settlement.Buildings.Count(b=>b.Kind==CivicBuildingKind.Dwelling)+" housing="+CivilizationModel.HousingCapacity(settlement)+" buildings="+settlement.Buildings.Count);
            int beforeBuildings=settlement.Buildings.Count;app.Clock.Start();app.Clock.Update(720,2880);app.Clock.Pause();app.SaveWorld();yield return Operation();yield return Ready(settlementId);yield return Capture("paid-village-growth");
            Require(settlement.Buildings.Count>beforeBuildings,"Paid construction expands the actual settlement");Require(settlement.Buildings.Any(b=>b.Kind==CivicBuildingKind.Workshop)&&settlement.Buildings.Any(b=>b.Kind==CivicBuildingKind.Market),"Supported workshops and market constructed by simulation");
            var before=new Dictionary<string,Vector3>();foreach(string id in app.Settlements.Residents.ResidentIds)if(app.Settlements.Residents.TryPosition(id,out var p))before[id]=p;
            app.Clock.SetSpeed(.25);app.Clock.Start();float until=Time.realtimeSinceStartup+8;while(Time.realtimeSinceStartup<until)yield return null;app.Clock.Pause();
            int moved=0;using(var trace=new StreamWriter(Path.Combine(output,"resident-motion.csv")))
            {trace.WriteLine("id,distance");foreach(var pair in before)if(app.Settlements.Residents.TryPosition(pair.Key,out var p)){double distance=Vector3.Distance(pair.Value,p);trace.WriteLine(pair.Key+","+distance.ToString(System.Globalization.CultureInfo.InvariantCulture));if(distance>1)moved++;}}
            Require(moved>=Math.Max(2,before.Count/3),"Nearby humanoids physically travel between destinations");yield return Capture("residents-walking");
            ResidentState target=null;foreach(var resident in settlement.Residents)if(app.Settlements.Residents.TryPosition(resident.Id,out _)){target=resident;break;}Require(target!=null,"An actual visible resident is available");string deadId=target.Id;
            app.Explorer.AcceptInput=false;bool aimed=false;
            for(int side=0;side<12&&!aimed;side++)
            {
                double angle=side*Math.PI/6,x=target.X+Math.Sin(angle)*2.8,z=target.Z+Math.Cos(angle)*2.8;if(settlement.Buildings.Any(b=>CivicGeometry.Contains(b,x,z,.4)))continue;
                var traveler=app.Living.Traveler;traveler.HasPosition=true;traveler.Flying=false;traveler.X=x;traveler.Z=z;traveler.Y=LocalSolidField.RenderedGround(app.Terrain.Geography,x,z)+.05;traveler.VelocityX=traveler.VelocityY=traveler.VelocityZ=0;traveler.FirstPerson=true;traveler.Tool=ToolKind.Stick;
                if(!app.Explorer.EnterAt(new GeoPoint(x,z),true))continue;yield return Ready(settlementId);
                var position=app.Explorer.GeographicPosition;double dx=target.X-position.X,dz=target.Z-position.Z,dy=target.Y+.9-(position.Y+1.65);traveler.Yaw=Math.Atan2(dx,dz)*Mathf.Rad2Deg;traveler.Pitch=-Math.Atan2(dy,Math.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;Physics.SyncTransforms();
                aimed=Physics.Raycast(app.Explorer.AimRay,out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.name=="Resident · "+deadId;
            }
            Require(aimed,"Traveler's actual interaction ray reaches a resident collider");yield return Capture("resident-targeted");double population=app.Society.TotalPopulation;string resources=ArchiveCodec.Hash(ArchiveCodec.Encode(settlement.Resources));
            app.Explorer.Interact(false);Require(target.Health==65,"Actual tool interaction applies nonlethal health damage");app.SaveWorld();yield return Operation();string saveId=app.Current.Id;app.ReturnToMenu();yield return null;yield return null;app.LoadWorld(saveId);yield return Operation();app.Clock.Pause();app.Explorer.AcceptInput=false;settlement=app.Society.Settlement(settlementId);target=settlement.Residents.Find(r=>r.Id==deadId);Require(target!=null&&target.Health==65,"Injured health and identity survive native reload");yield return Ready(settlementId);
            Require(Physics.Raycast(app.Explorer.AimRay,out var reloadedHit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&reloadedHit.collider.name=="Resident · "+deadId,"Reload restores actual actor at saved target pose");
            app.Explorer.Interact(false);app.Explorer.Interact(false);Require(!settlement.Residents.Any(r=>r.Id==deadId),"Actual attacks remove the killed identity");Require(app.Society.TotalPopulation==population-1,"Authoritative population decreases exactly once");Require(resources==ArchiveCodec.Hash(ArchiveCodec.Encode(settlement.Resources)),"Combat leaves resource authority unchanged");yield return Capture("resident-killed");
            app.SaveWorld();yield return Operation();app.ReturnToMenu();yield return null;yield return null;app.LoadWorld(saveId);yield return Operation();app.Clock.Pause();app.Explorer.AcceptInput=false;yield return Ready(settlementId);
            settlement=app.Society.Settlement(settlementId);Require(settlement.ResidentDeaths==1&&!settlement.Residents.Any(r=>r.Id==deadId),"Death persists across actual reload");Require(!app.Settlements.Residents.ResidentIds.Contains(deadId),"Streaming does not recreate a dead NPC");Require(app.Society.TotalPopulation==population-1,"Reload preserves the exact population effect");yield return Capture("death-reloaded");
            Require(errors==0,"Native settlement review has no runtime errors");File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: homes and paid growth, connected paths, moving humanoids, actual ray/tool health, injury reload, death/population/resource authority, no resurrection after reload.");Debug.Log("AEVORIA011_SETTLEMENT_REVIEW_COMPLETE");app.ReturnToMenu();yield return null;Application.Quit();
        }
        private IEnumerator Operation(){float end=Time.realtimeSinceStartup+900;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Require(!app.Busy,"World operation completed");}
        private IEnumerator Ready(string settlement)
        {float end=Time.realtimeSinceStartup+180;while(Time.realtimeSinceStartup<end){if(app.Terrain.IsReady&&app.Explorer.Ready&&app.Settlements.Paths.Network(settlement)!=null&&app.Settlements.Residents.VisibleResidents>=4)break;yield return null;}Require(app.Terrain.IsReady&&app.Explorer.Ready&&app.Settlements.Paths.Network(settlement)!=null&&app.Settlements.Residents.VisibleResidents>=4,"Village terrain, paths, residents and traveler ready");yield return new WaitForSecondsRealtime(.4f);}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        private void OnDestroy(){Application.logMessageReceived-=Logged;}
    }
}
