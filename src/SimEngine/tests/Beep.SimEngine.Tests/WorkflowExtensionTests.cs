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
    // New-file/kind proof uses only explicit JSON and normal player commands/immutable observations.
    internal static class WorkflowExtensionTests
    {
        internal const string Case = "Workflow extension: new master-listed kinds share physical projects and public contracts without scheduler edits";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static bool Exists(SimGame game, string name) => game.Seat(0).Snapshot.Entities.Any(x => x.Def == name);
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        { var rows = new List<ActivityNodeView>(); Work(game).TryCopyNodes(order, rows); return rows; }
        private static PlanView Plan(SimGame game, long identity)
        { var rows = new List<PlanView>(); Work(game).CopyPlans(rows); return rows.Single(x => x.Id == identity); }
        private static CommandResult Result(SimGame game, int sequence)
        { long cursor = 0; var rows = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, rows); return rows.Single(x => x.Sequence == sequence); }
        private static int Queue(Check c, SimGame game, Command command)
        { ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary public command preview"); c.Equal(hash, game.Hash(), "preview pure"); return game.Seat(0).Submit(command); }
        private static CommandResult Submit(Check c, SimGame game, Command command)
        { int sequence = Queue(c, game, command); Step(game); var result = Result(game, sequence); c.That(result.Accepted, "ordinary public input accepted"); return result; }
        private static bool Completed(SimGame game, string plan) => Work(game).TryReadActivityCompletion(plan, EntityId.None, out _);
        private static void Reach(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            var trail = new Queue<string>();
            for (int i = 0; i < 512 && !done(); i++)
            {
                var orders = new List<OrderView>(); Work(game).CopyOrders(orders); var jobs = new List<WorkJobView>(); Work(game).CopyJobs(jobs);
                trail.Enqueue(game.Clock.StepIndex + ":" + string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + string.Join(",", Nodes(game,o.Id).Select(n => n.Id + "/" + n.Progress.WholeTicks + "/" + n.Blocker)))) + " jobs=" + string.Join(",", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.Blocker)));
                if (trail.Count > 16) trail.Dequeue(); Step(game, twin);
            }
            if (!done()) throw new InvalidOperationException(stage + " exceeded512Updates stock=" + Stock(game,"source") + " trail=" + string.Join(" | ", trail));
        }
        private static GameFactory Factory(WorkflowExtensionModule module, bool half, Action<JsonValue,JsonValue> edit = null,
            Action<JsonValue> declarationEdit = null, bool omitInputs = false)
        {
            var workflows = Json.Parse(FrameworkProofContent.Read("workflow-extension.json"));
            var inputs = Json.Parse(FrameworkProofContent.Read("workflow-extension-inputs.json")); edit?.Invoke(workflows, inputs);
            var declaration = Json.Parse("{\"id\":\"proof.workflow-extension\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.construction\",\"version\":\"1.0.0\"},{\"id\":\"points\",\"version\":\"1.0.0\"},{\"id\":\"sim.world-setup\",\"version\":\"1.0.0\"}],\"documents\":[\"workflow-extension.json\",\"workflow-extension-inputs.json\"]}");
            declarationEdit?.Invoke(declaration); var declarations = JsonValue.NewArray(); declarations.Add(declaration);
            var documents = new Dictionary<string,string> { {"workflow-extension.json", workflows.ToString()} };
            if (!omitInputs) documents.Add("workflow-extension-inputs.json", inputs.ToString());
            return ConstructionPlanTests.Points(extraModules:new[]{module},extraDeclarations:declarations.ToString(),extraDocuments:documents,
                startupProfiles:new[]{"extension.start"},scenarioEdit:s=>s.Get("modules").Add(JsonValue.Of(module.Id)),worldEdit:world=>{
                    StartupContentAuthoring.Position(world,"machine",JsonValue.Number("4"));
                    if(half)world.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
                });
        }
        private static void Physical(Check c, bool half, bool cancel)
        {
            var module = new WorkflowExtensionModule(); var factory = Factory(module,half); var game = ConstructionPlanTests.NewPoints(factory);
            c.That(module.ActualAliases && module.Initialization.Initializations == 1, "new file/kind/provider keeps actual original workflow/graph/worker aliases before Initialize");
            var idle = ConstructionPlanTests.NewPoints(factory); ulong idleHash = idle.Hash(); c.That(!game.Modules.Contains(TycoonModule.ModuleId),"extension has no business dependency");
            var limits = new ReplayLimits(4*1024*1024,4000); var recording = game.RecordReplay(limits);
            Submit(c,game,new ConfigureWorkPolicyCommand {Worker=Entity(game,"machine"),Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            Submit(c,game,new SetHomeCommand {Worker=Entity(game,"machine"),Home=Entity(game,"source")});
            Submit(c,game,new ConfigureWorkPolicyCommand {Worker=Entity(game,"extension.machine"),Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("extension.builder")},null,true,true,WorkAssignmentMode.All)});
            var intent = new PlanIntent("build:extension.facility",Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":30}}"),"extension.field",70,true);
            ulong hash = game.Hash(); c.Equal(Code.None,Work(game).PreviewPlan(intent),"new project ordinary engine intent"); c.Equal(hash,game.Hash(),"plan preview pure");
            long plan = Submit(c,game,new SubmitPlanCommand {Intent=intent}).Arg0; var held = Plan(game,plan);
            c.That(held.Paused && held.Order == 0 && !Exists(game,"extension.site") && Stock(game,"source")==Fix.FromInt(20),"paused plan is intent only, no site/material grant");
            var twin = factory.Load(game.Save()); for(int i=0;i<3;i++)Step(game,twin); c.That(Plan(game,plan).Order==0,"saved paused intent remains pending");
            Submit(c,game,new ConfigurePlanCommand {Plan=plan,Group="extension.field",Priority=70,Paused=false}); twin=factory.Load(game.Save());
            Reach(game,twin,()=>Plan(game,plan).Order>0,"new kind binds original physical project"); int order=Plan(game,plan).Order;
            c.That(Exists(game,"extension.site")&&!Exists(game,"extension.facility"),"activation creates only unfinished real site");
            Reach(game,twin,()=>Nodes(game,order).Any(x=>x.Id=="materials"&&x.State==NodeState.Done),"native carrier supplies original material occurrence");
            c.That(Stock(game,"source")==Fix.FromInt(12)&&Nodes(game,order).Single(x=>x.Id=="assemble").Progress.Equals(WorkProgress.Zero)
                && Nodes(game,order).Single(x=>x.Id=="assemble").Blocker==new Code("work.extension-calibration"),"spent eight actual supplies, custom prior provider reports unearned calibration");
            int calibration=checked((int)Submit(c,game,new PostOrderCommand {Plan="extension.calibrate",Target=Entity(game,"extension.machine")}).Arg0);
            twin=factory.Load(game.Save()); Reach(game,twin,()=>Completed(game,"activity:extension.calibrate"),"ordinary timed calibration earns prerequisite");
            Reach(game,twin,()=>Nodes(game,order).Any(x=>x.Id=="assemble"&&x.State==NodeState.Working&&x.Progress.WholeTicks>0),"independent machine physically earns assembly");
            var assembly=Nodes(game,order).Single(x=>x.Id=="assemble"); var curing=Nodes(game,order).Single(x=>x.Id=="cure");
            c.That(assembly.Worker==Entity(game,"extension.machine")&&assembly.SelectedRole==new WorkerRoleId("extension.builder")
                && ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(x=>x.Id==assembly.Worker).Spatial).Position==Fix.FromInt(30),"actual independent IWorker role travels to original site");
            c.That(curing.Progress.WholeTicks>0&&curing.State==NodeState.Working&&Nodes(game,order).Single(x=>x.Id=="finish").Progress.Equals(WorkProgress.Zero),"parallel timed branch advances, join waits");
            c.That(Nodes(game,order).Single(x=>x.Id=="materials").Resources.Single().Spent,"original native material receipt remains spent once");
            var foreign=new List<PlanView>(); Work(game,1).CopyPlans(foreign); var hidden=new List<ActivityNodeView>();
            c.That(foreign.Count==0&&!Work(game,1).TryCopyNodes(order,hidden)&&hidden.Count==0,"new-kind plan/work facts company-private");
            Submit(c,game,new ConfigurePlanCommand {Plan=plan,Group="extension.field",Priority=70,Paused=true}); twin=factory.Load(game.Save());
            var paused=Nodes(game,order).Single(x=>x.Id=="assemble").Progress; for(int i=0;i<3;i++)Step(game,twin);
            c.That(Nodes(game,order).Single(x=>x.Id=="assemble").Progress.Equals(paused),"saved active plan pause preserves original earned work");
            game.Clock.Pause(); twin.Clock.Pause(); hash=game.Hash(); long at=game.Clock.StepIndex; Step(game,twin);
            c.That(game.Hash()==hash&&game.Clock.StepIndex==at,"sole game clock pause"); game.Clock.Resume(); twin.Clock.Resume();
            if(cancel)
            {
                int sequence=Queue(c,game,new CancelPlanCommand {Selection=new PlanCancellationSelection(false,new[]{plan})}); twin=factory.Load(game.Save()); Step(game,twin);
                c.That(Result(game,sequence).Accepted,"saved pending actual project cancellation");
                Reach(game,twin,()=>Plan(game,plan).State==PlanState.Cancelled&&!Exists(game,"extension.site"),"original cancellation/abandon owner");
                c.That(!Exists(game,"extension.facility")&&!Completed(game,"build:extension.facility"),"cancelled custom workflow cannot invent completed structure/history");
            }
            else
            {
                Submit(c,game,new ConfigurePlanCommand {Plan=plan,Group="extension.field",Priority=70,Paused=false}); twin=factory.Load(game.Save());
                Reach(game,twin,()=>Plan(game,plan).State==PlanState.Done,"parallel prerequisites and original construction effect");
                c.That(Exists(game,"extension.facility")&&!Exists(game,"extension.site")&&Completed(game,"build:extension.facility"),"one earned structure and original durable completion fact");
                Submit(c,game,new PostOrderCommand {Plan="extension.certify",Target=Entity(game,"extension.facility")}); twin=factory.Load(game.Save());
                Reach(game,twin,()=>Completed(game,"activity:extension.certify"),"custom provider recognizes actual constructed completion");
                Submit(c,game,new DemolishCommand {Target=Entity(game,"extension.facility")}); twin=factory.Load(game.Save());
                Reach(game,twin,()=>!Exists(game,"extension.facility"),"same external workflow physically removes structure");
            }
            c.That(Stock(game,"source")==Fix.FromInt(12)&&Stock(game,"machine")==Fix.Zero,"no duplicate material spend, grant or stranded carrier cargo");
            c.That(held.Paused&&held.Order==0&&assembly.Progress.WholeTicks>0&&assembly.Progress.WholeTicks<assembly.RequiredTicks,"retained plan/node observations detached");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"new module/content/work state terminal save");c.Equal(idleHash,idle.Hash(),"interleaved isolated extension session");
            c.That(new ReplayVerifier().Verify(factory,recording.Finish(),limits).Success,"public plans/qualification/physical build/cancel/certify/remove replay");
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"file","kind","requirement","node-cycle","completion-cycle","role","executor","unknown-field","version","dependency"})
            {
                var module=new WorkflowExtensionModule();
                c.Throws<ContentException>(()=>ConstructionPlanTests.NewPoints(Factory(module,false,(work,inputs)=>{
                    var build=work.Get("colony_workflows")[1];
                    if(fault=="kind"){work.Set("unregistered_workflows",work.Get("colony_workflows"));work.Remove("colony_workflows");}
                    if(fault=="requirement")build.Get("nodes")[0].Set("requirement",JsonValue.Of("missing"));
                    if(fault=="node-cycle")build.Get("nodes")[0].Set("after",Json.Parse("[\"finish\"]"));
                    if(fault=="completion-cycle")inputs.Get("colony_precedents")[0].Set("plan",JsonValue.Of("build:extension.facility"));
                    if(fault=="role")build.Get("nodes")[1].Set("roles",Json.Parse("[\"missing\"]"));
                    if(fault=="executor")build.Get("nodes")[1].Set("executor",JsonValue.Of("missing"));
                    if(fault=="unknown-field")build.Set("legacy_steps",JsonValue.NewArray());
                },decl=>{
                    if(fault=="version")decl.Set("version",JsonValue.Of("9.0.0"));
                    if(fault=="dependency")decl.Get("requires")[0].Set("id",JsonValue.Of("missing.module"));
                },omitInputs:fault=="file")),"strict early new-file/kind refusal "+fault);
                c.Equal(0,module.Initialization.Initializations,"invalid extension cannot reach Initialize "+fault);
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current unchanged engine identity");
            foreach(bool half in new[]{false,true})foreach(bool cancel in new[]{false,true})Physical(c,half,cancel);
            Invalid(c);
        }
    }
}
