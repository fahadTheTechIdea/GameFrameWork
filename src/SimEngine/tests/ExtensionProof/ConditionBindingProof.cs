using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent metadata binds the original equipment runtime; no parallel value/exposure owner.
    public sealed class IndependentConditionDef : ComponentDef<EquipmentCondition>, IEquipmentConditionDefinition
    {
        private EquipmentConditionDeclaration _declaration;
        private string _fault;
        internal Action<string> Observe;
        public Code CapabilityId { get { Observe?.Invoke("condition.identity"); return _declaration.CapabilityId; } }
        public IUnitDefinition Unit { get { Observe?.Invoke("condition.unit"); return _fault == "unit" ? null : _declaration.Unit; } }
        public Fix Maximum { get { Observe?.Invoke("condition.maximum"); return _fault == "maximum" ? Fix.Zero : _declaration.Maximum; } }
        public Fix Initial => _fault == "initial" ? _declaration.Maximum + Fix.One : _declaration.Initial;
        public IConditionWearRule Wear { get { Observe?.Invoke("condition.wear"); return _declaration.Wear; } }
        public EntityStateBindings States { get { Observe?.Invoke("condition.states"); return _fault == "states" ? null : _declaration.States; } }
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => EquipmentConditionDeclaration.SignalBindings;
        protected override void ReadCore(JsonReader reader)
        { _declaration = EquipmentConditionDeclaration.Read(reader); _fault = reader.String("proof_fault", "none"); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    // Direct interface implementation, deliberately no native rule-definition inheritance.
    public sealed class IndependentWearDef : Def, IConditionWearRule
    {
        private ConditionWearDeclaration _declaration;
        internal Action<string> Probe;
        public IUnitDefinition Unit { get { Probe?.Invoke("wear.unit"); return _declaration.Unit; } }
        protected override void ReadCore(JsonReader reader) => _declaration = ConditionWearDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(World world, EntityDef owner) => _declaration.ValidateBinding(world, owner);
        public ConditionWearDecision Observe(ConditionWearContext context)
        { Probe?.Invoke("wear.observe"); return _declaration.Observe(context, Id); }
    }
    public sealed class ConditionBindingModule : GameModule
    {
        private World _world;
        public int Reads { get; private set; }
        public bool NativeBinding { get; private set; }
        public override string Id => "proof.condition-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires
            => new[] { new ModuleRequirement(EquipmentConditionModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentConditionDef>("independent_condition", 2);
            types.Section<IndependentWearDef>("independent_wear");
        }
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            var metadata = composer.Content.Get<EntityDef>("contract.factory").Component<IConditionDefinition>(new Code("integrity"));
            NativeBinding = metadata is IndependentConditionDef && metadata.ComponentType == typeof(EquipmentCondition)
                && ((IConditionWearDefinition)metadata).Wear is IndependentWearDef
                && ReferenceEquals(metadata.Unit, composer.Content.Get<IUnitContentDefinition>("unit.condition"));
        }
        public ConditionWearSnapshot ProbeDecision(out ConditionWearContext context)
        {
            foreach (var entity in _world.Entities.All)
                if (entity.Def.Id == "contract.factory")
                    return ConditionWearContracts.Read(_world, entity.Get<IConditionWearProvider>(new Code("integrity")), out context);
            throw new InvalidOperationException("missing actual condition probe");
        }
        public void Arm(string point, OperationBindingFault mode)
        {
            foreach (var entity in _world.Entities.All)
                if (entity.Def.Id == "contract.factory")
                {
                    var native = entity.Get<EquipmentCondition>(new Code("integrity"));
                    Action<string> observe = name =>
                    {
                        if (name != point || Reads != 0) return; Reads++;
                        if (mode == OperationBindingFault.Exception) throw new InvalidOperationException("proof.condition.observation");
                        if (mode == OperationBindingFault.PauseClock) ((GameClock)_world.Clock).Pause();
                        if (mode == OperationBindingFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("source"), new PlayerId(0));
                        if (mode == OperationBindingFault.HiddenState)
                        {
                            // Fault injection through the SAME public four-field codec, without Touch.
                            var condition = native.ReadCondition(); var exposure = native.ReadExposure();
                            using (var stream = new MemoryStream())
                            {
                                var writer = new StateWriter(stream);
                                Fix value = condition.Value; long changed = condition.ChangedTick == -1 ? 0 : condition.ChangedTick;
                                long elapsed = exposure.ElapsedTicks + 1, updated = _world.Tick;
                                writer.Fix(ref value); writer.Long(ref changed); writer.Long(ref elapsed); writer.Long(ref updated);
                                var bytes = stream.ToArray(); native.Sync(new StateReader(bytes, 0, bytes.Length));
                            }
                        }
                    };
                    ((IndependentConditionDef)entity.Def.Component<IConditionDefinition>(new Code("integrity"))).Observe = observe;
                    _world.Content.Get<IndependentWearDef>("wear.independent").Probe = observe;
                    return;
                }
            throw new InvalidOperationException("missing actual condition probe");
        }
    }
}
