using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored policy and filtering for the one stock authority, including cargo and work buffers.</summary>
    public sealed class StorageDeclaration
    {
        private StorageDeclaration() { }
        public static StorageDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            var declaration = new StorageDeclaration(); declaration.ReadCore(reader); return declaration;
        }
        private string _policy;
        private string[] _accepts;
        private ResourceId[] _resources;
        private string[] _categories;
        private string[] _phases;
        private BillSpec _initial;
        private readonly List<KeyValuePair<string, Fix>> _initialLots = new List<KeyValuePair<string, Fix>>();
        public ResolvedContent Content { get; private set; }
        public ICapacityPolicy CapacityPolicy { get; private set; }
        public bool Public { get; private set; }
        public bool Evacuate { get; private set; }
        public Bill InitialStock { get; private set; } = Bill.Empty;
        public IReadOnlyList<ResourceLotAmount> InitialLots { get; private set; } = Array.Empty<ResourceLotAmount>();

        private void ReadCore(JsonReader r)
        {
            _policy = r.String("capacity_policy");
            _initial = BillSpec.Read(r, "initial_stock");
            JsonValue lots = r.Array("initial_lots");
            if (lots != null) for (int i = 0; i < lots.Count; i++)
            {
                var row = new JsonReader(lots[i], r.Where + ".initial_lots[" + i + "]");
                string id = row.String("lot"); Fix amount = row.Fix("amount"); row.End();
                if (!amount.IsPositive || _initialLots.Exists(pair => pair.Key == id)) throw r.Error("initial lots require unique identities and positive quantities");
                _initialLots.Add(new KeyValuePair<string, Fix>(id, amount));
            }
            _phases = r.Strings("phases");
            for (int i = 0; i < _phases.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(_phases[i])) throw r.Error("phase filter identity must not be empty");
                for (int k = 0; k < i; k++) if (_phases[k] == _phases[i]) throw r.Error("duplicate phase filter");
            }
            Public = r.Bool("public", true);
            Evacuate = r.Bool("evacuate", false);
            _accepts = r.Strings("accepts");
            var distinct = new HashSet<string>(StringComparer.Ordinal);
            foreach (string accepted in _accepts)
                if (string.IsNullOrWhiteSpace(accepted) || !distinct.Add(accepted) ||
                    accepted.StartsWith("category:", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(accepted.Substring(9)))
                    throw r.Error("accepted resource/category filters must be nonempty and unique");
        }

        public void Resolve(ContentResolver c)
        {
            Content = c.Content; CapacityPolicy = c.Def<ICapacityPolicyDefinition>(_policy);
            var resources = new List<ResourceId>(); var categories = new List<string>();
            foreach (string accepted in _accepts)
                if (accepted.StartsWith("category:", StringComparison.Ordinal)) categories.Add(accepted.Substring(9));
                else resources.Add(c.Resource(accepted));
            _resources = resources.ToArray(); _categories = categories.ToArray();
            foreach (ResourceId resource in _resources)
                if (!Accepts(Content, resource)) throw c.Fail("storage cannot accept authored resource '" + Content.ResourceName(resource) + "'");
            InitialStock = _initial == null ? Bill.Empty : _initial.Resolve(c);
            var initial = new List<ResourceAmount>(InitialStock.Lines); var lotStock = new List<ResourceLotAmount>();
            _initialLots.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            foreach (var pair in _initialLots)
            {
                var lot = c.Def<IResourceLotDefinition>(pair.Key);
                if (!Accepts(Content, lot.Resource) || UnitConversion.ValidateAmount(Content.Resource(lot.Resource).QuantityUnit, pair.Value) != UnitConversionStatus.Converted ||
                    CapacityPolicyContracts.Quantum(CapacityPolicy, lot.Resource).Floor(pair.Value) != pair.Value)
                    throw c.Fail("initial lot quantity violates resource units or capacity quantum");
                lotStock.Add(new ResourceLotAmount(lot.Resource, new Code(lot.Id), pair.Value)); initial.Add(new ResourceAmount(lot.Resource, pair.Value));
            }
            InitialLots = lotStock.AsReadOnly(); InitialStock = new Bill(initial);
            foreach (ResourceAmount line in InitialStock.Lines)
                if (!Accepts(Content, line.Resource) || CapacityPolicyContracts.Quantum(CapacityPolicy, line.Resource).Floor(line.Amount) != line.Amount)
                    throw c.Fail("initial stock is incompatible with storage resource filters or exact capacity quantum");
            if (!CapacityPolicyContracts.Check(CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), InitialStock.Lines).Fits)
                throw c.Fail("initial stock violates complete authored capacity policy");
        }

        internal static void ValidateDefinitions(World world)
        {
            foreach (EntityDef entity in world.Content.All<EntityDef>())
                foreach (ComponentDef part in entity.Components)
                {
                    bool runtime = typeof(IStorage).IsAssignableFrom(part.ComponentType);
                    if (runtime != (part is IStorageDefinition))
                        throw new ContentException(entity.Id + ": reusable storage requires matching IStorageDefinition and IStorage");
                    if (!runtime) continue;
                    EntityObservation.ReadProvider(world, part, null, () => {
                        var definition = (IStorageDefinition)part;
                        var content = definition.Content; var policy = definition.CapacityPolicy; var initial = definition.InitialStock;
                        if (definition.InitialLots == null || definition.InitialLots.Count != 0 && !typeof(IResourceLotEndpoint).IsAssignableFrom(part.ComponentType))
                            throw new ContentException(entity.Id + ": selected lot stock requires the actual lot inventory contract");
                        _ = definition.Public; _ = definition.Evacuate;
                        if (!ReferenceEquals(content, world.Content) || !(policy is ICapacityPolicyDefinition authored) ||
                            !world.Content.Owns(authored) || !ReferenceEquals(world.Content.Find<ICapacityPolicyDefinition>(authored.Id), policy) || initial == null)
                            throw new ContentException(entity.Id + ": storage must bind its actual catalog, capacity policy and explicit initial bill");
                        foreach (var line in initial.Lines)
                        {
                            var resource = world.Content.Resource(line.Resource);
                            if (resource == null || !definition.Accepts(world.Content, line.Resource) ||
                                UnitConversion.ValidateAmount(resource.QuantityUnit, line.Amount) != UnitConversionStatus.Converted ||
                                CapacityPolicyContracts.Quantum(policy, line.Resource).Floor(line.Amount) != line.Amount)
                                throw new ContentException(entity.Id + ": storage initial quantities must fit their actual filters/native quantum");
                        }
                        if (!CapacityPolicyContracts.Check(policy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), initial.Lines).Fits)
                            throw new ContentException(entity.Id + ": storage initial bill exceeds its whole authored policy");
                        return true;
                    });
                }
        }

        public bool Accepts(ResolvedContent content, ResourceId resource)
        {
            if (!ReferenceEquals(content, Content)) return false;
            IResourceDefinition definition = content.Resource(resource);
            if (definition == null) return false;
            bool phase = _phases.Length == 0;
            foreach (string allowed in _phases) if (allowed == definition.Phase.Id) phase = true;
            if (!phase) return false;
            bool accepted = _resources.Length == 0 && _categories.Length == 0;
            foreach (ResourceId id in _resources) if (id == resource) accepted = true;
            foreach (string category in _categories) if (category == definition.Category) accepted = true;
            return accepted && CapacityPolicyContracts.Check(CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(),
                new[] { new ResourceAmount(resource, Fix.Zero) }).Fits;
        }

    }

    /// <summary>Native authored adapter to the sole storage declaration, not a stock owner.</summary>
    public sealed class StorageDef : ComponentDef<Storage>, IStorageDefinition
    {
        private StorageDeclaration _declaration;
        public ResolvedContent Content => _declaration.Content;
        public ICapacityPolicy CapacityPolicy => _declaration.CapacityPolicy;
        public bool Public => _declaration.Public;
        public bool Evacuate => _declaration.Evacuate;
        public Bill InitialStock => _declaration.InitialStock;
        public IReadOnlyList<ResourceLotAmount> InitialLots => _declaration.InitialLots;
        public bool Accepts(ResolvedContent content, ResourceId resource) => _declaration.Accepts(content, resource);
        protected override void ReadCore(JsonReader reader) => _declaration = StorageDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }

    /// <summary>Only live balance/promise owner. Policy checks compute facts from these lists, not cached totals.</summary>
    public sealed class Storage : Component, IInspectable, IStorage, IEntityInitialization, IResourceConversionEndpoint, IResourceLotEndpoint
    {
        public new IStorageDefinition Def => (IStorageDefinition)base.Def;
        public ResourceEndpointId Endpoint => ResourceEndpointId.ForEntity(Entity.Id);
        public void WriteResourceState(IStateSync writer) => State.WriteResourceState(writer);
        public PlayerId Company => Entity.Owner;
        public int Version => Entity.Version;
        public void Initialize(World world) => ResourceTransactions.InitializeStock(world, Entity);
        public Code CheckRemoval(World world) => ResourceTransactions.CheckInventoryRemoval(this);
        private ResourceInventoryState _state;
        private ResourceInventoryState State => _state ?? (_state = new ResourceInventoryState(Endpoint, () => Company, () => Version,
            Entity.Touch, Def.Content, () => Def.CapacityPolicy, resource => Def.Accepts(Def.Content, resource), Def.Public, Def.Evacuate));
        public IReadOnlyList<ResourceAmount> Stock => State.Stock;
        public IReadOnlyList<ResourceLotAmount> Lots => State.Lots;
        public IReadOnlyList<ResourceLotAmount> SelectLots(ReservationOwner owner, ReservationToken token, Fix amount) => State.SelectLots(owner, token, amount);
        public ReservationToken ReserveLotsOut(ReservationOwner owner, ResourceId resource, IReadOnlyList<ResourceLotAmount> lots, Code selection) => State.ReserveLotsOut(owner, resource, lots, selection);
        public void IncreaseLotsReservedOut(ReservationOwner owner, ReservationToken token, IReadOnlyList<ResourceLotAmount> lots) => State.IncreaseLotsReservedOut(owner, token, lots);
        public Fix ReceiveLotsReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, IReadOnlyList<ResourceLotAmount> lots)
            => State.ReceiveLotsReserved(content, owner, token, lots);
        public void InitializeLots(IReadOnlyList<ResourceLotAmount> lots) => State.InitializeLots(lots);
        public IReadOnlyList<ResourceAmount> InboundPromises => State.InboundPromises;
        public IReadOnlyList<ResourceAmount> OutboundPromises => State.OutboundPromises;
        public IReadOnlyList<ResourceReservation> Reservations => State.Reservations;
        public ICapacityPolicy CapacityPolicy => State.CapacityPolicy;
        public bool Public => State.Public;
        public bool Evacuate => State.Evacuate;
        public bool Accepts(ResolvedContent content, ResourceId resource) => State.Accepts(content, resource);
        public CapacityCheck CheckIncoming(IReadOnlyList<ResourceAmount> incoming) => State.CheckIncoming(incoming);
        public CapacityCheck CheckConversion(IReadOnlyList<ResourceAmount> consumed, IReadOnlyList<ResourceAmount> produced) => State.CheckConversion(consumed, produced);
        public void ConvertReserved(ReservationOwner owner, IReadOnlyList<ResourceConversionInput> inputs, Bill produced, IReadOnlyList<ResourceLotAmount> lots = null) => State.ConvertReserved(owner, inputs, produced, lots);
        public Fix Offer(ResourceId resource, Fix ceiling) => State.Offer(resource, ceiling);
        public Fix Receive(ResolvedContent content, ResourceId resource, Fix amount) => State.Receive(content, resource, amount);
        public Fix Take(ResourceId resource, Fix amount) => State.Take(resource, amount);
        public Fix Amount(ResourceId resource) => State.Amount(resource);
        public Fix ReservedIn(ResourceId resource) => State.ReservedIn(resource);
        public Fix ReservedOut(ResourceId resource) => State.ReservedOut(resource);
        public Fix Available(ResourceId resource) => State.Available(resource);
        public bool Holds(Bill bill) => State.Holds(bill);
        public bool HoldsUnpromised(Bill bill) => State.HoldsUnpromised(bill);
        public Fix Add(ResourceId resource, Fix amount) => State.Add(resource, amount);
        public Fix Remove(ResourceId resource, Fix amount) => State.Remove(resource, amount);
        public bool CanReserveIn(ReservationOwner owner, ResourceId resource, Fix amount) => State.CanReserveIn(owner, resource, amount);
        public ReservationToken ReserveIn(ReservationOwner owner, ResourceId resource, Fix amount) => State.ReserveIn(owner, resource, amount);
        public bool CanReserveOut(ReservationOwner owner, ResourceId resource, Fix amount) => State.CanReserveOut(owner, resource, amount);
        public ReservationToken ReserveOut(ReservationOwner owner, ResourceId resource, Fix amount) => State.ReserveOut(owner, resource, amount);
        public void ReleaseIn(ReservationOwner owner, ReservationToken token) => State.ReleaseIn(owner, token);
        public void ReleaseOut(ReservationOwner owner, ReservationToken token) => State.ReleaseOut(owner, token);
        public ResourceReceiptCheck CheckReservedIn(ResolvedContent content, ReservationOwner owner, ReservationToken token) => State.CheckReservedIn(content, owner, token);
        public ResourceReceiptCheck CheckReservedOut(ReservationOwner owner, ReservationToken token) => State.CheckReservedOut(owner, token);
        public Fix ReceiveReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, Fix amount) => State.ReceiveReserved(content, owner, token, amount);
        public Fix TakeReserved(ReservationOwner owner, ReservationToken token, Fix amount) => State.TakeReserved(owner, token, amount);
        public void IncreaseReservedOut(ReservationOwner owner, ReservationToken token, Fix amount) => State.IncreaseReservedOut(owner, token, amount);
        public override void Sync(IStateSync s) => State.Sync(s);

        public static readonly Code FacetStock = new Code("facet.stock");
        public static readonly Code FacetCapacity = new Code("facet.capacity");
        public void Inspect(World world, PlayerId viewer, IList<Facet> into)
        {
            bool mine = !viewer.IsValid || viewer == Entity.Owner;
            CapacityCheck usage = CapacityPolicyContracts.Check(CapacityPolicy, Stock, InboundPromises, Array.Empty<ResourceAmount>());
            foreach (CapacityUsage dimension in usage.Usage)
                into.Add(new Facet(FacetCapacity, mine ? dimension.Projected : Fix.Zero, dimension.Unit.Id, dimension.Limit));
            if (mine) foreach (ResourceAmount line in Stock)
                if (world.ViewFor(viewer).CanSeeContent(world.Content.Resource(line.Resource)))
                    into.Add(new Facet(FacetStock, line.Amount, world.Content.ResourceName(line.Resource)));
        }
    }
}
