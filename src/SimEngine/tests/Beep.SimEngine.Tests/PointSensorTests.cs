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
    // Player commands and observations only; the non-friend extension assembly owns sensor geometry.
    public static class PointSensorTests
    {
        private static readonly Code Sight = new Code("sensor.sight");
        public static void Register() => TestRunner.Add("Point sensors: authored supply gates, physical survey and private saved sight", Run);
        internal static GameFactory Factory(string document = null, IReadOnlyList<GameModule> additionalModules = null,
            IReadOnlyList<JsonValue> additionalDeclarations = null, IReadOnlyDictionary<string, string> extraDocuments = null,
            Action<JsonValue> definitionEdit = null, string documentPath = "point-sensor.json")
        {
            var definitions = Json.Parse(PointGame.Definitions);
            var scenario = definitions.Get("scenarios")[0];
            scenario.Set("seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of(PointSensorModule.ModuleId));
            scenario.Get("settings").Set(KnowledgeVisibilityModule.ModuleId, Json.Parse("{\"policy\":\"sensor.sight.policy\"}"));
            definitionEdit?.Invoke(definitions);
            var selected = new List<GameModule> { new KnowledgeVisibilityModule(), new InventorySignalsModule() };
            if (additionalModules != null) selected.AddRange(additionalModules);
            var declarations = Json.Parse("[{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
            if (additionalDeclarations != null) foreach (var declaration in additionalDeclarations) declarations.Add(declaration);
            var sensorModule = Json.Parse("{\"id\":\"proof.point-sensors\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"points\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"},{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\"}],\"documents\":[]}");
            sensorModule.Get("documents").Add(JsonValue.Of(documentPath));
            return PointGame.Factory(definitions.ToString(),
                additionalModule: new PointSensorModule(),
                moduleDeclaration: sensorModule.ToString(),
                moduleDocument: document ?? FrameworkProofContent.Read(documentPath),
                extraModules: selected,
                extraDeclarations: declarations.ToString(), extraDocuments: extraDocuments,
                additionalDocuments: new Dictionary<string,string> { ["point-layers.json"] = FrameworkProofContent.Read("point-layers.json") }, startupProfiles: new[] { "point-sensor.start" });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 734 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static EntityView Find(SimGame game, string definition, int seat = 0)
        {
            var entities = new List<EntityView>(); game.Seat(seat).View.CopyEntities(entities);
            return entities.Single(e => e.Def == definition);
        }
        private static Fix Position(SimGame game) => ((PointSnapshot)Find(game, "sensor.machine").Spatial).Position;
        private static Fix Stock(SimGame game, string definition)
            => game.World.Entities.All.Single(e => e.Def.Id == definition).Get<IInventory>().Amount(game.World.Content.ResourceId("supply"));
        private static bool Known(SimGame game) => game.Seat(0).View.Feature<KnowledgeView>()
            .TryRead(new PlayerId(0), "sim.knowledge.facts", "sensor.reading", out _);
        private static PointLayerView Layer(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<PointLayerView>();
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("point sensor Update failed: " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string phase, SimGame twin = null)
        {
            for (int i = 0; i < 1200 && !done(); i++) Update(game, twin);
            if (!done())
            {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException(phase + " clock=" + game.Tick + " position=" + Position(game) + " jobs=" +
                    string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
            }
        }
        private static CommandResult Order(Check c, SimGame game, Command command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary command preview");
            var answer = TestKit.Order(game, 0, command); c.That(answer.Accepted, "actual engine command answer");
            return answer;
        }
        private static int Route(Check c, SimGame game, string from, string to, int batch)
        {
            game.Seat(0).View.TryFindResource("supply", out var resource);
            return (int)Order(c, game, new AddRouteCommand { From = Find(game, from).Id, To = Find(game, to).Id,
                Resource = resource, Settings = new LogisticsRouteSettings(true, Fix.FromInt(batch), 70) }).Arg0;
        }
        private static OperationSnapshot Operation(SimGame game)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetOperation(Find(game, "sensor.machine").Id, Sight, out var value))
                throw new InvalidOperationException("missing actual sensor operation");
            return value;
        }
        internal static GameFactory PhysicalFactory(string documentPath = "point-sensor.json", string documentOverride = null,
            IReadOnlyList<GameModule> additionalModules = null, IReadOnlyList<JsonValue> additionalDeclarations = null,
            IReadOnlyDictionary<string, string> extraDocuments = null)
        {
            // Keep the same source quantities, sensor positions, speeds and work duration. On a
            // one-dimensional line a carrier cannot pass a mobile sensor or park on the survey point.
            // Author real resource/work bays and put the source carrier on the sensor's delivery side.
            var document = Json.Parse(documentOverride ?? FrameworkProofContent.Read(documentPath));
            document.Get("entities")[0].Get("components").Set("point_dock", Json.Parse(
                "{\"resources\":[\"supply\"],\"work_roles\":[],\"slots\":[{\"id\":\"sensor.supply\",\"kind\":\"service\",\"offset\":1},{\"id\":\"sensor.exit\",\"kind\":\"departure\",\"offset\":2}]}"));
            return Factory(document.ToString(), additionalModules, additionalDeclarations, extraDocuments, definitionEdit: root => {
                if (additionalModules != null) foreach (var module in additionalModules)
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
                foreach (int index in new[] { 0, 2 }) root.Get("entities")[index].Get("components").Get("point_presence").Set("position", JsonValue.Number("6"));
                foreach (int index in new[] { 0, 2 }) root.Get("entity_setups")[index].Get("placement").Set("position", JsonValue.Number("6"));
                root.Get("entities")[1].Get("components").Set("point_dock", Json.Parse(
                    "{\"resources\":[\"supply\"],\"work_roles\":[\"inspect\"],\"slots\":[" +
                    "{\"id\":\"field.supply\",\"kind\":\"service\",\"resource\":\"supply\",\"offset\":1}," +
                    "{\"id\":\"field.cargo-exit\",\"kind\":\"departure\",\"resource\":\"supply\",\"offset\":2}," +
                    "{\"id\":\"field.survey\",\"kind\":\"service\",\"work_role\":\"inspect\",\"offset\":0}," +
                    "{\"id\":\"field.survey-exit\",\"kind\":\"departure\",\"work_role\":\"inspect\",\"offset\":0.25}]}"));
            }, documentPath: documentPath);
        }
        private static ActivityNodeView Node(SimGame game, int order)
        {
            var nodes = new List<ActivityNodeView>();
            if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes)) throw new InvalidOperationException("missing actual survey nodes");
            return nodes.Single(n => n.Id == "sample");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "existing core identity retained without runtime changes");
            var factory = PhysicalFactory(); var game = New(factory); var layer = Layer(game);
            var contact = Find(game, "sensor.contact", 1).Id;
            var origin = new PointRegion(Fix.FromInt(5), Fix.One, 1);
            var field = new PointRegion(Fix.FromInt(10), Fix.One, 1);
            var samples = new List<Fix>();
            c.That(game.World.Space is PointSpace && !game.World.Services.Has<IEconomy>(), "point world selects no raster or business");
            c.That(game.World.Content.Get<EntityDef>("sensor.machine").AuthoringPath.Contains("point-sensor.json"), "new prototype from explicit master-listed JSON");
            c.That(game.World.Entities.All.Single(e => e.Def.Id == "sensor.machine").Get<ISpatialSensor>(Sight) is PointSensor, "independent public sensor implementation installed");
            c.Equal(new Code("sensor.unsupplied"), Operation(game).Blocker, "actual empty inventory blocks sight");
            c.Equal(SpatialLayerReadStatus.Unavailable, layer.ReadLayer("point.terrain", origin, samples), "no fake current coverage before delivery");
            c.That(!game.Seat(0).View.TryGetEntity(contact, out _), "distant foreign contact hidden");
            c.That(!Known(game), "startup creates no remembered survey");
            c.Equal(1, Layer(game, 1).Layers.Count, "foreign seat sees only unlisted public water, never sensor terrain");

            int first = Route(c, game, "source", "sensor.machine", 1);
            c.Equal(Fix.Zero, Stock(game, "sensor.machine"), "route command does not instantly transfer material");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "in-flight supply checkpoint");
            Reach(game, () => Stock(game, "sensor.machine") == Fix.One, "real sensor supply delivery", twin);
            c.Equal(game.Hash(), twin.Hash(), "supply saved continuation identical");
            Order(c, game, new RemoveRouteCommand { Route = first });
            c.Equal(OperationStatus.Running, Operation(game).Status, "real delivered stock opens original execution gate");
            c.Equal(SpatialLayerReadStatus.Success, layer.ReadLayer("point.terrain", origin, samples), "actual non-tile source coverage now visible");
            c.Equal(SpatialLayerReadStatus.Unavailable, layer.ReadLayer("point.terrain", field, samples), "positive fractional radius not unlimited");
            var page = layer.ReadLayerPage("point.terrain", origin, new SpatialLayerPageRequest(0, 1), samples);
            c.Equal(SpatialLayerReadStatus.Success, page.Status, "scoped non-tile page");
            ulong stable = game.Hash(); int version = layer.Version;
            for (int i = 0; i < 4; i++) { layer.ReadLayer("point.terrain", origin, samples); Operation(game); }
            c.Equal(stable, game.Hash(), "sight and operation observations pure");
            c.Equal(version, layer.Version, "unchanged sensor does not repeatedly invalidate");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(new ConfigureOperationCommand { Entity = Find(game, "sensor.machine").Id, Operation = Sight, Enabled = false, Target = Fix.Half }), "private foreign sensor control reveals no existence");

            int second = Route(c, game, "source", "destination", 8);
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(8), "physical survey material delivery");
            Order(c, game, new RemoveRouteCommand { Route = second });
            Reach(game, () => { var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); return jobs.All(j => !j.IsHaul || j.State == JobState.Done || j.State == JobState.Cancelled); }, "physical haul departure");
            Order(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine").Id, Held = true });
            c.Equal(Fix.FromInt(11), Stock(game, "source"), "real nine supplied units removed from source");
            int survey = (int)Order(c, game, new PostOrderCommand { Plan = "sensor.sample", Target = Find(game, "destination").Id, Priority = 60 }).Arg0;
            Reach(game, () => Position(game) > Fix.FromInt(5) && Position(game) < Fix.FromInt(10), "physical sensor travel");
            c.That(Node(game, survey).Progress.WholeTicks == 0 && !Known(game), "travel earns no survey completion");
            c.Equal(SpatialLayerReadStatus.Invalidated, layer.ReadLayerPage("point.terrain", origin, new SpatialLayerPageRequest(0, 1, page.Cursor), samples).Status, "physical movement invalidates original private page");
            twin = factory.Load(game.Save());
            Reach(game, () => Node(game, survey).Progress.WholeTicks > 0, "physical sensor survey", twin);
            c.Equal(game.Hash(), twin.Hash(), "travelling sensor restores exact physical work");
            c.Equal(Find(game, "sensor.machine").Id, Node(game, survey).Worker, "actual non-tile sensor executes the survey");
            c.Equal(Fix.Zero, Stock(game, "destination"), "actual survey consumes eight real materials");
            c.That(!Known(game), "partial work cannot record remembered sample");
            c.That(game.Seat(0).View.TryGetEntity(contact, out _), "inclusive fractional point reach reveals actual foreign contact");
            var frozen = Find(game, "sensor.contact");
            c.Equal(SpatialLayerReadStatus.Success, layer.ReadLayer("point.terrain", field, samples), "actual field sight from physical arrival");
            twin = factory.Load(game.Save());
            Reach(game, () => Known(game), "completed physical point observation", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial sensor work resumes and completes identically");
            c.That(game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "sensor.reading", out var history), "native ledger retains earned point reading");
            c.Equal(Fix.Zero.Raw, new JsonReader(history.Data, "earned reading").Long("value_raw"), "earned observation captures actual zero layer sample, not invented deposit");

            var control = new ConfigureOperationCommand { Entity = Find(game, "sensor.machine").Id, Operation = Sight, Enabled = false, Target = Fix.FromRaw(Fix.One.Raw * 5 / 2) };
            Order(c, game, control);
            c.That(!game.Seat(0).View.TryGetEntity(contact, out _), "disabled sensor revokes live contact");
            c.Equal(1, layer.Layers.Count, "disabled sensor revokes terrain metadata, retaining unlisted public water");
            c.That(Known(game), "disabled sight does not erase learned survey");
            c.Equal(new Code("point.blocked"), Find(game, "sensor.machine").State, "entity Update settles inherited disabled state");
            c.Equal(new PlayerId(1), frozen.Owner, "held entity facts stay detached");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "disabled sensor/earned knowledge save together");
            c.Equal(1, Layer(twin).Layers.Count, "restore derives disabled terrain sight without fog state");
            control.Enabled = true; control.Target = Fix.Zero; Order(c, game, control);
            c.Equal(1, layer.Layers.Count, "explicit zero radius yields inactive terrain sight");
            control.Target = Fix.Half; Order(c, game, control);
            c.Equal(SpatialLayerReadStatus.Success, layer.ReadLayer("point.terrain", field, samples), "fractional non-tile radius legal");
            c.That(!game.Seat(0).View.TryGetEntity(contact, out _), "narrow current sight cannot reuse prior contact coverage");
            var fresh = New(factory);
            c.That(!Known(fresh) && Operation(fresh).Blocker == new Code("sensor.unsupplied"), "interleaved session borrows no knowledge/supply/control");
            c.Equal(Fix.FromInt(11), Stock(game, "source"), "no money/stock grants during controls or observation");
            c.Equal(Fix.One, Stock(game, "sensor.machine"), "sensor stock remains actual delivered material");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string fault in new[] { "space", "source", "signal", "unit", "negative", "missing-conditions" })
            {
                var document = Json.Parse(FrameworkProofContent.Read("point-sensor.json"));
                var sensor = document.Get("entities")[0].Get("components").Get("point_sensor");
                if (fault == "space") sensor.Set("space", JsonValue.Of("missing.space"));
                if (fault == "source") sensor.Get("conditions")[0].Set("source", JsonValue.Of("missing.source"));
                if (fault == "signal") sensor.Get("conditions")[0].Set("signal", JsonValue.Of("missing.signal"));
                if (fault == "unit") sensor.Get("operation").Set("unit", JsonValue.Of("unit.supply"));
                if (fault == "negative") sensor.Get("operation").Set("minimum", JsonValue.Number("-1"));
                if (fault == "missing-conditions") sensor.Remove("conditions");
                c.Throws<ContentException>(() => New(Factory(document.ToString())), "invalid authored sensor " + fault);
            }
        }
    }
}
