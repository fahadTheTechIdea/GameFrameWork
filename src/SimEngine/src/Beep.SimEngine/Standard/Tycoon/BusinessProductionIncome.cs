using System;
using System.Numerics;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public static class ProductionIncomeCodes
    {
        public static readonly Code Invalid = new Code("business.invalid-generation");
        public static readonly Code Range = new Code("business.income-range");
        public static readonly Code Quote = new Code("business.income-quote");
    }
    // The same account instance/codec owns settlement identity and historical earnings, not another ledger.
    internal sealed partial class BusinessAccounts : IResourceProductionSettlement
    {
        private World _world;
        private IResourceValuation _valuation;
        private IProductionIncomePolicy _income;
        private long _generationSequence;
        private readonly long[] _generationCounts;
        private readonly BigInteger[] _generationEarnings;
        internal void ConfigureIncome(World world, IResourceValuation valuation, IProductionIncomePolicy income)
        {
            if (_income != null || world == null || valuation == null || income == null || string.IsNullOrWhiteSpace(income.Id) || valuation.Currency != Currency)
                throw new InvalidOperationException("account income policy must be configured exactly once with an identity");
            _world = world; _valuation = valuation; _income = income;
        }
        public Code CheckGeneration(ResourceGenerationContext context) => PrepareGeneration(context, out _);
        public Code PrepareGeneration(ResourceGenerationContext context, out IResourceProductionCommit commit)
        {
            commit = null;
            if (_income == null || !context.Owner.IsValid || context.Cause.IsNone ||
                context.Consumed == null || context.Produced == null) return ProductionIncomeCodes.Invalid;
            // Neutral physical generation has no recipient account, not a fabricated player balance.
            if (context.Company == PlayerId.None) return Code.None;
            if (!IsAccount(context.Company)) return ProductionIncomeCodes.Invalid;
            int seat = context.Company.Value; Fix before = _balances[seat]; long sequence = _generationSequence;
            ProductionIncomeDecision decision = _income.Evaluate(context);
            Unchanged();
            if (!decision.Refusal.IsNone) return decision.Refusal;
            if (decision.Eligible == null) throw new InvalidOperationException("income policy returned no explicit eligibility bill");
            Fix payout = Fix.Zero;
            try
            {
                foreach (ResourceAmount line in decision.Eligible.Lines)
                {
                    if (!line.Amount.IsPositive || line.Amount > context.Produced.AmountOf(line.Resource))
                        throw new InvalidOperationException("income policy rewarded material outside actual generation");
                    ResourceValueQuote quote = _valuation.Quote(line.Resource, line.Amount); Unchanged();
                    if (quote.Status != ResourceValueStatus.Ready) return ProductionIncomeCodes.Quote;
                    if (quote.Policy != _valuation.Policy || quote.Mode != _valuation.Mode || quote.Currency != Currency || quote.Resource != line.Resource ||
                        quote.Quantity != line.Amount || quote.QuantityUnit != _world.Content.Resource(line.Resource)?.QuantityUnit.Unit ||
                        quote.Value < Fix.Zero || quote.UnitValue < Fix.Zero)
                        throw new InvalidOperationException("generation quote violated selected resource/policy/currency identity");
                    payout += quote.Value;
                }
                if (payout == Fix.Zero) return Code.None;
                Fix after = before + payout;
                long next = checked(sequence + 1), count = checked(_generationCounts[seat] + 1);
                commit = new GenerationCommit(this, seat, before, after, sequence, next, count, _generationEarnings[seat] + payout.Raw);
                return Code.None;
            }
            catch (OverflowException) { return ProductionIncomeCodes.Range; }

            void Unchanged()
            {
                if (_generationSequence != sequence || _balances[seat] != before)
                    throw new InvalidOperationException("income policy/valuation mutated pure account admission");
            }
        }
        private sealed class GenerationCommit : IResourceProductionCommit
        {
            private readonly BusinessAccounts _owner;
            private readonly int _seat;
            private readonly Fix _before, _after;
            private readonly long _previous, _next, _count;
            private readonly BigInteger _earned;
            private bool _committed;
            internal GenerationCommit(BusinessAccounts owner, int seat, Fix before, Fix after, long previous,
                long next, long count, BigInteger earned)
            { _owner = owner; _seat = seat; _before = before; _after = after; _previous = previous; _next = next; _count = count; _earned = earned; }
            public void Commit()
            {
                if (_committed || _owner._generationSequence != _previous || _owner._balances[_seat] != _before)
                    throw new InvalidOperationException("production settlement is duplicate or no longer matches synchronous admission");
                _owner._balances[_seat] = _after; _owner._generationSequence = _next;
                _owner._generationCounts[_seat] = _count; _owner._generationEarnings[_seat] = _earned; _committed = true;
            }
        }
    }
}
