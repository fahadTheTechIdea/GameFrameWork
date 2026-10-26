using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent engine-side semantic provider, not a test-owned learned table or scheduler.
    public sealed class RetentionSubjectDef : Def
    {
        private string[] _requires;
        public IReadOnlyList<RetentionSubjectDef> Requirements { get; private set; }
        private string _value;
        public JsonValue Value => Json.Parse(_value);
        protected override void ReadCore(JsonReader reader)
        {
            _requires = reader.Strings("requires");
            var text = new StringBuilder();
            (reader.Child("value") ?? throw reader.Error("explicit value required")).Raw.WriteCanonical(text); _value = text.ToString();
        }
        protected override void ResolveCore(ContentResolver resolver)
        { var refs = new List<RetentionSubjectDef>(); foreach (string id in _requires) refs.Add(resolver.Def<RetentionSubjectDef>(id)); Requirements = refs.AsReadOnly(); _requires = null; }
    }
    public sealed class RetentionKnowledgeProvider : IKnowledgeForgettingProvider
    {
        public const string ProviderId = "proof.retention";
        public static readonly Code Required = new Code("proof.knowledge-still-required");
        public World World { get; }
        public RetentionKnowledgeProvider(World world) { World = world; }
        public string Id => ProviderId;
        public int SchemaVersion => 1;
        public KnowledgeRetention Retention(string subject) => KnowledgeRetention.FirstLearned;
        public Code CheckObservation(string subject, JsonValue data)
        {
            var def = World.Content.Find<RetentionSubjectDef>(subject); if (def == null) return Codes.UnknownDef;
            var expected = new StringBuilder(); var actual = new StringBuilder(); def.Value.WriteCanonical(expected); data.WriteCanonical(actual);
            return expected.ToString() == actual.ToString() ? Code.None : Codes.InvalidArgument;
        }
        public Code CheckForgettingSubject(string subject) => World.Content.Find<RetentionSubjectDef>(subject) == null ? Codes.UnknownDef : Code.None;
        private Code CheckRemaining(PlayerId company, IReadOnlyList<KnowledgeObservation> retained)
        {
            foreach (var fact in retained) if (fact.Company == company)
                foreach (var required in World.Content.Get<RetentionSubjectDef>(fact.Subject).Requirements)
                {
                    bool found = false; foreach (var other in retained) if (other.Company == company && other.Subject == required.Id) { found = true; break; }
                    if (!found) return Required;
                }
            return Code.None;
        }
        public Code CheckForget(PlayerId company, IReadOnlyList<KnowledgeObservation> removed, IReadOnlyList<KnowledgeObservation> retained)
            => CheckRemaining(company, retained);
        public void ValidateKnowledge(IReadOnlyList<KnowledgeObservation> observations)
        {
            foreach (var fact in observations) if (!CheckRemaining(fact.Company, observations).IsNone) throw new InvalidOperationException("dependent retained knowledge is missing");
        }
    }
    public sealed class RetentionObservationDef : Def, IPhysicalWorkEffect
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.AsReadOnly(new IContentDefinition[] { _resolved });
        private string _subject; private RetentionSubjectDef _resolved;
        protected override void ReadCore(JsonReader reader) => _subject = reader.String("subject");
        protected override void ResolveCore(ContentResolver resolver) => _resolved = resolver.Def<RetentionSubjectDef>(_subject);
        public void RequireCapabilities(World world)
        { if (!world.Services.Has<ICompanyKnowledge>()) throw new ContentException("retention needs original ledger"); }
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => world.Services.Get<ICompanyKnowledge>().CheckRecord(order.Owner, RetentionKnowledgeProvider.ProviderId, _resolved.Id, _resolved.Value);
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => world.Services.Get<ICompanyKnowledge>().Record(order.Owner, RetentionKnowledgeProvider.ProviderId, _resolved.Id, _resolved.Value);
    
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }
    public sealed class KnowledgeForgettingProofModule : GameModule
    {
        public override string Id => "proof.retention";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(CompanyKnowledgeModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        { types.Section<RetentionSubjectDef>("retention_subjects"); types.Section<RetentionObservationDef>("retention_observations"); }
        public override void Compose(WorldComposer composer) => composer.Services.Bind(new RetentionKnowledgeProvider(composer.World));
    }
}
