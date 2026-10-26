using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.AutomationTests;

namespace Beep.SimEngine.Tests
{
    internal static class AutomationProfileTests
    {
        internal static void Contracts(Check c)
        {
            var factory = Factory(); var game = New(factory); var view = View(game); ulong hash = game.Hash();
            var profiles = new List<AutomationProfileSnapshot>(); view.CopyAutomationProfiles(profiles);
            c.Equal(5, profiles.Count, "all native and public-only authored rule profiles are discoverable");
            string previous = null;
            foreach (var profile in profiles)
            {
                c.That(previous == null || string.CompareOrdinal(previous, profile.Id) < 0, "profile discovery has stable ID order"); previous = profile.Id;
                c.That(view.TryGetAutomationProfile(profile.Id, out var single) && single.Id == profile.Id, "discovery and lookup use same profile");
            }
            c.That(view.TryGetAutomationProfile("high-level", out var metadata), "numeric profile lookup succeeds");
            c.Equal("proof.automation-rules.suspend", metadata.InputRecovery, "recovery identity comes from actual JSON policy");
            c.That(!metadata.InitialLatch && !metadata.InitialState["high"], "authored initial latch and named state exposed as detached values");
            c.Equal(GameDuration.Minutes(2), metadata.Dwell, "authored dwell is a game duration, not another timer");
            c.Equal(GameDuration.Minutes(4), metadata.Cooldown, "authored cooldown is exact game duration");
            c.That(!metadata.WhenTrue.Enabled && metadata.WhenFalse.Enabled && metadata.WhenTrue.Target == Fix.One,
                "true and false actions come from actual profile rather than host assumptions");
            c.Equal(new Code("unit.work-rate"), metadata.WhenTrue.Unit, "action declares target unit");
            c.Equal(ControlRuleArbitration.Exclusive, metadata.Arbitration, "arbitration declaration preserved");
            var input = metadata.Inputs[0];
            c.That(input.Id == "level" && input.Signal == "probe.level" && input.Metric == new Code("probe.level"), "input alias and signal binding identities are explicit");
            c.That(input.Kind == AutomationSignalKind.Number && input.Unit == new Code("unit.level"), "typed signal facts are copied");
            c.That(input.Providers.Count == 1 && input.Providers[0].Component == "automation_probe" && input.Providers[0].SchemaVersion == 1,
                "provider identity/version values support selection without codec handles");
            c.Throws<NotSupportedException>(() => ((IList<AutomationInputSnapshot>)metadata.Inputs).Clear(), "input metadata immutable");
            c.Throws<NotSupportedException>(() => ((IList<AutomationProviderSnapshot>)input.Providers).Clear(), "provider metadata immutable");
            c.Throws<NotSupportedException>(() => ((IDictionary<string, bool>)metadata.InitialState).Clear(), "initial named state immutable");
            c.Throws<NotSupportedException>(() => ((IDictionary<string, ControlThresholdDefinition>)metadata.Thresholds).Clear(), "profile threshold declarations immutable");
            c.Throws<ArgumentNullException>(() => view.CopyAutomationProfiles(null), "null discovery destination refuses explicitly");
            view.CopyAutomationProfiles(profiles); c.Equal(5, profiles.Count, "discovery clears prior caller entries");
            view.TryGetAutomationProfile("feedback", out var boolean);
            c.That(boolean.Thresholds.Count == 0 && boolean.Inputs[0].Kind == AutomationSignalKind.Boolean, "boolean profile invents no numeric band");
            view.TryGetAutomationProfile("mod.band", out var independent);
            c.That(!independent.Thresholds["band"].Above && independent.Thresholds["band"].Initial.On == Fix.FromInt(3), "public-only profile needs no concrete native-rule lookup");
            c.Equal(hash, game.Hash(), "complete metadata discovery/lookup is pure");
            var invalid = new OperationsView(game.World, new PlayerId(9)); invalid.CopyAutomationProfiles(profiles);
            c.Equal(0, profiles.Count, "unseated discovery clears output and exposes no observer metadata");
            c.That(!invalid.TryGetAutomationProfile("high-level", out var absent) && absent == null, "unseated lookup returns explicit absence");
            var negative = new OperationsView(game.World, new PlayerId(-2)); negative.CopyAutomationProfiles(profiles);
            c.Equal(0, profiles.Count, "invalid negative viewer is not observer");
            c.That(!view.TryGetAutomationProfile("missing", out absent) && absent == null && !view.TryGetAutomationProfile(null, out _), "unknown/null lookup never substitutes profile");
            c.That(Configure(game, Settings(game)).Accepted, "ordinary settings remain actual control owner");
            game.Update();
            c.That(!metadata.InitialState["high"] && metadata.Thresholds["high"].Initial.On == Fix.FromInt(5), "retained authored metadata does not follow live rule state");
            var twin = factory.Load(game.Save());
            c.That(View(twin).TryGetAutomationProfile("high-level", out var restored) && !ReferenceEquals(restored, metadata)
                && !ReferenceEquals(restored.Inputs[0], input), "loaded session returns detached profile objects");
            c.Equal(game.Hash(), twin.Hash(), "metadata read changes neither saved session");
            SignalOnly(c);
            Visibility(c);
        }

