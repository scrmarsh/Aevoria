using System;
using System.Collections;
using Aevoria.Persistence;
using Aevoria.Presentation;
using Aevoria.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Aevoria.Tests
{
    public sealed class CivilizationPlayTests
    {
        // Physical representative poses are presentation-driven saved state.
        // This lifecycle check compares economic/social authority; the native
        // resident fixture separately verifies injury, death and exact pose saves.
        private static string AuthorityHash(CivilizationSnapshot snapshot)
        {
            snapshot=snapshot.Copy();foreach(var settlement in snapshot.Settlements)foreach(var resident in settlement.Residents){resident.HasPosition=false;resident.X=resident.Y=resident.Z=0;}
            return ArchiveCodec.Hash(ArchiveCodec.Encode(snapshot));
        }
        [UnityTest,Timeout(600000)]
        public IEnumerator ActualWorldLifecycleRetainsSocietyAndItsTimeline()
        {
            var app=UnityEngine.Object.FindFirstObjectByType<AevoriaBootstrap>()??new GameObject("Society lifecycle").AddComponent<AevoriaBootstrap>();app.ReturnToMenu();yield return null;yield return null;
            app.CreateWorld("Living society regression","shaping-continents",WorldSize.Miniature,new GenerationSettings(landmass:LandmassType.Continents,renderRadius:2,nearLodRadius:0,mediumLodRadius:1),new ClimateSettings());float end=Time.realtimeSinceStartup+360;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;
            Assert.IsTrue(app.WorldActive,app.Message);Assert.IsFalse(app.Busy);Assert.IsNotNull(app.Society);Assert.AreEqual(4,app.Society.Civilizations.Count);app.Clock.Pause();
            foreach(var s in app.Society.Settlements){Assert.IsTrue(CivilizationGeography.Footprint(app.Terrain.Geography,s.X,s.Z,28,out _));Assert.GreaterOrEqual(s.Buildings.Count,3);}
            string beforeObservation=AuthorityHash(app.Society.Capture());app.SocietyView.FocusSettlement(app.Society.Settlements[0].Id);app.SocietyView.SetOverlay(SocietyOverlayMode.Political);yield return null;yield return null;
            end=Time.realtimeSinceStartup+90;while(app.Settlements.VisibleBuildings==0&&Time.realtimeSinceStartup<end)yield return null;Assert.GreaterOrEqual(app.Settlements.VisibleBuildings,3);Assert.Greater(app.Settlements.ColliderCount,0);
            var overlay=Shader.GetGlobalTexture("_AevoriaSocietyOverlay") as Texture2D;Assert.IsNotNull(overlay);bool checkedOwner=false,checkedNeutral=false;int resolution=app.Society.TerritoryResolution;
            for(int i=0;i<resolution*resolution;i++){int owner=app.Society.OwnerCell(i);Color pixel=overlay.GetPixel(i%resolution,i/resolution);if(owner<0){Assert.AreEqual(0,pixel.a,.001);checkedNeutral=true;}else{var civ=app.Society.Civilizations[owner];Assert.Greater(pixel.a,.2);Assert.AreEqual(civ.Red,pixel.r,.005);Assert.AreEqual(civ.Green,pixel.g,.005);Assert.AreEqual(civ.Blue,pixel.b,.005);checkedOwner=true;}}
            Assert.IsTrue(checkedOwner&&checkedNeutral);Assert.AreEqual(beforeObservation,AuthorityHash(app.Society.Capture()),"Observation and overlay selection cannot mutate society authority");app.SocietyView.SetOverlay(SocietyOverlayMode.Off);
            Assert.AreEqual(CharacterProfiles.Explorer,app.Living.Traveler.CharacterPreset);Assert.IsTrue(app.Explorer.SetAppearance(CharacterProfiles.Doctor),"Doctor is a normal appearance without an unlock requirement");
            var traveler=app.Living.Traveler;double east=traveler.X,north=traveler.Z,height=traveler.Y;
            Assert.IsTrue(app.Explorer.SetAppearance(CharacterProfiles.Roman));Assert.IsNull(app.Explorer.Character.Cane);Assert.IsTrue(app.Explorer.SetAppearance(CharacterProfiles.Doctor));Assert.IsNotNull(app.Explorer.Character.Cane);
            Assert.AreEqual(east,traveler.X);Assert.AreEqual(north,traveler.Z);Assert.AreEqual(height,traveler.Y);Assert.AreEqual(beforeObservation,AuthorityHash(app.Society.Capture()),"Costume selection must not mutate society");
            int historyBefore=app.Society.RecentHistory.Count;double populationBefore=app.Society.TotalPopulation;app.Clock.SetSpeed(8);app.Clock.Resume();app.Clock.Update(120*12*.25/8,int.MaxValue);app.Clock.Pause();app.Society.AdvanceTo(app.Clock.State.Tick,int.MaxValue);
            Assert.GreaterOrEqual(app.Society.Months,12);Assert.GreaterOrEqual(app.Society.RecentHistory.Count,historyBefore);Assert.AreNotEqual(populationBefore,app.Society.TotalPopulation);CivilizationModel.Validate(app.Society.Capture(),app.Terrain.Geography.Configuration,app.Clock.State.Tick);
            string before=AuthorityHash(app.Society.Capture());app.SaveWorld();end=Time.realtimeSinceStartup+180;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Assert.IsNotNull(app.Current,app.Message);string id=app.Current.Id;long tick=app.Clock.State.Tick;Assert.AreEqual(before,AuthorityHash(app.Current.Society));
            app.ReturnToMenu();yield return null;yield return null;Assert.IsNull(app.Society);app.LoadWorld(id);end=Time.realtimeSinceStartup+360;while(app.Busy&&Time.realtimeSinceStartup<end)yield return null;Assert.IsTrue(app.WorldActive,app.Message);app.Clock.Pause();
            Assert.AreEqual(tick,app.Clock.State.Tick);Assert.AreEqual(before,AuthorityHash(app.Society.Capture()));Assert.IsTrue(app.Clock.State.PopulationExternallyOwned);Assert.AreEqual((int)app.Society.TotalPopulation,app.Clock.State.Population);
            Assert.AreEqual(CharacterProfiles.Doctor,app.Living.Traveler.CharacterPreset);Assert.AreEqual(CharacterAppearance.Doctor,app.Explorer.Character.Appearance);
            app.ReturnToMenu();yield return null;yield return null;
        }
    }
}
