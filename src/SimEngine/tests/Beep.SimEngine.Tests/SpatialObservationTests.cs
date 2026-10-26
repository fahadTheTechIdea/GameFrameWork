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
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class SpatialObservationTests
    {
        public static void Register() => TestRunner.Add("Spatial observations: physical typed samples and remembered refresh", Run);
        private const string Provider = "sim.knowledge.facts";
        private static GameFactory Points(string work = null) => ConstructionPlanTests.Points(
            additionalModule: new KnowledgeProofTechnologyModule(),
            moduleDeclaration: "{\"id\":\"sim.technology\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"spatial-reading-work.json\",\"technology.json\"]}",
            moduleDocument: work ?? FrameworkProofContent.Read("spatial-reading-work.json"),
            knowledgeDefinitions: "{\"knowledge_effects\":[{\"id\":\"field.learn\",\"subject\":\"field.reading\"}]}",
            extraDocuments: new Dictionary<string, string> { ["technology.json"] = FrameworkProofContent.Read("technology.json") },
            additionalDocuments: new Dictionary<string, string> { ["spatial-layers.json"] = FrameworkProofContent.Read("point-layers.json") },
            scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(TechnologyModule.ModuleId)));
        private static KnowledgeObservation Reading(SimGame game)
        {
            if (!game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), Provider, "field.reading", out var reading))
                throw new InvalidOperationException("missing remembered field sample");
            return reading;
        }
        private static long Value(KnowledgeObservation reading) => new JsonReader(reading.Data, "sample").Long("value_raw");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " fault=" + game.Fault + " orders=" +
                string.Join(";", Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static Order Start(Check c, SimGame game, string plan)
        {
            var command = new PostOrderCommand { Plan = plan, Target = Find(game, "source").Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary physical sample preview");
            CommandResult result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary physical sample command");
            return Board(game).Order((int)result.Arg0);
        }
        private static void Write(Check c, SimGame game, int value)
            => c.That(TestKit.Order(game, 0, new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.Zero, Value = Fix.FromInt(value) }).Accepted,
                "ordinary provider command changes actual sample, not remembered data");
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "typed native subject has exact identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no fixed-fact-only legacy reader");
            GameFactory factory = Points(); SimGame game = ConstructionPlanTests.NewPoints(factory); Configure(c, game);
            var subject = game.World.Content.Get<PointReadingDef>("field.reading");
            c.That(ReferenceEquals(subject, (IKnowledgeSubjectDefinition)game.World.Content.Get<IKnowledgeSubjectDefinition>(subject.Id)), "content contract aliases actual subject");
            c.That(subject.AuthoringPath.Contains("spatial-reading-work.json"), "public sample comes from actual master JSON");
            c.That(ReferenceEquals(subject, game.World.Content.Get<KnowledgeEffectDef>("field.learn").Subject), "SAME native effect resolves public subject");
            c.Equal(Codes.InvalidArgument, subject.CheckTarget(game.World, Find(game, "destination")), "wrong actual site refuses before reading");
            Write(c, game, 11); Order first = Start(c, game, "field.sample");
            Reach(game, () => first.Node("sample").Progress.WholeTicks > 0, "first physical spatial sample");
            c.That(!game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), Provider, subject.Id, out _), "partial work publishes no remembered sample");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "first spatial partial save");
            Reach(game, () => first.State == OrderState.Done, "first physical capture", twin);
            c.Equal(game.Hash(), twin.Hash(), "physical capture twins agree");
            KnowledgeObservation original = Reading(game);
            c.Equal(Fix.FromInt(11).Raw, Value(original), "SAME native effect captures actual non-tile value");
            c.Equal("point.terrain", original.Data.Get("layer").Text, "stable actual layer identity remembered");
            c.Equal("field.points", original.Data.Get("space").Text, "stable actual space identity remembered");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "survey uses eight actual supplies");
            Write(c, game, 29);
            c.Equal(Fix.FromInt(11).Raw, Value(Reading(game)), "live world write does not rewrite remembered sample");
            c.Equal(original.ObservedTick, Reading(game).ObservedTick, "live world write does not impersonate observation");
            Order refresh = Start(c, game, "field.sample");
            Reach(game, () => refresh.Node("sample").Progress.WholeTicks > 0, "refresh physical sample");
            c.Equal(Fix.FromInt(4), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "second survey spends actual inputs");
            c.Equal(Value(original), Value(Reading(game)), "partial refresh keeps historical reading");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "old sample and partial refresh save together");
            Reach(game, () => refresh.State == OrderState.Done, "physical spatial refresh", twin);
            c.Equal(game.Hash(), twin.Hash(), "spatial refresh saved continuation agrees");
            KnowledgeObservation current = Reading(game);
            c.Equal(Fix.FromInt(29).Raw, Value(current), "completed work captures current real sample");
            c.Equal(original.LearnedTick, current.LearnedTick, "physical spatial refresh preserves first learning");
            c.That(current.ObservedTick > original.ObservedTick, "physical spatial refresh advances observed time");
            c.Equal(Fix.FromInt(11).Raw, Value(original), "held sample remains detached historical data");
            c.Throws<InvalidOperationException>(() => current.Data.Set("value_raw", JsonValue.Number("0")), "remembered spatial payload immutable");
            Write(c, game, 37); Order cancelled = Start(c, game, "field.reobserve");
            Reach(game, () => cancelled.Node("sample").Progress.WholeTicks > 0, "cancelled spatial observation");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancelled.Id }).Accepted, "ordinary spatial cancellation");
            c.Equal(Value(current), Value(Reading(game)), "cancelled work publishes no new world reading");
            c.Equal(current.ObservedTick, Reading(game).ObservedTick, "cancelled work earns no fresh observation time");
            c.That(!game.Seat(1).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), Provider, subject.Id, out _), "foreign seat cannot read remembered survey");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled work and remembered spatial reading persist");
            var known = new List<KnowledgeObservation>(); game.Seat(0).View.Feature<KnowledgeView>().CopyKnown(known);
            c.Equal(1, known.Count, "SAME subject/ledger record, no sample history/grid duplication");
            SimGame isolated = ConstructionPlanTests.NewPoints(factory);
            c.That(!isolated.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), Provider, subject.Id, out _), "interleaved session has no borrowed spatial reading");
            ulong hash = game.Hash(); JsonValue malformed = current.Data.Clone(); malformed.Set("layer", JsonValue.Of("point.water"));
            c.Equal(Codes.InvalidArgument, game.World.Services.Get<ICompanyKnowledge>().CheckRecord(new PlayerId(0), Provider, subject.Id, malformed), "historical payload cannot substitute layer");
            c.Equal(hash, game.Hash(), "subject readiness and malformed validation are pure");
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Points(FrameworkProofContent.Read("spatial-reading-work.json").Replace("\"position\":0", "\"position\":-1"))), "public subject invalid geometry fails before start");
            NativeProtocol(c);
        }
        // Native setup/capture/ledger calls are explicit protocol instrumentation, NOT timed tile survey acceptance.
        private static void NativeProtocol(Check c)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("spatial-observer.json"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(CompanyKnowledgeModule.ModuleId));
            root.Get("spatial_layers").Add(Content.Json.Parse("{\"id\":\"observer.byte\",\"space\":\"observer.tiles\",\"type\":\"surface.classification\",\"value_type\":\"byte\",\"visibility\":\"public\",\"unit\":\"observer.count\"}"));
            root.Set("entities", Content.Json.Parse("[{\"id\":\"survey.site\",\"kind\":\"structure\",\"components\":{\"tile_presence\":{\"space\":\"observer.tiles\",\"width\":1,\"height\":1,\"blocks\":false,\"movable\":false}}}]"));
            string knowledge = "{\"tile_readings\":[{\"id\":\"reading.byte\",\"layer\":\"observer.byte\",\"x\":1,\"y\":1,\"retention\":\"latest\",\"fresh_for\":null},{\"id\":\"reading.int\",\"layer\":\"observer.public\",\"x\":1,\"y\":1,\"retention\":\"latest\",\"fresh_for\":null},{\"id\":\"reading.fixed\",\"layer\":\"observer.private\",\"x\":1,\"y\":1,\"retention\":\"first\",\"fresh_for\":null}]}";
            GameFactory factory = SpatialLayerTests.Factory(root.ToString(), knowledge);
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "layers", Seed = 719 }); var map = (TileMap)game.World.Space;
            map.AssignOwner(game.World, "observer.private", new PlayerId(0));
            var tile = new TileCoord(1, 1); map.Layer<ByteLayer>("observer.byte").Set(tile, 5);
            map.Layer<IntLayer>("observer.public").Set(tile, -7); map.Layer<FixedLayer>("observer.private").Set(tile, Fix.FromInt(9));
            Entity site = game.World.Spawn(game.World.Content.Get<EntityDef>("survey.site"), new PlayerId(0),
                entity => entity.Get<ITilePresence>().Place(tile, new FixVec2(Fix.FromInt(1), Fix.FromInt(1))));
            var ledger = game.World.Services.Get<ICompanyKnowledge>();
            foreach (string id in new[] { "reading.byte", "reading.int", "reading.fixed" })
            {
                var reading = game.World.Content.Get<TileReadingDef>(id); ulong before = game.Hash();
                c.Equal(Code.None, reading.CheckTarget(game.World, site), "native sample admitted actual footprint/ownership " + id);
                JsonValue data = reading.Observe(game.World, site); c.Equal(Code.None, reading.CheckObservation(game.World, data), "native typed payload valid " + id);
                c.Equal(before, game.Hash(), "native capture is a pure read " + id);
                c.Equal(Code.None, ledger.Record(site.Owner, Provider, id, data), "same ledger records actual native sample " + id);
                c.That(ledger.TryRead(site.Owner, site.Owner, Provider, id, out var remembered), "native remembered sample exists " + id);
                long expected = id == "reading.byte" ? Fix.FromInt(5).Raw : id == "reading.int" ? Fix.FromInt(-7).Raw : Fix.FromInt(9).Raw;
                c.Equal(expected, Value(remembered), "native actual value retained " + id);
            }
            map.AssignOwner(game.World, "observer.private", new PlayerId(1));
            c.Equal(Codes.NotOwned, game.World.Content.Get<TileReadingDef>("reading.fixed").CheckTarget(game.World, site), "private owner loss refuses future capture before sample read");
            c.That(ledger.TryRead(new PlayerId(0), new PlayerId(0), Provider, "reading.fixed", out _), "owned prior historical knowledge is not current layer authority");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native captured historical values persist after owner change");
            c.Throws<ContentException>(() => SpatialLayerTests.Factory(root.ToString(), knowledge.Replace("\"x\":1", "\"x\":-1")).NewGame(new NewGameRequest { Scenario = "layers", Seed = 719 }), "native out-of-bounds subject refuses authoring");
        }
    }
}
