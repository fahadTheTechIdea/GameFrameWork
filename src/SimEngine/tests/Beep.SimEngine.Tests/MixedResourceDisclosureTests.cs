using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Explicit authored mixed starting cargo, then ordinary holds/reveals and native physical recovery only.
    internal static class MixedResourceDisclosureTests
    {
        internal const string Case = "Mixed resource disclosure: whole stock receipts and carrier facts preserve saved physical recovery";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Port = new Code("disposition.input");
        private static GameFactory Factory()
        {
            var handling = Json.Parse(FrameworkProofContent.Read("point-handling.json"));
            var second = handling.Get("handling_profiles")[0].Get("resources")[0].Clone();
            second.Set("resource", JsonValue.Of("direct.unused")); handling.Get("handling_profiles")[0].Get("resources").Add(second);
            return ResourceContentContractTests.Factory(false, new DirectResourceContentModule(), edit: document => {
                var resources = document.Get("contract_resources"); var ordered = JsonValue.NewArray();
                ordered.Add(resources[1].Clone()); ordered.Add(resources[0].Clone()); document.Set("contract_resources", ordered);
            }, worldEdit: root => {
                root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("16"));
                var target = root.Get("entities")[1].Get("components"); target.Remove("point_stock"); target.Remove("transformer");
                target.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"public\":true,\"accepts\":[\"direct.unused\"],\"phases\":[\"solid\"],\"initial_stock\":{}}"));
                target.Get("point_presence").Set("position", JsonValue.Number("50"));
                root.Get("entity_setups")[1].Get("placement").Set("position", JsonValue.Number("50"));
                var worker = root.Get("entities")[2].Get("components");
                worker.Get("storage").Set("initial_stock", Json.Parse("{\"supply\":4,\"direct.unused\":2}"));
                worker.Get("storage").Set("accepts", Json.Parse("[\"supply\",\"direct.unused\"]"));
                worker.Get("point_presence").Set("position", JsonValue.Number("20"));
                root.Get("entity_setups")[2].Get("placement").Set("position", JsonValue.Number("20"));
                var scenario = root.Get("scenarios")[0];
                scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                scenario.Get("settings").Set(InventorySightModule.ModuleId,
                    Json.Parse("{\"target_kind\":\"resource\",\"target\":\"supply\",\"initial_hidden\":false}"));
                scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"recovery.high\"}"));
            }, extraModules: new GameModule[] { new InventorySightModule(), new LogisticsRecoveryPolicyModule() },
                extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]},"
                    + "{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["logistics-recovery.json"] = FrameworkProofContent.Read("logistics-recovery.json"),
                    ["point-handling.json"] = handling.ToString() });
        }
        private static Fix Stock(SimGame game, EntityId site, ResourceId resource)
        {
            Code refusal = game.Observer.Feature<LogisticsView>().ReadDiagnostics(site, resource, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual mixed stock/" + refusal); return value.Stock;
        }
        private static WorkJobView? Job(SimGame game, EntityId worker)
        {
            var rows = new List<WorkJobView>(); game.Observer.Feature<WorkView>().CopyJobs(rows);
            foreach (var row in rows) if (row.IsHaul && row.Worker == worker && (row.State == JobState.Assigned || row.State == JobState.Paused)) return row;
            return null;
        }
        private static void Hidden(Check c, SimGame game, EntityId worker, EntityId target, ResourceId known, ResourceId secret, bool privateReceipts)
        {
            ulong hash = game.Hash(); long at = game.Clock.StepIndex;
            var inventory = game.Seat(0).View.Feature<InventoryView>(); var logistics = game.Seat(0).View.Feature<LogisticsView>();
            c.That(game.Seat(0).View.TryGetEntity(worker, out _) && game.Seat(0).View.TryFindResource("direct.unused", out var visible) && visible == known
                && !game.Seat(0).View.TryFindResource("supply", out _), "actual vehicle and requested resource visible while other catalog resource is hidden");
            c.Equal(Code.None, inventory.ReadResourcePort(worker, Port, known, out var port), "existing per-resource port projection stays authorized");
            c.That(port.Stock.AmountOf(secret) == Fix.Zero && port.Stock.Lines.All(line => line.Resource == known), "named port never pretends to return whole inventory or leak other resources");
            c.Equal(Codes.NotFound, logistics.ReadDiagnostics(target, known, out var facts), "visible unrelated resource diagnostics cannot reveal hidden carrier stock");
            c.That(facts == null, "carrier partition withheld as a whole, not silently reduced");
            c.Equal(Codes.NotFound, logistics.ReadCargoDisposition(worker, out var partition), "existing whole cargo partition closure remains private");
            c.That(partition == null, "no private partition payload");
            var returns = new List<CargoReturnView> { default };
            c.Equal(Codes.NotFound, logistics.CopyCargoReturns(worker, returns), "existing mixed cargo return closure remains private");
            c.Equal(0, returns.Count, "whole mixed return list clears old caller seed");
            var observer = game.Observer.Feature<InventoryView>();
            c.Equal(Code.None, observer.ReadResourcePort(worker, Port, known, out _), "authorized observer retains actual named resource facts");
            c.Equal(Code.None, game.Observer.Feature<LogisticsView>().ReadCargoDisposition(worker, out var actual), "observer retains original whole physical cargo");
            c.That(actual.Retained.AmountOf(secret) + actual.Unassigned.AmountOf(secret) + actual.Committed.AmountOf(secret) == Fix.FromInt(4), "hiding never removes the physical secret four units");
            c.That(observer.TryGetReservations(worker, Code.None, out var original), "actual cargo receipts remain observable");
            c.Equal(privateReceipts, original.Any(record => record.Resource == secret), "proof reaches the intended actual receipt ownership");
            bool admitted = inventory.TryGetReservations(worker, Code.None, out var receipts);
            c.Equal(!privateReceipts, admitted, "whole receipt list authorizes every actual resource, not stock or requested-resource guesses");
            if (privateReceipts) c.Equal(0, receipts.Count, "hidden receipt leaves no partial claim tokens or resource identities");
            else c.That(receipts.All(record => record.Resource == known), "unrelated visible-resource receipt facts remain authorized");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<InventoryView>().ReadResourcePort(worker, Port, known, out _), "foreign mixed inventory still private");
            c.That(!game.Seat(1).View.Feature<InventoryView>().TryGetReservations(worker, Code.None, out var foreign) && foreign.Count == 0, "foreign receipt output empty");
            c.Equal(hash, game.Hash(), "all mixed-resource queries remain pure");
            c.Equal(at, game.Clock.StepIndex, "disclosure never advances the single game clock");
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var request = new NewGameRequest { Scenario = "points", Seed = 11423, Seats = { Controller.Human, Controller.Human } };
            var game = factory.NewGame(request); var idle = factory.NewGame(request); ulong isolated = idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "mixed physical resources do not select business");
            var worker = TransformationContentContractTests.Entity(game, "machine"); var source = TransformationContentContractTests.Entity(game, "source");
            var target = TransformationContentContractTests.Entity(game, "destination");
            c.That(game.Seat(0).View.TryFindResource("direct.unused", out var known) && game.Seat(0).View.TryFindResource("supply", out _), "both independent resources initially known");
            var secret = PermanentRemovalTests.Supply(game);
            c.That(known.Value < secret.Value, "actual canonical mixed stock has a visible prefix before the hidden resource");
            var timer = Stopwatch.StartNew(); long began = game.Clock.StepIndex;
            void Reach(SimGame twin, Func<bool> done, string stage)
            {
                while (!done() && game.Clock.StepIndex - began < 256 && timer.Elapsed.TotalSeconds < 25)
                {
                    PermanentRemovalTests.Step(game, twin);
                    c.Equal(Fix.FromInt(20), Stock(game, source, secret) + Stock(game, worker, secret), "finite secret stock conserved across its explicitly compatible endpoints during " + stage);
                    c.Equal(Fix.FromInt(2), Stock(game, target, known) + Stock(game, worker, known), "finite known stock conserved across its explicitly compatible endpoints during " + stage);
                }
                if (!done())
                {
                    var returns = new List<CargoReturnView>(); game.Observer.Feature<LogisticsView>().CopyCargoReturns(worker, returns);
                    throw new InvalidOperationException(stage + "/256updates25seconds/" + game.Clock.StepIndex + "/at=" + PermanentRemovalTests.At(game, worker)
                        + "/job=" + Job(game, worker)?.Resource + "/returns=" + string.Join(";", returns.Select(row => row.Resource + "/" + row.Stock + "/" + row.Return.Status + "/" + row.Return.Blocker)));
                }
            }
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, game, new SetWorkerHeldCommand { Worker = worker, Held = true });
            PermanentRemovalTests.Submit(c, game, new ConfigureStockPolicyCommand { Store = worker, Resource = secret,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(4), Fix.FromInt(4), Fix.FromInt(8), 30, LogisticsReserveAccess.Protected)) });
            var inventory = game.Seat(0).View.Feature<InventoryView>();
            c.Equal(Code.None, inventory.ReadResourcePort(worker, Port, known, out var retained), "visible actual selected resource admitted");
            c.That(retained.Stock.Count == 1 && retained.Stock.AmountOf(known) == Fix.FromInt(2) && retained.Stock.AmountOf(secret) == Fix.Zero, "per-resource projection is not a second whole-inventory owner");
            c.Equal(Code.None, game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(target, known, out var retainedDiagnostics), "visible whole carrier diagnosis admitted");
            PermanentRemovalTests.Submit(c, game, new SetInventorySightCommand { Hidden = true });
            Hidden(c, game, worker, target, known, secret, false);
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "hidden mixed held stock restores exactly");
            Hidden(c, twin, worker, target, known, secret, false);
            game.Clock.Pause(); twin.Clock.Pause(); long stopped = game.Clock.StepIndex; PermanentRemovalTests.Step(game, twin);
            c.Equal(stopped, game.Clock.StepIndex, "paused mixed stock and receipts share one stopped clock");
            game.Clock.Resume(); twin.Clock.Resume();
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = false });
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetWorkerHeldCommand { Worker = worker, Held = false });
            Reach(twin, () => Job(game, worker)?.Resource == known && PermanentRemovalTests.At(game, worker) > Fix.FromInt(21), "physical first-resource recovery");
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = true });
            Hidden(c, game, worker, target, known, secret, false); twin = factory.Load(game.Save()); Hidden(c, twin, worker, target, known, secret, false);
            c.Equal(game.Hash(), twin.Hash(), "visible receipt and hidden unassigned cargo save together during real travel");
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = false });
            Reach(twin, () => Job(game, worker) == null && Stock(game, worker, known) == Fix.Zero && Stock(game, target, known) == Fix.FromInt(2), "physical first-resource delivery with actual reserve intact");
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new ConfigureStockPolicyCommand { Store = worker, Resource = secret,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.Zero, Fix.Zero, Fix.FromInt(8), 30, LogisticsReserveAccess.Protected)) });
            Reach(twin, () => Job(game, worker)?.Resource == secret, "physical second-resource recovery after ordinary reserve release");
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = true });
            Hidden(c, game, worker, target, known, secret, true); twin = factory.Load(game.Save()); Hidden(c, twin, worker, target, known, secret, true);
            c.Equal(game.Hash(), twin.Hash(), "actual secret outbound cargo receipt restores without public leak");
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = false });
            Reach(twin, () => Stock(game, worker, known) == Fix.Zero && Stock(game, worker, secret) == Fix.Zero && Job(game, worker) == null, "actual final mixed-resource delivery");
            c.That(Stock(game, source, secret) == Fix.FromInt(20) && Stock(game, target, known) == Fix.FromInt(2), "two original cargo resources travel and unload once to compatible destinations");
            c.That(retained.Stock.Count == 1 && retained.Stock.AmountOf(known) == Fix.FromInt(2)
                && retainedDiagnostics.Carriers.Single(row => row.Worker == worker).Cargo.Unassigned.AmountOf(known) == Fix.FromInt(2)
                && retainedDiagnostics.Carriers.Single(row => row.Worker == worker).Cargo.Retained.AmountOf(secret) == Fix.FromInt(4), "old authorized selected-resource and whole carrier facts remain detached");
            c.That(inventory.TryGetReservations(worker, Code.None, out var finalReceipts) && finalReceipts.Count == 0, "all real cargo promises settled through their original book");
            c.Equal(game.Hash(), twin.Hash(), "saved mixed-resource claims and physical recovery converge");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final physical inventory saves exactly");
            c.Equal(isolated, idle.Hash(), "interleaved mixed-resource session isolated");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "full recording contains every advancing update");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "complete mixed-resource visibility and real recovery replay");
            Console.WriteLine("mixed-disclosure/full/" + recording.InitialStep + "->" + game.Clock.StepIndex + "/inputs=" + recording.CommandCount
                + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
        }
    }
}
