using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static partial class PlanTemplateTests
    {
        private static GameFactory ReferenceFactory(bool automatic = false, Action<JsonValue> edit = null, Action<JsonValue> historyEdit = null,
            bool componentEnabled = true, ProofPlanReferenceControllersModule controllerModule = null)
        {
            var moduleRows = JsonValue.NewArray();
            moduleRows.Add(Content.Json.Parse(FrameworkProofContent.Read("plan-references-manifest.json")));
            moduleRows.Add(Content.Json.Parse("{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[]}"));
            var modules = new List<GameModule> { new PlanReferenceProofModule(), controllerModule ?? new ProofPlanReferenceControllersModule() };
            var document = Content.Json.Parse(FrameworkProofContent.Read("plan-references.json")); edit?.Invoke(document);
            var documents = new Dictionary<string, string> { ["plan-references.json"] = document.ToString() };
            if (automatic)
            {
                moduleRows.Add(Content.Json.Parse(FrameworkProofContent.Read("plan-history-manifest.json")));
                modules.Add(new PlanHistoryPolicyProofModule());
                var history = Content.Json.Parse(FrameworkProofContent.Read("plan-history.json")); historyEdit?.Invoke(history);
                documents.Add("plan-history.json", history.ToString());
            }
            return Repeating(root => {
                    var parts = root.Get("entities")[0].Get("components");
                    parts.Set("proof_plan_history", Content.Json.Parse("{\"group\":\"reference.component\",\"enabled\":true}"));
                    parts.Get("proof_plan_history").Set("enabled", JsonValue.Of(componentEnabled));
                    parts.Set("plan_reference_private_probe", Content.Json.Parse("{}"));
                },
                extraModules: modules, extraDeclarations: moduleRows.ToString(), extraDocuments: documents,
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(PlanReferenceProofModule.ModuleId));
                    scenario.Get("settings").Set(PlanReferenceProofModule.ModuleId,
                        Content.Json.Parse("{\"service\":\"reference.service\",\"system\":\"reference.system\"}"));
                    scenario.Get("modules").Add(JsonValue.Of(PlayerControllersModule.ModuleId));
                    scenario.Get("settings").Set(PlayerControllersModule.ModuleId,
                        Content.Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"reference.controller\"}]}"));
                    if (automatic) HistoryScenario("history.public")(scenario);
                });
        }
        // Fault instrumentation reads the actual bound controller, never another game controller instance.
        private static ProofPlanReferenceController ReferenceController(SimGame game)
        {
            object system = game.World.Services.Get<IPlayerControllers>();
            var rows = (IDictionary)system.GetType().GetField("_bindings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(system);
            object binding = rows[0];
            return (ProofPlanReferenceController)binding.GetType().GetField("Controller").GetValue(binding);
        }
        private static void SetReferenceFault(SimGame game, string kind, bool corrupt, bool throws)
        {
            Action alter; Action<Action> assign;
            if (kind == "component" || kind == "component-sibling")
            {
                var provider = TemplateOwner(game).Get<ProofPlanHistoryRetention>();
                alter = kind == "component-sibling" ? TemplateOwner(game).Get<ExposurePrivateProbe>().Corrupt : provider.CorruptReadState;
                assign = action => provider.ReadFault = action;
            }
            else
            {
                var state = kind == "service" ? game.World.Services.Get<ProofPlanReferenceService>().References
                    : kind == "system" ? game.World.Services.Get<ProofPlanReferenceSystem>().References : ReferenceController(game).References;
                alter = state.CorruptReadState; assign = action => state.ReadFault = action;
            }
            assign(() => { if (corrupt) alter(); if (throws) throw new ApplicationException("reference callback probe"); });
        }
        private static void ClearReferenceFault(SimGame game, string kind)
        {
            if (kind == "component" || kind == "component-sibling") TemplateOwner(game).Get<ProofPlanHistoryRetention>().ReadFault = null;
            else
            {
                var state = kind == "service" ? game.World.Services.Get<ProofPlanReferenceService>().References
                    : kind == "system" ? game.World.Services.Get<ProofPlanReferenceSystem>().References : ReferenceController(game).References;
                state.ReadFault = null;
            }
        }
        private static void ReferenceObservationContracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current reference observation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current self-described reference/provider codecs");
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(ReferenceFactory(edit: document => document.Get("plan_reference_profiles")[0].Remove("enabled"))),
                "public reference policy state is explicit JSON, not a legacy enabled default");
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Repeating(root => root.Get("entities")[0].Get("components").Set("proof_plan_history",
                Content.Json.Parse("{\"group\":\"reference.component\"}")))), "component reference state is explicitly authored");
            GameFactory factory = ReferenceFactory(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            var ids = new Dictionary<string, long>(); int position = 12;
            foreach (string kind in new[] { "service", "system", "component", "controller" })
            {
                long id = DependencySubmit(c, game, DependencyBuild(game, position, "reference." + kind)); position += 4;
                CancelHistory(c, game, "reference." + kind); ids.Add(kind, id);
                c.Equal(PlanCodes.HistoryProvider, View(game).PreviewRetirePlanHistory(History(id)).Blocker, "actual authored " + kind + " retains its native history");
            }
            var service = game.World.Services.Get<ProofPlanReferenceService>(); var system = game.World.Services.Get<ProofPlanReferenceSystem>();
            c.That(ReferenceEquals(service, game.World.Services.Get<IPlanHistoryRetention>())
                && ReferenceEquals(system, game.World.System<ProofPlanReferenceSystem>()), "service and system aliases share their one actual provider/codec owner");
            c.That(game.World.Content.Get<ProofPlanReferenceProfileDef>("reference.service").AuthoringPath.Contains("plan-references.json"),
                "reference providers come from a separately master-listed JSON document");
            int serviceReads = 0, systemReads = 0;
            service.References.ReadFault = () => serviceReads++; system.References.ReadFault = () => systemReads++;
            ulong hash = game.Hash(); long time = game.Clock.StepIndex;
            c.Equal(PlanCodes.HistoryProvider, View(game).PreviewRetirePlanHistory(History(ids["system"])).Blocker, "pure system reference admission preserves the same blocker");
            c.That(serviceReads == 1 && systemReads == 1, "service aliases and system/service dual discovery observe each instance once per actual plan");
            service.References.ReadFault = null; system.References.ReadFault = null;
            c.Equal(hash, game.Hash(), "actual provider codec observation is read-only"); c.Equal(time, game.Clock.StepIndex, "reference observation uses, never advances, the sole game clock");
            byte[] saved = game.Save(); SimGame twin = factory.Load(saved);
            c.Equal(hash, twin.Hash(), "service/system/component/controller states persist once through their real owners");
            foreach (string kind in ids.Keys)
            {
                c.Equal(PlanCodes.HistoryProvider, View(twin).PreviewRetirePlanHistory(History(ids[kind])).Blocker, "saved " + kind + " still owns its reference");
                foreach (bool throwing in new[] { false, true })
                {
                    SimGame bad = factory.Load(saved); SetReferenceFault(bad, kind, true, throwing);
                    if (throwing) c.Throws<ApplicationException>(() => View(bad).PreviewRetirePlanHistory(History(ids[kind])), "original changed-state callback error survives " + kind);
                    else c.Throws<InvalidOperationException>(() => View(bad).PreviewRetirePlanHistory(History(ids[kind])), "silent " + kind + " codec mutation fails");
                    if (throwing) c.That(bad.Fault?.Message.Contains("reference callback probe") == true, "first " + kind + " callback cause is not replaced by verification");
                    c.That(bad.Fault != null && bad.Fault.Phase == "observation.provider" && bad.State == GameState.Faulted,
                        "impure external " + kind + " preview latches the existing session fault");
                    c.Equal(4, ReferenceRecordCount(bad), "provider violation cannot publish successful retirement or erase a prefix");
                    c.Throws<InvalidOperationException>(() => bad.Save(), "partial observed state cannot be saved as resumable gameplay");
                    c.Throws<InvalidOperationException>(() => bad.Hash(), "partial observed state is not a successful deterministic checkpoint");
                    c.Equal(0, bad.Update(), "faulted game cannot resume merely because provider exception was caught");
                    c.Equal(time, bad.Clock.StepIndex, "fault never completes another game update");
                    c.Equal(hash, game.Hash(), "fault in one actual session does not mutate another reference owner/session");
                }
                SimGame unchanged = factory.Load(saved); SetReferenceFault(unchanged, kind, false, true);
                c.Throws<ApplicationException>(() => View(unchanged).PreviewRetirePlanHistory(History(ids[kind])), "unchanged throwing provider preserves its original exception");
                ClearReferenceFault(unchanged, kind);
                c.That(unchanged.Fault?.Message.Contains("reference callback probe") == true, "unexpected pure " + kind + " callback error is an actual terminal provider failure");
                c.Throws<InvalidOperationException>(() => unchanged.Hash(), "clearing a failed " + kind + " hook cannot publish a healthy hash");
                c.Throws<InvalidOperationException>(() => unchanged.Save(), "clearing a failed " + kind + " hook cannot checkpoint");
                SimGame commandGame = factory.Load(saved); SetReferenceFault(commandGame, kind, true, false);
                int ticket = commandGame.Seat(0).Submit(new RetirePlanHistoryCommand { Selection = History(ids[kind]) });
                c.Equal(0, commandGame.Update(), "ordinary retirement command stops on impure " + kind + " reference");
                bool accepted = false;
                foreach (CommandResult answer in commandGame.World.LastTickResults)
                    if (answer.Sequence == ticket && answer.Player == new PlayerId(0) && answer.Accepted) accepted = true;
                c.That(!accepted && commandGame.Fault?.Phase == "observation.provider" && ReferenceRecordCount(commandGame) == 4,
                    "impure command publishes no accepted retirement result or deleted history prefix");
            }
            SimGame swallowed = factory.Load(saved); var swallowedService = swallowed.World.Services.Get<ProofPlanReferenceService>();
            swallowedService.References.ReadFault = () => {
                var nested = EntityObservation.CaptureProvider(swallowed.World, swallowedService);
                swallowedService.References.CorruptReadState();
                try { nested.Verify(); } catch (InvalidOperationException) { }
                swallowedService.References.CorruptReadState();
            };
            c.Throws<InvalidOperationException>(() => View(swallowed).PreviewRetirePlanHistory(History(ids["service"])),
                "restoring provider fields and swallowing an inner violation cannot erase the existing session fault");
            c.That(swallowed.Fault?.Phase == "observation.provider" && swallowed.Update() == 0 && ReferenceRecordCount(swallowed) == 4,
                "latched shared observation failure remains authoritative without rollback or partial retirement");
            var reentrant = factory.Load(saved); int calls = 0;
            reentrant.World.Services.Get<ProofPlanReferenceService>().References.ReadFault = () => {
                calls++; try { View(reentrant).PreviewRetirePlanHistory(History(ids["service"])); } catch (InvalidOperationException) { }
            };
            c.Throws<InvalidOperationException>(() => View(reentrant).PreviewRetirePlanHistory(History(ids["service"])), "swallowing recursive reference admission cannot publish a healthy result");
            c.That(calls == 1 && reentrant.Fault?.Phase == "observation.provider", "original reference reentry guard owns the first terminal fault");
            c.Equal(4, ReferenceRecordCount(reentrant), "swallowed reentry retires no history prefix");
            c.Throws<InvalidOperationException>(() => reentrant.Save(), "swallowed reentry cannot checkpoint");
            TemplateAdvance(game, 3, twin); c.Equal(game.Hash(), twin.Hash(), "pure saved reference owners continue with the same engine update/clock");
            ReferenceSiblingFaults(c);
            ReferenceSchemaFaults(c);
        }
        private static void ReferenceSchemaFaults(Check c)
        {
            var healthyInit = new InitializeProbe(); var healthyModule = new ProofPlanReferenceControllersModule(healthyInit);
            var healthyFactory = ReferenceFactory(controllerModule: healthyModule); var healthy = ConstructionPlanTests.NewPoints(healthyFactory);
            c.That(healthyInit.Initializations == 1 && healthy.Fault == null, "actual schema provider initializes without a wrapper/controller substitute");
            ulong healthyHash = healthy.Hash(); c.Equal(healthyHash, healthyFactory.Load(healthy.Save()).Hash(), "ordinary controller schema and real reference codec restore exactly");
            var faults = new[] { "throw", "pause", "pause-throw", "corrupt", "corrupt-throw" };
            foreach (string fault in faults)
            {
                var init = new InitializeProbe(); var module = new ProofPlanReferenceControllersModule(init); Exception caught = null;
                var factory = ReferenceFactory(edit: json => json.Get("plan_reference_controllers")[0].Set("schema_fault", JsonValue.Of(fault)), controllerModule: module);
                try { ConstructionPlanTests.NewPoints(factory); } catch (Exception error) { caught = error; }
                c.That(caught != null, fault + " impure controller schema cannot create a healthy world");
                c.Equal(0, init.Initializations, fault + " schema admission fails before Initialize");
                c.Equal("observation.provider", module.ObservationFaultPhase, fault + " actual schema observation owns first startup failure");
                if (fault.Contains("throw")) c.That(caught is ApplicationException && caught.Message.Contains("proof.reference-schema"), fault + " original schema getter error survives startup");
            }
            var isolatedFactory = ReferenceFactory(); var isolated = ConstructionPlanTests.NewPoints(isolatedFactory); ulong untouched = isolated.Hash();
            foreach (bool restored in new[] { false, true })
            foreach (string fault in faults)
            foreach (string path in new[] { "hash", "save", "breakdown" })
            {
                var factory = ReferenceFactory(); var bad = ConstructionPlanTests.NewPoints(factory);
                long id = DependencySubmit(c, bad, DependencyBuild(bad, 12, "reference.controller")); CancelHistory(c, bad, "reference.controller");
                if (restored) bad = factory.Load(bad.Save());
                var controller = ReferenceController(bad); int calls = 0; long at = bad.Tick;
                controller.SchemaFault = () => {
                    calls++;
                    if (fault == "corrupt" || fault == "corrupt-throw") controller.References.CorruptReadState();
                    if (fault == "pause" || fault == "pause-throw") bad.Clock.Pause();
                    if (fault.Contains("throw")) throw new ApplicationException("proof.reference-schema");
                };
                Exception caught = null;
                try { if (path == "hash") bad.Hash(); else if (path == "save") bad.Save(); else bad.HashBreakdown(); }
                catch (Exception error) { caught = error; }
                c.That(caught != null && bad.Fault?.Phase == "observation.provider", path + "/" + fault + " fresh/restored schema read rejects impure checkpoint metadata");
                c.Equal(1, calls, path + "/" + fault + " schema getter runs once before checkpoint publication");
                if (fault.Contains("throw")) c.That(caught?.ToString().Contains("proof.reference-schema") == true
                    && bad.Fault?.Message.Contains("proof.reference-schema") == true, path + "/" + fault + " first callback cause remains original");
                if (bad.Fault == null) continue; // Keep actual baseline failures; do not invent terminal facts.
                var first = bad.Fault; controller.SchemaFault = null;
                c.Equal(1, ReferenceRecordCount(bad), "failed schema checkpoint erases no original plan history");
                c.Equal(PlanState.Cancelled, ReferenceRecordState(bad, id), "failed schema keeps actual cancelled plan identity");
                c.Throws<InvalidOperationException>(() => bad.Hash(), "clearing schema hook cannot publish healthy hash");
                c.Throws<InvalidOperationException>(() => bad.Save(), "clearing schema hook cannot checkpoint partial state");
                c.Equal(0, bad.Update(), "failed schema cannot complete an update");
                c.That(ReferenceEquals(first, bad.Fault) && bad.Tick == at && calls == 1, "original schema first fault and sole clock remain authoritative");
                c.Equal(untouched, isolated.Hash(), "failed controller schema leaves independent state untouched");
            }
        }
        // Negative framework diagnostics only: public gameplay views correctly reject terminal worlds.
        // Read the original native records; never write them or create a shadow history/model.
        private static IList ReferenceRecords(SimGame game)
        {
            var field = typeof(WorkBoard).GetField("_plans", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("missing actual native plan history");
            return (IList)field.GetValue(game.World.Services.Get<WorkBoard>());
        }
        private static int ReferenceRecordCount(SimGame game) => ReferenceRecords(game).Count;
        private static PlanState ReferenceRecordState(SimGame game, long id)
        {
            foreach (object row in ReferenceRecords(game))
                if ((long)row.GetType().GetField("Id", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(row) == id)
                    return (PlanState)row.GetType().GetField("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(row);
            throw new InvalidOperationException("missing original history identity " + id);
        }
        private static void ReferenceSiblingFaults(Check c)
        {
            foreach (bool held in new[] { false, true })
            {
                var factory = ReferenceFactory(componentEnabled: held); var game = ConstructionPlanTests.NewPoints(factory);
                long id = DependencySubmit(c, game, DependencyBuild(game, 12, "reference.component")); CancelHistory(c, game, "reference.component");
                TemplateReach(game, () => View(game).PreviewRetirePlanHistory(History(id)).Blocker == (held ? PlanCodes.HistoryProvider : Code.None), "original cancelled work settles before sibling probe");
                byte[] saved = game.Save(); ulong untouched = game.Hash();
                foreach (bool throwing in new[] { false, true })
                foreach (bool command in new[] { false, true })
                {
                    var bad = factory.Load(saved); var owner = TemplateOwner(bad); var sibling = owner.Get<ExposurePrivateProbe>();
                    int version = owner.Version, value = sibling.Value, calls = 0;
                    owner.Get<ProofPlanHistoryRetention>().ReadFault = () => { calls++; sibling.Corrupt(); if (throwing) throw new ApplicationException("reference callback probe"); };
                    Exception caught = null; int ticket = 0;
                    if (command)
                    {
                        ticket = bad.Seat(0).Submit(new RetirePlanHistoryCommand { Selection = History(id) });
                        c.Equal(0, bad.Update(), "impure sibling retirement command never completes an update");
                    }
                    else
                    {
                        try { View(bad).PreviewRetirePlanHistory(History(id)); } catch (Exception error) { caught = error; }
                        c.That(throwing ? caught is ApplicationException : caught is InvalidOperationException, "sibling mutation rejects preview with original callback error where thrown");
                    }
                    c.That(bad.Fault?.Phase == "observation.provider", "full retention component owner is observed even when it returns a healthy true/false decision");
                    c.That(calls == 1 && owner.Version == version && sibling.Value == value + 1, "negative sibling mutation uses its real codec, not entity revision or repeated callbacks");
                    if (bad.Fault == null) continue; // Preserve actual baseline failures without fabricating terminal facts.
                    if (throwing) c.That(bad.Fault.Message.Contains("reference callback probe"), "sibling-and-throw retains first cause");
                    c.Equal(1, ReferenceRecordCount(bad), "failed sibling preview/command erases no native history");
                    c.Equal(PlanState.Cancelled, ReferenceRecordState(bad, id), "original terminal history remains actual cancelled state");
                    if (command)
                    {
                        bool accepted = false;
                        foreach (var result in bad.World.LastTickResults) if (result.Player == new PlayerId(0) && result.Sequence == ticket && result.Accepted) accepted = true;
                        c.That(!accepted, "failed sibling callback publishes no accepted retirement result");
                    }
                    var first = bad.Fault; long at = bad.Tick;
                    c.Throws<InvalidOperationException>(() => bad.Save(), "sibling failure cannot checkpoint");
                    c.Throws<InvalidOperationException>(() => bad.Hash(), "sibling failure cannot hash as healthy");
                    c.Equal(0, bad.Update(), "sibling failure cannot advance");
                    c.That(ReferenceEquals(first, bad.Fault) && bad.Tick == at && calls == 1, "first sibling failure and sole game clock remain authoritative");
                    c.Equal(untouched, game.Hash(), "sibling failure leaves original saved reference session unchanged");
                }
            }
        }
        private static void ReferenceObservationAutomatic(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current automatic retention observation identity");
            foreach (var probe in new[] { (Kind: "service", Throws: true), (Kind: "system", Throws: true), (Kind: "component", Throws: true),
                (Kind: "controller", Throws: true), (Kind: "component-sibling", Throws: false), (Kind: "component-sibling", Throws: true) })
            {
                string kind = probe.Kind, group = "reference." + (kind == "component-sibling" ? "component" : kind);
                GameFactory factory = ReferenceFactory(true); SimGame game = ConstructionPlanTests.NewPoints(factory);
                long id = DependencySubmit(c, game, DependencyBuild(game, 12, group)); CancelHistory(c, game, group);
                TemplateAdvance(game, 2); c.Equal(PlanCodes.HistoryProvider, HistoryRows(game)[0].Blocker, "automatic expired " + kind + " history remains referenced");
                byte[] saved = game.Save(); long time = game.Clock.StepIndex;
                SetReferenceFault(game, kind, true, probe.Throws);
                c.Equal(0, game.Update(), "automatic retirement never completes a game update after impure " + kind + " reference");
                c.That(game.Fault?.Phase == "observation.provider" && ReferenceRecordState(game, id) == PlanState.Cancelled,
                    "automatic shared reference boundary faults without erasing the actual terminal record");
                if (probe.Throws) c.That(game.Fault?.Message.Contains("reference callback probe") == true, "automatic retention preserves original callback cause " + kind);
                c.Equal(time, game.Clock.StepIndex, "automatic reference fault does not advance a cleanup clock");
                c.Throws<InvalidOperationException>(() => game.Save(), "automatic partial retirement state cannot checkpoint");
                SimGame clean = factory.Load(saved); c.That(clean.Update() == 1 && clean.Fault == null && TemplatePlan(clean, id).State == PlanState.Cancelled,
                    "pre-fault saved native history and pure provider continue normally in a distinct session");
            }
        }
    }
}
