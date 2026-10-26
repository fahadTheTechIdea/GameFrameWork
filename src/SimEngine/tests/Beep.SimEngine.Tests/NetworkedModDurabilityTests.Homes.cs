using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Homes = Beep.SimEngine.Tests.FleetHomeTests;
using Policies = Beep.SimEngine.Tests.FleetWorkPolicyTests;
using Batching = Beep.SimEngine.Tests.FleetLoadTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string FleetHomeCase = "Networked fleet homes: saved area blocks, mixed-worker cargo policy holds and physical home replacement";
        private static WorkerPolicyView HomePolicy(SimGame game, EntityId worker)
        {
            Code refusal = game.Seat(0).View.Feature<WorkView>().TryReadPolicy(worker, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual worker-owned home/work policy/" + refusal); return value;
        }
        private static void FleetHomeRun(Check c)
        {
            var factory = FleetNavigationTests.Factory(named: true, homeControls: true);
            var pair = FleetPair(LiveSensorTests.New(factory), LiveSensorTests.New(factory));
            Console.WriteLine("MODULES fleet-homes=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId), "home/area/work controls require no business or platform");
            var native = pair.A.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0) && e.Def == "survey.machine").OrderBy(e => e.Id).First().Id;
            var external = FleetEntity(pair.A, "fleet.public-carrier"); var unsupported = FleetEntity(pair.A, "fleet.no-policy");
            var source = FleetEntity(pair.A, "survey.site"); var target = FleetEntity(pair.A, "fleet.destination"); var actors = new[] { native, external };
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            Pair restored = null; int issued = 0;
            int Input(Command command, Code expected = default, int seat = 0) {
                int ticket = Submit(c, pair, command, expected, seat); issued++;
                if (restored != null) c.Equal(ticket, Submit(c, restored, command, expected, seat), "actual saved home/work proposal identity");
                bool Answered(SimGame game) => Result(game, seat, ticket)?.Refusal == expected;
                if (restored == null) Reach(pair, Answered, "ordinary home/work player intent"); else Both(pair, restored, Answered, "same actual home/work intent on saved peers");
                return ticket;
            }
            WorkPolicySettings Policy(bool enabled, int priority, string area, bool interruptible, bool emergency, WorkAssignmentMode mode)
                => Policies.Settings(enabled, priority, area, interruptible, emergency, mode);
            int Membership(params EntityId[] members) => Input(Batching.MembershipCommand(members));
            WorkJobView Job(SimGame game, int id) => FleetJobs(game).Single(j => j.Id == id);
            var workers = pair.A.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0) &&
                pair.A.Seat(0).View.Feature<WorkView>().ReadWorkerExecution(e.Id, out _).IsNone).Select(e => e.Id).ToArray();
            foreach (var worker in workers) Input(new SetWorkerHeldCommand { Worker = worker, Held = true });
            Membership(native, unsupported);
            Input(Policies.Control(Policy(true, 77, "fleet.source-only", false, true, WorkAssignmentMode.All)), WorkPolicyCodes.NotPolicy);
            c.Equal(100, HomePolicy(pair.A, native).Settings.Priority, "unsupported policy owner refuses whole change without changing native prefix");
            Membership(external, native);
            Input(Policies.Control(Policy(true, 77, "missing", false, true, WorkAssignmentMode.All)), WorkPolicyCodes.InvalidSettings);
            Input(Homes.Home(external), Codes.InvalidArgument);
            c.That(actors.All(actor => FleetHome(pair.A, actor).Home == source), "late self-home refusal changes no valid earlier member");
            Input(Homes.Home(source), Codes.NotFound, 1);
            var before = Batching.View(pair.A).PreviewHome("delivery", EntityId.None);
            Input(Homes.Home(EntityId.None)); int same = Input(Homes.Home(EntityId.None));
            c.That(Result(pair.A, 0, same).Value.Arg0 == 2 && Result(pair.A, 0, same).Value.Arg1 == 0 &&
                actors.All(actor => FleetHome(pair.A, actor).Home == EntityId.None && FleetHome(pair.A, actor).State == WorkerHomeState.Held),
                "explicit idempotent clear creates no fallback home and releases no held worker");
            restored = FleetRestore(c, factory, pair); int areaStart = issued;
            var areaLeft = restored.A.RecordReplay(limits); var areaRight = restored.B.RecordReplay(limits);
            Membership(native); Input(Homes.Home(target));
            c.That(FleetHome(pair.A, native).Home == target && FleetHome(pair.A, external).Home == EntityId.None, "current membership changes only actual selected home owner");
            Membership(actors); Input(Homes.Home(target));
            Input(Policies.Control(Policy(true, 77, "fleet.source-only", false, true, WorkAssignmentMode.All)));
            int routeTicket = Input(new AddRouteCommand { From = source, To = target, Resource = FleetSteel(pair.A), Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 100) });
            int route = checked((int)Result(pair.A, 0, routeTicket).Value.Arg0); var positions = actors.Select(actor => TripPosition(pair.A, actor)).ToArray();
            Input(new SetWorkerHeldCommand { Worker = native, Held = false }); Input(new SetWorkerHeldCommand { Worker = external, Held = false });
            long blockedAt = pair.A.Clock.StepIndex;
            Both(pair, restored, game => game.Clock.StepIndex >= blockedAt + 4, "actual game time passes while original operating area blocks home and dispatch");
            for (int i = 0; i < actors.Length; i++) c.That(FleetHome(pair.A, actors[i]).Home == target && FleetHome(pair.A, actors[i]).State == WorkerHomeState.Blocked &&
                FleetHome(pair.A, actors[i]).Blocker == WorkPolicyCodes.OutsideArea && TripPosition(pair.A, actors[i]).Equals(positions[i]),
                "future home remains intent; actual area block prevents fractional movement or teleport");
            c.That(FleetJobs(pair.A).Count == 0 && FleetStock(pair.A, source).Stock == Fix.FromInt(20) && FleetStock(pair.A, target).Stock.IsZero,
                "area-blocked fleet fabricates no job or transferred material");
            ReplayPair(c, factory, areaLeft, areaRight, restored, limits, issued - areaStart, "saved cleared homes through current membership/area-blocked home and dispatch replay");
            restored = FleetRestore(c, factory, pair); int supplyStart = issued;
            var supplyLeft = restored.A.RecordReplay(limits); var supplyRight = restored.B.RecordReplay(limits);
            Input(Policies.Control(Policy(true, 77, "fleet.all", false, true, WorkAssignmentMode.All)));
            Both(pair, restored, game => FleetJobs(game).Count == 2 && FleetJobs(game).All(j => j.Worker.IsValid), "original native/independent carriers acquire two physical eight-unit jobs");
            var jobs = FleetJobs(pair.A).OrderBy(j => j.Id).ToArray();
            c.That(jobs.Select(j => j.Worker).OrderBy(id => id).SequenceEqual(actors.OrderBy(id => id)) && jobs.All(j => j.Amount == Fix.FromInt(8) && !j.Emergency),
                "only released actual members receive distinct ordinary cargo jobs");
            Input(new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(8), 100) });
            Both(pair, restored, game => FleetJobs(game).Any(j => j.Loaded > Fix.Zero && j.Loaded < j.Amount), "original timed handling earns partial real cargo");
            ReplayPair(c, factory, supplyLeft, supplyRight, restored, limits, issued - supplyStart, "saved area block through actual mixed-worker partial loading replay");
            restored = FleetRestore(c, factory, pair);
            var disableLeft = restored.A.RecordReplay(limits); var disableRight = restored.B.RecordReplay(limits);
            var pausedJobs = jobs.Select(j => Job(pair.A, j.Id)).ToArray(); var pausedPositions = actors.Select(actor => TripPosition(pair.A, actor)).ToArray();
            Input(Policies.Control(Policy(false, 13, "fleet.all", false, true, WorkAssignmentMode.All)));
            foreach (var old in pausedJobs) c.That(Job(pair.A, old.Id).State == JobState.Paused && Job(pair.A, old.Id).Blocker == WorkPolicyCodes.Disabled &&
                Job(pair.A, old.Id).Worker == old.Worker && Job(pair.A, old.Id).Loaded == old.Loaded && Job(pair.A, old.Id).HandlingProgress.Equals(old.HandlingProgress) &&
                Job(pair.A, old.Id).DestinationEndpoint == old.DestinationEndpoint, "whole work disable retains actual assignment/cargo/handling and endpoint");
            c.Equal(Fix.FromInt(16), FleetStock(pair.A, target).InboundPromised, "actual receiving promises remain held for both acquired jobs");
            ReplayPair(c, factory, disableLeft, disableRight, restored, limits, 1, "saved partial loading through exact current-policy job pause replay");
            restored = FleetRestore(c, factory, pair);
            var heldLeft = restored.A.RecordReplay(limits); var heldRight = restored.B.RecordReplay(limits);
            long heldAt = pair.A.Clock.StepIndex;
            Both(pair, restored, game => game.Clock.StepIndex >= heldAt + 8, "eight actual steps while work-policy disabled");
            for (int i = 0; i < actors.Length; i++) c.That(TripPosition(pair.A, actors[i]).Equals(pausedPositions[i]) && !HomePolicy(pair.A, actors[i]).Settings.Enabled,
                "policy pause stops exact fractional movement while the game clock continues");
            foreach (var old in pausedJobs) c.That(Job(pair.A, old.Id).Loaded == old.Loaded && Job(pair.A, old.Id).HandlingProgress.Equals(old.HandlingProgress), "quiet disabled time creates no handling progress or material");
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native sole-clock pause during saved work-policy hold");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 8, "other saved disabled-work peers advance their own actual time");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen, "paused original borrows no physical or policy state");
            pair.A.Net.RequestResume(); Both(pair, restored, game => game.Clock.StepIndex >= paused + 8, "native resumed policy pause converges at common epoch");
            Input(Policies.Control(Policy(true, 21, "fleet.all", true, false, WorkAssignmentMode.EmergencyOnly)));
            Both(pair, restored, game => jobs.All(j => Job(game, j.Id).Loaded == Fix.FromInt(8)) && FleetStock(game, target).Stock.IsZero &&
                actors.Any(actor => game.Seat(0).View.Feature<MotionView>().ReadExecution(actor, out var movement).IsNone && movement.Status == MoveStatus.Moving),
                "emergency-only new-assignment policy retains both already acquired ordinary cargo journeys");
            c.That(actors.All(actor => HomePolicy(pair.A, actor).Settings.AssignmentMode == WorkAssignmentMode.EmergencyOnly) &&
                jobs.All(j => Job(pair.A, j.Id).Worker == j.Worker && !Job(pair.A, j.Id).Emergency), "admission-only restriction never fabricates emergency status or reassigns original jobs");
            ReplayPair(c, factory, heldLeft, heldRight, restored, limits, 1, "saved disabled work through quiet isolation and retained ordinary loaded travel replay");
            restored = FleetRestore(c, factory, pair);
            var homeLeft = restored.A.RecordReplay(limits); var homeRight = restored.B.RecordReplay(limits);
            Input(Homes.Home(source));
            c.That(actors.All(actor => FleetHome(pair.A, actor).Home == source && FleetHome(pair.A, actor).State == WorkerHomeState.Working) &&
                jobs.All(j => Job(pair.A, j.Id).Worker == j.Worker && Job(pair.A, j.Id).Loaded == Fix.FromInt(8) && Job(pair.A, j.Id).DestinationEndpoint == j.DestinationEndpoint) &&
                FleetStock(pair.A, target).Stock.IsZero && FleetStock(pair.A, target).InboundPromised == Fix.FromInt(16), "busy home replacement is future intent and preserves both physical cargo/promise owners");
            ReplayPair(c, factory, homeLeft, homeRight, restored, limits, 1, "saved loaded ordinary work through actual busy home replacement replay");
            restored = FleetRestore(c, factory, pair);
            var removeLeft = restored.A.RecordReplay(limits); var removeRight = restored.B.RecordReplay(limits);
            Input(Policies.Control(Policy(true, 50, "fleet.all", true, false, WorkAssignmentMode.All))); Input(new RemoveFleetCommand { Fleet = "delivery" });
            c.That(!Batching.View(pair.A).TryRead("delivery", out _) && actors.All(actor => FleetHome(pair.A, actor).Home == source && HomePolicy(pair.A, actor).Settings.Priority == 50),
                "metadata removal retains actual worker-owned home and work policies");
            ReplayPair(c, factory, removeLeft, removeRight, restored, limits, 2, "saved busy homes through ordinary policy restore and fleet metadata removal replay");
            restored = FleetRestore(c, factory, pair);
            var deliveredLeft = restored.A.RecordReplay(limits); var deliveredRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => FleetStock(game, target).Stock == Fix.FromInt(16) && FleetJobs(game).Count == 0 &&
                actors.Any(actor => FleetHome(game, actor).State == WorkerHomeState.Returning), "same metadata-free jobs unload/depart and start actual current-home return");
            c.That(FleetStock(pair.A, source).Stock == Fix.FromInt(4) && actors.All(actor => FleetStock(pair.A, actor).Stock.IsZero) &&
                FleetStock(pair.A, source).OutboundPromised.IsZero && FleetStock(pair.A, target).InboundPromised.IsZero, "two actual eight-unit deliveries settle finite steel and all promises exactly once");
            ReplayPair(c, factory, deliveredLeft, deliveredRight, restored, limits, 0, "saved metadata-free cargo and current homes through real delivery/return departure replay");
            restored = FleetRestore(c, factory, pair);
            var returnLeft = restored.A.RecordReplay(limits); var returnRight = restored.B.RecordReplay(limits); long returningAt = pair.A.Clock.StepIndex;
            Both(pair, restored, game => actors.All(actor => FleetHome(game, actor).State == WorkerHomeState.AtHome), "saved native/independent current-home journeys physically arrive");
            c.That(pair.A.Clock.StepIndex > returningAt && actors.All(actor => FleetHome(pair.A, actor).Home == source) && FleetStock(pair.A, target).Stock == Fix.FromInt(16),
                "real home return takes positive game time and never repeats delivery");
            c.That(before.Entries.All(e => e.Current.Home == source && e.Current.State == WorkerHomeState.Held), "old public home/held facts remain detached after clear/edit/return");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "native original/restored home-work transports actually stall and reverse opaque packets");
            ReplayPair(c, factory, returnLeft, returnRight, restored, limits, 0, "saved returning workers through actual metadata-free current-home arrival replay");
            ReplayPair(c, factory, left, right, pair, limits, issued, "full native/independent home/area/work physical sequence replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced home-work worlds converge");
            FleetRestore(c, factory, pair); Console.WriteLine("HOME inputs=" + issued);
        }
    }
}
