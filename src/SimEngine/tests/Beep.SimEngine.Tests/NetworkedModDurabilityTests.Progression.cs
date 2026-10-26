using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Progression = Beep.SimEngine.Tests.NonspatialProgressionTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string ProgressionCase = "Networked mapless progression: earned research, saved production control and private objective replay";
        private static ResourceId ProgressionSupply(SimGame game)
        {
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("authored process supply missing");
            return resource;
        }
        private static void ProgressionObserve(SimGame game)
        {
            var process = Progression.Process(game);
            Fix accounted = Progression.Stock(game) + process.Wip.Stock.AmountOf(ProgressionSupply(game)) + Fix.FromInt(2 * Progression.Tokens(game));
            if (accounted != Fix.FromInt(20) && accounted != Fix.FromInt(18) || Progression.Tokens(game) > 2 ||
                Progression.Research(game, 1).Known || Progression.Objectives(game, 1).Count != 0)
                throw new InvalidOperationException("finite process/private progression invariant/" + Progression.Diagnostics(game));
        }
        private static Pair ProgressionPair(SimGame a, SimGame b) => new Pair(a, b, ProgressionObserve, Progression.Diagnostics, 2);
        private static ConfigurePlanCommand ProgressionPlan(long plan, bool paused) => new ConfigurePlanCommand {
            Plan = plan, Group = "qualification", Priority = 70, Paused = paused };
        private static ConfigureOperationCommand ProgressionOperation(SimGame game, bool enabled) => new ConfigureOperationCommand {
            Entity = Progression.Assembler(game).Id, Operation = new Code("transformation"), Enabled = enabled, Target = Fix.One };
        private static bool ProgressionDisabled(SimGame game)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetControl(Progression.Assembler(game).Id, new Code("transformation"), out var control))
                throw new InvalidOperationException("authored process operation missing");
            return !control.Enabled;
        }
        private static void ProgressionRun(Check c)
        {
            var factory = Progression.Factory(); var pair = ProgressionPair(Progression.New(factory, 839), Progression.New(factory, 839));
            Console.WriteLine("MODULES progression=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Snapshot.Space == null && Progression.Assembler(pair.A).Spatial == null &&
                !pair.A.Modules.Contains(TycoonModule.ModuleId) && !pair.A.Modules.Contains(TileNavigationModule.ModuleId) &&
                !pair.A.Modules.Contains(LogisticsModule.ModuleId), "actual research and production have no fabricated map, money or hauling prerequisite");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int locked = Submit(c, pair, Progression.Build(pair.A, 2), TechnologyCodes.Locked);
            int proposed = Submit(c, pair, new SubmitPlanCommand { Intent = new PlanIntent("activity:process.qualification",
                Json.Parse("{\"target\":" + Progression.Assembler(pair.A).Id.Value + "}"), "qualification", 30, true) });
            Reach(pair, game => Result(game, 0, proposed)?.Accepted == true, "real paused research intent admitted");
            long plan = Result(pair.A, 0, proposed).Value.Arg0;
            c.That(plan > 0 && Result(pair.A, 0, locked)?.Refusal == TechnologyCodes.Locked &&
                Result(pair.B, 0, locked)?.Refusal == TechnologyCodes.Locked && Progression.Process(pair.A).QueueCount == 0,
                "locked production refusal creates neither queued output nor an unlock");
            int foreign = Submit(c, pair, ProgressionPlan(plan, false), Codes.NotOwned, seat: 1);
            Reach(pair, game => Result(game, 1, foreign)?.Refusal == Codes.NotOwned, "same foreign plan refusal on both real peers");
            c.That(Progression.Plan(pair.A, plan).Paused && !Progression.Research(pair.A).Known && Progression.Stock(pair.A) == Fix.FromInt(20),
                "planned paused intent grants no work, knowledge or material debit");
            var initial = Progression.Research(pair.A);
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native pause before active research");
            long paused = pair.A.Clock.StepIndex; ulong held = pair.A.Hash();
            var restored = ProgressionPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var partialLeft = restored.A.RecordReplay(limits); var partialRight = restored.B.RecordReplay(limits);
            c.Equal(held, restored.A.Hash(), "paused research intent and command counters reattach exactly");
            c.That(!restored.A.Clock.Paused, "local host pause is not a second authoritative game clock in the save");
            int resumed = Submit(c, pair, ProgressionPlan(plan, false));
            c.Equal(resumed, Submit(c, restored, ProgressionPlan(plan, false)), "saved plan resume retains actual player command sequence");
            bool PartialResearch(SimGame game) => Progression.Research(game).ActiveOrder > 0 && Progression.Nodes(game)
                .Any(node => node.Progress.WholeTicks > 0 && node.Progress.WholeTicks < node.RequiredTicks);
            Reach(restored, PartialResearch, "real partial research in independently advancing saved session");
            pair.Round();
            c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == held && !Progression.Research(pair.A).Known &&
                Progression.Stock(pair.A) == Fix.FromInt(20), "original paused network session retains its own untouched research and stock");
            c.That(restored.A.Clock.StepIndex > paused && Progression.Stock(restored.A) == Fix.FromInt(18) &&
                !Progression.Research(restored.A).Known && Progression.Tokens(restored.A) == 0,
                "other session actually earns partial work and pays exactly two finite research materials without early output");
            var partial = Progression.Nodes(restored.A);
            ReplayPair(c, factory, partialLeft, partialRight, restored, limits, 1, "paused-intent to earned-partial-research replay");
            restored = ProgressionPair(factory.Load(restored.A.Save()), factory.Load(restored.B.Save()));
            var researchLeft = restored.A.RecordReplay(limits); var researchRight = restored.B.RecordReplay(limits);
            c.That(PartialResearch(restored.A) && Progression.Nodes(restored.A).Select(node => node.Progress).SequenceEqual(partial.Select(node => node.Progress)),
                "partial graph execution resumes, not a new research order or material charge");
            pair.A.Net.RequestResume(); Both(pair, restored, game => Progression.Research(game).Known, "real earned qualification after native resume");
            c.That(!initial.Known && !Progression.Research(pair.A, 1).Known && Progression.Stock(pair.A) == Fix.FromInt(18),
                "detached initial facts and foreign company never inherit research or repeat its cost");
            c.That(pair.A.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), TechnologyModule.ModuleId, "process.qualification", out var learned) &&
                !pair.A.Seat(1).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), TechnologyModule.ModuleId, "process.qualification", out _),
                "native technology uses the same private company knowledge ledger, not a parallel known flag");
            long admitted = Result(pair.A, 0, resumed).Value.Tick; long? completed = Progression.Research(pair.A).CompletedTick;
            Console.WriteLine("PROGRESSION research admitted=" + admitted + "/completed=" + completed + "/observed=" + pair.A.Clock.StepIndex);
            c.That(completed.HasValue && completed.Value - admitted + 1 >= pair.A.SimulationTime.Ticks(GameDuration.Minutes(6)) &&
                learned.LearnedTick == completed.Value, "sequential two-plus-four-minute research earns full declared game time and one shared knowledge timestamp");
            c.That(Progression.Objectives(pair.A).Single(value => value.Objective == "process.research").State == ObjectiveState.Completed &&
                pair.A.Outcome.IsNone && Progression.Tokens(pair.A) == 0, "native research objective completes without manufacturing or winning early");
            ReplayPair(c, factory, researchLeft, researchRight, restored, limits, 0, "earned-partial to private-qualification replay");
            restored = ProgressionPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var productionLeft = restored.A.RecordReplay(limits); var productionRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, Progression.Build(current.A, 2));
            Both(pair, restored, game => Progression.Process(game).Progress.WholeTicks > 0 &&
                Progression.Process(game).Progress.WholeTicks < Progression.Process(game).Required, "unlocked sequential process earns real partial batch work");
            foreach (var current in new[] { pair, restored }) Submit(c, current, ProgressionOperation(current.A, false));
            Both(pair, restored, ProgressionDisabled, "normal operation shutdown preserves current productive WIP");
            var disabled = Progression.Process(pair.A); Fix spent = Progression.Stock(pair.A);
            c.That(disabled.QueueCount == 2 && disabled.Progress.WholeTicks > 0 && disabled.Progress.WholeTicks < disabled.Required &&
                disabled.Wip.Stock.AmountOf(ProgressionSupply(pair.A)) == Fix.FromInt(3) && Progression.Tokens(pair.A) == 0,
                "first live execution retains its three actual input units, two queued jobs and no fabricated result");
            long execution = disabled.Queue[0].Execution;
            ReplayPair(c, factory, productionLeft, productionRight, restored, limits, 2, "qualified to disabled-productive-process replay");
            restored = ProgressionPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var runningLeft = restored.A.RecordReplay(limits); var runningRight = restored.B.RecordReplay(limits);
            long target = pair.A.Clock.StepIndex + 8;
            Both(pair, restored, game => game.Clock.StepIndex >= target, "disabled saved process observes real clock passage");
            c.That(ProgressionDisabled(restored.A) && Progression.Process(pair.A).Progress.Equals(disabled.Progress) &&
                Progression.Stock(pair.A) == spent && Progression.Process(restored.A).Queue[0].Execution == execution,
                "disabled operation saves and retains actual work/material/execution without catch-up or recharge");
            c.Equal(new Code("process.blocked"), Progression.Assembler(pair.A).State, "entity Update owns authored stopped-state projection");
            foreach (var current in new[] { pair, restored }) Submit(c, current, ProgressionOperation(current.A, true));
            Both(pair, restored, game => Progression.Tokens(game) == 1, "same partial execution earns only its first real output");
            c.That(pair.A.Outcome.IsNone && Progression.Process(pair.A).QueueCount == 1 &&
                Progression.Process(pair.A).Queue[0].Execution != execution, "first output leaves a distinct second execution and incomplete production objective");
            ReplayPair(c, factory, runningLeft, runningRight, restored, limits, 1, "saved-disabled to first-real-output replay");
            restored = ProgressionPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => !game.Outcome.IsNone, "dependent objective finishes after second actual sequential batch");
            c.That(pair.A.Outcome == new Code("process.completed") && Progression.Objectives(pair.A).All(value => value.State == ObjectiveState.Completed),
                "engine objective dependencies alone produce terminal outcome");
            c.That(Progression.Tokens(pair.A) == 2 && Progression.Stock(pair.A) == Fix.FromInt(14) && Progression.Process(pair.A).QueueCount == 0 &&
                Progression.Process(pair.A).Wip.Stock.IsEmpty, "twenty initial materials pay research two and exactly two net-two physical batches, with no stranded WIP");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0,
                "genuine original and restored peer transports stall and reverse actual byte batches");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "terminal native progression restores exactly");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 0, "first-output to terminal-objective replay");
            ReplayPair(c, factory, left, right, pair, limits, 7, "full private mapless progression replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(),
                "all four genuinely advanced native progression worlds converge");
        }
    }
}
