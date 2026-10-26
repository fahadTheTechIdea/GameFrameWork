using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public static class ResourcePortCodes
    {
        public static readonly Code Unavailable=new Code("resource.port-unavailable"),
            OriginalUnavailable=new Code("resource.original-port-unavailable");
        public static readonly Code DefinitionMissing = new Code("resource.port-definition-missing"),
            Packing = new Code("resource.port-packing"), RequirementCapacity = new Code("resource.port-requirement-capacity");
    }
    /// <summary>Ready actual stock or explicit unavailable binding. A default/malformed result is a provider fault.</summary>
    public readonly struct ResourcePortResolution
    {
        public IResourceInlet Store { get; }
        public Code Blocker { get; }
        public bool Ready=>Blocker.IsNone&&Store!=null;
        private ResourcePortResolution(IResourceInlet store,Code blocker){Store=store;Blocker=blocker;}
        public static ResourcePortResolution ReadyStore(IResourceInlet store)
            =>new ResourcePortResolution(store??throw new ArgumentNullException(nameof(store)),Code.None);
        public static ResourcePortResolution Unavailable(Code blocker)
            =>blocker.IsNone?throw new ArgumentException("unavailable port requires its actual typed blocker"):new ResourcePortResolution(null,blocker);
    }
    /// <summary>Named binding to an actual stock owner, not another inventory or endpoint registry.</summary>
    public interface IResourcePortDefinition : INamedCapabilityDefinition
    {
        ICapacityPolicy CapacityPolicy { get; }
        /// <summary>Every ready resolution aliases this owner's actual IInventory; never a selected or connected store.</summary>
        bool BindsEntityInventory { get; }
        bool Accepts(ResolvedContent content, ResourceId resource);
    }
    public interface IResourcePort : INamedEntityCapability
    {
        // Named stores must supply IResourceOutlet (which also supplies IResourceInlet).
        // The common resolution additionally represents unnamed receiving-only physical ports.
        // Null means current selection; an explicit original must resolve exactly or refuse, never substitute.
        ResourcePortResolution ResolveStore(World world, ResourceEndpointId? original = null);
    }
    /// <summary>Explicit authored binding to this entity's inventory. No implicit local-port fallback.</summary>
    public sealed class InventoryPortDef : ComponentDef<InventoryPort>, IResourcePortDefinition
    {
        public bool BindsEntityInventory => true;
        public Code CapabilityId { get; private set; }
        public ICapacityPolicy CapacityPolicy => Owner.Component<IInventoryDefinition>().CapacityPolicy;
        public bool Accepts(ResolvedContent content, ResourceId resource) => Owner.Component<IInventoryDefinition>().Accepts(content, resource);
        protected override void ReadCore(JsonReader reader)
        {
            string id = reader.String("id");
            try { ContractIdentity.ValidateId(id, "resource port"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id);
        }
        protected override void ValidateCore(EntityDef owner)
        {
            if (!owner.HasCapability<IInventory>() || owner.Component<IInventoryDefinition>() == null)
                throw new ContentException(owner.Id + ": inventory port requires its actual inventory metadata/capability");
        }
    }
    public sealed class InventoryPort : Component<InventoryPortDef>, IResourcePort
    {
        public Code CapabilityId => Def.CapabilityId;
        public ResourcePortResolution ResolveStore(World world, ResourceEndpointId? original = null)
            => original.HasValue && original.Value != ResourceEndpointId.ForEntity(Entity.Id)
                ? ResourcePortResolution.Unavailable(ResourcePortCodes.OriginalUnavailable)
                : ResourcePortResolution.ReadyStore(Entity.Get<IInventory>());
    }
    public readonly struct ResourcePortRoute
    {
        public ResourceId Resource { get; }
        public Code Port { get; }
        public ResourcePortRoute(ResourceId resource, Code port) { Resource = resource; Port = port; }
    }
    /// <summary>Original source selected by an active execution; a durable reference, never stock or a promise.</summary>
    public readonly struct TransformationPortLink
    {
        public ResourceId Resource { get; }
        public Code Port { get; }
        public ResourceEndpointId Endpoint { get; }
        public TransformationPortLink(ResourceId resource, Code port, ResourceEndpointId endpoint)
        { Resource = resource; Port = port; Endpoint = endpoint; }
    }
    /// <summary>Detached private route/stock facts. No retained provider or mutable stock handle.</summary>
    public readonly struct ResourcePortSnapshot
    {
        public ResourceId Resource { get; }
        public Code Port { get; }
        public ResourceEndpointId Endpoint { get; }
        public int Version { get; }
        public Bill Stock { get; }
        public Code Blocker { get; }
        public bool Ready=>Blocker.IsNone&&Stock!=null&&Endpoint.IsValid&&Version>=0;
        public ResourcePortSnapshot(ResourceId resource, Code port, ResourceEndpointId endpoint, int version, Bill stock,Code blocker)
        {
            if(!resource.IsValid||port.IsNone||(blocker.IsNone?(!endpoint.IsValid||version<0||stock==null):(stock!=null||version!=-1)))
                throw new ArgumentException("port facts distinguish actual stock from unavailable observation");
            Resource=resource;Port=port;Endpoint=endpoint;Version=version;Stock=stock;Blocker=blocker;
        }
    }
    internal sealed class ResourcePortBinding
    {
        internal readonly ResourcePortRoute Route;
        internal readonly IResourceDefinition Resource;
        internal readonly IResourceOutlet Store;
        internal readonly int Version;
        internal readonly ResourceEndpointId Endpoint;
        internal readonly PlayerId Company;
        internal readonly ICapacityPolicy Policy;
        internal readonly Code Blocker;
        internal bool Ready=>Blocker.IsNone;
        internal ResourcePortBinding(ResourcePortRoute route,ResourcePortResolution resolution,IResourcePortDefinition definition,ResourceEndpointId? original,IResourceDefinition resource)
        {
            Route=route;Resource=resource;Store=resolution.Ready?(IResourceOutlet)resolution.Store:null;Blocker=resolution.Blocker;
            Version=Store?.Version??-1;Endpoint=Store?.Endpoint??original??default;
            Company=Store?.Company??PlayerId.None;Policy=Store?.CapacityPolicy??definition.CapacityPolicy;
        }
    }
    internal static class ResourcePortContracts
    {
        internal static void ReadRoutes(JsonReader reader, string key, List<KeyValuePair<string, string>> destination)
        {
            JsonReader ports = reader.Child(key) ?? throw reader.Error("explicit " + key + " required; author {} for no resources");
            for (int i = 0; i < ports.Raw.Count; i++)
            {
                string resource = ports.Raw.KeyAt(i), port = ports.String(resource);
                try { ContractIdentity.ValidateId(port, "resource port"); }
                catch (ArgumentException error) { throw ports.Error(error.Message); }
                destination.Add(new KeyValuePair<string, string>(resource, port));
            }
            ports.End();
        }
        internal static IReadOnlyList<ResourcePortRoute> ResolveRoutes(ContentResolver c, List<KeyValuePair<string, string>> authored)
        {
            var routes = new List<ResourcePortRoute>();
            foreach (var pair in authored)
            {
                ResourceId resource = c.Resource(pair.Key);

                routes.Add(new ResourcePortRoute(resource, new Code(pair.Value)));
            }
            routes.Sort((a, b) => a.Resource.CompareTo(b.Resource)); return routes.AsReadOnly();
        }
        internal static void ValidateRoutes(IReadOnlyList<ResourcePortRoute> routes,
            Bill bill, IReadOnlyList<IResourceRate> rates, string direction)
        {
            var expected = new HashSet<ResourceId>();
            foreach (var line in bill.Lines) expected.Add(line.Resource);
            foreach (var rate in rates) expected.Add(rate.Resource.Resource);
            foreach (var route in routes)
            {
                ContractIdentity.ValidateId(route.Port.Id, "resource port");
                if (!expected.Remove(route.Resource))
                    throw new ContentException(direction + " port maps unrelated or duplicate resource " + route.Resource);
            }
            if (expected.Count != 0) throw new ContentException("every " + direction + " resource requires exactly one explicit port");
        }
        internal static Code CheckDefinitionBindings(ResolvedContent content, EntityDef owner, IReadOnlyList<ResourcePortRoute> routes,
            Bill bill, IReadOnlyList<IResourceRate> rates, bool requireCompleteBill)
        {
            var complete = new Dictionary<Code, List<ResourceAmount>>();
            foreach (var route in routes)
            {
                IResourcePortDefinition port = owner.Component<IResourcePortDefinition>(route.Port);
                if (port == null || !typeof(IResourcePort).IsAssignableFrom(port.ComponentType) || !port.Accepts(content, route.Resource))
                    return ResourcePortCodes.DefinitionMissing;
                Fix amount = bill.AmountOf(route.Resource);
                foreach (var rate in rates) if (rate.Resource.Resource == route.Resource) amount = rate.Quantity;
                if (port.CapacityPolicy == null || CapacityPolicyContracts.Quantum(port.CapacityPolicy, route.Resource).Floor(amount) != amount)
                    return ResourcePortCodes.Packing;
                if (requireCompleteBill)
                {
                    if (!complete.TryGetValue(route.Port, out var quantities)) complete.Add(route.Port, quantities = new List<ResourceAmount>());
                    quantities.Add(new ResourceAmount(route.Resource, amount));
                }
            }
            foreach (var group in complete)
                if (!CapacityPolicyContracts.Check(owner.Component<IResourcePortDefinition>(group.Key).CapacityPolicy,
                    Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), new Bill(group.Value).Lines).Fits)
                    return ResourcePortCodes.RequirementCapacity;
            return Code.None;
        }
        internal static void ValidateDefinitionBindings(ResolvedContent content, EntityDef owner, IReadOnlyList<ResourcePortRoute> routes,
            Bill bill, IReadOnlyList<IResourceRate> rates, bool requireCompleteBill)
        {
            Code refusal = CheckDefinitionBindings(content, owner, routes, bill, rates, requireCompleteBill);
            if (!refusal.IsNone) throw new ContentException(owner.Id + ": authored resource port binding refused: " + refusal.Id);
        }
        internal static IReadOnlyList<ResourcePortBinding> Bindings(World world, Entity entity, IReadOnlyList<ResourcePortRoute> routes,
            IReadOnlyList<TransformationPortLink> originals = null)
        {
            var bindings = new List<ResourcePortBinding>();
            if (routes == null) return bindings.AsReadOnly();
            if (originals != null && originals.Count != routes.Count) throw new CorruptStateException("original tool routes differ from authored requirements");
            int index = 0;
            foreach (ResourcePortRoute route in routes)
            {
                ResourceEndpointId? original = null;
                if (originals != null)
                {
                    var source = originals[index];
                    if (source.Resource != route.Resource || source.Port != route.Port || !source.Endpoint.IsValid)
                        throw new CorruptStateException("original tool route has invalid resource/port/endpoint");
                    original = source.Endpoint;
                }
                bindings.Add(ResourcePortContracts.Bind(world, entity, route.Resource, entity.Get<IResourcePort>(route.Port), original)); index++;
            }
            ResourcePortContracts.Verify(bindings);
            return bindings.AsReadOnly();
        }
        internal static IReadOnlyList<ResourceProductionIntent> QuantitiesByEndpoint(IReadOnlyList<ResourcePortBinding> bindings, Bill outputs,
            IReadOnlyList<IResourceOutputQualityRule> qualities = null)
        {
            var grouped = new Dictionary<ResourceEndpointId, List<ResourceAmount>>();
            foreach (var binding in bindings)
            {
                Fix quantity = outputs.AmountOf(binding.Route.Resource); if (!quantity.IsPositive) continue;
                if(!binding.Ready)throw new InvalidOperationException("unavailable resource port cannot authorize physical mutation");
                if (!grouped.TryGetValue(binding.Endpoint, out var amounts))
                    grouped.Add(binding.Endpoint, amounts = new List<ResourceAmount>());
                amounts.Add(new ResourceAmount(binding.Route.Resource, quantity));
            }
            var ids = new List<ResourceEndpointId>(grouped.Keys);
            ids.Sort((a, b) => { int scope = string.CompareOrdinal(a.Scope, b.Scope); return scope != 0 ? scope : a.Identity.CompareTo(b.Identity); });
            var result = new List<ResourceProductionIntent>();
            foreach (var id in ids)
            {
                Bill bill = new Bill(grouped[id]); var selected = new List<IResourceOutputQualityRule>();
                if (qualities != null) foreach (var rule in qualities) if (bill.AmountOf(rule.Quality.Resource).IsPositive) selected.Add(rule);
                result.Add(new ResourceProductionIntent(id, bill, selected));
            }
            return result.AsReadOnly();
        }
        internal static IReadOnlyList<ResourceClaimIntent> OutboundClaims(ReservationOwner owner,
            params IReadOnlyList<ResourceProductionIntent>[] demands)
        {
            // One transient endpoint vector for feed, reusable tools and operating draws; books own all claims.
            var grouped = new Dictionary<ResourceEndpointId, List<ResourceAmount>>();
            foreach (var demand in demands)
                foreach (var intent in demand)
                {
                    if (intent.Quantities.IsEmpty) continue;
                    if (!grouped.TryGetValue(intent.Endpoint, out var vector)) grouped.Add(intent.Endpoint, vector = new List<ResourceAmount>());
                    vector.AddRange(intent.Quantities.Lines);
                }
            var result = new List<ResourceClaimIntent>();
            foreach (var vector in grouped) result.Add(new ResourceClaimIntent(ReservationDirection.Outbound, owner, vector.Key, new Bill(vector.Value)));
            return result.AsReadOnly();
        }
        internal static Code Blocker(params IReadOnlyList<ResourcePortBinding>[] sets)
        { foreach(var set in sets)foreach(var binding in set)if(!binding.Ready)return binding.Blocker;return Code.None; }
        internal static void ValidateFacts(IReadOnlyList<ResourcePortSnapshot> facts, IReadOnlyList<ResourcePortSnapshot> expected)
        {
            if (facts.Count != expected.Count) throw new CorruptStateException("resource port facts count differs");
            for (int i = 0; i < facts.Count; i++)
            {
                var actual = facts[i]; var target = expected[i];
                if (actual.Resource != target.Resource || actual.Port != target.Port || actual.Endpoint != target.Endpoint || actual.Version != target.Version ||
                    actual.Blocker != target.Blocker || (actual.Stock == null) != (target.Stock == null) || actual.Stock != null && actual.Stock.Count != target.Stock.Count) throw new CorruptStateException("resource port facts differ from actual authored endpoints");
                if(actual.Stock!=null)foreach (var amount in actual.Stock.Lines)
                    if (amount.Amount != target.Stock.AmountOf(amount.Resource))
                        throw new CorruptStateException("resource port facts differ from actual native stock");
            }
        }
        internal static ResourcePortBinding Bind(World world, Entity entity, ResourceId resource, IResourcePort port, ResourceEndpointId? original = null)
        {
            var resourceDefinition = world.Content.Resource(resource);
            if (resourceDefinition == null || port == null || port.CapabilityId.IsNone ||
                !ReferenceEquals(port.Entity, entity) || !ReferenceEquals(entity.Get<IResourcePort>(port.CapabilityId), port))
                throw new InvalidOperationException("missing or foreign named resource port");
            IResourcePortDefinition definition = entity.Def.Component<IResourcePortDefinition>(port.CapabilityId);
            if (definition == null || !definition.Accepts(world.Content, resource))
                throw new InvalidOperationException("resource port returned invalid, foreign or differently packed stock");
            var resolution=Store(world,entity,port,original);
            var binding=new ResourcePortBinding(new ResourcePortRoute(resource,port.CapabilityId),resolution,definition,original,resourceDefinition);
            if (binding.Ready&&!ResourceQuantitySignals.Capture(world, binding.Store, resource, out _).IsNone)
                throw new InvalidOperationException("resource port lost actual receiving/outgoing compatibility");
            Verify(new[] { binding }); return binding;
        }
        internal static ResourcePortResolution Store(World world, Entity entity, IResourcePort port, ResourceEndpointId? original = null)
        {
            if (world == null || entity?.Alive != true || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                port == null || port.CapabilityId.IsNone || !ReferenceEquals(port.Entity, entity) ||
                !ReferenceEquals(entity.Get<IResourcePort>(port.CapabilityId), port))
                throw new InvalidOperationException("missing or foreign named resource port");
            IResourcePortDefinition definition = entity.Def.Component<IResourcePortDefinition>(port.CapabilityId);
            if(definition==null)throw new InvalidOperationException("resource port lost actual metadata");
            return EntityObservation.ReadProvider(world,port,entity,()=>
            {
                var resolution=port.ResolveStore(world,original);
                if(resolution.Blocker.IsNone?resolution.Store==null:resolution.Store!=null)
                    throw new InvalidOperationException("malformed resource-port availability result");
                if(resolution.Ready)ValidateStore(entity,definition,resolution.Store,original);
                return resolution;
            },observeTargetState:true);
        }
        internal static void ValidateStore(Entity entity,IResourcePortDefinition definition,IResourceInlet store,ResourceEndpointId? original)
        {
            if (definition != null && definition.BindsEntityInventory &&
                (!ReferenceEquals(store, entity.Get<IInventory>()) || store.Endpoint != ResourceEndpointId.ForEntity(entity.Id)))
                throw new InvalidOperationException("entity-inventory port returned another stock owner");
            if (definition == null || !(store is IResourceOutlet) || !store.Endpoint.IsValid ||
                store.Company != entity.Owner && !(store.Company == PlayerId.None && store.Public) || store.Version < 0 ||
                original.HasValue && store.Endpoint != original.Value || !CapacityPolicyContracts.Satisfies(store.CapacityPolicy, definition.CapacityPolicy))
                throw new InvalidOperationException("resource port returned invalid, foreign or differently packed stock");
        }
        internal static void Verify(IReadOnlyList<ResourcePortBinding> bindings)
        {
            foreach (var binding in bindings)
                if (binding.Ready&&(binding.Store.Version != binding.Version || binding.Store.Endpoint != binding.Endpoint || binding.Store.Company != binding.Company ||
                    !ReferenceEquals(binding.Store.CapacityPolicy, binding.Policy)))
                    throw new InvalidOperationException("port observation mutated actual endpoint");
        }

        internal static IReadOnlyList<ResourcePortSnapshot> Facts(IReadOnlyList<ResourcePortBinding> bindings)
        {
            var result = new List<ResourcePortSnapshot>();
            // One fact per resource route: repeat aliases retain the same endpoint/version, never copied stock authority.
            foreach (var binding in bindings)
            {
                if(!binding.Ready){result.Add(new ResourcePortSnapshot(binding.Route.Resource,binding.Route.Port,binding.Endpoint,-1,null,binding.Blocker));continue;}
                var amounts = new List<ResourceAmount>(); Fix stock = binding.Store.Amount(binding.Route.Resource);
                ResourceQuantitySignals.ValidateQuantity(binding.Resource,
                    CapacityPolicyContracts.Quantum(binding.Policy, binding.Route.Resource), stock);
                if (stock.IsPositive) amounts.Add(new ResourceAmount(binding.Route.Resource, stock));
                result.Add(new ResourcePortSnapshot(binding.Route.Resource, binding.Route.Port, binding.Endpoint, binding.Version, new Bill(amounts),Code.None));
                Verify(bindings);
            }
            return result.AsReadOnly();
        }

    }
    public static partial class TransformationContracts
    {
        internal static bool SamePortLinks(IReadOnlyList<TransformationPortLink> left, IReadOnlyList<TransformationPortLink> right)
        {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
                if (left[i].Resource != right[i].Resource || left[i].Port != right[i].Port || left[i].Endpoint != right[i].Endpoint) return false;
            return true;
        }
        internal static IReadOnlyList<ResourcePortBinding> OutputBindings(World world, Entity entity, ITransformationDefinition definition)
            => ResourcePortContracts.Bindings(world, entity, definition?.OutputPorts);
        internal static IReadOnlyList<ResourcePortBinding> InputBindings(World world, Entity entity, ITransformationDefinition definition)
            => ResourcePortContracts.Bindings(world, entity, definition?.InputPorts);
        internal static IReadOnlyList<ResourcePortBinding> ToolBindings(World world, Entity entity, ITransformationDefinition definition,
            IReadOnlyList<TransformationPortLink> originals = null)
            => ResourcePortContracts.Bindings(world, entity, definition?.ToolPorts, originals);
        internal static IReadOnlyList<ResourcePortBinding> OperatingBindings(World world, Entity entity, ITransformationDefinition definition)
            => ResourcePortContracts.Bindings(world, entity, definition?.OperatingPorts);
        internal static Bill OutputRates(ITransformationDefinition definition, ISimulationTime time, IReadOnlyList<ResourcePortBinding> bindings,
            WorkProgress before, WorkProgress after, bool cumulative)
            => PortRates(definition.OutputRates, time, bindings, before, after, ResourceRateRounding.Floor, cumulative);
        internal static Bill InputRates(ITransformationDefinition definition, ISimulationTime time, IReadOnlyList<ResourcePortBinding> bindings,
            WorkProgress before, WorkProgress after, bool cumulative)
            => PortRates(definition.InputRates, time, bindings, before, after, ResourceRateRounding.Ceiling, cumulative);
        internal static Bill OperatingRates(ITransformationDefinition definition, ISimulationTime time, IReadOnlyList<ResourcePortBinding> bindings,
            WorkProgress before, WorkProgress after, bool cumulative)
            => PortRates(definition.OperatingInputs, time, bindings, before, after, ResourceRateRounding.Ceiling, cumulative);
        internal static Bill ToolRates(ITransformationDefinition definition, ISimulationTime time, IReadOnlyList<ResourcePortBinding> bindings,
            WorkProgress before, WorkProgress after, bool cumulative)
            => PortRates(definition.ToolConsumptionRates, time, bindings, before, after, ResourceRateRounding.Ceiling, cumulative);
        private static Bill PortRates(IReadOnlyList<IResourceRate> authoredRates, ISimulationTime time, IReadOnlyList<ResourcePortBinding> bindings,
            WorkProgress before, WorkProgress after, ResourceRateRounding rounding, bool cumulative)
        {
            var amounts = new List<ResourceAmount>();
            foreach (var binding in bindings)
            {
                var rates = new List<IResourceRate>();
                foreach (var rate in authoredRates) if (rate.Resource.Resource == binding.Route.Resource) rates.Add(rate);
                Bill bill = cumulative ? ResourceRates.Accumulated(rates, time, binding.Policy, after, rounding)
                    : ResourceRates.Between(rates, time, binding.Policy, before, after, rounding);
                amounts.AddRange(bill.Lines); ResourcePortContracts.Verify(bindings);
            }
            return new Bill(amounts);
        }
    }
}
