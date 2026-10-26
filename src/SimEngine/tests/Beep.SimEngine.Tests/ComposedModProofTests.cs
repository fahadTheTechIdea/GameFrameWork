using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Player inputs and detached facts only. Every extension delegates to its
    // original contract/declaration/runtime owner in the engine or content module.
    internal static class ComposedModProofTests
    {
        internal const string Case = "External colony mod: worker project bidirectional endpoint signal and incident response share native execution";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(Action<JsonValue> edit = null, string missing = null)
        {
            var modules = new GameModule[] { new IndependentTileSensorModule(), new WorldSetupModule(), new OperationsModule(),
                new CompanyKnowledgeModule(), new KnowledgeVisibilityModule(), new MotionModule(), new TileNavigationModule(),
                new TileConditionsModule(), new ContentAccessModule(), new InventoryModule(), new WorkModule(), new LogisticsModule(),
                new ConstructionModule(), new InventorySignalsModule(), new IncidentResponseModule(), new ColonyExtensionProofModule() };
            var definition = new GameDefinition("colony-extension", modules.Where(m => m.Id != missing).ToArray());
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("colony-extension-master.json"))
                .Add("open-access.json", FrameworkProofContent.Access);
            foreach (string file in new[] { "independent-tile-sensor-world.json", "independent-tile-sensor-startup.json",
                "independent-tile-navigation-startup.json", "independent-tile-navigation.json", "independent-tile-navigation-conditions.json",
                "independent-tile-sensor-visibility.json", "independent-tile-work.json", "work-retry.json", "independent-tile-construction.json" })
                files.Add(file, FrameworkProofContent.Read(file));
            var document = Json.Parse(FrameworkProofContent.Read("colony-extension.json")); edit?.Invoke(document);
            files.Add("colony-extension.json", document.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "colony-extension", Seed = 857, Seats = { Controller.Human, Controller.Human } });
        private static IEnumerable<EntityView> Mine(SimGame game) => game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0));
        internal static EntityView Entity(SimGame game, string prototype) => Mine(game).Single(e => e.Def == prototype);
        internal static Fix Stock(SimGame game, EntityId? endpoint = null)
        {
            Fix sum = Fix.Zero;
            foreach (var entity in Mine(game).Where(e => !endpoint.HasValue || e.Id == endpoint.Value)) {
                var facets = new List<Facet>();
                if (!game.Seat(0).View.Inspect(entity.Id, facets)) throw new InvalidOperationException("missing owned endpoint facts");
                foreach (var facet in facets) if (facet.Key == Storage.FacetStock && facet.Subject == "steel") sum += facet.Value;
            }
            return sum;
        }
        internal static List<WorkJobView> Jobs(SimGame game)
        { var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
        internal static List<ActivityNodeView> Nodes(SimGame game, int order)
        {
            var rows = new List<ActivityNodeView>();
            if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, rows)) throw new InvalidOperationException("missing native graph " + order);
            return rows;
        }
        internal static IncidentLifecycleView Incident(SimGame game)
        {
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(Entity(game, "mod.endpoint").Id, new Code("failure"), out var state))
                throw new InvalidOperationException("missing actual independently authored incident");
            return state;
        }
        internal static AutomationSignalSnapshot Signal(SimGame game, string signal = "mod.stored")
        {
            Code refusal = game.Seat(0).View.Feature<OperationsView>().ReadSignal(Entity(game, "mod.endpoint").Id, new Code("stock"), signal, out var state);
            if (!refusal.IsNone) throw new InvalidOperationException("actual mod signal refused " + refusal);
            return state;
        }
        internal static bool Home(SimGame game) => game.Seat(0).View.Feature<WorkView>().ReadHome(Entity(game, "mod.machine").Id,
            out var home).IsNone && home.State == WorkerHomeState.AtHome;
        internal static string Diagnostics(SimGame game) => "step=" + game.Clock.StepIndex + "/total=" + Stock(game) +
            "/endpoint=" + Stock(game, Entity(game, "mod.endpoint").Id) + "/incident=" + Incident(game).Snapshot.Active + "/" + Incident(game).Blocker +
            "/jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker));
        internal static void Observe(SimGame game)
        {
            Fix total = Stock(game);
            if (total != Fix.FromInt(12) && total != Fix.FromInt(4) && total != Fix.FromInt(2))
                throw new InvalidOperationException("material creation/loss outside authored native consumption/" + Diagnostics(game));
            var value = Signal(game);
            if (!value.Value.HasValue || value.Value.Value != Stock(game, Entity(game, "mod.endpoint").Id))
                throw new InvalidOperationException("signal is not actual endpoint stock/" + Diagnostics(game));
            var occupied = new HashSet<TileCoord>();
            foreach (var entity in Mine(game)) if (!occupied.Add(((TileSpatialSnapshot)entity.Spatial).Tile))
                throw new InvalidOperationException("physical overlap/" + Diagnostics(game));
        }
        private static void Update(SimGame game, SimGame twin = null, Action observe = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("fault " + game.Fault + "/" + twin?.Fault);
            Observe(game); observe?.Invoke();
            if (twin != null && game.Hash() != twin.Hash()) throw new InvalidOperationException("saved mod twins differ/" + Diagnostics(game));
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null, Action observe = null)
        {
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < 360 && timer.Elapsed.TotalSeconds < 20 && !done(); i++) Update(game, twin, observe);
            if (!done()) throw new InvalidOperationException(stage + "/" + Diagnostics(game));
        }
        private static CommandResult Submit(Check c, SimGame game, Command command, Code expected = default)
        {
            ulong hash = game.Hash(); c.Equal(expected, game.Seat(0).Preview(command), "typed preview admission");
            c.Equal(hash, game.Hash(), "preview changes no module/stock/work/clock state");
            var result = TestKit.Order(game, 0, command); c.Equal(expected, result.Refusal, "normal command parity");
            if (expected.IsNone) c.That(result.Accepted, "ordinary player command accepted");
            if (game.Fault != null) throw new InvalidOperationException("command fault " + game.Fault);
            Observe(game); return result;
        }
        private static void Run(Check c)
        {
            foreach (string invalid in new[] { "worker", "inventory", "signal", "project", "cycle", "response" })
                c.Throws<ContentException>(() => New(Factory(root => {
                    if (invalid == "worker") root.Get("entities")[1].Get("components").Get("mod_worker").Set("roles", Json.Parse("[\"missing.role\",\"haul\"]"));
                    if (invalid == "inventory") root.Get("entities")[0].Get("components").Get("mod_stock").Set("resource", JsonValue.Of("missing.resource"));
                    if (invalid == "signal") root.Get("entities")[0].Get("components").Get("mod_signal").Set("inventory", JsonValue.Of("storage"));
                    if (invalid == "project") root.Get("entities")[3].Get("components").Get("mod_construction").Set("site", JsonValue.Of("mod.endpoint"));
                    if (invalid == "cycle") root.Get("mod_activities")[0].Get("nodes")[0].Set("after", Json.Parse("[\"build\"]"));
                    if (invalid == "response") root.Get("mod_incident_workflows")[0].Set("response", JsonValue.Of("missing.response"));
                })), "module addition refuses invalid authored " + invalid);
            c.Throws<InvalidOperationException>(() => New(Factory(missing: InventorySignalsModule.ModuleId)), "missing declared capability module refuses composition");
            var factory = Factory(); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 1000); var recording = game.RecordReplay(limits);
            var endpoint = Entity(game, "mod.endpoint").Id; var worker = Entity(game, "mod.machine").Id; var depot = Entity(game, "sensor.depot").Id;
            var retainedWorker = Entity(game, "mod.machine");
            c.That(game.Modules.Contains("proof.colony-extension") && !game.Modules.Contains(TycoonModule.ModuleId), "actual external content module without business");
            c.Equal(Fix.FromInt(12), Stock(game), "all mod work uses original finite depot stock");
            c.Equal(Fix.Zero, Signal(game).Value.Value, "new signal reads empty new endpoint, not fabricated initial state");
            c.Equal(Fix.Zero, Signal(game, "mod.stock-ready").Value.Value, "typed readiness false before physical receipt");
            c.That(game.Seat(0).View.TryFindResource("steel", out var steel), "public actual resource identity");
            ulong pure = game.Hash(); Signal(game); Incident(game);
            c.Equal(pure, game.Hash(), "signal and incident projections are pure");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<OperationsView>().ReadSignal(endpoint, new Code("stock"), "mod.stored", out _), "foreign signal facts do not leak endpoint contents");
            c.That(!game.Seat(1).View.Feature<IncidentsView>().TryGetLifecycle(endpoint, new Code("failure"), out _), "foreign incident view withholds actual episode");
            void Reserve(bool enabled) => Submit(c, game, new ConfigureStockPolicyCommand { Store = depot, Resource = steel,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(enabled, Fix.FromInt(4), Fix.FromInt(4), Fix.FromInt(4), 10, LogisticsReserveAccess.Protected)) });
            Reserve(true);
            Submit(c, game, new HaulResourcesCommand { Source = depot, Destination = endpoint, Worker = worker, Resource = steel, Ceiling = Fix.FromInt(8), Priority = 100 });
            Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero && j.Loaded < Fix.FromInt(8)), "new worker physically loads endpoint supply");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "new worker/endpoint claims save during partial handling");
            Until(game, () => Stock(game, endpoint) == Fix.FromInt(8) && Home(game), "new endpoint receives actual physical eight", twin);
            c.Equal(Fix.FromInt(8), Signal(game).Value.Value, "independent signal definition reads original custom inventory implementation");
            c.Equal(Fix.One, Signal(game, "mod.stock-ready").Value.Value, "readiness follows actual inlet receipt");
            var retainedSignal = Signal(game); twin = factory.Load(game.Save());
            c.Equal(Signal(game).Value.Value, Signal(twin).Value.Value, "restored new signal observes restored endpoint stock");
            var built = Submit(c, game, new ConstructCommand { Def = "mod.facility", Priority = 80,
                Placement = Json.Parse("{\"space\":\"sensor.tiles\",\"x\":18,\"y\":18}") });
            c.That(game.Seat(0).View.Feature<ConstructionView>().TryReadSite(built.Entity, out var site), "independent project/site metadata binds actual native order");
            twin = factory.Load(game.Save()); bool exported = false, travelled = false;
            void BuildFacts() {
                exported |= Jobs(game).Any(j => j.IsHaul && j.Source == endpoint && j.Loaded > Fix.Zero);
                travelled |= Entity(game, "mod.machine").State == new Code("moving");
            }
            Until(game, () => Nodes(game, site.Order).Single(n => n.Id == "build").Progress.WholeTicks > 0,
                "new endpoint exports real project supply and new worker earns work", twin, BuildFacts);
            var node = Nodes(game, site.Order).Single(n => n.Id == "build");
            c.That(exported && travelled && node.Worker == worker && node.Progress.WholeTicks < node.RequiredTicks, "same external worker physically transports and builds via original dispatch");
            c.Equal(new WorkerRoleId("sensor.fabricator"), node.SelectedRole, "authored mod role drives real productive work");
            c.Equal(new Code("working"), Entity(game, "mod.machine").State, "native entity state uses independent worker metadata");
            c.Equal(Fix.FromInt(4), Stock(game), "native project consumed exact eight once");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial independent project and worker persist");
            Until(game, () => Mine(game).Count(e => e.Def == "mod.facility") == 1 && Home(game), "physical project completion and worker home", twin);
            c.That(!Mine(game).Any(e => e.Def == "mod.site"), "native completion retires independently declared site");
            c.Equal(Fix.Zero, Signal(game).Value.Value, "same endpoint now empty after real outlet export");
            Submit(c, game, new SetWorkerHeldCommand { Worker = worker, Held = true });
            Submit(c, game, new RaiseIncidentCommand { Target = endpoint, Source = new Code("failure"), Kind = new Code("unsupported.kind") }, Codes.InvalidArgument);
            c.That(!Incident(game).Snapshot.Active, "unsupported typed incident request changes no episode");
            Submit(c, game, new RaiseIncidentCommand { Target = endpoint, Source = new Code("failure"), Kind = new Code("mod.equipment-failure") });
            twin = factory.Load(game.Save());
            Until(game, () => Nodes(game, Incident(game).OrderId).Single(n => n.Id == "repair").Blocker == WorkResourceCodes.Stock,
                "independent incident DAG reaches actual missing-material blocker", twin);
            c.Equal(new Code("stopped"), Entity(game, "mod.endpoint").State, "incident projects native authored stopped state");
            c.Equal(Fix.FromInt(4), Stock(game), "blocked incident does not spend protected depot reserve");
            c.Equal(0L, Nodes(game, Incident(game).OrderId).Single(n => n.Id == "repair").Progress.WholeTicks, "missing stock gives no repair work");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "typed blocked independent episode saves");
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex;
            Update(game, twin); c.Equal(paused, game.Clock.StepIndex, "sole clock pause freezes incident and supplied work");
            game.Clock.Resume(); twin.Clock.Resume(); Reserve(false);
            Submit(c, game, new SetWorkerHeldCommand { Worker = worker, Held = false });
            twin = factory.Load(game.Save()); bool responseCargo = false;
            Until(game, () => Nodes(game, Incident(game).OrderId).Single(n => n.Id == "repair").Progress.WholeTicks > 0,
                "automatic physical response resupply and earned repair", twin,
                () => responseCargo |= Jobs(game).Any(j => j.IsHaul && j.Destination == endpoint && j.Loaded > Fix.Zero));
            var repair = Nodes(game, Incident(game).OrderId).Single(n => n.Id == "repair");
            c.That(responseCargo && repair.Worker == worker && repair.Progress.WholeTicks < repair.RequiredTicks, "new response uses physical delivered spares and same native worker");
            c.Equal(Fix.FromInt(2), Stock(game), "original requirement owner consumes exact two repair spares");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial independently authored response persists");
            Until(game, () => !Incident(game).Snapshot.Active, "independent response completion effect", twin);
            c.That(Incident(game).Snapshot.Episode == 1 && Incident(game).Snapshot.ResolvedTick > Incident(game).Snapshot.RaisedTick,
                "native incident lifetime ends only after positive game-time response");
            c.Equal(Fix.FromInt(2), Stock(game), "final twelve minus eight build minus two repair, no creation or loss");
            c.Equal(Fix.Zero, Signal(game).Value.Value, "new signal remains bound to actual consumed endpoint stock");
            c.Equal(Fix.FromInt(8), retainedSignal.Value.Value, "retained signal snapshot remains detached");
            c.Equal(new TileCoord(4, 16), ((TileSpatialSnapshot)retainedWorker.Spatial).Tile, "retained opening worker view remains detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "every new mod addition saves at final state");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success, "external mod public command replay/" + replay.Failure + "/" + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "all composed additions replay to identical hash");
            c.Equal(isolatedHash, isolated.Hash(), "another session is not mutated by mod play, load or replay");
        }
    }
}
