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
    // Ordinary player input and immutable facts only; definitions/probes contain no positive gameplay.
    internal static class ConstructionContentContractTests
    {
        internal const string Case = "Construction content contracts: one physical owner and independent authored sites";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(InitializeProbe initialize, out ConstructionContentContractsProofModule proof,
            bool independent = true, Action<JsonValue> edit = null, string templates = null,
            Action<JsonValue> worldEdit = null, string geometryPhase = null, string geometryFault = null, string recovery = "point.recovery")
        {
            proof = new ConstructionContentContractsProofModule(initialize, geometryPhase, geometryFault);
            return ConstructionPlanTests.Points(extraModules: new GameModule[] { proof }, templateDefinitions: templates,
                worldEdit: worldEdit,
                recovery: recovery,
                extraDeclarations: "[{\"id\":\"proof.construction-content\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.construction\",\"version\":\"1.0.0\"}],\"documents\":[]}]",
                scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of("proof.construction-content")),
                edit: root => {
                    if (independent)
                    {
                        Rename(root.Get("entities")[0].Get("components"), "construction", "independent_construction");
                        Rename(root.Get("entities")[1].Get("components"), "site", "independent_site");
                    }
                    edit?.Invoke(root);
                });
        }
        private static void Rename(JsonValue parts, string from, string to)
        { var body = parts.Get(from); parts.Remove(from); parts.Set(to, body); }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static List<PlanView> Plans(SimGame game, int seat = 0)
        { var rows = new List<PlanView>(); Work(game, seat).CopyPlans(rows); return rows; }
        private static List<OrderView> Orders(SimGame game)
        { var rows = new List<OrderView>(); Work(game).CopyOrders(rows); return rows; }
        private static List<ActivityNodeView> Nodes(SimGame game, long plan)
        {
            var row = Plans(game).Single(p => p.Id == plan);
            var nodes = new List<ActivityNodeView>(); Work(game).TryCopyNodes(row.Order, nodes); return nodes;
        }
        private static bool Has(SimGame game, string def) => game.Seat(0).Snapshot.Entities.Any(e => e.Def == def);
        private static JsonValue Placement(int position) => Json.Parse("{\"space\":\"field.points\",\"position\":" + position + "}");
        private static PlanIntent Build(int position, bool paused = false)
        {
            var args = JsonValue.NewObject(); args.Set("placement", Placement(position));
            return new PlanIntent("build:point.tower", args, "construction.proof", 70, paused);
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 180 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " update=" + game.Clock.StepIndex
                + " orders=" + string.Join(",", Orders(game).Select(o => o.Plan + ":" + o.State + ":" + o.Blocker))
                + " source=" + Stock(game, "source") + " cargo=" + Stock(game, "machine"));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual construction build");
            c.Equal(154, EngineInfo.SaveFormat, "original site and work codecs");
            var native = Physical(c, false); var independent = Physical(c, true);
            c.That(native.SequenceEqual(independent), "native and independent JSON use identical physical completion coordinates");
            Invalid(c); Faults(c);
        }
        private static long[] Physical(Check c, bool independent)
        {
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var proof, independent);
            var game = New(factory);
            c.That(proof.OneOwner && proof.ActualBindings, "actual aliases share original construction/recovery owner, graph, runtime and state");
            c.Equal(1, initialize.Initializations, "valid metadata initializes once");
            Enable(c, game);
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits);
            Order(c, game, new SubmitPlanCommand { Intent = Build(12, true) });
            long plan = Plans(game).Single().Id;
            c.That(!Has(game, "point.tower-site") && !Has(game, "point.tower") && Orders(game).Count == 0
                && Stock(game, "source") == Fix.FromInt(20), "paused intent creates no instant structure, order or resource spend");
            c.Equal(0, Plans(game, 1).Count, "private company plan not disclosed");
            Order(c, game, new ConfigurePlanCommand { Plan = plan, Group = "construction.proof", Priority = 70, Paused = false });
            c.That(Has(game, "point.tower-site") && !Has(game, "point.tower"), "normal activation creates only its actual authored site");
            c.That(game.Seat(0).Snapshot.Entities.Single(e => e.Def == "point.tower-site").State == new Code("point.building"), "entity Update settles authored construction state");
            var retained = Nodes(game, plan);
            c.That(retained.Count == 2 && retained[0].Id == "prepare" && retained[0].RequiredTicks == 3
                && retained[1].Dependencies.SequenceEqual(new[] { "prepare" }) && retained[1].RequiredTicks == 6, "same authored DAG and durations visible");
            Reach(game, null, () => Nodes(game, plan).Any(n => n.Id == "build" && n.ProductiveSteps > 0), "machine physically reaches material-consuming work");
            c.That(Stock(game, "source") < Fix.FromInt(20), "actual eight-unit supply physically dispatched before useful work");
            Order(c, game, new ConfigurePlanCommand { Plan = plan, Group = "construction.proof", Priority = 70, Paused = true });
            var held = Nodes(game, plan).Select(n => n.Progress).ToArray(); var twin = factory.Load(game.Save());
            for (int i = 0; i < 4; i++) Step(game, twin);
            c.That(Nodes(game, plan).Select(n => n.Progress).SequenceEqual(held), "paused original order retains earned node progress");
            ulong hash = game.Hash(); long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == hash, "sole game clock freezes actual site");
            game.Clock.Resume(); twin.Clock.Resume();
            Order(c, game, new ConfigurePlanCommand { Plan = plan, Group = "construction.proof", Priority = 70, Paused = false });
            twin = factory.Load(game.Save());
            Reach(game, twin, () => Plans(game).Single(p => p.Id == plan).State == PlanState.Done, "saved physical construction completes");
            long built = Plans(game).Single(p => p.Id == plan).EndedTick;
            c.That(Has(game, "point.tower") && !Has(game, "point.tower-site")
                && Stock(game, "source") == Fix.FromInt(12) && Stock(game, "machine") == Fix.Zero, "real supply20 to12 earns one completed structure, no duplicated cargo");
            c.That(retained.All(n => n.ProductiveSteps == 0), "retained node facts remain detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed structure saves");
            Order(c, game, new SubmitPlanCommand { Intent = Build(30, true) });
            long cancelled = Plans(game).Max(p => p.Id);
            Order(c, game, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { cancelled }) });
            c.That(Plans(game).Single(p => p.Id == cancelled).State == PlanState.Cancelled && !Has(game, "point.tower-site"), "pending cancellation manufactures no project");
            Order(c, game, new ConstructCommand { Def = "point.tower", Placement = Placement(50), Priority = 70 });
            c.That(Has(game, "point.tower-site"), "ordinary direct placement uses same contract/site owner");
            Order(c, game, new CancelConstructionCommand { Site = Entity(game, "point.tower-site") });
            Reach(game, null, () => !Has(game, "point.tower-site"), "ordinary empty-site cancellation");
            c.That(Stock(game, "source") == Fix.FromInt(12), "cancelled direct site grants no stock or construction completion");
            Order(c, game, new DemolishCommand { Target = Entity(game, "point.tower") });
            c.That(Has(game, "point.tower"), "demolition not instant removal");
            twin = factory.Load(game.Save()); Reach(game, twin, () => !Has(game, "point.tower"), "original earned removal workflow");
            long removed = game.Clock.StepIndex;
            c.That(Stock(game, "source") == Fix.FromInt(12) && Stock(game, "machine") == Fix.Zero, "explicit empty salvage creates no synthetic yield");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary plans and physical construction/removal replay");
            var fresh = New(factory);
            c.That(Stock(fresh, "source") == Fix.FromInt(20) && Plans(fresh).Count == 0 && !Has(fresh, "point.tower-site"), "interleaved session retains separate projects and stock");
            return new[] { built, removed };
        }
        private static void Invalid(Check c)
        {
            foreach (string fault in new[] { "site", "activity", "state", "removal", "salvage", "unknown-site", "null-state", "null-target", "wrong-codec", "unbound", "orphan", "extra" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, edit: root => {
                    var target = root.Get("entities")[0].Clone(); target.Set("id", JsonValue.Of("unused.tower"));
                    var site = root.Get("entities")[1].Clone(); site.Set("id", JsonValue.Of("unused.site"));
                    var construction = target.Get("components").Get("independent_construction");
                    var binding = site.Get("components").Get("independent_site");
                    construction.Set("site", JsonValue.Of("unused.site")); binding.Set("target", JsonValue.Of("unused.tower"));
                    root.Get("entities").Add(target); root.Get("entities").Add(site);
                    if (fault == "site" || fault == "activity") construction.Remove(fault);
                    if (fault == "state") construction.Remove("states");
                    if (fault == "removal") construction.Get("demolish").Remove("activity");
                    if (fault == "salvage") construction.Get("demolish").Remove("salvage");
                    if (fault == "unknown-site") construction.Set("site", JsonValue.Of("missing"));
                    if (fault == "null-state") construction.Set("metadata_fault", JsonValue.Of(fault));
                    if (fault == "null-target") binding.Set("metadata_fault", JsonValue.Of(fault));
                    if (fault == "wrong-codec") Rename(site.Get("components"), "independent_site", "wrong_site_codec");
                    if (fault == "unbound") Rename(site.Get("components"), "independent_site", "unbound_site");
                    if (fault == "orphan") binding.Set("target", JsonValue.Of("point.tower"));
                    if (fault == "extra") construction.Set("host_progress", JsonValue.Number("9"));
                })), "invalid unused authored construction binding refuses " + fault);
                c.Equal(0, initialize.Initializations, "unused malformed content refused before Initialize " + fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach (string fault in new[] { "throw", "pause", "order", "touch", "reenter" })
            {
                var factory = Factory(new InitializeProbe(), out var proof); var game = New(factory);
                Order(c, game, new ConstructCommand { Def = "point.tower", Placement = Placement(12), Priority = 70 });
                proof.ArmFault(fault); game.Update();
                c.That(game.Fault != null, "actual construction metadata fault latches " + fault);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted site cannot publish healthy save " + fault);
                c.Throws<InvalidOperationException>(() => game.Hash(), "faulted site cannot publish healthy hash " + fault);
                if (fault == "throw") c.That(game.Fault.Message.Contains("proof.construction.site-state"), "first callback failure remains original");
            }
        }
    }
}
