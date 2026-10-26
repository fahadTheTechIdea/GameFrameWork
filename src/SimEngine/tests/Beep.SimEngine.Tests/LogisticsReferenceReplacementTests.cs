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
    internal static class LogisticsReferenceReplacementTests
    {
        internal const string Case = "Logistics replacement references: persistent routes stock policies and physical continuation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static bool MultiPolicyCase(string mode) => mode == "multi" || mode == "pinned" || mode == "current";
        private static bool FreshSelector(string mode) => mode == "fresh" || mode == "fresh-last";
        private static bool HasTwoPools(string mode) => mode == "selection" || FreshSelector(mode) || RetainsSelector(mode);
        private static bool RetainsSelector(string mode) => MultiPolicyCase(mode) || mode == "denied" || mode == "removed";
        private static bool UsesPool(string mode) => HasTwoPools(mode) || mode == "binding" || mode == "stable" || mode == "changed" || mode == "owner" || mode == "collision";
        private static string PoolContent(string mode)
        {
            var document = Json.Parse(FrameworkProofContent.Read("port-replacement.json"));
            if (mode == "owner") document.Get("resource_pools")[0].Set("owner", JsonValue.Number("1"));
            if (HasTwoPools(mode))
            {
                var next = document.Get("resource_pools")[0].Clone(); next.Set("id", JsonValue.Of("replacement.next"));
                next.Set("identity", JsonValue.Number("2")); document.Get("resource_pools").Add(next);
            }
            return document.ToString();
        }
        internal static GameFactory Factory(bool source, bool named, string refusal = null, Action<JsonValue> edit = null)
            => EntityReferenceReplacementTests.Factory(
                extraModules: UsesPool(refusal) ? new GameModule[] { new EndpointProofModule(), new OutputPortProofModule() } : null,
                extraDeclarations: UsesPool(refusal) ? """[{"id":"proof.pools","version":"1.0.0","requires":[{"id":"sim.inventory","version":"1.0.0"},{"id":"sim.resource-flows","version":"1.0.0"}],"documents":["port-replacement.json"]},{"id":"proof.output-ports","version":"1.0.0","requires":[{"id":"proof.pools","version":"1.0.0"},{"id":"sim.transformations","version":"1.0.0"},{"id":"sim.world-setup","version":"1.0.0"}],"documents":[]}]""" : null,
                extraDocuments: UsesPool(refusal) ? new Dictionary<string, string> { ["port-replacement.json"] = PoolContent(refusal) } : null,
                edit: root =>
            {
                var original = root.Get("entities")[source ? 0 : 1];
                if (named)
                {
                    root.Get("entities")[0].Get("components").Set("inventory_port", Json.Parse("{\"id\":\"stock\"}"));
                    root.Get("entities")[1].Get("components").Set("inventory_port", Json.Parse("{\"id\":\"stock\"}"));
                }
                var next = root.Get("entities")[3];
                if (refusal == "stable" || refusal == "changed" || refusal == "selection" || RetainsSelector(refusal))
                {
                    original.Get("components").Remove("inventory_port");
                    original.Get("components").Set("pool_port", Json.Parse("""{"id":"stock","pool":"replacement.receiver","alternatives":[],"endpoint_scope":"proof.pools","original_access":"retained"}"""));
                    if (RetainsSelector(refusal)) original.Get("components").Get("pool_port").Set("alternatives", Json.Parse("[\"replacement.next\"]"));
                }
                var parts = original.Get("components").Clone(); next.Set("components", parts);
                if (refusal == "port") parts.Remove("inventory_port");
                if (UsesPool(refusal))
                {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.pools"));
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.output-ports"));
                    parts.Remove("inventory_port");
                    parts.Set("pool_port", Json.Parse("""{"id":"stock","pool":"replacement.receiver","alternatives":[],"endpoint_scope":"proof.pools","original_access":"retained"}"""));
                    if (refusal == "selection") parts.Get("pool_port").Set("pool", JsonValue.Of("replacement.next"));
                    if (FreshSelector(refusal) || RetainsSelector(refusal) && refusal != "removed") parts.Get("pool_port").Set("alternatives", Json.Parse("[\"replacement.next\"]"));
                    if (refusal == "fresh-last")
                    {
                        parts.Get("pool_port").Set("pool", JsonValue.Of("replacement.next"));
                        parts.Get("pool_port").Set("alternatives", Json.Parse("[\"replacement.receiver\"]"));
                    }
                    if (refusal == "denied" || refusal == "current") parts.Get("pool_port").Set("original_access", JsonValue.Of("current_only"));
                    if (refusal == "collision")
                    {
                        var sourceParts = root.Get("entities")[0].Get("components"); sourceParts.Remove("inventory_port");
                        sourceParts.Set("pool_port", parts.Get("pool_port").Clone());
                    }
                    if (refusal == "changed")
                    {
                        parts.Remove("pool_port");
                        parts.Set("inventory_port", Json.Parse("{\"id\":\"stock\"}"));
                    }
                }
                if (refusal == "capacity") parts.Get("point_stock").Set("capacity_policy", JsonValue.Of("fixture.count.20"));
                if (refusal == "resource")
                {
                    var other = root.Get("resources")[0].Clone(); other.Set("id", JsonValue.Of("other")); root.Get("resources").Add(other);
                    parts.Get("point_stock").Set("resource", JsonValue.Of("other"));
                }
                root.Get("entity_replacement_effects")[0].Set("source", JsonValue.Of(source ? "source" : "destination"));
                // Keep the upgrade crew outside the target footprint until normal travel reaches it.
                StartupContentAuthoring.Position(root, "crew", JsonValue.Number(source ? "23" : "43"));
                var parking = root.Get("entities")[1].Clone(); parking.Set("id", JsonValue.Of("parking")); root.Get("entities").Add(parking);
                StartupContentAuthoring.Add(root, "parking", "parking", 0, Json.Parse("{\"space\":\"field.points\",\"position\":70}"));
                edit?.Invoke(root);
            });
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 31, Seats = new List<Controller> { Controller.Human, Controller.Human }, Mods = { ResourceFlowsModule.ModuleId } });
        private static EntityId Find(SimGame game, string def) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == def).Id;
        private static ResourceId Resource(SimGame game)
        { if (!game.Seat(0).View.TryFindResource("supply", out var id)) throw new InvalidOperationException("missing public resource"); return id; }
        private static List<RouteView> Routes(SimGame game, int seat = 0)
        { var list = new List<RouteView>(); game.Seat(seat).View.Feature<LogisticsView>().CopyRoutes(list); return list; }
        private static List<LogisticsDemandView> Demands(SimGame game, int seat = 0)
        { var list = new List<LogisticsDemandView>(); game.Seat(seat).View.Feature<LogisticsView>().CopyDemands(list); return list; }
        private static LogisticsOperationalSnapshot Balance(SimGame game, EntityId entity, Code port = default, ResourceEndpointId? endpoint = null)
        {
            Code check = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity, Resource(game), out var value, port, endpoint);
            if (!check.IsNone) throw new InvalidOperationException("replacement material diagnostics " + entity + "/" + port + "/" + check);
            return value;
        }
        private static void ObserveMaterial(Check c, SimGame game, ResourceEndpointId destination)
        {
            var route = Routes(game).Single();
            var balances = new[] { Balance(game, route.From, route.SourcePort), Balance(game, route.To, route.DestinationPort, destination), Balance(game, Find(game, "machine")) };
            c.That(balances.Select(value => value.Endpoint).Distinct().Count() == 3, "actual source, destination and cargo are distinct owned buffers");
            c.Equal(Fix.FromInt(20), balances.Aggregate(Fix.Zero, (sum, value) => sum + value.Stock), "every physical update conserves original finite20 across real route buffers and cargo");
            c.That(balances.All(value => value.Stock >= Fix.Zero && value.InboundPromised >= Fix.Zero && value.OutboundPromised >= Fix.Zero && value.OutboundPromised <= value.Stock),
                "actual material and inbound/outbound claims remain representable and funded");
        }
        private static (SimGame Game, SimGame Twin) Deliver(Check c, GameFactory factory, SimGame game, SimGame twin, int routeId, ResourceEndpointId destination)
        {
            // Observations/transport only: original engine rules own routes, loading, motion and unloading.
            bool savedLoaded = false, delivered = false; int steps = 0;
            IReplayRecorder recorder = null; var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            try
            {
                while (!delivered && steps++ < 160)
                {
                    game.Update(); twin.Update();
                    if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("replacement physical continuation fault: " + game.Fault + "/" + twin.Fault);
                    ObserveMaterial(c, game, destination); ObserveMaterial(c, twin, destination);
                    c.Equal(game.Hash(), twin.Hash(), "saved route sessions agree at every physical update");
                    var route = Routes(game).Single(); var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                    var carrier = game.Seat(0).Snapshot.Entities.Single(entity => entity.Def == "machine");
                    var from = (PointSnapshot)game.Seat(0).Snapshot.Entities.Single(entity => entity.Id == route.From).Spatial;
                    var to = (PointSnapshot)game.Seat(0).Snapshot.Entities.Single(entity => entity.Id == route.To).Spatial;
                    var position = (PointSnapshot)carrier.Spatial;
                    var loaded = jobs.FirstOrDefault(job => job.IsHaul && job.Route == routeId && job.ToilIndex == 2 && job.Loaded.IsPositive);
                    if (!savedLoaded && loaded.Loaded.IsPositive && position.Position > Fix.Min(from.Position, to.Position) + Fix.One && position.Position < Fix.Max(from.Position, to.Position) - Fix.One)
                    {
                        var incoming = Balance(game, route.To, route.DestinationPort, destination); var cargo = Balance(game, carrier.Id);
                        ulong state = game.Hash(); game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                        c.That(game.Hash() == state && twin.Hash() == state, "loaded in-transit save retains complete actual route state");
                        var restoredJobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(restoredJobs);
                        c.That(restoredJobs.Any(job => job.Id == loaded.Id && job.Route == routeId && job.Worker == loaded.Worker && job.ToilIndex == 2 && job.Loaded == loaded.Loaded),
                            "same loaded job, carrier and route resume without resubmission");
                        c.That(((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(entity => entity.Id == carrier.Id).Spatial).Position == position.Position &&
                            Balance(game, carrier.Id).Stock == cargo.Stock && Balance(game, route.To, route.DestinationPort, destination).InboundPromised == incoming.InboundPromised,
                            "loaded save preserves real physical position, cargo and destination claim");
                        ObserveMaterial(c, game, destination); ObserveMaterial(c, twin, destination);
                        recorder = game.RecordReplay(limits); savedLoaded = true;
                    }
                    delivered = Balance(game, route.To, route.DestinationPort, destination).Stock >= route.Settings.Batch &&
                        game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Entries.Any(entry => entry.Item.IsTransfer &&
                            entry.Item.Source.Endpoint == ResourceEndpointId.ForEntity(carrier.Id) && entry.Item.Destination.Endpoint == destination && entry.Item.Amount.IsPositive);
                }
                c.That(savedLoaded, "replacement route really travels loaded before delivery, not an instant checkpoint fallback");
                c.That(delivered, "rebound route delivers the entire original five-unit batch through actual handling and travel");
                if (recorder != null)
                {
                    c.Equal(0L, recorder.CommandCount, "saved loaded continuation needs no replacement route or duplicate haul request");
                    var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
                    c.That(verified.Success && verified.Game != null && verified.ActualHash == game.Hash(), "actual loaded route replay reaches identical physical state: " + verified.Failure + "/" + verified.Detail);
                }
                return (game, twin);
            }
            finally { recorder?.Dispose(); }
        }
        private static void Send(Check c, SimGame game, Command command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "pure public admission");
            int sequence = game.Seat(0).Submit(command); game.Update();
            long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Any(a => a.Sequence == sequence && a.Accepted), "normal engine inbox commits intent");
        }
        private static void Setup(Check c, SimGame game, bool source, bool named)
        {
            var resource = Resource(game); Code port = named ? new Code("stock") : Code.None;
            Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
            Send(c, game, new ConfigureStockPolicyCommand { Store = Find(game, source ? "source" : "destination"), Port = port, Resource = resource,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.FromInt(5), Fix.FromInt(25), Fix.FromInt(30), 47, LogisticsReserveAccess.Protected)) });
            Send(c, game, new AddRouteCommand { From = Find(game, "source"), To = Find(game, "destination"), SourcePort = port, DestinationPort = port,
                Resource = resource, Settings = new LogisticsRouteSettings(false, Fix.FromInt(5), 19) });
        }
        private static void Run(Check c)
        {
            foreach (string mode in new[] { "binding", "stable", "changed", "multi", "pinned", "current", "fresh", "fresh-last" }) IndependentPolicy(c, mode);
            IndependentRefusals(c);
            FreshSelectionFailures(c);
            RouteReselection(c);
            foreach (bool source in new[] { true, false }) foreach (bool named in new[] { false, true })
            {
                var factory = Factory(source, named); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                Setup(c, game, source, named);
                var old = Find(game, source ? "source" : "destination"); var route = Routes(game).Single(); var demand = Demands(game).Single();
                c.That(Routes(game, 1).Count == 0 && Demands(game, 1).Count == 0, "logistics intent stays company-private");
                var upgrade = new PostOrderCommand { Target = old, Plan = "upgrade" }; ulong before = game.Hash();
                c.Equal(Code.None, game.Seat(0).Preview(upgrade), "compatible authored replacement admitted"); c.Equal(before, game.Hash(), "admission changes no policy or clock state");
                Send(c, game, upgrade); var twin = factory.Load(game.Save());
                int steps = 0;
                while (!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next") && steps++ < 25)
                { game.Update(); twin.Update(); if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("replacement fault: " + game.Fault + "/" + twin.Fault); }
                var made = Find(game, "source.next"); var rebound = Routes(game).Single(); var band = Demands(game).Single();
                c.That(made != old && !game.Seat(0).Snapshot.Entities.Any(e => e.Id == old), "staffed update retires old site once");
                c.That(rebound.Id == route.Id && rebound.From == (source ? made : route.From) && rebound.To == (source ? route.To : made), "same standing route rebinds either leg");
                c.That(rebound.SourcePort == route.SourcePort && rebound.DestinationPort == route.DestinationPort && rebound.Settings.Batch == route.Settings.Batch &&
                    rebound.Settings.Priority == route.Settings.Priority && !rebound.Settings.Enabled, "route ports and explicit policy survive");
                c.That(rebound.CreatedTick == route.CreatedTick && rebound.LastDispatchTick == route.LastDispatchTick, "route queue age and dispatch history are not recreated");
                c.That(band.Destination == made && band.Endpoint == ResourceEndpointId.ForEntity(made) && band.Source.Identity == made.Value && band.Source.Entry == demand.Source.Entry,
                    "one native demand maps site endpoint and its owned identity together");
                c.That(band.Settings.Matches(demand.Settings) && band.Replenishing == demand.Replenishing && band.CreatedTick == demand.CreatedTick && band.LastDispatchTick == demand.LastDispatchTick,
                    "stock bands hysteresis and chronology survive replacement");
                c.That(route.From != rebound.From || route.To != rebound.To, "prior detached route facts retain retired identity");
                c.Equal(game.Hash(), twin.Hash(), "pending saved twins rebind identically");
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save()); c.Equal(game.Hash(), twin.Hash(), "rebound policies save through the existing codecs");
                // Free the target through physical worker travel, never by moving an entity in the harness.
                foreach (var session in new[] { game, twin })
                {
                    Send(c, session, new SetHomeCommand { Worker = Find(session, "crew"), Home = Find(session, "parking") });
                    Send(c, session, new ConfigureRouteCommand { Route = route.Id, Settings = new LogisticsRouteSettings(true, route.Settings.Batch, route.Settings.Priority) });
                    Send(c, session, new SetWorkerHeldCommand { Worker = Find(session, "machine"), Held = false });
                }
                (game, twin) = Deliver(c, factory, game, twin, route.Id, ResourceEndpointId.ForEntity(rebound.To));
                c.Equal(game.Hash(), twin.Hash(), "productive continuation remains deterministic");
                c.Equal(isolatedHash, isolated.Hash(), "another session has no replacement state");
            }
            foreach (string reason in new[] { "port", "capacity", "resource", "owner" })
            {
                var factory = Factory(false, true, reason); var game = New(factory); Setup(c, game, false, true);
                var old = Find(game, "destination"); var command = new PostOrderCommand { Target = old, Plan = "upgrade" };
                var expected = reason == "port" ? Codes.NotFound : reason == "capacity" ? Codes.InvalidArgument :
                    reason == "owner" ? Codes.NotOwned : StandardCodes.NotAccepted;
                ulong before = game.Hash(); c.Equal(expected, game.Seat(0).Preview(command), "incompatible " + reason + " refuses before mutation");
                c.Equal(before, game.Hash(), "refused preview preserves authoritative state");
                int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
                c.That(answers.Any(a => a.Sequence == sequence && a.Refusal == expected), "inbox reports identical typed refusal");
                c.That(game.Fault == null && Routes(game).Single().To == old && Demands(game).Single().Destination == old, "refusal preserves original logistics intent");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused checkpoint remains loadable");
            }
            {
                var factory = Factory(false, false); var game = New(factory); Setup(c, game, false, false);
                var site = Find(game, "destination"); Send(c, game, new PostOrderCommand { Target = site, Plan = "inspect" });
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                c.That(jobs.Any(j => j.Target == site && j.State == JobState.Assigned), "ordinary timed work has acquired its original site");
                ulong before = game.Hash(); var command = new PostOrderCommand { Target = site, Plan = "upgrade" };
                c.Equal(WorkBoard.ReplacementActiveExecution, game.Seat(0).Preview(command), "live original-site work blocks replacement before mutation");
                c.Equal(before, game.Hash(), "busy-site admission retains jobs and policies");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "busy-site checkpoint retains original references");
            }
        }
        private static void IndependentPolicy(Check c, string mode)
        {
            var factory = Factory(false, true, mode); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            if (mode == "multi" || mode == "current") Send(c, game, new SelectResourcePortCommand { Entity = Find(game, "destination"), Port = new Code("stock"), Endpoint = new ResourceEndpointId("proof.pools", 2) });
            Setup(c, game, false, true);
            if (mode == "pinned") Send(c, game, new SelectResourcePortCommand { Entity = Find(game, "destination"), Port = new Code("stock"), Endpoint = new ResourceEndpointId("proof.pools", 2) });
            var original = Demands(game).Single(); var route = Routes(game).Single(); var old = Find(game, "destination");
            var upgrade = new PostOrderCommand { Target = old, Plan = "upgrade" }; ulong before = game.Hash();
            Code admission = game.Seat(0).Preview(upgrade);
            c.Equal(Code.None, admission, mode + " stock policy admits its declared replacement backing");
            c.Equal(before, game.Hash(), "independent stock admission is read-only");
            if (!admission.IsNone) return; // Preserve the real baseline refusal rather than fabricate a replacement.
            Send(c, game, upgrade); var twin = factory.Load(game.Save()); int steps = 0;
            while (!game.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "source.next") && steps++ < 25)
            {
                game.Update(); twin.Update();
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("independent stock replacement fault " + game.Fault + "/" + twin.Fault);
            }
            var made = Find(game, "source.next"); var band = Demands(game).Single();
            var endpoint = mode == "changed" ? ResourceEndpointId.ForEntity(made) : new ResourceEndpointId("proof.pools", mode == "multi" || mode == "current" || mode == "fresh-last" ? 2 : 1);
            c.That(band.Destination == made && band.Endpoint == endpoint && band.Source.Identity == made.Value && band.Source.Entry == original.Source.Entry,
                mode + " original demand rebinds actual backing, site and source together");
            c.That(band.Settings.Matches(original.Settings) && band.Replenishing == original.Replenishing && band.CreatedTick == original.CreatedTick && band.LastDispatchTick == original.LastDispatchTick,
                "independent policy keeps settings, hysteresis, age and dispatch history");
            c.That(original.Destination == old && original.Endpoint == (mode == "binding" || FreshSelector(mode) ? ResourceEndpointId.ForEntity(old) : new ResourceEndpointId("proof.pools", mode == "multi" || mode == "current" ? 2 : 1)),
                "detached policy retains its exact original facts");
            c.That(Routes(game, 1).Count == 0 && Demands(game, 1).Count == 0, "independent policy remains company-private");
            c.Equal(game.Hash(), twin.Hash(), "pending replacement restores identical policy binding");
            game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            foreach (var session in new[] { game, twin })
            {
                bool automatic = MultiPolicyCase(mode) || FreshSelector(mode);
                if (automatic)
                {
                    var choices = new List<ResourcePortChoiceSnapshot>();
                    c.Equal(Code.None, session.Seat(0).View.Feature<InventoryView>().ReadResourcePortChoices(made, new Code("stock"), choices), "restored retained selector is publicly readable");
                    var selected = FreshSelector(mode) ? endpoint : new ResourceEndpointId("proof.pools", 2);
                    c.That(choices.Count == 2 && choices.Single(choice => choice.Selected).Endpoint == selected, "replacement preserves retained selection or explicit fresh initialization independently of sorted choice order");
                    c.That(Balance(session, made, new Code("stock")).Endpoint == selected, "current selection remains the actual declared store");
                }
                var requested = automatic ? new LogisticsDemandSettings(true, Fix.FromInt(5), Fix.FromInt(5), Fix.FromInt(5), original.Settings.Priority, original.Settings.ReserveAccess) : original.Settings;
                Send(c, session, new ConfigureStockPolicyCommand { Store = made, Port = new Code("stock"), Endpoint = endpoint, Resource = Resource(session), Change = LogisticsDemandChange.Replace(requested) });
                Send(c, session, new SetHomeCommand { Worker = Find(session, "crew"), Home = Find(session, "parking") });
                if (!automatic) Send(c, session, new ConfigureRouteCommand { Route = route.Id, Settings = new LogisticsRouteSettings(true, route.Settings.Batch, route.Settings.Priority) });
                Send(c, session, new SetWorkerHeldCommand { Worker = Find(session, "machine"), Held = false });
            }
            (game, twin) = Deliver(c, factory, game, twin, MultiPolicyCase(mode) || FreshSelector(mode) ? 0 : route.Id, endpoint);
            if (mode == "pinned")
                c.That(Balance(game, made, new Code("stock")).Stock == Fix.Zero && Balance(game, made, new Code("stock"), endpoint).Stock == Fix.FromInt(5),
                    "automatic supply delivers original pinned endpoint, not current selection or a duplicate buffer");
            c.Equal(game.Hash(), twin.Hash(), "independent policy loaded delivery agrees");
            c.Equal(isolatedHash, isolated.Hash(), "independent policy rebind remains session-local");
        }
        private static void IndependentRefusals(Check c)
        {
            foreach (string mode in new[] { "collision", "selection", "denied", "removed" })
            {
                var factory = Factory(false, true, mode); var game = New(factory); Setup(c, game, false, true);
                if (mode == "denied" || mode == "removed") Send(c, game, new SelectResourcePortCommand { Entity = Find(game, "destination"), Port = new Code("stock"), Endpoint = new ResourceEndpointId("proof.pools", 2) });
                if (mode == "collision") Send(c, game, new ConfigureStockPolicyCommand { Store = Find(game, "source"), Port = new Code("stock"),
                    Resource = Resource(game), Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.FromInt(5), Fix.FromInt(25), Fix.FromInt(30), 47, LogisticsReserveAccess.Protected)) });
                var original = Demands(game); var old = Find(game, "destination");
                var command = new PostOrderCommand { Target = old, Plan = "upgrade" }; ulong before = game.Hash();
                var expected = mode == "collision" ? Codes.InvalidArgument : ResourcePortCodes.OriginalUnavailable;
                c.Equal(expected, game.Seat(0).Preview(command), mode + " refuses rather than collapse intent or guess a selection");
                c.Equal(before, game.Hash(), "independent refusal preview has no persistent prefix");
                int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.That(results.Any(result => result.Sequence == sequence && result.Refusal == expected), "independent inbox and preview refusal agree");
                var retained = Demands(game);
                c.That(game.Fault == null && retained.Count == original.Count && retained.All(value => original.Any(prior => prior.Source.Equals(value.Source) && prior.Endpoint == value.Endpoint && prior.Settings.Matches(value.Settings))),
                    "independent refusal keeps all original policy identities and settings");
                c.That(Routes(game).Single().To == old && !game.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "source.next"), "independent refusal leaves original route and target live");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "independent refused state restores unchanged");
            }
        }
        private static void FreshSelectionFailures(Check c)
        {
            // Explicit negative protocol lens: actual private retention selects a valid but unauthored initial endpoint.
            foreach (string mode in new[] { "fresh", "fresh-last" })
            {
                var factory = Factory(false, true, mode, root => {
                    foreach (int index in new[] { 1, 3 })
                        root.Get("entities")[index].Get("components").Set("creation_probe", Json.Parse("{\"mode\":\"observe\",\"participant\":\"destination\"}"));
                });
                var game = New(factory); Setup(c, game, false, true);
                var world = game.World; var old = world.Entities.Get(Find(game, "destination"));
                var route = Routes(game).Single(); var demand = Demands(game).Single(); var next = world.Entities.NextIdentity;
                Entity candidate = null, made = null; Exception caught = null;
                ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) => {
                    if (phase != "configure" || entity?.Def.Id != "source.next") return;
                    candidate = entity;
                    entity.Get<CreationProbe>().BeforeRetain = () => entity.Get<IResourcePortSelection>(new Code("stock")).Select(world,
                        new ResourceEndpointId("proof.pools", mode == "fresh" ? 2 : 1));
                };
                try { Upgrades.TryReplace(world, old, world.Content.Get<EntityDef>("source.next"), out made); }
                catch (Exception error) { caught = error; }
                c.That(caught != null && game.Fault?.Message.Contains("prepared resource-port selection differs", StringComparison.Ordinal) == true,
                    "a fresh prepared capability must actually select its authored initial endpoint " + mode);
                c.That(made == null && candidate != null && !candidate.Alive && old.Alive && world.Entities.Get(next) == null && world.Entities.NextIdentity == next,
                    "wrong private initial selection faults before registration or source retirement " + mode);
                c.That(world.Services.Get<LogisticsService>().Routes.Single().To == route.To && world.Services.Get<LogisticsService>().Demands.Single().Endpoint == demand.Endpoint &&
                    !world.Events.HasPending, "wrong initial binding writes no persistent reference or success event " + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "wrong prepared initial selection cannot save healthy " + mode);
            }
        }
        private static void RouteReselection(Check c)
        {
            var factory = Factory(false, true, "binding"); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            var resource = Resource(game); var old = Find(game, "destination"); var stock = new Code("stock");
            Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
            Send(c, game, new AddRouteCommand { From = Find(game, "source"), To = old, SourcePort = stock, DestinationPort = stock,
                Resource = resource, Settings = new LogisticsRouteSettings(false, Fix.FromInt(5), 19) });
            var original = Routes(game).Single();
            Send(c, game, new PostOrderCommand { Target = old, Plan = "upgrade" }); var twin = factory.Load(game.Save());
            int steps = 0;
            while (!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next") && steps++ < 25)
            {
                game.Update(); twin.Update();
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("route reselection upgrade fault: " + game.Fault + "/" + twin.Fault);
            }
            var made = Find(game, "source.next"); var rebound = Routes(game).Single();
            c.That(rebound.To == made && rebound.Id == original.Id && rebound.CreatedTick == original.CreatedTick,
                "standing route selects a new backing store without resetting its identity or age");
            c.That(Demands(game).Count == 0 && Routes(game, 1).Count == 0, "route reselection invents no stock policy and remains private");
            c.Equal(game.Hash(), twin.Hash(), "pending replacement save reselects identically");
            game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            foreach (var session in new[] { game, twin })
            {
                Send(c, session, new SetHomeCommand { Worker = Find(session, "crew"), Home = Find(session, "parking") });
                Send(c, session, new ConfigureRouteCommand { Route = original.Id, Settings = new LogisticsRouteSettings(true, original.Settings.Batch, original.Settings.Priority) });
                Send(c, session, new SetWorkerHeldCommand { Worker = Find(session, "machine"), Held = false });
            }
            (game, twin) = Deliver(c, factory, game, twin, original.Id, new ResourceEndpointId("proof.pools", 1));
            c.Equal(isolatedHash, isolated.Hash(), "route backing changes stay session-local");
        }
    }
}
