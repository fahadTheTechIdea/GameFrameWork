using System;
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
    // Actual master/JSON plus ordinary public player intent. Illegal behavior exists only in the engine-side proof endpoint.
    internal static class CoupledProviderGuardTests
    {
        internal const string Case="Coupled provider guards: independent atomic inventory remains deterministic and illegal callbacks cannot publish or save";
        private static readonly Code Shared=new Code("shared"),Process=new Code("process");
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(string mode,bool half,ConnectedResourcePortModule proof)
            =>ConnectedResourcePortTests.Factory(half,false,true,proof,definitions:FrameworkProofContent.Read("atomic-inventory.json"),
                edit:data=>data.Get("entities")[1].Get("components").Get("atomic_inventory").Set("conversion_behavior",JsonValue.Of(mode)),
                editWorld:root=>{
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
                    root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("atomic.factory-start"));
                });
        private static EntityId Site(SimGame g)=>Entity(g,"atomic.factory");
        private static ResourceId Supply(SimGame g)
        {if(!g.Seat(0).View.TryFindResource("supply",out var r))throw new InvalidOperationException("actual supply missing");return r;}
        private static ResourcePortSnapshot Port(SimGame g)
        {Code refusal=g.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Site(g),Shared,Supply(g),out var row);if(!refusal.IsNone)throw new InvalidOperationException("actual port "+refusal);return row;}
        private static TransformationSnapshot Current(SimGame g)
        {if(!g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Site(g),Process,out var row))throw new InvalidOperationException("actual process missing");return row;}
        private static ConfigureOperationCommand Enable(SimGame g)=>new ConfigureOperationCommand {Entity=Site(g),Operation=Process,Enabled=true,Target=Fix.One};
        private static void Queue(Check c,SimGame g)=>PermanentRemovalTests.Submit(c,g,new EnqueueTransformationCommand {Transformer=Site(g),Capability=Process,Transformation="atomic.equal"});
        private static void Healthy(Check c,bool half)
        {
            var proof=new ConnectedResourcePortModule();var f=Factory("healthy",half,proof);var idle=ConstructionPlanTests.NewPoints(f);ulong isolated=idle.Hash();var g=ConstructionPlanTests.NewPoints(f);
            var limits=new ReplayLimits(4*1024*1024,3000);
            using(var recorder=g.RecordReplay(limits)) {
                c.Equal(Fix.FromInt(12),Port(g).Stock.AmountOf(Supply(g)),"independent endpoint has real authored full stock");
                Queue(c,g);PermanentRemovalTests.Submit(c,g,Enable(g));var twin=f.Load(g.Save());
                for(int i=0;i<40&&Current(g).Flow.Produced.AmountOf(Supply(g))<Fix.FromInt(4);i++)Step(g,twin);
                var active=Current(g);c.That(active.Flow.Produced.AmountOf(Supply(g))>=Fix.FromInt(4),"independent contract endpoint earns actual timed conversion");
                c.Equal(Code.None,active.Blocker,"full independent buffer has no false capacity wait");
                c.Equal(Fix.FromInt(12),Port(g).Stock.AmountOf(Supply(g)),"original delegated state retains exactly full net-zero stock");
                c.Equal(active.Flow.Consumed.AmountOf(Supply(g)),active.Flow.Produced.AmountOf(Supply(g)),"actual earned native input/output match");
                c.Equal(Fix.One,active.Tools.Receipts[0].Amount,"only draw delta consumed, original reusable tool remains");
                ulong pure=g.Hash();Port(g);Current(g);c.Equal(pure,g.Hash(),"full scoped provider observations are pure");
                c.That(!g.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Site(g),Process,out _),"private independent process withheld");
                twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"independent original stock/books/progress durable");Step(g,twin);
                var partial=Current(g);Fix retained=Port(g).Stock.AmountOf(Supply(g));
                c.Equal(Fix.FromInt(12)-partial.Flow.Consumed.AmountOf(Supply(g))+partial.Flow.Produced.AmountOf(Supply(g)),retained,"actual quantized input/output prefix accounts for retained stock");
                PermanentRemovalTests.Submit(c,g,new CancelTransformationCommand {Transformer=Site(g),Capability=Process,Index=0});
                twin=f.Load(g.Save());for(int i=0;i<20&&Current(g).QueueCount!=0;i++)Step(g,twin);
                c.Equal(0,Current(g).QueueCount,"independent continuous stop settles");
                c.Equal(retained,Port(g).Stock.AmountOf(Supply(g)),"stop preserves actual earned stock, without refund or unearned output");
                c.That(g.Seat(0).View.Feature<InventoryView>().TryGetReservations(Site(g), Code.None,out var receipts)&&receipts.Count==0,"all original tool records released");
                c.Equal(isolated,idle.Hash(),"interleaved session isolated");c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"public independent atomic workflow replays");
            }
        }
        private static void Negative(Check c,string mode)
        {
            var proof=new ConnectedResourcePortModule();var f=Factory(mode,false,proof);var idle=ConstructionPlanTests.NewPoints(f);ulong isolated=idle.Hash();var g=ConstructionPlanTests.NewPoints(f);
            Queue(c,g);c.Equal(Fix.FromInt(12),Port(g).Stock.AmountOf(Supply(g)),"disabled faulty endpoint remains actual full stock before illegal callback");
            int before=proof.CoupledFacts;var command=Enable(g);ulong pure=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(command),"control admission does not invoke illegal conversion callback");c.Equal(pure,g.Hash(),"pre-callback preview pure");
            g.Seat(0).Submit(command);
            for(int i=0;i<8&&g.Fault==null;i++){try{g.Update();}catch(Exception){}}
            c.That(g.Fault!=null,"illegal atomic callback is terminal, not healthy backpressure: "+mode);
            c.Equal(before,proof.CoupledFacts,"no successful consumed/produced conversion prefix: "+mode);
            if(mode.StartsWith("throw",StringComparison.Ordinal))c.That(g.Fault?.Message.Contains("authored atomic")==true,"first provider error retained: "+mode);
            c.Throws<InvalidOperationException>(()=>g.Save(),"faulted callback cannot checkpoint healthy: "+mode);
            c.Throws<InvalidOperationException>(()=>g.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Site(g),Shared,Supply(g),out _),"faulted stock facts withheld: "+mode);
            c.Equal(isolated,idle.Hash(),"faulted provider cannot contaminate interleaved session: "+mode);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual current engine identity");Healthy(c,false);Healthy(c,true);
            foreach(string mode in new[]{"unknown-status","mutate-check","clock-check","throw-check","noop-commit","extra-stock","clock-commit","throw-commit"})Negative(c,mode);
        }
    }
}
