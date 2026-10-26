using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent mod-owned intent schema. Marker is numeric author/player data, not an entity reference.
    public sealed class ReferenceSchemaPlanDef : Def, IPlayerActivityDefinition, IEntityTargetPlanDefinition, IActivityWorkflowBinding
    {
        private string _activity, _mode;
        private IAuthoredActivityDefinition _workflow;
        public string PlanKey => "reference-plan:" + Id;
        public IActivityDefinition Workflow => _workflow;
        public IActivityGraph Graph => _workflow.Graph;
        public bool AutomaticSupply => _workflow.AutomaticSupply;
        public string OnComplete => _workflow.OnComplete;
        public int Priority => _workflow.Priority;
        public IReadOnlyList<IContentDefinition> DisclosureContent => Array.AsReadOnly(new IContentDefinition[] { this, _workflow });
        protected override void ReadCore(JsonReader reader)
        { _activity = reader.String("activity"); _mode = reader.String("mode"); }
        protected override void ResolveCore(ContentResolver content) => _workflow = content.Def<IAuthoredActivityDefinition>(_activity);
        private JsonValue Core(JsonValue arguments)
        {
            var reader = new JsonReader(arguments, PlanKey + ".intent");
            int site = reader.Int("site"); int worker = reader.Int("operator"); _ = reader.Int("marker"); reader.End();
            if (site <= 0 || worker < 0) throw reader.Error("explicit site and optional operator identity required");
            var selected = worker == 0 ? ActivityWorkerSelection.Unbound :
                new ActivityWorkerSelection(new[] { new ActivityWorkerBinding(Graph.Nodes[0].Id, new EntityId(worker)) });
            return ActivityWorkerSelection.CreateArguments(new EntityId(site), selected);
        }
        public IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments) => PlanEntityReferenceContracts.Read(this, Core(arguments));
        public Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination)
            => _mode == "deny" ? new Code("proof.plan-replacement-denied") : PlanEntityReferenceContracts.Check(this, world, company, Core(arguments), source, destination);
        public JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination)
        {
            _ = Core(arguments); var copy = arguments.Clone();
            if (_mode == "omit-rebind") return copy;
            if (copy.Get("site").Text == source.Value.ToString(CultureInfo.InvariantCulture))
                copy.Set("site", JsonValue.Number(destination.Value.ToString(CultureInfo.InvariantCulture)));
            if (copy.Get("operator").Text == source.Value.ToString(CultureInfo.InvariantCulture))
                copy.Set("operator", JsonValue.Number(destination.Value.ToString(CultureInfo.InvariantCulture)));
            return copy;
        }
        public IActivityDefinition BindExecution(World world, EntityId target) => this;
        public string IntentIdentity(World world, JsonValue arguments)
        { var selection = ActivityWorkerSelection.Read(Core(arguments), Graph, PlanKey + ".core", out var target); return selection.Identity(target); }
        public string ExecutionIdentity(World world, Order order) => order.WorkerSelection.Identity(order.Target);
        public void ValidateTargetDefinition(EntityDef target) => _workflow.ValidateTargetDefinition(target);
        public PlanIntent CreateTargetIntent(EntityId target, IActivityWorkerSelection workers, string group, int priority)
            => throw new ContentException("this proof requires its explicit mod-owned schema, not recurring target binding");
        public Code CheckIntent(World world, PlayerId company, Entity target) => _workflow.CheckIntent(world, company, target);
        public Code CheckSubmission(World world, PlayerId company, JsonValue arguments)
        {
            var selection = ActivityWorkerSelection.Read(Core(arguments), Graph, PlanKey + ".core", out var target);
            Code result = world.Services.Get<WorkBoard>().CheckPlayerActivity(company, PlanKey, target, 0);
            return result.IsNone ? selection.Check(world, company, Graph) : result;
        }
        public Code CheckActivation(World world, PlayerId company, JsonValue arguments) => CheckSubmission(world, company, arguments);
        public Order Activate(World world, PlayerId company, JsonValue arguments, int priority)
        {
            if (!CheckActivation(world, company, arguments).IsNone) throw new InvalidOperationException("mod plan refused");
            var selection = ActivityWorkerSelection.Read(Core(arguments), Graph, PlanKey + ".core", out var target);
            return world.Services.Get<WorkBoard>().Post(this, company, target, priority, selection);
        }
    }
    internal sealed class ReferenceSchemaPlanSource : IActivitySource
    {
        public IReadOnlyList<IActivityDefinition> ActivityDefinitions { get; }
        internal ReferenceSchemaPlanSource(ResolvedContent content)
        {
            var definitions = new List<IActivityDefinition>();
            foreach (var definition in content.All<ReferenceSchemaPlanDef>()) definitions.Add(definition);
            ActivityDefinitions = definitions.AsReadOnly();
        }
    }
}
