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
    // Explicit file composition, ordinary player inputs/views/clock/saves and assertions only.
    internal static class ProgressionDeclarationTests
    {
        internal const string Case = "Progression declarations: one JSON owner for native and independent research objectives and physical production unlocks";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(InitializeProbe init, out ProgressionDeclarationModule proof, Action<JsonValue> edit = null, ProgressionViewProbeModule viewProbe = null,
            ProgressionBindingModule bindingProbe = null, Action<JsonValue> editBindings = null)
        {
            proof = new ProgressionDeclarationModule(init);
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new MotionModule(),
                new WorkModule(), new LogisticsModule(), new TransformationModule(), new WorldSetupModule(), new PointModule(),
                new CompanyKnowledgeModule(), new TechnologyModule(), new ObjectiveModule(), proof };
            if (viewProbe != null) modules.Add(viewProbe);
            if (bindingProbe != null) modules.Add(bindingProbe);
            var definition = new GameDefinition("progression-declarations", modules);
            var authored = Json.Parse(FrameworkProofContent.Read("progression-declarations.json"));
            if (bindingProbe != null) authored.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(bindingProbe.Id));
            edit?.Invoke(authored);
            if (viewProbe != null && bindingProbe != null) throw new ArgumentException("select one explicit proof manifest");
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read(bindingProbe != null ? "progression-bindings-master.json" : viewProbe == null ? "progression-declarations-master.json" : "progression-view-master.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("progression-declarations.json", authored.ToString());
            if (viewProbe != null) files.Add("progression-view-probe.json", FrameworkProofContent.Read("progression-view-probe.json"));
            if (bindingProbe != null)
            {
                var bindings = Json.Parse(FrameworkProofContent.Read("progression-bindings.json")); editBindings?.Invoke(bindings);
                files.Add("progression-bindings.json", bindings.ToString());
            }
            foreach (string name in new[] { "resources", "recipes", "world", "work", "logistics" })
                files.Add("planet-x-" + name + ".json", FrameworkProofContent.Read("planet-x-" + name + ".json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame NewSession(GameFactory factory) => New(factory, "progression-declarations");
        private static List<TechnologySnapshot> Technologies(SimGame game, int seat = 0)
        { var facts = new List<TechnologySnapshot>(); game.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(facts); return facts; }
        private static bool Known(SimGame game, string id) => Technologies(game).Single(x => x.Technology == id).Known;
        private static List<ObjectiveSnapshot> Objectives(SimGame game, int seat = 0)
        { var facts = new List<ObjectiveSnapshot>(); game.Seat(seat).View.Feature<ObjectiveView>().CopyObjectives(facts); return facts; }
        private static ObjectiveSnapshot Objective(SimGame game, string id) => Objectives(game).Single(x => x.Objective == id);
        private static OrderView Order(SimGame game, int id)
        { var facts = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(facts); return facts.Single(x => x.Id == id); }
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " exceeded256Updates, step=" + game.Clock.StepIndex
                + ", technology=" + string.Join(";", Technologies(game).Select(x => x.Technology + "/" + x.Known + "/" + x.ActiveOrder + "/" + x.Blocker))
                + ", objectives=" + string.Join(";", Objectives(game).Select(x => x.Objective + "/" + x.State + "/" + x.Blocker))
                + ", smelt=" + Process(game, "smelter").Phase + "/" + Process(game, "smelter").Blocker
                + ", tools=" + string.Join(";", new[] { "source", "receiver", "carrier", "smelter" }.Select(x => x + "/" + Stock(game, x, "crucible"))));
        }
        private static SimGame SubmitSaved(Check c, GameFactory factory, SimGame game, Command command)
        {
            ulong hash = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal private preview admits actual authored intent");
            c.Equal(hash, game.Hash(), "preview does not mutate state/RNG");
            game.Seat(0).Submit(command);
            return factory.Load(game.Save());
        }
        private static void Run(Check c)
        {
            foreach (string section in new[] { "technologies", "independent_technologies" })
                foreach (string invalid in new[] { "missing-array", "null-array", "missing-unlocks", "duplicate-unlock", "missing-unlock",
                    "missing-prerequisite", "cycle", "duplicate-prerequisite", "not-orderable", "no-final", "wrong-final", "zero-duration", "unknown-key" })
                {
                    var init = new InitializeProbe();
                    c.Throws<ContentException>(() => NewSession(Factory(init, out _, x => {
                        var definition = x.Get(section)[0];
                        if (invalid == "missing-array") definition.Remove("prerequisites");
                        if (invalid == "null-array") definition.Set("prerequisites", JsonValue.Null());
                        if (invalid == "missing-unlocks") definition.Remove("unlocks");
                        if (invalid == "duplicate-unlock") definition.Set("unlocks", Json.Parse("[\"decl.smelt\",\"decl.smelt\"]"));
                        if (invalid == "missing-unlock") definition.Set("unlocks", Json.Parse("[\"missing\"]"));
                        if (invalid == "missing-prerequisite") definition.Set("prerequisites", Json.Parse("[\"missing\"]"));
                        if (invalid == "cycle") definition.Set("prerequisites", Json.Parse("[\"decl.native\"]"));
                        if (invalid == "duplicate-prerequisite") definition.Set("prerequisites", Json.Parse("[\"decl.external\",\"decl.external\"]"));
                        if (invalid == "not-orderable") definition.Set("player_orderable", JsonValue.Of(false));
                        if (invalid == "no-final") definition.Remove("on_complete");
                        if (invalid == "wrong-final") definition.Set("on_complete", JsonValue.Of("decl.smelt"));
                        if (invalid == "zero-duration") for (int i = 0; i < definition.Get("nodes").Count; i++) definition.Get("nodes")[i].Get("duration").Set("numerator", JsonValue.Number("0"));
                        if (invalid == "unknown-key") definition.Set("extra", JsonValue.Of(true));
                    })), section + " rejects " + invalid);
                    c.Equal(0, init.Initializations, section + "/" + invalid + " refuses before Initialize");
                }
            foreach (string section in new[] { "objectives", "independent_objectives" })
                foreach (string invalid in new[] { "missing-array", "null-array", "missing-predicate", "wrong-predicate",
                    "missing-prerequisite", "cycle", "duplicate-prerequisite", "zero-deadline", "missing-deadline", "invalid-effect", "missing-effect", "unknown-key" })
                {
                    var init = new InitializeProbe();
                    c.Throws<ContentException>(() => NewSession(Factory(init, out _, x => {
                        var definition = x.Get(section)[0];
                        if (invalid == "missing-array") definition.Remove("prerequisites");
                        if (invalid == "null-array") definition.Set("prerequisites", JsonValue.Null());
                        if (invalid == "missing-predicate") definition.Set("predicate", JsonValue.Of("missing"));
                        if (invalid == "wrong-predicate") definition.Set("predicate", JsonValue.Of("decl.native"));
                        if (invalid == "missing-prerequisite") definition.Set("prerequisites", Json.Parse("[\"missing\"]"));
                        if (invalid == "cycle") definition.Set("prerequisites", Json.Parse("[\"decl.native-earned\"]"));
                        if (invalid == "duplicate-prerequisite") definition.Set("prerequisites", Json.Parse("[\"decl.external-earned\",\"decl.external-earned\"]"));
                        if (invalid == "zero-deadline") definition.Get("deadline").Set("numerator", JsonValue.Number("0"));
                        if (invalid == "missing-deadline") definition.Remove("deadline");
                        if (invalid == "invalid-effect") definition.Set("on_complete", JsonValue.Of(""));
                        if (invalid == "missing-effect") definition.Set("on_failure", JsonValue.Of("missing"));
                        if (invalid == "unknown-key") definition.Set("extra", JsonValue.Of(true));
                    })), section + " rejects " + invalid);
                    c.Equal(0, init.Initializations, section + "/" + invalid + " refuses before Initialize");
                }
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var proof); var game = NewSession(factory);
            c.That(proof.ActualAliases && initialize.Initializations == 1 && game.Modules.Count == 13, "one explicit master composes actual independent/native aliases");
            c.That(Technologies(game).All(x => !x.Known) && Objectives(game).All(x => x.State == ObjectiveState.Locked), "content never pre-grants earned state");
            var retained = Technologies(game); var retainedObjectives = Objectives(game);
            var native = new PostOrderCommand { Plan = "decl.native", Target = Actor(game, "receiver"), Priority = 70 };
            var recipe = Queue(game, "smelter", "space.smelt.planetx");
            ulong hash = game.Hash();
            c.Equal(TechnologyCodes.MissingPrerequisite, game.Seat(0).Preview(native), "native research waits for independent prerequisite");
            c.Equal(TechnologyCodes.Locked, game.Seat(0).Preview(recipe), "actual transformation remains technology locked");
            c.Equal(hash, game.Hash(), "refused previews preserve actual state/RNG");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var replay = game.RecordReplay(limits);
            var target = Actor(game, "receiver");
            var twin = SubmitSaved(c, factory, game, new SubmitPlanCommand {
                Intent = new PlanIntent("activity:decl.external", Json.Parse("{\"target\":" + target.Value + "}"), "decl.research", 70, false) });
            Until(game, twin, () => Technologies(game).Single(x => x.Technology == "decl.external").ActiveOrder > 0, "ordinary plan activation");
            var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(plans); long plan = plans.Single().Id;
            int order = Technologies(game).Single(x => x.Technology == "decl.external").ActiveOrder;
            Until(game, twin, () => Order(game, order).CurrentProgress.WholeTicks > 2 && Stock(game, "receiver", "crucible") == Fix.One, "physical tool delivery and partial research");
            c.That(!Known(game, "decl.external"), "partial research never grants knowledge");
            c.That(((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(x => x.Id == Actor(game, "carrier")).Spatial).Position > Fix.Zero, "worker travels physically to research owner");
            c.Equal(Fix.One, Stock(game, "receiver", "crucible"), "original tool physically supplied, not invented");
            twin = SubmitSaved(c, factory, game, new ConfigurePlanCommand { Plan = plan, Group = "decl.research", Priority = 70, Paused = true }); Step(game, twin);
            WorkProgress progress = Order(game, order).CurrentProgress;
            for (int i = 0; i < 5; i++) Step(game, twin);
            c.Equal(progress, Order(game, order).CurrentProgress, "saved paused physical research preserves exact progress");
            c.That(!Known(game, "decl.external"), "paused research stays unlearned");
            twin = SubmitSaved(c, factory, game, new ConfigurePlanCommand { Plan = plan, Group = "decl.research", Priority = 70, Paused = false });
            Until(game, twin, () => Known(game, "decl.external"), "earned independent research");
            c.That(Technologies(game, 1).All(x => !x.Known), "learned knowledge remains company private");
            c.Equal(TechnologyCodes.Locked, game.Seat(0).Preview(recipe), "first research never borrows later unlock");
            c.Equal(TechnologyCodes.Known, game.Seat(0).Preview(new PostOrderCommand { Plan = "decl.external", Target = target, Priority = 70 }), "learned research cannot complete twice");
            twin = SubmitSaved(c, factory, game, native);
            Until(game, twin, () => Known(game, "decl.native"), "native prerequisite research");
            Until(game, twin, () => Objective(game, "decl.native-earned").State == ObjectiveState.Completed, "native objective chain");
            c.That(Objective(game, "decl.external-earned").SettledTick < Objective(game, "decl.native-earned").ActivatedTick, "objective dependencies activate on a later pass");
            c.That(Objectives(game, 1).All(x => x.State != ObjectiveState.Completed), "foreign objectives cannot borrow owner's knowledge");
            c.That(retained.All(x => !x.Known) && retainedObjectives.All(x => x.State == ObjectiveState.Locked), "retained private facts remain detached");
            // Private receiver stock is not an automatic provider. Player explicitly requests physical tool relocation.
            twin = SubmitSaved(c, factory, game, new HaulResourcesCommand { Source = target, Destination = Actor(game, "smelter"),
                DestinationPort = new Code("space.tool"), Worker = Actor(game, "carrier"), Resource = Resource(game, "crucible"), Ceiling = Fix.One, Priority = 80 });
            Until(game, twin, () => Stock(game, "smelter", "crucible") == Fix.One, "explicit physical private tool relocation");
            c.Equal(Fix.Zero, Stock(game, "receiver", "crucible"), "private tool moves once without an automatic-provider fallback");
            twin = SubmitSaved(c, factory, game, recipe);
            Until(game, twin, () => Stock(game, "smelter", "alloy") == Fix.FromInt(6), "technology-unlocked physical smelting");
            c.Equal(Fix.FromInt(4), Stock(game, "smelter", "slag"), "original whole multi-output result conserved");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "original research/objective/stock/work chronology restores");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "public pending commands physical work objective chain and production replay");
            var isolated = NewSession(factory); Step(isolated);
            c.That(Technologies(isolated).All(x => !x.Known) && Stock(isolated, "source", "planetx.ore") == Fix.FromInt(30), "interleaved new session retains no learned or physical globals");
        }
    }
}
