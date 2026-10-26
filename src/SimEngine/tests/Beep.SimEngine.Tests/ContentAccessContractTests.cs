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
using static Beep.SimEngine.Tests.TileConditionContractTests;

namespace Beep.SimEngine.Tests
{
    internal static class ContentAccessContractTests
    {
        internal const string Case = "Content access contracts: authored policy governs physical work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static JsonValue Read(string name) => Json.Parse(FrameworkProofContent.Read(name));
        private const string Declaration = "{\"id\":\"proof.access-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"}],\"documents\":[\"access-contracts.json\"]}";
        private static JsonValue Authored(string entity, bool independent)
            => Json.Parse("{\"restricted_access\":" + (independent ? "[{\"id\":\"proof.restricted\",\"company\":0,\"denied_entities\":[]}]" : "[]") +
                ",\"proof_access_rules\":[{\"id\":\"proof.access.b\",\"entity\":\"" + entity + "\",\"enabled\":true}," +
                "{\"id\":\"proof.access.a\",\"entity\":\"" + entity + "\",\"enabled\":true}]}");
        internal static GameFactory Create(ContentAccessContractModule module, bool independent = true,
            Action<JsonValue, JsonValue> edit = null,Action<JsonValue> worldEdit=null)
        {
            var world = Read("colony.json"); var authored = Authored("tower", independent);
            var access = independent ? JsonValue.NewObject() : Read("open-access.json");
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
            worldEdit?.Invoke(world);
            edit?.Invoke(authored, access);
            return Colony.Factory(definitions: world.ToString(), additionalModules: new GameModule[] { module },
                moduleDeclarations: new[] { Declaration }, moduleDocuments: new Dictionary<string, string> {
                    ["framework/access.json"] = access.ToString(), ["access-contracts.json"] = authored.ToString() });
        }
        private static void Configure(Check c, SimGame game, string rule, bool enabled, SimGame twin = null)
        {
            var command = new ConfigureAccessRuleCommand { Rule = rule, Enabled = enabled };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary access configuration preview");
            c.Equal(hash, game.Hash(), "configuration preview leaves actual rule codec unchanged");
            c.That(TestKit.Order(game, 0, command).Accepted, "player configures original declared rule service");
            if (twin != null)
            {
                c.That(TestKit.Order(twin, 0, new ConfigureAccessRuleCommand { Rule = rule, Enabled = enabled }).Accepted, "saved twin configures same owner");
                c.Equal(game.Hash(), twin.Hash(), "saved rule state and command continuation identical");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current authored access build identity");
            c.Equal(154, EngineInfo.SaveFormat, "no new engine state schema or clock");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            AuthoredRefusals(c); Technology(c); Invalid(c); Faults(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new ContentAccessContractModule(); var factory = Create(module, independent);
            var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            c.That(module.ActualAlias && module.DirectDefinition && module.RuleAliases, "actual native/independent content and single rule codec aliases");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical colony never activates business");
            c.Equal(0L, game.Clock.StepIndex, "composition does not advance game time");
            module.Trace.Clear(); ulong hash = game.Hash();
            c.Equal(new Code("proof.access.a.denied"), game.Seat(0).Preview(Build(10, 16)), "stable ID beats reversed service composition order");
            c.That(module.Trace.SequenceEqual(new[] { "proof.access.a" }), "refused first rule does not evaluate later rule");
            c.Equal(hash, game.Hash(), "rule admission uses its actual pure state");
            c.Equal(new Code("proof.access.a.denied"), TestKit.Order(game, 0, Build(10, 16)).Refusal, "typed preview and command refusal match");
            c.That(!Mine(game, "site:tower").Any() && !Mine(game, "tower").Any(), "refused build creates no work/completed structure");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "enabled service aliases persist exactly once");
            Configure(c, game, "proof.access.a", false, twin);
            module.Trace.Clear();
            c.Equal(new Code("proof.access.b.denied"), game.Seat(0).Preview(Build(10, 16)), "second declared rule retains own restriction");
            c.That(module.Trace.SequenceEqual(new[] { "proof.access.a", "proof.access.b" }), "stable captured access order");
            var limits = new ReplayLimits(4 * 1024 * 1024, 5000);
            using (var recorder = game.RecordReplay(limits))
            {
                Configure(c, game, "proof.access.b", false, twin);
                c.Equal(Code.None, game.Seat(0).Preview(Build(10, 16)), "policy admits normal project after player changes restrictions");
                c.That(TestKit.Order(game, 0, Build(10, 16)).Accepted, "ordinary build accepted");
                c.That(Mine(game, "site:tower").Any() && !Mine(game, "tower").Any(), "accepted access never constructs instantly");
                Reach(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "access-gated loaded hauling");
                c.That(Mine(game).Any(e => e.State == new Code("state.moving")) && game.Clock.StepIndex > 1, "machines still move over game time");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial project and disabled restrictions save together");
                long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "one paused clock halts physical execution");
                game.Clock.Resume(); twin.Clock.Resume();
                Reach(game, () => Mine(game, "tower").Length == 1, "access-gated physical construction", twin);
                c.Equal(Fix.FromInt(20), Steel(game), "real tower consumes exact30 materials from50 opening stock");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "finished work and rule state checkpoint");
                c.Equal(idleHash, idle.Hash(), "new interleaved session retains separate enabled restrictions");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary configuration/build stream replays physical work");
            }
        }
        private static void AuthoredRefusals(Check c)
        {
            var module = new ContentAccessContractModule();
            var denied = New(Create(module, edit: (authored, access) =>
                authored.Get("restricted_access")[0].Set("denied_entities", Json.Parse("[\"tower\"]"))));
            module.Trace.Clear(); ulong before = denied.Hash();
            c.Equal(new Code("proof.access.basis"), denied.Seat(0).Preview(Build(10, 16)), "independent JSON basis restriction precedes rule services");
            c.Equal(0, module.Trace.Count, "basis refusal does not invoke extensions");
            c.Equal(before, denied.Hash(), "authored basis read is pure");
            c.Equal(new Code("proof.access.basis"), TestKit.Order(denied, 0, Build(10, 16)).Refusal, "authored policy command parity");
            var changed = New(Create(new ContentAccessContractModule(), edit: (authored, access) => {
                for (int i = 0; i < authored.Get("proof_access_rules").Count; i++)
                    authored.Get("proof_access_rules")[i].Set("enabled", JsonValue.Of(false));
            }));
            c.Equal(Code.None, changed.Seat(0).Preview(Build(10, 16)), "new JSON configuration changes actual admission");
            c.Equal(new Code("proof.access.basis"), denied.Seat(0).Preview(Build(10, 16)), "previous frozen policy remains unchanged");
            c.Equal(Codes.NotFound, changed.Seat(0).Preview(new ConfigureAccessRuleCommand { Rule = "missing", Enabled = true }), "no substituted rule");
            c.Equal(Codes.NotAllowed, changed.Seat(1).Preview(new ConfigureAccessRuleCommand { Rule = "proof.access.a", Enabled = false }), "foreign company cannot configure restrictions");
        }
        private static void Technology(Check c)
        {
            var module = new ContentAccessContractModule(); var authored = Authored("point.tower", true);
            var factory = ConstructionPlanTests.Points(additionalModule: new TechnologyModule(),
                moduleDeclaration: "{\"id\":\"sim.technology\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"technology.json\"]}",
                moduleDocument: FrameworkProofContent.Read("technology.json"), extraModules: new GameModule[] { module },
                extraDeclarations: "[" + Declaration + "]", extraDocuments: new Dictionary<string, string> {
                    ["framework/access.json"] = "{}", ["access-contracts.json"] = authored.ToString() },
                scenarioEdit: scenario => { scenario.Get("modules").Add(JsonValue.Of(TechnologyModule.ModuleId)); scenario.Get("modules").Add(JsonValue.Of(module.Id)); });
            var game = ConstructionPlanTests.NewPoints(factory);
            EntityId Find(string id) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == id && e.Owner == new PlayerId(0)).Id;
            var build = new ConstructCommand { Def = "point.tower", Priority = 20, Placement = Json.Parse("{\"space\":\"field.points\",\"position\":12}") };
            Configure(c, game, "proof.access.a", false); Configure(c, game, "proof.access.b", false);
            c.Equal(TechnologyCodes.Locked, game.Seat(0).Preview(build), "same aggregate preserves actual technology gate");
            c.Equal(TechnologyCodes.Locked, TestKit.Order(game, 0, build).Refusal, "normal locked research/build refusal parity");
            var policy = new ConfigureWorkPolicyCommand { Worker = Find("machine"), Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) };
            c.That(TestKit.Order(game, 0, policy).Accepted, "ordinary player enables real research machine");
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "research.foundation", Target = Find("source"), Priority = 70 });
            c.That(result.Accepted, "ordinary authored research admitted through same policy");
            var nodes = new List<ActivityNodeView>();
            bool Partial()
            {
                game.Seat(0).View.Feature<WorkView>().TryCopyNodes(checked((int)result.Arg0), nodes);
                return nodes.Any(n => n.Id == "research" && n.Progress.WholeTicks > 0 && n.State != NodeState.Done);
            }
            Reach(game, Partial, "partial actual research");
            c.Equal(TechnologyCodes.Locked, game.Seat(0).Preview(build), "partial physical research cannot unlock early");
            var twin = factory.Load(game.Save());
            Reach(game, () => game.Seat(0).Preview(build).IsNone, "physical research unlock", twin);
            c.Equal(game.Hash(), twin.Hash(), "shared learned-state continuation remains deterministic");
            c.That(game.Clock.StepIndex > 1, "research consumes one real game clock");
            var foreign = new List<TechnologySnapshot>(); game.Seat(1).View.Feature<TechnologyView>().CopyTechnologies(foreign);
            c.That(foreign.All(f => !f.Known), "access policy never leaks or grants another company's technology");
            Configure(c, game, "proof.access.b", true);
            c.Equal(new Code("proof.access.b.denied"), game.Seat(0).Preview(build), "ordinary rule remains effective after genuine technology unlock");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "learned ledger and actual rule codecs save without duplicate research state");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-policy", "two-policies", "duplicate-policy", "missing-array", "null-array", "unknown-entity",
                "duplicate-entity", "negative-company", "unknown-key", "duplicate-rule", "foreign-rule", "invalid-rule", "missing-rule", "stateful-policy" })
            {
                var module = new ContentAccessContractModule();
                c.Throws<ContentException>(() => New(Create(module, edit: (authored, access) => {
                    var policy = authored.Get("restricted_access")[0]; var rules = authored.Get("proof_access_rules");
                    if (mode == "missing-policy") authored.Set("restricted_access", JsonValue.NewArray());
                    if (mode == "two-policies") access.Set("open_content_access", Read("open-access.json").Get("open_content_access"));
                    if (mode == "duplicate-policy") authored.Get("restricted_access").Add(policy.Clone());
                    if (mode == "missing-array") policy.Remove("denied_entities");
                    if (mode == "null-array") policy.Set("denied_entities", JsonValue.Null());
                    if (mode == "unknown-entity") policy.Set("denied_entities", Json.Parse("[\"missing\"]"));
                    if (mode == "duplicate-entity") policy.Set("denied_entities", Json.Parse("[\"tower\",\"tower\"]"));
                    if (mode == "negative-company") policy.Set("company", JsonValue.Number("-1"));
                    if (mode == "unknown-key") policy.Set("unknown", JsonValue.Of(true));
                    if (mode == "duplicate-rule") for (int i = 0; i < rules.Count; i++) rules[i].Set("metadata_fault", JsonValue.Of("duplicate"));
                    if (mode == "foreign-rule") rules[0].Set("metadata_fault", JsonValue.Of("foreign"));
                    if (mode == "invalid-rule") rules[0].Set("metadata_fault", JsonValue.Of("invalid"));
                    if (mode == "missing-rule") authored.Set("proof_access_rules", JsonValue.NewArray());
                    if (mode == "stateful-policy")
                    {
                        authored.Set("restricted_access", JsonValue.NewArray());
                        authored.Set("stateful_access", Json.Parse("[{\"id\":\"bad.stateful-policy\"}]"));
                    }
                })), "malformed authored access fails before Initialize: " + mode);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "no initialized partial composition: " + mode);
            }
            foreach (AccessProbeFault fault in new[] { AccessProbeFault.Exception, AccessProbeFault.PauseClock,
                AccessProbeFault.LayerWrite, AccessProbeFault.RuleState })
            {
                var module = new ContentAccessContractModule(); module.Arm(AccessProbeTarget.RuleMetadata, fault);
                c.Throws<InvalidOperationException>(() => New(Create(module)), "composition metadata observation fails before Initialize: " + fault);
                c.That(module.Reads == 1 && module.MutationCompleted == (fault != AccessProbeFault.Exception),
                    "actual declared metadata provider mutation is observed: " + fault);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "no Initialize after metadata fault: " + fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach (AccessProbeTarget target in Enum.GetValues(typeof(AccessProbeTarget)))
                foreach (AccessProbeFault fault in Enum.GetValues(typeof(AccessProbeFault)))
                {
                    if (target == AccessProbeTarget.Basis && (fault == AccessProbeFault.RuleState || fault == AccessProbeFault.ChangeId || fault == AccessProbeFault.ChangeWorld)) continue;
                    var module = new ContentAccessContractModule(); var game = New(Create(module));
                    module.Arm(target, fault); long at = game.Clock.StepIndex; Code? answer = null;
                    c.Throws<InvalidOperationException>(() => answer = game.Seat(0).Preview(Build(10, 16)), "actual access observation fault " + target + "/" + fault);
                    c.That(!answer.HasValue && module.Reads == 1 && game.Fault?.Phase == "observation.provider", "first fault withholds access result");
                    c.That(module.MutationCompleted == (fault != AccessProbeFault.Exception && fault != AccessProbeFault.Recurse), "actual mutation or swallowed failure reached before guard refuses");
                    c.That(game.Update() == 0 && game.Clock.StepIndex == at, "faulted policy cannot advance game time");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy faulted access hash");
                    c.Throws<InvalidOperationException>(() => game.Save(), "no faulted access checkpoint");
                }
        }
    }
}
