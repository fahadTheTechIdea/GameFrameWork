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
    // Pure authored provider selection; ordinary player commands/views and one Game.Update owner.
    internal static class NetworkGraphAdmissionTests
    {
        internal const string Case="Network graph admission: complete occupied reciprocity and callback fault cutoff";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool half,string fault=null)
            =>SpatialNetworkGraphTests.Points(half,new PointLineGeometryProofModule(),extraAuthor:root=>{
                if(fault!=null)root.Get("point_spaces")[0].Get("network_graph").Set("fault",JsonValue.Of(fault));
            });
        private static List<NetworkPoolView> Pools(SimGame g)
        {
            var values=new List<NetworkPoolView>();
            Code refusal=g.Seat(0).View.Feature<InfrastructureView>().CopyPools("point.network",values);
            if(!refusal.IsNone)throw new InvalidOperationException("actual admission pool facts "+refusal);
            return values;
        }
        private static long Build(Check c,SimGame g)
            =>PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=SpatialCorridorExecutionTests.Intent(true,false)}).Arg0;
        private static void Healthy(Check c,bool half)
        {
            var f=Factory(half);var g=ConstructionPlanTests.NewPoints(f);var isolated=ConstructionPlanTests.NewPoints(f);ulong initial=isolated.Hash();
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var recorder=g.RecordReplay(limits)) {
                long plan=Build(c,g);
                for(int i=0;i<900&&Pools(g).Count==0;i++)Step(g);
                c.Equal(1,Pools(g).Count,"first physically installed vertex admitted");
                var retained=Pools(g).Single();c.Equal(1,retained.MemberCount,"actual partial physical graph");
                var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"partial graph stock and work saves");
                long at=g.Clock.StepIndex;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);c.Equal(at,g.Clock.StepIndex,"sole clock pause");g.Clock.Resume();twin.Clock.Resume();
                for(int i=0;i<900&&PermanentRemovalTests.Plan(g,plan).State!=PlanState.Done;i++)Step(g,twin);
                c.Equal(PlanState.Done,PermanentRemovalTests.Plan(g,plan).State,"complete normal physical graph");
                c.Equal(1,Pools(g).Count,"one reciprocal connected component");c.Equal(3,Pools(g).Single().MemberCount,"all actual vertices admitted");
                c.Equal(1,retained.MemberCount,"retained partial facts never rewrite");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"completed admission and original flow durable");
                c.Equal(initial,isolated.Hash(),"interleaved graph sessions isolated");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"same player physical input replays");
            }
        }
        private static void Malformed(Check c,bool half,string mode)
        {
            var f=Factory(half,mode);var g=ConstructionPlanTests.NewPoints(f);var healthy=ConstructionPlanTests.NewPoints(Factory(half));ulong other=healthy.Hash();
            long plan=Build(c,g);
            NetworkPoolView retained=null;
            var view=g.Seat(0).View.Feature<InfrastructureView>();
            for(int i=0;i<900&&g.Fault==null;i++) {
                if(Pools(g).Count==1)retained=Pools(g).Single();
                g.Update();
            }
            c.That(g.Fault!=null&&g.Snapshot.State==GameState.Faulted,"actual physical second vertex refuses malformed graph "+mode);
            if(g.Fault==null)throw new InvalidOperationException("malformed graph never faulted "+mode+" clock="+g.Clock.StepIndex+" plan="+PermanentRemovalTests.Plan(g,plan).State);
            var first=g.Fault;long at=g.Clock.StepIndex;
            c.That(retained!=null&&retained.MemberCount==1,"failed admission cannot rewrite retained earlier pool "+mode);
            string expected=mode=="backward-only"?"reciprocal connected edges":mode=="duplicate-vertex"||mode=="invalid-owner"?"invalid or duplicate actual vertex/company":
                mode=="throw"?"proof.graph.callback.first":mode=="mutate-layer"?"mutated its persisted state":"invalid potential neighbour";
            c.That(first.ToString().Contains(expected),"typed original admission cause retained "+mode);
            c.Equal(at,first.Tick,"fault time uses actual sole clock "+mode);
            c.Throws<InvalidOperationException>(()=>g.Save(),"partial malformed graph cannot checkpoint "+mode);
            var destination=new List<NetworkPoolView>();
            c.Throws<InvalidOperationException>(()=>view.CopyPools("point.network",destination),"retained graph view rejects first fault "+mode);
            c.Equal(0,destination.Count,"no healthy graph prefix after fault "+mode);
            c.Equal(0,g.Update(),"fault stops further update "+mode);c.Equal(at,g.Clock.StepIndex,"fault does not earn later time "+mode);
            c.That(ReferenceEquals(first,g.Fault),"first fault object cannot be replaced "+mode);
            c.Equal(other,healthy.Hash(),"malformed provider does not affect another actual session "+mode);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"complete graph admission build");c.Equal(154,EngineInfo.SaveFormat,"existing graph/stock/flow codecs unchanged");
            foreach(bool half in new[]{false,true})Healthy(c,half);
            foreach(bool half in new[]{false,true})foreach(string mode in new[]{"backward-only","duplicate-neighbour","self","invalid-neighbour","duplicate-vertex","invalid-owner","throw","mutate-layer"})
                Malformed(c,half,mode);
        }
    }
}
