using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class IndependentTileConstructionTests
    {
        internal const string Case = "Independent tile construction: physical material supply and native project completion";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "independent-sensor", Seed = 723, Seats = { Controller.Human, Controller.Human } });
        private static EntityView[] Entities(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Where(e => e.Def == id).ToArray();
        private static Fix Stock(SimGame game)
        {
            Fix total = Fix.Zero; var rows = new List<Facet>();
            foreach (var entity in game.Seat(0).Snapshot.Entities) {
                game.Seat(0).View.Inspect(entity.Id, rows);
                foreach (var row in rows) if (row.Key == new Code("facet.stock") && row.Subject == "steel") total += row.Value;
            }
            return total;
        }
        private static void Update(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
        private static void Run(Check c)
        {
            var factory = IndependentTileWorkTests.Factory(true); var game = New(factory);
            c.That(!game.World.Services.Has<TileMap>() && !game.World.Services.Has<IEconomy>(), "native physical construction needs neither concrete map nor business");
            c.Equal(Fix.FromInt(12), Stock(game), "only authored initial steel exists");
            var command = new ConstructCommand { Def = "sensor.facility", Placement = TilePlacement.Arguments("sensor.tiles", new TileCoord(14, 16)), Priority = 10 };
            ulong before = game.Hash(); long step = game.Clock.StepIndex;
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary construction preview uses independent placement provider");
            c.Equal(before, game.Hash(), "construction preview allocates/consumes nothing");
            c.Equal(step, game.Clock.StepIndex, "preview drives no clock");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary command posts physical construction project");
            c.Equal(0, Entities(game, "sensor.facility").Length, "accepted intent creates no instantly completed structure");
            c.Equal(1, Entities(game, "sensor.build-site").Length, "native engine creates actual authored site");
            c.That(game.Seat(0).View.Feature<ConstructionView>().TryReadSite(result.Entity, out var site), "private engine view reports actual site/work binding");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending supply/construction saves through original owners");
            bool cargoSeen = false, suppliedSave = false, workSave = false; long earned = 0; int updates = 0;
            var jobs = new List<WorkJobView>(); var nodes = new List<ActivityNodeView>();
            while (Entities(game, "sensor.facility").Length == 0 && updates++ < 240)
            {
                Update(game); Update(twin); c.Equal(game.Hash(), twin.Hash(), "physical supply/work saved twins continue identically");
                game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                bool loaded = jobs.Any(job => job.IsHaul && job.Loaded > Fix.Zero); cargoSeen |= loaded;
                if (loaded && !suppliedSave) { twin = factory.Load(game.Save()); suppliedSave = true; c.Equal(game.Hash(), twin.Hash(), "partially loaded physical materials checkpoint"); }
                if (game.Seat(0).View.Feature<WorkView>().TryCopyNodes(site.Order, nodes)) {
                    var node = nodes.Single(); earned = Math.Max(earned, node.Progress.WholeTicks);
                    if (node.Progress.WholeTicks > 0 && node.Progress.WholeTicks < node.RequiredTicks && !workSave) {
                        twin = factory.Load(game.Save()); workSave = true; c.Equal(game.Hash(), twin.Hash(), "earned incomplete construction checkpoint");
                    }
                }
                Fix stock = Stock(game);
                c.That(stock == Fix.FromInt(12) || stock == Fix.FromInt(4), "physical stock/cargo conserved apart from exact authored eight-material consumption");
            }
            var offers = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(offers);
            if (updates >= 240) throw new InvalidOperationException("construction bound reached: clock=" + game.Clock.StepIndex + " offers=" +
                string.Join(";", offers.Select(offer => offer.Kind + "/" + offer.Haul.Status + "/" + offer.Haul.Blocker)) + " jobs=" +
                string.Join(";", jobs.Select(job => job.Id + "/" + job.State + "/" + job.Loaded)) + " nodes=" +
                string.Join(";", nodes.Select(node => node.Id + "/" + node.State + "/" + node.Blocker + "/" + node.Progress.WholeTicks)));
            c.That(cargoSeen && suppliedSave && workSave, "completed project required physical loaded delivery and saved productive work");
            c.Equal(1, Entities(game, "sensor.facility").Length, "native completion creates exactly one authored structure");
            c.Equal(0, Entities(game, "sensor.build-site").Length, "native completion retires temporary site");
            c.That(earned >= 1, "completion follows observed productive work, not instant substitution");
            c.Equal(Fix.FromInt(4), Stock(game), "exact eight materials consumed with no duplicated cargo or refund");
            var fresh = New(factory); c.Equal(Fix.FromInt(12), Stock(fresh), "interleaved session retains original resources");
            c.Equal(0, Entities(fresh, "sensor.facility").Length, "interleaved session receives no completed project");
        }
    }
}
