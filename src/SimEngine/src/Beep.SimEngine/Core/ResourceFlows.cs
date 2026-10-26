using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine
{
    public enum ResourceFlowReadStatus : byte { Ready, Expired, CursorAhead, NotOwned, NoSuchCompany, UnknownResource, UnknownSelection, Unattributed, Undisclosed }

    /// <summary>Read-only journal contract. Only the declared event subscriber appends history.</summary>
    public interface IResourceFlowJournal
    {
        ResourceFlowPage Read(PlayerId company, long cursor);
        ResourceFlowSummary ReadSummary(PlayerId company);
        ResourceFlowPage ReadWindow(PlayerId company, long fromTick, long throughTick);
        ResourceFlowObservation ReadThroughput(ResourceFlowQuery query);
    }

    /// <summary>Immutable observer selection over exact committed facts, not a gameplay stock policy.</summary>
    public sealed class ResourceFlowQuery
    {
        public PlayerId Company { get; }
        public ResourceId Resource { get; }
        public IReadOnlyList<ResourceFlowKind> Kinds { get; }
        public long FromTick { get; }
        public long ThroughTick { get; }
        public DurationUnit Per { get; }
        public ResourceEndpointId? Source { get; }
        public ResourceEndpointId? Destination { get; }
        public ResourceEndpointId? Endpoint { get; }
        public Code? Cause { get; }
        public string OwnerScope { get; }
        public Code Selection { get; }
        public ResourceFlowQuery(PlayerId company, ResourceId resource, IEnumerable<ResourceFlowKind> kinds,
            long fromTick, long throughTick, DurationUnit per, ResourceEndpointId? source = null,
            ResourceEndpointId? destination = null, ResourceEndpointId? endpoint = null, Code? cause = null, string ownerScope = null,
            Code selection = default)
        {
            if (!resource.IsValid || kinds == null || fromTick < 0 || throughTick < fromTick)
                throw new ArgumentException("explicit resource, kinds and nonnegative half-open flow window required");
            GameDuration.SecondsPerUnit(per);
            foreach (var id in new[] {source, destination, endpoint})
                if (id.HasValue && !id.Value.IsValid) throw new ArgumentException("flow filter requires actual endpoint identity");
            if (ownerScope != null) _ = new Code(ownerScope);
            var values = new List<ResourceFlowKind>(kinds); values.Sort();
            if (values.Count == 0) throw new ArgumentException("explicit flow kinds cannot be empty");
            for (int i=0;i<values.Count;i++)
                if (!Enum.IsDefined(typeof(ResourceFlowKind), values[i]) || i>0 && values[i]==values[i-1])
                    throw new ArgumentException("flow kinds must be defined and distinct");
            Company=company;Resource=resource;Kinds=values.AsReadOnly();FromTick=fromTick;ThroughTick=throughTick;Per=per;
            Source=source;Destination=destination;Endpoint=endpoint;Cause=cause;OwnerScope=ownerScope;
            Selection = selection;
        }
        internal bool Matches(ResourceFlowFact fact)
        {
            var attribution = fact.Attribution;
            if (fact.Resource != Resource || Source.HasValue && attribution.Source != Source.Value ||
                Destination.HasValue && attribution.Destination != Destination.Value ||
                Endpoint.HasValue && attribution.Endpoint != Endpoint.Value || Cause.HasValue && fact.Cause != Cause.Value ||
                OwnerScope != null && attribution.OwnerScope != OwnerScope) return false;
            foreach (var kind in Kinds) if (kind==fact.Kind) return true;
            return false;
        }
    }
    public sealed class ResourceFlowObservation
    {
        public static readonly Code NoElapsedTime = new Code("resource-flow.no-elapsed-time");
        public static readonly Code Unavailable = new Code("resource-flow.unavailable");
        public ResourceFlowQuery Query { get; }
        public ResourceFlowReadStatus Status { get; }
        public bool Available => Status == ResourceFlowReadStatus.Ready;
        public UnitId Unit { get; }
        public BigInteger? AmountRaw { get; }
        public long Facts { get; }
        public MeasuredRate? Rate { get; }
        public Code RateBlocker { get; }
        private ResourceFlowObservation(ResourceFlowQuery query, ResourceFlowReadStatus status, UnitId unit,
            BigInteger? raw, long facts, MeasuredRate? rate, Code blocker)
        { Query=query;Status=status;Unit=unit;AmountRaw=raw;Facts=facts;Rate=rate;RateBlocker=blocker; }
        public static ResourceFlowObservation Refused(ResourceFlowQuery query, ResourceFlowReadStatus status)
        {
            if (query==null || status==ResourceFlowReadStatus.Ready || !Enum.IsDefined(typeof(ResourceFlowReadStatus),status))
                throw new ArgumentException("unavailable flow observation requires actual query and refusal");
            return new ResourceFlowObservation(query,status,default,null,0,null,Unavailable);
        }
        public static ResourceFlowObservation Measured(ResourceFlowQuery query, UnitId unit, BigInteger raw, long facts, ISimulationTime time)
        {
            if (query==null || !unit.IsValid || raw<0 || facts<0 || raw.IsZero!=(facts==0) ||
                time==null || time.QuantumNumerator<=0 || time.QuantumDenominator<=0)
                throw new ArgumentException("measured flow requires native unit, exact committed quantity/count and actual game time");
            long elapsed=query.ThroughTick-query.FromTick;
            MeasuredRate? rate=elapsed==0?(MeasuredRate?)null:MeasuredRate.Calculate(raw,elapsed,time,query.Per);
            return new ResourceFlowObservation(query,ResourceFlowReadStatus.Ready,unit,raw,facts,rate,
                elapsed==0?NoElapsedTime:Code.None);
        }
    }

    /// <summary>Cumulative committed history by native resource/kind/cause/attribution, never current stock.</summary>
    public readonly struct ResourceFlowAggregate
    {
        public ResourceId Resource { get; }
        public ResourceFlowKind Kind { get; }
        public Code Cause { get; }
        public ResourceFlowAttribution Attribution { get; }
        public UnitId Unit { get; }
        public BigInteger AmountRaw { get; }
        public long Facts { get; }
        public long FirstTick { get; }
        public long LastTick { get; }
        internal ResourceFlowAggregate(ResourceId resource, ResourceFlowKind kind, Code cause, ResourceFlowAttribution attribution, UnitId unit,
            BigInteger amount, long facts, long first, long last)
        { Resource = resource; Kind = kind; Cause = cause; Attribution = attribution; Unit = unit; AmountRaw = amount; Facts = facts; FirstTick = first; LastTick = last; }
    }
    public sealed class ResourceFlowSummary
    {
        public ResourceFlowReadStatus Status { get; }
        public long Latest { get; }
        public long EvictedThroughTick { get; }
        public IReadOnlyList<ResourceFlowAggregate> Aggregates { get; }
        public ResourceFlowSummary(ResourceFlowReadStatus status, long latest = 0, long evicted = -1,
            IEnumerable<ResourceFlowAggregate> aggregates = null)
        { Status = status; Latest = latest; EvictedThroughTick = evicted;
            Aggregates = new List<ResourceFlowAggregate>(aggregates ?? Array.Empty<ResourceFlowAggregate>()).AsReadOnly(); }
    }

    public readonly struct ResourceFlowTotals
    {
        public int TransferLegs { get; }
        /// <summary>Q32 native-unit raw amounts; arbitrary precision prevents historical sum overflow.</summary>
        public BigInteger MovedRaw { get; }
        public BigInteger StockPurchasedRaw { get; }
        public BigInteger CreatedRaw { get; }
        public BigInteger InitializedRaw { get; }
        public BigInteger ConsumedRaw { get; }
        public BigInteger DestroyedRaw { get; }
        public BigInteger LostRaw { get; }
        public BigInteger SoldRaw { get; }
        public ResourceFlowTotals(int legs, BigInteger moved, BigInteger purchased, BigInteger created,
            BigInteger initialized, BigInteger consumed, BigInteger destroyed, BigInteger lost, BigInteger sold)
        { TransferLegs = legs; MovedRaw = moved; StockPurchasedRaw = purchased; CreatedRaw = created;
            InitializedRaw = initialized; ConsumedRaw = consumed; DestroyedRaw = destroyed; LostRaw = lost; SoldRaw = sold; }
    }

    /// <summary>Detached retained-window facts, not whole-session accounting or current inventory.</summary>
    public sealed class ResourceFlowPage
    {
        public ResourceFlowReadStatus Status { get; }
        public long Oldest { get; }
        public long Latest { get; }
        public IReadOnlyList<FeedEntry<ResourceFlowFact>> Entries { get; }
        public ResourceFlowPage(ResourceFlowReadStatus status, long oldest = 0, long latest = 0,
            IEnumerable<FeedEntry<ResourceFlowFact>> entries = null)
        {
            Status = status; Oldest = oldest; Latest = latest;
            Entries = Array.AsReadOnly(entries == null ? Array.Empty<FeedEntry<ResourceFlowFact>>()
                : new List<FeedEntry<ResourceFlowFact>>(entries).ToArray());
        }
        public ResourceFlowTotals Totals(ResourceId resource)
        {
            int legs = 0; BigInteger moved = 0, purchased = 0, created = 0, initialized = 0, consumed = 0, destroyed = 0, lost = 0, sold = 0;
            foreach (FeedEntry<ResourceFlowFact> entry in Entries)
            {
                ResourceFlowFact fact = entry.Item;
                if (fact.Resource != resource) continue;
                if (fact.IsTransfer) { legs++; moved += fact.Amount.Raw; }
                if (fact.Kind == ResourceFlowKind.StockPurchase) purchased += fact.Amount.Raw;
                if (fact.Kind == ResourceFlowKind.AuthoredCreation || fact.Kind == ResourceFlowKind.Produced) created += fact.Amount.Raw;
                if (fact.Kind == ResourceFlowKind.Initialized) initialized += fact.Amount.Raw;
                if (fact.Kind == ResourceFlowKind.Consumed) consumed += fact.Amount.Raw;
                if (fact.Kind == ResourceFlowKind.Destroyed) destroyed += fact.Amount.Raw;
                if (fact.Kind == ResourceFlowKind.Lost) lost += fact.Amount.Raw;
                if (fact.Kind == ResourceFlowKind.Sold) sold += fact.Amount.Raw;
            }
            return new ResourceFlowTotals(legs, moved, purchased, created, initialized, consumed, destroyed, lost, sold);
        }
    }
}