        private static void SignalOnly(Check c)
        {
            var game = InventorySignalTests.New(InventorySignalTests.Factory());
            var seat = game.Seat(0); var view = seat.View.Feature<OperationsView>();
            var entities = new List<EntityView>(); seat.View.CopyEntities(entities);
            var id = entities.Find(x => x.Def == "stock.assembler" && x.Owner == seat.Id).Id;
            var operations = new List<Code>(); var signals = new List<Code>(); ulong hash = game.Hash();
            c.That(view.CopyOperationIds(id, operations) && !operations.Contains(new Code("feed")), "native inventory signal has no operation membership");
            c.That(view.CopySignalSourceIds(id, signals) && signals.Contains(new Code("feed")), "signal-only native capability discoverable through engine view");
            c.That(view.TryGetSignalSchema(id, new Code("feed"), out var schema) && schema.Version == 3, "discovered native source retains actual signal schema");
            view.CopySignalSourceIds(id, signals); c.Equal(2, signals.Count, "repeat signal discovery clears old results");
            c.That(string.CompareOrdinal(signals[0].Id, signals[1].Id) < 0, "source IDs have stable order independent of component authoring order");
            c.Equal(hash, game.Hash(), "native signal-only discovery changes no stock/promises or control state");
            c.That(!View(game, 1).CopySignalSourceIds(id, signals) && signals.Count == 0, "foreign company gets no signal capability IDs");
            c.That(!new OperationsView(game.World, new PlayerId(-2)).CopySignalSourceIds(id, signals) && signals.Count == 0, "negative invalid viewer cannot inspect signal-only capabilities");
            c.That(!new OperationsView(game.World, new PlayerId(9)).CopySignalSourceIds(id, signals), "unseated source viewer gets no capabilities");
            c.That(!view.CopySignalSourceIds(new EntityId(int.MaxValue), signals) && signals.Count == 0, "removed/unknown source clears caller output");
            c.Throws<ArgumentNullException>(() => view.CopySignalSourceIds(id, null), "null source destination explicitly refuses");
        }

