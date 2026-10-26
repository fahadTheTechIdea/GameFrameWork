using System;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class DockTravelSelectionTests
    {
        private const string RankedDepartureCase = "Dock departure ranking: guarded cost order precedes route evaluation and preserves original refusals";
        private const string RankedServiceCase = "Dock candidate ranking: cheaper feasible bays precede expensive identities without changing refusal precedence";

        private static void RankedServices(Check c)
        {
            foreach (string mode in new[] { "far", "blocked", "all-blocked", "pending", "order", "cost" })
                foreach (bool shuffled in new[] { false, true })
                {
                    var factory = Factory(false, "near", shuffled, serviceCase: "ranked-" + mode);
                    var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
                    ulong before = game.Hash(); var twin = factory.Load(game.Save());
                    foreach (var session in new[] { game, twin })
                    {
                        var world = session.World;
                        var destination = world.Entities.Get(Entity(session, "destination"));
                        var carrier = world.Entities.Get(Entity(session, "machine"));
                        session.Seat(0).View.TryFindResource("supply", out var resource);
                        var origin = new PointSnapshot(world.Space.Id, Fix.FromInt(10), Fix.FromInt(10));
                        if (mode == "cost")
                        {
                            c.Throws<InvalidOperationException>(() => world.Services.Get<WorkBoard>().PreviewDock(destination, carrier,
                                WorkDockRequest.Haul(resource, ResourceHandlingPhase.Unloading), origin,
                                out _, out _, out _, out _, productiveOnly: true), "expensive losing bay cost still uses the original guard");
                            c.That(session.Fault != null, "bay cost mutation latches the first-fault owner");
                            c.Throws<InvalidOperationException>(() => session.Save(), "cost mutation exposes no healthy checkpoint");
                            continue;
                        }
                        long step = session.Clock.StepIndex;
                        Code result = world.Services.Get<WorkBoard>().PreviewDock(destination, carrier,
                            WorkDockRequest.Haul(resource, ResourceHandlingPhase.Unloading), origin,
                            out Code selected, out long cost, out var arrival, out var travel, productiveOnly: true);
                        bool refused = mode == "all-blocked" || mode == "pending" || mode == "order";
                        c.Equal(mode == "pending" ? MotionAreaCodes.Pending : refused ? MotionAreaCodes.Outside : Code.None,
                            result, "total refusal keeps Pending, otherwise the FIRST original-ID blocker: " + mode);
                        c.Equal(refused ? Code.None : new Code(mode == "blocked" ? "a.expensive-bay" : "z.cheap-bay"),
                            selected, "cheapest feasible bay, or actual necessary fallback: " + mode);
                        c.Equal(refused ? 0L : Fix.FromInt(mode == "blocked" ? 4 : 1).Raw, cost, "no speculative cost/result prefix on refusal");
                        c.That(refused ? arrival == null : arrival is PointSnapshot at && at.Position == Fix.FromInt(13),
                            "complete successful bay-to-exit geometry is preserved: " + mode);
                        c.That(refused ? !travel.RemainingTicks.HasValue : travel.RemainingTicks.HasValue && travel.Blocker.IsNone,
                            "only a complete actual itinerary publishes travel: " + mode);
                        c.Equal(before, session.Hash(), "ranked bay selection changes no authoritative state");
                        c.Equal(step, session.Clock.StepIndex, "ranked bay selection uses but never advances the one clock");
                        c.That(session.Fault == null, "unselected expensive route is not invoked, necessary callbacks remain healthy");
                    }
                    if (mode != "cost") c.Equal(game.Hash(), twin.Hash(), "restored/shuffled sessions select identical physical intent");
                    c.Equal(isolated, idle.Hash(), "independent idle session remains isolated");
                }
        }

        private static void RankedDepartures(Check c)
        {
            foreach (string mode in new[] { "far", "blocked", "all-blocked", "pending", "cost" })
                foreach (bool shuffled in new[] { false, true })
                {
                    var factory = Factory(false, "near", shuffled, routeCase: "ranked-" + mode);
                    var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
                    ulong before = game.Hash(); var twin = factory.Load(game.Save());
                    foreach (var session in new[] { game, twin })
                    {
                        var world = session.World;
                        var destination = world.Entities.Get(Entity(session, "destination"));
                        var carrier = world.Entities.Get(Entity(session, "machine"));
                        session.Seat(0).View.TryFindResource("supply", out var resource);
                        var origin = new PointSnapshot(world.Space.Id, Fix.FromInt(10), Fix.FromInt(10));
                        if (mode == "cost")
                        {
                            c.Throws<InvalidOperationException>(() => world.Services.Get<WorkBoard>().PreviewDock(destination, carrier,
                                WorkDockRequest.Haul(resource, ResourceHandlingPhase.Unloading), origin,
                                out _, out _, out _, out _, productiveOnly: true), "even expensive losing exit cost is guarded");
                            c.That(session.Fault != null, "exit cost mutation latches original first-fault owner");
                            c.Throws<InvalidOperationException>(() => session.Save(), "no healthy save after exit cost mutation");
                            continue;
                        }
                        long step = session.Clock.StepIndex;
                        Code result = world.Services.Get<WorkBoard>().PreviewDock(destination, carrier,
                            WorkDockRequest.Haul(resource, ResourceHandlingPhase.Unloading), origin,
                            out Code selected, out _, out var arrival, out var travel, productiveOnly: true);
                        bool refused = mode == "all-blocked" || mode == "pending";
                        Code expected = mode == "pending" ? MotionAreaCodes.Pending : refused ? MotionAreaCodes.Unsupported : Code.None;
                        c.Equal(expected, result, "Pending or last ORIGINAL-ID refusal survives rank reversal: " + mode);
                        c.Equal(refused ? Code.None : new Code("z.near-bay"), selected, "ranked journey never replaces service choice: " + mode);
                        c.That(refused ? arrival == null : arrival is PointSnapshot at && at.Position == Fix.FromInt(mode == "blocked" ? 17 : 13),
                            "cheapest feasible exit or necessary fallback retains actual geometry: " + mode);
                        c.That(refused ? !travel.RemainingTicks.HasValue : travel.RemainingTicks.HasValue && travel.Blocker.IsNone,
                            "only successful complete journeys expose travel: " + mode);
                        c.Equal(before, session.Hash(), "ranking never changes claims, stock, geometry or native state: " + mode);
                        c.Equal(step, session.Clock.StepIndex, "ranking never advances the sole clock");
                        c.That(session.Fault == null, "irrelevant expensive route skipped, all necessary callbacks healthy: " + mode);
                    }
                    if (mode != "cost") c.Equal(game.Hash(), twin.Hash(), "saved independent sessions choose identically: " + mode);
                    c.Equal(isolated, idle.Hash(), "interleaved idle session remains unchanged: " + mode);
                }
        }
    }
}
