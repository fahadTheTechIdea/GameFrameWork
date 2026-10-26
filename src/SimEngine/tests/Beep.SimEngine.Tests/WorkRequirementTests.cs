using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class WorkRequirementTests
    {
        private static readonly Code WaitPermit = new Code("blocker.permit-pending");
        private static readonly Code MissingPermit = new Code("blocker.permit-missing");
        public static void Register()
        {
            TestRunner.Add("Requirements: authored providers, pure previews and private facts", Providers);
            TestRunner.Add("Requirements: physical work pauses, restores and consumes once", Physical);
            TestRunner.Add("Requirements: paused cancellation releases the existing job", Cancel);
            TestRunner.Add("Requirements: timed stages respect the same guards", Timed);
            TestRunner.Add("Requirements: travel continues physically when a guard changes", Travel);
            TestRunner.Add("Requirements: generated plans use the authored provider authority", Generated);
            TestRunner.Add("Requirements: missing/colliding providers and malformed results refuse", Invalid);
        }

        private static string Definitions()
            => Colony.Defs.Replace("\"worker_roles\":", @"
  ""work_permits"": [ { ""id"": ""site.ready"", ""blocker"": ""blocker.permit-pending"", ""missing"": ""blocker.permit-missing"" } ],
  ""target_resource_requirements"": [{ ""id"": ""inspect.inputs"",""port"":null, ""use"": ""consume"", ""quantities"": { ""steel"": 6 } }],
  ""work_cost_requirements"": [{ ""id"": ""inspect.cost"", ""amount"": 5, ""credit"": false, ""category"": ""construction"" }],
  ""work_requirement_groups"": [{ ""id"": ""inspect.ready"", ""mode"": ""all"", ""entries"": [
    { ""id"": ""a.site"", ""requirement"": ""site.ready"" }, { ""id"": ""b.reserve"", ""requirement"": ""company.reserve"" },
    { ""id"": ""inputs"", ""requirement"": ""inspect.inputs"" }, { ""id"": ""cost"", ""requirement"": ""inspect.cost"" }
  ] }],
  ""work_reserves"": [ { ""id"": ""company.reserve"", ""minimum"": 100, ""blocker"": ""blocker.reserve"", ""unit"": ""unit.cash"" } ],
  ""fault_requirements"": [ { ""id"": ""malformed"" } ],
  ""worker_roles"":")
                .Replace("\"construction\":{\"site\":\"site:tower\",\"activity\":\"tower.build-work\"", "\"work_permit\": { \"enabled\": false }, \"single_resource_port\": { \"resource\": \"steel\", \"capacity_policy\": \"fixture.count.40\", \"initial\": 30 }, \"construction\":{\"site\":\"site:tower\",\"activity\":\"tower.build-work\"")
                .Replace("\"id\": \"look\", \"roles\": [\"crane\"], \"hours\": 1", "\"id\": \"look\", \"roles\": [\"crane\"], \"hours\": 1, \"requirement\": \"inspect.ready\"");

        private static Entity Target(SimGame game, int seat = 0)
        {
            Entity target = ((global::Beep.SimEngine.Maps.TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>("tower"), new PlayerId(seat), new TileCoord(16 + seat * 6, 18));
            game.World.Entities.Flush();
            return target;
        }

        private static WorkProgress Progress(SimGame game, EntityId target)
        {
            var orders = new List<OrderView>();
            game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            foreach (OrderView order in orders) if (order.Target == target) return order.CurrentProgress;
            return WorkProgress.Zero;
        }

        private static bool Done(SimGame game, EntityId target) => game.Seat(0).View.Feature<WorkView>().ActiveOrders(target) == 0;

        private static void Permit(Check check, SimGame game, EntityId target, bool enabled)
            => check.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target, Enabled = enabled }).Accepted, "engine-owned permit command is accepted");

        private static void Providers(Check c)
        {
            SimGame game = Colony.Factory(Definitions()).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity target = Target(game), foreign = Target(game, 1);
            ResolvedContent content = game.World.Content;
            c.That(ReferenceEquals(content.Get<PermitRequirementDef>("site.ready"), content.Get<WorkRequirementDef>("site.ready")), "base and concrete queries return the same provider instance");
            c.Equal(4, content.Count<WorkRequirementDef>(), "existing indexes include independent concrete provider kinds");
            c.Equal(0, content.Get<ReserveRequirementDef>("company.reserve").Index, "base indexing does not overwrite a concrete table position");
            var providers = (IList<WorkRequirementEntry>)((IWorkRequirementGroup)content.Get<ActivityDef>("inspect").Graph.Nodes[0].Requirement).Entries;
            c.Throws<NotSupportedException>(() => providers.Clear(), "resolved requirement lists are immutable");
            WorkView view = game.Seat(0).View.Feature<WorkView>();
            var facts = new List<WorkRequirementView>();
            ulong hash = game.Hash();
            c.Equal(Code.None, view.PreviewOrderRequirements("inspect", target.Id, facts), "engine can preview a valid but currently unready order");
            c.Equal(3, facts.Count, "preview returns every authored requirement in stable order");
            c.Equal("look", facts[0].Node, "step identity is retained in diagnostics");
            c.Equal("site.ready", facts[0].Requirement, "provider identity is retained in diagnostics");
            c.Equal(WorkRequirementStatus.Pending, facts[0].Result.Status, "site guard reports pending, not implicit readiness");
            c.Equal(WaitPermit, facts[0].Result.Blocker, "typed blocker comes from authored provider content");
            c.That(!facts[0].Result.Quantity.HasValue, "an unmeasured guard fabricates no zero quantity");
            c.Equal(Fix.FromInt(10000), facts[1].Result.Quantity.Value.Available, "quantity reads the one existing economy");
            c.Equal(new Code("unit.cash"), facts[1].Result.Quantity.Value.Unit, "quantity carries its authored unit");
            WorkRequirementView snapshot = facts[0];
            c.Equal(Codes.NotOwned, view.PreviewOrderRequirements("inspect", foreign.Id, facts), "foreign operational facts stay private");
            c.Equal(0, facts.Count, "refused preview clears old diagnostics");
            c.Equal(Code.None, game.Observer.Feature<WorkView>().PreviewOrderRequirements("inspect", foreign.Id, facts), "explicit observer projection retains its authorized access");
            c.Equal(hash, game.Hash(), "preview neither consumes, reserves nor changes saved state");
            Permit(c, game, target.Id, true);
            c.Equal(WorkRequirementStatus.Pending, snapshot.Result.Status, "detached facts do not change when authoritative state changes");
            game.World.Services.Get<IEconomy>().Charge(new PlayerId(0), Fix.FromInt(9999), new Code("test.reserve"));
            view.PreviewOrderRequirements("inspect", target.Id, facts);
            c.Equal(WorkRequirementStatus.Blocked, facts[1].Result.Status, "reserve guard distinguishes a blocked quantitative criterion");
            c.Equal(Fix.One, facts[1].Result.Quantity.Value.Available, "observed shortage is actual engine state");
        }

        private static void Physical(Check c)
        {
            GameFactory factory = Colony.Factory(Definitions());
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            Entity target = Target(game);
            ResourceId steel = game.World.Content.ResourceId("steel");
            IEconomy economy = game.World.Services.Get<IEconomy>();
            CommandResult placed = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = target.Id });
            c.That(placed.Accepted, "pending work can be queued through the public command");
            game.RunTicks(checked((int)TestGame.Time.Hours(2)));
            c.Equal(WorkProgress.Zero, Progress(game, target.Id), "pending guard blocks progress");
            c.Equal(Fix.FromInt(30), target.Get<IInventory>().Amount(steel), "blocked stage spends no material");
            c.Equal(Fix.FromInt(10000), economy.Cash(new PlayerId(0)), "blocked stage spends no money");
            var orders = new List<OrderView>();
            game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            c.Equal(WaitPermit, orders[0].Blocker, "order diagnostics use the same requirement result as preview");
            Permit(c, game, target.Id, true);
            int travel = TestKit.RunUntil(game, () => Progress(game, target.Id).WholeTicks > 10, checked((int)TestGame.Time.Hours(3)), 1);
            c.That(travel > 10, "machine physically travels before working");
            c.Equal(Fix.FromInt(24), target.Get<IInventory>().Amount(steel), "satisfied stage consumes its bill once");
            c.Equal(Fix.FromInt(9995), economy.Cash(new PlayerId(0)), "satisfied stage charges once");
            WorkProgress progress = Progress(game, target.Id);
            Entity worker = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id);
            FixVec2 position = worker.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position;
            Permit(c, game, target.Id, false);
            Job job = game.World.Services.Get<WorkBoard>().Job(worker.Get<IWorker>().Job);
            c.Equal(JobState.Paused, job.State, "blocked work retains a paused job in the one job authority");
            c.That(job.IsLive && !worker.Get<IWorker>().Idle, "paused job keeps its assigned machine without offering it twice");
            game.RunTicks(checked((int)TestGame.Time.Hours(2)));
            c.Equal(progress, Progress(game, target.Id), "continuous guard stops work immediately and preserves progress");
            c.Equal(position, worker.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position, "pausing does not teleport the machine");
            c.That(game.Observer.TryGetEntity(worker.Id, out EntityView workerView) && workerView.State == game.World.Content.Get<IEntityStateDefinition>("stopped").State, "entity Update reports paused work truthfully");
            var facts = new List<WorkRequirementView>();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyOrderRequirements((int)placed.Arg0, facts) && facts[0].Result.Blocker == WaitPermit, "active diagnostics use the same provider evaluator");
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyOrderRequirements((int)placed.Arg0, facts) && facts.Count == 0, "active diagnostics preserve company privacy");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "paused job, permit, materials and progress restore through existing codecs");
            Permit(c, game, target.Id, true); Permit(c, restored, target.Id, true);
            int remaining = TestKit.RunUntil(game, () => Done(game, target.Id), checked((int)TestGame.Time.Hours(3)), 1);
            restored.RunTicks(remaining);
            c.That(Done(game, target.Id), "same physical job resumes and completes");
            c.Equal(game.Hash(), restored.Hash(), "saved continuation is deterministic");
            c.Equal(Fix.FromInt(24), target.Get<IInventory>().Amount(steel), "resume never consumes the bill twice");
            c.Equal(Fix.FromInt(9995), economy.Cash(new PlayerId(0)), "resume never charges twice");
            c.That(worker.Get<IWorker>().Idle, "completion releases the assigned machine");
        }

        private static void Cancel(Check c)
        {
            SimGame game = Colony.Factory(Definitions()).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity target = Target(game);
            Permit(c, game, target.Id, true);
            CommandResult placed = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = target.Id });
            TestKit.RunUntil(game, () => !Progress(game, target.Id).Equals(WorkProgress.Zero), checked((int)TestGame.Time.Hours(3)), 1);
            Entity worker = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id);
            Permit(c, game, target.Id, false);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = (int)placed.Arg0 }).Accepted, "normal cancellation accepts a paused order");
            c.That(worker.Get<IWorker>().Idle && Done(game, target.Id), "cancellation releases the same job reservation");
            c.Equal(Fix.FromInt(24), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "cancellation does not recreate spent materials");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "cancellation does not refund spent work costs");
        }

        private static void Timed(Check c)
        {
            string definitions = Definitions().Replace("\"id\": \"look\", \"roles\": [\"crane\"]", "\"id\": \"look\"");
            SimGame game = Colony.Factory(definitions).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity target = Target(game);
            TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = target.Id });
            game.RunTicks(20);
            c.Equal(WorkProgress.Zero, Progress(game, target.Id), "timed stages cannot bypass the guard");
            Permit(c, game, target.Id, true); game.RunTicks(20);
            WorkProgress progress = Progress(game, target.Id);
            c.That(!progress.Equals(WorkProgress.Zero), "satisfied timed stage advances over game ticks");
            Permit(c, game, target.Id, false); game.RunTicks(60);
            c.Equal(progress, Progress(game, target.Id), "timed progress pauses without starting over");
            c.Equal(1, game.Seat(0).View.Feature<WorkView>().IdleWorkers(new WorkerRoleId("crane")), "timed stage reserves no invented machine");
            Permit(c, game, target.Id, true);
            TestKit.RunUntil(game, () => Done(game, target.Id), checked((int)TestGame.Time.Hours(2)), 1);
            c.That(Done(game, target.Id), "timed stage resumes through existing work execution");
            c.Equal(Fix.FromInt(24), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "timed resume consumes once");
        }

        private static void Travel(Check c)
        {
            GameFactory factory = Colony.Factory(Definitions());
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            Entity target = Target(game);
            Permit(c, game, target.Id, true);
            TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = target.Id });
            Entity worker = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id);
            FixVec2 start = worker.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position;
            Permit(c, game, target.Id, false);
            SimGame restored = factory.Load(game.Save());
            int elapsed = TestKit.RunUntil(game, () => game.World.Services.Get<WorkBoard>().Job(worker.Get<IWorker>().Job)?.State == JobState.Paused,
                checked((int)TestGame.Time.Hours(3)), 1);
            restored.RunTicks(elapsed);
            c.That(worker.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position != start, "guard change does not teleport or freeze the travelling machine");
            c.Equal(WorkProgress.Zero, Progress(game, target.Id), "arrival cannot bypass the now-pending guard");
            Job job = game.World.Services.Get<WorkBoard>().Job(worker.Get<IWorker>().Job);
            c.That(job != null && job.State == JobState.Paused && job.Worker == worker.Id, "machine arrives and waits on its existing job");
            c.Equal(game.Hash(), restored.Hash(), "saved travel under a pending guard continues identically");
            Permit(c, game, target.Id, true); Permit(c, restored, target.Id, true);
            int remaining = TestKit.RunUntil(game, () => Done(game, target.Id), checked((int)TestGame.Time.Hours(2)), 1);
            restored.RunTicks(remaining);
            c.That(Done(game, target.Id), "arrival resumes ordinary timed work after the guard clears");
            c.Equal(game.Hash(), restored.Hash(), "resumed travel scenario has identical final state");
            c.Equal(Fix.FromInt(24), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "guard changes during travel do not consume materials twice");
        }

        private sealed class GeneratedPlan : IActivityDefinition
        {
            public bool AutomaticSupply => false;
            public IReadOnlyList<IContentDefinition> DisclosureContent => Array.Empty<IContentDefinition>();
            public string PlanKey => "test:generated";
            public IActivityGraph Graph { get; }
            public string OnComplete => null;
            public GeneratedPlan(WorkRequirementDef requirement, IActivityExecutor executor)
            {
                Graph = new ActivityGraph(new[] { ActivityNodeDef.Make("generated", executor, Array.Empty<WorkerRoleId>(), GameDuration.Minutes(10),
                    Array.Empty<string>(), null, requirement) }, PlanKey);
            }
        }

        private static void Generated(Check c)
        {
            SimGame game = Colony.Factory(Definitions()).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity target = Target(game);
            WorkBoard board = game.World.Services.Get<WorkBoard>();
            WorkRequirementDef provider = game.World.Content.Get<WorkRequirementDef>("site.ready");
            ulong initial = game.Hash();
            c.Throws<ContentException>(() => board.Post(new GeneratedPlan(new PermitRequirementDef { Id = "site.ready" }, game.World.Content.Get<IActivityExecutor>("work.timed")), new PlayerId(0), target.Id, 0),
                "same identity on an unauthored instance cannot impersonate a provider");
            c.Throws<ContentException>(() => board.Post(new GeneratedPlan(new WorkRequirementGroupDef { Id = "inspect.ready" }, game.World.Content.Get<IActivityExecutor>("work.timed")), new PlayerId(0), target.Id, 0),
                "unauthored composite cannot impersonate a registered requirement");
            c.That(new GeneratedPlan(null, game.World.Content.Get<IActivityExecutor>("work.timed")).Graph.Find("generated").Requirement == null, "absence explicitly means no extra guard");
            c.Equal(initial, game.Hash(), "refused plans change no identities, jobs or saved state");
            var plan = new GeneratedPlan(provider, game.World.Content.Get<IActivityExecutor>("work.timed"));
            c.That(ReferenceEquals(provider, plan.Graph.Nodes[0].Requirement), "generated node references the sole authored provider");
            Order order = board.Post(plan, new PlayerId(0), target.Id, 0);
            c.Equal(1, order.Id, "refused plans consume no order identity");
            c.Equal(1, board.Orders.Count, "valid generated plan uses the sole existing order store");
            game.RunTicks(20);
            c.Equal(WorkProgress.Zero, order.Nodes[0].Progress, "generated work uses the same pending evaluator");
            Permit(c, game, target.Id, true);
            TestKit.RunUntil(game, () => !order.IsActive, 30, 1);
            c.Equal(OrderState.Done, order.State, "generated work uses the same resumable execution path");
        }

        private static void Invalid(Check c)
        {
            void Refuse(string definitions, string what)
                => c.Throws<ContentException>(() => Colony.Factory(definitions).NewGame(new NewGameRequest { Scenario = "colony" }), what);
            Refuse(Definitions().Replace("\"requirement\": \"inspect.ready\"", "\"requirement\": \"unknown\""), "unknown requirement refuses content before play");
            Refuse(Definitions().Replace("\"requirement\": \"company.reserve\"", "\"requirement\": \"site.ready\""), "duplicate group references refuse content");
            Refuse(Definitions().Replace("\"id\": \"company.reserve\"", "\"id\": \"site.ready\""), "different provider kinds cannot own the same base-contract identity");
            c.Throws<ArgumentException>(() => WorkRequirementResult.Pending(Code.None), "pending results need a typed blocker");
            c.Throws<ArgumentException>(() => new WorkRequirementQuantity(Fix.Zero, Fix.One, Code.None), "quantity observations require an explicit unit");
            SimGame game = Colony.Factory(Definitions().Replace("\"work_permit\": { \"enabled\": false }, ", "")).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity target = Target(game);
            var facts = new List<WorkRequirementView>();
            game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("inspect", target.Id, facts);
            c.Equal(WorkRequirementStatus.Invalid, facts[0].Result.Status, "missing runtime capability reports invalid, not substituted readiness");
            c.Equal(MissingPermit, facts[0].Result.Blocker, "missing capability has a typed diagnostic");
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = target.Id }).Accepted, "queuing intent is distinct from readiness");
            c.That(Done(game, target.Id), "invalid execution fails instead of waiting forever");
            c.Equal(Fix.FromInt(30), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "invalid execution consumes no materials");
            var notifications = new List<Notification>(); long cursor = 0;
            game.Seat(0).ReadNotifications(ref cursor, notifications);
            c.That(notifications.Exists(n => n.Kind == WorkSystem.NoteOrderFailed), "existing notification authority reports failure");
            SimGame fault = Colony.Factory(Definitions().Replace("\"requirement\": \"inspect.ready\"", "\"requirement\": \"malformed\"")).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity badTarget = Target(fault);
            fault.Seat(0).Submit(new PostOrderCommand { Plan = "inspect", Target = badTarget.Id });
            c.Equal(0, fault.RunTicks(1), "malformed provider result cannot claim a completed tick");
            c.That(fault.Fault != null && fault.State == GameState.Faulted, "existing fault latch records contract violation without a fallback");
        }
    }
}
