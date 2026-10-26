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
using Volume = Beep.SimEngine.Tests.AuthoredEventVolumeTests;
using Physical = Beep.SimEngine.Tests.PermanentRemovalTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string EventPolicyCase = "Networked event policy: saved physical million-event delivery, native finite safety policy";
        internal const string IndependentEventPolicyCase = "Networked event policy contracts: saved physical million-event delivery, independent finite safety policy";
        private static long EventCount(SimGame game) => Volume.Events(game).ReadCount(new PlayerId(0));
        private static string EventDiagnostics(SimGame game) => "at=" + game.Clock.StepIndex + "/events=" + EventCount(game) +
            "/late=" + Volume.Events(game).ReadLate(new PlayerId(0)) + "/stock=" + Physical.Total(game) +
            "/plans=" + string.Join(";", EventPlans(game).Select(p => p.Id + "/" + p.State + "/" + p.Blocker));
        private static List<PlanView> EventPlans(SimGame game)
        { var rows = new List<PlanView>(); Volume.Work(game).CopyPlans(rows); return rows; }
        private static Pair EventPair(SimGame a, SimGame b)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long prior) && prior == game.Clock.StepIndex) return;
                if (Physical.Total(game) != Fix.FromInt(20)) throw new InvalidOperationException("event dispatch altered finite physical stock");
                long count = EventCount(game); int late = Volume.Events(game).ReadLate(new PlayerId(0));
                if (count < 0 || count % Volume.Burst != 0 || late != (count == 0 ? 0 : Volume.Burst))
                    throw new InvalidOperationException("partial/missing/duplicate downstream event receipt/" + EventDiagnostics(game));
                if (Volume.Events(game, 1).ReadCount(new PlayerId(0)) != 0 || Volume.Events(game).ReadCount(new PlayerId(1)) != 0 ||
                    Volume.Events(game, 1).ReadLate(new PlayerId(0)) != 0 || Volume.Events(game).ReadLate(new PlayerId(1)) != 0)
                    throw new InvalidOperationException("private downstream event receipts leaked across companies");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, EventDiagnostics, 0);
        }
        private static Pair EventRestore(Check c, GameFactory factory, Pair source)
        {
            var restored = EventPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()));
            c.Equal(source.A.Hash(), restored.A.Hash(), "actual event policy/receipts/physical work save together");
            c.Equal(source.B.Hash(), restored.B.Hash(), "other peer restores its exact canonical event state");
            c.Equal(EventCount(source.A), EventCount(restored.A), "real downstream cumulative receipts persist");
            return restored;
        }
        private static bool EventProductive(SimGame game, long plan)
        {
            var value = Volume.Plan(game, plan); var nodes = new List<ActivityNodeView>();
            return value.Order > 0 && Volume.Work(game).TryCopyNodes(value.Order, nodes) && nodes.Single().ProductiveSteps > 0 && nodes.Single().State != NodeState.Done;
        }
        private static bool EventLastPrefix(SimGame game, long plan)
        {
            var nodes = new List<ActivityNodeView>(); var value = Volume.Plan(game, plan);
            return value.Order > 0 && Volume.Work(game).TryCopyNodes(value.Order, nodes) &&
                nodes.Single().ProductiveSteps == nodes.Single().RequiredTicks - 1 && nodes.Single().State != NodeState.Done;
        }
        private static void EventPolicyRun(Check c, bool independent)
        {
            string profile = independent ? "events.independent" : "events.large";
            var factory = Volume.Factory(independent, profile, new EventVolumeModule());
            var pair = EventPair(Volume.New(factory), Volume.New(factory));
            Console.WriteLine("MODULES event-policy=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            var policy = pair.A.Seat(0).View.Feature<EventDispatchPolicyView>();
            c.That(policy.Policy == profile && policy.MaximumEventsPerDispatch == 2000000L && !pair.A.Modules.Contains(TycoonModule.ModuleId),
                "original explicit native/contract safety policy is not a default engine cap or business requirement");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            Pair restored = null; int issued = 0;
            int Input(Func<SimGame, Command> make, Code expected = default, int seat = 0) {
                int ticket = Submit(c, pair, make(pair.A), expected, seat); issued++;
                if (restored != null) c.Equal(ticket, Submit(c, restored, make(restored.A), expected, seat), "saved peers preserve actual player proposal identity");
                bool Answered(SimGame game) => Result(game, seat, ticket)?.Refusal == expected;
                if (restored == null) Reach(pair, Answered, "ordinary event-work player command"); else Both(pair, restored, Answered, "same actual command on saved event peers");
                return ticket;
            }
            long plan = Result(pair.A, 0, Input(game => Volume.PlanCommand(game, true))).Value.Arg0;
            Input(_ => new ConfigurePlanCommand { Plan = plan, Group = "events", Priority = 50, Paused = false }, Codes.NotOwned, 1);
            Input(Volume.SupplyCommand);
            Reach(pair, game => Physical.Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero && j.Loaded < Fix.FromInt(4)), "original carrier earns partial loading over actual time");
            c.That(EventCount(pair.A) == 0 && Volume.Plan(pair.A, plan).Paused, "paused intent and partial physical haul create no completion receipt");
            restored = EventRestore(c, factory, pair);
            var haulLeft = restored.A.RecordReplay(limits); var haulRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native pause on actual partial supply");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 8, "independent saved peers advance real physical supply while original is paused");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen && EventCount(pair.A) == 0, "paused original borrows no material, work or event receipts");
            pair.A.Net.RequestResume(); Both(pair, restored, game => game.Clock.StepIndex >= paused + 8, "native resumed peers converge with independently advanced supply");
            Both(pair, restored, game => Physical.Facts(game, Volume.Entity(game, "destination")).Stock == Fix.FromInt(4) &&
                Physical.Facts(game, Volume.Entity(game, "machine")).Stock.IsZero, "four actual units arrive without event-count inventory conversion");
            ReplayPair(c, factory, haulLeft, haulRight, restored, limits, 0, "saved partial haul through isolated supply delivery replay");
            restored = EventRestore(c, factory, pair);
            var readyLeft = restored.A.RecordReplay(limits); var readyRight = restored.B.RecordReplay(limits);
            Input(_ => new ConfigurePlanCommand { Plan = plan, Group = "events", Priority = 50, Paused = false });
            Both(pair, restored, game => EventProductive(game, plan), "physical inspection earns positive six-minute work before downstream effect");
            c.Equal(0L, EventCount(pair.A), "productive partial work cannot publish final burst");
            ReplayPair(c, factory, readyLeft, readyRight, restored, limits, 1, "saved supplied paused intent through positive physical work replay");
            restored = EventRestore(c, factory, pair);
            var workLeft = restored.A.RecordReplay(limits); var workRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => EventLastPrefix(game, plan), "observe final actual productive prefix before native completion pruning");
            int originalOrder = Volume.Plan(pair.A, plan).Order;
            var prefix = Physical.Node(pair.A, originalOrder, "check");
            c.That(prefix.RequiredTicks == pair.A.Clock.Time.ExactTicks(GameDuration.Minutes(6)) &&
                pair.A.Clock.Time.ExactTicks(pair.A.Clock.StepDuration.Times(prefix.ProductiveSteps + 1)) == prefix.RequiredTicks && EventCount(pair.A) == 0,
                "original six-minute graph earns every earlier productive step without premature events");
            Both(pair, restored, game => Volume.Plan(game, plan).State == PlanState.Done, "same physical inspection finishes and all million-plus events dispatch");
            c.Equal((long)Volume.Burst, EventCount(pair.A), "every event beyond the old global cap arrives exactly once");
            c.Equal(Volume.Burst, Volume.Events(pair.A).ReadLate(new PlayerId(0)), "last listener receives complete stable sequence");
            c.That(Volume.Work(pair.A).TryReadActivityCompletion("activity:events.inspect", Volume.Entity(pair.A, "destination"), out var completed) && completed.Order == originalOrder,
                "native compact completion receipt retains actual finished order identity after execution pruning");
            long finished = pair.A.Clock.StepIndex; long held = EventCount(pair.A);
            Both(pair, restored, game => game.Clock.StepIndex >= finished + 3, "terminal updates dispatch no duplicate receipt");
            c.Equal(held, EventCount(pair.A), "finished effect is not replayed on later updates");
            ReplayPair(c, factory, workLeft, workRight, restored, limits, 0, "saved positive work through full ordered delivery and quiet updates replay");
            restored = EventRestore(c, factory, pair);
            var repeatLeft = restored.A.RecordReplay(limits); var repeatRight = restored.B.RecordReplay(limits);
            int repeated = Input(_ => new RepeatPlanCommand { Plan = plan }); long cancelled = Result(pair.A, 0, repeated).Value.Arg0;
            Both(pair, restored, game => EventProductive(game, cancelled), "ordinary repeat earns real new partial work");
            c.That(cancelled > plan && EventCount(pair.A) == Volume.Burst, "new order neither reuses identity nor republishes old events");
            ReplayPair(c, factory, repeatLeft, repeatRight, restored, limits, 1, "saved terminal receipts through genuinely new partial work replay");
            restored = EventRestore(c, factory, pair);
            var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            Input(_ => new CancelPlanGroupCommand { Group = "events" });
            c.That(Volume.Plan(pair.A, cancelled).State == PlanState.Cancelled && EventCount(pair.A) == Volume.Burst,
                "cancelling earned prefix grants no final effect or receipt refund");
            int finalRepeat = Input(_ => new RepeatPlanCommand { Plan = plan }); long final = Result(pair.A, 0, finalRepeat).Value.Arg0;
            Both(pair, restored, game => Volume.Plan(game, final).State == PlanState.Done, "fresh final order earns a separate complete downstream sequence");
            c.That(final > cancelled && EventCount(pair.A) == 2L * Volume.Burst && Volume.Events(pair.A).ReadLate(new PlayerId(0)) == Volume.Burst,
                "cumulative receipts retain first burst and settle distinct second burst exactly once");
            c.That(Physical.Facts(pair.A, Volume.Entity(pair.A, "source")).Stock == Fix.FromInt(16) &&
                Physical.Facts(pair.A, Volume.Entity(pair.A, "destination")).Stock == Fix.FromInt(4), "actual four-unit transfer remains finite after two event bursts");
            c.That(prefix.ProductiveSteps == prefix.RequiredTicks - 1 && completed.Order == originalOrder &&
                Volume.Work(pair.A).TryReadActivityCompletion("activity:events.inspect", Volume.Entity(pair.A, "destination"), out var latest) && latest.Order > completed.Order &&
                policy.Policy == profile && held == Volume.Burst,
                "held physical/policy/scalar facts remain detached from later real orders");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "native saved/original transports really stall and reverse opaque packet batches");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 2, "saved repeated prefix through cancellation and fresh final full delivery replay");
            ReplayPair(c, factory, left, right, pair, limits, issued, "full saved physical event-policy sequence replay");
            c.Equal(7, issued, "independent count includes one actual foreign refusal and six owner commands");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced event worlds converge");
            EventRestore(c, factory, pair);
        }
    }
}
