using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // A direct content implementation delegates normal gameplay to the original engine algorithms.
    public sealed class IndependentLineEffectDef : Def, ILineEffectDefinition, IWorkEffectIntentAdmission
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => LineEffectContracts.ContentReferences(this);
        private LineEffectDeclaration _declaration;
        public ILineDefinition Line => _declaration.Line;
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public Code CheckIntent(World world, PlayerId company, Entity target)
            => LineEffectContracts.CheckIntent(world, this, company, target);
        public Code CheckStart(World world, PlayerId company, Entity target)
            => LineEffectContracts.CheckStart(world, this, company, target);
        public string Fault { get; private set; }
        internal Action ObserveOperation;
        string IContentDefinition.Id => Fault == "id-alias" ? "wrong.alias" : Id;
        string IContentDefinition.Module => Fault == "module-alias" ? "wrong.module" : Module;
        public LineEffectOperation Operation
        {
            get
            {
                ObserveOperation?.Invoke();
                if (Fault == "getter-throw") throw new ContentException("proof.line-effect.operation");
                return Fault == "invalid-enum" ? (LineEffectOperation)255 : _declaration.Operation;
            }
        }
        protected override void ReadCore(JsonReader reader)
        { Fault = reader.String("metadata_fault", "none"); _declaration = LineEffectDeclaration.Read(reader); }
        public void RequireCapabilities(World world) => LineEffectContracts.RequireCapabilities(world, this);
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node)
            => LineEffectContracts.ResolveSubject(world, order, node);
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => LineEffectContracts.CheckSubject(world, this, target, order, node);
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => LineEffectContracts.CommitSubject(world, this, target, order, node);
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => LineEffectContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }

    public sealed class LineEffectContractsModule : GameModule
    {
        private readonly Action<LineWork> _cursorCorruption;
        public LineEffectContractsModule() : this(null) { }
        public LineEffectContractsModule(Action<LineWork> cursorCorruption) { _cursorCorruption = cursorCorruption; }
        public override string Id => "proof.line-effect-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) };
        public readonly InitializeProbe Initialize = new InitializeProbe("proof.line-effect-init");
        public bool ActualAliases { get; private set; }
        public bool Independent { get; private set; }
        private bool _armed;
        public void ArmObservationFault() => _armed = true;
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentLineEffectDef>("independent_line_effects");
        public override void Compose(WorldComposer composer)
        {
            if (_cursorCorruption == null && composer.Content.All<IndependentLineEffectDef>().Any(effect => effect.Fault == "live-cursor"))
                throw new ContentException("live-cursor negative probe requires an explicit test mutation callback");
            var stamp = composer.Content.Get<ILineEffectDefinition>("segment.stamp");
            var finish = composer.Content.Get<ILineEffectDefinition>("segment.finish");
            ActualAliases = ReferenceEquals(stamp, composer.Content.Get<IWorkEffect>(stamp.Id)) &&
                ReferenceEquals(finish, composer.Content.Get<IWorkEffect>(finish.Id)) &&
                composer.Content.Owns(stamp) && composer.Content.Owns(finish);
            Independent = stamp is IndependentLineEffectDef && finish is IndependentLineEffectDef;
            composer.AddSystem(Initialize, 305);
            composer.AddSystem(new FaultInstaller(this), 307);
        }
        // Explicit negative observation injection only. No normal gameplay, state or scheduler is reproduced.
        private sealed class FaultInstaller : GameSystem
        {
            private readonly LineEffectContractsModule _module;
            public FaultInstaller(LineEffectContractsModule module) { _module = module; }
            public override string Id => "proof.line-effect-faults";
            public override int StateSchemaVersion => 1;
            public override void Update(World world, TickInfo time) { }
            public override void Initialize(World world)
            {
                foreach (var effect in world.Content.All<IndependentLineEffectDef>())
                    if (effect.Fault.StartsWith("live-", StringComparison.Ordinal))
                        effect.ObserveOperation = () =>
                        {
                            if (!_module._armed) return;
                            if (effect.Fault == "live-throw") throw new InvalidOperationException("proof.line-effect.live");
                            if (effect.Fault == "live-pause") ((GameClock)world.Clock).Pause();
                            Entity cursor = world.Entities.All.FirstOrDefault(e => e.Get<ILineWork>() != null);
                            if (cursor == null) throw new InvalidOperationException("negative probe requires actual work cursor");
                            if (effect.Fault == "live-touch") cursor.Touch();
                            if (effect.Fault == "live-cursor") _module._cursorCorruption(cursor.Get<LineWork>());
                        };
            }
        }
    }
}
