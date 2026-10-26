using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    internal static partial class ConnectedResourcePortTests
    {
        private const string TopologyContentionNativeCase = "Topology contention: repeated competing cuts and physical reconnect preserve native claims stock and allocation budgets";
        private const string TopologyContentionIndependentCase = "Topology contention: half-minute independent ports preserve competing cuts physical reconnect and allocation budgets";
        private static void TopologyContention(Check c, bool half, bool independent)
        {
            var profile = Json.Parse(FrameworkProofContent.Read("topology-contention.json"));
            Physical(c, half, independent, true, (check, factory, game, quantum, custom) => ContendTopology(check, factory, game, profile, quantum, custom),
                edit: data => {
                    for (int i = 0; i < profile.Get("entities").Count; i++) data.Get("entities").Add(profile.Get("entities")[i].Clone());
                    for (int i = 0; i < profile.Get("entity_setups").Count; i++) data.Get("entity_setups").Add(profile.Get("entity_setups")[i].Clone());
                }, editWorld: root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
                    for (int i = 0; i < profile.Get("entity_setups").Count; i++)
                        root.Get("world_setup_profiles")[0].Get("entries").Add(profile.Get("entity_setups")[i].Get("id").Clone());
                });
        }
        // Only ordinary player intents and detached engine facts, reusing the original physical setup.
        private static void ContendTopology(Check c, GameFactory factory, SimGame game, JsonValue profile, bool half, bool independent)
        {
            int rounds = int.Parse(profile.Get("rounds").Text, System.Globalization.CultureInfo.InvariantCulture);
            var targets = Enumerable.Range(0, profile.Get("entities").Count).Select(i => Entity(game, profile.Get("entities")[i].Get("id").Text)).ToArray();
            var worker = Entity(game, "machine"); var site = Site(game); var resource = Supply(game);
            var idle = factory.Load(game.Save()); ulong untouched = idle.Hash(); var twin = factory.Load(game.Save());
            var budget = new CapacityLogisticsStressTests.Profile(); long peak = 0, allocated = 0; int updates = 0;
            var trace = new Queue<string>(); var endpoints = new HashSet<ResourceEndpointId> { Pools(game).Single().Endpoint };
            var orders = new HashSet<int>(); int completedCuts = 0, refusedCuts = 0, submittedInputs = 0;
            BigInteger Consumed() => game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(row => row.Resource == resource && row.Kind == ResourceFlowKind.Consumed).Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw);
            BigInteger baseline = Consumed(); Fix opening = Total(game);
            PlanView Plan(long id) => PermanentRemovalTests.Plan(game, id);
            void Observe() {
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("topology contention fault " + game.Fault + "/" + twin.Fault);
                c.Equal(game.Hash(), twin.Hash(), "interleaved saved sessions preserve actual plans graphs claims work and clock every Update");
                c.Equal(new BigInteger(opening.Raw), new BigInteger(Total(game).Raw) + Consumed() - baseline,
                    "all real entity stock cargo WIP and connected pools plus classified consumption remain exactly funded");
                var assigned = PermanentRemovalTests.Jobs(game).Where(row => row.State == JobState.Assigned).ToArray();
                c.That(assigned.Length <= 1 && assigned.All(row => row.Worker == worker), "one actual machine serves competing cuts and physical reconstruction without double assignment");
                trace.Enqueue(game.Clock.StepIndex + ":" + string.Join(";", PermanentRemovalTests.Jobs(game).Select(row => row.Id + "/" + row.State + "/" + row.Blocker)));
                if (trace.Count > 8) trace.Dequeue();
            }
            void Update(SimGame session) {
                long before = GC.GetAllocatedBytesForCurrentThread(); session.Update();
                if (ReferenceEquals(session, game)) { long bytes = GC.GetAllocatedBytesForCurrentThread() - before; peak = Math.Max(peak, bytes); allocated = checked(allocated + bytes); updates++; }
            }
            void Step() { Update(game); Update(twin); Observe(); }
            void Wait(Func<bool> ready, string stage) {
                for (int i = 0; i < 512 && !ready(); i++) Step();
                if (!ready()) throw new InvalidOperationException(stage + " exceeded512Updates: " + string.Join(" | ", trace) + ";pools=" + Pools(game).Count + ";port=" + Port(game).Blocker);
            }
            CommandResult[] Batch(Command[] commands, bool duplicateLast = false) {
                var actual = new List<CommandResult>();
                foreach (var session in new[] { game, twin }) {
                    var sequences = new List<int>();
                    foreach (var command in commands) {
                        ulong pure = session.Hash(); c.Equal(Code.None, session.Seat(0).Preview(command), "ordinary topology/supply intent admits current facts");
                        c.Equal(pure, session.Hash(), "topology/supply preview changes no geometry pool authority or progress"); sequences.Add(session.Seat(0).Submit(command));
                        if (ReferenceEquals(session, game)) submittedInputs++;
                    }
                    if (duplicateLast) {
                        sequences.Add(session.Seat(0).Submit(commands.Last()));
                        if (ReferenceEquals(session, game)) submittedInputs++;
                    }
                    Update(session); long cursor = 0; var answers = new List<CommandResult>(); session.Seat(0).ReadResults(ref cursor, answers);
                    for (int i = 0; i < commands.Length; i++) {
                        var answer = answers.Single(row => row.Sequence == sequences[i]); c.That(answer.Accepted, "player topology/supply intent committed " + answer.Refusal);
                        if (!answer.Accepted) throw new InvalidOperationException("topology input refused " + answer.Refusal);
                        if (ReferenceEquals(session, game)) actual.Add(answer);
                    }
                    if (duplicateLast) c.Equal(PlanCodes.Duplicate, answers.Single(row => row.Sequence == sequences.Last()).Refusal,
                        "same-update duplicate reconnect owns no second marker order or supply bill");
                }
                Observe(); return actual.ToArray();
            }
            CommandResult Input(Command command) => Batch(new[] { command }).Single();
            void Save() {
                // A recorder belongs to its original session. Advance the restored twin alongside
                // that session instead of abandoning the recorded world at the first checkpoint.
                twin = factory.Load(game.Save());
                c.Equal(game.Hash(), twin.Hash(), "saved topology continuation restores exactly without replacing the recorded session");
                Observe();
            }
            var limits = new ReplayLimits(16 * 1024 * 1024, 5000);
            c.That(!game.Modules.Contains("sim.tycoon"), "physical topology competition and resource consumption require no business module");
            for (int round = 0; round < rounds; round++) {
                using var recording = game.RecordReplay(limits);
                int inputsAtStart = submittedInputs;
                if (round > 0) {
                    Fix before = Stock(game, "destination");
                    Input(new HaulResourcesCommand { Source = site, SourcePort = Shared, Destination = Entity(game, "destination"), Worker = worker, Resource = resource, Ceiling = Fix.FromInt(2), Priority = 100 });
                    Wait(() => Stock(game, "destination") == before + Fix.FromInt(2) && !PermanentRemovalTests.Jobs(game).Any(Live), "finite connected stock physically staged for next reconstruction");
                }
                var original = Pools(game).Single();
                int inspect = (int)Input(new PostOrderCommand { Plan = "connected.inspect", Target = site, Priority = 90 }).Arg0;
                Wait(() => PermanentRemovalTests.Node(game, inspect, "check").Progress.WholeTicks > 0, "real shared tool authority acquired before competing cuts");
                if (!game.Seat(0).View.Feature<InventoryView>().TryGetReservations(site, Shared, out var receipts)) throw new InvalidOperationException("missing actual shared tool claims");
                var held = receipts.Single();
                var cuts = Batch(targets.Select(target => (Command)new SubmitPlanCommand {
                    Intent = new PlanIntent("activity:corridor.remove", ActivityWorkerSelection.CreateArguments(target, ActivityWorkerSelection.Unbound), "contending-cuts", 100, false)
                }).ToArray()).Select(row => row.Arg0).ToArray();
                c.That(cuts.Distinct().Count() == targets.Length && cuts.All(id => Plan(id).State == PlanState.Pending && Plan(id).Order == 0 && Plan(id).Blocker == ResourceTransactions.ActivePromises),
                    "all competing persistent cuts wait on the same actual original receipt, without phantom execution");
                Save(); for (int i = 0; i < 3; i++) Step();
                c.That(Pools(game).Single().Endpoint == original.Endpoint && SpatialCorridorExecutionTests.Segment(game, true, 1).IsPositive &&
                    game.Seat(0).View.Feature<InventoryView>().TryGetReservations(site, Shared, out var saved) && saved.Single().Token.Equals(held.Token),
                    "saved concurrent waits preserve geometry original endpoint and exact receipt authority");
                Input(new CancelOrderCommand { Order = inspect });
                Wait(() => cuts.All(id => Plan(id).Order > 0) && cuts.Any(id => PermanentRemovalTests.Node(game, Plan(id).Order, "cut").Progress.WholeTicks > 0),
                    "released authority activates all distinct plans and one machine earns physical cutting");
                c.That(SpatialCorridorExecutionTests.Segment(game, true, 1).IsPositive, "partially earned competition has not erased the corridor");
                Save();
                Wait(() => cuts.All(id => Plan(id).State == PlanState.Done || Plan(id).State == PlanState.Failed), "one cut commits and losing physical plans report stale topology");
                c.Equal(1, cuts.Count(id => Plan(id).State == PlanState.Done), "exactly one contender owns the physical deletion");
                c.That(cuts.Where(id => Plan(id).State == PlanState.Failed).All(id => Plan(id).Blocker == Codes.NotFound), "losing cuts truthfully report removed geometry, never silent success or terminal world fault");
                foreach (long id in cuts) { c.That(orders.Add(Plan(id).Order), "every repeated contender retains one fresh native order identity"); if (Plan(id).State == PlanState.Done) completedCuts++; else refusedCuts++; }
                var split = Pools(game);
                c.That(split.Count == 2 && split.All(row => row.Endpoint != original.Endpoint && row.MemberCount == 1 && row.OutboundPromises.Count == 0 && row.InboundPromises.Count == 0),
                    "one committed split retires released original authority and leaves no contender promises");
                foreach (var pool in split) c.That(endpoints.Add(pool.Endpoint), "each split endpoint is a genuinely new owner, not recycled history");
                c.That(!Port(game).Ready && Port(game).Blocker == NetworkService.InsufficientMembers && Port(game).Stock == null, "existing minimum-member policy exposes a real unavailable port, not substitute capacity");
                Save();
                long rebuild = InputReconnect();
                long InputReconnect() => Batch(new Command[] { new SubmitPlanCommand { Intent = new PlanIntent("line:corridor.point-line", profile.Get("reconnect").Clone(), "reconnect", 60, false) } }, true).Single().Arg0;
                var marker = Entity(game, "corridor.cursor");
                var supply = new HaulResourcesCommand { Source = Entity(game, "destination"), Destination = marker, Worker = worker, Resource = resource, Ceiling = Fix.FromInt(2), Priority = 100 };
                Wait(() => Plan(rebuild).State == PlanState.Done || game.Seat(0).Preview(supply).IsNone, "physical carrier ready to supply genuine reconstruction");
                if (Plan(rebuild).State != PlanState.Done) Input(supply);
                Save(); Wait(() => Plan(rebuild).State == PlanState.Done && !PermanentRemovalTests.Jobs(game).Any(Live), "finite delivered materials and positive machine work reconnect actual graph");
                var merged = Pools(game).Single();
                c.That(endpoints.Add(merged.Endpoint) && merged.MemberCount == 3 && merged.OutboundPromises.Count == 0 && merged.InboundPromises.Count == 0 && Port(game).Ready && Port(game).Endpoint == merged.Endpoint,
                    "one new real merged owner rebinds original capability with no stale claims or duplicate reconstruction");
                c.Equal(opening - Fix.FromInt((round + 1) * 2), Total(game), "only the exact original two-unit reconstruction bill leaves real stock each round");
                c.That(cuts.All(id => Plan(id).State == PlanState.Done || Plan(id).State == PlanState.Failed), "reconnection never resurrects failed contenders to cut the new graph");
                c.That(!game.Seat(0).View.Feature<InventoryView>().TryGetReservations(site, Shared, out _, original.Endpoint), "retired receipt identity cannot acquire authority on reconstructed endpoint");
                Save();
                c.Equal(submittedInputs - inputsAtStart, recording.CommandCount, "round recorder includes every actual player input including rejected duplicate reconnect");
                c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "round recorder reaches every actual committed Update through final reconstruction");
                var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == game.Hash(), "actual complete competing cut/refusal/duplicate/refill/reconnect round reaches the final live state, not only a valid recording prefix");
                Console.WriteLine("topology replay round: round=" + round + ";initial-step=" + recording.InitialStep + ";final-step=" + game.Clock.StepIndex +
                    ";commands=" + recording.CommandCount + ";checkpoints=" + recording.CheckpointCount + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
            }
            var hidden = new List<PlanView>(); game.Seat(1).View.Feature<WorkView>().CopyPlans(hidden); c.Equal(0, hidden.Count, "competing topology plan histories remain company-private");
            c.Equal(untouched, idle.Hash(), "another loaded session retains its actual original topology work and materials");
            c.That(peak <= budget.PerUpdateBudget && allocated <= budget.TotalBudget, "original managed Update allocation budgets hold for this finite repeated topology workload");
            Console.WriteLine("topology contention metrics: half=" + half + ";independent-port=" + independent + ";updates=" + updates + ";cuts=" + completedCuts + ";refused=" + refusedCuts + ";peak-update-bytes=" + peak + ";total-update-bytes=" + allocated);
        }
    }
}
