using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Authored JSON, public player views and normal Update/save/commands only.
    internal static partial class RecipeObservationTests
    {
        internal const string Case = "Recipe observations: detached rates and atomic guarded metadata";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(ParticipantsCase, Participants);
        }
        private static readonly Code Capability = new Code("contract.process");
        internal static GameFactory Create(RecipeObservationModule module, bool batch = false, bool management = false,
            IReadOnlyList<GameModule> extraModules = null, string extraDeclarations = null,
            IReadOnlyDictionary<string, string> extraDocuments = null, Action<JsonValue, JsonValue> edit = null, InitializeProbe initialize = null, Action<JsonValue> worldEdit = null)
        {
            var setup = Json.Parse(FrameworkProofContent.Read("capacity-contracts.json"));
            setup.Set("transformations", JsonValue.NewArray());
            var authored = Json.Parse(FrameworkProofContent.Read("transformation-metadata.json")).Get("metadata_transformations");
            var flow = management ? Json.Parse(FrameworkProofContent.Read("transformations-contracts.json")).Get("independent_transformations")[0].Clone()
                : authored[batch ? 0 : 1].Clone();
            flow.Remove("fault"); flow.Set("id", JsonValue.Of("contract.make"));
            if (management)
            {
                var components = setup.Get("entities")[1].Get("components");
                components.Set("observed_processor", components.Get("transformer").Clone()); components.Remove("transformer");
                components.Set("management_gate", Json.Parse("{\"id\":\"management.staff\"}"));
            }
            var unused = authored[0].Clone(); unused.Remove("fault"); unused.Set("id", JsonValue.Of("observation.unused"));
            var hidden = unused.Clone(); hidden.Set("id", JsonValue.Of("observation.hidden"));
            var document = JsonValue.NewObject(); document.Set("observed_recipes", JsonValue.NewArray());
            document.Get("observed_recipes").Add(flow); document.Get("observed_recipes").Add(unused); document.Get("observed_recipes").Add(hidden);
            edit?.Invoke(setup, document);
            var capacity = new CapacityContractModule(initialize ?? new InitializeProbe());
            var modules = new List<GameModule> { capacity, module };
            if (extraModules != null) modules.AddRange(extraModules);
            var declarations = Json.Parse("[{\"id\":\"proof.capacity-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"capacity-contracts.json\"]},{\"id\":\"proof.recipe-observations\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"recipe-observation.json\"]}]");
            if (extraDeclarations != null) { var additional = Json.Parse(extraDeclarations); for (int i = 0; i < additional.Count; i++) declarations.Add(additional[i]); }
            var documents = new Dictionary<string, string> { { "capacity-contracts.json", setup.ToString() }, { "recipe-observation.json", document.ToString() } };
            if (extraDocuments != null) foreach (var pair in extraDocuments) documents.Add(pair.Key, pair.Value);
            return ConstructionPlanTests.Points(extraModules: modules, extraDeclarations: declarations.ToString(),
                extraDocuments: documents,
                startupProfiles: new[] { "contract.capacity-start" },
                scenarioEdit: scenario => { foreach (var selected in modules) scenario.Get("modules").Add(JsonValue.Of(selected.Id)); },
                worldEdit: root => {
                    root.Get("entities")[2].Get("components").Get("storage").Set("capacity_policy", JsonValue.Of("contract.carrier"));
                    worldEdit?.Invoke(root);
                });
        }
        private sealed class MutableRate : IResourceRate
        {
            public IResourceDefinition Resource { get; set; }
            public Fix Quantity { get; set; } = Fix.One;
            public long PeriodNumerator { get; set; } = 3;
            public long PeriodDenominator { get; set; } = 2;
            public DurationUnit PeriodUnit { get; set; } = DurationUnit.Minute;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current observation build");
            var metadata = new ResourceSnapshotTests.MutableMetadata();
            var rate = new MutableRate { Resource = metadata }; var retained = new ResourceRateInfo(rate);
            rate.Quantity = Fix.FromInt(9); rate.PeriodNumerator = 7; rate.PeriodUnit = DurationUnit.Hour;
            metadata.SourceTraits[0] = "changed"; metadata.SourceMeasures.Clear(); metadata.Unit.Dimension = "changed";
            c.That(retained.Quantity == Fix.One && retained.PeriodNumerator == 3 && retained.PeriodDenominator == 2
                && retained.PeriodUnit == DurationUnit.Minute, "exact rate scalars detached from mutable source");
            c.That(retained.Resource.QuantityUnit.Dimension == "mass" && retained.Resource.Traits[0] == "original.trait"
                && retained.Resource.CapacityMeasures.Count == 1 && retained.Resource.CapacityMeasures[0].Unit.Dimension == "mass",
                "nested resource, measure and unit facts deeply detached");
            Healthy(c); Faults(c);
            // Reuse the thin physical workflow, not another scheduler or test-side stock mutation.
            TransformationTimingTests.Batch(c, 10, 9);
        }
        private static void Healthy(Check c)
        {
            var module = new RecipeObservationModule(); var factory = Create(module); var game = ConstructionPlanTests.NewPoints(factory);
            var view = game.Seat(0).View.Feature<TransformationManagementView>(); var id = Entity(game, "contract.factory");
            ulong hash = game.Hash();
            c.Equal(Code.None, view.ReadToolConsumptionRates(id, Capability, "contract.make", out var rates), "owned supported authored rates");
            c.That(rates.Count == 1 && rates[0].Quantity == Fix.One && rates[0].PeriodNumerator == 1
                && rates[0].PeriodDenominator == 1 && rates[0].PeriodUnit == DurationUnit.Minute
                && rates[0].Resource.Id == "supply", "same actual exact period/resource facts");
            c.Throws<NotSupportedException>(() => ((IList<ResourceRateInfo>)rates).Clear(), "rate list immutable");
            c.Equal(Code.None, view.ReadToolConsumption(id, Capability, "contract.make", out var bill), "continuous bill projection");
            c.That(bill.IsEmpty, "continuous wear remains rates, not fabricated finite bill");
            c.Equal(Code.None, view.ReadInputPorts(id, Capability, "contract.make", out var inputs), "input projection");
            c.Equal(Code.None, view.ReadOutputPorts(id, Capability, "contract.make", out var outputs), "output projection");
            c.Equal(Code.None, view.ReadToolPorts(id, Capability, "contract.make", out var tools), "tool projection");
            c.Equal(Code.None, view.ReadOperatingPorts(id, Capability, "contract.make", out var operating), "operating projection");
            c.That(inputs.Count == 1 && outputs.Count == 1 && tools.Count == 1 && operating.Count == 1, "all actual named routes preserved");
            c.Equal(Codes.UnknownDef, view.ReadToolConsumptionRates(id, Capability, "absent", out var absent), "missing recipe refusal");
            c.Equal(Codes.UnknownDef, view.ReadToolConsumptionRates(id, Capability, "observation.hidden", out var hidden), "hidden recipe refusal");
            c.Equal(EnqueueTransformationHandler.NotMadeHere, view.ReadToolConsumptionRates(id, Capability, "observation.unused", out var unsupported), "unsupported actual recipe refusal");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadToolConsumptionRates(id, Capability, "contract.make", out var foreign), "foreign company refusal");
            c.Equal(Codes.NotFound, view.ReadToolConsumptionRates(id, Code.None, "contract.make", out var unnamed), "explicit named provider required");
            c.That(absent.Count + hidden.Count + unsupported.Count + foreign.Count + unnamed.Count == 0, "refusals expose no facts");
            c.Equal(hash, game.Hash(), "healthy management reads pure");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                var twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(game.Clock.StepIndex == 0 && game.Hash() == hash, "view boundary changes no paused game time");
                game.Clock.Resume(); twin.Clock.Resume();
                for (int i = 0; i < 12; i++)
                {
                    Step(game, twin); view.ReadToolConsumptionRates(id, Capability, "contract.make", out _);
                }
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "guarded views preserve saved clock replay");
            }
            c.That(rates[0].Quantity == Fix.One && rates[0].Resource.Id == "supply", "retained authored rate unchanged after Updates");
        }
        private static void Faults(Check c)
        {
            foreach (string site in new[] { "recipe", "rate", "resource", "unit", "period", "bill", "ports", "visibility" })
                foreach (ResourceSnapshotFault mode in Enum.GetValues(typeof(ResourceSnapshotFault)))
                {
                    var module = new RecipeObservationModule(); var game = ConstructionPlanTests.NewPoints(Create(module, batch: site == "bill"));
                    var view = game.Seat(0).View.Feature<TransformationManagementView>(); var id = Entity(game, "contract.factory");
                    view.ReadToolConsumptionRates(id, Capability, "contract.make", out var rates);
                    view.ReadInputPorts(id, Capability, "contract.make", out var ports);
                    view.ReadToolConsumption(id, Capability, "contract.make", out var bill);
                    if (site == "bill") c.That(!bill.IsEmpty, "finite authored bill exists before fault; stale facts must be cleared");
                    module.Arm(site, mode);
                    Action read = () =>
                    {
                        if (site == "bill") view.ReadToolConsumption(id, Capability, "contract.make", out bill);
                        else if (site == "ports") view.ReadInputPorts(id, Capability, "contract.make", out ports);
                        else view.ReadToolConsumptionRates(id, Capability, "contract.make", out rates);
                    };
                    c.Throws<InvalidOperationException>(read, "guarded callback refuses " + site + "/" + mode);
                    c.That(site == "bill" ? bill.IsEmpty : site == "ports" ? ports.Count == 0 : rates.Count == 0, "failed observation publishes empty out facts");
                    c.That(game.Fault != null && game.Fault.Phase == "observation.provider", "original scoped fault latched");
                    int reads = module.Reads; var fault = game.Fault;
                    c.Equal(1, reads, "actual nested callback invoked once");
                    c.Equal(0, game.Update(), "faulted session cannot update");
                    c.Throws<InvalidOperationException>(() => game.Save(), "no faulted save");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy hash for partial observation");
                    c.Throws<InvalidOperationException>(read, "poisoned observation cannot retry");
                    c.That(module.Reads == reads && ReferenceEquals(fault, game.Fault), "same first fault and no callback retry");
                }
        }
    }
}
