using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentVisibilityPolicyDef : Def, IKnowledgeVisibilityDefinition
    {
        private KnowledgeVisibilityDeclaration _declaration;
        public bool OwnEntities => _declaration.OwnEntities;
        public bool UnlistedEntities => _declaration.UnlistedEntities;
        public bool UnlistedContent => _declaration.UnlistedContent;
        public IReadOnlyList<IVisibilityTarget> Rules => _declaration.Rules;
        protected override void ReadCore(JsonReader reader) => _declaration = KnowledgeVisibilityDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class IndependentEntityVisibilityDef : Def, IVisibilityEntityTarget
    {
        private KnowledgeVisibilityTargetDeclaration<EntityDef> _declaration;
        protected override void ReadCore(JsonReader reader) => _declaration = KnowledgeVisibilityTargetDeclaration<EntityDef>.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public IReadOnlyList<IContentDefinition> Targets => _declaration.Targets;
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world, Id);
        public bool Allows(World world, PlayerId company) => _declaration.Allows(world, company);
        public bool AllowsEntity(World world, PlayerId company, Entity entity)
        {
            world.Services.Get<VisibilityContentContractsModule>().Probe(world, VisibilityProbeStage.Entity, company, _declaration.Target, entity);
            return ReferenceEquals(world.Entities.Get(entity.Id), entity) && ReferenceEquals(entity.Def, _declaration.Target)
                && entity.Get<ISpatialPresence>()?.Space == _declaration.Target.Component<ISpatialPresenceDefinition>()?.Space;
        }
    }
    public sealed class IndependentActivityVisibilityDef : Def, IVisibilityTarget
    {
        private KnowledgeVisibilityTargetDeclaration<IAuthoredActivityDefinition> _declaration;
        protected override void ReadCore(JsonReader reader) => _declaration = KnowledgeVisibilityTargetDeclaration<IAuthoredActivityDefinition>.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public IReadOnlyList<IContentDefinition> Targets => _declaration.Targets;
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world, Id);
        public bool Allows(World world, PlayerId company)
        {
            world.Services.Get<VisibilityContentContractsModule>().Probe(world, VisibilityProbeStage.Allow, company, _declaration.Target);
            return _declaration.Allows(world, company);
        }
    }
    public sealed class IndependentLayerVisibilityDef : Def, IVisibilitySampleTarget
    {
        private KnowledgeVisibilityTargetDeclaration<ISpatialLayerDefinition> _declaration;
        private Fix _start, _end;
        private string _subject;
        protected override void ReadCore(JsonReader reader)
        {
            _start = reader.Fix("start"); _end = reader.Fix("end"); _subject = reader.String("key_subject");
            if (_start < Fix.Zero || _end < _start) throw reader.Error("explicit ordered point visibility range required");
            _declaration = KnowledgeVisibilityTargetDeclaration<ISpatialLayerDefinition>.Read(reader);
        }
        protected override void ResolveCore(ContentResolver content)
        { _declaration.Resolve(content); content.Def<IKnowledgeSubjectDefinition>(_subject); }
        public IReadOnlyList<IContentDefinition> Targets => _declaration.Targets;
        public void ValidateBinding(World world)
        {
            _declaration.ValidateBinding(world, Id);
            if (!(_declaration.Target.Space is PointSpaceDef space) || world.Space?.Id != space.Id || _end > space.Extent)
                throw new ContentException(Id + ": exact selected point-space scope required");
        }
        public bool Allows(World world, PlayerId company) => _declaration.Allows(world, company);
        public bool AllowsSample(World world, PlayerId company, SpatialSnapshot sample)
        {
            world.Services.Get<VisibilityContentContractsModule>().Probe(world, VisibilityProbeStage.Sample, company, _declaration.Target);
            return sample is PointSnapshot point && point.Space == _declaration.Target.Space.Id && point.Position >= _start && point.Position <= _end;
        }
        public void CopyObservationKeys(World world, PlayerId company, IList<string> destination)
        {
            world.Services.Get<VisibilityContentContractsModule>().Probe(world, VisibilityProbeStage.Keys, company, _declaration.Target);
            destination.Clear();
            if (world.Services.Get<ICompanyKnowledge>().TryRead(company, company, "sim.knowledge.facts", _subject, out var fact))
                destination.Add(_subject + ":" + fact.ObservedTick.ToString(CultureInfo.InvariantCulture));
        }
    }
    // Independent authored multi-target rule. Counters/hooks are transient diagnostics, never visibility state.
    public sealed class DeclaredVisibilityTargetsDef : Def, IVisibilityTarget
    {
        private string[] _ids; private string _invalid; private bool _allowed;
        private IReadOnlyList<IContentDefinition> _targets;
        public int AllowCalls;
        public Action TargetsProbe;
        public IReadOnlyList<IContentDefinition> Targets { get { TargetsProbe?.Invoke(); return _targets; } }
        protected override void ReadCore(JsonReader reader)
        {
            if (!reader.Has("targets") || !reader.Has("invalid")) throw reader.Error("explicit targets and diagnostic selection required");
            _ids = reader.Strings("targets"); _invalid = reader.NullableString("invalid"); _allowed = reader.Bool("allowed");
            if (_invalid != null && _invalid != "null" && _invalid != "null-item" && _invalid != "foreign") throw reader.Error("unknown invalid target selection");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            var targets = new List<IContentDefinition>(); foreach (string id in _ids) targets.Add(content.Def<EntityDef>(id));
            if (_invalid == "null-item") targets.Add(null);
            if (_invalid == "foreign") targets.Add(new EntityDef());
            _targets = _invalid == "null" ? null : targets.AsReadOnly();
        }
        public bool Allows(World world, PlayerId company) { AllowCalls++; return _allowed && world.Players.IsSeat(company); }
        public void ValidateBinding(World world) { }
    }
    // Intentionally invalid metadata. It is never an accepted runtime policy or second state owner.
    public sealed class MissingVisibilityRulesDef : Def, IKnowledgeVisibilityDefinition
    {
        public bool OwnEntities => false;
        public bool UnlistedEntities => false;
        public bool UnlistedContent => false;
        public IReadOnlyList<IVisibilityTarget> Rules => null;
        protected override void ReadCore(JsonReader reader) { }
    }
    public enum VisibilityProbeStage { Allow, Entity, Sample, Keys }
    public enum VisibilityProbeFault { Pause, Throw, Recursive, SwallowedRecursive, PrivateEntity }
    // Transient proof diagnostics only. Original visibility/knowledge and queried entity retain state.
    public sealed class VisibilityContentContractsModule : GameModule
    {
        public override string Id => "proof.visibility-content";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(KnowledgeVisibilityModule.ModuleId, Version),
            new ModuleRequirement("proof.knowledge-content", Version), new ModuleRequirement(WorkModule.ModuleId, Version) };
        public InitializeProbe Initialize { get; private set; }
        public bool ActualAliases { get; private set; }
        public bool SharedOwner { get; private set; }
        public bool Independent { get; private set; }
        private VisibilityProbeStage? _stage;
        private VisibilityProbeFault _fault;
        public int Calls { get; private set; }
        public bool MutationCompleted { get; private set; }
        public EntityId ActualTarget { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentVisibilityPolicyDef>("independent_visibility_policies");
            types.Section<IndependentEntityVisibilityDef>("independent_entity_visibility");
            types.Section<IndependentActivityVisibilityDef>("independent_activity_visibility");
            types.Section<IndependentLayerVisibilityDef>("independent_layer_visibility");
            types.Section<MissingVisibilityRulesDef>("invalid_visibility_policies");
            types.Section<DeclaredVisibilityTargetsDef>("declared_visibility_targets");
            types.Component<CompletionProbeDef>("visibility_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind(this);
            var policy = composer.Content.Get<IKnowledgeVisibilityDefinition>("field.visibility");
            ActualAliases = composer.Content.Owns(policy);
            foreach (var rule in policy.Rules)
                ActualAliases &= composer.Content.Owns(rule) && ReferenceEquals(rule, composer.Content.Get<IVisibilityTarget>(rule.Id));
            SharedOwner = composer.Services.Get<IVisibility>() is KnowledgeVisibility &&
                !typeof(IPersistentService).IsAssignableFrom(typeof(KnowledgeVisibility));
            Independent = policy is IndependentVisibilityPolicyDef;
            Initialize = new InitializeProbe("proof.visibility-content-init"); composer.AddSystem(Initialize, 12353);
        }
        public void Arm(VisibilityProbeStage stage, VisibilityProbeFault fault)
        { _stage = stage; _fault = fault; Calls = 0; MutationCompleted = false; ActualTarget = EntityId.None; }
        internal void Probe(World world, VisibilityProbeStage stage, PlayerId company, IContentDefinition definition, Entity entity = null)
        {
            if (_stage != stage) return;
            Calls++; ActualTarget = entity?.Id ?? EntityId.None;
            if (_fault == VisibilityProbeFault.Throw) throw new InvalidOperationException("proof.visibility-callback");
            if (_fault == VisibilityProbeFault.Pause) { ((GameClock)world.Clock).Pause(); MutationCompleted = true; return; }
            if (_fault == VisibilityProbeFault.PrivateEntity)
            { entity.Get<CompletionProbe>(new Code("visibility.entity-probe")).MutatePrivate(); MutationCompleted = true; return; }
            if (_fault == VisibilityProbeFault.Recursive)
                world.Services.Get<IVisibility>().CanSeeContent(world, company, definition);
            if (_fault == VisibilityProbeFault.SwallowedRecursive)
            { try { world.Services.Get<IVisibility>().CanSeeContent(world, company, definition); } catch (InvalidOperationException) { MutationCompleted = true; } }
        }
    }
}
