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
    // Actual JSON/master, seat commands/results/views and Game.Update only. No host utility or network logic.
    internal static class SpatialUtilityConsumptionTests
    {
        internal const string Case = "Spatial utility consumption: owned physical point supply blockers and selected-space observation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, PointLineGeometryProofModule probe,
            UtilityOwnerObservationModule negative = null, string phase = null, string mode = null)
        {
            var utility = Json.Parse(FrameworkProofContent.Read("point-network-utilities.json"));
            var extra = new Dictionary<string,string> {
                ["point-owned-network.json"] = FrameworkProofContent.Read("point-owned-network.json"),
                ["point-network-utilities.json"] = utility.ToString()
            };
            Dictionary<string,string> moduleDocs = null;
            if (negative != null)
            {
                var failure = Json.Parse(FrameworkProofContent.Read("utility-owner-observation.json"));
                failure.Get("utility_owner_failures")[0].Set("phase", JsonValue.Of(phase));
                failure.Get("utility_owner_failures")[0].Set("mode", JsonValue.Of(mode));
                moduleDocs = new Dictionary<string,string> { ["utility-owner-observation.json"] = failure.ToString() };
                var parts = utility.Get("entities")[0].Get("components");
                parts.Set("utility_owner_witness", JsonValue.NewObject());
                parts.Set("utility_owner_probe", Json.Parse("{\"id\":\"point.observer\",\"source\":\"point.process\",\"failure\":\"utility.owner-failure\"}"));
                extra["point-network-utilities.json"] = utility.ToString();
            }
            return SpatialNetworkGraphTests.Points(half, probe, moreDocuments:extra,
                editCorridors:root => {
                    root.Get("lines")[0].Set("owned", JsonValue.Of(true));
                    root.Get("lines")[0].Set("owner_layer", JsonValue.Of("corridor.company"));
                },
                editNetwork:root => {
                    root.Get("networks")[0].Set("public", JsonValue.Of(false));
                    root.Get("networks")[0].Set("demand_policy", JsonValue.Of("point.utility-demand"));
                },
                extraModules:negative == null ? null : new GameModule[] { negative },
                extraDeclarations:negative == null ? null : "[" + FrameworkProofContent.Read("utility-owner-observation-module.json") + "]",
                moduleDocuments:moduleDocs,
                afterPorts:root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
                    root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("40"));
                    root.Get("entities")[0].Get("components").Set("network_signal", Json.Parse("{\"id\":\"point.pool\",\"network\":\"point.network\"}"));
                    root.Get("entities")[0].Get("components").Set("signal_network_gate", Json.Parse("{\"id\":\"point.buffer-gate\",\"networks\":[\"point.network\"],\"conditions\":[{\"id\":\"stock-band\",\"source\":\"point.pool\",\"signal\":\"point.pool-stock\",\"minimum\":0,\"maximum\":7,\"unit\":\"unit.supply\",\"blocker\":\"point.buffer-full\"}]}"));
                    root.Get("entities")[1].Get("components").Remove("ports");
                    root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("point.utility-plant.start"));
                    if (negative != null) root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(negative.Id));
                });
        }
        private static EntityId Plant(SimGame g) => Entity(g, "point.utility-plant");
        private static UtilityDemandSnapshot Demand(SimGame g, string consumer = "point.process")
        {
            if (!g.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(Plant(g), new Code(consumer), out var value))
                throw new InvalidOperationException("missing actual point utility");
            return value;
        }
        private static TransformationSnapshot ProcessPoint(SimGame g)
        {
            if (!g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Plant(g), new Code("point.process"), out var value))
                throw new InvalidOperationException("missing actual point processor");
            return value;
        }
        private static List<NetworkPoolView> Pools(SimGame g, int seat = 0)
        {
            var values = new List<NetworkPoolView>();
            var status = g.Seat(seat).View.Feature<InfrastructureView>().CopyPools("point.network", values);
            if (!status.IsNone) throw new InvalidOperationException("point utility pool facts: " + status);
            return values;
        }
        private static Fix Total(SimGame g) => SpatialCorridorExecutionTests.Total(g, true)
            + Pools(g).SelectMany(p=>p.Stock).Aggregate(Fix.Zero,(sum,line)=>sum+line.Amount);
        private static bool ReceivingPoolEmpty(SimGame g)
        {
            var status=g.Seat(0).View.Feature<InfrastructureView>().ReadConnection(Plant(g),"point.network",out var connection);
            if(!status.IsNone)throw new InvalidOperationException("actual utility connection: "+status);
            return Pools(g).Single(p=>p.Endpoint==connection.Endpoint).Stock.Count==0;
        }
        private static Fix Committed(SimGame g, ResourceFlowKind kind, Code? cause = null)
        {
            var history=g.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            if(history.Status!=ResourceFlowReadStatus.Ready)throw new InvalidOperationException("actual committed history: "+history.Status);
            var resource=PermanentRemovalTests.Supply(g);
            var raw=history.Aggregates.Where(a=>a.Resource==resource&&a.Kind==kind&&(!cause.HasValue||a.Cause==cause.Value))
                .Aggregate(System.Numerics.BigInteger.Zero,(sum,line)=>sum+line.AmountRaw);
            return Fix.FromRaw(checked((long)raw));
        }
        private static void Reach(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            for (int i=0;i<900&&!done();i++) Step(g,twin);
            if (!done()) throw new InvalidOperationException(stage+" clock="+g.Clock.StepIndex+" utility="+Demand(g).Blocker
                +" phase="+ProcessPoint(g).Phase+" queue="+ProcessPoint(g).QueueCount+" source="+Stock(g,"source")
                +" pools="+Pools(g).Count+" plant="+Stock(g,"point.utility-plant"));
        }
        private static void Control(Check c,SimGame g,bool enabled)
            => PermanentRemovalTests.Submit(c,g,new ConfigureOperationCommand {
                Entity=Plant(g),Operation=new Code("point.process"),Enabled=enabled,Target=Fix.One });
        private static void Physical(Check c,bool half)
        {
            var probe=new PointLineGeometryProofModule();var f=Factory(half,probe);
            var g=ConstructionPlanTests.NewPoints(f);var idle=ConstructionPlanTests.NewPoints(f);ulong isolated=idle.Hash();
            c.That(probe.SharedOwner&&probe.NativeOwnerAbsent&&!g.Modules.Contains(TycoonModule.ModuleId),"original non-tile owners, no business requirement");
            c.Equal(UtilityCodes.Inactive,Demand(g).Blocker,"idle processor is not invented readiness");
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var record=g.RecordReplay(limits))
            {
                PermanentRemovalTests.Submit(c,g,new EnqueueTransformationCommand {
                    Transformer=Plant(g),Capability=new Code("point.process"),Transformation="point.utility-recipe" });
                Reach(g,null,()=>Demand(g).Blocker==UtilityCodes.Shortage,"no unbuilt connection supplies utilities");
                c.Equal(Fix.Zero,Demand(g).Consumed.AmountOf(PermanentRemovalTests.Supply(g)),"unconnected operation consumes nothing");
                long build=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=SpatialCorridorExecutionTests.Intent(true,false)}).Arg0;
                var twin=f.Load(g.Save());
                Reach(g,twin,()=>PermanentRemovalTests.Plan(g,build).State==PlanState.Done,"physical materials travel and build original point corridor");
                Reach(g,twin,()=>Demand(g).Consumed.AmountOf(PermanentRemovalTests.Supply(g)).IsPositive,"timed original utility consumes real network supply");
                var retained=Demand(g);c.That(retained.Drawing&&retained.ElapsedTicks>0,"original processor owns utility exposure");
                ulong pure=g.Hash();Demand(g);
                c.That(!g.Seat(1).View.Feature<OperationsView>().TryGetUtilityDemand(Plant(g),new Code("point.process"),out _)
                    && Pools(g,1).Count==0,"private operation and owned pool facts withheld");
                c.Equal(pure,g.Hash(),"complete utility/privacy reads do not advance spatial stock or clock");
                Control(c,g,false);var stopped=Demand(g);twin=f.Load(g.Save());
                for(int i=0;i<3;i++)Step(g,twin);
                c.That(Demand(g).ElapsedTicks==stopped.ElapsedTicks&&Demand(g).Consumed.Lines.SequenceEqual(stopped.Consumed.Lines),"shutdown burns nothing");
                long at=g.Clock.StepIndex;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);c.Equal(at,g.Clock.StepIndex,"one clock pauses all owners");g.Clock.Resume();twin.Clock.Resume();
                Control(c,g,true);
                PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="corridor.remove",Target=Entity(g,"corridor.removal-site"),Priority=100});
                twin=f.Load(g.Save());
                Reach(g,twin,()=>SpatialCorridorExecutionTests.Segment(g,true,1).IsZero,"physical machine earns utility network cut");
                c.Equal(2,Pools(g).Count,"original topology splits actual company pool");
                Reach(g,twin,()=>ReceivingPoolEmpty(g)&&Stock(g,"point.utility-plant").IsZero&&Demand(g).Blocker==UtilityCodes.Shortage&&ProcessPoint(g).QueueCount==1,"disconnected finite stock becomes typed shortage");
                var exhausted=Demand(g);c.That(exhausted.ElapsedTicks>retained.ElapsedTicks,"remaining actual pool stock burns before shortage");
                int service=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{
                    Plan="point.utility-service",Target=Plant(g),Priority=20}).Arg0);
                var waiting=PermanentRemovalTests.Node(g,service,"service");c.Equal(UtilityCodes.Shortage,waiting.Blocker,"same utility contract gates work");
                twin=f.Load(g.Save());for(int i=0;i<3;i++)Step(g,twin);
                c.That(Demand(g).Consumed.Lines.SequenceEqual(exhausted.Consumed.Lines),"shortage consumes no fabricated utility units; zero-input half quanta may advance exposure");
                c.Equal(waiting.Progress,PermanentRemovalTests.Node(g,service,"service").Progress,"shortage earns no staffed work");
                PermanentRemovalTests.Submit(c,g,new CancelOrderCommand{Order=service});
                var path=Json.Parse("{\"space\":\"field.points\",\"path\":[{\"space\":\"field.points\",\"position\":5}]}");
                long rebuild=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{
                    Intent=new PlanIntent("line:corridor.point-line",path,"utility-repair",80,false)}).Arg0;
                twin=f.Load(g.Save());
                Reach(g,twin,()=>PermanentRemovalTests.Plan(g,rebuild).State==PlanState.Done,"ordinary physical supply and reconstruction");
                service=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{
                    Plan="point.utility-service",Target=Plant(g),Priority=20}).Arg0);twin=f.Load(g.Save());
                Reach(g,twin,()=>PermanentRemovalTests.Node(g,service,"service").Progress.WholeTicks>0,"real restored utility resumes original work");
                var resumed=Demand(g);
                Reach(g,twin,()=>ProcessPoint(g).QueueCount==0,"original processor completes timed recipe");
                c.Equal(1,Pools(g).Count,"same original graph rejoins, no utility network copy");
                var final=Demand(g);var resource=PermanentRemovalTests.Supply(g);
                c.Equal(Fix.FromInt(12),Committed(g,ResourceFlowKind.Consumed,TransformerManager.CauseOperatingInputs),"original journal records twelve actual utility units across completed execution");
                c.Equal(Fix.One,Committed(g,ResourceFlowKind.Produced),"original journal records one actual recipe output");
                c.Equal(Fix.FromInt(40),Total(g)+Committed(g,ResourceFlowKind.Consumed)-Committed(g,ResourceFlowKind.Produced),"all actual entity/pool stock and original committed consumption/production conserved");
                c.That(final.Blocker==UtilityCodes.Inactive&&final.Consumed.IsEmpty&&final.ElapsedTicks==0,"completed consumer is inactive, not a fabricated lifetime counter");
                c.That(retained.ElapsedTicks<resumed.ElapsedTicks&&retained.Consumed.AmountOf(resource)<resumed.Consumed.AmountOf(resource),"retained utility facts detached");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"completed original utility and spatial owners durable");
                c.Equal(isolated,idle.Hash(),"interleaved custom-space session isolated");
                c.That(new ReplayVerifier().Verify(f,record.Finish(),limits).Success,"actual commands/time/network/work/utility replay");
            }
        }
        private static void Failure(Check c,bool half,string phase,string mode)
        {
            var module=new UtilityOwnerObservationModule();
            var f=Factory(half,new PointLineGeometryProofModule(),module,phase,mode);var g=ConstructionPlanTests.NewPoints(f);
            var healthy=ConstructionPlanTests.NewPoints(Factory(half,new PointLineGeometryProofModule()));ulong isolated=healthy.Hash();
            var view=g.Seat(0).View.Feature<OperationsView>();var retained=Demand(g,"point.observer");
            long until=(half?4:2)+(phase=="update"?2:0);
            for(int i=0;i<10&&g.Clock.StepIndex<until&&g.Fault==null;i++)g.Update();
            UtilityDemandSnapshot failed=retained;Exception actual=null;
            if(phase=="view")try{view.TryGetUtilityDemand(Plant(g),new Code("point.observer"),out failed);}catch(Exception error){actual=error;}
            c.That(g.Fault!=null&&!module.Returned,"actual spatial mutation is not a healthy "+phase+" read");
            if(g.Fault==null)throw new InvalidOperationException("missing selected-space fault "+phase+"/"+mode);
            if(phase=="view")c.That(actual is InvalidOperationException&&failed.Consumer.IsNone&&failed.Ports==null,"no utility fact prefix from mutated layer");
            if(mode=="spatial-layer-throw")c.That(g.Fault.ToString().Contains("proof.utility-spatial-first-error"),"callback first error not masked");
            else c.That(g.Fault.ToString().Contains("persisted state"),"actual selected-space codec detects layer mutation");
            var first=g.Fault;long at=g.Clock.StepIndex;
            c.Throws<InvalidOperationException>(()=>view.TryGetUtilityDemand(Plant(g),Code.None,out _),"faulted utility rejects early returns");
            c.Throws<InvalidOperationException>(()=>g.Save(),"mutated utility callback cannot save healthy checkpoint");
            c.Equal(0,g.Update(),"first fault stops update");c.Equal(at,g.Clock.StepIndex,"first fault stops sole clock");
            c.That(ReferenceEquals(first,g.Fault)&&retained.ElapsedTicks==0,"first fault and detached original facts retained");
            c.Equal(isolated,healthy.Hash(),"mutated provider isolated from other actual session");Step(healthy);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current selected-space utility identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current original utility payload/schema");
            foreach(bool half in new[]{false,true})Physical(c,half);
            foreach(bool half in new[]{false,true})foreach(string phase in new[]{"view","update"})
                foreach(string mode in new[]{"spatial-layer","spatial-layer-throw"})Failure(c,half,phase,mode);
        }
    }
}
