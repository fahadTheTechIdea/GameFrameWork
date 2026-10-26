using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Public provider proof: classifies actual historical rows, owns no ledger or gameplay scheduler.
    public sealed class ProofEnvironmentalMetricDef : Def, IEnvironmentalMetricDefinition
    {
        private EnvironmentalMetricDeclaration _policy;
        public IUnitContentDefinition Unit => _policy.Unit;
        public GameDuration? Window => _policy.Window;
        public EnvironmentalMeasure Measure => EnvironmentalMeasure.Flow;
        public DurationUnit? RateUnit => _policy.RateUnit;
        private string _resource;
        private Code _cause;
        private ResourceId _resolved;
        private ResourceFlowKind _kind;
        private int _fault;
        protected override void ReadCore(JsonReader reader)
        {
            _policy = EnvironmentalMetricDeclaration.Read(reader); _resource = reader.String("resource"); _cause = new Code(reader.String("cause"));
            string kind = reader.String("kind");
            _kind = kind == "lost" ? ResourceFlowKind.Lost : kind == "destroyed" ? ResourceFlowKind.Destroyed : throw reader.Error("explicit proof kind must be lost or destroyed");
            _fault = reader.Int("fault_mode"); if (_fault < 0 || _fault > 2) throw reader.Error("unknown proof fault mode");
        }
        protected override void ResolveCore(ContentResolver resolver)
        {
            _policy.Resolve(resolver); var resource = resolver.Def<IResourceContentDefinition>(_resource); _resolved = resource.Resource;
            if (resource.QuantityUnit.Unit != Unit.Unit) throw resolver.Fail("proof metric requires actual native resource unit");
        }
        public void ValidateBinding(World world)
        {
            if (!world.Services.Has<IResourceFlowJournal>() || Window.HasValue) throw new ContentException("proof metric requires actual cumulative journal");
        }
        public EnvironmentalObservation Observe(EnvironmentalContext context)
        {
            if (_fault == 1) return null;
            if (_fault == 2) foreach (Entity entity in context.World.Entities.All) { entity.Touch(); break; }
            BigInteger amount = 0; long count = 0;
            foreach (var row in context.World.Services.Get<IResourceFlowJournal>().ReadSummary(context.Company).Aggregates)
                if (row.Resource == _resolved && row.Kind == _kind && row.Cause == _cause) { amount += row.AmountRaw; count = checked(count + row.Facts); }
            return EnvironmentalObservation.Measured(context, amount, count);
        }
    }
}
