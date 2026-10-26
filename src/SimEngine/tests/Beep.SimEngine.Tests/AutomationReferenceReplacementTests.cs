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

namespace Beep.SimEngine.Tests
{
    internal static class AutomationReferenceReplacementTests
    {
        internal const string Case = "Automation replacement references: retained decisions clocks recovery and typed resource targets";
        internal const string IndependentCase = "Automation replacement references: declared independent stock targets preserve decisions and physical refill";
        internal static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(IndependentCase, Independent); }
        private static GameFactory Factory(string mode, int dwell, string refusal = null, string binding = null)
        {
            var document = Json.Parse(FrameworkProofContent.Read("automation-replacement.json"));
            var rule = document.Get("control_rules")[0];
            rule.Get("dwell").Set("numerator", JsonValue.Number(dwell.ToString()));
            if (mode.StartsWith("stock") || mode.StartsWith("reserve"))
            {
                rule.Set("action_policy", JsonValue.Of(mode.StartsWith("stock") ? "replacement.stock" : "replacement.reserve"));
                rule.Get("when_true").Set("unit", JsonValue.Of("unit.supply")); rule.Get("when_false").Set("unit", JsonValue.Of("unit.supply"));
            }
            if (mode == "delegate") rule.Set("action_policy", JsonValue.Of("replacement.delegate"));
            var modules = new List<GameModule> { new AutomationModule(), new AutomationProofModule() };
            var declarations = Json.Parse("[{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"automation-replacement.json\"]},{\"id\":\"proof.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.automation\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
            var documents = new Dictionary<string, string> { ["automation-replacement.json"] = document.ToString() };
            if (binding != null)
            {
                modules.Add(new EndpointProofModule()); modules.Add(new OutputPortProofModule());
                var added = Json.Parse("""[{"id":"proof.pools","version":"1.0.0","requires":[{"id":"sim.inventory","version":"1.0.0"},{"id":"sim.resource-flows","version":"1.0.0"}],"documents":["port-replacement.json"]},{"id":"proof.output-ports","version":"1.0.0","requires":[{"id":"proof.pools","version":"1.0.0"},{"id":"sim.transformations","version":"1.0.0"},{"id":"sim.world-setup","version":"1.0.0"}],"documents":[]}]""");
                for (int i = 0; i < added.Count; i++) declarations.Add(added[i]);
                var pools = Json.Parse(FrameworkProofContent.Read("port-replacement.json")); var second = pools.Get("resource_pools")[0].Clone();
                second.Set("id", JsonValue.Of("replacement.next")); second.Set("identity", JsonValue.Number("2")); pools.Get("resource_pools").Add(second);
                documents.Add("port-replacement.json", pools.ToString());
                var band = document.Get("logistics_policies")[0].Get("stock").Get("supply");
                band.Set("minimum", JsonValue.Number("5")); band.Set("desired", JsonValue.Number("5"));
                rule.Get("when_false").Set("target", JsonValue.Number("5"));
                documents["automation-replacement.json"] = document.ToString();
            }
            return EntityReferenceReplacementTests.Factory(moduleDocument:
                mode == "delegate" ? "{\"reference_control_actions\":[{\"id\":\"replacement.delegate\",\"policy\":\"replacement.operation\",\"replacement_mode\":\"" +
                    (refusal ?? "normal") + "\"}]}" : "{}",
                extraModules: modules,
                extraDeclarations: declarations.ToString(),
                extraDocuments: documents,
                edit: root =>
                {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.automation"));
                    var original = root.Get("entities")[0].Get("components");
                    original.Set("automation_probe", Json.Parse("{\"level\":6,\"operation\":{\"id\":\"probe\",\"enabled\":true,\"target\":1,\"minimum\":0,\"maximum\":10,\"unit\":\"unit.work-rate\"}}"));
                    if (mode.Contains(".named")) original.Set("inventory_port", Json.Parse("{\"id\":\"stock\"}"));
                    if (mode.StartsWith("stock") || mode.StartsWith("reserve"))
                        original.Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":" + (mode.Contains(".named") ? "\"stock\"" : "null") + ",\"profile\":\"replacement.policy\"}]}"));
                    var next = original.Clone(); root.Get("entities")[3].Set("components", next);
                    root.Get("entities")[1].Get("components").Set("automation_probe", original.Get("automation_probe").Clone());
                    if (refusal == "input" || refusal == "target") next.Remove("automation_probe");
                    if (refusal == "unit") next.Get("automation_probe").Get("operation").Set("unit", JsonValue.Of("unit.boolean"));
                    if (refusal == "bounds") next.Get("automation_probe").Get("operation").Set("maximum", JsonValue.Number("6"));
                    if (refusal == "policy") next.Remove("logistics_policy");
                    if (binding != null)
                    {
                        var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of("proof.pools")); scenario.Get("modules").Add(JsonValue.Of("proof.output-ports"));
                        var donor = root.Get("entities")[0].Clone(); donor.Set("id", JsonValue.Of("donor"));
                        donor.Get("components").Remove("logistics_policy"); donor.Get("components").Remove("inventory_port"); donor.Get("components").Remove("automation_probe");
                        root.Get("entities").Add(donor); StartupContentAuthoring.Add(root, "donor", "donor", 0, Json.Parse("{\"space\":\"field.points\",\"position\":0}"));
                        original.Get("point_stock").Set("initial", JsonValue.Number("0")); original.Get("point_stock").Set("public", JsonValue.Of(false));
                        if (binding != "fresh")
                        {
                            original.Remove("inventory_port");
                            original.Set("pool_port", Json.Parse("""{"id":"stock","pool":"replacement.receiver","alternatives":["replacement.next"],"endpoint_scope":"proof.pools","original_access":"retained"}"""));
                            if (binding == "fixed") original.Get("pool_port").Set("alternatives", Json.Parse("[]"));
                        }
                        next = original.Clone(); root.Get("entities")[3].Set("components", next);
                        if (binding == "fresh")
                        {
                            next.Remove("inventory_port"); next.Set("pool_port", Json.Parse("""{"id":"stock","pool":"replacement.next","alternatives":["replacement.receiver"],"endpoint_scope":"proof.pools","original_access":"retained"}"""));
                        }
                        if (binding == "pinned")
                        { next.Get("pool_port").Set("pool", JsonValue.Of("replacement.next")); next.Get("pool_port").Set("alternatives", Json.Parse("[\"replacement.receiver\"]")); }
                        if (binding == "removed")
                        { next.Get("pool_port").Set("pool", JsonValue.Of("replacement.next")); next.Get("pool_port").Set("alternatives", Json.Parse("[]")); }
                        if (binding == "denied") next.Get("pool_port").Set("original_access", JsonValue.Of("current_only"));
                        if (binding == "late") next.Get("pool_port").Set("original_access", JsonValue.Of("current_only"));
                        var parking = root.Get("entities")[1].Clone(); parking.Set("id", JsonValue.Of("parking")); root.Get("entities").Add(parking);
                        StartupContentAuthoring.Add(root, "parking", "parking", 0, Json.Parse("{\"space\":\"field.points\",\"position\":70}"));
                    }
                });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 31, Seats = new List<Controller> { Controller.Human, Controller.Human }, Mods = { ResourceFlowsModule.ModuleId } });
        private static EntityId Find(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Id;
        private static AutomationRuleSnapshot Read(SimGame game)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetAutomationRule(new PlayerId(0), "field", out var result))
                throw new InvalidOperationException("missing actual automation rule");
            return result;
        }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("automation replacement fault: " + game.Fault + "/" + twin?.Fault); }
        private static void Send(Check c, SimGame game, Command command, SimGame twin = null)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "public player admission");
            int sequence = game.Seat(0).Submit(command); twin?.Seat(0).Submit(command); Step(game, twin);
            long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Any(a => a.Sequence == sequence && a.Accepted), "engine inbox commits intent");
        }
        private static void Configure(Check c, SimGame game, string mode, bool enabled = true, string refusal = null, ResourceEndpointId? endpoint = null)
        {
            var old = Find(game, "source");
            c.That(game.Seat(0).View.Feature<OperationsView>().TryGetAutomationProfile("replacement.rule", out var profile), "engine supplies complete authored settings");
            var bands = profile.Thresholds.ToDictionary(p => p.Key, p => p.Value.Initial);
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("missing public resource");
            Code port = mode.Contains(".named") ? new Code("stock") : Code.None;
            var resources = new List<ResourceInfo>(); game.Seat(0).View.CopyResources(resources);
            Code target = mode.StartsWith("stock") ? LogisticsPolicyRules.StockPolicyBinding(resources.Single(r => r.Resource == resource), port, endpoint ?? ResourceEndpointId.ForEntity(old))
                : mode.StartsWith("reserve") ? LogisticsPolicyRules.OutletPolicyTarget(port) : new Code("probe");
            var settings = new AutomationSettings(profile.Id, enabled, new ControlBinding(refusal == "input" ? Find(game, "destination") : old, target),
                new Dictionary<string, ControlBinding> { ["level"] = new ControlBinding(refusal == "target" ? Find(game, "destination") : old, new Code("probe")) },
                bands, profile.WhenTrue, profile.WhenFalse);
            Send(c, game, new ConfigureAutomationCommand { Rule = "field", Settings = settings });
        }
        private static void Physical(Check c, string mode, int dwell, ref bool pendingSeen, string state = "normal")
        {
            var factory = Factory(mode, dwell); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
            Configure(c, game, mode, state != "disabled"); var original = Read(game); var old = Find(game, "source");
            if (state == "waiting") Send(c, game, new SetAutomationProbeCommand { Entity = old, Level = Fix.FromInt(6), Available = false });
            if (dwell == 0 && state == "normal") Step(game); var before = Read(game);
            Send(c, game, new PostOrderCommand { Plan = "upgrade", Target = old }); var twin = factory.Load(game.Save());
            for (int i = 0; i < 25 && !game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next"); i++) Step(game, twin);
            var made = Find(game, "source.next"); var current = Read(game); pendingSeen |= current.PendingDecision.HasValue;
            c.Equal(made, current.Settings.Target.Entity, "action target rebinds to actual replacement");
            c.Equal(made, current.Settings.Inputs["level"].Entity, "all matching signal inputs rebind");
            c.Equal(old, original.Settings.Target.Entity, "prior immutable settings do not change");
            c.Equal(before.Settings.Profile, current.Settings.Profile, "profile remains original");
            c.Equal(before.Settings.Thresholds["high"], current.Settings.Thresholds["high"], "player hysteresis bands remain original");
            if (state == "normal" && dwell == 60) c.Equal(before.CandidateSince, current.CandidateSince, "replacement does not restart dwell");
            if (before.HasApplied)
            { c.Equal(before.LastAppliedAt, current.LastAppliedAt, "applied chronology retained"); c.Equal(before.CooldownUntil, current.CooldownUntil, "cooldown retained"); c.Equal(before.LastAppliedAction, current.LastAppliedAction, "last action retained"); }
            if (state == "waiting") c.That(current.WaitingForInputs && current.WaitingInput == "level" && current.Blocker == before.Blocker, "input recovery state retained");
            if (state == "disabled") c.That(!current.Settings.Enabled && !current.HasApplied && !current.PendingDecision.HasValue, "disabled intent rebinds without enabling or decisions");
            c.Equal(game.Hash(), twin.Hash(), "saved physical replacement continues identically");
            game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            if (state == "waiting") Send(c, game, new SetAutomationProbeCommand { Entity = made, Level = Fix.FromInt(6), Available = true }, twin);
            if (state != "disabled")
            {
                for (int i = 0; i < 90 && !Read(game).HasApplied; i++) Step(game, twin);
                var delivered = Read(game); c.That(delivered.HasApplied && delivered.Latch && delivered.Blocker.IsNone, "normal evaluator delivers existing true action");
                if (state == "normal") c.Equal(original.PendingDecision.HasValue ? original.PendingAt : original.CandidateSince + dwell + 1, delivered.LastAppliedAt, "original dwell and next-update delivery deadline retained");
                if (mode.StartsWith("stock"))
                {
                    game.Seat(0).View.TryFindResource("supply", out var resource); Code port = mode.Contains(".named") ? new Code("stock") : Code.None;
                    c.Equal(Code.None, game.Seat(0).View.Feature<LogisticsView>().ReadStockPolicy(made, resource, out var policy, port, ResourceEndpointId.ForEntity(made)), "stock policy follows native endpoint");
                    c.Equal(Fix.FromInt(7), policy.Desired, "normal stock command updates retained policy");
                    if (!port.IsNone) c.That(delivered.Settings.Target.Capability != original.Settings.Target.Capability, "named stock capability rewrites actual endpoint identity");
                }
                else if (mode.StartsWith("reserve"))
                {
                    game.Seat(0).View.TryFindResource("supply", out var resource); Code port = mode.Contains(".named") ? new Code("stock") : Code.None;
                    c.Equal(Code.None, game.Seat(0).View.Feature<LogisticsView>().ReadOutletPolicy(made, out var policy, port), "outlet policy follows actual port");
                    c.Equal(Fix.FromInt(7), policy.Reserve.Lines.Single(r => r.Resource == resource).Amount, "normal reserve command updates retained intent");
                }
                else c.That(game.Seat(0).View.Feature<OperationsView>().TryGetControl(made, new Code("probe"), out var operation) && operation.Target == Fix.FromInt(7), "normal operation command targets replacement");
            }
            c.Equal(game.Hash(), twin.Hash(), "saved control continuation is deterministic");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "control/reference state loads coherently");
            c.Equal(isolatedHash, isolated.Hash(), "independent session remains isolated");
            c.That(!game.Seat(1).View.Feature<OperationsView>().TryGetAutomationRule(new PlayerId(0), "field", out _), "other company cannot inspect rule bindings");
        }
        private static void Run(Check c)
        {
            bool pendingSeen = false;
            foreach (var mode in new[] { "operation", "delegate", "stock", "stock.named", "reserve", "reserve.named" })
            { Physical(c, mode, 60, ref pendingSeen); Physical(c, mode, 0, ref pendingSeen); }
            for (int dwell = 1; dwell <= 8; dwell++) Physical(c, "operation", dwell, ref pendingSeen);
            c.That(pendingSeen, "swept authored dwell boundaries include a pending next-update decision during actual replacement");
            Physical(c, "operation", 60, ref pendingSeen, "waiting"); Physical(c, "operation", 60, ref pendingSeen, "disabled");
            foreach (var refusal in new[] { "input", "target", "unit", "bounds", "policy" })
            {
                string mode = refusal == "policy" ? "reserve" : "operation"; var factory = Factory(mode, 60, refusal); var game = New(factory);
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true }); Configure(c, game, mode, refusal: refusal);
                Code expected = refusal == "input" ? AutomationCodes.ReplacementBinding : refusal == "target" ? OperationCodes.NotControllable :
                    refusal == "unit" ? AutomationCodes.UnitMismatch : refusal == "bounds" ? OperationCodes.TargetBounds : LogisticsPolicyCodes.NoPolicy;
                var command = new PostOrderCommand { Plan = "upgrade", Target = Find(game, "source") }; ulong before = game.Hash();
                c.Equal(expected, game.Seat(0).Preview(command), "incompatible typed reference refuses");
                c.Equal(before, game.Hash(), "replacement admission is pure");
                int sequence = game.Seat(0).Submit(command); Step(game); long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
                c.That(answers.Any(a => a.Sequence == sequence && a.Refusal == expected), "inbox agrees with preview refusal");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused rules remain saved coherently");
            }
            foreach (var mode in new[] { "deny", "mutate-check", "redirect" })
            {
                var factory = Factory("delegate", 60, mode); var game = New(factory);
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true }); Configure(c, game, "delegate");
                var command = new PostOrderCommand { Plan = "upgrade", Target = Find(game, "source") };
                if (mode == "deny")
                {
                    ulong hash = game.Hash(); Code refusal = new Code("proof.control-replacement-denied");
                    c.Equal(refusal, game.Seat(0).Preview(command), "independent action policy can refuse replacement");
                    c.Equal(hash, game.Hash(), "independent refusal is pure");
                    int sequence = game.Seat(0).Submit(command); Step(game); long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
                    c.That(answers.Any(a => a.Sequence == sequence && a.Refusal == refusal), "independent policy refusal has preview/inbox parity");
                    c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "independent refused state remains coherent");
                }
                else
                {
                    if (mode == "mutate-check") c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(command), "impure independent metadata check faults");
                    else
                    {
                        Send(c, game, command);
                        for (int i = 0; i < 25 && game.Fault == null; i++) game.Update();
                    }
                    c.That(game.Fault != null, "invalid replacement action faults original boundary");
                    c.Throws<InvalidOperationException>(() => game.Save(), "no healthy save follows action replacement violation");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy checkpoint follows action replacement violation");
                }
            }
        }
        private static void Independent(Check c)
        {
            foreach (string binding in new[] { "fixed", "pinned", "fresh", "removed", "denied", "late" }) Independent(c, binding);
        }
        private static void Independent(Check c, string binding)
        {
            var factory = Factory("stock.named", 60, binding: binding); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            var old = Find(game, "source"); var worker = Find(game, "machine"); var port = new Code("stock");
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("automation supply missing");
            Send(c, game, new SetWorkerHeldCommand { Worker = worker, Held = true });
            var originalEndpoint = binding == "fresh" ? ResourceEndpointId.ForEntity(old) : new ResourceEndpointId("proof.pools", 1);
            Configure(c, game, "stock.named", endpoint: originalEndpoint);
            if (binding == "pinned" || binding == "denied") Send(c, game, new SelectResourcePortCommand { Entity = old, Port = port, Endpoint = new ResourceEndpointId("proof.pools", 2) });
            var before = Read(game); var upgrade = new PostOrderCommand { Target = old, Plan = "upgrade" }; ulong prior = game.Hash();
            Code expected = binding == "removed" || binding == "denied" ? ResourcePortCodes.OriginalUnavailable : Code.None;
            Code admitted = game.Seat(0).Preview(upgrade);
            c.Equal(expected, admitted, "typed automation stock target forecasts declared backing " + binding);
            c.Equal(prior, game.Hash(), "automation stock replacement preview is pure " + binding);
            if (!expected.IsNone)
            {
                int sequence = game.Seat(0).Submit(upgrade); Step(game); long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.That(results.Any(result => result.Sequence == sequence && result.Refusal == expected), "automation stock replacement refusal agrees in inbox " + binding);
                var retained = Read(game);
                c.That(retained.Settings.Target.Entity == old && retained.Settings.Target.Capability == before.Settings.Target.Capability && retained.CandidateSince == before.CandidateSince,
                    "refused stock target never redirects capability or restarts rule " + binding);
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused target/decision checkpoint is coherent " + binding);
                return;
            }
            if (!admitted.IsNone) return; // Retain the real unsupported baseline, not an invented successful replacement.
            if (binding == "late")
            {
                // Explicit negative protocol lens, not a positive player shortcut: actual spawn loses admitted original access.
                var world = game.World; var source = world.Entities.Get(old); var next = world.Entities.NextIdentity;
                Entity candidate = null, rejected = null; Exception caught = null;
                ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) => {
                    if (phase != "spawn" || entity?.Def.Id != "source.next") return;
                    candidate = entity; entity.Get<IResourcePortSelection>(port).Select(world, new ResourceEndpointId("proof.pools", 2));
                };
                try { Upgrades.TryReplace(world, source, world.Content.Get<EntityDef>("source.next"), out rejected); }
                catch (Exception error) { caught = error; }
                c.That(caught != null && game.Fault?.Phase == "observation.provider" && game.Fault.Message.Contains("replacement stock target binding violated admission: " + ResourcePortCodes.OriginalUnavailable.Id, StringComparison.Ordinal),
                    "actual target binding fault retains first action-policy/provider cause");
                c.That(rejected == null && source.Alive && candidate?.Alive == true && ReferenceEquals(world.Entities.Get(next), candidate) && world.Entities.NextIdentity.Value == next.Value + 1,
                    "late automation target failure retains actual interrupted registration, not fabricated rollback");
                c.That(!world.Events.HasPending && world.Services.Get<EndpointPools>().Get(1).Amount(resource).IsZero && world.Services.Get<EndpointPools>().Get(2).Amount(resource).IsZero,
                    "failed action rebind publishes no replacement success or stock");
                c.Throws<InvalidOperationException>(() => game.Save(), "invalid actual stock target cannot save healthy");
                c.Throws<InvalidOperationException>(() => game.Hash(), "invalid actual stock target cannot hash healthy");
                c.Equal(isolatedHash, isolated.Hash(), "failed target binding leaves another session unchanged");
                return;
            }
            Send(c, game, upgrade); var twin = factory.Load(game.Save());
            Fix Pool(SimGame session, long id) => session.Seat(0).View.Feature<EndpointPoolDiagnostics>().Stock(id).AmountOf(resource);
            void Observe()
            {
                Fix native = Fix.Zero;
                foreach (var entity in game.Seat(0).Snapshot.Entities)
                    if (game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, resource, out var facts).IsNone) native += facts.Stock;
                c.Equal(Fix.FromInt(20), native + Pool(game, 1) + Pool(game, 2), "actual finite donor/cargo/native/pool owners conserve twenty " + binding);
                c.Equal(game.Hash(), twin.Hash(), "saved target and physical refill sessions agree each update " + binding);
            }
            void Reach(Func<bool> ready, int maximum, string stage)
            {
                for (int i = 0; i < maximum && !ready(); i++) { Step(game, twin); Observe(); }
                if (!ready()) throw new InvalidOperationException(stage + "/" + binding + " exceeded " + maximum + " updates at " + game.Tick + ": " + Read(game).Blocker);
            }
            Reach(() => game.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "source.next"), 25, "physical automation stock upgrade");
            var made = Find(game, "source.next"); var rebound = Read(game);
            var expectedEndpoint = new ResourceEndpointId("proof.pools", binding == "fresh" ? 2 : 1);
            var resources = new List<ResourceInfo>(); game.Seat(0).View.CopyResources(resources);
            c.That(rebound.Settings.Target.Entity == made && rebound.Settings.Target.Capability == LogisticsPolicyRules.StockPolicyBinding(resources.Single(info => info.Resource == resource), port, expectedEndpoint) &&
                rebound.Settings.Inputs["level"].Entity == made && rebound.CandidateSince == before.CandidateSince && !rebound.HasApplied,
                "actual resource target and input rebind while original dwell stays intact " + binding);
            c.That(before.Settings.Target.Entity == old && ((before.Settings.Target.Capability != rebound.Settings.Target.Capability) == (binding == "fresh")), "detached old target stays exact and only changed backing rewrites its typed endpoint " + binding);
            game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            Send(c, game, new SetHomeCommand { Worker = Find(game, "crew"), Home = Find(game, "parking") }, twin);
            Reach(() => Read(game).PendingDecision == true, 90, "rule stages its original next-update decision");
            var pending = Read(game); game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            c.That(Read(game).PendingDecision == true && Read(game).PendingAt == pending.PendingAt && pending.PendingAt == before.CandidateSince + 61 &&
                !Read(game).Candidate.HasValue && Read(game).CandidateSince == pending.CandidateSince && pending.CandidateSince == -1,
                "saved pending decision keeps its original deadline and rebound typed endpoint " + binding);
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            using (var pendingRecorder = game.RecordReplay(limits))
            {
                Step(game, twin); Observe();
                c.That(Read(game).HasApplied && !Read(game).PendingDecision.HasValue && Read(game).LastAppliedAt == pending.PendingAt,
                    "saved pending action is applied once at the original next-update deadline " + binding);
                c.Equal(0L, pendingRecorder.CommandCount, "saved pending control applies without player resubmission " + binding);
                var verified = new ReplayVerifier().Verify(factory, pendingRecorder.Finish(), limits);
                c.That(verified.Success && verified.ActualHash == game.Hash(), "actual pending independent control replay agrees " + binding);
            }
            var applied = Read(game);
            c.Equal(before.CandidateSince + 61, applied.LastAppliedAt, "original sixty-minute dwell and next-step delivery deadline survive replacement " + binding);
            c.Equal(Code.None, game.Seat(0).View.Feature<LogisticsView>().ReadStockPolicy(made, resource, out var policy, port, expectedEndpoint), "rule addresses actual retained stock policy " + binding);
            c.That(policy.Enabled && policy.Minimum == Fix.FromInt(5) && policy.Desired == Fix.FromInt(7) && policy.Maximum == Fix.FromInt(30), "normal action edits desired only, preserving player bands " + binding);
            Reach(() => game.Seat(0).View.Feature<WorkView>().ReadHome(Find(game, "crew"), out var home).IsNone && home.State == WorkerHomeState.AtHome, 180, "crew physically clears refill route");
            Send(c, game, new SetWorkerHeldCommand { Worker = worker, Held = false }, twin);
            bool Loaded()
            {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                var at = (PointSnapshot)game.Seat(0).Snapshot.Entities.Single(entity => entity.Id == worker).Spatial;
                return jobs.Any(job => job.IsHaul && job.Route == 0 && job.DestinationEndpoint == expectedEndpoint && job.Loaded == Fix.FromInt(7) && job.ToilIndex == 2) && at.Position > Fix.One && at.Position < Fix.FromInt(19);
            }
            Reach(Loaded, 180, "automatic rule-driven refill physically travels loaded");
            game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            using (var recorder = game.RecordReplay(limits))
            {
                Reach(() => Pool(game, expectedEndpoint.Identity) == Fix.FromInt(7), 180, "original rule-driven refill delivers seven");
                c.Equal(0L, recorder.CommandCount, "loaded control/refill continuation resubmits no action or shipment " + binding);
                var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
                c.That(verified.Success && verified.ActualHash == game.Hash(), "actual saved loaded independent automation refill replays identically " + binding);
            }
            c.That(Pool(game, binding == "fresh" ? 1 : 2).IsZero, "non-target pool receives no redirected stock " + binding);
            c.That(Read(game).LastAppliedAt == applied.LastAppliedAt && Read(game).CooldownUntil == applied.CooldownUntil,
                "loaded refill never reapplies an action or restarts cooldown " + binding);
            c.Equal(isolatedHash, isolated.Hash(), "control target replacement is session isolated " + binding);
            c.That(!game.Seat(1).View.Feature<OperationsView>().TryGetAutomationRule(new PlayerId(0), "field", out _), "independent control target stays private " + binding);
        }
    }
}
