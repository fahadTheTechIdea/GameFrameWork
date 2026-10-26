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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Commands, authored definitions, immutable engine views and clock only.
    internal static class WorkEffectContractTests
    {
        internal const string Case = "Work effect contracts: earned research and scoped refused completion";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(WorkEffectContractsModule module, bool independent = true, bool utility = false, Action<JsonValue> edit = null,
            IReadOnlyDictionary<string,string> additionalDocuments=null,Action<JsonValue> worldEdit=null)
        {
            var effects = Json.Parse(FrameworkProofContent.Read("work-effect-contracts.json"));
            effects.Get("independent_research_effects")[0].Set("consumer", utility ? JsonValue.Of("contract.process") : JsonValue.Null());
            if (!independent) effects.Set("independent_research_effects", JsonValue.NewArray());
            edit?.Invoke(effects);
            var retry = Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Set("work_retry_profiles", Json.Parse(FrameworkProofContent.Read("completion-retry.json")).Get("work_retry_profiles").Clone());
            retry.Get("work_retry_profiles")[0].Get("retry_blockers").Add(JsonValue.Of("utility.inactive"));
            retry.Get("work_retry_profiles")[0].Get("retry_blockers").Add(JsonValue.Of("utility.shortage"));
            var declaration=Json.Parse("{\"id\":\"proof.work-effect-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.technology\",\"version\":\"1.0.0\"}],\"documents\":[\"work-effect-contracts.json\"]}");
            var documents=new Dictionary<string,string>{["work-effect-contracts.json"]=effects.ToString()};
            if(additionalDocuments!=null)foreach(var entry in additionalDocuments){declaration.Get("documents").Add(JsonValue.Of(entry.Key));documents.Add(entry.Key,entry.Value);}
            return UtilityResearchContractTests.Factory(new UtilityResearchContractsModule(),
                extraModule: module,
                extraDeclaration: declaration.ToString(),
                extraDocuments: documents,
                configure: (tech, setup) => {
                    tech.Get("technologies")[0].Get("nodes")[1].Set("requirement", JsonValue.Of("preempt.ready"));
                    if (independent) tech.Set("technology_completion_effects", JsonValue.NewArray());
                    setup.Get("entities")[1].Get("components").Set("completion_probe", Json.Parse("{\"id\":\"completion.probe\"}"));
                }, retryProfiles: retry.ToString(),worldEdit:worldEdit);
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static TechnologySnapshot Knowledge(SimGame game, int seat = 0)
        {
            var list = new List<TechnologySnapshot>(); game.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(list);
            return list.Single(t => t.Technology == "research.foundation");
        }
        private static OrderView Work(SimGame game, int id)
        {
            var list = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(list);
            return list.Single(o => o.Id == id);
        }
        private static int Start(Check c, SimGame game)
        {
            Order(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, "lab.research-area", true, true, WorkAssignmentMode.All) });
            var posted = TestKit.Order(game, 0, new PostOrderCommand { Target = Entity(game, "contract.factory"), Plan = "research.foundation", Priority = 70 });
            c.That(posted.Accepted, "normal player orders actual material-consuming research"); return checked((int)posted.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "effect boundary actual build");
            c.Equal(154, EngineInfo.SaveFormat, "same native work/learned/target codecs");
            foreach (bool independent in new[] { false, true }) Physical(c, independent, false);
            Physical(c, true, true); Faults(c); Invalid(c);
        }
        private static void Physical(Check c, bool independent, bool utility)
        {
            var module = new WorkEffectContractsModule(); var factory = Factory(module, independent, utility);
            var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            c.That(module.ActualAlias && module.Independent == independent && typeof(TechnologyCompletionDef).BaseType == typeof(Def),
                "actual direct native/independent technology completion aliases");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && module.Initialize.Initializations > 0, "valid optional completion initializes without business");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recording = game.RecordReplay(limits))
            {
                int id = Start(c, game); var feature = game.Seat(0).View.Feature<WorkView>();
                c.Equal(WorkEffectCodes.NotComplete, feature.PreviewOrderEffect(id), "incomplete physical graph is not completion-ready");
                c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<WorkView>().PreviewOrderEffect(id), "foreign final-effect preview stays private");
                Until(game, null, () => Work(game, id).CurrentNeeded == 30 && Work(game, id).CurrentProgress.WholeTicks > 0, "partial actual research");
                c.That(!Knowledge(game).Known && Stock(game, "contract.factory") == Fix.FromInt(8), "partial work spends eight inputs without premature learning");
                var twin = factory.Load(game.Save()); long at = game.Clock.StepIndex;
                game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "same sole clock pauses earned physical research");
                game.Clock.Resume(); twin.Clock.Resume();
                if (utility)
                {
                    Until(game, twin, () => Work(game, id).State == OrderState.Completing && Work(game, id).CompletionFailures > 0, "actual pending final effect");
                    var retained = Work(game, id); ulong hash = game.Hash();
                    c.That(retained.NodesDone == retained.NodeCount && retained.Blocker == UtilityCodes.Inactive &&
                        !retained.CompletionEffectApplied && retained.CompletionRetryRemainingTicks > 0, "existing final retry retains earned graph without knowledge");
                    c.Equal(UtilityCodes.Inactive, feature.PreviewOrderEffect(id), "pure final preview exposes actual utility blocker");
                    c.Equal(hash, game.Hash(), "whole-order preview changes no actual codec or retry");
                    c.That(!Knowledge(game).Known && Stock(game, "contract.factory") == Fix.FromInt(8), "pending effect does not refund or double-spend work materials");
                    twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending final retry reloads original board");
                    Order(c, game, new EnqueueTransformationCommand { Transformer = Entity(game, "contract.factory"), Capability = new Code("contract.process"), Transformation = "contract.make" });
                    twin = factory.Load(game.Save());
                    Until(game, twin, () => Knowledge(game).Known, "ordinary processor start permits earned final effect");
                    c.That(retained.Blocker == UtilityCodes.Inactive && !retained.CompletionEffectApplied, "retained retry facts remain detached");
                }
                else Until(game, twin, () => Knowledge(game).Known, "native or independent earned completion");
                var learned = Knowledge(game);
                c.That(learned.CompletedTick.HasValue && learned.ActiveOrder == 0 && !Knowledge(game, 1).Known, "once-only learned record is company-private");
                c.Equal(TechnologyCodes.Known, game.Seat(0).View.Feature<TechnologyView>().PreviewResearch("research.foundation", Entity(game, "contract.factory"), 70),
                    "known research cannot run another completion grant");
                c.Equal(Code.None, game.Seat(0).Preview(new ConstructCommand { Def = "point.tower", Placement = Json.Parse("{\"space\":\"field.points\",\"position\":32}") }), "actual earned completion opens original build admission");
                for (int i = 0; i < 6; i++) Step(game, twin);
                c.Equal(learned.CompletedTick, Knowledge(game).CompletedTick, "later updates cannot repeat learned completion");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "earned final effect survives restore");
                c.Equal(isolatedHash, isolated.Hash(), "another session retains separate knowledge/work/stock");
                c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "normal completion and saved wait use same replay stream");
            }
        }
        private static void Faults(Check c)
        {
            foreach (RefusedEffectFault fault in Enum.GetValues(typeof(RefusedEffectFault)))
            {
                var module = new WorkEffectContractsModule(); var game = New(Factory(module)); module.Arm(fault); Start(c, game);
                long before = game.Clock.StepIndex; int steps = 0;
                while (game.Fault == null && steps++ < 120) { before = game.Clock.StepIndex; game.Update(); }
                c.That(game.Fault?.Phase == "work.effect" && module.Refusals == 1, "first actual refused callback faults " + fault);
                c.That(module.MutationCompleted == (fault != RefusedEffectFault.Exception), "real mutation occurred before refusal guard");
                c.That(game.Clock.StepIndex == before && game.Update() == 0, "refused mutation cannot finish/advance physical game time");
                c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy hash after refused effect fault");
                c.Throws<InvalidOperationException>(() => game.Save(), "no checkpoint after refused effect fault");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-consumer", "empty-consumer", "unknown-key", "stateful-unused", "wrong-completion-kind" })
            {
                var module = new WorkEffectContractsModule();
                c.Throws<ContentException>(() => New(Factory(module, edit: root => {
                    var effect = root.Get("independent_research_effects")[0];
                    if (mode == "missing-consumer") effect.Remove("consumer");
                    if (mode == "empty-consumer") effect.Set("consumer", JsonValue.Of(""));
                    if (mode == "unknown-key") effect.Set("legacy", JsonValue.Of(true));
                    if (mode == "stateful-unused") root.Set("stateful_work_effects", Json.Parse("[{\"id\":\"unused.stateful\"}]"));
                    if (mode == "wrong-completion-kind") {
                        root.Set("independent_research_effects", JsonValue.NewArray());
                        root.Set("stateful_work_effects", Json.Parse("[{\"id\":\"research.complete\"}]"));
                    }
                })), "early immutable authored effect refusal " + mode);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "no Initialize for invalid effect " + mode);
            }
        }
    }
}
