using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentResearchCompletionDef : Def, ITechnologyCompletionEffect, IWorkEffectPlanValidator, IWorkEffectPreview
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private Code _consumer;
        protected override void ReadCore(JsonReader reader)
        {
            JsonValue declared = reader.Value("consumer");
            if (declared == null || declared.Kind != JsonKind.Null && declared.Kind != JsonKind.String)
                throw reader.Error("explicit nullable consumer required");
            string consumer = declared.Kind == JsonKind.Null ? null : declared.Text;
            if (consumer != null && string.IsNullOrWhiteSpace(consumer)) throw reader.Error("empty consumer is not an opt-out");
            _consumer = consumer == null ? Code.None : new Code(consumer);
        }
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => TechnologyCompletionContracts.ValidatePlan(this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
        {
            Code check = TechnologyCompletionContracts.Check(world, order, node);
            if (!check.IsNone || _consumer.IsNone) return check;
            var provider = world.Entities.Get(order.Target)?.Get<IUtilityConsumer>(_consumer);
            return provider == null ? UtilityWorkRequirementDeclaration.MissingConsumer : UtilityContracts.Read(world, provider).Blocker;
        }
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
        {
            var probe = world.Services.Get<WorkEffectContractsModule>();
            if (probe.RefuseWithFault(world, order)) return WorkEffectResult.Pending(UtilityCodes.Inactive);
            Code refusal = Check(world, order, node);
            return refusal.IsNone ? TechnologyCompletionContracts.Apply(world, order, node) : WorkEffectContracts.Outcome(world, refusal);
        }
    }
    public sealed class StatefulWorkEffectDef : Def, IWorkEffect, IPersistentService
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private bool _state;
        public int StateSchemaVersion => 1;
        protected override void ReadCore(JsonReader reader) { }
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node) => WorkEffectResult.Applied;
        public void Sync(IStateSync sync) => sync.Bool(ref _state);
    }
    public sealed class CompletionProbeDef : ComponentDef<CompletionProbe>, INamedCapabilityDefinition
    {
        public Code CapabilityId { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { CapabilityId = new Code(reader.String("id")); if (CapabilityId.IsNone) throw reader.Error("explicit probe identity required"); }
    }
    public sealed class CompletionProbe : Component<CompletionProbeDef>, INamedEntityCapability
    {
        private long _counter;
        public Code CapabilityId => Def.CapabilityId;
        internal void MutatePrivate() { _counter = checked(_counter + 1); }
        public override void Sync(IStateSync sync) => sync.Long(ref _counter);
    }
    public enum RefusedEffectFault { PrivateTarget, TouchTarget, Clock, Identity, Exception }
    // Transient fault instrumentation only. WorkBoard and CompanyKnowledge own completion/state.
    public sealed class WorkEffectContractsModule : GameModule
    {
        public const string ModuleId = "proof.work-effect-contracts";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TechnologyModule.ModuleId, Version) };
        public InitializeProbe Initialize { get; private set; }
        public bool ActualAlias { get; private set; }
        public bool Independent { get; private set; }
        public bool IndependentTechnology { get; private set; }
        private RefusedEffectFault? _fault;
        private SpatialCallbackFault? _spatialFault;
        public bool SpatialPayloadRestored { get; private set; }
        public bool SpatialRevisionChanged { get; private set; }
        public int Refusals { get; private set; }
        public bool MutationCompleted { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentResearchCompletionDef>("independent_research_effects");
            types.Section<IndependentTechnologyDef>("independent_technologies");
            types.Section<StatefulWorkEffectDef>("stateful_work_effects");
            types.Component<CompletionProbeDef>("completion_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind(this);
            var effect = composer.Content.Get<ITechnologyCompletionEffect>("research.complete");
            ActualAlias = ReferenceEquals(effect, composer.Content.Get<IWorkEffect>(effect.Id)) && composer.Content.Owns(effect);
            Independent = effect is IndependentResearchCompletionDef;
            IndependentTechnology = composer.Content.Get<ITechnologyDefinition>("research.foundation") is IndependentTechnologyDef;
            Initialize = new InitializeProbe("proof.work-effect-init"); composer.AddSystem(Initialize, 12349);
        }
        public void Arm(RefusedEffectFault fault) { _fault = fault; _spatialFault=null; Refusals = 0; MutationCompleted = false; }
        public void ArmSpatial(SpatialCallbackFault fault)
        { _fault=null;_spatialFault=fault;Refusals=0;MutationCompleted=false;SpatialPayloadRestored=SpatialRevisionChanged=false; }
        internal bool RefuseWithFault(World world, Order order)
        {
            if(_spatialFault.HasValue)
            {
                Refusals++;var fault=_spatialFault.Value;
                var changed=SpatialMutationProbe.Change(world,fault==SpatialCallbackFault.Revision||fault==SpatialCallbackFault.RevisionThrow);
                SpatialPayloadRestored=changed.PayloadRestored;SpatialRevisionChanged=changed.RevisionChanged;MutationCompleted=true;
                if(fault==SpatialCallbackFault.LayerThrow||fault==SpatialCallbackFault.RevisionThrow)
                    throw new InvalidOperationException("proof.spatial-effect-first-error");
                return true;
            }
            if (!_fault.HasValue) return false;
            Refusals++;
            var target = world.Entities.Get(order.Target);
            if (_fault == RefusedEffectFault.Exception) throw new InvalidOperationException("proof.refused-effect");
            if (_fault == RefusedEffectFault.PrivateTarget) target.Get<CompletionProbe>(new Code("completion.probe")).MutatePrivate();
            if (_fault == RefusedEffectFault.TouchTarget) target.Touch();
            if (_fault == RefusedEffectFault.Clock) ((GameClock)world.Clock).Pause();
            if (_fault == RefusedEffectFault.Identity) world.Spawn(world.Content.Get<EntityDef>("contract.result"), order.Owner);
            MutationCompleted = true; return true;
        }
    }
}
