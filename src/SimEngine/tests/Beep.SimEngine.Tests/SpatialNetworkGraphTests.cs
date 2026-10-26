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
    // Authored JSON/master and public player commands/views/Update only; no pool/layer/worker mutations.
    internal static class SpatialNetworkGraphTests
    {
        internal const string Case="Spatial network graphs: one topology stock and flow owner across physical native and point corridors";
        internal static void Register()=>TestRunner.Add(Case,Run);
        internal static GameFactory Points(bool half,PointLineGeometryProofModule probe,bool missingGraph=false,Action<JsonValue> extraAuthor=null,
            Action<JsonValue> editCorridors=null,IReadOnlyDictionary<string,string> moreDocuments=null,Action<JsonValue> editNetwork=null,
            IReadOnlyList<GameModule> extraModules=null,string extraDeclarations=null,IReadOnlyDictionary<string,string> moduleDocuments=null,
            Action<JsonValue> afterPorts=null,string retryProfiles=null)
        {
            var documents=new Dictionary<string,string>{
                {"point-corridor-removal.json",FrameworkProofContent.Read("point-corridor-removal.json")},
                {"point-connected-network.json",FrameworkProofContent.Read("point-connected-network.json")}};
            if(editNetwork!=null){var network=Json.Parse(documents["point-connected-network.json"]);editNetwork(network);documents["point-connected-network.json"]=network.ToString();}
            if(moreDocuments!=null)foreach(var entry in moreDocuments)documents.Add(entry.Key,entry.Value);
            return SpatialCorridorExecutionTests.Points(half,probe,edit:editCorridors,
                extraDocuments:documents,
                editWorld:root=>{
                    root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("corridor.removal-site.start"));
                    if(missingGraph)root.Get("point_spaces")[0].Remove("network_graph");
                    extraAuthor?.Invoke(root);
                    root.Get("entities")[0].Get("components").Set("ports",Json.Parse("{\"list\":[{\"network\":\"point.network\",\"push\":[\"supply\"],\"pull\":[]}]}"));
                    root.Get("entities")[1].Get("components").Set("ports",Json.Parse("{\"list\":[{\"network\":\"point.network\",\"push\":[],\"pull\":[\"supply\"]}]}"));
                    afterPorts?.Invoke(root);
                },extraModules:extraModules,extraDeclarations:extraDeclarations,moduleDocuments:moduleDocuments,retryProfiles:retryProfiles);
        }
        private static GameFactory Native(bool half,InfrastructureServiceContractsProofModule probe)
            =>PhysicalLineRemovalTests.Create(false,additionalModule:probe,editDefinitions:root=>{
                    if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));},
                editNetwork:root=>{
                    var band=root.Get("entities")[0].Get("components").Get("signal_network_gate").Get("conditions")[0];
                    band.Set("minimum",JsonValue.Number("26"));band.Set("maximum",JsonValue.Number("30"));
                });
        private static string Network(bool point)=>point?"point.network":"binding.main";
        private static InfrastructureView View(SimGame g)=>g.Seat(0).View.Feature<InfrastructureView>();
        private static List<NetworkPoolView> Pools(SimGame g,bool point)
        {
            var rows=new List<NetworkPoolView>();Code refusal=View(g).CopyPools(Network(point),rows);
            if(!refusal.IsNone)throw new InvalidOperationException("actual graph pools "+refusal);return rows;
        }
        private static Fix Total(SimGame g,bool point)
        {
            string name=point?"supply":"steel";Fix total=Fix.Zero;
            g.Seat(0).View.TryFindResource(name,out var resource);
            foreach(var entity in g.Seat(0).Snapshot.Entities) {
                var facts=new List<Facet>();g.Seat(0).View.Inspect(entity.Id,facts);
                foreach(var fact in facts)if(fact.Key==Storage.FacetStock&&fact.Subject==name)total+=fact.Value;
            }
            return total+Pools(g,point).SelectMany(p=>p.Stock).Where(q=>q.Resource==resource).Aggregate(Fix.Zero,(sum,q)=>sum+q.Amount);
        }
        private static Fix Middle(SimGame g,bool point)
            =>point?SpatialCorridorExecutionTests.Segment(g,true,1):Fix.FromInt(PhysicalLineRemovalTests.Tile(g,11));
        private static List<ActivityNodeView> Nodes(SimGame g,int order)
        {
            var rows=new List<ActivityNodeView>();
            if(!g.Seat(0).View.Feature<WorkView>().TryCopyNodes(order,rows))throw new InvalidOperationException("missing actual graph work");
            return rows;
        }
        private static void Reach(SimGame g,SimGame twin,Func<bool> ready,string stage,bool point)
        {
            for(int i=0;i<900&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" clock="+g.Clock.StepIndex+" middle="+Middle(g,point)+" pools="+Pools(g,point).Count);
        }
        private static void Flow(Check c,bool point,bool half)
        {
            var native=new InfrastructureServiceContractsProofModule();var independent=new PointLineGeometryProofModule();
            var f=point?Points(half,independent):Native(half,native);
            var g=point?ConstructionPlanTests.NewPoints(f):PhysicalLineRemovalTests.New(f);
            var other=point?ConstructionPlanTests.NewPoints(f):PhysicalLineRemovalTests.New(f);ulong untouched=other.Hash();
            c.That(point?independent.SharedOwner&&independent.NativeOwnerAbsent:native.OneOwner&&native.PersistentOwners==1&&native.EndpointOwners==1,
                "same original topology/stock/flow owner with separate actual geometry");
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var recorder=g.RecordReplay(limits)) {
                if(point) {
                    c.Equal(0,Pools(g,true).Count,"unbuilt point layer is not fabricated network");
                    long plan=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=SpatialCorridorExecutionTests.Intent(true,false)}).Arg0;
                    var twinBuild=f.Load(g.Save());
                    Reach(g,twinBuild,()=>PermanentRemovalTests.Plan(g,plan).State==PlanState.Done,"physically supplied point graph construction",true);
                    c.Equal(Fix.FromInt(14),Total(g,true),"actual build consumes six supplies across entities and pools");
                }
                else {
                    c.That(Pools(g,false).All(p=>p.Stock.Count==0),"startup geometry never grants immediate flow stock");
                    Step(g);
                    var early=new List<NetworkFlowSnapshot>();c.Equal(Code.None,View(g).CopyFlow(Network(false),early),"actual first-step native service facts");
                    c.That(early.Any(x=>x.LastTick==g.Clock.StepIndex),"native service exists only after actual update");
                    for(int i=0;i<3;i++)Step(g);
                }
                c.Equal(1,Pools(g,point).Count,"three actual vertices form one connected pool");
                c.Equal(3,Pools(g,point).Single().MemberCount,"generic graph member facts are not tiles");
                var retained=Pools(g,point).Single();EntityId source=Entity(g,point?"source":"binding.source"),target=Entity(g,point?"destination":"binding.watcher");
                ulong pure=g.Hash();
                c.Equal(Code.None,View(g).ReadConnection(source,Network(point),out var from),"actual source contact");
                c.Equal(Code.None,View(g).ReadConnection(target,Network(point),out var to),"actual target contact");
                c.Equal(from.Endpoint,to.Endpoint,"same real connected stock identity");c.Equal(pure,g.Hash(),"graph facts pure");
                var saved=f.Load(g.Save());Step(g,saved);
                if(point)Reach(g,saved,()=>Stock(g,"destination").IsPositive,"elapsed real point transfer",true);
                var flow=new List<NetworkFlowSnapshot>();c.Equal(Code.None,View(g).CopyFlow(Network(point),flow),"original flow owner publishes earned elapsed facts");
                if(point)c.That(flow.Any(x=>x.LastTick>0)&&flow.All(x=>x.LastTick<=g.Clock.StepIndex),"point flow facts derive only from actual prior game steps");
                else {
                    c.Equal(Code.None,View(g).ReadGates(source,Network(false),out var gate),"actual authored native flow gate facts");
                    c.Equal(new Code("binding.backpressure"),gate.Blocker,"completed native service stops at authored room band");
                }
                Fix total=Total(g,point);
                var command=new PostOrderCommand{Plan=point?"corridor.remove":"removal.work",Target=Entity(g,point?"corridor.removal-site":"removal.site"),Priority=100};
                int order=(int)PermanentRemovalTests.Submit(c,g,command).Arg0;
                Reach(g,null,()=>Nodes(g,order).Single().Progress.WholeTicks>0,"machine travels and earns graph removal",point);
                c.That(Middle(g,point).IsPositive,"partial work never silently deletes vertex");
                var twin=f.Load(g.Save());long at=g.Clock.StepIndex;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);
                c.Equal(at,g.Clock.StepIndex,"one paused clock retains flow and physical work");g.Clock.Resume();twin.Clock.Resume();
                Reach(g,twin,()=>Middle(g,point)==Fix.Zero,"earned split through original disposition",point);
                c.Equal(2,Pools(g,point).Count,"same algorithm splits native and point pools");
                c.Equal(total,Total(g,point),"split conserves all actual tracked resource");
                c.Equal(3,retained.MemberCount,"old connected facts detached");
                c.Equal(Code.None,View(g).ReadConnection(source,Network(point),out from),"left actual connection");
                c.Equal(Code.None,View(g).ReadConnection(target,Network(point),out to),"right actual connection");
                c.That(from.Endpoint!=to.Endpoint,"split identities differ");c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"stable ID graph checkpoints");
                JsonValue args=point?Json.Parse("{\"space\":\"field.points\",\"path\":[{\"space\":\"field.points\",\"position\":5}]}"):
                    Json.Parse("{\"space\":\"recovery.tiles\",\"path\":[{\"space\":\"recovery.tiles\",\"x\":11,\"y\":28}]}");
                long rebuild=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=new PlanIntent(point?"line:corridor.point-line":"line:network.pipe",args,"reconnect",60,false)}).Arg0;
                if(!point) {
                    g.Seat(0).View.TryFindResource("steel",out var steel);
                    var marker=g.Seat(0).Snapshot.Entities.Single(e=>e.Def=="sim.line-work.network.pipe").Id;
                    PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand{Source=Entity(g,"depot"),Destination=marker,
                        Worker=Entity(g,"truck"),Resource=steel,Ceiling=Fix.One,Priority=100});
                }
                twin=f.Load(g.Save());Reach(g,twin,()=>PermanentRemovalTests.Plan(g,rebuild).State==PlanState.Done,"physical reconnect with actual material",point);
                c.Equal(1,Pools(g,point).Count,"original topology owner joins connected graph");
                c.Equal(total-Fix.FromInt(point?2:1),Total(g,point),"rebuild has only authored physical cost");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"reconnected geometry/pool/flow durable");
                c.Equal(untouched,other.Hash(),"interleaved graph sessions isolated");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"ordinary graph input stream replays");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual portable graph build");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"independently authored current global save envelope");
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})Flow(c,point,half);
            var probe=new PointLineGeometryProofModule();
            c.Throws<ContentException>(()=>ConstructionPlanTests.NewPoints(Points(false,probe,true)),"missing authored point graph refuses before initialization");
            c.Equal(0,probe.Initialization.Initializations,"missing graph never starts an update");
        }
    }
}
