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
    // Only authored files, ordinary player commands, clock input and immutable engine facts.
    internal static class KnowledgeContentContractTests
    {
        internal const string Case = "Knowledge content contracts: physical private learning and policies";
        private const string Provider = "sim.knowledge.facts", Reading = "field.stock-reading", Method = "field.method";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(KnowledgeContentContractsModule module, bool independent = true, Action<JsonValue> edit = null,
            IReadOnlyList<GameModule> additionalModules = null, IReadOnlyList<JsonValue> additionalDeclarations = null,
            IReadOnlyDictionary<string, string> additionalFiles = null, Action<JsonValue> scenarioEdit = null, Action<JsonValue> worldEdit = null)
        {
            var doc = Json.Parse(FrameworkProofContent.Read("knowledge-content-contracts.json")); edit?.Invoke(doc);
            if (!independent)
            {
                doc.Set("knowledge_sharing_policies", doc.Get("independent_sharing_policies")); doc.Remove("independent_sharing_policies");
                doc.Set("knowledge_forgetting_policies", doc.Get("independent_forgetting_policies")); doc.Remove("independent_forgetting_policies");
            }
            var native = JsonValue.NewObject();
            foreach (string section in new[] { "knowledge_facts", "knowledge_effects", "knowledge_sharing_policies", "knowledge_forgetting_policies" })
                if (doc.Get(section) != null) { native.Set(section, doc.Get(section)); doc.Remove(section); }
            var modules = new List<GameModule> { module }; if (additionalModules != null) modules.AddRange(additionalModules);
            var declarations = Json.Parse("[{\"id\":\"proof.knowledge-content\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"knowledge-content-contracts.json\"]}]");
            if (additionalDeclarations != null) foreach (var declaration in additionalDeclarations) declarations.Add(declaration);
            var files = new Dictionary<string, string> { ["knowledge-content-contracts.json"] = doc.ToString() };
            if (additionalFiles != null) foreach (var file in additionalFiles) files.Add(file.Key, file.Value);
            return ConstructionPlanTests.Points(extraModules: modules,
                extraDeclarations: declarations.ToString(), extraDocuments: files,
                knowledgeDefinitions: native.ToString(), scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(CompanyKnowledgeModule.ModuleId)); scenario.Get("modules").Add(JsonValue.Of(module.Id)); scenarioEdit?.Invoke(scenario);
                }, worldEdit: world => { StartupContentAuthoring.Position(world, "machine", JsonValue.Number("4")); worldEdit?.Invoke(world); });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static EntityId Entity(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static KnowledgeView Knowledge(SimGame game, int company = 0) => game.Seat(company).View.Feature<KnowledgeView>();
        private static bool Known(SimGame game, string subject, int company = 0)
            => Knowledge(game, company).TryRead(new PlayerId(company), Provider, subject, out _);
        private static KnowledgeObservation Fact(SimGame game, string subject, int company = 0)
        { if (!Knowledge(game, company).TryRead(new PlayerId(company), Provider, subject, out var fact)) throw new InvalidOperationException("missing " + company + "/" + subject); return fact; }
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        { var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes); return nodes; }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("knowledge step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 480 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex +
                " stock=" + TransformationContentContractTests.Stock(game, "source") + " known=" + Known(game, Reading));
        }
        private static int Begin(Check c, SimGame game)
        {
            var command = new PostOrderCommand { Plan = "field.observe", Target = Entity(game, "source"), Priority = 70 };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal activity preview");
            c.Equal(hash, game.Hash(), "activity preview pure"); var result = TestKit.Order(game, 0, command);
            c.That(result.Accepted, "normal positive-time field observation"); return checked((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current knowledge contract build"); c.Equal(154, EngineInfo.SaveFormat, "original saved ledger layout");
            foreach (string name in new[] { "KnowledgeSubjectDef", "KnowledgeSubjectPolicyDef", "KnowledgeSharingPolicyDef", "KnowledgeForgettingPolicyDef" })
                c.That(typeof(IKnowledgeSubjectDefinition).Assembly.GetType("Beep.SimEngine.Standard." + name) == null, "deleted dependency base " + name);
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Invalid(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new KnowledgeContentContractsModule(); var factory = Factory(module, independent); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            c.That(module.ActualAliases && module.SharedOwners && module.IndependentPolicies == independent && module.Initialize.Initializations > 0,
                "direct subject/policy aliases use original ledger and commands");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "knowledge work has no Tycoon prerequisite");
            c.That(!Known(game, Reading) && !Known(game, Method), "no initial knowledge grant");
            ulong hash = game.Hash(); c.Equal(Codes.NotFound, Knowledge(game).PreviewShare(new PlayerId(1), "field.share", out var missing), "unearned subjects cannot transmit");
            c.That(missing != null && missing.Entries.Count == 2, "engine returns actual per-subject blockers");
            c.Equal(Codes.NotAllowed, Knowledge(game, 1).PreviewShare(new PlayerId(0), "field.share", out _), "explicit sharing direction enforced");
            c.Equal(hash, game.Hash(), "knowledge previews pure");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000);
            using (var recorder = game.RecordReplay(limits))
            {
                c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
                    Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "player enables real machine");
                int order = Begin(c, game);
                Until(game, null, () => Nodes(game, order).Any(n => n.Progress.WholeTicks > 0), "partial physical observation");
                var retained = Nodes(game, order).Single();
                c.That(retained.RequiredTicks == 8 && retained.Progress.WholeTicks < 8 && retained.Worker == Entity(game, "machine"), "actual machine earns authored duration");
                c.Equal(Fix.FromInt(18), TransformationContentContractTests.Stock(game, "source"), "two native inputs physically consumed once");
                c.That(!Known(game, Reading), "partial work is not completed knowledge");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial work/stock save");
                long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin); c.Equal(at, game.Clock.StepIndex, "single paused game clock freezes work");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Known(game, Reading) && Known(game, Method), "earned node and final knowledge effects");
                c.That(retained.Progress.WholeTicks < 8, "retained partial graph facts immutable");
                var learned = Fact(game, Reading); c.Equal(18L * Fix.One.Raw, learned.Data.Get("quantity_raw").Kind == JsonKind.Number
                    ? new JsonReader(learned.Data, "reading").Long("quantity_raw") : -1L, "independent subject observes actual stock, not authored grant");
                c.That(learned.Data.Frozen && learned.ObservedTick >= at + 6, "earned observation frozen and game-timed");
                c.That(!Knowledge(game, 1).TryRead(new PlayerId(0), Provider, Reading, out _), "foreign company private knowledge not leaked");
                c.Equal(Code.None, Knowledge(game).ReadFreshness(new PlayerId(0), Provider, Reading, out var fresh), "native freshness accepts independent subject");
                c.That(fresh.IsFresh, "actual new observation fresh"); long age = fresh.AgeSteps;
                hash = game.Hash(); c.Equal(Code.None, Knowledge(game).PreviewShare(new PlayerId(1), "field.share", out var outgoing), "mixed native/independent subjects share normally");
                c.Equal(hash, game.Hash(), "outgoing projection pure");
                c.That(TestKit.Order(game, 0, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "field.share" }).Accepted, "normal whole knowledge transmission");
                var received = Fact(game, Reading, 1); c.Equal(learned.ObservedTick, received.ObservedTick, "sharing preserves real observation time");
                c.Equal(learned.Data.ToString(), received.Data.ToString(), "sharing copies historical data exactly");
                c.That(Known(game, Method, 1), "native permanent subject transmitted through same policy");
                c.Equal(Code.None, Knowledge(game, 1).ReadFreshness(new PlayerId(1), Provider, Reading, out var shared), "recipient private freshness");
                c.Equal(age + 1, shared.AgeSteps, "sharing consumes game time without refreshing knowledge");
                twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin); game.Clock.Resume(); twin.Clock.Resume();
                c.Equal(Code.None, Knowledge(game, 1).ReadFreshness(new PlayerId(1), Provider, Reading, out var paused), "saved recipient reading freshness");
                c.Equal(shared.AgeSteps, paused.AgeSteps, "pause does not age knowledge");
                for (int i = 0; i < 4; i++) Step(game, twin);
                c.Equal(Code.None, Knowledge(game).ReadFreshness(new PlayerId(0), Provider, Reading, out var stale), "expired history remains available");
                c.That(!stale.IsFresh && fresh.IsFresh, "expiry derives from sole clock and earlier snapshot stays immutable");
                c.That(TestKit.Order(game, 0, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "field.share" }).Accepted, "stale history transmission explicit");
                c.Equal(received.ObservedTick, Fact(game, Reading, 1).ObservedTick, "stale repetition never refreshes observation");
                c.Equal(received.LearnedTick, Fact(game, Reading, 1).LearnedTick, "repeat preserves recipient first learning");
                int cancelled = Begin(c, game);
                Until(game, null, () => Nodes(game, cancelled).Any(n => n.Progress.WholeTicks > 0), "second observation physical partial");
                c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancelled }).Accepted, "normal cancellation before earned new effect");
                c.Equal(learned.ObservedTick, Fact(game, Reading).ObservedTick, "cancelled work cannot forge new observation");
                c.Equal(Fix.FromInt(16), TransformationContentContractTests.Stock(game, "source"), "cancellation does not refund spent sample inputs");
                hash = game.Hash(); c.Equal(Code.None, Knowledge(game).PreviewForget("field.forget", out var forgetting), "same owner previews selective forgetting");
                c.Equal(hash, game.Hash(), "forget preview pure");
                c.That(TestKit.Order(game, 0, new ForgetKnowledgeCommand { Policy = "field.forget" }).Accepted, "normal player forgets selected historical reading");
                c.That(!Known(game, Reading) && Known(game, Method) && Known(game, Reading, 1), "forgetting affects only selected own-company fact");
                c.That(forgetting.Entries[0].Observation.HasValue && outgoing.Entries.Count == 2, "retained knowledge views remain detached");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "forgotten ledger state persists");
                int relearn = Begin(c, game); Until(game, null, () => Nodes(game, relearn).Any(n => n.Progress.WholeTicks > 0), "physical relearning");
                twin = factory.Load(game.Save()); Until(game, twin, () => Known(game, Reading), "new earned observation after explicit forgetting");
                var newFact = Fact(game, Reading); c.That(newFact.LearnedTick > learned.LearnedTick && newFact.ObservedTick > learned.ObservedTick, "relearning earns new actual timestamps");
                c.Equal(Fix.FromInt(14), TransformationContentContractTests.Stock(game, "source"), "third physical sample uses more stock");
                c.That(TestKit.Order(game, 0, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "field.share" }).Accepted, "new actual reading can update recipient");
                c.Equal(newFact.ObservedTick, Fact(game, Reading, 1).ObservedTick, "latest merge earns actual newer data");
                c.Equal(received.LearnedTick, Fact(game, Reading, 1).LearnedTick, "latest merge preserves original recipient acquisition");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed mixed knowledge/physical state saves");
                c.Equal(isolated, idle.Hash(), "interleaved session has no learned-state leakage");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary work/sharing/forgetting/relearning command stream replays");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "retention", "missing-freshness", "zero-freshness", "first-finite", "resource", "unused-resource", "subject", "provider", "empty-subjects", "duplicate-subject", "direction", "unseated-direction", "unseated-forget", "duplicate-company", "unknown-key" })
            {
                var module = new KnowledgeContentContractsModule();
                c.Throws<ContentException>(() => New(Factory(module, edit: doc => {
                    var subject = doc.Get("independent_stock_subjects")[0]; var sharing = doc.Get("independent_sharing_policies")[0]; var forget = doc.Get("independent_forgetting_policies")[0];
                    if (mode == "retention") subject.Set("retention", JsonValue.Of("default"));
                    if (mode == "missing-freshness") subject.Remove("fresh_for");
                    if (mode == "zero-freshness") subject.Get("fresh_for").Set("numerator", JsonValue.Number("0"));
                    if (mode == "first-finite") subject.Set("retention", JsonValue.Of("first"));
                    if (mode == "resource") subject.Set("resource", JsonValue.Of("missing"));
                    if (mode == "unused-resource") { var unused = subject.Clone(); unused.Set("id", JsonValue.Of("unused.reading")); unused.Set("resource", JsonValue.Of("missing")); doc.Get("independent_stock_subjects").Add(unused); }
                    if (mode == "subject") sharing.Get("subjects")[0].Set("subject", JsonValue.Of("missing"));
                    if (mode == "provider") sharing.Get("subjects")[0].Set("provider", JsonValue.Of("missing"));
                    if (mode == "empty-subjects") sharing.Set("subjects", JsonValue.NewArray());
                    if (mode == "duplicate-subject") sharing.Get("subjects").Add(sharing.Get("subjects")[0].Clone());
                    if (mode == "direction") sharing.Get("directions")[0].Set("recipient", JsonValue.Number("0"));
                    if (mode == "unseated-direction") sharing.Get("directions")[0].Set("recipient", JsonValue.Number("8"));
                    if (mode == "unseated-forget") forget.Get("companies").Add(JsonValue.Number("8"));
                    if (mode == "duplicate-company") forget.Get("companies").Add(JsonValue.Number("0"));
                    if (mode == "unknown-key") subject.Set("compatibility", JsonValue.Of(true));
                })), "invalid independent authored knowledge " + mode);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "invalid knowledge refuses before Initialize " + mode);
            }
        }
    }
}
