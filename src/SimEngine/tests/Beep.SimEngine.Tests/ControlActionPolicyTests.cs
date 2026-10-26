using System;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.AutomationTests;

namespace Beep.SimEngine.Tests
{
    internal static class ControlActionPolicyTests
    {
        internal const string ParticipantCase = "Control action participants: original codecs reject silent mutation and preserve callback failures across pure dispatch";
        internal static GameFactory Independent() => Factory(root => {
            var rule = root.Get("control_rules")[0]; rule.Set("action_policy", JsonValue.Of("proof.probe-actions"));
            rule.Set("when_true", Json.Parse("{\"enabled\":true,\"target\":10,\"unit\":\"unit.level\"}"));
            rule.Set("when_false", Json.Parse("{\"enabled\":true,\"target\":2,\"unit\":\"unit.level\"}"));
        });
        private static ProbeControlActionPolicyDef Policy(SimGame game) => game.World.Content.Get<ProbeControlActionPolicyDef>("proof.probe-actions");
        private static SetAutomationProbeHandler Handler(SimGame game)
        { game.World.Commands.TryGet("proof.set-automation-level", out var entry); return (SetAutomationProbeHandler)entry.Handler; }
        private static Fix ActualLevel(SimGame game)
        {
            var id = Find(game, "actuator").Id;
            Code refusal = View(game).ReadSignal(id, new Code("probe"), "probe.level", out var value);
            if (!refusal.IsNone || !value.Available) throw new InvalidOperationException("actual action target signal unavailable");
            return value.Value.Value;
        }

