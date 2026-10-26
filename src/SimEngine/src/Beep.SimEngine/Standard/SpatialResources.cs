using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored actual cell contract; each spatial provider owns its location semantics.</summary>
    public interface ISpatialResourceCellDefinition : IContentDefinition
    {
        long Identity { get; }
        bool Public { get; }
        ISpatialLayerDefinition Layer { get; }
        ICapacityPolicy Capacity { get; }
        void ValidateComposition(World world);
        IResourceOutlet Bind(World world, ResourceEndpointId endpoint);
        Code CheckSite(World world, Entity target);
    }

    /// <summary>Sole common cell authoring and binding rule, never another material balance/book.</summary>
    public sealed class SpatialResourceCellDeclaration
    {
        private string _layer, _capacity;
        public long Identity { get; private set; }
        public bool Public { get; private set; }
        public ISpatialLayerDefinition Layer { get; private set; }
        public ICapacityPolicy Capacity { get; private set; }
        private SpatialResourceCellDeclaration() { }
        public static SpatialResourceCellDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit spatial resource-cell declaration required");
            var declaration = new SpatialResourceCellDeclaration {
                Identity = reader.Long("identity"), _layer = reader.String("layer"), _capacity = reader.String("capacity_policy"),
                Public = reader.Bool("public") };
            if (declaration.Identity <= 0 || string.IsNullOrWhiteSpace(declaration._layer) || string.IsNullOrWhiteSpace(declaration._capacity))
                throw reader.Error("spatial cell needs positive identity, layer and capacity policy");
            return declaration;
        }
        public void Resolve(ContentResolver content)
        { Layer = content.Def<ISpatialLayerDefinition>(_layer); Capacity = content.Def<ICapacityPolicyDefinition>(_capacity); }
        public void ValidateResolved(ContentResolver content)
        { ValidateMetadata(content.Content, Identity, Layer, Capacity, Public); }
        public static void ValidateMetadata(ResolvedContent content, long identity, ISpatialLayerDefinition layer, ICapacityPolicy capacity, bool isPublic)
        {
            if (content == null || identity <= 0 || layer == null || !content.Owns(layer) ||
                !ReferenceEquals(content.Find<ISpatialLayerDefinition>(layer.Id), layer) ||
                !(capacity is ICapacityPolicyDefinition policy) || !ReferenceEquals(content.Find<ICapacityPolicyDefinition>(policy.Id), capacity) ||
                layer.ResourceMeasure != SpatialResourceMeasure.Quantity || layer.Resource == null ||
                isPublic && layer.Visibility == SpatialLayerVisibility.CompanyPrivate || !isPublic && layer.Visibility == SpatialLayerVisibility.Public)
                throw new ContentException("spatial resource cell requires actual quantity layer/capacity and explicit matching visibility/access");
        }
        public IResourceOutlet Bind<TLocation>(World world, ResourceEndpointId endpoint, ISpatialResourceQuantity<TLocation> quantity, TLocation location)
        {
            if (quantity == null || quantity.Id != Layer.Id || quantity.Space != Layer.Space.Id)
                throw new ContentException("spatial resource cell requires its actual selected quantity layer");
            var result = new SpatialResourceEndpoint<TLocation>(world, endpoint, quantity, location, Capacity, Public);
            quantity.BindResourceCell(location, result); return result;
        }
    }

    /// <summary>Sole native tile location strategy; reusable by direct JSON definitions.</summary>
    public sealed class TileResourceCellDeclaration
    {
        public SpatialResourceCellDeclaration Cell { get; }
        public TileCoord Tile { get; }
        private TileResourceCellDeclaration(SpatialResourceCellDeclaration cell, TileCoord tile) { Cell = cell; Tile = tile; }
        public static TileResourceCellDeclaration Read(JsonReader reader)
            => new TileResourceCellDeclaration(SpatialResourceCellDeclaration.Read(reader), new TileCoord(reader.Int("x"), reader.Int("y")));
        public void Resolve(ContentResolver content) => Cell.Resolve(content);
        public void ValidateResolved(ContentResolver content)
        {
            Cell.ValidateResolved(content);
            if (!(Cell.Layer.Space is ITileSpaceDefinition space) || !SpatialSpaceContracts.ContainsAny(space, new TileRect(Tile, 1, 1)))
                throw content.Fail("tile resource cell lies outside its authored tile space");
        }
        private ISpatialResourceQuantity<TileCoord> Quantity(World world)
        {
            if (!(world.Space is ITileSpatialSpace map) || map.Id != Cell.Layer.Space.Id || !map.InBounds(Tile))
                throw new ContentException("wrong selected resource space for tile cell");
            return TileSpatialGeometry.RequireLayer<ISpatialResourceQuantity<TileCoord>>(world, map, Cell.Layer, false);
        }
        public void ValidateComposition(World world) { _ = Quantity(world).ReadQuantity(Tile); }
        public IResourceOutlet Bind(World world, ResourceEndpointId endpoint) => Cell.Bind(world, endpoint, Quantity(world), Tile);
        public Code CheckSite(World world, Entity target)
        {
            ITilePresence presence = target.Get<ITilePresence>();
            return presence != null && presence.Placed && !presence.Movable && presence.Space == Cell.Layer.Space.Id && presence.Footprint.Contains(Tile)
                ? Code.None : Codes.InvalidArgument;
        }
    }

    public sealed class TileResourceCellDef : Def, ISpatialResourceCellDefinition
    {
        private TileResourceCellDeclaration _declaration;
        public TileCoord Tile => _declaration.Tile;
        public long Identity => _declaration.Cell.Identity;
        public bool Public => _declaration.Cell.Public;
        public ISpatialLayerDefinition Layer => _declaration.Cell.Layer;
        public ICapacityPolicy Capacity => _declaration.Cell.Capacity;
        protected override void ReadCore(JsonReader reader) => _declaration = TileResourceCellDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.ValidateResolved(content);
        public void ValidateComposition(World world) => _declaration.ValidateComposition(world);
        public IResourceOutlet Bind(World world, ResourceEndpointId endpoint) => _declaration.Bind(world, endpoint);
        public Code CheckSite(World world, Entity target) => _declaration.CheckSite(world, target);
    }

    /// <summary>One book/version over one actual provider cell. No material balance is stored here.</summary>
    public sealed class SpatialResourceEndpoint<TLocation> : IResourceOutlet, ISpatialResourceCellGuard
    {
        private readonly World _world;
        private readonly ISpatialResourceQuantity<TLocation> _quantity;
        private readonly TLocation _location;
        private ResourceReservationBook _book = new ResourceReservationBook();
        private int _version;
        private Fix _committing;
        private Fix _receiving;
        public SpatialResourceEndpoint(World world, ResourceEndpointId endpoint, ISpatialResourceQuantity<TLocation> quantity,
            TLocation location, ICapacityPolicy capacity, bool isPublic)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world)); _quantity = quantity ?? throw new ArgumentNullException(nameof(quantity));
            if (!endpoint.IsValid || quantity.ResourceMeasure != SpatialResourceMeasure.Quantity || quantity.Resource == null ||
                !ReferenceEquals(quantity.Resource, world.Content.Resource(quantity.Resource.Resource)) || quantity.Space != world.Space?.Id ||
                isPublic != (quantity.Visibility == SpatialLayerVisibility.Public)) throw new ContentException("invalid actual spatial resource endpoint binding");
            Endpoint = endpoint; _location = location; CapacityPolicy = capacity ?? throw new ArgumentNullException(nameof(capacity));
            Public = isPublic; _ = quantity.ReadQuantity(location);
        }
        public ResourceEndpointId Endpoint { get; }
        public World World => _world;
        public ISpatialLayer Layer => _quantity;
        public PlayerId Company => Layer.Owner;
        public int Version => _version;
        public Fix Quantity => _quantity.ReadQuantity(_location).Amount;
        public void WriteResourceState(IStateSync writer)
        {
            ResourceStateEncoding.RequireWriter(writer);
            Fix quantity = Quantity; writer.Fix(ref quantity); StageSync(writer, quantity);
        }
        public bool Public { get; }
        public bool Evacuate => false;
        public ICapacityPolicy CapacityPolicy { get; }
        public IReadOnlyList<ResourceReservation> Reservations => _book.Records;
        public IReadOnlyList<ResourceAmount> InboundPromises => _book.Promises(ReservationDirection.Inbound);
        public IReadOnlyList<ResourceAmount> OutboundPromises => _book.Promises(ReservationDirection.Outbound);
        private ResourceId Resource => Layer.Resource.Resource;
        public Fix Amount(ResourceId resource) => resource == Resource ? Quantity : Fix.Zero;
        public Fix ReservedOut(ResourceId resource) => _book.Amount(resource, ReservationDirection.Outbound);
        public Fix ReservedIn(ResourceId resource) => _book.Amount(resource, ReservationDirection.Inbound);
        public Fix Available(ResourceId resource) => Amount(resource) - ReservedOut(resource);
        public bool Accepts(ResolvedContent content, ResourceId resource) => ReferenceEquals(content, _world.Content) && resource == Resource;
        public bool AllowsQuantity(Fix nativeAfter)
        {
            if (nativeAfter < ReservedOut(Resource) - _committing) return false;
            Fix promised = ReservedIn(Resource) - _receiving;
            return !promised.IsPositive && !_receiving.IsPositive || FitsIncoming(nativeAfter, promised);
        }
        public void PreflightQuantityChange(Fix nativeAfter)
        {
            if (!AllowsQuantity(nativeAfter)) throw new InvalidOperationException("spatial material/capacity is protected by actual receipts");
            _ = checked(_version + 1);
        }
        public void QuantityChanged() { _version = checked(_version + 1); }
        public void PreflightOwnerChange()
        {
            _ = Quantity;
            if (Reservations.Count != 0 || _committing.IsPositive || _receiving.IsPositive) throw new InvalidOperationException("spatial ownership is protected by actual receipts");
            _ = checked(_version + 1);
        }
        public void OwnerChanged() { _version = checked(_version + 1); }
        private void PreflightBook()
        {
            _ = Quantity;
            if (_committing.IsPositive || _receiving.IsPositive) throw new InvalidOperationException("reentrant spatial resource commitment");
            _ = checked(_version + 1);
        }
        // Material compatibility only; the existing endpoint book handles current promise availability.
        private SpatialResourceChange MaterialCheck(Fix amount, Fix? stock = null)
        {
            UnitConversionResult stored = UnitConversion.Convert(stock ?? Quantity, Layer.Resource.QuantityUnit, Layer.Unit);
            if (stored.Status != UnitConversionStatus.Converted) throw new CorruptStateException("actual cell cannot recover its stored quantity");
            return SpatialResourceValues.CheckWithdrawal(Layer, stored.Amount, amount);
        }
        private bool ValidAmount(Fix amount) => amount.IsPositive && UnitConversion.ValidateAmount(Layer.Resource.QuantityUnit, amount) == UnitConversionStatus.Converted &&
            CapacityPolicyContracts.Quantum(CapacityPolicy, Resource).Floor(amount) == amount && MaterialCheck(amount).Ready;
        private IReadOnlyList<ResourceAmount> StockAt(Fix amount) => amount.IsPositive
            ? new[] { new ResourceAmount(Resource, amount) } : Array.Empty<ResourceAmount>();
        private SpatialResourceChange DepositCheck(Fix amount, Fix stock)
        {
            var stored = UnitConversion.Convert(stock, Layer.Resource.QuantityUnit, Layer.Unit);
            if (stored.Status != UnitConversionStatus.Converted) throw new CorruptStateException("actual cell cannot recover its stored quantity");
            return SpatialResourceValues.CheckDeposit(Layer, stored.Amount, amount);
        }
        private bool FitsIncoming(Fix stock, Fix promised)
            => CapacityPolicyContracts.Check(CapacityPolicy, StockAt(stock), Array.Empty<ResourceAmount>(), StockAt(promised)).Fits &&
                (!promised.IsPositive || DepositCheck(promised, stock).Ready);
        public bool CanReserveOut(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (resource != Resource || !_book.CanReserve(owner, resource, ReservationDirection.Outbound) || !ValidAmount(amount) || amount > Available(resource)) return false;
            return FitsPromises(ReservedOut(resource) + amount);
        }
        private bool FitsPromises(Fix amount) => CapacityPolicyContracts.Check(CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(),
            amount.IsPositive ? new[] { new ResourceAmount(Resource, amount) } : Array.Empty<ResourceAmount>()).Fits;
        public ReservationToken ReserveOut(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (!CanReserveOut(owner, resource, amount)) throw new InvalidOperationException("invalid spatial resource commitment");
            PreflightBook(); var token = _book.Add(Endpoint, owner, resource, amount, ReservationDirection.Outbound); _version++; return token;
        }
        public void IncreaseReservedOut(ReservationOwner owner, ReservationToken token, Fix amount)
        {
            ResourceReservation record = _book.Get(Endpoint, owner, token, ReservationDirection.Outbound);
            if (!ValidAmount(amount) || amount > Available(Resource) || !FitsPromises(ReservedOut(Resource) + amount))
                throw new InvalidOperationException("spatial receipt growth exceeds actual compatible stock");
            PreflightBook(); _book.Increase(Endpoint, owner, token, amount); _version++;
        }
        public void ReleaseOut(ReservationOwner owner, ReservationToken token)
        { _book.Get(Endpoint, owner, token, ReservationDirection.Outbound); PreflightBook(); _book.Release(Endpoint, owner, token, ReservationDirection.Outbound); _version++; }
        public ResourceReceiptCheck CheckReservedOut(ReservationOwner owner, ReservationToken token)
        {
            ResourceReceiptCheck check = _book.Check(Endpoint, owner, token, ReservationDirection.Outbound);
            if (!check.Ready) return check;
            if (check.Record.Resource != Resource || !ValidAmount(check.Record.Amount)) return new ResourceReceiptCheck(ResourceTransferStatus.Incompatible, check.Record);
            return ReservedOut(Resource) <= Quantity ? check : new ResourceReceiptCheck(ResourceTransferStatus.NoStock, check.Record);
        }
        public Fix TakeReserved(ReservationOwner owner, ReservationToken token, Fix amount)
        {
            ResourceReceiptCheck check = CheckReservedOut(owner, token);
            if (!check.Ready || amount > check.Record.Amount || !ValidAmount(amount)) throw new InvalidOperationException("invalid bounded spatial receipt debit");
            var prepared = EntityObservation.ReadProvider(_world, _quantity, null,
                () => _quantity.PrepareWithdrawal(_location, amount), space: _world.Space);
            if (prepared == null || !prepared.Check.Ready || prepared.Check.Amount != amount ||
                prepared.Check.Before != Quantity || prepared.Check.NativeUnit != Layer.Resource.QuantityUnit.Unit)
                throw new InvalidOperationException("spatial source refused exact reserved withdrawal preparation");
            PreflightBook(); _committing = amount;
            try
            {
                SpatialResourceChange moved = prepared.Commit();
                if (!moved.Committed || moved.Amount != amount) throw new InvalidOperationException("spatial source violated exact owned material debit");
                _book.Consume(Endpoint, owner, token, ReservationDirection.Outbound, amount);
                return amount;
            }
            finally { _committing = Fix.Zero; }
        }
        public Fix Take(ResourceId resource, Fix amount)
            => resource == Resource && amount <= Available(resource) && ValidAmount(amount) && _quantity.Withdraw(_location, amount).Committed ? amount : Fix.Zero;
        public CapacityCheck CheckIncoming(IReadOnlyList<ResourceAmount> incoming)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            foreach (var line in incoming) if (line.Resource != Resource) return new CapacityCheck(CapacityStatus.ResourceRejected, line.Resource);
            var check = CapacityPolicyContracts.Check(CapacityPolicy, StockAt(Quantity), InboundPromises, incoming);
            if (!check.Fits) return check;
            Fix added = ReservedIn(Resource);
            try { foreach (var line in incoming) added += line.Amount; }
            catch (OverflowException) { return new CapacityCheck(CapacityStatus.OutOfRange, Resource); }
            return !added.IsPositive || DepositCheck(added, Quantity).Ready ? check : new CapacityCheck(CapacityStatus.Unrepresentable, Resource);
        }
        public Fix Offer(ResourceId resource, Fix ceiling)
        {
            if (resource != Resource) return Fix.Zero;
            Fix offered = CapacityPolicyContracts.Offer(CapacityPolicy, StockAt(Quantity), InboundPromises, resource, ceiling).Amount;
            return SpatialResourceValues.DepositOffer(Layer, Quantity, ReservedIn(resource), offered, CapacityPolicyContracts.Quantum(CapacityPolicy, resource));
        }
        public Fix Receive(ResolvedContent content, ResourceId resource, Fix amount)
        {
            if (!Accepts(content, resource) || !amount.IsPositive || UnitConversion.ValidateAmount(Layer.Resource.QuantityUnit, amount) != UnitConversionStatus.Converted) return Fix.Zero;
            Fix accepted = Offer(resource, amount);
            return accepted.IsPositive && _quantity.Deposit(_location, accepted).Committed ? accepted : Fix.Zero;
        }
        public bool CanReserveIn(ReservationOwner owner, ResourceId resource, Fix amount)
            => resource == Resource && _book.CanReserve(owner, resource, ReservationDirection.Inbound) && amount.IsPositive && Offer(resource, amount) == amount;
        public ReservationToken ReserveIn(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (!CanReserveIn(owner, resource, amount)) throw new InvalidOperationException("invalid bounded spatial inbound commitment");
            PreflightBook(); var token = _book.Add(Endpoint, owner, resource, amount, ReservationDirection.Inbound); _version++; return token;
        }
        public void ReleaseIn(ReservationOwner owner, ReservationToken token)
        { _book.Get(Endpoint, owner, token, ReservationDirection.Inbound); PreflightBook(); _book.Release(Endpoint, owner, token, ReservationDirection.Inbound); _version++; }
        public ResourceReceiptCheck CheckReservedIn(ResolvedContent content, ReservationOwner owner, ReservationToken token)
        {
            var check = _book.Check(Endpoint, owner, token, ReservationDirection.Inbound);
            if (!check.Ready) return check;
            if (!Accepts(content, check.Record.Resource)) return new ResourceReceiptCheck(ResourceTransferStatus.Incompatible, check.Record);
            return FitsIncoming(Quantity, ReservedIn(Resource)) ? check : new ResourceReceiptCheck(ResourceTransferStatus.NoCapacity, check.Record);
        }
        public Fix ReceiveReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, Fix amount)
        {
            var check = CheckReservedIn(content, owner, token);
            if (!check.Ready || !amount.IsPositive || amount > check.Record.Amount ||
                CapacityPolicyContracts.Quantum(CapacityPolicy, Resource).Floor(amount) != amount || !DepositCheck(amount, Quantity).Ready)
                throw new InvalidOperationException("invalid bounded spatial receipt credit");
            var prepared = EntityObservation.ReadProvider(_world, _quantity, null,
                () => _quantity.PrepareDeposit(_location, amount), space: _world.Space);
            if (prepared == null || !prepared.Check.Ready || prepared.Check.Amount != amount ||
                prepared.Check.Before != Quantity || prepared.Check.NativeUnit != Layer.Resource.QuantityUnit.Unit)
                throw new InvalidOperationException("spatial destination refused exact reserved deposit preparation");
            PreflightBook(); _receiving = amount;
            try
            {
                var moved = prepared.Commit();
                if (!moved.Committed || moved.Amount != amount) throw new InvalidOperationException("spatial destination violated exact owned material credit");
                _book.Consume(Endpoint, owner, token, ReservationDirection.Inbound, amount);
                return amount;
            }
            finally { _receiving = Fix.Zero; }
        }
        public Action StageSync(IStateSync sync, Fix nativeQuantity)
        {
            // Pure guards fingerprint actual pre-publication state through this SAME codec.
            // Active markers still prohibit checkpoints/normal hashes, and every original
            // stock/book/capacity validation below remains mandatory for observations too.
            bool observation = sync is IStateObservationSync probe && probe.IsObservation && !sync.Reading;
            if ((_committing.IsPositive || _receiving.IsPositive) && !observation)
                throw new CorruptStateException("spatial resource commit is not settled");
            ResourceEndpointId endpoint = Endpoint; ResourceEndpointId.Sync(sync, ref endpoint);
            if (endpoint != Endpoint) throw new CorruptStateException("saved spatial endpoint differs from authored identity");
            int version = _version; sync.Int(ref version);
            if (version < 0) throw new CorruptStateException("invalid spatial endpoint version");
            ResourceReservationBook book = sync.Reading ? new ResourceReservationBook() : _book;
            book.Sync(sync, Endpoint);
            try
            {
                if (book.Amount(Resource, ReservationDirection.Outbound) > nativeQuantity || !FitsPromises(book.Amount(Resource, ReservationDirection.Outbound)))
                    throw new CorruptStateException("spatial promises exceed actual candidate stock/policy");
                Fix incoming = book.Amount(Resource, ReservationDirection.Inbound);
                if (incoming.IsPositive && !FitsIncoming(nativeQuantity, incoming))
                    throw new CorruptStateException("spatial promises exceed actual candidate capacity/representation");
                foreach (ResourceReservation record in book.Records)
                    if (record.Resource != Resource ||
                        CapacityPolicyContracts.Quantum(CapacityPolicy, Resource).Floor(record.Amount) != record.Amount ||
                        !(record.Direction == ReservationDirection.Outbound ? MaterialCheck(record.Amount, nativeQuantity).Ready :
                            record.Direction == ReservationDirection.Inbound && DepositCheck(record.Amount, nativeQuantity).Ready))
                        throw new CorruptStateException("invalid saved spatial native receipt");
            }
            catch (OverflowException) { throw new CorruptStateException("spatial promises overflow native quantity"); }
            return sync.Reading ? (Action)(() => { _book = book; _version = version; }) : null;
        }
    }

    public interface ISpatialResourceEndpoints : IResourceEndpointProvider
    {
        IResourceOutlet ResolveCell(ISpatialResourceCellDefinition definition);
    }
    /// <summary>Pure aliases of cells embedded in actual layers. Not a persistent stock/book owner.</summary>
    public sealed class SpatialResourceEndpoints : ISpatialResourceEndpoints
    {
        private readonly SortedDictionary<long, IResourceOutlet> _cells = new SortedDictionary<long, IResourceOutlet>();
        public World World { get; }
        public string EndpointScope => SpatialResourcesModule.ModuleId;
        public SpatialResourceEndpoints(World world)
        {
            World = world;
            var admitted = new List<(ISpatialResourceCellDefinition Definition, long Identity, ISpatialLayerDefinition Layer, ICapacityPolicy Capacity, bool Public)>();
            var identities = new HashSet<long>();
            foreach (ISpatialResourceCellDefinition definition in world.Content.All<ISpatialResourceCellDefinition>())
            {
                var metadata = EntityObservation.ReadProvider(world, definition, null, () => {
                    if (!(definition is Def actual) || !actual.IsFrozen || !world.Content.Owns(definition) ||
                        definition.Id != actual.Id || definition.Module != actual.Module ||
                        !ReferenceEquals(world.Content.Find<ISpatialResourceCellDefinition>(actual.Id), definition))
                        throw new ContentException("spatial cell requires its actual frozen catalog alias");
                    long declared = definition.Identity;
                    ISpatialLayerDefinition layer = definition.Layer; ICapacityPolicy capacity = definition.Capacity; bool isPublic = definition.Public;
                    SpatialResourceCellDeclaration.ValidateMetadata(world.Content, declared, layer, capacity, isPublic);
                    definition.ValidateComposition(world); return (definition, declared, layer, capacity, isPublic);
                });
                if (!identities.Add(metadata.declared)) throw new ContentException("duplicate authored spatial endpoint identity");
                admitted.Add(metadata);
            }
            foreach (var metadata in admitted)
            {
                var endpoint = new ResourceEndpointId(EndpointScope, metadata.Identity);
                var observation = EntityObservation.Capture(world);
                IResourceOutlet bound = metadata.Definition.Bind(world, endpoint); observation.Verify();
                if (bound == null || bound.Endpoint != endpoint || !ReferenceEquals(bound.CapacityPolicy, metadata.Capacity) ||
                    bound.Public != metadata.Public || !(bound is ISpatialResourceCellGuard guard) || guard.Layer.Id != metadata.Layer.Id)
                    throw new ContentException("spatial cell binding differs from its actual endpoint/layer/capacity/access");
                _cells.Add(metadata.Identity, bound);
            }
        }
        public IResourceEndpoint Resolve(ResourceEndpointId endpoint)
            => endpoint.Scope == EndpointScope && _cells.TryGetValue(endpoint.Identity, out var cell) ? cell : null;
        public IResourceOutlet ResolveCell(ISpatialResourceCellDefinition definition)
        {
            if (definition == null || !World.Content.Owns(definition)) return null;
            return Resolve(new ResourceEndpointId(EndpointScope, definition.Identity)) as IResourceOutlet;
        }
        public void InspectReservations(Action<ResourceEndpointId, IReadOnlyList<ResourceReservation>> inspect)
        { foreach (var cell in _cells.Values) inspect(cell.Endpoint, cell.Reservations); }
    }

    public enum SpatialResourceTransferDirection : byte { Withdraw=0, Deposit=1 }
    public interface ISpatialResourceTransferEffectDefinition : IPhysicalWorkEffect, IIncidentExposureEffect
    {
        ISpatialResourceCellDefinition Cell { get; }
        Bill Quantities { get; }
        SpatialResourceTransferDirection Direction { get; }
    }
    /// <summary>One strict file declaration for native and independent transfer metadata.</summary>
    public sealed class SpatialResourceTransferDeclaration
    {
        private readonly string _cell;
        private readonly BillSpec _quantities;
        public ISpatialResourceCellDefinition Cell { get; private set; }
        public Bill Quantities { get; private set; }
        public SpatialResourceTransferDirection Direction { get; }
        private SpatialResourceTransferDeclaration(JsonReader reader)
        {
            _cell=reader.String("cell");_quantities=BillSpec.Read(reader,"quantities");
            string operation=reader.String("operation");
            Direction=operation=="withdraw"?SpatialResourceTransferDirection.Withdraw:operation=="deposit"?SpatialResourceTransferDirection.Deposit:
                throw reader.Error("cell transfer requires explicit withdraw or deposit");
            if(string.IsNullOrWhiteSpace(_cell)||_quantities.IsEmpty)throw reader.Error("cell transfer requires explicit cell and nonempty native bill");
        }
        public static SpatialResourceTransferDeclaration Read(JsonReader reader)=>new SpatialResourceTransferDeclaration(reader??throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content){Cell=content.Def<ISpatialResourceCellDefinition>(_cell);Quantities=_quantities.Resolve(content);}
        public void Validate(ContentResolver content)
        {
            SpatialResourceCellDeclaration.ValidateMetadata(content.Content,Cell.Identity,Cell.Layer,Cell.Capacity,Cell.Public);
            if(Quantities.Count!=1||Quantities[0].Resource!=Cell.Layer.Resource.Resource)
                throw content.Fail("cell transfer bill must name its actual linked quantity resource");
        }
    }
    /// <summary>Sole cell/inventory movement rule; actual stock and receipt authority remain in ResourceTransactions.</summary>
    public static class SpatialResourceTransferContracts
    {
        public static IReadOnlyList<IContentDefinition> References(ISpatialResourceTransferEffectDefinition effect)
            =>Array.AsReadOnly(new IContentDefinition[]{effect.Cell,effect.Cell.Layer,effect.Cell.Layer.Space,(IResourceContentDefinition)effect.Cell.Layer.Resource});
        public static void RequireCapabilities(World world,ISpatialResourceTransferEffectDefinition effect)
        {
            EntityObservation.ReadProvider(world,effect,null,()=>{
                if(effect is not Def actual||!actual.IsFrozen||!world.Content.Owns(effect)||
                    !ReferenceEquals(world.Content.Find<ISpatialResourceTransferEffectDefinition>(effect.Id),effect)||
                    !ReferenceEquals(world.Content.Find<IWorkEffect>(effect.Id),effect)||effect.Cell==null||
                    !ReferenceEquals(world.Content.Find<ISpatialResourceCellDefinition>(effect.Cell.Id),effect.Cell)||
                    effect.Direction!=SpatialResourceTransferDirection.Withdraw&&effect.Direction!=SpatialResourceTransferDirection.Deposit||
                    effect.Quantities==null||effect.Quantities.Count!=1||effect.Quantities[0].Resource!=effect.Cell.Layer.Resource.Resource||
                    !world.Services.Has<IResourceTransaction>()||!world.Services.Has<ISpatialResourceEndpoints>())
                    throw new ContentException("cell transfer requires actual immutable metadata and selected endpoints/transactions");
                SpatialResourceCellDeclaration.ValidateMetadata(world.Content,effect.Cell.Identity,effect.Cell.Layer,effect.Cell.Capacity,effect.Cell.Public);
                var quantity=effect.Quantities[0];var quantum=CapacityPolicyContracts.Quantum(effect.Cell.Capacity,quantity.Resource);
                if(!quantity.Amount.IsPositive||quantum.Status!=CapacityStatus.Fits||quantum.Raw<=0||quantum.Floor(quantity.Amount)!=quantity.Amount||
                    world.Services.Get<ISpatialResourceEndpoints>().ResolveCell(effect.Cell)==null)
                    throw new ContentException(effect.Id+": invalid actual cell/native transfer quantity");
                return true;
            },world.Space);
        }
        private static ResourceMovementIntent Intent(ISpatialResourceTransferEffectDefinition effect,Entity target,ReservationOwner owner,IResourceOutlet cell)
        {
            var local=ResourceEndpointId.ForEntity(target.Id);bool deposit=effect.Direction==SpatialResourceTransferDirection.Deposit;
            return new ResourceMovementIntent(owner,target.Owner,deposit?local:cell.Endpoint,deposit?cell.Endpoint:local,
                effect.Quantities,Array.Empty<ResourceReservation>(),Array.Empty<ResourceReservation>(),Array.Empty<ResourceReservation>(),ResourceMovementParticipantDisposition.Retain);
        }
        public static Code Check(World world,ISpatialResourceTransferEffectDefinition effect,Entity target,ReservationOwner owner,out ResourceMovementIntent intent)
        {
            intent=default;RequireCapabilities(world,effect);
            if(target==null||!target.Alive||!ReferenceEquals(world.Entities.Get(target.Id),target))return Codes.NotFound;
            Code location=ReadSite(world,effect.Cell,target);
            if(!location.IsNone)return location;
            if(target.Get<IMotion>()?.Status==MoveStatus.Moving)return WorkEffectCodes.MovingSource;
            var cell=world.Services.Get<ISpatialResourceEndpoints>().ResolveCell(effect.Cell);
            if(cell==null)return Codes.NotFound;
            if(cell.Company!=target.Owner&&!cell.Public)return Codes.NotOwned;
            intent=Intent(effect,target,owner,cell);
            return Refusal(world.Services.Get<IResourceTransaction>().EvaluateMovement(intent).Status);
        }
        // Site predicates are observations, including the fresh check after an incident is raised.
        // The complete target codec catches private payload changes that do not touch its revision.
        internal static Code ReadSite(World world,ISpatialResourceCellDefinition cell,Entity target)
            =>EntityObservation.ReadProvider(world,cell,target,()=>cell.CheckSite(world,target),world.Space,observeTargetState:true);
        public static Code Refusal(ResourceTransferStatus status)
        {
            if(status==ResourceTransferStatus.Ready||status==ResourceTransferStatus.Committed)return Code.None;
            if(status==ResourceTransferStatus.NoStock)return WorkEffectCodes.NoStock;
            if(status==ResourceTransferStatus.NoCapacity||status==ResourceTransferStatus.Incompatible)return WorkEffectCodes.IncompatibleInventory;
            return status==ResourceTransferStatus.MissingEndpoint?Codes.NotFound:Codes.InvalidArgument;
        }
        public static ReservationOwner ExposureOwner(ISpatialResourceTransferEffectDefinition effect,IncidentPropagationContext context,Entity target)
            =>new ReservationOwner(FormattableString.Invariant($"sim.incident-exposure:{effect.Id.Length}:{effect.Id}:{context.Source.Id.Value}:{target.Id.Value}"),context.Incident.Episode);
    }
    public sealed class SpatialResourceTransferEffectDef : Def,ISpatialResourceTransferEffectDefinition
    {
        private SpatialResourceTransferDeclaration _declaration;
        public ISpatialResourceCellDefinition Cell=>_declaration.Cell;
        public Bill Quantities=>_declaration.Quantities;
        public SpatialResourceTransferDirection Direction=>_declaration.Direction;
        public IReadOnlyList<IContentDefinition> ContentDependencies=>SpatialResourceTransferContracts.References(this);
        protected override void ReadCore(JsonReader reader)=>_declaration=SpatialResourceTransferDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content)=>_declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content)=>_declaration.Validate(content);
        public void RequireCapabilities(World world)=>SpatialResourceTransferContracts.RequireCapabilities(world,this);
        public Code CheckSubject(World world,Entity target,Order order,IActivityNodeDefinition node)
            =>SpatialResourceTransferContracts.Check(world,this,target,new ReservationOwner("sim.work",order.Id),out _);
        public Code CommitSubject(World world,Entity target,Order order,IActivityNodeDefinition node)
        {
            Code refusal=SpatialResourceTransferContracts.Check(world,this,target,new ReservationOwner("sim.work",order.Id),out var intent);
            return !refusal.IsNone?refusal:SpatialResourceTransferContracts.Refusal(world.Services.Get<IResourceTransaction>().CommitMovement(intent).Status);
        }
        public Entity ResolveSubject(World world,Order order,IActivityNodeDefinition node)=>world.Entities.Get(order.Target);
        public void ValidatePlan(World world,IActivityDefinition plan,IActivityNodeDefinition node)=>PhysicalWorkEffectContracts.ValidatePlan(world,this,plan,node);
        public Code Check(World world,Order order,IActivityNodeDefinition node)=>PhysicalWorkEffectContracts.Check(world,this,order,node);
        public WorkEffectResult Apply(World world,Order order,IActivityNodeDefinition node)=>PhysicalWorkEffectContracts.Apply(world,this,order,node);
        public Code CheckExposure(IncidentPropagationContext context,Entity target)
        {
            if(Direction!=SpatialResourceTransferDirection.Deposit||context.World==null||context.Source==null||!context.Incident.Active||target==null)
                return Codes.InvalidArgument;
            return SpatialResourceTransferContracts.Check(context.World,this,target,SpatialResourceTransferContracts.ExposureOwner(this,context,target),out _);
        }
        public IncidentExposurePreparation PrepareExposure(IncidentPropagationContext context,Entity target)
        {
            Code refusal=CheckExposure(context,target);if(!refusal.IsNone)return new IncidentExposurePreparation(refusal);
            var world=context.World;var owner=SpatialResourceTransferContracts.ExposureOwner(this,context,target);
            refusal=SpatialResourceTransferContracts.Check(world,this,target,owner,out var intent);
            if(!refusal.IsNone)return new IncidentExposurePreparation(refusal);
            long step=world.Clock.StepIndex;PlayerId company=target.Owner;
            return new IncidentExposurePreparation(()=>{
                world.RequireHealthyUpdate();
                if(world.Clock.StepIndex!=step||!ReferenceEquals(world.Entities.Get(target.Id),target)||target.Owner!=company||
                    !SpatialResourceTransferContracts.ReadSite(world,Cell,target).IsNone)throw new InvalidOperationException("prepared spill no longer names its admitted physical site");
                var moved=world.Services.Get<IResourceTransaction>().CommitMovement(intent);
                if(moved.Status!=ResourceTransferStatus.Committed)throw new InvalidOperationException("spill transfer violated complete movement preflight: "+moved.Status);
            });
        }
    }
}
