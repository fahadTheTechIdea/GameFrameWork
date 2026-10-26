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
    internal static class ActivityStartRuleTests
    {
        internal const string Case = "Activity start rules: pure binding admission and fail-closed settlement";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(ActivityStartRuleProofModule probe)
        {
            var definition = new GameDefinition("start-rule-proof", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new WorkModule(), new WorldSetupModule(), new LocalWorkProofModule(), probe });
            var manifest = Json.Parse(FrameworkProofContent.Read("local-work-manifest.json"));
            manifest.Get("modules").Add(Json.Parse("""
                {"id":"proof.activity-start","version":"1.0.0","requires":[{"id":"sim.work","version":"1.0.0"}],"documents":["activity-start-rules.json"]}
                """));
            var local = Json.Parse(FrameworkProofContent.Read("local-work.json"));
            local.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(probe.Id));
            var files = new PreloadedFileSource().Add("manifest.json", manifest.ToString())
                .Add("framework/access.json", FrameworkProofContent.Access)
                .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("local-work.json", local.ToString())
                .Add("activity-start-rules.json", FrameworkProofContent.Read("activity-start-rules.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static PostOrderCommand Intent(SimGame game) => new PostOrderCommand { Plan = "settle", Target = game.Seat(0).Snapshot.Entities.Single().Id };
        private static void Run(Check c)
        {
            var probe = new ActivityStartRuleProofModule(); var factory = Factory(probe); var game = OptionalWorkTests.New(factory);
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "optional start rule requires no finance module");
            ulong hash = game.Hash();
            for (int i = 0; i < 3; i++) c.Equal(Code.None, game.Seat(0).Preview(Intent(game)), "normal pure admission");
            c.Equal(hash, game.Hash(), "repeated admission leaves native state unchanged"); c.Equal(0, probe.Commits, "preview never settles");
            game.Seat(0).Submit(Intent(game)); game.Update();
            c.That(game.Fault == null, "normal start remains healthy"); c.Equal(1, probe.Commits, "one successful start settlement");
            c.Equal(Codes.AlreadyDone, game.Seat(0).Preview(Intent(game)), "active work refuses another order");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial native order saves without second start state");
            for (int i = 0; i < 8; i++) { game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "native saved work continuation"); }
            c.Equal(1, probe.Commits, "reload/updates do not settle the start again");
            c.Equal(0, game.Seat(0).View.Feature<WorkView>().ActiveOrders(Intent(game).Target), "original native work completes over elapsed game time");
            var stock = new List<Facet>(); game.Seat(0).View.Inspect(Intent(game).Target, stock);
            c.Equal(Fix.FromInt(8), stock.Single(f => f.Key == Storage.FacetStock && f.Subject == "kit").Value, "physical materials consumed once by original work owner");

            foreach (StartRuleFault fault in new[] { StartRuleFault.BindingClock, StartRuleFault.CheckClock, StartRuleFault.CheckTarget, StartRuleFault.CheckSwap })
            {
                probe = new ActivityStartRuleProofModule(); factory = Factory(probe); game = OptionalWorkTests.New(factory);
                var command = Intent(game); game.Seat(0).Preview(command); probe.Arm(fault);
                c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(command), "bad pure callback refuses " + fault);
                c.That(game.Fault != null, "preview latches original session fault " + fault);
                var first = game.Fault; c.Throws<InvalidOperationException>(() => game.Save(), "faulted preview cannot save " + fault);
                c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(command), "faulted session refuses later preview " + fault);
                c.That(ReferenceEquals(first, game.Fault), "first fault stays authoritative " + fault);
                c.Equal(0, probe.Commits, "bad preview never settles " + fault);
            }
            foreach (StartRuleFault fault in new[] { StartRuleFault.CommitThrow, StartRuleFault.CommitRefuse, StartRuleFault.CommitSwap })
            {
                probe = new ActivityStartRuleProofModule(); factory = Factory(probe); game = OptionalWorkTests.New(factory); probe.Arm(fault);
                c.Throws<InvalidOperationException>(() => probe.InjectDirectSettlementFault(), "direct failed start throws " + fault);
                c.That(game.Fault != null, "module-caught direct settlement cannot remain healthy " + fault);
                if (fault == StartRuleFault.CommitThrow) c.That(game.Fault.Message.Contains("proof.start-first-error"), "first callback exception preserved");
                c.Throws<InvalidOperationException>(() => game.Save(), "partial failed settlement cannot save " + fault);
            }
        }
    }
}
