using System;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.RouteOperatingAreaTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class ItineraryAdmissionTests
    {
        private static GameFactory ControlledPoints()
        {
            var root = Content.Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("entities")[2].Get("components").Set("motion_execution", Content.Json.Parse(FrameworkProofContent.Read("motion-execution.json")));
            var text = new StringBuilder(); root.WriteCanonical(text); return PointGame.Factory(text.ToString());
        }
        private static MotionRouteCheck DurationRoute(SimGame game, Entity actor, SpatialSnapshot origin, SpatialSnapshot target,
            MotionDestinationKind kind = MotionDestinationKind.Exact)
        {
            var code = game.Seat(0).View.Feature<MotionView>().ReadRoute(actor.Id, origin, new MotionDestination(target, kind), out var route);
            if (!code.IsNone) throw new InvalidOperationException("prospective travel refused: " + code); return route;
        }
        private static void TravelControl(Check c, SimGame game, bool enabled, int rate)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Find(game, "machine").Id,
                Operation = new Code("travel"), Enabled = enabled, Target = Fix.FromInt(rate) }).Accepted,
                "ordinary engine command controls future and actual physical speed");
        private static void DurationAdmission(Check c)
        {
            var factory = Tiles(); var game = New(factory); var actor = Setup(c, game);
            var origin = actor.Get<ISpatialPresence>().Capture(); var target = At("colony.tiles", new TileCoord(18, 26));
            ulong hash = game.Hash(); long time = game.Clock.StepIndex; var mover = actor.Get<Mover>();
            var route = DurationRoute(game, actor, origin, target);
            c.That(route.Blocker.IsNone && route.Travel.HasValue && route.Travel.Value.RemainingTicks == 6 && route.Travel.Value.Blocker.IsNone,
                "native no-area prospective route uses authored six-minute physical speed, not a guessed travel cost");
            c.That(game.Hash() == hash && game.Clock.StepIndex == time && mover.Path.Count == 0 && mover.Status == MoveStatus.Idle,
                "prospective path scratch changes no actual route, clock, stock or job");
            var future = DurationRoute(game, actor, At("colony.tiles", new TileCoord(18, 23)), target);
            c.Equal(3L, future.Travel.Value.RemainingTicks.Value, "native future leg starts at supplied prospective arrival, never current presence");
            var local = DurationRoute(game, actor, origin, origin);
            c.Equal(0L, local.Travel.Value.RemainingTicks.Value, "already reached prospective geometry has no movement exposure");
            c.Equal(MotionAreaCodes.Outside, DurationRoute(game, actor, origin, Find(game, "facility.source").Get<ISpatialPresence>().Capture()).Blocker,
                "absence of area restriction does not bypass actual static obstacles");
            c.That(DurationRoute(game, actor, origin, Find(game, "facility.source").Get<ISpatialPresence>().Capture(), MotionDestinationKind.Interaction).Travel.HasValue,
                "interaction duration uses actual expanded arrival geometry, not an occupied building center");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadRoute(actor.Id, origin, new MotionDestination(target, MotionDestinationKind.Exact), out var hidden),
                "foreign company receives no private route/rate facts");
            c.That(hidden == null, "foreign route has no partial duration");
            c.Equal(Codes.InvalidArgument, game.Seat(0).View.Feature<MotionView>().ReadRoute(actor.Id, new PointSnapshot("foreign", Fix.Zero, Fix.Zero),
                new MotionDestination(target, MotionDestinationKind.Exact), out _), "foreign geometry is explicit invalid intent");
            c.Equal(hash, game.Hash(), "all native prospective probes retain original actual state");

            var points = NewPoint(ControlledPoints()); var machine = Find(points, "machine");
            var start = machine.Get<ISpatialPresence>().Capture(); var finish = Find(points, "destination").Get<ISpatialPresence>().Capture();
            c.Equal(10L, DurationRoute(points, machine, start, finish).Travel.Value.RemainingTicks.Value, "public-only owner supplies its own physical duration");
            TravelControl(c, points, true, 2);
            c.Equal(5L, DurationRoute(points, machine, start, finish).Travel.Value.RemainingTicks.Value, "future route uses actual controlled multiplier");
            c.Equal(3L, DurationRoute(points, machine, new PointSnapshot("field.points", Fix.FromInt(4), Fix.FromInt(4)), finish).Travel.Value.RemainingTicks.Value,
                "public future origin participates in the same provider budget");
            TravelControl(c, points, false, 2); var disabled = DurationRoute(points, machine, start, finish);
            c.That(disabled.Blocker.IsNone && disabled.Arrival != null && disabled.Travel.Value.RemainingTicks == null && disabled.Travel.Value.Blocker == OperationCodes.Disabled,
                "disabled operation retains structural proof but never fabricates payable movement duration");
            c.Equal(0L, DurationRoute(points, machine, start, start).Travel.Value.RemainingTicks.Value,
                "already reached geometry needs no movement operation even while travel is disabled");
            TravelControl(c, points, true, 0); var zero = DurationRoute(points, machine, start, finish);
            c.That(zero.Travel.Value.RemainingTicks == null && zero.Travel.Value.Blocker == MotionExecutionCodes.ZeroRate,
                "zero actual controlled rate has a typed unknown, not zero travel time");
            foreach (string fault in new[] { "route-duration-negative", "route-duration-empty", "route-duration-both" })
            {
                var broken = NewPoint(Points(fault)); var brokenActor = Find(broken, "machine");
                c.Throws<InvalidOperationException>(() => DurationRoute(broken, brokenActor, brokenActor.Get<ISpatialPresence>().Capture(), Find(broken, "destination").Get<ISpatialPresence>().Capture()),
                    "contradictory prospective duration refuses engine admission: " + fault);
            }
            var unsupported = NewPoint(Points("route-duration-unsupported")); var unsupportedActor = Find(unsupported, "machine");
            var unknown = DurationRoute(unsupported, unsupportedActor, unsupportedActor.Get<ISpatialPresence>().Capture(), Find(unsupported, "destination").Get<ISpatialPresence>().Capture());
            c.That(unknown.Blocker.IsNone && unknown.Travel.Value.RemainingTicks == null && unknown.Travel.Value.Blocker == MotionArrivalCodes.Unsupported,
                "structural-only mod explicitly reports duration unsupported, never another estimator fallback");
        }
        private static void DurationPhysical(Check c)
        {
            var factory = Tiles(); var game = New(factory); var actor = Setup(c, game);
            var route = DurationRoute(game, actor, actor.Get<ISpatialPresence>().Capture(), At("colony.tiles", new TileCoord(18, 26)));
            long start = game.Clock.StepIndex;
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = actor.Id, To = new TileCoord(18, 26) }).Accepted,
                "player starts actual native route after pure future-duration query");
            var twin = factory.Load(game.Save());
            Reach(game, () => actor.Get<IMotion>().Status == MoveStatus.Arrived, "prospective native physical duration", twin: twin);
            c.Equal(route.Travel.Value.RemainingTicks.Value, game.Clock.StepIndex - start, "native plain-route exposure equals its provider-owned prospective duration");
            c.Equal(game.Hash(), twin.Hash(), "native future duration adds no saved speculative route or clock");

            var pointFactory = ControlledPoints(); var points = NewPoint(pointFactory); var machine = Find(points, "machine");
            TravelControl(c, points, true, 2);
            var finish = Find(points, "destination").Get<ISpatialPresence>().Capture();
            var budget = DurationRoute(points, machine, machine.Get<ISpatialPresence>().Capture(), finish); start = points.Clock.StepIndex;
            c.That(TestKit.Order(points, 0, new SetHomeCommand { Worker = machine.Id, Home = Find(points, "destination").Id }).Accepted,
                "ordinary home command starts actual controlled public motion");
            var retained = DurationRoute(points, machine, new PointSnapshot("field.points", Fix.Zero, Fix.Zero), finish);
            c.Equal(budget.Travel.Value.RemainingTicks.Value, retained.Travel.Value.RemainingTicks.Value, "prospective origin ignores already advanced actual route without changing it");
            ulong hash = points.Hash(); var saved = pointFactory.Load(points.Save()); long pausedTime = points.Clock.StepIndex;
            points.Clock.Pause(); saved.Clock.Pause(); points.Update(); saved.Update();
            c.That(points.Clock.StepIndex == pausedTime && points.Hash() == hash, "one-clock pause freezes actual route; future geometry owns no time");
            points.Clock.Resume(); saved.Clock.Resume();
            Reach(points, () => machine.Get<IMotion>().Status == MoveStatus.Arrived, "prospective public physical duration", twin: saved);
            c.Equal(budget.Travel.Value.RemainingTicks.Value, points.Clock.StepIndex - start, "controlled public physical movement matches the prospective provider duration");
            c.Equal(points.Hash(), saved.Hash(), "public provider travel and current control continue identically after save");
            c.Equal(5L, retained.Travel.Value.RemainingTicks.Value, "retained prospective duration does not become a live progress owner");
        }
    }
}
