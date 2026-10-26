using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentWorkerConditionEffectDef : Def, IConditionEffect
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.AsReadOnly(new IContentDefinition[] { (IUnitContentDefinition)Unit });
        private ConditionEffectDeclaration _declaration;
        public Code Source => _declaration.Source;
        public IUnitDefinition Unit => _declaration.Unit;
        public ConditionChange Change => _declaration.Change;
        public bool WorkerSubject => true;
        protected override void ReadCore(JsonReader reader) => _declaration = ConditionEffectDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void RequireCapabilities(World world) => ConditionEffectContracts.ValidateDefinition(world, this);
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node)
        {
            Entity subject = ConditionEffectContracts.ResolveSubject(world, this, order, node);
            world.Services.Get<WorkerEffectContractsModule>().Probe(world, subject, WorkerEffectFault.ResolverPrivate);
            return subject;
        }
        public Code CheckSubject(World world, Entity subject, Order order, IActivityNodeDefinition node)
        {
            world.Services.Get<WorkerEffectContractsModule>().Probe(world, subject, WorkerEffectFault.CheckPrivate);
            return ConditionEffectContracts.Check(world, this, subject);
        }
        public Code CommitSubject(World world, Entity subject, Order order, IActivityNodeDefinition node)
        {
            var probe = world.Services.Get<WorkerEffectContractsModule>();
            if (probe.Refuse(world, subject)) return WorkEffectCodes.NoStock;
            return ConditionEffectContracts.Commit(world, this, subject);
        }
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => ConditionEffectContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }
    public enum WorkerEffectFault { ResolverPrivate, CheckPrivate, RefusedCommitPrivate, RefusedCommitTouch, ThrowCommit }
    // Transient callback diagnostics only; no condition, worker identity, elapsed work or recovery owner.
    public sealed class WorkerEffectContractsModule : GameModule
    {
        public override string Id => "proof.worker-effect-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(EquipmentConditionModule.ModuleId, Version), new ModuleRequirement(WorkModule.ModuleId, Version) };
        public InitializeProbe Initialize { get; private set; }
        public bool ActualAliases { get; private set; }
        public bool Independent { get; private set; }
        public bool SharedOwners { get; private set; }
        private WorkerEffectFault? _fault;
        public int FaultCallbacks { get; private set; }
        public bool MutationCompleted { get; private set; }
        public EntityId ActualSubject { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentWorkerConditionEffectDef>("independent_worker_condition_effects");
            types.Component<CompletionProbeDef>("worker_effect_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind(this);
            var effect = composer.Content.Get<IConditionEffect>("worker.damage");
            ActualAliases = effect.WorkerSubject && composer.Content.Owns(effect) &&
                ReferenceEquals(effect, composer.Content.Get<IWorkEffect>(effect.Id)) &&
                ReferenceEquals(effect.Unit, composer.Content.Find<IUnitContentDefinition>(effect.Unit.Unit.Id));
            Independent = effect is IndependentWorkerConditionEffectDef;
            SharedOwners = ReferenceEquals(composer.Services.Get<IActivityExecutionOrders>(), composer.Services.Get<WorkBoard>());
            Initialize = new InitializeProbe("proof.worker-effect-init"); composer.AddSystem(Initialize, 12351);
        }
        public void Arm(WorkerEffectFault fault) { _fault = fault; FaultCallbacks = 0; MutationCompleted = false; ActualSubject = EntityId.None; }
        internal void Probe(World world, Entity subject, WorkerEffectFault stage)
        {
            if (subject == null || _fault != stage) return;
            FaultCallbacks++; ActualSubject = subject.Id;
            subject.Get<CompletionProbe>(new Code("worker.effect-probe")).MutatePrivate(); MutationCompleted = true;
        }
        internal bool Refuse(World world, Entity subject)
        {
            if (!_fault.HasValue || _fault == WorkerEffectFault.ResolverPrivate || _fault == WorkerEffectFault.CheckPrivate) return false;
            FaultCallbacks++; ActualSubject = subject.Id;
            if (_fault == WorkerEffectFault.ThrowCommit) throw new InvalidOperationException("proof.worker-effect-commit");
            if (_fault == WorkerEffectFault.RefusedCommitPrivate) subject.Get<CompletionProbe>(new Code("worker.effect-probe")).MutatePrivate();
            if (_fault == WorkerEffectFault.RefusedCommitTouch) subject.Touch();
            MutationCompleted = true; return true;
        }
    }
}
