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

namespace Beep.SimEngine.Tests
{
    // Authored master/documents and public player observations only. No payment/production implementation.
    internal static class CancellationCostDurabilityTests
    {
        internal const string Case = "Cancellation cost durability: optional money every physical phase and queue account faults";
        private static readonly Code Pulse = new Code("pulse");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static string Mode(bool money, bool costs) => !money ? "cancellation.resources" : costs ? "cancellation.paid" : "cancellation.accounts";
        private static void Select(JsonValue root, bool money, bool costs)
        {
            var scenarios = root.Get("scenarios"); if (scenarios == null) return;
            for (int i = 0; i < scenarios.Count; i++)
            {
                var scenario = scenarios[i]; scenario.Set("game_mode", JsonValue.Of(Mode(money, costs)));
                if (!money) continue;
                scenario.Get("modules").Add(JsonValue.Of(TycoonModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                scenario.Get("settings").Set(TycoonModule.ModuleId, Json.Parse("{\"account_policy\":\"proof.cancellation.accounts\"}"));
            }
        }
        private static Dictionary<string, string> MoneyDocuments() => new Dictionary<string, string>
        {
            { "game-mode-properties.json", FrameworkProofContent.Read("game-mode-properties.json") },
            { "cancellation-cost-modes.json", FrameworkProofContent.Read("cancellation-cost-modes.json") },
            { "cancellation-cost-accounts.json", FrameworkProofContent.Read("cancellation-cost-accounts.json") }
        };
        private const string Declaration = "{\"id\":\"sim.tycoon\",\"version\":\"1.0.0\",\"documents\":[\"cancellation-cost-accounts.json\"]}";
        private static void SharedModes(JsonValue master, string owner)
        {
            var modules = master.Get("modules"); bool found = false;
            for (int i = 0; i < modules.Count; i++) if (modules[i].Get("id").Text == owner)
            {
                modules[i].Get("documents").Add(JsonValue.Of("game-mode-properties.json"));
                modules[i].Get("documents").Add(JsonValue.Of("cancellation-cost-modes.json")); found = true;
            }
            if (!found) throw new InvalidOperationException("missing actual shared configuration owner " + owner);
        }
        private static GameFactory Native(bool money, bool costs)
        {
            var documents = MoneyDocuments();
            var economics = Json.Parse(documents["cancellation-cost-accounts.json"]);
            var bindings = JsonValue.NewArray();
            foreach (string name in new[] { "planet-x-world.json", "production-blockers.json", "production-cancellation.json" })
            {
                var root = Json.Parse(FrameworkProofContent.Read(name)); Select(root, money, costs);
                var recipes = root.Get("transformations");
                if (recipes != null) for (int i = 0; i < recipes.Count; i++)
                    bindings.Add(Json.Parse("{\"id\":\"cancellation.cost." + recipes[i].Get("id").Text + "\",\"transformation\":\"" + recipes[i].Get("id").Text + "\",\"amount\":7}"));
                documents.Add(name, root.ToString());
            }
            economics.Set("transformation_cost_bindings", bindings); documents["cancellation-cost-accounts.json"] = economics.ToString();
            return ProductionBlockerTests.Factory("production-cancellation-master.json", "production-cancellation.json",
                root => { SharedModes(root, "points"); root.Get("modules").Add(Json.Parse(Declaration)); }, documents, new TycoonModule());
        }
        private static void Account(Check c, SimGame g, bool money, int expected)
        {
            ulong hash = g.Hash(); var view = g.Seat(0).View.Feature<BusinessView>();
            if (!money)
                c.That(view == null && !g.Modules.Contains(TycoonModule.ModuleId), "resource-only cancellation composes no account or fake ledger");
            else
            {
                c.Equal(Code.None, view.ReadAccount(new PlayerId(0), out var account), "original immutable account facts");
                c.Equal(Fix.FromInt(expected), account.Balance, "only authored productive startup changes money");
                c.That(account.BudgetReservations == 0 && account.Reserved == Fix.Zero && account.GenerationSettlements == 0,
                    "no invented payment hold or income accompanies cancellation");
                c.Equal(Codes.NotOwned, g.Seat(1).View.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var hidden), "foreign account remains private");
                c.That(hidden.Policy == null, "foreign account read publishes no prefix");
            }
            c.Equal(hash, g.Hash(), "monetary observations preserve actual state and random stream");
        }
        internal static void Native(Check c, bool money, bool costs)
        {
            var f = Native(money, costs); var stages = new HashSet<string>(StringComparer.Ordinal);
            ProductionCancellationTests.Physical(c, f, (check, g, phase) =>
            {
                stages.Add(phase); int expected = 20;
                if (costs)
                    expected = phase == "working" || phase == "returning" || phase == "return-done" ? 13
                        : phase == "continuous" || phase == "continuous-stop" || phase == "output-wait" || phase == "output-cancel" ? 6 : 20;
                Account(check, g, money, expected);
            });
            c.That(stages.SetEquals(new[] { "idle", "idle-cancel", "supplying", "supply-cancel", "working", "returning", "return-done",
                "continuous", "continuous-stop", "output-wait", "output-cancel" }), "all native phase/cancel checkpoints observed for selected money mode");
        }
        internal static GameFactory Independent(string fault, bool half)
        {
            var document = Json.Parse(FrameworkProofContent.Read("transformer-proof.json"));
            document.Get("entities")[0].Get("components").Get("pulse_transformer").Set("queue_fault", JsonValue.Of(fault));
            document.Get("transformations")[0].Get("duration").Set("numerator", JsonValue.Number("12"));
            return TransformerTests.Factory(root => { Select(root, true, true); if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); },
                new TycoonModule(), Declaration, "{}", document.ToString(), MoneyDocuments(), root => SharedModes(root, "proof.process"));
        }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest
        { Scenario = "process", Seed = 202, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Actor(SimGame g) => g.Seat(0).Snapshot.Entities.Single(x => x.Def == "emitter").Id;
        private static EnqueueTransformationCommand Queue(SimGame g) => new EnqueueTransformationCommand
        { Transformer = Actor(g), Capability = Pulse, Transformation = "pulse", Count = 3 };
        private static CancelTransformationCommand Cancel(SimGame g, int index) => new CancelTransformationCommand
        { Transformer = Actor(g), Capability = Pulse, Index = index };
        private static void Submit(Check c, SimGame g, Command command)
        {
            ulong hash = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(command), "ordinary monetary queue admission");
            c.Equal(hash, g.Hash(), "monetary queue preview pays nothing");
            int sequence = g.Seat(0).Submit(command); g.Update();
            if (g.Fault != null) throw new InvalidOperationException("money fixture step=" + g.Clock.StepIndex + " " + g.Fault.Message);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "ordinary monetary player input applies during Update");
        }
        private static void Fault(Check c, string mode, int index, bool half)
        {
            var f = Independent(mode, half); var g = New(f); var idle = New(f); ulong untouched = idle.Hash();
            Submit(c, g, Queue(g)); Submit(c, g, new ConfigureOperationCommand { Entity = Actor(g), Operation = Pulse, Enabled = false, Target = Fix.One });
            Account(c, g, true, 20); var publication = g.Seat(0).Snapshot;
            Command input = mode.StartsWith("append", StringComparison.Ordinal) ? Queue(g) : Cancel(g, index);
            if (mode.Contains("admission"))
            {
                g.Clock.Pause(); c.Throws<InvalidOperationException>(() => g.Seat(0).Preview(input), "account-changing admission refuses through original queue guard");
            }
            else
            {
                ulong hash = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(input), "negative mutation admits normal intent");
                c.Equal(hash, g.Hash(), "negative mutation has pure preview"); g.Seat(0).Submit(input); var twin = f.Load(g.Save());
                g.Update(); twin.Update(); c.That(twin.Fault?.Phase == "transformations.queue-account" && twin.Fault.Message == g.Fault?.Message,
                    "saved pending account mutation refuses identically");
            }
            c.That(g.Fault?.Phase == "transformations.queue-account" && g.Fault.Message.Contains("persisted state"), "actual original account codec detects charge or refund");
            var first = g.Fault; long at = g.Clock.StepIndex; g.Update();
            c.That(ReferenceEquals(first, g.Fault) && at == g.Clock.StepIndex, "first monetary fault stops gameplay and sole clock");
            c.Throws<InvalidOperationException>(() => g.Save(), "faulty monetary mutation cannot become healthy checkpoint");
            c.That(publication.Entities.Count(x => x.Def == "token") == 0 && g.Seat(0).Snapshot.Entities.Count(x => x.Def == "token") == 0,
                "invalid account mutation publishes no productive result"); c.Equal(untouched, idle.Hash(), "unrelated account/queue session remains isolated");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual shared queue-account guard build");
            Native(c, false, false); Native(c, true, false); Native(c, true, true);
            foreach (bool half in new[] { false, true })
            {
                Fault(c, "append-charge", 0, half); Fault(c, "append-admission-charge", 0, half);
                Fault(c, "cancel-admission-charge", 1, half);
                foreach (int index in new[] { 0, 1 }) { Fault(c, "cancel-charge", index, half); Fault(c, "cancel-credit", index, half); }
            }
        }
    }
}
