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
    // Actual files/master authoring and ordinary public player input/observations only.
    internal static class SystemCapabilityConsumerTests
    {
        internal const string Case = "System capability consumers: independent machines execute physical hauling and authored recurrence";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Routine = new Code("routine");
        private static GameFactory Factory(bool half, bool independent, out InitializeProbe initialize)
        {
            var world = Json.Parse(PointGame.Definitions); var scenario = world.Get("scenarios")[0];
            scenario.Set("seats", JsonValue.Number("2")); scenario.Get("modules").Add(JsonValue.Of("proof.system-consumers"));
            if (half) world.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            var parts = world.Get("entities")[2].Get("components"); parts.Remove("worker");
            parts.Set("contract_worker", Json.Parse("{\"role\":\"haul\",\"rate\":1.5,\"states\":{\"working\":\"processing\",\"stopped\":\"blocked\"}}"));
            StartupContentAuthoring.Position(world,"source",JsonValue.Number("2"));
            StartupContentAuthoring.Position(world,"machine",JsonValue.Number("4"));
            StartupContentAuthoring.Include(world,"contract.system.start");
            var doc = Json.Parse(FrameworkProofContent.Read("system-capability-consumers.json"));
            if (!independent) {
                var target = doc.Get("entities")[0].Get("components"); var metadata = target.Get("independent_recurring");
                metadata.Remove("metadata_fault"); target.Remove("independent_recurring"); target.Set("recurring_activity",metadata);
            }
            var probe = new InitializeProbe("proof.system-consumers.initialize"); initialize = probe;
            var module = new ProbeModule("proof.system-consumers",new ModuleVersion(1,0,0),
                content:t=>t.Component<IndependentRecurringActivityDef>("independent_recurring",8),
                compose:c=>c.AddSystem(probe,303));
            return PointGame.Factory(world.ToString(),additionalModule:module,
                moduleDeclaration:"{\"id\":\"proof.system-consumers\",\"version\":\"1.0.0\",\"documents\":[\"system-capability-consumers.json\"]}",
                moduleDocument:doc.ToString());
        }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest {
            Scenario="points",Seed=933,Seats={Controller.Human,Controller.Human}});
        private static EntityView Entity(SimGame g,string name) => g.Seat(0).Snapshot.Entities.Single(x=>x.Def==name);
        private static WorkView Work(SimGame g,int seat=0) => g.Seat(seat).View.Feature<WorkView>();
        private static Fix Stock(SimGame g,string name)
        {
            var facts=new List<Facet>();if(!g.Seat(0).View.Inspect(Entity(g,name).Id,facts))throw new InvalidOperationException("missing actual inventory");
            return facts.Single(x=>x.Key==Storage.FacetStock&&x.Subject=="supply").Value;
        }
        private static List<PlanView> Plans(SimGame g)
        {var rows=new List<PlanView>();Work(g).CopyPlans(rows);return rows.Where(x=>x.Origin.Source==Routine).ToList();}
        private static RecurringPlanView Schedule(SimGame g)
        {var refusal=Work(g).ReadRecurringPlan(Entity(g,"contract.system.target").Id,Routine,out var view);if(!refusal.IsNone)throw new InvalidOperationException("recurring facts "+refusal);return view;}
        private static void Reach(SimGame g,SimGame twin,Func<bool> done,string stage)
        {
            for(int i=0;i<320&&!done();i++)Step(g,twin);
            if(!done())throw new InvalidOperationException(stage+" exceeded320Updates clock="+g.Clock.StepIndex+" plans="+
                string.Join(";",Plans(g).Select(x=>x.Id+"/"+x.State+"/"+x.Blocker)));
        }
        private static void Configure(Check c,SimGame g,bool enabled)
            =>PermanentRemovalTests.Submit(c,g,new ConfigureRecurringPlanCommand {
                Entity=Entity(g,"contract.system.target").Id,Source=Routine,
                Settings=new RecurringPlanSettings(enabled,40,GameDuration.Minutes(6),Code.None)});
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual capability consumption build");
            c.Equal(154,EngineInfo.SaveFormat,"original worker/schedule/physical codecs unchanged");
            c.That(!typeof(Worker).IsAssignableFrom(typeof(ContractWorker)),"independent worker runtime is not native Worker");
            c.That(typeof(IWorker).IsAssignableFrom(typeof(ContractWorker)),"independent runtime binds the existing worker contract");
            foreach(bool independent in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,half,independent);
        }
        private static void Physical(Check c,bool half,bool independent)
        {
            var factory=Factory(half,independent,out var init);var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
            c.Equal(2,init.Initializations,"selected original systems initialize normally per session");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId)&&!game.Modules.Contains(TileNavigationModule.ModuleId),"physical contract game needs neither business nor tile navigation");
            var worker=Entity(game,"machine");c.Equal(Code.None,Work(game).ReadWorkerExecution(worker.Id,out var facts),"custom runtime specification projects through engine contracts");
            c.That(facts.Roles.Count==1&&facts.Roles[0]==new WorkerRoleId("haul")&&facts.BaseRate==Fix.FromRatio(3,2),"actual single-role independent metadata owns worker rate");
            c.That(game.Seat(0).View.TryFindResource("supply",out var resource),"authored supply resolves publicly");
            var target=Entity(game,"contract.system.target").Id;
            var limits=new ReplayLimits(2*1024*1024,2200);using var recorder=game.RecordReplay(limits);
            PermanentRemovalTests.Submit(c,game,new HaulResourcesCommand {
                Source=Entity(game,"source").Id,Destination=target,Worker=worker.Id,Resource=resource,Ceiling=Fix.FromInt(4),Priority=100});
            c.Equal(Fix.Zero,Stock(game,"contract.system.target"),"accepted intent moves no instant cargo");
            var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"saved independent assignment retains original owners");
            Reach(game,twin,()=>Stock(game,"contract.system.target")==Fix.FromInt(4),"independent pickup/travel/handling");
            c.Equal(Fix.FromInt(16),Stock(game,"source"),"physical delivery debits original source once");
            c.Equal(Fix.FromInt(18),((PointSnapshot)Entity(game,"machine").Spatial).Position,"independent motion reaches actual endpoint");
            Configure(c,game,true);twin=factory.Load(game.Save());
            Reach(game,twin,()=>Plans(game).Any(x=>x.State==PlanState.Active),"authored recurrence enters original physical graph");
            var plan=Plans(game).Single();var nodes=new List<ActivityNodeView>();Work(game).TryCopyNodes(plan.Order,nodes);
            c.That(nodes.Any(x=>x.Worker==worker.Id),"original system assigns actual independent worker through role contract");
            var foreign=new List<ActivityNodeView>();c.That(!Work(game,1).TryCopyNodes(plan.Order,foreign)&&foreign.Count==0,"foreign company cannot read actual private work");
            Configure(c,game,false);twin=factory.Load(game.Save());
            game.Clock.Pause();twin.Clock.Pause();ulong paused=game.Hash();Step(game,twin);c.Equal(paused,game.Hash(),"sole paused clock freezes productive work");game.Clock.Resume();twin.Clock.Resume();
            Reach(game,twin,()=>Plans(game).Single().State==PlanState.Done,"disabled source retains already-posted earned work");
            Reach(game,twin,()=>Schedule(game).Schedule.LastCompletedPlan==plan.Id,"next scheduling pass records actual successful completion");
            c.Equal(1,Plans(game).Count,"disabled source posts no replacement cycle");
            c.That(Schedule(game).Schedule.LastCompletedPlan==plan.Id,"actual metadata contract retains successful completion chronology");
            c.Equal(Fix.FromInt(4),Stock(game,"contract.system.target"),"timed service does not invent or consume unauthored resources");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"native/direct metadata and independent worker continue through actual save codecs");
            var verified=new ReplayVerifier().Verify(factory,recorder.Finish(),limits);c.That(verified.Success&&verified.Game.Hash()==game.Hash(),"ordinary hauling/configuration/physical recurrence replays identically");
            c.Equal(isolated,idle.Hash(),"contract providers share no assignment/stock/clock state across sessions");
        }
    }
}
