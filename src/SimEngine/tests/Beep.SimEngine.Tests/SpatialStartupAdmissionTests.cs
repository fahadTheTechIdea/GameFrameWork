using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Actual JSON/master, public seat intent/views/Update and initialization counters only; no host admission rules.
    internal static class SpatialStartupAdmissionTests
    {
        internal const string Case="Spatial startup admission: required providers and original revisions refuse before initialization without fallback";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static void Empty(JsonValue root,string profile)
        {
            root.Set("entities",JsonValue.NewArray());root.Set("entity_setups",JsonValue.NewArray());
            root.Set("world_setup_profiles",Json.Parse("[{\"id\":\""+profile+"\",\"entries\":[],\"includes\":[]}]"));
        }
        private static GameFactory Points(bool half,out ComponentCompositionModule proof,Action<JsonValue,JsonValue> edit=null)
            =>ComponentCompositionTests.Factory(half,out proof,edit,FrameworkProofContent.Read("spatial-startup-admission.json"));
        private static SimGame NewPoints(GameFactory factory)=>ConstructionPlanTests.NewPoints(factory);
        private static SimGame NewSpatial(GameFactory factory)=>factory.NewGame(new NewGameRequest{
            Scenario="spatial-composition",Seed=921,Seats={Controller.Human}});
        private static void Reach(SimGame g,SimGame twin,Func<bool> done,string stage)
        {
            for(int i=0;i<320&&!done();i++)Step(g,twin);
            if(!done()){
                var orders=new List<OrderView>();g.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                throw new InvalidOperationException(stage+" at="+g.Clock.StepIndex+" orders="+string.Join(";",orders.Select(o=>o.State+"/"+o.Blocker)));
            }
        }
        private static bool Active(SimGame g,EntityId target)=>g.Seat(0).View.Feature<WorkView>().ActiveOrders(target)>0;
        private static void PointPhysical(Check c,bool half)
        {
            var f=Points(half,out var proof);var g=NewPoints(f);
            var idle=NewPoints(Points(half,out _));ulong isolated=idle.Hash();
            c.That(proof.Initialize.Initializations==1&&proof.Requirements.Calls==1&&proof.Audit.Calls==1,"all authored unused spatial prerequisites admitted before one Initialize");
            c.That(g.Observer.Space is PointWorldSnapshot&&!g.Modules.Contains(TileNavigationModule.ModuleId)
                &&!g.Modules.Contains(TycoonModule.ModuleId),"actual point geometry, no tile/default-map/business fallback");
            c.That(!g.Seat(0).Snapshot.Entities.Any(e=>e.Def=="composition.unused"),"admission does not instantiate unused metadata");
            var limits=new ReplayLimits(2*1024*1024,2000);
            using(var record=g.RecordReplay(limits)){
                var target=Entity(g,"destination");int order=checked((int)PermanentRemovalTests.Submit(c,g,
                    new PostOrderCommand{Plan="inspect",Target=target,Priority=60}).Arg0);
                c.That(Active(g,target),"inspection remains real timed work");
                var privateNodes=new List<ActivityNodeView>();c.That(!g.Seat(1).View.Feature<WorkView>().TryCopyNodes(order,privateNodes)&&privateNodes.Count==0,"private work withheld");
                var twin=f.Load(g.Save());int calls=proof.Requirements.Calls;
                long at=g.Clock.StepIndex;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);c.Equal(at,g.Clock.StepIndex,"one clock pauses actual work");g.Clock.Resume();twin.Clock.Resume();
                Reach(g,twin,()=>!Active(g,target),"physical point travel/inspection");
                var retained=g.Seat(0).Snapshot.Entities.Single(e=>e.Def=="machine").Spatial;
                c.Equal(Fix.FromInt(18),((PointSnapshot)retained).Position,"machine reaches actual authored destination before completion");
                c.Equal(calls,proof.Requirements.Calls,"Update never reruns metadata admission");
                PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand{Source=Entity(g,"source"),Destination=target,Worker=Entity(g,"machine"),
                    Resource=PermanentRemovalTests.Supply(g),Ceiling=Fix.FromInt(4),Priority=80});
                twin=f.Load(g.Save());
                Reach(g,twin,()=>((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(e=>e.Def=="machine").Spatial).Position!=((PointSnapshot)retained).Position,"new physical travel differs from retained view");
                var moving=g.Seat(0).Snapshot.Entities.Single(e=>e.Def=="machine").Spatial;
                Reach(g,twin,()=>Stock(g,"destination")==Fix.FromInt(4),"real pickup/travel/delivery after prerequisite admission");
                c.Equal(Fix.FromInt(16),Stock(g,"source"),"actual source debited once");
                c.Equal(Fix.FromInt(4),Stock(g,"destination"),"actual destination credited once");
                c.That(((PointSnapshot)retained).Position==Fix.FromInt(18)&&!retained.Matches(moving),"retained view remains unchanged while actual travel differs");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"complete point physical state reloads");
                c.Equal(isolated,idle.Hash(),"separate actual session isolated");
                c.That(new ReplayVerifier().Verify(f,record.Finish(),limits).Success,"ordinary work/hauling/time stream replays");
            }
        }
        private static GameFactory Native(bool half,ComponentCompositionModule proof)
        {
            var data=Json.Parse(FrameworkProofContent.Read("spatial-startup-admission.json"));
            var parts=data.Get("entities")[0].Get("components");parts.Remove("point_presence");
            var requirements=parts.Get("composition_requirements");requirements.Set("space",JsonValue.Of("survey.tiles"));
            requirements.Set("requires",Json.Parse("[\"layers\",\"placement\",\"construction\",\"templates\",\"recovery\",\"rollout\",\"interaction\",\"overlap\",\"visibility-invalidation\",\"resources\",\"line-geometry\",\"line-removal\",\"network-graph\",\"networks\",\"tile-routing\"]"));
            requirements.Set("required_services",Json.Parse("[\"spatial-space\",\"line-geometry\",\"line-removal\",\"network-graph\",\"networks\"]"));
            return AuthoredSegmentWorkflowTests.Factory(extraModule:proof,extraDocument:"spatial-startup-admission.json",extraJson:data.ToString(),
                worldAuthor:root=>{if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));});
        }
        private static void NativePhysical(Check c,bool half)
        {
            var proof=new ComponentCompositionModule(true);var f=Native(half,proof);var g=LineDefinitionContractTests.New(f);
            var idle=LineDefinitionContractTests.New(Native(half,new ComponentCompositionModule(true)));ulong isolated=idle.Hash();
            c.That(proof.Initialize.Initializations==1&&proof.Requirements.Calls==1&&proof.Audit.Calls==1,"all native required spatial/service aliases admitted once");
            c.That(g.Observer.Space is TileWorldSnapshot&&g.Modules.Contains(TileNavigationModule.ModuleId),"only authored native provider/navigation selected");
            var limits=new ReplayLimits(2*1024*1024,2000);
            using(var record=g.RecordReplay(limits)){
                long plan=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=SpatialCorridorExecutionTests.Intent(false,false)}).Arg0;
                c.That(PermanentRemovalTests.Plan(g,plan).State!=PlanState.Done,"prerequisite admission never completes infrastructure instantly");
                var twin=f.Load(g.Save());int calls=proof.Requirements.Calls;
                Reach(g,twin,()=>PermanentRemovalTests.Plan(g,plan).State==PlanState.Done,"physically supplied native corridor");
                c.Equal(Fix.FromInt(34),LineDefinitionContractTests.Steel(g),"six real construction supplies consumed once");
                c.Equal(calls,proof.Requirements.Calls,"physical native work does not repeat prerequisite validation");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"completed native state reloads");
                c.Equal(isolated,idle.Hash(),"native session isolated");
                c.That(new ReplayVerifier().Verify(f,record.Finish(),limits).Success,"native physical commands/time replay");
            }
        }
        private static void Revision(Check c,string mode,bool independent)
        {
            var proof=new SpatialPresenceCompositionModule();Exception actual=null;
            try{NewSpatial(SpatialPresenceCompositionTests.Factory(proof,mode,independent,root=>Empty(root,"spatial.empty")));}catch(Exception error){actual=error;}
            c.That(actual is InvalidOperationException,"actual selected revision refuses "+mode);
            c.Equal(0,proof.Initialize.Initializations,"even consumer-free spatial session cannot Initialize with invalid revision");
            if(mode.EndsWith("throw",StringComparison.Ordinal))c.Equal("proof.space-revision-first-error",actual?.Message,"revision first callback error retained");
            else if(mode=="revision-negative")c.That(actual.Message.Contains("nonnegative"),"actual negative revision diagnosed");
            else c.That(actual.Message.Contains("observation"),"actual state/clock mutation diagnosed by original observer");
        }
        private static void Missing(Check c,string capability,bool service)
        {
            ComponentCompositionModule proof=null;Exception actual=null;
            try{
                var f=Points(false,out proof,(root,doc)=>{
                    Empty(root,"points.start");var scenario=root.Get("scenarios")[0];var parts=doc.Get("entities")[0].Get("components");
                    parts.Remove("point_presence");var requirements=parts.Get("composition_requirements");
                    requirements.Set("requires",JsonValue.NewArray());requirements.Set("required_services",JsonValue.NewArray());
                    if(service){
                        requirements.Get("required_services").Add(JsonValue.Of(capability));
                        if(capability=="spatial-space")scenario.Set("space",JsonValue.Null());
                        else{
                            requirements.Set("space",JsonValue.Of("negative.space"));scenario.Set("space",JsonValue.Of("negative.space"));
                            doc.Set("bare_composition_spaces",Json.Parse("[{\"id\":\"negative.space\"}]"));
                        }
                    }
                    else{
                        requirements.Set("space",JsonValue.Of("negative.space"));scenario.Set("space",JsonValue.Of("negative.space"));
                        doc.Set("bare_composition_spaces",Json.Parse("[{\"id\":\"negative.space\"}]"));
                        requirements.Get("requires").Add(JsonValue.Of(capability));
                    }
                });NewPoints(f);
            }catch(Exception error){actual=error;}
            c.That(service?actual is InvalidOperationException:actual is ContentException,"missing declared "+(service?"service ":"spatial capability ")+capability+" refuses");
            c.That(proof!=null&&proof.Initialize.Initializations==0,"missing required provider precedes all Initialize and fallback");
            if(actual==null)throw new InvalidOperationException("declared negative fixture actually has provider "+capability);
            if(service)c.That(actual.Message.Contains("missing service"),"original generic service freeze owns refusal");
            else c.That(actual.Message.Contains("missing required contract "+capability)&&proof.Requirements.Calls==1,"original unused component admission owns exact missing capability");
        }
        private static void EmptyAndLocal(Check c)
        {
            var proof=new SpatialPresenceCompositionModule();var f=SpatialPresenceCompositionTests.Factory(proof,"unused-valid",edit:root=>Empty(root,"spatial.empty"));var g=NewSpatial(f);
            c.That(proof.Initialize.Initializations==1&&g.Observer.Space is TileWorldSnapshot&&g.Seat(0).Snapshot.Entities.Count==0,"valid consumer-free selected space initializes without inventing entities");
            var twin=f.Load(g.Save());Step(g,twin);c.Equal(g.Hash(),twin.Hash(),"empty valid actual space survives save/update");
            var localFactory=SpatialPresenceCompositionTests.Factory(new SpatialPresenceCompositionModule(),"local");var local=NewSpatial(localFactory);
            c.That(local.Observer.Space==null&&local.Seat(0).Snapshot.Entities.Single().Spatial==null,"unselected geometry stays optional without default map");
            var localTwin=localFactory.Load(local.Save());Step(local,localTwin);c.Equal(local.Hash(),localTwin.Hash(),"intentional nonspatial session remains durable");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current spatial admission identity");c.Equal(154,EngineInfo.SaveFormat,"no revision/provider payload duplicate");
            foreach(bool independent in new[]{false,true})foreach(string mode in new[]{"revision-negative","revision-state","revision-clock","revision-throw","revision-state-throw","revision-clock-throw"})Revision(c,mode,independent);
            foreach(string capability in new[]{"layers","placement","construction","templates","recovery","rollout","interaction","overlap","visibility-invalidation","line-geometry","line-removal","network-graph","networks","tile-routing"})Missing(c,capability,false);
            foreach(string service in new[]{"spatial-space","line-geometry","line-removal","network-graph","networks"})Missing(c,service,true);
            foreach(string mode in new[]{"missing-provider","wrong-provider-id","wrong-geometry","missing-layers"}){
                var proof=new SpatialPresenceCompositionModule();c.Throws<ContentException>(()=>NewSpatial(SpatialPresenceCompositionTests.Factory(proof,mode)),"existing required native contract "+mode);c.Equal(0,proof.Initialize.Initializations,"native missing provider fails before Initialize");
            }
            EmptyAndLocal(c);
            foreach(bool half in new[]{false,true}){PointPhysical(c,half);NativePhysical(c,half);}
        }
    }
}
