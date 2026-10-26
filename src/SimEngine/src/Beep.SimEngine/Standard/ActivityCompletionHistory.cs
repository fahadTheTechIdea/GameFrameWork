using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Compact earned history, never an executing order, stock ledger or progress counter.</summary>
    public readonly struct ActivityCompletionFact
    {
        public PlayerId Company { get; }
        public EntityId Target { get; }
        public string Plan { get; }
        public int Order { get; }
        public long CreatedTick { get; }
        public long CompletedTick { get; }
        public ActivityExecutionBinding ExecutionBinding { get; }
        internal ActivityCompletionFact(PlayerId company, EntityId target, string plan, int order, long created, long completed, ActivityExecutionBinding binding)
        { Company = company; Target = target; Plan = plan; Order = order; CreatedTick = created; CompletedTick = completed; ExecutionBinding = binding; }
        internal ActivityCompletionFact(global::Beep.SimEngine.Standard.Order order)
            : this(order.Owner, order.Target, order.PlanKey, order.Id, order.CreatedTick, order.CompletedTick, order.ExecutionBinding) { }
    }
    public interface IActivityCompletionHistory
    {
        bool IsActivity(string plan);
        /// <summary>Exact company; None target explicitly queries company-wide latest completion.</summary>
        bool TryReadCompletion(PlayerId company, string plan, EntityId target, out ActivityCompletionFact completion);
        bool TryReadServiceCompletion(PlayerId company, string plan, EntityId target, Code source, Code capability, out ActivityCompletionFact completion);
    }
    public sealed partial class WorkBoard : IActivityCompletionHistory
    {
        private readonly Dictionary<(PlayerId, EntityId, string, Code, Code), ActivityCompletionFact> _completedActivities
            = new Dictionary<(PlayerId, EntityId, string, Code, Code), ActivityCompletionFact>();
        public bool IsActivity(string plan)
        {
            _world.RequireHealthyUpdate();
            return ResolvePlan(plan)?.PlanKey == plan && !string.IsNullOrEmpty(plan);
        }
        private static bool Later(ActivityCompletionFact next, ActivityCompletionFact previous)
            => next.CompletedTick > previous.CompletedTick || next.CompletedTick == previous.CompletedTick && next.Order > previous.Order;
        public bool TryReadCompletion(PlayerId company, string plan, EntityId target, out ActivityCompletionFact completion)
        {
            completion = default;
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company) || target.Value < 0 || !IsActivity(plan)) return false;
            bool found = false; ActivityCompletionFact latest = default;
            foreach (var fact in _completedActivities.Values) Consider(fact);
            foreach (Order order in _orders) if (order.State == OrderState.Done) Consider(new ActivityCompletionFact(order));
            completion = latest; return found;
            void Consider(ActivityCompletionFact fact)
            {
                if (fact.Company != company || fact.Plan != plan || target.IsValid && fact.Target != target) return;
                if (!found || Later(fact, latest)) { latest = fact; found = true; }
            }
        }
        public bool TryReadServiceCompletion(PlayerId company, string plan, EntityId target, Code source, Code capability, out ActivityCompletionFact completion)
        {
            completion = default;
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company) || !target.IsValid || source.IsNone || capability.IsNone || !IsActivity(plan)) return false;
            bool found = false; ActivityCompletionFact latest = default;
            foreach (var fact in _completedActivities.Values) Consider(fact);
            foreach (Order order in _orders) if (order.State == OrderState.Done) Consider(new ActivityCompletionFact(order));
            completion = latest; return found;
            void Consider(ActivityCompletionFact fact)
            {
                if (fact.Company != company || fact.Plan != plan || fact.Target != target || fact.ExecutionBinding.Source != source || fact.ExecutionBinding.Capability != capability) return;
                if (!found || Later(fact, latest)) { latest = fact; found = true; }
            }
        }
        private void CompactCompletion(Order order)
        {
            if (order.State != OrderState.Done) return;
            ValidateCompletion(order);
            var fact = new ActivityCompletionFact(order); var key = (fact.Company, fact.Target, fact.Plan, fact.ExecutionBinding.Source, fact.ExecutionBinding.Capability);
            if (!_completedActivities.TryGetValue(key, out var previous) || Later(fact, previous)) _completedActivities[key] = fact;
        }
        private void ValidateCompletionTime(Order order)
        {
            if (order.State == OrderState.Done ? order.CompletedTick < order.CreatedTick || order.CompletedTick > _world.Tick
                : order.CompletedTick != -1) throw new CorruptStateException("order completion time differs from actual terminal state");
        }
        private void SyncActivityCompletions(IStateSync sync)
        {
            var facts = new List<ActivityCompletionFact>(_completedActivities.Values);
            facts.Sort((a, b) => { int c = a.Company.Value.CompareTo(b.Company.Value); if (c != 0) return c;
                c = a.Target.Value.CompareTo(b.Target.Value); if (c != 0) return c;
                c = string.CompareOrdinal(a.Plan, b.Plan); if (c != 0) return c;
                c = string.CompareOrdinal(a.ExecutionBinding.Source.Id, b.ExecutionBinding.Source.Id);
                return c != 0 ? c : string.CompareOrdinal(a.ExecutionBinding.Capability.Id, b.ExecutionBinding.Capability.Id); });
            int count = sync.Count(facts.Count);
            var decoded = sync.Reading ? new Dictionary<(PlayerId, EntityId, string, Code, Code), ActivityCompletionFact>() : null;
            var executions = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                ActivityCompletionFact fact = sync.Reading ? default : facts[i];
                PlayerId company = fact.Company; EntityId target = fact.Target; string plan = fact.Plan;
                int order = fact.Order; long created = fact.CreatedTick, completed = fact.CompletedTick;
                sync.Player(ref company); sync.Entity(ref target); sync.String(ref plan); sync.Int(ref order);
                sync.Long(ref created); sync.Long(ref completed);
                ActivityExecutionBinding binding = ActivityExecutionBinding.Sync(sync, fact.ExecutionBinding);
                if (!_world.Players.IsSeat(company) || !target.IsValid || target.Value >= _world.Entities.NextIdentity.Value ||
                    !IsActivity(plan) || order <= 0 || order >= _nextOrder || Order(order) != null || !executions.Add(order) ||
                    created < 0 || completed < created || completed > _world.Tick)
                    throw new CorruptStateException("invalid compact activity completion identity/time");
                if (sync.Reading && !decoded.TryAdd((company, target, plan, binding.Source, binding.Capability), new ActivityCompletionFact(company, target, plan, order, created, completed, binding)))
                    throw new CorruptStateException("duplicate compact activity completion key");
            }
            if (sync.Reading) { _completedActivities.Clear(); foreach (var item in decoded) _completedActivities.Add(item.Key, item.Value); }
        }
    }
}
