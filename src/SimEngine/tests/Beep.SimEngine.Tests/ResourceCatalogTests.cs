using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ResourceCatalogTests
    {
        public static void Register()
        {
            TestRunner.Add("Resources: extensible catalog, authored units and explicit refusals", Catalog);
            TestRunner.Add("Resources: consumed extension quantities and physical saved sessions", Execution);
        }
        internal static GameFactory Factory(ResourceProofModule module, string units = null, string ore = null, string workflow = null, string manifest = null)
        {
            var definition = new GameDefinition("resource-proof", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new WorkModule(), new WorldSetupModule(), module });
            var files = new PreloadedFileSource().Add("manifest.json", manifest ?? FrameworkProofContent.Read("resources-manifest.json"))
                .Add("framework/access.json", FrameworkProofContent.Access)
                .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("units.json", units ?? FrameworkProofContent.Read("resource-units.json"))
                .Add("ore.json", ore ?? FrameworkProofContent.Read("resource-ore.json"))
                .Add("workflow.json", workflow ?? FrameworkProofContent.Read("resource-workflow.json"))
                .Add("unlisted.json", "{\"unknown_unlisted_gameplay\": []}");
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "resource-work", Seed = 31 });
        private static string Refused(Check c, string label, string units = null, string ore = null, string workflow = null, string manifest = null)
        {
            var module = new ResourceProofModule(); string message = "";
            try { New(Factory(module, units, ore, workflow, manifest)); c.That(false, label); }
            catch (ContentException error) { message = error.Message; c.That(true, label); }
            c.Equal(0, module.Initializations, label + " runs no Initialize callback");
            return message;
        }
        private static void Catalog(Check c)
        {
            var module = new ResourceProofModule(); GameFactory factory = Factory(module); SimGame game = New(factory);
            ResolvedContent content = game.World.Content; IResourceCatalog catalog = content;
            c.Equal(1, module.Initializations, "master-listed resource provider initializes normally");
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IWorkSupplyRequests>(), "resource definitions add no money or supply fallback");
            c.Equal(3, content.ResourceCount, "base and extension resources share one compact index");
            c.That(catalog.TryResolve("planetx.ore", out ResourceId ore), "external stable resource ID resolves through the catalog contract");
            ResourceDef baseOre = content.Get<ResourceDef>("planetx.ore"); PlanetOreDef specialized = content.Get<PlanetOreDef>("planetx.ore");
            c.That(ReferenceEquals(baseOre, specialized) && ReferenceEquals(baseOre, catalog.Get(ore)), "base aliases and contract lookup reference the same immutable definition");
            c.Equal("proof.resources", specialized.Module, "resource retains ordinary module ownership");
            c.Equal("planetx.synthetic-mineral", specialized.Composition, "provider reads its own authored fields");
            c.Equal(new Code("solid"), specialized.Phase, "phase is authored, not a binary default");
            c.That(specialized.HasTrait("space.mineral"), "extension handling traits remain accessible through the common contract");
            c.Equal(2, specialized.CapacityMeasures.Count, "explicit mass and volume measures coexist as separate dimensions");
            c.That(ReferenceEquals(specialized.QuantityUnit, content.Get<UnitDef>("unit.kg")), "quantity unit references the existing definition registry");
            c.Throws<NotSupportedException>(() => ((IList<IResourceContentDefinition>)content.Resources).Clear(), "resource projection is read only");
            c.Throws<NotSupportedException>(() => ((System.Collections.IList)catalog.Definitions).Clear(), "catalog contract projection is read only");
            c.Throws<NotSupportedException>(() => ((IList<string>)specialized.Traits).Clear(), "resource traits cannot mutate shared content");
            c.Throws<NotSupportedException>(() => ((IList<ResourceCapacityMeasure>)specialized.CapacityMeasures).Clear(), "capacity metadata cannot mutate shared content");
            c.Throws<ContentException>(() => catalog.Get(ResourceId.None), "invalid resource index fails explicitly");
            c.That(!catalog.TryResolve("missing.ore", out _) && !catalog.TryResolve(null, out _), "missing stable IDs are not fabricated");
            c.That(typeof(ResourceDef).GetMethod("Read").IsFinal && typeof(ResourceDef).GetMethod("Resolve").IsFinal, "provider hooks cannot bypass shared resource validation");
            c.That(typeof(ResourceDef).Assembly.GetType("Beep.SimEngine.Content.ItemDef") == null, "old sealed item definition has no alias");
            c.That(typeof(ResourceId).Assembly.GetType("Beep.SimEngine.ItemId") == null, "old item identity has no alias");

            UnitDef kg = content.Get<UnitDef>("unit.kg"), gram = content.Get<UnitDef>("unit.g"), litre = content.Get<UnitDef>("unit.litre");
            UnitDef count = content.Get<UnitDef>("unit.count"), pair = content.Get<UnitDef>("unit.pair");
            c.Throws<InvalidOperationException>(() => kg.Read(new JsonReader(Json.Parse("{}"), "late unit")), "shared unit cannot be reauthored after load");
            c.Throws<InvalidOperationException>(() => specialized.Read(new JsonReader(Json.Parse("{}"), "late resource")), "shared resource cannot be reauthored after load");
            c.Throws<InvalidOperationException>(() => specialized.Resolve(new ContentResolver(content, specialized)), "shared resource cannot resolve a second time");
            UnitConversionResult result = UnitConversion.Convert(Fix.One, kg, gram);
            c.Equal(UnitConversionStatus.Converted, result.Status, "dimension-compatible unit conversion succeeds exactly");
            c.Equal(Fix.FromInt(1000), result.Amount, "one kilogram converts to exactly one thousand grams");
            result = UnitConversion.Convert(Fix.FromInt(500), gram, kg);
            c.Equal(UnitConversionStatus.Converted, result.Status, "representable fractional bulk amount converts");
            c.Equal(Fix.Half, result.Amount, "five hundred grams is exactly half a kilogram");
            c.Equal(UnitConversionStatus.IncompatibleDimension, UnitConversion.Convert(Fix.One, kg, litre).Status, "no implicit mass-volume density conversion");
            c.Equal(UnitConversionStatus.FractionalDiscrete, UnitConversion.Convert(Fix.Half, count, count).Status, "fractional discrete source quantity refuses");
            c.Equal(UnitConversionStatus.FractionalDiscrete, UnitConversion.Convert(Fix.One, count, pair).Status, "fractional discrete destination quantity refuses");
            c.Equal(UnitConversionStatus.Unrepresentable, UnitConversion.Convert(Fix.One, kg, content.Get<UnitDef>("unit.third")).Status, "conversion never silently rounds an unrepresentable rational");
            c.Equal(UnitConversionStatus.OutOfRange, UnitConversion.Convert(Fix.One, content.Get<UnitDef>("unit.huge"), kg).Status, "conversion refuses overflow before narrowing");
            c.Equal(UnitConversionStatus.InvalidAmount, UnitConversion.Convert(-Fix.One, kg, gram).Status, "negative quantity refuses");
            c.Equal(UnitConversionStatus.InvalidUnit, UnitConversion.Convert(Fix.One, null, kg).Status, "absent unit is not substituted");
            var bill = new Bill(new[] { new ResourceAmount(ore, Fix.FromInt(2)), new ResourceAmount(content.ResourceId("coolant"), Fix.FromInt(3)), new ResourceAmount(content.ResourceId("kit"), Fix.FromInt(4)) });
            var quantities = new List<UnitQuantity>(); bill.CopyQuantities(catalog, quantities);
            c.Equal(3, quantities.Count, "bill reports a quantity vector instead of summing incompatible units");
            c.Equal(Fix.FromInt(2), quantities.Find(q => q.Unit == kg.Unit).Amount, "mass stays in its authored quantity unit");
            c.Equal(Fix.FromInt(3), quantities.Find(q => q.Unit == litre.Unit).Amount, "volume stays separate");
            c.Equal(Fix.FromInt(4), quantities.Find(q => q.Unit == count.Unit).Amount, "discrete count stays separate");
            var views = new List<ResourceInfo>(); game.Observer.CopyResources(views);
            ResourceInfo view = views.Find(r => r.Id == "planetx.ore");
            c.Equal(kg.Unit, view.QuantityUnit.Unit, "host reads immutable authored unit facts");
            c.Equal(2, view.CapacityMeasures.Count, "host receives detached capacity metadata");
            c.Throws<NotSupportedException>(() => ((IList<ResourceCapacityMeasure>)view.CapacityMeasures).Clear(), "view metadata cannot mutate content");
            var unitsView = new List<UnitInfo>(); game.Observer.CopyUnits(unitsView);
            c.Equal(10, unitsView.Count, "unit facts come from the same selected registry");
            ulong hash = game.Hash(); game.Observer.CopyResources(views); bill.CopyQuantities(catalog, quantities); UnitConversion.Convert(Fix.One, kg, gram);
            c.Equal(hash, game.Hash(), "catalog projections and conversion change no session state");

            string oreJson = FrameworkProofContent.Read("resource-ore.json"), unitsJson = FrameworkProofContent.Read("resource-units.json");
            string message = Refused(c, "missing resource unit refuses", ore: oreJson.Replace("\"unit\": \"unit.kg\"", "\"unit\": \"unit.missing\""));
            c.That(message.Contains("proof.resources@1.0.0 ore.json:planet_ores[0](planetx.ore)") && message.Contains("unit.missing"), "cross-reference diagnostic names owner/version/file/definition/reference");
            Refused(c, "missing phase refuses", ore: oreJson.Replace("\"phase\": \"solid\", ", ""));
            Refused(c, "missing explicit traits refuses", ore: oreJson.Replace("\"traits\": [\"space.mineral\", \"handling.bulk\"],", ""));
            Refused(c, "empty capacity measures refuse", ore: oreJson.Replace("{ \"unit.kg\": 1, \"unit.litre\": 0.5 }", "{}"));
            Refused(c, "negative capacity measure refuses", ore: oreJson.Replace("\"unit.kg\": 1", "\"unit.kg\": -1"));
            Refused(c, "ambiguous capacity dimension refuses", ore: oreJson.Replace("\"unit.litre\": 0.5", "\"unit.g\": 500"));
            Refused(c, "duplicate trait refuses", ore: oreJson.Replace("\"handling.bulk\"", "\"space.mineral\""));
            Refused(c, "provider-specific missing chemistry refuses", ore: oreJson.Replace("\"composition\": \"planetx.synthetic-mineral\"", "\"composition\": \"\""));
            Refused(c, "zero unit denominator refuses", units: unitsJson.Replace("\"denominator\": 1000", "\"denominator\": 0"));
            Refused(c, "unit discrete policy must be authored", units: unitsJson.Replace(", \"discrete\": false", ""));
            Refused(c, "old items section has no loader", ore: "{\"items\": []}");
            Refused(c, "old fluid field is not an alias", ore: oreJson.Replace("\"phase\": \"solid\"", "\"phase\": \"solid\", \"fluid\": false"));
            string workflow = FrameworkProofContent.Read("resource-workflow.json");
            message = Refused(c, "base and derived resource ID collision refuses", workflow: workflow.Replace("\"id\": \"kit\"", "\"id\": \"planetx.ore\""));
            c.That(message.Contains("ResourceDef") && message.Contains("declared twice"), "collision is detected across the resource base contract");
        }

        private static void Execution(Check c)
        {
            var module = new ResourceProofModule(); GameFactory factory = Factory(module); SimGame game = New(factory);
            var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(new PlayerId(0), entities); EntityId target = entities[0].Id;
            ResourceId ore = game.World.Content.ResourceId("planetx.ore"); IInventory stock = game.World.Entities.Get(target).Get<IInventory>();
            c.Equal(Fix.FromInt(3), stock.Amount(ore), "extension resource startup quantity is authored");
            var command = new PostOrderCommand { Plan = "refine", Target = target }; ulong before = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "new resource requirements use the existing command evaluator");
            c.Equal(before, game.Hash(), "preview does not consume ore");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary work consumes a derived resource with no scheduler edits");
            c.Equal(Fix.FromInt(3) - Fix.Half, stock.Amount(ore), "bulk half-unit consumption preserves exact quantity");
            game.RunTicks(2); SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "new resource identity and active quantity state save together");
            game.RunTicks(4); twin.RunTicks(4);
            c.Equal(0, game.Seat(0).View.Feature<WorkView>().ActiveOrders(target), "authored extension work takes actual simulation time");
            c.Equal(Fix.FromInt(3) - Fix.Half, stock.Amount(ore), "completion consumes no duplicate ore");
            c.Equal(game.Hash(), twin.Hash(), "saved extension sessions continue identically");
            c.That(factory.Describe(game.Save()).Compatible, "saved content records resource provider and authored documents");
            var changed = Factory(new ResourceProofModule(), ore: FrameworkProofContent.Read("resource-ore.json").Replace("planetx.synthetic-mineral", "planetx.changed-mineral"));
            c.That(!changed.Describe(game.Save()).Compatible, "changed selected chemistry invalidates save content identity");
            c.Throws<CorruptStateException>(() => changed.Load(game.Save()), "no legacy or substitute resource save load");
            string workflow = FrameworkProofContent.Read("resource-workflow.json");
            string message = Refused(c, "fractional authored count bill refuses", workflow: workflow.Replace("\"planetx.ore\": 0.5", "\"kit\": 0.5"));
            c.That(message.Contains("quantities['kit']") && message.Contains("FractionalDiscrete"), "authored requirement refusal includes its exact native quantity property path");
            Refused(c, "missing authored unit file is not supplied implicitly", manifest: FrameworkProofContent.Read("resources-manifest.json").Replace("\"units.json\", ", ""));
            OptionalWorkTests.Physical(c); // Same bounded thin-player proof covers directly affected physical consumers.
        }
    }
}
