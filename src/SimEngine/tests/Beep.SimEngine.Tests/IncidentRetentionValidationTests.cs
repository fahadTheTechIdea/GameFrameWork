using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using EnvironmentProof = Beep.SimEngine.Tests.EnvironmentalResponseAcceptanceTests;
using Response = Beep.SimEngine.Tests.EmergencyResponseAcceptanceTests;
using Logistics = Beep.SimEngine.Tests.LogisticsPolicyContentTests;

namespace Beep.SimEngine.Tests
{
    internal static class IncidentRetentionValidationTests
    {
        internal const string Case = "Incident retention: authoritative lifecycle validation protects resolved work and timestamps";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Source = new Code("failure");
        private static void Run(Check c)
        {
            var factory = EnvironmentProof.Factory(false, true); var game = EnvironmentProof.New(factory);
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && game.Modules.Contains(ExternalResponseModule.ModuleId),
                "actual independent incident is explicitly selected without business");
            var limits = new ReplayLimits(8 * 1024 * 1024, 2000); var recorder = game.RecordReplay(limits);
            var raised = TestKit.Order(game, 0, new RaiseIncidentCommand { Target = Logistics.Id(game, "response.station"),
                Source = Source, Kind = new Code("response.failure") });
            c.That(raised.Accepted && Response.Active(game), "ordinary incident input creates actual independent episode and work");
            var active = Response.Lifecycle(game); byte[] activeSave = game.Save();
            var twin = factory.Load(activeSave); c.Equal(game.Hash(), twin.Hash(), "active episode and original response order restore exactly");
            long prefixAt = game.Clock.StepIndex; var prefixTimer = Stopwatch.StartNew();
            void Reach(Func<bool> ready, string stage)
            {
                for (int step = 0; step < 512 && game.Clock.StepIndex - prefixAt < 512
                    && prefixTimer.Elapsed.TotalSeconds < 25 && !ready(); step++)
                {
                    if (game.Update() != 1 || twin.Update() != 1 || game.Fault != null || twin.Fault != null)
                        throw new InvalidOperationException("actual incident recovery fault/" + game.Fault + "/" + twin.Fault);
                }
                if (!ready()) throw new InvalidOperationException(stage + " exceeded512updates/" + game.Clock.StepIndex + "/"
                    + string.Join(";", Response.Phases(game).SelectMany(phase => phase.Nodes).Select(node => node.Id + ":" + node.State + ":" + node.Blocker)));
            }
            foreach (var current in new[] { game, twin })
                c.That(TestKit.Order(current, 0, Response.Dispatch(current, "recovery.carrier", "response.spare", 2, 100)).Accepted,
                    "player orders actual finite spare delivery through normal physical logistics");
            foreach (var current in new[] { game, twin })
                c.That(TestKit.Order(current, 0, Response.Dispatch(current, "recovery.service", "response.suppressant", 2, 90)).Accepted,
                    "player orders actual finite suppressant delivery without an instant supply fallback");
            Reach(() => Response.Complete(game, "response") && Response.Complete(game, "cleanup")
                && !Logistics.Jobs(game).Any(job => job.IsHaul && job.Worker.IsValid), "physical repair and cleanup");
            foreach (var current in new[] { game, twin })
                c.That(TestKit.Order(current, 0, Response.Dispatch(current, "recovery.service", "response.kit", 1, 100)).Accepted,
                    "player supplies original restart kit after the actual carrier is free");
            Reach(() => !Response.Active(game), "physical verified restart");
            var completed = Response.Lifecycle(game); byte[] resolvedSave = game.Save();
            c.That(completed.OrderId == active.OrderId && completed.OrderState == OrderState.Done && !completed.Snapshot.Active,
                "real physical response completes its original bound order and retains history");
            c.Equal(game.Hash(), twin.Hash(), "original and saved physical completion converge");
            c.Equal(completed.Snapshot.ResolvedTick, game.World.Services.Get<WorkBoard>().Order(completed.OrderId).CompletedTick,
                "actual resolution and completed work share one authoritative game-clock moment");
            c.Equal(4L, recorder.CommandCount, "physical lifecycle proof records only the incident and three real player haul inputs");
            c.Equal(game.Clock.StepIndex - recorder.InitialStep, (long)recorder.CheckpointCount, "full replay includes every physical update");
            var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "actual independent physical episode replay reaches original live state");
            Console.WriteLine("incident retention physical replay: initial=" + recorder.InitialStep + ";final=" + game.Clock.StepIndex
                + ";commands=" + recorder.CommandCount + ";checkpoints=" + recorder.CheckpointCount + ";live-hash=" + game.Hash().ToString("X16"));
            ulong frozen = game.Hash();
            foreach (string stage in new[] { "active", "resolved" })
            foreach (string path in new[] { "direct", "prune" })
            {
                if (stage == "active" && path == "prune") continue; // Cleanup only decides retention of terminal orders.
                foreach (string fault in new[] { "empty", "kind", "episode", "active-flag", "future", "raised-drift", "resolved-drift" })
                {
                    if (stage == "active" && fault == "resolved-drift") continue;
                    var bad = factory.Load(stage == "active" ? activeSave : resolvedSave);
                    var board = bad.World.Services.Get<WorkBoard>(); var order = board.Order(active.OrderId);
                    var target = bad.World.Entities.Get(Logistics.Id(bad, "response.station")); var provider = target.Get<ExternalIncident>(Source);
                    int calls = 0; long at = bad.Clock.StepIndex; long completedAt = order.CompletedTick;
                    provider.LifecycleFault = (_, actual) => {
                        calls++;
                        if (fault == "empty") return default;
                        if (fault == "kind") return new IncidentLifecycleSnapshot(new Code("proof.wrong-kind"), actual.Episode, actual.RaisedTick, actual.ResolvedTick);
                        if (fault == "episode") return new IncidentLifecycleSnapshot(actual.Kind, actual.Episode + 1, actual.RaisedTick, actual.ResolvedTick);
                        if (fault == "active-flag") return new IncidentLifecycleSnapshot(actual.Kind, actual.Episode, actual.RaisedTick, actual.Active ? actual.RaisedTick : -1);
                        if (fault == "future") return new IncidentLifecycleSnapshot(actual.Kind, actual.Episode, at + 5, actual.Active ? -1 : at + 6);
                        if (fault == "raised-drift") return new IncidentLifecycleSnapshot(actual.Kind, actual.Episode, actual.RaisedTick + 1, actual.ResolvedTick);
                        if (fault == "resolved-drift") return new IncidentLifecycleSnapshot(actual.Kind, actual.Episode, actual.RaisedTick, actual.ResolvedTick - 1);
                        throw new InvalidOperationException("explicit selected lifecycle fault required");
                    };
                    Exception caught = null; int advanced = -1;
                    try { if (path == "direct") board.RetainsOrder(order); else advanced = bad.Update(); }
                    catch (Exception error) { caught = error; }
                    c.That(bad.Fault?.Phase == "observation.provider" && (path == "direct" ? caught != null : advanced == 0),
                        stage + "/" + path + "/" + fault + " inconsistent lifecycle cannot authorize cleanup or a successful query");
                    c.Equal(1, calls, "actual selected incident report is read once at the failing boundary");
                    if (bad.Fault == null) continue; // Retain real failing baseline; no fabricated fail-stop state.
                    var first = bad.Fault; provider.LifecycleFault = null;
                    c.That(ReferenceEquals(order, board.Order(order.Id)) && order.CompletedTick == completedAt,
                        "refused incident history removes or rewrites no actual original work");
                    c.Throws<InvalidOperationException>(() => bad.Seat(0).View.Feature<WorkView>().CopyOrders(new List<OrderView>()), "invalid lifecycle cannot revive cached work facts");
                    c.Throws<InvalidOperationException>(() => bad.Save(), "invalid incident report cannot checkpoint a pruned history");
                    c.Throws<InvalidOperationException>(() => bad.Hash(), "invalid incident report cannot publish healthy state hash");
                    c.Equal(0, bad.Update(), "first fault blocks later game-clock advancement");
                    c.That(ReferenceEquals(first, bad.Fault) && bad.Clock.StepIndex == at && calls == 1,
                        "removing the malformed report cannot erase its first fault or rerun its callback");
                    c.Equal(frozen, game.Hash(), "malformed loaded provider report leaves independent actual completed session identical");
                }
            }
        }
    }
}
