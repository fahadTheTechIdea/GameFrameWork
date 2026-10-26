using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.AutomationTests;

namespace Beep.SimEngine.Tests
{
    // Explicit fault instrumentation of actual normal command/metadata owners, not host gameplay.
    internal static class DeferredActionCommitTests
    {
        internal const string Case = "Deferred action commits: metadata admission and swallowed native faults cannot publish applied rule state";
        private static ulong Rules(SimGame game)
        { var hash = new StateHasher(); game.World.System<AutomationSystem>().Sync(hash); return hash.Value; }
        private static Fix Actual(SimGame game, string entity)
            => Find(game, entity).Get<AutomationProbe>().ReadSignal(game.World,
                game.World.Content.Get<IAutomationSignalContent>("probe.level")).Value.Value;
        private static SimGame Pending(GameFactory factory)
        {
            var game = New(factory); var first = Settings(game);
            var second = new AutomationSettings(first.Profile, first.Enabled, Binding(Find(game, "sensor")),
                first.Inputs, first.Thresholds, first.WhenTrue, first.WhenFalse);
            game.Seat(0).Submit(new ConfigureAutomationCommand { Rule = "alpha", Settings = first });
            game.Seat(0).Submit(new ConfigureAutomationCommand { Rule = "beta", Settings = second });
            game.Update(); game.Update(); game.Update(); return game;
        }
        internal static void Run(Check c)
        {
            foreach (bool loaded in new[] { false, true })
            {
                var factory = ControlActionPolicyTests.Independent(); var healthy = Pending(factory);
                if (loaded) healthy = factory.Load(healthy.Save());
                c.That(Rule(healthy, "alpha").PendingDecision == true && Rule(healthy, "beta").PendingDecision == true,
                    "two actual targets own simultaneous saved deferred intents");
                var twin = factory.Load(healthy.Save()); healthy.Update(); twin.Update();
                c.That(Rule(healthy, "alpha").HasApplied && Rule(healthy, "beta").HasApplied &&
                    Actual(healthy, "actuator") == Fix.FromInt(10) && Actual(healthy, "sensor") == Fix.FromInt(10),
                    "healthy original command owner applies both deferred physical values");
                c.Equal(healthy.Hash(), twin.Hash(), "two same-step actions preserve saved deterministic continuation");
                foreach (string mode in new[] { "caught-native", "propagated-native", "direct-throw" })
                {
                    string context = mode + "/loaded=" + loaded;
                    factory = ControlActionPolicyTests.Independent(); var game = Pending(factory);
                    if (loaded) game = factory.Load(game.Save());
                    var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                    var world = game.World; ulong before = Rules(game); long time = game.Tick;
                    var snapshot = game.Seat(0).Snapshot; int calls = 0;
                    var primary = new InvalidOperationException("proof.original-action-commit");
                    world.Commands.TryGet("proof.set-automation-level", out var entry);
                    ((SetAutomationProbeHandler)entry.Handler).AppliedProbe = (owner, command) => {
                        calls++;
                        if (mode == "direct-throw") throw primary;
                        try { owner.Spawn(owner.Content.Get<Beep.SimEngine.Content.EntityDef>("sensor"), new PlayerId(0), _ => throw primary); }
                        catch (InvalidOperationException) { if (mode != "caught-native") throw; }
                    };
                    game.Update();
                    c.Equal(1, calls, "later due handler never executes after first failed action: " + context);
                    c.Equal(mode == "direct-throw" ? AutomationModule.ModuleId : "entity.create", world.Fault?.Phase,
                        "original first fault remains owned by native operation, not a success wrapper: " + context);
                    c.Equal("InvalidOperationException: proof.original-action-commit", world.Fault?.Message, "original commit failure remains first cause: " + context);
                    c.Equal(Fix.FromInt(10), Actual(game, "actuator"), "first physical command leg really executed and is not falsely rolled back: " + context);
                    c.Equal(Fix.FromInt(6), Actual(game, "sensor"), "later due target is not changed after first fault: " + context);
                    ulong? after = null; Exception codecError = null;
                    try { after = Rules(game); } catch (Exception error) { codecError = error; }
                    c.That(codecError == null && after == before, "actual rule codec contains no applied/latch/cooldown success prefix: " + context);
                    c.Equal(time, game.Tick, "failed action cannot complete the pending game step: " + context);
                    c.Equal(snapshot.Tick, game.Seat(0).Snapshot.Tick, "faulted publication retains healthy facts: " + context);
                    c.That(!world.Events.HasPending, "failed commit has no queued success event: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "failed commit cannot save healthy continuation: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "failed commit cannot certify healthy state: " + context);
                    c.Equal(isolated, independent.Hash(), "separate saved pending actions remain untouched: " + context);
                    // Command handlers are world-owned; no shared hook removal is required for this session.
                    long otherTime = independent.Tick; independent.Update();
                    c.That(independent.Tick > otherTime && independent.Fault == null, "separate original command owner actually advances: " + context);
                }
                foreach (string mode in new[] { "silent", "throw", "clock-throw" })
                {
                    string context = "cooldown/" + mode + "/loaded=" + loaded;
                    factory = Factory(); var game = New(factory);
                    c.That(Configure(game, Settings(game, "mod.band")).Accepted && Rule(game).PendingDecision.HasValue,
                        "real independent rule has a deferred command before metadata admission: " + context);
                    if (loaded) game = factory.Load(game.Save());
                    var world = game.World; var target = Find(game, "actuator"); int version = target.Version;
                    ulong before = Rules(game); long time = game.Tick; int calls = 0;
                    var primary = new InvalidOperationException("proof.original-cooldown-callback");
                    var rule = world.Content.Get<BandControlRuleDef>("mod.band");
                    rule.CooldownMetadataProbe = () => {
                        calls++;
                        if (mode == "clock-throw") game.Clock.Pause();
                        if (mode == "silent") {
                            var probe = target.Get<AutomationProbe>(); int old = probe.FaultMode; probe.FaultMode = 5;
                            try { probe.ReadSignal(world, world.Content.Get<IAutomationSignalContent>("probe.level")); }
                            finally { probe.FaultMode = old; }
                        }
                        else throw primary;
                    };
                    game.Update(); rule.CooldownMetadataProbe = null;
                    c.Equal(1, calls, "cooldown metadata is admitted once before physical action: " + context);
                    c.Equal("observation.provider", world.Fault?.Phase, "pure metadata fault belongs to existing observer: " + context);
                    if (mode != "silent") c.Equal("InvalidOperationException: proof.original-cooldown-callback", world.Fault?.Message,
                        "original cooldown error is preserved: " + context);
                    c.Equal(version, target.Version, "metadata failure occurs before the real handler mutates its target: " + context);
                    ulong? after = null; try { after = Rules(game); } catch (Exception) { }
                    c.That(after == before, "metadata failure records no applied-rule prefix: " + context);
                    c.Equal(time, game.Tick, "invalid metadata cannot complete game time: " + context);
                    if (world.Fault != null) c.Throws<InvalidOperationException>(() => game.Save(), "invalid metadata cannot save healthy continuation: " + context);
                }
            }
        }
    }
}
