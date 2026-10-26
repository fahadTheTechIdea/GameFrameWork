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
    // Authored JSON/master + ordinary public player inputs; no direct placement, job, stock or queue writes.
    internal static class DockQueuePromotionTests
    {
        internal const string Case = "Dock queue promotion: one cost-aware selector preserves physical waiting and shared eligible precedence";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, string mode, bool shuffled)
        {
            var root = Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var entities = JsonValue.NewArray(); entities.Add(root.Get("entities")[0]); entities.Add(root.Get("entities")[2]); root.Set("entities", entities);
            if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            root.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("promotion.start"));
            var document = Json.Parse(FrameworkProofContent.Read("dock-queue-promotion.json"));
            var slots = document.Get("entities")[0].Get("components").Get("point_dock").Get("slots");
            void Place(string id, int point) {
                var definitions = document.Get("entities"); for (int i = 0; i < definitions.Count; i++) if (definitions[i].Get("id").Text == id) definitions[i].Get("components").Get("point_presence").Set("position", JsonValue.Number(point.ToString()));
                var setups = document.Get("entity_setups"); for (int i = 0; i < setups.Count; i++) if (setups[i].Get("id").Text == id) setups[i].Get("placement").Set("position", JsonValue.Number(point.ToString()));
            }
            if (mode == "tie") {
                root.Get("entity_setups")[2].Get("placement").Set("position", JsonValue.Number("10"));
                slots[0].Set("offset", JsonValue.Number("2")); slots[1].Set("offset", JsonValue.Number("-2")); slots[2].Set("offset", JsonValue.Number("0"));
                Place("promotion.blocker.near", 8); Place("promotion.blocker.far", 12); Place("promotion.near-home", 2); Place("promotion.far-home", 18);
            }
            if (mode == "fifo") {
                root.Get("entity_setups")[2].Get("placement").Set("position", JsonValue.Number("5"));
                var one = JsonValue.NewArray(); for (int i = 1; i < slots.Count; i++) one.Add(slots[i]); slots = one;
                document.Get("entities")[0].Get("components").Get("point_dock").Set("slots", slots);
                document.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("promotion.younger"));
                document.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("promotion.alias"));
            }
            if (shuffled) { var reverse = JsonValue.NewArray(); for (int i = slots.Count - 1; i >= 0; i--) reverse.Add(slots[i]); document.Get("entities")[0].Get("components").Get("point_dock").Set("slots", reverse); }
            return PointGame.Factory(definitions: root.ToString(), additionalDocuments: new Dictionary<string, string> { { "dock-queue-promotion.json", document.ToString() } });
        }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest { Scenario = "points", Seed = 3661, Seats = { Controller.Human, Controller.Human } });
        private static WorkView Work(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<WorkView>();
        private static List<WorkJobView> Jobs(SimGame g, int seat = 0) { var rows = new List<WorkJobView>(); Work(g, seat).CopyJobs(rows); return rows; }
        private static WorkJobView Job(SimGame g, int id, int seat = 0) => Jobs(g, seat).Single(x => x.Id == id);
        private static Fix Position(SimGame g, string entity) => ((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(x => x.Id == Entity(g, entity)).Spatial).Position;
        private static void Until(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            var trail = new Queue<string>();
            for (int i = 0; i < 400 && !done(); i++) {
                trail.Enqueue(g.Clock.StepIndex + ":" + string.Join(";", Jobs(g).Concat(Jobs(g, 1)).Select(x => x.Id + "/" + x.State + "/" + x.ToilIndex + "/" + x.Blocker + "/" + x.DestinationDockSlot)));
                if (trail.Count > 8) trail.Dequeue(); Step(g, twin);
            }
            if (!done()) throw new InvalidOperationException(stage + " exceeds400Updates " + string.Join(" | ", trail));
        }
        private static long Input(Check c, SimGame g, SimGame twin, int seat, Func<SimGame, Command[]> make)
        {
            long result = 0;
            var sequence = new List<(SimGame Game, List<int> Sequences)>();
            foreach (var current in twin == null ? new[] { g } : new[] { g, twin }) {
                var ids = new List<int>(); ulong pure = current.Hash();
                foreach (var command in make(current)) { c.Equal(Code.None, current.Seat(seat).Preview(command), "ordinary player intent ready"); c.Equal(pure, current.Hash(), "queued command preview is pure"); ids.Add(current.Seat(seat).Submit(command)); pure = current.Hash(); }
                sequence.Add((current, ids));
            }
            Step(g, twin);
            foreach (var entry in sequence) {
                long cursor = 0; var results = new List<CommandResult>(); entry.Game.Seat(seat).ReadResults(ref cursor, results);
                foreach (int id in entry.Sequences) { var accepted = results.Single(x => x.Sequence == id); c.That(accepted.Accepted, "ordinary player intent accepted " + accepted.Refusal); if (!accepted.Accepted) throw new InvalidOperationException("promotion input " + accepted.Refusal); if (ReferenceEquals(entry.Game, g)) result = accepted.Arg0; }
            }
            return result;
        }
        private static SubmitPlanCommand Inspect(SimGame g, string target, string worker, int priority)
        {
            var args = JsonValue.NewObject(); args.Set("target", JsonValue.Number(Entity(g, target).Value.ToString()));
            var bindings = JsonValue.NewArray(); var binding = JsonValue.NewObject();
            binding.Set("node", JsonValue.Of("check")); binding.Set("role", JsonValue.Of("inspect")); binding.Set("worker", JsonValue.Number(Entity(g, worker).Value.ToString())); bindings.Add(binding); args.Set("workers", bindings);
            return new SubmitPlanCommand { Intent = new PlanIntent("activity:inspect", args, null, priority, false) };
        }
        private static int WorkJob(SimGame g, string worker, int seat = 0) => Jobs(g, seat).Single(x => x.Worker == Entity(g, worker)).Id;
        private static int Order(SimGame g, long plan, int seat = 0) { var rows = new List<PlanView>(); Work(g, seat).CopyPlans(rows); return rows.Single(x => x.Id == plan).Order; }
        private static ActivityNodeView Node(SimGame g, int order, int seat = 0) { var rows = new List<ActivityNodeView>(); if (!Work(g, seat).TryCopyNodes(order, rows)) throw new InvalidOperationException("missing owned physical activity"); return rows.Single(x => x.Id == "check"); }
        private static long Scenario(Check c, bool half, string mode, bool shuffled)
        {
            var f = Factory(half, mode, shuffled); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recorder = g.RecordReplay(limits);
            int id, younger = 0; bool haul = mode == "haul"; int order = 0, youngOrder = 0;
            if (haul) {
                id = checked((int)Input(c, g, null, 0, game => new Command[] { new HaulResourcesCommand { Source = Entity(game, "source"), Destination = Entity(game, "destination"),
                    Worker = Entity(game, "machine"), Resource = PermanentRemovalTests.Supply(game), Ceiling = Fix.FromInt(8), Priority = 100 } }));
            } else {
                long plan = Input(c, g, null, 0, game => new Command[] { Inspect(game, "destination", "machine", 100) }); id = WorkJob(g, "machine"); order = Order(g, plan);
            }
            if (mode == "fifo") {
                long plan = Input(c, g, null, 1, game => new Command[] { Inspect(game, "promotion.alias", "promotion.younger", 999) }); younger = WorkJob(g, "promotion.younger", 1); youngOrder = Order(g, plan, 1);
            }
            int youngId = younger; Fix waiting = Fix.FromInt(mode == "tie" ? 10 : 5);
            Until(g, null, () => Job(g, id).DestinationDockSlot == new Code("waiting") && Position(g, "machine") == waiting &&
                (youngId == 0 || Job(g, youngId, 1).DestinationDockSlot == new Code("alias.wait") && Position(g, "promotion.younger") == Fix.FromInt(4)), "physical arrived waiting");
            c.Equal(Fix.Zero, Stock(g, "destination"), "occupied bays do not permit phantom service");
            if (!haul) c.Equal(WorkProgress.Zero, Node(g, order).Progress, "waiting earns no inspection work");
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "arrived physical waiting claims restore");
            g.Clock.Pause(); twin.Clock.Pause(); ulong paused = g.Hash(); long clock = g.Clock.StepIndex; Step(g, twin);
            c.Equal(clock, g.Clock.StepIndex, "sole paused clock retains physical queue"); c.Equal(paused, g.Hash(), "paused queue and cargo/earned work remain unchanged");
            g.Clock.Resume(); twin.Clock.Resume();
            Input(c, g, twin, 0, game => new Command[] {
                new SetHomeCommand { Worker = Entity(game, "promotion.blocker.near"), Home = Entity(game, "promotion.near-home") },
                new SetHomeCommand { Worker = Entity(game, "promotion.blocker.far"), Home = Entity(game, "promotion.far-home") } });
            Until(g, twin, () => Job(g, id).DestinationDockSlot != new Code("waiting"), "fresh cost-aware physical promotion");
            Code expected = new Code(mode == "tie" ? "a.far-bay" : "z.near-bay");
            c.Equal(expected, Job(g, id).DestinationDockSlot, mode == "tie" ? "equal actual waiting-origin cost uses stable identity" : "nearest vacant service wins over alphabetically first far bay");
            c.That(Position(g, "promotion.blocker.near") != Fix.FromInt(mode == "tie" ? 8 : 11), "real home movement frees near bay");
            c.That(Position(g, "promotion.blocker.far") != Fix.FromInt(mode == "tie" ? 12 : 14), "real home movement frees far bay");
            if (mode == "fifo") {
                c.Equal(new Code("alias.wait"), Job(g, younger, 1).DestinationDockSlot, "higher-priority younger facility cannot steal older eligible shared physical bay");
                c.Equal(WorkProgress.Zero, Node(g, youngOrder, 1).Progress, "younger shared waiter earns no overlapping service");
                var foreign = new List<WorkJobView>(); Work(g, 1).CopyJobs(foreign); c.That(foreign.All(x => x.Id != id), "other-company queue intent is private");
            }
            twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "promoted original physical claim restores");
            Step(g, twin); c.Equal(expected, Job(g, id).DestinationDockSlot, "saved valid promotion is retained through physical travel");
            if (mode == "fifo") {
                Until(g, twin, () => !Jobs(g).Any(x => x.Id == id), "older physical work and departure");
                Until(g, twin, () => !Jobs(g, 1).Any(x => x.Id == youngId), "younger promotion, physical work and separate departure");
                c.That(Work(g).TryReadActivityCompletion("activity:inspect", Entity(g, "destination"), out _) &&
                    Work(g, 1).TryReadActivityCompletion("activity:inspect", Entity(g, "promotion.alias"), out _), "both original activities earn immutable completion after real shared service");
            } else Until(g, twin, () => !Jobs(g).Any(x => x.Id == id), "promoted physical work or hauling and departure");
            c.Equal(Fix.FromInt(haul ? 12 : 20), Stock(g, "source"), "original finite source accounting");
            c.Equal(Fix.FromInt(haul ? 8 : 0), Stock(g, "destination"), "original timed service accounting");
            c.Equal(Fix.Zero, Stock(g, "machine"), "physical cargo settles");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled physical promotions restore");
            c.Equal(isolated, idle.Hash(), "interleaved session remains unchanged");
            c.That(new ReplayVerifier().Verify(f, recorder.Finish(), limits).Success, "normal public inputs replay queue promotion and real home/work motion");
            return g.Clock.StepIndex;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual current one-selector promotion identity");
            foreach (bool half in new[] { false, true }) foreach (string mode in new[] { "haul", "tie", "fifo" })
                c.Equal(Scenario(c, half, mode, false), Scenario(c, half, mode, true), "shuffled authored slots retain deterministic physical promotion chronology");
        }
    }
}

