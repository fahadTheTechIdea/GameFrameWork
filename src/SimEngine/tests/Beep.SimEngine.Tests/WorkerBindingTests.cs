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
    // Author JSON and drive ordinary player commands, Update, immutable views and saves only.
    internal static class WorkerBindingTests
    {
        internal const string Case = "Worker bindings: independent metadata drives native physical work and pure observations";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Create(WorkerBindingModule module, bool independent = true, string rate = "1.5",
            InitializeProbe initialize = null, Action<JsonValue> edit = null)
            => RecipeObservationTests.Create(new RecipeObservationModule(), management: true, initialize: initialize,
                extraModules: new[] { module },
                extraDeclarations: "[{\"id\":\"proof.worker-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]}]",
                worldEdit: root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
                    root.Get("entity_states").Add(Json.Parse("{\"id\":\"worker.productive\",\"state\":\"worker.productive\",\"priority\":25}"));
                    root.Get("entity_states").Add(Json.Parse("{\"id\":\"worker.loading\",\"state\":\"worker.loading\",\"priority\":35}"));
                    root.Get("entity_states").Add(Json.Parse("{\"id\":\"worker.stopped\",\"state\":\"worker.stopped\",\"priority\":65}"));
                    var parts = root.Get("entities")[2].Get("components"); var metadata = parts.Get("worker").Clone();
                    metadata.Set("work_rate", JsonValue.Number(rate));
                    metadata.Get("states").Set("working", JsonValue.Of("worker.productive"));
                    metadata.Get("states").Set("loading", JsonValue.Of("worker.loading"));
                    metadata.Get("states").Set("stopped", JsonValue.Of("worker.stopped"));
                    if (independent) { parts.Remove("worker"); parts.Set("independent_worker", metadata); }
                    else parts.Set("worker", metadata);
                    edit?.Invoke(root);
                });
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static WorkerExecutionView Read(SimGame game)
        {
            Code result = View(game).ReadWorkerExecution(Entity(game, "machine"), out var value);
            if (!result.IsNone) throw new InvalidOperationException("worker facts " + result);
            return value;
        }
        private static List<OrderView> Orders(SimGame game) { var result = new List<OrderView>(); View(game).CopyOrders(result); return result; }
        private static void Reach(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 480 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex +
                " state=" + game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").State +
                " orders=" + string.Join(";", Orders(game).Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current worker contract revision");
            c.Equal(154, EngineInfo.SaveFormat, "same assignments/held/home state payload");
            c.That(typeof(Worker).BaseType == typeof(Component) && typeof(IStatefulWorker).IsAssignableFrom(typeof(Worker)),
                "original worker consumes contracts, not native definition inheritance");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Admission(c); Faults(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new WorkerBindingModule(); var factory = Create(module, independent); var game = ConstructionPlanTests.NewPoints(factory);
            c.Equal(independent, module.NativeBinding, "independent authored contract uses original runtime");
            var retained = Read(game); ulong hash = game.Hash();
            c.That(retained.BaseRate == Fix.FromRatio(3, 2) && retained.EffectiveRate == retained.BaseRate &&
                retained.Roles.SequenceEqual(new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }), "sole explicit authored rate and ordered roles");
            c.Throws<NotSupportedException>(() => ((IList<WorkerRoleId>)retained.Roles).Clear(), "worker view detached read-only roles");
            c.Equal(Codes.NotFound, View(game, 1).ReadWorkerExecution(Entity(game, "machine"), out var foreign), "worker metadata company-private");
            c.That(foreign == null && hash == game.Hash(), "refused view withholds facts and mutates nothing");
            var isolated = ConstructionPlanTests.NewPoints(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Enable(c, game);
                Order(c, game, new PostOrderCommand { Target = Entity(game, "destination"), Plan = "inspect" });
                Reach(game, null, () => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").State == new Code("point.travel"), "physical travel");
                var moving = game.Seat(0).Snapshot; var twin = factory.Load(game.Save());
                long at = game.Clock.StepIndex; hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(game.Clock.StepIndex == at && game.Hash() == hash, "one clock pause stops physical work");
                game.Clock.Resume(); twin.Clock.Resume();
                Reach(game, twin, () => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").State == new Code("worker.productive"), "authored productive state");
                long workAt = game.Clock.StepIndex;
                Reach(game, twin, () => View(game).TryReadActivityCompletion("activity:inspect", Entity(game, "destination"), out _), "authored rated work completion");
                c.That(game.Clock.StepIndex >= workAt + 3, "six-minute work at authored 1.5 rate needs physical updates");
                c.That(moving.Entities.Single(e => e.Def == "machine").State == new Code("point.travel"), "retained travel snapshot unchanged");
                Order(c, game, new EnqueueTransformationCommand { Transformer = Entity(game, "contract.factory"),
                    Capability = new Code("contract.process"), Transformation = "contract.make" });
                var supplyTwin = factory.Load(game.Save()); bool loaded = false;
                Reach(game, supplyTwin, () => {
                    loaded |= game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").State == new Code("worker.loading");
                    return Process(game).QueueCount == 0 && game.Seat(0).Snapshot.Entities.Any(e => e.Def == "contract.result");
                }, "same worker physically loads delivers and supplies manufacturing");
                c.That(loaded, "actual pickup/delivery uses authored loading state");
                c.That(Stock(game, "source") == Fix.FromInt(16) && Stock(game, "machine") == Fix.Zero &&
                    Stock(game, "contract.factory") == Fix.One, "one resource authority conserves input3 and reusable tool1");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native four-field worker saved continuation");
                c.Equal(isolatedHash, isolated.Hash(), "worker declarations do not share assignments across sessions");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary rated work and supply replay");
            }
            c.That(retained.BaseRate == Read(game).BaseRate && retained.Roles.Count == 2, "retained authored worker facts stable");
        }
        private static void Admission(Check c)
        {
            foreach (bool independent in new[] { false, true })
                foreach (string bad in new[] { "missing-rate", "zero", "empty-roles", "duplicate-roles", "unknown-role", "missing-state", "unknown-state", "extra-state", "unused" })
                {
                    var initialize = new InitializeProbe();
                    c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Create(new WorkerBindingModule(), independent, initialize: initialize, edit: root => {
                        string key = independent ? "independent_worker" : "worker";
                        var entity = root.Get("entities")[2];
                        if (bad == "unused") { entity = entity.Clone(); entity.Set("id", JsonValue.Of("worker.unused")); root.Get("entities").Add(entity); }
                        var part = entity.Get("components").Get(key);
                        if (bad == "missing-rate") part.Remove("work_rate");
                        if (bad == "zero" || bad == "unused") part.Set("work_rate", JsonValue.Number("0"));
                        if (bad == "empty-roles") part.Set("roles", Json.Parse("[]"));
                        if (bad == "duplicate-roles") part.Set("roles", Json.Parse("[\"inspect\",\"inspect\"]"));
                        if (bad == "unknown-role") part.Set("roles", Json.Parse("[\"missing\"]"));
                        if (bad == "missing-state") part.Get("states").Remove("loading");
                        if (bad == "unknown-state") part.Get("states").Set("working", JsonValue.Of("missing"));
                        if (bad == "extra-state") part.Get("states").Set("extra", JsonValue.Of("ready"));
                    })), "worker content refuses before Initialize " + independent + "/" + bad);
                    c.Equal(0, initialize.Initializations, "invalid worker has no initial game state");
                }
            foreach (string bad in new[] { "rate", "roles", "states" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Create(new WorkerBindingModule(), initialize: initialize,
                    edit: root => root.Get("entities")[2].Get("components").Get("independent_worker").Set("proof_fault", JsonValue.Of(bad)))), "contract metadata failure " + bad);
                c.Equal(0, initialize.Initializations, "lying metadata refused before Initialize");
            }
        }
        private static void Faults(Check c)
        {
            foreach (string point in new[] { "rate", "roles", "states" })
                foreach (WorkerBindingFault mode in Enum.GetValues(typeof(WorkerBindingFault)))
                    foreach (bool update in new[] { false, true })
                    {
                        var module = new WorkerBindingModule(); var factory = Create(module); var game = ConstructionPlanTests.NewPoints(factory);
                        long at = game.Clock.StepIndex; module.Arm(point, mode);
                        WorkerExecutionView published = null;
                        if (update) c.Equal(0, game.Update(), "faulted worker completes no Update " + point + "/" + mode);
                        else c.Throws<InvalidOperationException>(() => View(game).ReadWorkerExecution(Entity(game, "machine"), out published),
                            "actual worker metadata observation guarded " + point + "/" + mode);
                        c.That(published == null && module.Reads == 1, "impure observation publishes no worker facts");
                        c.That(game.Fault != null && game.Fault.Phase == "observation.provider" && game.Clock.StepIndex == at, "first provider fault stops sole game clock");
                        c.Equal(0, game.Update(), "faulted session cannot resume worker scheduling");
                        c.Throws<InvalidOperationException>(() => game.Save(), "faulted worker cannot checkpoint");
                        c.Throws<InvalidOperationException>(() => game.Hash(), "faulted worker cannot publish healthy hash");
                    }
        }
    }
}
