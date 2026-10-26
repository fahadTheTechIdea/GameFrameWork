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
    // Only authors inputs and plays through public commands/Update/immutable facts.
    internal static class IncidentContentContractTests
    {
        internal const string Case = "Incident content contracts: independent workflow and completion drive physical episodes and saved phase history";
        private static readonly Code Source = new Code("pressure.leak");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Build(InitializeProbe initialize, out IncidentContractModule proof,
            Action<JsonValue> edit = null, Action<JsonValue> responseEdit = null)
        {
            var document = Json.Parse(FrameworkProofContent.Read("incident-contracts.json")); edit?.Invoke(document);
            proof = new IncidentContractModule(initialize);
            return Factory(initialize, out _, extension: proof, contentDocuments:new Dictionary<string,string>{{"incident-contracts.json",document.ToString()}},
                responseEdit: response => { response.Remove("incident_workflows"); response.Remove("response_effects"); responseEdit?.Invoke(response); });
        }
        private static IncidentsView Incidents(SimGame game, int company = 0) => game.Seat(company).View.Feature<IncidentsView>();
        private static EntityId Plant(SimGame game) => Entity(game, "field.plant").Id;
        private static IncidentLifecycleView Lifecycle(SimGame game)
        {
            if (!Incidents(game).TryGetLifecycle(Plant(game), Source, out var value)) throw new InvalidOperationException("missing owned lifecycle");
            return value;
        }
        private static IncidentPhaseView Phase(SimGame game, string phase)
        {
            var phases = new List<IncidentPhaseView>(); if (!Incidents(game).CopyPhases(Plant(game), Source, phases)) throw new InvalidOperationException("missing owned phases");
            return phases.Single(value => value.Id == phase);
        }
        private static void Raise(Check c, SimGame game) => Order(c, game, new RaiseIncidentCommand { Target = Plant(game), Source = Source, Kind = Source });
        private static void Run(Check c)
        {
            var initialize = new InitializeProbe(); var factory = Build(initialize, out var proof); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current open workflow identity"); c.Equal(154, EngineInfo.SaveFormat, "existing codecs only");
            c.That(proof.IndependentIncidentContent && proof.IndependentAliases, "actual direct workflow/effect contracts, no native inheritance"); c.Equal(1, initialize.Initializations, "Initialize once");
            var before = Lifecycle(game); var detection = Phase(game, "detection"); var repair = Phase(game, "repair");
            c.That(before.Workflow == "pressure.leak" && before.Snapshot.Episode == 0 && before.OrderId == 0 && !before.OrderState.HasValue, "no invented episode execution");
            c.That(detection.NodeIds.SequenceEqual(new[]{"detect"}) && repair.NodeIds.SequenceEqual(new[]{"repair"}) && detection.Nodes.Count == 0 && !detection.Complete, "independent phase metadata labels actual DAG intent");
            c.Throws<NotSupportedException>(() => ((IList<string>)detection.NodeIds).Clear(), "detached phase mapping immutable");
            c.Throws<NotSupportedException>(() => ((IList<ActivityNodeView>)detection.Nodes).Clear(), "detached node observations immutable");
            var privatePhases = new List<IncidentPhaseView>{ detection };
            c.That(!Incidents(game,1).TryGetLifecycle(Plant(game),Source,out _) && !Incidents(game,1).CopyPhases(Plant(game),Source,privatePhases) && privatePhases.Count == 0, "foreign lifecycle and phases disclose nothing");
            var criteria = new List<WorkRequirementView>(); var work = game.Seat(0).View.Feature<WorkView>();
            ulong hash = game.Hash(); c.Equal(StandardCodes.NotOrderable,work.PreviewOrderRequirements("pressure.response",Plant(game),criteria),"unbound player shortcut remains forbidden"); c.Equal(hash,game.Hash(),"preview/phase reads pure");
            var recorder = game.RecordReplay(new ReplayLimits(4*1024*1024,3000));
            Order(c,game,new ConfigureAutomationCommand{Rule="plant.alert",Settings=Settings(game)}); Until(game,null,()=>Rule(game).HasApplied,"initial normal alert");
            Raise(c,game); var raised = Lifecycle(game);
            c.That(raised.Snapshot.Active && raised.Snapshot.Episode == 1 && Stock(game) == Fix.FromInt(20),"episode grants no repair or materials");
            var raisedPhase=Phase(game,"detection"); var twin=factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"raised episode saves through original board/component");
            Until(game,twin,()=>Alert(game).Settings.Level==CompanyAlertLevel.Emergency,"actual deferred emergency");
            Order(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"machine").Id,Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul"),new WorkerRoleId("inspect")},null,true,true,WorkAssignmentMode.All)});
            twin=factory.Load(game.Save()); Until(game,twin,()=>Stock(game)==Fix.FromInt(12)&&Active(game)&&Phase(game,"repair").Nodes.Count==1&&Phase(game,"repair").Nodes[0].Progress.WholeTicks>0,"real partial response");
            var active=Lifecycle(game); var partial=Phase(game,"repair"); var id=active.OrderId;
            c.That(id>0 && partial.Nodes[0].Worker.IsValid && partial.Nodes[0].JobId>0 && ((PointSnapshot)Entity(game,"machine").Spatial).Position==Fix.FromInt(20),"actual worker/job and physical travel");
            c.That(Phase(game,"detection").Complete&&!partial.Complete&&raisedPhase.NodesDone==0,"phase completion derives nodes; earlier facts detached");
            c.That(work.TryCopyOrderRequirements(id,criteria)&&criteria.Count>0,"actual response requirements remain engine diagnostics");
            game.Clock.Pause(); twin.Clock.Pause(); hash=game.Hash(); long at=game.Clock.StepIndex; Step(game,twin);
            c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"sole paused clock earns no physical work"); game.Clock.Resume();twin.Clock.Resume();
            Order(c,game,new CancelOrderCommand{Order=id});var resumed=Lifecycle(game);
            c.That(resumed.OrderId==id&&resumed.Snapshot.Active&&Stock(game)==Fix.FromInt(12),"ordinary cancel/resume retains episode/order and spent material once");
            c.That(Phase(game,"repair").Nodes[0].Progress.WholeTicks>=partial.Nodes[0].Progress.WholeTicks,"interrupted elapsed work retained");
            twin=factory.Load(game.Save()); Until(game,twin,()=>!Active(game),"independent effect completes same physical response");
            var completed=Lifecycle(game);c.That(completed.OrderId==id&&completed.OrderState==OrderState.Done&&completed.Snapshot.ResolvedTick>=completed.Snapshot.RaisedTick,"episode owner resolves only after real whole-order completion");
            c.That(Phase(game,"detection").Complete&&Phase(game,"repair").Complete&&Stock(game)==Fix.FromInt(12),"all actual phases complete without duplicate material cost");
            c.That(!Incidents(game).TryGetResponseOrder(Plant(game),Source,out _)&&!raisedPhase.Complete,"resolved episode no longer has active work; retained raised facts unchanged");
            twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"resolved phase history checkpoint");
            c.Equal(completed.Snapshot.ResolvedTick,Lifecycle(twin).Snapshot.ResolvedTick,"saved resolution is not guessed");
            Until(game,twin,()=>Alert(game).Settings.Level==CompanyAlertLevel.Normal,"restoration follows healthy signal");
            Raise(c,game);twin=factory.Load(game.Save());Until(game,twin,()=>Lifecycle(game).OrderId>0&&Lifecycle(game).OrderId!=id,"new ordinary episode gets fresh work");
            var second=Lifecycle(game);c.That(second.Snapshot.Episode==2&&second.Snapshot.Active&&Stock(game)==Fix.FromInt(12),"no instant new-episode grant");
            c.That(!completed.Snapshot.Active&&completed.Snapshot.Episode==1,"retained prior completed facts remain exact");
            Until(game,twin,()=>!Active(game),"second episode completes physically");c.That(Stock(game)==Fix.FromInt(4)&&Lifecycle(game).Snapshot.Episode==2,"second episode spends its own eight resources");
            twin=factory.Load(game.Save());for(int i=0;i<8;i++)Step(game,twin);c.That(Stock(game)==Fix.FromInt(4)&&!Active(game),"settled updates cannot repeat completed effects");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,3000)).Success,"ordinary physical/cancel/automation history replay");
            var fresh=New(factory);c.That(Stock(fresh)==Fix.FromInt(20)&&Lifecycle(fresh).Snapshot.Episode==0&&!Active(fresh),"interleaved sessions own distinct episodes/stock");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach(var fault in new[]{"kind","response","empty","duplicate-phase","missing-node","duplicate-node","unmapped","effect","unbound","both","source","workflow","player-order","node-completion","cycle","no-machine"})
            {
                var initialize=new InitializeProbe();
                c.Throws<ContentException>(()=>New(Build(initialize,out _,document=>{
                    var workflow=document.Get("independent_incident_workflows")[0];var effect=document.Get("independent_incident_completions")[0];
                    if(fault=="kind")workflow.Set("kind",JsonValue.Of("Bad Kind"));
                    if(fault=="response")workflow.Set("response",JsonValue.Of("missing"));
                    if(fault=="empty")workflow.Set("phases",Json.Parse("[]"));
                    if(fault=="duplicate-phase")workflow.Get("phases")[1].Set("id",JsonValue.Of("detection"));
                    if(fault=="missing-node")workflow.Get("phases")[0].Set("nodes",Json.Parse("[\"missing\"]"));
                    if(fault=="duplicate-node")workflow.Get("phases")[1].Set("nodes",Json.Parse("[\"detect\"]"));
                    if(fault=="unmapped")workflow.Set("phases",Json.Parse("[{\"id\":\"detection\",\"nodes\":[\"detect\"]}]"));
                    if(fault=="effect")effect.Set("id",JsonValue.Of("missing"));
                    if(fault=="unbound"){effect.Remove("bound_response");effect.Set("source",JsonValue.Of("pressure.leak"));}
                    if(fault=="both")effect.Set("source",JsonValue.Of("pressure.leak"));
                    if(fault=="source"){effect.Remove("bound_response");effect.Set("source",JsonValue.Of("Bad Source"));}
                    if(fault=="workflow")workflow.Set("id",JsonValue.Of("missing"));
                },response=>{
                    var activity=response.Get("activities")[0];
                    if(fault=="player-order")activity.Set("player_orderable",JsonValue.Of(true));
                    if(fault=="node-completion")activity.Get("nodes")[0].Set("effect",JsonValue.Of("pressure.resolve"));
                    if(fault=="cycle")activity.Get("nodes")[0].Set("after",Json.Parse("[\"repair\"]"));
                    if(fault=="no-machine")activity.Get("nodes")[1].Remove("roles");
                })),"early independent workflow refusal "+fault);
                c.Equal(0,initialize.Initializations,"rejected before Initialize "+fault);
            }
        }
    }
}
