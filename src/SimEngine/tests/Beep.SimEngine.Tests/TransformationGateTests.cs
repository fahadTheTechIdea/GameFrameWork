using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class TransformationGateTests
    {
        private static readonly Code Capability = new Code("transformation");
        private const string Machine = "gated.assembler";
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        public static void Register()
        {
            TestRunner.Add("Transformation gates: authored signals, named contracts and private previews", Contracts);
            TestRunner.Add("Transformation gates: timed waits, cancellation and saved provider faults", Execution);
            TestRunner.Add("Transformation gates: independent JSON metadata shares native timed execution and replay", IndependentMetadata);
        }
        private static GameFactory Factory(Action<JsonValue> edit = null, bool composite = false, bool independent = false)
        {
            JsonValue authored = Json.Parse(FrameworkProofContent.Read("transformation-gates.json"));
            if (composite) authored.Get("entities")[0].Get("components").Set("pulse_transformer",
                Json.Parse(FrameworkProofContent.Read("transformer-proof.json")).Get("entities")[0].Get("components").Get("pulse_transformer"));
            edit?.Invoke(authored);
            if (independent)
                for (int i = 0; i < authored.Get("entities").Count; i++)
                {
                    JsonValue components = authored.Get("entities")[i].Get("components");
                    JsonValue gate = components.Get("signal_transformation_gate");
                    if (gate == null) continue;
                    components.Remove("signal_transformation_gate"); components.Set("proof_signal_transformation_gate", gate);
                }
            return TransformerTests.Factory(process =>
            {
                process.Get("scenarios")[0].Get("settings").Get(WorldSetupModule.ModuleId).Set("profile", JsonValue.Of("transformation-gates.start"));
                process.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.transformation-gates"));
                process.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
                process.Get("transformations")[0].Get("tools").Set("supply", JsonValue.Number("2"));
                process.Get("transformations")[0].Get("tool_ports").Set("supply", JsonValue.Of("tool.input"));
            }, new TransformationGateProofModule(),
                "{\"id\":\"proof.transformation-gates\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"transformation-gates.json\"]}", Text(authored));
        }
        private static SimGame New(GameFactory factory) => TransformerTests.New(factory);
        private static Entity Find(SimGame game, string id = Machine) => TransformerTests.Find(game, id);
        private static IInventory Stock(SimGame game) => Find(game).Get<IInventory>();
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static TransformationSnapshot State(SimGame game) => TransformerTests.State(game, Machine);
        private static TransformationManagementView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<TransformationManagementView>();
        private static ExecutionGateEvaluation Gates(SimGame game)
        {
            Code refusal = View(game).ReadGates(Find(game).Id, Capability, "build", out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("gate view refused: " + refusal); return facts;
        }
        private static int Results(SimGame game)
        { int count = 0; foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "token") count++; return count; }
        private static ResourceFlowPage History(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
        private static CommandResult Queue(SimGame game, bool repeat = false, string prototype = Machine)
            => TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = Find(game, prototype).Id,
                Capability = prototype == Machine ? Capability : new Code("pulse"), Transformation = prototype == Machine ? "build" : "pulse", Count = 1, Repeat = repeat });
        private static void Tick(SimGame game, int count = 1)
        { for (int i = 0; i < count; i++) { game.Update(); if (game.Fault != null) throw new InvalidOperationException("gate tick=" + game.Tick + ": " + game.Fault); } }
        private static CommandResult Power(SimGame game, bool enabled, string prototype = Machine)
            => TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Find(game, prototype).Id, Operation = new Code("utility"), Enabled = enabled, Target = Fix.One });
        private static CommandResult Signal(SimGame game, Fix level, bool available)
            => TestKit.Order(game, 0, new SetAutomationProbeCommand { Entity = Find(game).Id, Level = level, Available = available });
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); ulong hash = game.Hash();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "authored gate semantics have current breaking identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no legacy gate/save reader");
            c.That(typeof(INamedEntityCapability).IsAssignableFrom(typeof(ITransformationGate)), "gates are named extensible contracts");
            c.That(typeof(ITransformationGate).GetMethod("CheckTransformation") == null, "Code-only gate path is deleted");
            var facts = Gates(game);
            c.Equal(Code.None, facts.Blocker, "actual available power and safe numeric signal admit");
            c.Equal(1, facts.Gates.Count, "native authored gate is the sole selected owner");
            c.Equal(new Code("safe-operation"), facts.Gates[0].Capability, "gate uses authored stable identity");
            c.Equal(2, facts.Gates[0].Conditions.Count, "all current condition facts are engine-calculated");
            c.Equal("a-power", facts.Gates[0].Conditions[0].Id, "condition order uses stable ID, not file position");
            c.Equal(Fix.One, facts.Gates[0].Conditions[0].Signal.Value.Value, "boolean provider fact remains typed");
            c.Equal(new Code("unit.level"), facts.Gates[0].Conditions[1].Signal.Unit, "numeric fact retains exact authored unit");
            var definition = Find(game).Def.Component<SignalTransformationGateDef>();
            c.That(ReferenceEquals(definition.Conditions[0].Signal, game.World.Content.Get<AutomationSignalDef>("utility.on")), "gate definition uses actual selected signal catalog alias, not a second registry");
            c.That(game.World.Space == null, "generic authored process gates impose no tile/map/platform dependency");
            c.Equal(hash, game.Hash(), "gate view creates no claims/timers/progress/state");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateSnapshot>)facts.Gates).Clear(), "gate list cannot mutate engine state");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateConditionSnapshot>)facts.Gates[0].Conditions).Clear(), "condition facts are detached immutable collections");
            c.Equal(Codes.NotFound, View(game, 1).ReadGates(Find(game).Id, Capability, "build", out var hidden), "foreign-company read refuses");
            c.That(hidden == null, "foreign gate/source facts are not leaked");
            c.Equal(Codes.NotFound, View(game).ReadGates(new EntityId(999), Capability, "build", out _), "unknown entity has no fabricated gate result");
            c.Equal(Codes.UnknownDef, View(game).ReadGates(Find(game).Id, Capability, "missing", out _), "unknown content refuses");
            c.Equal(EnqueueTransformationHandler.NotMadeHere, View(game).ReadGates(Find(game).Id, Capability, "pulse", out _), "unsupported transformation is not inferred");
            foreach (int level in new[] { 1, 4, 0, 5 })
            {
                c.That(Signal(game, Fix.FromInt(level), true).Accepted, "ordinary provider signal input: " + level);
                c.Equal(level == 1 || level == 4 ? Code.None : new Code("process.unsafe-pressure"), Gates(game).Blocker, "numeric gate uses inclusive exact native range: " + level);
            }
            Signal(game, Fix.FromInt(2), false);
            c.Equal(new Code("probe.sensor-offline"), Gates(game).Blocker, "unavailable signal retains actual typed blocker");
            c.That(!Gates(game).Gates[0].Conditions[0].Signal.Value.HasValue, "unavailable never becomes false/zero stock or a threshold value");
            Signal(game, Fix.FromInt(2), true); Power(game, false);
            c.Equal(new Code("process.no-power"), Gates(game).Blocker, "available false uses authored refusal");
            c.That(facts.Gates[0].Conditions[0].Ready, "retained old facts cannot become a mutable live execution handle");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "actual source controls save; gate creates no second codec/state");
            foreach (string invalid in new[] { "missing-id", "empty-transformations", "duplicate-transform", "unknown-transform", "unsupported-transform", "empty-conditions", "duplicate-condition",
                "missing-source", "wrong-source", "unknown-signal", "mixed-kind", "wrong-unit", "reversed", "missing-minimum", "missing-maximum", "mixed-fields", "missing-blocker", "implicit-boolean", "wrong-provider" })
            {
                c.Throws<ContentException>(() => New(Factory(json =>
                {
                    JsonValue gate = json.Get("entities")[0].Get("components").Get("signal_transformation_gate"), conditions = gate.Get("conditions"), range = conditions[0], power = conditions[1];
                    switch (invalid)
                    {
                        case "missing-id": gate.Remove("id"); break;
                        case "empty-transformations": gate.Set("transformations", Json.Parse("[]")); break;
                        case "duplicate-transform": gate.Get("transformations").Add(JsonValue.Of("build")); break;
                        case "unknown-transform": gate.Set("transformations", Json.Parse("[\"missing\"]")); break;
                        case "unsupported-transform": gate.Set("transformations", Json.Parse("[\"pulse\"]")); break;
                        case "empty-conditions": gate.Set("conditions", Json.Parse("[]")); break;
                        case "duplicate-condition": conditions.Add(Json.Parse(Text(power))); break;
                        case "missing-source": range.Remove("source"); break;
                        case "wrong-source": range.Set("source", JsonValue.Of("missing")); break;
                        case "unknown-signal": range.Set("signal", JsonValue.Of("missing")); break;
                        case "mixed-kind": power.Set("signal", JsonValue.Of("utility.level")); break;
                        case "wrong-unit": range.Set("unit", JsonValue.Of("unit.other")); break;
                        case "reversed": range.Set("minimum", JsonValue.Number("8")); break;
                        case "missing-minimum": range.Remove("minimum"); break;
                        case "missing-maximum": range.Remove("maximum"); break;
                        case "mixed-fields": power.Set("minimum", JsonValue.Number("0")); break;
                        case "missing-blocker": power.Remove("blocker"); break;
                        case "implicit-boolean": power.Remove("expected"); break;
                        case "wrong-provider": power.Set("source", JsonValue.Of("transformation")); break;
                    }
                })), "invalid authored gate fails before Initialize: " + invalid);
            }
            SimGame composite = New(Factory(composite: true));
            c.Equal(2, Gates(composite).Gates.Count, "native and independent named gate providers coexist without manager branches");
            c.Equal(new Code("pulse"), Gates(composite).Gates[0].Capability, "multiple gates compare by stable capability identity");
            c.That(TestKit.Order(composite, 0, new ConfigureOperationCommand { Entity = Find(composite).Id, Operation = new Code("pulse"), Enabled = false, Target = Fix.One }).Accepted,
                "ordinary control changes independent gate's real authority");
            c.Equal(new Code("pulse.utility-offline"), Gates(composite).Blocker, "independent contract gate participates in common reader");
            c.Equal(Code.None, View(game).ReadGates(Find(game, "gated.emitter").Id, new Code("pulse"), "pulse", out var external), "independent transformer exposes the same native gate facts");
            c.Equal(2, external.Gates.Count, "independent transformer and native JSON gate share one evaluator");
            Applicability(c);
        }
        private static void Applicability(Check c)
        {
            SimGame game = New(Factory(json => json.Get("entities")[0].Get("components").Get("transformer").Get("transformations").Add(JsonValue.Of("pulse"))));
            Power(game, false); ulong hash = game.Hash();
            c.Equal(Code.None, View(game).ReadGates(Find(game).Id, Capability, "pulse", out var facts), "explicit unrelated supported transformation can be inspected");
            c.That(!facts.Gates[0].Applies && facts.Gates[0].Conditions.Count == 0 && facts.Blocker.IsNone, "authored scope cannot block unrelated work or expose fabricated condition readings");
            c.Equal(hash, game.Hash(), "inapplicable gate projection is pure");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = Find(game).Id, Capability = Capability, Transformation = "pulse", Count = 1 }).Accepted,
                "ungated authored work starts despite a different utility requirement being false");
            Tick(game); c.Equal(1, Results(game), "ungated entity-only work uses actual native duration/completion");
        }
        private static void Execution(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); c.That(Power(game, false).Accepted, "player disables actual utility source");
            long history = History(game).Latest;
            c.That(Queue(game).Accepted, "valid persistent process intent can queue while its utility is blocked");
            c.Equal(0L, State(game).Progress.WholeTicks, "blocked startup earns no game work");
            c.Equal(Fix.FromInt(20), Stock(game).Amount(Supply(game)), "blocked startup moves no material");
            c.Equal(0, Stock(game).Reservations.Count, "blocked startup acquires no tools/claims");
            c.Equal(history, History(game).Latest, "blocked startup publishes no stock prefix");
            c.Equal(new Code("process.no-power"), State(game).Blocker, "native execution uses same engine gate as preview");
            SimGame twin = factory.Load(game.Save()); Tick(game, 3); Tick(twin, 3); c.Equal(game.Hash(), twin.Hash(), "saved startup wait remains deterministic");
            game.Seat(0).Submit(new ConfigureOperationCommand { Entity = Find(game).Id, Operation = new Code("utility"), Enabled = true, Target = Fix.One });
            SimGame pending = factory.Load(game.Save());
            twin.Seat(0).Submit(new ConfigureOperationCommand { Entity = Find(twin).Id, Operation = new Code("utility"), Enabled = true, Target = Fix.One });
            Tick(game); Tick(twin); Tick(pending);
            c.Equal(game.Hash(), pending.Hash(), "saved pending ordinary source control reopens actual process without a gate-specific command/timer");
            c.Equal(game.Hash(), twin.Hash(), "resumed startup allocates identical existing execution/claims");
            c.Equal(1L, State(game).Progress.WholeTicks, "utility availability earns first real work tick only");
            c.Equal(Fix.FromInt(3), State(game).Wip.Stock.AmountOf(Supply(game)), "physical input remains in real WIP");
            c.Equal(Fix.FromInt(2), State(game).Tools.Held.AmountOf(Supply(game)), "actual reusable tools remain held");
            Power(game, false); Power(twin, false); long progress = State(game).Progress.WholeTicks;
            Tick(game, 3); Tick(twin, 3);
            c.Equal(progress, State(game).Progress.WholeTicks, "mid-batch outage advances no progress");
            c.Equal(Fix.FromInt(3), State(game).Wip.Stock.AmountOf(Supply(game)), "outage preserves actual WIP");
            c.Equal(Fix.FromInt(2), State(game).Tools.Held.AmountOf(Supply(game)), "outage preserves actual owned tools");
            c.Equal(game.Hash(), twin.Hash(), "saved/interleaved source state remains isolated");
            SimGame saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "mid-batch gate wait restores with sole native codecs");
            Power(game, true); Power(twin, true); Tick(game, 4); Tick(twin, 4);
            c.Equal(1, Results(game), "restored utility completes one timed actual result");
            c.Equal(game.Hash(), twin.Hash(), "resumed result/resource completion is twin-identical");
            c.Equal(Fix.FromInt(18), Stock(game).Amount(Supply(game)), "one batch consumes raw material and produces one byproduct");
            c.Equal(0, Stock(game).Reservations.Count, "completion releases actual tool claims once");
            CancelBlocked(c, factory); CompletedWait(c, factory); Independent(c, factory); Faults(c);
        }
        private static void IndependentMetadata(Check c)
        {
            GameFactory factory = Factory(independent: true); SimGame game = New(factory);
            var definition = Find(game).Def.Component<ISignalTransformationGateDefinition>(new Code("safe-operation"));
            c.That(definition is ProofSignalTransformationGateDef && !(definition is SignalTransformationGateDef),
                "master-listed independent metadata does not inherit the native definition");
            c.That(Find(game).Get<ITransformationGate>(new Code("safe-operation")) is SignalTransformationGate,
                "independent definition uses the one original stateless gate runtime");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000);
            using var replay = game.RecordReplay(limits);
            ulong hash = game.Hash(); var retained = Gates(game);
            c.Equal(hash, game.Hash(), "metadata admission and live gate previews remain pure");
            c.Equal(Codes.NotFound, View(game, 1).ReadGates(Find(game).Id, Capability, "build", out var foreign),
                "foreign metadata binding reveals no production facts");
            c.That(foreign == null, "foreign gate publication has no prefix");
            c.That(Power(game, false).Accepted && Queue(game).Accepted, "ordinary player commands queue blocked production");
            Tick(game, 3);
            c.Equal(0L, State(game).Progress.WholeTicks, "blocked independent metadata earns no work");
            c.Equal(Fix.FromInt(20), Stock(game).Amount(Supply(game)), "blocked startup consumes no stock");
            c.That(Power(game, true).Accepted, "ordinary control enables actual signal source");
            c.Equal(1L, State(game).Progress.WholeTicks, "the native processor earns its first elapsed work step");
            c.That(Power(game, false).Accepted, "ordinary control interrupts the physical batch");
            long progress = State(game).Progress.WholeTicks;
            SimGame twin = factory.Load(game.Save()); Tick(game, 3); Tick(twin, 3);
            c.Equal(progress, State(game).Progress.WholeTicks, "outage preserves earned work");
            c.Equal(game.Hash(), twin.Hash(), "saved independent metadata restores existing processor/source state");
            c.Equal(Fix.FromInt(3), State(game).Wip.Stock.AmountOf(Supply(game)), "outage retains real raw WIP");
            c.Equal(Fix.FromInt(2), State(game).Tools.Held.AmountOf(Supply(game)), "outage retains actual reusable commitments");
            c.That(Power(game, true).Accepted && Power(twin, true).Accepted, "both saved sessions restart through normal controls");
            Tick(game, 4); Tick(twin, 4);
            c.Equal(1, Results(game), "original runtime completes one authored timed result");
            c.Equal(Fix.FromInt(18), Stock(game).Amount(Supply(game)), "raw input and byproduct conserve native stock");
            c.Equal(0, Stock(game).Reservations.Count, "completion releases reusable tools once");
            c.Equal(game.Hash(), twin.Hash(), "saved resumed completion remains identical");
            c.That(retained.Gates[0].Conditions[0].Ready, "retained pre-outage facts are detached");
            var verification = new ReplayVerifier().Verify(factory, replay.Finish(), limits);
            c.That(verification.Success, "complete ordinary input stream replays through original engine owners");
            c.Equal(game.Hash(), verification.ActualHash, "replay reaches the actual final live state");
            Action<JsonValue> addUnused = json => {
                JsonValue unused = Json.Parse(Text(json.Get("entities")[0]));
                unused.Set("id", JsonValue.Of("unused.gated.assembler")); json.Get("entities").Add(unused);
            };
            SimGame unusedValid = New(Factory(addUnused, independent: true));
            c.That(Find(unusedValid, "unused.gated.assembler") == null, "valid independent metadata can be loaded without spawning its prototype");
            foreach (string invalid in new[] { "unsupported-transform", "wrong-source" })
            {
                try
                {
                    New(Factory(json => {
                        addUnused(json);
                        JsonValue entities = json.Get("entities");
                        JsonValue gate = entities[entities.Count - 1].Get("components").Get("signal_transformation_gate");
                        if (invalid == "unsupported-transform") gate.Set("transformations", Json.Parse("[\"pulse\"]"));
                        else gate.Get("conditions")[0].Set("source", JsonValue.Of("missing"));
                    }, independent: true));
                    c.That(false, "unused malformed independent gate must refuse: " + invalid);
                }
                catch (ContentException error)
                {
                    c.That(error.Message.Contains("unused.gated.assembler") && error.Message.Contains(invalid == "unsupported-transform"
                        ? "unsupported transformation" : "compatible signal source"),
                        "engine refuses the actual unused binding, not unrelated setup: " + invalid + ": " + error.Message);
                }
            }
        }
        private static void CancelBlocked(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Queue(game); Signal(game, Fix.FromInt(2), false);
            c.That(State(game).Progress.WholeTicks > 0 && State(game).Blocker == new Code("probe.sensor-offline"), "actual unavailable sensor stops active batch");
            c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = Find(game).Id, Capability = Capability, Index = 0 }).Accepted,
                "ordinary cancel can return materials despite production gate refusal");
            Tick(game); c.That(State(game).QueueCount == 0 && Results(game) == 0, "cancel emits no result or duplicate execution");
            c.Equal(Fix.FromInt(20), Stock(game).Amount(Supply(game)), "raw input returns, not fabricated refund");
            c.Equal(0, Stock(game).Reservations.Count, "cancel retires own tools after actual return");
            game = New(factory); Queue(game); Stock(game).Add(Supply(game), Fix.FromInt(23)); Power(game, false);
            c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = Find(game).Id, Capability = Capability, Index = 0 }).Accepted,
                "cancel can enter actual full-storage return while utility is offline");
            c.That(State(game).Phase == TransformationPhase.Returning && State(game).Blocker == TransformerManager.BlockerReturnFull,
                "production gate cannot replace native capacity/disposition refusal");
            c.Equal(Fix.FromInt(2), State(game).Tools.Held.AmountOf(Supply(game)), "blocked cancellation retains its actual tools");
            SimGame twin = factory.Load(game.Save()); Tick(game, 2); Tick(twin, 2); c.Equal(game.Hash(), twin.Hash(), "saved full return stays deterministic independently of utility state");
            Stock(game).Take(Supply(game), Fix.FromInt(3)); Stock(twin).Take(Supply(twin), Fix.FromInt(3));
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "physical capacity release permits return even with utility unavailable");
            c.That(State(game).QueueCount == 0 && Results(game) == 0 && Stock(game).Reservations.Count == 0, "full-return completion cancels without new output or leaked tool claims");
        }
        private static void CompletedWait(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Queue(game, repeat: true); Stock(game).Add(Supply(game), Fix.FromInt(23));
            Tick(game, 5); c.Equal(TransformationPhase.Blocked, State(game).Phase, "completed batch waits on actual output capacity");
            Power(game, false); Stock(game).Take(Supply(game), Fix.One);
            Tick(game, 2); c.Equal(0, Results(game), "capacity release does not bypass an unavailable required utility");
            c.Equal(6L, State(game).Progress.WholeTicks, "gate retains earned complete work");
            SimGame twin = factory.Load(game.Save()); Power(game, true); Power(twin, true);
            c.Equal(1, Results(game), "reopening actual utility releases one completed result");
            c.Equal(game.Hash(), twin.Hash(), "completed wait restores exact current source and output conditions");
            Tick(game); Tick(twin); c.Equal(2L, State(game).Wip.Execution, "repeat allocates a fresh physical execution");
            Signal(game, Fix.Zero, true); Signal(twin, Fix.Zero, true); Tick(game, 2); Tick(twin, 2);
            c.Equal(new Code("process.unsafe-pressure"), State(game).Blocker, "range gate stops repeated physical work");
            c.Equal(game.Hash(), twin.Hash(), "repeat gate wait is deterministic");
        }
        private static void Independent(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Power(game, false, "gated.emitter");
            c.That(Queue(game, prototype: "gated.emitter").Accepted, "independent engine provider accepts blocked timed intent");
            Tick(game, 3); c.Equal(0, Results(game), "independent provider honors native authored signal gate");
            SimGame twin = factory.Load(game.Save()); Power(game, true, "gated.emitter"); Power(twin, true, "gated.emitter"); Tick(game); Tick(twin);
            c.Equal(1, Results(game), "independent provider completes actual entity after authored work");
            c.Equal(game.Hash(), twin.Hash(), "independent gated provider saves through its existing codec");
        }
        private static void Faults(Check c)
        {
            foreach (int fault in new[] { 1, 2 })
            {
                SimGame broken = New(Factory()); Queue(broken); ulong history = HistoryFingerprint(broken);
                Find(broken).Get<AutomationProbe>().FaultMode = fault; broken.Update();
                Faulted(c, broken, history, "signal provider fault " + fault);
                c.Equal(0, Results(broken), "malformed/pure-mutating signal creates no result");
                SimGame masked = New(Factory(composite: true));
                TestKit.Order(masked, 0, new ConfigureOperationCommand { Entity = Find(masked).Id, Operation = new Code("pulse"), Enabled = false, Target = Fix.One });
                Queue(masked); history = HistoryFingerprint(masked); Find(masked).Get<AutomationProbe>().FaultMode = fault; masked.Update();
                Faulted(c, masked, history, "later signal fault behind earlier ordinary blocked gate " + fault);
            }
            foreach (int fault in new[] { 4, 5, 6, 7 })
            {
                SimGame broken = New(Factory(composite: true)); Power(broken, false);
                Queue(broken); ulong history = HistoryFingerprint(broken); Find(broken).Get<PulseTransformer>().FaultMode = fault;
                broken.Update(); Faulted(c, broken, history, "independent gate fault " + fault);
                c.Equal(0, Stock(broken).Reservations.Count, "blocked earlier gate cannot hide invalid later pure facts or acquire tools");
            }
        }
        // Negative protocol inspection only: the journal's original codec, not a valid whole-world checkpoint.
        private static ulong HistoryFingerprint(SimGame game)
        {
            var hash = new StateHasher();
            ((IPersistentService)game.World.Services.Get<IResourceFlowJournal>()).Sync(hash);
            return hash.Value;
        }
        private static void Faulted(Check c, SimGame game, ulong history, string reason)
        {
            c.Equal(GameState.Faulted, game.State, reason + " faults normal engine Update");
            c.Equal(history, HistoryFingerprint(game), reason + " changes no actual journal bytes or completion prefix");
            c.Throws<InvalidOperationException>(() => History(game), reason + " rejects public journal reads after first fault");
            c.That(!game.World.Events.HasPending, reason + " queues no new success facts");
            c.Throws<InvalidOperationException>(() => game.Save(), reason + " cannot save partial execution");
            c.Throws<InvalidOperationException>(() => game.Hash(), reason + " cannot hash a resumable state");
        }
    }
}
