using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;
using static Beep.SimEngine.Tests.NetworkIncidentPropagationTests;

namespace Beep.SimEngine.Tests
{
    // Content selection and ordinary player input only; original engine owners perform every transition.
    internal static class NetworkPhysicalInterruptionTests
    {
        internal const string Case = "Network emergency: physical interruption rejects stale exposure and rebuilding earns a fresh delay";
        private static readonly Code Leak = new Code("leak"), Spread = new Code("spread");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool independent)
        {
            var removal=Json.Parse(FrameworkProofContent.Read("physical-line-removal.json"));
            var interruption=Json.Parse(FrameworkProofContent.Read("network-interruption.json"));
            if(independent) {
                removal.Set("independent_line_effects",removal.Get("line_effects"));removal.Remove("line_effects");
                interruption.Set("independent_network_incident_rules",interruption.Get("network_incident_propagation_rules"));
                interruption.Remove("network_incident_propagation_rules");
            }
            return Create(new InitializeProbe(),independent,edit:content=>{
                content.Get("activities")[0].Get("nodes")[0].Set("requirement",JsonValue.Of("interruption.response-water"));
            },editBindings:root=>{
                root.Get("tile_layer_setups")[0].Set("width",JsonValue.Number("3"));
                root.Get("tile_layer_setups")[1].Set("width",JsonValue.Number("3"));
                root.Get("entity_setups")[1].Get("placement").Set("x",JsonValue.Number("13"));
                root.Get("entities")[0].Get("components").Get("storage").Set("initial_stock",Json.Parse("{\"steel\":8}"));
                root.Get("entities")[0].Get("components").Get("incident_propagation").Set("rule",JsonValue.Of("interruption.spread"));
                root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("removal.site.start"));
                root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("removal.crane.start"));
            },additionalModules:new[] {new PhysicalLineRemovalProofModule()},
            additionalDeclarations:"[{\"id\":\"proof.physical-line-removal\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"physical-line-removal.json\",\"network-interruption.json\"]}]",
            additionalDocuments:new Dictionary<string,string> {
                {"physical-line-removal.json",removal.ToString()},{"network-interruption.json",interruption.ToString()}
            });
        }
        private static WorkView Work(SimGame game)=>game.Seat(0).View.Feature<WorkView>();
        private static int Tile(SimGame game)
        {
            var rows=new List<int>();
            if(game.Seat(0).View.Feature<TileMapView>().ReadLayer("network.line",new TileRect(11,28,1,1),rows)!=SpatialLayerReadStatus.Success)
                throw new InvalidOperationException("missing actual bridge geometry");
            return rows.Single();
        }
        private static Fix Amount(SimGame game,string entity,string resource)
        {
            var rows=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,entity),rows);
            return rows.Where(f=>f.Key==Storage.FacetStock&&f.Subject==resource).Aggregate(Fix.Zero,(s,f)=>s+f.Value);
        }
        private static Fix Steel(SimGame game)
        {
            var rows=new List<NetworkPoolView>();Code code=Network(game).CopyPools("binding.main",rows);
            if(!code.IsNone)throw new InvalidOperationException("actual pool stock "+code);
            game.Seat(0).View.TryFindResource("steel",out var steel);
            return Amount(game,"binding.source","steel")+rows.SelectMany(p=>p.Stock).Where(p=>p.Resource==steel).Aggregate(Fix.Zero,(s,p)=>s+p.Amount);
        }
        private static void Reach(SimGame game,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<256&&!ready();i++)Step(game,twin);
            if(!ready())throw new InvalidOperationException(stage+" step="+game.Clock.StepIndex+" bridge="+Tile(game)+
                " exposures="+string.Join(";",Exposure(game).Pending.Select(p=>p.Target+"/"+p.DueTick))+
                " source="+Incident(game,"binding.source",Leak).Snapshot.Active+"/"+Incident(game,"binding.source",Leak).Blocker+
                " target="+Incident(game,"binding.watcher",Leak).Snapshot.Active+"/"+Incident(game,"binding.watcher",Leak).Blocker);
        }
        private static void Supply(Check c,SimGame game,EntityId target,string resource)
        {
            game.Seat(0).View.TryFindResource(resource,out var id);
            Order(c,game,new HaulResourcesCommand {Source=Entity(game,"depot"),Destination=target,
                Worker=Entity(game,"truck"),Resource=id,Ceiling=Fix.One,Priority=100});
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual engine protocol");
            c.Equal(154,EngineInfo.SaveFormat,"original work/topology/incident/exposure codecs");
            c.Equal(Physical(c,false),Physical(c,true),"native/direct interruption chronology");
        }
        private static long Physical(Check c,bool independent)
        {
            var factory=Factory(independent);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            var limits=new ReplayLimits(4*1024*1024,4000);var recorder=game.RecordReplay(limits);
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"industrial emergency independent of business");
            c.Equal(Connection(game,"binding.source").Endpoint,Connection(game,"binding.watcher").Endpoint,"actual starting connected component");
            Order(c,game,new RaiseIncidentCommand {Target=Entity(game,"binding.source"),Source=Leak,Kind=new Code("network.fire")});
            Reach(game,null,()=>Exposure(game).Pending.Count==1,"first queued exposure");
            var original=Exposure(game).Pending.Single();
            c.Equal(Entity(game,"binding.watcher"),original.Target,"remote settled pool not eligible");
            c.Equal(60L,original.DueTick-original.ScheduledTick,"authored sixty-minute exposure");
            Order(c,game,new PostOrderCommand {Plan="removal.work",Target=Entity(game,"removal.site"),Priority=100});
            var orders=new List<OrderView>();Work(game).CopyOrders(orders);int cut=orders.Single(o=>o.Plan=="activity:removal.work").Id;
            Reach(game,null,()=>{var nodes=new List<ActivityNodeView>();return Work(game).TryCopyNodes(cut,nodes)&&nodes.Single().Progress.WholeTicks>0;},"partial physical cut");
            c.Equal(1,Tile(game),"partial work cannot disconnect");
            var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"partial cut and queued exposure save");
            Reach(game,twin,()=>Tile(game)==0,"earned physical cut");
            c.That(game.Clock.StepIndex<original.DueTick,"physical cut occurs before original exposure deadline");
            c.That(Connection(game,"binding.source").Endpoint!=Connection(game,"binding.watcher").Endpoint,"actual topology splits");
            c.Equal(Fix.FromInt(8),Steel(game),"cut conserves authored original supply");
            twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"disconnected pending exposure save");
            Reach(game,twin,()=>game.Clock.StepIndex>original.DueTick&&Exposure(game).Pending.Count==0,"stale due exposure refusal");
            c.That(!Incident(game,"binding.watcher",Leak).Snapshot.Active,"real cut rejects stale delayed damage");
            c.That(Incident(game,"binding.source",Leak).Snapshot.Active&&!Incident(game,"binding.source",Leak).Blocker.IsNone,
                "cause remains active with real response shortage, not test suppression");
            c.Equal(Code.None,Network(game).ReadGates(Entity(game,"binding.source"),"binding.main",out var gates),"source gate admission");
            c.That(gates.Blocker.IsNone,"no valve isolation substitutes for the physical cut");
            var lay=new LayLineCommand {Line="network.pipe"};lay.Tiles.Add(new TileCoord(11,28));Order(c,game,lay);
            var marker=game.Seat(0).Snapshot.Entities.Single(e=>e.Def=="sim.line-work.network.pipe").Id;
            Supply(c,game,marker,"steel");twin=factory.Load(game.Save());
            c.Equal(0,Tile(game),"reconstruction submission creates no instant link");
            Reach(game,twin,()=>Tile(game)==1,"physical material delivery and reconstruction");
            long rebuilt=game.Clock.StepIndex;
            c.Equal(Fix.FromInt(49),Amount(game,"depot","steel"),"one actual hauled reconstruction material consumed");
            c.Equal(Fix.FromInt(8),Steel(game),"reconstructed pools conserve original supply");
            c.Equal(Connection(game,"binding.source").Endpoint,Connection(game,"binding.watcher").Endpoint,"actual topology joins");
            Reach(game,twin,()=>Exposure(game).Pending.Count==1,"fresh connected exposure");
            var renewed=Exposure(game).Pending.Single();
            c.That(renewed.ScheduledTick>=rebuilt-1&&renewed.ScheduledTick>original.DueTick,"new scan follows real topology reconstruction");
            c.Equal(60L,renewed.DueTick-renewed.ScheduledTick,"reconnection earns fresh full delay");
            c.That(!Incident(game,"binding.watcher",Leak).Snapshot.Active&&renewed.DueTick>game.Clock.StepIndex,"reconnection never delivers instantly");
            var retained=Exposure(game);twin=factory.Load(game.Save());
            game.Clock.Pause();twin.Clock.Pause();ulong hash=game.Hash();long at=game.Clock.StepIndex;Step(game,twin);
            c.That(at==game.Clock.StepIndex&&hash==game.Hash(),"sole paused clock freezes renewed delay");
            game.Clock.Resume();twin.Clock.Resume();
            c.That(!game.Seat(1).View.Feature<IncidentPropagationView>().TryRead(Entity(game,"binding.source"),Spread,out _),"private exposure facts");
            Reach(game,twin,()=>Incident(game,"binding.watcher",Leak).Snapshot.Active,"fresh delayed damage");
            var damaged=Incident(game,"binding.watcher",Leak).Snapshot;
            c.That(damaged.Episode==1&&damaged.RaisedTick>=renewed.DueTick,"one exact target episode after fresh deadline");
            c.Equal(1,retained.Pending.Count,"detached pending facts retain old observation");
            c.That(!Incident(game,"binding.remote",Leak).Snapshot.Active,"disconnected remote never damaged");
            Supply(c,game,Entity(game,"binding.source"),"water");twin=factory.Load(game.Save());
            Reach(game,twin,()=>!Incident(game,"binding.source",Leak).Snapshot.Active,"actual source response water and work");
            c.That(Incident(game,"binding.source",Leak).Snapshot.ResolvedTick>damaged.RaisedTick,"source resolution earns physical response time");
            Reach(game,twin,()=>Exposure(game).Blocker==IncidentPropagationCodes.SourceEnded,"repaired source stops original cause");
            Supply(c,game,Entity(game,"binding.watcher"),"water");twin=factory.Load(game.Save());
            Reach(game,twin,()=>!Incident(game,"binding.watcher",Leak).Snapshot.Active,"actual target response water and work");
            c.Equal(1,Incident(game,"binding.watcher",Leak).Snapshot.Episode,"repair does not fabricate a new episode");
            c.Equal(Fix.FromInt(8),Amount(game,"depot","water"),"two actual response units consumed");
            c.Equal(Fix.Zero,Amount(game,"binding.source","water")+Amount(game,"binding.watcher","water")+Amount(game,"truck","water"),
                "delivered response stock consumed once, no stranded duplicate");
            c.Equal(0,Exposure(game).Pending.Count,"resolved cause retains no pending wave");
            c.Equal(Fix.FromInt(8),Steel(game),"hazard and repair conserve separate network supply");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed incident/topology/response checkpoint");
            c.Equal(isolatedHash,isolated.Hash(),"interleaved session unchanged");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"ordinary cut/rebuild/incident/physical response replay");
            return game.Clock.StepIndex;
        }
    }
}
