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
    // Composition and simulated player intent only; actual files/contracts own every execution rule.
    internal static class ActivitySourceAvailabilityTests
    {
        internal const string Case = "Activity resource availability: typed original source loss preserves timed work tools and alternatives";
        private static readonly Code Alias = new Code("alias"), Far = new Code("return");
        private static readonly ResourceEndpointId First = new ResourceEndpointId("proof.pools",9000000001),
            Second = new ResourceEndpointId("proof.pools",9000000002);
        internal static void Register() => TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool point,bool half) => ScopedHaulGame.Factory(restoredDockDelivery:true,pointDockRecovery:point,
            editCommon:root=>{
                if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
                root.Set("target_resource_requirements",Json.Parse(@"[
                    {""id"":""availability.work.tool"",""port"":""alias"",""use"":""hold"",""quantities"":{""steel"":1}},
                    {""id"":""availability.work.flow"",""port"":""alias"",""use"":""continuous"",""quantities"":{""steel"":4}},
                    {""id"":""availability.work.closed"",""port"":""closed"",""use"":""hold"",""quantities"":{""steel"":1}}]"));
                root.Set("work_requirement_groups",Json.Parse(@"[
                    {""id"":""availability.work.inputs"",""mode"":""all"",""entries"":[
                        {""id"":""tool"",""requirement"":""availability.work.tool""},{""id"":""flow"",""requirement"":""availability.work.flow""}]},
                    {""id"":""availability.work.any"",""mode"":""any"",""entries"":[
                        {""id"":""closed"",""requirement"":""availability.work.closed""},{""id"":""open"",""requirement"":""availability.work.tool""}]},
                    {""id"":""availability.work.all"",""mode"":""all"",""entries"":[
                        {""id"":""closed"",""requirement"":""availability.work.closed""},{""id"":""open"",""requirement"":""availability.work.tool""}]}]"));
                root.Set("activities",Json.Parse(@"[
                    {""id"":""availability.work.main"",""player_orderable"":true,""automatic_supply"":false,""priority"":70,""nodes"":[
                        {""id"":""work"",""executor"":""work.timed"",""roles"":[],""after"":[],""duration"":{""numerator"":20,""denominator"":1,""unit"":""minute""},""requirement"":""availability.work.inputs""}]},
                    {""id"":""availability.work.any-plan"",""player_orderable"":true,""automatic_supply"":false,""priority"":70,""nodes"":[
                        {""id"":""work"",""executor"":""work.timed"",""roles"":[],""after"":[],""duration"":{""numerator"":2,""denominator"":1,""unit"":""minute""},""requirement"":""availability.work.any""}]},
                    {""id"":""availability.work.all-plan"",""player_orderable"":true,""automatic_supply"":false,""priority"":70,""nodes"":[
                        {""id"":""work"",""executor"":""work.timed"",""roles"":[],""after"":[],""duration"":{""numerator"":2,""denominator"":1,""unit"":""minute""},""requirement"":""availability.work.all""}]}]"));
            },editBindings:root=>root.Get("entities")[0].Get("components").Set("physical_availability_probe",
                Json.Parse(@"{""id"":""closed"",""pool"":""source.first"",""mode"":""unavailable""}")));
        private static SimGame New(GameFactory f,bool point) => f.NewGame(new NewGameRequest{
            Scenario=point?"scoped-point-dock-restoration":"scoped-dock-restoration",Seed=6203,Seats={Controller.Human,Controller.Human}});
        private static EntityId Find(SimGame g,string name,bool point)=>Entity(g,point?"point."+name:name);
        private static WorkView Work(SimGame g,int seat=0)=>g.Seat(seat).View.Feature<WorkView>();
        private static ActivityNodeView Node(SimGame g,int order) {var nodes=new List<ActivityNodeView>();if(!Work(g).TryCopyNodes(order,nodes))throw new InvalidOperationException("actual order nodes missing");return nodes.Single();}
        private static List<WorkRequirementView> Requirements(SimGame g,int order){var facts=new List<WorkRequirementView>();if(!Work(g).TryCopyOrderRequirements(order,facts))throw new InvalidOperationException("actual order criteria missing");return facts;}
        private static bool Complete(SimGame g,string plan,EntityId target)=>Work(g).TryReadActivityCompletion("activity:"+plan,target,out _);
        private static LogisticsOperationalSnapshot Stock(SimGame g,EntityId far,ResourceId r) {
            Code refusal=g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(far,r,out var facts,Far,First);
            if(!refusal.IsNone)throw new InvalidOperationException("actual retained original stock refused "+refusal);return facts;
        }
        private static void Until(SimGame g,SimGame twin,Func<bool> ready,string stage,int order=0) {
            for(int i=0;i<1000&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" exceeds1000Updates clock="+g.Clock.StepIndex+
                (order==0?"":" node="+Node(g,order).State+"/"+Node(g,order).Progress+"/"+Node(g,order).Blocker)+
                " jobs="+string.Join(";",PermanentRemovalTests.Jobs(g).Select(x=>x.Id+"/"+x.State+"/"+x.Blocker)));
        }
        private static void Physical(Check c,bool point,bool half,bool cancel) {
            var f=Factory(point,half);var g=New(f,point);var idle=New(f,point);ulong untouched=idle.Hash();
            var target=Find(g,"restore.target",point);var far=Find(g,"recovery.far",point);
            if(!g.Seat(0).View.TryFindResource("steel",out var r))throw new InvalidOperationException("authored steel missing");
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=Find(g,"dock.queue",point),Held=true});
            PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse",point),Destination=target,
                SourcePort=new Code("stock"),DestinationPort=Alias,Worker=Find(g,"dock.machine",point),Resource=r,Ceiling=Fix.FromInt(8),Priority=100});
            var twin=f.Load(g.Save());Until(g,twin,()=>Stock(g,far,r).Stock==Fix.FromInt(8)&&
                !PermanentRemovalTests.Jobs(g).Any(x=>x.State==JobState.Open||x.State==JobState.Assigned||x.State==JobState.Paused),"physical activity feed");
            var facts=new List<WorkRequirementView>();ulong pure=g.Hash();
            c.Equal(Code.None,Work(g).PreviewOrderRequirements("availability.work.any-plan",target,facts),"engine previews any expression");
            c.Equal("open",facts.Last().Result.SelectedEntry,"any selects actual eligible branch instead of closed provider");
            var closed=facts.Single(x=>x.Requirement=="availability.work.closed").Result;
            c.Equal(PhysicalAvailabilityProbe.Closed,closed.Blocker,"independent physical provider cause reaches leaf");
            c.That(!closed.Quantity.HasValue&&closed.ResourceCandidates.Count==0&&closed.Resources.Count==0,"unavailable source invents no stock candidate or quantity");
            c.Equal(Code.None,Work(g).PreviewOrderRequirements("availability.work.all-plan",target,facts),"engine previews all expression");
            c.Equal(PhysicalAvailabilityProbe.Closed,facts.Last().Result.Blocker,"all retains actual unavailable cause");
            c.Equal(Codes.NotOwned,Work(g,1).PreviewOrderRequirements("availability.work.all-plan",target,facts),"foreign private criteria refused");
            c.Equal(0,facts.Count,"foreign criteria disclose no cause or endpoint");c.Equal(pure,g.Hash(),"all source previews are pure");
            int refused=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="availability.work.all-plan",Target=target}).Arg0);
            c.Equal(PhysicalAvailabilityProbe.Closed,Node(g,refused).Blocker,"posted all work waits on same physical cause");
            c.Equal(0,Node(g,refused).Resources.Count,"failed whole set acquires no partial tool");
            PermanentRemovalTests.Submit(c,g,new CancelOrderCommand{Order=refused});
            int any=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="availability.work.any-plan",Target=target}).Arg0);
            c.Equal("open",Node(g,any).Choices.Single().Entry,"normal execution commits eligible any branch");
            twin=f.Load(g.Save());Until(g,twin,()=>Complete(g,"availability.work.any-plan",target),"real any branch duration",any);
            c.Equal(Fix.FromInt(8),Stock(g,far,r).Stock,"temporary any tool never consumes stock");
            int order=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="availability.work.main",Target=target}).Arg0);
            twin=f.Load(g.Save());Until(g,twin,()=>Node(g,order).Progress.WholeTicks>=3,"actual held and continuous work",order);
            var active=Node(g,order);var token=active.Resources.Single(x=>x.Choice.Use==WorkResourceUse.Hold).Receipts.Single().Token;
            c.Equal(First,token.Endpoint,"actual reusable tool remains original endpoint");
            PermanentRemovalTests.Submit(c,g,new SelectResourcePortCommand{Entity=target,Port=Alias,Endpoint=Second});
            var blocked=Node(g,order);var actualStock=Stock(g,far,r).Stock;
            c.Equal(ResourcePortCodes.OriginalUnavailable,blocked.Blocker,"activity retains original physical loss, not generic lost claim");
            facts=Requirements(g,order);
            foreach(var leaf in facts.Where(x=>x.Requirement=="availability.work.tool"||x.Requirement=="availability.work.flow")) {
                c.Equal(ResourcePortCodes.OriginalUnavailable,leaf.Result.Blocker,"held and running leaf retain exact loss");
                c.That(!leaf.Result.Quantity.HasValue&&leaf.Result.ResourceCandidates.Count==0,"unavailable original has no invented measurement");
                c.Equal(First,leaf.Result.Resources.Single().Endpoint,"committed intent retains original identity");
            }
            c.Equal(ResourcePortCodes.OriginalUnavailable,facts.Last().Result.Blocker,"all root uses same physical cause");
            c.Equal(token,blocked.Resources.Single(x=>x.Choice.Use==WorkResourceUse.Hold).Receipts.Single().Token,"original tool receipt retained");
            twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"blocked activity restores exact receipts and progress");
            for(int i=0;i<4;i++)Step(g,twin);
            c.Equal(blocked.Progress,Node(g,order).Progress,"unavailable work earns no duration");
            c.Equal(actualStock,Stock(g,far,r).Stock,"unavailable work consumes no running input");
            var foreign=new List<ActivityNodeView>();c.That(!Work(g,1).TryCopyNodes(order,foreign)&&foreign.Count==0,"foreign live work has no private source facts");
            g.Clock.Pause();twin.Clock.Pause();ulong paused=g.Hash();for(int i=0;i<3;i++)Step(g,twin);
            c.Equal(paused,g.Hash(),"one paused game clock advances no work");g.Clock.Resume();twin.Clock.Resume();
            if(cancel) {
                PermanentRemovalTests.Submit(c,g,new CancelOrderCommand{Order=order});twin=f.Load(g.Save());
                c.Equal(actualStock,Stock(g,far,r).Stock,"cancellation while unavailable preserves already consumed accounting");
                c.Equal(Fix.Zero,Stock(g,far,r).OutboundPromised,"cancellation releases original independent tool claim");
                c.That(!Complete(g,"availability.work.main",target),"cancelled work creates no completion");
            } else {
                PermanentRemovalTests.Submit(c,g,new SelectResourcePortCommand{Entity=target,Port=Alias,Endpoint=First});twin=f.Load(g.Save());
                Until(g,twin,()=>Complete(g,"availability.work.main",target),"same original restoration and timed completion",order);
                c.Equal(Fix.FromInt(4),Stock(g,far,r).Stock,"four actual continuous inputs consumed once");
                c.Equal(Fix.Zero,Stock(g,far,r).OutboundPromised,"completed reusable tool releases original receipt");
            }
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"settled activity state durable");c.Equal(untouched,idle.Hash(),"interleaved session isolated");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"physical feed availability changes and activity outcome replay");
        }
        private static void Run(Check c) {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current engine identity");c.Equal(154,EngineInfo.SaveFormat,"availability adds no save owner");
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})foreach(bool cancel in new[]{false,true})Physical(c,point,half,cancel);
        }
    }
}
