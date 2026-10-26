using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent game-side rule selected by master-listed content, not a second wear scheduler.
    public sealed class ProofConditionWearDef : Def, IConditionWearRule
    {
        private string _unit;
        private IUnitDefinition _resolved;
        private Fix _quantity;
        private int _fault;
        public IUnitDefinition Unit => _resolved;
        protected override void ReadCore(JsonReader reader)
        {
            _unit = reader.String("unit"); _quantity = reader.Fix("quantity"); _fault = reader.Int("fault");
            if (!_quantity.IsPositive || _fault < 0 || _fault > 3) throw reader.Error("explicit positive rule quantity and fault mode required");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _resolved = content.Def<IUnitContentDefinition>(_unit);
            if (UnitConversion.ValidateAmount(Unit, _quantity) != UnitConversionStatus.Converted) throw content.Fail("invalid rule quantum");
        }
        public void ValidateBinding(World world, EntityDef owner) { }
        public ConditionWearDecision Observe(ConditionWearContext context)
        {
            if (_fault == 1) context.Entity.Touch();
            if (_fault == 2) return default;
            return new ConditionWearDecision(true, _fault == 3 ? context.Condition.Maximum + Fix.One
                : Fix.Min(_quantity, context.Condition.Value), Code.None);
        }
    }
}
