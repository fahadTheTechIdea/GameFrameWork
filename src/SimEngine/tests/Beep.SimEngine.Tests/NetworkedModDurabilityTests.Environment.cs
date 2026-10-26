using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Environment = Beep.SimEngine.Tests.EnvironmentalResponseAcceptanceTests;
using Response = Beep.SimEngine.Tests.EmergencyResponseAcceptanceTests;
using Logistics = Beep.SimEngine.Tests.LogisticsPolicyContentTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string EnvironmentCase = "Networked environment: interrupted recycling, physical emergency response and exact disposal history";
        internal const string ExternalEnvironmentCase = "Networked external environment: half-minute response contracts, saved recycling and exact disposal history";
        private static string EnvironmentDiagnostics(SimGame game) => "at=" + game.Clock.StepIndex + "/process=" + Response.Process(game).Phase +
            "/progress=" + Response.Process(game).Progress + "/blocker=" + Response.Process(game).Blocker + "/incident=" + Response.Active(game) +
            "/phases=" + string.Join(";", Response.Phases(game).SelectMany(p => p.Nodes).Select(n => n.Id + "/" + n.State + "/" + n.Blocker)) +
            "/jobs=" + string.Join(";", Logistics.Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.DockWaiting));
        private static Pair EnvironmentPair(Check c, SimGame a, SimGame b)
        {
            // Observation bookkeeping only: check each actual committed epoch once,
            // not repeatedly during transport stalls. No state/decision is cached for gameplay.
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long prior) && prior == game.Clock.StepIndex) return;
                Environment.Balance(c, game);
                var foreign = game.Seat(1).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(0), "environment.loss");
                if (foreign.Available || foreign.Blocker != Codes.NotOwned || foreign.Unit.IsValid || foreign.AmountRaw.HasValue || foreign.ThroughTick != 0)
                    throw new InvalidOperationException("private environmental flow/unit/time leaked");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, EnvironmentDiagnostics, 2);
        }
        private static Pair EnvironmentRestore(Check c, GameFactory factory, Pair source)
        {
            var a = factory.Load(source.A.Save()); var b = factory.Load(source.B.Save());
            c.Equal(source.A.Hash(), a.Hash(), "saved native process/work/flow owners restore exactly");
            c.Equal(source.B.Hash(), b.Hash(), "saved peer restores exactly");
            Environment.SavedMetrics(c, source.A, a); Environment.SavedMetrics(c, source.B, b);
            return EnvironmentPair(c, a, b);
        }
        private static bool EnvironmentFree(SimGame game) => !Logistics.Jobs(game).Any(job => job.Worker.IsValid);
        private static bool EnvironmentNoHaul(SimGame game) => !Logistics.Jobs(game).Any(job => job.IsHaul && job.Worker.IsValid);
        private static bool EnvironmentAmount(SimGame game, string metric, int amount)
            => Environment.Metric(game, metric).AmountRaw == new BigInteger(Fix.FromInt(amount).Raw);
        private static EnqueueTransformationCommand EnvironmentRecycle(SimGame game) => new EnqueueTransformationCommand {
            Transformer = Logistics.Id(game, "response.station"), Capability = new Code("process"), Transformation = "environment.recovery", Count = 1 };
        private static void EnvironmentRun(Check c, bool external)
        {
            bool half = external; var factory = Environment.Factory(half, external);
            var pair = EnvironmentPair(c, Environment.New(factory), Environment.New(factory));
            Console.WriteLine("MODULES environment=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId), "actual recycling and response need no business module");
            c.Equal(external, pair.A.Modules.Contains(Beep.SimEngine.ExtensionProof.ExternalResponseModule.ModuleId), "explicit independent response/metric content selection");
            foreach (string metric in Environment.Metrics) {
                Environment.Amount(c, pair.A, metric, 0, 0);
                c.That(!Environment.Metric(pair.A, metric).Rate.HasValue && Environment.Metric(pair.A, metric).RateBlocker == EnvironmentalCodes.NoElapsedTime,
                    "initial exact environmental rates cannot invent elapsed time");
            }
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int recycling = Submit(c, pair, EnvironmentRecycle(pair.A));
            int foreign = Submit(c, pair, EnvironmentRecycle(pair.B), Codes.NotFound, seat: 1);
            c.Equal(0L, left.CommandCount, "unadmitted network proposals are not canonical engine inputs");
            Reach(pair, game => Result(game, 0, recycling)?.Accepted == true && Result(game, 1, foreign)?.Refusal == Codes.NotFound &&
                Response.Process(game).Wip.Stock.AmountOf(Response.Resource(game, "response.waste")) == Fix.FromInt(2) &&
                Response.Process(game).Progress.WholeTicks > 0, "actual productive finite-waste recycling and private refusal");
            var working = Response.Process(pair.A); long execution = working.Wip.Execution;
            c.Equal(half ? 240L : 120L, working.Required, "unchanged two-hour work follows sole minute/half-minute game quantum");
            c.That(working.Progress.WholeTicks < working.Required, "real protected waste is not already a completed result");
            var restored = EnvironmentRestore(c, factory, pair);
            var partialLeft = restored.A.RecordReplay(limits); var partialRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new RaiseIncidentCommand {
                Target = Logistics.Id(current.A, "response.station"), Source = new Code("failure"), Kind = new Code("response.failure") });
            Both(pair, restored, game => Response.Active(game) && !Response.Complete(game, "isolation"), "normal incident still requires physical isolation");
            Environment.Amount(c, pair.A, "environment.loss", 0, 0);
            Both(pair, restored, game => EnvironmentAmount(game, "environment.loss", 1) && Response.Process(game).Blocker == new Code("response.isolated"),
                "earned physical isolation commits one loss and the next entity update settles its safety gate");
            var captured = Environment.Metric(pair.A, "environment.loss"); var held = Response.Process(pair.A);
            c.That(Response.Complete(pair.A, "isolation") && held.Blocker == new Code("response.isolated") && held.Wip.Execution == execution,
                "original safety gate holds the same productive execution");
            Environment.Amount(c, pair.A, "environment.recovered", 0, 0); Environment.Amount(c, pair.A, "environment.disposed", 0, 0);
            ReplayPair(c, factory, partialLeft, partialRight, restored, limits, 1, "productive-recycling to earned-isolation-loss replay");
            restored = EnvironmentRestore(c, factory, pair);
            var blockedLeft = restored.A.RecordReplay(limits); var blockedRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native network pause while recycling is isolated");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 8, "other actual saved peers advance the unsupplied isolated episode");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen, "paused originals borrow no age, waste or incident progress");
            c.Equal(held.Progress, Response.Process(restored.A).Progress, "unsafe isolated recycling earns no work while another session advances");
            pair.A.Net.RequestResume(); Both(pair, restored, game => Response.Active(game) && Response.Complete(game, "isolation"), "native resume converges at actual common epoch");
            int originalOrder = Response.Lifecycle(pair.A).OrderId;
            int cancellation = Submit(c, pair, new CancelOrderCommand { Order = originalOrder });
            c.Equal(cancellation, Submit(c, restored, new CancelOrderCommand { Order = originalOrder }), "saved cancellation preserves the same native player sequence");
            Both(pair, restored, game => Result(game, 0, cancellation)?.Accepted == true && Response.Lifecycle(game).OrderId == originalOrder &&
                Response.Lifecycle(game).OrderState == OrderState.Active && Response.Complete(game, "isolation"),
                "ordinary cancellation resumes the SAME original order and its completed-phase effects");
            Environment.Amount(c, pair.A, "environment.loss", 1, 1);
            foreach (var current in new[] { pair, restored }) {
                Submit(c, current, Response.Dispatch(current.A, "recovery.carrier", "response.spare", 2, 100));
                Submit(c, current, Response.Dispatch(current.A, "recovery.service", "response.suppressant", 2, 90));
            }
            bool PartialHaul(SimGame game) => Logistics.Jobs(game).Any(job => job.IsHaul && job.Worker == Logistics.Id(game, "recovery.carrier") &&
                job.Loaded > Fix.Zero && job.Loaded < Fix.FromInt(2)) && Logistics.Jobs(game).Any(job => job.IsHaul && job.DockWaiting);
            Both(pair, restored, PartialHaul, "physical emergency supplies partially load while another carrier waits at the real dock");
            ReplayPair(c, factory, blockedLeft, blockedRight, restored, limits, 3, "isolated-loss to cancelled-response and partial-supply replay");
            restored = EnvironmentRestore(c, factory, pair);
            var haulLeft = restored.A.RecordReplay(limits); var haulRight = restored.B.RecordReplay(limits);
            c.That(PartialHaul(restored.A), "saved source queue and partial cargo remain actual unfinished logistics");
            Both(pair, restored, game => Response.Node(game, "repair").ProductiveSteps > 0 && Response.Node(game, "cleanup").ProductiveSteps > 0 &&
                !Response.Complete(game, "response") && !Response.Complete(game, "cleanup"), "real supplied parallel machine repair and cleanup");
            c.Equal(half ? 24L : 12L, Response.Node(pair.A, "repair").RequiredTicks, "original twelve-minute repair retains authored duration");
            ReplayPair(c, factory, haulLeft, haulRight, restored, limits, 0, "partial-cargo to parallel-productive-response replay");
            restored = EnvironmentRestore(c, factory, pair);
            var responseLeft = restored.A.RecordReplay(limits); var responseRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => Response.Complete(game, "response") && Response.Complete(game, "cleanup") && EnvironmentNoHaul(game),
                "parallel physical repair, cleanup and carrier departures complete");
            c.That(Response.Active(pair.A) && !Response.Complete(pair.A, "restart") && Response.Node(pair.A, "restart").ProductiveSteps == 0 &&
                !Response.Node(pair.A, "restart").Blocker.IsNone, "repair alone cannot fabricate the missing verification kit or restart");
            c.Equal(held.Progress, Response.Process(pair.A).Progress, "long physical response retains original interrupted recycling work");
            foreach (var current in new[] { pair, restored }) Submit(c, current, Response.Dispatch(current.A, "recovery.service", "response.kit", 1, 100));
            Both(pair, restored, game => Response.Stock(game, "recovery.service", "response.kit") == Fix.One && Response.Active(game), "verification kit is physically aboard before restart");
            ReplayPair(c, factory, responseLeft, responseRight, restored, limits, 1, "parallel-response to physical-verification-kit replay");
            restored = EnvironmentRestore(c, factory, pair);
            var restartLeft = restored.A.RecordReplay(limits); var restartRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => !Response.Active(game), "actual supplied timed verified restart resolves the episode");
            c.Equal(execution, Response.Process(pair.A).Wip.Execution, "same real protected-waste execution resumes");
            long halfWork = working.Required / 2;
            Both(pair, restored, game => Response.Process(game).Progress.WholeTicks >= halfWork, "remaining physical recycling earns its first half");
            Both(pair, restored, game => Response.Process(game).QueueCount == 0 && EnvironmentFree(game), "same physical recycling completes once and releases machines");
            Environment.Amount(c, pair.A, "environment.loss", 1, 1); Environment.Amount(c, pair.A, "environment.recovered", 1, 1);
            Environment.Amount(c, pair.A, "environment.feed", 2, 1);
            c.Equal(Fix.One, Response.Stock(pair.A, "response.station", "environment.residue"), "real recycling residue still requires separate disposal");
            ReplayPair(c, factory, restartLeft, restartRight, restored, limits, 0, "aboard-kit to verified-restart and finite-recycling replay");
            restored = EnvironmentRestore(c, factory, pair);
            var disposalLeft = restored.A.RecordReplay(limits); var disposalRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new PostOrderCommand {
                Plan = "environment.disposal", Target = Logistics.Id(current.A, "response.station") });
            Both(pair, restored, Environment.DisposalPartial, "real fuel-consuming eight-minute staffed residue disposal earns partial work");
            Environment.Amount(c, pair.A, "environment.disposed", 0, 0);
            ReplayPair(c, factory, disposalLeft, disposalRight, restored, limits, 1, "produced-residue to partial-disposal replay");
            restored = EnvironmentRestore(c, factory, pair);
            var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            c.That(Environment.DisposalPartial(restored.A), "load keeps actual earned disposal work, not an instant destruction");
            Both(pair, restored, game => EnvironmentAmount(game, "environment.disposed", 1) && EnvironmentFree(game), "earned disposal destroys actual residue and releases worker");
            Environment.Amount(c, pair.A, "environment.disposed", 1, 1); Environment.Amount(c, pair.A, "environment.other-kind", 0, 0);
            Environment.Amount(c, pair.A, "environment.other-cause", 0, 0); Environment.Rates(c, pair.A, half);
            var summary = pair.A.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            c.That(summary.EvictedThroughTick >= captured.ThroughTick, "finite retained history actually evicts the original loss fact");
            c.Equal(EnvironmentalCodes.HistoryExpired, Environment.Metric(pair.A, "environment.recent-loss").Blocker, "incomplete rolling history cannot fabricate zero pollution");
            c.Equal(captured.AmountRaw, Environment.Metric(pair.A, "environment.loss").AmountRaw, "original cumulative loss survives individual history eviction");
            long quiet = pair.A.Clock.StepIndex;
            Both(pair, restored, game => game.Clock.StepIndex - quiet >= (half ? 122 : 61), "complete quiet hour earns a verifiable zero-loss window");
            var empty = Environment.Metric(pair.A, "environment.recent-loss");
            c.That(empty.Available && empty.AmountRaw == BigInteger.Zero && empty.Rate.HasValue && empty.Rate.Value.Numerator.IsZero,
                "a fully observed quiet window certifies exact zero amount and rate");
            Environment.Rates(c, pair.A, half);
            c.That(working.Wip.Stock.AmountOf(Response.Resource(pair.A, "response.waste")) == Fix.FromInt(2) &&
                captured.AmountRaw == new BigInteger(Fix.One.Raw), "old real WIP and environmental observations stay detached");
            c.That(Response.Process(pair.A).Wip.Stock.IsEmpty && Response.Process(pair.A).Tools.Held.IsEmpty &&
                Response.Stock(pair.A, "response.station", "environment.residue").IsZero && Response.Stock(pair.A, "response.station", "response.waste").IsZero,
                "original finite waste and residue settle without copied WIP or held tools");
            foreach (string entity in new[] { "response.station", "response.depot", "recovery.carrier", "recovery.service", "crane" })
                foreach (string resource in new[] { "steel", "response.waste", "environment.residue", "response.spare", "response.suppressant", "response.kit", "recovery.fuel" }) {
                    c.Equal(Code.None, Logistics.View(pair.A).ReadDiagnostics(Logistics.Id(pair.A, entity), Response.Resource(pair.A, resource), out var facts), "actual terminal resource diagnostics");
                    c.Equal(Fix.Zero, facts.OutboundPromised + facts.InboundPromised, "all real environmental response/disposal promises settle");
                }
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "original and restored native transports actually stall and reorder bytes");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 0, "partial-disposal through finite-history and quiet-rate replay");
            ReplayPair(c, factory, left, right, pair, limits, 8, "full interrupted-physical-environment sequence replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced environmental sessions converge");
            EnvironmentRestore(c, factory, pair);
        }
    }
}
