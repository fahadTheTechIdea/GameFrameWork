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
    // JSON composition and public player interaction only. No stock/job/world mutation.
    internal static class TransporterRetirementTests
    {
        internal const string Case = "Transporter removal: native and bound cargo guard physical retirement through partial transfers";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Compose(bool half, bool bound)
            => Factory(half, endpoints:true,
                modules:bound ? new GameModule[] {new LogisticsRecoveryPolicyProofModule()} : null,
                declarations:bound ? "[{\"id\":\"proof.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\"}],\"documents\":[]}]" : "[]",
                documents:new Dictionary<string,string> {{"transporter-retirement.json",FrameworkProofContent.Read("transporter-retirement.json")}},
                startup:new[] {"carrier.start"}, authorWorld:world => {
                    var machine=world.Get("entities")[2].Get("components");
                    machine.Get("point_presence").Set("position",JsonValue.Number("4"));
                    machine.Set("point_dock",Json.Parse("{\"work_roles\":[\"extension.builder\"],\"resources\":[\"supply\"],\"slots\":[{\"id\":\"carrier.service.a\",\"kind\":\"service\",\"offset\":1},{\"id\":\"carrier.service.b\",\"kind\":\"service\",\"offset\":2},{\"id\":\"carrier.exit\",\"kind\":\"departure\",\"offset\":3},{\"id\":\"carrier.wait\",\"kind\":\"waiting\",\"offset\":4}]}"));
                    if (bound) {
                        machine.Remove("transporter");
                        machine.Set("proof_bound_cargo",Json.Parse("{\"cargo\":\"cargo.container\",\"cargo_replacements\":[],\"handling\":\"points.handling\",\"fault\":\"none\"}"));
                    }
                });
        internal static long Activity(Check c, SimGame g, string activity, EntityId target, params string[] nodes)
        {
            int actor=Entity(g,"lifecycle.b").Value;
            var bindings=JsonValue.NewArray();
            foreach (string node in nodes) bindings.Add(Json.Parse("{\"node\":\""+node+"\",\"role\":\"extension.builder\",\"worker\":"+actor+"}"));
            var args=JsonValue.NewObject(); args.Set("target",JsonValue.Number(target.Value.ToString())); args.Set("workers",bindings);
            return Submit(c,g,new SubmitPlanCommand {Intent=new PlanIntent("activity:"+activity,args,"carrier.retirement",100,false)}).Arg0;
        }
        private static CargoDispositionSnapshot Cargo(Check c, SimGame g, EntityId worker)
        {
            ulong before=g.Hash(); var view=g.Seat(0).View.Feature<LogisticsView>();
            c.Equal(Code.None,view.ReadCargoDisposition(worker,out var value),"actual immutable cargo binding facts");
            c.Equal(before,g.Hash(),"cargo binding diagnostics do not change provider state"); return value;
        }
        private static void Scenario(Check c, bool half, bool bound, bool delivery)
        {
            var f=Compose(half,bound); var g=ConstructionPlanTests.NewPoints(f); var idle=ConstructionPlanTests.NewPoints(f); ulong isolated=idle.Hash();
            var limits=new ReplayLimits(4*1024*1024,6000); using var recorder=g.RecordReplay(limits);
            EntityId worker=Entity(g,"machine"), store=Entity(g,bound?"cargo.container":"machine");
            Policy(c,g,"machine",true); Policy(c,g,"lifecycle.b",true,true);
            Submit(c,g,new SetHomeCommand {Worker=Entity(g,"lifecycle.b"),Home=Entity(g,"lifecycle.home")});
            int haul=checked((int)Submit(c,g,new HaulResourcesCommand {Source=Entity(g,"source"),Destination=Entity(g,"destination"),
                Worker=worker,Resource=Supply(g),Ceiling=Fix.FromInt(8),Priority=100}).Arg0);
            var twin=Save(c,f,g);
            Reach(g,twin,()=>Job(g,haul).Loaded.IsPositive && Job(g,haul).Loaded<Job(g,haul).Amount && Job(g,haul).ToilIndex==(delivery?3:1),"partial actual cargo transfer");
            Policy(c,g,"machine",false); var prefix=Job(g,haul); var retained=Cargo(c,g,worker);
            Fix quantity=Facts(g,store).Stock, position=At(g,worker), cargoPosition=At(g,store);
            c.That(quantity.IsPositive && retained.Endpoint==ResourceEndpointId.ForEntity(store),"cargo facts identify actual native/separate inventory owner");
            if(bound) c.Equal(Fix.Zero,Facts(g,worker).Stock,"remote cargo is not copied into worker inventory");
            long blocked=Activity(c,g,"carrier.retire",worker,"retire"); twin=Save(c,f,g);
            Reach(g,twin,()=>Plan(g,blocked).Order>0 && Work(g).PreviewOrderEffect(Plan(g,blocked).Order)==ResourceTransactions.ActivePromises,"loaded cargo claim refusal");
            c.That(Exists(g,worker) && Facts(g,store).Stock==quantity && At(g,worker)==position,"guarded loaded worker remains physically present with actual cargo");
            c.Equal(prefix.HandlingProgress,Job(g,haul).HandlingProgress,"wait preserves committed handling prefix");
            c.Equal(Fix.FromInt(20),Total(g),"retirement never loses promised cargo");
            twin=Save(c,f,g); g.Clock.Pause(); twin.Clock.Pause(); ulong paused=g.Hash();
            c.Equal(0,g.Update(1000000),"one paused clock freezes guarded retirement"); c.Equal(0,twin.Update(1000000),"saved sole clock freezes");
            c.That(g.Hash()==paused && g.Hash()==twin.Hash(),"paused active-cargo checkpoint identity"); g.Clock.Resume(); twin.Clock.Resume();
            Submit(c,g,new SetWorkerHeldCommand {Worker=worker,Held=true}); twin=Save(c,f,g);
            c.That(!Jobs(g).Any(x=>x.Id==haul) && Facts(g,store).OutboundPromised.IsZero,"original haul cancellation releases real cargo promises");
            Reach(g,twin,()=>Work(g).PreviewOrderEffect(Plan(g,blocked).Order)==ResourceTransactions.UndisposedStock,"retirement service clears its own physical dock before final stock refusal");
            c.Equal(ResourceTransactions.UndisposedStock,Work(g).PreviewOrderEffect(Plan(g,blocked).Order),"released remote/native stock still prevents removal");
            c.That(Facts(g,store).Stock==quantity && At(g,worker)==position,"interruption cannot silently dispose or move cargo");
            Submit(c,g,new CancelPlanCommand {Selection=new PlanCancellationSelection(false,new[]{blocked})});
            // Remote inventory is recovered at ITS authored placement, not invented vehicle geometry.
            long recovery=Activity(c,g,bound?"carrier.pack-container":"carrier.pack-retire",bound?store:worker,bound?new[]{"pack"}:new[]{"pack","retire"});
            twin=Save(c,f,g); Reach(g,twin,()=>Plan(g,recovery).Order>0 && Node(g,Plan(g,recovery).Order,"pack").Progress.WholeTicks>0,"physical cargo packing starts");
            c.That(Facts(g,store).Stock==quantity && Exists(g,worker),"timed packing creates no instant fallback");
            twin=Save(c,f,g); Reach(g,twin,()=>Plan(g,recovery).State==PlanState.Done,"actual inventory recovery finishes");
            if(bound) {
                c.That(Exists(g,worker) && Facts(g,store).Stock.IsZero && At(g,worker)==position,"separate recovery empties real cargo without removing or moving worker");
                long final=Activity(c,g,"carrier.retire",worker,"retire"); twin=Save(c,f,g);
                Reach(g,twin,()=>Plan(g,final).State==PlanState.Done,"empty bound cargo permits later physical retirement");
            }
            c.That(!Exists(g,worker),"actual loaded transporter retires only after explicit recovery");
            c.Equal(Fix.FromInt(20),Total(g),"partial transfer plus packed cargo conserves exactly twenty");
            Fix packed=Fix.Zero; int containers=0;
            foreach(var entity in g.Seat(0).Snapshot.Entities) {
                if(entity.Def=="source" || entity.Def=="destination" || entity.Def=="retire.receiver" || entity.Def=="cargo.container") continue;
                if(g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id,Supply(g),out var facts).IsNone && facts.Stock.IsPositive) {
                    packed+=facts.Stock; containers++; c.Equal(cargoPosition,((PointSnapshot)entity.Spatial).Position,"packed stock uses actual inventory placement");
                }
            }
            c.That(containers>0 && packed==quantity,"all residual cargo packed once at actual inventory owner");
            c.That(prefix.Loaded==quantity && prefix.HandlingProgress.WholeTicks>0 && retained.HasResidualCargo,"retained historical cargo/handling facts stay detached");
            var hidden=new List<PlanView>(); Work(g,1).CopyPlans(hidden); c.Equal(0,hidden.Count,"retirement intent remains company private");
            c.Equal(bound?Codes.NotOwned:Codes.NotFound,g.Seat(1).View.Feature<LogisticsView>().ReadDiagnostics(store,Supply(g),out _),"actual separate/removed native inventory facts do not leak");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"terminal packed-cargo retirement durable");
            c.Equal(isolated,idle.Hash(),"retirement and bound cargo isolate idle interleaved session");
            c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"public partial-transfer/cargo-retirement stream replays identically");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current engine protocol identity");
            foreach(bool half in new[]{false,true}) foreach(bool bound in new[]{false,true}) foreach(bool delivery in new[]{false,true}) Scenario(c,half,bound,delivery);
        }
    }
}
