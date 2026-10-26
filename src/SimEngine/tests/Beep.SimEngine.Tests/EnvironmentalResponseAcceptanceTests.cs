using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.LogisticsPolicyContentTests;
using static Beep.SimEngine.Tests.EmergencyResponseAcceptanceTests;

namespace Beep.SimEngine.Tests
{
    // Engine commands/Update and immutable facts only. No environmental ledger or recovery gameplay.
    internal static class EnvironmentalResponseAcceptanceTests
    {
        internal const string Case = "Environment acceptance: physical loss recycling disposal and durable exact rates";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static readonly string[] Metrics = { "environment.loss", "environment.recovered", "environment.disposed", "environment.feed", "environment.recent-loss", "environment.other-kind", "environment.other-cause" };
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(bool halfMinute, bool external = false)
            => PhysicalRecoveryScenario.Create(false, halfMinute: halfMinute, environmentalResponse: true, externalResponse: external);
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 928,
            Mods = { ResourceFlowsModule.ModuleId, EnvironmentalMetricsModule.ModuleId }, Seats = { Controller.Human, Controller.Human } });
        internal static EnvironmentalObservation Metric(SimGame game, string metric)
            => game.Seat(0).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(0), metric);
        internal static void Amount(Check c, SimGame game, string metric, int amount, long facts)
        {
            var value = Metric(game, metric); c.That(value.Available && value.Blocker.IsNone, "native environmental metric available " + metric);
            c.Equal(new BigInteger(Fix.FromInt(amount).Raw), value.AmountRaw ?? -BigInteger.One, "actual classified amount " + metric);
            c.Equal(facts, value.Facts, "once-only fact count " + metric);
        }
        internal static void Balance(Check c, SimGame game)
        {
            var rows = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates;
            foreach (var item in new[] { ("steel", 30), ("recovery.fuel", 200), ("response.waste", 2), ("environment.residue", 0), ("response.spare", 2), ("response.suppressant", 2), ("response.kit", 1) }) {
                Fix stock = Process(game).Wip.Stock.AmountOf(Resource(game, item.Item1));
                foreach (string entity in new[] { "recovery.source", "recovery.destination", "recovery.carrier", "recovery.service", "crane", "response.station", "response.depot" }) stock += Stock(game, entity, item.Item1);
                BigInteger produced = 0, removed = 0;
                foreach (var row in rows.Where(r => r.Resource == Resource(game, item.Item1))) {
                    if (row.Kind == ResourceFlowKind.Produced) produced += row.AmountRaw;
                    if (row.Kind == ResourceFlowKind.Consumed || row.Kind == ResourceFlowKind.Lost || row.Kind == ResourceFlowKind.Destroyed) removed += row.AmountRaw;
                }
                c.Equal(new BigInteger(Fix.FromInt(item.Item2).Raw) + produced, new BigInteger(stock.Raw) + removed, "native physical environmental conservation " + item.Item1);
            }
            c.That(!rows.Any(r => r.Kind == ResourceFlowKind.AuthoredCreation || r.Kind == ResourceFlowKind.StockPurchase), "environmental recovery never grants or purchases stock");
        }
        private static void ReachStage(Check c, SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++) { Step(c, game, twin); Balance(c, game); }
            if (!done()) throw new InvalidOperationException(stage + " exceeded512Updates at=" + game.Clock.StepIndex + "/phases=" + string.Join(";", Phases(game).SelectMany(p => p.Nodes).Select(n => n.Id + "/" + n.State + "/" + n.Blocker)) + "/process=" + Process(game).Phase + "/" + Process(game).Blocker + "/jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.Worker + "/" + j.State + "/" + j.Blocker)));
        }
        private static SimGame Checkpoint(Check c, GameFactory factory, SimGame game)
        {
            ulong hash = game.Hash();
            var twin = Partial(c, factory, game);
            SavedMetrics(c, game, twin);
            c.Equal(hash, game.Hash(), "metric observations and paused Updates change no state"); return twin;
        }
        internal static void SavedMetrics(Check c, SimGame game, SimGame twin)
        {
            var before = Metrics.Select(m => Metric(game, m)).ToArray();
            for (int i = 0; i < Metrics.Length; i++) {
                var now = Metric(twin, Metrics[i]); var old = before[i];
                c.Equal(old.AmountRaw, now.AmountRaw, "saved exact environmental amount " + Metrics[i]); c.Equal(old.Facts, now.Facts, "saved fact count " + Metrics[i]);
                c.Equal(old.Rate, now.Rate, "saved reduced environmental rate " + Metrics[i]); c.Equal(old.Blocker, now.Blocker, "saved environmental availability " + Metrics[i]);
                c.Equal(old.RateBlocker, now.RateBlocker, "saved rate refusal " + Metrics[i]); c.Equal(old.FromTick, now.FromTick, "saved coverage start " + Metrics[i]); c.Equal(old.ThroughTick, now.ThroughTick, "saved coverage end " + Metrics[i]);
            }
        }
        internal static void Rates(Check c, SimGame game, bool halfMinute)
        {
            foreach (var item in new[] { ("environment.loss", 1), ("environment.recovered", 1), ("environment.disposed", 1), ("environment.feed", 2) }) {
                var value = Metric(game, item.Item1); c.That(value.Rate.HasValue && value.RateBlocker.IsNone, "elapsed native rate available " + item.Item1);
                var rate = value.Rate.Value; c.Equal(DurationUnit.Hour, rate.Per, "authored rate unit");
                c.Equal(new BigInteger(item.Item2) * 3600 * rate.Denominator, rate.Numerator * (value.ThroughTick - value.FromTick) * (halfMinute ? 30 : 60), "exact quantity/game-time rational " + item.Item1);
            }
        }
        internal static bool DisposalPartial(SimGame game)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            var order = orders.SingleOrDefault(o => o.Plan == "activity:environment.disposal"); var nodes = new List<ActivityNodeView>();
            return order.Id > 0 && game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, nodes) && nodes.Any(n => n.ProductiveSteps > 0 && n.State != NodeState.Done);
        }
        private static void Run(Check c) { foreach (bool halfMinute in new[] { false, true }) Scenario(c, halfMinute); }
        internal static void Scenario(Check c, bool halfMinute, bool external = false)
        {
            var factory = Factory(halfMinute, external);
            var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); using var recording = game.RecordReplay(Limits);
            c.Equal(external, game.Modules.Contains(Beep.SimEngine.ExtensionProof.ExternalResponseModule.ModuleId), "explicit external module selection");
            c.That(!game.Modules.Contains("sim.tycoon"), "environmental recovery is independent of business");
            foreach (string metric in Metrics) { Amount(c, game, metric, 0, 0); c.That(!Metric(game, metric).Rate.HasValue && Metric(game, metric).RateBlocker == EnvironmentalCodes.NoElapsedTime, "zero-age metric has no fabricated rate"); }
            ulong pure = game.Hash(); var foreign = game.Seat(1).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(0), "environment.loss");
            c.That(!foreign.Available && foreign.Blocker == Codes.NotOwned && !foreign.Unit.IsValid && foreign.ThroughTick == 0, "private environmental amount/unit/time withheld");
            c.Equal(Codes.UnknownDef, Metric(game, "environment.absent").Blocker, "missing metric never substitutes a default"); c.Equal(pure, game.Hash(), "private/unknown previews pure");
            var twin = Input(c, factory, game, new EnqueueTransformationCommand { Transformer = Id(game, "response.station"), Capability = new Code("process"), Transformation = "environment.recovery", Count = 1 });
            c.Equal(Fix.FromInt(2), Process(game).Wip.Stock.AmountOf(Resource(game, "response.waste")), "recycling protects real finite waste WIP"); var execution = Process(game).Wip.Execution;
            twin = Checkpoint(c, factory, game);
            twin = Input(c, factory, game, new RaiseIncidentCommand { Target = Id(game, "response.station"), Source = new Code("failure"), Kind = new Code("response.failure") });
            Amount(c, game, "environment.loss", 0, 0);
            ReachStage(c, game, twin, () => Metric(game, "environment.loss").AmountRaw == new BigInteger(Fix.One.Raw), "positive-time physical loss effect");
            var captured = Metric(game, "environment.loss"); c.That(Complete(game, "isolation"), "loss follows earned physical isolation, not input submission");
            Amount(c, game, "environment.recovered", 0, 0); Amount(c, game, "environment.disposed", 0, 0);
            twin = Checkpoint(c, factory, game); var held = Process(game);
            for (int i = 0; i < 8; i++) { Step(c, game, twin); Balance(c, game); c.Equal(held.Progress, Process(game).Progress, "unsafe paused recycling earns no work"); }
            game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(Id(game, "response.station"), new Code("failure"), out var episode);
            twin = Input(c, factory, game, new CancelOrderCommand { Order = episode.OrderId });
            Amount(c, game, "environment.loss", 1, 1); twin = Checkpoint(c, factory, game);
            twin = Input(c, factory, game, Dispatch(game, "recovery.carrier", "response.spare", 2, 100), Dispatch(game, "recovery.service", "response.suppressant", 2, 90));
            ReachStage(c, game, twin, () => Complete(game, "response") && Complete(game, "cleanup") && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "physical repair and cleanup readiness");
            Amount(c, game, "environment.recovered", 0, 0); Amount(c, game, "environment.disposed", 0, 0); twin = Checkpoint(c, factory, game);
            twin = Input(c, factory, game, Dispatch(game, "recovery.service", "response.kit", 1, 100));
            ReachStage(c, game, twin, () => !Active(game), "physical verified restart"); c.Equal(execution, Process(game).Wip.Execution, "same waste recovery execution resumes"); twin = Checkpoint(c, factory, game);
            ReachStage(c, game, twin, () => Process(game).QueueCount == 0 && !Jobs(game).Any(j => j.Worker.IsValid), "same physical recovery result commits once");
            Amount(c, game, "environment.loss", 1, 1); Amount(c, game, "environment.recovered", 1, 1); Amount(c, game, "environment.feed", 2, 1);
            c.Equal(Fix.One, Stock(game, "response.station", "environment.residue"), "actual conversion residue awaits disposal"); twin = Checkpoint(c, factory, game);
            twin = Input(c, factory, game, new PostOrderCommand { Plan = "environment.disposal", Target = Id(game, "response.station") });
            ReachStage(c, game, twin, () => DisposalPartial(game), "staffed residue disposal");
            Amount(c, game, "environment.disposed", 0, 0); twin = Checkpoint(c, factory, game);
            ReachStage(c, game, twin, () => Metric(game, "environment.disposed").AmountRaw == new BigInteger(Fix.One.Raw) && !Jobs(game).Any(j => j.Worker.IsValid), "positive-time verified residue destruction");
            Amount(c, game, "environment.disposed", 1, 1); Amount(c, game, "environment.other-kind", 0, 0); Amount(c, game, "environment.other-cause", 0, 0);
            Rates(c, game, halfMinute); var summary = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            c.That(summary.EvictedThroughTick >= captured.ThroughTick, "finite retained feed actually evicted original loss");
            c.Equal(EnvironmentalCodes.HistoryExpired, Metric(game, "environment.recent-loss").Blocker, "incomplete window refuses rather than reports fake zero");
            c.Equal(captured.AmountRaw, Metric(game, "environment.loss").AmountRaw, "cumulative loss survives individual eviction"); c.Equal(new BigInteger(Fix.One.Raw), captured.AmountRaw.Value, "captured observations remain detached");
            twin = Checkpoint(c, factory, game); long quiet = game.Clock.StepIndex;
            ReachStage(c, game, twin, () => game.Clock.StepIndex - quiet >= (halfMinute ? 122 : 61), "complete quiet rate window");
            var empty = Metric(game, "environment.recent-loss"); c.That(empty.Available && empty.AmountRaw == BigInteger.Zero && empty.Rate.HasValue && empty.Rate.Value.Numerator.IsZero, "complete quiet window certifies real zero loss and zero rate");
            Rates(c, game, halfMinute); Amount(c, game, "environment.disposed", 1, 1); twin = Checkpoint(c, factory, game);
            c.Equal(Fix.Zero, Stock(game, "response.station", "environment.residue"), "disposal removes only actual produced residue"); c.Equal(Fix.Zero, Stock(game, "response.station", "response.waste"), "only actual protected waste consumed");
            c.That(Process(game).Wip.Stock.IsEmpty && Process(game).Tools.Held.IsEmpty, "original WIP/tools released"); c.Equal(untouched, isolated.Hash(), "independent environmental session untouched");
            foreach (string entity in new[] { "response.station", "response.depot", "recovery.carrier", "recovery.service", "crane" })
                foreach (string resource in new[] { "steel", "response.waste", "environment.residue", "response.spare", "response.suppressant", "response.kit", "recovery.fuel" }) {
                    c.Equal(Code.None, View(game).ReadDiagnostics(Id(game, entity), Resource(game, resource), out var facts), "terminal environmental resource facts");
                    c.Equal(Fix.Zero, facts.OutboundPromised + facts.InboundPromised, "terminal recovery/disposal claims settled");
                }
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), Limits); c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "physical environment replay " + replay.Failure + "/" + replay.Detail);
        }
    }
}
