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
    internal static class FleetServiceTests
    {
        internal const string Case = "Fleet service: whole cadence control, earned deadlines and physical saved cycles";
        internal static readonly Code Service = new Code("service.interval"), Inspection = new Code("service.inspect");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id);
        private static FleetView View(SimGame game, int company = 0) => game.Seat(company).View.Feature<FleetView>();
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        internal static RecurringPlanView Read(SimGame game, EntityId actor, Code source)
        {
            var refusal = game.Seat(0).View.Feature<WorkView>().ReadRecurringPlan(actor, source, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("service read refused: " + refusal); return value;
        }
        internal static PlanView Plan(SimGame game, long id)
        { var values = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(values); return values.Single(p => p.Id == id); }
        internal static RecurringPlanSettings Settings(bool enabled, int priority, GameDuration interval, Code facility = default)
            => new RecurringPlanSettings(enabled, priority, interval, facility);
        internal static ConfigureFleetRecurringPlanCommand Control(Code source, RecurringPlanSettings settings)
            => new ConfigureFleetRecurringPlanCommand { Fleet = "service", Source = source, Settings = settings };
        internal static ConfigureFleetCommand MembershipCommand(IReadOnlyList<EntityId> ids)
            => new ConfigureFleetCommand { Fleet = "service", Settings = new FleetSettings("fleet.haul", ids) };
        private static void Members(Check c, SimGame game, params EntityId[] ids)
            => c.That(TestKit.Order(game, 0, MembershipCommand(ids)).Accepted, "normal current membership");
        private static void Held(Check c, SimGame game, EntityId actor, bool held, SimGame twin = null)
        {
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor, Held = held }).Accepted, "normal held control");
            if (twin != null) c.That(TestKit.Order(twin, 0, new SetWorkerHeldCommand { Worker = actor, Held = held }).Accepted, "same saved held control");
        }
        private static Fix Total(SimGame game)
        {
            var steel = game.World.Content.ResourceId("steel"); Fix sum = Fix.Zero;
            foreach (var e in game.World.Entities.All) if (e.Get<IInventory>() is IInventory stock) sum += stock.Amount(steel); return sum;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("service clock=" + game.Clock.StepIndex + " " + game.Fault);
            if (Total(game) != Fix.FromInt(20)) throw new InvalidOperationException("service steel conservation=" + game.Clock.StepIndex);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1500 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " sources=" + string.Join(";",
                game.World.Entities.All.SelectMany(e => e.Components).OfType<IRecurringPlanSource>().Select(s => s.Entity.Id + "/" + s.CapabilityId + "/" + RecurringPlanContracts.Read(game.World, s).Blocker + "/" + s.ReadSchedule().LastPlan + "/" + s.ReadSchedule().LastCompletedPlan)) +
                " jobs=" + string.Join(";", Board(game).Jobs.Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.Loaded + "/" + j.Blocker)) +
                " positions=" + string.Join(";", game.World.Entities.All.Where(e => e.Get<IWorker>() != null).Select(e => e.Id + "/" + e.Get<ITilePresence>().Position + "/" + e.Get<IMotion>().Status + "/fuel=" + e.Get<IInventory>()?.Amount(game.World.Content.ResourceId("fleet.fuel")))));
        }
        private static void Apply(Check c, SimGame game, Code source, RecurringPlanSettings settings, SimGame twin = null)
        {
            c.Equal(Code.None, View(game).PreviewRecurringPlan("service", source, settings).Blocker, "private named whole admission");
            c.That(TestKit.Order(game, 0, Control(source, settings)).Accepted, "normal recurring fleet command");
            if (twin != null)
            { c.That(TestKit.Order(twin, 0, Control(source, settings)).Accepted, "same saved recurring fleet intent"); c.Equal(game.Hash(), twin.Hash(), "saved original source/work owners identical"); }
        }
        internal static ServiceMovementWindow Window(SimGame game, EntityId actor)
        {
            var refusal = game.Seat(0).View.Feature<LogisticsView>().ReadHaulTripServiceWindow(actor, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("trip service read refused: " + refusal); return value;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current fleet cadence identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no copied fleet timing payload");
            var factory = FleetNavigationTests.Factory(named: true, serviceControls: true); var game = LiveSensorTests.New(factory);
            var native = Find(game, "fleet.trip-native"); var external = Find(game, "fleet.trip-public");
            var actors = new[] { native, external }.OrderBy(e => e.Id).ToArray(); var ids = actors.Select(e => e.Id).ToArray();
            var unsupported = Find(game, "fleet.restricted"); var foreign = Find(game, "survey.foreign-sensor");
            var source = Find(game, "survey.site"); var target = Find(game, "fleet.destination"); var steel = game.World.Content.ResourceId("steel");
            foreach (var e in game.World.Entities.All.Where(e => e.Owner == new PlayerId(0) && e.Get<IWorker>() != null)) Held(c, game, e.Id, true);
            c.That(native.Get<IRecurringPlanSource>(Service) is RecurringActivitySource && external.Get<IRecurringPlanSource>(Service) is ProofRecurringSource, "original native and independent public cadence providers");
            c.That(native.Get<IRecurringPlanSource>(Inspection) is ProofRecurringSource && external.Get<IRecurringPlanSource>(Inspection) is RecurringActivitySource, "second named source uses cross-provider composition");
            c.That(native.Def.AuthoringPath.Contains("native-fleet-service-policies.json") && external.Def.AuthoringPath.Contains("fleet-service-machines.json"), "real master-listed service definitions");
            c.That(!game.World.Services.Has<IEconomy>(), "machine service and hauling independent of money");
            c.Equal(7, game.Seat(0).View.FeatureRegistrations.Single(r => r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "private cadence view revision");
            c.Equal(1, game.Seat(0).View.StateCodecs.Single(r => r.Kind == StateCodecKind.Service && r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "membership-only original codec");
            var initial = Settings(false, 100, GameDuration.Minutes(200)); var requested = Settings(true, 110, GameDuration.Minutes(200));
            Members(c, game, native.Id, unsupported.Id); ulong hash = game.Hash(); var rejected = View(game).PreviewRecurringPlan("service", Service, requested);
            c.Equal(RecurringPlanCodes.NotSource, rejected.Blocker, "missing source refuses whole selection"); c.That(!rejected.Entries.Single(e => e.Entity == unsupported.Id).Schedule.HasValue, "no fabricated schedule"); c.Equal(hash, game.Hash(), "whole refusal pure");
            c.Equal(RecurringPlanCodes.NotSource, TestKit.Order(game, 0, Control(Service, requested)).Refusal, "normal whole refusal parity");
            c.That(!Read(game, native.Id, Service).Schedule.Enabled, "valid native prefix unchanged");
            Members(c, game, ids.Reverse().ToArray()); hash = game.Hash(); var before = View(game).PreviewRecurringPlan("service", Service, requested);
            c.That(before.Entries.Select(e => e.Entity).SequenceEqual(ids) && before.Entries.All(e => e.Source == Service && e.Schedule.HasValue && !e.Schedule.Value.Enabled && e.Schedule.Value.LastPlan == 0), "canonical explicit-source original settings and no unearned service");
            c.Throws<NotSupportedException>(() => ((IList<RecurringPlanSelectionEntry>)before.Entries).Clear(), "detached immutable schedule selection");
            foreach (var malformed in new[] { null, Array.Empty<EntityId>(), new[] { ids[0], ids[0] }, new[] { ids[0], EntityId.None } })
                c.Equal(Codes.InvalidArgument, RecurringPlanContracts.PreviewGroup(game.World, new PlayerId(0), malformed, Service, requested).Blocker, "malformed selection refuses");
            c.Equal(Codes.NotSeated, RecurringPlanContracts.PreviewGroup(game.World, PlayerId.None, ids, Service, requested).Blocker, "observer no mutation authority");
            c.Equal(Codes.InvalidArgument, View(game).PreviewRecurringPlan("service", Code.None, requested).Blocker, "explicit source required");
            c.Equal(RecurringPlanCodes.NotSource, View(game).PreviewRecurringPlan("service", new Code("unknown.source"), requested).Blocker, "no substitute source");
            c.Equal(Codes.InvalidArgument, View(game).PreviewRecurringPlan("service", Service, null).Blocker, "no default settings");
            c.Equal(RecurringPlanCodes.Interval, View(game).PreviewRecurringPlan("service", Service, Settings(true, 1, GameDuration.Decimal("0.5", DurationUnit.Minute))).Blocker, "unrepresentable source cadence refuses");
            c.Equal(ActivityFacilityCodes.NotSelected, View(game).PreviewRecurringPlan("service", Service, Settings(true, 1, GameDuration.Minutes(200), new Code("unknown.facility"))).Blocker, "worker-home binding cannot receive unrelated facility policy");
            c.Equal(Codes.NotFound, View(game, 1).PreviewRecurringPlan("service", Service, requested).Blocker, "private company fleet controls");
            var hidden = RecurringPlanContracts.PreviewGroup(game.World, new PlayerId(0), new[] { foreign.Id }, Service, requested);
            c.That(hidden.Blocker == Codes.NotOwned && !hidden.Entries[0].Schedule.HasValue, "foreign schedule never read");
            c.Equal(hash, game.Hash(), "settings admission does not mutate history/jobs/clock");
            var versions = actors.Select(e => e.Version).ToArray(); var same = TestKit.Order(game, 0, Control(Service, initial));
            c.That(same.Accepted && same.Arg0 == 2 && same.Arg1 == 0 && actors.Select(e => e.Version).SequenceEqual(versions), "identical native/public cadence no Touch");
            game.Seat(0).Submit(Control(Service, requested)); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending explicit source/settings saved"); Update(game, twin); c.Equal(game.Hash(), twin.Hash(), "same pending command updates actual sources");
            c.That(before.Entries.All(e => !e.Schedule.Value.Enabled && e.Schedule.Value.LastPlan == 0), "previous schedule facts stay frozen");
            c.That(actors.All(e => e.Get<IWorker>().Held && e.Get<IWorker>().Job == 0 && WorkerHomeContracts.ReadHome(game.World, e) == source.Id), "settings preserve actual held/job/home");
            Apply(c, game, Inspection, Settings(true, 90, GameDuration.Minutes(300)), twin);
            Held(c, game, native.Id, false, twin); Held(c, game, external.Id, false, twin);
            Reach(game, () => actors.Any(e => e.Get<IWorker>().Job != 0 && e.Get<IMotion>().Status == MoveStatus.Moving), "actual bound service travel", twin);
            c.That(actors.All(e => Read(game, e.Id, Service).Schedule.LastCompletedPlan == 0), "travel does not earn instant completion");
            Reach(game, () => Board(game).Orders.Any(o => o.Plan.PlanKey == "activity:fleet.inspection" && o.Node("inspect").Progress.WholeTicks > 0 && o.Node("inspect").Progress.WholeTicks < 3), "positive partial inspection work", twin);
            var active = Board(game).Orders.First(o => o.Plan.PlanKey == "activity:fleet.inspection" && o.Node("inspect").Progress.WholeTicks > 0 && o.Node("inspect").Progress.WholeTicks < 3);
            var snapshots = actors.Select(e => Read(game, e.Id, Service).Schedule).ToArray(); int order = active.Id;
            Apply(c, game, Service, Settings(true, 140, GameDuration.Minutes(200)), twin);
            c.That(Board(game).Order(order) != null && active.Node("inspect").Progress.WholeTicks > 0, "future priority edit retains actual physical work");
            for (int i = 0; i < actors.Length; i++) c.That(Read(game, actors[i].Id, Service).Schedule.LastPlan == snapshots[i].LastPlan && Read(game, actors[i].Id, Service).Schedule.LastPostedTick == snapshots[i].LastPostedTick, "edit does not reset existing cycle identity/post time");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial actual service/progress/settings saved");
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex; Update(game, twin); c.Equal(paused, game.Clock.StepIndex, "sole clock pause freezes service progress"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, () => actors.All(e => new[] { Service, Inspection }.All(s => Read(game, e.Id, s).Schedule.LastCompletedPlan > 0)), "both sources both physical first services", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved original source/work service completion identity");
            foreach (var actor in actors)
                foreach (var capability in new[] { Service, Inspection })
                {
                    var reading = Read(game, actor.Id, capability); var plan = Plan(game, reading.Schedule.LastCompletedPlan);
                    c.That(plan.State == PlanState.Done && plan.EndedTick > plan.CreatedTick && plan.Origin.Entity == actor.Id && plan.Origin.Source == capability, "each source earns actual whole physical completion and proper origin");
                    c.Equal(plan.EndedTick, reading.LastCompletedTick.Value, "original board alone owns actual success time");
                    c.Equal(plan.EndedTick + game.World.Time.ExactTicks(reading.Schedule.Interval), reading.ServiceDueTick.Value, "deadline anchored in real completion");
                }
            snapshots = actors.Select(e => Read(game, e.Id, Service).Schedule).ToArray();
            Apply(c, game, Service, Settings(true, 150, GameDuration.Decimal("400.0", DurationUnit.Minute)), twin);
            for (int i = 0; i < actors.Length; i++)
            {
                var reading = Read(game, actors[i].Id, Service);
                c.That(reading.Schedule.LastPlan == snapshots[i].LastPlan && reading.Schedule.LastCompletedPlan == snapshots[i].LastCompletedPlan && reading.Schedule.LastPostedTick == snapshots[i].LastPostedTick, "interval edit preserves earned history rather than posting/resetting");
                c.Equal(reading.LastCompletedTick.Value + 400, reading.ServiceDueTick.Value, "exact edited interval derives from original completion");
                c.Equal(300L, game.World.Time.ExactTicks(Read(game, actors[i].Id, Inspection).Schedule.Interval), "other named source unaffected");
                var earliest = new[] { Read(game, actors[i].Id, Service), Read(game, actors[i].Id, Inspection) }.OrderBy(v => v.ServiceDueTick).ThenBy(v => v.Source.Id, StringComparer.Ordinal).First();
                c.That(Window(game, actors[i].Id).Source == earliest.Source && Window(game, actors[i].Id).DueUpdateStep == earliest.ServiceDueTick, "original trip policy chooses earliest actual multi-source deadline");
            }
            Held(c, game, native.Id, true, twin); Members(c, game, external.Id); Members(c, twin, external.Id);
            var result = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 100) });
            var other = TestKit.Order(twin, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 100) });
            c.That(result.Accepted && other.Accepted && result.Arg0 == other.Arg0, "normal physical cargo route after earned service"); int route = checked((int)result.Arg0);
            Reach(game, () => Board(game).Jobs.Any(j => j.IsHaul && j.Worker == external.Id), "service-funded public haul", twin); var haul = Board(game).Jobs.Single(j => j.IsHaul);
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(3), 100) }).Accepted, "disable future route intake");
            TestKit.Order(twin, 0, new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(3), 100) });
            Reach(game, () => haul.Loaded == haul.Amount && haul.Toils[haul.ToilIndex].Executor == WorkPhases.Travel && haul.Toils[haul.ToilIndex].At == target.Id, "fully loaded public travel", twin);
            var receipt = haul.InReservation; var cargoReceipt = haul.CargoOutReservation; long prior = Read(game, external.Id, Service).Schedule.LastCompletedPlan;
            Apply(c, game, Service, Settings(true, 200, GameDuration.Minutes(1)), twin);
            Reach(game, () => Read(game, external.Id, Service).Schedule.LastPlan > prior, "new due service queues behind actual busy carrier", twin); long queued = Read(game, external.Id, Service).Schedule.LastPlan;
            c.That(haul.IsLive && external.Get<IWorker>().Job == haul.Id && haul.InReservation.Equals(receipt) && haul.CargoOutReservation.Equals(cargoReceipt), "new due source retains acquired physical hauling and promises");
            c.That(Read(game, external.Id, Service).Schedule.LastCompletedPlan == prior && Plan(game, queued).State != PlanState.Done, "posting no fictional service completion");
            Apply(c, game, Service, Settings(false, 210, GameDuration.Minutes(200)), twin);
            c.That(Read(game, native.Id, Service).Schedule.Enabled && game.World.Time.ExactTicks(Read(game, native.Id, Service).Schedule.Interval) == 400, "current public-only membership leaves native settings unchanged");
            c.Equal(queued, Read(game, external.Id, Service).Schedule.LastPlan, "disabled future scheduling does not cancel queued actual service");
            c.That(TestKit.Order(game, 0, new RemoveFleetCommand { Fleet = "service" }).Accepted && TestKit.Order(twin, 0, new RemoveFleetCommand { Fleet = "service" }).Accepted, "remove fleet metadata, retain physical owners");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "loaded cargo/queued disabled service saved together");
            Reach(game, () => haul.State == JobState.Done, "retained actual cargo delivery and departure", twin);
            c.That(target.Get<IInventory>().Amount(steel) == Fix.FromInt(3) && source.Get<IInventory>().Amount(steel) == Fix.FromInt(17), "service never teleports or duplicates cargo");
            Reach(game, () => Read(game, external.Id, Service).Schedule.LastCompletedPlan == queued, "queued source physically services after carrier delivery", twin);
            c.That(!Read(game, external.Id, Service).Schedule.Enabled && Read(game, external.Id, Service).LastCompletedTick > Plan(game, queued).CreatedTick, "disabled future source still acknowledges real retained work");
            c.Equal(game.Hash(), twin.Hash(), "saved physical service and cargo continuation identical"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final roundtrip");
            var isolated = LiveSensorTests.New(factory); c.That(!View(isolated).TryRead("service", out _) && !Read(isolated, Find(isolated, "fleet.trip-public").Id, Service).Schedule.Enabled, "session isolation actual membership/settings");
            Members(c, isolated); c.Equal(FleetCodes.Empty, View(isolated).PreviewRecurringPlan("service", Service, requested).Blocker, "explicit empty fleet refuses control");
            c.That(!LiveSensorTests.New(FleetNavigationTests.Factory()).World.Services.Has<IMachineFleets>(), "optional omission no hidden fleet service owner"); Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (var mode in new[] { "missing-interval", "missing-cadence", "extra", "unknown-plan", "unaligned" })
            {
                var doc = Content.Json.Parse(FrameworkProofContent.Read("native-fleet-service-policies.json")); var settings = doc.Get("entities")[0].Get("components").Get("recurring_activity");
                if (mode == "missing-interval") settings.Remove("interval"); if (mode == "missing-cadence") settings.Remove("cadence");
                if (mode == "extra") settings.Set("instant_service", JsonValue.Of(true)); if (mode == "unknown-plan") settings.Set("plan", JsonValue.Of("activity:unknown"));
                if (mode == "unaligned") settings.Set("interval", Content.Json.Parse("{\"numerator\":1,\"denominator\":2,\"unit\":\"minute\"}"));
                c.Throws<ContentException>(() => LiveSensorTests.New(FleetNavigationTests.Factory(named: true, serviceControls: true, servicePolicies: doc.ToString())), "strict JSON refuses " + mode);
            }
            var defect = Content.Json.Parse(FrameworkProofContent.Read("fleet-service-machines.json"));
            defect.Get("entities")[0].Get("components").Get("proof_fleet_service").Set("fault", JsonValue.Number("6"));
            var game = LiveSensorTests.New(FleetNavigationTests.Factory(named: true, serviceControls: true, proofService: defect.ToString())); var actor = Find(game, "fleet.trip-public");
            c.Throws<InvalidOperationException>(() => RecurringPlanContracts.ConfigureGroup(game.World, new PlayerId(0), new[] { actor.Id }, Service, Settings(false, 120, GameDuration.Minutes(400))), "public lying interval commit faults original selection");
            c.That(game.Fault != null, "original session fail-stop"); c.Throws<InvalidOperationException>(() => game.Save(), "lying cadence provider cannot healthy checkpoint");
        }
    }
}
