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
    // JSON composition, ordinary player commands/Update/facts/saves and negative instrumentation only.
    internal static class GeometryProviderObservationTests
    {
        internal const string Case = "Geometry providers: construction recovery and physical rollout observe actual state before publication";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe initialize, out ConstructionContentContractsProofModule proof,
            bool direct, bool half = false, string phase = null, string fault = null,
            Action<JsonValue> edit = null, Action<JsonValue> worldEdit = null, string recovery = "point.recovery")
            => ConstructionContentContractTests.Factory(initialize, out proof, direct, edit,
                worldEdit: root => {
                    if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                    root.Get("entities").Add(Json.Parse("{\"id\":\"geometry.assembler\",\"kind\":\"entity\",\"components\":{\"point_presence\":{\"space\":\"field.points\",\"position\":40},\"storage\":{\"capacity_policy\":\"fixture.count.40\",\"public\":false,\"initial_stock\":{}},\"transformer\":{\"automatic_supply\":true,\"wip_capacity\":\"fixture.count.20\",\"transformations\":[\"geometry.build\"],\"states\":{\"working\":\"processing\",\"stopped\":\"blocked\"},\"operation\":{\"id\":\"transformation\",\"enabled\":true,\"target\":1,\"minimum\":1,\"maximum\":1,\"unit\":\"unit.work-rate\"}}}}"));
                    root.Get("entities").Add(Json.Parse("{\"id\":\"geometry.result\",\"kind\":\"entity\",\"components\":{\"point_presence\":{\"space\":\"field.points\",\"position\":0}}}"));
                    root.Set("transformations", Json.Parse("[{\"id\":\"geometry.build\",\"mode\":\"batch\",\"cancel_policy\":\"return_inputs\",\"operating_inputs\":[],\"tools\":{},\"inputs\":{\"supply\":2},\"entity_output\":\"geometry.result\",\"output_home\":\"unassigned\",\"duration\":{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"}}]"));
                    root.Get("entities")[root.Get("entities").Count-2].Get("components").Set("inventory_port",Json.Parse("[{\"id\":\"geometry.feed\"}]"));
                    var recipe=root.Get("transformations")[0];
                    recipe.Set("input_ports",Json.Parse("{\"supply\":\"geometry.feed\"}"));
                    recipe.Set("output_ports",JsonValue.NewObject()); recipe.Set("tool_ports",JsonValue.NewObject());
                    recipe.Set("operating_ports",JsonValue.NewObject()); recipe.Set("tool_consumption",JsonValue.NewObject());
                    StartupContentAuthoring.Add(root, "geometry.assembler", "geometry.assembler", 0,
                        Json.Parse("{\"space\":\"field.points\",\"position\":40}"));
                    worldEdit?.Invoke(root);
                }, geometryPhase: phase, geometryFault: fault, recovery: recovery);
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static int Count(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Count(x => x.Def == prototype);
        private static TransformationManagementView Production(SimGame game) => game.Seat(0).View.Feature<TransformationManagementView>();
        private static EntityOutputPreview Preview(SimGame game) => Production(game).PreviewEntityOutput(Entity(game,"geometry.assembler"), new Code("transformation"), "geometry.build");
        private static List<PlanView> Plans(SimGame game)
        { var values = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(values); return values; }
        private static List<ActivityNodeView> Nodes(SimGame game)
        { var values = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(Plans(game).Single().Order, values); return values; }
        private static void Input(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary engine admission");
            c.Equal(hash, game.Hash(), "pure preview changes no state"); game.Seat(0).Submit(command); Step(game);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 600 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage+" stalled at "+game.Clock.StepIndex+" fault="+game.Fault+
                " source="+Stock(game,"source")+" plans="+string.Join(";",Plans(game).Select(x=>x.State+"/"+x.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual guarded geometry identity");
            c.Equal(154, EngineInfo.SaveFormat, "same original spatial/work/stock codecs");
            foreach (bool direct in new[]{false,true})
            {
                Early(c,direct); Late(c,direct);
                foreach (bool half in new[]{false,true}) Physical(c,direct,half);
            }
        }
        private static void Early(Check c, bool direct)
        {
            foreach (string phase in new[]{"construction-definition","recovery-definition","rollout-definition"})
                foreach (string fault in new[]{"state","clock","throw","state-throw","clock-throw"})
                {
                    var initialize = new InitializeProbe(); Exception refused = null;
                    try { New(Factory(initialize,out _,direct,phase:phase,fault:fault)); } catch(Exception error) { refused=error; }
                    c.That(refused is InvalidOperationException,"actual geometry admission rejects "+phase+"/"+fault);
                    c.Equal(0,initialize.Initializations,"no Initialize before actual provider refusal");
                    if(fault.EndsWith("throw",StringComparison.Ordinal)) c.Equal("proof.geometry-first-error",refused?.Message,"original callback exception survives mutation");
                }
            foreach(string invalid in new[]{"missing-space","wrong-site","wrong-output"})
            {
                var initialize=new InitializeProbe(); Exception refused=null;
                try { New(Factory(initialize,out _,direct,
                    edit: root => { if(invalid=="wrong-site") root.Get("entities")[1].Get("components").Get("point_presence").Set("space",JsonValue.Of("geometry.other")); },
                    worldEdit: root => {
                        if(invalid=="missing-space") root.Get("scenarios")[0].Set("space",JsonValue.Null());
                        else {
                            root.Get("point_spaces").Add(Json.Parse("{\"id\":\"geometry.other\",\"extent\":100,\"access_open\":true,\"rollout_offset\":1}"));
                            if(invalid=="wrong-output") root.Get("entities")[root.Get("entities").Count-1].Get("components").Get("point_presence").Set("space",JsonValue.Of("geometry.other"));
                        }
                    })); } catch(Exception error) { refused=error; }
                c.That(refused is ContentException,"missing/incompatible geometry refuses without fallback "+invalid);
                c.Equal(0,initialize.Initializations,"geometry requirement precedes initialization "+invalid);
            }
            var optional=New(Factory(new InitializeProbe(),out _,direct,recovery:null));
            c.That(optional.Seat(0).View.Feature<ConstructionView>().RecoveryContainer==null,"explicit absent recovery fabricates no container");
            c.Equal(optional.Hash(),Factory(new InitializeProbe(),out _,direct,recovery:null).Load(optional.Save()).Hash(),"optional recovery selection retains original save");
        }
        private static void Late(Check c, bool direct)
        {
            foreach (string phase in new[]{"rollout-definition","rollout","placement"})
                foreach (string fault in new[]{"state","clock","throw","state-throw","clock-throw"})
                {
                    var factory=Factory(new InitializeProbe(),out var proof,direct);
                    var idle=New(factory); ulong isolated=idle.Hash(); var game=New(factory);
                    var retained=Preview(game); c.Equal(Code.None,retained.Refusal,"healthy original preview");
                    proof.ArmGeometryFault(phase,fault); EntityOutputPreview published=default; Exception refused=null;
                    try { published=Preview(game); } catch(Exception error) { refused=error; }
                    c.That(refused is InvalidOperationException&&game.Fault!=null,"actual callback failure faults without healthy geometry "+phase+"/"+fault);
                    c.That(published.Output==null&&published.Placement==null,"failed provider publishes no placement prefix");
                    if(fault.EndsWith("throw",StringComparison.Ordinal)) c.Equal("proof.geometry-first-error",refused?.Message,"runtime first callback error retained");
                    c.Throws<InvalidOperationException>(()=>game.Save(),"faulted result geometry cannot checkpoint");
                    c.Throws<InvalidOperationException>(()=>Production(game).PreviewEntityOutput(EntityId.None,new Code("transformation"),"missing"),"cached missing query obeys first-fault cutoff");
                    c.Equal("geometry.result",retained.Output,"retained prior fact independent of later fault");
                    c.Equal(isolated,idle.Hash(),"actual fault does not touch interleaved spatial session");
                }
        }
        private static void Physical(Check c, bool direct, bool half)
        {
            var factory=Factory(new InitializeProbe(),out _,direct,half); var game=New(factory); var idle=New(factory); ulong isolated=idle.Hash();
            c.That(game.Observer.Space is PointWorldSnapshot&&!game.Modules.Contains(TileNavigationModule.ModuleId)&&!game.Modules.Contains(TycoonModule.ModuleId),"non-tile physical game needs no tile fallback or business");
            var limits=new ReplayLimits(4*1024*1024,4000); var replay=game.RecordReplay(limits);
            Input(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"machine"),Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("inspect"),new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            var args=Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":12}}");
            Input(c,game,new SubmitPlanCommand{Intent=new PlanIntent("build:point.tower",args,"geometry.physical",70,false)});
            c.That(Count(game,"point.tower")==0&&Count(game,"point.tower-site")==1,"admission creates only physical site");
            Until(game,null,()=>Nodes(game).Any(x=>x.Id=="build"&&x.ProductiveSteps>0),"supplied staffed building");
            var twin=factory.Load(game.Save()); long at=game.Clock.StepIndex; ulong hash=game.Hash();
            game.Clock.Pause(); twin.Clock.Pause(); Step(game,twin); c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"sole clock pause grants no work or geometry");
            game.Clock.Resume(); twin.Clock.Resume(); Until(game,twin,()=>Plans(game).Single().State==PlanState.Done,"saved physical construction");
            c.That(Count(game,"point.tower")==1&&Count(game,"point.tower-site")==0&&Stock(game,"source")==Fix.FromInt(12),"once-only real eight-unit construction");
            var retained=Preview(game); c.Equal(Code.None,retained.Refusal,"actual provider determines result placement");
            c.Equal(Fix.FromInt(41),((PointPlacement)retained.Placement).Position,"same provider owns result offset");
            c.Equal(Codes.NotFound,game.Seat(1).View.Feature<TransformationManagementView>().PreviewEntityOutput(Entity(game,"geometry.assembler"),new Code("transformation"),"geometry.build").Refusal,"foreign geometry preview stays private");
            Input(c,game,new EnqueueTransformationCommand{Transformer=Entity(game,"geometry.assembler"),Capability=new Code("transformation"),Transformation="geometry.build",Count=2});
            c.Equal(0,Count(game,"geometry.result"),"enqueue cannot grant instant result");
            twin=factory.Load(game.Save()); Until(game,twin,()=>Count(game,"geometry.result")==1,"first timed supplied result"); long first=game.Clock.StepIndex;
            twin=factory.Load(game.Save()); Until(game,twin,()=>Count(game,"geometry.result")==2,"second sequential physical result");
            c.That(game.Clock.StepIndex-first>=game.SimulationTime.Ticks(GameDuration.Minutes(6)),"second result earns its own authored duration");
            c.That(Stock(game,"source")==Fix.FromInt(8)&&Stock(game,"machine")==Fix.Zero,"four manufactured inputs consumed once and no stranded carrier stock");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"original geometry/result/work/clock save hash");
            c.Equal(Fix.FromInt(41),((PointPlacement)retained.Placement).Position,"retained geometry remains immutable");
            c.Equal(isolated,idle.Hash(),"interleaved healthy session retains independent state");
            c.That(new ReplayVerifier().Verify(factory,replay.Finish(),limits).Success,"public build and sequential physical manufacturing replay");
        }
    }
}
