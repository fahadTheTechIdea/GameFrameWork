using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Player requests, ordinary commands, public immutable observations and bounded Update only.
    internal static class MandatoryGameModeTests
    {
        internal const string Case = "Mandatory modes: explicit survival configuration and resource-only physical play";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Process = new Code("transformation");
        private static JsonValue Read(string file) => Json.Parse(FrameworkProofContent.Read(file));
        private static GameFactory Factory(JsonValue world = null, JsonValue modes = null)
            => Colony.Factory(definitions: world?.ToString(), moduleDocuments: modes == null ? null :
                new Dictionary<string, string> { ["colony-game-modes.json"] = modes.ToString() });
        private static NewGameRequest Request() => new NewGameRequest { Scenario = "colony", Seed = 839 };
        private static SimGame New(GameFactory factory) => factory.NewGame(Request());
        private static IEnumerable<EntityView> Mine(SimGame game, string prototype = null)
            => game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0) && (prototype == null || e.Def == prototype));
        private static GameModePropertyView Property(IGameModeConfiguration configuration, string id)
            => configuration.Properties.Single(p => p.Property.Id == id);
        private static TransformationSnapshot Production(SimGame game, EntityId plant)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(plant, Process, out var result))
                throw new InvalidOperationException("missing public transformation facts");
            return result;
        }
        private static Fix Steel(SimGame game)
        {
            Fix amount = Fix.Zero;
            foreach (var entity in Mine(game))
            {
                var facets = new List<Facet>();
                if (!game.Seat(0).View.Inspect(entity.Id, facets)) throw new InvalidOperationException("missing owned inventory facts");
                foreach (var fact in facets) if (fact.Key == Storage.FacetStock && fact.Subject == "steel") amount += fact.Value;
            }
            return amount;
        }
        private sealed class Observations
        {
            internal bool Moving, Loaded, Productive;
            internal void Read(SimGame game)
            {
                Moving |= Mine(game).Any(e => e.State == new Code("state.moving"));
                var work = game.Seat(0).View.Feature<WorkView>();
                var jobs = new List<WorkJobView>(); work.CopyJobs(jobs);
                Loaded |= jobs.Any(j => j.IsHaul && j.Loaded > Fix.Zero);
                var orders = new List<OrderView>(); work.CopyOrders(orders);
                foreach (var order in orders)
                {
                    var nodes = new List<ActivityNodeView>();
                    if (work.TryCopyNodes(order.Id, nodes))
                        Productive |= nodes.Any(n => n.ProductiveSteps > 0 && !n.Progress.Equals(WorkProgress.Zero));
                }
            }
        }
        private static string Diagnostics(SimGame game, EntityId plant)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            var process = Production(game, plant);
            return "mode=" + game.GameMode.ModeId + "/clock=" + game.Clock.StepIndex + "/production=" +
                process.Phase + "/" + process.Progress + "/" + process.Blocker + "/queue=" + process.QueueCount +
                "/orders=" + string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)) +
                "/jobs=" + string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker));
        }
        private static void Update(SimGame game, EntityId plant, Observations observations, SimGame twin = null)
        {
            game.Update(); observations.Read(game);
            if (game.Fault != null) throw new InvalidOperationException(game.Fault + "/" + Diagnostics(game, plant));
            if (twin == null) return;
            twin.Update();
            if (twin.Fault != null || twin.Hash() != game.Hash())
                throw new InvalidOperationException("saved mode continuation differs/" + twin.Fault + "/" + Diagnostics(game, plant));
        }
        private static void Reach(SimGame game, EntityId plant, Observations observations, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !ready(); i++) Update(game, plant, observations, twin);
            if (!ready()) throw new InvalidOperationException(stage + "/" + Diagnostics(game, plant));
        }
        private static CommandResult Submit(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "resource-only player preview");
            c.Equal(before, game.Hash(), "preview neither changes configuration nor advances physical work");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary engine command accepted");
            if (game.Fault != null) throw new InvalidOperationException("command fault " + game.Fault);
            return result;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "required mode selection has direct semantic identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same mode identity codec, no new state store");
            var factory = Factory(); var game = New(factory); ulong initial = game.Hash();
            c.Equal("colony.survival", factory.Scenarios().Single().GameMode, "menu reads explicit authored mode");
            c.That(ReferenceEquals(game.GameMode, factory.ScenarioGameMode("colony")) &&
                ReferenceEquals(game.GameMode, factory.PreviewGameMode(Request())), "one immutable owner for empty request intent");
            c.Equal(initial, game.Hash(), "metadata/profile preview is pure");
            c.Equal(8, game.GameMode.Properties.Count, "canonical monetary schemas contain no unrelated proof properties");
            foreach (var property in game.GameMode.Properties)
            {
                c.That(property.Property.Id.StartsWith("business.", StringComparison.Ordinal) &&
                    property.Value.Kind == GameModeValueKind.Boolean && !property.Value.Boolean, "survival authors each monetary capability off");
                c.Equal(GameModeValueSource.Profile, property.Source, "survival false has explicit profile provenance");
            }
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && game.Seat(0).View.Feature<BusinessView>() == null,
                "available optional code is neither selected nor composed as a business feature");
            c.Equal(0L, game.Clock.StepIndex, "Initialize configuration does not spend game time");
            c.Throws<NotSupportedException>(() => ((IList<GameModePropertyView>)game.GameMode.Properties).Clear(),
                "retained configuration is immutable");

            var world = Read("colony.json");
            world.Get("scenarios")[0].Set("game_mode", JsonValue.Of("colony.resource-defaults"));
            var defaults = New(Factory(world: world));
            c.That(defaults.GameMode.Properties.All(p => !p.Value.Boolean && p.Source == GameModeValueSource.PropertyDefault),
                "empty authored profile uses actual JSON defaults, not loader defaults");
            world = Read("colony.json");
            world.Get("scenarios")[0].Get("mode_overrides").Set("business.money.enabled", JsonValue.Of(false));
            var scenario = New(Factory(world: world));
            c.Equal(GameModeValueSource.Scenario, Property(scenario.GameMode, "business.money.enabled").Source, "explicit equal scenario override remains intent");
            var request = Request();
            request.ModeOverrides.Add(new GameModeAssignment(new Code("business.money.enabled"), GameModeValue.FromBoolean(false)));
            var preview = factory.PreviewGameMode(request); var requested = factory.NewGame(request);
            c.Equal(GameModeValueSource.Request, Property(requested.GameMode, "business.money.enabled").Source, "explicit request provenance");
            c.Equal(Property(preview, "business.money.enabled").Value, Property(requested.GameMode, "business.money.enabled").Value, "preview/creation parity");
            c.That(initial != requested.Hash(), "equal value with different intent participates in authoritative hash");
            ulong captured = requested.Hash(); request.ModeOverrides.Clear();
            request.ModeOverrides.Add(new GameModeAssignment(new Code("business.money.enabled"), GameModeValue.FromBoolean(true)));
            c.Equal(captured, requested.Hash(), "caller list mutation cannot change captured session");
            c.That(!Property(preview, "business.money.enabled").Value.Boolean, "retained request preview remains immutable");
            var requestTwin = factory.Load(requested.Save());
            c.Equal(captured, requestTwin.Hash(), "captured request identity saved without a second setting authority");
            c.Equal(GameModeValueSource.Request, Property(requestTwin.GameMode, "business.money.enabled").Source, "saved request provenance");
            Invalid(c);

            var emptyFactory = NativeSurveyTests.Factory();
            var empty = emptyFactory.NewGame(new NewGameRequest { Scenario = "native-survey", Seed = 839 });
            c.Equal("native-survey-world.resource-only", empty.GameMode.ModeId, "native nonbusiness game still requires an authored profile");
            c.Equal(0, empty.GameMode.Properties.Count, "no schema means an explicit empty configuration");
            c.That(!empty.GameMode.TryGet(new Code("business.money.enabled"), out var unavailable) &&
                unavailable.Kind == GameModeValueKind.Unavailable, "absent schema is unavailable, not invented false");
            c.Throws<InvalidOperationException>(() => { _ = unavailable.Boolean; }, "unavailable cannot masquerade as a disabled setting");
            c.Equal(empty.Hash(), emptyFactory.Load(empty.Save()).Hash(), "explicit empty configuration participates in saves");

            var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            EntityId plant = Mine(game, "factory").Single().Id, depot = Mine(game, "depot").Single().Id;
            var observations = new Observations();
            c.Equal(Fix.FromInt(50), Steel(game), "actual resource-only opening stock");
            Submit(c, game, new EnqueueTransformationCommand { Capability = Process, Transformer = plant, Transformation = "build_truck", Count = 2, Home = depot });
            c.Equal(2, Mine(game, "truck").Count(), "manufactured machines do not spawn at enqueue");
            var site = Submit(c, game, new ConstructCommand { Def = "tower", Priority = 50,
                Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
            c.That(Mine(game, "site:tower").Any(e => e.Id == site.Entity) && !Mine(game, "tower").Any(), "ordinary construction creates only physical project");
            c.Equal(Codes.NotOwned, game.Seat(1).Preview(new CancelConstructionCommand { Site = site.Entity }), "money-off never bypasses company ownership");
            Reach(game, plant, observations, () => Production(game, plant).Progress.WholeTicks > 0 && Mine(game, "truck").Count() == 2,
                "partial resource-only production");
            c.That(Production(game, plant).Progress.WholeTicks < Production(game, plant).Required &&
                Production(game, plant).QueueCount == 2, "real partial first batch with queued second machine");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save during real travel/haul/production");
            long paused = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause();
            for (int i = 0; i < 3; i++) Update(game, plant, observations, twin);
            c.Equal(paused, game.Clock.StepIndex, "sole pause freezes all physical work");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, plant, observations, () => Mine(game, "truck").Count() == 3 && Production(game, plant).QueueCount == 1,
                "first physical manufactured machine", twin);
            Reach(game, plant, observations, () => Mine(game, "truck").Count() == 4 && Production(game, plant).QueueCount == 0 &&
                Mine(game, "tower").Count() == 1, "second sequential machine and staged construction", twin);
            c.That(observations.Moving && observations.Loaded && observations.Productive, "physical travel, loaded hauling and positive machine work remain authoritative");
            c.Equal(Fix.Zero, Steel(game), "two ten-steel machines and thirty-steel tower consume actual fifty steel");
            Submit(c, game, new EnqueueTransformationCommand { Capability = Process, Transformer = plant, Transformation = "build_truck", Home = depot });
            twin = factory.Load(game.Save());
            for (int i = 0; i < 20; i++) Update(game, plant, observations, twin);
            c.That(Production(game, plant).QueueCount == 1 && Production(game, plant).Progress.Equals(WorkProgress.Zero) &&
                !Production(game, plant).Blocker.IsNone, "physical shortage retains typed blocked intent even with all monetary gates off");
            c.Equal(4, Mine(game, "truck").Count(), "no instant/free output at shortage");
            c.Equal(Fix.Zero, Steel(game), "no invented resource default");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "shortage plus mandatory mode roundtrip");
            c.Equal(isolatedHash, isolated.Hash(), "shared profiles do not share live sessions");
            c.Equal(initial, New(factory).Hash(), "physical updates cannot mutate shared authored mode or startup state");
            c.That(game.Fault == null && twin.Fault == null && game.Clock.StepIndex > 0, "healthy timed engine continuation without business module");
        }
        private static void Invalid(Check c)
        {
            foreach (string error in new[] { "missing-mode", "null-mode", "empty-mode", "unknown-mode", "missing-overrides", "null-overrides", "array-overrides" })
            {
                var world = Read("colony.json"); var scenario = world.Get("scenarios")[0];
                if (error == "missing-mode") scenario.Remove("game_mode");
                if (error == "null-mode") scenario.Set("game_mode", JsonValue.Null());
                if (error == "empty-mode") scenario.Set("game_mode", JsonValue.Of(""));
                if (error == "unknown-mode") scenario.Set("game_mode", JsonValue.Of("missing.mode"));
                if (error == "missing-overrides") scenario.Remove("mode_overrides");
                if (error == "null-overrides") scenario.Set("mode_overrides", JsonValue.Null());
                if (error == "array-overrides") scenario.Set("mode_overrides", JsonValue.NewArray());
                c.Throws<ContentException>(() => Factory(world: world).PreviewGameMode(Request()), "strict configuration preview refuses " + error);
                c.Throws<ContentException>(() => New(Factory(world: world)), "strict creation refuses " + error);
                if (error != "unknown-mode")
                    c.Throws<ContentException>(() => Factory(world: world).Scenarios(), "strict menu metadata refuses " + error);
            }
            int composed = 0;
            var native = GameModeConfigurationTests.Factory(compose: _ => composed++);
            var request = new NewGameRequest { Scenario = "survival", Seed = 839 };
            request.ModeOverrides.Add(new GameModeAssignment(new Code("business.money.enabled"), GameModeValue.FromBoolean(true)));
            c.Throws<ContentException>(() => native.PreviewGameMode(request), "enabled flag requires explicitly selected owner, not merely registered code");
            c.Throws<ContentException>(() => native.NewGame(request), "enabled unsupported flag refuses before composition");
            c.Equal(0, composed, "invalid capability invokes no gameplay composition");
            var modes = Read("colony-game-modes.json"); modes.Get("game_modes")[0].Get("properties").Set("unknown.property", JsonValue.Of(false));
            c.Throws<ContentException>(() => New(Factory(modes: modes)), "unknown authored property never becomes an invented default");
            modes = Read("colony-game-modes.json"); modes.Get("game_modes")[0].Get("properties").Set("business.production_income.enabled", JsonValue.Of(true));
            c.Throws<ContentException>(() => New(Factory(modes: modes)), "income-on never auto-enables missing money or values");
            c.Throws<ArgumentNullException>(() => GameModeConfiguration.ValidateSelectedCapabilities(null, null), "no nullable capability-validation bypass");
            c.Throws<ArgumentNullException>(() => GameModeConfiguration.SyncIdentity(null, null), "no nullable identity-protocol bypass");
        }
    }
}
