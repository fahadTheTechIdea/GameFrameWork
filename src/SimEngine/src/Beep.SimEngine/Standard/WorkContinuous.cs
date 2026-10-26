using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Standard
{
    // The same WorkBoard owner; progress, stock, promises and flow facts keep their existing owners.
    public sealed partial class WorkBoard
    {
        private static bool HasRunningResourceRequirement(IWorkRequirement root)
            => HasRequirement<IResourceWorkRequirement>(root, resource => WorkResourceUses.Running(resource.Use));
        internal static Bill WorkConsumed(Bill total, WorkProgress progress, long required, IResourceCatalog content)
        {
            if (required <= 0) throw new ArgumentOutOfRangeException(nameof(required));
            if (progress.Complete(required)) return total;
            BigInteger work = ((BigInteger)progress.WholeTicks << Fix.FractionalBits) + progress.Fraction;
            BigInteger duration = (BigInteger)required << Fix.FractionalBits;
            var amounts = new List<ResourceAmount>();
            foreach (var line in total.Lines)
            {
                long quantum = content.Get(line.Resource).QuantityUnit.Discrete ? Fix.One.Raw : 1;
                BigInteger divisor = duration * quantum;
                BigInteger raw = ((BigInteger)line.Amount.Raw * work + divisor - 1) / divisor * quantum;
                if (raw > 0) amounts.Add(new ResourceAmount(line.Resource, Fix.FromRaw(checked((long)raw))));
            }
            return new Bill(amounts);
        }
        internal static Bill ElapsedConsumed(IReadOnlyList<IResourceRate> rates, WorkProgress elapsed, ISimulationTime time, IResourceCatalog content)
        {
            var amounts = new List<ResourceAmount>();
            foreach (var rate in rates)
            {
                long quantum = content.Get(rate.Resource.Resource).QuantityUnit.Discrete ? Fix.One.Raw : 1;
                BigInteger raw = QuantizedGameRates.AccumulatedRaw(rate.Quantity, rate.PeriodNumerator, rate.PeriodDenominator,
                    rate.PeriodUnit, time, elapsed, quantum, true);
                if (raw > 0) amounts.Add(new ResourceAmount(rate.Resource.Resource, Fix.FromRaw(checked((long)raw))));
            }
            return new Bill(amounts);
        }
        private Bill RunningDelta(WorkResourceChoice choice, WorkProgress before, WorkProgress after, long required,
            long productiveBefore, long productiveAfter)
        {
            bool bounded = choice.Use == WorkResourceUse.Continuous;
            Bill spent = bounded ? WorkConsumed(choice.Quantities, before, required, _world.Content)
                : ElapsedConsumed(choice.Rates, new WorkProgress(productiveBefore), _world.Time, _world.Content);
            Bill next = bounded ? WorkConsumed(choice.Quantities, after, required, _world.Content)
                : ElapsedConsumed(choice.Rates, new WorkProgress(productiveAfter), _world.Time, _world.Content);
            return PositiveDifference(next, spent);
        }
        private static Bill PositiveDifference(Bill after, Bill before)
        {
            var delta = new List<ResourceAmount>();
            foreach (var line in after.Lines)
            {
                Fix previous = before.AmountOf(line.Resource);
                if (line.Amount > previous) delta.Add(new ResourceAmount(line.Resource, line.Amount - previous));
            }
            return new Bill(delta);
        }
        private Bill ResourceDemand(WorkResourceChoice choice, WorkRequirementContext context)
            => WorkResourceUses.Running(choice.Use)
                ? RunningDelta(choice, context.Progress, context.NextProgress, _world.Time.Ticks(context.Definition.Duration), context.ProductiveSteps, context.NextProductiveSteps)
                : choice.Quantities;
        internal IReadOnlyList<WorkResourceUsage> RunningUsage(ActivityNodeExecution node, IActivityNodeDefinition definition)
        {
            var budgets = new List<WorkResourceUsage>();
            foreach (var occurrence in SelectedOccurrences(definition.Requirement, node))
                if (occurrence.Requirement is IResourceWorkRequirement resource && WorkResourceUses.Running(resource.Use))
                {
                    Bill total = ScaledBill(resource, definition.RequirementMultiplier, _world.Content);
                    bool bounded = resource.Use == WorkResourceUse.Continuous;
                    IReadOnlyList<IResourceRate> rates = ScaledRates(resource, total);
                    Bill consumed = bounded ? WorkConsumed(total, node.Progress, _world.Time.Ticks(definition.Duration), _world.Content)
                        : ElapsedConsumed(rates, new WorkProgress(node.ProductiveSteps), _world.Time, _world.Content);
                    budgets.Add(new WorkResourceUsage(occurrence.Path, resource.Id, Binding(node, occurrence.Path)?.Choice.Endpoint ?? default,
                        resource.Use, total, rates, consumed, bounded ? PositiveDifference(total, consumed) : null));
                }
            return budgets.AsReadOnly();
        }
    }
}
