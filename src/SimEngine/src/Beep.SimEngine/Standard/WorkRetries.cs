using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable authored retry decisions; job state and deadlines belong to WorkBoard.</summary>
    public interface IWorkRetryPolicy : IContentDefinition
    {
        bool CanRetry(Code blocker);
        /// <summary>Pure positive delay in the actual session, for a positive consecutive failure count.</summary>
        long Delay(ISimulationTime time, int failures);
    }

    public abstract class WorkRetryProfileDef : Def, IWorkRetryPolicy
    {
        public abstract bool CanRetry(Code blocker);
        public abstract long Delay(ISimulationTime time, int failures);
    }

    public sealed class AuthoredWorkRetryProfileDef : WorkRetryProfileDef
    {
        private WorkRetryDeclaration _declaration;
        public GameDuration InitialDelay => _declaration.InitialDelay;
        public GameDuration MaximumDelay => _declaration.MaximumDelay;
        public int Multiplier => _declaration.Multiplier;
        public IReadOnlyList<Code> RetryBlockers => _declaration.RetryBlockers;
        protected override void ReadCore(JsonReader reader) => _declaration = WorkRetryDeclaration.Read(reader);
        public override bool CanRetry(Code blocker) => _declaration.CanRetry(blocker);
        public override long Delay(ISimulationTime time, int failures) => _declaration.Delay(time, failures);
    }
    /// <summary>Sole native JSON retry reader and capped backoff decision, with no job or clock state.</summary>
    public sealed class WorkRetryDeclaration
    {
        private WorkRetryDeclaration() { }
        public GameDuration InitialDelay { get; private set; }
        public GameDuration MaximumDelay { get; private set; }
        public int Multiplier { get; private set; }
        public IReadOnlyList<Code> RetryBlockers { get; private set; }
        public static WorkRetryDeclaration Read(JsonReader reader)
        {
            var declaration = new WorkRetryDeclaration(); declaration.ReadCore(reader); return declaration;
        }
        private void ReadCore(JsonReader reader)
        {
            InitialDelay = (reader.Child("initial_delay") ?? throw reader.Error("missing initial_delay")).RationalDuration();
            MaximumDelay = (reader.Child("maximum_delay") ?? throw reader.Error("missing maximum_delay")).RationalDuration();
            Multiplier = reader.Int("multiplier");
            if (InitialDelay.IsZero || MaximumDelay.IsZero || Multiplier < 1)
                throw reader.Error("retry durations must be positive and multiplier at least one");
            string[] ids = reader.Strings("retry_blockers");
            if (ids.Length == 0) throw reader.Error("retry_blockers must explicitly name at least one blocker");
            Array.Sort(ids, StringComparer.Ordinal); var blockers = new Code[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                try { ContractIdentity.ValidateId(ids[i], "retry blocker"); }
                catch (ArgumentException ex) { throw reader.Error(ex.Message); }
                if (i > 0 && ids[i] == ids[i - 1]) throw reader.Error("duplicate retry blocker '" + ids[i] + "'");
                blockers[i] = new Code(ids[i]);
            }
            RetryBlockers = Array.AsReadOnly(blockers);
        }
        public bool CanRetry(Code blocker)
        {
            if (blocker.IsNone) return false;
            foreach (Code configured in RetryBlockers) if (configured == blocker) return true;
            return false;
        }
        public long Delay(ISimulationTime time, int failures)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            if (failures < 1) throw new ArgumentOutOfRangeException(nameof(failures));
            long delay = time.Ticks(InitialDelay), maximum = time.Ticks(MaximumDelay);
            if (delay <= 0 || maximum < delay) throw new ContentException("retry maximum must cover its positive initial delay in the selected session");
            if (Multiplier == 1) return delay;
            for (int i = 1; i < failures && delay < maximum; i++)
            {
                if (delay > maximum / Multiplier) return maximum;
                delay = checked(delay * Multiplier);
            }
            return delay;
        }
    }
}
