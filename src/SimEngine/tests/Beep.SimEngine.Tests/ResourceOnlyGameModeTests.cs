using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    /// <summary>Player inputs and observations only. Existing engine modules execute all work/process rules.</summary>
    public static class ResourceOnlyGameModeTests
    {
        public static void Register() => TestRunner.Add("Game modes: physical manufacturing and work without business or Tycoon", Physical);

        internal static GameFactory Factory(bool manufacture, MonetaryCostCategory? category = null, bool money = false,
            bool costs = false, int openingCash = 0, bool credit = false, Action<JsonValue> edit = null, bool logistics = false)
        {
            // Registering optional code is not selecting it. Unpriced scenarios omit Tycoon;
            // native monetary authoring selects its owner, which stays dormant when its flags are off.
            var modules = new List<GameModule> { new ContentAccessModule(), new InventoryModule(), new WorldSetupModule(),
                new ProbeModule("proof.modes", new ModuleVersion(1, 0, 0)), new TycoonModule() };
            string contentFile, manifestFile, scenario;
            if (manufacture)
            {
                modules.Add(new OperationsModule()); modules.Add(new TransformationModule()); modules.Add(new ProcessProofModule());
                contentFile = "process.json"; manifestFile = "process-manifest.json"; scenario = "process";
            }
            else
            {
                modules.Add(new MotionModule()); modules.Add(new WorkModule()); modules.Add(new LocalWorkProofModule());
                contentFile = "local-work.json"; manifestFile = "local-work-manifest.json"; scenario = "local-work";
            }
            JsonValue content = Json.Parse(FrameworkProofContent.Read(contentFile));
            JsonValue selected = content.Get("scenarios")[0];
            selected.Set("game_mode", JsonValue.Of("survival")); selected.Get("modules").Add(JsonValue.Of("proof.modes"));
            JsonValue modes = Json.Parse(FrameworkProofContent.Read("game-modes.json"));
            JsonValue accounts = Json.Parse(FrameworkProofContent.Read("business-accounts.json"));
            if (category.HasValue)
            {
                string authoredCategory = category.Value.ToString().ToLowerInvariant();
                string costProperty = "business." + authoredCategory + "_costs.enabled";
                JsonValue profile = Json.Parse("{\"id\":\"cost-proof\",\"name\":\"Cost proof\",\"properties\":{}}");
                profile.Get("properties").Set("business.money.enabled", JsonValue.Of(money));
                profile.Get("properties").Set(costProperty, JsonValue.Of(costs));
                modes.Get("game_modes").Add(profile); selected.Set("game_mode", JsonValue.Of("cost-proof"));
                if (manufacture) accounts.Set("transformation_cost_bindings", Json.Parse("[{\"id\":\"resource-only.manufacturing-cost\",\"transformation\":\"" + content.Get("transformations")[0].Get("id").Text + "\",\"amount\":7}]"));
                else
                {
                    content.Set("work_cost_requirements", Json.Parse("[{\"id\":\"local.money\",\"amount\":7,\"credit\":" + (credit ? "true" : "false") + ",\"category\":\"" + authoredCategory + "\"}]"));
                    content.Set("work_requirement_groups", Json.Parse("[{\"id\":\"local.ready\",\"mode\":\"all\",\"entries\":[{\"id\":\"materials\",\"requirement\":\"proof.local-work.settle.settling.inputs\"},{\"id\":\"money\",\"requirement\":\"local.money\"}]}]"));
                    content.Get("activities")[0].Get("nodes")[0].Set("requirement", JsonValue.Of("local.ready"));
                }
            }
            // Native monetary authoring belongs to Tycoon even when its monetary gates are off.
            // Unpriced physical scenarios still omit the module. Selection alone creates no accounts.
            if (money || category.HasValue)
                selected.Get("modules").Add(JsonValue.Of(TycoonModule.ModuleId));
            if (logistics)
            {
                modules.Add(new LogisticsModule()); selected.Get("modules").Add(JsonValue.Of(LogisticsModule.ModuleId));
            }
            if (money)
            {
                if (selected.Get("settings") == null) selected.Set("settings", Json.Parse("{}"));
                selected.Get("settings").Set(TycoonModule.ModuleId, Json.Parse("{\"account_policy\":\"colony.accounts\"}"));
                content.Get("units").Add(Json.Parse("{\"id\":\"unit.money\",\"dimension\":\"currency\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":false}"));
                accounts.Get("business_accounts")[0].Set("opening_cash", Json.Parse(openingCash.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            edit?.Invoke(content);
            if (selected.Get("id").Text != scenario) throw new InvalidOperationException("unexpected authored fixture scenario");
            JsonValue manifest = Json.Parse(FrameworkProofContent.Read(manifestFile));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"proof.modes\",\"version\":\"1.0.0\",\"documents\":[\"game-mode-properties.json\",\"game-mode-proof-properties.json\",\"game-modes.json\"]}"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.tycoon\",\"version\":\"1.0.0\",\"documents\":" + (money || manufacture && category.HasValue ? "[\"business-accounts.json\"]" : "[]") + "}"));
            if (logistics) manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.logistics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
            var definition = new GameDefinition("resource-only-modes", modules);
            var files = new PreloadedFileSource().Add("manifest.json", manifest.ToString()).Add(contentFile, content.ToString())
                .Add("framework/access.json", FrameworkProofContent.Access)
                .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("game-mode-properties.json", FrameworkProofContent.Read("game-mode-properties.json"))
                .Add("game-mode-proof-properties.json", FrameworkProofContent.Read("game-mode-proof-properties.json"))
                .Add("game-modes.json", modes.ToString()).Add("business-accounts.json", accounts.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static EntityId Find(SimGame game, string prototype)
        {
            var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(new PlayerId(0), entities);
            foreach (EntityView entity in entities) if (entity.Def == prototype) return entity.Id;
            throw new InvalidOperationException("missing authored " + prototype);
        }
        private static Fix Stock(SimGame game, EntityId entity, string resource)
            => game.World.Entities.Get(entity).Get<IInventory>().Amount(game.World.Content.ResourceId(resource));
        private static void NoBusiness(Check c, SimGame game)
        {
            c.Equal("survival", game.GameMode.ModeId, "physical session explicitly selects the authored survival configuration");
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessAccounts>() &&
                !game.World.Services.Has<IResourceValuation>() && !game.World.Services.Has<IResourceProductionSettlement>(),
                "actual work/manufacturing session has no money, valuation, income or fake account service");
            bool selected = false; foreach (string module in game.Modules) if (module == TycoonModule.ModuleId) selected = true;
            c.That(!selected, "business code availability does not select the optional module");
            c.That(game.GameMode.TryGet(new Code("business.tycoon.enabled"), out var tycoon) && !tycoon.Boolean,
                "Tycoon is explicitly off without disabling physical mechanics");
        }
        private static OrderView Active(SimGame game)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            if (orders.Count != 1) throw new InvalidOperationException("expected one authored work order");
            return orders[0];
        }
        private static int Count(SimGame game, string prototype)
        {
            var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(new PlayerId(0), entities);
            int count = 0; foreach (EntityView entity in entities) if (entity.Def == prototype) count++;
            return count;
        }
        private static void Physical(Check c)
        {
            var workFactory = Factory(false); var work = workFactory.NewGame(new NewGameRequest { Scenario = "local-work", Seed = 29 });
            NoBusiness(c, work); EntityId target = Find(work, "local");
            ulong before = work.Hash(); var command = new PostOrderCommand { Plan = "settle", Target = target };
            c.Equal(Code.None, work.Seat(0).Preview(command), "normal resource-only work preview is available without Tycoon");
            c.Equal(before, work.Hash(), "preview consumes neither inputs nor time");
            c.That(TestKit.Order(work, 0, command).Accepted, "ordinary player command starts the existing work engine");
            c.Equal(Fix.FromInt(10), Stock(work, target, "kit"), "queued work retains actual materials until productive advancement");
            c.Equal(6L, Active(work).CurrentNeeded, "physical work still needs the authored elapsed game time");
            c.Equal(WorkProgress.Zero, Active(work).CurrentProgress, "money-off does not make work instant");
            work.RunTicks(2); c.Equal(2L, Active(work).CurrentProgress.WholeTicks, "the existing game loop advances real work");
            c.Equal(Fix.FromInt(8), Stock(work, target, "kit"), "actual productive work consumes authored materials once");
            var workTwin = workFactory.Load(work.Save()); work.RunTicks(4); workTwin.RunTicks(4);
            c.Equal(0, work.Seat(0).View.Feature<WorkView>().ActiveOrders(target), "resource-only work completes normally");
            c.Equal(Fix.FromInt(8), Stock(work, target, "kit"), "saved continuation does not consume materials twice");
            c.Equal(work.Hash(), workTwin.Hash(), "work and mode identity continue identically without business");

            var processFactory = Factory(true); var process = processFactory.NewGame(new NewGameRequest { Scenario = "process", Seed = 31 });
            NoBusiness(c, process); EntityId assembler = Find(process, "assembler");
            c.That(TestKit.Order(process, 0, new EnqueueTransformationCommand
                { Transformer = assembler, Capability = new Code("transformation"), Transformation = "build", Count = 1 }).Accepted,
                "manufacturing is an ordinary resource/machine command, not a Tycoon action");
            c.Equal(Fix.FromInt(17), Stock(process, assembler, "supply"), "engine moves actual recipe inputs into work in progress");
            c.Equal(0, Count(process, "token"), "machine output is not spawned instantly at enqueue");
            process.RunTicks(2); c.Equal(0, Count(process, "token"), "manufacturing remains incomplete before its required duration");
            var processTwin = processFactory.Load(process.Save()); process.RunTicks(3); processTwin.RunTicks(3);
            c.Equal(1, Count(process, "token"), "physical timed manufacturing creates its authored entity output");
            c.Equal(Fix.FromInt(18), Stock(process, assembler, "supply"), "completed recipe conserves authored consumed/produced materials");
            c.Equal(process.Hash(), processTwin.Hash(), "manufacturing saves and finishes without a market/account service");
            c.That(work.Fault == null && process.Fault == null, "neither workflow attempts to access absent Tycoon/business state");
        }
    }
}
