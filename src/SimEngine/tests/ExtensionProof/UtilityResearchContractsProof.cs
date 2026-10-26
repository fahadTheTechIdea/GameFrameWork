using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentUtilityRequirementDef : Def, IWorkRequirement
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private UtilityWorkRequirementDeclaration _declaration;
        public Code Consumer => _declaration.Consumer;
        IReadOnlyList<string> IDependencyNode.Dependencies => Array.Empty<string>();
        protected override void ReadCore(JsonReader reader) => _declaration = UtilityWorkRequirementDeclaration.Read(reader, "source");
        public WorkRequirementResult Evaluate(WorkRequirementContext context) => _declaration.Evaluate(context, context.Target);
    }
    public sealed class ResearchUtilityDef : ComponentDef<ResearchUtility>, INamedCapabilityDefinition
    {
        public Code CapabilityId { get; private set; }
        public Code Source { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            CapabilityId = new Code(reader.String("id")); Source = new Code(reader.String("source"));
            if (CapabilityId.IsNone || Source.IsNone) throw reader.Error("explicit utility identities required");
        }
        protected override void ValidateCore(EntityDef owner)
        {
            if (Source == CapabilityId || owner.Component<INamedCapabilityDefinition>(Source) == null ||
                !typeof(IUtilityConsumer).IsAssignableFrom(owner.Component<INamedCapabilityDefinition>(Source).ComponentType))
                throw new ContentException(owner.Id + ": research utility needs another actual utility component");
        }
    }
    public enum UtilityResearchFault { PrivateState, Clock, Identity, Exception }
    // No consumption or research loop: forwards the original guarded consumer. Counter is codec instrumentation.
    public sealed class ResearchUtility : Component<ResearchUtilityDef>, IUtilityConsumer
    {
        private long _counter;
        public Code CapabilityId => Def.CapabilityId;
        public IResourcePort ResolveUtilityPort(World world, ResourceId resource)
            => Entity.Get<IUtilityConsumer>(Def.Source).ResolveUtilityPort(world, resource);
        public UtilityDemandSnapshot ReadUtilityDemand(World world)
        {
            world.Services.Get<UtilityResearchContractsModule>().Observe(world, this);
            var native = UtilityContracts.Read(world, Entity.Get<IUtilityConsumer>(Def.Source));
            return new UtilityDemandSnapshot(CapabilityId, native.ElapsedTicks, native.Ports, native.Drawing,
                native.Consumed, native.NextInputs, native.Available, native.NextBlocker);
        }
        internal void ChangePrivateState() { _counter = checked(_counter + 1); }
        public override void Sync(IStateSync sync) => sync.Long(ref _counter);
    }
    public sealed class UtilityResearchContractsModule : GameModule
    {
        public const string ModuleId = "proof.utility-research";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(WorkModule.ModuleId, Version),
            new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public bool DirectAlias { get; private set; }
        private UtilityResearchFault? _fault;
        public bool MutationCompleted { get; private set; }
        public int Reads { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentUtilityRequirementDef>("independent_utility_requirements");
            types.Component<ResearchUtilityDef>("research_utility", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind(this);
            var requirement = composer.Content.Get<IWorkRequirement>("lab.independent-ready");
            DirectAlias = requirement is IndependentUtilityRequirementDef &&
                typeof(IndependentUtilityRequirementDef).BaseType == typeof(Def) && composer.Content.Owns(requirement);
        }
        public void Arm(UtilityResearchFault fault) { _fault = fault; Reads = 0; MutationCompleted = false; }
        internal void Observe(World world, ResearchUtility provider)
        {
            if (!_fault.HasValue || Reads != 0) return;
            Reads++;
            if (_fault == UtilityResearchFault.Exception) throw new InvalidOperationException("proof.utility-read");
            if (_fault == UtilityResearchFault.PrivateState) provider.ChangePrivateState();
            if (_fault == UtilityResearchFault.Clock) ((GameClock)world.Clock).Pause();
            if (_fault == UtilityResearchFault.Identity) world.Spawn(world.Content.Get<EntityDef>("contract.result"), provider.Entity.Owner);
            MutationCompleted = true;
        }
    }
}
