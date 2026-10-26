using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class AutomationTests
    {
        public static void Register()
        {
            TestRunner.Add("Automation: threshold and recovery contracts, bindings and private controls", Contracts);
            TestRunner.Add("Automation: threshold edits, recovery, deferred timing and saved processes", Processes);
            TestRunner.Add(ControlActionPolicyTests.ParticipantCase, ControlActionPolicyTests.Participants);
            TestRunner.Add("Control actions: independent normal commands deferred timing and saved continuation", ControlActionPolicyTests.Execution);
            TestRunner.Add("Input recovery: actual decisions exclude pure validation and retain saved timed continuation", InputRecoveryTests.Execution);
            TestRunner.Add("Input recovery: resource-only physical network wait withdrawal and saved valve restart", InputRecoveryTests.PhysicalNetwork);
            TestRunner.Add("Input recovery providers: bound saved state and original failures remain inside native observation", InputRecoveryTests.ProviderObservation);
            TestRunner.Add(RuleProviderObservationTests.Case, RuleProviderObservationTests.Run);
            TestRunner.Add(RuleProviderObservationTests.DwellCase, RuleProviderObservationTests.Dwell);
            TestRunner.Add(RuleProviderObservationTests.ConfigurationCase, RuleProviderObservationTests.Configuration);
            TestRunner.Add(DeferredActionCommitTests.Case, DeferredActionCommitTests.Run);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        internal static GameFactory Factory(Action<JsonValue> ruleEdit = null, Action<JsonValue> processEdit = null,
            string hiddenProfileDependency = null)
        {
            var modules = new List<GameModule> { new InventoryModule(), new OperationsModule(),
                new ContentAccessModule(), new TransformationModule(), new WorldSetupModule(), new AutomationModule(), new ProcessProofModule(), new AutomationProofModule() };
            if (hiddenProfileDependency != null) { modules.Add(new CompanyKnowledgeModule()); modules.Add(new AutomationKnowledgeVisibilityModule()); }
            var definition = new GameDefinition("automation", modules);
            JsonValue process = Json.Parse(FrameworkProofContent.Read("process.json"));
            process.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            process.Get("scenarios")[0].Set("default_seats", JsonValue.Number("2"));
            process.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.automation"));
            process.Get("scenarios")[0].Get("settings").Get(WorldSetupModule.ModuleId).Set("profile", JsonValue.Of("automation.start"));
            processEdit?.Invoke(process);
            JsonValue manifest = Json.Parse(FrameworkProofContent.Read("process-manifest.json"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"automation-rules.json\",\"input-recovery.json\"]}"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"proof.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.automation\",\"version\":\"1.0.0\"}],\"documents\":[\"automation-proof.json\"]}"));
            if (hiddenProfileDependency != null)
            {
                manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\",\"requires\":[],\"documents\":[\"automation-profile-knowledge.json\"]}"));
                manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"automation-profile-visibility.json\"]}"));
                process.Get("scenarios")[0].Get("settings").Set("sim.knowledge-visibility", Json.Parse("{\"policy\":\"automation.profile.visibility\"}"));
                process.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(KnowledgeVisibilityModule.ModuleId));
            }
            JsonValue rules = Json.Parse(FrameworkProofContent.Read("automation-rules.json")); ruleEdit?.Invoke(rules);
            var files = new PreloadedFileSource().Add("manifest.json", Text(manifest)).Add("framework/access.json", FrameworkProofContent.Access)
                .Add("input-recovery.json", FrameworkProofContent.Read("input-recovery.json"))
                .Add("process.json", Text(process)).Add("automation-rules.json", Text(rules))
                .Add("automation-proof.json", FrameworkProofContent.Read("automation-proof.json"));
            if (hiddenProfileDependency != null)
            {
                JsonValue visibility = Json.Parse(FrameworkProofContent.Read("automation-profile-visibility.json"));
                string selected = hiddenProfileDependency == "high-level" ? "rule" : hiddenProfileDependency == "probe.level" ? "signal"
                    : hiddenProfileDependency == "proof.automation-rules.suspend" ? "recovery" : "action";
                var visibilityRules = JsonValue.NewArray(); visibilityRules.Add(JsonValue.Of("automation.profile.visibility." + selected));
                visibility.Get("knowledge_visibility_policies")[0].Set("rules", visibilityRules);
                files.Add("automation-profile-knowledge.json", FrameworkProofContent.Read("automation-profile-knowledge.json"));
                files.Add("automation-profile-visibility.json", Text(visibility));
            }
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "process", Seed = 197 });
        internal static Entity Find(SimGame game, string prototype, int company = 0)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype && entity.Owner == new PlayerId(company)) return entity; return null; }
        internal static OperationsView View(SimGame game, int company = 0) => game.Seat(company).View.Feature<OperationsView>();
        internal static ControlBinding Binding(Entity entity, string capability = "probe") => new ControlBinding(entity.Id, new Code(capability));
        internal static AutomationSettings Settings(SimGame game, string profile = "high-level", bool enabled = true,
            string target = "actuator", int company = 0)
        {
            string input = profile == "feedback" ? "enabled" : profile == "mod.inverse" ? "sample" : profile == "backlog" ? "jobs" : "level";
            return new AutomationSettings(profile, enabled, Binding(Find(game, target, company), target == "assembler" ? "transformation" : "probe"),
                new Dictionary<string, ControlBinding> { [input] = Binding(Find(game, profile == "backlog" ? "assembler" : "sensor", company), profile == "backlog" ? "transformation" : "probe") },
                ControlThresholds.Initial(game.World.Content.Get<IControlRule>(profile)),
                game.World.Content.Get<IControlRule>(profile).WhenTrue, game.World.Content.Get<IControlRule>(profile).WhenFalse);
        }
        internal static AutomationRuleSnapshot Rule(SimGame game, string id = "field", int company = 0)
        { return View(game, company).TryGetAutomationRule(new PlayerId(company), id, out AutomationRuleSnapshot result) ? result : null; }
        internal static CommandResult Configure(SimGame game, AutomationSettings settings, string id = "field", int company = 0)
            => TestKit.Order(game, company, new ConfigureAutomationCommand { Rule = id, Settings = settings });
        internal static bool Enabled(SimGame game, string target = "actuator", int company = 0)
        {
            Entity entity = Find(game, target, company);
            return View(game, company).TryGetControl(entity.Id, new Code(target == "assembler" ? "transformation" : "probe"), out var control) && control.Enabled;
        }
        internal static void Level(Check check, SimGame game, int level, bool available = true)
            => check.That(TestKit.Order(game, 0, new SetAutomationProbeCommand { Entity = Find(game, "sensor").Id, Level = Fix.FromInt(level), Available = available }).Accepted,
                "engine-side provider accepts an ordinary deterministic environment request");

        private static void Contracts(Check check)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            check.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "new framework behavior has current build identity");
            check.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "new rule owner uses a breaking self-described checkpoint");
            check.That(game.World.Space == null, "automation does not require a tile or other map");
            check.That(game.World.Services.Get<IAutomationRules>() is GameSystem, "service alias refers to the sole system codec owner");
            var profiles = game.World.Content.All<IControlRule>();
            check.That(profiles.Count == 5 && game.World.Content.Find<IControlRule>("mod.inverse") is InverseControlRuleDef,
                "master-listed native and independent JSON rule definitions share the existing base catalog");
            var profile = game.World.Content.Get<IControlRule>("high-level");
            check.That(ReferenceEquals(profile.Inputs[0].Signal, game.World.Content.Get<AutomationSignalDef>("probe.level")), "rule inputs alias the existing typed signal definition");
            AutomationSettings settings = Settings(game); ulong hash = game.Hash();
            check.Equal(Code.None, View(game).PreviewAutomation("field", settings), "public engine preview accepts exact current bindings");
            check.Equal(hash, game.Hash(), "rule preview leaves authoritative state unchanged");
            check.That(Configure(game, settings).Accepted, "ordinary command registers policy, not immediate control");
            check.That(Enabled(game), "registering a rule does not apply its new decision in the same step");
            AutomationRuleSnapshot retained = Rule(game);
            check.That(retained.EffectiveEnabled && retained.Candidate == true && retained.CandidateSince == 0,
                "private immutable facts expose the actual authored dwell candidate");
            var own = new List<AutomationRuleSnapshot>(); var foreign = new List<AutomationRuleSnapshot>();
            View(game).CopyAutomationRules(own); View(game, 1).CopyAutomationRules(foreign);
            check.That(own.Count == 1 && foreign.Count == 0 && !View(game, 1).TryGetAutomationRule(new PlayerId(0), "field", out _), "rule settings and diagnostics remain company-private");
            game.Observer.Feature<OperationsView>().CopyAutomationRules(own);
            check.Equal(1, own.Count, "explicit observer sees detached rule facts");
            check.Equal(Codes.NotSeated, game.Observer.Feature<OperationsView>().PreviewAutomation("observer", settings), "observer cannot fabricate a policy actor");
            hash = game.Hash(); Rule(game); View(game).CopyAutomationRules(own);
            check.Equal(hash, game.Hash(), "diagnostics never read signal providers or evaluate rules");
            check.Equal(AutomationCodes.Conflict, View(game).PreviewAutomation("second", settings), "exclusive arbitration refuses a second enabled target owner");
            check.Equal(AutomationCodes.Conflict, Configure(game, settings, "second").Refusal, "command and preview conflict refusal match");
            check.That(Configure(game, Settings(game, enabled: false), "second").Accepted, "explicit disabled policy does not claim an active target");
            check.Equal(AutomationCodes.Conflict, View(game).PreviewAutomation("second", settings), "reenabling repeats the same conflict admission");
            var unknown = new AutomationSettings("missing", true, settings.Target, settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
            check.Equal(Codes.UnknownDef, View(game).PreviewAutomation("missing", unknown), "unknown JSON profile is not substituted");
            check.Equal(AutomationCodes.InvalidSettings, View(game).PreviewAutomation("bad id", settings), "local rule IDs are canonical contracts");
            check.Equal(AutomationCodes.InvalidSettings, View(game).PreviewAutomation("incomplete", new AutomationSettings("high-level", true, settings.Target, new Dictionary<string, ControlBinding>(), settings.Thresholds, settings.WhenTrue, settings.WhenFalse)), "missing declared input is refused");
            var badSource = new AutomationSettings("high-level", true, settings.Target, new Dictionary<string, ControlBinding> { ["level"] = Binding(Find(game, "sensor"), "missing") }, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
            check.Equal(AutomationCodes.NotSignalSource, View(game).PreviewAutomation("source", badSource), "named sources never fall back to the first part");
            var foreignSource = new AutomationSettings("high-level", true, settings.Target, new Dictionary<string, ControlBinding> { ["level"] = Binding(Find(game, "sensor", 1)) }, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
            check.Equal(Codes.NotOwned, View(game).PreviewAutomation("private", foreignSource), "foreign private signal binding refuses before reading");
            var wrongSchema = new AutomationSettings("high-level", true, settings.Target, new Dictionary<string, ControlBinding> { ["level"] = Binding(Find(game, "assembler"), "transformation") }, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
            check.Equal(OperationCodes.SignalUnavailable, View(game).PreviewAutomation("schema", wrongSchema), "incompatible signal schema is unavailable, not a zero reading");
            var foreignTarget = new AutomationSettings("high-level", true, Binding(Find(game, "actuator", 1)), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
            check.Equal(Codes.NotOwned, View(game).PreviewAutomation("foreign", foreignTarget), "operation command ownership remains authoritative");
            check.That(Configure(game, Settings(game, company: 1), company: 1).Accepted, "the same local stable rule ID may belong to another company");
            check.Equal(0L, retained.CandidateSince, "retained snapshot does not follow later live updates");
            check.Throws<NotSupportedException>(() => ((IDictionary<string, bool>)retained.State)["high"] = false, "snapshots expose no mutable rule state");
            check.Throws<NotSupportedException>(() => ((IDictionary<string, ControlBinding>)settings.Inputs)["level"] = settings.Target, "settings input bindings are immutable");
            SimGame saved = factory.Load(game.Save()); check.Equal(game.Hash(), saved.Hash(), "both private policies save through one actual codec");
            SimGame independent = New(factory); View(independent).CopyAutomationRules(own);
            check.Equal(0, own.Count, "interleaved sessions share definitions, not live rules");
            StrictContent(check); LogicalRules(check); Corruption(check); ProviderFaults(check);
            InputRecoveryTests.Contracts(check);
            ThresholdSettingsTests.Contracts(check);
            AutomationProfileTests.Contracts(check);
            ControlActionPolicyTests.Contracts(check);
            OutletReserveActionTests.Contracts(check);
            StockTargetActionTests.Contracts(check);
            RuntimeActionSettingsTests.Contracts(check);
            StockBandControlTests.Contracts(check);
        }

        private static void StrictContent(Check check)
        {
            foreach (string fault in new[] { "arbitration", "latch", "duration", "actions", "unit", "band", "input", "root", "cycle", "unused", "boolean", "unknown" })
                check.Throws<ContentException>(() => New(Factory(json => {
                    JsonValue rule = json.Get("control_rules")[0], node = rule.Get("conditions")[0];
                    switch (fault)
                    {
                        case "arbitration": rule.Remove("arbitration"); break;
                        case "latch": rule.Remove("initial_latch"); break;
                        case "duration": rule.Remove("dwell"); break;
                        case "actions": rule.Get("when_true").Remove("enabled"); break;
                        case "unit": node.Set("unit", JsonValue.Of("unit.wrong")); break;
                        case "band": node.Set("off", JsonValue.Number("6")); break;
                        case "input": node.Set("input", JsonValue.Of("missing")); break;
                        case "root": rule.Set("root", JsonValue.Of("missing")); break;
                        case "cycle": rule.Set("conditions", Json.Parse("[{\"id\":\"high\",\"kind\":\"all\",\"initial\":false,\"conditions\":[\"high\"]}]")); break;
                        case "unused": rule.Get("conditions").Add(Json.Parse("{\"id\":\"unused\",\"kind\":\"above\",\"initial\":false,\"input\":\"level\",\"unit\":\"unit.level\",\"on\":5,\"off\":3}")); break;
                        case "boolean": node.Set("kind", JsonValue.Of("boolean")); node.Set("expected", JsonValue.Of(true)); node.Remove("unit"); node.Remove("on"); node.Remove("off"); break;
                        case "unknown": rule.Set("legacy", JsonValue.Of(true)); break;
                    }
                })), "strict JSON refuses " + fault + " before initialization");
            GameFactory bounds = Factory(json => json.Get("control_rules")[0].Get("when_true").Set("target", JsonValue.Number("11")));
            SimGame game = New(bounds); var settings = Settings(game);
            check.Equal(OperationCodes.TargetBounds, View(game).PreviewAutomation("bounds", settings), "both actions preflight the actual bounded operation contract");
            check.Equal(OperationCodes.TargetBounds, Configure(game, settings, "bounds").Refusal, "excessive action is refused by the same command handler");
            GameFactory units = Factory(json => {
                json.Get("control_rules")[0].Get("when_true").Set("unit", JsonValue.Of("unit.other"));
                json.Get("control_rules")[0].Get("when_false").Set("unit", JsonValue.Of("unit.other"));
            });
            game = New(units);
            check.Equal(AutomationCodes.UnitMismatch, View(game).PreviewAutomation("units", Settings(game)), "operation units must match exactly without implicit conversion");
        }

        private static void LogicalRules(Check check)
        {
            GameFactory factory = Factory(json => {
                JsonValue rule = json.Get("control_rules")[0];
                rule.Get("inputs").Add(Json.Parse("{\"id\":\"enabled\",\"signal\":\"probe.enabled\"}"));
                rule.Get("conditions").Add(Json.Parse("{\"id\":\"low\",\"kind\":\"below\",\"input\":\"level\",\"unit\":\"unit.level\",\"on\":2,\"off\":4,\"initial\":false}"));
                rule.Get("conditions").Add(Json.Parse("{\"id\":\"enabled\",\"kind\":\"boolean\",\"input\":\"enabled\",\"expected\":true,\"initial\":false}"));
                rule.Get("conditions").Add(Json.Parse("{\"id\":\"outside\",\"kind\":\"any\",\"initial\":false,\"conditions\":[\"low\",\"high\"]}"));
                rule.Get("conditions").Add(Json.Parse("{\"id\":\"root\",\"kind\":\"all\",\"initial\":false,\"conditions\":[\"outside\",\"enabled\"]}"));
                rule.Set("root", JsonValue.Of("root"));
            });
            SimGame game = New(factory); var inputs = new Dictionary<string, ControlBinding> { ["level"] = Binding(Find(game, "sensor")), ["enabled"] = Binding(Find(game, "sensor")) };
            var profile = game.World.Content.Get<IControlRule>("high-level"); var bands = ControlThresholds.Initial(profile);
            check.That(Configure(game, new AutomationSettings("high-level", true, Binding(Find(game, "actuator")), inputs, bands, profile.WhenTrue, profile.WhenFalse)).Accepted, "logical DAG uses exact named inputs through ordinary admission");
            check.That(Rule(game).State["high"] && !Rule(game).State["low"] && Rule(game).State["outside"] && Rule(game).State["root"], "all/any/boolean/comparison nodes evaluate in shared canonical topology order");
            Level(check, game, 4); check.That(Rule(game).State["high"], "above node retains its own hysteresis within the band");
            Level(check, game, 2); check.That(!Rule(game).State["outside"], "strict below threshold does not cross at equality");
            Level(check, game, 1); check.That(Rule(game).State["low"] && Rule(game).State["root"], "below branch enables the same any/all graph");
            Level(check, game, 3); check.That(Rule(game).State["low"], "below node retains hysteresis until its separate off threshold");
            Level(check, game, 4); check.That(!Rule(game).State["low"] && !Rule(game).State["root"], "below node unlatches at off equality");
            check.That(Configure(New(factory), new AutomationSettings("high-level", true, Binding(Find(game, "actuator")), inputs, bands, profile.WhenTrue, profile.WhenFalse), "same-layout").Accepted, "binding IDs resolve only inside the receiving session, not retained provider objects");
        }

        private static void Processes(Check check)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            check.That(Configure(game, Settings(game)).Accepted, "timed rule starts its physical process policy");
            check.That(Enabled(game) && Rule(game).CandidateSince == 0 && !Rule(game).PendingDecision.HasValue, "dwell begins without instant shutdown");
            Twin(check, factory, game);
            game.Update(); check.That(Enabled(game) && !Rule(game).PendingDecision.HasValue, "one elapsed tick cannot satisfy a two-minute dwell");
            game.Update(); check.That(Enabled(game) && Rule(game).PendingDecision == true && Rule(game).PendingAt == game.Tick, "satisfied dwell queues only the next-step action");
            Twin(check, factory, game);
            game.Clock.Pause(); long tick = game.Tick; game.Update();
            check.That(game.Tick == tick && Rule(game).PendingAt == tick && Enabled(game), "game pause freezes pending action and game-time deadlines");
            game.Clock.Resume(); game.Update();
            check.That(!Enabled(game) && Rule(game).Latch && Rule(game).LastAppliedAt == 3 && Rule(game).CooldownUntil == 7, "normal handler applies once at declared next-step phase and starts authored cooldown");
            Twin(check, factory, game);
            Level(check, game, 4); check.That(!Enabled(game) && Rule(game).State["high"], "hysteresis prevents a band-internal restart");
            Level(check, game, 3); check.That(Rule(game).Candidate == false && Rule(game).CandidateSince == 5, "off threshold begins a distinct stable candidate");
            game.Update(); check.That(!Enabled(game), "false dwell cannot invent completed work");
            game.Update(); check.That(!Enabled(game) && Rule(game).PendingDecision == false, "restart waits through both dwell and cooldown then defers");
            Twin(check, factory, game); game.Update(); check.That(Enabled(game) && !Rule(game).Latch, "physical operation restarts through its own ordinary controls");
            Level(check, game, 6); game.RunTicks(2);
            check.That(Rule(game).Candidate == true && !Rule(game).PendingDecision.HasValue, "completed dwell still waits for the actual cooldown deadline");
            game.Update(); check.That(Rule(game).PendingDecision == true, "cooldown deadline admits the next deterministic intent");
            Twin(check, factory, game);
            check.That(Configure(game, Settings(game, enabled: false)).Accepted, "explicit disable cancels an uncommitted pending intent");
            check.That(Enabled(game) && !Rule(game).EffectiveEnabled && !Rule(game).PendingDecision.HasValue, "policy disable does not apply or undo process controls");
            SimGame feedback = New(factory); check.That(Configure(feedback, Settings(feedback, "feedback", target: "sensor")).Accepted, "same-source control feedback is valid with next-step timing");
            check.That(Enabled(feedback, "sensor") && Rule(feedback).PendingDecision == true, "new feedback decision never changes its same-step input");
            for (int i = 0; i < 4; i++)
            {
                Twin(check, factory, feedback); feedback.Update();
                check.Equal(i % 2 != 0, Enabled(feedback, "sensor"), "feedback advances once per step rather than recursively");
            }
            SimGame cancelled = New(factory); Configure(cancelled, Settings(cancelled, "feedback"));
            check.That(TestKit.Order(cancelled, 0, new RemoveAutomationCommand { Rule = "field" }).Accepted, "remove command cancels policy before its due phase");
            check.That(Rule(cancelled) == null && Enabled(cancelled), "removal never applies an abandoned deferred action");
            SimGame mod = New(factory); check.That(Configure(mod, Settings(mod, "mod.inverse", target: "sensor")).Accepted, "independently authored rule executes through IControlRule with no scheduler branch");
            mod.Update(); check.That(!Enabled(mod, "sensor") && Rule(mod).State.ContainsKey("inverse"), "mod-defined state and action use the ordinary operation owner");
            Twin(check, factory, mod); mod.Update(); check.That(Enabled(mod, "sensor"), "mod feedback also has bounded next-step semantics");
            NativeProduction(check, factory); ReferenceFailures(check, factory); TimingBoundaries(check); CrossFeedback(check, factory); Corruption(check);
            ThresholdSettingsTests.Execution(check);
            AutomationProfileTests.PlayerExecution(check);
            OutletReserveActionTests.Execution(check);
            StockTargetActionTests.Execution(check);
            RuntimeActionSettingsTests.Execution(check);
            StockBandControlTests.Execution(check);
        }

        private static void CrossFeedback(Check check, GameFactory factory)
        {
            SimGame game = New(factory);
            var reverse = new AutomationSettings("feedback", true, Binding(Find(game, "sensor")),
                new Dictionary<string, ControlBinding> { ["enabled"] = Binding(Find(game, "actuator")) }, ControlThresholds.Initial(game.World.Content.Get<IControlRule>("feedback")),
                game.World.Content.Get<IControlRule>("feedback").WhenTrue, game.World.Content.Get<IControlRule>("feedback").WhenFalse);
            game.Seat(0).Submit(new ConfigureAutomationCommand { Rule = "forward", Settings = Settings(game, "feedback") });
            game.Seat(0).Submit(new ConfigureAutomationCommand { Rule = "reverse", Settings = reverse });
            game.Update();
            check.That(Enabled(game) && Enabled(game, "sensor") && Rule(game, "forward").PendingDecision == true
                && Rule(game, "reverse").PendingDecision == true, "cross-bound policies capture both initial signals before any new action");
            for (int i = 0; i < 4; i++)
            {
                Twin(check, factory, game); game.Update();
                check.That(Enabled(game) == (i % 2 != 0) && Enabled(game, "sensor") == (i % 2 != 0),
                    "multiple feedback rules commit one old intent each, not an order-dependent recursive cascade");
            }
        }

        private static void TimingBoundaries(Check check)
        {
            GameFactory factory = Factory(processEdit: json => json.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")));
            SimGame game = New(factory); Configure(game, Settings(game)); game.RunTicks(3);
            check.That(Enabled(game) && !Rule(game).PendingDecision.HasValue, "authored two-minute dwell needs four actual thirty-second quanta");
            game.Update(); check.That(Enabled(game) && Rule(game).PendingAt == 5, "actual quantum conversion still defers action to the next step");
            game.Update(); check.That(!Enabled(game) && Rule(game).CooldownUntil == 13, "four-minute cooldown resolves to eight session ticks");
            Twin(check, factory, game);
            factory = Factory(); game = New(factory); long start = (1L << 45) + 19;
            WideTimeTests.SetTick(game, start); Configure(game, Settings(game));
            check.Equal(start, Rule(game).CandidateSince, "candidate timestamps retain full-width game time");
            game.RunTicks(2); check.Equal(start + 3, Rule(game).PendingAt, "deferred deadlines never narrow to platform integer ages");
            Twin(check, factory, game); game.Update();
            check.Equal(start + 7, Rule(game).CooldownUntil, "applied cooldown remains exact past integer tick limits");
            Twin(check, factory, game);
        }

        private static void Twin(Check check, GameFactory factory, SimGame game)
        {
            byte[] save = game.Save();
            SimGame first = factory.Load(save), second = factory.Load(save);
            check.Equal(game.Hash(), first.Hash(), "checkpoint retains candidate/pending/cooldown/latch through the sole rule codec");
            first.Update(); second.Update();
            check.Equal(first.Hash(), second.Hash(), "saved twins continue identically through actual public Update");
            first.Update(); second.Update(); check.Equal(first.Hash(), second.Hash(), "interleaved restored sessions remain isolated");
        }

        private static void NativeProduction(Check check, GameFactory factory)
        {
            SimGame game = New(factory); Entity assembler = Find(game, "assembler");
            check.That(Configure(game, Settings(game, "high-level", target: "assembler")).Accepted, "same generic rule can control a native transformer instead of a proof provider");
            check.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = assembler.Id, Transformation = "build" }).Accepted, "player queues real transformation through its existing command");
            game.RunTicks(2); WorkProgress progress = assembler.Get<Transformer>().Progress;
            check.That(!Enabled(game, "assembler") && progress.WholeTicks > 0, "deferred operation gate pauses actual elapsed native work, not a host counter");
            ResourceId supply = game.World.Content.ResourceId("supply"); Fix stock = assembler.Get<IInventory>().Amount(supply);
            game.RunTicks(4); check.Equal(progress, assembler.Get<Transformer>().Progress, "disabled transformation retains its actual progress");
            check.Equal(stock, assembler.Get<IInventory>().Amount(supply), "disabled transformation does not duplicate native input consumption");
            Level(check, game, 2); game.RunTicks(9);
            check.That(assembler.Get<Transformer>().Queue.Count == 0 && assembler.Get<IInventory>().Amount(supply) == Fix.FromInt(18), "engine automation resumes and completes the ordinary material workflow exactly once");
            SimGame restored = factory.Load(game.Save()); game.Update(); restored.Update(); check.Equal(game.Hash(), restored.Hash(), "automated native process continuation remains deterministic");
        }

        private static void ReferenceFailures(Check check, GameFactory factory)
        {
            SimGame offline = New(factory); Configure(offline, Settings(offline, "feedback")); Level(check, offline, 6, false);
            check.That(Enabled(offline) && !Rule(offline).HasApplied && !Rule(offline).PendingDecision.HasValue
                && Rule(offline).Blocker == new Code("probe.sensor-offline"), "input failure before a due intent cancels it before any real control change");
            Twin(check, factory, offline);
            SimGame game = New(factory); Configure(game, Settings(game)); Level(check, game, 6, false);
            check.That(!Rule(game).EffectiveEnabled && Rule(game).Blocker == new Code("probe.sensor-offline") && !Rule(game).Candidate.HasValue,
                "unavailable signal suspends with its exact typed blocker, never zero");
            SimGame restored = factory.Load(game.Save()); check.Equal(game.Hash(), restored.Hash(), "suspended rule saves its actual blocker");
            Level(check, game, 6); check.That(!Rule(game).EffectiveEnabled, "recovered source does not silently reactivate a suspended policy");
            check.That(Configure(game, Settings(game)).Accepted && Rule(game).EffectiveEnabled, "explicit successful configuration resumes the rule");
            SimGame missing = New(factory); Configure(missing, Settings(missing, "feedback"));
            Entity sensor = Find(missing, "sensor"); missing.World.Despawn(sensor); missing.World.Settle();
            restored = factory.Load(missing.Save()); missing.Update(); restored.Update();
            check.That(Rule(missing).Blocker == Codes.NotFound && Enabled(missing) && !Rule(missing).PendingDecision.HasValue,
                "removed input cancels a due action without binding another entity");
            check.Equal(missing.Hash(), restored.Hash(), "dangling pre-action references refuse identically after restore");
            SimGame rebind = New(factory); Configure(rebind, Settings(rebind, "feedback")); rebind.Update();
            check.That(!Enabled(rebind), "first operation has an actual committed control");
            check.That(Configure(rebind, Settings(rebind, "feedback", target: "sensor")).Accepted && !Rule(rebind).HasApplied,
                "changed target starts a new decision lifecycle rather than inheriting a foreign target latch");
            rebind.Update(); check.That(!Enabled(rebind, "sensor"), "rebound target receives its own ordinary deferred action");
        }

        private static void ProviderFaults(Check check)
        {
            foreach (int fault in new[] { 1, 2, 3, 4 })
            {
                GameFactory factory = Factory(); SimGame game = New(factory);
                Configure(game, Settings(game, "feedback"));
                Entity entity = Find(game, fault == 1 || fault == 2 ? "sensor" : "actuator");
                entity.Get<AutomationProbe>().FaultMode = fault;
                game.Update();
                check.Equal(GameState.Faulted, game.State, "malformed/mutating/lying provider faults normal Update: " + fault);
                check.Throws<InvalidOperationException>(() => game.Save(), "faulted partial controls cannot be saved: " + fault);
                check.Throws<InvalidOperationException>(() => game.Hash(), "faulted partial controls cannot be hashed as valid: " + fault);
            }
        }

        private static void Corruption(Check check)
        {
            foreach (string fault in new[] { "company", "keys", "candidate", "candidate-now", "pending", "zero-pending", "dual", "suspended", "latch", "cooldown", "applied", "decision", "duplicate", "conflict",
                "wait-policy", "wait-blocker", "wait-clear", "wait-disabled", "wait-input", "wait-conflict", "wait-intent",
                "threshold-key", "threshold-unit", "threshold-band", "action-unit", "applied-unit", "applied-missing" })
            {
                bool waiting = fault.StartsWith("wait-", StringComparison.Ordinal);
                bool multiple = fault == "duplicate" || fault == "conflict" || fault == "wait-conflict";
                SimGame game = New(Factory(root =>
                {
                    if (waiting && fault != "wait-policy") root.Get("control_rules")[0].Set("input_recovery", JsonValue.Of("proof.input.offline"));
                })); AutomationSettings settings = Settings(game, enabled: fault != "wait-disabled");
                if (fault == "action-unit") settings = new AutomationSettings(settings.Profile, settings.Enabled, settings.Target,
                    settings.Inputs, settings.Thresholds, new ControlAction(false, Fix.One, new Code("unit.count")), settings.WhenFalse);
                if (fault.StartsWith("threshold-", StringComparison.Ordinal))
                    settings = new AutomationSettings(settings.Profile, settings.Enabled, settings.Target, settings.Inputs,
                        new Dictionary<string, ControlThresholdBand> { [fault == "threshold-key" ? "other" : "high"] =
                            new ControlThresholdBand(new Code(fault == "threshold-unit" ? "unit.count" : "unit.level"),
                                Fix.FromInt(fault == "threshold-band" ? 2 : 5), Fix.FromInt(3)) }, settings.WhenTrue, settings.WhenFalse);
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); writer.Count(multiple ? 2 : 1);
                    for (int i = 0; i < (multiple ? 2 : 1); i++)
                    {
                        PlayerId company = new PlayerId(fault == "company" ? 9 : 0); writer.Player(ref company);
                        string id = i == 1 && fault != "duplicate" ? "second" : "field"; writer.String(ref id); AutomationSettings.Sync(writer, settings);
                        writer.Count(1); string key = fault == "keys" ? "other" : "high"; writer.String(ref key); bool value = false; writer.Bool(ref value);
                        bool latch = fault == "latch", applied = fault == "applied" || fault.StartsWith("applied-", StringComparison.Ordinal); writer.Bool(ref latch); writer.Bool(ref applied);
                        if (applied) ControlAction.Sync(writer, fault == "applied-missing" ? default : fault == "applied-unit"
                            ? new ControlAction(true, Fix.One, new Code("unit.count")) : settings.WhenFalse);
                        byte candidate = fault == "candidate" || fault == "candidate-now" || fault == "dual" ? (byte)2 : (byte)0; writer.Byte(ref candidate);
                        long since = candidate == 0 ? -1 : fault == "candidate" ? 1 : 0; writer.Long(ref since);
                        long cooldown = fault == "cooldown" ? 5 : 0; writer.Long(ref cooldown);
                        byte pending = fault == "decision" ? (byte)3 : fault == "pending" || fault == "zero-pending" || fault == "dual" || fault == "suspended" || fault == "wait-intent" ? (byte)2 : (byte)0; writer.Byte(ref pending);
                        long due = pending == 0 ? -1 : fault == "pending" ? 1 : 0; writer.Long(ref due);
                        long at = fault == "applied" ? 0 : -1; writer.Long(ref at);
                        string blocker = fault == "wait-blocker" ? "unknown.failure" : fault == "suspended" || waiting && fault != "wait-clear" ? "probe.sensor-offline" : null; writer.String(ref blocker);
                        string waitingInput = waiting ? fault == "wait-input" ? "missing" : "level" : null; writer.String(ref waitingInput);
                    }
                    writer.Flush(); byte[] bytes = stream.ToArray();
                    var system = (GameSystem)game.World.Services.Get<IAutomationRules>();
                    check.Throws<CorruptStateException>(() => { system.Sync(new StateReader(bytes, 0, bytes.Length)); system.OnLoaded(game.World); },
                        "sole rule codec rejects corrupt " + fault + " before continuation");
                }
            }
        }
    }
}
