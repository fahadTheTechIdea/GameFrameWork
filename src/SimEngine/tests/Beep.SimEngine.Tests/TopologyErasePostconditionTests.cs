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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Physical acceptance uses players; the separate commit literal instruments actual protocol owners.
    internal static class TopologyErasePostconditionTests
    {
        internal const string Case = "Topology erase postconditions: physical graph stock commit parity and faulty erase cutoff";
        internal const string CommitCase = "Topology commit: clock random and retired resources guarded across physical erase";
        internal const string EndpointCase = "Topology endpoint integrity: retained claims survive erase and callback mutations fault";
        internal static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(CommitCase, CommitObservation); TestRunner.Add(EndpointCase, EndpointIntegrity); }
        internal static GameFactory Factory(bool point, bool half, string fault = null)
        {
            if (!point) return PhysicalLineRemovalTests.Create(false, editDefinitions: root => {
                    if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                }, editNetwork: root => {
                    var band = root.Get("entities")[0].Get("components").Get("signal_network_gate").Get("conditions")[0];
                    band.Set("minimum", JsonValue.Number("26")); band.Set("maximum", JsonValue.Number("30"));
                });
            return SpatialNetworkGraphTests.Points(half, new PointLineGeometryProofModule(),
                extraAuthor: root => { if (fault != null) root.Get("point_spaces")[0].Get("network_graph").Set("erase_fault", JsonValue.Of(fault)); },
                editCorridors: root => { var line = root.Get("lines")[0]; line.Set("owned", JsonValue.Of(true)); line.Set("owner_layer", JsonValue.Of("corridor.company")); },
                moreDocuments: new Dictionary<string,string> { { "point-owned-network.json", FrameworkProofContent.Read("point-owned-network.json") } },
                editNetwork: root => { root.Get("networks")[0].Set("public", JsonValue.Of(false)); root.Get("networks")[0].Set("capacity_policy", JsonValue.Of("fixture.count.8")); },
                afterPorts: root => {
                    root.Get("entities")[1].Get("components").Get("ports").Get("list")[0].Set("pull", JsonValue.NewArray());
                    root.Get("entities")[0].Get("components").Set("network_resource_port",
                        Json.Parse("{\"id\":\"graph\",\"network\":\"point.network\",\"minimum_members\":1}"));
                });
        }
        private static SimGame New(GameFactory f, bool point) => point ? ConstructionPlanTests.NewPoints(f) : PhysicalLineRemovalTests.New(f);
        private static InfrastructureView View(SimGame g) => g.Seat(0).View.Feature<InfrastructureView>();
        internal static List<NetworkPoolView> Pools(SimGame g, bool point)
        { var rows = new List<NetworkPoolView>(); Code code = View(g).CopyPools(point ? "point.network" : "binding.main", rows); if (!code.IsNone) throw new InvalidOperationException("actual topology pools " + code); return rows; }
        private static ResourceId Resource(SimGame g, bool point)
        { if (!g.Seat(0).View.TryFindResource(point ? "supply" : "steel", out var r)) throw new InvalidOperationException("authored topology resource missing"); return r; }
        private static Fix Total(SimGame g, bool point)
        {
            string name = point ? "supply" : "steel"; var r = Resource(g, point); Fix total = Fix.Zero;
            foreach (var entity in g.Seat(0).Snapshot.Entities) {
                var facts = new List<Facet>(); g.Seat(0).View.Inspect(entity.Id, facts);
                foreach (var fact in facts) if (fact.Key == Storage.FacetStock && fact.Subject == name) total += fact.Value;
            }
            return total + Pools(g, point).SelectMany(x => x.Stock).Where(x => x.Resource == r).Aggregate(Fix.Zero, (sum, x) => sum + x.Amount);
        }
        private static Fix PoolStock(SimGame g, bool point) => Pools(g, point).SelectMany(x => x.Stock).Where(x => x.Resource == Resource(g, point)).Aggregate(Fix.Zero, (sum, x) => sum + x.Amount);
        private static Fix Middle(SimGame g, bool point) => point ? SpatialCorridorExecutionTests.Segment(g, true, 1) : Fix.FromInt(PhysicalLineRemovalTests.Tile(g, 11));
        private static List<ActivityNodeView> Nodes(SimGame g, int order)
        { var rows = new List<ActivityNodeView>(); if (!g.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, rows)) throw new InvalidOperationException("actual cut order missing"); return rows; }
        private static void Until(SimGame g, SimGame twin, Func<bool> ready, string stage, bool point)
        {
            var elapsed = Stopwatch.StartNew(); int updates = 0;
            for (; updates < 512 && elapsed.ElapsedMilliseconds < 20000 && !ready(); updates++) Step(g, twin);
            if (!ready()) {
                var plans = new List<PlanView>(); g.Seat(0).View.Feature<WorkView>().CopyPlans(plans);
                throw new InvalidOperationException(stage + " exceeded512Updates/20000ms updates=" + updates + "/elapsed=" + elapsed.ElapsedMilliseconds + "/point=" + point + "/clock=" + g.Clock.StepIndex + "/middle=" + Middle(g, point) +
                    "/pools=" + Pools(g, point).Count + "/plans=" + string.Join(";", plans.Select(x => x.Id + "/" + x.State + "/" + x.Blocker + "/" + x.Order)) +
                    "/jobs=" + string.Join(";", PermanentRemovalTests.Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
            }
        }
        internal static void Ready(Check c, GameFactory f, SimGame g, bool point, bool checkpoint = true)
        {
            if (point) {
                long build = PermanentRemovalTests.Submit(c, g, new SubmitPlanCommand { Intent = SpatialCorridorExecutionTests.Intent(true, false) }).Arg0;
                var twin = checkpoint ? f.Load(g.Save()) : null; Until(g, twin, () => PermanentRemovalTests.Plan(g, build).State == PlanState.Done, "real corridor construction", true);
                Until(g, twin, () => PoolStock(g, true) == Fix.FromInt(14), "real finite stock enters owned graph", true);
            } else for (int i = 0; i < 4; i++) Step(g);
            c.Equal(1, Pools(g, point).Count, "actual three-vertex graph initially connected");
            c.That(PoolStock(g, point).IsPositive, "actual network stock participates in commit");
        }
        private static PostOrderCommand Cut(SimGame g, bool point) => new PostOrderCommand {
            Plan = point ? "corridor.remove" : "removal.work", Target = Entity(g, point ? "corridor.removal-site" : "removal.site"), Priority = 100 };
        internal static SimGame Healthy(Check c, bool point, bool half, GameFactory supplied = null)
        {
            var f = supplied ?? Factory(point, half); var g = New(f, point); var idle = New(f, point); ulong isolated = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            Ready(c, f, g, point); var oldPool = Pools(g, point).Single(); Fix total = Total(g, point), stock = PoolStock(g, point);
            int order = (int)PermanentRemovalTests.Submit(c, g, Cut(g, point)).Arg0;
            Until(g, null, () => Nodes(g, order).Single().Progress.WholeTicks > 0, "actual earned cut progress", point);
            c.That(Middle(g, point).IsPositive, "unfinished physical cut cannot publish deleted geometry");
            var twin = f.Load(g.Save()); long time = g.Clock.StepIndex; g.Clock.Pause(); twin.Clock.Pause(); Step(g, twin);
            c.Equal(time, g.Clock.StepIndex, "sole clock pause stops physical topology work"); g.Clock.Resume(); twin.Clock.Resume();
            Until(g, twin, () => Middle(g, point).IsZero, "actual erase and split commit", point);
            c.Equal(2, Pools(g, point).Count, "actual physical split and admitted graph agree"); c.Equal(total, Total(g, point), "split conserves all finite material");
            c.Equal(stock, PoolStock(g, point), "split preserves exact original pooled stock"); c.Equal(3, oldPool.MemberCount, "old public pool facts remain detached");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "split physical geometry and pool state durable");
            var foreignOrders = new List<OrderView>(); g.Seat(1).View.Feature<WorkView>().CopyOrders(foreignOrders); c.Equal(0, foreignOrders.Count, "private physical cut work withheld");
            var args = point ? Json.Parse("{\"space\":\"field.points\",\"path\":[{\"space\":\"field.points\",\"position\":5}]}") :
                Json.Parse("{\"space\":\"recovery.tiles\",\"path\":[{\"space\":\"recovery.tiles\",\"x\":11,\"y\":28}]}");
            long plan = PermanentRemovalTests.Submit(c, g, new SubmitPlanCommand { Intent = new PlanIntent(point ? "line:corridor.point-line" : "line:network.pipe", args, "reconnect", 60, false) }).Arg0;
            if (!point) {
                var marker = g.Seat(0).Snapshot.Entities.Single(x => x.Def == "sim.line-work.network.pipe").Id;
                var haul = new HaulResourcesCommand { Source = Entity(g, "depot"), Destination = marker, Worker = Entity(g, "truck"),
                    Resource = Resource(g, false), Ceiling = Fix.One, Priority = 100 };
                Until(g, null, () => PermanentRemovalTests.Plan(g, plan).State == PlanState.Done || g.Seat(0).Preview(haul).IsNone,
                    "actual carrier ready for reconnect supply", point);
                if (PermanentRemovalTests.Plan(g, plan).State != PlanState.Done) PermanentRemovalTests.Submit(c, g, haul);
            }
            else {
                var marker = Entity(g, "corridor.cursor");
                var haul = new HaulResourcesCommand { Source = Entity(g, "source"), SourcePort = new Code("graph"),
                    Destination = marker, Worker = Entity(g, "machine"), Resource = Resource(g, true), Ceiling = Fix.FromInt(2), Priority = 100 };
                Until(g, null, () => PermanentRemovalTests.Plan(g, plan).State == PlanState.Done || g.Seat(0).Preview(haul).IsNone,
                    "actual carrier ready for reconnect supply", point);
                if (PermanentRemovalTests.Plan(g, plan).State != PlanState.Done) PermanentRemovalTests.Submit(c, g, haul);
            }
            twin = f.Load(g.Save()); Until(g, twin, () => PermanentRemovalTests.Plan(g, plan).State == PlanState.Done, "real reconnect", point);
            c.Equal(1, Pools(g, point).Count, "ordinary construction merges physical and logical topology");
            c.Equal(total - Fix.FromInt(point ? 2 : 1), Total(g, point), "merge only spends authored construction material");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "merged topology durable"); c.Equal(isolated, idle.Hash(), "healthy topology sessions isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "physical construction erase split and merge replay");
            return g;
        }
        private static void Negative(Check c, string mode, bool half)
        {
            var f = Factory(true, half, mode); var g = New(f, true); var idle = New(f, true); ulong isolated = idle.Hash();
            Ready(c, f, g, true); var pool = Pools(g, true).Single(); Fix stock = PoolStock(g, true); var command = Cut(g, true);
            ulong pure = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(command), "faulty erase callback does not affect pure work admission"); c.Equal(pure, g.Hash(), "admission pure");
            g.Seat(0).Submit(command); GameSnapshot before = null, foreign = null;
            var elapsed = Stopwatch.StartNew();
            for (int i = 0; i < 512 && elapsed.ElapsedMilliseconds < 20000 && g.Fault == null; i++) { before = g.Seat(0).Snapshot; foreign = g.Seat(1).Snapshot; g.Update(); }
            var fault = g.Fault; c.That(fault != null, "faulty erase cannot publish healthy graph " + mode);
            if (fault == null) throw new InvalidOperationException("faulty erase exceeds512Updates/20000ms elapsed=" + elapsed.ElapsedMilliseconds + "/clock=" + g.Clock.StepIndex + "/" + mode);
            c.That(fault.Message.Contains(mode == "throw" ? "proof.original-physical-erase-error" : "admitted physical topology"), "original erase failure or physical mismatch diagnostic retained");
            var after = g.Seat(0).Snapshot; c.Equal(GameState.Faulted, after.State, "faulted topology publication explicit"); c.Equal(before.Tick, after.Tick, "last healthy topology time retained");
            c.Equal(0, after.Changed.Count + after.Spawned.Count + after.Removed.Count, "no invalid erase completion delta prefix");
            c.That(ReferenceEquals(before.Space, after.Space) && ReferenceEquals(foreign.Space, g.Seat(1).Snapshot.Space), "per-viewer healthy geometry retained");
            foreach (var prior in before.Entities) c.That(ReferenceEquals(prior.Spatial, after.Entities.Single(x => x.Id == prior.Id).Spatial), "healthy entity payload retained");
            c.Equal(stock, pool.Stock.Where(x => x.Resource == Resource(idle, true)).Aggregate(Fix.Zero, (sum, x) => sum + x.Amount), "retained original pool facts detached despite interrupted commit");
            long stopped = g.Clock.StepIndex; g.Update(); c.Equal(stopped, g.Clock.StepIndex, "faulted erase cannot advance clock");
            c.Throws<InvalidOperationException>(() => Pools(g, true), "live invalid graph projection refused");
            c.Throws<InvalidOperationException>(() => g.Hash(), "interrupted topology cannot hash healthy"); c.Throws<InvalidOperationException>(() => g.Save(), "interrupted topology cannot save healthy");
            c.That(ReferenceEquals(fault, g.Fault), "first erase fault retained"); c.Equal(isolated, idle.Hash(), "faulty erase session isolated");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current topology postcondition identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no duplicate topology codec/state");
            foreach (bool point in new[] { false, true }) foreach (bool half in new[] { false, true }) Healthy(c, point, half);
            foreach (string mode in new[] { "noop", "restore", "extra", "throw" }) foreach (bool half in new[] { false, true }) Negative(c, mode, half);
        }
        private static void CommitObservation(Check c)
        {
            foreach (bool half in new[] { false, true })
            foreach (string mode in new[] { "pause", "speed", "random", "random-restore", "clock-throw", "retired-stock", "retired-claim" })
            {
                string context = mode + "/half=" + half;
                var f = Factory(true, half); var g = New(f, true); Ready(c, f, g, true, checkpoint: false);
                var independent = f.Load(g.Save()); ulong isolated = independent.Hash();
                var world = g.World; var pool = Pools(g, true).Single(); var resource = Resource(g, true);
                var retired = (IResourceOutlet)world.Services.Providers<NetworkService>().Single().Resolve(pool.Endpoint);
                var line = world.Content.Get<ILineDefinition>("corridor.point-line");
                var path = new SpatialPlacement[] { new PointPlacement("field.points", Fix.FromInt(5)) };
                ulong pure = g.Hash(); c.Equal(Code.None, View(g).PreviewRemoval(line.Id, path), "ordinary removal preview ready: " + context);
                c.Equal(pure, g.Hash(), "preview leaves original owners unchanged: " + context);
                var published = g.Seat(0).Snapshot; int calls = 0; bool erased = false;
                var original = new InvalidOperationException("proof.original-erase-clock-error");
                ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) => {
                    if (phase != "erase") return; calls++;
                    var geometry = (INetworkGraphGeometry)world.Space; var vertices = new List<NetworkVertex>();
                    geometry.CopyVertices(world, line, vertices);
                    string removed = geometry.VertexIdentity(world, line, path[0]);
                    erased = !vertices.Any(x => x.Identity == removed);
                    if (mode == "pause" || mode == "clock-throw") g.Clock.Pause();
                    else if (mode == "speed") g.Clock.Speed = 2;
                    else if (mode == "random" || mode == "random-restore") {
                        ulong state = world.Random.State; world.Random.NextULong();
                        if (mode == "random-restore") world.Random.State = state;
                    }
                    else if (mode == "retired-stock") c.Equal(Fix.One, retired.Receive(world.Content, resource, Fix.One), "intrusion reaches actual drained owner: " + context);
                    else if (mode == "retired-claim") retired.ReserveIn(new ReservationOwner("proof.erase-intrusion", 1), resource, Fix.One);
                    if (mode == "clock-throw") throw original;
                };
                Exception caught = null;
                try { world.Services.Get<INetworkTopology>().Remove(g.Seat(0).Id, line, path); }
                catch (Exception error) { caught = error; }
                c.Equal(1, calls, "one actual physical erase callback: " + context);
                c.That(caught is InvalidOperationException, "interrupted commit cannot return healthy: " + context);
                c.That(world.Fault != null && world.Fault.Phase == "network.topology.commit", "original topology fault owner: " + context);
                if (mode == "clock-throw") c.That(ReferenceEquals(original, caught), "exact original throwing callback survives: " + context);
                else c.That(caught != null && caught.Message.Contains(mode.StartsWith("retired-") ? "retired network endpoint" : "game-clock"), "scoped invariant diagnostic: " + context);
                c.That(erased, "intended physical erase ran before intrusion: " + context);
                c.Equal(3, pool.MemberCount, "retained pool facts detached: " + context);
                c.Equal(published.Tick, g.Seat(0).Snapshot.Tick, "last healthy publication retained: " + context);
                c.Throws<InvalidOperationException>(() => Pools(g, true), "invalid live pools unavailable: " + context);
                c.Throws<InvalidOperationException>(() => g.Save(), "interrupted commit cannot save: " + context);
                c.Throws<InvalidOperationException>(() => g.Hash(), "interrupted commit cannot hash: " + context);
                c.Equal(isolated, independent.Hash(), "saved session unaffected: " + context);
                long time = independent.Clock.StepIndex; Step(independent);
                c.That(independent.Clock.StepIndex > time, "independent saved session still advances: " + context);
            }
        }
        private static void EndpointIntegrity(Check c)
        {
            foreach (bool half in new[] { false, true })
            {
                var f = Factory(true, half); var template = New(f, true); Ready(c, f, template, true, checkpoint: false);
                var originalPool = Pools(template, true).Single();
                long build = PermanentRemovalTests.Submit(c, template, new SubmitPlanCommand { Intent = SpatialCorridorExecutionTests.Intent(true, false, 8) }).Arg0;
                var supply = new HaulResourcesCommand { Source = Entity(template, "source"), SourcePort = new Code("graph"),
                    Destination = Entity(template, "corridor.cursor"), Worker = Entity(template, "machine"),
                    Resource = Resource(template, true), Ceiling = Fix.FromInt(6), Priority = 100 };
                Until(template, null, () => template.Seat(0).Preview(supply).IsNone, "carrier ready to supply disconnected corridor", true);
                PermanentRemovalTests.Submit(c, template, supply);
                Until(template, null, () => PermanentRemovalTests.Plan(template, build).State == PlanState.Done, "second disconnected physical corridor", true);
                c.Equal(2, Pools(template, true).Count, "ordinary physical build creates two disconnected pools");
                var original = Pools(template, true).Single(x => x.Endpoint == originalPool.Endpoint);
                var retained = Pools(template, true).Single(x => x.Endpoint != original.Endpoint);
                var resource = Resource(template, true);
                var move = new ResourceMovementIntent(new ReservationOwner("proof.retained-supply", 1), template.Seat(0).Id,
                    original.Endpoint, retained.Endpoint, new Bill(new[] { new ResourceAmount(resource, Fix.FromInt(2)) }),
                    Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
                c.That(template.World.Services.Get<IResourceTransaction>().CommitMovement(move).Committed, "actual transaction supplies retained endpoint without new stock");
                Step(template); byte[] saved = template.Save();
                foreach (string mode in new[] { "healthy", "stock", "stock-restore", "add-in", "add-out", "release-in", "release-out", "replace-in", "claim-cycle", "vertices-release-in", "version-release-out" })
                {
                    string context = mode + "/half=" + half; var g = f.Load(saved); var world = g.World;
                    var service = world.Services.Providers<NetworkService>().Single();
                    var endpoint = (IResourceOutlet)service.Resolve(retained.Endpoint);
                    var owner = new ReservationOwner("proof.retained-existing", 1); var intrusion = new ReservationOwner("proof.retained-intrusion", 1);
                    ReservationToken incoming = endpoint.ReserveIn(owner, resource, Fix.One), outgoing = endpoint.ReserveOut(owner, resource, Fix.One);
                    // Checkpoint actual live receipt identities, not a copied balance or shadow claim book.
                    g = f.Load(g.Save()); world = g.World; service = world.Services.Providers<NetworkService>().Single();
                    endpoint = (IResourceOutlet)service.Resolve(retained.Endpoint);
                    c.That(endpoint.CheckReservedIn(world.Content, owner, incoming).Ready && endpoint.CheckReservedOut(owner, outgoing).Ready,
                        "original inbound/outbound tokens survive initial save: " + context);
                    int version = endpoint.Version; Fix stock = endpoint.Amount(resource); var snapshot = g.Seat(0).Snapshot;
                    var independent = f.Load(g.Save()); ulong isolated = independent.Hash();
                    var line = world.Content.Get<ILineDefinition>("corridor.point-line");
                    var path = new SpatialPlacement[] { new PointPlacement("field.points", Fix.FromInt(5)) };
                    ulong pure = g.Hash(); c.Equal(Code.None, View(g).PreviewRemoval(line.Id, path), "unrelated claims do not block removal: " + context);
                    c.Equal(pure, g.Hash(), "retained-claim preview pure: " + context); int calls = 0, lateCalls = 0;
                    ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) => {
                        if (phase != "erase") {
                            if (calls != 0 && lateCalls == 0 && (mode == "vertices-release-in" && phase == "network.vertices" || mode == "version-release-out" && phase == "network.version")) {
                                lateCalls++;
                                if (mode == "vertices-release-in") endpoint.ReleaseIn(owner, incoming); else endpoint.ReleaseOut(owner, outgoing);
                            }
                            return;
                        }
                        calls++;
                        if (mode == "stock" || mode == "stock-restore") {
                            c.Equal(Fix.One, endpoint.Take(resource, Fix.One), "intrusion uses actual unpromised stock: " + context);
                            if (mode == "stock-restore") c.Equal(Fix.One, endpoint.Receive(world.Content, resource, Fix.One), "intrusion restores quantity but not history: " + context);
                        }
                        else if (mode == "add-in") endpoint.ReserveIn(intrusion, resource, Fix.One);
                        else if (mode == "add-out") endpoint.ReserveOut(intrusion, resource, Fix.One);
                        else if (mode == "release-in" || mode == "replace-in") {
                            endpoint.ReleaseIn(owner, incoming);
                            if (mode == "replace-in") endpoint.ReserveIn(intrusion, resource, Fix.One);
                        }
                        else if (mode == "release-out") endpoint.ReleaseOut(owner, outgoing);
                        else if (mode == "claim-cycle") {
                            var token = endpoint.ReserveIn(intrusion, resource, Fix.One); endpoint.ReleaseIn(intrusion, token);
                        }
                    };
                    Exception caught = null; Code answer = Code.None;
                    try { answer = world.Services.Get<INetworkTopology>().Remove(g.Seat(0).Id, line, path); }
                    catch (Exception error) { caught = error; }
                    c.Equal(1, calls, "one real erase callback: " + context);
                    if (mode == "vertices-release-in" || mode == "version-release-out") c.Equal(1, lateCalls, "actual post-erase graph validation callback exercised: " + context);
                    if (mode == "healthy") {
                        c.That(caught == null && answer.IsNone && world.Fault == null, "unrelated live claims permit healthy commit");
                        c.Equal(version, endpoint.Version, "unchanged pool version retained"); c.Equal(stock, endpoint.Amount(resource), "unchanged pool stock retained");
                        c.That(ReferenceEquals(endpoint, service.Resolve(retained.Endpoint)), "unchanged endpoint owner identity retained");
                        c.Equal(3, Pools(g, true).Count, "split plus disconnected retained component published");
                        c.That(endpoint.CheckReservedIn(world.Content, owner, incoming).Ready && endpoint.CheckReservedOut(owner, outgoing).Ready,
                            "exact token authority survives unrelated physical erase");
                        Step(g); var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "healthy retained claims and topology durable");
                        Step(g, twin); c.That(g.Fault == null && twin.Fault == null, "saved retained claims continue normally");
                    }
                    else {
                        c.That(caught is InvalidOperationException, "callback cannot return healthy after endpoint mutation: " + context);
                        c.That(world.Fault != null && world.Fault.Phase == "network.topology.commit", "original commit fault owner: " + context);
                        c.That(endpoint.Version > version, "real original owner records mutation: " + context);
                        if (mode != "stock") c.Equal(stock, endpoint.Amount(resource), "final stock alone cannot prove purity: " + context);
                        c.Equal(snapshot.Tick, g.Seat(0).Snapshot.Tick, "last healthy snapshot retained: " + context);
                        c.Throws<InvalidOperationException>(() => Pools(g, true), "faulted live pools unavailable: " + context);
                        c.Throws<InvalidOperationException>(() => g.Save(), "faulted receipt mutation cannot save: " + context);
                        c.Throws<InvalidOperationException>(() => g.Hash(), "faulted receipt mutation cannot hash: " + context);
                    }
                    c.Equal(isolated, independent.Hash(), "restored independent receipt owner unaffected: " + context);
                    long time = independent.Clock.StepIndex; Step(independent); c.That(independent.Clock.StepIndex > time, "independent session advances: " + context);
                }
            }
        }
    }
}
