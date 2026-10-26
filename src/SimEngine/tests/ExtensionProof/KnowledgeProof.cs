using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class StockKnowledgeSubjectDef : Def
    {
        private string _resource;
        public IResourceContentDefinition Resource { get; private set; }
        public ResourceId ResourceId { get; private set; }
        public KnowledgeRetention Retention { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _resource = reader.String("resource");
            Retention = KnowledgeRetentionPolicy.Read(reader);
        }
        protected override void ResolveCore(ContentResolver content)
        { Resource = content.Def<IResourceContentDefinition>(_resource); ResourceId = content.Resource(_resource); }
    }
    // Engine-side extension: no learned table, codec, scheduler or host-side discovery rule.
    public sealed class StockKnowledgeProvider : IKnowledgeProvider
    {
        public const string ProviderId = "proof.stock-knowledge", SubjectId = "source.reading";
        public StockKnowledgeProvider(World world) { World = world; }
        // Explicit transient protocol-fault instrumentation; not authored gameplay or saved state.
        public Action ObserveIdentity;
        public string Id { get { ObserveIdentity?.Invoke(); return ProviderId; } }
        public int SchemaVersion => 2;
        public World World { get; }
        public KnowledgeRetention Retention(string subject) => World.Content.Get<StockKnowledgeSubjectDef>(subject).Retention;
        public Code CheckObservation(string subject, JsonValue data)
        {
            StockKnowledgeSubjectDef definition = World.Content.Find<StockKnowledgeSubjectDef>(subject);
            if (definition == null) return Codes.UnknownDef;
            try
            {
                var reader = new JsonReader(data, Id);
                string resource = reader.String("resource"); long raw = reader.Long("quantity_raw"); reader.End();
                return resource != definition.Resource.Id || raw < 0 || UnitConversion.ValidateAmount(definition.Resource.QuantityUnit, Fix.FromRaw(raw)) != UnitConversionStatus.Converted
                    ? Codes.InvalidArgument : Code.None;
            }
            catch (ContentException) { return Codes.InvalidArgument; }
        }
        public void ValidateKnowledge(IReadOnlyList<KnowledgeObservation> observations) { }
    }

    public sealed class StockKnowledgeEffectDef : Def, IPhysicalWorkEffect
    {
        // The subject is an opaque observation result, not a consumed resource prerequisite.
        // Actual known consumables/tools are declared by the activity's requirement contracts.
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.AsReadOnly(new IContentDefinition[] { _resolved });
        private string _subject;
        private StockKnowledgeSubjectDef _resolved;
        protected override void ReadCore(JsonReader reader)
        {
            _subject = reader.String("subject");
        }
        protected override void ResolveCore(ContentResolver content)
        { _resolved = content.Def<StockKnowledgeSubjectDef>(_subject); }
        public void RequireCapabilities(World world)
        { if (!world.Services.Has<ICompanyKnowledge>()) throw new ContentException("stock observation needs knowledge owner"); }
        private JsonValue Observe(Entity target)
        {
            IInletPort inventory = target.Get<IInletPort>();
            if (inventory == null) return null;
            var data = JsonValue.NewObject(); data.Set("resource", JsonValue.Of(_resolved.Resource.Id));
            data.Set("quantity_raw", JsonValue.Number(inventory.Amount(_resolved.ResourceId).Raw.ToString(CultureInfo.InvariantCulture)));
            return data;
        }
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
        {
            JsonValue data = Observe(target); return data == null ? Codes.InvalidArgument
                : world.Services.Get<ICompanyKnowledge>().CheckRecord(order.Owner, StockKnowledgeProvider.ProviderId, _subject, data);
        }
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => world.Services.Get<ICompanyKnowledge>().Record(order.Owner, StockKnowledgeProvider.ProviderId, _subject, Observe(target));
    
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }

    public sealed class KnowledgeProofTechnologyModule : TechnologyModule
    {
        public override void DeclareContent(ContentTypes types)
        { base.DeclareContent(types); types.Section<StockKnowledgeSubjectDef>("stock_knowledge_subjects"); types.Section<StockKnowledgeEffectDef>("stock_knowledge_effects"); types.Section<PointReadingDef>("point_readings"); }
        public override void Compose(WorldComposer composer)
        { base.Compose(composer); composer.Services.Bind(new StockKnowledgeProvider(composer.World)); }
    }
}
