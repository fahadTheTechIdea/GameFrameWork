using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static partial class RecurringPlanTests
    {
        private static readonly Code NativeRepeat = new Code("native.repeat"), PublicRepeat = new Code("public.repeat");
        private static JsonValue AuthoredParts(JsonValue root) => root.Get("entities")[0].Get("components");
        private static JsonValue AuthoredRow(JsonValue root, bool provider)
            => AuthoredParts(root).Get(provider ? "proof_plan_recurring" : "recurring_activity");
        private static GameFactory AuthoredFactory(Action<JsonValue> edit = null)
        {
            var root = Json.Parse(FrameworkProofContent.Read("recurring-plan-intents.json")); edit?.Invoke(root);
            return ConstructionPlanTests.Points(additionalModule: new PlanProofModule(),
                moduleDeclaration: "{\"id\":\"proof.plans\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"recurring-plan-intents.json\"]}",
                moduleDocument: Text(root), scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of("proof.plans")), startupProfiles: new[] { "recurring-plan-intents.start" });
        }
        private static Entity AuthoredEntity(SimGame game, string definition = "recurring.controller")
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) return entity;
            throw new InvalidOperationException("missing authored recurrence entity: " + definition);
        }
        private static RecurringPlanView AuthoredRead(SimGame game, bool provider)
            => Read(game, AuthoredEntity(game).Id, provider ? PublicRepeat : NativeRepeat);
        private static CommandResult AuthoredConfigure(SimGame game, bool provider, bool enabled, int priority = 60, int minutes = 1)
            => TestKit.Order(game, 0, new ConfigureRecurringPlanCommand { Entity = AuthoredEntity(game).Id,
                Source = provider ? PublicRepeat : NativeRepeat,
                Settings = new RecurringPlanSettings(enabled, priority, GameDuration.Minutes(minutes), Code.None) });
        private static int AuthoredCount(SimGame game, string definition)
        { int count = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) count++; return count; }
        private static void AuthoredReach(SimGame game, Func<bool> complete, string stage, SimGame twin = null, int limit = 480)
        {
            for (int i = 0; i < limit && !complete(); i++) Advance(game, 1, twin);
            if (!complete()) throw new InvalidOperationException(stage + " timeout; game update=" + game.Clock.StepIndex +
                "; native=" + AuthoredRead(game, false).Blocker + "; public=" + AuthoredRead(game, true).Blocker +
                "; plans=" + string.Join(";", Plans(game).ConvertAll(p => p.Id + "/" + p.State + "/" + p.Blocker)) +
                "; jobs=" + game.World.Services.Get<WorkBoard>().Jobs.Count);
        }
        private static void AuthoredContracts(Check c)
        {
            var mutable = Json.Parse("{\"placement\":{\"position\":12}} ");
            var binding = new RecurringPlanBinding(RecurringPlanTarget.AuthoredIntent, Array.Empty<string>(), RecurringReserveUse.None, mutable);
            mutable.Get("placement").Set("position", JsonValue.Number("80"));
            c.Equal("12", binding.Arguments.Get("placement").Get("position").Text, "binding takes its own immutable authored value");
            c.Throws<InvalidOperationException>(() => binding.Arguments.Get("placement").Set("position", JsonValue.Number("13")), "nested binding arguments are immutable");
            c.Throws<ArgumentException>(() => new RecurringPlanBinding(RecurringPlanTarget.AuthoredIntent, Array.Empty<string>(), RecurringReserveUse.None, null), "authored intent has no null default");
            c.Throws<ArgumentException>(() => new RecurringPlanBinding(RecurringPlanTarget.AuthoredIntent, new[] { "build" }, RecurringReserveUse.None, mutable), "source cannot duplicate provider-owned machine selection");
            c.Throws<ArgumentException>(() => new RecurringPlanBinding(RecurringPlanTarget.AuthoredIntent, Array.Empty<string>(), RecurringReserveUse.SelfService, mutable), "arbitrary authored plans cannot borrow a reserve exemption");
            c.Throws<ArgumentException>(() => new RecurringPlanBinding(RecurringPlanTarget.Self, Array.Empty<string>(), RecurringReserveUse.None, mutable), "entity targeting cannot silently ignore authored arguments");
            foreach (bool provider in new[] { false, true })
            {
                foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                    j => AuthoredRow(j, provider).Remove("plan"),
                    j => AuthoredRow(j, provider).Set("plan", JsonValue.Of("unknown:missing")),
                    j => AuthoredRow(j, provider).Set("plan", JsonValue.Of("demolish:point.tower")),
                    j => AuthoredRow(j, provider).Set("plan", JsonValue.Of("activity:inspect")),
                    j => AuthoredRow(j, provider).Remove("arguments"),
                    j => AuthoredRow(j, provider).Set("arguments", JsonValue.Null()),
                    j => AuthoredRow(j, provider).Set("arguments", JsonValue.NewArray()),
                    j => AuthoredRow(j, provider).Set("arguments", JsonValue.NewObject()),
                    j => AuthoredRow(j, provider).Get("arguments").Set("extra", JsonValue.Of(true)),
                    j => AuthoredRow(j, provider).Get("arguments").Get("placement").Set("space", JsonValue.Of("other")),
                    j => AuthoredRow(j, provider).Set("target", JsonValue.Of("self")),
                    j => AuthoredRow(j, provider).Set("target", JsonValue.Of("worker_home")),
                    j => AuthoredRow(j, provider).Set("worker_nodes", Json.Parse("[\"build\"]")),
                    j => AuthoredRow(j, provider).Set("reserve_use", JsonValue.Of("self_service")),
                    j => AuthoredRow(j, provider).Set("facility_policy", JsonValue.Of("unknown.facility")),
                    j => AuthoredRow(j, provider).Set("activity", JsonValue.Of("inspect"))
                }) c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(AuthoredFactory(edit)), "native/public invalid authored binding fails before play");
                var factory = AuthoredFactory(); var game = ConstructionPlanTests.NewPoints(factory);
                Entity owner = AuthoredEntity(game); var definition = owner.Def.Component<IRecurringPlanDefinition>(provider ? PublicRepeat : NativeRepeat);
                var expected = game.World.Content.Get<EntityDef>("point.tower").Component<IConstructionDefinition>();
                c.That(ReferenceEquals(expected, RecurringPlanContracts.ResolvePlan(game.World, definition)), "source resolves exact existing construction definition, not ActivityDef");
                c.That(definition is IRecurringPlanBindingDefinition && !(definition is IActivitySource), "source references a plan, never republishes its graph");
                c.That(owner.Def.AuthoringPath.Contains("recurring-plan-intents.json"), "source is master-listed independent authored content");
                var before = AuthoredRead(game, provider); ulong hash = game.Hash(); var view = game.Seat(0).View.Feature<WorkView>();
                c.That(before.Schedule.LastPlan == 0 && before.Schedule.LastPostedTick == -1 && before.Blocker == RecurringPlanCodes.Disabled &&
                    before.Intent.Definition == "build:point.tower" && before.Intent.Arguments.Get("placement").Get("position").Text == "12", "virgin authored intent has no site, work or posting age");
                c.Equal(Code.None, view.PreviewPlan(before.Intent), "ordinary construction admission owns recurring intent syntax");
                c.Equal(Code.None, view.PreviewRecurringPlan(owner.Id, before.Source, new RecurringPlanSettings(true, 60, GameDuration.Minutes(1), Code.None)), "source setting preview reuses recurring contract");
                c.Equal(ActivityFacilityCodes.NotSelected, view.PreviewRecurringPlan(owner.Id, before.Source, new RecurringPlanSettings(true, 60, GameDuration.Minutes(1), new Code("other.facility"))), "authored intent has no facility policy fallback");
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadRecurringPlan(owner.Id, before.Source, out var hidden), "private source intent does not leak to another company");
                c.That(hidden.Intent == null && hidden.Schedule.Settings == null, "foreign read clears every result fact");
                c.Equal(hash, game.Hash(), "all previews and private reads are pure");
                c.Equal(0, game.World.Services.Get<WorkBoard>().Orders.Count, "disabled authoring creates no native order");
                c.That(AuthoredConfigure(game, provider, true).Accepted, "player enables either ordinary source");
                var admitted = AuthoredRead(game, provider); var plan = Plan(game, admitted.Schedule.LastPlan);
                c.That(plan.State == PlanState.Active && plan.Origin.Entity == owner.Id && plan.Origin.Source == before.Source && plan.CreatedTick == admitted.Schedule.LastPostedTick,
                    "actual existing plan records source provenance and accepted sole-clock coordinate");
                c.Equal(1, Plans(game).Count, "one source creates one native board intent");
                c.That(before.Schedule.LastPlan == 0 && !before.Schedule.Enabled, "retained source view is detached");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "queued/current authored arguments and actual history save through existing owners");
                Advance(game, 3, twin); c.Equal(game.Hash(), twin.Hash(), "saved source and construction share deterministic continuation");
            }
            var race = ConstructionPlanTests.NewPoints(AuthoredFactory(j => {
                AuthoredRow(j, false).Set("enabled", JsonValue.Of(true)); AuthoredRow(j, true).Set("enabled", JsonValue.Of(true));
            }));
            Advance(race);
            c.That(AuthoredRead(race, false).Schedule.LastPlan > 0 && AuthoredRead(race, true).Schedule.LastPlan == 0 &&
                AuthoredRead(race, true).Blocker == PlanCodes.Duplicate && Plans(race).Count == 1,
                "native/public equal placement competition uses existing ranked pass and live duplicate guard");
            var manual = ConstructionPlanTests.NewPoints(AuthoredFactory());
            var pending = AuthoredRead(manual, false).Intent;
            c.That(TestKit.Order(manual, 0, new SubmitPlanCommand { Intent = new PlanIntent(pending.Definition, pending.Arguments, "manual", 70, true) }).Accepted,
                "player can reserve the same intent without creating a structure");
            c.That(AuthoredConfigure(manual, false, true).Accepted && AuthoredRead(manual, false).Blocker == PlanCodes.Duplicate &&
                AuthoredRead(manual, false).Schedule.LastPlan == 0 && AuthoredCount(manual, "point.tower-site") == 0,
                "manual paused plan blocks automatic duplicate without a posting timestamp or ghost site");
            foreach (int fault in new[] { 10, 11 })
            {
                var broken = ConstructionPlanTests.NewPoints(AuthoredFactory(j => {
                    AuthoredRow(j, true).Set("fault", JsonValue.Number(fault.ToString())); AuthoredRow(j, true).Set("enabled", JsonValue.Of(true));
                }));
                long time = broken.Clock.StepIndex; broken.Update();
                c.That(broken.Fault != null && broken.Clock.StepIndex == time, "forged authored request/pacing mutation faults before sole clock completion");
                c.Throws<InvalidOperationException>(() => broken.Save(), "faulted authored recurrence cannot checkpoint partial gameplay");
            }
        }
        private static void AuthoredPhysical(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                var factory = AuthoredFactory(); var game = ConstructionPlanTests.NewPoints(factory);
                c.That(AuthoredConfigure(game, provider, true).Accepted, "start authored physical recurrence");
                EmergencyPreemptionTests.Configure(c, game);
                long first = AuthoredRead(game, provider).Schedule.LastPlan; var initial = Plan(game, first);
                var board = game.World.Services.Get<WorkBoard>(); var order = board.Order(initial.Order);
                c.That(initial.State == PlanState.Active && AuthoredCount(game, "point.tower-site") == 1 && AuthoredCount(game, "point.tower") == 0,
                    "posting creates a real unfinished site, never the completed structure");
                var stock = AuthoredEntity(game, "source").Get<IInventory>(); ResourceId supply = game.World.Content.ResourceId("supply");
                c.Equal(Fix.FromInt(20), stock.Amount(supply), "intent posting spends no distant stock instantly");
                c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = first, Group = "edited.group", Priority = 75, Paused = true }).Accepted,
                    "player pauses and edits the admitted native plan");
                var twin = factory.Load(game.Save()); var progress = order.Node("prepare").Progress;
                Advance(game, 8, twin);
                c.That(order.Node("prepare").Progress.Equals(progress) && AuthoredRead(game, provider).Schedule.LastPlan == first && Plans(game).Count == 1,
                    "cadence cannot repost a paused outstanding plan or advance its actual graph");
                c.Equal(game.Hash(), twin.Hash(), "paused plan/source/native site survives checkpoint continuation");
                long current = game.Clock.StepIndex; ulong pausedHash = game.Hash(); game.Clock.Pause();
                c.Equal(0, game.Update(), "sole game clock pause stops source observation and all physical work");
                c.Equal(current, game.Clock.StepIndex, "no recurrent source clock advances while the game clock is paused");
                c.Equal(pausedHash, game.Hash(), "local pacing pause does not create another simulation chronology"); game.Clock.Resume();
                c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = first, Group = "edited.group", Priority = 75, Paused = false }).Accepted &&
                    TestKit.Order(twin, 0, new ConfigurePlanCommand { Plan = first, Group = "edited.group", Priority = 75, Paused = false }).Accepted, "resume actual native work in each session");
                AuthoredReach(game, () => Plan(game, first).State == PlanState.Done, "first physical authored build", twin);
                c.Equal(game.Hash(), twin.Hash(), "saved physical machine/material/site completion agrees");
                c.Equal(1, AuthoredCount(game, "point.tower"), "first structure exists only after timed graph completion");
                c.Equal(Fix.FromInt(12), stock.Amount(supply), "first build consumed eight actual hauled inputs");
                c.That(board.Order(initial.Order) == null && Plan(game, first).Target == initial.Target, "terminal plan history survives native order pruning");
                Advance(game, 1, twin);
                long second = AuthoredRead(game, provider).Schedule.LastPlan;
                c.That(second > first && Plan(game, second).State == PlanState.Active && AuthoredCount(game, "point.tower") == 1,
                    "eligible recurrence creates fresh intent/site, never clones a completed entity or execution");
                c.That(AuthoredConfigure(game, provider, false).Accepted && AuthoredConfigure(twin, provider, false).Accepted,
                    "source disable stops future repeat without cancelling admitted physical work");
                AuthoredReach(game, () => Plan(game, second).State == PlanState.Done, "second physical authored build", twin);
                c.Equal(2, AuthoredCount(game, "point.tower"), "selected point provider permits two completed placements without inventing a spacing rule");
                c.Equal(Fix.FromInt(4), stock.Amount(supply), "second build pays its own eight real inputs");
                Advance(game, 5, twin); c.Equal(second, AuthoredRead(game, provider).Schedule.LastPlan, "disabled source keeps actual terminal reference without another posting");
                c.Equal(game.Hash(), twin.Hash(), "two completed histories and disabled cadence remain exact");
                c.That(AuthoredConfigure(game, provider, true).Accepted && AuthoredConfigure(twin, provider, true).Accepted, "player restarts recurrence against finite stock");
                long third = AuthoredRead(game, provider).Schedule.LastPlan;
                Advance(game, 70, twin);
                c.That(third > second && AuthoredRead(game, provider).Schedule.LastPlan == third && Plan(game, third).State == PlanState.Active &&
                    AuthoredCount(game, "point.tower") == 2, "finite material shortage retains one physical plan, never spawns a free third structure or repeated jobs");
                c.Equal(game.Hash(), twin.Hash(), "shortage and retained source provenance agree in interleaved sessions");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "save during repeated partial supply retains native receipts and work");
                c.That(AuthoredConfigure(game, provider, false).Accepted && AuthoredConfigure(twin, provider, false).Accepted &&
                    TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { third }) }).Accepted &&
                    TestKit.Order(twin, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { third }) }).Accepted, "normal disable/cancel uses existing material disposition owner");
                AuthoredReach(game, () => AuthoredCount(game, "point.tower-site") == 0, "cancelled shortage site recovery", twin);
                Fix remaining = Fix.Zero;
                foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Get<IInventory>() is IInventory inventory) remaining += inventory.Amount(supply);
                c.Equal(Fix.FromInt(4), remaining, "cancellation preserves all four unconsumed units in actual endpoint inventories");
                c.Equal(game.Hash(), twin.Hash(), "interrupted repeated construction and physical recovery remain deterministic");
                long otherTime = twin.Clock.StepIndex; Advance(game);
                c.Equal(otherTime, twin.Clock.StepIndex, "independent sessions never share source or game time storage");
            }
            AuthoredTilePlacement(c);
        }
        private static void AuthoredTilePlacement(Check c)
        {
            var root = Json.Parse(FrameworkProofContent.Read("recurring-plan-intents.json")); root.Remove("entity_setups"); root.Remove("world_setup_profiles");
            var parts = AuthoredParts(root); var publicSource = parts.Get("proof_plan_recurring"); parts.Remove("proof_plan_recurring");
            parts.Set("proof_recurring_activity", publicSource);
            foreach (var source in new[] { parts.Get("recurring_activity"), publicSource })
            {
                source.Set("plan", JsonValue.Of("build:tower"));
                source.Get("arguments").Set("placement", TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)));
            }
            var factory = Colony.Factory(additionalDocuments: new Dictionary<string, string> { ["recurring-plan-intents.json"] = Text(root) });
            var game = New(factory);
            // Initial fixture entity, not a player construction/spawn action or a host rule.
            game.World.Spawn(game.World.Content.Get<EntityDef>("recurring.controller"), new PlayerId(0)); game.World.Entities.Flush();
            var obstruction = ResourceDispositionTests.Spawn(game, "tower", 10, 16);
            c.That(AuthoredConfigure(game, false, true).Accepted, "tile recurrence uses same ordinary provider binding");
            long plan = AuthoredRead(game, false).Schedule.LastPlan;
            c.That(Plan(game, plan).State == PlanState.Pending && Plan(game, plan).Blocker == Codes.Blocked && AuthoredCount(game, "site:tower") == 0,
                "occupied native tile placement remains pending without a ghost site or bypass");
            var twin = factory.Load(game.Save()); Advance(game, 4, twin);
            c.That(AuthoredRead(game, false).Schedule.LastPlan == plan && Plans(game).Count == 1, "occupied placement cannot flood automatic duplicates");
            c.Equal(game.Hash(), twin.Hash(), "occupied intent and actual placement save exactly");
            c.That(AuthoredConfigure(game, false, false).Accepted && AuthoredConfigure(twin, false, false).Accepted,
                "disable source without withdrawing the already recorded placement");
            c.That(TestKit.Order(game, 0, new DemolishCommand { Target = obstruction.Id }).Accepted &&
                TestKit.Order(twin, 0, new DemolishCommand { Target = obstruction.Id }).Accepted, "player clears actual obstruction through timed machine demolition");
            AuthoredReach(game, () => Plan(game, plan).State == PlanState.Active, "tile placement after physical demolition", twin);
            c.That(Plan(game, plan).Target.IsValid && AuthoredCount(game, "site:tower") == 1 && game.Tick > 1,
                "existing promotion creates the native site only after placement is actually free");
            c.Equal(game.Hash(), twin.Hash(), "tile placement/native work/source origin agree after saved physical recovery");
        }
    }
}
