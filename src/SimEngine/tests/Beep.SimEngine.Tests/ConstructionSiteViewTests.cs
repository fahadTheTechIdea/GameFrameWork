using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ConstructionSiteViewTests
    {
        public const string CaseName = "Construction sites: authored identity private facts and saved continuation";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static void Run(Check check)
        {
            GameFactory factory = ConstructionPlanTests.Points(root =>
            {
                root.Get("entities")[0].Get("components").Get("construction").Set("site", JsonValue.Of("assembly.foundation"));
                root.Get("entities")[1].Set("id", JsonValue.Of("assembly.foundation"));
            });
            SimGame game = ConstructionPlanTests.NewPoints(factory);
            var owner = game.Seat(0);
            ConstructionView view = owner.View.Feature<ConstructionView>();
            var command = new ConstructCommand { Def = "point.tower", Placement = Json.Parse("{\"space\":\"field.points\",\"position\":12}"), Priority = 5 };
            check.Equal(Code.None, owner.Preview(command), "ordinary engine construction admission");
            owner.Submit(command);
            game.Update();
            check.That(game.Fault == null, "healthy construction update");
            var site = game.Snapshot.Entities.Single(e => e.Def == "assembly.foundation");
            ulong before = game.Hash();
            check.That(view.TryReadSite(site.Id, out var facts), "site read through existing capability contract");
            check.Equal(site.Id, facts.Site, "actual site identity");
            check.Equal("point.tower", facts.TargetDefinition, "authored result, never site-name parsing");
            check.That(facts.Order > 0, "real engine order identity");
            check.Equal(before, game.Hash(), "projection does not change physical state");
            check.That(!game.Seat(1).View.Feature<ConstructionView>().TryReadSite(site.Id, out var privateFacts), "foreign company intent withheld");
            check.Equal(default(EntityId), privateFacts.Site, "refused output cleared");
            check.That(!view.TryReadSite(default, out var missing), "missing site refused");
            check.Equal(default(EntityId), missing.Site, "missing output cleared");
            var source = game.Snapshot.Entities.First(e => e.Def == "source");
            check.That(!view.TryReadSite(source.Id, out var nonSite), "ordinary storage is not inferred as a project");
            check.Equal(default(EntityId), nonSite.Site, "non-site output cleared");
            SimGame twin = factory.Load(game.Save());
            check.Equal(game.Hash(), twin.Hash(), "saved site state identity");
            check.That(twin.Seat(0).View.Feature<ConstructionView>().TryReadSite(site.Id, out var loaded), "restored authored site facts");
            check.Equal(facts.TargetDefinition, loaded.TargetDefinition, "restored result identity");
            check.Equal(facts.Order, loaded.Order, "restored original work link");
            var orders = new List<OrderView>();
            for (int step = 0; step < 3; step++)
            {
                ulong heldTwin = twin.Hash();
                game.Update();
                check.Equal(heldTwin, twin.Hash(), "interleaved session cannot change the other site");
                twin.Update();
                check.That(game.Fault == null && twin.Fault == null, "healthy saved continuation");
                check.Equal(game.Hash(), twin.Hash(), "original construction and work owners continue identically");
                owner.View.Feature<WorkView>().CopyOrders(orders);
                check.That(orders.Any(o => o.Id == facts.Order && o.Target == facts.Site), "projection links actual visible order");
            }
            check.Equal("point.tower", facts.TargetDefinition, "retained detached facts stable after updates");
        }
    }
}
