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
    public static class DeclaredPlanCatalogTests
    {
        public const string Case = "Declared plan catalog: JSON extension, physical work and captured ownership";
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(string declarations = null, bool selected = true)
        {
            var definition = new GameDefinition("declared-plan-contract", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(),
                new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule(), new InfrastructureModule(),
                new DeclaredPlanModule() });
            JsonValue world = Content.Json.Parse(FrameworkProofContent.Read("declared-plan-world.json"));
            if (!selected)
            {
                JsonValue modules = world.Get("scenarios")[0].Get("modules"), active = JsonValue.NewArray();
                for (int i = 0; i < modules.Count; i++) if (modules[i].Text != DeclaredPlanModule.ModuleId) active.Add(modules[i]);
                world.Get("scenarios")[0].Set("modules", active);
            }
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("declared-plan-master.json"))
                .Add("declared-plan-world.json", world.ToString()).Add("open-access.json", FrameworkProofContent.Access)
                .Add("declared-plans.json", declarations ?? FrameworkProofContent.Read("declared-plans.json"));
            foreach (string name in new[] { "declared-plan-startup.json", "declared-plan-work.json",
                "authored-segment-work.json", "authored-segment-infrastructure.json", "native-survey-work.json",
                "native-survey-navigation.json", "native-survey-knowledge.json", "work-retry.json" })
                files.Add(name, FrameworkProofContent.Read(name));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 755 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Site(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "survey.site");
        private static Fix Stock(SimGame game)
        {
            Fix stock = Fix.Zero; ResourceId steel = game.World.Content.ResourceId("steel");
            foreach (Entity e in game.World.Entities.All) if (e.Get<IInventory>() is IInventory inventory) stock += inventory.Amount(steel);
            return stock;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("declared plans time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " nodes=" +
                string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => o.PlanKey + "/" + n.Id + "/" + n.State + "/" + n.JobId + "/" + n.Blocker))));
        }
        private static List<PlanView> Plans(SimGame game, int seat = 0)
        { var result = new List<PlanView>(); game.Seat(seat).View.Feature<WorkView>().CopyPlans(result); return result; }
        private sealed class Undeclared : IActivityDefinition
        {
            private readonly IActivityDefinition _source;
            public Undeclared(IActivityDefinition source) { _source = source; }
            public string PlanKey => _source.PlanKey;
            public IActivityGraph Graph => _source.Graph;
            public bool AutomaticSupply => _source.AutomaticSupply;
            public IReadOnlyList<IContentDefinition> DisclosureContent => _source.DisclosureContent;
            public string OnComplete => _source.OnComplete;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "captured contract catalog identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same saved board/plan/job bytes");
            var factory = Factory(); var game = New(factory); var board = Board(game);
            var catalog = game.World.Services.Get<IActivityCatalog>();
            var source = game.World.Services.Get<DeclaredPlanSource>();
            var definition = game.World.Content.Get<DeclaredPlanDef>("declared.deployment");
            c.That(ReferenceEquals(catalog, board), "catalog aliases sole board authority");
            c.Equal(1, source.Reads, "declared service aliases captured once before initialization");
            c.That(ReferenceEquals(source, game.World.Services.Get<IDeclaredPlanSource>()), "module aliases share same immutable source");
            c.Equal(string.Join("|", catalog.PlanKeys.OrderBy(x => x, StringComparer.Ordinal)), string.Join("|", catalog.PlanKeys), "canonical ordinal discovery");
            c.Equal(catalog.PlanKeys.Count, catalog.PlanKeys.Distinct().Count(), "no duplicate keys");
            c.Throws<NotSupportedException>(() => ((IList<string>)catalog.PlanKeys).Add("invented"), "captured keys cannot mutate");
            c.That(ReferenceEquals(definition, catalog.ResolvePlan("extension:deployment")), "arbitrary mod key resolves actual JSON definition without prefix edits");
            c.That(ReferenceEquals(definition.Workflow, game.World.Content.Get<ActivityDef>("declared.profile")) &&
                ReferenceEquals(definition.Graph, definition.Workflow.Graph), "mod binds same selected activity and graph, no parser/compiler copy");
            c.That(ReferenceEquals(catalog.ResolvePlan("line:segment.road-line"), game.World.Content.Get<ILineDefinition>("segment.road-line")), "native infrastructure declares same source contract");
            c.That(ReferenceEquals(catalog.ResolvePlan("activity:declared.profile"), definition.Workflow), "ordinary activity aliases remain discoverable");
            c.That(catalog.ResolvePlan("unknown:missing") == null && catalog.ResolvePlan(null) == null, "unknown key has no fallback");
            ulong hash = game.Hash();
            for (int i = 0; i < 20; i++) c.That(ReferenceEquals(definition, catalog.ResolvePlan(definition.PlanKey)), "lookup uses captured alias");
            c.Equal(1, source.Reads, "lookups never reinvoke provider");
            c.Equal(hash, game.Hash(), "discovery is pure");
            c.Throws<InvalidOperationException>(() => board.Post(new Undeclared(definition), new PlayerId(0), Site(game).Id, 60), "same key/graph cannot authorize unregistered instance");
            c.Equal(hash, game.Hash(), "refused undeclared post allocates no IDs/jobs/claims");
            c.Equal(0, board.Orders.Count, "no hidden order from rejected post");
            c.Equal(Fix.FromInt(60), Stock(game), "native setup authored actual site and machine stock");
            c.That(!game.World.Services.Has<IEconomy>(), "mod work needs no business module");
            var intent = new PlanIntent(definition.PlanKey, ActivityWorkerSelection.CreateArguments(Site(game).Id, ActivityWorkerSelection.Unbound), "mod", 75, true);
            var submit = new SubmitPlanCommand { Intent = intent };
            c.Equal(Code.None, game.Seat(0).Preview(submit), "same public engine plan preview admits declared contract");
            c.Equal(hash, game.Hash(), "mod plan preview pure");
            var result = TestKit.Order(game, 0, submit); c.That(result.Accepted, "ordinary public command submits mod plan");
            long planId = result.Arg0; c.Equal(PlanState.Pending, Plans(game).Single(p => p.Id == planId).State, "paused intent earns no work");
            c.Equal(0, board.Orders.Count, "no execution beneath paused mod intent");
            c.Equal(0, Plans(game, 1).Count, "foreign company sees no private mod intent");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending mod intent restores through captured definition");
            c.Equal(1, twin.World.Services.Get<DeclaredPlanSource>().Reads, "restore captures declarations once in its session");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = planId, Group = "mod", Priority = 75, Paused = false }).Accepted, "player promotes through existing policy command");
            Reach(game, () => Plans(game).Single(p => p.Id == planId).State == PlanState.Active, "mod activation");
            Order order = board.Order(Plans(game).Single(p => p.Id == planId).Order);
            c.That(ReferenceEquals(order.Plan, definition) && order.PlanKey == "extension:deployment", "same declared definition owns actual execution");
            Reach(game, () => order.Node("prepare").Progress.WholeTicks > 0, "physical approach/tool-backed mod work");
            c.Equal(Fix.FromInt(60), Stock(game), "actual tool hold cannot consume stock");
            c.That(Site(game).Get<IInventory>().Reservations.Count > 0, "same actual endpoint owns tool promise");
            c.Equal(0L, order.Node("install").Progress.WholeTicks, "authored dependency cannot skip physical preparation");
            var views = new List<ActivityNodeView>(); c.That(game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, views), "private native node view supports mod key");
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, new List<ActivityNodeView>()), "foreign company denied actual node diagnostics");
            var frozen = views.Single(n => n.Id == "prepare"); var progress = frozen.Progress;
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "active mod key/graph/claims restore without custom codec");
            Reach(game, () => order.State == OrderState.Done, "mod staged completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved mod workflow continues identically");
            c.Equal(progress, frozen.Progress, "old detached node view remains immutable");
            c.Equal(Fix.FromInt(52), Stock(game), "existing coordinator consumes actual eight once");
            c.Equal(PlanState.Done, Plans(game).Single(p => p.Id == planId).State, "same persistent board settles mod intent");
            c.Equal(0, Site(game).Get<IInventory>().Reservations.Count, "physical completion releases actual tool receipt");
            c.Equal(1, source.Reads, "commands/views/saves/updates never resolve providers again");

            var line = new LayLineCommand { Line = "segment.road-line" };
            line.Tiles.AddRange(new[] { new TileCoord(20, 16), new TileCoord(21, 16), new TileCoord(22, 16) });
            c.That(TestKit.Order(game, 0, line).Accepted, "native line activation uses same captured lookup and actual bound graph");
            Order corridor = board.Orders.Last();
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "bound native cursor reconstructs through declared source");
            Reach(game, () => corridor.State == OrderState.Done, "native declared line", twin);
            c.Equal(game.Hash(), twin.Hash(), "native target binding continues saved execution identically");
            c.Equal(Fix.FromInt(46), Stock(game), "native line retains its actual worker resource coordinator");
            foreach (TileCoord tile in line.Tiles) c.Equal((byte)1, game.World.Services.Get<TileMap>().Layer<ByteLayer>("segment.road")[tile], "only earned native tile exists");

            result = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = intent }); c.That(result.Accepted, "completed mod intent can be repeated as fresh plan");
            long cancelId = result.Arg0;
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = cancelId, Group = "mod", Priority = 75, Paused = false }).Accepted, "repeat activates normal physical work");
            Reach(game, () => board.Orders.Any(o => o.PlanKey == definition.PlanKey && o.IsActive && o.Node("prepare").Progress.WholeTicks > 0), "repeat tool preparation");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { cancelId }) }).Accepted, "normal public cancellation");
            c.Equal(PlanState.Cancelled, Plans(game).Single(p => p.Id == cancelId).State, "same board settles cancelled mod intent");
            c.Equal(Fix.FromInt(46), Stock(game), "cancellation invents no refund/consumption");
            c.Equal(0, Site(game).Get<IInventory>().Reservations.Count, "cancel releases actual held tool");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled mod history saves through same lookup");
            c.Equal(Fix.FromInt(60), Stock(New(factory)), "new session has isolated authored stock");

            string duplicate = "{\"declared_plans\":[{\"id\":\"first\",\"plan_key\":\"extension:deployment\",\"activity\":\"declared.profile\"},{\"id\":\"second\",\"plan_key\":\"extension:deployment\",\"activity\":\"declared.profile\"}]}";
            c.Throws<ContentException>(() => New(Factory(duplicate)), "different authored definitions cannot own same plan key");
            string collision = "{\"declared_plans\":[{\"id\":\"collision\",\"plan_key\":\"line:segment.road-line\",\"activity\":\"declared.profile\"}]}";
            c.Throws<ContentException>(() => New(Factory(collision)), "native/mod key collision fails before Initialize");
            string invalid = "{\"declared_plans\":[{\"id\":\"missing\",\"plan_key\":\"extension:bad\",\"activity\":\"unknown\"}]}";
            c.Throws<ContentException>(() => New(Factory(invalid)), "missing authored workflow fails");
            var unused = New(Factory(invalid, selected: false));
            c.That(unused.World.Services.Get<IActivityCatalog>().ResolvePlan("extension:bad") == null &&
                !unused.World.Services.Has<DeclaredPlanSource>(), "unselected module has no resolver/source/read fallback");
        }
    }
}

