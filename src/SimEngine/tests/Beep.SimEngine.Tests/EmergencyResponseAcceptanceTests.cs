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

namespace Beep.SimEngine.Tests
{
    // Player commands and detached native observations only; no response execution here.
    internal static class EmergencyResponseAcceptanceTests
    {
        internal const string Case = "Emergency response acceptance: physical phases materials fuel shutdown and verified restart";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static ResourceId Resource(SimGame game, string name)
        { if (!game.Seat(0).View.TryFindResource(name, out var id)) throw new InvalidOperationException("missing response resource " + name); return id; }
        internal static Fix Stock(SimGame game, string entity, string resource)
        { var result = View(game).ReadDiagnostics(Id(game, entity), Resource(game, resource), out var facts); if (!result.IsNone) throw new InvalidOperationException("response stock " + entity + "/" + result); return facts.Stock; }
        internal static List<IncidentPhaseView> Phases(SimGame game)
        { var rows = new List<IncidentPhaseView>(); if (!game.Seat(0).View.Feature<IncidentsView>().CopyPhases(Id(game, "response.station"), new Code("failure"), rows)) throw new InvalidOperationException("missing response phases"); return rows; }
        internal static ActivityNodeView Node(SimGame game, string id) => Phases(game).SelectMany(p => p.Nodes).Single(n => n.Id == id);
        internal static bool Complete(SimGame game, string phase) => Phases(game).Single(p => p.Id == phase).Complete;
        internal static IncidentLifecycleView Lifecycle(SimGame game)
        { if (!game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(Id(game, "response.station"), new Code("failure"), out var facts)) throw new InvalidOperationException("missing incident lifecycle"); return facts; }
        internal static bool Active(SimGame game) => Lifecycle(game).Snapshot.Active;
        internal static TransformationSnapshot Process(SimGame game)
        { if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Id(game, "response.station"), new Code("process"), out var facts)) throw new InvalidOperationException("missing response operation"); return facts; }
        private static WorkJobView Haul(SimGame game, int id) => Jobs(game).Single(j => j.Id == id);
        internal static void Integrity(Check c, SimGame game, int episodes = 1, int fuel = 200)
        {
            var journal = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            foreach (var item in new[] { ("steel", 30), ("recovery.fuel", fuel), ("response.spare", 2 * episodes), ("response.suppressant", 2 * episodes), ("response.waste", episodes), ("response.kit", 1) }) {
                Fix stock = Fix.Zero;
                foreach (string entity in new[] { "recovery.source", "recovery.destination", "recovery.carrier", "recovery.service", "crane", "response.station", "response.depot" }) stock += Stock(game, entity, item.Item1);
                stock += Process(game).Wip.Stock.AmountOf(Resource(game, item.Item1));
                var consumed = journal.Aggregates.Where(a => a.Resource == Resource(game, item.Item1) && a.Kind == ResourceFlowKind.Consumed).Aggregate(BigInteger.Zero, (sum, a) => sum + a.AmountRaw);
                c.Equal(new BigInteger(Fix.FromInt(item.Item2).Raw), new BigInteger(stock.Raw) + consumed, "physical response conservation " + item.Item1);
            }
            c.That(!journal.Aggregates.Any(a => a.Kind == ResourceFlowKind.StockPurchase || a.Kind == ResourceFlowKind.AuthoredCreation), "emergency never grants supplies or invokes business");
            var workers = Jobs(game).Where(j => j.Worker.IsValid).Select(j => j.Worker).ToList();
            c.Equal(workers.Count, workers.Distinct().Count(), "ordinary and emergency jobs never double-assign machines");
        }
        internal static void Reach(Check c, SimGame game, SimGame twin, Func<bool> ready, string stage, int episodes = 1, int fuel = 200)
        {
            for (int i = 0; i < 512 && !ready(); i++) { Step(c, game, twin); Integrity(c, game, episodes, fuel); }
            if (!ready()) throw new InvalidOperationException(stage + " exceeded512Updates at=" + game.Clock.StepIndex + "/crane-fuel=" + Stock(game, "crane", "recovery.fuel") + "/phases=" + string.Join(";", Phases(game).SelectMany(p => p.Nodes).Select(n => n.Id + "/" + n.State + "/" + n.Progress + "/" + n.Blocker)) + "/jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.Worker + "/" + j.State + "/" + j.Blocker + "/toil=" + j.ToilIndex + "/" + j.Loaded + "/" + j.DockWaiting)));
        }
        internal static HaulResourcesCommand Dispatch(SimGame game, string worker, string resource, int quantity, int priority) => new HaulResourcesCommand {
            Source = Id(game, "response.depot"), Destination = Id(game, "response.station"), Worker = Id(game, worker), Resource = Resource(game, resource), Ceiling = Fix.FromInt(quantity), Priority = priority };
        private static void Run(Check c)
        { foreach (bool halfMinute in new[] { false, true }) Scenario(c, halfMinute); }
        private static void Scenario(Check c, bool halfMinute)
        {
            var factory = PhysicalRecoveryScenario.Create(false, halfMinute: halfMinute, emergencyResponse: true);
            SimGame NewSession() => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 921, Mods = { ResourceFlowsModule.ModuleId }, Seats = { Controller.Human, Controller.Human } });
            var game = NewSession(); var isolated = NewSession(); ulong untouched = isolated.Hash(); using var recording = game.RecordReplay(Limits);
            c.That(!game.Modules.Contains("sim.tycoon"), "incident response and production are not business dependent");
            var twin = Input(c, factory, game, new EnqueueTransformationCommand { Transformer = Id(game, "response.station"), Capability = new Code("process"), Transformation = "response.batch", Count = 1 });
            c.Equal(Fix.FromInt(2), Process(game).Wip.Stock.AmountOf(Resource(game, "steel")), "running batch protects real inputs");
            c.Equal(Fix.One, Process(game).Tools.Held.AmountOf(Resource(game, "steel")), "running batch holds reusable tool");
            twin = Partial(c, factory, game);
            var raise = new RaiseIncidentCommand { Target = Id(game, "response.station"), Source = new Code("failure"), Kind = new Code("response.failure") };
            ulong before = game.Hash(); c.Equal(Codes.NotOwned, game.Seat(1).Preview(raise), "foreign emergency command refused"); c.Equal(before, game.Hash(), "incident preview is pure");
            twin = Input(c, factory, game, raise);
            Reach(c, game, twin, () => Node(game, "detect").Progress.WholeTicks > 0 && !Complete(game, "detection"), "timed detection"); twin = Partial(c, factory, game);
            Reach(c, game, twin, () => Node(game, "isolate").ProductiveSteps > 0 && !Complete(game, "isolation"), "physical isolation"); twin = Partial(c, factory, game);
            c.That(Jobs(game).Where(j => !j.IsHaul && j.Target == Id(game, "response.station")).All(j => j.Emergency), "response uses ordinary native emergency jobs");
            c.That(!game.Seat(1).View.Feature<IncidentsView>().TryGetLifecycle(Id(game, "response.station"), new Code("failure"), out _), "foreign incident facts withheld");
            Reach(c, game, twin, () => Complete(game, "isolation") && Process(game).Blocker == new Code("response.isolated"), "isolation freezes unsafe production");
            var held = Process(game); twin = Partial(c, factory, game);
            for (int i = 0; i < 8; i++) { Step(c, game, twin); Integrity(c, game); c.Equal(held.Progress, Process(game).Progress, "isolated operation earns no work"); }
            c.Equal(0L, Node(game, "stabilize").ProductiveSteps, "missing physical suppressant prevents stabilization"); c.That(!Node(game, "stabilize").Blocker.IsNone, "actual missing-supply blocker visible");
            twin = Input(c, factory, game, Dispatch(game, "recovery.carrier", "response.spare", 2, 100), Dispatch(game, "recovery.service", "response.suppressant", 2, 90));
            int primary = Jobs(game).Single(j => j.IsHaul && j.Worker == Id(game, "recovery.carrier")).Id;
            int secondary = Jobs(game).Single(j => j.IsHaul && j.Worker == Id(game, "recovery.service")).Id;
            Reach(c, game, twin, () => Haul(game, secondary).DockWaiting && Haul(game, primary).Loaded > Fix.Zero && Haul(game, primary).Loaded < Fix.FromInt(2), "source bay queue and partial supply loading");
            c.Equal(Fix.Zero, Haul(game, secondary).Loaded, "queued supply vehicle cannot load through occupied bay"); twin = Partial(c, factory, game);
            Reach(c, game, twin, () => Node(game, "stabilize").ProductiveSteps > 0 && !Node(game, "stabilize").State.Equals(NodeState.Done), "physically supplied stabilization"); twin = Partial(c, factory, game);
            Reach(c, game, twin, () => Node(game, "repair").ProductiveSteps > 0 && Node(game, "cleanup").ProductiveSteps > 0 && !Complete(game, "response") && !Complete(game, "cleanup"), "parallel staffed repair and cleanup");
            c.Equal(halfMinute ? 24L : 12L, Node(game, "repair").RequiredTicks, "authored twelve-minute repair follows sole clock"); twin = Partial(c, factory, game);
            Reach(c, game, twin, () => Complete(game, "response") && Complete(game, "cleanup"), "repair and cleanup complete physically");
            c.That(Active(game) && !Complete(game, "restart"), "completed repairs alone do not restart operation"); twin = Partial(c, factory, game);
            for (int i = 0; i < 8; i++) { Step(c, game, twin); Integrity(c, game); c.Equal(held.Progress, Process(game).Progress, "readiness wait preserves isolated WIP"); }
            c.Equal(0L, Node(game, "restart").ProductiveSteps, "missing verification kit blocks restart regardless of elapsed time"); c.That(!Node(game, "restart").Blocker.IsNone, "readiness blocker visible from shared DAG");
            Reach(c, game, twin, () => !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "initial physical supplies and departures settle");
            twin = Input(c, factory, game, Dispatch(game, "recovery.service", "response.kit", 1, 100));
            Reach(c, game, twin, () => Stock(game, "recovery.service", "response.kit") == Fix.One, "verification kit physically aboard"); twin = Partial(c, factory, game);
            Reach(c, game, twin, () => Node(game, "restart").ProductiveSteps > 0 && Active(game), "staffed verified restart"); twin = Partial(c, factory, game);
            Reach(c, game, twin, () => !Active(game), "full response resolves current episode");
            c.That(Phases(game).All(p => p.Complete), "all five native phases complete");
            c.Equal(held.Wip.Execution, Process(game).Wip.Execution, "restart retains original processing execution");
            Reach(c, game, twin, () => Process(game).QueueCount == 0 && !Jobs(game).Any(j => j.Worker.IsValid), "same batch finishes once and all physical jobs depart");
            c.Equal(Fix.One, Stock(game, "response.station", "response.product"), "once-only resumed product"); c.Equal(Fix.One, Stock(game, "response.station", "response.kit"), "verification kit is released not destroyed");
            c.That(Process(game).Wip.Stock.IsEmpty && Process(game).Tools.Held.IsEmpty, "processing completion releases original WIP and tool"); twin = Partial(c, factory, game);
            var flows = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            foreach (var item in new[] { ("steel", 2), ("response.spare", 2), ("response.suppressant", 2), ("response.waste", 1), ("response.kit", 0) }) {
                var consumed = flows.Aggregates.Where(a => a.Resource == Resource(game, item.Item1) && a.Kind == ResourceFlowKind.Consumed).Aggregate(BigInteger.Zero, (sum, a) => sum + a.AmountRaw);
                c.Equal(new BigInteger(Fix.FromInt(item.Item2).Raw), consumed, "actual workflow consumption, not just shortage gating " + item.Item1);
            }
            var produced = flows.Aggregates.Where(a => a.Resource == Resource(game, "response.product") && a.Kind == ResourceFlowKind.Produced).Aggregate(BigInteger.Zero, (sum, a) => sum + a.AmountRaw);
            c.Equal(new BigInteger(Fix.One.Raw), produced, "original resource journal records exactly one resumed result");
            foreach (string entity in new[] { "response.station", "response.depot", "recovery.carrier", "recovery.service" })
                foreach (string resource in new[] { "steel", "response.spare", "response.suppressant", "response.kit" }) {
                    c.Equal(Code.None, View(game).ReadDiagnostics(Id(game, entity), Resource(game, resource), out var facts), "terminal native resource facts");
                    c.Equal(Fix.Zero, facts.OutboundPromised + facts.InboundPromised, "terminal response and haul claims release completely");
                }
            for (int i = 0; i < 12; i++) { Step(c, game, twin); Integrity(c, game); }
            c.Equal(Fix.One, Stock(game, "response.station", "response.product"), "terminal Updates never repeat production or incident completion");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal response state restores exactly"); c.Equal(untouched, isolated.Hash(), "independent session remains untouched");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), Limits); c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "full physical emergency replay " + replay.Failure + "/" + replay.Detail);
        }
    }
}
