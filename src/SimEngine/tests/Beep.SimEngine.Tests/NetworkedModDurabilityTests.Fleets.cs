using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Batching = Beep.SimEngine.Tests.FleetLoadTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string FleetBatchCase = "Networked named fleets: saved whole batching, original wait age and acquired native/contract cargo";
        private static EntityId FleetEntity(SimGame game, string definition)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Owner == new PlayerId(0) && e.Def == definition).Id;
        private static ResourceId FleetSteel(SimGame game)
        { if (!game.Seat(0).View.TryFindResource("steel", out var value)) throw new InvalidOperationException("authored fleet steel absent"); return value; }
        private static LogisticsOperationalSnapshot FleetStock(SimGame game, EntityId id)
            => FleetStock(game, id, FleetSteel(game));
        private static LogisticsOperationalSnapshot FleetStock(SimGame game, EntityId id, ResourceId resource)
        {
            Code refusal = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(id, resource, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("owned fleet inventory/" + refusal); return value;
        }
        private static List<WorkJobView> FleetJobs(SimGame game)
        { var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows.Where(j => j.IsHaul).ToList(); }
        private static WorkerHomeView FleetHome(SimGame game, EntityId worker)
        {
            Code refusal = game.Seat(0).View.Feature<WorkView>().ReadHome(worker, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual fleet home/" + refusal); return value;
        }
        private static HaulLoadPolicySnapshot FleetLoad(SimGame game, EntityId worker)
        {
            Code refusal = game.Seat(0).View.Feature<LogisticsView>().ReadHaulLoadPolicy(worker, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual fleet load policy/" + refusal); return value;
        }
        private static TileCoord FleetPosition(SimGame game, EntityId worker)
            => ((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == worker).Spatial).Tile;
        private static string FleetDiagnostics(SimGame game) => "at=" + game.Clock.StepIndex + "/jobs=" + string.Join(";", FleetJobs(game).Select(j =>
            j.Id + "/" + j.Worker + "/" + j.ToilIndex + "/" + j.Loaded + "/" + j.Blocker)) + "/fleets=" + FleetMembers(game);
        private static string FleetMembers(SimGame game)
        { var rows = new List<FleetSnapshot>(); Batching.View(game).CopyFleets(rows); return string.Join(";", rows.Select(f => f.Id + ":" + string.Join(",", f.Settings.Members))); }
        private static Pair FleetPair(SimGame a, SimGame b, Action<SimGame> additionalObserve = null, Func<SimGame, string> additionalDescribe = null)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long before) && before == game.Clock.StepIndex) return;
                var steel = FleetSteel(game); Fix total = Fix.Zero;
                foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0)))
                    if (game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, steel, out var stock).IsNone) total += stock.Stock;
                if (total != Fix.FromInt(20)) throw new InvalidOperationException("fleet work violated finite twenty-unit conservation/" + FleetDiagnostics(game));
                var privateRows = new List<FleetSnapshot>(); Batching.View(game, 1).CopyFleets(privateRows);
                if (privateRows.Count != 0 || Batching.View(game, 1).TryRead("delivery", out _)) throw new InvalidOperationException("fleet membership leaked to another company");
                var assigned = FleetJobs(game).Where(j => j.Worker.IsValid).ToArray();
                if (assigned.Select(j => j.Worker).Distinct().Count() != assigned.Length || assigned.Any(j => j.Purchase))
                    throw new InvalidOperationException("fleet policy fabricated duplicate assignment/purchase");
                additionalObserve?.Invoke(game);
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, additionalDescribe ?? FleetDiagnostics, 0);
        }
        private static Pair FleetRestore(Check c, GameFactory factory, Pair source, Action<SimGame> additionalObserve = null, Func<SimGame, string> additionalDescribe = null)
        {
            var pair = FleetPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()), additionalObserve, additionalDescribe);
            c.Equal(source.A.Hash(), pair.A.Hash(), "fleet metadata/component policies/physical jobs restore together");
            c.Equal(source.B.Hash(), pair.B.Hash(), "saved other peer reattaches exact native state");
            return pair;
        }
        private static void FleetBatchRun(Check c)
        {
            var factory = FleetNavigationTests.Factory(named: true, loadControls: true);
            var pair = FleetPair(LiveSensorTests.New(factory), LiveSensorTests.New(factory));
            Console.WriteLine("MODULES fleet-batching=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Modules.Contains(MachineFleetsModule.ModuleId) && !pair.A.Modules.Contains(TycoonModule.ModuleId), "original fleet module requires no business or platform");
            var native = FleetEntity(pair.A, "fleet.load-native"); var external = FleetEntity(pair.A, "fleet.load-public");
            var limited = FleetEntity(pair.A, "fleet.restricted"); var source = FleetEntity(pair.A, "survey.site"); var target = FleetEntity(pair.A, "fleet.destination");
            var steel = FleetSteel(pair.A); var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits); Pair restored = null; int issued = 0;
            int Input(Command command, Code expected = default, int seat = 0) {
                int ticket = Submit(c, pair, command, expected, seat); issued++;
                if (restored != null) c.Equal(ticket, Submit(c, restored, command, expected, seat), "actual saved fleet command identity");
                bool Answered(SimGame game) => Result(game, seat, ticket)?.Refusal == expected;
                if (restored == null) Reach(pair, Answered, "normal fleet player intent"); else Both(pair, restored, Answered, "normal fleet intent on saved peers");
                return ticket;
            }
            HaulLoadSettings Settings(bool enabled, int minimum, GameDuration wait, bool bypass = false, string profile = "fleet.batch")
                => Batching.Settings(steel, enabled, minimum, wait, bypass, profile);
            int Membership(params EntityId[] members) => Input(Batching.MembershipCommand(members));
            void Route(int id, bool enabled, int amount) => Input(new ConfigureRouteCommand { Route = id, Settings = new LogisticsRouteSettings(enabled, Fix.FromInt(amount), 100) });
            bool HasJob(SimGame game, int route, EntityId worker) => FleetJobs(game).Any(j => j.Route == route && j.Worker == worker);
            WorkJobView Job(SimGame game, int id) => FleetJobs(game).Single(j => j.Id == id);
            bool InTransit(SimGame game, int id, EntityId worker) => FleetJobs(game).Any(j => j.Id == id && j.Loaded == j.Amount) &&
                FleetPosition(game, worker).X > 10 && FleetPosition(game, worker).X < 29 && FleetStock(game, target).Stock == (worker == external ? Fix.Zero : Fix.FromInt(3));
            var workers = pair.A.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0) &&
                pair.A.Seat(0).View.Feature<WorkView>().ReadWorkerExecution(e.Id, out _).IsNone).Select(e => e.Id).ToArray();
            foreach (var worker in workers) Input(new SetWorkerHeldCommand { Worker = worker, Held = true });
            Membership(native, limited); var initial = FleetLoad(pair.A, native);
            Input(Batching.Control(Settings(false, 3, GameDuration.Decimal("12.5", DurationUnit.Minute), true)), HaulLoadPolicyCodes.NotPolicy);
            c.That(FleetLoad(pair.A, native).Enabled && initial.Enabled, "unsupported member refuses whole edit without mutating valid prefix");
            Input(new ConfigureFleetNavigationCommand { Fleet = "delivery", Profile = "terrain.route" }, MotionNavigationCodes.NotAllowed);
            c.That(Batching.View(pair.A).TryRead("delivery", out var restricted) && restricted.Members.Count == 2 &&
                restricted.Members.All(m => m.Navigation.Profile == "survey.route"), "whole navigation refusal changes no compatible prefix");
            Membership(external, native);
            c.That(Batching.View(pair.A).TryRead("delivery", out var enrolled) && enrolled.Settings.Members.SequenceEqual(new[] { native, external }.OrderBy(id => id)), "native canonical detached membership from reverse caller order");
            Input(new ConfigureFleetCommand { Fleet = "conflict", Settings = new FleetSettings("fleet.haul", new[] { external, limited }) }, FleetCodes.MemberConflict);
            c.That(!Batching.View(pair.A).TryRead("conflict", out _), "conflicting set cannot create a valid prefix fleet");
            var before = Batching.View(pair.A).PreviewLoadPolicy("delivery", Settings(false, 3, GameDuration.Decimal("12.5", DurationUnit.Minute), true));
            Input(Batching.Control(Settings(false, 3, GameDuration.Decimal("12.5", DurationUnit.Minute), true)));
            foreach (var worker in new[] { native, external }) c.That(!FleetLoad(pair.A, worker).Enabled && FleetHome(pair.A, worker).State == WorkerHomeState.Held &&
                FleetHome(pair.A, worker).Home == source, "whole native/contract edit preserves original held/home state");
            c.That(before.Entries.All(e => e.Settings.Enabled), "previous policy values remain detached after actual edit");
            Input(Batching.Control(Settings(true, 8, GameDuration.Minutes(10), true)), Codes.NotFound, 1);
            int unchanged = Input(Batching.Control(Settings(false, 3, GameDuration.Decimal("12.5", DurationUnit.Minute), true)));
            c.That(Result(pair.A, 0, unchanged).Value.Arg0 == 2 && Result(pair.A, 0, unchanged).Value.Arg1 == 0, "identical whole edit reports actual unchanged owners");
            int routeTicket = Input(new AddRouteCommand { From = source, To = target, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 100) });
            int route = checked((int)Result(pair.A, 0, routeTicket).Value.Arg0); long created = Batching.Route(pair.A, route).CreatedTick;
            Input(new SetWorkerHeldCommand { Worker = external, Held = false });
            c.Equal(HaulLoadPolicyCodes.Disabled, Batching.Offer(pair.A, route).Haul.Blocker, "disabled actual carrier intake blocks real request");
            c.That(FleetJobs(pair.A).Count == 0 && FleetStock(pair.A, source).Stock == Fix.FromInt(20), "waiting reserves no fictional vehicle cargo");
            restored = FleetRestore(c, factory, pair); int waitStart = issued;
            var waitLeft = restored.A.RecordReplay(limits); var waitRight = restored.B.RecordReplay(limits);
            Input(Batching.Control(Settings(true, 8, GameDuration.Minutes(10), true)));
            c.That(Batching.Offer(pair.A, route).Haul.Blocker == HaulLoadPolicyCodes.Minimum && Batching.Route(pair.A, route).CreatedTick == created &&
                Batching.Route(pair.A, route).LastDispatchTick == created, "changed policy retains original request age while short batch waits");
            Both(pair, restored, game => HasJob(game, route, external), "original ten-minute wait deadline admits actual short contract shipment");
            var first = FleetJobs(pair.A).Single(j => j.Worker == external); long admitted = Batching.Route(pair.A, route).LastDispatchTick;
            c.That(admitted - created >= pair.A.Clock.Time.ExactTicks(GameDuration.Minutes(10)) && first.Amount == Fix.FromInt(3), "real max-wait admission from original clock age");
            Route(route, false, 3);
            Both(pair, restored, game => FleetJobs(game).Any(j => j.Id == first.Id && j.Loaded > Fix.Zero && j.Loaded < j.Amount), "actual partial timed contract loading");
            ReplayPair(c, factory, waitLeft, waitRight, restored, limits, issued - waitStart, "saved waiting fleet through actual partial deadline shipment replay");
            restored = FleetRestore(c, factory, pair); int cargoStart = issued;
            var cargoLeft = restored.A.RecordReplay(limits); var cargoRight = restored.B.RecordReplay(limits);
            Input(Batching.Control(Settings(false, 8, GameDuration.Minutes(100))));
            c.That(Job(pair.A, first.Id).Worker == first.Worker && Job(pair.A, first.Id).DestinationEndpoint == first.DestinationEndpoint &&
                FleetStock(pair.A, target).InboundPromised == Fix.FromInt(3) && Batching.Route(pair.A, route).LastDispatchTick == admitted,
                "future intake edit retains actual job/worker/destination promise and dispatch history");
            Input(new RemoveFleetCommand { Fleet = "delivery" });
            Both(pair, restored, game => InTransit(game, first.Id, external), "removed fleet metadata does not cancel actual loaded travel");
            c.That(!Batching.View(pair.A).TryRead("delivery", out _) && !FleetLoad(pair.A, external).Enabled, "metadata removal preserves actual disabled component policy");
            ReplayPair(c, factory, cargoLeft, cargoRight, restored, limits, issued - cargoStart, "saved acquired cargo through disabled intake and fleet metadata removal replay");
            restored = FleetRestore(c, factory, pair);
            var travelLeft = restored.A.RecordReplay(limits); var travelRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native sole-clock pause during loaded contract travel");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash(); var heldPosition = FleetPosition(pair.A, external);
            Reach(restored, game => game.Clock.StepIndex >= paused + 8, "saved peers physically advance loaded vehicle while original is paused");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen && FleetPosition(pair.A, external).Equals(heldPosition), "paused original borrows no road movement, inventory or claims");
            pair.A.Net.RequestResume(); Both(pair, restored, game => game.Clock.StepIndex >= paused + 8, "native resumed cargo state converges with independent saved journey");
            Both(pair, restored, game => FleetStock(game, target).Stock == Fix.FromInt(3) && FleetHome(game, external).Job == 0 &&
                FleetPosition(game, external).X < 29, "contract vehicle delivers and physically clears destination exit");
            ReplayPair(c, factory, travelLeft, travelRight, restored, limits, 0, "saved metadata-free loaded travel through isolated delivery replay");
            restored = FleetRestore(c, factory, pair); int secondStart = issued;
            var secondLeft = restored.A.RecordReplay(limits); var secondRight = restored.B.RecordReplay(limits);
            Input(new SetWorkerHeldCommand { Worker = external, Held = true }); Membership(native);
            Input(Batching.Control(Settings(true, 2, GameDuration.Minutes(1000), profile: "fleet.odd")));
            c.That(FleetLoad(pair.A, native).Profile == "fleet.odd" && !FleetLoad(pair.A, external).Enabled, "edited membership changes only actual current native policy owner");
            Route(route, true, 2); Input(new SetWorkerHeldCommand { Worker = native, Held = false });
            c.That(Batching.Offer(pair.A, route).Haul.Blocker == new Code("proof.odd-load") && FleetJobs(pair.A).Count == 0 &&
                Batching.Route(pair.A, route).LastDispatchTick == admitted, "independent authored rule rejects even offer without invented job or age reset");
            Route(route, true, 3);
            Both(pair, restored, game => HasJob(game, route, native), "same native dispatcher accepts independently authored odd batch");
            var second = FleetJobs(pair.A).Single(j => j.Worker == native); c.That(second.Id != first.Id && second.Amount == Fix.FromInt(3), "second actual trip has a distinct native job identity");
            Route(route, false, 3);
            Both(pair, restored, game => FleetJobs(game).Any(j => j.Id == second.Id && j.Loaded > Fix.Zero && j.Loaded < j.Amount), "native partial timed acquisition");
            ReplayPair(c, factory, secondLeft, secondRight, restored, limits, issued - secondStart, "saved first delivery through membership edit and independent-rule native partial shipment replay");
            restored = FleetRestore(c, factory, pair);
            var nativeLeft = restored.A.RecordReplay(limits); var nativeRight = restored.B.RecordReplay(limits);
            Input(Batching.Control(Settings(false, 8, GameDuration.Minutes(100))));
            Both(pair, restored, game => InTransit(game, second.Id, native), "disabled future native intake retains loaded physical journey");
            c.That(Job(pair.A, second.Id).DestinationEndpoint == second.DestinationEndpoint && FleetStock(pair.A, target).InboundPromised == Fix.FromInt(3), "native busy policy change preserves actual delivery promise");
            ReplayPair(c, factory, nativeLeft, nativeRight, restored, limits, 1, "saved partial native acquisition through disabled-intake loaded journey replay");
            restored = FleetRestore(c, factory, pair);
            var endLeft = restored.A.RecordReplay(limits); var endRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => FleetStock(game, target).Stock == Fix.FromInt(6) && FleetHome(game, native).Job == 0 &&
                FleetPosition(game, native).X < 29, "second acquired native trip delivers and clears destination departure");
            c.That(FleetStock(pair.A, source).Stock == Fix.FromInt(14) && FleetStock(pair.A, target).InboundPromised.IsZero &&
                FleetStock(pair.A, source).OutboundPromised.IsZero && FleetStock(pair.A, native).Stock.IsZero && FleetStock(pair.A, external).Stock.IsZero,
                "both actual finite shipments settle all cargo and promises exactly once");
            c.That(enrolled.Settings.Members.Count == 2 && before.Entries.All(e => e.Settings.Enabled) && initial.Enabled, "old detached fleet/policy values survive removal and real membership replacement");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "original and saved native fleet peers actually stall and reverse bytes");
            ReplayPair(c, factory, endLeft, endRight, restored, limits, 0, "saved disabled loaded native journey through settled delivery replay");
            ReplayPair(c, factory, left, right, pair, limits, issued, "full named fleet native/contract physical sequence replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced fleet worlds converge");
            FleetRestore(c, factory, pair); Console.WriteLine("FLEET inputs=" + issued);
        }
    }
}
