using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    // Authored content, player commands, bounded Update and observations; no gameplay rules.
    internal static class WorkerSpecificationTests
    {
        internal static void Register()
        {
            TestRunner.Add("Worker specifications: explicit JSON, definition contracts and private facts", Admission);
            TestRunner.Add("Worker specifications: authored physical rates, role guards and saved continuation", Physical);
        }
        private static JsonValue Parts(JsonValue root) => root.Get("entities")[2].Get("components");
        private static string Text(JsonValue root) { var text = new StringBuilder(); root.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(string rate = "1", bool external = false, Action<JsonValue> edit = null)
        {
            var root = Content.Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("activities")[0].Get("nodes")[0].Set("duration", Content.Json.Parse("{\"numerator\":2,\"denominator\":5,\"unit\":\"hour\"}"));
            if (external)
            {
                Parts(root).Remove("worker");
                Parts(root).Set("contract_worker", Content.Json.Parse("{\"role\":\"inspect\",\"rate\":" + rate + ",\"states\":{\"working\":\"processing\",\"stopped\":\"blocked\"}}"));
            }
            else Parts(root).Get("worker").Set("work_rate", JsonValue.Number(rate));
            WorkerExecutionTestContent.Add(root);
            edit?.Invoke(root);
            return PointGame.Factory(Text(root));
        }
        private static SimGame New(GameFactory factory, string scenario = "points")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 811 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Owner == new PlayerId(0) && entity.Def.Id == id) return entity;
            throw new InvalidOperationException("missing specification entity " + id);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static WorkerExecutionView Read(SimGame game, string id = "machine")
        {
            Code refusal = game.Seat(0).View.Feature<WorkView>().ReadWorkerExecution(Find(game, id).Id, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException(refusal.ToString()); return value;
        }
        private static JsonValue TileParts(JsonValue root)
        {
            var values = root.Get("entities");
            for (int i = 0; i < values.Count; i++) if (values[i].Get("id").Text == "crane") return values[i].Get("components");
            throw new InvalidOperationException("missing authored crane");
        }
        private static GameFactory TileFactory(Action<JsonValue> edit)
        {
            var root = Content.Json.Parse(Colony.Defs); edit(root); return Colony.Factory(Text(root));
        }
        private static void Admission(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current clean contract revision");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no duplicated rate/roles save payload");
            c.That(typeof(IWorker).GetProperty("WorkRate") == null && typeof(Worker).GetProperty("WorkRate") == null &&
                typeof(ContractWorker).GetProperty("WorkRate") == null, "runtime base getter removed without compatibility alias");
            foreach (bool external in new[] { false, true })
            {
                string component = external ? "contract_worker" : "worker", field = external ? "rate" : "work_rate";
                c.Throws<ContentException>(() => New(Factory(external: external, edit: root => Parts(root).Get(component).Remove(field))), "required authored rate " + component);
                foreach (var bad in new[] { JsonValue.Null(), JsonValue.Of("1"), JsonValue.Of(true), JsonValue.Number("0"),
                    JsonValue.Number("-1"), JsonValue.Number("2147483648"), JsonValue.Number("0.00000000001") })
                    c.Throws<ContentException>(() => New(Factory(external: external, edit: root => Parts(root).Get(component).Set(field, bad))), "invalid rate has no implicit fallback " + component + "/" + bad);
                foreach (string rate in new[] { "0.5", "1.5", "2147483647" })
                {
                    var factory = Factory(rate, external); var game = New(factory); var worker = Find(game, "machine");
                    var definition = worker.Def.Component<IWorkerDefinition>(); var value = Read(game); ulong hash = game.Hash();
                    c.Equal(Fix.Parse(rate), definition.WorkRate, "one authored definition owns exact base rating");
                    c.Equal(definition.WorkRate, value.BaseRate, "private facts report actual definition rate");
                    c.Equal(value.BaseRate, value.EffectiveRate, "identity modifier does not invent another base");
                    c.Equal(definition.Roles[0], value.Roles[0], "private facts retain declared role ordering");
                    c.That(WorkerExecutionContracts.Can(game.World, worker, new WorkerRoleId("inspect")) &&
                        !WorkerExecutionContracts.Can(game.World, worker, WorkerRoleId.None), "canonical role eligibility matches declaration");
                    c.Throws<NotSupportedException>(() => ((IList<WorkerRoleId>)value.Roles).Clear(), "role facts are detached read-only");
                    c.Throws<NotSupportedException>(() => ((IList<WorkerRoleId>)definition.Roles).Clear(), "actual native/public metadata is immutable");
                    c.Throws<InvalidOperationException>(() => ((ComponentDef)definition).Read(new JsonReader(Content.Json.Parse("{}"), "late worker")), "actual worker definition cannot be reauthored");
                    c.Throws<InvalidOperationException>(() => ((ComponentDef)definition).Resolve(null), "actual worker definition cannot be re-resolved");
                    c.Equal(Fix.Parse(rate), definition.WorkRate, "refused metadata mutation preserves the authored rating");
                    c.Equal(hash, game.Hash(), "specification and eligibility reads are pure");
                    c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "positive authored specification restores through manifest identity");
                }
            }
            c.Throws<ContentException>(() => New(TileFactory(root => TileParts(root).Get("mover").Remove("class")), "colony"), "mover class required");
            foreach (var bad in new[] { JsonValue.Null(), JsonValue.Of(""), JsonValue.Of(" "), JsonValue.Of("missing"), JsonValue.Number("1") })
                c.Throws<ContentException>(() => New(TileFactory(root => TileParts(root).Get("mover").Set("class", bad)), "colony"), "invalid class never becomes land " + bad);
            foreach (var bad in new[] { JsonValue.Null(), JsonValue.Of("5"), JsonValue.Number("0"), JsonValue.Number("-1") })
                c.Throws<ContentException>(() => New(TileFactory(root => TileParts(root).Get("mover").Set("speed", bad)), "colony"), "mover speed stays explicit positive " + bad);
            var tile = New(TileFactory(root => { }), "colony"); var mover = Find(tile, "crane").Def.Component<MoverDef>();
            ulong tileHash = tile.Hash();
            c.Throws<InvalidOperationException>(() => mover.Read(new JsonReader(Content.Json.Parse("{}"), "late mover")), "native mover cannot be reauthored");
            c.Throws<InvalidOperationException>(() => mover.Resolve(null), "native mover cannot be re-resolved");
            c.Equal("land", mover.MovementClass, "refused mover metadata mutation preserves explicit class");
            c.Equal(tileHash, tile.Hash(), "metadata mutation refusals preserve physical state and content identity");
            foreach (string fault in new[] { "metadata-zero", "metadata-null", "metadata-empty", "metadata-duplicate", "metadata-unknown" })
                c.Throws<ContentException>(() => New(Factory(external: true, edit: root => Parts(root).Get("contract_worker").Set("specification_fault", JsonValue.Of(fault)))), "public worker metadata refuses before Initialize " + fault);
            foreach (var bad in new[] { JsonValue.Null(), JsonValue.Of("inspect"), Content.Json.Parse("[]"),
                Content.Json.Parse("[\"inspect\",\"inspect\"]"), Content.Json.Parse("[\"missing\"]") })
                c.Throws<ContentException>(() => New(Factory(edit: root => Parts(root).Get("worker").Set("roles", bad))), "native roles require exact declared nonempty membership " + bad);
            foreach (string key in new[] { "undeclared_worker", "non_worker_specification" })
                c.Throws<ContentException>(() => New(Factory(edit: root =>
                { Parts(root).Remove("worker"); Parts(root).Remove("worker_execution"); Parts(root).Set(key, Content.Json.Parse("{}")); })),
                    "declared metadata must match actual worker capability " + key);
            var ordinary = Factory("1.5"); var live = New(ordinary); var machine = Find(live, "machine"); var retained = Read(live);
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, live.World.System<WorkSystem>().StateSchemaVersion, "work coordinator identifies declaration admission");
            c.Equal(Codes.NotFound, live.Seat(1).View.Feature<WorkView>().ReadWorkerExecution(machine.Id, out var hidden), "foreign company cannot read specification");
            c.That(hidden == null, "refused private read leaks no roles/rate handles");
            c.Equal(Codes.InvalidArgument, live.Seat(0).View.Feature<WorkView>().ReadWorkerExecution(Find(live, "source").Id, out _), "non-worker never gets fabricated rating");
            c.That(WorkerExecutionTestContent.Control(live, machine.Id, true, Fix.FromRatio(1, 2)).Accepted, "sole operation command controls modifier");
            c.Equal(Fix.FromRatio(3, 2), Read(live).BaseRate, "control cannot mutate authored base");
            c.Equal(Fix.FromRatio(3, 4), Read(live).EffectiveRate, "actual effective rating composes with declared base");
            c.Equal(Fix.FromRatio(3, 2), retained.EffectiveRate, "retained specification/execution facts remain frozen");
            c.Equal(live.Hash(), ordinary.Load(live.Save()).Hash(), "control and authored rating save without new codec");
            var idle = New(Factory(external: true)); var view = idle.Seat(0).View.Feature<WorkView>();
            c.Equal(1, view.IdleWorkers(new WorkerRoleId("inspect")), "private idle query uses canonical declaration");
            c.Equal(0, view.IdleWorkers(new WorkerRoleId("haul")), "undeclared role cannot authorize public worker");
            c.Equal(0, idle.Seat(1).View.Feature<WorkView>().IdleWorkers(new WorkerRoleId("inspect")), "idle role counts respect company privacy");
            c.Throws<ArgumentException>(() => WorkerExecutionContracts.BaseRate(idle.World, Find(live, "machine")), "cross-session worker handle is not an authored alias");
            c.Throws<ArgumentException>(() => WorkerExecutionContracts.BaseRate(idle.World, Find(idle, "source")), "execution entry point rejects actual non-worker");
            RatedRequirements(c);
        }
        private static void RatedRequirements(Check c)
        {
            var factory = PhysicalRecoveryTestContent.Factory(WorkerExecutionTestContent.Author(FrameworkProofContent.Read("worker-candidates.json")));
            var game = ResourceDispositionTests.New(factory); var target = Spawn(game, "candidates.target", 20, 20);
            var loader = Spawn(game, "candidates.loader", 18, 20); var crane = Spawn(game, "candidates.crane", 16, 20);
            foreach (Entity worker in new[] { loader, crane })
            {
                c.That(WorkerExecutionTestContent.Control(game, worker.Id, true, Fix.FromRatio(1, 2)).Accepted, "normal command scales effective work, not declared machine rating");
                var candidates = new List<WorkWorkerCandidate>(); ulong hash = game.Hash();
                c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("candidates.strict", "work", target.Id, candidates), "same engine selector evaluates public rating requirement");
                var offer = candidates.Find(value => value.Worker == worker.Id);
                var rating = WorkerExecutionContracts.BaseRate(game.World, worker);
                c.Equal(WorkRequirementStatus.Satisfied, offer.Result.Status, "authored base rating still meets requirement when effective rate is lower");
                c.Equal(rating, offer.Result.Quantity.Value.Available, "public requirement reports canonical authored rating");
                c.Equal(rating * Fix.FromRatio(1, 2), WorkerExecutionContracts.Read(game.World, worker).EffectiveRate, "execution modifier is separate from machine specification");
                c.Equal(hash, game.Hash(), "requirement/candidate reads allocate no job or physical progress");
            }
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "base/effective distinction survives current metadata and control restore");
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 220 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (done()) return;
            var facts = new List<string>();
            foreach (var order in Board(game).Orders) foreach (var node in order.Nodes)
                facts.Add(order.Id + "/" + node.Id + "/" + node.Progress + "/" + order.Blocker);
            throw new InvalidOperationException(stage + " timed out at tick " + game.Tick + ": " + string.Join(";", facts));
        }
        private static void Physical(Check c)
        {
            foreach (bool external in new[] { false, true }) foreach (string rate in new[] { "0.5", "1.5", "2" })
                TimedWork(c, Factory(rate, external), Fix.Parse(rate), "destination", true);
            var stationary = Factory("0.5", edit: root =>
            {
                Parts(root).Remove("point_presence"); Parts(root).Remove("point_motion"); Parts(root).Remove("storage"); Parts(root).Remove("transporter");
            });
            TimedWork(c, stationary, Fix.FromRatio(1, 2), "machine", false);
            TileWork(c);
            foreach (string fault in new[] { "runtime-null", "runtime-mismatch", "runtime-can-none", "runtime-can-false", "runtime-touch" })
            {
                var factory = Factory(external: true, edit: root => Parts(root).Get("contract_worker").Set("specification_fault", JsonValue.Of(fault)));
                var queried = New(factory);
                c.Throws<InvalidOperationException>(() => Read(queried), "canonical private read refuses lying/impure public role provider " + fault);
                var game = New(factory); game.Update();
                c.That(game.Fault != null, "normal Update faults invalid actual worker " + fault);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted worker cannot checkpoint " + fault);
            }
            var extra = New(Factory(external: true, edit: root => Parts(root).Get("contract_worker").Set("specification_fault", JsonValue.Of("runtime-can-extra"))));
            c.Throws<InvalidOperationException>(() => extra.Seat(0).View.Feature<WorkView>().IdleWorkers(new WorkerRoleId("haul")), "shared role query refuses unauthorized positive Can");
            c.Throws<InvalidOperationException>(() => WorkerExecutionContracts.Can(extra.World, Find(extra, "machine"), new WorkerRoleId("haul")), "dispatch contract never trusts undeclared provider role");
            var guard = New(Factory(external: true, edit: root =>
            {
                Parts(root).Get("contract_worker").Set("specification_fault", JsonValue.Of("runtime-can-extra"));
                root.Get("activities")[0].Get("nodes")[0].Set("roles", Content.Json.Parse("[\"haul\"]"));
            }));
            guard.Seat(0).Submit(new PostOrderCommand { Plan = "inspect", Target = Find(guard, "destination").Id }); guard.Update();
            c.That(guard.Fault != null, "normal dispatch faults provider falsely accepting an undeclared role");
            c.That(Find(guard, "machine").Get<IWorker>().Job == 0, "invalid role never commits an assignment");
        }
        private static void TimedWork(Check c, GameFactory factory, Fix rate, string target, bool travelling)
        {
            var game = New(factory); var isolated = New(factory); var worker = Find(game, "machine");
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = Find(game, target).Id });
            c.That(result.Accepted, "ordinary player activity accepts actual compatible worker");
            var order = Board(game).Order(checked((int)result.Arg0)); var node = order.Nodes[0];
            c.Equal(new WorkProgress(0, 0), node.Progress, "command cannot grant movement or work instantly");
            if (travelling)
            {
                Tick(game);
                c.That(worker.Get<PointPresence>().Position > Fix.Zero && worker.Get<PointPresence>().Position < Fix.FromInt(10), "actual point provider moves over game time");
                c.Equal(new WorkProgress(0, 0), node.Progress, "travelling earns no remote work");
                var movingTwin = factory.Load(game.Save()); c.Equal(game.Hash(), movingTwin.Hash(), "authored specification saves while travelling");
                Reach(game, () => node.Progress.WholeTicks > 0, "specification physical arrival/start", movingTwin);
                c.Equal(game.Hash(), movingTwin.Hash(), "travel continuation uses same authored roles/rate");
                c.Equal(Fix.FromInt(10), worker.Get<PointPresence>().Position, "work occurs only at actual destination");
            }
            else
            {
                c.That(worker.Get<IMotion>() == null && worker.Get<ISpatialPresence>() == null, "stationary metadata needs no invented spatial/motion capability");
                Reach(game, () => node.Progress.WholeTicks > 0, "stationary work start");
            }
            var progress = node.Progress; Tick(game);
            c.Equal(progress.AdvanceUnbounded(rate), node.Progress, "one Update earns exact authored fractional/whole work");
            c.That(WorkerExecutionTestContent.Control(game, worker.Id, true, Fix.Zero).Accepted, "ordinary zero modifier pauses actual work");
            progress = node.Progress;
            var twin = factory.Load(game.Save());
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); Tick(isolated); }
            c.Equal(progress, node.Progress, "zero effective rate grants no work over elapsed updates");
            c.Equal(rate, Read(game).BaseRate, "runtime wait retains sole authored base metadata");
            c.Equal(game.Hash(), twin.Hash(), "saved zero-rate waits preserve actual activity progress");
            c.That(WorkerExecutionTestContent.Control(game, worker.Id, true, Fix.One).Accepted, "resume by ordinary player command");
            c.That(WorkerExecutionTestContent.Control(twin, Find(twin, "machine").Id, true, Fix.One).Accepted, "saved twin resumes through identical command");
            Reach(game, () => !order.IsActive, "authored physical work completion", twin);
            c.Equal(OrderState.Done, order.State, "physical workflow completes through existing coordinator");
            c.Equal(game.Hash(), twin.Hash(), "saved fractional physical work completes deterministically");
            c.Equal(0, Find(isolated, "machine").Get<IWorker>().Job, "interleaved session gets no assignment leakage");
            c.Equal(0, Board(isolated).Orders.Count, "interleaved session gets no activity leakage");
        }
        private static void TileWork(Check c)
        {
            var factory = TileFactory(root =>
            {
                TileParts(root).Get("worker").Set("work_rate", JsonValue.Number("1.5"));
                root.Get("activities")[0].Get("nodes")[0].Set("duration", Content.Json.Parse("{\"numerator\":2,\"denominator\":5,\"unit\":\"hour\"}"));
                WorkerExecutionTestContent.Add(root);
            });
            var game = New(factory, "colony"); var worker = Find(game, "crane");
            // Initial scenario fixture only; work still starts through an ordinary player command.
            var target = Spawn(game, "tower", 10, 16);
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = target.Id });
            c.That(result.Accepted, "tile player order uses explicitly authored class and work rate");
            var order = Board(game).Order(checked((int)result.Arg0)); var node = order.Nodes[0];
            Reach(game, () => node.Progress.WholeTicks > 0, "explicit tile travel/work start");
            c.Equal("land", worker.Def.Component<MoverDef>().MovementClass, "actual native tile class is authored, not invented");
            c.Equal(Fix.FromRatio(3, 2), Read(game, "crane").BaseRate, "tile worker projects same specification contract");
            var progress = node.Progress; Tick(game); c.Equal(progress.AdvanceUnbounded(Fix.FromRatio(3, 2)), node.Progress, "tile work earns actual authored 1.5 rate");
            var twin = factory.Load(game.Save()); Reach(game, () => !order.IsActive, "tile authored work finish", twin);
            c.Equal(OrderState.Done, order.State, "native tile work completes without scheduler changes");
            c.Equal(game.Hash(), twin.Hash(), "tile worker declaration continues identically after save");
        }
    }
}
