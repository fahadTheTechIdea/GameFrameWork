using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Optional synchronous participant in the existing resource transaction, never another ledger or event consumer.
    public sealed partial class ResourceTransactions
    {
        private static readonly Code GenerationRange = new Code("resources.generation-range");
        private Code PrepareGeneration(ReservationOwner owner, PlayerId company, Code cause, Bill consumed, Bill produced,
            bool committing, out IResourceProductionCommit commit)
        {
            commit = null;
            if (produced.IsEmpty || !_world.Services.TryGet<IResourceProductionSettlement>(out var settlement)) return Code.None;
            var context = new ResourceGenerationContext(owner, company, cause, consumed, produced);
            var prepared = EntityObservation.ReadProvider(_world, settlement, null, () => {
                IResourceProductionCommit candidate = null;
                Code refusal = committing ? settlement.PrepareGeneration(context, out candidate) : settlement.CheckGeneration(context);
                if (!refusal.IsNone && candidate != null) throw new InvalidOperationException("refused generation returned a payable commit");
                return (refusal, candidate);
            }, space: _world.Space, observeTargetState: true);
            commit = prepared.Item2;
            return prepared.Item1;
        }
        private Code PrepareConversionGeneration(ResourceConversionIntent intent, bool committing, out IResourceProductionCommit commit)
        {
            commit = null;
            if (intent.Outputs.Count == 0 || !_world.Services.Has<IResourceProductionSettlement>()) return Code.None;
            Bill consumed, produced;
            try
            {
                var inputs = new List<ResourceAmount>();
                foreach (var input in intent.Inputs) if (input.Consumed.IsPositive)
                    inputs.Add(new ResourceAmount(input.Receipt.Resource, input.Consumed));
                var outputs = new List<ResourceAmount>();
                foreach (ResourceProductionIntent output in intent.Outputs) outputs.AddRange(output.Quantities.Lines);
                consumed = new Bill(inputs); produced = new Bill(outputs);
            }
            catch (OverflowException) { return GenerationRange; }
            return PrepareGeneration(intent.Owner, intent.Company, intent.Cause, consumed, produced, committing, out commit);
        }
    }
}
