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
using static Beep.SimEngine.Tests.ProgressionDisclosureTests;

namespace Beep.SimEngine.Tests
{
    // Explicit file composition and ordinary player input/immutable observations only.
    internal static class ActivityDisclosureTests
    {
        internal const string Case = "Activity disclosure: required content contracts and private work facts through physical execution and revocation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame Session(GameFactory factory) => New(factory, "activity-disclosure");
        private static List<OrderView> Orders(WorkView view)
        { var rows = new List<OrderView>(); view.CopyOrders(rows); return rows; }
        private static List<PlanView> Plans(WorkView view)
        { var rows = new List<PlanView>(); view.CopyPlans(rows); return rows; }
        private static bool Known(SimGame game)
        {
            var rows = new List<TechnologySnapshot>(); game.Observer.Feature<TechnologyView>().CopyTechnologies(rows);
            return rows.Any(x => x.Company.Value == 0 && x.Technology == "decl.external" && x.Known);
        }
        private static PlanIntent Intent(SimGame game, string definition, string actor, params long[] dependencies)
            => new PlanIntent(definition, ActivityWorkerSelection.CreateArguments(Actor(game, actor), ActivityWorkerSelection.Unbound), "disclosure", 70, true, dependencies);
        private static void Run(Check c)
        {
            foreach (string invalid in new[] { "null", "empty", "null-entry", "foreign", "duplicate", "missing-reference", "missing-array" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => Session(Fixture(initialize, file => {
                    var definition = file.Get("malformed_activity_disclosure")[0];
                    if (invalid == "missing-array") definition.Remove("references");
                    else {
                        definition.Set("mode", JsonValue.Of(invalid));
                        if (invalid == "duplicate") definition.Set("references", Json.Parse("[\"decl.native\",\"decl.native\"]"));
                        if (invalid == "missing-reference") definition.Set("references", Json.Parse("[\"missing\"]"));
                    }
                }, new ActivityDisclosureModule())), "unused malformed activity disclosure refuses " + invalid);
                c.Equal(0, initialize.Initializations, "declaration refuses before Initialize");
            }
            var factory = Fixture(new InitializeProbe(), activity: new ActivityDisclosureModule()); var game = Session(factory);
            c.Equal(16, game.Modules.Count, "all sixteen modules are explicit in master and scenario");
            var work = game.Seat(0).View.Feature<WorkView>(); var observer = game.Observer.Feature<WorkView>();
            var requirements = new List<WorkRequirementView>();
            ulong hash = game.Hash();
            c.Equal(Codes.UnknownDef, work.PreviewOrderRequirements("decl.native", Actor(game, "receiver"), requirements), "visible native activity with hidden prerequisite has no requirements disclosure");
            var binding = Intent(game, "binding:disclosure", "assembler");
            c.Equal(Codes.UnknownDef, work.PreviewPlan(binding), "independent binding cannot bypass hidden authored workflow");
            c.Equal(Codes.UnknownDef, game.Seat(0).Preview(new SubmitPlanCommand { Intent = binding }), "ordinary command matches bound-plan disclosure preview");
            c.Equal(hash, game.Hash(), "hidden previews are pure");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recording = game.RecordReplay(limits);
            var twin = Reveal(c, factory, game, "tech");
            twin = Input(c, factory, game, new SubmitPlanCommand { Intent = binding }); Step(game, twin);
            long parent = Plans(work).Single().Id;
            twin = Input(c, factory, game, new SubmitPlanCommand { Intent = Intent(game, "activity:disclose.predicate.activity", "smelter", parent) }); Step(game, twin);
            c.That(Plans(work).Count == 2 && Plans(work).Single(x => x.Id != parent).Dependencies.Single() == parent, "original paused dependency intent exposes actual prerequisite only when whole declaration is visible");
            twin = Input(c, factory, game, new PostOrderCommand { Plan = "decl.external", Target = Actor(game, "receiver"), Priority = 70 }); Step(game, twin);
            int order = Orders(observer).Single(x => x.Plan == "activity:decl.external").Id;
            Until(game, twin, () => {
                var nodes = new List<ActivityNodeView>(); observer.TryCopyNodes(order, nodes);
                return nodes.Any(x => x.Id == "research" && x.State == NodeState.Working && !x.Progress.Equals(WorkProgress.Zero));
            }, "physical staffed research starts");
            c.That(work.TryReadProgress(order, out var retained), "visible active order exposes truthful original progress");
            var visibleNodes = new List<ActivityNodeView>(); c.That(work.TryCopyNodes(order, visibleNodes), "visible actual nodes publish");
            EntityId researcher = visibleNodes.Single(x => x.Id == "research").Worker;
            c.That(researcher.IsValid && work.ReadWorkerAssignment(researcher, out var assignment).IsNone && assignment.HasValue, "public assignment is the actual physically working machine");
            twin = Input(c, factory, game, new ForgetKnowledgeCommand { Policy = "disclose.forget" }); Step(game, twin);
            c.That(!Known(game), "revocation happens while earned research remains unfinished");
            c.That(Plans(work).Count == 0 && Plans(observer).Count == 2, "hidden bound plan withholds visible dependent rather than leaking prerequisite ID");
            c.That(Orders(work).All(x => x.Id != order) && Orders(observer).Any(x => x.Id == order), "private work listing withholds original order while observer retains it");
            c.That(!work.TryReadProgress(order, out var hidden) && hidden == null, "retained work view rechecks current authorization");
            var nodesAfter = new List<ActivityNodeView>(); c.That(!work.TryCopyNodes(order, nodesAfter) && nodesAfter.Count == 0, "known order ID cannot reveal hidden node progress or worker");
            c.That(!work.TryCopyOrderRequirements(order, requirements) && requirements.Count == 0, "hidden live requirements expose no partial facts");
            c.Equal(Codes.NotFound, work.PreviewNodeEffect(order, "research"), "hidden direct node effect behaves as missing");
            c.Equal(Codes.NotFound, work.PreviewOrderEffect(order), "hidden final effect behaves as missing");
            c.Equal(0, work.ActiveOrders(Actor(game, "receiver")), "private active count does not reveal hidden work");
            c.Equal(Codes.NotFound, work.ReadWorkerAssignment(researcher, out _), "assignment cannot expose hidden activity through an owned visible worker");
            c.Equal(Codes.NotFound, work.ReadWorkerExecution(researcher, out _), "worker execution projection cannot bypass assignment disclosure");
            var jobs = new List<WorkJobView>(); work.CopyJobs(jobs);
            c.That(jobs.All(x => x.IsHaul || x.Target != Actor(game, "receiver")), "hidden activity job fact is withheld");
            var candidates = new List<WorkWorkerCandidate>(); c.That(!work.TryCopyWorkerCandidates(order, "research", candidates) && candidates.Count == 0, "hidden selection details withheld");
            var queue = new List<WorkDispatchOffer>(); work.CopyDispatchQueue(queue);
            var offers = new List<WorkerDispatchOffer>(); work.CopyAssignments(offers);
            c.That(offers.All(x => x.Scope != WorkModule.ModuleId || x.Entry == "job" || x.Identity != order), "native timed offers disclose no hidden activity identity");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "hidden pending plans and staffed partial execution restore without copied state");
            Until(game, twin, () => Known(game), "hidden already-running research physically completes"); Step(game, twin);
            c.That(!work.TryReadActivityCompletion("activity:decl.external", Actor(game, "receiver"), out _), "hidden earned history cannot bypass disclosure");
            c.That(retained.UnfinishedNodes > 0 && visibleNodes.Single(x => x.Id == "research").State == NodeState.Working, "returned facts remain detached as hidden work completes");
            c.That(Plans(game.Seat(1).View.Feature<WorkView>()).Count == 0, "foreign company receives no owned plan metadata");
            twin = Reveal(c, factory, game, "tech");
            c.That(work.TryReadActivityCompletion("activity:decl.external", Actor(game, "receiver"), out var completed) && completed.Order == order, "physical re-discovery reveals the same earned completion, never a new order");
            c.That(Plans(work).Count == 2 && Plans(work).Single(x => x.Id != parent).Dependencies.Single() == parent, "re-discovery restores whole original dependency metadata");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "restored disclosure and once-only completion preserve hash");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "saved ordinary inputs replay physical discovery, partial research, forgetting and completion");
            var isolated = Session(factory); Step(isolated);
            c.That(Plans(isolated.Seat(0).View.Feature<WorkView>()).Count == 0 && !Known(isolated) && game.Fault == null, "interleaved session has no leaked visibility or earned state");
        }
    }
}
