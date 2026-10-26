using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.AutomationTests;

namespace Beep.SimEngine.Tests
{
    // Direct original-owner fault instrumentation, not player gameplay or a second rule executor.
    internal static class RuleProviderObservationTests
    {
        internal const string Case = "Rule providers: original evaluation participants and composition errors remain inside one observation owner";
        internal const string DwellCase = "Rule dwell admission: bound metadata faults cannot publish candidate state or deferred intent";
        internal const string ConfigurationCase = "Rule configuration admission: disclosure and initial metadata preserve bound state and existing records";
        private static ulong Rules(SimGame game)
        { var hash = new StateHasher(); game.World.System<AutomationSystem>().Sync(hash); return hash.Value; }
        private static AutomationSettings Bound(SimGame game, bool shared)
        {
            var settings = Settings(game, "mod.band");
            return !shared ? settings : new AutomationSettings(settings.Profile, settings.Enabled,
                Binding(Find(game, "sensor")), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
        }
        internal static void Configuration(Check c)
        {
            SimGame Prepared(GameFactory factory, bool prior, bool loaded)
            {
                var game = New(factory);
                if (prior) c.That(Configure(game, Settings(game)).Accepted, "ordinary previous profile exists before rebind");
                return loaded ? factory.Load(game.Save()) : game;
            }
            int contexts = 0;
            foreach (bool prior in new[] { false, true })
            foreach (bool loaded in new[] { false, true })
            foreach (string participant in new[] { "sensor", "actuator", "shared" })
            {
                var factory = Factory(); var healthy = Prepared(factory, prior, loaded);
                var settings = Bound(healthy, participant == "shared"); ulong hash = healthy.Hash();
                c.Equal(Code.None, View(healthy).PreviewAutomation("field", settings), "healthy public profile admission");
                c.Equal(hash, healthy.Hash(), "complete healthy configure preview is pure");
                var twin = factory.Load(healthy.Save());
                healthy.Seat(0).Submit(new ConfigureAutomationCommand { Rule = "field", Settings = settings });
                twin.Seat(0).Submit(new ConfigureAutomationCommand { Rule = "field", Settings = settings });
                healthy.Update(); twin.Update(); healthy.Update(); twin.Update();
                c.That(healthy.Fault == null && Rule(healthy).Settings.Profile == "mod.band" && Rule(healthy).HasApplied && healthy.Hash() == twin.Hash(),
                    "fresh/rebound original record has healthy saved deferred continuation");
                var disabled = new AutomationSettings(settings.Profile, false, settings.Target, settings.Inputs,
                    settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
                healthy.World.Services.Get<IAutomationRules>().Configure(new PlayerId(0), "field", disabled);
                twin.World.Services.Get<IAutomationRules>().Configure(new PlayerId(0), "field", disabled);
                c.That(Rule(healthy).HasApplied && !Rule(healthy).Settings.Enabled && healthy.Hash() == twin.Hash(),
                    "same-binding engine edit retains actual applied history, not a new seed");
                foreach (string stage in new[] { "disclosure", "initial-state", "initial-latch" })
                foreach (string entry in new[] { "preview", "direct", "inbox" })
                foreach (string mode in new[] { "silent", "payload-throw", "clock-throw" })
                {
                    if (entry == "preview" && stage != "disclosure") continue; // Preview does not seed a rule.
                    contexts++; string context = stage + "/" + entry + "/" + participant + "/" + mode + "/prior=" + prior + "/loaded=" + loaded;
                    factory = Factory(); var game = Prepared(factory, prior, loaded);
                    settings = Bound(game, participant == "shared");
                    var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                    var world = game.World; ulong before = Rules(game); long time = game.Tick;
                    var published = game.Seat(0).Snapshot;
                    var victim = Find(game, participant == "actuator" ? "actuator" : "sensor").Get<AutomationProbe>();
                    var sensor = Find(game, "sensor"); int sensorVersion = sensor.Version;
                    var rule = world.Content.Get<BandControlRuleDef>("mod.band");
                    var primary = new InvalidOperationException("proof.original-configuration-callback");
                    int calls = 0; bool payload = false, unchangedVersion = false;
                    if (entry == "inbox") {
                        game.Seat(0).Submit(new ConfigureAutomationCommand { Rule = "field", Settings = settings });
                        game.Seat(0).Submit(new SetAutomationProbeCommand { Entity = sensor.Id, Level = Fix.FromInt(99), Available = true });
                    }
                    Action invoke = () => {
                        if (++calls != 1) return;
                        int version = victim.Entity.Version; var old = new StateHasher(); victim.Sync(old);
                        if (mode == "clock-throw") game.Clock.Pause();
                        else {
                            int oldMode = victim.FaultMode; victim.FaultMode = 5;
                            try { victim.ReadSignal(world, world.Content.Get<IAutomationSignalContent>("probe.level")); }
                            finally { victim.FaultMode = oldMode; }
                        }
                        var next = new StateHasher(); victim.Sync(next);
                        payload = old.Value != next.Value; unchangedVersion = version == victim.Entity.Version;
                        if (mode != "silent") throw primary;
                    };
                    if (stage == "disclosure") rule.RecoveryMetadataProbe = invoke;
                    else if (stage == "initial-state") rule.InitialStateMetadataProbe = invoke;
                    else rule.InitialLatchMetadataProbe = invoke;
                    Exception thrown = null;
                    try {
                        if (entry == "preview") View(game).PreviewAutomation("field", settings);
                        else if (entry == "direct") world.Services.Get<IAutomationRules>().Configure(new PlayerId(0), "field", settings);
                        else game.Update();
                    }
                    catch (Exception error) { thrown = error; }
                    finally { rule.RecoveryMetadataProbe = rule.InitialStateMetadataProbe = rule.InitialLatchMetadataProbe = null; }
                    c.That(calls > 0 && unchangedVersion && (mode == "clock-throw" || payload), "actual configuration getter mutates its bound state without Touch: " + context);
                    c.Equal("observation.provider", world.Fault?.Phase, "existing observer owns configuration metadata interval: " + context);
                    if (mode != "silent") c.Equal("InvalidOperationException: proof.original-configuration-callback", world.Fault?.Message,
                        "original configuration cause remains first: " + context);
                    if (entry != "inbox") c.That(thrown is InvalidOperationException && (mode == "silent" || ReferenceEquals(thrown, primary)),
                        "direct/preview original failure propagates without wrapping: " + context);
                    ulong? after = null; try { after = Rules(game); } catch (Exception) { }
                    c.That(after == before, "failed metadata neither adds a record nor replaces previous state: " + context);
                    c.Equal(time, game.Tick, "failed configuration completes no game step: " + context);
                    c.Equal(published.Tick, game.Seat(0).Snapshot.Tick, "failed configuration retains healthy publication: " + context);
                    c.That(!world.Events.HasPending, "failed configuration has no success events: " + context);
                    if (entry == "inbox") c.That(world.LastTickResults.Count == 0 && sensor.Version == sensorVersion,
                        "no accepted configuration or later queued command prefix: " + context);
                    if (world.Fault != null) {
                        c.Throws<InvalidOperationException>(() => game.Save(), "invalid configuration cannot save as healthy: " + context);
                        c.Throws<InvalidOperationException>(() => game.Hash(), "invalid configuration cannot certify a hash: " + context);
                    }
                    c.Equal(isolated, independent.Hash(), "separate saved session remains untouched: " + context);
                    independent.World.Services.Get<IAutomationRules>().Configure(new PlayerId(0), "field", settings);
                    independent.Update();
                    c.That(independent.Tick > time && independent.Fault == null, "independent ordinary rule continues after hook removal: " + context);
                }
            }
            c.Equal(252, contexts, "complete selected configuration entry-point and binding matrix");
            var refusalGame = New(Factory()); var valid = Bound(refusalGame, false);
            var refusals = new[] {
                (Id: "null", Settings: (AutomationSettings)null, Expected: AutomationCodes.InvalidSettings),
                (Id: "bad id", Settings: valid, Expected: AutomationCodes.InvalidSettings),
                (Id: "unknown", Settings: new AutomationSettings("missing", valid.Enabled, valid.Target, valid.Inputs,
                    valid.Thresholds, valid.WhenTrue, valid.WhenFalse), Expected: Codes.UnknownDef),
                (Id: "missing-input", Settings: new AutomationSettings(valid.Profile, valid.Enabled, valid.Target,
                    new System.Collections.Generic.Dictionary<string, ControlBinding>(), valid.Thresholds, valid.WhenTrue, valid.WhenFalse), Expected: AutomationCodes.InvalidSettings),
                (Id: "foreign-target", Settings: new AutomationSettings(valid.Profile, valid.Enabled, Binding(Find(refusalGame, "actuator", 1)),
                    valid.Inputs, valid.Thresholds, valid.WhenTrue, valid.WhenFalse), Expected: Codes.NotOwned),
                (Id: "foreign-source", Settings: new AutomationSettings(valid.Profile, valid.Enabled, valid.Target,
                    new System.Collections.Generic.Dictionary<string, ControlBinding> { ["level"] = Binding(Find(refusalGame, "sensor", 1)) },
                    valid.Thresholds, valid.WhenTrue, valid.WhenFalse), Expected: Codes.NotOwned)
            };
            foreach (var refusal in refusals) {
                ulong before = refusalGame.Hash();
                c.Equal(refusal.Expected, View(refusalGame).PreviewAutomation(refusal.Id, refusal.Settings), "original typed preview refusal: " + refusal.Id);
                c.Equal(before, refusalGame.Hash(), "typed refusal preview stays pure: " + refusal.Id);
                if (refusal.Settings == null) {
                    c.Throws<ArgumentNullException>(() => refusalGame.Seat(0).Submit(new ConfigureAutomationCommand { Rule = refusal.Id }),
                        "absent required settings cannot be serialized into a fabricated inbox intent");
                    c.Equal(before, refusalGame.Hash(), "malformed input cannot allocate sequence or enqueue a prefix");
                    c.That(refusalGame.Fault == null, "malformed caller input is not a simulation fault");
                    continue;
                }
                c.Equal(refusal.Expected, Configure(refusalGame, refusal.Settings, refusal.Id).Refusal, "ordinary inbox retains typed refusal: " + refusal.Id);
                c.That(refusalGame.Fault == null && Rule(refusalGame, refusal.Id) == null, "typed refusal neither faults nor creates a rule: " + refusal.Id);
            }
            c.Equal(Codes.NotSeated, refusalGame.World.Services.Get<IAutomationRules>().CheckConfigure(PlayerId.None, "field", null),
                "unseated refusal precedes null settings without fabricating an actor");
            c.That(Configure(refusalGame, valid).Accepted, "actual enabled target owner still configures normally");
            c.Equal(AutomationCodes.Conflict, View(refusalGame).PreviewAutomation("other", valid), "existing target arbitration is unchanged");
        }
        internal static void Dwell(Check c)
        {
            void Change(SimGame game) => game.Seat(0).Submit(new SetAutomationProbeCommand {
                Entity = Find(game, "sensor").Id, Level = Fix.Zero, Available = true
            });
            SimGame Prepared(GameFactory factory, bool shared, bool loaded)
            {
                var game = New(factory);
                c.That(Configure(game, Bound(game, shared)).Accepted, "ordinary independent zero-dwell rule configured");
                game.Update();
                c.That(Rule(game).HasApplied && !Rule(game).PendingDecision.HasValue, "old false intent settles before testing a new decision");
                return loaded ? factory.Load(game.Save()) : game;
            }
            foreach (bool loaded in new[] { false, true })
            foreach (string participant in new[] { "sensor", "actuator", "shared" })
            {
                var factory = Factory(); var healthy = Prepared(factory, participant == "shared", loaded);
                var twin = factory.Load(healthy.Save()); Change(healthy); Change(twin);
                healthy.Update(); twin.Update();
                c.That(Rule(healthy).PendingDecision == true && healthy.Hash() == twin.Hash(),
                    "healthy condition change records only next-step intent with saved identity");
                healthy.Update(); twin.Update();
                c.That(healthy.Fault == null && Rule(healthy).HasApplied && !Rule(healthy).PendingDecision.HasValue && healthy.Hash() == twin.Hash(),
                    "healthy original action commits next step with saved deterministic continuation");
                foreach (string mode in new[] { "silent", "payload-throw", "clock-throw" })
                {
                    string context = participant + "/" + mode + "/loaded=" + loaded;
                    factory = Factory(); var game = Prepared(factory, participant == "shared", loaded);
                    var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                    var world = game.World; ulong before = Rules(game); long time = game.Tick;
                    var snapshot = game.Seat(0).Snapshot;
                    var victim = Find(game, participant == "actuator" ? "actuator" : "sensor").Get<AutomationProbe>();
                    var rule = world.Content.Get<BandControlRuleDef>("mod.band");
                    var primary = new InvalidOperationException("proof.original-dwell-callback");
                    int calls = 0; bool payload = false, sameVersion = false;
                    Change(game);
                    rule.TimingMetadataProbe = () => {
                        calls++; int version = victim.Entity.Version; var old = new StateHasher(); victim.Sync(old);
                        if (mode == "clock-throw") game.Clock.Pause();
                        else {
                            int oldMode = victim.FaultMode; victim.FaultMode = 5;
                            try { victim.ReadSignal(world, world.Content.Get<IAutomationSignalContent>("probe.level")); }
                            finally { victim.FaultMode = oldMode; }
                        }
                        var next = new StateHasher(); victim.Sync(next);
                        payload = old.Value != next.Value; sameVersion = version == victim.Entity.Version;
                        if (mode != "silent") throw primary;
                    };
                    try { game.Update(); } finally { rule.TimingMetadataProbe = null; }
                    c.Equal(1, calls, "actual dwell metadata read once at new decision admission: " + context);
                    c.That(sameVersion && (mode == "clock-throw" || payload), "actual hostile payload changes without Touch: " + context);
                    c.Equal("observation.provider", world.Fault?.Phase, "existing observer owns invalid dwell interval: " + context);
                    if (mode != "silent") c.Equal("InvalidOperationException: proof.original-dwell-callback", world.Fault?.Message,
                        "original dwell cause is not replaced by later verification: " + context);
                    ulong? after = null; try { after = Rules(game); } catch (Exception) { }
                    c.That(after == before, "failed dwell publishes no state/candidate/pending/due prefix: " + context);
                    c.Equal(time, game.Tick, "invalid dwell cannot complete game time: " + context);
                    c.Equal(snapshot.Tick, game.Seat(0).Snapshot.Tick, "invalid dwell retains healthy published facts: " + context);
                    c.That(!world.Events.HasPending, "invalid dwell publishes no success event: " + context);
                    if (world.Fault != null) {
                        c.Throws<InvalidOperationException>(() => game.Save(), "failed dwell cannot save healthy continuation: " + context);
                        c.Throws<InvalidOperationException>(() => game.Hash(), "failed dwell cannot certify healthy state: " + context);
                    }
                    c.Equal(isolated, independent.Hash(), "saved independent session remains untouched: " + context);
                    Change(independent); independent.Update();
                    c.That(independent.Tick > time && independent.Fault == null && Rule(independent).PendingDecision == true,
                        "independent real engine decision still advances after hook removal: " + context);
                }
            }
        }
        internal static void Run(Check c)
        {
            foreach (bool loaded in new[] { false, true })
            foreach (string participant in new[] { "sensor", "actuator", "shared" })
            {
                var factory = Factory(); var healthy = New(factory);
                c.That(Configure(healthy, Bound(healthy, participant == "shared")).Accepted, "ordinary independent band rule configured");
                healthy.Update(); if (loaded) healthy = factory.Load(healthy.Save());
                var twin = factory.Load(healthy.Save()); healthy.Update(); twin.Update();
                c.That(healthy.Fault == null && healthy.Tick == twin.Tick && healthy.Hash() == twin.Hash(),
                    "actual independent evaluator and shared binding retain saved deterministic continuation");
                foreach (string failure in new[] { "silent", "payload-throw", "clock-throw" })
                {
                    string context = participant + "/" + failure + "/loaded=" + loaded;
                    factory = Factory(); var game = New(factory);
                    c.That(Configure(game, Bound(game, participant == "shared")).Accepted, "ordinary rule owns actual bound identities: " + context);
                    game.Update(); if (loaded) game = factory.Load(game.Save());
                    var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                    var world = game.World; ulong before = Rules(game); long time = game.Tick;
                    var published = game.Seat(0).Snapshot;
                    var victim = Find(game, participant == "actuator" ? "actuator" : "sensor").Get<AutomationProbe>();
                    int calls = 0; bool actualPayload = false, unchangedVersion = false;
                    var primary = new InvalidOperationException("proof.original-rule-callback");
                    world.Content.Get<BandControlRuleDef>("mod.band").EvaluateProbe = () => {
                        calls++; int version = victim.Entity.Version;
                        var old = new StateHasher(); victim.Sync(old);
                        if (failure == "clock-throw") game.Clock.Pause();
                        else {
                            int mode = victim.FaultMode; victim.FaultMode = 5;
                            try { victim.ReadSignal(world, world.Content.Get<IAutomationSignalContent>("probe.level")); }
                            finally { victim.FaultMode = mode; }
                        }
                        var next = new StateHasher(); victim.Sync(next);
                        actualPayload = old.Value != next.Value; unchangedVersion = version == victim.Entity.Version;
                        if (failure != "silent") throw primary;
                    };
                    game.Update();
                    c.Equal(1, calls, "one actual evaluator callback before first-fault cutoff: " + context);
                    c.That(unchangedVersion && (failure == "clock-throw" || actualPayload), "negative probe changes actual original payload without Touch: " + context);
                    c.Equal("observation.provider", world.Fault?.Phase, "original observer owns actual rule failure: " + context);
                    if (failure != "silent") c.Equal("InvalidOperationException: proof.original-rule-callback", world.Fault?.Message,
                        "original evaluator cause is never replaced by verification: " + context);
                    c.Equal(time, game.Tick, "invalid evaluator cannot complete the game step: " + context);
                    c.Equal(published.Tick, game.Seat(0).Snapshot.Tick, "invalid evaluator retains last healthy published facts: " + context);
                    c.Equal(before, Rules(game), "failed evaluation publishes no rule state or next-step intent prefix: " + context);
                    c.That(!world.Events.HasPending, "failed evaluation has no success-event prefix: " + context);
                    if (world.Fault != null) {
                        c.Throws<InvalidOperationException>(() => game.Save(), "invalid evaluator cannot save healthy continuation: " + context);
                        c.Throws<InvalidOperationException>(() => game.Hash(), "invalid evaluator cannot certify healthy state: " + context);
                    }
                    c.Equal(isolated, independent.Hash(), "separate saved session remains unchanged: " + context);
                    // Remove per-content negative hook before the independent session uses this same definition.
                    world.Content.Get<BandControlRuleDef>("mod.band").EvaluateProbe = null;
                    long otherTime = independent.Tick; independent.Update();
                    c.That(independent.Tick > otherTime && independent.Fault == null, "separate saved rule actually continues: " + context);
                }
            }
            foreach (bool loaded in new[] { false, true })
            foreach (string stage in new[] { "node", "action", "rule" })
            {
                var factory = Factory(); var healthy = New(factory); if (loaded) healthy = factory.Load(healthy.Save());
                ulong before = healthy.Hash(); healthy.World.System<AutomationSystem>().ValidateComposition(healthy.World);
                c.Equal(before, healthy.Hash(), "actual composition validator remains pure on valid resolved providers: " + stage);
                foreach (string failure in new[] { "throw", "clock", "clock-throw" })
                {
                    string context = stage + "/" + failure + "/loaded=" + loaded;
                    factory = Factory(); var game = New(factory); if (loaded) game = factory.Load(game.Save());
                    var world = game.World; var primary = new InvalidOperationException("proof.original-composition-callback");
                    int calls = 0; Action invoke = () => {
                        calls++; if (failure != "throw") game.Clock.Pause();
                        if (failure != "clock") throw primary;
                    };
                    if (stage == "node") world.Content.Get<BandControlRuleDef>("mod.band").RecoveryMetadataProbe = invoke;
                    else if (stage == "rule") world.Content.Get<BandControlRuleDef>("mod.band").TimingMetadataProbe = invoke;
                    else world.Content.Get<ProbeControlActionPolicyDef>("proof.probe-actions").ValidateProbe = invoke;
                    Exception thrown = null; long time = game.Tick;
                    try { world.System<AutomationSystem>().ValidateComposition(world); } catch (Exception error) { thrown = error; }
                    c.Equal(1, calls, "actual composition callback is not retried after invalid result: " + context);
                    c.That(thrown is InvalidOperationException, "actual composition callback refusal propagates: " + context);
                    if (failure != "clock") c.That(ReferenceEquals(primary, thrown), "composition preserves original exception identity: " + context);
                    c.Equal("observation.provider", world.Fault?.Phase, "original observer owns invalid composition interval: " + context);
                    c.Equal(time, game.Tick, "composition adds no independent game time: " + context);
                    if (world.Fault != null) c.Throws<InvalidOperationException>(() => game.Save(), "invalid composition cannot resume as healthy: " + context);
                }
            }
        }
    }
}
