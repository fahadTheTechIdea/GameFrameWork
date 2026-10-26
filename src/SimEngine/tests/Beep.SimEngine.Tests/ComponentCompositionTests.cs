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
    // Actual authored files/master and public player commands/views/Update only.
    internal static class ComponentCompositionTests
    {
        internal const string Case = "Component composition: authored prerequisites validate before initialization and preserve physical execution";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static JsonValue Document() => Json.Parse(FrameworkProofContent.Read("component-composition.json"));
        internal static GameFactory Factory(bool half, out ComponentCompositionModule probe,
            Action<JsonValue, JsonValue> edit = null,string authoredDocument=null)
        {
            var world = Json.Parse(PointGame.Definitions); var scenario = world.Get("scenarios")[0];
            scenario.Set("seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of("proof.component-composition"));
            if (half) world.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            StartupContentAuthoring.Position(world, "source", JsonValue.Number("2"));
            StartupContentAuthoring.Position(world, "destination", JsonValue.Number("18"));
            StartupContentAuthoring.Position(world, "machine", JsonValue.Number("4"));
            var document = authoredDocument==null?Document():Json.Parse(authoredDocument); edit?.Invoke(world, document);
            probe = new ComponentCompositionModule();
            return PointGame.Factory(world.ToString(), additionalModule: probe,
                moduleDeclaration: "{\"id\":\"proof.component-composition\",\"version\":\"1.0.0\",\"requires\":[],\"documents\":[\"component-composition.json\"]}",
                moduleDocument: document.ToString());
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 921, Seats = { Controller.Human, Controller.Human } });
        private static EntityView Entity(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(x => x.Def == id);
        private static Fix Position(SimGame game) => ((PointSnapshot)Entity(game, "machine").Spatial).Position;
        private static Fix Stock(SimGame game, string id)
        {
            var rows = new List<Facet>();
            if (!game.Seat(0).View.Inspect(Entity(game, id).Id, rows)) throw new InvalidOperationException("missing stock inspection");
            return rows.Single(x => x.Key == Storage.FacetStock && x.Subject == "supply").Value;
        }
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static void Reach(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 320 && !done(); i++) Step(game, twin);
            if (!done()) {
                var orders = new List<OrderView>(); Work(game).CopyOrders(orders);
                throw new InvalidOperationException(stage + " exceeded320Updates clock=" + game.Clock.StepIndex +
                    " position=" + Position(game) + " stock=" + Stock(game, "source") + "/" + Stock(game, "destination") +
                    " orders=" + string.Join(";", orders.Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current metadata composition identity");
            c.Equal(154, EngineInfo.SaveFormat, "original physical schemas unchanged");
            c.That(typeof(IComponentCompositionValidation).IsAssignableFrom(typeof(ISpatialPresenceDefinition)), "native presence shares original generic composition pass");
            foreach (bool half in new[] { false, true }) Physical(c, half);
            Native(c);
            Refusals(c);
        }
        private static void Physical(Check c, bool half)
        {
            var factory = Factory(half, out var probe); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            c.Equal(2, probe.Requirements.Calls, "actual unused prerequisite metadata admitted once per session");
            c.Equal(2, probe.Audit.Calls, "second metadata validator coexists with presence and prerequisites");
            c.Equal(2, probe.Initialize.Initializations, "valid composition initializes both sessions");
            c.That(game.Seat(0).Snapshot.Entities.All(x => x.Def != "composition.unused"), "metadata validation never instantiates unused prototype");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical resources and work have no business requirement");
            c.Throws<ArgumentException>(() => probe.Requirements.Owner.Component<IComponentCompositionValidation>(), "plural lifecycle is not a singular entity capability query");
            var limits = new ReplayLimits(2 * 1024 * 1024, 2000); using var recorder = game.RecordReplay(limits);
            var command = new PostOrderCommand { Plan = "inspect", Target = Entity(game, "destination").Id };
            var result = PermanentRemovalTests.Submit(c, game, command);
            c.That(Work(game).ActiveOrders(command.Target) > 0, "posted staffed work is not instant");
            var foreign = new List<ActivityNodeView>();
            c.That(!Work(game, 1).TryCopyNodes((int)result.Arg0, foreign) && foreign.Count == 0, "foreign company gets no private graph state");
            var twin = factory.Load(game.Save());
            int callbacks = probe.Requirements.Calls;
            game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); Step(game, twin);
            c.Equal(paused, game.Hash(), "one paused clock freezes physical work"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, twin, () => Work(game).ActiveOrders(command.Target) == 0, "timed point inspection");
            c.Equal(Fix.FromInt(18), Position(game), "machine physically reaches exact target before completing work");
            c.Equal(callbacks, probe.Requirements.Calls, "Update never reruns metadata composition admission");
            var retained = Entity(game, "machine").Spatial;
            c.That(game.Seat(0).View.TryFindResource("supply", out var supply), "actual authored resource resolves publicly");
            PermanentRemovalTests.Submit(c, game, new HaulResourcesCommand {
                Source = Entity(game, "source").Id, Destination = command.Target, Worker = Entity(game, "machine").Id,
                Resource = supply, Ceiling = Fix.FromInt(4), Priority = 100 });
            c.Equal(Fix.Zero, Stock(game, "destination"), "posting haul creates no transferred stock");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "physical pickup intent saves without repeating startup");
            Reach(game, twin, () => Position(game) != Fix.FromInt(18), "physical trip to source");
            c.Equal(Fix.FromInt(18), ((PointSnapshot)retained).Position, "retained spatial state is detached");
            Reach(game, twin, () => Stock(game, "destination") == Fix.FromInt(4), "physical handling and delivery");
            c.Equal(Fix.FromInt(16), Stock(game, "source"), "actual delivered stock debited once");
            c.Equal(Fix.FromInt(4), Stock(game, "destination"), "actual delivered stock credited once");
            var save = game.Save(); c.Equal(game.Hash(), factory.Load(save).Hash(), "completed physical state restores");
            var changed = Factory(half, out _, (world, doc) => doc.Get("entities")[0].Get("components").Get("composition_requirements").Set("requires", Json.Parse("[\"placement\",\"resources\"]")));
            c.That(!changed.Describe(save).Compatible, "changed actual unused metadata changes selected content identity");
            c.Throws<Exception>(() => changed.Load(save), "incompatible composition cannot silently restore old save");
            var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(verified.Success && verified.Game.Hash() == game.Hash(), "ordinary player work/haul stream replays identically");
            c.Equal(isolated, idle.Hash(), "interleaved session shares only immutable metadata, not physical state");
        }
        private static void Native(Check c)
        {
            var doc = Document(); var parts = doc.Get("entities")[0].Get("components"); parts.Remove("point_presence");
            parts.Get("composition_requirements").Set("space", JsonValue.Of("survey.tiles"));
            parts.Get("composition_requirements").Get("requires").Add(JsonValue.Of("networks"));
            var probe = new ComponentCompositionModule(true);
            var factory = AuthoredSegmentWorkflowTests.Factory(extraModule: probe, extraDocument: "component-composition.json", extraJson: doc.ToString());
            var game = LineDefinitionContractTests.New(factory);
            c.Equal(1, probe.Requirements.Calls, "same metadata contract checks native service/geometry aliases");
            c.Equal(1, probe.Audit.Calls, "native prototype retains two lifecycle validators");
            var limits = new ReplayLimits(2 * 1024 * 1024, 2000); using var recorder = game.RecordReplay(limits);
            var lay = new LayLineCommand { Line = "segment.road-line" }; lay.Tiles.AddRange(new[] { new TileCoord(16,10), new TileCoord(17,10), new TileCoord(18,10) });
            PermanentRemovalTests.Submit(c, game, lay); var order = LineDefinitionContractTests.Orders(game).Last();
            c.Equal(9, order.NodeCount, "original authored multi-cell graph executes beneath metadata admission");
            c.Equal(0, LineDefinitionContractTests.Tile(game, "segment.road", 16, 10), "metadata/intent does not complete infrastructure");
            var twin = factory.Load(game.Save()); int calls = probe.Requirements.Calls;
            LineDefinitionContractTests.Reach(game, twin, () => LineDefinitionContractTests.Plans(game).Single(x => x.Order == order.Id).State == PlanState.Done, "native prerequisite-admitted corridor");
            c.Equal(Fix.FromInt(34), LineDefinitionContractTests.Steel(game), "physical native work consumes original materials once");
            c.Equal(calls, probe.Requirements.Calls, "physical corridor execution never repeats composition callbacks");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native completed corridor restores");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "native ordinary physical corridor replays");
        }
        private static void Refusals(Check c)
        {
            foreach (string mode in new[] { "no-space", "wrong-space", "missing-provider", "missing-layers", "missing-placement", "missing-service", "throw", "pause", "geometry", "unknown", "duplicate", "unknown-space" })
            {
                ComponentCompositionModule probe = null;
                c.Throws<Exception>(() => New(Factory(false, out probe, (world, doc) => {
                    var scenario = world.Get("scenarios")[0]; var parts = doc.Get("entities")[0].Get("components");
                    var required = parts.Get("composition_requirements");
                    if (mode == "no-space") scenario.Set("space", JsonValue.Null());
                    if (mode == "wrong-space") { var other = world.Get("point_spaces")[0].Clone(); other.Set("id", JsonValue.Of("other.points")); world.Get("point_spaces").Add(other); scenario.Set("space", JsonValue.Of("other.points")); }
                    if (mode == "missing-provider" || mode == "missing-layers" || mode == "missing-placement") {
                        string id = "negative.space"; parts.Remove("point_presence"); required.Set("space", JsonValue.Of(id)); scenario.Set("space", JsonValue.Of(id));
                        doc.Set(mode == "missing-provider" ? "absent_composition_spaces" : "bare_composition_spaces", Json.Parse("[{\"id\":\"negative.space\"}]"));
                        if (mode == "missing-placement") required.Set("requires", Json.Parse("[\"placement\"]"));
                    }
                    if (mode == "missing-service") required.Get("requires").Add(JsonValue.Of("networks"));
                    if (mode == "throw" || mode == "pause" || mode == "geometry") required.Set("fault", JsonValue.Of(mode));
                    if (mode == "unknown") required.Get("requires").Add(JsonValue.Of("fictional-provider"));
                    if (mode == "duplicate") required.Get("requires").Add(JsonValue.Of("layers"));
                    if (mode == "unknown-space") required.Set("space", JsonValue.Of("unregistered.space"));
                })), "authored prerequisite refusal " + mode);
                c.That(probe != null && probe.Initialize.Initializations == 0, mode + " refuses before any Initialize");
                bool authored = mode == "unknown" || mode == "duplicate" || mode == "unknown-space" || mode == "missing-provider";
                c.That(authored ? probe.Requirements == null || probe.Requirements.Calls == 0 : probe.Requirements != null && probe.Requirements.Calls == 1,
                    mode + " refuses at original authored/provider/hook boundary");
            }
        }
    }
}
