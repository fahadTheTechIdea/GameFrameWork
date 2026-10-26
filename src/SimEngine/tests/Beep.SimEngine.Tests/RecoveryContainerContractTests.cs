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
    // Ordinary player inputs and detached facts only. No stock/progress/geometry gameplay here.
    internal static class RecoveryContainerContractTests
    {
        internal const string Case = "Recovery container contracts: supplied cancellation and earned salvage share physical disposition";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe initialize, out RecoveryContainerProofModule proof,
            bool independent, Action<JsonValue> edit = null, Action<JsonValue> constructionEdit = null, string selection = "container.contract")
        {
            proof = new RecoveryContainerProofModule(initialize);
            var document = Json.Parse(FrameworkProofContent.Read("recovery-container-contracts.json"));
            if (!independent)
            { var rows = document.Get("independent_recovery_containers"); document.Remove("independent_recovery_containers"); document.Set("resource_recovery_profiles", rows); }
            edit?.Invoke(document);
            return ConstructionPlanTests.Points(recovery: selection,
                extraModules: new GameModule[] { new LogisticsRecoveryPolicyModule(), proof },
                extraDeclarations: "[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"container-return-policy.json\"]},{\"id\":\"proof.recovery-containers\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.construction\",\"version\":\"1.0.0\"}],\"documents\":[\"recovery-container-contracts.json\"]}]",
                extraDocuments: new Dictionary<string,string> { { "recovery-container-contracts.json", document.ToString() }, { "container-return-policy.json", FrameworkProofContent.Read("logistics-recovery.json") } },
                flows: FrameworkProofContent.Read("recovery-flows.json"),
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of("proof.recovery-containers"));
                    scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
                    scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"recovery.high\"}"));
                },
                edit: root => {
                    // Explicit proof content: inputs are needed by the first node, while its inspect work is disabled.
                    root.Get("activities")[0].Set("nodes", Json.Parse("[{\"executor\":\"work.staffed\",\"id\":\"build\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":1,\"unit\":\"hour\"},\"requirement\":\"point.build-inputs\"}]"));
                    root.Get("entities")[0].Get("components").Get("construction").Get("demolish").Set("salvage", Json.Parse("{\"supply\":6}"));
                    constructionEdit?.Invoke(root);
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static ConstructionView Construction(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<ConstructionView>();
        private static List<WorkJobView> Jobs(SimGame game)
        { var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
        private static bool ActiveJobs(SimGame game) => Jobs(game).Any(j => j.State == JobState.Assigned || j.State == JobState.Paused);
        private static bool Has(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Any(e => e.Def == definition);
        private static EntityView[] Containers(SimGame game) => game.Seat(0).Snapshot.Entities.Where(e => e.Def == "point.recovery-container").ToArray();
        private static Fix Amount(SimGame game, EntityId entity)
        { var facts = new List<Facet>(); game.Seat(0).View.Inspect(entity, facts); return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value); }
        private static Fix Total(SimGame game) => game.Seat(0).Snapshot.Entities.Aggregate(Fix.Zero, (sum, entity) => sum + Amount(game, entity.Id));
        private static ResourceFlowPage Flows(SimGame game, long cursor = 0) => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor);
        private static JsonValue Placement(int at) => Json.Parse("{\"space\":\"field.points\",\"position\":" + at + "}");
        private static void Worker(Check c, SimGame game, bool inspect)
            => Order(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
                Settings = new WorkPolicySettings(true, 0, inspect ? new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") } : new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " update=" + game.Clock.StepIndex
                + " total=" + Total(game) + " cargo=" + Stock(game, "machine") + " containers=" + Containers(game).Length
                + " jobs=" + string.Join(",", Jobs(game).Select(j => j.Id + ":" + j.State + ":" + j.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual recovery contract protocol");
            c.Equal(154, EngineInfo.SaveFormat, "original stock/site/work codecs unchanged");
            var native = Physical(c, false); var direct = Physical(c, true);
            c.That(native.SequenceEqual(direct), "native and independent profiles earn identical physical completion coordinates");
            Invalid(c); ExplicitNone(c);
        }
        private static long[] Physical(Check c, bool independent)
        {
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var proof, independent); var game = New(factory);
            c.That(proof.ActualAliases, "actual selected catalog profile shares one original construction/recovery owner");
            c.Equal(1, initialize.Initializations, "validated content initializes once");
            c.That(!game.Modules.Contains("sim.tycoon"), "physical work needs no optional business module");
            c.Equal("point.recovery-container", Construction(game).RecoveryContainer, "view exposes actual authored prototype identity");
            ulong hash = game.Hash();
            c.Equal(Code.None, Construction(game).CheckRecovery(Entity(game, "source")), "selected native geometry admits real source stock");
            c.Equal(Codes.NotOwned, Construction(game, 1).CheckRecovery(Entity(game, "source")), "foreign company cannot inspect private stock disposition");
            c.Equal(hash, game.Hash(), "whole stock preview allocates no containers or receipts");
            Worker(c, game, false);
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits);
            Order(c, game, new ConstructCommand { Def = "point.tower", Placement = Placement(12), Priority = 70 });
            c.That(!Has(game, "point.tower") && Containers(game).Length == 0, "ordinary placement creates only an unbuilt physical site");
            Reach(game, null, () => Stock(game, "point.tower-site") == Fix.FromInt(8) && !ActiveJobs(game), "real eight-unit delivery before cancellation");
            c.That(Stock(game, "source") == Fix.FromInt(12) && Total(game) == Fix.FromInt(20) && game.Clock.StepIndex > 12, "haul-only worker moves materials without constructing or consuming");
            var twin = factory.Load(game.Save());
            hash = game.Hash(); long before = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(game.Hash() == hash && game.Clock.StepIndex == before, "one clock pauses stocked site and physical carrier");
            game.Clock.Resume(); twin.Clock.Resume();
            long cursor = Flows(game).Latest;
            Order(c, game, new CancelConstructionCommand { Site = Entity(game, "point.tower-site") });
            twin = factory.Load(game.Save());
            c.That(!Has(game, "point.tower-site") && !Has(game, "point.tower") && Containers(game).Length == 2, "cancelled eight units occupy two actual authored four-unit containers");
            var retained = Containers(game);
            c.That(retained.All(e => ((PointSnapshot)e.Spatial).Position == Fix.FromInt(12)) && Total(game) == Fix.FromInt(20), "material stays at real cancelled location, not refunded to source");
            var disposition = Flows(game, cursor);
            c.That(disposition.Entries.All(e => e.Item.Kind == ResourceFlowKind.StockTransfer)
                && disposition.Entries.Aggregate(Fix.Zero, (n, e) => n + e.Item.Amount) == Fix.FromInt(8), "cancellation moves actual stock, never fabricated produced yield");
            Reach(game, twin, () => Containers(game).Length == 0 && !ActiveJobs(game), "real later cancelled-material collection and delivery");
            long returned = game.Clock.StepIndex;
            c.That(Total(game) == Fix.FromInt(20) && Stock(game, "machine") == Fix.Zero && retained.Length == 2, "physical return conserves every unit and retained facts remain detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "finished cancellation saves original receipts/state");

            Worker(c, game, true);
            Order(c, game, new ConstructCommand { Def = "point.tower", Placement = Placement(12), Priority = 70 });
            Reach(game, null, () => Has(game, "point.tower"), "normal supplied machine construction after cancellation");
            c.That(Total(game) == Fix.FromInt(12) && !Has(game, "point.tower-site"), "one earned build consumes exactly eight actual units");
            long built = game.Clock.StepIndex; cursor = Flows(game).Latest;
            Order(c, game, new DemolishCommand { Target = Entity(game, "point.tower") });
            c.That(Has(game, "point.tower") && Containers(game).Length == 0, "demolition command creates no instant salvage or removal");
            twin = factory.Load(game.Save());
            Reach(game, twin, () => !Has(game, "point.tower"), "machine earns six-minute demolition and explicit structural yield");
            long removed = game.Clock.StepIndex;
            c.That(removed > built + 1 && Containers(game).Length == 2 && Total(game) == Fix.FromInt(18), "earned six-unit salvage packs four plus two at real structure location");
            var produced = Flows(game, cursor).Entries.Where(e => e.Item.Cause == new Code("demolish:point.tower")).ToArray();
            c.That(produced.All(e => e.Item.Kind == ResourceFlowKind.Produced)
                && produced.Aggregate(Fix.Zero, (sum, e) => sum + e.Item.Amount) == Fix.FromInt(6), "explicit demolition cause reports exactly six produced units once");
            Reach(game, twin, () => Containers(game).Length == 0 && !ActiveJobs(game), "physical salvage collection and final delivery");
            c.That(Total(game) == Fix.FromInt(18) && Stock(game, "machine") == Fix.Zero, "hauling changes location only, never reproduces salvage");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed disposition chain durable");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary cancellation/work/demolition commands replay through engine");
            var fresh = New(factory); c.That(Total(fresh) == Fix.FromInt(20) && Containers(fresh).Length == 0, "interleaved session has independent stock and disposition");
            return new[] { returned, built, removed, game.Clock.StepIndex };
        }
        private static void Invalid(Check c)
        {
            foreach (bool independent in new[] { false, true })
                foreach (string fault in new[] { "space", "container", "unknown-space", "unknown-container", "stocked", "inventory", "extra", "unused" })
                {
                    var initialize = new InitializeProbe();
                    c.Throws<ContentException>(() => New(Factory(initialize, out _, independent, edit: root => {
                        var row = root.Get(independent ? "independent_recovery_containers" : "resource_recovery_profiles")[fault == "unused" ? 1 : 0];
                        if (fault == "space" || fault == "container") row.Remove(fault);
                        if (fault == "unknown-space") row.Set("space", JsonValue.Of("missing"));
                        if (fault == "unknown-container") row.Set("container", JsonValue.Of("missing"));
                        if (fault == "stocked" || fault == "unused") row.Set("container", JsonValue.Of("source"));
                        if (fault == "inventory") row.Set("container", JsonValue.Of("point.empty-recovery"));
                        if (fault == "extra") row.Set("host_pack", JsonValue.Number("4"));
                    }, constructionEdit: root => root.Get("entities").Add(Json.Parse("{\"id\":\"point.empty-recovery\",\"kind\":\"entity\",\"components\":{\"point_presence\":{\"space\":\"field.points\",\"position\":0}}}")))), "invalid loaded profile refuses " + independent + "/" + fault);
                    c.Equal(0, initialize.Initializations, "before Initialize " + independent + "/" + fault);
                }
            foreach (string fault in new[] { "foreign-space", "null-container" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, true, edit: root => root.Get("independent_recovery_containers")[1].Set("metadata_fault", JsonValue.Of(fault)))), "unused direct contract getter metadata refuses " + fault);
                c.Equal(0, initialize.Initializations, "actual contract admission before Initialize " + fault);
            }
        }
        private static void ExplicitNone(Check c)
        {
            var game = New(ConstructionPlanTests.Points(recovery: null));
            ulong hash = game.Hash();
            c.Equal(null, Construction(game).RecoveryContainer, "explicit null creates no default profile");
            c.Equal(ResourceTransactions.MissingRecovery, Construction(game).CheckRecovery(Entity(game, "source")), "stocked missing recovery remains typed and retained");
            c.Equal(Code.None, Construction(game).CheckRecovery(Entity(game, "destination")), "empty removal needs no invented container");
            c.Equal(hash, game.Hash(), "explicit missing diagnostics pure");
        }
    }
}
