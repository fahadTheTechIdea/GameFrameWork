using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public static partial class ResourceDispositionTests
    {
        internal const string MixedCargoCase = "Cargo player continuation: authored mixed stock saved interruption and exact fractional delivery";

        // The driver authors content, sends player commands and observes detached facts only.
        // Original logistics/work/inventory owners choose, reserve, move and unload the cargo.
        private static void PublicMixedCargo(Check c)
        {
            foreach (bool half in new[] { false, true }) PublicMixedCargo(c, half);
        }

        private static void PublicMixedCargo(Check c, bool half)
        {
            var factory = PhysicalRecoveryTestContent.Factory(Json, cargo: true,
                additionalDocuments: new Dictionary<string, string> {
                    ["mixed-cargo-startup.json"] = FrameworkProofContent.Read("mixed-cargo-startup.json") },
                edit: root => {
                    root.Get("scenarios")[0].Get("settings").Set(WorldSetupModule.ModuleId,
                        Content.Json.Parse("{\"profile\":\"disposition.mixed-start\"}"));
                    if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                });
            var game = New(factory); SimGame twin = null;
            EntityId machine = game.Seat(0).Snapshot.Entities.Single(row => row.Def == "loaded_truck").Id;
            EntityId destination = game.Seat(0).Snapshot.Entities.Single(row => row.Def == "water_store").Id;
            if (!game.Seat(0).View.TryFindResource("steel", out var steel) || !game.Seat(0).View.TryFindResource("water", out var water))
                throw new InvalidOperationException("mixed cargo requires its actual authored resources");
            var view = game.Seat(0).View.Feature<LogisticsView>();
            var rows = new List<CargoReturnView>(); Fix load = Fix.FromRatio(11, 2), quantum = half ? Fix.FromRatio(1, 2) : Fix.One;
            Fix Stock(SimGame session, EntityId entity, string resource) {
                var facets = new List<Facet>(); session.Seat(0).View.Inspect(entity, facets);
                return facets.Where(row => row.Key == Storage.FacetStock && row.Subject == resource)
                    .Aggregate(Fix.Zero, (sum, row) => sum + row.Value);
            }
            List<WorkJobView> Jobs(SimGame session) {
                var values = new List<WorkJobView>(); session.Seat(0).View.Feature<WorkView>().CopyJobs(values); return values;
            }
            void Observe() {
                if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("mixed cargo fault half=" + half + "/" + game.Fault + "/" + twin?.Fault);
                c.Equal(Fix.FromInt(3), Stock(game, machine, "steel"), "blocked first resource is never erased or substituted");
                c.Equal(load, Stock(game, machine, "water") + Stock(game, destination, "water"), "all finite water survives every physical update");
                if (twin != null) c.Equal(game.Hash(), twin.Hash(), "original cargo claims motion and handling survive interleaved saved continuation");
            }
            void Step() { game.Update(); twin?.Update(); Observe(); }
            void Send(bool held) {
                var command = new SetWorkerHeldCommand { Worker = machine, Held = held };
                ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary hold preview uses engine eligibility");
                c.Equal(hash, game.Hash(), "hold preview mutates no cargo job or clock");
                var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary hold/release accepted " + result.Refusal);
                if (!result.Accepted) throw new InvalidOperationException("mixed cargo command refused " + result.Refusal);
                if (twin != null) c.That(TestKit.Order(twin, 0, command).Accepted, "saved twin accepts identical player intent");
                Observe();
            }
            void Save() { twin = factory.Load(game.Save()); Observe(); }
            void Until(Func<bool> ready, string stage) {
                var watch = Stopwatch.StartNew(); int updates = 0;
                while (!ready() && updates < 300 && watch.Elapsed < TimeSpan.FromSeconds(20)) { Step(); updates++; }
                if (!ready()) throw new InvalidOperationException(stage + " half=" + half + " clock=" + game.Clock.StepIndex +
                    " updates=" + updates + " cargo=" + Stock(game, machine, "water") + " destination=" + Stock(game, destination, "water") +
                    " state=" + game.Seat(0).Snapshot.Entities.Single(row => row.Id == machine).State +
                    " jobs=" + string.Join(",", Jobs(game).Select(row => row.Id + "/" + row.State + "/" + row.Blocker)));
            }
            IReadOnlyList<ResourceReservation> Reservations(SimGame session, EntityId entity) {
                c.That(session.Seat(0).View.Feature<InventoryView>().TryGetReservations(entity, Code.None, out var values), "private view resolves the actual cargo or receiver book");
                return values;
            }
            void SameReceipt(ResourceReservation expected, ResourceReservation actual) {
                c.That(expected.Token.Equals(actual.Token) && expected.Owner.Equals(actual.Owner) && expected.Resource == actual.Resource &&
                    expected.Direction == actual.Direction && expected.Amount == actual.Amount, "save retains exact original token owner direction and outstanding quantity");
            }

            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "mixed cargo recovery has no business dependency");
            c.Equal(2, game.Seat(0).Snapshot.Entities.Count, "master-listed startup owns exactly the authored carrier and receiver");
            Send(true); Save();
            ulong initialHash = game.Hash();
            c.Equal(Code.None, view.CopyCargoReturns(machine, rows), "engine publishes per-resource mixed cargo diagnostics");
            c.That(rows.Count == 2 && rows[0].Resource == steel && rows[1].Resource == water, "later deliverable resource follows the blocked canonical first line");
            c.Equal(HaulStatus.NoDestination, rows[0].Return.Status, "no compatible steel destination is truthful blocked intent");
            c.Equal(HaulStatus.NoCarrier, rows[1].Return.Status, "held carrier cannot silently reserve a return");
            CargoReturnView captured = rows[1];
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().CopyCargoReturns(machine, new List<CargoReturnView>()), "foreign player cannot read the load");
            c.That(!game.Seat(1).View.Feature<InventoryView>().TryGetReservations(machine, Code.None, out _), "foreign player cannot read cargo receipt authority");
            c.Equal(initialHash, game.Hash(), "mixed diagnostics and denied reads are pure");
            SimGame isolated = factory.Load(game.Save()); ulong isolatedHash = isolated.Hash();
            long cursor = Read(game).Latest;
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using var recording = game.RecordReplay(limits);
            Send(false);
            Until(() => Jobs(game).Any(row => row.Worker == machine && row.RecoveryKind == LogisticsRecoveryKind.CargoReturn), "ordinary automatic mixed return acquisition");
            var first = Jobs(game).Single(row => row.Worker == machine);
            c.That(first.Resource == water && first.Loaded == load && first.Amount == load && !first.Source.IsValid && !first.Purchase && first.Destination == destination,
                "sole selector skips blocked steel and acquires a real already-loaded water journey");
            c.Equal(Fix.Zero, Stock(game, destination, "water"), "selection and reservations do not instantly unload");
            var cargoReceipt = Reservations(game, machine).Single(); var receivingReceipt = Reservations(game, destination).Single();
            c.That(cargoReceipt.Direction == ReservationDirection.Outbound && receivingReceipt.Direction == ReservationDirection.Inbound &&
                cargoReceipt.Owner.Equals(receivingReceipt.Owner) && cargoReceipt.Amount == load && receivingReceipt.Amount == load,
                "one original job owns both exact loaded-cargo and destination commitments");
            c.Equal(ResourceEndpointId.ForEntity(machine), cargoReceipt.Token.Endpoint, "outbound promise belongs to the real loaded carrier");
            c.Equal(ResourceEndpointId.ForEntity(destination), receivingReceipt.Token.Endpoint, "inbound promise belongs to the real receiver");
            Until(() => game.Seat(0).Snapshot.Entities.Single(row => row.Id == machine).State == new Code("state.moving"), "real carrier travel before interruption");
            Save(); SameReceipt(cargoReceipt, Reservations(twin, machine).Single()); SameReceipt(receivingReceipt, Reservations(twin, destination).Single());
            Send(true);
            c.That(!Jobs(game).Any(row => row.Id == first.Id), "ordinary player hold retires the original haul commitment");
            c.Equal(0, Reservations(game, machine).Count, "interruption releases original cargo promise");
            c.Equal(0, Reservations(game, destination).Count, "interruption releases original receiver promise");
            c.Equal(load, Stock(game, machine, "water"), "interrupted travel retains all actual cargo");
            c.Equal(cursor, Read(game).Latest, "travel and cancellation publish no fictitious physical unload");
            c.Equal(Code.None, view.ReadCargoDisposition(machine, out var held), "engine exposes the interrupted physical partition");
            c.That(held.Committed.IsEmpty && held.Retained.IsEmpty && held.Unassigned.AmountOf(water) == load && held.Unassigned.AmountOf(steel) == Fix.FromInt(3),
                "both original resources remain truthful unassigned cargo");
            Save();
            for (int i = 0; i < 4; i++) Step();
            game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); Step();
            c.Equal(paused, game.Hash(), "sole paused game clock freezes cargo recovery"); game.Clock.Resume(); twin.Clock.Resume();
            Send(false);
            Until(() => Stock(game, destination, "water").IsPositive, "saved interrupted load earns its first productive unload");
            c.Equal(quantum, Stock(game, destination, "water"), "one authored game-time handling quantum transfers exactly its real amount");
            var resumed = Jobs(game).Single(row => row.Worker == machine);
            c.That(resumed.Id > first.Id && resumed.Resource == water, "engine replans through a new actual job rather than reviving cancelled authority");
            cargoReceipt = Reservations(game, machine).Single(); receivingReceipt = Reservations(game, destination).Single();
            c.That(cargoReceipt.Amount == load - quantum && receivingReceipt.Amount == load - quantum &&
                cargoReceipt.Owner.Equals(receivingReceipt.Owner), "partial unload shrinks both original resumed promises to the exact remainder");
            Save(); SameReceipt(cargoReceipt, Reservations(twin, machine).Single()); SameReceipt(receivingReceipt, Reservations(twin, destination).Single());
            Until(() => Stock(game, machine, "water").IsZero && !Jobs(game).Any(row => row.Id == resumed.Id), "saved fractional unload and departure finish physically");
            for (int i = 0; i < 8; i++) Step();
            c.Equal(0, Reservations(game, machine).Count, "settled return leaves no orphan cargo authority");
            c.Equal(0, Reservations(game, destination).Count, "settled return leaves no orphan destination authority");
            c.Equal(load, captured.Stock, "captured pre-dispatch mixed view remains immutable after delivery");
            c.Equal(HaulStatus.NoCarrier, captured.Return.Status, "captured held preview is not a live job object");
            c.Equal(Code.None, view.CopyCargoReturns(machine, rows), "remaining blocked line stays inspectable");
            c.That(rows.Count == 1 && rows[0].Resource == steel && rows[0].Return.Status == HaulStatus.NoDestination, "unsupported cargo remains real stock with an explicit blocker");
            var page = Read(game, cursor); Fix remaining = load, received = Fix.Zero;
            c.Equal(half ? 11 : 6, page.Entries.Count, "only productive quanta and the exact final fractional remainder publish facts");
            foreach (var entry in page.Entries) {
                var fact = entry.Item; Fix amount = Fix.Min(quantum, remaining);
                c.That(fact.Kind == ResourceFlowKind.StockTransfer && fact.Resource == water && fact.Source.Endpoint == cargoReceipt.Token.Endpoint &&
                    fact.Destination.Endpoint == receivingReceipt.Token.Endpoint, "every fact retains actual resource and physical endpoint provenance");
                c.Equal(amount, fact.Amount, "actual handling fact never rounds away native fractional cargo");
                c.Equal(remaining, fact.SourceBefore.Value, "source history continues the saved outstanding stock");
                c.Equal(received, fact.DestinationBefore, "receiver history continues without duplication");
                remaining -= amount; received += amount;
                c.Equal(remaining, fact.SourceAfter.Value, "source fact agrees with exact physical removal");
                c.Equal(received, fact.DestinationAfter, "receiver fact agrees with exact physical admission");
            }
            c.Equal(new BigInteger(load.Raw), page.Totals(water).MovedRaw, "actual water transfers account for the finite load exactly once");
            c.Equal(BigInteger.Zero, page.Totals(steel).LostRaw, "blocked steel is never disguised as loss");
            c.Equal(isolatedHash, isolated.Hash(), "interleaved active sessions do not advance or mutate another loaded session");
            Save();
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.Game?.Hash() == game.Hash(), "actual ordinary-command mixed-cargo replay " + replay.Failure + "/" + replay.Detail);
        }
    }
}
