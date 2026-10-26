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
    // Thin JSON authoring, ordinary player input, Update and detached observations only.
    internal static class CapacityContentContractTests
    {
        internal const string Case = "Capacity content contracts: independent JSON admission, physical hauling and manufacturing";
        private static readonly ReplayLimits Limits = new ReplayLimits(4 * 1024 * 1024, 3000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe initialize, out CapacityContractModule proof,
            Action<JsonValue> edit = null, Action<JsonValue> rootEdit = null)
        {
            proof = new CapacityContractModule(initialize);
            var document = Json.Parse(FrameworkProofContent.Read("capacity-contracts.json")); edit?.Invoke(document);
            return ConstructionPlanTests.Points(extraModules: new GameModule[] { proof },
                extraDeclarations: "[{\"id\":\"proof.capacity-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"capacity-contracts.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "capacity-contracts.json", document.ToString() } },
                startupProfiles: new[] { "contract.capacity-start" },
                scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(CapacityContractModule.ModuleId)),
                worldEdit: root =>
                {
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
                throw new InvalidOperationException("capacity step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 480 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex +
                " source=" + Stock(game, "source") + " destination=" + Stock(game, "destination") + " cargo=" + Stock(game, "machine"));
        }
        private static CommandResult Order(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player preview");
            c.Equal(hash, game.Hash(), "preview pure");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary player command accepted"); return result;
        }
        private static void Enable(Check c, SimGame game) => Order(c, game, new ConfigureWorkPolicyCommand
        {
            Worker = Entity(game, "machine"),
            Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All)
        });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine build");
            c.Equal(154, EngineInfo.SaveFormat, "original inventories, WIP and job codecs unchanged");
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var proof); var game = New(factory);
            c.Equal(1, initialize.Initializations, "one Initialize after complete resolution");
            c.That(proof.IndependentAlias && proof.OriginalBindings && proof.NativeParity, "direct owned aliases and original native consumers share exact evaluator");
            c.Equal(CapacityStatus.Fits, proof.Mixed.Status, "whole mixed stock and promises fit");
            c.Equal(Fix.FromInt(2), proof.MixedOffer.Amount, "volume constrains native bulk offer");
            c.Equal(Fix.FromInt(4) + Fix.Half, proof.Mixed.Usage.Single(v => v.Unit == new UnitId("unit.capacity.mass")).Projected, "stock and promised mass measured together");
            c.Equal(Fix.FromInt(3), proof.Mixed.Usage.Single(v => v.Unit == new UnitId("unit.capacity.volume")).Projected, "independent volume usage");
            c.Equal(CapacityStatus.ResourceRejected, proof.RejectedOffer, "specialized Check is not bypassed by Offer");
            c.Equal(CapacityStatus.ResourceRejected, proof.RejectedQuantum, "specialized Check is not bypassed by Quantum");
            c.Equal(2 * Fix.One.Raw, proof.FractionalQuantum.Raw, "continuous native amount obeys discrete measured capacity");
            c.Equal(CapacityStatus.FractionalDiscrete, proof.FractionalCheck, "inexact discrete measured amount refuses");
            c.Equal(Fix.FromInt(2), proof.FractionalOffer.Amount, "offer floors to exact measured quantum");
            c.Equal(CapacityStatus.InvalidRequest, proof.ZeroInstances, "zero scale refuses");
            c.Equal(CapacityStatus.OutOfRange, proof.WideInstances, "wide exact arithmetic refuses representational overflow");
            c.Equal(CapacityStatus.LimitExceeded, proof.Closed, "explicit zero capacity remains closed");
            var policies = new List<CapacityPolicyInfo>(); ulong hash = game.Hash(); game.Seat(0).View.CopyCapacityPolicies(policies);
            var retained = policies.Single(p => p.Id == "contract.carrier");
            c.That(policies.Select(p => p.Id).SequenceEqual(policies.Select(p => p.Id).OrderBy(id => id, StringComparer.Ordinal)), "contract metadata sorted deterministically");
            c.Equal(Fix.FromInt(8), retained.Limits.Single().Amount, "actual independent capacity projected");
            c.Throws<NotSupportedException>(() => ((IList<CapacityLimitInfo>)retained.Limits).Clear(), "detached authored metadata immutable");
            c.Equal(hash, game.Hash(), "metadata observation pure");
            Physical(c, factory, game); Manufacturing(c, factory); Invalid(c);
            c.Equal(Fix.FromInt(8), retained.Limits.Single().Amount, "retained facts unchanged after sessions advance");
        }
        private static void Physical(Check c, GameFactory factory, SimGame game)
        {
            var recorder = game.RecordReplay(Limits); Enable(c, game);
            game.Seat(0).View.TryFindResource("supply", out var supply);
            var source = Entity(game, "source"); var destination = Entity(game, "destination"); var machine = Entity(game, "machine");
            var offer = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source, destination, supply, Fix.FromInt(20));
            c.That(offer.Status == HaulStatus.Ready && offer.Amount == Fix.FromInt(8), "original carrier capacity limits actual haul offer");
            int job = checked((int)Order(c, game, new HaulResourcesCommand { Source = source, Destination = destination, Worker = machine, Resource = supply, Ceiling = Fix.FromInt(20), Priority = 20 }).Arg0);
            Until(game, null, () => Stock(game, "machine") > Fix.Zero && Stock(game, "machine") < Fix.FromInt(8), "partial loading");
            c.That(Position(game) == Fix.FromInt(20) && Stock(game, "destination") == Fix.Zero, "load after physical approach, no instant destination delivery");
            c.Equal(Fix.FromInt(20), Stock(game, "source") + Stock(game, "machine") + Stock(game, "destination"), "native stock conserved mid-load");
            var twin = factory.Load(game.Save()); ulong hash = game.Hash(); long time = game.Clock.StepIndex;
            game.Clock.Pause(); twin.Clock.Pause();
            for (int i = 0; i < 3; i++) Step(game, twin);
            c.That(game.Clock.StepIndex == time && game.Hash() == hash, "sole clock freezes capacity commitments and physical progress");
            game.Clock.Resume(); twin.Clock.Resume();
            Until(game, twin, () => Stock(game, "destination") > Fix.Zero && Stock(game, "destination") < Fix.FromInt(8), "partial unloading");
            c.Equal(Fix.FromInt(10), Position(game), "unloading only at actual destination");
            c.Equal(Fix.FromInt(20), Stock(game, "source") + Stock(game, "machine") + Stock(game, "destination"), "native stock conserved mid-delivery");
            twin = factory.Load(game.Save());
            Until(game, twin, () => Stock(game, "destination") == Fix.FromInt(8) && Stock(game, "machine") == Fix.Zero && !LiveJob(game, job), "first bounded delivery");
            c.That(Stock(game, "source") == Fix.FromInt(12) && game.Clock.StepIndex > 20, "first haul takes real game time");
            var privateJobs = new List<WorkJobView>(); game.Seat(1).View.Feature<WorkView>().CopyJobs(privateJobs);
            c.Equal(0, privateJobs.Count, "cargo jobs company private");
            offer = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source, destination, supply, Fix.FromInt(20));
            c.That(offer.Status == HaulStatus.Ready && offer.Amount == Fix.FromInt(2), "actual remaining destination capacity clips next haul");
            int second = checked((int)Order(c, game, new HaulResourcesCommand { Source = source, Destination = destination, Worker = machine, Resource = supply, Ceiling = Fix.FromInt(20), Priority = 20 }).Arg0);
            twin = factory.Load(game.Save());
            Until(game, twin, () => Stock(game, "destination") == Fix.FromInt(10) && Stock(game, "machine") == Fix.Zero && !LiveJob(game, second), "destination exactly full");
            c.That(Stock(game, "source") == Fix.FromInt(10) && Stock(game, "destination") == Fix.FromInt(10), "exact final conservation and capacity");
            c.Equal(HaulStatus.NoCapacity, game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source, destination, supply, Fix.One).Status, "full destination refuses another haul");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "delivered commitments checkpoint identically");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "physical commands and sole-clock pauses replay");
            var fresh = New(factory); c.That(Stock(fresh, "source") == Fix.FromInt(20) && Stock(fresh, "destination") == Fix.Zero, "new interleaved session isolated");
        }
        private static TransformationSnapshot Process(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Entity(game, "contract.factory"), new Code("contract.process"), out var snapshot))
                throw new InvalidOperationException("missing named factory");
            return snapshot;
        }
        private static void Manufacturing(Check c, GameFactory factory)
        {
            var game = New(factory); Enable(c, game); var recorder = game.RecordReplay(Limits);
            Order(c, game, new EnqueueTransformationCommand { Transformer = Entity(game, "contract.factory"), Capability = new Code("contract.process"), Transformation = "contract.make" });
            Until(game, null, () => Process(game).Phase == TransformationPhase.Working, "physical supplied WIP manufacturing");
            var retained = Process(game); c.That(retained.QueueCount == 1 && retained.Progress.WholeTicks < retained.Required, "work-in-progress takes authored time");
            c.That(Stock(game, "source") < Fix.FromInt(20) && game.Seat(0).Snapshot.Entities.All(e => e.Def != "contract.result"), "original physical supply consumed before result exists");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Entity(game, "contract.factory"), new Code("contract.process"), out _), "WIP is private");
            var twin = factory.Load(game.Save());
            Until(game, twin, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "contract.result") && Process(game).QueueCount == 0, "timed manufacturing completion");
            c.That(Stock(game, "source") == Fix.FromInt(16) && Stock(game, "contract.factory") == Fix.One && Stock(game, "machine") == Fix.Zero, "three inputs consumed, one reusable tool returned through original owners");
            c.Equal(30L, retained.Required, "engine resolves authored manufacturing duration");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed independent WIP capacity checkpoint");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "ordinary manufacturing inputs replay");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing", "empty", "null", "negative", "fractional", "unknown", "dimension", "unused", "trait", "unknown-key" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, edit: root =>
                {
                    var policy = root.Get("independent_capacity_policies")[mode == "unused" ? 8 : 0];
                    if (mode == "missing") policy.Remove("limits");
                    if (mode == "empty") policy.Set("limits", JsonValue.NewObject());
                    if (mode == "null") policy.Set("limits", JsonValue.Null());
                    if (mode == "negative") policy.Get("limits").Set("unit.capacity.count", JsonValue.Number("-1"));
                    if (mode == "fractional") policy.Get("limits").Set("unit.capacity.count", JsonValue.Number("1.5"));
                    if (mode == "unknown" || mode == "unused") policy.Get("limits").Set("missing.unit", JsonValue.Number("1"));
                    if (mode == "dimension") policy.Get("limits").Set("unit.supply", JsonValue.Number("1"));
                    if (mode == "trait") policy.Set("required_trait", JsonValue.Of(""));
                    if (mode == "unknown-key") policy.Set("old_capacity", JsonValue.Number("100"));
                })), "invalid independent declaration: " + mode);
                c.Equal(0, initialize.Initializations, "invalid declaration refused before Initialize: " + mode);
            }
            foreach (string selected in new[] { "missing.capacity", "source", null })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, rootEdit: root => root.Get("entities")[2].Get("components").Get("storage").Set("capacity_policy", selected == null ? JsonValue.Null() : JsonValue.Of(selected)))), "missing/null/wrong-kind selected capacity");
                c.Equal(0, initialize.Initializations, "bad binding refused before Initialize");
            }
            var badStock = new InitializeProbe();
            c.Throws<ContentException>(() => New(Factory(badStock, out _, edit: root => root.Get("independent_capacity_policies")[0].Get("limits").Set("unit.capacity.count", JsonValue.Number("19")))), "explicit initial stock cannot exceed selected contract");
            c.Equal(0, badStock.Initializations, "overfull startup refused before Initialize");
        }
    }
}
