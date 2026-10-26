using System;
using System.Linq;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Runs inside the two exact ResourceRecoveryTests suites, not an additional broad gate.
    internal static partial class PointRecoveryTests
    {
        private static GameFactory Factory(Action<JsonValue> edit = null, string recovery = "point.recovery", Action<JsonValue> scenario = null, bool automaticRecovery = false)
            => ConstructionPlanTests.Points(edit, recovery, FrameworkProofContent.Read("point-recovery.json"), FrameworkProofContent.Read("recovery-flows.json"), selected =>
            {
                scenario?.Invoke(selected);
                if (!automaticRecovery) return;
                selected.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                selected.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"recovery.high\"}"));
            }, additionalModule: automaticRecovery ? new LogisticsRecoveryPolicyModule() : null,
                moduleDeclaration: automaticRecovery ? "{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}" : null,
                moduleDocument: automaticRecovery ? FrameworkProofContent.Read("logistics-recovery.json") : null);
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 733 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); request.Mods.Add(ResourceFlowsModule.ModuleId);
            return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; return null; }
        private static List<Entity> Containers(SimGame game)
        {
            var list = new List<Entity>(); string id = game.Seat(0).View.Feature<ConstructionView>().RecoveryContainer;
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) list.Add(entity); return list;
        }
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("point recovery tick=" + game.Tick + " " + game.Fault); }
        private static void Reach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 720 && !ready(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!ready())
            {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException(stage + " tick=" + game.Tick + " containers=" + Containers(game).Count + " jobs=" + string.Join(";", jobs.ConvertAll(j => j.Id + "/" + j.State + "/" + j.Blocker)));
            }
        }
        private static ResourceId Resource(SimGame game) => game.World.Content.ResourceId("supply");
        private static Fix Total(SimGame game)
        { Fix total = Fix.Zero; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Owner == new PlayerId(0)) total += entity.Get<IInventory>()?.Amount(Resource(game)) ?? Fix.Zero; return total; }
        private static ResourceFlowPage Flows(SimGame game, long cursor = 0) => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor);
        private static CommandResult Build(SimGame game)
            => TestKit.Order(game, 0, new SubmitPlanCommand { Intent = new PlanIntent("build:point.tower", Content.Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":12}}"), "field", 70, false) });

        internal static void Admission(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); World world = game.World;
            IResourceRecovery recovery = world.Services.Get<IResourceRecovery>(); ConstructionView view = game.Seat(0).View.Feature<ConstructionView>();
            c.That(ReferenceEquals(world.Services.Providers<ConstructionService>().Single(), recovery), "point recovery aliases the same construction owner");
            c.That(ReferenceEquals(recovery.Definition, world.Content.Get<ResourceRecoveryDef>("point.recovery")), "selected recovery profile aliases exact master catalog");
            c.That(recovery.Definition.Container.AuthoringPath.Contains("point-construction.json:entities"), "container prototype explicitly master-authored rather than generated");
            c.Equal("point.recovery-container", view.RecoveryContainer, "immutable view exposes content identity not executable geometry");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "breaking semantics have explicit identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no old save reader");
            Entity source = Find(game, "source"); ulong hash = game.Hash();
            c.Equal(Code.None, view.CheckRecovery(source.Id), "point stock admits exact native recovery without tiles");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<ConstructionView>().CheckRecovery(source.Id), "foreign company gets no private recovery diagnostics");
            c.Equal(hash, game.Hash(), "repeated private/pure recovery previews allocate no entities or receipts");
            c.Equal(0, Containers(game).Count, "preflight creates no ghost buffers");
            var requester = new ReservationOwner("proof.yield", 1); var bill = new Bill(new[] { new ResourceAmount(Resource(game), Fix.One) });
            c.Equal(Codes.OutOfBounds, recovery.CheckYield(world, new PlayerId(0), new PointPlacement(world.Space.Id, Fix.FromInt(101)), bill, requester, new Code("proof.yield")), "selected geometry owns recovery bounds");
            c.Equal(Codes.InvalidArgument, recovery.CheckYield(world, new PlayerId(0), new PointPlacement("foreign", Fix.One), bill, requester, new Code("proof.yield")), "foreign geometry does not silently convert");
            c.Equal(ConstructionService.RecoveryIncompatible, recovery.CheckYield(world, new PlayerId(0), new PointPlacement(world.Space.Id, Fix.One), new Bill(new[] { new ResourceAmount(Resource(game), Fix.FromRatio(3, 2)) }), requester, new Code("proof.yield")), "fractional count yield explicitly refuses");
            c.Equal(hash, game.Hash(), "bounds/quantum refusals leave full identity unchanged");
            SimGame other = New(factory); c.Equal(Codes.InvalidArgument, recovery.CheckEvacuation(other.World, Find(other, "source")), "recovery cannot use another session's geometry/profile");
            c.Equal(hash, game.Hash(), "foreign session check leaves original untouched");

            foreach (string fault in new[] { "space", "container", "initial", "inventory", "motion" })
                c.Throws<ContentException>(() => New(Factory(root =>
                {
                    JsonValue profile = root.Get("resource_recovery_profiles")[0], container = root.Get("entities")[2].Get("components");
                    if (fault == "space") profile.Set("space", JsonValue.Of("unknown"));
                    if (fault == "container") profile.Set("container", JsonValue.Of("source"));
                    if (fault == "initial") container.Get("storage").Set("initial_stock", Content.Json.Parse("{\"supply\":1}"));
                    if (fault == "inventory") container.Remove("storage");
                    if (fault == "motion") container.Set("point_motion", Content.Json.Parse("{\"rate\":1,\"states\":{\"moving\":\"travel\",\"stopped\":\"blocked\"}}"));
                })), "invalid recovery " + fault + " refuses before Initialize");
            c.Throws<ContentException>(() => New(Factory(recovery: "unknown")), "unknown recovery profile refuses");
            c.Throws<ContentException>(() => New(Factory(scenario: scenario => scenario.Get("settings").Get(ConstructionModule.ModuleId).Remove("recovery"))), "missing selection never fabricates a default");
            SimGame none = New(Factory(recovery: null)); hash = none.Hash();
            c.Equal(null, none.Seat(0).View.Feature<ConstructionView>().RecoveryContainer, "explicit null has no recovery prototype");
            c.Equal(ResourceTransactions.MissingRecovery, none.Seat(0).View.Feature<ConstructionView>().CheckRecovery(Find(none, "source").Id), "stocked missing recovery stays typed and retained");
            c.Equal(Code.None, none.Seat(0).View.Feature<ConstructionView>().CheckRecovery(Find(none, "destination").Id), "empty removal needs no invented recovery prototype");
            c.Equal(hash, none.Hash(), "explicit missing recovery diagnostics are pure");
            SimGame filtered = New(Factory(root => root.Get("entities")[2].Get("components").Get("storage").Set("phases", Content.Json.Parse("[\"liquid\"]"))));
            hash = filtered.Hash(); c.Equal(ConstructionService.RecoveryIncompatible, filtered.Seat(0).View.Feature<ConstructionView>().CheckRecovery(Find(filtered, "source").Id), "actual resource filter refuses the whole stock before mutation");
            c.Equal(hash, filtered.Hash(), "incompatible recovery allocates/spends nothing");
        }

        internal static void Physical(Check c)
        {
            GameFactory factory = Factory(automaticRecovery: true); SimGame game = New(factory); EmergencyPreemptionTests.Configure(c, game);
            Entity source = Find(game, "source"); long cursor = Flows(game).Latest;
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "point.recover", Target = source.Id, Priority = 90 }).Accepted, "player posts authored timed point recovery activity");
            c.Equal(0, Containers(game).Count, "posting physical recovery does not instantly unpack stock");
            SimGame twin = factory.Load(game.Save()); long start = game.Tick;
            Reach(game, () => Containers(game).Count != 0, "timed source recovery", twin);
            c.That(game.Tick > start + 1, "machine recovery earns actual elapsed work before disposition");
            c.Equal(game.Hash(), twin.Hash(), "saved machine recovery creates the same actual buffers/receipts");
            c.Equal(5, Containers(game).Count, "twenty source units split into five authored capacity-four containers");
            c.Equal(Fix.Zero, source.Get<IInventory>().Amount(Resource(game)), "real source emptied via owned exact receipts");
            c.Equal(Fix.FromInt(20), Total(game), "live recovery conserves stock instead of creating salvage");
            foreach (Entity container in Containers(game)) c.Equal(Fix.Zero, ((PointSnapshot)container.Get<ISpatialPresence>().Capture()).Position, "containers stay at actual source point without tile conversion");
            ResourceFlowPage page = Flows(game, cursor); Fix moved = Fix.Zero;
            foreach (var entry in page.Entries) { c.Equal(ResourceFlowKind.StockTransfer, entry.Item.Kind, "recovery emits transfer not produced stock"); moved += entry.Item.Amount; }
            c.Equal(Fix.FromInt(20), moved, "verified stock-transfer facts cover every recovered unit");
            Reach(game, () => Containers(game).Count == 0, "physical recovery loading and delivery", twin);
            c.Equal(Fix.FromInt(20), Total(game), "real later hauling loses no recovered quantity"); c.Equal(game.Hash(), twin.Hash(), "saved recovery/handling continues identically");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed point recovery is durable");
            Cancelled(c); Constructed(c);
        }

        private static void Cancelled(Check c)
        {
            GameFactory factory = Factory(root => root.Get("activities")[0].Set("nodes", Content.Json.Parse("[{\"id\":\"build\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":1,\"unit\":\"hour\"},\"requirement\":\"point.build-inputs\"}]")), automaticRecovery: true);
            SimGame game = New(factory); EmergencyPreemptionTests.Configure(c, game, haulOnly: true);
            CommandResult submitted = Build(game); c.That(submitted.Accepted, "player plans site while machine can haul but cannot construct"); Entity site = Find(game, "point.tower-site");
            Reach(game, () => site.Get<IInventory>().Amount(Resource(game)) == Fix.FromInt(8) && site.Get<IInventory>().Reservations.Count == 0 && Find(game, "machine").Get<IWorker>().Job == 0, "actual materials reach unbuilt site");
            c.That(game.Tick > 12 && Find(game, "point.tower") == null, "hauling travels to point site without instant building");
            var owner = new ReservationOwner("proof.foreign-promise", 1); IInventory inventory = site.Get<IInventory>(); ReservationToken token = inventory.ReserveIn(owner, Resource(game), Fix.One);
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { submitted.Arg0 }) }).Accepted, "player cancels actual stocked point site");
            Order order = game.World.Services.Get<WorkBoard>().Order(site.Get<IConstructionSite>().Order);
            c.That(site.Alive && order.State == OrderState.Cancelled, "native ended order retains stock and foreign promises");
            c.Equal(ResourceTransactions.ActivePromises, order.Blocker, "native retained site reports current promise blocker");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "blocked point cancellation and real claims save");
            inventory.ReleaseIn(owner, token); twin.World.Entities.Get(site.Id).Get<IInventory>().ReleaseIn(owner, token); long cursor = Flows(game).Latest;
            Tick(game); Tick(twin); c.That(!site.Alive, "same cleanup owner evacuates point stock after actual release");
            c.Equal(2, Containers(game).Count, "eight delivered units move to two real local containers");
            foreach (Entity container in Containers(game)) c.Equal(Fix.FromInt(12), ((PointSnapshot)container.Get<ISpatialPresence>().Capture()).Position, "cancelled stock remains at actual site until hauled");
            c.Equal(Fix.FromInt(20), Total(game), "cancelled construction neither destroys nor refunds resources");
            foreach (var entry in Flows(game, cursor).Entries) c.Equal(ResourceFlowKind.StockTransfer, entry.Item.Kind, "cancelled actual stock never emits fabricated yield");
            c.Equal(game.Hash(), twin.Hash(), "saved stock disposition uses identical native receipts");
            Reach(game, () => Containers(game).Count == 0, "physical cancelled-site stock return", twin);
            c.Equal(Fix.FromInt(20), Total(game), "physical return conserves all cancelled inputs"); c.Equal(game.Hash(), twin.Hash(), "saved point cancellation and hauling continue identically");
        }

        private static void Constructed(Check c)
        {
            GameFactory factory = Factory(root =>
            {
                JsonValue construction = root.Get("entities")[0].Get("components").Get("construction");
                root.Get("activities")[0].Get("nodes")[1].Set("requirement", JsonValue.Of("point.surplus-inputs"));
                construction.Get("demolish").Set("salvage", Content.Json.Parse("{\"supply\":6}"));
            }, automaticRecovery: true);
            SimGame game = New(factory); EmergencyPreemptionTests.Configure(c, game); c.That(Build(game).Accepted, "player constructs with consumable inputs and physical held tools");
            Entity site = Find(game, "point.tower-site"); Order order = game.World.Services.Get<WorkBoard>().Order(site.Get<IConstructionSite>().Order);
            Reach(game, () => !order.Nodes[1].Progress.Equals(WorkProgress.Zero), "actual input consumption and tool-backed construction");
            c.Equal(Fix.FromInt(4), site.Get<IInventory>().Amount(Resource(game)), "four actual held tools remain after eight inputs are consumed");
            c.Equal(Fix.FromInt(12), Total(game), "authored construction consumes exactly eight actual source units");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => Find(game, "point.tower") != null, "point completion with genuine surplus", twin);
            c.That(!site.Alive, "completed site empties actual tool surplus before removal");
            c.Equal(1, Containers(game).Count, "released tools move to one real capacity-four container");
            c.Equal(Fix.FromInt(12), ((PointSnapshot)Containers(game)[0].Get<ISpatialPresence>().Capture()).Position, "surplus remains at completed point site");
            c.Equal(game.Hash(), twin.Hash(), "saved point build and real surplus recovery match");
            Reach(game, () => Containers(game).Count == 0, "physical tool surplus return", twin);
            Entity tower = Find(game, "point.tower"); ulong hash = game.Hash(); var demolish = new DemolishCommand { Target = tower.Id };
            c.Equal(Code.None, game.Seat(0).Preview(demolish), "point demolition shares generic geometry/native admission"); c.Equal(hash, game.Hash(), "demolition preview is pure");
            long start = game.Tick; c.That(TestKit.Order(game, 0, demolish).Accepted, "player commands real point demolition");
            c.That(tower.Alive, "demolition submission cannot instantly remove structure"); twin = factory.Load(game.Save()); long cursor = Flows(game).Latest;
            Reach(game, () => !tower.Alive, "timed machine demolition and native authored salvage", twin);
            c.That(game.Tick > start + 1, "point demolition takes actual machine travel/work time");
            c.Equal(2, Containers(game).Count, "six authored salvage units split into four and two");
            c.Equal(Fix.FromInt(18), Total(game), "authored yield adds six, distinct from twelve surviving real inputs/tools");
            Fix produced = Fix.Zero; foreach (var entry in Flows(game, cursor).Entries) if (entry.Item.Cause == new Code("demolish:point.tower")) { c.Equal(ResourceFlowKind.Produced, entry.Item.Kind, "structural salvage is explicit lifecycle yield"); produced += entry.Item.Amount; }
            c.Equal(Fix.FromInt(6), produced, "authored salvage cause accounts for exactly six native units"); c.Equal(game.Hash(), twin.Hash(), "saved point demolition matches exactly");
            Reach(game, () => Containers(game).Count == 0, "physical structural salvage delivery", twin);
            c.Equal(Fix.FromInt(18), Total(game), "later hauling preserves salvage without duplicating consumed inputs"); c.Equal(game.Hash(), twin.Hash(), "saved structural yield and real delivery continue identically");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "finished point build/recovery/demolition chain saves durably");
        }
    }
}
