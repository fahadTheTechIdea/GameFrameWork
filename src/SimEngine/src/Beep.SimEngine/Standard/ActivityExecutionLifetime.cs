using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Provenance on the original work order, never a worker reservation.</summary>
    public readonly struct ActivityExecutionBinding
    {
        public Code Source { get; }
        public Code Capability { get; }
        public long Execution { get; }
        public bool IsBound => !Source.IsNone;
        public ActivityExecutionBinding(Code source, Code capability, long execution)
        {
            ContractIdentity.ValidateId(source.Id, "activity lifetime source");
            ContractIdentity.ValidateId(capability.Id, "activity execution capability");
            if (execution <= 0) throw new ArgumentOutOfRangeException(nameof(execution));
            Source = source; Capability = capability; Execution = execution;
        }
        internal static ActivityExecutionBinding Sync(IStateSync sync, ActivityExecutionBinding value)
        {
            string source = value.Source.Id, capability = value.Capability.Id; long execution = value.Execution;
            sync.String(ref source); sync.String(ref capability); sync.Long(ref execution);
            if (source == null && capability == null && execution == 0) return default;
            try { return new ActivityExecutionBinding(new Code(source), new Code(capability), execution); }
            catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
        }
    }
    public interface IActivityExecutionLifetime : INamedEntityCapability
    {
        /// <summary>Pure structural/checkpoint validation, including retired execution provenance.</summary>
        bool ValidateExecution(World world, string plan, ActivityExecutionBinding binding);
        /// <summary>Pure demand check; false allows ordinary minimum-work completion and departure.</summary>
        bool RetainProductiveWork(World world, Order order, IActivityNodeDefinition node);
    }
    public readonly struct ActivityServiceOrder
    {
        public int Id { get; }
        public string Plan { get; }
        public bool Active { get; }
        internal ActivityServiceOrder(Order order) { Id = order.Id; Plan = order.PlanKey; Active = order.IsActive; }
    }
    public interface IActivityExecutionOrders
    {
        /// <summary>Post an admitted engine-side activity through the sole work owner; returns detached identity, never mutable order state.</summary>
        ActivityServiceOrder PostActivity(IActivityDefinition plan, PlayerId company, EntityId target, int priority,
            ActivityWorkerSelection workers = null);
        void CopyServiceOrders(PlayerId company, EntityId target, ActivityExecutionBinding binding, IList<ActivityServiceOrder> destination);
        Code PostServiceActivity(PlayerId company, EntityId target, string plan, int priority, ActivityExecutionBinding binding, out int order);
    }
    public sealed partial class WorkBoard : IActivityExecutionOrders
    {
        public ActivityServiceOrder PostActivity(IActivityDefinition plan, PlayerId company, EntityId target, int priority,
            ActivityWorkerSelection workers = null)
            => new ActivityServiceOrder(Post(plan, company, target, priority, workers));
        public void CopyServiceOrders(PlayerId company, EntityId target, ActivityExecutionBinding binding, IList<ActivityServiceOrder> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            foreach (var order in _orders)
                if (order.Owner == company && order.Target == target && order.ExecutionBinding.Equals(binding))
                    destination.Add(new ActivityServiceOrder(order));
        }
        public Code PostServiceActivity(PlayerId company, EntityId targetId, string planKey, int priority, ActivityExecutionBinding binding, out int identity)
        {
            identity = 0;
            Entity target = _world.Entities.Get(targetId);
            if (target?.Alive != true) return Codes.NotFound;
            if (target.Owner != company) return Codes.NotOwned;
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (priority < 0 || !binding.IsBound) return Codes.InvalidArgument;
            var source = target.Get<IActivityExecutionLifetime>(binding.Source);
            var definition = ResolvePlan(planKey) as IPlayerActivityDefinition;
            if (source == null || definition == null) return Codes.UnknownDef;
            bool active = EntityObservation.ReadProvider(_world, source, target, () => source.ValidateExecution(_world, planKey, binding));
            if (!active) return Codes.AlreadyDone;
            Code refusal = PlanObservation.Intent(definition, _world, company, target);
            if (!refusal.IsNone) return refusal;
            var execution = ResolveExecutionPlan(planKey, targetId);
            Order order = Post(execution, company, targetId, priority);
            order.ExecutionBinding = binding; identity = order.Id; return Code.None;
        }
        internal bool RetainProductiveWork(Order order, IActivityNodeDefinition node)
        {
            if (!order.ExecutionBinding.IsBound) return false;
            Entity target = _world.Entities.Get(order.Target);
            var source = target?.Get<IActivityExecutionLifetime>(order.ExecutionBinding.Source);
            if (source == null) return false;
            return EntityObservation.ReadProvider(_world, source, target, () => source.RetainProductiveWork(_world, order, node));
        }
        private void ValidateExecutionBinding(Order order)
        {
            if (!order.ExecutionBinding.IsBound) return;
            Entity target = _world.Entities.Get(order.Target);
            var source = target?.Get<IActivityExecutionLifetime>(order.ExecutionBinding.Source);
            if (source == null)
            {
                if (order.IsActive) throw new CorruptStateException("active work lost its declared lifetime owner");
                return;
            }
            EntityObservation.ReadProvider(_world, source, target, () => {
                source.ValidateExecution(_world, order.PlanKey, order.ExecutionBinding); return true;
            });
        }
    }
}
