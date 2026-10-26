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
    internal static class RecoveryContentContractTests
    {
        internal const string Case = "Recovery content contracts: independent JSON return and evacuation preserve physical cargo";
        private static readonly ReplayLimits Limits = new ReplayLimits(4 * 1024 * 1024, 3000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe initialize, out RecoveryContractModule proof,
            Action<JsonValue> edit = null, Action<JsonValue> rootEdit = null)
        {
            proof = new RecoveryContractModule(initialize);
            var root = Json.Parse(PointGame.Definitions); var scenario = root.Get("scenarios")[0];
            scenario.Set("seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of(RecoveryContractModule.ModuleId));
            scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"contract.recovery\"}"));
            root.Set("work_policies", Json.Parse("[{\"id\":\"contract.worker\",\"enabled\":false,\"priority\":0,\"allowed_roles\":[\"haul\",\"inspect\"],\"area\":null,\"interruptible\":true,\"emergency_eligible\":true,\"assignment_mode\":\"all\",\"states\":{\"disabled\":\"blocked\",\"standby\":\"blocked\"}}]"));
            var source = root.Get("entities")[0].Get("components"); source.Remove("point_stock");
            source.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"public\":true,\"evacuate\":true,\"accepts\":[\"supply\"],\"phases\":[],\"initial_stock\":{\"supply\":6}}"));
            var machine = root.Get("entities")[2].Get("components");
            machine.Get("storage").Set("initial_stock", Json.Parse("{\"supply\":6}"));
            machine.Set("work_policy", Json.Parse("{\"profile\":\"contract.worker\"}"));
            var destination = root.Get("entities")[1].Get("components"); destination.Remove("point_stock");
            destination.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"public\":true,\"evacuate\":false,\"accepts\":[\"supply\"],\"phases\":[],\"initial_stock\":{}}"));
            destination.Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"contract.stock\"}]}"));
            root.Set("logistics_policies", Json.Parse("[{\"id\":\"contract.stock\",\"stock\":{\"supply\":{\"enabled\":true,\"minimum\":6,\"desired\":6,\"maximum\":12,\"priority\":77,\"reserve_access\":\"protected\"}},\"outlet\":{\"enabled\":true,\"export_priority\":0,\"reserve\":{},\"reserve_access\":\"protected\",\"allowed_destinations\":null},\"states\":{\"disabled\":\"blocked\"}}]"));
            var profiles = Json.Parse(FrameworkProofContent.Read("recovery-contracts.json")); edit?.Invoke(profiles); rootEdit?.Invoke(root);
            return PointGame.Factory(root.ToString(), extraModules: new GameModule[] { new LogisticsRecoveryPolicyModule(), proof },
                extraDeclarations: "[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.recovery-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\"}],\"documents\":[\"recovery-contracts.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "recovery-contracts.json", profiles.ToString() } });
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 811 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static EntityId Entity(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(value => value.Def == id).Id;
        private static LogisticsView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<LogisticsView>();
        private static Fix Amount(SimGame game, EntityId entity)
        { var values = new List<Facet>(); game.Seat(0).View.Inspect(entity, values); return values.Where(value => value.Key == Storage.FacetStock && value.Subject == "supply").Aggregate(Fix.Zero, (n, value) => n + value.Value); }
        private static List<LogisticsDispatchOffer> Queue(SimGame game, int seat = 0)
        { var result = new List<LogisticsDispatchOffer>(); View(game, seat).CopyQueue(result); return result; }
        private static List<WorkJobView> Jobs(SimGame game)
        { var result = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(result); return result; }
        private static bool Live(WorkJobView job) => job.State == JobState.Assigned || job.State == JobState.Paused;
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("recovery step=" + game.Clock.StepIndex + " faults=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 700 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
        }
        private static void Command(Check c, SimGame game, Command command)
        { ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal preview"); c.Equal(hash, game.Hash(), "preview pure"); c.That(TestKit.Order(game, 0, command).Accepted, "normal player command"); }
        private static void Enable(Check c, SimGame game, bool enabled)
            => Command(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
                Settings = new WorkPolicySettings(enabled, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
        private static void Run(Check c)
        {
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var proof); var game = New(factory);
            var source = Entity(game, "source"); var machine = Entity(game, "machine"); var destination = Entity(game, "destination");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current actual contract protocol");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "original saved state owners");
            c.That(proof.IndependentAliases, "actual selected profiles and destinations have no native base requirement");
            c.Equal(1, initialize.Initializations, "valid Initialize once");
            c.That(!game.Modules.Contains("sim.tycoon"), "resource-only recovery requires no business module");
            c.That(game.Seat(0).View.TryFindResource("supply", out var supply), "actual resource identity");
            Step(game); // Only normal Update observes residual recovery intent; views never create chronology.
            var offers = Queue(game); var returns = new List<CargoReturnView>(); ulong hash = game.Hash();
            c.That(offers.Any(q => q.Kind == LogisticsDispatchKind.CargoReturn && q.Priority == 110)
                && offers.Any(q => q.Kind == LogisticsDispatchKind.Evacuation && q.Priority == 90), "both JSON kinds use the same real dispatch queue");
            c.Equal(0, Queue(game, 1).Count, "recovery queue private");
            c.Equal(Code.None, View(game).CopyCargoReturns(machine, returns), "native cargo facts");
            c.That(returns.Single().Stock == Fix.FromInt(6), "real authored cargo is not fabricated by recovery");
            var foreignCargo = new List<CargoReturnView>();
            c.Equal(Codes.NotFound, View(game, 1).CopyCargoReturns(machine, foreignCargo), "private carrier identity is undisclosed");
            c.Equal(0, foreignCargo.Count, "no foreign cargo facts are published");
            c.Equal(Code.None, View(game).ReadCargoDisposition(machine, out var before), "actual native cargo disposition");
            c.Equal(Fix.FromInt(6), before.Unassigned.AmountOf(supply), "one original unassigned stock owner");
            c.Equal(hash, game.Hash(), "private facts and previews are pure");
            var recorder = game.RecordReplay(Limits);
            for (int i = 0; i < 3; i++) Step(game);
            c.That(Amount(game, machine) == Fix.FromInt(6) && Amount(game, source) == Fix.FromInt(6) && Amount(game, destination) == Fix.Zero, "disabled worker does not instantly return or evacuate");
            Enable(c, game, true);
            var first = Jobs(game).Single(j => Live(j));
            c.That(first.RecoveryKind == LogisticsRecoveryKind.CargoReturn && !first.Source.IsValid && first.Loaded == Fix.FromInt(6), "declared return priority admits original loaded carrier");
            c.Equal(Fix.Zero, Amount(game, destination), "assignment cannot instantly unload");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "loaded return saves");
            Until(game, twin, () => Amount(game, destination) == Fix.FromInt(6) && !Jobs(game).Any(Live), "timed return and departure");
            c.That(game.Clock.StepIndex > 3 && Amount(game, machine) == Fix.Zero && Amount(game, source) == Fix.FromInt(6), "real return travels/unloads while evacuation respects fulfilled demand");
            c.That(Queue(game).Any(q => q.Kind == LogisticsDispatchKind.Evacuation && q.Haul.Blocker == RecoveryDestinationDeclaration.NoDemand), "independent demand-only policy reports same typed no-demand blocker");
            Command(c, game, new ConfigureStockPolicyCommand { Store = destination, Resource = supply,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(12), Fix.FromInt(12), Fix.FromInt(12), 77, LogisticsReserveAccess.Protected)) });
            twin = factory.Load(game.Save());
            Until(game, twin, () => Jobs(game).Any(j => Live(j) && j.RecoveryKind == LogisticsRecoveryKind.Evacuation && j.Loaded.IsPositive), "physical evacuation pickup");
            var evacuation = Jobs(game).Single(Live);
            c.That(evacuation.Source == source && evacuation.Destination == destination && evacuation.Amount == Fix.FromInt(6), "original evacuation binds actual endpoints and demand gap");
            Enable(c, game, false); twin = factory.Load(game.Save());
            Fix cargo = Amount(game, machine); Fix position = ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(value => value.Id == machine).Spatial).Position;
            for (int i = 0; i < 3; i++) Step(game, twin);
            c.That(Amount(game, machine) == cargo && ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(value => value.Id == machine).Spatial).Position == position, "policy pause retains physical cargo/position");
            game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long step = game.Clock.StepIndex; Step(game, twin);
            c.That(game.Hash() == hash && game.Clock.StepIndex == step, "sole-clock pause advances nothing"); game.Clock.Resume(); twin.Clock.Resume();
            Enable(c, game, true); twin = factory.Load(game.Save());
            Until(game, twin, () => Amount(game, destination) == Fix.FromInt(12) && !Jobs(game).Any(Live), "same resumed evacuation completes");
            c.That(Amount(game, source) == Fix.Zero && Amount(game, machine) == Fix.Zero, "physical endpoints empty only after real delivery");
            c.Equal(Fix.FromInt(12), Amount(game, source) + Amount(game, machine) + Amount(game, destination), "return and evacuation conserve every unit");
            c.Equal(Fix.FromInt(6), before.Unassigned.AmountOf(supply), "retained cargo facts immutable");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "original chronology/job/stock codecs");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "normal commands replay actual recoveries");
            var fresh = New(factory); c.That(Amount(fresh, Entity(fresh, "machine")) == Fix.FromInt(6) && Amount(fresh, Entity(fresh, "destination")) == Fix.Zero, "fresh interleaved session isolated");
            Modes(c); Invalid(c);
        }
        private static void Modes(Check c)
        {
            foreach (string mode in new[] { "nearest_available", "demand_first", "demand_only" })
            {
                var factory = Factory(new InitializeProbe(), out _, document => document.Get("independent_recovery_destinations")[1].Set("mode", JsonValue.Of(mode)),
                    root => root.Get("logistics_policies")[0].Get("stock").Get("supply").Set("enabled", JsonValue.Of(false)));
                var game = New(factory); Enable(c, game, true);
                if (mode == "demand_only")
                {
                    c.That(Queue(game).Any(q => q.Kind == LogisticsDispatchKind.CargoReturn && q.Haul.Blocker == RecoveryDestinationDeclaration.NoDemand), "no-demand mode refuses real return");
                    c.Equal(Fix.FromInt(6), Amount(game, Entity(game, "machine")), "refusal retains cargo");
                }
                else
                {
                    var twin = factory.Load(game.Save()); Until(game, twin, () => Amount(game, Entity(game, "destination")) == Fix.FromInt(6) && !Jobs(game).Any(Live), "authored no-demand fallback " + mode);
                    c.Equal(Fix.FromInt(6), Amount(game, Entity(game, "source")), "mode never grants or evacuates unrequested stock");
                }
            }
            var disabled = New(Factory(new InitializeProbe(), out _, document =>
            { document.Get("independent_recovery_profiles")[0].Get("evacuation").Set("enabled", JsonValue.Of(false)); document.Get("independent_recovery_profiles")[0].Get("cargo_return").Set("enabled", JsonValue.Of(false)); }));
            Enable(c, disabled, true); var queue = Queue(disabled);
            c.That(queue.Any(q => q.Kind == LogisticsDispatchKind.CargoReturn && q.Haul.Blocker == LogisticsService.CargoReturnDisabled)
                && queue.Any(q => q.Kind == LogisticsDispatchKind.Evacuation && q.Haul.Blocker == LogisticsService.EvacuationDisabled), "explicit disabled kinds retain typed blockers");
        }
        private static void Invalid(Check c)
        {
            foreach (string fault in new[] { "evacuation", "return", "enabled", "priority", "negative", "attempts", "zero-attempts", "blockers", "duplicate", "blank", "destination", "unknown", "wrong-kind", "unused", "mode", "invalid-mode", "selection", "enabled-type", "blocker-array", "blocker-type", "destination-id" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, document =>
                {
                    var profile = document.Get("independent_recovery_profiles")[0]; var evacuation = profile.Get("evacuation");
                    if (fault == "evacuation") profile.Remove("evacuation"); if (fault == "return") profile.Remove("cargo_return");
                    if (fault == "enabled") evacuation.Remove("enabled"); if (fault == "priority") evacuation.Remove("priority");
                    if (fault == "enabled-type") evacuation.Set("enabled", JsonValue.Of("true"));
                    if (fault == "blocker-array") evacuation.Set("reroute_blockers", JsonValue.NewObject());
                    if (fault == "blocker-type") evacuation.Set("reroute_blockers", Json.Parse("[1]"));
                    if (fault == "destination-id") evacuation.Set("destination_policy", JsonValue.Of("Bad Destination"));
                    if (fault == "negative") evacuation.Set("priority", JsonValue.Number("-1")); if (fault == "attempts") evacuation.Remove("attempts_per_pass");
                    if (fault == "zero-attempts") evacuation.Set("attempts_per_pass", JsonValue.Number("0")); if (fault == "blockers") evacuation.Remove("reroute_blockers");
                    if (fault == "duplicate") evacuation.Set("reroute_blockers", Json.Parse("[\"core.not-found\",\"core.not-found\"]"));
                    if (fault == "blank") evacuation.Set("reroute_blockers", Json.Parse("[\"\"]")); if (fault == "destination") evacuation.Remove("destination_policy");
                    if (fault == "unknown") evacuation.Set("destination_policy", JsonValue.Of("missing"));
                    if (fault == "wrong-kind") evacuation.Set("destination_policy", JsonValue.Of("contract.recovery"));
                    if (fault == "unused") document.Get("independent_recovery_profiles")[1].Get("cargo_return").Set("destination_policy", JsonValue.Of("missing"));
                    if (fault == "mode") document.Get("independent_recovery_destinations")[0].Remove("mode");
                    if (fault == "invalid-mode") document.Get("independent_recovery_destinations")[0].Set("mode", JsonValue.Of("default"));
                }, root => { if (fault == "selection") root.Get("scenarios")[0].Get("settings").Get(LogisticsRecoveryPolicyModule.ModuleId).Set("profile", JsonValue.Of("contract.nearest")); })), "early recovery refusal " + fault);
                c.Equal(0, initialize.Initializations, "before Initialize " + fault);
            }
        }
    }
}

