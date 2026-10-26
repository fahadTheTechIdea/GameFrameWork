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
    // Existing master-listed JSON, public input/time/views/save/replay only. No research/utility logic here.
    internal static class UtilityResearchAcceptanceTests
    {
        internal const string Case="Utility research acceptance: independent earned completion unlocks physical manufacturing";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool direct,bool half,out UtilityResearchContractsModule utility,out WorkEffectContractsModule effect,
            string missing=null)
        {
            utility=new UtilityResearchContractsModule();effect=new WorkEffectContractsModule();
            var effects=Json.Parse(FrameworkProofContent.Read("work-effect-contracts.json"));
            effects.Get("independent_research_effects")[0].Set("consumer",JsonValue.Of("lab.utility"));
            if(!direct)effects.Set("independent_research_effects",JsonValue.NewArray());
            var retry=Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Set("work_retry_profiles",Json.Parse(FrameworkProofContent.Read("completion-retry.json")).Get("work_retry_profiles").Clone());
            retry.Get("work_retry_profiles")[0].Get("retry_blockers").Add(JsonValue.Of("utility.inactive"));
            retry.Get("work_retry_profiles")[0].Get("retry_blockers").Add(JsonValue.Of("utility.shortage"));
            return UtilityResearchContractTests.Factory(utility,direct,
                edit: root=>{if(missing!=null)root.Get(direct?"independent_utility_requirements":"target_utility_requirements")[0].Set(direct?"source":"consumer",JsonValue.Of(missing));},
                extraModule:effect,
                extraDeclaration:"{\"id\":\"proof.work-effect-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.technology\",\"version\":\"1.0.0\"}],\"documents\":[\"work-effect-contracts.json\"]}",
                extraDocuments:new Dictionary<string,string>{{"work-effect-contracts.json",effects.ToString()}},
                configure:(tech,setup)=>{
                    var foundation=tech.Get("technologies")[0];
                    foundation.Set("unlocks",Json.Parse("[\"unlock.tower\",\"unlock.manufacture\"]"));
                    var advanced=foundation.Clone();advanced.Set("id",JsonValue.Of("research.advanced"));
                    advanced.Set("prerequisites",Json.Parse("[\"research.foundation\"]"));advanced.Set("unlocks",JsonValue.NewArray());
                    advanced.Set("nodes",Json.Parse("[{\"executor\":\"work.timed\",\"id\":\"confirm\",\"duration\":{\"numerator\":2,\"denominator\":1,\"unit\":\"minute\"}}]"));
                    tech.Get("technologies").Add(advanced);
                    tech.Set("technology_transformation_unlocks",Json.Parse("[{\"id\":\"unlock.manufacture\",\"definition\":\"contract.make\"}]"));
                    if(direct){tech.Set("independent_technologies",tech.Get("technologies"));tech.Set("technologies",JsonValue.NewArray());tech.Set("technology_completion_effects",JsonValue.NewArray());}
                    var recipes=setup.Get("transformations");var power=recipes[0].Clone();power.Set("id",JsonValue.Of("lab.power"));recipes.Add(power);
                    var reward=recipes[0];reward.Set("operating_inputs",JsonValue.NewArray());reward.Set("operating_ports",JsonValue.NewObject());
                    reward.Set("outputs",JsonValue.NewObject());reward.Set("output_ports",JsonValue.NewObject());reward.Set("entity_output",JsonValue.Of("contract.result"));reward.Set("output_home",JsonValue.Of("unassigned"));
                    reward.Set("inputs",Json.Parse("{\"supply\":3}"));reward.Set("input_ports",Json.Parse("{\"supply\":\"material.input\"}"));
                    reward.Set("tools",Json.Parse("{\"supply\":1}"));reward.Set("tool_ports",Json.Parse("{\"supply\":\"tool.input\"}"));
                    reward.Set("duration",Json.Parse("{\"numerator\":6,\"denominator\":1,\"unit\":\"minute\"}"));
                    var parts=setup.Get("entities")[1].Get("components");parts.Get("transformer").Set("transformations",Json.Parse("[\"lab.power\",\"contract.make\"]"));
                    parts.Set("completion_probe",Json.Parse("{\"id\":\"completion.probe\"}"));
                },retryProfiles:retry.ToString(),worldEdit:root=>{
                    if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
                });
        }
        private static SimGame New(GameFactory factory)=>ConstructionPlanTests.NewPoints(factory);
        private static TechnologySnapshot Knowledge(SimGame game,string id="research.foundation",int seat=0)
        {var values=new List<TechnologySnapshot>();game.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(values);return values.Single(x=>x.Technology==id);}
        private static ActivityNodeView Node(SimGame game,int order)
        {var values=new List<ActivityNodeView>();game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order,values);return values.Single(x=>x.Id=="research");}
        private static UtilityDemandSnapshot Demand(SimGame game)
        {if(!game.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(Entity(game,"contract.factory"),new Code("lab.utility"),out var facts))throw new InvalidOperationException("missing actual utility view");return facts;}
        private static CommandResult Input(Check c,SimGame game,Command command,Code? expected=null)
        {
            ulong hash=game.Hash();c.Equal(expected??Code.None,game.Seat(0).Preview(command),"normal engine admission/refusal");
            c.Equal(hash,game.Hash(),"preview changes no utility/research state");int sequence=game.Seat(0).Submit(command);Step(game);
            long cursor=0;var answers=new List<CommandResult>();game.Seat(0).ReadResults(ref cursor,answers);var result=answers.Single(x=>x.Sequence==sequence);
            c.Equal(expected??Code.None,result.Refusal,"actual command agrees with preview");return result;
        }
        private static EnqueueTransformationCommand Queue(SimGame game,string recipe)=>new EnqueueTransformationCommand{Transformer=Entity(game,"contract.factory"),Capability=new Code("contract.process"),Transformation=recipe};
        private static void Until(SimGame game,SimGame twin,Func<bool> done,string stage)
        {
            for(int i=0;i<700&&!done();i++)Step(game,twin);
            if(!done())throw new InvalidOperationException(stage+" stalled at "+game.Clock.StepIndex+" known="+Knowledge(game).Known+" utility="+Demand(game).Blocker+" phase="+Process(game).Phase+" fuel="+Stock(game,"contract.factory"));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual engine source identity");c.Equal(154,EngineInfo.SaveFormat,"original research/work/utility codecs");
            foreach(bool direct in new[]{false,true}){Missing(c,direct);foreach(bool half in new[]{false,true})Physical(c,direct,half);}
            Faults(c);
        }
        private static void Missing(Check c,bool direct)
        {
            var game=New(Factory(direct,false,out _,out _,"missing.consumer"));ulong hash=game.Hash();
            var facts=new List<WorkRequirementView>();c.Equal(Code.None,game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("research.foundation",Entity(game,"contract.factory"),facts),"absent named runtime remains inspectable");
            c.That(facts.Any(x=>x.Result.Blocker==UtilityWorkRequirementDeclaration.MissingConsumer),"absent consumer is typed pending, never another provider");
            c.Equal(hash,game.Hash(),"missing provider diagnostic remains pure");
        }
        private static void Physical(Check c,bool direct,bool half)
        {
            var factory=Factory(direct,half,out _,out var effect);var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
            c.That(effect.ActualAlias&&effect.Independent==direct&&effect.IndependentTechnology==direct,"actual native/direct technology and completion aliases, existing sole owners");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId)&&!game.Modules.Contains(TileNavigationModule.ModuleId),"utility research is resource-only non-tile gameplay");
            var limits=new ReplayLimits(6*1024*1024,6000);var replay=game.RecordReplay(limits);
            Input(c,game,Queue(game,"contract.make"),TechnologyCodes.Locked);c.That(Process(game).QueueCount==0,"locked manufacture allocates no queue");
            Input(c,game,new PostOrderCommand{Plan="research.advanced",Target=Entity(game,"contract.factory"),Priority=70},TechnologyCodes.MissingPrerequisite);
            Input(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"machine"),Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("inspect"),new WorkerRoleId("haul")},"lab.research-area",true,true,WorkAssignmentMode.All)});
            Input(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"lab.carrier"),Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul")},"lab.carrier-area",true,true,WorkAssignmentMode.All)});
            Input(c,game,Queue(game,"lab.power"));
            int order=checked((int)Input(c,game,new PostOrderCommand{Plan="research.foundation",Target=Entity(game,"contract.factory"),Priority=70}).Arg0);
            Until(game,null,()=>Node(game,order).ProductiveSteps>0&&!Knowledge(game).Known,"physical traveled utility research");
            var retained=Node(game,order);var utility=Demand(game);c.That(retained.Worker.IsValid&&utility.Consumed.Count>0,"actual worker and original utility burn");
            Input(c,game,new ConfigureOperationCommand{Entity=Entity(game,"contract.factory"),Operation=new Code("contract.process"),Enabled=false,Target=Fix.One});
            Until(game,null,()=>Node(game,order).State==NodeState.Paused,"shutdown settles actual research");
            var held=Node(game,order).Progress;long burnt=Demand(game).ElapsedTicks;var twin=factory.Load(game.Save());
            for(int i=0;i<3;i++)Step(game,twin);c.That(Node(game,order).Progress.Equals(held)&&Demand(game).ElapsedTicks==burnt&&!Knowledge(game).Known,"shutdown earns neither utility nor knowledge");
            long at=game.Clock.StepIndex;ulong pausedHash=game.Hash();game.Clock.Pause();twin.Clock.Pause();Step(game,twin);c.That(at==game.Clock.StepIndex&&pausedHash==game.Hash(),"sole clock pauses both owners");game.Clock.Resume();twin.Clock.Resume();
            Input(c,game,new ConfigureOperationCommand{Entity=Entity(game,"contract.factory"),Operation=new Code("contract.process"),Enabled=true,Target=Fix.One});
            Until(game,null,()=>Node(game,order).Blocker==UtilityCodes.Shortage,"finite physical utility shortage");
            var shortage=Node(game,order).Progress;twin=factory.Load(game.Save());for(int i=0;i<3;i++)Step(game,twin);
            c.That(Node(game,order).Progress.Equals(shortage)&&!Knowledge(game).Known,"fuel exhaustion cannot grant remaining research");
            game.Seat(0).View.TryFindResource("supply",out var supply);
            Input(c,game,new HaulResourcesCommand{Source=Entity(game,"source"),Destination=Entity(game,"contract.factory"),Worker=Entity(game,"lab.carrier"),DestinationPort=new Code("operating.input"),Resource=supply,Ceiling=Fix.FromInt(8),Priority=90});
            c.That(!Knowledge(game).Known,"refuel input grants no instant delivery or knowledge");twin=factory.Load(game.Save());
            Until(game,twin,()=>Knowledge(game).Known,"saved physical refuel and earned completion");
            c.That(!Knowledge(game,seat:1).Known&&retained.Progress.WholeTicks<retained.RequiredTicks&&Knowledge(game).CompletedTick.HasValue,"only own original ledger earns complete knowledge, retained partial fact detached");
            long learned=Knowledge(game).CompletedTick.Value;
            Input(c,game,new PostOrderCommand{Plan="research.advanced",Target=Entity(game,"contract.factory"),Priority=70});twin=factory.Load(game.Save());
            Until(game,twin,()=>Knowledge(game,"research.advanced").Known,"same dependency graph earns advanced research");
            c.That(Knowledge(game,"research.advanced").CompletedTick>learned&&!Knowledge(game,"research.advanced",1).Known,"dependent completion later and private");
            var utilityConsumed=Demand(game).Consumed.AmountOf(supply);
            Input(c,game,new CancelTransformationCommand{Transformer=Entity(game,"contract.factory"),Capability=new Code("contract.process"),Index=0});
            c.That(Demand(game).Blocker==UtilityCodes.Inactive,"cancellation does not fabricate running utility");
            Input(c,game,Queue(game,"contract.make"));c.That(game.Seat(0).Snapshot.Entities.All(x=>x.Def!="contract.result"),"earned unlock and queue create no instant product");
            Until(game,null,()=>Process(game).Phase==TransformationPhase.Working,"earned recipe physically acquires input and reusable tool");var process=Process(game);twin=factory.Load(game.Save());
            at=game.Clock.StepIndex;game.Clock.Pause();twin.Clock.Pause();Step(game,twin);c.Equal(at,game.Clock.StepIndex,"manufacture uses the same paused clock");game.Clock.Resume();twin.Clock.Resume();
            Until(game,twin,()=>Process(game).QueueCount==0&&game.Seat(0).Snapshot.Entities.Count(x=>x.Def=="contract.result")==1,"saved unlocked transformation executes once");
            c.That(process.Progress.WholeTicks<process.Required&&retained.RequiredTicks==game.SimulationTime.Ticks(GameDuration.Minutes(30)),"authored research/manufacture timing from same converter");
            c.Equal(Fix.FromInt(36),Stock(game,"source")+Stock(game,"machine")+Stock(game,"lab.carrier")+Stock(game,"contract.factory")+utilityConsumed+Fix.FromInt(11),"opening supply conserved through8research/3manufacture/original utility consumption");
            c.Equal(TechnologyCodes.Known,game.Seat(0).Preview(new PostOrderCommand{Plan="research.foundation",Target=Entity(game,"contract.factory"),Priority=70}),"completion never grants or spends twice");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"same learned/work/processor/resource codecs retain final state");
            c.Equal(isolated,idle.Hash(),"interleaved optional utility/progression session isolated");
            c.That(new ReplayVerifier().Verify(factory,replay.Finish(),limits).Success,"normal refusal/shutdown/refuel/research/unlocked manufacture replay");
        }
        private static void Faults(Check c)
        {
            foreach(UtilityResearchFault fault in Enum.GetValues(typeof(UtilityResearchFault)))
            {
                var factory=Factory(true,false,out var module,out _);var game=New(factory);module.Arm(fault);
                c.Throws<InvalidOperationException>(()=>Demand(game),"actual independent utility rejects "+fault);
                c.That(game.Fault!=null&&!KnowledgeAvailable(game),"first fault withholds learned diagnostics");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted utility cannot checkpoint");
            }
        }
        private static bool KnowledgeAvailable(SimGame game)
        {try{Knowledge(game);return true;}catch(InvalidOperationException){return false;}}
    }
}
