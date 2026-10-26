using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class ResourceTransactionTests
    {
        public static void Register()
        {
            TestRunner.Add("Resource transactions: pure receipt checks, atomic refusals and saved exact commits", Contracts);
            TestRunner.Add("Resource transactions: haul phases, orphan saves and provider faults", Physical);
        }
        private static GameFactory Factory() => PointGame.Factory(FrameworkProofContent.Read("haul-quantum.json"));
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 19 });
        private static Entity Find(SimGame game, string def)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == def && entity.Alive) return entity;
            throw new InvalidOperationException("missing " + def);
        }
        private static Entity Spawn(SimGame game, string def)
        {
            Entity entity = game.World.Spawn(game.World.Content.Get<EntityDef>(def), new PlayerId(0));
            game.World.Settle(); return entity;
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, int limit, string stage)
        {
            for (int i = 0; i < limit && !done() && game.Fault == null && (twin == null || twin.Fault == null); i++)
            { game.Update(); twin?.Update(); }
            if (!done() || game.Fault != null || twin?.Fault != null)
                throw new InvalidOperationException(stage + " stalled at tick " + game.Tick + "; fault=" + game.Fault + "; twin=" + twin?.Fault);
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); World world = game.World;
            Entity source = Find(game, "source"), destination = Find(game, "destination");
            IInventory stock = source.Get<IInventory>(), dest = destination.Get<IInventory>(); ResourceId resource = world.Content.ResourceId("supply");
            var owner = new ReservationOwner("proof.transfer", 1); PlayerId company = new PlayerId(0);
            ReservationToken outgoing = stock.ReserveOut(owner, resource, Fix.FromInt(30)), incoming = dest.ReserveIn(owner, resource, Fix.FromInt(30));
            var intent = new ResourceTransferIntent(owner, company, outgoing, incoming, resource, Fix.FromInt(30));
            IResourceTransaction transfer = world.Services.Get<IResourceTransaction>(); ulong before = game.Hash();
            c.Equal(ResourceTransferStatus.Ready, stock.CheckReservedOut(owner, outgoing).Status, "source exposes its actual pure receipt/stock check");
            c.Equal(ResourceTransferStatus.Ready, dest.CheckReservedIn(world.Content, owner, incoming).Status, "destination excludes only its own promise in the shared admission evaluator");
            c.Equal(ResourceTransferStatus.InvalidReceipt, stock.CheckReservedOut(new ReservationOwner("proof.transfer", 2), outgoing).Status, "foreign requester fails pure endpoint preflight");
            c.Equal(ResourceTransferStatus.InvalidReceipt, dest.CheckReservedIn(world.Content, owner, outgoing).Status, "wrong endpoint/direction fails without consuming either promise");
            ResourceTransferCheck retained = transfer.Evaluate(intent);
            c.That(retained.Ready, "one engine transaction evaluator checks both existing providers");
            c.Equal(source.Version, retained.SourceVersion, "source checkpoint uses its actual entity version");
            c.Equal(destination.Version, retained.DestinationVersion, "destination checkpoint uses its actual entity version");
            c.Equal(before, game.Hash(), "all receipt/transaction queries are pure");
            c.Equal(ResourceTransferStatus.InvalidRequest, transfer.Commit(default).Status, "default intent refuses explicitly");
            c.Equal(ResourceTransferStatus.InvalidReceipt, transfer.Commit(new ResourceTransferIntent(owner, company, outgoing, incoming, resource, Fix.FromInt(31))).Status,
                "transfer cannot exceed either remaining receipt");
            c.Equal(ResourceTransferStatus.InvalidRequest, transfer.Commit(new ResourceTransferIntent(owner, new PlayerId(1), outgoing, incoming, resource, Fix.FromInt(30))).Status,
                "foreign company cannot use another company's valid receipts");
            c.Equal(before, game.Hash(), "invalid intentions mutate no inventory, promise sequence or checkpoint");
            c.Equal(Fix.FromInt(5), dest.Add(resource, Fix.FromInt(5)), "ordinary compatible stock can change after a retained preview");
            c.That(transfer.Evaluate(intent).DestinationVersion != retained.DestinationVersion, "current preview observes changed destination checkpoint");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "existing module-owned commitments survive save without a new transaction ledger");
            ResourceTransferResult committed = transfer.Commit(intent);
            ResourceTransferResult committedTwin = twin.World.Services.Get<IResourceTransaction>().Commit(intent);
            c.That(committed.Committed && committedTwin.Committed, "commit re-evaluates current state instead of trusting a retained preview");
            c.Equal(Fix.FromInt(30), committed.Amount, "exact quantity leaves the source and enters destination once");
            c.Equal(Fix.FromInt(30), stock.Amount(resource), "source actual balance changes exactly");
            c.Equal(Fix.FromInt(35), dest.Amount(resource), "destination keeps its intervening actual stock plus the exact delivery");
            c.Equal(0, stock.Reservations.Count + dest.Reservations.Count, "successful transfer consumes both owned receipts");
            c.Equal(game.Hash(), twin.Hash(), "exact endpoint versions/stock/receipts continue identically");
            c.That(retained.Ready, "retained preview remains detached, never a mutable permission");
            before = game.Hash();
            c.Equal(ResourceTransferStatus.InvalidReceipt, transfer.Commit(intent).Status, "consumed physical transfer cannot be repeated");
            c.Equal(before, game.Hash(), "repeated transfer refuses before any physical mutation");
            outgoing = stock.ReserveOut(owner, resource, Fix.FromInt(30)); incoming = dest.ReserveIn(owner, resource, Fix.FromInt(30));
            intent = new ResourceTransferIntent(owner, company, outgoing, incoming, resource, Fix.FromInt(30));
            dest.ReleaseIn(owner, incoming); before = game.Hash();
            c.Equal(ResourceTransferStatus.InvalidReceipt, transfer.Commit(intent).Status, "missing destination promise prevents source consumption");
            c.Equal(before, game.Hash(), "late receipt refusal leaves the source fully protected");
            c.Equal(Fix.FromInt(30), stock.ReservedOut(resource), "refused primitive does not release the owner's unspent promise");
            stock.ReleaseOut(owner, outgoing);
            incoming = dest.ReserveIn(owner, resource, Fix.FromInt(30)); outgoing = stock.ReserveOut(owner, resource, Fix.FromInt(30));
            intent = new ResourceTransferIntent(owner, company, outgoing, incoming, resource, Fix.FromInt(30));
            // Internal fault injection only: ordinary gameplay cannot discard protected stock/receipts.
            c.Throws<InvalidOperationException>(() => world.Despawn(destination), "normal removal preserves active transfer claims and stock");
            world.Entities.Destroy(destination, world.Space); world.Settle(); before = game.Hash();
            c.Equal(ResourceTransferStatus.MissingEndpoint, transfer.Commit(intent).Status, "removed destination is a normal typed refusal");
            c.Equal(before, game.Hash(), "removed endpoint cannot trigger partial source consumption");
            stock.ReleaseOut(owner, outgoing);
            PaidContracts(c);
        }
        private static void PaidContracts(Check c)
        {
            GameFactory factory = Colony.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 23 });
            World world = game.World; TileMap map = (TileMap)world.Space; ResourceId steel = world.Content.ResourceId("steel");
            Entity source = map.SpawnAt(world, world.Content.Get<EntityDef>("module_supplier"), PlayerId.None, new TileCoord(28, 18));
            Entity destination = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(12, 18));
            world.Settle(); IInventory dest = destination.Get<IInventory>(); dest.Remove(steel, Fix.FromInt(30));
            var owner = new ReservationOwner("proof.purchase", 1); PlayerId company = new PlayerId(0); ISupplier supplier = source.Get<ISupplier>();
            var intent = new ResourceTransferIntent(owner, company, supplier.ReserveSupply(owner, steel, Fix.FromInt(15)),
                dest.ReserveIn(owner, steel, Fix.FromInt(15)), steel, Fix.FromInt(15), ResourceTransferKind.Supply);
            IEconomy economy = world.Services.Get<IEconomy>(); economy.Charge(company, economy.Cash(company), new Code("proof.cash"));
            IResourceTransaction transfer = world.Services.Get<IResourceTransaction>(); ulong before = game.Hash();
            c.That(supplier.CheckSupply(world, company, owner, intent.Source, intent.Amount).Ready, "physical receipt admission is independent of payment");
            c.Equal(ResourceTransferStatus.NoPayment, transfer.Evaluate(intent).Status, "transaction preview reports the supplier's actual payment blocker");
            c.Equal(ResourceTransferStatus.NoPayment, transfer.Commit(intent).Status, "payment refusal happens before stock or cargo admission");
            c.Equal(before, game.Hash(), "unaffordable transfer changes neither money, endpoint nor receipt");
            c.Equal(Fix.FromInt(25), source.Get<IInventory>().Amount(steel), "unaffordable finite supply leaves actual stock intact");
            c.Equal(Fix.FromInt(15), source.Get<IInventory>().ReservedOut(steel), "unspent finite promise stays with its original owner");
            economy.Credit(company, Fix.FromInt(105), new Code("proof.cash")); SimGame twin = factory.Load(game.Save());
            c.That(transfer.Commit(intent).Committed, "funded current-state commit fulfills the same owned intention");
            twin.World.Services.Get<IResourceTransaction>().Commit(intent);
            c.Equal(Fix.Zero, economy.Cash(company), "exact finite supply payment occurs once");
            c.Equal(Fix.FromInt(10), source.Get<IInventory>().Amount(steel), "finite source's sole stock owner loses exactly the sold quantity");
            c.Equal(Fix.FromInt(15), dest.Amount(steel), "exact paid receipt enters the destination's actual inventory");
            c.Equal(game.Hash(), twin.Hash(), "paid owned transfer survives saved continuation");
        }
        private static Job Acquire(SimGame game, Entity source, Entity destination)
        {
            int id = game.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), source.Id, destination.Id,
                game.World.Content.ResourceId("supply"), Fix.FromInt(30), 100);
            return game.World.Services.Get<WorkBoard>().Job(id) ?? throw new InvalidOperationException("missing acquired haul");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); World world = game.World;
            Entity source = Find(game, "source"), destination = Find(game, "destination"), machine = Find(game, "machine");
            ResourceId resource = world.Content.ResourceId("supply"); Job job = Acquire(game, source, destination);
            IHaulExecution haul = world.Services.Get<IHaulExecution>(); ulong before = game.Hash();
            c.Throws<InvalidOperationException>(() => haul.Load(world, job, machine, source, resource, job.Amount), "even a co-located carrier cannot load outside its current physical toil");
            c.Equal(before, game.Hash(), "out-of-phase physical call consumes no stock or receipt");
            job.Loaded = job.Amount;
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "save cannot invent loaded cargo while source/capacity promises remain"); job.Loaded = Fix.Zero;
            ReservationToken pickup = job.OutReservation; job.OutReservation = default;
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "unloaded phase requires its owned source receipt"); job.OutReservation = pickup;
            Toil actual = job.Toils[1]; job.Toils[1] = Toil.Load(source.Id, resource, Fix.FromInt(15));
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "saved itinerary cannot change the exact committed quantity"); job.Toils[1] = actual;
            JobState state = job.State; job.State = JobState.Done;
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "terminal haul cannot retain live worker/endpoint commitments"); job.State = state;
            machine.Get<IWorker>().Job = 999;
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "saved selected worker must name the same existing assignment"); machine.Get<IWorker>().Job = job.Id;
            c.Equal(before, game.Hash(), "restoring injected job fields leaves the real commitment unchanged");
            IInventory stock = source.Get<IInventory>(); var orphanOwner = new ReservationOwner("sim.haul", 999);
            ReservationToken orphan = stock.ReserveOut(orphanOwner, resource, Fix.FromInt(3));
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "orphan endpoint promise cannot survive without a live haul"); stock.ReleaseOut(orphanOwner, orphan);
            var moduleOwner = new ReservationOwner("proof.unrelated", 999); ReservationToken unrelated = stock.ReserveOut(moduleOwner, resource, Fix.FromInt(3));
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "another module's owned reservation is not reinterpreted as a haul"); stock.ReleaseOut(moduleOwner, unrelated);
            SimGame twin = factory.Load(game.Save());
            Until(game, twin, () => job.Loaded.IsPositive, 30, "exact physical pickup");
            c.Equal(job.Amount, machine.Get<ITransporter>().Cargo.Amount(resource), "loaded phase is actual protected cargo");
            c.Equal(game.Hash(), twin.Hash(), "source-to-cargo transaction continues identically after save");
            job.Loaded = Fix.Zero;
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "loaded phase cannot discard its actual quantity while cargo receipt survives"); job.Loaded = job.Amount;
            job.OutReservation = pickup;
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "loaded phase cannot resurrect a spent source receipt"); job.OutReservation = default;
            before = game.Hash();
            c.Throws<InvalidOperationException>(() => haul.Unload(world, job, machine, destination, resource), "physical unload requires the current unload toil and actual arrival");
            c.Equal(before, game.Hash(), "premature unload cannot teleport actual cargo");
            twin = factory.Load(game.Save());
            Until(game, twin, () => destination.Get<IInventory>().Amount(resource) == Fix.FromInt(30), 200, "exact physical delivery");
            c.Equal(JobState.Done, job.State, "physical delivery ends the same authoritative job");
            c.Equal(Fix.Zero, job.Loaded, "completed transfer leaves no stale loaded phase");
            c.Equal(Fix.FromInt(60), stock.Amount(resource) + machine.Get<ITransporter>().Cargo.Amount(resource) + destination.Get<IInventory>().Amount(resource), "two exact transfers conserve actual ownership");
            c.Equal(game.Hash(), twin.Hash(), "cargo-to-destination transaction continues identically");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed itinerary/claim cleanup remains loadable");
            RemovedDestination(c); RemovedCarrier(c); PaidRefusal(c);
            foreach (string prototype in new[] { "fault.partial", "fault.retained", "fault.query", "fault.status", "fault.version", "fault.admission" }) ProviderFault(c, prototype);
        }
        private static void RemovedDestination(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Find(game, "source"), destination = Find(game, "destination"), machine = Find(game, "machine");
            Job job = Acquire(game, source, destination); ulong before = game.Hash();
            c.Equal(ResourceTransactions.ActivePromises, game.World.CheckDespawn(destination), "normal destination removal protects the owned admission claim");
            c.Throws<InvalidOperationException>(() => game.World.Despawn(destination), "normal removal cannot silently erase haul promises");
            c.Equal(before, game.Hash(), "protected destination refusal changes no job or receipt");
            // Bypass only to exercise recovery from a corrupted/missing participant, never a player path.
            game.World.Entities.Destroy(destination, game.World.Space); game.World.Settle();
            SimGame twin = factory.Load(game.Save()); game.Update(); twin.Update(); ResourceId resource = game.World.Content.ResourceId("supply");
            c.That(!job.IsLive && game.Fault == null, "missing final destination ends a complete intention before source pickup");
            c.Equal(Fix.FromInt(60), source.Get<IInventory>().Amount(resource), "invalid final destination cannot consume physical source stock");
            c.Equal(0, source.Get<IInventory>().Reservations.Count + machine.Get<ITransporter>().Cargo.Reservations.Count, "surviving source/cargo promises release after destination removal");
            c.Equal(game.Hash(), twin.Hash(), "saved removed-destination cleanup is deterministic");
        }
        private static void RemovedCarrier(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Find(game, "source"), destination = Find(game, "destination"), machine = Find(game, "machine");
            Job job = Acquire(game, source, destination); ulong before = game.Hash();
            c.Equal(ResourceTransactions.ActivePromises, game.World.CheckDespawn(machine), "normal carrier removal protects the owned cargo admission claim");
            c.Throws<InvalidOperationException>(() => game.World.Despawn(machine), "normal removal cannot discard the acquired carrier commitment");
            c.Equal(before, game.Hash(), "protected carrier refusal changes no endpoint or assignment");
            // Internal missing-participant fault injection is separate from protected normal removal.
            game.World.Entities.Destroy(machine, game.World.Space); game.World.Settle();
            SimGame twin = factory.Load(game.Save()); game.Update(); twin.Update();
            c.That(!job.IsLive && game.Fault == null, "missing acquired carrier releases the complete intention, not an anonymous waiting job");
            c.Equal(0, source.Get<IInventory>().Reservations.Count + destination.Get<IInventory>().Reservations.Count, "surviving endpoints do not remain monopolized by a missing carrier");
            c.Equal(game.Hash(), twin.Hash(), "saved missing-carrier cleanup is deterministic");
        }
        private static void PaidRefusal(Check c)
        {
            GameFactory factory = Colony.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 23 });
            World world = game.World; TileMap map = (TileMap)world.Space; ResourceId steel = world.Content.ResourceId("steel");
            Entity source = map.SpawnAt(world, world.Content.Get<EntityDef>("module_supplier"), PlayerId.None, new TileCoord(28, 18));
            Entity destination = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(12, 18)); world.Settle();
            destination.Get<IInventory>().Remove(steel, Fix.FromInt(30));
            int id = world.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), source.Id, destination.Id, steel, Fix.FromInt(20), 100, true);
            Job job = world.Services.Get<WorkBoard>().Job(id); Entity machine = world.Entities.Get(job.Worker);
            IEconomy economy = world.Services.Get<IEconomy>(); economy.Charge(new PlayerId(0), economy.Cash(new PlayerId(0)), new Code("proof.cash"));
            SimGame twin = factory.Load(game.Save()); Until(game, twin, () => !job.IsLive, 2880, "physical unaffordable pickup");
            c.Equal(JobState.Failed, job.State, "payment changes during actual travel are a normal refusal, not a partial transfer fault");
            c.Equal(Fix.FromInt(25), source.Get<IInventory>().Amount(steel), "refused physical payment leaves actual finite supply intact");
            c.Equal(Fix.Zero, machine.Get<ITransporter>().Cargo.Amount(steel) + destination.Get<IInventory>().Amount(steel), "unaffordable pickup creates no actual cargo or delivery");
            c.Equal(0, source.Get<IInventory>().Reservations.Count + destination.Get<IInventory>().Reservations.Count + machine.Get<ITransporter>().Cargo.Reservations.Count,
                "physical refusal releases source/cargo/destination promises through the same job");
            c.Equal(Fix.Zero, economy.Cash(new PlayerId(0)), "normal refusal fabricates no compensating refund");
            c.Equal(game.Hash(), twin.Hash(), "saved paid-refusal cleanup continues identically");
        }
        private static void ProviderFault(Check c, string prototype)
        {
            SimGame game = New(Factory()); World world = game.World; Entity defective = Spawn(game, prototype);
            Entity source = prototype == "fault.admission" ? Find(game, "source") : defective;
            Entity destination = prototype == "fault.admission" ? defective : Find(game, "destination");
            Entity machine = Find(game, "machine"); Job job = Acquire(game, source, destination); ResourceId resource = world.Content.ResourceId("supply");
            for (int i = 0; i < 200 && game.Fault == null; i++) game.Update();
            c.That(game.Fault != null, "authored provider violation faults deterministically: " + prototype);
            c.Equal("sim.work", game.Fault?.Phase, "fault names the physical engine execution phase: " + prototype);
            string reason = prototype == "fault.query" ? "mutated endpoint state during a pure transfer check"
                : prototype == "fault.status" ? "invalid preflight status"
                : prototype == "fault.admission" ? "destination violated exact physical transfer receipt"
                : "source violated exact physical transfer receipt";
            c.That(game.Fault?.Message.Contains(reason) == true, "fault proves the intended protocol violation, not an unrelated failure: " + prototype);
            c.Throws<InvalidOperationException>(() => game.Save(), "partial/faulted physical state cannot be saved as resumable: " + prototype);
            long tick = game.Tick; ulong hash = game.Hash(); game.Update(); game.Update();
            c.Equal(tick, game.Tick, "faulted game cannot continue its partial tick: " + prototype);
            c.Equal(hash, game.Hash(), "fault is sticky with no fabricated rollback: " + prototype);
            if (prototype == "fault.partial" || prototype == "fault.version")
            {
                c.Equal(Fix.FromInt(30), source.Get<IInventory>().Amount(resource), "source violation does not recreate physically spent source stock");
                c.Equal(Fix.Zero, machine.Get<ITransporter>().Cargo.Amount(resource), "partial source receipt is rejected before any cargo admission");
            }
            else if (prototype == "fault.retained" || prototype == "fault.query" || prototype == "fault.status")
                c.Equal(Fix.FromInt(60), source.Get<IInventory>().Amount(resource), "invalid preflight/receipt provider cannot fabricate actual cargo: " + prototype);
            else
                c.Equal(Fix.FromInt(30), destination.Get<IInventory>().Amount(resource), "dishonest admission is faulted without a fabricated compensating reverse transfer");
            c.That(job.IsLive, "faulted partial job is not mislabeled completed or safely cancelled: " + prototype);
        }
    }
}
