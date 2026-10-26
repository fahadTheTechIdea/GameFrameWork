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
using Remote = Beep.SimEngine.Tests.RemoteSignalLifecycleTests;
using Physical = Beep.SimEngine.Tests.PermanentRemovalTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string RemoteLifecycleCase = "Networked remote signals: native binding, physical provider lifecycle and saved historical references";
        internal const string IndependentRemoteLifecycleCase = "Networked remote signal contracts: independent binding metadata, physical lifecycle and saved historical references";
        private static readonly Code RemoteSource = new Code("sensor.remote");
        private static EntityId RemoteSensor(SimGame game) => Remote.Find(game, "sensor.machine");
        private static RemoteSignalBindingSnapshot RemoteBinding(SimGame game) => Remote.Binding(game, RemoteSensor(game));
        private static AutomationSignalSnapshot RemoteSignal(SimGame game, string signal = "sensor.utility.ready")
        {
            Code refusal = game.Seat(0).View.Feature<OperationsView>().ReadSignal(RemoteSensor(game), RemoteSource, signal, out var fact);
            if (!refusal.IsNone) throw new InvalidOperationException("actual remote signal refused/" + refusal); return fact;
        }
        private static SpatialLayerReadStatus RemoteSight(SimGame game)
        {
            var values = new List<Fix>();
            return game.Seat(0).View.Feature<PointLayerView>().ReadLayer("point.terrain", new PointRegion(Physical.At(game, RemoteSensor(game)), Fix.One, 1), values);
        }
        private static BindRemoteSignalCommand RemoteBind(SimGame game, EntityId provider)
            => new BindRemoteSignalCommand { Entity = RemoteSensor(game), SignalSource = RemoteSource, Provider = provider };
        // Player-authored target/worker selection only; original plans/work own admission and movement.
        private static SubmitPlanCommand RemotePlan(string activity, string node, EntityId target, EntityId worker, bool paused = false)
            => new SubmitPlanCommand { Intent = new PlanIntent("activity:" + activity, Json.Parse("{\"target\":" + target.Value +
                ",\"workers\":[{\"node\":\"" + node + "\",\"role\":\"inspect\",\"worker\":" + worker.Value + "}]}"), "remote.lifecycle", 80, paused) };
        private static ConfigurePlanCommand RemotePause(long plan, bool paused)
            => new ConfigurePlanCommand { Plan = plan, Group = "remote.lifecycle", Priority = 80, Paused = paused };
        private static bool RemoteNoHaul(SimGame game) => !Physical.Jobs(game).Any(j => j.IsHaul &&
            (j.State == JobState.Open || j.State == JobState.Assigned || j.State == JobState.Paused));
        private static bool RemotePartial(SimGame game, int order, string node)
        {
            var rows = new List<ActivityNodeView>();
            return game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, rows) && rows.Any(n => n.Id == node &&
                n.ProductiveSteps > 0 && n.Progress.WholeTicks > 0 && n.Progress.WholeTicks < n.RequiredTicks && n.State != NodeState.Done);
        }
        private static string RemoteDiagnostics(SimGame game)
        {
            var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlanGroup("remote.lifecycle", plans);
            return "at=" + game.Clock.StepIndex + "/position=" + Physical.At(game, RemoteSensor(game)) +
                "/provider=" + RemoteBinding(game).Provider + "/binding=" + RemoteBinding(game).Blocker + "/ready=" + RemoteSignal(game).Value +
                "/signal=" + RemoteSignal(game).Blocker + "/plans=" + string.Join(";", plans.Select(p => p.Id + "/" + p.State + "/" + p.Paused + "/" + p.Order + "/" + p.Blocker)) +
                "/jobs=" + string.Join(";", Physical.Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.ToilIndex));
        }
        private static Pair RemotePair(SimGame a, SimGame b)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long prior) && prior == game.Clock.StepIndex) return;
                if (Physical.Total(game) != Fix.FromInt(20)) throw new InvalidOperationException("remote lifecycle lost or duplicated original stock/" + RemoteDiagnostics(game));
                foreach (var fact in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates)
                    if (fact.Kind == ResourceFlowKind.AuthoredCreation || fact.Kind == ResourceFlowKind.StockPurchase || fact.Kind == ResourceFlowKind.Consumed ||
                        fact.Kind == ResourceFlowKind.Produced || fact.Kind == ResourceFlowKind.Lost || fact.Kind == ResourceFlowKind.Destroyed)
                        throw new InvalidOperationException("stateless readiness/lifecycle must not grant, consume or discard resources");
                Code refusal = game.Seat(1).View.Feature<RemoteSignalsView>().ReadBinding(RemoteSensor(game), RemoteSource, out var hidden);
                if (refusal != Codes.NotFound || hidden.Provider != EntityId.None || hidden.Entity != EntityId.None ||
                    game.Seat(1).View.Feature<OperationsView>().ReadSignal(RemoteSensor(game), RemoteSource, "sensor.utility.ready", out var signal) != Codes.NotFound || signal.Value.HasValue)
                    throw new InvalidOperationException("private remote binding/signal leaked");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, RemoteDiagnostics, 0);
        }
        private static Pair RemoteRestore(Check c, GameFactory factory, Pair source)
        {
            var restored = RemotePair(factory.Load(source.A.Save()), factory.Load(source.B.Save()));
            c.Equal(source.A.Hash(), restored.A.Hash(), "native binding/work/resource state restores exact actual hash");
            c.Equal(source.B.Hash(), restored.B.Hash(), "saved peer restores actual state"); return restored;
        }
        private static void RemoteLifecycleRun(Check c, bool independent)
        {
            var factory = RemoteSensorTests.Factory(independent: independent, lifecycle: true, recordFlows: true);
            SimGame New() => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 734, Seats = { Controller.Human, Controller.Human } });
            var pair = RemotePair(New(), New());
            Console.WriteLine("MODULES remote-lifecycle=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Modules.Contains(RemoteSignalsModule.ModuleId) && !pair.A.Modules.Contains(TycoonModule.ModuleId), "explicit remote module needs no business");
            c.Equal(independent, pair.A.Modules.Contains(IndependentRemoteSignalsModule.ModuleId), "actual independent metadata selection, original sole runtime");
            c.That(RemoteBinding(pair.A).Provider == EntityId.None && RemoteSignal(pair.A).Blocker == RemoteSignalCodes.Unbound &&
                RemoteSight(pair.A) == SpatialLayerReadStatus.Unavailable, "unbound has no guessed readiness or sight");
            var sensor = RemoteSensor(pair.A); var original = Remote.Find(pair.A, "utility.remote-alternative"); var other = Remote.Find(pair.A, "utility.remote-provider");
            var limits = new ReplayLimits(8 * 1024 * 1024, 2500);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int invalid = Submit(c, pair, RemoteBind(pair.A, Remote.Find(pair.A, "source")), RemoteSignalCodes.Incompatible);
            int bound = Submit(c, pair, RemoteBind(pair.A, original));
            int haul = Submit(c, pair, new HaulResourcesCommand { Source = Remote.Find(pair.A, "source"), Destination = other,
                Worker = Remote.Find(pair.A, "machine"), Resource = Physical.Supply(pair.A), Ceiling = Fix.One, Priority = 70 });
            int foreign = Submit(c, pair, RemoteBind(pair.B, original), Codes.NotFound, seat: 1);
            bool Loaded(SimGame game) => Physical.Jobs(game).Any(j => j.IsHaul && j.Loaded.IsPositive) && Physical.Facts(game, other).Stock.IsZero;
            Reach(pair, Loaded, "actual physically loaded supply for the unselected compatible provider");
            c.That(Result(pair.A, 0, invalid)?.Refusal == RemoteSignalCodes.Incompatible && Result(pair.A, 0, bound)?.Accepted == true &&
                Result(pair.A, 0, haul)?.Accepted == true && Result(pair.A, 1, foreign)?.Refusal == Codes.NotFound, "native owner/refusal semantics commit without partial binding or stock changes");
            c.That(RemoteBinding(pair.A).Provider == original && RemoteSignal(pair.A).Available && RemoteSignal(pair.A).Value == Fix.Zero,
                "selected empty provider remains false while another provider supply is aboard");
            var restored = RemoteRestore(c, factory, pair);
            var cargoLeft = restored.A.RecordReplay(limits); var cargoRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => Physical.Facts(g, other).Stock == Fix.One && RemoteNoHaul(g), "real one-minute loading/travel/two-minute unloading/departure complete");
            c.That(RemoteSignal(pair.A).Value == Fix.Zero && RemoteSight(pair.A) == SpatialLayerReadStatus.Unavailable,
                "supplied alternative is never silently substituted for selected empty provider");
            foreach (var current in new[] { pair, restored }) Submit(c, current, RemoteBind(current.A, other));
            Both(pair, restored, g => RemoteBinding(g).Provider == other, "explicit player switch to genuinely supplied provider");
            c.That(RemoteSignal(pair.A).Available && RemoteSignal(pair.A).Value == Fix.One && RemoteSight(pair.A) == SpatialLayerReadStatus.Success,
                "real supplied remote signal opens actual non-tile sensor sight");
            c.Equal(Fix.One, RemoteSignal(pair.A, "remote.requested").Value.Value, "remote typed requested amount remains native fact");
            c.Equal(Fix.One, RemoteSignal(pair.A, "remote.available").Value.Value, "remote contribution is original provider stock fact");
            var selected = RemoteBinding(pair.A);
            ReplayPair(c, factory, cargoLeft, cargoRight, restored, limits, 1, "saved cargo to explicit supplied remote binding replay");
            restored = RemoteRestore(c, factory, pair);
            var suppliedLeft = restored.A.RecordReplay(limits); var suppliedRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, RemoteBind(current.A, original));
            Both(pair, restored, g => RemoteBinding(g).Provider == original, "ordinary switch back to selected empty alternative");
            c.That(RemoteSignal(pair.A).Value == Fix.Zero && RemoteSight(pair.A) == SpatialLayerReadStatus.Unavailable && selected.Provider == other,
                "live switch revokes readiness/sight, retained prior binding stays detached");
            int planned = 0;
            foreach (var current in new[] { pair, restored }) {
                int sequence = Submit(c, current, RemotePlan("remote.upgrade", "replace", original, sensor, true));
                if (planned == 0) planned = sequence; else c.Equal(planned, sequence, "saved command sequence retains physical plan identity");
            }
            Both(pair, restored, g => Result(g, 0, planned)?.Accepted == true, "paused explicit-worker upgrade intent admitted");
            long upgradePlan = Result(pair.A, 0, planned).Value.Arg0;
            c.That(Physical.Plan(pair.A, upgradePlan).Paused && Physical.At(pair.A, sensor) == Fix.FromInt(80) && Physical.Exists(pair.A, original),
                "paused intent neither moves selected machine nor replaces provider");
            ReplayPair(c, factory, suppliedLeft, suppliedRight, restored, limits, 2, "saved supplied binding to paused physical replacement intent replay");
            restored = RemoteRestore(c, factory, pair);
            var planLeft = restored.A.RecordReplay(limits); var planRight = restored.B.RecordReplay(limits);
            int competing = 0;
            foreach (var current in new[] { pair, restored }) {
                int sequence = Submit(c, current, RemotePlan("remote.upgrade", "replace", original, Remote.Find(current.A, "machine"), true));
                if (competing == 0) competing = sequence; else c.Equal(competing, sequence, "different explicit worker is a distinct actual pending intent");
            }
            Both(pair, restored, g => Result(g, 0, competing)?.Accepted == true, "other paused replacement remains an independently guarded intent");
            long competingPlan = Result(pair.A, 0, competing).Value.Arg0;
            int resumed = 0;
            foreach (var current in new[] { pair, restored }) {
                int sequence = Submit(c, current, RemotePause(upgradePlan, false));
                if (resumed == 0) resumed = sequence; else c.Equal(resumed, sequence, "same normal resume command");
            }
            Both(pair, restored, g => Result(g, 0, resumed)?.Accepted == true && Physical.Plan(g, upgradePlan).Blocker == StandardCodes.WrongTarget,
                "another incompatible pending old-prototype target still refuses activation");
            c.That(Physical.Plan(pair.A, upgradePlan).Order == 0 && Physical.At(pair.A, sensor) == Fix.FromInt(80) && Physical.Exists(pair.A, original),
                "own-plan admission does not bypass another plan or create a work prefix");
            ulong guarded = pair.A.Hash();
            c.Equal(StandardCodes.WrongTarget, pair.A.Seat(0).Preview(new PostOrderCommand { Plan = "remote.upgrade", Target = original }),
                "external replacement admission retains both pending-plan reference guards");
            c.Equal(guarded, pair.A.Hash(), "external refusal and nested plan views leave activation scope/authority unchanged");
            c.Equal(guarded, factory.Load(pair.A.Save()).Hash(), "blocked plan saves with no retained admission context");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { competingPlan }) });
            bool Traveling(SimGame game) => Physical.At(game, sensor) < Fix.FromInt(80) && Physical.At(game, sensor) > Fix.FromInt(4);
            Both(pair, restored, Traveling, "selected original sensor worker actually travels from point80 toward point4");
            int upgradeOrder = Physical.Plan(pair.A, upgradePlan).Order;
            c.That(upgradeOrder > 0 && Physical.Exists(pair.A, original) && RemoteBinding(pair.A).Provider == original &&
                Physical.Node(pair.A, upgradeOrder, "replace").Progress.WholeTicks == 0, "travel is neither earned staffed work nor reference replacement");
            ReplayPair(c, factory, planLeft, planRight, restored, limits, 3, "saved paused plan through real foreign-intent guard to physical travel replay");
            restored = RemoteRestore(c, factory, pair);
            var travelLeft = restored.A.RecordReplay(limits); var travelRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native sole-clock pause during actual provider-work travel");
            long paused = pair.A.Clock.StepIndex; ulong held = pair.A.Hash(); Fix heldAt = Physical.At(pair.A, sensor);
            Reach(restored, g => g.Clock.StepIndex >= paused + 8, "other saved peers advance real machine travel independently"); pair.Round();
            c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == held && Physical.At(pair.A, sensor) == heldAt &&
                Physical.At(restored.A, sensor) < heldAt, "paused original borrows no movement, reference update or elapsed work");
            pair.A.Net.RequestResume(); Both(pair, restored, g => RemotePartial(g, upgradeOrder, "replace"), "full route arrival earns original positive three-minute replacement work");
            var partial = Physical.Node(pair.A, upgradeOrder, "replace");
            c.That(partial.Worker == sensor && partial.RequestedWorker == sensor && partial.RequiredTicks == 3 &&
                Physical.At(pair.A, sensor) == Fix.FromInt(4) && Physical.Exists(pair.A, original) && RemoteBinding(pair.A).Provider == original,
                "same selected machine/real location/partial work do not replace early");
            ReplayPair(c, factory, travelLeft, travelRight, restored, limits, 0, "saved actual travel to staffed replacement prefix replay");
            restored = RemoteRestore(c, factory, pair);
            var replaceLeft = restored.A.RecordReplay(limits); var replaceRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => RemoteBinding(g).Provider != original, "earned original replacement retargets sole actual reference owner");
            var replaced = RemoteBinding(pair.A); EntityId replacement = replaced.Provider;
            c.That(replacement.IsValid && replacement != other && !Physical.Exists(pair.A, original) && Physical.Exists(pair.A, replacement) &&
                Physical.At(pair.A, replacement) == Fix.FromInt(4) && replaced.Blocker == Code.None, "actual replacement retains point4, not target prototype default point3 or compatible existing provider");
            c.That(RemoteSignal(pair.A).Value == Fix.Zero && Physical.Facts(pair.A, other).Stock == Fix.One,
                "replacement has its own original empty stock, unselected supplied provider remains untouched");
            c.That(pair.A.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:remote.upgrade", EntityId.None, out var complete) &&
                complete.Order == upgradeOrder, "native history keeps actual original upgrade order identity");
            ReplayPair(c, factory, replaceLeft, replaceRight, restored, limits, 0, "saved staffed prefix to reference-preserving replacement replay");
            restored = RemoteRestore(c, factory, pair);
            var replacedLeft = restored.A.RecordReplay(limits); var replacedRight = restored.B.RecordReplay(limits);
            int removal = 0;
            foreach (var current in new[] { pair, restored }) {
                int sequence = Submit(c, current, RemotePlan("remote.remove", "retire", replacement, sensor));
                if (removal == 0) removal = sequence; else c.Equal(removal, sequence, "same saved physical removal proposal");
            }
            Both(pair, restored, g => Result(g, 0, removal)?.Accepted == true && Physical.Plan(g, Result(g, 0, removal).Value.Arg0).Order > 0 &&
                RemotePartial(g, Physical.Plan(g, Result(g, 0, removal).Value.Arg0).Order, "retire"), "original physical removal earns a positive prefix");
            long removalPlan = Result(pair.A, 0, removal).Value.Arg0; int cancelledOrder = Physical.Plan(pair.A, removalPlan).Order;
            c.That(Physical.Exists(pair.A, replacement) && RemoteBinding(pair.A).Provider == replacement, "partial removal has not retired the live selected provider");
            ReplayPair(c, factory, replacedLeft, replacedRight, restored, limits, 1, "saved replacement to partial physical removal replay");
            restored = RemoteRestore(c, factory, pair);
            var removeLeft = restored.A.RecordReplay(limits); var removeRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { removalPlan }) });
            Both(pair, restored, g => Physical.Plan(g, removalPlan).State == PlanState.Cancelled, "ordinary cancellation settles physical removal without deleting provider");
            long quiet = pair.A.Clock.StepIndex + 4; Both(pair, restored, g => g.Clock.StepIndex >= quiet, "cancelled retirement remains physically incomplete");
            c.That(Physical.Exists(pair.A, replacement) && RemoteBinding(pair.A).Provider == replacement && RemoteBinding(pair.A).Blocker.IsNone,
                "cancelled partial work retains sole live reference and original stock");
            ReplayPair(c, factory, removeLeft, removeRight, restored, limits, 1, "saved partial removal through genuine cancellation replay");
            restored = RemoteRestore(c, factory, pair);
            var cancelledLeft = restored.A.RecordReplay(limits); var cancelledRight = restored.B.RecordReplay(limits);
            int fresh = 0;
            foreach (var current in new[] { pair, restored }) {
                int sequence = Submit(c, current, RemotePlan("remote.remove", "retire", replacement, sensor));
                if (fresh == 0) fresh = sequence; else c.Equal(fresh, sequence, "fresh plan proposal uses actual next saved input identity");
            }
            Both(pair, restored, g => Result(g, 0, fresh)?.Accepted == true && Physical.Plan(g, Result(g, 0, fresh).Value.Arg0).Order > 0 &&
                RemotePartial(g, Physical.Plan(g, Result(g, 0, fresh).Value.Arg0).Order, "retire"), "fresh original retirement earns new positive work");
            int finalOrder = Physical.Plan(pair.A, Result(pair.A, 0, fresh).Value.Arg0).Order;
            c.That(finalOrder != cancelledOrder && Physical.Node(pair.A, finalOrder, "retire").RequiredTicks == 3, "new retirement keeps original duration rather than inheriting cancelled progress");
            ReplayPair(c, factory, cancelledLeft, cancelledRight, restored, limits, 1, "cancelled removal to fresh physical retirement prefix replay");
            restored = RemoteRestore(c, factory, pair);
            var retireLeft = restored.A.RecordReplay(limits); var retireRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => !Physical.Exists(g, replacement), "fresh earned retirement actually removes selected provider");
            c.That(RemoteBinding(pair.A).Provider == replacement && RemoteBinding(pair.A).Blocker == Codes.NotFound &&
                !RemoteSignal(pair.A).Available && RemoteSignal(pair.A).Blocker == Codes.NotFound && RemoteSight(pair.A) == SpatialLayerReadStatus.Unavailable,
                "removed historical reference is preserved with typed unavailability and no stale ready/sight");
            c.That(Physical.Exists(pair.A, other) && Physical.Facts(pair.A, other).Stock == Fix.One && replaced.Provider == replacement,
                "supplied compatible survivor and detached replacement fact do not become implicit fallback");
            ReplayPair(c, factory, retireLeft, retireRight, restored, limits, 0, "saved fresh prefix to historical missing-provider replay");
            restored = RemoteRestore(c, factory, pair);
            var lostLeft = restored.A.RecordReplay(limits); var lostRight = restored.B.RecordReplay(limits);
            c.Equal(Codes.NotFound, RemoteBinding(restored.A).Blocker, "saved dangling selected identity remains truthfully unavailable");
            foreach (var current in new[] { pair, restored }) Submit(c, current, RemoteBind(current.A, other));
            Both(pair, restored, g => RemoteBinding(g).Provider == other, "explicit player rebind restores genuinely supplied live source");
            c.That(RemoteSignal(pair.A).Value == Fix.One && RemoteSight(pair.A) == SpatialLayerReadStatus.Success, "new explicit source reopens original live visibility gate");
            foreach (var current in new[] { pair, restored }) Submit(c, current, RemoteBind(current.A, EntityId.None));
            Both(pair, restored, g => RemoteBinding(g).Provider == EntityId.None, "explicit unbind uses same original reference owner");
            quiet = pair.A.Clock.StepIndex + 4; Both(pair, restored, g => g.Clock.StepIndex >= quiet, "unbound quiet continuation creates no fallback or extra work");
            c.That(RemoteSignal(pair.A).Blocker == RemoteSignalCodes.Unbound && RemoteSight(pair.A) == SpatialLayerReadStatus.Unavailable &&
                Physical.Facts(pair.A, other).Stock == Fix.One, "unbound never consumes supplied source or reports stale sight");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && restored.Stalls > 0 && restored.ReversedBatches > 0, "actual original and restored transports stall and shuffle");
            ReplayPair(c, factory, lostLeft, lostRight, restored, limits, 2, "saved historical missing provider to explicit rebind/unbind replay");
            ReplayPair(c, factory, left, right, pair, limits, 15, "full physical remote reference lifecycle replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced remote worlds converge");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "settled remote/stock/physical history survives final native save");
        }
    }
}
