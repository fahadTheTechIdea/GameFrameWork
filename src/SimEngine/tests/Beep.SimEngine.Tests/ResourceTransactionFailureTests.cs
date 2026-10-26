using System;
using System.Linq;
using System.Reflection;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Direct engine contract proof; original providers/books own every irreversible prefix.
    internal static class ResourceTransactionFailureTests
    {
        internal const string Case = "Resource transaction failures: direct provider violations are terminal at the original owner";
        internal const string ResolveCase = "Resource endpoint resolution: conversion admission rejects resolver stock and clock mutation including missing endpoints";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(ResolveCase, Resolution);
        }
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReservationOwner Owner = new ReservationOwner("proof.direct-failure", 42);
        // Negative protocol probes, not player gameplay: the existing pool/transaction owns all state.
        private static void Resolution(Check c)
        {
            var factory = EndpointGame.Factory();
            foreach (bool commit in new[] { false, true })
            foreach (string mode in new[] { "healthy", "missing", "stock", "clock", "missing-stock", "missing-clock", "throw", "stock-throw" })
            {
                var primary = factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 101 });
                ResourceClaimTests.Seed(primary);
                var initialPools = primary.World.Services.Get<EndpointPools>();
                var steel = primary.World.Content.ResourceId("steel");
                var bill = new Bill(new[] { new ResourceAmount(steel, Fix.One) });
                var original = primary.World.Services.Get<IResourceTransaction>().AcquireClaims(Company,
                    new[] { new ResourceClaimIntent(ReservationDirection.Outbound, Owner, initialPools.Get(1).Endpoint, bill) },
                    Array.Empty<ResourceReservation>());
                if (!original.Committed) throw new InvalidOperationException("resolution probe needs an actual original receipt");
                // Every negative branch starts from an actual saved claim, not a fabricated token.
                var game = factory.Load(primary.Save()); var isolated = factory.Load(primary.Save());
                ulong untouched = isolated.Hash();
                var pools = game.World.Services.Get<EndpointPools>(); var tx = game.World.Services.Get<IResourceTransaction>();
                var destination = new ResourceEndpointId(pools.EndpointScope, mode.StartsWith("missing") ? 99 : 2);
                var intent = new ResourceConversionIntent(Owner, Company,
                    new[] { new ResourceConversionInput(ResourceFlowKind.Destroyed, original.Receipts.Single(), Fix.One, false) },
                    new[] { new ResourceProductionIntent(destination, bill) }, new Code("proof.resolver"));
                long step = game.Clock.StepIndex; ulong before = game.Hash();
                var history = game.Seat(0).View.Feature<Views.ResourceFlowView>().Read(Company, 0);
                Fix sourceBefore = pools.Get(1).Amount(steel), targetBefore = pools.Get(2).Amount(steel);
                int observed = 0; var originalError = new InvalidOperationException("proof.original-resolver-error");
                pools.ResolveObservation = endpoint => {
                    if (endpoint != destination) return;
                    observed++;
                    if (mode.Contains("stock")) pools.Get(2).Take(steel, Fix.One);
                    if (mode.Contains("clock")) game.Clock.Pause();
                    if (mode.Contains("throw")) throw originalError;
                };
                Exception thrown = null; ResourceInventoryResult result = default;
                try { result = commit ? tx.CommitConversion(intent, null) : tx.EvaluateConversion(intent, null); }
                catch (Exception error) { thrown = error; }
                string context = mode + "/commit=" + commit;
                c.Equal(1, observed, "selected resolver invoked exactly once, including absence: " + context);
                if (mode == "healthy" || mode == "missing")
                {
                    c.That(thrown == null && game.Fault == null, "valid resolution or absence stays healthy: " + context);
                    c.Equal(mode == "missing" ? ResourceTransferStatus.MissingEndpoint : commit ? ResourceTransferStatus.Committed : ResourceTransferStatus.Ready,
                        result.Status, "ordinary result remains typed: " + context);
                    if (mode == "missing" || !commit) c.Equal(before, game.Hash(), "healthy read/refusal changes no stock claims clock or history: " + context);
                    else {
                        c.Equal(sourceBefore - Fix.One, pools.Get(1).Amount(steel), "healthy commit destroys the exact original receipt");
                        c.Equal(targetBefore + Fix.One, pools.Get(2).Amount(steel), "healthy commit produces only its declared output");
                        game.Update(); // The original event pipeline must settle before a checkpoint.
                        c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "healthy conversion still saves canonically");
                    }
                }
                else
                {
                    c.That(thrown is InvalidOperationException && game.Fault != null, "mutating or throwing resolver is terminal before admission: " + context);
                    if (mode.Contains("throw")) c.That(ReferenceEquals(originalError, thrown), "original callback exception is preserved: " + context);
                    if (game.Fault != null) {
                        var first = game.Fault; int calls = pools.ResolveCalls;
                        c.Equal(sourceBefore, pools.Get(1).Amount(steel), "no claimed input consumption follows resolver fault: " + context);
                        c.Equal(targetBefore - (mode.Contains("stock") ? Fix.One : Fix.Zero), pools.Get(2).Amount(steel), "actual illegal mutation is not refunded or converted: " + context);
                        c.Equal(1, pools.Get(1).Reservations.Count, "original input receipt survives without an invented release: " + context);
                        c.Equal(0, pools.Get(2).Reservations.Count, "no receiving receipt is acquired after failed resolution: " + context);
                        c.That(!game.World.Events.HasPending, "no success facts escape failed resolution: " + context);
                        c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<Views.ResourceFlowView>().Read(Company, 0), "faulted history cannot look healthy: " + context);
                        c.Throws<InvalidOperationException>(() => tx.EvaluateConversion(intent, null), "caught resolver error cannot resume transactions: " + context);
                        c.Equal(calls, pools.ResolveCalls, "first-fault cutoff does not call the resolver again: " + context);
                        c.Throws<InvalidOperationException>(() => game.Save(), "no save after malformed resolution: " + context);
                        c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy hash after malformed resolution: " + context);
                        game.Update(); c.Equal(step, game.Clock.StepIndex, "failed resolver earns no game time: " + context);
                        c.That(ReferenceEquals(first, game.Fault), "first resolver failure retains precedence: " + context);
                        c.That(!history.Entries.Any(row => row.Item.Cause == new Code("proof.resolver")), "detached healthy history acquires no failed conversion prefix: " + context);
                    }
                }
                c.Equal(untouched, isolated.Hash(), "resolver probe leaves interleaved session unchanged: " + context);
            }
        }
        private static void HealthyRefusals(Check c)
        {
            var game = EndpointGame.Factory().NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 101 });
            var tx = game.World.Services.Get<IResourceTransaction>(); ulong before = game.Hash(); bool pending = game.World.Events.HasPending;
            foreach (var method in typeof(IResourceTransaction).GetMethods()) {
                object result = null; Exception thrown = null;
                try { result = method.Invoke(tx, ResourceTransactionCutoffTests.Arguments(method, false)); }
                catch (TargetInvocationException error) { thrown = error.InnerException; }
                object status = result?.GetType().GetProperty("Status")?.GetValue(result);
                c.That(thrown == null && status is ResourceTransferStatus value &&
                    (value == ResourceTransferStatus.InvalidRequest || value == ResourceTransferStatus.InvalidReceipt),
                    "ordinary invalid request remains a typed refusal " + method.Name);
                c.That(game.Fault == null, "ordinary refusal is not a provider failure " + method.Name);
                c.That(game.Fault == null && before == game.Hash() && pending == game.World.Events.HasPending,
                    "ordinary refusal changes no state or facts " + method.Name);
            }
        }
        private static void Verify(Check c, string operation, bool restored)
        {
            var factory = EndpointGame.Factory();
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 101 });
            SimGame other = factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 101 });
            ResourceClaimTests.Seed(game); ResourceClaimTests.Seed(other);
            var tx = game.World.Services.Get<IResourceTransaction>(); var pools = game.World.Services.Get<EndpointPools>();
            var steel = game.World.Content.ResourceId("steel"); var bill = new Bill(new[] { new ResourceAmount(steel, Fix.One) });
            var claims = new[] {
                new ResourceClaimIntent(ReservationDirection.Outbound, Owner, pools.Get(1).Endpoint, bill),
                new ResourceClaimIntent(ReservationDirection.Outbound, Owner, pools.Get(2).Endpoint, bill) };
            ResourceReservation[] held = Array.Empty<ResourceReservation>();
            if (operation == "consume" || operation == "release" || operation == "conversion") {
                var acquired = tx.AcquireClaims(Company, operation == "conversion" ? new[] { claims[0] } : claims, Array.Empty<ResourceReservation>());
                if (!acquired.Committed) throw new InvalidOperationException("healthy direct fault fixture refused claims");
                held = acquired.Receipts.ToArray();
            }
            if (restored) game = factory.Load(game.Save());
            tx = game.World.Services.Get<IResourceTransaction>(); pools = game.World.Services.Get<EndpointPools>();
            ulong isolated = other.Hash(); long time = game.Clock.StepIndex;
            Fix stock1 = pools.Get(1).Amount(steel), stock2 = pools.Get(2).Amount(steel);
            int records1 = pools.Get(1).Reservations.Count, records2 = pools.Get(2).Reservations.Count;
            pools.ClaimFault = operation == "query" ? ClaimFaultMode.MutatingCheck : operation == "acquire" ? ClaimFaultMode.ExtraAcquire :
                operation == "consume" ? ClaimFaultMode.ShortTake : operation == "release" ? ClaimFaultMode.IgnoreRelease : ClaimFaultMode.ShortReceive;
            var movement = new ResourceMovementIntent(Owner, Company, pools.Get(1).Endpoint, pools.Get(2).Endpoint, bill,
                Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
            var conversion = new ResourceConversionIntent(Owner, Company,
                held.Select(receipt => new ResourceConversionInput(ResourceFlowKind.Consumed, receipt, Fix.One, true)),
                new[] { new ResourceProductionIntent(pools.Get(2).Endpoint, bill) }, new Code("proof.direct-failure"));
            Exception thrown = null;
            try {
                switch (operation) {
                    case "query": tx.EvaluateClaims(Company, claims, Array.Empty<ResourceReservation>()); break;
                    case "acquire": tx.AcquireClaims(Company, claims, Array.Empty<ResourceReservation>()); break;
                    case "consume": tx.ConsumeClaims(Company, held, new Code("proof.direct-failure"), Array.Empty<ResourceReservation>()); break;
                    case "release": tx.ReleaseClaims(new ResourceClaimReleaseIntent(Company, held, held,
                        new[] { pools.Get(1).Endpoint, pools.Get(2).Endpoint }, ResourceClaimReleasePolicy.Exact)); break;
                    case "movement": tx.CommitMovement(movement); break;
                    case "conversion": tx.CommitConversion(conversion, null); break;
                    default: throw new InvalidOperationException("unknown direct fault operation");
                }
            } catch (Exception error) { thrown = error; }
            string label = operation + (restored ? "/restored" : "/new");
            c.That(thrown is InvalidOperationException, "actual malformed callback is rejected " + label);
            c.That(game.Fault != null && thrown != null && game.Fault.Message.Contains(thrown.Message), "original owner latches the actual first error " + label);
            bool actualPrefix = operation switch {
                "query" => pools.Get(2).Reservations.Count > records2,
                "acquire" => pools.Get(1).Reservations.Count > records1 && pools.Get(2).Reservations.Count > records2,
                "consume" => pools.Get(1).Amount(steel) < stock1 && pools.Get(2).Amount(steel) < stock2,
                "release" => pools.Get(1).Reservations.Count < records1 && pools.Get(2).Reservations.Count == records2,
                _ => pools.Get(1).Amount(steel) == stock1 - Fix.One && pools.Get(2).Amount(steel) == stock2 + Fix.One
            };
            c.That(actualPrefix, "actual violating provider changed its original stock/book; no rollback fiction " + label);
            c.That(!game.World.Events.HasPending, "no unverified success prefix " + label);
            var first = game.Fault; int resolves = pools.ResolveCalls;
            c.Throws<InvalidOperationException>(() => tx.EvaluateClaims(Company, claims, Array.Empty<ResourceReservation>()), "direct caller cannot continue after catching error " + label);
            c.Equal(resolves, pools.ResolveCalls, "provider not revisited after first fault " + label);
            c.Throws<InvalidOperationException>(() => game.Save(), "partial direct mutation cannot save " + label);
            c.Throws<InvalidOperationException>(() => game.Hash(), "partial direct mutation cannot hash healthy " + label);
            game.Update(); c.Equal(time, game.Clock.StepIndex, "terminal direct error earns no time " + label);
            c.That(ReferenceEquals(first, game.Fault), "first failure retained after public Update " + label);
            c.Equal(isolated, other.Hash(), "other actual session untouched " + label);
        }
        private static void Run(Check c)
        {
            HealthyRefusals(c);
            var map = typeof(ResourceTransactions).GetInterfaceMap(typeof(IResourceTransaction));
            foreach (var method in map.TargetMethods)
                c.That(method.GetMethodBody().ExceptionHandlingClauses.Any(clause => clause.Flags == ExceptionHandlingClauseOptions.Clause && clause.CatchType == typeof(Exception)),
                    "original interface implementation owns its exception boundary " + method.Name);
            foreach (string operation in new[] { "query", "acquire", "consume", "release", "movement", "conversion" }) {
                Verify(c, operation, false); Verify(c, operation, true);
            }
        }
    }
}
