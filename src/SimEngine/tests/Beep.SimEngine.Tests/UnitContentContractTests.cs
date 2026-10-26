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
    // JSON authoring and ordinary player/clock/Update/view/save/replay input only.
    internal static class UnitContentContractTests
    {
        internal const string Case = "Unit content contracts: independent JSON units, exact measures and physical execution";
        private static readonly ReplayLimits Limits = new ReplayLimits(4 * 1024 * 1024, 3000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe initialize, out UnitContractModule units, out CapacityContractModule capacity,
            Action<JsonValue> edit = null, Action<JsonValue> rootEdit = null)
        {
            units = new UnitContractModule(); capacity = new CapacityContractModule(initialize);
            var document = Json.Parse(FrameworkProofContent.Read("units-contracts.json")); edit?.Invoke(document);
            return ConstructionPlanTests.Points(extraModules: new GameModule[] { capacity, units },
                extraDeclarations: "[{\"id\":\"proof.capacity-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"capacity-contracts.json\"]},{\"id\":\"proof.unit-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"units-contracts.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "units-contracts.json", document.ToString() }, { "capacity-contracts.json", FrameworkProofContent.Read("capacity-contracts.json") } },
                startupProfiles: new[] { "contract.capacity-start" },
                scenarioEdit: scenario => { scenario.Get("modules").Add(JsonValue.Of(CapacityContractModule.ModuleId)); scenario.Get("modules").Add(JsonValue.Of(UnitContractModule.ModuleId)); },
                worldEdit: root =>
                {
                    root.Set("units", JsonValue.NewArray()); // Exactly one explicit definition of each unit; no overlay or replacement at runtime.
                    StartupContentAuthoring.Position(root, "source", JsonValue.Number("20"));
                    root.Get("entities")[0].Get("components").Get("point_stock").Set("capacity_policy", JsonValue.Of("contract.source"));
                    root.Get("entities")[1].Get("components").Get("point_stock").Set("capacity_policy", JsonValue.Of("contract.destination"));
                    root.Get("entities")[2].Get("components").Get("storage").Set("capacity_policy", JsonValue.Of("contract.carrier"));
                    rootEdit?.Invoke(root);
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static EntityId Entity(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == id).Id;
        private static Fix Stock(SimGame game, string id)
        {
            var facts = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, id), facts);
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static Fix Position(SimGame game) => ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").Spatial).Position;
        private static bool LiveJob(SimGame game, int id)
        { var facts = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(facts); return facts.Any(f => f.Id == id); }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("unit step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 480 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " source=" + Stock(game, "source") +
                " destination=" + Stock(game, "destination") + " cargo=" + Stock(game, "machine"));
        }
        private static CommandResult Order(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player preview");
            c.Equal(hash, game.Hash(), "preview does not mutate actual session");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal player command"); return result;
        }
        private static void Enable(Check c, SimGame game) => Order(c, game, new ConfigureWorkPolicyCommand
        {
            Worker = Entity(game, "machine"),
            Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All)
        });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current authored-unit composition identity");
            c.Equal(154, EngineInfo.SaveFormat, "original saved units/resources/WIP/jobs unchanged");
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var units, out var capacity); var game = New(factory);
            c.That(units.OwnedAliases && units.OriginalBindings && capacity.IndependentAlias && capacity.NativeParity, "actual direct unit aliases feed original resources/limits and exact native/direct policy math");
            c.Equal(1, initialize.Initializations, "one Initialize after complete unit metadata validation");
            c.That(units.NormalizedNumerator == 1 && units.NormalizedDenominator == 1000, "one shared reader normalizes exact authored scale");
            var conversions = units.Conversions;
            c.That(conversions[0].Status == UnitConversionStatus.Converted && conversions[0].Amount == Fix.FromInt(1000), "one native mass becomes exact thousand grams");
            c.That(conversions[1].Status == UnitConversionStatus.Converted && conversions[1].Amount == Fix.One, "three native mass converts to one authored third-scale unit");
            c.Equal(UnitConversionStatus.Unrepresentable, conversions[2].Status, "inexact rational conversion refuses without rounding");
            c.Equal(UnitConversionStatus.FractionalDiscrete, conversions[3].Status, "fractional target pair refuses");
            c.That(conversions[4].Status == UnitConversionStatus.Converted && conversions[4].Amount == Fix.One, "two native counts become one discrete pair");
            c.Equal(UnitConversionStatus.OutOfRange, conversions[5].Status, "wide conversion has explicit representational failure");
            c.That(conversions[6].Status == UnitConversionStatus.Converted && conversions[6].Amount == Fix.One, "wide intermediate cancels exactly without hidden scale ceiling");
            c.Equal(UnitConversionStatus.IncompatibleDimension, conversions[7].Status, "mass never silently becomes volume");
            c.Equal(UnitConversionStatus.InvalidAmount, conversions[8].Status, "negative native amount refuses");
            c.Equal(UnitConversionStatus.FractionalDiscrete, conversions[9].Status, "fractional native count refuses");
            c.That(capacity.Mixed.Fits && capacity.MixedOffer.Amount == Fix.FromInt(2) && capacity.FractionalOffer.Amount == Fix.FromInt(2), "all measured and packing math uses actual direct units");
            var facts = new List<UnitInfo>(); ulong hash = game.Hash(); game.Seat(0).View.CopyUnits(facts);
            var grams = facts.Single(f => f.Unit == new UnitId("unit.contract.g"));
            c.That(facts.Select(f => f.Unit).SequenceEqual(facts.Select(f => f.Unit).OrderBy(id => id)), "copied units stably sorted");
            c.That(grams.Dimension == "mass" && grams.ScaleNumerator == 1 && grams.ScaleDenominator == 1000, "public immutable metadata includes independent unit");
            var resources = new List<ResourceInfo>(); game.Seat(0).View.CopyResources(resources);
            c.That(resources.Single(r => r.Id == "supply").QuantityUnit.Unit == new UnitId("unit.supply"), "original resource projection carries actual authored unit");
            c.Equal(hash, game.Hash(), "unit/resource metadata observations pure");
            Physical(c, factory, game); Manufacturing(c, factory); Invalid(c);
            c.That(grams.ScaleNumerator == 1 && grams.ScaleDenominator == 1000, "retained detached unit facts unchanged after advances");
        }
        private static void Physical(Check c, GameFactory factory, SimGame game)
        {
            Enable(c, game); var recorder = game.RecordReplay(Limits); game.Seat(0).View.TryFindResource("supply", out var supply);
            var source = Entity(game, "source"); var destination = Entity(game, "destination");
            var offer = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source, destination, supply, Fix.FromInt(20));
            c.That(offer.Status == HaulStatus.Ready && offer.Amount == Fix.FromInt(8) && offer.QuantityQuantum.Raw == Fix.One.Raw, "direct count units control real carrier admission");
            int job = checked((int)Order(c, game, new HaulResourcesCommand { Source = source, Destination = destination, Worker = Entity(game, "machine"), Resource = supply, Ceiling = Fix.FromInt(20), Priority = 20 }).Arg0);
            Until(game, null, () => Stock(game, "machine") > Fix.Zero && Stock(game, "machine") < Fix.FromInt(8), "partial loading in direct native quantity unit");
            c.That(Position(game) == Fix.FromInt(20) && Stock(game, "destination") == Fix.Zero, "cargo loads only after physical source arrival");
            c.Equal(Fix.FromInt(20), Stock(game, "source") + Stock(game, "machine") + Stock(game, "destination"), "same native stock owner conserves initial material");
            var twin = factory.Load(game.Save()); long time = game.Clock.StepIndex; ulong hash = game.Hash();
            game.Clock.Pause(); twin.Clock.Pause(); for (int i = 0; i < 3; i++) Step(game, twin);
            c.That(game.Clock.StepIndex == time && game.Hash() == hash, "sole clock freezes unit-based physical commitments");
            game.Clock.Resume(); twin.Clock.Resume();
            Until(game, twin, () => Stock(game, "destination") > Fix.Zero && Stock(game, "destination") < Fix.FromInt(8), "partial destination delivery");
            c.Equal(Fix.FromInt(10), Position(game), "delivery only at actual destination");
            twin = factory.Load(game.Save());
            Until(game, twin, () => Stock(game, "destination") == Fix.FromInt(8) && Stock(game, "machine") == Fix.Zero && !LiveJob(game, job), "ordinary job completes and releases worker");
            c.That(Stock(game, "source") == Fix.FromInt(12) && game.Clock.StepIndex > 20, "physical haul consumes game time and preserves exact quantities");
            var privateJobs = new List<WorkJobView>(); game.Seat(1).View.Feature<WorkView>().CopyJobs(privateJobs);
            c.Equal(0, privateJobs.Count, "foreign company receives no cargo job");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "direct-unit physical checkpoint exact");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "normal hauling and clock control replay with exact unit manifest");
            var fresh = New(factory); c.That(Stock(fresh, "source") == Fix.FromInt(20) && Stock(fresh, "destination") == Fix.Zero, "interleaved session stock remains independent");
        }
        private static TransformationSnapshot Process(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Entity(game, "contract.factory"), new Code("contract.process"), out var value))
                throw new InvalidOperationException("missing named factory");
            return value;
        }
        private static void Manufacturing(Check c, GameFactory factory)
        {
            var game = New(factory); Enable(c, game); var recorder = game.RecordReplay(Limits);
            Order(c, game, new EnqueueTransformationCommand { Transformer = Entity(game, "contract.factory"), Capability = new Code("contract.process"), Transformation = "contract.make" });
            Until(game, null, () => Process(game).Phase == TransformationPhase.Working, "original physically supplied WIP");
            var retained = Process(game);
            c.That(retained.QueueCount == 1 && retained.Required == 30 && game.Seat(0).Snapshot.Entities.All(e => e.Def != "contract.result"), "direct-unit inputs do not skip authored manufacturing time");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Entity(game, "contract.factory"), new Code("contract.process"), out _), "actual WIP private");
            var twin = factory.Load(game.Save());
            Until(game, twin, () => Process(game).QueueCount == 0 && game.Seat(0).Snapshot.Entities.Any(e => e.Def == "contract.result"), "physical manufacturing result");
            c.That(Stock(game, "source") == Fix.FromInt(16) && Stock(game, "contract.factory") == Fix.One && Stock(game, "machine") == Fix.Zero, "original native-unit owners consume three inputs and return reusable tool");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "direct-unit completed manufacturing checkpoint");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "manufacturing player inputs replay");
            c.That(retained.QueueCount == 1 && retained.Required == 30, "retained WIP facts unchanged");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "dimension", "empty-dimension", "scale", "null-scale", "numerator", "denominator", "fractional", "discrete", "extra", "identity", "unused" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, out _, edit: root =>
                {
                    var unit = root.Get("independent_units")[mode == "unused" ? 8 : 0];
                    if (mode == "dimension") unit.Remove("dimension");
                    if (mode == "empty-dimension") unit.Set("dimension", JsonValue.Of(" "));
                    if (mode == "scale") unit.Remove("scale");
                    if (mode == "null-scale") unit.Set("scale", JsonValue.Null());
                    if (mode == "numerator" || mode == "unused") unit.Get("scale").Set("numerator", JsonValue.Number("0"));
                    if (mode == "denominator") unit.Get("scale").Set("denominator", JsonValue.Number("-1"));
                    if (mode == "fractional") unit.Get("scale").Set("numerator", JsonValue.Number("1.5"));
                    if (mode == "discrete") unit.Remove("discrete");
                    if (mode == "extra") unit.Set("rounded_scale", JsonValue.Number("1"));
                    if (mode == "identity") unit.Set("id", JsonValue.Of("Bad Unit"));
                })), "malformed shared declaration: " + mode);
                c.Equal(0, initialize.Initializations, "malformed authored unit before Initialize: " + mode);
            }
            foreach (string mode in new[] { "identity", "dimension", "numerator", "denominator", "unreduced" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, out _, edit: root =>
                {
                    var unit = root.Get("independent_units")[8].Clone(); unit.Set("id", JsonValue.Of("unit.invalid.unused")); unit.Set("mode", JsonValue.Of(mode));
                    root.Get("invalid_units").Add(unit);
                })), "actual independent metadata validation: " + mode);
                c.Equal(0, initialize.Initializations, "unused invalid provider refuses before consumers/Initialize");
            }
            foreach (string reference in new[] { "missing.unit", "source" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, out _, rootEdit: root => root.Get("resources")[0].Set("unit", JsonValue.Of(reference)))), "missing/wrong-kind quantity unit");
                c.Equal(0, initialize.Initializations, "invalid resource unit before Initialize");
            }
            var duplicate = new InitializeProbe();
            c.Throws<ContentException>(() => New(Factory(duplicate, out _, out _, rootEdit: root => root.Get("units").Add(Json.Parse("{\"id\":\"unit.capacity.count\",\"dimension\":\"count\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":true}")))), "two content kinds cannot masquerade as one unit contract");
            c.Equal(0, duplicate.Initializations, "ambiguous unit refuses before Initialize");
        }
    }
}