        internal static void Contracts(Check c)
        {
            foreach (string fault in new[] { "missing", "unknown", "wrong-kind", "command", "owner", "type", "unbound" })
                c.Throws<ContentException>(() => New(Factory(root => {
                    var rule = root.Get("control_rules")[0]; var policy = root.Get("operation_control_actions")[0];
                    switch (fault)
                    {
                        case "missing": rule.Remove("action_policy"); break;
                        case "unknown": rule.Set("action_policy", JsonValue.Of("missing")); break;
                        case "wrong-kind": rule.Set("action_policy", JsonValue.Of("probe.level")); break;
                        case "command": policy.Set("command", JsonValue.Of("missing")); break;
                        case "owner": policy.Set("owner", JsonValue.Of("proof.automation")); break;
                        case "type": policy.Set("command", JsonValue.Of("proof.set-automation-level")); policy.Set("owner", JsonValue.Of("proof.automation")); break;
                        case "unbound": policy.Set("command", JsonValue.Of("sim.remove-automation")); policy.Set("owner", JsonValue.Of(AutomationModule.ModuleId)); break;
                    }
                })), "explicit action policy/command composition refuses before Update: " + fault);

            var factory = Independent(); var game = New(factory); var settings = Settings(game); ulong hash = game.Hash();
            c.That(View(game).TryGetAutomationProfile("high-level", out var profile) && profile.ActionPolicy == "proof.probe-actions"
                && profile.ActionCommand == "proof.set-automation-level" && profile.ActionCommandOwner == "proof.automation",
                "complete immutable profile exposes selected action and normal command identities");
            c.Equal(Code.None, View(game).PreviewAutomation("field", settings), "independent action with different unit passes provider binding, not concrete operation unit gate");
            c.Equal(hash, game.Hash(), "independent action preview constructs no gameplay state");
            var wrongUnit = New(Factory(root => root.Get("control_rules")[0].Set("action_policy", JsonValue.Of("proof.probe-actions"))));
            c.Equal(AutomationCodes.UnitMismatch, View(wrongUnit).PreviewAutomation("field", Settings(wrongUnit)), "independent policy interprets exact own action unit");
            var foreign = new AutomationSettings(settings.Profile, settings.Enabled, Binding(Find(game, "actuator", 1)), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
            c.Equal(Codes.NotOwned, View(game).PreviewAutomation("foreign", foreign), "generic action cannot grant target ownership");
            c.Equal(Codes.NotOwned, Configure(game, foreign, "foreign").Refusal, "normal configure agrees on foreign action target");
            var absent = new AutomationSettings(settings.Profile, settings.Enabled,
                new ControlBinding(settings.Target.Entity, new Code("missing")), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
            c.Equal(AutomationCodes.NotSignalSource, View(game).PreviewAutomation("missing", absent), "provider alone interprets named target capability");
            hash = game.Hash();
            c.Equal(Code.None, ControlActionDispatch.Prepare(game.World, new PlayerId(0), settings, profile.WhenTrue, out var entry, out var prepared),
                "shared dispatcher resolves actual independent command handler");
            ((SetAutomationProbeCommand)Policy(game).Retained).Level = Fix.FromInt(99);
            c.That(prepared is SetAutomationProbeCommand detached && detached.Level == Fix.FromInt(10)
                && prepared.Player == new PlayerId(0) && prepared.IssuedTick == game.Clock.StepIndex,
                "existing command codec snapshots provider-retained command and engine stamps actual sender/time");
            c.That(entry.Id == profile.ActionCommand && entry.Owner == profile.ActionCommandOwner, "selected registration cannot be a fabricated normal command");
            c.Equal(hash, game.Hash(), "prepared command and mutated detached provider template are not authoritative state");
            var targets = new System.Collections.Generic.List<Code>();
            c.That(View(game).CopyControlTargetIds(settings.Target.Entity, profile.Id, targets) && targets.Count == 1
                && targets[0] == settings.Target.Capability, "public action provider exposes actual targets without host type checks");
            var mutated = New(Independent()); var mutatedTarget = Find(mutated, "actuator");
            Policy(mutated).ReadProbe = () => mutatedTarget.Touch();
            c.Throws<InvalidOperationException>(() => View(mutated).CopyControlTargetIds(mutatedTarget.Id, "high-level", targets),
                "target discovery guards the actual participating entity against provider mutation");
            Faults(c);
        }

        private static void Faults(Check c)
        {
            foreach (int mode in new[] { 1, 2, 3, 4, 5, 6, 7, 8 })
            {
                var game = New(Independent()); var settings = Settings(game); var policy = Policy(game);
                policy.FaultMode = mode <= 4 ? mode : 0; policy.Redirect = Find(game, "actuator", 1).Id;
                if (mode == 5) policy.CommandProbe = command => command.IssuedTick = 100;
                if (mode == 6) policy.CommandProbe = command => Find(game, "sensor").Touch();
                if (mode == 7) policy.CommandProbe = command => ((SetAutomationProbeCommand)command).SyncProbe = () => Find(game, "sensor").Touch();
                if (mode == 8) Handler(game).CheckProbe = command => command.Level = Fix.FromInt(77);
                long time = game.Clock.StepIndex;
                string beforeRules = RuleBytes(game);
                c.Throws<InvalidOperationException>(() => View(game).PreviewAutomation("fault", settings), "pure action/normal command boundary faults invalid provider: " + mode);
                c.Equal(time, game.Clock.StepIndex, "action fault creates no independent time advance: " + mode);
                c.Equal(beforeRules, RuleBytes(game), "actual automation codec contains no rule prefix after invalid preview: " + mode);
                c.Throws<InvalidOperationException>(() => Rule(game, "fault"), "normal rule views reject after action fault: " + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "no healthy save follows action fault: " + mode);
                c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy hash follows action fault: " + mode);
            }
            // Negative-only white-box instrumentation of the original state owner, not a healthy query.
            static string RuleBytes(SimGame game)
            {
                using (var stream = new MemoryStream())
                {
                    game.World.System<AutomationSystem>().Sync(new StateWriter(stream));
                    return Convert.ToBase64String(stream.ToArray());
                }
            }
        }

        internal static void Participants(Check c)
        {
            foreach (bool loaded in new[] { false, true })
            foreach (bool refused in new[] { false, true })
            foreach (string participant in new[] { "target", "input", "shared" })
            {
                var healthyFactory = Independent(); var healthy = New(healthyFactory);
                if (loaded) healthy = healthyFactory.Load(healthy.Save());
                var settings = Settings(healthy);
                if (participant == "shared") settings = new AutomationSettings(settings.Profile, settings.Enabled,
                    Binding(Find(healthy, "sensor")), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
                Code held = new Code("proof.action-held"); if (refused) Handler(healthy).RefusalProbe = held;
                ulong before = healthy.Hash();
                c.Equal(refused ? held : Code.None, View(healthy).PreviewAutomation("pure", settings), "actual pure ready/refused action keeps its normal result");
                c.Equal(before, healthy.Hash(), "actual pure participant admission changes no saved state");
                var healthyTwin = healthyFactory.Load(healthy.Save());
                healthy.Update(); healthyTwin.Update();
                c.Equal(healthy.Hash(), healthyTwin.Hash(), "pure admission retains normal saved continuation");
                foreach (string stage in new[] { "binding", "create", "copy", "check", "fingerprint" })
                foreach (string failure in new[] { "silent", "payload-throw", "clock-throw" })
                {
                    string context = participant + "/" + stage + "/" + failure + "/loaded=" + loaded + "/refused=" + refused;
                    var factory = Independent(); var game = New(factory); if (loaded) game = factory.Load(game.Save());
                    var world = game.World; settings = Settings(game);
                    if (participant == "shared") settings = new AutomationSettings(settings.Profile, settings.Enabled,
                        Binding(Find(game, "sensor")), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
                    var victim = Find(game, participant == "target" ? "actuator" : "sensor");
                    var probe = victim.Get<AutomationProbe>(); var policy = Policy(game); var handler = Handler(game);
                    if (refused) handler.RefusalProbe = held;
                    var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                    var published = game.Seat(0).Snapshot; long time = game.Tick; int version = victim.Version;
                    var priorRules = new StateHasher(); world.System<AutomationSystem>().Sync(priorRules);
                    var primary = new InvalidOperationException("proof.original-action-callback");
                    int calls = 0, updated = 0; game.Updated += _ => updated++;
                    Action mutate = () => {
                        if (++calls != 1) return;
                        if (failure == "clock-throw") game.Clock.Pause();
                        else {
                            // Deliberate protocol violation through the actual original saved field,
                            // using existing negative signal instrumentation; no copied state owner.
                            int oldMode = probe.FaultMode; probe.FaultMode = 5;
                            try { probe.ReadSignal(world, world.Content.Get<IAutomationSignalContent>("probe.level")); }
                            finally { probe.FaultMode = oldMode; }
                        }
                        if (failure != "silent") throw primary;
                    };
                    if (stage == "binding") policy.ReadProbe = mutate;
                    else if (stage == "create") policy.CommandProbe = _ => mutate();
                    else if (stage == "copy") policy.CommandProbe = command => ((SetAutomationProbeCommand)command).SyncProbe = mutate;
                    else if (stage == "check") handler.CheckProbe = _ => mutate();
                    else handler.CheckProbe = command => command.SyncProbe = mutate;
                    Exception thrown = null;
                    try { View(game).PreviewAutomation("invalid", settings); } catch (Exception error) { thrown = error; }
                    c.Equal(1, calls, "one actual provider callback without fallback/retry: " + context);
                    c.That(thrown is InvalidOperationException && world.Fault != null, "actual participant violation cannot return healthy admission: " + context);
                    if (failure != "silent") c.That(ReferenceEquals(primary, thrown), "verification cannot mask the original callback exception: " + context);
                    c.Equal("observation.provider", world.Fault?.Phase, "existing observation owner retains the fault: " + context);
                    c.Equal(version, victim.Version, "silent payload violation is not manufactured as a Touch/version change: " + context);
                    c.Equal(time, game.Tick, "pure action admission advances no independent game time: " + context);
                    c.Equal(0, updated, "invalid action admission emits no game Update: " + context);
                    c.That(ReferenceEquals(published, game.Seat(0).Snapshot), "no unfinished action state is published: " + context);
                    var afterRules = new StateHasher(); world.System<AutomationSystem>().Sync(afterRules);
                    c.Equal(priorRules.Value, afterRules.Value, "actual automation codec contains no new rule/intent prefix: " + context);
                    if (world.Fault != null) {
                        c.Throws<InvalidOperationException>(() => game.Save(), "violated action admission cannot save: " + context);
                        c.Throws<InvalidOperationException>(() => game.Hash(), "violated action admission cannot certify healthy state: " + context);
                    }
                    c.That(!world.Events.HasPending, "no action success event prefix: " + context);
                    c.Equal(isolated, independent.Hash(), "separate restored session is unchanged: " + context);
                    long independentTime = independent.Tick; independent.Update();
                    c.That(independent.Tick > independentTime && independent.Fault == null, "separate restored session actually advances: " + context);
                }
            }
        }
        internal static void Execution(Check c)
        {
            var factory = Independent(); var game = New(factory); var settings = Settings(game);
            c.That(Configure(game, settings).Accepted, "player configures ordinary authored rule with independent action policy");
            game.Update(); game.Update();
            c.That(Rule(game).PendingDecision == true && ActualLevel(game) == Fix.Zero && Enabled(game), "new action still earns dwell and queues only a next-update decision");
            ((SetAutomationProbeCommand)Policy(game).Retained).Level = Fix.FromInt(99);
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "pending independent action decision saves through existing rule codec");
            game.Clock.Pause(); ulong paused = game.Hash();
            c.Equal(0, game.Update(), "sole game-clock pause holds generic action"); c.Equal(paused, game.Hash(), "paused action changes no rule or target state");
            game.Clock.Resume(); game.Update(); twin.Update();
            c.That(ActualLevel(game) == Fix.FromInt(10) && Enabled(game) && Rule(game).Latch,
                "ordinary independent handler changes environment value, not operation control or retained stale template");
            c.Equal(game.Hash(), twin.Hash(), "saved deferred normal handler commit remains identical");
            var source = Find(game, "sensor").Id;
            c.That(TestKit.Order(game, 0, new SetAutomationProbeCommand { Entity = source, Level = Fix.FromInt(2), Available = true }).Accepted,
                "player changes actual signal through same normal environment command");
            c.That(ActualLevel(game) == Fix.FromInt(10), "opposite generic action never ignores dwell or actual cooldown");
            twin = factory.Load(game.Save());
            for (int i = 0; i < 16; i++) { game.Update(); twin.Update(); }
            c.That(ActualLevel(game) == Fix.FromInt(2) && !Rule(game).Latch && Enabled(game), "opposite action uses normal handler after full dwell/cooldown");
            c.Equal(game.Hash(), twin.Hash(), "saved independent action cooldown/transition stays deterministic");

            var held = New(factory); Configure(held, Settings(held)); held.Update(); held.Update();
            var blocker = new Code("proof.control-held"); Handler(held).RefusalProbe = blocker; held.Update();
            c.That(Rule(held).Suspended && Rule(held).Blocker == blocker && !Rule(held).HasApplied && ActualLevel(held) == Fix.Zero,
                "normal handler refusal at actual commit suspends without applying an operation fallback");
            var cancelled = New(factory); Configure(cancelled, Settings(cancelled)); cancelled.Update(); cancelled.Update();
            c.That(TestKit.Order(cancelled, 0, new RemoveAutomationCommand { Rule = "field" }).Accepted
                && Rule(cancelled) == null && ActualLevel(cancelled) == Fix.Zero, "ordinary removal cancels generic pending decision before due phase");
        }
    }
}
