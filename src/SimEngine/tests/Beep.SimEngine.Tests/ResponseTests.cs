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
    public static class ResponseTests
    {
        private static readonly Code Source = new Code("damage"), Kind = new Code("module.jam");
        public static void Register()
        {
            TestRunner.Add("Responses: external authored effects and physical saved recovery", Physical);
            TestRunner.Add("Wide time: external incident and physical saved recovery", WideRecovery);
            TestRunner.Add("Responses: cancellation reposts normal work without duplicate jobs", Repost);
            TestRunner.Add("Responses: episode and completion guards reject stale or instant work", Episode);
            TestRunner.Add("Responses: typed effect refusals stop dependent work and upgrades", Refusal);
            TestRunner.Add("Responses: authored references validate before any system initialization", Invalid);
        }
        private static string Definitions()
            => Colony.Defs.Replace("\"worker_roles\":", @"
  ""response_effects"": [ { ""id"": ""module.restore"", ""source"": ""damage"" } ],
  ""mark_effects"": [ { ""id"": ""module.mark"", ""source"": ""damage"", ""amount"": 2 } ],
  ""target_resource_requirements"": [{ ""id"": ""module.repair.inputs"",""port"":null, ""use"": ""consume"", ""quantities"": { ""steel"": 3 } }],
  ""work_cost_requirements"": [{ ""id"": ""module.repair.cost"", ""amount"": 7, ""credit"": false, ""category"": ""running"" }],
  ""work_requirement_groups"": [{ ""id"": ""module.repair.ready"", ""mode"": ""all"", ""entries"": [
    { ""id"": ""inputs"", ""requirement"": ""module.repair.inputs"" }, { ""id"": ""cost"", ""requirement"": ""module.repair.cost"" }
  ] }],
  ""worker_roles"":")
                .Replace("\"worker\": { \"roles\": [\"crane\"], \"states\": { \"working\": \"working\", \"loading\": \"loading\", \"stopped\": \"stopped\" } }", "\"contract_worker\": { \"role\": \"crane\", \"rate\": 2, \"states\": { \"working\": \"working\", \"stopped\": \"stopped\" } }")
                .Replace("\"construction\":", "\"contract_incident\": { \"id\": \"damage\", \"kind\": \"module.jam\", \"response\": \"unjam\" }, \"single_resource_port\": { \"resource\": \"steel\", \"capacity_policy\": \"fixture.count.40\", \"initial\": 30 }, \"construction\":")
                .Replace("\"activities\": [", @"""activities"": [
    { ""id"": ""unjam"", ""priority"": 80, ""automatic_supply"": true, ""nodes"": [
      { ""id"": ""repair"", ""executor"":""work.staffed"",""roles"": [""crane""], ""duration"":{""numerator"":1,""denominator"":1,""unit"":""hour""}, ""requirement"": ""module.repair.ready"", ""effect"": ""module.mark"" }
    ], ""on_complete"": ""module.restore"" },");
        private static Entity Target(SimGame game, int seat = 0)
        {
            Entity target = ((global::Beep.SimEngine.Maps.TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>("tower"), new PlayerId(seat), new TileCoord(16 + seat * 6, 18));
            game.World.Entities.Flush(); return target;
        }
        private static CommandResult Raise(SimGame game, Entity target)
            => TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = Source, Kind = Kind });
        private static void Physical(Check c) => Physical(c, 0);
        private static void WideRecovery(Check c) => Physical(c, 1L << 40);
        private static void Physical(Check c, long startTick)
        {
            GameFactory factory = Colony.Factory(Definitions());
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            if (startTick != 0) WideTimeTests.SetTick(game, startTick);
            Entity target = Target(game), foreign = Target(game, 1);
            ContractIncident incident = target.Get<ContractIncident>();
            IncidentsView view = game.Seat(0).View.Feature<IncidentsView>();
            c.That(ReferenceEquals(game.World.Content.Get<IWorkEffect>("module.mark"), game.World.Content.Get<MarkEffectDef>("module.mark")), "typed effects alias the existing content instance");
            c.That(view.TryGetIncident(target.Id, Source, out IncidentSnapshot before) && !before.Active, "inactive is an explicit engine fact");
            c.That(!view.TryGetIncident(foreign.Id, Source, out _), "foreign incident facts remain private");
            c.Equal(Codes.NotOwned, Raise(game, foreign).Refusal, "incident command honors company ownership");
            Entity machine = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id);
            FixVec2 original = machine.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position;
            c.That(machine.Get<Worker>() == null && machine.Get<IWorker>() is ContractWorker, "independent machine executes the unchanged work system");
            c.That(Raise(game, target).Accepted, "engine-owned scenario command raises the authored incident");
            c.That(view.TryGetIncident(target.Id, Source, out IncidentSnapshot active) && active.Active && active.Episode == 1 && active.Kind == Kind, "typed facts report actual incident identity and episode");
            c.Equal(startTick + 1, active.RaisedTick, "incident retains the full authoritative timestamp after the rejected foreign command");
            c.That(view.TryGetResponseOrder(target.Id, Source, out int orderId), "response is an ordinary authoritative work order");
            Order order = game.World.Services.Get<WorkBoard>().Order(orderId);
            c.That(order.ResponseSource == Source && order.ResponseEpisode == active.Episode, "existing order binds the incident episode");
            c.That(incident.Completed == 0 && order.IsActive, "raising never instantly repairs the target");
            var identities = new List<Code>();
            c.That(view.CopyIncidentIds(target.Id, identities) && identities.Count == 1 && identities[0] == Source, "view exposes authored named provider identities");
            ulong hash = game.Hash();
            view.TryGetIncident(target.Id, Source, out _); view.TryGetResponseOrder(target.Id, Source, out _);
            c.Equal(hash, game.Hash(), "incident and response queries neither advance nor mutate gameplay");
            game.RunTicks(20);
            c.That(machine.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position != original, "repair machine physically travels toward its target");
            c.That(ResponseWork.Read(incident).Active && target.State == new Code("state.stopped"), "entity Update settles the active incident state while work takes time");
            c.Equal(Fix.FromInt(27), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "normal work consumes actual repair material");
            c.Equal(Fix.FromInt(9993), ((TestLedger)game.World.Services.Get<IEconomy>()).Cash(target.Owner), "normal work charges the authored repair bill once");
            SimGame restored = factory.Load(game.Save());
            c.Equal(EngineInfo.SaveFormat, SaveGame.Describe(game.Save()).Format, "response order and codec identity use the current breaking saved layout");
            c.Equal(game.Hash(), restored.Hash(), "in-flight response and incident state restore together");
            IncidentsView restoredView = restored.Seat(0).View.Feature<IncidentsView>();
            c.That(restoredView.TryGetResponseOrder(target.Id, Source, out int restoredId) && restoredId == orderId, "save retains the same response order instead of posting a duplicate");
            int ticks = 0;
            while (ResponseWork.Read(incident).Active && ticks++ < 400) { game.RunTicks(1); restored.RunTicks(1); }
            c.That(!ResponseWork.Read(incident).Active, "bounded physical workflow completes recovery");
            c.Equal(1, incident.Completed, "whole-order response effect commits once");
            c.Equal(2, incident.Marks, "authored external step effect executes once at completion");
            c.Equal(game.Hash(), restored.Hash(), "saved response continues deterministically in interleaved sessions");
            c.That(!view.TryGetResponseOrder(target.Id, Source, out _), "completed response has no live duplicate");
            c.That(!before.Active && active.Active, "captured facts remain detached after recovery");
            c.Equal(target.Def.InitialState.State, target.State, "entity Update clears stopped state after actual repair");
            c.That(!ResponseWork.Read(foreign.Get<ContractIncident>()).Active, "response never changes another company");
        }
        private static void Repost(Check c)
        {
            SimGame game = Colony.Factory(Definitions()).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity target = Target(game);
            c.That(Raise(game, target).Accepted, "incident starts through the module command");
            WorkBoard board = game.World.Services.Get<WorkBoard>();
            IncidentsView view = game.Seat(0).View.Feature<IncidentsView>();
            view.TryGetResponseOrder(target.Id, Source, out int original);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = original }).Accepted, "ordinary cancellation releases response work");
            c.That(view.TryGetResponseOrder(target.Id, Source, out int replacement) && replacement == original, "engine provider resumes the same cancelled response records without resetting physical work");
            c.That(ResponseWork.Read(target.Get<ContractIncident>()).Active, "cancellation does not fabricate completed repair");
            for (int i = 0; i < 5; i++) ResponseWork.EnsurePosted(game.World, target, target.Get<ContractIncident>());
            int count = 0;
            for (int i = 0; i < board.Orders.Count; i++) if (board.Orders[i].IsActive && board.Orders[i].Target == target.Id) count++;
            c.Equal(1, count, "repeated engine dispatch cannot duplicate a live response");
            c.Equal(1, board.Order(replacement).ResponseEpisode, "repost retains the original incident episode");
            int ticks = 0;
            while (ResponseWork.AnyActive(target) && ticks++ < 400) game.RunTicks(1);
            c.That(!ResponseWork.AnyActive(target), "reenrolled response completes through physical work");
            c.Equal(1, target.Get<ContractIncident>().Completed, "cancelled work cannot also complete its effect");
        }
        private static void Episode(Check c)
        {
            SimGame game = Colony.Factory(Definitions()).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity target = Target(game);
            c.That(Raise(game, target).Accepted, "episode starts normally");
            ContractIncident incident = target.Get<ContractIncident>();
            WorkBoard board = game.World.Services.Get<WorkBoard>();
            Order order = board.ActiveOn(target.Id, incident.ResponsePlan.PlanKey);
            c.Equal(IncidentCodes.StaleResponse, incident.CompleteResponse(game.World, order), "active unfinished order cannot instantly clear incident");
            c.That(ResponseWork.Read(incident).Active, "refused completion leaves incident state unchanged");
            c.Equal(IncidentCodes.StaleResponse, incident.CompleteResponse(game.World, new Order { Id = order.Id }), "forged order object is not an authoritative completion");
            // Explicit persistence/completion fault injection, not host gameplay.
            order.State = OrderState.Completing; order.Nodes[0].State = NodeState.Done; order.ResponseEpisode++;
            c.Equal(IncidentCodes.StaleResponse, incident.CompleteResponse(game.World, order), "same-tick work from a different episode cannot clear current incident");
            order.ResponseEpisode--; order.ResponseSource = new Code("other-source");
            c.Equal(IncidentCodes.StaleResponse, incident.CompleteResponse(game.World, order), "wrong named incident source cannot clear another provider");
            order.ResponseSource = Source; order.Owner = new PlayerId(1);
            c.Equal(IncidentCodes.StaleResponse, incident.CompleteResponse(game.World, order), "completed work must belong to the incident owner");
            c.Equal(0, incident.Completed, "all refused completion probes are nonmutating");
            c.That(!incident.Raise(game.World, new Code("undeclared")), "unknown or duplicate incident cannot fabricate another episode");
            c.Throws<ArgumentException>(() => IncidentSnapshot.Raised(Kind, 0, 0), "active incident needs positive stable episode identity");
        }
        private static void Refusal(Check c)
        {
            string defs = Definitions().Replace("\"id\": \"module.mark\", \"source\": \"damage\"", "\"id\": \"module.mark\", \"source\": \"absent\"")
                .Replace("\"id\": \"unjam\", \"priority\": 80", "\"id\": \"unjam\", \"priority\": 80, \"replace_with\": \"tank\"");
            SimGame game = Colony.Factory(defs).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity target = Target(game); Raise(game, target);
            Order order = game.World.Services.Get<WorkBoard>().ActiveOn(target.Id, "activity:unjam");
            int ticks = 0;
            while (order.IsActive && ticks++ < 400) game.RunTicks(1);
            c.Equal(OrderState.Failed, order.State, "typed effect refusal ends the existing order as failed");
            c.Equal(IncidentCodes.NotResponse, order.Blocker, "refusal preserves the exact engine blocker");
            c.That(target.Alive && target.Def.Id == "tower", "failed step prevents both final effect and upgrade");
            c.That(ResponseWork.AnyActive(target), "refused effect cannot fabricate recovery");
            c.Equal(0, target.Get<ContractIncident>().Marks, "refusal has no partial provider mutation");
            c.Equal(0, target.Get<ContractIncident>().Completed, "dependent completion never runs after refusal");
            for (int i = 0; i < order.Nodes.Count; i++)
            {
                Job job = game.World.Services.Get<WorkBoard>().Job(order.Nodes[i].JobId);
                c.That(job == null || !job.IsLive, "failed work releases its assigned machine");
            }
            string finalDefs = Definitions().Replace("\"id\": \"module.restore\", \"source\": \"damage\"", "\"id\": \"module.restore\", \"source\": \"absent\"")
                .Replace("\"id\": \"unjam\", \"priority\": 80", "\"id\": \"unjam\", \"priority\": 80, \"replace_with\": \"tank\"");
            SimGame finalGame = Colony.Factory(finalDefs).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity finalTarget = Target(finalGame); Raise(finalGame, finalTarget);
            Order finalOrder = finalGame.World.Services.Get<WorkBoard>().ActiveOn(finalTarget.Id, "activity:unjam");
            ticks = 0;
            while (finalOrder.IsActive && ticks++ < 400) finalGame.RunTicks(1);
            c.Equal(OrderState.Failed, finalOrder.State, "whole-order effect refusal also fails instead of reporting completion");
            c.Equal(IncidentCodes.NotResponse, finalOrder.Blocker, "whole-order refusal retains the typed reason");
            c.That(finalTarget.Alive && finalTarget.Def.Id == "tower", "upgrade waits for successful completion effect validation");
            c.That(ResponseWork.AnyActive(finalTarget), "refused final effect leaves incident unresolved");
            c.Equal(2, finalTarget.Get<ContractIncident>().Marks, "completed step effects are not fabricated away by later refusal");
            c.Equal(0, finalTarget.Get<ContractIncident>().Completed, "refused final recovery commits no incident mutation");
        }
        private sealed class ProbeValidator : ICompositionValidator
        { public int Calls; public void ValidateComposition(World world) => Calls++; }
        private sealed class ProbeModule : GameModule
        {
            public readonly ProbeValidator Validator = new ProbeValidator();
            public int Initialized;
            public override string Id => "probe";
            public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
            public override void Compose(WorldComposer composer)
            {
                composer.Services.Bind(Validator); composer.Services.Bind<ICompositionValidator>(Validator);
                composer.AddSystem(new ProbeSystem(this), 2);
            }
        }
        private sealed class ProbeSystem : GameSystem
        {
            public override int StateSchemaVersion => 1;
            private readonly ProbeModule _probe;
            public ProbeSystem(ProbeModule probe) => _probe = probe;
            public override string Id => "probe.initialize";
            public override void Initialize(World world) => _probe.Initialized++;
            public override void Update(World world, TickInfo tick) { }
        }
        private static GameFactory ProbeFactory(string definitions, ProbeModule probe)
        {
            var definition = new GameDefinition("colony", new GameModule[] { new InventoryModule(), new OperationsModule(), new ContentAccessModule(), new TransformationModule(), new MotionModule(), new TileNavigationModule(), new WorkModule(), new LogisticsModule(), new ConstructionModule(), new InfrastructureModule(), new IncidentResponseModule(), new ColonyModule(), probe });
            string manifest = Colony.Manifest.Replace("\"modules\": [", "\"modules\": [{ \"id\": \"probe\", \"version\": \"1.0.0\", \"documents\": [] },");
            var files = new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", manifest).Add("colony.json", definitions)
                .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static void Invalid(Check c)
        {
            var probe = new ProbeModule();
            GameFactory bad = ProbeFactory(Definitions().Replace("\"effect\": \"module.mark\"", "\"effect\": \"missing.effect\""), probe);
            c.Throws<ContentException>(() => bad.NewGame(new NewGameRequest { Scenario = "colony", Mods = { "probe" } }), "unknown authored effect refuses before gameplay");
            c.Equal(0, probe.Initialized, "invalid work reference runs no system Initialize, even an earlier-ordered one");
            var goodProbe = new ProbeModule();
            SimGame game = ProbeFactory(Definitions(), goodProbe).NewGame(new NewGameRequest { Scenario = "colony", Mods = { "probe" } });
            c.Equal(1, goodProbe.Validator.Calls, "service aliases validate the same provider exactly once");
            c.Equal(1, goodProbe.Initialized, "valid composition initializes its system normally");
            c.Throws<ContentException>(() => Colony.Factory(Definitions().Replace("\"id\": \"module.mark\"", "\"id\": \"module.restore\"")).NewGame(new NewGameRequest { Scenario = "colony" }), "shared-base effect identity cannot have competing provider owners");
            c.Throws<ContentException>(() => Colony.Factory(Definitions().Replace("\"response\": \"unjam\"", "\"response\": \"missing\"")).NewGame(new NewGameRequest { Scenario = "colony" }), "incident response references must resolve");
            c.Throws<ContentException>(() => Colony.Factory(Definitions().Replace("\"roles\": [\"crane\"], \"hours\": 1, \"requirement\"", "\"hours\": 1, \"requirement\"")).NewGame(new NewGameRequest { Scenario = "colony" }), "timer-only response cannot substitute for a physical machine");
            c.Throws<ContentException>(() => Colony.Factory(Definitions().Replace("\"roles\": [\"crane\"], \"hours\": 1, \"requirement\"", "\"roles\": [\"crane\"], \"hours\": 0, \"requirement\"")).NewGame(new NewGameRequest { Scenario = "colony" }), "zero-time response cannot instantly repair");
            c.Throws<ContentException>(() => Colony.Factory(Definitions().Replace("\"source\": \"damage\" } ],", "\"source\": \"\" } ],")).NewGame(new NewGameRequest { Scenario = "colony" }), "response effect source cannot be implicit");
            c.Throws<ContentException>(() => Colony.Factory(Colony.Defs.Replace("\"operation\": \"finish\"", "\"operation\": \"unknown\"")).NewGame(new NewGameRequest { Scenario = "colony" }), "invalid construction effect operation refuses authored content");
            c.Throws<ContentException>(() => Colony.Factory(Definitions().Replace("\"on_complete\": \"module.restore\"", "\"on_complete\": \"absent.effect\"")).NewGame(new NewGameRequest { Scenario = "colony" }), "whole-order effect references validate before initialization");
            c.Throws<ContentException>(() => Colony.Factory(Definitions().Replace("\"id\": \"sim.line.finish\"", "\"id\": \"other.line.finish\"")).NewGame(new NewGameRequest { Scenario = "colony" }), "generated line work validates its required authored effect before initialization");
            c.That(typeof(WorkBoard).GetMethod("RegisterEffect") == null, "old effect registration path is removed, not retained as compatibility");
        }
    }
}
