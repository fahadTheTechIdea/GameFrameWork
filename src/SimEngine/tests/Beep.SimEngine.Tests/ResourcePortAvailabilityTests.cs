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
    // All behavior is engine-owned. Driver authors real files and simulates player intent through seats/views/Update.
    internal static class ResourcePortAvailabilityTests
    {
        internal const string Case="Resource port availability: typed original loss preserves manufacturing time tools WIP and physical cancellation";
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",9000000001),
            Second=new ResourceEndpointId("proof.pools",9000000002);
        private static readonly Code Process=new Code("process");
        internal static void Register()=>TestRunner.Add(Case,Run);
        internal static GameFactory Factory(bool point,bool half)
        {
            var document=Json.Parse(FrameworkProofContent.Read("resource-port-availability.json"));
            if(point) {
                var parts=document.Get("entities")[0].Get("components");parts.Remove("tile_presence");
                parts.Set("point_presence",Json.Parse("{\"space\":\"restore.points\",\"position\":16}"));
                document.Get("entity_setups")[0].Set("placement",Json.Parse("{\"space\":\"restore.points\",\"position\":16}"));
            }
            return ScopedHaulGame.Factory(restoredDockDelivery:true,pointDockRecovery:point,availabilityDefinitions:document.ToString(),
                editCommon:root=>{if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));},
                editBindings:root=>root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("availability.factory-start")));
        }
        private static SimGame New(GameFactory f,bool point)=>f.NewGame(new NewGameRequest{
            Scenario=point?"scoped-point-dock-restoration":"scoped-dock-restoration",Seed=1893,Seats={Controller.Human,Controller.Human}});
        private static EntityId Find(SimGame g,string name,bool point=false)=>Entity(g,point?"point."+name:name);
        private static EntityId Machine(SimGame g)=>Find(g,"availability.factory");
        private static TransformationSnapshot Current(SimGame g)
        { if(!g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Machine(g),Process,out var value))throw new InvalidOperationException("actual process missing");return value; }
        private static ResourceId Steel(SimGame g)
        { if(!g.Seat(0).View.TryFindResource("steel",out var value))throw new InvalidOperationException("authored steel missing");return value; }
        private static List<WorkJobView> Jobs(SimGame g)=>PermanentRemovalTests.Jobs(g);
        private static bool Live(WorkJobView j)=>j.State==JobState.Open||j.State==JobState.Assigned||j.State==JobState.Paused;
        private static ResourcePortSnapshot Port(SimGame g,string name)
        {
            Code refusal=g.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Machine(g),new Code(name),Steel(g),out var value);
            if(!refusal.IsNone)throw new InvalidOperationException("actual port facts "+refusal);return value;
        }
        private static void Route(Check c,SimGame g,string port,ResourceEndpointId endpoint)=>PermanentRemovalTests.Submit(c,g,new SelectResourcePortCommand {
            Entity=Machine(g),Port=new Code(port),Endpoint=endpoint});
        private static void Reach(SimGame g,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<1000&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" exceeded1000Updates clock="+g.Clock.StepIndex+" phase="+Current(g).Phase+"/"+Current(g).Blocker+
                " progress="+Current(g).Progress+" jobs="+string.Join(";",Jobs(g).Select(j=>j.State+"/"+j.Blocker)));
        }
        private static void Hold(Check c,SimGame g,bool point,bool held)=>PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand {
            Worker=Find(g,"dock.machine",point),Held=held});
        private static void Physical(Check c,bool point,bool half,string mode)
        {
            var f=Factory(point,half);var g=New(f,point);var isolated=New(f,point);ulong untouched=isolated.Hash();var resource=Steel(g);
            var limits=new ReplayLimits(4*1024*1024,5000);
            using(var record=g.RecordReplay(limits)) {
                PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=Find(g,"dock.queue",point),Held=true});
                int supply=checked((int)PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand {
                    Source=Find(g,"source.warehouse",point),Destination=Machine(g),SourcePort=new Code("stock"),DestinationPort=new Code("raw"),
                    Resource=resource,Ceiling=Fix.FromInt(8),Worker=Find(g,"dock.machine",point),Priority=100}).Arg0);
                var twin=f.Load(g.Save());Reach(g,twin,()=>Port(g,"raw").Stock.AmountOf(resource)==Fix.FromInt(8)&&!Jobs(g).Any(Live),"real physical feed supply");
                Hold(c,g,point,true);
                PermanentRemovalTests.Submit(c,g,new EnqueueTransformationCommand {Transformer=Machine(g),Capability=Process,Transformation=mode=="flow"?"availability.flow":"availability.batch"});
                twin=f.Load(g.Save());Reach(g,twin,()=>Current(g).Wip.Execution>0&&Current(g).Progress.WholeTicks>0,"actual timed process and reusable tool acquisition");
                var active=Current(g);var receipt=active.Tools.Receipts.Single();
                c.Equal(Fix.One,receipt.Amount,"one real original reusable tool hold");
                Route(c,g,"tool",Second);var blocked=Current(g);
                c.Equal(ResourcePortCodes.OriginalUnavailable,blocked.Blocker,"original tool binding loss is a typed gameplay blocker");
                c.Equal(receipt.Token,blocked.Tools.Receipts.Single().Token,"same original receipt remains protected");
                if(mode=="flow") {
                    var unavailable=blocked.Flow.ToolPorts.Single();
                    c.That(!unavailable.Ready&&unavailable.Blocker==ResourcePortCodes.OriginalUnavailable&&unavailable.Stock==null&&unavailable.Version==-1,
                        "unavailable port observation contains no invented zero stock");
                    c.Equal(First,unavailable.Endpoint,"unavailable original identity retained, never current substitution");
                }
                ulong pure=g.Hash();Current(g);c.That(!g.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Machine(g),Process,out _),"private blocked process withheld");c.Equal(pure,g.Hash(),"blocked process observations pure");
                twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"blocked original tools and WIP durable");
                for(int i=0;i<4;i++)Step(g,twin);
                c.Equal(blocked.Progress,Current(g).Progress,"lost tool access earns no manufacturing time");
                c.Equal(blocked.Operating.Consumed.AmountOf(resource),Current(g).Operating.Consumed.AmountOf(resource),"lost tool access consumes no repeated operating inputs");
                c.Equal(blocked.Tools.Receipts.Single().Token,Current(g).Tools.Receipts.Single().Token,"saved wait retains exact actual tool token");
                Route(c,g,"tool",First);twin=f.Load(g.Save());
                Reach(g,twin,()=>Current(g).Progress.WholeTicks>blocked.Progress.WholeTicks||Current(g).QueueCount==0,"actual original restoration resumes time");
                if(mode=="cancel") {
                    Route(c,g,"raw",Second);
                    PermanentRemovalTests.Submit(c,g,new CancelTransformationCommand{Transformer=Machine(g),Capability=Process,Index=0});
                    var returning=Current(g);c.Equal(TransformationPhase.Returning,returning.Phase,"normal cancellation retains original physical WIP");
                    c.Equal(ResourcePortCodes.OriginalUnavailable,returning.Blocker,"missing original return source blocks, not current bank refund");
                    c.Equal(Fix.One,returning.Wip.Stock.AmountOf(resource),"actual input preserved during return wait");
                    twin=f.Load(g.Save());for(int i=0;i<3;i++)Step(g,twin);
                    c.Equal(returning.Wip.Stock.AmountOf(resource),Current(g).Wip.Stock.AmountOf(resource),"saved return wait changes no input stock");
                    c.Equal(returning.Tools.Receipts.Single().Token,Current(g).Tools.Receipts.Single().Token,"return wait retains reusable tool");
                    Route(c,g,"raw",First);twin=f.Load(g.Save());Reach(g,twin,()=>Current(g).QueueCount==0,"exact original return restoration");
                    c.Equal(Fix.Zero,Port(g,"output").Stock.AmountOf(resource),"cancellation creates no output/refund to unrelated store");
                }
                else if(mode=="flow") {
                    Reach(g,twin,()=>Port(g,"output").Stock.AmountOf(resource).IsPositive,"real accumulated continuous output");
                    PermanentRemovalTests.Submit(c,g,new CancelTransformationCommand{Transformer=Machine(g),Capability=Process,Index=0});
                    twin=f.Load(g.Save());Reach(g,twin,()=>Current(g).QueueCount==0,"normal continuous stop settles tools");
                    c.That(Port(g,"output").Stock.AmountOf(resource).IsPositive,"continuous stop preserves actually produced output");
                }
                else Reach(g,twin,()=>Current(g).QueueCount==0,"earned batch conversion");
                c.That(Current(g).Tools.Receipts.Count==0&&Current(g).Wip.Stock.IsEmpty,"final original tool/WIP ownership settles once");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"completed availability transition save/load");
                c.Equal(untouched,isolated.Hash(),"interleaved manufacturing sessions isolated");
                c.That(new ReplayVerifier().Verify(f,record.Finish(),limits).Success,"physical supply and typed interruption/cancellation replay");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current explicit availability identity");c.Equal(154,EngineInfo.SaveFormat,"no duplicated stock/port availability saved owner");
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})foreach(string mode in new[]{"batch","cancel","flow"})Physical(c,point,half,mode);
        }
    }
}

