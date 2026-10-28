using Aevoria.World;
using NUnit.Framework;
namespace Aevoria.Tests
{
    public sealed class CryosphereClimateTests
    {
        [Test]public void WarmMountainsHaveNoPerennialSnowStamp()
        {Assert.AreEqual(0,CryosphereClimate.PerennialSnow(5,0,1));Assert.AreEqual(0,CryosphereClimate.PerennialIce(5,0));}
        [Test]public void TropicalSummitsAndColdLowlandsCanSupportPerennialSnow()
        {Assert.AreEqual(1,CryosphereClimate.PerennialSnow(-18,0,.8));Assert.AreEqual(1,CryosphereClimate.PerennialSnow(-25,15,.8));}
        [Test]public void DryColdGroundAndWarmSummersLimitPermanentCover()
        {Assert.Less(CryosphereClimate.PerennialSnow(-20,0,.10),.05);Assert.AreEqual(0,CryosphereClimate.PerennialSnow(-8,12,.8));Assert.AreEqual(0,CryosphereClimate.PerennialIce(-8,12));}
        [Test]public void PermanentlyFrozenWaterDependsOnTheWarmestSeason()
        {Assert.AreEqual(1,CryosphereClimate.PerennialIce(-20,8));Assert.AreEqual(0,CryosphereClimate.PerennialIce(-8,8));}
    }
}
