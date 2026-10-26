using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public static class ResourceSignalCodes
    {
        public static readonly Code Incompatible = new Code("resource.signal-incompatible"),
            QuantityUnavailable = new Code("resource.signal-quantity-unavailable"),
            CapacityUnavailable = new Code("resource.signal-capacity-unavailable");
    }
    /// <summary>Detached native quantities from an existing stock and reservation owner.</summary>
    public readonly struct ResourceQuantitySnapshot
    {
        public ResourceId Resource { get; }
        public Fix Stored { get; }
        public Fix Available { get; }
        public Fix Inbound { get; }
        public Fix Outbound { get; }
        public CapacityOffer Receiving { get; }
        public ResourceQuantitySnapshot(ResourceId resource, Fix stored, Fix available, Fix inbound, Fix outbound,
            CapacityOffer receiving)
        {
            if (!resource.IsValid || stored < Fix.Zero || available < Fix.Zero || inbound < Fix.Zero || outbound < Fix.Zero ||
                outbound > stored || available != stored - outbound || !Enum.IsDefined(typeof(CapacityStatus), receiving.Status) ||
                receiving.Amount < Fix.Zero || receiving.Status != CapacityStatus.Fits && !receiving.Amount.IsZero)
                throw new ArgumentException("quantity facts require nonnegative coherent stock and promises");
            Resource = resource; Stored = stored; Available = available; Inbound = inbound; Outbound = outbound; Receiving = receiving;
        }
    }

    internal static class ResourceQuantitySignals
    {
        internal static void ValidateQuantity(IResourceDefinition resource, CapacityQuantum quantum, Fix amount)
        {
            if (quantum.Status != CapacityStatus.Fits || quantum.Raw <= 0 || amount < Fix.Zero ||
                UnitConversion.ValidateAmount(resource.QuantityUnit, amount) != UnitConversionStatus.Converted ||
                amount.IsPositive && quantum.Floor(amount) != amount)
                throw new InvalidOperationException("endpoint returned malformed native signal quantities");
        }
        internal static Code Capture(World world, IResourceOutlet endpoint, ResourceId id, out ResourceQuantitySnapshot facts)
        {
            facts = default;
            IResourceDefinition resource = world.Content.Resource(id);
            if (resource == null) return Codes.UnknownDef;
            int version = endpoint.Version; var identity = endpoint.Endpoint; var company = endpoint.Company;
            var policy = endpoint.CapacityPolicy;
            if (version < 0 || !identity.IsValid || policy == null) throw new InvalidOperationException("invalid signal endpoint identity/policy");
            bool accepts = endpoint.Accepts(world.Content, id);
            if (accepts)
            {
                CapacityQuantum quantum = CapacityPolicyContracts.Quantum(policy, id);
                Fix stored = endpoint.Amount(id), available = endpoint.Available(id),
                    inbound = endpoint.ReservedIn(id), outbound = endpoint.ReservedOut(id);
                ValidateQuantity(resource, quantum, stored); ValidateQuantity(resource, quantum, available);
                ValidateQuantity(resource, quantum, inbound); ValidateQuantity(resource, quantum, outbound);
                if (outbound > stored || available != stored - outbound)
                    throw new InvalidOperationException("endpoint returned inconsistent stock and outbound promises");
                CapacityCheck current = endpoint.CheckIncoming(Array.Empty<ResourceAmount>());
                if (!Enum.IsDefined(typeof(CapacityStatus), current.Status))
                    throw new InvalidOperationException("endpoint returned unknown receiving capacity status");
                var receiving = new CapacityOffer(current.Status);
                if (current.Fits)
                {
                    Fix ceiling = quantum.Floor(Fix.FromRaw(long.MaxValue));
                    Fix room = endpoint.Offer(id, ceiling);
                    ValidateQuantity(resource, quantum, room);
                    if (room > ceiling || room.IsPositive && !endpoint.CheckIncoming(new[] { new ResourceAmount(id, room) }).Fits)
                        throw new InvalidOperationException("endpoint returned unadmitted receiving room");
                    receiving = new CapacityOffer(CapacityStatus.Fits, room);
                }
                facts = new ResourceQuantitySnapshot(id, stored, available, inbound, outbound, receiving);
            }
            if (endpoint.Version != version || endpoint.Endpoint != identity || endpoint.Company != company ||
                !ReferenceEquals(endpoint.CapacityPolicy, policy))
                throw new InvalidOperationException("endpoint mutated a pure quantity observation");
            return accepts ? Code.None : ResourceSignalCodes.Incompatible;
        }
        internal static IResourceAutomationSignalDefinition Definition(World world, IAutomationSignalContent signal)
        {
            var quantity = signal as IResourceAutomationSignalDefinition;
            if (quantity?.Resource == null || !ReferenceEquals(world.Content.Resource(quantity.Resource.Resource), quantity.Resource) ||
                quantity.QuantityUnit == null || !ReferenceEquals(world.Content.Get<IUnitContentDefinition>(quantity.QuantityUnit.Unit.Id), quantity.QuantityUnit) ||
                new Code(quantity.QuantityUnit.Unit.Id) != signal.Unit)
                throw new InvalidOperationException("resource signal lost its actual catalog resource/unit metadata");
            return quantity;
        }
        internal static AutomationSignalSnapshot Read(World world, IAutomationSignalContent signal, ResourceQuantitySnapshot facts, string prefix)
        {
            var quantity = Definition(world, signal);
            if (facts.Resource != quantity.Resource.Resource) throw new InvalidOperationException("quantity observation belongs to another resource");
            var unit = quantity.Resource.QuantityUnit;
            if (UnitConversion.ValidateAmount(unit, facts.Stored) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(unit, facts.Available) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(unit, facts.Inbound) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(unit, facts.Outbound) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(unit, facts.Receiving.Amount) != UnitConversionStatus.Converted)
                throw new InvalidOperationException("resource diagnostic returned an invalid native quantity");
            Fix native;
            string metric = signal.Metric.Id;
            if (metric == prefix + ".stored") native = facts.Stored;
            else if (metric == prefix + ".available") native = facts.Available;
            else if (metric == prefix + ".inbound") native = facts.Inbound;
            else if (metric == prefix + ".outbound") native = facts.Outbound;
            else if (metric == prefix + ".receiving-room")
            {
                if (facts.Receiving.Status != CapacityStatus.Fits && facts.Receiving.Status != CapacityStatus.LimitExceeded)
                    return signal.Unavailable(ResourceSignalCodes.CapacityUnavailable);
                native = facts.Receiving.Amount;
            }
            else return signal.Unavailable(OperationCodes.SignalUnavailable);
            return ReadNative(world, signal, native);
        }
        internal static AutomationSignalSnapshot ReadNative(World world, IAutomationSignalContent signal, Fix native)
        {
            var quantity = Definition(world, signal);
            UnitConversionResult converted = UnitConversion.Convert(native, quantity.Resource.QuantityUnit, quantity.QuantityUnit);
            return converted.Status == UnitConversionStatus.Converted ? signal.Number(converted.Amount, signal.Unit)
                : signal.Unavailable(converted.Status == UnitConversionStatus.OutOfRange
                    ? OperationCodes.SignalNumericRange : ResourceSignalCodes.QuantityUnavailable);
        }
    }
}
