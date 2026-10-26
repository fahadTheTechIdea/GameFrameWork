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
    public static partial class CompletionEffectTests
    {
        private static void OutcomeReplacement(Check c)
        {
            foreach (bool stocked in new[] { false, true })
            {
                var factory = Factory(proofEdit: root => {
                    var retained = JsonValue.NewArray();
                    var originals = root.Get("entity_replacement_effects");
                    for (int i = 0; i < originals.Count; i++) if (originals[i].Get("id").Text != "proof.upgrade") retained.Add(originals[i].Clone());
                    root.Set("entity_replacement_effects", retained);
                    root.Set("outcome_replacement_effects", Content.Json.Parse("[{\"id\":\"proof.upgrade\",\"source\":\"point.tower\",\"empty\":\"point.enhanced\",\"stocked\":\"point.tower\",\"resource\":\"supply\"}]"));
                });
                var game = New(factory); var target = BuildUpgradeTarget(c, game);
                if (stocked)
                    for (int i = 0; i < 3; i++)
                    {
                        Submit(c, game, "proof.order", target); var production = Order(game, target.Id, "activity:proof.order");
                        Reach(game, () => !production.IsActive, "ordinary staffed production for selected outcome");
                        c.Equal(OrderState.Done, production.State, "selector stock is earned by native physical production");
                    }
                Submit(c, game, "proof.upgrade", target); var order = Order(game, target.Id, "activity:proof.upgrade");
                Reach(game, () => order.Node("upgrade").Progress.WholeTicks > 0, "real supplied outcome-selected work");
                c.Equal(WorkBoard.ReplacementActiveExecution, Upgrades.Check(game.World, target, game.World.Content.Get<EntityDef>("point.enhanced")), "unearned active work still refuses external replacement");
                var owner = new ReservationOwner("proof.outcome-wait", 1);
                var token = target.Get<IInventory>().ReserveIn(owner, Supply(game), Fix.One);
                var twin = factory.Load(game.Save());
                Reach(game, () => !order.IsActive || order.State == OrderState.Completing, "fully earned replacement waits on real claim", twin);
                c.Equal(OrderState.Completing, order.State, "outcome effect waits rather than failing or bypassing inventory guard");
                c.Equal(Upgrades.InventoryReserved, order.Blocker, "actual foreign reservation is preserved");
                c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "earned graph frees its actual worker before replacement");
                Release(game, target.Id, owner, token); Release(twin, target.Id, owner, token);
                ulong retainedHash = game.Hash();
                c.Equal(WorkBoard.ReplacementActiveExecution, Upgrades.Check(game.World, target, game.World.Content.Get<EntityDef>(stocked ? "point.enhanced" : "point.tower")),
                    "even earned execution refuses a declared but unselected outcome");
                c.Equal(Code.None, Upgrades.Check(game.World, target, game.World.Content.Get<EntityDef>(stocked ? "point.tower" : "point.enhanced")),
                    "only the actual current outcome passes the same guarded coordinator");
                c.Equal(retainedHash, game.Hash(), "outcome admission is a pure observation");
                Reach(game, () => !order.IsActive, "declared selected outcome commits", twin);
                c.Equal(OrderState.Done, order.State, "own earned dynamic replacement is not mistaken for foreign active execution");
                if (order.State != OrderState.Done) continue;
                var made = Find(game, stocked ? "point.tower" : "point.enhanced");
                c.That(made != null && made.Id != target.Id && !target.Alive, "one real selected replacement retires original source");
                c.Equal(Fix.FromInt(stocked ? 1 : 0), made.Get<IInventory>().Amount(Supply(game)), "native work consumes two units and replacement conserves remaining actual stock");
                c.Equal(Fix.FromInt(12), made.Get<PointPresence>().Position, "selected outcome keeps original physical placement");
                c.That(UpgradeView(game).TryReadActivityCompletion(order.PlanKey, EntityId.None, out var earned) && earned.Target == target.Id && earned.Order == order.Id,
                    "earned history retains original target/order, never rewritten to replacement identity");
                c.Equal(game.Hash(), twin.Hash(), "saved outcome-selection continuation is deterministic");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "real selected outcome and earned history persist");
            }
        }

        private static JsonValue UpgradeAt(int position)
            => Content.Json.Parse("{\"space\":\"field.points\",\"position\":" + position + "}");
        private static WorkView UpgradeView(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static PlanView UpgradePlan(SimGame game, long id)
        {
            var values = new List<PlanView>(); UpgradeView(game).CopyPlans(values);
            foreach (PlanView value in values) if (value.Id == id) return value;
            throw new InvalidOperationException("missing actual upgrade intent " + id);
        }
        private static Entity BuildUpgradeTarget(Check c, SimGame game)
        {
            EmergencyPreemptionTests.Configure(c, game);
            var arguments = JsonValue.NewObject(); arguments.Set("placement", UpgradeAt(12));
            c.That(TestKit.Order(game, 0, new SubmitPlanCommand { Intent = new PlanIntent("build:point.tower",
                arguments, "initial.build", 70, false) }).Accepted, "player commands physically supplied initial construction");
            Reach(game, () => Find(game, "point.tower") != null, "initial physical facility");
            return Find(game, "point.tower");
        }
        private static void UpgradeContracts(Check c)
        {
            foreach (string field in new[] { "source", "target" })
                c.Throws<ContentException>(() => New(Factory(proofEdit: j => j.Get("entity_replacement_effects")[1].Remove(field))),
                    "replacement requires explicit " + field + " reference");
            c.Throws<ContentException>(() => New(Factory(proofEdit: j => j.Get("entity_replacement_effects")[1].Set("target", JsonValue.Of("missing")))),
                "missing replacement prototype fails before gameplay");
            c.Throws<ContentException>(() => New(Factory(proofEdit: j => j.Get("entity_activities")[0].Remove("target"))),
                "portable entity activity requires an explicit prototype");
            c.Throws<ContentException>(() => New(Factory(proofEdit: j => j.Get("entity_activities")[0].Set("target", JsonValue.Of("source")))),
                "activity and replacement source mismatch fails before gameplay");
            c.Throws<ContentException>(() => New(Factory(proofEdit: j => j.Get("entity_activities")[0].Set("replace_with", JsonValue.Of("point.enhanced")))),
                "deleted replacement shorthand has no compatibility reader");
            c.Throws<ContentException>(() => New(Factory(proofEdit: j =>
            {
                JsonValue activity = j.Get("entity_activities")[0];
                activity.Get("nodes")[0].Set("effect", JsonValue.Of("proof.upgrade"));
            })), "replacement cannot retire an entity between graph nodes");
            c.Throws<ContentException>(() => New(Factory(proofEdit: j =>
            {
                JsonValue nodes = j.Get("entity_activities")[0].Get("nodes");
                foreach (JsonValue node in new[] { nodes[0], nodes[1] })
                { node.Set("roles", JsonValue.NewArray()); node.Set("duration", Content.Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"hour\"}")); node.Remove("requirement"); }
            })), "timer-only or zero-time upgrade cannot authorize physical replacement");
            GameFactory factory = Factory(); SimGame game = New(factory);
            IEntityActivityDefinition activityDef = game.World.Content.Get<EntityActivityDef>("proof.upgrade");
            IEntityReplacementEffect replacement = game.World.Content.Get<EntityReplacementEffectDef>("proof.upgrade");
            c.That(!game.World.Content.Get<ActivityDef>("proof.system-refurbish").PlayerOrderable,
                "physical replacement graph can belong to a module, not only player-orderable content");
            c.Equal(StandardCodes.NotOrderable, game.Seat(0).Preview(new PostOrderCommand { Plan = "proof.system-refurbish", Target = Find(game, "source").Id }),
                "physical effect capability never grants a player privileged module work");
            c.That(ReferenceEquals(Board(game).ResolvePlan(activityDef.PlanKey), activityDef), "derived content uses the existing activity catalog and resolver");
            c.That(replacement.AcceptsSourceDefinition(activityDef.TargetDefinition), "authored activity/effect share the actual prototype identity");
            c.That(((EntityActivityDef)activityDef).AuthoringPath.Contains("completion-effects.json"), "master-listed document owns entity activity content");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "generic completion semantics have a direct schema identity");
            c.Equal(StandardCodes.WrongTarget, game.Seat(0).Preview(new PostOrderCommand { Plan = "proof.upgrade", Target = Find(game, "source").Id }),
                "exact prototype activity refuses an unrelated owned endpoint");
            PlanTemplate authored = UpgradeView(game).AuthoredPlanTemplate("proof.upgrade-template");
            ulong before = game.Hash();
            c.Equal(Codes.NotFound, UpgradeView(game).PreviewPlanTemplate(authored, UpgradeAt(12), "missing").Blocker,
                "portable target intent never invents an unbuilt facility");
            c.Equal(before, game.Hash(), "target preview does not create a plan, entity, stock or time");
            Entity target = BuildUpgradeTarget(c, game);
            var owner = new ReservationOwner("proof.upgrade-wait", 1);
            ReservationToken token = target.Get<IInventory>().ReserveIn(owner, Supply(game), Fix.One);
            var direct = new PostOrderCommand { Plan = "proof.upgrade", Target = target.Id };
            before = game.Hash();
            c.Equal(Upgrades.InventoryReserved, game.Seat(0).Preview(direct), "immediate preview rechecks actual replacement reservation");
            c.Equal(before, game.Hash(), "current admission preview is pure");
            c.Equal(Upgrades.InventoryReserved, TestKit.Order(game, 0, direct).Refusal, "immediate command shares current start refusal");
            before = game.Hash();
            c.Equal(Codes.NotOwned, UpgradeView(game, 1).PreviewPlanTemplate(authored, UpgradeAt(12), "private").Blocker,
                "entity template never exposes a foreign company's facility");
            c.That(UpgradeView(game).PreviewPlanTemplate(authored, UpgradeAt(12), "upgrade").Ready,
                "structural persistent intent may wait behind real inbound claims");
            c.Equal(before, game.Hash(), "all structural and current previews preserve native stock/clock/state");
            var external = new PostOrderCommand { Plan = "proof.process", Target = target.Id };
            c.Equal(ResourceTransactions.ActivePromises, game.Seat(0).Preview(external), "public completion provider supplies its own current admission rule");
            var externalIntent = new PlanIntent("activity:proof.process", ActivityWorkerSelection.CreateArguments(target.Id, ActivityWorkerSelection.Unbound),
                "external", 60, true);
            c.Equal(Code.None, UpgradeView(game).PreviewPlan(externalIntent), "public admission contract retains structurally valid blocked intent");
            c.Equal(before, game.Hash(), "public provider admission also preserves stock, identity and the sole clock");
            CommandResult placed = TestKit.Order(game, 0, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(authored), Anchor = UpgradeAt(12), Group = "upgrade" });
            c.That(placed.Accepted, "player places a paused authored upgrade template");
            PlanView plan = UpgradePlan(game, placed.Arg0);
            c.That(plan.State == PlanState.Pending && plan.Order == 0 && plan.Paused, "paused upgrade creates no second execution owner");
            c.That(UpgradeView(game).CapturePlanTemplate("upgrade", UpgradeAt(12), out PlanTemplate captured).Ready,
                "actual target capture uses its prototype-relative placement");
            c.Equal("0", captured.Entries[0].Arguments.Get("placement").Get("offset").Text, "captured geometry is relative, not a live target identity");
            c.That(captured.Entries[0].Arguments.Get("target") == null, "portable JSON contains no entity ID");
            c.Equal(Codes.NotFound, UpgradeView(game).PreviewPlanTemplate(captured, UpgradeAt(16), "other").Blocker,
                "translated target missing at exact position refuses rather than selecting a nearby entity");
            var selection = new ActivityWorkerSelection(new[] { new ActivityWorkerBinding("upgrade", Find(game, "machine").Id) });
            PlanIntent selected = activityDef.CreateTargetIntent(target.Id, selection, "selected", 70);
            c.Equal(PlanCodes.LiveBindings, activityDef.CaptureTemplate(game.World, new PlayerId(0), selected.Arguments,
                UpgradeAt(12), out JsonValue notPortable), "portable capture refuses live machine bindings");
            c.That(notPortable == null, "refused capture returns no fabricated template");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = plan.Id, Group = "upgrade", Priority = 80, Paused = false }).Accepted,
                "player releases upgrade plan pause without altering claims");
            plan = UpgradePlan(game, plan.Id);
            c.Equal(Upgrades.InventoryReserved, plan.Blocker, "pending view reports existing native reservation blocker");
            c.Equal(0, plan.Order, "blocked activation creates no native order prefix");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "pending physical admission and real claims remain durable");
            c.That(ReferenceEquals(game.Clock, game.World.Clock), "host-neutral lifecycle exposes the one session game clock");
            long update = game.Clock.StepIndex; before = game.Hash(); game.Clock.Pause();
            for (int i = 0; i < 4; i++) game.Update();
            c.Equal(update, game.Clock.StepIndex, "sole clock pause advances neither pending intent nor game time");
            c.Equal(before, game.Hash(), "clock pause does not mutate simulated upgrade state");
            game.Clock.Resume(); Release(game, target.Id, owner, token);
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { plan.Id }) }).Accepted, "player cancels actual pending upgrade");
            c.That(target.Alive && Find(game, "point.enhanced") == null, "cancelled intent creates no free replacement");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled pending template saves through existing codec");
        }

        private static void UpgradePhysical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = BuildUpgradeTarget(c, game);
            long id = Submit(c, game, "proof.upgrade", target); Order order = Order(game, target.Id, "activity:proof.upgrade");
            Reach(game, () => order.Node("upgrade").Progress.WholeTicks > 0, "real delivered upgrade materials");
            c.That(target.Alive && order.IsActive && Find(game, "point.enhanced") == null, "physical upgrade work cannot replace the entity prematurely");
            c.That(order.Node("prepare").State == NodeState.Done, "authored preparation dependency completes first");
            c.That(order.Node("upgrade").Progress.WholeTicks < game.SimulationTime.Ticks(order.Plan.Graph.Find("upgrade").Duration),
                "remaining upgrade duration is earned through game updates");
            var owner = new ReservationOwner("proof.upgrade-final-wait", 1);
            ReservationToken token = target.Get<IInventory>().ReserveIn(owner, Supply(game), Fix.One);
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "partially earned upgrade and its actual material claim roundtrip");
            Reach(game, () => order.State == OrderState.Completing, "ordinary deferred final replacement", twin);
            c.Equal(Upgrades.InventoryReserved, order.Blocker, "completion rechecks claims added after physical start");
            c.That(!order.CompletionEffectApplied && target.Alive, "waiting effect does not commit or retire its target");
            c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "earned work releases machine while replacement waits");
            c.Equal(Fix.FromInt(10), Find(game, "source").Get<IInventory>().Amount(Supply(game)),
                "initial build and upgrade consume exactly eight plus two real hauled units");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(Supply(game)), "consumed upgrade material is not copied into replacement stock");
            c.Equal(game.Hash(), twin.Hash(), "saved physical upgrade and final retry are deterministic");
            SimGame overdue = factory.Load(game.Save()); ulong pausedHash = overdue.Hash(); long pausedTime = overdue.Clock.StepIndex;
            overdue.Clock.Pause(); for (int i = 0; i < 5; i++) overdue.Update();
            c.Equal(pausedTime, overdue.Clock.StepIndex, "single clock pause freezes final effect retry");
            c.Equal(pausedHash, overdue.Hash(), "paused completion creates no claims, stock or progress");
            Release(game, target.Id, owner, token); Release(twin, target.Id, owner, token);
            Reach(game, () => order.State == OrderState.Done, "replacement after actual reservation release", twin);
            Entity made = Find(game, "point.enhanced");
            c.That(made != null && !target.Alive && made.Id != target.Id && made.Owner == target.Owner,
                "ordinary completed effect replaces identity while preserving owner");
            c.Equal(Fix.FromInt(12), made.Get<PointPresence>().Position, "selected geometry preserves real position, not prototype position99");
            c.That(order.CompletionEffectApplied && UpgradePlan(game, id).State == PlanState.Done,
                "native final effect and existing plan own terminal success");
            c.Equal(game.Hash(), twin.Hash(), "saved interleaved physical replacement matches");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed replacement stock/geometry/history is durable");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(new PostOrderCommand { Plan = "proof.upgrade", Target = target.Id }),
                "retired identity cannot be upgraded again or silently rebound");
            UpgradeCancellation(c); NativeUpgrade(c); ExternalEntityActivity(c);
        }
        private static void UpgradeCancellation(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = BuildUpgradeTarget(c, game);
            long id = Submit(c, game, "proof.upgrade", target); Order order = Order(game, target.Id, "activity:proof.upgrade");
            Reach(game, () => order.Node("upgrade").Progress.WholeTicks > 0, "cancellable earned upgrade");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { id }) }).Accepted, "player interrupts physical upgrade through existing plan command");
            c.That(target.Alive && Find(game, "point.enhanced") == null, "cancelled physical work never commits replacement");
            c.Equal(OrderState.Cancelled, order.State, "native order owns cancellation");
            c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "cancellation releases actual worker claim");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "spent progress and cancellation history remain durable");
        }
        private static void NativeUpgrade(Check c)
        {
            const string json = @"{
              ""entities"":[{""id"":""depot.enhanced"",""kind"":""entity"",""components"":{
                ""tile_presence"":{""space"":""colony.tiles"",""width"":2,""height"":2,""blocks"":true,""movable"":false},
                ""storage"":{""capacity_policy"":""fixture.count.200"",""public"":false,""initial_stock"":{}}}}],
              ""entity_replacement_effects"":[{""id"":""native.upgrade"",""source"":""depot"",""target"":""depot.enhanced""}],
              ""target_resource_requirements"":[{""id"":""native.upgrade-materials"",""port"":null,""use"":""consume"",""quantities"":{""steel"":2}}],
              ""entity_activities"":[{""id"":""native.upgrade"",""target"":""depot"",""player_orderable"":true,""automatic_supply"":false,
                ""priority"":80,""on_complete"":""native.upgrade"",""nodes"":[{""id"":""upgrade"",""executor"":""work.staffed"",""roles"":[""crane""],""duration"":{""numerator"":1,""denominator"":10,""unit"":""hour""},""requirement"":""native.upgrade-materials""}]}]}";
            GameFactory factory = Colony.Factory(additionalDocuments: new Dictionary<string, string> { ["entity-upgrades.json"] = json });
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 719 });
            Entity target = Find(game, "depot"); ResourceId steel = game.World.Content.ResourceId("steel");
            Fix before = target.Get<IInventory>().Amount(steel); var position = target.Get<ITilePresence>().Tile;
            long id = Submit(c, game, "native.upgrade", target); Order order = Order(game, target.Id, "activity:native.upgrade");
            Reach(game, () => order.Nodes[0].Progress.WholeTicks > 0, "native tile physical upgrade");
            c.That(target.Alive, "native facility still exists during actual machine work");
            JsonValue origin = TilePlacement.Arguments(game.World.Space.Id, position);
            c.That(UpgradeView(game).CapturePlanTemplate("completion", origin, out PlanTemplate template).Ready,
                "native existing-target activity shares portable capture with removal");
            c.Equal("0", template.Entries[0].Arguments.Get("placement").Get("dx").Text, "native template records relative x only");
            c.Equal("0", template.Entries[0].Arguments.Get("placement").Get("dy").Text, "native template records relative y only");
            c.Equal(Codes.NotFound, UpgradeView(game).PreviewPlanTemplate(template,
                TilePlacement.Arguments(game.World.Space.Id, new TileCoord(27, 27)), "missing.native").Blocker,
                "native translated intent cannot invent another depot");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "native saved replacement", twin);
            Entity made = Find(game, "depot.enhanced");
            c.That(made != null && !target.Alive && made.Get<ITilePresence>().Tile == position, "native placement and identity use the same sole replacement coordinator");
            c.Equal(before - Fix.FromInt(2), made.Get<IInventory>().Amount(steel), "native replacement preserves stock after physically consuming its authored bill");
            c.Equal(PlanState.Done, UpgradePlan(game, id).State, "native upgrade completes existing persistent plan");
            c.Equal(game.Hash(), twin.Hash(), "native upgrade saved twin remains deterministic");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native completed replacement remains durable");
        }
        private static void ExternalEntityActivity(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = BuildUpgradeTarget(c, game);
            var owner = new ReservationOwner("proof.external-admission", 1);
            ReservationToken token = target.Get<IInventory>().ReserveIn(owner, Supply(game), Fix.One);
            long id = Submit(c, game, "proof.process", target);
            c.Equal(ResourceTransactions.ActivePromises, UpgradePlan(game, id).Blocker, "public provider start gate blocks ordinary persistent activation");
            c.Equal(0, UpgradePlan(game, id).Order, "blocked custom provider posts no order or machine claim");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "custom pending admission saves through native plan state");
            Release(game, target.Id, owner, token); Release(twin, target.Id, owner, token);
            Reach(game, () => Order(game, target.Id, "activity:proof.process")?.Node("process").Progress.WholeTicks > 0,
                "public provider real machine work", twin);
            c.That(target.Alive && target.Get<IInventory>().Amount(Supply(game)) == Fix.Zero, "public final effect cannot run before physical work completes");
            Reach(game, () => UpgradePlan(game, id).State == PlanState.Done, "public provider ordinary completion", twin);
            c.That(target.Alive && ReferenceEquals(game.World.Entities.Get(target.Id), target), "ordinary provider-defined work does not imply replacement");
            c.Equal(Fix.One, target.Get<IInventory>().Amount(Supply(game)), "public module commits its explicit authored production through native transactions");
            c.Equal(game.Hash(), twin.Hash(), "public effect admission and saved physical continuation are deterministic");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "custom completed entity activity remains durable without a scheduler branch");
        }
    }
}
