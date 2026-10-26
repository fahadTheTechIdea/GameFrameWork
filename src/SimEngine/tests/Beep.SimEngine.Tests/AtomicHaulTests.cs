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
    public static partial class AtomicHaulTests
    {
        public static void Register()
        {
            TestRunner.Add("Atomic hauling: common quantities, all-participant refusal and exclusive carrier acquisition", Acquisition);
            TestRunner.Add("Atomic hauling: protected cargo, route interruption and saved physical workflows", Physical);
            TestRunner.Add("Atomic hauling: finite paid physical delivery and saved native settlement", PaidSupply);
            TestRunner.Add(OperationCase, OperationAdmission);
        }
        private static GameFactory Factory() => PointGame.Factory(FrameworkProofContent.Read("haul-quantum.json"));
        internal static GameFactory CompetitionFactory(Action<JsonValue> edit = null)
        {
            var authored = Json.Parse(FrameworkProofContent.Read("haul-competition.json")); edit?.Invoke(authored);
            return PointGame.Factory(FrameworkProofContent.Read("haul-quantum.json"),
                additionalDocuments: new Dictionary<string, string> { { "haul-competition.json", authored.ToString() } });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 19 });
        private static Entity Find(SimGame game, string def)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == def && entity.Alive) return entity;
            throw new InvalidOperationException("missing " + def);
        }
        private static void Acquisition(Check c)
        {
            GameFactory factory = CompetitionFactory(); SimGame game = factory.NewGame(new NewGameRequest {
                Scenario = "haul.competition", Seed = 19, Seats = { Controller.Human, Controller.Human } }); World world = game.World;
            Entity source = Find(game, "competition.source"), destination = Find(game, "competition.destination"), machine = Find(game, "machine");
            Entity second = world.Entities.All.Single(e => e.Alive && e.Def.Id == "machine" && e.Id != machine.Id);
            IWorkerControl controls = world.Services.Get<IWorkerControl>();
            controls.SetHeld(world, machine.Owner, second.Id, true);
            ResourceId resource = world.Content.ResourceId("supply"); IHaulExecution haul = world.Services.Get<IHaulExecution>();
            IInventory stock = source.Get<IInventory>(), dest = destination.Get<IInventory>(), cargo = machine.Get<ITransporter>().Cargo;
            CapacityQuantum qs = stock.CapacityPolicy.Quantum(resource), qc = cargo.CapacityPolicy.Quantum(resource), qd = dest.CapacityPolicy.Quantum(resource);
            c.Equal(Fix.FromInt(3).Raw, qs.Raw, "source quantum comes from JSON unit scales/discreteness");
            c.Equal(Fix.FromInt(2).Raw, qc.Raw, "carrier quantum is independent of the source");
            c.Equal(Fix.FromInt(5).Raw, qd.Raw, "destination has a third exact measured step");
            CapacityQuantum common = CapacityQuantum.Combine(qs, qc, qd);
            c.Equal(Fix.FromInt(30).Raw, common.Raw, "LCM determines common native quantity without a decrement loop");
            c.Equal(Fix.FromInt(30), common.Floor(Fix.FromInt(35)), "bounded ceiling selects the greatest common exact quantity");
            c.Equal(Fix.Zero, common.Floor(Fix.FromInt(29)), "smaller-than-quantum request has no fabricated partial load");
            c.Equal(CapacityStatus.OutOfRange, CapacityQuantum.Combine(new CapacityQuantum(CapacityStatus.Fits, long.MaxValue),
                new CapacityQuantum(CapacityStatus.Fits, long.MaxValue - 1)).Status, "common quantum overflow refuses without numeric wrap");
            c.Equal(CapacityStatus.InvalidRequest, CapacityQuantum.Combine(default(CapacityQuantum)).Status, "missing provider quantum refuses");
            c.Equal(CapacityStatus.UnknownResource, stock.CapacityPolicy.Quantum(new ResourceId(int.MaxValue)).Status, "quantum query uses the same bound resource catalog");
            ulong before = game.Hash();
            c.Equal(HaulStatus.NoCapacity, haul.PreviewHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(29)).Status,
                "common quantity refusal happens before any participant acquisition");
            c.Equal(0, haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(29), 100), "impossible acquire returns no job");
            c.Equal(before, game.Hash(), "refusal changes no receipt sequence, stock, worker or job ID");
            controls.SetHeld(world, machine.Owner, machine.Id, true); before = game.Hash();
            c.Equal(HaulStatus.NoCarrier, haul.PreviewHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(35)).Status,
                "a player-held carrier cannot be promised");
            c.Equal(0, haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(35), 100), "held worker prevents all endpoint acquisition");
            c.Equal(before, game.Hash(), "held-carrier refusal has all-or-none state semantics");
            controls.SetHeld(world, machine.Owner, machine.Id, false);
            HaulPreview preview = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source.Id, destination.Id, resource, Fix.FromInt(35));
            c.Equal(HaulStatus.Ready, preview.Status, "host view asks the engine's one selection owner");
            c.Equal(Fix.FromInt(30), preview.Amount, "view contains the engine-calculated common quantity");
            c.Equal(machine.Id, preview.Carrier, "preview identifies the actual available carrier");
            c.Equal(new Code("service"), preview.SourceDockSlot, "resource haul excludes a closer work-only bay even on a worker with the same role");
            c.Equal(HaulStatus.NotFound, game.Seat(1).View.Feature<LogisticsView>()
                .PreviewHaul(source.Id, destination.Id, resource, Fix.FromInt(35)).Status, "foreign viewer cannot obtain private selection facts");
            int id = haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(35), 100);
            Job job = world.Services.Get<WorkBoard>().Job(id);
            c.Equal(1, id, "earlier refusals did not consume the first job ID");
            c.Equal(JobState.Assigned, job.State, "a haul is assigned as part of participant acquisition, not an anonymous open job");
            c.Equal(new Code("service"), job.SourceDockSlot, "acquisition uses the same request-compatible physical bay as preview");
            c.Equal(id, machine.Get<IWorker>().Job, "carrier claim uses the existing worker/job relation");
            c.That(job.OutReservation.IsValid && job.InReservation.IsValid && job.CargoInReservation.IsValid, "all three endpoint/cargo promises acquired together");
            c.Equal(Fix.FromInt(30), stock.ReservedOut(resource), "source promises the common quantity once");
            c.Equal(Fix.FromInt(30), dest.ReservedIn(resource), "destination promises the same common quantity");
            c.Equal(Fix.FromInt(30), cargo.ReservedIn(resource), "cargo capacity is owned for the selected carrier");
            before = game.Hash();
            c.Equal(0, haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(35), 100), "competing request cannot reserve the same carrier twice");
            c.Equal(before, game.Hash(), "competing acquisition makes no partial source/destination promises");
            c.Equal(Fix.FromInt(30), preview.Amount, "retained preview remains a detached fact after acquisition");
            controls.SetHeld(world, second.Owner, second.Id, false);
            int next = haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(35), 100);
            c.Equal(2, next, "a different eligible carrier permits a second complete commitment");
            c.Equal(next, second.Get<IWorker>().Job, "second job owns a distinct existing worker");
            c.Equal(Fix.FromInt(60), stock.ReservedOut(resource), "competing jobs cannot promise more than actual source stock");
            c.Equal(Fix.FromInt(60), dest.ReservedIn(resource), "destination aggregates derive from two distinct owned intentions");
            before = game.Hash();
            c.Equal(0, haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(35), 100), "exhausted source prevents every new participant claim");
            c.Equal(before, game.Hash(), "exhausted-source refusal consumes no job or sequence");
            SimGame twin = factory.Load(game.Save());
            c.Equal(StateCodecTests.ExpectedSaveFormat, SaveGame.Describe(game.Save()).Format, "complete carrier/cargo commitment uses current independently authored protocol");
            c.Equal(game.Hash(), twin.Hash(), "both acquired jobs restore with their exact carriers and receipts");
            Until(game, twin, () => dest.Amount(resource) == Fix.FromInt(60) && AtHome(game, machine.Id) && AtHome(game, second.Id),
                200, "two physical deliveries, dock departures and distinct home returns", () => {
                    c.Equal(Fix.FromInt(60), stock.Amount(resource) + dest.Amount(resource) + cargo.Amount(resource) +
                        second.Get<ITransporter>().Cargo.Amount(resource), "each update conserves all actual finite stock");
                    c.That(machine.Get<PointPresence>().Position > second.Get<PointPresence>().Position, "carriers never overlap or cross in the authored one-dimensional corridor");
                    c.Equal(game.Hash(), twin.Hash(), "each saved physical update preserves deterministic identity");
                });
            c.Equal(Fix.Zero, stock.Amount(resource), "both actual source loads transfer without duplication");
            c.That(stock.Reservations.Count == 0 && dest.Reservations.Count == 0 && cargo.Reservations.Count == 0 &&
                second.Get<ITransporter>().Cargo.Reservations.Count == 0, "completed departures release all three participant books");
            c.That(machine.Get<IWorker>().Idle && second.Get<IWorker>().Idle, "both machines complete their actual jobs before resting at authored homes");
            c.Equal(game.Hash(), twin.Hash(), "two competing saved physical transfers continue identically");
            controls.SetHeld(world, machine.Owner, machine.Id, true);
            var posted = TestKit.Order(game, 0, new PostOrderCommand { Plan = "dock.inspect", Target = source.Id });
            c.That(posted.Accepted, "same machine role may perform actual staffed work through the original activity owner");
            var workJobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(workJobs);
            c.Equal(new Code("a.work-only"), workJobs.Single(j => !j.IsHaul).DestinationDockSlot,
                "work excludes the closer resource-only bay, rather than classifying by the actor's role alone");
            twin = factory.Load(game.Save());
            Until(game, twin, () => second.Get<IWorker>().Idle && AtHome(game, second.Id), 200, "request-filtered staffed work and departure", () => {
                c.Equal(game.Hash(), twin.Hash(), "saved work-bay admission and actual execution remain deterministic");
                c.Equal(Fix.FromInt(60), stock.Amount(resource) + dest.Amount(resource) + cargo.Amount(resource) + second.Get<ITransporter>().Cargo.Amount(resource),
                    "bay routing creates or consumes no physical stock");
            });
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "filtered physical work and released dock checkpoint normally");
        }
        private static bool AtHome(SimGame game, EntityId worker)
            => game.Seat(0).View.Feature<WorkView>().ReadHome(worker, out var value).IsNone && value.State == WorkerHomeState.AtHome;
        private static void Until(SimGame game, SimGame twin, Func<bool> done, int limit, string stage, Action observe = null)
        {
            for (int i = 0; i < limit && game.Fault == null && (twin == null || twin.Fault == null) && !done(); i++) {
                game.Update(); twin?.Update();
                if (game.Fault == null && twin?.Fault == null) observe?.Invoke();
            }
            if (game.Fault != null || twin?.Fault != null || !done())
            {
                var jobs = new List<WorkJobView>();
                if (game.Fault == null) game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                string work = string.Join("; ", jobs.Select(j => "job=" + j.Id + ",state=" + j.State + ",worker=" + j.Worker +
                    ",toil=" + j.ToilIndex + ",loaded=" + j.Loaded + "/" + j.Amount + ",handled=" + j.HandledAmount +
                    ",progress=" + j.HandlingProgress.WholeTicks + ",blocker=" + j.Blocker + ",failures=" + j.FailureCount));
                string entities = game.Fault != null ? "live diagnostics withheld after first fault" : string.Join("; ", game.World.Entities.All.Where(e => e.Alive).Select(e => e.Def.Id + "#" + e.Id +
                    ",state=" + e.State + ",stock=[" + string.Join(",", e.Get<IInventory>()?.Stock.Select(line => line.Resource + "=" + line.Amount) ?? Array.Empty<string>()) + "]" +
                    ",held=" + e.Get<IWorker>()?.Held + ",motion=" + e.Get<IMotion>()?.Status +
                    ",position=" + e.Get<PointPresence>()?.Position));
                throw new InvalidOperationException(stage + " stalled at game update " + game.Tick + "; fault=" + game.Fault +
                    "; twin=" + twin?.Fault + "; " + work + "; " + entities);
            }
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); World world = game.World;
            Entity source = Find(game, "source"), destination = Find(game, "destination"), machine = Find(game, "machine");
            IInventory cargo = machine.Get<ITransporter>().Cargo, stock = source.Get<IInventory>(), dest = destination.Get<IInventory>();
            ResourceId resource = world.Content.ResourceId("supply");
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = destination.Id, Resource = resource, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(30), Fix.FromInt(30), Fix.FromInt(30), 45, LogisticsReserveAccess.Protected)) }).Accepted,
                "normal stock policy initiates the same atomic acquisition path");
            // Authored30 units at1 loading unit/minute occupy the entire original loading bound.
            // Observe actual loading and subsequent travel as separate bounded engine stages.
            Until(game, null, () => cargo.Amount(resource)==Fix.FromInt(30), 30, "authored common-quantity loading");
            Until(game, null, () => cargo.Amount(resource).IsPositive && machine.Get<PointPresence>().Position.IsPositive, 30, "physical loaded departure");
            Job job = world.Services.Get<WorkBoard>().Job(machine.Get<IWorker>().Job);
            c.Equal(Fix.FromInt(30), cargo.Amount(resource), "physical pickup admits exact common quantity");
            c.That(!job.CargoInReservation.IsValid && job.CargoOutReservation.IsValid, "cargo capacity receipt transitions to an actual protected-stock receipt");
            c.Equal(Fix.Zero, cargo.Remove(resource, Fix.One), "unreserved consumers cannot steal committed carried cargo");
            c.Equal(Fix.FromInt(30), cargo.ReservedOut(resource), "loaded cargo remains owned until physical unload or cancellation");
            c.Equal(Fix.Zero, dest.Amount(resource), "commitment and load do not teleport delivery");
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "engine command interrupts actual physical route");
            game.Update();
            c.Equal(Fix.FromInt(30), cargo.Amount(resource), "interruption retains actual carried stock");
            c.Equal(Fix.FromInt(30), cargo.ReservedOut(resource), "temporary route loss retains protected physical cargo during authored retry");
            c.Equal(Fix.FromInt(30), dest.ReservedIn(resource), "pending original delivery retains its actual destination promise");
            c.Equal(job.Id, machine.Get<IWorker>().Job, "pending physical delivery retains its selected carrier");
            c.That(job.State==JobState.Paused&&job.RetryAt>game.Clock.StepIndex,"route loss enters actual deterministic retry, not instant cancellation");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "paused physical cargo saves with original retry and protected claims");
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = true }).Accepted, "restored route permits actual return and replanning");
            TestKit.Order(twin, 0, new SetPointAccessCommand { Open = true });
            Until(game, twin, () => dest.Amount(resource) == Fix.FromInt(30) && cargo.Amount(resource).IsZero, 240, "physical return/replanned delivery");
            c.Equal(Fix.FromInt(60), stock.Amount(resource) + dest.Amount(resource) + cargo.Amount(resource), "cancel/return/replan preserves all actual resource ownership");
            c.Equal(game.Hash(), twin.Hash(), "saved interrupted acquisition recovers deterministically");
        }
        private static void PaidSupply(Check c)
        {
            var setup = PaidSupplyFixture(); GameFactory factory = setup.Factory; SimGame game = setup.Game;
            World world = game.World; ResourceId steel = world.Content.ResourceId("steel");
            Entity source = setup.Source, destination = setup.Destination;
            IHaulExecution haul = world.Services.Get<IHaulExecution>(); IEconomy economy = world.Services.Get<IEconomy>();
            Fix cash = economy.Cash(new PlayerId(0));
            int first = haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, steel, Fix.FromInt(20), 100, true);
            int second = haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, steel, Fix.FromInt(20), 100, true);
            c.That(first != 0 && second != 0, "two finite paid intentions acquire distinct carriers");
            c.Equal(Fix.FromInt(25), source.Get<IInventory>().ReservedOut(steel), "finite supply promises use the existing source inventory, not a supply balance");
            c.Equal(Fix.FromInt(5), world.Services.Get<WorkBoard>().Job(second).Amount, "second paid load is limited by unpromised source stock");
            c.Equal(cash, economy.Cash(new PlayerId(0)), "atomic planning does not charge before physical pickup");
            SimGame twin = factory.Load(game.Save());
            Until(game, twin, () => destination.Get<IInventory>().Amount(steel) == Fix.FromInt(25), 2880, "two finite paid physical deliveries");
            c.Equal(Fix.Zero, source.Get<IInventory>().Amount(steel), "finite supplier fulfills only actual owned stock");
            c.Equal(cash - Fix.FromInt(175), economy.Cash(new PlayerId(0)), "payment matches two exact physical fulfilled receipts once");
            c.Equal(game.Hash(), twin.Hash(), "paid supply/cargo commitments save and continue identically");
        }
        private static (GameFactory Factory, SimGame Game, Entity Source, Entity Destination) PaidSupplyFixture()
        {
            // Explicit finite-purchase contract probe. Resource-only defaults remain unchanged;
            // native acquisition, physical handling and sole Tycoon settlement own all decisions.
            var root=Json.Parse(Colony.Defs);var scenario=root.Get("scenarios")[0];
            scenario.Set("game_mode",JsonValue.Of("colony.paid-supply"));
            scenario.Get("modules").Add(JsonValue.Of(TycoonModule.ModuleId));
            scenario.Get("settings").Set(TycoonModule.ModuleId,Json.Parse("{\"account_policy\":\"colony.paid.accounts\"}"));
            var master=Json.Parse(Colony.Manifest);
            var modules=master.Get("modules");
            for(int i=0;i<modules.Count;i++)if(modules[i].Get("id").Text==TycoonModule.ModuleId)
                modules[i].Get("documents").Add(JsonValue.Of("colony-paid-supply.json"));
            GameFactory factory = Colony.Factory(definitions:root.ToString(),masterDocument:master.ToString(),
                moduleDocuments:new Dictionary<string,string>{{"colony-paid-supply.json",FrameworkProofContent.Read("colony-paid-supply.json")}});
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 23 });
            World world = game.World; TileMap map = (TileMap)world.Space; ResourceId steel = world.Content.ResourceId("steel");
            Entity source = map.SpawnAt(world, world.Content.Get<EntityDef>("module_supplier"), PlayerId.None, new TileCoord(28, 18));
            Entity destination = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(12, 18));
            world.Settle();
            destination.Get<IInventory>().Remove(steel, Fix.FromInt(30));
            return (factory, game, source, destination);
        }
    }
}
