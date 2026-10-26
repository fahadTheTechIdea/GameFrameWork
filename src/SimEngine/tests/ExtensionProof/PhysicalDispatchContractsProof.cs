using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentDispatchEffectDef : Def, ITransformationDispatchEffect, IWorkEffectIntentAdmission
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        private TransformationDispatchDeclaration _declaration;
        public ITransformationDefinition Transformation => _declaration.Transformation;
        public Code Capability => _declaration.Capability;
        public int Count => _declaration.Count;
        public bool Repeat => _declaration.Repeat;
        public bool HomeIsTarget => _declaration.HomeIsTarget;
        protected override void ReadCore(JsonReader reader) => _declaration = TransformationDispatchDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver resolver) => _declaration.Resolve(resolver);
        public void RequireCapabilities(World world) => TransformationDispatchContracts.ValidateDefinition(world, this);
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => CheckStart(world, order.Owner, target);
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
        {
            Code result = TransformationDispatchContracts.Commit(world, this, order.Owner, target);
            if (result.IsNone) world.Services.Get<PhysicalDispatchContractsModule>().AfterCommit(world);
            return result;
        }
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => TransformationDispatchContracts.ValidatePlan(world, this, plan, node);
        public Code CheckIntent(World world, PlayerId company, Entity target)
            => TransformationDispatchContracts.CheckIntent(world, this, company, target);
        public Code CheckStart(World world, PlayerId company, Entity target) => TransformationDispatchContracts.CheckStart(world, this, company, target);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }

    // Intentional metadata violation: this is never a legitimate gameplay provider.
    public sealed class InvalidDispatchEffectDef : Def, ITransformationDispatchEffect
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        private TransformationDispatchDeclaration _declaration;
        public ITransformationDefinition Transformation => _declaration.Transformation;
        public Code Capability => _declaration.Capability;
        public int Count => 0;
        public bool Repeat => _declaration.Repeat;
        public bool HomeIsTarget => _declaration.HomeIsTarget;
        protected override void ReadCore(JsonReader reader) => _declaration = TransformationDispatchDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver resolver) => _declaration.Resolve(resolver);
        public void RequireCapabilities(World world) => TransformationDispatchContracts.ValidateDefinition(world, this);
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node) => Code.None;
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node) => Code.None;
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => TransformationDispatchContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }

    public enum AppliedDispatchFault { Pause, Speed, Exception }
    public sealed class PhysicalDispatchContractsModule : GameModule
    {
        public override string Id => "proof.physical-dispatch-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(OperationsModule.ModuleId, Version), new ModuleRequirement(WorkModule.ModuleId, Version),
            new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public InitializeProbe Initialize { get; private set; }
        public bool ActualAliases { get; private set; }
        public bool Direct { get; private set; }
        public bool Independent { get; private set; }
        public bool QueueOwner { get; private set; }
        public int Commits { get; private set; }
        public int CommittedQueueCount { get; private set; }
        public bool MutationCompleted { get; private set; }
        private AppliedDispatchFault? _fault;
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentDispatchEffectDef>("independent_dispatch_effects");
            types.Section<InvalidDispatchEffectDef>("invalid_dispatch_effects");
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind(this);
            var effect = composer.Content.Get<ITransformationDispatchEffect>("dispatch.enqueue");
            ActualAliases = composer.Content.Owns(effect) && ReferenceEquals(effect, composer.Content.Get<IWorkEffect>(effect.Id)) &&
                ReferenceEquals(effect.Transformation, composer.Content.Get<ITransformationDefinition>(effect.Transformation.Id));
            Direct = effect.GetType().BaseType == typeof(Def);
            Independent = effect is IndependentDispatchEffectDef;
            QueueOwner = ReferenceEquals(composer.Services.Get<ITransformationQueue>(), composer.Services.Get<TransformerManager>());
            Initialize = new InitializeProbe("proof.physical-dispatch-init"); composer.AddSystem(Initialize, 12350);
        }
        public void Arm(AppliedDispatchFault fault) { _fault = fault; Commits = 0; MutationCompleted = false; }
        internal void AfterCommit(World world)
        {
            Commits++;
            foreach (var entity in world.Entities.All)
                if (entity.Alive && entity.Def.Id == "dispatch.factory")
                    if (world.ViewFor(entity.Owner).Feature<Beep.SimEngine.Views.TransformationManagementView>().TryGetTransformation(
                        entity.Id, new Code("dispatch.production"), out var snapshot)) CommittedQueueCount = snapshot.QueueCount;
            if (!_fault.HasValue) return;
            if (_fault == AppliedDispatchFault.Exception) throw new InvalidOperationException("proof.applied-dispatch");
            if (_fault == AppliedDispatchFault.Pause) ((GameClock)world.Clock).Pause();
            if (_fault == AppliedDispatchFault.Speed) ((GameClock)world.Clock).Speed = 2;
            MutationCompleted = true;
        }
    }
}