        private static void Visibility(Check c)
        {
            foreach (string hidden in new[] { "high-level", "probe.level", "proof.automation-rules.suspend", "proof.automation-rules.operation" })
            {
                var factory = Factory(hiddenProfileDependency: hidden); var game = New(factory); var view = View(game);
                var profiles = new List<AutomationProfileSnapshot>(); ulong hash = game.Hash();
                view.CopyAutomationProfiles(profiles);
                c.That(!profiles.Exists(x => x.Id == "high-level") && !view.TryGetAutomationProfile("high-level", out var absent) && absent == null,
                    "actual knowledge visibility withholds whole profile for hidden dependency: " + hidden);
                c.That(game.Observer.Feature<OperationsView>().TryGetAutomationProfile("high-level", out _), "explicit observer keeps authored metadata: " + hidden);
                var settings = Settings(game);
                c.Equal(Codes.UnknownDef, view.PreviewAutomation("field", settings), "configuration shares complete visibility admission: " + hidden);
                c.Equal(hash, game.Hash(), "hidden reads/admission allocate no state: " + hidden);
                c.Equal(Codes.UnknownDef, Configure(game, settings).Refusal, "ordinary command refuses hidden profile dependency: " + hidden);
                c.That(Rule(game) == null && Enabled(game), "hidden refusal applies no rule/control prefix: " + hidden);
                // Instrument an actual existing knowledge owner; this is admission proof, not physical discovery.
                var fact = game.World.Content.Get<KnowledgeFactDef>("automation.profile.observed");
                c.Equal(Code.None, game.World.Services.Get<ICompanyKnowledge>().Record(new PlayerId(0), "sim.knowledge.facts", fact.Id, fact.Value),
                    "fixture records actual authored company observation: " + hidden);
                c.That(view.TryGetAutomationProfile("high-level", out _) && view.PreviewAutomation("field", settings).IsNone,
                    "same view sees newly learned dependency through existing knowledge owner: " + hidden);
                c.That(!View(game, 1).TryGetAutomationProfile("high-level", out _), "another company gains no knowledge: " + hidden);
                var twin = factory.Load(game.Save());
                c.That(View(twin).TryGetAutomationProfile("high-level", out _) && !View(twin, 1).TryGetAutomationProfile("high-level", out _),
                    "saved company knowledge preserves scoped metadata: " + hidden);
                c.Equal(game.Hash(), twin.Hash(), "visibility reads leave saved twins identical: " + hidden);
            }
        }

        internal static void PlayerExecution(Check c)
        {
            // This workflow has no World, catalog, entity/component or evaluator access.
            var factory = Factory(); var game = New(factory); var seat = game.Seat(0);
            var operations = seat.View.Feature<OperationsView>(); var profiles = new List<AutomationProfileSnapshot>();
            operations.CopyAutomationProfiles(profiles); var profile = profiles.Find(x => x.Id == "mod.band");
            c.That(profile != null, "player discovers independent JSON rule through public engine view");
            var entities = new List<EntityView>(); seat.View.CopyEntities(entities);
            EntityId source = entities.Find(x => x.Owner == seat.Id && x.Def == "sensor").Id;
            EntityId target = entities.Find(x => x.Owner == seat.Id && x.Def == "actuator").Id;
            var ids = new List<Code>(); operations.CopySignalSourceIds(source, ids); var sourceCapability = ids[0];
            operations.CopyOperationIds(target, ids); var targetCapability = ids[0];
            c.That(operations.TryGetSignalSchema(source, sourceCapability, out var schema)
                && schema.StateCodec.Id == profile.Inputs[0].Providers[0].Component && schema.Version == profile.Inputs[0].Providers[0].SchemaVersion,
                "player selects live declared signal provider from engine facts");
            var bands = new Dictionary<string, ControlThresholdBand>();
            foreach (var threshold in profile.Thresholds) bands.Add(threshold.Key, threshold.Value.Initial);
            // Explicit player choice: select a below-band that stops the actual level-six operation.
            bands["band"] = new ControlThresholdBand(profile.Thresholds["band"].Initial.Unit, Fix.FromInt(7), Fix.FromInt(9));
            var settings = new AutomationSettings(profile.Id, true, new ControlBinding(target, targetCapability),
                new Dictionary<string, ControlBinding> { [profile.Inputs[0].Id] = new ControlBinding(source, sourceCapability) }, bands, profile.WhenTrue, profile.WhenFalse);
            c.Equal(Code.None, operations.PreviewAutomation("player-field", settings), "engine previews discovered profile and explicit player choices");
            seat.Submit(new ConfigureAutomationCommand { Rule = "player-field", Settings = settings });
            var twin = factory.Load(game.Save()); game.Update(); twin.Update();
            c.That(operations.TryGetAutomationRule(seat.Id, "player-field", out var rule) && rule.PendingDecision == true,
                "engine alone evaluates player-bound independent rule");
            c.That(operations.TryGetControl(target, targetCapability, out var control) && control.Enabled, "configuration cannot commit a same-update action");
            game.Update(); twin.Update();
            c.That(operations.TryGetControl(target, targetCapability, out control) && !control.Enabled, "normal next-update engine controls perform player request");
            c.Equal(game.Hash(), twin.Hash(), "view-only setup and queued command continue identically after save/load");
            c.That(profile.Thresholds["band"].Initial.On == Fix.FromInt(3) && !profile.InitialState["band"], "retained independent authored metadata stays unchanged");
        }
    }
}
