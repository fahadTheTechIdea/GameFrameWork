using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class RecurringReferenceReplacementTests
    {
        internal const string Case = "Recurring replacement references: historical provenance current source and earned saved cadence";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Source = new Code("routine");
        private static GameFactory Factory(bool independent, string incompatible = null)
            => EntityReferenceReplacementTests.Factory(edit: root =>
            {
                string key = independent ? "reference_recurring" : "recurring_activity";
                var settings = Json.Parse(FrameworkProofContent.Read("recurring-replacement.json")).Get(independent ? "contract_source" : "native_source");
                root.Get("entities")[0].Get("components").Set(key, settings.Clone());
                var nextParts = root.Get("entities")[0].Get("components").Clone(); root.Get("entities")[3].Set("components", nextParts);
                nextParts.Get(key).Set("priority", JsonValue.Number("99"));
                nextParts.Get(key).Set("interval", Json.Parse("{\"numerator\":7,\"denominator\":1,\"unit\":\"minute\"}"));
                if (incompatible == "missing") nextParts.Remove(key);
                if (incompatible == "cadence") nextParts.Get(key).Set("cadence", JsonValue.Of("submission"));
                var upgradeCrew = root.Get("entities")[2].Clone(); upgradeCrew.Set("id", JsonValue.Of("upgrade-crew"));
                upgradeCrew.Get("components").Get("worker").Set("roles", Json.Parse("[\"inspect\"]")); root.Get("entities").Add(upgradeCrew);
                StartupContentAuthoring.Add(root, "upgrade-crew", "upgrade-crew", 0, Json.Parse("{\"space\":\"field.points\",\"position\":15}"));
                // Explicit authored startup dependency chain makes destination the first allocated entity.
                string previous = null;
                foreach (string id in new[] { "destination", "source", "machine", "crew", "reference", "upgrade-crew" })
                {
                    for (int i = 0; i < root.Get("entity_setups").Count; i++)
                        if (root.Get("entity_setups")[i].Get("id").Text == id)
                            root.Get("entity_setups")[i].Set("after", previous == null ? Json.Parse("[]") : Json.Parse("[\"" + previous + "\"]"));
                    previous = id;
                }
            });
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 31, Seats = new List<Controller> { Controller.Human, Controller.Human }, Mods = { ResourceFlowsModule.ModuleId } });
        private static EntityId Find(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static List<PlanView> Plans(SimGame game, int seat = 0)
        { var list = new List<PlanView>(); game.Seat(seat).View.Feature<WorkView>().CopyPlans(list); return list; }
        private static RecurringPlanView Read(SimGame game, EntityId entity)
        {
            Code result = game.Seat(0).View.Feature<WorkView>().ReadRecurringPlan(entity, Source, out var value);
            if (!result.IsNone) throw new InvalidOperationException("recurring view refused: " + result);
            return value;
        }
        private static void Send(Check c, SimGame game, Command command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "public player intent admitted");
            int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0;
            var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Any(a => a.Sequence == sequence && a.Accepted), "ordinary inbox commits intent");
        }
        private static void Configure(Check c, SimGame game, EntityId entity, bool enabled)
            => Send(c, game, new ConfigureRecurringPlanCommand { Entity = entity, Source = Source,
                Settings = new RecurringPlanSettings(enabled, 73, GameDuration.Minutes(60), Code.None) });
        private static void Reach(SimGame game, Func<bool> done, string stage, int bound, SimGame twin = null)
        {
            for (int i = 0; i < bound && !done(); i++)
            {
                game.Update(); twin?.Update();
                if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException(stage + ": " + game.Fault + "/" + twin?.Fault);
            }
            if (!done())
            {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException(stage + " did not finish within " + bound + " ordinary updates; " +
                    string.Join(";", jobs.Select(j => j.Worker + "/" + j.State + "/" + j.ToilIndex + "/" + j.Blocker)));
            }
        }
        private static void Run(Check c)
        {
            foreach (bool independent in new[] { false, true }) foreach (bool inFlight in new[] { false, true })
            {
                var factory = Factory(independent); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                var old = Find(game, "source"); c.Equal(1, Find(game, "destination").Value, "file-authored dependency order binds the actual living target");
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "upgrade-crew"), Held = true });
                Configure(c, game, old, true);
                Reach(game, () => Read(game, old).Schedule.LastPlan != 0, "first recurring posting", 5);
                long head = Read(game, old).Schedule.LastPlan;
                if (inFlight)
                {
                    Reach(game, () => {
                        var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                        return orders.Any(o => o.Id == Plans(game).Single(p => p.Id == head).Order && o.CurrentProgress.WholeTicks > 0);
                    }, "earned active cycle", 40);
                    Send(c, game, new ConfigurePlanCommand { Plan = head, Group = "field.routine", Priority = 73, Paused = true });
                }
                else Reach(game, () => Read(game, old).Schedule.LastCompletedPlan == head, "first earned completed cycle", 40);
                Configure(c, game, old, false);
                var before = Read(game, old); var history = Plans(game).Single(p => p.Id == head);
                c.That(history.Origin.Entity == old && history.CurrentSource == old, "posting provenance and current binding initially agree");
                c.That(!before.Schedule.Enabled && before.Schedule.Priority == 73 && before.Schedule.Interval.Literal == GameDuration.Minutes(60).Literal,
                    "player policy differs from both prototypes' authored defaults");
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "upgrade-crew"), Held = false });
                ulong pure = game.Hash(); var upgrade = new PostOrderCommand { Plan = "upgrade", Target = old };
                c.Equal(Code.None, game.Seat(0).Preview(upgrade), "source may upgrade without rewriting the other site's execution");
                c.Equal(pure, game.Hash(), "source admission changes no cadence or history");
                Send(c, game, upgrade); var twin = factory.Load(game.Save());
                Reach(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next"), "physical source upgrade", 70, twin);
                var made = Find(game, "source.next"); var after = Read(game, made); var rebound = Plans(game).Single(p => p.Id == head);
                c.That(made != old && !game.Seat(0).Snapshot.Entities.Any(e => e.Id == old), "native replacement retires old source once");
                c.That(rebound.CurrentSource == made && rebound.Origin.Entity == old && rebound.Target == history.Target &&
                    rebound.Arguments.ToString() == history.Arguments.ToString() && rebound.State == history.State && rebound.Order == history.Order &&
                    rebound.CreatedTick == history.CreatedTick && rebound.EndedTick == history.EndedTick,
                    "one live binding changes while actual provenance execution and timestamps remain factual");
                c.That(after.Schedule.LastPlan == before.Schedule.LastPlan && after.Schedule.LastCompletedPlan == before.Schedule.LastCompletedPlan &&
                    after.Schedule.LastPostedTick == before.Schedule.LastPostedTick && after.LastCompletedTick == before.LastCompletedTick &&
                    after.Schedule.Priority == before.Schedule.Priority && after.Schedule.Enabled == before.Schedule.Enabled &&
                    after.Schedule.Interval.Literal == before.Schedule.Interval.Literal,
                    "retained native/direct source keeps player policy and real schedule, not new defaults");
                c.That(history.CurrentSource == old && history.Origin.Entity == old, "old detached facts remain unchanged");
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadRecurringPlan(made, Source, out _), "source remains company-private");
                c.That(Plans(game, 1).Count == 0, "cycle bindings do not leak through other company views");
                c.Equal(game.Hash(), twin.Hash(), "pending upgrade twins retain identical source binding");
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save()); c.Equal(game.Hash(), twin.Hash(), "head binding and schedule survive new codecs");
                if (inFlight)
                {
                    foreach (var session in new[] { game, twin })
                        Send(c, session, new ConfigurePlanCommand { Plan = head, Group = "field.routine", Priority = 73, Paused = false });
                    Reach(game, () => Read(game, made).Schedule.LastCompletedPlan == head, "preserved active execution completes", 70, twin);
                    c.That(Plans(game).Single(p => p.Id == head).Origin.Entity == old, "finishing retained work never rewrites posting history");
                }
                var completed = Read(game, made);
                foreach (var session in new[] { game, twin }) Configure(c, session, made, true);
                var enabled = Read(game, made);
                c.Equal(completed.LastCompletedTick.Value + 60, enabled.DueTick.Value, "future cadence is anchored to actual earned completion, not replacement");
                Reach(game, () => Read(game, made).Schedule.LastPlan > head, "next actual scheduled posting", 100, twin);
                long next = Read(game, made).Schedule.LastPlan; var current = Plans(game).Single(p => p.Id == next);
                c.That(current.Origin.Entity == made && current.CurrentSource == made, "new cycle records new source without changing prior provenance");
                Reach(game, () => Read(game, made).Schedule.LastCompletedPlan == next, "next physical recurring work", 70, twin);
                c.That(Plans(game).Single(p => p.Id == head).Origin.Entity == old && Plans(game).Single(p => p.Id == next).Origin.Entity == made,
                    "old and new cycles retain distinct true posting origins");
                c.Equal(game.Hash(), twin.Hash(), "earned future recurrence remains deterministic after load");
                c.Equal(isolatedHash, isolated.Hash(), "another session has no source or cycle changes");
            }
            foreach (bool independent in new[] { false, true }) foreach (string reason in new[] { "missing", "cadence" })
            {
                var factory = Factory(independent, reason); var game = New(factory); var old = Find(game, "source");
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
                var command = new PostOrderCommand { Plan = "upgrade", Target = old }; ulong before = game.Hash();
                c.Equal(WorkBoard.ReplacementRecurringSource, game.Seat(0).Preview(command), "incompatible named recurring " + reason + " refuses");
                c.Equal(before, game.Hash(), "refused source preflight preserves original state");
                int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
                c.That(answers.Any(a => a.Sequence == sequence && a.Refusal == WorkBoard.ReplacementRecurringSource), "source preview and inbox refuse identically");
                c.That(game.Fault == null && !game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next"), "refusal grants no source replacement");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused source stays loadable");
            }
        }
    }
}
