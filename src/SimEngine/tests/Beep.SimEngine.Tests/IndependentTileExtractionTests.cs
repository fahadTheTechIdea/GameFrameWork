using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class IndependentTileExtractionTests
    {
        internal const string Case = "Independent tile extraction: physical work and optional earned resource readings";
        internal const string NativeCase = "Native tile extraction: visibility-enabled prepared debit and optional timed survey";
        internal static void Register()
        {
            TestRunner.Add(Case, c => Run(c, false));
            TestRunner.Add(NativeCase, c => Run(c, true));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "independent-sensor", Seed = 723, Seats = { Controller.Human, Controller.Human } });
        private static EntityView Entity(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == id);
        private static Fix Stock(SimGame game, EntityId target)
        {
            var facts = new List<Facet>(); game.Seat(0).View.Inspect(target, facts);
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static bool Known(SimGame game, int viewer, out KnowledgeObservation observation)
            => game.Seat(viewer).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "sensor.deposit-reading", out observation);
        private static void Update(Check c, SimGame game, SimGame twin)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("extraction fault: " + game.Fault + "/" + twin?.Fault);
            if (twin != null) c.Equal(game.Hash(), twin.Hash(), "saved original layer/endpoint/work/knowledge owners continue identically");
        }
        private static void Execute(Check c, GameFactory factory, SimGame game, ref SimGame twin, EntityId target, string activity, long duration)
        {
            var command = new PostOrderCommand { Target = target, Plan = activity };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary engine preview admits authored " + activity);
            c.Equal(hash, game.Hash(), "preview neither spends material nor advances work");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary physical order admitted: " + result.Refusal);
            int order = checked((int)result.Arg0); twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "pending activity restores through original codecs");
            bool Completed() => game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:" + activity, target, out _);
            var nodes = new List<ActivityNodeView>(); bool partial = false; int updates = 0; long earned = 0;
            while (!Completed() && updates++ < 160)
            {
                Update(c, game, twin);
                if (Completed()) break;
                if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes)) throw new InvalidOperationException("missing actual activity " + activity);
                var node = nodes.Single(); earned = Math.Max(earned, node.Progress.WholeTicks);
                if (node.Progress.WholeTicks > 0 && node.Progress.WholeTicks < node.RequiredTicks && !partial)
                {
                    partial = true; twin = factory.Load(game.Save());
                    c.Equal(game.Hash(), twin.Hash(), "actual partial productive work/resource cell saves");
                    c.Equal(new TileCoord(13, 20), ((TileSpatialSnapshot)Entity(game, "sensor.cart").Spatial).Tile, "machine earns work at authored service bay only");
                    if (activity == "sensor.extraction") {
                        c.Equal(Fix.Zero, Stock(game, target), "unfinished extraction creates no free material");
                        c.That(!Known(game, 0, out _), "extraction has no survey prerequisite or fabricated knowledge");
                    }
                    else c.That(!Known(game, 0, out _), "unfinished survey yields no instant reading");
                    game.Clock.Pause(); twin.Clock.Pause(); long step = game.Clock.StepIndex; ulong paused = game.Hash();
                    Update(c, game, twin); c.Equal(step, game.Clock.StepIndex, "one paused clock advances neither work nor material");
                    c.Equal(paused, game.Hash(), "paused partial checkpoint remains unchanged"); game.Clock.Resume(); twin.Clock.Resume();
                }
            }
            if (!Completed()) throw new InvalidOperationException(activity + " stalled at clock " + game.Clock.StepIndex + ": " +
                string.Join(";", nodes.Select(n => n.State + "/" + n.Blocker + "/" + n.Progress.WholeTicks)));
            c.That(partial && updates < 160, "bounded activity includes an interrupted productive save");
            c.Equal(duration, earned, "exact authored game minutes precede completion and departure");
        }
        private static void Run(Check c, bool native)
        {
            var factory = IndependentTileWorkTests.Factory(extraction: true, nativeTiles: native); var game = New(factory); var fresh = New(factory);
            ulong isolated = fresh.Hash(); EntityId target = Entity(game, "sensor.worksite").Id;
            var origin = (TileSpatialSnapshot)Entity(game, "sensor.cart").Spatial;
            c.That(native ? game.World.Space is TileMap : game.World.Space is SensorTileSpace && !game.World.Services.Has<TileMap>(),
                "JSON selects the actual native/independent geometry provider, not a fallback");
            c.That(game.World.Services.Has<IVisibility>() && !game.World.Services.Has<IEconomy>(), "physical extraction has active visibility and no mandatory business");
            c.Equal(native ? 6 : 2, game.World.Space.StateSchemaVersion, "actual provider owns original resource-cell books in its declared codec");
            c.Equal(Fix.Zero, Stock(game, target), "authored target starts empty");
            c.That(!Known(game, 0, out _), "deposit begins unsurveyed");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = game.RecordReplay(limits);
            SimGame twin = null; Execute(c, factory, game, ref twin, target, "sensor.extraction", 6);
            c.Equal(Fix.FromInt(2), Stock(game, target), "native transaction credits exact two finite materials after earned work");
            c.That(!Known(game, 0, out _), "extraction completes without surveying");
            c.That(origin.Position != ((TileSpatialSnapshot)Entity(game, "sensor.cart").Spatial).Position, "worker physically travels before producing extracted stock");
            Execute(c, factory, game, ref twin, target, "sensor.survey", 3);
            c.That(Known(game, 0, out var observation), "optional timed survey earns original company knowledge");
            c.Equal(8L * Fix.One.Raw, long.Parse(observation.Data.Get("value_raw").Text, System.Globalization.CultureInfo.InvariantCulture),
                "reading samples actual depleted half-unit layer, not copied native storage");
            c.Equal(Fix.FromInt(2), Stock(game, target), "survey neither extracts nor duplicates material");
            c.That(!Known(game, 1, out _), "foreign seat cannot read company-private deposit knowledge");
            if (native) {
                var samples = new List<int>(); var area = new TileRect(14, 20, 1, 1);
                c.Equal(SpatialLayerReadStatus.Success, game.Seat(0).View.Feature<TileMapView>().ReadLayer("sensor.deposit", area, samples), "native owner can query actual depleted private sample with visibility active");
                c.Equal(8, samples.Single(), "native public layer query reads actual conserved stored remainder");
                c.That(game.Seat(1).View.Feature<TileMapView>().ReadLayer("sensor.deposit", area, samples) != SpatialLayerReadStatus.Success && samples.Count == 0,
                    "foreign viewer receives no private resource samples or stale output prefix");
            }
            c.That(!Known(fresh, 0, out _) && Stock(fresh, Entity(fresh, "sensor.worksite").Id) == Fix.Zero, "fresh session has no stock/knowledge leakage");
            c.Equal(isolated, fresh.Hash(), "interleaved session remains untouched");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "earned reading and actual depleted layer restore together");
            c.That(Known(twin, 0, out var restored) && observation.Data.ToString() == restored.Data.ToString(), "restored immutable knowledge retains actual earned sample");
            var verification = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verification.Success && verification.Game?.Hash() == game.Hash(), "ordinary extraction/survey command replay: " + verification.Failure + "/" + verification.Detail);
        }
    }
}
