using Aevoria.Simulation;
using Aevoria.World;
using NUnit.Framework;

namespace Aevoria.Tests
{
    public sealed class FoundationTests
    {
        [Test]
        public void SameSeedIsIndependentOfFrameCadence()
        {
            var a = new SimulationClock(new WorldState("same"));
            var b = new SimulationClock(new WorldState("same"));
            a.Start(); b.Start();
            a.Update(2.5);
            a.Update(0);
            for (int i = 0; i < 10; i++) b.Update(0.25);
            Assert.AreEqual(a.State.Tick, b.State.Tick);
            Assert.AreEqual(a.State.RandomState, b.State.RandomState);
            Assert.AreEqual(a.State.Population, b.State.Population);
        }

        [Test]
        public void PauseResumeAndSpeedControlTicks()
        {
            var clock = new SimulationClock(new WorldState("clock"));
            clock.Start();
            clock.Update(0.25);
            clock.Pause();
            clock.Update(10);
            Assert.AreEqual(1, clock.State.Tick);
            clock.SetSpeed(2);
            clock.Resume();
            clock.Update(0.5);
            Assert.AreEqual(5, clock.State.Tick);
        }

        [Test]
        public void ChunkCoordinatesUseFloorAcrossOrigin()
        {
            var config = new WorldConfiguration("world", 2, 2, 64, 1);
            var chunk = config.ChunkAt(new GeoPoint(-0.1, 64));
            Assert.AreEqual(-1, chunk.X);
            Assert.AreEqual(1, chunk.Y);
        }
    }
}