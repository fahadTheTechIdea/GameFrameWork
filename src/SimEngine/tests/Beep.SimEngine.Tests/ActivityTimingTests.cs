using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ActivityExecutorContractTests;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // JSON authoring, normal player intent and immutable facts; no alternate duration/execution owner.
    internal static class ActivityTimingTests
    {
        internal const string Case = "Activity timing: session converter owns graph work and cumulative physical inputs";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(out ActivityExecutorContractsModule proof, bool independent, int seconds, string duration)
            => Create(out proof, independent, edit: data => {
                var nodes = data.Get("activities")[0].Get("nodes");
                for (int i = 0; i < nodes.Count; i++) nodes[i].Set("duration", Json.Parse(duration));
                data.Set("target_resource_requirements", Json.Parse("[{\"id\":\"clock.inputs\",\"port\":null,\"use\":\"continuous\",\"quantities\":{\"steel\":8}}]"));
                nodes[1].Set("requirement", JsonValue.Of("clock.inputs"));
            }, worldEdit: world => world.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number(seconds.ToString())));
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current exact protocol");
            foreach (Type type in new[] { typeof(IActivityNodeDefinition), typeof(ActivityNodeDef) })
                c.That(type.GetMethod("RequiredTicks") == null, "no overridable node conversion or compatibility helper " + type.Name);
            foreach (bool independent in new[] { false, true })
            {
                Physical(c, independent, 60, 2); Physical(c, independent, 10, 9);
                Range(c, independent);
            }
        }
        private static void Physical(Check c, bool independent, int seconds, long required)
        {
            var factory = Factory(out var proof, independent, seconds, "{\"numerator\":90,\"denominator\":1,\"unit\":\"second\"}");
            var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            c.That(proof.ActualAliases, "actual native/direct strategies use same clock");
            var limits = new ReplayLimits(4 * 1024 * 1024, 5000); var recorder = game.RecordReplay(limits);
            int id = Begin(c, game); var rows = Nodes(game, id);
            c.That(rows.All(n => n.RequiredTicks == required), "all node diagnostics use selected quantum " + seconds);
            c.That(Work(game).TryReadProgress(id, out var analysis), "actual graph diagnostics");
            c.Equal((BigInteger)(3 * required) << Fix.FractionalBits, analysis.Work.TotalRequiredRaw, "same converter totals all graph nodes");
            c.That(!Work(game, 1).TryReadProgress(id, out _), "foreign company receives no work diagnostics");
            Reach(game, null, () => Nodes(game, id).Single(n => n.Id == "operate").Progress.WholeTicks == 1, "partial actual ninety-second work");
            var retained = Nodes(game, id).Single(n => n.Id == "operate"); var before = retained.Progress;
            c.That(retained.Worker.IsValid && !retained.Progress.Complete(required), "arrival then positive physical work, never instant");
            c.Equal(Fix.FromInt(seconds == 60 ? 16 : 19), Steel(game), "cumulative running input uses same converted goal");
            c.That(retained.RunningInputs.Count == 1, "running resource diagnostics derive from original progress");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial running input and node goal restore");
            ulong hash = game.Hash(); long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(hash == game.Hash() && at == game.Clock.StepIndex, "sole paused clock freezes work, inputs and graph");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, twin, () => Nodes(game, id).Single(n => n.Id == "operate").State == NodeState.Done, "actual physical predecessor completion");
            var complete = Nodes(game, id).Single(n => n.Id == "operate");
            c.Equal(new WorkProgress(required), complete.Progress, "native work clamps to session-converted goal");
            c.Equal(required - before.WholeTicks, game.Clock.StepIndex - at, "actual remaining productive updates match detached RequiredTicks");
            c.Equal(Fix.FromInt(12), Steel(game), "running inputs consume exactly eight units across either quantum");
            c.Equal(before, retained.Progress, "retained immutable facts stay unchanged");
            Reach(game, twin, () => Nodes(game, id).Single(n => n.Id == "finish").Progress.WholeTicks == 1, "fixed successor after parallel join");
            rows = Nodes(game, id); var finish = rows.Single(n => n.Id == "finish");
            c.That(rows.Where(n => n.Id != "finish").All(n => n.Progress.Equals(new WorkProgress(required))), "timed and staffed nodes share sole conversion");
            c.That(Work(game).TryReadProgress(id, out analysis), "actual remaining fixed-work diagnostics");
            c.Equal((BigInteger)(required - 1) << Fix.FractionalBits, analysis.Work.RemainingRaw, "graph analysis agrees with pending fixed-work goal");
            at = game.Clock.StepIndex;
            Reach(game, twin, () => Plans(game).Single().State == PlanState.Done, "ordinary resolved plan");
            c.Equal(required - finish.Progress.WholeTicks, game.Clock.StepIndex - at, "fixed work completion shares view/graph session goal");
            c.That(!Work(game).TryReadProgress(id, out _), "resolved order is removed; no fabricated active graph history");
            c.Equal(Fix.FromInt(12), Steel(game), "completion neither double-spends nor refunds running input");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original payload restores");
            c.Equal(isolatedHash, isolated.Hash(), "session converter/chronology isolation");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary work/pause/physical input replay " + seconds);
        }
        private static void Range(Check c, bool independent)
        {
            var factory = Factory(out _, independent, 60, "{\"numerator\":40000000,\"denominator\":1,\"unit\":\"hour\"}");
            var game = New(factory); int id = Begin(c, game);
            c.That(Nodes(game, id).All(n => n.RequiredTicks == 2400000000L), "wide node goals never narrow to int");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "wide goal save validates through sole converter");
            factory = Factory(out _, independent, 60, "{\"numerator\":1,\"denominator\":100,\"unit\":\"second\"}");
            game = New(factory); id = Begin(c, game);
            c.That(Nodes(game, id).All(n => n.RequiredTicks == 1), "positive subquantum work uses one shared ceiling");
            ActivityExecutorContractsModule invalid = null;
            c.Throws<ContentException>(() => New(Factory(out invalid, independent, 60,
                "{\"numerator\":9223372036854775807,\"denominator\":1,\"unit\":\"hour\"}")), "unrepresentable authored goal refuses before world starts");
            c.Equal(0, invalid.Initialize.Initializations, "duration range refusal before Initialize");
        }
    }
}
