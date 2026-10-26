using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    public static class FrontierResourceFeasibilityTests
    {
        public const string Case = "Response frontier materials: joint shortage, actual commitments and saved physical completion";
        private static readonly Code Source = new Code("damage");
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Fixture(int stock = 8, bool tools = false, bool single = false, bool inputs = true)
            => IncidentPreparednessTests.Fixture(fleet: single ? new[] { 3 } : new[] { 1, 2 },
                documentName: "prepared-frontier-resources.json", edit: root =>
                    root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number(stock.ToString())),
                document: root => {
                    if (tools) {
                        root.Get("target_resource_requirements").Add(Json.Parse("{\"id\":\"prepared.tool\",\"port\":null,\"use\":\"hold\",\"quantities\":{\"supply\":4}}"));
                        root.Get("activities")[0].Get("nodes")[0].Set("requirement", JsonValue.Of("prepared.tool"));
                    }
                    if (!inputs) {
                        root.Get("activities")[0].Get("nodes")[0].Remove("requirement");
                        root.Get("activities")[0].Get("nodes")[1].Remove("requirement");
                    }
                });
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 607 }; request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static IncidentPreparednessSnapshot Read(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<IncidentsView>().ReadPreparedness(Find(game, "destination").Id, Source, out var result);
            if (!code.IsNone) throw new InvalidOperationException("frontier read refused: " + code); return result;
        }
        private static Fix Stock(SimGame game) => Find(game, "destination").Get<IOutletPort>().Amount(Supply(game));
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("frontier time=" + game.Tick + " " + game.Fault);
            if (twin != null) Step(twin);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " stock=" + Stock(game) + " nodes=" +
                string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => n.Id + "/" + n.State + "/" + n.Progress + "/" + n.Blocker))));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "strict whole-frontier material semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same actual claims/work state payloads");
            var factory = Fixture(6); var game = New(factory); ulong hash = game.Hash(); var shortfall = Read(game);
            c.That(!game.World.Services.Has<IEconomy>() && game.World.Space is not Beep.SimEngine.Maps.TileMap,
                "contract-based point-space response does not require business or a platform map");
            c.That(shortfall.Prospective && Board(game).Orders.Count == 0, "prospective material read creates no incident or work");
            c.Equal(2, shortfall.FrontierWorkersCovered, "two actual distinct role machines are covered");
            c.That(shortfall.Nodes.Where(n => n.Frontier).All(n => n.Criteria.Status == WorkRequirementStatus.Satisfied), "each node separately has four of six available");
            c.That(shortfall.FrontierResources.HasValue && shortfall.FrontierResources.Value.Status == ResourceTransferStatus.NoStock,
                "same native transaction detects combined eight-unit shortage");
            c.That(!shortfall.ReadyForFrontier, "individual ready does not fabricate simultaneous material feasibility");
            c.Equal(2, shortfall.FrontierResources.Value.Availability.Count, "native whole-set availability retains both obligations");
            c.That(shortfall.FrontierResources.Value.Availability.All(a => a.Required == Fix.FromInt(4) && a.Available == Fix.FromInt(6)),
                "observations use actual shared endpoint quantities, not another balance");
            c.That(shortfall.Nodes.Where(n => n.Frontier).Select(n => n.Criteria.Resources[0].Endpoint).Distinct().Count() == 1, "same actual endpoint shared, not duplicated stock");
            c.Equal(hash, game.Hash(), "whole-frontier selection does not claim or consume");
            c.Throws<NotSupportedException>(() => ((IList<ResourceClaimAvailability>)shortfall.FrontierResources.Value.Availability).Clear(), "joint resource observations detached and immutable");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<IncidentsView>().ReadPreparedness(Find(game, "destination").Id, Source, out var hidden), "joint stock facts private");
            c.That(hidden == null && game.Hash() == hash, "private refusal leaks nothing and remains pure");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "prospective shortage has no new saved preparedness state");

            var exact = New(Fixture()); var ready = Read(exact); hash = exact.Hash();
            c.That(ready.ReadyForFrontier && ready.FrontierResources.Value.Ready, "exact eight units support both four-unit obligations");
            c.That(ready.Nodes.Where(n => n.Frontier).All(n => n.Candidates.Any(candidate => candidate.Worker == n.CoveredWorker &&
                candidate.Result.Status == WorkRequirementStatus.Satisfied)), "criteria relate to real covered candidate, not unrelated first offer");
            var transaction = exact.World.Services.Get<IResourceTransaction>(); var owner = new ReservationOwner("proof.frontier-competitor", 1);
            var stock = Find(exact, "destination").Get<IOutletPort>();
            var acquired = transaction.AcquireClaims(new PlayerId(0), new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, stock.Endpoint,
                new Bill(new[] { new ResourceAmount(Supply(exact), Fix.One) })) }, System.Array.Empty<ResourceReservation>());
            c.That(acquired.Committed, "real competing commitment belongs to existing endpoint book");
            ulong reserved = exact.Hash(); var contention = Read(exact);
            c.That(contention.Nodes.Where(n => n.Frontier).All(n => n.Criteria.Status == WorkRequirementStatus.Satisfied), "individual offers still fit seven unpromised units");
            c.That(!contention.ReadyForFrontier && contention.FrontierResources.Value.Status == ResourceTransferStatus.NoStock, "real external commitment removes whole-frontier feasibility");
            c.Equal(reserved, exact.Hash(), "contention preview neither releases competitor nor acquires new claims");
            c.That(transaction.ReleaseClaims(new ResourceClaimReleaseIntent(new PlayerId(0), acquired.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "same owner releases actual competing receipt");
            c.That(Read(exact).ReadyForFrontier && Stock(exact) == Fix.FromInt(8), "released promise restores actual material feasibility without generating stock");
            c.That(ready.ReadyForFrontier && ready.FrontierResources.Value.Availability.All(a => a.Available == Fix.FromInt(8)), "retained ready snapshot unaffected by later claims");

            var missing = Read(New(Fixture(0)));
            c.That(!missing.FrontierResources.HasValue && !missing.ReadyForFrontier, "individual shortage leaves whole-set observation explicitly unevaluated");
            var single = Read(New(Fixture(single: true)));
            c.That(single.FrontierWorkerShortfall == 1 && !single.FrontierResources.HasValue && !single.ReadyForFrontier, "missing actual worker is not a fabricated joint-ready check");
            var empty = Read(New(Fixture(0, inputs: false)));
            c.That(empty.ReadyForFrontier && empty.FrontierResources.Value.Ready && empty.FrontierResources.Value.Availability.Count == 0,
                "authored material-free frontier has no invented obligation");
            var heldShort = Read(New(Fixture(6, tools: true)));
            c.That(!heldShort.ReadyForFrontier && heldShort.FrontierResources.Value.Status == ResourceTransferStatus.NoStock,
                "held tools and consumables cannot independently spend same six units");

            foreach (bool tools in new[] { false, true })
            {
                factory = Fixture(tools: tools); game = New(factory); var before = Read(game); Order response = Raise(c, game);
                c.That(response.State != OrderState.Done && Read(game).OrderId == response.Id, "actual incident owns ordinary native response graph");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "response preserves actual existing claims/jobs through save");
                game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); long time = game.Tick; Step(game, twin); Read(game);
                c.That(game.Hash() == paused && game.Tick == time, "sole paused clock and pure frontier read advance nothing");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => response.Node("a.flex").Progress.WholeTicks > 0 && response.Node("b.inspect").Progress.WholeTicks > 0,
                    "both actual machines earn physical frontier work", twin);
                var active = Read(game);
                c.That(active.FrontierWorkersCovered == 2 && active.ReadyForFrontier && active.FrontierResources.Value.Ready,
                    "actual bound/spent commitments are not charged a second time during readiness");
                c.Equal(0, active.FrontierResources.Value.Availability.Count, "existing commitments need no new startup claims");
                c.Equal(Fix.FromInt(tools ? 4 : 0), Stock(game), "physical work consumes actual input once and retains tools as stock");
                c.That(response.Node("a.flex").Resources.Count > 0 && response.Node("b.inspect").Resources.Count > 0,
                    "existing native node bindings own real material commitments");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial consumed/held physical commitments checkpoint");
                Until(game, () => response.State == OrderState.Done, "actual response and restart complete", twin);
                c.Equal(Fix.FromInt(tools ? 4 : 0), Stock(game), "completion releases actual tool claim but never recreates consumed material");
                c.That(!Find(game, "destination").Get<IIncidentResponse>(Source).ReadIncident().Active, "only real completed response resolves incident");
                c.Equal(game.Hash(), twin.Hash(), "saved physical response continuation identical");
                c.That(before.Prospective && before.ReadyForFrontier && before.OrderId == 0, "detached prospective facts never become actual state");
                c.That(!Read(game).ReadyForFrontier, "next episode cannot reuse materials spent by previous physical response");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed response saves without duplicated preparedness authority");
            }
        }
    }
}
