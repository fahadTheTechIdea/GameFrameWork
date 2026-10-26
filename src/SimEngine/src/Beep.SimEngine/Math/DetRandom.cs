using System;

namespace Beep.SimEngine
{
    /// <summary>
    /// The simulation's one random-number generator (xorshift64*): seeded by the game, saved with it,
    /// advanced only by systems. Never use System.Random in a simulation.
    /// </summary>
    public sealed class DetRandom
    {
        private ulong _state;
        private ulong _observationVersion;

        public DetRandom(ulong seed) { _state = Mix(seed); }

        public ulong State
        {
            get => _state;
            set
            {
                if (value == 0) throw new ArgumentOutOfRangeException(nameof(value), "random state must be nonzero");
                ChangeState(value);
            }
        }
        // Transient invalidation of this stream, not another saved clock or random ledger.
        // Detects draw-and-restore and equal-state writes inside supposedly pure callbacks.
        internal ulong ObservationVersion => _observationVersion;
        private void ChangeState(ulong value)
        {
            ulong version = checked(_observationVersion + 1);
            _state = value; _observationVersion = version;
        }

        /// <summary>SplitMix64 finaliser, so neighbouring seeds start far apart.</summary>
        public static ulong Mix(ulong seed)
        {
            ulong z = seed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return z == 0 ? 0x9E3779B97F4A7C15UL : z;
        }

        public ulong NextULong()
        {
            ulong state = _state;
            state ^= state >> 12;
            state ^= state << 25;
            state ^= state >> 27;
            ChangeState(state);
            return state * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>Uniform in [0, maxExclusive).</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextULong() % (ulong)maxExclusive);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            ulong width = (ulong)((long)maxExclusive - minInclusive);
            return checked((int)(minInclusive + (long)(NextULong() % width)));
        }

        /// <summary>Uniform in [0, 1).</summary>
        public Fix NextFix() => Fix.FromRaw((long)(NextULong() >> 32));

        public Fix NextFix(Fix min, Fix max)
        {
            if (max <= min) throw new ArgumentOutOfRangeException(nameof(max));
            var width = Fix.FromRaw(checked(max.Raw - min.Raw));
            return min + width * NextFix();
        }

        /// <summary>True with probability <paramref name="p"/> (0..1).</summary>
        public bool Chance(Fix p)
        {
            if (p < Fix.Zero || p > Fix.One) throw new ArgumentOutOfRangeException(nameof(p));
            return NextFix() < p;
        }
    }
}
