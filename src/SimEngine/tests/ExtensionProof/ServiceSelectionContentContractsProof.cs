using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentFleetPolicyDef : Def, IFleetPolicy
    {
        private FleetPolicyDeclaration _declaration;
        internal string Fault { get; private set; }
        internal Action<string, EntityId, EntityId> Observer;
        public IReadOnlyList<IContentDefinition> ContentDependencies
        { get { Observer?.Invoke("dependencies", EntityId.None, EntityId.None); return _declaration.ContentDependencies; } }
        public IReadOnlyList<string> NavigationClasses
        { get { Observer?.Invoke("classes", EntityId.None, EntityId.None); return _declaration.NavigationClasses; } }
        protected override void ReadCore(JsonReader reader)
        { _declaration=FleetPolicyDeclaration.Read(reader); Fault=reader.String("metadata_fault", ""); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void Validate(World world)
        { Observer?.Invoke("validate", EntityId.None, EntityId.None); _declaration.Validate(world); }
        public Code CheckMember(World world,Entity member)
        { Observer?.Invoke("member", EntityId.None, member.Id); return _declaration.CheckMember(world,member); }
        public Code CheckNavigation(string profile)
        { Observer?.Invoke("navigation", EntityId.None, EntityId.None); return _declaration.CheckNavigation(profile); }
    }
    public sealed class IndependentFacilityPolicyDef : Def, IActivityFacilityPolicy
    {
        private ActivityFacilityPolicyDeclaration _declaration;
        internal string Fault { get; private set; }
        internal Action<string, EntityId, EntityId> Observer;
        public string Node
        { get { Observer?.Invoke("node", EntityId.None, EntityId.None); return _declaration.Node; } }
        protected override void ReadCore(JsonReader reader)
        { _declaration=ActivityFacilityPolicyDeclaration.Read(reader); Fault=reader.String("metadata_fault", ""); }
        public long? Rank(ActivityFacilityFacts facts)
        { Observer?.Invoke("rank", facts.Target, facts.Worker); return _declaration.Rank(facts); }
    }
    // Content registration and diagnostic probe only. All actual admission and work use native engine owners.
    public sealed class ServiceSelectionContentModule : GameModule
    {
        public const string ModuleId="proof.service-selection";
        private readonly InitializeProbe _initialize;
        private bool _armed;
        public void ArmObservationFault() => _armed=true;
        public ServiceSelectionContentModule(InitializeProbe initialize) { _initialize=initialize; }
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[]{new ModuleRequirement(WorkModule.ModuleId,Version)};
        public bool IndependentPolicies { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentFleetPolicyDef>("independent_fleet_policies");
            types.Section<IndependentFacilityPolicyDef>("independent_facility_policies");
            types.Component<ExposurePrivateProbeDef>("selection_private_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            var fleet=composer.Content.Get<IFleetPolicy>("contract.fleet");
            var facility=composer.Content.Get<IActivityFacilityPolicy>("contract.facility.home");
            IndependentPolicies=fleet is IndependentFleetPolicyDef && facility is IndependentFacilityPolicyDef
                &&composer.Content.Owns(fleet)&&composer.Content.Owns(facility);
            void Observe(string fault, string phase, EntityId target, EntityId worker)
            {
                // Deliberately invalid observer injection, not normal game or policy logic.
                if ((!_armed && phase!="validate") || !fault.StartsWith(phase+"-", StringComparison.Ordinal)) return;
                World world=composer.World;
                if(fault.EndsWith("throw", StringComparison.Ordinal)) throw new ContentException("proof.selection."+phase);
                if(fault.EndsWith("pause", StringComparison.Ordinal)) ((GameClock)world.Clock).Pause();
                if(fault.EndsWith("private", StringComparison.Ordinal))
                    foreach(Entity entity in world.Entities.All)
                        if(entity.Id==(fault=="rank-target-private"?target:worker) || phase=="node" && entity.Def.Id=="machine.a")
                            entity.Get<ExposurePrivateProbe>()?.Corrupt();
            }
            foreach(var policy in composer.Content.All<IndependentFleetPolicyDef>())
                policy.Observer=(phase,target,worker)=>Observe(policy.Fault,phase,target,worker);
            foreach(var policy in composer.Content.All<IndependentFacilityPolicyDef>())
                policy.Observer=(phase,target,worker)=>Observe(policy.Fault,phase,target,worker);
            composer.AddSystem(_initialize,301);
        }
    }
}
