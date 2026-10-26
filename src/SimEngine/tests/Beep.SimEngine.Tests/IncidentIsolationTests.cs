using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class IncidentIsolationTests
    {
        private static readonly Code Source = new Code("leak"), SignalSource = new Code("isolation"), Process = new Code("process");
        private static readonly Code Blocked = new Code("incident.isolated");
        public static void Register()
        {
            TestRunner.Add("Incident isolation: authored phase signals and gate admission", Contracts);
            TestRunner.Add("Incident isolation: timed process shutdown and saved restart", Physical);
        }
        private static string Text(JsonValue json) { var text = new StringBuilder(); json.WriteCanonical(text); return text.ToString(); }
        private static JsonValue Parts(JsonValue json) => json.Get("entities")[0].Get("components");
        private static GameFactory Factory(Action<JsonValue> edit = null, string work = null)
        {
            JsonValue json = Json.Parse(FrameworkProofContent.Read("incident-isolation.json")); edit?.Invoke(json);
            return Colony.Factory(additionalDocuments: new Dictionary<string, string>
            {
                { "incident-phases.json", work ?? FrameworkProofContent.Read("incident-phases.json") },
                { "incident-isolation.json", Text(json) }
            });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 813 });
        private static Entity Target(SimGame game, int seat = 0)
        {
            Entity entity = ((global::Beep.SimEngine.Maps.TileMap)game.World.Space).SpawnAt(game.World,
                game.World.Content.Get<EntityDef>("isolated-machine"), new PlayerId(seat), new TileCoord(16 + seat * 6, 18));
            game.World.Entities.Flush(); return entity;
        }
        private static void Tick(SimGame game)
        { if (game.RunTicks(1) != 1) throw new InvalidOperationException("isolation tick=" + game.Tick + ": " + game.Fault); }
        private static void Reach(SimGame game, Entity target, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            {
                var state = State(game, target.Id); Order order = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, Source);
                throw new InvalidOperationException(stage + " timeout; process=" + state.Phase + "/" + state.Blocker +
                    "; response=" + order?.State + "/" + order?.Blocker);
            }
        }
        private static TransformationSnapshot State(SimGame game, EntityId target)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(target, Process, out var state))
                throw new InvalidOperationException("missing process snapshot"); return state;
        }
        private static AutomationSignalSnapshot Signal(SimGame game, EntityId target, string signal = "leak.isolation-hold")
        {
            Code refusal = game.Seat(0).View.Feature<OperationsView>().ReadSignal(target, SignalSource, signal, out var state);
            if (!refusal.IsNone) throw new InvalidOperationException("missing signal=" + refusal); return state;
        }
        private static ExecutionGateEvaluation Gates(SimGame game, EntityId target, string recipe = "leak.batch")
        {
            Code refusal = game.Seat(0).View.Feature<TransformationManagementView>().ReadGates(target, Process, recipe, out var gates);
            if (!refusal.IsNone) throw new InvalidOperationException("missing gates=" + refusal); return gates;
        }
        private static CommandResult Raise(SimGame game, EntityId target)
            => TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target, Source = Source, Kind = new Code("machine.leak") });
        private static CommandResult Queue(SimGame game, EntityId target, string recipe = "leak.batch")
            => TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = target, Capability = Process, Transformation = recipe, Count = 1 });
        private static CommandResult Control(SimGame game, EntityId target, bool enabled)
            => TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = target, Operation = Process, Enabled = enabled, Target = Fix.One });
        private static void Invalid(Check c, Action<JsonValue> edit, string message)
            => c.Throws<ContentException>(() => New(Factory(edit)), message);
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Target(game), foreign = Target(game, 1);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current incident signal semantics have explicit build identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "existing incident/work/gate/transformer layouts stay unchanged");
            c.That(target.Def.Component<IPhasedIncidentDefinition>(Source) is IncidentDef, "named phased definition contract is public metadata");
            var definition = target.Def.Component<IncidentSignalDef>(SignalSource);
            c.That(ReferenceEquals(definition.Workflow, target.Get<IPhasedIncidentResponse>(Source).Workflow), "signal aliases actual authored source workflow");
            c.That(ReferenceEquals(definition.Phase, IncidentPhases.Find(definition.Workflow, "isolation")), "phase metadata aliases the existing native mapping");
            c.Equal(2, definition.SignalBindings.Count, "typed active and phase-hold metrics are explicitly declared");
            c.That(!typeof(IncidentSignal).GetMethod("Sync").DeclaringType.Equals(typeof(IncidentSignal)), "stateless signal declares no progress/hold codec payload");
            c.That(game.Seat(0).View.Feature<OperationsView>().TryGetSignalSchema(target.Id, SignalSource, out var schema) && schema.Version == 1,
                "existing signal schema registry publishes incident binding");
            c.Equal(Fix.Zero, Signal(game, target.Id).Value.Value, "unraised has explicit available false hold");
            c.Equal(Fix.Zero, Signal(game, target.Id, "leak.active").Value.Value, "unraised has explicit available false episode");
            c.Equal(Code.None, Gates(game, target.Id).Blocker, "unraised process readiness remains open");
            ulong hash = game.Hash(); var signal = Signal(game, target.Id); var gates = Gates(game, target.Id);
            c.Equal(hash, game.Hash(), "reading derived incident signals/gates cannot advance or mutate gameplay");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateSnapshot>)gates.Gates).Clear(), "existing readiness facts remain immutable");
            c.Equal(Codes.NotOwned, game.Seat(0).View.Feature<OperationsView>().ReadSignal(foreign.Id, SignalSource, "leak.active", out _), "foreign incident signal stays private");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadGates(target.Id, Process, "leak.batch", out _), "foreign company cannot inspect safety conditions");
            c.Throws<ArgumentException>(() => game.World.Content.Get<AutomationSignalDef>("leak.active").ReadFrom(New(factory).World, target.Get<IncidentSignal>(SignalSource)), "foreign session signal/source refuses");
            Invalid(c, j => Parts(j).Get("incident_signal").Remove("id"), "missing signal capability identity fails");
            Invalid(c, j => Parts(j).Get("incident_signal").Set("source", JsonValue.Of("absent")), "missing named incident source fails before initialization");
            Invalid(c, j => Parts(j).Get("incident_signal").Set("source", JsonValue.Of("process")), "nonincident source cannot substitute");
            Invalid(c, j => Parts(j).Get("incident_signal").Set("workflow", JsonValue.Of("absent")), "missing workflow fails");
            Invalid(c, j => Parts(j).Get("incident_signal").Set("phase", JsonValue.Of("absent")), "missing phase fails");
            Invalid(c, j => Parts(j).Get("incident_signal").Set("phase", JsonValue.Of("")), "blank phase is not an inferred isolation stage");
            Invalid(c, j => Parts(j).Get("incident_signal").Remove("phase"), "phase is explicit, not a gameplay default");
            Invalid(c, j => Parts(j).Get("incident_signal").Set("enabled", JsonValue.Of(true)), "no hidden hold/enabled-state mirror is accepted");
            Invalid(c, j => j.Get("signals")[0].Get("providers")[0].Set("schema", JsonValue.Number("2")), "wrong signal schema fails");
            Invalid(c, j => j.Get("signals")[0].Set("metric", JsonValue.Of("incident.guessed")), "unknown metric cannot become a default boolean");
            Invalid(c, j => j.Get("signals")[0].Set("unit", JsonValue.Of("unit.count")), "signal units remain exact");
            Invalid(c, j => j.Get("signals")[0].Set("kind", JsonValue.Of("number")), "boolean incident is not an invented numeric quantity");
            Invalid(c, j => Parts(j).Get("signal_transformation_gate").Get("conditions")[0].Set("source", JsonValue.Of("leak")), "incident episode part is not a substituted signal provider");
            JsonValue other = Json.Parse(FrameworkProofContent.Read("incident-phases.json"));
            var workflow = other.Get("incident_workflows")[0].Clone(); workflow.Set("id", JsonValue.Of("other.workflow")); other.Get("incident_workflows").Add(workflow);
            c.Throws<ContentException>(() => New(Factory(j => Parts(j).Get("incident_signal").Set("workflow", JsonValue.Of("other.workflow")), Text(other))),
                "even structurally equal workflow must match exact named source identity");
            c.That(Raise(game, target.Id).Accepted, "normal player action raises the actual incident");
            c.Equal(Fix.One, Signal(game, target.Id, "leak.active").Value.Value, "active signal reflects named current episode immediately");
            c.Equal(Fix.Zero, Signal(game, target.Id).Value.Value, "physical isolation is not complete merely because incident was raised");
            c.That(!target.Get<Incident>(Source).ReadLifecycle().ResolvedTick.Equals(game.Tick), "signal reads never instant-repair");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "new signal component does not create an independent saved hold value");
            c.Equal(Fix.Zero, signal.Value.Value, "captured preincident fact remains detached");
            Mapless(c);
        }
        private static void Mapless(Check c)
        {
            var definition = new GameDefinition("local-work", new GameModule[] { new ContentAccessModule(), new InventoryModule(),
                new MotionModule(), new WorkModule(), new WorldSetupModule(), new IncidentResponseModule(), new LocalWorkProofModule() });
            JsonValue manifest = Json.Parse(FrameworkProofContent.Read("local-work-manifest.json"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.incidents\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
            JsonValue content = Json.Parse(FrameworkProofContent.Read("local-work.json"));
            content.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("sim.incidents"));
            content.Set("worker_roles", Json.Parse("[{\"id\":\"repairer\"}]"));
            content.Set("response_effects", Json.Parse("[{\"id\":\"local.resolve\",\"bound_response\":true}]"));
            content.Get("activities").Add(Json.Parse("{\"id\":\"local.response\",\"automatic_supply\":false,\"nodes\":[{\"id\":\"repair\",\"executor\":\"work.staffed\",\"roles\":[\"repairer\"],\"duration\":{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"}}],\"on_complete\":\"local.resolve\"}"));
            content.Set("incident_workflows", Json.Parse("[{\"id\":\"local.incident\",\"kind\":\"local.jam\",\"response\":\"local.response\",\"phases\":[{\"id\":\"service\",\"nodes\":[\"repair\"]}]}]"));
            JsonValue parts = content.Get("entities")[0].Get("components");
            parts.Set("incident", Json.Parse("{\"id\":\"jam\",\"workflow\":\"local.incident\",\"states\":{\"active\":\"idle\"}}"));
            parts.Set("incident_signal", Json.Parse("{\"id\":\"sensor\",\"source\":\"jam\",\"workflow\":\"local.incident\",\"phase\":\"service\"}"));
            parts.Set("worker", Json.Parse("{\"roles\":[\"repairer\"],\"work_rate\":1,\"states\":{\"working\":\"idle\",\"loading\":\"idle\",\"stopped\":\"idle\"}}"));
            var files = new PreloadedFileSource().Add("manifest.json", Text(manifest)).Add("local-work.json", Text(content))
                .Add("framework/access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            var factory = new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
            SimGame game = OptionalWorkTests.New(factory); Entity target = null;
            foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == "local") target = entity;
            c.That(game.World.Space == null && !game.World.Content.HasModule(OperationsModule.ModuleId), "incident signal composition imposes no spatial/operations module dependency");
            c.That(target.Get<IAutomationSignalSource>(new Code("sensor")) is IncidentSignal, "mapless signal remains a real named engine capability");
            c.That(target.Get<Incident>(new Code("jam")).Raise(game.World, new Code("local.jam")), "engine-side mapless fixture raises its real incident");
            Tick(game); SimGame restored = factory.Load(game.Save());
            for (int i = 0; i < 20; i++) { Tick(game); Tick(restored); }
            c.That(!target.Get<Incident>(new Code("jam")).ReadLifecycle().Active, "ordinary native machine work resolves without maps, money or host response rules");
            c.Equal(game.Hash(), restored.Hash(), "mapless incident/signal registration preserves saved identity");
        }
        private static void Physical(Check c)
        {
            Batch(c); Immediate(c); Continuous(c); PermanentFailure(c);
        }
        private static void Batch(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Target(game);
            ResourceId steel = game.World.Content.ResourceId("steel"), product = game.World.Content.ResourceId("leak.product"); IInventory store = target.Get<IInventory>();
            c.That(Queue(game, target.Id).Accepted, "ordinary request starts a real material processor");
            c.That(State(game, target.Id).Progress.WholeTicks > 0 && State(game, target.Id).Wip.Stock.AmountOf(steel) == Fix.FromInt(2), "native WIP and elapsed batch work actually exist");
            c.That(Raise(game, target.Id).Accepted, "leak starts while processing is already running");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "running work and incident checkpoint restore together");
            Reach(game, target, () => Signal(game, target.Id).Value.Value == Fix.One, "physical isolation", twin);
            c.Equal(Blocked, Gates(game, target.Id).Blocker, "completed physical isolation closes actual readiness gate");
            TransformationSnapshot captured = State(game, target.Id); Tick(game); Tick(twin);
            c.Equal(captured.Progress, State(game, target.Id).Progress, "next processing update cannot add work after isolation phase400");
            c.Equal(Blocked, State(game, target.Id).Blocker, "native processor records the actual safety blocker");
            c.Equal(Fix.Zero, store.Amount(product), "paused batch cannot fabricate completed output");
            c.Equal(Fix.FromInt(2), State(game, target.Id).Wip.Stock.AmountOf(steel), "paused batch retains actual WIP instead of returning/reconsuming inputs");
            c.Equal(Fix.FromInt(2), State(game, target.Id).Tools.Held.AmountOf(steel), "native processor tools remain committed while unsafe");
            WorkBoard board = game.World.Services.Get<WorkBoard>(); Order response = board.ActiveResponseOn(target.Id, Source);
            game.World.System<WorkSystem>().Cancel(game.World, response); twin = factory.Load(game.Save());
            c.Equal(Fix.One, Signal(game, target.Id).Value.Value, "cancellation cannot undo already-earned isolation");
            c.Equal(game.Hash(), twin.Hash(), "cancelled response and gated WIP form a valid saved checkpoint");
            Tick(game); Tick(twin);
            c.That(board.ActiveResponseOn(target.Id, Source)?.Id == response.Id, "response resumes its own physical work without another safety store");
            Reach(game, target, () => response.Node("repair").State == NodeState.Done, "real repair", twin);
            c.That(response.Node("cleanup").State != NodeState.Done && target.Get<Incident>(Source).ReadLifecycle().Active, "repair alone cannot restart before authored cleanup");
            c.Equal(captured.Progress, State(game, target.Id).Progress, "cleanup blocker prevents production progress throughout repair");
            c.Equal(Fix.FromInt(24), store.Amount(steel), "WIP2 and one isolation plus three repair units spend exactly six native units");
            c.That(Control(game, target.Id, true).Accepted && Control(twin, target.Id, true).Accepted, "manual enable is intent, never a safety bypass");
            c.Equal(captured.Progress, State(game, target.Id).Progress, "enabled intent still cannot advance isolated batch");
            c.That(Control(game, target.Id, false).Accepted && Control(twin, target.Id, false).Accepted, "player can deliberately keep operation shut down");
            c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted &&
                TestKit.Order(twin, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted, "ordinary cleanup readiness opens the physical branch");
            Reach(game, target, () => !target.Get<Incident>(Source).ReadLifecycle().Active, "physical cleanup and restart", twin);
            Tick(game); Tick(twin);
            c.Equal(Fix.Zero, Signal(game, target.Id).Value.Value, "only actual response resolution releases phase hold");
            c.Equal(Code.None, Gates(game, target.Id).Blocker, "authored safety readiness is clear after whole graph finishes");
            c.That(game.Seat(0).View.Feature<OperationsView>().TryGetControl(target.Id, Process, out var control) && !control.Enabled, "resolution cannot override manual disabled intent");
            c.Equal(captured.Progress, State(game, target.Id).Progress, "manually disabled work remains paused after safety restart");
            c.Equal(game.Hash(), twin.Hash(), "timed response, cancellation and manual policy continue identically in saved twin");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "resolved incident and retained batch WIP restore together");
            c.That(Control(game, target.Id, true).Accepted && Control(twin, target.Id, true).Accepted, "player resumes an actually safe operation");
            Reach(game, target, () => State(game, target.Id).QueueCount == 0, "same batch completion", twin);
            c.Equal(Fix.FromInt(24), store.Amount(steel), "resumed batch consumes no duplicate startup material");
            c.Equal(Fix.One, store.Amount(product), "one actual batch produces exactly one native output");
            c.Equal(Fix.Zero, store.ReservedOut(steel), "both response and process tools release through existing books");
            c.Equal(game.Hash(), twin.Hash(), "batch output and cleared native receipts are deterministic");
            c.That(captured.QueueCount == 1 && captured.Wip.Stock.AmountOf(steel) == Fix.FromInt(2), "captured WIP/queue facts remain detached after completion");
        }
        private static void Immediate(Check c)
        {
            GameFactory factory = Factory(j => Parts(j).Get("signal_transformation_gate").Get("conditions")[0].Set("signal", JsonValue.Of("leak.active")));
            SimGame game = New(factory); Entity target = Target(game); ResourceId steel = game.World.Content.ResourceId("steel");
            c.That(Raise(game, target.Id).Accepted, "immediate shutdown policy starts from actual episode");
            c.Equal(Blocked, Gates(game, target.Id).Blocker, "active signal closes safety gate without invented physical isolation");
            c.That(Queue(game, target.Id).Accepted, "blocked physical execution can retain a normal requested queue");
            c.Equal(WorkProgress.Zero, State(game, target.Id).Progress, "active emergency gate prevents batch startup work");
            c.Equal(Fix.FromInt(30), target.Get<IInventory>().Amount(steel), "emergency wait cannot consume startup material before responders work");
            c.That(State(game, target.Id).Wip.Stock.IsEmpty && State(game, target.Id).Tools.Held.IsEmpty, "startup wait owns no fabricated WIP or tool receipts");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "immediate safe startup wait restores");
        }
        private static void Continuous(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Target(game); ResourceId product = game.World.Content.ResourceId("leak.product");
            c.That(Queue(game, target.Id, "leak.flow").Accepted && Raise(game, target.Id).Accepted, "real continuous process and leak start through commands");
            Reach(game, target, () => Signal(game, target.Id).Value.Value == Fix.One, "continuous isolation");
            Tick(game); WorkProgress progress = State(game, target.Id).Progress; Fix before = target.Get<IInventory>().Amount(product);
            SimGame twin = factory.Load(game.Save());
            for (int i = 0; i < 20; i++) { Tick(game); Tick(twin); }
            c.Equal(progress, State(game, target.Id).Progress, "continuous elapsed flow time cannot advance while isolated");
            c.Equal(before, target.Get<IInventory>().Amount(product), "continuous isolation creates no extra output");
            c.Equal(Blocked, State(game, target.Id).Blocker, "continuous process uses same real safety blocker");
            c.Equal(game.Hash(), twin.Hash(), "continuous flow budgets and gated response restore without an extra hold codec");
            c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted &&
                TestKit.Order(twin, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted, "continuous restart still requires physical cleanup");
            Reach(game, target, () => !target.Get<Incident>(Source).ReadLifecycle().Active, "continuous response finish", twin);
            for (int i = 0; i < 12; i++) { Tick(game); Tick(twin); }
            c.That(State(game, target.Id).Progress.WholeTicks > progress.WholeTicks && target.Get<IInventory>().Amount(product) > before,
                "continuous resource conversion resumes only after real response completion");
            c.Equal(game.Hash(), twin.Hash(), "resumed continuous flow outputs remain deterministic");
        }
        private static void PermanentFailure(Check c)
        {
            JsonValue work = Json.Parse(FrameworkProofContent.Read("incident-phases.json"));
            work.Get("activities")[0].Get("nodes")[2].Set("requirement", JsonValue.Of("leak.cleanup-permit"));
            GameFactory factory = Factory(j => Parts(j).Remove("work_permit"), Text(work)); SimGame game = New(factory); Entity target = Target(game);
            c.That(Queue(game, target.Id).Accepted && Raise(game, target.Id).Accepted, "failed physical response fixture starts normally");
            Reach(game, target, () => game.World.Services.Get<WorkBoard>().ResponseOn(target.Id, Source, 1)?.State == OrderState.Failed, "permanent response failure");
            Tick(game); c.Equal(Fix.One, Signal(game, target.Id).Value.Value, "failed repair cannot release completed isolation");
            c.Equal(Blocked, State(game, target.Id).Blocker, "failed incident continues to stop unsafe processing");
            SimGame twin = factory.Load(game.Save()); WorkProgress progress = State(game, target.Id).Progress;
            for (int i = 0; i < 20; i++) { Tick(game); Tick(twin); }
            c.Equal(progress, State(game, target.Id).Progress, "permanent failure cannot act as implicit restart readiness");
            c.Equal(game.Hash(), twin.Hash(), "failed safety hold survives deterministic saved continuation");
        }
    }
}
