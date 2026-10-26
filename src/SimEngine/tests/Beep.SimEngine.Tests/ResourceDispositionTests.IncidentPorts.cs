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
using JsonParser = Beep.SimEngine.Content.Json;

namespace Beep.SimEngine.Tests
{
    public static partial class ResourceDispositionTests
    {
        internal const string IncidentPortCase = "Incident effect ports: saved physical supply cancellation and repeated episodes retain original phase ownership";
        private static void IncidentPorts(Check c)
        {
            foreach (bool scoped in new[] { false, true }) foreach (bool custom in new[] { false, true }) {
                var authored = JsonParser.Parse(FrameworkProofContent.Read("work-effect-incident-ports.json"));
                authored.Get("activities")[0].Get("nodes")[0].Set("effect", JsonValue.Of(custom ? "port.dispose-independent" : "port.dispose"));
                PortPhysical(c, scoped, custom, "destroyed", (factory, game, abandoned, site) => IncidentPortContinuation(c, factory, game, site, scoped, custom),
                    edit: root => {
                        root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(IncidentResponseModule.ModuleId));
                        root.Get("scenarios")[0].Get("settings").Get("sim.work").Set("retry", JsonValue.Of("disposition.incident.retry"));
                        root.Get("entities")[1].Get("components").Set("incident", JsonParser.Parse("""[{"id":"damage","workflow":"disposition.incident","states":{"active":"blocked"}},{"id":"alarm","workflow":"disposition.incident","states":{"active":"blocked"}}]"""));
                    }, incidentDocument: authored.ToString());
            }
        }
        // Player-only continuation: no World/provider access, material grants, work edits or host-owned response rules.
        private static void IncidentPortContinuation(Check c, GameFactory factory, SimGame game, EntityId site, bool scoped, bool custom)
        {
            string context = "incident/scoped=" + scoped + "/custom=" + custom;
            string effect = custom ? "port.dispose-independent" : "port.dispose";
            var damage = new Code("damage"); var alarm = new Code("alarm");
            var twin = factory.Load(game.Save()); var idle = factory.Load(game.Save()); ulong isolated = idle.Hash();
            var carrier = game.Seat(0).Snapshot.Entities.Single(row => row.Def == "machine").Id;
            var source = game.Seat(0).Snapshot.Entities.Single(row => row.Def == "source").Id;
            if (!game.Seat(0).View.TryFindResource("supply", out var supply) || !game.Seat(0).View.TryFindResource("spare", out var spare)) throw new InvalidOperationException("authored response resources absent");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            WorkView Work(SimGame session) => session.Seat(0).View.Feature<WorkView>();
            IncidentsView Incidents(SimGame session) => session.Seat(0).View.Feature<IncidentsView>();
            IncidentLifecycleView Life(Code capability) {
                if (!Incidents(game).TryGetLifecycle(site, capability, out var row)) throw new InvalidOperationException("actual incident lifecycle absent: " + context);
                return row;
            }
            ActivityNodeView Node(int order, string id) {
                var rows = new List<ActivityNodeView>();
                if (!Work(game).TryCopyNodes(order, rows)) throw new InvalidOperationException("actual incident node absent: " + context + "/" + order);
                return rows.Single(row => row.Id == id);
            }
            List<WorkJobView> Jobs() { var rows = new List<WorkJobView>(); Work(game).CopyJobs(rows); return rows; }
            LogisticsOperationalSnapshot Stock(ResourceId resource, string port) {
                var refusal = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(site, resource, out var row, new Code(port));
                if (!refusal.IsNone) throw new InvalidOperationException("actual response stock " + context + "/" + refusal);
                return row;
            }
            BigInteger Removed(ResourceId resource) => game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(row => row.Resource == resource && row.Cause == new Code(effect) && row.Kind == ResourceFlowKind.Destroyed)
                .Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw);
            void Observe() {
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("physical incident fault " + context + "/" + game.Fault + "/" + twin.Fault);
                c.Equal(game.Hash(), twin.Hash(), "loaded incident twin agrees after each actual update: " + context);
                foreach (var resource in new[] { supply, spare }) {
                    Fix total = Fix.Zero;
                    foreach (var row in game.Seat(0).Snapshot.Entities)
                        if (game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(row.Id, resource, out var stock).IsNone) total += stock.Stock;
                    if (scoped) total += Stock(resource, resource == supply ? "feed" : "parts").Stock;
                    c.Equal(new BigInteger(Fix.FromInt(resource == supply ? 20 : 10).Raw), new BigInteger(total.Raw) + Removed(resource), "finite physical stock plus classified removal conserved: " + context);
                }
            }
            void Step() { game.Update(); twin.Update(); Observe(); }
            void Reach(Func<bool> ready, string stage) {
                for (int i = 0; i < 180 && !ready(); i++) Step();
                if (!ready()) throw new InvalidOperationException(stage + " exceeded180 updates at " + game.Clock.StepIndex + "/" + context + "; jobs=" + string.Join(";", Jobs().Select(row => row.Id + "/" + row.State + "/" + row.ToilIndex + "/" + row.Blocker)));
            }
            void Send(Command command) {
                var first = TestKit.Order(game, 0, command); var other = TestKit.Order(twin, 0, command);
                c.That(first.Accepted && other.Accepted, "ordinary response intent accepted: " + command.GetType().Name + "/" + first.Refusal + "/" + context);
                if (!first.Accepted || !other.Accepted) throw new InvalidOperationException("response command refused " + first.Refusal + "/" + other.Refusal);
                Observe();
            }
            void Save() { game = factory.Load(game.Save()); twin = factory.Load(game.Save()); Observe(); }
            void Haul(ResourceId resource, string port, Fix amount) => Send(new HaulResourcesCommand {
                Source = source, Destination = site, SourcePort = new Code("stock"), DestinationPort = new Code(port), Resource = resource, Ceiling = amount, Worker = carrier, Priority = 99 });
            var plans = new List<PlanView>(); Work(game).CopyPlans(plans);
            Send(new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { plans.Single().Id }) });
            c.Equal(BigInteger.Zero, Removed(supply), "cancelled ordinary partially earned plan destroys no supplied material: " + context);
            c.That(Stock(supply, "feed").Stock == Fix.FromInt(2) && Stock(spare, "parts").Stock == Fix.One, "actual supplied bill survives player cancellation: " + context);
            Save(); Send(new RaiseIncidentCommand { Target = site, Source = damage, Kind = new Code("proof.disposition") });
            int firstOrder = Life(damage).OrderId;
            c.That(firstOrder > 0 && Life(damage).Snapshot.Active && Life(damage).Snapshot.Episode == 1, "incident module owns first response identity and episode: " + context);
            c.That(!Incidents(game).TryGetResponseOrder(site, alarm, out _) && !Life(alarm).Snapshot.Active, "other named provider is not accidentally enrolled: " + context);
            c.That(!game.Seat(1).View.Feature<IncidentsView>().TryGetLifecycle(site, damage, out _), "response lifecycle is company-private: " + context);
            ulong pure = game.Hash();
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new RaiseIncidentCommand { Target = site, Source = damage, Kind = new Code("proof.disposition") }), "active episode cannot be raised twice: " + context);
            c.Equal(pure, game.Hash(), "duplicate raise preview preserves actual episode: " + context);
            Reach(() => Node(firstOrder, "cleanup").Progress.WholeTicks > 0 && Node(firstOrder, "cleanup").Progress.WholeTicks < 4, "response machine really travels and earns partial cleanup");
            var partial = Node(firstOrder, "cleanup"); Save();
            using (var recording = game.RecordReplay(limits)) {
                Send(new CancelOrderCommand { Order = firstOrder });
                c.That(Life(damage).OrderId == firstOrder && Node(firstOrder, "cleanup").Progress.WholeTicks >= partial.Progress.WholeTicks, "automatic response resumes same episode/order without resetting earned work: " + context);
                c.Equal(BigInteger.Zero, Removed(supply), "pre-effect cancellation cannot spend a partial bill: " + context);
                Reach(() => Node(firstOrder, "cleanup").State == NodeState.Done && Node(firstOrder, "restart").Progress.WholeTicks > 0 && Node(firstOrder, "restart").Progress.WholeTicks < 3, "cleanup completes before positive-time restart");
                c.Equal(new BigInteger(Fix.FromInt(2).Raw), Removed(supply), "earned cleanup destroys exactly original first bill: " + context);
                Send(new CancelOrderCommand { Order = firstOrder });
                c.That(Life(damage).OrderId == firstOrder && Node(firstOrder, "cleanup").State == NodeState.Done && Node(firstOrder, "cleanup").Progress.WholeTicks == 4, "cancelling restart cannot repeat the completed cleanup/effect: " + context);
                c.Equal(new BigInteger(Fix.FromInt(2).Raw), Removed(supply), "post-effect cancellation neither refunds nor repeats disposition: " + context);
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "saved restarted response retains original completed phase: " + context);
                Reach(() => !Life(damage).Snapshot.Active, "bound response completion resolves actual episode");
                for (int i = 0; i < 8; i++) Step();
                c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "actual partial response and both cancellation commands replay: " + context);
            }
            c.That(Life(damage).OrderState == OrderState.Done && Life(damage).Snapshot.ResolvedTick >= Life(damage).Snapshot.RaisedTick + 7, "episode resolution follows authored cleanup and restart time: " + context);
            c.Equal(new BigInteger(Fix.One.Raw), Removed(spare), "first complete episode spends exact second resource once: " + context);
            Save(); Send(new RaiseIncidentCommand { Target = site, Source = damage, Kind = new Code("proof.disposition") });
            int secondOrder = Life(damage).OrderId;
            c.That(secondOrder > firstOrder && Life(damage).Snapshot.Episode == 2, "next actual episode has its own monotonically allocated execution: " + context);
            Reach(() => Node(secondOrder, "cleanup").State == NodeState.Completing, "unsupplied cleanup reaches typed effect wait without false resolution");
            c.Equal(WorkEffectCodes.NoStock, Node(secondOrder, "cleanup").Blocker, "missing whole effect bill is a typed authored retry: " + context);
            c.That(Life(damage).Snapshot.Active && Node(secondOrder, "restart").Progress.WholeTicks == 0, "dependent restart cannot bypass missing physical supplies: " + context);
            Save();
            using (var recording = game.RecordReplay(limits)) {
                Haul(supply, "feed", Fix.FromInt(2));
                Reach(() => Stock(supply, "feed").Stock == Fix.FromInt(2) && !Jobs().Any(row => row.IsHaul) && Work(game).ReadHome(carrier, out var home).IsNone && home.State == WorkerHomeState.AtHome, "first refill really unloads and carrier returns");
                c.That(Stock(supply, "feed").OutboundPromised.IsZero && Life(damage).Snapshot.Active, "available first material is not claimed while second is missing: " + context);
                c.Equal(new BigInteger(Fix.FromInt(2).Raw), Removed(supply), "short supply causes no partial second disposition: " + context);
                Haul(spare, "parts", Fix.One);
                Reach(() => !Life(damage).Snapshot.Active, "physically supplied second episode resumes original pending effect and restart");
                for (int i = 0; i < 8; i++) Step();
                c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "actual saved effect wait refill and repeated episode completion replay: " + context);
            }
            c.Equal(new BigInteger(Fix.FromInt(4).Raw), Removed(supply), "two real episodes destroy only four supplied units: " + context);
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), Removed(spare), "two real episodes destroy only two supplied spares: " + context);
            var phases = new List<IncidentPhaseView>(); c.That(Incidents(game).CopyPhases(site, damage, phases) && phases.Count == 2 && phases.All(row => row.Complete), "public phase views read the same completed DAG: " + context);
            c.That(Life(damage).OrderId == secondOrder && Life(damage).OrderState == OrderState.Done && Life(alarm).Snapshot.Episode == 0, "terminal history names current response without touching other source: " + context);
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "repeated episode and classified endpoint facts save canonically: " + context);
            c.Equal(isolated, idle.Hash(), "all incident work leaves unrelated loaded session unchanged: " + context);
        }
    }
}
