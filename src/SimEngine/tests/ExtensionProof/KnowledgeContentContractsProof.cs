using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // A registered content extension observes actual native stock; the original ledger owns history.
    public sealed class IndependentStockSubjectDef : Def, IKnowledgeSubjectDefinition
    {
        private string _resource;
        private IResourceContentDefinition _resolved;
        private ResourceId _quantity;
        public KnowledgeRetention Retention { get; private set; }
        public GameDuration? FreshFor { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { _resource = reader.String("resource"); Retention = KnowledgeRetentionPolicy.Read(reader);
            FreshFor = KnowledgeRetentionPolicy.ReadFreshness(reader, Retention); }
        protected override void ResolveCore(ContentResolver content)
        { _resolved = content.Def<IResourceContentDefinition>(_resource); _quantity = content.Resource(_resource); }
        public Code CheckTarget(World world, Entity target)
            => target == null || !target.Alive || !ReferenceEquals(world.Entities.Get(target.Id), target) ? Codes.NotFound
                : target.Get<IInletPort>() == null ? Codes.InvalidArgument : Code.None;
        public JsonValue Observe(World world, Entity target)
        {
            if (!CheckTarget(world, target).IsNone) throw new InvalidOperationException("actual stock observation target required");
            var value = JsonValue.NewObject(); value.Set("resource", JsonValue.Of(_resolved.Id));
            value.Set("quantity_raw", JsonValue.Number(target.Get<IInletPort>().Amount(_quantity).Raw.ToString(CultureInfo.InvariantCulture)));
            return value;
        }
        public Code CheckObservation(World world, JsonValue data)
        {
            try
            {
                var reader = new JsonReader(data, Id); string resource = reader.String("resource"); long raw = reader.Long("quantity_raw"); reader.End();
                return resource == _resolved.Id && raw >= 0 &&
                    UnitConversion.ValidateAmount(_resolved.QuantityUnit, Fix.FromRaw(raw)) == UnitConversionStatus.Converted ? Code.None : Codes.InvalidArgument;
            }
            catch (ContentException) { return Codes.InvalidArgument; }
        }
    }
    public sealed class IndependentKnowledgeSharingDef : Def, IKnowledgeSharingPolicy
    {
        private KnowledgeSharingDeclaration _declaration;
        public IReadOnlyList<KnowledgeSubjectKey> Subjects => _declaration.Subjects;
        protected override void ReadCore(JsonReader reader) => _declaration = KnowledgeSharingDeclaration.Read(reader);
        public bool Allows(World world, PlayerId sender, PlayerId recipient)
            => world.Content.Owns(this) && _declaration.Allows(sender, recipient);
        public void ValidateBinding(World world)
        { KnowledgeContentContracts.ValidateAlias(world, this); _declaration.ValidateBinding(world, Id); }
    }
    public sealed class IndependentKnowledgeForgettingDef : Def, IKnowledgeForgettingPolicy
    {
        private KnowledgeForgettingDeclaration _declaration;
        public IReadOnlyList<KnowledgeSubjectKey> Subjects => _declaration.Subjects;
        protected override void ReadCore(JsonReader reader) => _declaration = KnowledgeForgettingDeclaration.Read(reader);
        public bool Allows(World world, PlayerId company) => world.Content.Owns(this) && _declaration.Allows(company);
        public void ValidateBinding(World world)
        { KnowledgeContentContracts.ValidateAlias(world, this); _declaration.ValidateBinding(world, Id); }
    }
    public sealed class KnowledgeContentContractsModule : GameModule
    {
        public override string Id => "proof.knowledge-content";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(CompanyKnowledgeModule.ModuleId, Version) };
        public InitializeProbe Initialize { get; private set; }
        public bool ActualAliases { get; private set; }
        public bool SharedOwners { get; private set; }
        public bool IndependentPolicies { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentStockSubjectDef>("independent_stock_subjects");
            types.Section<IndependentKnowledgeSharingDef>("independent_sharing_policies");
            types.Section<IndependentKnowledgeForgettingDef>("independent_forgetting_policies");
        }
        public override void Compose(WorldComposer composer)
        {
            var subject = composer.Content.Get<IKnowledgeSubjectDefinition>("field.stock-reading");
            var sharing = composer.Content.Get<IKnowledgeSharingPolicy>("field.share");
            var forgetting = composer.Content.Get<IKnowledgeForgettingPolicy>("field.forget");
            ActualAliases = subject.GetType().BaseType == typeof(Def) && composer.Content.Owns(subject) &&
                ReferenceEquals(subject, composer.Content.Get<KnowledgeEffectDef>("field.stock-learn").Subject) &&
                ReferenceEquals(sharing, composer.Content.Get<IKnowledgeSubjectPolicy>(sharing.Id)) &&
                ReferenceEquals(forgetting, composer.Content.Get<IKnowledgeSubjectPolicy>(forgetting.Id));
            var owner = composer.Services.Get<ICompanyKnowledge>();
            SharedOwners = ReferenceEquals(owner, composer.Services.Get<IKnowledgeFreshness>()) &&
                ReferenceEquals(owner, composer.Services.Get<IKnowledgeSharing>()) &&
                ReferenceEquals(owner, composer.Services.Get<IKnowledgeForgetting>());
            IndependentPolicies = sharing is IndependentKnowledgeSharingDef && forgetting is IndependentKnowledgeForgettingDef;
            Initialize = new InitializeProbe("proof.knowledge-content-init"); composer.AddSystem(Initialize, 12352);
        }
    }
}
