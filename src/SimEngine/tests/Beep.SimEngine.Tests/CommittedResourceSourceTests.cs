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
    // Actual JSON composition and public simulated player requests only.
    internal static class CommittedResourceSourceTests
    {
        internal const string Case = "Committed activity sources: original policy refusal isolated from alternatives and malformed providers";
        private static readonly Code FirstPort=new Code("first"),SecondPort=new Code("second"),Control=new Code("work-access"),Closed=new Code("proof.source-policy-closed");
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",711),Second=new ResourceEndpointId("proof.pools",712);
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(string mode,bool half=false,string use="hold") {
            var root=Json.Parse(PointGame.Definitions);root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.committed-sources"));
            root.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile",JsonValue.Of("commit.start"));
            if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
            var doc=Json.Parse(FrameworkProofContent.Read("committed-work-sources.json"));var req=doc.Get("guarded_resource_sources")[0];
            req.Set("mode",JsonValue.Of(mode));req.Set("use",JsonValue.Of(use));
            req.Get("quantities").Set("supply",JsonValue.Number(use=="continuous"?"4":"1"));
            return PointGame.Factory(definitions:root.ToString(),
                extraModules:new GameModule[]{new EndpointProofModule(),new OutputPortProofModule(),new CommittedResourceSourceModule()},
                extraDeclarations:@"[
                    {""id"":""proof.pools"",""version"":""1.0.0"",""requires"":[{""id"":""sim.inventory"",""version"":""1.0.0""},{""id"":""sim.resource-flows"",""version"":""1.0.0""}],""documents"":[]},
                    {""id"":""proof.output-ports"",""version"":""1.0.0"",""requires"":[{""id"":""proof.pools"",""version"":""1.0.0""},{""id"":""sim.transformations"",""version"":""1.0.0""},{""id"":""sim.world-setup"",""version"":""1.0.0""}],""documents"":[]},
                    {""id"":""proof.committed-sources"",""version"":""1.0.0"",""requires"":[{""id"":""sim.work"",""version"":""1.0.0""},{""id"":""sim.operations"",""version"":""1.0.0""},{""id"":""proof.output-ports"",""version"":""1.0.0""}],""documents"":[""committed-work-sources.json""]}]",
                extraDocuments:new Dictionary<string,string>{{"committed-work-sources.json",doc.ToString()}});
        }
        private static SimGame New(GameFactory f)=>f.NewGame(new NewGameRequest{Scenario="points",Seed=6389,Seats={Controller.Human,Controller.Human}});
        private static WorkView Work(SimGame g,int seat=0)=>g.Seat(seat).View.Feature<WorkView>();
        private static ActivityNodeView Node(SimGame g,int id){var nodes=new List<ActivityNodeView>();if(!Work(g).TryCopyNodes(id,nodes))throw new InvalidOperationException("actual activity nodes missing");return nodes.Single();}
        private static WorkRequirementResult Criteria(SimGame g,int id){var facts=new List<WorkRequirementView>();if(!Work(g).TryCopyOrderRequirements(id,facts))throw new InvalidOperationException("actual criteria missing");return facts.Single().Result;}
        private static LogisticsOperationalSnapshot Facts(SimGame g,EntityId target,ResourceId r,Code port) {
            Code refusal=g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(target,r,out var facts,port);
            if(!refusal.IsNone)throw new InvalidOperationException("real endpoint refused "+refusal);return facts;
        }
        private static bool Complete(SimGame g,EntityId target)=>Work(g).TryReadActivityCompletion("activity:commit.activity",target,out _);
        private static void Until(SimGame g,SimGame twin,Func<bool> ready,string stage,int order=0) {
            for(int i=0;i<1000&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" exceeds1000Updates clock="+g.Clock.StepIndex+
                (order==0?"":" node="+Node(g,order).State+"/"+Node(g,order).Progress+"/"+Node(g,order).Blocker)+
                " jobs="+string.Join(";",PermanentRemovalTests.Jobs(g).Select(x=>x.Id+"/"+x.State+"/"+x.Blocker)));
        }
        private static void Gate(Check c,SimGame g,EntityId target,bool enabled)=>PermanentRemovalTests.Submit(c,g,
            new ConfigureOperationCommand{Entity=target,Operation=Control,Enabled=enabled,Target=Fix.One});
        private static void Feed(Check c,SimGame g,GameFactory f,EntityId target,ResourceId r,Code port) {
            PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand{Source=Entity(g,"source"),Destination=target,DestinationPort=port,
                Worker=Entity(g,"machine"),Resource=r,Ceiling=Fix.FromInt(8),Priority=100});
            var twin=f.Load(g.Save());Until(g,twin,()=>Facts(g,target,r,port).Stock==Fix.FromInt(8)&&
                !PermanentRemovalTests.Jobs(g).Any(x=>x.State==JobState.Open||x.State==JobState.Assigned||x.State==JobState.Paused),"physical committed source supply");
        }
        private static void Physical(Check c,string mode,string use,bool half,bool cancel) {
            var f=Factory(mode,half,use);var g=New(f);var idle=New(f);ulong untouched=idle.Hash();var target=Entity(g,"commit.target");
            if(!g.Seat(0).View.TryFindResource("supply",out var r))throw new InvalidOperationException("authored supply missing");
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=g.RecordReplay(limits);
            Feed(c,g,f,target,r,FirstPort);Feed(c,g,f,target,r,SecondPort);
            var preview=new List<WorkRequirementView>();ulong pure=g.Hash();
            c.Equal(Code.None,Work(g).PreviewOrderRequirements("commit.activity",target,preview),"ordinary independent requirement preview");
            c.Equal(2,preview.Single().Result.ResourceCandidates.Count,"uncommitted physical alternatives retained");
            c.Equal(First,preview.Single().Result.Resources.Single().Endpoint,"initial stable cost selects actual first stock");
            c.Equal(pure,g.Hash(),"initial multi-source query pure");
            int order=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="commit.activity",Target=target}).Arg0);
            var twin=f.Load(g.Save());Until(g,twin,()=>Node(g,order).Progress.WholeTicks>=3,"earned original source work",order);
            var active=Node(g,order);var original=active.Resources.Single();c.Equal(First,original.Choice.Endpoint,"original occurrence committed");
            if(use=="hold")c.Equal(First,original.Receipts.Single().Token.Endpoint,"held actual original token");
            var actual=Criteria(g,order);c.Equal(1,actual.ResourceCandidates.Count,"committed query checks original only");
            c.Equal(First,actual.ResourceCandidates.Single().Source.Endpoint,"alternative candidate cannot participate after commitment");
            Gate(c,g,target,false);
            var blocked=Node(g,order);var originalStock=Facts(g,target,r,FirstPort).Stock;
            var expected=mode=="missing"||mode=="omit"?WorkResourceCodes.MissingSource:Closed;
            c.Equal(expected,blocked.Blocker,"exact original source policy refusal retained "+mode+"/"+use);
            actual=Criteria(g,order);c.Equal(mode=="blocked"?WorkRequirementStatus.Blocked:WorkRequirementStatus.Pending,actual.Status,"original provider refusal status retained");
            c.Equal(expected,actual.Blocker,"live immutable source criteria retain exact cause");
            c.Equal(First,actual.Resources.Single().Endpoint,"blocked intent cannot substitute stocked alternate");
            c.Equal(mode=="omit"?0:1,actual.ResourceCandidates.Count,"omitted original does not probe alternative");
            twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"original blocked resources and policy durable");
            for(int i=0;i<4;i++)Step(g,twin);
            c.Equal(blocked.Progress,Node(g,order).Progress,"policy blocked original earns no time");
            c.Equal(originalStock,Facts(g,target,r,FirstPort).Stock,"policy blocked original consumes no running inputs");
            c.Equal(Fix.FromInt(8),Facts(g,target,r,SecondPort).Stock,"available alternate is neither selected nor consumed");
            c.Equal(original.Receipts.Count,Node(g,order).Resources.Single().Receipts.Count,"original receipt lifetime retained");
            if(use=="hold")c.Equal(original.Receipts.Single().Token,Node(g,order).Resources.Single().Receipts.Single().Token,"same held token retained");
            if(use=="hold")c.Equal(Fix.One,Facts(g,target,r,FirstPort).OutboundPromised,"omitted or policy-blocked original still has its actual bank promise");
            var foreign=new List<WorkRequirementView>();c.That(!Work(g,1).TryCopyOrderRequirements(order,foreign)&&foreign.Count==0,"foreign private source facts withheld");
            g.Clock.Pause();twin.Clock.Pause();pure=g.Hash();for(int i=0;i<3;i++)Step(g,twin);c.Equal(pure,g.Hash(),"paused single clock earns no source work");g.Clock.Resume();twin.Clock.Resume();
            if(cancel) {
                PermanentRemovalTests.Submit(c,g,new CancelOrderCommand{Order=order});
                c.Equal(originalStock,Facts(g,target,r,FirstPort).Stock,"cancel blocked source does not refund earned consumption");
                c.That(!Complete(g,target),"cancel blocked work emits no completion");
            } else {
                Gate(c,g,target,true);twin=f.Load(g.Save());Until(g,twin,()=>Complete(g,target),"restored original completion",order);
                c.Equal(Fix.FromInt(use=="hold"?8:4),Facts(g,target,r,FirstPort).Stock,"original real input accounting once");
            }
            c.Equal(Fix.Zero,Facts(g,target,r,FirstPort).OutboundPromised,"terminal original holds released");
            c.Equal(Fix.FromInt(8),Facts(g,target,r,SecondPort).Stock,"terminal alternate untouched");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"terminal original resource state durable");c.Equal(untouched,idle.Hash(),"independent sessions isolated");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"public physical source and policy workflow replays");
        }
        private static void Spent(Check c,bool half) {
            var f=Factory("blocked",half,"consume");var g=New(f);var idle=New(f);ulong untouched=idle.Hash();var target=Entity(g,"commit.target");
            if(!g.Seat(0).View.TryFindResource("supply",out var r))throw new InvalidOperationException("authored supply missing");
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=g.RecordReplay(limits);
            Feed(c,g,f,target,r,FirstPort);Feed(c,g,f,target,r,SecondPort);
            int order=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="commit.activity",Target=target}).Arg0);
            var twin=f.Load(g.Save());Until(g,twin,()=>Node(g,order).Resources.Single().Spent&&Node(g,order).Progress.WholeTicks>=3,"actual consumed startup input",order);
            var earned=Node(g,order).Progress;Gate(c,g,target,false);
            var actual=Criteria(g,order);c.Equal(WorkRequirementStatus.Satisfied,actual.Status,"spent input does not rerequire source policy");
            c.Equal(0,actual.ResourceCandidates.Count,"spent input probes no original or alternative stock");
            c.Equal(First,actual.Resources.Single().Endpoint,"spent accounting retains original identity");
            c.Equal(Fix.FromInt(7),Facts(g,target,r,FirstPort).Stock,"startup consumes exactly one actual original unit");
            twin=f.Load(g.Save());Step(g,twin);c.That(Node(g,order).Progress.WholeTicks>earned.WholeTicks,"source policy loss cannot revoke already spent input");
            Until(g,twin,()=>Complete(g,target),"spent original timed completion",order);
            c.Equal(Fix.FromInt(7),Facts(g,target,r,FirstPort).Stock,"startup not consumed twice on continuation");
            c.Equal(Fix.FromInt(8),Facts(g,target,r,SecondPort).Stock,"spent alternate untouched");c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"spent original accounting durable");
            c.Equal(untouched,idle.Hash(),"spent source sessions isolated");c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"spent source policy changes replay");
        }
        private static void Negative(Check c,string mode) {
            var f=Factory(mode);var g=New(f);var idle=New(f);ulong untouched=idle.Hash();var facts=new List<WorkRequirementView>();var target=Entity(g,"commit.target");
            c.Throws<InvalidOperationException>(()=>Work(g).PreviewOrderRequirements("commit.activity",target,facts),"malformed source provider faults "+mode);
            var fault=g.Fault;c.That(fault!=null,"first source provider fault retained "+mode);
            if(mode=="throw-enumerate")c.That(fault.Message.Contains("proof.original-source-enumeration-error"),"original enumeration error retained");
            if(mode=="throw-check")c.That(fault.Message.Contains("proof.original-source-check-error"),"original source check error retained");
            c.Throws<InvalidOperationException>(()=>g.Hash(),"faulted source query cannot hash healthy");
            c.Throws<InvalidOperationException>(()=>g.Save(),"faulted source query cannot save healthy");
            c.Throws<InvalidOperationException>(()=>Work(g).PreviewOrderRequirements("absent",EntityId.None,facts),"cached early query cannot continue healthy");
            c.That(ReferenceEquals(fault,g.Fault),"later refusal never replaces first source fault");c.Equal(untouched,idle.Hash(),"malformed source session isolated");
        }
        private static void Run(Check c) {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current source contract identity");c.Equal(154,EngineInfo.SaveFormat,"no duplicate source/save owner");
            foreach(string mode in new[]{"pending","blocked","missing","omit"})
                foreach(string use in new[]{"hold","continuous"})foreach(bool half in new[]{false,true})foreach(bool cancel in new[]{false,true})Physical(c,mode,use,half,cancel);
            foreach(string mode in new[]{"unavailable-candidates","duplicate","negative-cost","invalid-endpoint","throw-enumerate","touch-enumerate","clock-enumerate","invalid-check","throw-check"})Negative(c,mode);
            Spent(c,false);Spent(c,true);
        }
    }
}
