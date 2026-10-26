using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IBusinessAccountDefinition : IContentDefinition
    {
        IUnitDefinition CurrencyUnit { get; }
        Fix OpeningCash { get; }
    }
    /// <summary>Master-listed initial parameters, independent of resource valuation and live balances.</summary>
    public sealed class BusinessAccountPolicyDef : Def, IBusinessAccountDefinition
    {
        private string _currency;
        private IUnitDefinition _currencyUnit;
        private Fix _opening;
        public IUnitDefinition CurrencyUnit => _currencyUnit;
        public Fix OpeningCash => _opening;
        protected override void ReadCore(JsonReader reader)
        {
            try { ContractIdentity.ValidateId(Id, "account definition"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            _currency = reader.String("currency"); _opening = reader.Fix("opening_cash");
            if (_opening < Fix.Zero) throw reader.Error("opening_cash cannot be negative");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _currencyUnit = content.Def<IUnitContentDefinition>(_currency);
            if (_currencyUnit.Dimension != "currency" || _currencyUnit.Discrete || !_currencyUnit.Unit.IsValid)
                throw content.Fail("accounts require an authored continuous currency unit");
        }
    }
}
