using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentOperatingInputsDef : ComponentDef<OperatingInputs>, IStatefulOperatingInputsDefinition
    {
        public bool BindsEntityInventory => true;
        private OperatingInputsDeclaration _declaration;
        private string _fault;
        internal Action<string> Observe;
        public Code CapabilityId => _declaration.CapabilityId;
        public OperatingPhase Phase { get { Observe?.Invoke("phase"); return _fault == "phase" ? (OperatingPhase)255 : _declaration.Phase; } }
        public IReadOnlyList<IResourceRate> Inputs { get { Observe?.Invoke("inputs"); return _fault == "inputs" ? null : _declaration.Inputs; } }
        public EntityStateBindings States { get { Observe?.Invoke("states"); return _fault == "states" ? null : _declaration.States; } }
        public ICapacityPolicy CapacityPolicy => Owner.Component<IInventoryDefinition>().CapacityPolicy;
        public bool Accepts(ResolvedContent content, ResourceId resource)
        {
            foreach (var rate in Inputs) if (rate.Resource.Resource == resource) return Owner.Component<IInventoryDefinition>().Accepts(content, resource);
            return false;
        }
        protected override void ReadCore(JsonReader reader)
        { _declaration = OperatingInputsDeclaration.Read(reader); _fault = reader.String("proof_fault", "none"); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateBinding(owner);
    }
    // Deliberately incomplete metadata for the actual reusable runtime, used only for early refusal.
    public sealed class UndeclaredOperatingInputsDef : ComponentDef<OperatingInputs>, IOperatingInputsDefinition
    {
        public bool BindsEntityInventory => true;
        private OperatingInputsDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public OperatingPhase Phase => _declaration.Phase;
        public IReadOnlyList<IResourceRate> Inputs => _declaration.Inputs;
        public ICapacityPolicy CapacityPolicy => Owner.Component<IInventoryDefinition>().CapacityPolicy;
        public bool Accepts(ResolvedContent content, ResourceId resource) => Owner.Component<IInventoryDefinition>().Accepts(content, resource);
        protected override void ReadCore(JsonReader reader) => _declaration = OperatingInputsDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public enum OperatingInputsBindingFault { Exception, HiddenExposure, Touch, PauseClock, AllocateIdentity }
    public sealed class OperatingInputsBindingModule : GameModule
    {
        private World _world;
        public bool NativeBinding { get; private set; }
        public int Reads { get; private set; }
        public override string Id => "proof.operating-inputs-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentOperatingInputsDef>("independent_operating_inputs", 1);
            types.Component<UndeclaredOperatingInputsDef>("undeclared_operating_inputs", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            var definition = composer.Content.Get<EntityDef>("machine").Component<IOperatingInputsDefinition>(new Code("propulsion"));
            NativeBinding = definition is IndependentOperatingInputsDef && definition.ComponentType == typeof(OperatingInputs);
        }
        public void Arm(string point, OperatingInputsBindingFault fault)
        {
            foreach (var entity in _world.Entities.All)
                if (entity.Def.Id == "machine")
                {
                    var provider = entity.Get<IOperatingInputs>(new Code("propulsion"));
                    ((IndependentOperatingInputsDef)entity.Def.Component<IOperatingInputsDefinition>(new Code("propulsion"))).Observe = name => {
                        if (name != point || Reads != 0) return; Reads++;
                        if (fault == OperatingInputsBindingFault.Exception) throw new InvalidOperationException("proof.operating.observation");
                        if (fault == OperatingInputsBindingFault.Touch) entity.Touch();
                        if (fault == OperatingInputsBindingFault.PauseClock) ((GameClock)_world.Clock).Pause();
                        if (fault == OperatingInputsBindingFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("source"), new PlayerId(0));
                        if (fault == OperatingInputsBindingFault.HiddenExposure)
                        {
                            // Alter only the ORIGINAL two-field exposure codec, deliberately without Touch.
                            using (var stream = new MemoryStream())
                            {
                                var writer = new StateWriter(stream); long elapsed = 1, updated = _world.Tick;
                                writer.Long(ref elapsed); writer.Long(ref updated); writer.Flush();
                                var bytes = stream.ToArray(); ((Component)provider).Sync(new StateReader(bytes, 0, bytes.Length));
                            }
                        }
                    };
                    return;
                }
            throw new InvalidOperationException("missing actual exposure observation probe");
        }
    }
}
