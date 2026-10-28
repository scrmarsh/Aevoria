using System;

namespace Aevoria.Simulation
{
    // Xorshift64* transition ported from Aevoria-old/ArcadeRandom.cs.
    // Preserve the transition so future migrations can compare sequences.
    public sealed class DeterministicRandom
    {
        public ulong State { get; private set; }

        public DeterministicRandom(ulong state)
        {
            State = state == 0 ? 1UL : state;
        }

        public ulong NextBits()
        {
            ulong x = State;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            State = x;
            return x * 2685821657736338717UL;
        }

        public double NextDouble()
        {
            return (NextBits() >> 11) * (1.0 / (1UL << 53));
        }
    }
}