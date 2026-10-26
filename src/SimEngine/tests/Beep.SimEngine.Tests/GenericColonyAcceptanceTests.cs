using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Same player-proof owner; all incident, process, DAG, pressure and victory rules are engine JSON/contracts.
    internal static partial class GenericColonyCompositionTests
    {
        private static void EmergencyChain(Check c)
        {
            const string document = "colony-acceptance.json";
            var factory = Colony.Factory(
                additionalModules: new GameModule[] { new GameConditionsModule(), new ScenarioPressureModule() },
                moduleDocuments: new Dictionary<string, string> { [document] = FrameworkProofContent.Read(document) },
                masterDocument: FrameworkProofContent.Read("colony-acceptance-master.json"));
            SimGame Start() => factory.NewGame(new NewGameRequest { Scenario = "colony-acceptance", Seed = 811,
                Seats = { Controller.Human, Controller.Human } });
            var game = Start(); var isolated = Start(); ulong isolatedHash = isolated.Hash();
            var observations = new Observations(); var plant = One(game,"factory").Id; var depot = One(game,"depot").Id;
            var target = One(game,"acceptance.machine").Id;
            var foreign = game.Seat(1).Snapshot.Entities.Single(e => e.Owner == new PlayerId(1) && e.Def == "acceptance.machine").Id;
            var source = new Code("acceptance.leak");
            IncidentsView Incidents(SimGame session) => session.Seat(0).View.Feature<IncidentsView>();
            IncidentLifecycleView Life(SimGame session, int seat = 0)
            {
                if (!session.Seat(seat).View.Feature<IncidentsView>().TryGetLifecycle(seat==0 ? target : foreign,source,out var view))
                    throw new InvalidOperationException("missing authored incident lifecycle");
                return view;
            }
            List<IncidentPhaseView> Phases(SimGame session)
            {
                var rows = new List<IncidentPhaseView>();
                if (!Incidents(session).CopyPhases(target,source,rows)) throw new InvalidOperationException("missing authored incident phases");
                return rows;
            }
            List<ObjectiveSnapshot> Objectives(SimGame session, int seat = 0)
            {
                var rows = new List<ObjectiveSnapshot>();
                session.Seat(seat).View.Feature<ObjectiveView>().CopyObjectives(rows); return rows;
            }
            ObjectiveSnapshot Objective(SimGame session,string id) => Objectives(session).Single(o=>o.Objective=="acceptance."+id);
            ScenarioPressureSnapshot Pressure(SimGame session)
            {
                var rows=new List<ScenarioPressureSnapshot>();
                session.Seat(0).View.Feature<ScenarioPressureView>().CopyPressure(rows); return rows.Single();
            }
            string Detail()
                => Diagnostics(game,plant)+"/phases="+string.Join(";",Phases(game).Select(p=>p.Id+"/"+p.Complete+"/"+
                    string.Join(",",p.Nodes.Select(n=>n.State+"/"+n.Progress+"/"+n.Blocker))))+
                    "/objectives="+string.Join(";",Objectives(game).Select(o=>o.Objective+"/"+o.State+"/"+o.Blocker))+
                    "/pressure="+Pressure(game).Accepted+"/"+Pressure(game).Spent+"/"+Pressure(game).Blocker;
            void Until(Func<bool> ready,string stage,SimGame twin=null)
            {
                // Wall time is a failure watchdog only, never a simulation clock or completion rule.
                var watchdog=Stopwatch.StartNew();
                for(int updates=0; updates<2000 && watchdog.Elapsed.TotalSeconds<30 && !ready();updates++)
                    Update(game,plant,observations,twin);
                if(!ready()) throw new InvalidOperationException(stage+"/watchdog="+watchdog.Elapsed+"/"+Detail());
            }
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current integrated source identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"no new acceptance state codec");
            c.Equal("colony.survival",game.GameMode.ModeId,"authored resource-only mode");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && game.Seat(0).View.Feature<BusinessView>()==null,
                "physical industry/emergency needs no business, money or market");
            c.Equal(12,game.Snapshot.Entities.Count,"engine-owned JSON starts exactly two independent colonies and response assets");
            c.Equal(Fix.FromInt(60),Steel(game),"actual 50 industrial plus10 preparedness steel");
            c.Equal(Fix.FromInt(10),Stock(game,foreign,1),"foreign emergency reserve remains independently authored");
            c.That(Life(game).Snapshot.Episode==0 && !Life(game).Snapshot.Active,"no test-created emergency");
            c.That(Objectives(game).All(o=>o.State==ObjectiveState.Locked),"objectives begin as engine-owned intent only");
            c.Equal(0,Objectives(game,1).Count,"no other-company objective enrollment leakage");
            ulong hash=game.Hash();c.That(!Incidents(game).TryGetLifecycle(foreign,source,out _),"foreign incidents stay private");
            c.Equal(hash,game.Hash(),"private reads create neither episode nor objective progress");
            var limits=new ReplayLimits(16*1024*1024,10000);
            using(var recorder=game.RecordReplay(limits))
            {
                var crane=One(game,"crane").Id;
                var settings=new WorkPolicySettings(true,5,new[]{new WorkerRoleId("crane")},null,true,true,WorkAssignmentMode.All);
                Submit(c,game,new ConfigureWorkPolicyCommand{Worker=crane,Settings=settings});
                Submit(c,game,new EnqueueTransformationCommand{Capability=Process,Transformer=plant,Transformation="build_truck",Count=2,Home=depot});
                var build=Submit(c,game,new ConstructCommand{Def="tower",Priority=50,
                    Placement=global::Beep.SimEngine.Maps.TilePlacement.Arguments("colony.tiles",new TileCoord(10,16))});
                c.That(Mine(game,"site:tower").Any(e=>e.Id==build.Entity) && !Mine(game,"tower").Any(),"physical project is not a completed structure");
                c.Equal(2,Trucks(game),"manufacturing does not spawn finished machines on submission");
                var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"pending pressure/industry state saves exactly");
                Until(()=>Life(game).Snapshot.Active && Life(game).OrderId>0,"engine-generated physical emergency",twin);
                c.Equal(1L,Pressure(game).Accepted,"engine alone raises one finite-budget incident");
                c.Equal(Fix.One,Pressure(game).Spent,"accepted incident consumes authored pressure budget");
                c.Equal(Fix.Zero,Pressure(game).Available,"no hidden refill/bonus pressure");
                c.Equal(-1L,Life(game).Snapshot.ResolvedTick,"active incident is not an instant completed repair");
                c.Equal(new Code("state.stopped"),One(game,"acceptance.machine").State,"entity Update settles native active-incident state");
                twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"active response/pressure checkpoint");
                Until(()=>Phases(game).Single(p=>p.Id=="repair").Nodes.Any(n=>n.Progress.WholeTicks>0),"real response travel/isolation/repair",twin);
                var repair=Phases(game).Single(p=>p.Id=="repair");
                c.That(repair.Nodes.Any(n=>n.Worker.IsValid && n.ProductiveSteps>0),"actual role machine performs positive response work");
                c.Equal(Fix.FromInt(6),Stock(game,target),"isolation1 plus repair3 consume preparedness steel exactly once");
                var cleanup=Phases(game).Single(p=>p.Id=="cleanup");
                c.That(!cleanup.Complete && cleanup.Nodes.Any(n=>n.Blocker==new Code("acceptance.cleanup-disabled")),
                    "parallel cleanup waits on its authored player permit");
                c.That(!Phases(game).Single(p=>p.Id=="restart").Complete,"restart depends on both physical branches");
                twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"partial repair/material/tool claims checkpoint");
                var captured=Phases(game);
                long at=game.Clock.StepIndex;game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();
                Update(game,plant,observations,twin);c.Equal(at,game.Clock.StepIndex,"sole-clock pause suspends all industry/response");
                c.Equal(hash,game.Hash(),"pause changes no physical progress or pressure decision");
                twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"paused integrated state checkpoint");
                game.Clock.Resume();twin.Clock.Resume();
                Until(()=>Trucks(game)==3 && Production(game,plant).QueueCount==1,"first sequential physical machine",twin);
                c.That(Production(game,plant).Progress.WholeTicks<Production(game,plant).Required,"second machine still needs game time");
                twin=factory.Load(game.Save());
                Until(()=>Trucks(game)==4 && Production(game,plant).QueueCount==0 && Mine(game,"tower").Count()==1 &&
                    Objective(game,"detected").State==ObjectiveState.Completed,"industry and engine-owned objective dependencies",twin);
                c.That(Life(game).Snapshot.Active && game.Outcome.IsNone,"unresolved emergency prevents authored victory");
                c.Equal(ObjectiveState.Completed,Objective(game,"fleet").State,"manufacturing objective commits actual entities");
                c.Equal(ObjectiveState.Completed,Objective(game,"build").State,"construction objective commits completed native structure");
                c.That(Objective(game,"build").ActivatedTick>Objective(game,"fleet").SettledTick &&
                    Objective(game,"detected").ActivatedTick>Objective(game,"build").SettledTick,"objective dependencies activate on later updates");
                c.Equal(Fix.FromInt(6),Steel(game),"complete industry consumes50 actual steel, response4; no fabricated resources");
                c.That(observations.Travel && observations.LoadedHaul && observations.Work,"timed loaded transport and machine work witnessed");
                c.That(captured.Single(p=>p.Id=="restart").Complete==false,"retained phase views are detached");
                c.Equal(Codes.NotOwned,game.Seat(1).Preview(new SetWorkPermitCommand{Target=target,Enabled=true}),"foreign cleanup command refuses normally");
                Submit(c,game,new SetWorkPermitCommand{Target=target,Enabled=true});
                c.That(Life(game).Snapshot.Active,"permitting cleanup does not instantly resolve the incident");
                twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"ordinary cleanup command/response checkpoint");
                Until(()=>!game.Outcome.IsNone,"physical cleanup/restart and engine-owned victory",twin);
                c.Equal(new Code("acceptance.colony-secured"),game.Outcome,"only authored native objective effect ends the session");
                c.Equal(ObjectiveState.Completed,Objective(game,"recovered").State,"objective observes actual resolved episode");
                var resolved=Life(game);c.That(!resolved.Snapshot.Active && resolved.Snapshot.Episode==1 &&
                    resolved.Snapshot.ResolvedTick>resolved.Snapshot.RaisedTick,"one physical episode, no synthetic completion");
                c.That(Phases(game).All(p=>p.Complete),"all authored incident stages complete through the same native DAG");
                c.Equal(Fix.FromInt(6),Steel(game),"terminal balance unchanged by cleanup/save/outcome");
                c.Equal(Fix.FromInt(10),Stock(game,foreign,1),"response does not steal private foreign reserves");
                c.That(Life(game,1).Snapshot.Episode==0 && !Life(game,1).Snapshot.Active,"pressure targets only enrolled company");
                c.Equal(1L,Pressure(game).Accepted,"no hidden extra pressure episode");
                c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"ended integrated state restores exactly");
                c.Equal(isolatedHash,isolated.Hash(),"interleaved untouched session remains isolated");
                var replay=new ReplayVerifier().Verify(factory,recorder.Finish(),limits);
                c.That(replay.Success && replay.Game!=null && replay.Game.Hash()==game.Hash(),
                    "canonical pressure/industry/objective replay: "+replay.Failure+"/"+replay.Detail);
            }
        }
    }
}

