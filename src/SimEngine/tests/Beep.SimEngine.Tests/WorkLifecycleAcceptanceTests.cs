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
using static Beep.SimEngine.Tests.PermanentRemovalTests;

namespace Beep.SimEngine.Tests
{
    // Authored fixture and public player input/facts only. All rules execute through the original engine.
    internal static class WorkLifecycleAcceptanceTests
    {
        internal const string Case = "Work lifecycle acceptance: permanent loss pause cancel reselection and partial cargo commit once";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static EntityId Entity(SimGame game, string name) => TransformationContentContractTests.Entity(game, name);
        internal static GameFactory Compose(bool half, bool independent)
            => Factory(half,
                modules: independent ? new GameModule[] { new LogisticsRecoveryPolicyProofModule() } : null,
                declarations: independent ? "[{\"id\":\"proof.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\"}],\"documents\":[]}]" : "[]",
                documents: new Dictionary<string,string> { ["transporter-retirement.json"] = FrameworkProofContent.Read("transporter-retirement.json") },
                startup: new[] { "carrier.start" },
                authorWorld: world => {
                    if (!independent) return;
                    var machine = world.Get("entities")[2].Get("components");
                    machine.Remove("transporter");
                    machine.Set("proof_bound_cargo", Json.Parse("{\"cargo\":\"cargo.container\",\"cargo_replacements\":[],\"handling\":\"points.handling\",\"fault\":\"none\"}"));
                },
                authorLifecycle: json => {
                    if (!independent) return;
                    for (int i = 0; i < 2; i++)
                    {
                        var components = json.Get("entities")[i].Get("components"); var worker = components.Get("worker");
                        worker.Set("role", worker.Get("roles")[0]); worker.Remove("roles");
                        worker.Set("rate", worker.Get("work_rate")); worker.Remove("work_rate");
                        worker.Get("states").Remove("loading"); components.Remove("worker"); components.Set("contract_worker", worker);
                    }
                });
        private static ActivityNodeView Node(SimGame g, int order, string node)
        { var rows = new List<ActivityNodeView>(); if (!Work(g).TryCopyNodes(order, rows)) throw new InvalidOperationException("missing active order " + order); return rows.Single(x => x.Id == node); }
        private static Fix Cargo(SimGame g, bool independent) => Facts(g, Entity(g, independent ? "cargo.container" : "machine")).Stock;
        private static bool Live(WorkJobView job) => job.State == JobState.Open || job.State == JobState.Assigned || job.State == JobState.Paused;
        private static void Reach(SimGame g, SimGame twin, Func<bool> done, string phase)
        {
            var trail = new Queue<string>();
            for (int i = 0; i < 512 && !done(); i++)
            {
                var returns = new List<CargoReturnView>();
                g.Seat(0).View.Feature<LogisticsView>().CopyCargoReturns(Entity(g, "machine"), returns);
                trail.Enqueue(g.Clock.StepIndex + ":stock=" + Total(g) + ":jobs=" +
                    string.Join(";", Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.Worker + "/" + x.ToilIndex + "/" + x.Loaded + "/" + x.Blocker)) +
                    ":return=" + string.Join(";", returns.Select(x => x.Stock + "/" + x.Return.Status + "/" + x.Return.Blocker)) +
                    ":destination=" + Facts(g, Entity(g, "destination")).Stock);
                if (trail.Count > 12) trail.Dequeue();
                Step(g, twin);
            }
            if (!done()) throw new InvalidOperationException(phase + " exceeds512Updates " + string.Join(" | ", trail));
        }
        private static void Pause(Check c, SimGame g, long plan, bool paused)
            => Submit(c, g, new ConfigurePlanCommand { Plan = plan, Group = "lifecycle", Priority = 70, Paused = paused });
        private static void Band(Check c, SimGame g, Fix desired, bool enabled)
            => Submit(c, g, new ConfigureStockPolicyCommand { Store = Entity(g, "destination"), Resource = Supply(g),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(enabled, desired, desired,
                    enabled ? desired : Fix.FromInt(40), 30, LogisticsReserveAccess.Protected)) });
        private static void Scenario(Check c, bool half, bool independent, bool cancel)
        {
            var f = Compose(half, independent); var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong isolation = idle.Hash();
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "workflow lifecycle is physical, not compulsory business");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recorder = g.RecordReplay(limits);
            Policy(c, g, "machine", true); Policy(c, g, "lifecycle.a", true, true);
            Submit(c, g, new SetHomeCommand { Worker = Entity(g, "machine"), Home = Entity(g, "source") });
            long build = Submit(c, g, new SubmitPlanCommand { Intent = new PlanIntent("build:extension.facility",
                Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":30}}"), "lifecycle", 70, false) }).Arg0;
            var twin = Save(c, f, g);
            Reach(g, twin, () => Plan(g, build).Order > 0, "real construction site"); int order = Plan(g, build).Order;
            Reach(g, twin, () => Node(g, order, "materials").State == NodeState.Done, "physical material supply/consumption");
            c.That(Node(g, order, "materials").Resources.Single().Spent && Total(g) == Fix.FromInt(12), "eight actual materials spent exactly once before loss");
            Submit(c, g, new PostOrderCommand { Plan = "extension.calibrate", Target = Entity(g, "lifecycle.a") }); twin = Save(c, f, g);
            Reach(g, twin, () => Node(g, order, "assemble").Progress.WholeTicks >= (half ? 4 : 2), "actual first actor assembly prefix");
            Pause(c, g, build, true); var prefix = Node(g, order, "assemble"); EntityId actor = Entity(g, "lifecycle.a");
            var retainedEntity = g.Seat(0).Snapshot.Entities.Single(x => x.Id == actor);
            c.That(prefix.Worker == actor && At(g, actor) == Fix.FromInt(30), "assigned native/independent worker physically earned prefix");
            c.That(Node(g, order, "cure").Progress.WholeTicks > 0 && Node(g, order, "finish").Progress.Equals(WorkProgress.Zero), "parallel earned cure and dependency gate");
            Policy(c, g, "lifecycle.b", true, true);
            long retirement = Submit(c, g, new SubmitPlanCommand { Intent = new PlanIntent("activity:retire.worker-plan",
                Json.Parse("{\"target\":" + actor.Value + ",\"workers\":[{\"node\":\"retire\",\"role\":\"extension.builder\",\"worker\":" + Entity(g, "lifecycle.b").Value + "}]}"),
                "lifecycle", 100, false) }).Arg0;
            twin = Save(c, f, g);
            Reach(g, twin, () => Plan(g, retirement).Order > 0 && Node(g, Plan(g, retirement).Order, "retire").Progress.WholeTicks > 0, "other machine physical retirement prefix");
            c.That(Exists(g, actor) && At(g, Entity(g, "lifecycle.b")) == Fix.FromInt(31), "no premature permanent loss or remote retirement");
            twin = Save(c, f, g);
            Reach(g, twin, () => Plan(g, retirement).State == PlanState.Done, "actual permanent worker loss");
            c.That(!Exists(g, actor), "removed actor really absent from engine snapshot");
            c.Equal(prefix.Progress, Node(g, order, "assemble").Progress, "lost worker retains original earned graph work");
            c.Equal(prefix.JobId, Node(g, order, "assemble").JobId, "same original job survives actual permanent loss");
            c.Equal(Fix.FromInt(12), Total(g), "loss never refunds or repeats spent inputs");
            c.Equal(Codes.NotFound, Work(g).PreviewPlanRepeat(retirement), "removed stable target cannot be replaced by another worker");
            twin = Save(c, f, g);
            g.Clock.Pause(); twin.Clock.Pause(); long time = g.Clock.StepIndex; ulong paused = g.Hash();
            for (int i = 0; i < 3; i++) Step(g, twin);
            c.That(g.Clock.StepIndex == time && g.Hash() == paused && g.Hash() == twin.Hash(), "paused post-loss checkpoint earns no clock/work/cargo");
            g.Clock.Resume(); twin.Clock.Resume(); for (int i = 0; i < 3; i++) Step(g, twin);
            c.Equal(prefix.Progress, Node(g, order, "assemble").Progress, "plan pause still retains prefix after clock resumes");
            if (cancel)
            {
                var command = new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { build }) };
                c.Equal(Code.None, g.Seat(0).Preview(command), "ordinary partial construction cancellation preview");
                int sequence = g.Seat(0).Submit(command); twin = Save(c, f, g); Step(g, twin);
                long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
                c.That(results.Single(x => x.Sequence == sequence).Accepted, "saved post-loss cancel input accepted");
                Reach(g, twin, () => Plan(g, build).State == PlanState.Cancelled &&
                    !g.Seat(0).Snapshot.Entities.Any(x => x.Def == "extension.site"), "actual abandonment cleanup");
                c.That(!g.Seat(0).Snapshot.Entities.Any(x => x.Def == "extension.facility") &&
                    !Work(g).TryReadActivityCompletion("build:extension.facility", EntityId.None, out _), "cancelled prefix grants no terminal effect/history");
            }
            else
            {
                Pause(c, g, build, false); twin = Save(c, f, g);
                Reach(g, twin, () => Node(g, order, "assemble").Worker == Entity(g, "lifecycle.b") &&
                    Node(g, order, "assemble").Progress.WholeTicks > prefix.Progress.WholeTicks, "actual backup reselection/remaining work");
                c.That(At(g, Entity(g, "lifecycle.b")) == Fix.FromInt(30), "replacement physically travels from retirement dock");
                c.Equal(prefix.JobId, Node(g, order, "assemble").JobId, "reselection keeps actual original node/job identity");
                twin = Save(c, f, g); Reach(g, twin, () => Plan(g, build).State == PlanState.Done, "once-only physical facility effect");
                c.That(g.Seat(0).Snapshot.Entities.Count(x => x.Def == "extension.facility") == 1 &&
                    Work(g).TryReadActivityCompletion("build:extension.facility", EntityId.None, out _), "completion commits one real facility/history");
            }
            for (int i = 0; i < 5; i++) Step(g, twin);
            c.Equal(cancel ? 0 : 1, g.Seat(0).Snapshot.Entities.Count(x => x.Def == "extension.facility"), "terminal updates never duplicate final effect");
            c.That(retainedEntity.Id == actor && prefix.Worker == actor && prefix.Progress.WholeTicks >= (half ? 4 : 2), "retained lost-worker facts remain detached");
            c.Equal(Fix.FromInt(12), Total(g), "exact material accounting after cancel or completion");
            Carrier(c, f, g, independent, false); Carrier(c, f, g, independent, true);
            c.Equal(Fix.FromInt(12), Total(g), "both partial pickup/delivery recoveries conserve remaining material");
            var hidden = new List<PlanView>(); Work(g, 1).CopyPlans(hidden); c.Equal(0, hidden.Count, "all loss/cancel/recovery plans company private");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "terminal graph/cargo/stock/clock save identity");
            c.Equal(isolation, idle.Hash(), "interleaved session unchanged by permanent loss/recovery");
            var replay = new ReplayVerifier().Verify(f, recorder.Finish(), limits);
            c.That(replay.Success, "complete public physical lifecycle replay: " + replay.Failure + "/" + replay.Detail);
        }
        private static void Carrier(Check c, GameFactory f, SimGame g, bool independent, bool delivery)
        {
            Band(c, g, Fix.Zero, false);
            int haul = checked((int)Submit(c, g, new HaulResourcesCommand { Source = Entity(g, "source"), Destination = Entity(g, "destination"),
                Worker = Entity(g, "machine"), Resource = Supply(g), Ceiling = Fix.FromInt(8), Priority = 100 }).Arg0);
            var twin = Save(c, f, g);
            Reach(g, twin, () => Job(g, haul).Loaded.IsPositive && Job(g, haul).Loaded < Job(g, haul).Amount &&
                Job(g, haul).ToilIndex == (delivery ? 3 : 1), "actual partial " + (delivery ? "delivery" : "pickup"));
            Policy(c, g, "machine", false); var retained = Job(g, haul); var handling = retained.HandlingProgress;
            Fix cargo = Cargo(g, independent), position = At(g, Entity(g, "machine")), prefix = Facts(g, Entity(g, "destination")).Stock,
                source = Facts(g, Entity(g, "source")).Stock;
            c.That(cargo.IsPositive && cargo == retained.Loaded && handling.WholeTicks > 0, "actual partial inventory and earned handling, not cargo shadow");
            twin = Save(c, f, g); for (int i = 0; i < 3; i++) Step(g, twin);
            c.That(Job(g, haul).State == JobState.Paused && Job(g, haul).HandlingProgress.Equals(handling) &&
                Cargo(g, independent) == cargo && At(g, Entity(g, "machine")) == position, "paused partial transfer retains handling/stock/physical pose");
            var command = new SetWorkerHeldCommand { Worker = Entity(g, "machine"), Held = true };
            c.Equal(Code.None, g.Seat(0).Preview(command), "ordinary held interruption preview"); int sequence = g.Seat(0).Submit(command);
            twin = Save(c, f, g); g.Clock.Pause(); twin.Clock.Pause(); ulong paused = g.Hash(); long time = g.Clock.StepIndex; Step(g, twin);
            c.That(g.Hash() == paused && g.Clock.StepIndex == time && Jobs(g).Any(x => x.Id == haul && Live(x)), "saved cancellation inbox waits under sole paused clock");
            g.Clock.Resume(); twin.Clock.Resume(); Step(g, twin);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "saved actual partial held input accepted");
            c.That(!Jobs(g).Any(x => x.Id == haul && Live(x)) && Cargo(g, independent) == cargo &&
                At(g, Entity(g, "machine")) == position, "cancelled commitment does not destroy or teleport actual cargo");
            c.That(Facts(g, Entity(g, "source")).Stock == source && Facts(g, Entity(g, "destination")).Stock == prefix,
                "cancellation retains already committed pickup/delivery prefix");
            c.That(Facts(g, Entity(g, "source")).OutboundPromised.IsZero && Facts(g, Entity(g, "destination")).InboundPromised.IsZero,
                "old exact promises release without changing actual stock");
            Band(c, g, prefix + cargo, true); Policy(c, g, "machine", true);
            Submit(c, g, new SetWorkerHeldCommand { Worker = Entity(g, "machine"), Held = false }); twin = Save(c, f, g);
            Reach(g, twin, () => Jobs(g).Any(x => Live(x) && x.RecoveryKind == LogisticsRecoveryKind.CargoReturn),
                "actual residual recovery reselection independent=" + independent + " delivery=" + delivery + " expected=" + (prefix + cargo));
            var recovery = Jobs(g).Single(x => Live(x) && x.RecoveryKind == LogisticsRecoveryKind.CargoReturn);
            c.That(recovery.Id != haul && recovery.Amount == cargo && recovery.Loaded == cargo, "new original recovery protects only real residual");
            twin = Save(c, f, g); Reach(g, twin, () => Cargo(g, independent).IsZero && !Jobs(g).Any(x => x.IsHaul && Live(x)), "physical once-only residual delivery");
            c.Equal(prefix + cargo, Facts(g, Entity(g, "destination")).Stock, "exact prefix plus real residual delivered once");
            c.Equal(source, Facts(g, Entity(g, "source")).Stock, "recovery never loads cancelled unpicked promise");
            c.That(Facts(g, Entity(g, "destination")).InboundPromised.IsZero && Total(g) == Fix.FromInt(12), "exact receipt release and conservation");
            c.That(retained.Loaded == cargo && retained.HandlingProgress.Equals(handling), "retained partial-transfer facts immutable");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current literal work-lifecycle build");
            foreach (bool half in new[] { false, true }) foreach (bool independent in new[] { false, true })
                foreach (bool cancel in new[] { false, true }) Scenario(c, half, independent, cancel);
        }
    }
}
