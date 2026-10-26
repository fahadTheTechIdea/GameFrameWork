using System;
using System.Linq;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static partial class AtomicHaulTests
    {
        private const string OperationCase = "Haul operation admission: compound supplier claims and cancellation cannot checkpoint or advance time";

        // Named negative provider protocol probes. Existing finite stock/books and native logistics do all work.
        private static void OperationAdmission(Check c)
        {
            var setup = PaidSupplyFixture(); var factory = setup.Factory;
            byte[] opening = setup.Game.Save();
            foreach (bool ending in new[] { false, true })
            foreach (bool after in new[] { false, true })
            foreach (bool restored in new[] { false, true })
            foreach (bool handled in new[] { true, false })
            foreach (string operation in new[] { "hash", "save", "world", "update", "acquire" })
            {
                var game = restored ? factory.Load(opening) : PaidSupplyFixture().Game;
                var world = game.World; var source = world.Entities.Get(setup.Source.Id);
                var destination = world.Entities.Get(setup.Destination.Id); var resource = world.Content.ResourceId("steel");
                var supplier = source.Get<FiniteSupplier>(); var haul = world.Services.Get<IHaulExecution>();
                Func<int> acquire = () => haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(20), 100, true);
                int id = ending ? acquire() : 0;
                if (ending && id == 0) throw new InvalidOperationException("existing paid haul setup refused");
                byte[] original = game.Save(); var isolated = factory.Load(original); ulong isolatedHash = isolated.Hash();
                long time = game.Clock.StepIndex, pending = game.Clock.PendingTicks; Fix alpha = game.Clock.Alpha;
                Fix cash = world.Services.Get<IEconomy>().Cash(new PlayerId(0)); int updated = 0, calls = 0;
                game.Updated += _ => updated++;
                Exception admissionError = null, commitError = null; bool escaped = false;
                string context = "ending=" + ending + "/after=" + after + "/restored=" + restored + "/handled=" + handled + "/" + operation;
                Action probe = () => {
                    calls++;
                    c.Equal(ending != after ? Fix.FromInt(20) : Fix.Zero, source.Get<IInventory>().ReservedOut(resource),
                        "hook surrounds the actual original supply-book delta: " + context);
                    c.That(haul.OperationInProgress && !world.Services.Get<IResourceTransaction>().OperationInProgress,
                        "actual logistics owner protects the compound job outside any resource transaction: " + context);
                    var nested = world.Services.Get<IResourceTransaction>().CommitMovement(default);
                    c.That(!nested.Committed && haul.OperationInProgress && game.Fault == null,
                        "nested resource refusal cannot release the enclosing logistics operation: " + context);
                    try {
                        if (operation == "hash") game.Hash();
                        else if (operation == "save") game.Save();
                        else if (operation == "world") world.Step();
                        else if (operation == "update") game.Update();
                        else acquire();
                        escaped = true;
                    }
                    catch (Exception error) { admissionError = error; if (!handled) throw; }
                };
                if (ending) { if (after) supplier.AfterSupplyRelease = probe; else supplier.BeforeSupplyRelease = probe; }
                else { if (after) supplier.AfterSupplyClaim = probe; else supplier.BeforeSupplyClaim = probe; }
                int acquired = 0;
                try {
                    if (ending) {
                        var job = world.Services.Get<WorkBoard>().Job(id);
                        world.Services.Get<IWorkerControl>().SetHeld(world, job.Owner, job.Worker, true);
                    }
                    else acquired = acquire();
                }
                catch (Exception error) { commitError = error; }
                supplier.BeforeSupplyClaim = null; supplier.AfterSupplyClaim = null;
                supplier.BeforeSupplyRelease = null; supplier.AfterSupplyRelease = null;
                c.That(calls == 1 && !escaped && admissionError is InvalidOperationException && admissionError.Message.Contains("operation"),
                    "unfinished compound logistics rejects checkpoint/time/reentrant acquisition: " + context);
                if (calls != 1 || escaped || !(admissionError is InvalidOperationException) || !admissionError.Message.Contains("operation"))
                    throw new InvalidOperationException("compound logistics escaped its original owner: " + context + ", escaped=" + escaped + ", error=" + admissionError?.Message);
                c.That(game.Clock.StepIndex == time && game.Clock.PendingTicks == pending && game.Clock.Alpha == alpha && updated == 0,
                    "denied reentry changes no time budget or update publication: " + context);
                c.Equal(cash, world.Services.Get<IEconomy>().Cash(new PlayerId(0)), "claims/cancellation never perform financial settlement: " + context);
                if (handled) {
                    c.That(commitError == null && game.Fault == null && (ending || acquired != 0), "handled refusal permits original operation: " + context);
                    c.That(!haul.OperationInProgress, "completed logistics releases its transient permission: " + context);
                    c.Equal(ending ? Fix.Zero : Fix.FromInt(20), source.Get<IInventory>().ReservedOut(resource), "original source claim finishes exactly once: " + context);
                    c.Equal(ending ? Fix.Zero : Fix.FromInt(20), destination.Get<IInventory>().ReservedIn(resource), "original destination claim finishes exactly once: " + context);
                    var twin = factory.Load(original);
                    if (ending) { var job = twin.World.Services.Get<WorkBoard>().Job(id); twin.World.Services.Get<IWorkerControl>().SetHeld(twin.World, job.Owner, job.Worker, true); }
                    else twin.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(20), 100, true);
                    c.Equal(game.Hash(), twin.Hash(), "caught pure refusal changes no job/carrier/receipt IDs: " + context);
                    c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed operation admits durable native state: " + context);
                    game.Update(); twin.Update(); c.That(game.Fault == null && twin.Fault == null, "normal simulation resumes after operation: " + context);
                    c.Equal(game.Hash(), twin.Hash(), "original physical workflow continues identically: " + context);
                }
                else {
                    c.That(ReferenceEquals(commitError, admissionError) && game.Fault != null, "propagated failure latches original cause without success: " + context);
                    c.Equal(ending != after ? Fix.FromInt(20) : Fix.Zero, source.Get<IInventory>().ReservedOut(resource), "terminal prefix remains actual, never rolled back: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "failed compound operation cannot certify a resumable save: " + context);
                }
                c.Equal(isolatedHash, isolated.Hash(), "another session remains isolated from transient operation permission: " + context);
            }
            foreach (bool ending in new[] { false, true })
            {
                var broken = PaidSupplyFixture(); var game = broken.Game; var world = game.World;
                var supplier = broken.Source.Get<FiniteSupplier>(); var haul = world.Services.Get<IHaulExecution>();
                var resource = world.Content.ResourceId("steel");
                Func<int> acquire = () => haul.AcquireHaul(new PlayerId(0), broken.Source.Id, broken.Destination.Id, resource, Fix.FromInt(20), 100, true);
                int id = ending ? acquire() : 0;
                var owner = ending ? world.Services.Get<WorkBoard>().Job(id).ReservationOwner : default;
                if (ending) supplier.AfterSupplyRelease = () => supplier.ReserveSupply(owner, resource, Fix.FromInt(20));
                else supplier.AfterSupplyClaim = () => {
                    ResourceReservation claim = supplier.Reservations.Single();
                    broken.Source.Get<IInventory>().ReleaseOut(claim.Owner, claim.Token);
                };
                Exception failure = null;
                try {
                    if (ending) { var job = world.Services.Get<WorkBoard>().Job(id); world.Services.Get<IWorkerControl>().SetHeld(world, job.Owner, job.Worker, true); }
                    else acquire();
                }
                catch (Exception error) { failure = error; }
                supplier.AfterSupplyClaim = null; supplier.AfterSupplyRelease = null;
                c.That(failure is InvalidOperationException && game.Fault != null, "malformed provider cannot publish a completed compound claim: ending=" + ending);
                c.Equal(ending ? Fix.FromInt(20) : Fix.Zero, broken.Source.Get<IInventory>().ReservedOut(resource), "invalid returned/released claim retains its truthful terminal prefix");
                c.Equal(Fix.FromInt(25), broken.Source.Get<IInventory>().Amount(resource), "claim violation never spends or manufactures source stock");
                c.Throws<InvalidOperationException>(() => game.Save(), "invalid compound claim cannot become a durable successful job");
            }
        }
    }
}
