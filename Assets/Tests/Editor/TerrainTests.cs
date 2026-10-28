using System;
using System.Reflection;
using System.Threading;
using Aevoria.World;
using Aevoria.Core;
using Aevoria.Persistence;
using Aevoria.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Aevoria.Tests
{
    public sealed class TerrainTests : InputTestFixture
    {
        [Test,Timeout(600000)] public void SeedsAreReproducibleInReverseQueryOrder()
        {
            var a=new ProceduralGeography(GenerationSettings.Preset("test",WorldSize.Medium));
            var b=new ProceduralGeography(GenerationSettings.Preset("test",WorldSize.Medium));
            var other=new ProceduralGeography(GenerationSettings.Preset("other",WorldSize.Medium));
            var heights=new float[100]; int differences=0;
            for(int i=0;i<100;i++) heights[i]=a.ElevationAt(i*13-650,i*7-350);
            for(int i=99;i>=0;i--) { Assert.AreEqual(heights[i],b.ElevationAt(i*13-650,i*7-350)); if(heights[i]!=other.ElevationAt(i*13-650,i*7-350)) differences++; }
            Assert.Greater(differences,80);
        }
        [Test] public void InvalidConfigurationsFailBeforeAllocations()
        {
            Assert.Throws<ArgumentException>(()=>new GenerationSettings(mountains:float.PositiveInfinity));
            Assert.Throws<ArgumentException>(()=>new GenerationSettings(renderRadius:100));
            Assert.Throws<ArgumentException>(()=>new WorldConfiguration("s",999,2,64,4));
            Assert.Throws<ArgumentException>(()=>new WorldConfiguration("s",2,2,63,4));
            Assert.Throws<ArgumentException>(()=>new WorldConfiguration("s",2,2,64,double.NaN));
        }
        [Test] public void ElevationsAreFiniteAndOceanClassificationMatchesSea()
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset("water",WorldSize.Small));
            for(int y=-1024;y<=1024;y+=32) for(int x=-1024;x<=1024;x+=32)
            {
                var sample=geo.Sample(new GeoPoint(x,y)); Assert.IsFalse(float.IsNaN(sample.ElevationMeters)); Assert.IsFalse(float.IsInfinity(sample.ElevationMeters));
                Assert.AreEqual(sample.ElevationMeters<geo.SeaLevel,sample.Biome==Biome.Ocean);
            }
        }
        [Test] public void ActualMeshBordersShareHeightsAndNormalsAtAllLods()
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset("mesh",WorldSize.Small));
            MethodInfo generate=typeof(WorldRenderer).GetMethod("Generate",BindingFlags.Static|BindingFlags.NonPublic);
            for(int lod=0;lod<=2;lod++)
            {
                object left=generate.Invoke(null,new object[]{geo,new ChunkKey(-1,0),lod,CancellationToken.None});
                object right=generate.Invoke(null,new object[]{geo,new ChunkKey(0,0),lod,CancellationToken.None});
                var vertices=(Vector3[])left.GetType().GetField("Vertices").GetValue(left);
                var other=(Vector3[])right.GetType().GetField("Vertices").GetValue(right);
                var normals=(Vector3[])left.GetType().GetField("Normals").GetValue(left);
                var otherNormals=(Vector3[])right.GetType().GetField("Normals").GetValue(right);
                int n=64>>lod,row=n+1;
                for(int i=0;i<=n;i++) { Assert.AreEqual(vertices[i*row+n],other[i*row]); Assert.AreEqual(normals[i*row+n],otherNormals[i*row]); }
                var indices=(int[])left.GetType().GetField("Indices").GetValue(left);
                foreach(int index in indices) Assert.That(index,Is.InRange(0,vertices.Length-1));
            }
        }
        [Test] public void SeaLevelFloodsExistingHeights()
        {
            var dry=new ProceduralGeography(GenerationSettings.Preset("waterline",WorldSize.Medium,new GenerationSettings(seaLevel:0)));
            var wet=new ProceduralGeography(GenerationSettings.Preset("waterline",WorldSize.Medium,new GenerationSettings(seaLevel:100)));
            int flooded=0;
            // Cover the map: a particular seed's centre transect can lie entirely
            // above or below both waterlines after a versioned landform change.
            for(int ix=-32;ix<=32;ix++)
            for(int iz=-32;iz<=32;iz++)
            {
                double x=ix*dry.Configuration.WidthMeters/64,z=iz*dry.Configuration.DepthMeters/64;
                Assert.AreEqual(dry.Geology.ContinentalAt(x,z),wet.Geology.ContinentalAt(x,z));
                Assert.IsFalse(float.IsNaN(wet.ElevationAt(x,z))); // Sea level changes climate, dunes and outlet incision in generator 5.
                if(dry.Sample(new GeoPoint(x,z)).Biome!=Biome.Ocean && wet.Sample(new GeoPoint(x,z)).Biome==Biome.Ocean) flooded++;
            }
            Assert.Greater(flooded,0);
        }
        [Test] public void MixedLodSamplesMatchAtLargeSignedCoordinates()
        {
            var geo=new ProceduralGeography(GenerationSettings.Preset("large-border",WorldSize.Large));
            MethodInfo generate=typeof(WorldRenderer).GetMethod("Generate",BindingFlags.Static|BindingFlags.NonPublic);
            object left=generate.Invoke(null,new object[]{geo,new ChunkKey(50,-31),0,CancellationToken.None});
            object right=generate.Invoke(null,new object[]{geo,new ChunkKey(51,-31),2,CancellationToken.None});
            var vertices=(Vector3[])left.GetType().GetField("Vertices").GetValue(left);
            var other=(Vector3[])right.GetType().GetField("Vertices").GetValue(right);
            var normals=(Vector3[])left.GetType().GetField("Normals").GetValue(left);
            var otherNormals=(Vector3[])right.GetType().GetField("Normals").GetValue(right);
            for(int z=0;z<=16;z++) {Assert.AreEqual(vertices[z*4*65+64],other[z*17]);Assert.AreEqual(normals[z*4*65+64],otherNormals[z*17]);}
        }
        [Test] public void ZoomSensitivityScalesWheelTravelAndSettles()
        {
            var obj=new GameObject("sensitivity camera");obj.AddComponent<Camera>();var camera=obj.AddComponent<MapCamera>();var mouse=InputSystem.AddDevice<Mouse>();
            var handle=typeof(MapCamera).GetMethod("HandleMouse",BindingFlags.Instance|BindingFlags.NonPublic);
            try
            {
                camera.Initialize(null);camera.ZoomTowardCursor=false;camera.ZoomSensitivity=0.3f;
                InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(Screen.width*0.75f,Screen.height*0.5f),scroll=new Vector2(0,1)});InputSystem.Update();
                handle.Invoke(camera,new object[]{mouse,false});for(int i=0;i<120;i++) camera.Advance(1f/60);
                float gentle=camera.Distance;Assert.Less(gentle,900);
                camera.Initialize(null);camera.ZoomSensitivity=1.5f;
                InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(Screen.width*0.75f,Screen.height*0.5f),scroll=new Vector2(0,1)});InputSystem.Update();
                handle.Invoke(camera,new object[]{mouse,false});for(int i=0;i<120;i++) camera.Advance(1f/60);
                Assert.Less(camera.Distance,gentle*0.85f);Assert.GreaterOrEqual(camera.Distance,2);
            }
            finally {InputSystem.RemoveDevice(mouse);UnityEngine.Object.DestroyImmediate(obj);}
        }
        [TestCase(false,1f)]
        [TestCase(true,120f)]
        public void WheelNotchUsesConfiguredInputSystemUnits(bool legacy,float wheel)
        {
            var previous=InputSystem.settings.scrollDeltaBehavior;
            var obj=new GameObject("wheel units");obj.AddComponent<Camera>();var camera=obj.AddComponent<MapCamera>();var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                InputSystem.settings.scrollDeltaBehavior=legacy?InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange:InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms;
                camera.Initialize(null);camera.ZoomTowardCursor=false;
                InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(Screen.width*0.75f,Screen.height*0.5f),scroll=new Vector2(0,wheel)});InputSystem.Update();
                typeof(MapCamera).GetMethod("HandleMouse",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(camera,new object[]{mouse,false});
                float distance=(float)typeof(MapCamera).GetField("targetDistance",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(camera);
                Assert.AreEqual(900*Mathf.Exp(-1.1f*.22f),distance,0.01f);
            }
            finally {InputSystem.settings.scrollDeltaBehavior=previous;InputSystem.RemoveDevice(mouse);UnityEngine.Object.DestroyImmediate(obj);}
        }
        [Test] public void KeyboardMovementIsCameraRelativeAndUsesConfiguredBoostAndMovementBindings()
        {
            var obj=new GameObject("keyboard camera"); obj.AddComponent<Camera>(); var camera=obj.AddComponent<MapCamera>();
            var keyboard=InputSystem.AddDevice<Keyboard>();
            var handle=typeof(MapCamera).GetMethod("HandleKeyboard",BindingFlags.Instance|BindingFlags.NonPublic);
            var focus=typeof(MapCamera).GetField("targetFocus",BindingFlags.Instance|BindingFlags.NonPublic);
            var yaw=typeof(MapCamera).GetField("targetYaw",BindingFlags.Instance|BindingFlags.NonPublic);
            var original=GameInput.Settings;bool textEntry=GameInput.TextEntry;int consumed=GameInput.ConsumedFrame;GameInput.Settings=new ApplicationSettings();GameInput.TextEntry=false;GameInput.ConsumedFrame=-1;
            try
            {
                camera.Initialize(null); yaw.SetValue(camera,90f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); InputSystem.Update();
                for(int i=0;i<60;i++) handle.Invoke(camera,new object[]{keyboard,1f/60});
                Vector3 normal=(Vector3)focus.GetValue(camera); Assert.Greater(normal.x,0); Assert.That(Mathf.Abs(normal.z),Is.LessThan(0.01f));
                camera.Initialize(null); yaw.SetValue(camera,90f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftCtrl)); InputSystem.Update();
                for(int i=0;i<60;i++) handle.Invoke(camera,new object[]{keyboard,1f/60});
                Assert.Greater(((Vector3)focus.GetValue(camera)).x,normal.x*2);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
                for(int i=0;i<120;i++) handle.Invoke(camera,new object[]{keyboard,1f/60});
                var velocity=(Vector3)typeof(MapCamera).GetField("movementVelocity",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(camera);
                Assert.Less(velocity.magnitude,0.001f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R)); InputSystem.Update();
                handle.Invoke(camera,new object[]{keyboard,1f/60}); Assert.AreEqual(0,(float)yaw.GetValue(camera));
                GameInput.Settings.Bindings.Add(new KeyBinding(GameAction.Forward,"UpArrow"));GameInput.Settings.Bindings.Add(new KeyBinding(GameAction.Sprint,"RightCtrl"));GameInput.Settings.Validate();
                camera.Initialize(null);yaw.SetValue(camera,90f);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();
                for(int i=0;i<60;i++)handle.Invoke(camera,new object[]{keyboard,1f/60});Assert.Less(((Vector3)focus.GetValue(camera)).magnitude,.001f,"Replaced movement key no longer drives the camera");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.UpArrow,Key.RightCtrl));InputSystem.Update();
                for(int i=0;i<60;i++)handle.Invoke(camera,new object[]{keyboard,1f/60});Assert.Greater(((Vector3)focus.GetValue(camera)).x,normal.x*2,"The live consumer reads remapped movement and boost keys");
                GameInput.TextEntry=true;camera.Initialize(null);for(int i=0;i<60;i++)handle.Invoke(camera,new object[]{keyboard,1f/60});Assert.Less(((Vector3)focus.GetValue(camera)).magnitude,.001f,"Command entry suppresses remapped inputs");
            }
            finally { GameInput.Settings=original;GameInput.TextEntry=textEntry;GameInput.ConsumedFrame=consumed;InputSystem.RemoveDevice(keyboard); UnityEngine.Object.DestroyImmediate(obj); }
        }
        [Test] public void SyntheticMouseOrbitZoomAndHudGate()
        {
            var obj=new GameObject("test camera"); obj.AddComponent<Camera>(); var camera=obj.AddComponent<MapCamera>();
            var mouse=InputSystem.AddDevice<Mouse>();
            var handle=typeof(MapCamera).GetMethod("HandleMouse",BindingFlags.Instance|BindingFlags.NonPublic);
            float Read(string field)=>(float)typeof(MapCamera).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(camera);
            try
            {
                camera.Initialize(new ProceduralGeography(GenerationSettings.Preset("camera",WorldSize.Small)));
                for(int i=0;i<40;i++)
                {
                    InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(Screen.width*0.75f,Screen.height*0.5f),delta=new Vector2(120,-120),scroll=new Vector2(0,1) }.WithButton(MouseButton.Right)); InputSystem.Update();
                    handle.Invoke(camera,new object[]{mouse,false});
                }
                // Alpha006 deliberately extends free orbit through the horizon
                // to straight upward; retain the input/HUD/zoom regression.
                Assert.That(Read("targetPitch"),Is.InRange(-90f,89.7f)); Assert.AreEqual(89.7f,Read("targetPitch"));
                Assert.Greater(Read("targetYaw"),0); Assert.AreEqual(2,Read("targetDistance"));
                float yaw=Read("targetYaw"),distance=Read("targetDistance");
                handle.Invoke(camera,new object[]{mouse,true}); Assert.AreEqual(yaw,Read("targetYaw")); Assert.AreEqual(distance,Read("targetDistance"));
                InputSystem.QueueStateEvent(mouse,new MouseState()); InputSystem.Update();
                for(int i=0;i<40;i++)
                {
                    InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(Screen.width*0.75f,Screen.height*0.5f),delta=new Vector2(0,120) }.WithButton(MouseButton.Right)); InputSystem.Update();
                    handle.Invoke(camera,new object[]{mouse,false});
                }
                Assert.AreEqual(-90,Read("targetPitch"));
                for(int i=0;i<80;i++) { InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(Screen.width*0.75f,Screen.height*0.5f),scroll=new Vector2(0,-1) }); InputSystem.Update(); handle.Invoke(camera,new object[]{mouse,false}); }
                Assert.AreEqual((float)GenerationSettings.Preset("camera",WorldSize.Small).WidthMeters*1.4f,Read("targetDistance"),.01f);
                InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(Screen.width*0.75f,Screen.height*0.5f),delta=new Vector2(50,20) }.WithButton(MouseButton.Middle)); InputSystem.Update();
                Vector3 focus=(Vector3)typeof(MapCamera).GetField("targetFocus",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(camera);
                handle.Invoke(camera,new object[]{mouse,false});
                Assert.AreNotEqual(focus,(Vector3)typeof(MapCamera).GetField("targetFocus",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(camera));
            }
            finally { InputSystem.RemoveDevice(mouse); UnityEngine.Object.DestroyImmediate(obj); }
        }
    }
}
