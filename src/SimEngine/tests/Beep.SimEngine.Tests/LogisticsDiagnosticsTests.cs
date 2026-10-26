using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class LogisticsDiagnosticsTests
    {
        internal const string Case = "Logistics diagnostics: private pipeline, physical incoming throughput and saved continuation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(int retained = 64)
        {
            var root = Json.Parse(PointGame.Definitions); WorkerExecutionTestContent.Add(root);
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var flows = Json.Parse(FrameworkProofContent.Read("resource-flows.json"));
            flows.Get("resource_flow_journals")[0].Set("retained_records_per_company", JsonValue.Number(retained.ToString()));
            var text = new StringBuilder(); root.WriteCanonical(text);
            var flowText = new StringBuilder(); flows.WriteCanonical(flowText);
            return PointGame.Factory(text.ToString(), flowDefinitions: flowText.ToString());
        }
        private static SimGame New(GameFactory factory, bool history = true)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 631 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            if (history) request.Mods.Add(ResourceFlowsModule.ModuleId); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id);
        private static LogisticsOperationalSnapshot Read(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(Find(game, "destination").Id,
                game.World.Content.ResourceId("supply"), out var snapshot);
            if (!code.IsNone) throw new InvalidOperationException("logistics diagnostic: " + code); return snapshot;
        }
        private static LogisticsThroughputSnapshot Throughput(SimGame game, long from)
        {
            Code code = game.Seat(0).View.Feature<LogisticsView>().ReadIncomingThroughput(Find(game, "destination").Id,
                game.World.Content.ResourceId("supply"), from, game.Tick, out var snapshot);
            if (!code.IsNone) throw new InvalidOperationException("haul throughput: " + code); return snapshot;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("logistics Update: " + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 400 && !done(); i++) Step(game, twin);
            if (!done()) { var diagnostic = Read(game); throw new InvalidOperationException(stage + " step=" + game.Tick +
                " stock=" + diagnostic.Stock + " gap=" + diagnostic.UnpromisedGap + " jobs=" +
                string.Join(";", diagnostic.Shipments.Select(j => j.Job + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker))); }
        }
        private static void Supply(Check c, SimGame game)
        {
            Entity worker = Find(game, "machine"), target = Find(game, "destination");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = worker.Id, Held = true }).Accepted, "player holds real carrier through the engine");
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = target.Id, Resource = game.World.Content.ResourceId("supply"),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(8), Fix.FromInt(40), 10, LogisticsReserveAccess.Protected)) }).Accepted,
                "player sets authored native stock bands, no demo matching logic");
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory); Supply(c, game);
            Entity target = Find(game, "destination"), worker = Find(game, "machine"), source = Find(game, "source");
            ResourceId resource = game.World.Content.ResourceId("supply"); var view = game.Seat(0).View.Feature<LogisticsView>();
            c.That(ReferenceEquals(game.World.Services.Get<ILogisticsDiagnostics>(), game.World.Services.Get<LogisticsService>()), "diagnostics alias the original service");
            c.That(!game.World.Services.Has<IEconomy>(), "physical logistics diagnostics require no business module");
            ulong hash = game.Hash(); var waiting = Read(game);
            c.Equal(Fix.FromInt(8), waiting.UnmetDesired, "physical unmet desired stock comes from actual standing demand");
            c.Equal(Fix.FromInt(8), waiting.UnpromisedGap, "unpromised gap uses original dispatch rules");
            c.Equal(Fix.Zero, waiting.InboundPromised, "blocked demand fabricates no incoming stock");
            c.That(waiting.QueueLength == 1 && waiting.BlockedOffers == 1 && waiting.Offers[0].Haul.Status == HaulStatus.NoCarrier,
                "same real queue exposes current carrier refusal");
            c.That(waiting.Carriers.Count == 1 && waiting.Carriers[0].Held && !waiting.Carriers[0].Unassigned, "held carrier is not reported as unassigned capacity");
            c.Equal(Fix.FromInt(8), waiting.Carriers[0].IncomingCapacity, "capacity is specific to actual resource and cargo provider");
            c.Equal(BigInteger.Zero, Throughput(game, 0).IncomingRaw, "complete history can prove zero actual incoming haul");
            c.Equal(hash, game.Hash(), "diagnostic and throughput reads are pure");
            c.Throws<NotSupportedException>(() => ((IList<LogisticsCarrierDiagnostic>)waiting.Carriers).Clear(), "carrier snapshot is immutable");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<LogisticsView>().ReadDiagnostics(target.Id, resource, out var hidden), "foreign company cannot inspect fleet or target");
            c.That(hidden == null, "private refusal returns no partial snapshot");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<LogisticsView>().ReadIncomingThroughput(target.Id, resource, 0, game.Tick, out var hiddenRate), "throughput history remains private");
            c.That(hiddenRate == null, "foreign throughput reveals no history bounds or quantities");
            c.Equal(Codes.InvalidArgument, view.ReadIncomingThroughput(target.Id, resource, 0, 0, out var invalid), "zero-time rate is refused instead of dividing or inventing a rate");
            c.That(invalid == null, "invalid window returns no partial rate");
            var route = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = resource,
                Settings = new LogisticsRouteSettings(false, Fix.FromInt(8), 1) });
            c.That(route.Accepted, "player declares a disabled route");
            c.That(Read(game).Offers.Any(o => o.Route != 0 && o.Haul.Blocker == LogisticsService.RouteDisabled),
                "policy-blocked supply remains visible with original typed refusal");
            long start = game.Tick;
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = worker.Id, Held = false }).Accepted, "player returns carrier to ordinary shared dispatch");
            Until(game, () => Read(game).Shipments.Count == 1, "native haul acquisition");
            var acquired = Read(game);
            c.Equal(Fix.FromInt(8), acquired.InboundPromised, "original shipment owns actual destination capacity");
            c.Equal(Fix.Zero, acquired.UnpromisedGap, "already promised cargo is not requested twice");
            c.Equal(Fix.FromInt(8), acquired.UnmetDesired, "incoming promise is not completed delivered stock");
            c.That(acquired.Carriers[0].Job != 0 && !acquired.Carriers[0].Unassigned, "busy real carrier is not idle capacity");
            Until(game, () => Read(game).Shipments.Any(j => j.Loaded.IsPositive), "physical loading");
            var loaded = Read(game);
            c.That(loaded.Shipments[0].Loaded.IsPositive && loaded.Stock.IsZero, "loaded cargo remains physical while destination is empty");
            c.Equal(BigInteger.Zero, Throughput(game, start).IncomingRaw, "loading into cargo is not counted as target throughput");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "loaded diagnostic derives correctly from saved owners");
            game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); Step(game, twin); Read(game);
            c.Equal(hash, game.Hash(), "sole paused game clock freezes pipeline and history");
            game.Clock.Resume(); twin.Clock.Resume();
            Until(game, () => Read(game).Stock.IsPositive, "physical partial delivery", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved loaded pipeline follows identical physical delivery");
            c.Equal(new BigInteger(Read(game).Stock.Raw), Throughput(game, start).IncomingRaw, "throughput measures actual committed received quantity");
            twin = factory.Load(game.Save()); Until(game, () => Read(game).Stock == Fix.FromInt(8) && Read(game).Shipments.Count == 0, "native delivery completion", twin);
            var finished = Read(game); var measured = Throughput(game, start);
            c.Equal(game.Hash(), twin.Hash(), "partial delivery saves and completes identically");
            c.Equal(Fix.Zero, finished.UnmetDesired, "physical stock satisfies demand");
            c.Equal(Fix.Zero, finished.InboundPromised, "finished shipment leaves no incoming promises");
            c.Equal(new BigInteger(Fix.FromInt(8).Raw), measured.IncomingRaw, "eight actual delivered units counted once, not both load and unload legs");
            c.That(measured.TransferLegs > 0 && measured.Unit == new UnitId("unit.supply"), "measured rate retains native unit and actual committed leg count");
            c.That(measured.RateRawNumerator * (game.Tick - start) * game.World.Time.QuantumNumerator ==
                measured.IncomingRaw * game.World.Time.QuantumDenominator * measured.RateSecondsDenominator, "rate is exact per elapsed game second without rounding");
            c.Equal(Fix.FromInt(20), source.Get<IOutletPort>().Amount(resource) + target.Get<IInletPort>().Amount(resource) + worker.Get<ITransporter>().Cargo.Amount(resource), "physical resource conservation has no diagnostic ledger");
            c.That(waiting.Stock.IsZero && waiting.UnmetDesired == Fix.FromInt(8) && waiting.InboundPromised.IsZero, "retained immutable facts do not follow later live stock");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed diagnostics need no extra codec");
            var absent = New(factory, history: false); Step(absent);
            c.Equal(LogisticsDiagnosticCodes.NoHistory, absent.Seat(0).View.Feature<LogisticsView>().ReadIncomingThroughput(Find(absent, "destination").Id,
                absent.World.Content.ResourceId("supply"), 0, absent.Tick, out var noHistory), "absent journal does not fabricate zero throughput");
            c.That(noHistory == null, "no-history refusal has no fake measurement");
            var limited = New(Factory(2)); Supply(c, limited);
            c.That(TestKit.Order(limited, 0, new SetWorkerHeldCommand { Worker = Find(limited, "machine").Id, Held = false }).Accepted, "limited retention follows same ordinary player workflow");
            Until(limited, () => Read(limited).Stock == Fix.FromInt(8), "bounded retained-window delivery");
            c.Equal(LogisticsDiagnosticCodes.ExpiredHistory, limited.Seat(0).View.Feature<LogisticsView>().ReadIncomingThroughput(Find(limited, "destination").Id,
                limited.World.Content.ResourceId("supply"), 0, limited.Tick, out var expired), "expired window is explicitly incomplete, not a lower fake throughput");
            c.That(expired == null, "expired history returns no incomplete rate");
        }
    }
}
