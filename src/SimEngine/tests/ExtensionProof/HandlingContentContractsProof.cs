using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentHandlingDef : Def, IResourceHandlingProfile
    {
        private ResourceHandlingDeclaration _declaration;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceHandlingDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public ResourceHandlingCheck CheckHandling(ISimulationTime time, ResourceId resource, Fix amount, ResourceHandlingPhase phase)
            => _declaration.CheckHandling(time, resource, amount, phase);
    }
    // Deliberately adversarial engine extension, never a gameplay scheduler/fixture mutation.
    public sealed class FaultHandlingDef : Def, IResourceHandlingProfile
    {
        private string _mode;
        private World _session;
        protected override void ReadCore(JsonReader reader)
        {
            _mode = reader.String("mode");
            if (_mode != "pause" && _mode != "throw" && _mode != "default") throw reader.Error("explicit fault mode required");
        }
        public void Attach(World world) => _session = world;
        public ResourceHandlingCheck CheckHandling(ISimulationTime time, ResourceId resource, Fix amount, ResourceHandlingPhase phase)
        {
            if (_mode == "throw") throw new InvalidOperationException("adversarial handling callback");
            if (_mode == "pause") { ((GameClock)_session.Clock).Pause(); return new ResourceHandlingCheck(ResourceHandlingStatus.Ready, 1); }
            return default;
        }
    }
    public sealed class HandlingContractModule : GameModule
    {
        public const string ModuleId = "proof.handling-contracts";
        private readonly InitializeProbe _initialize;
        public HandlingContractModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(LogisticsModule.ModuleId, Version) };
        public bool IndependentAlias { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentHandlingDef>("independent_handling_profiles");
            types.Section<SlotHandlingDef>("slot_handling_profiles");
            types.Section<FaultHandlingDef>("fault_handling_profiles");
        }
        public override void Compose(WorldComposer composer)
        {
            var handler = composer.Content.Get<IResourceHandlingProfile>("contract.handling");
            IndependentAlias = !(handler is AuthoredResourceHandlingDef) && composer.Content.Owns(handler)
                && ReferenceEquals(composer.Content.Find<IResourceHandlingProfile>(handler.Id), handler);
            foreach (var fault in composer.Content.All<FaultHandlingDef>()) fault.Attach(composer.World);
            composer.AddSystem(_initialize, 301);
        }
    }
}
