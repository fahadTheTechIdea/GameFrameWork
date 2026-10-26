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
    // JSON intent and public player input/Update/views only; no duplicate policy or hauling implementation.
    internal static class LogisticsPolicyContentTests
    {
        internal const string Case = "Logistics policy content: direct JSON profiles govern original physical stock and export rules";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(LogisticsPolicyContentModule proof, bool direct = true, bool external = false,
            Action<JsonValue> edit = null, Action<JsonValue> editRoot = null, bool recovery = false,
            bool shipment = false, Action<JsonValue> editWork = null)
        {
            var profiles = Json.Parse(FrameworkProofContent.Read("logistics-policy-content.json"));
            if (direct) {
                var entries = profiles.Get("logistics_policies"); for (int i = 0; i < entries.Count; i++) entries[i].Set("proof_fault", JsonValue.Of("none"));
                profiles.Set("independent_logistics_policies", entries); profiles.Remove("logistics_policies");
            }
            edit?.Invoke(profiles);
            var modules = new List<GameModule> { proof };
            var declarations = Json.Parse("[{\"id\":\"proof.logistics-policy-content\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-profile-proof.json\"]}]");
            var documents = new Dictionary<string, string> { ["logistics-profile-proof.json"] = profiles.ToString() };
            if (recovery) {
                modules.Add(new LogisticsRecoveryPolicyModule());
                declarations.Add(Json.Parse("{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}"));
                documents.Add("logistics-recovery.json", FrameworkProofContent.Read("logistics-recovery.json"));
            }
            return DispatchAcceptanceTests.Factory(shipment: shipment, extraModules: modules, extraDeclarations: declarations.ToString(), extraDocuments: documents, editWork: editWork, editRoot: root => {
                    var source = root.Get("entities")[0].Get("components");
                    if (external) source.Set("contract_outlet_policy", Json.Parse("{\"profile\":\"policy.source\",\"fault\":0}"));
                    else source.Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"policy.source\"}]}"));
                    root.Get("entities")[1].Get("components").Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"policy.destination\"}]}"));
                    root.Set("point_interval_regions", Json.Parse("[{\"id\":\"policy.short\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":8},{\"id\":\"policy.all\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":100}]"));
                    if (recovery) root.Get("scenarios")[0].Get("settings").Set("sim.logistics-recovery", Json.Parse("{\"profile\":\"recovery.high\"}"));
                    editRoot?.Invoke(root);
                });
        }
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 764, Mods = { "proof.shipments" }, Seats = { Controller.Human, Controller.Human } });
        internal static EntityId Id(SimGame game, string name) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == name && e.Owner == new PlayerId(0)).Id;
        internal static ResourceId Supply(SimGame game)
        { if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("missing authored supply"); return resource; }
        internal static LogisticsView View(SimGame game) => game.Seat(0).View.Feature<LogisticsView>();
        internal static List<WorkJobView> Jobs(SimGame game)
        { var facts = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(facts); return facts; }
        internal static Fix Stock(SimGame game, string name)
        { var facts = new List<Facet>(); game.Seat(0).View.Inspect(Id(game, name), facts); return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value); }
        internal static void Step(Check c, SimGame game, SimGame twin)
        { game.Update(); twin.Update(); if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("actual stock-policy fault " + game.Fault + "/" + twin.Fault); c.Equal(game.Hash(), twin.Hash(), "actual policy/stock/claim/clock twins"); }
        internal static void Until(Check c, SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++) Step(c, game, twin);
            if (!done()) throw new InvalidOperationException(stage + " exceeded512Updates at=" + game.Clock.StepIndex + "/jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/loaded=" + j.Loaded)));
        }
        internal static SimGame Input(Check c, GameFactory factory, SimGame game, params Command[] commands)
        {
            var sequences = new List<int>(); foreach (var command in commands) {
                ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary policy command preview"); c.Equal(before, game.Hash(), "policy preview claims/spends nothing"); sequences.Add(game.Seat(0).Submit(command));
            }
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending actual policy inbox restores"); Step(c, game, twin);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            foreach (int sequence in sequences) { var result = results.Single(r => r.Sequence == sequence); c.That(result.Accepted, "actual policy command result " + result.Refusal); if (!result.Accepted) throw new InvalidOperationException("policy refusal " + result.Refusal); }
            return twin;
        }
        internal static ConfigureWorkPolicyCommand Worker(SimGame game, string area) => new ConfigureWorkPolicyCommand { Worker = Id(game, "machine"),
            Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, area, true, true, WorkAssignmentMode.All) };
        internal static ConfigureCompanyAlertCommand Reserve(int minimum) => new ConfigureCompanyAlertCommand { Company = new PlayerId(0),
            Settings = new CompanyAlertSettings("alerts.standard", CompanyAlertLevel.Normal, new[] { new WorkerReserveSetting(new WorkerRoleId("haul"), minimum) }) };
        private static ConfigureOutletPolicyCommand Export(SimGame game, int reserve, bool enabled = true, string destination = "destination")
            => new ConfigureOutletPolicyCommand { Source = Id(game, "source"), Change = OutletPolicyChange.Replace(new OutletPolicySettings(enabled,
                new Bill(new[] { new ResourceAmount(Supply(game), Fix.FromInt(reserve)) }), 100, new[] { destination }, LogisticsReserveAccess.Protected)) };
        private static ConfigureStockPolicyCommand Band(SimGame game, int maximum) => new ConfigureStockPolicyCommand { Store = Id(game, "destination"), Resource = Supply(game),
            Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(maximum), Fix.FromInt(maximum), Fix.FromInt(maximum), 100, LogisticsReserveAccess.Protected)) };
        internal static void Wait(Check c, SimGame game, SimGame twin, string why)
        {
            for (int i = 0; i < 12; i++) Step(c, game, twin);
            c.That(!Jobs(game).Any(j => j.Worker.IsValid), why + " prevents physical acquisition");
            c.Equal(Fix.FromInt(20), Stock(game, "source") + Stock(game, "destination") + Stock(game, "machine"), "blocked policy never duplicates/destroys actual supply");
        }
        internal static SimGame Partial(Check c, GameFactory factory, SimGame game)
        {
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial actual haul receipts/policies restore");
            game.Clock.Pause(); twin.Clock.Pause(); ulong before = game.Hash(); c.Equal(0, game.Update(), "sole clock pauses loading/travel"); c.Equal(0, twin.Update(), "saved clock pauses"); c.Equal(before, game.Hash(), "pause preserves exact stock and receipts"); game.Clock.Resume(); twin.Clock.Resume(); return twin;
        }
        private static JsonValue Unused(JsonValue document, bool direct) => document.Get(direct ? "independent_logistics_policies" : "logistics_policies")[2];
        private static void Refuse(Check c, bool direct, Action<JsonValue> edit, string why)
        { var proof = new LogisticsPolicyContentModule(); c.Throws<ContentException>(() => New(Factory(proof, direct, edit: edit)), why); c.Equal(0, proof.Initialize.Initializations, "invalid unused actual metadata refuses before Initialize"); }
        private static void Run(Check c)
        {
            foreach (bool direct in new[] { false, true }) {
                foreach (string field in new[] { "stock", "outlet", "states" }) Refuse(c, direct, doc => Unused(doc, direct).Remove(field), "required " + field);
                Refuse(c, direct, doc => Unused(doc, direct).Set("unexpected", JsonValue.Of(true)), "strict unknown profile field");
                Refuse(c, direct, doc => Unused(doc, direct).Get("stock").Get("supply").Set("minimum", JsonValue.Number("-1")), "negative stock floor");
                Refuse(c, direct, doc => Unused(doc, direct).Get("stock").Get("supply").Set("desired", JsonValue.Number("0")), "inverted desired band");
                Refuse(c, direct, doc => Unused(doc, direct).Get("stock").Get("supply").Set("maximum", JsonValue.Number("3")), "inverted maximum band");
                Refuse(c, direct, doc => Unused(doc, direct).Get("stock").Get("supply").Set("minimum", JsonValue.Number("0.5")), "unused fractional discrete stock floor");
                Refuse(c, direct, doc => Unused(doc, direct).Get("outlet").Get("reserve").Set("supply", JsonValue.Number("0.5")), "unused fractional discrete reserve");
                Refuse(c, direct, doc => Unused(doc, direct).Get("outlet").Set("allowed_destinations", Json.Parse("[\"missing.target\"]")), "unused missing destination");
                Refuse(c, direct, doc => Unused(doc, direct).Get("outlet").Set("allowed_destinations", Json.Parse("[\"destination\",\"destination\"]")), "unused duplicate destination");
                Refuse(c, direct, doc => Unused(doc, direct).Get("states").Set("disabled", JsonValue.Of("missing.state")), "unused missing state alias");
                Refuse(c, direct, doc => Unused(doc, direct).Get("stock").Set("missing.resource", Unused(doc, direct).Get("stock").Get("supply").Clone()), "unused missing resource");
                foreach (bool external in new[] { false, true }) Physical(c, direct, external);
            }
            foreach (string fault in new[] { "null-stock", "duplicate-stock", "null-outlet", "null-states" })
                Refuse(c, true, doc => Unused(doc, true).Set("proof_fault", JsonValue.Of(fault)), "actual independent unused " + fault);
            foreach (string mode in new[] { "clock", "throw", "throw-after-clock" }) Fault(c, mode);
        }
        private static void Physical(Check c, bool direct, bool external)
        {
            var proof = new LogisticsPolicyContentModule(); var factory = Factory(proof, direct, external); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); using var recorder = game.RecordReplay(Limits);
            c.Equal(2, proof.Initialize.Initializations, "explicit native/direct module/master initialization");
            ulong before = game.Hash(); c.Equal(Code.None, View(game).ReadOutletPolicy(Id(game, "source"), out var held), "actual profile controls original source owner");
            c.Equal(Fix.FromInt(16), held.Reserve.AmountOf(Supply(game)), "authored native/direct reserve"); c.Equal(Code.None, View(game).ReadStockPolicy(Id(game, "destination"), Supply(game), out var originalBand), "original actual-store stock owner");
            c.Equal(Fix.FromInt(8), originalBand.Maximum, "authored native/direct receiving maximum"); c.Equal(before, game.Hash(), "policy fact reads pure");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().ReadOutletPolicy(Id(game, "source"), out var foreign), "foreign settings withheld"); c.That(foreign == null, "no private foreign prefix");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(Export(game, 12)), "foreign export command denied"); c.Equal(Codes.NotFound, game.Seat(1).Preview(Band(game, 8)), "foreign stock command denied"); c.Equal(before, game.Hash(), "foreign refusals pure");
            var twin = Input(c, factory, game, Worker(game, "policy.short")); Wait(c, game, twin, "operating area");
            c.That(View(game).PreviewHaul(Id(game, "source"), Id(game, "destination"), Supply(game), Fix.FromInt(8)).Status != HaulStatus.Ready, "area cannot authorize forbidden pickup/delivery");
            twin = Input(c, factory, game, Worker(game, "policy.all"), Reserve(1)); Wait(c, game, twin, "company ready-worker reserve");
            twin = Input(c, factory, game, Reserve(0), Export(game, 16, destination: "acceptance.sink")); Wait(c, game, twin, "destination filter");
            c.Equal(LogisticsPolicyCodes.Destination, View(game).PreviewHaul(Id(game, "source"), Id(game, "destination"), Supply(game), Fix.FromInt(8)).Blocker, "original typed destination permission refusal");
            twin = Input(c, factory, game, Export(game, 16)); Until(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "reserve-limited original physical hauling");
            var first = Jobs(game).Single(j => j.IsHaul && j.Worker.IsValid); c.Equal(Fix.FromInt(4), first.Amount, "only four actual units exported above reserve16"); c.Equal(Fix.Zero, first.Loaded, "acquisition cannot teleport loading"); c.Equal(Fix.Zero, Stock(game, "destination"), "assignment cannot instantly deliver");
            twin = Partial(c, factory, game); twin = Input(c, factory, game, Band(game, 4));
            Until(c, game, twin, () => Stock(game, "destination") == Fix.FromInt(4) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "saved first physical four-unit delivery");
            c.Equal(Fix.FromInt(16), Stock(game, "source"), "first actual export leaves16 reserve");
            twin = Input(c, factory, game, Export(game, 12)); Wait(c, game, twin, "receiving maximum4");
            c.Equal(LogisticsPolicyCodes.Maximum, View(game).PreviewHaul(Id(game, "source"), Id(game, "destination"), Supply(game), Fix.FromInt(8)).Blocker, "original receiving maximum refusal");
            twin = Input(c, factory, game, Export(game, 12, false), Band(game, 8)); Wait(c, game, twin, "disabled export owner");
            twin = Input(c, factory, game, Export(game, 12)); Until(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "normal source restart after actual policy release");
            c.Equal(Fix.FromInt(4), Jobs(game).Single(j => j.IsHaul && j.Worker.IsValid).Amount, "fresh receiving/reserve gap uses actual current four-unit balance");
            Until(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.Loaded.IsPositive), "actual loaded cargo"); twin = Partial(c, factory, game);
            Until(c, game, twin, () => Stock(game, "destination") == Fix.FromInt(8) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "saved second physical delivery");
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "retained reserve12 never bypassed"); c.Equal(Fix.Zero, Stock(game, "machine"), "completed native cargo empty");
            c.Equal(Fix.FromInt(16), held.Reserve.AmountOf(Supply(game)), "retained outlet settings detached"); c.Equal(Fix.FromInt(8), originalBand.Maximum, "retained original stock settings detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal physical policy state restores without extra resources"); c.Equal(untouched, isolated.Hash(), "interleaved policies/stocks/clock isolated");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits); c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "actual policy/claim/travel replay: " + replay.Failure + "/" + replay.Detail);
        }
        private static void Fault(Check c, string mode)
        {
            var proof = new LogisticsPolicyContentModule(); var factory = Factory(proof); var isolated = New(factory); var game = New(factory); ulong untouched = isolated.Hash();
            var disable = Export(game, 16, false); c.Equal(Code.None, game.Seat(0).Preview(disable), "normal disabled source admission");
            game.Seat(0).Submit(disable); game.Update(); c.That(game.Fault == null, "normal disabled source commits before callback injection"); proof.Arm("policy.source", mode);
            c.Equal(0, game.Update(), "original Update stops without publishing a successful faulting step");
            c.That(game.Fault != null, "actual profile state callback faults original entity Update " + mode);
            if (mode != "clock") c.Equal("InvalidOperationException: proof.logistics-policy-first-error", game.Fault?.Message, "actual first callback error preserved");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted policy session cannot checkpoint");
            c.Throws<InvalidOperationException>(() => View(game).ReadOutletPolicy(EntityId.None, out _), "faulted missing query publishes no healthy prefix"); c.Equal(untouched, isolated.Hash(), "negative metadata callback isolated");
        }
    }
}
