using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Pacing = Beep.SimEngine.Tests.ScenarioPressureTests;
using Physical = Beep.SimEngine.Tests.PermanentRemovalTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string ScenarioPressureCase = "Networked scenario pressure: saved reservations, ordinary command conflict, refill and physical incident recovery";
        internal const string ScenarioPolicyCase = "Networked pressure contract: saved grace, native worker policy command and explicit zero refill";
        internal const string MultiCompanyPressureCase = "Networked multi-company pressure: private physical recoveries saved budgets and producer refusals";
        internal const string MultiCompanyPressureIntegrityCase = "Networked pressure saves: late company corruption and consumed or foreign reservation tickets";
        private static EntityId[] ScenarioTargets(SimGame game, int seat = 0)
            => game.Seat(seat).Snapshot.Entities.Where(e => e.Owner == new PlayerId(seat) && e.Def == "pressure.machine").OrderBy(e => e.Id).Select(e => e.Id).ToArray();
        private static string ScenarioDiagnostics(SimGame game)
        {
            var f = Pacing.Facts(game); return "at=" + game.Clock.StepIndex + "/pressure=" + f.Available + "/" + f.Reserved + "/" + f.Spent +
                "/reply=" + f.Accepted + "/" + f.Refused + "/pending=" + f.PendingSequence + "/" + f.PendingSource + "/" + f.Blocker +
                "/targets=" + string.Join(";", ScenarioTargets(game).Select(id => id + "/" + Pacing.Active(game, id) + "/" + Physical.Facts(game, id).Stock));
        }
        private static Pair ScenarioPair(SimGame a, SimGame b, Fix cost)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long before) && before == game.Clock.StepIndex) return;
                var f = Pacing.Facts(game);
                if (f.Available < Fix.Zero || f.Reserved < Fix.Zero || f.Available + f.Reserved > Fix.One ||
                    f.Spent != Fix.FromInt(checked((int)f.Accepted)) * cost || (f.PendingSequence == 0 ? !f.Reserved.IsZero : f.Reserved != cost) ||
                    f.Decisions > game.Clock.StepIndex || f.Decisions > 0 && f.NextDecision != f.LastDecision + game.Clock.Time.ExactTicks(GameDuration.Minutes(1)))
                    throw new InvalidOperationException("native pressure cost/correlation/cadence invariant/" + ScenarioDiagnostics(game));
                BigInteger stock = 0, consumed = 0; var supply = Physical.Supply(game);
                foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0)))
                    if (game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, supply, out var value).IsNone) stock += value.Stock.Raw;
                foreach (var row in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates) {
                    if (row.Kind == ResourceFlowKind.AuthoredCreation || row.Kind == ResourceFlowKind.StockPurchase)
                        throw new InvalidOperationException("pacing fabricated supplies or purchase");
                    if (row.Resource != supply) continue;
                    if (row.Kind == ResourceFlowKind.Consumed) consumed += row.AmountRaw;
                    if (row.Kind == ResourceFlowKind.Produced || row.Kind == ResourceFlowKind.Lost || row.Kind == ResourceFlowKind.Destroyed)
                        throw new InvalidOperationException("pressure response must use original finite supplies");
                }
                if (stock + consumed != new BigInteger(Fix.FromInt(60).Raw)) throw new InvalidOperationException("physical pressure response conservation/" + ScenarioDiagnostics(game));
                var privateRows = new List<ScenarioPressureSnapshot> { f }; game.Seat(1).View.Feature<ScenarioPressureView>().CopyPressure(privateRows);
                if (privateRows.Count != 0 || ScenarioTargets(game, 1).Any(id => {
                    game.Seat(1).View.Feature<IncidentsView>().TryGetIncident(id, Pacing.Leak, out var incident); return incident.Active;
                })) throw new InvalidOperationException("private pressure targeted/disclosed another company");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, ScenarioDiagnostics, 0);
        }
        private static Pair ScenarioRestore(Check c, GameFactory factory, Pair source, Fix cost)
        {
            var pair = ScenarioPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()), cost);
            c.Equal(source.A.Hash(), pair.A.Hash(), "pressure reservation/counters/native inbox/physical response save together");
            c.Equal(source.B.Hash(), pair.B.Hash(), "saved peer restores exact actual pressure state"); return pair;
        }
        private static CommandResult? ScenarioReply(SimGame game, int sequence, int seat = 0)
        {
            var rows = new List<CommandResult>(); long cursor = 0; game.Seat(seat).ReadResults(ref cursor, rows);
            return rows.Where(r => r.Stream == new Code(ScenarioPressureModule.ModuleId) && r.Sequence == sequence).Select(r => (CommandResult?)r).SingleOrDefault();
        }
        private static bool ScenarioRepairing(SimGame game, EntityId target, int seat = 0)
        {
            var nodes = new List<ActivityNodeView>();
            return game.Seat(seat).View.Feature<IncidentsView>().TryGetResponseOrder(target, Pacing.Leak, out int order) &&
                game.Seat(seat).View.Feature<WorkView>().TryCopyNodes(order, nodes) && nodes.Any(n => n.Id == "repair" && n.ProductiveSteps > 0 && n.State != NodeState.Done);
        }
        private static GameFactory MultiCompanyPressureFactory()
        {
            var content = Json.Parse(FrameworkProofContent.Read("scenario-pressure.json"));
            var profiles = content.Get("scenario_pressure_profiles");
            var second = Enumerable.Range(0, profiles.Count).Select(i => profiles[i]).Single(p => p.Get("id").Text == "pressure.incidents").Clone();
            second.Set("id", JsonValue.Of("pressure.two-company")); second.Set("refill", JsonValue.Number("0"));
            second.Get("cadence").Set("numerator", JsonValue.Number("3")); second.Get("cooldown").Set("numerator", JsonValue.Number("30"));
            second.Set("sources", Json.Parse("[\"pressure.z-leak\"]")); profiles.Add(second);
            var response = Json.Parse(FrameworkProofContent.Read("pressure-response.json"));
            response.Get("entity_setups")[2].Get("placement").Set("position", JsonValue.Number("80"));
            response.Get("entity_setups").Add(Json.Parse("{\"id\":\"pressure.foreign-worker\",\"after\":[],\"entity\":\"machine\",\"company\":1,\"placement\":{\"space\":\"field.points\",\"position\":60},\"home\":null}"));
            response.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("pressure.foreign-worker"));
            return Pacing.Factory(content: content.ToString(), response: response.ToString(), edit: scenario =>
                scenario.Get("settings").Set(ScenarioPressureModule.ModuleId,
                    Json.Parse("{\"companies\":[{\"company\":1,\"profile\":\"pressure.two-company\"},{\"company\":0,\"profile\":\"pressure.incidents\"}]}")));
        }
        private static ScenarioPressureSnapshot CompanyPressure(SimGame game, int seat)
        {
            var rows = new List<ScenarioPressureSnapshot>(); game.Seat(seat).View.Feature<ScenarioPressureView>().CopyPressure(rows);
            if (rows.Count != 1 || rows[0].Company != new PlayerId(seat))
                throw new InvalidOperationException("private pressure view exposed missing or foreign company state");
            return rows[0];
        }
        private static EntityId CompanyWorker(SimGame game, int seat)
            => game.Seat(seat).Snapshot.Entities.Single(e => e.Owner == new PlayerId(seat) && e.Def == "machine").Id;
        private static Fix CompanyStock(SimGame game, int seat, EntityId entity)
        {
            game.Seat(seat).View.TryFindResource("supply", out var resource);
            var code = game.Seat(seat).View.Feature<LogisticsView>().ReadDiagnostics(entity, resource, out var facts);
            if (!code.IsNone) throw new InvalidOperationException("missing actual private stock/" + seat + "/" + entity + "/" + code);
            return facts.Stock;
        }
        private static string MultiCompanyDiagnostics(SimGame game)
            => "at=" + game.Clock.StepIndex + "/" + string.Join(";", new[] { 0, 1 }.Select(seat => {
                var f = CompanyPressure(game, seat); return seat + ":" + f.Available + "/" + f.Reserved + "/" + f.Spent + "/" + f.Accepted + "/" + f.Blocker;
            }));
        private static Pair MultiCompanyPair(SimGame a, SimGame b)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long before) && before == game.Clock.StepIndex) return;
                game.Seat(0).View.TryFindResource("supply", out var supply);
                foreach (int seat in new[] { 0, 1 })
                {
                    var company = new PlayerId(seat); var f = CompanyPressure(game, seat);
                    if (f.Available < Fix.Zero || f.Reserved < Fix.Zero || f.Available + f.Reserved > Fix.One ||
                        f.Spent.Raw != (BigInteger)f.Accepted * Fix.One.Raw || (f.PendingSequence == 0 ? !f.Reserved.IsZero : f.Reserved != Fix.One))
                        throw new InvalidOperationException("private pacing invariant/" + MultiCompanyDiagnostics(game));
                    BigInteger stock = 0, consumed = 0;
                    foreach (var entity in game.Seat(seat).Snapshot.Entities.Where(e => e.Owner == company))
                        if (game.Seat(seat).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, supply, out var value).IsNone) stock += value.Stock.Raw;
                    foreach (var row in game.Seat(seat).View.Feature<ResourceFlowView>().ReadSummary(company).Aggregates)
                    {
                        if (row.Kind == ResourceFlowKind.AuthoredCreation || row.Kind == ResourceFlowKind.StockPurchase)
                            throw new InvalidOperationException("multi-company pressure fabricated stock");
                        if (row.Resource != supply) continue;
                        if (row.Kind == ResourceFlowKind.Consumed) consumed += row.AmountRaw;
                        if (row.Kind == ResourceFlowKind.Produced || row.Kind == ResourceFlowKind.Lost || row.Kind == ResourceFlowKind.Destroyed)
                            throw new InvalidOperationException("multi-company response changed unearned resource disposition");
                    }
                    if (stock + consumed != (BigInteger)Fix.FromInt(seat == 0 ? 60 : 20).Raw)
                        throw new InvalidOperationException("private finite supply conservation/" + seat + "/" + stock + "/" + consumed);
                    var candidates = new List<PressureCandidateSnapshot>();
                    if (game.Seat(1 - seat).View.Feature<ScenarioPressureView>().CopyCandidates(company,
                        seat == 0 ? "pressure.a-leak" : "pressure.z-leak", candidates) || candidates.Count != 0)
                        throw new InvalidOperationException("foreign pressure candidates leaked");
                }
                var all = new List<ScenarioPressureSnapshot>(); game.Observer.Feature<ScenarioPressureView>().CopyPressure(all);
                if (all.Count != 2) throw new InvalidOperationException("observer must expose exactly the two actual authored company bindings");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, MultiCompanyDiagnostics, 0);
        }
        private static Pair MultiCompanyRestore(Check c, GameFactory factory, Pair source)
        {
            var restored = MultiCompanyPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()));
            c.That(restored.A.Hash() == source.A.Hash() && restored.B.Hash() == source.B.Hash(), "both companies' actual reservations/work/materials/clock restore without reenrollment");
            return restored;
        }
        private static void MultiCompanyPressureRun(Check c)
        {
            var factory = MultiCompanyPressureFactory(); var pair = MultiCompanyPair(Pacing.New(factory, ResourceFlowsModule.ModuleId), Pacing.New(factory, ResourceFlowsModule.ModuleId));
            var idle = Pacing.New(factory, ResourceFlowsModule.ModuleId); ulong untouched = idle.Hash();
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId) && ScenarioTargets(pair.A).Length == 2 && ScenarioTargets(pair.A, 1).Length == 1,
                "actual two-company resource-only setup, no economy or extra spawned targets");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            var targets = new[] { ScenarioTargets(pair.A)[0], ScenarioTargets(pair.A, 1)[0] };
            var enable = new int[2];
            foreach (int seat in new[] { 0, 1 }) enable[seat] = Submit(c, pair, EmergencyPreemptionTests.ConfigureCommand(CompanyWorker(pair.A, seat)), seat: seat);
            Reach(pair, game => CompanyPressure(game, 0).PendingSequence > 0 && CompanyPressure(game, 1).PendingSequence > 0, "both native companies reserve before either incident applies");
            var pending = new[] { CompanyPressure(pair.A, 0), CompanyPressure(pair.A, 1) };
            c.That(pending.All(f => f.Reserved == Fix.One && f.Spent.IsZero) && pending[0].PendingSource == "pressure.a-leak" && pending[1].PendingSource == "pressure.z-leak",
                "distinct authored source profiles reserve independently despite reversed enrollment JSON");
            foreach (int seat in new[] { 0, 1 }) c.That(!Pacing.Active(pair.A, targets[seat], seat) && Result(pair.A, seat, enable[seat])?.Accepted == true, "each real local worker policy applies before deferred native incident");
            var restored = MultiCompanyRestore(c, factory, pair); var duplicate = new int[2];
            foreach (int seat in new[] { 0, 1 })
            {
                var command = new RaiseIncidentCommand { Target = targets[seat], Source = Pacing.Leak, Kind = Pacing.Leak };
                duplicate[seat] = Submit(c, pair, command, seat: seat); c.Equal(duplicate[seat], Submit(c, restored, command, seat: seat), "saved pair retains each company's actual player sequence");
            }
            Both(pair, restored, game => new[] { 0, 1 }.All(seat => CompanyPressure(game, seat).Accepted == 1 && Result(game, seat, duplicate[seat])?.Refusal == Codes.InvalidArgument),
                "older native command wins each legitimate later player conflict, without backdating or state injection");
            foreach (int seat in new[] { 0, 1 })
            {
                var result = ScenarioReply(pair.A, pending[seat].PendingSequence, seat).Value;
                c.That(result.Accepted && result.Player == new PlayerId(seat) && result.Entity == targets[seat] && result.Tick == Result(pair.A, seat, duplicate[seat]).Value.Tick,
                    "same numeric native tickets in separate companies retain distinct actual command/result beneficiaries");
                c.That(Pacing.Active(pair.A, targets[seat], seat) && CompanyPressure(pair.A, seat).Refused == 0, "native accepts; only the later player producer refuses");
            }
            restored = MultiCompanyRestore(c, factory, pair);
            Both(pair, restored, game => new[] { 0, 1 }.All(seat => ScenarioRepairing(game, targets[seat], seat)), "both own-company workers travel and earn partial twelve-minute repair");
            foreach (int seat in new[] { 0, 1 })
            {
                var position = ((PointSnapshot)pair.A.Seat(seat).Snapshot.Entities.Single(e => e.Id == CompanyWorker(pair.A, seat)).Spatial).Position;
                c.Equal(Fix.FromInt(seat == 0 ? 20 : 80), position, "actual owned worker reaches its own authored field");
                c.Equal(Fix.FromInt(12), CompanyStock(pair.A, seat, targets[seat]), "each target consumes exactly its own eight supplies after arrival");
            }
            restored = MultiCompanyRestore(c, factory, pair);
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native pause freezes both companies in the original session");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 3, "independent saved companies earn their own three game minutes");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen, "paused session borrows neither company's time, work or pacing refill");
            pair.A.Net.RequestResume(); Both(pair, restored, game => game.Clock.StepIndex >= paused + 3, "same native resumed companies converge at an actual common epoch");
            Both(pair, restored, game => new[] { 0, 1 }.All(seat => !Pacing.Active(game, targets[seat], seat)), "both real material-consuming responses finish");
            restored = MultiCompanyRestore(c, factory, pair);
            Both(pair, restored, game => game.Clock.StepIndex >= 63, "distinct authored refill/cadence/cooldown policies remain independent");
            c.That(CompanyPressure(pair.A, 0).Available == Fix.FromRatio(3, 4) && CompanyPressure(pair.A, 1).Available.IsZero &&
                new[] { 0, 1 }.All(seat => CompanyPressure(pair.A, seat).Spent == Fix.One && CompanyPressure(pair.A, seat).Accepted == 1
                    && CompanyPressure(pair.A, seat).PendingSequence == 0 && CompanyPressure(pair.A, seat).Blocker == PressureCodes.Budget),
                "company0 earns three real quarter-refills while zero-refill company1 cannot borrow points or repeat incidents");
            foreach (int seat in new[] { 0, 1 }) c.Equal(Fix.FromInt(12), CompanyStock(pair.A, seat, targets[seat]), "completed saved responses never repeat material consumption");
            c.Equal(Fix.FromInt(20), CompanyStock(pair.A, 0, ScenarioTargets(pair.A)[1]), "unselected company0 target retains all authored materials");
            c.Equal(untouched, idle.Hash(), "all active peer/save/control work leaves an untouched two-company session unchanged");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "both original and saved transports actually stall and reverse packet batches");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "four actually advanced two-company worlds agree");
            long final = pair.A.Tick; long inputs = left.CommandCount, checkpoints = left.CheckpointCount;
            ReplayPair(c, factory, left, right, pair, limits, 4, "full initial two-company policies/pressure/conflicts/physical recovery replay");
            Console.WriteLine("multi-company pressure replay: initial=" + left.InitialStep + ";final=" + final + ";commands=" + inputs + ";checkpoints=" + checkpoints
                + ";hash=" + pair.A.Hash().ToString("X16") + ";stalls=" + pair.Stalls + ";reversed=" + pair.ReversedBatches);
        }
        private static void ScenarioPressureRun(Check c)
        {
            var factory = Pacing.Factory();
            var pair = ScenarioPair(Pacing.New(factory, ResourceFlowsModule.ModuleId), Pacing.New(factory, ResourceFlowsModule.ModuleId), Fix.One);
            Console.WriteLine("MODULES scenario-pressure=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId), "pacing capacity and physical supplies are not money/business");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            var targets = ScenarioTargets(pair.A); var first = targets[0]; var second = targets[1]; var initial = Pacing.Facts(pair.A);
            c.That(initial.Available == Fix.One && initial.Reserved.IsZero && initial.Spent.IsZero && initial.PendingSequence == 0, "original authored initial capacity grants no incident or spend");
            var candidates = new List<PressureCandidateSnapshot>();
            c.That(pair.A.Seat(0).View.Feature<ScenarioPressureView>().CopyCandidates(new PlayerId(0), "pressure.a-leak", candidates) &&
                candidates.Select(v => v.Entity).SequenceEqual(targets), "native source ranks actual private eligible targets by stable identity");
            c.That(!pair.A.Seat(1).View.Feature<ScenarioPressureView>().CopyCandidates(new PlayerId(0), "pressure.a-leak", candidates) && candidates.Count == 0, "foreign candidate read clears and exposes no private target");
            Reach(pair, game => Pacing.Facts(game).PendingSequence > 0, "native first decision reserves and submits for next update");
            var pending = Pacing.Facts(pair.A);
            c.That(pending.PendingSource == "pressure.a-leak" && pending.Reserved == Fix.One && pending.Available.IsZero && pending.Spent.IsZero &&
                !Pacing.Active(pair.A, first), "stable source tie-break reserves actual cost without instant mutation or charge");
            var restored = ScenarioRestore(c, factory, pair, Fix.One);
            var conflictLeft = restored.A.RecordReplay(limits); var conflictRight = restored.B.RecordReplay(limits);
            // Older native issue time legitimately wins. The later player's initially valid
            // preview becomes an ordinary refusal; no backdating or incident-state injection.
            var duplicate = new RaiseIncidentCommand { Target = first, Source = Pacing.Leak, Kind = Pacing.Leak };
            int human = Submit(c, pair, duplicate); c.Equal(human, Submit(c, restored, duplicate), "same actual later human proposal");
            Both(pair, restored, game => Pacing.Facts(game).Accepted == 1 && Result(game, 0, human)?.Refusal == Codes.InvalidArgument,
                "older queued pressure action wins real ordinary command conflict on all peers");
            var reply = ScenarioReply(pair.A, pending.PendingSequence).Value;
            c.That(reply.Accepted && reply.Command == "sim.raise-incident" && reply.Entity == first && reply.Sequence == human &&
                reply.Stream != Result(pair.A, 0, human).Value.Stream && reply.Tick == Result(pair.A, 0, human).Value.Tick,
                "equal sequence numbers in two producers retain distinct authoritative outcomes");
            var charged = Pacing.Facts(pair.A);
            c.That(charged.Reserved.IsZero && charged.Spent == Fix.One && charged.Refused == 0 && charged.CooldownUntil == reply.Tick + pair.A.Clock.Time.ExactTicks(GameDuration.Minutes(60)),
                "only actual native acceptance charges reservation once and starts authored cooldown");
            ReplayPair(c, factory, conflictLeft, conflictRight, restored, limits, 1, "saved pressure reservation through real native/human issue-order conflict replay");
            restored = ScenarioRestore(c, factory, pair, Fix.One);
            var repairLeft = restored.A.RecordReplay(limits); var repairRight = restored.B.RecordReplay(limits);
            var worker = FleetEntity(pair.A, "machine"); var enable = EmergencyPreemptionTests.ConfigureCommand(worker);
            int enabled = Submit(c, pair, enable); c.Equal(enabled, Submit(c, restored, enable), "same player worker-policy identity");
            Both(pair, restored, game => ScenarioRepairing(game, first), "actual worker travels and earns positive twelve-minute repair after three-minute detection");
            c.That(Pacing.Active(pair.A, first) && Physical.At(pair.A, worker) == Fix.FromInt(20) &&
                Physical.Facts(pair.A, first).Stock == Fix.FromInt(12) && Physical.Facts(pair.A, second).Stock == Fix.FromInt(20), "response consumes eight actual target supplies only after physical worker arrival");
            ReplayPair(c, factory, repairLeft, repairRight, restored, limits, 1, "saved accepted incident through ordinary worker release and physical repair prefix replay");
            restored = ScenarioRestore(c, factory, pair, Fix.One);
            var workLeft = restored.A.RecordReplay(limits); var workRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native sole-clock pause on saved positive response");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 8, "other saved response peers physically progress on their own time");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen, "paused original borrows no pressure age, work or supplies");
            pair.A.Net.RequestResume(); Both(pair, restored, game => game.Clock.StepIndex >= paused + 8, "native resumed response converges at common actual epoch");
            Both(pair, restored, game => !Pacing.Active(game, first), "same actual first response finishes without replenishment grant");
            c.That(Pacing.Facts(pair.A).Accepted == 1 && Physical.Facts(pair.A, first).Stock == Fix.FromInt(12), "physical recovery preserves native spend and finite remainder");
            ReplayPair(c, factory, workLeft, workRight, restored, limits, 0, "saved repair prefix through isolated real first recovery replay");
            restored = ScenarioRestore(c, factory, pair, Fix.One);
            var budgetLeft = restored.A.RecordReplay(limits); var budgetRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => game.Clock.StepIndex >= 63, "three original twenty-minute refill periods pass and cooldown expires");
            c.That(Pacing.Facts(pair.A).Available == Fix.FromRatio(3, 4) && Pacing.Facts(pair.A).Reserved.IsZero &&
                Pacing.Facts(pair.A).Blocker == PressureCodes.Budget && Pacing.Facts(pair.A).LastRefill == 60, "insufficient pacing capacity is an exact typed wait, not a free action");
            ReplayPair(c, factory, budgetLeft, budgetRight, restored, limits, 0, "saved first recovery through exact cooldown/refill budget wait replay");
            restored = ScenarioRestore(c, factory, pair, Fix.One);
            var refillLeft = restored.A.RecordReplay(limits); var refillRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => Pacing.Facts(game).PendingSequence > pending.PendingSequence, "fourth actual refill period funds a fresh next-update action");
            var secondPending = Pacing.Facts(pair.A);
            c.That(secondPending.LastRefill == 80 && secondPending.Reserved == Fix.One && secondPending.Available.IsZero && secondPending.Spent == Fix.One &&
                secondPending.Accepted == 1 && !Pacing.Active(pair.A, first), "fresh reservation is not charge or instant repeated incident");
            ReplayPair(c, factory, refillLeft, refillRight, restored, limits, 0, "saved budget wait through fresh affordable native reservation replay");
            restored = ScenarioRestore(c, factory, pair, Fix.One);
            var acceptedLeft = restored.A.RecordReplay(limits); var acceptedRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => Pacing.Facts(game).Accepted == 2, "actual second native acceptance spends fresh reservation once");
            c.That(ScenarioReply(pair.A, secondPending.PendingSequence).Value.Accepted && Pacing.Facts(pair.A).Spent == Fix.FromInt(2) &&
                Pacing.Facts(pair.A).Reserved.IsZero && Pacing.Active(pair.A, first), "same ordinary native command/result stream owns second incident and cost");
            ReplayPair(c, factory, acceptedLeft, acceptedRight, restored, limits, 0, "saved fresh native reservation through actual second acceptance replay");
            restored = ScenarioRestore(c, factory, pair, Fix.One);
            var secondWorkLeft = restored.A.RecordReplay(limits); var secondWorkRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => ScenarioRepairing(game, first), "fresh second response requires actual new productive work");
            c.Equal(Fix.FromInt(4), Physical.Facts(pair.A, first).Stock, "second real response consumes another eight, never restores the first bill");
            ReplayPair(c, factory, secondWorkLeft, secondWorkRight, restored, limits, 0, "saved repeated incident through fresh physically supplied repair prefix replay");
            restored = ScenarioRestore(c, factory, pair, Fix.One);
            var endLeft = restored.A.RecordReplay(limits); var endRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => !Pacing.Active(game, first), "same second response physically finishes within original cooldown");
            c.That(Physical.Facts(pair.A, first).Stock == Fix.FromInt(4) && Physical.Facts(pair.A, second).Stock == Fix.FromInt(20) &&
                Pacing.Facts(pair.A).Accepted == 2 && Pacing.Facts(pair.A).Spent == Fix.FromInt(2), "two earned recoveries preserve finite target supplies and actual pressure spending");
            c.That(pending.Reserved == Fix.One && pending.Spent.IsZero && charged.Accepted == 1, "held pacing values remain detached after refills and new accepted action");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "native original/restored pressure transports stall and reverse bytes");
            ReplayPair(c, factory, endLeft, endRight, restored, limits, 0, "saved fresh repair prefix through actual second finite recovery replay");
            ReplayPair(c, factory, left, right, pair, limits, 2, "full native pressure/two actual recoveries sequence replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced pressure worlds converge");
            ScenarioRestore(c, factory, pair, Fix.One);
        }
        private static void ScenarioPolicyRun(Check c)
        {
            var factory = Pacing.Factory("pressure.policy"); var cost = Fix.FromRatio(1, 2);
            var pair = ScenarioPair(Pacing.New(factory, ResourceFlowsModule.ModuleId), Pacing.New(factory, ResourceFlowsModule.ModuleId), cost);
            Console.WriteLine("MODULES scenario-policy=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            var worker = FleetEntity(pair.A, "machine");
            Reach(pair, game => game.Clock.StepIndex >= 3, "original independent source grace advances on normal game time");
            c.That(Pacing.Facts(pair.A).Blocker.Id == "proof.pressure.grace" && Pacing.Facts(pair.A).Spent.IsZero &&
                !HomePolicy(pair.A, worker).Settings.Enabled, "authored five-minute grace grants no early policy or spend");
            var restored = ScenarioRestore(c, factory, pair, cost);
            var graceLeft = restored.A.RecordReplay(limits); var graceRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native pause while authored grace remains pending");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            long graceEnd = pair.A.Clock.Time.ExactTicks(GameDuration.Minutes(5));
            Reach(restored, game => game.Clock.StepIndex >= graceEnd, "saved independent peers earn actual authored remaining grace time");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen && restored.A.Clock.StepIndex > paused, "paused source borrows no grace age or native action from genuinely advancing peers");
            pair.A.Net.RequestResume(); Both(pair, restored, game => game.Clock.StepIndex >= graceEnd, "native resumed grace worlds converge at authored boundary");
            c.That(Pacing.Facts(pair.A).PendingSequence == 0 && !HomePolicy(pair.A, worker).Settings.Enabled, "original grace holds through its actual last unearned step");
            ReplayPair(c, factory, graceLeft, graceRight, restored, limits, 0, "saved grace through independently advanced native pause/resume replay");
            restored = ScenarioRestore(c, factory, pair, cost);
            var pendingLeft = restored.A.RecordReplay(limits); var pendingRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => Pacing.Facts(game).PendingSequence > 0, "independent contract source proposes original worker policy for next update");
            var pending = Pacing.Facts(pair.A);
            c.That(pending.PendingSource == "pressure.worker" && pending.Reserved == cost && pending.Available == cost && pending.Spent.IsZero &&
                !HomePolicy(pair.A, worker).Settings.Enabled, "public source reserves half unit without instantaneous policy mutation");
            ReplayPair(c, factory, pendingLeft, pendingRight, restored, limits, 0, "saved grace through actual independent-source native reservation replay");
            restored = ScenarioRestore(c, factory, pair, cost);
            var acceptedLeft = restored.A.RecordReplay(limits); var acceptedRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => Pacing.Facts(game).Accepted == 1, "original native policy command applies independent proposal");
            c.That(HomePolicy(pair.A, worker).Settings.Enabled && Pacing.Facts(pair.A).Spent == cost && Pacing.Facts(pair.A).Available == cost &&
                ScenarioReply(pair.A, pending.PendingSequence).Value.Accepted, "accepted ordinary command spends same actual reserved half unit once");
            ReplayPair(c, factory, acceptedLeft, acceptedRight, restored, limits, 0, "saved independent pending command through actual native policy acceptance replay");
            restored = ScenarioRestore(c, factory, pair, cost);
            var quietLeft = restored.A.RecordReplay(limits); var quietRight = restored.B.RecordReplay(limits); long acceptedAt = pair.A.Clock.StepIndex;
            Both(pair, restored, game => game.Clock.StepIndex >= acceptedAt + 24, "original explicit zero-refill profile observes real quiet time");
            c.That(Pacing.Facts(pair.A).Accepted == 1 && Pacing.Facts(pair.A).Spent == cost && Pacing.Facts(pair.A).Available == cost &&
                Pacing.Facts(pair.A).Reserved.IsZero && Pacing.Facts(pair.A).PendingSequence == 0 && Pacing.Facts(pair.A).Blocker == PressureCodes.NoTarget &&
                ScenarioTargets(pair.A).All(id => !Pacing.Active(pair.A, id)), "zero refill and no eligible target fabricate no extra policy action or incident");
            c.That(pending.Reserved == cost && pending.Spent.IsZero && pair.Stalls > 0 && restored.Stalls > 0 &&
                pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "detached independent-source facts and actual saved transport witnesses retained");
            ReplayPair(c, factory, quietLeft, quietRight, restored, limits, 0, "saved accepted independent action through zero-refill quiet state replay");
            ReplayPair(c, factory, left, right, pair, limits, 0, "full independent pressure-source native policy sequence replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced independent pressure worlds converge");
            ScenarioRestore(c, factory, pair, cost);
        }
    }
}
