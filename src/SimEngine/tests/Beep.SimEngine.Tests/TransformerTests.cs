using System;
using System.Collections.Generic;
using System.Linq;
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
    public static class TransformerTests
    {
        internal const string RetirementCase = "Transformation lifecycle boundary: queued native and independent executions cannot be retired";
        public static void Register()
        {
            TestRunner.Add(RetirementCase, Retirement);
            TestRunner.Add("Transformations: contract dispatch, authored content and private management", Contracts);
            TestRunner.Add("Transformations: timed native and independent providers with saved continuation", Processes);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        internal static GameFactory Factory(Action<JsonValue> edit = null, GameModule additionalModule = null,
            string additionalDeclaration = null, string additionalDocument = null, string transformerDocument = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null, Action<JsonValue> manifestEdit = null)
        {
            if (additionalModule == null && (additionalDeclaration != null || additionalDocument != null) ||
                additionalModule != null && (additionalDeclaration == null || additionalDocument == null))
                throw new ArgumentException("additional proof module, declaration and document must be supplied together");
            var modules = new List<GameModule> {
                new InventoryModule(), new OperationsModule(), new ContentAccessModule(), new TransformationModule(), new WorldSetupModule(),
                new ProcessProofModule(), new TransformerProofModule(), new ResourceFlowsModule() };
            if (additionalModule != null) modules.Add(additionalModule);
            var definition = new GameDefinition("transformations", modules);
            JsonValue process = Json.Parse(FrameworkProofContent.Read("process.json"));
            process.Get("scenarios")[0].Get("settings").Get(WorldSetupModule.ModuleId).Set("profile", JsonValue.Of("transformer.start"));
            process.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            process.Get("scenarios")[0].Set("default_seats", JsonValue.Number("2"));
            process.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.transformer")); edit?.Invoke(process);
            JsonValue manifest = Json.Parse(FrameworkProofContent.Read("process-manifest.json"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"proof.transformer\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"transformer-proof.json\"]}"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"flows.json\"]}"));
            manifestEdit?.Invoke(manifest);
            JsonValue additional = null;
            if (additionalModule != null)
            {
                additional = Json.Parse(additionalDeclaration);
                if (additional.Get("documents").Count != 1 && additionalDocuments == null) throw new ArgumentException("explicit additional proof documents required");
                manifest.Get("modules").Add(additional);
            }
            var files = new PreloadedFileSource().Add("manifest.json", Text(manifest)).Add("process.json", Text(process))
                .Add("framework/access.json", FrameworkProofContent.Access)
                .Add("transformer-proof.json", transformerDocument ?? FrameworkProofContent.Read("transformer-proof.json"))
                .Add("flows.json", FrameworkProofContent.Read("endpoints-flows.json"));
            if (additionalModule != null)
            {
                for (int i = 0; i < additional.Get("documents").Count; i++)
                {
                    string path = additional.Get("documents")[i].Text;
                    if (additionalDocuments == null) files.Add(path, additionalDocument);
                    else
                    {
                        if (!additionalDocuments.TryGetValue(path, out var data)) throw new ArgumentException("listed proof document is missing: " + path);
                        files.Add(path, data);
                    }
                }
            }
            if (additionalDocuments != null) foreach (var pair in additionalDocuments)
            {
                bool listed = false;
                for (int i = 0; i < additional.Get("documents").Count; i++) if (additional.Get("documents")[i].Text == pair.Key) listed = true;
                if (!listed) files.Add(pair.Key, pair.Value);
            }
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "process", Seed = 31 });
        internal static Entity Find(SimGame game, string prototype)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype) return entity; return null; }
        private static int Count(SimGame game, string prototype)
        { int count = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype) count++; return count; }
        private static Code Named(string prototype) => new Code(prototype == "emitter" ? "pulse" : "transformation");
        // Explicit negative lifecycle protocol lens. Setup/cancellation use ordinary player commands;
        // direct World/Upgrades calls below attempt only forbidden retirement of saved active work.
        private static void Retirement(Check c)
        {
            foreach (string prototype in new[] { "assembler", "emitter" }) foreach (bool working in new[] { false, true })
            {
                string context = prototype + (working ? "/working" : "/queued");
                var factory = Factory(); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                EntityId id = game.Seat(0).Snapshot.Entities.Single(row => row.Def == prototype).Id;
                TransformationSnapshot Facts(SimGame session)
                {
                    if (!View(session).TryGetTransformation(id, Named(prototype), out var facts)) throw new InvalidOperationException("retirement facts missing " + context);
                    return facts;
                }
                c.That(typeof(IEntityRemovalGuard).IsAssignableFrom(typeof(ITransformer)), "every processor contract requires explicit retirement protection " + context);
                Code idleRemoval = prototype == "assembler" ? ResourceTransactions.UndisposedStock : Code.None;
                c.Equal(idleRemoval, game.World.CheckDespawn(game.World.Entities.Get(id)), "idle processor retains the independent native-stock disposal guard " + context);
                if (!working) c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = id, Operation = Named(prototype), Enabled = false, Target = Fix.One }).Accepted,
                    "ordinary stop keeps the future request unstarted " + context);
                c.That(Queue(game, prototype, prototype == "emitter" ? "pulse" : "build").Accepted, "ordinary queue owns one actual execution " + context);
                var original = Facts(game);
                c.That(original.QueueCount == 1 && (working ? original.Progress.WholeTicks > 0 : original.Progress.Equals(WorkProgress.Zero)),
                    "probe distinguishes earned work from queued intent " + context);
                c.That(prototype == "emitter" ? original.Wip.Stock.IsEmpty : working ? !original.Wip.Stock.IsEmpty : original.Wip.Stock.IsEmpty,
                    "independent no-WIP and native real-WIP execution facts stay distinct " + context);
                byte[] checkpoint = game.Save(); game = factory.Load(checkpoint); var entity = game.World.Entities.Get(id); ulong before = game.Hash();
                c.Equal(idleRemoval.IsNone ? TransformerManager.BlockerActive : idleRemoval, game.World.CheckDespawn(entity), "ordinary removal admission protects stock and the queue " + context);
                c.Equal(TransformerManager.BlockerActive, Upgrades.Check(game.World, entity, entity.Def), "replacement admission protects the same queue " + context);
                c.Equal(before, game.Hash(), "both lifecycle previews preserve original receipts/work/identity " + context);
                var removal = factory.Load(checkpoint); var removedEntity = removal.World.Entities.Get(id);
                c.Throws<InvalidOperationException>(() => removal.World.Despawn(removedEntity), "actual forbidden despawn refuses before erasing work " + context);
                c.Equal(before, removal.Hash(), "refused despawn cannot erase execution or material " + context);
                var replacement = factory.Load(checkpoint); var replacedEntity = replacement.World.Entities.Get(id);
                Code admission = Upgrades.Check(replacement.World, replacedEntity, replacedEntity.Def);
                if (admission == TransformerManager.BlockerActive)
                {
                    Code refused = Upgrades.TryReplace(replacement.World, replacedEntity, replacedEntity.Def, out var made);
                    c.That(refused == TransformerManager.BlockerActive && made == null, "actual forbidden replacement publishes no new entity " + context);
                }
                else c.That(false, "unsafe active-work admission cannot proceed to a replacement in this non-replacement fixture " + context);
                c.Equal(before, replacement.Hash(), "refused replacement preserves entity sequence, work and material " + context);
                c.That(removal.Fault == null && replacement.Fault == null, "ordinary active-work refusals are not fatal faults " + context);
                c.Equal(original.Queue[0].Execution, Facts(game).Queue[0].Execution, "saved active request retains its original execution identity " + context);
                var limits = new ReplayLimits(4 * 1024 * 1024, 1000); using var recording = game.RecordReplay(limits);
                c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = id, Capability = Named(prototype), Index = 0 }).Accepted,
                    "normal cancellation, not forced retirement, releases owned work " + context);
                for (int i = 0; i < 8 && Facts(game).QueueCount != 0; i++) { game.Update(); if (game.Fault != null) throw new InvalidOperationException("retirement cancellation fault " + game.Fault); }
                var terminal = Facts(game);
                c.That(terminal.QueueCount == 0 && terminal.Wip.Stock.IsEmpty && terminal.Progress.Equals(WorkProgress.Zero), "cancellation returns native material and clears only its request " + context);
                c.Equal(idleRemoval, game.World.CheckDespawn(game.World.Entities.Get(id)), "cancelled processor releases work but never discards native stock " + context);
                c.Equal(Code.None, Upgrades.Check(game.World, game.World.Entities.Get(id), game.World.Entities.Get(id).Def), "cancelled processor may now be replaced " + context);
                c.Equal(0, game.Seat(0).Snapshot.Entities.Count(row => row.Def == "token"), "cancellation never manufactures an output " + context);
                c.Equal(1L, recording.CommandCount, "replay contains only normal cancel, never raw lifecycle probes " + context);
                var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == game.Hash(), "actual saved cancellation replays identically " + context);
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "empty execution remains durably healthy " + context);
                c.That(!View(game, 1).TryGetTransformation(id, Named(prototype), out _), "another company cannot read processor history " + context);
                c.Equal(isolatedHash, isolated.Hash(), "retirement refusals affect no independent session " + context);
            }
        }
        private static TransformationManagementView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<TransformationManagementView>();
        internal static TransformationSnapshot State(SimGame game, string prototype = "assembler")
        { if (!View(game).TryGetTransformation(Find(game, prototype).Id, Named(prototype), out var snapshot)) throw new InvalidOperationException("missing facts"); return snapshot; }
        internal static CommandResult Queue(SimGame game, string prototype, string transformation, int count = 1, bool repeat = false)
            => TestKit.Order(game, 0, new EnqueueTransformationCommand
            {
                Transformer = Find(game, prototype).Id,
                Capability = Named(prototype),
                Transformation = transformation,
                Count = count,
                Repeat = repeat
            });
        internal static void Twin(Check check, GameFactory factory, SimGame game)
        {
            SimGame first = factory.Load(game.Save()), second = factory.Load(game.Save());
            check.Equal(game.Hash(), first.Hash(), "provider-owned queue/progress/control restore once");
            first.Update(); second.Update(); check.Equal(first.Hash(), second.Hash(), "restored providers continue identically");
            first.Update(); second.Update(); check.Equal(first.Hash(), second.Hash(), "interleaved sessions retain isolated transformation state");
        }
        private static void Contracts(Check check)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity native = Find(game, "assembler"), pulse = Find(game, "emitter");
            check.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "breaking contract migration has current build");
            check.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "named command/content/provider checkpoint identity is breaking");
            foreach (string old in new[] { "Producer", "ProducerDef", "RecipeDef", "ProductionSystem", "ProductionModule" })
                check.That(typeof(SimGame).Assembly.GetType("Beep.SimEngine.Standard." + old) == null, "old runtime path deleted: " + old);
            check.That(game.World.Space == null && !game.World.Services.Has<IEconomy>(), "map and money remain optional");
            check.That(native.Get<ITransformer>(Named("assembler")) is Transformer && pulse.Get<ITransformer>(Named("emitter")) is PulseTransformer,
                "same named contract resolves unrelated runtime providers");
            check.That(pulse.Get<Transformer>() == null && !pulse.Has<IInventory>(), "independent process does not inherit or wrap the native inventory transformer");
            ulong hash = game.Hash();
            check.Equal(Code.None, View(game).PreviewEnqueue(native.Id, Named("assembler"), "build", 51, false, default), "no arbitrary fifty-entry queue limit");
            check.Equal(Code.None, View(game).PreviewEnqueue(pulse.Id, Named("emitter"), "pulse", 2, false, default), "same pure preview admits an external provider");
            check.Equal(hash, game.Hash(), "all successful previews remain pure");
            check.Equal(Codes.InvalidArgument, View(game).PreviewEnqueue(native.Id, Code.None, "build", 1, false, default), "named capability cannot be inferred");
            check.Equal(Codes.NotFound, View(game).PreviewEnqueue(native.Id, new Code("missing"), "build", 1, false, default), "wrong named capability refuses");
            check.Equal(Codes.UnknownDef, View(game).PreviewEnqueue(native.Id, Named("assembler"), "missing", 1, false, default), "missing content refuses before provider call");
            check.Equal(Codes.InvalidArgument, View(game).PreviewEnqueue(native.Id, Named("assembler"), "build", 0, false, default), "zero batch count refuses");
            check.Equal(Codes.NotFound, View(game, 1).PreviewEnqueue(native.Id, Named("assembler"), "build", 1, false, default), "private management does not reveal foreign capabilities");
            check.Equal(Codes.NotFound, TestKit.Order(game, 1, new EnqueueTransformationCommand
            {
                Transformer = native.Id,
                Capability = Named("assembler"),
                Transformation = "build"
            }).Refusal, "ordinary foreign command returns its exact ownership refusal");
            check.That(!View(game, 1).TryGetTransformation(native.Id, Named("assembler"), out _), "foreign progress stays private");
            check.That(Queue(game, "assembler", "build", 51).Accepted && State(game).QueueCount == 51, "actual command admits more than fifty without a parallel queue");
            TransformationSnapshot retained = State(game); var copied = new List<TransformationQueueEntry>();
            check.That(View(game).CopyQueue(native.Id, Named("assembler"), copied) && copied.Count == 51, "queue facts are projected through the capability");
            check.Throws<NotSupportedException>(() => ((IList<TransformationQueueEntry>)retained.Queue).Clear(), "snapshot queue is immutable");
            check.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = native.Id, Capability = Named("assembler"), Index = 50 }).Accepted,
                "tail cancellation goes through the same provider");
            check.That(retained.QueueCount == 51 && State(game).QueueCount == 50, "retained snapshot never follows live queue mutation");
            check.Equal(Codes.InvalidArgument, View(game).PreviewCancel(native.Id, Named("assembler"), 50), "cancel preview uses actual current indices");
            Twin(check, factory, game);
            foreach (string invalid in new[] { "missing", "zero", "negative", "unit", "scalar-duration", "unsupported", "cost", "supply", "duplicate", "auto-repeat" })
                check.Throws<ContentException>(() => New(Factory(json =>
                {
                    JsonValue row = json.Get("transformations")[0], duration = row.Get("duration");
                    switch (invalid)
                    {
                        case "missing": row.Remove("duration"); break;
                        case "zero": duration.Set("numerator", JsonValue.Number("0")); break;
                        case "negative": duration.Set("numerator", JsonValue.Number("-1")); break;
                        case "unit": duration.Set("unit", JsonValue.Of("wall-clock")); break;
                        case "scalar-duration": row.Set("duration", JsonValue.Number("1")); break;
                        case "unsupported": json.Get("entities")[0].Get("components").Get("transformer").Get("transformations").Add(JsonValue.Of("missing")); break;
                        case "cost": row.Set("cash", JsonValue.Number("-1")); break;
                        case "supply": json.Get("entities")[0].Get("components").Get("transformer").Remove("automatic_supply"); break;
                        case "duplicate": json.Get("entities")[0].Get("components").Get("transformer").Get("transformations").Add(JsonValue.Of("build")); break;
                        case "auto-repeat": json.Get("entities")[0].Get("components").Get("transformer").Set("idle_transformation", JsonValue.Of("missing")); break;
                    }
                })), "invalid explicit transformation content refuses before tick one: " + invalid);
            Checkpoints(check); Faults(check);
        }
        private static void Processes(Check check)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity native = Find(game, "assembler");
            ResourceId supply = game.World.Content.ResourceId("supply");
            check.That(Queue(game, "assembler", "build", 2).Accepted, "ordinary player intent starts native physical batch");
            check.That(State(game).Progress.WholeTicks == 1 && Count(game, "token") == 0, "manager calls provider once per tick and entity output is timed");
            check.Equal(Fix.FromInt(17), native.Get<IInventory>().Amount(supply), "first native input batch consumes exactly once through transactions");
            Twin(check, factory, game);
            check.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = native.Id, Operation = Named("assembler"), Enabled = false, Target = Fix.One }).Accepted,
                "ordinary operation controls pause the actual provider");
            WorkProgress stopped = State(game).Progress; game.RunTicks(3);
            check.Equal(stopped, State(game).Progress, "disabled native transformer retains elapsed progress");
            check.Equal(Fix.FromInt(17), native.Get<IInventory>().Amount(supply), "disabled native transformer does not consume again");
            Twin(check, factory, game);
            TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = native.Id, Operation = Named("assembler"), Enabled = true, Target = Fix.One });
            game.RunTicks(12);
            check.That(Count(game, "token") == 2 && State(game).QueueCount == 0, "sequential native batches release two real entities once");
            check.Equal(Fix.FromInt(16), native.Get<IInventory>().Amount(supply), "two input/output batches preserve exact native accounting");
            Twin(check, factory, game);
            GameFactory automatic = Factory(json => json.Get("entities")[0].Get("components").Get("transformer").Set("idle_transformation", JsonValue.Of("build")));
            SimGame repeated = New(automatic); repeated.RunTicks(12);
            check.That(Count(repeated, "token") == 2 && State(repeated).QueueCount == 0, "authored auto-repeat executes timed batches, not a fictitious continuous stream");
            Twin(check, automatic, repeated);
            SimGame missingHome = New(factory); Entity baseEntity = Find(missingHome, "emitter"), factoryEntity = Find(missingHome, "assembler");
            ulong noHomeHash = missingHome.Hash();
            check.Equal(Codes.InvalidArgument, missingHome.Seat(0).Preview(new EnqueueTransformationCommand
            {
                Transformer = factoryEntity.Id,
                Capability = Named("assembler"),
                Transformation = "build",
                Home = baseEntity.Id
            }), "nonworker entity output cannot accept meaningless worker-home intent");
            check.Equal(noHomeHash, missingHome.Hash(), "inapplicable home preview changes no queue, progress or stock");
            SimGame pulse = New(factory); Entity emitter = Find(pulse, "emitter");
            check.That(Queue(pulse, "emitter", "pulse", 2).Accepted && Count(pulse, "token") == 0, "unrelated provider starts real timed work through unchanged command/manager");
            check.Equal(pulse.World.Content.Get<IEntityStateDefinition>("working").State, emitter.State, "Game.Update calls entity.Update to settle provider-authored state");
            Twin(check, factory, pulse); pulse.Update();
            check.That(Count(pulse, "token") == 1 && State(pulse, "emitter").QueueCount == 1, "external provider creates the first real engine entity after two ticks");
            pulse.RunTicks(2); check.That(Count(pulse, "token") == 2 && State(pulse, "emitter").QueueCount == 0, "same manager advances the next external batch once");
            Twin(check, factory, pulse);
            check.That(Queue(pulse, "emitter", "pulse", repeat: true).Accepted, "repeat stays in the provider's native queue");
            pulse.RunTicks(5); check.That(Count(pulse, "token") == 5 && State(pulse, "emitter").QueueCount == 1, "each completed repeated execution creates one actual result");
            check.That(TestKit.Order(pulse, 0, new CancelTransformationCommand { Transformer = emitter.Id, Capability = Named("emitter"), Index = 0 }).Accepted,
                "external active repeat cancels through the same contract");
            int completed = Count(pulse, "token"); pulse.RunTicks(4); check.Equal(completed, Count(pulse, "token"), "cancelled repeat cannot emit another result");
            Twin(check, factory, pulse);
            SimGame independent = New(factory); check.That(State(independent).QueueCount == 0 && Count(independent, "token") == 0, "new sessions share no provider queue/result state");
            // Directly affected existing native inventory-provider/output-backpressure cases, not extra suite names.
            CapabilityTests.ExternalProduction(check); CapabilityTests.IncompatibleProduction(check);
            Gates(check); Checkpoints(check); Faults(check);
        }
        private static void Gates(Check check)
        {
            GameFactory factory = Factory(json => json.Get("entities")[0].Get("components").Set("pulse_transformer",
                Json.Parse(FrameworkProofContent.Read("transformer-proof.json")).Get("entities")[0].Get("components").Get("pulse_transformer")));
            SimGame game = New(factory); Entity assembler = Find(game, "assembler"); ResourceId supply = game.World.Content.ResourceId("supply");
            check.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = assembler.Id, Operation = new Code("pulse"), Enabled = false, Target = Fix.One }).Accepted,
                "engine-owned named gate is controlled through ordinary operation commands");
            check.That(Queue(game, "assembler", "build").Accepted && State(game).Blocker == new Code("pulse.utility-offline"),
                "optional gate blocks native readiness through its typed contract");
            check.That(State(game).Progress.Equals(WorkProgress.Zero) && assembler.Get<IInventory>().Amount(supply) == Fix.FromInt(20),
                "gate refusal occurs before irreversible inputs and progress");
            Twin(check, factory, game);
            TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = assembler.Id, Operation = new Code("pulse"), Enabled = true, Target = Fix.One });
            game.RunTicks(6); check.That(State(game).QueueCount == 0 && Count(game, "token") == 1, "actual gate restoration permits timed processing, not instant results");
            SimGame faulted = New(factory); Find(faulted, "assembler").Get<PulseTransformer>().FaultMode = 4;
            Queue(faulted, "assembler", "build");
            check.Equal(GameState.Faulted, faulted.State, "mutating gate faults the normal shared readiness path");
            check.Throws<InvalidOperationException>(() => faulted.Save(), "gate partial state cannot checkpoint");
        }
        private static void Checkpoints(Check check)
        {
            foreach (string corrupt in new[] { "phase", "empty-progress", "unsupported", "future-progress", "unearned-output" })
            {
                SimGame game = New(Factory()); Transformer part = Find(game, "assembler").Get<Transformer>();
                switch (corrupt)
                {
                    case "phase": part.Phase = (TransformationPhase)99; break;
                    case "empty-progress": part.Progress = WorkProgress.Zero.Advance(Fix.One, 2); break;
                    case "unsupported": part.Queue.Add(new TransformationQueueEntry(1, "pulse", false, default)); break;
                    case "future-progress":
                        part.Enqueue(game.World.Content.Get<TransformationDef>("build"), 1, false, default);
                        part.Phase = TransformationPhase.Working; part.Progress = WorkProgress.Zero.Advance(Fix.FromInt(10), 10); break;
                    case "unearned-output":
                        part.Enqueue(game.World.Content.Get<TransformationDef>("build"), 1, false, default);
                        part.Phase = TransformationPhase.Blocked; break;
                }
                check.Throws<CorruptStateException>(() => game.Save(), "actual provider/manager checkpoint validation refuses " + corrupt);
            }
        }
        private static void Faults(Check check)
        {
            foreach (int fault in new[] { 1, 2, 3 })
            {
                SimGame game = New(Factory()); Entity emitter = Find(game, "emitter");
                emitter.Get<PulseTransformer>().FaultMode = fault;
                if (fault == 2) game.Seat(0).Submit(new EnqueueTransformationCommand
                {
                    Transformer = emitter.Id,
                    Capability = Named("emitter"),
                    Transformation = "pulse"
                });
                else if (fault == 1) game.Seat(0).Submit(new CancelTransformationCommand { Transformer = emitter.Id, Capability = Named("emitter"), Index = 0 });
                game.Update();
                if (fault == 1 && game.State != GameState.Faulted)
                    check.Throws<InvalidOperationException>(() => View(game).TryGetTransformation(emitter.Id, Named("emitter"), out _), "mutating provider snapshot is detected by pure view");
                else check.Equal(GameState.Faulted, game.State, "provider check/execution errors fault normal Update: " + fault);
                check.Throws<InvalidOperationException>(() => game.Save(), "invalid provider state cannot checkpoint: " + fault);
            }
        }
    }
}
