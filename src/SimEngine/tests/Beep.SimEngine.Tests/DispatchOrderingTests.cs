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
    public static class DispatchOrderingTests
    {
        private const string Case = "Dispatch ordering: authored player priority, starvation and physical saved execution";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(string profile, string profiles = null)
            => DispatchAgingTests.Factory(profile: profile, profiles: profiles, edit: root =>
                root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("20")));
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 297,
            Mods = { "proof.shipments" }, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Entity(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Id;
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static List<WorkerDispatchOffer> Queue(SimGame game)
        { var offers = new List<WorkerDispatchOffer>(); Work(game).CopyAssignments(offers); return offers; }
        private static List<WorkJobView> Jobs(SimGame game)
        { var jobs = new List<WorkJobView>(); Work(game).CopyJobs(jobs); return jobs; }
        private static ActivityNodeView Node(SimGame game, int id)
        {
            var nodes = new List<ActivityNodeView>();
            if (!Work(game).TryCopyNodes(id, nodes)) throw new InvalidOperationException("missing ordering nodes");
            return nodes.Single();
        }
        private static bool Completed(SimGame game, string plan, string target, int id)
            => Work(game).TryReadActivityCompletion("activity:" + plan, Entity(game, target), out var fact) && fact.Order == id;
        private static Fix Stock(SimGame game, string prototype)
        {
            var facets = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, prototype), facets);
            return facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.RunTicks(1); twin?.RunTicks(1);
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("ordering clock=" + game.Tick + "/fault=" + game.Fault + "/twin=" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string phase, SimGame twin = null)
        {
            for (int i = 0; i < 1200 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(phase + "/clock=" + game.Tick + "/jobs=" +
                string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.Worker)));
        }
        private static CommandResult Command(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary priority/work preview");
            c.Equal(hash, game.Hash(), "preview cannot change age/resources/claims");
            var answer = TestKit.Order(game, 0, command); c.That(answer.Accepted, "actual ordinary command answer");
            if (!answer.Accepted) throw new InvalidOperationException("ordering refusal " + answer.Refusal);
            return answer;
        }
        private static ConfigureWorkPolicyCommand Enabled(SimGame game) => new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
            Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) };
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current dispatch semantics identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "derived ordering adds no saved counter/queue");
            foreach (string profile in new[] { "dispatch.priority", "dispatch.fair", "dispatch.mod-priority" })
                Physical(c, profile);
            Emergency(c);
            Contracts(c);
        }
        private static void Physical(Check c, string profile)
        {
            var factory = Factory(profile); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            bool priorityFirst = profile != "dispatch.fair";
            WorkerDispatchOrdering expected = priorityFirst ? WorkerDispatchOrdering.PriorityFirst : WorkerDispatchOrdering.StarvationFirst;
            c.Equal(expected, game.World.Services.Get<IWorkerDispatchPolicy>().Ordering, "actual selected native/public content contract");
            c.That(!game.World.Services.Has<IEconomy>(), "dispatch and physical work need no business");
            using (var recorder = game.RecordReplay(Limits))
            {
                int low = (int)Command(c, game, new PostOrderCommand { Plan = "ordering.low", Target = Entity(game, "source"), Priority = 1 }).Arg0;
                Until(game, () => game.Tick >= 7, "actual pending age");
                int high = (int)Command(c, game, new PostOrderCommand { Plan = "ordering.high", Target = Entity(game, "destination"), Priority = 1000 }).Arg0;
                var offers = Queue(game);
                c.That(offers.Count == 2 && offers.All(o => o.Ordering == expected), "same policy stamped on actual queue facts");
                var old = offers.Single(o => o.Target == Entity(game, "source"));
                var young = offers.Single(o => o.Target == Entity(game, "destination"));
                c.That(old.Starved && !young.Starved, "actual original age crosses authored threshold");
                c.Equal(priorityFirst ? young.Identity : old.Identity, offers[0].Identity, "explicit ordering controls age versus player priority");
                c.Equal(1, old.Priority, "policy never rewrites explicit old priority");
                c.Equal(1000, young.Priority, "policy never clamps explicit new priority");
                c.That(!Node(game, low).Worker.IsValid && !Node(game, high).Worker.IsValid, "disabled worker remains physically unassigned");
                ulong hash = game.Hash(); var hidden = new List<WorkerDispatchOffer>();
                game.Seat(1).View.Feature<WorkView>().CopyAssignments(hidden);
                c.Equal(0, hidden.Count, "foreign company cannot read policy-ranked private work");
                Queue(game); c.Equal(hash, game.Hash(), "rank reads pure and allocate no claims");
                var twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "pending age/policy selected content restores");
                var command = Enabled(game); game.Seat(0).Submit(command); twin.Seat(0).Submit(Enabled(twin)); Step(game, twin);
                int first = priorityFirst ? high : low, second = priorityFirst ? low : high;
                c.Equal(Entity(game, "machine"), Node(game, first).Worker, "actual engine dispatcher follows selected order");
                c.That(!Node(game, second).Worker.IsValid, "waiting order does not borrow worker");
                if (priorityFirst)
                {
                    c.Equal(Fix.FromInt(20), Stock(game, "destination"), "assigned travelling machine spends no remote inputs");
                    c.Equal(0L, Node(game, high).Progress.WholeTicks, "assignment is not productive work or teleport");
                    Until(game, () => Node(game, high).Progress.WholeTicks > 0, "real high-priority travel and work", twin);
                    c.Equal(Fix.FromInt(12), Stock(game, "destination"), "only real arrived work consumes eight inputs");
                    c.Equal(Fix.FromInt(20), Stock(game, "source"), "waiting aged work spends no materials");
                    c.That(((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Entity(game, "machine")).Spatial).Position == Fix.FromInt(10), "actual machine reaches remote site");
                }
                else Until(game, () => Node(game, low).Progress.WholeTicks > 0, "actual oldest-first work", twin);
                twin = factory.Load(game.Save());
                game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long clock = game.Tick;
                game.Update(); twin.Update(); c.Equal(clock, game.Tick, "sole-clock pause earns no work");
                c.Equal(hash, game.Hash(), "paused policy/material/work state stable");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => Completed(game, priorityFirst ? "ordering.high" : "ordering.low", priorityFirst ? "destination" : "source", first), "first ordinary execution completes", twin);
                Until(game, () => Completed(game, priorityFirst ? "ordering.low" : "ordering.high", priorityFirst ? "source" : "destination", second), "second ordinary execution physically completes", twin);
                c.Equal(Fix.FromInt(12), Stock(game, "source"), "source20 minus actual8");
                c.Equal(Fix.FromInt(12), Stock(game, "destination"), "destination20 minus actual8");
                c.Equal(game.Hash(), twin.Hash(), "saved real travel/work/material continuation identical");
                c.Equal(untouched, isolated.Hash(), "selected immutable profile borrows no session state");
                c.Equal(expected, old.Ordering, "held policy facts stay detached");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "same commands/physical order replay: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void Emergency(Check c)
        {
            var factory = Factory("dispatch.priority"); var game = New(factory);
            int ordinary = (int)Command(c, game, new PostOrderCommand { Plan = "ordering.low", Target = Entity(game, "source"), Priority = int.MaxValue }).Arg0;
            Until(game, () => game.Tick >= 7, "ordinary emergency age");
            Command(c, game, new RaiseIncidentCommand { Target = Entity(game, "destination"), Source = new Code("damage"), Kind = new Code("proof.failure") });
            var offers = Queue(game);
            c.That(offers[0].Emergency && offers[0].Priority < int.MaxValue, "priority-first never outranks actual emergency");
            Command(c, game, Enabled(game));
            c.That(!Node(game, ordinary).Worker.IsValid && Jobs(game).Any(j => j.Target == Entity(game, "destination") && j.Worker == Entity(game, "machine")), "real machine assigned to ordinary incident response");
            var twin = factory.Load(game.Save());
            Until(game, () => Work(game).TryReadActivityCompletion("activity:shared.dry-response", Entity(game, "destination"), out _), "real emergency travels and responds", twin);
            c.Equal(game.Hash(), twin.Hash(), "emergency precedence survives saved physical response");
            c.Equal(Fix.FromInt(20), Stock(game, "destination"), "authored dry response never invents consumption");
        }
        private static void Contracts(Check c)
        {
            string original = FrameworkProofContent.Read("dispatch-policy.json");
            foreach (string fault in new[] { "missing", "invalid", "null" })
            {
                var document = Json.Parse(original); var row = document.Get("dispatch_profiles")[5];
                if (fault == "missing") row.Remove("ordering");
                if (fault == "invalid") row.Set("ordering", JsonValue.Of("age-magic"));
                if (fault == "null") row.Set("ordering", JsonValue.Null());
                c.Throws<ContentException>(() => New(Factory("dispatch.priority", document.ToString())), "missing/invalid ordering refused before Update: " + fault);
            }
            var a = new WorkerDispatchOffer("sim.work", 1, "job", new PlayerId(0), new EntityId(1), new EntityId(2),
                true, Code.None, false, 1000, 9, 10, 1, 1, 0, true).WithStarvation(5);
            var b = new WorkerDispatchOffer("sim.work", 2, "job", new PlayerId(0), new EntityId(1), new EntityId(2),
                true, Code.None, false, 1, 0, 10, 1, 1, 0, true).WithStarvation(5);
            c.That(WorkSystem.CompareAssignments(a, b) < 0 && WorkSystem.CompareAssignments(b, a) > 0, "priority-first comparison antisymmetric");
            var agedA = a.WithOrdering(WorkerDispatchOrdering.StarvationFirst); var agedB = b.WithOrdering(WorkerDispatchOrdering.StarvationFirst);
            c.That(WorkSystem.CompareAssignments(agedA, agedB) > 0 && WorkSystem.CompareAssignments(agedB, agedA) < 0, "authored starvation-first comparison antisymmetric");
            c.Equal(0, WorkSystem.CompareAssignments(a, a), "same stable offer compares equal");
            c.Throws<InvalidOperationException>(() => WorkSystem.CompareAssignments(a, agedB), "foreign derived policy comparison cannot silently substitute");
            c.Throws<InvalidOperationException>(() => WorkSystem.CompareAssignments(a.WithOrdering((WorkerDispatchOrdering)9), a.WithOrdering((WorkerDispatchOrdering)9)), "invalid contract ordering rejects");
            var candidate = agedB.WithCandidate(new EntityId(3), 2);
            c.That(candidate.Ordering == agedB.Ordering && candidate.Starved == agedB.Starved && candidate.EffectivePriority == agedB.EffectivePriority, "candidate projection preserves original engine-stamped rank");
            var emergency = a.WithOrdering(WorkerDispatchOrdering.PriorityFirst);
            var blocked = new WorkerDispatchOffer("sim.work", 3, "job", new PlayerId(0), new EntityId(1), EntityId.None,
                false, WorkSystem.BlockerNoWorker, true, int.MaxValue, 0, 10, null, 1, 0, true).WithStarvation(5);
            c.That(WorkSystem.CompareAssignments(emergency, blocked) < 0, "eligibility remains absolute before emergency/priority/age");
        }
    }
}
