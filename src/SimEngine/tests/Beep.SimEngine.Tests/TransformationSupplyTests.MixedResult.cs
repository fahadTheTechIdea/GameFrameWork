using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static partial class TransformationSupplyTests
    {
        private const string MixedResultCase = "Mixed result accounting: physical supply capacity clearing rollout wait and saved machine creation commit together";
        private static void MixedResult(Check c)
        {
            var profile = Json.Parse(FrameworkProofContent.Read("mixed-result-accounting.json"));
            var factory = Factory(edit: root => {
                root.Set("resources", profile.Get("resources").Clone());
                var recipes = JsonValue.NewArray(); recipes.Add(profile.Get("recipe").Clone()); recipes.Add(root.Get("transformations")[1].Clone()); root.Set("transformations", recipes);
                foreach (string field in new[] { "id", "kind", "components" }) root.Get("entities")[0].Set(field, profile.Get("result").Get(field).Clone());
                var parts = root.Get("entities")[1].Get("components"); parts.Get("storage").Set("capacity_policy", JsonValue.Of("fixture.count.8"));
                parts.Set("point_dock", profile.Get("dock").Clone());
            }, basisEdit: root => root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId)));
            var game = New(factory); var twin = factory.Load(game.Save()); var idle = factory.Load(game.Save()); ulong untouched = idle.Hash();
            EntityId Id(string definition) => game.Seat(0).Snapshot.Entities.Single(row => row.Def == definition).Id;
            var site = Id("supply.factory"); var donor = Id("source"); var carrier = Id("machine");
            if (!game.Seat(0).View.TryFindResource("supply", out var supply) || !game.Seat(0).View.TryFindResource("mixed.byproduct", out var product)) throw new InvalidOperationException("missing actual mixed result resources");
            var budget = new CapacityLogisticsStressTests.Profile(); long peak = 0, allocated = 0; int updates = 0, submitted = 0;
            var trace = new Queue<string>();
            TransformationSnapshot Facts() {
                if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(site, First, out var value)) throw new InvalidOperationException("missing actual mixed processor"); return value;
            }
            List<WorkJobView> Jobs() { var values = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(values); return values; }
            bool IdleCarrier() => !Jobs().Any(row => row.IsHaul && (row.State == JobState.Open || row.State == JobState.Assigned || row.State == JobState.Paused));
            int Results() => game.Seat(0).Snapshot.Entities.Count(row => row.Def == "supply.result");
            Fix Material(EntityId entity, ResourceId resource) {
                var result = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity, resource, out var value);
                if (!result.IsNone) throw new InvalidOperationException("mixed actual stock " + result); return value.Stock;
            }
            BigInteger Classified(ResourceId resource, ResourceFlowKind kind) => game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(row => row.Resource == resource && row.Kind == kind).Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw);
            void Observe() {
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("mixed physical result fault " + game.Fault + "/" + twin.Fault);
                c.Equal(game.Hash(), twin.Hash(), "saved mixed-result twins retain actual stock WIP receipts capacity rollout identities and clock every Update");
                foreach (var resource in new[] { supply, product }) {
                    Fix total = Facts().Wip.Stock.AmountOf(resource);
                    foreach (var entity in game.Seat(0).Snapshot.Entities)
                        if (game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, resource, out var value).IsNone) total += value.Stock;
                    BigInteger opening = resource == supply ? new BigInteger(Fix.FromInt(20).Raw) : BigInteger.Zero;
                    c.Equal(opening + Classified(resource, ResourceFlowKind.Produced), new BigInteger(total.Raw) + Classified(resource, ResourceFlowKind.Consumed),
                        "mixed physical entity stock cargo and WIP plus original committed classifications remain exactly funded");
                    c.Equal(BigInteger.Zero, Classified(resource, ResourceFlowKind.Destroyed) + Classified(resource, ResourceFlowKind.Lost) + Classified(resource, ResourceFlowKind.AuthoredCreation),
                        "ordinary mixed manufacture destroys loses or grants no extra material");
                }
                c.That(Results() <= 1, "single mixed execution cannot create a second result entity");
                if (Results() == 0) c.Equal(BigInteger.Zero, Classified(product, ResourceFlowKind.Produced), "no byproduct prefix exists before actual entity result commits");
                else c.Equal(new BigInteger(Fix.FromInt(2).Raw), Classified(product, ResourceFlowKind.Produced), "actual entity appears together with its complete two-unit byproduct");
                var state = Facts(); trace.Enqueue(game.Clock.StepIndex + "/" + state.Phase + "/" + state.Blocker + "/" + state.Progress + "/" + string.Join(";", Jobs().Select(row => row.Id + "/" + row.State + "/" + row.Loaded + "/" + row.Blocker)));
                if (trace.Count > 8) trace.Dequeue();
            }
            void Update(SimGame session) {
                long before = GC.GetAllocatedBytesForCurrentThread(); session.Update();
                if (ReferenceEquals(session, game)) { long bytes = GC.GetAllocatedBytesForCurrentThread() - before; peak = Math.Max(peak, bytes); allocated = checked(allocated + bytes); updates++; }
            }
            void Step() { Update(game); Update(twin); Observe(); }
            void Wait(Func<bool> done, string stage) {
                for (int i = 0; i < 512 && !done(); i++) Step();
                if (!done()) throw new InvalidOperationException(stage + " exceeded512Updates: " + string.Join(" | ", trace));
            }
            void Input(Command command) {
                foreach (var session in new[] { game, twin }) {
                    ulong pure = session.Hash(); c.Equal(Code.None, session.Seat(0).Preview(command), "ordinary mixed-production physical intent admits"); c.Equal(pure, session.Hash(), "preview changes no stock progress geometry or identities");
                    int sequence = session.Seat(0).Submit(command); if (ReferenceEquals(session, game)) submitted++;
                    Update(session); long cursor = 0; var answers = new List<CommandResult>(); session.Seat(0).ReadResults(ref cursor, answers);
                    var answer = answers.Single(row => row.Sequence == sequence); c.That(answer.Accepted, "inbox commits mixed physical intent " + answer.Refusal);
                    if (!answer.Accepted) throw new InvalidOperationException("mixed player input refused " + answer.Refusal);
                }
                Observe();
            }
            void Save() { twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved actual mixed state restores without abandoning recorded primary"); Observe(); }
            void Control(bool enabled) => Input(new ConfigureOperationCommand { Entity = site, Operation = First, Enabled = enabled, Target = Fix.One });
            void Haul(EntityId from, EntityId to, int amount) => Input(new HaulResourcesCommand { Source = from, Destination = to, Resource = supply, Worker = carrier, Ceiling = Fix.FromInt(amount), Priority = 100 });
            var limits = new ReplayLimits(32 * 1024 * 1024, 5000); using var recording = game.RecordReplay(limits);
            c.That(!game.Modules.Contains("sim.tycoon"), "physical mixed resources and machine manufacturing need no business module"); Observe();
            Input(new EnqueueTransformationCommand { Transformer = site, Capability = First, Transformation = "supply.first" });
            Wait(() => Jobs().Any(row => row.IsHaul && row.Loaded.IsPositive && row.ToilIndex == 2), "real automatic startup materials physically loaded and travelling");
            c.That(Facts().Wip.Stock.IsEmpty && Facts().Progress.WholeTicks == 0 && Results() == 0, "loaded startup cargo is not instant processor stock work or a new machine"); Save();
            Wait(() => Facts().Progress.WholeTicks > 0 && Facts().Progress.WholeTicks < Facts().Required, "physically supplied partial original thirty-minute manufacture");
            Control(false); var partial = Facts(); var receipt = partial.Tools.Receipts.Single(); long execution = partial.Wip.Execution;
            c.That(partial.Wip.Stock.AmountOf(supply) == Fix.FromInt(3) && receipt.Amount == Fix.One, "real WIP feed and original catalyst hold have one authoritative owner");
            Wait(IdleCarrier, "startup carrier physically departs"); Haul(donor, site, 7);
            Wait(() => Material(site, supply) == Fix.FromInt(8) && IdleCarrier(), "normal finite delivery fills actual output-capacity destination");
            c.That(Facts().Progress.Equals(partial.Progress), "disabled physical deliveries earn no manufacture progress"); Control(true);
            Wait(() => Facts().Blocker == TransformerManager.BlockerOutput, "earned mixed completion waits for actual resource output room");
            c.That(Facts().Progress.Complete(Facts().Required) && Facts().Wip.Stock.AmountOf(supply) == Fix.FromInt(3) && Facts().Tools.Receipts.Single().Token.Equals(receipt.Token) && Results() == 0,
                "full actual output store retains earned work original WIP tool authority and no entity prefix");
            Save(); for (int i = 0; i < 3; i++) Step();
            c.Equal(BigInteger.Zero, Classified(supply, ResourceFlowKind.Consumed), "saved output-capacity wait consumes no feed or wear");
            Control(false); Haul(site, donor, 1);
            Wait(() => Jobs().Any(row => row.IsHaul && row.Source == site && row.Loaded == Fix.One), "only real physical pickup frees output room");
            Input(new SetPointAccessCommand { Open = false }); Control(true);
            Wait(() => Facts().Phase == TransformationPhase.Blocked && Facts().Blocker == Codes.Blocked, "actual engine geometry refuses mixed machine rollout despite available resource room");
            c.That(Material(site, supply) == Fix.FromInt(7) && Facts().Wip.Execution == execution && Facts().Tools.Receipts.Single().Token.Equals(receipt.Token) && Results() == 0,
                "rollout refusal cannot consume original WIP or catalyst or emit partial byproducts");
            Save(); for (int i = 0; i < 3; i++) Step(); c.Equal(BigInteger.Zero, Classified(supply, ResourceFlowKind.Consumed), "saved rollout wait preserves all physical input authority");
            Input(new SetPointAccessCommand { Open = true });
            Wait(() => Results() == 1 && Facts().QueueCount == 0 && IdleCarrier(), "opened real rollout commits whole result and actual return cargo finishes");
            c.Equal(new BigInteger(Fix.FromInt(4).Raw), Classified(supply, ResourceFlowKind.Consumed), "mixed completion spends exactly three actual feed plus one catalyst wear");
            c.Equal(Fix.FromInt(2), Material(site, product), "whole mixed conversion leaves exactly two actual byproducts");
            var result = game.Seat(0).Snapshot.Entities.Single(row => row.Def == "supply.result");
            c.That(result.Spatial is PointSnapshot point && point.Position >= Fix.FromInt(61), "new machine uses actual authored rollout lane, never prototype position zero");
            c.Equal(Fix.Zero, Material(result.Id, supply), "result creation grants no hidden initial cargo");
            c.That(Facts().Wip.Stock.IsEmpty && Facts().Tools.Receipts.Count == 0, "whole completion releases original WIP and tool receipts once");
            Save(); for (int i = 0; i < 5; i++) Step(); c.Equal(1, Results(), "idle Updates cannot repeat completed mixed entity creation");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(site, First, out _), "mixed execution facts remain company-private");
            c.Equal(untouched, idle.Hash(), "unrelated saved session retains original physical resources and machine identities");
            c.Equal(submitted, recording.CommandCount, "recording contains every actual supply control and geometry input");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "recording covers every committed primary Update through completed rollout");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "complete mixed supply capacity clearing rollout wait and machine-result replay reaches final live hash");
            c.That(peak <= budget.PerUpdateBudget && allocated <= budget.TotalBudget, "original per-tier managed primary Update budgets hold for finite mixed-result workflow with recording");
            Console.WriteLine("mixed result metrics: updates=" + updates + ";commands=" + submitted + ";entities=" + Results() + ";peak-update-bytes=" + peak + ";total-update-bytes=" + allocated +
                ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
        }
    }
}
