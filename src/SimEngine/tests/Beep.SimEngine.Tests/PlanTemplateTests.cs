using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static partial class PlanTemplateTests
    {
        public static void Register()
        {
            TestRunner.Add("Recurring templates: authored groups, private admission and cycle ownership", c => { Contracts(c); RecurringContracts(c); RemovalTemplateContracts(c); PlanDependencyContracts(c); DependencyEditingContracts(c); LibraryContracts(c); RecurringLibraryContracts(c); HistoryContracts(c); AutomaticHistoryContracts(c); HistoryClosureChain(c); ActiveDependencyContracts(c); });
            TestRunner.Add("Recurring templates: physical projects, cancellation and saved continuation", c => { Physical(c); RecurringPhysical(c); RemovalTemplatePhysical(c); RemovalTemplateRecurrence(c); PlanDependencyPhysical(c); DependencyEditingPhysical(c); LibraryPhysical(c); RecurringLibraryPhysical(c); HistoryFailedExecution(c); HistoryControllerPhysical(c); AutomaticHistoryPhysical(c); HistoryClosureGroups(c); ActiveDependencyPhysical(c); });
            TestRunner.Add("Plan history references: guarded provider reads and original first failures", ReferenceObservationContracts);
            TestRunner.Add("Plan history references: automatic cleanup keeps real retained records on faults", ReferenceObservationAutomatic);
            TestRunner.Add("Plan history: physical completion and saved retirement preserve structures and stock", HistoryPhysical);
        }
        private static GameFactory Points(string json = null) => ConstructionPlanTests.Points(templateDefinitions: json ?? FrameworkProofContent.Read("plan-templates.json"));
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static JsonValue At(SimGame game, int x) => game.World.Space is TileMap
            ? TilePlacement.Arguments(game.World.Space.Id, new TileCoord(x, 16)) : Content.Json.Parse("{\"space\":\"field.points\",\"position\":" + x + "}");
        private static List<PlanView> Group(SimGame game, string group)
        { var list = new List<PlanView>(); View(game).CopyPlanGroup(group, list); return list; }
        private static PlanTemplate Read(JsonValue json)
        { var reader = new JsonReader(json, "template"); PlanTemplate value = PlanTemplate.Read(reader); reader.End(); return value; }
        private static CommandResult Place(SimGame game, PlanTemplate value, int x, string group)
            => TestKit.Order(game, 0, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(value), Anchor = At(game, x), Group = group });
        private static void Until(SimGame game, Func<bool> done, string group, SimGame twin = null)
        {
            for (int i = 0; i < 1200 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException("template stage tick=" + game.Tick + " plans=" + string.Join(";", Group(game, group).ConvertAll(p => p.Id + "/" + p.State + "/" + p.Blocker)));
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = Points(); SimGame game = ConstructionPlanTests.NewPoints(factory); WorkView view = View(game);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "explicit template command identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no old command/save reader");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, game.World.Services.Get<WorkBoard>().StateSchemaVersion, "prerequisite links extend the sole native plan codec");
            PlanTemplate authored = view.AuthoredPlanTemplate("point.pair");
            c.That(authored != null && authored.Entries.Count == 2, "master-listed explicit JSON template resolved");
            c.That(game.World.Content.Get<PlanTemplateDef>("point.pair").AuthoringPath.Contains("plan-templates.json"), "master explicitly lists separate template document");
            c.That(view.AuthoredPlanTemplate("missing") == null, "missing template never fabricates a default");
            ulong hash = game.Hash(); c.That(view.PreviewPlanTemplate(authored, At(game, 12), "first").Ready, "entire expanded layout admitted by native group board");
            c.Equal(hash, game.Hash(), "preview does not allocate entities/plans/orders/claims");
            JsonValue exported = authored.ToJson(); c.Throws<InvalidOperationException>(() => exported.Get("entries")[0].Get("arguments").Set("target", JsonValue.Number("1")), "export freezes nested content");
            PlanTemplate round = Read(Content.Json.Parse(exported.ToString())); c.Equal(exported.ToString(), round.ToJson().ToString(), "one JSON schema round trips stable ordered intent only");
            var rows = new List<PlanTemplateEntry>(round.Entries); var capturedValue = new PlanTemplate(round.Space, rows); rows.Clear(); c.Equal(2, capturedValue.Entries.Count, "template captures immutable entry membership");
            c.Throws<ArgumentException>(() => new PlanTemplate(round.Space, Array.Empty<PlanTemplateEntry>()), "empty template refuses");
            c.Throws<ArgumentException>(() => new PlanTemplate(round.Space, new[] { authored.Entries[0], authored.Entries[0] }), "duplicate stable entry identity refuses");
            foreach (string field in new[] { "definition", "priority", "paused", "arguments", "after" })
            { JsonValue bad = exported.Clone(); bad.Get("entries")[1].Remove(field); c.Throws<ContentException>(() => Read(bad), "required field " + field + " never defaults"); }
            JsonValue extra = exported.Clone(); extra.Get("entries")[0].Set("stock", JsonValue.Number("9")); c.Throws<ContentException>(() => Read(extra), "execution/stock payload refuses");
            JsonValue noSpace = exported.Clone(); noSpace.Remove("space"); c.Throws<ContentException>(() => Read(noSpace), "template space selection never defaults");
            var late = new PlanTemplate(round.Space, new[] { authored.Entries[0], new PlanTemplateEntry("bad", "build:missing", authored.Entries[1].Arguments, 1, true) });
            PlanGroupCheck failure = view.PreviewPlanTemplate(late, At(game, 12), "bad");
            c.That(failure.Blocker == Codes.UnknownDef && failure.Entries.Count == 2 && failure.Entries[0].Blocker.IsNone, "late missing definition returns all entry diagnostics");
            c.Equal(failure.Blocker, Place(game, late, 12, "bad").Refusal, "whole failure preview/command parity"); c.Equal(0, Group(game, "bad").Count, "failed expansion creates no prefix");
            var duplicate = new PlanTemplate(round.Space, new[] { authored.Entries[0], new PlanTemplateEntry("other", authored.Entries[0].Definition, authored.Entries[0].Arguments, 1, true) });
            c.Equal(PlanCodes.Duplicate, Place(game, duplicate, 12, "duplicate").Refusal, "expanded duplicate geometry uses native identity admission");
            c.Equal(Codes.InvalidArgument, view.PreviewPlanTemplate(authored, At(game, 12), "").Blocker, "explicit fresh group required");
            c.Equal(Codes.InvalidArgument, view.PreviewPlanTemplate(new PlanTemplate("other", authored.Entries), At(game, 12), "other-space").Blocker, "template cannot substitute its explicitly selected space");
            c.Equal(Codes.InvalidArgument, view.PreviewPlanTemplate(authored, Content.Json.Parse("{\"space\":\"wrong\",\"position\":12}"), "wrong").Blocker, "no cross-space conversion");
            c.Equal(Codes.OutOfBounds, Place(game, authored, 99, "outside").Refusal, "one out-of-bounds member refuses whole placement");
            CommandResult placed = Place(game, authored, 12, "first"); c.That(placed.Accepted && placed.Arg0 == 1 && placed.Arg1 == 2, "refused templates allocated no plan identities");
            c.That(Group(game, "first").TrueForAll(p => p.State == PlanState.Pending && p.Order == 0), "paused template creates intention only, no ghost/project");
            hash = game.Hash(); PlanGroupCheck capture = view.CapturePlanTemplate("first", At(game, 10), out PlanTemplate portable);
            c.That(capture.Ready && capture.Entries.Count == 2 && portable.Entries[0].Arguments.Get("placement").Get("offset").Text == "2", "engine captures relative provider geometry");
            c.Equal(hash, game.Hash(), "capture is pure");
            c.That(!portable.ToJson().ToString().Contains("target") && !portable.ToJson().ToString().Contains("order"), "captured value contains no live entity or execution links");
            PlanGroupCheck foreign = View(game, 1).CapturePlanTemplate("first", At(game, 10), out PlanTemplate hidden);
            c.That(foreign.Blocker == Codes.NotFound && hidden == null && foreign.Entries.Count == 0, "foreign capture reveals no group members or template");
            c.That(Place(game, portable, 30, "translated").Accepted, "captured template places at another explicit anchor");
            c.Equal("32", Group(game, "translated")[0].Arguments.Get("placement").Get("position").Text, "engine—not host—translates relative offset");
            c.Equal(PlanCodes.GroupExists, Place(game, portable, 40, "first").Refusal, "existing history label cannot be overwritten");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "expanded intentions save only through native plan codec");
            c.That(TestKit.Order(game, 0, new SubmitPlanCommand { Intent = new PlanIntent("activity:inspect", Content.Json.Parse("{\"target\":" + EmergencyPreemptionTests.Find(game, "source").Id.Value + "}"), "entity-intent", 1, true) }).Accepted, "normal entity activity still submits");
            c.Equal(PlanCodes.TemplateUnsupported, view.CapturePlanTemplate("entity-intent", At(game, 10), out _).Blocker, "live target ID workflow cannot silently become portable");
            foreach (string mode in new[] { "missing", "entity-id", "wrong-space", "invalid-geometry" })
            {
                JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("plan-templates.json")); JsonValue row = root.Get("plan_templates")[0].Get("entries")[1];
                if (mode == "missing") row.Set("definition", JsonValue.Of("build:missing"));
                if (mode == "entity-id") row.Set("definition", JsonValue.Of("activity:inspect"));
                if (mode == "wrong-space") row.Get("arguments").Get("placement").Set("space", JsonValue.Of("other"));
                if (mode == "invalid-geometry") row.Get("arguments").Get("placement").Set("target", JsonValue.Number("1"));
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Points(root.ToString())), "master " + mode + " refuses before Initialize");
            }
            foreach (long raw in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue, Fix.Parse("12.123456").Raw })
                c.Equal(raw, Fix.Parse(Fix.FromRaw(raw).ToExactString()).Raw, "exact authored scalar retains every native bit");
            JsonValue multiple = Content.Json.Parse(FrameworkProofContent.Read("plan-templates.json"));
            JsonValue other = multiple.Get("plan_templates")[0].Clone(); other.Set("id", JsonValue.Of("another-space")); other.Set("space", JsonValue.Of("another-world"));
            other.Get("entries")[0].Set("definition", JsonValue.Of("build:other-world-prototype")); multiple.Get("plan_templates").Add(other);
            SimGame selected = ConstructionPlanTests.NewPoints(Points(multiple.ToString()));
            c.That(View(selected).AuthoredPlanTemplate("another-space") != null && View(selected).PreviewPlanTemplate(View(selected).AuthoredPlanTemplate("another-space"), At(selected, 12), "other").Blocker == Codes.InvalidArgument,
                "other authored spatial templates coexist without forcing irrelevant provider composition");
            var space = (ISpatialPlacementTemplates)game.World.Space;
            JsonValue fractional = Content.Json.Parse("{\"space\":\"field.points\",\"position\":12.123456}");
            JsonValue relative = space.CaptureTemplatePlacement(fractional, At(game, 10));
            c.Equal(((PointPlacement)((ISpatialPlacementIntent)space).ReadPlacement(fractional)).Position,
                ((PointPlacement)((ISpatialPlacementIntent)space).ReadPlacement(space.PlaceTemplatePlacement(relative, At(game, 10)))).Position, "point provider never rounds captured fractional geometry to log precision");
            Tiles(c);
        }
        private static void Tiles(Check c)
        {
            GameFactory factory = Colony.Factory(); SimGame game = ResourceDispositionTests.New(factory); string space = game.World.Space.Id;
            var arguments = JsonValue.NewObject(); arguments.Set("placement", Content.Json.Parse("{\"space\":\"" + space + "\",\"dx\":0,\"dy\":0}"));
            var template = new PlanTemplate(space, new[] { new PlanTemplateEntry("tower", "build:tower", arguments, 1, true) });
            c.That(Place(game, template, 10, "tiles").Accepted, "same board/command supports native tile geometry");
            c.That(View(game).CapturePlanTemplate("tiles", At(game, 8), out PlanTemplate captured).Ready, "tile group captures via selected provider");
            c.Equal("2", captured.Entries[0].Arguments.Get("placement").Get("dx").Text, "tile offset stays authored tile geometry");
            c.That(Place(game, captured, 20, "tile-copy").Accepted, "captured tile layout places without host coordinates logic");
            c.Equal("22", Group(game, "tile-copy")[0].Arguments.Get("placement").Get("x").Text, "relative tile intent translated exactly");
            var geometry = (ISpatialPlacementTemplates)game.World.Space;
            JsonValue from = TilePlacement.Arguments(space, new TileCoord(int.MinValue, 0)), to = TilePlacement.Arguments(space, new TileCoord(int.MaxValue, 0));
            JsonValue wide = geometry.CaptureTemplatePlacement(to, from); c.Equal("4294967295", wide.Get("dx").Text, "relative geometry supports full native difference, no artificial size cap");
            c.Equal(int.MaxValue.ToString(), geometry.PlaceTemplatePlacement(wide, from).Get("x").Text, "wide relative offset maps exactly to native coordinate");
            arguments.Set("placement", wide); var overflow = new PlanTemplate(space, new[] { new PlanTemplateEntry("overflow", "build:tower", arguments, 1, true) });
            c.Equal(Codes.InvalidArgument, View(game).PreviewPlanTemplate(overflow, to, "overflow").Blocker, "actual native coordinate overflow refuses whole template");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Points(); SimGame game = ConstructionPlanTests.NewPoints(factory); PlanTemplate template = View(game).AuthoredPlanTemplate("point.pair");
            JsonValue anchor = At(game, 12); game.Seat(0).Submit(new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(template), Anchor = anchor, Group = "field" });
            anchor.Set("position", JsonValue.Number("80")); SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "saved template inbox carries exact frozen value and anchor");
            c.Equal("12", Group(game, "field")[0].Arguments.Get("placement").Get("position").Text, "submitted anchor captures player input rather than caller mutation");
            EmergencyPreemptionTests.Configure(c, game); EmergencyPreemptionTests.Configure(c, twin);
            c.That(TestKit.Order(game, 0, new ConfigurePlanGroupCommand { Group = "field", Priority = 70, Paused = false }).Accepted, "player starts real template group");
            c.That(TestKit.Order(twin, 0, new ConfigurePlanGroupCommand { Group = "field", Priority = 70, Paused = false }).Accepted, "saved twin starts identical real group");
            c.That(Group(game, "field").TrueForAll(p => p.State == PlanState.Active && p.Order > 0), "template expands into native separate physical site/orders");
            Until(game, () => Group(game, "field").Exists(p => game.World.Services.Get<WorkBoard>().Order(p.Order)?.Node("build").Progress.WholeTicks > 0), "field", twin);
            var before = Group(game, "field");
            c.That(View(game).CapturePlanTemplate("field", At(game, 12), out PlanTemplate active).Ready, "active capture reads original intent, not site state/progress/resources");
            c.Equal(template.ToJson().Get("entries")[0].Get("arguments").ToString(), active.Entries[0].Arguments.ToString(), "active relative intent remains same geometry");
            c.That(TestKit.Order(game, 0, new ConfigurePlanGroupCommand { Group = "field", Priority = 70, Paused = true }).Accepted, "physical group pause retains actual work");
            c.That(TestKit.Order(twin, 0, new ConfigurePlanGroupCommand { Group = "field", Priority = 70, Paused = true }).Accepted, "twin pause retains same actual work");
            SimGame paused = factory.Load(game.Save()); for (int i = 0; i < 6; i++) { Tick(game); Tick(paused); }
            c.Equal(game.Hash(), paused.Hash(), "paused real template execution saves identically"); twin = paused;
            TestKit.Order(game, 0, new ConfigurePlanGroupCommand { Group = "field", Priority = 70, Paused = false }); TestKit.Order(twin, 0, new ConfigurePlanGroupCommand { Group = "field", Priority = 70, Paused = false });
            Until(game, () => Group(game, "field").TrueForAll(p => p.State == PlanState.Done), "field", twin);
            c.Equal(game.Hash(), twin.Hash(), "two physically supplied constructions complete from saved state identically");
            int completed = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "point.tower") completed++;
            c.Equal(2, completed, "only real completed structures exist after timed work");
            c.Equal(Fix.FromInt(4), EmergencyPreemptionTests.Find(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "two actual eight-input constructions consume physical supply, not copies");
            c.That(View(game).CapturePlanTemplate("field", At(game, 12), out PlanTemplate finished).Ready, "terminal capture uses retained authored intent even after site retired");
            c.That(Place(game, finished, 30, "later").Accepted, "completed layout places fresh native plans without completed state");
            c.That(Group(game, "later").TrueForAll(p => p.State != PlanState.Done), "no copied completion/progress/stock");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "placed fresh workflow remains durable");
            c.That(TestKit.Order(game, 0, new CancelPlanGroupCommand { Group = "later" }).Accepted, "template execution cancellation uses native group/site owners");
            c.That(Group(game, "later").TrueForAll(p => p.State == PlanState.Cancelled), "no separate template scheduler resurrects cancelled work");
        }
    }
}
