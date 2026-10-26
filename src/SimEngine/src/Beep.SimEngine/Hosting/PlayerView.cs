using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Hosting
{
    /// <summary>An entity as a player sees it on the map. A value copy: holding it changes nothing.</summary>
    public struct EntityView
    {
        public EntityId Id;
        /// <summary>Entity def id.</summary>
        public string Def;
        /// <summary>Presentation key the host maps to a sprite, scene or prefab.</summary>
        public string Visual;
        public EntityKind Kind;
        public PlayerId Owner;
        public SpatialSnapshot Spatial;
        public Code State;
        public long StateSince;
        public int Version;
        public string Category;

        public override string ToString() => Def + Id + " " + State;
        internal bool Matches(World world, EntityView other)
        {
            bool sameSpatial = EntityObservation.MatchesSpatial(world, world.Entities.Get(Id), Spatial, other.Spatial);
            return Id == other.Id && Def == other.Def && Visual == other.Visual && Kind == other.Kind && Owner == other.Owner &&
                State == other.State && StateSince == other.StateSince && Version == other.Version && Category == other.Category && sameSpatial;
        }
    }

    /// <summary>A def a player can build or buy, as the catalogue lists it.</summary>
    public struct CatalogueEntry
    {
        public string Id;
        public EntityKind Kind;
        public string Category;
        public string Visual;
        public string[] Tags;
    }

    /// <summary>Immutable authored unit facts, without a provider or catalog writer.</summary>
    public readonly struct UnitInfo : IUnitDefinition
    {
        public UnitId Unit { get; }
        public string Dimension { get; }
        public long ScaleNumerator { get; }
        public long ScaleDenominator { get; }
        public bool Discrete { get; }
        internal UnitInfo(IUnitDefinition unit)
        {
            Unit = unit.Unit; Dimension = unit.Dimension; ScaleNumerator = unit.ScaleNumerator;
            ScaleDenominator = unit.ScaleDenominator; Discrete = unit.Discrete;
        }
    }
    /// <summary>Immutable resource facts copied from the single catalog.</summary>
    public readonly struct ResourceInfo : IResourceDefinition
    {
        public ResourceId Resource { get; }
        public string Id { get; }
        public string Category { get; }
        public UnitInfo QuantityUnit { get; }
        IUnitDefinition IResourceDefinition.QuantityUnit => QuantityUnit;
        public Code Phase { get; }
        public IReadOnlyList<string> Traits { get; }
        public IReadOnlyList<ResourceCapacityMeasure> CapacityMeasures { get; }
        public bool HasTrait(string trait) => ResourceDeclaration.HasTrait(Traits, trait);
        internal ResourceInfo(IResourceDefinition resource)
        {
            Resource = resource.Resource; Id = resource.Id; Category = resource.Category;
            QuantityUnit = new UnitInfo(resource.QuantityUnit); Phase = resource.Phase;
            var sourceTraits = resource.Traits;
            var traits = new string[sourceTraits.Count];
            for (int i = 0; i < traits.Length; i++) traits[i] = sourceTraits[i];
            Traits = Array.AsReadOnly(traits);
            var sourceMeasures = resource.CapacityMeasures;
            var measures = new ResourceCapacityMeasure[sourceMeasures.Count];
            for (int i = 0; i < measures.Length; i++)
                measures[i] = new ResourceCapacityMeasure(new UnitInfo(sourceMeasures[i].Unit), sourceMeasures[i].PerQuantity);
            CapacityMeasures = Array.AsReadOnly(measures);
        }
    }

    /// <summary>Detached native rate facts; never an executable rate or content provider.</summary>
    public readonly struct ResourceRateInfo
    {
        public ResourceInfo Resource { get; }
        public Fix Quantity { get; }
        public long PeriodNumerator { get; }
        public long PeriodDenominator { get; }
        public DurationUnit PeriodUnit { get; }
        internal ResourceRateInfo(IResourceRate rate)
        {
            Resource = new ResourceInfo(rate.Resource); Quantity = rate.Quantity;
            PeriodNumerator = rate.PeriodNumerator; PeriodDenominator = rate.PeriodDenominator;
            PeriodUnit = rate.PeriodUnit;
        }
    }

    public readonly struct CapacityLimitInfo
    {
        public UnitInfo Unit { get; }
        public Fix Amount { get; }
        internal CapacityLimitInfo(CapacityLimit limit) { Unit = new UnitInfo(limit.Unit); Amount = limit.Amount; }
    }
    /// <summary>Authored facts only; never an executable policy or live buffer writer.</summary>
    public readonly struct CapacityPolicyInfo
    {
        public string Id { get; }
        public string Module { get; }
        public IReadOnlyList<CapacityLimitInfo> Limits { get; }
        internal CapacityPolicyInfo(ICapacityPolicyDefinition policy)
        {
            Id = policy.Id; Module = policy.Module;
            var limits = new CapacityLimitInfo[policy.Limits.Count];
            for (int i = 0; i < limits.Length; i++) limits[i] = new CapacityLimitInfo(policy.Limits[i]);
            Limits = Array.AsReadOnly(limits);
        }
    }

    /// <summary>
    /// Everything one seat may see, and nothing else. Bound to its viewer; every answer is a copy. Game
    /// modules add richer read models as features (<see cref="Feature{T}"/>) — a company panel, a
    /// research tree — built for this viewer. Reading never changes the game.
    /// </summary>
    public sealed class PlayerView
    {
        private readonly World _world;
        private readonly IReadOnlyDictionary<Type, ViewFeatureRegistration> _featureFactories;
        private readonly Dictionary<Type, object> _features = new Dictionary<Type, object>();
        private IVisibility _visibility;
        private bool _visibilityLooked;

        internal PlayerView(World world, PlayerId viewer, IReadOnlyDictionary<Type, ViewFeatureRegistration> features)
        {
            _world = world;
            Viewer = viewer;
            _featureFactories = features;
        }

        /// <summary>
        /// A seat's view for game code that plays a seat from inside the simulation (the engine's
        /// autopilot): exactly what a human client of that seat sees, nothing more.
        /// </summary>
        internal static PlayerView Create(World world, PlayerId viewer) => new PlayerView(world, viewer, world.ViewFeatures);

        /// <summary>Whose view this is; <see cref="PlayerId.None"/> for the all-seeing observer (replays, spectators).</summary>
        public PlayerId Viewer { get; }
        public bool IsObserver => Viewer == PlayerId.None;

        public long Tick => _world.Tick;
        public ISimulationTime SimulationTime => _world.Time;
        public CalendarReading Date => _world.Date;
        public SpatialWorldSnapshot Space
        {
            get
            {
                return EntityObservation.ReadSpace(_world, Viewer);
            }
        }
        public int SeatCount => _world.Players.Count;
        public Code Outcome => _world.Outcome;
        public IReadOnlyList<EventRegistration> EventRegistrations => _world.Events.Registrations;
        public IReadOnlyList<EventSubscription> EventSubscriptions => _world.Events.Subscriptions;
        /// <summary>Captured codec declarations only; never serializers or mutable providers.</summary>
        public IReadOnlyList<StateCodecRegistration> StateCodecs => _world.StateCodecs;

        /// <summary>Immutable module-owned view identities, sorted by stable ID. Reading constructs no feature.</summary>
        public IReadOnlyList<ViewFeatureRegistration> FeatureRegistrations
        {
            get
            {
                var entries = new List<ViewFeatureRegistration>(_featureFactories.Values);
                entries.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return entries.AsReadOnly();
            }
        }

        /// <summary>A module's read model for this viewer, or null if the game has no such feature.</summary>
        public T Feature<T>() where T : class
        {
            if (!_featureFactories.TryGetValue(typeof(T), out ViewFeatureRegistration registration)) return null;
            return (T)Feature(registration);
        }

        private object Feature(ViewFeatureRegistration registration)
        {
            if (_features.TryGetValue(registration.Contract, out object value)) return value;
            value = registration.Source == null ? registration.Create(_world, Viewer) : Feature(registration.Source);
            _features.Add(registration.Contract, value);
            return value;
        }

        // ------------------------------------------------------------------ entities

        /// <summary>Current private entity facts share the same sight authority as entity snapshots.</summary>
        internal bool CanObserveOwned(Entity entity)
        {
            _world.RequireHealthyUpdate();
            return entity != null && entity.Alive && (IsObserver || _world.Players.IsSeat(Viewer) && entity.Owner == Viewer)
                && CanSee(entity);
        }

        public int EntityCount
        {
            get
            {
                _world.RequireHealthyUpdate();
                int count = 0;
                foreach (Entity entity in _world.Entities.All) if (entity.Alive && CanSee(entity)) count++;
                return count;
            }
        }

        /// <summary>Whether this viewer may see the entity (always, without a fog rule; the observer sees all).</summary>
        internal bool CanSee(Entity e)
        {
            _world.RequireHealthyUpdate();
            if (!_visibilityLooked) { _world.Services.TryGet(out _visibility); _visibilityLooked = true; }
            return IsObserver || _world.Players.IsSeat(Viewer) && (_visibility == null ||
                EntityObservation.ReadProvider(_world, _visibility, e, () => _visibility.CanSee(_world, Viewer, e),
                    space: _world.Space, observeTargetState: true));
        }

        /// <summary>Read-only disclosure of actual owned content for external engine-module projections.</summary>
        public bool CanSeeContent(IContentDefinition definition)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Content.Owns(definition)) return false;
            if (!_visibilityLooked) { _world.Services.TryGet(out _visibility); _visibilityLooked = true; }
            if (IsObserver) return true;
            if (!_world.Players.IsSeat(Viewer)) return false;
            if (_visibility == null) return true;
            // One transient iterative closure over actual content; no recursion or parallel permission policy.
            var seen = new HashSet<IContentDefinition>(); var pending = new Stack<IContentDefinition>();
            pending.Push(definition);
            while (pending.Count != 0)
            {
                IContentDefinition content = pending.Pop();
                if (!_world.Content.Owns(content))
                    throw new InvalidOperationException("metadata visibility requires actual owned referenced content");
                if (!seen.Add(content)) continue;
                var facts = EntityObservation.ReadProvider(_world, content, null, () => {
                    bool allowed = EntityObservation.ReadProvider(_world, _visibility, null,
                        () => _visibility.CanSeeContent(_world, Viewer, content), space: _world.Space);
                    var references = new List<IContentDefinition>();
                    if (!allowed) return (false, references);
                    if (content is IContentDependencyDefinition provider)
                    {
                        IReadOnlyList<IContentDefinition> dependencies = provider.ContentDependencies;
                        if (dependencies == null) throw new InvalidOperationException("metadata requires explicit content dependencies");
                        var aliases = new HashSet<(Type Kind, string Id)>();
                        foreach (IContentDefinition dependency in dependencies)
                        {
                            if (!(dependency is Def actual) || !actual.IsFrozen || dependency is IPersistentService ||
                                dependency.Id != actual.Id || dependency.Module != actual.Module ||
                                !aliases.Add((actual.GetType(), actual.Id)))
                                throw new InvalidOperationException("metadata dependencies require unique actual immutable aliases");
                            references.Add(dependency);
                        }
                    }
                    if (content is IResourceDefinition resource)
                    {
                        references.Add(resource.QuantityUnit as IContentDefinition);
                        foreach (ResourceCapacityMeasure measure in resource.CapacityMeasures) references.Add(measure.Unit as IContentDefinition);
                    }
                    if (content is ICapacityPolicyDefinition policy)
                        foreach (CapacityLimit limit in policy.Limits) references.Add(limit.Unit as IContentDefinition);
                    return (true, references);
                }, space: _world.Space);
                if (!facts.Item1) return false;
                // Reverse push preserves authored depth-first decision order without consuming the CLR stack.
                for (int i = facts.Item2.Count - 1; i >= 0; i--) pending.Push(facts.Item2[i]);
            }
            return true;
        }
        /// <summary>Every entity on the map. Clears <paramref name="dst"/> first.</summary>
        public void CopyEntities(IList<EntityView> dst)
        {
            dst.Clear();
            _world.RequireHealthyUpdate();
            var values = new List<EntityView>();
            IReadOnlyList<Entity> all = _world.Entities.All;
            for (int i = 0; i < all.Count; i++) if (all[i].Alive && CanSee(all[i])) values.Add(ViewOf(_world, all[i]));
            foreach (EntityView value in values) dst.Add(value);
        }

        /// <summary>Entities of one owner. Clears <paramref name="dst"/> first.</summary>
        public void CopyEntities(PlayerId owner, IList<EntityView> dst)
        {
            dst.Clear();
            _world.RequireHealthyUpdate();
            var values = new List<EntityView>();
            IReadOnlyList<Entity> all = _world.Entities.All;
            for (int i = 0; i < all.Count; i++) if (all[i].Alive && all[i].Owner == owner && CanSee(all[i])) values.Add(ViewOf(_world, all[i]));
            foreach (EntityView value in values) dst.Add(value);
        }

        public bool TryGetEntity(EntityId id, out EntityView view)
        {
            view = default;
            _world.RequireHealthyUpdate();
            Entity e = _world.Entities.Get(id);
            if (e != null && !CanSee(e)) e = null;
            view = e == null ? default : ViewOf(_world, e);
            return e != null;
        }

        /// <summary>The entity's inspect panel for this viewer. Clears <paramref name="dst"/> first; false if no such entity.</summary>
        public bool Inspect(EntityId id, IList<Facet> dst)
        {
            dst.Clear();
            _world.RequireHealthyUpdate();
            Entity e = _world.Entities.Get(id);
            if (e == null || !CanSee(e)) return false;
            var values = new List<Facet>();
            for (int i = 0; i < e.Components.Count; i++)
                if (e.Components[i] is IInspectable ins)
                    EntityObservation.ReadProvider(_world, ins, e, () => { ins.Inspect(_world, Viewer, values); return true; },
                        space: _world.Space, observeTargetState: true);
            foreach (Facet value in values) dst.Add(value);
            return true;
        }

        /// <summary>Whole resource-bearing facts use the original metadata authority, never partial redaction.</summary>
        internal bool CanSeeResources(IReadOnlyList<ResourceAmount> resources, bool knownReferencesOnly = false)
        {
            _world.RequireHealthyUpdate();
            foreach (ResourceAmount line in resources)
            {
                var definition = _world.Content.Resource(line.Resource);
                // Requested values still go unchanged to their original validator. Unknown
                // IDs are not metadata disclosures and never become valid/default resources.
                if (definition == null && knownReferencesOnly) continue;
                if (!CanSeeContent(definition)) return false;
            }
            return true;
        }

        internal static EntityView ViewOf(World world, Entity e)
        {
            world.RequireHealthyUpdate();
            SpatialSnapshot spatial = EntityObservation.ReadSpatial(world, e);
            return new EntityView
            {
                Id = e.Id, Def = e.Def.Id, Visual = e.Def.Visual, Kind = e.Kind, Owner = e.Owner, Spatial = spatial,
                State = e.State, StateSince = e.StateSince, Version = e.Version, Category = e.Def.Category
            };
        }

        // ------------------------------------------------------------------ catalogues

        /// <summary>Every entity def in the game. Clears <paramref name="dst"/> first.</summary>
        public void CopyCatalogue(IList<CatalogueEntry> dst)
        {
            dst.Clear();
            _world.RequireHealthyUpdate();
            var values = new List<CatalogueEntry>();
            IReadOnlyList<EntityDef> defs = _world.Content.All<EntityDef>();
            for (int i = 0; i < defs.Count; i++)
                if (CanSeeContent(defs[i]))
                values.Add(new CatalogueEntry
                {
                    Id = defs[i].Id, Kind = defs[i].Kind, Category = defs[i].Category, Visual = defs[i].Visual,
                    Tags = new List<string>(defs[i].Tags).ToArray()
                });
            foreach (CatalogueEntry value in values) dst.Add(value);
        }

        /// <summary>Every resource kind. Clears <paramref name="dst"/> first.</summary>
        public void CopyResources(IList<ResourceInfo> dst)
        {
            dst.Clear();
            _world.RequireHealthyUpdate();
            var values = new List<ResourceInfo>();
            IReadOnlyList<IResourceContentDefinition> resources = _world.Content.Resources;
            for (int i = 0; i < resources.Count; i++)
                if (CanSeeContent(resources[i]))
                {
                    IResourceContentDefinition resource = resources[i];
                    values.Add(EntityObservation.ReadProvider(_world, resource, null, () => new ResourceInfo(resource)));
                }
            foreach (ResourceInfo value in values) dst.Add(value);
        }

        public void CopyUnits(IList<UnitInfo> dst)
        {
            dst.Clear();
            _world.RequireHealthyUpdate();
            var values = new List<UnitInfo>();
            foreach (IUnitContentDefinition unit in _world.Content.All<IUnitContentDefinition>())
                if (CanSeeContent(unit)) values.Add(EntityObservation.ReadProvider(_world, unit, null, () => new UnitInfo(unit)));
            values.Sort((a, b) => a.Unit.CompareTo(b.Unit));
            foreach (UnitInfo value in values) dst.Add(value);
        }

        public void CopyCapacityPolicies(IList<CapacityPolicyInfo> dst)
        {
            dst.Clear();
            _world.RequireHealthyUpdate();
            var values = new List<CapacityPolicyInfo>();
            var policies = new List<ICapacityPolicyDefinition>(_world.Content.All<ICapacityPolicyDefinition>());
            policies.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            foreach (ICapacityPolicyDefinition policy in policies) if (CanSeeContent(policy))
                values.Add(EntityObservation.ReadProvider(_world, policy, null, () => new CapacityPolicyInfo(policy)));
            foreach (CapacityPolicyInfo value in values) dst.Add(value);
        }

        public bool TryFindResource(string id, out ResourceId resource)
        {
            resource = default;
            _world.RequireHealthyUpdate();
            if (_world.Content.TryResource(id, out resource) && CanSeeContent(_world.Content.Resource(resource))) return true;
            resource = default; return false;
        }
        public string ResourceName(ResourceId resource)
        {
            _world.RequireHealthyUpdate();
            return CanSeeContent(_world.Content.Resource(resource)) ? _world.Content.ResourceName(resource) : null;
        }

        /// <summary>The ids of every command the game understands.</summary>
        public IReadOnlyList<string> CommandIds
        {
            get
            {
                var ids = new List<string>();
                for (int i = 0; i < _world.Commands.Entries.Count; i++) ids.Add(_world.Commands.Entries[i].Id);
                return ids.AsReadOnly();
            }
        }

        /// <summary>The seats and who plays each.</summary>
        public Controller ControllerOf(PlayerId seat) => _world.Players.ControllerOf(seat);
    }
}
