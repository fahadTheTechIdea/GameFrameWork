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
    public static class DockRequirementTests
    {
        public const string Case = "Dock requirements: authored admission, native queue and saved physical work";
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(string dockContent = null)
        {
            var definition = new GameDefinition("native-dock", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(),
                new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule() });
            JsonValue world = Content.Json.Parse(FrameworkProofContent.Read("native-survey-world.json"));
            world.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("dock.start"));
            JsonValue master = Content.Json.Parse(FrameworkProofContent.Read("native-survey-master.json"));
            for (int i = 0; i < master.Get("modules").Count; i++)
            {
                var module = master.Get("modules")[i];
                if (module.Get("id").Text == "sim.work") module.Get("documents").Add(JsonValue.Of("native-dock-work.json"));
                if (module.Get("id").Text == "sim.world-setup") module.Get("documents").Add(JsonValue.Of("native-dock-startup.json"));
            }
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString())
                .Add("native-survey-world.json", world.ToString())
                .Add("native-dock-work.json", dockContent ?? FrameworkProofContent.Read("native-dock-work.json"))
                .Add("native-dock-startup.json", FrameworkProofContent.Read("native-dock-startup.json"))
                .Add("native-survey-startup.json", FrameworkProofContent.Read("native-survey-startup.json"))
                .Add("native-survey-navigation.json", FrameworkProofContent.Read("native-survey-navigation.json"))
                .Add("native-survey-work.json", FrameworkProofContent.Read("native-survey-work.json"))
                .Add("native-survey-knowledge.json", FrameworkProofContent.Read("native-survey-knowledge.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 727 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Site(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "dock.site");
        private static Entity[] Machines(SimGame game) => game.World.Entities.All.Where(e => e.Def.Id == "survey.machine").OrderBy(e => e.Id.Value).ToArray();
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("dock time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " nodes=" +
                string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => o.Id + "/" + n.Id + "/" + n.State + "/" + n.Blocker + "/" + n.Progress.WholeTicks))) +
                " jobs=" + string.Join(";", Board(game).Jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.DestinationDockSlot)));
        }
        private static void Hold(Check c, SimGame game, Entity machine, bool held)
            => c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = held }).Accepted, "normal machine availability command");
        private static Order Post(Check c, SimGame game, string plan)
        {
            var command = new PostOrderCommand { Plan = plan, Target = Site(game).Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal dock order intent preview");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal dock order accepted");
            return Board(game).Order((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "authored dock admission identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no duplicate persistent dock layout");
            var factory = Factory(); var game = New(factory); Entity site = Site(game); Entity[] machines = Machines(game);
            ResourceId steel = game.World.Content.ResourceId("steel");
            c.Equal(4, game.World.Entities.All.Count, "only actual authored startup entities");
            c.That(ReferenceEquals(Board(game), game.World.Services.Get<IWorkDockAdmission>()), "same sole work/dock owner");
            c.That(!game.World.Services.Has<IEconomy>(), "physical dock work needs no business provider");
            c.That(game.World.Content.Get<DockWorkRequirementDef>("dock.admission").AuthoringPath.Contains("native-dock-work.json"), "actual master-listed dock leaf");
            c.That(ReferenceEquals(game.World.Content.Get<WorkRequirementDef>("dock.admission"), game.World.Content.Get<DockWorkRequirementDef>("dock.admission")), "same contract/content definition");
            foreach (Entity machine in machines) Hold(c, game, machine, true);
            Hold(c, game, machines[0], false); Hold(c, game, machines[1], false);
            var requirements = new List<WorkRequirementView>(); var candidates = new List<WorkWorkerCandidate>();
            ulong hash = game.Hash();
            c.Equal(Code.None, View(game).PreviewOrderRequirements("dock.parallel", site.Id, requirements), "shared all-expression preview");
            c.That(requirements.Count(r => r.Requirement == "dock.admission" && r.Result.Status == WorkRequirementStatus.Satisfied) == 2, "actual eligible machines admit to real dock");
            c.Equal(Code.None, View(game).PreviewWorkerCandidates("dock.parallel", "a", site.Id, candidates), "same selected candidate roles");
            c.Equal(2, candidates.Count(v => v.Result.Status == WorkRequirementStatus.Satisfied), "two current unheld workers available");
            c.That(candidates.Where(v => v.Result.Status == WorkRequirementStatus.Satisfied).All(v => v.Role == new WorkerRoleId("survey")), "actual role not fabricated by leaf");
            c.Equal(hash, game.Hash(), "dock offers/diagnostics allocate no jobs or stock promises");
            Order parallel = Post(c, game, "dock.parallel");
            Reach(game, () => Board(game).Job(parallel.Node("a").JobId)?.Worker.IsValid == true && Board(game).Job(parallel.Node("b").JobId)?.Worker.IsValid == true, "native slot acquisition");
            Job a = Board(game).Job(parallel.Node("a").JobId), b = Board(game).Job(parallel.Node("b").JobId);
            c.Equal(new Code("bay"), a.DestinationDockSlot, "first actual job owns service intent");
            c.Equal(new Code("wait"), b.DestinationDockSlot, "second actual job owns waiting intent");
            c.That(a.Worker != b.Worker, "two jobs own distinct actual machines");
            c.That(parallel.Nodes.All(n => n.Progress.WholeTicks == 0), "claims and travel earn no instantaneous work");
            c.Equal(Fix.FromInt(20), site.Get<IInventory>().Amount(steel), "acquisition reserves without consuming physical input");
            hash = game.Hash(); c.That(View(game).TryCopyOrderRequirements(parallel.Id, requirements), "live node requirements available");
            c.That(requirements.Count(r => r.Requirement == "dock.admission" && r.Result.Status == WorkRequirementStatus.Satisfied) == 2, "retained jobs exclude only their own native slot claims");
            c.Equal(hash, game.Hash(), "retained admission reads are pure");
            c.That(!View(game, 1).TryCopyOrderRequirements(parallel.Id, requirements) && requirements.Count == 0, "foreign company gets no private dock requirements");
            Hold(c, game, machines[2], false);
            c.Equal(Code.None, View(game).PreviewOrderRequirements("dock.inspect", site.Id, requirements), "busy dock is a typed readiness wait, not invalid intent");
            c.Equal(Code.None, View(game).PreviewWorkerCandidates("dock.inspect", "inspect", site.Id, candidates), "per-machine dock feasibility uses native selector");
            c.That(candidates.Any(v => v.Worker == machines[2].Id && v.Result.Blocker == WorkDockCodes.Full), "actual third machine sees bounded dock full");
            Order pending = Post(c, game, "dock.inspect");
            c.Equal(0, pending.Node("inspect").JobId, "full dock allocates no waiting overflow job");
            c.Equal(requirements.Single().Result.Blocker, pending.Node("inspect").Blocker, "execution shares aggregate preview blocker without hiding per-machine dock refusal");
            Hold(c, game, machines[2], true);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = pending.Id }).Accepted, "cancel unacquired dock intent normally");
            c.Equal(OrderState.Cancelled, pending.State, "cancelled contender retains no native slot");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "travelling and queued dock claims save through existing work owner");
            Reach(game, () => parallel.Node("a").Progress.WholeTicks > 0, "physical service arrival", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved native travel reaches service identically");
            c.Equal(new TileCoord(15, 10), game.World.Entities.Get(a.Worker).Get<ITilePresence>().Tile, "productive worker physically occupies authored bay");
            c.Equal(Fix.FromInt(12), site.Get<IInventory>().Amount(steel), "first physical work consumes exactly eight materials");
            c.Equal(0L, parallel.Node("b").Progress.WholeTicks, "waiting worker earns no overlapping service work");
            twin = factory.Load(game.Save());
            Reach(game, () => parallel.Node("a").State == NodeState.Done, "first physical departure", twin);
            var clearExit = new MoveVehicleCommand { Vehicle = parallel.Node("a").CompletedBy, To = new TileCoord(2, 4) };
            c.Equal(Code.None, game.Seat(0).Preview(clearExit), "completed machine may be moved away from its home dock");
            c.That(TestKit.Order(game, 0, clearExit).Accepted, "normal player clears actual occupied exit");
            c.That(TestKit.Order(twin, 0, clearExit).Accepted, "same player command in saved continuation");
            Reach(game, () => parallel.State == OrderState.Done, "queue departure and second physical service", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial work, waiting promotion and departure resume identically");
            c.Equal(Fix.FromInt(4), site.Get<IInventory>().Amount(steel), "two real jobs consume sixteen once, no phantom dock inventory");
            c.That(parallel.Nodes.All(n => n.CompletedBy.IsValid && n.Progress.WholeTicks > 0), "both machine nodes complete physical work");
            c.That(a.DestinationDockSlot.IsNone && b.DestinationDockSlot.IsNone, "physical departure releases native job intents");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed claims remain released on load");
            var isolated = New(factory); c.Equal(Fix.FromInt(20), Site(isolated).Get<IInventory>().Amount(steel), "interleaved session shares no material or dock claims");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            string source = FrameworkProofContent.Read("native-dock-work.json");
            JsonValue missing = Content.Json.Parse(source); missing.Get("entities")[0].Get("components").Remove("tile_work_dock");
            var game = New(Factory(missing.ToString())); var criteria = new List<WorkRequirementView>();
            c.Equal(Code.None, View(game).PreviewOrderRequirements("dock.inspect", Site(game).Id, criteria), "undocked target still has valid activity intent");
            c.That(criteria.Any(r => r.Result.Blocker == WorkDockCodes.Missing), "ordinary interaction never substitutes a required dock");
            Order order = Post(c, game, "dock.inspect");
            c.Equal(0, order.Node("inspect").JobId, "missing dock acquires no generic-proximity job");
            JsonValue noRoles = Content.Json.Parse(source); noRoles.Get("activities")[1].Get("nodes")[0].Set("roles", Content.Json.Parse("[]"));
            c.Throws<ContentException>(() => New(Factory(noRoles.ToString())), "dock leaf requires declared machine roles before Initialize");
            JsonValue unknown = Content.Json.Parse(source); unknown.Get("activities")[1].Get("nodes")[0].Set("requirement", JsonValue.Of("missing"));
            c.Throws<ContentException>(() => New(Factory(unknown.ToString())), "missing authored dock requirement reference refuses");
            JsonValue extra = Content.Json.Parse(source); extra.Get("dock_work_requirements")[0].Set("reserve", JsonValue.Number("1"));
            c.Throws<ContentException>(() => New(Factory(extra.ToString())), "selected leaf rejects a duplicate reservation field before Initialize");
        }
    }
}
