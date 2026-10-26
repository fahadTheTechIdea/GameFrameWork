using System;
using System.Collections.Generic;
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
    internal static class ThresholdSettingsTests
    {
        private static AutomationSettings With(AutomationSettings original, IReadOnlyDictionary<string, ControlThresholdBand> bands)
            => new AutomationSettings(original.Profile, original.Enabled, original.Target, original.Inputs, bands, original.WhenTrue, original.WhenFalse);
        private static AutomationSettings Band(AutomationSettings original, int on, int off, string key = "high", string unit = "unit.level")
            => With(original, new Dictionary<string, ControlThresholdBand> { [key] = new ControlThresholdBand(new Code(unit), Fix.FromInt(on), Fix.FromInt(off)) });
        internal static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); var settings = Settings(game);
            var profile = game.World.Content.Get<IControlRule>("high-level");
            c.That(profile.Thresholds["high"].Above && profile.Thresholds["high"].Input == "level", "authored stable numeric node exposes typed metadata through rule contract");
            c.Equal(Fix.FromInt(5), profile.Thresholds["high"].Initial.On, "initial settings come from explicit JSON, not invented default");
            c.Throws<NotSupportedException>(() => ((IDictionary<string, IControlThresholdDefinition>)profile.Thresholds).Clear(), "authored numeric metadata is immutable");
            c.Equal(0, game.World.Content.Get<IControlRule>("feedback").Thresholds.Count, "boolean/logical rules invent no numeric parameters");
            c.That(View(game).TryGetAutomationProfile("high-level", out var metadata), "host reads complete profile through public engine view without World access");
            var declarations = metadata.Thresholds;
            c.That(!ReferenceEquals(declarations["high"], profile.Thresholds["high"]) && declarations["high"].Initial.On == Fix.FromInt(5), "management metadata is detached, not a live provider handle");
            c.Throws<NotSupportedException>(() => ((IDictionary<string, ControlThresholdDefinition>)declarations).Clear(), "management threshold catalogue is immutable");
            c.That(!View(game).TryGetAutomationProfile("missing", out _) && !View(game).TryGetAutomationProfile(null, out _), "unknown metadata has explicit absence without substitution");
            c.That(!new Beep.SimEngine.Views.OperationsView(game.World, new PlayerId(9)).TryGetAutomationProfile("high-level", out _), "unseated metadata viewer cannot become observer");
            c.Equal(AutomationCodes.InvalidSettings, View(game).PreviewAutomation("field", With(settings, new Dictionary<string, ControlThresholdBand>())), "omitted bands are not filled from profile");
            c.Equal(AutomationCodes.InvalidSettings, View(game).PreviewAutomation("field", Band(settings, 5, 3, "other")), "unknown band identity refuses");
            c.Equal(AutomationCodes.InvalidSettings, View(game).PreviewAutomation("field", Band(settings, 2, 3)), "reversed above hysteresis refuses");
            c.Equal(AutomationCodes.UnitMismatch, View(game).PreviewAutomation("field", Band(settings, 5, 3, unit: "unit.count")), "threshold units cannot be inferred or converted");
            c.Equal(AutomationCodes.UnitMismatch, Configure(game, Band(settings, 5, 3, unit: "unit.count")).Refusal, "command and preview threshold refusal match");
            c.That(Rule(game) == null && Enabled(game), "refused configuration applies no rule or operation prefix");
            var mutable = new Dictionary<string, ControlThresholdBand> { ["high"] = new ControlThresholdBand(new Code("unit.level"), Fix.FromInt(9), Fix.FromInt(7)) };
            var explicitSettings = With(settings, mutable); mutable["high"] = settings.Thresholds["high"];
            c.Equal(Fix.FromInt(9), explicitSettings.Thresholds["high"].On, "settings copy mutable player input");
            c.Throws<NotSupportedException>(() => ((IDictionary<string, ControlThresholdBand>)explicitSettings.Thresholds).Clear(), "player settings expose immutable bands");
            ulong hash = game.Hash(); View(game).PreviewAutomation("field", explicitSettings);
            c.Equal(hash, game.Hash(), "threshold preview is pure");
            c.That(Configure(game, explicitSettings).Accepted, "ordinary configuration accepts complete explicit bands");
            var retained = Rule(game);
            c.That(!retained.State["high"] && retained.Candidate == false, "evaluation uses changed threshold, not authored initial on value");
            c.Equal(Fix.FromInt(9), retained.Settings.Thresholds["high"].On, "immutable view exposes actual selected threshold");
            var foreign = new List<AutomationRuleSnapshot>(); View(game, 1).CopyAutomationRules(foreign);
            c.Equal(0, foreign.Count, "private threshold settings do not leak to another company");
            game.Seat(0).Submit(new ConfigureAutomationCommand { Rule = "field", Settings = Band(settings, 4, 2) });
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "saved pending configure command owns exact threshold payload");
            game.Update(); twin.Update();
            c.Equal(game.Hash(), twin.Hash(), "threshold command replays once after restore");
            c.Equal(Fix.FromInt(9), retained.Settings.Thresholds["high"].On, "retained settings never follow a later edit");
            c.Equal(Fix.FromInt(4), Rule(twin).Settings.Thresholds["high"].On, "restored queued edit uses explicit value");

            var mod = Settings(game, "mod.band");
            c.That(game.World.Content.Get<IControlRule>("mod.band") is BandControlRuleDef, "independent public rule comes from master-listed JSON");
            c.Equal(AutomationCodes.InvalidSettings, View(game).PreviewAutomation("mod", Band(mod, 7, 5, "band")), "below-provider hysteresis direction is enforced by same validator");
            c.Equal(Code.None, View(game).PreviewAutomation("field", Band(settings, -2, -3)), "representable signed values have no arbitrary engine threshold ceiling");

            using (var memory = new MemoryStream())
            {
                var writer = new StateWriter(memory);
                AutomationSettings.Sync(writer, Band(settings, 2, 3)); writer.Flush(); var bytes = memory.ToArray();
                var decoded = AutomationSettings.Sync(new StateReader(bytes, 0, bytes.Length), null);
                c.Equal(AutomationCodes.InvalidSettings, ControlThresholds.Check(profile, decoded.Thresholds), "decoded bands still require rule-direction admission");
            }
            c.Throws<ArgumentNullException>(() => With(settings, null), "null bands never become initial defaults");
        }
        internal static void Execution(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            var settings = Settings(game); Configure(game, settings); game.RunTicks(2);
            c.That(Rule(game).PendingDecision == true && Enabled(game), "original band earns actual dwell then defers action");
            var shifted = Band(settings, 9, 7);
            c.That(Configure(game, shifted).Accepted, "player edits thresholds before old action's due phase");
            c.That(Enabled(game) && Rule(game).PendingDecision == null && Rule(game).Candidate == false,
                "edit cancels stale pending stop without applying it");
            long since = Rule(game).CandidateSince;
            SimGame twin = factory.Load(game.Save()); game.Update(); twin.Update();
            c.Equal(game.Hash(), twin.Hash(), "saved edited dwell continues identically");
            c.That(Rule(game).PendingDecision == null && Rule(game).CandidateSince == since, "fresh dwell cannot borrow old elapsed work");
            game.Update(); twin.Update(); c.That(Rule(game).PendingDecision == false, "new band earns its own full dwell before intent");
            game.Update(); twin.Update(); c.That(Enabled(game) && Rule(game).HasApplied && !Rule(game).Latch, "new intent applies only through normal next-step command");
            long applied = Rule(game).LastAppliedAt, cooldown = Rule(game).CooldownUntil;
            c.That(Configure(game, Band(settings, 5, 3)).Accepted, "player edits again during committed cooldown");
            c.That(Rule(game).LastAppliedAt == applied && Rule(game).CooldownUntil == cooldown && !Rule(game).Latch,
                "same-binding edits preserve actual applied history and cooldown");
            c.That(Rule(game).Candidate == true && Enabled(game), "new candidate never reverses actual control in same update");
            game.Clock.Pause(); ulong paused = game.Hash(); long now = game.Clock.StepIndex;
            c.Equal(0, game.Update(), "sole game clock pause freezes edited-rule dwell");
            c.Equal(paused, game.Hash(), "paused threshold evaluation changes no state");
            game.Clock.Resume();
            for (int attempt = 0; attempt < 16 && game.Clock.StepIndex <= cooldown + 1; attempt++) game.Update();
            c.That(game.Clock.StepIndex > now && !Enabled(game) && Rule(game).Latch, "cooldown and fresh dwell govern physical stop");
            twin = factory.Load(game.Save()); game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "committed edited latch continues through save/load");

            SimGame mod = New(factory); var independent = Settings(mod, "mod.band");
            c.That(Configure(mod, Band(independent, 7, 9, "band")).Accepted, "independent public evaluator accepts explicit below bands");
            c.That(Enabled(mod) && Rule(mod).PendingDecision == true, "independent rule cannot commit same-update actions");
            mod.Update(); c.That(!Enabled(mod) && Rule(mod).Latch, "independent numeric rule controls actual operation through shared owner");
            c.That(Configure(mod, Band(independent, 2, 4, "band")).Accepted, "below-band edit uses same ordinary settings command");
            mod.Update(); c.That(Enabled(mod) && !Rule(mod).Latch, "independent edited rule restarts without coordinator modifications");
            SimGame modTwin = factory.Load(mod.Save()); mod.Update(); modTwin.Update(); c.Equal(mod.Hash(), modTwin.Hash(), "public rule's configured thresholds persist deterministically");
            NativeProcess(c);
        }
        private static void NativeProcess(Check c)
        {
            GameFactory factory = Factory(processEdit: root => root.Get("transformations")[0].Set("duration", Json.Parse("{\"numerator\":20,\"denominator\":1,\"unit\":\"minute\"}")));
            SimGame game = New(factory); Entity assembler = Find(game, "assembler");
            var settings = Settings(game, target: "assembler");
            c.That(Configure(game, Band(settings, 9, 7)).Accepted, "edited numeric profile binds native process through ordinary controls");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = assembler.Id, Transformation = "build" }).Accepted,
                "player queues timed material transformation");
            game.RunTicks(2); var process = assembler.Get<Transformer>();
            c.That(process.Progress.WholeTicks > 0 && Enabled(game, "assembler"), "shifted threshold lets actual native work progress");
            c.That(Configure(game, settings).Accepted, "lowered thresholds start fresh stop dwell on live process");
            game.RunTicks(4); WorkProgress earned = process.Progress;
            ResourceId supply = game.World.Content.ResourceId("supply"); Fix stock = assembler.Get<IInventory>().Amount(supply);
            c.That(!Enabled(game, "assembler") && process.Queue.Count > 0, "changed band stops actual incomplete process after dwell and deferred action");
            game.RunTicks(3);
            c.Equal(earned, process.Progress, "threshold-driven shutdown preserves actual earned progress");
            c.Equal(stock, assembler.Get<IInventory>().Amount(supply), "threshold-driven pause consumes no extra material");
            c.That(Configure(game, Band(settings, 9, 7)).Accepted, "higher band requests restart without host gameplay");
            SimGame twin = factory.Load(game.Save());
            for (int step = 0; step < 32; step++) { game.Update(); twin.Update(); }
            c.That(process.Queue.Count == 0 && assembler.Get<IInventory>().Amount(supply) == Fix.FromInt(18), "edited threshold restarts real workflow with exact-once material consumption");
            c.Equal(game.Hash(), twin.Hash(), "saved edited native process continues identically");
        }
    }
}
