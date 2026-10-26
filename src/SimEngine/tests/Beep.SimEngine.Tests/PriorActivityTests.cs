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
    public static class PriorActivityTests
    {
        public const string Case = "Prior activities: earned compact history, exact scopes and saved physical chains";
        public static void Register() => TestRunner.Add(Case, Run);
        private const string Preparation = "activity:prior.prepare";
        internal static GameFactory Factory(string work = null, PriorActivityProofModule proof = null, string dependencies = null, string proofRequirements = null)
        {
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(),
                new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule() };
            if (proof != null) modules.Add(proof);
            var definition = new GameDefinition("native-prior", modules.ToArray());
            var world = Content.Json.Parse(FrameworkProofContent.Read("native-survey-world.json"));
            world.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("prior.start"));
            var master = Content.Json.Parse(FrameworkProofContent.Read("native-survey-master.json"));
            for (int i = 0; i < master.Get("modules").Count; i++)
            {
                var module = master.Get("modules")[i];
                if (module.Get("id").Text == "sim.work") module.Get("documents").Add(JsonValue.Of("native-prior-work.json"));
                if (module.Get("id").Text == "sim.world-setup") module.Get("documents").Add(JsonValue.Of("native-prior-startup.json"));
                if (proof != null && module.Get("id").Text == "sim.work") module.Get("documents").Add(JsonValue.Of("native-prior-dependencies.json"));
            }
            if (proof != null)
            {
                world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(proof.Id));
                master.Get("modules").Add(Content.Json.Parse("{\"id\":\"proof.prior-activity\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"prior-activity-proof.json\"]}"));
            }
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString())
                .Add("native-survey-world.json", world.ToString()).Add("native-prior-work.json", work ?? FrameworkProofContent.Read("native-prior-work.json"))
                .Add("native-prior-startup.json", FrameworkProofContent.Read("native-prior-startup.json"))
                .Add("native-survey-startup.json", FrameworkProofContent.Read("native-survey-startup.json"))
                .Add("native-survey-navigation.json", FrameworkProofContent.Read("native-survey-navigation.json"))
                .Add("native-survey-work.json", FrameworkProofContent.Read("native-survey-work.json"))
                .Add("native-survey-knowledge.json", FrameworkProofContent.Read("native-survey-knowledge.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            if (proof != null) files.Add("native-prior-dependencies.json", dependencies ?? FrameworkProofContent.Read("native-prior-dependencies.json"))
                .Add("prior-activity-proof.json", proofRequirements ?? FrameworkProofContent.Read("prior-activity-proof.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 733 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Site(SimGame game, int x) => game.World.Entities.All.Single(e => e.Def.Id == "prior.site" && e.Get<ITilePresence>().Tile.X == x);
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("prior time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " orders=" + string.Join(";",
                Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static Order Start(Check c, SimGame game, string plan, EntityId target)
        {
            var command = new PostOrderCommand { Plan = plan, Target = target, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal prior-activity intent preview");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal authored physical order accepted");
            return Board(game).Order((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "compact earned history identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "breaking compact completion payload, no legacy reader");
            var factory = Factory(); var game = New(factory); Entity a = Site(game, 16), b = Site(game, 24);
            ResourceId steel = game.World.Content.ResourceId("steel");
            c.Equal(3, game.World.Entities.All.Count, "native master/startup creates actual two sites and machine");
            c.That(!game.World.Services.Has<IEconomy>(), "prior physical work has no business dependency");
            c.That(ReferenceEquals(Board(game), game.World.Services.Get<IActivityCompletionHistory>()), "same sole board owns completion evidence");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "compact facts use board codec only");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.Systems.OfType<WorkSystem>().Single().StateSchemaVersion, "work system codec has no new history owner");
            c.That(game.World.Content.Get<PriorActivityRequirementDef>("prior.target-ready").AuthoringPath.Contains("native-prior-work.json"), "actual master-listed requirement origin");
            var criteria = new List<WorkRequirementView>();
            ulong before = game.Hash();
            c.That(!View(game).TryReadActivityCompletion(Preparation, a.Id, out _) && !View(game).TryReadActivityCompletion(Preparation, EntityId.None, out _), "startup grants no target/company completion");
            c.Equal(Code.None, View(game).PreviewOrderRequirements("prior.target", a.Id, criteria), "valid intent has pending prior-work facts");
            c.Equal(new Code("prior.target-unprepared"), criteria.Single().Result.Blocker, "authored typed prerequisite blocker");
            c.Equal(before, game.Hash(), "history reads and previews are pure");
            Order dependent = Start(c, game, "prior.target", a.Id);
            c.Equal(0, dependent.Node("inspect").JobId, "unearned prerequisite acquires no worker");
            Order prepare = Start(c, game, "prior.prepare", a.Id);
            Reach(game, () => prepare.Node("prepare").Progress.WholeTicks > 0, "actual preparation travel/material/work");
            c.Equal(Fix.FromInt(24), a.Get<IInventory>().Amount(steel), "real preparation spends eight physical materials");
            c.That(!View(game).TryReadActivityCompletion(Preparation, a.Id, out _), "partial physical work earns no prior-activity fact");
            c.Equal(0L, dependent.Node("inspect").Progress.WholeTicks, "dependent does not fake prerequisite completion");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial prerequisite and blocked dependent checkpoint");
            Reach(game, () => prepare.State == OrderState.Done, "earned preparation completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved actual prerequisite work completes identically");
            c.That(Board(game).Order(prepare.Id) == null, "actual terminal preparation order is pruned");
            c.That(View(game).TryReadActivityCompletion(Preparation, a.Id, out var first), "compact earned evidence survives ordinary pruning");
            c.Equal(prepare.Id, first.Order, "fact retains exact earned execution identity");
            c.Equal(a.Id, first.Target, "fact retains actual target generation");
            c.Equal(prepare.CreatedTick, first.CreatedTick, "fact retains actual order creation time");
            c.Equal(prepare.CompletedTick, first.CompletedTick, "fact retains actual completion, not pruning-read time");
            c.That(first.CompletedTick >= first.CreatedTick && first.CompletedTick <= game.Tick, "one actual clock provides history chronology");
            c.That(!View(game, 1).TryReadActivityCompletion(Preparation, a.Id, out _), "foreign seat receives no private completion identity");
            c.That(!game.World.Services.Get<IActivityCompletionHistory>().TryReadCompletion(new PlayerId(-2), Preparation, EntityId.None, out _), "invalid viewer does not become observer");
            before = game.Hash();
            c.That(!View(game).TryReadActivityCompletion(Preparation, b.Id, out _), "same prototype at other target borrows no completion");
            c.Equal(Code.None, View(game).PreviewOrderRequirements("prior.target", b.Id, criteria), "other actual target has valid intent");
            c.Equal(new Code("prior.target-unprepared"), criteria.Single().Result.Blocker, "explicit target scope stays blocked");
            c.Equal(Code.None, View(game).PreviewOrderRequirements("prior.company", b.Id, criteria), "explicit company scope preview");
            c.Equal(WorkRequirementStatus.Satisfied, criteria.Single().Result.Status, "company scope deliberately admits earned work on another site");
            c.That(View(game).TryReadActivityCompletion(Preparation, EntityId.None, out var company) && company.Order == first.Order, "private company-wide read chooses same actual fact");
            c.Equal(before, game.Hash(), "compacted scope reads/diagnostics allocate no state");
            twin = factory.Load(game.Save());
            Reach(game, () => dependent.State == OrderState.Done, "physical dependent work after pruning", twin);
            c.Equal(game.Hash(), twin.Hash(), "compacted prerequisite drives saved dependent identically");
            c.That(dependent.Node("inspect").Progress.WholeTicks > 0 && dependent.Node("inspect").CompletedBy.IsValid, "dependent executes real machine work");
            c.Equal(Fix.FromInt(24), a.Get<IInventory>().Amount(steel), "prior readiness does not copy or refund stock");
            Order companyWork = Start(c, game, "prior.company", b.Id);
            twin = factory.Load(game.Save()); Reach(game, () => companyWork.State == OrderState.Done, "company-scoped physical travel to other site", twin);
            c.Equal(game.Hash(), twin.Hash(), "company-scoped workflow saves and completes physically");
            c.That(companyWork.Node("inspect").CompletedBy.IsValid, "company knowledge does not teleport an executing machine");
            c.That(!View(game).TryReadActivityCompletion(Preparation, b.Id, out _), "different activity completion cannot fabricate target preparation");
            Order cancelled = Start(c, game, "prior.prepare", a.Id);
            Reach(game, () => cancelled.Node("prepare").Progress.WholeTicks > 0, "partial repeat preparation");
            c.Equal(Fix.FromInt(16), a.Get<IInventory>().Amount(steel), "cancelled repeat spent actual own materials");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancelled.Id }).Accepted, "normal partial prerequisite cancellation");
            c.That(View(game).TryReadActivityCompletion(Preparation, a.Id, out var afterCancel) && afterCancel.Order == first.Order, "cancelled partial execution earns no newer evidence");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled execution preserves compact history without refund");
            Order repeated = Start(c, game, "prior.prepare", a.Id);
            twin = factory.Load(game.Save()); Reach(game, () => repeated.State == OrderState.Done, "second earned preparation generation", twin);
            c.Equal(game.Hash(), twin.Hash(), "repeat and compact replacement continue identically");
            c.Equal(Fix.FromInt(8), a.Get<IInventory>().Amount(steel), "three actual attempts consume once each, no completion-store inventory");
            c.That(View(game).TryReadActivityCompletion(Preparation, a.Id, out var latest) && latest.Order == repeated.Id, "latest same-target fact replaces earlier compact execution");
            c.That(latest.CompletedTick > first.CompletedTick && latest.CreatedTick > first.CreatedTick, "repeated completion has actual new chronology");
            c.Equal(prepare.Id, first.Order, "held immutable fact never changes to latest execution");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final compact facts checkpoint canonically");
            var isolated = New(factory);
            c.That(!View(isolated).TryReadActivityCompletion(Preparation, EntityId.None, out _), "interleaved session borrows no completion facts");
            c.Equal(Fix.FromInt(32), Site(isolated, 16).Get<IInventory>().Amount(steel), "interleaved startup has its own authored stock");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            string source = FrameworkProofContent.Read("native-prior-work.json");
            foreach (string mode in new[] { "missing", "unselected", "scope", "absent-scope", "blocker", "extra" })
            {
                var json = Content.Json.Parse(source); var leaf = json.Get("prior_activity_requirements")[0];
                if (mode == "missing") leaf.Set("plan", JsonValue.Of("activity:missing"));
                if (mode == "unselected") leaf.Set("plan", JsonValue.Of("build:missing-provider"));
                if (mode == "scope") leaf.Set("scope", JsonValue.Of("prototype"));
                if (mode == "absent-scope") leaf.Remove("scope");
                if (mode == "blocker") leaf.Set("blocker", JsonValue.Of(" "));
                if (mode == "extra") leaf.Set("completed", JsonValue.Of(true));
                c.Throws<ContentException>(() => New(Factory(json.ToString())), "authored prior requirement refuses " + mode);
            }
        }
    }
}
