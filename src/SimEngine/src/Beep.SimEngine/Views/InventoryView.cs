using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class InventoryView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal InventoryView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private bool Visible(Entity entity) => entity != null && _world.ViewFor(_viewer).CanSee(entity);

        /// <summary>Private detached lot partitions of the same local/named inventory. No cached totals.</summary>
        public bool TryGetLots(EntityId entity, Code port, out IReadOnlyList<ResourceLotAmount> lots,
            Code quality = default, Fix minimum = default, Fix maximum = default, ResourceEndpointId? original = null)
        {
            lots = System.Array.Empty<ResourceLotAmount>(); _world.RequireHealthyUpdate();
            var store = ResolvePrivateStore(entity, port, original, out Entity live);
            if (!(store is IResourceLotEndpoint source)) return false;
            var selection = ReadStore(live, store, () => {
                if (!quality.IsNone && (!ResourceLotSelection.ValidRange(_world.Content, quality, minimum, maximum) ||
                    !_world.ViewFor(_viewer).CanSeeContent(_world.Content.Get<IResourceQualityDefinition>(quality.Id))))
                    return (Ready: false, Lots: (IReadOnlyList<ResourceLotAmount>)System.Array.Empty<ResourceLotAmount>());
                var detached = new List<ResourceLotAmount>(source.Lots).AsReadOnly();
                return (Ready: true, Lots: quality.IsNone ? (IReadOnlyList<ResourceLotAmount>)detached
                    : ResourceLotSelection.Select(_world.Content, detached, quality, minimum, maximum));
            });
            if (!selection.Ready) return false;
            var facts = selection.Lots;
            foreach (var lot in facts) if (!ResourceLotSelection.LotVisible(_world, _viewer, lot)) return false;
            lots = facts; return true;
        }

        /// <summary>Actual owned named endpoints, detached and ordinally ordered for deterministic player decisions.</summary>
        public bool CopyResourcePortIds(EntityId entity, IList<Code> destination)
        {
            if (destination == null) throw new System.ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate(); Entity live = _world.Entities.Get(entity);
            if (!Visible(live) || _viewer.IsValid && live.Owner != _viewer) return false;
            var ids = new List<Code>();
            foreach (Component component in live.Components)
                if (component is IResourcePort port)
                    ids.Add(EntityObservation.ReadProvider(_world, port, live, () => {
                        Code identity = port.CapabilityId;
                        if (identity.IsNone || !ReferenceEquals(live.Get<IResourcePort>(identity), port) ||
                            live.Def.Component<IResourcePortDefinition>(identity) == null)
                            throw new System.InvalidOperationException("resource port identity must name its actual entity and metadata");
                        return identity;
                    }, _world.Space, observeTargetState: true));
            ids.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            _world.RequireHealthyUpdate(); foreach (Code identity in ids) destination.Add(identity);
            return true;
        }

        public Code ReadResourcePortChoices(EntityId entity,Code port,IList<ResourcePortChoiceSnapshot> destination)
        {
            destination.Clear();_world.RequireHealthyUpdate();Entity live=_world.Entities.Get(entity);
            if(!Visible(live))return Codes.NotFound;
            if(_viewer.IsValid&&live.Owner!=_viewer)return Codes.NotOwned;
            return port.IsNone?Codes.InvalidArgument:ResourcePortSelectionContracts.CopyChoices(_world,live.Get<IResourcePortSelection>(port),destination);
        }

        /// <summary>Detached facts of an explicitly named actual store, regardless of entity/service stock ownership.</summary>
        public Code ReadResourcePort(EntityId entity, Code port, ResourceId resource, out ResourcePortSnapshot snapshot)
        {
            snapshot = default; _world.RequireHealthyUpdate(); Entity live = _world.Entities.Get(entity);
            if (!Visible(live)) return Codes.NotFound;
            if (_viewer.IsValid && live.Owner != _viewer) return Codes.NotOwned;
            if (port.IsNone || _world.Content.Resource(resource) == null) return Codes.InvalidArgument;
            if (!_world.ViewFor(_viewer).CanSeeContent(_world.Content.Resource(resource))) return Codes.NotFound;
            IResourcePort provider = live.Get<IResourcePort>(port);
            if (provider == null || !Accepts(live.Def, resource, port, live))
                return StandardCodes.NotAccepted;
            snapshot = EntityObservation.ReadProvider(_world, provider, live, () => ResourcePortContracts.Facts(new[] {
                ResourcePortContracts.Bind(_world, live, resource, provider) })[0]);
            return Code.None;
        }

        /// <summary>Detached actual quantity. None selects the local inlet; a named/original binding never substitutes another store.</summary>
        public bool TryGetAmount(EntityId entity, ResourceId resource, out Fix amount, Code port = default, ResourceEndpointId? original = null)
        {
            amount = Fix.Zero; _world.RequireHealthyUpdate();
            var definition = _world.Content.Resource(resource);
            if (definition == null || !_world.ViewFor(_viewer).CanSeeContent(definition)) return false;
            IResourceInlet store = ResolvePrivateStore(entity, port, original, out Entity live);
            if (store == null) return false;
            Fix value = ReadStore(live, store, () => {
                Fix current = store.Amount(resource);
                if (UnitConversion.ValidateAmount(definition.QuantityUnit, current) != UnitConversionStatus.Converted)
                    throw new System.InvalidOperationException("inventory quantity violates its actual resource unit");
                return current;
            });
            amount = value; return true;
        }

        /// <summary>Private actual store capacity. None selects only the local inlet; a named
        /// port and optional original select that exact binding, never a replacement store.</summary>
        public bool TryGetCapacity(EntityId entity, Code port, out CapacityCheck capacity, ResourceEndpointId? original = null)
        {
            capacity = default; _world.RequireHealthyUpdate();
            IResourceInlet store = ResolvePrivateStore(entity, port, original, out Entity live);
            if (store == null) return false;
            capacity = ReadStore(live, store, () => {
                var value = store.CheckIncoming(System.Array.Empty<ResourceAmount>());
                if (!System.Enum.IsDefined(typeof(CapacityStatus), value.Status))
                    throw new System.InvalidOperationException("inventory capacity returned an unknown status");
                return value;
            });
            return true;
        }

        /// <summary>Detached private records from the same explicit local/named/original store.</summary>
        public bool TryGetReservations(EntityId entity, Code port, out IReadOnlyList<ResourceReservation> reservations, ResourceEndpointId? original = null)
        {
            reservations = System.Array.Empty<ResourceReservation>(); _world.RequireHealthyUpdate();
            IResourceInlet store = ResolvePrivateStore(entity, port, original, out Entity live);
            if (store == null) return false;
            var facts = ReadStore(live, store, () => {
                IReadOnlyList<ResourceReservation> records = store.Reservations;
                if (records == null) throw new System.InvalidOperationException("inventory reservations must be explicit");
                var copy = new ResourceReservation[records.Count];
                for (int i = 0; i < copy.Length; i++) copy[i] = records[i];
                return System.Array.AsReadOnly(copy);
            });
            foreach (ResourceReservation record in facts)
                if (!_world.ViewFor(_viewer).CanSeeContent(_world.Content.Resource(record.Resource))) return false;
            reservations = facts; return true;
        }

        private IResourceInlet ResolvePrivateStore(EntityId entity, Code port, ResourceEndpointId? original, out Entity live)
        {
            live = _world.Entities.Get(entity);
            if (!Visible(live) || _viewer.IsValid && live.Owner != _viewer) return null;
            IResourceInlet store;
            if (port.IsNone) store = live.Get<IInletPort>();
            else
            {
                IResourcePort named = live.Get<IResourcePort>(port);
                if (named == null) return null;
                var resolution = EntityObservation.ReadProvider(_world, named, live,
                    () => ResourcePortContracts.Store(_world, named.Entity, named, original), _world.Space, observeTargetState: true);
                store = resolution.Ready ? resolution.Store : null;
            }
            if (store == null) return null;
            return ReadStore(live, store, () =>
                (!original.HasValue || store.Endpoint == original.Value) &&
                (_viewer == PlayerId.None || store.Company == _viewer) ? store : null);
        }

        private T ReadStore<T>(Entity live, IResourceInlet store, System.Func<T> read)
            => EntityObservation.ReadProvider(_world, store, live, () => {
                // A returned endpoint can be a facade with no codec. Its registered owner
                // persists the real pool, including siblings whose versions this facade
                // does not expose. Observe original services before even endpoint getters.
                var owners = new List<EntityObservation.ProviderScope>();
                foreach (IResourceEndpointProvider provider in _world.Services.Providers<IResourceEndpointProvider>())
                    if (provider is IPersistentService)
                        owners.Add(EntityObservation.CaptureProvider(_world, provider));
                int version = store.Version; ResourceEndpointId endpoint = store.Endpoint; PlayerId company = store.Company;
                ICapacityPolicy policy = store.CapacityPolicy;
                if (version < 0 || !endpoint.IsValid || policy == null)
                    throw new System.InvalidOperationException("inventory view requires actual endpoint identity and capacity policy");
                T value = read();
                if (store.Version != version || store.Endpoint != endpoint || store.Company != company ||
                    !ReferenceEquals(store.CapacityPolicy, policy))
                    throw new System.InvalidOperationException("inventory endpoint mutated a pure commitment observation");
                foreach (var owner in owners) owner.Verify();
                return value;
            }, _world.Space, observeTargetState: true);

        private bool Accepts(EntityDef definition, ResourceId resource, Code port, Entity target = null)
        {
            if (port.IsNone)
            {
                IInletPortDefinition local = definition.Component<IInletPortDefinition>();
                return local != null && EntityObservation.ReadProvider(_world, local, target,
                    () => local.Accepts(_world.Content, resource), _world.Space, observeTargetState: target != null);
            }
            IResourcePortDefinition named = definition.Component<IResourcePortDefinition>(port);
            return named != null && EntityObservation.ReadProvider(_world, named, target,
                () => named.Accepts(_world.Content, resource), _world.Space, observeTargetState: target != null);
        }
        /// <summary>Compatibility of an explicitly selected actual store; None selects the local inlet, never a named fallback.</summary>
        public bool StoreAccepts(string def, ResourceId resource, Code port)
        {
            _world.RequireHealthyUpdate();
            EntityDef definition = _world.Content.Find<EntityDef>(def);
            if (!_world.ViewFor(_viewer).CanSeeContent(definition) || !_world.ViewFor(_viewer).CanSeeContent(_world.Content.Resource(resource))) return false;
            return Accepts(definition, resource, port);
        }

    }
}
