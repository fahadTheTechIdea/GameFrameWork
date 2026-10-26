using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Authored inputs, existing engine commands/execution contracts and observations only.
    public static class InventorySignalTests
    {
        private const string Native = "stock.assembler", Independent = "stock.independent";
        private static readonly Code Feed = new Code("feed"), Operation = new Code("transformation");
        public static void Register()
        {
            TestRunner.Add("Resource signals: inventory and network contracts and private facts", Contracts);
            TestRunner.Add("Resource signals: inventory and network controls and saved continuation", Execution);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        internal static GameFactory Factory(Action<JsonValue> edit = null, Action<JsonValue> processEdit = null)
        {
            var definition = new GameDefinition("inventory-signals", new GameModule[] { new InventoryModule(), new OperationsModule(),
                new ContentAccessModule(), new TransformationModule(), new WorldSetupModule(), new AutomationModule(), new InventorySignalsModule(),
                new ProcessProofModule(), new ResourceFlowsModule() });
            JsonValue process = Json.Parse(FrameworkProofContent.Read("process.json"));
            process.Get("resources")[0].Get("capacity").Set("unit.capacity.mass", JsonValue.Number("1"));
            process.Get("scenarios")[0].Get("settings").Get(WorldSetupModule.ModuleId).Set("profile", JsonValue.Of("inventory-signals.start"));
            processEdit?.Invoke(process);
            var scenario = process.Get("scenarios")[0]; scenario.Set("seats", JsonValue.Number("2")); scenario.Set("default_seats", JsonValue.Number("2"));
            foreach (string module in new[] { InventorySignalsModule.ModuleId, AutomationModule.ModuleId, ResourceFlowsModule.ModuleId })
                scenario.Get("modules").Add(JsonValue.Of(module));
            JsonValue manifest = Json.Parse(FrameworkProofContent.Read("process-manifest.json"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"inventory-signals.json\"]}"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"flows.json\"]}"));
            JsonValue inputs = Json.Parse(FrameworkProofContent.Read("inventory-signals.json")); edit?.Invoke(inputs);
            var files = new PreloadedFileSource().Add("manifest.json", Text(manifest)).Add("process.json", Text(process))
                .Add("framework/access.json", FrameworkProofContent.Access).Add("inventory-signals.json", Text(inputs))
                .Add("flows.json", FrameworkProofContent.Read("endpoints-flows.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "process", Seed = 331 });
        private static Entity Find(SimGame game, string prototype = Native, int company = 0)
        {
            foreach (Entity entity in game.World.Entities.All)
                if (entity.Alive && entity.Def.Id == prototype && entity.Owner == new PlayerId(company)) return entity;
            throw new InvalidOperationException("missing inventory signal entity " + prototype);
        }
        private static IInventory Stock(SimGame game, string prototype = Native) => Find(game, prototype).Get<IInventory>();
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static ResourceId Fluid(SimGame game) => game.World.Content.ResourceId("fluid");
        private static OperationsView View(SimGame game, int company = 0) => game.Seat(company).View.Feature<OperationsView>();
        private static AutomationSignalSnapshot Signal(SimGame game, string signal, string prototype = Native)
        {
            Code refusal = View(game).ReadSignal(Find(game, prototype).Id, Feed, signal, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("signal view refused " + refusal); return value;
        }
        private static bool Ready(SimGame game, string signal, string prototype = Native)
        {
            var value = Signal(game, signal, prototype);
            if (!value.Available) throw new InvalidOperationException("expected an available readiness fact, got " + value.Blocker);
            return value.Value == Fix.One;
        }
        private static Fix Quantity(SimGame game, string signal, string prototype = Native)
        {
            var value = Signal(game, signal, prototype);
            if (!value.Available || value.Kind != AutomationSignalKind.Number)
                throw new InvalidOperationException("expected exact quantity " + signal + ": " + value.Blocker);
            return value.Value.Value;
        }
        private static TransformationSnapshot State(SimGame game, string prototype = Native)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game, prototype).Id, Operation, out var value))
                throw new InvalidOperationException("missing process snapshot"); return value;
        }
        private static AutomationRuleSnapshot Rule(SimGame game)
        {
            if (!View(game).TryGetAutomationRule(new PlayerId(0), "feed", out var value)) throw new InvalidOperationException("missing stock control"); return value;
        }
        private static bool Enabled(SimGame game, string prototype = Native)
        { return View(game).TryGetControl(Find(game, prototype).Id, Operation, out var value) && value.Enabled; }
        private static CommandResult Configure(SimGame game, string prototype = Native)
            => TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "feed", Settings = new AutomationSettings("stock.run", true,
                new ControlBinding(Find(game, prototype).Id, Operation), new Dictionary<string, ControlBinding> {
                    ["ready"] = new ControlBinding(Find(game, prototype).Id, Feed) }, ControlThresholds.Initial(game.World.Content.Get<IControlRule>("stock.run")),
                    game.World.Content.Get<IControlRule>("stock.run").WhenTrue, game.World.Content.Get<IControlRule>("stock.run").WhenFalse) });
        private static CommandResult Queue(SimGame game, int count = 1, string prototype = Native)
            => TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = Find(game, prototype).Id, Capability = Operation, Transformation = "build", Count = count });
        private static void Tick(SimGame game, int count = 1)
        { for (int i = 0; i < count; i++) { game.Update(); if (game.Fault != null) throw new InvalidOperationException("inventory signal tick=" + game.Tick + ": " + game.Fault); } }
        private static void Reach(SimGame game, Func<bool> reached, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 150 && !reached(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!reached()) throw new InvalidOperationException(stage + " timeout tick=" + game.Tick + " phase=" + State(game).Phase + " blocker=" + State(game).Blocker);
        }
        private static ResourceFlowPage History(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current semantics have exact build identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no existing state owner or payload was duplicated");
            c.That(game.World.Space == null && !game.World.Services.Has<WorkBoard>(), "inventory signals require no spatial or work subsystem");
            c.That(Find(game).Get<IInventory>() is Storage && Find(game, Independent).Get<IInventory>() is SingleResourcePort,
                "unrelated inventory implementations remain their own sole stock authorities");
            c.That(View(game).TryGetSignalSchema(Find(game).Id, Feed, out var schema) && schema.Version == 3 && schema.Bindings.Count == 8,
                "named source publishes fixed readiness and explicitly unit-parameterized quantity contracts");
            foreach (string prototype in new[] { Native, Independent })
                foreach (string signal in new[] { "stock.ready", "available.ready", "incoming.ready" })
                    c.That(Ready(game, signal, prototype), "actual initial inventory admits " + prototype + "/" + signal);
            ulong hash = game.Hash(); var retained = Signal(game, "stock.ready");
            c.Equal(new Code("unit.boolean"), retained.Unit, "boolean readiness never fabricates a dimensionless numeric stock unit");
            c.Equal(hash, game.Hash(), "all signal reads create no claims, balances, clocks or cached readiness");
            c.Equal(Codes.NotOwned, View(game, 1).ReadSignal(Find(game).Id, Feed, "stock.ready", out var hidden), "foreign-company facts refuse before provider reads");
            c.That(!hidden.Value.HasValue, "private refusal does not disclose stock readiness");
            c.Equal(Codes.NotFound, View(game).ReadSignal(new EntityId(99999), Feed, "stock.ready", out _), "missing entities are not substituted");
            c.Equal(Codes.UnknownDef, View(game).ReadSignal(Find(game).Id, Feed, "missing", out _), "missing signal is not a zero reading");
            c.That(!Signal(game, "process.backlog").Available, "incompatible declared schema is explicitly unavailable");
            c.Equal(Code.None, View(game).ReadSignal(Find(game).Id, new Code("missing"), "stock.ready", out var missing), "existing typed reader handles missing named source");
            c.Equal(OperationCodes.SignalUnavailable, missing.Blocker, "missing named source never falls back to first inventory");
            c.Equal(Code.None, game.Observer.Feature<OperationsView>().ReadSignal(Find(game).Id, Feed, "stock.ready", out var observed), "explicit observer can read detached facts");
            c.That(observed.Available, "observer receives real readiness, not a live stock handle");
            var definition = Find(game).Def.Component<InventorySignalDef>(Feed);
            c.That(ReferenceEquals(definition.Inventory, Find(game).Def.Component<IInventoryDefinition>()), "signal metadata aliases the exact existing inventory definition");
            c.Throws<NotSupportedException>(() => ((IList<ResourceAmount>)definition.Quantities.Lines).Clear(), "authored bill is immutable across sessions");
            var requester = new ReservationOwner("proof.signal", 1);
            ResourceClaimResult claims = game.World.Services.Get<IResourceTransaction>().AcquireClaims(new PlayerId(0), new[] {
                new ResourceClaimIntent(ReservationDirection.Outbound, requester, Stock(game).Endpoint, new Bill(new[] { new ResourceAmount(Supply(game), Fix.FromInt(13)), new ResourceAmount(Fluid(game), Fix.FromInt(2)) })) }, System.Array.Empty<ResourceReservation>());
            c.That(claims.Committed, "existing engine transaction owner acquires actual outbound promises");
            c.That(Ready(game, "stock.ready") && !Ready(game, "available.ready"), "stored stock and unpromised whole-set stock are distinct facts");
            c.That(retained.Value == Fix.One, "retained immutable signal never follows later reservations");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "stock promises persist only through their original inventory codec");
            c.That(game.World.Services.Get<IResourceTransaction>().ReleaseClaims(new ResourceClaimReleaseIntent(new PlayerId(0), claims.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "existing receipt owner releases exact promises");
            c.That(Ready(game, "available.ready"), "release changes derived readiness without signal state");
            ReservationToken inbound = Stock(game).ReserveIn(requester, Supply(game), Fix.FromInt(16));
            c.That(!Ready(game, "incoming.ready") && Ready(game, "stock.ready"), "real future inbound capacity is included in whole-bill admission");
            Stock(game).ReleaseIn(requester, inbound); c.That(Ready(game, "incoming.ready"), "released inbound promise reopens actual capacity");
            SimGame other = New(factory);
            c.Throws<ArgumentException>(() => game.World.Content.Get<AutomationSignalDef>("stock.ready").ReadFrom(game.World,
                Find(other).Get<IAutomationSignalSource>(Feed)), "same-ID foreign session provider is rejected");
            c.Equal(other.Hash(), factory.Load(other.Save()).Hash(), "independent untouched session is checkpoint-identical");
            Thresholds(c); NumericFacts(c, factory); ReceivingRoom(c, factory); InvalidContent(c); InvalidQuantities(c); Faults(c, factory);
            NetworkSignalTests.Contracts(c);
        }
        private static void Thresholds(Check c)
        {
            foreach (int supply in new[] { 7, 8, 27, 28 })
            {
                SimGame game = New(Factory(json => json.Get("entities")[0].Get("components").Get("storage").Get("initial_stock").Set("supply", JsonValue.Number(supply.ToString()))));
                c.Equal(supply >= 8, Ready(game, "stock.ready"), "inclusive count threshold " + supply);
                c.Equal(supply <= 27, Ready(game, "incoming.ready"), "whole measured capacity boundary " + supply);
            }
            SimGame fluid = New(Factory(json => json.Get("entities")[0].Get("components").Get("storage").Get("initial_stock").Set("fluid", JsonValue.Number("1.5"))));
            c.That(!Ready(fluid, "stock.ready") && !Ready(fluid, "available.ready"), "one deficient fluid blocks the whole multi-resource bill");
            fluid = New(Factory(json => json.Get("entities")[0].Get("components").Get("inventory_signal").Get("quantities").Set("fluid", JsonValue.Number("2.5"))));
            c.That(Ready(fluid, "stock.ready"), "continuous native fluid thresholds remain exact, not rounded into discrete counts");
            SimGame only = New(Factory(json => {
                json.Get("entities")[0].Get("components").Remove("signal_transformation_gate");
                json.Get("entities")[0].Get("components").Remove("transformer"); }));
            c.That(Ready(only, "stock.ready"), "standalone inventory needs no local controllable operation or process owner");
        }
        private static void InvalidContent(Check c)
        {
            foreach (string invalid in new[] { "id", "invalid-id", "inventory", "wrong-key", "no-owner", "quantities", "empty", "unknown", "zero", "negative", "fraction", "capacity", "incompatible", "extra", "schema", "metric", "unit", "kind" })
                c.Throws<ContentException>(() => New(Factory(json => {
                    var parts = json.Get("entities")[0].Get("components"); var source = parts.Get("inventory_signal"); var bill = source.Get("quantities"); var signal = json.Get("signals")[0];
                    switch (invalid)
                    {
                        case "id": source.Remove("id"); break;
                        case "invalid-id": source.Set("id", JsonValue.Of("bad id")); break;
                        case "inventory": source.Remove("inventory"); break;
                        case "wrong-key": source.Set("inventory", JsonValue.Of("process_stock")); break;
                        case "no-owner": parts.Remove("storage"); break;
                        case "quantities": source.Remove("quantities"); break;
                        case "empty": source.Set("quantities", Json.Parse("{}")); break;
                        case "unknown": bill.Set("absent", JsonValue.Number("1")); break;
                        case "zero": bill.Set("supply", JsonValue.Number("0")); break;
                        case "negative": bill.Set("supply", JsonValue.Number("-1")); break;
                        case "fraction": bill.Set("supply", JsonValue.Number("0.5")); break;
                        case "capacity": bill.Set("supply", JsonValue.Number("41")); break;
                        case "incompatible": parts.Get("storage").Set("accepts", Json.Parse("[\"fluid\"]")); parts.Get("storage").Get("initial_stock").Remove("supply"); break;
                        case "extra": source.Set("legacy", JsonValue.Of(true)); break;
                        case "schema": signal.Get("providers")[0].Set("schema", JsonValue.Number("1")); break;
                        case "metric": signal.Set("metric", JsonValue.Of("inventory.fake")); break;
                        case "unit": signal.Set("unit", JsonValue.Of("unit.kit")); break;
                        case "kind": signal.Set("kind", JsonValue.Of("number")); break;
                    }
                })), "strict authored inventory signal refuses " + invalid + " before play");
        }
        private static void NumericFacts(Check c, GameFactory factory)
        {
            SimGame game = New(factory);
            c.Throws<ArgumentException>(() => new AutomationMetricSchema(new Code("bad"), AutomationSignalKind.Number, Code.None),
                "fixed metric constructors never silently permit missing units");
            c.Throws<ArgumentException>(() => AutomationMetricSchema.Quantity(Code.None), "quantity metrics require explicit identities");
            View(game).TryGetSignalSchema(Find(game).Id, Feed, out var schema);
            c.That(schema.TryGet(new Code("inventory.stored"), out var numeric) && numeric.UsesAuthoredUnit && numeric.Kind == AutomationSignalKind.Number,
                "quantity schema requires an authored resolved unit, not a fabricated native unit code");
            c.That(schema.TryGet(new Code("inventory.stock-ready"), out var fixedMetric) && !fixedMetric.UsesAuthoredUnit && fixedMetric.Unit == new Code("unit.boolean"),
                "fixed boolean bindings retain exact unit identity");
            c.Throws<NotSupportedException>(() => ((IList<AutomationMetricSchema>)schema.Bindings).Clear(), "quantity metadata is immutable");
            foreach (string prototype in new[] { Native, Independent })
            {
                c.Equal(Fix.FromInt(20), Quantity(game, "stock.amount", prototype), "actual stored native stock " + prototype);
                c.Equal(Fix.FromInt(20), Quantity(game, "available.amount", prototype), "actual available native stock " + prototype);
                c.Equal(Fix.Zero, Quantity(game, "inbound.amount", prototype), "available zero inbound is not missing " + prototype);
                c.Equal(Fix.Zero, Quantity(game, "outbound.amount", prototype), "available zero outbound is not missing " + prototype);
                var retained = Signal(game, "available.amount", prototype); var holder = new ReservationOwner("proof.quantity", 1);
                IInventory stock = Stock(game, prototype); IResourceTransaction transactions = game.World.Services.Get<IResourceTransaction>();
                ResourceClaimResult claims = transactions.AcquireClaims(new PlayerId(0), new[] {
                    new ResourceClaimIntent(ReservationDirection.Outbound, holder, stock.Endpoint, new Bill(new[] { new ResourceAmount(Supply(game), Fix.FromInt(13)) })) }, System.Array.Empty<ResourceReservation>());
                c.That(claims.Committed, "real existing outbound receipts drive quantity facts " + prototype);
                ReservationToken inbound = stock.ReserveIn(holder, Supply(game), Fix.FromInt(5));
                c.That(inbound.IsValid, "real existing inlet reserves capacity " + prototype);
                c.Equal(Fix.FromInt(20), Quantity(game, "stock.amount", prototype), "promises do not reduce actual stored stock " + prototype);
                c.Equal(Fix.FromInt(7), Quantity(game, "available.amount", prototype), "outbound promises reduce availability " + prototype);
                c.Equal(Fix.FromInt(13), Quantity(game, "outbound.amount", prototype), "actual outbound quantity " + prototype);
                c.Equal(Fix.FromInt(5), Quantity(game, "inbound.amount", prototype), "actual inbound quantity " + prototype);
                c.Equal(Fix.FromInt(20), retained.Value.Value, "retained quantity facts do not follow later reservations " + prototype);
                SimGame twin = factory.Load(game.Save());
                c.Equal(game.Hash(), twin.Hash(), "quantities and promises save only in original owners " + prototype);
                c.Equal(Fix.FromInt(7), Quantity(twin, "available.amount", prototype), "saved original receipts derive the same quantity " + prototype);
                c.That(transactions.ReleaseClaims(new ResourceClaimReleaseIntent(new PlayerId(0), claims.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "original outbound owner releases actual quantity " + prototype);
                stock.ReleaseIn(holder, inbound);
                c.Equal(Fix.FromInt(20), Quantity(game, "available.amount", prototype), "release changes facts without signal state " + prototype);
                c.Equal(Fix.Zero, Quantity(game, "inbound.amount", prototype), "released inlet is an exact available zero " + prototype);
            }
            ulong hash = game.Hash();
            c.Equal(Fix.FromInt(10), Quantity(game, "pair.amount"), "exact discrete scaled quantity conversion");
            c.Equal(Fix.FromInt(3), Quantity(game, "fluid.amount"), "fluid stays in its explicit volume unit");
            c.Equal(Fix.Parse("1.5"), Quantity(game, "fluid.pair"), "fractional scaled continuous quantity is exact");
            c.Equal(Fix.One, Quantity(game, "fluid.third"), "exact rational unit scales are respected");
            var range = Signal(game, "fluid.nano");
            c.That(!range.Available && !range.Value.HasValue && range.Blocker == OperationCodes.SignalNumericRange,
                "output range overflow withholds value, never clamps or substitutes zero");
            var incompatible = Signal(game, "fluid.amount", Independent);
            c.That(!incompatible.Available && incompatible.Blocker == ResourceSignalCodes.Incompatible,
                "unrelated provider lacking this resource returns typed incompatibility");
            SimGame odd = New(Factory(json => json.Get("entities")[0].Get("components").Get("storage").Get("initial_stock").Set("supply", JsonValue.Number("19"))));
            var discrete = Signal(odd, "pair.amount");
            c.That(!discrete.Available && discrete.Blocker == ResourceSignalCodes.QuantityUnavailable, "fractional discrete output cannot be rounded");
            SimGame third = New(Factory(json => json.Get("entities")[0].Get("components").Get("storage").Get("initial_stock").Set("fluid", JsonValue.Number("1"))));
            var imprecise = Signal(third, "fluid.third");
            c.That(!imprecise.Available && imprecise.Blocker == ResourceSignalCodes.QuantityUnavailable, "unrepresentable rational conversion is unavailable, not approximated");
            SimGame empty = New(Factory(json => json.Get("entities")[0].Get("components").Get("storage").Get("initial_stock").Remove("fluid")));
            c.Equal(Fix.Zero, Quantity(empty, "fluid.amount"), "accepted empty stock is available numeric zero");
            c.Equal(Codes.NotOwned, View(game, 1).ReadSignal(Find(game).Id, Feed, "stock.amount", out var hidden), "numeric stock remains company-private");
            c.That(!hidden.Value.HasValue, "private numeric refusal reveals no stock");
            c.Equal(Code.None, game.Observer.Feature<OperationsView>().ReadSignal(Find(game).Id, Feed, "stock.amount", out var observed), "explicit observer reads detached quantity");
            c.Equal(Fix.FromInt(20), observed.Value.Value, "observer receives actual stock only");
            var authored = game.World.Content.Get<ResourceAutomationSignalDef>("stock.amount");
            c.That(ReferenceEquals(authored.Resource, game.World.Content.Resource(Supply(game))) &&
                ReferenceEquals(authored.QuantityUnit, game.World.Content.Get<UnitDef>("unit.kit")), "signal aliases existing exact catalog resource and unit contracts");
            SimGame other = New(factory); ulong otherHash = other.Hash();
            c.Throws<ArgumentException>(() => authored.ReadFrom(game.World, Find(other).Get<IAutomationSignalSource>(Feed)), "numeric sources cannot cross same-ID sessions");
            c.Equal(otherHash, other.Hash(), "foreign quantity refusal preserves other session");
            c.Equal(hash, game.Hash(), "all numeric/unit/private observations remain pure without cached signal state");
            GameFactory numericFactory = Factory(json => json.Get("entities")[0].Get("components").Get("inventory_signal").Set("quantities", Json.Parse("null")));
            SimGame numericOnly = New(numericFactory);
            c.Equal(Fix.FromInt(20), Quantity(numericOnly, "stock.amount"), "explicit numeric-only source needs no fabricated readiness threshold");
            foreach (string readiness in new[] { "stock.ready", "available.ready", "incoming.ready" })
            {
                var disabled = Signal(numericOnly, readiness);
                c.That(!disabled.Available && !disabled.Value.HasValue && disabled.Blocker == InventorySignal.NoThreshold,
                    "explicitly disabled readiness never fabricates an empty-bill true signal " + readiness);
            }
            c.Equal(numericOnly.Hash(), numericFactory.Load(numericOnly.Save()).Hash(), "numeric-only source roundtrip uses exact content identity");
        }
        private static void ReceivingRoom(Check c, GameFactory factory)
        {
            foreach (string prototype in new[] { Native, Independent })
            {
                SimGame game = New(factory); IInventory stock = Stock(game, prototype);
                Fix room = Fix.FromInt(prototype == Native ? 17 : 20);
                c.Equal(room, Quantity(game, "receiving.room", prototype), "actual mixed/shared receiving capacity " + prototype);
                var retained = Signal(game, "receiving.room", prototype); var holder = new ReservationOwner("proof.room", 1);
                ReservationToken outbound = stock.ReserveOut(holder, Supply(game), Fix.FromInt(5));
                c.That(outbound.IsValid, "real outbound promise exists " + prototype);
                c.Equal(room, Quantity(game, "receiving.room", prototype), "outbound promise never frees physical capacity " + prototype);
                ReservationToken inbound = stock.ReserveIn(holder, Supply(game), Fix.FromInt(5));
                c.That(inbound.IsValid, "real inbound promise exists " + prototype);
                c.Equal(room - Fix.FromInt(5), Quantity(game, "receiving.room", prototype), "inbound reserves actual shared room " + prototype);
                SimGame twin = factory.Load(game.Save());
                c.Equal(room - Fix.FromInt(5), Quantity(twin, "receiving.room", prototype), "saved original receipts derive same receiving room " + prototype);
                c.Equal(room, retained.Value.Value, "retained receiving facts are detached " + prototype);
                stock.ReleaseIn(holder, inbound); stock.ReleaseOut(holder, outbound);
                c.Equal(room, Quantity(game, "receiving.room", prototype), "release reopens room in its original owner " + prototype);
                c.Equal(room - Fix.FromInt(5), Quantity(twin, "receiving.room", prototype), "release cannot affect another session " + prototype);
                inbound = stock.ReserveIn(holder, Supply(game), room);
                c.That(inbound.IsValid, "all exact remaining room can be promised " + prototype);
                c.Equal(Fix.Zero, Quantity(game, "receiving.room", prototype), "known full capacity is available zero " + prototype);
                ulong hash = game.Hash(); for (int i = 0; i < 6; i++) Signal(game, "receiving.room", prototype);
                c.Equal(hash, game.Hash(), "room reads acquire nothing or advance time " + prototype);
            }
            SimGame native = New(factory);
            c.Equal(Fix.FromInt(17), Quantity(native, "receiving.fluid"), "fluid room shares mass limit with supply");
            var mixedOwner = new ReservationOwner("proof.room-mixed", 1);
            ReservationToken fluidPromise = Stock(native).ReserveIn(mixedOwner, Fluid(native), Fix.FromInt(2));
            c.That(fluidPromise.IsValid, "different resource reserves shared mass capacity");
            c.Equal(Fix.FromInt(15), Quantity(native, "receiving.room"), "fluid promise reduces supply room through original mixed capacity owner");
            Stock(native).ReleaseIn(mixedOwner, fluidPromise);
            c.Equal(Fix.FromInt(17), Quantity(native, "receiving.room"), "cross-resource promise release reopens supply room");
            c.That(!Signal(native, "receiving.pairs").Available && Signal(native, "receiving.pairs").Blocker == ResourceSignalCodes.QuantityUnavailable,
                "odd room cannot be rounded into a discrete pair");
            c.Equal(Fix.FromInt(10), Quantity(native, "receiving.pairs", Independent), "independent provider room converts exact discrete pairs");
            c.Equal(Codes.NotOwned, View(native, 1).ReadSignal(Find(native).Id, Feed, "receiving.room", out var hidden), "room remains company private");
            c.That(!hidden.Value.HasValue, "private room reveals no capacity");
            foreach (int mode in new[] { 3, 4, 10, 11, 12, 13 })
            {
                SimGame hostile = New(factory); Find(hostile, Independent).Get<SingleResourcePort>().SignalFault = mode;
                c.Throws<InvalidOperationException>(() => Signal(hostile, "receiving.room", Independent), "room mutation/malformed provider faults " + mode);
            }
            SimGame unavailable = New(factory); Find(unavailable, Independent).Get<SingleResourcePort>().SignalFault = 5;
            var reading = Signal(unavailable, "receiving.room", Independent);
            c.That(!reading.Available && !reading.Value.HasValue && reading.Blocker == ResourceSignalCodes.CapacityUnavailable,
                "known unrepresentable capacity never pretends to be full/empty");
            c.Equal(Fix.FromInt(20), Quantity(unavailable, "stock.amount", Independent), "capacity refusal does not hide otherwise valid stored quantity");
            c.Throws<ContentException>(() => New(Factory(json => json.Get("signals")[0].Get("providers")[0].Set("schema", JsonValue.Number("2")))),
                "old inventory schema is refused, not loaded through a compatibility declaration");
        }
        private static void InvalidQuantities(Check c)
        {
            foreach (string invalid in new[] { "resource", "unknown-resource", "unit", "unknown-unit", "dimension", "kind", "metric", "schema", "extra", "untyped" })
                c.Throws<ContentException>(() => New(Factory(json => {
                    var signal = json.Get("resource_signals")[0];
                    switch (invalid)
                    {
                        case "resource": signal.Remove("resource"); break;
                        case "unknown-resource": signal.Set("resource", JsonValue.Of("absent")); break;
                        case "unit": signal.Remove("unit"); break;
                        case "unknown-unit": signal.Set("unit", JsonValue.Of("unit.absent")); break;
                        case "dimension": signal.Set("unit", JsonValue.Of("unit.fluid")); break;
                        case "kind": signal.Set("kind", JsonValue.Of("boolean")); break;
                        case "metric": signal.Set("metric", JsonValue.Of("inventory.fake")); break;
                        case "schema": signal.Get("providers")[0].Set("schema", JsonValue.Number("1")); break;
                        case "extra": signal.Set("legacy", JsonValue.Of(true)); break;
                        case "untyped":
                            var untyped = json.Get("signals")[0]; untyped.Set("kind", JsonValue.Of("number"));
                            untyped.Set("metric", JsonValue.Of("inventory.stored")); untyped.Set("unit", JsonValue.Of("unit.kit")); break;
                    }
                })), "explicit quantity content refuses " + invalid + " before play");
        }
        private static void Faults(Check c, GameFactory factory)
        {
            foreach (int mode in new[] { 1, 2, 3, 4 })
            {
                SimGame game = New(factory); var provider = Find(game, Independent).Get<SingleResourcePort>(); provider.SignalFault = mode;
                c.Throws<InvalidOperationException>(() => Signal(game, mode < 3 ? "stock.ready" : "incoming.ready", Independent), "guarded signal rejects hostile inventory observation " + mode);
            }
            SimGame unavailable = New(factory); Find(unavailable, Independent).Get<SingleResourcePort>().SignalFault = 5;
            var value = Signal(unavailable, "incoming.ready", Independent);
            c.That(!value.Available && !value.Value.HasValue && value.Blocker == InventorySignal.CapacityUnavailable,
                "unrepresentable actual capacity remains typed unavailable, never a false reading");
            SimGame fault = New(factory); Configure(fault, Independent); Find(fault, Independent).Get<SingleResourcePort>().SignalFault = 1;
            fault.Update(); c.That(fault.Fault != null, "hostile inventory signal faults real automation Update without successful control");
            foreach (int mode in new[] { 1, 2, 6, 7, 8, 9 })
            {
                SimGame game = New(factory); Find(game, Independent).Get<SingleResourcePort>().SignalFault = mode;
                c.Throws<InvalidOperationException>(() => Signal(game, "stock.amount", Independent), "numeric inventory guards reject mutation/malformed promises " + mode);
            }
            fault = New(factory); ConfigureNumeric(fault, Independent); Find(fault, Independent).Get<SingleResourcePort>().SignalFault = 8;
            fault.Update(); c.That(fault.Fault != null, "malformed numeric facts fault actual automation Update");
            c.Throws<InvalidOperationException>(() => fault.Save(), "faulted numeric control cannot save partial resumable state");
        }
        private static void Execution(Check c)
        {
            GameFactory factory = Factory(); CapacityGate(c, factory); StockControl(c, factory, Native); StockControl(c, factory, Independent);
            foreach (string prototype in new[] { Native, Independent }) { NumericHysteresis(c, factory, prototype); NumericControl(c, factory, prototype); }
            ScaledOperatingControl(c);
            NetworkSignalTests.Execution(c);
        }
        private static void CapacityGate(Check c, GameFactory factory)
        {
            SimGame game = New(factory); IInventory stock = Stock(game); var owner = new ReservationOwner("proof.delivery", 1);
            ReservationToken inbound = stock.ReserveIn(owner, Supply(game), Fix.FromInt(16)); long history = History(game).Latest;
            c.That(Queue(game).Accepted, "player queues real work while promised capacity blocks execution");
            c.That(State(game).Progress.Equals(WorkProgress.Zero) && State(game).Wip.Stock.IsEmpty, "signal gate creates no work or WIP while blocked");
            c.Equal(new Code("process.promised-capacity"), State(game).Blocker, "existing process gate consumes the real incoming signal refusal");
            c.Equal(history, History(game).Latest, "pure incoming gate records no fabricated resource effect");
            SimGame twin = factory.Load(game.Save()); Tick(game, 3); Tick(twin, 3);
            c.Equal(game.Hash(), twin.Hash(), "saved promised-capacity wait continues identically");
            stock.ReleaseIn(owner, inbound); Stock(twin).ReleaseIn(owner, inbound); Tick(game); Tick(twin);
            c.That(State(game).Progress.WholeTicks == 1 && State(game).Wip.Stock.AmountOf(Supply(game)) == Fix.FromInt(3), "actual resource release admits physical inputs and first real work tick");
            inbound = stock.ReserveIn(owner, Supply(game), Fix.FromInt(16)); Stock(twin).ReserveIn(owner, Supply(twin), Fix.FromInt(16));
            var progress = State(game).Progress; Tick(game, 3); Tick(twin, 3);
            c.Equal(progress, State(game).Progress, "new actual inbound commitment pauses already-running work");
            c.Equal(Fix.FromInt(3), State(game).Wip.Stock.AmountOf(Supply(game)), "pause retains actual WIP rather than copying or losing stock");
            c.Equal(game.Hash(), twin.Hash(), "saved twins retain gate/promise/progress ownership");
            SimGame paused = factory.Load(game.Save()); c.Equal(game.Hash(), paused.Hash(), "running gate wait checkpoints with only existing owners");
            stock.ReleaseIn(owner, inbound); Stock(twin).ReleaseIn(owner, inbound); Reach(game, () => State(game).QueueCount == 0, "physical capacity restart", twin);
            c.Equal(Fix.FromInt(18), stock.Amount(Supply(game)), "real timed conversion consumes three and returns one byproduct exactly once");
            c.Equal(Fix.FromInt(3), stock.Amount(Fluid(game)), "threshold fluid is observed, not silently consumed");
            c.Equal(game.Hash(), twin.Hash(), "physical gate completion and journal are saved-twin identical");
            c.That(History(game).Totals(Supply(game)).ConsumedRaw > 0, "real consumption reaches the existing flow journal");
        }
        private static void StockControl(Check c, GameFactory factory, string prototype)
        {
            SimGame game = New(factory);
            c.That(Queue(game, 10, prototype).Accepted && Configure(game, prototype).Accepted, "ordinary process and rule commands start inventory-driven work " + prototype);
            Reach(game, () => Rule(game).PendingDecision == false, "stock threshold next-tick intent");
            c.That(Enabled(game, prototype) && !Ready(game, "available.ready", prototype), "new low-stock decision cannot apply recursively in its capture step");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending stock decision survives original rule/process codecs");
            Tick(game); Tick(twin);
            c.That(!Enabled(game, prototype) && State(game, prototype).Progress.WholeTicks > 0, "deferred normal operation handler pauses real earned work");
            var progress = State(game, prototype).Progress; Fix material = Stock(game, prototype).Amount(Supply(game)); long history = History(game).Latest;
            Tick(game, 4); Tick(twin, 4);
            c.Equal(progress, State(game, prototype).Progress, "low-stock control preserves unfinished real progress");
            c.Equal(material, Stock(game, prototype).Amount(Supply(game)), "disabled work neither consumes nor fabricates material");
            c.Equal(history, History(game).Latest, "disabled work appends no fake stock flow");
            c.Equal(game.Hash(), twin.Hash(), "native and unrelated provider continuations use the same control owner");
            SimGame saved = factory.Load(game.Save()); Tick(game); Tick(saved); Tick(twin);
            c.Equal(game.Hash(), saved.Hash(), "saved disabled stock control continues exactly");
            var cancel = new CancelTransformationCommand { Transformer = Find(game, prototype).Id, Capability = Operation, Index = 0 };
            c.That(TestKit.Order(game, 0, cancel).Accepted, "normal cancellation may recover real WIP while automation disables processing");
            c.That(TestKit.Order(twin, 0, new CancelTransformationCommand { Transformer = Find(twin, prototype).Id, Capability = Operation, Index = 0 }).Accepted,
                "saved twin executes the same actual cancellation");
            c.That(State(game, prototype).Wip.Stock.IsEmpty && Ready(game, "available.ready", prototype), "existing return-input workflow restores readiness without a signal-specific grant");
            c.Equal(game.Hash(), twin.Hash(), "cancellation, returned material and next rule observation match");
            Reach(game, () => Enabled(game, prototype), "returned-stock restart", twin);
            c.That(State(game, prototype).Progress.WholeTicks > 0 && State(game, prototype).Wip.Stock.AmountOf(Supply(game)) == Fix.FromInt(3),
                "returned stock restarts a fresh physical process through deferred ordinary control");
            c.Equal(game.Hash(), twin.Hash(), "restarted physical work remains deterministic");
            SimGame isolated = New(factory);
            c.That(Ready(isolated, "available.ready", prototype) && Stock(isolated, prototype).Amount(Supply(isolated)) == Fix.FromInt(20), "interleaved session receives no other session's reservations/control/material");
        }
        private static CommandResult ConfigureNumeric(SimGame game, string prototype, string profile = "stock.numeric")
            => TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "feed", Settings = new AutomationSettings(profile, true,
                new ControlBinding(Find(game, prototype).Id, Operation), new Dictionary<string, ControlBinding> {
                    ["stock"] = new ControlBinding(Find(game, prototype).Id, Feed) }, ControlThresholds.Initial(game.World.Content.Get<IControlRule>(profile)),
                    game.World.Content.Get<IControlRule>(profile).WhenTrue, game.World.Content.Get<IControlRule>(profile).WhenFalse) });
        private static ResourceClaimResult Claim(SimGame game, string prototype, int amount, long identity)
            => game.World.Services.Get<IResourceTransaction>().AcquireClaims(new PlayerId(0), new[] {
                new ResourceClaimIntent(ReservationDirection.Outbound, new ReservationOwner("proof.numeric", identity), Stock(game, prototype).Endpoint,
                    new Bill(new[] { new ResourceAmount(Supply(game), Fix.FromInt(amount)) })) }, System.Array.Empty<ResourceReservation>());
        private static void NumericHysteresis(Check c, GameFactory factory, string prototype)
        {
            SimGame game = New(factory);
            c.That(ConfigureNumeric(game, prototype).Accepted, "ordinary rule command binds exact resource units " + prototype);
            Reach(game, () => Rule(game).HasApplied && Rule(game).Latch, "numeric high-stock dwell");
            var middle = Claim(game, prototype, 11, 1); c.That(middle.Committed, "real promises move observation inside hysteresis band " + prototype);
            Tick(game, 3);
            c.Equal(Fix.FromInt(9), Quantity(game, "available.amount", prototype), "numeric band reads actual unpromised stock " + prototype);
            c.That(Rule(game).Latch && Enabled(game, prototype), "already-on control remains on inside band " + prototype);
            var transactions = game.World.Services.Get<IResourceTransaction>();
            c.That(transactions.ReleaseClaims(new ResourceClaimReleaseIntent(new PlayerId(0), middle.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "real native claims release without fake stock " + prototype);
            var low = Claim(game, prototype, 13, 2); c.That(low.Committed, "actual low-stock promises select lower threshold " + prototype);
            Tick(game);
            c.That(Rule(game).Candidate == false && Rule(game).PendingDecision == null && Rule(game).Latch,
                "dwell prevents immediate low-stock control " + prototype);
            Tick(game);
            c.That(Rule(game).PendingDecision == false && Enabled(game, prototype), "numeric decision defers to next simulation step " + prototype);
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(!Enabled(game, prototype) && !Rule(game).Latch, "existing normal handler applies numeric disable " + prototype);
            c.Equal(game.Hash(), twin.Hash(), "saved pending numeric decisions execute identically " + prototype);
            c.That(transactions.ReleaseClaims(new ResourceClaimReleaseIntent(new PlayerId(0), low.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed &&
                twin.World.Services.Get<IResourceTransaction>().ReleaseClaims(new ResourceClaimReleaseIntent(new PlayerId(0), low.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "twins release actual original low promises " + prototype);
            middle = Claim(game, prototype, 11, 3); var twinMiddle = Claim(twin, prototype, 11, 3);
            c.That(middle.Committed && twinMiddle.Committed, "both native ledgers derive the same new band " + prototype);
            Tick(game, 4); Tick(twin, 4);
            c.That(!Enabled(game, prototype) && !Rule(game).Latch && Quantity(game, "available.amount", prototype) == Fix.FromInt(9),
                "already-off control stays off at the same band value " + prototype);
            c.Equal(game.Hash(), twin.Hash(), "hysteresis state stays in the existing rule codec " + prototype);
            ulong paused = game.Hash(); long step = game.Tick; game.Clock.Pause(); Tick(game, 3);
            c.Equal(step, game.Tick, "sole game clock pause freezes numeric controls " + prototype);
            c.Equal(paused, game.Hash(), "pause changes no rule/stock/elapsed state " + prototype); game.Clock.Resume();
            c.That(transactions.ReleaseClaims(new ResourceClaimReleaseIntent(new PlayerId(0), middle.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed &&
                twin.World.Services.Get<IResourceTransaction>().ReleaseClaims(new ResourceClaimReleaseIntent(new PlayerId(0), twinMiddle.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "actual release crosses upper stock threshold " + prototype);
            Tick(game); Tick(twin);
            c.That(Rule(game).Candidate == true && !Enabled(game, prototype), "restart still observes declared dwell " + prototype);
            Reach(game, () => Enabled(game, prototype) && Rule(game).Latch, "numeric high-stock restart", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved real quantities and normal controls restart identically " + prototype);
            c.Equal(Fix.FromInt(20), Stock(game, prototype).Amount(Supply(game)), "control/reservations alone never manufacture or consume stock " + prototype);
        }
        private static void NumericControl(Check c, GameFactory factory, string prototype)
        {
            SimGame game = New(factory);
            c.That(Queue(game, 10, prototype).Accepted && ConfigureNumeric(game, prototype).Accepted, "numeric rule controls actual queued physical work " + prototype);
            Reach(game, () => Rule(game).PendingDecision == false, "physical numeric low-stock decision");
            c.That(Enabled(game, prototype) && Quantity(game, "available.amount", prototype) < Fix.FromInt(8),
                "real spending crosses the exact threshold before deferred shutdown " + prototype);
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(!Enabled(game, prototype) && State(game, prototype).Progress.WholeTicks > 0,
                "numeric shutdown retains earned physical work and WIP " + prototype);
            WorkProgress progress = State(game, prototype).Progress; Fix stock = Stock(game, prototype).Amount(Supply(game));
            long history = History(game).Latest; Tick(game, 4); Tick(twin, 4);
            c.Equal(progress, State(game, prototype).Progress, "numeric waits advance no physical work " + prototype);
            c.Equal(stock, Stock(game, prototype).Amount(Supply(game)), "numeric waits spend no material " + prototype);
            c.Equal(history, History(game).Latest, "numeric waits emit no fabricated stock flows " + prototype);
            c.Equal(game.Hash(), twin.Hash(), "saved numeric stopped process continues identically " + prototype);
            c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = Find(game, prototype).Id, Capability = Operation, Index = 0 }).Accepted &&
                TestKit.Order(twin, 0, new CancelTransformationCommand { Transformer = Find(twin, prototype).Id, Capability = Operation, Index = 0 }).Accepted,
                "ordinary cancellation returns actual WIP while numeric control is off " + prototype);
            c.That(State(game, prototype).Wip.Stock.IsEmpty && Quantity(game, "available.amount", prototype) > Fix.FromInt(8) &&
                Quantity(game, "available.amount", prototype) <= Fix.FromInt(12), "real returned inputs lie inside numeric hysteresis band " + prototype);
            Tick(game, 4); Tick(twin, 4);
            c.That(!Enabled(game, prototype), "returning into the band does not ignore upper restart threshold " + prototype);
            c.Equal(game.Hash(), twin.Hash(), "returned WIP and held-off rule are saved-twin identical " + prototype);
            c.That(ConfigureNumeric(game, prototype, "stock.restart").Accepted && ConfigureNumeric(twin, prototype, "stock.restart").Accepted,
                "player selects another authored numeric policy through existing commands " + prototype);
            Reach(game, () => Enabled(game, prototype), "authored lower-threshold restart", twin);
            c.That(State(game, prototype).Progress.WholeTicks > 0 && State(game, prototype).Wip.Stock.AmountOf(Supply(game)) == Fix.FromInt(3),
                "numeric restart creates only real timed work through existing receipts " + prototype);
            c.Equal(game.Hash(), twin.Hash(), "normal restarted physical execution remains deterministic " + prototype);
            c.That(History(game).Totals(Supply(game)).ConsumedRaw > 0, "actual numeric-controlled spending reaches sole resource history " + prototype);
        }
        private static void ScaledOperatingControl(Check c)
        {
            GameFactory factory = Factory(json => {
                var signal = json.Get("resource_signals")[0];
                signal.Set("metric", JsonValue.Of("inventory.available")); signal.Set("resource", JsonValue.Of("fluid"));
                signal.Set("unit", JsonValue.Of("unit.fluid.pair"));
                var profile = json.Get("control_rules")[1]; profile.Get("inputs")[0].Set("signal", JsonValue.Of("stock.amount"));
                var condition = profile.Get("conditions")[0]; condition.Set("unit", JsonValue.Of("unit.fluid.pair"));
                condition.Set("on", JsonValue.Number("1")); condition.Set("off", JsonValue.Number("0.5"));
                profile.Get("dwell").Set("numerator", JsonValue.Number("0")); profile.Get("cooldown").Set("numerator", JsonValue.Number("0"));
                json.Get("entities")[1].Get("components").Remove("transformer");
            }, process => {
                process.Get("entities")[0].Get("components").Remove("transformer");
                process.Get("transformations")[0].Set("operating_inputs",
                    Json.Parse("[{\"resource\":\"fluid\",\"quantity\":0.25,\"period\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}}]"));
            });
            SimGame game = New(factory);
            c.Equal(Fix.Parse("1.5"), Quantity(game, "stock.amount"), "numeric control can author exact scaled fluid output units");
            c.That(Queue(game, 10).Accepted && ConfigureNumeric(game, Native).Accepted, "ordinary player commands control real resource-rate processing");
            Reach(game, () => Rule(game).PendingDecision == false, "scaled fluid operating threshold");
            c.That(Enabled(game) && Quantity(game, "stock.amount") < Fix.Parse("0.5"), "actual fluid consumption crosses scaled numeric threshold before next-step disable");
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(!Enabled(game) && State(game).Progress.WholeTicks > 0, "scaled quantity rule stops real operating inputs while preserving work");
            Fix remaining = Stock(game).Amount(Fluid(game)); WorkProgress progress = State(game).Progress;
            c.That(History(game).Totals(Fluid(game)).ConsumedRaw == (Fix.FromInt(3) - remaining).Raw,
                "exact actual fluid consumption matches sole resource history, not scaled signal balances");
            Tick(game, 5); Tick(twin, 5);
            c.Equal(remaining, Stock(game).Amount(Fluid(game)), "disabled numeric control burns no operating fluid");
            c.Equal(progress, State(game).Progress, "disabled scaled rule earns no process work");
            c.Equal(game.Hash(), twin.Hash(), "scaled resource signals use original process/rule codecs on continuation");
            ulong hash = game.Hash(); long step = game.Tick; game.Clock.Pause(); Tick(game, 3); game.Clock.Resume();
            c.Equal(step, game.Tick, "scaled fluid control shares sole paused game clock");
            c.Equal(hash, game.Hash(), "pause cannot advance fuel exposure or rule deadlines");
            c.That(TestKit.Order(game, 0, new RemoveAutomationCommand { Rule = "feed" }).Accepted,
                "player removes original controller before assigning another exclusive rule");
            var unsupported = new AutomationSettings("stock.numeric", true,
                new ControlBinding(Find(game).Id, Operation), new Dictionary<string, ControlBinding> {
                    ["stock"] = new ControlBinding(Find(game, Independent).Id, Feed) }, ControlThresholds.Initial(game.World.Content.Get<IControlRule>("stock.numeric")),
                    game.World.Content.Get<IControlRule>("stock.numeric").WhenTrue, game.World.Content.Get<IControlRule>("stock.numeric").WhenFalse);
            c.Equal(Code.None, View(game).PreviewAutomation("unsupported", unsupported), "known quantity schema allows intent without inventing a live fluid value");
            c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "unsupported", Settings = unsupported }).Accepted,
                "ordinary rule intent can wait on a currently unavailable resource");
            c.That(View(game).TryGetAutomationRule(new PlayerId(0), "unsupported", out var unavailable) &&
                unavailable.Blocker == ResourceSignalCodes.Incompatible && !unavailable.PendingDecision.HasValue,
                "unsupported fluid source suspends control with its exact typed blocker, never substitutes zero");
        }
    }
}
