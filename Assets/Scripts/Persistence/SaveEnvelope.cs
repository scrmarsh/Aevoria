using System;
using Aevoria.Simulation;

namespace Aevoria.Persistence
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int SchemaVersion = 1;
        public string Seed;
        public long Tick;
        public int Population;
        public int Food;
        public string RandomState;
    }

    public static class SaveValidation
    {
        public static void Validate(SaveEnvelope save)
        {
            if (save == null || save.SchemaVersion != 1 || string.IsNullOrWhiteSpace(save.Seed)
                || save.Tick < 0 || save.Population < 0 || save.Food < 0
                || !ulong.TryParse(save.RandomState, out _))
                throw new ArgumentException("Unsupported or invalid save envelope.");
        }
    }
}