using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent metadata uses the original JSON declaration, runtime, reference state and codec.</summary>
    public sealed class IndependentRemoteSignalDef : ComponentDef<RemoteSignal>, IRemoteSignalDefinition
    {
        private RemoteSignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public IReadOnlyList<EntityDef> ProviderDefinitions => _declaration.ProviderDefinitions;
        public Code Source => _declaration.Source;
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => UtilitySignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = RemoteSignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class IndependentRemoteSignalsModule : GameModule
    {
        public const string ModuleId = "proof.remote-utility";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(RemoteSignalsModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types) => types.Component<IndependentRemoteSignalDef>("proof_remote_utility_signal", 1);
    }
}
