using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent JSON metadata binds the SAME stateless Transporter, not another inventory.
    public sealed class IndependentTransporterDef : ComponentDef<Transporter>, ITransporterDefinition
    {
        private TransporterDeclaration _declaration;
        private bool _invalid;
        internal Action Observe;
        internal IResourceHandlingProfile Alternative;
        public IResourceHandlingProfile Handling { get { Observe?.Invoke(); return _invalid ? null : Alternative ?? _declaration.Handling; } }
        protected override void ReadCore(JsonReader reader)
        { _declaration = TransporterDeclaration.Read(reader); _invalid = reader.Bool("proof_null", false); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(EntityDef owner) => TransporterDeclaration.ValidateBinding(owner);
    }
    // Intentionally invalid metadata/runtime pairings, no alternative gameplay implementation.
    public sealed class UndeclaredTransporterDef : ComponentDef<Transporter>
    { protected override void ReadCore(JsonReader reader) { } }
    public sealed class NonTransporterDefinition : ComponentDef<NonTransporter>, ITransporterDefinition
    {
        private TransporterDeclaration _declaration;
        public IResourceHandlingProfile Handling => _declaration.Handling;
        public void ValidateBinding(EntityDef owner) => TransporterDeclaration.ValidateBinding(owner);
        protected override void ReadCore(JsonReader reader) => _declaration = TransporterDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class NonTransporter : Component<NonTransporterDefinition> { }
    public enum TransporterBindingFault { Exception, Touch, PauseClock, AllocateIdentity }
    public sealed class TransporterBindingModule : GameModule
    {
        private World _world;
        private IResourceHandlingProfile _profile;
        public bool NativeBinding { get; private set; }
        public int Reads { get; private set; }
        public override string Id => "proof.transporter-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(LogisticsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentTransporterDef>("independent_transporter", 2);
            types.Component<UndeclaredTransporterDef>("undeclared_transporter", 2);
            types.Component<NonTransporterDefinition>("non_transporter", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            _profile = composer.Content.Get<IResourceHandlingProfile>("contract.handling");
            var definition = composer.Content.Get<EntityDef>("machine").Component<ITransporterDefinition>();
            NativeBinding = definition is IndependentTransporterDef && definition.ComponentType == typeof(Transporter)
                && ReferenceEquals(definition.Handling, _profile);
        }
        private IndependentTransporterDef Metadata()
            => (IndependentTransporterDef)_world.Content.Get<EntityDef>("machine").Component<ITransporterDefinition>();
        public void Arm(TransporterBindingFault fault)
        {
            Metadata().Observe = () => {
                if (Reads != 0) return; Reads++;
                if (fault == TransporterBindingFault.Exception) throw new InvalidOperationException("proof.transporter.observation");
                if (fault == TransporterBindingFault.PauseClock) ((GameClock)_world.Clock).Pause();
                foreach (var entity in _world.Entities.All)
                    if (entity.Def.Id == "machine")
                    {
                        if (fault == TransporterBindingFault.Touch) entity.Touch();
                        if (fault == TransporterBindingFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("source"), new PlayerId(0));
                        return;
                    }
            };
        }
        public void ArmForeign(TransporterBindingModule other) => Metadata().Alternative = other._profile;
    }
}
