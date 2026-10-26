using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentContactProfileDef : Def, IAuthoredExplorationProfileDefinition
    {
        private AuthoredExplorationProfileDeclaration _declaration;
        public IReadOnlyList<AuthoredExplorationEntry> Entries => _declaration.Entries;
        public EntityStateBindings States => _declaration.States;
        protected override void ReadCore(JsonReader reader) => _declaration = AuthoredExplorationProfileDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class IndependentContactBindingDef : ComponentDef<AuthoredExploration>, IAuthoredExplorationDefinition
    {
        private AuthoredExplorationDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Sensor => _declaration.Sensor;
        public IAuthoredExplorationProfileDefinition Profile => _declaration.Profile;
        protected override void ReadCore(JsonReader reader) => _declaration = AuthoredExplorationDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public void ValidateBinding(World world) => AuthoredExplorationContracts.ValidateBinding(world, this);
    }
    // Registered negative metadata proves all loaded profile validation; never an accepted owner.
    public sealed class MissingContactEntriesDef : Def, IAuthoredExplorationProfileDefinition
    {
        private EntityStateBindings _states;
        public IReadOnlyList<AuthoredExplorationEntry> Entries => null;
        public EntityStateBindings States => _states;
        protected override void ReadCore(JsonReader reader) => _states = EntityStateBindings.Read(reader, "sampling");
        protected override void ResolveCore(ContentResolver content) => _states = _states.Resolve(content);
    }
    public sealed class InvalidContactBindingDef : ComponentDef<AuthoredExploration>, IAuthoredExplorationDefinition
    {
        private AuthoredExplorationDeclaration _declaration;
        private string _fault;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Sensor => _declaration.Sensor;
        public IAuthoredExplorationProfileDefinition Profile => _declaration.Profile;
        protected override void ReadCore(JsonReader reader)
        { _fault = reader.String("fault"); _declaration = AuthoredExplorationDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public void ValidateBinding(World world)
        {
            AuthoredExplorationContracts.ValidateBinding(world, this);
            var probe = world.Services.Get<ExplorationBindingsModule>(); probe.InvalidCalls++;
            if (_fault == "pause") { ((GameClock)world.Clock).Pause(); probe.MutationReached = true; return; }
            throw new InvalidOperationException("proof.exploration.invalid-binding");
        }
    }
    public sealed class ExplorationBindingsModule : GameModule
    {
        public const string ModuleId = "proof.exploration-bindings";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(ExplorationModule.ModuleId, Version), new ModuleRequirement(PointSensorModule.ModuleId, Version) };
        public InitializeProbe Initialize { get; private set; }
        public bool ActualAliases { get; private set; }
        public bool SharedComponent { get; private set; }
        public bool Independent { get; private set; }
        public int InvalidCalls { get; internal set; }
        public bool MutationReached { get; internal set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentContactProfileDef>("independent_contact_profiles");
            types.Section<MissingContactEntriesDef>("invalid_contact_profiles");
            types.Component<IndependentContactBindingDef>("independent_contact", 1);
            types.Component<InvalidContactBindingDef>("invalid_contact", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind(this);
            var profile = composer.Content.Get<IAuthoredExplorationProfileDefinition>("binding.profile");
            var entity = composer.Content.Get<EntityDef>("sensor.machine");
            var binding = entity.Component<IAuthoredExplorationDefinition>(new Code("binding.contact"));
            ActualAliases = composer.Content.Owns(profile) && ReferenceEquals(binding.Profile, profile);
            SharedComponent = ((ComponentDef)binding).ComponentType == typeof(AuthoredExploration);
            Independent = profile is IndependentContactProfileDef && binding is IndependentContactBindingDef;
            Initialize = new InitializeProbe("proof.exploration-bindings-init"); composer.AddSystem(Initialize, 12354);
        }
    }
}
