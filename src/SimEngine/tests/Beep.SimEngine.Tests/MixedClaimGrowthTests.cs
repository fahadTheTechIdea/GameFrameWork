using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationInputPortTests;

namespace Beep.SimEngine.Tests
{
    // Thin player: ordinary engine requests, copied immutable views and Update. No stock/claim mutation.
    public static class MixedClaimGrowthTests
    {
        public const string Case = "P8 mixed claim growth: whole fresh/increase admission, original tokens and saved replenishment";
        public static void Register() => TestRunner.Add(Case, Run);
        private static IReadOnlyList<ResourceReservation> Claims(SimGame game, string entity = "ports.source")
        {
            if (!game.Seat(0).View.Feature<InventoryView>().TryGetReservations(Find(game, entity), Code.None, out var result))
                throw new InvalidOperationException("missing actual private receipt facts");
            return result;
        }
        private static Bill Quantity(SimGame game, string resource, Fix amount)
            => new Bill(new[] { new ResourceAmount(Resource(game, resource), amount) });
        private static ResourceClaimIntent Fresh(SimGame game, ReservationOwner owner, ResourceEndpointId endpoint, string resource, Fix amount,
            ReservationDirection direction = ReservationDirection.Outbound)
            => new ResourceClaimIntent(direction, owner, endpoint, Quantity(game, resource, amount));
        private static void Refuse(Check c, SimGame game, ResourceClaimIntent[] requests, string reason)
        {
            var original = Claims(game).ToArray(); ulong hash = game.Hash();
            var command = new ClaimProbeCommand { Intents = requests };
            c.Equal(Codes.Blocked, game.Seat(0).Preview(command), reason + " pure preview refuses");
            c.Equal(hash, game.Hash(), reason + " preview changes no state/token sequence");
            c.That(!TestKit.Order(game, 0, command).Accepted && game.Fault == null, reason + " Update refuses without fault");
            var actual = Claims(game);
            c.Equal(original.Length, actual.Count, reason + " changes no original/fresh claim prefix");
            for (int i = 0; i < actual.Count; i++)
            {
                c.Equal(original[i].Token, actual[i].Token, reason + " preserves exact actual token");
                c.Equal(original[i].Amount, actual[i].Amount, reason + " preserves exact protected amount");
            }
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory); var owner = new ReservationOwner("proof.replenishment", 1);
            var other = new ReservationOwner("proof.replenishment", 2); var half = Fix.Parse("0.5");
            var source = ResourceEndpointId.ForEntity(Find(game, "ports.source"));
            var sink = ResourceEndpointId.ForEntity(Find(game, "ports.sink-entity"));
            var limits = new ReplayLimits(2 * 1024 * 1024, 400); var recorder = game.RecordReplay(limits);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "complete mixed acquisition identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "pending intents preserve explicit original growth metadata");
            c.That(!game.Modules.Contains("sim.tycoon") && game.Seat(0).Snapshot.Space == null, "physical claims require no business/platform map");
            c.That(typeof(IInletPort).IsAssignableFrom(typeof(IOutletPort)) && typeof(IResourceInlet).IsAssignableFrom(typeof(IResourceOutlet)),
                "every outlet keeps its inlet contract");
            Order(c, game, new ClaimProbeCommand { Intents = new[] {
                Fresh(game, owner, source, "ports.ore", Fix.FromInt(2)), Fresh(game, owner, source, "ports.jig", Fix.One),
                Fresh(game, other, source, "ports.ore", Fix.FromInt(21)) } });
            var initial = Claims(game); var ore = initial.Single(row => row.Owner.Equals(owner) && row.Resource == Resource(game, "ports.ore"));
            var jig = initial.Single(row => row.Resource == Resource(game, "ports.jig"));
            var contending = initial.Single(row => row.Owner.Equals(other));
            ResourceClaimIntent Grow(ResourceReservation original, Fix amount) => new ResourceClaimIntent(original,
                new Bill(new[] { new ResourceAmount(original.Resource, amount) }));
            ResourceClaimIntent[] Ready() => new[] { Grow(ore, half), Fresh(game, owner, source, "ports.water", Fix.One),
                Fresh(game, owner, sink, "ports.alloy", Fix.One, ReservationDirection.Inbound) };
            Refuse(c, game, new[] { Grow(ore, half), Fresh(game, owner, source, "ports.water", Fix.FromInt(7)) }, "later liquid shortage");
            Refuse(c, game, new[] { Grow(ore, half), Fresh(game, new ReservationOwner("proof.replenishment", 3), source, "ports.ore", Fix.One) },
                "aggregate fresh/growth contention despite individually available requests");
            Refuse(c, game, new[] { Grow(ore, half), Fresh(game, owner, sink, "ports.alloy", Fix.FromInt(101), ReservationDirection.Inbound) },
                "later full inlet");
            foreach (string bad in new[] { "duplicate", "fresh-overlap", "stale", "owner", "resource", "direction", "invalid-token", "multi", "empty", "null", "discrete", "overflow", "missing" })
            {
                var original = ore; var bill = Quantity(game, "ports.ore", half);
                if (bad == "stale") original = new ResourceReservation(ore.Token, owner, ore.Resource, Fix.One, ore.Direction);
                if (bad == "owner") original = new ResourceReservation(ore.Token, other, ore.Resource, ore.Amount, ore.Direction);
                if (bad == "resource") original = new ResourceReservation(ore.Token, owner, Resource(game, "ports.water"), ore.Amount, ore.Direction);
                if (bad == "direction") original = new ResourceReservation(ore.Token, owner, ore.Resource, ore.Amount, ReservationDirection.Inbound);
                if (bad == "invalid-token") original = new ResourceReservation(default, owner, ore.Resource, ore.Amount, ore.Direction);
                if (bad == "missing") original = new ResourceReservation(new ReservationToken(new ResourceEndpointId("proof.absent", 1), 1), owner, ore.Resource, ore.Amount, ore.Direction);
                if (bad == "multi") bill = new Bill(new[] { new ResourceAmount(ore.Resource, half), new ResourceAmount(Resource(game, "ports.water"), half) });
                if (bad == "empty") bill = Bill.Empty;
                if (bad == "null") bill = null;
                if (bad == "discrete") { original = jig; bill = Quantity(game, "ports.jig", half); }
                if (bad == "overflow") bill = Quantity(game, "ports.ore", Fix.FromRaw(long.MaxValue));
                var requests = new[] { new ResourceClaimIntent(original, bill), Fresh(game, owner, source, "ports.water", Fix.One) };
                if (bad == "duplicate") requests[1] = requests[0];
                if (bad == "fresh-overlap") requests[1] = Fresh(game, owner, source, "ports.ore", half);
                Refuse(c, game, requests, bad);
            }
            c.Equal(Codes.Blocked, game.Seat(1).Preview(new ClaimProbeCommand { Intents = Ready() }), "copied receipt never grants foreign endpoint access");
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(new ClaimProbeCommand { Intents = Ready() }), "complete mixed request preview ready");
            c.Equal(before, game.Hash(), "successful mixed preview changes no state");
            game.Seat(0).Submit(new ClaimProbeCommand { Intents = Ready() }); var twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "saved pending fresh/increase envelope commits identical endpoint books");
            var current = Claims(game); var increased = current.Single(row => row.Token.Equals(ore.Token));
            c.Equal(Fix.Parse("2.5"), increased.Amount, "increase returns actual full original protected amount");
            c.Equal(ore.Token, increased.Token, "growth preserves original authority instead of acquiring a replacement token");
            c.Equal(Fix.FromInt(2), initial.Single(row => row.Token.Equals(ore.Token)).Amount, "earlier detached facts stay immutable");
            c.Equal(Fix.FromInt(24), Stock(game, "ports.ore", "ports.source"), "growth changes protection, not physical stock");
            c.Equal(Fix.FromInt(21), current.Single(row => row.Token.Equals(contending.Token)).Amount, "growth does not alter other owner");
            c.Equal(1, Claims(game, "ports.sink-entity").Count, "fresh inlet is acquired with growing outbound claims");
            Refuse(c, game, Ready(), "old exact amount cannot authorize later growth");
            twin = factory.Load(game.Save());
            var water = current.Single(row => row.Resource == Resource(game, "ports.water"));
            var cause = new Code("proof.replenishment-consumption");
            ResourceConversionIntent Consume() => new ResourceConversionIntent(owner, new PlayerId(0), new[] {
                new ResourceConversionInput(ResourceFlowKind.Consumed, increased, half, false), new ResourceConversionInput(ResourceFlowKind.Consumed, water, water.Amount, false) },
                Array.Empty<ResourceProductionIntent>(), cause);
            Order(c, game, new ConversionProbeCommand { Intent = Consume() }); Order(c, twin, new ConversionProbeCommand { Intent = Consume() });
            c.Equal(game.Hash(), twin.Hash(), "saved actual receipt consumes only replenishment and retains original charge");
            var replenishable = Claims(game).Single(row => row.Token.Equals(ore.Token));
            c.Equal(Fix.FromInt(2), replenishable.Amount, "same original native hold survives exact partial consumption");
            c.Equal(Fix.Parse("23.5"), Stock(game, "ports.ore", "ports.source"), "only consumed draw reduces physical stock");
            twin = factory.Load(game.Save());
            ResourceClaimIntent[] Replenish() => new[] { Grow(replenishable, half), Fresh(game, owner, source, "ports.water", half) };
            Order(c, game, new ClaimProbeCommand { Intents = Replenish() }); Order(c, twin, new ClaimProbeCommand { Intents = Replenish() });
            c.Equal(game.Hash(), twin.Hash(), "saved surviving original hold replenishes with native fresh feed");
            c.Equal(Fix.Parse("2.5"), Claims(game).Single(row => row.Token.Equals(ore.Token)).Amount, "later replenishment still uses the original token");
            Refuse(c, game, Replenish(), "spent full original metadata is not receipt authority");
            twin = factory.Load(game.Save());
            var holds = Claims(game).Where(row => row.Owner.Equals(owner)).Concat(Claims(game, "ports.sink-entity")).ToArray();
            Order(c, game, new ClaimProbeCommand { Release = true, Receipts = holds });
            Order(c, twin, new ClaimProbeCommand { Release = true, Receipts = holds });
            c.Equal(game.Hash(), twin.Hash(), "complete mixed release retires actual claims without resource refunds");
            c.Equal(1, Claims(game).Count, "only actual contending owner remains");
            c.Equal(Fix.Parse("23.5"), Stock(game, "ports.ore", "ports.source"), "release preserves true post-consumption physical stock");
            var history = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
            c.Equal(new BigInteger(half.Raw), history.Totals(ore.Resource).ConsumedRaw, "actual receipt consumption history counts only the draw");
            c.Equal(new BigInteger(Fix.One.Raw), history.Totals(water.Resource).ConsumedRaw, "native liquid consumption is not merged into mass");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success, "mixed acquire/consume/replenish player replay verifies: " + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "replay retains actual tokens, contention and disposition");
        }
    }
}
