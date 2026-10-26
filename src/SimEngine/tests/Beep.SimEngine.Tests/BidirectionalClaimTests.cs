using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class BidirectionalClaimTests
    {
        internal const string Case = "Bidirectional resource claims: atomic mixed commitments and physical saved continuation";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Cause = new Code("proof.mixed-consumption");
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 101 });
        private static IResourceTransaction Tx(SimGame game) => game.World.Services.Get<IResourceTransaction>();
        private static EndpointPools Pools(SimGame game) => game.World.Services.Get<EndpointPools>();
        private static IInventory Source(SimGame game) => game.World.Entities.All.First(e => e.Def.Id == "source").Get<IInventory>();
        private static ResourceId R(SimGame game, string id) => game.World.Content.ResourceId(id);
        private static ReservationOwner Owner(long id) => new ReservationOwner("proof.mixed", id);
        private static Bill Bill(SimGame game, string id, Fix amount) => new Bill(new[] { new ResourceAmount(R(game, id), amount) });
        private static ResourceClaimIntent Claim(SimGame game, ReservationDirection direction, long pool, long owner, string resource, Fix amount)
            => new ResourceClaimIntent(direction, Owner(owner), pool == 0 ? Source(game).Endpoint : Pools(game).Get(pool).Endpoint, Bill(game, resource, amount));
        private static ResourceReservation[] Copy(IReadOnlyList<ResourceReservation> records) => records.ToArray();
        private static ResourceFlowPage History(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0);
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("mixed Update fault: " + game.Fault);
            if (twin == null) return;
            twin.Update(); if (twin.Fault != null || twin.Hash() != game.Hash()) throw new InvalidOperationException("mixed saved twins diverged at " + game.Tick);
        }
        private static void Seed(SimGame game)
        {
            long id = 100;
            foreach (var row in new[] { (1L, "steel", Fix.FromInt(4)), (1L, "water", Fix.FromInt(2)),
                (2L, "steel", Fix.FromInt(3)), (5000000000L, "steel", Fix.FromInt(2)) })
            {
                var intent = new ResourceMovementIntent(Owner(id++), Company, Source(game).Endpoint,
                    Pools(game).Get(row.Item1).Endpoint, Bill(game, row.Item2, row.Item3), Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
                if (!Tx(game).CommitMovement(intent).Committed) throw new InvalidOperationException("actual seed movement refused");
            }
            Update(game);
        }
        private static ResourceClaimIntent[] Mixed(SimGame game) => new[] {
            Claim(game, ReservationDirection.Outbound, 1, 77, "steel", Fix.FromInt(2)),
            Claim(game, ReservationDirection.Inbound, 1, 77, "steel", Fix.One),
            Claim(game, ReservationDirection.Inbound, 2, 88, "water", Fix.FromRatio(1, 2)),
            Claim(game, ReservationDirection.Outbound, 2, 88, "steel", Fix.One),
            Claim(game, ReservationDirection.Inbound, 5000000000, 90, "steel", Fix.FromInt(2)),
            Claim(game, ReservationDirection.Outbound, 5000000000, 90, "steel", Fix.One),
            Claim(game, ReservationDirection.Outbound, 0, 70, "water", Fix.One),
            Claim(game, ReservationDirection.Inbound, 0, 70, "water", Fix.One) };
        private static void Refuse(Check c, SimGame game, ResourceClaimIntent[] set, ResourceTransferStatus expected, string label)
        {
            ulong hash = game.Hash();
            c.Equal(expected, Tx(game).EvaluateClaims(Company, set, System.Array.Empty<ResourceReservation>()).Status, label + " pure admission");
            var result = Tx(game).AcquireClaims(Company, set, System.Array.Empty<ResourceReservation>());
            c.Equal(expected, result.Status, label + " complete acquisition refuses");
            c.Equal(0, result.Receipts.Count, label + " no prefix claims");
            c.Equal(hash, game.Hash(), label + " books, tokens, versions, stock and history unchanged");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "strict current mixed protocol build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "explicit direction in pending claim command payload");
            c.That(typeof(IResourceInlet).IsAssignableFrom(typeof(IResourceOutlet)), "every outlet remains an inlet");
            Admission(c); Transfers(c); Commands(c); Faults(c); Physical(c);
        }
        private static void Admission(Check c)
        {
            var factory = EndpointGame.Factory(); var game = New(factory); Seed(game);
            c.That(game.World.Space == null && !game.World.Services.Has<WorkBoard>(), "mixed endpoint protocol requires no map/work/business scheduler");
            var set = Mixed(game); ulong hash = game.Hash(); var preview = Tx(game).EvaluateClaims(Company, set, System.Array.Empty<ResourceReservation>());
            c.That(preview.Ready && preview.Availability.Count == 8, "entity/scoped/wide mixed set retains per-native-line offers");
            foreach (var offer in preview.Availability)
                c.That(offer.Available >= offer.Required && (offer.Direction != ReservationDirection.Inbound || offer.Available == offer.Required), "inbound offers are bounded native amounts, not pooled heterogeneous capacity");
            c.Equal(hash, game.Hash(), "ready offers mutate no owner or token sequence");
            Refuse(c, game, new[] { Claim(game, ReservationDirection.Outbound, 1, 1, "steel", Fix.FromInt(3)),
                Claim(game, ReservationDirection.Outbound, 1, 2, "steel", Fix.FromInt(2)) }, ResourceTransferStatus.NoStock, "aggregate different-owner source shortage");
            Refuse(c, game, new[] { Claim(game, ReservationDirection.Outbound, 1, 1, "steel", Fix.One),
                Claim(game, ReservationDirection.Inbound, 1, 2, "steel", Fix.FromInt(3)),
                Claim(game, ReservationDirection.Inbound, 1, 3, "water", Fix.FromInt(2)) }, ResourceTransferStatus.NoCapacity, "aggregate mixed-resource capacity shortage");
            Refuse(c, game, new[] { Claim(game, ReservationDirection.Outbound, 1, 1, "steel", Fix.FromInt(4)),
                Claim(game, ReservationDirection.Inbound, 1, 2, "water", Fix.FromInt(5)) }, ResourceTransferStatus.NoCapacity, "outbound promises never free inlet space");
            Refuse(c, game, new[] { Claim(game, ReservationDirection.Inbound, 2, 1, "water", Fix.One),
                Claim(game, ReservationDirection.Outbound, 2, 2, "water", Fix.One) }, ResourceTransferStatus.NoStock, "incoming promise cannot fund current outgoing stock");
            var one = Claim(game, ReservationDirection.Inbound, 1, 1, "steel", Fix.One);
            Refuse(c, game, new[] { one, one }, ResourceTransferStatus.InvalidRequest, "duplicate same-side owner");
            Refuse(c, game, new[] { Claim(game, (ReservationDirection)42, 1, 1, "steel", Fix.One) }, ResourceTransferStatus.InvalidRequest, "invalid direction");
            Refuse(c, game, new[] { Claim(game, ReservationDirection.Inbound, 1, 1, "steel", Fix.FromRatio(1, 2)) }, ResourceTransferStatus.InvalidRequest, "fractional discrete inlet demand");
            Refuse(c, game, new[] { one, Claim(game, ReservationDirection.Inbound, 3, 2, "steel", Fix.One) }, ResourceTransferStatus.InvalidRequest, "foreign destination");
            Refuse(c, game, new[] { one, new ResourceClaimIntent(ReservationDirection.Inbound, Owner(2), new ResourceEndpointId("missing", 99), Bill(game, "steel", Fix.One)) }, ResourceTransferStatus.MissingEndpoint, "missing later destination");
            var held = Tx(game).AcquireClaims(Company, new[] { Claim(game, ReservationDirection.Inbound, 1, 55, "water", Fix.FromInt(3)) }, System.Array.Empty<ResourceReservation>());
            c.That(held.Committed, "actual existing inlet hold");
            Refuse(c, game, new[] { Claim(game, ReservationDirection.Inbound, 1, 56, "steel", Fix.FromInt(2)) }, ResourceTransferStatus.NoCapacity, "existing inbound promises counted");
            Refuse(c, game, new[] { Claim(game, ReservationDirection.Inbound, 1, 55, "water", Fix.One) }, ResourceTransferStatus.InvalidReceipt, "duplicate actual held owner-side");
            c.That(Tx(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, held.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "exact preexisting inbound receipt releases");
            var twin = factory.Load(game.Save()); var reversed = Mixed(twin); Array.Reverse(reversed);
            var acquired = Tx(game).AcquireClaims(Company, set, System.Array.Empty<ResourceReservation>()); var other = Tx(twin).AcquireClaims(Company, reversed, System.Array.Empty<ResourceReservation>());
            c.That(acquired.Committed && other.Committed && acquired.Receipts.Count == 8, "one complete acquisition allows opposite directions for same owner/resource");
            c.Equal(game.Hash(), twin.Hash(), "reversed mixed input produces canonical actual book and token state");
            c.That(acquired.Receipts.SequenceEqual(other.Receipts), "returned canonical native receipts agree exactly");
            c.Equal(Fix.FromInt(4), Pools(game).Get(1).Amount(R(game, "steel")), "neither side moves stock at acquisition");
            c.Equal(6, History(game).Entries.Count, "only two authored initial-stock facts and four real seeds, no mixed-reservation facts");
            c.Throws<NotSupportedException>(() => ((IList<ResourceReservation>)acquired.Receipts).Clear(), "retained mixed receipt set immutable");
            hash = game.Hash(); c.That(Tx(game).EvaluateClaimReceipts(Company, acquired.Receipts).Ready, "all actual mixed receipts validate");
            c.Equal(ResourceTransferStatus.InvalidReceipt, Tx(game).ConsumeClaims(Company, acquired.Receipts, Cause, System.Array.Empty<ResourceReservation>()).Status, "mixed consumption cannot spend inbound capacity");
            var inbound = acquired.Receipts.First(r => r.Direction == ReservationDirection.Inbound);
            c.Equal(ResourceTransferStatus.InvalidReceipt, Tx(game).ExtendClaim(Company, inbound, Fix.One).Status, "inlet promise cannot extend as source stock");
            var forged = Copy(acquired.Receipts); var original = forged[0];
            forged[0] = new ResourceReservation(original.Token, original.Owner, original.Resource, original.Amount,
                original.Direction == ReservationDirection.Inbound ? ReservationDirection.Outbound : ReservationDirection.Inbound);
            c.Equal(ResourceTransferStatus.InvalidReceipt, Tx(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, forged, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Status, "wrong-side forged receipt releases no valid prefix");
            c.Equal(hash, game.Hash(), "receipt refusals preserve whole mixed set");
            var restored = factory.Load(game.Save()); c.Equal(hash, restored.Hash(), "actual books alone save both directions");
            c.That(Tx(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, acquired.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed && Tx(restored).ReleaseClaims(new ResourceClaimReleaseIntent(Company, acquired.Receipts.Reverse().ToArray(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "whole exact mixed release and restored reversed release");
            c.Equal(game.Hash(), restored.Hash(), "mixed release deterministic across saved continuation");
            hash = game.Hash(); c.Equal(ResourceTransferStatus.InvalidReceipt, Tx(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, acquired.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Status, "double release refused");
            c.Equal(hash, game.Hash(), "double release changes no token sequence");
            Pools(game).InletOnly = true;
            var inletSet = new[] { Claim(game, ReservationDirection.Inbound, 2, 200, "steel", Fix.One) };
            c.That(Pools(game).Resolve(Pools(game).Get(2).Endpoint) is IResourceInlet && !(Pools(game).Resolve(Pools(game).Get(2).Endpoint) is IResourceOutlet), "external endpoint actually exposes inlet only");
            c.That(Tx(game).EvaluateClaims(Company, inletSet, System.Array.Empty<ResourceReservation>()).Ready, "inlet-only provider admits generic incoming claims");
            Refuse(c, game, new[] { inletSet[0], Claim(game, ReservationDirection.Outbound, 2, 201, "steel", Fix.One) }, ResourceTransferStatus.Incompatible, "inlet-only cached endpoint cannot claim outbound");
            var inlet = Tx(game).AcquireClaims(Company, inletSet, System.Array.Empty<ResourceReservation>());
            c.That(inlet.Committed && Tx(game).EvaluateClaimReceipts(Company, inlet.Receipts).Ready && Tx(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, inlet.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "same native book serves inlet-only acquisition/evaluation/release without new store");
        }
        private static void Transfers(Check c)
        {
            var game = New(EndpointGame.Factory()); Seed(game); var source = Source(game); var pool = Pools(game).Get(1);
            var acquired = Tx(game).AcquireClaims(Company, new[] {
                Claim(game, ReservationDirection.Outbound, 0, 300, "steel", Fix.FromInt(3)),
                Claim(game, ReservationDirection.Inbound, 1, 300, "steel", Fix.FromInt(3)) }, System.Array.Empty<ResourceReservation>());
            var outgoing = acquired.Receipts.Single(r => r.Direction == ReservationDirection.Outbound);
            var incoming = acquired.Receipts.Single(r => r.Direction == ReservationDirection.Inbound);
            c.That(Tx(game).CommitTransfers(new[] { new ResourceTransferIntent(Owner(300), Company, outgoing.Token, incoming.Token, R(game, "steel"), Fix.One) }).Committed, "existing transfer protocol physically spends mixed receipts partially");
            Update(game); ulong hash = game.Hash();
            c.Equal(ResourceTransferStatus.InvalidReceipt, Tx(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, acquired.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Status, "stale full-size records cannot release changed promises");
            c.Equal(hash, game.Hash(), "stale mixed release leaves both actual remainders");
            var remaining = source.Reservations.Concat(pool.Reservations).Where(r => r.Owner.Equals(Owner(300))).ToArray();
            c.That(remaining.Length == 2 && remaining.All(r => r.Amount == Fix.FromInt(2)), "partial physical transfer leaves both exact two-unit remainders");
            c.That(Tx(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, remaining, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "remaining mixed records release without manufacturing stock");
            c.Equal(Fix.FromInt(20), source.Amount(R(game, "steel")), "only actual one-unit movement reduces source");
            c.Equal(Fix.FromInt(5), pool.Amount(R(game, "steel")), "only actual one-unit movement increases destination");
            var movement = new ResourceMovementIntent(Owner(301), Company, source.Endpoint, Pools(game).Get(2).Endpoint,
                new Bill(new[] { new ResourceAmount(R(game, "steel"), Fix.FromInt(2)), new ResourceAmount(R(game, "water"), Fix.FromRatio(1, 2)) }), Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
            hash = game.Hash(); c.That(Tx(game).EvaluateMovement(movement).Ready, "whole two-resource movement preview uses mixed admission");
            c.Equal(hash, game.Hash(), "movement preview pure");
            c.That(Tx(game).CommitMovement(movement).Committed, "movement obtains one complete source/inlet set through original transaction"); Update(game);
            c.Equal(0, source.Reservations.Count + Pools(game).Get(2).Reservations.Count, "complete movement leaks no source or inlet hold");
            var held = Tx(game).AcquireClaims(Company, new[] { Claim(game, ReservationDirection.Outbound, 0, 302, "steel", Fix.FromInt(3)) }, System.Array.Empty<ResourceReservation>());
            movement = new ResourceMovementIntent(Owner(302), Company, source.Endpoint, Pools(game).Get(5000000000).Endpoint,
                Bill(game, "steel", Fix.One), held.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
            c.That(Tx(game).CommitMovement(movement).Committed, "existing larger source receipt supports bounded movement to wide endpoint"); Update(game);
            var remainder = source.Reservations.Single(r => r.Owner.Equals(Owner(302)));
            c.That(remainder.Token.Equals(held.Receipts[0].Token) && remainder.Amount == Fix.FromInt(2), "movement preserves true larger source token remainder");
            c.Equal(0, Pools(game).Get(5000000000).Reservations.Count, "bounded movement consumes its exact inlet promise");
            c.That(Tx(game).ReleaseClaims(new ResourceClaimReleaseIntent(Company, new[] { remainder }, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "retained source receipt explicitly releases");
            c.Equal(10, History(game).Entries.Count, "two initialization facts, four seeds and four actual resource transfer lines recorded");
        }
        private static void Commands(Check c)
        {
            var factory = EndpointGame.Factory(); var game = New(factory); Seed(game);
            game.Seat(0).Submit(new ClaimProbeCommand { Intents = Mixed(game) });
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending mixed directions survive strict saved command codec");
            using (var recorder = game.RecordReplay(Limits))
            {
                Update(game, twin);
                var records = Source(game).Reservations.Concat(Pools(game).Get(1).Reservations).Concat(Pools(game).Get(2).Reservations)
                    .Concat(Pools(game).Get(5000000000).Reservations).ToArray();
                c.That(records.Length == 8 && records.Count(r => r.Direction == ReservationDirection.Inbound) == 4, "ordinary Update preserves declared inbound/outbound command intents");
                game.Seat(0).Submit(new ClaimProbeCommand { Consume = true, Receipts = records });
                Update(game); long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.That(results.Count == 2 && results[0].Accepted && !results[1].Accepted && results[1].Refusal == Codes.InvalidArgument && game.Fault == null, "inbound consumption is ordinary typed refusal, not terminal session fault");
                game.Seat(0).Submit(new ClaimProbeCommand { Release = true, Receipts = records.Reverse().ToArray() });
                twin = factory.Load(game.Save()); Update(game, twin);
                c.Equal(0, Source(game).Reservations.Count + Pools(game).Get(1).Reservations.Count + Pools(game).Get(2).Reservations.Count + Pools(game).Get(5000000000).Reservations.Count, "pending saved mixed release removes only original records");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "canonical replay reproduces saved initial mixed admission, typed refusal and exact release: " + replay.Failure + "/" + replay.Detail);
            }
            c.Equal(6, History(game).Entries.Count, "commands retain initialization/seeding history but create no fictitious transport facts");
        }
        private static void Faults(Check c)
        {
            foreach (var mode in new[] { ClaimFaultMode.MutatingInCheck, ClaimFaultMode.ExtraInAcquire, ClaimFaultMode.IgnoreInRelease, ClaimFaultMode.WrongInDirection, ClaimFaultMode.SilentVersion })
            {
                var game = New(EndpointGame.Factory()); Seed(game);
                var command = new ClaimProbeCommand { Intents = new[] {
                    Claim(game, ReservationDirection.Outbound, 0, 400, "steel", Fix.One),
                    Claim(game, ReservationDirection.Inbound, 2, 400, "steel", Fix.One) } };
                if (mode == ClaimFaultMode.IgnoreInRelease)
                { command.Receipts = Copy(Tx(game).AcquireClaims(Company, command.Intents, System.Array.Empty<ResourceReservation>()).Receipts); command.Release = true; }
                Pools(game).ClaimFault = mode; game.Seat(0).Submit(command); game.Update();
                c.That(game.Fault != null, mode + " native inlet contract violation faults original Update");
                c.Throws<InvalidOperationException>(() => game.Save(), mode + " no resumable partial provider state");
                c.Throws<InvalidOperationException>(() => game.Hash(), mode + " no verified partial checkpoint");
                c.Equal(6, History(game).Entries.Count, mode + " initialization/seeding only, no prefix-success flow history or invented rollback");
            }
        }
        private static Fix Stock(SimGame game, EntityId entity, string resource)
        {
            var facts = new List<Facet>(); if (!game.Seat(0).View.Inspect(entity, facts)) throw new InvalidOperationException("missing player-visible stock");
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == resource).Aggregate(Fix.Zero, (sum, fact) => sum + fact.Value);
        }
        private static void Reach(SimGame game, Func<bool> done, string phase, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Update(game, twin);
            if (done()) return;
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            throw new InvalidOperationException(phase + "/step=" + game.Tick + "/jobs=" + string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
        }
        private static void Physical(Check c)
        {
            // Normal player acceptance: no World, provider/book mutation or fabricated arrival/completion.
            var factory = Colony.Factory(); var game = Colony.New(283); var idle = Colony.New(283); ulong idleHash = idle.Hash();
            using (var recorder = game.RecordReplay(Limits))
            {
                var command = new ConstructCommand { Def = "tower", Priority = 60, Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) };
                ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "public construction preview remains available");
                c.Equal(hash, game.Hash(), "public policy/placement preview pure");
                var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary player starts physical construction");
                c.That(game.Seat(0).Snapshot.Entities.Any(e => e.Id == result.Entity && e.Def == "site:tower"), "unfinished site exists before physical work");
                bool moving = false, loaded = false;
                Reach(game, () => { moving |= game.Seat(0).Snapshot.Entities.Any(e => e.State == new Code("state.moving"));
                    var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                    loaded |= jobs.Any(j => j.IsHaul && j.Loaded > Fix.Zero); return moving && loaded; }, "actual loaded travel");
                c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "loaded travel does not instantly complete structure");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "moving loaded native commitments saved without second claim manager");
                long step = game.Tick; game.Clock.Pause(); twin.Clock.Pause(); Update(game, twin);
                c.Equal(step, game.Tick, "sole clock pause prevents all travel/loading/building progress");
                game.Clock.Resume(); twin.Clock.Resume();
                Reach(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "delivered staged construction", twin);
                Fix steel = Fix.Zero; foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == Company)) steel += Stock(game, entity.Id, "steel");
                c.Equal(Fix.FromInt(20), steel, "physical 50 minus 30 steel conservation");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "public timed hauling/building replay matches real saved continuation: " + replay.Failure + "/" + replay.Detail);
            }
            c.Equal(idleHash, idle.Hash(), "native game and replay leave independent session unchanged");
        }
    }
}
