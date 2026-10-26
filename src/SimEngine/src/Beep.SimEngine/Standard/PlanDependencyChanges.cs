using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable replacement intent for the prerequisites of one existing live manual plan.</summary>
    public sealed class PlanDependencyChange
    {
        public long Plan { get; }
        public IReadOnlyList<long> Dependencies { get; }
        public PlanDependencyChange(long plan, IEnumerable<long> dependencies)
        {
            if (plan <= 0) throw new ArgumentException("dependency edits need an actual positive plan ID");
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            Plan = plan; Dependencies = PlanIntent.CaptureDependencies(dependencies);
            foreach (long id in Dependencies)
                if (id == plan) throw new ArgumentException("a plan cannot depend on itself");
        }
    }

    /// <summary>One atomic, immutable command value. Execution/topology remain in the existing board.</summary>
    public sealed class PlanDependencyChanges
    {
        public IReadOnlyList<PlanDependencyChange> Entries { get; }
        public PlanDependencyChanges(IEnumerable<PlanDependencyChange> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var copy = new List<PlanDependencyChange>(entries);
            if (copy.Count == 0) throw new ArgumentException("dependency edits require explicit nonempty changes");
            foreach (var entry in copy) if (entry == null) throw new ArgumentException("dependency edit cannot contain a missing entry");
            copy.Sort((a, b) => a.Plan.CompareTo(b.Plan));
            for (int i = 1; i < copy.Count; i++)
                if (copy[i].Plan == copy[i - 1].Plan) throw new ArgumentException("each plan can be edited only once in an atomic set");
            Entries = copy.AsReadOnly();
        }
        internal static void Sync(IStateSync sync, ref PlanDependencyChanges changes)
        {
            bool present = changes != null; sync.Bool(ref present);
            if (!present) { if (sync.Reading) changes = null; return; }
            int count = sync.Count(changes?.Entries.Count ?? 0);
            var decoded = sync.Reading ? new List<PlanDependencyChange>() : null;
            long previous = 0;
            for (int i = 0; i < count; i++)
            {
                long plan = sync.Reading ? 0 : changes.Entries[i].Plan; sync.Long(ref plan);
                if (plan <= previous) throw new CorruptStateException("dependency edits require canonical positive unique plan IDs");
                previous = plan;
                IReadOnlyList<long> dependencies = sync.Reading ? null : changes.Entries[i].Dependencies;
                PlanIntent.SyncDependencies(sync, ref dependencies);
                if (sync.Reading)
                    try { decoded.Add(new PlanDependencyChange(plan, dependencies)); }
                    catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
            }
            if (sync.Reading)
                try { changes = new PlanDependencyChanges(decoded); }
                catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
        }
    }
}
