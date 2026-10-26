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
    internal static class ReplayFailureBoundaryTests
    {
        internal const string Case = "Replay failure boundary: extension callbacks and active process control recovery";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        private static readonly IReplayVerifier Verifier = new ReplayVerifier();
        internal static void Register() => TestRunner.Add(Case, Run);
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static void Step(SimGame game)
        { if (game.Update() != 1 || game.Fault != null) throw new InvalidOperationException("Update stopped at " + game.Tick + ": " + game.Fault); }
        private static EntityView Entity(SimGame game, string prototype)
        {
            var facts = new List<EntityView>(); game.Seat(0).View.CopyEntities(new PlayerId(0), facts);
            foreach (var fact in facts) if (fact.Def == prototype) return fact;
            throw new InvalidOperationException("missing player-visible " + prototype);
        }
        private static int Count(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Count(e => e.Def == prototype);
        private static Fix Stock(SimGame game, EntityId entity, string resource)
        {
            var facets = new List<Facet>(); game.Seat(0).View.Inspect(entity, facets);
            foreach (var facet in facets) if (facet.Key == Storage.FacetStock && facet.Subject == resource) return facet.Value;
            throw new InvalidOperationException("missing actual stock " + resource);
        }
        private static SimGame Verify(Check c, GameFactory factory, SimGame source, byte[] artifact, string label)
        {
            ulong before = source.Hash(); var result = Verifier.Verify(factory, artifact, Limits);
            c.That(result.Success && result.Game != null, label + ": " + result.Failure + "/" + result.Detail);
            if (!result.Success) throw new InvalidOperationException(label + ": " + result.Detail);
            c.Equal(before, result.ActualHash, label + " authoritative checkpoint"); c.Equal(before, result.Game.Hash(), label + " independent verified session");
            c.Equal(before, source.Hash(), label + " source unchanged"); c.Equal(source.Tick, result.Step, label + " sole clock");
            return result.Game;
        }
        private static GameFactory FaultFactory(ReplayProbeFailure mode = ReplayProbeFailure.None)
            => TransformerTests.Factory(json => json.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.replay-failure")),
                new ReplayFailureProofModule(mode), FrameworkProofContent.Read("replay-failure-module.json"),
                FrameworkProofContent.Read("replay-failure-probe.json"));
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "strict current build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "original saved layouts");
            Faults(c); Manufacturing(c); Automation(c); Incident(c);
        }
        private static void Faults(Check c)
        {
            GameFactory factory = FaultFactory(); SimGame source = TransformerTests.New(factory), isolated = TransformerTests.New(factory);
            byte[] artifact;
            using (var recording = source.RecordReplay(Limits))
            { source.Seat(0).Submit(new ReplayProbeCommand()); Step(source); Step(source); artifact = recording.Finish(); }
            ulong sourceHash = source.Hash(), isolatedHash = isolated.Hash();
            Verify(c, factory, source, artifact, "healthy public extension");
            c.Throws<ArgumentNullException>(() => Verifier.Verify(null, artifact, Limits), "null factory remains caller error");
            c.Throws<ArgumentNullException>(() => Verifier.Verify(factory, null, Limits), "null artifact remains caller error");
            c.Throws<ArgumentNullException>(() => Verifier.Verify(factory, artifact, null), "null limits remain caller error");
            foreach (var mode in new[] { ReplayProbeFailure.Compose, ReplayProbeFailure.Restore, ReplayProbeFailure.Loaded,
                ReplayProbeFailure.InitialHash, ReplayProbeFailure.CheckpointHash, ReplayProbeFailure.FinalHash,
                ReplayProbeFailure.Constructor, ReplayProbeFailure.Decode, ReplayProbeFailure.CopyWrite, ReplayProbeFailure.CopyRead,
                ReplayProbeFailure.Update, ReplayProbeFailure.CorruptPayload, ReplayProbeFailure.InvalidPayload })
            {
                var failed = Verifier.Verify(FaultFactory(mode), artifact, Limits);
                string code = mode == ReplayProbeFailure.Update ? "replay.fault" :
                    mode == ReplayProbeFailure.CorruptPayload || mode == ReplayProbeFailure.InvalidPayload ? "replay.invalid" : "replay.provider";
                c.That(!failed.Success && failed.Game == null && failed.Failure == new Code(code), mode + " typed failure, no unverified live game: " + failed.Detail);
                string callback = mode == ReplayProbeFailure.Compose || mode == ReplayProbeFailure.Restore || mode == ReplayProbeFailure.Loaded ? "session.restore" :
                    mode == ReplayProbeFailure.InitialHash ? "initial.hash" : mode == ReplayProbeFailure.CheckpointHash ? "checkpoint.hash" :
                    mode == ReplayProbeFailure.FinalHash ? "final.hash" :
                    mode == ReplayProbeFailure.CopyWrite || mode == ReplayProbeFailure.CopyRead ? "input.admit:proof.replay-input" : "input.decode:proof.replay-input";
                if (code == "replay.provider") c.That(failed.Detail.StartsWith(callback + ":") && failed.Detail.Contains("probe"),
                    mode + " callback and original exception provenance retained");
                long step = mode == ReplayProbeFailure.CheckpointHash ? 1 : mode == ReplayProbeFailure.FinalHash ? 2 : 0;
                int index = mode == ReplayProbeFailure.Compose || mode == ReplayProbeFailure.Restore || mode == ReplayProbeFailure.Loaded || mode == ReplayProbeFailure.InitialHash ? -1 :
                    mode == ReplayProbeFailure.FinalHash ? 3 : mode == ReplayProbeFailure.CheckpointHash || mode == ReplayProbeFailure.Update ? 1 : 0;
                c.That(failed.Step == step && failed.Record == index, mode + " exact failure coordinate: " + failed.Step + "/" + failed.Record);
                c.Equal(sourceHash, source.Hash(), mode + " cannot mutate recording source"); c.Equal(isolatedHash, isolated.Hash(), mode + " cannot poison another session");
            }
            Verify(c, factory, source, artifact, "healthy factory remains usable after hostile verifications");
        }
        private static TransformationSnapshot Process(SimGame game, string prototype, Code capability)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Entity(game, prototype).Id, capability, out var value))
                throw new InvalidOperationException("missing actual transformation facts");
            return value;
        }
        private static void Manufacturing(Check c)
        {
            GameFactory factory = TransformerTests.Factory(); SimGame game = TransformerTests.New(factory);
            Code native = new Code("transformation"), external = new Code("pulse"); EntityId assembler = Entity(game, "assembler").Id, emitter = Entity(game, "emitter").Id;
            byte[] prefix;
            c.That(game.Seat(0).View.TryFindResource("supply", out var supply), "authored public resource identity");
            using (var recording = game.RecordReplay(Limits))
            {
                c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = assembler, Capability = native, Transformation = "build", Count = 3 }).Accepted, "ordinary native batch");
                c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = emitter, Capability = external, Transformation = "pulse", Count = 3 }).Accepted, "ordinary independent batch");
                c.That(Process(game, "assembler", native).Progress.WholeTicks > 0 && Process(game, "assembler", native).Wip.Stock.AmountOf(supply) == Fix.FromInt(3),
                    "real native queued work retains consumed WIP");
                c.That(Process(game, "emitter", external).Progress.WholeTicks > 0 && Count(game, "token") == 0, "independent timed work creates no instant entity");
                prefix = recording.Finish();
            }
            Verify(c, factory, game, prefix, "replay to active native and external work");
            SimGame active = factory.Load(game.Save()); c.Equal(game.Hash(), active.Hash(), "partial WIP/queue save");
            byte[] suffix;
            using (var recording = active.RecordReplay(Limits))
            {
                c.That(TestKit.Order(active, 0, new ConfigureOperationCommand { Entity = assembler, Operation = native, Enabled = false, Target = Fix.One }).Accepted, "player disables active native processor");
                WorkProgress held = Process(active, "assembler", native).Progress; Fix quantity = Stock(active, assembler, "supply");
                Step(active); Step(active); c.Equal(held, Process(active, "assembler", native).Progress, "disabled work retains real progress");
                c.Equal(quantity, Stock(active, assembler, "supply"), "disabled work cannot repeat input spending");
                active.Clock.Pause(); long time = active.Tick; ulong hash = active.Hash(); active.Update();
                c.That(active.Tick == time && active.Hash() == hash, "pause uses sole clock, no replay step invented"); active.Clock.Resume();
                c.That(TestKit.Order(active, 0, new ConfigureOperationCommand { Entity = assembler, Operation = native, Enabled = true, Target = Fix.One }).Accepted, "ordinary resume");
                int updates = 0;
                while ((Process(active, "assembler", native).QueueCount > 0 || Process(active, "emitter", external).QueueCount > 0) && updates++ < 80) Step(active);
                c.That(Process(active, "assembler", native).QueueCount == 0 && Process(active, "emitter", external).QueueCount == 0, "bounded native and external batches finish");
                c.Equal(6, Count(active, "token"), "exact real engine outputs"); c.Equal(Fix.FromInt(14), Stock(active, assembler, "supply"), "20 minus six net consumed supplies");
                suffix = recording.Finish();
            }
            Verify(c, factory, active, suffix, "active saved manufacturing control continuation");
            c.That(!active.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(assembler, native, out _), "replay does not replace company-private views");
        }
        private static AutomationRuleSnapshot Rule(SimGame game)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetAutomationRule(new PlayerId(0), "replay.control", out var rule)) throw new InvalidOperationException("missing actual rule");
            return rule;
        }
        private static void Automation(Check c)
        {
            GameFactory factory = AutomationTests.Factory(); SimGame game = AutomationTests.New(factory);
            var view = game.Seat(0).View.Feature<OperationsView>(); c.That(view.TryGetAutomationProfile("high-level", out var profile), "authored rule supplied through immutable engine view");
            var thresholds = new Dictionary<string, ControlThresholdBand>(); foreach (var row in profile.Thresholds) thresholds.Add(row.Key, row.Value.Initial);
            EntityId target = Entity(game, "actuator").Id, sensor = Entity(game, "sensor").Id;
            var settings = new AutomationSettings(profile.Id, true, new ControlBinding(target, new Code("probe")),
                new Dictionary<string, ControlBinding> { ["level"] = new ControlBinding(sensor, new Code("probe")) }, thresholds, profile.WhenTrue, profile.WhenFalse);
            byte[] before;
            using (var recording = game.RecordReplay(Limits))
            {
                c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "replay.control", Settings = settings }).Accepted, "ordinary authored control configuration");
                int updates = 0; while (!Rule(game).PendingDecision.HasValue && updates++ < 20) Step(game);
                c.That(Rule(game).PendingDecision == true && Rule(game).PendingAt == game.Tick, "decision waits for next step");
                c.That(view.TryGetControl(target, new Code("probe"), out var control) && control.Enabled, "pending decision did not shut down same-step input");
                before = recording.Finish();
            }
            Verify(c, factory, game, before, "replay to deferred control decision");
            SimGame twin = factory.Load(game.Save()); byte[] after; var retained = Rule(game);
            using (var recording = game.RecordReplay(Limits))
            {
                game.Clock.Pause(); game.Update(); c.Equal(retained.PendingAt, Rule(game).PendingAt, "pending game-time action freezes while paused");
                game.Clock.Resume(); Step(game); Step(twin);
                c.That(view.TryGetControl(target, new Code("probe"), out var control) && !control.Enabled && Rule(game).Latch && !Rule(game).PendingDecision.HasValue,
                    "original engine applies due action once");
                c.Equal(game.Hash(), twin.Hash(), "saved pending action continues identically");
                after = recording.Finish();
            }
            Verify(c, factory, game, after, "active next-step control save/replay continuation");
            c.That(retained.PendingDecision == true && !game.Seat(1).View.Feature<OperationsView>().TryGetAutomationRule(new PlayerId(0), "replay.control", out _),
                "retained pending facts immutable, foreign rule private");
        }
        private static IncidentPhaseView Phase(SimGame game, EntityId target, string phase)
        {
            var phases = new List<IncidentPhaseView>(); game.Seat(0).View.Feature<IncidentsView>().CopyPhases(target, new Code("leak"), phases);
            foreach (var value in phases) if (value.Id == phase) return value;
            throw new InvalidOperationException("missing response phase " + phase);
        }
        private static void Incident(Check c)
        {
            JsonValue content = Json.Parse(FrameworkProofContent.Read("incident-phases.json"));
            // Explicit resource-only scenario authoring, not an implicit monetary fallback.
            content.Remove("work_cost_requirements"); JsonValue group = content.Get("work_requirement_groups")[0].Get("entries");
            var requirements = JsonValue.NewArray(); requirements.Add(group[0]); requirements.Add(group[1]);
            content.Get("work_requirement_groups")[0].Set("entries", requirements);
            JsonValue basis = Json.Parse(Colony.Defs); basis.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("replay.incident.start"));
            GameFactory factory = Colony.Factory(definitions: Text(basis), additionalDocuments: new Dictionary<string, string> {
                ["incident-phases.json"] = Text(content), ["replay-incident-startup.json"] = FrameworkProofContent.Read("replay-incident-startup.json") });
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 953 }); EntityId target = Entity(game, "leaking-machine").Id;
            Code source = new Code("leak"); var view = game.Seat(0).View.Feature<IncidentsView>(); byte[] before;
            using (var recording = game.RecordReplay(Limits))
            {
                c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target, Source = source, Kind = new Code("machine.leak") }).Accepted, "player raises authored incident");
                int updates = 0; while ((Phase(game, target, "repair").Nodes.Count == 0 || Phase(game, target, "repair").Nodes[0].Progress.WholeTicks == 0) && updates++ < 600) Step(game);
                var repair = Phase(game, target, "repair");
                c.That(repair.Nodes.Count > 0 && repair.Nodes[0].Progress.WholeTicks > 0 && repair.Nodes[0].Worker.IsValid,
                    "bounded physical repair starts with real machine; blocker=" + (repair.Nodes.Count > 0 ? repair.Nodes[0].Blocker.ToString() : "no node"));
                c.That(Phase(game, target, "isolation").Complete && !Phase(game, target, "cleanup").Complete, "dependency progress and blocked parallel branch remain real");
                c.Equal(Fix.FromInt(26), Stock(game, target, "steel"), "response physically consumed one isolation and three repair materials");
                before = recording.Finish();
            }
            Verify(c, factory, game, before, "replay to active incident machine/tool/material work");
            SimGame restored = factory.Load(game.Save()); byte[] after;
            using (var recording = game.RecordReplay(Limits))
            {
                c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target, Enabled = true }).Accepted, "ordinary player enables cleanup");
                c.That(TestKit.Order(restored, 0, new SetWorkPermitCommand { Target = target, Enabled = true }).Accepted, "same ordinary intent on saved twin");
                int updates = 0; bool parallel = false;
                while (view.TryGetLifecycle(target, source, out var live) && live.Snapshot.Active && updates++ < 600)
                {
                    Step(game); Step(restored);
                    parallel |= Phase(game, target, "repair").Nodes[0].State == NodeState.Working && Phase(game, target, "cleanup").Nodes[0].State == NodeState.Working;
                }
                c.That(view.TryGetLifecycle(target, source, out var done) && !done.Snapshot.Active && done.OrderState == OrderState.Done, "bounded physical response completes");
                c.That(parallel, "real machines work parallel repair/cleanup branches"); c.Equal(game.Hash(), restored.Hash(), "saved active response completes identically");
                c.Equal(Fix.FromInt(26), Stock(game, target, "steel"), "completion repeats no consumption");
                c.That(Phase(game, target, "restart").Complete, "restart follows both real branches"); after = recording.Finish();
            }
            Verify(c, factory, game, after, "active saved incident response replay");
        }
    }
}
