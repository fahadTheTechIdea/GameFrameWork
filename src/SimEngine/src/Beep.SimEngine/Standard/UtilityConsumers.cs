using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Resource-specific named bindings to actual stores; no utility inventory or consumption loop.</summary>
    public interface IUtilityConsumer : INamedEntityCapability
    {
        IResourcePort ResolveUtilityPort(World world, ResourceId resource);
        UtilityDemandSnapshot ReadUtilityDemand(World world);
    }
    public static class UtilityCodes
    {
        public static readonly Code Inactive = new Code("utility.inactive"), Shortage = new Code("utility.shortage");
    }
    /// <summary>Detached native per-resource sources and next/consumed quantities from the existing execution owner.</summary>
    public readonly struct UtilityDemandSnapshot
    {
        public static UtilityDemandSnapshot None => new UtilityDemandSnapshot(Code.None, 0,
            Array.Empty<ResourcePortSnapshot>(), false, Bill.Empty, Bill.Empty, Bill.Empty, Code.None);
        public Code Consumer { get; }
        public IReadOnlyList<ResourcePortSnapshot> Ports { get; }
        public long ElapsedTicks { get; }
        public IReadOnlyList<ResourceId> Resources { get; }
        public bool Drawing { get; }
        public Bill Consumed { get; }
        public Bill NextInputs { get; }
        public Bill Available { get; }
        public Code NextBlocker { get; }
        public bool Satisfied => Blocker.IsNone;
        public Code Blocker
        {
            get
            {
                if (!Drawing) return UtilityCodes.Inactive;
                if (!NextBlocker.IsNone) return NextBlocker;
                foreach (ResourceAmount line in NextInputs.Lines)
                    if (Available.AmountOf(line.Resource) != line.Amount) return UtilityCodes.Shortage;
                return Code.None;
            }
        }
        public UtilityDemandSnapshot(Code consumer, long elapsedTicks, IEnumerable<ResourcePortSnapshot> ports,
            bool drawing, Bill consumed, Bill nextInputs, Bill available, Code nextBlocker)
        {
            if (ports == null || consumed == null || nextInputs == null || available == null) throw new ArgumentNullException();
            if (elapsedTicks < 0) throw new ArgumentException("utility facts require nonnegative exposure");
            var bindings = new List<ResourcePortSnapshot>(ports); bindings.Sort((a,b) => a.Resource.CompareTo(b.Resource));
            var ids = new List<ResourceId>();
            foreach (var binding in bindings)
            {
                if (!binding.Resource.IsValid || binding.Port.IsNone || ids.Contains(binding.Resource)||
                    binding.Blocker.IsNone&&!binding.Ready||!binding.Blocker.IsNone&&(binding.Stock!=null||binding.Version!=-1))
                    throw new ArgumentException("utility facts require unique native resource bindings");
                ids.Add(binding.Resource);
            }
            foreach (Bill bill in new[] { consumed, nextInputs, available }) foreach (ResourceAmount line in bill.Lines)
                if (!ids.Contains(line.Resource)) throw new ArgumentException("utility quantity is not in its declared ports");
            foreach (ResourceAmount line in available.Lines)
                if (line.Amount > nextInputs.AmountOf(line.Resource)) throw new ArgumentException("available utility contribution exceeds current demand");
            if (drawing && (consumer.IsNone || ids.Count == 0)) throw new ArgumentException("drawing utility requires actual owner/ports");
            Consumer = consumer; ElapsedTicks = elapsedTicks; Ports = bindings.AsReadOnly(); Resources = ids.AsReadOnly();
            Drawing = drawing; Consumed = consumed; NextInputs = nextInputs; Available = available; NextBlocker = nextBlocker;
        }
    }
    public static class UtilityContracts
    {
        internal static bool CanDisclose(World world, PlayerId viewer, UtilityDemandSnapshot demand)
        {
            world.RequireHealthyUpdate(); var view = world.ViewFor(viewer);
            foreach (ResourcePortSnapshot port in demand.Ports)
                if (!view.CanSeeContent(world.Content.Resource(port.Resource)) ||
                    port.Stock != null && !view.CanSeeResources(port.Stock.Lines)) return false;
            return view.CanSeeResources(demand.Consumed.Lines) && view.CanSeeResources(demand.NextInputs.Lines)
                && view.CanSeeResources(demand.Available.Lines);
        }

        internal static IReadOnlyList<ResourcePortBinding> Bindings(World world, IUtilityConsumer provider, IReadOnlyList<ResourceId> resources)
        {
            return EntityObservation.ReadProvider(world, provider, null, () =>
            {
                Entity entity = provider.Entity;
                var result = new List<ResourcePortBinding>();
                foreach (var resource in resources)
                {
                    IResourcePort port = provider.ResolveUtilityPort(world, resource);
                    result.Add(ResourcePortContracts.Bind(world, entity, resource, port));
                }
                return result.AsReadOnly();
            }, observeTargetState: true);
        }
        /// <summary>Pure actual-provider demand read. Withholds facts if its existing codec or scoped session changes.</summary>
        public static UtilityDemandSnapshot Read(World world, IUtilityConsumer provider)
        {
            UtilityDemandSnapshot ReadDemand()
            {
                Entity entity = provider.Entity;
                Code identity = provider.CapabilityId;
                if (entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                    identity.IsNone || !ReferenceEquals(entity.Get<IUtilityConsumer>(identity), provider))
                    throw new InvalidOperationException("utility consumer must be the actual named live provider");
                var facts = provider.ReadUtilityDemand(world); Validate(world, provider, facts);
                if (!ReferenceEquals(provider.Entity, entity) || provider.CapabilityId != identity ||
                    !ReferenceEquals(entity.Get<IUtilityConsumer>(identity), provider))
                    throw new InvalidOperationException("utility consumer mutated its identity during observation");
                return facts;
            }
            return EntityObservation.ReadProvider(world, provider, null, ReadDemand, world.Space, observeTargetState: true);
        }
        internal static void Validate(World world, IUtilityConsumer provider, UtilityDemandSnapshot facts)
        {
            if (facts.Consumer != provider.CapabilityId || facts.Ports == null || facts.Resources == null ||
                facts.Consumed == null || facts.NextInputs == null || facts.Available == null ||
                facts.ElapsedTicks > world.Tick && facts.ElapsedTicks - world.Tick > 1)
                throw new InvalidOperationException("utility consumer returned malformed owner/resource facts");
            var bindings = Bindings(world, provider, facts.Resources);
            ResourcePortContracts.ValidateFacts(facts.Ports, ResourcePortContracts.Facts(bindings));
            foreach (var binding in bindings)
            {
                ResourceId resource = binding.Route.Resource;
                if(!binding.Ready) {
                    if(facts.Available.AmountOf(resource).IsPositive||facts.NextBlocker!=ResourcePortContracts.Blocker(bindings))
                        throw new InvalidOperationException("unavailable utility port reported available stock or lost its blocker");
                    foreach(Bill bill in new[]{facts.Consumed,facts.NextInputs,facts.Available})
                        ResourceQuantitySignals.ValidateQuantity(world.Content.Resource(resource),CapacityPolicyContracts.Quantum(binding.Policy, resource),bill.AmountOf(resource));
                    continue;
                }
                if (!ResourceQuantitySignals.Capture(world, binding.Store, resource, out var quantity).IsNone)
                    throw new InvalidOperationException("utility resource lost actual endpoint compatibility");
                var definition = world.Content.Resource(resource); CapacityQuantum quantum = CapacityPolicyContracts.Quantum(binding.Store.CapacityPolicy, resource);
                foreach (Bill bill in new[] { facts.Consumed, facts.NextInputs, facts.Available })
                    ResourceQuantitySignals.ValidateQuantity(definition, quantum, bill.AmountOf(resource));
                if (facts.Available.AmountOf(resource) != Fix.Min(facts.NextInputs.AmountOf(resource), quantity.Available))
                    throw new InvalidOperationException("utility availability differs from actual selected unpromised stock");
                ResourcePortContracts.Verify(bindings);
            }
        }
    }
}
