using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine
{
    public enum EntityKind : byte
    {
        /// <summary>Occupies a footprint of tiles and does not move.</summary>
        Structure = 0,
        /// <summary>Moves across the map (trucks, rigs, cranes, vessels).</summary>
        Vehicle = 1,
        /// <summary>A non-blocking mark on the map: a ghost, a survey stake, a salvage pile.</summary>
        Marker = 2,
        /// <summary>A generic simulation entity; no spatial role is implied.</summary>
        Generic = 3
    }

}

namespace Beep.SimEngine.Content
{
    /// <summary>
    /// A piece of immutable game content (RimWorld's Def, Factorio's prototype): read once from JSON
    /// when a content set is resolved, cross-referenced, then frozen. Worlds share defs; a def never
    /// holds game state.
    /// </summary>
    public interface IContentDefinition
    {
        string Id { get; }
        string Module { get; }
    }

    /// <summary>Explicit actual immutable references used by a content provider; no inferred or fallback dependencies.</summary>
    public interface IContentDependencyDefinition : IContentDefinition
    {
        IReadOnlyList<IContentDefinition> ContentDependencies { get; }
    }

    public abstract class Def : IContentDefinition
    {
        private readonly ContentAuthoring _authoring = new ContentAuthoring();
        private string _id, _module, _authoringPath;
        private int _index;
        public bool IsFrozen => _authoring.IsFrozen;
        public string Id { get => _id; internal set { _authoring.RequireMutable(); _id = value; } }
        /// <summary>The content module that declared it.</summary>
        public string Module { get => _module; internal set { _authoring.RequireMutable(); _module = value; } }
        /// <summary>Position in its table: stable for a given content set.</summary>
        public int Index { get => _index; internal set { _authoring.RequireMutable(); _index = value; } }
        internal string AuthoringPath { get => _authoringPath; set { _authoring.RequireMutable(); _authoringPath = value; } }

        /// <summary>Reads this def's own keys. The reader fails the load on any key left unread.</summary>
        public void Read(JsonReader r)
        { if (r == null) throw new ArgumentNullException(nameof(r)); _authoring.Read(() => { ReadCore(r); r.End(); }); }
        protected abstract void ReadCore(JsonReader r);

        /// <summary>Resolves references to other defs once every def has been read.</summary>
        public void Resolve(ContentResolver c)
        { if (c == null) throw new ArgumentNullException(nameof(c)); _authoring.Resolve(() => ResolveCore(c)); }
        protected virtual void ResolveCore(ContentResolver c) { }
        /// <summary>Validates linked metadata once, after every definition has resolved and before publication.</summary>
        public void ValidateResolved(ContentResolver c)
        { if (c == null) throw new ArgumentNullException(nameof(c)); _authoring.Validate(() => ValidateResolvedCore(c)); }
        protected virtual void ValidateResolvedCore(ContentResolver c) { }
        internal void Freeze() => _authoring.Freeze();
        internal void RequireMutable() => _authoring.RequireMutable();

        public override string ToString() => GetType().Name + ":" + Id;
    }

    /// <summary>
    /// The definition of one kind of entity: its kind, footprint and the component defs it is built
    /// from. Structures, vehicles and markers are composed by the same rule.
    /// </summary>
    public sealed class EntityDef : Def
    {
        private ComponentDef[] _components = new ComponentDef[0];
        private IReadOnlyList<ComponentDef> _componentView = Array.AsReadOnly(Array.Empty<ComponentDef>());
        private int[] _slotByType = new int[0];
        // One authored metadata index, shared by definition and actual runtime capability queries.
        private readonly Dictionary<Code, int> _namedSlots = new Dictionary<Code, int>();

        public EntityKind Kind { get; private set; }
        /// <summary>Menu/catalogue category content id.</summary>
        public string Category { get; private set; }
        /// <summary>Presentation key a host maps to a sprite, scene or prefab.</summary>
        public string Visual { get; private set; }
        public IReadOnlyList<string> Tags { get; private set; }
        public IReadOnlyList<ComponentDef> Components => _componentView;
        public IEntityStateDefinition InitialState { get; private set; }

