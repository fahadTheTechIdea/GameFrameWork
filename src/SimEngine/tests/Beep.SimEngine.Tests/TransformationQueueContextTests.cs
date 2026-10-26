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
    // Only authored composition and public player requests/views/Update/saves/replay/clock.
    internal static class TransformationQueueContextTests
    {
        internal const string Case = "Transformation queue context: legitimate physical cancellation and original clock identity mutation faults";
        private static readonly Code Pulse=new Code("pulse"),Process=new Code("process"),Raw=new Code("raw"),Tool=new Code("tool");
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",9000000001),Second=new ResourceEndpointId("proof.pools",9000000002);
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Independent(string mode,bool half=false) {
            var doc=Json.Parse(FrameworkProofContent.Read("queue-context.json"));doc.Get("entities")[0].Get("components").Get("pulse_transformer").Set("queue_fault",JsonValue.Of(mode));
            return TransformerTests.Factory(root=>{
                if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.queue-context"));
                root.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile",JsonValue.Of("queue.context.start"));
            },new QueueContextProofModule(),
                @"{""id"":""proof.queue-context"",""version"":""1.0.0"",""requires"":[{""id"":""proof.transformer"",""version"":""1.0.0""},{""id"":""sim.world-setup"",""version"":""1.0.0""}],""documents"":[""queue-context.json""]}",
                doc.ToString());
        }
        private static SimGame New(GameFactory f,string scenario="process")=>f.NewGame(new NewGameRequest{Scenario=scenario,Seed=6811,Seats={Controller.Human,Controller.Human}});
        private static TransformationManagementView View(SimGame g,int seat=0)=>g.Seat(seat).View.Feature<TransformationManagementView>();
        private static TransformationSnapshot Current(SimGame g,EntityId site,Code capability) {
            if(!View(g).TryGetTransformation(site,capability,out var p))throw new InvalidOperationException("actual queue missing");return p;
        }
        private static void Until(SimGame g,SimGame twin,Func<bool> ready,string stage,EntityId site,Code capability) {
            for(int i=0;i<1000&&!ready();i++)Step(g,twin);
            if(!ready()) {var p=Current(g,site,capability);throw new InvalidOperationException(stage+" exceeds1000Updates clock="+g.Clock.StepIndex+" queue="+p.QueueCount+"/"+p.Phase+"/"+p.Blocker+"/"+p.Progress);}
        }
        private static void Enqueue(Check c,SimGame g,EntityId site,Code cap,string recipe,int count=1)=>PermanentRemovalTests.Submit(c,g,
            new EnqueueTransformationCommand{Transformer=site,Capability=cap,Transformation=recipe,Count=count});
        private static void Cancel(Check c,SimGame g,EntityId site,Code cap,int index)=>PermanentRemovalTests.Submit(c,g,
            new CancelTransformationCommand{Transformer=site,Capability=cap,Index=index});
        private static void Healthy(Check c,bool half) {
            var f=Independent("none",half);var g=New(f);var idle=New(f);ulong untouched=idle.Hash();var site=Entity(g,"queue.context.emitter");
            var limits=new ReplayLimits(4*1024*1024,5000);using var recording=g.RecordReplay(limits);
            Enqueue(c,g,site,Pulse,"pulse",3);var original=Current(g,site,Pulse);c.Equal(3,original.QueueCount,"legitimate independent exact append accepted");
            var twin=f.Load(g.Save());Step(g,twin);c.Equal(g.Hash(),twin.Hash(),"ordinary append state restores");
            Cancel(c,g,site,Pulse,1);var tail=Current(g,site,Pulse);
            c.Equal(2,tail.QueueCount,"legitimate independent tail removes one");c.Equal(original.Queue[0].Execution,tail.Queue[0].Execution,"tail preserves active head identity");
            c.Equal(original.Queue[2].Execution,tail.Queue[1].Execution,"tail preserves unrelated next entry");
            PermanentRemovalTests.Submit(c,g,new ConfigureOperationCommand{Entity=site,Operation=Pulse,Enabled=true,Target=Fix.One});
            c.That(Current(g,site,Pulse).Progress.WholeTicks>0,"independent process earns real time");
            Cancel(c,g,site,Pulse,0);c.Equal(1,Current(g,site,Pulse).QueueCount,"working independent head cancellation preserves last entry");
            twin=f.Load(g.Save());Until(g,twin,()=>Current(g,site,Pulse).QueueCount==0,"remaining independent real completion",site,Pulse);
            c.Equal(1,g.Seat(0).Snapshot.Entities.Count(x=>x.Def=="token"),"only remaining timed entry creates one real result");
            c.Equal(3,original.QueueCount,"retained original append snapshot detached");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"independent terminal queue durable");c.Equal(untouched,idle.Hash(),"independent queue sessions isolated");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"legitimate append/tail/head/completion replay");
        }
        private static LogisticsOperationalSnapshot Stock(SimGame g,EntityId site,ResourceId r,Code port) {
            Code refusal=g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(site,r,out var facts,port);
            if(!refusal.IsNone)throw new InvalidOperationException("actual original physical stock refused "+refusal);return facts;
        }
        private static void Physical(Check c,bool point,bool half) {
            var f=ResourcePortAvailabilityTests.Factory(point,half);var g=New(f,point?"scoped-point-dock-restoration":"scoped-dock-restoration");
            var idle=New(f,point?"scoped-point-dock-restoration":"scoped-dock-restoration");ulong untouched=idle.Hash();
            var site=Entity(g,"availability.factory");string prefix=point?"point.":"";
            if(!g.Seat(0).View.TryFindResource("steel",out var r))throw new InvalidOperationException("actual steel missing");
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=Entity(g,prefix+"dock.queue"),Held=true});
            PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand{Source=Entity(g,prefix+"source.warehouse"),Destination=site,SourcePort=new Code("stock"),
                DestinationPort=Raw,Worker=Entity(g,prefix+"dock.machine"),Resource=r,Ceiling=Fix.FromInt(8),Priority=100});
            var twin=f.Load(g.Save());Until(g,twin,()=>Stock(g,site,r,Raw).Stock==Fix.FromInt(8)&&
                !PermanentRemovalTests.Jobs(g).Any(x=>x.State==JobState.Open||x.State==JobState.Assigned||x.State==JobState.Paused),"actual production feed",site,Process);
            Enqueue(c,g,site,Process,"availability.batch",2);Until(g,null,()=>Current(g,site,Process).Progress.WholeTicks>0,"actual original batch work",site,Process);
            PermanentRemovalTests.Submit(c,g,new ConfigureOperationCommand{Entity=site,Operation=Process,Enabled=false,Target=Fix.One});
            var active=Current(g,site,Process);var token=active.Tools.Receipts.Single().Token;
            Cancel(c,g,site,Process,1);var tail=Current(g,site,Process);
            c.Equal(1,tail.QueueCount,"native physical tail cancellation removes one request only");
            c.Equal(active.Wip.Execution,tail.Wip.Execution,"tail preserves original physical execution");c.Equal(active.Progress,tail.Progress,"tail preserves earned progress");
            c.Equal(token,tail.Tools.Receipts.Single().Token,"tail preserves original reusable tool");c.Equal(active.Wip.Stock.AmountOf(r),tail.Wip.Stock.AmountOf(r),"tail preserves actual WIP");
            PermanentRemovalTests.Submit(c,g,new SelectResourcePortCommand{Entity=site,Port=Raw,Endpoint=Second});
            Cancel(c,g,site,Process,0);var returning=Current(g,site,Process);var actual=Stock(g,site,r,Tool).Stock;
            c.Equal(TransformationPhase.Returning,returning.Phase,"physical head cancellation retains original return");c.Equal(ResourcePortCodes.OriginalUnavailable,returning.Blocker,"original inaccessible return retains exact cause");
            c.Equal(token,returning.Tools.Receipts.Single().Token,"return wait protects actual original tool");
            twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"return context saves exact commitments");
            for(int i=0;i<4;i++)Step(g,twin);var waiting=Current(g,site,Process);
            c.Equal(returning.Progress,waiting.Progress,"return wait earns no manufacturing time");c.Equal(actual,Stock(g,site,r,Tool).Stock,"return wait changes no original stock");
            c.Equal(returning.Operating.Consumed.AmountOf(r),waiting.Operating.Consumed.AmountOf(r),"return wait pays no repeated operating input");
            c.That(!View(g,1).TryGetTransformation(site,Process,out _),"private return context withheld");
            g.Clock.Pause();twin.Clock.Pause();ulong frozen=g.Hash();for(int i=0;i<3;i++)Step(g,twin);c.Equal(frozen,g.Hash(),"single paused clock preserves return");g.Clock.Resume();twin.Clock.Resume();
            PermanentRemovalTests.Submit(c,g,new SelectResourcePortCommand{Entity=site,Port=Raw,Endpoint=First});twin=f.Load(g.Save());
            Until(g,twin,()=>Current(g,site,Process).QueueCount==0,"actual original WIP return and release",site,Process);
            var done=Current(g,site,Process);c.That(done.Wip.Stock.IsEmpty&&done.Tools.Receipts.Count==0,"physical original WIP and reusable claims settle");
            c.Equal(Fix.FromInt(8)-waiting.Operating.Consumed.AmountOf(r),Stock(g,site,r,Raw).Stock,"actual raw returns once without refunding operating consumption");
            c.Equal(Fix.Zero,Stock(g,site,r,new Code("output")).Stock,"cancelled work creates no output");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"physical cancellation state durable");c.Equal(untouched,idle.Hash(),"physical return sessions isolated");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"guarded real append/tail/return/cancel replay");
        }
        private static void Negative(Check c,string mode,int index) {
            var f=Independent(mode);var g=New(f);var idle=New(f);ulong untouched=idle.Hash();var site=Entity(g,"queue.context.emitter");
            Enqueue(c,g,site,Pulse,"pulse",3);var command=new CancelTransformationCommand{Transformer=site,Capability=Pulse,Index=index};
            ulong pure=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(command),"negative callback normal admission remains pure");c.Equal(pure,g.Hash(),"negative commit not invoked by preview");
            g.Seat(0).Submit(command);g.Update();var fault=g.Fault;
            c.That(fault!=null,"queue context mutation records actual fault "+mode+"/"+index);
            if(fault==null)throw new InvalidOperationException("missing expected queue context fault "+mode+"/"+index);
            if(mode=="cancel-throw")c.That(fault.Message.Contains("proof.original-queue-cancellation-error"),"original cancellation error retained");
            long stopped=g.Clock.StepIndex;g.Update();c.Equal(stopped,g.Clock.StepIndex,"faulted queue context cannot advance game clock");
            c.Throws<InvalidOperationException>(()=>g.Save(),"faulted partial queue cannot save healthy");c.Throws<InvalidOperationException>(()=>g.Hash(),"faulted partial queue cannot hash healthy");
            c.Throws<InvalidOperationException>(()=>View(g).PreviewCancel(EntityId.None,Pulse,0),"early missing queue query cannot continue healthy");
            c.That(ReferenceEquals(fault,g.Fault),"later calls preserve first original queue fault");c.Equal(untouched,idle.Hash(),"faulted queue context session isolated");
        }
        private static void Run(Check c) {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current queue context identity");c.Equal(154,EngineInfo.SaveFormat,"no new queue or clock state");
            Healthy(c,false);Healthy(c,true);
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,point,half);
            foreach(string mode in new[]{"cancel-pause","cancel-speed","cancel-create","cancel-throw"})foreach(int index in new[]{0,1})Negative(c,mode,index);
        }
    }
}
