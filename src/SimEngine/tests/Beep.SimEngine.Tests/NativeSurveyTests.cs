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

namespace Beep.SimEngine.Tests
{
    public static class NativeSurveyTests
    {
        public static void Register() => TestRunner.Add("Native surveys: authored startup, physical travel and durable observations", Run);
        internal static GameFactory Factory(string startup = null, string world = null, string navigation = null)
        {
            var definition = new GameDefinition("native-survey", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(),
                new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule() });
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("native-survey-master.json"))
                .Add("native-survey-world.json", world ?? FrameworkProofContent.Read("native-survey-world.json"))
                .Add("native-survey-startup.json", startup ?? FrameworkProofContent.Read("native-survey-startup.json"))
                .Add("native-survey-navigation.json", navigation ?? FrameworkProofContent.Read("native-survey-navigation.json"))
                .Add("native-survey-work.json", FrameworkProofContent.Read("native-survey-work.json"))
                .Add("native-survey-knowledge.json", FrameworkProofContent.Read("native-survey-knowledge.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 719 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Entity(SimGame game, string prototype) => game.World.Entities.All.Single(e => e.Def.Id == prototype);
        private static bool Known(SimGame game) => game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out _);
        private static KnowledgeObservation Reading(SimGame game)
        {
            if (!game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out var reading))
                throw new InvalidOperationException("native survey has no remembered reading");
            return reading;
        }
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("native survey time=" + game.Tick + " " + game.Fault); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " orders=" +
                string.Join(";", game.World.Services.Get<WorkBoard>().Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static Order Start(Check c, SimGame game, string plan)
        {
            var command = new PostOrderCommand { Plan = plan, Target = Entity(game, "survey.site").Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal native survey preview");
            CommandResult result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal native survey accepted");
            return game.World.Services.Get<WorkBoard>().Order((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "authored native startup identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no legacy initialization reader");
            GameFactory factory = Factory(); SimGame game = New(factory);
            Entity site = Entity(game, "survey.site"), machine = Entity(game, "survey.machine");
            var map = (TileMap)game.World.Space; var sample = new TileCoord(16, 10); ResourceId steel = game.World.Content.ResourceId("steel");
            c.Equal(2, game.World.Entities.All.Count, "engine JSON startup creates exactly authored entities");
            c.Equal(site.Id, machine.Get<IWorker>().Home, "startup intent resolves to actual existing worker home");
            c.Equal(new TileCoord(2, 10), machine.Get<ITilePresence>().Tile, "worker starts at authored distant position");
            c.Equal(Fix.FromInt(20), site.Get<IInventory>().Amount(steel), "initial materials generated by native inventory initialization");
            c.Equal(Fix.FromInt(31), map.Layer<MapLayer>("survey.deposit").GetSample(sample), "engine seeds actual authored deposit");
            c.Equal(Fix.FromInt(42), map.Layer<MapLayer>("survey.terrain").GetSample(sample), "same sample owner preserves byte semantics");
            c.Equal(Fix.FromInt(-7), map.Layer<MapLayer>("survey.class").GetSample(sample), "same sample owner preserves signed int semantics");
            c.That(game.World.Content.Get<WorldSetupProfileDef>("survey.start").AuthoringPath.Contains("native-survey-startup.json"), "actual master startup file origin");
            c.That(game.World.Content.Get<UniformTileNavigationDef>("survey.route").AuthoringPath.Contains("native-survey-navigation.json"), "actual master navigation file origin");
            c.Equal(10, game.World.Services.Get<NavService>().Grid("survey.route").MinCost, "existing navigation service owns authored rule");
            c.That(!game.World.Services.Has<IEconomy>(), "native physical survey requires no economy or Tycoon provider");
            c.Equal(0, game.World.Services.Persistent().Count(s => s.Provider.Id == WorldSetupModule.ModuleId), "startup retains no second entity registry/codec");
            c.That(!Known(game), "authored deposit/startup never pre-grants survey knowledge");
            ulong before = game.Hash();
            c.Equal(Codes.InvalidArgument, map.Layer<MapLayer>("survey.terrain").CheckSample(sample, Fix.FromRatio(1, 2)), "byte sample refuses fractional truncation");
            c.Equal(Codes.InvalidArgument, map.Layer<MapLayer>("survey.deposit").CheckSample(sample, Fix.FromInt(-1)), "material sample refuses negative stock");
            c.Equal(before, game.Hash(), "normalized sample readiness is pure");

            Order first = Start(c, game, "survey.sample");
            Reach(game, () => machine.Get<IMotion>().Status == MoveStatus.Moving, "real machine travel");
            c.That(!Known(game) && first.Node("sample").Progress.WholeTicks == 0, "physical travel earns no early survey");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual moving native worker saves identically");
            c.Equal(2, twin.World.Entities.All.Count, "load does not execute startup twice");
            Reach(game, () => first.Node("sample").Progress.WholeTicks > 0, "first native partial work", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved physical travel resumes identically");
            c.That(!Known(game), "partial physical work does not capture sample");
            c.Equal(Fix.FromInt(12), site.Get<IInventory>().Amount(steel), "actual first survey consumes eight native materials");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial spent-work checkpoint");
            Reach(game, () => first.State == OrderState.Done, "first completed native survey", twin);
            c.Equal(game.Hash(), twin.Hash(), "native physical observation continues identically");
            KnowledgeObservation original = Reading(game);
            c.Equal(Fix.FromInt(31).Raw, new JsonReader(original.Data, "reading").Long("value_raw"), "completed work captures actual native deposit");
            c.That(original.LearnedTick > 0 && original.LearnedTick == original.ObservedTick && original.ObservedTick <= game.Tick, "native capture uses one actual game clock");
            c.That(machine.Get<ITilePresence>().Tile != new TileCoord(2, 10), "machine actually moved from its authored start");
            c.Equal(Fix.FromInt(31), map.Layer<MapLayer>("survey.deposit").GetSample(sample), "survey neither extracts nor invents deposit material");
            c.That(!game.Seat(1).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), original.Provider, original.Subject, out _), "foreign player gets no private survey");
            Order refresh = Start(c, game, "survey.sample");
            Reach(game, () => refresh.Node("sample").Progress.WholeTicks > 0, "native repeated physical sample");
            c.Equal(Fix.FromInt(4), site.Get<IInventory>().Amount(steel), "second sample consumes next physical eight materials");
            c.Equal(original.ObservedTick, Reading(game).ObservedTick, "partial native refresh preserves previous reading time");
            twin = factory.Load(game.Save()); Reach(game, () => refresh.State == OrderState.Done, "native refreshed sample", twin);
            c.Equal(game.Hash(), twin.Hash(), "native repeated capture restores deterministically");
            KnowledgeObservation latest = Reading(game);
            c.Equal(original.LearnedTick, latest.LearnedTick, "same-value native sample preserves first learning");
            c.That(latest.ObservedTick > original.ObservedTick, "completed native work re-establishes actual observation time");
            c.Equal(original.ObservedTick, original.LearnedTick, "held original observation remains detached");
            Order cancelled = Start(c, game, "survey.reobserve");
            Reach(game, () => cancelled.Node("sample").Progress.WholeTicks > 0, "cancelled native sample work");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancelled.Id }).Accepted, "normal native observation cancellation");
            c.Equal(latest.ObservedTick, Reading(game).ObservedTick, "cancelled native work earns no fresh sample time");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled native work/historical sample persists");
            SimGame isolated = New(factory); c.That(!Known(isolated), "interleaved startup borrows no knowledge");
            c.Equal(Fix.FromInt(20), Entity(isolated, "survey.site").Get<IInventory>().Amount(steel), "another startup has independent actual stock");
            var known = new List<KnowledgeObservation>(); game.Seat(0).View.Feature<KnowledgeView>().CopyKnown(known);
            c.Equal(1, known.Count, "one native remembered subject, no sample/clock/registry duplication");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            string startup = FrameworkProofContent.Read("native-survey-startup.json");
            foreach (string mode in new[] { "cycle", "missing-entry", "home-dependency", "unseated", "collision", "fractional-byte", "negative-quantity" })
            {
                JsonValue json = Content.Json.Parse(startup);
                if (mode == "cycle") json.Get("tile_layer_setups")[0].Set("after", Content.Json.Parse("[\"start.machine\"]"));
                if (mode == "missing-entry") json.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("missing"));
                if (mode == "home-dependency") json.Get("entity_setups")[1].Set("after", Content.Json.Parse("[]"));
                if (mode == "unseated") json.Get("entity_setups")[1].Set("company", JsonValue.Number("7"));
                if (mode == "collision") json.Get("entity_setups")[1].Get("placement").Set("x", JsonValue.Number("16"));
                if (mode == "fractional-byte") json.Get("tile_layer_setups")[1].Set("value", JsonValue.Number("0.5"));
                if (mode == "negative-quantity") json.Get("tile_layer_setups")[0].Set("value", JsonValue.Number("-1"));
                c.Throws<ContentException>(() => New(Factory(startup: json.ToString())), "authored startup fails explicitly " + mode);
            }
            JsonValue world = Content.Json.Parse(FrameworkProofContent.Read("native-survey-world.json"));
            world.Get("scenarios")[0].Get("settings").Remove(WorldSetupModule.ModuleId);
            c.Throws<ContentException>(() => New(Factory(world: world.ToString())), "selected startup has no invented default profile");
            c.Throws<ContentException>(() => New(Factory(navigation: FrameworkProofContent.Read("native-survey-navigation.json").Replace("\"cost\":10", "\"cost\":0"))), "authored uniform movement has no zero-cost fallback");
        }
    }
}
