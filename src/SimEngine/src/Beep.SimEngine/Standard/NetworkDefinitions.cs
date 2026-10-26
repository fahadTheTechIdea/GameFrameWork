using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable authored network metadata. The existing line service owns its live topology and stock.</summary>
    public interface INetworkDefinition : IContentDefinition
    {
        ILineDefinition Line { get; }
        IReadOnlyList<ResourceId> Resources { get; }
        ICapacityPolicyDefinition CapacityPolicy { get; }
        bool Public { get; }
        IReadOnlyList<IResourceRate> Rates { get; }
        INetworkFlowPolicy FlowPolicy { get; }
        INetworkSolver Solver { get; }
        INetworkDemandPolicy DemandPolicy { get; }
        bool Carries(ResourceId resource);
        IResourceRate Rate(ResourceId resource);
    }

    /// <summary>Sole strict network authoring and resource-selection implementation, not another content/state owner.</summary>
    public sealed class NetworkDeclaration
    {
        private readonly string _line, _capacity, _flow, _solver, _demand;
        private readonly ResourceRateSpec _rates;
        public ILineDefinition Line { get; private set; }
        public IReadOnlyList<ResourceId> Resources { get; private set; }
        public ICapacityPolicyDefinition CapacityPolicy { get; private set; }
        public bool Public { get; }
        public IReadOnlyList<IResourceRate> Rates { get; private set; }
        public INetworkFlowPolicy FlowPolicy { get; private set; }
        public INetworkSolver Solver { get; private set; }
        public INetworkDemandPolicy DemandPolicy { get; private set; }
        private NetworkDeclaration(JsonReader reader)
        {
            _line = reader.String("line"); _capacity = reader.String("capacity_policy");
            _flow = reader.String("flow_policy"); _solver = reader.String("solver"); _demand = reader.String("demand_policy");
            try
            {
                foreach (string id in new[] { _line, _capacity, _flow, _solver, _demand })
                    ContractIdentity.ValidateId(id, "network reference");
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            _rates = ResourceRateSpec.Read(reader, "rates"); Public = reader.Bool("public", false);
        }
        public static NetworkDeclaration Read(JsonReader reader)
            => new NetworkDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Line != null) throw new InvalidOperationException("network metadata resolves once");
            Line = content.Def<ILineDefinition>(_line);
            CapacityPolicy = content.Def<ICapacityPolicyDefinition>(_capacity);
            FlowPolicy = content.Def<INetworkFlowPolicy>(_flow);
            Solver = content.Def<INetworkSolver>(_solver);
            DemandPolicy = content.Def<INetworkDemandPolicy>(_demand);
            Rates = _rates.Resolve(content);
            var resources = new ResourceId[Rates.Count];
            for (int i = 0; i < resources.Length; i++) resources[i] = Rates[i].Resource.Resource;
            Resources = Array.AsReadOnly(resources);
        }
        public bool Carries(ResourceId resource)
        { foreach (ResourceId carried in Resources) if (carried == resource) return true; return false; }
        public IResourceRate Rate(ResourceId resource)
        { foreach (IResourceRate rate in Rates) if (rate.Resource.Resource == resource) return rate; return null; }
    }

    /// <summary>Shared actual metadata admission. No snapshot catalog, pool, topology or gameplay state.</summary>
    public static class NetworkDefinitionContracts
    {
        private static void RequireAlias<T>(T definition, ContentResolver content) where T : class, IContentDefinition
        {
            if (!(definition is Def actual) || string.IsNullOrWhiteSpace(actual.Id) || string.IsNullOrWhiteSpace(actual.Module) ||
                !content.Content.Owns(definition) || definition.Id != actual.Id ||
                definition.Module != actual.Module || !ReferenceEquals(content.Content.Find<T>(actual.Id), definition))
                throw content.Fail("network requires its actual catalog " + typeof(T).Name + " alias");
        }
        public static void Validate(INetworkDefinition definition, ContentResolver content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            RequireAlias(definition, content);
            _ = definition.Public; // Observe every authored property, including unused access metadata.
            var line = definition.Line; var capacity = definition.CapacityPolicy;
            var rates = definition.Rates; var resources = definition.Resources;
            RequireAlias(line, content); RequireAlias(capacity, content);
            RequireAlias(definition.FlowPolicy, content); RequireAlias(definition.Solver, content); RequireAlias(definition.DemandPolicy, content);
            // Explicit empty rates author connectivity only; never substitute a fictitious resource.
            if (rates == null || resources == null || resources.Count != rates.Count)
                throw content.Fail("network requires matching explicit rate/resource declarations");
            var seen = new HashSet<ResourceId>(); string previous = null;
            for (int i = 0; i < rates.Count; i++)
            {
                IResourceRate rate = rates[i];
                if (rate?.Resource == null || !ReferenceEquals(content.Content.Resource(rate.Resource.Resource), rate.Resource) ||
                    resources[i] != rate.Resource.Resource || !seen.Add(resources[i]) || !rate.Quantity.IsPositive ||
                    rate.PeriodNumerator <= 0 || rate.PeriodDenominator <= 0 ||
                    UnitConversion.ValidateAmount(rate.Resource.QuantityUnit, rate.Quantity) != UnitConversionStatus.Converted ||
                    previous != null && string.CompareOrdinal(previous, rate.Resource.Id) >= 0)
                    throw content.Fail("network rates require actual unique ordered native resource quantities and positive periods");
                try { GameDuration.SecondsPerUnit(rate.PeriodUnit); }
                catch (ArgumentException error) { throw content.Fail(error.Message); }
                if (!CapacityPolicyContracts.Check(capacity, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(),
                    new[] { new ResourceAmount(resources[i], Fix.Zero) }).Fits)
                    throw content.Fail("network capacity lacks a compatible measure for '" + rate.Resource.Id + "'");
                if (!definition.Carries(resources[i]) || !ReferenceEquals(definition.Rate(resources[i]), rate))
                    throw content.Fail("network selectors must return the actual declared resource/rate");
                previous = rate.Resource.Id;
            }
            foreach (var resource in content.Content.Resources)
                if (definition.Carries(resource.Resource) != seen.Contains(resource.Resource) ||
                    !seen.Contains(resource.Resource) && definition.Rate(resource.Resource) != null)
                    throw content.Fail("network selector claims an undeclared resource");
            if (definition.Carries(ResourceId.None) || definition.Rate(ResourceId.None) != null)
                throw content.Fail("network selector cannot claim an invalid resource");
        }
        internal static T Read<T>(World world, INetworkDefinition definition, Entity entity, Func<T> read)
            => NetworkPolicyContracts.Read(world, definition, entity, read, observeTargetState: true);
        internal static void ValidateComposition(World world)
        {
            foreach (var definition in world.Content.All<INetworkDefinition>())
                Read(world, definition, null, () =>
                {
                    Validate(definition, new ContentResolver(world.Content, (Def)definition));
                    return true;
                });
        }
    }
}
