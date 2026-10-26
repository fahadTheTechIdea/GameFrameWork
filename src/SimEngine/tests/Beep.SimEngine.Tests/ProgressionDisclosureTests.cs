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
    // Explicit files and ordinary player commands/views only. Original engine owners run all rules.
    internal static class ProgressionDisclosureTests
    {
        internal const string Case = "Progression disclosure: typed predicate inputs and private transitive prerequisites through physical discovery and revocation";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Fixture(InitializeProbe initialize, Action<JsonValue> edit = null, ActivityDisclosureModule activity = null, string planDocument = null)
        {
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new MotionModule(),
                new WorkModule(), new LogisticsModule(), new TransformationModule(), new WorldSetupModule(), new PointModule(),
                new CompanyKnowledgeModule(), new TechnologyModule(), new ObjectiveModule(), new ProgressionDeclarationModule(initialize),
                new KnowledgeVisibilityModule(), new ProgressionDisclosureModule() };
            if (activity != null) modules.Add(activity);
            var definition = new GameDefinition("progression-disclosure", modules);
            string authoredName = activity == null ? "progression-disclosure.json" : "activity-disclosure.json";
            var authored = Json.Parse(FrameworkProofContent.Read(authoredName)); edit?.Invoke(authored);
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read(planDocument != null ? "plan-disclosure-master.json" : activity == null ? "progression-disclosure-master.json" : "activity-disclosure-master.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("progression-declarations.json", FrameworkProofContent.Read("progression-declarations.json"))
                .Add("progression-disclosure.json", activity == null ? authored.ToString() : FrameworkProofContent.Read("progression-disclosure.json"));
            if (activity != null) files.Add(authoredName, authored.ToString());
            if (planDocument != null) files.Add(planDocument, FrameworkProofContent.Read(planDocument));
            foreach (string name in new[] { "resources", "recipes", "world", "work", "logistics" })
                files.Add("planet-x-" + name + ".json", FrameworkProofContent.Read("planet-x-" + name + ".json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame Session(GameFactory factory) => New(factory, "progression-disclosure");
        private static List<TechnologySnapshot> Technologies(SimGame game, int seat = 0)
        { var rows = new List<TechnologySnapshot>(); game.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(rows); return rows; }
        private static List<ObjectiveSnapshot> Objectives(SimGame game, bool observer = false)
        { var rows = new List<ObjectiveSnapshot>(); (observer ? game.Observer : game.Seat(0).View).Feature<ObjectiveView>().CopyObjectives(rows); return rows; }
        private static bool Known(SimGame game, string id) => Technologies(game).Single(x => x.Technology == id).Known;
        internal static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " exceeded256Updates, clock=" + game.Clock.StepIndex
                + ", objectives=" + string.Join(";", Objectives(game, true).Select(x => x.Company + "/" + x.Objective + "/" + x.State + "/" + x.Blocker)));
        }
        internal static SimGame Input(Check c, GameFactory factory, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary preview admits authored intent");
            c.Equal(hash, game.Hash(), "ordinary preview preserves state/RNG"); game.Seat(0).Submit(command); return factory.Load(game.Save());
        }
        private static bool Fact(SimGame game, string subject)
            => game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), AuthoredKnowledgeProvider.ProviderId, "disclose." + subject, out _);
        internal static SimGame Reveal(Check c, GameFactory factory, SimGame game, string subject)
        {
            var twin = Input(c, factory, game, new PostOrderCommand { Plan = "disclose." + subject + ".activity", Target = Actor(game, "receiver"), Priority = 80 });
            Step(game, twin); c.That(!Fact(game, subject), "discovery needs actual machine travel/productive game time");
            Until(game, twin, () => Fact(game, subject), "physical " + subject + " discovery");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "earned discovery and pending physical work restore");
            return twin;
        }
        private static void Run(Check c)
        {
            foreach (string invalid in new[] { "null", "null-entry", "foreign", "duplicate", "missing-reference", "missing-array" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => Session(Fixture(initialize, file => {
                    var definition = file.Get("malformed_disclosure_predicates")[0];
                    if (invalid == "missing-array") definition.Remove("references");
                    else if (invalid == "duplicate") definition.Set("references", Json.Parse("[\"decl.native\",\"decl.native\"]"));
                    else if (invalid == "missing-reference") definition.Set("references", Json.Parse("[\"missing\"]"));
                    else definition.Set("mode", JsonValue.Of(invalid));
                })), "unused malformed observation declaration rejects " + invalid);
                c.Equal(0, initialize.Initializations, "invalid declared input refuses before Initialize");
            }
            var initialized = new InitializeProbe(); var factory = Fixture(initialized); var game = Session(factory);
            c.That(initialized.Initializations == 1 && game.Modules.Count == 15, "explicit native fifteen-module master and independent predicate compose");
            var tech = game.Seat(0).View.Feature<TechnologyView>(); var objective = game.Seat(0).View.Feature<ObjectiveView>();
            ulong initial = game.Hash();
            c.That(Technologies(game).Count == 0, "hidden prerequisite withholds native dependent metadata too");
            c.Equal(Codes.NotFound, tech.ReadTechnology("decl.native", out var hiddenTechnology), "single read respects transitive hidden prerequisite");
            c.Equal<string>(null, hiddenTechnology.Technology, "hidden single read publishes no metadata");
            c.That(Objectives(game).Count == 0, "hidden objective dependency and predicate inputs disclose no progress/deadline/IDs");
            foreach (string id in new[] { "decl.external-earned", "decl.native-earned", "disclose.forward-earned", "missing" })
                c.Equal(Codes.NotFound, objective.Observe(id).Blocker, "undisclosed direct query is indistinguishable from missing: " + id);
            c.Equal(Codes.UnknownDef, tech.PreviewResearch("decl.native", Actor(game, "receiver"), 70), "preview refuses native research with undisclosed prerequisite");
            c.Equal(Codes.UnknownDef, game.Seat(0).Preview(new PostOrderCommand { Plan = "decl.native", Target = Actor(game, "receiver"), Priority = 70 }), "command uses same undisclosed prerequisite admission");
            c.Equal(initial, game.Hash(), "undisclosed reads and refused previews preserve state/RNG");
            var observerTech = new List<TechnologySnapshot>(); game.Observer.Feature<TechnologyView>().CopyTechnologies(observerTech);
            c.That(observerTech.Count == 4 && Objectives(game, true).Count == 6, "explicit observer authority retains whole actual company enrollment");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recording = game.RecordReplay(limits);
            var twin = Reveal(c, factory, game, "tech");
            c.That(Technologies(game).Count == 2 && Technologies(game).Single(x => x.Technology == "decl.native").Prerequisites.Single() == "decl.external", "physically learned disclosure publishes truthful whole prerequisite metadata");
            c.Equal(Code.None, tech.ReadTechnology("decl.native", out var disclosedTechnology), "physical discovery enables same single read");
            c.That(!disclosedTechnology.Known && disclosedTechnology.Prerequisites.Single() == "decl.external", "disclosure publishes prerequisites without granting research");
            c.That(Technologies(game, 1).Count == 0, "foreign company receives no discovery metadata");
            c.Equal(TechnologyCodes.MissingPrerequisite, tech.PreviewResearch("decl.native", Actor(game, "receiver"), 70), "discovery does not grant learned research completion");
            var retained = Technologies(game);
            twin = Input(c, factory, game, new PostOrderCommand { Plan = "decl.external", Target = Actor(game, "receiver"), Priority = 70 });
            Until(game, twin, () => Known(game, "decl.external"), "physical prerequisite research"); Step(game, twin);
            c.Equal(Fix.One, Stock(game, "receiver", "crucible"), "original held tool physically supplied for research");
            c.That(Objectives(game).Count == 0 && Objectives(game, true).Where(x => x.Company.Value == 0).All(x => x.State != ObjectiveState.Completed), "hidden predicate content cannot auto-complete objectives from earned hidden facts");
            twin = Reveal(c, factory, game, "objective");
            Until(game, twin, () => Objectives(game).Any(x => x.Objective == "decl.external-earned" && x.State == ObjectiveState.Completed), "disclosed prerequisite objective");
            c.That(Objectives(game).Count == 1, "still-hidden predicate keeps native and independent objective entries withheld");
            twin = Input(c, factory, game, new PostOrderCommand { Plan = "decl.native", Target = Actor(game, "receiver"), Priority = 70 });
            Until(game, twin, () => Known(game, "decl.native"), "physical dependent research"); Step(game, twin);
            c.Equal(Codes.NotFound, objective.Observe("disclose.forward-earned").Blocker, "independent predicate DAG cannot bypass hidden native child metadata");
            c.That(Objectives(game, true).Where(x => x.Company.Value == 0 && x.Objective != "decl.external-earned").All(x => x.Blocker == ObjectiveCodes.Undisclosed), "original lifecycle waits with typed undisclosed blocker rather than executing hidden observation");
            twin = Reveal(c, factory, game, "predicate");
            Until(game, twin, () => Objectives(game).Count == 3 && Objectives(game).All(x => x.State == ObjectiveState.Completed), "disclosed native and independent objective completion");
            c.That(Objectives(game).Single(x => x.Objective == "decl.native-earned").Prerequisites.Single() == "decl.external-earned", "whole actual objective prerequisites publish only when authorized");
            c.That(retained.All(x => !x.Known), "old detached metadata never changes after earned research");
            var retainedComplete = Objectives(game);
            twin = Input(c, factory, game, new ForgetKnowledgeCommand { Policy = "disclose.forget" }); Step(game, twin);
            c.That(Technologies(game).Count == 0 && Objectives(game).Count == 0, "ordinary saved forgetting revokes transitive menus even for completed lifecycle");
            c.Equal(Codes.NotFound, tech.ReadTechnology("decl.native", out var revokedTechnology), "revocation withholds earned individual knowledge too");
            c.Equal<string>(null, revokedTechnology.Technology, "revoked direct read clears earned identity");
            c.That(retainedComplete.All(x => x.State == ObjectiveState.Completed), "already returned immutable facts remain detached after revocation");
            c.Equal(Codes.UnknownDef, tech.PreviewResearch("decl.native", Actor(game, "receiver"), 70), "retained technology view re-observes revocation, not cached authority");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "revoked views and original earned work/knowledge chronology restore");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "physical discovery/research/revocation replays with native saved owners");
            twin = Reveal(c, factory, game, "tech");
            c.That(Technologies(game).All(x => x.Known), "re-discovery reveals original earned completion without duplicate research");
            c.Equal(Code.None, tech.ReadTechnology("decl.native", out var earnedTechnology), "earned technology single read restored after re-discovery");
            c.That(earnedTechnology.Known && earnedTechnology.CompletedTick.HasValue, "same original earned knowledge chronology");
            c.Equal(TechnologyCodes.Known, tech.PreviewResearch("decl.native", Actor(game, "receiver"), 70), "research remains earned exactly once after metadata revocation");
            var isolated = Session(factory); Step(isolated);
            c.That(Technologies(isolated).Count == 0 && Objectives(isolated).Count == 0 && game.Fault == null, "interleaved session has no shared disclosure/earned ledger state");
        }
    }
}