        protected override void ReadCore(JsonReader r)
        {
            string kind = r.String("kind");
            switch (kind)
            {
                case "structure": Kind = EntityKind.Structure; break;
                case "vehicle": Kind = EntityKind.Vehicle; break;
                case "marker": Kind = EntityKind.Marker; break;
                case "entity": Kind = EntityKind.Generic; break;
                default: throw r.Error("kind must be entity, structure, vehicle or marker");
            }
            Category = r.String("category", "general");
            Visual = r.String("visual", Id);
            Tags = Array.AsReadOnly(r.Strings("tags"));
            r.Skip("components");
        }

        internal void SetComponents(ComponentDef[] defs)
        {
            RequireMutable();
            if (defs == null) throw new ArgumentNullException(nameof(defs));
            var unique = new HashSet<ComponentDef>();
            foreach (ComponentDef def in defs)
                if (def == null || def.IsFrozen || def.Owner != null || !unique.Add(def))
                    throw new ContentException("entity components must be fresh, distinct unowned definitions");
            _components = (ComponentDef[])defs.Clone();
            _componentView = Array.AsReadOnly(_components);
            var definitionContracts = new HashSet<Type>();
            _namedSlots.Clear(); _slotByType = new int[0];
            for (int i = 0; i < defs.Length; i++)
            {
                defs[i].Owner = this;
                defs[i].Slot = i;
                if (typeof(INamedEntityCapability).IsAssignableFrom(defs[i].ComponentType))
                {
                    if (!(defs[i] is INamedCapabilityDefinition named) || named.CapabilityId.IsNone)
                        throw new ContentException("entity '" + Id + "': named capability requires an authored definition identity");
                }
                if (defs[i] is INamedCapabilityDefinition identity)
                {
                    if (identity.CapabilityId.IsNone || _namedSlots.ContainsKey(identity.CapabilityId))
                        throw new ContentException("entity '" + Id + "': missing or duplicate named capability '" + identity.CapabilityId.Id + "'");
                    _namedSlots.Add(identity.CapabilityId, i);
                }
                foreach (Type contract in defs[i].GetType().GetInterfaces())
                    if (contract != typeof(IComponentDefinition) && contract != typeof(IComponentCompositionValidation) && !typeof(INamedCapabilityDefinition).IsAssignableFrom(contract) && typeof(IComponentDefinition).IsAssignableFrom(contract)
                        && !definitionContracts.Add(contract))
                        throw new ContentException("entity '" + Id + "': multiple owners of definition contract " + contract.FullName);
                RegisterSlot(defs[i].ComponentType, i);
                Type[] contracts = defs[i].ComponentType.GetInterfaces();
                for (int k = 0; k < contracts.Length; k++)
                    if (contracts[k] != typeof(IEntityCapability) && !typeof(INamedEntityCapability).IsAssignableFrom(contracts[k]) && typeof(IEntityCapability).IsAssignableFrom(contracts[k]))
                        RegisterSlot(contracts[k], i);
            }
        }

        private void RegisterSlot(Type type, int slot)
        {
            int typeId = ComponentTypes.IdOf(type);
            if (typeId >= _slotByType.Length)
            {
                int[] grown = new int[typeId + 8];
                for (int k = 0; k < grown.Length; k++) grown[k] = k < _slotByType.Length ? _slotByType[k] : -1;
                _slotByType = grown;
            }
            if ((_slotByType[typeId] >= 0 && _slotByType[typeId] != slot) || _slotByType[typeId] == -2)
            {
                if (typeof(INamedEntityCapability).IsAssignableFrom(type)) { _slotByType[typeId] = -2; return; }
                throw new ContentException("entity '" + Id + "': multiple owners of capability " + type.FullName);
            }
            _slotByType[typeId] = slot;
        }

        /// <summary>Slot of the component of this runtime type, or -1.</summary>
        internal int SlotOf(int typeId) => typeId < _slotByType.Length ? _slotByType[typeId] : -1;
        internal int NamedSlot(Code identity) => _namedSlots.TryGetValue(identity, out int slot) ? slot : -1;

