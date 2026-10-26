using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IWorkLoadSignalDefinition : IAutomationSignalDefinition
    {
        WorkLoadScope Scope { get; }
        bool IsCapacity { get; }
        WorkerRoleId Role { get; }
    }

    /// <summary>Sole workload JSON declaration; never stores counts, jobs or time.</summary>
    public sealed class WorkLoadSignalDeclaration
    {
        public Code CapabilityId { get; private set; }
        public WorkLoadScope Scope { get; private set; }
        public bool IsCapacity { get; private set; }
        public WorkerRoleId Role { get; private set; }
        public static IReadOnlyList<AutomationMetricSchema> Metrics { get; } = Array.AsReadOnly(new[]
        {
            Metric("work.live-jobs"), Metric("work.activity-jobs"), Metric("work.haul-jobs"),
            Metric("work.open-jobs"), Metric("work.assigned-jobs"), Metric("work.paused-jobs"),
            Metric("work.dock-waiting"), Metric("work.dock-arrived-waiting"),
            Metric("work.workers-total"), Metric("work.workers-busy"), Metric("work.workers-held"),
            Metric("work.workers-idle"), Metric("work.workers-ordinary-ready"), Metric("work.workers-emergency-ready")
        });
        private static AutomationMetricSchema Metric(string id)
            => new AutomationMetricSchema(new Code(id), AutomationSignalKind.Number, new Code("unit.count"));
        private WorkLoadSignalDeclaration(JsonReader reader)
        {
            string id = reader.String("id"), scope = reader.String("scope");
            try { ContractIdentity.ValidateId(id, "workload signal"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id);
            IsCapacity = scope == "fleet" || scope == "fleet_worker";
            Scope = scope == "company" || scope == "fleet" ? WorkLoadScope.Company : scope == "endpoint" ? WorkLoadScope.Endpoint :
                scope == "worker" || scope == "fleet_worker" ? WorkLoadScope.Worker : throw reader.Error("workload scope must be company, endpoint, worker, fleet or fleet_worker");
            if (IsCapacity)
            {
                JsonValue role = reader.Value("role");
                if (role == null || role.Kind != JsonKind.Null && role.Kind != JsonKind.String) throw reader.Error("fleet role must explicitly be null or a declared role ID");
                try { Role = role.Kind == JsonKind.Null ? WorkerRoleId.None : new WorkerRoleId(role.Text); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
            }
        }
        public static WorkLoadSignalDeclaration Read(JsonReader reader)
            => new WorkLoadSignalDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content) { if (Role != WorkerRoleId.None) content.Def<IWorkerRoleDefinition>(Role.Value); }
        public void ValidateOwner(EntityDef owner) => WorkLoadSignalBindingContracts.ValidateOwner(owner, Scope, IsCapacity, Role);
    }

    public sealed class WorkLoadSignalDef : ComponentDef<WorkLoadSignal>, IWorkLoadSignalDefinition
    {
        private WorkLoadSignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public WorkLoadScope Scope => _declaration.Scope;
        public bool IsCapacity => _declaration.IsCapacity;
        public WorkerRoleId Role => _declaration.Role;
        public int SignalSchemaVersion => 2;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => WorkLoadSignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = WorkLoadSignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }

    public static class WorkLoadSignalBindingContracts
    {
        internal static void ValidateOwner(EntityDef owner, WorkLoadScope scope, bool capacity, WorkerRoleId role)
        {
            if (!Enum.IsDefined(typeof(WorkLoadScope), scope) || capacity && scope == WorkLoadScope.Endpoint ||
                !capacity && role != WorkerRoleId.None)
                throw new ContentException(owner.Id + ": invalid workload scope or role selection");
            if (scope == WorkLoadScope.Worker && owner.Component<IWorkerDefinition>() == null)
                throw new ContentException(owner.Id + ": worker workload signal requires an actual worker definition");
        }
        internal static T Observe<T>(World world, IWorkLoadSignalDefinition definition, Entity entity,
            Func<WorkLoadScope, bool, WorkerRoleId, T> read)
            => EntityObservation.ReadProvider(world, definition, entity, () => {
                var part = AutomationSignalContracts.ValidateReusable(world, definition, typeof(WorkLoadSignal), 1, 2,
                    WorkLoadSignalDeclaration.Metrics, entity);
                var scope = definition.Scope; bool capacity = definition.IsCapacity; var role = definition.Role;
                ValidateOwner(part.Owner, scope, capacity, role);
                if (role != WorkerRoleId.None)
                {
                    var selected = world.Content.Find<IWorkerRoleDefinition>(role.Value);
                    if (selected == null || !world.Content.Owns(selected) || selected.Role != role)
                        throw new ContentException(part.Owner.Id + ": workload role must be an actual declared role");
                }
                return read(scope, capacity, role);
            }, observeTargetState: true, faultPhase: "workload.signal-metadata");
        public static void ValidateBinding(World world, IWorkLoadSignalDefinition definition)
            => Observe(world, definition, null, (scope, capacity, role) => true);
        internal static void ValidateComposition(World world)
        {
            foreach (var owner in world.Content.All<EntityDef>()) foreach (var part in owner.Components)
            {
                bool runtime = part.ComponentType == typeof(WorkLoadSignal);
                if (runtime != (part is IWorkLoadSignalDefinition))
                    throw new ContentException(owner.Id + ": original workload runtime requires matching metadata contract");
                if (runtime) ValidateBinding(world, (IWorkLoadSignalDefinition)part);
            }
        }
    }

    public sealed class WorkLoadSignal : Component, IAutomationSignalSource
    {
        private IWorkLoadSignalDefinition Binding => (IWorkLoadSignalDefinition)Def;
        public Code CapabilityId => Binding.CapabilityId;
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            if (world == null || signal == null || Entity == null || !Entity.Alive ||
                !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                !ReferenceEquals(Entity.Get<IAutomationSignalSource>(CapabilityId), this) ||
                !ReferenceEquals(world.Content.Find<IAutomationSignalContent>(signal.Id), signal))
                throw new ArgumentException("workload signals require their actual live session and authored signal");
            return WorkLoadSignalBindingContracts.Observe(world, Binding, Entity, (scope, isCapacity, role) =>
            {
                var query = world.Services.Get<IWorkLoad>();
                if (isCapacity)
                {
                    Code capacityRefusal = query.ReadWorkerCapacity(Entity.Owner, role,
                        scope == WorkLoadScope.Company ? EntityId.None : Entity.Id, out var capacity);
                    if (!ReferenceEquals(world.Services.Get<IWorkLoad>(), query)) throw new InvalidOperationException("worker capacity query authority changed during observation");
                    if (!capacityRefusal.IsNone) return signal.Unavailable(capacityRefusal);
                    int machines;
                    switch (signal.Metric.Id)
                    {
                        case "work.workers-total": machines = capacity.TotalWorkers; break;
                        case "work.workers-busy": machines = capacity.BusyWorkers; break;
                        case "work.workers-held": machines = capacity.HeldWorkers; break;
                        case "work.workers-idle": machines = capacity.IdleWorkers; break;
                        case "work.workers-ordinary-ready": machines = capacity.OrdinaryReady; break;
                        case "work.workers-emergency-ready": machines = capacity.EmergencyReady; break;
                        default: return signal.Unavailable(OperationCodes.SignalUnavailable);
                    }
                    return signal.Number(Fix.FromInt(machines), new Code("unit.count"));
                }
                Code refusal = query.ReadWorkLoad(Entity.Owner, scope,
                    scope == WorkLoadScope.Company ? EntityId.None : Entity.Id, out var value);
                if (!ReferenceEquals(world.Services.Get<IWorkLoad>(), query))
                    throw new InvalidOperationException("workload query authority changed during observation");
                if (!refusal.IsNone) return signal.Unavailable(refusal);
                int count;
                switch (signal.Metric.Id)
                {
                    case "work.live-jobs": count = value.LiveJobs; break;
                    case "work.activity-jobs": count = value.ActivityJobs; break;
                    case "work.haul-jobs": count = value.HaulJobs; break;
                    case "work.open-jobs": count = value.OpenJobs; break;
                    case "work.assigned-jobs": count = value.AssignedJobs; break;
                    case "work.paused-jobs": count = value.PausedJobs; break;
                    case "work.dock-waiting": count = value.DockWaiting; break;
                    case "work.dock-arrived-waiting": count = value.DockArrivedWaiting; break;
                    default: return signal.Unavailable(OperationCodes.SignalUnavailable);
                }
                return signal.Number(Fix.FromInt(count), new Code("unit.count"));
            });
        }
    }
}
