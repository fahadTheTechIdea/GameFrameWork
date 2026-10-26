using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public readonly struct LogisticsDemandId : IEquatable<LogisticsDemandId>
    {
        public string Scope { get; }
        public long Identity { get; }
        public Code Entry { get; }
        public bool IsValid => Scope != null && Identity > 0;
        public LogisticsDemandId(string scope, long identity, Code entry)
        {
            ContractIdentity.ValidateId(scope, "logistics demand");
            if (identity <= 0) throw new ArgumentOutOfRangeException(nameof(identity));
            if (!entry.IsNone) ContractIdentity.ValidateId(entry.Id, "logistics demand entry");
            Scope = scope; Identity = identity; Entry = entry;
        }
        public bool Equals(LogisticsDemandId other) => Scope == other.Scope && Identity == other.Identity && Entry == other.Entry;
        public override bool Equals(object obj) => obj is LogisticsDemandId other && Equals(other);
        public override int GetHashCode() => (Scope?.GetHashCode() ?? 0) ^ Identity.GetHashCode() ^ Entry.GetHashCode();
        public static void Sync(IStateSync sync, ref LogisticsDemandId source)
        {
            string scope = source.Scope, entry = source.Entry.Id; long identity = source.Identity;
            sync.String(ref scope); sync.Long(ref identity); sync.String(ref entry);
            try { var value = new LogisticsDemandId(scope, identity, entry == null ? Code.None : new Code(entry)); if (sync.Reading) source = value; }
            catch (ArgumentException ex) { throw new CorruptStateException("invalid logistics demand identity: " + ex.Message); }
        }
    }
    public enum LogisticsDemandStatus : byte { Active = 1, Expired = 2, Invalid = 3 }
    public readonly struct LogisticsDemandSourceCheck
    {
        public LogisticsDemandStatus Status { get; }
        public Code Refusal { get; }
        public bool Emergency { get; }
        public LogisticsDemandSourceCheck(LogisticsDemandStatus status, Code refusal, bool emergency)
        { Status = status; Refusal = refusal; Emergency = emergency; }
        public bool IsWellFormed => Status >= LogisticsDemandStatus.Active && Status <= LogisticsDemandStatus.Invalid &&
            (Status == LogisticsDemandStatus.Active ? Refusal.IsNone : !Refusal.IsNone && !Emergency);
        public static LogisticsDemandSourceCheck Active(bool emergency) => new LogisticsDemandSourceCheck(LogisticsDemandStatus.Active, Code.None, emergency);
        public static LogisticsDemandSourceCheck Expired(Code reason) => new LogisticsDemandSourceCheck(LogisticsDemandStatus.Expired, reason, false);
        public static LogisticsDemandSourceCheck Invalid(Code reason) => new LogisticsDemandSourceCheck(LogisticsDemandStatus.Invalid, reason, false);
    }
    /// <summary>A declared service checks its own existing workflow records, never another demand store.</summary>
    public interface ILogisticsDemandOwner
    {
        World World { get; }
        string DemandScope { get; }
        /// <summary>Pure whole source metadata authorization; physical source liveness never depends on this projection.</summary>
        Code CheckSourceDisclosure(PlayerId viewer, LogisticsDemandId source, PlayerId company, EntityId destination);
        LogisticsDemandSourceCheck CheckSource(LogisticsDemandId source, PlayerId company, EntityId destination);
    }
    /// <summary>Detached explicit physical-port intent. Endpoint books alone own stock and promises.</summary>
    public readonly struct LogisticsSupplyRequest
    {
        public Code Port { get; }
        public ResourceEndpointId Endpoint { get; }
        public Bill Quantities { get; }
        private readonly IReadOnlyList<Content.IResourceLotSelectionDefinition> _selections;
        public IReadOnlyList<Content.IResourceLotSelectionDefinition> Selections => _selections ?? Array.Empty<Content.IResourceLotSelectionDefinition>();
        public Content.IResourceLotSelectionDefinition SelectionFor(ResourceId resource)
        { foreach (var selection in Selections) if (selection.Resource == resource) return selection; return null; }
        public LogisticsSupplyRequest(Code port, ResourceEndpointId endpoint, Bill quantities, IEnumerable<Content.IResourceLotSelectionDefinition> selections = null)
        { Port = port; Endpoint = endpoint; Quantities = quantities; _selections = selections == null ? null : new List<Content.IResourceLotSelectionDefinition>(selections).AsReadOnly(); }
        public static LogisticsSupplyRequest Entity(EntityId entity, Bill quantities)
            => new LogisticsSupplyRequest(Code.None, ResourceEndpointId.ForEntity(entity), quantities);
    }
    /// <summary>The existing matcher holds all requests; one complete explicit envelope replaces a source's intent.</summary>
    public interface ILogisticsDemandRequests
    {
        Code CheckRequests(LogisticsDemandId source, PlayerId company, EntityId destination, IReadOnlyList<LogisticsSupplyRequest> requests, int priority);
        Code ReplaceRequests(LogisticsDemandId source, PlayerId company, EntityId destination, IReadOnlyList<LogisticsSupplyRequest> requests, int priority);
        Code SetRequest(LogisticsDemandId source, PlayerId company, EntityId destination, ResourceId resource, Fix level, int priority);
        void WithdrawRequests(LogisticsDemandId source);
    }
    /// <summary>Pure current workflow ownership/classification, through the same declared services.</summary>
    public interface ILogisticsDemandSources
    {
        LogisticsDemandSourceCheck InspectSource(LogisticsDemandId source, PlayerId company, EntityId destination);
        Code CheckSourceDisclosure(PlayerId viewer, LogisticsDemandId source, PlayerId company, EntityId destination);
        Code CheckDemandDisclosure(PlayerId viewer, EntityId destination, ResourceId resource, ResourceEndpointId? endpoint = null);
    }
    public static class LogisticsDemandCodes
    {
        public static readonly Code UnknownOwner = new Code("logistics.unknown-demand-owner");
        public static readonly Code Expired = new Code("logistics.demand-expired");
        public static readonly Code InvalidSource = new Code("logistics.invalid-demand-source");
    }
}
