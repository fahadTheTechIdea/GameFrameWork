using System;
using System.Collections.Generic;
using System.Threading;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Kernel
{
    /// <summary>Engine-owned initialization after a new entity is registered. Never runs on load or replacement.</summary>
    public interface IEntityInitialization
    {
        void Initialize(World world);
    }

    /// <summary>Pure cross-capability validation after every component payload has been decoded or written.</summary>
    public interface IEntityCheckpointValidation
    {
        void ValidateCheckpoint();
    }

    /// <summary>Pure typed preflight for removing authoritative entity state.</summary>
    public interface IEntityRemovalGuard
    {
        Code CheckRemoval(World world);
    }

    /// <summary>Optional provider-owned auxiliary state retention during replacement. The destination
    /// is privately prepared, not initialized as a new entity. Never change the source, stock or promises;
    /// resources move only through the existing transaction protocol. Normal persistence still uses Sync.</summary>
    public interface IEntityReplacementState
    {
        void RestoreReplacementState(World world, Component source);
    }

    /// <summary>Service, system and entity-component owners of live references. Admission is pure; commit changes only references while
    /// both identities are live, before source retirement. Existing Sync remains authoritative.</summary>
    public interface IEntityReplacementReferenceOwner
    {
        Code CheckReferenceReplacement(World world, Entity source, EntityDef destination);
        void ReplaceEntityReferences(World world, Entity source, Entity destination);
    }

    /// <summary>
    /// Per-entity state for one component def. A component is data: its configuration is its
    /// <see cref="Def"/>, its fields are state, and its one <see cref="Sync"/> saves, loads and hashes
    /// them. Systems hold the logic.
    /// </summary>
    public abstract class Component : IEntityCapability
    {
        public Entity Entity { get; internal set; }
        public ComponentDef Def { get; internal set; }

        /// <summary>Sets initial state from the def when the entity is created (not on load).</summary>
        public virtual void OnCreate() { }

        /// <summary>Saves, loads or hashes this component's state.</summary>
        public virtual void Sync(IStateSync s) { }
    }

    /// <summary>A component whose def is <typeparamref name="TDef"/>.</summary>
    public abstract class Component<TDef> : Component where TDef : ComponentDef
    {
        public new TDef Def => (TDef)base.Def;
    }

    /// <summary>Small integer ids for component runtime types, for O(1) lookup on an entity.</summary>
    public static class ComponentTypes
    {
        private static readonly Dictionary<Type, int> Ids = new Dictionary<Type, int>();
        private static readonly object Gate = new object();

        public static int IdOf(Type type)
        {
            lock (Gate)
            {
                if (!Ids.TryGetValue(type, out int id)) { id = Ids.Count; Ids.Add(type, id); }
                return id;
            }
        }

        public static int Id<T>() where T : class, IEntityCapability => Cache<T>.Id;

        private static class Cache<T> where T : class, IEntityCapability
        {
            public static readonly int Id = IdOf(typeof(T));
        }
    }

    /// <summary>
    /// A thing in the world: a structure, a vehicle or a marker, composed of the components its def
    /// declares. Identity, owner, placement and presentation state live here; everything else is a
    /// component.
    /// </summary>
    public sealed class Entity : IEntity
    {
        private readonly Component[] _components;
        private readonly IReadOnlyList<Component> _componentView;
        private readonly IEntityStateSource[] _stateSources;
        private bool _updating;
        // Transient last completed permission coordinate; gameplay chronology stays in GameClock/StateSince.
        private long _completedUpdate = -1;

        internal Entity(EntityId id, EntityDef def, PlayerId owner, long tick, string scenario)
        {
            Scenario = scenario;
            Id = id;
            Def = def;
            Owner = owner;
            State = def.InitialState.State;
            StateSince = tick;
            var sources = new List<IEntityStateSource>();
            _components = new Component[def.Components.Count];
            for (int i = 0; i < _components.Length; i++)
            {
                Component c = def.Components[i].Create();
                c.Entity = this;
                c.Def = def.Components[i];
                if (c is INamedEntityCapability named && c.Def is INamedCapabilityDefinition declaration && named.CapabilityId != declaration.CapabilityId)
                    throw new ContentException("entity '" + def.Id + "': runtime named capability differs from its authored identity");
                _components[i] = c;
                if (c is IEntityStateSource source) sources.Add(source);
            }
            _stateSources = sources.ToArray();
            _componentView = Array.AsReadOnly(_components);
        }

        public EntityId Id { get; }
        /// <summary>Immutable session identity for context-free native checkpoint validation; never another world or geometry owner.</summary>
        internal string Scenario { get; }
        public EntityDef Def { get; }
        public EntityKind Kind => Def.Kind;
        public PlayerId Owner { get; internal set; }

        /// <summary>Authoritative state code settled only by this entity's fixed Update.</summary>
        public Code State { get; private set; }

        /// <summary>Tick <see cref="State"/> last changed.</summary>
        public long StateSince { get; private set; }

        /// <summary>Bumped whenever something a presenter shows changes; hosts diff on it.</summary>
        public int Version { get; private set; }

        public bool Alive { get; internal set; }

        public IReadOnlyList<Component> Components => _componentView;

        public T Get<T>() where T : class, IEntityCapability
        {
            if (typeof(T) == typeof(IEntityCapability)) throw new ArgumentException("query a specific capability; Components exposes all parts");
            if (typeof(T).IsInterface && typeof(INamedEntityCapability).IsAssignableFrom(typeof(T)))
                throw new ArgumentException("named capabilities require an explicit identity");
            int slot = Def.SlotOf(ComponentTypes.Id<T>());
            if (slot == -2) throw new ArgumentException("multiple capability instances require an explicit named identity");
            return slot < 0 ? null : (T)(object)_components[slot];
        }

        public T Get<T>(Code identity) where T : class, INamedEntityCapability
        {
            if (identity.IsNone) throw new ArgumentException("named capabilities require an explicit identity", nameof(identity));
            int slot = Def.NamedSlot(identity);
            return slot < 0 ? null : _components[slot] as T;
        }

        public bool TryGet<T>(out T component) where T : class, IEntityCapability
        {
            component = Get<T>();
            return component != null;
        }

        public bool Has<T>() where T : class, IEntityCapability => Get<T>() != null;

        public void Update(EntityUpdateContext context)
        {
            if (!context.IsCurrent(this)) throw new InvalidOperationException("entity update requires its current engine-issued context");
            World world = context.World;
            world.RequireHealthyUpdate();
            if (!Alive) return;
            if (_updating || _completedUpdate == context.Tick)
            {
                var error = new InvalidOperationException("an entity may update only once in its issued game update");
                world.LatchFault("entity.update:" + Id.Value, error);
                throw error;
            }
            _updating = true;
            try
            {
                IEntityStateDefinition selected = null;
                int selectedPriority = 0;
                string selectedId = null;
                Code selectedState = Code.None;
                for (int i = 0; i < _stateSources.Length; i++)
                {
                    var source = _stateSources[i];
                    var part = (Component)source;
                    IEntityStateDefinition candidate;
                    int priority = 0;
                    string identity = null;
                    Code state = Code.None;
                    try
                    {
                        world.RequireHealthyUpdate();
                        var observation = EntityObservation.CaptureProvider(world, part,
                            "entity.state:" + Id.Value + ":" + part.Def.Key + "[" + part.Def.Slot + "]");
                        candidate = source.GetState(context);
                        if (candidate != null)
                        {
                            // Contract metadata reads belong to the same actual source observation.
                            identity = candidate.Id;
                            if (!ReferenceEquals(world.Content.Find<IEntityStateDefinition>(identity), candidate))
                                throw new InvalidOperationException("entity state source returned a definition outside this session's content");
                            state = candidate.State;
                            priority = candidate.Priority;
                            if (string.IsNullOrWhiteSpace(state.Id))
                                throw new InvalidOperationException("entity state source returned an empty published state code");
                        }
                        observation.Verify();
                        world.RequireHealthyUpdate();
                    }
                    catch (Exception error)
                    {
                        world.LatchFault("entity.state:" + Id.Value + ":" + part.Def.Key + "[" + part.Def.Slot + "]", error);
                        throw;
                    }
                    if (candidate == null) continue;
                    if (selected == null || priority > selectedPriority ||
                        (priority == selectedPriority && string.CompareOrdinal(identity, selectedId) < 0))
                    {
                        selected = candidate;
                        selectedPriority = priority; selectedId = identity; selectedState = state;
                    }
                }
                world.RequireHealthyUpdate();
                if (selected == null)
                    selectedState = EntityObservation.ReadProvider(world, Def.InitialState, this, () =>
                    {
                        Code state = Def.InitialState.State;
                        if (string.IsNullOrWhiteSpace(state.Id)) throw new InvalidOperationException("initial entity state lost its published code");
                        return state;
                    });
                TransitionState(selectedState, context.Tick);
                _completedUpdate = context.Tick;
            }
            finally { _updating = false; }
        }

        private void TransitionState(Code state, long tick)
        {
            if (State == state) return;
            int version = checked(Version + 1);
            State = state;
            StateSince = tick;
            Version = version;
        }

        /// <summary>Marks a presentation-visible change (stock, progress, owner...).</summary>
        public void Touch() => Version = checked(Version + 1);

        internal void Sync(IStateSync s)
        {
            PlayerId owner = Owner; s.Player(ref owner); Owner = owner;
            string state = State.Id; s.String(ref state); State = state == null ? Code.None : new Code(state);
            long since = StateSince; s.Long(ref since);
            if (since < 0) throw new CorruptStateException("entity state tick must be nonnegative");
            StateSince = since;
            int version = Version; s.Int(ref version); Version = version;
            for (int i = 0; i < _components.Length; i++) _components[i].Sync(s);
            for (int i = 0; i < _components.Length; i++)
                if (_components[i] is IEntityCheckpointValidation validation)
                {
                    int observedVersion = Version; PlayerId observedOwner = Owner; Code observedState = State; long observedSince = StateSince;
                    validation.ValidateCheckpoint();
                    if (Version != observedVersion || Owner != observedOwner || State != observedState || StateSince != observedSince)
                        throw new CorruptStateException("checkpoint validator mutated its entity");
                }
        }

        public override string ToString() => Def.Id + Id;
    }

    internal static class VecExtensions
    {
        public static FixVec2 Normalized(this FixVec2 v)
        {
            Fix len = v.Length;
            return len == Fix.Zero ? FixVec2.Zero : new FixVec2(v.X / len, v.Y / len);
        }
    }

    /// <summary>
    /// Every live component of one type, in entity-id order, plus bucketed views for spread
    /// schedules: a system that runs every <c>period</c> ticks over all entities visits only
    /// <c>Bucket(period, tick % period)</c> each tick (RimWorld's TickList).
    /// </summary>
    public sealed class ComponentList<T> where T : class, IEntityCapability
    {
        private readonly List<T> _resources = new List<T>();
        private readonly Dictionary<int, List<T>[]> _buckets = new Dictionary<int, List<T>[]>();
        private static readonly List<T> Empty = new List<T>();

        public int Count => _resources.Count;
        public T this[int i] => _resources[i];
        public IReadOnlyList<T> Resources => _resources.AsReadOnly();

        /// <summary>The components whose entity id falls in <paramref name="bucket"/> of <paramref name="period"/>.</summary>
        public IReadOnlyList<T> Bucket(int period, int bucket)
        {
            if (period <= 1) return _resources;
            if (!_buckets.TryGetValue(period, out List<T>[] b))
            {
                b = new List<T>[period];
                for (int i = 0; i < period; i++) b[i] = new List<T>();
                for (int i = 0; i < _resources.Count; i++) b[_resources[i].Entity.Id.Value % period].Add(_resources[i]);
                _buckets.Add(period, b);
            }
            return bucket >= 0 && bucket < period ? b[bucket] : Empty;
        }

        internal void Add(T c)
        {
            _resources.Add(c);
            foreach (KeyValuePair<int, List<T>[]> kv in _buckets) kv.Value[c.Entity.Id.Value % kv.Key].Add(c);
        }

        internal void RemoveDead()
        {
            _resources.RemoveAll(c => !c.Entity.Alive);
            foreach (KeyValuePair<int, List<T>[]> kv in _buckets)
                for (int i = 0; i < kv.Value.Length; i++) kv.Value[i].RemoveAll(c => !c.Entity.Alive);
        }
    }

    internal interface IComponentIndex
    {
        void Add(Component c);
        void RemoveDead();
    }

    internal sealed class ComponentIndex<T> : IComponentIndex where T : class, IEntityCapability
    {
        public readonly ComponentList<T> List = new ComponentList<T>();
        public void Add(Component c) => List.Add((T)(object)c);
        public void RemoveDead() => List.RemoveDead();
    }

    /// <summary>
    /// Owns every entity. Spawning assigns an id and claims tiles at once (so two orders in one tick
    /// cannot take the same ground); adding to the iteration lists and removing dead entities happen at
    /// <see cref="Flush"/>, between phases, so no system ever sees a list change under it (OpenRA's
    /// frame-end tasks).
    /// </summary>
    public sealed class EntityStore
    {
        private readonly string _scenario;
        internal EntityStore(string scenario)
        {
            if (string.IsNullOrWhiteSpace(scenario)) throw new ArgumentException("entity store needs its actual session scenario", nameof(scenario));
            _scenario = scenario;
        }
        private readonly List<Entity> _all = new List<Entity>();
        private readonly Dictionary<int, Entity> _byId = new Dictionary<int, Entity>();
        private readonly List<Entity> _pending = new List<Entity>();
        private readonly Dictionary<Type, IComponentIndex> _indexes = new Dictionary<Type, IComponentIndex>();
        private readonly List<IComponentIndex> _indexList = new List<IComponentIndex>();
        private readonly List<EntityId> _removedLog = new List<EntityId>();
        private int _nextId = 1;
        internal EntityId NextIdentity => new EntityId(_nextId);
        private bool _dirty;
        private bool _creating;
        private bool _replacing;
        private int _removalDepth;

        public int Count => _all.Count;
        public IReadOnlyList<Entity> All => _all.AsReadOnly();

        public Entity Get(EntityId id) => _byId.TryGetValue(id.Value, out Entity e) && e.Alive ? e : null;

        // Guard-only copy of the original registry, including registered frame-pending entities.
        // The deferred execution lists and persistence remain unchanged; hosts receive no mutable handles.
        internal IReadOnlyList<Entity> CaptureRegistered()
        {
            var result = new List<Entity>();
            foreach (Entity entity in _byId.Values) if (entity.Alive) result.Add(entity);
            result.Sort((left, right) => left.Id.Value.CompareTo(right.Id.Value));
            return result.AsReadOnly();
        }

        public bool TryGet(EntityId id, out Entity e)
        {
            e = Get(id);
            return e != null;
        }

        public ComponentList<T> Components<T>() where T : class, IEntityCapability
        {
            if (typeof(T) == typeof(IEntityCapability)) throw new ArgumentException("query a specific capability; entity.Components exposes all parts");
            if (typeof(T).IsInterface && typeof(INamedEntityCapability).IsAssignableFrom(typeof(T)))
                throw new ArgumentException("query named capabilities on an entity with their explicit identity");
            if (!_indexes.TryGetValue(typeof(T), out IComponentIndex index))
            {
                var typed = new ComponentIndex<T>();
                for (int i = 0; i < _all.Count; i++)
                {
                    T c = _all[i].Get<T>();
                    if (c != null && _all[i].Alive) typed.List.Add(c);
                }
                index = typed;
                _indexes.Add(typeof(T), index);
                _indexList.Add(index);
            }
            return ((ComponentIndex<T>)index).List;
        }

        internal Entity Create(EntityDef def, PlayerId owner, long tick, Action<Entity> configure, ISpatialSpace space, Action requireHealthy, Action<Exception> creationFailed)
        {
            if (_creating || _replacing) throw new InvalidOperationException("entity creation or replacement already in progress");
            _creating = true;
            try
            {
                int next = checked(_nextId + 1);
                var e = new Entity(new EntityId(_nextId), def, owner, tick, _scenario) { Alive = true };
                try
                {
                    configure?.Invoke(e);
                    requireHealthy();
                    space?.OnSpawn(e);
                    requireHealthy();
                    for (int i = 0; i < e.Components.Count; i++)
                    {
                        e.Components[i].OnCreate();
                        requireHealthy();
                    }
                }
                catch (Exception error)
                {
                    creationFailed(error);
                    try { space?.OnDespawn(e); }
                    catch (Exception cleanupError) { creationFailed(cleanupError); }
                    finally { e.Alive = false; }
                    throw;
                }
                _nextId = next;
                _byId.Add(e.Id.Value, e);
                _pending.Add(e);
                _dirty = true;
                return e;
            }
            finally { _creating = false; }
        }

        internal void Destroy(Entity e, ISpatialSpace space)
        {
            if (!e.Alive) return;
            // Spatial retirement is unfinished until its original callback returns. A depth,
            // rather than a boolean, preserves this boundary across nested valid removals.
            _removalDepth = checked(_removalDepth + 1);
            try
            {
                e.Alive = false;
                space?.OnDespawn(e);
                _removedLog.Add(e.Id);
                _dirty = true;
            }
            finally { _removalDepth--; }
        }

        internal Code Replace(Entity old, EntityDef definition, long tick, ISpatialSpace space,
            Action<Entity> configure, Action verifyPreparation, Action<Entity> restore, Func<Entity, Code> check,
            Action<Entity> relocate, out Entity made)
        {
            made = null;
            if (verifyPreparation == null) throw new ArgumentNullException(nameof(verifyPreparation));
            if (_replacing || _creating) throw new InvalidOperationException("entity creation or replacement already in progress");
            if (!ReferenceEquals(Get(old.Id), old)) throw new InvalidOperationException("replacement source is not live in this store");
            int next = checked(_nextId + 1);
            var target = new Entity(new EntityId(_nextId), definition, old.Owner, tick, _scenario);
            // Preparation owns no world identity or geometry; no creation grants or ID consumption.
            configure?.Invoke(target);
            verifyPreparation();
            foreach (Component component in target.Components)
            {
                component.OnCreate();
                verifyPreparation();
            }
            restore?.Invoke(target);
            verifyPreparation();
            Code refusal = check(target);
            if (!refusal.IsNone) return refusal;

            _replacing = true;
            target.Alive = true;
            space?.OnDespawn(old);
            space?.OnSpawn(target);
            _nextId = next;
            _byId.Add(target.Id.Value, target);
            _pending.Add(target);
            _dirty = true;
            // Both registered endpoints remain live until actual stock relocation verifies.
            relocate(target);
            old.Alive = false;
            _removedLog.Add(old.Id);
            _replacing = false;
            made = target;
            return Code.None;
        }

        /// <summary>Applies pending spawns and removals to the iteration lists.</summary>
        internal void Flush()
        {
            if (!_dirty) return;
            _dirty = false;
            for (int p = 0; p < _pending.Count; p++)
            {
                Entity e = _pending[p];
                if (!e.Alive) continue;
                _all.Add(e);
                foreach (KeyValuePair<Type, IComponentIndex> kv in _indexes)
                {
                    for (int c = 0; c < e.Components.Count; c++)
                        if (kv.Key.IsInstanceOfType(e.Components[c])) kv.Value.Add(e.Components[c]);
                }
            }
            _pending.Clear();
            if (_removedLog.Count > 0)
            {
                _all.RemoveAll(x => !x.Alive);
                for (int i = 0; i < _indexList.Count; i++) _indexList[i].RemoveDead();
                for (int i = 0; i < _removedLog.Count; i++) _byId.Remove(_removedLog[i].Value);
                _removedLog.Clear();
            }
        }

        internal void Sync(IStateSync s, ResolvedContent content)
        {
            RequireCompletedLifecycle();
            s.Section("entities");
            int next = _nextId; s.Int(ref next); _nextId = next;
            int n = s.Count(_all.Count);
            if (s.Reading)
            {
                for (int i = 0; i < n; i++)
                {
                    int id = 0; s.Int(ref id);
                    string defId = null; s.String(ref defId);
                    EntityDef def = content.Find<EntityDef>(defId) ?? throw new CorruptStateException("save names unknown entity def '" + defId + "'");
                    var e = new Entity(new EntityId(id), def, PlayerId.None, 0, _scenario) { Alive = true };
                    e.Sync(s);
                    _byId.Add(id, e);
                    _pending.Add(e);
                }
                _dirty = true;
                Flush();
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    Entity e = _all[i];
                    int id = e.Id.Value; s.Int(ref id);
                    string defId = e.Def.Id; s.String(ref defId);
                    e.Sync(s);
                }
            }
        }

        internal bool HasPending => _dirty;
        internal bool Replacing => _replacing;
        internal void RequireCompletedLifecycle()
        {
            if (_replacing || _creating || _removalDepth != 0)
                throw new InvalidOperationException("entity creation, replacement or removal must complete before advancing or checkpointing the world");
        }
    }
}
