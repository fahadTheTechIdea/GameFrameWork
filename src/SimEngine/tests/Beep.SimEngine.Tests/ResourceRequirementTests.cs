using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.PhysicalRecoveryTestContent;

namespace Beep.SimEngine.Tests
{
    public static class ResourceRequirementTests
    {
        private static string Definitions => WorkerExecutionTestContent.Author(FrameworkProofContent.Read("resource-requirements.json"));
        private static readonly PlayerId Company = new PlayerId(0);
        public static void Register()
        {
            TestRunner.Add("Activity resources: authored scoped offers, native sets and private selection", Admission);
            TestRunner.Add("Activity resources: physical consumption, held tools, interruption and saved ownership", Physical);
        }
        private static ResourceId Resource(SimGame game, string id) => game.World.Content.ResourceId(id);
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static ResourceFlowPage History(SimGame game, long cursor = 0) => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, cursor);
        private static Entity Find(SimGame game, EntityId id) => game.World.Entities.Get(id);
        private static void Seed(SimGame game, Entity target)
        {
            ResourceId water = Resource(game, "water"); IInventory stock = target.Get<IInventory>();
            foreach (long identity in new[] { 1L, 5000000000L })
            {
                var destination = game.World.Services.Get<EndpointPools>().Get(identity);
                var owner = new ReservationOwner("proof.connection-fill", identity);
                var transfer = new ResourceTransferIntent(owner, Company, stock.ReserveOut(owner, water, Fix.One),
                    destination.ReserveIn(owner, water, Fix.One), water, Fix.One);
                if (!game.World.Services.Get<IResourceTransaction>().Commit(transfer).Committed) throw new InvalidOperationException("actual connected pool fill refused");
            }
            Tick(game);
        }
        private static Order Post(Check c, SimGame game, Entity target, string plan = "requirements.work")
        {
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = plan });
            c.That(result.Accepted, "player submits the ordinary engine activity command");
            return Board(game).Order(checked((int)result.Arg0));
        }
        private static void Reach(SimGame game, Func<bool> condition, string stage, params SimGame[] twins)
        {
            for (int tick = 0; tick < 500 && !condition(); tick++) { Tick(game); foreach (var twin in twins) Tick(twin); }
            if (condition()) return;
            var diagnostic = new List<string>();
            foreach (var order in Board(game).Orders) foreach (var node in order.Nodes)
                diagnostic.Add(order.Id + "/" + node.Id + ":" + node.State + ", job=" + node.JobId + ", progress=" + node.Progress.WholeTicks + ", " + node.Blocker);
            throw new InvalidOperationException(stage + " timeout: " + string.Join("; ", diagnostic));
        }
        private static List<WorkWorkerCandidate> Offers(Check c, SimGame game, Entity target, string plan = "requirements.work")
        {
            var offers = new List<WorkWorkerCandidate>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates(plan, "work", target.Id, offers), "private preview uses the engine selector");
            return offers;
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "requirements.target", 20, 20), builder = Spawn(game, "requirements.builder", 6, 20);
            Entity empty = Spawn(game, "requirements.empty-builder", 10, 20);
            Spawn(game, "requirements.builder", 26, 24, 1); Seed(game, target);
            ulong before = game.Hash(); var offers = Offers(c, game, target);
            c.Equal(builder.Id, offers[0].Worker, "actual tool holder wins over a nearer empty machine");
            c.That(offers[0].Result.Status == WorkRequirementStatus.Satisfied, "actual local and connected source set is ready");
            c.That(offers.Exists(x => x.Worker == empty.Id && x.Result.Status != WorkRequirementStatus.Satisfied), "missing tools are typed candidate refusal");
            c.That(offers.TrueForAll(x => Find(game, x.Worker).Owner == Company), "foreign machines do not leak in offers");
            c.Equal(3, offers[0].Result.Resources.Count, "one selected resource intent for each active expression occurrence");
            foreach (var choice in offers[0].Result.Resources)
            {
                c.That(choice.Path.Count == 3 && choice.Path[0] == "requirements.choice" && choice.Path[1] == "a.preferred", "selected resources retain exact named all/any occurrence paths");
                c.That(choice.Quantities.Count == 1, "selected bill is the sole authored native intent");
                if (choice.Requirement == "requirements.fluid")
                    c.Equal(new ResourceEndpointId("colony.pools", 1), choice.Endpoint, "equal-cost scoped sources use stable wide endpoint identity rather than declaration order");
                if (choice.Requirement == "requirements.tools") c.Equal(builder.Get<IInventory>().Endpoint, choice.Endpoint, "worker source is its real existing inventory");
            }
            var criteria = new List<WorkRequirementView>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("requirements.work", target.Id, criteria), "same evaluator provides per-entry source facts");
            int scoped = 0;
            foreach (var fact in criteria) if (fact.Requirement == "requirements.fluid")
            {
                scoped++;
                c.Equal(2, fact.Result.ResourceCandidates.Count, "foreign connected endpoint is omitted, not disclosed with private quantities");
                foreach (var source in fact.Result.ResourceCandidates)
                {
                    c.Equal(Fix.One, source.Availability[0].Available, "source observation reads actual native unpromised stock");
                    c.Equal(Fix.FromRatio(1, 2), source.Availability[0].Required, "fractional native fluid requirement is explicit");
                    c.Equal(new UnitId("unit.water"), source.Availability[0].Unit, "native unit comes from the sole catalog");
                }
            }
            c.Equal(2, scoped, "both any branches are evaluated, not silently skipped");
            var joint = Offers(c, game, target, "requirements.multisource");
            c.Equal(WorkRequirementStatus.Satisfied, joint[0].Result.Status, "joint selection finds a feasible alternative instead of falsely blocking individually ready leaves on the same scarce source");
            var endpoints = new HashSet<ResourceEndpointId>();
            foreach (var choice in joint[0].Result.Resources) if (choice.Endpoint.Scope == "colony.pools") endpoints.Add(choice.Endpoint);
            c.Equal(2, endpoints.Count, "different occurrences share available providers without overpromising the cheapest pool");
            c.Equal(before, game.Hash(), "worker/source/branch diagnostics change no stock/receipt/sequence/node/job state");
            c.Equal(0, builder.Get<IWorker>().Job, "pure source previews do not acquire a worker");
            c.Throws<NotSupportedException>(() => ((IList<WorkResourceChoice>)offers[0].Result.Resources).Clear(), "selected source facts are immutable");
            c.Throws<NotSupportedException>(() => ((IList<string>)offers[0].Result.Resources[0].Path).Clear(), "occurrence path is immutable");
            var foreign = new List<WorkWorkerCandidate>(offers);
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<WorkView>().PreviewWorkerCandidates("requirements.work", "work", target.Id, foreign), "foreign work/source preview is refused");
            c.Equal(0, foreign.Count, "private refusal clears prior source/worker offers");
            var conflict = Offers(c, game, target, "requirements.blocked");
            c.That(conflict.TrueForAll(x => x.Result.Status != WorkRequirementStatus.Satisfied), "complete all-group refuses aggregate native stock conflict even if every leaf individually fits");
            Order blocked = Post(c, game, target, "requirements.blocked");
            c.Equal(0, blocked.Node("work").Resources.Count, "failed complete resource admission acquires no partial claims");
            c.Equal(0, blocked.Node("work").JobId, "failed complete admission posts no physical machine job");
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(Resource(game, "steel")), "resource conflict consumes no native materials");
            c.Equal(0, target.Get<IInventory>().Reservations.Count, "resource conflict leaves all endpoint books unchanged");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "existing node codec owns source/receipt state");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "source/receipt codec breaks old saves explicitly");
            foreach (string invalid in new[]
            {
                Definitions.Replace("\"use\":\"hold\"", "\"use\":\"magic\""),
                Definitions.Replace("\"quantities\":{\"wrench\":1}", "\"quantities\":{}"),
                Definitions.Replace("\"quantities\":{\"wrench\":1}", "\"quantities\":{\"wrench\":0.5}"),
                Definitions.Replace("\"quantities\":{\"wrench\":1}", "\"quantities\":{\"missing\":1}"),
                Definitions.Replace("\"hours\":1", "\"hours\":0"),
                Definitions.Replace("\"roles\":[\"resource.builder\"]", "\"roles\":[]")
            }) c.Throws<ContentException>(() => New(Factory(invalid)), "invalid usage/empty/native/reference/instant/scoped authored work refuses before Update");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "waiting conflicted work and pure offers have one durable board");
            WorkStartTests.Admission(c);
            ContinuousWorkTests.Admission(c);
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "requirements.target", 20, 20), first = Spawn(game, "requirements.builder", 6, 20), second = Spawn(game, "requirements.builder", 4, 24);
            Seed(game, target); ResourceId steel = Resource(game, "steel"), wrench = Resource(game, "wrench"), water = Resource(game, "water");
            long cursor = History(game).Latest; Order order = Post(c, game, target); var node = order.Node("work"); Job job = Board(game).Job(node.JobId);
            c.Equal(first.Id, job.Worker, "closest compatible real worker is acquired with the complete source set");
            c.Equal(job.Id, first.Get<IWorker>().Job, "existing worker/job relation remains the only machine claim");
            c.Equal(3, node.Resources.Count, "existing node records actual selected resource links");
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(steel), "assignment reserves materials but does not consume remotely");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Available(steel), "actual target book protects its material promise during travel");
            c.Equal(Fix.One, first.Get<IInventory>().Amount(wrench), "held tool remains physical native stock");
            c.Equal(Fix.Zero, first.Get<IInventory>().Available(wrench), "owned tool cannot be consumed or assigned elsewhere");
            c.Equal(0, History(game, cursor).Entries.Count, "assignment invents no consumption history");
            var views = new List<ActivityNodeView>(); c.That(game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, views), "private view exposes actual source links and receipt facts");
            var old = views[0]; c.Throws<NotSupportedException>(() => ((IList<WorkResourceBinding>)old.Resources).Clear(), "actual binding snapshot is immutable");
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, views) && views.Count == 0, "actual tool/source links are company-private");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "travelling worker and native promises save together");
            Reach(game, () => node.Progress.WholeTicks >= 3, "first actual physical work", twin);
            c.Equal(game.Hash(), twin.Hash(), "travel and first consumption continue identically after save");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(steel), "materials consume exactly once only at reached Work");
            c.Equal(Fix.FromRatio(1, 2), game.World.Services.Get<EndpointPools>().Get(1).Amount(water), "connected native fluid consumes through its actual scoped owner");
            c.Equal(Fix.One, game.World.Services.Get<EndpointPools>().Get(5000000000).Amount(water), "unselected source is not spent");
            c.Equal(2, History(game, cursor).Entries.Count, "real target/scoped spending emits one fact per actual native line");
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), History(game, cursor).Totals(steel).ConsumedRaw, "material accounting is actual count use");
            c.Equal(BigInteger.Zero, History(game, cursor).Totals(wrench).ConsumedRaw, "tool hold is not fictitious consumption");
            c.That(node.Resources[0].Spent || node.Resources[1].Spent, "spent occurrence is an explicit durable node fact");
            c.That(!old.Resources[0].Spent && !old.Resources[1].Spent, "old detached snapshot does not follow later spending");
            WorkProgress progress = node.Progress;
            foreach (var current in new[] { game, twin })
                c.That(TestKit.Order(current, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = false }).Accepted, "player policy guard pauses the same engine activity");
            c.Equal(NodeState.Paused, node.State, "a different ready recipe does not override committed consumed inputs");
            c.Equal(progress, node.Progress, "pause preserves actual progress");
            c.Equal(Fix.Zero, first.Get<IInventory>().Available(wrench), "pause retains the owned tool promise");
            SimGame paused = factory.Load(game.Save()); c.Equal(game.Hash(), paused.Hash(), "paused live tool and spent materials retain the sole codec");
            for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); Tick(paused); }
            c.Equal(progress, node.Progress, "paused resource work cannot advance or consume again");
            foreach (var current in new[] { game, twin, paused })
                c.That(TestKit.Order(current, 0, new MoveVehicleCommand { Vehicle = first.Id, To = new TileCoord(12, 10) }).Accepted, "player interruption uses the normal movement command");
            c.Equal(Fix.One, first.Get<IInventory>().Available(wrench), "interruption releases only the old machine's unspent tool");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(steel), "interruption never recreates spent materials");
            c.Equal("a.preferred", node.Choices[0].Entry, "spent-work recipe branch remains committed through interruption");
            c.Equal(game.Hash(), paused.Hash(), "paused/interrupted twins retain identical claims, progress and branches");
            foreach (var current in new[] { game, twin, paused })
                c.That(TestKit.Order(current, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted, "player re-enables the original physical workflow");
            Reach(game, () => Board(game).Job(node.JobId)?.Worker == second.Id, "reselect second actual tool holder", twin, paused);
            c.Equal(Fix.Zero, second.Get<IInventory>().Available(wrench), "new worker's actual tool is protected before work resumes");
            c.Equal(progress, node.Progress, "reselection does not reset work or advance during new travel");
            Reach(game, () => order.State == OrderState.Done, "physical resource workflow completion", twin, paused);
            c.Equal(game.Hash(), twin.Hash(), "completion remains identical in the saved twin");
            c.Equal(game.Hash(), paused.Hash(), "paused and interrupted saved continuation matches completion");
            c.Equal(Fix.One, second.Get<IInventory>().Available(wrench), "completion releases the actual tool without destroying it");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(steel), "new worker does not charge the material bill again");
            c.Equal(2, History(game, cursor).Entries.Count, "pause/reselection/completion emit no duplicate consumption");
            c.Equal(2, node.Resources.Count, "terminal node retains only actual spent facts, not live claims");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed source facts and history save identically");
            Cancellation(c, factory);
            Timed(c, factory);
            LostAndRate(c, factory);
            SavedIntegrity(c, factory);
            ProviderFault(c, factory);
            JointSources(c, factory);
            WorkStartTests.Physical(c);
            ContinuousWorkTests.Physical(c);
        }
        private static void Cancellation(Check c, GameFactory factory)
        {
            foreach (bool afterWork in new[] { false, true })
            {
                SimGame game = New(factory); Entity target = Spawn(game, "requirements.target", 20, 20), worker = Spawn(game, "requirements.builder", 6, 20);
                Seed(game, target); Order order = Post(c, game, target); var node = order.Node("work"); ResourceId steel = Resource(game, "steel"), wrench = Resource(game, "wrench");
                if (afterWork) Reach(game, () => node.Progress.WholeTicks > 0, "cancellation after physical consumption");
                SimGame twin = factory.Load(game.Save());
                foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "ordinary cancellation releases native resource promises");
                c.Equal(game.Hash(), twin.Hash(), "cancelled resource workflow has identical saved continuation");
                c.Equal(afterWork ? Fix.FromInt(8) : Fix.FromInt(10), target.Get<IInventory>().Amount(steel), "cancellation releases unspent materials and never refunds consumed ones");
                c.Equal(Fix.One, worker.Get<IInventory>().Available(wrench), "cancellation releases the actual held tool");
                c.Equal(0, worker.Get<IWorker>().Job, "cancellation releases the existing actual machine relation");
                c.Equal(afterWork ? 2 : 0, node.Resources.Count, "terminal cancellation keeps only real spent occurrences");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled spent/unspent distinction restores through one node codec");
            }
        }
        private static void Timed(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Spawn(game, "requirements.target", 20, 20); Seed(game, target);
            Order order = Post(c, game, target, "requirements.timed"); var node = order.Node("work");
            c.Equal(1, node.Resources.Count, "positive timed work acquires the same native resource claim path");
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(Resource(game, "steel")), "timed assignment does not consume before elapsed work starts");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "saved timed resource work", twin);
            c.Equal(game.Hash(), twin.Hash(), "timed source commitments and consumption continue deterministically");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(Resource(game, "steel")), "positive timed work consumes its native bill once");
        }
        private static void LostAndRate(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Spawn(game, "requirements.target", 20, 20), worker = Spawn(game, "requirements.builder", 6, 20);
            Seed(game, target); long cursor = History(game).Latest; Order order = Post(c, game, target); var node = order.Node("work");
            Reach(game, () => node.Progress.WholeTicks > 0, "lost tool setup");
            WorkProgress progress = node.Progress; WorkResourceBinding held = null;
            foreach (var binding in node.Resources) if (binding.Choice.Use == WorkResourceUse.Hold) held = binding;
            // Native endpoint instrumentation simulates a legitimate owner releasing one exact claim.
            c.That(game.World.Services.Get<IResourceTransaction>().ReleaseClaims(new ResourceClaimReleaseIntent(Company, held.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "lost claim is an actual released native endpoint record");
            Tick(game);
            WorkResourceBinding renewed = null; foreach (var binding in node.Resources) if (binding.Choice.Use == WorkResourceUse.Hold) renewed = binding;
            c.That(renewed != null && renewed.Receipts[0].Token.Sequence > held.Receipts[0].Token.Sequence, "lost tool causes explicit release/reselection and a new actual token, never stale reuse");
            c.Equal(progress, node.Progress, "lost-claim frame advances no physical work");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(Resource(game, "steel")), "lost tool never repeats consumed materials");
            c.Equal(2, History(game, cursor).Entries.Count, "lost/reselected claim emits no fake or repeated consumption history");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "renewed tool and prior spending have one coherent saved codec");
            Reach(game, () => order.State == OrderState.Done, "lost-tool recovered completion");

            game = New(factory); target = Spawn(game, "requirements.target", 20, 20); worker = Spawn(game, "requirements.builder", 6, 20);
            Seed(game, target); cursor = History(game).Latest;
            WorkerExecutionTestContent.Control(game, worker.Id, true, Fix.Zero);
            order = Post(c, game, target); node = order.Node("work");
            for (int i = 0; i < 10; i++) Tick(game);
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(Resource(game, "steel")), "zero effective work cannot spend startup materials");
            c.Equal(0L, node.Progress.WholeTicks, "zero effective work advances no elapsed physical work");
            c.Equal(0, History(game, cursor).Entries.Count, "zero effective work emits no consumed fact");
            WorkerExecutionTestContent.Control(game, worker.Id, true, Fix.One);
            SimGame twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "restored positive work", twin);
            c.Equal(game.Hash(), twin.Hash(), "resumed native claims/spending continue identically after saved zero-work state");
            c.Equal(2, History(game, cursor).Entries.Count, "positive resumed work spends exactly once");
        }
        private static void SavedIntegrity(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Spawn(game, "requirements.target", 20, 20); Spawn(game, "requirements.builder", 6, 20);
            Seed(game, target); Order order = Post(c, game, target); var node = order.Node("work");
            var original = node.Resources; var actual = original[0];
            Action<IReadOnlyList<WorkResourceBinding>, string> reject = (bindings, message) =>
            {
                node.Resources = bindings;
                try { c.Throws<CorruptStateException>(() => factory.Load(game.Save()), message); }
                finally { node.Resources = original; }
            };
            var changed = new List<WorkResourceBinding>(original);
            changed[0] = new WorkResourceBinding(actual.Choice, actual.Receipts, true);
            reject(changed.AsReadOnly(), "saved fake spent flag cannot hide a live native promise");
            changed = new List<WorkResourceBinding>(original); changed.Add(actual);
            reject(changed.AsReadOnly(), "saved duplicate occurrence/token is refused");
            changed = new List<WorkResourceBinding>(original); changed.RemoveAt(0);
            reject(changed.AsReadOnly(), "assigned saved workflow cannot lose a selected resource occurrence");
            var receipt = actual.Receipts[0];
            changed = new List<WorkResourceBinding>(original);
            changed[0] = new WorkResourceBinding(actual.Choice, new[] { new ResourceReservation(receipt.Token,
                new ReservationOwner("foreign", 123), receipt.Resource, receipt.Amount, receipt.Direction) }, false);
            reject(changed.AsReadOnly(), "saved resource owner cannot be substituted by token possession");
            changed = new List<WorkResourceBinding>(original);
            changed[0] = new WorkResourceBinding(new WorkResourceChoice(actual.Choice.Path, actual.Choice.Requirement, actual.Choice.Endpoint,
                actual.Choice.Quantities, actual.Choice.Use == WorkResourceUse.Consume ? WorkResourceUse.Hold : WorkResourceUse.Consume,
                actual.Choice.Rates), actual.Receipts, false);
            reject(changed.AsReadOnly(), "saved use cannot change an authored consumable into a held tool");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "rejected corrupted snapshots never mutate the original endpoint books");
            var orphan = new ReservationOwner("sim.work.resources.orphan", order.Id);
            var orphanToken = target.Get<IInventory>().ReserveOut(orphan, Resource(game, "steel"), Fix.One);
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "entity endpoint orphaned engine resource promise has no accepted save path");
            target.Get<IInventory>().ReleaseOut(orphan, orphanToken);
            game.World.Services.Get<EndpointPools>().Get(5000000000).ReserveOut(orphan, Resource(game, "water"), Fix.FromRatio(1, 2));
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "scoped endpoint orphaned engine promise is validated by its real provider book, not a second endpoint registry");
        }
        private static void ProviderFault(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Spawn(game, "requirements.target", 20, 20); Spawn(game, "requirements.builder", 6, 20);
            Seed(game, target); long history = History(game).Latest; var pools = game.World.Services.Get<EndpointPools>();
            pools.FaultEndpoint = 1; pools.ClaimFault = ClaimFaultMode.ExtraAcquire;
            game.Seat(0).Submit(new PostOrderCommand { Target = target.Id, Plan = "requirements.work" }); game.Update();
            c.That(game.Fault != null && game.Fault.Message.Contains("unrelated batch promises"), "hostile scoped acquisition faults the normal activity assignment for its exact protocol violation");
            c.Equal(history, History(game).Latest, "failed resource assignment publishes no fake physical consumption");
            c.Throws<InvalidOperationException>(() => game.Save(), "failed scoped activity acquisition cannot checkpoint partial native promises");
            c.Throws<InvalidOperationException>(() => game.Hash(), "failed scoped activity acquisition has no settled hash fallback");
        }
        private static void JointSources(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Spawn(game, "requirements.target", 20, 20); Spawn(game, "requirements.builder", 6, 20);
            Seed(game, target); long cursor = History(game).Latest; Order order = Post(c, game, target, "requirements.multisource");
            var pools = game.World.Services.Get<EndpointPools>(); ResourceId water = Resource(game, "water");
            c.Equal(Fix.FromRatio(1, 2), pools.Get(1).Available(water), "first occurrence holds its real half-unit at the cheapest source");
            c.Equal(Fix.FromRatio(1, 4), pools.Get(5000000000).Available(water), "second occurrence atomically holds three-quarter unit at the feasible alternative");
            c.Equal(Fix.One, pools.Get(1).Amount(water), "joint assignment does not transport or consume remote stock instantly");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "joint-source physical work", twin);
            c.Equal(game.Hash(), twin.Hash(), "iteratively selected multiple scoped providers save and continue identically");
            c.Equal(new BigInteger(Fix.FromRatio(5, 4).Raw), History(game, cursor).Totals(water).ConsumedRaw, "both exact native occurrences are consumed once through the actual physical work tick");
            c.Equal(2, History(game, cursor).Entries.Count, "joint provider choice creates only its two actual spent native facts");
        }
    }
}
