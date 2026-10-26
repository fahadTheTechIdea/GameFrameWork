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
    // Authored content, normal player commands/Update and immutable facts. No gameplay owned here.
    internal static class UtilitySignalBindingTests
    {
        internal const string Case="Utility signal bindings: direct JSON observes timed fuel, physical refuel and saved manufacture";
        private static readonly Code Utility=new Code("utility"),ProcessId=new Code("contract.process");
        internal static void Register()=>TestRunner.Add(Case,Run);
        internal static GameFactory Create(InitializeProbe initialize,out UtilitySignalBindingsModule proof,bool independent=true,Action<JsonValue> edit=null)
        {
            proof=new UtilitySignalBindingsModule();var descriptors=Json.Parse(FrameworkProofContent.Read("utility-signal-bindings.json"));
            if(!independent)foreach(string section in new[]{"signals","resource_signals"})
                for(int i=0;i<descriptors.Get(section).Count;i++)descriptors.Get(section)[i].Get("providers")[0].Set("component",JsonValue.Of("utility_signal"));
            return RecipeObservationTests.Create(new RecipeObservationModule(),batch:true,initialize:initialize,
                extraModules:new GameModule[]{new UtilitySignalsModule(),proof},
                extraDeclarations:"[{\"id\":\"sim.utility-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.utility-signal-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.utility-signals\",\"version\":\"1.0.0\"}],\"documents\":[\"utility-signal-bindings.json\"]}]",
                extraDocuments:new Dictionary<string,string>{{"utility-signal-bindings.json",descriptors.ToString()}},
                edit:(setup,recipes)=>{
                    var parts=setup.Get("entities")[1].Get("components");parts.Get("storage").Set("initial_stock",Json.Parse("{\"supply\":2}"));
                    parts.Get("transformer").Set("automatic_supply",JsonValue.Of(false));
                    parts.Set("point_dock",Json.Parse("{\"work_roles\":[\"inspect\"],\"resources\":[\"supply\"],\"slots\":[{\"id\":\"service.left\",\"kind\":\"service\",\"offset\":-1},{\"id\":\"service.right\",\"kind\":\"service\",\"offset\":1},{\"id\":\"departure.left\",\"kind\":\"departure\",\"offset\":-2},{\"id\":\"departure.right\",\"kind\":\"departure\",\"offset\":4}]}"));
                    var binding=Json.Parse("{\"id\":\"utility\",\"consumer\":\"contract.process\"}");
                    if(independent)binding.Set("metadata_fault",JsonValue.Of("none"));parts.Set(independent?"independent_utility_signal":"utility_signal",binding);
                    parts.Set("utility_signal_private_probe",Json.Parse("{}"));
                    var recipe=recipes.Get("observed_recipes")[0];
                    recipe.Set("tools",Json.Parse("{}"));recipe.Set("tool_ports",Json.Parse("{}"));recipe.Set("tool_consumption",Json.Parse("{}"));
                    recipe.Set("inputs",Json.Parse("{}"));recipe.Set("input_ports",Json.Parse("{}"));
                    recipe.Set("outputs",Json.Parse("{\"supply\":1}"));recipe.Set("output_ports",Json.Parse("{\"supply\":\"material.output\"}"));recipe.Remove("entity_output");recipe.Remove("output_home");
                    recipe.Set("operating_inputs",Json.Parse("[{\"resource\":\"supply\",\"quantity\":1,\"period\":{\"numerator\":4,\"denominator\":1,\"unit\":\"minute\"}}]"));
                    recipe.Set("operating_ports",Json.Parse("{\"supply\":\"operating.input\"}"));recipe.Set("duration",Json.Parse("{\"numerator\":32,\"denominator\":1,\"unit\":\"minute\"}"));
                    StartupContentAuthoring.Position(setup,"contract.factory",JsonValue.Number("12"));edit?.Invoke(setup);
                },worldEdit:root=>{StartupContentAuthoring.Position(root,"machine",JsonValue.Number("4"));StartupContentAuthoring.Position(root,"source",JsonValue.Number("28"));});
        }
        private static SimGame New(GameFactory factory)=>ConstructionPlanTests.NewPoints(factory);
        private static OperationsView View(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<OperationsView>();
        private static UtilityDemandSnapshot Demand(SimGame game)
        {if(!View(game).TryGetUtilityDemand(Entity(game,"contract.factory"),ProcessId,out var value))throw new InvalidOperationException("no actual utility demand");return value;}
        private static AutomationSignalSnapshot Read(SimGame game,string metric)
        {var code=View(game).ReadSignal(Entity(game,"contract.factory"),Utility,"binding.utility-"+metric,out var value);if(!code.IsNone)throw new InvalidOperationException("utility signal "+code);return value;}
        private static void Facts(Check c,SimGame game)
        {
            ulong hash=game.Hash();var demand=Demand(game);game.Seat(0).View.TryFindResource("supply",out var supply);
            foreach(var metric in new[]{"ready","requested","available","consumed"})
            {
                var signal=Read(game,metric);
                if(!demand.Drawing)c.That(!signal.Available&&signal.Blocker==UtilityCodes.Inactive,"actual inactive utility "+metric);
                else if(!demand.NextBlocker.IsNone)c.That(!signal.Available&&signal.Blocker==demand.NextBlocker,"actual utility provider blocker "+metric);
                else
                {
                    Fix expected=metric=="ready"?(demand.Satisfied?Fix.One:Fix.Zero):metric=="requested"?demand.NextInputs.AmountOf(supply):metric=="available"?demand.Available.AmountOf(supply):demand.Consumed.AmountOf(supply);
                    c.That(signal.Available&&signal.Value==expected,"signal derives actual consumer facts "+metric);
                }
            }
            c.Equal(hash,game.Hash(),"all observed utility facts pure");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current utility binding identity");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current authored save format, same utility/processor/stock codecs");
            c.That(Physical(c,false)==Physical(c,true),"native/direct metadata has equal actual physical completion chronology");Invalid(c);Faults(c);
        }
        private static long Physical(Check c,bool independent)
        {
            var init=new InitializeProbe();var factory=Create(init,out var proof,independent);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            c.That(proof.ActualBinding==independent,"actual independent/native metadata reuses original signal runtime");c.Equal(2,init.Initializations,"two valid sessions Initialize once each");
            Facts(c,game);c.That(!game.Modules.Contains(TycoonModule.ModuleId),"physical manufacturing has no business dependency");
            c.Equal(Codes.NotFound,View(game,1).ReadSignal(Entity(game,"contract.factory"),Utility,"binding.utility-consumed",out _),"foreign utility quantities private");
            Fix opening=Stock(game,"source")+Stock(game,"machine")+Stock(game,"contract.factory");var limits=new ReplayLimits(4*1024*1024,3000);using var recorder=game.RecordReplay(limits);
            Enable(c,game);Order(c,game,new EnqueueTransformationCommand{Transformer=Entity(game,"contract.factory"),Capability=ProcessId,Transformation="contract.make"});
            Until(game,null,()=>Demand(game).Drawing&&Demand(game).ElapsedTicks>=4,"engine earns timed fuel consumption");Facts(c,game);var retained=Read(game,"consumed");
            c.That(retained.Value>=Fix.One&&Stock(game,"contract.factory")<Fix.FromInt(2),"signals reflect real fuel burn, not submission");
            Order(c,game,new ConfigureOperationCommand{Entity=Entity(game,"contract.factory"),Operation=ProcessId,Enabled=false,Target=Fix.One});
            Facts(c,game);var stopped=Demand(game);var twin=factory.Load(game.Save());for(int i=0;i<4;i++)Step(game,twin);
            c.That(Demand(game).ElapsedTicks==stopped.ElapsedTicks&&Stock(game,"contract.factory")==Stock(twin,"contract.factory"),"shutdown retains original exposure/stock");
            ulong hash=game.Hash();long at=game.Clock.StepIndex;game.Clock.Pause();twin.Clock.Pause();Step(game,twin);c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"sole clock pause changes no utility facts");game.Clock.Resume();twin.Clock.Resume();
            Order(c,game,new ConfigureOperationCommand{Entity=Entity(game,"contract.factory"),Operation=ProcessId,Enabled=true,Target=Fix.One});
            Until(game,null,()=>Demand(game).Blocker==UtilityCodes.Shortage,"two actual units run out before manufacture completes");Facts(c,game);
            c.That(Read(game,"ready").Value==Fix.Zero&&Read(game,"requested").Value==Fix.One&&Read(game,"available").Value==Fix.Zero&&Read(game,"consumed").Value==Fix.FromInt(2),"shortage publishes false readiness with real demand/consumption");
            twin=factory.Load(game.Save());var shortage=Demand(game);for(int i=0;i<3;i++)Step(game,twin);
            c.That(Demand(game).ElapsedTicks==shortage.ElapsedTicks&&Process(game).QueueCount==1,"saved shortage never advances productive time or completes output");
            game.Seat(0).View.TryFindResource("supply",out var supply);Fix sourceBefore=Stock(game,"source");
            Order(c,game,new HaulResourcesCommand{Source=Entity(game,"source"),Destination=Entity(game,"contract.factory"),DestinationPort=new Code("operating.input"),Worker=Entity(game,"machine"),Resource=supply,Ceiling=Fix.FromInt(8),Priority=90});
            c.That(Stock(game,"contract.factory")==Fix.Zero&&Read(game,"ready").Value==Fix.Zero,"refuel order grants no immediate material or readiness");
            twin=factory.Load(game.Save());Until(game,twin,()=>Demand(game).Consumed.AmountOf(supply)>Fix.FromInt(2),"real pickup, timed docking and delivery resume consumption");Facts(c,game);
            c.That(Stock(game,"source")==sourceBefore-Fix.FromInt(8)&&retained.Value<Read(game,"consumed").Value,"source physically supplied eight and prior facts remain detached");
            Until(game,twin,()=>Process(game).QueueCount==0,"actual32-minute fueled manufacture completes");Facts(c,game);
            c.Equal(Fix.FromInt(8),opening+Fix.One-Stock(game,"source")-Stock(game,"machine")-Stock(game,"contract.factory"),"one earned output and eight burned units balance actual stock");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed original state restores");c.Equal(isolatedHash,isolated.Hash(),"interleaved session retains own idle inventory/utility");
            var replay=new ReplayVerifier().Verify(factory,recorder.Finish(),limits);
            c.That(replay.Success&&replay.ActualHash==game.Hash(),"complete physical refuel and manufacture replay to final live hash");
            Console.WriteLine("utility signal physical replay: independent="+independent+";initial="+recorder.InitialStep+";final="+game.Tick
                +";commands="+recorder.CommandCount+";checkpoints="+recorder.CheckpointCount+";expected-hash="+game.Hash().ToString("X16")+";actual-hash="+replay.ActualHash.ToString("X16"));return game.Clock.StepIndex;
        }
        private static void Invalid(Check c)
        {
            foreach(var fault in new[]{"missing-consumer","empty-consumer","self-consumer","wrong-consumer","consumer-none","schema-version","bindings-null","bindings-empty","unbound"})
            {
                var init=new InitializeProbe();Action create=()=>New(Create(init,out _,edit:doc=>{
                    var unused=doc.Get("entities")[1].Clone();unused.Set("id",JsonValue.Of("utility.unused"));doc.Get("entities").Add(unused);var parts=unused.Get("components");var row=parts.Get("independent_utility_signal");
                    if(fault=="missing-consumer")row.Remove("consumer");if(fault=="empty-consumer")row.Set("consumer",JsonValue.Of(""));if(fault=="self-consumer")row.Set("consumer",JsonValue.Of("utility"));if(fault=="wrong-consumer")row.Set("consumer",JsonValue.Of("material.input"));
                    if(new[]{"consumer-none","schema-version","bindings-null","bindings-empty"}.Contains(fault))row.Set("metadata_fault",JsonValue.Of(fault));
                    if(fault=="unbound"){row.Remove("metadata_fault");parts.Remove("independent_utility_signal");parts.Set("unbound_utility_signal",row);}
                }));c.Throws<ContentException>(create,"unused malformed utility binding refused "+fault);c.Equal(0,init.Initializations,"invalid binding before Initialize "+fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach(var fault in new[]{"live-throw","live-pause","live-touch","live-private","live-pause-throw","live-touch-throw","live-private-throw"})
            foreach(bool restored in new[]{false,true})
            {
                var factory=Create(new InitializeProbe(),out _,edit:doc=>doc.Get("entities")[1].Get("components").Get("independent_utility_signal").Set("metadata_fault",JsonValue.Of(fault)));
                var game=New(factory);if(restored)game=factory.Load(game.Save());
                game.Update();if(game.Fault==null)c.Throws<InvalidOperationException>(()=>Read(game,"ready"),"live metadata refused "+fault);
                c.That(game.Fault?.Phase=="utility.signal-metadata","one utility metadata owner reports callback and verification faults "+fault);
                if(fault=="live-throw"||fault.EndsWith("-throw",StringComparison.Ordinal))
                    c.That(game.Fault?.Message.Contains("proof.utility-signal.consumer")==true,"original utility callback cause survives combined mutation/throw "+fault);
                var first=game.Fault;long at=game.Tick;
                c.Throws<InvalidOperationException>(()=>Read(game,"consumed"),"faulted signals publish no healthy facts "+fault);c.Throws<InvalidOperationException>(()=>game.Save(),"faulted checkpoint refused "+fault);c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted hash refused "+fault);
                c.Equal(0,game.Update(),"terminal utility metadata cannot advance "+fault);
                c.That(ReferenceEquals(first,game.Fault)&&game.Tick==at,"first utility error and sole clock remain authoritative "+fault);
            }
        }
    }
}
