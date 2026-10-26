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
using static Beep.SimEngine.Tests.PermanentRemovalTests;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Player commands and immutable observations only. One actual inventory, no driver scheduler or cargo mutation.
    internal static partial class SharedCargoRecoveryTests
    {
        internal const string Case="Shared cargo recovery: eligible carrier ranking, retained age and physical handoff";
        internal static void Register(){TestRunner.Add(Case,Run);RegisterPolicies();RegisterReplacement();}
        private static GameFactory Factory(string mode,Action<JsonValue> author=null,Action<JsonValue> worldAuthor=null,
            IReadOnlyList<GameModule> extraModules=null,JsonValue extraDeclarations=null)=>BoundCargoReplacementTests.Factory(edit:content=>{
            author?.Invoke(content);
            if(mode!="tie")return;
            var entries=content.Get("entity_setups");
            for(int i=0;i<entries.Count;i++)if(entries[i].Get("id").Text=="cargo.peer")
                entries[i].Get("placement").Set("position",JsonValue.Number("61"));
        },worldEdit:root=>{
            StartupContentAuthoring.Include(root,"cargo.shared-start");
            root.Get("scenarios")[0].Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId,Json.Parse("{\"profile\":\"cargo.automatic-recovery\"}"));
            root.Get("entities")[0].Get("components").Get("point_stock").Set("public",JsonValue.Of(false));
            root.Get("point_spaces")[0].Set("interaction_reach",JsonValue.Number("1"));
            worldAuthor?.Invoke(root);
        },extraModules:extraModules,extraDeclarations:extraDeclarations);
        private static SimGame New(GameFactory factory)=>factory.NewGame(new NewGameRequest{
            Scenario="points",Seed=61,Seats=new List<Controller>{Controller.Human,Controller.Human},Mods={ResourceFlowsModule.ModuleId}});
        private static List<LogisticsDispatchOffer> Recoveries(SimGame game,int seat=0)
        {var rows=new List<LogisticsDispatchOffer>();game.Seat(seat).View.Feature<LogisticsView>().CopyQueue(rows);return rows.Where(x=>x.Kind==LogisticsDispatchKind.CargoReturn).ToList();}
        private static CargoDispositionSnapshot Cargo(SimGame game,EntityId worker)
        {var code=game.Seat(0).View.Feature<LogisticsView>().ReadCargoDisposition(worker,out var value);if(!code.IsNone)throw new InvalidOperationException("shared cargo "+code);return value;}
        private static void ReleaseBoth(Check c,SimGame game,EntityId first,EntityId second)
        {
            var sequences=new List<int>();
            foreach(EntityId worker in new[]{first,second})
            {var command=new SetWorkerHeldCommand{Worker=worker,Held=false};c.Equal(Code.None,game.Seat(0).Preview(command),"public same-update release admission");sequences.Add(game.Seat(0).Submit(command));}
            PermanentRemovalTests.Step(game);long cursor=0;var results=new List<CommandResult>();game.Seat(0).ReadResults(ref cursor,results);
            c.That(sequences.All(sequence=>results.Any(x=>x.Sequence==sequence&&x.Accepted)),"engine processes both player releases before dispatch");
        }
        private static void Scenario(Check c,string mode)
        {
            var factory=Factory(mode);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            var limits=new ReplayLimits(4*1024*1024,6000);using var recorder=game.RecordReplay(limits);
            var fleet=game.Seat(0).Snapshot.Entities.Where(x=>x.Def=="cargo.carrier").OrderBy(x=>x.Id.Value).ToArray();
            c.Equal(2,fleet.Length,"explicit JSON startup authors two transporters sharing one cargo inventory");
            EntityId first=fleet[0].Id,peer=fleet[1].Id,container=Entity(game,"cargo.container"),receiver=Entity(game,"destination");
            Submit(c,game,new SetWorkerHeldCommand{Worker=Entity(game,"machine"),Held=true});
            Submit(c,game,new SetWorkerHeldCommand{Worker=peer,Held=true});
            if(mode=="priority")Submit(c,game,new ConfigureWorkPolicyCommand{Worker=first,Settings=new WorkPolicySettings(true,100,
                new[]{new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            int pickup=checked((int)Submit(c,game,new HaulResourcesCommand{Source=Entity(game,"source"),Destination=receiver,Worker=first,
                Resource=Supply(game),Ceiling=Fix.FromInt(8),Priority=100}).Arg0);
            var twin=Save(c,factory,game);Reach(game,twin,()=>Job(game,pickup).Loaded==Fix.FromInt(8),"actual shared-buffer loading");
            Submit(c,game,new SetWorkerHeldCommand{Worker=first,Held=true});
            Fix reserve=mode=="held"?Fix.FromInt(2):Fix.Zero,quantity=Fix.FromInt(8)-reserve;
            if(reserve.IsPositive)Submit(c,game,new ConfigureStockPolicyCommand{Store=container,Resource=Supply(game),
                Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,reserve,reserve,Fix.FromInt(8),73,LogisticsReserveAccess.Protected))});
            twin=Save(c,factory,game);for(int i=0;i<5;i++)PermanentRemovalTests.Step(game,twin);
            var before=Recoveries(game).Single();var original=Cargo(game,first);var alternate=Cargo(game,peer);
            c.That(before.SourceEndpoint==ResourceEndpointId.ForEntity(container)&&before.Requested==quantity&&before.AgeTicks>=5,
                "one actual recovery chronology owns spare shared cargo");
            c.That(original.Endpoint==alternate.Endpoint&&original.Retained.AmountOf(Supply(game))==reserve&&alternate.Retained.AmountOf(Supply(game))==reserve,
                "both providers observe the same protected stock, not two cargo copies");
            ulong pure=game.Hash();var returns=new List<CargoReturnView>();
            c.Equal(Code.None,game.Seat(0).View.Feature<LogisticsView>().CopyCargoReturns(peer,returns),"alternate carrier reads actual return preview");
            c.That(returns.Single().Return.Status!=HaulStatus.NoSupply,"held alternate still has real shared supply, not a fabricated missing intent");
            c.Equal(pure,game.Hash(),"shared candidate and return observations do not rebase persistent origin or age");
            if(mode=="held")Submit(c,game,new SetWorkerHeldCommand{Worker=peer,Held=false});else ReleaseBoth(c,game,first,peer);
            twin=Save(c,factory,game);Reach(game,twin,()=>Jobs(game).Any(x=>x.IsHaul),"eligible shared carrier acquires one recovery");
            var assigned=Jobs(game).Single(x=>x.IsHaul);EntityId chosen=mode=="held"||mode=="cost"?peer:first;
            c.Equal(chosen,assigned.Worker,"native carrier priority, travel cost and stable identity select the authored expected worker: "+mode);
            var active=Recoveries(game).Single();
            c.Equal(before.RecoveryOrigin,active.RecoveryOrigin,"worker selection does not rewrite the logical recovery origin");
            c.That(active.SourceEndpoint==before.SourceEndpoint&&active.CreatedTick==before.CreatedTick&&active.LastDispatchTick>=before.LastDispatchTick,
                "selection preserves one intent and original starvation age, dispatch alone stamps last attempt");
            c.Equal(quantity,Facts(game,container).OutboundPromised,"one actual shared cargo-out promise, not one per alias");
            c.Equal(quantity,Cargo(game,first).Committed.AmountOf(Supply(game)),"shared disposition sees actual committed cargo once");
            c.That(assigned.Loaded==quantity&&Facts(game,receiver).Stock.IsZero,"recovery acquisition creates no instant delivery");
            if(mode=="held")
            {
                Submit(c,game,new SetWorkerHeldCommand{Worker=peer,Held=true});twin=Save(c,factory,game);
                c.That(!Jobs(game).Any(x=>x.Id==assigned.Id)&&Facts(game,container).OutboundPromised.IsZero&&Facts(game,container).Stock==Fix.FromInt(8),
                    "cancelled alternate releases real promises without losing or refunding shared stock");
                var released=Recoveries(game).Single();
                c.That(released.CreatedTick==before.CreatedTick&&released.LastDispatchTick==active.LastDispatchTick,"cancellation preserves the original recovery chronology");
                game.Clock.Pause();twin.Clock.Pause();ulong paused=game.Hash();c.Equal(0,game.Update(1000000),"sole paused clock freezes shared recovery");
                c.Equal(0,twin.Update(1000000),"saved shared recovery freezes identically");c.That(game.Hash()==paused&&twin.Hash()==paused,"paused shared recovery checkpoint");
                game.Clock.Resume();twin.Clock.Resume();Submit(c,game,new SetWorkerHeldCommand{Worker=first,Held=false});twin=Save(c,factory,game);
                Reach(game,twin,()=>Jobs(game).Any(x=>x.IsHaul&&x.Worker==first),"original eligible carrier physically takes over same shared recovery");
                c.That(Recoveries(game).Single().CreatedTick==before.CreatedTick&&Facts(game,container).OutboundPromised==quantity,
                    "handoff keeps original age with exactly one replacement promise");chosen=first;
            }
            twin=Save(c,factory,game);long started=game.Clock.StepIndex;
            Reach(game,twin,()=>{c.Equal(Fix.FromInt(20),Total(game),"actual shared stock conserved through every delivery update");
                return Facts(game,receiver).Stock==quantity&&!Jobs(game).Any()&&Recoveries(game).Count==0;},"shared cargo physically returns and settles");
            c.That(game.Clock.StepIndex>started&&Facts(game,container).Stock==reserve&&Facts(game,container).OutboundPromised.IsZero,
                "timed delivery drains only spare cargo and leaves the one actual reserve");
            c.That(!Cargo(game,first).HasResidualCargo&&!Cargo(game,peer).HasResidualCargo,"no duplicate residual recovery remains on either provider");
            c.That(before.Requested==quantity&&before.SourceEndpoint==original.Endpoint,"retained historical facts remain detached");
            c.Equal(0,Recoveries(game,1).Count,"shared recovery offers stay company-private");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"shared recovery terminal codec is coherent");
            c.Equal(isolatedHash,isolated.Hash(),"another session has no shared recovery changes");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"shared carrier selection and physical handoff replay identically");
        }
        private static void Run(Check c){foreach(string mode in new[]{"held","cost","priority","tie"})Scenario(c,mode);}
    }
}
