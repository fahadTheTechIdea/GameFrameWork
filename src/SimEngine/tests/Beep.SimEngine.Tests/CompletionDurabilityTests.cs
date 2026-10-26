using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Ordinary player input, detached facts, engine Update/save/replay; no history or progress writer.
    internal static class CompletionDurabilityTests
    {
        internal const string Case = "Completion durability: earned prerequisites and active nodes in saved player chains";
        private const string Preparation = "activity:prior.prepare";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(out PriorActivityProofModule proof)
        {
            proof = new PriorActivityProofModule();
            return PriorActivityTests.Factory(proof: proof,
                dependencies: FrameworkProofContent.Read("completion-durability.json"),
                proofRequirements: FrameworkProofContent.Read("completion-durability-requirements.json"));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 734 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static EntityId Site(SimGame game, int index) => game.Seat(0).Snapshot.Entities.Where(e => e.Def == "prior.site").OrderBy(e => e.Id.Value).ElementAt(index).Id;
        private static EntityId Machine(SimGame game) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "survey.machine").Id;
        private static Fix Stock(SimGame game, EntityId id)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(id, rows);
            return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static List<OrderView> Orders(SimGame game, int seat = 0)
        { var rows = new List<OrderView>(); View(game, seat).CopyOrders(rows); return rows; }
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        { var rows = new List<ActivityNodeView>(); View(game).TryCopyNodes(order, rows); return rows; }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("completion step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " orders="
                + string.Join(";", Orders(game).Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static int Start(Check c, SimGame game, string plan, EntityId target)
        {
            var command = new PostOrderCommand { Plan = plan, Target = target, Priority = 60 };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary prior intent admission");
            c.Equal(hash, game.Hash(), "intent preview earns no work or completion");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary authored order accepted"); return (int)result.Arg0;
        }
        private static void Policy(Check c, SimGame game, bool enabled)
        {
            var command = new ConfigureWorkPolicyCommand { Worker = Machine(game),
                Settings = new WorkPolicySettings(enabled, 1, new[] { new WorkerRoleId("survey") }, null, true, false, WorkAssignmentMode.All) };
            c.Equal(Code.None, game.Seat(0).Preview(command), "engine work policy admission");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary work policy change");
        }
        private static void AssertNodes(Check c, SimGame game, SimGame twin, int order, string stage)
        {
            c.Equal(game.Hash(), twin.Hash(), stage + " identical original saved state");
            var before = Nodes(game, order); var after = Nodes(twin, order);
            c.That(before.Count == after.Count && before.Select(n => n.Id).SequenceEqual(after.Select(n => n.Id)), stage + " stable graph node IDs");
            for (int i = 0; i < before.Count; i++)
            {
                var a = before[i]; var b = after[i];
                c.That(a.State == b.State && a.JobId == b.JobId && a.Worker == b.Worker && a.CompletedBy == b.CompletedBy
                    && a.Progress.Equals(b.Progress) && a.ProductiveSteps == b.ProductiveSteps && a.Blocker == b.Blocker
                    && a.Dependencies.SequenceEqual(b.Dependencies) && a.Choices.Count == b.Choices.Count
                    && a.Resources.Count == b.Resources.Count, stage + " actual saved node work/claims/choices/links");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current completion protocol");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current protocol history/work persistent codecs");
            var factory = Factory(out var proof); var game = New(factory); EntityId a = Site(game, 0), b = Site(game, 1);
            c.That(proof.Initializations == 1 && !game.Modules.Contains("sim.tycoon"), "actual authored contracts and resource-only composition");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits);
            ulong hash = game.Hash(); var criteria = new List<WorkRequirementView>();
            c.That(!View(game).TryReadActivityCompletion(Preparation, a, out _), "startup never grants earned completion");
            c.Equal(Code.None, View(game).PreviewOrderRequirements("durability.dependent", a, criteria), "authored dependent diagnostics");
            c.That(criteria.Any(r => r.Result.Blocker == new Code("prior.target-unprepared"))
                && criteria.Any(r => r.Result.Blocker == new Code("proof.prepare-incomplete")), "native and independent requirements share real absence");
            c.Equal(hash, game.Hash(), "history and nested diagnostics pure");
            int dependent = Start(c, game, "durability.dependent", a);
            c.That(Nodes(game, dependent).All(n => n.Progress.Equals(WorkProgress.Zero) && n.JobId == 0), "blocked prerequisite starts no physical dependent work");
            var twin = factory.Load(game.Save()); AssertNodes(c, game, twin, dependent, "blocked dependent");
            int prepare = Start(c, game, "prior.prepare", a); twin = factory.Load(game.Save());
            Reach(game, twin, () => Nodes(game, prepare).Any(n => n.ProductiveSteps > 0), "partial prerequisite");
            c.That(Stock(game, a) == Fix.FromInt(24) && !View(game).TryReadActivityCompletion(Preparation, a, out _), "partial actual work consumes eight but earns no completion fact");
            AssertNodes(c, game, factory.Load(game.Save()), prepare, "productive prerequisite");
            Reach(game, twin, () => View(game).TryReadActivityCompletion(Preparation, a, out _), "earned full preparation");
            c.That(View(game).TryReadActivityCompletion(Preparation, a, out var first) && first.Order == prepare && first.Target == a,
                "compact evidence retains exact completed execution and target");
            c.That(!Orders(game).Any(o => o.Id == prepare), "terminal prerequisite pruned, evidence not an active-order fallback");
            c.That(first.CompletedTick >= first.CreatedTick && first.CompletedTick <= game.Clock.StepIndex, "one game clock owns earned chronology");
            twin = factory.Load(game.Save());
            c.That(View(twin).TryReadActivityCompletion(Preparation, a, out var saved) && saved.Order == first.Order
                && saved.CreatedTick == first.CreatedTick && saved.CompletedTick == first.CompletedTick, "compact prerequisite persists beside actual active dependent graph");
            Reach(game, twin, () => Nodes(game, dependent).Any(n => n.Id == "first" && n.ProductiveSteps > 0), "dependent productive first node");
            var retained = Nodes(game, dependent); c.That(retained.Count == 2 && retained.Single(n => n.Id == "second").Dependencies.SequenceEqual(new[] { "first" }), "authored out-of-order nodes keep stable compiled dependencies");
            c.That(Nodes(game, dependent).Single(n => n.Id == "second").Progress.Equals(WorkProgress.Zero), "dependent node cannot run before earned predecessor");
            Policy(c, game, false); twin = factory.Load(game.Save()); AssertNodes(c, game, twin, dependent, "policy-paused active graph");
            var paused = Nodes(game, dependent).Select(n => n.Progress).ToArray();
            for (int i = 0; i < 4; i++) Step(game, twin);
            c.That(Nodes(game, dependent).Select(n => n.Progress).SequenceEqual(paused), "disabled actual worker preserves active earned progress");
            hash = game.Hash(); long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == hash, "sole clock pause freezes original history and active nodes");
            game.Clock.Resume(); twin.Clock.Resume(); Policy(c, game, true); twin = factory.Load(game.Save());
            Reach(game, twin, () => Nodes(game, dependent).Any(n => n.Id == "second" && n.ProductiveSteps > 0), "saved second node");
            AssertNodes(c, game, factory.Load(game.Save()), dependent, "earned first and active second");
            c.That(View(game).TryReadProgress(dependent, out var progress) && progress.CompletedNodes == 1 && progress.UnfinishedNodes == 1, "engine progress comes from actual graph");
            Reach(game, twin, () => View(game).TryReadActivityCompletion("activity:durability.dependent", a, out _), "full dependent chain");
            c.That(Stock(game, a) == Fix.FromInt(24) && retained.Any(n => n.ProductiveSteps > 0)
                && retained.Single(n => n.Id == "second").Progress.Equals(WorkProgress.Zero) && progress.UnfinishedNodes == 1, "no duplicated prerequisite cost and retained views detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "all completed facts canonical checkpoint");
            c.That(!View(game, 1).TryReadActivityCompletion(Preparation, a, out _) && Orders(game, 1).Count == 0, "foreign seat sees no private completion or activity");
            c.That(!View(game).TryReadActivityCompletion(Preparation, b, out _) && View(game).TryReadActivityCompletion(Preparation, EntityId.None, out var company)
                && company.Order == first.Order, "explicit company scope does not fabricate another target's completion");
            criteria.Clear(); c.Equal(Code.None, View(game).PreviewOrderRequirements("durability.dependent", b, criteria), "other target diagnostics");
            c.That(criteria.Any(r => r.Result.Blocker == new Code("prior.target-unprepared")), "target prerequisite still blocks other site");
            int cancel = Start(c, game, "prior.prepare", a);
            Reach(game, null, () => Nodes(game, cancel).Any(n => n.ProductiveSteps > 0), "partial repeated preparation");
            game.Seat(0).Submit(new CancelOrderCommand { Order = cancel }); twin = factory.Load(game.Save()); Step(game, twin);
            c.That(View(game).TryReadActivityCompletion(Preparation, a, out var afterCancel) && afterCancel.Order == first.Order
                && Stock(game, a) == Fix.FromInt(16) && !Orders(game).Any(o => o.Id == cancel), "saved cancellation spends own eight, never replaces earned fact or refunds");
            int repeat = Start(c, game, "prior.prepare", a); twin = factory.Load(game.Save());
            Reach(game, twin, () => View(game).TryReadActivityCompletion(Preparation, a, out var fact) && fact.Order == repeat, "fresh earned repeated completion");
            c.That(View(game).TryReadActivityCompletion(Preparation, a, out var latest) && latest.Order > first.Order
                && latest.CompletedTick > first.CompletedTick && Stock(game, a) == Fix.FromInt(8) && first.Order == prepare, "latest exact execution replaces compact key without mutating retained fact");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "replaced compact history durable");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary commands, policies and physical completion replay");
            var isolated = New(factory);
            for (int i = 0; i < 3; i++) { Step(isolated); Step(game, twin); }
            c.That(!View(isolated).TryReadActivityCompletion(Preparation, EntityId.None, out _) && Stock(isolated, Site(isolated, 0)) == Fix.FromInt(32), "interleaved sessions share no completion or stock");
            Faulted(c, game, proof, a, dependent, retained);
        }
        private static void Faulted(Check c, SimGame game, PriorActivityProofModule proof, EntityId target, int oldOrder, List<ActivityNodeView> retained)
        {
            proof.FaultNextUpdate = true; game.Update();
            c.That(game.Fault != null && game.Fault.Message.Contains("proof.prior.first"), "original update failure latched");
            var fact = default(ActivityCompletionFact); var rows = retained.ToList(); var criteria = new List<WorkRequirementView>();
            c.Throws<InvalidOperationException>(() => View(game).TryReadActivityCompletion(Preparation, target, out fact), "faulted earned fact query refuses");
            c.That(fact.Order == 0, "failed completion output remains cleared");
            c.Throws<InvalidOperationException>(() => View(game).TryReadActivityCompletion(null, EntityId.None, out _), "faulted missing completion query refuses before early return");
            c.Throws<InvalidOperationException>(() => View(game, 1).TryReadServiceCompletion(null, EntityId.None, Code.None, Code.None, out _), "faulted private invalid service query refuses");
            c.Throws<InvalidOperationException>(() => proof.FaultedHistory.TryReadCompletion(new PlayerId(-2), null, EntityId.None, out _), "actual history invalid company cannot bypass fault gate");
            c.Throws<InvalidOperationException>(() => proof.FaultedHistory.TryReadServiceCompletion(new PlayerId(-2), null, EntityId.None, Code.None, Code.None, out _), "actual service history fault gate");
            c.Throws<InvalidOperationException>(() => proof.FaultedHistory.IsActivity(null), "faulted activity identity query refuses");
            c.Throws<InvalidOperationException>(() => View(game).TryCopyNodes(oldOrder, rows), "faulted pruned node query refuses"); c.Equal(0, rows.Count, "failed node list cleared");
            c.Throws<InvalidOperationException>(() => View(game).TryCopyOrderRequirements(oldOrder, criteria), "faulted missing active requirements refuse"); c.Equal(0, criteria.Count, "failed criteria cleared");
            c.Throws<InvalidOperationException>(() => View(game).PreviewOrderRequirements(null, EntityId.None, criteria), "faulted invalid preview refuses");
            c.Throws<InvalidOperationException>(() => View(game).TryReadProgress(oldOrder, out _), "faulted pruned progress refuses");
            c.Throws<InvalidOperationException>(() => Orders(game), "faulted active-order publication refuses");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted earned state cannot checkpoint");
            c.Throws<InvalidOperationException>(() => game.Hash(), "faulted earned state cannot hash as healthy");
            c.That(game.Fault.Message.Contains("proof.prior.first") && retained.Any(n => n.ProductiveSteps > 0), "query rejection preserves first error and previously detached facts");
        }
    }
}
