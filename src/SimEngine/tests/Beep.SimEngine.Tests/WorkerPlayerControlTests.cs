using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Only player intent, authoritative observations and bounded engine time drive these workflows.
    public static class WorkerPlayerControlTests
    {
        public static void Register() => TestRunner.Add("Workers: spatial-neutral held control, paid work and cargo preservation", Run);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "purchase-haul", Seed = 71 });
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Def.Id == id) return entity; throw new InvalidOperationException("missing " + id); }
        private static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInventory>().Amount(game.World.Content.ResourceId("supply"));
        private static BusinessAccountSnapshot Account(SimGame game)
        {
            var result = game.Seat(0).View.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var value);
            if (!result.IsNone) throw new InvalidOperationException("account refused: " + result); return value;
        }
        private static ActivityNodeView Node(SimGame game)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            foreach (var order in orders) if (order.Target == Find(game, "budget.site-a").Id)
            { var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, nodes); return nodes[0]; }
            throw new InvalidOperationException("missing build order");
        }
        private static void Update(SimGame game, SimGame twin = null, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (game.RunTicks(1) != 1) throw new InvalidOperationException("worker control Update fault: " + game.Fault);
                if (twin != null && twin.RunTicks(1) != 1) throw new InvalidOperationException("worker control twin fault: " + twin.Fault);
            }
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 120 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " timed out at " + game.Tick + ", orders=" + game.Seat(0).View.Feature<WorkView>().ActiveOrders(Find(game, "budget.site-a").Id));
        }
        private static void Held(Check c, SimGame game, string worker, bool held, SimGame twin = null)
        {
            EntityId id = Find(game, worker).Id; var command = new SetWorkerHeldCommand { Worker = id, Held = held };
            ulong hash = game.Hash();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerHeld(id, held), "view uses current spatial-neutral admission");
            c.Equal(Code.None, game.Seat(0).Preview(command), "player command uses the exact same current admission");
            c.Equal(hash, game.Hash(), "held-control previews change no motion, work, resource or account state");
            game.Seat(0).Submit(command);
            if (twin != null) twin.Seat(0).Submit(new SetWorkerHeldCommand { Worker = Find(twin, worker).Id, Held = held }); Update(game, twin);
            c.Equal(held, Find(game, worker).Get<IWorker>().Held, "engine commits requested actual worker-held state");
        }
        private static void Post(SimGame game)
        { game.Seat(0).Submit(new PostOrderCommand { Plan = "budget.build", Target = Find(game, "budget.site-a").Id }); Update(game); }
        private static WorkPolicySettings Settings(bool interruptible)
            => new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect") }, null, interruptible, true, WorkAssignmentMode.All);
        private static void Run(Check c) { Admission(c); Physical(c, false); Physical(c, true); Cargo(c); }

        private static void Admission(Check c)
        {
            var factory = WorkBudgetTests.Factory(controls: true); var game = New(factory);
            var control = game.World.Services.Get<IWorkerControl>();
            c.That(ReferenceEquals(control, game.World.Services.Get<IWorkerDispatch>()), "player control and dispatch alias the same engine work owner");
            var view = game.Seat(0).View.Feature<WorkView>(); Entity stationary = Find(game, "control.stationary");
            c.That(stationary.Get<IMotion>() == null && stationary.Get<ISpatialPresence>() == null, "authored stationary worker needs no navigation or world geometry");
            ulong hash = game.Hash();
            c.Equal(StandardCodes.NotWorker, view.PreviewWorkerHeld(Find(game, "processor").Id, true), "nonworker refuses rather than gaining fake control");
            c.Equal(Codes.NotFound, view.PreviewWorkerHeld(new EntityId(99999), true), "missing worker has a typed refusal");
            c.Equal(Codes.AlreadyDone, view.PreviewWorkerHeld(stationary.Id, false), "release of unheld worker remains an explicit refusal");
            c.Equal(Codes.NotOwned, PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().PreviewWorkerHeld(stationary.Id, true), "foreign control preview cannot borrow another company's worker");
            c.Equal(hash, game.Hash(), "all control refusals are pure");
            Held(c, game, "control.stationary", true);
            c.Equal(new Code("purchase.blocked"), stationary.State, "entity Update settles stationary held worker's authored stopped state");
            c.Equal(Code.None, view.ReadHome(stationary.Id, out var home), "existing immutable worker view reports actual held facts");
            c.Equal(WorkerHomeState.Held, home.State, "held intent is not another parallel snapshot/store");
            Held(c, game, "control.stationary", true);
            var twin = factory.Load(game.Save()); Update(game, twin, 3);
            c.Equal(game.Hash(), twin.Hash(), "stationary held intent survives native save/continue");
            Held(c, game, "control.stationary", false, twin); c.Equal(game.Hash(), twin.Hash(), "stationary release uses the same engine owner after restore");
            c.That(!stationary.Get<IWorker>().HeldArrived && stationary.Get<IWorker>().Job == 0, "release invents neither arrival nor assignment");
            var command = new SetWorkerHeldCommand { Worker = stationary.Id, Held = true };
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); command.Sync(writer); writer.Flush(); byte[] bytes = stream.ToArray();
                var copy = new SetWorkerHeldCommand(); copy.Sync(new StateReader(bytes, 0, bytes.Length));
                c.Equal(command.Worker, copy.Worker, "current command stream retains worker identity");
                c.Equal(command.Held, copy.Held, "current command stream retains explicit held intent");
            }
        }
        private static void Physical(Check c, bool survival)
        {
            var factory = WorkBudgetTests.Factory(survival: survival, controls: true); var game = New(factory);
            Held(c, game, "budget.worker-a", true); Held(c, game, "budget.worker-b", true); Post(game); Update(game, count: 3);
            c.Equal(WorkProgress.Zero, Node(game).Progress, "holding all compatible machines leaves intent pending without fake work");
            c.Equal(0, Node(game).Costs.Count, "unassigned held intent creates no partial monetary promise");
            c.Equal(Fix.FromInt(8), Stock(game, "budget.site-a"), "held queued work consumes no materials");
            Held(c, game, "budget.worker-a", false);
            Reach(game, () => Node(game).Worker.IsValid, "released point-space worker is assigned");
            c.That(Find(game, "budget.worker-a").Get<IMobility>() == null, "real assigned machine is not a concrete tile-navigation vehicle");
            if (!survival) c.Equal(Fix.FromInt(80), Account(game).Reserved, "ordinary assignment acquires actual native budget");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Find(game, "budget.worker-a").Id, Settings = Settings(false) }).Accepted,
                "player configures authored worker policy through its existing engine contract");
            ulong hash = game.Hash(); var hold = new SetWorkerHeldCommand { Worker = Find(game, "budget.worker-a").Id, Held = true };
            c.Equal(WorkPolicyCodes.NotInterruptible, game.Seat(0).Preview(hold), "noninterruptible actual work refuses player hold");
            c.Equal(WorkPolicyCodes.NotInterruptible, game.Seat(0).View.Feature<WorkView>().PreviewWorkerHeld(hold.Worker, true), "policy refusal is identical in management preview");
            c.Equal(hash, game.Hash(), "noninterruptible refusal releases no resource or budget");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = hold.Worker, Settings = Settings(true) }).Accepted, "player explicitly restores interruptibility");
            Fix position = Find(game, "budget.worker-a").Get<PointPresence>().Position;
            Held(c, game, "budget.worker-a", true);
            c.Equal(position, Find(game, "budget.worker-a").Get<PointPresence>().Position, "hold stops actual motion before another movement update");
            c.Equal(MoveStatus.Idle, Find(game, "budget.worker-a").Get<IMotion>().Status, "manual hold owns no lingering route");
            c.Equal(WorkProgress.Zero, Node(game).Progress, "interrupted travel earns no productive work");
            c.Equal(0, Node(game).Costs.Count, "manual interruption releases all unpaid cost bindings");
            if (!survival) { c.Equal(Fix.FromInt(100), Account(game).Balance, "unpaid hold releases promises without fabricating a refund"); c.Equal(Fix.Zero, Account(game).Reserved, "native account has no orphan hold after manual interruption"); }
            else c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IMonetaryBudgets>(), "generic worker control never requires optional economics");
            var twin = factory.Load(game.Save()); Update(game, twin, 3);
            c.Equal(game.Hash(), twin.Hash(), "held traveling work checkpoint continues with physical position and intent unchanged");
            c.Equal(position, Find(game, "budget.worker-a").Get<PointPresence>().Position, "held updates never teleport or move the machine");
            Held(c, game, "budget.worker-a", false, twin);
            Reach(game, () => Node(game).Progress.WholeTicks > 0, "released machine reaches site and earns productive work", twin);
            var progress = Node(game).Progress; c.Equal(Fix.FromInt(6), Stock(game, "budget.site-a"), "physical resumed work consumes authored building materials once");
            if (!survival) c.Equal(Fix.FromInt(20), Account(game).Balance, "actual resumed work pays80 only at productive progress");
            Held(c, game, "budget.worker-a", true, twin); Update(game, twin, 2);
            c.Equal(progress, Node(game).Progress, "paid interruption preserves but never advances actual progress");
            c.Equal(Fix.FromInt(6), Stock(game, "budget.site-a"), "paid hold returns neither spent material nor fake replacements");
            if (!survival) c.Equal(Fix.FromInt(20), Account(game).Balance, "paid interruption never refunds spent money");
            twin = factory.Load(game.Save()); Held(c, game, "budget.worker-a", false, twin);
            Reach(game, () => game.Seat(0).View.Feature<WorkView>().ActiveOrders(Find(game, "budget.site-a").Id) == 0, "paid physical work resumes and completes", twin);
            c.Equal(Fix.FromInt(6), Stock(game, "budget.site-a"), "second assignment does not consume startup materials twice");
            if (!survival) { c.Equal(Fix.FromInt(20), Account(game).Balance, "second assignment never pays already-spent costs twice"); c.Equal(Fix.Zero, Account(game).Reserved, "completed resumed work leaves no monetary promises"); }
            Update(game, twin, 3); c.Equal(game.Hash(), twin.Hash(), "held unpaid/paid/survival continuations remain deterministic");
        }
        private static void Cargo(Check c)
        {
            var factory = WorkBudgetTests.Factory(controls: true); var game = New(factory);
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id,
                Resource = game.World.Content.ResourceId("supply"), Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,
                Fix.FromInt(8), Fix.FromInt(8), Fix.FromInt(8), 100, LogisticsReserveAccess.Protected)) }).Accepted,
                "stock-policy intent lets engine physically dispatch a purchasing carrier");
            Reach(game, () => Stock(game, "machine").IsPositive, "carrier physically earns and loads a purchase chunk");
            Fix cargo = Stock(game, "machine"), balance = Account(game).Balance;
            c.That(cargo < Fix.FromInt(8) && balance < Fix.FromInt(100), "actual paid cargo exists before full loading");
            Held(c, game, "machine", true);
            c.Equal(cargo, Stock(game, "machine"), "interrupting a haul retains actual purchased cargo rather than destroying it");
            c.Equal(balance, Account(game).Balance, "holding purchased cargo neither refunds nor purchases it again");
            c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "actual haul owner frees the interrupted worker");
            var twin = factory.Load(game.Save()); Update(game, twin, 4);
            c.Equal(cargo, Stock(game, "machine"), "held saved cargo waits physically in its actual inventory");
            c.Equal(balance, Account(game).Balance, "held carrier cannot silently execute another purchase");
            c.Equal(game.Hash(), twin.Hash(), "paid cargo and released commitments continue identically");
            Held(c, game, "machine", false, twin); Update(game, twin, 3);
            c.Equal(balance, Account(game).Balance, "returning a paid carrier to dispatch does not repurchase cargo during travel");
            c.Equal(game.Hash(), twin.Hash(), "released residual cargo uses existing deterministic recovery owners");
        }
    }
}
