using System;
using System.Collections.Generic;
using System.Reflection;
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
    public static class TransformationRunningTests
    {
        private static readonly Code Capability = new Code("process");
        private const string Declaration = "{\"id\":\"proof.transformation-supply\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"},{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"running-inputs.json\"]}";
        public static void Register()
        {
            TestRunner.Add("Transformation running inputs: authored rates, admission and private saved facts", Contracts);
            TestRunner.Add("Transformation running inputs: physical supply, controls, cancellation and timed consumption", Physical);
        }
        private static string Text(JsonValue json) { var text = new StringBuilder(); json.WriteCanonical(text); return text.ToString(); }
        private static JsonValue Component(JsonValue json, string name) => json.Get("entities")[1].Get("components").Get(name);
        private static JsonValue Rate(JsonValue json) => json.Get("transformations")[0].Get("operating_inputs")[0];
        private static GameFactory Factory(Action<JsonValue> edit = null, Action<JsonValue> editBasis = null, bool utilities = false,
            Action<JsonValue> editUtilities = null)
        {
            JsonValue content = Json.Parse(FrameworkProofContent.Read("running-inputs.json")); edit?.Invoke(content);
            JsonValue basis = Json.Parse(PointGame.Definitions);
            basis.Get("scenarios")[0].Set("seats", JsonValue.Number("2")); basis.Get("scenarios")[0].Set("default_seats", JsonValue.Number("2"));
            basis.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
            editBasis?.Invoke(basis);
            if (utilities)
            {
                basis.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(UtilitySignalsModule.ModuleId));
                basis.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.utility-consumer"));
                content.Get("entities")[1].Get("components").Set("utility_signal", Json.Parse("{\"id\":\"utility\",\"consumer\":\"process\"}"));
            }
            JsonValue utility = utilities ? Json.Parse(FrameworkProofContent.Read("utility-consumers.json")) : null; editUtilities?.Invoke(utility);
            return PointGame.Factory(Text(basis), additionalModule: new TransformationSupplyProofModule(), moduleDeclaration: Declaration, moduleDocument: Text(content),
                flowDefinitions: FrameworkProofContent.Read("running-input-flows.json"),
                extraModules: utilities ? new GameModule[] { new UtilitySignalsModule(), new UtilityConsumerProofModule() } : null,
                extraDeclarations: utilities ? "[{\"id\":\"sim.utility-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.utility-consumer\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.utility-signals\",\"version\":\"1.0.0\"}],\"documents\":[\"utility-consumers.json\"]}]" : null,
                extraDocuments: utilities ? new Dictionary<string, string> { ["utility-consumers.json"] = Text(utility) } : null, startupProfiles: utilities ? new[] { "running-inputs.start", "utility-consumers.start" } : new[] { "running-inputs.start" });
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 611 }; request.Mods.Add("proof.transformation-supply"); return factory.NewGame(request); }
        private static Entity Find(SimGame game, string prototype = "running.factory")
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype) return entity; throw new InvalidOperationException("missing " + prototype); }
        private static IInventory Stock(SimGame game) => Find(game).Get<IInventory>();
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static TransformationSnapshot State(SimGame game)
        { if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game).Id, Capability, out var state)) throw new InvalidOperationException("missing process facts"); return state; }
        private static void Tick(SimGame game) { if (game.RunTicks(1) != 1) throw new InvalidOperationException("fault: " + game.Fault); }
        private static void Enqueue(Check c, SimGame game, bool repeat = false)
            => c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = Find(game).Id, Capability = Capability, Transformation = "running.make", Repeat = repeat }).Accepted, "ordinary enqueue accepted");
        private static void Control(Check c, SimGame game, bool enabled, Fix target)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Find(game).Id, Operation = Capability, Enabled = enabled, Target = target }).Accepted, "ordinary process control accepted");
        private static void Cancel(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = Find(game).Id, Capability = Capability, Index = 0 }).Accepted, "ordinary process cancellation accepted");
        private static int Results(SimGame game)
        { int count = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "running.result") count++; return count; }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 800 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            {
                var jobs = new List<string>(); foreach (Job job in game.World.Services.Get<WorkBoard>().Jobs) jobs.Add(job.Id + "/" + job.State + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timeout tick=" + game.Tick + ", phase=" + State(game).Phase + ", blocker=" + State(game).Blocker + ", elapsed=" + State(game).Operating.ElapsedTicks + ", jobs=" + string.Join(";", jobs));
            }
        }
        private static void Fluid(JsonValue json, int stock)
        {
            json.Set("resources", Json.Parse("[{\"id\":\"coolant\",\"unit\":\"unit.capacity.volume\",\"category\":\"fluid\",\"phase\":\"liquid\",\"traits\":[],\"capacity\":{\"unit.capacity.volume\":1,\"unit.capacity.mass\":1}}]"));
            json.Set("capacity_policies", Json.Parse("[{\"id\":\"running.mixed\",\"limits\":{\"unit.capacity.mass\":40}}]"));
            Component(json, "storage").Set("capacity_policy", JsonValue.Of("running.mixed"));
            if (stock > 0) Component(json, "storage").Get("initial_stock").Set("coolant", JsonValue.Number(stock.ToString()));
            json.Get("transformations")[0].Get("operating_inputs").Add(Json.Parse("{\"resource\":\"coolant\",\"quantity\":1,\"period\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}}"));
        }
        private static void SupplyMass(JsonValue basis) => basis.Get("resources")[0].Get("capacity").Set("unit.capacity.mass", JsonValue.Number("1"));
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "explicit current engine identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "direct breaking save layout");
            c.That(Find(game).Get<ITransformer>(Capability) is IUtilityConsumer, "native component exposes operating contract");
            c.That(ReferenceEquals(((IUtilityConsumer)Find(game).Get<ITransformer>(Capability)).ResolveUtilityPort(game.World, Supply(game)).ResolveStore(game.World).Store, Stock(game)), "explicit operating port selects the existing inventory");
            c.Equal(0L, State(game).Operating.ElapsedTicks, "new processor has no earned running time");
            foreach (string invalid in new[] { "missing", "unknown", "zero", "period-zero", "denominator", "duplicate", "native", "unit", "capacity", "overflow" })
            {
                c.Throws<ContentException>(() => New(Factory(json =>
                {
                    JsonValue row = json.Get("transformations")[0];
                    switch (invalid)
                    {
                        case "missing": row.Remove("operating_inputs"); break;
                        case "unknown": Rate(json).Set("resource", JsonValue.Of("missing")); break;
                        case "zero": Rate(json).Set("quantity", JsonValue.Number("0")); break;
                        case "period-zero": Rate(json).Get("period").Set("numerator", JsonValue.Number("0")); break;
                        case "denominator": Rate(json).Get("period").Set("denominator", JsonValue.Number("0")); break;
                        case "duplicate": row.Get("operating_inputs").Add(Json.Parse(Text(Rate(json)))); break;
                        case "native": Rate(json).Set("quantity", JsonValue.Number("0.5")); break;
                        case "unit": Rate(json).Get("period").Set("unit", JsonValue.Of("frame")); break;
                        case "capacity": Component(json, "storage").Set("capacity_policy", JsonValue.Of("fixture.count.4")); Component(json, "storage").Set("initial_stock", Json.Parse("{}")); break;
                        case "overflow": Rate(json).Get("period").Set("numerator", JsonValue.Number("1")); Rate(json).Get("period").Set("denominator", JsonValue.Number(long.MaxValue.ToString())); break;
                    }
                })), "strict authored rate refusal: " + invalid);
            }
            IResourceRate rate = game.World.Content.Get<TransformationDef>("running.make").OperatingInputs[0];
            c.That(ReferenceEquals(rate.Resource, game.World.Content.Resource(Supply(game))), "rates retain catalog resource alias and native unit");
            c.Equal(Fix.One, ResourceRates.Accumulated(new[] { rate }, game.SimulationTime, Stock(game).CapacityPolicy, new WorkProgress(3), ResourceRateRounding.Ceiling).AmountOf(Supply(game)), "three elapsed minutes consume one native count");
            c.Equal(Fix.FromInt(2), ResourceRates.Accumulated(new[] { rate }, game.SimulationTime, Stock(game).CapacityPolicy, new WorkProgress(4), ResourceRateRounding.Ceiling).AmountOf(Supply(game)), "cumulative ceiling purchases a new native quantum once");
            c.That(ResourceRates.Between(new[] { rate }, game.SimulationTime, Stock(game).CapacityPolicy, new WorkProgress(1), new WorkProgress(checked(1 + 1)), ResourceRateRounding.Ceiling).IsEmpty, "rounding is not repeated each update");
            var wide = new ResourceRate(rate.Resource, Fix.One, long.MaxValue, 1, DurationUnit.Second);
            c.Equal(Fix.FromInt(60), ResourceRates.Accumulated(new[] { wide }, game.SimulationTime, Stock(game).CapacityPolicy, new WorkProgress(long.MaxValue), ResourceRateRounding.Ceiling).AmountOf(Supply(game)), "wide time and rational multiplication never narrow to int or fixed work");
            long finalTick = long.MaxValue;
            c.Throws<OverflowException>(() => ResourceRates.Between(new[] { rate }, game.SimulationTime, Stock(game).CapacityPolicy, new WorkProgress(finalTick), new WorkProgress(checked(finalTick + 1)), ResourceRateRounding.Ceiling), "unrepresentable next elapsed time refuses explicitly");
            c.Throws<ArgumentException>(() => ResourceRates.Accumulated(new[] { rate, rate }, game.SimulationTime, Stock(game).CapacityPolicy, new WorkProgress(1), ResourceRateRounding.Ceiling), "duplicate rate contract rows refuse");
            SimGame fluid = New(Factory(json => Fluid(json, 2), SupplyMass));
            ResourceId coolant = fluid.World.Content.ResourceId("coolant");
            var fraction = new ResourceRate(fluid.World.Content.Resource(coolant), Fix.One, 7, 2, DurationUnit.Minute);
            Fix raw = ResourceRates.Accumulated(new[] { fraction }, fluid.SimulationTime, Stock(fluid).CapacityPolicy, new WorkProgress(1), ResourceRateRounding.Ceiling).AmountOf(coolant);
            c.Equal(Fix.FromRaw((Fix.One.Raw * 2 + 6) / 7), raw, "unaligned rational fluid rate uses exact cumulative native precision");
            Enqueue(c, game); TransformationSnapshot saved = State(game); ulong hash = game.Hash();
            c.Equal(1L, saved.Operating.ElapsedTicks, "ordinary enqueue/update earns actual running quantum");
            c.That(saved.Operating.Consumed.AmountOf(Supply(game)) == Fix.One && saved.Wip.Stock.AmountOf(Supply(game)) == Fix.FromInt(3) && saved.Tools.Held.AmountOf(Supply(game)) == Fix.One, "burn, raw WIP and tools are distinct real facts");
            for (int i = 0; i < 3; i++) State(game);
            c.Equal(hash, game.Hash(), "private operating reads are pure");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game).Id, Capability, out _), "company-private rates/progress facts do not leak");
            SimGame twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "sole processor codec retains running time");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "interleaved saved continuation preserves cumulative rounding");
            c.Equal(1L, saved.Operating.ElapsedTicks, "retained running snapshot stays detached");
            SimGame corrupt = factory.Load(twin.Save());
            typeof(Transformer).GetField("_runningTicks", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Find(corrupt).Get<Transformer>(), -1L);
            c.Throws<CorruptStateException>(() => corrupt.Save(), "negative elapsed lifetime refuses checkpoint");
            SimGame missing = New(Factory(json => Component(json, "storage").Set("initial_stock", Json.Parse("{\"supply\":4}")))); Enqueue(c, missing);
            c.That(State(missing).Phase == TransformationPhase.Supplying && State(missing).Blocker == TransformerManager.BlockerOperatingInputs, "first operating shortage blocks before startup");
            c.That(State(missing).Wip.Execution == 0 && State(missing).Operating.ElapsedTicks == 0 && Stock(missing).Amount(Supply(missing)) == Fix.FromInt(4) && Stock(missing).Reservations.Count == 0, "no startup, payment, promise or fuel prefix on shortage");
            SimGame mixed = New(Factory(json => Fluid(json, 2), SupplyMass)); Enqueue(c, mixed); Tick(mixed);
            TransformationSnapshot before = State(mixed); Fix stockBefore = Stock(mixed).Amount(Supply(mixed)); Tick(mixed);
            c.That(State(mixed).Blocker == TransformerManager.BlockerOperatingInputs && State(mixed).Operating.ElapsedTicks == before.Operating.ElapsedTicks && State(mixed).Progress.Equals(before.Progress), "missing second resource freezes entire mixed running increment");
            c.Equal(stockBefore, Stock(mixed).Amount(Supply(mixed)), "whole mixed shortage never consumes available sibling resource");
            c.That(Utility(mixed).Resources.Count == 2 && !Utility(mixed).Satisfied && Utility(mixed).Blocker == UtilityCodes.Shortage,
                "one generic utility demand retains multiple native resources and whole-set shortage");
            SimGame future = factory.Load(twin.Save());
            typeof(Transformer).GetField("_runningTicks", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Find(future).Get<Transformer>(), future.Tick + 10);
            c.Throws<CorruptStateException>(() => future.Save(), "future running lifetime refuses checkpoint");
            c.That(future.RunTicks(1) == 0 && future.Fault != null, "invalid running lifetime faults the ordinary engine Update");
            c.Throws<InvalidOperationException>(() => future.Hash(), "faulted process cannot publish a resumable hash");
            SimGame empty = New(Factory(json => json.Get("transformations")[0].Set("operating_inputs", Json.Parse("[]")))); Enqueue(c, empty);
            c.That(State(empty).Operating.NextInputs.IsEmpty && State(empty).Operating.Consumed.IsEmpty, "explicit empty rates earn work without invented fuel");
            SimGame rational = New(Factory(editBasis: basis =>
            {
                JsonValue time = basis.Get("time_definitions")[0]; time.Get("quantum").Set("numerator", JsonValue.Number("1"));
                time.Get("quantum").Set("denominator", JsonValue.Number("2")); time.Set("calendar", JsonValue.Null());
            }));
            var exact = new ResourceRate(rational.World.Content.Resource(Supply(rational)), Fix.One, 1, 2, DurationUnit.Second);
            c.Equal(Fix.One, ResourceRates.Accumulated(new[] { exact }, rational.SimulationTime, Stock(rational).CapacityPolicy, new WorkProgress(1), ResourceRateRounding.Ceiling).AmountOf(Supply(rational)), "rational session quantum matches exact rational rate without tick-period rounding");
            UtilityFacts(c);
        }
        private static void Physical(Check c)
        {
            foreach (string speed in new[] { "0.5", "1", "2" })
            {
                GameFactory factory = Factory(json => Component(json, "transformer").Get("operation").Set("target", JsonValue.Number(speed)));
                SimGame game = New(factory); Enqueue(c, game); SimGame twin = factory.Load(game.Save());
                Reach(game, () => Results(game) == 1, "timed rate " + speed, twin);
                int fuel = speed == "0.5" ? 8 : speed == "1" ? 4 : 2;
                c.Equal(Fix.FromInt(20 - 3 - fuel), Stock(game).Amount(Supply(game)), "speed " + speed + " changes real elapsed fuel use, not fixed work budget");
                c.That(State(game).QueueCount == 0 && State(game).Operating.ElapsedTicks == 0 && Stock(game).Reservations.Count == 0, "completion resets batch time and releases tools");
                c.Equal(game.Hash(), twin.Hash(), "rate " + speed + " saved twins complete identically");
            }
            GameFactory local = Factory(); SimGame paused = New(local); Enqueue(c, paused); Control(c, paused, false, Fix.One);
            var frozen = State(paused); Fix fuelBefore = Stock(paused).Amount(Supply(paused)); SimGame restored = local.Load(paused.Save());
            for (int i = 0; i < 7; i++) { Tick(paused); Tick(restored); }
            c.That(State(paused).Operating.ElapsedTicks == frozen.Operating.ElapsedTicks && State(paused).Progress.Equals(frozen.Progress), "disabled updates earn no running time or work");
            c.Equal(fuelBefore, Stock(paused).Amount(Supply(paused)), "disabled process burns no fuel"); c.Equal(paused.Hash(), restored.Hash(), "saved pause has deterministic continuation");
            Control(c, paused, true, Fix.FromInt(2)); Control(c, restored, true, Fix.FromInt(2));
            Reach(paused, () => Results(paused) == 1, "changed speed after pause", restored); c.Equal(paused.Hash(), restored.Hash(), "changed target uses real running time across resume");
            SimGame cancelled = New(local); Enqueue(c, cancelled); Reach(cancelled, () => State(cancelled).Operating.ElapsedTicks == 4, "burn before cancellation");
            c.Equal(Fix.FromInt(2), State(cancelled).Operating.Consumed.AmountOf(Supply(cancelled)), "four running minutes spent two native fuel counts");
            Cancel(c, cancelled); c.Equal(Fix.FromInt(18), Stock(cancelled).Amount(Supply(cancelled)), "cancellation returns raw inputs and tools, never burned fuel");
            c.That(State(cancelled).QueueCount == 0 && State(cancelled).Operating.ElapsedTicks == 0 && Results(cancelled) == 0, "cancelled process has no result or extra time owner");
            GatesAndOutputWait(c);
            PhysicalSupply(c);
            UtilityExecution(c);
        }
        private static OperationsView UtilityView(SimGame game, int company = 0) => game.Seat(company).View.Feature<OperationsView>();
        private static UtilityDemandSnapshot Utility(SimGame game, string prototype = "running.factory", string capability = "process")
        {
            if (!UtilityView(game).TryGetUtilityDemand(Find(game, prototype).Id, new Code(capability), out var facts))
                throw new InvalidOperationException("missing named utility demand"); return facts;
        }
        private static AutomationSignalSnapshot UtilitySignal(SimGame game, string signal, string prototype = "running.factory")
        {
            Code refusal = UtilityView(game).ReadSignal(Find(game, prototype).Id, new Code("utility"), signal, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("utility signal refused " + refusal); return facts;
        }
        private static void UtilityFacts(Check c)
        {
            GameFactory factory = Factory(utilities: true); SimGame game = New(factory); var ids = new List<Code>();
            c.That(UtilityView(game).CopyUtilityConsumerIds(Find(game).Id, ids) && ids.Count == 1 && ids[0] == Capability,
                "private consumer discovery uses existing provider identity");
            c.That(!Utility(game).Drawing && !Utility(game).Satisfied && Utility(game).Blocker == UtilityCodes.Inactive,
                "idle processor does not fabricate active utility demand");
            c.That(!UtilitySignal(game, "utility.ready").Available && UtilitySignal(game, "utility.ready").Blocker == UtilityCodes.Inactive,
                "inactive utility is typed unavailable, not a ready default");
            c.That(!UtilitySignal(game, "utility.requested").Available && UtilitySignal(game, "utility.requested").Blocker == UtilityCodes.Inactive,
                "idle numeric utility demand is inactive, never an invented zero or resource incompatibility");
            UtilityDemandSnapshot independent = Utility(game, "utility.independent", "meter");
            c.That(Find(game, "utility.independent").Get<IUtilityConsumer>(new Code("meter")) is ProofUtilityConsumer,
                "unrelated public-only consumer requires no native transformer cast");
            c.That(independent.Satisfied && independent.NextInputs.AmountOf(Supply(game)) == Fix.FromInt(2) &&
                independent.Available.AmountOf(Supply(game)) == Fix.FromInt(2) && independent.Consumed.IsEmpty,
                "independent declared demand observes real stock without inventing consumption");
            c.Equal(Fix.One, UtilitySignal(game, "utility.pairs", "utility.independent").Value.Value, "generic consumer shares exact resource unit conversion");
            c.Throws<NotSupportedException>(() => ((IList<ResourceId>)independent.Resources).Clear(), "consumer resources are immutable detached identities");
            var holder = new ReservationOwner("proof.utility", 1); IInventory stock = Find(game, "utility.independent").Get<IInventory>();
            ReservationToken claim = stock.ReserveOut(holder, Supply(game), Fix.FromInt(4)); c.That(claim.IsValid, "actual reservation owns generic consumer shortage");
            var shortfall = Utility(game, "utility.independent", "meter");
            c.That(!shortfall.Satisfied && shortfall.Blocker == UtilityCodes.Shortage && shortfall.Available.AmountOf(Supply(game)) == Fix.One,
                "whole demand is not satisfied by partial unpromised stock");
            c.Equal(Fix.Zero, UtilitySignal(game, "utility.ready", "utility.independent").Value.Value, "actual shortage is available false readiness");
            c.Equal(Fix.FromInt(2), UtilitySignal(game, "utility.requested", "utility.independent").Value.Value, "shortage retains authored demand quantity");
            c.Equal(Fix.One, UtilitySignal(game, "utility.available", "utility.independent").Value.Value, "shortage retains real partial contribution");
            c.That(independent.Satisfied, "retained satisfied facts never follow later claim mutations");
            ulong hash = game.Hash(); for (int i = 0; i < 6; i++) Utility(game, "utility.independent", "meter");
            c.Equal(hash, game.Hash(), "utility read never allocates claims or advances work/time");
            SimGame twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "utility facts save only through existing stock/provider owners");
            stock.ReleaseOut(holder, claim); c.That(Utility(game, "utility.independent", "meter").Satisfied && !Utility(twin, "utility.independent", "meter").Satisfied,
                "release restores actual readiness without changing saved session");
            c.That(!UtilityView(game, 1).TryGetUtilityDemand(Find(game).Id, Capability, out _), "foreign utility demand is withheld");
            c.That(!UtilityView(game).TryGetUtilityDemand(Find(game).Id, new Code("missing"), out _), "missing consumer is never substituted");
            c.Equal(Codes.NotOwned, UtilityView(game, 1).ReadSignal(Find(game, "utility.independent").Id, new Code("utility"), "utility.available", out var hidden),
                "private signal admission precedes consumer reads"); c.That(!hidden.Value.HasValue, "private utility value is not disclosed");
            foreach (int mode in new[] { 1, 2, 3, 4 })
            {
                SimGame hostile = New(factory); Find(hostile, "utility.independent").Get<ProofUtilityConsumer>().Fault = mode;
                c.Throws<InvalidOperationException>(() => Utility(hostile, "utility.independent", "meter"), "mutating/malformed public consumer refuses " + mode);
            }
            SimGame range = New(factory); Find(range, "utility.independent").Get<ProofUtilityConsumer>().Fault = 5;
            var unavailable = UtilitySignal(range, "utility.ready", "utility.independent");
            c.That(!unavailable.Available && !unavailable.Value.HasValue && unavailable.Blocker == new Code("proof.utility-range"),
                "unrepresentable draw never pretends to be a normal shortage boolean");
            foreach (string invalid in new[] { "consumer", "metric", "schema", "unit" })
                c.Throws<ContentException>(() => New(Factory(utilities: true, editUtilities: json =>
                {
                    switch (invalid)
                    {
                        case "consumer": json.Get("entities")[0].Get("components").Get("utility_signal").Set("consumer", JsonValue.Of("missing")); break;
                        case "metric": json.Get("resource_signals")[0].Set("metric", JsonValue.Of("utility.fake")); break;
                        case "schema": json.Get("signals")[0].Get("providers")[0].Set("schema", JsonValue.Number("2")); break;
                        case "unit": json.Get("resource_signals")[0].Set("unit", JsonValue.Of("unit.capacity.volume")); break;
                    }
                })), "invalid authored generic utility refuses before play " + invalid);
        }
        private static void UtilityExecution(Check c)
        {
            GameFactory factory = Factory(utilities: true); SimGame game = New(factory); Enqueue(c, game);
            c.That(Utility(game).Satisfied && Utility(game).NextInputs.IsEmpty, "paid cumulative quantum permits an exact zero next demand");
            c.Equal(Fix.One, UtilitySignal(game, "utility.consumed").Value.Value, "consumed signal reports actual running consumption, not hypothetical delivery");
            Tick(game); Tick(game); var due = Utility(game);
            c.That(due.ElapsedTicks == 3 && due.NextInputs.AmountOf(Supply(game)) == Fix.One && due.Available.AmountOf(Supply(game)) == Fix.One,
                "next native utility draw comes from actual running lifetime and one game clock");
            var pairs = UtilitySignal(game, "utility.pairs");
            c.That(!pairs.Available && pairs.Blocker == ResourceSignalCodes.QuantityUnavailable, "odd native draw cannot be rounded into discrete utility pairs");
            var holder = new ReservationOwner("proof.utility-wait", 1);
            ReservationToken claim = Stock(game).ReserveOut(holder, Supply(game), Stock(game).Available(Supply(game)));
            c.That(claim.IsValid && !Utility(game).Satisfied && Utility(game).Blocker == UtilityCodes.Shortage, "actual competing claim makes whole utility draw wait");
            var before = State(game); Fix amount = Stock(game).Amount(Supply(game)); SimGame twin = factory.Load(game.Save());
            for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); }
            c.That(State(game).Progress.Equals(before.Progress) && Utility(game).ElapsedTicks == due.ElapsedTicks &&
                Utility(game).Consumed.AmountOf(Supply(game)) == Fix.One && Stock(game).Amount(Supply(game)) == amount,
                "utility shortage advances no work, burn or partial draw");
            c.Equal(game.Hash(), twin.Hash(), "saved utility wait preserves original request/receipt/consumption owners");
            long time = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); game.Update();
            c.Equal(time, game.Clock.StepIndex, "single paused game clock freezes utility execution"); c.Equal(hash, game.Hash(), "paused utility observations create no state"); game.Clock.Resume();
            Stock(game).ReleaseOut(holder, claim); Stock(twin).ReleaseOut(holder, claim); Tick(game); Tick(twin);
            c.That(Utility(game).ElapsedTicks == 4 && Utility(game).Consumed.AmountOf(Supply(game)) == Fix.FromInt(2),
                "released claim resumes exactly one real running draw");
            c.Equal(game.Hash(), twin.Hash(), "saved release/resume continues identical utility consumption");
            Control(c, game, false, Fix.One);
            c.That(!Utility(game).Drawing && Utility(game).Blocker == UtilityCodes.Inactive, "disabled processor retains history without active utility satisfaction");
            c.That(!UtilitySignal(game, "utility.ready").Available, "disabled demand never publishes a fabricated ready signal");
        }
        private static void GatesAndOutputWait(Check c)
        {
            GameFactory factory = Factory(json =>
            {
                json.Set("signals", Json.Parse("[{\"id\":\"running.target\",\"kind\":\"number\",\"metric\":\"operation.target\",\"unit\":\"unit.work-rate\",\"providers\":[{\"component\":\"transformer\",\"schema\":1}]}]"));
                json.Get("entities")[1].Get("components").Set("signal_transformation_gate", Json.Parse("{\"id\":\"safe-rate\",\"transformations\":[\"running.make\"],\"conditions\":[{\"id\":\"rate\",\"source\":\"process\",\"signal\":\"running.target\",\"minimum\":1,\"maximum\":2,\"unit\":\"unit.work-rate\",\"blocker\":\"process.rate-gated\"}]}"));
            });
            SimGame game = New(factory); Enqueue(c, game); var running = State(game); Fix held = Stock(game).Amount(Supply(game));
            Control(c, game, true, Fix.FromRatio(1, 2)); SimGame twin = factory.Load(game.Save());
            for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); }
            c.That(State(game).Blocker == new Code("process.rate-gated") && State(game).Operating.ElapsedTicks == running.Operating.ElapsedTicks && State(game).Progress.Equals(running.Progress), "authored gate pauses enabled process without work or running time");
            c.Equal(held, Stock(game).Amount(Supply(game)), "signal-gated process draws no running resources"); c.Equal(game.Hash(), twin.Hash(), "gate wait saves identically");
            Control(c, game, true, Fix.One); Control(c, twin, true, Fix.One); Reach(game, () => Results(game) == 1, "gate resume", twin);
            c.Equal(game.Hash(), twin.Hash(), "gate resume uses actual earned lifetime, not elapsed wall time");
            GameFactory blockedFactory = Factory(json =>
            {
                json.Get("transformations")[0].Get("duration").Set("numerator", JsonValue.Number("2"));
                json.Get("transformations")[0].Set("outputs", Json.Parse("{\"supply\":40}"));
            });
            SimGame blocked = New(blockedFactory); Enqueue(c, blocked); Tick(blocked); var finished = State(blocked); Fix inventory = Stock(blocked).Amount(Supply(blocked));
            c.That(finished.Phase == TransformationPhase.Blocked && finished.Blocker == TransformerManager.BlockerOutput && finished.Operating.ElapsedTicks == 2, "real completed work waits for whole output admission");
            SimGame saved = blockedFactory.Load(blocked.Save());
            for (int i = 0; i < 5; i++) { Tick(blocked); Tick(saved); }
            c.That(State(blocked).Operating.ElapsedTicks == finished.Operating.ElapsedTicks && State(blocked).Operating.NextInputs.IsEmpty && Results(blocked) == 0, "completed output wait has no additional running demand or result prefix");
            c.Equal(inventory, Stock(blocked).Amount(Supply(blocked)), "full output wait does not burn fuel"); c.Equal(blocked.Hash(), saved.Hash(), "saved completed-output wait remains deterministic");
            Cancel(c, blocked); c.Equal(Fix.FromInt(19), Stock(blocked).Amount(Supply(blocked)), "completed cancellation returns raw inputs but keeps one fuel count burned");
        }
        private static void PhysicalSupply(Check c)
        {
            GameFactory factory = Factory(json =>
            {
                Component(json, "storage").Set("initial_stock", Json.Parse("{}"));
                Component(json, "transformer").Set("automatic_supply", JsonValue.Of(true));
            });
            SimGame game = New(factory); Enqueue(c, game);
            c.That(State(game).Phase == TransformationPhase.Supplying && State(game).Operating.ElapsedTicks == 0, "no local stock means physical startup wait");
            Reach(game, () => Find(game, "machine").Get<ITransporter>().Cargo.Amount(Supply(game)) == Fix.FromInt(5), "first startup plus operating draw pickup");
            c.That(State(game).Wip.Stock.IsEmpty && Stock(game).Amount(Supply(game)).IsZero && Results(game) == 0, "loaded stock cannot teleport into process");
            c.Equal(Fix.FromInt(15), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "real finite supplier spends complete startup plus first draw");
            Reach(game, () => State(game).Phase == TransformationPhase.Working && State(game).Blocker == TransformerManager.BlockerOperatingInputs, "running fuel shortage");
            var wait = State(game); var demands = new List<LogisticsDemandView>(); game.Seat(0).View.Feature<LogisticsView>().CopyDemands(demands);
            c.That(wait.Operating.ElapsedTicks == 3 && wait.Tools.Held.AmountOf(Supply(game)) == Fix.One && wait.Wip.Stock.AmountOf(Supply(game)) == Fix.FromInt(3), "shortage retains actual elapsed work, raw WIP and protected tool");
            c.That(demands.Count == 1 && demands[0].Source.Entry == Capability, "working demand belongs to actual selected processor");
            c.That(!Utility(game).Satisfied && Utility(game).Blocker == UtilityCodes.Shortage && Utility(game).NextInputs.AmountOf(Supply(game)) == Fix.One,
                "public utility demand exposes real physically supplied running shortage");
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(State(game).Operating.ElapsedTicks == wait.Operating.ElapsedTicks && State(game).Progress.Equals(wait.Progress), "physical fuel wait burns no time or protected tools");
            c.Equal(game.Hash(), twin.Hash(), "saved working shortage and acquired hauling restore identically");
            Reach(game, () => Results(game) == 1, "physical repeated operating draws", twin);
            c.Equal(game.Hash(), twin.Hash(), "physical supply and running consumption continue as saved twins");
            c.That(Stock(game).Amount(Supply(game)) == Fix.One && Stock(game).Reservations.Count == 0 && State(game).QueueCount == 0, "only actual tool remains; no duplicate fuel or WIP");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "eight physically moved units equal raw3/tool1/burn4");
            var history = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
            Fix burned = Fix.Zero;
            foreach (var entry in history.Entries)
                if (entry.Item.Cause == TransformerManager.CauseOperatingInputs) burned += entry.Item.Amount;
            c.Equal(Fix.FromInt(4), burned, "sole native flow history records all real operating consumption exactly once");
            c.That(game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Status == ResourceFlowReadStatus.NotOwned, "operating consumption history remains company-private");
            Fix final = Stock(game).Amount(Supply(game)); for (int i = 0; i < 4; i++) Tick(game);
            c.Equal(final, Stock(game).Amount(Supply(game)), "idle processor has no implicit operating draw");
            c.That(!Utility(game).Drawing && Utility(game).Blocker == UtilityCodes.Inactive, "completed physical supply produces no implicit utility demand");
        }
    }
}
