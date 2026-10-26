using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Only authored content and player commands; original logistics owners make every decision.
    internal static class LoadDecisionDisclosureTests
    {
        internal const string Case = "Load decision disclosure: private consulted batching inputs withhold automatic offers without changing physical dispatch";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static LogisticsView View(SimGame game) => game.Seat(0).View.Feature<LogisticsView>();
        private static List<LogisticsDispatchOffer> Queue(LogisticsView view)
        { var rows = new List<LogisticsDispatchOffer>(); view.CopyQueue(rows); return rows; }
        private static HaulResourcesCommand Manual(EntityId source, EntityId target, EntityId worker, ResourceId supply)
            => new HaulResourcesCommand { Source = source, Destination = target, Worker = worker, Resource = supply, Ceiling = Fix.FromInt(2) };
        private static void Hidden(Check c, SimGame game, EntityId source, EntityId target, EntityId worker, ResourceId supply)
        {
            ulong before = game.Hash(); long at = game.Clock.StepIndex;
            var actual = Queue(game.Observer.Feature<LogisticsView>()).Where(row => row.Destination == target && row.Resource == supply).ToArray();
            c.That(actual.Length == 2 && actual.Any(row => row.Route != 0) && actual.Any(row => row.Route == 0), "observer retains actual route and source-selected demand");
            c.That(actual.Single(row => row.Route != 0).Haul.Status == HaulStatus.PolicyBlocked
                && !actual.Single(row => row.Route != 0).Haul.Carrier.IsValid, "actual route refusal has no selected carrier");
            c.Equal(HaulStatus.NoSupply, actual.Single(row => row.Route == 0).Haul.Status, "original first-refusal precedence can retain no-supply while later candidates consult batching");
            Console.WriteLine("load-decision/observer/" + string.Join(";", actual.Select(row => row.Kind + "/" + row.Haul.Status + "/" + row.Haul.Blocker)));
            c.Equal(0, Queue(View(game)).Count(row => row.Destination == target && row.Resource == supply),
                "whole consulted footprint withholds route and source-selected demand, including discarded candidates");
            c.Equal(Codes.NotFound, View(game).ReadDiagnostics(target, supply, out var withheld), "whole diagnostic cannot publish private automatic decision");
            c.That(withheld == null, "private decision publishes no healthy stock/carrier prefix");
            c.Equal(HaulStatus.Ready, View(game).PreviewHaul(source, target, supply, Fix.FromInt(2), worker: worker).Status,
                "explicit manual haul still bypasses unconsulted batching policy through original rules");
            c.Equal(Code.None, game.Seat(0).Preview(Manual(source, target, worker, supply)), "ordinary manual command preview agrees, not blanket private-policy refusal");
            c.Equal(before, game.Hash(), "derived decision metadata changes no game state");
            c.Equal(at, game.Clock.StepIndex, "decision views drive no time");
        }
        private static void Both(Check c, SimGame game, SimGame twin, Func<Command> command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command()), "ordinary saved command admission");
            int sequence = game.Seat(0).Submit(command()); twin.Seat(0).Submit(command());
            PermanentRemovalTests.Step(game, twin);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(row => row.Sequence == sequence).Accepted, "actual command accepted on saved continuation");
        }
        private static void Run(Check c)
        {
            foreach (bool resource in new[] { false, true })
            {
                var factory = LoadPolicyDisclosureTests.Factory(resource, "buffer", edit: root => {
                    var metadata = root.Get("resources")[0].Clone(); metadata.Set("id", JsonValue.Of("buffer")); root.Get("resources").Add(metadata);
                });
                var request = new NewGameRequest { Scenario = "points", Seed = 12523, Seats = { Controller.Human, Controller.Human } };
                var game = factory.NewGame(request); var idle = factory.NewGame(request); ulong isolated = idle.Hash();
                var source = TransformationContentContractTests.Entity(game, "source"); var target = TransformationContentContractTests.Entity(game, "destination");
                var worker = TransformationContentContractTests.Entity(game, "reserve.truck"); var supply = PermanentRemovalTests.Supply(game);
                if (!game.Seat(0).View.TryFindResource("buffer", out var buffer)) throw new InvalidOperationException("authored metadata resource required");
                var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
                var settings = new HaulLoadSettings("load.batch", true, new Bill(new[] {
                    new ResourceAmount(supply, Fix.FromInt(4)), new ResourceAmount(buffer, Fix.One) }), GameDuration.Minutes(100), false);
                PermanentRemovalTests.Submit(c, game, new ConfigureHaulLoadPolicyCommand { Worker = worker, Settings = settings });
                PermanentRemovalTests.Submit(c, game, new AddRouteCommand { From = source, To = target, Resource = supply, Settings = new LogisticsRouteSettings(true, Fix.FromInt(2), 1) });
                PermanentRemovalTests.Submit(c, game, new ConfigureStockPolicyCommand { Store = target, Resource = supply,
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(2), Fix.FromInt(2), Fix.FromInt(40), 1, LogisticsReserveAccess.Protected)) });
                var known = Queue(View(game)).Where(row => row.Destination == target && row.Resource == supply).ToArray();
                c.Equal(2, known.Length, "visible route and demand each expose actual waiting decision");
                int route = known.Single(row => row.Route != 0).Route;
                PermanentRemovalTests.Submit(c, game, new SetInventorySightCommand { Hidden = true });
                Hidden(c, game, source, target, worker, supply);
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "hidden queued decision restores original state");
                Hidden(c, twin, source, target, worker, supply);
                var inactive = factory.Load(game.Save());
                using (var inactiveRecording = inactive.RecordReplay(limits))
                {
                    PermanentRemovalTests.Submit(c, inactive, new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(2), 1) });
                    var visible = Queue(View(inactive)).Where(row => row.Destination == target && row.Resource == supply).ToArray();
                    c.That(visible.Length == 1 && visible[0].Route == route && visible[0].Haul.Status == HaulStatus.PolicyBlocked,
                        "disabled route never consults hidden batching policy and remains visible while demand is withheld");
                    var inactiveReplay = new ReplayVerifier().Verify(factory, inactiveRecording.Finish(), limits);
                    c.That(inactiveReplay.Success && inactiveReplay.ActualHash == inactive.Hash(), "actual disabled-route input replays without hiding unrelated decisions");
                }
                Both(c, game, twin, () => new ConfigureHaulLoadPolicyCommand { Worker = worker,
                    Settings = new HaulLoadSettings("load.visible", true, Bill.Empty, GameDuration.Minutes(0), false) });
                Both(c, game, twin, () => new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(2), 1) });
                c.Equal(Code.None, View(game).ReadDiagnostics(target, supply, out var revealed), "known replacement removes consulted private dependency while content stays hidden");
                c.That(revealed != null, "known current decision has real diagnostics, not fabricated empty data");
                bool loaded = false; var watch = Stopwatch.StartNew(); int updates = 0;
                while (updates < 128 && watch.ElapsedMilliseconds < 25000)
                {
                    Fix cargo = LoadPolicyDisclosureTests.Stock(game, worker, supply); loaded |= cargo == Fix.FromInt(2);
                    Fix delivered = LoadPolicyDisclosureTests.Stock(game, target, supply);
                    c.Equal(Fix.FromInt(20), LoadPolicyDisclosureTests.Stock(game, source, supply) + delivered + cargo, "every physical update conserves original supply");
                    if (delivered == Fix.FromInt(2) && cargo.IsZero && PermanentRemovalTests.Jobs(game).Count == 0) break;
                    PermanentRemovalTests.Step(game, twin); updates++;
                }
                c.That(loaded && updates < 128 && watch.ElapsedMilliseconds < 25000, "ordinary two-unit automatic delivery physically loads moves and settles within original bounds");
                c.Equal(Fix.FromInt(18), LoadPolicyDisclosureTests.Stock(game, source, supply), "one physical batch removes exactly two from source");
                c.Equal(Fix.FromInt(2), LoadPolicyDisclosureTests.Stock(game, target, supply), "one physical batch delivers exactly two");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled original policy/queue/work checkpoint restores");
                c.Equal(isolated, idle.Hash(), "shared content does not share mutable decision state");
                var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == game.Hash(), "actual queued player inputs and physical continuation replay");
                Console.WriteLine("load-decision/" + (resource ? "minimum-resource" : "profile") + "/full/0->" + game.Clock.StepIndex
                    + "/inputs=" + recording.CommandCount + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
            }
        }
    }
}
