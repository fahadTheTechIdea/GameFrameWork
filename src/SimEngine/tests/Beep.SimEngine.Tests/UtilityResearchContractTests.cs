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
    // Authored JSON, player commands/views and the one clock only. No live world or gameplay mutation.
    internal static class UtilityResearchContractTests
    {
        internal const string Case = "Utility research contracts: physical supply governs earned knowledge";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static JsonValue Read(string name) => Json.Parse(FrameworkProofContent.Read(name));
        internal static GameFactory Factory(UtilityResearchContractsModule module, bool independent = true, Action<JsonValue> edit = null,
            GameModule extraModule = null, string extraDeclaration = null, IReadOnlyDictionary<string, string> extraDocuments = null,
            Action<JsonValue, JsonValue> configure = null, string retryProfiles = null, Action<JsonValue> worldEdit = null)
        {
            var tech = Read("technology.json"); var foundation = tech.Get("technologies")[1].Clone();
            foundation.Get("nodes")[1].Set("requirement", JsonValue.Of(independent ? "lab.independent-resources" : "lab.native-resources"));
            foundation.Get("nodes")[1].Set("duration", Json.Parse("{\"numerator\":30,\"denominator\":1,\"unit\":\"minute\"}"));
            tech.Set("technologies", JsonValue.NewArray()); tech.Get("technologies").Add(foundation);
            tech.Set("technology_activity_unlocks", JsonValue.NewArray());
            tech.Set("technology_requirements", JsonValue.NewArray()); tech.Set("work_requirement_groups", JsonValue.NewArray());
            var setup = Read("capacity-contracts.json"); var recipe = setup.Get("transformations")[0];
            recipe.Set("tools", JsonValue.NewObject()); recipe.Set("tool_ports", JsonValue.NewObject());
            recipe.Set("inputs", JsonValue.NewObject()); recipe.Set("input_ports", JsonValue.NewObject());
            recipe.Set("outputs", Json.Parse("{\"supply\":1}")); recipe.Set("output_ports", Json.Parse("{\"supply\":\"material.output\"}"));
            recipe.Remove("entity_output");
            recipe.Remove("output_home");
            recipe.Set("duration", Json.Parse("{\"numerator\":120,\"denominator\":1,\"unit\":\"minute\"}"));
            recipe.Set("operating_ports", Json.Parse("{\"supply\":\"operating.input\"}"));
            recipe.Set("operating_inputs", Json.Parse("[{\"resource\":\"supply\",\"quantity\":1,\"period\":{\"numerator\":4,\"denominator\":1,\"unit\":\"minute\"}}]"));
            var components = setup.Get("entities")[1].Get("components");
            components.Get("storage").Set("initial_stock", Json.Parse("{\"supply\":16}"));
            components.Get("transformer").Set("automatic_supply", JsonValue.Of(false));
            components.Set("point_dock", Json.Parse("{\"work_roles\":[\"inspect\"],\"resources\":[\"supply\"],\"slots\":[{\"id\":\"service.left\",\"kind\":\"service\",\"offset\":-1},{\"id\":\"service.right\",\"kind\":\"service\",\"offset\":1},{\"id\":\"departure.left\",\"kind\":\"departure\",\"offset\":-2},{\"id\":\"departure.right\",\"kind\":\"departure\",\"offset\":4}]}"));
            components.Set("research_utility", Json.Parse("{\"id\":\"lab.utility\",\"source\":\"contract.process\"}"));
            configure?.Invoke(tech, setup);
            StartupContentAuthoring.Position(setup, "contract.factory", JsonValue.Number("12"));
            var document = Read("utility-research-contracts.json"); edit?.Invoke(document);
            var capacity = new CapacityContractModule(new InitializeProbe());
            var modules = new List<GameModule> { capacity, module }; if (extraModule != null) modules.Add(extraModule);
            var declarations = Json.Parse("[{\"id\":\"proof.capacity-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"capacity-contracts.json\"]},{\"id\":\"proof.utility-research\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"utility-research-contracts.json\"]}]");
            if (extraDeclaration != null) declarations.Add(Json.Parse(extraDeclaration));
            var documents = new Dictionary<string, string> { ["capacity-contracts.json"] = setup.ToString(), ["utility-research-contracts.json"] = document.ToString() };
            if (extraDocuments != null) foreach (var entry in extraDocuments) documents.Add(entry.Key, entry.Value);
            return ConstructionPlanTests.Points(additionalModule: new TechnologyModule(),
                moduleDeclaration: "{\"id\":\"sim.technology\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"technology.json\"]}",
                moduleDocument: tech.ToString(),
                extraModules: modules, extraDeclarations: declarations.ToString(), extraDocuments: documents, retryProfiles: retryProfiles,
                startupProfiles: new[] { "contract.capacity-start", "lab.carrier-start" },
                scenarioEdit: scenario => { foreach (string id in new[] { TechnologyModule.ModuleId, capacity.Id, module.Id }) scenario.Get("modules").Add(JsonValue.Of(id)); if (extraModule != null) scenario.Get("modules").Add(JsonValue.Of(extraModule.Id)); },
                worldEdit: root => { root.Get("entities")[2].Get("components").Get("storage").Set("capacity_policy", JsonValue.Of("contract.carrier")); StartupContentAuthoring.Position(root, "machine", JsonValue.Number("4")); StartupContentAuthoring.Position(root, "source", JsonValue.Number("28")); worldEdit?.Invoke(root); });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static UtilityDemandSnapshot Demand(SimGame game, string id = "contract.process")
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(Entity(game, "contract.factory"), new Code(id), out var facts))
                throw new InvalidOperationException("missing actual utility facts");
            return facts;
        }
        private static ActivityNodeView Research(SimGame game, int order)
        {
            var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes);
            return nodes.Single(n => n.Id == "research");
        }
        private static bool Known(SimGame game, int seat = 0)
        {
            var facts = new List<TechnologySnapshot>(); game.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(facts);
            return facts.Single(n => n.Technology == "research.foundation").Known;
        }
        private static void Control(Check c, SimGame game, bool enabled) => Order(c, game, new ConfigureOperationCommand {
            Entity = Entity(game, "contract.factory"), Operation = new Code("contract.process"), Enabled = enabled, Target = Fix.One });
        private static void EnableCarrier(Check c, SimGame game) => Order(c, game, new ConfigureWorkPolicyCommand {
            Worker = Entity(game, "lab.carrier"), Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, "lab.carrier-area", true, true, WorkAssignmentMode.All) });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current utility contract identity");
            c.Equal(154, EngineInfo.SaveFormat, "same native research/resource/processor codecs");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Faults(c); Invalid(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new UtilityResearchContractsModule(); var factory = Factory(module, independent);
            var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            c.That(module.DirectAlias && !game.Modules.Contains(TycoonModule.ModuleId), "direct independently authored requirement and no business dependency");
            c.Equal(UtilityCodes.Inactive, Demand(game).Blocker, "idle processor is never fabricated readiness");
            c.Equal(TechnologyCodes.Locked, game.Seat(0).Preview(new ConstructCommand { Def = "point.tower", Placement = Json.Parse("{\"space\":\"field.points\",\"position\":32}") }), "knowledge starts locked");
            Enable(c, game); EnableCarrier(c, game);
            Order(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, "lab.research-area", true, true, WorkAssignmentMode.All) });
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recording = game.RecordReplay(limits))
            {
                Order(c, game, new EnqueueTransformationCommand { Transformer = Entity(game, "contract.factory"), Capability = new Code("contract.process"), Transformation = "contract.make" });
                var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "research.foundation", Target = Entity(game, "contract.factory"), Priority = 70 });
                c.That(result.Accepted, "ordinary player posts native utility-dependent research"); int id = checked((int)result.Arg0);
                Until(game, null, () => Research(game, id).Progress.WholeTicks > 0 && !Known(game), "first staffed utility research");
                var before = Research(game, id); var utility = Demand(game);
                c.That(before.Worker.IsValid && before.RequiredTicks == 30 && utility.Consumed.Count > 0, "real traveled worker/time/materials and native utility burn");
                ulong hash = game.Hash(); var proxy = Demand(game, "lab.utility");
                c.That(proxy.ElapsedTicks == utility.ElapsedTicks && proxy.Consumed.Lines.SequenceEqual(utility.Consumed.Lines) && proxy.Blocker == utility.Blocker,
                    "independent utility delegates original execution rather than duplicating it");
                c.Equal(hash, game.Hash(), "codec-guarded utility reads do not burn/reserve work inputs");
                c.That(!game.Seat(1).View.Feature<OperationsView>().TryGetUtilityDemand(Entity(game, "contract.factory"), new Code("lab.utility"), out _),
                    "actual utility progress remains private");
                var privateNodes = new List<ActivityNodeView>();
                c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(id, privateNodes) && privateNodes.Count == 0, "actual research graph remains private");
                Control(c, game, false);
                Until(game, null, () => Research(game, id).State == NodeState.Paused, "ordinary shutdown pauses research");
                var paused = Research(game, id); var stopped = Demand(game);
                c.Equal(UtilityCodes.Inactive, paused.Blocker, "same actual inactive consumer blocker");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "shutdown progress saves original owners");
                for (int i = 0; i < 4; i++) Step(game, twin);
                c.That(Research(game, id).Progress.Equals(paused.Progress) && Demand(game).ElapsedTicks == stopped.ElapsedTicks && !Known(game), "shutdown burns/earns nothing");
                long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "sole clock stops both research and utility");
                game.Clock.Resume(); twin.Clock.Resume();
                Control(c, game, true);
                Until(game, null, () => Research(game, id).Blocker == UtilityCodes.Shortage, "actual fuel shortage before research finishes");
                var shortage = Research(game, id); var exhausted = Demand(game);
                c.That(shortage.Progress.WholeTicks > 0 && !Known(game) && exhausted.Blocker == UtilityCodes.Shortage, "physical shortage retains partial earned knowledge");
                twin = factory.Load(game.Save());
                for (int i = 0; i < 3; i++) Step(game, twin);
                c.That(Research(game, id).Progress.Equals(shortage.Progress) && Demand(game).ElapsedTicks == exhausted.ElapsedTicks, "shortage advances neither owner");
                game.Seat(0).View.TryFindResource("supply", out var supply);
                Order(c, game, new HaulResourcesCommand { Source = Entity(game, "source"), Destination = Entity(game, "contract.factory"),
                    Worker = Entity(game, "lab.carrier"), DestinationPort = new Code("operating.input"), Resource = supply, Ceiling = Fix.FromInt(8), Priority = 90 });
                c.That(!Known(game), "haul command never teleports knowledge or delivery");
                twin = factory.Load(game.Save());
                try { Until(game, twin, () => Known(game), "physical refuel resumes earned research"); }
                catch (InvalidOperationException error)
                {
                    var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                    throw new InvalidOperationException(error.Message + " carrier=" + Stock(game, "lab.carrier") + " factory=" + Stock(game, "contract.factory") +
                        " research=" + Research(game, id).State + "/" + Research(game, id).Progress + "/" + Research(game, id).Blocker +
                        " jobs=" + string.Join(";", jobs.Select(j => j.Id + "/" + j.Worker + "/" + j.State + "/" + j.Blocker + "/toil=" + j.ToilIndex + "/loaded=" + j.Loaded)), error);
                }
                c.That(Known(game) && !Known(game, 1) && game.Clock.StepIndex >= at + 29, "only own completed native graph learns");
                c.Equal(Code.None, game.Seat(0).Preview(new ConstructCommand { Def = "point.tower", Placement = Json.Parse("{\"space\":\"field.points\",\"position\":32}") }), "earned research unlocks original construction");
                var finalUtility = Demand(game);
                c.Equal(Fix.FromInt(36), Stock(game, "source") + Stock(game, "machine") + Stock(game, "lab.carrier") + Stock(game, "contract.factory")
                    + Fix.FromInt(8) + finalUtility.Consumed.AmountOf(supply), "opening stock equals physical stock plus research and utility consumption");
                c.That(before.Progress.WholeTicks > 0 && before.Progress.WholeTicks < 30 && utility.ElapsedTicks < finalUtility.ElapsedTicks, "retained facts remain detached");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed physical utility research reloads");
                c.Equal(idleHash, idle.Hash(), "interleaved session retains its own locked research and resources");
                c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "normal commands/clock/physical interruption replay");
            }
        }
        private static void Faults(Check c)
        {
            foreach (UtilityResearchFault fault in Enum.GetValues(typeof(UtilityResearchFault)))
            foreach (bool work in new[] { false, true })
            {
                var module = new UtilityResearchContractsModule(); var game = New(Factory(module)); module.Arm(fault);
                long at = game.Clock.StepIndex; bool returned = false;
                c.Throws<InvalidOperationException>(() => {
                    if (work)
                    {
                        var facts = new List<WorkRequirementView>();
                        game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("research.foundation", Entity(game, "contract.factory"), facts);
                    }
                    else Demand(game, "lab.utility");
                    returned = true;
                }, "actual utility " + (work ? "requirement" : "view") + " refuses " + fault);
                c.That(!returned && module.Reads == 1 && game.Fault?.Phase == "observation.provider", "utility fault withholds successful result");
                c.That(module.MutationCompleted == (fault != UtilityResearchFault.Exception), "actual callback mutation completed before guard");
                c.That(game.Update() == 0 && game.Clock.StepIndex == at, "faulted utility cannot advance game clock");
                c.Throws<InvalidOperationException>(() => game.Hash(), "utility fault cannot hash");
                c.Throws<InvalidOperationException>(() => game.Save(), "utility fault cannot save");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing", "empty", "bad-key" })
                c.Throws<ContentException>(() => New(Factory(new UtilityResearchContractsModule(), edit: root => {
                    var leaf = root.Get("independent_utility_requirements")[0];
                    if (mode == "missing") leaf.Remove("source");
                    if (mode == "empty") leaf.Set("source", JsonValue.Of(""));
                    if (mode == "bad-key") leaf.Set("consumer", JsonValue.Of("contract.process"));
                })), "strict JSON utility leaf rejects " + mode);
            var game = New(Factory(new UtilityResearchContractsModule(), edit: root => root.Get("independent_utility_requirements")[0].Set("source", JsonValue.Of("missing.consumer"))));
            var facts = new List<WorkRequirementView>(); ulong hash = game.Hash();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("research.foundation", Entity(game, "contract.factory"), facts), "explicit absent runtime consumer still diagnoses");
            c.That(facts.Any(f => f.Result.Blocker == UtilityWorkRequirementDeclaration.MissingConsumer), "absent named utility is typed pending, never substituted");
            c.Equal(hash, game.Hash(), "missing consumer diagnostic leaves original state untouched");
        }
    }
}
