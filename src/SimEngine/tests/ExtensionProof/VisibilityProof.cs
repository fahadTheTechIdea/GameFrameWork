using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Public extension predicate reads the same historical observation, never current inventory.
    public sealed class StockVisibilityTargetDef : Def, IVisibilityTarget
    {
        private string _resource;
        private IResourceContentDefinition _target;
        private Fix _minimum;
        protected override void ReadCore(JsonReader reader)
        { _resource = reader.String("definition"); _minimum = reader.Fix("minimum"); if (_minimum < Fix.Zero) throw reader.Error("negative visibility minimum"); }
        protected override void ResolveCore(ContentResolver content) => _target = content.Def<IResourceContentDefinition>(_resource);
        public IReadOnlyList<IContentDefinition> Targets => KnowledgeVisibilityContracts.SingleTarget(_target);
        public void ValidateBinding(World world)
        {
            if (!world.Services.Has<ICompanyKnowledge>() || !world.Services.Has<StockKnowledgeProvider>() ||
                UnitConversion.ValidateAmount(_target.QuantityUnit, _minimum) != UnitConversionStatus.Converted)
                throw new ContentException("stock visibility needs actual typed knowledge provider/unit");
        }
        public bool Allows(World world, PlayerId company)
            => world.Services.Get<ICompanyKnowledge>().TryRead(company, company, StockKnowledgeProvider.ProviderId, StockKnowledgeProvider.SubjectId, out KnowledgeObservation observation)
                && new JsonReader(observation.Data, "stock.visibility").Long("quantity_raw") >= _minimum.Raw;
    }
    public sealed class StockKnowledgeVisibilityModule : KnowledgeVisibilityModule
    {
        public override void DeclareContent(ContentTypes types)
        { base.DeclareContent(types); types.Section<StockVisibilityTargetDef>("stock_knowledge_visibility"); }
    }
}
