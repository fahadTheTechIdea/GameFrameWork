using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum PlanTemplateSelectionKind : byte { Snapshot = 1, Stored = 2 }

    /// <summary>Immutable placement source. Stored identity/revision resolves only inside the engine.</summary>
    public sealed class PlanTemplateSelection
    {
        public PlanTemplateSelectionKind Kind { get; }
        public PlanTemplate Template { get; }
        public long Id { get; }
        public long Revision { get; }
        internal bool Matches(PlanTemplateSelection other) => other != null && Kind == other.Kind && Id == other.Id && Revision == other.Revision
            && (Kind == PlanTemplateSelectionKind.Stored || Template.ToJson().ToString() == other.Template.ToJson().ToString());
        private PlanTemplateSelection(PlanTemplateSelectionKind kind, PlanTemplate template, long id, long revision)
        { Kind = kind; Template = template; Id = id; Revision = revision; }
        public static PlanTemplateSelection Snapshot(PlanTemplate template)
            => new PlanTemplateSelection(PlanTemplateSelectionKind.Snapshot, template ?? throw new ArgumentNullException(nameof(template)), 0, 0);
        public static PlanTemplateSelection Stored(long id, long revision)
        {
            if (id <= 0 || revision <= 0) throw new ArgumentException("stored template selection requires positive actual identity and revision");
            return new PlanTemplateSelection(PlanTemplateSelectionKind.Stored, null, id, revision);
        }
        internal static void Sync(IStateSync sync, ref PlanTemplateSelection selection)
        {
            bool present = selection != null; sync.Bool(ref present);
            if (!present) { if (sync.Reading) selection = null; return; }
            PlanTemplateSelectionKind kind = selection?.Kind ?? default; sync.Enum(ref kind);
            if (kind == PlanTemplateSelectionKind.Snapshot)
            {
                PlanTemplate template = selection?.Template; PlanTemplate.Sync(sync, ref template);
                if (template == null) throw new CorruptStateException("snapshot selection requires explicit template");
                if (sync.Reading) selection = Snapshot(template);
            }
            else if (kind == PlanTemplateSelectionKind.Stored)
            {
                long id = selection?.Id ?? 0, revision = selection?.Revision ?? 0; sync.Long(ref id); sync.Long(ref revision);
                if (id <= 0 || revision <= 0) throw new CorruptStateException("invalid stored template identity/revision");
                if (sync.Reading) selection = Stored(id, revision);
            }
            else throw new CorruptStateException("unknown plan template selection kind");
        }
    }

    /// <summary>Detached private design. The same immutable template value is used by JSON and placement.</summary>
    public sealed class StoredPlanTemplateView
    {
        public long Id { get; }
        public string Name { get; }
        public long Revision { get; }
        public PlanTemplate Template { get; }
        internal StoredPlanTemplateView(long id, string name, long revision, PlanTemplate template)
        { Id = id; Name = name; Revision = revision; Template = template; }
    }
}
