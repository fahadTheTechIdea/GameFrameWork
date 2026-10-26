using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using JsonParser = Beep.SimEngine.Content.Json;

namespace Beep.SimEngine.Tests
{
    public static partial class ResourceDispositionTests
    {
        internal const string PortCase = "Work effect ports: native and independent whole bills wait for physical supply and saved timed completion";
        internal const string PortBindingCase = "Work effect bindings: opaque stores availability and acquired claim faults preserve original ownership";
        private static GameFactory PortFactory(bool scoped, string kind, Action<JsonValue> edit = null, Action<JsonValue> editEffect = null, string incidentDocument = null)
        {
            var authored = JsonParser.Parse(FrameworkProofContent.Read("work-effect-ports.json"));
            authored.Get("inventory_effects")[0].Set("kind", JsonValue.Of(kind));
            authored.Get("independent_inventory_effects")[0].Set("kind", JsonValue.Of(kind)); editEffect?.Invoke(authored);
            var declarations = JsonParser.Parse("""[{"id":"proof.pools","version":"1.0.0","requires":[{"id":"sim.inventory","version":"1.0.0"},{"id":"sim.resource-flows","version":"1.0.0"}],"documents":[]},{"id":"proof.output-ports","version":"1.0.0","requires":[{"id":"proof.pools","version":"1.0.0"},{"id":"sim.transformations","version":"1.0.0"},{"id":"sim.world-setup","version":"1.0.0"}],"documents":[]},{"id":"proof.inventory-work","version":"1.0.0","requires":[{"id":"sim.work","version":"1.0.0"}],"documents":[]}]""");
            if (incidentDocument != null) declarations[2].Get("documents").Add(JsonValue.Of("work-effect-incident-ports.json"));
            return EntityReferenceReplacementTests.Factory(moduleDocument: authored.ToString(),
                extraModules: new GameModule[] { new EndpointProofModule(), new OutputPortProofModule(), new InventoryWorkProofModule() },
                extraDeclarations: declarations.ToString(),
                extraDocuments: incidentDocument == null ? null : new Dictionary<string, string> { ["work-effect-incident-ports.json"] = incidentDocument },
                edit: root => {
                    foreach (string module in new[] { "proof.pools", "proof.output-ports", "proof.inventory-work" }) root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module));
                    var source = root.Get("entities")[0].Get("components"); source.Remove("point_stock");
                    source.Set("storage", JsonParser.Parse("""{"capacity_policy":"fixture.count.40","public":true,"initial_stock":{"supply":20,"spare":10}}"""));
                    source.Set("inventory_port", JsonParser.Parse("""{"id":"stock"}"""));
                    var target = root.Get("entities")[1].Get("components"); target.Remove("point_stock"); target.Remove("inventory_port");
                    if (scoped) target.Set("pool_port", JsonParser.Parse("""[{"id":"feed","pool":"disposition.feed","alternatives":[],"endpoint_scope":"proof.pools","original_access":"retained"},{"id":"parts","pool":"disposition.parts","alternatives":[],"endpoint_scope":"proof.pools","original_access":"retained"}]"""));
                    else {
                        target.Set("storage", JsonParser.Parse("""{"capacity_policy":"fixture.count.40","public":false,"initial_stock":{}}"""));
                        target.Set("inventory_port", JsonParser.Parse("""[{"id":"feed"},{"id":"parts"}]"""));
                    }
                    root.Get("entities")[2].Get("components").Get("worker").Set("roles", JsonParser.Parse("[\"haul\"]"));
                    root.Get("entities")[2].Get("components").Get("transporter").Set("handling", JsonValue.Of("disposition.handling"));
                    foreach (int index in new[] { 0, 1 }) root.Get("entities")[index].Get("components").Set("point_dock", JsonParser.Parse("""{"work_roles":["inspect","haul"],"resources":["supply","spare"],"slots":[{"id":"service","kind":"service","offset":1},{"id":"wait","kind":"waiting","offset":2},{"id":"exit","kind":"departure","offset":3}]}"""));
                    var parking = root.Get("entities")[0].Clone(); parking.Set("id", JsonValue.Of("parking"));
                    parking.Get("components").Get("storage").Set("initial_stock", JsonParser.Parse("{}")); root.Get("entities").Add(parking);
                    StartupContentAuthoring.Add(root, "parking", "parking", 0, JsonParser.Parse("{\"space\":\"field.points\",\"position\":70}"));
                    edit?.Invoke(root);
                });
        }
        private static SimGame PortNew(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 31, Seats = { Controller.Human, Controller.Human }, Mods = { ResourceFlowsModule.ModuleId } });
        private static void Ports(Check c)
        {
            foreach (string bad in new[] { "missing", "null", "empty", "unmapped", "unknown", "blank" })
                c.Throws<ContentException>(() => PortNew(PortFactory(true, "destroyed", editEffect: root => {
                    var effect = root.Get("inventory_effects")[0];
                    if (bad == "missing") effect.Remove("ports");
                    else if (bad == "null") effect.Set("ports", JsonParser.Parse("null"));
                    else if (bad == "empty") effect.Set("ports", JsonParser.Parse("{}"));
                    else if (bad == "unmapped") effect.Get("ports").Remove("spare");
                    else if (bad == "unknown") effect.Get("ports").Set("missing.resource", JsonValue.Of("feed"));
                    else effect.Get("ports").Set("supply", JsonValue.Of(""));
                })), "unused native effect refuses invalid explicit port map before play: " + bad);
            var deniedFactory = PortFactory(false, "destroyed", edit: root => {
                var parts = root.Get("entities")[1].Get("components"); parts.Set("inventory_port", JsonParser.Parse("{\"id\":\"parts\"}"));
                parts.Get("storage").Set("initial_stock", JsonParser.Parse("{\"supply\":5,\"spare\":5}"));
            });
            var denied = PortNew(deniedFactory); var deniedTarget = denied.Seat(0).Snapshot.Entities.Single(row => row.Def == "destination").Id;
            var deniedIntent = new SubmitPlanCommand { Intent = new PlanIntent("activity:port.dispose", ActivityWorkerSelection.CreateArguments(deniedTarget, ActivityWorkerSelection.Unbound), "effect", 61, false) };
            ulong deniedHash = denied.Hash(); c.Equal(ResourcePortCodes.DefinitionMissing, denied.Seat(0).Preview(deniedIntent), "missing named feed cannot fall back to stocked local inventory");
            c.Equal(deniedHash, denied.Hash(), "missing-binding preview is pure");
            c.Equal(ResourcePortCodes.DefinitionMissing, TestKit.Order(denied, 0, deniedIntent).Refusal, "preview and inbox share missing-binding refusal");
            c.That(denied.Fault == null && deniedFactory.Load(denied.Save()).Hash() == denied.Hash(), "normal missing-binding refusal remains healthy and loadable");
            foreach (bool scoped in new[] { false, true }) foreach (bool custom in new[] { false, true }) foreach (string kind in new[] { "consumed", "destroyed", "lost" })
                PortPhysical(c, scoped, custom, kind);
        }
        // Player authoring/commands, public views and the sole Update only. No World or material/progress edits.
        private static void PortPhysical(Check c, bool scoped, bool custom, string kind, Action<GameFactory, SimGame, int, EntityId> probe = null,
            bool bindingProbe = false, Action<JsonValue> edit = null, string incidentDocument = null)
        {
            string context = "scoped=" + scoped + "/custom=" + custom + "/" + kind;
            var factory = PortFactory(scoped, kind, edit: root => {
                if (bindingProbe) {
                    root.Get("entities")[1].Get("components").Get("pool_port")[0].Set("alternatives", JsonParser.Parse("[\"disposition.alternative\"]"));
                    root.Get("scenarios")[0].Get("settings").Get("sim.work").Set("retry", JsonValue.Of("disposition.retry"));
                }
                edit?.Invoke(root);
            }, editEffect: !bindingProbe ? null : root => {
                var alternative = root.Get("resource_pools")[0].Clone(); alternative.Set("id", JsonValue.Of("disposition.alternative"));
                alternative.Set("identity", JsonValue.Number("3")); root.Get("resource_pools").Add(alternative);
                root.Set("work_retry_profiles", JsonParser.Parse("""[{"id":"disposition.retry","initial_delay":{"numerator":1,"denominator":1,"unit":"minute"},"maximum_delay":{"numerator":4,"denominator":1,"unit":"minute"},"multiplier":2,"retry_blockers":["proof.physical-port-closed"]}]"""));
            }, incidentDocument: incidentDocument);
            var game = PortNew(factory); SimGame twin = null; var idle = PortNew(factory); ulong untouched = idle.Hash();
            EntityId Id(string name) => game.Seat(0).Snapshot.Entities.Single(row => row.Def == name).Id;
            var site = Id("destination"); var carrier = Id("machine");
            if (!game.Seat(0).View.TryFindResource("supply", out var supply) || !game.Seat(0).View.TryFindResource("spare", out var spare)) throw new InvalidOperationException("authored disposition resources missing");
            string effect = custom ? "port.dispose-independent" : "port.dispose";
            ResourceFlowKind category = kind == "consumed" ? ResourceFlowKind.Consumed : kind == "destroyed" ? ResourceFlowKind.Destroyed : ResourceFlowKind.Lost;
            LogisticsOperationalSnapshot Stock(SimGame session, ResourceId resource, Code port)
            { var refusal = session.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(site, resource, out var facts, port); if (!refusal.IsNone) throw new InvalidOperationException("effect stock " + context + "/" + refusal); return facts; }
            PlanView Plan() { var rows = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(rows); return rows.Single(); }
            List<WorkJobView> Jobs() { var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
            BigInteger Removed(ResourceId resource) => game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(row => row.Resource == resource && row.Cause == new Code(effect) && ResourceConversionInput.IsDispositionKind(row.Kind))
                .Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw);
            void Observe()
            {
                if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("work effect fault " + context + ": " + game.Fault + "/" + twin?.Fault);
                foreach (var resource in new[] { supply, spare }) {
                    Fix total = Fix.Zero;
                    foreach (var entity in game.Seat(0).Snapshot.Entities)
                        if (game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, resource, out var facts).IsNone) total += facts.Stock;
                    if (scoped) total += Stock(game, resource, new Code(resource == supply ? "feed" : "parts")).Stock;
                    c.Equal(new BigInteger(Fix.FromInt(resource == supply ? 20 : 10).Raw), new BigInteger(total.Raw) + Removed(resource), "each update conserves actual native/cargo/scoped stock plus committed disposition " + context);
                }
                if (twin != null) c.Equal(game.Hash(), twin.Hash(), "saved physical effect sessions agree each update " + context);
            }
            void Step() { game.Update(); twin?.Update(); Observe(); }
            void Reach(Func<bool> ready, string stage)
            {
                for (int i = 0; i < 180 && !ready(); i++) Step();
                if (!ready()) throw new InvalidOperationException(stage + "/" + context + " exceeded180 updates at " + game.Clock.StepIndex + "; jobs=" + string.Join(";", Jobs().Select(job => job.Id + "/" + job.State + "/" + job.ToilIndex + "/" + job.Blocker)) +
                    "; positions=" + string.Join(";", game.Seat(0).Snapshot.Entities.Where(entity => entity.Def == "machine" || entity.Def == "crew").Select(entity => entity.Def + "/" + ((PointSnapshot)entity.Spatial).Position)));
            }
            void Send(Command command) {
                var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary " + command.GetType().Name + " accepted " + context + "/" + result.Refusal);
                if (!result.Accepted) throw new InvalidOperationException("physical effect setup refused " + command.GetType().Name + "/" + result.Refusal + "/" + context);
                if (twin != null) c.That(TestKit.Order(twin, 0, command).Accepted, "saved twin accepts same intent " + context); Observe();
            }
            void Save() { game = factory.Load(game.Save()); twin = factory.Load(game.Save()); Observe(); }
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical disposition needs no business module " + context);
            if (scoped) c.That(!game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(site, supply, out _).IsNone, "scoped effect target has no local inventory fallback " + context);
            Send(new SetHomeCommand { Worker = Id("crew"), Home = Id("parking") });
            Send(new SetHomeCommand { Worker = carrier, Home = Id("source") });
            Reach(() => game.Seat(0).View.Feature<WorkView>().ReadHome(Id("crew"), out var home).IsNone && home.State == WorkerHomeState.AtHome, "crew physically clears receiving approach");
            Send(new SubmitPlanCommand { Intent = new PlanIntent("activity:" + effect, ActivityWorkerSelection.CreateArguments(site, ActivityWorkerSelection.Unbound), "disposition", 61, false) });
            c.That(Plan().State == PlanState.Pending && Plan().Order == 0 && Plan().Blocker == WorkEffectCodes.NoStock, "whole effect waits without a phantom execution or material claim " + context);
            Save(); Send(new HaulResourcesCommand { Source = Id("source"), Destination = site, SourcePort = new Code("stock"), DestinationPort = new Code("feed"), Resource = supply, Ceiling = Fix.FromInt(2), Worker = carrier, Priority = 90 });
            Reach(() => Jobs().Any(job => job.IsHaul && job.Loaded == Fix.FromInt(2) && job.ToilIndex == 2), "first material really loads for transit");
            var loaded = Jobs().Single(job => job.IsHaul); Save();
            c.That(Jobs().Any(job => job.Id == loaded.Id && job.DestinationPort == new Code("feed") && job.DestinationEndpoint == loaded.DestinationEndpoint && job.Loaded == Fix.FromInt(2)), "loaded save retains original job and actual named destination " + context);
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            using (var recording = game.RecordReplay(limits)) {
                Reach(() => Stock(game, supply, new Code("feed")).Stock == Fix.FromInt(2) && !Jobs().Any() &&
                    game.Seat(0).View.Feature<WorkView>().ReadHome(carrier, out var home).IsNone && home.State == WorkerHomeState.AtHome, "first physical delivery and return settle");
                c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "actual saved loaded named-port delivery replay " + context);
            }
            c.That(Plan().State == PlanState.Pending && Plan().Order == 0 && Plan().Blocker == WorkEffectCodes.NoStock && Stock(game, supply, new Code("feed")).OutboundPromised.IsZero,
                "missing second resource reserves and consumes none of the available first resource " + context);
            Send(new HaulResourcesCommand { Source = Id("source"), Destination = site, SourcePort = new Code("stock"), DestinationPort = new Code("parts"), Resource = spare, Ceiling = Fix.One, Worker = carrier, Priority = 90 });
            Reach(() => Plan().Order > 0, "whole supplied bill activates the original plan execution");
            int order = Plan().Order;
            ActivityNodeView Node() { var nodes = new List<ActivityNodeView>(); if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes)) throw new InvalidOperationException("actual supplied node missing"); return nodes.Single(); }
            Reach(() => Node().Progress.WholeTicks > 0 && Node().Progress.WholeTicks < 4, "travel precedes partial positive-time staffed work");
            var partial = Node(); c.Equal(BigInteger.Zero, Removed(supply), "partial machine work cannot apply the final material effect early " + context); Save();
            c.That(Node().Progress.Equals(partial.Progress) && Node().JobId == partial.JobId, "saved partial effect keeps exact progress and job " + context);
            if (probe != null) { probe(factory, game, order, site); return; }
            using (var recording = game.RecordReplay(limits)) {
                Reach(() => Plan().State == PlanState.Done, "earned whole bill disposition completes"); for (int i = 0; i < 8; i++) Step();
                c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "actual partial work and once-only effect replay " + context);
            }
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), Removed(supply), "exact two supplied units removed once " + context);
            c.Equal(new BigInteger(Fix.One.Raw), Removed(spare), "exact spare removed once from its declared store " + context);
            var summary = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            c.That(summary.Aggregates.Where(row => row.Cause == new Code(effect)).All(row => row.Kind == category), "authored kind survives endpoint transactions without consumed substitution " + context);
            c.That(Stock(game, supply, new Code("feed")).Stock.IsZero && Stock(game, spare, new Code("parts")).Stock.IsZero && Stock(game, supply, new Code("feed")).OutboundPromised.IsZero,
                "commit spends actual endpoint stock and retires only acquired effect promises " + context);
            var privatePlans = new List<PlanView>(); game.Seat(1).View.Feature<WorkView>().CopyPlans(privatePlans); c.Equal(0, privatePlans.Count, "effect plan remains company-private " + context);
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal named-port effect and journal save canonically " + context); c.Equal(untouched, idle.Hash(), "effect changes no unrelated session " + context);
        }
        private static void PortBindings(Check c)
        {
            // Raw providers are armed ONLY at this explicit negative-protocol boundary. Physical setup above remains public intent.
            PortPhysical(c, true, true, "destroyed", (factory, earned, order, site) => {
                byte[] checkpoint = earned.Save(); var idle = factory.Load(checkpoint); ulong isolated = idle.Hash();
                var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
                foreach (string mode in new[] { "healthy-wrapper", "unavailable-before", "redirect-after-acquire", "unavailable-after-acquire", "acquire-throw", "short-take" }) {
                    var game = factory.Load(checkpoint); var pools = game.World.Services.Get<EndpointPools>();
                    var feed = (PoolPort)game.World.Entities.Get(site).Get<IResourcePort>(new Code("feed"));
                    game.Seat(0).View.TryFindResource("supply", out var supply); game.Seat(0).View.TryFindResource("spare", out var spare);
                    pools.FaultEndpoint = 1; pools.ProbeConversion = mode != "short-take";
                    if (mode == "short-take") pools.ClaimFault = ClaimFaultMode.ShortTake;
                    bool closed = mode == "unavailable-before"; int acquired = 0;
                    feed.ResolveProbe = (_, _) => closed ? ResourcePortResolution.Unavailable(PhysicalAvailabilityProbe.Closed) : (ResourcePortResolution?)null;
                    if (mode == "redirect-after-acquire" || mode == "unavailable-after-acquire" || mode == "acquire-throw")
                        pools.AfterReserveOut = (_, owner, _, _, _) => {
                            if (!owner.Equals(new ReservationOwner("sim.work", order))) return;
                            acquired++;
                            if (mode == "redirect-after-acquire") feed.Select(game.World, new ResourceEndpointId("proof.pools", 3));
                            if (mode == "unavailable-after-acquire") closed = true;
                            if (mode == "acquire-throw") throw new InvalidOperationException("proof.original-disposition-acquire");
                        };
                    PlanView Plan() { var rows = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(rows); return rows.Single(); }
                    BigInteger Removed(ResourceId resource) => game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                        .Where(row => row.Resource == resource && row.Cause == new Code("port.dispose-independent") && row.Kind == ResourceFlowKind.Destroyed)
                        .Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw);
                    ulong pure = game.Hash();
                    c.Equal(closed ? PhysicalAvailabilityProbe.Closed : Code.None, game.Seat(0).View.Feature<WorkView>().PreviewOrderEffect(order), "same whole-bill preview reports actual availability: " + mode);
                    c.Equal(pure, game.Hash(), "opaque binding preview is pure: " + mode);
                    c.Equal(0, pools.Get(1).Reservations.Count + pools.Get(2).Reservations.Count, "preview acquires no stock prefix: " + mode);
                    if (mode == "unavailable-before") {
                        for (int i = 0; i < 12; i++) game.Update();
                        c.That(game.Fault == null && Plan().State == PlanState.Active && Plan().Order == order && Plan().Blocker == PhysicalAvailabilityProbe.Closed,
                            "authored transient blocker retains the original earned execution");
                        c.Equal(Fix.FromInt(2), pools.Get(1).Amount(supply), "unavailable preflight does not spend first material");
                        c.Equal(Fix.One, pools.Get(2).Amount(spare), "unavailable preflight does not spend second material");
                        c.Equal(0, pools.Get(1).Reservations.Count + pools.Get(2).Reservations.Count, "unavailable preflight leaves no acquired prefix");
                        c.Equal(BigInteger.Zero, Removed(supply), "waiting effect publishes no destruction");
                        var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                        var waiting = orders.Single(row => row.Id == order);
                        c.That(waiting.State == OrderState.Completing && waiting.CompletionFailures > 0 && !waiting.CompletionEffectApplied,
                            "waiting effect retains earned completion with the original authored retry owner");
                        var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes);
                        c.That(nodes.Single().State == NodeState.Done && nodes.Single().Progress.WholeTicks == 4, "unavailable final effect does not repeat already earned machine work");
                        var resumed = factory.Load(game.Save()); c.Equal(game.Hash(), resumed.Hash(), "saved completion retry retains original authoritative progress and deadline");
                        closed = false;
                    }
                    var healthyPage = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
                    using (var recording = game.RecordReplay(limits)) {
                        for (int i = 0; i < 16 && game.Fault == null && Plan().State != PlanState.Done; i++) game.Update();
                        if (mode == "healthy-wrapper" || mode == "unavailable-before") {
                            c.That(game.Fault == null, "valid actual stock completes without false CLR binding change: " + mode + "/" + game.Fault);
                            if (game.Fault == null) {
                                c.Equal(PlanState.Done, Plan().State, "original physical execution completes once: " + mode);
                                for (int i = 0; i < 8; i++) game.Update();
                                c.Equal(new BigInteger(Fix.FromInt(2).Raw), Removed(supply), "exact original supply disposition: " + mode);
                                c.Equal(new BigInteger(Fix.One.Raw), Removed(spare), "exact original spare disposition: " + mode);
                                c.Equal(0, pools.Get(1).Reservations.Count + pools.Get(2).Reservations.Count, "actual complete claim vector retired: " + mode);
                                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "healthy wrapper/retry completion saves without wrapper identity: " + mode);
                                c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "actual saved earned completion replay: " + mode);
                            }
                        } else {
                            string cause = mode == "acquire-throw" ? "proof.original-disposition-acquire" : mode == "short-take" ? "conversion input violated exact consumption" : "disposition port changed after complete claim acquisition";
                            c.That(game.Fault != null && game.Fault.Message.Contains(cause), "original provider/binding failure retained: " + mode + "/" + game.Fault);
                            c.Equal(mode == "short-take" ? Fix.Zero : Fix.FromInt(2), pools.Get(1).Amount(supply), "actual stock prefix is diagnostic, never a fabricated rollback: " + mode);
                            c.Equal(Fix.One, pools.Get(2).Amount(spare), "unvisited second material remains actual stock: " + mode);
                            c.Equal(mode == "short-take" ? 0 : 1, pools.Get(1).Reservations.Count, "original first-book prefix retained truthfully: " + mode);
                            c.Equal(mode == "acquire-throw" ? 0 : 1, pools.Get(2).Reservations.Count, "whole acquisition versus thrown first acquisition has the actual second-book prefix: " + mode);
                            foreach (var receipt in pools.Get(1).Reservations.Concat(pools.Get(2).Reservations))
                                c.That(receipt.Owner.Equals(new ReservationOwner("sim.work", order)) && receipt.Direction == ReservationDirection.Outbound,
                                    "diagnostic orphan prefix still belongs only to original earned work: " + mode);
                            c.That(mode == "short-take" || acquired == 1, "fault occurs after the actual original reserve callback: " + mode);
                            if (mode == "redirect-after-acquire") c.Equal(new ResourceEndpointId("proof.pools", 3), feed.SelectedEndpoint, "hostile selected endpoint is not silently substituted back");
                            c.Equal(Fix.Zero, pools.Get(3).Amount(supply), "empty alternate store never receives or provides fictitious material: " + mode);
                            c.That(!healthyPage.Entries.Any(row => row.Item.Cause == new Code("port.dispose-independent")), "detached prior history acquires no later disposition: " + mode);
                            c.That(!game.World.Events.HasPending, "no queued unverified conversion success: " + mode);
                            c.Throws<InvalidOperationException>(() => game.Save(), "terminal effect cannot save: " + mode);
                            c.Throws<InvalidOperationException>(() => game.Hash(), "terminal effect cannot advertise healthy hash: " + mode);
                            c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0), "terminal effect withholds healthy history: " + mode);
                            c.Throws<InvalidOperationException>(() => recording.Finish(), "partial provider failure cannot finish a resumable replay: " + mode);
                            var first = game.Fault; long stopped = game.Clock.StepIndex; game.Update();
                            c.That(ReferenceEquals(first, game.Fault) && stopped == game.Clock.StepIndex, "first failure and sole game clock remain stopped: " + mode);
                        }
                    }
                    c.Equal(isolated, idle.Hash(), "opaque/faulted providers do not alter another loaded session: " + mode);
                }
            }, bindingProbe: true);
        }
    }
}
