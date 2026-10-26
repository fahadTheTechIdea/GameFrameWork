using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional pure reference declaration on an existing module provider or controller.
    /// Read authoritative provider state; do not create another pin/history authority.</summary>
    public interface IPlanHistoryRetention
    {
        bool RetainsPlan(World world, PlayerId company, long plan);
    }

    /// <summary>Explicit immutable atomic retirement intent, never a completion grant.</summary>
    public sealed class PlanHistorySelection
    {
        public IReadOnlyList<long> Plans { get; }
        public PlanHistorySelection(IEnumerable<long> plans)
        {
            if (plans == null) throw new ArgumentNullException(nameof(plans));
            Plans = PlanIntent.CaptureDependencies(plans);
            if (Plans.Count == 0) throw new ArgumentException("history selection must contain actual plan identities");
        }
        internal static void Sync(IStateSync sync, ref PlanHistorySelection selection)
        {
            bool present = selection != null; sync.Bool(ref present);
            if (!present) { if (sync.Reading) selection = null; return; }
            IReadOnlyList<long> plans = selection?.Plans;
            PlanIntent.SyncDependencies(sync, ref plans);
            if (sync.Reading)
                try { selection = new PlanHistorySelection(plans); }
                catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
        }
    }
}
