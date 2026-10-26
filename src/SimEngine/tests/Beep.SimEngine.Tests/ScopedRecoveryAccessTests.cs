using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Player commands and detached facts only; authored JSON/native engine own all selection and timing.
    internal static class ScopedRecoveryAccessTests
    {
        internal const string Case="Scoped recovery access: feasible sites, source areas, reduced quantities and original claims";
        private static readonly Code Stock=new Code("a-stock"), Alias=new Code("z-stock"), Far=new Code("return");
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",9000000005),
            Second=new ResourceEndpointId("proof.pools",9000000006);
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static EntityId Find(SimGame g,string id)=>g.Seat(0).Snapshot.Entities.Single(e=>e.Def==id).Id;
        private static ResourceId Steel(SimGame g)
        {if(!g.Seat(0).View.TryFindResource("steel",out var r))throw new InvalidOperationException("missing steel");return r;}
        private static LogisticsView Logistics(SimGame g,int seat=0)=>g.Seat(seat).View.Feature<LogisticsView>();
        private static List<WorkJobView> Jobs(SimGame g)
        {var rows=new List<WorkJobView>();g.Seat(0).View.Feature<WorkView>().CopyJobs(rows);return rows;}
        private static bool Live(WorkJobView j)=>j.State==JobState.Open||j.State==JobState.Assigned||j.State==JobState.Paused;
        private static WorkJobView Job(SimGame g,int id)=>Jobs(g).Single(j=>j.Id==id);
        private static List<LogisticsDispatchOffer> Queue(SimGame g,int seat=0)
        {var rows=new List<LogisticsDispatchOffer>();Logistics(g,seat).CopyQueue(rows);return rows;}
        private static LogisticsOperationalSnapshot Facts(SimGame g,string site,Code port=default,ResourceEndpointId? endpoint=null)
        {
            var refusal=Logistics(g).ReadDiagnostics(Find(g,site),Steel(g),out var f,port,endpoint);
            if(!refusal.IsNone)throw new InvalidOperationException("access facts "+site+"/"+refusal);return f;
        }
        private static void Step(SimGame g,SimGame twin=null)
        {
            g.Update();twin?.Update();
            if(g.Fault!=null||twin?.Fault!=null||twin!=null&&g.Hash()!=twin.Hash())
                throw new InvalidOperationException("access Update "+g.Tick+"/"+g.Fault+"/"+twin?.Fault);
        }
        private static void Until(SimGame g,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<1000&&!done();i++)Step(g,twin);
            if(!done())throw new InvalidOperationException(stage+" at "+g.Tick+" jobs="+
                string.Join(";",Jobs(g).Select(j=>j.Id+"/"+j.State+"/"+j.Source+"/"+j.SourcePort+"/"+j.SourceEndpoint+"/"+j.Loaded+"/"+j.Blocker))+
                " queue="+string.Join(";",Queue(g).Select(q=>q.Kind+"/"+q.RecoveryOrigin+"/"+q.SourcePort+"/"+q.SourceEndpoint+"/"+q.Haul.Status)));
        }
        private static long Command(Check c,SimGame g,Command command)
        {
            ulong h=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(command),"ordinary access command preview");
            c.Equal(h,g.Hash(),"preview pure");var result=TestKit.Order(g,0,command);
            c.That(result.Accepted,"ordinary native command "+result.Refusal);
            if(!result.Accepted||g.Fault!=null)throw new InvalidOperationException("access command "+result.Refusal+"/"+g.Fault);return result.Arg0;
        }
        private static SimGame Save(Check c,GameFactory f,SimGame g)
        {var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"actual store/access chronology and claims save");return twin;}
        private static void Band(Check c,SimGame g,int amount)
            =>Command(c,g,new ConfigureStockPolicyCommand{Store=Find(g,"recovery.far"),Port=Far,Resource=Steel(g),
                Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,Fix.FromInt(amount),Fix.FromInt(amount),Fix.FromInt(amount),100,LogisticsReserveAccess.Protected))});
        private static HaulPreview Preview(SimGame g,string source)
            =>Logistics(g).PreviewHaul(Find(g,source),Find(g,"recovery.far"),Steel(g),Fix.FromInt(6),
                sourcePort:Stock,destinationPort:Far);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"multi-access recovery semantics");
            c.Equal(154,EngineInfo.SaveFormat,"same saved layout, no compatibility path");
            var factory=ScopedHaulGame.Factory(scopedAccess:true);
            Scenario(c,factory,false,false);Scenario(c,factory,true,false);Scenario(c,factory,false,true);
        }
        private static void Scenario(Check c,GameFactory f,bool area,bool reduced)
        {
            var g=f.NewGame(new NewGameRequest{Scenario="scoped-recovery-access",Seed=991,Seats={Controller.Human,Controller.Human}});
            var recorder=g.RecordReplay(new ReplayLimits(4*1024*1024,6000));
            c.That(!g.Modules.Contains("sim.tycoon"),"physical recovery requires no business/money module");
            c.That(!g.Seat(0).View.Feature<InventoryView>().TryGetCapacity(Find(g,"access.primary"), Code.None,out _),"all access points are named-only");
            int funding=checked((int)Command(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse"),Destination=Find(g,"access.near"),
                SourcePort=new Code("stock"),DestinationPort=Stock,Resource=Steel(g),Ceiling=Fix.FromInt(6),Worker=Find(g,"access.machine"),Priority=100}));
            Until(g,()=>!Jobs(g).Any(j=>j.Id==funding&&Live(j)),"ordinary physical shared-store funding");
            c.Equal(Fix.FromInt(6),Facts(g,"access.primary",Stock).Stock,"primary observes actual physically funded shared stock");
            c.Equal(Fix.FromInt(6),Facts(g,"access.near",Stock).Stock,"near access aliases stock, never creates another balance");
            c.Equal(Fix.FromInt(6),Facts(g,"access.tie",Stock).Stock,"third physical access shares same actual store");
            Until(g,()=>Queue(g).Any(q=>q.Kind==LogisticsDispatchKind.Evacuation),"original actual-store chronology");
            var original=Queue(g).Single(q=>q.Kind==LogisticsDispatchKind.Evacuation);
            c.Equal(1,Queue(g).Count(q=>q.Kind==LogisticsDispatchKind.Evacuation),"six physical/name aliases own one intent");
            c.Equal(First,original.SourceEndpoint,"actual scoped origin identity");
            c.Equal(0,Queue(g,1).Count,"access offers company-private");
            if(area)
                Command(c,g,new ConfigureWorkPolicyCommand{Worker=Find(g,"access.machine"),
                    Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul")},"access.without-primary-and-near",true,true,WorkAssignmentMode.All)});
            ulong h=g.Hash();var primary=Preview(g,"access.primary");var near=Preview(g,"access.near");var tie=Preview(g,"access.tie");
            c.Equal(h,g.Hash(),"all native physical previews pure");
            if(area)
            {
                c.Equal(HaulStatus.PolicyBlocked,primary.Status,"canonical source outside worker area");
                c.Equal(HaulStatus.PolicyBlocked,near.Status,"nearer source outside worker area");
                c.Equal(HaulStatus.Ready,tie.Status,"third actual access remains feasible");
            }
            else
            {
                c.Equal(HaulStatus.Ready,primary.Status,"far canonical access feasible but expensive");
                c.Equal(HaulStatus.Ready,near.Status,"near actual access feasible");
                c.Equal(HaulStatus.Ready,tie.Status,"third actual access feasible");
                c.That(near.TravelCost<primary.TravelCost,"native complete route cost beats canonical entity identity");
                c.Equal(near.TravelCost,tie.TravelCost,"two feasible accesses have exact equal native complete route cost");
                c.That(Find(g,"access.near").Value<Find(g,"access.tie").Value,"equal-cost chosen access has lower stable physical identity");
            }
            Band(c,g,reduced?3:6);
            Until(g,()=>Jobs(g).Any(j=>Live(j)&&j.SourceEndpoint==First),"fresh native multi-access evacuation");
            var job=Jobs(g).Single(Live);int id=job.Id;string selected=area?"access.tie":"access.near";
            c.Equal(Find(g,selected),job.Source,"native selector uses feasible actual physical source");
            c.Equal(Stock,job.SourcePort,"equal actual named aliases use ordinal stable name");
            c.Equal(First,job.SourceEndpoint,"selected physical source uses original actual store");
            c.Equal(Fix.FromInt(reduced?3:6),job.Amount,"policy-reduced native quantity retains selected binding");
            c.Equal(Fix.Zero,job.Loaded,"assignment never teleports pickup");
            c.Equal(Fix.FromInt(reduced?3:6),Facts(g,"access.primary",Stock).OutboundPromised,"one actual original outbound receipt");
            c.Equal(original.CreatedTick,Queue(g).Single(q=>q.Kind==LogisticsDispatchKind.Evacuation).CreatedTick,
                "worker-specific access choice never rebases actual-store age");
            Command(c,g,new SelectResourcePortCommand{Entity=Find(g,selected),Port=Stock,Endpoint = new ResourceEndpointId("proof.pools", Second.Identity)});
            c.Equal(Second,Facts(g,selected,Stock).Endpoint,"current selected access changes only future discovery");
            c.Equal(First,Job(g,id).SourceEndpoint,"acquired source remains original after current port edit");
            c.Equal(Stock,Job(g,id).SourcePort,"acquired source name retained");
            c.Equal(Fix.Zero,Facts(g,selected,Stock).OutboundPromised,"unrelated current endpoint never inherits original claim");
            var twin=Save(c,f,g);
            Until(g,()=>Job(g,id).Loaded.IsPositive,"native original named partial pickup",twin);
            c.That(Job(g,id).Loaded<Fix.FromInt(reduced?3:6),"pickup consumes positive game time");
            c.Equal(Fix.FromInt(6),Facts(g,"access.primary",Stock).Stock+Facts(g,"access.machine").Stock,"shared actual partial pickup conserved once");
            twin=Save(c,f,g);
            Until(g,()=>!Jobs(g).Any(j=>j.Id==id&&Live(j)),"actual selected source delivery/departure",twin);
            c.Equal(Fix.FromInt(reduced?3:6),Facts(g,"recovery.far",Far).Stock,"only physical earned delivery receives quantity");
            if(reduced)
            {
                c.Equal(Fix.FromInt(3),Facts(g,"access.primary",Stock).Stock,"reduced first demand retains actual residual stock");
                c.Equal(original.CreatedTick,Queue(g).Single(q=>q.Kind==LogisticsDispatchKind.Evacuation).CreatedTick,"residual actual origin age preserved");
                Band(c,g,6);
                Until(g,()=>Jobs(g).Any(j=>Live(j)&&j.SourceEndpoint==First),"normal demand change evacuates original residual");
                var next=Jobs(g).Single(Live);
                c.Equal(Fix.FromInt(3),next.Amount,"residual real three, not replayed original six");
                c.That(next.Id!=id,"residual requires distinct ordinary execution");
                c.Equal(First,next.SourceEndpoint,"residual selects exact actual source even after current binding edit");
                twin=Save(c,f,g);
                Until(g,()=>!Jobs(g).Any(Live)&&Facts(g,"access.machine").Stock.IsZero,"native residual movement/handling/departure",twin);
            }
            c.Equal(Fix.FromInt(6),Facts(g,"recovery.far",Far).Stock,"complete actual six arrive over physical time");
            c.Equal(Fix.Zero,Facts(g,"access.primary",Stock).Stock,"actual original store exhausted exactly once");
            c.Equal(Fix.Zero,Facts(g,"access.primary",Stock).OutboundPromised,"shared original outbound receipts settled");
            c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).InboundPromised,"actual destination receipt settled");
            c.Equal(Fix.Zero,Facts(g,"access.machine").Stock,"carrier empty through actual unload");
            c.Equal(Fix.FromInt(30),Facts(g,"source.warehouse").Stock+Facts(g,"access.primary",Stock).Stock+
                Facts(g,"recovery.far",Far).Stock+Facts(g,"access.machine").Stock,"original thirty conserved, alias stock never summed twice");
            c.Equal(g.Hash(),twin.Hash(),"selected access native saved continuation identical");
            c.That(new ReplayVerifier().Verify(f,recorder.Finish(),new ReplayLimits(4*1024*1024,6000)).Success,"native physical multi-access replay");
        }
    }
}
