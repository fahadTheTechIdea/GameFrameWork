using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string AutomationCase = "Networked emergency automation: deferred company decisions and physical repairs survive reordered saved sessions";
        private static readonly Code AlertLeak = new Code("pressure.leak");
        private static OperationsView AlertOperations(SimGame game) => game.Seat(0).View.Feature<OperationsView>();
        private static AutomationRuleSnapshot AlertRule(SimGame game)
            => AlertOperations(game).TryGetAutomationRule(new PlayerId(0), "plant.alert", out var rule) ? rule : null;
        private static CompanyAlertSnapshot AlertState(SimGame game, int seat = 0)
        {
            Code refusal = game.Seat(seat).View.Feature<CompanyAlertView>().TryReadAlert(new PlayerId(seat), out var state);
            if (!refusal.IsNone) throw new InvalidOperationException("private company alert/" + refusal);
            return state;
        }
        private static IncidentSnapshot AlertEpisode(SimGame game)
        {
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetIncident(Entity(game, "field.plant"), AlertLeak, out var incident))
                throw new InvalidOperationException("authored alert incident missing");
            return incident;
        }
        private static bool AlertIncident(SimGame game) => AlertEpisode(game).Active;
        private static Fix AlertStock(SimGame game, string definition = "field.plant")
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, definition), rows);
            return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static List<WorkJobView> AlertJobs(SimGame game)
        { var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); return jobs; }
        private static Fix AlertPosition(SimGame game, string definition)
            => ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Entity(game, definition)).Spatial).Position;
        private static WorkerHomeView AlertWorker(SimGame game)
        {
            Code refusal = game.Seat(0).View.Feature<WorkView>().ReadHome(Entity(game, "machine"), out var worker);
            if (!refusal.IsNone) throw new InvalidOperationException("actual emergency worker/" + refusal);
            return worker;
        }
        private static ActivityNodeView? AlertRepair(SimGame game)
        {
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetResponseOrder(Entity(game, "field.plant"), AlertLeak, out int order)) return null;
            var nodes = new List<ActivityNodeView>();
            if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes)) throw new InvalidOperationException("actual response graph disappeared");
            foreach (var node in nodes) if (node.Id == "repair") return node;
            throw new InvalidOperationException("authored repair node missing from actual response graph");
        }
        private static string AlertDiagnostics(SimGame game)
        {
            var rule = AlertRule(game);
            var queue = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(queue);
            return "at=" + game.Clock.StepIndex + "/alert=" + AlertState(game).Settings.Level + "/version=" + AlertState(game).Version +
                "/pending=" + rule?.PendingDecision + "/due=" + rule?.PendingAt + "/stock=" + AlertStock(game)
                + "/source=" + AlertStock(game, "source") + "/cargo=" + AlertStock(game, "machine") + "/incident=" + AlertIncident(game)
                + "/episode=" + AlertEpisode(game).Episode + "/worker=" + AlertWorker(game).State
                + "/dispatch=" + string.Join(";", queue.Select(offer => offer.Kind + ":" + offer.Haul.Status + ":" + offer.Haul.Blocker + ":emergency=" + offer.Emergency));
        }
        private static void AlertObserve(SimGame game)
        {
            Fix stock = AlertStock(game);
            int version = AlertState(game).Version;
            // Test oracle for this explicit three-episode fixture, never a simulation counter or rule.
            // Inactive IncidentSnapshot intentionally has no episode; the real policy revision retains it.
            int episode = Math.Max(AlertEpisode(game).Episode, (version + 1) / 2);
            Fix total = stock + AlertStock(game, "source") + AlertStock(game, "machine");
            if (version < 0 || version > 6 || episode < 0 || episode > 3
                || stock < Fix.FromInt(Math.Max(0, 20 - 8 * episode)) || stock > Fix.FromInt(20)
                || total < Fix.FromInt(40 - 8 * episode) || total > Fix.FromInt(40 - 8 * Math.Max(0, episode - 1))
                || AlertState(game, 1).Version != 0 ||
                game.Seat(1).View.Feature<OperationsView>().TryGetAutomationRule(new PlayerId(0), "plant.alert", out _))
                throw new InvalidOperationException("private response state escaped its owner/" + AlertDiagnostics(game));
        }
        private static Pair AlertPair(SimGame a, SimGame b) => new Pair(a, b, AlertObserve, AlertDiagnostics, 2);
        private static AutomationSettings AlertSettings(SimGame game)
        {
            if (!AlertOperations(game).TryGetAutomationProfile("automation.incident-alert", out var profile))
                throw new InvalidOperationException("authored company alert profile missing");
            return new AutomationSettings(profile.Id, true, ControlBinding.ForCompany(CompanyAlertControlActionDeclaration.Capability),
                new Dictionary<string, ControlBinding> { ["incident"] = new ControlBinding(Entity(game, "field.plant"), new Code("incident-state")) },
                new Dictionary<string, ControlThresholdBand>(), profile.WhenTrue, profile.WhenFalse);
        }
        private static void AutomationRun(Check c)
        {
            var response = Json.Parse(FrameworkProofContent.Read("objective-response.json"));
            var responseActivity = response.Get("activities")[0];
            if (responseActivity.Get("id")?.Text != "pressure.response") throw new InvalidOperationException("explicit response activity required");
            responseActivity.Set("automatic_supply", JsonValue.Of(true));
            var recovery = Json.Parse(FrameworkProofContent.Read("logistics-recovery.json"));
            var destinationPolicy = recovery.Get("recovery_destination_policies")[0];
            if (destinationPolicy.Get("id")?.Text != "recovery.nearest") throw new InvalidOperationException("explicit recovery destination policy required");
            destinationPolicy.Set("mode", JsonValue.Of("demand_only"));
            var factory = CompanyAlertAutomationTests.Factory(response: response.ToString(), recovery: recovery.ToString());
            var pair = AlertPair(ConstructionPlanTests.NewPoints(factory), ConstructionPlanTests.NewPoints(factory));
            Console.WriteLine("MODULES automation=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Modules.Contains(AutomationModule.ModuleId) && pair.A.Modules.Contains(CompanyAlertModule.ModuleId) &&
                pair.A.Modules.Contains(LogisticsRecoveryPolicyModule.ModuleId) && !pair.A.Modules.Contains(TycoonModule.ModuleId),
                "explicit native rules, company and cargo recovery policies operate without business");
            var limits = new ReplayLimits(8 * 1024 * 1024, 2000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int configured = Submit(c, pair, new ConfigureAutomationCommand { Rule = "plant.alert", Settings = AlertSettings(pair.A) });
            int foreign = Submit(c, pair, new ConfigureAutomationCommand { Rule = "foreign.alert", Settings = AlertSettings(pair.B) }, Codes.NotOwned, seat: 1);
            Reach(pair, g => AlertRule(g)?.HasApplied == true && !AlertRule(g).Latch && Result(g, 1, foreign).HasValue,
                "initial normal native decision and foreign refusal admitted");
            c.That(Result(pair.A, 0, configured)?.Accepted == true && Result(pair.B, 0, configured)?.Accepted == true &&
                Result(pair.A, 1, foreign)?.Refusal == Codes.NotOwned && Result(pair.B, 1, foreign)?.Refusal == Codes.NotOwned,
                "both peer result streams retain exact authorized and denied inputs");
            Submit(c, pair, new RaiseIncidentCommand { Target = Entity(pair.A, "field.plant"), Source = AlertLeak, Kind = AlertLeak });
            Reach(pair, g => AlertRule(g).PendingDecision == true, "authored dwell queues real emergency decision");
            var pending = AlertRule(pair.A);
            c.That(AlertIncident(pair.A) && AlertState(pair.A).Settings.Level == CompanyAlertLevel.Normal && AlertStock(pair.A) == Fix.FromInt(20),
                "pending action has not applied alert, repair or material effects");
            c.Equal(pair.A.Clock.StepIndex, pending.PendingAt, "pending decision names the next unexecuted engine step");
            var restored = AlertPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var middleLeft = restored.A.RecordReplay(limits); var middleRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "pending true decision and cooldown save exactly");
            Both(pair, restored, g => AlertState(g).Settings.Level == CompanyAlertLevel.Emergency, "saved next-step emergency application");
            c.That(AlertState(pair.A).Version == 1 && AlertRule(pair.A).Latch && AlertStock(pair.A) == Fix.FromInt(20),
                "one actual policy commit grants neither a repair nor materials");
            c.That(pending.PendingDecision == true && pending.CandidateSince == -1 && pending.Settings.Target.Kind == ControlTargetKind.Company,
                "retained pending publication does not follow live rule state");
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "pause applied emergency without stopping independent session");
            long paused = pair.A.Clock.StepIndex, target = paused + 4; ulong frozen = pair.A.Hash(); var timer = Stopwatch.StartNew();
            for (int i = 0; i < 1000 && timer.Elapsed.TotalSeconds < 25 &&
                (!restored.SameEpoch || restored.A.Clock.StepIndex != target); i++) { pair.Round(); restored.Round(target); }
            Console.WriteLine("AUTOMATION isolated paused=" + paused + "/target=" + target + "/original=" + AlertDiagnostics(pair.A) +
                "/saved=" + AlertDiagnostics(restored.A) + "/frozen=" + frozen.ToString("X16") + "/now=" + pair.A.Hash().ToString("X16"));
            c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen && restored.SameEpoch && restored.A.Clock.StepIndex == target,
                "original remains frozen while the saved session advances actual incident and automation time");
            pair.A.Net.RequestResume(); Both(pair, restored, g => g.Clock.StepIndex >= target, "resumed emergency session converges at common epoch");
            c.That(AlertIncident(pair.A) && AlertStock(pair.A) == Fix.FromInt(20), "emergency priority alone cannot replace an enabled compatible worker");
            var offers = new List<WorkerDispatchOffer>(); pair.A.Seat(0).View.Feature<WorkView>().CopyAssignments(offers);
            c.That(offers.Any(o => o.Emergency && o.EffectivePriority == o.Priority + 500L), "actual dispatch sees authored emergency priority");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new ConfigureWorkPolicyCommand {
                Worker = Entity(current.A, "machine"), Settings = new WorkPolicySettings(true, 0,
                    new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            bool PartialRepair(SimGame game) {
                var node = AlertRepair(game);
                return node.HasValue && node.Value.Progress.WholeTicks > 0 && node.Value.Progress.WholeTicks < node.Value.RequiredTicks;
            }
            Both(pair, restored, PartialRepair, "compatible machine travels and earns productive physical repair");
            c.That(AlertIncident(pair.A) && AlertStock(pair.A) == Fix.FromInt(12) && AlertRepair(pair.A).Value.Worker == Entity(pair.A, "machine"),
                "actual assigned machine consumes eight finite materials and still needs work time");
            ReplayPair(c, factory, middleLeft, middleRight, restored, limits, 1, "pending-emergency to partial-repair replay");
            restored = AlertPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var repairLeft = restored.A.RecordReplay(limits); var repairRight = restored.B.RecordReplay(limits);
            c.That(PartialRepair(restored.A) && pair.A.Hash() == restored.A.Hash(), "partial physical repair and reservations reattach unchanged");
            Both(pair, restored, g => !AlertIncident(g) && AlertRule(g).PendingDecision == false, "healthy signal earns deferred normal decision");
            c.Equal(CompanyAlertLevel.Emergency, AlertState(pair.A).Settings.Level, "completion pass does not apply its new restoration decision");
            ReplayPair(c, factory, repairLeft, repairRight, restored, limits, 0, "partial-repair to pending-normal replay");
            restored = AlertPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var lateLeft = restored.A.RecordReplay(limits); var lateRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "pending false decision saves through same rule owner");
            Both(pair, restored, g => AlertState(g).Settings.Level == CompanyAlertLevel.Normal && AlertState(g).Version == 2,
                "next Update restores normal through original command handler");
            target = pair.A.Clock.StepIndex + 8;
            Both(pair, restored, g => g.Clock.StepIndex >= target, "settled decisions do not repeat effects");
            c.That(!AlertIncident(pair.A) && AlertStock(pair.A) == Fix.FromInt(12) && AlertState(pair.A).Version == 2 &&
                AlertRule(pair.A).PendingDecision == null, "repair and both policy transitions occur exactly once");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && restored.Stalls > 0 && restored.ReversedBatches > 0,
                "actual original and restored transports stall and reorder batches");
            ReplayPair(c, factory, lateLeft, lateRight, restored, limits, 0, "pending-normal to settled replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(),
                "all four independent worlds converge after actual physical response");
            for (int round = 2; round <= 3; round++)
            {
                Fix opening = Fix.FromInt(20 - 8 * (round - 1)), expected = Fix.FromInt(round == 3 ? 0 : 4);
                c.Equal(opening, AlertStock(pair.A), "next incident starts with only genuine prior remaining material");
                restored = AlertPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
                var incidentLeft = restored.A.RecordReplay(limits); var incidentRight = restored.B.RecordReplay(limits);
                int ticket = Submit(c, pair, new RaiseIncidentCommand { Target = Entity(pair.A, "field.plant"), Source = AlertLeak, Kind = AlertLeak });
                c.Equal(ticket, Submit(c, restored, new RaiseIncidentCommand { Target = Entity(restored.A, "field.plant"), Source = AlertLeak, Kind = AlertLeak }),
                    "saved independent transport keeps the real repeated player input identity");
                Both(pair, restored, g => AlertRule(g).PendingDecision == true, "repeated native incident earns emergency dwell");
                c.That(Result(pair.A, 0, ticket)?.Accepted == true && AlertEpisode(pair.A).Episode == round
                    && AlertState(pair.A).Version == 2 * (round - 1) && AlertState(pair.A).Settings.Level == CompanyAlertLevel.Normal
                    && AlertStock(pair.A) == opening, "repeated pending intent does not apply policy, consume a bill or duplicate an episode");
                ReplayPair(c, factory, incidentLeft, incidentRight, restored, limits, 1, "saved settled state through repeated pending emergency replay");
                restored = AlertPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
                var pendingLeft = restored.A.RecordReplay(limits); var pendingRight = restored.B.RecordReplay(limits);
                Both(pair, restored, g => AlertState(g).Settings.Level == CompanyAlertLevel.Emergency && AlertState(g).Version == 2 * round - 1,
                    "saved repeated emergency commits its actual next-step policy once");
                var reserves = AlertState(pair.A).Settings.WorkerReserves;
                c.That(reserves.Count == 1 && reserves[0].MinimumReady == 1 && AlertRule(pair.A).Latch,
                    "same authored preparedness remains active without reconfiguring the already-enabled machine");
                if (round == 3)
                {
                    bool LoadedSupply(SimGame game)
                    {
                        var jobs = AlertJobs(game);
                        var worker = Entity(game, "machine");
                        var position = AlertPosition(game, "machine");
                        var sourcePosition = AlertPosition(game, "source");
                        var destinationPosition = AlertPosition(game, "field.plant");
                        return position > sourcePosition && position < destinationPosition && jobs.Any(job => job.IsHaul
                            && job.Loaded == Fix.FromInt(4) && job.Source == Entity(game, "source")
                            && job.Destination == Entity(game, "field.plant") && job.Worker == worker);
                    }
                    Both(pair, restored, LoadedSupply, "third emergency physically loads and moves its four-unit shortfall");
                    c.That(AlertStock(pair.A) == opening && AlertStock(pair.A, "source") == Fix.FromInt(16)
                        && AlertStock(pair.A, "machine") == Fix.FromInt(4) && AlertIncident(pair.A)
                        && AlertRepair(pair.A)?.Progress.WholeTicks == 0, "loaded supply is real cargo, not an instant repair, spare grant or second balance");
                    ReplayPair(c, factory, pendingLeft, pendingRight, restored, limits, 0, "saved pending emergency through actual loaded supply journey replay");
                    restored = AlertPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
                    c.That(LoadedSupply(restored.A) && pair.A.Hash() == restored.A.Hash(), "saved physical resupply retains original job, worker, cargo and journey");
                    pendingLeft = restored.A.RecordReplay(limits); pendingRight = restored.B.RecordReplay(limits);
                    var retainedCargoJob = AlertJobs(pair.A).Single(job => job.IsHaul && job.State == JobState.Assigned
                        && job.Worker == Entity(pair.A, "machine") && job.Loaded == Fix.FromInt(4));
                    int hold = Submit(c, pair, new SetWorkerHeldCommand { Worker = Entity(pair.A, "machine"), Held = true });
                    c.Equal(hold, Submit(c, restored, new SetWorkerHeldCommand { Worker = Entity(restored.A, "machine"), Held = true }),
                        "saved original hold uses identical native player command identity");
                    int denied = Submit(c, pair, new SetWorkerHeldCommand { Worker = Entity(pair.A, "machine"), Held = true }, Codes.NotFound, seat: 1);
                    c.Equal(denied, Submit(c, restored, new SetWorkerHeldCommand { Worker = Entity(restored.A, "machine"), Held = true }, Codes.NotFound, seat: 1),
                        "foreign hold has the same native private refusal and transport sequence");
                    Both(pair, restored, g => AlertWorker(g).State == WorkerHomeState.Held && Result(g, 1, denied).HasValue,
                        "ordinary held control interrupts actual loaded emergency hauling");
                    Fix heldAt = AlertPosition(pair.A, "machine"); long heldUntil = pair.A.Tick + 8;
                    c.That(Result(pair.A, 0, hold)?.Accepted == true && Result(pair.A, 1, denied)?.Refusal == Codes.NotFound
                        && AlertWorker(pair.A).Job == 0 && AlertStock(pair.A) == Fix.FromInt(4)
                        && AlertStock(pair.A, "source") == Fix.FromInt(16) && AlertStock(pair.A, "machine") == Fix.FromInt(4),
                        "accepted owned hold and denied foreign hold preserve physical cargo and stocks without a refund");
                    c.That(!AlertJobs(pair.A).Any(job => job.Id == retainedCargoJob.Id
                        && (job.State == JobState.Open || job.State == JobState.Assigned || job.State == JobState.Paused)),
                        "old haul commitment ends rather than a held worker retaining an active job");
                    Both(pair, restored, g => g.Clock.StepIndex >= heldUntil, "held emergency cargo waits through real advancing game time");
                    c.That(AlertWorker(pair.A).State == WorkerHomeState.Held && AlertWorker(pair.A).Job == 0
                        && AlertPosition(pair.A, "machine") == heldAt && AlertStock(pair.A, "machine") == Fix.FromInt(4)
                        && AlertStock(pair.A) == Fix.FromInt(4) && AlertStock(pair.A, "source") == Fix.FromInt(16)
                        && AlertIncident(pair.A) && AlertRepair(pair.A)?.Progress.WholeTicks == 0
                        && AlertState(pair.A).Settings.Level == CompanyAlertLevel.Emergency && AlertState(pair.A).Version == 5,
                        "emergency reserves never override explicit hold, teleport cargo or grant repair progress");
                    ReplayPair(c, factory, pendingLeft, pendingRight, restored, limits, 2, "loaded supply through owned/foreign hold and actual wait replay");
                    restored = AlertPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
                    c.That(AlertWorker(restored.A).State == WorkerHomeState.Held && AlertStock(restored.A, "machine") == Fix.FromInt(4)
                        && AlertPosition(restored.A, "machine") == heldAt && restored.A.Hash() == pair.A.Hash(),
                        "saved held cargo preserves real location, worker intent and resource ownership");
                    pendingLeft = restored.A.RecordReplay(limits); pendingRight = restored.B.RecordReplay(limits);
                    int release = Submit(c, pair, new SetWorkerHeldCommand { Worker = Entity(pair.A, "machine"), Held = false });
                    c.Equal(release, Submit(c, restored, new SetWorkerHeldCommand { Worker = Entity(restored.A, "machine"), Held = false }),
                        "saved held cargo releases through the same ordinary command");
                    Both(pair, restored, g => Result(g, 0, release)?.Accepted == true && AlertWorker(g).State != WorkerHomeState.Held,
                        "released worker returns to existing native dispatch and recovery owners");
                    c.That(retainedCargoJob.Loaded == Fix.FromInt(4) && retainedCargoJob.State == JobState.Assigned,
                        "retained loaded-job snapshot stays detached after native interruption and release");
                    bool RecoveringSupply(SimGame game) => AlertJobs(game).Any(job => job.IsHaul
                        && job.State == JobState.Assigned && job.RecoveryKind == LogisticsRecoveryKind.CargoReturn
                        && job.Emergency && job.HaulDemand.Equals(retainedCargoJob.HaulDemand)
                        && job.Id != retainedCargoJob.Id && job.Worker == Entity(game, "machine")
                        && job.Destination == Entity(game, "field.plant") && job.Loaded == Fix.FromInt(4));
                    Both(pair, restored, RecoveringSupply, "native recovery reacquires actual residual cargo for the waiting activity demand");
                    c.That(AlertStock(pair.A) == Fix.FromInt(4) && AlertStock(pair.A, "source") == Fix.FromInt(16)
                        && AlertStock(pair.A, "machine") == Fix.FromInt(4) && AlertRepair(pair.A)?.Progress.WholeTicks == 0,
                        "new recovery claims only retained cargo, without source reload or instant delivery");
                    ReplayPair(c, factory, pendingLeft, pendingRight, restored, limits, 1, "saved held cargo through native release and new recovery assignment replay");
                    restored = AlertPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
                    c.That(RecoveringSupply(restored.A) && restored.A.Hash() == pair.A.Hash(),
                        "saved new recovery preserves actual residual cargo and fresh destination commitment");
                    pendingLeft = restored.A.RecordReplay(limits); pendingRight = restored.B.RecordReplay(limits);
                }
                Both(pair, restored, PartialRepair, "repeated compatible machine earns supplied partial repair");
                c.That(AlertIncident(pair.A) && AlertStock(pair.A) == expected && AlertStock(pair.A, "machine") == Fix.Zero
                    && AlertRepair(pair.A).Value.Worker == Entity(pair.A, "machine"), "whole eight-unit bill is consumed once by real assigned physical work");
                ReplayPair(c, factory, pendingLeft, pendingRight, restored, limits, 0, "saved repeated emergency through partial physical repair replay");
                restored = AlertPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
                var settledLeft = restored.A.RecordReplay(limits); var settledRight = restored.B.RecordReplay(limits);
                Both(pair, restored, g => !AlertIncident(g) && AlertRule(g).PendingDecision == false, "repeated repair earns deferred normal restoration");
                c.Equal(CompanyAlertLevel.Emergency, AlertState(pair.A).Settings.Level, "repeated completion cannot apply restoration in the same pass");
                Both(pair, restored, g => AlertState(g).Settings.Level == CompanyAlertLevel.Normal && AlertState(g).Version == 2 * round,
                    "repeated actual false branch restores normal exactly once");
                long settledAt = pair.A.Clock.StepIndex + 8;
                Both(pair, restored, g => g.Clock.StepIndex >= settledAt, "repeated settled policy does not replay prior effects");
                c.That(AlertStock(pair.A) == expected && AlertStock(pair.A, "source") == Fix.FromInt(round == 3 ? 16 : 20)
                    && AlertStock(pair.A, "machine") == Fix.Zero && AlertRule(pair.A).PendingDecision == null
                    && AlertState(pair.A).Version == 2 * round && AlertState(pair.A).Settings.WorkerReserves.Count == 0,
                    "settled repeated response keeps exact finite stock, empty cargo and once-only policy revision");
                c.Equal(Fix.FromInt(40 - 8 * round), AlertStock(pair.A) + AlertStock(pair.A, "source") + AlertStock(pair.A, "machine"),
                    "only three genuine eight-unit repairs reduce the opening forty supplies");
                ReplayPair(c, factory, settledLeft, settledRight, restored, limits, 0, "saved partial repair through repeated normal settlement replay");
                c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && restored.Stalls > 0 && restored.ReversedBatches > 0,
                    "each repeated original/saved transport actually stalls and reverses batches");
            }
            c.Equal(pair.A.Clock.StepIndex - left.InitialStep, (long)left.CheckpointCount, "full replay includes every advancing repeated emergency update");
            ReplayPair(c, factory, left, right, pair, limits, 9, "full three-emergency held cargo recovery and automation replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(),
                "all four original and independently advanced saved worlds converge after three real responses");
            Console.WriteLine("networked repeated emergency replay: initial=" + left.InitialStep + ";final=" + pair.A.Tick
                + ";commands=" + left.CommandCount + ";checkpoints=" + left.CheckpointCount + ";live-hash=" + pair.A.Hash().ToString("X16")
                + ";plant=" + AlertStock(pair.A) + ";source=" + AlertStock(pair.A, "source") + ";cargo=" + AlertStock(pair.A, "machine")
                + ";policy-version=" + AlertState(pair.A).Version + ";stalls=" + pair.Stalls + ";reversed=" + pair.ReversedBatches);
        }
    }
}
