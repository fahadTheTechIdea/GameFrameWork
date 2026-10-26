using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Ordinary player inputs/public views only. Read-only framework probes run at the real command boundary.
    internal static class CargoReturnDisclosureTests
    {
        internal const string Case = "Cargo return disclosure: hidden destinations and request owners withhold ready recovery decisions";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool activity)
        {
            var work = Json.Parse(FrameworkProofContent.Read("cargo-demand-lifecycle.json"));
            work.Get("target_resource_requirements")[0].Get("quantities").Set("supply", JsonValue.Number("4"));
            var recovery = Json.Parse(FrameworkProofContent.Read("logistics-recovery.json"));
            recovery.Get("recovery_destination_policies")[0].Set("mode", JsonValue.Of("demand_only"));
            return ConstructionPlanTests.Points(extraModules: new GameModule[] { new InventorySightModule(), new LogisticsRecoveryPolicyModule() },
                extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]},"
                    + "{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["logistics-recovery.json"] = recovery.ToString() },
                additionalDocuments: new Dictionary<string, string> { ["cargo-demand-lifecycle.json"] = work.ToString() },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                    scenario.Get("settings").Set(InventorySightModule.ModuleId, Json.Parse(activity
                        ? "{\"target_kind\":\"activity\",\"target\":\"cargo.request\",\"initial_hidden\":false}"
                        : "{\"target_kind\":\"entity\",\"target\":\"destination\",\"initial_hidden\":false}"));
                    scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"recovery.high\"}"));
                }, worldEdit: root => {
                    root.Get("entities")[1].Get("components").Get("point_presence").Set("position", JsonValue.Number("50"));
                    root.Get("entity_setups")[1].Get("placement").Set("position", JsonValue.Number("50"));
                });
        }
        private static Fix Stock(SimGame game, EntityId site, ResourceId resource)
        {
            Code refusal = game.Observer.Feature<LogisticsView>().ReadDiagnostics(site, resource, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual cargo disclosure stock/" + refusal); return value.Stock;
        }
        private static CommandResult Result(SimGame game, int sequence)
        {
            long cursor = 0; var rows = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, rows);
            return rows.Single(row => row.Sequence == sequence);
        }
        private static void Healthy(Check c, bool activity)
        {
            var factory = Factory(activity); var game = ConstructionPlanTests.NewPoints(factory); var idle = ConstructionPlanTests.NewPoints(factory);
            ulong isolated = idle.Hash(); c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical recovery and work require no business module");
            EntityId worker = TransformationContentContractTests.Entity(game, "machine"), source = TransformationContentContractTests.Entity(game, "source"),
                target = TransformationContentContractTests.Entity(game, "destination");
            var resource = PermanentRemovalTests.Supply(game); var ids = new[] { source, target, worker };
            var timer = Stopwatch.StartNew(); long began = game.Clock.StepIndex;
            void Reach(SimGame twin, Func<bool> done, string stage, bool conserving = true)
            {
                while (!done() && game.Clock.StepIndex - began < 256 && timer.Elapsed.TotalSeconds < 25)
                {
                    PermanentRemovalTests.Step(game, twin);
                    if (conserving) c.Equal(Fix.FromInt(20), ids.Aggregate(Fix.Zero, (total, id) => total + Stock(game, id, resource)),
                        "finite stock conserved during " + stage);
                    else c.Equal(Fix.FromInt(16), Stock(game, source, resource), "recovery never reloads the original finite supply");
                }
                if (!done()) throw new InvalidOperationException(stage + "/256updates25seconds/" + game.Clock.StepIndex + "/"
                    + string.Join(";", PermanentRemovalTests.Jobs(game).Select(job => job.Id + "/" + job.State + "/" + job.Blocker)));
            }
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, game, new ConfigureWorkPolicyCommand { Worker = worker,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            PermanentRemovalTests.Submit(c, game, new HaulResourcesCommand { Source = source, Destination = target, Worker = worker,
                Resource = resource, Ceiling = Fix.FromInt(4), Priority = 100 });
            Reach(null, () => Stock(game, worker, resource) == Fix.FromInt(4) && PermanentRemovalTests.At(game, worker) > Fix.One, "actual loaded travel");
            PermanentRemovalTests.Submit(c, game, new SetWorkerHeldCommand { Worker = worker, Held = true });
            int order = checked((int)PermanentRemovalTests.Submit(c, game, new PostOrderCommand { Plan = "cargo.request", Target = target }).Arg0);
            var visible = new List<int>(); var hidden = new List<int>();
            game.Seat(0).Submit(new SetWorkerHeldCommand { Worker = worker, Held = false });
            foreach (bool diagnostics in new[] { false, true }) visible.Add(game.Seat(0).Submit(new ProbeReadyCargoDisclosureCommand {
                Worker = worker, Destination = target, Request = order, Diagnostics = diagnostics }));
            game.Seat(0).Submit(new SetInventorySightCommand { Hidden = true });
            foreach (bool diagnostics in new[] { false, true }) hidden.Add(game.Seat(0).Submit(new ProbeReadyCargoDisclosureCommand {
                Worker = worker, Destination = target, Request = order, Diagnostics = diagnostics }));
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "real held cargo and pending ordered queries restore exactly");
            // Release -> visible reads -> hide -> private reads all precede the native assignment pass.
            // The selected diagnostic command reads actual engine views and has no Apply gameplay/cache.
            PermanentRemovalTests.Step(game, twin);
            foreach (var current in new[] { game, twin })
            {
                foreach (int sequence in visible) c.That(Result(current, sequence).Accepted, "actual ready visible recovery and diagnosis admitted");
                foreach (int sequence in hidden) c.That(!Result(current, sequence).Accepted && Result(current, sequence).Refusal == Codes.NotFound,
                    "actual ready hidden destination/request refuses without a result prefix");
                c.That(current.Seat(0).View.TryGetEntity(worker, out _) && current.Seat(0).View.TryFindResource("supply", out _),
                    "visible actual vehicle/resource cannot authorize hidden recovery references");
                c.Equal(activity, current.Seat(0).View.TryGetEntity(target, out _), "only destination mode hides the actual target entity");
                c.Equal(Code.None, current.Seat(0).View.Feature<LogisticsView>().ReadCargoDisposition(worker, out var partition),
                    "unrelated actual own cargo partition remains visible");
                c.Equal(Fix.FromInt(4), partition.Committed.AmountOf(resource), "physical recovery owns the same four-unit outbound cargo claim");
                var jobs = new List<WorkJobView>(); current.Observer.Feature<WorkView>().CopyJobs(jobs);
                c.That(jobs.Any(job => job.IsHaul && job.Worker == worker && job.Destination == target && job.Loaded == Fix.FromInt(4)
                    && job.RecoveryKind == LogisticsRecoveryKind.CargoReturn && job.HaulDemand.Scope == WorkModule.ModuleId
                    && job.HaulDemand.Identity == order), "privacy refusal does not prevent actual request-backed cargo recovery acquisition");
                c.That(Stock(current, source, resource) == Fix.FromInt(16) && Stock(current, target, resource) == Fix.Zero,
                    "new recovery has no instant reload or delivery");
            }
            c.Equal(game.Hash(), twin.Hash(), "saved pending private queries and original recovery acquire identically");
            var acquired = factory.Load(game.Save()); c.Equal(game.Hash(), acquired.Hash(), "private acquired physical recovery checkpoint restores exactly");
            game.Clock.Pause(); twin.Clock.Pause(); long stopped = game.Clock.StepIndex; PermanentRemovalTests.Step(game, twin);
            c.Equal(stopped, game.Clock.StepIndex, "paused native recovery and cargo stay on the sole clock");
            c.Equal(Fix.FromInt(4), Stock(game, worker, resource), "paused visibility neither destroys nor teleports cargo");
            game.Clock.Resume(); twin.Clock.Resume();
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = false });
            bool Done() => game.Observer.Feature<WorkView>().TryReadCompanyActivityCompletion(new PlayerId(0), "activity:cargo.request", target, out _);
            Reach(twin, Done, "actual physical recovery and supplied work", false);
            c.That(Stock(game, worker, resource) == Fix.Zero && Stock(game, target, resource) == Fix.Zero
                && Stock(game, source, resource) == Fix.FromInt(16), "original four units travel, deliver and fund the authored work bill once");
            c.Equal(game.Hash(), twin.Hash(), "saved private recovery and subsequent physical work converge");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed physical work checkpoint restores exactly");
            c.Equal(isolated, idle.Hash(), "interleaved session visibility and decisions remain isolated");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "full recording includes every advancing update");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "complete cargo/request/visibility/physical-work replay");
            Console.WriteLine("cargo-return/" + (activity ? "request" : "destination") + "/full/" + recording.InitialStep + "->" + game.Clock.StepIndex
                + "/inputs=" + recording.CommandCount + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
        }
        private static void Run(Check c) { Healthy(c, false); Healthy(c, true); }
    }
}
