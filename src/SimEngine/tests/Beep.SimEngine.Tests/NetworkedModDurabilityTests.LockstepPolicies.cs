using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Physical = Beep.SimEngine.Tests.PermanentRemovalTests;
using Authored = Beep.SimEngine.Tests.AuthoredLockstepPolicyTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string SmallLockstepCase = "Networked authored policy: four-command batches retain physical work and saved shuffled sessions";
        internal const string IndependentLockstepCase = "Networked policy contracts: independent half-minute large batches retain physical work and saved sessions";
        internal const string UnlimitedLockstepCase = "Networked unlimited policy: explicit null budgets retain large batches and saved physical sessions";
        private static LockstepPolicyView PolicyFacts(SimGame game)
            => game.Seat(0).View.Feature<LockstepPolicyView>() ?? throw new InvalidOperationException("actual selected lockstep policy missing");
        private static PlanView PolicyPlan(SimGame game, long id) => Physical.Plan(game, id);
        private static ConfigurePlanCommand PolicyConfigure(long plan, int priority, bool paused)
            => new ConfigurePlanCommand { Plan = plan, Group = "net", Priority = priority, Paused = paused };
        // Public committed answers are observations of game admission, not decoded
        // transport frames or a second queue/counter authority.
        private static List<CommandResult> PolicyBatchAnswers(SimGame game)
        {
            long cursor = 0; var rows = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, rows);
            return rows.Where(r => r.Player == new PlayerId(0) && r.Stream == CommandStreams.Player && r.Sequence >= 3 && r.Sequence <= 302)
                .OrderBy(r => r.Sequence).ToList();
        }
        private static string PolicyDiagnostics(SimGame game)
        {
            var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlanGroup("net", plans);
            return "at=" + game.Clock.StepIndex + "/policy=" + PolicyFacts(game).Policy + "/answers=" + PolicyBatchAnswers(game).Count +
                "/plans=" + string.Join(";", plans.Select(p => p.Id + "/" + p.State + "/" + p.Paused + "/" + p.Order + "/" + p.Priority + "/" + p.Blocker)) +
                "/jobs=" + string.Join(";", Physical.Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.ToilIndex + "/" + j.Loaded + "/" + j.Blocker));
        }
        private static Pair PolicyPair(SimGame a, SimGame b, string profile, long? ahead, long? commands)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long prior) && prior == game.Clock.StepIndex) return;
                var facts = PolicyFacts(game);
                if (facts.Policy != profile || facts.MaximumAhead != ahead || facts.MaximumCommandsPerFrame != commands ||
                    game.Net != null && (game.Net.MaximumAhead != ahead || game.Net.MaximumCommandsPerFrame != commands || game.Net.InputDelay != 2) ||
                    Physical.Total(game) != Fix.FromInt(20)) throw new InvalidOperationException("actual authored policy/finite stock changed/" + PolicyDiagnostics(game));
                var hidden = new List<PlanView>(); game.Seat(1).View.Feature<WorkView>().CopyPlanGroup("net", hidden);
                if (hidden.Count != 0) throw new InvalidOperationException("private network plan leaked");
                foreach (var row in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates)
                    if (row.Kind != ResourceFlowKind.Initialized && row.Kind != ResourceFlowKind.StockTransfer)
                        throw new InvalidOperationException("input policy fabricated or discarded real supplies");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, PolicyDiagnostics, 2);
        }
        private static Pair PolicyRestore(Check c, GameFactory factory, Pair source, string profile, long? ahead, long? commands)
        {
            var restored = PolicyPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()), profile, ahead, commands);
            c.Equal(source.A.Hash(), restored.A.Hash(), "saved actual work/stock/inbox/counters resolve same selected policy");
            c.Equal(source.B.Hash(), restored.B.Hash(), "other saved peer resolves same native state"); return restored;
        }
        private static void PolicyBatch(Check c, SimGame game, long? commands)
        {
            var answers = PolicyBatchAnswers(game); c.Equal(300, answers.Count, "all actual player batch inputs have committed answers");
            c.That(answers.Select(r => r.Sequence).SequenceEqual(Enumerable.Range(3, 300)) && answers.All(r => r.Accepted),
                "native producer identities retain every original accepted input exactly once");
            var epochs = answers.GroupBy(r => r.Tick).OrderBy(g => g.Key).ToList();
            c.Equal(commands == 4 ? 75 : 1, epochs.Count, "only original authored command budget partitions actual game admissions");
            c.That(epochs.All(g => g.Count() == (commands == 4 ? 4 : 300)), "actual per-epoch answers obey original four/400/null policy without old256 batching");
            c.Equal(commands == 4 ? 74L : 0L, epochs.Last().Key - epochs.First().Key, "bounded policy consumes actual consecutive game-clock epochs, not extra clocks");
        }
        private static void AuthoredPolicyRun(Check c, bool half, string profile, long? ahead, long? commands)
        {
            var factory = Authored.Factory(half, profile, new LockstepPolicyProofModule(), recordFlows: true);
            var pair = PolicyPair(Authored.New(factory), Authored.New(factory), profile, ahead, commands);
            Console.WriteLine("MODULES authored-policy=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Modules.Contains(LockstepPolicyModule.ModuleId) && !pair.A.Modules.Contains(TycoonModule.ModuleId), "explicit optional policy requires no business or platform");
            var selected = PolicyFacts(pair.A); var limits = new ReplayLimits(8 * 1024 * 1024, 3000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int hauled = Submit(c, pair, new HaulResourcesCommand { Source = FleetEntity(pair.A, "source"), Destination = FleetEntity(pair.A, "destination"),
                Worker = FleetEntity(pair.A, "machine"), Resource = Physical.Supply(pair.A), Ceiling = Fix.FromInt(4), Priority = 100 });
            int planned = Submit(c, pair, new SubmitPlanCommand { Intent = new PlanIntent("activity:inspect",
                Json.Parse("{\"target\":" + FleetEntity(pair.A, "source").Value + "}"), "net", 1, true) });
            bool Loaded(SimGame game) => Result(game, 0, planned)?.Accepted == true && Physical.Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero && j.Loaded < Fix.FromInt(4)) &&
                Physical.Facts(game, FleetEntity(game, "destination")).Stock.IsZero;
            Reach(pair, Loaded, "original physical cargo loads partially while native plan remains paused");
            long plan = Result(pair.A, 0, planned).Value.Arg0;
            c.That(Result(pair.A, 0, hauled)?.Accepted == true && PolicyPlan(pair.A, plan).Paused && PolicyPlan(pair.A, plan).Order == 0,
                "actual native hauling has not delivered stock or activated paused work");
            c.Equal(2L, left.CommandCount, "two prelude commands are actually admitted before saving");
            var restored = PolicyRestore(c, factory, pair, profile, ahead, commands);
            var batchLeft = restored.A.RecordReplay(limits); var batchRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored })
                for (int i = 1; i <= 300; i++) c.Equal(i + 2, Submit(c, current, PolicyConfigure(plan, i, i != 300)),
                    "each pending proposal retains original native producer sequence");
            c.That(left.CommandCount == 2 && batchLeft.CommandCount == 0, "unadmitted transport proposals are not canonical game commands or saved-world claims");
            Both(pair, restored, g => PolicyBatchAnswers(g).Count == 300 && PolicyPlan(g, plan).Priority == 300 && PolicyPlan(g, plan).Order > 0,
                "native authored batching admits whole ordered inputs before physical plan work");
            foreach (var game in new[] { pair.A, pair.B, restored.A, restored.B }) PolicyBatch(c, game, commands);
            c.That(!PolicyPlan(pair.A, plan).Paused && PolicyPlan(pair.A, plan).State == PlanState.Active,
                "last real input activates only the original plan once");
            ReplayPair(c, factory, batchLeft, batchRight, restored, limits, 300, "saved partial cargo through actual selected-policy batch admission replay");
            restored = PolicyRestore(c, factory, pair, profile, ahead, commands);
            var admittedLeft = restored.A.RecordReplay(limits); var admittedRight = restored.B.RecordReplay(limits);
            int order = PolicyPlan(pair.A, plan).Order;
            bool Productive(SimGame game) => LoadNodes(game, order).Any(n => n.Progress.WholeTicks > 0 && n.Progress.WholeTicks < n.RequiredTicks);
            Both(pair, restored, Productive, "original carrier finishes real delivery then travels back for actual staffed inspection");
            var partial = Physical.Node(pair.A, order, "check");
            c.That(partial.Worker == FleetEntity(pair.A, "machine") && partial.RequiredTicks == (half ? 12 : 6) &&
                Physical.At(pair.A, partial.Worker) == Fix.Zero && Physical.Facts(pair.A, FleetEntity(pair.A, "destination")).Stock == Fix.FromInt(4),
                "original six game minutes follow sole minute/half-minute quantum and require actual arrival/full hauling");
            ReplayPair(c, factory, admittedLeft, admittedRight, restored, limits, 0, "saved admitted inputs to physical six-minute work prefix replay");
            restored = PolicyRestore(c, factory, pair, profile, ahead, commands);
            var workLeft = restored.A.RecordReplay(limits); var workRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) c.Equal(303, Submit(c, current, PolicyConfigure(plan, 300, true)), "reattached native input sequence survives large admission");
            Both(pair, restored, g => PolicyPlan(g, plan).Paused, "ordinary delayed player plan pause holds actual earned work");
            var pausedProgress = Physical.Node(pair.A, order, "check").Progress;
            c.That(pausedProgress.WholeTicks >= partial.Progress.WholeTicks && pausedProgress.WholeTicks < partial.RequiredTicks,
                "input delay permits only real earlier work, never premature completion or rollback");
            ReplayPair(c, factory, workLeft, workRight, restored, limits, 1, "saved earned work through actual delayed pause replay");
            restored = PolicyRestore(c, factory, pair, profile, ahead, commands);
            var heldLeft = restored.A.RecordReplay(limits); var heldRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native sole-clock pause with original policy-held inspection");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, g => g.Clock.StepIndex >= paused + 4, "separate saved peers advance real clock while retaining paused plan"); pair.Round();
            c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen && restored.A.Clock.StepIndex > paused &&
                Physical.Node(restored.A, order, "check").Progress.Equals(pausedProgress), "current policy/world/clock scopes remain isolated across actual advancing sessions");
            pair.A.Net.RequestResume(); Both(pair, restored, g => g.Clock.StepIndex >= paused + 4, "original native clock catches up without repeated input or work");
            foreach (var current in new[] { pair, restored }) c.Equal(304, Submit(c, current, PolicyConfigure(plan, 300, false)), "actual resumed plan uses saved next native input identity");
            Both(pair, restored, g => !PolicyPlan(g, plan).Paused && Physical.Node(g, order, "check").Progress.WholeTicks > pausedProgress.WholeTicks,
                "normal delayed resume earns only fresh physical work");
            c.Equal(order, PolicyPlan(pair.A, plan).Order, "resumption preserves sole actual execution identity");
            ReplayPair(c, factory, heldLeft, heldRight, restored, limits, 1, "saved policy-held work through native clock isolation and normal resume replay");
            restored = PolicyRestore(c, factory, pair, profile, ahead, commands);
            var resumedLeft = restored.A.RecordReplay(limits); var resumedRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => PolicyPlan(g, plan).State == PlanState.Done, "full original timed inspection actually completes");
            long quiet = pair.A.Clock.StepIndex + 4; Both(pair, restored, g => g.Clock.StepIndex >= quiet, "completed authored-policy world stays settled");
            c.That(Physical.Facts(pair.A, FleetEntity(pair.A, "source")).Stock == Fix.FromInt(16) && Physical.Facts(pair.A, FleetEntity(pair.A, "destination")).Stock == Fix.FromInt(4) &&
                pair.A.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:inspect", FleetEntity(pair.A, "source"), out var done) && done.Order == order,
                "full real delivery and once-only original work history remain authoritative");
            BigInteger moved = pair.A.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(r => r.Kind == ResourceFlowKind.StockTransfer && r.Resource == Physical.Supply(pair.A)).Aggregate(BigInteger.Zero, (sum, r) => sum + r.AmountRaw);
            c.Equal(new BigInteger(Fix.FromInt(8).Raw), moved, "actual native journal records four into carrier and four delivered, never input-volume grants");
            c.That(selected.Policy == profile && selected.MaximumAhead == ahead && selected.MaximumCommandsPerFrame == commands &&
                pair.Stalls > 0 && pair.ReversedBatches > 0 && restored.Stalls > 0 && restored.ReversedBatches > 0,
                "retained immutable policy and real stalled/shuffled transports keep original semantics");
            ReplayPair(c, factory, resumedLeft, resumedRight, restored, limits, 0, "saved resumed physical inspection to settled policy world replay");
            ReplayPair(c, factory, left, right, pair, limits, 304, "full native authored-policy physical stream replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "four actually advanced policy worlds converge");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "settled world saves through original codecs, policy still resolved from content");
        }
    }
}
