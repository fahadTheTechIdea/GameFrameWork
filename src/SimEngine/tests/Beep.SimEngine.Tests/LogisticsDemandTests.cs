using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class LogisticsDemandTests
    {
        public static void Register()
        {
            TestRunner.Add("Logistics demands: declared sources, atomic native intent and private saved facts", Admission);
            TestRunner.Add("Logistics demands: JSON mod, physical delivery and retired saved commitments", Physical);
        }
        private static string Json => FrameworkProofContent.Read("logistics-demands.json");
        private const string Declaration = "{\"id\":\"proof.demands\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"demands.json\"]}";
        private static GameFactory Factory(string json = null, DemandProofModule module = null, string definitions = null)
            => PointGame.Factory(definitions ?? PointGame.Definitions, additionalModule: module ?? new DemandProofModule(), moduleDeclaration: Declaration, moduleDocument: json ?? Json);
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 211 }; request.Mods.Add("proof.demands");
            return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string prototype)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype) return entity;
            throw new InvalidOperationException("missing demand entity " + prototype);
        }
        private static List<LogisticsDemandView> Facts(SimGame game)
        { var result = new List<LogisticsDemandView>(); game.Seat(0).View.Feature<LogisticsView>().CopyDemands(result); return result; }
        private static Bill Want(ResourceId resource, Fix quantity) => new Bill(new[] { new ResourceAmount(resource, quantity) });
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, SimGame isolated = null)
        {
            for (int i = 0; i < 120 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); if (isolated != null) Tick(isolated); }
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in game.World.Services.Get<WorkBoard>().Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timed out at tick " + game.Tick + ": " + string.Join(";", facts));
            }
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); World world = game.World;
            Entity destination = Find(game, "destination"), machine = Find(game, "machine");
            ResourceId supply = world.Content.ResourceId("supply"); PlayerId company = new PlayerId(0);
            DemandProofOwner owner = world.Services.Get<DemandProofOwner>(); LogisticsDemandId source = owner.Definition.Source;
            ILogisticsDemandRequests requests = world.Services.Get<ILogisticsDemandRequests>(); LogisticsService log = world.Services.Get<LogisticsService>();
            c.That(ReferenceEquals(log, requests), "generic requests alias the one existing logistics owner");
            c.Equal(4, world.Services.Providers<ILogisticsDemandOwner>().Count, "stock work transformation mod providers deduplicate service aliases");
            c.That(source.Identity > int.MaxValue, "JSON mod owns a positive 64-bit identity beyond entity/order int IDs");
            c.Equal("proof.demands", source.Scope, "source scope is authored, not inferred from resource or destination");
            c.That(!default(LogisticsDemandId).IsValid && !default(LogisticsDemandSourceCheck).IsWellFormed, "default identity or source facts never fabricate readiness");
            c.Throws<ArgumentException>(() => new LogisticsDemandId("Bad Scope", 1, Code.None), "noncanonical source scope refuses");
            c.Throws<ArgumentOutOfRangeException>(() => new LogisticsDemandId("proof.demands", 0, Code.None), "zero source identity refuses");
            c.Throws<ArgumentOutOfRangeException>(() => new LogisticsDemandId("proof.demands", -1, Code.None), "negative source identity refuses");
            ulong before = game.Hash(); var wanted = new[] { LogisticsSupplyRequest.Entity(destination.Id, Want(supply, Fix.FromInt(8))) };
            c.Equal(Code.None, requests.CheckRequests(source, company, destination.Id, wanted, int.MaxValue), "pure complete native request admission accepts explicit priority without an arbitrary ceiling");
            c.Equal(before, game.Hash(), "request preview creates no stock, promises, job or intent");
            c.Equal(LogisticsDemandCodes.UnknownOwner, requests.CheckRequests(new LogisticsDemandId("missing.owner", 1, Code.None), company, destination.Id, wanted, 1), "undeclared owner has no fallback");
            c.Equal(LogisticsDemandCodes.InvalidSource, requests.CheckRequests(new LogisticsDemandId(source.Scope, source.Identity + 1, Code.None), company, destination.Id, wanted, 1), "declared scope cannot fabricate owner workflow identity");
            c.Equal(Codes.NotOwned, requests.CheckRequests(source, new PlayerId(1), destination.Id, wanted, 1), "company mismatch refuses");
            c.Equal(Codes.NotOwned, requests.CheckRequests(source, company, machine.Id, wanted, 1), "source cannot redirect intent to another prototype");
            c.Equal(Codes.InvalidArgument, requests.CheckRequests(source, company, destination.Id, wanted, -1), "negative priority refuses");
            c.Equal(Codes.InvalidArgument, requests.CheckRequests(source, company, destination.Id, null, 1), "missing bill refuses");
            c.Equal(Codes.InvalidArgument, requests.CheckRequests(default, company, destination.Id, wanted, 1), "default source refuses");
            c.Equal(Codes.InvalidArgument, requests.SetRequest(source, company, destination.Id, supply, Fix.Half, 1), "fractional native count intent refuses rather than rounds");
            c.Equal(Codes.InvalidArgument, requests.SetRequest(source, company, destination.Id, supply, -Fix.One, 1), "negative quantity never silently withdraws");
            c.Throws<ArgumentException>(() => requests.WithdrawRequests(new LogisticsDemandId("missing.owner", 1, Code.None)), "withdrawal requires a declared source");
            c.Equal(before, game.Hash(), "all rejected intents leave the sole state unchanged");
            c.Equal(Code.None, requests.ReplaceRequests(source, company, destination.Id, wanted, 65), "whole bill installs actual native intent");
            var retained = Facts(game)[0]; c.Equal(Fix.FromInt(8), retained.Quantity, "private fact reports actual requested quantity");
            c.Equal(LogisticsDemandStatus.Active, retained.Check.Status, "private fact uses the live owner check");
            var hidden = new List<LogisticsDemandView>(Facts(game));
            PlayerView.Create(world, new PlayerId(1)).Feature<LogisticsView>().CopyDemands(hidden);
            c.Equal(0, hidden.Count, "foreign query clears private source facts");
            PlayerView.Create(world, PlayerId.None).Feature<LogisticsView>().CopyDemands(hidden);
            c.Equal(1, hidden.Count, "observer receives detached facts");
            Tick(game);
            c.Equal(Code.None, requests.ReplaceRequests(source, company, destination.Id, new[] { LogisticsSupplyRequest.Entity(destination.Id, Want(supply, Fix.FromInt(5))) }, 90), "replacing quantities uses the same request identity");
            c.Equal(retained.CreatedTick, Facts(game)[0].CreatedTick, "retained lines preserve original age");
            c.Equal(game.Tick - retained.CreatedTick, Facts(game)[0].AgeTicks, "age is actual elapsed game ticks");
            c.Equal(Fix.FromInt(8), retained.Quantity, "retained immutable facts do not follow later state mutation");
            c.Equal(Code.None, log.ConfigureStockPolicy(company, destination.Id, supply, LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(3), Fix.FromInt(3), Fix.FromInt(3), 4, LogisticsReserveAccess.Protected))), "player stock shares the same demand table");
            c.Equal(Fix.FromInt(8), log.DemandOf(destination.Id, supply), "distinct scope contributions aggregate without overwrite");
            c.Equal(32, SaveGame.Describe(game.Save()).Format, "open scoped source and age use current save format");
            c.Equal(StateCodecTests.ExpectedLogisticsSchema, log.StateSchemaVersion, "sole logistics service owns one current codec");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "64-bit owner identity and original age restore without a legacy reader");
            c.Equal(source, Facts(restored)[0].Source, "restored external source retains exact scope and long identity");
            c.Equal(Code.None, requests.ReplaceRequests(source, company, destination.Id, Array.Empty<LogisticsSupplyRequest>(), 90), "empty bill withdraws all absent source lines");
            c.Equal(Fix.FromInt(3), log.DemandOf(destination.Id, supply), "whole replacement preserves another owner's requests");
            c.Equal(Code.None, log.ConfigureStockPolicy(company, destination.Id, supply, LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.Zero, Fix.Zero, Fix.Zero, 4, LogisticsReserveAccess.Protected))), "explicit stock policy disables demand without another mutation path");
            c.Equal(Fix.Zero, log.DemandOf(destination.Id, supply), "disabled stock row contributes no live demand");
            Bill huge = Want(supply, Fix.FromInt(int.MaxValue));
            c.Equal(Code.None, requests.ReplaceRequests(source, company, destination.Id, new[] { LogisticsSupplyRequest.Entity(destination.Id, huge) }, 1), "standing desired quantity has no arbitrary world or capacity cap");
            before = game.Hash();
            c.Equal(Codes.InvalidArgument, log.ConfigureStockPolicy(company, destination.Id, supply, LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.One, Fix.One, 1, LogisticsReserveAccess.Protected))), "aggregate fixed-point representational overflow refuses before mutation");
            c.Equal(before, game.Hash(), "overflow preflight changes no requests");
            requests.WithdrawRequests(source);
            c.Throws<ContentException>(() => New(Factory(module: new DemandProofModule(duplicate: true))), "duplicate declared scope refuses before tick one");
            c.Throws<ContentException>(() => New(Factory(module: new DemandProofModule(foreign: world))), "foreign-session owner refuses before tick one");
            c.Throws<ContentException>(() => New(Factory(Json.Replace("\"identity\": 4294967297", "\"identity\": 1.5"))), "authored source ID must be an exact long");
            c.Throws<ContentException>(() => New(Factory(Json.Replace("\"active\": true", "\"active_typo\": true"))), "missing explicit provider lifecycle refuses");
            c.Throws<ContentException>(() => New(Factory(Json.Replace("\"supply\": 8", "\"supply\": 0.5"))), "JSON native count bill cannot fabricate fractional units");
            WholeBill(c); CorruptRecords(c); NativeOwners(c);
        }
        private static void WholeBill(Check c)
        {
            JsonValue defs = Content.Json.Parse(PointGame.Definitions);
            defs.Get("resources").Add(Content.Json.Parse("{\"id\":\"other\",\"unit\":\"unit.supply\",\"category\":\"general\",\"phase\":\"solid\",\"traits\":[],\"capacity\":{\"unit.supply\":1}}"));
            var text = new StringBuilder(); defs.WriteCanonical(text);
            SimGame game = New(Factory(definitions: text.ToString())); Entity destination = Find(game, "destination");
            var owner = game.World.Services.Get<DemandProofOwner>(); var requests = game.World.Services.Get<ILogisticsDemandRequests>();
            ResourceId supply = game.World.Content.ResourceId("supply"), other = game.World.Content.ResourceId("other");
            requests.ReplaceRequests(owner.Definition.Source, new PlayerId(0), destination.Id, new[] { LogisticsSupplyRequest.Entity(destination.Id, Want(supply, Fix.FromInt(8))) }, 1);
            ulong before = game.Hash(); Bill mixed = new Bill(new[] { new ResourceAmount(supply, Fix.One), new ResourceAmount(other, Fix.One) });
            c.Equal(Codes.InvalidArgument, requests.ReplaceRequests(owner.Definition.Source, new PlayerId(0), destination.Id, new[] { LogisticsSupplyRequest.Entity(destination.Id, mixed) }, 2), "one incompatible native line rejects the entire replacement");
            c.Equal(before, game.Hash(), "whole-set refusal preserves every previous line and its age");
            c.Equal(Fix.FromInt(8), Facts(game)[0].Quantity, "first valid line is not partially installed");
        }
        private static void CorruptRecords(Check c)
        {
            foreach (string kind in new[] { "future", "duplicate", "unknown", "fraction", "wrong-binding", "overflow" })
            {
                SimGame game = New(Factory()); var log = game.World.Services.Get<LogisticsService>(); Entity target = Find(game, "destination");
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); int next = 1; writer.Int(ref next);
                    writer.Count(kind == "duplicate" || kind == "overflow" ? 2 : 1);
                    for (int i = 0; i < (kind == "duplicate" || kind == "overflow" ? 2 : 1); i++)
                    {
                        EntityId destination = target.Id; ResourceId resource = game.World.Content.ResourceId("supply");
                        Fix quantity = kind == "fraction" ? Fix.Half : kind == "overflow" ? Fix.FromInt(int.MaxValue) : Fix.One;
                        int priority = 1; PlayerId company = new PlayerId(0); long created = kind == "future" ? game.Tick + 1 : 0;
                        LogisticsDemandId source = kind == "unknown" ? new LogisticsDemandId("missing.owner", 1, Code.None) : kind == "wrong-binding" ? new LogisticsDemandId("proof.demands", 2, Code.None)
                            : i == 1 && kind == "overflow" ? new LogisticsDemandId(LogisticsService.StockDemandScope, target.Id.Value, Code.None) : game.World.Services.Get<DemandProofOwner>().Definition.Source;
                        writer.Entity(ref destination); writer.Resource(ref resource);
                        LogisticsDemandSettings.Sync(writer, new LogisticsDemandSettings(true, quantity, quantity, quantity, priority, LogisticsReserveAccess.Protected)); bool replenishing = false;
                        writer.Bool(ref replenishing); writer.Player(ref company);
                        LogisticsDemandId.Sync(writer, ref source); writer.Long(ref created);
                    }
                    writer.Count(0); writer.Flush(); byte[] bytes = stream.ToArray();
                    c.Throws<CorruptStateException>(() => { log.Sync(new StateReader(bytes, 0, bytes.Length)); log.ValidateLoadedDemands(); }, "sole codec rejects " + kind + " source records");
                }
            }
        }
        private static void NativeOwners(Check c)
        {
            SimGame game = New(Factory()); Entity destination = Find(game, "source"), machine = Find(game, "machine"); machine.Get<IWorker>().Held = true;
            var board = game.World.Services.Get<WorkBoard>(); var log = game.World.Services.Get<LogisticsService>(); ResourceId supply = game.World.Content.ResourceId("supply");
            Order order = board.Post(game.World.Content.Get<ActivityDef>("inspect"), destination.Owner, destination.Id, 1);
            c.Equal((long)order.Id, (long)destination.Id.Value, "different declared scopes may use the same numeric identity without collision");
            log.RequestInputs(order.Id, destination, new[] { LogisticsSupplyRequest.Entity(destination.Id, Want(supply, Fix.FromInt(2))) }, 1);
            c.Equal(Code.None, log.ConfigureStockPolicy(destination.Owner, destination.Id, supply, LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.One, Fix.One, 1, LogisticsReserveAccess.Protected))), "real native order and player stock coexist");
            c.Equal(Fix.FromInt(3), log.DemandOf(destination.Id, supply), "source owner no longer relies on a closed enum");
            long created = Facts(game)[0].CreatedTick; Tick(game); log.RequestInputs(order.Id, destination, new[] { LogisticsSupplyRequest.Entity(destination.Id, Want(supply, Fix.FromInt(4))) }, 1);
            c.Equal(created, Facts(game)[0].CreatedTick, "work request refresh retains age rather than withdraw/recreate");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "native order cancels through its actual engine command");
            c.Equal(Fix.One, log.DemandOf(destination.Id, supply), "ending actual order removes only native work intent");

            GameFactory colony = Colony.Factory(); SimGame transformation = colony.NewGame(new NewGameRequest { Scenario = "colony", Seed = 31 });
            Entity factory = transformation.World.Entities.Get(Colony.Mine(transformation, 0, "factory")[0].Id);
            c.That(TestKit.Order(transformation, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = factory.Id, Transformation = "build_truck" }).Accepted, "transformer has a real authoritative queued transformation");
            var requests = transformation.World.Services.Get<ITransformationSupplyRequests>(); var native = transformation.World.Services.Get<LogisticsService>(); ResourceId steel = transformation.World.Content.ResourceId("steel");
            ITransformer provider = factory.Get<ITransformer>(new Code("transformation"));
            requests.RequestInputs(provider, new[] { LogisticsSupplyRequest.Entity(factory.Id, Want(steel, Fix.FromInt(10))) }, 2); long age = native.Demands[0].CreatedTick;
            requests.RequestInputs(provider, new[] { LogisticsSupplyRequest.Entity(factory.Id, Want(steel, Fix.FromInt(3))) }, 5);
            c.Equal(age, native.Demands[0].CreatedTick, "transformer refresh retains sole request age");
            c.Equal(new LogisticsDemandId(TransformationModule.ModuleId, factory.Id.Value, provider.CapabilityId), native.Demands[0].Source, "transformation system owns actual named transformer source identity");
            requests.RequestInputs(provider, Array.Empty<LogisticsSupplyRequest>(), 5);
            c.Equal(Fix.Zero, native.DemandOf(factory.Id, steel), "complete changed transformation bill withdraws obsolete native inputs");
            c.Equal(transformation.Hash(), colony.Load(transformation.Save()).Hash(), "native transformer owner alias has no duplicate state codec");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory), isolated = New(factory);
            Entity source = Find(game, "source"), destination = Find(game, "destination"), machine = Find(game, "machine");
            ResourceId supply = game.World.Content.ResourceId("supply"); IInventory stock = source.Get<IInventory>(), dest = destination.Get<IInventory>(), cargo = machine.Get<ITransporter>().Cargo;
            c.That(TestKit.Order(game, 0, new SubmitProofDemandCommand()).Accepted, "player requests JSON-authored external workflow through ordinary engine command");
            c.Equal(Fix.Zero, dest.Amount(supply), "request creates no instant delivery");
            SimGame posted = factory.Load(game.Save()); c.Equal(game.Hash(), posted.Hash(), "posted external intent saves before physical dispatch");
            Reach(game, () => cargo.Amount(supply).IsPositive && machine.Get<PointPresence>().Position.IsPositive, "external request pickup and travel", posted, isolated);
            c.Equal(game.Hash(), posted.Hash(), "saved posted request dispatches and travels identically");
            c.Equal(Fix.FromInt(8), cargo.Amount(supply), "shared selector loads actual authored native quantity");
            c.Equal(Fix.FromInt(12), stock.Amount(supply), "pickup subtracts actual source inventory");
            c.Equal(Fix.Zero, dest.Amount(supply), "loaded vehicle has not teleported cargo to destination");
            Job job = game.World.Services.Get<WorkBoard>().Job(machine.Get<IWorker>().Job);
            c.That(job.CargoOutReservation.IsValid && job.InReservation.IsValid, "existing haul owns actual protected cargo and destination receipts");
            c.Equal(Fix.FromInt(8), dest.ReservedIn(supply), "real destination capacity remains committed");
            SimGame mid = factory.Load(game.Save()); c.Equal(game.Hash(), mid.Hash(), "loaded physical mod haul saves exact progress and claims");
            c.That(TestKit.Order(game, 0, new RetireProofDemandCommand()).Accepted, "source lifecycle ends through engine command without withdrawing live haul claims");
            c.That(TestKit.Order(mid, 0, new RetireProofDemandCommand()).Accepted, "saved twin applies identical source retirement");
            c.Equal(Code.None, game.Seat(0).View.Feature<LogisticsView>().ReadStockTarget(destination.Id, supply, out var target), "visible stock target available");
            c.Equal(Fix.Zero, target, "expired intent stops contributing immediately before periodic prune");
            c.Equal(Fix.FromInt(8), cargo.Amount(supply), "retirement does not erase loaded cargo");
            c.Equal(Fix.FromInt(8), dest.ReservedIn(supply), "retirement does not release another physical execution's receipt");
            c.Equal(game.Hash(), mid.Hash(), "retired partial loaded twins remain identical");
            var retired = Facts(game); c.That(retired.Count == 0 || retired[0].Check.Status == LogisticsDemandStatus.Expired, "current diagnostics report expiry or the ordinary phase has pruned it");
            SimGame expired = factory.Load(game.Save()); c.Equal(game.Hash(), expired.Hash(), "retired provider state restores before logistics owner validation");
            Reach(game, () => dest.Amount(supply) == Fix.FromInt(8) && cargo.Amount(supply).IsZero, "retired committed physical unload", expired, isolated);
            c.Equal(game.Hash(), expired.Hash(), "retirement never revives intent on saved continuation");
            c.Equal(Fix.FromInt(20), stock.Amount(supply) + dest.Amount(supply) + cargo.Amount(supply), "physical delivery conserves all native inventory");
            c.Equal(0, cargo.Reservations.Count, "completed hauling has no orphan cargo claims");
            c.Equal(0, dest.Reservations.Count, "completed hauling consumes destination claim exactly");
            c.Equal(Fix.Zero, Find(isolated, "destination").Get<IInventory>().Amount(supply), "interleaved session without request receives no borrowed deliveries");
            c.Equal(Fix.FromInt(20), Find(isolated, "source").Get<IInventory>().Amount(supply), "interleaved session retains its own native supply");
            c.Equal(0, Facts(isolated).Count, "another session owns no shared request table");
            c.Equal(LogisticsDemandCodes.Expired, game.Seat(0).Preview(new SubmitProofDemandCommand()), "retired workflow cannot silently resubmit");
            Reach(game, () => Facts(game).Count == 0, "ordinary expired intent pruning", expired);
            for (int i = 0; i < 20; i++) { Tick(game); Tick(expired); }
            c.Equal(Fix.FromInt(8), dest.Amount(supply), "finished retired intent cannot produce duplicate loads later");
            c.Equal(game.Hash(), expired.Hash(), "retired delivered checkpoint remains deterministic");
            Withdrawal(c, factory); ExpiredIntent(c, Factory(Json.Replace("\"destination\": \"destination\"", "\"destination\": \"source\"")));
        }
        private static void Withdrawal(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity destination = Find(game, "destination"), machine = Find(game, "machine"); ResourceId supply = game.World.Content.ResourceId("supply");
            TestKit.Order(game, 0, new SubmitProofDemandCommand());
            Reach(game, () => machine.Get<ITransporter>().Cargo.Amount(supply).IsPositive, "withdrawal loaded checkpoint");
            c.That(TestKit.Order(game, 0, new RetireProofDemandCommand { Withdraw = true }).Accepted, "explicit complete-source withdrawal changes intent only");
            c.Equal(0, Facts(game).Count, "withdrawal removes sole intent immediately");
            c.Equal(Fix.FromInt(8), destination.Get<IInventory>().ReservedIn(supply), "withdrawal preserves acquired destination receipt");
            SimGame twin = factory.Load(game.Save());
            Reach(game, () => destination.Get<IInventory>().Amount(supply) == Fix.FromInt(8), "withdrawn live commitment delivery", twin);
            c.Equal(game.Hash(), twin.Hash(), "withdrawn live cargo finishes through unchanged saved haul execution");
        }
        private static void ExpiredIntent(Check c, GameFactory factory)
        {
            SimGame game = New(factory); var owner = game.World.Services.Get<DemandProofOwner>(); var requests = game.World.Services.Get<ILogisticsDemandRequests>(); Entity target = Find(game, "source");
            requests.ReplaceRequests(owner.Definition.Source, new PlayerId(0), target.Id, new[] { LogisticsSupplyRequest.Entity(target.Id, owner.Definition.Quantities) }, 1);
            ResourceId supply = game.World.Content.ResourceId("supply");
            c.Equal(Fix.FromInt(12), game.World.Services.Get<LogisticsService>().Spare(target.Get<IOutletPort>(), supply), "active desired stock reduces actual unpromised export supply");
            owner.Retire(); ulong before = game.Hash();
            c.Equal(LogisticsDemandStatus.Expired, Facts(game)[0].Check.Status, "unpruned expiry is an explicit source fact");
            c.Equal(Fix.Zero, game.World.Services.Get<LogisticsService>().DemandOf(target.Id, game.World.Content.ResourceId("supply")), "unpruned expiry cannot reserve desired stock");
            c.Equal(Fix.FromInt(20), game.World.Services.Get<LogisticsService>().Spare(target.Get<IOutletPort>(), supply), "expired desired stock stops blocking supply immediately");
            c.Equal(before, game.Hash(), "expired view and total queries remain pure");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "explicit expired intent can save until next normal planning phase");
            c.Equal(LogisticsDemandStatus.Expired, Facts(twin)[0].Check.Status, "restore never substitutes active owner lifecycle");
            Reach(game, () => Facts(game).Count == 0, "expired without acquired hauls", twin);
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "retired unacquired intent fabricates no delivery");
            c.Equal(game.Hash(), twin.Hash(), "normal expiry pruning continues identically");
        }
    }
}
