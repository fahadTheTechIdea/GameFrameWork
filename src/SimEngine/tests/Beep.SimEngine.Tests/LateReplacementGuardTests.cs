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
    internal static class LateReplacementGuardTests
    {
        internal const string Case="Replacement retirement guards: earned relocation faults cannot publish healthy success";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(string mode)=>EntityReferenceReplacementTests.Factory(edit:root=>
            root.Get("entities")[0].Get("components").Set("replacement_probe",Json.Parse(
                "{\"allowed_targets\":[\"source.next\"],\"fail_initialization\":false,\"check_fault\":\"retirement-"+mode+"\"}")));
        private static SimGame New(GameFactory factory)=>factory.NewGame(new NewGameRequest{Scenario="points",Seed=67,
            Seats=new List<Controller>{Controller.Human,Controller.Human},Mods={ResourceFlowsModule.ModuleId}});
        private static void Run(Check c)
        {
            foreach(string mode in new[]{"none","payload","payload-deny","clock","space","throw","deny"})
            {
                var factory=Factory(mode);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
                var limits=new ReplayLimits(4*1024*1024,6000);using var recorder=mode=="none"?game.RecordReplay(limits):null;
                EntityId source=Entity(game,"source");ResourceId resource=Supply(game);
                Submit(c,game,new SetWorkerHeldCommand{Worker=Entity(game,"machine"),Held=true});
                var intent=new PostOrderCommand{Target=source,Plan="upgrade"};ulong before=game.Hash();
                c.Equal(Code.None,game.Seat(0).Preview(intent),"late guard cannot fault positive stock-bearing admission");
                c.Equal(before,game.Hash(),"late guard admission remains pure");
                int order=checked((int)Submit(c,game,intent).Arg0);var twin=Save(c,factory,game);
                Reach(game,twin,()=>Node(game,order,"upgrade").Progress.WholeTicks>0,"real crew earns partial replacement work");
                c.That(Exists(game,source)&&Facts(game,source).Stock==Fix.FromInt(20)&&game.Fault==null,"pending work has no relocation or late failure");
                twin=Save(c,factory,game);var prior=Facts(game,source);var priorFlow=game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
                int priorCount=priorFlow.Entries.Count;long priorLatest=priorFlow.Latest;
                EntityId next=game.World.Entities.NextIdentity;var published=game.Seat(0).Snapshot;
                int steps=0;
                while(game.Fault==null&&!game.Seat(0).Snapshot.Entities.Any(x=>x.Def=="source.next")&&steps++<24)
                {
                    published=game.Seat(0).Snapshot;game.Update();twin.Update();
                    if(game.Fault==null)c.Equal(game.Hash(),twin.Hash(),"saved partial work remains deterministic through healthy updates");
                }
                c.That(steps<=24,"late retirement boundary is bounded, never an unobserved stall");
                if(mode=="none")
                {
                    EntityId made=Entity(game,"source.next");
                    c.That(game.Fault==null&&twin.Fault==null&&!Exists(game,source)&&Facts(game,made).Stock==Fix.FromInt(20),"healthy control completes full staffed receipt relocation");
                    var flow=game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
                    c.That(flow.Entries.Count(x=>x.Item.IsTransfer&&x.Item.Source.Endpoint==ResourceEndpointId.ForEntity(source)&&
                        x.Item.Destination.Endpoint==ResourceEndpointId.ForEntity(made)&&x.Item.Amount==Fix.FromInt(20))==1,"healthy relocation has exactly one actual receipt transfer");
                    c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"healthy late-guard control saves normally");
                    c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"healthy physical late-guard control replays identically");
                }
                else
                {
                    c.That(game.Fault!=null&&twin.Fault!=null,"both saved sessions fault at the earned late boundary: "+mode);
                    c.Equal(mode=="deny"?"entity.replace":"observation.provider",game.Fault?.Phase,"original guard or replacement first-fault owner");
                    c.Equal(game.Fault?.Phase,twin.Fault?.Phase,"saved late guard has same fault boundary");
                    c.Equal(game.Clock.StepIndex,twin.Clock.StepIndex,"saved sessions earn identical time before late failure");
                    c.Equal(published.Tick,game.Seat(0).Snapshot.Tick,"fault retains last healthy publication");
                    c.That(!game.Seat(0).Snapshot.Entities.Any(x=>x.Def=="source.next"),"failed target is never published as healthy completion");
                    // Fault inspection only: observe actual interrupted engine state, never
                    // restore stock/identities or implement compensation in a player driver.
                    var old=game.World.Entities.Get(source);var candidate=game.World.Entities.Get(next);
                    c.That(old?.Alive==true&&candidate?.Alive==true&&candidate.Def.Id=="source.next","actual interrupted identities remain, not a fabricated rollback");
                    c.That(old.Get<IInventory>().Amount(resource).IsZero&&candidate.Get<IInventory>().Amount(resource)==Fix.FromInt(20),
                        "real stock relocated before late failure, never duplicated or restored by test code");
                    if(mode=="payload"||mode=="payload-deny")c.Equal(1,old.Get<SingleResourcePort>().ServiceCount,"actual companion payload mutation is observed without Touch");
                    if(mode=="throw")c.That(game.Fault.Message.Contains("authored replacement retirement exception"),"authored late callback cause retained");
                    if(mode=="deny")c.That(game.Fault.Message.Contains(ReplacementProbe.Denied.Id),"late typed refusal remains truthful after irreversible relocation");
                    var firstFault=game.Fault;
                    c.Throws<InvalidOperationException>(()=>game.Save(),"faulted late replacement cannot save healthy");
                    c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted late replacement cannot hash healthy");
                    c.Throws<InvalidOperationException>(()=>game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0),"live receipt history withholds failed success prefix");
                    c.Equal(Codes.NotAllowed,Upgrades.Check(game.World,old,old.Def),"terminal replacement refuses before re-entering callback");
                    c.That(ReferenceEquals(firstFault,game.Fault),"subsequent refusals preserve original first fault");
                }
                c.That(prior.Stock==Fix.FromInt(20)&&priorFlow.Entries.Count==priorCount&&priorFlow.Latest==priorLatest,"retained detached healthy facts remain historical");
                c.Equal(isolatedHash,isolated.Hash(),"late guard failure/completion isolates another session");
            }
        }
    }
}