        public bool HasTag(string tag)
        {
            for (int i = 0; i < Tags.Count; i++) if (Tags[i] == tag) return true;
            return false;
        }

        public TDef Component<TDef>() where TDef : class, IComponentDefinition
        {
            if (typeof(TDef) == typeof(IComponentDefinition) || typeof(TDef) == typeof(IComponentCompositionValidation) || typeof(TDef).IsInterface && typeof(INamedCapabilityDefinition).IsAssignableFrom(typeof(TDef)))
                throw new ArgumentException("query a specific singular definition contract or supply a named identity");
            TDef result = null;
            for (int i = 0; i < _components.Length; i++)
                if (_components[i] is TDef d)
                {
                    if (result != null) throw new ArgumentException("multiple definitions require an explicit named identity");
                    result = d;
                }
            return result;
        }

        public bool Has<TDef>() where TDef : class, IComponentDefinition => Component<TDef>() != null;
        public TDef Component<TDef>(Code identity) where TDef : class, INamedCapabilityDefinition
        {
            if (identity.IsNone) throw new ArgumentException("named definitions require an explicit identity", nameof(identity));
            int slot = NamedSlot(identity);
            return slot < 0 ? null : _components[slot] as TDef;
        }

        /// <summary>Whether the prototype declares an implementation of this entity capability.</summary>
        public bool HasCapability<T>() where T : class, IEntityCapability
        {
            if (typeof(T).IsInterface && typeof(INamedEntityCapability).IsAssignableFrom(typeof(T)))
                throw new ArgumentException("named capabilities require an explicit identity");
            return SlotOf(ComponentTypes.Id<T>()) != -1;
        }

        public bool HasCapability<T>(Code identity) where T : class, INamedEntityCapability
        {
            if (identity.IsNone) throw new ArgumentException("named capabilities require an explicit identity", nameof(identity));
            int slot = NamedSlot(identity);
            return slot >= 0 && typeof(T).IsAssignableFrom(_components[slot].ComponentType);
        }

        protected override void ResolveCore(ContentResolver c)
        {
            InitialState = c.Content.DefaultEntityState;
            for (int i = 0; i < _components.Length; i++) _components[i].Resolve(c);
        }
    }

    /// <summary>
    /// The immutable configuration of one component (RimWorld's CompProperties, OpenRA's TraitInfo).
    /// It is read once, strictly, from the entity def's "components" block under its registered key,
    /// and it creates the per-entity component that holds only state.
    /// </summary>
    public interface IComponentDefinition
    {
        Type ComponentType { get; }
    }

    /// <summary>Pure admission of authored component metadata against this session's frozen
    /// registrations. Invoked for every loaded entity definition, including unused prototypes,
    /// before service preparation or any system initialization. Inspect required contracts;
    /// never register providers, mutate simulation state or depend on initialized state.
    /// Several components may implement this lifecycle contract; it is not a singular capability.</summary>
    public interface IComponentCompositionValidation : IComponentDefinition
    {
        void ValidateComposition(World world, EntityDef entity);
    }

    public interface INamedCapabilityDefinition : IComponentDefinition
    {
        Code CapabilityId { get; }
    }

    public abstract class ComponentDef : IComponentDefinition
    {
        private readonly ContentAuthoring _authoring = new ContentAuthoring();
        private string _key;
        private StateCodecRegistration _stateCodec;
        private OperationProviderSchema _operationSchema;
        private AutomationSignalProviderSchema _signalSchema;
        private EntityDef _owner;
        private int _slot;
        public bool IsFrozen => _authoring.IsFrozen;
        public string Key { get => _key; internal set { _authoring.RequireMutable(); _key = value; } }
        public StateCodecRegistration StateCodec { get => _stateCodec; internal set { _authoring.RequireMutable(); _stateCodec = value; } }
        public OperationProviderSchema OperationSchema { get => _operationSchema; internal set { _authoring.RequireMutable(); _operationSchema = value; } }
        public AutomationSignalProviderSchema SignalSchema { get => _signalSchema; internal set { _authoring.RequireMutable(); _signalSchema = value; } }
        public EntityDef Owner { get => _owner; internal set { _authoring.RequireMutable(); _owner = value; } }
        public int Slot { get => _slot; internal set { _authoring.RequireMutable(); _slot = value; } }

