using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface ITransformationStaffingRequirement
    {
        string Id { get; }
        string Plan { get; }
        string Node { get; }
        int Count { get; }
        IReadOnlyList<WorkerRoleId> Roles { get; }
        IAuthoredActivityDefinition Activity { get; }
    }
    public interface ITransformationStaffing : ITransformationGate
    {
        TransformationStaffingSnapshot ReadStaffing(World world, ITransformer processor, ITransformationDefinition transformation);
    }
    public interface ITransformationStaffingLifecycle : INamedEntityCapability
    {
        void PrepareStaffing(World world, ITransformer processor);
    }
    public sealed class TransformationStaffingNeedSnapshot
    {
        public string Id { get; }
        public string Plan { get; }
        public string Node { get; }
        public int Count { get; }
        public IReadOnlyList<WorkerRoleId> Roles { get; }
        public IReadOnlyList<WorkExecutionFacts> Assignments { get; }
        public bool Ready => Assignments.Count == Count;
        public Code Blocker => Ready ? Code.None : StaffedTransformationGate.Shortage;
        public TransformationStaffingNeedSnapshot(ITransformationStaffingRequirement requirement, IEnumerable<WorkExecutionFacts> assignments)
        {
            if (requirement == null || assignments == null) throw new ArgumentNullException();
            Id = requirement.Id; Plan = requirement.Plan; Node = requirement.Node; Count = requirement.Count;
            Roles = new List<WorkerRoleId>(requirement.Roles).AsReadOnly();
            Assignments = new List<WorkExecutionFacts>(assignments).AsReadOnly();
        }
    }
    public sealed class TransformationStaffingSnapshot
    {
        public Code Capability { get; }
        public bool Applies { get; }
        public Code Blocker { get; }
        public IReadOnlyList<TransformationStaffingNeedSnapshot> Requirements { get; }
        public TransformationStaffingSnapshot(Code capability, bool applies, Code blocker, IEnumerable<TransformationStaffingNeedSnapshot> requirements)
        {
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            Capability = capability; Applies = applies; Blocker = blocker;
            Requirements = new List<TransformationStaffingNeedSnapshot>(requirements).AsReadOnly();
        }
    }
    public interface IStaffedTransformationGateDefinition : INamedCapabilityDefinition
    {
        bool ProcessBound { get; }
        int OrdersPerUpdate { get; }
        IReadOnlyList<Code> Processes { get; }
        IReadOnlyList<ITransformationDefinition> Transformations { get; }
        IReadOnlyList<ITransformationStaffingRequirement> Requirements { get; }
    }

    /// <summary>Sole staffing JSON authoring owner; no worker, order or material execution state.</summary>
    public sealed class StaffedTransformationGateDeclaration
    {
        private sealed class Need : ITransformationStaffingRequirement
        {
            internal string ActivityId;
            internal string[] RoleIds;
            public IAuthoredActivityDefinition Activity { get; internal set; }
            public string Id { get; internal set; }
            public string Plan => Activity.PlanKey;
            public string Node { get; internal set; }
            public int Count { get; internal set; }
            public IReadOnlyList<WorkerRoleId> Roles { get; internal set; }
        }
        private string[] _transformations;
        public bool ProcessBound { get; private set; }
        public int OrdersPerUpdate { get; private set; }
        public IReadOnlyList<Code> Processes { get; private set; } = Array.Empty<Code>();
        private readonly List<Need> _needs = new List<Need>();
        public Code CapabilityId { get; private set; }
        public IReadOnlyList<ITransformationDefinition> Transformations { get; private set; }
        public IReadOnlyList<ITransformationStaffingRequirement> Requirements { get; private set; }
        private StaffedTransformationGateDeclaration(JsonReader reader)
        {
            CapabilityId = new Code(reader.String("id"));
            string mode = reader.String("mode");
            if (mode != "facility" && mode != "process") throw reader.Error("staffing mode must be explicit facility or process");
            ProcessBound = mode == "process";
            if (ProcessBound)
            {
                var processes = new List<Code>(); var seen = new HashSet<Code>();
                foreach (string id in reader.Strings("processes"))
                {
                    ContractIdentity.ValidateId(id, "staffed processor"); var process = new Code(id);
                    if (!seen.Add(process)) throw reader.Error("duplicate staffed processor"); processes.Add(process);
                }
                processes.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); Processes = processes.AsReadOnly();
                OrdersPerUpdate = reader.Int("orders_per_update");
                if (processes.Count == 0 || OrdersPerUpdate < 1) throw reader.Error("process staffing requires processors and an explicit positive posting budget");
            }
            try { ContractIdentity.ValidateId(CapabilityId.Id, "staffing gate"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            _transformations = reader.Strings("transformations");
            var transforms = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in _transformations) if (!transforms.Add(id)) throw reader.Error("duplicate staffing transformation");
            JsonValue needs = reader.Array("requirements");
            if (_transformations.Length == 0 || needs == null || needs.Count == 0) throw reader.Error("explicit transformations and staffing requirements required");
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < needs.Count; i++)
            {
                var row = new JsonReader(needs[i], reader.Where + ".requirements[" + i + "]");
                var need = new Need { Id = row.String("id"), ActivityId = row.String("activity"), Node = row.String("node"),
                    Count = row.Int("count"), RoleIds = row.Strings("roles") }; row.End();
                try { ContractIdentity.ValidateId(need.Id, "staffing requirement"); }
                catch (ArgumentException error) { throw row.Error(error.Message); }
                if (!identities.Add(need.Id) || need.Count < 1 || need.RoleIds.Length == 0)
                    throw row.Error("staffing requires unique identities, positive counts and explicit alternative roles");
                _needs.Add(need);
            }
            _needs.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        }
        public static StaffedTransformationGateDeclaration Read(JsonReader reader)
            => new StaffedTransformationGateDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Transformations != null) throw new InvalidOperationException("staffing metadata resolves once");
            var transformations = new List<ITransformationDefinition>();
            foreach (string id in _transformations) transformations.Add(content.Def<ITransformationDefinition>(id));
            transformations.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); Transformations = transformations.AsReadOnly();
            var requirements = new List<ITransformationStaffingRequirement>();
            foreach (var need in _needs)
            {
                need.Activity = content.Def<IAuthoredActivityDefinition>(need.ActivityId);
                var node = need.Activity.Graph.Find(need.Node);
                if (node == null || !ActivityExecutorContracts.RequiresWorker(node) || node.Duration.IsZero)
                    throw content.Fail("staffing must reference a positive-duration machine activity node");
                var roles = new List<WorkerRoleId>(); var seen = new HashSet<WorkerRoleId>();
                foreach (string id in need.RoleIds)
                {
                    WorkerRoleId role = content.Def<IWorkerRoleDefinition>(id).Role; bool allowed = false;
                    foreach (var candidate in node.Roles) if (candidate == role) allowed = true;
                    if (!allowed || !seen.Add(role)) throw content.Fail("staffing role must be unique and allowed by its actual node");
                    roles.Add(role);
                }
                roles.Sort((a, b) => string.CompareOrdinal(a.Value, b.Value)); need.Roles = roles.AsReadOnly(); requirements.Add(need);
            }
            Requirements = requirements.AsReadOnly();
        }
        public void ValidateOwner(EntityDef owner)
            => StaffingGateBindingContracts.ValidateOwner(owner, ProcessBound, OrdersPerUpdate, Processes, Transformations, Requirements);
    }

    public sealed class StaffedTransformationGateDef : ComponentDef<StaffedTransformationGate>, IStaffedTransformationGateDefinition
    {
        private StaffedTransformationGateDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public bool ProcessBound => _declaration.ProcessBound;
        public int OrdersPerUpdate => _declaration.OrdersPerUpdate;
        public IReadOnlyList<Code> Processes => _declaration.Processes;
        public IReadOnlyList<ITransformationDefinition> Transformations => _declaration.Transformations;
        public IReadOnlyList<ITransformationStaffingRequirement> Requirements => _declaration.Requirements;
        protected override void ReadCore(JsonReader reader) => _declaration = StaffedTransformationGateDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }

    public static class StaffingGateBindingContracts
    {
        internal static void ValidateOwner(EntityDef owner, bool processBound, int ordersPerUpdate, IReadOnlyList<Code> processes,
            IReadOnlyList<ITransformationDefinition> transformations, IReadOnlyList<ITransformationStaffingRequirement> requirements)
        {
            if (processes == null || transformations == null || transformations.Count == 0 || requirements == null || requirements.Count == 0 ||
                processBound && (processes.Count == 0 || ordersPerUpdate < 1) || !processBound && (processes.Count != 0 || ordersPerUpdate != 0))
                throw new ContentException(owner.Id + ": invalid explicit staffing policy");
            foreach (var process in processes)
            {
                bool found = false;
                foreach (var component in owner.Components) if (component is ITransformerDefinition transformer && transformer.CapabilityId == process)
                {
                    found = true;
                    foreach (var recipe in transformations)
                    {
                        bool supported = false; foreach (var actual in transformer.Transformations) if (ReferenceEquals(actual, recipe)) supported = true;
                        if (!supported) throw new ContentException(owner.Id + ": scoped processor cannot execute staffing recipe");
                    }
                }
                if (!found) throw new ContentException(owner.Id + ": missing scoped staffing processor");
            }
            foreach (var transformation in transformations)
            {
                bool supported = false;
                foreach (var component in owner.Components) if (component is ITransformerDefinition transformer)
                    foreach (var actual in transformer.Transformations) if (ReferenceEquals(actual, transformation)) supported = true;
                if (!supported) throw new ContentException(owner.Id + ": staffing names an unsupported transformation");
            }
            foreach (var need in requirements) if (processBound) need.Activity.ValidateTargetDefinition(owner);
            foreach (var need in requirements)
                if (need.Activity is IEntityActivityDefinition targeted && !ReferenceEquals(targeted.TargetDefinition, owner))
                    throw new ContentException(owner.Id + ": staffing activity targets another entity definition");
        }
        public static void ValidateBinding(World world, IStaffedTransformationGateDefinition definition)
        {
            EntityObservation.ReadProvider(world, definition, null, () => {
                var part = ExecutionGateContracts.ValidateReusable(world, definition, typeof(StaffedTransformationGate), 2, null);
                bool processBound = definition.ProcessBound; var processes = definition.Processes;
                var transformations = definition.Transformations; var requirements = definition.Requirements;
                if (transformations == null || requirements == null || processes == null)
                    throw new ContentException(part.Owner.Id + ": missing staffing metadata");
                string previous = null;
                foreach (var recipe in transformations)
                {
                    if (recipe == null || !world.Content.Owns(recipe) || !ReferenceEquals(world.Content.Find<ITransformationDefinition>(recipe.Id), recipe) ||
                        previous != null && string.CompareOrdinal(previous, recipe.Id) >= 0)
                        throw new ContentException(part.Owner.Id + ": staffing requires actual ordered transformation aliases");
                    previous = recipe.Id;
                }
                previous = null;
                foreach (var process in processes)
                {
                    if (process.IsNone || previous != null && string.CompareOrdinal(previous, process.Id) >= 0)
                        throw new ContentException(part.Owner.Id + ": staffing requires unique ordered processor identities");
                    previous = process.Id;
                }
                previous = null; var covered = new HashSet<string>(StringComparer.Ordinal);
                var counts = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var need in requirements)
                {
                    if (need == null || need.Activity == null || !world.Content.Owns(need.Activity) ||
                        !ReferenceEquals(world.Content.Find<IAuthoredActivityDefinition>(need.Activity.Id), need.Activity) ||
                        need.Plan != need.Activity.PlanKey || need.Count < 1 || need.Roles == null || need.Roles.Count == 0 ||
                        previous != null && string.CompareOrdinal(previous, need.Id) >= 0)
                        throw new ContentException(part.Owner.Id + ": staffing requires actual ordered activity requirements");
                    ContractIdentity.ValidateId(need.Id, "staffing requirement"); previous = need.Id;
                    var node = need.Activity.Graph.Find(need.Node);
                    if (node == null || !ActivityExecutorContracts.RequiresWorker(node) || node.Duration.IsZero)
                        throw new ContentException(part.Owner.Id + ": staffing requires a positive-duration machine node");
                    var seen = new HashSet<WorkerRoleId>(); string previousRole = null;
                    foreach (var role in need.Roles)
                    {
                        bool allowed = false; foreach (var actual in node.Roles) if (actual == role) allowed = true;
                        if (!allowed || !seen.Add(role) || previousRole != null && string.CompareOrdinal(previousRole, role.Value) >= 0)
                            throw new ContentException(part.Owner.Id + ": staffing requires ordered compatible role alternatives");
                        previousRole = role.Value;
                    }
                    if (!processBound) continue;
                    if (!covered.Add(need.Plan + "/" + need.Node) || counts.TryGetValue(need.Plan, out int count) && count != need.Count ||
                        node.Roles.Count != need.Roles.Count || !need.Activity.PlayerOrderable || need.Activity.OnComplete != null)
                        throw new ContentException(part.Owner.Id + ": automatic staffing requires complete effect-free teams");
                    counts[need.Plan] = need.Count;
                    foreach (var member in need.Activity.Graph.Nodes)
                        if (!ActivityExecutorContracts.RequiresWorker(member) || member.Duration.IsZero || member.Dependencies.Count != 0 || member.Effect != null)
                            throw new ContentException(part.Owner.Id + ": automatic staffing requires parallel positive-time effect-free nodes");
                }
                if (processBound) foreach (var need in requirements) foreach (var node in need.Activity.Graph.Nodes)
                    if (!covered.Contains(need.Plan + "/" + node.Id)) throw new ContentException(part.Owner.Id + ": uncovered automatic staffing node");
                ValidateOwner(part.Owner, processBound, definition.OrdersPerUpdate, processes, transformations, requirements);
                if (!world.Services.Has<IWorkAssignments>() || !world.Services.Has<IWorkerDispatch>() || processBound && !world.Services.Has<IActivityExecutionOrders>())
                    throw new ContentException(part.Owner.Id + ": selected staffing requires actual work and dispatch services");
                return true;
            });
        }
        internal static void ValidateComposition(World world)
        {
            foreach (var owner in world.Content.All<EntityDef>()) foreach (var part in owner.Components)
            {
                bool runtime = part.ComponentType == typeof(StaffedTransformationGate);
                if (runtime != (part is IStaffedTransformationGateDefinition))
                    throw new ContentException(owner.Id + ": original staffing runtime requires its metadata contract");
                if (runtime) ValidateBinding(world, (IStaffedTransformationGateDefinition)part);
            }
        }
    }
    /// <summary>No execution state: the original work owner alone holds actors, work, physical travel and docks.</summary>
    public sealed class StaffedTransformationGate : Component, ITransformationStaffing, ITransformationStaffingLifecycle, IActivityExecutionLifetime
    {
        public static readonly Code Shortage = new Code("transformation.staffing-shortage");
        private IStaffedTransformationGateDefinition Binding => (IStaffedTransformationGateDefinition)Def;
        public Code CapabilityId => Binding.CapabilityId;
        public TransformationStaffingSnapshot ReadStaffing(World world, ITransformer processor, ITransformationDefinition transformation)
        {
            bool applies = false;
            foreach (var definition in Binding.Transformations) if (ReferenceEquals(definition, transformation)) applies = true;
            if (Binding.ProcessBound && !SupportsProcess(processor)) applies = false;
            if (!applies) return new TransformationStaffingSnapshot(CapabilityId, false, Code.None, Array.Empty<TransformationStaffingNeedSnapshot>());
            var state = Binding.ProcessBound ? TransformationContracts.Read(world, processor) : default;
            ActivityExecutionBinding binding = Binding.ProcessBound && state.QueueCount > 0 && state.Phase != TransformationPhase.Returning && state.Transformation == transformation.Id
                ? new ActivityExecutionBinding(CapabilityId, processor.CapabilityId, state.Queue[0].Execution) : default;
            var workers = new SortedDictionary<EntityId, WorkExecutionFacts>(Comparer<EntityId>.Create((a, b) => a.Value.CompareTo(b.Value)));
            IWorkAssignments assignments = world.Services.Get<IWorkAssignments>();
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive && entity.Owner == Entity.Owner && entity.Get<IWorker>() != null &&
                    assignments.CheckProductiveAssignment(Entity.Owner, entity.Id, Entity.Id, out var fact).IsNone && fact.HasValue)
                    if (!Binding.ProcessBound || binding.IsBound && fact.Value.ExecutionBinding.Equals(binding)) workers.Add(entity.Id, fact.Value);
            var capacities = new List<int>(); var compatible = new List<IReadOnlyList<EntityId>>(); long needed = 0;
            foreach (var need in Binding.Requirements)
            {
                capacities.Add(need.Count); needed += need.Count; var row = new List<EntityId>();
                foreach (var pair in workers)
                {
                    if (pair.Value.Plan != need.Plan || pair.Value.Node != need.Node) continue;
                    foreach (var role in need.Roles) if (pair.Value.Role == role) { row.Add(pair.Key); break; }
                }
                compatible.Add(row.AsReadOnly());
            }
            var selected = DistinctWorkerMatching.Solve(capacities.AsReadOnly(), compatible.AsReadOnly());
            var facts = new List<TransformationStaffingNeedSnapshot>();
            for (int i = 0; i < Binding.Requirements.Count; i++)
            {
                var matches = new List<WorkExecutionFacts>();
                foreach (var match in selected) if (match.Demand == i) matches.Add(workers[match.Worker]);
                facts.Add(new TransformationStaffingNeedSnapshot(Binding.Requirements[i], matches));
            }
            return new TransformationStaffingSnapshot(CapabilityId, true, selected.Count == needed ? Code.None : Shortage, facts);
        }
        private bool SupportsProcess(ITransformer processor)
        {
            if (processor?.Entity != Entity) return false;
            foreach (var capability in Binding.Processes) if (capability == processor.CapabilityId) return true;
            return false;
        }
        public void PrepareStaffing(World world, ITransformer processor)
        {
            if (!Binding.ProcessBound || !SupportsProcess(processor)) return;
            var state = TransformationContracts.Read(world, processor);
            if (state.QueueCount == 0 || state.Phase == TransformationPhase.Returning || !OperationContracts.ReadControl(world, processor).Enabled) return;
            bool applies = false; foreach (var recipe in Binding.Transformations) if (recipe.Id == state.Transformation) applies = true;
            if (!applies) return;
            var binding = new ActivityExecutionBinding(CapabilityId, processor.CapabilityId, state.Queue[0].Execution);
            var owner = world.Services.Get<IActivityExecutionOrders>(); var orders = new List<ActivityServiceOrder>();
            owner.CopyServiceOrders(Entity.Owner, Entity.Id, binding, orders);
            var posted = new HashSet<string>(StringComparer.Ordinal); int budget = Binding.OrdersPerUpdate;
            foreach (var need in Binding.Requirements)
            {
                if (!posted.Add(need.Plan)) continue;
                int active = 0; foreach (var order in orders) if (order.Active && order.Plan == need.Plan) active++;
                var activity = world.Services.Get<IActivityCatalog>().ResolvePlan(need.Plan) as IPlayerActivityDefinition;
                while (active < need.Count && budget > 0)
                {
                    Code refusal = owner.PostServiceActivity(Entity.Owner, Entity.Id, need.Plan, activity.Priority, binding, out _);
                    if (!refusal.IsNone) break;
                    active++; budget--;
                }
            }
        }
        public bool ValidateExecution(World world, string plan, ActivityExecutionBinding binding)
        {
            var processor = Entity.Get<ITransformer>(binding.Capability); bool authored = false;
            foreach (var need in Binding.Requirements) if (need.Plan == plan) authored = true;
            if (!Binding.ProcessBound || binding.Source != CapabilityId || !authored || !SupportsProcess(processor))
                throw new CorruptStateException("work lifetime binding differs from its authored processor/team");
            var state = TransformationContracts.Read(world, processor);
            if (binding.Execution >= state.NextExecution)
                throw new CorruptStateException("work refers to a never-allocated process execution");
            return state.QueueCount > 0 && state.Phase != TransformationPhase.Returning && state.Queue[0].Execution == binding.Execution;
        }
        public bool RetainProductiveWork(World world, Order order, IActivityNodeDefinition node)
        {
            ValidateExecution(world, order.PlanKey, order.ExecutionBinding);
            var state = TransformationContracts.Read(world, Entity.Get<ITransformer>(order.ExecutionBinding.Capability));
            return state.QueueCount > 0 && state.Phase != TransformationPhase.Returning &&
                state.Queue[0].Execution == order.ExecutionBinding.Execution;
        }
        public ExecutionGateSnapshot ReadGate(World world, ITransformer processor, ITransformationDefinition transformation)
        {
            TransformationStaffingSnapshot facts = ReadStaffing(world, processor, transformation);
            return new ExecutionGateSnapshot(CapabilityId, facts.Applies, facts.Blocker, Array.Empty<ExecutionGateConditionSnapshot>());
        }
    }
}
