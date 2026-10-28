using System;

namespace Aevoria.Simulation
{
    public sealed class WorldState
    {
        public const int CurrentSchema = 1;
        public int SchemaVersion { get; private set; }
        public string Seed { get; private set; }
        public long Tick { get; private set; }
        public int Population { get; private set; }
        public int Food { get; private set; }
        public ulong RandomState { get; private set; }
        public bool PopulationExternallyOwned {get;private set;}
        public void UseAggregatePopulation(int population,int food)
        {if(population<0||food<0)throw new ArgumentOutOfRangeException(nameof(population));PopulationExternallyOwned=true;Population=population;Food=food;}

        public WorldState(string seed)
        {
            if (string.IsNullOrWhiteSpace(seed)) throw new ArgumentException("A seed is required.", nameof(seed));
            SchemaVersion = CurrentSchema;
            Seed = seed;
            Population = 24;
            Food = 100;
            RandomState = SeedToState(seed);
        }

        internal void Advance(DeterministicRandom random)
        {
            // Deliberately small aggregate prototype. No citizens or Unity objects.
            Tick++;
            if(PopulationExternallyOwned){RandomState=random.State;return;}
            Food += 2;
            if (Tick % 5 == 0 && Food >= Population)
            {
                Food -= Population;
                if (random.NextDouble() < 0.4) Population++;
            }
            RandomState = random.State;
        }

        public static WorldState Restore(string seed, long tick, int population, int food, ulong randomState)
        {
            if(tick<0 || population<0 || food<0 || randomState==0) throw new ArgumentException("Invalid simulation state.");
            return new WorldState(seed) {Tick=tick, Population=population, Food=food, RandomState=randomState};
        }

        private static ulong SeedToState(string seed)
        {
            ulong hash = 14695981039346656037UL;
            foreach (char c in seed)
            {
                hash ^= c;
                hash *= 1099511628211UL;
            }
            return hash == 0 ? 1UL : hash;
        }
    }
}
