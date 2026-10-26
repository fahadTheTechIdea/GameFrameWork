using System.Linq;
using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.AutomationTests;

namespace Beep.SimEngine.Tests
{
    // Direct recovery cases own execution; fault hooks instrument actual engine-side providers.
    internal static class InputRecoveryTests
    {
        private static GameFactory WaitingFactory(bool independent = false, bool immediate = false)
            => Factory(root =>
            {
                for (int i = 0; i < root.Get("control_rules").Count; i++)
                    root.Get("control_rules")[i].Set("input_recovery", JsonValue.Of(independent ? "proof.input.conditional" : "proof.input.offline"));
                if (immediate) root.Get("control_rules")[0].Get("dwell").Set("numerator", JsonValue.Number("0"));
            });
        private static ConditionalInputRecoveryPolicyDef Probe(SimGame game)
            => game.World.Content.Get<ConditionalInputRecoveryPolicyDef>("proof.input.conditional");
        private static void Update(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("input recovery update " + game.Tick + ": " + game.Fault); }
        private static void Advance(SimGame game, int steps, SimGame twin = null)
        { for (int i = 0; i < steps; i++) { Update(game); if (twin != null) Update(twin); } }
        internal static void Contracts(Check c)
        {
            GameFactory factory = WaitingFactory(); SimGame game = New(factory);
            var definition = game.World.Content.Get<IControlRule>("high-level");
            c.That(ReferenceEquals(definition.InputRecovery, game.World.Content.Get<IControlInputRecoveryPolicy>("proof.input.offline")),
                "rule uses actual master-listed catalog policy, not a fallback");
            c.Equal(StateCodecTests.ExpectedAutomationSchema, ((GameSystem)game.World.Services.Get<IAutomationRules>()).StateSchemaVersion,
                "sole rule codec owns wait state and actual applied action history");
            var policy = (AuthoredControlInputRecoveryPolicyDef)definition.InputRecovery;
            c.Throws<NotSupportedException>(() => ((IList<Code>)policy.RetryBlockers).Clear(), "authored retry classifications are immutable");
            c.Throws<ArgumentException>(() => new ControlInputFailure("", new Code("probe.level"), new Code("probe.offline")), "failure context requires explicit stable input identity");
            c.Throws<ArgumentException>(() => new ControlInputFailure("level", Code.None, new Code("probe.offline")), "failure context never invents a signal");
            c.Equal(ControlInputRecoveryAction.Wait, policy.Decide(new ControlInputFailure("level", new Code("probe.level"), new Code("probe.sensor-offline"))),
                "exact authored blocker permits waiting");
            c.Equal(ControlInputRecoveryAction.Suspend, policy.Decide(new ControlInputFailure("level", new Code("probe.level"), new Code("unknown.failure"))),
                "unlisted failures never silently retry");
            foreach (string invalid in new[] { "missing", "unknown", "wrong", "array", "null", "duplicate", "empty", "extra" })
                c.Throws<ContentException>(() => New(Factory(root =>
                {
                    var rule = root.Get("control_rules")[0]; var authored = root.Get("control_input_recovery_policies")[0];
                    switch (invalid)
                    {
                        case "missing": rule.Remove("input_recovery"); break;
                        case "unknown": rule.Set("input_recovery", JsonValue.Of("absent")); break;
                        case "wrong": rule.Set("input_recovery", JsonValue.Of("probe.level")); break;
                        case "array": authored.Remove("retry_blockers"); break;
                        case "null": authored.Set("retry_blockers", JsonValue.Null()); break;
                        case "duplicate": authored.Set("retry_blockers", Json.Parse("[\"probe.offline\",\"probe.offline\"]")); break;
                        case "empty": authored.Set("retry_blockers", Json.Parse("[\"\"]")); break;
                        case "extra": authored.Set("retry_every", JsonValue.Number("1")); break;
                    }
                })), "strict recovery content refuses " + invalid + " before updates");

            c.That(Configure(game, Settings(game)).Accepted, "player configures native waiting policy through original command");
            Level(c, game, 6, false); var waiting = Rule(game);
            c.That(waiting.WaitingForInputs && waiting.WaitingInput == "level" && !waiting.Suspended && !waiting.EffectiveEnabled,
                "immutable snapshot distinguishes a waiting input from permanent suspension");
            c.That(waiting.Blocker == new Code("probe.sensor-offline") && !waiting.Candidate.HasValue && !waiting.PendingDecision.HasValue,
                "waiting discards candidate and stale pending control without fabricating a signal");
            c.Equal(AutomationCodes.Conflict, View(game).PreviewAutomation("second", Settings(game)), "waiting rule retains exclusive target admission");
            c.Equal(AutomationCodes.Conflict, Configure(game, Settings(game), "second").Refusal, "waiting conflict preview and command agree");
            var privateRules = new List<AutomationRuleSnapshot>(); View(game, 1).CopyAutomationRules(privateRules);
            c.Equal(0, privateRules.Count, "private waiting input facts do not leak to another company");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "waiting input/blocker use existing sole codec");
            c.That(Rule(twin).WaitingInput == "level" && Rule(twin).Blocker == waiting.Blocker, "saved waiting facts retain exact identity and classification");
            Level(c, game, 6); c.That(waiting.WaitingForInputs && waiting.WaitingInput == "level", "retained snapshot never follows live recovery");
            c.That(!Rule(game).WaitingForInputs && Rule(game).Candidate.HasValue, "normal Update resumes valid authored waiting source without host reconfiguration");
            c.That(Rule(twin).WaitingForInputs, "other session's source recovery cannot change waiting state");

