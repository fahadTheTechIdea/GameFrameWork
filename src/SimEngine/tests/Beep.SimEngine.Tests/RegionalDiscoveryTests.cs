using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class RegionalDiscoveryTests
    {
        public static void Register() => TestRunner.Add("Regional discovery: physical learning, sample privacy and saved continuation", Run);
        private static string Facts => FrameworkProofContent.Read("layer-discovery-facts.json");
        private static string Visibility => FrameworkProofContent.Read("regional-point-visibility.json");
        internal static GameFactory Points(string visibility = null, string areas = null)
            => TechnologyTests.DiscoveryFactory(facts: Facts, work: FrameworkProofContent.Read("regional-discovery-work.json"),
                visibility: visibility ?? Visibility, additionalDocuments: new Dictionary<string, string> {
                    ["regional-layers.json"] = FrameworkProofContent.Read("point-layers.json"),
                    ["regional-areas.json"] = areas ?? FrameworkProofContent.Read("regional-point-areas.json"),
                    ["regional-knowledge-exchange.json"] = FrameworkProofContent.Read("regional-knowledge-exchange.json") });
        internal static GameFactory Tiles(string visibility = null)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("spatial-observer.json"));
            JsonValue scenario = root.Get("scenarios")[0];
            scenario.Get("modules").Add(JsonValue.Of(CompanyKnowledgeModule.ModuleId));
            scenario.Get("modules").Add(JsonValue.Of(KnowledgeVisibilityModule.ModuleId));
            scenario.Set("settings", Content.Json.Parse("{\"sim.knowledge-visibility\":{\"policy\":\"tile.discovery\"}}"));
            return SpatialLayerTests.Factory(root.ToString(), Facts, visibility ?? FrameworkProofContent.Read("regional-tile-visibility.json"));
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " fault=" + game.Fault + " orders=" +
                string.Join(";", Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static Order Start(Check c, SimGame game, string activity)
        {
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Plan = activity, Target = Find(game, "source").Id, Priority = 60 });
            c.That(result.Accepted, "ordinary player posts authored physical activity " + activity);
            return Board(game).Order((int)result.Arg0);
        }
        private static PointRegion Region(int start, long count = 1) => new PointRegion(Fix.FromInt(start), Fix.One, count);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "regional authorization has an exact engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "semantic migration has no old save reader");
            c.That(!typeof(IPersistentService).IsAssignableFrom(typeof(KnowledgeVisibility)), "same visibility owns no second learned codec");
            Physical(c); NativeProtocol(c); InvalidAuthoring(c);
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Points(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            var space = (PointSpace)game.World.Space;
            var mine = game.Seat(0).View.Feature<PointLayerView>(); var other = game.Seat(1).View.Feature<PointLayerView>();
            var observer = game.Observer.Feature<PointLayerView>(); var values = new List<Fix> { Fix.One };
            c.That(game.World.Content.Get<LayerRegionKnowledgeVisibilityDef>("visibility.regional-terrain").AuthoringPath.Contains("visibility.json"), "regional rule loaded through actual master document");
            c.That(game.World.Content.Get<PointIntervalRegionDef>("discover.first").AuthoringPath.Contains("regional-areas.json"), "public region definitions loaded once through actual master");
            // Explicit engine-side initial samples/ownership are protocol setup, NOT discovery grants.
            space.AssignOwner(game.World, "point.deposit", new PlayerId(0));
            foreach (int point in new[] { 1, 3, 5 })
            {
                c.Equal(Code.None, space.WriteLayer(game.World, PlayerId.None, "point.terrain", Fix.FromInt(point), Fix.FromInt(point + 10)), "actual initial terrain sample");
                c.Equal(Code.None, space.WriteLayer(game.World, PlayerId.None, "point.deposit", Fix.FromInt(point), Fix.FromInt(point + 20)), "actual initial stored deposit sample");
            }
            c.That(mine.Layers.Count == 0 && other.Layers.Count == 0 && observer.Layers.Count == 3, "unknown layers expose no company metadata");
            int before = mine.Version, foreign = other.Version;
            var retained = mine.Layers; Configure(c, game);
            Order first = Start(c, game, "field.discover");
            Reach(game, () => first.Node("sample").Progress.WholeTicks > 0, "first partial learning");
            c.That(mine.Layers.Count == 0 && mine.Version == before, "partial real work grants no premature region");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "first partial native work saves without fog cache");
            Reach(game, () => mine.Layers.Count == 2, "first physical region completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "earned first-region twins agree");
            c.That(mine.Version == before + 1 && other.Version == foreign && retained.Count == 0, "only beneficiary invalidates once and retained metadata stays detached");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "real machine work consumes eight supplies");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("point.terrain", Region(1), values), "known point is readable");
            c.Equal(Fix.FromInt(11), values[0], "known sample is actual value, not learned-fact payload");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("point.deposit", Region(1), values), "knowledge AND ownership authorize actual stored deposit");
            c.Equal(Fix.FromInt(21), values[0], "knowledge creates no deposit stock");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("point.terrain", Region(1, 5), values), "mixed known/unknown full read refuses atomically");
            c.Equal(0, values.Count, "mixed refusal clears all retained values");
            var page = mine.ReadLayerPage("point.terrain", Region(1, 5), new SpatialLayerPageRequest(0, 1), values);
            c.That(page.Status == SpatialLayerReadStatus.Success && page.TotalCount == 5 && page.Count == 1, "bounded known prefix may read a larger partly unknown request");
            before = mine.Version;
            ulong hash = game.Hash();
            var denied = mine.ReadLayerPage("point.terrain", Region(1, 5), new SpatialLayerPageRequest(2, 1, page.Cursor), values);
            c.That(denied.Status == SpatialLayerReadStatus.Unavailable && denied.Cursor == null && denied.TotalCount == 0 && values.Count == 0, "unknown page reveals no cursor/count/partial samples");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.FromInt(5), Value = Fix.One }), "ordinary sample preview refuses unknown point");
            c.Equal(hash, game.Hash(), "sample and command previews are pure");
            c.Equal(Code.None, space.WriteLayer(game.World, PlayerId.None, "point.terrain", Fix.FromInt(5), Fix.FromInt(55)), "observer writes unknown actual point");
            c.That(mine.Version == before && other.Version == foreign, "unknown write in known layer leaks no company version");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayerPage("point.terrain", Region(1, 5), new SpatialLayerPageRequest(0, 1, page.Cursor), values).Status, "hidden point write preserves held known page");
            c.Equal(Codes.NotFound, TestKit.Order(game, 0, new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.FromInt(5), Value = Fix.One }).Refusal, "normal command and preview use identical sample admission");
            Order second = Start(c, game, "field.discover-second");
            Reach(game, () => second.Node("sample").Progress.WholeTicks > 0, "second partial learning");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("point.terrain", Region(5), values), "second area remains unknown during timed work");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "second partial work and existing knowledge restore");
            Reach(game, () => second.State == OrderState.Done, "second physical region completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "second region earned from identical physical continuation");
            c.That(mine.Layers.Count == 2 && mine.Version == before + 1 && other.Version == foreign, "new area invalidates ONCE despite unchanged layer metadata membership");
            c.Equal(Fix.FromInt(4), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "two activities consume actual sixteen supplies once");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("point.terrain", Region(5), values), "second known area becomes readable");
            c.Equal(Fix.FromInt(55), values[0], "new area reads actual intervening hidden write");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("point.terrain", Region(3), values), "gap between learned areas remains unknown");
            c.Equal(SpatialLayerReadStatus.Invalidated, mine.ReadLayerPage("point.terrain", Region(1, 5), new SpatialLayerPageRequest(0, 1, page.Cursor), values).Status, "new area invalidates old known-page continuation");
            int sequence = game.Seat(0).Submit(new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.FromInt(5), Value = Fix.FromInt(77) });
            twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(sequence > 0 && game.Hash() == twin.Hash(), "queued ordinary known-sample command restores and commits identically");
            c.That(mine.Version == before + 2 && other.Version == foreign, "known sample write invalidates only known beneficiary");
            c.Equal(SpatialLayerReadStatus.Success, twin.Seat(0).View.Feature<PointLayerView>().ReadLayer("point.terrain", Region(5), values), "restored scope cache derives from fully restored knowledge");
            c.Equal(Fix.FromInt(77), values[0], "queued command changed actual native value");
            before = mine.Version; Tick(game); c.Equal(before, mine.Version, "stable scope refresh does not repeatedly invalidate");
            foreach (PlayerId invalid in new[] { new PlayerId(-2), new PlayerId(9), new PlayerId(1) })
            {
                var view = new PointLayerView(space, invalid);
                c.Equal(SpatialLayerReadStatus.Unavailable, view.ReadLayer("point.terrain", Region(5), values), "foreign/invalid viewer cannot borrow learned area");
                c.Equal(0, view.Layers.Count, "foreign/invalid viewer has no regional metadata");
            }
            c.Equal(SpatialLayerReadStatus.Success, observer.ReadLayer("point.terrain", Region(1, 5), values), "exact observer deliberately reads all actual samples");
            c.Equal(0, ConstructionPlanTests.NewPoints(factory).Seat(0).View.Feature<PointLayerView>().Layers.Count, "interleaved new session borrows no learned state");
            Exchange(c, factory, game);
            SimGame cancelled = ConstructionPlanTests.NewPoints(factory); Configure(c, cancelled);
            Order abandoned = Start(c, cancelled, "field.discover");
            c.That(TestKit.Order(cancelled, 0, new CancelOrderCommand { Order = abandoned.Id }).Accepted, "ordinary player cancellation stops physical learning");
            for (int i = 0; i < 30; i++) Tick(cancelled);
            c.That(cancelled.Seat(0).View.Feature<PointLayerView>().Layers.Count == 0 && cancelled.Hash() == factory.Load(cancelled.Save()).Hash(), "cancelled incomplete learning reveals nothing and checkpoints");
        }
        private static KnowledgeObservation Fact(SimGame game, int seat, string subject)
        {
            if (!game.Seat(seat).View.Feature<KnowledgeView>().TryRead(new PlayerId(seat), "sim.knowledge.facts", subject, out var fact))
                throw new InvalidOperationException("missing regional fact " + seat + "/" + subject);
            return fact;
        }
        private static SimGame ExchangeCommand(Check c, GameFactory factory, SimGame game, int seat, Command command, Code expected)
        {
            ulong before = game.Hash();
            c.Equal(expected, game.Seat(seat).Preview(command), "regional exchange preview and ordinary admission agree");
            c.Equal(before, game.Hash(), "regional exchange preview changes no learned state or game time");
            int sequence = game.Seat(seat).Submit(command);
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "regional exchange saves the actual pending command before commit");
            Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "regional exchange applies identically after pending-inbox restore");
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(seat).ReadResults(ref cursor, results);
            var result = results.Single(row => row.Sequence == sequence);
            c.That(result.Accepted == expected.IsNone && result.Refusal == expected, "regional exchange result preserves accepted/refused semantics");
            c.Equal(Fix.FromInt(4), TransformationContentContractTests.Stock(game, "source"),
                "knowledge exchange never repeats physical survey costs or generates materials");
            return twin;
        }
        private static void Exchange(Check c, GameFactory factory, SimGame game)
        {
            // Both regional facts above were earned by the original two eighteen-minute supplied
            // activities. From this checkpoint onward every mutation is an ordinary player command.
            var idle = ConstructionPlanTests.NewPoints(factory); ulong isolated = idle.Hash();
            var mine = game.Seat(0).View.Feature<PointLayerView>(); var other = game.Seat(1).View.Feature<PointLayerView>();
            var knowledge = game.Seat(0).View.Feature<KnowledgeView>();
            var original = Fact(game, 0, "field.observed");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "regional exchange needs no business module");
            c.That(((Def)game.World.Content.Get<IKnowledgeSharingPolicy>("regional.share-both")).AuthoringPath.Contains("regional-knowledge-exchange.json"),
                "sharing and forgetting are explicit file/master content, not player-side policy");
            int own = mine.Version, foreign = other.Version;
            var limits = new ReplayLimits(4 * 1024 * 1024, 1000); using var recording = game.RecordReplay(limits);
            var twin = ExchangeCommand(c, factory, game, 0, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "regional.share-both" }, Code.None);
            c.That(mine.Version == own && other.Version == foreign + 1, "sharing changes only recipient's regional scope once");
            var received = Fact(game, 1, "field.observed");
            c.That(received.ObservedTick == original.ObservedTick && received.Data.ToString() == original.Data.ToString(),
                "sharing preserves earned observation chronology and immutable payload");
            var values = new List<Fix>();
            c.Equal(SpatialLayerReadStatus.Success, other.ReadLayer("point.terrain", Region(1), values), "recipient learns the actual first region");
            c.Equal(Fix.FromInt(11), values.Single(), "recipient reads original native terrain, not a copied sample ledger");
            c.Equal(SpatialLayerReadStatus.Success, other.ReadLayer("point.terrain", Region(5), values), "recipient learns actual second region");
            c.Equal(Fix.FromInt(77), values.Single(), "recipient reads current previously edited second sample");
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer("point.terrain", Region(3), values), "sharing disjoint regions does not reveal the intervening gap");
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer("point.deposit", Region(1), values), "shared knowledge never transfers private layer ownership");
            c.Equal(0, values.Count, "ownership refusal clears shared sample output");

            var firstPage = mine.ReadLayerPage("point.terrain", Region(1), new SpatialLayerPageRequest(0, 1), values);
            var secondPage = mine.ReadLayerPage("point.terrain", Region(5), new SpatialLayerPageRequest(0, 1), values);
            var heldLayers = mine.Layers; var heldFacts = new List<KnowledgeObservation>(); knowledge.CopyKnown(heldFacts);
            own = mine.Version; foreign = other.Version;
            twin = ExchangeCommand(c, factory, game, 0, new ForgetKnowledgeCommand { Policy = "regional.forget-first" }, Code.None);
            c.That(mine.Version == own + 1 && other.Version == foreign, "selective forgetting changes only sender scope once");
            c.That(!knowledge.TryRead(new PlayerId(0), "sim.knowledge.facts", "field.observed", out _) && Fact(game, 0, "field.second").Data.Frozen,
                "forgetting first fact retains the other actual earned fact");
            c.That(heldLayers.Count == 2 && heldFacts.Any(row => row.Subject == "field.observed") && mine.Layers.Count == 1,
                "old projections stay detached while actual private all-requirement layer becomes hidden");
            var refused = mine.ReadLayerPage("point.terrain", Region(1), new SpatialLayerPageRequest(0, 1, firstPage.Cursor), values);
            c.That(refused.Status == SpatialLayerReadStatus.Unavailable && refused.TotalCount == 0 && refused.Cursor == null && values.Count == 0,
                "forgotten regional sample refuses before old page identity disclosure");
            c.Equal(SpatialLayerReadStatus.Invalidated, mine.ReadLayerPage("point.terrain", Region(5), new SpatialLayerPageRequest(0, 1, secondPage.Cursor), values).Status,
                "scope change invalidates retained page even when its own region stays known");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("point.terrain", Region(5), values), "any-fact second region stays readable after first fact forgotten");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("point.deposit", Region(5), values), "all-fact private second region loses authorization");
            c.Equal(SpatialLayerReadStatus.Success, other.ReadLayer("point.terrain", Region(1), values), "sender forgetting never revokes recipient's history");
            c.Equal(SpatialLayerReadStatus.Unavailable, twin.Seat(0).View.Feature<PointLayerView>().ReadLayer("point.terrain", Region(1), values),
                "saved command continuation does not resurrect forgotten scope");

            ExchangeCommand(c, factory, game, 1, new ForgetKnowledgeCommand { Policy = "regional.forget-second" }, Code.None);
            c.That(!game.Seat(1).View.Feature<KnowledgeView>().TryRead(new PlayerId(1), "sim.knowledge.facts", "field.second", out _),
                "recipient intentionally removes second fact before atomic refusal check");
            own = mine.Version; foreign = other.Version;
            ExchangeCommand(c, factory, game, 0, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "regional.share-both" }, Codes.NotFound);
            c.That(mine.Version == own && other.Version == foreign && !game.Seat(1).View.Feature<KnowledgeView>()
                .TryRead(new PlayerId(1), "sim.knowledge.facts", "field.second", out _), "missing first share input prevents available second-fact prefix and all scope changes");

            ExchangeCommand(c, factory, game, 1, new ShareKnowledgeCommand { Recipient = new PlayerId(0), Policy = "regional.return-first" }, Code.None);
            c.That(Fact(game, 0, "field.observed").ObservedTick == original.ObservedTick,
                "return sharing restores historical observation, never fabricates a fresh survey time");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("point.deposit", Region(5), values), "returned first fact restores actual owned all-fact region");
            c.Equal(Fix.FromInt(25), values.Single(), "returned knowledge generates no deposit quantity");
            ExchangeCommand(c, factory, game, 0, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "regional.share-both" }, Code.None);
            c.Equal(SpatialLayerReadStatus.Success, other.ReadLayer("point.terrain", Region(5), values), "complete re-share restores recipient second coverage");
            own = mine.Version; foreign = other.Version;
            var observedAt = Fact(game, 1, "field.observed").ObservedTick;
            ExchangeCommand(c, factory, game, 0, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "regional.share-both" }, Code.None);
            c.That(mine.Version == own && other.Version == foreign && Fact(game, 1, "field.observed").ObservedTick == observedAt,
                "duplicate permanent share does not invalidate stable scopes or refresh observation time");
            ExchangeCommand(c, factory, game, 1, new ForgetKnowledgeCommand { Policy = "regional.forget-first" }, Codes.NotAllowed);
            c.That(mine.Version == own && other.Version == foreign, "wrong-company policy refusal changes neither scope");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final exchanged ledger and independent regional provider restore exactly");
            c.Equal(isolated, idle.Hash(), "regional exchange never changes another session's knowledge, samples or clock");
            c.Equal(8, recording.CommandCount, "exchange recording contains every actual accepted/refused player command");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "exchange recording spans every saved-inbox continuation update");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "regional exchange replays to final live state, not a valid prefix");
            Console.WriteLine("regional exchange replay: initial=" + recording.InitialStep + ";final=" + game.Clock.StepIndex + ";commands=" + recording.CommandCount
                + ";checkpoints=" + recording.CheckpointCount + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + verified.ActualHash.ToString("X16"));
        }
        private static void Learn(Check c, SimGame game, int seat, string id)
        {
            // Explicit protocol instrumentation only. The player proof above earns both facts physically.
            KnowledgeFactDef fact = game.World.Content.Get<KnowledgeFactDef>(id);
            c.Equal(Code.None, game.World.Services.Get<ICompanyKnowledge>().Record(new PlayerId(seat), "sim.knowledge.facts", id, fact.Value), "native protocol fixture records actual declared fact");
            Tick(game);
        }
        private static void NativeProtocol(Check c)
        {
            GameFactory factory = Tiles(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "layers", Seed = 19 });
            var map = (TileMap)game.World.Space; var mine = game.Seat(0).View.Feature<TileMapView>();
            var other = game.Seat(1).View.Feature<TileMapView>(); var observer = game.Observer.Feature<TileMapView>();
            var first = new TileCoord(69996, 69996); var gap = new TileCoord(69997, 69996); var second = new TileCoord(69999, 69996);
            var whole = new TileRect(first, 4, 1); var values = new List<int> { 999 }; var fixedValues = new List<Fix>();
            map.AssignOwner(game.World, "observer.private", new PlayerId(0));
            map.Layer<IntLayer>("observer.public").Set(first, 11); map.Layer<IntLayer>("observer.public").Set(gap, 33);
            map.Layer<IntLayer>("observer.public").Set(second, 55); map.Layer<FixedLayer>("observer.private").Set(first, Fix.FromInt(7));
            int before = mine.Version, foreign = other.Version;
            c.That(mine.Layers.Count == 0 && mine.Version == before, "wide native sample setup discloses no unknown layer");
            Learn(c, game, 0, "field.observed");
            c.That(mine.Layers.Count == 2 && mine.Version == before + 1 && other.Version == foreign, "native authored regional knowledge privately reveals metadata once");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("observer.public", new TileRect(first, 1, 1), values), "native rectangular sample authorization");
            c.Equal(11, values[0], "actual wide sparse integer sample");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("observer.private", new TileRect(first, 1, 1), fixedValues), "same authorization for actual fixed resource layer");
            c.Equal(Fix.FromInt(7), fixedValues[0], "actual native quantity, no learned stock copy");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("observer.public", whole, values), "native mixed rectangle refuses atomically");
            c.Equal(0, values.Count, "native mixed rectangle exposes no prefix");
            var page = mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(0, 1), values);
            before = mine.Version; var baseline = mine.ReadChanges().Cursor;
            map.Layer<IntLayer>("observer.public").Set(gap, 44);
            c.That(mine.Version == before && other.Version == foreign, "hidden tile IN SAME CHUNK does not change visible company version");
            SpatialRegionChanges<TileRect> delta = mine.ReadChanges(baseline);
            c.That(!delta.FullInvalidation && delta.ChangedRegions.Count == 0, "hidden tile produces no changed-chunk disclosure");
            var denied = mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(1, 1, page.Cursor), values);
            c.That(denied.Status == SpatialLayerReadStatus.Unavailable && denied.Cursor == null && denied.TotalCount == 0, "native hidden page refuses cursor/count");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(0, 1, page.Cursor), values).Status, "hidden same-chunk write preserves known native page");
            Learn(c, game, 0, "field.second");
            c.That(mine.Layers.Count == 2 && mine.Version == before + 1, "native second region invalidates without changing known layer list");
            delta = mine.ReadChanges(baseline);
            c.That(delta.FullInvalidation && delta.ChangedRegions.Count == 0, "region reveal does not enumerate hidden chunks");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("observer.public", new TileRect(second, 1, 1), values), "native painted sample region is supported through same contract");
            c.Equal(55, values[0], "actual second region value");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("observer.public", new TileRect(gap, 1, 1), values), "painted/rectangular union leaves actual gap hidden");
            c.Equal(SpatialLayerReadStatus.Invalidated, mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(0, 1, page.Cursor), values).Status, "native reveal invalidates retained known page");
            before = mine.Version; baseline = mine.ReadChanges().Cursor; map.Layer<IntLayer>("observer.public").Set(second, 66);
            delta = mine.ReadChanges(baseline);
            c.That(mine.Version == before + 1 && delta.ChangedRegions.Count == 1 && !delta.FullInvalidation && other.Version == foreign, "known single tile produces private real chunk invalidation");
            Learn(c, game, 1, "field.observed");
            c.That(other.Layers.Count == 1 && other.LayerIds[0] == "observer.public", "foreign own knowledge cannot bypass private layer ownership");
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer("observer.private", new TileRect(first, 1, 1), fixedValues), "private native ownership remains mandatory");
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer("observer.public", new TileRect(second, 1, 1), values), "other company's different partial knowledge is authoritative");
            c.Equal(SpatialLayerReadStatus.Success, observer.ReadLayer("observer.public", whole, values), "exact observer reads whole native rectangle explicitly");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "sole knowledge and native sparse values restore");
            c.Equal(SpatialLayerReadStatus.Success, twin.Seat(0).View.Feature<TileMapView>().ReadLayer("observer.public", new TileRect(second, 1, 1), values), "after-load scope refresh uses restored ledger");
            c.Equal(66, values[0], "restored actual native sample");
            ulong hash = game.Hash(); _ = mine.Layers; mine.ReadChanges(baseline); mine.ReadLayer("observer.public", whole, values);
            c.Equal(hash, game.Hash(), "regional reads/deltas are pure");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "native continued sessions agree");
            JsonValue strict = Content.Json.Parse(FrameworkProofContent.Read("regional-tile-visibility.json"));
            strict.Get("knowledge_visibility_policies")[0].Set("unlisted_content", JsonValue.Of(false));
            SimGame deniedLinks = Tiles(strict.ToString()).NewGame(new NewGameRequest { Scenario = "layers" }); Learn(c, deniedLinks, 0, "field.observed");
            c.Equal(0, deniedLinks.Seat(0).View.Feature<TileMapView>().Layers.Count, "known regions cannot reveal separately hidden linked units/resources");
        }
        private static void InvalidAuthoring(Check c)
        {
            foreach (string mode in new[] { "missing-region", "missing-layer", "missing-fact", "empty", "duplicate-region", "duplicate-fact", "mode" })
            {
                JsonValue root = Content.Json.Parse(Visibility); JsonValue rule = root.Get("layer_region_knowledge_visibility")[0];
                JsonValue entry = rule.Get("regions")[0];
                switch (mode)
                {
                    case "missing-region": entry.Set("region", JsonValue.Of("missing")); break;
                    case "missing-layer": rule.Set("definition", JsonValue.Of("missing")); break;
                    case "missing-fact": entry.Set("facts", Content.Json.Parse("[\"missing\"]")); break;
                    case "empty": rule.Set("regions", JsonValue.NewArray()); break;
                    case "duplicate-region": rule.Get("regions").Add(entry.Clone()); break;
                    case "duplicate-fact": entry.Set("facts", Content.Json.Parse("[\"field.observed\",\"field.observed\"]")); break;
                    case "mode": entry.Set("mode", JsonValue.Of("guess")); break;
                }
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Points(root.ToString())), "invalid authored regional content refuses: " + mode);
            }
            JsonValue areas = Content.Json.Parse(FrameworkProofContent.Read("regional-point-areas.json"));
            areas.Get("point_interval_regions")[0].Set("space", JsonValue.Of("missing"));
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Points(areas: areas.ToString())), "region with unresolvable space refuses before play");
            areas = Content.Json.Parse(FrameworkProofContent.Read("regional-point-areas.json"));
            areas.Set("tile_spaces", Content.Json.Parse("[{\"id\":\"foreign.tiles\",\"width\":10,\"height\":10}]"));
            areas.Set("rectangular_tile_regions", Content.Json.Parse("[{\"id\":\"foreign.region\",\"space\":\"foreign.tiles\",\"x\":0,\"y\":0,\"width\":1,\"height\":1}]"));
            JsonValue foreign = Content.Json.Parse(Visibility);
            foreign.Get("layer_region_knowledge_visibility")[0].Get("regions")[0].Set("region", JsonValue.Of("foreign.region"));
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Points(foreign.ToString(), areas.ToString())), "fully resolved region in a different valid space refuses linked validation");
        }
    }
}
