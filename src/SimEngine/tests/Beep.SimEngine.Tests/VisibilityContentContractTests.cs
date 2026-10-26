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
    // Files, ordinary player intents/clock and immutable views only; no private world/gameplay writes.
    internal static partial class VisibilityContentContractTests
    {
        internal const string Case = "Visibility content contracts: earned private discovery and guarded rules";
        internal static void Register()
        { TestRunner.Add(Case, Run); TestRunner.Add("Visibility target metadata: typed actual references avoid runtime match callbacks", Targets);
          TestRunner.Add("Visibility facts: single and multiple targets retain live guarded observations", Allocation); }
        private static GameFactory Factory(VisibilityContentContractsModule module, bool independent = true, Action<JsonValue> edit = null, bool missingPolicy = false,
            string targetDocument = null)
        {
            var doc = Json.Parse(FrameworkProofContent.Read("visibility-content-contracts.json")); edit?.Invoke(doc);
            if (!independent)
            {
                foreach (var pair in new[] { ("independent_visibility_policies", "knowledge_visibility_policies"),
                    ("independent_entity_visibility", "entity_knowledge_visibility"), ("independent_activity_visibility", "activity_knowledge_visibility") })
                { doc.Set(pair.Item2, doc.Get(pair.Item1)); doc.Remove(pair.Item1); }
            }
            var declaration = Json.Parse("{\"id\":\"proof.visibility-content\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\"},{\"id\":\"proof.knowledge-content\",\"version\":\"1.0.0\"},{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"visibility-content-contracts.json\"]}");
            var files = new Dictionary<string, string> { ["visibility-content-contracts.json"] = doc.ToString() };
            if (targetDocument != null) { declaration.Get("documents").Add(JsonValue.Of("visibility-targets.json")); files.Add("visibility-targets.json", targetDocument); }
            return KnowledgeContentContractTests.Factory(new KnowledgeContentContractsModule(), independent,
                additionalModules: new GameModule[] { new KnowledgeVisibilityModule(), module },
                additionalDeclarations: new[] {
                    Json.Parse("{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[]}"),
                    declaration },
                additionalFiles: files,
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(KnowledgeVisibilityModule.ModuleId)); scenario.Get("modules").Add(JsonValue.Of(module.Id));
                    scenario.Get("settings").Set(KnowledgeVisibilityModule.ModuleId, Json.Parse("{\"policy\":\"" + (missingPolicy ? "missing" : "field.visibility") + "\"}"));
                }, worldEdit: world => {
                    // Author layer metadata in the existing point module's actual document.
                    var layers = Json.Parse(FrameworkProofContent.Read("point-layers.json"));
                    foreach (string key in new[] { "units", "resources", "spatial_layers" })
                    { if (world.Get(key) == null) world.Set(key, JsonValue.NewArray()); for (int i = 0; i < layers.Get(key).Count; i++) world.Get(key).Add(layers.Get(key)[i]); }
                    world.Get("entities")[1].Get("components").Set("visibility_probe", Json.Parse("{\"id\":\"visibility.entity-probe\"}"));
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static EntityId Entity(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static KnowledgeView Knowledge(SimGame game, int company = 0) => game.Seat(company).View.Feature<KnowledgeView>();
        private static bool Known(SimGame game, int company = 0)
            => Knowledge(game, company).TryRead(new PlayerId(company), "sim.knowledge.facts", "field.stock-reading", out _);
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        { var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes); return nodes; }
        private static PointLayerView Layers(SimGame game, int company = 0) => game.Seat(company).View.Feature<PointLayerView>();
        private static bool Visible(SimGame game, int company = 0) => game.Seat(company).Snapshot.Entities.Any(e => e.Def == "destination");
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("visibility step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 480 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " known=" + Known(game) + " visible=" + Visible(game));
        }
        private static PostOrderCommand Secret(SimGame game) => new PostOrderCommand { Plan = "field.secret", Target = Entity(game, "source"), Priority = 60 };
        private static int Begin(Check c, SimGame game)
        {
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "ordinary worker policy");
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "field.observe", Target = Entity(game, "source"), Priority = 70 });
            c.That(result.Accepted, "normal discover activity visible before secret"); return checked((int)result.Arg0);
        }
        private static void Learn(Check c, SimGame game)
        { Begin(c, game); Until(game, null, () => Known(game) && Visible(game), "earned private discovery"); }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual visibility contract build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same original state layout");
            c.That(typeof(IVisibilityTarget).Assembly.GetType("Beep.SimEngine.Standard.VisibilityTargetDef") == null, "obsolete visibility dependency base deleted");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Invalid(c); Faults(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new VisibilityContentContractsModule(); var factory = Factory(module, independent); var game = New(factory); var idle = New(factory); ulong untouched = idle.Hash();
            c.That(module.ActualAliases && module.SharedOwner && module.Independent == independent && module.Initialize.Initializations > 0, "actual native/independent policy/rules share sole visibility owner");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "discovery not business-dependent");
            c.That(!Visible(game) && !Visible(game, 1), "authored unlearned entity hidden from private snapshots");
            var observerRows = new List<EntityView>(); game.Observer.CopyEntities(observerRows);
            c.That(observerRows.Any(e => e.Def == "destination"), "observer sees actual entity, not fabricated absence");
            ulong before = game.Hash(); c.Equal(Codes.UnknownDef, game.Seat(0).Preview(Secret(game)), "hidden activity preview has unknown refusal");
            c.Equal(SpatialLayerReadStatus.Unavailable, Layers(game).ReadLayer("point.terrain", new PointRegion(Fix.FromInt(5), Fix.One, 1), new List<Fix>()), "unlearned layer details hidden");
            c.Equal(before, game.Hash(), "hidden content/entity/layer queries pure");
            c.Equal(Codes.UnknownDef, TestKit.Order(game, 0, Secret(game)).Refusal, "hidden normal command uses same refusal");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000);
            using (var recorder = game.RecordReplay(limits))
            {
                int order = Begin(c, game); Until(game, null, () => Nodes(game, order).Any(n => n.Progress.WholeTicks > 0), "partial physical knowledge work");
                var retained = Nodes(game, order).Single(); c.That(retained.RequiredTicks == 8 && retained.Progress.WholeTicks < 8, "eight-minute stage not instant");
                c.Equal(Fix.FromInt(18), TransformationContentContractTests.Stock(game, "source"), "physical sample consumes two materials");
                c.That(!Known(game) && !Visible(game), "partial work does not reveal hidden entity");
                var cursor = Layers(game).ReadChanges().Cursor; var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial discovery save");
                long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin); c.Equal(at, game.Clock.StepIndex, "one paused clock does not discover");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Known(game) && Visible(game), "earned rule visibility");
                c.That(!Visible(game, 1), "other company still lacks private knowledge");
                c.Equal(Code.None, game.Seat(0).Preview(Secret(game)), "same engine reveals normal activity after earned fact");
                c.That(game.Seat(0).View.TryGetEntity(Entity(game, "destination"), out var visible), "actual entity now queryable through same engine view");
                var oldDestination = visible;
                var changes = Layers(game).ReadChanges(cursor); c.That(changes.Status == SpatialRegionChangeStatus.Success && changes.FullInvalidation, "earned layer scope invalidates native cursor");
                before = game.Hash();
                var values = new List<Fix>(); c.Equal(SpatialLayerReadStatus.Success, Layers(game).ReadLayer("point.terrain", new PointRegion(Fix.FromInt(5), Fix.One, 1), values), "learned allowed point scope visible");
                c.Equal(1, values.Count, "actual layer sample result");
                c.Equal(SpatialLayerReadStatus.Unavailable, Layers(game).ReadLayer("point.terrain", new PointRegion(Fix.FromInt(50), Fix.One, 1), values), "learned prototype does not bypass sample scope");
                c.Equal(0, values.Count, "no out-of-scope sample leakage"); c.Equal(before, game.Hash(), "revealed projection pure");
                c.That(TestKit.Order(game, 0, Secret(game)).Accepted, "normal secret work now admitted");
                c.That(TestKit.Order(game, 0, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "field.share" }).Accepted, "ordinary exact sharing reveals only recipient");
                c.That(Visible(game, 1) && Known(game, 1), "recipient actual private snapshot responds to same ledger");
                c.Equal(Codes.NotOwned, game.Seat(1).Preview(new PostOrderCommand { Plan = "field.secret", Target = Entity(game, "source"), Priority = 60 }), "known definition still uses normal target ownership admission");
                // Source belongs to company0; the engine must not grant execution merely from discovery.
                c.Equal(Codes.NotOwned, TestKit.Order(game, 1, new PostOrderCommand { Plan = "field.secret", Target = Entity(game, "source"), Priority = 60 }).Refusal, "sharing never grants target ownership");
                c.That(TestKit.Order(game, 0, new ForgetKnowledgeCommand { Policy = "field.forget" }).Accepted, "normal selective forgetting");
                c.That(!Visible(game) && Visible(game, 1), "forgetting hides only own company entity");
                c.Equal(Codes.UnknownDef, game.Seat(0).Preview(Secret(game)), "forgotten definition command gate becomes hidden again");
                c.Equal(SpatialLayerReadStatus.Unavailable, Layers(game).ReadLayer("point.terrain", new PointRegion(Fix.FromInt(5), Fix.One, 1), values), "forgotten layer scope disappears");
                c.That(oldDestination.Def == "destination" && retained.Progress.WholeTicks < 8, "retained immutable facts do not rewrite");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "private hidden/visible companies save");
                int again = Begin(c, game); Until(game, null, () => Nodes(game, again).Any(n => n.Progress.WholeTicks > 0), "new physical relearning");
                twin = factory.Load(game.Save()); Until(game, twin, () => Known(game) && Visible(game), "same original owner earns discovery again");
                c.Equal(Fix.FromInt(16), TransformationContentContractTests.Stock(game, "source"), "relearning consumes real further stock");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "complete discovery save");
                c.Equal(untouched, idle.Hash(), "interleaved world knowledge/visibility isolated");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary discovery/share/forget/relearn stream replays");
            }
        }
        private static void Targets(Check c)
        {
            string document = FrameworkProofContent.Read("visibility-targets.json");
            var module = new VisibilityContentContractsModule();
            var factory = Factory(module, edit: doc => doc.Get("independent_visibility_policies")[0].Get("rules").Add(JsonValue.Of("field.visibility-declared")), targetDocument: document);
            var game = New(factory); var rule = game.World.Content.Get<DeclaredVisibilityTargetsDef>("field.visibility-declared");
            var source = game.World.Content.Get<EntityDef>("source"); var machine = game.World.Content.Get<EntityDef>("machine");
            c.That(rule.Targets.Count == 2 && ReferenceEquals(rule.Targets[0], source) && ReferenceEquals(rule.Targets[1], machine), "explicit multi-target metadata retains actual frozen references");
            c.Throws<NotSupportedException>(() => ((IList<IContentDefinition>)rule.Targets).Clear(), "declared target collection is readonly");
            rule.AllowCalls = 0; ulong hash = game.Hash();
            c.That(!game.Seat(0).View.CanSeeContent(source) && !game.Seat(0).View.CanSeeContent(machine), "one rule applies dynamic refusal to both declared targets");
            c.Equal(2, rule.AllowCalls, "one original dynamic callback per actual target query, no match callbacks");
            c.That(game.Seat(0).View.CanSeeContent(game.World.Content.Get<IResourceContentDefinition>("supply")), "unrelated typed resource uses original unlisted policy");
            c.Equal(2, rule.AllowCalls, "unrelated definitions execute no dynamic callback");
            c.Equal(hash, game.Hash(), "target association and visibility reads mutate no authoritative state");
            c.Equal(hash, factory.Load(game.Save()).Hash(), "actual multi-target rule restores through original content composition");
            foreach (string mode in new[] { "missing", "empty", "null", "null-item", "foreign", "duplicate" }) {
                var malformed = Json.Parse(document); var row = malformed.Get("declared_visibility_targets")[0];
                if (mode == "missing") row.Remove("targets");
                else if (mode == "empty") row.Set("targets", JsonValue.NewArray());
                else if (mode == "duplicate") row.Set("targets", Json.Parse("[\"source\",\"source\"]"));
                else row.Set("invalid", JsonValue.Of(mode));
                var invalid = new VisibilityContentContractsModule();
                c.Throws<ContentException>(() => New(Factory(invalid, targetDocument: malformed.ToString())), "unused invalid target metadata refuses: " + mode);
                c.Equal(0, invalid.Initialize?.Initializations ?? 0, "invalid target metadata never initializes: " + mode);
            }
            foreach (bool throws in new[] { false, true }) {
                var broken = New(factory); var actual = broken.World.Content.Get<DeclaredVisibilityTargetsDef>("field.visibility-declared");
                var primary = new InvalidOperationException("proof.visibility-targets-original");
                actual.TargetsProbe = () => { if (throws) throw primary; ((GameClock)broken.Clock).Pause(); };
                Exception error = null; try { broken.Seat(0).View.CanSeeContent(source); } catch (Exception caught) { error = caught; } finally { actual.TargetsProbe = null; }
                c.That(error is InvalidOperationException && (!throws || ReferenceEquals(error, primary)), "target metadata preserves original mutation/throw refusal");
                c.That(broken.Fault != null, "original observed policy read guards target metadata");
                c.Throws<InvalidOperationException>(() => broken.Save(), "faulting target metadata cannot certify a healthy checkpoint");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-policy", "missing-flag", "missing-rules", "duplicate-rule", "unknown-rule", "unknown-target", "unknown-subject", "empty-facts", "duplicate-fact", "mode", "range", "unused-null-rules", "ambiguous-rule", "unknown-key" })
            {
                var module = new VisibilityContentContractsModule();
                c.Throws<ContentException>(() => New(Factory(module, missingPolicy: mode == "missing-policy", edit: doc => {
                    var policy = doc.Get("independent_visibility_policies")[0]; var rule = doc.Get("independent_activity_visibility")[0];
                    if (mode == "missing-flag") policy.Remove("own_entities");
                    if (mode == "missing-rules") policy.Remove("rules");
                    if (mode == "duplicate-rule") policy.Get("rules").Add(policy.Get("rules")[0].Clone());
                    if (mode == "unknown-rule") policy.Get("rules").Add(JsonValue.Of("missing"));
                    if (mode == "unknown-target") rule.Set("definition", JsonValue.Of("missing"));
                    if (mode == "unknown-subject") rule.Set("facts", Json.Parse("[\"missing\"]"));
                    if (mode == "empty-facts") rule.Set("facts", JsonValue.NewArray());
                    if (mode == "duplicate-fact") rule.Get("facts").Add(rule.Get("facts")[0].Clone());
                    if (mode == "mode") rule.Set("mode", JsonValue.Of("default"));
                    if (mode == "range") doc.Get("independent_layer_visibility")[0].Set("end", JsonValue.Number("-1"));
                    if (mode == "unused-null-rules") doc.Set("invalid_visibility_policies", Json.Parse("[{\"id\":\"unused.invalid-policy\"}]"));
                    if (mode == "ambiguous-rule") doc.Get("independent_entity_visibility")[0].Set("id", JsonValue.Of("field.visibility-secret"));
                    if (mode == "unknown-key") rule.Set("fallback", JsonValue.Of(true));
                })), "invalid registered visibility authoring " + mode);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "invalid definition never initializes " + mode);
            }
        }
        private static void Faults(Check c)
        {
            foreach (var pair in new[] {
                (VisibilityProbeStage.Allow, VisibilityProbeFault.Pause), (VisibilityProbeStage.Allow, VisibilityProbeFault.Throw),
                (VisibilityProbeStage.Allow, VisibilityProbeFault.Recursive), (VisibilityProbeStage.Allow, VisibilityProbeFault.SwallowedRecursive),
                (VisibilityProbeStage.Entity, VisibilityProbeFault.PrivateEntity), (VisibilityProbeStage.Sample, VisibilityProbeFault.Pause),
                (VisibilityProbeStage.Keys, VisibilityProbeFault.Throw) })
            {
                var module = new VisibilityContentContractsModule(); var game = New(Factory(module));
                if (pair.Item1 != VisibilityProbeStage.Allow) Learn(c, game);
                EntityId target = pair.Item1 == VisibilityProbeStage.Entity ? Entity(game, "destination") : EntityId.None;
                module.Arm(pair.Item1, pair.Item2); long at = game.Clock.StepIndex;
                if (pair.Item1 == VisibilityProbeStage.Keys) game.Update();
                else c.Throws<InvalidOperationException>(() => {
                    if (pair.Item1 == VisibilityProbeStage.Allow) game.Seat(0).Preview(Secret(game));
                    if (pair.Item1 == VisibilityProbeStage.Entity) game.Seat(0).View.TryGetEntity(target, out _);
                    if (pair.Item1 == VisibilityProbeStage.Sample) Layers(game).ReadLayer("point.terrain", new PointRegion(Fix.FromInt(5), Fix.One, 1), new List<Fix>());
                }, "actual selected visibility callback faults " + pair);
                c.That(game.Fault != null && module.Calls == 1, "first actual callback latches fault and withholds result " + pair);
                if (pair.Item1 == VisibilityProbeStage.Entity) c.Equal(target, module.ActualTarget, "private codec guard observes queried actual entity");
                c.That(module.MutationCompleted == (pair.Item2 == VisibilityProbeFault.Pause || pair.Item2 == VisibilityProbeFault.PrivateEntity || pair.Item2 == VisibilityProbeFault.SwallowedRecursive), "mutation or swallowed recursion actually reached " + pair);
                c.Equal(at, game.Clock.StepIndex, "fault does not advance game time " + pair); c.Equal(0, game.Update(), "faulted visibility cannot resume");
                c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy faulted visibility hash"); c.Throws<InvalidOperationException>(() => game.Save(), "no faulted visibility save");
            }
        }
    }
}
