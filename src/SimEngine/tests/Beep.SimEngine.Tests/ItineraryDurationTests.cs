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
        private static JsonValue TravelExposure(bool tile) => Content.Json.Parse(
            "{\"id\":\"trip.exposure\",\"phase\":\"movement\",\"inputs\":[],\"states\":{\"disabled\":\"" + (tile ? "stopped" : "blocked") + "\"}}");
        private static GameFactory JourneyTiles()
        {
            var root = Content.Json.Parse(FrameworkProofContent.Read("tile-service-docks.json"));
            root.Get("entities")[2].Get("components").Set("operating_inputs", TravelExposure(true));
            var text = new StringBuilder(); root.WriteCanonical(text);
            var definitions = Content.Json.Parse(Colony.Defs);
            definitions.Get("tile_spaces")[0].Set("height", JsonValue.Number("48"));
            var authored = new StringBuilder(); definitions.WriteCanonical(authored);
            return Colony.Factory(definitions: authored.ToString(), additionalDefinitions: text.ToString());
        }
        private static Entity JourneyActor(Check c, SimGame game)
        {
            var initial = new System.Collections.Generic.List<Entity>(); foreach (Entity entity in game.World.Entities.All) if (entity.Has<IWorker>()) initial.Add(entity);
            foreach (Entity entity in initial) c.That(TestKit.Order(game, entity.Owner.Value,
                new MoveVehicleCommand { Vehicle = entity.Id, To = entity.Get<ITilePresence>().Tile }).Accepted, "ordinary player commands hold unrelated native workers");
            Spawn(game, "facility.source", 20, 20); Spawn(game, "facility.destination", 20, 30);
            var actor = Spawn(game, "facility.worker", 19, 18);
            Tick(game); // Settle normal creation events before checkpoint/purity assertions.
            return actor;
        }
        private static GameFactory JourneyPoints(bool returning = false)
        {
            var root = Content.Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var parts = root.Get("entities")[2].Get("components"); parts.Set("operating_inputs", TravelExposure(false));
            if (returning)
            {
                parts.Get("storage").Set("initial_stock", Content.Json.Parse("{\"supply\":4}"));
                StartupContentAuthoring.Position(root, "machine", JsonValue.Number("4"));
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                root.Get("scenarios")[0].Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Content.Json.Parse("{\"profile\":\"recovery.high\"}"));
            }
            var text = new StringBuilder(); root.WriteCanonical(text); return PointGame.Factory(text.ToString(),
                extraModules: returning ? new GameModule[] { new LogisticsRecoveryPolicyModule() } : null,
                extraDeclarations: returning ? "[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}]" : null,
                extraDocuments: returning ? new System.Collections.Generic.Dictionary<string, string> { ["logistics-recovery.json"] = FrameworkProofContent.Read("logistics-recovery.json") } : null);
        }
        private static long AcceptedTravel(SimGame game, Entity actor)
        {
            var code = game.Seat(0).View.Feature<MotionView>().ReadOperatingInputs(actor.Id, 0, out var facts);
            if (!code.IsNone) throw new InvalidOperationException("trip exposure refused: " + code);
            foreach (var provider in facts.Providers) if (provider.Demand.Consumer == new Code("trip.exposure")) return provider.Exposure.ElapsedTicks;
            throw new InvalidOperationException("missing explicitly authored trip exposure");
        }
        private static HaulPreview JourneyPreview(SimGame game, bool tile, int amount = 4)
            => game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game, tile ? "facility.source" : "source").Id,
                Find(game, tile ? "facility.destination" : "destination").Id, game.World.Content.ResourceId(tile ? "steel" : "supply"), Fix.FromInt(amount));
        private static void JourneyAdmission(Check c)
        {
            var max = MotionArrivalContracts.Combine(new MotionTravelEstimate(long.MaxValue, Code.None), new MotionTravelEstimate(0, Code.None));
            c.Equal(long.MaxValue, max.RemainingTicks.Value, "trip totals retain signed64-bit duration without narrowing/platform cap");
            var overflow = MotionArrivalContracts.Combine(max, new MotionTravelEstimate(1, Code.None));
            c.That(overflow.RemainingTicks == null && overflow.Blocker == MotionArrivalCodes.Overflow, "overflow never exposes an incomplete known trip prefix");
            var unknown = MotionArrivalContracts.Combine(new MotionTravelEstimate(null, MotionArrivalCodes.Unsupported), new MotionTravelEstimate(4, Code.None));
            c.That(unknown.RemainingTicks == null && unknown.Blocker == MotionArrivalCodes.Unsupported, "unsupported leg never becomes zero guessed travel");
            c.Throws<ArgumentException>(() => MotionArrivalContracts.Combine(unknown, default), "an earlier unknown does not hide malformed later leg metadata");

            var game = New(JourneyTiles()); var actor = JourneyActor(c, game); ulong hash = game.Hash();
            var preview = JourneyPreview(game, true);
            c.That(preview.Status == HaulStatus.Ready && preview.Carrier == actor.Id && preview.Travel.Value.RemainingTicks == 18,
                "same selected native trip includes two approach, ten delivery and six departure movement intervals");
            c.Equal(hash, game.Hash(), "selected whole-trip query neither allocates claims/jobs nor moves time/presence");
            var foreign = game.Seat(1).View.Feature<LogisticsView>().PreviewHaul(preview.Source, preview.Destination, preview.Resource, Fix.FromInt(4));
            c.That(foreign.Status == HaulStatus.NotFound && foreign.Travel == null, "private refusal exposes no selected trip duration");

            var alternatives = New(Facilities()); var first = Setup(c, alternatives, 22); Policy(c, alternatives, first.Id, "itinerary.connected", true);
            var alternative = JourneyPreview(alternatives, true);
            c.That(alternative.Status == HaulStatus.Ready && alternative.SourceDockSlot == new Code("bay.b") &&
                alternative.DestinationDockSlot == new Code("bay") && alternative.Travel.Value.RemainingTicks > 9,
                "complete selected alternative includes reachable departure, not rejected earlier bays or delivery-only prefix");
            int assigned = Haul(alternatives).AcquireHaul(new PlayerId(0), alternative.Source, alternative.Destination, alternative.Resource, Fix.FromInt(4), 100);
            c.That(assigned > 0, "first real haul owns the finite source bay");
            var second = Spawn(alternatives, "facility.worker", 18, 24); Policy(c, alternatives, second.Id, "itinerary.connected", true);
            var queued = JourneyPreview(alternatives, true);
            c.That(queued.Status == HaulStatus.Ready && queued.Carrier == second.Id && queued.SourceDockSlot == new Code("wait.a") && queued.Travel.HasValue,
                "selected waiting trip retains full future service/departure travel without guessed queue delay");
            c.Equal(Code.None, Board(alternatives).PreviewDock(Find(alternatives, "facility.source"), second,
                WorkDockRequest.Haul(queued.Resource, ResourceHandlingPhase.Loading), null, out var selected, out _, out _, out var approach),
                "existing dock selector publishes the same waiting-to-service journey");
            c.That(selected == queued.SourceDockSlot && approach.RemainingTicks == 3,
                "waiting approach plus physical service promotion are both included in movement exposure");

            var unsupported = NewPoint(Points("route-duration-unsupported")); var partial = JourneyPreview(unsupported, false);
            c.That(partial.Status == HaulStatus.Ready && partial.Travel.Value.RemainingTicks == null && partial.Travel.Value.Blocker == MotionArrivalCodes.Unsupported,
                "structurally admitted public itinerary explicitly carries unknown complete duration");
        }
        private static void JourneyPhysical(Check c)
        {
            foreach (bool tile in new[] { true, false })
            {
                var factory = tile ? JourneyTiles() : JourneyPoints(); var game = tile ? New(factory) : NewPoint(factory);
                var actor = tile ? JourneyActor(c, game) : Find(game, "machine"); var preview = JourneyPreview(game, tile);
                c.That(preview.Status == HaulStatus.Ready && preview.Travel.Value.RemainingTicks.HasValue, "whole prospective physical trip is engine-selected and known");
                long before = AcceptedTravel(game, actor); long start = game.Clock.StepIndex;
                int id = Haul(game).AcquireHaul(new PlayerId(0), preview.Source, preview.Destination, preview.Resource, preview.Amount, 100);
                c.That(id > 0, "ordinary native acquisition freshly admits the selected complete trip"); var job = Board(game).Job(id);
                Reach(game, () => job.ToilIndex >= 2, "selected trip loaded physical cargo");
                var twin = factory.Load(game.Save()); ulong hash = game.Hash(); long pausedTime = game.Clock.StepIndex;
                game.Clock.Pause(); twin.Clock.Pause(); game.Update(); twin.Update();
                c.That(game.Clock.StepIndex == pausedTime && game.Hash() == hash, "one-clock pause freezes loaded itinerary, not another trip timer");
                game.Clock.Resume(); twin.Clock.Resume();
                Reach(game, () => !job.IsLive, "selected complete trip physically delivers and departs", twin: twin);
                c.Equal(preview.Travel.Value.RemainingTicks.Value, AcceptedTravel(game, actor) - before,
                    "actual accepted native/public movement exposure equals complete quiet prospective trip");
                c.That(game.Clock.StepIndex - start > preview.Travel.Value.RemainingTicks.Value,
                    "real handling and workflow waits take game time separately, not counted as movement exposure");
                c.Equal(Fix.FromInt(4), game.World.Entities.Get(preview.Destination).Get<IInletPort>().Amount(preview.Resource), "selected trip physically delivers exact stock once");
                c.Equal(Fix.Zero, actor.Get<ITransporter>().Cargo.Amount(preview.Resource), "completed trip retains no duplicated carrier cargo");
                c.Equal(game.Hash(), twin.Hash(), "selected journey and native claims continue identically after loaded save");
                c.That(preview.Travel.Value.RemainingTicks == (tile ? 18 : 10), "retained trip total never follows actual job progress");
            }
            var returnFactory = JourneyPoints(true); var returning = NewPoint(returnFactory); var machine = Find(returning, "machine");
            returning.World.Services.Get<LogisticsService>().PrepareDispatch();
            var supply = returning.World.Content.ResourceId("supply"); var proposed = Haul(returning).PreviewReturnCargo(machine.Id, supply);
            c.That(proposed.Status == HaulStatus.Ready && proposed.Travel.Value.RemainingTicks == 4 && proposed.Destination == Find(returning, "source").Id,
                "cargo return budgets only its selected destination journey, never a fictitious pickup");
            Tick(returning); var saved = returnFactory.Load(returning.Save());
            Reach(returning, () => machine.Get<ITransporter>().Cargo.Amount(supply) == Fix.Zero, "selected cargo return", twin: saved);
            c.Equal(4L, AcceptedTravel(returning, machine), "real public cargo return earns exactly its native movement exposure");
            c.Equal(returning.Hash(), saved.Hash(), "selected return cargo and real stock remain durable");
        }
    }
}
