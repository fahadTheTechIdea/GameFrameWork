using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IWorkerExecutionDefinition : INamedCapabilityDefinition { }
    public interface IWorkerExecution : INamedEntityCapability { ExecutionRateSnapshot ReadExecution(World world); }
    public sealed class WorkerExecutionView
    {
        public EntityId Worker { get; }
        public Fix BaseRate { get; }
        public IReadOnlyList<WorkerRoleId> Roles { get; }
        public Fix EffectiveRate { get; }
        public ExecutionGateEvaluation Gates { get; }
        public IReadOnlyList<ExecutionRateSnapshot> Providers { get; }
        internal WorkerExecutionView(EntityId worker, Fix baseRate, IReadOnlyList<WorkerRoleId> roles, ExecutionRateEvaluation value)
        { Worker = worker; BaseRate = baseRate; Roles = roles; EffectiveRate = value.Rate; Gates = value.Gates; Providers = value.Providers; }
    }
    /// <summary>WorkSystem remains the only dispatcher and physical job owner.</summary>
    public static class WorkerExecutionContracts
    {
        private static (Fix Rate, IReadOnlyList<WorkerRoleId> Roles) Metadata(ResolvedContent content, IWorkerDefinition definition)
        {
            Fix rate = definition.WorkRate; var roles = definition.Roles;
            if (!rate.IsPositive || roles == null || roles.Count == 0) throw new InvalidOperationException("worker needs explicit positive base rate and roles");
            var values = new List<WorkerRoleId>(roles.Count); var distinct = new HashSet<WorkerRoleId>();
            foreach (var role in roles)
            {
                if (!role.IsValid || content.Find<IWorkerRoleDefinition>(role.Value)?.Role != role || !distinct.Add(role))
                    throw new InvalidOperationException("worker specification has missing, duplicate or undeclared roles");
                values.Add(role);
            }
            if (definition is IStatefulWorkerDefinition stateful)
            {
                var states = stateful.States;
                if (!typeof(IStatefulWorker).IsAssignableFrom(definition.ComponentType) || states == null || states.Definitions.Count != 3)
                    throw new InvalidOperationException("stateful worker needs its actual state source and three declared modes");
                foreach (string mode in new[] { "working", "loading", "stopped" })
                    if (!states.Definitions.TryGetValue(mode, out var state) || state == null ||
                        !content.Owns(state) || !ReferenceEquals(content.Find<IEntityStateDefinition>(state.Id), state))
                        throw new InvalidOperationException("worker mode must reference its actual owned state catalog");
            }
            else if (typeof(IStatefulWorker).IsAssignableFrom(definition.ComponentType))
                throw new InvalidOperationException("reusable worker requires IStatefulWorkerDefinition");
            return (rate, values.AsReadOnly());
        }
        private static (Fix Rate, IReadOnlyList<WorkerRoleId> Roles) Specification(World world, Entity entity)
        {
            var worker = entity?.Get<IWorker>(); var definition = entity?.Def.Component<IWorkerDefinition>();
            if (world == null || worker == null || definition == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity))
                throw new ArgumentException("worker specification requires the actual matching live worker and authored definition");
            return EntityObservation.ReadProvider(world, worker, entity, () =>
            {
                if (!ReferenceEquals(worker.Entity, entity) || !definition.ComponentType.IsInstanceOfType(worker))
                    throw new ArgumentException("worker specification requires the actual matching live worker and authored definition");
                var value = Metadata(world.Content, definition); var actual = worker.Roles;
                if (actual == null || actual.Count != value.Roles.Count || worker.Can(WorkerRoleId.None))
                    throw new InvalidOperationException("worker roles do not match authored specification");
                for (int i = 0; i < actual.Count; i++)
                    if (actual[i] != value.Roles[i] || !worker.Can(value.Roles[i])) throw new InvalidOperationException("worker lies about declared roles");
                return value;
            });
        }
        public static Fix BaseRate(World world, Entity entity) => Specification(world, entity).Rate;
        public static bool Can(World world, Entity entity, WorkerRoleId role)
        {
            var value = Specification(world, entity); bool expected = false;
            foreach (var declared in value.Roles) if (declared == role) { expected = true; break; }
            var worker = entity.Get<IWorker>();
            bool actual = EntityObservation.ReadProvider(world, worker, entity, () => worker.Can(role));
            if (actual != expected) throw new InvalidOperationException("worker eligibility does not match declared role");
            return actual;
        }
        public static WorkerExecutionView Read(World world, Entity entity)
        {
            var spec = Specification(world, entity);
            var value = ExecutionRateContracts.Read<IWorkerExecution, IWorkerExecutionDefinition>(world, entity, spec.Rate, provider => provider.ReadExecution(world));
            return new WorkerExecutionView(entity.Id, spec.Rate, spec.Roles, value);
        }
        internal static void ValidateDefinitions(World world)
        {
            foreach (EntityDef entity in world.Content.All<EntityDef>()) foreach (ComponentDef part in entity.Components)
            {
                bool runtime = typeof(IWorker).IsAssignableFrom(part.ComponentType);
                if (runtime != (part is IWorkerDefinition)) throw new ContentException(entity.Id + ": worker must declare matching IWorkerDefinition");
                if (runtime)
                {
                    try { Metadata(world.Content, (IWorkerDefinition)part); }
                    catch (InvalidOperationException error) { throw new ContentException(entity.Id + ": " + error.Message); }
                }
            }
            ExecutionRateContracts.ValidateDefinitions<IWorkerExecution, IWorkerExecutionDefinition, IWorker>(world);
        }
        internal static void ValidateLive(World world)
        { foreach (Entity entity in world.Entities.All) if (entity.Alive && entity.Get<IWorker>() != null) Read(world, entity); }
    }
    public sealed class WorkerExecutionDef : ControlledExecutionDef<WorkerExecution>, IWorkerExecutionDefinition
    {
        protected override Code MultiplierUnit => new Code("unit.work-rate");
        protected override bool HasTarget(EntityDef owner) => owner.HasCapability<IWorker>();
    }
    public sealed class WorkerExecution : ControlledExecution<WorkerExecutionDef>, IWorkerExecution { }
}
