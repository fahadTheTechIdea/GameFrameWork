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
    // Public player intent and detached facts only; native JSON/engine contracts own startup and execution.
    internal static class NativeNonspatialStartupTests
    {
        internal const string Case = "Native startup: nonspatial work, process and resource contracts with saved continuation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static EntityView Entity(SimGame game, string prototype)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype);
        private static Fix Stock(SimGame game, string prototype, string resource)
        {
            var facts = new List<Facet>();
            if (!game.Seat(0).View.Inspect(Entity(game, prototype).Id, facts))
                throw new InvalidOperationException("missing public inspection " + prototype);
            return facts.Single(f => f.Key == Storage.FacetStock && f.Subject == resource).Value;
        }
        private static OrderView Order(SimGame game)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            return orders.Single();
        }
        private static TransformationSnapshot Process(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(
                Entity(game, "assembler").Id, new Code("transformation"), out var state))
                throw new InvalidOperationException("missing public process facts");
            return state;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("native startup continuation at " + game.Tick + ": " + game.Fault + "/" + twin?.Fault);
        }
        private static void Run(Check c)
        {
            Work(c, false); Work(c, true); Manufacturing(c);
            foreach (string invalid in new[] { "reference", "cycle", "company", "obsolete", "placement" })
            {
                var module = new LocalWorkProofModule();
                var root = Json.Parse(FrameworkProofContent.Read("local-work.json"));
                if (invalid == "reference") root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("absent"));
                if (invalid == "cycle") root.Get("entity_setups")[0].Get("after").Add(JsonValue.Of("local-start"));
                if (invalid == "company") root.Get("entity_setups")[0].Set("company", JsonValue.Number("1"));
                if (invalid == "obsolete") root.Set("work_starts", JsonValue.NewArray());
                if (invalid == "placement") root.Get("entity_setups")[0].Set("placement", JsonValue.NewObject());
                c.Throws<ContentException>(() => OptionalWorkTests.New(OptionalWorkTests.Factory(module, root.ToString())), "native startup refuses " + invalid);
                c.Equal(0, module.Initializations, invalid + " refuses before provider Initialize");
            }
        }
        private static void Startup(Check c, SimGame game, string prototype, Fix opening, string resource)
        {
            c.That(game.Modules.Contains(WorldSetupModule.ModuleId), "scenario explicitly selects the native startup owner");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && !game.Modules.Contains(TileNavigationModule.ModuleId), "startup adds neither money nor a platform/map dependency");
            c.Equal(1, game.Seat(0).Snapshot.Entities.Count, "selected authored entry spawns exactly once");
            c.That(game.Seat(0).Snapshot.Space == null && Entity(game, prototype).Spatial == null, "nonspatial startup invents no coordinates");
            c.Equal(opening, Stock(game, prototype, resource), "opening stock is authored and created by the actual engine provider");
        }
        private static void Work(Check c, bool resource)
        {
            var local = new LocalWorkProofModule(); var ore = new ResourceProofModule();
            var factory = resource ? ResourceCatalogTests.Factory(ore) : OptionalWorkTests.Factory(local);
            string scenario = resource ? "resource-work" : "local-work", prototype = resource ? "ore-bin" : "local",
                material = resource ? "planetx.ore" : "kit", activity = resource ? "refine" : "settle";
            Fix opening = Fix.FromInt(resource ? 3 : 10), remaining = resource ? Fix.Parse("2.5") : Fix.FromInt(8);
            var game = factory.NewGame(new NewGameRequest { Scenario = scenario, Seed = 791 });
            Startup(c, game, prototype, opening, material);
            c.Equal(1, resource ? ore.Initializations : local.Initializations, "provider initializes once after native startup");
            var recording = game.RecordReplay(new ReplayLimits(1024 * 1024, 50));
            var post = new PostOrderCommand { Plan = activity, Target = Entity(game, prototype).Id };
            ulong hash = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(post), "native work preview accepts normal player intent");
            c.Equal(hash, game.Hash(), "work preview advances no startup/work/stock/clock state");
            c.That(TestKit.Order(game, 0, post).Accepted, "native timed work starts through an ordinary command");
            c.Equal(opening, Stock(game, prototype, material), "accepted intent alone does not spend productive inputs");
            c.Equal(6L, Order(game).CurrentNeeded, "duration resolves through the sole game clock");
            c.Equal(WorkProgress.Zero, Order(game).CurrentProgress, "starting work is not instant completion");
            Step(game);
            c.Equal(remaining, Stock(game, prototype, material), "first productive update consumes actual authored inputs once");
            Step(game);
            c.Equal(2L, Order(game).CurrentProgress.WholeTicks, "Game.Update owns earned work");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "in-progress startup/work state restores exactly");
            c.Equal(1, twin.Seat(0).Snapshot.Entities.Count, "load does not rerun the startup graph");
            c.Equal(remaining, Stock(twin, prototype, material), "load does not grant opening inventory again");
            for (int i = 0; i < 4; i++) Step(game, twin);
            c.Equal(0, game.Seat(0).View.Feature<WorkView>().ActiveOrders(Entity(game, prototype).Id), "six physical updates finish authored work");
            c.Equal(remaining, Stock(game, prototype, material), "completion does not charge twice");
            c.That(factory.Describe(game.Save()).Compatible, "native startup module manifest remains self-describing");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), new ReplayLimits(1024 * 1024, 50)).Success, "native work/startup commands replay identically");
        }
        private static void Manufacturing(Check c)
        {
            var module = new ProcessProofModule(); var factory = OptionalProcessTests.Factory(module);
            var game = factory.NewGame(new NewGameRequest { Scenario = "process", Seed = 793 });
            Startup(c, game, "assembler", Fix.FromInt(20), "supply");
            c.Equal(1, module.Initializations, "process provider initializes once after native startup");
            var recording = game.RecordReplay(new ReplayLimits(1024 * 1024, 50));
            var queue = new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = Entity(game, "assembler").Id, Transformation = "build", Count = 1 };
            ulong hash = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(queue), "process preview accepts ordinary native queue intent");
            c.Equal(hash, game.Hash(), "process preview mutates no state");
            c.That(TestKit.Order(game, 0, queue).Accepted, "process starts through the existing queue owner");
            c.Equal(Fix.FromInt(17), Stock(game, "assembler", "supply"), "real process inputs consumed once");
            c.Equal(1L, Process(game).Progress.WholeTicks, "process earns only one update on command execution");
            c.Equal(6L, Process(game).Required, "process still needs its authored duration");
            c.Equal(1, game.Seat(0).Snapshot.Entities.Count, "no completed entity is fabricated at startup");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "active process saves exactly");
            c.Equal(1, twin.Seat(0).Snapshot.Entities.Count, "process restore does not duplicate startup entities");
            c.Equal(Fix.FromInt(17), Stock(twin, "assembler", "supply"), "restored process retains actual spent inputs");
            for (int i = 0; i < 5; i++) Step(game, twin);
            c.Equal(1, game.Seat(0).Snapshot.Entities.Count(e => e.Def == "token"), "one real output appears after six updates");
            c.Equal(Fix.FromInt(18), Stock(game, "assembler", "supply"), "real batch output commits once");
            c.Equal(0, Process(game).QueueCount, "authoritative native queue completes");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), new ReplayLimits(1024 * 1024, 50)).Success, "native process/startup commands replay identically");
        }
    }
}
