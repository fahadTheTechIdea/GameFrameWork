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
    // Existing actual JSON/master and public simulated player actions; no demand, stock, job or clock mirror.
    internal static class UnavailableDispatchDemandTests
    {
        internal const string Case = "Unavailable dispatch demand: active and quiet original stock intents remain visible without invented gaps";
        private static readonly ResourceEndpointId First = new ResourceEndpointId("proof.pools",9000000001), Second = new ResourceEndpointId("proof.pools",9000000002);
        private static readonly Code Alias = new Code("alias"), Far = new Code("return");
        internal static void Register() => TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool point,bool half)=>ScopedHaulGame.Factory(restoredDockDelivery:true,pointDockRecovery:point,
            editCommon:root=>{if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));});
        private static SimGame New(GameFactory f,bool point)=>f.NewGame(new NewGameRequest{Scenario=point?"scoped-point-dock-restoration":"scoped-dock-restoration",Seed=5891,Seats={Controller.Human,Controller.Human}});
        private static EntityId Find(SimGame g,string name,bool point)=>Entity(g,point?"point."+name:name);
        private static ResourceId Steel(SimGame g){if(!g.Seat(0).View.TryFindResource("steel",out var r))throw new InvalidOperationException("missing steel");return r;}
        private static LogisticsView View(SimGame g,int seat=0)=>g.Seat(seat).View.Feature<LogisticsView>();
        private static List<LogisticsDispatchOffer> Queue(SimGame g,int seat=0){var rows=new List<LogisticsDispatchOffer>();View(g,seat).CopyQueue(rows);return rows;}
        private static LogisticsDemandView Demand(SimGame g,EntityId target){var rows=new List<LogisticsDemandView>();View(g).CopyDemands(rows);return rows.Single(x=>x.Destination==target&&x.Port==Alias&&x.Endpoint==First);}
        private static LogisticsDispatchOffer Offer(SimGame g,EntityId target)=>Queue(g).Single(x=>x.Kind==LogisticsDispatchKind.Demand&&x.Destination==target&&x.DestinationPort==Alias&&x.DestinationEndpoint==First);
        private static LogisticsOperationalSnapshot Facts(SimGame g,EntityId target,Code port=default,ResourceEndpointId? original=null)
        {var code=View(g).ReadDiagnostics(target,Steel(g),out var value,port,original);if(!code.IsNone)throw new InvalidOperationException("actual stock refused "+code);return value;}
        private static void Select(Check c,SimGame g,EntityId target,ResourceEndpointId endpoint)=>PermanentRemovalTests.Submit(c,g,new SelectResourcePortCommand{Entity=target,Port=Alias,Endpoint=endpoint});
        private static void Until(SimGame g,SimGame twin,Func<bool> done,string stage)
        {
            for(int i=0;i<1000&&!done();i++)Step(g,twin);
            if(!done())throw new InvalidOperationException(stage+" exceeds1000Updates clock="+g.Clock.StepIndex+" queue="+string.Join(";",Queue(g).Select(x=>x.Kind+"/"+x.DestinationPort+"/"+x.Requested+"/"+x.Haul.Blocker)));
        }
        private static void Unknown(Check c,SimGame g,EntityId target,LogisticsDemandView original,bool replenishing)
        {
            ulong pure=g.Hash();var value=Offer(g,target);var intent=Demand(g,target);
            c.That(value.Requested==null&&value.Haul.Status==HaulStatus.PolicyBlocked&&value.Haul.Blocker==ResourcePortCodes.OriginalUnavailable,"unavailable original gap is unknown with actual typed blocker");
            c.That(!value.Haul.Carrier.IsValid,"blocked observation never fabricates selected transporter");
            c.Equal(original.Source,value.Source,"same stable source intent");c.Equal(First,value.DestinationEndpoint,"same original physical endpoint");c.Equal(Alias,value.DestinationPort,"same authored port");
            c.Equal(original.CreatedTick,value.CreatedTick,"unavailable observation preserves original age origin");c.Equal(intent.Priority,value.Priority,"actual player policy priority");
            c.Equal(replenishing,intent.Replenishing,"availability does not change original replenishment state");c.Equal(Fix.FromInt(8),intent.Quantity,"authored policy target remains separate from unknown actual gap");
            c.That(value.AgeTicks==g.Clock.StepIndex-original.CreatedTick,"age comes from the sole game clock");
            c.Equal(0,Queue(g,1).Count,"foreign company sees no private blocked endpoint or gap");
            c.Equal(pure,g.Hash(),"unknown queue observations are pure");
        }
        private static void Scenario(Check c,bool point,bool half)
        {
            var f=Factory(point,half);var g=New(f,point);var idle=New(f,point);ulong isolated=idle.Hash();
            var target=Find(g,"restore.target",point);var worker=Find(g,"dock.machine",point);var far=Find(g,"recovery.far",point);var r=Steel(g);
            var limits=new ReplayLimits(4*1024*1024,6000);using var recorder=g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=Find(g,"dock.queue",point),Held=true});
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=worker,Held=true});
            PermanentRemovalTests.Submit(c,g,new ConfigureStockPolicyCommand{Store=target,Port=Alias,Resource=r,Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,Fix.FromInt(8),Fix.FromInt(8),Fix.FromInt(8),200,LogisticsReserveAccess.Protected))});
            var original=Demand(g,target);c.That(original.Replenishing,"actual empty original starts replenishing");
            c.Equal((Fix?)Fix.FromInt(8),Offer(g,target).Requested,"ready actual stock has a known eight-unit gap");
            Select(c,g,target,Second);Unknown(c,g,target,original,true);
            c.Equal(Fix.Zero,Facts(g,far,Far,First).Stock,"original empty actual stock not fabricated by observations");
            var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"unavailable original demand and known policy persist without saved unknown state");
            var blocked=Offer(g,target);g.Clock.Pause();twin.Clock.Pause();ulong paused=g.Hash();long clock=g.Clock.StepIndex;Step(g,twin);
            c.Equal(clock,g.Clock.StepIndex,"sole paused clock keeps blocked intent age");c.Equal(paused,g.Hash(),"pause changes no demand/stock/promise");
            g.Clock.Resume();twin.Clock.Resume();for(int i=0;i<3;i++)Step(g,twin);
            Unknown(c,g,target,original,true);c.Equal(blocked.LastDispatchTick,Offer(g,target).LastDispatchTick,"blocked intent does not pretend successful dispatch");
            c.Equal(0,PermanentRemovalTests.Jobs(g).Count,"blocked offer acquires no job/worker/material receipt");
            Select(c,g,target,First);c.Equal((Fix?)Fix.FromInt(8),Offer(g,target).Requested,"restoration recomputes actual gap, not a saved estimate");
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=worker,Held=false});
            int id=checked((int)PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse",point),Destination=target,SourcePort=new Code("stock"),DestinationPort=Alias,
                Worker=worker,Resource=r,Ceiling=Fix.FromInt(8),Priority=100}).Arg0);
            c.Equal(Fix.Zero,Facts(g,far,Far,First).Stock,"known dispatch never instantly fills original stock");
            twin=f.Load(g.Save());Until(g,twin,()=>!PermanentRemovalTests.Jobs(g).Any(x=>x.Id==id),"real physical target fill and departure");
            Until(g,twin,()=>!Demand(g,target).Replenishing,"actual filled band becomes quiet");
            c.Equal(Fix.FromInt(8),Facts(g,far,Far,First).Stock,"eight actually delivered once");c.Equal(Fix.Zero,Facts(g,worker).Stock,"real cargo settles");
            c.That(!Queue(g).Any(x=>x.Destination==target&&x.DestinationEndpoint==First),"ready fulfilled quiet band is not a fake dispatch request");
            var quiet=Demand(g,target);Select(c,g,target,Second);Unknown(c,g,target,quiet,false);
            twin=f.Load(g.Save());for(int i=0;i<3;i++)Step(g,twin);Unknown(c,g,target,quiet,false);
            c.Equal(Fix.FromInt(8),Facts(g,far,Far,First).Stock,"quiet inaccessible original stock is retained, not assumed empty");
            c.Equal(Fix.Zero,Facts(g,target,Alias).Stock,"current unrelated selected endpoint stays empty");
            Select(c,g,target,First);c.That(!Queue(g).Any(x=>x.Destination==target&&x.DestinationEndpoint==First),"restored quiet fulfilled intent disappears only by actual no-gap semantics");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"settled intent/stock saves");c.Equal(isolated,idle.Hash(),"blocked demand sessions isolated");
            c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"ordinary policies/selections/physical delivery replay unknown then known gaps");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual current unavailable dispatch identity");
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})Scenario(c,point,half);
        }
    }
}

