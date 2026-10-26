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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Actual authored files and ordinary player commands/immutable facts/Update only; never transaction or stock calls.
    internal static class SameEndpointConversionTests
    {
        internal const string Case="Same endpoint conversion: full native and connected buffers process atomically and preserve physical backpressure";
        private static readonly Code Shared=new Code("shared"), Process=new Code("process");
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static ResourceId Supply(SimGame g)
        { if(!g.Seat(0).View.TryFindResource("supply",out var r))throw new InvalidOperationException("authored supply missing");return r; }
        private static ResourcePortSnapshot Port(SimGame g,string prototype)
        {
            Code refusal=g.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Entity(g,prototype),Shared,Supply(g),out var value);
            if(!refusal.IsNone)throw new InvalidOperationException("actual net port "+refusal);return value;
        }
        private static TransformationSnapshot Current(SimGame g,string prototype)
        {
            if(!g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Entity(g,prototype),Process,out var value))
                throw new InvalidOperationException("actual net processor missing");return value;
        }
        private static List<NetworkPoolView> Pools(SimGame g)
        {
            var rows=new List<NetworkPoolView>();Code refusal=g.Seat(0).View.Feature<InfrastructureView>().CopyPools("point.network",rows);
            if(!refusal.IsNone)throw new InvalidOperationException("original pool "+refusal);return rows;
        }
        private static Fix Total(SimGame g)=>SpatialCorridorExecutionTests.Total(g,true)+Pools(g).SelectMany(p=>p.Stock).Aggregate(Fix.Zero,(s,q)=>s+q.Amount);
        private static void Reach(SimGame g,SimGame twin,Func<bool> ready,string stage,string prototype="connected.factory")
        {
            for(int i=0;i<800&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" exceeded800Updates step="+g.Clock.StepIndex+" phase="+Current(g,prototype).Phase+
                "/"+Current(g,prototype).Blocker+" stock="+(Port(g,prototype).Stock?.AmountOf(Supply(g)).ToString()??Port(g,prototype).Blocker.ToString())+
                " jobs="+string.Join(";",PermanentRemovalTests.Jobs(g).Select(j=>j.State+"/"+j.Blocker)));
        }
        private static void Enqueue(Check c,SimGame g,string prototype,string recipe)=>PermanentRemovalTests.Submit(c,g,
            new EnqueueTransformationCommand{Transformer=Entity(g,prototype),Capability=Process,Transformation=recipe});
        private static void Stop(Check c,SimGame g,string prototype)=>PermanentRemovalTests.Submit(c,g,
            new CancelTransformationCommand{Transformer=Entity(g,prototype),Capability=Process,Index=0});
        private static BigInteger Recorded(SimGame g,string recipe,ResourceFlowKind kind)
        {
            var facts=g.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            if(facts.Status!=ResourceFlowReadStatus.Ready)throw new InvalidOperationException("actual flow journal "+facts.Status);
            return facts.Aggregates.Where(x=>x.Cause==new Code(recipe)&&x.Kind==kind&&x.Resource==Supply(g)).Aggregate(BigInteger.Zero,(s,x)=>s+x.AmountRaw);
        }
        private static void Preview(Check c,SimGame g,string prototype,string recipe,Code blocker)
        {
            ulong before=g.Hash();c.Equal(Code.None,g.Seat(0).View.Feature<TransformationManagementView>().ReadExecutionPreview(Entity(g,prototype),Process,recipe,out var facts),"actual execution preview");
            c.Equal((Code?)blocker,facts.CurrentBlocker,"preview and normal execution share net-capacity refusal");
            c.That(!g.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Entity(g,prototype),Process,out _),"private processor withheld");
            c.Equal(before,g.Hash(),"net preview is pure");
        }
        private static void Physical(Check c,bool half,bool independent,bool owned)
        {
            var proof=new ConnectedResourcePortModule();
            var f=ConnectedResourcePortTests.Factory(half,independent,owned,proof,
                definitions:FrameworkProofContent.Read("same-endpoint-conversion.json"),editWorld:root=>{
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
                    root.Get("entities")[0].Get("components").Get("point_stock").Set("initial",JsonValue.Number("18"));
                    root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("net.local-start"));
                });
            var g=ConstructionPlanTests.NewPoints(f);var idle=ConstructionPlanTests.NewPoints(f);ulong isolated=idle.Hash();
            var limits=new ReplayLimits(4*1024*1024,5000);
            using(var recorder=g.RecordReplay(limits)) {
                c.That(proof.SharedOwner,"same topology/stock owner, no conversion endpoint registry");
                c.Equal(Fix.FromInt(30),Total(g),"real authored eighteen network supplies plus twelve local supplies");
                long build=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=SpatialCorridorExecutionTests.Intent(true,false)}).Arg0;
                var twin=f.Load(g.Save());Reach(g,twin,()=>PermanentRemovalTests.Plan(g,build).State==PlanState.Done&&Port(g,"connected.factory").Ready&&Port(g,"connected.factory").Stock.AmountOf(Supply(g))==Fix.FromInt(12),"physically filled connected circuit");
                c.Equal(Fix.FromInt(24),Total(g),"six real corridor inputs consumed, no injected buffer");
                foreach(string prototype in new[]{"connected.factory","net.local"}) {
                    Fix beforeEqual=Total(g);
                    Enqueue(c,g,prototype,"net.equal");
                    PermanentRemovalTests.Submit(c,g,new ConfigureOperationCommand{Entity=Entity(g,prototype),Operation=Process,Enabled=true,Target=Fix.One});
                    twin=f.Load(g.Save());Reach(g,twin,()=>Current(g,prototype).Flow.Produced.AmountOf(Supply(g))>=Fix.FromInt(4),"full buffer equal-input/output productive work",prototype);
                    var active=Current(g,prototype);var tool=active.Tools.Receipts.Single();
                    c.Equal(Code.None,active.Blocker,"full same-store circuit is productive, not false output-full");
                    c.Equal(Fix.FromInt(12),Port(g,prototype).Stock.AmountOf(Supply(g)),"net-zero conversion retains exactly full measured stock");
                    c.Equal(Port(g,prototype).Endpoint,tool.Token.Endpoint,"actual original reusable-tool endpoint");
                    c.Equal(Fix.One,tool.Amount,"additional running input consumes only its delta and preserves original tool");
                    c.Equal(active.Flow.Consumed.AmountOf(Supply(g)),active.Flow.Produced.AmountOf(Supply(g)),"earned native flow consumes/produces equal amounts");
                    c.Equal(Recorded(g,"net.equal",ResourceFlowKind.Consumed),Recorded(g,"net.equal",ResourceFlowKind.Produced),"verified consume/produce journal deltas match");
                    Preview(c,g,prototype,"net.equal",Code.None);twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"full-buffer progress/partial tool books restore");
                    long step=g.Clock.StepIndex;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);c.Equal(step,g.Clock.StepIndex,"single clock pause freezes net conversion");g.Clock.Resume();twin.Clock.Resume();
                    Stop(c,g,prototype);twin=f.Load(g.Save());Reach(g,twin,()=>Current(g,prototype).QueueCount==0,"continuous stop at full buffer",prototype);
                    c.Equal(beforeEqual,Total(g),"continuous stop invents no output/refund");
                    Enqueue(c,g,prototype,"net.growing");twin=f.Load(g.Save());
                    Reach(g,twin,()=>Current(g,prototype).Blocker==TransformerManager.BlockerOutput,"positive net growth is real capacity refusal",prototype);
                    var blocked=Current(g,prototype);var retainedTools=blocked.Tools.Receipts;
                    Preview(c,g,prototype,"net.growing",TransformerManager.BlockerOutput);
                    var consumption=Recorded(g,"net.growing",ResourceFlowKind.Consumed);var production=Recorded(g,"net.growing",ResourceFlowKind.Produced);
                    twin=f.Load(g.Save());for(int i=0;i<4;i++)Step(g,twin);
                    c.Equal(blocked.Progress,Current(g,prototype).Progress,"capacity refusal earns no speculative progress");
                    c.Equal(retainedTools.Count,Current(g,prototype).Tools.Receipts.Count,"capacity wait neither invents nor discards tool claims");
                    if(retainedTools.Count>0)c.Equal(retainedTools.Single().Token,Current(g,prototype).Tools.Receipts.Single().Token,"wait retains exact actual original tool receipt");
                    c.Equal(Fix.FromInt(12),Port(g,prototype).Stock.AmountOf(Supply(g)),"refused positive-net draw spends no stock");
                    c.Equal(consumption,Recorded(g,"net.growing",ResourceFlowKind.Consumed),"no consumed success prefix on refusal");
                    c.Equal(production,Recorded(g,"net.growing",ResourceFlowKind.Produced),"no produced success prefix on refusal");
                    if(prototype=="connected.factory") {
                        long before=g.Clock.StepIndex;
                        PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand{Source=Entity(g,prototype),SourcePort=Shared,Destination=Entity(g,"destination"),Worker=Entity(g,"machine"),Resource=Supply(g),Ceiling=Fix.FromInt(2),Priority=100});
                        c.Equal(Fix.Zero,Stock(g,"destination"),"drain requires real loading/travel/unloading");twin=f.Load(g.Save());
                        Reach(g,twin,()=>Stock(g,"destination")==Fix.FromInt(2)&&Current(g,prototype).Progress.WholeTicks>blocked.Progress.WholeTicks&&Current(g,prototype).Blocker==TransformerManager.BlockerOutput,"physical drain resumes bounded net growth",prototype);
                        c.That(g.Clock.StepIndex>before+1,"physical capacity release takes game time");
                        c.Equal(Fix.FromInt(12),Port(g,prototype).Stock.AmountOf(Supply(g)),"actual growth refills only physical freed capacity");
                        c.Equal(Fix.FromInt(26),Total(g),"two actual net-generated supplies account for the physical drain");
                    }
                    Stop(c,g,prototype);twin=f.Load(g.Save());Reach(g,twin,()=>Current(g,prototype).QueueCount==0,"blocked continuous cancellation releases original tool",prototype);
                }
                c.Equal(Recorded(g,"net.growing",ResourceFlowKind.Produced)-Recorded(g,"net.growing",ResourceFlowKind.Consumed),new BigInteger(Fix.FromInt(2).Raw),"journal net generation equals actual physical increase");
                c.Equal(Fix.FromInt(26),Total(g),"settled full circuits retain accounted stock");
                c.Equal(0,Pools(g).Single().OutboundPromises.Count,"shared full-buffer circuit releases every actual tool/pickup promise");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"complete full-buffer net state durable");c.Equal(isolated,idle.Hash(),"interleaved independent session unchanged");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"same public player stream replays full-buffer production and physical backpressure");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current actual engine identity");
            foreach(bool half in new[]{false,true})foreach(bool independent in new[]{false,true})foreach(bool owned in new[]{false,true})Physical(c,half,independent,owned);
        }
    }
}
