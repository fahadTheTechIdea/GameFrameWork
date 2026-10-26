using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class PlaybookDependencyTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        public static void Register()
            => TestRunner.Add("Player controllers: conditional dependency playbooks and physical independent branches", Run);
        private static GameFactory Factory(string json = null, string conditions = null)
            => ConstructionPlanTests.Points(extraModules: new GameModule[] { new PlayerControllerProofModule(), new InventorySignalsModule(), new GameConditionProofModule() },
                extraDeclarations: "[{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[\"playbook-branches.json\"]},{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.conditions\",\"version\":\"1.0.0\",\"documents\":[\"playbook-conditions.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["playbook-branches.json"] = json ?? FrameworkProofContent.Read("playbook-branches.json"), ["playbook-conditions.json"] = conditions ?? FrameworkProofContent.Read("playbook-conditions.json") },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(PlayerControllersModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(InventorySignalsModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(GameConditionsModule.ModuleId));
                    scenario.Get("settings").Set(PlayerControllersModule.ModuleId, Content.Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"strategy.branches\"}]}"));
                }, startupProfiles: new[] { "playbook-branches.start" });
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static List<PlaybookStepSnapshot> Steps(SimGame game)
        { var values = new List<PlaybookStepSnapshot>(); if (!game.Seat(0).View.Feature<PlayerControllersView>().CopyPlaybook(Company, values)) throw new InvalidOperationException("missing playbook diagnostics"); return values; }
        private static PlaybookStepSnapshot Step(SimGame game, string id) => Steps(game).Single(s => s.Id == id);
        private static PlayerControllerFacts Facts(SimGame game)
        { var values = new List<PlayerControllerFacts>(); game.Seat(0).View.Feature<PlayerControllersView>().CopyControllers(values); return values.Single(); }
        private static List<PlanView> Plans(SimGame game)
        { var values = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(values); return values; }
        private static Entity Beacon(SimGame game, int company = 0)
            => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == "strategy.beacon" && e.Owner == new PlayerId(company));
        private static void Switch(Check c, SimGame game, Controller selection)
            => c.That(TestKit.Order(game, 0, new SetControllerCommand { Controller = selection }).Accepted, "player changes native controller selection");
        private static void Risk(Check c, SimGame game, int company = 0)
            => c.That(TestKit.Order(game, company, new ConfigureOperationCommand { Entity = Beacon(game, company).Id, Operation = new Code("risk-dial"), Enabled = true, Target = Fix.Zero }).Accepted,
                "player changes actual signal provider with ordinary operation command");
        private static Fix Stock(SimGame game)
        {
            Fix value = Fix.Zero;
            foreach (var entity in game.World.Entities.All)
                if (entity.Alive && entity.Owner == Company && entity.Get<IInletPort>() is IInletPort inventory) value += inventory.Amount(Supply(game));
            return value;
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1000 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " controller=" + Facts(game).Blocker +
                " steps=" + string.Join(";", Steps(game).Select(s => s.Id + "/" + s.Status + "/" + s.Blocker + "/" + s.FirstPlan)) +
                " plans=" + string.Join(";", Plans(game).Select(p => p.Id + "/" + p.State + "/" + p.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current dependency/condition strategy semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no old positional playbook reader");
            Physical(c); Cancellation(c); RefusalsAndAuthoring(c); Codec(c);
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); var initial = Steps(game); ulong hash = game.Hash();
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessAccounts>(), "strategy/construction use materials and time, not Tycoon");
            c.That(game.World.Content.Get<PlanPlaybookDef>("strategy.branches").AuthoringPath.Contains("playbook-branches.json"), "master-listed graph content is authoritative");
            c.That(initial.Select(s => s.Id).SequenceEqual(new[] { "a.root", "b.root", "child", "risk" }), "stable IDs, not source order, own strategy records");
            c.That(initial[0].Status == PlaybookStepStatus.Ready && initial[1].Status == PlaybookStepStatus.Ready && initial[2].Status == PlaybookStepStatus.Waiting,
                "both independent roots ready while two-prerequisite child waits");
            c.That(initial[3].Status == PlaybookStepStatus.Blocked && initial[3].Blocker.Id == "controller.risk-disabled", "higher-priority unsafe branch does not block independent expansion");
            c.Throws<NotSupportedException>(() => ((IList<string>)initial[2].Dependencies).Clear(), "retained dependency facts are immutable");
            c.Equal(hash, game.Hash(), "all playbook diagnostics/native admission/conditions are pure");
            c.Equal(Fix.FromInt(52), Stock(game), "actual own starting supplies are authored, not strategy grants");
            Risk(c, game, 1); c.Equal("controller.risk-disabled", Step(game, "risk").Blocker.Id, "foreign ready signal cannot satisfy private own condition");
            Configure(c, game); Switch(c, game, Controller.Autopilot);
            c.That(Facts(game).AwaitingCommands == 2 && Plans(game).Count == 0 && Step(game, "a.root").PendingCommand < Step(game, "b.root").PendingCommand,
                "same authored command budget submits both stable tie-ordered independent roots for NEXT Update");
            c.That(initial.All(s => s.PendingCommand == 0 && s.FirstPlan == 0), "retained intent facts do not mutate with provider state");
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(Plans(game).Count == 2 && Plans(game).All(p => p.State == PlanState.Active) && game.Hash() == twin.Hash(), "saved ordinary submissions create two real projects, not completed structures");
            c.That(Step(game, "a.root").FirstPlan > 0 && Step(game, "b.root").FirstPlan > Step(game, "a.root").FirstPlan && Step(game, "child").FirstPlan == 0,
                "accepted identities bind their actual nodes; no child starts on submission/acceptance alone");
            Risk(c, game); twin = factory.Load(game.Save());
            Reach(game, () => Step(game, "risk").FirstPlan > 0, "conditioned independent branch acceptance", twin);
            c.That(Step(game, "a.root").Status != PlaybookStepStatus.Done && Step(game, "risk").Status == PlaybookStepStatus.Executing && Step(game, "child").Status == PlaybookStepStatus.Waiting,
                "private signal/public elapsed condition starts independent high-priority branch while roots are physically unfinished");
            c.That(game.World.Content.Get<ElapsedGameConditionDef>("strategy.wait") is IGameCondition, "public content condition works without scheduler/provider type branch");
            Reach(game, () => Board(game).Orders.Any(o => o.Node("build")?.Progress.WholeTicks > 0), "productive real construction", twin);
            c.That(Find(game, "machine").Get<PointPresence>().Position > Fix.Zero && Stock(game) < Fix.FromInt(52), "worker travel and actual material consumption drive physical progress");
            Switch(c, game, Controller.Human); long decisions = Facts(game).Decisions; twin = factory.Load(game.Save());
            for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); }
            c.That(Facts(game).Decisions == decisions && game.Hash() == twin.Hash(), "human handover preserves graph links while engine physical work continues");
            Switch(c, game, Controller.Autopilot); twin = factory.Load(game.Save());
            Reach(game, () => Step(game, "child").FirstPlan > 0, "two actual root completions unlock child", twin);
            c.That(Step(game, "a.root").Status == PlaybookStepStatus.Done && Step(game, "b.root").Status == PlaybookStepStatus.Done,
                "child dispatch waits for BOTH real native prerequisite completions");
            twin = factory.Load(game.Save()); Reach(game, () => Steps(game).All(s => s.Status == PlaybookStepStatus.Done), "all physical branches", twin);
            c.That(game.Hash() == twin.Hash() && Plans(game).Count == 4 && Plans(game).All(p => p.State == PlanState.Done), "native graph/hauling/construction saved continuation is identical");
            c.Equal(4, game.World.Entities.All.Count(e => e.Alive && e.Def.Id == "point.tower" && e.Owner == Company), "native work alone creates four real structures");
            c.Equal(Fix.FromInt(20), Stock(game), "four real buildings consume exactly32 supply from52, no free replenishment");
            c.Equal(Fix.FromInt(32), Beacon(game, 1).Get<IInventory>().Amount(Supply(game)), "foreign stock remains untouched by private strategic selection");
            for (int i = 0; i < 7; i++) { Tick(game); Tick(twin); }
            c.That(Plans(game).Count == 4 && Facts(game).AwaitingCommands == 0 && Facts(game).Blocker.IsNone && game.Hash() == twin.Hash(), "finished strategy retains native links, never resubmits or copies progress");
            c.That(Steps(New(factory)).All(s => s.FirstPlan == 0 && s.PendingCommand == 0), "shared content does not share strategic execution between sessions");
        }
        private static void Cancellation(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Configure(c, game); Switch(c, game, Controller.Autopilot); Tick(game);
            long first = Step(game, "a.root").FirstPlan;
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { first }) }).Accepted, "player cancels the actual linked native root");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => Step(game, "b.root").Status == PlaybookStepStatus.Done, "independent root after cancellation", twin);
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.That(Step(game, "a.root").Status == PlaybookStepStatus.Blocked && Step(game, "child").Status == PlaybookStepStatus.Waiting && Step(game, "child").FirstPlan == 0,
                "cancelled prerequisite blocks only descendants, not independent physically supplied root");
            c.That(Plans(game).Count == 2 && game.Hash() == twin.Hash(), "saved cancellation preserves real links and never silently restarts or substitutes another plan");
        }
        private static void Codec(Check c)
        {
            // Explicit isolated codec instrumentation; never player gameplay or fabricated live execution.
            SimGame game = New(Factory()); var definition = game.World.Content.Get<PlanPlaybookDef>("strategy.branches");
            var provider = (IPlayerControllerState)definition.Create(); c.Equal(3, provider.StateSchemaVersion, "one native per-node codec replaces cursor layout");
            foreach (string fault in new[] { "count", "identity", "negative", "dual", "ticket", "overlap" })
            {
                var before = new StateHasher(); provider.Sync(before); byte[] bytes;
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); writer.Count(fault == "count" ? 3 : 4);
                    foreach (string original in new[] { "a.root", "b.root", "child", "risk" })
                    {
                        string id = fault == "identity" && original == "a.root" ? "unknown" : original;
                        int pending = fault == "negative" && original == "a.root" ? -1 :
                            fault == "dual" && original == "a.root" || fault == "ticket" && (original == "a.root" || original == "b.root") ? 1 : 0;
                        long first = fault == "dual" && original == "a.root" || fault == "overlap" && (original == "a.root" || original == "b.root") ? 1 : 0;
                        writer.String(ref id); writer.Int(ref pending); writer.Long(ref first);
                    }
                    writer.Flush(); bytes = stream.ToArray();
                }
                c.Throws<CorruptStateException>(() => provider.Sync(new StateReader(bytes, 0, bytes.Length)), "native node codec refuses malformed " + fault);
                var after = new StateHasher(); provider.Sync(after); c.Equal(before.Value, after.Value, "failed node decode preserves provider state: " + fault);
            }
        }
        private static void RefusalsAndAuthoring(Check c)
        {
            SimGame game = New(Factory()); var destination = Steps(game); IPlayerControllers controllers = game.World.Services.Get<IPlayerControllers>();
            c.That(!game.Seat(1).View.Feature<PlayerControllersView>().CopyPlaybook(Company, destination) && destination.Count == 0, "foreign controller graph is private and clears output");
            foreach (int viewer in new[] { -2, 8 })
            {
                c.That(!controllers.CopyPlaybook(new PlayerId(viewer), Company, destination) && destination.Count == 0, "invalid/nonseat is not graph observer: " + viewer);
                var facts = new List<PlayerControllerFacts>(); controllers.CopyControllers(new PlayerId(viewer), facts); c.Equal(0, facts.Count, "same exact viewer policy protects controller cadence: " + viewer);
            }
            c.That(game.Observer.Feature<PlayerControllersView>().CopyPlaybook(Company, destination) && destination.Count == 4, "exact observer explicitly reads own-company context diagnostics");
            var selection = new SetControllerCommand { Controller = Controller.Autopilot };
            c.Equal(Codes.NotFound, game.Seat(1).Preview(selection), "unbound rival has same ordinary missing-profile preview");
            c.Equal(Codes.NotFound, TestKit.Order(game, 1, selection).Refusal, "ordinary command refusal matches player/rival preview, no implicit controller");
            foreach (string fault in new[] { "after", "priority", "conditions", "missing-edge", "cycle", "duplicate", "condition", "mode", "signal" })
            {
                JsonValue json = Content.Json.Parse(FrameworkProofContent.Read("playbook-branches.json")); var rows = json.Get("plan_playbooks")[0].Get("steps");
                JsonValue conditions = Content.Json.Parse(FrameworkProofContent.Read("playbook-conditions.json"));
                if (fault == "after" || fault == "priority" || fault == "conditions") rows[0].Remove(fault);
                if (fault == "missing-edge") rows[0].Set("after", Content.Json.Parse("[\"missing\"]"));
                if (fault == "cycle") { rows[1].Set("after", Content.Json.Parse("[\"child\"]")); }
                if (fault == "duplicate") rows[1].Set("id", JsonValue.Of("a.root"));
                if (fault == "condition") rows[0].Set("conditions", Content.Json.Parse("[\"missing\"]"));
                if (fault == "mode") conditions.Get("signal_conditions")[0].Set("mode", JsonValue.Of("secret"));
                if (fault == "signal") conditions.Get("signal_conditions")[0].Get("conditions")[0].Set("signal", JsonValue.Of("missing"));
                c.Throws<ContentException>(() => New(Factory(json.ToString(), conditions.ToString())), "invalid strategy graph/condition refuses before gameplay: " + fault);
            }
            JsonValue blocked = Content.Json.Parse(FrameworkProofContent.Read("playbook-conditions.json"));
            blocked.Get("signal_conditions")[1].Get("conditions")[0].Set("minimum", JsonValue.Number("33"));
            SimGame waiting = New(Factory(conditions: blocked.ToString())); Configure(c, waiting); Switch(c, waiting, Controller.Autopilot);
            c.That(Plans(waiting).Count == 0 && Step(waiting, "a.root").Blocker.Id == "controller.reserve" && Step(waiting, "b.root").Blocker.Id == "controller.reserve",
                "authored insufficient native reserve is a typed strategic wait, not grant/instant fallback");
            c.Equal(Fix.FromInt(52), Stock(waiting), "blocked strategy consumes no physical resources");
        }
    }
}
