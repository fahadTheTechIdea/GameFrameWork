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
        private const string AccountingCase = "Manufacturing accounting: physical supply blocked WIP return and saved catalyst wear conserve actual resources";
        private static void Accounting(Check c)
        {
            var profile = Json.Parse(FrameworkProofContent.Read("transformation-accounting.json"));
            var factory = Factory(edit: root => {
                root.Set("resources", profile.Get("resources").Clone());
                root.Set("capacity_policies", profile.Get("capacity_policies").Clone());
                var recipes = Json.Parse("[]"); recipes.Add(profile.Get("recipe").Clone());
                recipes.Add(root.Get("transformations")[1].Clone()); root.Set("transformations", recipes);
                root.Get("entities")[1].Get("components").Get("storage").Set("capacity_policy", JsonValue.Of("accounting.factory.5"));
            }, basisEdit: root => root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId)));
            var game = New(factory); var idle = New(factory); ulong untouched = idle.Hash(); SimGame twin = null;
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            ulong initialHash = game.Hash(); using var recording = game.RecordReplay(limits);
            int inputs = 0, restores = 0;
            EntityId Id(SimGame session, string definition) => session.Seat(0).Snapshot.Entities.Single(row => row.Def == definition).Id;
            EntityId machine = Id(game, "machine"), site = Id(game, "supply.factory"), donor = Id(game, "source");
            ResourceId supply = default, product = default;
            c.That(game.Seat(0).View.TryFindResource("supply", out supply) &&
                game.Seat(0).View.TryFindResource("accounting.product", out product), "both actual file-authored resources resolve");
            TransformationSnapshot Facts(SimGame session) {
                if (!session.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(site, First, out var value))
                    throw new InvalidOperationException("missing actual manufacturing state");
                return value;
            }
            List<WorkJobView> Jobs(SimGame session) { var jobs = new List<WorkJobView>(); session.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); return jobs; }
            Fix Material(SimGame session, EntityId entity, ResourceId resource) {
                Code result = session.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity, resource, out var value);
                if (!result.IsNone) throw new InvalidOperationException("actual inventory diagnostics: " + result);
                return value.Stock;
            }
            BigInteger Classified(SimGame session, ResourceId resource, ResourceFlowKind kind)
                => session.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                    .Where(row => row.Resource == resource && row.Kind == kind).Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw);
            Fix Actual(SimGame session, ResourceId resource) {
                Fix total = Facts(session).Wip.Stock.AmountOf(resource);
                foreach (var entity in session.Seat(0).Snapshot.Entities)
                    if (session.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, resource, out var value).IsNone) total += value.Stock;
                return total;
            }
            void Observe() {
                foreach (var resource in new[] { supply, product }) {
                    BigInteger opening = resource == supply ? new BigInteger(Fix.FromInt(20).Raw) : BigInteger.Zero;
                    c.Equal(opening + Classified(game, resource, ResourceFlowKind.Produced), new BigInteger(Actual(game, resource).Raw) +
                        Classified(game, resource, ResourceFlowKind.Consumed) + Classified(game, resource, ResourceFlowKind.Destroyed) +
                        Classified(game, resource, ResourceFlowKind.Lost), "per-resource actual stock/cargo/WIP and original committed classifications balance each update");
                }
                if (twin != null) c.Equal(game.Hash(), twin.Hash(), "interleaved saved physical twins have identical resources, claims and progress");
            }
            void StepBoth() { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("manufacturing fault: " + game.Fault + "/" + twin?.Fault); Observe(); }
            void Reach(Func<bool> done, string stage) {
                for (int i = 0; i < 240 && !done(); i++) StepBoth();
                if (!done()) throw new InvalidOperationException(stage + " exceeded240 updates at " + game.Clock.StepIndex + ", phase=" + Facts(game).Phase + ", blocker=" + Facts(game).Blocker +
                    ", jobs=" + string.Join(";", Jobs(game).Select(row => row.Id + "/" + row.State + "/" + row.Loaded + "/" + row.Blocker)));
            }
            void Input(Command command) {
                void Send(SimGame session) {
                    ulong before = session.Hash(); c.Equal(Code.None, session.Seat(0).Preview(command), "ordinary player intent admitted");
                    c.Equal(before, session.Hash(), "preview changes no progress, material or history");
                    int sequence = session.Seat(0).Submit(command); session.Update();
                    if (session.Fault != null) throw new InvalidOperationException("manufacturing command: " + session.Fault);
                    long cursor = 0; var answers = new List<CommandResult>(); session.Seat(0).ReadResults(ref cursor, answers);
                    c.That(answers.Any(answer => answer.Sequence == sequence && answer.Accepted), "real inbox commits only player intent");
                }
                Send(game); if (twin != null) Send(twin); inputs++; Observe();
            }
            void Restore() {
                // Keep the actual recorded primary alive; only the independent saved twin is replaced.
                // Replacing the primary here would leave its recorder attached to an abandoned prefix.
                twin = factory.Load(game.Save()); restores++;
                c.Equal(game.Hash(), twin.Hash(), "loaded physical checkpoint matches the original recorded primary");
                c.That(recording.Active && recording.Failure.IsNone, "original recorder remains active across physical checkpoint restores");
                Observe();
            }
            bool IdleCarrier() => !Jobs(game).Any(row => row.IsHaul && (row.State == JobState.Open || row.State == JobState.Assigned || row.State == JobState.Paused));
            HaulResourcesCommand Haul(EntityId from, EntityId to, int amount) => new HaulResourcesCommand {
                Source = from, Destination = to, Resource = supply, Ceiling = Fix.FromInt(amount), Worker = machine, Priority = 100 };

            c.That(!game.Modules.Contains("sim.tycoon"), "physical manufacturing and resource accounting need no business module");
            Observe();
            Input(new EnqueueTransformationCommand { Transformer = site, Capability = First, Transformation = "supply.first" });
            Reach(() => Jobs(game).Any(row => row.IsHaul && row.Loaded.IsPositive && row.ToilIndex == 2), "automatic supply really loads and travels");
            c.That(Facts(game).Wip.Stock.IsEmpty && Facts(game).Progress.WholeTicks == 0, "work cannot start or consume material while the first carrier is travelling");
            Restore();
            Reach(() => Facts(game).Progress.WholeTicks > 0 && Facts(game).Progress.WholeTicks < 30, "physically supplied first partial batch");
            Input(new ConfigureOperationCommand { Entity = site, Operation = First, Enabled = false, Target = Fix.One });
            var partial = Facts(game); c.That(partial.Wip.Stock.AmountOf(supply) == Fix.FromInt(3) && partial.Tools.Held.AmountOf(supply) == Fix.FromInt(2), "one real WIP buffer holds feed while original stock retains reusable tools");
            c.Equal(BigInteger.Zero, Classified(game, supply, ResourceFlowKind.Consumed), "uncompleted batch has no consumption or wear");
            Reach(IdleCarrier, "first haul physically departs");
            Input(Haul(donor, site, 3)); Reach(() => Material(game, site, supply) == Fix.FromInt(5) && IdleCarrier(), "fill actual return destination through another physical delivery");
            c.That(Facts(game).Progress.Equals(partial.Progress), "disabled batch earns no work during delivery or departure");
            Input(new CancelTransformationCommand { Transformer = site, Capability = First, Index = 0 });
            c.That(Facts(game).Phase == TransformationPhase.Returning && Facts(game).Blocker == TransformerManager.BlockerReturnFull,
                "full actual source prevents whole WIP return rather than dropping material");
            c.That(Facts(game).Wip.Stock.AmountOf(supply) == Fix.FromInt(3) && Facts(game).Tools.Held.AmountOf(supply) == Fix.FromInt(2), "return wait preserves actual feed and original tool promises");
            Restore(); long wait = game.Clock.StepIndex; for (int i = 0; i < 4; i++) StepBoth();
            c.That(game.Clock.StepIndex == wait + 4 && Facts(game).Phase == TransformationPhase.Returning, "blocked cancellation survives saved elapsed time without instant return");
            c.Equal(BigInteger.Zero, Classified(game, supply, ResourceFlowKind.Consumed), "cancelled partial work never consumes feed or catalyst");
            Input(Haul(site, donor, 3));
            Reach(() => Facts(game).QueueCount == 0 && IdleCarrier(), "normal physical pickup frees capacity for original WIP return");
            c.That(Facts(game).Wip.Stock.IsEmpty && Facts(game).Tools.Receipts.Count == 0 && Material(game, site, supply) == Fix.FromInt(5), "actual return releases tools once, with no refund or fabricated production");
            c.Equal(BigInteger.Zero, Classified(game, product, ResourceFlowKind.Produced), "cancelled work creates no output");
            Input(new EnqueueTransformationCommand { Transformer = site, Capability = First, Transformation = "supply.first" });
            Input(new ConfigureOperationCommand { Entity = site, Operation = First, Enabled = true, Target = Fix.One });
            long started = game.Clock.StepIndex;
            Reach(() => Facts(game).QueueCount == 0, "second batch earns original thirty-minute processing");
            c.That(game.Clock.StepIndex >= started + 29, "manufacturing duration is unchanged, not replaced by an instant fallback");
            c.Equal(new BigInteger(Fix.FromInt(4).Raw), Classified(game, supply, ResourceFlowKind.Consumed), "completed batch consumes exactly three feed plus one tool wear");
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), Classified(game, product, ResourceFlowKind.Produced), "produced history contains exactly the two earned outputs");
            c.Equal(BigInteger.Zero, Classified(game, supply, ResourceFlowKind.Produced), "WIP return is a transfer, never newly produced feed");
            c.Equal(BigInteger.Zero, Classified(game, product, ResourceFlowKind.Consumed), "no fictitious product consumption balances duplicate output");
            foreach (var resource in new[] { supply, product }) {
                c.Equal(BigInteger.Zero, Classified(game, resource, ResourceFlowKind.Destroyed), "ordinary manufacturing/cancellation destroys no unaccounted material");
                c.Equal(BigInteger.Zero, Classified(game, resource, ResourceFlowKind.Lost), "ordinary manufacturing/cancellation loses no unaccounted material");
            }
            c.That(game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).EvictedThroughTick >= 0,
                "actual two-record history eviction does not erase cumulative manufacturing accounting");
            c.Equal(Fix.FromInt(16), Actual(game, supply), "remaining supply exists only in actual source, cargo and factory buffers");
            c.Equal(Fix.FromInt(2), Actual(game, product), "complete earned product exists once");
            c.That(Facts(game).Wip.Stock.IsEmpty && Facts(game).Tools.Receipts.Count == 0, "terminal execution retains no WIP or reusable promises");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(site, First, out _), "other company cannot inspect private processing history");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal resources and history restore through original codecs");
            c.Equal(2, restores, "real loaded-travel and blocked-return checkpoints both continue as saved twins");
            c.Equal(7, inputs, "complete player workflow includes both queues, both controls, both manual hauls and cancellation");
            c.Equal(inputs, recording.CommandCount, "recorder includes every actual input from initial unsupplied manufacturing");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount,
                "recorder includes every committed update through automatic supply, cancellation and second batch completion");
            byte[] bytes = recording.Finish(); var description = ReplayVerifier.Describe(bytes, limits);
            c.That(description.InitialHash == initialHash && description.InitialStep == recording.InitialStep,
                "replay starts at the actual initial unsupplied session, not a blocked-return suffix");
            c.That(description.FinalStep == game.Clock.StepIndex && description.FinalHash == game.Hash()
                && description.Commands == inputs && description.Checkpoints == recording.CheckpointCount,
                "finished artifact describes the complete live workflow and all original checkpoints");
            var replay = new ReplayVerifier().Verify(factory, bytes, limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "initial supply, saved travel, blocked cancellation, physical return and earned second batch replay to the final live hash");
            Console.WriteLine("manufacturing accounting replay: initial=" + recording.InitialStep + ";final=" + game.Clock.StepIndex
                + ";commands=" + recording.CommandCount + ";checkpoints=" + recording.CheckpointCount + ";restores=" + restores
                + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
            c.Equal(untouched, idle.Hash(), "another session remains unchanged");
        }
    }
}
