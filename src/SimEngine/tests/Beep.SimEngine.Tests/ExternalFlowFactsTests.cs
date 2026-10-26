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
    // Authored composition, public commands/time/facts only. No provider execution or stock/rate rules here.
    internal static class ExternalFlowFactsTests
    {
        internal const string Case="Network flow contracts: external detached facts and first-fault metadata guards";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool direct,bool half=false,string fault="none")
        {
            var document=Json.Parse(FrameworkProofContent.Read("network-definition-contracts.json"));
            if(direct)document.Get("independent_networks")[0].Set("metadata_fault",JsonValue.Of(fault));
            else document.Set("independent_networks",JsonValue.NewArray());
            return NetworkSignalBindingTests.Create(new InitializeProbe(),out _,edit:root=>{if(direct)root.Remove("networks");},
                editDefinitions:root=>{if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));},
                extraModules:new GameModule[]{new NetworkDefinitionContractsModule()},
                extraDeclarations:"[{\"id\":\"proof.network-definition-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"network-definition-contracts.json\"]}]",
                extraDocuments:new Dictionary<string,string>{{"network-definition-contracts.json",document.ToString()}});
        }
        private static InfrastructureView View(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<InfrastructureView>();
        private static Fix StockSteel(SimGame game,string owner)
        {
            var facts=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,owner),facts);
            return facts.Where(x=>x.Key==Storage.FacetStock&&x.Subject=="steel").Aggregate(Fix.Zero,(sum,x)=>sum+x.Value);
        }
        private static Fix Pool(SimGame game)
        {
            var pools=new List<NetworkPoolView>();var result=View(game).CopyPools("binding.main",pools);
            if(!result.IsNone||pools.Count!=1)throw new InvalidOperationException("pool diagnostic "+result);
            game.Seat(0).View.TryFindResource("steel",out var steel);
            return pools[0].Stock.Where(x=>x.Resource==steel).Aggregate(Fix.Zero,(sum,x)=>sum+x.Amount);
        }
        private static void Reach(SimGame game,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<256&&!ready();i++)Step(game,twin);
            if(!ready())throw new InvalidOperationException(stage+" at "+game.Clock.StepIndex+" fault="+game.Fault);
        }
        private static void Same(Check c,NetworkFlowSnapshot expected,NetworkFlowSnapshot actual)
        {
            c.That(expected.Entity==actual.Entity&&expected.Resource==actual.Resource&&expected.Pull==actual.Pull,
                "external fact keeps original entity/resource/direction");
            c.That(expected.Source==actual.Source&&expected.Destination==actual.Destination,"external fact keeps actual endpoint identities");
            c.That(expected.ElapsedTicks==actual.ElapsedTicks&&expected.LastTick==actual.LastTick
                &&expected.NextAllowance==actual.NextAllowance&&expected.Blocker==actual.Blocker,"external fact keeps actual earned-time/allowance/blocker");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current external fact contract build");
            c.Equal(154,EngineInfo.SaveFormat,"no new stock/flow/work/clock codec");
            NetworkFlowSnapshot seed=null;
            foreach(bool half in new[]{false,true})foreach(bool direct in new[]{false,true})seed=Physical(c,direct,half);
            Invalid(c,seed);Faults(c,seed);
        }
        private static NetworkFlowSnapshot Physical(Check c,bool direct,bool half)
        {
            var factory=Factory(direct,half);var game=NetworkIncidentPropagationTests.New(factory);
            var isolated=NetworkIncidentPropagationTests.New(factory);ulong idle=isolated.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"actual network and hauling need no business");
            var limits=new ReplayLimits(4*1024*1024,3000);var recorder=game.RecordReplay(limits);
            var twin=factory.Load(game.Save());Step(game,twin);
            var facts=new List<NetworkFlowSnapshot>();ulong before=game.Hash();
            c.Equal(Code.None,View(game).CopyFlow("binding.main",facts),"normal native/direct flow facts");
            var first=facts.Single(x=>x.Entity==Entity(game,"binding.source"));
            c.That(first.ElapsedTicks==1&&first.LastTick==game.Clock.StepIndex&&first.NextAllowance==Fix.FromInt(half?2:4),
                "actual one-clock earned allowance and endpoint cursor");
            var copy=IndependentNetworkDef.CopyFlowFact(first);Same(c,first,copy);
            c.That(!ReferenceEquals(first,copy),"non-friend module constructs original public DTO, not live state or native inheritance");
            c.Equal(before,game.Hash(),"copy and metadata observations are pure");
            var foreign=new List<NetworkFlowSnapshot>{copy};
            c.Equal(Code.None,View(game,1).CopyFlow("binding.main",foreign),"private company flow observation admitted");
            c.Equal(0,foreign.Count,"foreign cursor identities and earned quantities do not leak");
            var missing=new List<NetworkFlowSnapshot>{copy};
            c.Equal(Codes.UnknownDef,View(game).CopyFlow("missing.network",missing),"unknown content typed refusal");
            c.Equal(0,missing.Count,"typed refusal cannot retain stale caller facts");
            Reach(game,twin,()=>Pool(game)==Fix.FromInt(8),"actual source injection to backpressure");
            c.Equal(Fix.FromInt(30),StockSteel(game,"binding.source")+Pool(game),"finite source and original pool conserve stock");
            Same(c,first,copy);long at=game.Clock.StepIndex;ulong hash=game.Hash();
            game.Clock.Pause();twin.Clock.Pause();Step(game,twin);
            c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"single paused clock freezes physical flow and cursor state");
            game.Clock.Resume();twin.Clock.Resume();twin=factory.Load(game.Save());Step(game,twin);
            game.Seat(0).View.TryFindResource("steel",out var steel);
            var command=new HaulResourcesCommand{Source=Entity(game,"depot"),Destination=Entity(game,"binding.watcher"),
                Worker=Entity(game,"truck"),Resource=steel,Ceiling=Fix.FromInt(5),Priority=90};
            before=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary physical hauling preview");
            c.Equal(before,game.Hash(),"preview grants no resources");game.Seat(0).Submit(command);Step(game);
            c.Equal(Fix.Zero,StockSteel(game,"binding.watcher"),"command grants no instant delivery");
            twin=factory.Load(game.Save());Reach(game,twin,()=>StockSteel(game,"binding.watcher")==Fix.FromInt(5),"actual hauled delivery");
            c.Equal(Fix.FromInt(45),StockSteel(game,"depot"),"physical cargo comes from actual depot");
            c.Equal(Fix.FromInt(8),Pool(game),"local delivery creates no connected stock");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"same original saved flow/work/stock state");
            c.Equal(idle,isolated.Hash(),"independent session remains unchanged");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"normal input/physical flow/haul replay");
            Same(c,first,copy);return copy;
        }
        private static void Invalid(Check c,NetworkFlowSnapshot seed)
        {
            NetworkFlowSnapshot Make(EntityId? entity=null,ResourceId? resource=null,ResourceEndpointId? source=null,
                ResourceEndpointId? destination=null,long? elapsed=null,long? last=null,Fix? next=null,Code? blocker=null)
                =>new NetworkFlowSnapshot(entity??seed.Entity,resource??seed.Resource,seed.Pull,source??seed.Source,destination??seed.Destination,
                    elapsed??seed.ElapsedTicks,last??seed.LastTick,next??seed.NextAllowance,blocker??seed.Blocker);
            c.Throws<ArgumentException>(()=>Make(entity:EntityId.None),"invalid entity refused");
            c.Throws<ArgumentException>(()=>Make(resource:ResourceId.None),"invalid resource refused");
            c.Throws<ArgumentException>(()=>Make(source:default(ResourceEndpointId)),"invalid source refused");
            c.Throws<ArgumentException>(()=>Make(destination:default(ResourceEndpointId)),"invalid destination refused");
            c.Throws<ArgumentException>(()=>Make(destination:seed.Source),"same endpoint cannot be a physical flow");
            c.Throws<ArgumentException>(()=>Make(elapsed:-1),"negative earned time refused");
            c.Throws<ArgumentException>(()=>Make(last:0),"nonpositive completed timestamp refused");
            c.Throws<ArgumentException>(()=>Make(elapsed:seed.LastTick+1),"earned time beyond sole clock timestamp refused");
            c.Throws<ArgumentException>(()=>Make(next:-Fix.One),"negative allowance refused");
            c.Throws<ArgumentException>(()=>Make(blocker:new Code("proof.blocked"),next:Fix.One),"blocked positive allowance refused");
            c.That(Make(blocker:new Code("proof.blocked"),next:Fix.Zero).NextAllowance.IsZero,"blocked zero is valid typed fact");
            c.Throws<ArgumentNullException>(()=>IndependentNetworkDef.CopyFlowFact(null),"external copier never fabricates missing facts");
        }
        private static void Faults(Check c,NetworkFlowSnapshot seed)
        {
            foreach(string fault in new[]{"live-throw","live-pause","live-touch","live-private","live-private-throw","live-pause-throw"})
            {
                var game=NetworkIncidentPropagationTests.New(Factory(true,fault:fault));Step(game);
                c.That(game.Fault==null,"actual first physical transfer precedes live diagnostic injection "+fault);
                var facts=new List<NetworkFlowSnapshot>{seed};
                c.Throws<InvalidOperationException>(()=>View(game).CopyFlow("binding.main",facts),"late Rate callback refuses "+fault);
                c.That(game.Fault!=null&&facts.Count==0,"no partial or stale publication after fault "+fault);
                if(fault=="live-private-throw"||fault=="live-pause-throw")
                    c.Equal("InvalidOperationException: proof.network-definition.original-after-mutation",game.Fault.Message,
                        "callback error retained rather than finally-mask replacement "+fault);
                var original=game.Fault;long at=game.Clock.StepIndex;
                c.Throws<InvalidOperationException>(()=>View(game,1).CopyFlow("missing.network",facts),"cached foreign/unknown read refuses first fault");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted state cannot save healthy");
                game.Update();c.That(ReferenceEquals(original,game.Fault)&&at==game.Clock.StepIndex,"same first fault and no subsequent engine update");
            }
        }
    }
}
