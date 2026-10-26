using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.LogisticsPolicyContentTests;

namespace Beep.SimEngine.Tests
{
    // Player intent and detached facts only; all admission, ranking, claims and timed execution stay native.
    internal static class AllDispatchPolicyAcceptanceTests
    {
        internal const string Case = "Dispatch policy acceptance: every physical dispatch path shares area reserve capacity and permission authority";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static List<WorkerDispatchOffer> Offers(SimGame game)
        { var rows = new List<WorkerDispatchOffer>(); game.Seat(0).View.Feature<WorkView>().CopyAssignments(rows); return rows; }
        private static List<LogisticsDispatchOffer> Queue(SimGame game)
        { var rows = new List<LogisticsDispatchOffer>(); View(game).CopyQueue(rows); return rows; }
        private static bool Done(SimGame game, string activity)
            => game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:" + activity, Id(game, "destination"), out _);
        private static ConfigureWorkPolicyCommand Policy(SimGame game, string area, bool enabled = true, bool emergency = true, bool haul = true, bool inspect = true)
            => new ConfigureWorkPolicyCommand { Worker = Id(game, "machine"), Settings = new WorkPolicySettings(enabled, 0,
                (haul ? new[] { new WorkerRoleId("haul") } : Array.Empty<WorkerRoleId>()).Concat(inspect ? new[] { new WorkerRoleId("inspect") } : Array.Empty<WorkerRoleId>()), area, true, emergency, WorkAssignmentMode.All) };
        private static ConfigureCompanyAlertCommand Floor(bool inspect = false)
            => new ConfigureCompanyAlertCommand { Company = new PlayerId(0), Settings = new CompanyAlertSettings("alerts.standard", CompanyAlertLevel.Normal,
                new[] { new WorkerReserveSetting(new WorkerRoleId(inspect ? "inspect" : "haul"), 1) }) };
        private static ConfigureOutletPolicyCommand Export(SimGame game, string origin, int reserve, bool enabled = true, string destination = "destination", bool release = false)
            => new ConfigureOutletPolicyCommand { Source = Id(game, origin), Change = OutletPolicyChange.Replace(new OutletPolicySettings(enabled,
                reserve == 0 ? Bill.Empty : new Bill(new[] { new ResourceAmount(Supply(game), Fix.FromInt(reserve)) }), 100, new[] { destination },
                release ? LogisticsReserveAccess.EmergencyOnly : LogisticsReserveAccess.Protected)) };
        private static ConfigureStockPolicyCommand Ceiling(SimGame game, PolicyDispatchKind kind, int maximum)
            => new ConfigureStockPolicyCommand { Store = Id(game, "destination"), Resource = Supply(game), Change = LogisticsDemandChange.Replace(
                new LogisticsDemandSettings(true, kind == PolicyDispatchKind.Demand ? Fix.FromInt(maximum) : Fix.Zero,
                    kind == PolicyDispatchKind.Demand ? Fix.FromInt(maximum) : Fix.Zero, Fix.FromInt(maximum), 100, LogisticsReserveAccess.Protected)) };
        private static HaulResourcesCommand Haul(SimGame game)
            => new HaulResourcesCommand { Source = Id(game, "source"), Destination = Id(game, "destination"), Worker = Id(game, "machine"), Resource = Supply(game), Ceiling = Fix.FromInt(8), Priority = 100 };
        private static void Waiting(Check c, SimGame game, SimGame twin, string why, int total = 20)
        {
            for (int i = 0; i < 12; i++) Step(c, game, twin);
            c.That(!Jobs(game).Any(j => j.Worker.IsValid), why + " leaves all actual jobs unassigned");
            c.Equal(Fix.FromInt(total), Stock(game, "source") + Stock(game, "destination") + Stock(game, "machine"), why + " creates no resources/completion");
        }
        private static SimGame RefusedHaul(Check c, GameFactory factory, SimGame game)
        {
            var command = Haul(game); ulong before = game.Hash(); Code expected = game.Seat(0).Preview(command);
            c.That(!expected.IsNone, "actual explicit-haul preview refused"); c.Equal(before, game.Hash(), "refused explicit preview pure");
            int sequence = game.Seat(0).Submit(command); var twin = factory.Load(game.Save()); Step(c, game, twin);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results); var result = results.Single(r => r.Sequence == sequence);
            c.That(!result.Accepted, "actual explicit haul command refused"); c.Equal(expected, result.Refusal, "preview/command policy refusal parity");
            c.That(!Jobs(game).Any(j => j.Worker.IsValid), "refused explicit haul owns no worker/claims"); return twin;
        }
        private static HaulPreview Preview(SimGame game, PolicyDispatchKind kind, string origin)
        {
            if (kind == PolicyDispatchKind.CargoReturn) {
                var rows = new List<CargoReturnView>(); RequireFacts(View(game).CopyCargoReturns(Id(game, "machine"), rows));
                return rows.Single(r => r.Resource == Supply(game)).Return;
            }
            if (kind == PolicyDispatchKind.EmergencySupply) return Queue(game).Single(r => r.Kind == LogisticsDispatchKind.Demand && r.Destination == Id(game, "destination")).Haul;
            return View(game).PreviewHaul(Id(game, origin), Id(game, "destination"), Supply(game), Fix.FromInt(8));
        }
        private static void RequireFacts(Code refusal) { if (!refusal.IsNone) throw new InvalidOperationException("actual cargo facts refused " + refusal); }
        private static void Refusal(Check c, SimGame game, PolicyDispatchKind kind, string origin, Code expected)
        {
            var preview = Preview(game, kind, origin);
            if (kind != PolicyDispatchKind.EmergencySupply) { c.Equal(expected, preview.Blocker, "actual original admission blocker"); return; }
            // Automatic source search retains its first infeasible source (the empty startup sink),
            // not necessarily the later stocked source's policy blocker. Never invent a diagnostic.
            c.Equal(HaulStatus.NoSupply, preview.Status, "actual first infeasible-source summary"); c.Equal(Code.None, preview.Blocker, "original source summary carries no substituted blocker");
            var row = Queue(game).Single(q => q.Kind == LogisticsDispatchKind.Demand && q.Destination == Id(game, "destination"));
            c.That(row.Emergency, "blocked actual demand retains emergency classification");
            var assignment = Offers(game).Single(o => o.Scope == "sim.logistics" && o.Target == Id(game, "destination"));
            c.That(!assignment.Ready, "blocked actual emergency supply remains unassignable");
            c.Equal(new Code("logistics.dispatch-unavailable"), assignment.Blocker, "original typed aggregate dispatch blocker");
        }
        private static void Finish(Check c, GameFactory factory, SimGame game, SimGame twin, SimGame isolated, ulong untouched, IReplayRecorder recorder)
        {
            c.Equal(game.Hash(), twin.Hash(), "actual terminal policy twins"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal original state restores");
            c.Equal(untouched, isolated.Hash(), "interleaved sessions stay isolated"); ulong before = game.Hash(); var foreign = new List<WorkerDispatchOffer>(); game.Seat(1).View.Feature<WorkView>().CopyAssignments(foreign);
            c.Equal(0, foreign.Count, "foreign dispatch facts private"); c.Equal(before, game.Hash(), "terminal facts pure");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits); c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "actual policy/claims/motion replay " + replay.Failure + "/" + replay.Detail);
        }
        private static void Run(Check c)
        {
            foreach (PolicyDispatchKind kind in Enum.GetValues(typeof(PolicyDispatchKind)))
                foreach (bool direct in new[] { false, true }) foreach (bool external in new[] { false, true }) {
                    if (DispatchPolicyScenario.IsLocal(kind)) Local(c, kind, direct, external); else Transfers(c, kind, direct, external);
                }
        }
        private static void Transfers(Check c, PolicyDispatchKind kind, bool direct, bool external)
        {
            bool cargo = kind == PolicyDispatchKind.CargoReturn, supplied = DispatchPolicyScenario.IsSuppliedWork(kind), emergency = kind == PolicyDispatchKind.EmergencySupply, manual = kind == PolicyDispatchKind.Manual;
            string origin = cargo ? "machine" : "source", activity = emergency ? "policy.supplied-response" : "policy.work-supply";
            int amount = cargo ? 2 : 4, initialReserve = cargo ? 2 : 16, fullReserve = cargo ? 4 : 20;
            var factory = DispatchPolicyScenario.Create(kind, direct, external); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); using var recorder = game.RecordReplay(Limits);
            ulong before = game.Hash(); c.Equal(Code.None, View(game).ReadOutletPolicy(Id(game, origin), out var held), "actual origin policy owner");
            c.Equal(Fix.FromInt(initialReserve), held.Reserve.AmountOf(Supply(game)), "actual authored stock/cargo reserve");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(Export(game, origin, 0)), "foreign origin configuration refused"); c.Equal(Codes.NotFound, game.Seat(1).Preview(Ceiling(game, kind, 8)), "foreign receiving configuration refused");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().ReadOutletPolicy(Id(game, origin), out var foreign), "foreign actual origin facts withheld"); c.That(foreign == null, "no foreign policy prefix"); c.Equal(before, game.Hash(), "actual policy checks pure");
            var start = new List<Command> { Ceiling(game, kind, amount) };
            if (kind == PolicyDispatchKind.Route) start.Add(new AddRouteCommand { From = Id(game, "source"), To = Id(game, "destination"), Resource = Supply(game), Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 100) });
            if (kind == PolicyDispatchKind.WorkSupply) start.Add(new PostOrderCommand { Plan = activity, Target = Id(game, "destination"), Priority = 100 });
            if (emergency) start.Add(new RaiseIncidentCommand { Target = Id(game, "destination"), Source = new Code("damage"), Kind = new Code("proof.failure") });
            var twin = Input(c, factory, game, start.ToArray()); Waiting(c, game, twin, kind + " disabled worker");
            if (manual) twin = RefusedHaul(c, factory, game);
            twin = Input(c, factory, game, Policy(game, "policy.short")); Waiting(c, game, twin, kind + " area"); c.That(Preview(game, kind, origin).Status != HaulStatus.Ready, "actual area prohibits acquisition");
            if (manual) twin = RefusedHaul(c, factory, game);
            twin = Input(c, factory, game, Policy(game, "policy.all", haul: false)); Waiting(c, game, twin, kind + " permitted roles");
            if (manual) twin = RefusedHaul(c, factory, game);
            if (emergency) {
                twin = Input(c, factory, game, Policy(game, "policy.all", emergency: false), Floor()); Waiting(c, game, twin, "emergency eligibility");
                twin = Input(c, factory, game, Policy(game, "policy.all"), Export(game, origin, fullReserve)); Waiting(c, game, twin, "emergency cannot bypass protected material reserve");
            } else {
                twin = Input(c, factory, game, Policy(game, "policy.all"), Floor()); Waiting(c, game, twin, kind + " ordinary ready-worker reserve");
                if (manual) twin = RefusedHaul(c, factory, game);
            }
            var releaseFloor = emergency ? Floor() : Reserve(0);
            twin = Input(c, factory, game, releaseFloor, Export(game, origin, initialReserve, destination: "machine")); Waiting(c, game, twin, kind + " destination permission");
            Refusal(c, game, kind, origin, LogisticsPolicyCodes.Destination); if (manual) twin = RefusedHaul(c, factory, game);
            twin = Input(c, factory, game, Export(game, origin, fullReserve)); Waiting(c, game, twin, kind + " full physical reserve"); if (manual) twin = RefusedHaul(c, factory, game);
            twin = Input(c, factory, game, Export(game, origin, initialReserve));
            if (manual) twin = Input(c, factory, game, Haul(game));
            Until(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), kind + " actual first acquisition");
            var first = Jobs(game).Single(j => j.IsHaul && j.Worker.IsValid); c.Equal(Fix.FromInt(amount), first.Amount, "original stock/cargo reserve and receiving maximum constrain actual first job");
            c.Equal(Id(game, "destination"), first.Destination, "actual promised receiver"); c.That(!first.Purchase, "physical dispatch is not a purchase"); c.Equal(Fix.Zero, Stock(game, "destination"), "assignment cannot teleport delivery");
            if (kind == PolicyDispatchKind.Route) c.That(first.Route > 0, "original standing route identity");
            if (cargo || kind == PolicyDispatchKind.Evacuation) c.Equal(cargo ? LogisticsRecoveryKind.CargoReturn : LogisticsRecoveryKind.Evacuation, first.RecoveryKind.Value, "original recovery kind");
            if (kind == PolicyDispatchKind.Independent) c.That(Offers(game).Any(o => o.Scope == "proof.shipments" && o.Identity > int.MaxValue), "non-friend source preserves full64-bit authored identity");
            if (supplied) c.That(first.HaulDemand.Scope == "sim.work" && first.Emergency == emergency, "actual native work-supply source and emergency classification");
            c.Equal(cargo ? Fix.FromInt(amount) : Fix.Zero, first.Loaded, "only original cargo-return already carries material");
            twin = Partial(c, factory, game);
            if (!cargo) { Until(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.Loaded.IsPositive), kind + " timed actual loading"); twin = Partial(c, factory, game); }
            Until(c, game, twin, () => Stock(game, "destination") == Fix.FromInt(amount) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), kind + " first physical delivery");
            c.Equal(Fix.FromInt(16), Stock(game, "source"), "actual finite protected warehouse remains16"); c.Equal(Fix.FromInt(cargo ? 2 : 0), Stock(game, "machine"), "actual remaining carrier cargo");
            if (supplied) c.That(!Done(game, activity), "four supplied units cannot complete eight-unit work");
            twin = Input(c, factory, game, Export(game, origin, emergency ? 20 : cargo ? 0 : 12, release: emergency)); Waiting(c, game, twin, kind + " full receiver maximum");
            Refusal(c, game, kind, origin, cargo ? LogisticsPolicyCodes.Destination : LogisticsPolicyCodes.Maximum);
            c.Equal(Code.None, View(game).ReadStockPolicy(Id(game, "destination"), Supply(game), out var full), "original actual maximum facts"); c.Equal(Fix.FromInt(amount), full.Maximum, "current receiver exactly full");
            if (manual) twin = RefusedHaul(c, factory, game);
            twin = Input(c, factory, game, Export(game, origin, emergency ? 20 : cargo ? 0 : 12, false, release: emergency), Ceiling(game, kind, amount * 2)); Waiting(c, game, twin, kind + " disabled export after capacity release");
            Refusal(c, game, kind, origin, LogisticsPolicyCodes.Disabled); if (manual) twin = RefusedHaul(c, factory, game);
            twin = Input(c, factory, game, Export(game, origin, emergency ? 20 : cargo ? 0 : 12, release: emergency));
            if (manual) twin = Input(c, factory, game, Haul(game));
            Until(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), kind + " normal restart");
            c.Equal(Fix.FromInt(amount), Jobs(game).Single(j => j.IsHaul && j.Worker.IsValid).Amount, "original fresh trip uses remaining real gap"); twin = Partial(c, factory, game);
            if (supplied) {
                Until(c, game, twin, () => Jobs(game).Any(j => !j.IsHaul && j.Worker.IsValid), kind + " fully supplied native work");
                c.That(!Done(game, activity), "native work is not instant after delivery"); twin = Partial(c, factory, game);
                Until(c, game, twin, () => Done(game, activity), kind + " physical native completion");
                c.Equal(Fix.Zero, Stock(game, "destination"), "native work consumes exactly eight actual units once");
            } else Until(c, game, twin, () => Stock(game, "destination") == Fix.FromInt(amount * 2) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), kind + " second actual delivery");
            Waiting(c, game, twin, kind + " terminal policy ceiling", supplied ? 12 : 20);
            c.Equal(Fix.FromInt(cargo ? 16 : 12), Stock(game, "source"), "terminal actual finite source"); c.Equal(Fix.Zero, Stock(game, "machine"), "terminal original cargo empty");
            c.Equal(Fix.FromInt(initialReserve), held.Reserve.AmountOf(Supply(game)), "retained original outlet settings detached"); Finish(c, factory, game, twin, isolated, untouched, recorder);
        }
        private static void Local(Check c, PolicyDispatchKind kind, bool direct, bool external)
        {
            bool emergency = DispatchPolicyScenario.IsEmergency(kind); string activity = emergency ? "policy.local-response" : "policy.local-work";
            var factory = DispatchPolicyScenario.Create(kind, direct, external); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); using var recorder = game.RecordReplay(Limits);
            Command intent = emergency ? new RaiseIncidentCommand { Target = Id(game, "destination"), Source = new Code("damage"), Kind = new Code("proof.failure") }
                : new PostOrderCommand { Plan = activity, Target = Id(game, "destination"), Priority = 100 };
            ulong before = game.Hash(); c.Equal(Codes.NotOwned, game.Seat(1).Preview(intent), "foreign local work/response intent denied"); c.Equal(before, game.Hash(), "foreign work/response refusal pure");
            var twin = Input(c, factory, game, intent); Waiting(c, game, twin, kind + " disabled native worker");
            twin = Input(c, factory, game, Policy(game, "policy.short")); Waiting(c, game, twin, kind + " native operating area");
            twin = Input(c, factory, game, Policy(game, "policy.all", inspect: false)); Waiting(c, game, twin, kind + " native allowed roles");
            twin = Input(c, factory, game, Policy(game, "policy.all", emergency: !emergency), Floor(inspect: true)); Waiting(c, game, twin, kind + (emergency ? " emergency eligibility" : " ordinary ready-worker reserve"));
            c.That(!Done(game, activity), "blocked native intent never publishes completion");
            // Export restrictions apply to transfers, not lawful local consumption of already-supplied inputs.
            var conditions = new List<Command> { Export(game, "destination", 8, false, "machine"), Ceiling(game, kind, 0), Policy(game, "policy.all") };
            if (!emergency) conditions.Add(Reserve(0));
            twin = Input(c, factory, game, conditions.ToArray());
            Until(c, game, twin, () => Jobs(game).Any(j => !j.IsHaul && j.Worker.IsValid), kind + " actual native acquisition");
            c.That(!Jobs(game).Any(j => j.IsHaul), "native local input requires no duplicate transport"); c.That(!Done(game, activity), "native acquisition cannot instantly complete");
            c.Equal(Fix.FromInt(8), Stock(game, "destination"), "remote acquisition cannot consume before physical arrival"); twin = Partial(c, factory, game);
            Until(c, game, twin, () => Done(game, activity), kind + " timed native work"); c.Equal(Fix.Zero, Stock(game, "destination"), "local eight actual inputs consumed once");
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "local work does not drain protected remote warehouse"); c.Equal(Fix.Zero, Stock(game, "machine"), "local response/work invents no cargo");
            Finish(c, factory, game, twin, isolated, untouched, recorder);
        }
    }
}
