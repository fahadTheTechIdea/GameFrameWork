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
    // Ordinary inputs and detached views; the original physical-service body remains the sole driver.
    internal static class ServiceActivityDisclosureTests
    {
        internal const string Case = "Service activity disclosure: hidden workflows protect schedules and consulted trip decisions without stopping admitted physical work";
        private static readonly Code Service = new Code("service.interval");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static ConfigureRecurringPlanCommand Configure(EntityId worker, bool enabled, int priority = 60)
            => new ConfigureRecurringPlanCommand { Entity = worker, Source = Service,
                Settings = new RecurringPlanSettings(enabled, priority, GameDuration.Minutes(60), Code.None) };
        private static RecurringPlanView Actual(SimGame game, EntityId worker)
        {
            var code = game.Observer.Feature<WorkView>().ReadRecurringPlan(worker, Service, out var value);
            if (!code.IsNone) throw new InvalidOperationException("observer service facts: " + code);
            return value;
        }
        private static void Both(Check c, SimGame game, SimGame twin, Func<Command> command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command()), "ordinary branch input is admitted");
            int sequence = game.Seat(0).Submit(command()); twin.Seat(0).Submit(command()); PermanentRemovalTests.Step(game, twin);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(row => row.Sequence == sequence).Accepted, "branch command executes normally");
        }
        private static void Hidden(Check c, SimGame game, EntityId worker)
        {
            var work = game.Seat(0).View.Feature<WorkView>(); var logistics = game.Seat(0).View.Feature<LogisticsView>();
            ulong hash = game.Hash();
            c.Equal(Codes.NotFound, work.ReadRecurringPlan(worker, Service, out var plan), "private activity hides recurring intent and chronology");
            c.That(plan.Intent == null && plan.GroupIntent == null && plan.Schedule.Settings == null, "no partial recurring facts escape");
            c.Equal(Codes.NotFound, work.ReadRecurringFacilities(worker, Service, out var facilities), "private activity hides facility-kind refusal");
            c.That(facilities == null, "no facility facts escape");
            c.Equal(Codes.NotFound, work.ReadRecurringTemplate(worker, Service, out var template), "private activity hides template-kind refusal");
            c.That(template == null, "no future template facts escape");
            c.Equal(Codes.NotFound, logistics.ReadHaulTripServiceSources(worker, out var sources), "private source binding list is opaque");
            c.That(sources == null, "no source IDs escape");
            c.Equal(Codes.NotFound, logistics.ReadHaulTripServiceWindow(worker, out var window), "consulted private service window is opaque");
            c.That(window == null, "no private service deadline escapes");
            c.Equal(Codes.NotFound, work.PreviewRecurringPlan(worker, Service, Configure(worker, true, 61).Settings), "typed activation respects activity knowledge");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(Configure(worker, true, 61)), "ordinary activation shares typed refusal");
            c.Equal(hash, game.Hash(), "all denied reads and previews are pure");
        }
        private static void Verify(Check c, GameFactory factory, SimGame parent, EntityId worker, EntityId source, EntityId target, ResourceId supply)
        {
            ulong parentHash = parent.Hash(); var game = factory.Load(parent.Save()); var twin = factory.Load(game.Save());
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            Both(c, game, twin, () => new SetInventorySightCommand { Hidden = true });
            Hidden(c, game, worker); Hidden(c, factory.Load(game.Save()), worker);
            var refused = factory.Load(game.Save());
            using (var denied = refused.RecordReplay(limits))
            {
                int sequence = refused.Seat(0).Submit(Configure(worker, true, 61)); refused.Update();
                long cursor = 0; var rows = new List<CommandResult>(); refused.Seat(0).ReadResults(ref cursor, rows);
                var result = rows.Single(row => row.Sequence == sequence);
                c.That(!result.Accepted && result.Refusal == Codes.NotFound, "executed activation cannot bypass private activity");
                c.Equal(60, Actual(refused, worker).Schedule.Priority, "refused policy has no settings prefix");
                var replay = new ReplayVerifier().Verify(factory, denied.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == refused.Hash(), "actual denied activation replays");
            }
            var disabled = factory.Load(game.Save());
            using (var stop = disabled.RecordReplay(limits))
            {
                long admitted = Actual(disabled, worker).Schedule.LastPlan;
                PermanentRemovalTests.Submit(c, disabled, Configure(worker, false));
                c.That(!Actual(disabled, worker).Schedule.Enabled && Actual(disabled, worker).Schedule.LastPlan == admitted,
                    "safe blind disable affects future intent, not admitted physical work");
                var view = disabled.Seat(0).View.Feature<LogisticsView>();
                c.Equal(Code.None, view.ReadHaulTripServiceWindow(worker, out var window), "unconsulted disabled source does not hide unrestricted decision");
                c.That(window != null && window.Unrestricted, "disabled source invents no deadline");
                c.Equal(Codes.NotFound, view.ReadHaulTripServiceSources(worker, out _), "binding metadata remains private when disabled");
                var replay = new ReplayVerifier().Verify(factory, stop.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == disabled.Hash(), "actual safe disable replays");
            }
            var watch = Stopwatch.StartNew(); int updates = 0;
            while (Actual(game, worker).Schedule.LastCompletedPlan == 0 && updates < 128 && watch.ElapsedMilliseconds < 25000)
            {
                PermanentRemovalTests.Step(game, twin); updates++;
            }
            c.That(updates < 128 && watch.ElapsedMilliseconds < 25000 && Actual(game, worker).Schedule.LastCompletedPlan > 0,
                "already admitted physical service completes while its activity is hidden");
            c.Equal(Fix.FromInt(17), LoadPolicyDisclosureTests.Stock(game, source, supply), "private service consumes actual three spares once");
            Hidden(c, game, worker);
            var haul = new HaulResourcesCommand { Source = source, Destination = target, Worker = worker, Resource = supply, Ceiling = Fix.FromInt(4) };
            c.Equal(HaulStatus.NotFound, game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker).Status,
                "fresh trip decision retains consulted private service dependency");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(haul), "ordinary trip preview shares private dependency refusal");
            Both(c, game, twin, () => new SetInventorySightCommand { Hidden = false });
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().ReadRecurringPlan(worker, Service, out var revealed), "reveal returns actual earned history");
            c.That(revealed.Schedule.LastCompletedPlan > 0, "reveal does not fabricate a new completion");
            var crossing = factory.Load(game.Save());
            using (var crossingRecording = crossing.RecordReplay(limits))
            {
                var logistics = crossing.Seat(0).View.Feature<LogisticsView>();
                var fitting = logistics.PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker);
                if (fitting.Status != HaulStatus.Ready || !fitting.Travel?.RemainingTicks.HasValue == true)
                    throw new InvalidOperationException("actual visible fitting service journey required");
                var interval = GameDuration.FromSteps(crossing.Clock.StepIndex - revealed.LastCompletedTick.Value
                    + fitting.Travel.Value.RemainingTicks.Value, crossing.Clock.Time);
                PermanentRemovalTests.Submit(c, crossing, new ConfigureRecurringPlanCommand { Entity = worker, Source = Service,
                    Settings = new RecurringPlanSettings(true, 60, interval, Code.None) });
                c.Equal(RecurringPlanCodes.MovementWindow,
                    logistics.PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker).Blocker,
                    "actual visible crossing journey refuses before privacy changes");
                PermanentRemovalTests.Submit(c, crossing, new SetInventorySightCommand { Hidden = true });
                var privateRefusal = logistics.PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker);
                c.That(privateRefusal.Status == HaulStatus.NotFound && !privateRefusal.Carrier.IsValid && privateRefusal.Blocker.IsNone,
                    "refused candidate retains private service dependency without publishing carrier or blocker");
                c.Equal(Codes.NotFound, crossing.Seat(0).Preview(haul), "ordinary refused journey shares opaque typed refusal");
                int sequence = crossing.Seat(0).Submit(haul); crossing.Update();
                long cursor = 0; var rows = new List<CommandResult>(); crossing.Seat(0).ReadResults(ref cursor, rows);
                c.That(rows.Single(row => row.Sequence == sequence).Refusal == Codes.NotFound
                    && !rows.Single(row => row.Sequence == sequence).Accepted, "executed refused journey cannot reveal or bypass service source");
                c.That(LoadPolicyDisclosureTests.Stock(crossing, source, supply) == Fix.FromInt(17)
                    && LoadPolicyDisclosureTests.Stock(crossing, worker, supply).IsZero, "private refusal creates no cargo prefix");
                var replay = new ReplayVerifier().Verify(factory, crossingRecording.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == crossing.Hash(), "actual crossing hide and denied trip inputs replay");
            }
            Both(c, game, twin, () => new HaulResourcesCommand { Source = source, Destination = target, Worker = worker, Resource = supply, Ceiling = Fix.FromInt(4) });
            bool loaded = false; watch.Restart(); updates = 0;
            while (updates < 128 && watch.ElapsedMilliseconds < 25000)
            {
                var cargo = LoadPolicyDisclosureTests.Stock(game, worker, supply); var delivered = LoadPolicyDisclosureTests.Stock(game, target, supply);
                loaded |= cargo == Fix.FromInt(4);
                c.Equal(Fix.FromInt(17), LoadPolicyDisclosureTests.Stock(game, source, supply) + cargo + delivered, "revealed delivery conserves physical stock");
                if (delivered == Fix.FromInt(4) && cargo.IsZero && !PermanentRemovalTests.Jobs(game).Any(row => row.IsHaul && row.Worker == worker)) break;
                PermanentRemovalTests.Step(game, twin); updates++;
            }
            c.That(loaded && updates < 128 && watch.ElapsedMilliseconds < 25000, "revealed serviced worker really loads travels and delivers");
            c.Equal(Fix.FromInt(13), LoadPolicyDisclosureTests.Stock(game, source, supply), "service and delivery have exact material accounting");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "private-to-revealed physical continuation checkpoints");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "actual hide work reveal and physical delivery replay");
            c.Equal(parentHash, parent.Hash(), "branch diagnostics leave canonical physical-service session untouched");
        }
        private static void Run(Check c)
        {
            var factory = PhysicalTripServiceTests.Factory(new GameModule[] { new InventorySightModule() },
                Json.Parse("[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]"),
                scenario => scenario.Get("settings").Set(InventorySightModule.ModuleId,
                    Json.Parse("{\"target_kind\":\"activity\",\"target\":\"trip.physical-service\",\"initial_hidden\":false}")));
            PhysicalTripServiceTests.Physical(c, factory, (actual, game, worker, source, target, supply) => Verify(c, actual, game, worker, source, target, supply));
        }
    }
}
