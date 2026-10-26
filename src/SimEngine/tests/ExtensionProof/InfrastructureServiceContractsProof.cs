using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Composition facts only; no normal gameplay mutations or replacement service.
    public sealed class InfrastructureServiceContractsProofModule : GameModule
    {
        public override string Id => "proof.infrastructure-services";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) };
        public int Compositions { get; private set; }
        public bool OneOwner { get; private set; }
        public bool ConcreteBound { get; private set; }
        public int PersistentOwners { get; private set; }
        public int EndpointOwners { get; private set; }
        public override void Compose(WorldComposer composer)
        {
            var services = composer.Services;
            services.Require<ILineConstruction>(); services.Require<ILineWorkEffects>(); services.Require<ILineSimulation>();
            object construction = services.Get<ILineConstruction>();
            OneOwner = ReferenceEquals(construction, services.Get<ILineWorkEffects>()) &&
                ReferenceEquals(construction,services.Get<ILineRemoval>()) &&
                ReferenceEquals(services.Get<ILineGeometry>(),services.Get<ILineRemovalGeometry>()) &&
                !ReferenceEquals(construction, services.Get<ILineGeometry>()) &&
                ReferenceEquals(services.Get<INetworkTopology>(), services.Get<ILineSimulation>()) &&
                ReferenceEquals(services.Get<INetworkTopology>(), services.Get<INetworkDiagnostics>()) &&
                !ReferenceEquals(services.Get<ILineGeometry>(), services.Get<INetworkTopology>());
            ConcreteBound = services.Has<NetworkService>();
            PersistentOwners = services.Providers<IPersistentService>().Count(p => p.Id == "sim.lines");
            EndpointOwners = services.Providers<IResourceEndpointProvider>().Count(p => ReferenceEquals(p, services.Get<INetworkTopology>()));
            Compositions++;
        }
    }

    // Unused authored effects must refuse missing capability before Initialize.
    public sealed class MissingLineEffectsProofModule : GameModule
    {
        private readonly InitializeProbe _initialize;
        public MissingLineEffectsProofModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => "proof.missing-line-effects";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentLineEffectDef>("independent_line_effects");
        public override void Compose(WorldComposer composer) => composer.AddSystem(_initialize, 303);
    }
}
