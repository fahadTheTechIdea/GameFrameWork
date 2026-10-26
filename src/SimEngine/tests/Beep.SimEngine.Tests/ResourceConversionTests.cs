using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ResourceConversionTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReservationOwner Owner = new ReservationOwner("proof.conversion", 1);
        private static readonly ReservationOwner Other = new ReservationOwner("proof.conversion", 2);
        private static readonly Code Cause = new Code("proof.convert");
        private static readonly ResourceReservation[] Empty = Array.Empty<ResourceReservation>();
        internal const string DispositionCase = "Resource disposition conversion: explicit mixed receipt kinds preserve partial claims scoped journals and replay";
        public static void Register()
        {
            TestRunner.Add(DispositionCase, Dispositions);
            TestRunner.Add("Resource conversion: whole admission, scoped outputs and canonical saved facts", Contracts);
            TestRunner.Add("Resource conversion: timed transformer completion and provider fault boundaries", Execution);
            TestRunner.Add("Coupled conversion providers: original mixed endpoint receipts facts and terminal fault boundaries", CoupledProviders);
        }
        private static IResourceTransaction Transactions(SimGame game) => game.World.Services.Get<IResourceTransaction>();
        private static EndpointPools Pools(SimGame game) => game.World.Services.Get<EndpointPools>();
        private static ResourceId Id(SimGame game, string name) => game.World.Content.ResourceId(name);
        private static ResourceFlowPage History(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0);
        private static IInventory Source(SimGame game) => TransformerTests.Find(game, "source").Get<IInventory>();
        private static Bill BillOf(ResourceId resource, Fix amount) => new Bill(new[] { new ResourceAmount(resource, amount) });
        private static Bill Mixed(SimGame game, int steel, Fix water) => new Bill(new[] {
            new ResourceAmount(Id(game, "steel"), Fix.FromInt(steel)), new ResourceAmount(Id(game, "water"), water) });
        private static ResourceReservation[] Hold(SimGame game, IResourceOutlet endpoint, Bill bill, ReservationOwner? owner = null)
        {
            var result = Transactions(game).AcquireClaims(Company, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner ?? Owner, endpoint.Endpoint, bill) }, System.Array.Empty<ResourceReservation>());
            if (!result.Committed) throw new InvalidOperationException("fixture actual claim refused: " + result.Status);
            return new List<ResourceReservation>(result.Receipts).ToArray();
        }
        private static ResourceProductionIntent Output(IResourceInlet endpoint, Bill bill) => new ResourceProductionIntent(endpoint.Endpoint, bill);
        private static ResourceConversionIntent Convert(IReadOnlyList<ResourceReservation> consumed, IReadOnlyList<ResourceReservation> released,
            params ResourceProductionIntent[] outputs) => new ResourceConversionIntent(Owner, Company,
                consumed == null || released == null ? null : consumed.Select(record => new ResourceConversionInput(ResourceFlowKind.Consumed, record, record.Amount, false)).Concat(
                    released.Select(record => new ResourceConversionInput(ResourceFlowKind.Consumed, record, Fix.Zero, true))), outputs, Cause);
        private static void Tick(SimGame game, int count = 1)
        {
            for (int i = 0; i < count; i++) { game.Update(); if (game.Fault != null) throw new InvalidOperationException("conversion Update: " + game.Fault); }
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 91 });
        private static void Seed(SimGame game, IResourceInlet endpoint, Bill bill)
        {
            var result = Transactions(game).CommitMovement(new ResourceMovementIntent(new ReservationOwner("proof.seed", endpoint.Endpoint.Identity),
                Company, Source(game).Endpoint, endpoint.Endpoint, bill, Empty, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain));
            if (!result.Committed) throw new InvalidOperationException("actual seed movement refused: " + result.Status);
            Tick(game);
        }
        private static void Refuse(Check c, SimGame game, ResourceConversionIntent intent, ResourceTransferStatus expected, string reason)
        {
            ulong before = game.Hash(); long history = History(game).Latest;
            c.Equal(expected, Transactions(game).EvaluateConversion(intent, null).Status, reason + " pure admission");
            var result = Transactions(game).CommitConversion(intent, null);
            c.Equal(expected, result.Status, reason + " fresh commit refusal"); c.Equal(0, result.ChangedResources, reason + " no committed prefix");
            c.Equal(before, game.Hash(), reason + " stock/books/versions/sequence unchanged"); c.Equal(history, History(game).Latest, reason + " no history");
        }
        // Explicit receipt/transaction protocol proof; no player scheduler, stock mirror or host gameplay.
        private static void Dispositions(Check c)
        {
            foreach (bool half in new[] { false, true }) foreach (bool native in new[] { false, true }) foreach (bool coupled in new[] { false, true })
            {
                string context = "half=" + half + "/native=" + native + "/coupled=" + coupled;
                var json = Json.Parse(FrameworkProofContent.Read("endpoints.json"));
                if (half) json.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                var text = new System.Text.StringBuilder(); json.WriteCanonical(text); var factory = EndpointGame.Factory(content: text.ToString());
                var game = New(factory); var idle = New(factory); ulong untouched = idle.Hash(); var steel = Id(game, "steel"); var water = Id(game, "water");
                Seed(game, Pools(game).Get(1), Mixed(game, 2, Fix.One)); Seed(game, Pools(game).Get(2), Mixed(game, 4, Fix.FromInt(2)));
                IResourceOutlet Local(SimGame session) => native ? Source(session) : Pools(session).Get(2);
                IResourceInlet OutputStore(SimGame session) => coupled ? (IResourceInlet)Local(session) : native ? Pools(session).Get(2) : Source(session);
                Fix Total(SimGame session, ResourceId resource) => Source(session).Amount(resource) + Pools(session).Get(1).Amount(resource) + Pools(session).Get(2).Amount(resource);
                c.Equal(Fix.FromInt(30), Total(game, steel), "JSON startup and actual seed movements conserve original thirty steel " + context);
                c.Equal(Fix.FromInt(5), Total(game, water), "JSON startup and actual seed movements conserve original five water " + context);
                BigInteger Classified(SimGame session, ResourceId resource, ResourceFlowKind kind)
                    => session.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(Company).Aggregates
                        .Where(row => row.Resource == resource && row.Kind == kind && row.Cause == Cause)
                        .Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw);
                var localSteel = Hold(game, Local(game), BillOf(steel, Fix.FromInt(2))).Single();
                var localWater = Hold(game, Local(game), BillOf(water, Fix.One)).Single();
                var remoteSteel = Hold(game, Pools(game).Get(1), BillOf(steel, Fix.FromInt(2))).Single();
                var releasedWater = Hold(game, Pools(game).Get(1), BillOf(water, Fix.FromRatio(1, 4))).Single();
                var unrelated = Hold(game, Local(game), BillOf(steel, Fix.One), Other).Single();
                foreach (ResourceFlowKind invalid in new[] { ResourceFlowKind.StockTransfer, ResourceFlowKind.Produced, ResourceFlowKind.Sold, (ResourceFlowKind)255 })
                    Refuse(c, game, new ResourceConversionIntent(Owner, Company,
                        new[] { new ResourceConversionInput(invalid, localSteel, Fix.One, false) }, Array.Empty<ResourceProductionIntent>(), Cause),
                        ResourceTransferStatus.InvalidReceipt, "non-disposition kind refuses the whole receipt vector " + context + "/" + invalid);
                var first = new ResourceConversionIntent(Owner, Company, new[] {
                    new ResourceConversionInput(ResourceFlowKind.Destroyed, localSteel, Fix.One, false),
                    new ResourceConversionInput(ResourceFlowKind.Lost, localWater, Fix.FromRatio(1, 2), true),
                    new ResourceConversionInput(ResourceFlowKind.Consumed, remoteSteel, Fix.One, false),
                    new ResourceConversionInput(ResourceFlowKind.Lost, releasedWater, Fix.Zero, true) },
                    new[] { Output(OutputStore(game), BillOf(steel, Fix.One)) }, Cause);
                ulong pure = game.Hash(); c.That(Transactions(game).EvaluateConversion(first, null).Ready, "mixed kinds use complete original capacity/receipt admission " + context);
                c.Equal(pure, game.Hash(), "mixed-kind preflight is pure " + context);
                byte[] original = game.Save(); game = factory.Load(original); var limits = new ReplayLimits(4 * 1024 * 1024, 1000);
                using (var recording = game.RecordReplay(limits))
                {
                    c.That(TestKit.Order(game, 0, new ConversionProbeCommand { Intent = first }).Accepted, "same engine transaction commits each declared kind " + context);
                    if (game.Fault != null) throw new InvalidOperationException("mixed disposition commit: " + game.Fault);
                    c.Equal(1L, recording.CommandCount, "actual command codec carries all three accounting kinds " + context);
                    var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
                    c.That(replay.Success && replay.ActualHash == game.Hash(), "mixed-kind command replay reaches exact stock and journal " + context);
                }
                c.Equal(Fix.FromInt(29), Total(game, steel), "two actual steel draws and one authored product conserve native stock " + context);
                c.Equal(Fix.FromRatio(9, 2), Total(game, water), "only the actual half-water loss leaves stock " + context);
                c.Equal(new BigInteger(Fix.One.Raw), Classified(game, steel, ResourceFlowKind.Destroyed), "local steel is destroyed, not silently consumed " + context);
                c.Equal(new BigInteger(Fix.One.Raw), Classified(game, steel, ResourceFlowKind.Consumed), "remote steel retains independent consumed classification " + context);
                c.Equal(new BigInteger(Fix.FromRatio(1, 2).Raw), Classified(game, water, ResourceFlowKind.Lost), "water loss has exact native accounting " + context);
                c.Equal(BigInteger.Zero, Classified(game, water, ResourceFlowKind.Consumed), "releasing unused half-water is neither consumption nor extra loss " + context);
                c.That(!Pools(game).Get(1).CheckReservedOut(Owner, releasedWater.Token).Ready,
                    "release-only lost-kind row retires authority without losing actual water or emitting another loss " + context);
                var retained = History(game); var destroyed = retained.Entries.Single(row => row.Item.Kind == ResourceFlowKind.Destroyed).Item;
                var lost = retained.Entries.Single(row => row.Item.Kind == ResourceFlowKind.Lost).Item;
                c.That(destroyed.Source.Equals(localSteel.Token) && destroyed.Endpoint == Local(game).Endpoint && lost.Source.Equals(localWater.Token),
                    "each classified fact retains its actual original receipt and endpoint " + context);
                game = factory.Load(game.Save()); var twin = factory.Load(game.Save());
                c.Equal(game.Hash(), twin.Hash(), "scoped destroyed/lost journal metadata restores without inventory-entity substitution " + context);
                var residualLocal = Local(game).CheckReservedOut(Owner, localSteel.Token); var residualRemote = Pools(game).Get(1).CheckReservedOut(Owner, remoteSteel.Token);
                c.That(residualLocal.Ready && residualLocal.Record.Amount == Fix.One && residualRemote.Ready && residualRemote.Record.Amount == Fix.One,
                    "partial draws retain exact original reduced tokens " + context);
                c.That(!Local(game).CheckReservedOut(Owner, localWater.Token).Ready && Local(game).CheckReservedOut(Other, unrelated.Token).Ready,
                    "only selected unused water is released, foreign steel stays protected " + context);
                var finish = new ResourceConversionIntent(Owner, Company, new[] {
                    new ResourceConversionInput(ResourceFlowKind.Destroyed, residualLocal.Record, Fix.One, true),
                    new ResourceConversionInput(ResourceFlowKind.Consumed, residualRemote.Record, Fix.One, true) }, Array.Empty<ResourceProductionIntent>(), Cause);
                using (var recording = game.RecordReplay(limits))
                {
                    c.That(TestKit.Order(game, 0, new ConversionProbeCommand { Intent = finish }).Accepted, "saved partial receipt finishes through same disposition owner " + context);
                    if (game.Fault != null) throw new InvalidOperationException("reduced disposition commit: " + game.Fault);
                    c.That(TestKit.Order(twin, 0, new ConversionProbeCommand { Intent = finish }).Accepted, "saved twin accepts the same exact reduced records " + context);
                    c.Equal(game.Hash(), twin.Hash(), "classified reduced receipt continuation stays deterministic " + context);
                    var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
                    c.That(replay.Success && replay.ActualHash == game.Hash(), "actual saved reduced-kind completion replays " + context);
                }
                c.Equal(Fix.FromInt(27), Total(game, steel), "final material equals original30 minus destroyed2 minus consumed2 plus produced1 " + context);
                c.Equal(new BigInteger(Fix.FromInt(2).Raw), Classified(game, steel, ResourceFlowKind.Destroyed), "saved destruction is accumulated exactly once " + context);
                c.Equal(new BigInteger(Fix.FromInt(2).Raw), Classified(game, steel, ResourceFlowKind.Consumed), "saved remote consumption is accumulated exactly once " + context);
                c.That(Local(game).CheckReservedOut(Other, unrelated.Token).Ready && !Local(game).CheckReservedOut(Owner, localSteel.Token).Ready,
                    "completion retires its own token only " + context);
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "complete classified receipt facts restore canonically " + context);
                c.That(retained.Entries.Single(row => row.Item.Kind == ResourceFlowKind.Destroyed).Item.Amount == Fix.One,
                    "retained first classified page stays detached " + context);
                c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().ReadSummary(Company).Status, "classified resource history is company-private " + context);
                c.Equal(untouched, idle.Hash(), "kind/receipt/codec changes affect no independent session " + context);
            }
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "conversion semantics identify current source");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "only current scoped production reader");
            IInventory source = Source(game); var first = Pools(game).Get(1); var wide = Pools(game).Get(5000000000);
            Seed(game, first, BillOf(Id(game, "steel"), Fix.FromInt(2)));
            var consumed = Hold(game, source, Mixed(game, 3, Fix.FromRatio(1, 2)));
            var tools = Hold(game, first, BillOf(Id(game, "steel"), Fix.FromInt(2)));
            var other = Hold(game, source, BillOf(Id(game, "steel"), Fix.One), Other);
            ReservationToken incoming = wide.ReserveIn(Other, Id(game, "water"), Fix.One);
            var outputs = new[] { Output(first, Mixed(game, 1, Fix.FromRatio(1, 2))), Output(wide, BillOf(Id(game, "steel"), Fix.FromInt(2))) };
            ResourceConversionIntent intent = Convert(consumed, tools, outputs);
            ulong before = game.Hash();
            c.That(Transactions(game).EvaluateConversion(intent, null).Ready, "complete mixed-unit multi-endpoint conversion admits");
            c.Equal(before, game.Hash(), "admission cannot create output receipts or spend tool/input prefixes");
            Refuse(c, game, default, ResourceTransferStatus.InvalidRequest, "missing explicit intent");
            Refuse(c, game, Convert(Empty, Empty), ResourceTransferStatus.InvalidRequest, "all-empty conversion");
            Refuse(c, game, Convert(consumed, new[] { consumed[0] }, outputs), ResourceTransferStatus.InvalidReceipt, "same token consumed and released");
            Refuse(c, game, Convert(other, tools, outputs), ResourceTransferStatus.InvalidReceipt, "another execution's input");
            Refuse(c, game, Convert(consumed, tools, outputs[0], outputs[0]), ResourceTransferStatus.InvalidRequest, "duplicate output owner endpoint");
            Refuse(c, game, Convert(consumed, tools, Output(Pools(game).Get(3), BillOf(Id(game, "steel"), Fix.One))), ResourceTransferStatus.InvalidRequest, "private foreign output");
            Refuse(c, game, Convert(consumed, tools, new ResourceProductionIntent(new ResourceEndpointId("missing.owner", 1), outputs[0].Quantities)), ResourceTransferStatus.MissingEndpoint, "removed output owner");
            Refuse(c, game, Convert(consumed, tools, Output(wide, BillOf(Id(game, "steel"), Fix.FromRatio(1, 2)))), ResourceTransferStatus.InvalidRequest, "fractional discrete production");
            Refuse(c, game, Convert(consumed, tools, Output(wide, BillOf(new ResourceId(999999), Fix.One))), ResourceTransferStatus.InvalidRequest, "unknown output resource");
            Refuse(c, game, new ResourceConversionIntent(Owner, Company, intent.Inputs, outputs, Code.None), ResourceTransferStatus.InvalidRequest, "missing authored cause");
            Refuse(c, game, Convert(consumed, null, outputs), ResourceTransferStatus.InvalidRequest, "missing explicit release list");
            Refuse(c, game, Convert(consumed, tools, outputs[0], Output(wide, Mixed(game, 8, Fix.FromInt(2)))), ResourceTransferStatus.NoCapacity, "aggregate output plus actual unrelated promise");
            ReservationToken pressure = wide.ReserveIn(new ReservationOwner("proof.pressure", 1), Id(game, "steel"), Fix.FromInt(8));
            Refuse(c, game, intent, ResourceTransferStatus.NoCapacity, "retained ready preview is not authority");
            wide.ReleaseIn(new ReservationOwner("proof.pressure", 1), pressure);
            ResourceFlowPage retained = History(game);
            var shuffledInputs = new List<ResourceReservation>(consumed); shuffledInputs.Reverse();
            var shuffledOutputs = new List<ResourceProductionIntent>(outputs); shuffledOutputs.Reverse();
            SimGame twin = factory.Load(game.Save());
            game.Seat(0).Submit(new ConversionProbeCommand { Intent = intent });
            SimGame pending = factory.Load(game.Save());
            twin.Seat(0).Submit(new ConversionProbeCommand { Intent = Convert(shuffledInputs, tools, shuffledOutputs.ToArray()) });
            Tick(game); Tick(twin); Tick(pending);
            c.Equal(game.Hash(), twin.Hash(), "input/output order cannot choose mutation versions, inlet tokens or history");
            c.Equal(game.Hash(), pending.Hash(), "pending conversion command saves exact detached receipt/output request");
            c.Equal(Fix.FromInt(25), source.Amount(Id(game, "steel")), "actual steel seed and input consumption only");
            c.Equal(Fix.FromRatio(9, 2), source.Amount(Id(game, "water")), "native fractional input consumption exact");
            c.Equal(Fix.FromInt(3), first.Amount(Id(game, "steel")), "tool stock stays local and output adds only authored amount");
            c.Equal(Fix.FromRatio(1, 2), first.Amount(Id(game, "water")), "second native output line committed with same whole conversion");
            c.Equal(Fix.FromInt(2), wide.Amount(Id(game, "steel")), "scoped wide output has real stock without dummy entity");
            c.That(source.Reservations.Count == 1 && source.Reservations[0].Token.Equals(other[0].Token), "unrelated source hold survives");
            c.That(first.Reservations.Count == 0 && wide.Reservations.Count == 1 && wide.Reservations[0].Token.Equals(incoming), "all temporary output/tool claims retire, unrelated incoming promise survives");
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), History(game).Totals(Id(game, "steel")).ConsumedRaw, "only raw inputs count as consumed");
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), History(game).Totals(Id(game, "steel")).CreatedRaw, "all real product lines count once, tools never refunded");
            c.Equal(BigInteger.Zero, retained.Totals(Id(game, "steel")).ConsumedRaw, "retained private immutable history does not acquire later facts");
            c.That(game.Seat(1).View.Feature<ResourceFlowView>().Read(Company, 0).Status == ResourceFlowReadStatus.NotOwned, "foreign company cannot inspect conversion inputs/results");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "scoped production facts read through sole current journal codec");
            Refuse(c, game, intent, ResourceTransferStatus.InvalidReceipt, "spent input/tool references");
            var releasedOnly = new ResourceConversionIntent(Other, Company,
                other.Select(record => new ResourceConversionInput(ResourceFlowKind.Consumed, record, Fix.Zero, true)), Array.Empty<ResourceProductionIntent>(), Cause);
            long latest = History(game).Latest;
            c.That(Transactions(game).CommitConversion(releasedOnly, null).Committed, "explicit release-only resource operation retires its actual hold");
            c.Equal(latest, History(game).Latest, "release-only never creates/consumes material history");
            SnapshotCopy(c, game);
            PartialInputs(c);
        }
        private static void PartialInputs(Check c)
        {
            GameFactory factory = EndpointGame.Factory(); SimGame game = New(factory);
            IInventory source = Source(game); ResourceId water = Id(game, "water");
            var held = Hold(game, source, BillOf(water, Fix.FromRatio(7, 2)));
            var unrelated = Hold(game, source, BillOf(water, Fix.FromRatio(1, 2)), Other);
            var first = new ResourceConversionIntent(Owner, Company,
                new[] { new ResourceConversionInput(ResourceFlowKind.Consumed, held[0], Fix.FromRatio(5, 4), false) },
                new[] { Output(Pools(game).Get(1), BillOf(water, Fix.FromRatio(5, 4))) }, Cause);
            ulong hash = game.Hash();
            c.That(Transactions(game).EvaluateConversion(first, null).Ready && game.Hash() == hash,
                "partial input admission is pure and uses the actual complete held record");
            c.That(Transactions(game).CommitConversion(first, null).Committed, "partial conversion commits through the original native owner");
            Tick(game);
            ResourceReceiptCheck residual = source.CheckReservedOut(Owner, held[0].Token);
            c.That(residual.Ready && residual.Record.Token.Equals(held[0].Token) && residual.Record.Owner.Equals(Owner) &&
                residual.Record.Amount == Fix.FromRatio(9, 4), "partial consumption retains the same original token with exact reduced authority");
            c.Equal(Fix.FromRatio(15, 4), source.Amount(water), "only the declared1.25 input leaves the original five water");
            c.Equal(Fix.FromRatio(5, 4), Pools(game).Get(1).Amount(water), "scoped product receives only the declared1.25 output");
            c.That(source.CheckReservedOut(Other, unrelated[0].Token).Ready, "partial conversion retains another owner's native receipt");
            Refuse(c, game, first, ResourceTransferStatus.InvalidReceipt, "stale original3.5 receipt after actual partial consumption");
            SimGame twin = factory.Load(game.Save());
            ResourceReceiptCheck saved = Source(twin).CheckReservedOut(Owner, held[0].Token);
            c.That(saved.Ready && saved.Record.Equals(residual.Record), "save/load preserves the original reduced record, not a substitute promise");
            c.Equal(game.Hash(), twin.Hash(), "partial native stock, receipt and fact sequence share one checkpoint");
            var finish = new ResourceConversionIntent(Owner, Company,
                new[] { new ResourceConversionInput(ResourceFlowKind.Consumed, residual.Record, Fix.One, true) },
                new[] { Output(source, BillOf(water, Fix.FromRatio(1, 2))) }, Cause);
            game.Seat(0).Submit(new ConversionProbeCommand { Intent = finish });
            twin.Seat(0).Submit(new ConversionProbeCommand { Intent = finish });
            Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "saved partial receipt continues through one same-endpoint consume/produce/release commit");
            c.Equal(Fix.FromRatio(13, 4), source.Amount(water), "later native input1 and output0.5 change only actual stock");
            c.That(!source.CheckReservedOut(Owner, held[0].Token).Ready, "release-remainder retires that execution's original residual token");
            c.That(source.Reservations.Count == 1 && source.CheckReservedOut(Other, unrelated[0].Token).Ready,
                "completion releases only its own remaining1.25, not the other owner's0.5");
            ResourceFlowPage facts = History(game);
            c.Equal(new BigInteger(Fix.FromRatio(9, 4).Raw), facts.Totals(water).ConsumedRaw, "two partial input facts total exactly2.25 native water");
            c.Equal(new BigInteger(Fix.FromRatio(7, 4).Raw), facts.Totals(water).CreatedRaw, "two declared product facts total exactly1.75; released authority creates no stock");
            c.Equal(BigInteger.Zero, facts.Totals(water).LostRaw + facts.Totals(water).DestroyedRaw,
                "partial receipt retirement is neither fictitious loss nor destruction");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed partial conversion and unrelated live receipt restore together");
        }
        private static void SnapshotCopy(Check c, SimGame game)
        {
            var records = Hold(game, Source(game), BillOf(Id(game, "steel"), Fix.One));
            var outputs = new[] { Output(Pools(game).Get(2), BillOf(Id(game, "steel"), Fix.One)) };
            var intent = Convert(records, Empty, outputs); records[0] = default; outputs[0] = default;
            c.That(Transactions(game).EvaluateConversion(intent, null).Ready, "retained request lists are detached from caller mutation");
            c.That(Transactions(game).CommitConversion(intent, null).Committed, "actual immutable request still commits its complete authored conversion");
            Tick(game);
            var madeOnly = Convert(Empty, Empty, Output(Pools(game).Get(2), BillOf(Id(game, "steel"), Fix.One)));
            c.That(Transactions(game).CommitConversion(madeOnly, null).Committed, "explicit authored creation-only resource conversion needs no fabricated input");
            Tick(game);
        }
        private static GameFactory NativeFactory(Action<JsonValue> edit = null) => TransformerWipTests.Factory(json =>
        {
            JsonValue row = json.Get("transformations")[0]; row.Get("tools").Set("supply", JsonValue.Number("2"));
            row.Get("tool_ports").Set("supply", JsonValue.Of("tool.input"));
            row.Remove("entity_output"); row.Remove("output_home"); edit?.Invoke(json);
        });
        private static void Execution(Check c)
        {
            Native(c); Faults(c);
        }
        private static void Native(Check c)
        {
            GameFactory factory = NativeFactory(); SimGame game = TransformerTests.New(factory);
            Entity machine = TransformerTests.Find(game, "assembler"); IInventory source = machine.Get<IInventory>();
            ResourceId supply = Id(game, "supply"); var provider = machine.Get<ITransformerTools>(new Code("transformation"));
            c.That(TransformerTests.Queue(game, "assembler", "build", repeat: true).Accepted, "player enqueues actual timed repeat");
            c.That(TransformerTests.State(game).Wip.Stock.AmountOf(supply) == Fix.FromInt(3) && TransformerTests.State(game).Tools.Held.AmountOf(supply) == Fix.FromInt(2), "real startup partitions raw WIP and reusable local tools");
            var pressureOwner = new ReservationOwner("proof.output-pressure", 1);
            ReservationToken pressure = source.ReserveIn(pressureOwner, supply, Fix.FromInt(23));
            Tick(game, 5);
            var blocked = TransformerTests.State(game);
            c.That(blocked.Phase == TransformationPhase.Blocked && blocked.Blocker == TransformerManager.BlockerOutput, "full current output blocks complete conversion");
            c.That(blocked.Progress.Complete(blocked.Required) && provider.WorkInProgress.Amount(supply) == Fix.FromInt(3), "completed time cannot prematurely consume actual WIP");
            c.That(source.ReservedOut(supply) == Fix.FromInt(2), "blocked output cannot release reusable tool holds");
            c.Equal(BigInteger.Zero, History(game).Totals(supply).ConsumedRaw + History(game).Totals(supply).CreatedRaw, "blocked conversion publishes no result prefix");
            SimGame twin = factory.Load(game.Save()); ulong heldHash = game.Hash();
            source.ReleaseIn(pressureOwner, pressure);
            TransformerTests.Find(twin, "assembler").Get<IInventory>().ReleaseIn(pressureOwner, pressure);
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "saved completed-WIP conversion continues identically after real capacity release");
            c.That(game.Hash() != heldHash && TransformerTests.State(game).Tools.Held.IsEmpty && source.Amount(supply) == Fix.FromInt(18), "complete conversion consumes3 produces1 and releases2 tools exactly once");
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), History(game).Totals(supply).ConsumedRaw, "one batch input fact");
            c.Equal(new BigInteger(Fix.One.Raw), History(game).Totals(supply).CreatedRaw, "one batch result fact");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "repeat reacquires through same native owner");
            c.That(TransformerTests.State(game).Wip.Execution == 2 && source.ReservedOut(supply) == Fix.FromInt(2), "repeat has new actual execution/tool receipts, not completed history reuse");
            c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = machine.Id, Capability = new Code("transformation"), Index = 0 }).Accepted, "cancel repeated physical work through ordinary engine command");
            Tick(game);
            c.That(TransformerTests.State(game).QueueCount == 0 && source.Amount(supply) == Fix.FromInt(18) && source.Reservations.Count == 0, "cancel returns raw inputs and releases tools without another output");
            c.Equal(new BigInteger(Fix.One.Raw), History(game).Totals(supply).CreatedRaw, "cancel never creates a conversion refund");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed conversion/cancel facts and actual stock share current codec");
            foreach (string fault in new[] { "partial_receive", "ignore_release" })
            {
                SimGame broken = TransformerTests.New(NativeFactory(json => json.Get("entities")[0].Get("components").Get("process_stock").Set("receipt_fault", JsonValue.Of(fault))));
                c.That(TransformerTests.Queue(broken, "assembler", "build").Accepted, "hostile completion provider has valid physical startup: " + fault);
                ResourceFlowPage history = History(broken); broken.RunTicks(5);
                Faulted(c, broken, history, "native completion " + fault);
            }
        }
        private static void Faulted(Check c, SimGame game, ResourceFlowPage healthy, string reason)
        {
            c.Equal(GameState.Faulted, game.State, reason + " faults actual Update");
            c.Throws<InvalidOperationException>(() => History(game), reason + " cannot expose healthy live history after terminal failure");
            c.That(!healthy.Entries.Any(entry => entry.Item.Kind == ResourceFlowKind.Consumed || entry.Item.Kind == ResourceFlowKind.Produced),
                reason + " detached last-healthy facts acquire no later conversion prefix");
            c.That(!game.World.Events.HasPending, reason + " no queued resource success prefix either");
            c.Throws<InvalidOperationException>(() => game.Save(), reason + " cannot checkpoint partial mutation");
            c.Throws<InvalidOperationException>(() => game.Hash(), reason + " cannot hash resumable partial mutation");
        }
        private static void CoupledProviders(Check c)
        {
            foreach (bool half in new[] { false, true })
            {
                var definitions = Json.Parse(FrameworkProofContent.Read("endpoints.json"));
                if (half) definitions.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                var factory = EndpointGame.Factory(content: definitions.ToString()); var template = New(factory);
                var local = Pools(template).Get(2); var peer = Pools(template).Get(1);
                Seed(template, local, Mixed(template, 4, Fix.FromInt(2))); Seed(template, peer, BillOf(Id(template, "water"), Fix.One));
                var input = Hold(template, local, BillOf(Id(template, "steel"), Fix.FromInt(2))).Single();
                var tool = Hold(template, local, BillOf(Id(template, "water"), Fix.FromRatio(1, 2))).Single();
                var remote = Hold(template, peer, BillOf(Id(template, "water"), Fix.FromRatio(1, 2))).Single();
                var unrelated = Hold(template, local, BillOf(Id(template, "steel"), Fix.One), Other).Single();
                ReservationToken incoming = local.ReserveIn(Other, Id(template, "water"), Fix.FromRatio(1, 2));
                var intent = new ResourceConversionIntent(Owner, Company, new[] {
                    new ResourceConversionInput(ResourceFlowKind.Consumed, input, Fix.One, false), new ResourceConversionInput(ResourceFlowKind.Consumed, tool, Fix.Zero, true),
                    new ResourceConversionInput(ResourceFlowKind.Consumed, remote, Fix.FromRatio(1, 4), false) },
                    new[] { Output(local, Mixed(template, 1, Fix.FromRatio(1, 2))) }, Cause);
                byte[] saved = template.Save();
                foreach (string mode in new[] { "healthy", "refuse", "unknown-status", "check-clock", "check-stock", "check-peer", "check-throw",
                    "noop", "consume-only", "erase-remainder", "erase-other", "peer", "clock", "random-restore", "silent-version", "throw" })
                {
                    string context = mode + "/half=" + half; var game = factory.Load(saved); var pools = Pools(game);
                    local = pools.Get(2); peer = pools.Get(1); var steel = Id(game, "steel"); var water = Id(game, "water");
                    pools.ProbeConversion = true; pools.FaultEndpoint = 2;
                    c.That(pools.Resolve(local.Endpoint) is IResourceConversionEndpoint, "external probe actually exposes coupled capability: " + context);
                    ulong before = game.Hash();
                    c.That(Transactions(game).EvaluateConversion(intent, null).Ready, "original mixed-endpoint conversion ready before fault: " + context);
                    c.Equal(before, game.Hash(), "external coupled preview pure: " + context);
                    var independent = factory.Load(saved); ulong isolated = independent.Hash(); var history = History(game);
                    var original = new InvalidOperationException("proof.coupled-original-error"); int checks = 0, commits = 0;
                    pools.ConversionCheck = (stock, consumed, produced) => {
                        checks++;
                        c.Equal(Fix.One, new Bill(consumed).AmountOf(steel), "provider sees actual coupled draw only: " + context);
                        c.Equal(2, produced.Count, "provider sees complete native output vector: " + context);
                        if (mode == "unknown-status") return new CapacityCheck((CapacityStatus)255);
                        if (mode == "check-clock") game.Clock.Pause();
                        if (mode == "check-stock") stock.Take(water, Fix.One);
                        if (mode == "check-peer") peer.Take(water, Fix.FromRatio(1, 4));
                        if (mode == "check-throw") throw original;
                        return mode == "refuse" || mode.StartsWith("check-") ? new CapacityCheck(CapacityStatus.LimitExceeded) : stock.CheckConversion(consumed, produced);
                    };
                    pools.ConversionCommit = (stock, owner, inputs, produced) => {
                        commits++;
                        c.Equal(2, inputs.Count, "one callback owns actual local input and released tool: " + context);
                        if (mode == "noop") return;
                        if (mode == "consume-only") {
                            foreach (var item in inputs) if (item.Consumed.IsPositive) stock.TakeReserved(owner, item.Receipt.Token, item.Consumed);
                            return;
                        }
                        stock.ConvertReserved(owner, inputs, produced);
                        if (mode == "erase-remainder") stock.ReleaseOut(owner, input.Token);
                        if (mode == "erase-other") stock.ReleaseOut(Other, unrelated.Token);
                        if (mode == "peer") peer.Take(water, Fix.FromRatio(1, 4));
                        if (mode == "clock") game.Clock.Pause();
                        if (mode == "random-restore") { ulong state = game.World.Random.State; game.World.Random.NextULong(); game.World.Random.State = state; }
                        if (mode == "throw") throw original;
                    };
                    if (mode == "silent-version") pools.ClaimFault = ClaimFaultMode.SilentVersion;
                    Exception caught = null; ResourceInventoryResult answer = default;
                    try { answer = mode.StartsWith("check-") || mode == "unknown-status" ? Transactions(game).EvaluateConversion(intent, null) : Transactions(game).CommitConversion(intent, null); }
                    catch (Exception error) { caught = error; }
                    c.That(checks > 0, "actual coupled capacity callback exercised: " + context);
                    if (mode == "healthy") {
                        c.That(caught == null && answer.Committed, "external coupled provider commits through original transaction");
                        c.Equal(1, commits, "one original coupled mutation callback");
                        c.Equal(Fix.FromInt(4), local.Amount(steel), "same-resource consume/produce native balance exact");
                        c.Equal(Fix.FromRatio(5, 2), local.Amount(water), "new local output exact");
                        c.Equal(Fix.FromRatio(3, 4), peer.Amount(water), "second endpoint consumes only actual delta");
                        c.Equal(Fix.One, local.CheckReservedOut(Owner, input.Token).Record.Amount, "original partial token retained");
                        c.Equal(Fix.FromRatio(1, 4), peer.CheckReservedOut(Owner, remote.Token).Record.Amount, "remote partial token retained");
                        c.That(!local.CheckReservedOut(Owner, tool.Token).Ready, "released tool token retires without material loss");
                        c.That(local.CheckReservedOut(Other, unrelated.Token).Ready && local.CheckReservedIn(game.World.Content, Other, incoming).Ready,
                            "unrelated inbound/outbound authority preserved");
                        Tick(game); var facts = History(game).Entries.Where(x => x.Sequence > history.Latest).Select(x => x.Item).ToArray();
                        c.Equal(4, facts.Length, "only two actual consumption and two production facts");
                        c.Equal(2, facts.Count(x => x.Kind == ResourceFlowKind.Consumed), "each actual input has consumed fact");
                        c.Equal(2, facts.Count(x => x.Kind == ResourceFlowKind.Produced), "each actual output has produced fact");
                        c.That(facts.All(x => x.Cause == Cause && x.Company == Company), "native facts carry original attribution");
                        var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual mixed stock and token identities durable");
                        Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "saved coupled commit continues identically");
                    }
                    else if (mode == "refuse") {
                        c.That(caught == null && answer.Status == ResourceTransferStatus.NoCapacity, "valid capacity refusal remains typed and healthy");
                        c.Equal(0, commits, "capacity refusal calls no coupled mutation"); c.Equal(before, game.Hash(), "capacity refusal preserves full original state");
                        c.Equal(GameState.Running, game.State, "capacity refusal is not a fault");
                    }
                    else {
                        c.That(caught is InvalidOperationException, "provider violation cannot return healthy: " + context);
                        string phase = mode == "check-clock" || mode == "check-throw" || mode == "unknown-status" ? "observation.provider" : "resource.transaction";
                        c.Equal(phase, game.World.Fault?.Phase, "actual first-fault owner retained: " + context);
                        if (mode == "check-throw" || mode == "throw") c.That(ReferenceEquals(original, caught), "exact original callback exception survives: " + context);
                        c.Equal(mode.StartsWith("check-") || mode == "unknown-status" ? 0 : 1, commits, "fault phase cuts off later mutation: " + context);
                        var fault = game.World.Fault; long time = game.Clock.StepIndex; game.Update();
                        c.Equal(time, game.Clock.StepIndex, "faulted conversion advances no time: " + context);
                        Faulted(c, game, history, context); c.That(ReferenceEquals(fault, game.World.Fault), "first fault not replaced by continuation: " + context);
                    }
                    c.Equal(isolated, independent.Hash(), "other saved endpoint session unchanged: " + context);
                    long independentTime = independent.Clock.StepIndex; Tick(independent);
                    c.That(independent.Clock.StepIndex > independentTime, "other session remains playable: " + context);
                }
            }
        }
        private static void Faults(Check c)
        {
            // A probe that exposes only IResourceOutlet is not a same-endpoint converter.
            // Capability loss must remain a healthy typed refusal, not a manufactured execution fault.
            var incompatible = New(EndpointGame.Factory()); var incompatiblePools = Pools(incompatible);
            var original = incompatiblePools.Get(2);
            Seed(incompatible, original, BillOf(Id(incompatible, "steel"), Fix.FromInt(4)));
            var originalInput = Hold(incompatible, original, BillOf(Id(incompatible, "steel"), Fix.FromInt(2)));
            var coupled = Convert(originalInput, Empty, Output(original, BillOf(Id(incompatible, "steel"), Fix.One)));
            c.That(Transactions(incompatible).EvaluateConversion(coupled, null).Ready, "original endpoint declares actual same-endpoint conversion");
            incompatiblePools.ClaimFault = ClaimFaultMode.ShortTake;
            Refuse(c, incompatible, coupled, ResourceTransferStatus.Incompatible, "replacement endpoint without conversion capability");
            c.Equal(GameState.Running, incompatible.State, "missing conversion capability is not a provider execution failure");
            foreach (ClaimFaultMode fault in new[] { ClaimFaultMode.ShortTake, ClaimFaultMode.IgnoreRelease, ClaimFaultMode.ShortReceive,
                ClaimFaultMode.OtherStock, ClaimFaultMode.OtherReceipt, ClaimFaultMode.OtherEndpoint,
                ClaimFaultMode.MutatingOtherEndpointCheck, ClaimFaultMode.SilentVersion, ClaimFaultMode.RefusingIn })
            {
                SimGame game = New(EndpointGame.Factory()); var pools = Pools(game); var input = pools.Get(2); var output = pools.Get(1);
                Seed(game, input, Mixed(game, 4, Fix.FromInt(2))); Seed(game, output, BillOf(Id(game, "water"), Fix.One));
                var consumed = Hold(game, input, BillOf(Id(game, "steel"), Fix.FromInt(2)));
                var tools = Hold(game, input, BillOf(Id(game, "water"), Fix.FromRatio(1, 2)));
                Hold(game, input, BillOf(Id(game, "steel"), Fix.One), Other);
                // Exercise the actual primitive callbacks supplied by this probe. Coupled execution
                // is covered above and by PartialInputs; this facade deliberately has no converter.
                var intent = Convert(consumed, tools, Output(output, BillOf(Id(game, "water"), Fix.FromRatio(1, 2))));
                c.That(Transactions(game).EvaluateConversion(intent, null).Ready, "actual complete conversion ready before injecting provider fault: " + fault);
                ResourceFlowPage latest = History(game); pools.ClaimFault = fault;
                pools.FaultEndpoint = fault == ClaimFaultMode.ShortReceive || fault == ClaimFaultMode.RefusingIn ? 1 : 2;
                pools.FaultPeerEndpoint = pools.FaultEndpoint == 1 ? 2 : 1;
                game.Seat(0).Submit(new ConversionProbeCommand { Intent = intent }); game.Update();
                Faulted(c, game, latest, "conversion provider " + fault);
            }
        }
    }
}