        /// <summary>The runtime component class this def creates.</summary>
        public abstract Type ComponentType { get; }

        public void Read(JsonReader r)
        { if (r == null) throw new ArgumentNullException(nameof(r)); _authoring.Read(() => { ReadCore(r); r.End(); }); }
        protected abstract void ReadCore(JsonReader r);
        public void Resolve(ContentResolver c)
        { if (c == null) throw new ArgumentNullException(nameof(c)); _authoring.Resolve(() => ResolveCore(c)); }
        protected virtual void ResolveCore(ContentResolver c) { }
        internal void Freeze() => _authoring.Freeze();


        /// <summary>Checks this def against its siblings (e.g. "needs a storage component"). Throw to fail.</summary>
        public void Validate(EntityDef owner)
        {
            _authoring.Validate(() =>
            {
                if (owner == null || !ReferenceEquals(Owner, owner)) throw new ContentException("component validation requires its actual owner");
                if (this is INamedCapabilityDefinition named && owner.NamedSlot(named.CapabilityId) != Slot)
                    throw new ContentException(owner.Id + ": resolved named capability changed its authored identity");
                if (this is Standard.IAutomationSignalDefinition signal && !ReferenceEquals(SignalSchema, signal.SignalSchema))
                    throw new ContentException(owner.Id + ": signal metadata must expose its actual registered schema");
                if (this is Standard.IOperationDefinition operation)
                {
                    if (!ReferenceEquals(OperationSchema, operation.OperationSchema) || OperationSchema == null)
                        throw new ContentException(owner.Id + ": operation metadata must expose its actual registered schema");
                    var controls = operation.ControlSettings;
                    if (OperationSchema.Controllable != (controls != null) || controls != null && controls.Id != operation.CapabilityId)
                        throw new ContentException(owner.Id + ": authored controls disagree with actual operation schema/identity");
                }
                ValidateCore(owner);
            });
        }
        protected virtual void ValidateCore(EntityDef owner) { }

        internal abstract Component Create();
    }

    /// <summary>A component def whose runtime component is <typeparamref name="TComponent"/>.</summary>
    public abstract class ComponentDef<TComponent> : ComponentDef where TComponent : Component, new()
    {
        public sealed override Type ComponentType => typeof(TComponent);
        internal sealed override Component Create() => new TComponent();
    }

    /// <summary>What a def's Resolve step may look up. Unknown ids fail the load, naming the def.</summary>
    public sealed class ContentResolver
    {
        private readonly ResolvedContent _content;
        private readonly Def _current;

        internal ContentResolver(ResolvedContent content, Def current)
        {
            _content = content;
            _current = current;
        }

        public ResolvedContent Content => _content;
        /// <summary>The actual content owner currently resolving; never a live entity or state proxy.</summary>
        public IContentDefinition Definition => _current;

        public ResourceId Resource(string id)
        {
            if (string.IsNullOrEmpty(id)) throw Fail("a resource id is required");
            if (!_content.TryResource(id, out ResourceId resource)) throw Fail("unknown resource '" + id + "'");
            return resource;
        }

        public ResourceId OptionalResource(string id) => string.IsNullOrEmpty(id) ? ResourceId.None : Resource(id);

        public TDef Def<TDef>(string id) where TDef : class, IContentDefinition
        {
            TDef d = _content.Find<TDef>(id);
            if (d == null) throw Fail("unknown " + typeof(TDef).Name + " '" + id + "'");
            return d;
        }

        public TDef OptionalDef<TDef>(string id) where TDef : class, IContentDefinition => string.IsNullOrEmpty(id) ? null : Def<TDef>(id);

        public ContentException Fail(string message) => new ContentException((_current.AuthoringPath ?? _current.ToString()) + ": " + message);
    }
}
