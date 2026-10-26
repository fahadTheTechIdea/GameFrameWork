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
    public static class OperationTests
    {
        private static readonly Code ProductionId = new Code("transformation"), DialId = new Code("dial");
        public static void Register()
        {
            TestRunner.Add("Operations: transformer controls gate resources and saved progress", ProducerControls);
            TestRunner.Add("Operations: ordinary cancellation releases paused transformation", Cancel);
            TestRunner.Add("Operations: external provider uses generic commands, views and signals", External);
            TestRunner.Add("Operations: typed signals are pure, private and explicit", Signals);
            TestRunner.Add("Operations: composite entities keep one owner per named operation", Composite);
            TestRunner.Add("Operations: malformed content and facts refuse instead of defaulting", Invalid);
        }

        private static string Definitions()
            => Colony.Defs.Replace("\"worker_roles\":", @"
  ""signals"": [
    { ""id"": ""enabled"", ""kind"": ""boolean"", ""metric"": ""operation.enabled"", ""unit"": ""unit.boolean"", ""providers"": [{ ""component"": ""transformer"", ""schema"": 1 }, { ""component"": ""dial_operation"", ""schema"": 1 }] },
    { ""id"": ""running"", ""kind"": ""boolean"", ""metric"": ""operation.running"", ""unit"": ""unit.boolean"", ""providers"": [{ ""component"": ""transformer"", ""schema"": 1 }] },
    { ""id"": ""progress"", ""kind"": ""number"", ""metric"": ""transformation.progress"", ""unit"": ""unit.work-ticks"", ""providers"": [{ ""component"": ""transformer"", ""schema"": 1 }] },
    { ""id"": ""backlog"", ""kind"": ""number"", ""metric"": ""transformation.backlog"", ""unit"": ""unit.count"", ""providers"": [{ ""component"": ""transformer"", ""schema"": 1 }] },
    { ""id"": ""accumulated"", ""kind"": ""number"", ""metric"": ""dial.accumulated"", ""unit"": ""unit.units"", ""providers"": [{ ""component"": ""dial_operation"", ""schema"": 1 }] }
  ],
  ""worker_roles"":")
                .Replace("\"minimum\": 1, \"maximum\": 1", "\"minimum\": 0.5, \"maximum\": 2")
                .Replace("\"storage\": { \"capacity_policy\": \"fixture.count.100\", \"public\": false }", "\"single_resource_port\": { \"resource\": \"steel\", \"capacity_policy\": \"fixture.count.40\", \"initial\": 30 }")
                .Replace("\"duration\": { \"numerator\": 3, \"denominator\": 1, \"unit\": \"hour\" }, \"entity_output\": \"truck\"", "\"duration\": { \"numerator\": 3, \"denominator\": 1, \"unit\": \"hour\" }, \"cash\": 5, \"entity_output\": \"truck\"")
                .Replace("\"construction\":", "\"dial_operation\": { \"operation\": { \"id\": \"dial\", \"enabled\": true, \"target\": 2, \"minimum\": 0, \"maximum\": 5, \"unit\": \"unit.units-per-tick\" } }, \"construction\":");

        private static Entity Factory(SimGame game) => game.World.Entities.Get(Colony.Mine(game, 0, "factory")[0].Id);
        private static Entity Dial(SimGame game, int seat = 0)
        {
            Entity entity = ((global::Beep.SimEngine.Maps.TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>("tower"), new PlayerId(seat), new TileCoord(16 + seat * 4, 18));
            game.World.Entities.Flush();
            return entity;
        }
        private static CommandResult Configure(SimGame game, EntityId entity, bool enabled, Fix target, string operation = "transformation")
            => TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = entity, Operation = new Code(operation), Enabled = enabled, Target = target });
        private static TransformationSnapshot Production(SimGame game, EntityId entity)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(entity, new Code("transformation"), out TransformationSnapshot result)) throw new InvalidOperationException("missing transformer facts");
            return result;
        }

        private static void ProducerControls(Check c)
        {
            GameFactory factory = Colony.Factory(Definitions().Replace("\"enabled\": true, \"target\": 1", "\"enabled\": false, \"target\": 1"));
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            Entity entity = Factory(game);
            OperationsView view = game.Seat(0).View.Feature<OperationsView>();
            c.That(ReferenceEquals(entity.Get<IOperation>(ProductionId), entity.Get<Transformer>()) && ReferenceEquals(entity.Get<IControllableOperation>(ProductionId), entity.Get<Transformer>()), "existing transformer owns its operation and controls, with no proxy");
            c.That(view.TryGetControl(entity.Id, ProductionId, out OperationControlSnapshot initial) && !initial.Enabled, "enabled state is authored, not fabricated on creation");
            c.Equal(Fix.Half, initial.Minimum, "minimum comes from JSON");
            c.Equal(Fix.FromInt(2), initial.Maximum, "maximum comes from JSON");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = entity.Id, Transformation = "build_truck" }).Accepted, "disabled factory still accepts player queue intent");
            game.RunTicks(60);
            c.Equal(Fix.FromInt(30), entity.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "disabled operation consumes no inputs");
            c.Equal(Fix.FromInt(10000), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "disabled operation consumes no money");
            c.Equal(WorkProgress.Zero, Production(game, entity.Id).Progress, "disabled operation progresses no work");
            c.That(view.TryGetOperation(entity.Id, ProductionId, out OperationSnapshot disabled) && disabled.Status == OperationStatus.Disabled && disabled.Blocker == OperationCodes.Disabled, "engine reports actual disabled state and blocker");
            ulong before = game.Hash();
            c.Equal(OperationCodes.TargetBounds, view.PreviewConfigure(entity.Id, ProductionId, true, Fix.FromInt(3)), "engine preview refuses excessive target");
            c.Equal(before, game.Hash(), "control previews do not mutate saved state");
            c.Equal(OperationCodes.TargetBounds, Configure(game, entity.Id, true, Fix.FromInt(3)).Refusal, "command and preview refuse identically");
            c.That(Configure(game, entity.Id, true, Fix.FromInt(2)).Accepted, "bounded controls are changed by the generic engine command");
            game.RunTicks(10);
            WorkProgress progress = Production(game, entity.Id).Progress;
            c.That(progress.WholeTicks > 10, "authored target controls actual engine work rate");
            c.Equal(Fix.FromInt(20), entity.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "input bill is consumed once on start");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "cost is charged once on start");
            c.That(Configure(game, entity.Id, false, Fix.FromInt(2)).Accepted, "player may pause an active transformer");
            game.RunTicks(30);
            c.Equal(progress, Production(game, entity.Id).Progress, "pause preserves progress without restarting");
            c.That(game.Observer.TryGetEntity(entity.Id, out EntityView snapshot) && snapshot.State == game.World.Content.Get<IEntityStateDefinition>("stopped").State, "entity Update owns the resulting stopped transition");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "controls and paused progress restore through existing codecs");
            c.Equal(EngineInfo.SaveFormat, SaveGame.Describe(game.Save()).Format, "active control state uses the current breaking save format");
            c.That(Configure(game, entity.Id, true, Fix.FromInt(2)).Accepted && Configure(restored, entity.Id, true, Fix.FromInt(2)).Accepted, "original and restored operations resume normally");
            int elapsed = TestKit.RunUntil(game, () => Production(game, entity.Id).QueueCount == 0, checked((int)TestGame.Time.Hours(4)), 1);
            restored.RunTicks(elapsed);
            c.Equal(0, Production(game, entity.Id).QueueCount, "same queue completes after real work time");
            c.Equal(game.Hash(), restored.Hash(), "saved continuation is deterministic");
            c.Equal(Fix.FromInt(20), entity.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "resume does not consume inputs twice");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "resume does not charge twice");
            c.That(view.TryGetOperation(entity.Id, ProductionId, out OperationSnapshot idle) && idle.Status == OperationStatus.Idle, "empty queue is idle, not inferred running");
        }

        private static void Cancel(Check c)
        {
            SimGame game = Colony.Factory(Definitions()).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity entity = Factory(game);
            TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = entity.Id, Transformation = "build_truck" });
            c.That(Configure(game, entity.Id, false, Fix.One).Accepted, "working operation is disabled through its public command");
            c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Capability = new Code("transformation"), Transformer = entity.Id, Index = 0 }).Accepted, "existing cancellation accepts a paused operation");
            game.RunTicks(200);
            c.Equal(0, Production(game, entity.Id).QueueCount, "cancelled queue is not resurrected");
            c.Equal(Fix.FromInt(20), entity.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "cancel does not recreate spent inputs");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "cancel does not recreate spent cash");
            c.Equal(2, Colony.Mine(game, 0, "truck").Count, "cancelled operation creates no completed vehicle");
        }

        private static void External(Check c)
        {
            GameFactory factory = Colony.Factory(Definitions());
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            Entity entity = Dial(game), foreign = Dial(game, 1);
            OperationsView view = game.Seat(0).View.Feature<OperationsView>();
            c.That(entity.Get<Transformer>() == null && entity.Get<IControllableOperation>(DialId) is DialOperation, "external operation requires no transformer or coordinator edits");
            c.That(view.TryGetControl(entity.Id, DialId, out OperationControlSnapshot initial) && initial.Target == Fix.FromInt(2), "external authored controls use the same immutable facts");
            ulong hash = game.Hash();
            c.Equal(Code.None, view.PreviewConfigure(entity.Id, DialId, false, Fix.FromInt(4)), "same generic preview accepts external provider");
            c.Equal(hash, game.Hash(), "external control preview is nonmutating");
            c.Equal(Codes.NotFound, view.PreviewConfigure(foreign.Id, DialId, false, Fix.One), "foreign control preview is private");
            c.Equal(Codes.NotFound, Configure(game, foreign.Id, false, Fix.One, "dial").Refusal, "foreign command has identical private refusal semantics");
            c.That(!view.TryGetControl(foreign.Id, DialId, out _) && !view.TryGetOperation(foreign.Id, DialId, out _), "foreign operation settings and facts stay private");
            c.That(Configure(game, entity.Id, false, Fix.FromInt(4), "dial").Accepted, "generic command controls independent provider");
            c.Equal(Fix.FromInt(2), initial.Target, "captured controls do not change with provider state");
            view.ReadSignal(entity.Id, DialId, "accumulated", out AutomationSignalSnapshot stopped);
            game.RunTicks(30);
            view.ReadSignal(entity.Id, DialId, "accumulated", out AutomationSignalSnapshot still);
            c.Equal(stopped.Value, still.Value, "external engine-side work stops while disabled");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "external operation saves through its own sole component codec");
            c.That(Configure(game, entity.Id, true, Fix.FromInt(4), "dial").Accepted && Configure(restored, entity.Id, true, Fix.FromInt(4), "dial").Accepted, "external provider resumes on both runs");
            game.RunTicks(25); restored.RunTicks(25);
            view.ReadSignal(entity.Id, DialId, "accumulated", out AutomationSignalSnapshot resumed);
            c.That(resumed.Value > stopped.Value, "module-defined signal reports actual progressed work");
            c.Equal(game.Hash(), restored.Hash(), "interleaved saved external operation continues identically");
            c.Equal(OperationCodes.TargetBounds, view.PreviewConfigure(entity.Id, DialId, true, Fix.FromInt(6)), "external authored bounds are enforced without a built-in table");
            c.That(Configure(game, entity.Id, true, Fix.Zero, "dial").Accepted, "external zero target is valid within its authored bounds");
            game.RunTicks(10);
            c.That(view.TryGetOperation(entity.Id, DialId, out OperationSnapshot idle) && idle.Status == OperationStatus.Idle, "enabled zero-target operation reports idle");
            c.Equal(entity.Def.InitialState.State, entity.State, "entity Update settles zero-target operation to its authored idle state");
            view.ReadSignal(entity.Id, DialId, "accumulated", out AutomationSignalSnapshot zeroTarget);
            c.Equal(resumed.Value, zeroTarget.Value, "idle zero-target operation performs no work");
        }

        private static void Signals(Check c)
        {
            SimGame game = Colony.Factory(Definitions()).NewGame(new NewGameRequest { Scenario = "colony" });
            Entity entity = Factory(game);
            OperationsView view = game.Seat(0).View.Feature<OperationsView>();
            ulong hash = game.Hash();
            c.Equal(Code.None, view.ReadSignal(entity.Id, ProductionId, "enabled", out AutomationSignalSnapshot enabled), "engine reads declared boolean signal");
            c.That(enabled.Available && enabled.Kind == AutomationSignalKind.Boolean && enabled.Value == Fix.One, "signal is typed and reports actual enabled state");
            c.Equal(Code.None, view.ReadSignal(entity.Id, ProductionId, "progress", out AutomationSignalSnapshot progress), "engine reads declared numeric signal");
            c.That(progress.Available && progress.Value == Fix.Zero && progress.Unit == new Code("unit.work-ticks"), "measured zero has explicit authored units");
            c.Equal(Code.None, view.ReadSignal(entity.Id, ProductionId, "accumulated", out AutomationSignalSnapshot unsupported), "valid signal request can report unsupported binding");
            c.That(!unsupported.Available && unsupported.Value == null && unsupported.Blocker == OperationCodes.SignalUnavailable, "unsupported binding is unavailable, not fabricated zero");
            c.Equal(Codes.UnknownDef, view.ReadSignal(entity.Id, ProductionId, "missing", out AutomationSignalSnapshot missing), "unknown signal identity refuses explicitly");
            c.That(!missing.Available, "refused request exposes no stale reading");
            EntityId foreign = Colony.Mine(game, 1, "factory")[0].Id;
            c.Equal(Codes.NotOwned, view.ReadSignal(foreign, ProductionId, "enabled", out _), "foreign signal readings remain private");
            c.That(game.Observer.Feature<OperationsView>().ReadSignal(foreign, ProductionId, "enabled", out _) == Code.None, "authorized observer retains access");
            c.Equal(hash, game.Hash(), "signal reads neither mutate, reserve nor consume");
            c.That(Configure(game, entity.Id, false, Fix.One).Accepted, "operation may change after a snapshot is captured");
            c.Equal(Fix.One, enabled.Value.Value, "captured signal remains immutable after control change");
        }

        private static void Composite(Check c)
        {
            string definitions = Definitions().Replace("\"transformer\":", "\"dial_operation\": { \"operation\": { \"id\": \"dial\", \"enabled\": true, \"target\": 2, \"minimum\": 0, \"maximum\": 5, \"unit\": \"unit.units-per-tick\" } }, \"transformer\":");
            GameFactory factory = Colony.Factory(definitions);
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            Entity entity = Factory(game);
            OperationsView view = game.Seat(0).View.Feature<OperationsView>();
            var ids = new List<Code>();
            c.That(view.CopyOperationIds(entity.Id, ids) && ids.Count == 2 && ids.Contains(ProductionId) && ids.Contains(DialId), "engine exposes both authored operation identities on one entity");
            c.That(entity.Def.HasCapability<IControllableOperation>(ProductionId) && entity.Def.HasCapability<IControllableOperation>(DialId), "prototype queries address the same named capability declarations");
            c.That(entity.Get<IOperation>(ProductionId) is Transformer && entity.Get<IOperation>(DialId) is DialOperation, "named queries return the actual distinct state owners");
            c.Throws<ArgumentException>(() => entity.Get<IOperation>(), "unqualified lookup cannot silently select the first operation");
            c.Throws<ArgumentException>(() => entity.Def.HasCapability<IOperation>(), "unqualified prototype lookup cannot silently miss named capabilities");
            c.Equal(Codes.InvalidArgument, view.PreviewConfigure(entity.Id, Code.None, false, Fix.One), "commands require explicit operation identity");
            c.That(Configure(game, entity.Id, false, Fix.One).Accepted, "player independently disables transformation");
            c.That(view.TryGetOperation(entity.Id, ProductionId, out OperationSnapshot transformation) && transformation.Status == OperationStatus.Disabled, "transformation reports its own disabled state");
            c.That(view.TryGetOperation(entity.Id, DialId, out OperationSnapshot dial) && dial.Status == OperationStatus.Running, "another operation on the same entity remains running");
            view.ReadSignal(entity.Id, DialId, "accumulated", out AutomationSignalSnapshot before);
            game.RunTicks(10);
            view.ReadSignal(entity.Id, DialId, "accumulated", out AutomationSignalSnapshot after);
            c.That(after.Value > before.Value, "other operation continues real engine progress independently");
            c.That(view.CopyOperationIds(Colony.Mine(game, 1, "factory")[0].Id, ids) == false && ids.Count == 0, "enumerated identities respect privacy and clear stale results");
            c.That(entity.Get<IInventory>() is SingleResourcePort, "named operations do not weaken single inventory ownership");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "all named operation state restores through existing component slots/codecs");
            game.RunTicks(25); restored.RunTicks(25);
            c.Equal(game.Hash(), restored.Hash(), "composite entity continues deterministically after save");
        }

        private static void Invalid(Check c)
        {
            void Refuse(string json, string message) => c.Throws<ContentException>(() => Colony.Factory(json).NewGame(new NewGameRequest { Scenario = "colony" }), message);
            Refuse(Colony.Defs.Replace(", \"operation\": { \"id\": \"transformation\", \"enabled\": true, \"target\": 1, \"minimum\": 1, \"maximum\": 1, \"unit\": \"unit.work-rate\" }", ""), "transformer cannot fabricate absent operation settings");
            Refuse(Definitions().Replace("\"enabled\": true, \"target\": 1", "\"target\": 1"), "initial enable state must be authored");
            Refuse(Definitions().Replace("\"id\": \"transformation\", ", ""), "operation identity cannot be implicit");
            Refuse(Definitions().Replace("\"minimum\": 0.5", "\"minimum\": 3"), "inverted bounds refuse content");
            Refuse(Definitions().Replace("\"maximum\": 2", "\"maximum\": 0.75"), "out-of-range initial targets refuse content");
            Refuse(Definitions().Replace("\"minimum\": 0.5", "\"minimum\": 0"), "transformer rate cannot create nonfinishing zero work");
            Refuse(Definitions().Replace("\"unit\": \"unit.work-rate\"", "\"unit\": \"unit.cash\""), "transformer target cannot silently reinterpret another unit");
            Refuse(Definitions().Replace("\"kind\": \"number\"", "\"kind\": \"anything\""), "unknown signal kind refuses content");
            Refuse(Definitions().Replace("\"id\": \"backlog\"", "\"id\": \"progress\""), "signal identities have one owner");
            Refuse(Definitions().Replace("\"transformer\":", "\"dial_operation\": { \"operation\": { \"id\": \"transformation\", \"enabled\": true, \"target\": 1, \"minimum\": 0, \"maximum\": 2, \"unit\": \"unit.units-per-tick\" } }, \"transformer\":"), "ambiguous operation owners refuse prototypes");
            c.Throws<ArgumentException>(() => new AutomationSignalSnapshot("bad", AutomationSignalKind.Boolean, new Code("unit.boolean"), Fix.FromInt(2), Code.None), "invalid boolean values cannot masquerade as typed signals");
            c.Throws<ArgumentException>(() => new OperationSnapshot(OperationStatus.Running, OperationCodes.Disabled), "running facts cannot carry a contradictory blocker");
            Refuse(Definitions().Replace("\"metric\": \"transformation.progress\", \"unit\": \"unit.work-ticks\"", "\"metric\": \"transformation.progress\", \"unit\": \"unit.wrong\""), "provider unit mismatch refuses content before Initialize");
        }
    }
}
