using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Facility capacity; actual activity progress and allocation belong to WorkBoard.</summary>
    public interface ICompanyActivityRateDefinition : INamedCapabilityDefinition
    {
        Code Pool { get; }
        Fix NominalRate { get; }
    }
    public interface ICompanyActivityRateSource : INamedEntityCapability
    {
        Code Pool { get; }
        ExecutionRateSnapshot ReadCapacity(World world);
    }
    public interface ICompanyActivityExecutor : IActivityExecutor
    {
        Code Pool { get; }
    }
    public interface ICompanyActivityRateGate : INamedEntityCapability
    {
        ExecutionGateSnapshot ReadGate(World world, ICompanyActivityRateSource source);
    }
    /// <summary>Reuse the original operation controls, signal gates, entity states and codec.</summary>
    public sealed class CompanyActivityRateDef : ControlledExecutionDef<CompanyActivityRateSource>, ICompanyActivityRateDefinition
    {
        public Code Pool { get; private set; }
        public Fix NominalRate { get; private set; }
        protected override Code MultiplierUnit => new Code("unit.work-rate");
        protected override bool HasTarget(EntityDef owner) => true;
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader);
            Pool = ActivityExecutorContracts.ReadCode(reader, "pool");
            NominalRate = reader.Fix("rate");
            if (!NominalRate.IsPositive) throw reader.Error("facility capacity rate must be positive");
        }
    }
    public sealed class CompanyActivityRateSource : ControlledExecution<CompanyActivityRateDef>, ICompanyActivityRateSource, IEntityStateSource, IOperation
    {
        public Code Pool => Def.Pool;
        public ExecutionRateSnapshot ReadCapacity(World world)
        {
            var execution = ReadExecution(world);
            var gate = execution.Gate;
            if (gate.Blocker.IsNone)
            {
                var safety = ExecutionGateContracts.Read<ICompanyActivityRateGate>(world, Entity, provider => provider.ReadGate(world, this));
                if (!safety.Blocker.IsNone) gate = new ExecutionGateSnapshot(CapabilityId, true, safety.Blocker, gate.Conditions);
            }
            return new ExecutionRateSnapshot(gate, Def.NominalRate * execution.Multiplier);
        }
        IEntityStateDefinition IEntityStateSource.GetState(EntityUpdateContext context)
            => ReadCapacity(context.World).Gate.Blocker.IsNone ? base.GetState(context) : Def.States["disabled"];
        OperationSnapshot IOperation.ReadOperation(World world)
        {
            var value = ReadCapacity(world);
            return !value.Gate.Blocker.IsNone ? new OperationSnapshot(OperationStatus.Disabled, value.Gate.Blocker)
                : new OperationSnapshot(value.Multiplier.IsPositive ? OperationStatus.Running : OperationStatus.Idle, Code.None);
        }
    }
    /// <summary>File-selected equal sharing of owned operating facilities, no alternate work scheduler.</summary>
    public sealed class CompanyActivityExecutorDef : Def, ICompanyActivityExecutor
    {
        public Code Pool { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Pool = ActivityExecutorContracts.ReadCode(reader, "pool");
            if (reader.String("allocation") != "equal") throw reader.Error("unknown company activity allocation");
        }
        public void ValidateDefinition(IActivityNodeDefinition definition) => ActivityExecutorContracts.ValidateRoles(definition, false);
        public void ValidateComposition(World world)
        {
            bool found = false;
            foreach (EntityDef entity in world.Content.All<EntityDef>()) foreach (ComponentDef part in entity.Components)
            {
                bool runtime = typeof(ICompanyActivityRateSource).IsAssignableFrom(part.ComponentType);
                if (runtime != (part is ICompanyActivityRateDefinition))
                    throw new ContentException(entity.Id + ": facility rate requires matching runtime and metadata contracts");
                if (part is ICompanyActivityRateDefinition definition)
                {
                    ContractIdentity.ValidateId(definition.Pool.Id, "facility rate pool");
                    if (!definition.NominalRate.IsPositive || definition.CapabilityId.IsNone ||
                        !ReferenceEquals(entity.Component<ICompanyActivityRateDefinition>(definition.CapabilityId), definition))
                        throw new ContentException(entity.Id + ": facility rate requires actual positive named metadata");
                    if (definition.Pool == Pool) found = true;
                }
            }
            if (!found) throw new ContentException(Id + ": pool has no selected facility rate source: " + Pool);
        }
        public IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context) => Array.Empty<Toil>();
        public bool MatchesItinerary(ActivityExecutorContext context, IReadOnlyList<Toil> itinerary) => itinerary.Count == 0;
        public Code CheckWorker(WorkRequirementContext context) => Code.None;
        public ActivityApproachResult Reapproach(ActivityExecutorContext context, int current) => throw new InvalidOperationException("company activity has no worker approach");
        public Fix ReadRate(ActivityExecutorContext context) => context.World.Services.Get<WorkBoard>().CompanyActivityRate(context.Order, context.Definition, Pool);
    }
    public sealed partial class WorkBoard
    {
        // Derived per-update observations only, never saved capacity, claims or another progress owner.
        private Dictionary<(int Order, string Node), Fix> _companyActivityRates;
        internal void BeginCompanyActivityRates()
        {
            if (_companyActivityRates != null) throw new InvalidOperationException("facility capacity update re-entered");
            var consumers = new Dictionary<(PlayerId Company, Code Pool), List<(int Order, string Node)>>();
            foreach (Order order in Orders)
            {
                if (!order.IsActive || order.State == OrderState.Completing || !PlanOrderBlocker(order.Id).IsNone) continue;
                Entity target = _world.Entities.Get(order.Target);
                if (target?.Alive != true || target.Owner != order.Owner) continue;
                foreach (IActivityNodeDefinition node in order.Plan.Graph.Nodes)
                {
                    var execution = order.Node(node.Id);
                    if (!(node.Executor is ICompanyActivityExecutor executor) || node.Duration.IsZero ||
                        execution.State == NodeState.Done || execution.State == NodeState.Completing || execution.State == NodeState.Failed ||
                        !WorkSystem.DependenciesDone(order, node)) continue;
                    var key = (order.Owner, executor.Pool);
                    if (!consumers.TryGetValue(key, out var members))
                        consumers.Add(key, members = new List<(int Order, string Node)>());
                    members.Add((order.Id, node.Id));
                }
            }
            var captured = new Dictionary<(int Order, string Node), Fix>();
            var pools = new List<(PlayerId Company, Code Pool)>(consumers.Keys);
            pools.Sort((a, b) => { int company = a.Company.Value.CompareTo(b.Company.Value); return company != 0 ? company : string.CompareOrdinal(a.Pool.Id, b.Pool.Id); });
            foreach (var key in pools)
            {
                var members = consumers[key];
                Fix share = ReadCompanyCapacity(key.Company, key.Pool) / Fix.FromInt(members.Count);
                foreach (var member in members) captured.Add(member, share);
            }
            _world.RequireHealthyUpdate(); _companyActivityRates = captured;
        }
        internal void EndCompanyActivityRates() => _companyActivityRates = null;
        private Fix ReadCompanyCapacity(PlayerId company, Code pool)
        {
            Fix capacity = Fix.Zero;
            foreach (Entity entity in _world.Entities.All)
            {
                if (!entity.Alive || entity.Owner != company) continue;
                ExecutionGateContracts.Read<ICompanyActivityRateSource>(_world, entity, source => {
                    var definition = entity.Def.Component<ICompanyActivityRateDefinition>(source.CapabilityId);
                    if (definition == null || definition.Pool != source.Pool || !definition.NominalRate.IsPositive)
                        throw new InvalidOperationException("facility capacity lost its actual metadata binding");
                    ExecutionRateSnapshot value = source.ReadCapacity(_world);
                    if (value.Multiplier < Fix.Zero || value.Gate == null || !value.Gate.Applies)
                        throw new InvalidOperationException("invalid facility capacity observation");
                    if (source.Pool == pool && value.Gate.Blocker.IsNone) capacity += value.Multiplier;
                    return value.Gate;
                });
            }
            return capacity;
        }
        internal Fix CompanyActivityRate(Order order, IActivityNodeDefinition definition, Code pool)
        {
            if (_companyActivityRates == null)
                throw new InvalidOperationException("facility activity rate requires the original work update observation");
            if (!(definition.Executor is ICompanyActivityExecutor executor) || executor.Pool != pool)
                throw new InvalidOperationException("facility activity lost its actual executor pool");
            return _companyActivityRates.TryGetValue((order.Id, definition.Id), out Fix rate) ? rate : Fix.Zero;
        }
    }
}
