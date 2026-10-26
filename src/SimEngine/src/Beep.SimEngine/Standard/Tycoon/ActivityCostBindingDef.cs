using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional Tycoon authoring; existing work admission, receipts and spending remain authoritative.</summary>
    public sealed class ActivityCostBindingDef : WorkRequirementDef, IActivityRequirementBinding
    {
        private string _costId;
        private IWorkRequirement _physical;
        private IWorkCostRequirement _cost;
        private bool _bound;
        public string ActivityId { get; private set; }
        public string NodeId { get; private set; }
        public WorkRequirementMode Mode => WorkRequirementMode.All;
        public IReadOnlyList<WorkRequirementEntry> Entries { get; private set; }
        private IReadOnlyList<IContentDefinition> _references;
        public override IReadOnlyList<IContentDefinition> ContentDependencies => _references;

        protected override void ReadCore(JsonReader reader)
        {
            ActivityId = reader.String("activity"); NodeId = reader.String("node"); _costId = reader.String("cost");
            try
            {
                ContractIdentity.ValidateId(ActivityId, "activity");
                ContractIdentity.ValidateId(NodeId, "node");
                ContractIdentity.ValidateId(_costId, "cost");
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _cost = content.Def<IWorkCostRequirement>(_costId);
            PublishEntries();
        }
        public void BindPhysicalRequirement(IWorkRequirement physical)
        {
            RequireMutable();
            if (_bound) throw new ContentException(Id + ": activity cost binding can attach only once");
            _bound = true; _physical = physical; PublishEntries();
        }
        private void PublishEntries()
        {
            if (!_bound || _cost == null) return;
            var entries = new List<WorkRequirementEntry> { new WorkRequirementEntry("cost", _cost) };
            var references = new List<IContentDefinition> { _cost };
            if (_physical != null)
            {
                if (ReferenceEquals(_physical, this)) throw new ContentException(Id + ": self-referencing activity binding");
                entries.Add(new WorkRequirementEntry("physical", _physical)); references.Add(_physical);
            }
            Entries = entries.AsReadOnly(); _references = references.AsReadOnly();
        }
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            var activity = content.Def<IAuthoredActivityDefinition>(ActivityId);
            if (!_bound || Entries == null || !ReferenceEquals(activity.Graph.Find(NodeId)?.Requirement, this))
                throw content.Fail("cost binding requires an actual authored activity node");
        }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context) => context.Evaluate(this);
    }
}
