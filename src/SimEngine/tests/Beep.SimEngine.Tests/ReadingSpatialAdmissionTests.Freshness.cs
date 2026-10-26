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

namespace Beep.SimEngine.Tests
{
    internal static partial class ReadingSpatialAdmissionTests
    {
        internal const string FreshnessCase = "Spatial reading freshness: independent point surveys expire share forget and physically refresh through native policy";
        private const string PointSubject = "reading.point", PointLayerId = "point.terrain";
        private static PointRegion PointSample => new PointRegion(Fix.Zero, Fix.One, 1);
        private static KnowledgeFreshnessSnapshot PointFreshness(SimGame game, int seat = 0)
        {
            Code code = Knowledge(game, seat).ReadFreshness(new PlayerId(seat), Provider, PointSubject, out var value);
            if (!code.IsNone) throw new InvalidOperationException("point freshness refusal " + seat + "/" + code);
            return value;
        }
        private static void PointPermission(Check c, SimGame game, int seat, bool fresh, Fix expected)
        {
            var values = new List<Fix> { Fix.One };
            var view = game.Seat(seat).View.Feature<PointLayerView>();
            c.Equal(fresh ? SpatialLayerReadStatus.Success : SpatialLayerReadStatus.Unavailable,
                view.ReadLayer(PointLayerId, PointSample, values), "native fresh policy governs independent exact point sample");
            if (fresh) c.That(values.Count == 1 && values[0] == expected, "authorized point reads actual current layer value");
            else c.Equal(0, values.Count, "unearned or expired point query publishes no prefix");
        }
        private static void FinitePointReadings(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current independent freshness engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no alternate freshness save format");
            foreach (bool half in new[] { false, true }) FinitePointReading(c, half);
        }
        private static void FinitePointReading(Check c, bool half)
        {
            var factory = PhysicalFactory(false, half, finite: true);
            var game = New(factory, "points", 2); var idle = New(factory, "points", 2); ulong isolated = idle.Hash();
            var target = Entity(game, "source"); var worker = Entity(game, "machine");
            var start = game.Seat(0).Snapshot.Entities.Single(row => row.Id == worker).Spatial;
            var mine = game.Seat(0).View.Feature<PointLayerView>(); var other = game.Seat(1).View.Feature<PointLayerView>();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "finite point survey needs no business module");
            c.That(!Known(game, PointSubject), "finite intent never grants an unearned observation");
            c.Equal(Codes.NotFound, Knowledge(game).ReadFreshness(new PlayerId(0), Provider, PointSubject, out var absent), "unearned finite reading has no lifetime snapshot");
            c.That(absent == null, "unearned finite snapshot is empty");
            PointPermission(c, game, 0, false, Fix.Zero); PointPermission(c, game, 1, false, Fix.Zero);
            c.Equal(Fix.FromInt(20), Stock(game, target, "supply"), "original finite survey materials are authored once");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = game.RecordReplay(limits);
            int first = checked((int)PermanentRemovalTests.Submit(c, game, new PostOrderCommand { Plan = "reading.point-sample", Target = target, Priority = 60 }).Arg0);
            Until(c, game, null, first, () => Nodes(game, first).Any(row => row.Progress.WholeTicks > 0 && row.Progress.WholeTicks < row.RequiredTicks), "finite point partial work");
            c.That(!Known(game, PointSubject) && !start.Matches(game.Seat(0).Snapshot.Entities.Single(row => row.Id == worker).Spatial),
                "independent machine actually travels and works before finite knowledge exists");
            c.Equal(Fix.FromInt(12), Stock(game, target, "supply"), "full eight-material survey cost is consumed once before completion");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "finite survey saves partial real work without another knowledge store");
            Until(c, game, twin, first, () => Known(game, PointSubject), "finite point earned observation");
            var original = PointFreshness(game);
            c.That(original.IsFresh && original.AgeSteps == 1 && original.FreshFor.HasValue, "actual completion uses interval-start chronology, not a fake zero-age record");
            c.Equal(half ? 5L : 3L, original.FreshSteps.Value, "file-authored150seconds rounds through actual minute/half-minute quantum");
            c.Equal(0L, new JsonReader(original.Observation.Data, "original point").Long("value_raw"), "independent survey captures actual initial layer sample");
            PointPermission(c, game, 0, true, Fix.Zero); PointPermission(c, game, 1, false, Fix.Zero);
            c.That(mine.Layers.Any(row => row.Id == PointLayerId) && other.Layers.All(row => row.Id != PointLayerId), "exact point metadata follows earned private freshness");
            var values = new List<Fix>(); var mixed = new PointRegion(Fix.Zero, Fix.One, 2);
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer(PointLayerId, mixed, values), "finite point never authorizes its surrounding interval");
            c.Equal(0, values.Count, "mixed independent point query publishes no partial sample");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(new WritePointLayerCommand { Layer = PointLayerId, Position = Fix.One, Value = Fix.One }),
                "ordinary point write cannot expand survey coverage");

            PermanentRemovalTests.Submit(c, game, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "reading.point-share" });
            var shared = PointFreshness(game, 1);
            c.That(shared.Observation.ObservedTick == original.Observation.ObservedTick && shared.AgeSteps == PointFreshness(game).AgeSteps && shared.AgeSteps == 2,
                "sharing preserves original age and expiry instead of renewing recipient lifetime");
            PointPermission(c, game, 1, true, Fix.Zero);
            c.Equal(Codes.NotFound, Knowledge(game, 1).ReadFreshness(new PlayerId(0), Provider, PointSubject, out absent), "shared own reading never exposes another company's ledger");
            c.That(absent == null, "foreign finite freshness remains empty");
            var retainedPage = other.ReadLayerPage(PointLayerId, PointSample, new SpatialLayerPageRequest(0, 1), values);
            c.Equal(SpatialLayerReadStatus.Success, retainedPage.Status, "recipient captures a real finite sample page");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "shared finite age and exact visibility restore together");
            game.Clock.Pause(); twin.Clock.Pause(); long pausedAt = game.Clock.StepIndex; ulong paused = game.Hash();
            for (int i = 0; i < 3; i++) Step(c, game, twin);
            c.That(game.Clock.StepIndex == pausedAt && game.Hash() == paused && PointFreshness(game, 1).AgeSteps == 2,
                "sole paused clock freezes independent shared expiry");
            game.Clock.Resume(); twin.Clock.Resume();

            PermanentRemovalTests.Submit(c, game, new WritePointLayerCommand { Layer = PointLayerId, Position = Fix.Zero, Value = Fix.FromInt(7) });
            c.Equal(0L, new JsonReader(PointFreshness(game).Observation.Data, "remembered point").Long("value_raw"), "ordinary live point edit does not rewrite remembered sample");
            c.Equal(SpatialLayerReadStatus.Success, game.Observer.Feature<PointLayerView>().ReadLayer(PointLayerId, PointSample, values), "explicit observer sees actual independent point edit");
            c.That(values.Count == 1 && values[0] == Fix.FromInt(7), "accepted ordinary point command really changed the layer");
            twin = factory.Load(game.Save());
            long deadline = checked(original.Observation.ObservedTick + original.FreshSteps.Value);
            for (int i = 0; i < 8 && game.Clock.StepIndex < deadline; i++) Step(c, game, twin);
            c.Equal(deadline, game.Clock.StepIndex, "independent finite visibility expires at exact original-clock deadline");
            c.That(!PointFreshness(game).IsFresh && !PointFreshness(game, 1).IsFresh, "sender and recipient expire together without reset on sharing");
            PointPermission(c, game, 0, false, Fix.Zero); PointPermission(c, game, 1, false, Fix.Zero);
            var refused = other.ReadLayerPage(PointLayerId, PointSample, new SpatialLayerPageRequest(0, 1, retainedPage.Cursor), values);
            c.That(refused.Status == SpatialLayerReadStatus.Unavailable && refused.Cursor == null && refused.TotalCount == 0 && values.Count == 0,
                "expired independent page refuses before retained cursor disclosure");
            c.That(mine.Layers.All(row => row.Id != PointLayerId) && other.Layers.All(row => row.Id != PointLayerId), "expired independent layer metadata disappears for both companies");
            var restored = factory.Load(game.Save()); PointPermission(c, restored, 0, false, Fix.Zero); PointPermission(c, restored, 1, false, Fix.Zero);
            c.Equal(game.Hash(), restored.Hash(), "expired point checkpoint does not resurrect either company's scope");

            PermanentRemovalTests.Submit(c, game, new ForgetKnowledgeCommand { Policy = "reading.point-forget" });
            c.That(!Known(game, PointSubject) && !PointFreshness(game, 1).IsFresh, "forgetting sender leaves recipient's original stale history intact");
            int refresh = checked((int)PermanentRemovalTests.Submit(c, game, new PostOrderCommand { Plan = "reading.point-refresh", Target = target, Priority = 60 }).Arg0);
            Until(c, game, null, refresh, () => Nodes(game, refresh).Any(row => row.Progress.WholeTicks > 0), "finite point physical refresh partial");
            c.That(!Known(game, PointSubject), "partial refresh cannot restore forgotten history or visibility");
            PointPermission(c, game, 0, false, Fix.Zero);
            twin = factory.Load(game.Save()); Until(c, game, twin, refresh, () => Known(game, PointSubject), "finite point refreshed after actual work");
            var latest = PointFreshness(game);
            c.That(latest.IsFresh && latest.Observation.ObservedTick > original.Observation.ObservedTick,
                "only completed six-minute authored refresh creates a new finite lifetime");
            c.Equal(Fix.FromInt(7).Raw, new JsonReader(latest.Observation.Data, "new point").Long("value_raw"), "physical refresh captures current independent value");
            c.That(!PointFreshness(game, 1).IsFresh && PointFreshness(game, 1).Observation.ObservedTick == original.Observation.ObservedTick,
                "sender refresh never remotely renews recipient's stale sample");
            PointPermission(c, game, 0, true, Fix.FromInt(7)); PointPermission(c, game, 1, false, Fix.Zero);
            PermanentRemovalTests.Submit(c, game, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "reading.point-share" });
            var reshared = PointFreshness(game, 1);
            c.That(reshared.IsFresh && reshared.Observation.ObservedTick == latest.Observation.ObservedTick
                && reshared.Observation.LearnedTick == shared.Observation.LearnedTick, "explicit re-share copies new observation while retaining recipient's first learning time");
            PointPermission(c, game, 1, true, Fix.FromInt(7));
            int cancelled = checked((int)PermanentRemovalTests.Submit(c, game, new PostOrderCommand { Plan = "reading.point-refresh", Target = target, Priority = 60 }).Arg0);
            Until(c, game, null, cancelled, () => Nodes(game, cancelled).Any(row => row.Progress.WholeTicks > 0), "cancelled finite point refresh partial");
            PermanentRemovalTests.Submit(c, game, new CancelOrderCommand { Order = cancelled });
            c.Equal(latest.Observation.ObservedTick, PointFreshness(game).Observation.ObservedTick, "cancelled actual work cannot renew survey lifetime");
            c.Equal(Fix.FromInt(12), Stock(game, target, "supply"), "explicit resource-free refresh never repeats the original eight-material cost");
            c.That(original.IsFresh && original.AgeSteps == 1 && shared.IsFresh && shared.AgeSteps == 2,
                "retained sender and recipient freshness facts remain immutable historical snapshots");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled finite point work and original ledger restore exactly");
            c.Equal(isolated, idle.Hash(), "independent session borrows no point knowledge, values, lifetime or clock");
            c.Equal(8, recording.CommandCount, "recording includes all physical sampling sharing editing forgetting refresh and cancellation inputs");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "finite point recording spans every actual advancing update");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "finite independent point workflow replays to final live state, not a prefix");
            Console.WriteLine("finite point replay: half=" + half + ";initial=" + recording.InitialStep + ";final=" + game.Clock.StepIndex
                + ";commands=" + recording.CommandCount + ";checkpoints=" + recording.CheckpointCount
                + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + verified.ActualHash.ToString("X16"));
        }
    }
}
