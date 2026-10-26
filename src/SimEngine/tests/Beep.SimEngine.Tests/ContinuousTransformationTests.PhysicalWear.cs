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
    public static partial class ContinuousTransformationTests
    {
        private const string PhysicalWearMinuteCase = "Continuous accounting: hauled catalyst wear backpressure replenishment and saved stop retain original authority";
        private const string PhysicalWearHalfCase = "Continuous accounting quantum: half-minute hauled catalyst wear preserves original authority and classified balances";
        private static void PhysicalWear(Check c, bool half)
        {
            var profile = Json.Parse(FrameworkProofContent.Read("continuous-catalyst-accounting.json"));
            var factory = Factory(edit: root => {
                foreach (string section in new[] { "inputs", "outputs", "tool_consumption" }) Definition(root).Set(section, profile.Get(section).Clone());
                root.Set("handling_profiles", profile.Get("handling_profiles").Clone());
                Part(root, "storage").Set("capacity_policy", profile.Get("storage_capacity").Clone());
                Part(root, "storage").Set("initial_stock", Json.Parse("{}"));
                Part(root, "transformer").Get("operation").Set("enabled", JsonValue.Of(false));
            }, basisEdit: root => {
                if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                var sink = root.Get("entities")[1].Get("components"); sink.Remove("point_stock");
                sink.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"public\":false,\"initial_stock\":{}}"));
                root.Get("entities")[2].Get("components").Get("transporter").Set("handling", JsonValue.Of("flow.accounting.handling"));
            }, flowDefinitions: profile.Get("flow_document").ToString());
            var game = New(factory); var twin = factory.Load(game.Save()); var idle = factory.Load(game.Save()); ulong untouched = idle.Hash();
            EntityId Id(string definition) => game.Seat(0).Snapshot.Entities.Single(row => row.Def == definition).Id;
            var site = Id("flow.factory"); var donor = Id("source"); var sinkId = Id("destination"); var machine = Id("machine");
            ResourceId supply = default, product = default, residue = default;
            if (!game.Seat(0).View.TryFindResource("supply", out supply) || !game.Seat(0).View.TryFindResource("flow.product", out product) ||
                !game.Seat(0).View.TryFindResource("flow.residue", out residue)) throw new InvalidOperationException("missing actual continuous accounting resources");
            int delivery = int.Parse(profile.Get("initial_delivery").Text, System.Globalization.CultureInfo.InvariantCulture);
            var budget = new CapacityLogisticsStressTests.Profile(); long peak = 0, allocated = 0; int updates = 0, submitted = 0;
            var trace = new Queue<string>();
            TransformationSnapshot Facts() {
                if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(site, Capability, out var value)) throw new InvalidOperationException("missing actual continuous processor");
                return value;
            }
            List<WorkJobView> Jobs() { var values = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(values); return values; }
            bool CarrierIdle() => !Jobs().Any(row => row.IsHaul && (row.State == JobState.Open || row.State == JobState.Assigned || row.State == JobState.Paused));
            Fix Material(EntityId entity, ResourceId resource) {
                var refusal = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity, resource, out var value);
                if (!refusal.IsNone) throw new InvalidOperationException("actual continuous stock " + refusal); return value.Stock;
            }
            BigInteger Classified(ResourceId resource, ResourceFlowKind kind) => game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(row => row.Resource == resource && row.Kind == kind).Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw);
            void Observe() {
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("continuous wear fault " + game.Fault + "/" + twin.Fault);
                c.Equal(game.Hash(), twin.Hash(), "saved interleaved physical wear sessions retain identical actual stock claims work and time");
                foreach (var resource in new[] { supply, product, residue }) {
                    Fix total = Facts().Wip.Stock.AmountOf(resource);
                    foreach (var entity in game.Seat(0).Snapshot.Entities)
                        if (game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, resource, out var value).IsNone) total += value.Stock;
                    BigInteger opening = resource == supply ? new BigInteger(Fix.FromInt(20).Raw) : BigInteger.Zero;
                    c.Equal(opening + Classified(resource, ResourceFlowKind.Produced), new BigInteger(total.Raw) + Classified(resource, ResourceFlowKind.Consumed),
                        "every physical stock cargo WIP and cumulative feed wear fuel coproduct fact balances exactly");
                    c.Equal(BigInteger.Zero, Classified(resource, ResourceFlowKind.Destroyed) + Classified(resource, ResourceFlowKind.Lost) +
                        Classified(resource, ResourceFlowKind.AuthoredCreation) + Classified(resource, ResourceFlowKind.StockPurchase), "ordinary physical processing invents or discards no material");
                }
                var state = Facts();
                if (state.Wip.Execution > 0) {
                    c.Equal(new BigInteger((state.Flow.Consumed.AmountOf(supply) + state.Flow.ToolConsumed.AmountOf(supply) + state.Operating.Consumed.AmountOf(supply)).Raw),
                        Classified(supply, ResourceFlowKind.Consumed), "one committed consumption includes actual feed discrete wear and elapsed operating draw without duplicated charges");
                    c.That(state.Wip.Stock.IsEmpty && state.Tools.Held.AmountOf(supply) == Fix.One, "continuous flow has no hidden batch WIP and retains one real original catalyst charge");
                }
                trace.Enqueue(game.Clock.StepIndex + "/" + state.Phase + "/" + state.Blocker + "/" + state.Progress + "/" + string.Join(";", Jobs().Select(row => row.Id + "/" + row.State + "/" + row.Loaded + "/" + row.Blocker)));
                if (trace.Count > 8) trace.Dequeue();
            }
            void Update(SimGame session) {
                long before = GC.GetAllocatedBytesForCurrentThread(); session.Update();
                if (ReferenceEquals(session, game)) { long bytes = GC.GetAllocatedBytesForCurrentThread() - before; peak = Math.Max(peak, bytes); allocated = checked(allocated + bytes); updates++; }
            }
            void Step() { Update(game); Update(twin); Observe(); }
            void Wait(Func<bool> ready, string stage) {
                for (int i = 0; i < 512 && !ready(); i++) Step();
                if (!ready()) throw new InvalidOperationException(stage + " exceeded512Updates: " + string.Join(" | ", trace));
            }
            void Input(Command command) {
                foreach (var session in new[] { game, twin }) {
                    ulong pure = session.Hash(); c.Equal(Code.None, session.Seat(0).Preview(command), "normal continuous production/physical hauling intent admits");
                    c.Equal(pure, session.Hash(), "preview creates no material progress commitment or time");
                    int sequence = session.Seat(0).Submit(command); if (ReferenceEquals(session, game)) submitted++;
                    Update(session); long cursor = 0; var answers = new List<CommandResult>(); session.Seat(0).ReadResults(ref cursor, answers);
                    var answer = answers.Single(row => row.Sequence == sequence); c.That(answer.Accepted, "normal inbox commits continuous/physical intent " + answer.Refusal);
                    if (!answer.Accepted) throw new InvalidOperationException("continuous input refused " + answer.Refusal);
                }
                Observe();
            }
            void Save() { twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual loaded state matches without abandoning recorded primary"); Observe(); }
            void Control(bool enabled, Fix target) => Input(new ConfigureOperationCommand { Entity = site, Operation = Capability, Enabled = enabled, Target = target });
            void Haul(EntityId from, EntityId to, ResourceId resource, Fix amount) => Input(new HaulResourcesCommand {
                Source = from, Destination = to, Worker = machine, Resource = resource, Ceiling = amount, Priority = 100 });
            c.That(!game.Modules.Contains("sim.tycoon"), "physical catalyst wear and production need no business module");
            var limits = new ReplayLimits(32 * 1024 * 1024, 5000); using var recording = game.RecordReplay(limits);
            Observe(); Input(new EnqueueTransformationCommand { Transformer = site, Capability = Capability, Transformation = "flow.refine" });
            Haul(donor, site, supply, Fix.FromInt(delivery));
            Wait(() => Jobs().Any(row => row.IsHaul && row.Loaded.IsPositive && row.ToilIndex == 2), "finite startup cargo physically loaded and travelling");
            c.That(Facts().Wip.Execution == 0 && Facts().Progress.Equals(WorkProgress.Zero) && Facts().Flow.Produced.IsEmpty && Material(site, supply) == Fix.Zero,
                "loaded cargo is not processor supply and disabled empty stream cannot earn work or products");
            Save(); Wait(() => Material(site, supply) == Fix.FromInt(delivery) && CarrierIdle(), "actual startup delivery unloads and carrier departs");
            Control(true, Fix.Half);
            Wait(() => Facts().Wip.Execution > 0 && Facts().Flow.ToolConsumed.AmountOf(supply) == Fix.One, "physically funded stream earns its first native discrete wear quantum");
            var original = Facts(); var receipt = original.Tools.Receipts.Single();
            c.That(receipt.Amount == Fix.One && original.Flow.Consumed.AmountOf(supply).IsPositive && original.Operating.Consumed.AmountOf(supply).IsPositive,
                "whole physical startup admits feed wear operating input and original catalyst hold together");
            Save(); Wait(() => Facts().Blocker == TransformerManager.BlockerOutput, "actual coproduct growth fills original eight-unit store");
            var blocked = Facts(); BigInteger burned = Classified(supply, ResourceFlowKind.Consumed);
            c.That(blocked.Flow.NextOutputs.Lines.Any(row => row.Amount.IsPositive), "backpressure blocks an actual pending product delta");
            Save(); for (int i = 0; i < 4; i++) Step();
            c.That(Facts().Progress.Equals(blocked.Progress) && Facts().Operating.ElapsedTicks == blocked.Operating.ElapsedTicks && Facts().Flow.ToolConsumed.Lines.SequenceEqual(blocked.Flow.ToolConsumed.Lines),
                "saved output wait earns no throughput catalyst wear or elapsed operating draw");
            c.Equal(burned, Classified(supply, ResourceFlowKind.Consumed), "backpressure never burns ready feed wear or fuel prefix");
            Control(false, Fix.FromRatio(1, 4));
            foreach (var resource in new[] { product, residue }) {
                Fix output = Material(site, resource); c.That(output.IsPositive, "both original coproducts really exist before physical clearing");
                Haul(site, sinkId, resource, output);
                Wait(() => Jobs().Any(row => row.IsHaul && row.Resource == resource && row.Loaded.IsPositive), "real produced material is loaded for physical output clearing");
                Save(); Wait(() => Material(site, resource) == Fix.Zero && CarrierIdle(), "real produced cargo unloads at independent sink and carrier departs");
            }
            c.That(Facts().Progress.Equals(blocked.Progress) && Facts().Tools.Receipts.Single().Token.Equals(receipt.Token), "disabled physical output clearing retains earned work and original catalyst token");
            Fix refill = Fix.FromInt(delivery) - Material(site, supply);
            c.That(refill.IsPositive, "earned feed wear and fuel create actual replenishment need"); Haul(donor, site, supply, refill);
            Wait(() => Jobs().Any(row => row.IsHaul && row.Loaded.IsPositive), "finite original donor physically supplies replenishment");
            Save(); Wait(() => Material(site, supply) == Fix.FromInt(delivery) && CarrierIdle(), "replenishment reaches original actual store without fabricated supply");
            c.Equal(burned, Classified(supply, ResourceFlowKind.Consumed), "disabled trips/refill cause no extra wear feed or fuel");
            Control(true, Fix.FromRatio(1, 4));
            Wait(() => Facts().Flow.ToolConsumed.AmountOf(supply) >= Fix.FromInt(2), "slower actual resumed work earns next discrete catalyst quantum");
            var resumed = Facts();
            c.That(resumed.Wip.Execution == original.Wip.Execution && resumed.Tools.Receipts.Single().Token.Equals(receipt.Token) &&
                resumed.Tools.Receipts.Single().Amount == Fix.One, "catalyst replenishment retains original execution charge and token instead of allocating another authority");
            c.Equal(Fix.One, original.Flow.ToolConsumed.AmountOf(supply), "retained earlier wear facts are immutable");
            Control(false, Fix.FromRatio(1, 4)); Save(); var beforeStop = Classified(supply, ResourceFlowKind.Consumed);
            Input(new CancelTransformationCommand { Transformer = site, Capability = Capability, Index = 0 });
            c.That(Facts().QueueCount == 0 && Facts().Tools.Receipts.Count == 0 && Facts().Wip.Stock.IsEmpty, "normal continuous stop releases original charge and leaves no hidden WIP");
            c.Equal(beforeStop, Classified(supply, ResourceFlowKind.Consumed), "stop never refunds or repeats earned feed wear or operating draw");
            c.That(Material(site, supply) >= Fix.One, "unused original catalyst exists as actual released stock");
            Save(); for (int i = 0; i < 3; i++) Step();
            c.Equal(beforeStop, Classified(supply, ResourceFlowKind.Consumed), "stopped saved continuation has no ghost process consumption");
            c.That(game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).EvictedThroughTick >= 0, "two-record eviction retains all cumulative wear feed fuel and coproduct accounting");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(site, Capability, out _) &&
                game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Status == ResourceFlowReadStatus.NotOwned, "continuous execution and material accounting remain company-private");
            c.Equal(untouched, idle.Hash(), "independent loaded session retains its own resources progress and clock");
            c.Equal(submitted, recording.CommandCount, "complete recording contains every physical delivery control queue and stop input");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "complete recording reaches every actual primary Update");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "entire physical catalyst supply backpressure refill and stop replay reaches final live hash");
            c.That(peak <= budget.PerUpdateBudget && allocated <= budget.TotalBudget, "original managed primary Update allocation budgets pass with full recording");
            Console.WriteLine("continuous wear metrics: half=" + half + ";updates=" + updates + ";commands=" + submitted + ";peak-update-bytes=" + peak + ";total-update-bytes=" + allocated +
                ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
        }
    }
}
