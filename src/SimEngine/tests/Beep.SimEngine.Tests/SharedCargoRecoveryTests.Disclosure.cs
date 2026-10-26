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
    internal static partial class SharedCargoRecoveryTests
    {
        internal const string DisclosureCase = "Shared cargo disclosure: original and later discarded service candidates protect aggregate recovery without changing physical handoff";
        internal static void RegisterDisclosure() => TestRunner.Add(DisclosureCase, Disclosure);
        private static void Disclosure(Check c)
        {
            foreach (bool originalPending in new[] { true, false })
            {
                var factory = Factory("disclosure", author: content => {
                    var pending = Json.Parse(FrameworkProofContent.Read("pending-carrier-service.json"));
                    for (int i = 0; i < pending.Get("activities").Count; i++) content.Get("activities").Add(pending.Get("activities")[i].Clone());
                    content.Set("haul_trip_profiles", pending.Get("haul_trip_profiles").Clone());
                    AutomaticServiceDisclosureTests.AuthorPendingService(content.Get("entities")[0].Get("components"));
                }, worldAuthor: root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                    root.Get("scenarios")[0].Get("settings").Set(InventorySightModule.ModuleId,
                        Json.Parse("{\"target_kind\":\"activity\",\"target\":\"trip.pending-service\",\"initial_hidden\":false}"));
                }, extraModules: new GameModule[] { new InventorySightModule() },
                    extraDeclarations: Json.Parse("[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]"));
                var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
                var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = game.RecordReplay(limits);
                var fleet = game.Seat(0).Snapshot.Entities.Where(row => row.Def == "cargo.carrier").OrderBy(row => row.Id.Value).ToArray();
                if (fleet.Length != 2) throw new InvalidOperationException("two actual shared cargo carriers required");
                var first = fleet[0].Id; var peer = fleet[1].Id; var selected = originalPending ? peer : first; var waiting = originalPending ? first : peer;
                var source = TransformationContentContractTests.Entity(game, "source"); var target = TransformationContentContractTests.Entity(game, "destination");
                var container = TransformationContentContractTests.Entity(game, "cargo.container"); var supply = PermanentRemovalTests.Supply(game);
                PermanentRemovalTests.Submit(c, game, new SetWorkerHeldCommand { Worker = TransformationContentContractTests.Entity(game, "machine"), Held = true });
                PermanentRemovalTests.Submit(c, game, new SetWorkerHeldCommand { Worker = peer, Held = true });
                var pickup = PermanentRemovalTests.Submit(c, game, new HaulResourcesCommand { Source = source, Destination = target, Worker = first,
                    Resource = supply, Ceiling = Fix.FromInt(8), Priority = 100 });
                int updates = 0; bool partial = false; SimGame twin = null; var watch = Stopwatch.StartNew();
                while (updates < 160 && watch.ElapsedMilliseconds < 25000)
                {
                    Fix cargo = LoadPolicyDisclosureTests.Stock(game, container, supply);
                    c.Equal(Fix.FromInt(20), LoadPolicyDisclosureTests.Stock(game, source, supply) + LoadPolicyDisclosureTests.Stock(game, target, supply) + cargo,
                        "initial physical pickup conserves one actual shared buffer");
                    if (!partial && cargo.IsPositive && cargo < Fix.FromInt(8))
                    { partial = true; twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual partial shared-buffer pickup checkpoints"); }
                    if (PermanentRemovalTests.Jobs(game).Any(row => row.Id == pickup.Arg0 && row.Loaded == Fix.FromInt(8))) break;
                    PermanentRemovalTests.Step(game, twin); updates++;
                }
                c.That(partial && updates < 160 && watch.ElapsedMilliseconds < 25000, "original carrier really loads all eight units over bounded game time");
                PermanentRemovalTests.Submit(c, game, new SetWorkerHeldCommand { Worker = first, Held = true });
                c.That(LoadPolicyDisclosureTests.Stock(game, container, supply) == Fix.FromInt(8) && LoadPolicyDisclosureTests.Stock(game, source, supply) == Fix.FromInt(12),
                    "holding releases execution, not physically retained shared cargo");
                PermanentRemovalTests.Submit(c, game, new ConfigureRecurringPlanCommand { Entity = waiting, Source = new Code("service.pending"),
                    Settings = new RecurringPlanSettings(true, 60, GameDuration.Minutes(60), Code.None) });
                c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().ReadRecurringPlan(waiting, new Code("service.pending"), out var pendingSource), "losing source is known before hiding");
                c.That(pendingSource.Schedule.LastPlan == 0 && pendingSource.RequestBlocker == RecurringPlanCodes.NoTarget, "missing home is a typed pending service requirement, never a default");
                var before = new List<LogisticsDispatchOffer>(); game.Observer.Feature<LogisticsView>().CopyQueue(before);
                var retained = before.Single(row => row.Kind == LogisticsDispatchKind.CargoReturn && row.Resource == supply);
                c.That(retained.SourceEndpoint == ResourceEndpointId.ForEntity(container) && retained.Requested == Fix.FromInt(8), "one original recovery retains actual shared endpoint and age");
                var visible = new List<int>(); var hidden = new List<int>();
                foreach (var worker in new[] { first, peer }) game.Seat(0).Submit(new SetWorkerHeldCommand { Worker = worker, Held = false });
                foreach (bool diagnostics in new[] { false, true }) visible.Add(game.Seat(0).Submit(new ProbeAutomaticServiceDisclosureCommand {
                    Kind = LogisticsDispatchKind.CargoReturn, Worker = selected, WaitingWorker = waiting, Source = retained.RecoveryOrigin, Destination = target, Diagnostics = diagnostics }));
                game.Seat(0).Submit(new SetInventorySightCommand { Hidden = true });
                foreach (bool diagnostics in new[] { false, true }) hidden.Add(game.Seat(0).Submit(new ProbeAutomaticServiceDisclosureCommand {
                    Kind = LogisticsDispatchKind.CargoReturn, Worker = selected, WaitingWorker = waiting, Source = retained.RecoveryOrigin, Destination = target, Diagnostics = diagnostics }));
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual shared cargo and ordered release/query/visibility inbox saves");
                PermanentRemovalTests.Step(game, twin);
                foreach (var current in new[] { game, twin })
                {
                    long cursor = 0; var results = new List<CommandResult>(); current.Seat(0).ReadResults(ref cursor, results);
                    foreach (int sequence in visible) c.That(results.Single(row => row.Sequence == sequence).Accepted, "actual selected recovery and whole diagnosis are visible before hiding");
                    foreach (int sequence in hidden) c.That(!results.Single(row => row.Sequence == sequence).Accepted
                        && results.Single(row => row.Sequence == sequence).Refusal == Codes.NotFound, "discarded source dependency withholds aggregate recovery and diagnosis on saved originals");
                    var jobs = PermanentRemovalTests.Jobs(current).Where(row => row.IsHaul).ToArray();
                    c.That(jobs.Length == 1 && jobs[0].Worker == selected && jobs[0].Loaded == Fix.FromInt(8), "native recovery still assigns one eligible carrier with actual retained cargo");
                    var actual = new List<LogisticsDispatchOffer>(); current.Observer.Feature<LogisticsView>().CopyQueue(actual);
                    var row = actual.Single(item => item.Kind == LogisticsDispatchKind.CargoReturn && item.Resource == supply);
                    c.That(row.CreatedTick == retained.CreatedTick && row.RecoveryOrigin == retained.RecoveryOrigin && row.SourceEndpoint == retained.SourceEndpoint,
                        "carrier ranking does not rewrite recovery origin, endpoint or starvation age");
                }
                updates = 0; watch.Restart();
                while (updates < 160 && watch.ElapsedMilliseconds < 25000)
                {
                    Fix cargo = LoadPolicyDisclosureTests.Stock(game, container, supply), delivered = LoadPolicyDisclosureTests.Stock(game, target, supply);
                    c.Equal(Fix.FromInt(20), LoadPolicyDisclosureTests.Stock(game, source, supply) + cargo + delivered, "physical handoff counts one buffer, never one copy per alias");
                    c.Equal(Fix.FromInt(12), LoadPolicyDisclosureTests.Stock(game, source, supply), "retained recovery does not perform another pickup");
                    if (cargo.IsZero && delivered == Fix.FromInt(8) && !PermanentRemovalTests.Jobs(game).Any(row => row.IsHaul)) break;
                    PermanentRemovalTests.Step(game, twin); updates++;
                }
                c.That(updates < 160 && watch.ElapsedMilliseconds < 25000, "selected recovery physically moves and unloads all retained material");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled original age, service policy and shared-buffer recovery restore");
                c.Equal(isolated, idle.Hash(), "interleaved shared cargo session remains isolated");
                var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
                c.That(verified.Success && verified.ActualHash == game.Hash(), "actual pickup hold pending service hide and recovery handoff replay");
                Console.WriteLine("shared-service/" + (originalPending ? "original" : "later") + "/0->" + game.Clock.StepIndex
                    + "/inputs=" + recording.CommandCount + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
            }
        }
    }
}
