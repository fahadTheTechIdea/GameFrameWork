using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Existing authored files and public simulated player input; no production rules in driver.
    internal static class ReturnExecutionPreviewTests
    {
        internal const string Case = "Return execution preview: original cancellation blocker and no productive ETA while disabled";
        private static readonly Code Process=new Code("process"),Raw=new Code("raw"),Tool=new Code("tool");
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",9000000001),Second=new ResourceEndpointId("proof.pools",9000000002);
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static SimGame New(GameFactory f,bool point)=>f.NewGame(new NewGameRequest{
            Scenario=point?"scoped-point-dock-restoration":"scoped-dock-restoration",Seed=7001,Seats={Controller.Human,Controller.Human}});
        private static TransformationManagementView View(SimGame g,int seat=0)=>g.Seat(seat).View.Feature<TransformationManagementView>();
        private static TransformationExecutionPreview Read(Check c,SimGame g,EntityId site) {
            ulong hash=g.Hash();c.Equal(Code.None,View(g).ReadExecutionPreview(site,Process,"availability.batch",out var facts),"actual engine execution projection");
            c.Equal(hash,g.Hash(),"preview changes no state RNG work or demand");if(facts==null)throw new InvalidOperationException("actual execution projection missing");return facts;
        }
        private static TransformationSnapshot Current(SimGame g,EntityId site) {
            if(!View(g).TryGetTransformation(site,Process,out var p))throw new InvalidOperationException("actual process missing");return p;
        }
        private static LogisticsOperationalSnapshot Stock(SimGame g,EntityId site,ResourceId r,Code port) {
            Code refusal=g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(site,r,out var facts,port);
            if(!refusal.IsNone)throw new InvalidOperationException("actual port refused "+refusal);return facts;
        }
        private static void Until(SimGame g,SimGame twin,Func<bool> ready,string stage,EntityId site) {
            for(int i=0;i<1000&&!ready();i++)Step(g,twin);
            if(!ready()){var p=Current(g,site);throw new InvalidOperationException(stage+" exceeds1000Updates clock="+g.Clock.StepIndex+" process="+p.Phase+"/"+p.Blocker+"/"+p.Progress);}
        }
        private static void Returning(Check c,TransformationExecutionPreview p) {
            c.Equal(TransformationPhase.Returning,p.Current.Phase,"actual snapshot is physical return");
            c.Equal(ProductiveEstimateStatus.Returning,p.EstimateStatus,"return is distinct from manufacturing blocked/continuous work");
            c.Equal((Code?)ResourcePortCodes.OriginalUnavailable,p.CurrentBlocker,"actual return cause overrides disabled production");
            c.That(!p.RemainingProductiveTime.HasValue&&!p.ProductiveCompletionStep.HasValue,"return has no invented productive ETA");
            c.That(!p.Control.Enabled,"production remains disabled while original return is pending");
        }
        internal static void Physical(Check c,bool point,bool half) {
            var f=ResourcePortAvailabilityTests.Factory(point,half);var g=New(f,point);var idle=New(f,point);ulong untouched=idle.Hash();
            var site=Entity(g,"availability.factory");string prefix=point?"point.":"";
            if(!g.Seat(0).View.TryFindResource("steel",out var r))throw new InvalidOperationException("authored steel missing");
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=g.RecordReplay(limits);
            var initial=Read(c,g,site);c.Equal(ProductiveEstimateStatus.NotHead,initial.EstimateStatus,"unstarted recipe has no current execution");
            c.That(initial.CurrentBlocker==null&&!initial.ProductiveCompletionStep.HasValue,"unstarted recipe has no fabricated blocker/ETA");
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=Entity(g,prefix+"dock.queue"),Held=true});
            PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand{Source=Entity(g,prefix+"source.warehouse"),Destination=site,SourcePort=new Code("stock"),
                DestinationPort=Raw,Worker=Entity(g,prefix+"dock.machine"),Resource=r,Ceiling=Fix.FromInt(8),Priority=100});
            var twin=f.Load(g.Save());Until(g,twin,()=>Stock(g,site,r,Raw).Stock==Fix.FromInt(8)&&
                !PermanentRemovalTests.Jobs(g).Any(x=>x.State==JobState.Open||x.State==JobState.Assigned||x.State==JobState.Paused),"actual preview input feed",site);
            PermanentRemovalTests.Submit(c,g,new EnqueueTransformationCommand{Transformer=site,Capability=Process,Transformation="availability.batch"});
            Until(g,null,()=>Current(g,site).Phase==TransformationPhase.Working&&Current(g,site).Progress.WholeTicks>0,"actual timed productive preview",site);
            var working=Read(c,g,site);long remaining=working.Current.Required-working.Current.Progress.WholeTicks;
            c.Equal(ProductiveEstimateStatus.Conditional,working.EstimateStatus,"normal working ETA semantics retained");
            c.Equal((long?)(g.Clock.StepIndex+remaining),working.ProductiveCompletionStep,"ETA comes from actual original remaining work");
            c.Equal(remaining,g.Clock.Time.Ticks(working.RemainingProductiveTime.Value),"productive remaining time uses sole authored quantum");
            PermanentRemovalTests.Submit(c,g,new ConfigureOperationCommand{Entity=site,Operation=Process,Enabled=false,Target=Fix.One});
            var disabled=Read(c,g,site);c.Equal(ProductiveEstimateStatus.Blocked,disabled.EstimateStatus,"disabled production before cancellation remains blocked");
            c.Equal((Code?)OperationCodes.Disabled,disabled.CurrentBlocker,"disabled ordinary production cause retained");
            PermanentRemovalTests.Submit(c,g,new SelectResourcePortCommand{Entity=site,Port=Raw,Endpoint=Second});
            PermanentRemovalTests.Submit(c,g,new CancelTransformationCommand{Transformer=site,Capability=Process,Index=0});
            var returning=Read(c,g,site);Returning(c,returning);
            var token=returning.Current.Tools.Receipts.Single().Token;Fix actualStock=Stock(g,site,r,Tool).Stock;
            c.Equal(First,token.Endpoint,"actual original reusable tool retained");
            ulong pure=g.Hash();c.Equal(Codes.NotFound,View(g,1).ReadExecutionPreview(site,Process,"availability.batch",out var foreign),"private return projection refused");
            c.That(foreign==null,"foreign return query publishes no prefix");c.Equal(pure,g.Hash(),"private refusal is pure");
            twin=f.Load(g.Save());Returning(c,Read(c,twin,site));c.Equal(g.Hash(),twin.Hash(),"return projection and actual state durable");
            for(int i=0;i<4;i++)Step(g,twin);Returning(c,Read(c,g,site));
            c.Equal(returning.Current.Progress,Current(g,site).Progress,"return preview earns no productive progress");
            c.Equal(actualStock,Stock(g,site,r,Tool).Stock,"return preview/wait consumes no material");
            g.Clock.Pause();twin.Clock.Pause();pure=g.Hash();var paused=Read(c,g,site);Returning(c,paused);c.That(paused.ClockPaused,"return reports actual sole clock pause");
            for(int i=0;i<3;i++)Step(g,twin);c.Equal(pure,g.Hash(),"paused return preview mutates no clock or state");g.Clock.Resume();twin.Clock.Resume();
            PermanentRemovalTests.Submit(c,g,new SelectResourcePortCommand{Entity=site,Port=Raw,Endpoint=First});twin=f.Load(g.Save());
            Until(g,twin,()=>Current(g,site).QueueCount==0,"restored original physical return",site);
            var done=Read(c,g,site);c.Equal(ProductiveEstimateStatus.NotHead,done.EstimateStatus,"settled return no longer pretends productive head");
            c.That(done.CurrentBlocker==null&&!done.ProductiveCompletionStep.HasValue,"settled preview has no stale blocker or ETA");
            c.Equal(Fix.FromInt(8)-returning.Current.Operating.Consumed.AmountOf(r),Stock(g,site,r,Raw).Stock,"original WIP returns once without refunding spent operating inputs");
            c.Equal(Fix.Zero,Stock(g,site,r,new Code("output")).Stock,"cancelled work creates no result");
            Returning(c,returning);c.Equal(token,returning.Current.Tools.Receipts.Single().Token,"retained return projection is detached after release");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"settled projection durable");c.Equal(untouched,idle.Hash(),"preview sessions isolated");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"actual time supply disabled original return and restoration replay");
        }
        private static void Run(Check c) {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current return projection identity");c.Equal(154,EngineInfo.SaveFormat,"no saved preview clock or phase duplicate");
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,point,half);
        }
    }
}
