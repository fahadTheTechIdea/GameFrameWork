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

namespace Beep.SimEngine.Tests
{
    internal static class KnowledgeSharingTests
    {
        internal static void Register() => TestRunner.Add("Knowledge sharing: authored private exchange preserves physical observation age", Run);
        private const string Provider = "sim.knowledge.facts", Reading = "survey.reading", Region = "contact.north.known";
        private static PlayerId Company(int seat) => new PlayerId(seat);
        private static GameFactory Factory(JsonValue sharing = null)
        {
            var reading = Content.Json.Parse(FrameworkProofContent.Read("native-survey-knowledge.json"));
            reading.Get("tile_readings")[0].Set("fresh_for", Content.Json.Parse("{\"numerator\":150,\"denominator\":1,\"unit\":\"second\"}"));
            return ContactExplorationTests.Factory(sharing: sharing ?? Content.Json.Parse(FrameworkProofContent.Read("native-knowledge-sharing.json")), reading: reading);
        }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static KnowledgeView Knowledge(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<KnowledgeView>();
        private static KnowledgeObservation Fact(SimGame game, int seat, string subject)
        {
            if (!Knowledge(game, seat).TryRead(Company(seat), Provider, subject, out var fact)) throw new InvalidOperationException("missing own observation " + seat + "/" + subject);
            return fact;
        }
        private static void Update(SimGame game, SimGame twin = null)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("sharing clock=" + game.Tick + "/" + game.Fault); if (twin != null) Update(twin); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1000 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + "/clock=" + game.Tick + "/position=" + Find(game, "survey.machine").Get<ITilePresence>().Tile
                + "/orders=" + string.Join(";", game.World.Services.Get<WorkBoard>().Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static ShareKnowledgeCommand Share(string policy, int recipient = 1) => new ShareKnowledgeCommand { Recipient = Company(recipient), Policy = policy };
        private static Order Survey(Check c, SimGame game, string plan)
        {
            var command = new PostOrderCommand { Plan = plan, Target = Find(game, "survey.site").Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal physical survey admission");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal player survey, no test knowledge grant");
            return game.World.Services.Get<WorkBoard>().Order((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "sharing changes deterministic engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same ledger payload fields and no new saved owner");
            var factory = Factory(); var game = LiveSensorTests.New(factory); var ledger = game.World.Services.Get<ICompanyKnowledge>();
            c.That(ReferenceEquals(ledger, game.World.Services.Get<IKnowledgeSharing>()), "same ledger owns sharing");
            c.That(ReferenceEquals(ledger, game.World.Services.Get<IKnowledgeFreshness>()), "same ledger owns historical age");
            c.Equal(3, ((CompanyKnowledgeService)ledger).StateSchemaVersion, "acquired old information has explicit new chronology semantics");
            c.That(!game.World.Services.Has<IEconomy>(), "no purchase, market or account prerequisite");
            c.That(!typeof(IKnowledgeSharingProvider).IsAssignableFrom(typeof(TechnologyService)), "research provider does not implicitly opt in");
            c.That(game.World.Content.Get<AuthoredKnowledgeSharingDef>("share.whole").AuthoringPath.Contains("native-knowledge-sharing.json"), "sharing configuration is master-listed JSON");
            c.Equal(Codes.NotFound, Knowledge(game).PreviewShare(Company(1), "share.whole", out var absent), "unearned outgoing facts unavailable");
            c.Equal(2, absent.Entries.Count, "only explicit canonical subject set");
            c.Equal(Region, absent.Entries[0].Key.Subject, "stable provider/subject order independent of JSON order");
            c.That(absent.Entries.All(e => !e.Observation.HasValue), "preview fabricates no fact");
            c.Equal(Codes.NotAllowed, Knowledge(game, 1).PreviewShare(Company(0), "share.region", out absent), "asymmetric direction is not an implicit alliance");
            c.That(absent == null, "forbidden direction gives no subject/policy internals");
            c.Equal(Codes.InvalidArgument, Knowledge(game).PreviewShare(Company(0), "share.region", out absent), "self-sharing refused");
            c.Equal(Codes.NotSeated, Knowledge(game).PreviewShare(new PlayerId(8), "share.region", out absent), "recipient must actually exist");
            c.Equal(Codes.NotFound, Knowledge(game).PreviewShare(Company(1), "missing.policy", out absent), "unknown policy unavailable");
            c.Equal(Codes.InvalidArgument, Knowledge(game).PreviewShare(Company(1), null, out absent), "malformed policy explicit refusal");
            c.Equal(Codes.NotSeated, game.World.Services.Get<IKnowledgeSharing>().PreviewShare(PlayerId.None, Company(1), "share.region", out absent), "tooling viewer is not a source company");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = Find(game, "survey.machine").Id, Held = true }).Accepted, "player holds automatic jobs during manual contact travel");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = Find(game, "survey.machine").Id, To = new TileCoord(8, 10) }).Accepted, "ordinary real movement toward discovery");
            var twin = factory.Load(game.Save());
            Reach(game, () => Knowledge(game).TryRead(Company(0), Provider, Region, out _), "earned contact exploration", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial travel/actual contact history saved twins");
            Reach(game, () => Find(game, "survey.machine").Get<IMotion>().Status == MoveStatus.Arrived, "manual movement settles");
            var sourceRegion = Fact(game, 0, Region);
            c.That(Find(game, "survey.machine").Get<ITilePresence>().Tile == new TileCoord(8, 10), "real machine physically visited declared region");
            var region = new TileRect(8, 10, 1, 1); var values = new List<int>(); var map = game.Seat(1).View.Feature<TileMapView>();
            c.Equal(SpatialLayerReadStatus.Unavailable, map.ReadLayer("survey.terrain", region, values), "recipient has no unearned visibility");
            c.Equal(Codes.NotFound, Knowledge(game, 1).ReadFreshness(Company(0), Provider, Region, out _), "sharing permission is not remote sender inspection");
            ulong hash = game.Hash();
            c.Equal(Codes.NotFound, Knowledge(game).PreviewShare(Company(1), "share.whole", out var incomplete), "whole profile waits for missing physical reading");
            c.That(incomplete.Entries[0].Observation.HasValue && !incomplete.Entries[1].Observation.HasValue, "safe sender-only missing-subject diagnostics");
            c.Equal(hash, game.Hash(), "whole admission is pure");
            var refused = TestKit.Order(game, 0, Share("share.whole"));
            c.That(!refused.Accepted && refused.Refusal == Codes.NotFound, "normal command has same missing-subject refusal");
            c.That(!Knowledge(game, 1).TryRead(Company(1), Provider, Region, out _), "no recipient prefix on incomplete profile");

            hash = game.Hash(); c.Equal(Code.None, Knowledge(game).PreviewShare(Company(1), "share.region", out var preview), "earned outgoing region admitted");
            c.Equal(Code.None, game.Seat(0).Preview(Share("share.region")), "ordinary command and management preview match");
            c.Equal(hash, game.Hash(), "all sharing previews are observational");
            c.Throws<NotSupportedException>(() => ((IList<KnowledgeSubjectSnapshot>)preview.Entries).Clear(), "entry collection detached and immutable");
            c.Throws<InvalidOperationException>(() => preview.Entries[0].Observation.Value.Data.Set("area", JsonValue.Of("forged")), "outgoing payload frozen");
            var pending = Share("share.region"); int sequence = game.Seat(0).Submit(pending); long receiptTime = game.Tick;
            pending.Policy = "missing.policy"; pending.Recipient = Company(0);
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending share saved through original command inbox");
            Update(game, twin); var results = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(r => r.Sequence == sequence).Accepted, "inbox snapshots caller-owned intent before mutation");
            c.Equal(game.Hash(), twin.Hash(), "saved pending share commits same original ledger");
            var received = Fact(game, 1, Region);
            c.Equal(receiptTime, received.LearnedTick, "recipient learned at actual receipt, not before sharing");
            c.Equal(sourceRegion.ObservedTick, received.ObservedTick, "first fact retains actual earlier source observation time");
            c.That(received.ObservedTick < received.LearnedTick, "old observation can legitimately precede recipient acquisition");
            c.Equal(sourceRegion.Data.ToString(), received.Data.ToString(), "same immutable observed data");
            c.Equal(SpatialLayerReadStatus.Success, map.ReadLayer("survey.terrain", region, values), "original regional visibility now sees recipient knowledge");
            c.Equal(0, values[0], "visibility reads actual native public sample, not copied terrain");
            c.Equal(SpatialLayerReadStatus.Unavailable, map.ReadLayer("survey.terrain", new TileRect(6, 10, 3, 1), values), "sharing does not expose surrounding unvisited samples");
            c.Equal(0, values.Count, "mixed read contains no successful prefix");
            c.Equal(Codes.NotFound, Knowledge(game).ReadFreshness(Company(1), Provider, Region, out _), "sender cannot inspect recipient facts after sharing");
            c.Equal(Code.None, Knowledge(game).PreviewShare(Company(1), "share.region", out var repeated), "repeat admission discloses no recipient possession");
            c.Equal(preview.Entries[0].Observation.Value.ObservedTick, repeated.Entries[0].Observation.Value.ObservedTick, "outgoing preview unchanged after receipt");
            c.That(TestKit.Order(game, 0, Share("share.region")).Accepted, "repeat first-learned share is idempotently accepted");
            c.Equal(received.LearnedTick, Fact(game, 1, Region).LearnedTick, "repeat never rewrites first acquisition");
            c.Equal(received.ObservedTick, Fact(game, 1, Region).ObservedTick, "repeat never observes again");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = Find(game, "survey.machine").Id, Held = false }).Accepted, "player releases worker for genuine survey");
            var order = Survey(c, game, "survey.sample");
            Reach(game, () => order.Node("sample").Progress.WholeTicks > 0, "physical material-consuming survey starts");
            c.That(!Knowledge(game).TryRead(Company(0), Provider, Reading, out _), "partial survey cannot be shared as completed knowledge");
            c.Equal(Fix.FromInt(12), Find(game, "survey.site").Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "actual eight materials consumed once");
            twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "physical survey completes", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial physical survey continuation identical");
            var reading = Fact(game, 0, Reading);
            c.Equal(Code.None, Knowledge(game).PreviewShare(Company(1), "share.whole", out var whole), "all actual authored subjects now available");
            c.Equal(2, whole.Entries.Count, "whole-set admission uses original ledger subjects");
            long learnedAt = game.Tick; c.That(TestKit.Order(game, 0, Share("share.whole")).Accepted, "actual whole share succeeds without resources or re-survey");
            var recipientReading = Fact(game, 1, Reading);
            c.Equal(learnedAt, recipientReading.LearnedTick, "latest reading recipient acquisition uses clock");
            c.Equal(reading.ObservedTick, recipientReading.ObservedTick, "reading timestamp not refreshed by sharing");
            c.Equal(reading.Data.ToString(), recipientReading.Data.ToString(), "physical reading data copied exactly");
            c.Equal(Code.None, Knowledge(game, 1).ReadFreshness(Company(1), Provider, Reading, out var fresh), "recipient own original-provider freshness");
            c.That(fresh.IsFresh && fresh.AgeSteps == 2, "sharing spends an actual minute but preserves original observation age");
            c.Equal(SpatialLayerReadStatus.Unavailable, map.ReadLayer("survey.deposit", new TileRect(16, 10, 1, 1), new List<Fix>()), "shared historical reading does not bypass actual company-private layer owner");
            twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); long now = game.Tick;
            for (int i = 0; i < 3; i++) Update(game, twin);
            c.Equal(now, game.Tick, "paused sole clock does not age transmitted data");
            c.Equal(game.Hash(), twin.Hash(), "paused acquired old information saves correctly");
            game.Clock.Resume(); twin.Clock.Resume(); Update(game, twin);
            c.Equal(Code.None, Knowledge(game, 1).ReadFreshness(Company(1), Provider, Reading, out var expired), "stale transmission remains historical");
            c.That(!expired.IsFresh && expired.AgeSteps == 3, "same exact expiry as original source");
            c.That(fresh.IsFresh && fresh.AgeSteps == 2, "retained freshness snapshot stays detached");
            c.That(TestKit.Order(game, 0, Share("share.reading")).Accepted, "stale information may be explicitly shared, not silently freshened");
            c.Equal(recipientReading.ObservedTick, Fact(game, 1, Reading).ObservedTick, "stale repeat cannot renew reading");
            c.Equal(recipientReading.LearnedTick, Fact(game, 1, Reading).LearnedTick, "stale repeat cannot acquire a second first time");
            var refresh = Survey(c, game, "survey.reobserve"); twin = factory.Load(game.Save());
            Reach(game, () => refresh.State == OrderState.Done, "genuine positive-time physical refresh", twin);
            var newer = Fact(game, 0, Reading); c.That(newer.ObservedTick > reading.ObservedTick, "only completed re-survey earns newer reading");
            c.Equal(game.Hash(), twin.Hash(), "real refresh saved twins");
            c.That(TestKit.Order(game, 1, Share("share.reading", 0)).Accepted, "explicit permitted reverse transmission accepted");
            c.Equal(newer.ObservedTick, Fact(game, 0, Reading).ObservedTick, "older incoming reading never overwrites newer recipient data");
            c.Equal(reading.LearnedTick, Fact(game, 0, Reading).LearnedTick, "recipient original acquisition preserved");
            c.That(TestKit.Order(game, 0, Share("share.reading")).Accepted, "newer actual outgoing observation updates recipient");
            c.Equal(newer.ObservedTick, Fact(game, 1, Reading).ObservedTick, "latest merge retains real newer observation time");
            c.Equal(recipientReading.LearnedTick, Fact(game, 1, Reading).LearnedTick, "latest merge preserves original recipient learning");
            c.Equal(recipientReading.ObservedTick, whole.Entries[1].Observation.Value.ObservedTick, "earlier outgoing snapshot never rewrites after refresh");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "shared first and latest chronology round-trip in same ledger payload");
            var rows = new List<KnowledgeObservation>(); Knowledge(game, 1).CopyKnown(rows);
            c.Equal(2, rows.Count, "recipient only has explicitly transmitted facts, no geological/region subscription");
            c.Equal(Fix.FromInt(12), Find(game, "survey.site").Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "sharing never consumes/creates/refunds physical stock");
            var isolated = LiveSensorTests.New(factory); c.That(!Knowledge(isolated, 1).TryRead(Company(1), Provider, Region, out _), "independent session has no shared history");
            var unconfigured = LiveSensorTests.New(ContactExplorationTests.Factory());
            c.Equal(Codes.NotFound, Knowledge(unconfigured).PreviewShare(Company(1), "share.region", out absent), "no profile implies no implicit sharing policy");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-subjects", "empty-subjects", "duplicate-subject", "provider", "subject", "empty-key", "missing-directions", "empty-directions", "duplicate-direction", "negative", "self", "unseated", "extra" })
            {
                var doc = Content.Json.Parse(FrameworkProofContent.Read("native-knowledge-sharing.json")); var policy = doc.Get("knowledge_sharing_policies")[0];
                if (mode == "missing-subjects") policy.Remove("subjects");
                if (mode == "empty-subjects") policy.Set("subjects", JsonValue.NewArray());
                if (mode == "duplicate-subject") policy.Get("subjects").Add(policy.Get("subjects")[0].Clone());
                if (mode == "provider") policy.Get("subjects")[0].Set("provider", JsonValue.Of("missing.provider"));
                if (mode == "subject") policy.Get("subjects")[0].Set("subject", JsonValue.Of("missing.subject"));
                if (mode == "empty-key") policy.Get("subjects")[0].Set("subject", JsonValue.Of(""));
                if (mode == "missing-directions") policy.Remove("directions");
                if (mode == "empty-directions") policy.Set("directions", JsonValue.NewArray());
                if (mode == "duplicate-direction") policy.Get("directions").Add(policy.Get("directions")[0].Clone());
                if (mode == "negative") policy.Get("directions")[0].Set("sender", JsonValue.Number("-1"));
                if (mode == "self") policy.Get("directions")[0].Set("recipient", JsonValue.Number("0"));
                if (mode == "unseated") policy.Get("directions")[0].Set("recipient", JsonValue.Number("8"));
                if (mode == "extra") policy.Set("fallback", JsonValue.Of(true));
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(doc)), "invalid authored sharing policy " + mode);
            }
        }
    }
}
