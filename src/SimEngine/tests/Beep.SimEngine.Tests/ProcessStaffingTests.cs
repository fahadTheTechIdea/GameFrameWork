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

namespace Beep.SimEngine.Tests
{
    internal static class ProcessStaffingTests
    {
        internal const string Case = "Process staffing: automatic exact queue teams, retained duty, sibling contention and saved departure";
        internal const string IndependentCase = "Process staffing: independent JSON metadata shares queue teams lifetime and replay";
        internal static void Register()
        {
            TestRunner.Add(Case, c => Run(c, false));
            TestRunner.Add(IndependentCase, c => Run(c, true));
        }
        private static readonly Code A = new Code("process"), B = new Code("process.b");
        private static JsonValue Gate(JsonValue content) => content.Get("entities")[0].Get("components").Get("staffed_transformation_gate");
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "native-survey", Seed = 931, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Target(SimGame game) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "staff.process-factory").Id;
        private static TransformationManagementView View(SimGame game) => game.Seat(0).View.Feature<TransformationManagementView>();
        private static TransformationSnapshot State(SimGame game, Code process)
        {
            if (!View(game).TryGetTransformation(Target(game), process, out var state)) throw new InvalidOperationException("missing processor");
            return state;
        }
        private static TransformationStaffingSnapshot Staff(SimGame game, Code process, string recipe = "staff.batch")
        {
            Code result = View(game).ReadStaffing(Target(game), process, recipe, out var facts);
            if (!result.IsNone || facts.Count != 1) throw new InvalidOperationException("missing staffing " + result);
            return facts[0];
        }
        private static List<OrderView> Orders(SimGame game)
        { var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders); return orders; }
        private static bool Live(OrderView order) => order.State == OrderState.Active || order.State == OrderState.Completing;
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        {
            var facts = new List<ActivityNodeView>();
            if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, facts)) throw new InvalidOperationException("missing actual service nodes");
            return facts;
        }
        private static Fix Stock(SimGame game)
        {
            var facets = new List<Facet>(); game.Seat(0).View.Inspect(Target(game), facets);
            return facets.Where(f=>f.Key==Storage.FacetStock && f.Subject=="steel").Aggregate(Fix.Zero,(sum,row)=>sum+row.Value);
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("process staffing fault " + game.Fault);
            if (twin == null) return;
            twin.Update(); if (twin.Fault != null || twin.Hash() != game.Hash()) throw new InvalidOperationException("saved team divergence at " + game.Tick);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 200 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + "/time=" + game.Tick + "/A=" + State(game,A).Blocker + "/B=" + State(game,B).Blocker);
        }
        private static void Command(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "public command preview");
            c.Equal(before, game.Hash(), "preview never posts staffing work");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "public command accepted: " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("command refused " + result.Refusal);
        }
        private static void Enqueue(Check c, SimGame game, Code process, string recipe = "staff.batch", int count = 1, bool repeat = false)
            => Command(c, game, new EnqueueTransformationCommand { Transformer = Target(game), Capability = process, Transformation = recipe, Count = count, Repeat = repeat });
        private static void Enabled(Check c, SimGame game, bool enabled)
            => Command(c, game, new ConfigureOperationCommand { Entity = Target(game), Operation = A, Enabled = enabled, Target = Fix.One });
        private static void Cancel(Check c, SimGame game, Code process)
            => Command(c, game, new CancelTransformationCommand { Transformer = Target(game), Capability = process, Index = 0 });
        private static void Run(Check c, bool independent)
        {
            GameFactory Factory(Action<JsonValue> edit = null) => ProcessStaffingGame.Factory(edit, independent);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "actual queue/lifetime protocol"); c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "changed original queue/order codecs");
            var factory = Factory(); var game = New(factory); var limits = new ReplayLimits(4 * 1024 * 1024, 800); var recorder = game.RecordReplay(limits);
            var metadata = game.World.Entities.Get(Target(game)).Def.Component<IStaffedTransformationGateDefinition>(new Code("staffing"));
            c.Equal(independent, metadata is ProofStaffingGateDef, "actual queue gate binds the selected independent metadata contract");
            c.Equal(0, Orders(game).Count, "idle processors do not create work");
            ulong hash = game.Hash(); Staff(game, A); Staff(game, B); c.Equal(hash, game.Hash(), "diagnostics never create work");
            Enqueue(c, game, A, count:2); Enqueue(c, game, B);
            var first = State(game,A).Queue.Select(q => q.Execution).ToArray();
            c.That(first.Length == 2 && first[0] > 0 && first[1] > first[0], "identical queued recipes have distinct stable IDs before startup");
            c.Equal(1L, State(game,B).Queue[0].Execution, "sibling owns its own sequence; capability prevents identity collision");
            c.That(Orders(game).All(o => o.ExecutionBinding.IsBound), "engine posts only original bound work orders");
            c.That(State(game,A).Wip.Stock.IsEmpty && State(game,B).Wip.Stock.IsEmpty, "posted/travelling machines grant no startup resources");
            Until(game, () => State(game,A).Progress.WholeTicks > 0, "first real team arrives");
            var team = Staff(game,A).Requirements.SelectMany(n => n.Assignments).ToList();
            c.Equal(2, team.Count, "two actual physical workers"); c.Equal(2, team.Select(f=>f.Worker).Distinct().Count(), "team actors are distinct");
            c.That(team.All(f=>f.ExecutionBinding.Capability == A && f.ExecutionBinding.Execution == first[0]), "all facts identify exact head processor/execution");
            c.Equal(0, Staff(game,B).Requirements.Sum(n=>n.Assignments.Count), "same-recipe sibling cannot reuse A's service assignments");
            c.That(State(game,B).Wip.Stock.IsEmpty && State(game,B).Tools.Receipts.Count == 0, "sibling contention makes no material promises");
            Enabled(c, game, false); var stopped = State(game,A); var jobIds = Staff(game,A).Requirements.SelectMany(n=>n.Assignments).Select(f=>f.Job).ToArray();
            var beforeDuty = Nodes(game,team[0].Order);
            var twin = factory.Load(game.Save());
            for (int i=0;i<12;i++) Step(game,twin);
            c.Equal(stopped.Progress, State(game,A).Progress, "disabled process earns no progress");
            c.That(jobIds.SequenceEqual(Staff(game,A).Requirements.SelectMany(n=>n.Assignments).Select(f=>f.Job)), "same jobs retain duty beyond one-minute minimum");
            c.That(Staff(game,A).Requirements.All(n=>n.Ready), "long disabled wait does not retire physical service team");
            c.That(Nodes(game,team[0].Order).All(n=>n.Progress.Complete(n.RequiredTicks) && n.ProductiveSteps>beforeDuty.Single(b=>b.Id==n.Id).ProductiveSteps),"service exposure continues after minimum work saturates");
            c.Equal(0, Staff(game,B).Requirements.Sum(n=>n.Assignments.Count), "sibling remains physically waiting without duplicate leases");
            c.Equal(game.Hash(),twin.Hash(),"minimum-saturated service jobs save and continue deterministically");
            var worker = team[0].Worker;
            Command(c,game,new SetWorkerHeldCommand { Worker=worker,Held=true });
            c.Equal(StaffedTransformationGate.Shortage, Staff(game,A).Blocker,"player can withhold a real bound machine");
            twin=factory.Load(game.Save()); Step(game,twin);
            Command(c,game,new SetWorkerHeldCommand { Worker=worker,Held=false });
            Enabled(c,game,true);
            Until(game,()=>State(game,A).QueueCount==0 && State(game,B).QueueCount==0 && Orders(game).All(o=>!Live(o)),"all three ordinary executions finish and teams depart");
            var work = game.Seat(0).View.Feature<WorkView>();
            c.That(work.TryReadServiceCompletion("activity:staff.service", Target(game), new Code("staffing"), A, out var completeA), "completed A team retained in original compact history");
            c.That(work.TryReadServiceCompletion("activity:staff.service", Target(game), new Code("staffing"), B, out var completeB), "completed B team has its own compact source key");
            c.Equal(first[1],completeA.ExecutionBinding.Execution,"sequential same-recipe final work binds second queue ID");
            c.Equal(1L,completeB.ExecutionBinding.Execution,"sibling completion preserves its own queue identity");
            c.That(completeA.CompletedTick >= completeA.CreatedTick && completeB.CompletedTick >= completeB.CreatedTick,"ended demand earns actual work completion, not cancellation");
            var completedSave=factory.Load(game.Save()); c.Equal(game.Hash(),completedSave.Hash(),"compact source-specific completion keys survive save/load");
            c.That(completedSave.Seat(0).View.Feature<WorkView>().TryReadServiceCompletion("activity:staff.service",Target(completedSave),new Code("staffing"),A,out var restoredCompletion),"restored compact history exposes exact process completion");
            c.Equal(completeA.ExecutionBinding,restoredCompletion.ExecutionBinding,"saved completion retains original source/capability/execution binding");
            c.Equal(Codes.NotFound,game.Seat(1).View.Feature<TransformationManagementView>().ReadStaffing(Target(game),A,"staff.batch",out var hidden),"automatic teams stay company private");
            c.Equal(0,hidden.Count,"private source leaks no actual workers");
            var verified = new ReplayVerifier().Verify(factory,recorder.Finish(),limits);
            c.That(verified.Success,"automatic work and queue IDs replay through public player commands");
            c.Equal(game.Hash(), verified.ActualHash, "queue staffing replay reaches actual final live state");

            var flow=New(factory); Enqueue(c,flow,A,"staff.flow");
            Until(flow,()=>State(flow,A).Progress.WholeTicks>=12,"continuous process retains team beyond minimum");
            var execution=State(flow,A).Queue[0].Execution; var flowJobs=Staff(flow,A,"staff.flow").Requirements.SelectMany(n=>n.Assignments).Select(f=>f.Job).ToArray();
            c.Equal(2,flowJobs.Length,"continuous demand retains actual team");
            var saved=factory.Load(flow.Save()); Step(flow,saved); Step(flow,saved);
            c.That(flowJobs.SequenceEqual(Staff(flow,A,"staff.flow").Requirements.SelectMany(n=>n.Assignments).Select(f=>f.Job)),"continuous service has no periodic ghost replacement");
            Cancel(c,flow,A);
            Until(flow,()=>State(flow,A).QueueCount==0 && Orders(flow).All(o=>!Live(o)),"continuous stop releases through native work completion");
            c.Equal(0,State(flow,A).Tools.Receipts.Count,"stop releases actual material tool");
            c.That(flow.Seat(0).View.Feature<WorkView>().TryReadServiceCompletion("activity:staff.service",Target(flow),new Code("staffing"),A,out var ended), "continuous team earned compact completion");
            c.Equal(execution,ended.ExecutionBinding.Execution,"completed service preserves immutable execution provenance");
            foreach(var id in flow.Seat(0).Snapshot.Entities.Where(e=>e.Def=="survey.machine" || e.Def=="staff.assistant").Select(e=>e.Id))
            { c.Equal(Code.None,flow.Seat(0).View.Feature<WorkView>().ReadWorkerAssignment(id,out var fact),"owned worker readable after departure"); c.That(!fact.HasValue,"no stranded service job after process stop"); }

            var repeated=New(factory); Enqueue(c,repeated,A,repeat:true);
            Until(repeated,()=>State(repeated,A).NextExecution>=3,"repeating recipe allocates a fresh queue identity");
            c.That(State(repeated,A).Queue[0].Execution>1,"repeat never reuses prior staffing execution");
            Cancel(c,repeated,A); Until(repeated,()=>State(repeated,A).QueueCount==0 && Orders(repeated).All(o=>!Live(o)),"repeating head cancellation ends service");

            var serviced=New(Factory(json=>{
                json.Set("target_resource_requirements",Json.Parse("[{\"id\":\"staff.elapsed\",\"port\":null,\"use\":\"elapsed\",\"rates\":[{\"resource\":\"steel\",\"quantity\":1,\"period\":{\"numerator\":5,\"denominator\":1,\"unit\":\"minute\"}}]}]"));
                var nodes=json.Get("activities")[0].Get("nodes"); for(int i=0;i<nodes.Count;i++) nodes[i].Set("requirement",JsonValue.Of("staff.elapsed"));
            }));
            Enqueue(c,serviced,A); Until(serviced,()=>State(serviced,A).Progress.WholeTicks>0,"elapsed-resource service arrives");
            int serviceOrder=Staff(serviced,A).Requirements[0].Assignments[0].Order;
            Enabled(c,serviced,false); var serviceStock=Stock(serviced); var serviceBefore=Nodes(serviced,serviceOrder);
            for(int i=0;i<6;i++) Step(serviced);
            var serviceAfter=Nodes(serviced,serviceOrder);
            long expectedBurn=serviceAfter.Sum(n=>(n.ProductiveSteps+4)/5)-serviceBefore.Sum(n=>(n.ProductiveSteps+4)/5);
            c.That(expectedBurn>0,"retained service earns real elapsed exposure");
            c.Equal(serviceStock-Fix.FromInt(checked((int)expectedBurn)),Stock(serviced),"original elapsed resource owner consumes exact duty deltas beyond minimum");
            c.That(serviceAfter.All(n=>n.Progress.Equals(serviceBefore.Single(b=>b.Id==n.Id).Progress)),"running material use does not create a second work clock");
            Cancel(c,serviced,A); Until(serviced,()=>State(serviced,A).QueueCount==0 && Orders(serviced).All(o=>!Live(o)),"resource-consuming service releases normally");

            var replacement=New(factory); Enqueue(c,replacement,A);
            Until(replacement,()=>State(replacement,A).Progress.WholeTicks>0,"replacement baseline team arrives");
            var oldTeam=Staff(replacement,A).Requirements.SelectMany(n=>n.Assignments).ToArray(); var retainedWip=State(replacement,A).Wip.Execution;
            Command(c,replacement,new CancelOrderCommand { Order=oldTeam[0].Order });
            c.Equal(retainedWip,State(replacement,A).Wip.Execution,"service cancellation never restarts funded material execution");
            Until(replacement,()=>Staff(replacement,A).Requirements.All(n=>n.Ready),"live queue demand acquires replacement ordinary team");
            c.That(Staff(replacement,A).Requirements.SelectMany(n=>n.Assignments).All(f=>f.Order!=oldTeam[0].Order && f.ExecutionBinding.Execution==retainedWip),"replacement uses new real work order for same process execution");
            Until(replacement,()=>State(replacement,A).QueueCount==0 && Orders(replacement).All(o=>!Live(o)),"replacement completes original production");
            var multiple=New(Factory(json=>{
                var needs=Gate(json).Get("requirements"); for(int i=0;i<needs.Count;i++) needs[i].Set("count",JsonValue.Number("2"));
            }));
            Enqueue(c,multiple,A); Until(multiple,()=>Staff(multiple,A).Requirements.Sum(n=>n.Assignments.Count)==2,"first of two authored complete teams arrives");
            c.Equal(2,Orders(multiple).Count,"posting budget creates two real team orders over successive updates");
            c.Equal(StaffedTransformationGate.Shortage,Staff(multiple,A).Blocker,"two physical actors cannot impersonate four team positions");
            c.That(State(multiple,A).Wip.Stock.IsEmpty && State(multiple,A).Tools.Receipts.Count==0,"incomplete counted teams create no resource prefix");
            c.Throws<ContentException>(()=>New(Factory(json=>Gate(json).Remove("mode"))),"no implicit legacy staffing mode");
            c.Throws<ContentException>(()=>New(Factory(json=>Gate(json).Set("orders_per_update",JsonValue.Number("0")))),"posting budget is explicit positive content, not an engine size cap");
            c.Throws<ContentException>(()=>New(Factory(json=>Gate(json).Set("processes",Json.Parse("[\"missing\"]")))),"undeclared processor rejected before update");
            c.Throws<ContentException>(()=>New(Factory(json=>Gate(json).Get("requirements")[0].Set("count",JsonValue.Number("2")))),"incomplete team graph count rejected");
            c.Throws<ContentException>(()=>New(Factory(json=>Gate(json).Get("requirements")[0].Set("roles",Json.Parse("[\"survey\"]")))),"automatic node cannot silently acquire an uncounted alternative role");
        }
    }
}
