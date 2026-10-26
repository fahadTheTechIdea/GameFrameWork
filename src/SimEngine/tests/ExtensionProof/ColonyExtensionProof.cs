using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // One external content package, no execution owner. Every declaration and
    // runtime below already exists and keeps its original contract/codec owner.
    public sealed class ColonyExtensionProofModule : GameModule
    {
        public override string Id => "proof.colony-extension";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(ConstructionModule.ModuleId, Version),
            new ModuleRequirement(InventorySignalsModule.ModuleId, Version),
            new ModuleRequirement(IncidentResponseModule.ModuleId, Version),
            new ModuleRequirement(TileNavigationModule.ModuleId, Version),
            new ModuleRequirement("proof.tile-sensor-space", Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentWorkerDef>("mod_worker", 1);
            types.Component<SingleResourcePortDef>("mod_stock", 4);
            types.Component<IndependentConstructionDef>("mod_construction", 1);
            types.Component<IndependentConstructionSiteDef>("mod_site", 1);
            types.Component<IndependentInventorySignalDef>("mod_signal", 1);
            types.Section<IndependentActivityContentDef>("mod_activities");
            types.Section<IndependentIncidentWorkflowDef>("mod_incident_workflows");
            types.Section<IndependentIncidentCompletionDef>("mod_incident_completions");
        }
    }
}
