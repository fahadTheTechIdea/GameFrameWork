using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class CapacityLogisticsStressTests
    {
        private const string DockNativeCase = "Dock contention: native buffers repeated physical queue bursts preserve saved resources and allocation budgets";
        private const string DockIndependentCase = "Dock contention: independent buffers half-minute physical queue bursts preserve saved resources and allocation budgets";
        private static void DockContention(Check c, bool native, bool half)
        {
            var workload = Json.Parse(FrameworkProofContent.Read("dock-contention.json"));
            var profile = new Profile(); int carriers = int.Parse(workload.Get("carriers").Text, System.Globalization.CultureInfo.InvariantCulture);
            string destination = workload.Get("destination").Text;
            var factory = Factory(profile, carriers, native, half, configure: root => {
                var entities = root.Get("entities");
                for (int i = 0; i < entities.Count; i++)
                    if (entities[i].Get("id").Text == "source" || entities[i].Get("id").Text == destination)
                        entities[i].Get("components").Set("tile_work_dock", workload.Get("dock").Clone());
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
            });
            var game = New(factory); var idle = New(factory); ulong untouched = idle.Hash(); SimGame twin = null;
            var source = Entity(game, "source"); var target = Entity(game, destination);
            var workers = Enumerable.Range(0, carriers).Select(i => Entity(game, "stress.carrier." + i)).ToArray();
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("authored dock resource missing");
            Fix opening = Fix.FromInt(checked(carriers * profile.Rounds * profile.Quantity));
            long peak = 0, allocated = 0; int updates = 0; bool waiting = false, loaded = false, savedQueue = false;
            var identities = new HashSet<int>(); var trace = new Queue<string>();
            var limits = new ReplayLimits(32 * 1024 * 1024, profile.MaxUpdates * profile.Rounds);
            using var recording = game.RecordReplay(limits);
            Fix Stock(EntityId entity) {
                Code refusal = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity, resource, out var value);
                if (!refusal.IsNone) throw new InvalidOperationException("dock stock diagnostics: " + refusal);
                return value.Stock;
            }
            void Observe() {
                Accounting(c, game, opening, resource);
                var jobs = Jobs(game).Where(row => row.State == JobState.Assigned || row.State == JobState.Paused).ToArray();
                c.That(jobs.Count(row => row.Source == source && row.SourceDockSlot == new Code("bay")) <= 1,
                    "one actual source bay has at most one service claim");
                c.That(jobs.Count(row => row.Destination == target && row.DestinationDockSlot == new Code("bay")) <= 1,
                    "one actual destination bay has at most one service claim");
                foreach (var bay in new[] { new TileCoord(7, 16), new TileCoord(27, 16) }) {
                    int occupants = game.Seat(0).Snapshot.Entities.Count(entity => workers.Contains(entity.Id) &&
                        entity.Spatial is TileSpatialSnapshot at && at.Footprint.Contains(bay));
                    c.That(occupants <= 1, "shared service geometry never overlaps physical carriers");
                }
                foreach (var job in jobs) {
                    waiting |= job.DockWaiting;
                    loaded |= job.Loaded.IsPositive;
                    if (!job.SourceDockSlot.IsNone && job.SourceDockSlot.Id.StartsWith("wait.", StringComparison.Ordinal) && job.ToilIndex <= 1)
                        c.That(job.Loaded == Fix.Zero && job.HandlingProgress.WholeTicks == 0, "waiting pickup has no premature loading or handling time");
                }
                var summary = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
                c.That(summary.Status == ResourceFlowReadStatus.Ready && summary.Aggregates.All(row =>
                    row.Kind == ResourceFlowKind.Initialized || row.Kind == ResourceFlowKind.StockTransfer),
                    "repeated finite hauling creates no produced, purchased, consumed, destroyed or lost material");
                if (twin != null) c.Equal(game.Hash(), twin.Hash(), "queued and loaded saved twins preserve claims, geometry and accounting each update");
                trace.Enqueue(game.Clock.StepIndex + ":" + string.Join(";", jobs.Select(row => row.Id + "/" + row.State + "/" + row.ToilIndex + "/" + row.Loaded + "/" + row.Blocker + "/" + row.SourceDockSlot + "/" + row.DestinationDockSlot)));
                if (trace.Count > 8) trace.Dequeue();
            }
            void Step() {
                long before = GC.GetAllocatedBytesForCurrentThread(); game.Update(); long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                peak = Math.Max(peak, bytes); allocated = checked(allocated + bytes); updates++;
                twin?.Update();
                if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("dock contention fault: " + game.Fault + "/" + twin?.Fault);
                Observe();
            }
            HaulResourcesCommand Intent(int index) => new HaulResourcesCommand { Source = source, Destination = target,
                Worker = workers[index], Resource = resource, Ceiling = Fix.FromInt(profile.Quantity), Priority = 10 };
            c.That(!game.Modules.Contains("sim.tycoon"), "finite dock contention needs no business module");
            for (int round = 0; round < profile.Rounds; round++) {
                var accepted = new List<int>(); var competing = new List<int>(); long at = game.Clock.StepIndex;
                foreach (var session in twin == null ? new[] { game } : new[] { game, twin })
                    for (int i = 0; i < carriers; i++) {
                        ulong pure = session.Hash(); c.Equal(Code.None, session.Seat(0).Preview(Intent(i)), "actual carrier and dock preflight accepts first burst intent");
                        c.Equal(pure, session.Hash(), "dock preflight changes no world state");
                        int first = session.Seat(0).Submit(Intent(i)), duplicate = session.Seat(0).Submit(Intent(i));
                        if (ReferenceEquals(session, game)) { accepted.Add(first); competing.Add(duplicate); }
                    }
                c.Equal(at, game.Clock.StepIndex, "queued requests do not advance the one game clock");
                Step(); long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
                foreach (int sequence in accepted) c.That(answers.Single(row => row.Sequence == sequence).Accepted, "first ordered request acquires actual carrier and endpoint claims");
                foreach (int sequence in competing) c.That(!answers.Single(row => row.Sequence == sequence).Accepted, "duplicate carrier request creates no job or extra resource claim");
                var current = Jobs(game); c.Equal(carriers, current.Count, "one new generation owns exactly one job per carrier");
                foreach (var job in current) c.That(identities.Add(job.Id), "repeated bursts keep fresh stable job identities");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "queued dock claims restore without another queue model");
                bool done = false;
                for (int i = 0; i < profile.MaxUpdates; i++) {
                    if (Jobs(game).All(row => row.State == JobState.Done)) { done = true; break; }
                    Step();
                    if (!savedQueue && Jobs(game).Any(row => row.DockWaiting) && Jobs(game).Any(row => row.Loaded.IsPositive)) {
                        twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual loaded and waiting queue state restores together"); savedQueue = true;
                    }
                }
                if (!done) throw new InvalidOperationException("dock round exceeded original" + profile.MaxUpdates + " updates, native=" + native + "/half=" + half + "/round=" + round + "/" + string.Join(" | ", trace));
                c.Equal(Fix.FromInt((round + 1) * carriers * profile.Quantity), Stock(target), "every full batch arrives exactly once before the next burst");
                c.That(Jobs(game).All(row => row.Loaded == Fix.Zero), "completed dock work leaves no stranded cargo");
            }
            c.That(waiting && loaded && savedQueue, "authored workload really contains waiting, loaded movement and a saved queue, not instant service");
            c.Equal(Fix.Zero, Stock(source), "all finite source material is delivered once");
            c.Equal(carriers * profile.Rounds, identities.Count, "all repeated generations are counted even when completed rows retire");
            foreach (var entity in game.Seat(0).Snapshot.Entities) {
                Code refusal = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, resource, out var terminal);
                c.That(refusal.IsNone && terminal.InboundPromised == Fix.Zero && terminal.OutboundPromised == Fix.Zero,
                    "completed finite workload leaves no orphan material promises at any actual endpoint");
            }
            c.That(Jobs(game).All(row => row.SourceDockSlot.IsNone && row.DestinationDockSlot.IsNone),
                "terminal physical departure releases both original dock-slot claims");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed queue/resources restore exactly");
            c.Equal(untouched, idle.Hash(), "another session retains its original private queues and resources");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "actual repeated duplicate/refusal/queue/hauling stream replays");
            c.That(peak <= profile.PerUpdateBudget, "original per-update managed allocation budget");
            c.That(allocated <= profile.TotalBudget, "original total Update managed allocation budget");
            Console.WriteLine("dock contention metrics: native=" + native + ";half=" + half + ";updates=" + updates + ";hauls=" + identities.Count + ";peak-update-bytes=" + peak + ";total-update-bytes=" + allocated);
        }
    }
}
