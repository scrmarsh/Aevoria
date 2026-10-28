using Aevoria.Presentation;
using Aevoria.World;
using NUnit.Framework;
using UnityEngine;
namespace Aevoria.Tests
{
    public sealed class WorldOriginTests
    {
        [Test]public void TopDownSmallStepsStayContinuousAtLargeCoordinates()
        {
            WorldOrigin.Reset();var obj=new GameObject("precise top-down movement");obj.AddComponent<Camera>();var map=obj.AddComponent<MapCamera>();
            try
            {
                map.Initialize(new PlaceholderGeography(Aevoria.World.GenerationSettings.Preset("origin",Aevoria.World.WorldSize.Colossal)));
                map.SetView(new Aevoria.World.GeoPoint(800000,-700000),0,89,30);double start=(double)obj.transform.position.x+WorldOrigin.Offset.x,previous=start;
                for(int i=0;i<300;i++)
                {
                    map.MoveHorizontal(new Vector2(.003f,0));map.Advance(1/60f);double current=(double)obj.transform.position.x+WorldOrigin.Offset.x;
                    if(i>30)Assert.That(current-previous,Is.InRange(.002f,.004f));previous=current;
                }
                Assert.That(previous-start,Is.InRange(.87,.91));
            }
            finally{Object.DestroyImmediate(obj);WorldOrigin.Reset();}
        }
        [Test]public void RebasePreservesGeographicPoseAndKeepsLocalMotionPrecise()
        {
            WorldOrigin.Reset();var actor=new GameObject("origin actor");var child=new GameObject("rig child");child.transform.SetParent(actor.transform,false);child.transform.localPosition=new Vector3(.13f,1.7f,.21f);
            try
            {
                actor.transform.position=new Vector3(900000,35,-800000);WorldOrigin.Follow(actor.transform.position);
                Assert.Less(Mathf.Abs(actor.transform.position.x),1024);Assert.Less(Mathf.Abs(actor.transform.position.z),1024);
                Assert.AreEqual(new Vector3(900000,35,-800000),WorldOrigin.Geographic(actor.transform));
                float start=actor.transform.position.x;for(int i=0;i<60;i++)actor.transform.position+=Vector3.right*.003f;
                Assert.That(actor.transform.position.x-start,Is.EqualTo(.18f).Within(.002f),"Three-millimetre steps must accumulate at Colossal coordinates");
                Assert.AreEqual(new Vector3(.13f,1.7f,.21f),child.transform.localPosition);
                var before=WorldOrigin.Geographic(actor.transform);WorldOrigin.Reset();Assert.AreEqual(before,actor.transform.position);
            }
            finally{Object.DestroyImmediate(actor);WorldOrigin.Reset();}
        }
    }
}
