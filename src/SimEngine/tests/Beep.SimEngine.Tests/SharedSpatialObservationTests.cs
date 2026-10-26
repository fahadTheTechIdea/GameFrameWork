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
    // Pure authored inputs, actual public player commands/views and Update. Fault probes are engine callbacks only.
    internal static class SharedSpatialObservationTests
    {
        internal const string Case="Shared spatial observation: original codecs and revisions guard physical native and point callbacks";
        internal static void Register(){TestRunner.Add(Case,Run);TestRunner.Add(NestedSpatialObservationTests.Case,NestedSpatialObservationTests.Run);}
        private static void Quantum(JsonValue root,bool half)
        { if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30")); }
        private static GameFactory AccessFactory(bool point,bool half,ContentAccessContractModule probe)
        {
            if(!point)return ContentAccessContractTests.Create(probe,edit:(root,_)=>{
                foreach(var id in new[]{0,1})root.Get("proof_access_rules")[id].Set("enabled",JsonValue.Of(false));
            },worldEdit:root=>Quantum(root,half));
            return ConstructionPlanTests.Points(additionalModule:probe,
                moduleDeclaration:"{\"id\":\"proof.access-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"}],\"documents\":[\"shared-spatial-access.json\"]}",
                moduleDocument:FrameworkProofContent.Read("shared-spatial-access.json"),
                extraDocuments:new Dictionary<string,string>{["framework/access.json"]="{}"},
                additionalDocuments:new Dictionary<string,string>{["shared-spatial-observation.json"]=FrameworkProofContent.Read("shared-spatial-observation.json")},
                scenarioEdit:root=>root.Get("modules").Add(JsonValue.Of(probe.Id)),worldEdit:root=>Quantum(root,half));
        }
        private static GameFactory EffectFactory(bool half,WorkEffectContractsModule probe)
            =>WorkEffectContractTests.Factory(probe,additionalDocuments:new Dictionary<string,string>{
                ["shared-spatial-observation.json"]=FrameworkProofContent.Read("shared-spatial-observation.json")},
                worldEdit:root=>Quantum(root,half));
        private static SimGame New(GameFactory factory,bool point)=>point?ConstructionPlanTests.NewPoints(factory):PhysicalLineRemovalTests.New(factory);
        private static JsonValue Location(bool point,int x)=>point?Json.Parse("{\"space\":\"field.points\",\"position\":"+x+"}")
            :TilePlacement.Arguments("colony.tiles",new TileCoord(x,16));
        private static ConstructCommand Build(bool point)=>new ConstructCommand{Def=point?"point.tower":"tower",Placement=Location(point,10),Priority=70};
        private static bool Exists(SimGame g,string prototype)=>g.Seat(0).Snapshot.Entities.Any(e=>e.Def==prototype);
        private static Fix StockTotal(SimGame g,bool point)
        {
            Fix total=Fix.Zero;string resource=point?"supply":"steel";
            foreach(var entity in g.Seat(0).Snapshot.Entities){
                var rows=new List<Facet>();g.Seat(0).View.Inspect(entity.Id,rows);
                foreach(var row in rows)if(row.Key==Storage.FacetStock&&row.Subject==resource)total+=row.Value;
            }
            return total;
        }
        private static void Reach(SimGame g,SimGame twin,Func<bool> done,string stage)
        {
            for(int i=0;i<900&&!done();i++)Step(g,twin);
            if(!done()) {
                var plans=new List<PlanView>();g.Seat(0).View.Feature<WorkView>().CopyPlans(plans);
                throw new InvalidOperationException(stage+" at="+g.Clock.StepIndex+" plans="+string.Join(";",plans.Select(p=>p.State+"/"+p.Blocker)));
            }
        }
        private static void Physical(Check c,bool point,bool half)
        {
            Console.WriteLine("  stage construction point="+point+" half="+half);
            var probe=new ContentAccessContractModule();var f=AccessFactory(point,half,probe);var g=New(f,point);
            var idle=New(AccessFactory(point,half,new ContentAccessContractModule()),point);ulong isolated=idle.Hash();
            c.That(probe.ActualAlias&&probe.RuleAliases&&!g.Modules.Contains(TycoonModule.ModuleId),"same original content/rule state owners without business");
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var record=g.RecordReplay(limits)) {
                PermanentRemovalTests.Submit(c,g,new ConfigureWorkPolicyCommand{Worker=g.Seat(0).Snapshot.Entities.Single(e=>e.Owner==new PlayerId(0)&&e.Def==(point?"machine":"crane")).Id,
                    Settings=new WorkPolicySettings(true,0,point?new[]{new WorkerRoleId("inspect"),new WorkerRoleId("haul")}:new[]{new WorkerRoleId("crane")},null,true,true,WorkAssignmentMode.All)});
                Fix opening=StockTotal(g,point);string prototype=point?"point.tower":"tower";
                var args=JsonValue.NewObject();args.Set("placement",Location(point,10));
                long plan=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{
                    Intent=new PlanIntent("build:"+prototype,args,"guard.build",70,true)}).Arg0;
                c.That(!Exists(g,prototype)&&PermanentRemovalTests.Plan(g,plan).Order==0,"plan admission cannot fabricate construction");
                var work=g.Seat(0).View.Feature<WorkView>();ulong pure=g.Hash();
                var check=work.CapturePlanTemplate("guard.build",Location(point,10),out var template);
                c.Equal(Code.None,check.Blocker,"original template callback captures selected geometry");
                c.Equal(Code.None,work.PreviewPlanTemplate(template,Location(point,20),"guard.copy").Blocker,"same template provider previews another location");
                c.Equal(pure,g.Hash(),"template/access/geometry callbacks pure");
                PermanentRemovalTests.Submit(c,g,new StorePlanTemplateCommand{
                    Template=0,ExpectedRevision=0,Name="guard.layout",Value=template});
                var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"original template and pending plan save");
                PermanentRemovalTests.Submit(c,g,new ConfigurePlanCommand{Plan=plan,Group="guard.build",Priority=70,Paused=false});twin=f.Load(g.Save());
                Reach(g,twin,()=>PermanentRemovalTests.Jobs(g).Any(j=>j.Loaded>Fix.Zero),"real loaded machine moves toward selected site");
                Console.WriteLine("  stage loaded point="+point+" half="+half+" at="+g.Clock.StepIndex);
                c.That(!Exists(g,prototype),"partial hauling never completes a structure");
                var retained=PermanentRemovalTests.Plan(g,plan);twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"partial physical progress saves");
                long at=g.Clock.StepIndex;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);c.Equal(at,g.Clock.StepIndex,"one clock pauses every owner");g.Clock.Resume();twin.Clock.Resume();
                Reach(g,twin,()=>Exists(g,prototype),"original earned construction effects legitimately change space");
                Console.WriteLine("  stage built point="+point+" half="+half+" at="+g.Clock.StepIndex);
                c.Equal(opening-Fix.FromInt(point?8:30),StockTotal(g,point),"actual construction material consumption conserved");
                c.That(retained.State!=PlanState.Done,"retained plan facts detached");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"successful geometry-changing effect checkpoint");
                c.Equal(isolated,idle.Hash(),"another actual session isolated");
                c.That(new ReplayVerifier().Verify(f,record.Finish(),limits).Success,"plan/template/player/clock/physical changes replay");
            }
        }
        private static bool Known(SimGame g,int seat=0)
        {
            var facts=new List<TechnologySnapshot>();g.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(facts);
            return facts.Single(t=>t.Technology=="research.foundation").Known;
        }
        private static int Research(Check c,SimGame g)
        {
            PermanentRemovalTests.Submit(c,g,new ConfigureWorkPolicyCommand{Worker=Entity(g,"machine"),
                Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("inspect"),new WorkerRoleId("haul")},"lab.research-area",true,true,WorkAssignmentMode.All)});
            return checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{
                Plan="research.foundation",Target=Entity(g,"contract.factory"),Priority=70}).Arg0);
        }
        private static void PhysicalEffect(Check c,bool half)
        {
            Console.WriteLine("  stage earned effect half="+half);
            var f=EffectFactory(half,new WorkEffectContractsModule());var g=ConstructionPlanTests.NewPoints(f);
            var idle=ConstructionPlanTests.NewPoints(EffectFactory(half,new WorkEffectContractsModule()));ulong isolated=idle.Hash();
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var record=g.RecordReplay(limits)) {
                int id=Research(c,g);
                Reach(g,null,()=>PermanentRemovalTests.Node(g,id,"research").Progress.WholeTicks>0&&!Known(g),"actual staffed research begins");
                var retained=PermanentRemovalTests.Node(g,id,"research");var twin=f.Load(g.Save());
                Reach(g,twin,()=>Known(g),"successful original completion grants earned private knowledge");
                Console.WriteLine("  stage learned half="+half+" at="+g.Clock.StepIndex);
                c.That(!Known(g,1)&&retained.Progress.WholeTicks<retained.RequiredTicks,"private earned state and detached partial progress");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"original work/knowledge/space state durable");
                c.Equal(isolated,idle.Hash(),"other research session isolated");
                c.That(new ReplayVerifier().Verify(f,record.Finish(),limits).Success,"earned successful completion replays");
            }
        }
        private static void Terminal(Check c,SimGame g,SimGame idle,ulong isolated)
        {
            var first=g.Fault;if(first==null)throw new InvalidOperationException("missing actual spatial callback fault");
            long at=g.Clock.StepIndex;
            c.Throws<InvalidOperationException>(()=>g.Save(),"mutated/refused callback cannot save healthy state");
            c.Throws<InvalidOperationException>(()=>g.Hash(),"mutated callback cannot hash healthy state");
            c.Throws<InvalidOperationException>(()=>g.Seat(0).View.Feature<WorkView>().CopyPlans(new List<PlanView>()),"cached private work view cannot return a prefix");
            c.Equal(0,g.Update(),"terminal callback fault stops update");c.Equal(at,g.Clock.StepIndex,"terminal callback fault stops sole clock");
            c.That(ReferenceEquals(first,g.Fault),"first fault remains authoritative");
            c.Equal(isolated,idle.Hash(),"fault is isolated from another actual session");Step(idle);
        }
        private static void AccessFailure(Check c,bool point,bool half,AccessProbeTarget target,SpatialCallbackFault fault)
        {
            Console.WriteLine("  stage access point="+point+" half="+half+" target="+target+" fault="+fault);
            var probe=new ContentAccessContractModule();var g=New(AccessFactory(point,half,probe),point);
            var idle=New(AccessFactory(point,half,new ContentAccessContractModule()),point);ulong isolated=idle.Hash();
            probe.ArmSpatial(target,fault);bool returned=false;Exception actual=null;
            try{g.Seat(0).Preview(Build(point));returned=true;}catch(Exception error){actual=error;}
            c.That(actual is InvalidOperationException&&!returned&&g.Fault!=null&&probe.Reads==1&&probe.MutationCompleted,"selected "+target+"/"+fault+" cannot publish healthy admission");
            c.That(probe.SpatialRevisionChanged,"original selected observation revision actually changed");
            bool restored=fault==SpatialCallbackFault.Revision||fault==SpatialCallbackFault.RevisionThrow;
            c.Equal(restored,probe.SpatialPayloadRestored,"actual codec payload restoration independently measured");
            if(fault==SpatialCallbackFault.LayerThrow||fault==SpatialCallbackFault.RevisionThrow)
                c.Equal("proof.spatial-access-first-error",actual?.Message,"callback original error not masked by verification");
            else if(restored)c.That(actual.Message.Contains("original revision"),"revision guard rejects same-payload mutation");
            Terminal(c,g,idle,isolated);
        }
        private static void RefusedEffectFailure(Check c,bool half,SpatialCallbackFault fault)
        {
            Console.WriteLine("  stage refused effect half="+half+" fault="+fault);
            var probe=new WorkEffectContractsModule();var f=EffectFactory(half,probe);var g=ConstructionPlanTests.NewPoints(f);
            var idle=ConstructionPlanTests.NewPoints(EffectFactory(half,new WorkEffectContractsModule()));ulong isolated=idle.Hash();
            int order=Research(c,g);
            Reach(g,null,()=>PermanentRemovalTests.Node(g,order,"research").Progress.WholeTicks>0&&!Known(g),"real work precedes refused completion");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"physical work saves before negative callback");
            probe.ArmSpatial(fault);
            Console.WriteLine("  stage refused effect armed at="+g.Clock.StepIndex);
            for(int i=0;i<180&&g.Fault==null;i++)g.Update();
            c.That(g.Fault!=null&&probe.Refusals==1&&probe.MutationCompleted&&probe.SpatialRevisionChanged,"actual refused earned effect cannot change selected space");
            bool restored=fault==SpatialCallbackFault.Revision||fault==SpatialCallbackFault.RevisionThrow;
            c.Equal(restored,probe.SpatialPayloadRestored,"refused effect restoration measured over original codec");
            if(fault==SpatialCallbackFault.LayerThrow||fault==SpatialCallbackFault.RevisionThrow)
                c.That(g.Fault.ToString().Contains("proof.spatial-effect-first-error"),"refused effect callback first error retained");
            else if(restored)c.That(g.Fault.ToString().Contains("original revision"),"refused same-payload effect fails revision guard");
            Terminal(c,g,idle,isolated);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual shared spatial observer build");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"no parallel saved revision or observer payload");
            foreach(bool half in new[]{false,true}){Physical(c,false,half);Physical(c,true,half);PhysicalEffect(c,half);}
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})
                foreach(AccessProbeTarget target in Enum.GetValues(typeof(AccessProbeTarget)))
                    foreach(SpatialCallbackFault fault in Enum.GetValues(typeof(SpatialCallbackFault)))AccessFailure(c,point,half,target,fault);
            foreach(bool half in new[]{false,true})foreach(SpatialCallbackFault fault in Enum.GetValues(typeof(SpatialCallbackFault)))RefusedEffectFailure(c,half,fault);
        }
    }
}
