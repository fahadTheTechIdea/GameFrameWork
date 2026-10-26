using System;
using System.Collections.Generic;
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
    public static class PressureSignalTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Leak = new Code("pressure.leak"), Dial = new Code("risk-dial"), Feed = new Code("feed");
        private const string Source = "pressure.plant-leak";
        public static void Register()
            => TestRunner.Add("Scenario pressure: typed plant conditions, private candidates and physical recovery", Run);
        private static GameFactory Factory(string profile = "pressure.plant", string json = null, string visibility = null, Action<JsonValue> edit = null)
            => ConstructionPlanTests.Points(extraModules: new GameModule[] { new ScenarioPressureProofModule(), new InventorySignalsModule(), new KnowledgeVisibilityModule() },
                extraDeclarations: "[{\"id\":\"sim.scenario-pressure\",\"version\":\"1.0.0\",\"documents\":[\"pressure-signals.json\"]},{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"pressure-visibility.json\"]}]",
                extraDocuments: new Dictionary<string, string> {
                    ["pressure-signals.json"] = json ?? FrameworkProofContent.Read("pressure-signals.json"),
                    ["pressure-visibility.json"] = FrameworkProofContent.Read("pressure-visibility.json") },
                additionalDocuments: new Dictionary<string, string> { ["pressure-response.json"] = FrameworkProofContent.Read("pressure-response.json") },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(ScenarioPressureModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(InventorySignalsModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(IncidentResponseModule.ModuleId));
                    scenario.Get("settings").Set(ScenarioPressureModule.ModuleId, Content.Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"" + profile + "\"}]}"));
                    if (visibility != null) {
                        scenario.Get("modules").Add(JsonValue.Of(KnowledgeVisibilityModule.ModuleId));
                        scenario.Get("settings").Set(KnowledgeVisibilityModule.ModuleId, Content.Json.Parse("{\"policy\":\"" + visibility + "\"}"));
                    }
                    edit?.Invoke(scenario);
                }, startupProfiles: new[] { "pressure-signals.start", "pressure-response.start" });
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static List<Entity> Targets(SimGame game, int company = 0)
            => game.World.Entities.All.Where(e => e.Alive && e.Owner == new PlayerId(company) && e.Def.Id == "pressure.signal-machine").OrderBy(e => e.Id).ToList();
        private static ScenarioPressureSnapshot Facts(SimGame game)
        { var facts = new List<ScenarioPressureSnapshot>(); game.Seat(0).View.Feature<ScenarioPressureView>().CopyPressure(facts); return facts.Single(); }
        private static List<PressureCandidateSnapshot> Candidates(SimGame game, string source = Source)
        { var facts = new List<PressureCandidateSnapshot>(); if (!game.Seat(0).View.Feature<ScenarioPressureView>().CopyCandidates(Company, source, facts)) throw new InvalidOperationException("missing enrolled source " + source); return facts; }
        private static bool Active(SimGame game, EntityId entity)
        { game.Seat(0).View.Feature<IncidentsView>().TryGetIncident(entity, Leak, out var facts); return facts.Active; }
        private static void SetRisk(Check c, SimGame game, EntityId entity, bool enabled, int company = 0)
            => c.That(TestKit.Order(game, company, new ConfigureOperationCommand { Entity = entity, Operation = Dial, Enabled = enabled, Target = Fix.Zero }).Accepted,
                "player changes actual public-provider operation with native command");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 400 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " pressure=" + Facts(game).Blocker + "/" + Facts(game).PendingSequence +
                " candidates=" + string.Join(";", Candidates(game).Select(f => f.Entity + "/" + f.Blocker)) +
                " orders=" + string.Join(";", Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "signal-conditioned source has exact current semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no implicit old pressure JSON/save reader");
            Physical(c); PrivateAndUnavailable(c); InvalidContent(c);
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); var targets = Targets(game);
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessAccounts>(), "typed pressure and physical response require no business module");
            c.That(game.World.Content.Get<IncidentPressureSourceDef>(Source).AuthoringPath.Contains("pressure-signals.json"), "conditions come from explicit master-listed content");
            var retained = Candidates(game); ulong hash = game.Hash();
            c.That(retained.Count == 2 && retained.All(f => !f.Ready && f.Blocker.Id == "pressure.safe-operation"), "safe operations block only actual own candidates");
            c.That(retained.All(f => f.Conditions.Count == 2 && f.Conditions[0].Id == "risk" && f.Conditions[1].Id == "stock" && f.Conditions[1].Ready && f.Conditions[1].Signal.Value == Fix.FromInt(20)),
                "all conditions are complete, stable and exactly typed actual inventory quantities");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateConditionSnapshot>)retained[0].Conditions).Clear(), "retained candidate conditions are immutable");
            c.Equal(hash, game.Hash(), "candidate reads do not reserve points, submit or change chronology");
            Tick(game);
            c.That(Facts(game).PendingSequence == 0 && Facts(game).Available == Fix.FromInt(2) && Facts(game).Spent == Fix.Zero && Facts(game).Blocker.Id == "pressure.safe-operation", "closed conditions wait without pressure spending");
            SetRisk(c, game, Targets(game, 1).Single().Id, true, 1);
            c.That(Facts(game).PendingSequence == 0 && Candidates(game).Count == 2, "foreign enabled signal cannot supply an own target condition");
            SetRisk(c, game, targets[1].Id, true);
            var pending = Facts(game);
            c.That(pending.PendingSequence > 0 && pending.Reserved == Fix.One && pending.Spent == Fix.Zero && !Active(game, targets[1].Id), "ready plant reserves ordinary next-Update incident rather than instant mutation");
            var ready = Candidates(game);
            c.That(!ready[0].Ready && ready[1].Ready && retained.All(f => !f.Ready), "ready later target bypasses blocked lower identity; retained facts remain detached");
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(Active(game, targets[1].Id) && !Active(game, targets[0].Id) && Facts(game).Accepted == 1 && Facts(game).Spent == Fix.One && game.Hash() == twin.Hash(), "pending typed selection restores and spends only its actual ordinary acceptance");
            var results = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Any(r => r.Sequence == pending.PendingSequence && r.Accepted && r.Entity == targets[1].Id && r.Command == "sim.raise-incident"), "candidate identity agrees with actual native result");
            Configure(c, game); twin = factory.Load(game.Save());
            Reach(game, () => Board(game).Orders.Any(o => o.Target == targets[1].Id && o.Node("repair").Progress.WholeTicks > 0), "physical conditioned repair", twin);
            c.That(Find(game, "machine").Get<PointPresence>().Position == Fix.FromInt(20) && Active(game, targets[1].Id), "actual worker reaches target and repairs over time");
            c.Equal(Fix.FromInt(12), targets[1].Get<IInventory>().Amount(Supply(game)), "actual response consumes eight supply, crossing authored risk stock band");
            twin = factory.Load(game.Save()); Reach(game, () => !Active(game, targets[1].Id), "response completion", twin);
            c.That(game.Hash() == twin.Hash() && Candidates(game).Single(f => f.Entity == targets[1].Id).Blocker.Id == "pressure.stock-band", "completed response changes same engine candidate evaluator; partial save agrees");
            while (game.Tick <= Facts(game).CooldownUntil) Tick(game);
            c.That(Facts(game).Accepted == 1 && Facts(game).PendingSequence == 0 && Facts(game).Available == Fix.One, "remaining point cannot cause another incident on safe/depleted plants");
            SetRisk(c, game, targets[0].Id, true);
            c.That(Facts(game).PendingSequence > 0 && Facts(game).Reserved == Fix.One && !Active(game, targets[0].Id), "player opens first plant condition after cooldown with same remaining budget");
            twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(Active(game, targets[0].Id) && !Active(game, targets[1].Id) && Facts(game).Accepted == 2 && Facts(game).Spent == Fix.FromInt(2) && game.Hash() == twin.Hash(), "new actual ready target selected without extra stock, money, scheduler or clock");
        }
        private static void PrivateAndUnavailable(Check c)
        {
            SimGame game = New(Factory()); EntityId target = Targets(game)[0].Id; var destination = Candidates(game);
            c.That(!game.Seat(1).View.Feature<ScenarioPressureView>().CopyCandidates(Company, Source, destination) && destination.Count == 0, "foreign viewer cannot expose pressure candidates");
            IScenarioPressure pressure = game.World.Services.Get<IScenarioPressure>();
            c.That(!pressure.CopyCandidates(new PlayerId(-2), Company, Source, destination) && destination.Count == 0, "malformed negative viewer is not observer");
            c.That(!pressure.CopyCandidates(new PlayerId(8), Company, Source, destination) && destination.Count == 0, "nonseat has no candidates");
            c.That(!game.Seat(0).View.Feature<ScenarioPressureView>().CopyCandidates(Company, "pressure.missing-threshold", destination) && destination.Count == 0, "source must belong to actual enrolled profile");
            c.That(game.Observer.Feature<ScenarioPressureView>().CopyCandidates(Company, Source, destination) && destination.Count == 2, "exact observer explicitly reads the company's PRIVATE-context candidates");
            foreach (int viewer in new[] { -2, 8 })
                c.Equal(Codes.NotSeated, new OperationsView(game.World, new PlayerId(viewer)).ReadSignal(target, Feed, "pressure.stock", out _), "invalid direct signal viewer refuses before provider: " + viewer);
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<OperationsView>().ReadSignal(target, Feed, "pressure.stock", out _), "visible foreign stock stays private");
            SimGame hidden = New(Factory(visibility: "pressure.hide-entities")); EntityId hiddenTarget = Targets(hidden)[0].Id;
            c.Equal(Codes.NotFound, hidden.Seat(0).View.Feature<OperationsView>().ReadSignal(hiddenTarget, Feed, "pressure.stock", out _), "hidden owned entity cannot leak through signal reader");
            c.Equal(0, Candidates(hidden).Count, "pressure uses existing entity visibility, not actual-world enumeration");
            Tick(hidden); c.That(Facts(hidden).Blocker == PressureCodes.NoTarget && Facts(hidden).Spent == Fix.Zero, "hidden plant causes no decision grant");
            SimGame hiddenSignal = New(Factory(visibility: "pressure.hide-signals"));
            c.Equal(Codes.UnknownDef, hiddenSignal.Seat(0).View.Feature<OperationsView>().ReadSignal(Targets(hiddenSignal)[0].Id, Feed, "pressure.stock", out _), "hidden signal definition is not a substitute numeric value");
            c.That(Candidates(hiddenSignal).All(f => f.Conditions.All(v => !v.Signal.Available && v.Blocker == Codes.UnknownDef)), "private candidate evaluator retains each unavailable signal blocker");
            GameFactory unavailableFactory = Factory("pressure.unavailable"); SimGame unavailable = New(unavailableFactory);
            var unavailableFacts = Candidates(unavailable, "pressure.missing-threshold");
            c.That(unavailableFacts.All(f => f.Blocker == InventorySignal.NoThreshold && f.Conditions[0].Signal.Value == null), "explicit numeric-only inventory reports missing Boolean threshold, never false/zero");
            Tick(unavailable); SimGame twin = unavailableFactory.Load(unavailable.Save()); Tick(unavailable); Tick(twin);
            c.That(Facts(unavailable).Blocker == InventorySignal.NoThreshold && Facts(unavailable).Reserved == Fix.Zero && Facts(unavailable).Available == Fix.FromInt(2) && unavailable.Hash() == twin.Hash(), "typed unavailable wait is saved, unspent and deterministic");
        }
        private static void InvalidContent(Check c)
        {
            foreach (string fault in new[] { "missing", "duplicate", "signal", "source", "kind", "unit", "range" })
            {
                JsonValue json = Content.Json.Parse(FrameworkProofContent.Read("pressure-signals.json"));
                JsonValue source = json.Get("incident_pressure_sources")[0], conditions = source.Get("conditions"), condition = conditions[0];
                if (fault == "missing") source.Remove("conditions");
                if (fault == "duplicate") conditions[1].Set("id", JsonValue.Of("stock"));
                if (fault == "signal") condition.Set("signal", JsonValue.Of("missing"));
                if (fault == "source") condition.Set("source", JsonValue.Of("missing"));
                if (fault == "kind") condition.Set("expected", JsonValue.Of(true));
                if (fault == "unit") condition.Set("unit", JsonValue.Of("unit.boolean"));
                if (fault == "range") condition.Set("minimum", JsonValue.Number("41"));
                c.Throws<ContentException>(() => New(Factory(json: json.ToString())), "invalid pressure condition refuses before Initialize: " + fault);
            }
        }
    }
}
