using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class FrontierCostFeasibilityTests
    {
        public const string Case = "Response frontier costs: optional combined funding, held obligations and physical recovery";
        private static readonly Code Source = new Code("damage");
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Fixture(int opening = 160, bool survival = false, bool lifecycle = false)
            => WorkBudgetTests.Factory(opening, survival, lifecycle, incidents: true,
                physicalDocuments: new[] { "prepared-frontier-costs.json" }, startupProfiles: new[] { "prepared-frontier-costs.start" },
                physicalEdit: root => root.Get("scenarios")[0].Set("seats", Content.JsonValue.Number("2")));
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "purchase-haul", Seed = 617 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id = "frontier.site")
            => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id);
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static IncidentPreparednessSnapshot Read(SimGame game, string target = "frontier.site")
        {
            Code code = game.Seat(0).View.Feature<IncidentsView>().ReadPreparedness(Find(game, target).Id, Source, out var snapshot);
            if (!code.IsNone) throw new InvalidOperationException("cost frontier refused: " + code); return snapshot;
        }
        private static BusinessAccountSnapshot Account(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var snapshot);
            if (!code.IsNone) throw new InvalidOperationException("account refused: " + code); return snapshot;
        }
        private static Fix Stock(SimGame game) => Find(game).Get<IOutletPort>().Amount(game.World.Content.ResourceId("supply"));
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("cost time=" + game.Tick + " " + game.Fault);
            if (twin != null) Step(twin);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " stock=" + Stock(game) + " nodes=" +
                string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => n.Id + "/" + n.State + "/" + n.Progress + "/" + n.Blocker))));
        }
        private static Order Raise(Check c, SimGame game)
        {
            var command = new RaiseIncidentCommand { Target = Find(game).Id, Source = Source, Kind = new Code("frontier.failure") };
            c.Equal(Code.None, game.Seat(0).Preview(command), "engine previews actual owned incident");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary command raises native incident");
            return Board(game).ActiveResponseOn(Find(game).Id, Source) ?? throw new InvalidOperationException("response missing");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "combined optional cost semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "existing work and account payloads unchanged");
            var factory = Fixture(120); var game = New(factory); ulong hash = game.Hash(); var shortage = Read(game);
            c.That(shortage.Nodes.Where(n => n.Frontier).All(n => n.Criteria.Status == WorkRequirementStatus.Satisfied), "each eighty-unit node separately fits one hundred twenty");
            c.Equal(2, shortage.FrontierWorkersCovered, "actual distinct machines covered");
            c.That(shortage.FrontierResources.Value.Ready && shortage.FrontierDockBlocker.Value.IsNone, "physical resources and ordinary interaction feasible");
            c.Equal(WorkSystem.BlockerCash, shortage.FrontierCostBlocker.Value, "combined one hundred sixty does not fit same account twice");
            c.That(!shortage.ReadyForFrontier && shortage.Prospective && Board(game).Orders.Count == 0, "pure optional combined refusal creates no work or promises");
            c.Equal(Fix.Zero, Account(game).Reserved, "prospective cost admission reserves nothing");
            c.Equal(hash, game.Hash(), "whole cost read does not charge or mutate");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "prospective cost shortage has no saved preparedness cache");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<IncidentsView>().ReadPreparedness(Find(game).Id, Source, out var hidden), "foreign company denied optional cost facts");
            c.That(hidden == null && hash == game.Hash(), "private refusal leaks no partial state");

            factory = Fixture(); game = New(factory); var exact = Read(game);
            c.That(exact.ReadyForFrontier && exact.FrontierCostBlocker.Value.IsNone, "exact combined funds fit canonical current frontier");
            c.That(exact.Nodes.Where(n => n.Frontier).All(n => n.Criteria.Costs.Count == 2), "authored cost occurrences stay on actual node rows");
            c.Throws<NotSupportedException>(() => ((IList<WorkCostChoice>)exact.Nodes.First(n => n.Frontier).Criteria.Costs).Clear(), "existing per-node cost facts immutable");
            var budgets = game.World.Services.Get<IMonetaryBudgets>();
            var requests = new[] { new MonetaryBudgetRequest(new ReservationOwner("proof.frontier-cost-competitor", 1), Fix.FromInt(30)) };
            c.Equal(MonetaryBudgetStatus.Ready, budgets.EvaluateReservations(new PlayerId(0), requests), "actual account admits independent competing obligation");
            c.Equal(MonetaryBudgetStatus.Committed, budgets.Reserve(new PlayerId(0), requests, out var receipts), "sole existing account owns real competing receipt");
            hash = game.Hash(); var contention = Read(game);
            c.That(contention.Nodes.Where(n => n.Frontier).All(n => n.Criteria.Status == WorkRequirementStatus.Satisfied), "individual eighty still fits one hundred thirty available");
            c.That(!contention.ReadyForFrontier && contention.FrontierCostBlocker == WorkSystem.BlockerCash, "existing competitor hold removes combined feasibility");
            c.Equal(hash, game.Hash(), "preview does not release competitor or create substitute funds");
            c.Equal(MonetaryBudgetStatus.Committed, budgets.Release(new PlayerId(0), receipts), "same account releases actual independent receipt");
            c.That(Read(game).ReadyForFrontier && Account(game).Balance == Fix.FromInt(160), "release restores feasibility without income");
            c.That(exact.ReadyForFrontier && exact.FrontierCostBlocker.Value.IsNone, "retained facts do not become live authorization");
            c.That(!Read(New(Fixture(0))).FrontierCostBlocker.HasValue, "individual funding shortage leaves whole set explicitly unevaluated");

            var physicalOnly = IncidentPreparednessTests.Fixture(fleet: new[] { 1, 2 });
            var noMoneyRequest = new NewGameRequest { Scenario = "points", Seed = 617 }; noMoneyRequest.Mods.Add("proof.shipments");
            noMoneyRequest.Seats.Add(Controller.Human); noMoneyRequest.Seats.Add(Controller.Human);
            var noMoney = physicalOnly.NewGame(noMoneyRequest);
            c.That(!noMoney.World.Services.Has<IEconomy>() && Read(noMoney, "destination").FrontierCostBlocker.Value.IsNone, "un-authored costs require no money module/account");

            foreach (bool survival in new[] { false, true })
            {
                factory = Fixture(survival ? 0 : 160, survival); game = New(factory); var before = Read(game);
                c.That(before.ReadyForFrontier, "enabled exact funds or disabled optional costs admit physical response");
                c.Equal(!survival, game.World.Services.Has<IEconomy>(), "money-disabled mode has no fabricated economy provider");
                Order order = Raise(c, game);
                Until(game, () => order.Nodes.Take(2).All(n => Board(game).Job(n.JobId)?.Worker.IsValid == true), "real two-node assignments");
                c.That(Read(game).ReadyForFrontier, "actual unspent held obligations fit together without double charging");
                if (!survival) c.Equal(Fix.FromInt(160), Account(game).Reserved, "native account owns all four authored receipts");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual held or disabled physical state saves");
                game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long time = game.Tick; Step(game, twin); Read(game);
                c.That(game.Hash() == hash && game.Tick == time, "sole paused clock advances neither physical work nor optional payment");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => order.Node("a").Progress.WholeTicks > 0 && order.Node("b").Progress.WholeTicks > 0, "both actual machines reach productive work", twin);
                c.That(Read(game).ReadyForFrontier && Read(game).FrontierCostBlocker.Value.IsNone, "spent obligations do not demand another payment at zero cash");
                c.Equal(Fix.FromInt(4), Stock(game), "actual work consumes two material units per node once");
                if (!survival) c.That(Account(game).Balance == Fix.Zero && Account(game).Reserved == Fix.Zero, "productive work settles exact held cost once");
                twin = factory.Load(game.Save());
                Until(game, () => order.State == OrderState.Done, "physical response and restart complete", twin);
                c.Equal(game.Hash(), twin.Hash(), "saved physical response and optional payment identical");
                c.That(!Find(game).Get<IIncidentResponse>(Source).ReadIncident().Active, "only real completed response resolves incident");
                c.Equal(Fix.FromInt(4), Stock(game), "completion invents no repeated consumption or refund");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed response uses existing save owners");
            }

            factory = Fixture(260, lifecycle: true); game = New(factory); Order held = Raise(c, game);
            Until(game, () => Account(game).Reserved == Fix.FromInt(160), "actual response obligations reserved before travel");
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "budget.upkeep", Target = Find(game, "processor").Id }).Accepted, "ordinary authored timed running work starts");
            Until(game, () => Account(game).Balance == Fix.FromInt(110), "real timed credit-authorized cost reduces funds");
            hash = game.Hash(); var underfunded = Read(game);
            c.That(underfunded.Nodes.Where(n => n.Frontier).All(n => n.Criteria.Status == WorkRequirementStatus.Satisfied), "each held eighty remains individually spendable against one hundred ten");
            c.That(!underfunded.ReadyForFrontier && underfunded.FrontierCostBlocker == WorkSystem.BlockerCash, "combined existing one hundred sixty receipts are not collectively spendable");
            c.That(held.Nodes.Take(2).All(n => n.Progress.Equals(WorkProgress.Zero)), "underfunding observed during real travel, not instantaneous work");
            c.Equal(Fix.FromInt(8), Stock(game), "pure held-cost check consumes no materials");
            c.Equal(hash, game.Hash(), "joint held-spend admission changes no actual receipts or balance");
            var fundedTwin = factory.Load(game.Save()); Step(game, fundedTwin);
            c.Equal(game.Hash(), fundedTwin.Hash(), "actual underfunded obligations checkpoint identically");
            var funding = new EnqueueTransformationCommand { Transformer = Find(game, "processor").Id, Capability = new Code("transformation"), Transformation = "budget.fund", Count = 1 };
            c.That(TestKit.Order(game, 0, funding).Accepted && TestKit.Order(fundedTwin, 0, funding).Accepted, "player queues existing physical resource generation in both sessions");
            Until(game, () => Account(game).GenerationSettlements == 1, "real timed generation funds ordinary work", fundedTwin);
            c.That(Read(game).ReadyForFrontier && Account(game).Balance == Fix.FromInt(310), "actual optional generation income restores combined affordability without a buyer");
            fundedTwin = factory.Load(game.Save());
            Until(game, () => held.State == OrderState.Done, "funded physical response completes", fundedTwin);
            c.That(Account(game).Balance == Fix.FromInt(150) && Account(game).Reserved == Fix.Zero, "existing account pays exactly one hundred sixty after physical arrival");
            c.Equal(1L, Account(game).GenerationSettlements, "native generation settlement not repeated on load");
            c.Equal(Fix.FromInt(4), Stock(game), "funded response consumes actual four units once");
            c.Equal(game.Hash(), fundedTwin.Hash(), "saved underfunding, production and physical response identical");
        }
    }
}
