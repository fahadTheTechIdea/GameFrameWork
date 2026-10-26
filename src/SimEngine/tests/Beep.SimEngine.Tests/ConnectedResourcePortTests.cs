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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Real files/master and public seats/views/Update only; never direct stock, claims, topology or processor state.
    internal static partial class ConnectedResourcePortTests
    {
        internal const string Case = "Connected resource ports: real shared pools drive timed production work and physical hauling";
        internal const string ReplacementCase = "Connected replacement: topology-owned stock policies survive physical upgrade and loaded hauling";
        private static readonly Code Shared = new Code("shared"), Process = new Code("process");
        internal static void Register()
        {
            TestRunner.Add(Case, Run); TestRunner.Add(ReplacementCase, Replacement); TestRunner.Add(TopologyCase, TopologyContinuation);
            TestRunner.Add(TopologyContentionNativeCase, c => TopologyContention(c, false, false));
            TestRunner.Add(TopologyContentionIndependentCase, c => TopologyContention(c, true, true));
        }
        internal static GameFactory Factory(bool half, bool independent, bool owned, ConnectedResourcePortModule probe,
            Action<JsonValue> edit = null, string definitions = null, Action<JsonValue> editWorld = null)
        {
            var data = Json.Parse(definitions ?? FrameworkProofContent.Read("connected-resource-ports.json"));
            if (independent) {
                var parts = data.Get("entities")[0].Get("components");
                parts.Set("independent_connected_port", parts.Get("network_resource_port")); parts.Remove("network_resource_port");
            }
            edit?.Invoke(data);
            var retry = Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Set("work_retry_profiles", Json.Parse(FrameworkProofContent.Read("completion-retry.json")).Get("work_retry_profiles"));
            return SpatialNetworkGraphTests.Points(half, new PointLineGeometryProofModule(),
                extraModules: new GameModule[] { probe }, extraDeclarations: "[" + FrameworkProofContent.Read("connected-resource-ports-module.json") + "]",
                moduleDocuments: new Dictionary<string,string> { { "connected-resource-ports.json", data.ToString() } },
                moreDocuments: owned ? new Dictionary<string,string> { { "point-owned-network.json", FrameworkProofContent.Read("point-owned-network.json") } } : null,
                editCorridors: root => { if (owned) { root.Get("lines")[0].Set("owned", JsonValue.Of(true)); root.Get("lines")[0].Set("owner_layer", JsonValue.Of("corridor.company")); } },
                editNetwork: root => root.Get("networks")[0].Set("public", JsonValue.Of(!owned)), retryProfiles: retry.ToString(),
                afterPorts: root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(probe.Id));
                    root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("connected.factory-start"));
                    root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("16"));
                    root.Get("entities")[1].Get("components").Get("ports").Get("list")[0].Set("pull", Json.Parse("[]"));
                    editWorld?.Invoke(root);
                });
        }
        private static EntityId Site(SimGame g) => Entity(g, "connected.factory");
        private static ResourceId Supply(SimGame g)
        { if (!g.Seat(0).View.TryFindResource("supply", out var r)) throw new InvalidOperationException("authored supply missing"); return r; }
        private static ResourcePortSnapshot Port(SimGame g)
        {
            Code refusal = g.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Site(g), Shared, Supply(g), out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("connected port facts " + refusal); return value;
        }
        private static TransformationSnapshot ProcessFacts(SimGame g)
        { if (!g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Site(g), Process, out var value)) throw new InvalidOperationException("actual process missing"); return value; }
        private static List<NetworkPoolView> Pools(SimGame g)
        {
            var values = new List<NetworkPoolView>();
            Code refusal = g.Seat(0).View.Feature<InfrastructureView>().CopyPools("point.network", values);
            if (!refusal.IsNone) throw new InvalidOperationException("actual shared pools " + refusal); return values;
        }
        private static Fix Total(SimGame g) => SpatialCorridorExecutionTests.Total(g, true) +
            Pools(g).SelectMany(p => p.Stock).Aggregate(Fix.Zero, (s,q) => s + q.Amount) + ProcessFacts(g).Wip.Stock.AmountOf(Supply(g));
        private static bool Live(WorkJobView j) => j.State == JobState.Open || j.State == JobState.Assigned || j.State == JobState.Paused;
        private static void Reach(SimGame g, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 800 && !ready(); i++) Step(g, twin);
            if (!ready()) throw new InvalidOperationException(stage + " exceeded800Updates step=" + g.Clock.StepIndex +
                " port=" + Port(g).Blocker + " process=" + ProcessFacts(g).Phase + "/" + ProcessFacts(g).Blocker +
                " pools=" + string.Join(";", Pools(g).Select(p => p.MemberCount + "/" + string.Join(",", p.Stock.Select(q => q.Amount)))) +
                " jobs=" + string.Join(";", PermanentRemovalTests.Jobs(g).Select(j => j.State + "/" + j.Blocker)));
        }
        private static void Physical(Check c, bool half, bool independent, bool owned, Action<Check, GameFactory, SimGame, bool, bool> continuation = null,
            Action<JsonValue> edit = null, Action<JsonValue> editWorld = null)
        {
            var probe = new ConnectedResourcePortModule(); var f = Factory(half, independent, owned, probe, edit: edit, editWorld: editWorld);
            var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong isolated = idle.Hash();
            var limits = new ReplayLimits(4*1024*1024, 5000);
            using (var recorder = g.RecordReplay(limits)) {
                c.That(probe.SharedOwner, "resource access, diagnostics and topology share the original service");
                c.Equal(0, Pools(g).Count, "no fabricated network before physical construction");
                var missing = Port(g); c.That(!missing.Ready && missing.Stock == null && missing.Version == -1 && missing.Blocker == NetworkService.Disconnected,
                    "disconnected port has explicit unavailable facts, not invented empty stock");
                PermanentRemovalTests.Submit(c,g,new EnqueueTransformationCommand { Transformer=Site(g), Capability=Process, Transformation="connected.batch" });
                var waiting = ProcessFacts(g); c.That(waiting.Wip.Execution == 0 && waiting.Blocker == NetworkService.Disconnected,
                    "ordinary queued production waits before acquisition on its actual disconnected port");
                c.Equal(Fix.FromInt(16), Total(g), "waiting queue creates no resources or consumes inputs");
                long build = PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand { Intent=SpatialCorridorExecutionTests.Intent(true,false) }).Arg0;
                var twin=f.Load(g.Save());
                Reach(g,twin,()=>Pools(g).Count==1&&Pools(g)[0].MemberCount==1&&Port(g).Blocker==NetworkService.InsufficientMembers,"first physical member");
                c.That(Port(g).Stock==null && ProcessFacts(g).Wip.Execution==0,"undersized network gives no artificial members, usable stock or WIP");
                twin=f.Load(g.Save());
                Reach(g,twin,()=>PermanentRemovalTests.Plan(g,build).State==PlanState.Done,"physical three-member corridor");
                c.Equal(3,Pools(g).Single().MemberCount,"actual authored connected capacity instances");
                Reach(g,twin,()=>ProcessFacts(g).Wip.Execution>0&&ProcessFacts(g).Progress.WholeTicks>0,"actual pool-fed timed manufacturing");
                var active=ProcessFacts(g);var pool=Pools(g).Single();var tool=active.Tools.Receipts.Single();
                c.Equal(pool.Endpoint,Port(g).Endpoint,"named port uses exact original network inventory");
                c.Equal(Fix.FromInt(8),active.Wip.Stock.AmountOf(Supply(g)),"eight physical inputs admitted by three instances, not per-member four");
                c.Equal(Fix.One,tool.Amount,"real reusable tool claim retained in original book");
                c.Equal(pool.Endpoint,tool.Token.Endpoint,"processor tool receipt belongs to the original network reservation owner");
                c.Equal(Fix.FromInt(9),Total(g),"six construction inputs and one earned operating input consumed exactly once");
                ulong pure=g.Hash();c.Equal(Codes.NotOwned,g.Seat(1).View.Feature<InventoryView>().ReadResourcePort(Site(g),Shared,Supply(g),out _),"private foreign-company port refused");
                c.That(!g.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Site(g),Process,out _),"private process withheld");Port(g);c.Equal(pure,g.Hash(),"port views do not mutate capacity or topology");
                twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"active pool WIP and original tool claims restore exactly");
                long at=g.Clock.StepIndex;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);c.Equal(at,g.Clock.StepIndex,"one paused clock freezes physical production");g.Clock.Resume();twin.Clock.Resume();
                if(independent)PermanentRemovalTests.Submit(c,g,new CancelTransformationCommand { Transformer=Site(g),Capability=Process,Index=0 });
                twin=f.Load(g.Save());Reach(g,twin,()=>ProcessFacts(g).QueueCount==0,"actual same-pool completion or cancellation");
                c.Equal(Fix.FromInt(9),Port(g).Stock.AmountOf(Supply(g)),"complete result or canceled raw WIP returns to actual pool once");
                c.Equal(0,Pools(g).Single().OutboundPromises.Count,"processor releases actual reusable claims");
                int inspect=(int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand { Plan="connected.inspect",Target=Site(g),Priority=90 }).Arg0;
                twin=f.Load(g.Save());Reach(g,twin,()=>PermanentRemovalTests.Node(g,inspect,"check").Progress.WholeTicks>0,"machine travels to connected named work port");
                var held=Pools(g).Single().OutboundPromises.Single();c.Equal(Fix.FromInt(2),held.Amount,"ordinary activity holds actual pool tools");
                twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"named work-source claims durable");
                PermanentRemovalTests.Submit(c,g,new CancelOrderCommand { Order=inspect });twin=f.Load(g.Save());
                Reach(g,twin,()=>Pools(g).Single().OutboundPromises.Count==0&&!PermanentRemovalTests.Jobs(g).Any(Live),"physical work cancellation releases original claims");
                Fix destination=Stock(g,"destination");long before=g.Clock.StepIndex;
                PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand { Source=Site(g),SourcePort=Shared,Destination=Entity(g,"destination"),Worker=Entity(g,"machine"),Resource=Supply(g),Ceiling=Fix.FromInt(3),Priority=90 });
                c.Equal(destination,Stock(g,"destination"),"named network haul cannot deliver instantly");twin=f.Load(g.Save());
                Reach(g,twin,()=>Stock(g,"destination")==destination+Fix.FromInt(3)&&!PermanentRemovalTests.Jobs(g).Any(Live),"physical named network haul");
                c.That(g.Clock.StepIndex>before+1,"hauling/load/travel/unload takes actual game time");
                c.Equal(Fix.FromInt(6),Port(g).Stock.AmountOf(Supply(g)),"three hauled units actually leave original pool");
                c.Equal(Fix.FromInt(9),Total(g),"manufacturing/cancellation/work/haul conserve the original remaining stock");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"settled shared endpoint state durable");c.Equal(isolated,idle.Hash(),"interleaved game remains isolated");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"same public command stream replays physical connected-port execution");
            }
            continuation?.Invoke(c, f, g, half, independent);
        }
        private static void Invalid(Check c)
        {
            foreach(string failure in new[]{"missing-minimum","zero-minimum","negative-minimum","missing-network","unknown-network","undeclared-connection","pull-without-inlet","push-without-outlet","extra"}) {
                var probe=new ConnectedResourcePortModule();
                c.Throws<ContentException>(()=>ConstructionPlanTests.NewPoints(Factory(false,false,true,probe,data=>{
                    var unused=data.Get("entities")[0].Clone();unused.Set("id",JsonValue.Of("connected.unused"));data.Get("entities").Add(unused);
                    var parts=unused.Get("components");var port=parts.Get("network_resource_port");
                    if(failure=="missing-minimum")port.Remove("minimum_members");if(failure=="zero-minimum")port.Set("minimum_members",JsonValue.Number("0"));
                    if(failure=="negative-minimum")port.Set("minimum_members",JsonValue.Number("-1"));if(failure=="missing-network")port.Remove("network");
                    if(failure=="unknown-network")port.Set("network",JsonValue.Of("missing"));if(failure=="undeclared-connection")parts.Remove("ports");
                    if(failure=="pull-without-inlet")parts.Get("ports").Get("list")[0].Set("pull",Json.Parse("[\"supply\"]"));
                    if(failure=="push-without-outlet")parts.Get("ports").Get("list")[0].Set("push",Json.Parse("[\"supply\"]"));
                    if(failure=="extra")port.Set("pool",JsonValue.Number("1"));
                })),"loaded unused malformed connected port refuses before Initialize "+failure);
            }
        }
        private static void NativeFootprints(Check c)
        {
            foreach (bool shrink in new[] { false, true })
            {
                var factory = NetworkSignalBindingTests.Create(new InitializeProbe(), out _, independent: false, edit: data =>
                {
                    var original = data.Get("entities")[0]; original.Get("components").Get("tile_presence").Set("width", JsonValue.Number("3"));
                    original.Get("components").Set("network_resource_port", Json.Parse("""{"id":"shared","network":"binding.main","minimum_members":1}"""));
                    data.Get("entity_setups")[0].Get("placement").Set("x", JsonValue.Number("7"));
                    var next = original.Clone(); next.Set("id", JsonValue.Of("binding.source.next"));
                    if (shrink) next.Get("components").Get("tile_presence").Set("width", JsonValue.Number("1"));
                    data.Get("entities").Add(next);
                    data.Set("entity_replacement_effects", Json.Parse("""[{"id":"binding.replace","source":"binding.source","target":"binding.source.next"}]"""));
                    data.Set("activities", Json.Parse("""[{"id":"binding.upgrade","player_orderable":true,"automatic_supply":false,"on_complete":"binding.replace","nodes":[{"id":"upgrade","executor":"work.staffed","roles":["haul"],"duration":{"numerator":3,"denominator":1,"unit":"minute"},"after":[],"requirement":null,"effect":null}]}]"""));
                });
                var game = PhysicalLineRemovalTests.New(factory); var source = Entity(game, "binding.source");
                if (!game.Seat(0).View.TryFindResource("steel", out var resource)) throw new InvalidOperationException("native authored steel missing");
                c.Equal(Code.None, game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(source, Shared, resource, out var original), "actual wide native footprint reaches settled network");
                c.That(original.Ready, "native footprint test has a real connected endpoint, not a fabricated preview");
                PermanentRemovalTests.Submit(c, game, new ConfigureStockPolicyCommand { Store = source, Port = Shared, Resource = resource,
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.One, Fix.FromInt(2), Fix.FromInt(4), 47, LogisticsReserveAccess.Protected)) });
                var command = new PostOrderCommand { Target = source, Plan = "binding.upgrade" }; ulong before = game.Hash();
                var expected = shrink ? NetworkService.Disconnected : Code.None;
                c.Equal(expected, game.Seat(0).Preview(command), "native future footprint, not old extent, decides replacement contact");
                c.Equal(before, game.Hash(), "native projected contact preview is read-only");
                if (shrink)
                {
                    int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                    c.That(results.Any(result => result.Sequence == sequence && result.Refusal == NetworkService.Disconnected), "disconnected future native footprint refuses through normal inbox");
                    c.That(game.Fault == null && game.Seat(0).Snapshot.Entities.Any(entity => entity.Id == source) && !game.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "binding.source.next"),
                        "native refused projection neither retires source nor fabricates a structure");
                }
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native projected admission/refusal remains durable");
            }
        }
        private static void Replacement(Check c)
        {
            NativeFootprints(c);
            foreach (bool independent in new[] { false, true })
            foreach (bool insufficient in new[] { false, true })
            {
                var factory = Factory(false, independent, true, new ConnectedResourcePortModule(), edit: data =>
                {
                    var original = data.Get("entities")[0]; original.Get("components").Remove("transformer");
                    var next = original.Clone(); next.Set("id", JsonValue.Of("connected.factory.next"));
                    next.Get("components").Get(independent ? "independent_connected_port" : "network_resource_port").Set("minimum_members", JsonValue.Number(insufficient ? "4" : "3"));
                    data.Get("entities").Add(next);
                    data.Set("entity_replacement_effects", Json.Parse("""[{"id":"connected.replace","source":"connected.factory","target":"connected.factory.next"}]"""));
                    data.Get("activities").Add(Json.Parse("""{"id":"connected.upgrade","player_orderable":true,"automatic_supply":false,"on_complete":"connected.replace","nodes":[{"id":"upgrade","executor":"work.staffed","roles":["inspect"],"duration":{"numerator":3,"denominator":1,"unit":"minute"},"after":[],"requirement":null,"effect":null}]}"""));
                });
                var game = ConstructionPlanTests.NewPoints(factory); var idle = ConstructionPlanTests.NewPoints(factory); ulong isolated = idle.Hash();
                SimGame twin = null; bool observeMaterial = false;
                void StepBoth()
                {
                    Step(game, twin);
                    if (twin != null) c.Equal(game.Hash(), twin.Hash(), "connected replacement saved sessions agree on every physical update");
                    if (observeMaterial)
                    {
                        c.Equal(Fix.FromInt(10), Total(game), "every post-build physical update conserves remaining10 across actual buffers and pool");
                        c.That(Pools(game).All(pool => pool.OutboundPromises.Sum(line => line.Amount.Raw) <= pool.Stock.Sum(line => line.Amount.Raw)), "network outgoing claims remain funded by actual stock");
                    }
                }
                void Wait(Func<bool> ready, string stage)
                {
                    for (int i = 0; i < 800 && !ready(); i++) StepBoth();
                    if (!ready()) throw new InvalidOperationException(stage + " exceeded800Updates/clock=" + game.Clock.StepIndex);
                }
                List<LogisticsDemandView> Bands(SimGame session)
                { var rows = new List<LogisticsDemandView>(); session.Seat(0).View.Feature<LogisticsView>().CopyDemands(rows); return rows; }
                List<RouteView> Routes(SimGame session)
                { var rows = new List<RouteView>(); session.Seat(0).View.Feature<LogisticsView>().CopyRoutes(rows); return rows; }
                Fix Total(SimGame session) => SpatialCorridorExecutionTests.Total(session, true) + Pools(session).SelectMany(pool => pool.Stock).Aggregate(Fix.Zero, (sum, line) => sum + line.Amount);
                long construction = PermanentRemovalTests.Submit(c, game, new SubmitPlanCommand { Intent = SpatialCorridorExecutionTests.Intent(true, false) }).Arg0;
                twin = factory.Load(game.Save());
                Wait(() => PermanentRemovalTests.Plan(game, construction).State == PlanState.Done && Pools(game).Count == 1 &&
                    Pools(game).Single().Stock.Sum(line => line.Amount.Raw) == Fix.FromInt(10).Raw && !PermanentRemovalTests.Jobs(game).Any(Live), "actual connected corridor and earned pool fill");
                c.Equal(Fix.FromInt(10), Total(game), "six authored construction materials consumed from original16, not injected network stock");
                observeMaterial = true;
                var old = Site(game); var resource = Supply(game); var originalPool = Pools(game).Single();
                PermanentRemovalTests.Submit(c, game, new ConfigureStockPolicyCommand { Store = old, Port = Shared, Resource = resource,
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.FromInt(2), Fix.FromInt(4), Fix.FromInt(8), 47, LogisticsReserveAccess.Protected)) });
                PermanentRemovalTests.Submit(c, game, new AddRouteCommand { From = old, SourcePort = Shared, To = Entity(game, "destination"), Resource = resource,
                    Settings = new LogisticsRouteSettings(false, Fix.FromInt(3), 19) });
                var band = Bands(game).Single(); var route = Routes(game).Single();
                var command = new PostOrderCommand { Target = old, Plan = "connected.upgrade" }; ulong before = game.Hash();
                Code expected = insufficient ? NetworkService.InsufficientMembers : Code.None;
                Code admission = game.Seat(0).Preview(command);
                c.Equal(expected, admission, "connected replacement admission uses actual future membership and backing");
                c.Equal(before, game.Hash(), "connected replacement preview changes no topology, stock, policy or clock");
                if (insufficient)
                {
                    int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                    c.That(results.Any(result => result.Sequence == sequence && result.Refusal == expected), "connected typed membership refusal agrees through inbox");
                    c.That(game.Fault == null && Bands(game).Single().Destination == old && Routes(game).Single().From == old, "refusal preserves original site and logistics intent");
                    c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused connected checkpoint remains coherent");
                    continue;
                }
                if (!admission.IsNone) continue; // Baseline must retain its real refusal; no fabricated replacement.
                PermanentRemovalTests.Submit(c, game, command); twin = factory.Load(game.Save());
                Wait(() => game.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "connected.factory.next"), "timed connected facility replacement");
                var made = Entity(game, "connected.factory.next"); var rebound = Bands(game).Single();
                c.That(made != old && rebound.Destination == made && rebound.Endpoint == originalPool.Endpoint && rebound.Source.Identity == made.Value && rebound.Source.Entry == band.Source.Entry,
                    "original connected policy maps site/source together while retaining topology-owned endpoint");
                c.That(rebound.Settings.Matches(band.Settings) && rebound.Replenishing == band.Replenishing && rebound.CreatedTick == band.CreatedTick && rebound.LastDispatchTick == band.LastDispatchTick,
                    "connected stock settings, hysteresis and age are not recreated");
                c.That(Routes(game).Single().Id == route.Id && Routes(game).Single().From == made && Routes(game).Single().CreatedTick == route.CreatedTick,
                    "same physical route rebinds without resetting identity or age");
                var retainedPool = Pools(game).Single();
                c.That(retainedPool.Endpoint == originalPool.Endpoint && retainedPool.Version == originalPool.Version && retainedPool.MemberCount == 3,
                    "replacement does not recreate, resize or mutate the actual settled network inventory");
                c.Equal(Fix.FromInt(10), Total(game), "replacement transfers no pool stock into another buffer");
                var foreign = new List<LogisticsDemandView>(); game.Seat(1).View.Feature<LogisticsView>().CopyDemands(foreign);
                c.That(Bands(game).Count == 1 && foreign.Count == 0, "one original policy row remains company-private");
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                PermanentRemovalTests.Submit(c, game, new ConfigureRouteCommand { Route = route.Id, Settings = new LogisticsRouteSettings(true, route.Settings.Batch, route.Settings.Priority) });
                PermanentRemovalTests.Submit(c, twin, new ConfigureRouteCommand { Route = route.Id, Settings = new LogisticsRouteSettings(true, route.Settings.Batch, route.Settings.Priority) });
                Wait(() => PermanentRemovalTests.Jobs(game).Any(job => job.IsHaul && job.Route == route.Id && job.ToilIndex == 2 && job.Loaded.IsPositive &&
                    game.Seat(0).Snapshot.Entities.Single(entity => entity.Id == job.Worker).Spatial is PointSnapshot point && point.Position > Fix.FromInt(4) && point.Position < Fix.FromInt(9)), "actual loaded connected haul inside route");
                var loaded = PermanentRemovalTests.Jobs(game).Single(job => job.IsHaul && job.Route == route.Id && job.ToilIndex == 2 && job.Loaded.IsPositive);
                ulong loadedHash = game.Hash(); game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                c.That(game.Hash() == loadedHash && twin.Hash() == loadedHash && PermanentRemovalTests.Jobs(game).Any(job => job.Id == loaded.Id && job.Loaded == loaded.Loaded), "loaded connected receipt/job and real pool survive restoration");
                var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
                using (var replay = game.RecordReplay(limits))
                {
                    Wait(() => Stock(game, "destination") >= Fix.FromInt(3), "physical connected cargo unloading");
                    c.Equal(Fix.FromInt(10), Total(game), "physical network extraction, cargo and destination conserve original remaining stock");
                    c.Equal(0L, replay.CommandCount, "loaded connected continuation resubmits no route or shipment");
                    var verified = new ReplayVerifier().Verify(factory, replay.Finish(), limits);
                    c.That(verified.Success && verified.ActualHash == game.Hash(), "actual loaded connected continuation replay agrees");
                }
                c.Equal(isolated, idle.Hash(), "another connected session has no replacement effects");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current actual engine identity");
            foreach(bool half in new[]{false,true})foreach(bool independent in new[]{false,true})foreach(bool owned in new[]{false,true})Physical(c,half,independent,owned);
            Invalid(c);
        }
    }
}
