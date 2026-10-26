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
    // Public intent/facts only; original logistics owns recovery chronology, acquisition and delivery.
    internal static class BoundCargoRecoveryReplacementTests
    {
        internal const string Case = "Bound cargo recovery replacement: retained endpoint chronology and physical return";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool replaceCarrier)
            => BoundCargoReplacementTests.Factory(worldEdit: root => {
                root.Get("scenarios")[0].Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId,
                    Json.Parse("{\"profile\":\"cargo.automatic-recovery\"}"));
                // Explicit initial pickup remains allowed, but automatic return must travel to the public receiver.
                root.Get("entities")[0].Get("components").Get("point_stock").Set("public",JsonValue.Of(false));
                root.Get("point_spaces")[0].Set("interaction_reach",JsonValue.Number("1"));
                if (!replaceCarrier) return;
                StartupContentAuthoring.Position(root,"crew",JsonValue.Number("23"));
                root.Get("entities")[3].Set("components",Json.Parse(FrameworkProofContent.Read("bound-cargo-replacement.json"))
                    .Get("entities")[0].Get("components").Clone());
                root.Get("entity_replacement_effects")[0].Set("source",JsonValue.Of("cargo.carrier"));
            });
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario="points",Seed=59,Seats=new List<Controller>{Controller.Human,Controller.Human},Mods={ResourceFlowsModule.ModuleId} });
        private static List<LogisticsDispatchOffer> Offers(SimGame game,int seat=0)
        { var rows=new List<LogisticsDispatchOffer>();game.Seat(seat).View.Feature<LogisticsView>().CopyQueue(rows);return rows; }
        private static LogisticsDispatchOffer Recovery(SimGame game) => Offers(game).Single(x=>x.Kind==LogisticsDispatchKind.CargoReturn);
        private static CargoDispositionSnapshot Cargo(SimGame game,EntityId worker)
        {
            var result=game.Seat(0).View.Feature<LogisticsView>().ReadCargoDisposition(worker,out var value);
            if(!result.IsNone)throw new InvalidOperationException("recovery cargo facts: "+result);return value;
        }
        private static void Scenario(Check c,bool replaceCarrier)
        {
            var factory=Factory(replaceCarrier);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            var limits=new ReplayLimits(4*1024*1024,6000);using var recorder=game.RecordReplay(limits);
            EntityId carrier=Entity(game,"cargo.carrier"),container=Entity(game,"cargo.container"),receiver=Entity(game,"destination");
            Submit(c,game,new SetWorkerHeldCommand{Worker=Entity(game,"machine"),Held=true});
            int haul=checked((int)Submit(c,game,new HaulResourcesCommand{Source=Entity(game,"source"),Destination=receiver,Worker=carrier,
                Resource=Supply(game),Ceiling=Fix.FromInt(8),Priority=100}).Arg0);
            var twin=Save(c,factory,game);
            Reach(game,twin,()=>Job(game,haul).Loaded==Fix.FromInt(8),"real loading before automatic return");
            Submit(c,game,new SetWorkerHeldCommand{Worker=carrier,Held=true});twin=Save(c,factory,game);
            Reach(game,twin,()=>Offers(game).Any(x=>x.Kind==LogisticsDispatchKind.CargoReturn),"ordinary held cargo becomes pending recovery");
            for(int i=0;i<5;i++)PermanentRemovalTests.Step(game,twin);
            int retirement=checked((int)Submit(c,game,new PostOrderCommand{Target=carrier,Plan="cargo.retire"}).Arg0);
            ulong retirementHash=game.Hash();
            c.Equal(ResourceTransactions.UndisposedStock,Work(game).PreviewOrderEffect(retirement),"ordinary retirement completion still protects loaded external cargo");
            c.Equal(retirementHash,game.Hash(),"retirement effect preview is pure");
            c.That(Exists(game,carrier)&&Facts(game,container).Stock==Fix.FromInt(8),"retirement intent cannot discard actual cargo");
            Submit(c,game,new CancelOrderCommand{Order=retirement});
            {
                var refusal=new PostOrderCommand{Target=carrier,Plan="cargo.discard-binding"};ulong stable=game.Hash();
                c.Equal(ResourceTransactions.UndisposedStock,game.Seat(0).Preview(refusal),"replacement losing binding still protects loaded external cargo");
                c.Equal(stable,game.Hash(),"loaded retirement refusal is pure");
                int sequence=game.Seat(0).Submit(refusal);PermanentRemovalTests.Step(game);long cursor=0;var results=new List<CommandResult>();
                game.Seat(0).ReadResults(ref cursor,results);
                c.That(results.Any(x=>x.Sequence==sequence&&x.Refusal==ResourceTransactions.UndisposedStock),"preview and inbox agree on protected cargo refusal");
            }
            var before=Recovery(game);Fix position=At(game,carrier);long oldAge=game.Clock.StepIndex-before.CreatedTick;
            c.That(before.RecoveryOrigin==carrier&&before.SourceEndpoint==ResourceEndpointId.ForEntity(container)&&before.Requested==Fix.FromInt(8),
                "one actual intent separates carrier identity from independently owned cargo endpoint");
            c.That(oldAge>=5&&before.Priority==73,"pending recovery has real starvation age and authored priority");
            string prototype=replaceCarrier?"source.next":"cargo.container.next";
            var upgrade=new PostOrderCommand{Target=replaceCarrier?carrier:container,Plan=replaceCarrier?"upgrade":"cargo.upgrade"};
            int order=checked((int)Submit(c,game,upgrade).Arg0);twin=Save(c,factory,game);
            Reach(game,twin,()=>Node(game,order,"upgrade").Progress.WholeTicks>0,"actual staffed reference replacement starts");
            twin=Save(c,factory,game);
            Reach(game,twin,()=>game.Seat(0).Snapshot.Entities.Any(x=>x.Def==prototype),"actual staffed reference replacement completes");
            EntityId made=Entity(game,prototype),actualCarrier=replaceCarrier?made:carrier,actualContainer=replaceCarrier?container:made;
            var after=Recovery(game);
            c.Equal(actualCarrier,after.RecoveryOrigin,"only replaced carrier identity changes recovery origin");
            c.Equal(ResourceEndpointId.ForEntity(actualContainer),after.SourceEndpoint,"only replaced inventory identity changes recovery source");
            c.Equal(before.CreatedTick,after.CreatedTick,"replacement preserves original recovery starvation age");
            c.Equal(before.LastDispatchTick,after.LastDispatchTick,"replacement is not a new dispatch attempt");
            c.That(after.Priority==before.Priority&&after.Requested==before.Requested&&after.SourcePort==before.SourcePort,
                "replacement retains recovery policy, actual amount and source binding");
            c.Equal(position,At(game,actualCarrier),"held carrier has no replacement teleport");
            c.That(Cargo(game,actualCarrier).Endpoint==after.SourceEndpoint&&Facts(game,actualContainer).Stock==Fix.FromInt(8),
                "component and recovery intent agree on the same real inventory");
            c.Equal(Fix.FromInt(20),Total(game),"physical replacement conserves original twenty units");
            c.That(before.RecoveryOrigin==carrier&&before.SourceEndpoint==ResourceEndpointId.ForEntity(container),"earlier detached offer is unchanged");
            c.Equal(0,Offers(game,1).Count,"recovery chronology remains company-private");twin=Save(c,factory,game);
            if(replaceCarrier)
            {
                // Ordinary home intent clears the upgrade crew ahead of the carrier; no collision exemption.
                Submit(c,game,new SetHomeCommand{Worker=Entity(game,"crew"),Home=Entity(game,"cargo.parking")});twin=Save(c,factory,game);
                Reach(game,twin,()=>Work(game).ReadHome(Entity(game,"crew"),out var home).IsNone&&home.State==WorkerHomeState.AtHome,
                    "upgrade crew physically clears return corridor");
                c.That(At(game,Entity(game,"crew"))>At(game,receiver),"authored parking clears the actual receiver approach");
            }
            Submit(c,game,new SetWorkerHeldCommand{Worker=actualCarrier,Held=false});twin=Save(c,factory,game);
            Reach(game,twin,()=>Jobs(game).Any(x=>x.IsHaul&&x.Worker==actualCarrier),"ordinary automatic return acquires retained cargo");
            c.That(Facts(game,receiver).Stock.IsZero&&Cargo(game,actualCarrier).HasResidualCargo,"automatic acquisition cannot instantly deliver cargo");
            twin=Save(c,factory,game);long started=game.Clock.StepIndex;
            Reach(game,twin,()=>Facts(game,receiver).Stock==Fix.FromInt(8)&&!Jobs(game).Any()&&Offers(game).All(x=>x.Kind!=LogisticsDispatchKind.CargoReturn),
                "original automatic return physically delivers and settles claims");
            c.That(game.Clock.StepIndex>started&&At(game,actualCarrier)>position,"return earns actual travel and handling time");
            c.That(Facts(game,actualContainer).Stock.IsZero&&!Cargo(game,actualCarrier).HasResidualCargo,"actual independent cargo buffer drains exactly once");
            c.Equal(Fix.FromInt(20),Total(game),"automatic return creates no stock, refund or duplicate cargo");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"terminal recovery and reference checkpoint is coherent");
            c.Equal(isolatedHash,isolated.Hash(),"independent session remains unchanged");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"replacement and automatic return replay identically");
        }
        private static void Run(Check c) { Scenario(c,false);Scenario(c,true); }
    }
}
