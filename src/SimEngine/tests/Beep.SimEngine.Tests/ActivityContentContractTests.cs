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
using static Beep.SimEngine.Tests.ControlContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Authors startup inputs and simulates a player; graph execution and decisions remain in the engine.
    internal static class ActivityContentContractTests
    {
        internal const string Case = "Activity content contracts: independent JSON graphs drive player plans, phased response and physical construction";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Build(InitializeProbe initialize, out ActivityContractModule proof, Action<JsonValue> edit = null,
            ActivityContractModule extension = null, IReadOnlyDictionary<string,string> contentDocuments = null, Action<JsonValue> worldEdit = null,
            IReadOnlyList<GameModule> additionalModules = null, JsonValue additionalDeclarations = null, Action<JsonValue> scenarioEdit = null)
        {
            var document = Json.Parse(FrameworkProofContent.Read("activity-contracts.json")); edit?.Invoke(document);
            var incident = Json.Parse(FrameworkProofContent.Read("incident-contracts.json"));
            incident.Get("independent_incident_workflows")[0].Set("phases", Json.Parse("[{\"id\":\"detection\",\"nodes\":[\"detect\"]},{\"id\":\"preparation\",\"nodes\":[\"prepare\"]},{\"id\":\"repair\",\"nodes\":[\"repair\"]},{\"id\":\"verification\",\"nodes\":[\"verify\"]},{\"id\":\"restart\",\"nodes\":[\"restart\"]}]"));
            proof = extension ?? new ActivityContractModule(initialize);
            var documents = new Dictionary<string,string> {
                ["activity-contracts.json"] = document.ToString(), ["incident-contracts.json"] = incident.ToString()
            };
            if(contentDocuments!=null)foreach(var entry in contentDocuments)documents.Add(entry.Key,entry.Value);
            return Factory(initialize, out _, extension: proof, contentDocuments: documents, worldEdit:worldEdit, additionalModules:additionalModules, additionalDeclarations:additionalDeclarations, scenarioEdit:scenarioEdit, responseEdit: response => {
                response.Remove("incident_workflows"); response.Remove("response_effects"); response.Remove("activities");
            }, constructionEdit: construction => {
                var remaining = JsonValue.NewArray();
                var activities = construction.Get("activities");
                for (int i = 0; i < activities.Count; i++)
                    if (activities[i].Get("id").Text != "point.tower.build-work") remaining.Add(activities[i]);
                construction.Set("activities", remaining);
            });
        }
        internal static WorkView Work(SimGame game, int company = 0) => game.Seat(company).View.Feature<WorkView>();
        internal static EntityId Plant(SimGame game) => Entity(game,"field.plant").Id;
        internal static PlanView Plan(SimGame game, long id)
        { var list = new List<PlanView>(); Work(game).CopyPlans(list); return list.Single(value => value.Id == id); }
        internal static ActivityNodeView Node(SimGame game, int order, string id)
        {
            var list = new List<ActivityNodeView>();
            if (!Work(game).TryCopyNodes(order,list)) throw new InvalidOperationException("missing owned order " + order);
            return list.Single(value => value.Id == id);
        }
        private static IncidentLifecycleView Lifecycle(SimGame game)
        {
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(Plant(game),new Code("pressure.leak"),out var value))
                throw new InvalidOperationException("missing response lifecycle");
            return value;
        }
        private static void Run(Check c)
        {
            var initialize = new InitializeProbe(); var factory = Build(initialize,out var proof); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current activity protocol"); c.Equal(154,EngineInfo.SaveFormat,"same saved execution model");
            c.That(proof.IndependentActivityContent && proof.ConstructionAlias && proof.IndependentIncidentContent && proof.IndependentAliases,"actual owned contracts, not native activity/workflow/effect inheritance");
            c.Equal(1,initialize.Initializations,"Initialize once after all content validation");
            var criteria = new List<WorkRequirementView>(); ulong hash = game.Hash();
            c.Equal(Code.None,Work(game).PreviewOrderRequirements("contract.inspection",Plant(game),criteria),"direct activity preview uses same graph");
            c.That(criteria.Count > 0,"same requirement diagnostics");
            c.Equal(StandardCodes.NotOrderable,Work(game).PreviewOrderRequirements("pressure.response",Plant(game),criteria),"bound response cannot be ordered as a player shortcut");
            c.Equal(hash,game.Hash(),"previews pure");
            var recorder = game.RecordReplay(new ReplayLimits(4*1024*1024,3000));
            Order(c,game,new ConfigureWorkPolicyCommand { Worker=Entity(game,"machine").Id,Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul"),new WorkerRoleId("inspect")},null,true,true,WorkAssignmentMode.All) });
            var intent = new PlanIntent("activity:contract.inspection",ActivityWorkerSelection.CreateArguments(Plant(game),ActivityWorkerSelection.Unbound),"contract.activities",60,true);
            c.Equal(Code.None,Work(game).PreviewPlan(intent),"ordinary plan admission through direct contract");
            var accepted = TestKit.Order(game,0,new SubmitPlanCommand { Intent=intent }); c.That(accepted.Accepted,"paused player intent accepted"); long plan = accepted.Arg0;
            var paused = Plan(game,plan); c.That(paused.Paused && paused.Order==0 && Stock(game)==Fix.FromInt(20),"paused intent creates no physical work or material grant");
            var twin = factory.Load(game.Save()); for(int i=0;i<3;i++)Step(game,twin);
            c.That(Plan(game,plan).Order==0 && Stock(game)==Fix.FromInt(20),"saved paused intent stays pending");
            var foreign = new List<PlanView>(); Work(game,1).CopyPlans(foreign); c.Equal(0,foreign.Count,"plan execution remains private");
            Order(c,game,new ConfigurePlanCommand { Plan=plan,Group="contract.activities",Priority=60,Paused=false });
            twin=factory.Load(game.Save()); Until(game,twin,()=>Plan(game,plan).Order>0,"direct plan binds to sole native order"); int id=Plan(game,plan).Order;
            Until(game,twin,()=>Node(game,id,"inspect").Progress.WholeTicks>0 && Node(game,id,"inspect").State!=NodeState.Done,"physical inspection starts");
            var inspection=Node(game,id,"inspect"); var calibration=Node(game,id,"calibrate");
            c.That(inspection.Worker.IsValid && inspection.JobId>0 && ((PointSnapshot)Entity(game,"machine").Spatial).Position==Fix.FromInt(20),"machine travels to actual activity target");
            c.That(calibration.Progress.WholeTicks>0 && inspection.Dependencies.SequenceEqual(new[]{"prepare"}),"parallel branch progresses independently after shared dependency");
            c.Equal(Fix.FromInt(18),Stock(game),"consumed material spent once; held tools remain physical stock");
            c.Equal(Code.None,View(game).ReadSignal(Plant(game),new Code("feed"),"field.stock",out var held),"ordinary inventory signal reads held inputs");
            c.Equal(Fix.FromInt(16),held.Value,"held tools reduce available inventory while work runs");
            c.Throws<NotSupportedException>(()=>((IList<string>)inspection.Dependencies).Clear(),"node dependency facts immutable");
            var privateNodes=new List<ActivityNodeView>(); c.That(!Work(game,1).TryCopyNodes(id,privateNodes)&&privateNodes.Count==0,"foreign node facts denied");
            game.Clock.Pause(); twin.Clock.Pause(); hash=game.Hash(); long at=game.Clock.StepIndex; Step(game,twin);
            c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"sole paused clock earns no activity time"); game.Clock.Resume();twin.Clock.Resume();
            twin=factory.Load(game.Save()); Until(game,twin,()=>Node(game,id,"finish").Progress.WholeTicks>0,"parallel join waits for both prerequisites");
            c.That(Node(game,id,"inspect").State==NodeState.Done&&Node(game,id,"calibrate").State==NodeState.Done,"join begins only after both branches complete");
            Until(game,twin,()=>Plan(game,plan).State==PlanState.Done,"parallel join finishes physical player plan");
            c.That(Stock(game)==Fix.FromInt(18) && inspection.State!=NodeState.Done && paused.Paused,"completion releases tools without spending again; retained facts unchanged");
            c.Equal(Code.None,View(game).ReadSignal(Plant(game),new Code("feed"),"field.stock",out var released),"ordinary inventory signal reads released tools");
            c.Equal(Fix.FromInt(18),released.Value,"completed work releases holds without duplicating stock");
            Order(c,game,new ConfigureAutomationCommand { Rule="plant.alert",Settings=Settings(game) }); Until(game,null,()=>Rule(game).HasApplied,"initial normal alert");
            Order(c,game,new RaiseIncidentCommand { Target=Plant(game),Source=new Code("pressure.leak"),Kind=new Code("pressure.leak") });
            twin=factory.Load(game.Save()); Until(game,twin,()=>Lifecycle(game).OrderId>0,"same incident owner binds direct authored response"); int response=Lifecycle(game).OrderId;
            Until(game,twin,()=>Node(game,response,"repair").Progress.WholeTicks>0&&Node(game,response,"repair").State!=NodeState.Done,"real five-stage partial response");
            var repair=Node(game,response,"repair"); c.That(Stock(game)==Fix.FromInt(10)&&Node(game,response,"verify").Progress.WholeTicks>0,"response spends eight once while verification runs in parallel");
            Order(c,game,new CancelOrderCommand { Order=response });
            c.That(Lifecycle(game).OrderId==response&&Active(game)&&Stock(game)==Fix.FromInt(10)&&Node(game,response,"repair").Progress.WholeTicks>=repair.Progress.WholeTicks,"ordinary incident cancellation resumes actual order without replaying inputs");
            twin=factory.Load(game.Save()); Until(game,twin,()=>!Active(game),"direct response completes through existing effect owner");
            var phases=new List<IncidentPhaseView>(); game.Seat(0).View.Feature<IncidentsView>().CopyPhases(Plant(game),new Code("pressure.leak"),phases);
            c.That(phases.Count==5&&phases.All(value=>value.Complete)&&Lifecycle(game).OrderState==OrderState.Done,"phase history is same five authored node mappings");
            c.Equal(Fix.FromInt(10),Stock(game),"response completes without duplicate material cost");
            Order(c,game,new ConstructCommand { Def="point.tower",Placement=Json.Parse("{\"space\":\"field.points\",\"position\":30}"),Priority=50 });
            c.That(game.Seat(0).Snapshot.Entities.Any(value=>value.Def=="point.tower-site")&&!game.Seat(0).Snapshot.Entities.Any(value=>value.Def=="point.tower"),"direct construction creates only actual site, not finished structure");
            twin=factory.Load(game.Save()); Until(game,twin,()=>game.Seat(0).Snapshot.Entities.Any(value=>value.Def=="point.tower"),"same direct authored graph physically hauls and constructs");
            c.That(!game.Seat(0).Snapshot.Entities.Any(value=>value.Def=="point.tower-site")&&((PointSnapshot)Entity(game,"point.tower").Spatial).Position==Fix.FromInt(30),"construction settles same site at original location");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"all completed contract graphs save through existing codecs");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,3000)).Success,"ordinary plans, cancellation, response and construction replay");
            var fresh=New(factory); c.That(Stock(fresh)==Fix.FromInt(20)&&!Active(fresh)&&!fresh.Seat(0).Snapshot.Entities.Any(value=>value.Def=="point.tower"),"interleaved sessions never share graph execution state");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"empty","duplicate","dependency","cycle","executor","requirement","role","duration","priority","tag","duplicate-tag","response-player","response-completion","construction-player","construction-tags","construction-effect"})
            {
                var initialize=new InitializeProbe();
                c.Throws<ContentException>(()=>New(Build(initialize,out _,document=>{
                    var activity=document.Get("independent_activities")[0]; var node=activity.Get("nodes")[0];
                    if(fault=="empty")activity.Set("nodes",Json.Parse("[]"));
                    if(fault=="duplicate")activity.Get("nodes")[1].Set("id",JsonValue.Of("prepare"));
                    if(fault=="dependency")node.Set("after",Json.Parse("[\"missing\"]"));
                    if(fault=="cycle")node.Set("after",Json.Parse("[\"finish\"]"));
                    if(fault=="executor")node.Set("executor",JsonValue.Of("missing"));
                    if(fault=="requirement")node.Set("requirement",JsonValue.Of("missing"));
                    if(fault=="role")activity.Get("nodes")[1].Set("roles",Json.Parse("[\"missing\"]"));
                    if(fault=="duration")node.Get("duration").Set("numerator",JsonValue.Number("-1"));
                    if(fault=="priority")activity.Set("priority",JsonValue.Number("-1"));
                    if(fault=="tag")activity.Set("target_tags",Json.Parse("[\"Bad Tag\"]"));
                    if(fault=="duplicate-tag")activity.Set("target_tags",Json.Parse("[\"site\",\"site\"]"));
                    if(fault=="response-player")document.Get("independent_activities")[1].Set("player_orderable",JsonValue.Of(true));
                    if(fault=="response-completion")document.Get("independent_activities")[1].Get("nodes")[0].Set("effect",JsonValue.Of("pressure.resolve"));
                    if(fault=="construction-player")document.Get("independent_activities")[2].Set("player_orderable",JsonValue.Of(true));
                    if(fault=="construction-tags")document.Get("independent_activities")[2].Set("target_tags",Json.Parse("[\"site\"]"));
                    if(fault=="construction-effect")document.Get("independent_activities")[2].Set("on_complete",JsonValue.Of("pressure.resolve"));
                })),"early authored activity refusal "+fault);
                c.Equal(0,initialize.Initializations,"invalid content rejected before Initialize "+fault);
            }
        }
    }
}
