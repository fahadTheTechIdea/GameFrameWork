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
    // JSON content and public player commands/Update/views/save/replay, never gameplay state mutations.
    internal static class NetworkSignalBindingTests
    {
        internal const string Case="Network signal bindings: independent JSON observes timed flow, backpressure and physical hauling";
        private static readonly Code Pool=new Code("pool");
        internal static void Register()=>TestRunner.Add(Case,Run);
        internal static GameFactory Create(InitializeProbe initialize,out NetworkSignalBindingsModule proof,bool independent=true,Action<JsonValue> edit=null,
            IReadOnlyList<GameModule> extraModules=null,string extraDeclarations=null,IReadOnlyDictionary<string,string> extraDocuments=null,Action<JsonValue> editScenario=null,Action<JsonValue> editMaster=null,
            Action<JsonValue> editDefinitions=null,Action<JsonValue> editFlows=null,Action<JsonValue> editTopology=null)
        {
            proof=new NetworkSignalBindingsModule(initialize);
            var document=Json.Parse(FrameworkProofContent.Read("network-signal-bindings.json"));
            if(!independent)
            {
                for(int i=0;i<document.Get("resource_signals").Count;i++)document.Get("resource_signals")[i].Get("providers")[0].Set("component",JsonValue.Of("network_signal"));
                for(int i=0;i<document.Get("entities").Count;i++)
                {var parts=document.Get("entities")[i].Get("components");var binding=parts.Get("independent_network_signal");binding.Remove("metadata_fault");parts.Remove("independent_network_signal");parts.Set("network_signal",binding);}
            }
            edit?.Invoke(document);var definitions=Json.Parse(FrameworkProofContent.Read("resource-recovery.json"));
            var physical=JsonValue.NewArray();for(int i=0;i<definitions.Get("entities").Count;i++)
                if(definitions.Get("entities")[i].Get("id").Text!="market"&&definitions.Get("entities")[i].Get("id").Text!="external")physical.Add(definitions.Get("entities")[i]);
            definitions.Set("entities",physical); // Explicit resource-only authored selection, not a supplier fallback.
            var scenario=definitions.Get("scenarios")[0];scenario.Set("modules",Json.Parse("[\"proof.network-signal-bindings\",\"sim.world-setup\",\"sim.logistics\",\"sim.construction\",\"sim.operations\",\"sim.resource-flows\"]"));
            scenario.Get("settings").Set(WorldSetupModule.ModuleId,Json.Parse("{\"profile\":\"binding.start\"}"));
            definitions.Set("uniform_tile_navigation",Json.Parse("[{\"id\":\"land\",\"space\":\"recovery.tiles\",\"cost\":10}]"));
            var topology=Json.Parse(FrameworkProofContent.Read("network-transfers.json"));var endpoints=JsonValue.NewArray();
            for(int i=0;i<topology.Get("entities").Count;i++)if(topology.Get("entities")[i].Get("components").Get("single_resource_port")==null)endpoints.Add(topology.Get("entities")[i]);
            topology.Set("entities",endpoints);
            editTopology?.Invoke(topology);
            var modules=new List<GameModule>{new ContentAccessModule(),new InventoryModule(),new OperationsModule(),new MotionModule(),new TileNavigationModule(),new WorkModule(),new LogisticsModule(),new ConstructionModule(),new InfrastructureModule(),new WorldSetupModule(),new ResourceFlowsModule(),proof};
            var master=Json.Parse(FrameworkProofContent.Read("network-signal-bindings-master.json"));
            if(extraModules!=null)foreach(var module in extraModules){modules.Add(module);scenario.Get("modules").Add(JsonValue.Of(module.Id));}
            if(extraDeclarations!=null){var declarations=Json.Parse(extraDeclarations);for(int i=0;i<declarations.Count;i++)master.Get("modules").Add(declarations[i]);}
            editScenario?.Invoke(scenario);
            editDefinitions?.Invoke(definitions);
            editMaster?.Invoke(master);
            var flows=Json.Parse(FrameworkProofContent.Read("endpoints-flows.json"));editFlows?.Invoke(flows);
            var definition=new GameDefinition("network-bindings",modules);
            var files=new PreloadedFileSource().Add("manifest.json",master.ToString())
                .Add("open-access.json",FrameworkProofContent.Access).Add("work-retry.json",FrameworkProofContent.Read("work-retry.json"))
                .Add("flows.json",flows.ToString()).Add("resource-recovery.json",definitions.ToString())
                .Add("network-transfers.json",topology.ToString()).Add("network-signal-bindings.json",document.ToString());
            if(extraDocuments!=null)foreach(var documentFile in extraDocuments)files.Add(documentFile.Key,documentFile.Value);
            return new GameFactory(definition,ContentSet.Load(files,definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {var request=new NewGameRequest{Scenario="colony",Seed=887};request.Seats.Add(Controller.Human);request.Seats.Add(Controller.Human);return factory.NewGame(request);}
        private static OperationsView View(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<OperationsView>();
        private static AutomationSignalSnapshot Read(SimGame game,string metric,string owner="binding.watcher")
        {var refusal=View(game).ReadSignal(Entity(game,owner),Pool,"binding."+metric,out var value);if(!refusal.IsNone)throw new InvalidOperationException("network signal "+refusal);return value;}
        private static Fix StockSteel(SimGame game,string owner)
        {var rows=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,owner),rows);return rows.Where(f=>f.Key==Storage.FacetStock&&f.Subject=="steel").Aggregate(Fix.Zero,(sum,row)=>sum+row.Value);}
        private static void Facts(Check c,SimGame game)
        {
            ulong hash=game.Hash();var pools=new List<NetworkPoolView>();c.Equal(Code.None,game.Seat(0).View.Feature<InfrastructureView>().CopyPools("binding.main",pools),"original pool diagnostic admitted");
            c.Equal(1,pools.Count,"one original connected pool");game.Seat(0).View.TryFindResource("steel",out var steel);
            Fix amount=pools[0].Stock.Where(x=>x.Resource==steel).Aggregate(Fix.Zero,(sum,x)=>sum+x.Amount);
            foreach(string owner in new[]{"binding.source","binding.watcher"})
            {
                foreach(string metric in new[]{"stored","available","inbound","outbound","room","pairs"})
                {
                    Fix expected=metric=="inbound"||metric=="outbound"?Fix.Zero:metric=="room"?Fix.FromInt(10)-amount:metric=="pairs"?amount/Fix.FromInt(2):amount;
                    var value=Read(game,metric,owner);c.That(value.Available&&value.Value==expected,"actual shared pool/scaled facts "+owner+"/"+metric);
                }
                c.That(!Read(game,"water",owner).Available,"incompatible resource has typed unavailable facts, no invented zero");
            }
            c.That(Read(game,"stored","binding.remote").Blocker==NetworkService.Disconnected,"remote declared connection remains physically disconnected");
            c.Equal(hash,game.Hash(),"pool/signal reads pure and never advance topology/time");
        }
        private static void Run(Check c)
        {c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current network binding identity");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current authored save format, same original network/stock codecs");c.Equal(Physical(c,false),Physical(c,true),"native/direct declarations have equal physical chronology");Invalid(c);Faults(c);}
        private static long Physical(Check c,bool independent)
        {
            var init=new InitializeProbe();var factory=Create(init,out var proof,independent);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            c.That(proof.ActualBinding==independent,"independent definition binds original network signal runtime without native inheritance");c.Equal(2,init.Initializations,"valid authored sessions Initialize once each");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"physical networks/hauling need no business module");
            c.Equal(Codes.NotOwned,View(game,1).ReadSignal(Entity(game,"binding.source"),Pool,"binding.stored",out var foreign),"visible foreign-company connected quantities refused");
            c.That(!foreign.Available,"foreign refusal publishes no private signal value");
            var opening=Read(game,"stored");c.That(opening.Available&&opening.Value==Fix.Zero,"engine-initialized authored network earns no stock before Update");
            var limits=new ReplayLimits(4*1024*1024,3000);using var recording=game.RecordReplay(limits);var twin=factory.Load(game.Save());
            for(int i=0;i<4&&!Read(game,"stored").Available;i++)Step(game,twin);
            c.That(Read(game,"stored").Available&&Read(game,"stored").Value<=Fix.FromInt(4),"first earned flow cannot exceed authored four-unit minute rate");
            for(int i=0;i<4&&Read(game,"stored").Value<Fix.FromInt(8);i++)Step(game,twin);
            c.Equal(Fix.FromInt(8),Read(game,"stored").Value.Value,"two earned rate-limited injections reach backpressure");Facts(c,game);var retained=Read(game,"stored");
            c.Equal(Fix.FromInt(22),StockSteel(game,"binding.source"),"eight units actually left source");c.Equal(Fix.Zero,StockSteel(game,"binding.watcher"),"connected signal is not local inventory");
            c.Equal(Code.None,game.Seat(0).View.Feature<InfrastructureView>().ReadGates(Entity(game,"binding.source"),"binding.main",out var gate),"actual existing gate facts");
            c.Equal(new Code("binding.backpressure"),gate.Blocker,"existing gate consumes independent connected signal");
            twin=factory.Load(game.Save());for(int i=0;i<4;i++)Step(game,twin);c.Equal(Fix.FromInt(8),Read(game,"stored").Value.Value,"backpressure retains original stock without accumulating extra flow");
            long at=game.Clock.StepIndex;ulong hash=game.Hash();game.Clock.Pause();twin.Clock.Pause();Step(game,twin);c.That(at==game.Clock.StepIndex&&hash==game.Hash(),"single paused clock freezes network/work state");game.Clock.Resume();twin.Clock.Resume();
            game.Seat(0).View.TryFindResource("steel",out var steel);Fix depot=StockSteel(game,"depot");
            Order(c,game,new HaulResourcesCommand{Source=Entity(game,"depot"),Destination=Entity(game,"binding.watcher"),Worker=Entity(game,"truck"),Resource=steel,Ceiling=Fix.FromInt(5),Priority=90});
            c.Equal(Fix.Zero,StockSteel(game,"binding.watcher"),"player hauling creates no instant delivery");twin=factory.Load(game.Save());
            int updates=0;for(;updates<240&&StockSteel(game,"binding.watcher")<Fix.FromInt(5);updates++)Step(game,twin);
            if(StockSteel(game,"binding.watcher")!=Fix.FromInt(5))throw new InvalidOperationException("physical hauling stalled step="+game.Clock.StepIndex+" depot="+StockSteel(game,"depot")+" cargo="+StockSteel(game,"truck"));
            c.That(updates>1,"movement/loading/delivery require real game time");c.Equal(depot-Fix.FromInt(5),StockSteel(game,"depot"),"five delivered units came from depot");
            c.Equal(Fix.FromInt(8),Read(game,"stored").Value.Value,"physical local delivery cannot fabricate connected pool stock");Facts(c,game);
            c.Equal(Fix.FromInt(30),StockSteel(game,"binding.source")+Read(game,"stored").Value.Value,"network flow conserves source plus pool");c.Equal(Fix.FromInt(8),retained.Value.Value,"retained signal detached from future game state");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed haul/backpressure original codecs restore");c.Equal(isolatedHash,isolated.Hash(),"interleaved idle session stays isolated");
            var replay=new ReplayVerifier().Verify(factory,recording.Finish(),limits);
            c.That(replay.Success&&replay.ActualHash==game.Hash(),"complete physical network flow and hauling replay to final live hash");
            Console.WriteLine("network signal physical replay: independent="+independent+";initial="+recording.InitialStep+";final="+game.Tick
                +";commands="+recording.CommandCount+";checkpoints="+recording.CheckpointCount+";expected-hash="+game.Hash().ToString("X16")+";actual-hash="+replay.ActualHash.ToString("X16"));return game.Clock.StepIndex;
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"missing-network","empty-network","unknown-network","unsupported-network","empty-id","extra","network-null","schema-version","bindings-null","bindings-empty","unbound"})
            {
                var init=new InitializeProbe();c.Throws<ContentException>(()=>New(Create(init,out _,edit:doc=>{
                    var unused=doc.Get("entities")[0].Clone();unused.Set("id",JsonValue.Of("binding.unused"));doc.Get("entities").Add(unused);var parts=unused.Get("components");var row=parts.Get("independent_network_signal");
                    if(fault=="missing-network")row.Remove("network");if(fault=="empty-network")row.Set("network",JsonValue.Of(""));if(fault=="unknown-network")row.Set("network",JsonValue.Of("missing"));if(fault=="unsupported-network")row.Set("network",JsonValue.Of("network.secondary"));if(fault=="empty-id")row.Set("id",JsonValue.Of(""));if(fault=="extra")row.Set("pool_id",JsonValue.Number("1"));
                    if(new[]{"network-null","schema-version","bindings-null","bindings-empty"}.Contains(fault))row.Set("metadata_fault",JsonValue.Of(fault));
                    if(fault=="unbound"){row.Remove("metadata_fault");parts.Remove("independent_network_signal");parts.Set("unbound_network_signal",row);}
                })),"unused malformed binding refused "+fault);c.Equal(0,init.Initializations,"invalid metadata before Initialize "+fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach(string fault in new[]{"live-throw","live-pause","live-touch","live-private","live-pause-throw","live-touch-throw","live-private-throw"})
            foreach(bool restored in new[]{false,true})
            {
                var factory=Create(new InitializeProbe(),out _,edit:doc=>doc.Get("entities")[0].Get("components").Get("independent_network_signal").Set("metadata_fault",JsonValue.Of(fault)));
                var game=New(factory);if(restored)game=factory.Load(game.Save());game.Update();
                if(game.Fault==null)c.Throws<InvalidOperationException>(()=>Read(game,"stored","binding.source"),"live metadata cannot publish healthy facts "+fault);
                c.That(game.Fault?.Phase=="network.signal-metadata","one network metadata owner reports callback and verification faults "+fault);
                if(fault=="live-throw"||fault.EndsWith("-throw",StringComparison.Ordinal))
                    c.That(game.Fault?.Message.Contains("proof.network-signal.network")==true,"original network callback cause survives combined mutation/throw "+fault);
                var first=game.Fault;long at=game.Tick;
                c.Throws<InvalidOperationException>(()=>Read(game,"stored","binding.source"),"faulted quantities refused "+fault);c.Throws<InvalidOperationException>(()=>game.Save(),"faulted save refused "+fault);c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted hash refused "+fault);
                c.Equal(0,game.Update(),"terminal network metadata cannot advance "+fault);
                c.That(ReferenceEquals(first,game.Fault)&&game.Tick==at,"first network error and sole clock remain authoritative "+fault);
            }
        }
    }
}
