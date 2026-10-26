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

namespace Beep.SimEngine.Tests
{
    public static partial class TransformationSupplyTests
    {
        private static readonly Code First = new Code("a-process"), Second = new Code("b-process");
        private const string Declaration = "{\"id\":\"proof.transformation-supply\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"},{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"transformation-supply.json\"]}";
        public static void Register()
        {
            TestRunner.Add("Transformation supply: named demand ownership and private saved contracts", Contracts);
            TestRunner.Add("Transformation supply: shared physical delivery, cancellation and saved continuation", Physical);
            TestRunner.Add(AccountingCase, Accounting);
            TestRunner.Add(MixedResultCase, MixedResult);
        }
        private static string Text(JsonValue json) { var text = new StringBuilder(); json.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(Action<JsonValue> edit = null, bool supplied = true, Action<JsonValue> basisEdit = null)
        {
            JsonValue content = Json.Parse(FrameworkProofContent.Read("transformation-supply.json")); edit?.Invoke(content);
            JsonValue basis = Json.Parse(PointGame.Definitions);
            basisEdit?.Invoke(basis);
            if (!supplied) basis.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("0"));
            basis.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            basis.Get("scenarios")[0].Set("default_seats", JsonValue.Number("2"));
            return PointGame.Factory(Text(basis), additionalModule: new TransformationSupplyProofModule(), moduleDeclaration: Declaration, moduleDocument: Text(content), startupProfiles: new[] { "transformation-supply.start" });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 502 }; request.Mods.Add("proof.transformation-supply");
            return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string prototype = "supply.factory")
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype) return entity;
            throw new InvalidOperationException("missing " + prototype);
        }
        private static ITransformer Provider(SimGame game, Code entry) => Find(game).Get<ITransformer>(entry);
        private static IInventory Stock(SimGame game) => Find(game).Get<IInventory>();
        private static ResourceId Resource(SimGame game) => game.World.Content.ResourceId("supply");
        private static LogisticsService Logistics(SimGame game) => game.World.Services.Get<LogisticsService>();
        private static LogisticsDemandId Source(SimGame game, Code entry) => new LogisticsDemandId(TransformationModule.ModuleId, Find(game).Id.Value, entry);
        private static Bill Want(SimGame game, int amount) => new Bill(new[] { new ResourceAmount(Resource(game), Fix.FromInt(amount)) });
        private static TransformationSnapshot State(SimGame game, Code entry)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game).Id, entry, out var snapshot)) throw new InvalidOperationException("missing " + entry);
            return snapshot;
        }
        private static List<LogisticsDemandView> Demands(SimGame game, int seat = 0)
        { var facts = new List<LogisticsDemandView>(); game.Seat(seat).View.Feature<LogisticsView>().CopyDemands(facts); return facts; }
        private static int Results(SimGame game)
        { int count = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "supply.result") count++; return count; }
        private static void Tick(SimGame game) { if (game.RunTicks(1) != 1) throw new InvalidOperationException("tick fault: " + game.Fault); }
        private static void Pair(SimGame game, bool reverse = false)
        {
            foreach (Code entry in reverse ? new[] { Second, First } : new[] { First, Second })
                game.Seat(0).Submit(new EnqueueTransformationCommand { Transformer = Find(game).Id, Capability = entry, Transformation = entry == First ? "supply.first" : "supply.second" });
            Tick(game);
        }
        private static void Control(Check c, SimGame game, Code entry, bool enabled)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Find(game).Id, Operation = entry, Enabled = enabled, Target = Fix.One }).Accepted, "ordinary operation control accepted for " + entry);
        private static void Cancel(Check c, SimGame game, Code entry)
            => c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = Find(game).Id, Capability = entry, Index = 0 }).Accepted, "ordinary named cancellation accepted for " + entry);
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 180 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            {
                var jobs = new List<string>(); foreach (Job job in game.World.Services.Get<WorkBoard>().Jobs) jobs.Add(job.Id + "/" + job.State + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ", first=" + State(game, First).Phase + "/" + State(game, First).Blocker + ", second=" + State(game, Second).Phase + "/" + State(game, Second).Blocker + ", jobs=" + string.Join(";", jobs));
            }
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(supplied: false); SimGame game = New(factory); Pair(game, true);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "named supply has explicit breaking identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "source entry is a breaking save fact");
            c.Equal(StateCodecTests.ExpectedLogisticsSchema, Logistics(game).StateSchemaVersion, "existing logistics codec owns source entry");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, game.World.Services.Get<WorkBoard>().StateSchemaVersion, "existing physical job codec owns provenance entry");
            c.That(Provider(game, First) is Transformer && Provider(game, Second) is Transformer && !ReferenceEquals(Provider(game, First), Provider(game, Second)), "two named components reuse the same native owner, not a copied processor");
            c.Throws<ArgumentException>(() => Find(game).Get<Transformer>(), "ambiguous concrete runtime lookup cannot select a fabricated first provider");
            c.Throws<ArgumentException>(() => Find(game).Def.Component<TransformerDef>(), "ambiguous concrete definition lookup requires identity");
            c.That(Find(game).Def.HasCapability<Transformer>() && ReferenceEquals(Find(game).Get<Transformer>(First), Provider(game, First)), "named concrete resolution and existence reuse actual slot/parts");
            c.That(Find(game).Def.Component<TransformerDef>(Second).CapabilityId == Second && Find(game).Def.HasCapability<ITransformer>(Second), "named content metadata resolves actual definition");
            c.Equal(2, Demands(game).Count, "two automatic providers compose and submit distinct rows");
            c.Equal(Fix.FromInt(7), Logistics(game).DemandOf(Find(game).Id, Resource(game)), "shared inventory demand aggregates both complete input/tool requirements");
            c.That(State(game, First).Wip.Stock.IsEmpty && State(game, Second).Wip.Stock.IsEmpty && Results(game) == 0, "empty physical stock has no fabricated progress/output prefix");
            c.That(Source(game, First).IsValid && !Source(game, First).Equals(Source(game, Second)), "actual named capabilities distinguish same-entity sources");
            var ids = new HashSet<LogisticsDemandId> { Source(game, First), Source(game, Second) }; c.Equal(2, ids.Count, "equality and hash collection retain both entries");
            c.That(typeof(LogisticsDemandId).GetConstructor(new[] { typeof(string), typeof(long) }) == null, "ambiguous constructor is removed");
            c.That(typeof(ITransformationSupplyRequests).GetMethod("RequestInputs").GetParameters()[0].ParameterType == typeof(ITransformer), "supply requires actual named provider");
            c.That(typeof(ITransformationSupplyRequests).GetMethod("WithdrawInputs").GetParameters()[0].ParameterType == typeof(ITransformer), "withdrawal cannot infer entity-wide scope");
            c.Throws<ArgumentException>(() => new LogisticsDemandId("sim.transformations", 1, new Code("Bad Entry")), "noncanonical entry refuses");
            var wide = new LogisticsDemandId("proof.wide", (long)int.MaxValue + 21, Second); RoundTrip(c, wide);
            RoundTrip(c, Source(game, First)); RoundTrip(c, new LogisticsDemandId(LogisticsService.StockDemandScope, Find(game).Id.Value, Code.None));
            ulong hash = game.Hash(); var queue = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(queue);
            c.That(queue.Count == 1 && queue[0].Source.Equals(Source(game, First)) && queue[0].Requested == Fix.FromInt(7), "same-cost/priority/age sources tie by stable named entry into one aggregate haul offer");
            c.Equal(0, Demands(game, 1).Count, "private named source facts never leak to another company");
            c.Equal(hash, game.Hash(), "all source and queue projections remain pure");
            SimGame saved = factory.Load(game.Save()); c.Equal(hash, saved.Hash(), "two source entries restore without copied records/authority");
            c.Equal(Second, Demands(saved)[1].Source.Entry, "saved request keeps its actual capability identity");
            var inspect = game.World.Services.Get<ILogisticsDemandSources>(); Entity entity = Find(game);
            foreach (Code entry in new[] { Code.None, new Code("missing") })
                c.Equal(LogisticsDemandStatus.Invalid, inspect.InspectSource(Source(game, entry), entity.Owner, entity.Id).Status, "missing named transformation source refuses: " + entry);
            c.Equal(LogisticsDemandStatus.Invalid, inspect.InspectSource(new LogisticsDemandId(TransformationModule.ModuleId, entity.Id.Value + 1, First), entity.Owner, entity.Id).Status, "source entity cannot disagree with destination");
            c.Equal(LogisticsDemandStatus.Invalid, inspect.InspectSource(Source(game, First), new PlayerId(1), entity.Id).Status, "named source company cannot be forged");
            c.Equal(LogisticsDemandStatus.Invalid, inspect.InspectSource(new LogisticsDemandId(LogisticsService.StockDemandScope, entity.Id.Value, First), entity.Owner, entity.Id).Status, "stock owner refuses an unowned provider entry");
            c.Equal(LogisticsDemandStatus.Invalid, game.World.Services.Get<WorkBoard>().CheckSource(new LogisticsDemandId(WorkModule.ModuleId, 1, First), entity.Owner, entity.Id).Status, "order owner refuses an unowned provider entry");
            var requests = game.World.Services.Get<ITransformationSupplyRequests>(); long firstAge = Demands(game)[0].CreatedTick, secondAge = Demands(game)[1].CreatedTick;
            requests.RequestInputs(Provider(game, First), new[] { LogisticsSupplyRequest.Entity(Find(game).Id, Want(game, 5)) }, 10);
            c.Equal(2, Demands(game).Count, "refreshing one provider retains sibling row");
            c.Equal(Fix.FromInt(8), Logistics(game).DemandOf(entity.Id, Resource(game)), "one changed bill and unchanged sibling aggregate exactly");
            c.That(Demands(game)[0].CreatedTick == firstAge && Demands(game)[1].CreatedTick == secondAge && Demands(game)[1].Quantity == Fix.FromInt(3), "refresh preserves each surviving row's independent age and amount");
            hash = game.Hash(); SimGame foreign = New(factory);
            c.Throws<ArgumentException>(() => requests.RequestInputs(Provider(foreign, First), new[] { LogisticsSupplyRequest.Entity(Find(foreign).Id, Want(game, 4)) }, 1), "foreign live same-ID provider cannot write requests");
            c.Throws<ArgumentException>(() => requests.WithdrawInputs(Provider(foreign, First)), "foreign provider cannot erase native rows");
            c.Throws<ArgumentException>(() => requests.RequestInputs(null, new[] { LogisticsSupplyRequest.Entity(Find(game).Id, Want(game, 4)) }, 1), "null provider has no fallback identity");
            c.Equal(hash, game.Hash(), "forged requests/withdrawal leave both sources unchanged");
            requests.WithdrawInputs(Provider(game, First));
            c.That(Demands(game).Count == 1 && Demands(game)[0].Source.Entry == Second && Demands(game)[0].CreatedTick == secondAge, "withdrawal removes only actual named owner");
            c.Equal(Fix.FromInt(3), Logistics(game).DemandOf(entity.Id, Resource(game)), "sibling intent survives complete withdrawal");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "withdrawn source and retained sibling checkpoint identically");
            c.Equal(LogisticsDemandStatus.Active, inspect.InspectSource(Source(game, Second), entity.Owner, entity.Id).Status, "selected sibling remains active");
            Control(c, game, First, false);
            c.Equal(LogisticsDemandStatus.Expired, inspect.InspectSource(Source(game, First), entity.Owner, entity.Id).Status, "disabled named provider expires rather than borrowing sibling queue");
            c.Equal(OperationCodes.Disabled, inspect.InspectSource(Source(game, First), entity.Owner, entity.Id).Refusal, "disabled source retains typed reason");
            c.Equal(Fix.FromInt(3), Logistics(game).DemandOf(entity.Id, Resource(game)), "disabled provider no longer protects idle inventory demand");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "disabled source expiry restores deterministically");
            Cancel(c, game, First);
            c.Equal(LogisticsDemandStatus.Expired, inspect.InspectSource(Source(game, First), entity.Owner, entity.Id).Status, "empty cancelled provider does not inherit sibling liveness");
            c.Equal(1, State(game, Second).QueueCount, "sibling queue survives named cancellation");
            c.Throws<ContentException>(() => New(Factory(json => json.Get("entities")[1].Get("components").Get("transformer")[1].Get("operation").Set("id", JsonValue.Of(First.Id)))), "duplicate named owners still refuse before tick one");
            c.Throws<ContentException>(() => New(Factory(json => json.Get("entities")[1].Get("components").Set("transformer", JsonValue.NewArray()))), "empty named instance array cannot fabricate content");
            c.Throws<ContentException>(() => New(Factory(json => json.Get("entities")[1].Get("components").Get("transformer").Add(JsonValue.Of(false)))), "nonobject named instance refuses");
            c.Throws<ContentException>(() => New(Factory(json => json.Get("entities")[1].Get("components").Set("storage", Json.Parse("[{\"capacity_policy\":\"fixture.count.20\"}]")))), "singular inventory cannot use an instance array to duplicate ownership");
            c.Throws<ContentException>(() => New(Factory(json => json.Get("entities")[1].Get("components").Get("transformer").Add(Json.Parse("{}")))), "missing named runtime identity/settings refuses");
            GameFactory reversedFactory = Factory(json =>
            {
                JsonValue parts = json.Get("entities")[1].Get("components").Get("transformer"), reverse = JsonValue.NewArray();
                reverse.Add(parts[1].Clone()); reverse.Add(parts[0].Clone()); json.Get("entities")[1].Get("components").Set("transformer", reverse);
            }, supplied: false);
            SimGame reversed = New(reversedFactory); Pair(reversed, true);
            c.That(Demands(reversed)[0].Source.Entry == First && Demands(reversed)[1].Source.Entry == Second, "authored array order cannot substitute source ordering");
            c.That(((ITransformerWip)Provider(reversed, First)).WorkInProgress.Endpoint == ((ITransformerWip)Provider(game, First)).WorkInProgress.Endpoint, "canonical named parts keep actual WIP scope stable across array ordering");
            GameFactory tripleFactory = Factory(json =>
            {
                JsonValue parts = json.Get("entities")[1].Get("components").Get("transformer"), third = parts[1].Clone();
                third.Get("operation").Set("id", JsonValue.Of("c-process")); parts.Add(third);
            }, supplied: false);
            SimGame triple = New(tripleFactory); Pair(triple);
            c.That(TestKit.Order(triple, 0, new EnqueueTransformationCommand { Transformer = Find(triple).Id, Capability = new Code("c-process"), Transformation = "supply.second" }).Accepted, "third named processor has no two-instance engine cap");
            c.That(Demands(triple).Count == 3 && Logistics(triple).DemandOf(Find(triple).Id, Resource(triple)) == Fix.FromInt(10), "three names retain independent actual rows under the same registered type");
            c.Equal(triple.Hash(), tripleFactory.Load(triple.Save()).Hash(), "three named same-type component codecs restore each actual instance");
            c.Throws<ArgumentException>(() => Find(triple).Get<Transformer>(), "third named instance never restores a first-slot fallback");
            GameFactory noAutoFactory = Factory(json => json.Get("entities")[1].Get("components").Get("transformer")[1].Set("automatic_supply", JsonValue.Of(false)));
            SimGame noAuto = New(noAutoFactory);
            c.Equal(LogisticsDemandStatus.Invalid, noAuto.World.Services.Get<ILogisticsDemandSources>().InspectSource(Source(noAuto, Second), Find(noAuto).Owner, Find(noAuto).Id).Status, "nonautomatic provider cannot impersonate an automatic source");
            c.Throws<InvalidOperationException>(() => noAuto.World.Services.Get<ITransformationSupplyRequests>().RequestInputs(Provider(noAuto, Second), new[] { LogisticsSupplyRequest.Entity(Find(noAuto).Id, Want(noAuto, 3)) }, 1), "nonautomatic request refuses without automatic fallback");
            CorruptSource(c, factory);
        }
        private static void RoundTrip(Check c, LogisticsDemandId identity)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); var original = identity; LogisticsDemandId.Sync(writer, ref identity); writer.Flush(); byte[] bytes = stream.ToArray();
                identity = default; LogisticsDemandId.Sync(new StateReader(bytes, 0, bytes.Length), ref identity);
                c.Equal(original, identity, "sole identity codec round trips explicit scope/64-bit identity/entry");
            }
        }
        private static void CorruptSource(Check c, GameFactory factory)
        {
            foreach (Code entry in new[] { Code.None, new Code("unknown-owner") })
            {
                SimGame game = New(factory); Pair(game); var log = Logistics(game);
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); int route = 1; writer.Int(ref route); writer.Count(1);
                    EntityId destination = Find(game).Id; ResourceId resource = Resource(game); PlayerId owner = Find(game).Owner;
                    writer.Entity(ref destination); writer.Resource(ref resource); LogisticsDemandSettings.Sync(writer, new LogisticsDemandSettings(true, Fix.One, Fix.One, Fix.One, 1, LogisticsReserveAccess.Protected));
                    bool refill = false; writer.Bool(ref refill); writer.Player(ref owner); var source = Source(game, entry); LogisticsDemandId.Sync(writer, ref source);
                    long created = 0; writer.Long(ref created); writer.Count(0); writer.Flush(); byte[] bytes = stream.ToArray();
                    log.Sync(new StateReader(bytes, 0, bytes.Length));
                    c.Throws<CorruptStateException>(() => game.Hash(), "unknown/unnamed saved provider request cannot checkpoint");
                }
            }
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Pair(game); SimGame restored = factory.Load(game.Save()), isolated = New(factory);
            c.Equal(game.Hash(), restored.Hash(), "saved dual request restores before dispatch");
            c.Equal(Fix.Zero, Stock(game).Amount(Resource(game)), "request is not instant stock");
            Reach(game, () => Find(game, "machine").Get<ITransporter>().Cargo.Amount(Resource(game)).IsPositive && Find(game, "machine").Get<PointPresence>().Position.IsPositive, "loaded physical approach", restored);
            c.Equal(game.Hash(), restored.Hash(), "shared-source haul travels identically after restore");
            Job haul = game.World.Services.Get<WorkBoard>().Job(Find(game, "machine").Get<IWorker>().Job);
            c.That(haul.HaulDemand.Equals(Source(game, First)) && haul.InReservation.IsValid && haul.CargoOutReservation.IsValid, "one selected named source owns actual loaded job/native receipts");
            c.That(Stock(game).Amount(Resource(game)).IsZero && Stock(game).ReservedIn(Resource(game)) == Fix.FromInt(7), "aggregate delivery waits in actual vehicle with receiving capacity protected");
            c.Equal(Fix.FromInt(13), Find(game, "source").Get<IInventory>().Amount(Resource(game)), "loading spends seven actual units once");
            SimGame loaded = factory.Load(game.Save()); c.Equal(game.Hash(), loaded.Hash(), "physical provenance includes named source in saved haul");
            Reach(game, () => State(game, First).Wip.Execution > 0 && State(game, Second).Wip.Execution > 0, "both supplied native executions", loaded);
            c.Equal(game.Hash(), loaded.Hash(), "saved loaded cargo delivers and starts both providers identically");
            c.That(State(game, First).Wip.Stock.AmountOf(Resource(game)) == Fix.FromInt(3) && State(game, Second).Wip.Stock.AmountOf(Resource(game)) == Fix.FromInt(2), "shared local stock becomes two actual distinct WIP owners");
            c.That(State(game, First).Wip.Endpoint != State(game, Second).Wip.Endpoint && Provider(game, First) is ITransformerTools a && Provider(game, Second) is ITransformerTools b && !a.WorkInProgressOwner.Equals(b.WorkInProgressOwner), "native WIP identities and tool owners remain distinct");
            c.That(Stock(game).Amount(Resource(game)) == Fix.FromInt(2) && Stock(game).ReservedOut(Resource(game)) == Fix.FromInt(2), "same inventory holds exact two reusable claims, not a stock copy");
            c.Equal(0, Results(game), "actual manufacture still takes game time after travel/handling");
            c.Equal(Fix.Zero, Logistics(game).DemandOf(Find(game).Id, Resource(game)), "started providers expire/withdraw only their unacquired supply intent");
            c.Equal(0, Demands(game).Count, "native starts clear both fulfilled source rows");
            c.Equal(LogisticsDemandStatus.Expired, game.World.Services.Get<ILogisticsDemandSources>().InspectSource(Source(game, First), Find(game).Owner, Find(game).Id).Status, "working process cannot keep stale supply active");
            SimGame working = factory.Load(game.Save()); Reach(game, () => Results(game) == 2 && State(game, Second).QueueCount == 0, "both timed results", working);
            c.Equal(game.Hash(), working.Hash(), "two WIP/tool scopes complete identically after restore");
            c.That(State(game, First).QueueCount == 0 && State(game, Second).QueueCount == 0 && Stock(game).Amount(Resource(game)) == Fix.FromInt(2) && Stock(game).Reservations.Count == 0, "two completed processes consume five raw units and release both actual tools once");
            for (int i = 0; i < 12; i++) { Tick(game); Tick(working); }
            c.That(Results(game) == 2 && Demands(game).Count == 0 && game.Hash() == working.Hash(), "idle ticks manufacture no extra result or phantom request");
            c.That(Stock(isolated).Amount(Resource(isolated)).IsZero && Results(isolated) == 0 && Demands(isolated).Count == 0, "other session has no shared supply/WIP/queue state");
            LoadedCancellation(c, factory); WorkingCancellation(c, factory); PendingControls(c, factory);
        }
        private static void LoadedCancellation(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Pair(game);
            Reach(game, () => Find(game, "machine").Get<ITransporter>().Cargo.Amount(Resource(game)) == Fix.FromInt(7) && Find(game, "machine").Get<PointPresence>().Position.IsPositive, "cancelled source loaded approach");
            Cancel(c, game, First);
            c.That(State(game, First).QueueCount == 0 && State(game, Second).QueueCount == 1 && Logistics(game).DemandOf(Find(game).Id, Resource(game)) == Fix.FromInt(3), "selected requester cancellation keeps sibling intent only");
            Job haul = game.World.Services.Get<WorkBoard>().Job(Find(game, "machine").Get<IWorker>().Job);
            c.That(haul.HaulDemand.Equals(Source(game, First)) && haul.InReservation.IsValid && haul.CargoOutReservation.IsValid, "expired requester retains the actual acquired physical cargo/provenance");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "expired selected source with loaded cargo is lawful saved physical history");
            Reach(game, () => Results(game) == 1 && State(game, Second).QueueCount == 0, "surviving sibling after loaded source cancellation", twin);
            c.Equal(game.Hash(), twin.Hash(), "source cancellation delivery and sibling result continue identically");
            c.That(Results(game) == 1 && Stock(game).Amount(Resource(game)) == Fix.FromInt(5) && Stock(game).Reservations.Count == 0, "cargo is delivered, only sibling raw input spent, no cancelled result/refund/duplication");
            c.Equal(Fix.FromInt(13), Find(game, "source").Get<IInventory>().Amount(Resource(game)), "ended requester never refunds physical pickup");
        }
        private static void WorkingCancellation(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Pair(game); Reach(game, () => State(game, First).Wip.Execution > 0 && State(game, Second).Wip.Execution > 0, "working cancellation setup");
            long sibling = State(game, Second).Wip.Execution;
            Cancel(c, game, First);
            c.That(State(game, First).QueueCount == 0 && State(game, First).Wip.Stock.IsEmpty && State(game, First).Tools.Held.IsEmpty, "named working cancellation returns only its real WIP/tools");
            c.That(State(game, Second).Wip.Execution == sibling && State(game, Second).Wip.Stock.AmountOf(Resource(game)) == Fix.FromInt(2) && State(game, Second).Tools.Held.AmountOf(Resource(game)) == Fix.One, "sibling execution/progress/material/claims survive physical return");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => Results(game) == 1 && State(game, Second).QueueCount == 0, "working cancellation sibling completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "one cancelled and one live owner restore independently");
            c.That(Stock(game).Amount(Resource(game)) == Fix.FromInt(5) && Stock(game).Reservations.Count == 0 && Results(game) == 1, "native return and sibling completion conserve actual material");
        }
        private static void PendingControls(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Pair(game);
            game.Seat(0).Submit(new ConfigureOperationCommand { Entity = Find(game).Id, Operation = First, Enabled = false, Target = Fix.One });
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "saved pending named control expires only selected demand source");
            c.Equal(Fix.FromInt(3), Logistics(game).DemandOf(Find(game).Id, Resource(game)), "other automatic provider remains independently supplied while first is disabled");
            Reach(game, () => Results(game) == 1 && State(game, Second).QueueCount == 0, "disabled first independent sibling completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "disabled source and active sibling progress share no control state");
            c.That(State(game, First).Wip.Execution == 0 && State(game, First).QueueCount == 1 && Results(game) == 1, "disabled first remains intent without progress/output");
            Control(c, game, First, true); Control(c, twin, First, true);
            Reach(game, () => Results(game) == 2 && State(game, First).QueueCount == 0, "reenabled first actual supply", twin);
            c.Equal(game.Hash(), twin.Hash(), "reenabled provider reacquires actual remaining supply identically");
            c.That(Stock(game).Amount(Resource(game)) == Fix.FromInt(2) && Stock(game).Reservations.Count == 0 && Demands(game).Count == 0, "pending disable retains already acquired delivery; reenable reuses its actual remaining stock");
        }
    }
}
