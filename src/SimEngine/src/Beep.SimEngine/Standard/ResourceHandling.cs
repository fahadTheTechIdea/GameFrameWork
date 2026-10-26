using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum ResourceHandlingPhase : byte { Loading = 1, Unloading = 2 }
    public enum ResourceHandlingStatus : byte { Ready = 1, InvalidRequest, UnsupportedResource, OutOfRange }
    public readonly struct ResourceHandlingCheck
    {
        public ResourceHandlingStatus Status { get; }
        public long RequiredTicks { get; }
        public bool Ready => Status == ResourceHandlingStatus.Ready;
        public Code Blocker => Ready ? Code.None : Status == ResourceHandlingStatus.UnsupportedResource ? HaulHandlingCodes.Unsupported
            : Status == ResourceHandlingStatus.OutOfRange ? HaulHandlingCodes.OutOfRange : HaulHandlingCodes.Invalid;
        public ResourceHandlingCheck(ResourceHandlingStatus status, long requiredTicks = 0)
        {
            if (status < ResourceHandlingStatus.Ready || status > ResourceHandlingStatus.OutOfRange ||
                (status == ResourceHandlingStatus.Ready ? requiredTicks <= 0 : requiredTicks != 0)) throw new ArgumentException("invalid handling check");
            Status = status; RequiredTicks = requiredTicks;
        }
    }
    public interface IResourceHandlingProfile : IContentDefinition
    {
        /// <summary>Pure positive duration, monotonically nondecreasing over compatible positive native quantities.</summary>
        ResourceHandlingCheck CheckHandling(ISimulationTime time, ResourceId resource, Fix amount, ResourceHandlingPhase phase);
    }
    public sealed class AuthoredResourceHandlingDef : Def, IResourceHandlingProfile
    {
        private ResourceHandlingDeclaration _declaration;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceHandlingDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public ResourceHandlingCheck CheckHandling(ISimulationTime time, ResourceId resource, Fix amount, ResourceHandlingPhase phase)
            => _declaration.CheckHandling(time, resource, amount, phase);
    }
    /// <summary>Sole explicit linear-resource declaration, reference resolver and exact duration calculation.</summary>
    public sealed class ResourceHandlingDeclaration
    {
        private sealed class Row
        {
            internal string ResourceName;
            internal IResourceContentDefinition Resource;
            internal Fix Quantity;
            internal GameDuration Loading, Unloading;
        }
        private readonly List<Row> _rows = new List<Row>();
        private ResourceHandlingDeclaration() { }
        public static ResourceHandlingDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit resource handling declaration required");
            var declaration = new ResourceHandlingDeclaration(); declaration.ReadCore(reader); return declaration;
        }
        private void ReadCore(JsonReader reader)
        {
            JsonValue rows = reader.Array("resources");
            if (rows == null || rows.Count == 0) throw reader.Error("handling needs explicit nonempty resource rows");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                var r = new JsonReader(rows[i], reader.Where + ".resources[" + i + "]");
                var row = new Row { ResourceName = r.String("resource"), Quantity = r.Fix("quantity") };
                row.Loading = ReadDuration(r, "loading"); row.Unloading = ReadDuration(r, "unloading"); r.End();
                if (!row.Quantity.IsPositive || !ids.Add(row.ResourceName)) throw r.Error("positive native quantity and unique resource required");
                _rows.Add(row);
            }
            _rows.Sort((a, b) => string.CompareOrdinal(a.ResourceName, b.ResourceName));
        }
        private static GameDuration ReadDuration(JsonReader reader, string key)
        {
            var r = new JsonReader(reader.Object(key) ?? throw reader.Error("explicit " + key + " duration required"), reader.Where + "." + key);
            GameDuration duration = r.RationalDuration();
            if (duration.IsZero) throw reader.Error("handling duration must be positive");
            return duration;
        }
        public void Resolve(ContentResolver content)
        {
            foreach (Row row in _rows)
            {
                row.Resource = content.Def<IResourceContentDefinition>(row.ResourceName);
                if (row.Resource.QuantityUnit.Discrete && row.Quantity.Raw % Fix.One.Raw != 0)
                    throw content.Fail("handling basis must respect native resource quantum");
            }
        }
        public ResourceHandlingCheck CheckHandling(ISimulationTime time, ResourceId resource, Fix amount, ResourceHandlingPhase phase)
        {
            if (time == null || !resource.IsValid || !amount.IsPositive || phase < ResourceHandlingPhase.Loading || phase > ResourceHandlingPhase.Unloading)
                return new ResourceHandlingCheck(ResourceHandlingStatus.InvalidRequest);
            foreach (Row row in _rows)
            {
                if (row.Resource.Resource != resource) continue;
                if (row.Resource.QuantityUnit.Discrete && amount.Raw % Fix.One.Raw != 0)
                    return new ResourceHandlingCheck(ResourceHandlingStatus.InvalidRequest);
                try { return new ResourceHandlingCheck(ResourceHandlingStatus.Ready, time.Ticks(phase == ResourceHandlingPhase.Loading ? row.Loading : row.Unloading, amount.Raw, row.Quantity.Raw)); }
                catch (OverflowException) { return new ResourceHandlingCheck(ResourceHandlingStatus.OutOfRange); }
            }
            return new ResourceHandlingCheck(ResourceHandlingStatus.UnsupportedResource);
        }
    }
    public static class HaulHandlingCodes
    {
        public static readonly Code Invalid = new Code("haul.handling-invalid"), Unsupported = new Code("haul.handling-unsupported-resource"),
            OutOfRange = new Code("haul.handling-time-out-of-range");
        public static readonly Code Payment = new Code("haul.payment-wait"), Stock = new Code("haul.stock-wait"), Capacity = new Code("haul.capacity-wait");
        internal static Code Wait(ResourceTransferStatus status) => status == ResourceTransferStatus.NoPayment ? Payment
            : status == ResourceTransferStatus.NoStock ? Stock : status == ResourceTransferStatus.NoCapacity ? Capacity : Code.None;
        internal static bool IsWait(Code code) => code == Payment || code == Stock || code == Capacity;
        internal static Code PurchaseRefusal(ResourceTransferStatus status)
        {
            Code wait = Wait(status);
            if (!wait.IsNone) return wait;
            switch (status)
            {
                case ResourceTransferStatus.TradingDisabled: return ResourceTransactions.TradingDisabled;
                case ResourceTransferStatus.UnavailableCapability: return ResourceTransactions.UnavailableTradingCapability;
                case ResourceTransferStatus.SettlementRefused: return new Code("resources.purchase-settlement-refused");
                case ResourceTransferStatus.Incompatible: return new Code("resources.purchase-price-unavailable");
                case ResourceTransferStatus.MissingEndpoint: return Codes.NotFound;
                default: return Codes.InvalidArgument;
            }
        }
    }
    /// <summary>Stateless rules. Job owns progress; inventory owners keep all stock and promises.</summary>
    internal static class HaulHandling
    {
        internal static void ValidateDefinitions(World world)
        {
            foreach (EntityDef entity in world.Content.All<EntityDef>())
                foreach (ComponentDef part in entity.Components)
                {
                    bool runtime = typeof(ITransporter).IsAssignableFrom(part.ComponentType);
                    if (runtime != (part is ITransporterDefinition))
                        throw new ContentException(entity.Id + ": transporter requires matching authored ITransporterDefinition");
                    if (!runtime) continue;
                    var profile = EntityObservation.ReadProvider(world, part, null, () => {
                        var definition = (ITransporterDefinition)part;
                        definition.ValidateBinding(entity); return definition.Handling;
                    });
                    if (profile == null) throw new ContentException(entity.Id + ": explicit transporter handling required");
                    EntityObservation.ReadProvider(world, profile, null, () => { ValidateProfile(world, profile); return true; });
                }
        }
        private static void ValidateProfile(World world, IResourceHandlingProfile profile)
        {
            if (!world.Content.Owns(profile) || !ReferenceEquals(world.Content.Find<IResourceHandlingProfile>(profile.Id), profile))
                throw new ContentException("transporter handling must alias actual authored content");
        }
        internal static ResourceHandlingCheck Check(World world, Entity carrier, ResourceId resource, Fix amount, ResourceHandlingPhase phase)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            ITransporter transporter = carrier?.Get<ITransporter>();
            if (transporter == null) return new ResourceHandlingCheck(ResourceHandlingStatus.InvalidRequest);
            IResourceHandlingProfile profile = EntityObservation.ReadProvider(world, transporter, carrier, () => {
                var definition = carrier.Def.Component<ITransporterDefinition>();
                if (definition == null || !ReferenceEquals(transporter.Entity, carrier) || !definition.ComponentType.IsInstanceOfType(transporter))
                    throw new InvalidOperationException("handling requires the actual matching transporter definition and runtime");
                var actual = transporter.Handling;
                if (actual == null || !ReferenceEquals(actual, definition.Handling))
                    throw new InvalidOperationException("transporter handling differs from its authored binding");
                return actual;
            });
            return EntityObservation.ReadProvider(world, profile, carrier, () =>
            {
                ValidateProfile(world, profile);
                ResourceHandlingCheck check = profile.CheckHandling(world.Time, resource, amount, phase);
                if (check.Status < ResourceHandlingStatus.Ready || check.Status > ResourceHandlingStatus.OutOfRange ||
                    (check.Ready ? check.RequiredTicks <= 0 : check.RequiredTicks != 0))
                    throw new InvalidOperationException("handling provider returned invalid facts");
                return check;
            });
        }
        /// <summary>Greatest completed native prefix through the same authored duration contract, never a copied rate.</summary>
        internal static Fix Earned(World world, Entity carrier, ResourceId resource, Fix total,
            ResourceHandlingPhase phase, WorkProgress progress, CapacityQuantum quantum)
        {
            if (quantum.Status != CapacityStatus.Fits || quantum.Raw <= 0 || quantum.Floor(total) != total)
                throw new InvalidOperationException("handling requires exact common native quantum");
            ResourceHandlingCheck whole = Check(world, carrier, resource, total, phase);
            if (!whole.Ready) throw new InvalidOperationException("handling profile no longer admits its actual shipment");
            if (progress.Complete(whole.RequiredTicks)) return total;
            long low = 0, high = total.Raw / quantum.Raw, lowTicks = 0, highTicks = whole.RequiredTicks;
            while (low < high)
            {
                long distance = high - low, middle = low + distance / 2 + distance % 2;
                ResourceHandlingCheck check = Check(world, carrier, resource, Fix.FromRaw(checked(middle * quantum.Raw)), phase);
                if (!check.Ready || check.RequiredTicks < lowTicks || check.RequiredTicks > highTicks)
                    throw new InvalidOperationException("handling provider returned nonmonotone native prefix duration");
                if (progress.Complete(check.RequiredTicks)) { low = middle; lowTicks = check.RequiredTicks; }
                else { high = middle - 1; highTicks = check.RequiredTicks; }
            }
            return Fix.FromRaw(checked(low * quantum.Raw));
        }
        internal static Fix Handled(Job job) => Phase(job) == ResourceHandlingPhase.Loading ? job.Loaded : job.Amount - job.Loaded;
        // Completed delivery at earlier outputs is not handling credit for the current output.
        internal static Fix LegAmount(Job job) => Phase(job) == ResourceHandlingPhase.Loading ? job.Amount : job.Amount - job.UnloadingBaseline;
        internal static ResourceHandlingPhase Phase(Job job) => !job.From.IsValid || job.ToilIndex >= 2 ? ResourceHandlingPhase.Unloading : ResourceHandlingPhase.Loading;
    }
}
