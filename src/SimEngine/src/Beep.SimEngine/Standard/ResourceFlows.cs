using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public interface IResourceFlowJournalDefinition : IContentDefinition
    {
        int RetainedRecordsPerCompany { get; }
    }

    public sealed class ResourceFlowJournalDef : Def, IResourceFlowJournalDefinition
    {
        private ResourceFlowJournalDeclaration _declaration;
        public int RetainedRecordsPerCompany => _declaration.RetainedRecordsPerCompany;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceFlowJournalDeclaration.Read(reader);
    }

    /// <summary>Sole explicit journal authoring rule; no stock, retention state or default.</summary>
    public sealed class ResourceFlowJournalDeclaration
    {
        public int RetainedRecordsPerCompany { get; }
        private ResourceFlowJournalDeclaration(int capacity) { RetainedRecordsPerCompany = capacity; }
        public static ResourceFlowJournalDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit resource-flow journal declaration required");
            int capacity = reader.Int("retained_records_per_company");
            if (capacity <= 0) throw reader.Error("retained_records_per_company must be positive");
            return new ResourceFlowJournalDeclaration(capacity);
        }
    }

    public sealed class ResourceFlowsModule : GameModule
    {
        public const string ModuleId = "sim.resource-flows";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[]
        { new ModuleRequirement(InventoryModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        { types.Section<ResourceFlowJournalDef>("resource_flow_journals"); types.Section<ResourceFlowQueryDef>("resource_flow_queries"); }
        public override void Compose(WorldComposer composer)
        {
            IReadOnlyList<IResourceFlowJournalDefinition> definitions = composer.Content.All<IResourceFlowJournalDefinition>();
            if (definitions.Count != 1) throw new ContentException("selected content must declare exactly one resource-flow journal policy");
            IResourceFlowJournalDefinition definition = definitions[0];
            int capacity = EntityObservation.ReadProvider(composer.World, definition, null, () => {
                if (!(definition is Def actual) || !actual.IsFrozen || !composer.Content.Owns(definition) ||
                    definition.Id != actual.Id || definition.Module != actual.Module ||
                    !ReferenceEquals(composer.Content.Find<IResourceFlowJournalDefinition>(actual.Id), definition))
                    throw new ContentException("resource-flow journal requires its actual frozen catalog policy");
                int retained = definition.RetainedRecordsPerCompany;
                if (retained <= 0) throw new ContentException(actual.Id + ": retained_records_per_company must be positive");
                return retained;
            });
            var journal = new ResourceFlowJournal(composer.World, capacity);
            composer.Services.Bind<IResourceFlowJournal>(journal);
            composer.Events.Require<ResourceFlowFact>(ResourceFlowFact.EventId, 7);
            composer.Events.Subscribe<ResourceFlowFact>(ModuleId + ".record", journal.Record);
            composer.AddViewFeature(ModuleId, 10, (world, viewer) => new ResourceFlowView(world, viewer));
            foreach (var query in composer.Content.All<IResourceFlowQueryDefinition>())
                EntityObservation.ReadProvider(composer.World, query, null, () => {
                    ResourceFlowQueryDeclaration.Validate(composer.Content, query);
                    _ = ResourceFlowQueryDeclaration.Create(query, PlayerId.None, composer.World.Time, 0);
                    return true;
                }, observeTargetState: true);
        }
    }

    /// <summary>Sole committed history owner: retained facts and cumulative summaries, never balances.</summary>
    internal sealed class ResourceFlowJournal : IResourceFlowJournal, IPersistentService
    {
        private readonly World _world;
        private readonly int _capacity;
        private readonly struct AggregateKey : IComparable<AggregateKey>
        {
            internal readonly ResourceId Resource; internal readonly ResourceFlowKind Kind; internal readonly Code Cause;
            internal readonly ResourceFlowAttribution Attribution;
            internal AggregateKey(ResourceId resource, ResourceFlowKind kind, Code cause, ResourceFlowAttribution attribution)
            { Resource = resource; Kind = kind; Cause = cause; Attribution = attribution; }
            public int CompareTo(AggregateKey other)
            { int c = Resource.CompareTo(other.Resource); if (c != 0) return c; c = Kind.CompareTo(other.Kind);
                if (c != 0) return c; c = string.CompareOrdinal(Cause.Id, other.Cause.Id);
                return c != 0 ? c : Attribution.CompareTo(other.Attribution); }
        }
        private sealed class Partition
        {
            internal readonly FeedRing<ResourceFlowFact> Feed;
            internal readonly SortedDictionary<AggregateKey, ResourceFlowAggregate> Aggregates = new SortedDictionary<AggregateKey, ResourceFlowAggregate>();
            internal long Evicted = -1;
            internal Partition(int capacity) { Feed = new FeedRing<ResourceFlowFact>(capacity); }
        }
        private SortedDictionary<int, Partition> _companies = new SortedDictionary<int, Partition>();
        public string Id => ResourceFlowsModule.ModuleId;
        public int StateSchemaVersion => 8;
        internal ResourceFlowJournal(World world, int capacity)
        { _world = world; _capacity = capacity; }

        internal void Record(ResourceFlowFact fact)
        {
            Validate(fact);
            int company = fact.Company.Value;
            if (!_companies.TryGetValue(company, out Partition partition))
            { partition = new Partition(_capacity); _companies.Add(company, partition); }
            foreach (var value in partition.Aggregates.Values)
                if (value.LastTick > fact.Tick) throw new CorruptStateException("resource-flow facts cannot arrive out of game-time order");
            var key = new AggregateKey(fact.Resource, fact.Kind, fact.Cause, fact.Attribution);
            partition.Aggregates.TryGetValue(key, out var previous);
            var aggregate = new ResourceFlowAggregate(fact.Resource, fact.Kind, fact.Cause, fact.Attribution, fact.Unit,
                previous.AmountRaw + fact.Amount.Raw, checked(previous.Facts + 1), previous.Facts == 0 ? fact.Tick : previous.FirstTick, fact.Tick);
            long evicted = partition.Evicted;
            if (partition.Feed.Latest >= _capacity && partition.Feed.TryReadOldest(out var oldest)) evicted = oldest.Item.Tick;
            partition.Feed.Append(fact); partition.Evicted = evicted; partition.Aggregates[key] = aggregate;
        }
        private bool ValidCompany(PlayerId company) => company == PlayerId.None || _world.Players.IsSeat(company);
        public ResourceFlowPage Read(PlayerId company, long cursor)
        {
            _world.RequireHealthyUpdate();
            if (!ValidCompany(company)) return new ResourceFlowPage(ResourceFlowReadStatus.NoSuchCompany);
            if (!_companies.TryGetValue(company.Value, out Partition partition))
                return new ResourceFlowPage(cursor == 0 ? ResourceFlowReadStatus.Ready : ResourceFlowReadStatus.CursorAhead, 1, 0);
            FeedRing<ResourceFlowFact> feed = partition.Feed;
            if (cursor < 0 || cursor > feed.Latest)
                return new ResourceFlowPage(ResourceFlowReadStatus.CursorAhead, feed.Oldest, feed.Latest);
            var entries = new List<FeedEntry<ResourceFlowFact>>();
            ResourceFlowReadStatus status = cursor < feed.Oldest - 1 ? ResourceFlowReadStatus.Expired : ResourceFlowReadStatus.Ready;
            feed.CopyEntriesSince(ref cursor, entries);
            return new ResourceFlowPage(status, feed.Oldest, feed.Latest, entries);
        }
        public ResourceFlowSummary ReadSummary(PlayerId company)
        {
            _world.RequireHealthyUpdate();
            if (!ValidCompany(company)) return new ResourceFlowSummary(ResourceFlowReadStatus.NoSuchCompany);
            if (!_companies.TryGetValue(company.Value, out Partition partition)) return new ResourceFlowSummary(ResourceFlowReadStatus.Ready);
            return new ResourceFlowSummary(ResourceFlowReadStatus.Ready, partition.Feed.Latest, partition.Evicted, partition.Aggregates.Values);
        }
        public ResourceFlowPage ReadWindow(PlayerId company, long fromTick, long throughTick)
        {
            _world.RequireHealthyUpdate();
            if (!ValidCompany(company)) return new ResourceFlowPage(ResourceFlowReadStatus.NoSuchCompany);
            if (fromTick < 0 || throughTick < fromTick || throughTick > _world.Tick) return new ResourceFlowPage(ResourceFlowReadStatus.CursorAhead);
            if (!_companies.TryGetValue(company.Value, out Partition partition)) return new ResourceFlowPage(ResourceFlowReadStatus.Ready, 1, 0);
            if (fromTick < throughTick && partition.Evicted >= fromTick)
                return new ResourceFlowPage(ResourceFlowReadStatus.Expired, partition.Feed.Oldest, partition.Feed.Latest);
            var entries = new List<FeedEntry<ResourceFlowFact>>(); long cursor = 0; partition.Feed.CopyEntriesSince(ref cursor, entries);
            entries.RemoveAll(entry => entry.Item.Tick < fromTick || entry.Item.Tick >= throughTick);
            return new ResourceFlowPage(ResourceFlowReadStatus.Ready, partition.Feed.Oldest, partition.Feed.Latest, entries);
        }
        public ResourceFlowObservation ReadThroughput(ResourceFlowQuery query)
        {
            _world.RequireHealthyUpdate();
            if (query==null) throw new ArgumentNullException(nameof(query));
            if (!ValidCompany(query.Company)) return ResourceFlowObservation.Refused(query,ResourceFlowReadStatus.NoSuchCompany);
            var resource=_world.Content.Resource(query.Resource);
            if (resource==null) return ResourceFlowObservation.Refused(query,ResourceFlowReadStatus.UnknownResource);
            var selection = query.Selection.IsNone ? null : _world.Content.Find<IResourceLotSelectionDefinition>(query.Selection.Id);
            if (!query.Selection.IsNone && (selection == null || selection.Resource != query.Resource))
                return ResourceFlowObservation.Refused(query, ResourceFlowReadStatus.UnknownSelection);
            var page=ReadWindow(query.Company,query.FromTick,query.ThroughTick);
            if (page.Status!=ResourceFlowReadStatus.Ready) return ResourceFlowObservation.Refused(query,page.Status);
            BigInteger raw=BigInteger.Zero;long facts=0;
            foreach (var entry in page.Entries) if (query.Matches(entry.Item))
            {
                var fact = entry.Item; Fix amount = fact.Amount;
                if (selection != null)
                {
                    if (fact.Lots.Count == 0) return ResourceFlowObservation.Refused(query, ResourceFlowReadStatus.Unattributed);
                    amount = ResourceLotSelection.EligibleAmount(_world.Content, fact.Lots, fact.Resource, selection);
                }
                if (amount.IsPositive) { raw += amount.Raw; facts = checked(facts + 1); }
            }
            return ResourceFlowObservation.Measured(query,resource.QuantityUnit.Unit,raw,facts,_world.Time);
        }
        private void Validate(ResourceFlowFact fact)
        {
            IResourceContentDefinition resource = _world.Content.Resource(fact.Resource);
            if (fact.Tick < 0 || fact.Tick > _world.Tick || !ValidCompany(fact.Company) || !fact.Owner.IsValid ||
                resource == null || fact.Unit != resource.QuantityUnit.Unit || !fact.Amount.IsPositive ||
                UnitConversion.ValidateAmount(resource.QuantityUnit, fact.Amount) != UnitConversionStatus.Converted ||
                fact.Kind > ResourceFlowKind.Sold || !fact.Attribution.IsValidFor(fact.Kind))
                throw new CorruptStateException("invalid committed resource-flow fact");
            var selection = fact.Selection.IsNone ? null : _world.Content.Find<IResourceLotSelectionDefinition>(fact.Selection.Id);
            if (!fact.Selection.IsNone && (selection == null || selection.Resource != fact.Resource || fact.Lots.Count == 0 ||
                !fact.IsTransfer && !fact.Source.IsValid)) throw new CorruptStateException("invalid flow selection attribution");
            Fix total = Fix.Zero; string previousLot = null; bool first = true;
            foreach (var lot in fact.Lots)
            {
                if (lot.Resource != fact.Resource || !lot.Amount.IsPositive || !lot.ValidMetadata(_world.Content) ||
                    UnitConversion.ValidateAmount(resource.QuantityUnit, lot.Amount) != UnitConversionStatus.Converted ||
                    !first && string.CompareOrdinal(previousLot, lot.Lot.Id) >= 0 ||
                    !ResourceLotSelection.Matches(_world.Content, lot, selection)) throw new CorruptStateException("invalid flow lot attribution");
                try { total += lot.Amount; } catch (OverflowException) { throw new CorruptStateException("flow lot total overflow"); }
                first = false; previousLot = lot.Lot.Id;
            }
            if (fact.Lots.Count != 0 && total != fact.Amount) throw new CorruptStateException("flow lots differ from committed quantity");
            if (!fact.IsTransfer) { ValidateLifecycle(fact, resource); return; }
            bool creation = fact.Kind == ResourceFlowKind.AuthoredCreation;
            if (!fact.Source.IsValid || !fact.Destination.IsValid || fact.Source.Endpoint == fact.Destination.Endpoint ||
                fact.Endpoint.IsValid || !fact.Cause.IsNone ||
                fact.SourceVersionBefore < 0 || fact.SourceVersionAfter <= fact.SourceVersionBefore ||
                fact.DestinationVersionBefore < 0 || fact.DestinationVersionAfter <= fact.DestinationVersionBefore ||
                fact.DestinationBefore < Fix.Zero || fact.DestinationAfter < Fix.Zero ||
                fact.DestinationAfter.Raw - fact.DestinationBefore.Raw != fact.Amount.Raw ||
                creation != !fact.SourceBefore.HasValue || creation != !fact.SourceAfter.HasValue ||
                !creation && (fact.SourceBefore.Value < Fix.Zero || fact.SourceAfter.Value < Fix.Zero ||
                    fact.SourceBefore.Value.Raw - fact.SourceAfter.Value.Raw != fact.Amount.Raw))
                throw new CorruptStateException("invalid committed resource-flow fact");
            if (UnitConversion.ValidateAmount(resource.QuantityUnit, fact.DestinationBefore) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(resource.QuantityUnit, fact.DestinationAfter) != UnitConversionStatus.Converted ||
                !creation && (UnitConversion.ValidateAmount(resource.QuantityUnit, fact.SourceBefore.Value) != UnitConversionStatus.Converted ||
                    UnitConversion.ValidateAmount(resource.QuantityUnit, fact.SourceAfter.Value) != UnitConversionStatus.Converted))
                throw new CorruptStateException("resource-flow stocks violate native units");
        }
        private static void ValidateLifecycle(ResourceFlowFact fact, IResourceContentDefinition resource)
        {
            bool adds = fact.Kind == ResourceFlowKind.Initialized || fact.Kind == ResourceFlowKind.Produced;
            if (!fact.Endpoint.IsValid || fact.Cause.IsNone || fact.Destination.IsValid ||
                fact.Source.IsValid && (!ResourceConversionInput.IsDispositionKind(fact.Kind) || fact.Source.Endpoint != fact.Endpoint) ||
                !fact.Source.IsValid && fact.Endpoint.Scope != ResourceEndpointId.EntityScope && fact.Kind != ResourceFlowKind.Produced ||
                adds && (fact.SourceBefore.HasValue || fact.SourceAfter.HasValue || fact.SourceVersionBefore != 0 || fact.SourceVersionAfter != 0) ||
                !adds && (!fact.SourceBefore.HasValue || !fact.SourceAfter.HasValue || fact.DestinationBefore != Fix.Zero ||
                    fact.DestinationAfter != Fix.Zero || fact.DestinationVersionBefore != 0 || fact.DestinationVersionAfter != 0))
                throw new CorruptStateException("invalid lifecycle resource-flow endpoint");
            Fix before = adds ? fact.DestinationBefore : fact.SourceBefore.Value, after = adds ? fact.DestinationAfter : fact.SourceAfter.Value;
            int versionBefore = adds ? fact.DestinationVersionBefore : fact.SourceVersionBefore,
                versionAfter = adds ? fact.DestinationVersionAfter : fact.SourceVersionAfter;
            if (versionBefore < 0 || versionAfter <= versionBefore || before < Fix.Zero || after < Fix.Zero ||
                (adds ? after.Raw - before.Raw : before.Raw - after.Raw) != fact.Amount.Raw ||
                UnitConversion.ValidateAmount(resource.QuantityUnit, before) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(resource.QuantityUnit, after) != UnitConversionStatus.Converted)
                throw new CorruptStateException("invalid lifecycle resource-flow delta/version/unit");
        }
        public void Sync(IStateSync s)
        {
            int count = s.Count(_companies.Count);
            if (count < 0 || count > (long)_world.Players.Count + 1) throw new CorruptStateException("invalid resource-flow company count");
            if (s.Reading)
            {
                var companies = new SortedDictionary<int, Partition>(); int previous = int.MinValue;
                for (int i = 0; i < count; i++)
                {
                    int company = 0; s.Int(ref company);
                    if (company <= previous || !ValidCompany(new PlayerId(company))) throw new CorruptStateException("invalid resource-flow company order");
                    var partition = new Partition(_capacity);
                    SyncPartition(s, company, partition);
                    companies.Add(company, partition); previous = company;
                }
                _companies = companies;
            }
            else foreach (KeyValuePair<int, Partition> entry in _companies)
            { int company = entry.Key; s.Int(ref company); SyncPartition(s, company, entry.Value); }
        }
        private void SyncPartition(IStateSync sync, int company, Partition partition)
        {
            SyncFeed(sync, company, partition.Feed); sync.Long(ref partition.Evicted);
            int count = sync.Count(partition.Aggregates.Count); AggregateKey? previous = null;
            IEnumerable<ResourceFlowAggregate> rows = sync.Reading ? Array.Empty<ResourceFlowAggregate>() : partition.Aggregates.Values;
            foreach (var value in rows)
            { var row = value; SyncAggregate(sync, ref row); }
            if (sync.Reading) for (int i = 0; i < count; i++)
            {
                ResourceFlowAggregate row = default; SyncAggregate(sync, ref row);
                var key = new AggregateKey(row.Resource, row.Kind, row.Cause, row.Attribution);
                if (previous.HasValue && previous.Value.CompareTo(key) >= 0) throw new CorruptStateException("duplicate/unordered cumulative resource-flow key");
                partition.Aggregates.Add(key, row); previous = key;
            }
            ValidatePartition(partition);
        }
        private void SyncAggregate(IStateSync sync, ref ResourceFlowAggregate row)
        {
            ResourceId resource = row.Resource; int kind = (int)row.Kind; string cause = row.Cause.Id, unit = row.Unit.Id;
            var attribution = row.Attribution;
            string amount = row.AmountRaw.ToString(CultureInfo.InvariantCulture); long facts = row.Facts, first = row.FirstTick, last = row.LastTick;
            sync.Resource(ref resource); sync.Int(ref kind); sync.String(ref cause); sync.String(ref unit); sync.String(ref amount);
            ResourceFlowAttribution.Sync(sync, ref attribution);
            sync.Long(ref facts); sync.Long(ref first); sync.Long(ref last);
            if (sync.Reading)
            {
                if (!BigInteger.TryParse(amount, NumberStyles.None, CultureInfo.InvariantCulture, out BigInteger raw) || raw.ToString(CultureInfo.InvariantCulture) != amount)
                    throw new CorruptStateException("noncanonical cumulative quantity");
                try { row = new ResourceFlowAggregate(resource, (ResourceFlowKind)kind, cause == null ? Code.None : new Code(cause), attribution, new UnitId(unit), raw, facts, first, last); }
                catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
            }
            if (kind < 0 || kind > (int)ResourceFlowKind.Sold) throw new CorruptStateException("invalid cumulative resource-flow kind");
        }
        private void ValidatePartition(Partition partition)
        {
            var feed = partition.Feed;
            if (feed.Latest == 0 || partition.Evicted < -1 || partition.Evicted > _world.Tick ||
                (feed.Oldest == 1) != (partition.Evicted == -1)) throw new CorruptStateException("invalid cumulative resource history/eviction boundary");
            BigInteger facts = 0;
            foreach (var row in partition.Aggregates.Values)
            {
                IResourceContentDefinition resource = _world.Content.Resource(row.Resource);
                if (resource == null || resource.QuantityUnit.Unit != row.Unit || row.Facts <= 0 || row.AmountRaw < row.Facts ||
                    row.AmountRaw > (BigInteger)row.Facts * long.MaxValue || row.FirstTick < 0 || row.LastTick < row.FirstTick || row.LastTick > _world.Tick ||
                    resource.QuantityUnit.Discrete && row.AmountRaw % Fix.One.Raw != 0 || (row.Kind <= ResourceFlowKind.AuthoredCreation) != row.Cause.IsNone ||
                    !row.Attribution.IsValidFor(row.Kind))
                    throw new CorruptStateException("invalid cumulative resource units/quantity/count/cause/time");
                if (resource.QuantityUnit.Discrete && row.AmountRaw < (BigInteger)row.Facts * Fix.One.Raw)
                    throw new CorruptStateException("cumulative discrete quantities cannot contain less than one native unit per positive fact");
                facts += row.Facts;
            }
            if (facts != feed.Latest) throw new CorruptStateException("cumulative resource counts differ from sole fact sequence");
            var retained = new SortedDictionary<AggregateKey, ResourceFlowAggregate>(); var entries = new List<FeedEntry<ResourceFlowFact>>(); long cursor = 0;
            feed.CopyEntriesSince(ref cursor, entries);
            if (entries[0].Item.Tick < partition.Evicted) throw new CorruptStateException("eviction boundary exceeds retained facts");
            foreach (var entry in entries)
            {
                var fact = entry.Item; var key = new AggregateKey(fact.Resource, fact.Kind, fact.Cause, fact.Attribution); retained.TryGetValue(key, out var prior);
                retained[key] = new ResourceFlowAggregate(fact.Resource, fact.Kind, fact.Cause, fact.Attribution, fact.Unit, prior.AmountRaw + fact.Amount.Raw,
                    prior.Facts + 1, prior.Facts == 0 ? fact.Tick : prior.FirstTick, fact.Tick);
            }
            foreach (var pair in retained)
            {
                var amount = pair.Value;
                if (!partition.Aggregates.TryGetValue(pair.Key, out var all) || all.Facts < amount.Facts || all.AmountRaw < amount.AmountRaw ||
                    all.FirstTick > amount.FirstTick || all.LastTick != amount.LastTick ||
                    feed.Oldest == 1 && (all.Facts != amount.Facts || all.AmountRaw != amount.AmountRaw || all.FirstTick != amount.FirstTick))
                    throw new CorruptStateException("cumulative resource history differs from retained verified facts");
                long missingFacts = all.Facts - amount.Facts; BigInteger missingAmount = all.AmountRaw - amount.AmountRaw;
                long quantum = _world.Content.Resource(all.Resource).QuantityUnit.Discrete ? Fix.One.Raw : 1;
                if (missingFacts == 0 ? missingAmount != 0 || all.FirstTick != amount.FirstTick
                    : missingAmount < (BigInteger)missingFacts * quantum || missingAmount > (BigInteger)missingFacts * long.MaxValue || all.FirstTick > partition.Evicted)
                    throw new CorruptStateException("cumulative evicted contribution differs from actual count/quantity boundaries");
            }
            foreach (var pair in partition.Aggregates)
                if (!retained.ContainsKey(pair.Key) && pair.Value.LastTick > partition.Evicted)
                    throw new CorruptStateException("missing cumulative key has an unevicted latest fact");
        }
        private void SyncFeed(IStateSync s, int company, FeedRing<ResourceFlowFact> feed)
        {
            long previousTick = -1;
            feed.Sync(s, (IStateSync sync, ref ResourceFlowFact fact) =>
            {
                SyncFact(sync, ref fact); Validate(fact);
                if (fact.Company.Value != company || fact.Tick < previousTick)
                    throw new CorruptStateException("resource-flow partition/tick order differs");
                previousTick = fact.Tick;
            });
        }
        private static void SyncFact(IStateSync s, ref ResourceFlowFact fact)
        {
            long tick = fact.Tick; s.Long(ref tick);
            PlayerId company = fact.Company; s.Player(ref company);
            string scope = fact.Owner.Scope; long identity = fact.Owner.Identity; s.String(ref scope); s.Long(ref identity);
            ReservationToken source = fact.Source, destination = fact.Destination;
            ReservationToken.Sync(s, ref source); ReservationToken.Sync(s, ref destination);
            ResourceId resource = fact.Resource; Fix amount = fact.Amount; s.Resource(ref resource); s.Fix(ref amount);
            int kindValue = (int)fact.Kind; s.Int(ref kindValue);
            if (kindValue < 0 || kindValue > (int)ResourceFlowKind.Sold) throw new CorruptStateException("invalid resource-flow kind");
            ResourceFlowKind kind = (ResourceFlowKind)kindValue;
            ResourceEndpointId endpoint = fact.Endpoint; string cause = fact.Cause.Id;
            ResourceEndpointId.Sync(s, ref endpoint); s.String(ref cause);
            string unit = fact.Unit.Id; s.String(ref unit);
            bool hasStock = fact.SourceBefore.HasValue; s.Bool(ref hasStock);
            Fix beforeOut = fact.SourceBefore ?? Fix.Zero, afterOut = fact.SourceAfter ?? Fix.Zero;
            if (hasStock) { s.Fix(ref beforeOut); s.Fix(ref afterOut); }
            Fix beforeIn = fact.DestinationBefore, afterIn = fact.DestinationAfter; s.Fix(ref beforeIn); s.Fix(ref afterIn);
            int sourceBefore = fact.SourceVersionBefore, sourceAfter = fact.SourceVersionAfter,
                destinationBefore = fact.DestinationVersionBefore, destinationAfter = fact.DestinationVersionAfter;
            s.Int(ref sourceBefore); s.Int(ref sourceAfter); s.Int(ref destinationBefore); s.Int(ref destinationAfter);
            string selection = fact.Selection.Id; s.String(ref selection);
            int count = s.Count(fact.Lots.Count); var lots = s.Reading ? new List<ResourceLotAmount>() : null;
            for (int i = 0; i < count; i++)
            { var lot = ResourceLotAmount.Sync(s, s.Reading ? default : fact.Lots[i]); if (s.Reading) lots.Add(lot); }
            if (s.Reading)
            {
                UnitId unitId;
                try { unitId = new UnitId(unit); }
                catch (ArgumentException) { throw new CorruptStateException("invalid resource-flow unit identity"); }
                var owner = new ReservationOwner(scope, identity);
                if (kind <= ResourceFlowKind.AuthoredCreation)
                {
                    if (endpoint.IsValid || cause != null) throw new CorruptStateException("transfer has lifecycle metadata");
                    fact = new ResourceFlowFact(tick, new ResourceTransferIntent(owner, company, source, destination, resource, amount,
                        kind == ResourceFlowKind.StockTransfer ? ResourceTransferKind.Stock : ResourceTransferKind.Supply), unitId, kind,
                        hasStock ? beforeOut : (Fix?)null, hasStock ? afterOut : (Fix?)null, beforeIn, afterIn,
                        sourceBefore, sourceAfter, destinationBefore, destinationAfter);
                }
                else
                {
                    bool adds = kind == ResourceFlowKind.Initialized || kind == ResourceFlowKind.Produced;
                    bool removes = ResourceConversionInput.IsDispositionKind(kind);
                    if (destination.IsValid || source.IsValid && (!removes || source.Endpoint != endpoint) ||
                        adds && (hasStock || sourceBefore != 0 || sourceAfter != 0) ||
                        !adds && (!hasStock || beforeIn != Fix.Zero || afterIn != Fix.Zero || destinationBefore != 0 || destinationAfter != 0))
                        throw new CorruptStateException("lifecycle has transfer metadata");
                    Code causeId;
                    try { causeId = new Code(cause); }
                    catch (ArgumentException) { throw new CorruptStateException("invalid resource-flow cause identity"); }
                    if (source.IsValid)
                        fact = new ResourceFlowFact(tick, company,
                            new ResourceReservation(source, owner, resource, amount, ReservationDirection.Outbound), causeId,
                            unitId, beforeOut, afterOut, sourceBefore, sourceAfter, kind);
                    else
                    {
                        if (kind == ResourceFlowKind.Produced)
                            fact = new ResourceFlowFact(tick, owner, company, endpoint,
                                new ResourceAmount(resource, amount), causeId, unitId, beforeIn, afterIn, destinationBefore, destinationAfter);
                        else
                        {
                            if (endpoint.Scope != ResourceEndpointId.EntityScope) throw new CorruptStateException("unowned lifecycle endpoint is not an entity");
                            fact = new ResourceFlowFact(tick, new ResourceInventoryIntent(owner, company, endpoint.ToEntity(), Bill.Empty, kind, causeId),
                                new ResourceAmount(resource, amount), unitId, adds ? beforeIn : beforeOut, adds ? afterIn : afterOut,
                                adds ? destinationBefore : sourceBefore, adds ? destinationAfter : sourceAfter);
                        }
                    }
                }
                fact = fact.WithLots(lots, selection == null ? Code.None : new Code(selection));
            }
        }
    }
}
