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
    // Pure JSON/master authoring and actual public player commands/views/Update only.
    internal static class OwnedSpatialNetworkTests
    {
        internal const string Case="Owned spatial networks: exact point ownership private physical flow and write-set admission";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool half,PointLineGeometryProofModule probe,string invalid=null)
        {
            var data=Json.Parse(FrameworkProofContent.Read("point-owned-network.json"));
            if(invalid=="fixed-owner")data.Get("spatial_layers")[0].Set("value_type",JsonValue.Of("fixed"));
            return SpatialNetworkGraphTests.Points(half,probe,moreDocuments:new Dictionary<string,string>{{"point-owned-network.json",data.ToString()}},
                editCorridors:root=>{
                    var line=root.Get("lines")[0];line.Set("owned",JsonValue.Of(true));line.Set("owner_layer",JsonValue.Of(invalid=="missing-owner"?"absent.owner":"corridor.company"));
                },editNetwork:root=>root.Get("networks")[0].Set("public",JsonValue.Of(false)));
        }
        private static InfrastructureView View(SimGame g,int seat=0)=>g.Seat(seat).View.Feature<InfrastructureView>();
        private static List<NetworkPoolView> Pools(SimGame g,int seat=0)
        {
            var values=new List<NetworkPoolView>();Code refusal=View(g,seat).CopyPools("point.network",values);
            if(!refusal.IsNone)throw new InvalidOperationException("owned actual pool facts "+refusal);return values;
        }
        private static Fix Layer(SimGame g,string name,int position)
        {
            var values=new List<Fix>();
            var status=g.Seat(0).View.Feature<PointLayerView>().ReadLayer(name,new PointRegion(Fix.FromInt(position),Fix.One,1),values);
            if(status!=SpatialLayerReadStatus.Success||values.Count!=1)throw new InvalidOperationException("owned sample "+status);
            return values[0];
        }
        private static Fix Total(SimGame g)
            =>SpatialCorridorExecutionTests.Total(g,true)+Pools(g).SelectMany(p=>p.Stock).Aggregate(Fix.Zero,(sum,q)=>sum+q.Amount);
        private static void Reach(SimGame g,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<900&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" clock="+g.Clock.StepIndex+" owner="+Layer(g,"corridor.company",5)+" pools="+Pools(g).Count);
        }
        private static void Privacy(Check c,SimGame g)
        {
            ulong before=g.Hash();c.Equal(0,Pools(g,1).Count,"foreign seat sees no private pool stock/identity");
            var flow=new List<NetworkFlowSnapshot>();c.Equal(Code.None,View(g,1).CopyFlow("point.network",flow),"foreign flow query is typed");
            c.Equal(0,flow.Count,"foreign seat sees no private flow timing");
            c.Equal(Codes.NotFound,View(g,1).ReadConnection(Entity(g,"source"),"point.network",out _),"foreign operational connection withheld");
            c.Equal(before,g.Hash(),"foreign private queries pure");
        }
        private static void Physical(Check c,bool half)
        {
            var probe=new PointLineGeometryProofModule();var f=Factory(half,probe);var g=ConstructionPlanTests.NewPoints(f);var idle=ConstructionPlanTests.NewPoints(f);ulong isolated=idle.Hash();
            c.That(probe.SharedOwner&&probe.NativeOwnerAbsent,"owned point uses same framework owners without native tile services");
            c.That(probe.IntegerAdmission.FractionRejected&&probe.IntegerAdmission.DuplicateRejected&&probe.IntegerAdmission.NoPartialWrite,
                "original sparse writer refuses fractional/duplicate write-set with no depth/owner prefix");
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var recorder=g.RecordReplay(limits)) {
                c.Equal(Fix.FromInt(20),Total(g),"all opening supply actual");
                long plan=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=SpatialCorridorExecutionTests.Intent(true,false)}).Arg0;
                Reach(g,null,()=>Pools(g).Count==1,"first physically owned segment");
                c.Equal(Fix.One,Layer(g,"corridor.company",3),"company0 encoded in actual declared integer layer");
                c.Equal(Fix.Zero,Layer(g,"corridor.company",7),"future ownership not fabricated");
                var held=Pools(g).Single();c.Equal(new PlayerId(0),held.Company,"actual pool company follows actual owned vertex");
                Privacy(c,g);
                var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"partial ownership layer/work/pool checkpoint");
                Reach(g,twin,()=>PermanentRemovalTests.Plan(g,plan).State==PlanState.Done,"complete physical owned graph");
                c.Equal(Fix.FromInt(14),Total(g),"build consumes only six actually supplied units");
                for(int p=3;p<=7;p+=2)c.Equal(Fix.One,Layer(g,"corridor.company",p),"each earned owned segment retained");
                var source=Entity(g,"source");var target=Entity(g,"destination");
                c.Equal(Code.None,View(g).ReadConnection(source,"point.network",out var from),"owner source connection");c.Equal(Code.None,View(g).ReadConnection(target,"point.network",out var to),"owner receiver connection");
                c.Equal(from.Endpoint,to.Endpoint,"owned physical pool connected");c.Equal(new PlayerId(0),from.Company,"connection uses actual company not public fallback");
                Reach(g,twin,()=>Stock(g,"destination").IsPositive,"real private timed stock transfer");Privacy(c,g);
                var path=Array.AsReadOnly(new SpatialPlacement[]{new PointPlacement("field.points",Fix.FromInt(5))});ulong before=g.Hash();
                c.Equal(LineCodes.ForeignLine,View(g,1).PreviewRemoval("corridor.point-line",path),"foreign company cannot erase actual ownership");c.Equal(before,g.Hash(),"foreign removal preview pure");
                var cut=new PostOrderCommand{Plan="corridor.remove",Target=Entity(g,"corridor.removal-site"),Priority=100};
                c.Equal(Codes.NotOwned,g.Seat(1).Preview(cut),"foreign target command refuses same owner semantics");c.Equal(before,g.Hash(),"foreign command preview pure");
                int sequence=g.Seat(1).Submit(cut);Step(g);long cursor=0;var results=new List<CommandResult>();g.Seat(1).ReadResults(ref cursor,results);
                var refused=results.Single(r=>r.Sequence==sequence);c.That(!refused.Accepted&&refused.Refusal==Codes.NotOwned,"ordinary submitted foreign removal refused");
                c.Equal(Fix.One,Layer(g,"corridor.company",5),"refused command keeps ownership");c.Equal(Fix.FromInt(14),Total(g),"refused command grants no cargo/stock");
                PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan=cut.Plan,Target=cut.Target,Priority=cut.Priority});
                twin=f.Load(g.Save());Reach(g,twin,()=>Layer(g,"corridor.depth",5)==Fix.Zero,"owner machine earns cut");
                c.Equal(Fix.Zero,Layer(g,"corridor.company",5),"same spatial writer clears depth and actual company");
                c.Equal(2,Pools(g).Count,"original owned topology split");c.That(Pools(g).All(p=>p.Company==new PlayerId(0)),"split retains actual company without new balance");
                c.Equal(Fix.FromInt(14),Total(g),"owned split conserves all actual stock");Privacy(c,g);
                var args=Json.Parse("{\"space\":\"field.points\",\"path\":[{\"space\":\"field.points\",\"position\":5}]}");
                long rebuild=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=new PlanIntent("line:corridor.point-line",args,"owned-rebuild",60,false)}).Arg0;
                twin=f.Load(g.Save());Reach(g,twin,()=>PermanentRemovalTests.Plan(g,rebuild).State==PlanState.Done,"physically supplied owned rebuild");
                c.Equal(Fix.One,Layer(g,"corridor.company",5),"rebuild earns actual company again");c.Equal(1,Pools(g).Count,"same original owned pool rejoins");
                c.Equal(Fix.FromInt(12),Total(g),"rebuild consumes actual two supplies");Privacy(c,g);
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"owned sample/pool/flow saved");c.Equal(1,held.MemberCount,"retained earlier pool facts detached");
                c.Equal(isolated,idle.Hash(),"owned world sessions isolated");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"private owned player stream replays through original engine");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current owned provider build");c.Equal(154,EngineInfo.SaveFormat,"existing graph/point codecs");
            foreach(bool half in new[]{false,true})Physical(c,half);
            foreach(string fault in new[]{"fixed-owner","missing-owner"}) {
                var probe=new PointLineGeometryProofModule();c.Throws<ContentException>(()=>ConstructionPlanTests.NewPoints(Factory(false,probe,fault)),"invalid actual owner metadata "+fault);
                c.Equal(0,probe.Initialization.Initializations,"metadata refusal before Initialize "+fault);
            }
        }
    }
}
