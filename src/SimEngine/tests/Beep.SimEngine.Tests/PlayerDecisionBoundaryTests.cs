using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.App;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class PlayerDecisionBoundaryTests
    {
        internal const string Case = "Player decision boundary: expired contexts, stable dispatch and terminal command admission";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);

        // Thin player inputs and fault instrumentation, never a gameplay scheduler.
        private sealed class Input : PlayerController
        {
            private readonly Action<PlayerDecisionContext> _action;
            internal Input(Action<PlayerDecisionContext> action) => _action = action;
            public override void Decide(PlayerDecisionContext context) => _action(context);
        }
        private static SetControllerCommand Human() => new SetControllerCommand { Controller = Controller.Human };
        private static void Expired(Check c, PlayerDecisionContext context, string label)
        {
            c.Equal(new PlayerId(0), context.Id, label + " retains immutable seat identity");
            c.Throws<InvalidOperationException>(() => { _ = context.View; }, label + " view access expired");
            c.Throws<InvalidOperationException>(() => { _ = context.Tick; }, label + " clock coordinate expired");
            c.Throws<InvalidOperationException>(() => { _ = context.Time; }, label + " simulation time expired");
            c.Throws<InvalidOperationException>(() => { _ = context.Date; }, label + " calendar expired");
            c.Throws<InvalidOperationException>(() => { _ = context.Answers; }, label + " answer access expired");
            c.Throws<InvalidOperationException>(() => context.TryGetAnswer(1, out _), label + " retained-reply search expired");
            c.Throws<InvalidOperationException>(() => context.Preview(Human()), label + " preview expired");
            c.Throws<InvalidOperationException>(() => context.Submit(Human()), label + " submission expired");
            c.Throws<InvalidOperationException>(() => context.ReportBlocker(new Code("proof.expired")), label + " blocker expired");
            c.Throws<InvalidOperationException>(() => context.ObserveCondition(null), label + " condition access expires before provider");
            c.Throws<InvalidOperationException>(() => context.ObservePlanSites(null), label + " sites access expires before provider");
        }
        private static void Dispatch(Check c)
        {
            var game = ViewObservationProof.New(); var calls = new List<string>();
            var contexts = new List<PlayerDecisionContext>();
            var next = new Input(ctx => calls.Add("next:" + ctx.Tick));
            var foreign = new Input(ctx => calls.Add("foreign:" + ctx.Tick));
            var second = new Input(ctx => calls.Add("second:" + ctx.Tick));
            Input first = null;
            first = new Input(ctx =>
            {
                calls.Add("first:" + ctx.Tick); contexts.Add(ctx);
                c.Equal(Code.None, ctx.Preview(Human()), "open context uses engine preview");
                c.That(!ctx.TryGetAnswer(99999, out _), "no invented command answer");
                game.Seat(0).Detach(first); game.Seat(0).Detach(second);
                game.Seat(0).Attach(next); game.Seat(1).Attach(foreign);
                ctx.Submit(Human());
            });
            game.Seat(0).Attach(first); game.Seat(0).Attach(second);
            c.Throws<InvalidOperationException>(() => game.Seat(0).Attach(first), "duplicate exact controller refuses");
            c.Throws<ArgumentNullException>(() => game.Seat(0).Attach(null), "null controller refuses");
            c.Equal(1, game.RunTicks(1), "mutating attachment callback completes ordinary update");
            c.Equal("first:0|second:0", string.Join("|", calls), "detach and cross-seat attach do not rewrite current dispatch set");
            Expired(c, contexts[0], "successful attached invocation");
            long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Count == 1 && answers[0].Accepted, "captured controller input applied once through real inbox");
            game.RunTicks(1);
            c.Equal("first:0|second:0|next:1|foreign:1", string.Join("|", calls), "attachment edits affect next whole-seat pass");
            var twin = ViewObservationProof.Factory().Load(game.Save());
            game.Seat(0).Detach(next); game.Seat(1).Detach(foreign);
            game.RunTicks(2); twin.RunTicks(2);
            c.Equal(game.Hash(), twin.Hash(), "transient player attachment is not a parallel saved gameplay owner");
        }
        private static void Terminal(Check c, SimGame game, int completed, long start, string phase)
        {
            c.That(game.Fault != null && game.State == GameState.Faulted && game.Fault.Phase == phase,
                "terminal decision reports its actual first phase");
            c.That(game.Tick == start + completed && game.Time.StartTick == start && game.Time.Tick == game.Tick,
                "failure reports only completed sole-clock updates");
            c.Equal(completed, game.Clock.TicksThisFrame, "failure clock uses actual batch count");
            c.Equal(completed, game.Time.TicksRun, "failure event timing uses actual batch count");
            c.That(game.Clock.PendingTicks == 0 && game.Clock.Alpha == Fix.Zero, "failure clears unapplied pacing");
            var before = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), "partial decision cannot save");
            c.Throws<InvalidOperationException>(() => game.Hash(), "partial decision cannot claim a valid hash");
            c.Throws<InvalidOperationException>(() => game.Seat(0).Submit(Human()), "faulted public input refuses before admission");
            c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(Human()), "faulted preview does not call handlers");
            c.Throws<InvalidOperationException>(() => game.World.Submit(new PlayerId(0), Human(), CommandStreams.Player), "native inbox refuses faulted input");
            var stamped = Human(); stamped.Player = new PlayerId(0); stamped.Sequence = 100; stamped.IssuedTick = game.Tick;
            c.Throws<InvalidOperationException>(() => game.World.SubmitStamped(stamped), "faulted stamped admission refuses");
            c.Equal(0, game.RunTicks(5), "fault continuation does not retry callbacks");
            c.That(ReferenceEquals(before, game.Fault), "fault continuation preserves first cause");
        }
        private static void AttachedFailures(Check c)
        {
            foreach (int completed in new[] { 0, 2 })
            {
                var game = ViewObservationProof.New(); var previous = new[] { game.Snapshot, game.Seat(0).Snapshot, game.Seat(1).Snapshot };
                int later = 0, decisions = 0, events = 0, sequence = 0; PlayerDecisionContext failed = null;
                game.Seat(0).Attach(new Input(ctx =>
                {
                    decisions++;
                    if (ctx.Tick != completed) return;
                    failed = ctx; sequence = ctx.Submit(Human());
                    throw new InvalidOperationException("proof.decision.failure");
                }));
                game.Seat(0).Attach(new Input(_ => later++)); game.Seat(1).Attach(new Input(_ => later++));
                game.Updated += _ => { events++; c.Equal(GameState.Faulted, game.Seat(1).Snapshot.State, "Updated sees all private fault projections settled"); };
                c.Equal(completed, game.RunTicks(6), "callback exception settles partial batch without escaping Update");
                c.That(decisions == completed + 1 && later == completed * 2 && events == 1,
                    "failed decision prevents later controller/seat/step execution");
                c.That(game.Fault.Message.Contains("proof.decision.failure") && game.Fault.Tick == completed,
                    "original exception and actual failure coordinate retained");
                Expired(c, failed, "failed attached invocation");
                c.That(game.World.IsPendingCommand(new PlayerId(0), CommandStreams.Player, sequence), "admitted input remains unapplied, never silently removed");
                long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.Equal(0, results.Count, "failed callback does not fabricate acceptance or refusal");
                var after = new[] { game.Snapshot, game.Seat(0).Snapshot, game.Seat(1).Snapshot };
                for (int i = 0; i < after.Length; i++)
                    c.That(after[i].Tick == previous[i].Tick && after[i].Viewer == previous[i].Viewer &&
                        after[i].Entities.Select(e => e.Version).SequenceEqual(previous[i].Entities.Select(e => e.Version)) &&
                        after[i].Changed.Count == 0 && after[i].Spawned.Count == 0 && after[i].Removed.Count == 0,
                        "decision fault retains each viewer's prior safe epoch, not partial or observer facts");
                Terminal(c, game, completed, 0, "player.decide.0");
                c.Equal(completed + 1, decisions, "terminal continuation runs no decision again");
            }
            var network = ViewObservationProof.New();
            network.StartLockstep(new LockstepOptions { LocalSeats = { 0, 1 }, InputDelay = 0, ChecksumEvery = 0 });
            var outgoing = new List<byte[]>(); network.Net.DrainOutgoing(outgoing); outgoing.Clear();
            network.Seat(0).Attach(new Input(ctx => { ctx.Submit(Human()); throw new InvalidOperationException("proof.net.decision"); }));
            c.Equal(0, network.RunTicks(1), "network decision fails before sealing or applying pending input");
            c.Equal(0, network.Net.DrainOutgoing(outgoing), "failed network input emits no sealed frame");
            Terminal(c, network, 0, 0, "player.decide.0");
        }
        private static void SwallowedObservation(Check c)
        {
            var game = ViewObservationProof.New(); var id = game.Seat(0).Snapshot.Entities[0].Id;
            var visibility = (ViewObservationVisibility)game.World.Services.Get<IVisibility>(); int callbacks = 0;
            visibility.OnEntity = (world, viewer, entity) => { callbacks++; throw new InvalidOperationException("proof.first.observation"); };
            game.Seat(0).Attach(new Input(ctx =>
            {
                try { ctx.View.Inspect(id, new List<Facet>()); } catch (InvalidOperationException) { }
                c.Throws<InvalidOperationException>(() => ctx.Submit(Human()), "swallowed latched fault cannot submit through still-open context");
            }));
            c.Equal(0, game.RunTicks(2), "post-decision health gate stops a swallowed provider fault");
            c.That(game.Fault != null && game.Fault.Message.Contains("proof.first.observation") && game.Fault.Phase != "player.decide.0",
                "common invocation preserves observation's earlier cause");
            game.RunTicks(1); c.Equal(1, callbacks, "fault publication and continuation never retry failed observations");
        }
        private static void Authored(Check c)
        {
            foreach (string mode in new[] { "budget", "expired" })
            {
                var factory = PlayerControllerTests.Factory("reply.proof", json => json.Get("reply_controllers")[0].Set("mode", JsonValue.Of(mode)));
                var game = ConstructionPlanTests.NewPoints(factory);
                game.Seat(0).Submit(new SetControllerCommand { Controller = Controller.Autopilot });
                int ran = game.RunTicks(8);
                c.That(ran < 8 && game.Fault != null, "authored " + mode + " failure stops bounded real loop");
                c.That(game.Fault.Message.Contains(mode == "budget" ? "command budget" : "context has expired"),
                    "authored " + mode + " retains exact invocation error");
                Terminal(c, game, ran, 0, "player.decide.0");
            }
            var healthyFactory = PlayerControllerTests.Factory("reply.proof");
            var healthy = ConstructionPlanTests.NewPoints(healthyFactory);
            healthy.Seat(0).Submit(new SetControllerCommand { Controller = Controller.Autopilot });
            healthy.RunTicks(1); var twin = healthyFactory.Load(healthy.Save());
            healthy.RunTicks(7); twin.RunTicks(7);
            c.That(healthy.Fault == null && twin.Fault == null && healthy.Hash() == twin.Hash(),
                "authored successful decision, pending real refusal and saved cadence preserve twin identity");
        }
        private static IEnumerable<Wait> ConstructionScript(Action<PlayerDecisionContext> capture, Action<CommandResult> answer)
        {
            yield return Wait.Ticks(1);
            int sequence = 0;
            yield return Wait.Do(ctx =>
            {
                capture(ctx);
                sequence = ctx.Submit(new ConstructCommand { Def = "tower", Priority = 50,
                    Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
            });
            yield return Wait.Ticks(2);
            yield return Wait.ForAnswer(sequence, answer);
            yield return Wait.Do(capture);
        }
        private static List<WorkJobView> Jobs(SimGame game)
        { var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); return jobs; }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++)
            {
                game.RunTicks(1); twin?.RunTicks(1);
                if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                    throw new InvalidOperationException(stage + "/time=" + game.Tick + "/fault=" + game.Fault + "/twin=" + twin?.Fault);
            }
            if (!done()) throw new InvalidOperationException(stage + "/time=" + game.Tick + "/jobs=" +
                string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
        }
        private static void Physical(Check c)
        {
            var factory = Colony.Factory(); var game = Colony.New(290);
            var contexts = new List<PlayerDecisionContext>(); bool answered = false; int replies = 0;
            var script = new ScriptedController(() => ConstructionScript(ctx => contexts.Add(ctx),
                result => { replies++; answered = result.Accepted; }));
            game.Seat(0).Attach(script);
            using (var recorder = game.RecordReplay(Limits))
            {
                c.Equal(1, game.RunTicks(1), "script initially waits in actual loop");
                c.That(contexts.Count == 0 && !answered, "wait does not submit instant construction");
                Until(game, () => script.Finished, "fresh-context deferred player input");
                c.That(answered && replies == 1 && contexts.Count == 2 && !ReferenceEquals(contexts[0], contexts[1]),
                    "deferred actions get fresh contexts and one actual retained command answer");
                foreach (var context in contexts) Expired(c, context, "deferred action");
                game.Seat(0).Detach(script);
                Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "physical pickup");
                c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "input acknowledgment is not completed construction");
                var twin = factory.Load(game.Save());
                Until(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "physical delivery/build saved continuation", twin);
                Fix steel = Fix.Zero;
                foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0)))
                {
                    var facets = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, facets);
                    steel += facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel").Aggregate(Fix.Zero, (sum, row) => sum + row.Value);
                }
                c.Equal(Fix.FromInt(20), steel, "fifty-minus-thirty native material conservation");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(),
                    "fresh-context player inputs replay real physical gameplay: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current invocation semantics identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "existing native persistence layout unchanged");
            Dispatch(c); AttachedFailures(c); SwallowedObservation(c); Authored(c); Physical(c);
        }
    }
}

