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
    internal static class RecoveryReferenceReplacementTests
    {
        internal const string Case = "Recovery replacement references: preflight disposition retained age and physical evacuation return";
        internal const string IndependentCase = "Recovery replacement references: named independent evacuation preserves backing age and loaded continuation";
        internal static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(IndependentCase, Independent); }
        private static GameFactory Factory(bool direct, bool named, bool returning, string refusal = null, string independent = null)
        {
            var modules = new List<GameModule> { new LogisticsRecoveryPolicyModule() };
            var declarations = Json.Parse("[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"recovery-replacement.json\"]}]");
            var documents = new Dictionary<string, string> { ["recovery-replacement.json"] = FrameworkProofContent.Read("recovery-replacement.json") };
            if (independent != null)
            {
                modules.Add(new EndpointProofModule()); modules.Add(new OutputPortProofModule());
                var poolDeclarations = Json.Parse("""[{"id":"proof.pools","version":"1.0.0","requires":[{"id":"sim.inventory","version":"1.0.0"},{"id":"sim.resource-flows","version":"1.0.0"}],"documents":["port-replacement.json"]},{"id":"proof.output-ports","version":"1.0.0","requires":[{"id":"proof.pools","version":"1.0.0"},{"id":"sim.transformations","version":"1.0.0"},{"id":"sim.world-setup","version":"1.0.0"}],"documents":[]}]""");
                for (int i = 0; i < poolDeclarations.Count; i++) declarations.Add(poolDeclarations[i]);
                var pools = Json.Parse(FrameworkProofContent.Read("port-replacement.json"));
                var first = pools.Get("resource_pools")[0]; first.Set("public", JsonValue.Of(true)); first.Set("evacuate", JsonValue.Of(true));
                var second = first.Clone(); second.Set("id", JsonValue.Of("replacement.next")); second.Set("identity", JsonValue.Number("2")); pools.Get("resource_pools").Add(second);
                documents.Add("port-replacement.json", pools.ToString());
            }
            return EntityReferenceReplacementTests.Factory(
                extraModules: modules,
                extraDeclarations: declarations.ToString(),
                extraDocuments: documents,
                edit: root =>
                {
                    var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                    scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"replacement.recovery\"}"));
                    var actor = root.Get("entities")[returning ? 2 : 0];
                    var parts = actor.Get("components"); string stock = direct ? "point_stock" : "storage";
                    if (!returning)
                    {
                        parts.Remove("point_stock");
                        parts.Set(stock, Json.Parse(direct
                            ? "{\"resource\":\"supply\",\"capacity_policy\":\"fixture.count.40\",\"initial\":6,\"public\":true,\"evacuate\":true}"
                            : "{\"capacity_policy\":\"fixture.count.40\",\"public\":true,\"evacuate\":true,\"accepts\":[\"supply\"],\"phases\":[],\"initial_stock\":{\"supply\":6}}"));
                        if (named) parts.Set("inventory_port", Json.Parse("{\"id\":\"stock\"}"));
                    }
                    else
                    {
                        stock = "storage"; parts.Get(stock).Set("initial_stock", Json.Parse("{\"supply\":6}"));
                        root.Get("entity_replacement_effects")[0].Set("source", JsonValue.Of("machine"));
                    }
                    var next = parts.Clone(); root.Get("entities")[3].Set("components", next);
                    if (refusal == "private") next.Get(stock).Set("public", JsonValue.Of(false));
                    if (refusal == "evacuation") next.Get(stock).Set("evacuate", JsonValue.Of(false));
                    if (refusal == "port") next.Remove("inventory_port");
                    if (refusal == "transporter") next.Remove("transporter");
                    // Keep unused source stock out of the cargo-return proof's conservation totals.
                    if (returning)
                    {
                        root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("0"));
                        StartupContentAuthoring.Position(root, "source", JsonValue.Number("80"));
                        root.Get("point_spaces")[0].Set("interaction_reach", JsonValue.Number("1"));
                    }
                    var parking = root.Get("entities")[1].Clone(); parking.Set("id", JsonValue.Of("parking"));
                    root.Get("entities").Add(parking);
                    StartupContentAuthoring.Add(root, "parking", "parking", 0, Json.Parse("{\"space\":\"field.points\",\"position\":70}"));
                    if (independent != null)
                    {
                        scenario.Get("modules").Add(JsonValue.Of("proof.pools")); scenario.Get("modules").Add(JsonValue.Of("proof.output-ports"));
                        var donor = actor.Clone(); donor.Set("id", JsonValue.Of("donor"));
                        donor.Get("components").Remove("inventory_port");
                        donor.Get("components").Get(stock).Set("public", JsonValue.Of(false)); donor.Get("components").Get(stock).Set("evacuate", JsonValue.Of(false));
                        root.Get("entities").Add(donor); StartupContentAuthoring.Add(root, "donor", "donor", 0, Json.Parse("{\"space\":\"field.points\",\"position\":0}"));
                        parts.Get(stock).Set("initial_stock", Json.Parse("{}")); parts.Get(stock).Set("public", JsonValue.Of(false)); parts.Get(stock).Set("evacuate", JsonValue.Of(false));
                        parts.Remove("inventory_port");
                        parts.Set("pool_port", Json.Parse("""{"id":"stock","pool":"replacement.receiver","alternatives":["replacement.next"],"endpoint_scope":"proof.pools","original_access":"retained"}"""));
                        if (independent == "fixed") parts.Get("pool_port").Set("alternatives", Json.Parse("[]"));
                        next = parts.Clone(); root.Get("entities")[3].Set("components", next);
                        if (independent == "retained")
                        { next.Get("pool_port").Set("pool", JsonValue.Of("replacement.next")); next.Get("pool_port").Set("alternatives", Json.Parse("[\"replacement.receiver\"]")); }
                        if (independent == "removed")
                        { next.Get("pool_port").Set("pool", JsonValue.Of("replacement.next")); next.Get("pool_port").Set("alternatives", Json.Parse("[]")); }
                        if (independent == "missing" || independent == "local") next.Remove("pool_port");
                        if (independent == "local") next.Set("inventory_port", Json.Parse("{\"id\":\"stock\"}"));
                        root.Set("work_policies", Json.Parse("""[{"id":"replacement.carrier","enabled":true,"priority":0,"allowed_roles":["haul"],"area":null,"interruptible":true,"emergency_eligible":true,"assignment_mode":"all","states":{"disabled":"blocked","standby":"blocked"}}]"""));
                        root.Get("entities")[2].Get("components").Set("work_policy", Json.Parse("{\"profile\":\"replacement.carrier\"}"));
                    }
                });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 31, Seats = new List<Controller> { Controller.Human, Controller.Human }, Mods = { ResourceFlowsModule.ModuleId } });
        private static EntityId Find(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Id;
        private static List<LogisticsDispatchOffer> Offers(SimGame game, int seat = 0)
        { var values = new List<LogisticsDispatchOffer>(); game.Seat(seat).View.Feature<LogisticsView>().CopyQueue(values); return values; }
        private static LogisticsDispatchOffer Offer(SimGame game, bool returning)
            => Offers(game).Single(o => o.Kind == (returning ? LogisticsDispatchKind.CargoReturn : LogisticsDispatchKind.Evacuation));
        private static Fix Amount(SimGame game, EntityId actor)
        {
            var facets = new List<Facet>(); game.Seat(0).View.Inspect(actor, facets);
            return facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static Fix Total(SimGame game) => game.Seat(0).Snapshot.Entities.Aggregate(Fix.Zero, (sum, e) => sum + Amount(game, e.Id));
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("recovery replacement fault: " + game.Fault + "/" + twin?.Fault); }
        private static void Send(Check c, SimGame game, Command command, SimGame twin = null)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "public admission");
            int sequence = game.Seat(0).Submit(command); twin?.Seat(0).Submit(command); Step(game, twin); long cursor = 0;
            var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Any(a => a.Sequence == sequence && a.Accepted), "engine inbox commits player intent");
        }
        private static void Physical(Check c, bool direct, bool named, bool returning)
        {
            var factory = Factory(direct, named, returning); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            var old = Find(game, returning ? "machine" : "source");
            Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
            for (int i = 0; i < 4; i++) Step(game);
            var before = Offer(game, returning);
            c.That(before.RecoveryOrigin == old && before.Requested == Fix.FromInt(6) && before.Priority == 73, "actual recovery intent has authored source quantity and priority");
            if (named) c.Equal(Code.None, before.SourcePort, "native endpoint aliases retain one canonical default recovery binding");
            ulong hash = game.Hash(); var command = new PostOrderCommand { Plan = "upgrade", Target = old };
            c.Equal(Code.None, game.Seat(0).Preview(command), "compatible recovery source upgrade admits");
            c.Equal(hash, game.Hash(), "preflight changes no recovery chronology or stock");
            Send(c, game, command); var twin = factory.Load(game.Save());
            for (int i = 0; i < 30 && !game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next"); i++) Step(game, twin);
            if (!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next"))
            {
                var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException("replacement timeout; returning=" + returning + "; clock=" + game.Tick +
                    "; orders=" + string.Join(",", orders.Select(o => o.State + "/" + o.Blocker)) +
                    "; jobs=" + string.Join(",", jobs.Select(j => j.State + "/" + j.Blocker + "/" + j.ToilIndex)) +
                    "; actors=" + string.Join(",", game.Seat(0).Snapshot.Entities.Where(e => e.Spatial is PointSnapshot).Select(e => e.Def + ":" + ((PointSnapshot)e.Spatial).Position)));
            }
            var made = Find(game, "source.next"); var retained = Offer(game, returning);
            c.Equal(made, retained.RecoveryOrigin, "existing recovery origin rebinds");
            c.Equal(ResourceEndpointId.ForEntity(made), retained.SourceEndpoint, "actual recovery source endpoint rebinds");
            c.Equal(before.CreatedTick, retained.CreatedTick, "original starvation age is retained");
            c.Equal(before.LastDispatchTick, retained.LastDispatchTick, "replacement is not a dispatch or a new wait");
            c.Equal(before.Priority, retained.Priority, "player policy priority remains authored");
            c.Equal(before.SourcePort, retained.SourcePort, "named/default binding remains original");
            c.Equal(old, before.RecoveryOrigin, "prior detached recovery facts do not change");
            c.Equal(Fix.FromInt(6), Total(game), "replacement never clones or loses cargo/source stock");
            c.Equal(game.Hash(), twin.Hash(), "saved pending upgrade continues identically");
            game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            // Move the completed upgrade crew away through the ordinary home command before the physical haul.
            Send(c, game, new SetHomeCommand { Worker = Find(game, "crew"), Home = Find(game, "parking") }, twin);
            Send(c, game, new SetWorkerHeldCommand { Worker = returning ? made : Find(game, "machine"), Held = false }, twin);
            bool drained = false; int steps = 0;
            for (; steps < 180 && !drained; steps++)
            {
                drained = Amount(game, made).IsZero && Amount(game, Find(game, "destination")) == Fix.FromInt(6) &&
                    Offers(game).All(o => o.Kind != (returning ? LogisticsDispatchKind.CargoReturn : LogisticsDispatchKind.Evacuation));
                if (!drained) { Step(game, twin); c.Equal(Fix.FromInt(6), Total(game), "physical recovery conserves actual stock at every update"); }
            }
            if (!drained)
            {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException("physical recovery timeout; clock=" + game.Tick + "; origin=" + made +
                    "; entities=" + string.Join(",", game.Seat(0).Snapshot.Entities.Select(e => e.Def + ":" + Amount(game, e.Id))) +
                    "; jobs=" + string.Join(",", jobs.Select(j => j.State + "/" + j.Blocker + "/" + j.ToilIndex)));
            }
            c.That(steps > 1, "recovery earns physical travel/loading/delivery time");
            c.Equal(Fix.FromInt(6), Amount(game, Find(game, "destination")), "all real material reaches the selected destination");
            c.Equal(game.Hash(), twin.Hash(), "saved rebound recovery dispatch continues identically");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed recovery/reference state loads coherently");
            c.Equal(0, Offers(game, 1).Count, "other company sees no private recovery intent");
            c.Equal(isolatedHash, isolated.Hash(), "interleaved independent session is unchanged");
        }
        private static void Run(Check c)
        {
            foreach (bool direct in new[] { false, true }) foreach (bool named in new[] { false, true }) Physical(c, direct, named, false);
            Physical(c, false, false, true);
            foreach (bool direct in new[] { false, true }) foreach (string mode in new[] { "private", "evacuation" })
            {
                var factory = Factory(direct, true, false, mode); var game = New(factory);
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
                c.That(Offer(game, false).Requested == Fix.FromInt(6), "refusal starts with actual pending evacuation");
                var command = new PostOrderCommand { Plan = "upgrade", Target = Find(game, "source") }; ulong hash = game.Hash();
                c.Equal(LogisticsService.ReplacementRecoverySource, game.Seat(0).Preview(command), "incompatible recovery disposition refuses before work");
                c.Equal(hash, game.Hash(), "refused admission preserves actual intent and stock");
                int sequence = game.Seat(0).Submit(command); Step(game); long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
                c.That(answers.Any(a => a.Sequence == sequence && a.Refusal == LogisticsService.ReplacementRecoverySource), "preview/inbox refusal parity");
                c.That(game.Fault == null && game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source"), "refusal is not a commit fault");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused evacuation remains saveable");
            }
            foreach (bool direct in new[] { false, true })
            {
                var factory = Factory(direct, true, false, "port"); var game = New(factory);
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
                c.Equal(Code.None, Offer(game, false).SourcePort, "unused named alias is not an acquired recovery binding");
                c.Equal(Code.None, game.Seat(0).Preview(new PostOrderCommand { Plan = "upgrade", Target = Find(game, "source") }),
                    "removing an unused alias preserves the actual canonical recovery source");
            }
            {
                var factory = Factory(false, false, true, "transporter"); var game = New(factory);
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
                var command = new PostOrderCommand { Plan = "upgrade", Target = Find(game, "machine") };
                c.Equal(LogisticsService.ReplacementRecoverySource, game.Seat(0).Preview(command), "cargo return requires retained transporter ownership");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused cargo return remains coherent");
            }
        }
        private static void Independent(Check c)
        {
            foreach (string mode in new[] { "fixed", "retained", "removed", "missing", "local", "busy", "late" }) Independent(c, mode);
        }
        private static void Independent(Check c, string mode)
        {
            var factory = Factory(false, true, false, independent: mode); var game = New(factory); SimGame twin = null;
            var isolated = New(factory); ulong isolatedHash = isolated.Hash(); var stock = new Code("stock");
            var old = Find(game, "source"); var worker = Find(game, "machine");
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("recovery supply missing");
            Fix Pool(SimGame session, long identity) => session.Seat(0).View.Feature<EndpointPoolDiagnostics>().Stock(identity).AmountOf(resource);
            void Observe()
            {
                c.Equal(Fix.FromInt(6), Total(game) + Pool(game, 1) + Pool(game, 2), "every recovery/replacement update conserves original six across distinct actual owners " + mode);
                if (twin != null) c.Equal(game.Hash(), twin.Hash(), "saved recovery/replacement sessions agree at each physical update " + mode);
            }
            void Reach(Func<bool> ready, int maximum, string stage)
            {
                for (int i = 0; i < maximum && !ready(); i++) { Step(game, twin); Observe(); }
                if (!ready())
                {
                    var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                    throw new InvalidOperationException(stage + "/" + mode + " exceeded " + maximum + " updates at " + game.Tick + ": " + string.Join(";", jobs.Select(job => job.Id + "/" + job.State + "/" + job.ToilIndex + "/" + job.Loaded + "/" + job.Blocker)));
                }
            }
            Send(c, game, new HaulResourcesCommand { Source = Find(game, "donor"), Destination = old, DestinationPort = stock,
                Resource = resource, Ceiling = Fix.FromInt(6), Worker = worker, Priority = 100 });
            Reach(() => Pool(game, 1) == Fix.FromInt(6), 180, "physically populate independent public store");
            Send(c, game, new SetWorkerHeldCommand { Worker = worker, Held = true });
            // Normal player policy/home sequence clears the actual occupied work site without a collision exemption.
            Send(c, game, new ConfigureWorkPolicyCommand { Worker = worker, Settings = new WorkPolicySettings(true, 0, Array.Empty<WorkerRoleId>(), null, true, true, WorkAssignmentMode.All) });
            Send(c, game, new SetHomeCommand { Worker = worker, Home = Find(game, "donor") });
            Send(c, game, new SetWorkerHeldCommand { Worker = worker, Held = false });
            Reach(() => game.Seat(0).View.Feature<WorkView>().ReadHome(worker, out var home).IsNone && home.State == WorkerHomeState.AtHome, 180, "carrier physically clears upgrade approach");
            Send(c, game, new SetWorkerHeldCommand { Worker = worker, Held = true });
            Send(c, game, new ConfigureWorkPolicyCommand { Worker = worker, Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            for (int i = 0; i < 5; i++) { Step(game); Observe(); }
            var before = Offer(game, false);
            c.That(before.RecoveryOrigin == old && before.SourcePort == stock && before.SourceEndpoint == new ResourceEndpointId("proof.pools", 1) && before.Requested == Fix.FromInt(6),
                "actual named evacuation refers to physically delivered independent stock " + mode);
            if (mode == "busy")
            {
                Send(c, game, new SetWorkerHeldCommand { Worker = worker, Held = false });
                var acquired = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(acquired);
                c.That(acquired.Any(job => job.IsHaul && job.Source == old && job.SourceEndpoint == before.SourceEndpoint && job.Worker == worker && job.State == JobState.Assigned),
                    "ordinary independent recovery really acquires the original access site before the busy refusal");
            }
            var upgrade = new PostOrderCommand { Target = old, Plan = "upgrade" }; ulong prior = game.Hash();
            var expected = mode == "removed" ? ResourcePortCodes.OriginalUnavailable : mode == "missing" ? Codes.NotFound : mode == "local" ? LogisticsService.ReplacementRecoverySource :
                mode == "busy" ? LogisticsService.ReplacementAssigned : Code.None;
            Code admission = game.Seat(0).Preview(upgrade);
            c.Equal(expected, admission, "same independent evacuation backing admits and incompatible backing refuses " + mode);
            c.Equal(prior, game.Hash(), "recovery replacement preview changes no state or age " + mode);
            if (!expected.IsNone)
            {
                int sequence = game.Seat(0).Submit(upgrade); Step(game); long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.That(results.Any(result => result.Sequence == sequence && result.Refusal == expected), "independent evacuation preview/inbox refusal agrees " + mode);
                var preserved = Offer(game, false);
                c.That(preserved.SourceEndpoint == before.SourceEndpoint && preserved.RecoveryOrigin == old && preserved.CreatedTick == before.CreatedTick && Pool(game, 1) == Fix.FromInt(6),
                    "refusal preserves actual independent stock, source and starvation age " + mode);
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused independent recovery remains loadable " + mode);
                return;
            }
            if (!admission.IsNone) return; // Retain real baseline refusal, never fabricate successful replacement.
            if (mode == "late")
            {
                // Explicit negative protocol lens: after private validation, actual spawn changes a valid selected reference.
                var world = game.World; var original = world.Entities.Get(old); var next = world.Entities.NextIdentity;
                Entity candidate = null, rejected = null; Exception caught = null;
                ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) => {
                    if (phase != "spawn" || entity?.Def.Id != "source.next") return;
                    candidate = entity; entity.Get<IResourcePortSelection>(stock).Select(world, new ResourceEndpointId("proof.pools", 2));
                };
                try { Upgrades.TryReplace(world, original, world.Content.Get<EntityDef>("source.next"), out rejected); }
                catch (Exception error) { caught = error; }
                c.That(caught != null && game.Fault?.Phase == "entity.replace" && game.Fault.Message.Contains("replacement recovery binding violated admission", StringComparison.Ordinal),
                    "changed current access cannot masquerade as usable original evacuation backing");
                c.That(rejected == null && original.Alive && candidate?.Alive == true && ReferenceEquals(world.Entities.Get(next), candidate) && world.Entities.NextIdentity.Value == next.Value + 1,
                    "late access violation retains actual interrupted registration rather than fake rollback");
                c.That(!world.Events.HasPending && world.Services.Get<EndpointPools>().Get(1).Amount(resource) == Fix.FromInt(6) && world.Services.Get<EndpointPools>().Get(2).Amount(resource).IsZero,
                    "late binding failure neither copies stock nor publishes replacement success");
                c.Throws<InvalidOperationException>(() => game.Save(), "late recovery binding failure cannot save healthy");
                c.Equal(isolatedHash, isolated.Hash(), "late recovery fault leaves another session untouched");
                return;
            }
            Send(c, game, upgrade); twin = factory.Load(game.Save());
            Reach(() => game.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "source.next"), 30, "physical independent recovery source upgrade");
            var made = Find(game, "source.next"); var rebound = Offer(game, false);
            c.That(rebound.RecoveryOrigin == made && rebound.SourcePort == stock && rebound.SourceEndpoint == before.SourceEndpoint &&
                rebound.CreatedTick == before.CreatedTick && rebound.LastDispatchTick == before.LastDispatchTick && rebound.Priority == before.Priority && rebound.Requested == before.Requested,
                "only origin changes; independent backing, policy and chronology survive replacement " + mode);
            c.That(before.RecoveryOrigin == old && Pool(game, 1) == Fix.FromInt(6) && Pool(game, 2).IsZero, "detached old offer and actual pool do not reset or redirect " + mode);
            game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            Send(c, game, new SetHomeCommand { Worker = Find(game, "crew"), Home = Find(game, "parking") }, twin);
            Reach(() => game.Seat(0).View.Feature<WorkView>().ReadHome(Find(game, "crew"), out var home).IsNone && home.State == WorkerHomeState.AtHome, 180, "upgrade crew physically clears the corridor");
            Send(c, game, new SetWorkerHeldCommand { Worker = worker, Held = false }, twin);
            bool Loaded()
            {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                var at = (PointSnapshot)game.Seat(0).Snapshot.Entities.Single(entity => entity.Id == worker).Spatial;
                return jobs.Any(job => job.IsHaul && job.Source == made && job.ToilIndex == 2 && job.Loaded == Fix.FromInt(6)) && at.Position > Fix.FromInt(21) && at.Position < Fix.FromInt(39);
            }
            Reach(Loaded, 180, "rebound evacuation actually loads and travels");
            game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            c.That(Loaded() && Pool(game, 1).IsZero && Amount(game, worker) == Fix.FromInt(6), "loaded checkpoint preserves the actual emptied source and moving six-unit cargo " + mode);
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            using (var recorder = game.RecordReplay(limits))
            {
                Reach(() => Amount(game, Find(game, "destination")) == Fix.FromInt(6) && Offers(game).All(offer => offer.Kind != LogisticsDispatchKind.Evacuation), 180, "same loaded evacuation physically delivers");
                c.Equal(0L, recorder.CommandCount, "saved loaded recovery needs no replacement intent or haul command " + mode);
                var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
                c.That(verified.Success && verified.ActualHash == game.Hash(), "actual saved loaded independent evacuation replays identically " + mode);
            }
            c.Equal(0, Offers(game, 1).Count, "independent evacuation stays company-private " + mode);
            c.Equal(isolatedHash, isolated.Hash(), "independent recovery replacement does not affect another session " + mode);
        }
    }
}
