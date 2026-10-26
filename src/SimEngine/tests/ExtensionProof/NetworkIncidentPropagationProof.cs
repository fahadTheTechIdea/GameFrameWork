using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Reuses the framework declaration and eligibility; no incident, graph, flow or work state here.
    public sealed class IndependentNetworkPropagationRuleDef : Def, IIncidentPropagationRule
    {
        private NetworkIncidentPropagationDeclaration _declaration;
        public GameDuration Interval => _declaration.Interval;
        public GameDuration Delay => _declaration.Delay;
        public Code TargetSource => _declaration.TargetSource;
        public Code TargetKind => _declaration.TargetKind;
        public bool CrossCompany => _declaration.CrossCompany;
        protected override void ReadCore(JsonReader reader) => _declaration = NetworkIncidentPropagationDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateSource(EntityDef source) => _declaration.ValidateSource(source);
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world, Id);
        public IncidentExposureObservation Observe(IncidentPropagationContext context, Entity target)
            => _declaration.Observe(context, target, Id);
    }
    public sealed class NetworkIncidentProofModule : GameModule
    {
        private readonly InitializeProbe _initialize;
        public NetworkIncidentProofModule(InitializeProbe initialize = null) { _initialize = initialize; }
        public override string Id => "proof.network-incidents";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(IncidentPropagationModule.ModuleId,Version) };
        public override void DeclareContent(ContentTypes types)
            => types.Section<IndependentNetworkPropagationRuleDef>("independent_network_incident_rules");
        public override void Compose(WorldComposer composer)
        { if (_initialize != null) composer.AddSystem(_initialize,305); }
    }
}
