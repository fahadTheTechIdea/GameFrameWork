using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum WorkerDispatchOrdering : byte { PriorityFirst = 0, StarvationFirst = 1 }
    /// <summary>Pure immutable content. WorkSystem owns ranking; existing intent owners retain age.</summary>
    public interface IWorkerDispatchPolicy : IContentDefinition
    {
        WorkerDispatchOrdering Ordering { get; }
        IReadOnlyList<string> DispatchScopes { get; }
        void ValidateTime(ISimulationTime time);
        /// <summary>Null explicitly excludes the offer. Otherwise positive ticks in the actual session.</summary>
        long? StarvationAfter(ISimulationTime time, WorkerDispatchOffer offer);
    }

    public abstract class DispatchProfileDef : Def, IWorkerDispatchPolicy
    {
        public abstract WorkerDispatchOrdering Ordering { get; }
        public abstract IReadOnlyList<string> DispatchScopes { get; }
        public abstract void ValidateTime(ISimulationTime time);
        public abstract long? StarvationAfter(ISimulationTime time, WorkerDispatchOffer offer);
    }

    public interface IWorkerDispatchPreemptionPolicy
    {
        /// <summary>Pure willingness check over detached facts; engine safety and real admission remain authoritative.</summary>
        bool CanPreempt(WorkerDispatchOffer challenger, WorkExecutionFacts incumbent);
    }

    public sealed class AuthoredDispatchProfileDef : DispatchProfileDef, IWorkerDispatchPreemptionPolicy
    {
        private DispatchPolicyDeclaration _declaration;
        public GameDuration StarvationDelay => _declaration.StarvationDelay;
        public bool PreemptEmergencies => _declaration.PreemptEmergencies;
        public override WorkerDispatchOrdering Ordering => _declaration.Ordering;
        public override IReadOnlyList<string> DispatchScopes => _declaration.DispatchScopes;
        protected override void ReadCore(JsonReader reader) => _declaration = DispatchPolicyDeclaration.Read(reader);
        public override void ValidateTime(ISimulationTime time) => _declaration.ValidateTime(time);
        public override long? StarvationAfter(ISimulationTime time, WorkerDispatchOffer offer) => _declaration.StarvationAfter(time, offer);
        public bool CanPreempt(WorkerDispatchOffer challenger, WorkExecutionFacts incumbent) => _declaration.CanPreempt(challenger, incumbent);
    }
    /// <summary>Sole native authored dispatch decision reader; WorkSystem retains ranking and interruption safety.</summary>
    public sealed class DispatchPolicyDeclaration
    {
        private DispatchPolicyDeclaration() { }
        private IReadOnlyList<string> _scopes;
        public GameDuration StarvationDelay { get; private set; }
        public bool PreemptEmergencies { get; private set; }
        private WorkerDispatchOrdering _ordering;
        public WorkerDispatchOrdering Ordering => _ordering;
        public IReadOnlyList<string> DispatchScopes => _scopes;
        public static DispatchPolicyDeclaration Read(JsonReader reader)
        {
            var declaration = new DispatchPolicyDeclaration(); declaration.ReadCore(reader); return declaration;
        }
        private void ReadCore(JsonReader reader)
        {
            PreemptEmergencies = reader.Bool("preempt_emergencies");
            _ordering = ReadOrdering(reader);
            StarvationDelay = (reader.Child("starvation_after") ?? throw reader.Error("missing starvation_after")).RationalDuration();
            if (StarvationDelay.IsZero) throw reader.Error("starvation_after must be positive");
            if (reader.Array("scopes") == null) throw reader.Error("scopes must be explicit, including an intentional empty list");
            string[] scopes = reader.Strings("scopes"); Array.Sort(scopes, StringComparer.Ordinal);
            for (int i = 0; i < scopes.Length; i++)
            {
                try { ContractIdentity.ValidateId(scopes[i], "dispatch policy scope"); }
                catch (ArgumentException ex) { throw reader.Error(ex.Message); }
                if (i > 0 && scopes[i] == scopes[i - 1]) throw reader.Error("duplicate dispatch policy scope");
            }
            _scopes = Array.AsReadOnly(scopes);
        }
        public static WorkerDispatchOrdering ReadOrdering(JsonReader reader)
        {
            string value = reader.String("ordering");
            return value == "priority-first" ? WorkerDispatchOrdering.PriorityFirst :
                value == "starvation-first" ? WorkerDispatchOrdering.StarvationFirst :
                throw reader.Error("ordering must explicitly be priority-first or starvation-first");
        }
        public void ValidateTime(ISimulationTime time)
        {
            if (time == null || time.Ticks(StarvationDelay) <= 0)
                throw new ContentException("dispatch policy needs a positive authored session threshold");
        }
        public long? StarvationAfter(ISimulationTime time, WorkerDispatchOffer offer)
        {
            ValidateTime(time);
            foreach (string scope in DispatchScopes) if (scope == offer.Scope) return time.Ticks(StarvationDelay);
            return null;
        }
        public bool CanPreempt(WorkerDispatchOffer challenger, WorkExecutionFacts incumbent)
            => PreemptEmergencies && challenger.Emergency && !incumbent.Emergency && !incumbent.IsHaul;
    }
}
