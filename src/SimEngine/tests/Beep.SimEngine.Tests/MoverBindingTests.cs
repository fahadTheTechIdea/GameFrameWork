using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // JSON intent, ordinary player commands/Update/immutable views, no native movement/game rules.
    internal static class MoverBindingTests
    {
        internal const string Case = "Mover bindings: authored contracts drive physical tile travel and work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Create(MoverBindingModule module, out SpaceContentContractsModule space, bool independent = true,
            Action<JsonValue> edit = null)
        {
            space = new SpaceContentContractsModule();
            return SpaceContentContractTests.Create(space, extraModules: new[] { module }, edit: (world, authored, master) => {
                var parts = world.Get("entities")[1].Get("components"); var mover = parts.Get("mover");
                mover.Get("classes").Add(JsonValue.Of("survey.heavy"));
                if (independent) { parts.Remove("mover"); parts.Set("independent_mover", mover); }
                world.Set("uniform_tile_navigation", Json.Parse("[{\"id\":\"survey.heavy\",\"space\":\"survey.tiles\",\"cost\":20}]"));
                world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
                master.Get("modules").Add(Json.Parse("{\"id\":\"proof.mover-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.tile-navigation\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
                edit?.Invoke(world);
            });
        }
        private static TileSpatialSnapshot Position(SimGame game) => (TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Def == "survey.machine").Spatial;
        private static MotionView View(SimGame game) => game.Seat(0).View.Feature<MotionView>();
        private static MotionArrivalView Arrival(SimGame game)
        {
            if (!View(game).ReadArrival(Entity(game, "survey.machine"), out var value).IsNone) throw new InvalidOperationException("no arrival facts");
            return value;
        }
        private static bool Done(SimGame game) => game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:survey.sample", Entity(game, "survey.site"), out _);
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 480 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " tile=" + Position(game).Tile + " arrival=" + Arrival(game).Blocker);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current reusable motion identity");
            c.Equal(154, EngineInfo.SaveFormat, "same native mover codec4 and save layout");
            c.That(typeof(Mover).BaseType == typeof(Component) && typeof(ITileMover).IsAssignableFrom(typeof(Mover)), "original route runtime consumes contract metadata");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Invalid(c); Faults(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new MoverBindingModule(); var factory = Create(module, out var space, independent);
            var game = LiveSensorTests.New(factory);
            c.That(space.Aliases && space.Independent && module.Independent == independent && space.Initialize.Initializations == 1,
                "native/independent original mover in actual independent authored space");
            var fresh = LiveSensorTests.New(factory); ulong freshHash = fresh.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                var id = Entity(game, "survey.machine"); var origin = Position(game);
                Order(c, game, new SetWorkerHeldCommand { Worker = id, Held = true });
                c.Equal(Code.None, View(game).ReadNavigation(id, out var initial), "actual navigation facts");
                c.That(initial.Profile == "survey.route" && initial.Allowed.SequenceEqual(new[] { "survey.heavy", "survey.route" }), "one explicit sorted allowed list");
                c.Throws<NotSupportedException>(() => ((IList<string>)initial.Allowed).Clear(), "retained route choices immutable");
                ulong hash = game.Hash();
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadNavigation(id, out var hidden), "foreign route facts private");
                c.That(hidden == null && game.Hash() == hash, "private and healthy navigation projections pure");
                c.Equal(MotionNavigationCodes.NotAllowed, View(game).PreviewNavigation(id, "missing"), "undeclared profile typed refusal");
                var move = new MoveVehicleCommand { Vehicle = id, To = new TileCoord(10, 10) };
                Order(c, game, move); Step(game);
                c.That(Position(game).Position != origin.Position && Position(game).Tile != move.To, "real partial travel not placement");
                c.Equal(new Code("moving"), game.Seat(0).Snapshot.Entities.Single(e => e.Def == "survey.machine").State, "entity Update resolves actual authored moving state");
                var first = Arrival(game); c.That(first.Status == MoveStatus.Moving && first.RemainingTicks > 0, "engine computes remaining physical time");
                var selection = new ConfigureMotionNavigationCommand { Entity = id, Profile = "survey.heavy" };
                Order(c, game, selection); Step(game);
                var changed = Arrival(game);
                c.That(changed.RemainingTicks > first.RemainingTicks && initial.Profile == "survey.route", "authored costly route extends actual ETA without changing retained facts");
                c.Equal(Code.None, View(game).ReadNavigation(id, out var selected), "actual selected route");
                c.Equal("survey.heavy", selected.Profile, "original selected navigation owner");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "selected partial route saved through original codec");
                long at = game.Clock.StepIndex; hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(game.Clock.StepIndex == at && game.Hash() == hash, "sole clock pauses route/time/state");
                game.Clock.Resume(); twin.Clock.Resume();
                Reach(game, twin, () => Arrival(game).Status == MoveStatus.Arrived, "actual costly-route arrival");
                c.That(Position(game).Tile == move.To && game.Clock.StepIndex == at + changed.RemainingTicks, "native travel earns exact projected arrival, no extra loop");
                Order(c, game, new SetWorkerHeldCommand { Worker = id, Held = false });
                Order(c, twin, new SetWorkerHeldCommand { Worker = Entity(twin, "survey.machine"), Held = false });
                Order(c, game, new PostOrderCommand { Plan = "survey.sample", Target = Entity(game, "survey.site"), Priority = 60 });
                Order(c, twin, new PostOrderCommand { Plan = "survey.sample", Target = Entity(twin, "survey.site"), Priority = 60 });
                at = game.Clock.StepIndex; Reach(game, twin, () => Done(game), "physical survey with original contract-bound mover");
                c.That(game.Clock.StepIndex >= at + 30, "travel and half-hour productive work take game time");
                var facets = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, "survey.site"), facets);
                c.Equal(Fix.FromInt(12), facets.Single(f => f.Key == Storage.FacetStock && f.Subject == "steel").Value, "original transaction/material owner");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed native movement/work checkpoint");
                c.Equal(freshHash, fresh.Hash(), "interleaved independent route/space/materials");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary route selection/travel/work replay");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (bool independent in new[] { false, true })
                foreach (string bad in new[] { "class", "classes", "speed", "states", "blank", "unlisted", "duplicate", "zero", "negative", "unknown-state", "extra-state", "extra", "unused", "presence", "stationary" })
                {
                    var module = new MoverBindingModule(); SpaceContentContractsModule space = null;
                    c.Throws<ContentException>(() => LiveSensorTests.New(Create(module, out space, independent, edit: world => {
                        var entity = world.Get("entities")[1];
                        if (bad == "unused") { entity = entity.Clone(); entity.Set("id", JsonValue.Of("unused.mover")); world.Get("entities").Add(entity); }
                        var parts = entity.Get("components"); var row = parts.Get(independent ? "independent_mover" : "mover");
                        if (bad == "class" || bad == "classes" || bad == "speed" || bad == "states") row.Remove(bad);
                        if (bad == "blank") row.Set("class", JsonValue.Of(" "));
                        if (bad == "unlisted") row.Set("class", JsonValue.Of("missing.route"));
                        if (bad == "duplicate") row.Get("classes").Add(JsonValue.Of("survey.route"));
                        if (bad == "zero" || bad == "unused") row.Set("speed", JsonValue.Number("0"));
                        if (bad == "negative") row.Set("speed", JsonValue.Number("-1"));
                        if (bad == "unknown-state") row.Get("states").Set("moving", JsonValue.Of("missing.state"));
                        if (bad == "extra-state") row.Get("states").Set("idle", JsonValue.Of("survey.idle"));
                        if (bad == "extra") row.Set("host_speed", JsonValue.Number("100"));
                        if (bad == "presence") parts.Remove("tile_presence");
                        if (bad == "stationary") parts.Get("tile_presence").Set("movable", JsonValue.Of(false));
                    })), "mover declaration refuses " + independent + "/" + bad);
                    c.Equal(0, space.Initialize.Initializations, "loaded malformed mover before Initialize");
                }
            foreach (string bad in new[] { "class", "classes", "speed", "space", "states", "undeclared" })
            {
                var module = new MoverBindingModule(); SpaceContentContractsModule space = null;
                c.Throws<ContentException>(() => LiveSensorTests.New(Create(module, out space, edit: world => {
                    var parts = world.Get("entities")[1].Get("components"); var row = parts.Get("independent_mover");
                    if (bad == "undeclared") { parts.Remove("independent_mover"); parts.Set("undeclared_mover", row); }
                    else row.Set("proof_fault", JsonValue.Of(bad));
                })), "actual mover binding refuses " + bad);
                c.Equal(0, space.Initialize.Initializations, "actual malformed metadata/provider pair before Initialize");
            }
        }
        private static void Faults(Check c)
        {
            foreach (MoverBindingFault fault in Enum.GetValues(typeof(MoverBindingFault)))
                foreach (bool route in new[] { false, true })
                {
                    var module = new MoverBindingModule(); var game = LiveSensorTests.New(Create(module, out _));
                    Order(c, game, new SetWorkerHeldCommand { Worker = Entity(game, "survey.machine"), Held = true });
                    Order(c, game, new MoveVehicleCommand { Vehicle = Entity(game, "survey.machine"), To = new TileCoord(10, 10) }); Step(game);
                    module.Arm(fault); long at = game.Clock.StepIndex;
                    MotionArrivalView arrival = null; MotionRouteCheck projection = null;
                    c.Throws<InvalidOperationException>(() => {
                        if (route) View(game).ReadRoute(Entity(game, "survey.machine"), Position(game),
                            new MotionDestination(game.Seat(0).Snapshot.Entities.Single(e => e.Def == "survey.site").Spatial, MotionDestinationKind.Interaction), out projection);
                        else View(game).ReadArrival(Entity(game, "survey.machine"), out arrival);
                    }, "actual original motion codec guards speed observation " + fault + "/" + route);
                    c.That(arrival == null && projection == null && module.Reads == 1 && game.Fault?.Phase == "observation.provider", "first provider fault withholds motion facts");
                    c.That(game.Update() == 0 && game.Clock.StepIndex == at, "faulted motion cannot advance clock");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "faulted motion not healthy hash");
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted motion no checkpoint");
                }
        }
    }
}
