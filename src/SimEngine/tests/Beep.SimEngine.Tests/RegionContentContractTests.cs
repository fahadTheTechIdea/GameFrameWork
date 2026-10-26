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
    internal static class RegionContentContractTests
    {
        internal const string Case = "Region content contracts: physical discovery and private area versions";
        private static readonly Code Source = new Code("binding.contact");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(RegionContentModule module, Action<JsonValue> edit = null)
        {
            var doc = Json.Parse(FrameworkProofContent.Read("region-content-contracts.json")); edit?.Invoke(doc);
            return ExplorationBindingTests.Factory(new ExplorationBindingsModule(),
                sensorEdit: sensor => {
                    sensor.Get("entities")[0].Get("components").Set("work_policy", Json.Parse("{\"profile\":\"region.machine\"}"));
                    sensor.Get("knowledge_visibility_policies")[0].Set("rules", Json.Parse("[\"region.discovery\",\"sensor.contact.sight\"]"));
                }, extraModules: new GameModule[] { module }, extraDeclarations: new[] {
                    Json.Parse("{\"id\":\"proof.region-content\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"proof.exploration-bindings\",\"version\":\"1.0.0\"}],\"documents\":[\"region-content-contracts.json\"]}") },
                extraFiles: new Dictionary<string,string> { ["region-content-contracts.json"] = doc.ToString() });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 134273 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static EntityId Entity(SimGame game, string id) => TransformationContentContractTests.Entity(game, id);
        private static Fix Stock(SimGame game, string id) => TransformationContentContractTests.Stock(game, id);
        private static WorkView Work(SimGame game, int company = 0) => game.Seat(company).View.Feature<WorkView>();
        private static Fix Position(SimGame game) => ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Def == "sensor.machine").Spatial).Position;
        private static ExplorationEntrySnapshot Contact(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<ExplorationView>().Read(Entity(game, "sensor.machine"), Source, out var value);
            if (!code.IsNone) throw new InvalidOperationException("contact read " + code); return value.Entries.First();
        }
        private static bool Known(SimGame game, int company = 0) => game.Seat(company).View.Feature<KnowledgeView>()
            .TryRead(new PlayerId(company), "sim.knowledge.facts", "binding.reset-known", out _);
        private static List<WorkJobView> Jobs(SimGame game)
        { var rows = new List<WorkJobView>(); Work(game).CopyJobs(rows); return rows; }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("region step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 480 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " position=" + Position(game) +
                " contact=" + Contact(game).Progress + "/" + Contact(game).Blocker +
                " jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
        }
        private static CommandResult Order(Check c, SimGame game, Command command, SimGame twin = null, int company = 0)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(company).Preview(command), "ordinary public command preview");
            c.Equal(before, game.Hash(), "preview does not change area/progress/stock"); var result = TestKit.Order(game, company, command);
            c.That(result.Accepted, "ordinary engine command");
            if (twin != null) { c.That(TestKit.Order(twin, company, command).Accepted, "same saved twin command"); c.Equal(game.Hash(), twin.Hash(), "command twin hash"); }
            return result;
        }
        private static SetOperatingAreaCommand Edit(SimGame game, string name, int maximum, int company = 0)
            => new SetOperatingAreaCommand { Name = name, Template = "region.travel", ShapeJson = "{\"maximum\":" + maximum + ",\"minimum\":0}",
                ExpectedRevision = Work(game, company).ReadOperatingArea(name)?.Revision ?? -1 };
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "region contract build"); c.Equal(154, EngineInfo.SaveFormat, "original owner codecs retained");
            c.Equal(typeof(Def), typeof(PointIntervalRegionDef).BaseType, "direct provider has no required native region base");
            Physical(c); Invalid(c);
        }
        private static void Physical(Check c)
        {
            var module = new RegionContentModule(); var factory = Factory(module); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            c.That(module.ActualAliases && module.OriginalAreaOwner && module.Initialize.Initializations > 0, "actual region aliases bind original area/contact/requirement owners");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "no mandatory business module");
            var initial = Work(game).ReadOperatingArea("region.travel");
            c.Equal(0L, initial.Revision, "authored private starting area version");
            c.Equal(initial.ShapeJson, Work(game, 1).ReadOperatingArea("region.travel").ShapeJson, "same intent, independent company state");
            Order(c, game, Edit(game, "region.travel", 8, 1), company: 1);
            c.Equal(initial.ShapeJson, Work(game).ReadOperatingArea("region.travel").ShapeJson, "foreign company edit cannot alter own area");
            c.Equal(0L, Work(game).ReadOperatingArea("region.travel").Revision, "foreign revision sequence isolated");
            var create = Edit(game, "region.player", 20); Order(c, game, create);
            var area = Work(game).ReadOperatingArea("region.player");
            c.That(area.Revision > 0 && Work(game, 1).ReadOperatingArea("region.player") == null, "new private named version has no foreign copy");
            c.Equal(initial.ShapeJson, area.ShapeJson, "shared reader canonicalizes explicit member order");
            c.Equal(OperatingAreaCodes.Stale, game.Seat(0).Preview(create), "stale create cannot overwrite");
            ulong hash = game.Hash();
            foreach (string shape in new[] { "null", "{", "{\"minimum\":-1,\"maximum\":20}", "{\"minimum\":20,\"maximum\":1}", "{\"minimum\":0,\"maximum\":101}", "{\"minimum\":0,\"maximum\":20,\"space\":\"other\"}", "{\"minimum\":0,\"maximum\":20,\"fallback\":true}" })
                c.Equal(OperatingAreaCodes.InvalidShape, Work(game).PreviewOperatingArea("region.player", "region.travel", shape, area.Revision), "ordinary invalid shape refusal " + shape);
            c.Equal(hash, game.Hash(), "invalid player shape is not a provider fault or side effect");
            var requirements = new List<WorkRequirementView>();
            c.Equal(Code.None, Work(game).PreviewOrderRequirements("region.inspect", Entity(game, "destination"), requirements), "original requirement preview");
            c.Equal(WorkRequirementStatus.Satisfied, requirements.Single().Result.Status, "fixed direct region contains actual destination");
            c.Equal(Code.None, Work(game).PreviewOrderRequirements("region.inspect", Entity(game, "source"), requirements), "ordinary outside diagnostic");
            c.Equal(SpatialWorkCodes.OutsideRegion, requirements.Single().Result.Blocker, "fixed authored requirement remains separate from operating area");
            var values = new List<Fix>(); var layers = game.Seat(0).View.Feature<PointLayerView>();
            c.Equal(SpatialLayerReadStatus.Unavailable, layers.ReadLayer("point.terrain", new PointRegion(Fix.FromInt(10), Fix.One, 1), values), "private regional discovery initially hidden");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Order(c, game, new SetWorkerHeldCommand { Worker = Entity(game, "sensor.machine"), Held = true });
                game.Seat(0).View.TryFindResource("supply", out var resource);
                int route = checked((int)Order(c, game, new AddRouteCommand { From = Entity(game, "source"), To = Entity(game, "sensor.machine"), Resource = resource,
                    Settings = new LogisticsRouteSettings(true, Fix.One, 70) }).Arg0);
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "docked supply/private areas save");
                Until(game, twin, () => Stock(game, "sensor.machine") == Fix.One, "physical supply");
                Order(c, game, new RemoveRouteCommand { Route = route });
                Until(game, null, () => Jobs(game).All(j => j.State == JobState.Done || j.State == JobState.Cancelled), "carrier physical departure");
                Order(c, game, new SetWorkerHeldCommand { Worker = Entity(game, "machine"), Held = true });
                Order(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "sensor.machine"),
                    Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect") }, "region.player", true, true, WorkAssignmentMode.All) });
                c.Equal(OperatingAreaCodes.InUse, Work(game).PreviewRemoveOperatingArea("region.player", area.Revision), "assigned runtime area cannot be removed");
                Order(c, game, new SetWorkerHeldCommand { Worker = Entity(game, "sensor.machine"), Held = false });
                Order(c, game, new PostOrderCommand { Plan = "binding.travel", Target = Entity(game, "destination"), Priority = 70 });
                Until(game, null, () => Position(game) == Fix.FromInt(7), "physical partial travel");
                Order(c, game, Edit(game, "region.player", 7)); var narrow = Work(game).ReadOperatingArea("region.player");
                c.That(narrow.Revision > area.Revision, "edit version is not another clock");
                Until(game, null, () => Jobs(game).Any(j => !j.IsHaul && (j.Blocker == WorkPolicyCodes.OutsideArea || j.Blocker == MotionAreaCodes.Outside)), "real area-blocked existing work");
                var stopped = Position(game); twin = factory.Load(game.Save());
                for (int i = 0; i < 3; i++) Step(game, twin);
                c.Equal(stopped, Position(game), "blocked route does not teleport or cross forbidden geometry");
                c.That(!Known(game) && Contact(game).Progress.Equals(WorkProgress.Zero), "outside contact earns no discovery");
                c.Equal(initial.ShapeJson, area.ShapeJson, "held immutable area facts retained");
                c.That(area.ShapeJson != narrow.ShapeJson, "later area edit does not mutate held facts");
                Order(c, game, Edit(game, "region.player", 20), twin);
                Until(game, twin, () => Position(game) == Fix.FromInt(10), "same physical job resumes in expanded area");
                Order(c, game, new PostOrderCommand { Plan = "region.inspect", Target = Entity(game, "destination"), Priority = 70 });
                Until(game, null, () => Contact(game).Progress.WholeTicks >= 4, "actual direct-region dwell");
                c.That(!Known(game) && Contact(game).RequiredSteps == 12, "positive game-time discovery not an area edit reward");
                var partial = Contact(game); twin = factory.Load(game.Save()); long at = game.Clock.StepIndex;
                game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin); c.Equal(at, game.Clock.StepIndex, "single paused clock freezes area-bound work and contact");
                game.Clock.Resume(); twin.Clock.Resume(); Until(game, twin, () => Known(game), "earned direct-region discovery");
                c.That(partial.Progress.WholeTicks < 12 && !Known(game, 1), "detached partial facts and private knowledge");
                c.Equal(SpatialLayerReadStatus.Success, layers.ReadLayer("point.terrain", new PointRegion(Fix.FromInt(10), Fix.One, 1), values), "regional layer rule accepts actual direct content contract");
                c.Equal(SpatialLayerReadStatus.Unavailable, layers.ReadLayer("point.terrain", new PointRegion(Fix.FromInt(5), Fix.One, 1), values), "wide worker area does not widen fixed discovered region");
                c.Equal(0, values.Count, "outside discovery publishes no prefix");
                c.Equal(SpatialLayerReadStatus.Unavailable, game.Seat(1).View.Feature<PointLayerView>().ReadLayer("point.terrain", new PointRegion(Fix.FromInt(10), Fix.One, 1), values), "foreign region knowledge stays private");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "earned region/private area/policy save");
                c.Equal(Fix.FromInt(19), Stock(game, "source"), "real one-unit supply balance"); c.Equal(Fix.One, Stock(game, "sensor.machine"), "no material duplication");
                c.Equal(isolated, idle.Hash(), "interleaved area/knowledge state isolated");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "normal shape/policy/physical command stream replays");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "shape", "space", "factory-null", "factory-self", "factory-identity", "factory-shape", "factory-space", "factory-throw" })
            {
                var module = new RegionContentModule(); Action run = () => New(Factory(module, doc => doc.Set("invalid_region_content",
                    Json.Parse("[{\"id\":\"unused.invalid-region\",\"space\":\"field.points\",\"mode\":\"" + mode + "\"}]"))));
                if (mode == "shape" || mode == "space") c.Throws<ContentException>(run, "invalid all-loaded region metadata " + mode);
                else c.Throws<InvalidOperationException>(run, "actual contradictory registered factory " + mode);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "invalid region cannot initialize " + mode);
            }
            foreach (string mode in new[] { "unknown-space", "unknown-policy-area", "unknown-requirement", "unknown-discovery", "bounds", "duplicate-alias" })
            {
                var module = new RegionContentModule();
                c.Throws<ContentException>(() => New(Factory(module, doc => {
                    var region = doc.Get("point_interval_regions")[0];
                    if (mode == "unknown-space") region.Set("space", JsonValue.Of("missing"));
                    if (mode == "unknown-policy-area") doc.Get("work_policies")[0].Set("area", JsonValue.Of("missing"));
                    if (mode == "unknown-requirement") doc.Get("target_region_requirements")[0].Set("region", JsonValue.Of("missing"));
                    if (mode == "unknown-discovery") doc.Get("layer_region_knowledge_visibility")[0].Get("regions")[0].Set("region", JsonValue.Of("missing"));
                    if (mode == "bounds") region.Set("maximum", JsonValue.Number("101"));
                    if (mode == "duplicate-alias") doc.Set("invalid_region_content", Json.Parse("[{\"id\":\"region.travel\",\"space\":\"field.points\",\"mode\":\"factory-self\"}]"));
                })), "invalid registered region reference " + mode);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "bad reference before Initialize " + mode);
            }
        }
    }
}
