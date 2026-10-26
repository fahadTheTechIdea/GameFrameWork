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
    // Authored intent and ordinary player inputs only. Native inventory, jobs, visibility and time own execution.
    internal static class CargoContentVisibilityTests
    {
        internal const string Case = "Cargo content visibility: hidden metadata withholds whole saved physical cargo facts";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory()
            => ResourceContentContractTests.Factory(false, new DirectResourceContentModule(), worldEdit: root => {
                root.Get("entities")[1].Get("components").Get("point_presence").Set("position", JsonValue.Number("50"));
                root.Get("entity_setups")[1].Get("placement").Set("position", JsonValue.Number("50"));
                var scenario = root.Get("scenarios")[0];
                scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                scenario.Get("settings").Set(InventorySightModule.ModuleId,
                    Json.Parse("{\"target_kind\":\"resource\",\"target\":\"supply\",\"initial_hidden\":false}"));
                scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"recovery.high\"}"));
            }, extraModules: new GameModule[] { new InventorySightModule(), new LogisticsRecoveryPolicyModule() },
                extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]},"
                    + "{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["logistics-recovery.json"] = FrameworkProofContent.Read("logistics-recovery.json") });
        private static LogisticsView View(SimGame game) => game.Seat(0).View.Feature<LogisticsView>();
        private static CargoDispositionSnapshot Actual(SimGame game, EntityId worker)
        {
            Code refusal = game.Observer.Feature<LogisticsView>().ReadCargoDisposition(worker, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual cargo/" + refusal); return value;
        }
        private static Fix Stock(SimGame game, EntityId site, ResourceId resource)
        {
            Code refusal = game.Observer.Feature<LogisticsView>().ReadDiagnostics(site, resource, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual stock/" + refusal); return value.Stock;
        }
        private static void Hidden(Check c, SimGame game, EntityId worker, ResourceId resource)
        {
            ulong hash = game.Hash(); long at = game.Clock.StepIndex;
            c.That(game.Seat(0).View.TryGetEntity(worker, out _), "resource secrecy does not hide the actual owned vehicle");
            c.That(!game.Seat(0).View.TryFindResource("supply", out _), "original content authority actually hides independent resource metadata");
            c.Equal(Codes.NotFound, game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(worker,
                new Code("disposition.input"), resource, out _), "ordinary inventory already refuses cached hidden resource identity");
            var rows = new List<CargoReturnView> { default };
            c.Equal(Codes.NotFound, View(game).CopyCargoReturns(worker, rows), "cargo return facts obey the same content authority");
            c.Equal(0, rows.Count, "hidden cargo leaves no seeded or authorized partial prefix");
            c.Equal(Codes.NotFound, View(game).ReadCargoDisposition(worker, out var partition), "whole hidden-resource partition is withheld");
            c.That(partition == null, "no fabricated empty or partially redacted partition");
            var foreign = game.Seat(1).View.Feature<LogisticsView>(); rows.Add(default);
            c.Equal(Codes.NotFound, foreign.CopyCargoReturns(worker, rows), "foreign company cannot disclose cargo by cached vehicle ID");
            c.Equal(0, rows.Count, "foreign cargo list is cleared");
            c.Equal(Codes.NotFound, foreign.ReadCargoDisposition(worker, out var privatePartition), "foreign partition remains private");
            c.That(privatePartition == null, "foreign partition has no payload");
            var observer = game.Observer.Feature<LogisticsView>();
            c.Equal(Code.None, observer.CopyCargoReturns(worker, rows), "explicit observer retains authorized actual cargo diagnosis");
            c.That(rows.Count == 1 && rows[0].Resource == resource && rows[0].Stock == Fix.FromInt(4), "metadata hiding never destroys or invents actual physical cargo");
            var actual = Actual(game, worker);
            c.Equal(Fix.FromInt(4), actual.Retained.AmountOf(resource) + actual.Committed.AmountOf(resource) + actual.Unassigned.AmountOf(resource), "actual partition still conserves all four units");
            c.Equal(hash, game.Hash(), "current-content and observer queries do not mutate state");
            c.Equal(at, game.Clock.StepIndex, "cargo disclosure never advances the sole clock");
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var request = new NewGameRequest { Scenario = "points", Seed = 11223, Seats = { Controller.Human, Controller.Human } };
            var game = factory.NewGame(request); var idle = factory.NewGame(request); ulong isolated = idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical cargo proof requires no business module");
            EntityId source = TransformationContentContractTests.Entity(game, "source"), target = TransformationContentContractTests.Entity(game, "destination"),
                worker = TransformationContentContractTests.Entity(game, "machine");
            c.That(game.Seat(0).View.TryFindResource("supply", out var resource), "authored independent resource initially visible");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            var timer = Stopwatch.StartNew(); long began = game.Clock.StepIndex;
            void Reach(SimGame twin, Func<bool> done, string stage)
            {
                while (!done() && game.Clock.StepIndex - began < 256 && timer.Elapsed.TotalSeconds < 20)
                {
                    PermanentRemovalTests.Step(game, twin);
                    c.Equal(Fix.FromInt(20), Stock(game, source, resource) + Stock(game, target, resource) + Stock(game, worker, resource), "finite stock conserved during " + stage);
                }
                if (!done()) throw new InvalidOperationException(stage + "/bounded256updates20seconds/at=" + game.Clock.StepIndex + "/jobs="
                    + string.Join(";", PermanentRemovalTests.Jobs(game).Select(job => job.Id + "/" + job.State + "/" + job.Blocker)));
            }
            PermanentRemovalTests.Submit(c, game, new HaulResourcesCommand { Source = source, Destination = target, Worker = worker,
                Resource = resource, Ceiling = Fix.FromInt(4), Priority = 100 });
            Reach(null, () => Stock(game, worker, resource) == Fix.FromInt(4) && PermanentRemovalTests.At(game, worker) > Fix.One, "actual loaded travel");
            c.Equal(Code.None, View(game).ReadCargoDisposition(worker, out var retained), "visible actual cargo partition admitted");
            c.Equal(Fix.FromInt(4), retained.Committed.AmountOf(resource), "active physical delivery owns original outbound cargo claim");
            var returns = new List<CargoReturnView>();
            c.Equal(Code.None, View(game).CopyCargoReturns(worker, returns), "visible return facts admitted");
            var retainedReturn = returns.Single();
            PermanentRemovalTests.Submit(c, game, new SetInventorySightCommand { Hidden = true });
            Hidden(c, game, worker, resource);
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "hidden content and real in-flight cargo restore together");
            Hidden(c, twin, worker, resource);
            game.Clock.Pause(); twin.Clock.Pause(); long stopped = game.Clock.StepIndex;
            PermanentRemovalTests.Step(game, twin); Hidden(c, game, worker, resource);
            c.Equal(stopped, game.Clock.StepIndex, "paused hidden cargo never receives a synthetic update");
            game.Clock.Resume(); twin.Clock.Resume();
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = false });
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetWorkerHeldCommand { Worker = worker, Held = true });
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new ConfigureStockPolicyCommand { Store = worker, Resource = resource,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.One, Fix.FromInt(8), 30, LogisticsReserveAccess.Protected)) });
            var held = Actual(game, worker);
            c.That(held.Retained.AmountOf(resource) == Fix.One && held.Committed.IsEmpty && held.Unassigned.AmountOf(resource) == Fix.FromInt(3),
                "ordinary hold releases job receipts while actual stock policy retains one unit");
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = true });
            Hidden(c, game, worker, resource); twin = factory.Load(game.Save()); Hidden(c, twin, worker, resource);
            c.Equal(game.Hash(), twin.Hash(), "hidden held policy partition saves without alternate state");
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = false });
            c.Equal(Code.None, View(game).ReadCargoDisposition(worker, out var revealed), "same cached view follows current reveal");
            c.That(revealed.Retained.AmountOf(resource) == Fix.One && revealed.Unassigned.AmountOf(resource) == Fix.FromInt(3), "reveal reports existing policy quantities without a regrant");
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new ConfigureStockPolicyCommand { Store = worker, Resource = resource,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.Zero, Fix.Zero, Fix.FromInt(8), 30, LogisticsReserveAccess.Protected)) });
            Fix departure = PermanentRemovalTests.At(game, worker); long releasedAt = game.Clock.StepIndex;
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetWorkerHeldCommand { Worker = worker, Held = false });
            c.That(departure > Fix.One && Stock(game, worker, resource) == Fix.FromInt(4), "release cannot teleport original loaded cargo home");
            Reach(twin, () => Stock(game, worker, resource) == Fix.Zero && !PermanentRemovalTests.Jobs(game).Any(job => job.IsHaul && job.Worker.IsValid), "actual cargo recovery");
            c.That(game.Clock.StepIndex > releasedAt + 1 && Stock(game, source, resource) == Fix.FromInt(20) && Stock(game, target, resource) == Fix.Zero,
                "cancelled original delivery returns finite cargo physically to nearest source over game time");
            c.That(retained.Committed.AmountOf(resource) == Fix.FromInt(4) && retainedReturn.Stock == Fix.FromInt(4), "previously authorized immutable facts do not change after hide hold and recovery");
            c.Equal(game.Hash(), twin.Hash(), "saved policy and original cargo recovery converge");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "finished physical cargo checkpoint restores exactly");
            c.Equal(isolated, idle.Hash(), "interleaved session visibility and stock remain isolated");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "full recording includes every advancing game update");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "actual visibility policy commands and complete physical cargo lifecycle replay");
            Console.WriteLine("cargo-content/full/" + recording.InitialStep + "->" + game.Clock.StepIndex + "/inputs=" + recording.CommandCount
                + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
        }
    }
}
