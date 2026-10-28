using System;
using System.Diagnostics;

namespace Aevoria.Simulation
{
    public sealed class SimulationClock
    {
        public const double SecondsPerTick = 0.25;
        public WorldState State { get; }
        public bool Running { get; private set; }
        public double Speed { get; private set; } = 1.0;
        public double BacklogSeconds { get; private set; }
        public double LastTickMilliseconds { get; private set; }

        private readonly DeterministicRandom random;

        public SimulationClock(WorldState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            random = new DeterministicRandom(state.RandomState);
        }

        public void Start() { Running = true; }
        public void Pause() { Running = false; }
        public void Resume() { Running = true; }
        public void RestoreControlState(double speed, bool running, double backlog)
        {
            if(double.IsNaN(backlog) || double.IsInfinity(backlog) || backlog<0) throw new ArgumentException("Invalid clock backlog.");
            SetSpeed(speed); BacklogSeconds=backlog; Running=running;
        }

        public void SetSpeed(double speed)
        {
            if (double.IsNaN(speed) || double.IsInfinity(speed) || speed < 0.25 || speed > 128)
                throw new ArgumentOutOfRangeException(nameof(speed));
            Speed = speed;
        }

        // The renderer supplies elapsed wall time; it never decides how many ticks a frame means.
        // The per-frame cap prevents long stalls without discarding simulation debt.
        public int Update(double elapsedSeconds, int maxTicks = 8)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (maxTicks <= 0) throw new ArgumentOutOfRangeException(nameof(maxTicks));
            if (!Running) return 0;
            BacklogSeconds += elapsedSeconds * Speed;
            int ticks = 0;
            while (BacklogSeconds + 1e-9 >= SecondsPerTick && ticks < maxTicks)
            {
                long start = Stopwatch.GetTimestamp();
                State.Advance(random);
                LastTickMilliseconds = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                BacklogSeconds -= SecondsPerTick;
                ticks++;
            }
            return ticks;
        }
    }
}
