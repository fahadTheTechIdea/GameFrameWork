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
    internal static class KnowledgeForgettingTests
    {
        internal static void Register() => TestRunner.Add("Knowledge forgetting: private whole removal and physical relearning", Run);
        private const string Provider = "sim.knowledge.facts", Region = "contact.north.known", Reading = "survey.reading";
        private static readonly PlayerId Company = new PlayerId(0);
        private static GameFactory Factory(JsonValue forgetting = null, bool configured = true, bool technology = false, JsonValue sharing = null)
        {
            var master = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-master.json"));
            var masterModules = master.Get("modules");
            for (int i = 0; i < masterModules.Count; i++) if (masterModules[i].Get("id").Text == WorkModule.ModuleId) masterModules[i].Get("documents").Add(JsonValue.Of("knowledge-retention-work.json"));
            master.Get("modules").Add(Content.Json.Parse("{\"id\":\"proof.retention\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"knowledge-retention-proof.json\"]}"));
            var world = ContactExplorationTests.WorldDocument(); world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.retention"));
            var modules = new List<GameModule> { new KnowledgeForgettingProofModule() };
            if (technology)
            {
                master.Get("modules").Add(Content.Json.Parse("{\"id\":\"sim.technology\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
                world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(TechnologyModule.ModuleId)); modules.Add(new TechnologyModule());
            }
            var reading = Content.Json.Parse(FrameworkProofContent.Read("native-survey-knowledge.json"));
            reading.Get("tile_readings")[0].Set("fresh_for", Content.Json.Parse("{\"numerator\":150,\"denominator\":1,\"unit\":\"second\"}"));
            return ContactExplorationTests.Factory(world: world, masterDocument: master, reading: reading,
                sharing: sharing ?? Content.Json.Parse(FrameworkProofContent.Read("native-knowledge-sharing.json")),
                forgetting: configured ? forgetting ?? Content.Json.Parse(FrameworkProofContent.Read("native-knowledge-forgetting.json")) : null,
                additionalModules: modules, additionalDocuments: new Dictionary<string, string> {
                    ["knowledge-retention-proof.json"] = FrameworkProofContent.Read("knowledge-retention-proof.json"),
                    ["knowledge-retention-work.json"] = FrameworkProofContent.Read("knowledge-retention-work.json") });
        }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static KnowledgeView Knowledge(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<KnowledgeView>();
        private static bool Known(SimGame game, string subject, int seat = 0, string provider = Provider)
            => Knowledge(game, seat).TryRead(new PlayerId(seat), provider, subject, out _);
        private static void Update(SimGame game, SimGame twin = null)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("forget clock=" + game.Tick + "/" + game.Fault); if (twin != null) Update(twin); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1000 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + "/clock=" + game.Tick + "/position=" + Find(game, "survey.machine").Get<ITilePresence>().Tile
                + "/orders=" + string.Join(";", game.World.Services.Get<WorkBoard>().Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static ForgetKnowledgeCommand Forget(string policy) => new ForgetKnowledgeCommand { Policy = policy };
        private static void Sensor(Check c, SimGame game, bool enabled, SimGame twin = null)
        {
            var command = new ConfigureOperationCommand { Entity = Find(game, "survey.machine").Id, Operation = new Code("survey.sensor"), Enabled = enabled, Target = Fix.FromInt(2) };
            c.That(TestKit.Order(game, 0, command).Accepted, "normal sensor control"); if (twin != null) c.That(TestKit.Order(twin, 0, command).Accepted, "same restored sensor control");
        }
        private static Order Start(Check c, SimGame game, string plan)
        {
            var command = new PostOrderCommand { Plan = plan, Target = Find(game, "survey.site").Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "actual work preview"); var result = TestKit.Order(game, 0, command);
            c.That(result.Accepted, "ordinary player work earns observation"); return game.World.Services.Get<WorkBoard>().Order((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "forgetting engine identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no new ledger layout");
            var factory = Factory(); var game = LiveSensorTests.New(factory); var ledger = game.World.Services.Get<ICompanyKnowledge>();
            c.That(ReferenceEquals(ledger, game.World.Services.Get<IKnowledgeForgetting>()), "same original ledger owns removal");
            c.That(!game.World.Services.Has<IEconomy>(), "information controls do not require economics");
            c.That(game.World.Content.Get<AuthoredKnowledgeForgettingDef>("forget.complete").AuthoringPath.Contains("native-knowledge-forgetting.json"), "master-authored selection and company policy");
            c.Equal(Code.None, Knowledge(game).PreviewForget("forget.complete", out var empty), "absent facts are idempotent no-ops");
            c.Equal(3, empty.Entries.Count, "canonical complete explicit subject set"); c.That(empty.Entries.All(e => !e.Observation.HasValue), "no invented facts");
            c.Equal(Codes.NotAllowed, Knowledge(game, 1).PreviewForget("forget.complete", out var hidden), "foreign company cannot use restricted policy");
            c.That(hidden == null, "forbidden policy discloses no observations");
            c.Equal(Codes.NotSeated, game.World.Services.Get<IKnowledgeForgetting>().PreviewForget(PlayerId.None, "forget.complete", out hidden), "observer cannot impersonate company");
            c.Equal(Codes.NotFound, Knowledge(game).PreviewForget("missing", out hidden), "missing policy refuses");
            c.Equal(Codes.InvalidArgument, Knowledge(game).PreviewForget(null, out hidden), "malformed policy refuses");
            c.That(TestKit.Order(game, 0, Forget("forget.complete")).Accepted, "ordinary absent removal accepted");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = Find(game, "survey.machine").Id, Held = true }).Accepted, "hold autonomous home during manual discovery");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = Find(game, "survey.machine").Id, To = new TileCoord(8, 10) }).Accepted, "normal physical contact travel");
            var twin = factory.Load(game.Save()); Reach(game, () => Known(game, Region), "physical contact discovery", twin);
            c.Equal(game.Hash(), twin.Hash(), "contact travel and earned fact saved twins");
            Reach(game, () => Find(game, "survey.machine").Get<IMotion>().Status == MoveStatus.Arrived, "settled physical arrival"); Sensor(c, game, false);
            c.That(TestKit.Order(game, 0, new ShareKnowledgeCommand { Policy = "share.region", Recipient = new PlayerId(1) }).Accepted, "explicitly export actual earned fact before forgetting");
            var map = game.Seat(0).View.Feature<TileMapView>(); var rect = new TileRect(8, 10, 1, 1); var values = new List<int>();
            var page = map.ReadLayerPage("survey.terrain", rect, new SpatialLayerPageRequest(0, 1), values); c.Equal(SpatialLayerReadStatus.Success, page.Status, "known region readable"); int version = map.Version;
            ulong hash = game.Hash(); c.Equal(Code.None, Knowledge(game).PreviewForget("forget.region", out var preview), "same owned fact eligible for removal");
            c.Equal(Code.None, game.Seat(0).Preview(Forget("forget.region")), "management and normal command admission identical");
            c.Equal(hash, game.Hash(), "removal preview changes no knowledge/cursor/clock");
            c.Throws<NotSupportedException>(() => ((IList<KnowledgeSubjectSnapshot>)preview.Entries).Clear(), "entry list immutable");
            c.Throws<InvalidOperationException>(() => preview.Entries[0].Observation.Value.Data.Set("area", JsonValue.Of("fake")), "original observed data frozen");
            var pending = Forget("forget.region"); int sequence = game.Seat(0).Submit(pending); pending.Policy = "missing";
            game.Clock.Pause(); twin = factory.Load(game.Save()); twin.Clock.Pause(); long clock = game.Tick;
            for (int i = 0; i < 3; i++) Update(game, twin);
            c.Equal(clock, game.Tick, "paused single clock does not process pending forgetting"); c.That(Known(game, Region), "pause does not delete fact");
            c.Equal(game.Hash(), twin.Hash(), "paused saved inbox identical"); game.Clock.Resume(); twin.Clock.Resume(); Update(game, twin);
            var results = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(r => r.Sequence == sequence).Accepted, "snapshotted original profile applied after resume");
            c.That(!Known(game, Region), "original ledger row removed"); c.That(Known(game, Region, 1), "exported other-company copy not globally revoked");
            c.Equal(game.Hash(), twin.Hash(), "same saved pending removal"); c.That(map.Version > version, "original visibility scope invalidates on known membership removal");
            c.Equal(SpatialLayerReadStatus.Unavailable, map.ReadLayerPage("survey.terrain", rect, new SpatialLayerPageRequest(0, 1, page.Cursor), values).Status, "permission checked before old cursor");
            c.Equal(0, values.Count, "forgotten region emits no samples");
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(1).View.Feature<TileMapView>().ReadLayer("survey.terrain", rect, values), "other company retains learned regional visibility");
            c.That(preview.Entries[0].Observation.HasValue, "retained snapshot does not rewrite after removal");
            c.That(TestKit.Order(game, 0, Forget("forget.region")).Accepted, "repeat absent forgetting remains safe");
            Sensor(c, game, true); c.That(!Known(game, Region), "eligible live source must spend full authored dwell again");
            c.Equal(1L, Find(game, "survey.machine").Get<IExplorationSource>(new Code("contact.source")).ReadExploration(game.World).Entries[0].Progress.WholeTicks, "first real relearning interval only");
            twin = factory.Load(game.Save()); Reach(game, () => Known(game, Region), "positive-time contact relearning", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial relearning saved twins"); Knowledge(game).TryRead(Company, Provider, Region, out var relearned);
            c.That(relearned.ObservedTick > preview.Entries[0].Observation.Value.ObservedTick, "new actual observation not old history resurrection");
            c.Equal(Fix.FromInt(20), Find(game, "survey.site").Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "forgetting/contact does not fabricate material refund");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = Find(game, "survey.machine").Id, Held = false }).Accepted, "release actual worker for independent provider work");
            var first = Start(c, game, "retention.learn.base"); Reach(game, () => first.State == OrderState.Done, "physical independent base capture");
            var second = Start(c, game, "retention.learn.child"); twin = factory.Load(game.Save()); Reach(game, () => second.State == OrderState.Done, "physical dependent capture", twin);
            c.Equal(game.Hash(), twin.Hash(), "independent provider uses same original ledger saves");
            c.That(Known(game, "retention.base", provider: RetentionKnowledgeProvider.ProviderId) && Known(game, "retention.child", provider: RetentionKnowledgeProvider.ProviderId), "independent actual provider facts physically earned");
            hash = game.Hash(); c.Equal(RetentionKnowledgeProvider.Required, Knowledge(game).PreviewForget("forget.protected", out var blocked), "whole provider retained dependency veto");
            c.Equal(hash, game.Hash(), "whole removed/retained preview pure");
            c.That(blocked.Entries.Single(e => e.Key.Subject == Region).Blocker.IsNone, "native participant ready does not override another provider veto");
            var refused = TestKit.Order(game, 0, Forget("forget.protected")); c.That(!refused.Accepted && refused.Refusal == RetentionKnowledgeProvider.Required, "same provider refusal through normal command");
            c.That(Known(game, Region) && Known(game, "retention.base", provider: RetentionKnowledgeProvider.ProviderId), "no native or independent deletion prefix on whole refusal");
            c.Equal(Code.None, Knowledge(game).PreviewForget("forget.complete", out var complete), "removing dependent and base together is admissible");
            game.Seat(0).Submit(Forget("forget.complete")); twin = factory.Load(game.Save()); Update(game, twin);
            c.That(!Known(game, Region) && !Known(game, "retention.base", provider: RetentionKnowledgeProvider.ProviderId) && !Known(game, "retention.child", provider: RetentionKnowledgeProvider.ProviderId), "whole owned set removed from original ledger");
            c.That(Known(game, Region, 1), "whole owned removal still preserves foreign export"); c.Equal(game.Hash(), twin.Hash(), "whole multi-provider saved removal");
            var survey = Start(c, game, "survey.sample"); Reach(game, () => survey.Node("sample").Progress.WholeTicks > 0, "material-consuming physical survey");
            twin = factory.Load(game.Save()); Reach(game, () => survey.State == OrderState.Done, "completed physical survey", twin);
            c.Equal(game.Hash(), twin.Hash(), "survey material/progress/history saved continuation");
            c.Equal(Fix.FromInt(12), Find(game, "survey.site").Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "eight actual survey materials spent");
            Knowledge(game).TryRead(Company, Provider, Reading, out var oldReading); c.That(Known(game, Reading), "real survey earned reading");
            bool east = Known(game, "contact.east.known"); c.That(TestKit.Order(game, 0, Forget("forget.reading")).Accepted, "explicit selected survey history forgotten");
            c.That(!Known(game, Reading), "fresh historical row actually removed, not merely made stale");
            c.Equal(Codes.NotFound, Knowledge(game).ReadFreshness(Company, Provider, Reading, out var absent), "no residual freshness/expiry table"); c.That(absent == null, "removed data has no freshness snapshot");
            c.Equal(east, Known(game, "contact.east.known"), "unselected subject preserved");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "forgotten survey does not resurrect on load");
            var refresh = Start(c, game, "survey.reobserve"); c.That(!Known(game, Reading), "partial real survey cannot restore knowledge instantly");
            twin = factory.Load(game.Save()); Reach(game, () => refresh.State == OrderState.Done, "actual physical survey relearning", twin);
            Knowledge(game).TryRead(Company, Provider, Reading, out var newReading); c.That(newReading.ObservedTick > oldReading.ObservedTick, "new positive-time capture after forgetting");
            c.Equal(newReading.ObservedTick, newReading.LearnedTick, "relearned original ledger first time starts anew");
            c.Equal(game.Hash(), twin.Hash(), "actual physical relearning saved twins");
            c.Equal(Fix.FromInt(12), Find(game, "survey.site").Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "forgetting refunds no material and authored reobserve consumes none");
            var isolated = LiveSensorTests.New(factory); c.That(!Known(isolated, Reading), "new session borrows no knowledge");
            var omitted = LiveSensorTests.New(Factory(configured: false)); c.Equal(Codes.NotFound, Knowledge(omitted).PreviewForget("forget.region", out hidden), "no authored profile means no implicit forgetting");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "subjects", "empty", "duplicate", "provider", "subject", "companies", "no-companies", "duplicate-company", "negative", "fraction", "unseated", "extra", "unsupported" })
            {
                var doc = Content.Json.Parse(FrameworkProofContent.Read("native-knowledge-forgetting.json")); var policy = doc.Get("knowledge_forgetting_policies")[0];
                if (mode == "subjects") policy.Remove("subjects"); if (mode == "empty") policy.Set("subjects", JsonValue.NewArray());
                if (mode == "duplicate") policy.Get("subjects").Add(policy.Get("subjects")[0].Clone());
                if (mode == "provider") policy.Get("subjects")[0].Set("provider", JsonValue.Of("missing"));
                if (mode == "subject") policy.Get("subjects")[0].Set("subject", JsonValue.Of("missing"));
                if (mode == "companies") policy.Remove("companies"); if (mode == "no-companies") policy.Set("companies", JsonValue.NewArray());
                if (mode == "duplicate-company") policy.Set("companies", Content.Json.Parse("[0,0]"));
                if (mode == "negative") policy.Set("companies", Content.Json.Parse("[-1]")); if (mode == "fraction") policy.Set("companies", Content.Json.Parse("[0.5]"));
                if (mode == "unseated") policy.Set("companies", Content.Json.Parse("[8]")); if (mode == "extra") policy.Set("force", JsonValue.Of(true));
                if (mode == "unsupported") policy.Get("subjects")[0].Set("provider", JsonValue.Of(TechnologyModule.ModuleId));
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(doc, technology: mode == "unsupported")), "invalid forgetting authoring " + mode);
            }
            var sharing = Content.Json.Parse(FrameworkProofContent.Read("native-knowledge-sharing.json")); var key = sharing.Get("knowledge_sharing_policies")[0].Get("subjects")[0];
            key.Set("provider", JsonValue.Of(RetentionKnowledgeProvider.ProviderId)); key.Set("subject", JsonValue.Of("retention.base"));
            c.Throws<ContentException>(() => LiveSensorTests.New(Factory(sharing: sharing)), "actual independent forgetting provider has no implicit sharing opt-in");
        }
    }
}