            factory = WaitingFactory(independent: true); game = New(factory); Configure(game, Settings(game)); Level(c, game, 6, false);
            c.That(game.World.Content.Get<IControlRule>("high-level").InputRecovery is ConditionalInputRecoveryPolicyDef,
                "public-only authored policy selects recovery without modifying scheduler");
            Probe(game).Calls = 0; Update(game); c.Equal(1, Probe(game).Calls, "waiting input evaluates recovery provider once per actual Update");
            Entity source = Find(game, "sensor"); Probe(game).Calls = 0; game.World.Despawn(source); game.World.Settle(); Update(game);
            c.That(Rule(game).Suspended && !Rule(game).WaitingForInputs && Rule(game).Blocker == Codes.NotFound,
                "removed bound source always suspends, never rebinds another provider");
            c.Equal(0, Probe(game).Calls, "reference failure bypasses even an external recovery policy");
            game = New(WaitingFactory(independent: true)); Configure(game, Settings(game)); Level(c, game, 6, false);
            Probe(game).Calls = 0; game.World.Despawn(Find(game, "actuator")); game.World.Settle(); Update(game);
            c.That(Rule(game).Suspended && !Rule(game).WaitingForInputs && Rule(game).Blocker == Codes.NotFound,
                "removed control target suspends waiting rule without granting an alternative target");
            c.Equal(0, Probe(game).Calls, "invalid target bypasses recovery provider");

            factory = WaitingFactory(); game = New(factory); Configure(game, Settings(game)); Level(c, game, 6, false);
            c.That(Configure(game, Settings(game, enabled: false)).Accepted, "player disable cancels existing input wait");
            c.That(!Rule(game).WaitingForInputs && !Rule(game).PendingDecision.HasValue, "disabled rule owns no recovery action");
            c.Equal(Code.None, View(game).PreviewAutomation("replacement", Settings(game)), "disabled waiting rule releases target claim");
            c.That(Configure(game, Settings(game), "replacement").Accepted, "new target owner admits normally after explicit disable");
            c.That(TestKit.Order(game, 0, new RemoveAutomationCommand { Rule = "replacement" }).Accepted, "normal cancellation removes waiting rule");
            c.That(!View(game).TryGetAutomationRule(new PlayerId(0), "replacement", out _), "cancelled wait has no orphan retry record");

