using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.PlanetXProductionTests;

namespace Beep.SimEngine.Tests
{
    // Authored input variants, normal commands/views/clock/saves and assertions only.
    internal static class ProgressionBindingTests
    {
        internal const string Case = "Progression startup: all declared bindings bounds and references fail before initialization including unused content";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame Session(GameFactory factory) => New(factory, "progression-declarations");
        private static List<TechnologySnapshot> Technologies(SimGame game)
        { var facts = new List<TechnologySnapshot>(); game.Seat(0).View.Feature<TechnologyView>().CopyTechnologies(facts); return facts; }
        private static List<ObjectiveSnapshot> Objectives(SimGame game)
        { var facts = new List<ObjectiveSnapshot>(); game.Seat(0).View.Feature<ObjectiveView>().CopyObjectives(facts); return facts; }
        private static List<OrderView> Orders(SimGame game)
        { var facts = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(facts); return facts; }
        private static GameFactory Factory(InitializeProbe init, ProgressionBindingModule probe, Action<JsonValue> edit = null, Action<JsonValue> bindings = null)
            => ProgressionDeclarationTests.Factory(init, out _, edit, bindingProbe: probe, editBindings: bindings);
        private static void Enrollment(JsonValue authored, string enrollment, string family)
        {
            JsonValue settings = authored.Get("scenarios")[0].Get("settings").Get("sim.objectives");
            if (enrollment == "empty") settings.Set("companies", JsonValue.NewArray());
            if (enrollment != "selected") return;
            JsonValue objective = authored.Get("independent_objectives")[0];
            objective.Set(family == "predicate" ? "predicate" : "on_complete", JsonValue.Of("binding.unused-" + family));
        }
        private static void Run(Check c)
        {
            foreach (string section in new[] { "technologies", "independent_technologies" })
                foreach (string invalid in new[] { "missing-prerequisite", "cycle", "missing-effect", "wrong-effect" })
                {
                    var init = new InitializeProbe();
                    c.Throws<ContentException>(() => Session(Factory(init, new ProgressionBindingModule(), x => {
                        var definition = x.Get(section)[0];
                        if (invalid == "missing-prerequisite") definition.Set("prerequisites", Json.Parse("[\"missing\"]"));
                        if (invalid == "cycle") definition.Set("prerequisites", Json.Parse("[\"decl.native\"]"));
                        if (invalid == "missing-effect") definition.Set("on_complete", JsonValue.Of("missing"));
                        if (invalid == "wrong-effect") definition.Set("on_complete", JsonValue.Of("binding.unused-effect"));
                    })), section + "/" + invalid + " refuses");
                    c.Equal(0, init.Initializations, "invalid research never initializes");
                }
            foreach (string section in new[] { "objectives", "independent_objectives" })
                foreach (string enrollment in new[] { "enrolled", "empty" })
                    foreach (string invalid in new[] { "missing-predicate", "wrong-predicate", "missing-prerequisite", "cycle", "missing-completion", "missing-failure" })
                    {
                        var init = new InitializeProbe();
                        c.Throws<ContentException>(() => Session(Factory(init, new ProgressionBindingModule(), x => {
                            Enrollment(x, enrollment, "predicate"); var definition = x.Get(section)[0];
                            if (invalid == "missing-predicate") definition.Set("predicate", JsonValue.Of("missing"));
                            if (invalid == "wrong-predicate") definition.Set("predicate", JsonValue.Of("decl.native"));
                            if (invalid == "missing-prerequisite") definition.Set("prerequisites", Json.Parse("[\"missing\"]"));
                            if (invalid == "cycle") definition.Set("prerequisites", Json.Parse("[\"decl.native-earned\"]"));
                            if (invalid == "missing-completion") definition.Set("on_complete", JsonValue.Of("missing"));
                            if (invalid == "missing-failure") definition.Set("on_failure", JsonValue.Of("missing"));
                        })), section + "/" + enrollment + "/" + invalid + " refuses");
                        c.Equal(0, init.Initializations, "invalid objective never initializes");
                    }
            foreach (string invalid in new[] { "missing", "cycle" })
            {
                var init = new InitializeProbe();
                c.Throws<ContentException>(() => Session(Factory(init, new ProgressionBindingModule(), x =>
                    x.Set("logical_objective_predicates", Json.Parse("[{\"id\":\"binding.dag\",\"kind\":\"all\",\"predicates\":[\"" + (invalid == "missing" ? "missing" : "binding.dag") + "\"]}]")))), "unused predicate DAG rejects " + invalid);
                c.Equal(0, init.Initializations, "invalid predicate DAG never initializes");
            }
            foreach (string amount in new[] { "0", "-1", "0.5" })
            {
                var init = new InitializeProbe();
                c.Throws<ContentException>(() => Session(Factory(init, new ProgressionBindingModule(), x =>
                    x.Set("inventory_effects", Json.Parse("[{\"id\":\"binding.disposition\",\"kind\":\"consumed\",\"ports\":{\"space.blank\":\"disposition.input\"},\"quantities\":{\"space.blank\":" + amount + "}}]")))), "unused native physical effect rejects invalid amount/quantum " + amount);
                c.Equal(0, init.Initializations, "invalid native effect never initializes");
            }
            foreach (string family in new[] { "predicate", "effect" })
                foreach (string enrollment in new[] { "unused", "selected", "empty" })
                    foreach (string invalid in new[] { "negative", "above-maximum", "inverted", "missing-bound", "provider", "throw", "clock" })
                    {
                        var init = new InitializeProbe(); var probe = new ProgressionBindingModule();
                        Action create = () => Session(Factory(init, probe, x => Enrollment(x, enrollment, family), x => {
                            var definition = x.Get("progression_binding_" + (family == "predicate" ? "predicates" : "effects"))[0];
                            if (invalid == "negative") definition.Set("minimum", JsonValue.Number("-1"));
                            if (invalid == "above-maximum") definition.Set("maximum", JsonValue.Number("2"));
                            if (invalid == "inverted") { definition.Set("minimum", JsonValue.Number("1")); definition.Set("maximum", JsonValue.Number("0")); }
                            if (invalid == "missing-bound") definition.Remove("maximum");
                            if (invalid == "provider" || invalid == "throw" || invalid == "clock") definition.Set("mode", JsonValue.Of(invalid));
                        }));
                        if (invalid == "throw" || invalid == "clock") c.Throws<InvalidOperationException>(create, family + "/" + enrollment + "/" + invalid + " binding fails through original observation boundary");
                        else c.Throws<ContentException>(create, family + "/" + enrollment + "/" + invalid + " binding refuses");
                        c.Equal(0, init.Initializations, "invalid binding never initializes");
                        if (invalid != "missing-bound") c.Equal(1, family == "predicate" ? probe.PredicateValidations : probe.EffectValidations, "actual binding validator runs once even without enrollment");
                    }
            var emptyInit = new InitializeProbe(); var emptyProbe = new ProgressionBindingModule();
            var emptyFactory = Factory(emptyInit, emptyProbe, x => Enrollment(x, "empty", "predicate")); var empty = Session(emptyFactory);
            c.That(emptyInit.Initializations == 1 && emptyProbe.PredicateValidations == 1 && emptyProbe.EffectValidations == 1, "all valid unused bindings validate before empty-enrollment initialization");
            c.Equal(0, Objectives(empty).Count, "validation does not create objective enrollment");
            Step(empty); c.That(empty.Fault == null, "validation never observes or executes unused probe content");

            var initialize = new InitializeProbe(); var positive = new ProgressionBindingModule(); var factory = Factory(initialize, positive); var game = Session(factory);
            c.That(game.Modules.Count == 14 && initialize.Initializations == 1 && positive.PredicateValidations == 1 && positive.EffectValidations == 1, "explicit master binds actual validators once before Initialize");
            c.That(Technologies(game).All(x => !x.Known), "startup validation grants no research");
            c.That(Objectives(game).All(x => x.State == ObjectiveState.Locked), "startup validation executes no objective");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var replay = game.RecordReplay(limits);
            var target = Actor(game, "receiver"); var command = new PostOrderCommand { Plan = "decl.external", Target = target, Priority = 70 };
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal research preview admits validated content");
            c.Equal(before, game.Hash(), "validated preview is pure"); game.Seat(0).Submit(command); var twin = factory.Load(game.Save());
            ProgressionDisclosureTests.Until(game, twin, () => Orders(game).Any(x => x.CurrentProgress.WholeTicks > 2), "validated physical research partial");
            c.That(!Technologies(game).Single(x => x.Technology == "decl.external").Known, "partial physically supplied research is not granted");
            c.Equal(Fix.One, Stock(game, "receiver", "crucible"), "original held tool is physically supplied");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial research save restores original authoritative state");
            ProgressionDisclosureTests.Until(game, twin, () => Technologies(game).Single(x => x.Technology == "decl.external").Known, "validated independent research completion");
            c.That(Objectives(game).Single(x => x.Objective == "decl.external-earned").State == ObjectiveState.Completed, "original objective completes without probe execution");
            command = new PostOrderCommand { Plan = "decl.native", Target = target, Priority = 70 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "actual prerequisite admits native research");
            game.Seat(0).Submit(command); twin = factory.Load(game.Save());
            ProgressionDisclosureTests.Until(game, twin, () => Technologies(game).Single(x => x.Technology == "decl.native").Known, "validated native research completion");
            c.That(Objectives(game).All(x => x.State == ObjectiveState.Completed), "original objective dependency chain settles");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal progression restores");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "validated composition and normal physical command stream replay");
            var isolated = Session(factory); Step(isolated); Step(game, twin);
            c.That(Technologies(isolated).All(x => !x.Known) && Objectives(isolated).All(x => x.State != ObjectiveState.Completed), "interleaved session has no earned state leakage");
            c.That(game.Fault == null && twin.Fault == null && isolated.Fault == null, "unused probe observation/effect callbacks never execute");
        }
    }
}
