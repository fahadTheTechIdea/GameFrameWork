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
    // Pure JSON authoring, normal commands and detached facts. All recurring/work decisions are engine-owned.
    internal static class RecurringActivityBindingTests
    {
        internal const string Case = "Recurring bindings: direct JSON schedules share physical work, stock gates and saved history";
        private static readonly Code Routine = new Code("routine");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static JsonValue Parts(JsonValue doc) => doc.Get("entities")[0].Get("components");
        private static GameFactory Build(InitializeProbe initialize, out RecurringActivityBindingsModule proof,
            bool independent = true, Action<JsonValue> edit = null)
        {
            var doc = Json.Parse(FrameworkProofContent.Read("recurring-bindings.json"));
            if (!independent)
            {
                var parts = Parts(doc); var row = parts.Get("independent_recurring");
                row.Remove("metadata_fault"); parts.Remove("independent_recurring"); parts.Set("recurring_activity", row);
            }
            edit?.Invoke(doc); proof = new RecurringActivityBindingsModule(initialize);
            return Factory(initialize, out _, extension: proof,
                contentDocuments: new Dictionary<string,string> { ["recurring-bindings.json"] = doc.ToString() },
                conditionsEdit: j => j.Get("world_setup_profiles")[0].Get("includes").Add(JsonValue.Of("contract.routine-start")));
        }
        private static EntityId Target(SimGame game) => Entity(game,"field.routine").Id;
        private static WorkView View(SimGame game,int seat=0) => game.Seat(seat).View.Feature<WorkView>();
        private static RecurringPlanView Read(SimGame game)
        { var code=View(game).ReadRecurringPlan(Target(game),Routine,out var value);if(!code.IsNone)throw new InvalidOperationException("routine "+code);return value; }
        private static List<PlanView> Plans(SimGame game)
        {var values=new List<PlanView>();View(game).CopyPlans(values);return values.Where(p=>p.Origin.Source==Routine).ToList();}
        private static PlanView Plan(SimGame game,long id)=>Plans(game).Single(p=>p.Id==id);
        private static Fix StockHere(SimGame game)
        {var facets=new List<Facet>();game.Seat(0).View.Inspect(Target(game),facets);return facets.Where(f=>f.Key==Storage.FacetStock&&f.Subject=="supply").Aggregate(Fix.Zero,(n,f)=>n+f.Value);}
        private static void Configure(Check c,SimGame game,bool enabled)=>Order(c,game,new ConfigureRecurringPlanCommand{Entity=Target(game),Source=Routine,Settings=new RecurringPlanSettings(enabled,40,GameDuration.Minutes(6),Code.None)});
        private static void Worker(Check c,SimGame game)=>Order(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"machine").Id,Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("inspect"),new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
        private static WorkProgress Progress(SimGame game,long plan)
        {var nodes=new List<ActivityNodeView>();if(!View(game).TryCopyNodes(Plan(game,plan).Order,nodes))throw new InvalidOperationException("missing actual nodes");return nodes.Single(n=>n.Id=="service").Progress;}
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current recurring contract identity");
            c.Equal(154,EngineInfo.SaveFormat,"original schedule8/board/work codecs");
            c.That(Healthy(c,false).SequenceEqual(Healthy(c,true)),"native/direct metadata earns identical actual plan chronology");
            Cancel(c);Gate(c);Invalid(c);Faults(c);
        }
        private static long[] Healthy(Check c,bool independent)
        {
            var initialize=new InitializeProbe();var factory=Build(initialize,out var proof,independent);var game=New(factory);
            c.That(proof.ActualBinding==independent,"actual contract reuses original runtime");c.Equal(1,initialize.Initializations,"valid Initialize once");
            var retained=Read(game);ulong hash=game.Hash();
            c.That(retained.Blocker==RecurringPlanCodes.Disabled&&retained.Schedule.LastPlan==0&&Plans(game).Count==0&&StockHere(game)==Fix.FromInt(20),"disabled JSON intent invents no work or stock change");
            c.Equal(Codes.NotFound,View(game,1).ReadRecurringPlan(Target(game),Routine,out var hidden),"foreign source diagnostics refused");
            c.That(hidden.Intent==null&&hidden.Schedule.Settings==null,"foreign read publishes no intent/settings");c.Equal(hash,game.Hash(),"inspection pure");
            var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,3000));Worker(c,game);Configure(c,game,true);
            long first=Read(game).Schedule.LastPlan;
            c.That(first>0&&Plan(game,first).Origin.Entity==Target(game)&&StockHere(game)==Fix.FromInt(20),"source posts one original plan without instant consumption");
            Until(game,null,()=>StockHere(game)==Fix.FromInt(12),"machine earns first physical recurring service");
            c.That(Plan(game,first).State==PlanState.Active&&((PointSnapshot)Entity(game,"machine").Spatial).Position==Fix.FromInt(25),"actual worker travels4 to25 before real eight-unit bill");
            Order(c,game,new ConfigurePlanCommand{Plan=first,Group="field.paused",Priority=40,Paused=true});
            var progress=Progress(game,first);var twin=factory.Load(game.Save());
            for(int i=0;i<8;i++)Step(game,twin);
            c.That(Progress(game,first).Equals(progress)&&Read(game).Schedule.LastPlan==first&&Plans(game).Count==1,"paused outstanding cycle never repeats or advances work");
            hash=game.Hash();long at=game.Clock.StepIndex;game.Clock.Pause();twin.Clock.Pause();Step(game,twin);
            c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"sole clock pause freezes schedule and physical work");game.Clock.Resume();twin.Clock.Resume();
            Order(c,game,new ConfigurePlanCommand{Plan=first,Group="field.paused",Priority=40,Paused=false});twin=factory.Load(game.Save());
            Until(game,twin,()=>Plan(game,first).State==PlanState.Done,"saved original first-cycle completion");
            Until(game,twin,()=>Read(game).Schedule.LastCompletedPlan==first,"original coordinator acknowledges earned completion on its next pass");
            long ended=Plan(game,first).EndedTick;var completed=Read(game);
            c.That(completed.Schedule.LastCompletedPlan==first&&completed.ServiceDueTick==ended+6,"service cadence derives from earned completion, not posting age");
            c.That(retained.Schedule.LastPlan==0&&!retained.Schedule.Enabled&&StockHere(game)==Fix.FromInt(12),"retained schedule detached and first spend exactly once");
            Until(game,twin,()=>Read(game).Schedule.LastPlan>first,"six-minute completed-service deadline");
            long second=Read(game).Schedule.LastPlan;long posted=Read(game).Schedule.LastPostedTick;
            c.That(posted==ended+6&&Plan(game,second).Origin.Source==Routine&&Plans(game).Count==2,"one fresh cycle at original due time and source provenance");
            Until(game,twin,()=>Plan(game,second).State==PlanState.Done,"second real material-consuming cycle");
            Until(game,twin,()=>Read(game).Blocker==new Code("routine.reserve"),"actual stock gate prevents another repeat");
            c.That(StockHere(game)==Fix.FromInt(4)&&Read(game).Schedule.LastCompletedPlan==second&&Plans(game).Count==2,"stock20 to12 to4; no fabricated resupply or ghost third plan");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed history and resource wait checkpoint");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,3000)).Success,"ordinary plan/source commands and physical decisions replay");
            var fresh=New(factory);c.That(Read(fresh).Schedule.LastPlan==0&&StockHere(fresh)==Fix.FromInt(20),"interleaved session retains separate schedule/history/stock");
            return new[]{Plan(game,first).CreatedTick,ended,posted,Plan(game,second).EndedTick};
        }
        private static void Cancel(Check c)
        {
            var factory=Build(new InitializeProbe(),out _);var game=New(factory);Worker(c,game);Configure(c,game,true);
            long first=Read(game).Schedule.LastPlan;
            Until(game,null,()=>((PointSnapshot)Entity(game,"machine").Spatial).Position>Fix.FromInt(4),"ordinary recurring worker departs");
            Configure(c,game,false);c.That(Plan(game,first).State==PlanState.Active,"disable only future repeats");
            Order(c,game,new CancelPlanCommand{Selection=new PlanCancellationSelection(false,new[]{first})});
            c.That(Plan(game,first).State==PlanState.Cancelled&&StockHere(game)==Fix.FromInt(20)&&Read(game).Schedule.LastCompletedPlan==0,"cancel before arrival grants no completion or repeated material spend");
            var twin=factory.Load(game.Save());for(int i=0;i<8;i++)Step(game,twin);
            c.That(Plans(game).Count==1&&Read(game).Schedule.LastPlan==first,"saved disabled/cancelled state does not secretly retry");
            Configure(c,game,true);twin=factory.Load(game.Save());
            Until(game,twin,()=>Read(game).Schedule.LastCompletedPlan>first,"original retry earns a new physical completion");
            Configure(c,game,false);
            c.That(StockHere(game)==Fix.FromInt(12)&&Plans(game).Count==2&&Plan(game,first).State==PlanState.Cancelled,"one cancelled and one completed cycle retain actual history");
        }
        private static void Gate(Check c)
        {
            var factory=Build(new InitializeProbe(),out _,edit:j=>{var condition=Parts(j).Get("independent_recurring").Get("conditions")[0];condition.Set("minimum",JsonValue.Number("30"));condition.Set("maximum",JsonValue.Number("40"));});var game=New(factory);Configure(c,game,true);
            c.That(Read(game).Blocker==new Code("routine.reserve")&&Plans(game).Count==0,"authored stock condition blocks initial source intent");
            var twin=factory.Load(game.Save());for(int i=0;i<12;i++)Step(game,twin);
            c.That(StockHere(game)==Fix.FromInt(20)&&Read(game).Schedule.LastPlan==0,"saved blocked policy neither consumes stock nor invents elapsed service");
        }
        private static void Invalid(Check c)
        {
            foreach(var fault in new[]{"interval","inexact","plan","template","cadence","target","worker-nodes","policy","dispatch","condition-source","condition-duplicate","initial-null","binding-null","conditions-null","cadence-invalid","validation-throw","validation-pause","validation-reenter","unbound"})
            {
                var initialize=new InitializeProbe();Action create=()=>New(Build(initialize,out _,edit:j=>{
                    var unused=j.Get("entities")[0].Clone();unused.Set("id",JsonValue.Of("field.unused-routine"));j.Get("entities").Add(unused);
                    var parts=unused.Get("components");var row=parts.Get("independent_recurring");
                    if(fault=="interval")row.Get("interval").Set("numerator",JsonValue.Number("0"));if(fault=="inexact")row.Get("interval").Set("denominator",JsonValue.Number("7"));
                    if(fault=="plan")row.Set("plan",JsonValue.Of("missing"));if(fault=="template")row.Set("template",JsonValue.Of("missing"));
                    if(fault=="cadence")row.Set("cadence",JsonValue.Of("implicit"));if(fault=="target")row.Set("target",JsonValue.Of("implicit"));
                    if(fault=="worker-nodes")row.Set("worker_nodes",Json.Parse("[\"service\"]"));if(fault=="policy")row.Set("operation_policy",JsonValue.Of("missing"));
                    if(fault=="dispatch")row.Set("dispatch_mode",JsonValue.Of("service_first"));if(fault=="condition-source")row.Get("conditions")[0].Set("source",JsonValue.Of("missing"));
                    if(fault=="condition-duplicate")row.Get("conditions").Add(row.Get("conditions")[0].Clone());
                    if(fault.EndsWith("-null")||fault=="cadence-invalid"||fault.StartsWith("validation-"))row.Set("metadata_fault",JsonValue.Of(fault));
                    if(fault=="unbound"){row.Remove("metadata_fault");parts.Remove("independent_recurring");parts.Set("unbound_recurring",row);}
                }));
                if(fault.StartsWith("validation-"))c.Throws<InvalidOperationException>(create,"pure binding callback refusal "+fault);else c.Throws<ContentException>(create,"unused binding refusal before play "+fault);
                c.Equal(0,initialize.Initializations,"invalid binding never initializes "+fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach(var fault in new[]{"live-throw","live-pause","live-touch","live-private"})
            {
                var factory=Build(new InitializeProbe(),out _,edit:j=>Parts(j).Get("independent_recurring").Set("metadata_fault",JsonValue.Of(fault)));var game=New(factory);
                for(int i=0;i<5&&game.Fault==null;i++)game.Update();
                c.That(game.Fault?.Phase==(fault=="live-throw"?"observation.provider":"work.recurring.metadata"),"actual recurring metadata codec/clock fault "+fault);
                c.Throws<InvalidOperationException>(()=>Read(game),"faulted live recurring facts refused "+fault);
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted recurrence cannot checkpoint "+fault);
                c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted recurrence cannot report healthy hash "+fault);
            }
        }
    }
}
