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
    internal static class WorkOrderRetentionObservationTests
    {
        internal const string Case = "Work retention: original target callbacks preserve order entity state and first faults";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory() => ConstructionPlanTests.Points(
            additionalDocuments: new Dictionary<string, string> { ["work-order-retention.json"] = FrameworkProofContent.Read("work-order-retention.json") },
            startupProfiles: new[] { "retention.start" });
        private static Entity Site(SimGame game) => game.World.Entities.All.Single(entity => entity.Alive && entity.Def.Id == "retention.site");
        private static List<OrderView> Orders(SimGame game)
        { var rows = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(rows); return rows; }
        private static int Cancel(Check c, SimGame game)
        {
            var posted = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = Site(game).Id });
            c.That(posted.Accepted, "ordinary file-authored activity admits at actual retained target");
            int id = checked((int)posted.Arg0);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = id }).Accepted, "player cancels actual unearned work");
            c.That(Orders(game).Single(row => row.Id == id).State == OrderState.Cancelled, "real target retention keeps original cancelled order");
            return id;
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var isolated = ConstructionPlanTests.NewPoints(factory);
            c.That(!isolated.Modules.Contains(TycoonModule.ModuleId), "retention proof needs no business");
            var limits = new ReplayLimits(8 * 1024 * 1024, 2000);
            var recorder = isolated.RecordReplay(limits); int goodId = Cancel(c, isolated);
            var twin = factory.Load(isolated.Save());
            c.Equal(isolated.Hash(), twin.Hash(), "original retained order and native target codecs save unchanged");
            for (int step = 0; step < 4; step++)
            {
                c.Equal(1, isolated.Update(), "pure target retention permits original normal cleanup pass");
                c.Equal(1, twin.Update(), "independent restored retention permits normal update");
                c.Equal(isolated.Hash(), twin.Hash(), "pure retained target/order state remains identical");
            }
            c.That(Orders(isolated).Any(row => row.Id == goodId && row.State == OrderState.Cancelled), "pure provider intentionally retains its actual order without replacement");
            c.Equal(2L, recorder.CommandCount, "full ordinary retention replay has exactly two real player inputs");
            c.Equal(isolated.Clock.StepIndex - recorder.InitialStep, (long)recorder.CheckpointCount, "full retention replay records every advancing native step");
            byte[] replay = recorder.Finish();
            var verified = new ReplayVerifier().Verify(factory, replay, limits);
            c.That(verified.Success && verified.ActualHash == isolated.Hash(), "actual full two-input retained-order replay matches native live hash");
            Console.WriteLine("work retention pure replay: initial=" + recorder.InitialStep + ";final=" + isolated.Clock.StepIndex
                + ";commands=" + recorder.CommandCount + ";checkpoints=" + recorder.CheckpointCount + ";live-hash=" + isolated.Hash().ToString("X16"));
            ulong frozen = isolated.Hash();
            foreach (bool saved in new[] { false, true })
            {
                var denied = ConstructionPlanTests.NewPoints(factory); Cancel(c, denied);
                if (saved) denied = factory.Load(denied.Save());
                ulong before = denied.Hash(); int calls = 0;
                Site(denied).Get<ActivityRecord>().ReadFault = (_, _) => { calls++; throw new ApplicationException("proof.private-retention"); };
                var rows = new List<OrderView> { default }; Exception caught = null;
                try { denied.Seat(1).View.Feature<WorkView>().CopyOrders(rows); }
                catch (Exception error) { caught = error; }
                c.That(caught == null && denied.Fault == null && calls == 0 && rows.Count == 0,
                    "foreign ordinary list denies before invoking any private retention callback");
                c.That(denied.Fault == null && denied.Hash() == before, "denied foreign retention query preserves original healthy state and time");
            }
            foreach (bool saved in new[] { false, true })
            foreach (string path in new[] { "direct", "view", "prune" })
            foreach (string fault in new[] { "touch", "private", "order", "pause", "throw", "private-throw", "order-throw", "pause-throw", "reenter", "reenter-catch" })
            {
                var game = ConstructionPlanTests.NewPoints(factory); int id = Cancel(c, game);
                if (saved) game = factory.Load(game.Save());
                var board = game.World.Services.Get<WorkBoard>(); Order original = board.Order(id); var site = Site(game);
                var provider = site.Get<ActivityRecord>(); var permit = site.Get<WorkPermit>();
                long at = game.Clock.StepIndex; int priority = original.Priority, calls = 0; var cached = game.Seat(0).View.Feature<WorkView>();
                provider.ReadFault = (record, order) => {
                    calls++;
                    if (fault == "touch") record.Entity.Touch();
                    if (fault.StartsWith("private", StringComparison.Ordinal)) permit.CorruptReadState();
                    if (fault.StartsWith("order", StringComparison.Ordinal)) order.Priority++;
                    if (fault.StartsWith("pause", StringComparison.Ordinal)) game.Clock.Pause();
                    if (fault == "reenter") board.RetainsOrder(order);
                    if (fault == "reenter-catch")
                    {
                        try { board.RetainsOrder(order); }
                        catch (InvalidOperationException) { } // Deliberately malicious provider swallowing its own nested violation.
                    }
                    if (fault.Contains("throw")) throw new ApplicationException("proof.work-retention");
                };
                Exception caught = null; int completed = -1; var published = new List<OrderView> { default };
                try
                {
                    if (path == "view") cached.CopyOrders(published);
                    else if (path == "prune") completed = game.Update();
                    else board.RetainsOrder(original);
                }
                catch (Exception error) { caught = error; }
                c.That(game.Fault?.Phase == "observation.provider" && (path == "prune" ? completed == 0 : caught != null),
                    path + "/" + saved + "/" + fault + " impure original retention query faults before successful publication/update");
                c.Equal(1, calls, "actual bound callback runs once before fail-stop");
                if (fault.Contains("throw")) c.That(game.Fault?.Message.Contains("proof.work-retention") == true
                    && (path == "prune" || caught?.ToString().Contains("proof.work-retention") == true), "original callback cause is not masked by later verification");
                if (path == "view") c.Equal(0, published.Count, "failed retention read publishes no order prefix");
                if (game.Fault == null) continue; // Preserve real baseline failures; never fabricate terminal behavior.
                var first = game.Fault; provider.ReadFault = null;
                c.That(ReferenceEquals(board.Order(id), original) && original.State == OrderState.Cancelled,
                    "failed retention removes no original cancelled order or replaces its identity");
                c.Equal(fault.StartsWith("order", StringComparison.Ordinal) ? priority + 1 : priority, original.Priority,
                    "terminal diagnosis preserves actual mutation, not a fabricated rollback");
                c.Throws<InvalidOperationException>(() => cached.CopyOrders(published), "clearing hook cannot revive cached public order query");
                c.Equal(0, published.Count, "faulted cached query clears its caller collection");
                c.Throws<InvalidOperationException>(() => game.Hash(), "impure query cannot produce a healthy hash");
                c.Throws<InvalidOperationException>(() => game.Save(), "impure query cannot checkpoint partial state");
                c.Equal(0, game.Update(), "original fail-stop owner prohibits further advancing update");
                c.That(ReferenceEquals(first, game.Fault) && game.Clock.StepIndex == at && calls == 1, "first fault and sole committed game clock stay authoritative");
                c.Equal(frozen, isolated.Hash(), "instance-scoped negative callback cannot corrupt isolated original session");
            }
        }
    }
}
