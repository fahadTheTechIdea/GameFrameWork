using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.PhysicalRecoveryTestContent;

namespace Beep.SimEngine.Tests
{
    public static class WorkerCandidateTests
    {
        private static string Definitions => WorkerExecutionTestContent.Author(FrameworkProofContent.Read("worker-candidates.json"));
        public static void Register()
        {
            TestRunner.Add("Worker candidates: authored roles, scoped offers and pure private selection", Admission);
            TestRunner.Add("Worker candidates: committed alternatives, physical reselection and saved ownership", Physical);
        }
        private static List<WorkWorkerCandidate> Offers(Check c, SimGame game, Entity target, string plan = "candidates.work")
        {
            var candidates = new List<WorkWorkerCandidate>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates(plan, "work", target.Id, candidates), "engine previews actual worker offers");
            return candidates;
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "candidates.target", 20, 20);
            Entity loader = Spawn(game, "candidates.loader", 18, 20);
            Entity crane = Spawn(game, "candidates.crane", 16, 20);
            Spawn(game, "candidates.loader", 22, 20, 1); Tick(game);
            var node = game.World.Content.Get<ActivityDef>("candidates.work").Graph.Find("work");
            c.Equal(2, node.Roles.Count, "multiple roles resolve through existing worker-role definitions");
            c.Equal("crane", node.Roles[0].Value, "allowed roles use canonical identity order");
            c.Throws<NotSupportedException>(() => ((IList<WorkerRoleId>)node.Roles).Clear(), "node role set is immutable");
            c.That(game.World.Content.Get<WorkRequirementDef>("candidates.fast") is IWorkerWorkRequirement, "external scoped leaf uses the same base registry alias");
            ulong before = game.Hash(); var candidates = Offers(c, game, target, "candidates.strict");
            c.Equal(loader.Id, candidates[0].Worker, "nearest actual compatible worker is first");
            c.Equal("loader", candidates[0].Role.Value, "alternate role selects without scheduler or enum edits");
            c.That(candidates[0].TravelCost.HasValue && candidates[0].TravelCost.Value >= 0, "offer has a real spatial cost");
            c.Equal(Fix.FromInt(2), candidates[0].Result.Quantity.Value.Available, "external predicate observes the actual candidate rate");
            c.That(candidates.Exists(x => x.Result.Blocker == new Code("candidates.too-slow")), "slow owned machines have typed refusals rather than disappearing");
            c.That(candidates.TrueForAll(x => game.World.Entities.Get(x.Worker).Owner == new PlayerId(0)), "foreign machines never leak through private offers");
            c.Equal(0, loader.Get<IWorker>().Job, "pure offer claims no machine");
            c.Equal(before, game.Hash(), "candidate preview changes no stock, jobs, choice, IDs or money");
            var choiceOffers = Offers(c, game, target);
            c.Equal("a.preferred", choiceOffers[0].Result.Choices[0].Entry, "offer exposes the satisfied occurrence before any acquisition");
            c.Throws<NotSupportedException>(() => ((IList<string>)choiceOffers[0].Result.Choices[0].Path).Clear(), "offer choice paths are immutable");
            loader.Get<IWorker>().Held = true;
            candidates = Offers(c, game, target, "candidates.strict");
            c.Equal(crane.Id, candidates[0].Worker, "held nearest worker is not selected");
            c.That(candidates.Exists(x => x.Worker == loader.Id && x.Result.Blocker == WorkSystem.BlockerWorkerHeld), "held state has an explicit offer blocker");
            loader.Get<IWorker>().Held = false;
            WorkerExecutionTestContent.Control(game, loader.Id, false, Fix.One);
            candidates = Offers(c, game, target, "candidates.strict");
            c.That(candidates.Exists(x => x.Worker == loader.Id && x.Result.Blocker == WorkSystem.BlockerWorkerUnavailable), "actual operation eligibility gates candidate assignment");
            WorkerExecutionTestContent.Control(game, loader.Id, true, Fix.One);
            var foreign = new List<WorkWorkerCandidate>(candidates);
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<WorkView>().PreviewWorkerCandidates("candidates.work", "work", target.Id, foreign), "foreign candidate preview is refused");
            c.Equal(0, foreign.Count, "refusal clears previously retained private offers");
            c.Equal(Codes.InvalidArgument, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("candidates.work", "missing", target.Id, foreign), "unknown node refuses explicitly");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"roles\":[\"loader\",\"crane\"]", "\"role\":\"crane\""))), "old singular node role is removed");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"roles\":[\"loader\",\"crane\"]", "\"roles\":[\"loader\",\"loader\"]"))), "duplicate roles refuse before Initialize");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"roles\":[\"loader\",\"crane\"]", "\"roles\":[\"unknown\"]"))), "unknown role references refuse");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"roles\":[\"loader\",\"crane\"]", "\"roles\":[]"))), "worker-scoped requirement cannot become a timed fallback");
            c.Throws<ArgumentNullException>(() => ActivityNodeDef.Make("work", game.World.Content.Get<IActivityExecutor>("work.staffed"), null, GameDuration.Minutes(1), null, null), "generated nodes require an explicit role set");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, game.World.Services.Get<WorkBoard>().StateSchemaVersion, "choices use the sole existing work codec");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "old save layout is explicitly refused");
            Tie(c);
        }
        private static void Tie(Check c)
        {
            SimGame game = New(Factory(Definitions)); Entity target = Spawn(game, "candidates.target", 20, 20);
            Entity left = Spawn(game, "candidates.loader", 18, 20), right = Spawn(game, "candidates.loader", 23, 20);
            Tick(game); var candidates = Offers(c, game, target, "candidates.strict");
            WorkWorkerCandidate a = candidates.Find(x => x.Worker == left.Id), b = candidates.Find(x => x.Worker == right.Id);
            c.Equal(a.TravelCost, b.TravelCost, "mirrored candidate geometry has equal actual travel cost");
            c.Equal(left.Id, candidates[0].Worker, "equal cost and role use stable entity identity");
        }
        private static Order Post(Check c, SimGame game, Entity target, string plan = "candidates.work")
        {
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = plan });
            c.That(result.Accepted, "player posts through the existing engine command");
            return game.World.Services.Get<WorkBoard>().Order(checked((int)result.Arg0));
        }
        private static void Reach(SimGame game, Func<bool> ready, string stage, params SimGame[] twins)
        {
            for (int i = 0; !ready() && i < 500; i++) { Tick(game); foreach (var twin in twins) Tick(twin); }
            if (!ready())
            {
                var details = new List<string>();
                foreach (Order order in game.World.Services.Get<WorkBoard>().Orders)
                    foreach (var node in order.Nodes) details.Add(order.Id + ":" + node.Id + "/" + node.State + "/job=" + node.JobId + "/" + node.Progress + "/" + node.Blocker);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join("; ", details));
            }
        }
        private static void Permit(Check c, SimGame game, EntityId target, bool enabled)
            => c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target, Enabled = enabled }).Accepted, "player changes the engine-owned permit");
        private static void Physical(Check c)
        {
            // Only directly affected parallel graph/job execution, not its entire older test family.
            ActivityGraphTests.Physical(c);
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "candidates.target", 20, 20);
            Entity loader = Spawn(game, "candidates.loader", 18, 20), crane = Spawn(game, "candidates.crane", 16, 20);
            Tick(game); var offered = Offers(c, game, target)[0];
            Order order = Post(c, game, target); WorkBoard board = game.World.Services.Get<WorkBoard>();
            var node = order.Node("work"); Job job = board.Job(node.JobId);
            c.Equal(offered.Worker, job.Worker, "actual assignment uses the same current offer selector");
            c.Equal(loader.Id, job.Worker, "physical job uses a non-default authored role");
            c.Equal("loader", job.Role.Value, "job saves the actual selected role, not the first allowed role");
            c.Equal(job.Id, loader.Get<IWorker>().Job, "existing exclusive worker claim is the reservation");
            var fresh = Offers(c, game, target, "candidates.strict");
            c.That(fresh.Exists(x => x.Worker == loader.Id && x.Result.Blocker == WorkSystem.BlockerWorkerBusy), "another proposed job sees the actual exclusive claim as busy");
            c.Equal("a.preferred", node.Choices[0].Entry, "actual assignment commits the satisfied preferred branch");
            c.Equal("candidates.root", node.Choices[0].Path[0], "committed occurrence has stable root identity");
            c.Equal("worker", node.Choices[0].Path[1], "nested committed occurrence retains its authored branch path");
            c.Throws<NotSupportedException>(() => ((IList<WorkRequirementChoice>)node.Choices).Clear(), "committed choices are immutable facts");
            var views = new List<ActivityNodeView>();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, views), "owner can inspect committed node facts");
            c.Equal("a.preferred", views[0].Choices[0].Entry, "node view exposes committed branch, not a recomputed alternative");
            c.Equal(loader.Id, views[0].Worker, "private node projection reads actual assigned worker from its job");
            c.Equal("loader", views[0].SelectedRole.Value, "private projection exposes the actual assigned role");
            var retained = views[0];
            var offers = new List<WorkWorkerCandidate>();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyWorkerCandidates(order.Id, "work", offers), "active candidate view shares the selector");
            c.That(offers.Exists(x => x.Worker == loader.Id && x.Result.Status == WorkRequirementStatus.Satisfied), "owned assignment does not refuse itself as busy");
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyWorkerCandidates(order.Id, "work", offers) && offers.Count == 0, "active machine choices are company-private");
            ResourceId steel = game.World.Content.ResourceId("steel");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(steel), "actual inputs spent once");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "actual payment spent once");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "selected machine/role/choice round-trip in the existing codec");
            Reach(game, () => node.Progress.WholeTicks >= 6, "selected loader travels and works", twin);
            c.Equal(game.Hash(), twin.Hash(), "travel/work saved twin identity");
            WorkProgress progress = node.Progress;
            Permit(c, game, target.Id, false); Permit(c, twin, target.Id, false);
            c.Equal(NodeState.Paused, node.State, "committed branch pauses instead of silently swapping to the satisfied safe branch");
            c.Equal(progress, node.Progress, "pause retains actual physical progress");
            c.Equal("a.preferred", node.Choices[0].Entry, "paused choice remains committed");
            var criteria = new List<WorkRequirementView>();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyOrderRequirements(order.Id, criteria), "active readiness reads committed branches");
            c.Equal(WorkRequirementStatus.Pending, criteria[criteria.Count - 1].Result.Status, "active diagnostic uses committed readiness rather than fresh safe choice");
            c.Equal(new Code("candidates.disabled"), criteria[criteria.Count - 1].Result.Blocker, "private diagnostic and actual pause share the blocker");
            SimGame paused = factory.Load(game.Save()); c.Equal(game.Hash(), paused.Hash(), "paused branch and job save together");
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); Tick(paused); }
            c.Equal(progress, node.Progress, "no progress passes a blocked committed branch");
            c.Equal(game.Hash(), paused.Hash(), "paused continuation remains identical");
            // A real player interruption explicitly releases the choice; progress/input remain owned.
            foreach (SimGame current in new[] { game, twin, paused })
                c.That(TestKit.Order(current, 0, new MoveVehicleCommand { Vehicle = loader.Id, To = new TileCoord(10, 10) }).Accepted, "player interrupts actual worker through engine command");
            job = board.Job(node.JobId);
            c.That(job.Worker != loader.Id && job.Worker.IsValid, "released job reselects an available actual machine");
            c.Equal("z.safe", node.Choices[0].Entry, "reselection explicitly commits the currently valid safe branch");
            c.Equal(progress, node.Progress, "reselection never resets or fabricates work");
            c.Equal("a.preferred", retained.Choices[0].Entry, "old immutable snapshot does not follow changed choices");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(steel), "reselection does not consume inputs again");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "reselection does not charge again");
            SimGame reselected = factory.Load(game.Save()); c.Equal(game.Hash(), reselected.Hash(), "reselected worker/branch save without a second ledger");
            Reach(game, () => order.State == OrderState.Done, "reselected machine physically completes", twin, paused, reselected);
            c.Equal(game.Hash(), twin.Hash(), "interrupted twin completes identically");
            c.Equal(game.Hash(), reselected.Hash(), "reselected saved continuation completes identically");
            c.Equal("z.safe", node.Choices[0].Entry, "completed facts retain the actual branch");
            LossAndCancel(c);
            TimedChoices(c);
        }
        private static void LossAndCancel(Check c)
        {
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "candidates.target", 20, 20), loader = Spawn(game, "candidates.loader", 18, 20);
            Spawn(game, "candidates.crane", 16, 20); Tick(game);
            Order order = Post(c, game, target); var node = order.Node("work");
            Reach(game, () => node.Progress.WholeTicks >= 2, "work before lost participant");
            WorkProgress progress = node.Progress; game.World.Despawn(loader); game.World.Settle();
            SimGame lost = factory.Load(game.Save());
            Tick(game); Tick(lost);
            c.That(game.World.Services.Get<WorkBoard>().Job(node.JobId).Worker != loader.Id, "normal Update releases missing participant and reselects");
            c.Equal(progress, node.Progress, "missing participant preserves work progress");
            c.Equal(game.Hash(), lost.Hash(), "lost-participant save reselects deterministically after load");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "player cancels selected work");
            c.Equal(OrderState.Cancelled, order.State, "normal terminal order state");
            c.Equal(0, node.JobId, "cancellation releases sole job link");
            c.Equal(0, node.Choices.Count, "cancellation releases committed choices");
            c.Equal(progress, node.Progress, "cancellation retains real spent work");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled state saves with no orphan worker/choice");
            // Corrupt named selections are refused by the existing load-phase validator.
            SimGame corrupt = factory.Load(lost.Save()); var bad = corrupt.World.Services.Get<WorkBoard>().Order(order.Id).Node("work");
            bad.Choices = Array.AsReadOnly(new[] { new WorkRequirementChoice(new[] { "candidates.root", "worker" }, "absent") });
            c.Throws<CorruptStateException>(() => factory.Load(corrupt.Save()), "saved unknown committed entry cannot silently select another");
        }
        private static void TimedChoices(Check c)
        {
            string definitions = FrameworkProofContent.Read("requirement-expressions.json").Replace("\"minimum\":20000", "\"minimum\":100");
            GameFactory factory = Factory(definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "expression.target", 20, 20); Tick(game);
            Order order = Post(c, game, target, "expression.timer"); var node = order.Node("wait");
            c.Equal(0, node.JobId, "timed node does not invent a machine job");
            c.Equal("z.cash", node.Choices[0].Entry, "timed execution commits its actual initially satisfied branch");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "timed choice saves without a second progress owner");
            Permit(c, game, target.Id, true); Permit(c, twin, target.Id, true);
            c.Equal("z.cash", node.Choices[0].Entry, "new preferred readiness does not change committed timed choice");
            foreach (var current in new[] { game, twin })
                current.World.Services.Get<IEconomy>().Charge(new PlayerId(0), Fix.FromInt(10000), new Code("test.setup"));
            WorkProgress progress = node.Progress; Tick(game); Tick(twin);
            c.Equal(NodeState.Paused, node.State, "timed execution pauses its actual branch despite a ready alternative");
            c.Equal(progress, node.Progress, "timed blocked choice retains real elapsed work");
            c.Equal(game.Hash(), twin.Hash(), "timed blocked continuation identity");
            SimGame paused = factory.Load(game.Save()); c.Equal(game.Hash(), paused.Hash(), "timed paused commitment round-trips");
            foreach (var current in new[] { game, twin, paused })
                current.World.Services.Get<IEconomy>().Credit(new PlayerId(0), Fix.FromInt(10000), new Code("test.setup"));
            Reach(game, () => order.State == OrderState.Done, "timed committed branch resumes", twin, paused);
            c.Equal(game.Hash(), paused.Hash(), "timed saved pause resumes identically");
            c.Equal("z.cash", node.Choices[0].Entry, "completed timed facts retain actual choice");
        }
    }
}
