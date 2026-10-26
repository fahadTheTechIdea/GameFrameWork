using System;
using System.Collections.Generic;

namespace Beep.SimEngine
{
    /// <summary>Actual authored admission basis and physical instance count, never another stock owner.</summary>
    public interface IInstancedCapacityPolicy : ICapacityPolicy
    {
        ICapacityPolicyDefinition Definition { get; }
        long Instances { get; }
    }

    /// <summary>The authored evaluator owns arithmetic; this supplies actual or explicitly required membership.</summary>
    public sealed class InstancedCapacityPolicy : IInstancedCapacityPolicy
    {
        public ICapacityPolicyDefinition Definition { get; }
        public long Instances { get; }
        public InstancedCapacityPolicy(ICapacityPolicyDefinition definition, long instances)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (instances <= 0) throw new ArgumentOutOfRangeException(nameof(instances));
            Instances = instances;
        }
        public string Id => Definition.Id;
        public IReadOnlyList<CapacityLimit> Limits => Definition.Limits;
        public CapacityQuantum Quantum(ResourceId resource) => Definition.Quantum(resource);
        public CapacityCheck Check(IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> promised,
            IReadOnlyList<ResourceAmount> incoming, long instances = 1)
            => Definition.Check(stock, promised, incoming, checked(Instances * instances));
        public CapacityOffer Offer(IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> promised,
            ResourceId resource, Fix ceiling, long instances = 1)
            => Definition.Offer(stock, promised, resource, ceiling, checked(Instances * instances));
    }

    public static class CapacityPolicyContracts
    {
        private static IReadOnlyList<ResourceAmount> Frozen(IReadOnlyList<ResourceAmount> values)
            => new List<ResourceAmount>(values ?? throw new ArgumentNullException(nameof(values))).AsReadOnly();

        /// <summary>One stateless callback boundary; callers retain their sole stock/promise ownership.</summary>
        public static CapacityCheck Check(ICapacityPolicy policy, IReadOnlyList<ResourceAmount> stock,
            IReadOnlyList<ResourceAmount> promised, IReadOnlyList<ResourceAmount> incoming, long instances = 1)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            CapacityCheck check = policy.Check(Frozen(stock), Frozen(promised), Frozen(incoming), instances);
            if (!Enum.IsDefined(typeof(CapacityStatus), check.Status))
                throw new InvalidOperationException("capacity policy returned an invalid check status");
            return check;
        }

        public static CapacityQuantum Quantum(ICapacityPolicy policy, ResourceId resource)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            CapacityQuantum quantum = policy.Quantum(resource);
            if (!Enum.IsDefined(typeof(CapacityStatus), quantum.Status) ||
                (quantum.Status == CapacityStatus.Fits ? quantum.Raw <= 0 : quantum.Raw != 0))
                throw new InvalidOperationException("capacity policy returned an invalid native quantum");
            return quantum;
        }

        public static CapacityOffer Offer(ICapacityPolicy policy, IReadOnlyList<ResourceAmount> stock,
            IReadOnlyList<ResourceAmount> promised, ResourceId resource, Fix ceiling, long instances = 1)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            var held = Frozen(stock); var pending = Frozen(promised);
            CapacityOffer offer = policy.Offer(held, pending, resource, ceiling, instances);
            if (!Enum.IsDefined(typeof(CapacityStatus), offer.Status) || offer.Amount < Fix.Zero ||
                offer.Amount > Fix.Max(Fix.Zero, ceiling) || offer.Status != CapacityStatus.Fits && !offer.Amount.IsZero)
                throw new InvalidOperationException("capacity policy returned an invalid bounded native offer");
            if (offer.Amount.IsPositive && (Quantum(policy, resource).Floor(offer.Amount) != offer.Amount ||
                !Check(policy, held, pending, new[] { new ResourceAmount(resource, offer.Amount) }, instances).Fits))
                throw new InvalidOperationException("capacity policy returned an invalid bounded native offer");
            return offer;
        }

        /// <summary>Static packing stays exact; instanced packing shares the actual authored basis with enough physical instances.</summary>
        public static bool Satisfies(ICapacityPolicy actual, ICapacityPolicy required)
        {
            if (actual == null || required == null) return false;
            if (ReferenceEquals(actual, required)) return true;
            return actual is IInstancedCapacityPolicy supplied && required is IInstancedCapacityPolicy declared &&
                supplied.Definition != null && ReferenceEquals(supplied.Definition, declared.Definition) &&
                declared.Instances > 0 && supplied.Instances >= declared.Instances;
        }
    }
}
