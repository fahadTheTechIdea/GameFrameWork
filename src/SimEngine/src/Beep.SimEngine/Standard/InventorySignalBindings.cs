using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable metadata for the existing stateless inventory signal component.</summary>
    public interface IInventorySignalDefinition : IAutomationSignalDefinition
    {
        string InventoryKey { get; }
        IInventoryDefinition Inventory { get; }
        /// <summary>Explicit null selects numeric-only facts; no threshold is inferred.</summary>
        Bill Quantities { get; }
    }

    /// <summary>Sole JSON declaration/resolver. Does not own stock, promises or cached signals.</summary>
    public sealed class InventorySignalDeclaration
    {
        private readonly BillSpec _quantities;
        private ResolvedContent _content;
        public Code CapabilityId { get; }
        public string InventoryKey { get; }
        public IInventoryDefinition Inventory { get; private set; }
        public Bill Quantities { get; private set; }
        public static IReadOnlyList<AutomationMetricSchema> Metrics { get; } = Array.AsReadOnly(new[]
        {
            new AutomationMetricSchema(new Code("inventory.stock-ready"), AutomationSignalKind.Boolean, new Code("unit.boolean")),
            new AutomationMetricSchema(new Code("inventory.available-ready"), AutomationSignalKind.Boolean, new Code("unit.boolean")),
            new AutomationMetricSchema(new Code("inventory.incoming-ready"), AutomationSignalKind.Boolean, new Code("unit.boolean")),
            AutomationMetricSchema.Quantity(new Code("inventory.stored")),
            AutomationMetricSchema.Quantity(new Code("inventory.available")),
            AutomationMetricSchema.Quantity(new Code("inventory.inbound")),
            AutomationMetricSchema.Quantity(new Code("inventory.outbound")),
            AutomationMetricSchema.Quantity(new Code("inventory.receiving-room"))
        });
        private InventorySignalDeclaration(JsonReader reader)
        {
            string id = reader.String("id"); InventoryKey = reader.String("inventory");
            try
            {
                ContractIdentity.ValidateId(id, "inventory signal");
                ContractIdentity.ValidateId(InventoryKey, "inventory component key");
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id);
            JsonValue quantities = reader.Value("quantities");
            if (quantities == null) throw reader.Error("quantities requires a nonempty native-resource bill or explicit null for numeric-only signals");
            if (quantities.Kind != JsonKind.Null)
            {
                _quantities = BillSpec.Read(reader, "quantities");
                if (_quantities.IsEmpty) throw reader.Error("inventory readiness signals require a nonempty native-resource bill");
            }
        }
        public static InventorySignalDeclaration Read(JsonReader reader)
            => new InventorySignalDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (_content != null) throw new InvalidOperationException("inventory signal declaration resolves once");
            _content = content.Content; Quantities = _quantities?.Resolve(content);
        }
        public void ValidateOwner(EntityDef owner)
        {
            Inventory = owner.Component<IInventoryDefinition>();
            InventorySignalBindingContracts.ValidateInventory(_content, owner, InventoryKey, Inventory, Quantities);
        }
    }

    /// <summary>Actual immutable aliases and original scoped observations; never another state owner.</summary>
    public static class InventorySignalBindingContracts
    {
        internal static void ValidateInventory(ResolvedContent content, EntityDef owner, string key,
            IInventoryDefinition inventory, Bill quantities)
        {
            if (content == null || !(inventory is ComponentDef part) || !ReferenceEquals(part.Owner, owner) ||
                !ReferenceEquals(owner.Component<IInventoryDefinition>(), inventory) || part.Key != key ||
                !typeof(IInventory).IsAssignableFrom(part.ComponentType) || !(inventory.CapacityPolicy is ICapacityPolicyDefinition policy) ||
                !ReferenceEquals(content.Find<ICapacityPolicyDefinition>(policy.Id), policy))
                throw new ContentException(owner.Id + ": inventory signal requires its exact singular inventory and authored capacity policy");
            if (quantities == null) return;
            if (quantities.IsEmpty) throw new ContentException(owner.Id + ": readiness threshold must be nonempty or explicitly numeric-only");
            foreach (var line in quantities.Lines)
            {
                var resource = content.Resource(line.Resource);
                if (resource == null || line.Amount <= Fix.Zero || !inventory.Accepts(content, line.Resource) ||
                    UnitConversion.ValidateAmount(resource.QuantityUnit, line.Amount) != UnitConversionStatus.Converted ||
                    CapacityPolicyContracts.Quantum(policy, line.Resource).Floor(line.Amount) != line.Amount)
                    throw new ContentException(owner.Id + ": inventory signal threshold violates resource compatibility or native capacity quantum");
            }
            if (!CapacityPolicyContracts.Check(policy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), quantities.Lines).Fits)
                throw new ContentException(owner.Id + ": inventory signal bill exceeds its actual empty capacity policy");
        }
        private static void ValidateCore(World world, IInventorySignalDefinition definition, Entity entity)
        {
            var part = AutomationSignalContracts.ValidateReusable(world, definition, typeof(InventorySignal), 1, 3,
                InventorySignalDeclaration.Metrics, entity);
            ValidateInventory(world.Content, part.Owner, definition.InventoryKey, definition.Inventory, definition.Quantities);
        }
        internal static T Observe<T>(World world, IInventorySignalDefinition definition, Entity entity, Func<T> read)
            => EntityObservation.ReadProvider(world, definition, entity, () => {
                ValidateCore(world, definition, entity); return read();
            }, observeTargetState: true, faultPhase: "inventory.signal-metadata");
        public static void ValidateBinding(World world, IInventorySignalDefinition definition)
            => Observe(world, definition, null, () => true);
        internal static void ValidateComposition(World world)
        {
            foreach (var owner in world.Content.All<EntityDef>())
                foreach (var part in owner.Components)
                {
                    bool runtime = part.ComponentType == typeof(InventorySignal);
                    if (runtime != (part is IInventorySignalDefinition))
                        throw new ContentException(owner.Id + ": original inventory signal requires matching metadata contract");
                    if (runtime) ValidateBinding(world, (IInventorySignalDefinition)part);
                }
        }
    }
}
