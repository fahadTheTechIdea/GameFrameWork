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
    // Authored input and public player commands/clock/views only; no reserve/flow decisions here.
    internal static class NetworkGateBindingTests
    {
        internal const string Case="Network gate bindings: independent JSON reserve policy waits for physical resupply and resumes earned flow";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Create(InitializeProbe initialize,out NetworkGateBindingsModule proof,bool independent=true,Action<JsonValue> edit=null)
        {
            proof=new NetworkGateBindingsModule();var document=Json.Parse(FrameworkProofContent.Read("network-gate-bindings.json"));
            if(!independent){var parts=document.Get("entities")[0].Get("components");var gate=parts.Get("independent_network_gate");gate.Remove("metadata_fault");parts.Remove("independent_network_gate");parts.Set("signal_network_gate",gate);}
            edit?.Invoke(document);
            return NetworkSignalBindingTests.Create(initialize,out _,edit:root=>{
                var entities=JsonValue.NewArray();for(int i=1;i<root.Get("entities").Count;i++)entities.Add(root.Get("entities")[i]);root.Set("entities",entities);
                root.Get("networks")[0].Set("capacity_policy",JsonValue.Of("gate.pool"));
            },extraModules:new GameModule[]{new InventorySignalsModule(),proof},
                extraDeclarations:"[{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.network-gate-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"network-gate-bindings.json\"]}]",
                extraDocuments:new Dictionary<string,string>{{"network-gate-bindings.json",document.ToString()}});
        }
        private static SimGame New(GameFactory factory)
        {var request=new NewGameRequest{Scenario="colony",Seed=907};request.Seats.Add(Controller.Human);request.Seats.Add(Controller.Human);return factory.NewGame(request);}
        private static InfrastructureView View(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<InfrastructureView>();
        private static ExecutionGateEvaluation Read(SimGame game,string network="binding.main")
        {var refusal=View(game).ReadGates(Entity(game,"binding.source"),network,out var value);if(!refusal.IsNone)throw new InvalidOperationException("gate view refused "+refusal);return value;}
        private static Fix StockSteel(SimGame game,string owner)
        {var rows=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,owner),rows);return rows.Where(f=>f.Key==Storage.FacetStock&&f.Subject=="steel").Aggregate(Fix.Zero,(sum,row)=>sum+row.Value);}
        private static Fix Pool(SimGame game)
        {var refusal=game.Seat(0).View.Feature<OperationsView>().ReadSignal(Entity(game,"binding.watcher"),new Code("pool"),"binding.stored",out var value);if(!refusal.IsNone||!value.Available)throw new InvalidOperationException("actual pool unavailable "+refusal+"/"+value.Blocker);return value.Value.Value;}
        private static void Facts(Check c,SimGame game,bool ready)
        {
            ulong hash=game.Hash();var facts=Read(game);c.Equal(1,facts.Gates.Count,"one original gate runtime");var gate=facts.Gates[0];c.That(gate.Applies&&gate.Capability==new Code("safety")&&gate.Conditions.Count==1,"actual declared gate/network/condition identities");
            var condition=gate.Conditions[0];c.That(condition.Id=="reserve"&&condition.Source==new Code("supply")&&condition.Signal.Signal=="gate.supply"&&condition.Signal.Available,"original condition reports actual inventory source");
            c.Equal(StockSteel(game,"binding.source"),condition.Signal.Value.Value,"gate facts derive actual source availability, never a readiness cache");c.That(condition.Ready==ready&&facts.Blocker==(ready?Code.None:new Code("gate.reserve")),"authored reserve policy interpreted by original evaluator");
            var other=Read(game,"network.secondary");c.That(other.Blocker.IsNone&&other.Gates.Count==1&&!other.Gates[0].Applies&&other.Gates[0].Conditions.Count==0,"non-gated declared connection has no substituted conditions/blocker");
            c.Equal(hash,game.Hash(),"gate views pure");
        }
        private static void Run(Check c)
        {c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current gate contract identity");c.Equal(154,EngineInfo.SaveFormat,"same original gate/flow/stock state schemas");c.Equal(Physical(c,false),Physical(c,true),"native/direct actual resupply/resume chronology identical");Invalid(c);Faults(c);}
        private static long Physical(Check c,bool independent)
        {
            var init=new InitializeProbe();var factory=Create(init,out var proof,independent);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            c.That(proof.ActualBinding==independent,"direct focused contract uses original gate runtime without native inheritance");c.Equal(2,init.Initializations,"actual authored sessions Initialize once each");c.That(!game.Modules.Contains(TycoonModule.ModuleId),"gate policy/physical supply need no business");Facts(c,game,true);
            c.Equal(Codes.NotFound,View(game,1).ReadGates(Entity(game,"binding.source"),"binding.main",out var foreign),"foreign gate source view refused");c.That(foreign==null,"refusal exposes no private gate facts");
            var limits=new ReplayLimits(4*1024*1024,3000);var recording=game.RecordReplay(limits);var twin=factory.Load(game.Save());Step(game,twin);
            c.Equal(Fix.FromInt(4),Pool(game),"one game minute earns only four units");c.Equal(Fix.FromInt(26),StockSteel(game,"binding.source"),"actual source supplies first injection");Facts(c,game,true);Step(game,twin);
            c.Equal(Fix.FromInt(8),Pool(game),"second allowed physical injection");c.Equal(Fix.FromInt(22),StockSteel(game,"binding.source"),"actual reserve threshold closes after earned withdrawal");Facts(c,game,false);var retained=Read(game);
            twin=factory.Load(game.Save());for(int i=0;i<4;i++)Step(game,twin);c.Equal(Fix.FromInt(8),Pool(game),"closed gate never accrues catch-up stock or flow");
            ulong hash=game.Hash();long at=game.Clock.StepIndex;game.Clock.Pause();twin.Clock.Pause();Step(game,twin);c.That(hash==game.Hash()&&at==game.Clock.StepIndex,"one paused clock freezes actual gate/flow/haul owners");game.Clock.Resume();twin.Clock.Resume();
            game.Seat(0).View.TryFindResource("steel",out var steel);Order(c,game,new HaulResourcesCommand{Source=Entity(game,"depot"),Destination=Entity(game,"binding.source"),Worker=Entity(game,"truck"),Resource=steel,Ceiling=Fix.FromInt(5),Priority=90});
            Facts(c,game,false);c.Equal(Fix.FromInt(22),StockSteel(game,"binding.source"),"promise/submission cannot reopen source reserve");c.Equal(Fix.FromInt(8),Pool(game),"resupply command grants no instant network injection");
            twin=factory.Load(game.Save());int updates=0;
            for(;updates<240&&(StockSteel(game,"depot")!=Fix.FromInt(45)||StockSteel(game,"truck")!=Fix.Zero||Pool(game)!=Fix.FromInt(12)||StockSteel(game,"binding.source")!=Fix.FromInt(23));updates++)Step(game,twin);
            if(Pool(game)!=Fix.FromInt(12)||StockSteel(game,"binding.source")!=Fix.FromInt(23)||StockSteel(game,"truck")!=Fix.Zero)throw new InvalidOperationException("physical reserve resupply stalled step="+game.Clock.StepIndex+" pool="+Pool(game)+" source="+StockSteel(game,"binding.source")+" cargo="+StockSteel(game,"truck"));
            c.That(updates>1,"actual travel/load/delivery earns reopening over time");Facts(c,game,false);
            c.Equal(Fix.FromInt(35),Pool(game)+StockSteel(game,"binding.source"),"actual initial thirty plus five resupplied balance");
            for(int i=0;i<4;i++)Step(game,twin);c.Equal(Fix.FromInt(12),Pool(game),"resumed injection obeys four-unit rate, no accumulated blocked credit");
            c.Equal(Fix.FromInt(22),retained.Gates[0].Conditions[0].Signal.Value.Value,"saved/refueled current state never mutates retained gate facts");c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed saved physical gate state restores");c.Equal(isolatedHash,isolated.Hash(),"interleaved untouched session isolated");
            c.That(new ReplayVerifier().Verify(factory,recording.Finish(),limits).Success,"normal commands/clock/physical gate resupply replay");return game.Clock.StepIndex;
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"missing-id","empty-id","missing-networks","empty-network-id","empty-networks","duplicate-networks","unknown-network","unsupported-network","missing-conditions","empty-conditions","duplicate-conditions","missing-source","empty-source","unknown-signal","empty-blocker","empty-unit","range","extra","networks-null","networks-empty","networks-duplicate","conditions-null","conditions-empty","conditions-duplicate","unbound","wrong-codec"})
            {
                var init=new InitializeProbe();c.Throws<ContentException>(()=>New(Create(init,out _,edit:doc=>{
                    var unused=doc.Get("entities")[0].Clone();unused.Set("id",JsonValue.Of("gate.unused"));doc.Get("entities").Add(unused);var parts=unused.Get("components");var row=parts.Get("independent_network_gate");var condition=row.Get("conditions")[0];
                    if(fault=="missing-id")row.Remove("id");if(fault=="empty-id")row.Set("id",JsonValue.Of(""));if(fault=="missing-networks")row.Remove("networks");if(fault=="empty-network-id")row.Set("networks",Json.Parse("[\"\"]"));if(fault=="empty-networks")row.Set("networks",Json.Parse("[]"));if(fault=="duplicate-networks")row.Set("networks",Json.Parse("[\"binding.main\",\"binding.main\"]"));if(fault=="unknown-network")row.Set("networks",Json.Parse("[\"missing\"]"));if(fault=="unsupported-network")row.Set("networks",Json.Parse("[\"network.public\"]"));
                    if(fault=="missing-conditions")row.Remove("conditions");if(fault=="empty-conditions")row.Set("conditions",Json.Parse("[]"));if(fault=="duplicate-conditions")row.Get("conditions").Add(condition.Clone());if(fault=="missing-source")condition.Remove("source");if(fault=="empty-source")condition.Set("source",JsonValue.Of(""));if(fault=="unknown-signal")condition.Set("signal",JsonValue.Of("missing"));if(fault=="empty-blocker")condition.Set("blocker",JsonValue.Of(""));if(fault=="empty-unit")condition.Set("unit",JsonValue.Of(""));if(fault=="range")condition.Set("maximum",JsonValue.Number("20"));if(fault=="extra")row.Set("balance",JsonValue.Number("1"));
                    if(new[]{"networks-null","networks-empty","networks-duplicate","conditions-null","conditions-empty","conditions-duplicate"}.Contains(fault))row.Set("metadata_fault",JsonValue.Of(fault));
                    if(fault=="unbound"||fault=="wrong-codec"){row.Remove("metadata_fault");parts.Remove("independent_network_gate");parts.Set(fault=="unbound"?"unbound_network_gate":"wrong_codec_network_gate",row);}
                })),"unused malformed network gate refuses "+fault);c.Equal(0,init.Initializations,"malformed gate refused before Initialize "+fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach(string fault in new[]{"live-throw","live-pause","live-touch","live-private"})
            {
                var game=New(Create(new InitializeProbe(),out _,edit:doc=>doc.Get("entities")[0].Get("components").Get("independent_network_gate").Set("metadata_fault",JsonValue.Of(fault))));game.Update();if(game.Fault==null)c.Throws<InvalidOperationException>(()=>Read(game),"live metadata cannot publish healthy gate "+fault);
                c.That(game.Fault?.Phase==(fault=="live-throw"?"observation.provider":"network.gate-metadata"),"actual getter codec/clock first fault "+fault);c.Throws<InvalidOperationException>(()=>Read(game),"faulted gate facts refuse "+fault);c.Throws<InvalidOperationException>(()=>game.Save(),"faulted checkpoint refuses "+fault);c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted hash refuses "+fault);
            }
        }
    }
}
