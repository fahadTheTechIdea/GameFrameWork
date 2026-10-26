using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed class LogisticsDemandSettings
    {
        public bool Enabled { get; }
        public Fix Minimum { get; }
        public Fix Desired { get; }
        public Fix Maximum { get; }
        public int Priority { get; }
        public LogisticsReserveAccess ReserveAccess { get; }
        public Code Selection { get; }
        public LogisticsDemandSettings(bool enabled, Fix minimum, Fix desired, Fix maximum, int priority, LogisticsReserveAccess reserveAccess, Code selection = default)
        {
            if (minimum < Fix.Zero || desired < minimum || maximum < desired || priority < 0)
                throw new ArgumentException("stock policy requires ordered nonnegative native bands and priority");
            if (reserveAccess != LogisticsReserveAccess.Protected && reserveAccess != LogisticsReserveAccess.EmergencyOnly)
                throw new ArgumentException("stock policy requires explicit reserve access");
            Enabled = enabled; Minimum = minimum; Desired = desired; Maximum = maximum; Priority = priority; ReserveAccess = reserveAccess; Selection = selection;
        }
        public bool Matches(LogisticsDemandSettings other) => other != null && Enabled == other.Enabled && Minimum == other.Minimum &&
            Desired == other.Desired && Maximum == other.Maximum && Priority == other.Priority && ReserveAccess == other.ReserveAccess && Selection == other.Selection;
        public static LogisticsDemandSettings Read(JsonReader r)
        {
            string selection = r.Has("selection") ? r.String("selection") : null;
            try { return new LogisticsDemandSettings(r.Bool("enabled"), r.Fix("minimum"), r.Fix("desired"), r.Fix("maximum"), r.Int("priority"), LogisticsPolicyRules.ReadReserveAccess(r), selection == null ? Code.None : new Code(selection)); }
            catch (ArgumentException e) { throw r.Error(e.Message); }
        }
        public static LogisticsDemandSettings Sync(IStateSync sync, LogisticsDemandSettings settings)
        {
            if (!sync.Reading && settings == null) throw new CorruptStateException("missing demand settings");
            bool enabled = settings?.Enabled ?? false; Fix min = settings?.Minimum ?? Fix.Zero, desired = settings?.Desired ?? Fix.Zero, max = settings?.Maximum ?? Fix.Zero;
            int priority = settings?.Priority ?? 0;
            sync.Bool(ref enabled); sync.Fix(ref min); sync.Fix(ref desired); sync.Fix(ref max); sync.Int(ref priority);
            byte access = (byte)(settings?.ReserveAccess ?? 0); sync.Byte(ref access);
            string selection = settings?.Selection.Id; sync.String(ref selection);
            if (!sync.Reading) return settings;
            try { return new LogisticsDemandSettings(enabled, min, desired, max, priority, (LogisticsReserveAccess)access, selection == null ? Code.None : new Code(selection)); }
            catch (ArgumentException) { throw new CorruptStateException("invalid saved demand settings"); }
        }
    }
    public interface ILogisticsDemand { LogisticsDemandSettings ReadDemand(); }
    public enum LogisticsStockLevel : byte { Minimum = 1, Desired = 2, Maximum = 3 }
    /// <summary>Immutable normal-command intent. The logistics demand owner resolves existing bands.</summary>
    public sealed class LogisticsDemandChange
    {
        public enum ChangeKind : byte { Replace = 1, Level = 2 }
        private LogisticsDemandChange(ChangeKind kind, LogisticsDemandSettings settings, LogisticsStockLevel level, Fix target, bool enabled)
        { Kind = kind; Settings = settings; StockLevel = level; Target = target; Enabled = enabled; }
        public ChangeKind Kind { get; }
        public LogisticsDemandSettings Settings { get; }
        public Fix Target { get; }
        public bool Enabled { get; }
        public LogisticsStockLevel StockLevel { get; }
        public static LogisticsDemandChange Replace(LogisticsDemandSettings settings)
            => new LogisticsDemandChange(ChangeKind.Replace, settings ?? throw new ArgumentNullException(nameof(settings)), default, Fix.Zero, false);
        public static LogisticsDemandChange Level(LogisticsStockLevel level, Fix target, bool enabled)
        {
            if (level < LogisticsStockLevel.Minimum || level > LogisticsStockLevel.Maximum || target < Fix.Zero)
                throw new ArgumentException("stock-level change requires an explicit level and nonnegative native quantity");
            return new LogisticsDemandChange(ChangeKind.Level, null, level, target, enabled);
        }
        public static LogisticsDemandChange Sync(IStateSync sync, LogisticsDemandChange change)
        {
            if (!sync.Reading && change == null) throw new CorruptStateException("missing stock change");
            byte kind = (byte)(change?.Kind ?? 0); sync.Byte(ref kind);
            if (kind == (byte)ChangeKind.Replace)
            {
                var settings = LogisticsDemandSettings.Sync(sync, change?.Settings);
                return sync.Reading ? Replace(settings) : change;
            }
            if (kind != (byte)ChangeKind.Level) throw new CorruptStateException("unknown stock change kind");
            byte level = (byte)(change?.StockLevel ?? 0); sync.Byte(ref level);
            Fix target = change?.Target ?? Fix.Zero; bool enabled = change?.Enabled ?? false;
            sync.Fix(ref target); sync.Bool(ref enabled);
            if (!sync.Reading) return change;
            try { return Level((LogisticsStockLevel)level, target, enabled); }
            catch (ArgumentException) { throw new CorruptStateException("invalid saved stock-level change"); }
        }
    }
    public interface ILogisticsStockPolicies
    {
        Code CheckStockPolicy(PlayerId company, EntityId store, ResourceId resource, LogisticsDemandChange change, Code port = default, ResourceEndpointId? endpoint = null);
        Code ConfigureStockPolicy(PlayerId company, EntityId store, ResourceId resource, LogisticsDemandChange change, Code port = default, ResourceEndpointId? endpoint = null);
        bool TryReadStockPolicy(EntityId store, ResourceId resource, out LogisticsDemandSettings settings, Code port = default, ResourceEndpointId? endpoint = null);
        void CopyStockPolicyTargets(EntityId store, ResourceId resource, IList<LogisticsStockPolicyTarget> destination);
        /// <summary>Pure declared target prediction; independent of whether persistent references have rebound yet.</summary>
        Code CheckStockTargetReplacement(Entity source, EntityDef destination, ResourceId resource, Code port, ResourceEndpointId original, Fix quantity);
        /// <summary>Validate actual prepared access and return its exact target endpoint without editing stock, policy or chronology.</summary>
        Code RebindStockTarget(Entity source, Entity destination, ResourceId resource, Code port, ResourceEndpointId original, out ResourceEndpointId endpoint);
    }
    public enum LogisticsReserveAccess : byte { Protected = 1, EmergencyOnly = 2 }
    public sealed class OutletPolicySettings
    {
        public bool Enabled { get; }
        public Bill Reserve { get; }
        public int ExportPriority { get; }
        public IReadOnlyList<string> AllowedDestinations { get; }
        public LogisticsReserveAccess ReserveAccess { get; }
        public OutletPolicySettings(bool enabled, Bill reserve, int exportPriority, IEnumerable<string> allowedDestinations, LogisticsReserveAccess reserveAccess)
        {
            if (reserve == null || exportPriority < 0) throw new ArgumentException("export policy requires explicit reserves and nonnegative priority");
            if (reserveAccess != LogisticsReserveAccess.Protected && reserveAccess != LogisticsReserveAccess.EmergencyOnly)
                throw new ArgumentException("export policy requires an explicit reserve access mode");
            if (allowedDestinations != null)
            {
                var ids = new List<string>(allowedDestinations); ids.Sort(StringComparer.Ordinal);
                for (int i = 0; i < ids.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(ids[i])) throw new ArgumentException("destination prototype needs an explicit content identity");
                    if (i > 0 && ids[i] == ids[i - 1]) throw new ArgumentException("duplicate destination prototype");
                }
                AllowedDestinations = ids.AsReadOnly();
            }
            Enabled = enabled; Reserve = reserve; ExportPriority = exportPriority; ReserveAccess = reserveAccess;
        }
        public bool Allows(string prototype)
        { if (AllowedDestinations == null) return true; foreach (string id in AllowedDestinations) if (id == prototype) return true; return false; }
        public bool Matches(OutletPolicySettings other)
        {
            if (other == null || Enabled != other.Enabled || ExportPriority != other.ExportPriority || ReserveAccess != other.ReserveAccess ||
                Reserve.Count != other.Reserve.Count || (AllowedDestinations == null) != (other.AllowedDestinations == null)) return false;
            for (int i = 0; i < Reserve.Count; i++)
                if (Reserve[i].Resource != other.Reserve[i].Resource || Reserve[i].Amount != other.Reserve[i].Amount) return false;
            if (AllowedDestinations != null)
            {
                if (AllowedDestinations.Count != other.AllowedDestinations.Count) return false;
                for (int i = 0; i < AllowedDestinations.Count; i++) if (AllowedDestinations[i] != other.AllowedDestinations[i]) return false;
            }
            return true;
        }
        public static OutletPolicySettings Sync(IStateSync sync, OutletPolicySettings settings)
        {
            if (!sync.Reading && settings == null) throw new CorruptStateException("missing outlet settings");
            bool enabled = settings?.Enabled ?? false, filtered = settings?.AllowedDestinations != null; int priority = settings?.ExportPriority ?? 0;
            byte access = (byte)(settings?.ReserveAccess ?? 0);
            sync.Bool(ref enabled); sync.Int(ref priority); sync.Byte(ref access); sync.Bool(ref filtered);
            int count = sync.Count(settings?.Reserve.Count ?? 0); var quantities = new List<ResourceAmount>(); ResourceId previous = default;
            for (int i = 0; i < count; i++)
            {
                ResourceId resource = sync.Reading ? default : settings.Reserve[i].Resource; Fix quantity = sync.Reading ? default : settings.Reserve[i].Amount;
                sync.Resource(ref resource); sync.Fix(ref quantity);
                if (!resource.IsValid || !quantity.IsPositive || i > 0 && resource.Value <= previous.Value) throw new CorruptStateException("invalid canonical reserve bill");
                previous = resource; quantities.Add(new ResourceAmount(resource, quantity));
            }
            var destinations = filtered ? new List<string>() : null; count = filtered ? sync.Count(settings?.AllowedDestinations.Count ?? 0) : 0;
            string prior = null;
            for (int i = 0; i < count; i++)
            {
                string id = sync.Reading ? null : settings.AllowedDestinations[i]; sync.String(ref id);
                if (prior != null && string.CompareOrdinal(prior, id) >= 0) throw new CorruptStateException("noncanonical destination filter");
                prior = id; destinations.Add(id);
            }
            if (!sync.Reading) return settings;
            try { return new OutletPolicySettings(enabled, new Bill(quantities), priority, destinations, (LogisticsReserveAccess)access); }
            catch (ArgumentException) { throw new CorruptStateException("invalid saved outlet settings"); }
        }
    }
    public interface IOutletPolicy : IEntityCapability
    {
        void CopyOutletPolicyPorts(IList<Code> destination);
        OutletPolicySettings ReadOutletPolicy(Code port = default);
        Code CheckOutletPolicy(World world, OutletPolicySettings settings, Code port = default);
        void ConfigureOutletPolicy(World world, OutletPolicySettings settings, Code port = default);
    }
    /// <summary>Detached command intent; providers alone own current outlet settings.</summary>
    public sealed class OutletPolicyChange
    {
        public enum ChangeKind : byte { Replace = 1, Reserve = 2 }
        private OutletPolicyChange(ChangeKind kind, OutletPolicySettings settings, ResourceId resource, Fix amount, bool enabled)
        { Kind = kind; Settings = settings; Resource = resource; Amount = amount; Enabled = enabled; }
        public ChangeKind Kind { get; }
        public OutletPolicySettings Settings { get; }
        public ResourceId Resource { get; }
        public Fix Amount { get; }
        public bool Enabled { get; }
        public static OutletPolicyChange Replace(OutletPolicySettings settings)
            => new OutletPolicyChange(ChangeKind.Replace, settings ?? throw new ArgumentNullException(nameof(settings)), default, Fix.Zero, false);
        public static OutletPolicyChange Reserve(ResourceId resource, Fix amount, bool enabled)
        {
            if (!resource.IsValid || amount < Fix.Zero) throw new ArgumentException("reserve change requires a resource and nonnegative native quantity");
            return new OutletPolicyChange(ChangeKind.Reserve, null, resource, amount, enabled);
        }
        public static OutletPolicyChange Sync(IStateSync sync, OutletPolicyChange change)
        {
            if (!sync.Reading && change == null) throw new CorruptStateException("missing outlet change");
            byte kind = (byte)(change?.Kind ?? 0); sync.Byte(ref kind);
            if (kind == (byte)ChangeKind.Replace)
            {
                var settings = OutletPolicySettings.Sync(sync, change?.Settings);
                return sync.Reading ? Replace(settings) : change;
            }
            if (kind != (byte)ChangeKind.Reserve) throw new CorruptStateException("unknown outlet change kind");
            ResourceId resource = change?.Resource ?? default; Fix amount = change?.Amount ?? Fix.Zero; bool enabled = change?.Enabled ?? false;
            sync.Resource(ref resource); sync.Fix(ref amount); sync.Bool(ref enabled);
            if (!sync.Reading) return change;
            try { return Reserve(resource, amount, enabled); }
            catch (ArgumentException) { throw new CorruptStateException("invalid saved outlet reserve change"); }
        }
    }
    public static class LogisticsPolicyCodes
    {
        public static readonly Code Disabled = new Code("logistics.export-disabled"), Destination = new Code("logistics.export-destination"),
            Reserve = new Code("logistics.export-reserve"), Maximum = new Code("logistics.stock-maximum"), NoPolicy = new Code("logistics.no-outlet-policy"),
            NoStockPolicy = new Code("logistics.no-stock-policy"),
            StockPolicyOwner = new Code("logistics.stock-policy-owner");
    }
    public readonly struct StockPolicyEntry
    {
        public ResourceId Resource { get; }
        public LogisticsDemandSettings Settings { get; }
        public StockPolicyEntry(ResourceId resource, LogisticsDemandSettings settings) { Resource = resource; Settings = settings; }
    }
    public sealed class LogisticsPolicyDef : Def, ILogisticsPolicyDefinition
    {
        private LogisticsPolicyDeclaration _declaration;
        public IReadOnlyList<StockPolicyEntry> Stock => _declaration.Stock;
        public OutletPolicySettings Outlet => _declaration.Outlet;
        public EntityStateBindings States => _declaration.States;
        protected override void ReadCore(JsonReader reader) => _declaration = LogisticsPolicyDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => LogisticsPolicyDeclaration.Validate(content.Content, this);
    }
    public static partial class LogisticsPolicyRules
    {
        internal static Code CheckReplacementPort(World world, EntityDef destination, Code port, ResourceId resource,
            Fix quantity, bool outlet, out ICapacityPolicy capacity, bool entityInventory = false)
        {
            capacity = null; bool accepts;
            if (port.IsNone)
            {
                var definition = destination.Component<IInventoryDefinition>();
                if (definition == null || !destination.HasCapability<IInletPort>() || outlet && !destination.HasCapability<IOutletPort>())
                    return StandardCodes.NoStorage;
                capacity = definition.CapacityPolicy; accepts = definition.Accepts(world.Content, resource);
            }
            else
            {
                var definition = destination.Component<IResourcePortDefinition>(port);
                if (definition == null) return Codes.NotFound;
                if (entityInventory && !definition.BindsEntityInventory) return LogisticsService.ReplacementIndependentEndpoint;
                capacity = definition.CapacityPolicy; accepts = definition.Accepts(world.Content, resource);
            }
            if (!accepts) return StandardCodes.NotAccepted;
            return Native(capacity, resource, quantity) ? Code.None : Codes.InvalidArgument;
        }
        public static LogisticsReserveAccess ReadReserveAccess(JsonReader reader)
        {
            string access = reader.String("reserve_access");
            return access == "protected" ? LogisticsReserveAccess.Protected : access == "emergency_only" ? LogisticsReserveAccess.EmergencyOnly
                : throw reader.Error("reserve_access must be protected or emergency_only");
        }
        /// <summary>One initializer for authored stock rows; existing logistics owns policy and refill.</summary>
        public static void InitializeStock(World world, Entity entity, ILogisticsPolicyDefinition profile, Code port = default)
        {
            if (world == null || entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                profile == null || !world.Content.Owns(profile) || !ReferenceEquals(world.Content.Find<ILogisticsPolicyDefinition>(profile.Id), profile))
                throw new ArgumentException("stock initialization requires actual entity/content");
            var stock = EntityObservation.ReadProvider(world, profile, entity, () => {
                LogisticsPolicyDeclaration.Validate(world.Content, profile); return new List<StockPolicyEntry>(profile.Stock).AsReadOnly();
            });
            foreach (StockPolicyEntry entry in stock)
            {
                var policies = world.Services.Get<ILogisticsStockPolicies>();
                if (policies.TryReadStockPolicy(entity.Id,entry.Resource,out var current,port) && !current.Matches(entry.Settings))
                    throw new ContentException(entity.Def.Id + ": aliased authored stock bands conflict for the same actual store");
                Code result = policies.ConfigureStockPolicy(entity.Owner, entity.Id, entry.Resource, LogisticsDemandChange.Replace(entry.Settings), port);
                if (!result.IsNone) throw new InvalidOperationException("initial stock policy refused: " + result);
            }
        }
        // IOutletPolicy is singular per entity. All action profiles claim this same public contract slot.
        public static readonly Code OutletPolicyBinding = new Code("logistics.outlet-policy");
        public static Code OutletPolicyTarget(Code port)
            => port.IsNone ? OutletPolicyBinding : new Code(OutletPolicyBinding.Id + "." + PolicyField(port.Id));
        private static string PolicyField(string value) => value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + value;
        public static Code StockPolicyBinding(IResourceDefinition resource, Code port = default, ResourceEndpointId? endpoint = null)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            ContractIdentity.ValidateId(resource.Id, "stock resource");
            string prefix = "logistics.stock." + resource.Id;
            if (port.IsNone) return new Code(prefix);
            if (!endpoint.HasValue || !endpoint.Value.IsValid) throw new ArgumentException("named stock target requires its actual endpoint");
            return new Code(prefix + "." + PolicyField(port.Id) + "." + PolicyField(endpoint.Value.Scope) + "." + endpoint.Value.Identity.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        internal static bool ParsePolicyTarget(Code target, string prefix, bool stock, out Code port, out ResourceEndpointId endpoint)
        {
            port = Code.None; endpoint = default;
            if (target.Id == prefix) return true; // Explicit entity-local intent, not a named-port fallback.
            if (target.Id == null || !target.Id.StartsWith(prefix + ".", StringComparison.Ordinal)) return false;
            string tail = target.Id.Substring(prefix.Length + 1); int position = 0;
            bool Field(out string value)
            {
                value = null; int end = tail.IndexOf('.',position);
                if (end < 0 || !int.TryParse(tail.Substring(position,end-position),System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,out int length) || length < 1 || length > tail.Length-end-1) return false;
                value = tail.Substring(end+1,length); position = end+1+length;
                if (position < tail.Length) { if (tail[position] != '.') return false; position++; }
                return true;
            }
            if (!Field(out string name)) return false;
            try { ContractIdentity.ValidateId(name,"policy resource port"); } catch (ArgumentException) { return false; }
            port = new Code(name);
            if (!stock) return position == tail.Length && OutletPolicyTarget(port) == target;
            if (!Field(out string scope) || !long.TryParse(tail.Substring(position),System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,out long identity)) return false;
            endpoint = new ResourceEndpointId(scope,identity); return endpoint.IsValid;
        }
        public static Code CheckChange(World world, PlayerId player, Entity entity, OutletPolicyChange change, out OutletPolicySettings settings, Code port = default)
        {
            settings = null; if (world == null) return Codes.NotFound;
            world.RequireHealthyUpdate();
            if (entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                entity.Owner != player || !world.ViewFor(player).CanObserveOwned(entity)) return Codes.NotFound;
            return CheckProviderChange(world, entity, change, out settings, port);
        }
        private static Code CheckProviderChange(World world, Entity entity, OutletPolicyChange change, out OutletPolicySettings settings, Code port = default)
        {
            settings = null;
            if (world == null || entity?.Alive != true || !ReferenceEquals(world.Entities.Get(entity.Id), entity)) return Codes.NotFound;
            if (change == null) return Codes.InvalidArgument;
            IOutletPolicy policy = entity.Get<IOutletPolicy>(); if (policy == null) return LogisticsPolicyCodes.NoPolicy;
            var observation = EntityObservation.CaptureProvider(world, policy);
            IResourceInlet observed = null; int version = 0; ResourceEndpointId identity = default; PlayerId company = default;
            try
            {
                if (policy.ReadOutletPolicy(port) == null) return LogisticsPolicyCodes.NoPolicy;
                IResourceInlet inlet = PolicyPort(world, entity, port);
                if (inlet == null) return Codes.InvalidArgument;
                observed = inlet; version = inlet.Version; identity = inlet.Endpoint; company = inlet.Company;
                OutletPolicySettings next = change.Settings;
                if (change.Kind == OutletPolicyChange.ChangeKind.Reserve)
                {
                    if (world.Content.Resource(change.Resource) == null || !inlet.Accepts(world.Content, change.Resource)
                        || !Native(inlet.CapacityPolicy, change.Resource, change.Amount)) return Codes.InvalidArgument;
                    var current = Read(world, entity, port);
                    var lines = new List<ResourceAmount>();
                    foreach (var line in current.Reserve.Lines) if (line.Resource != change.Resource) lines.Add(line);
                    if (change.Amount.IsPositive) lines.Add(new ResourceAmount(change.Resource, change.Amount));
                    next = new OutletPolicySettings(change.Enabled, new Bill(lines), current.ExportPriority, current.AllowedDestinations, current.ReserveAccess);
                }
                Code refusal = CheckOutlet(world.Content, inlet, next);
                if (refusal.IsNone) refusal = policy.CheckOutletPolicy(world, next, port);
                if (refusal.IsNone) settings = next;
                return refusal;
            }
            finally
            {
                observation.Verify();
                if (observed != null && (observed.Version != version || observed.Endpoint != identity || observed.Company != company))
                    throw new InvalidOperationException("outlet admission mutated actual resource endpoint");
            }
        }
        internal static bool Native(ICapacityPolicy capacity, ResourceId resource, Fix amount) => CapacityPolicyContracts.Quantum(capacity, resource).Floor(amount) == amount;
        public static void ValidateProfile(ResolvedContent content, EntityDef owner, ILogisticsPolicyDefinition profile, Code port = default)
        {
            if (content == null || owner == null || !content.Owns(owner)) throw new ContentException("logistics policy requires actual entity definition");
            LogisticsPolicyDeclaration.Validate(content, profile);
            IInventoryDefinition local = port.IsNone ? owner.Component<IInventoryDefinition>() : null;
            IResourcePortDefinition named = port.IsNone ? null : owner.Component<IResourcePortDefinition>(port);
            if (port.IsNone && (!owner.HasCapability<IOutletPort>() || local == null) || !port.IsNone && named == null)
                throw new ContentException(owner.Id + ": logistics policy needs its exact authored resource port");
            ICapacityPolicy capacity = local?.CapacityPolicy ?? named.CapacityPolicy;
            bool Accepts(ResourceId resource) => local != null ? local.Accepts(content, resource) : named.Accepts(content, resource);
            var maxima = new List<ResourceAmount>();
            foreach (StockPolicyEntry entry in profile.Stock)
            {
                if (!Accepts(entry.Resource) || !Native(capacity, entry.Resource, entry.Settings.Minimum) ||
                    !Native(capacity, entry.Resource, entry.Settings.Desired) || !Native(capacity, entry.Resource, entry.Settings.Maximum))
                    throw new ContentException(owner.Id + ": incompatible native stock band");
                if (entry.Settings.Maximum.IsPositive) maxima.Add(new ResourceAmount(entry.Resource, entry.Settings.Maximum));
            }
            if (!CapacityPolicyContracts.Check(capacity, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), maxima).Fits)
                throw new ContentException(owner.Id + ": complete stock maxima violate native capacity");
            foreach (ResourceAmount line in profile.Outlet.Reserve.Lines)
                if (!Accepts(line.Resource) || !Native(capacity, line.Resource, line.Amount)) throw new ContentException(owner.Id + ": incompatible reserve");
            if (!CapacityPolicyContracts.Check(capacity, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), profile.Outlet.Reserve.Lines).Fits)
                throw new ContentException(owner.Id + ": complete reserves violate native capacity");
        }
        public static Code CheckOutlet(ResolvedContent content, IResourceInlet port, OutletPolicySettings settings)
        {
            if (port == null || settings == null) return Codes.InvalidArgument;
            foreach (ResourceAmount reserve in settings.Reserve.Lines)
                if (content.Resource(reserve.Resource) == null || !port.Accepts(content, reserve.Resource) || !Native(port.CapacityPolicy, reserve.Resource, reserve.Amount)) return Codes.InvalidArgument;
            if (!CapacityPolicyContracts.Check(port.CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), settings.Reserve.Lines).Fits) return Codes.InvalidArgument;
            if (settings.AllowedDestinations != null) foreach (string id in settings.AllowedDestinations) if (content.Find<EntityDef>(id) == null) return Codes.InvalidArgument;
            return Code.None;
        }
        internal static IResourceInlet PolicyPort(World world, Entity source, Code port)
        {
            if (port.IsNone) return source.Get<IInletPort>();
            IResourcePort named = source.Get<IResourcePort>(port);
            return named == null ? null : ResourcePortContracts.Store(world, source, named).Store;
        }
        public static OutletPolicySettings Read(World world, Entity source, Code port = default)
        {
            if (world == null || source == null || !source.Alive || !ReferenceEquals(world.Entities.Get(source.Id), source))
                throw new ArgumentException("outlet observation requires an actual live entity in this world");
            var policy = source.Get<IOutletPolicy>(); if (policy == null) return null;
            var observation = EntityObservation.CaptureProvider(world, policy);
            try
            {
                var settings = policy.ReadOutletPolicy(port);
                if (settings == null && !port.IsNone && PolicyPort(world,source,port)?.Endpoint == ResourceEndpointId.ForEntity(source.Id))
                    settings = policy.ReadOutletPolicy(); // Explicit alias of the same local store, never another scoped store.
                if (settings == null) return null; // This owner has no policy for the explicitly requested port.
                if (!CheckOutlet(world.Content, PolicyPort(world, source, port), settings).IsNone)
                    throw new InvalidOperationException("invalid actual outlet policy facts");
                return settings;
            }
            finally { observation.Verify(); }
        }
        /// <summary>One exact settings commit for single commands, automation and named selections.</summary>
        public static void Configure(World world, Entity entity, OutletPolicyChange change, Code port = default)
        {
            Code refusal = CheckProviderChange(world, entity, change, out var settings, port);
            if (!refusal.IsNone) throw new InvalidOperationException("outlet policy refused: " + refusal);
            if (settings.Matches(Read(world, entity, port))) return;
            try
            {
                int version = entity.Version; var owner = entity.Owner; long time = world.Tick; var next = world.Entities.NextIdentity;
                var worker = entity.Get<IWorker>(); int job = worker?.Job ?? 0;
                EntityId home = worker == null ? EntityId.None : WorkerHomeContracts.ReadHome(world, entity);
                bool held = worker?.Held ?? false, arrived = worker?.HeldArrived ?? false;
                entity.Get<IOutletPolicy>().ConfigureOutletPolicy(world, settings, port);
                if (!settings.Matches(Read(world, entity, port)) || entity.Version <= version || entity.Owner != owner || world.Tick != time ||
                    world.Entities.NextIdentity != next || !ReferenceEquals(world.Entities.Get(entity.Id), entity) || !entity.Alive ||
                    worker != null && (worker.Job != job || WorkerHomeContracts.ReadHome(world, entity) != home || worker.Held != held || worker.HeldArrived != arrived))
                    throw new InvalidOperationException("outlet policy did not commit exact settings");
            }
            catch (Exception error) { world.LatchFault("logistics.configure-outlet-policy", error); throw; }
        }
    }
    public readonly struct LogisticsPolicyPortProfile
    {
        public Code Port { get; }
        public ILogisticsPolicyDefinition Profile { get; }
        public LogisticsPolicyPortProfile(Code port, ILogisticsPolicyDefinition profile) { Port = port; Profile = profile; }
    }
    public sealed class LogisticsPolicyComponentDef : ComponentDef<LogisticsPolicyComponent>
    {
        private readonly List<(Code Port, string Profile)> _profiles = new List<(Code, string)>();
        private ResolvedContent _content;
        internal ResolvedContent Content => _content;
        public IReadOnlyList<LogisticsPolicyPortProfile> Profiles { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            JsonValue profiles = r.Array("profiles");
            if (profiles == null || profiles.Count == 0) throw r.Error("policy profiles must explicitly bind at least one resource port");
            var seen = new HashSet<Code>();
            for (int i = 0; i < profiles.Count; i++)
            {
                var entry = new JsonReader(profiles[i], r.Where + ".profiles[" + i + "]");
                JsonValue name = entry.Value("port");
                if (name == null || name.Kind != JsonKind.Null && name.Kind != JsonKind.String)
                    throw entry.Error("port must be explicit null for entity stock or a named resource port");
                Code port = Code.None;
                if (name.Kind == JsonKind.String)
                {
                    try { ContractIdentity.ValidateId(name.Text, "policy resource port"); }
                    catch (ArgumentException error) { throw entry.Error(error.Message); }
                    port = new Code(name.Text);
                }
                if (!seen.Add(port)) throw entry.Error("duplicate policy resource port");
                _profiles.Add((port, entry.String("profile"))); entry.End();
            }
            _profiles.Sort((a,b) => string.CompareOrdinal(a.Port.Id, b.Port.Id));
        }
        protected override void ResolveCore(ContentResolver c)
        {
            _content = c.Content;
            var profiles = new List<LogisticsPolicyPortProfile>();
            foreach (var entry in _profiles) profiles.Add(new LogisticsPolicyPortProfile(entry.Port, c.Def<ILogisticsPolicyDefinition>(entry.Profile)));
            Profiles = profiles.AsReadOnly();
        }
        protected override void ValidateCore(EntityDef owner)
        { foreach (var entry in Profiles) LogisticsPolicyRules.ValidateProfile(_content, owner, entry.Profile, entry.Port); }
    }
    // One entity-owned policy component. Actual stock/refill/age remain in existing demand rows.
    public sealed class LogisticsPolicyComponent : Component<LogisticsPolicyComponentDef>, IOutletPolicy, IEntityInitialization, IEntityStateSource
    {
        private readonly Dictionary<Code, OutletPolicySettings> _outlets = new Dictionary<Code, OutletPolicySettings>();
        public override void OnCreate()
        { foreach (var entry in Def.Profiles) _outlets.Add(entry.Port, entry.Profile.Outlet); }
        public void Initialize(World world)
        { foreach (var entry in Def.Profiles) LogisticsPolicyRules.InitializeStock(world, Entity, entry.Profile, entry.Port); }
        public void CopyOutletPolicyPorts(IList<Code> destination)
        { destination.Clear(); foreach (var entry in Def.Profiles) destination.Add(entry.Port); }
        public OutletPolicySettings ReadOutletPolicy(Code port = default)
            => _outlets.TryGetValue(port, out var settings) ? settings : null;
        public Code CheckOutletPolicy(World world, OutletPolicySettings settings, Code port = default)
        {
            if (!ReferenceEquals(world.Entities.Get(Entity.Id), Entity)) return Codes.NotFound;
            if (!_outlets.ContainsKey(port)) return LogisticsPolicyCodes.NoPolicy;
            return LogisticsPolicyRules.CheckOutlet(world.Content, LogisticsPolicyRules.PolicyPort(world, Entity, port), settings);
        }
        public void ConfigureOutletPolicy(World world, OutletPolicySettings settings, Code port = default)
        {
            Code refusal = CheckOutletPolicy(world, settings, port);
            if (!refusal.IsNone) throw new InvalidOperationException("outlet policy refused: " + refusal);
            _outlets[port] = settings; Entity.Touch();
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
        {
            foreach (var entry in Def.Profiles) if (_outlets[entry.Port].Enabled) return null;
            return Def.Profiles[0].Profile.States["disabled"]; // One disabled alias does not shut down every operation on its physical site.
        }
        public override void Sync(IStateSync sync)
        {
            int count = sync.Count(Def.Profiles.Count);
            if (count != Def.Profiles.Count) throw new CorruptStateException("policy ports differ from authored bindings");
            foreach (var entry in Def.Profiles)
            {
                string name = entry.Port.Id; sync.String(ref name);
                if (name != entry.Port.Id) throw new CorruptStateException("saved outlet policy port differs");
                var next = OutletPolicySettings.Sync(sync, ReadOutletPolicy(entry.Port));
                // Validate against immutable exact-port metadata; live providers are observed after restore.
                foreach (var line in next.Reserve.Lines)
                {
                    var local = entry.Port.IsNone ? Entity.Def.Component<IInventoryDefinition>() : null;
                    var named = entry.Port.IsNone ? null : Entity.Def.Component<IResourcePortDefinition>(entry.Port);
                    var capacity = local?.CapacityPolicy ?? named?.CapacityPolicy;
                    bool accepts = local != null ? local.Accepts(Def.Content, line.Resource) : named?.Accepts(Def.Content, line.Resource) == true;
                    if (!accepts || capacity == null || !LogisticsPolicyRules.Native(capacity,line.Resource,line.Amount))
                        throw new CorruptStateException("invalid native outlet reserve");
                }
                var definition = entry.Port.IsNone ? Entity.Def.Component<IInventoryDefinition>().CapacityPolicy
                    : Entity.Def.Component<IResourcePortDefinition>(entry.Port).CapacityPolicy;
                if (!definition.Check(Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), next.Reserve.Lines).Fits)
                    throw new CorruptStateException("saved outlet reserves exceed native capacity");
                if (next.AllowedDestinations != null)
                    foreach (string id in next.AllowedDestinations)
                        if (Def.Content.Find<EntityDef>(id) == null) throw new CorruptStateException("unknown saved outlet destination");
                _outlets[entry.Port] = next;
            }
        }
    }
}
