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
    internal static class HandlingContentContractTests
    {
        internal const string Case = "Handling content contracts: independent JSON rates, physical prefixes and guarded callbacks";
        private static readonly ReplayLimits Limits = new ReplayLimits(4 * 1024 * 1024, 3000);
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(InitializeProbe initialize, out HandlingContractModule proof,
            string selected = "contract.handling", Action<JsonValue> edit = null, Action<JsonValue> rootEdit = null, GameModule extraModule = null)
        {
            proof = new HandlingContractModule(initialize);
            var profiles = Json.Parse(FrameworkProofContent.Read("handling-contracts.json")); edit?.Invoke(profiles);
            return ConstructionPlanTests.Points(extraModules: extraModule == null ? new GameModule[] { proof } : new GameModule[] { proof, extraModule },
                extraDeclarations: "[{\"id\":\"proof.handling-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"handling-contracts.json\"]}" +
                    (extraModule == null ? "" : ",{\"id\":\"" + extraModule.Id + "\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[]}") + "]",
                extraDocuments: new Dictionary<string, string> { { "handling-contracts.json", profiles.ToString() } },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(HandlingContractModule.ModuleId));
                    if (extraModule != null) scenario.Get("modules").Add(JsonValue.Of(extraModule.Id));
                },
                worldEdit: root =>
                {
                    StartupContentAuthoring.Position(root, "source", JsonValue.Number("20"));
                    root.Get("entities")[2].Get("components").Get("transporter").Set("handling", selected == null ? JsonValue.Null() : JsonValue.Of(selected));
                    rootEdit?.Invoke(root);
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        internal static EntityId Entity(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == id).Id;
        internal static Fix Stock(SimGame game, string id)
        { var values = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, id), values); return values.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (a, b) => a + b.Value); }
        internal static Fix Position(SimGame game) => ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Entity(game, "machine")).Spatial).Position;
        internal static List<WorkJobView> Jobs(SimGame game, int seat = 0)
        { var jobs = new List<WorkJobView>(); game.Seat(seat).View.Feature<WorkView>().CopyJobs(jobs); return jobs; }
        internal static WorkJobView Job(SimGame game, int id) => Jobs(game).Single(j => j.Id == id);
        internal static void Step(Check c, SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("handling step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
            c.Equal(Fix.FromInt(20), Stock(game, "source") + Stock(game, "machine") + Stock(game, "destination"), "actual native stock conserved each update");
        }
        internal static void Until(Check c, SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 400 && !ready(); i++) Step(c, game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " jobs=" +
                string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.ToilIndex + "/" + j.Loaded + "/" + j.Blocker)));
        }
        internal static CommandResult Order(Check c, SimGame game, Command command)
        { ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player preview"); c.Equal(hash, game.Hash(), "preview pure"); var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal command accepted"); return result; }
        internal static void Policy(Check c, SimGame game, bool enabled)
            => Order(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
                Settings = new WorkPolicySettings(enabled, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current build identity");
            c.Equal(154, EngineInfo.SaveFormat, "same physical/job/receipt codecs");
            Physical(c, "contract.handling", 12, 8); Physical(c, "contract.slots", 10, 6);
            FinerTime(c); Invalid(c); Faults(c);
        }
        private static void Physical(Check c, string selected, long loading, long unloading)
        {
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var proof, selected); var game = New(factory);
            c.That(proof.IndependentAlias, "actual owned contract without mandatory native inheritance");
            c.Equal(1, initialize.Initializations, "valid Initialize once");
            c.That(game.Seat(0).View.TryFindResource("supply", out var supply), "actual resource identity");
            var recorder = game.RecordReplay(Limits); Policy(c, game, true);
            var command = new HaulResourcesCommand { Source = Entity(game, "source"), Destination = Entity(game, "destination"),
                Worker = Entity(game, "machine"), Resource = supply, Ceiling = Fix.FromInt(8), Priority = 20 };
            int id = checked((int)Order(c, game, command).Arg0);
            c.Equal(Fix.Zero, Stock(game, "machine"), "no cargo before physical approach");
            Until(c, game, null, () => Stock(game, "machine") > Fix.Zero && Stock(game, "machine") < Fix.FromInt(8), "partial loading");
            var retained = Job(game, id); ulong hash = game.Hash(); Fix partial = Stock(game, "machine");
            c.Equal(loading, retained.HandlingRequiredTicks.Value, "engine-calculated exact full loading duration");
            c.That(retained.Loaded == partial && retained.HandledAmount == partial && retained.HandlingProgress.WholeTicks > 0, "actual prefix and progress are native job facts");
            c.Equal(Fix.FromInt(20), Position(game), "loading only after real source arrival");
            c.That(game.Clock.StepIndex > 20, "real approach takes time");
            c.Equal(0, Jobs(game, 1).Count, "partial cargo jobs private");
            c.Equal(hash, game.Hash(), "private handling projections pure");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial loading saved");
            game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long time = game.Clock.StepIndex;
            for (int i = 0; i < 3; i++) Step(c, game, twin);
            c.That(game.Clock.StepIndex == time && game.Hash() == hash && Stock(game, "machine") == partial, "sole clock freezes handling and cargo");
            game.Clock.Resume(); twin.Clock.Resume();
            Policy(c, game, false); twin = factory.Load(game.Save()); var held = Job(game, id); partial = Stock(game, "machine");
            for (int i = 0; i < 4; i++) Step(c, game, twin);
            c.That(Job(game, id).HandlingProgress.Equals(held.HandlingProgress) && Stock(game, "machine") == partial && Position(game) == Fix.FromInt(20), "ordinary disabled work policy retains prefix/position/progress");
            Policy(c, game, true); twin = factory.Load(game.Save());
            Until(c, game, twin, () => Stock(game, "machine") == Fix.FromInt(8), "completed loading");
            c.That(Stock(game, "source") == Fix.FromInt(12) && Stock(game, "destination") == Fix.Zero, "full cargo does not teleport to destination");
            twin = factory.Load(game.Save());
            Until(c, game, twin, () => Stock(game, "destination") > Fix.Zero && Stock(game, "destination") < Fix.FromInt(8), "partial unloading");
            var delivery = Job(game, id);
            c.Equal(unloading, delivery.HandlingRequiredTicks.Value, "engine-calculated full unloading duration");
            c.That(delivery.Loaded == Stock(game, "machine") && delivery.HandledAmount == Stock(game, "destination"), "actual progressive delivery through original receipts");
            c.Equal(Fix.FromInt(10), Position(game), "unload only at real destination");
            c.That(retained.Loaded < Fix.FromInt(8) && retained.HandlingRequiredTicks == loading, "retained loading facts unchanged");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial unloading saved");
            Until(c, game, twin, () => Stock(game, "destination") == Fix.FromInt(8) && Stock(game, "machine") == Fix.Zero, "complete delivery");
            c.That(Stock(game, "source") == Fix.FromInt(12) && game.Clock.StepIndex > 20 + loading + unloading, "elapsed travel/service and exact final stock");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "delivered checkpoint continuation");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "normal haul/policy/clock commands replay");
            var fresh = New(factory); c.That(Stock(fresh, "source") == Fix.FromInt(20) && Stock(fresh, "destination") == Fix.Zero && Jobs(fresh).Count == 0, "fresh interleaved session isolated");
        }
        private static void FinerTime(Check c)
        {
            var game = New(Factory(new InitializeProbe(), out _, rootEdit: root => root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"))));
            Policy(c, game, true); game.Seat(0).View.TryFindResource("supply", out var supply);
            int id = checked((int)Order(c, game, new HaulResourcesCommand { Source = Entity(game, "source"), Destination = Entity(game, "destination"), Worker = Entity(game, "machine"), Resource = supply, Ceiling = Fix.FromInt(8), Priority = 20 }).Arg0);
            Until(c, game, null, () => Stock(game, "machine") > Fix.Zero, "finer-time approach");
            c.Equal(24L, Job(game, id).HandlingRequiredTicks.Value, "same authored ratio resolves against selected30-second clock");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "rows", "empty", "quantity", "fractional", "duplicate", "resource", "loading", "zero", "negative", "denominator", "unit", "unused" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, edit: json =>
                {
                    var profile = json.Get("independent_handling_profiles")[mode == "unused" ? 1 : 0];
                    var row = profile.Get("resources")[0];
                    if (mode == "rows") profile.Remove("resources");
                    if (mode == "empty") profile.Set("resources", JsonValue.NewArray());
                    if (mode == "quantity") row.Set("quantity", JsonValue.Number("0"));
                    if (mode == "fractional") row.Set("quantity", JsonValue.Number("1.5"));
                    if (mode == "duplicate") profile.Get("resources").Add(row.Clone());
                    if (mode == "resource" || mode == "unused") row.Set("resource", JsonValue.Of("missing.resource"));
                    if (mode == "loading") row.Remove("loading");
                    if (mode == "zero") row.Get("loading").Set("numerator", JsonValue.Number("0"));
                    if (mode == "negative") row.Get("unloading").Set("numerator", JsonValue.Number("-1"));
                    if (mode == "denominator") row.Get("loading").Set("denominator", JsonValue.Number("0"));
                    if (mode == "unit") row.Get("loading").Set("unit", JsonValue.Of("unknown"));
                })), "invalid shared handling declaration: " + mode);
                c.Equal(0, initialize.Initializations, "before Initialize: " + mode);
            }
            foreach (string selected in new[] { "missing.handling", "source", null })
            {
                var initialize = new InitializeProbe(); c.Throws<ContentException>(() => New(Factory(initialize, out _, selected)), "missing/null/wrong-kind handler");
                c.Equal(0, initialize.Initializations, "bad selected handler before Initialize");
            }
        }
        private static void Faults(Check c)
        {
            foreach (string mode in new[] { "pause", "throw", "default" })
            {
                var game = New(Factory(new InitializeProbe(), out _, "contract." + mode + "-fault"));
                Policy(c, game, true); game.Seat(0).View.TryFindResource("supply", out var supply);
                var source = Entity(game, "source"); var destination = Entity(game, "destination");
                c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source, destination, supply, Fix.FromInt(8)), "guarded actual duration callback: " + mode);
                c.That(game.Fault != null, "provider failure latches actual session: " + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted callback cannot checkpoint: " + mode);
            }
        }
    }
}
