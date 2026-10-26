using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentIncidentProcessDef : ComponentDef<IndependentIncidentProcess>, IIncidentProcessDefinition
    {
        private IncidentProcessBindingDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public IIncidentProcessProfile Profile => _declaration.Profile;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentProcessBindingDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(World world) => IncidentProcessContracts.ValidateDefinition(world, this);
    }
    public sealed class IndependentIncidentProcess : Component, IIncidentProcess
    {
        private readonly IncidentProcessState _state;
        public IndependentIncidentProcess() { _state = new IncidentProcessState(this); }
        public IIncidentProcessDefinition Definition => (IIncidentProcessDefinition)Def;
        public Code CapabilityId => Definition.CapabilityId;
        public IncidentProcessSnapshot ReadProcess(World world) => _state.Read(world);
        public void UpdateProcess(World world, TickInfo tick) => _state.Update(world, tick);
        public void ValidateProcessCheckpoint(World world) => _state.ValidateLoaded(world);
        public override void Sync(IStateSync sync) => _state.Sync(sync);
    }
    public sealed class IncidentProcessProofModule : GameModule
    {
        public override string Id => "proof.incident-process";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(IncidentProcessModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Component<IndependentIncidentProcessDef>("independent_incident_process", 1);
    }
}
