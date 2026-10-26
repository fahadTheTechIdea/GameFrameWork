using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Native JSON registration; independent definitions use the same declaration and runtime.</summary>
    public sealed class InventorySignalDef : ComponentDef<InventorySignal>, IInventorySignalDefinition
    {
        private InventorySignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public string InventoryKey => _declaration.InventoryKey;
        public IInventoryDefinition Inventory => _declaration.Inventory;
        public Bill Quantities => _declaration.Quantities;
        public int SignalSchemaVersion => 3;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => InventorySignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = InventorySignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }

    /// <summary>Stateless facts only. No balance, reservation, cached readiness, timer or control state.</summary>
    public sealed class InventorySignal : Component, IAutomationSignalSource
    {
        public static readonly Code CapacityUnavailable = new Code("inventory.signal-capacity-unavailable"),
            NoThreshold = new Code("inventory.signal-no-threshold");
        private IInventorySignalDefinition Binding => base.Def as IInventorySignalDefinition
            ?? throw new InvalidOperationException("inventory signal requires its actual metadata contract");
        public Code CapabilityId => Binding.CapabilityId;
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            if (world == null || signal == null || Entity == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                !ReferenceEquals(Entity.Get<IAutomationSignalSource>(CapabilityId), this) ||
                !ReferenceEquals(world.Content.Find<IAutomationSignalContent>(signal?.Id), signal))
                throw new ArgumentException("inventory signal requires its actual live session and authored signal");
            return InventorySignalBindingContracts.Observe(world, Binding, Entity, () => Read(world, signal, Binding));
        }
        private AutomationSignalSnapshot Read(World world, IAutomationSignalContent signal, IInventorySignalDefinition definition)
        {
            IInventory inventory = Entity.Get<IInventory>();
            if (inventory == null || !ReferenceEquals(inventory.Entity, Entity) ||
                !ReferenceEquals(Entity.Def.Component<IInventoryDefinition>(), definition.Inventory) ||
                inventory.Endpoint != ResourceEndpointId.ForEntity(Entity.Id) || inventory.Company != Entity.Owner ||
                inventory.Version < 0 || !ReferenceEquals(inventory.CapacityPolicy, definition.Inventory.CapacityPolicy))
                throw new InvalidOperationException("inventory signal lost its actual bound inventory identity/policy");
            int version = inventory.Version;
            var value = EntityObservation.ReadProvider(world, inventory, Entity, () => Measure(world, signal, inventory, definition));
            if (inventory.Version != version || inventory.Endpoint != ResourceEndpointId.ForEntity(Entity.Id) ||
                inventory.Company != Entity.Owner || !ReferenceEquals(Entity.Get<IInventory>(), inventory) ||
                !ReferenceEquals(inventory.CapacityPolicy, definition.Inventory.CapacityPolicy))
                throw new InvalidOperationException("inventory mutated a pure signal observation");
            return value;
        }
        private AutomationSignalSnapshot Measure(World world, IAutomationSignalContent signal, IInventory inventory, IInventorySignalDefinition definition)
        {
            if (signal is IResourceAutomationSignalDefinition quantity) return MeasureQuantity(world, signal, quantity, inventory);
            if (definition.Quantities == null) return signal.Unavailable(NoThreshold);
            foreach (ResourceAmount line in definition.Quantities.Lines)
                if (!inventory.Accepts(world.Content, line.Resource)) return signal.Unavailable(ResourceSignalCodes.Incompatible);
            if (signal.Metric.Id == "inventory.incoming-ready")
            {
                CapacityCheck capacity = inventory.CheckIncoming(definition.Quantities.Lines);
                if (!Enum.IsDefined(typeof(CapacityStatus), capacity.Status))
                    throw new InvalidOperationException("inventory returned an unknown capacity status");
                return capacity.Status == CapacityStatus.Fits ? signal.Boolean(true)
                    : capacity.Status == CapacityStatus.LimitExceeded ? signal.Boolean(false)
                    : signal.Unavailable(CapacityUnavailable);
            }
            bool available = signal.Metric.Id == "inventory.available-ready";
            if (!available && signal.Metric.Id != "inventory.stock-ready") return signal.Unavailable(OperationCodes.SignalUnavailable);
            bool ready = true;
            foreach (ResourceAmount line in definition.Quantities.Lines)
            {
                Fix amount = inventory.Amount(line.Resource);
                Fix observed = available ? inventory.Available(line.Resource) : amount;
                IResourceDefinition resource = world.Content.Resource(line.Resource);
                CapacityQuantum quantum = CapacityPolicyContracts.Quantum(inventory.CapacityPolicy, line.Resource);
                ValidateQuantity(resource, quantum, amount); ValidateQuantity(resource, quantum, observed);
                if (observed > amount) throw new InvalidOperationException("inventory returned inconsistent available stock");
                ready &= observed >= line.Amount;
            }
            return signal.Boolean(ready);
        }
        private static void ValidateQuantity(IResourceDefinition resource, CapacityQuantum quantum, Fix amount)
            => ResourceQuantitySignals.ValidateQuantity(resource, quantum, amount);
        private static AutomationSignalSnapshot MeasureQuantity(World world, IAutomationSignalContent signal,
            IResourceAutomationSignalDefinition quantity, IInventory inventory)
        {
            ResourceQuantitySignals.Definition(world, signal);
            Code refusal = ResourceQuantitySignals.Capture(world, inventory, quantity.Resource.Resource, out var facts);
            return refusal.IsNone ? ResourceQuantitySignals.Read(world, signal, facts, "inventory") : signal.Unavailable(refusal);
        }
    }
}
