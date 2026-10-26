using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent content only: stock, signal calculation and work remain in the engine.
    public sealed class IndependentInventorySignalDef : ComponentDef<InventorySignal>, IInventorySignalDefinition
    {
        private InventorySignalDeclaration _declaration;
        internal Action ObserveQuantities;
        public string Fault { get; private set; }
        public Code CapabilityId => _declaration.CapabilityId;
        public string InventoryKey => _declaration.InventoryKey;
        public IInventoryDefinition Inventory => Fault == "inventory-null" ? null : _declaration.Inventory;
        public Bill Quantities { get { ObserveQuantities?.Invoke(); return Fault == "quantities-empty" ? Bill.Empty : _declaration.Quantities; } }
        public int SignalSchemaVersion => Fault == "schema-version" ? 2 : 3;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => Fault == "bindings-null" ? null : InventorySignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader)
        { Fault = reader.String("metadata_fault"); _declaration = InventorySignalDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }
    public sealed class UnboundInventorySignalDef : ComponentDef<InventorySignal>, IAutomationSignalDefinition
    {
        private InventorySignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public int SignalSchemaVersion => 3;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => InventorySignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = InventorySignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class InventorySignalBindingsModule : ControlContractModule
    {
        public bool ActualBinding { get; private set; }
        public InventorySignalBindingsModule(InitializeProbe initialize) : base(initialize) { }
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InventorySignalsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Component<IndependentInventorySignalDef>("independent_inventory_signal", 1);
            types.Component<UnboundInventorySignalDef>("unbound_inventory_signal", 1);
            types.Component<ExposurePrivateProbeDef>("inventory_signal_private_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var binding = composer.Content.Get<EntityDef>("field.plant").Component<IInventorySignalDefinition>(new Code("feed"));
            ActualBinding = binding is IndependentInventorySignalDef && binding.ComponentType == typeof(InventorySignal)
                && binding.InventoryKey == "storage" && binding.Inventory != null;
            composer.AddSystem(new ProbeInstaller(), 303);
        }
        private sealed class ProbeInstaller : GameSystem
        {
            public override string Id => "proof.inventory-signal-bindings";
            public override int StateSchemaVersion => 1;
            public override void Update(World world, TickInfo time) { } // No gameplay, only negative observation installation.
            public override void Initialize(World world)
            {
                foreach (var owner in world.Content.All<EntityDef>())
                    foreach (var part in owner.Components)
                        if (part is IndependentInventorySignalDef definition && definition.Fault.StartsWith("live-", StringComparison.Ordinal))
                            definition.ObserveQuantities = () => {
                                Entity source = null;
                                foreach (var entity in world.Entities.All) if (ReferenceEquals(entity.Def, owner)) { source = entity; break; }
                                if (source == null || world.Clock.StepIndex == 0) return;
                                if (definition.Fault == "live-pause" || definition.Fault == "live-pause-throw") ((GameClock)world.Clock).Pause();
                                if (definition.Fault == "live-touch" || definition.Fault == "live-touch-throw") source.Touch();
                                if (definition.Fault == "live-private" || definition.Fault == "live-private-throw") source.Get<ExposurePrivateProbe>().Corrupt();
                                if (definition.Fault == "live-throw" || definition.Fault.EndsWith("-throw", StringComparison.Ordinal))
                                    throw new InvalidOperationException("proof.inventory-signal.quantities");
                            };
            }
        }
    }
}
