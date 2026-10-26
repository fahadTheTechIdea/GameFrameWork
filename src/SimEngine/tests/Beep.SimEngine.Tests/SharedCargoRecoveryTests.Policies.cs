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
        internal const string DenialCase="Shared cargo policy gates: denied roles areas standby and physical release";
        internal const string GuardCase="Dispatch candidate priority: selected and losing policy callbacks cannot mutate or escape faults";
        private static void RegisterPolicies(){TestRunner.Add(DenialCase,Denials);TestRunner.Add(GuardCase,PriorityGuards);}
        private static void Denials(Check c)
        {
            foreach(string mode in new[]{"role","area","disabled","standby"})
            {
                var factory=Factory("denials");var game=New(factory);var idle=New(factory);ulong idleHash=idle.Hash();
                var limits=new ReplayLimits(4*1024*1024,6000);using var recorder=game.RecordReplay(limits);
                var fleet=game.Seat(0).Snapshot.Entities.Where(x=>x.Def=="cargo.carrier").OrderBy(x=>x.Id.Value).ToArray();
                EntityId first=fleet[0].Id,peer=fleet[1].Id,store=Entity(game,"cargo.container"),receiver=Entity(game,"destination");
                Submit(c,game,new SetWorkerHeldCommand{Worker=Entity(game,"machine"),Held=true});
                Submit(c,game,new SetWorkerHeldCommand{Worker=peer,Held=true});
                int pickup=checked((int)Submit(c,game,new HaulResourcesCommand{Source=Entity(game,"source"),Destination=receiver,Worker=first,
                    Resource=Supply(game),Ceiling=Fix.FromInt(8),Priority=100}).Arg0);
                var twin=Save(c,factory,game);Reach(game,twin,()=>Job(game,pickup).Loaded==Fix.FromInt(8),"real cargo before denied policy");
                Submit(c,game,new SetWorkerHeldCommand{Worker=first,Held=true});var before=Recoveries(game).Single();
                var settings=new WorkPolicySettings(mode!="disabled",200,new[]{new WorkerRoleId(mode=="role"?"inspect":"haul")},
                    mode=="area"?"cargo.peer-only-area":null,true,true,mode=="standby"?WorkAssignmentMode.EmergencyAndSelfService:WorkAssignmentMode.All);
                Submit(c,game,new ConfigureWorkPolicyCommand{Worker=peer,Settings=settings});
                Submit(c,game,new SetWorkerHeldCommand{Worker=peer,Held=false});twin=Save(c,factory,game);
                Code expected=mode=="role"?WorkPolicyCodes.Role:mode=="area"?WorkPolicyCodes.OutsideArea:mode=="disabled"?WorkPolicyCodes.Disabled:WorkPolicyCodes.Standby;
                ulong pure=game.Hash();var returns=new List<CargoReturnView>();
                c.Equal(Code.None,game.Seat(0).View.Feature<LogisticsView>().CopyCargoReturns(peer,returns),"owned denied candidate facts");
                c.Equal(expected,returns.Single().Return.Blocker,"full native policy refuses shared candidate: "+mode);
                c.Equal(pure,game.Hash(),"denied preview changes no recovery age, policy or stock");
                Fix position=At(game,peer);for(int i=0;i<6;i++)PermanentRemovalTests.Step(game,twin);
                var denied=Recoveries(game).Single();
                c.That(!Jobs(game).Any()&&Facts(game,store).Stock==Fix.FromInt(8)&&Facts(game,store).OutboundPromised.IsZero,
                    "higher-priority denied carrier gets no job or reservation");
                c.That(denied.CreatedTick==before.CreatedTick&&denied.LastDispatchTick==before.LastDispatchTick&&At(game,peer)==position,
                    "denied work preserves original chronology and physical position");
                var valid=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All);
                Submit(c,game,new ConfigureWorkPolicyCommand{Worker=peer,Settings=valid});twin=Save(c,factory,game);
                Reach(game,twin,()=>Jobs(game).Any(x=>x.IsHaul&&x.Worker==peer),"ordinary policy edit releases shared candidate");
                c.Equal(before.CreatedTick,Recoveries(game).Single().CreatedTick,"policy release retains original recovery age");
                twin=Save(c,factory,game);Reach(game,twin,()=>Facts(game,receiver).Stock==Fix.FromInt(8)&&!Jobs(game).Any()&&Recoveries(game).Count==0,
                    "released candidate physically delivers original cargo");
                c.That(Facts(game,store).Stock.IsZero&&Facts(game,store).OutboundPromised.IsZero&&Total(game)==Fix.FromInt(20),"denied and released phases conserve stock and claims");
                c.That(before.Requested==Fix.FromInt(8)&&denied.CreatedTick==before.CreatedTick,"retained denied facts stay detached");
                c.Equal(0,Recoveries(game,1).Count,"policy recovery remains private");c.Equal(idleHash,idle.Hash(),"denied policy isolates other session");
                c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"released terminal state durable");
                c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"denied policies and physical release replay identically");
            }
        }
        // White-box boundary diagnostic, NOT a player harness. Arm only the external
        // provider's negative instrumentation; invoke the original comparator directly.
        // No stock, job, progress, geometry or simulation-time state is written here.
        private static void PriorityGuards(Check c)
        {
            var factory=Factory("guards",content=>{
                var parts=content.Get("entities")[0].Get("components");parts.Remove("work_policy");
                parts.Set("proof_recovery_work_policy",Json.Parse("{\"profile\":\"cargo.work-policy\",\"fault\":0}"));
            });
            foreach(string fault in new[]{"touch","payload","throw"})foreach(bool selected in new[]{false,true})
            {
                var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
                var fleet=game.Seat(0).Snapshot.Entities.Where(x=>x.Def=="cargo.carrier").OrderBy(x=>x.Id.Value).ToArray();
                EntityId first=fleet[0].Id,peer=fleet[1].Id,store=Entity(game,"cargo.container");
                var target=game.World.Entities.Get(selected?first:peer).Get<ProofWorkPolicy>();
                var published=game.Seat(0).Snapshot;long clock=game.Clock.StepIndex;
                var a=new WorkerDispatchOffer("sim.logistics",store.Value,"guard.unit",new PlayerId(0),first,first,true,Code.None,false,73,clock,clock,1,1,0,true);
                var b=new WorkerDispatchOffer("sim.logistics",store.Value,"guard.unit",new PlayerId(0),first,peer,true,Code.None,false,73,clock,clock,2,1,0,true);
                target.ReadProbe=fault;bool threw=false;
                try{LogisticsService.CompareCandidates(game.World,a,b);}catch(InvalidOperationException){threw=true;}
                c.That(threw&&game.Fault!=null,"ranking immediately faults selected/losing policy callback: "+fault+"/selected="+selected);
                if(game.Fault==null){target.ReadProbe=null;continue;} // Baseline cleanup of diagnostics only; never a healthy success substitute.
                var firstFault=game.Fault;
                c.Equal(clock,game.Clock.StepIndex,"ranking fault advances no simulation time");
                c.Equal(published.Tick,game.Seat(0).Snapshot.Tick,"ranking fault publishes no success snapshot");
                c.Throws<InvalidOperationException>(()=>game.Save(),"rank-faulted session cannot save");
                c.Throws<InvalidOperationException>(()=>game.Hash(),"rank-faulted session cannot hash healthy");
                c.That(ReferenceEquals(firstFault,game.Fault),"ranking preserves first fault");
                c.Equal(isolatedHash,isolated.Hash(),"rank callback cannot corrupt another session");
            }
        }
    }
}
