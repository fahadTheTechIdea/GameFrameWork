using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.ControlContentContractTests;

namespace Beep.SimEngine.Tests
{
    // JSON authoring and normal public player input only; no test-owned stock/signal/work progression.
    internal static class InventorySignalBindingTests
    {
        internal const string Case = "Inventory signal bindings: independent JSON facts follow physical stock and saved recovery";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Build(InitializeProbe initialize, out InventorySignalBindingsModule proof,
            bool independent = true, bool numericOnly = false, Action<JsonValue> edit = null)
        {
            var descriptors = Json.Parse(FrameworkProofContent.Read("inventory-signal-bindings.json"));
            if (!independent)
                foreach (string section in new[] { "signals", "resource_signals" })
                    for (int i=0;i<descriptors.Get(section).Count;i++)
                        descriptors.Get(section)[i].Get("providers")[0].Set("component",JsonValue.Of("inventory_signal"));
            proof = new InventorySignalBindingsModule(initialize);
            return Factory(initialize,out _,extension:proof,
                contentDocuments:new Dictionary<string,string>{{"inventory-signal-bindings.json",descriptors.ToString()}},
                conditionsEdit:doc => {
                    if (independent)
                    {
                        foreach (string section in new[] { "signals", "resource_signals" })
                            for(int i=0;i<doc.Get(section).Count;i++)
                                if (doc.Get(section)[i].Get("providers")[0].Get("component").Text == "inventory_signal")
                                    doc.Get(section)[i].Get("providers").Add(Json.Parse("{\"component\":\"independent_inventory_signal\",\"schema\":3}"));
                    }
                    for(int i=0;i<doc.Get("entities").Count;i++)
                    {
                        var parts=doc.Get("entities")[i].Get("components");var row=parts.Get("inventory_signal");
                        row.Set("quantities",numericOnly?JsonValue.Null():Json.Parse("{\"supply\":16}"));
                        if(independent){parts.Remove("inventory_signal");row.Set("metadata_fault",JsonValue.Of("none"));parts.Set("independent_inventory_signal",row);}
                        parts.Set("inventory_signal_private_probe",Json.Parse("{}"));
                    }
                    edit?.Invoke(doc);
                });
        }
        private static AutomationSignalSnapshot Read(SimGame game,string metric)
        {
            var code=View(game).ReadSignal(Entity(game,"field.plant").Id,new Code("feed"),"binding."+metric,out var value);
            if(!code.IsNone)throw new InvalidOperationException("signal "+code);return value;
        }
        private static void Facts(Check c,SimGame game,int stock,bool numericOnly=false)
        {
            ulong hash=game.Hash();
            foreach(var metric in new[]{"stored","available","inbound","outbound","receiving-room"})
            {
                var value=Read(game,metric);int expected=metric=="stored"||metric=="available"?stock:metric=="receiving-room"?40-stock:0;
                c.That(value.Available&&value.Unit==new Code("unit.supply")&&value.Value==Fix.FromInt(expected),"actual native quantity "+metric+" stock="+stock);
            }
            foreach(var metric in new[]{"stock-ready","available-ready","incoming-ready"})
            {
                var value=Read(game,metric);
                if(numericOnly)c.That(!value.Available&&value.Blocker==InventorySignal.NoThreshold,"explicit numeric-only threshold "+metric);
                else c.That(value.Available&&value.Value==(metric=="incoming-ready"||stock>=16?Fix.One:Fix.Zero),"original threshold logic "+metric+" stock="+stock);
            }
            c.Equal(hash,game.Hash(),"all metric reads pure");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual inventory contract build");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current authored save format, original signal and inventory codec owners");
            c.That(Healthy(c,false)==Healthy(c,true),"independent/native bindings earn identical physical recovery time");
            var numeric=New(Build(new InitializeProbe(),out _,numericOnly:true));Facts(c,numeric,20,true);
            Invalid(c);Faults(c);
        }
        private static long Healthy(Check c,bool independent)
        {
            var initialize=new InitializeProbe();var factory=Build(initialize,out var proof,independent);var game=New(factory);
            c.That(proof.ActualBinding==independent,"actual original runtime uses native/independent metadata");c.Equal(1,initialize.Initializations,"valid Initialize once");
            Facts(c,game,20);var retained=Read(game,"available");
            c.Equal(Codes.NotOwned,View(game,1).ReadSignal(Entity(game,"field.plant").Id,new Code("feed"),"binding.stored",out _),"foreign inventory facts remain private");
            var limits=new ReplayLimits(4*1024*1024,3000);using var recorder=game.RecordReplay(limits);
            Order(c,game,new RaiseIncidentCommand{Target=Entity(game,"field.plant").Id,Source=new Code("pressure.leak"),Kind=new Code("pressure.leak")});
            Order(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"machine").Id,Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("inspect"),new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            c.That(Stock(game)==Fix.FromInt(20),"normal work commands do not instantly spend stock");
            var twin=factory.Load(game.Save());Until(game,twin,()=>Stock(game)==Fix.FromInt(12),"physical recovery consumes actual supply");
            c.That(((PointSnapshot)Entity(game,"machine").Spatial).Position==Fix.FromInt(20)&&Active(game),"machine actually travels4 to20 before signal changes");
            Facts(c,game,12);Facts(c,twin,12);c.That(retained.Value==Fix.FromInt(20),"retained scalar snapshot never follows mutable stock");
            ulong hash=game.Hash();long at=game.Clock.StepIndex;game.Clock.Pause();twin.Clock.Pause();Step(game,twin);
            c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"sole clock pause changes no physical stock or signal facts");game.Clock.Resume();twin.Clock.Resume();
            Until(game,twin,()=>!Active(game),"saved actual repair completion");Facts(c,game,12);
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"post-recovery save retains actual stock/facts");
            var replay=new ReplayVerifier().Verify(factory,recorder.Finish(),limits);
            c.That(replay.Success&&replay.ActualHash==game.Hash(),"complete physical recovery and stock-derived observations replay to final live hash");
            Console.WriteLine("inventory signal physical replay: independent="+independent+";initial="+recorder.InitialStep+";final="+game.Tick
                +";commands="+recorder.CommandCount+";checkpoints="+recorder.CheckpointCount+";expected-hash="+game.Hash().ToString("X16")+";actual-hash="+replay.ActualHash.ToString("X16"));
            var fresh=New(factory);Facts(c,fresh,20);c.That(!Active(fresh),"interleaved session has no shared incident or inventory state");
            return game.Clock.StepIndex;
        }
        private static void Invalid(Check c)
        {
            foreach(var fault in new[]{"missing-inventory","missing-quantities","empty-quantities","unknown-resource","negative","capacity","inventory-null","quantities-empty","schema-version","bindings-null","unbound"})
            {
                var initialize=new InitializeProbe();Action create=()=>New(Build(initialize,out _,edit:doc=>{
                    var unused=doc.Get("entities")[0].Clone();unused.Set("id",JsonValue.Of("binding.unused"));doc.Get("entities").Add(unused);
                    var parts=unused.Get("components");var row=parts.Get("independent_inventory_signal");
                    if(fault=="missing-inventory")row.Set("inventory",JsonValue.Of("absent"));
                    if(fault=="missing-quantities")row.Remove("quantities");if(fault=="empty-quantities")row.Set("quantities",Json.Parse("{}"));
                    if(fault=="unknown-resource")row.Set("quantities",Json.Parse("{\"missing\":1}"));if(fault=="negative")row.Set("quantities",Json.Parse("{\"supply\":-1}"));
                    if(fault=="capacity")row.Set("quantities",Json.Parse("{\"supply\":41}"));
                    if(new[]{"inventory-null","quantities-empty","schema-version","bindings-null"}.Contains(fault))row.Set("metadata_fault",JsonValue.Of(fault));
                    if(fault=="unbound"){row.Remove("metadata_fault");parts.Remove("independent_inventory_signal");parts.Set("unbound_inventory_signal",row);}
                }));
                c.Throws<ContentException>(create,"unused invalid inventory binding refused "+fault);c.Equal(0,initialize.Initializations,"invalid metadata before Initialize "+fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach(var fault in new[]{"live-throw","live-pause","live-touch","live-private","live-pause-throw","live-touch-throw","live-private-throw"})
            foreach(bool restored in new[]{false,true})
            {
                var factory=Build(new InitializeProbe(),out _,edit:doc=>doc.Get("entities")[0].Get("components").Get("independent_inventory_signal").Set("metadata_fault",JsonValue.Of(fault)));
                var game=New(factory);if(restored)game=factory.Load(game.Save());
                game.Update();if(game.Fault==null)c.Throws<InvalidOperationException>(()=>Read(game,"stored"),"actual live metadata refuses "+fault);
                c.That(game.Fault?.Phase=="inventory.signal-metadata","one inventory metadata owner reports callback and verification faults "+fault);
                if(fault=="live-throw"||fault.EndsWith("-throw",StringComparison.Ordinal))
                    c.That(game.Fault?.Message.Contains("proof.inventory-signal.quantities")==true,"original inventory callback cause survives combined mutation/throw "+fault);
                var first=game.Fault;long at=game.Tick;
                c.Throws<InvalidOperationException>(()=>Read(game,"available"),"faulted signal cannot publish healthy facts "+fault);
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted signal cannot checkpoint "+fault);c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted signal cannot hash healthy "+fault);
                c.Equal(0,game.Update(),"terminal inventory metadata cannot advance "+fault);
                c.That(ReferenceEquals(first,game.Fault)&&game.Tick==at,"first inventory error and sole clock remain authoritative "+fault);
            }
        }
    }
}
