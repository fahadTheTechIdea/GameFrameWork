using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Engine execution only; clients receive detached result views, never this context.</summary>
    public readonly struct WorkRequirementContext
    {
        public World World { get; }
        public PlayerId Owner { get; }
        public Entity Target { get; }
        public string Node => Definition.Id;
        public IActivityNodeDefinition Definition { get; }
        public Entity Worker { get; }
        /// <summary>Actual role chosen by the existing worker selector or retained live job.</summary>
        public WorkerRoleId Role { get; }
        public Fix Multiplier => Definition.RequirementMultiplier;
        public WorkProgress Progress { get; }
        public WorkProgress NextProgress { get; }
        public long ProductiveSteps { get; }
        public long NextProductiveSteps { get; }
        /// <summary>Existing committed source for this exact resource occurrence; null before acquisition.</summary>
        public ResourceEndpointId? CommittedSource { get; }
        internal ActivityNodeExecution Execution { get; }
        internal IReadOnlyList<WorkRequirementChoice> Choices { get; }
        internal WorkRequirementContext(World world, PlayerId owner, Entity target, IActivityNodeDefinition definition, Entity worker = null,
            IReadOnlyList<WorkRequirementChoice> choices = null, ActivityNodeExecution execution = null, WorkerRoleId role = default)
        {
            World = world; Owner = owner; Target = target; Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Worker = worker; Role = role; Choices = choices; Execution = execution; Progress = execution?.Progress ?? WorkProgress.Zero;
            CommittedSource = null;
            NextProgress = Progress.Advance(definition.Roles.Count == 0 ? Fix.One : worker == null ? Fix.Zero :
                world.Services.Get<IWorkerDispatch>().EffectiveWorkRate(world, worker), world.Time.Ticks(definition.Duration));
            ProductiveSteps = execution?.ProductiveSteps ?? 0;
            bool serviceDuty = execution != null && worker != null && world.Services.Get<IWorkerDispatch>().EffectiveWorkRate(world, worker).IsPositive &&
                world.Services.Get<WorkBoard>().RetainProductiveWork(world.Services.Get<WorkBoard>().Order(execution.OrderId), definition);
            NextProductiveSteps = NextProgress.Equals(Progress) && !serviceDuty ? ProductiveSteps : checked(ProductiveSteps + 1);
        }
        private WorkRequirementContext(WorkRequirementContext original, ResourceEndpointId? source)
        {
            World = original.World; Owner = original.Owner; Target = original.Target; Definition = original.Definition;
            Worker = original.Worker; Role = original.Role; Choices = original.Choices; Execution = original.Execution;
            Progress = original.Progress; NextProgress = original.NextProgress;
            ProductiveSteps = original.ProductiveSteps; NextProductiveSteps = original.NextProductiveSteps; CommittedSource = source;
        }
        internal WorkRequirementContext WithSource(ResourceEndpointId? source) => new WorkRequirementContext(this, source);
        public WorkRequirementResult Evaluate(IWorkRequirement requirement)
            => World.Services.Get<WorkBoard>().EvaluateRequirement(requirement, this, null);
    }

    /// <summary>A deterministic, nonmutating company/target guard. Evaluate must never reserve or consume.</summary>
    public interface IWorkRequirement : IContentDependencyDefinition, IDependencyNode
    {
        new string Id { get; }
        WorkRequirementResult Evaluate(WorkRequirementContext context);
    }

    /// <summary>Evaluate against context.Worker. The engine reports pending when no candidate is supplied.</summary>
    public interface IWorkerWorkRequirement : IWorkRequirement { }
    /// <summary>Offers existing physically usable source IDs. Pure guards/offers never acquire or consume.</summary>
    public interface IResourceWorkRequirement : IWorkRequirement
    {
        /// <summary>Native startup/held bill; total budget for Continuous; reference quantities derived from Rates for Elapsed.</summary>
        Bill Quantities { get; }
        IReadOnlyList<IResourceLotSelectionDefinition> Selections { get; }
        WorkResourceUse Use { get; }
        /// <summary>Explicit exact game-time rates for Elapsed; empty for other uses. Never derived from activity duration.</summary>
        IReadOnlyList<IResourceRate> Rates { get; }
        /// <summary>None publishes actual candidates; a typed physical refusal requires an empty list. No stock observation or acquisition.</summary>
        Code CopySources(WorkRequirementContext context, IList<WorkResourceSource> destination);
        WorkRequirementResult CheckSource(WorkRequirementContext context, ResourceEndpointId endpoint);
    }
    /// <summary>Explicit target physical-port input intent may be supplied by the existing logistics owner.</summary>
    public interface IWorkSupplyRequirement : IResourceWorkRequirement { Code Port { get; } }
    public interface IWorkCostRequirement : IWorkRequirement
    {
        Fix Amount { get; }
        bool Credit { get; }
        MonetaryCostCategory CostCategory { get; }
    }

    /// <summary>Authored providers register normal module-owned sections of their concrete subclasses.</summary>
    public abstract class WorkRequirementDef : Def, IWorkRequirement, IDependencyNode
    {
        public abstract IReadOnlyList<IContentDefinition> ContentDependencies { get; }
        public abstract WorkRequirementResult Evaluate(WorkRequirementContext context);
        IReadOnlyList<string> IDependencyNode.Dependencies => WorkRequirementContracts.Dependencies(this);
    }
    public static class WorkRequirementContracts
    {
        public static IReadOnlyList<string> Dependencies(IWorkRequirement requirement)
        {
            if (!(requirement is IWorkRequirementGroup group)) return Array.Empty<string>();
            if (group.Entries == null) throw new ContentException(requirement.Id + ": requirement group entries are required");
            var ids = new string[group.Entries.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = group.Entries[i]?.Requirement?.Id;
            return Array.AsReadOnly(ids);
        }
    }

    public enum WorkRequirementMode : byte { All = 0, Any = 1 }
    public interface IWorkRequirementGroup : IWorkRequirement
    {
        WorkRequirementMode Mode { get; }
        IReadOnlyList<WorkRequirementEntry> Entries { get; }
    }
    public sealed class WorkRequirementEntry
    {
        public string Id { get; }
        public IWorkRequirement Requirement { get; }
        public WorkRequirementEntry(string id, IWorkRequirement requirement)
        {
            if (string.IsNullOrWhiteSpace(id) || requirement == null) throw new ArgumentException("a requirement entry needs a stable identity and provider");
            Id = id; Requirement = requirement;
        }
    }
    /// <summary>Authored Boolean composition over the same requirement family and content aliases.</summary>
    public sealed class WorkRequirementGroupDeclaration
    {
        private string[] _ids, _requirements;
        public WorkRequirementMode Mode { get; private set; }
        public IReadOnlyList<WorkRequirementEntry> Entries { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        private WorkRequirementGroupDeclaration(JsonReader reader)
        {
            string mode = reader.String("mode");
            if (mode == "all") Mode = WorkRequirementMode.All;
            else if (mode == "any") Mode = WorkRequirementMode.Any;
            else throw reader.Error("mode must be all or any");
            JsonValue entries = reader.Array("entries") ?? throw reader.Error("a requirement group needs entries");
            if (entries.Count == 0) throw reader.Error("a requirement group needs at least one entry");
            _ids = new string[entries.Count]; _requirements = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = new JsonReader(entries[i], reader.Where + ".entries[" + i + "]");
                _ids[i] = entry.String("id"); _requirements[i] = entry.String("requirement"); entry.End();
                if (string.IsNullOrWhiteSpace(_ids[i]) || string.IsNullOrWhiteSpace(_requirements[i])) throw entry.Error("entry identities and references must be nonempty");
            }
        }
        public static WorkRequirementGroupDeclaration Read(JsonReader reader)
            => new WorkRequirementGroupDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            var entries = new List<WorkRequirementEntry>();
            if (_ids == null) throw new InvalidOperationException("requirement group resolves once");
            for (int i = 0; i < _ids.Length; i++) entries.Add(new WorkRequirementEntry(_ids[i], content.Def<IWorkRequirement>(_requirements[i])));
            entries.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); Entries = entries.AsReadOnly();
            var references = new List<IContentDefinition>();
            foreach (WorkRequirementEntry entry in Entries) references.Add(entry.Requirement);
            ContentDependencies = references.AsReadOnly();
            _ids = _requirements = null;
        }
    }
    public sealed class WorkRequirementGroupDef : WorkRequirementDef, IWorkRequirementGroup
    {
        public override IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        private WorkRequirementGroupDeclaration _declaration;
        public WorkRequirementMode Mode => _declaration.Mode;
        public IReadOnlyList<WorkRequirementEntry> Entries => _declaration.Entries;
        protected override void ReadCore(JsonReader reader) => _declaration = WorkRequirementGroupDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public override WorkRequirementResult Evaluate(WorkRequirementContext context) => context.Evaluate(this);
    }
}
