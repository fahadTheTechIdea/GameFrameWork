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
using static Beep.SimEngine.Tests.PlanetXProductionTests;

namespace Beep.SimEngine.Tests
{
    // JSON input variants, normal player commands/views/clock/saves and verification assertions only.
    internal static class BidirectionalPortTests
    {
        internal const string Case = "Bidirectional ports: universal inlet contracts explicit compatibility and timed structure cargo return trips";
        private static readonly string[] Names = { "source", "receiver", "smelter", "assembler", "carrier", "cargo2" };
        private static readonly Code Port = new Code("ports.stock"), Same = new Code("ports.same");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void ConfigureStorage(JsonValue parts, bool independent)
        {
            parts.Get("storage").Set("phases", Json.Parse("[\"solid\"]"));
            JsonValue ports = parts.Get("inventory_port") ?? JsonValue.NewArray();
            ports.Add(Json.Parse("{\"id\":\"ports.stock\"}")); ports.Add(Json.Parse("{\"id\":\"ports.same\"}")); parts.Set("inventory_port", ports);
            if (independent) { parts.Set("independent_storage", parts.Get("storage")); parts.Remove("storage"); }
        }
        private static GameFactory Create(BidirectionalPortModule module, bool independent)
            => Factory(ports: module, worldEdit: world => {
                for (int i = 0; i < world.Get("entities").Count; i++) ConfigureStorage(world.Get("entities")[i].Get("components"), independent);
                var source = world.Get("entities")[0].Get("components").Get(independent ? "independent_storage" : "storage");
                source.Set("phases", Json.Parse("[\"solid\",\"liquid\"]")); source.Get("initial_stock").Set("space.incompatible-fluid", JsonValue.Number("1"));
                world.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("ports.start"));
            }, portEdit: authored => {
                if (!independent) return;
                var parts = authored.Get("entities")[0].Get("components"); parts.Set("independent_storage", parts.Get("storage")); parts.Remove("storage");
            });
        private static List<WorkJobView> Jobs(SimGame game)
        { var facts = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(facts); return facts; }
        private static Fix Total(SimGame game, string resource) => Names.Aggregate(Fix.Zero, (sum, name) => sum + Stock(game, name, resource));
        private static void Advance(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("bidirectional update=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
            if (Total(game, "planetx.ore") != Fix.FromInt(30) || Total(game, "blank") != Fix.FromInt(12)
                || Total(game, "crucible") != Fix.One || Total(game, "fixture") != Fix.One || Total(game, "incompatible-fluid") != Fix.One)
                throw new InvalidOperationException("actual stock conservation failed at update=" + game.Clock.StepIndex);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 256 && !ready(); i++) Advance(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " exceeded256Updates, step=" + game.Clock.StepIndex
                + ", jobs=" + string.Join(";", Jobs(game).Select(x => x.Id + "/" + x.State + "/" + x.Blocker + "/" + x.Loaded))
                + ", ore=" + string.Join(";", Names.Select(x => x + "/" + Stock(game, x, "planetx.ore")))
                + ", positions=" + string.Join(";", Names.Select(x => x + "/" + ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Actor(game, x)).Spatial).Position)));
        }
        private static SimGame Input(Check c, GameFactory factory, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary input uses original admission");
            c.Equal(before, game.Hash(), "preview changes no state/RNG/receipts"); game.Seat(0).Submit(command); return factory.Load(game.Save());
        }
        private static ResourcePortSnapshot ReadPort(Check c, SimGame game, string name, string resource, Code? port = null)
        {
            c.Equal(Code.None, game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Actor(game, name), port ?? Port, Resource(game, resource), out var facts), "actual named endpoint facts " + name);
            return facts;
        }
        private static void Run(Check c)
        {
            c.That(typeof(IResourceInlet).IsAssignableFrom(typeof(IResourceOutlet)) && typeof(IInletPort).IsAssignableFrom(typeof(IOutletPort)), "all outgoing generic/entity contracts universally include receiving");
            c.That(typeof(IInletPort).IsAssignableFrom(typeof(IInventory)) && typeof(IInletPort).IsAssignableFrom(typeof(ITransporter).GetProperty("Cargo").PropertyType), "transport cargo contract includes the same inlet/outlet inventory");
            c.That(typeof(IResourcePort).GetMethod("ResolveStore").ReturnType==typeof(ResourcePortResolution)&&
                typeof(IResourceInlet).IsAssignableFrom(typeof(ResourcePortResolution).GetProperty("Store").PropertyType),
                "typed named availability resolves an actual receiving store, never a parallel port-only buffer");
            foreach (Type type in typeof(IResourceOutlet).Assembly.GetTypes().Where(t => typeof(IResourceOutlet).IsAssignableFrom(t)))
                c.That(typeof(IResourceInlet).IsAssignableFrom(type), "compiled generic outlet inherits inlet: " + type.FullName);
            foreach (Type type in typeof(IOutletPort).Assembly.GetTypes().Where(t => typeof(IOutletPort).IsAssignableFrom(t)))
                c.That(typeof(IInletPort).IsAssignableFrom(type), "compiled entity outlet inherits inlet: " + type.FullName);
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
        }
        private static void Physical(Check c, bool independent)
        {
            var proof = new BidirectionalPortModule(); var factory = Create(proof, independent); var game = New(factory);
            c.That(proof.ActualAliases && proof.Initialize.Initializations == 1 && game.Modules.Count == 10, "explicit master composes shared native/independent inventory aliases once");
            c.That(game.Modules.All(x => x == ContentAccessModule.ModuleId || x == InventoryModule.ModuleId || x == OperationsModule.ModuleId
                || x == MotionModule.ModuleId || x == WorkModule.ModuleId || x == LogisticsModule.ModuleId || x == TransformationModule.ModuleId
                || x == WorldSetupModule.ModuleId || x == "points" || x == proof.Id), "only authored physical modules selected; no economy/supplier/admin/instant provider");
            var inventory = game.Seat(0).View.Feature<InventoryView>(); var ore = Resource(game, "planetx.ore"); var fluid = Resource(game, "incompatible-fluid");
            foreach (string name in Names)
            {
                string accepted = name == "assembler" ? "blank" : "planetx.ore";
                c.That(inventory.StoreAccepts("space." + name, Resource(game, accepted), Code.None), "actual inlet prototype accepts compatible resource " + name);
                c.That(inventory.TryGetCapacity(Actor(game, name), Code.None, out _) && inventory.TryGetReservations(Actor(game, name), Code.None, out var receipts) && receipts.Count == 0, "actual exporting endpoint exposes inlet capacity/promises " + name);
                var first = ReadPort(c, game, name, accepted); var same = ReadPort(c, game, name, accepted, Same);
                c.That(first.Endpoint == ResourceEndpointId.ForEntity(Actor(game, name)) && first.Endpoint == same.Endpoint
                    && first.Version == same.Version && first.Stock.AmountOf(Resource(game, accepted)) == Stock(game, name, accepted), "both names resolve original one stock owner " + name);
            }
            c.That(inventory.StoreAccepts("space.source", fluid, Code.None), "source explicit liquid phase allows its authored fluid");
            c.That(!inventory.StoreAccepts("space.receiver", fluid, Code.None) && !inventory.StoreAccepts("space.carrier", fluid, Code.None) && !inventory.StoreAccepts("space.cargo2", fluid, Code.None), "receiving/exporting capability never implies accepting an incompatible phase");
            c.That(!inventory.StoreAccepts("space.cargo2", Resource(game, "blank"), Code.None), "explicit cargo resource filter excludes another solid");
            var retained = ReadPort(c, game, "source", "planetx.ore");
            ulong before = game.Hash();
            c.Equal(StandardCodes.NotAccepted, inventory.ReadResourcePort(Actor(game, "receiver"), Port, fluid, out _), "named receiving metadata rejects explicit incompatible phase");
            c.Equal(StandardCodes.NotAccepted, inventory.ReadResourcePort(Actor(game, "receiver"), new Code("ports.missing"), ore, out _), "missing named port has no local stock fallback");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<InventoryView>().ReadResourcePort(Actor(game, "source"), Port, ore, out _), "foreign port read has no private stock");
            var invalid = new HaulResourcesCommand { Source = Actor(game, "source"), Destination = Actor(game, "receiver"), Worker = Actor(game, "carrier"), SourcePort = Port, DestinationPort = Port, Resource = fluid, Ceiling = Fix.One, Priority = 70 };
            c.Equal(StandardCodes.NotAccepted, game.Seat(0).Preview(invalid), "ordinary haul refuses incompatible receiving phase before stock/receipt acquisition");
            invalid.Resource = ore; invalid.DestinationPort = new Code("ports.missing");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(invalid), "ordinary haul refuses missing named receiving port without fallback");
            c.Equal(before, game.Hash(), "all refused compatibility/private previews preserve state/RNG/receipts");
            c.Equal(Fix.One, Stock(game, "source", "incompatible-fluid"), "refused fluid remains original actual source stock");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var replay = game.RecordReplay(limits);
            var twin = Input(c, factory, game, new SetWorkerHeldCommand { Worker = Actor(game, "cargo2"), Held = true }); Advance(game, twin);
            Transfer(c, factory, game, ref twin, "source", "receiver", Fix.FromInt(4));
            c.Equal(Fix.FromInt(4), Stock(game, "receiver", "planetx.ore"), "structure inlet actually receives physical stock");
            Transfer(c, factory, game, ref twin, "receiver", "source", Fix.FromInt(4));
            c.Equal(Fix.FromInt(30), Stock(game, "source", "planetx.ore"), "same structure outlet/inlet receives its physical return");
            Transfer(c, factory, game, ref twin, "source", "cargo2", Fix.FromInt(2));
            c.Equal(Fix.FromInt(2), Stock(game, "cargo2", "planetx.ore"), "held physical transporter is a real compatible inlet");
            Transfer(c, factory, game, ref twin, "cargo2", "source", Fix.FromInt(2));
            c.Equal(Fix.Zero, Stock(game, "cargo2", "planetx.ore"), "same transport cargo is an actual outlet on the return trip");
            c.Equal(Fix.FromInt(30), retained.Stock.AmountOf(ore), "retained port facts never become live balance aliases");
            c.Equal(Fix.FromInt(30), Stock(game, "source", "planetx.ore"), "complete native amount returns without duplication");
            foreach (string name in Names) c.That(inventory.TryGetReservations(Actor(game, name), Code.None, out var receipts) && receipts.Count == 0, "all physical inlet/outlet receipts settled " + name);
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal two-way chronology restores once");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "normal held/movement/handling/two-way cargo command stream replays");
            var isolated = New(factory); Advance(isolated); Advance(game, twin);
            c.That(Stock(isolated, "source", "planetx.ore") == Fix.FromInt(30) && Stock(isolated, "cargo2", "planetx.ore") == Fix.Zero, "interleaved session has no stock/receipt leakage");
        }
        private static void Transfer(Check c, GameFactory factory, SimGame game, ref SimGame twin, string source, string destination, Fix amount)
        {
            Fix baseline = Stock(game, destination, "planetx.ore"); long at = game.Clock.StepIndex;
            var command = new HaulResourcesCommand { Source = Actor(game, source), Destination = Actor(game, destination), Worker = Actor(game, "carrier"),
                SourcePort = Port, DestinationPort = Same, Resource = Resource(game, "planetx.ore"), Ceiling = amount, Priority = 70 };
            twin = Input(c, factory, game, command);
            c.Equal(baseline, Stock(game, destination, "planetx.ore"), "pending player input never performs instant delivery");
            Advance(game, twin); c.Equal(baseline, Stock(game, destination, "planetx.ore"), "first update cannot bypass physical loading/travel/handling");
            Until(game, twin, () => Stock(game, "carrier", "planetx.ore") > Fix.Zero && Stock(game, "carrier", "planetx.ore") < amount, source + "→" + destination + " partial loading");
            var job = Jobs(game).Single(x => x.IsHaul && x.State != JobState.Done && x.State != JobState.Cancelled && x.State != JobState.Failed);
            c.That(job.SourceEndpoint == ResourceEndpointId.ForEntity(Actor(game, source)) && job.DestinationEndpoint == ResourceEndpointId.ForEntity(Actor(game, destination))
                && job.SourcePort == Port && job.DestinationPort == Same && !job.Purchase, "ordinary job retains exact one-owner bidirectional endpoints and explicit names");
            var partial = ReadPort(c, game, "carrier", "planetx.ore");
            c.Equal(Stock(game, "carrier", "planetx.ore"), partial.Stock.AmountOf(Resource(game, "planetx.ore")), "actual cargo inlet balance is partial physical stock");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial loading/receiving receipts restore through original owner");
            Until(game, twin, () => Stock(game, destination, "planetx.ore") > baseline && Stock(game, destination, "planetx.ore") < baseline + amount,
                source + "→" + destination + " partial physical unloading");
            Fix unloading = ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(x => x.Id == Actor(game, "carrier")).Spatial).Position;
            Fix endpoint = ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(x => x.Id == Actor(game, destination)).Spatial).Position;
            c.Equal(endpoint - (destination == "cargo2" ? Fix.FromInt(2) : Fix.Zero), unloading, "actual unloading is physically at endpoint or authored service bay");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial unloading restores original two-way balances and receipts");
            Until(game, twin, () => Stock(game, destination, "planetx.ore") == baseline + amount && Stock(game, "carrier", "planetx.ore") == Fix.Zero
                && !Jobs(game).Any(x => x.Id == job.Id), source + "→" + destination + " physical delivery and live-job settlement");
            c.That(game.Clock.StepIndex > at + 2, "delivery and live-job settlement require physical game time, never instant transfer");
            c.That(partial.Stock.AmountOf(Resource(game, "planetx.ore")) > Fix.Zero, "detached partial cargo facts survive complete unload");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed direction restores with no initial-stock regrant");
        }
    }
}
