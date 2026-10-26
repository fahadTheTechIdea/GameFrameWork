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
    internal static partial class SharedCargoRecoveryTests
    {
        internal const string ReplacementCase="Shared cargo replacement: acquired claim refusal, alias rebinding and saved continuation";
        private static void RegisterReplacement()=>TestRunner.Add(ReplacementCase,SharedReplacement);
        private static void RefuseReplacement(Check c,SimGame game,PostOrderCommand command,Code expected)
        {
            ulong before=game.Hash();c.Equal(expected,game.Seat(0).Preview(command),"actual live commitment refuses replacement");
            c.Equal(before,game.Hash(),"replacement refusal preview is pure");int sequence=game.Seat(0).Submit(command);PermanentRemovalTests.Step(game);
            long cursor=0;var results=new List<CommandResult>();game.Seat(0).ReadResults(ref cursor,results);
            c.That(results.Any(x=>x.Sequence==sequence&&x.Refusal==expected),"replacement inbox preserves same live-claim refusal");
        }
        private static void SharedReplacement(Check c)
        {
            foreach(bool replaceCarrier in new[]{false,true})
            {
                var factory=Factory("replacement",worldAuthor:root=>{
                    if(replaceCarrier)StartupContentAuthoring.Position(root,"crew",JsonValue.Number("23"));
                    root.Get("entities")[3].Set("components",Json.Parse(FrameworkProofContent.Read("bound-cargo-replacement.json"))
                        .Get("entities")[0].Get("components").Clone());
                    root.Get("entity_replacement_effects")[0].Set("source",JsonValue.Of("cargo.carrier"));
                });
                var game=New(factory);var idle=New(factory);ulong idleHash=idle.Hash();
                var limits=new ReplayLimits(4*1024*1024,6000);using var recorder=game.RecordReplay(limits);
                var fleet=game.Seat(0).Snapshot.Entities.Where(x=>x.Def=="cargo.carrier").OrderBy(x=>x.Id.Value).ToArray();
                EntityId first=fleet[0].Id,peer=fleet[1].Id,store=Entity(game,"cargo.container"),receiver=Entity(game,"destination");
                Submit(c,game,new SetWorkerHeldCommand{Worker=Entity(game,"machine"),Held=true});
                Submit(c,game,new SetWorkerHeldCommand{Worker=peer,Held=true});
                int pickup=checked((int)Submit(c,game,new HaulResourcesCommand{Source=Entity(game,"source"),Destination=receiver,Worker=first,
                    Resource=Supply(game),Ceiling=Fix.FromInt(8),Priority=100}).Arg0);
                var twin=Save(c,factory,game);Reach(game,twin,()=>Job(game,pickup).Loaded==Fix.FromInt(8),"actual cargo loading before shared replacement");
                Submit(c,game,new SetWorkerHeldCommand{Worker=first,Held=true});twin=Save(c,factory,game);
                for(int i=0;i<5;i++)PermanentRemovalTests.Step(game,twin);var original=Recoveries(game).Single();
                Submit(c,game,new SetWorkerHeldCommand{Worker=peer,Held=false});twin=Save(c,factory,game);
                Reach(game,twin,()=>Jobs(game).Any(x=>x.IsHaul&&x.Worker==peer&&x.Loaded.IsPositive&&x.Loaded<Fix.FromInt(8)),
                    "peer reaches partial physical unloading with real remaining cargo claims");
                int returnJob=Jobs(game).Single(x=>x.IsHaul).Id;
                RefuseReplacement(c,game,new PostOrderCommand{Target=store,Plan="cargo.upgrade"},Upgrades.InventoryReserved);
                RefuseReplacement(c,game,new PostOrderCommand{Target=peer,Plan="upgrade"},Upgrades.WorkerBusy);
                c.That(Job(game,returnJob).Worker==peer&&Job(game,returnJob).Loaded.IsPositive&&Facts(game,store).OutboundPromised==Job(game,returnJob).Loaded,
                    "refused buffer and busy-carrier upgrades retain the original live job and actual promise");
                c.Equal(Fix.FromInt(20),Total(game),"normal inbox advancement during refusals conserves actual stock");
                if(!replaceCarrier)
                {
                    Submit(c,game,new SetWorkerHeldCommand{Worker=peer,Held=true});
                    c.That(!Jobs(game).Any(x=>x.Id==returnJob)&&Facts(game,store).OutboundPromised.IsZero,"ordinary cancellation settles acquired buffer claims before replacement");
                }
                var before=Recoveries(game).Single();var priorCargo=Cargo(game,first);Fix position=At(game,first),retained=Facts(game,store).Stock;
                int upgrade=checked((int)Submit(c,game,new PostOrderCommand{Target=replaceCarrier?first:store,Plan=replaceCarrier?"upgrade":"cargo.upgrade"}).Arg0);
                twin=Save(c,factory,game);Reach(game,twin,()=>Node(game,upgrade,"upgrade").Progress.WholeTicks>0,"shared reference upgrade earns staffed work");
                twin=Save(c,factory,game);string prototype=replaceCarrier?"source.next":"cargo.container.next";
                Reach(game,twin,()=>game.Seat(0).Snapshot.Entities.Any(x=>x.Def==prototype),"shared reference upgrade physically completes");
                EntityId made=Entity(game,prototype),actualFirst=replaceCarrier?made:first,actualStore=replaceCarrier?store:made;
                var rebound=Recoveries(game).Single();
                c.That(rebound.RecoveryOrigin==actualFirst&&rebound.SourceEndpoint==ResourceEndpointId.ForEntity(actualStore)&&
                    rebound.CreatedTick==original.CreatedTick&&rebound.LastDispatchTick==before.LastDispatchTick,"single shared recovery changes only actual replaced reference, never chronology");
                c.That(Cargo(game,actualFirst).Endpoint==rebound.SourceEndpoint&&Cargo(game,peer).Endpoint==rebound.SourceEndpoint,
                    "both actual providers agree with original recovery on one cargo buffer");
                c.Equal(position,At(game,actualFirst),"held carrier is never teleported by either replacement");
                c.Equal(Fix.FromInt(20),Total(game),"shared replacement keeps actual prefix deliveries plus retained cargo");
                if(replaceCarrier)
                    c.That(Job(game,returnJob).Worker==peer&&Job(game,returnJob).Loaded.IsPositive&&Facts(game,actualStore).OutboundPromised==Job(game,returnJob).Loaded,
                        "idle alias replacement preserves peer job identity and acquired remaining promise");
                else
                    c.That(!Exists(game,store)&&Facts(game,actualStore).Stock==retained&&Facts(game,actualStore).OutboundPromised.IsZero,
                        "unreserved buffer replacement relocates only the real residual stock");
                c.That(priorCargo.Endpoint==ResourceEndpointId.ForEntity(store)&&before.SourceEndpoint==priorCargo.Endpoint,"earlier shared cargo facts remain detached");
                twin=Save(c,factory,game);
                if(!replaceCarrier)
                {
                    Submit(c,game,new SetWorkerHeldCommand{Worker=peer,Held=false});twin=Save(c,factory,game);
                    Reach(game,twin,()=>Jobs(game).Any(x=>x.IsHaul&&x.Worker==peer),"same peer resumes residual shared return after buffer replacement");
                }
                Reach(game,twin,()=>{c.Equal(Fix.FromInt(20),Total(game),"shared replacement delivery conserves stock each update");
                    return Facts(game,receiver).Stock==Fix.FromInt(8)&&!Jobs(game).Any()&&Recoveries(game).Count==0;},"original and residual shared deliveries finish physically");
                c.That(Facts(game,actualStore).Stock.IsZero&&Facts(game,actualStore).OutboundPromised.IsZero&&!Cargo(game,actualFirst).HasResidualCargo&&!Cargo(game,peer).HasResidualCargo,
                    "one actual shared buffer drains and all acquired claims settle once");
                c.Equal(Code.None,game.Seat(0).Preview(new PostOrderCommand{Target=peer,Plan="upgrade"}),"finished peer becomes normally replaceable after physical job settlement");
                c.Equal(0,Recoveries(game,1).Count,"shared replacement recovery remains private");
                c.Equal(idleHash,idle.Hash(),"shared replacement leaves another session unchanged");
                c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"shared replacement terminal state is durable");
                c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"claim refusals, shared replacement and physical continuation replay identically");
            }
        }
    }
}
