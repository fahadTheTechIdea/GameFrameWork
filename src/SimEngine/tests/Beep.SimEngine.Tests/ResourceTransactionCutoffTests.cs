using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Kernel fault-boundary proof. No host gameplay, additional inventory or protocol implementation.
    internal static class ResourceTransactionCutoffTests
    {
        internal const string Case = "Resource transaction cutoff: every contract entry stops before caller or provider work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private interface IListProbe { int Touches { get; } }
        private sealed class StoppedList<T> : IReadOnlyList<T>, IListProbe
        {
            public int Touches { get; private set; }
            private InvalidOperationException Access() { Touches++; return new InvalidOperationException("proof.caller-list-touched-after-fault"); }
            public int Count => throw Access();
            public T this[int index] => throw Access();
            public IEnumerator<T> GetEnumerator() => throw Access();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        internal static object[] Arguments(MethodInfo method, bool poison)
            => method.GetParameters().Select(parameter => {
                Type type = parameter.ParameterType;
                if (poison && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
                    return Activator.CreateInstance(typeof(StoppedList<>).MakeGenericType(type.GetGenericArguments()), true);
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }).ToArray();
        private static void CheckCall(Check c, IResourceTransaction transactions, SimGame game, MethodInfo method, bool poison)
        {
            object[] arguments = Arguments(method, poison); var first = game.Fault; Exception thrown = null;
            try { method.Invoke(transactions, arguments); }
            catch (TargetInvocationException error) { thrown = error.InnerException; }
            string label = method.Name + (poison ? "/caller-lists" : "/default");
            c.That(thrown is InvalidOperationException && thrown.Message.StartsWith("game update stopped:", StringComparison.Ordinal),
                "original terminal guard precedes all API work " + label);
            c.Equal(0, arguments.OfType<IListProbe>().Sum(list => list.Touches), "caller collections untouched " + label);
            c.That(ReferenceEquals(first, game.Fault), "API preserves the same first fault " + label);
        }
        private static void Verify(Check c, bool restored)
        {
            var factory = ResourceLifecycleCommitTests.Factory("clock");
            var game = ConstructionPlanTests.NewPoints(factory);
            var other = ConstructionPlanTests.NewPoints(factory); var reference = ConstructionPlanTests.NewPoints(factory);
            ResourceLifecycleCommitTests.MakeRoom(game); ResourceLifecycleCommitTests.MakeRoom(other); ResourceLifecycleCommitTests.MakeRoom(reference);
            var entity = ResourceLifecycleCommitTests.Endpoint(game); var inventory = entity.Get<IInventory>();
            var resource = game.World.Content.ResourceId("supply"); var owner = new ReservationOwner("proof.terminal-held", 1);
            var bill = new Bill(new[] { new ResourceAmount(resource, Fix.One) });
            var intents = new[] {
                new ResourceClaimIntent(ReservationDirection.Outbound, owner, inventory.Endpoint, bill),
                new ResourceClaimIntent(ReservationDirection.Inbound, owner, inventory.Endpoint, bill) };
            var acquired = game.World.Services.Get<IResourceTransaction>().AcquireClaims(entity.Owner, intents, Array.Empty<ResourceReservation>());
            c.That(acquired.Committed && acquired.Receipts.Count == 2, "healthy actual mixed receipts acquired before fault");
            if (restored) game = factory.Load(game.Save());
            entity = ResourceLifecycleCommitTests.Endpoint(game); inventory = entity.Get<IInventory>();
            var transactions = game.World.Services.Get<IResourceTransaction>();
            var before = inventory.Reservations.ToArray(); long time = game.Clock.StepIndex;
            game.Clock.Pause();
            c.Throws<InvalidOperationException>(() => transactions.CommitInventory(ResourceLifecycleCommitTests.Intent(game, false)), "real faulty commit stops the session");
            var first = game.Fault; c.That(first != null, "original first fault actually exists");
            var methods = typeof(IResourceTransaction).GetInterfaces().Concat(new[] { typeof(IResourceTransaction) })
                .SelectMany(contract => contract.GetMethods()).Distinct()
                .OrderBy(method => method.Name, StringComparer.Ordinal).ToArray();
            c.That(methods.Length > 0, "transaction contract methods discovered");
            foreach (var method in methods) { CheckCall(c, transactions, game, method, false); CheckCall(c, transactions, game, method, true); }
            c.Throws<InvalidOperationException>(() => transactions.ReadClaims(entity.Owner, inventory.Endpoint, owner), "valid claim read cannot bypass stopped session");
            var release = new ResourceClaimReleaseIntent(entity.Owner, before, before, new[] { inventory.Endpoint }, ResourceClaimReleasePolicy.Exact);
            c.Throws<InvalidOperationException>(() => transactions.ReleaseClaims(release), "valid held claims cannot be released after fault");
            c.Throws<InvalidOperationException>(() => transactions.AcquireClaims(entity.Owner, intents, Array.Empty<ResourceReservation>()), "valid new claims cannot be acquired after fault");
            var after = inventory.Reservations.ToArray();
            c.Equal(before.Length, after.Length, "stopped APIs preserve actual held books");
            c.That(before.Length == after.Length && before.Zip(after, (left, right) => left.Token.Equals(right.Token) &&
                left.Owner.Equals(right.Owner) && left.Resource == right.Resource && left.Amount == right.Amount && left.Direction == right.Direction).All(same => same),
                "every original token/owner/resource/amount/direction remains exact");
            c.That(!game.World.Events.HasPending, "stopped methods publish no success facts");
            for (int i = 0; i < 3; i++) {
                other.Update(); reference.Update(); game.Update();
                c.That(other.Fault == null && reference.Fault == null, "other actual sessions continue normally");
                c.Equal(time, game.Clock.StepIndex, "faulted session earns no time while others advance");
                c.Equal(time + i + 1, other.Clock.StepIndex, "unaffected session genuinely advances its original clock");
                c.Equal(other.Hash(), reference.Hash(), "interleaved other/reference sessions remain identical");
            }
            c.That(ReferenceEquals(first, game.Fault), "original failure remains authoritative after all APIs and Updates");
            c.Throws<InvalidOperationException>(() => game.Hash(), "faulted resources cannot claim healthy hash");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted resources cannot checkpoint");
        }
        private static void Run(Check c) { Verify(c, false); Verify(c, true); }
    }
}
