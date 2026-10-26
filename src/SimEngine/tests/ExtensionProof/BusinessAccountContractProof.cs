using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent content metadata only. No financial state, services or gameplay.
    public sealed class IndependentBusinessAccountDef : Def, IBusinessAccountDefinition
    {
        private string _currency;
        public IUnitDefinition CurrencyUnit { get; private set; }
        public Fix OpeningCash { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _currency = reader.String("currency");
            OpeningCash = reader.Fix("opening_cash");
        }
        protected override void ResolveCore(ContentResolver content)
            => CurrencyUnit = content.Def<IUnitContentDefinition>(_currency);
    }
}
