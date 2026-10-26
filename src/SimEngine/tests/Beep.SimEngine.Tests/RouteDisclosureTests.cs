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
    // JSON authoring and normal public lifecycle only; all selection/delivery remains engine-owned.
    internal static class RouteDisclosureTests
    {
        internal const string Case = "Route disclosure: private aggregates and route controls preserve physical delivery and cancellation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, string masked)
            => ResourceContentContractTests.Factory(half, new DirectResourceContentModule(), worldEdit: root => {
                var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                var settings = Json.Parse("{\"target_kind\":\"entity\",\"target\":\"source\",\"initial_hidden\":false}"); settings.Set("target", JsonValue.Of(masked));
                scenario.Get("settings").Set(InventorySightModule.ModuleId, settings);
            }, extraModules: new GameModule[] { new InventorySightModule() },
            extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static LogisticsView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<LogisticsView>();
        private static Fix Stock(SimGame g, EntityId id, ResourceId resource)
        { var code = g.Observer.Feature<LogisticsView>().ReadDiagnostics(id, resource, out var value); if (!code.IsNone) throw new InvalidOperationException("actual route stock/" + code); return value.Stock; }
        private static AddRouteCommand Add(EntityId source, EntityId target, ResourceId r, bool enabled = true)
            => new AddRouteCommand { From = source, To = target, Resource = r, Settings = new LogisticsRouteSettings(enabled, Fix.FromInt(8), 10) };
        private static Code Parity(Check c, SimGame g, int seat, Command command)
        {
            var v = View(g, seat);
            Code typed = command is AddRouteCommand add ? v.PreviewRoute(add.From, add.To, add.Resource, add.Settings, add.SourcePort, add.DestinationPort) :
                command is ConfigureRouteCommand edit ? v.PreviewRouteSettings(edit.Route, edit.Settings) : v.PreviewRemoveRoute(((RemoveRouteCommand)command).Route);
            c.Equal(typed, g.Seat(seat).Preview(command), "typed route add/edit/remove matches normal command");
            return typed;
        }
        private static void Reach(Check c, SimGame g, SimGame twin, Func<bool> done, EntityId[] stores, ResourceId r, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++)
            {
                TransformationContentContractTests.Step(g, twin); Fix sum = Fix.Zero;
                foreach (var id in stores) sum += Stock(g, id, r);
                c.Equal(Fix.FromInt(20), sum, "physical route conservation/" + stage);
            }
            if (!done()) throw new InvalidOperationException(stage + "/clock=" + g.Clock.StepIndex + "/stocks=" + string.Join(";", stores.Select(x => x + "/" + Stock(g, x, r))));
        }
        private static void Healthy(Check c, bool half, string maskedName)
        {
            var f = Factory(half, maskedName); var request = new NewGameRequest { Scenario = "points", Seed = 33913, Seats = { Controller.Human, Controller.Human } };
            var g = f.NewGame(request); var idle = f.NewGame(request); ulong isolated = idle.Hash();
            var source = TransformationContentContractTests.Entity(g, "source"); var target = TransformationContentContractTests.Entity(g, "destination");
            var carrier = TransformationContentContractTests.Entity(g, "machine"); var stores = new[] { source, target, carrier };
            var masked = maskedName == "source" ? source : maskedName == "destination" ? target : carrier;
            bool endpointHidden = masked != carrier;
            c.That(g.Seat(0).View.TryFindResource("supply", out var r), "actual resource");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current route disclosure identity");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            c.Equal(Codes.NotFound, View(g).ReadStockTarget(EntityId.None, r, out _), "missing intent is explicitly unavailable");
            c.Equal(Code.None, View(g).ReadStockTarget(target, r, out var zero), "visible no-demand store available"); c.Equal(Fix.Zero, zero, "actual zero demand remains zero");
            c.Equal(Codes.InvalidArgument, View(g).ReadStockTarget(target, default, out _), "visible malformed resource remains typed");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Add(source, target, r)), "foreign add private");
            PermanentRemovalTests.Submit(c, g, new SetWorkerHeldCommand { Worker = carrier, Held = true });
            PermanentRemovalTests.Submit(c, g, Add(source, target, r));
            var routes = new List<RouteView>(); View(g).CopyRoutes(routes); c.Equal(1, routes.Count, "ordinary route publishes actual intent");
            var retained = routes[0]; int route = retained.Id;
            c.Equal(Codes.InvalidArgument, Parity(c, g, 0, new ConfigureRouteCommand { Route = route }), "visible null route settings remain typed");
            c.Equal(Codes.NotFound, Parity(c, g, 1, new RemoveRouteCommand { Route = route }), "foreign cancellation private");
            PermanentRemovalTests.Submit(c, g, new ConfigureStockPolicyCommand { Store = target, Resource = r,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(2), Fix.FromInt(8), Fix.FromInt(8), 20, LogisticsReserveAccess.Protected)) });
            var demands = new List<LogisticsDemandView>(); View(g).CopyDemands(demands); c.Equal(1, demands.Count, "actual stock demand publishes");
            var oldDemand = demands[0]; c.Equal(Code.None, View(g).ReadStockTarget(target, r, out var desired), "visible intent known"); c.Equal(Fix.FromInt(8), desired, "actual desired quantity");
            PermanentRemovalTests.Submit(c, g, new SetWorkerHeldCommand { Worker = carrier, Held = false });
            Reach(c, g, null, () => Stock(g, carrier, r).IsPositive, stores, r, "real route/demand pickup");
            var jobs = new List<WorkJobView>(); g.Observer.Feature<WorkView>().CopyJobs(jobs);
            int job = jobs.Single(x => x.Worker == carrier && x.State != JobState.Done).Id;
            c.Equal(Code.None, View(g).ReadDiagnostics(target, r, out var retainedDiagnostic), "visible complete diagnostic available");
            c.That(retainedDiagnostic.Carriers.Count == 1 && retainedDiagnostic.Shipments.Any(x => x.Job == job), "actual derived carrier and shipment facts");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true });
            ulong pure = g.Hash(); View(g).CopyRoutes(routes); c.Equal(endpointHidden ? 0 : 1, routes.Count, "private endpoint route row withheld whole");
            View(g).CopyDemands(demands); c.Equal(masked == target ? 0 : 1, demands.Count, "private target demand withheld before owner callback");
            c.Equal(Codes.NotFound, View(g).ReadStockTarget(masked, default, out _), "private target before invalid resource");
            c.Equal(Codes.NotFound, View(g, 1).ReadStockTarget(target, r, out _), "foreign target omitted");
            c.Equal(Codes.NotFound, View(g).ReadDiagnostics(target, r, out var hidden), "rich diagnostics with private carrier/source/target unavailable whole"); c.That(hidden == null, "no redacted diagnostic or fabricated reduced totals");
            var offers = new List<LogisticsDispatchOffer>(); View(g).CopyQueue(offers);
            if (endpointHidden) c.That(offers.All(x => x.Route != route), "route offers with private endpoint withheld even refused candidates");
            View(g, 1).CopyQueue(offers); c.Equal(0, offers.Count, "foreign queue empty");
            var edit = new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(8), 99) };
            c.Equal(endpointHidden ? Codes.NotFound : Code.None, Parity(c, g, 0, edit), "edit depends on endpoint sight, not worker sight");
            if (endpointHidden)
            {
                c.Equal(Codes.NotFound, Parity(c, g, 0, new AddRouteCommand { From = source, To = target, Settings = null, Resource = default, SourcePort = new Code("missing") }), "hidden endpoint access before malformed binding details");
                c.Equal(Codes.NotFound, Parity(c, g, 0, new ConfigureRouteCommand { Route = route }), "hidden edit access before null settings");
            }
            c.Equal(Code.None, Parity(c, g, 0, new RemoveRouteCommand { Route = route }), "owned cancellation does not require endpoint sight");
            c.Equal(pure, g.Hash(), "private aggregates and route previews pure");
            var pending = new List<(int Seat, int Sequence)>();
            foreach (int seat in endpointHidden ? new[] { 0, 1 } : new[] { 1 })
            {
                pending.Add((seat, g.Seat(seat).Submit(Add(source, target, r))));
                pending.Add((seat, g.Seat(seat).Submit(new ConfigureRouteCommand { Route = route, Settings = edit.Settings })));
            }
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "saved private route/demand/cargo and pending inbox durable");
            TransformationContentContractTests.Step(g, twin);
            foreach (var input in pending)
            {
                long cursor = 0; var results = new List<CommandResult>(); g.Seat(input.Seat).ReadResults(ref cursor, results);
                var result = results.Single(x => x.Sequence == input.Sequence); c.That(!result.Accepted && result.Refusal == Codes.NotFound, "actual private route input refuses");
            }
            g.Observer.Feature<LogisticsView>().CopyRoutes(routes); c.That(routes.Count == 1 && routes[0].Settings.Enabled && routes[0].Settings.Priority == 10, "refused edits allocate no new route or change old intent");
            PermanentRemovalTests.Submit(c, g, new RemoveRouteCommand { Route = route }); twin = f.Load(g.Save());
            g.Observer.Feature<LogisticsView>().CopyRoutes(routes); c.Equal(0, routes.Count, "owned unseen cancellation retires future route");
            jobs.Clear(); g.Observer.Feature<WorkView>().CopyJobs(jobs); c.That(jobs.Any(x => x.Id == job && x.Worker == carrier && x.State != JobState.Done), "cancelling future intent preserves acquired shipment");
            Reach(c, g, twin, () => Stock(g, target, r) == Fix.FromInt(8) && Stock(g, carrier, r).IsZero, stores, r, "hidden delivery after route cancellation");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false });
            c.Equal(Code.None, View(g).ReadDiagnostics(target, r, out var restored), "reveal restores whole actual diagnostics");
            c.That(restored.Stock == Fix.FromInt(8) && retainedDiagnostic.Stock == Fix.Zero && retained.Settings.Enabled && oldDemand.Quantity == Fix.FromInt(8), "retained immutable snapshots do not track later state");
            c.Equal(Code.None, View(g).ReadStockTarget(target, r, out desired), "restored actual intent"); c.Equal(Fix.FromInt(8), desired, "standing stock policy survives route cancellation");
            c.Equal(Fix.FromInt(12), Stock(g, source, r), "finite source charged once");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled state durable"); c.Equal(isolated, idle.Hash(), "independent sessions isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "route/demand/private refusals/cancellation/delivery replay");
        }
        private static void Run(Check c) { foreach (bool half in new[] { false, true }) foreach (string masked in new[] { "source", "destination", "machine" }) Healthy(c, half, masked); }
    }
}

