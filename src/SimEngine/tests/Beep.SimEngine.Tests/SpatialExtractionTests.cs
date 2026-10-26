using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static class SpatialExtractionTests
    {
        internal static void Register()
        {
            TestRunner.Add("Spatial extraction: authored ownership, receipts and actual transfer provenance", Admission);
            TestRunner.Add("Spatial extraction: timed work, blockers and saved physical continuation", Physical);
            TestRunner.Add("Spatial extraction: pure single-use preparations retain receipt and revision guards", Preparation);
            TestRunner.Add("Spatial resources: bounded incoming receipts preserve actual layers and saved commitments", Deposits);
        }
        private const string Declaration = "{\"id\":\"sim.spatial-resources\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"extraction.json\"]}";
        private static string Text(JsonValue root) { var text = new StringBuilder(); root.WriteCanonical(text); return text.ToString(); }
        private static string Retry()
        {
            var retry = Content.Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Get("work_retry_profiles")[0].Get("retry_blockers").Add(JsonValue.Of(WorkEffectCodes.NoStock.Id));
            retry.Get("work_retry_profiles")[0].Get("retry_blockers").Add(JsonValue.Of(WorkEffectCodes.IncompatibleInventory.Id));
            return Text(retry);
        }
        private static GameFactory Tiles(Action<JsonValue> edit = null)
        {
            var root = Content.Json.Parse(FrameworkProofContent.Read("spatial-extraction.json")); edit?.Invoke(root);
            return Colony.Factory(retryProfiles: Retry(), additionalModules: new GameModule[] { new SpatialResourcesModule() },
                moduleDeclarations: new[] { Declaration }, moduleDocuments: new Dictionary<string, string> { ["extraction.json"] = Text(root) });
        }
        internal static GameFactory Points(Action<JsonValue> edit = null)
        {
            var root = Content.Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var extraction = Content.Json.Parse(FrameworkProofContent.Read("point-spatial-extraction.json")); edit?.Invoke(extraction);
            return PointGame.Factory(Text(root), FrameworkProofContent.Read("point-layers.json"), new PointSpatialResourcesModule(), Declaration,
                Text(extraction), retryProfiles: Retry());
        }
        internal static SimGame Create(GameFactory factory, bool point = false)
        {
            var request = new NewGameRequest { Scenario = point ? "points" : "colony", Seed = 373 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            request.Mods.Add(SpatialResourcesModule.ModuleId); request.Mods.Add(ResourceFlowsModule.ModuleId);
            return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Find(SimGame game, string definition)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) return entity; throw new InvalidOperationException(definition); }
        private static ISpatialResourceCellDefinition Definition(SimGame game, bool point = false) => game.World.Content.Get<ISpatialResourceCellDefinition>(point ? "point.extract.cell" : "extract.cell");
        private static IResourceOutlet Source(SimGame game, bool point = false) => game.World.Services.Get<ISpatialResourceEndpoints>().ResolveCell(Definition(game, point));
        private static Entity Setup(Check c, SimGame game, bool point = false, bool worker = true)
        {
            if (point)
            {
                var space = (PointSpace)game.World.Space; space.AssignOwner(game.World, "point.deposit", new PlayerId(0));
                c.That(space.WriteLayer(game.World, new PlayerId(0), "point.deposit", Fix.FromInt(10), Fix.FromInt(8)).IsNone, "arrange explicit actual point material");
                Tick(game); return Find(game, "destination");
            }
            var actors = new List<Entity>(); foreach (Entity entity in game.World.Entities.All) if (entity.Has<IWorker>()) actors.Add(entity);
            foreach (Entity entity in actors) c.That(TestKit.Order(game, entity.Owner.Value, new MoveVehicleCommand { Vehicle = entity.Id, To = entity.Get<ITilePresence>().Tile }).Accepted, "player holds unrelated initial machines");
            var map = (TileMap)game.World.Space; map.AssignOwner(game.World, "extract.deposit", new PlayerId(0));
            map.Layer<ByteLayer>("extract.deposit").Set(new TileCoord(20, 20), 12);
            Entity target = Spawn(game, "extract.site", 20, 20); if (worker) Spawn(game, "extract.worker", 12, 20);
            Tick(game); return target;
        }
        private static Order Post(Check c, SimGame game, Entity target, bool point = false, string plan = null)
        {
            var command = new PostOrderCommand { Plan = plan ?? (point ? "point.extract.activity" : "extract.activity"), Target = target.Id };
            c.Equal(Code.None, game.Seat(0).Preview(command), "engine admits ordinary extraction order");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "player posts existing activity command: " + result.Refusal);
            return Board(game).Order(checked((int)result.Arg0));
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 480 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            {
                var diagnostics = new List<string>();
                foreach (Order order in Board(game).Orders) diagnostics.Add(order.Id + "/" + order.State + "/" + order.Blocker + "/" + order.Node("operate").State + "/" + order.Node("operate").Blocker);
                throw new InvalidOperationException(stage + " at tick=" + game.Tick + " " + string.Join(";", diagnostics));
            }
        }
        private static ResourceMovementIntent Movement(SimGame game, Entity destination, ReservationOwner owner, Fix amount, IReadOnlyList<ResourceReservation> held = null, bool point = false)
            => new ResourceMovementIntent(owner, new PlayerId(0), Source(game, point).Endpoint, ResourceEndpointId.ForEntity(destination.Id),
                BillOf(Definition(game, point).Layer.Resource.Resource, amount), held ?? Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
        private static Bill BillOf(ResourceId resource, Fix amount) => new Bill(new[] { new ResourceAmount(resource, amount) });
        private static List<ResourceFlowFact> Facts(SimGame game)
        {
            var result = new List<ResourceFlowFact>();
            foreach (var entry in game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Entries)
                if (entry.Item.Source.Endpoint.Scope == SpatialResourcesModule.ModuleId) result.Add(entry.Item);
            return result;
        }
        private static void VerifyTransfer(Check c, SimGame game, ResourceEndpointId endpoint, Fix amount)
        {
            var facts = Facts(game); c.That(facts.Count > 0, "actual cell transfer has existing journal provenance");
            ResourceFlowFact fact = facts[facts.Count - 1];
            c.Equal(ResourceFlowKind.StockTransfer, fact.Kind, "deposit extraction is real stock movement, not Produced/Initialized");
            c.Equal(endpoint, fact.Source.Endpoint, "historical source receipt identifies actual scoped cell");
            c.Equal(amount, fact.Amount, "historical exact native amount matches material moved");
            c.Equal(amount, fact.SourceBefore.Value - fact.SourceAfter.Value, "actual deposit debit is conserved");
            c.Equal(amount, fact.DestinationAfter - fact.DestinationBefore, "actual inventory credit is conserved");
            c.That(fact.SourceVersionAfter > fact.SourceVersionBefore, "actual cell endpoint mutation advances persistent source version");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Status, "foreign seat cannot inspect extraction history");
        }
        private static void Deposits(Check c)
        {
            foreach (bool point in new[] { false, true })
            {
                var factory = point ? Points() : Tiles(); var game = Create(factory, point); Setup(c, game, point, false);
                var cell = Source(game, point); var resource = Definition(game, point).Layer.Resource.Resource;
                var owner = new ReservationOwner("proof.incoming", 1); var foreign = new ReservationOwner("proof.incoming", 2);
                Fix before = cell.Amount(resource); ulong hash = game.Hash();
                c.That(cell.CheckIncoming(new[] { new ResourceAmount(resource, Fix.FromInt(10)) }).Fits, "actual finite cell previews incoming room");
                c.Equal(Fix.FromInt(40) - before, cell.Offer(resource, Fix.FromInt(100)), "authored capacity, not an unlimited inlet");
                c.Equal(hash, game.Hash(), "incoming observations preserve actual layer and book");
                var inbound = cell.ReserveIn(owner, resource, Fix.FromInt(10));
                var outbound = cell.ReserveOut(owner, resource, Fix.FromInt(2));
                c.Equal(before - Fix.FromInt(2), cell.Available(resource), "incoming promise cannot fund outgoing stock");
                c.Equal(Fix.FromInt(40) - before - Fix.FromInt(10), cell.Offer(resource, Fix.FromInt(100)), "outgoing promise never frees incoming capacity");
                c.Equal(ResourceTransferStatus.InvalidReceipt, cell.CheckReservedIn(game.World.Content, foreign, inbound).Status, "foreign request cannot spend incoming receipt");
                hash = game.Hash();
                if (point)
                {
                    var space = (PointSpace)game.World.Space;
                    c.Throws<InvalidOperationException>(() => space.WriteLayer(game.World, new PlayerId(0), "point.deposit", Fix.FromInt(10), Fix.FromInt(31)), "point raw growth cannot consume promised room");
                    c.Throws<InvalidOperationException>(() => space.AssignOwner(game.World, "point.deposit", new PlayerId(1)), "point owner protected by incoming and outgoing receipts");
                }
                else
                {
                    var map = (TileMap)game.World.Space; var layer = map.Layer<ByteLayer>("extract.deposit");
                    c.Throws<InvalidOperationException>(() => layer.Set(new TileCoord(20, 20), 62), "native raw growth cannot consume promised room");
                    c.Throws<InvalidOperationException>(() => map.AssignOwner(game.World, "extract.deposit", new PlayerId(1)), "native owner protected by incoming and outgoing receipts");
                    c.Equal(Fix.One, SpatialResourceValues.DepositOffer(layer, Fix.FromInt(126), Fix.Zero, Fix.FromInt(20),
                        CapacityPolicyContracts.Quantum(cell.CapacityPolicy, resource)), "byte storage bound clips exact compatible prefix without overflow");
                }
                c.Equal(hash, game.Hash(), "refused raw growth and owner changes are atomic");
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); ResourceEndpointId identity = cell.Endpoint; ResourceEndpointId.Sync(writer, ref identity);
                    int version = cell.Version; writer.Int(ref version); var book = new ResourceReservationBook();
                    book.Add(identity, owner, resource, Fix.FromInt(35), ReservationDirection.Inbound); book.Sync(writer, identity); writer.Flush();
                    c.Throws<CorruptStateException>(() => ((ISpatialResourceCellGuard)cell).StageSync(new StateReader(stream.ToArray(), 0, checked((int)stream.Length)), before), "over-capacity incoming checkpoint never publishes");
                }
                c.Equal(hash, game.Hash(), "invalid incoming restore retains authoritative state");
                var twin = factory.Load(game.Save()); var loaded = Source(twin, point);
                c.Equal(game.Hash(), twin.Hash(), "incoming and outgoing claims share original saved cell owner");
                c.That(loaded.CheckReservedIn(twin.World.Content, owner, inbound).Ready, "restored incoming receipt remains exact and usable");
                c.Equal(Fix.FromInt(3), cell.ReceiveReserved(game.World.Content, owner, inbound, Fix.FromInt(3)), "bounded incoming credit uses original layer writer");
                c.Equal(Fix.FromInt(3), loaded.ReceiveReserved(twin.World.Content, owner, inbound, Fix.FromInt(3)), "saved twin spends same receipt independently");
                c.Equal(before + Fix.FromInt(3), cell.Amount(resource), "incoming material changes actual layer, not a separate balance");
                c.Equal(Fix.FromInt(7), cell.ReservedIn(resource), "partial incoming receipt preserves remaining room");
                c.Equal(Fix.FromInt(2), cell.ReservedOut(resource), "incoming credit retains actual outbound commitment");
                c.Equal(game.Hash(), twin.Hash(), "partial incoming stock/book continuation is deterministic");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "partial inbound commitment survives another save");
                cell.ReleaseIn(owner, inbound); loaded.ReleaseIn(owner, inbound);
                c.Equal(ResourceTransferStatus.InvalidReceipt, cell.CheckReservedIn(game.World.Content, owner, inbound).Status, "released incoming token cannot be reused");
                cell.ReleaseOut(owner, outbound); loaded.ReleaseOut(owner, outbound);
                c.Equal(game.Hash(), twin.Hash(), "both directions settle through original receipt owner");
            }
        }
        private static void Admission(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current receipt-protected extraction identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one new embedded cell book payload, no old reader");
            foreach (Action<JsonValue> invalid in new Action<JsonValue>[] {
                root => root.Get("tile_resource_cells")[0].Set("identity", JsonValue.Number("0")),
                root => root.Get("tile_resource_cells")[0].Set("x", JsonValue.Number("-1")),
                root => root.Get("tile_resource_cells")[0].Set("public", JsonValue.Of(true)),
                root => root.Get("spatial_layers")[0].Set("resource_measure", JsonValue.Of("associated")),
                root => root.Get("spatial_resource_transfer_effects")[0].Set("quantities", Content.Json.Parse("{\"water\":1}")),
                root => { var duplicate = root.Get("tile_resource_cells")[0].Clone(); duplicate.Set("id", JsonValue.Of("extract.duplicate")); root.Get("tile_resource_cells").Add(duplicate); },
                root => { var duplicate = root.Get("tile_resource_cells")[0].Clone(); duplicate.Set("id", JsonValue.Of("extract.duplicate")); duplicate.Set("identity", JsonValue.Number("2")); root.Get("tile_resource_cells").Add(duplicate); },
                root => root.Get("activities")[0].Get("nodes")[0].Set("duration", Content.Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"hour\"}")),
                root => root.Get("activities")[0].Get("nodes")[0].Set("roles", Content.Json.Parse("[]")),
                root => root.Get("tile_resource_cells")[0].Set("layer", JsonValue.Of("extract.missing"))
            }) c.Throws<ContentException>(() => Create(Tiles(invalid)), "invalid authored cell/effect/instant-work admission fails before tick one");
            c.Throws<ContentException>(() => Create(Points(root => root.Get("spatial_resource_transfer_effects")[0].Set("quantities", Content.Json.Parse("{\"point.water\":1}"))), true),
                "post-resolution validation rejects wrong resource even when public cells are declared after effects");
            foreach (bool point in new[] { false, true })
            {
                GameFactory factory = point ? Points() : Tiles(); SimGame game = Create(factory, point); Entity target = Setup(c, game, point, false);
                IResourceOutlet source = Source(game, point); ResourceId resource = Definition(game, point).Layer.Resource.Resource;
                c.That(source is IResourceInlet && source is ISpatialResourceCellGuard, "each actual outlet is also an inlet and embedded guard");
                c.Equal(point ? 5 : 6, game.World.Space.StateSchemaVersion, "actual native/public layer owner owns the current codec");
                c.Equal(point ? Fix.FromInt(8) : Fix.FromInt(6), source.Amount(resource), "endpoint derives actual cell native stock");
                c.Equal(Fix.Zero, source.Amount(game.World.Content.ResourceId(point ? "point.water" : "water")), "unlinked resource never acquires a balance");
                c.That(source.CheckIncoming(new[] { new ResourceAmount(resource, Fix.One) }).Fits && source.CanReserveIn(new ReservationOwner("proof.in", 1), resource, Fix.One), "finite source exposes its bounded actual inlet");
                var owner = new ReservationOwner("proof.cell", 1); var foreign = new ReservationOwner("proof.cell", 2);
                ulong hash = game.Hash(); int version = source.Version;
                var transaction = game.World.Services.Get<IResourceTransaction>();
                var intent = new ResourceClaimIntent(ReservationDirection.Outbound, owner, source.Endpoint, BillOf(resource, Fix.FromInt(2)));
                c.That(transaction.EvaluateClaims(new PlayerId(0), new[] { intent }, System.Array.Empty<ResourceReservation>()).Ready, "existing coordinator previews actual spatial stock");
                c.Equal(hash, game.Hash(), "cell availability preview is pure");
                var claims = transaction.AcquireClaims(new PlayerId(0), new[] { intent }, System.Array.Empty<ResourceReservation>()); c.That(claims.Committed, "existing coordinator creates real owned cell receipt");
                c.Equal(version + 1, source.Version, "receipt mutation advances actual endpoint version");
                c.Equal(Fix.FromInt(2), source.ReservedOut(resource), "one embedded existing book owns promise quantity");
                c.Equal(ResourceTransferStatus.InvalidReceipt, source.CheckReservedOut(foreign, claims.Receipts[0].Token).Status, "foreign requester cannot spend owned receipt");
                hash = game.Hash();
                int oldCellVersion=source.Version, oldOwnerView=game.Seat(0).View.Space.Version, oldOtherView=game.Seat(1).View.Space.Version;
                if (point)
                {
                    var space = (PointSpace)game.World.Space; PointLayer layer = null; foreach (var current in space.SpatialLayers) if (current.Id == "point.deposit") layer = (PointLayer)current;
                    c.Equal(SpatialResourceChangeStatus.ReservedQuantity, layer.Withdraw(Fix.FromInt(10), Fix.FromInt(7)).Status, "ordinary public depletion cannot steal promised material");
                    c.Throws<InvalidOperationException>(() => space.WriteLayer(game.World, new PlayerId(0), layer.Id, Fix.FromInt(10), Fix.One), "actual point writer protects promises");
                    c.Throws<InvalidOperationException>(() => space.AssignOwner(game.World, layer.Id, new PlayerId(1)), "public ownership cannot migrate live receipts");
                }
                else
                {
                    var map = (TileMap)game.World.Space; var layer = map.Layer<ByteLayer>("extract.deposit"); var tile = new TileCoord(20, 20);
                    c.Equal(SpatialResourceChangeStatus.ReservedQuantity, layer.Withdraw(tile, Fix.FromInt(5)).Status, "ordinary native depletion cannot steal promised material");
                    c.Throws<InvalidOperationException>(() => layer.Set(tile, 2), "actual native Set protects promises");
                    c.Throws<InvalidOperationException>(() => layer.Fill((x, y) => 0), "atomic native generation cannot erase promised stock");
                    c.Throws<InvalidOperationException>(() => map.AssignOwner(game.World, layer.Id, new PlayerId(1)), "native ownership cannot migrate live receipts");
                }
                c.Equal(hash, game.Hash(), "stock/ownership/generation refusals preserve actual values/books/codec");
                c.That(source.Version==oldCellVersion&&source.Company==new PlayerId(0),"receipt-protected refusal retains actual endpoint owner/version");
                c.That(game.Seat(0).View.Space.Version==oldOwnerView&&game.Seat(1).View.Space.Version==oldOtherView,"receipt-protected refusal publishes no viewer revision prefix");
                c.Equal(ResourceTransferStatus.InvalidRequest, transaction.EvaluateClaims(new PlayerId(1), new[] { new ResourceClaimIntent(ReservationDirection.Outbound, foreign, source.Endpoint, BillOf(resource, Fix.One)) }, System.Array.Empty<ResourceReservation>()).Status, "foreign company cannot infer/use private deposit");
                SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual spatial stock and owned receipt roundtrip");
                IResourceOutlet loaded = Source(twin, point); c.Equal(source.Version, loaded.Version, "persistent endpoint versions do not rebase with map view cursors");
                c.That(source.CheckReservedOut(owner, claims.Receipts[0].Token).Ready && loaded.CheckReservedOut(owner, claims.Receipts[0].Token).Ready, "original actual receipt remains usable after load");
                var move = Movement(game, target, owner, Fix.One, claims.Receipts, point);
                c.That(transaction.CommitMovement(move).Committed, "existing movement protocol boundedly spends cell receipt");
                c.That(twin.World.Services.Get<IResourceTransaction>().CommitMovement(Movement(twin, Find(twin, target.Def.Id), owner, Fix.One, claims.Receipts, point)).Committed, "saved twin spends the same real receipt independently");
                Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "partial spatial transfer/history/version continuation is deterministic");
                c.Equal(Fix.One, source.ReservedOut(resource), "bounded debit keeps unspent receipt remainder");
                c.Equal(Fix.One, target.Get<IInletPort>().Amount(resource), "actual destination inventory receives exact native stock");
                VerifyTransfer(c, game, source.Endpoint, Fix.One);
                var remaining = source.Reservations[0];
                c.That(transaction.ReleaseClaims(new ResourceClaimReleaseIntent(new PlayerId(0), new[] { remaining }, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "release uses the existing actual endpoint book");
                c.Equal(Fix.Zero, source.ReservedOut(resource), "released receipt protects no stock");
                c.Equal(ResourceTransferStatus.InvalidReceipt, source.CheckReservedOut(owner, remaining.Token).Status, "released token cannot be spent twice");
                hash = game.Hash();
                var wrongBook = new ResourceReservationBook(); wrongBook.Add(source.Endpoint, owner, game.World.Content.ResourceId(point ? "point.water" : "water"), Fix.One, ReservationDirection.Outbound);
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); ResourceEndpointId identity = source.Endpoint; ResourceEndpointId.Sync(writer, ref identity); int savedVersion = source.Version; writer.Int(ref savedVersion); wrongBook.Sync(writer, identity); writer.Flush();
                    c.Throws<CorruptStateException>(() => ((ISpatialResourceCellGuard)source).StageSync(new StateReader(stream.ToArray(), 0, checked((int)stream.Length)), source.Amount(resource)), "invalid staged spatial book never publishes");
                }
                c.Equal(hash, game.Hash(), "bad restore retains original actual receipt owner");
                c.That(ReferenceEquals(Definition(game, point), Definition(twin, point)), "saved sessions legitimately share immutable resolved content");
                SimGame foreignCatalog = Create(point ? Points() : Tiles(), point);
                c.That(game.World.Services.Get<ISpatialResourceEndpoints>().ResolveCell(Definition(foreignCatalog, point)) == null, "foreign catalog definitions cannot alias actual cell bindings");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled real cell checkpoint remains resumable");

                // Instrument the existing codec, not a player command or an alternate stock owner.
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); ResourceEndpointId identity = source.Endpoint; ResourceEndpointId.Sync(writer, ref identity);
                    int exhausted = int.MaxValue; writer.Int(ref exhausted); new ResourceReservationBook().Sync(writer, identity); writer.Flush();
                    ((ISpatialResourceCellGuard)source).StageSync(new StateReader(stream.ToArray(), 0, checked((int)stream.Length)), source.Amount(resource))();
                }
                hash = game.Hash();
                c.Throws<OverflowException>(() => source.ReserveOut(owner, resource, Fix.One), "exhausted persistent cell version refuses before adding a receipt");
                c.Throws<OverflowException>(() => source.Take(resource, Fix.One), "exhausted persistent cell version refuses before debiting real stock");
                if(point)c.Throws<OverflowException>(()=>((PointSpace)game.World.Space).AssignOwner(game.World,"point.deposit",new PlayerId(1)),"exhausted actual cell revision refuses before ownership publication");
                c.Equal(hash, game.Hash(), "exhausted cell version preserves actual layer stock/book/geometry cursors");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "exhausted persistent source version remains exact across checkpoint");
            }
            // A separate named protocol fixture starts before Update; automatic jobs in the
            // physical scenario above may legitimately hold their OWN additional receipts.
            var transferFactory=Points();var transfer=Create(transferFactory,true);var pointSpace=(PointSpace)transfer.World.Space;
            pointSpace.AssignOwner(transfer.World,"point.deposit",new PlayerId(0));
            c.Equal(Code.None,pointSpace.WriteLayer(transfer.World,new PlayerId(0),"point.deposit",Fix.FromInt(10),Fix.FromInt(8)),"isolated protocol seeds actual authored cell");
            var cell=Source(transfer,true);var material=Definition(transfer,true).Layer.Resource.Resource;var promiseOwner=new ReservationOwner("proof.ownership",1);
            var token=cell.ReserveOut(promiseOwner,material,Fix.One);ulong protectedHash=transfer.Hash();
            c.Throws<InvalidOperationException>(()=>pointSpace.AssignOwner(transfer.World,"point.deposit",new PlayerId(1)),"actual owned receipt refuses transfer before any publication");
            c.Equal(protectedHash,transfer.Hash(),"receipt refusal leaves actual owner/book/material unchanged");
            cell.ReleaseOut(promiseOwner,token);var saved=transferFactory.Load(transfer.Save());int beforeTransfer=cell.Version;Fix stock=cell.Amount(material);
            pointSpace.AssignOwner(transfer.World,"point.deposit",new PlayerId(1));
            ((PointSpace)saved.World.Space).AssignOwner(saved.World,"point.deposit",new PlayerId(1));
            c.That(cell.Company==new PlayerId(1)&&cell.Version==beforeTransfer+1,"released actual cell transfers owner/version exactly once");
            c.Equal(stock,cell.Amount(material),"ownership transfer neither copies nor consumes physical material");
            c.Equal(transfer.Hash(),saved.Hash(),"released-cell ownership preserves saved twin continuation");
        }
        private static SpatialResourceChangeCommit Prepare(SimGame game, bool point, Fix amount)
        {
            if (!point) return ((TileMap)game.World.Space).Layer<ByteLayer>("extract.deposit").PrepareWithdrawal(new TileCoord(20, 20), amount);
            foreach (var layer in ((ISpatialLayerProvider)game.World.Space).SpatialLayers)
                if (layer.Id == "point.deposit") return ((ISpatialResourceQuantity<Fix>)layer).PrepareWithdrawal(Fix.FromInt(10), amount);
            throw new InvalidOperationException("missing actual point quantity");
        }
        private static void Preparation(Check c)
        {
            foreach (bool point in new[] { false, true })
            {
                var game = Create(point ? Points() : Tiles(), point); Setup(c, game, point, false);
                var source = Source(game, point); var resource = Definition(game, point).Layer.Resource.Resource;
                var owner = new ReservationOwner("proof.prepared", 1);
                var receipt = source.ReserveOut(owner, resource, Fix.FromInt(2));
                ulong hash = game.Hash(); int version = source.Version; Fix opening = source.Amount(resource);
                var denied = Prepare(game, point, opening);
                c.That(denied.Check.Ready, "pure material preparation grants no receipt authority");
                c.Equal(hash, game.Hash(), "preparation changes no actual stock/receipts/cursors");
                c.Equal(version, source.Version, "preparation changes no endpoint version");
                c.Throws<InvalidOperationException>(() => denied.Commit(), "actual commit cannot erase reserved material");
                c.Equal(hash, game.Hash(), "receipt-protected refusal has no mutation prefix");
                c.Throws<InvalidOperationException>(() => denied.Commit(), "failed preparation cannot be retried or retained as authority");
                var first = Prepare(game, point, Fix.One); var stale = Prepare(game, point, Fix.One);
                c.That(first.Commit().Committed, "unreserved material commits through original provider owner");
                c.Equal(opening - Fix.One, source.Amount(resource), "one exact debit reaches actual sparse material");
                c.Equal(Fix.FromInt(2), source.ReservedOut(resource), "preparation never spends another receipt");
                hash = game.Hash();
                c.Throws<InvalidOperationException>(() => first.Commit(), "successful preparation is single-use");
                c.Throws<InvalidOperationException>(() => stale.Commit(), "changed provider revisions invalidate a retained preparation");
                c.Equal(hash, game.Hash(), "single-use/stale refusal retains stock/book/cursors");
                c.That(source.CheckReservedOut(owner, receipt).Ready, "original receipt remains spendable after guarded preparations");
                source.ReleaseOut(owner, receipt);
            }
        }
        private static void Physical(Check c)
        {
            foreach (bool point in new[] { false, true })
            {
                GameFactory factory = point ? Points() : Tiles(); SimGame game = Create(factory, point); Entity target = Setup(c, game, point);
                IResourceOutlet source = Source(game, point); ResourceId resource = Definition(game, point).Layer.Resource.Resource; Fix opening = source.Amount(resource);
                Order order = Post(c, game, target, point); EntityId actorId = EntityId.None;
                Reach(game, () => !order.Node("operate").Progress.Equals(WorkProgress.Zero), "machine physically reaches and begins real extraction work");
                actorId = game.World.Entities.Get(Board(game).Job(order.Node("operate").JobId).Worker).Id;
                c.Equal(opening, source.Amount(resource), "travel/partial work does not grant or consume deposit material");
                c.Equal(Fix.Zero, target.Get<IInletPort>().Amount(resource), "no instant extraction inventory before earned work");
                c.That(order.Node("operate").Progress.WholeTicks > 0, "actual engine work owns elapsed extraction time");
                var effect = game.World.Content.Get<ISpatialResourceTransferEffectDefinition>(point ? "point.extract.material" : "extract.material");
                c.Equal(WorkEffectStatus.Failed, effect.Apply(game.World, order, order.Plan.Graph.Find("operate")).Status, "direct completion call cannot bypass earned work/state");
                c.Equal(opening, source.Amount(resource), "premature effect changes no source stock");
                SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save includes actual travel/work phase, not host timers");
                Order loadedOrder = Board(twin).Order(order.Id);
                Reach(game, () => order.State == OrderState.Done, "native/public timed extraction completion", twin);
                c.Equal(OrderState.Done, loadedOrder.State, "saved twin completes same physical DAG");
                c.Equal(Fix.FromInt(2), target.Get<IInletPort>().Amount(resource), "earned work transfers exact native deposit into actual inventory");
                c.Equal(opening - Fix.FromInt(2), source.Amount(resource), "actual spatial availability depletes exactly");
                c.Equal(0, source.Reservations.Count, "successful final transaction retains no orphan source receipts");
                c.Equal(0, game.World.Entities.Get(actorId).Get<IWorker>().Job, "finished extraction releases actual machine");
                c.Equal(game.Hash(), twin.Hash(), "same physical saved source/book/history state continues deterministically");
                VerifyTransfer(c, game, source.Endpoint, Fix.FromInt(2));
                var nodes = new List<ActivityNodeView>(); c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, nodes), "foreign seat cannot see private extraction DAG");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed extracted stock/deposit persists without reopening work");
            }
            // Actual capacity, not a guessed output grant, blocks the completed native operation.
            GameFactory nativeFactory = Tiles(); SimGame native = Create(nativeFactory); Entity site = Setup(c, native);
            ResourceId steel = Definition(native).Layer.Resource.Resource; var blockOwner = new ReservationOwner("proof.capacity", 1);
            ReservationToken capacity = site.Get<IInletPort>().ReserveIn(blockOwner, steel, Fix.FromInt(8));
            Order waiting = Post(c, native, site);
            Reach(native, () => waiting.Node("operate").State == NodeState.Completing, "earned extraction blocked by actual promised destination capacity");
            c.Equal(WorkEffectCodes.IncompatibleInventory, waiting.Node("operate").Blocker, "completion exposes existing typed capacity blocker");
            c.Equal(Fix.FromInt(6), Source(native).Amount(steel), "blocked completion retains actual deposit stock");
            c.Equal(0, Source(native).Reservations.Count, "refused whole-set movement creates no source prefix claim");
            SimGame waitingTwin = nativeFactory.Load(native.Save()); WorkProgress earned = waiting.Node("operate").Progress;
            for (int i = 0; i < 5; i++) { Tick(native); Tick(waitingTwin); }
            c.Equal(earned, waiting.Node("operate").Progress, "completion waiting never earns duplicate work");
            site.Get<IInletPort>().ReleaseIn(blockOwner, capacity); Find(waitingTwin, site.Def.Id).Get<IInletPort>().ReleaseIn(blockOwner, capacity);
            Reach(native, () => waiting.State == OrderState.Done, "existing retry completes after real capacity release", waitingTwin);
            c.Equal(native.Hash(), waitingTwin.Hash(), "blocked effect/save/retry uses sole graph/transaction owners");
            c.Equal(Fix.FromInt(2), site.Get<IInletPort>().Amount(steel), "released capacity permits exactly one transfer");

            GameFactory competingFactory = Tiles(root => {
                root.Get("spatial_resource_transfer_effects")[0].Set("quantities", Content.Json.Parse("{\"steel\":6}"));
                var alternative = root.Get("activities")[0].Clone(); alternative.Set("id", JsonValue.Of("extract.competing")); root.Get("activities").Add(alternative);
            });
            SimGame competing = Create(competingFactory); Entity sharedSite = Setup(c, competing);
            Order first = Post(c, competing, sharedSite);
            c.Equal(Codes.AlreadyDone, competing.Seat(0).Preview(new PostOrderCommand { Plan = "extract.activity", Target = sharedSite.Id }), "ordinary activity uniqueness remains authoritative");
            Order second = Post(c, competing, sharedSite, plan: "extract.competing");
            Reach(competing, () => first.State == OrderState.Done && second.Node("operate").State == NodeState.Completing, "two physical orders compete for actual finite deposit");
            c.Equal(WorkEffectCodes.NoStock, second.Node("operate").Blocker, "second earned operation cannot duplicate exhausted deposit");
            c.Equal(Fix.Zero, Source(competing).Amount(steel), "first operation exhausts actual deposit");
            c.Equal(Fix.FromInt(6), sharedSite.Get<IInletPort>().Amount(steel), "competing work conserves one real deposit quantity");
            c.That(TestKit.Order(competing, 0, new CancelOrderCommand { Order = second.Id }).Accepted, "ordinary cancellation stops blocked earned extraction");
            c.Equal(0, Source(competing).Reservations.Count, "cancelled blocked work leaves no orphan spatial claims");
            c.Equal(competing.Hash(), competingFactory.Load(competing.Save()).Hash(), "competing/cancelled physical work and empty deposit remain durable");
        }
    }
}