            foreach (int mode in new[] { 1, 2, 3 })
            {
                game = New(WaitingFactory(independent: true)); Configure(game, Settings(game));
                var actuator = Find(game, "actuator").Get<IControllableOperation>(new Code("probe"));
                var priorControl = actuator.ReadControl(); // Explicit white-box fault instrumentation, not a healthy post-fault view.
                if (mode == 1) Probe(game).FaultMode = 1;
                else Probe(game).ReadProbe = () => Find(game, mode == 2 ? "sensor" : "actuator").Touch();
                game.Seat(0).Submit(new SetAutomationProbeCommand { Entity = Find(game, "sensor").Id, Level = Fix.FromInt(6), Available = false });
                game.Update();
                c.That(game.Fault != null, "invalid/mutating recovery callback faults engine execution " + mode);
                c.That(actuator.ReadControl().Enabled == priorControl.Enabled && actuator.ReadControl().Target == priorControl.Target,
                    "faulting recovery never authorizes a normal operation change " + mode);
                c.Throws<InvalidOperationException>(() => Enabled(game), "normal views reject after recovery fault " + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "no healthy save follows recovery fault " + mode);
                c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy hash follows recovery fault " + mode);
            }
        }

        internal static void ProviderObservation(Check c)
        {
            foreach (bool loaded in new[] { false, true })
            foreach (string participant in new[] { "sensor", "actuator" })
            foreach (string failure in new[] { "silent", "payload-throw", "clock-throw" })
            {
                string context = participant + "/" + failure + "/loaded=" + loaded;
                var factory = WaitingFactory(independent: true, immediate: true); var game = New(factory);
                c.That(Configure(game, Settings(game)).Accepted && Rule(game).PendingDecision == true,
                    "actual immediate rule owns a saved next-step intent before input loss: " + context);
                if (loaded) game = factory.Load(game.Save());
                var world = game.World; var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                var policy = Probe(game); policy.Calls = 0;
                var target = Find(game, "actuator").Get<AutomationProbe>(); var control = target.ReadControl();
                var victim = Find(game, participant).Get<AutomationProbe>();
                var priorRules = new StateHasher(); world.System<AutomationSystem>().Sync(priorRules);
                var published = game.Seat(0).Snapshot; long time = game.Tick;
                var primary = new InvalidOperationException("proof.original-recovery-callback");
                int calls = 0; bool silentVersion = false, actualPayloadChange = false;
                policy.ReadProbe = () => {
                    if (++calls != 1) return;
                    int version = victim.Entity.Version; var before = new StateHasher(); victim.Sync(before);
                    if (failure == "clock-throw") game.Clock.Pause();
                    else {
                        int old = victim.FaultMode; victim.FaultMode = 5;
                        try { victim.ReadSignal(world, world.Content.Get<IAutomationSignalContent>("probe.level")); }
                        finally { victim.FaultMode = old; }
                    }
                    var after = new StateHasher(); victim.Sync(after);
                    silentVersion = version == victim.Entity.Version; actualPayloadChange = before.Value != after.Value;
                    if (failure != "silent") throw primary;
                };
                game.Seat(0).Submit(new SetAutomationProbeCommand { Entity = Find(game, "sensor").Id, Level = Fix.FromInt(6), Available = false });
                game.Update();
                c.Equal(1, calls, "original failed recovery callback runs once with no post-fault retry: " + context);
                c.That(silentVersion && (failure == "clock-throw" || actualPayloadChange),
                    "probe changes actual saved payload without an invented participant version: " + context);
                c.Equal("observation.provider", world.Fault?.Phase, "existing observation owns recovery boundary failure: " + context);
                if (failure != "silent") c.Equal("InvalidOperationException: proof.original-recovery-callback", world.Fault?.Message,
                    "verification cannot mask the original recovery callback exception: " + context);
                c.Equal(time, game.Tick, "failed recovery never completes the pending game step: " + context);
                var actual = target.ReadControl();
                c.That(actual.Enabled == control.Enabled && actual.Target == control.Target,
                    "failed input recovery applies no stale ordinary control action: " + context);
                c.Equal(published.Tick, game.Seat(0).Snapshot.Tick, "fault publication retains last healthy entity facts: " + context);
                var afterRules = new StateHasher(); world.System<AutomationSystem>().Sync(afterRules);
                c.Equal(priorRules.Value, afterRules.Value, "actual automation codec has no wait/action success prefix after failed policy: " + context);
                c.That(!world.Events.HasPending, "failed recovery publishes no action success event: " + context);
                if (world.Fault != null) {
                    c.Throws<InvalidOperationException>(() => game.Save(), "failed recovery cannot checkpoint healthy continuation: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "failed recovery cannot certify healthy state: " + context);
                }
                c.Equal(isolated, independent.Hash(), "separate restored pending session is untouched: " + context);
                long beforeTime = independent.Tick; independent.Update();
                c.That(independent.Tick > beforeTime && independent.Fault == null, "separate pending session actually advances normally: " + context);
            }
        }
        internal static void Execution(Check c)
        {
            GameFactory factory = WaitingFactory(); SimGame game = New(factory); Configure(game, Settings(game)); Level(c, game, 6, false);
            SimGame twin = factory.Load(game.Save()); ulong hash = game.Hash(); long current = game.Tick;
            game.Clock.Pause(); game.Update(); game.Update();
            c.Equal(current, game.Tick, "paused single clock freezes all recovery checks");
            c.Equal(hash, game.Hash(), "waiting policy advances no independent time or work while paused"); game.Clock.Resume();
            Advance(game, 5, twin); c.That(Rule(game).WaitingForInputs && Rule(twin).WaitingForInputs, "long unavailable interval remains explicit waiting");
            Level(c, game, 6); Level(c, twin, 6); long began = game.Tick - 1;
            c.That(Rule(game).CandidateSince == began && !Rule(game).PendingDecision.HasValue && Enabled(game),
                "recovery begins fresh authored dwell, never accumulates unavailable-time credit");
            Advance(game, 1, twin); c.That(!Rule(game).PendingDecision.HasValue && Enabled(game), "first fresh minute does not complete two-minute dwell");
            Advance(game, 1, twin); c.That(Rule(game).PendingDecision == true && Enabled(game), "recovered stability queues next-step action only");
            twin = factory.Load(game.Save()); Advance(game, 1, twin);
            c.That(!Enabled(game) && Rule(game).HasApplied && Rule(game).Latch, "normal handler applies recovered action exactly once");
            c.Equal(game.Hash(), twin.Hash(), "saved waiting/dwell/pending continuation is identical");
            long cooldown = Rule(game).CooldownUntil; Level(c, game, 2, false); Level(c, twin, 2, false);
            c.That(Rule(game).WaitingForInputs && Rule(game).Latch && Rule(game).CooldownUntil == cooldown,
                "input loss preserves actual applied latch and original cooldown deadline");
            Level(c, game, 2); Level(c, twin, 2); Advance(game, 2, twin);
            c.That(Rule(game).PendingDecision == false && !Enabled(game), "fresh false dwell also respects original cooldown before restart");
            Advance(game, 1, twin); c.That(Enabled(game) && !Rule(game).Latch, "recovered low input restarts through original control contract");
            c.Equal(game.Hash(), twin.Hash(), "interleaved recovery/cooldown sessions remain isolated and identical");

            factory = WaitingFactory(independent: true, immediate: true); game = New(factory); Configure(game, Settings(game));
            c.That(Rule(game).PendingDecision == true, "ordinary immediate profile starts a next-step decision, not instant action");
            int decisions = 0;
            Probe(game).ReadProbe = () => { if (game.World.Stepping) decisions++; };
            Probe(game).Calls = 0; Level(c, game, 6, false);
            c.That(Rule(game).WaitingForInputs && !Rule(game).PendingDecision.HasValue && Enabled(game), "offline due input cancels old intent before any control commit");
            c.Equal(1, decisions, "pre-commit failure never retries again later in the same actual Update");
            ulong waitingState = game.Hash(); long waitingTime = game.Tick; int validationCalls = Probe(game).Calls;
            for (int read = 0; read < 3; read++) { Rule(game); Enabled(game); }
            c.That(Probe(game).Calls > validationCalls, "actual read-only codecs validate the waiting policy independently of execution");
            c.Equal(1, decisions, "repeated pure validation never becomes another recovery decision");
            c.Equal(waitingTime, game.Tick, "pure wait validation advances no game time");
            c.Equal(waitingState, game.Hash(), "pure wait validation changes no saved state");
            decisions = 0; Update(game);
            c.Equal(1, decisions, "a later actual unavailable Update earns exactly one new recovery decision");
            Probe(game).ReadProbe = null;
            Level(c, game, 6); c.That(Rule(game).PendingDecision == true && Enabled(game), "recovered immediate policy creates a new next-step intent");
            Update(game); c.That(!Enabled(game), "independent policy never bypasses normal deferred command execution");

            factory = WaitingFactory(); game = New(factory); long far = (1L << 45) + 37;
            WideTimeTests.SetTick(game, far); Configure(game, Settings(game)); Level(c, game, 6, false);
            twin = factory.Load(game.Save()); Advance(game, 3, twin); Level(c, game, 6); Level(c, twin, 6);
            c.Equal(game.Tick - 1, Rule(game).CandidateSince, "recovery stability retains the single clock's full-width timestamp");
            c.That(Rule(game).CandidateSince > far && !Rule(game).PendingDecision.HasValue, "large-time recovery has no narrowing or lost-time credit");
            Advance(game, 3, twin); c.That(!Enabled(game) && Rule(game).LastAppliedAt > far, "full-width recovered action still runs at its exact normal deadline");
            c.Equal(game.Hash(), twin.Hash(), "large-time restored recovery remains deterministic");

        }
        internal static void PhysicalNetwork(Check c)
        {
            GameFactory factory = NetworkSignalTests.Factory(root => root.Get("control_rules")[0].Set("input_recovery", JsonValue.Of("proof.input.network")));
            SimGame game = NetworkSignalTests.Setup(factory, out Entity source, out Entity watcher, out Entity independent);
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && !game.World.Services.Has<IBusinessAccounts>(),
                "physical signal recovery requires no supplier, money or business module");
            var settings = new AutomationSettings("pool.fill", true, new ControlBinding(source.Id, new Code("valve")),
                new Dictionary<string, ControlBinding> { ["stock"] = new ControlBinding(independent.Id, new Code("pool")) }, ControlThresholds.Initial(game.World.Content.Get<IControlRule>("pool.fill")),
                game.World.Content.Get<IControlRule>("pool.fill").WhenTrue, game.World.Content.Get<IControlRule>("pool.fill").WhenFalse);
            c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "fill", Settings = settings }).Accepted,
                "player configures waiting network profile while connection is unsettled");
            var view = View(game); view.TryGetAutomationRule(new PlayerId(0), "fill", out var waiting);
            c.That(waiting.WaitingForInputs && waiting.WaitingInput == "stock" && waiting.Blocker == NetworkService.TopologyPending,
                "actual pending topology is waiting, not a fabricated stock signal or permanent disable");
            SimGame twin = factory.Load(game.Save()); Advance(game, 3, twin);
            view.TryGetAutomationRule(new PlayerId(0), "fill", out var state); view.TryGetControl(source.Id, new Code("valve"), out var valve);
            c.That(!state.WaitingForInputs && state.HasApplied && state.Latch && !valve.Enabled,
                "network resumes automatically then physically closes actual valve through normal commands");
            ResourceId steel = game.World.Content.ResourceId("steel"); var lines = game.World.Services.Providers<NetworkService>().Single();
            c.Equal(Fix.FromInt(10), lines.Component("arbitration.main", 0).Amount(steel), "actual pool contains only earned physical source transfers");
            c.Equal(Fix.FromInt(20), source.Get<IInventory>().Amount(steel), "waiting/recovery creates no duplicate pool stock");
            c.Equal(game.Hash(), twin.Hash(), "waiting connected pool and physical valve continue identically after restore");
            var movement = new ResourceMovementIntent(new ReservationOwner("proof.input-withdraw", 1), new PlayerId(0),
                lines.Component("arbitration.main", 0).Endpoint, ResourceEndpointId.ForEntity(watcher.Id),
                new Bill(new[] { new ResourceAmount(steel, Fix.FromInt(6)) }), Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
            c.That(game.World.Services.Get<IResourceTransaction>().CommitMovement(movement).Committed, "real receipt protocol physically withdraws six units");
            Update(game); twin = factory.Load(game.Save()); Advance(game, 1, twin);
            view.TryGetControl(source.Id, new Code("valve"), out valve);
            c.That(valve.Enabled, "post-recovery stock hysteresis still reopens actual valve on the next game update");
            c.Equal(Fix.FromInt(30), source.Get<IInventory>().Amount(steel) + watcher.Get<IInventory>().Amount(steel) + lines.Component("arbitration.main", 0).Amount(steel),
                "physical network recovery conserves total native resource exactly");
            c.Equal(game.Hash(), twin.Hash(), "saved post-wait withdrawal and restart remain deterministic");
        }
    }
}
