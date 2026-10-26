using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    /// <summary>Transient startup intent links, not an entity store or retained gameplay authority.</summary>
    public interface IWorldSetupContext { EntityId RequireEntity(string entry); }
    public interface IWorldSetupDefinition : IContentDefinition, IDependencyNode
    {
        new string Id { get; }
        /// <summary>Pure selected-session declaration admission before any system Initialize.</summary>
        void ValidateComposition(World world);
        Code Check(World world, IWorldSetupContext context);
        EntityId Generate(World world, IWorldSetupContext context);
    }
    public abstract class WorldSetupDef : Def, IWorldSetupDefinition
    {
        public IReadOnlyList<string> Dependencies { get; private set; }
        protected sealed override void ReadCore(JsonReader reader)
        {
            if (reader.Raw.Get("after")?.Kind != JsonKind.Array) throw reader.Error("explicit after array required");
            Dependencies = Array.AsReadOnly(reader.Strings("after")); ReadSetup(reader);
        }
        protected abstract void ReadSetup(JsonReader reader);
        public abstract void ValidateComposition(World world);
        public abstract Code Check(World world, IWorldSetupContext context);
        public abstract EntityId Generate(World world, IWorldSetupContext context);
    }
    public interface IWorldSetupProfileDefinition : IContentDefinition, IDependencyNode
    {
        new string Id { get; }
        IReadOnlyList<IWorldSetupDefinition> Entries { get; }
        IReadOnlyList<IWorldSetupProfileDefinition> Includes { get; }
        DependencyGraph<IWorldSetupDefinition> Graph { get; }
    }
    public sealed class WorldSetupProfileDef : Def, IWorldSetupProfileDefinition, IDependencyNode
    {
        private string[] _entries, _includes;
        public IReadOnlyList<IWorldSetupDefinition> Entries { get; private set; }
        public IReadOnlyList<IWorldSetupProfileDefinition> Includes { get; private set; }
        public IReadOnlyList<string> Dependencies { get; private set; }
        public DependencyGraph<IWorldSetupDefinition> Graph { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            if (reader.Raw.Get("entries")?.Kind != JsonKind.Array || reader.Raw.Get("includes")?.Kind != JsonKind.Array)
                throw reader.Error("explicit entries and includes arrays are required");
            _entries = reader.Strings("entries"); _includes = reader.Strings("includes");
            ValidateUnique(_entries, reader, "entries"); ValidateUnique(_includes, reader, "includes");
            Dependencies = Array.AsReadOnly(_includes);
        }
        private static void ValidateUnique(string[] ids, JsonReader reader, string field)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    throw reader.Error(field + " requires unique nonempty stable identities");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            var entries = new IWorldSetupDefinition[_entries.Length];
            for (int i = 0; i < _entries.Length; i++) entries[i] = content.Def<IWorldSetupDefinition>(_entries[i]);
            Entries = Array.AsReadOnly(entries);
            var includes = new IWorldSetupProfileDefinition[_includes.Length];
            for (int i = 0; i < _includes.Length; i++) includes[i] = content.Def<IWorldSetupProfileDefinition>(_includes[i]);
            Includes = Array.AsReadOnly(includes);
        }
        protected override void ValidateResolvedCore(ContentResolver content) => Graph = Compile(this);
        /// <summary>Sole profile closure/compiler, shared by native and independently implemented content contracts.</summary>
        public static DependencyGraph<IWorldSetupDefinition> Compile(IWorldSetupProfileDefinition root)
        {
            if (root == null) throw new ContentException("startup profile is required");
            string owner = root.Id;
            // Profile and entry topology both use the existing sole immutable graph compiler.
            // Iterative closure collection adds no recursion/depth or platform-size limit.
            var profiles = new Dictionary<string, IWorldSetupProfileDefinition>(StringComparer.Ordinal);
            var pending = new Queue<IWorldSetupProfileDefinition>(); pending.Enqueue(root);
            while (pending.Count != 0)
            {
                IWorldSetupProfileDefinition profile = pending.Dequeue();
                if (string.IsNullOrWhiteSpace(profile.Id)) throw new ContentException(owner + ": profile requires stable identity");
                if (profiles.TryGetValue(profile.Id, out IWorldSetupProfileDefinition previous))
                {
                    if (!ReferenceEquals(previous, profile)) throw new ContentException(owner + ": conflicting startup profile '" + profile.Id + "'");
                    continue;
                }
                profiles.Add(profile.Id, profile);
                if (profile.Includes == null || profile.Entries == null)
                    throw new ContentException(owner + ": profile requires resolved entries and includes");
                if (profile.Dependencies == null) throw new ContentException(owner + ": profile requires dependency declaration");
                var declared = new HashSet<string>(profile.Dependencies, StringComparer.Ordinal);
                foreach (IWorldSetupProfileDefinition included in profile.Includes)
                {
                    if (included == null) throw new ContentException(owner + ": missing included profile");
                    if (!declared.Remove(included.Id)) throw new ContentException(owner + ": included profile differs from declared dependency");
                    pending.Enqueue(included);
                }
                if (declared.Count != 0) throw new ContentException(owner + ": declared profile dependency has no included definition");
            }
            var topology = new DependencyGraph<IWorldSetupProfileDefinition>(profiles.Values, owner + ".includes");
            var entries = new Dictionary<string, IWorldSetupDefinition>(StringComparer.Ordinal);
            foreach (IWorldSetupProfileDefinition profile in topology.Nodes)
                foreach (IWorldSetupDefinition entry in profile.Entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) throw new ContentException(owner + ": missing startup entry");
                    if (entries.TryGetValue(entry.Id, out IWorldSetupDefinition existing))
                    {
                        if (!ReferenceEquals(existing, entry)) throw new ContentException(owner + ": conflicting startup entry '" + entry.Id + "'");
                    }
                    else entries.Add(entry.Id, entry);
                }
            return new DependencyGraph<IWorldSetupDefinition>(entries.Values, owner);
        }
    }
    public sealed class EntitySetupDef : WorldSetupDef
    {
        private string _entity, _home;
        private EntityDef _resolved;
        private EntitySetupDef _resolvedHome;
        private JsonValue _placement;
        public PlayerId Company { get; private set; }
        protected override void ReadSetup(JsonReader reader)
        {
            _entity = reader.String("entity"); Company = new PlayerId(reader.Int("company"));
            JsonValue placement = reader.Raw.Get("placement") ?? throw reader.Error("explicit placement or null required");
            reader.Skip("placement"); _placement = placement.Kind == JsonKind.Null ? null : placement.Clone(); _placement?.Freeze();
            JsonValue home = reader.Raw.Get("home") ?? throw reader.Error("explicit home or null required");
            if (home.Kind == JsonKind.Null) { reader.Skip("home"); _home = null; } else _home = reader.String("home");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _resolved = content.Def<EntityDef>(_entity);
            if (Company != PlayerId.None && !Company.IsValid) throw content.Fail("invalid startup company");
            if (_home != null)
            {
                _resolvedHome = content.Def<EntitySetupDef>(_home);
                if (_resolved.Component<IWorkerDefinition>() == null || !ContainsDependency(_home))
                    throw content.Fail("worker home requires its declared entity dependency");
            }
            if ((_resolved.Component<ISpatialPresenceDefinition>() != null) != (_placement != null))
                throw content.Fail("startup placement must match the actual entity presence");
        }
        private Code CheckCompany(World world)
            => Company != PlayerId.None && !world.Players.IsSeat(Company) ? Codes.NotSeated : Code.None;
        public override void ValidateComposition(World world)
        {
            Code company = CheckCompany(world);
            if (!company.IsNone) throw new ContentException(Id + ": startup company refused " + company);
            if (_resolvedHome != null && _resolvedHome.Company != Company)
                throw new ContentException(Id + ": authored startup home belongs to another company");
            if (_placement == null) return;
            if (!(world.Space is ISpatialPlacementIntent provider) || _resolved.Component<ISpatialPresenceDefinition>().Space != world.Space.Id)
                throw new ContentException(Id + ": startup placement requires the actual selected spatial provider");
            SpatialPlacement placement = EntityObservation.ReadProvider(world, provider, null, () => provider.ReadPlacement(_placement));
            if (placement == null || placement.Space != world.Space.Id)
                throw new ContentException(Id + ": startup placement decoder returned no matching selected-space intent");
        }
        private bool ContainsDependency(string id) { foreach (string after in Dependencies) if (after == id) return true; return false; }
        public override Code Check(World world, IWorldSetupContext context)
        {
            Code company = CheckCompany(world); if (!company.IsNone) return company;
            if (_home != null)
            {
                Entity home = world.Entities.Get(context.RequireEntity(_home));
                if (home == null || home.Owner != Company) return Codes.NotOwned;
            }
            if (_placement == null) return Code.None;
            if (!(world.Space is ISpatialPlacementIntent provider)) return Codes.InvalidArgument;
            return provider.CheckPlacement(world, _resolved, provider.ReadPlacement(_placement), null);
        }
        public override EntityId Generate(World world, IWorldSetupContext context)
        {
            Code refusal = Check(world, context); if (!refusal.IsNone) throw new ContentException(Id + ": startup refused " + refusal);
            Entity entity;
            if (_placement == null) entity = world.Spawn(_resolved, Company);
            else
            {
                var provider = (ISpatialPlacementIntent)world.Space;
                SpatialPlacement placement = provider.ReadPlacement(_placement);
                entity = world.Spawn(_resolved, Company, created => provider.ConfigurePlacement(created, placement));
            }
            if (_home != null) entity.Get<IWorker>().Home = context.RequireEntity(_home);
            return entity.Id;
        }
    }
    public sealed class TileLayerSetupDef : WorldSetupDef
    {
        public override void ValidateComposition(World world)
        {
            Code refusal = Check(world, null);
            if (!refusal.IsNone) throw new ContentException(Id + ": startup layer declaration refused " + refusal);
        }
        private string _layer;
        private ISpatialLayerDefinition _resolved;
        public TileRect Region { get; private set; }
        private Fix _value;
        private PlayerId _owner;
        protected override void ReadSetup(JsonReader reader)
        {
            _layer = reader.String("layer");
            Region = new TileRect(reader.Int("x"), reader.Int("y"), reader.Int("width"), reader.Int("height"));
            if (Region.X < 0 || Region.Y < 0 || Region.W <= 0 || Region.H <= 0)
                throw reader.Error("layer setup requires a nonnegative origin and positive explicit dimensions");
            _value = reader.Fix("value"); _owner = new PlayerId(reader.Int("owner"));
        }
        protected override void ResolveCore(ContentResolver content) => _resolved = content.Def<ISpatialLayerDefinition>(_layer);
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            if (!(_resolved.Space is ITileSpaceDefinition space) || !SpatialSpaceContracts.ContainsAny(space, Region)
                || _owner != PlayerId.None && (!_owner.IsValid || _resolved.Visibility == SpatialLayerVisibility.Public))
                throw content.Fail("invalid tile layer setup space/location/owner");
        }
        public override Code Check(World world, IWorldSetupContext context)
        {
            if (_owner != PlayerId.None && !world.Players.IsSeat(_owner)) return Codes.NotSeated;
            if (!(world.Space is ITileSpatialSpace map) || map.Id != _resolved.Space.Id) return Codes.InvalidArgument;
            if (TileSpatialGeometry.RequireSampleLayer(world, map, _resolved, false) is not ITileWritableSampleLayer layer) return Codes.InvalidArgument;
            for (int y = Region.Y; y < Region.Bottom; y++)
                for (int x = Region.X; x < Region.Right; x++)
                {
                    Code refusal = layer.CheckSample(new TileCoord(x, y), _value);
                    if (!refusal.IsNone) return refusal;
                }
            return Code.None;
        }
        public override EntityId Generate(World world, IWorldSetupContext context)
        {
            Code refusal = Check(world, context); if (!refusal.IsNone) throw new ContentException(Id + ": startup refused " + refusal);
            var map = (ITileSpatialSpace)world.Space;
            if (_resolved.Visibility == SpatialLayerVisibility.CompanyPrivate) ((ISpatialLayerProvider)map).AssignOwner(world, _layer, _owner);
            var layer = (ITileWritableSampleLayer)TileSpatialGeometry.RequireSampleLayer(world, map, _resolved, false);
            for (int y = Region.Y; y < Region.Bottom; y++)
                for (int x = Region.X; x < Region.Right; x++) layer.SetSample(new TileCoord(x, y), _value);
            return EntityId.None;
        }
    }
    internal sealed class WorldSetupStage : WorldGenStage, ICompositionValidator
    {
        private readonly IWorldSetupProfileDefinition _profile;
        internal WorldSetupStage(IWorldSetupProfileDefinition profile) { _profile = profile; }
        public override string Id => WorldSetupModule.ModuleId;
        public void ValidateComposition(World world)
        {
            foreach (IWorldSetupDefinition entry in _profile.Graph.Nodes)
            {
                var observation = EntityObservation.Capture(world); entry.ValidateComposition(world); observation.Verify();
            }
        }
        public override void Generate(World world, ScenarioDef scenario)
        {
            var context = new Context(world);
            foreach (IWorldSetupDefinition entry in _profile.Graph.Nodes)
            {
                var observation = EntityObservation.Capture(world); Code refusal = entry.Check(world, context); observation.Verify();
                if (!refusal.IsNone) throw new ContentException(entry.Id + ": startup refused " + refusal);
                EntityId result = entry.Generate(world, context);
                if (result != EntityId.None && (!result.IsValid || world.Entities.Get(result) == null)) throw new InvalidOperationException("startup returned foreign entity identity");
                context.Add(entry.Id, result);
            }
        }
        private sealed class Context : IWorldSetupContext
        {
            private readonly World _world;
            private readonly Dictionary<string, EntityId> _results = new Dictionary<string, EntityId>(StringComparer.Ordinal);
            internal Context(World world) { _world = world; }
            internal void Add(string id, EntityId result) => _results.Add(id, result);
            public EntityId RequireEntity(string entry)
            {
                if (!_results.TryGetValue(entry, out EntityId id) || !id.IsValid || _world.Entities.Get(id) == null)
                    throw new ContentException("startup dependency has no actual entity: " + entry);
                return id;
            }
        }
    }
}
