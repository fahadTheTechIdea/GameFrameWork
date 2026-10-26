using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class InfrastructureModule : GameModule
    {
        public const string ModuleId = "sim.infrastructure";
        public const int SystemOrder = 420;
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[]
        {
            new ModuleRequirement(WorkModule.ModuleId, Version),
            new ModuleRequirement(InventoryModule.ModuleId, Version)
        });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<LineEffectDef>("line_effects");
            types.Section<LineDef>("lines");
            types.Section<TileLinePlacementPolicyDef>("tile_line_placement_policies");
            types.Section<NetworkDef>("networks");
            types.Section<TargetNetworkResourceRequirementDef>("target_network_resource_requirements");
            types.Section<WorkerNetworkResourceRequirementDef>("worker_network_resource_requirements");
            types.Section<RoundRobinNetworkFlowPolicyDef>("network_round_robin_policies");
            types.Section<ResourceTransferNetworkSolverDef>("resource_transfer_network_solvers");
            types.Section<CapacityNetworkDemandPolicyDef>("capacity_network_demands");
            types.Section<UtilityNetworkDemandPolicyDef>("utility_network_demands");
            types.Component<PortsDef>("ports", 1);
            types.Component<NetworkResourcePortDef>("network_resource_port", 1);
            types.Component<SignalNetworkGateDef>("signal_network_gate", 1);
            types.Component<NetworkSignalDef>("network_signal", 1);
            types.Component<LineWorkDef>("line_work", 3);
        }
        public override void Compose(WorldComposer composer)
        {
            LineDefinitionContracts.ValidateComposition(composer.World);
            LinePlacementContracts.ValidateComposition(composer.World);
            NetworkDefinitionContracts.ValidateComposition(composer.World);
            NetworkPortsContracts.ValidateComposition(composer.World);
            NetworkSignalBindingContracts.ValidateComposition(composer.World);
            NetworkGateBindingContracts.ValidateComposition(composer.World);
            composer.Services.Require<WorkBoard>();
            if (!(composer.World.Space is ILineGeometryComposition spatial))
                throw new ContentException("selected infrastructure requires declared corridor geometry");
            spatial.ComposeLineGeometry(composer);
            composer.Services.Require<ILineGeometry>();
            if(composer.Services.TryGet<INetworkGraphGeometry>(out var geometry))
            {
                var networks=new NetworkService(composer.World,geometry);
                composer.Services.Bind<INetworkTopology>(networks);
                composer.Services.Bind<ILineSimulation>(networks);
                composer.Services.Bind<INetworkDiagnostics>(networks);
                composer.Services.Bind<INetworkResourceAccess>(networks);
                composer.AddViewFeature("sim.infrastructure",7,(world,viewer)=>new InfrastructureView(world,viewer));
            }
            else foreach(var network in composer.Content.All<INetworkDefinition>())
                if(network.Line.Layer.Space.Id==composer.World.Space.Id)
                    throw new ContentException(network.Id+": selected network requires INetworkGraphGeometry");
            var lines = new LineConstructionService(composer.World,composer.Services.Get<ILineGeometry>());
            composer.Services.Bind<ILineConstruction>(lines);
            composer.Services.Bind<ILineWorkEffects>(lines);
            if (composer.Services.Get<ILineGeometry>() is ILineRemovalGeometry)
                composer.Services.Bind<ILineRemoval>(lines);
            composer.AddSystem(new LineSystem(), SystemOrder);
        }
    }
}
