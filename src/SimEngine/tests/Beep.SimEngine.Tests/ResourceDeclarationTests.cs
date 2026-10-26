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
    // Explicit JSON authoring, public commands/views, one clock and normal Update only.
    internal static class ResourceDeclarationTests
    {
        internal const string Case = "Resource declarations: shared JSON metadata and timed native hauling";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(ResourceDeclarationModule module, Action<JsonValue> edit = null)
        {
            var document = Json.Parse(FrameworkProofContent.Read("resource-declarations.json")); edit?.Invoke(document);
            return ConstructionPlanTests.Points(additionalModule: module,
                moduleDeclaration: "{\"id\":\"proof.resource-declarations\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"resource-declarations.json\"]}",
                moduleDocument: document.ToString(),
                scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(module.Id)),
                worldEdit: root => StartupContentAuthoring.Position(root, "source", JsonValue.Number("20")));
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static EntityId Entity(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == id).Id;
        private static Fix Stock(SimGame game, string id)
        {
            var facts = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, id), facts);
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("resource declaration step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 480 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " source=" + Stock(game, "source")
                + " destination=" + Stock(game, "destination") + " cargo=" + Stock(game, "machine"));
        }
        private static CommandResult Order(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal preview accepts");
            c.Equal(before, game.Hash(), "preview is pure");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal command accepts"); return result;
        }
        // Immutable authoring fixture, not a conversion algorithm or session provider.
        private sealed class AuthoredUnit : IUnitDefinition
        {
            private readonly UnitDeclaration _declaration;
            internal AuthoredUnit()
            {
                var reader = new JsonReader(Json.Parse("{\"dimension\":\"count\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":true}"), "measure-unit");
                _declaration = UnitDeclaration.Read("unit.measure-proof", reader); reader.End();
            }
            public UnitId Unit => _declaration.Unit;
            public string Dimension => _declaration.Dimension;
            public long ScaleNumerator => _declaration.ScaleNumerator;
            public long ScaleDenominator => _declaration.ScaleDenominator;
            public bool Discrete => _declaration.Discrete;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current declaration build identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "authoritative resource/state payload identity");
            var unit = new AuthoredUnit();
            var measure = new ResourceCapacityMeasure(unit, Fix.Half);
            c.That(ReferenceEquals(unit, measure.Unit) && measure.PerQuantity == Fix.Half, "public authoring preserves fractional discrete coefficient");
            c.Throws<ArgumentNullException>(() => new ResourceCapacityMeasure(null, Fix.One), "null measure unit refuses");
            c.Throws<ArgumentOutOfRangeException>(() => new ResourceCapacityMeasure(unit, Fix.Zero), "zero coefficient refuses");
            c.Throws<ArgumentOutOfRangeException>(() => new ResourceCapacityMeasure(unit, -Fix.One), "negative coefficient refuses");
            var reader = new JsonReader(Json.Parse(FrameworkProofContent.Read("resource-declarations.json")).Get("planet_ores")[0].Clone(), "shared-declaration");
            reader.Skip("id"); reader.Skip("composition");
            var declaration = ResourceDeclaration.Read("declaration.ore", reader); reader.End();
            c.That(declaration.HasTrait("mineral") && declaration.HasTrait("dense") && !declaration.HasTrait("missing"), "shared ordinal trait lookup");
            c.Throws<InvalidOperationException>(() => { var value = declaration.QuantityUnit; }, "unresolved quantity metadata cannot masquerade as ready");
            c.Throws<InvalidOperationException>(() => { var value = declaration.CapacityMeasures; }, "unresolved capacity metadata cannot masquerade as ready");
            c.Throws<ArgumentNullException>(() => declaration.Resolve(null), "absent resolver refuses");
            c.Throws<ArgumentNullException>(() => ResourceDeclaration.Read("test", null), "absent JSON reader refuses");

            var initialize = new InitializeProbe(); var module = new ResourceDeclarationModule(initialize); var factory = Factory(module); var game = New(factory);
            c.Equal(1, initialize.Initializations, "all metadata resolves before one Initialize");
            var facts = new List<ResourceInfo>(); ulong hash = game.Hash(); game.Seat(0).View.CopyResources(facts);
            var ore = facts.Single(f => f.Id == "declaration.ore"); var supply = facts.Single(f => f.Id == "supply");
            var unused = facts.Single(f => f.Id == "declaration.unused");
            c.That(ore.Category == "mineral" && ore.Phase == new Code("solid") && ore.QuantityUnit.Unit == new UnitId("unit.capacity.mass"), "specialized resource shares strict authored common metadata");
            c.That(ore.Traits.SequenceEqual(new[] { "mineral", "dense" }) && ore.CapacityMeasures.Count == 2, "traits and multiple dimensions preserved");
            c.That(ore.CapacityMeasures.Select(m => m.Unit.Unit).SequenceEqual(ore.CapacityMeasures.Select(m => m.Unit.Unit).OrderBy(id => id)), "resolved measures have stable order");
            c.That(ore.CapacityMeasures.Single(m => m.Unit.Dimension == "volume").PerQuantity == Fix.Half, "no invented density or rounding");
            c.That(supply.Traits.Count == 0 && unused.Traits.Count == 0 && unused.CapacityMeasures[0].PerQuantity == Fix.Half, "native and unused specialized declarations retain explicit empty traits and fractional coefficient");
            c.Throws<NotSupportedException>(() => ((IList<string>)ore.Traits).Clear(), "traits read only");
            c.Throws<NotSupportedException>(() => ((IList<ResourceCapacityMeasure>)ore.CapacityMeasures).Clear(), "measure facts read only");
            c.That(game.Seat(0).View.TryFindResource(ore.Id, out var id) && id == ore.Resource, "one original stable/numeric catalog identity");
            c.Equal(hash, game.Hash(), "metadata observations pure");

            Order(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"), Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits);
            var offer = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Entity(game, "source"), Entity(game, "destination"), supply.Resource, Fix.FromInt(20));
            c.That(offer.Status == HaulStatus.Ready && offer.Amount == Fix.FromInt(8), "original native catalog and capacity admission unchanged");
            Order(c, game, new HaulResourcesCommand { Source = Entity(game, "source"), Destination = Entity(game, "destination"), Worker = Entity(game, "machine"), Resource = supply.Resource, Ceiling = Fix.FromInt(20), Priority = 20 });
            Until(game, null, () => Stock(game, "machine") > Fix.Zero && Stock(game, "machine") < Fix.FromInt(8), "partial physical loading");
            c.That(((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").Spatial).Position == Fix.FromInt(20)
                && Stock(game, "destination") == Fix.Zero, "no material movement before actual source arrival");
            c.Equal(Fix.FromInt(20), Stock(game, "source") + Stock(game, "machine") + Stock(game, "destination"), "original stock owners conserve quantities during loading");
            var twin = factory.Load(game.Save()); hash = game.Hash(); long at = game.Clock.StepIndex;
            game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == hash, "one clock pauses partial physical transfer");
            game.Clock.Resume(); twin.Clock.Resume();
            Until(game, twin, () => Stock(game, "destination") > Fix.Zero && Stock(game, "destination") < Fix.FromInt(8), "partial physical delivery");
            twin = factory.Load(game.Save());
            Until(game, twin, () => Stock(game, "destination") == Fix.FromInt(8) && Stock(game, "machine") == Fix.Zero, "physical delivery completion");
            c.That(Stock(game, "source") == Fix.FromInt(12) && game.Clock.StepIndex > 20, "native hauling still consumes time and exact material");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed save/load identity");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "public hauling and clock input replay");
            c.That(ore.Traits.SequenceEqual(new[] { "mineral", "dense" }) && ore.CapacityMeasures.Count == 2, "retained metadata unchanged across physical updates");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "unit", "category", "phase", "traits", "empty-trait", "duplicate-trait", "capacity", "empty-capacity", "zero", "negative", "blank-unit", "missing-unit", "wrong-kind", "ambiguous", "composition", "extra", "unused" })
            {
                var initialize = new InitializeProbe(); var module = new ResourceDeclarationModule(initialize);
                c.Throws<ContentException>(() => New(Factory(module, root =>
                {
                    var resource = root.Get("planet_ores")[mode == "unused" ? 1 : 0];
                    if (mode == "unit" || mode == "category" || mode == "phase" || mode == "traits" || mode == "capacity" || mode == "composition") resource.Remove(mode);
                    if (mode == "empty-trait") resource.Get("traits").Add(JsonValue.Of(" "));
                    if (mode == "duplicate-trait") resource.Get("traits").Add(JsonValue.Of("mineral"));
                    if (mode == "empty-capacity") resource.Set("capacity", JsonValue.NewObject());
                    if (mode == "zero" || mode == "negative" || mode == "unused") resource.Get("capacity").Set(mode == "unused" ? "unit.supply" : "unit.capacity.mass", JsonValue.Number(mode == "negative" ? "-1" : "0"));
                    if (mode == "blank-unit") resource.Get("capacity").Set(" ", JsonValue.Number("1"));
                    if (mode == "missing-unit") resource.Set("unit", JsonValue.Of("unit.missing"));
                    if (mode == "wrong-kind") resource.Set("unit", JsonValue.Of("source"));
                    if (mode == "ambiguous")
                    {
                        resource.Get("capacity").Set("unit.supply", JsonValue.Number("1"));
                        resource.Get("capacity").Set("unit.capacity.count", JsonValue.Number("1"));
                    }
                    if (mode == "extra") resource.Set("fabricated_density", JsonValue.Number("1"));
                })), "strict declaration refuses: " + mode);
                c.Equal(0, initialize.Initializations, "malformed declaration before Initialize: " + mode);
            }
        }
    }
}
