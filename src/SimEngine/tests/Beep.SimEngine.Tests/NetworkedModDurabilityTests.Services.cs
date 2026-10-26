using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Cadence = Beep.SimEngine.Tests.FleetServiceTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string FleetServiceCase = "Networked fleet service: saved earned cadence, multi-source deadlines and queued physical cargo/service";
        private static EntityId[] ServiceActors(SimGame game) => new[] { FleetEntity(game, "fleet.trip-native"), FleetEntity(game, "fleet.trip-public") };
        private static Code[] ServiceSources() => new[] { Cadence.Service, Cadence.Inspection };
        private static string ServiceDiagnostics(SimGame game) => TripDiagnostics(game) + "/cadence=" + string.Join(";", ServiceActors(game).SelectMany(actor =>
            ServiceSources().Select(source => { var value = Cadence.Read(game, actor, source); return actor + "/" + source + "/" + value.Schedule.LastPlan +
                "/" + value.Schedule.LastCompletedPlan + "/" + value.Blocker; })));
        private static void ServiceObserve(SimGame game)
        {
            TripObserve(game, 60); var current = new HashSet<long>();
            foreach (var actor in ServiceActors(game)) foreach (var source in ServiceSources()) {
                var value = Cadence.Read(game, actor, source);
                if (value.Schedule.LastPlan > 0) {
                    var plan = Cadence.Plan(game, value.Schedule.LastPlan);
                    if (!current.Add(plan.Id) || plan.Origin.Entity != actor || plan.Origin.Source != source)
                        throw new InvalidOperationException("service source borrowed/duplicated another origin's plan");
                }
                if (value.Schedule.LastCompletedPlan > 0) {
                    var plan = Cadence.Plan(game, value.Schedule.LastCompletedPlan);
                    if (plan.State != PlanState.Done || plan.Origin.Entity != actor || plan.Origin.Source != source || value.LastCompletedTick != plan.EndedTick)
                        throw new InvalidOperationException("service source invented earned completion");
                }
            }
        }
        private static Pair ServiceRestore(Check c, GameFactory factory, Pair source)
            => FleetRestore(c, factory, source, ServiceObserve, ServiceDiagnostics);
        private static (long Plan, int Order, EntityId Worker, ActivityNodeView Node)? ServicePrefix(SimGame game)
        {
            foreach (var actor in ServiceActors(game)) foreach (var source in ServiceSources()) {
                long id = Cadence.Read(game, actor, source).Schedule.LastPlan; if (id == 0) continue;
                var plan = Cadence.Plan(game, id); var nodes = new List<ActivityNodeView>();
                if (plan.Order == 0 || !game.Seat(0).View.Feature<WorkView>().TryCopyNodes(plan.Order, nodes)) continue;
                var node = nodes.Single(n => n.Id == "inspect");
                if (node.ProductiveSteps > 0 && node.ProductiveSteps < node.RequiredTicks && node.State != NodeState.Done) return (id, plan.Order, actor, node);
            }
            return null;
        }
        private static bool ServiceFirstDone(SimGame game)
            => ServiceActors(game).All(actor => ServiceSources().All(source => Cadence.Read(game, actor, source).Schedule.LastCompletedPlan > 0));
        private static ActivityNodeView? ServiceSettlePrefix(SimGame game)
        {
            foreach (var actor in ServiceActors(game)) foreach (var source in ServiceSources()) {
                long id = Cadence.Read(game, actor, source).Schedule.LastPlan; if (id == 0) continue;
                var plan = Cadence.Plan(game, id); var nodes = new List<ActivityNodeView>();
                if (plan.Order == 0 || !game.Seat(0).View.Feature<WorkView>().TryCopyNodes(plan.Order, nodes)) continue;
                var inspect = nodes.Single(n => n.Id == "inspect"); var settle = nodes.Single(n => n.Id == "settle");
                if (inspect.State == NodeState.Done && settle.ProductiveSteps > 0 && settle.ProductiveSteps < settle.RequiredTicks && settle.State != NodeState.Done) return settle;
            }
            return null;
        }
        private static void FleetServiceRun(Check c)
        {
            var factory = FleetNavigationTests.Factory(named: true, serviceControls: true, recordFlows: true);
            var pair = FleetPair(LiveSensorTests.New(factory), LiveSensorTests.New(factory), ServiceObserve, ServiceDiagnostics);
            Console.WriteLine("MODULES fleet-service=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId), "physical service schedules require no business or platform");
            var actors = ServiceActors(pair.A); var native = actors[0]; var external = actors[1];
            var limited = FleetEntity(pair.A, "fleet.restricted"); var source = FleetEntity(pair.A, "survey.site"); var target = FleetEntity(pair.A, "fleet.destination");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            Pair restored = null; int issued = 0;
            int Input(Command command, Code expected = default, int seat = 0) {
                int ticket = Submit(c, pair, command, expected, seat); issued++;
                if (restored != null) c.Equal(ticket, Submit(c, restored, command, expected, seat), "actual saved service command identity");
                bool Answered(SimGame game) => Result(game, seat, ticket)?.Refusal == expected;
                if (restored == null) Reach(pair, Answered, "ordinary service player intent"); else Both(pair, restored, Answered, "same actual service command on saved peers");
                return ticket;
            }
            var workers = pair.A.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0) &&
                pair.A.Seat(0).View.Feature<WorkView>().ReadWorkerExecution(e.Id, out _).IsNone).Select(e => e.Id).ToArray();
            foreach (var worker in workers) Input(new SetWorkerHeldCommand { Worker = worker, Held = true });
            Input(Cadence.MembershipCommand(new[] { native, limited }));
            Input(Cadence.Control(Cadence.Service, Cadence.Settings(true, 110, GameDuration.Minutes(200))), RecurringPlanCodes.NotSource);
            c.That(!Cadence.Read(pair.A, native, Cadence.Service).Schedule.Enabled, "missing source refuses whole edit without changing valid native prefix");
            Input(Cadence.MembershipCommand(actors.Reverse().ToArray()));
            Input(Cadence.Control(Cadence.Service, Cadence.Settings(true, 110, GameDuration.Minutes(200))), Codes.NotFound, 1);
            Input(Cadence.Control(Cadence.Service, Cadence.Settings(true, 110, GameDuration.Decimal("0.5", DurationUnit.Minute))), RecurringPlanCodes.Interval);
            int same = Input(Cadence.Control(Cadence.Service, Cadence.Settings(false, 100, GameDuration.Minutes(200))));
            c.That(Result(pair.A, 0, same).Value.Arg0 == 2 && Result(pair.A, 0, same).Value.Arg1 == 0, "identical whole cadence edit changes no source owner");
            var before = pair.A.Seat(0).View.Feature<FleetView>().PreviewRecurringPlan("service", Cadence.Service, Cadence.Settings(true, 110, GameDuration.Minutes(200)));
            Input(Cadence.Control(Cadence.Service, Cadence.Settings(true, 110, GameDuration.Minutes(200))));
            Input(Cadence.Control(Cadence.Inspection, Cadence.Settings(true, 90, GameDuration.Minutes(300))));
            c.That(actors.All(actor => ServiceSources().All(capability => Cadence.Read(pair.A, actor, capability).Schedule.LastCompletedPlan == 0)) &&
                actors.All(actor => FleetHome(pair.A, actor).State == WorkerHomeState.Held && FleetHome(pair.A, actor).Home == source), "enabled intent keeps real held/home state and grants no earned service");
            restored = ServiceRestore(c, factory, pair); int start = issued;
            var startLeft = restored.A.RecordReplay(limits); var startRight = restored.B.RecordReplay(limits);
            Input(new SetWorkerHeldCommand { Worker = native, Held = false }); Input(new SetWorkerHeldCommand { Worker = external, Held = false });
            Both(pair, restored, game => ServicePrefix(game).HasValue, "original bound source requires physical travel and positive partial six-minute work");
            var prefix = ServicePrefix(pair.A).Value;
            c.That(prefix.Node.Worker == prefix.Worker && prefix.Node.RequiredTicks == pair.A.Clock.Time.ExactTicks(GameDuration.Minutes(6)), "original graph binds actual source worker and duration");
            var posted = actors.SelectMany(actor => ServiceSources().Select(capability => (Actor: actor, Source: capability, Value: Cadence.Read(pair.A, actor, capability).Schedule))).ToArray();
            Input(Cadence.Control(Cadence.Service, Cadence.Settings(true, 140, GameDuration.Minutes(200))));
            c.That(posted.All(p => Cadence.Read(pair.A, p.Actor, p.Source).Schedule.LastPlan == p.Value.LastPlan &&
                Cadence.Read(pair.A, p.Actor, p.Source).Schedule.LastPostedTick == p.Value.LastPostedTick), "future priority edit resets no existing cycle or post time");
            c.That(Cadence.Plan(pair.A, prefix.Plan).Order == prefix.Order &&
                (Cadence.Plan(pair.A, prefix.Plan).State == PlanState.Active || Cadence.Plan(pair.A, prefix.Plan).State == PlanState.Done), "actual partial execution is retained or genuinely finished, never replaced");
            ReplayPair(c, factory, startLeft, startRight, restored, limits, issued - start, "saved held sources through actual partial work and future priority edit replay");
            restored = ServiceRestore(c, factory, pair);
            var workLeft = restored.A.RecordReplay(limits); var workRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native sole-clock pause during real partial service");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 8, "other saved service peers earn actual work independently");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen, "paused original borrows no work, service history or fuel");
            pair.A.Net.RequestResume(); Both(pair, restored, game => game.Clock.StepIndex >= paused + 8, "native resumed partial service converges at common actual epoch");
            Both(pair, restored, game => ServiceSettlePrefix(game).HasValue, "original dependent settling stage earns positive time only after staffed inspection");
            c.Equal(pair.A.Clock.Time.ExactTicks(GameDuration.Minutes(3)), ServiceSettlePrefix(pair.A).Value.RequiredTicks, "original authored three-minute settling stage remains unchanged");
            ReplayPair(c, factory, workLeft, workRight, restored, limits, 0, "saved staffed prefix through isolated work and positive dependent settling replay");
            restored = ServiceRestore(c, factory, pair);
            var settleLeft = restored.A.RecordReplay(limits); var settleRight = restored.B.RecordReplay(limits);
            Both(pair, restored, ServiceFirstDone, "both sources on both native/independent machines earn first actual service");
            foreach (var actor in actors) foreach (var capability in ServiceSources()) {
                var reading = Cadence.Read(pair.A, actor, capability); var plan = Cadence.Plan(pair.A, reading.Schedule.LastCompletedPlan);
                c.That(plan.State == PlanState.Done && plan.EndedTick > plan.CreatedTick && plan.Origin.Entity == actor && plan.Origin.Source == capability,
                    "each named source earns its own physical completion and actual origin");
                c.Equal(plan.EndedTick + pair.A.Clock.Time.ExactTicks(reading.Schedule.Interval), reading.ServiceDueTick.Value, "service deadline derives from actual earned completion on sole clock");
            }
            ReplayPair(c, factory, settleLeft, settleRight, restored, limits, 0, "saved dependent settling through four real complete service cycles replay");
            restored = ServiceRestore(c, factory, pair);
            var intervalLeft = restored.A.RecordReplay(limits); var intervalRight = restored.B.RecordReplay(limits);
            var earned = actors.Select(actor => Cadence.Read(pair.A, actor, Cadence.Service).Schedule).ToArray();
            Input(Cadence.Control(Cadence.Service, Cadence.Settings(true, 150, GameDuration.Decimal("400.0", DurationUnit.Minute))));
            for (int i = 0; i < actors.Length; i++) {
                var value = Cadence.Read(pair.A, actors[i], Cadence.Service);
                c.That(value.Schedule.LastPlan == earned[i].LastPlan && value.Schedule.LastCompletedPlan == earned[i].LastCompletedPlan &&
                    value.Schedule.LastPostedTick == earned[i].LastPostedTick, "interval edit preserves earned plan/completion/post history");
                c.Equal(value.LastCompletedTick.Value + pair.A.Clock.Time.ExactTicks(GameDuration.Minutes(400)), value.ServiceDueTick.Value, "edited interval derives new deadline from original success");
                c.Equal(pair.A.Clock.Time.ExactTicks(GameDuration.Minutes(300)), pair.A.Clock.Time.ExactTicks(Cadence.Read(pair.A, actors[i], Cadence.Inspection).Schedule.Interval), "other named source cadence stays unchanged");
                var earliest = ServiceSources().Select(capability => Cadence.Read(pair.A, actors[i], capability)).OrderBy(v => v.ServiceDueTick).ThenBy(v => v.Source.Id, StringComparer.Ordinal).First();
                c.That(Cadence.Window(pair.A, actors[i]).Source == earliest.Source && Cadence.Window(pair.A, actors[i]).DueUpdateStep == earliest.ServiceDueTick,
                    "native trip policy selects actual earliest multi-source service deadline");
            }
            ReplayPair(c, factory, intervalLeft, intervalRight, restored, limits, 1, "saved earned service through exact interval and multi-source deadline edit replay");
            restored = ServiceRestore(c, factory, pair); int routeStart = issued;
            var routeLeft = restored.A.RecordReplay(limits); var routeRight = restored.B.RecordReplay(limits);
            Input(new SetWorkerHeldCommand { Worker = native, Held = true }); Input(Cadence.MembershipCommand(new[] { external }));
            int ticket = Input(new AddRouteCommand { From = source, To = target, Resource = FleetSteel(pair.A), Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 100) });
            int route = checked((int)Result(pair.A, 0, ticket).Value.Arg0);
            Both(pair, restored, game => FleetJobs(game).Any(j => j.Worker == external), "earned service window funds actual independent physical cargo trip");
            var haul = FleetJobs(pair.A).Single(j => j.Worker == external);
            Input(new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(3), 100) });
            Both(pair, restored, game => FleetJobs(game).Any(j => j.Id == haul.Id && j.Loaded == Fix.FromInt(3)) &&
                FleetPosition(game, external).X > 10 && FleetPosition(game, external).X < 29, "physically loaded cargo travels before future service is shortened");
            c.Equal(Fix.Zero, FleetStock(pair.A, target).Stock, "service and route previews do not teleport steel");
            ReplayPair(c, factory, routeLeft, routeRight, restored, limits, issued - routeStart, "saved earned deadlines through service-funded actual loaded cargo replay");
            restored = ServiceRestore(c, factory, pair); int queueStart = issued;
            var queueLeft = restored.A.RecordReplay(limits); var queueRight = restored.B.RecordReplay(limits);
            long prior = Cadence.Read(pair.A, external, Cadence.Service).Schedule.LastCompletedPlan;
            Input(Cadence.Control(Cadence.Service, Cadence.Settings(true, 200, GameDuration.Minutes(1))));
            Both(pair, restored, game => Cadence.Read(game, external, Cadence.Service).Schedule.LastPlan > prior, "newly due service posts real intent behind already acquired cargo");
            long queued = Cadence.Read(pair.A, external, Cadence.Service).Schedule.LastPlan;
            c.That(FleetJobs(pair.A).Any(j => j.Id == haul.Id && j.Worker == external && j.Loaded == Fix.FromInt(3) && j.DestinationEndpoint == haul.DestinationEndpoint) &&
                FleetHome(pair.A, external).Job == haul.Id && FleetStock(pair.A, target).InboundPromised == Fix.FromInt(3), "new due source keeps actual hauling worker/cargo/promise intact");
            c.That(Cadence.Read(pair.A, external, Cadence.Service).Schedule.LastCompletedPlan == prior && Cadence.Plan(pair.A, queued).State != PlanState.Done, "posted due service is not earned completion");
            Input(Cadence.Control(Cadence.Service, Cadence.Settings(false, 210, GameDuration.Minutes(200))));
            c.That(Cadence.Read(pair.A, native, Cadence.Service).Schedule.Enabled &&
                pair.A.Clock.Time.ExactTicks(Cadence.Read(pair.A, native, Cadence.Service).Schedule.Interval) == pair.A.Clock.Time.ExactTicks(GameDuration.Minutes(400)) &&
                Cadence.Read(pair.A, external, Cadence.Service).Schedule.LastPlan == queued, "current membership edits no removed native source or retained queued intent");
            Input(new RemoveFleetCommand { Fleet = "service" });
            ReplayPair(c, factory, queueLeft, queueRight, restored, limits, issued - queueStart, "saved acquired cargo through due queued source/future disable and metadata removal replay");
            restored = ServiceRestore(c, factory, pair);
            var cargoLeft = restored.A.RecordReplay(limits); var cargoRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => FleetStock(game, target).Stock == Fix.FromInt(3) && !FleetJobs(game).Any(j => j.Id == haul.Id), "same acquired cargo finishes real unloading and departure before queued service");
            c.That(FleetStock(pair.A, source).Stock == Fix.FromInt(17) && !pair.A.Seat(0).View.Feature<FleetView>().TryRead("service", out _), "metadata-free physical delivery conserves original steel");
            ReplayPair(c, factory, cargoLeft, cargoRight, restored, limits, 0, "saved loaded cargo and queued disabled source through actual delivery replay");
            restored = ServiceRestore(c, factory, pair);
            var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => Cadence.Read(game, external, Cadence.Service).Schedule.LastCompletedPlan == queued, "same queued source physically travels and services after cargo delivery");
            var completed = Cadence.Plan(pair.A, queued); var final = Cadence.Read(pair.A, external, Cadence.Service);
            c.That(!final.Schedule.Enabled && final.LastCompletedTick > completed.CreatedTick && completed.State == PlanState.Done &&
                completed.Origin.Entity == external && completed.Origin.Source == Cadence.Service, "disabled future source acknowledges only real retained physical work");
            long finished = pair.A.Clock.StepIndex;
            Both(pair, restored, game => game.Clock.StepIndex >= finished + 3, "quiet updates post no disabled-future service replacement");
            c.That(Cadence.Read(pair.A, external, Cadence.Service).Schedule.LastPlan == queued && Cadence.Read(pair.A, external, Cadence.Service).Schedule.LastCompletedPlan == queued &&
                FleetStock(pair.A, target).Stock == Fix.FromInt(3) && FleetStock(pair.A, external).Stock.IsZero &&
                FleetStock(pair.A, target).InboundPromised.IsZero && FleetStock(pair.A, source).OutboundPromised.IsZero, "real retained completion settles once with no duplicate intent, cargo or promise");
            c.That(before.Entries.All(e => !e.Schedule.Value.Enabled && e.Schedule.Value.LastPlan == 0) && earned.All(v => v.LastCompletedPlan > 0), "old public source/history facts remain detached");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "original and restored service peers actually stall and reverse opaque packets");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 0, "saved delivered cargo and retained disabled source through real completion/quiet updates replay");
            ReplayPair(c, factory, left, right, pair, limits, issued, "full multi-source service/physical cargo sequence replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced service worlds converge");
            ServiceRestore(c, factory, pair); Console.WriteLine("SERVICE inputs=" + issued);
        }
    }
}
