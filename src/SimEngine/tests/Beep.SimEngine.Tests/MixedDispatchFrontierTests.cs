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
    internal static class MixedDispatchFrontierTests
    {
        private const string Case = "Mixed dispatch frontiers: priority barriers, physical stock and saved recovery";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool provider)
            => DispatchAgingTests.Factory(profile: "dispatch.priority", shipment: provider, shipmentPriority: 100,
                shipmentDefinitions: FrameworkProofContent.Read("dispatch-frontier.json"), shipmentDocument: "dispatch-frontier.json",
                edit: root =>
                {
                    root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("8"));
                    root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("20"));
                    root.Get("entities")[2].Get("components").Get("worker").Set("roles", Json.Parse("[\"inspect\"]"));
                }, startupProfiles: new[] { "dispatch-frontier.start" });
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 298,
            Mods = { "proof.shipments" }, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Entity(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Id;
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static List<WorkJobView> Jobs(SimGame game)
        { var rows = new List<WorkJobView>(); Work(game).CopyJobs(rows); return rows; }
        private static ActivityNodeView Node(SimGame game, int order)
        {
            var rows = new List<ActivityNodeView>();
            if (!Work(game).TryCopyNodes(order, rows)) throw new InvalidOperationException("missing frontier order " + order);
            return rows.Single();
        }
        private static Fix Stock(SimGame game, string prototype)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, prototype), rows);
            return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static Fix Total(SimGame game) => new[] { "source", "destination", "frontier.sink", "machine", "frontier.courier" }
            .Aggregate(Fix.Zero, (sum, name) => sum + Stock(game, name));
        private static bool Done(SimGame game, string plan, string target, int order)
            => Work(game).TryReadActivityCompletion("activity:" + plan, Entity(game, target), out var fact) && fact.Order == order;
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("frontier clock=" + game.Tick + "/fault=" + game.Fault + "/twin=" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string phase, SimGame twin = null)
        {
            for (int i = 0; i < 1200 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(phase + "/clock=" + game.Tick + "/jobs=" +
                string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.Loaded)));
        }
        private static CommandResult Command(Check c, SimGame game, Command command, SimGame twin = null)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal command preview");
            c.Equal(hash, game.Hash(), "preview creates no stock/worker claims");
            int sequence = game.Seat(0).Submit(command); twin?.Seat(0).Submit(command); Update(game, twin);
            long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            var answer = answers.Single(r => r.Sequence == sequence);
            c.That(answer.Accepted, "normal player command admitted: " + answer.Refusal);
            if (!answer.Accepted) throw new InvalidOperationException("frontier refusal " + answer.Refusal);
            return answer;
        }
        private static ResourceId Supply(SimGame game)
        {
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("missing authored supply");
            return resource;
        }
        private static ConfigureStockPolicyCommand Demand(SimGame game, string target, bool enabled)
            => new ConfigureStockPolicyCommand { Store = Entity(game, target), Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(enabled, Fix.One, Fix.FromInt(8), Fix.FromInt(8),
                    100, LogisticsReserveAccess.Protected)) };
        private static int EnableTogether(Check c, SimGame game, SimGame twin, bool courier, PostOrderCommand pending)
        {
            var sequences = new List<int>();
            int posted = 0;
            if (pending != null)
            { c.Equal(Code.None, game.Seat(0).Preview(pending), "timed work enters same real Update as eligibility");
                posted = game.Seat(0).Submit(pending); twin.Seat(0).Submit(pending); }
            foreach (string name in new[] { "machine", "frontier.inspector", "frontier.courier" })
            {
                var command = new ConfigureWorkPolicyCommand { Worker = Entity(game, name),
                    Settings = new WorkPolicySettings(name != "frontier.courier" || courier, 0,
                        new[] { new WorkerRoleId(name == "frontier.courier" ? "haul" : "inspect") },
                        null, true, true, WorkAssignmentMode.All) };
                ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "simultaneous machine eligibility preview");
                c.Equal(hash, game.Hash(), "eligibility preview pure");
                sequences.Add(game.Seat(0).Submit(command)); twin.Seat(0).Submit(command);
            }
            Update(game, twin);
            long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            c.That(sequences.All(s => answers.Single(a => a.Sequence == s).Accepted), "same Update admits all actual worker policies");
            if (posted == 0) return 0;
            var post = answers.Single(a => a.Sequence == posted); c.That(post.Accepted, "actual timed post accepted");
            return checked((int)post.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "native batch semantics identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no extra saved frontier/claim ledger");
            foreach (bool provider in new[] { true, false })
                foreach (bool timer in new[] { false, true }) Physical(c, provider, timer, true);
            Physical(c, true, false, false);
        }
        private static void Physical(Check c, bool provider, bool timer, bool courier)
        {
            var factory = Factory(provider); var game = New(factory); var isolated = New(factory); ulong idleHash = isolated.Hash();
            string lowPlan = timer ? "frontier.timer-low" : "ordering.low";
            using (var recorder = game.RecordReplay(Limits))
            {
                if (!provider) Command(c, game, Demand(game, "frontier.sink", true));
                int high = (int)Command(c, game, new PostOrderCommand { Plan = "ordering.high", Target = Entity(game, "destination"), Priority = 1000 }).Arg0;
                int low = timer ? 0 : (int)Command(c, game, new PostOrderCommand { Plan = lowPlan, Target = Entity(game, "source"), Priority = 1 }).Arg0;
                // Staffed offers become ready in the same normal Update that enables their actual actors.
                // A timed low node must not start before the carrier can compete, so post it together below.
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending authored mixed intents restore");
                int timed = EnableTogether(c, game, twin, courier, timer ? new PostOrderCommand { Plan = lowPlan, Target = Entity(game, "source"), Priority = 1 } : null);
                if (timer) low = timed;
                if (courier)
                {
                    c.That(Node(game, high).Worker.IsValid, "highest native work owns its real machine");
                    c.That(!Node(game, low).Worker.IsValid && Node(game, low).Resources.Count == 0 &&
                        Node(game, low).Progress.WholeTicks == 0, "lower native/timed admission cannot reserve across shipment frontier");
                    var haul = Jobs(game).Single(j => j.IsHaul && j.Destination == Entity(game, "frontier.sink"));
                    c.Equal(Entity(game, "frontier.courier"), haul.Worker, "intervening actual shipment receives distinct carrier");
                    c.Equal(Fix.FromInt(8), haul.Amount, "shipment holds exact finite source stock/cargo capacity");
                    c.Equal(Fix.Zero, Stock(game, "frontier.sink"), "assignment does not teleport delivery");
                    twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual competing claims save/restore");
                    Until(game, () => Done(game, "ordering.high", "destination", high) &&
                        Stock(game, "frontier.sink") == Fix.FromInt(8) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid),
                        "physical priority work and eight-unit timed shipment", twin);
                    c.That(!Node(game, low).Worker.IsValid && Node(game, low).Resources.Count == 0 &&
                        Node(game, low).Progress.WholeTicks == 0 && !Node(game, low).Blocker.IsNone,
                        "unfunded low work stays blocked, never borrows claimed materials");
                    c.Equal(Fix.Zero, Stock(game, "source"), "real shipment removes exactly eight source stock");
                    c.Equal(Fix.FromInt(12), Stock(game, "destination"), "real highest work consumes its own eight inputs");
                    c.Equal(Fix.FromInt(20), Total(game), "28 initial minus8 real productive consumption; haul conserves supply");
                    var foreign = new List<WorkerDispatchOffer>(); game.Seat(1).View.Feature<WorkView>().CopyAssignments(foreign);
                    c.Equal(0, foreign.Count, "private blocked/source facts remain company-scoped");
                    Command(c, game, new CancelOrderCommand { Order = low }, twin);
                    Command(c, game, Demand(game, "source", true), twin);
                    Until(game, () => Stock(game, "source") == Fix.FromInt(8) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid),
                        "ordinary physical replenishment after cancellation", twin);
                    Command(c, game, Demand(game, "source", false), twin);
                    int recovered = (int)Command(c, game, new PostOrderCommand { Plan = lowPlan, Target = Entity(game, "source"), Priority = 1 }, twin).Arg0;
                    Until(game, () => Done(game, lowPlan, "source", recovered), "physically supplied low work completion", twin);
                    c.Equal(Fix.FromInt(12), Total(game), "28 initial minus16 actual work, no duplication or instant recovery");
                    c.Equal(Fix.Zero, Stock(game, "source"), "restored ordinary low work consumes exactly delivered eight");
                }
                else
                {
                    c.That(Node(game, high).Worker.IsValid && Node(game, low).Worker.IsValid,
                        "blocked/unmatched external offer is not a native batching barrier");
                    Until(game, () => Done(game, "ordering.high", "destination", high) && Done(game, lowPlan, "source", low),
                        "adjacent native physical frontier", twin);
                    c.Equal(Fix.FromInt(12), Total(game), "adjacent native work consumes16, blocked provider grants nothing");
                    c.Equal(Fix.Zero, Stock(game, "frontier.sink"), "disabled carrier cannot invent delivery");
                }
                c.Equal(game.Hash(), twin.Hash(), "physical mixed frontier saved twins stay identical");
                c.Equal(idleHash, isolated.Hash(), "frontier policies/claims isolated across interleaved sessions");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(),
                    "canonical replay preserves real mixed priority: " + replay.Failure + "/" + replay.Detail);
            }
        }
    }
}
