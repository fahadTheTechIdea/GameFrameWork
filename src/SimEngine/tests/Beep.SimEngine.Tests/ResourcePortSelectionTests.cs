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
    // Actual JSON/master and public seat APIs only. All routing, permission and physical recovery rules are engine-owned.
    internal static class ResourcePortSelectionTests
    {
        internal const string Case="Resource port selection: engine-owned controls preserve live physical claims across native and point restoration";
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",9000000001),
            Second=new ResourceEndpointId("proof.pools",9000000002);
        private static readonly Code Alias=new Code("alias"),Far=new Code("return");
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool point,bool half,string fault=null)=>ScopedHaulGame.Factory(restoredDockDelivery:true,pointDockRecovery:point,
            editCommon:root=>{if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));},
            editBindings:fault==null?null:root=>root.Get("entities")[0].Get("components").Set("selection_fault_port",
                Json.Parse("{\"id\":\"selection.fail\",\"source\":\"alias\",\"profile\":\"selection."+fault+"\"}")),
            selectionFaultDefinitions:fault==null?null:FrameworkProofContent.Read("resource-port-selection-faults.json"));
        private static SimGame New(GameFactory f,bool point)=>f.NewGame(new NewGameRequest {
            Scenario=point?"scoped-point-dock-restoration":"scoped-dock-restoration",Seed=1294,Seats={Controller.Human,Controller.Human}});
        private static EntityId Find(SimGame g,string name,bool point)=>Entity(g,point?"point."+name:name);
        private static List<WorkJobView> Jobs(SimGame g)=>PermanentRemovalTests.Jobs(g);
        private static bool Live(WorkJobView j)=>j.State==JobState.Open||j.State==JobState.Assigned||j.State==JobState.Paused;
        private static WorkJobView Job(SimGame g,int id)=>Jobs(g).Single(j=>j.Id==id);
        private static ResourceId Steel(SimGame g)
        { if(!g.Seat(0).View.TryFindResource("steel",out var value))throw new InvalidOperationException("authored steel missing");return value; }
        private static LogisticsOperationalSnapshot Facts(SimGame g,string name,bool point,Code port=default,ResourceEndpointId? original=null)
        {
            Code refusal=g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(Find(g,name,point),Steel(g),out var value,port,original);
            if(!refusal.IsNone)throw new InvalidOperationException("selection facts "+name+"/"+refusal);return value;
        }
        private static LogisticsOperationalSnapshot Original(SimGame g,bool point)=>Facts(g,"recovery.far",point,Far,First);
        private static Fix Cargo(SimGame g,bool point)=>Facts(g,"dock.machine",point).Stock;
        private static List<ResourcePortChoiceSnapshot> Choices(SimGame g,bool point,int seat=0)
        {
            var values=new List<ResourcePortChoiceSnapshot>();
            Code refusal=g.Seat(seat).View.Feature<InventoryView>().ReadResourcePortChoices(Find(g,"restore.target",point),Alias,values);
            if(seat==0&&!refusal.IsNone)throw new InvalidOperationException("selection choices "+refusal);
            return values;
        }
        private static IReadOnlyList<ResourceReservation> Records(SimGame game, EntityId entity, Code port, ResourceEndpointId? original = null)
        {
            if (!game.Seat(0).View.Feature<InventoryView>().TryGetReservations(entity, port, out var records, original))
                throw new InvalidOperationException("actual named commitment facts unavailable " + entity + "/" + port + "/" + original);
            return records;
        }
        private static void Reach(SimGame g,SimGame twin,Func<bool> ready,string stage)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<1000&&watch.ElapsedMilliseconds<20000&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" exceeded1000Updates/20s clock="+g.Clock.StepIndex+
                " jobs="+string.Join(";",Jobs(g).Select(j=>j.Id+"/"+j.State+"/"+j.ToilIndex+"/"+j.Loaded+"/"+j.Blocker)));
        }
        private static SelectResourcePortCommand Route(SimGame g,bool point,ResourceEndpointId endpoint)=>new SelectResourcePortCommand {
            Entity=Find(g,"restore.target",point),Port=Alias,Endpoint=endpoint};
        private static void Held(Check c,SimGame g,bool point,bool held)=>PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand {
            Worker=Find(g,"dock.machine",point),Held=held});
        private static void Band(Check c,SimGame g,bool point,string site,Code port,int amount,int priority)=>PermanentRemovalTests.Submit(c,g,new ConfigureStockPolicyCommand {
            Store=Find(g,site,point),Port=port,Resource=Steel(g),
            Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,Fix.FromInt(amount),Fix.FromInt(amount),Fix.FromInt(amount),priority,LogisticsReserveAccess.Protected))});
        private static void Physical(Check c,bool point,bool half)
        {
            var f=Factory(point,half);var g=New(f,point);var isolated=New(f,point);ulong untouched=isolated.Hash();
            var limits=new ReplayLimits(4*1024*1024,5000);
            using(var record=g.RecordReplay(limits)) {
                var rows=Choices(g,point);ulong pure=g.Hash();
                var inventory=g.Seat(0).View.Feature<InventoryView>(); var target=Find(g,"restore.target",point);
                c.That(!inventory.TryGetCapacity(target,Code.None,out _) && !inventory.TryGetReservations(target,Code.None,out _),
                    "named-only entity has no invented local stock or commitment book");
                c.That(inventory.TryGetCapacity(target,Alias,out var namedCapacity,First)&&namedCapacity.Fits,
                    "named capacity comes from actual selected service-owned store");
                c.Equal(0,Records(g,target,Alias,First).Count,"initial named book is actually empty");
                c.That(!inventory.TryGetCapacity(target,new Code("missing"),out _) &&
                    !inventory.TryGetReservations(target,new Code("missing"),out var missing) && missing.Count==0,
                    "missing named endpoint never falls back to another store");
                c.That(rows.Count==2&&rows[0].Endpoint==First&&rows[0].Selected&&rows[1].Endpoint==Second&&!rows[1].Selected&&rows.All(x=>x.Blocker.IsNone),"JSON-declared choices sorted and actual selected reference exposed");
                c.Equal(0,Choices(g,point,1).Count,"foreign seat receives no private choice list");
                c.Equal(pure,g.Hash(),"choice diagnostics pure");
                var invalid=Route(g,point,new ResourceEndpointId("proof.pools",9000000003));
                c.Equal(Codes.InvalidArgument,g.Seat(0).Preview(invalid),"undeclared endpoint refused before selection");
                c.Equal(Codes.NotOwned,g.Seat(1).Preview(Route(g,point,Second)),"foreign site cannot be routed");
                c.Equal(pure,g.Hash(),"refused previews are pure");
                int sequence=g.Seat(0).Submit(Route(g,point,Second));var twin=f.Load(g.Save());
                c.That(Choices(g,point).Single(x=>x.Selected).Endpoint==First,"pending command is not immediate route mutation");
                Step(g,twin);long cursor=0;var results=new List<CommandResult>();g.Seat(0).ReadResults(ref cursor,results);
                c.That(results.Single(x=>x.Sequence==sequence).Accepted,"engine-owned command survives pending inbox save");
                c.Equal(Second,Choices(g,point).Single(x=>x.Selected).Endpoint,"public command selects actual declared endpoint");
                c.Equal(First,rows.Single(x=>x.Selected).Endpoint,"retained choice facts detached");
                PermanentRemovalTests.Submit(c,g,Route(g,point,First));
                PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=Find(g,"dock.queue",point),Held=true});
                int funding=checked((int)PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand {
                    Source=Find(g,"source.warehouse",point),Destination=Find(g,"source.sink",point),
                    SourcePort=new Code("stock"),DestinationPort=new Code("tank"),Resource=Steel(g),Ceiling=Fix.FromInt(8),
                    Worker=Find(g,"dock.machine",point),Priority=100}).Arg0);
                twin=f.Load(g.Save());Reach(g,twin,()=>Job(g,funding).Loaded==Fix.FromInt(8)&&Job(g,funding).ToilIndex==2,"actual complete physical pickup");
                Held(c,g,point,true);c.Equal(Fix.FromInt(8),Cargo(g,point),"interrupted pickup retains real cargo");
                Band(c,g,point,"restore.target",Alias,8,200);Band(c,g,point,"recovery.far",Far,0,100);Held(c,g,point,false);
                twin=f.Load(g.Save());Reach(g,twin,()=>Jobs(g).Any(j=>Live(j)&&j.Worker==Find(g,"dock.machine",point)&&j.DestinationEndpoint==First),"ordinary original destination claims");
                int job=Jobs(g).Single(j=>Live(j)&&j.Worker==Find(g,"dock.machine",point)).Id;
                if(point)Reach(g,twin,()=>Job(g,job).Loaded.IsPositive&&Job(g,job).Loaded<Fix.FromInt(8),"real partial point unload");
                Fix prefix=Original(g,point).Stock,remaining=Cargo(g,point);var bound=Job(g,job);
                var held=Records(g,target,Alias,First);
                c.That(held.Count>0&&held.All(x=>x.Token.Endpoint==First),"actual inbound receipts identify original scoped endpoint");
                c.Equal(remaining,held.Where(x=>x.Resource==Steel(g)&&x.Direction==ReservationDirection.Inbound).Aggregate(Fix.Zero,(sum,x)=>sum+x.Amount),
                    "original inbound claim exactly protects still-carried quantity");
                var cargo=Records(g,Find(g,"dock.machine",point),Code.None);
                c.Equal(remaining,cargo.Where(x=>x.Resource==Steel(g)&&x.Direction==ReservationDirection.Outbound).Aggregate(Fix.Zero,(sum,x)=>sum+x.Amount),
                    "same physical cargo remains protected by actual outbound receipt");
                c.Throws<NotSupportedException>(()=>((IList<ResourceReservation>)held).Clear(),"retained named receipt collection is immutable");
                c.That(inventory.TryGetCapacity(target,Alias,out var heldCapacity,First)&&heldCapacity.Fits,
                    "actual provider admits its promised store without invented usage dimensions");
                c.Throws<NotSupportedException>(()=>((IList<CapacityUsage>)heldCapacity.Usage).Clear(),"named capacity usage facts are immutable even when provider emits no rows");
                var foreign=g.Seat(1).View.Feature<InventoryView>();
                c.That(!foreign.TryGetReservations(target,Alias,out var privateRecords,First)&&privateRecords.Count==0&&
                    !foreign.TryGetCapacity(target,Alias,out var privateCapacity,First)&&privateCapacity.Usage.Count==0,
                    "foreign seat receives no scoped commitment or capacity prefix");
                PermanentRemovalTests.Submit(c,g,Route(g,point,Second));
                twin=f.Load(g.Save());Reach(g,twin,()=>Job(g,job).State==JobState.Paused,"physical original-binding wait");
                c.Equal(remaining,Cargo(g,point),"routing never transfers cargo to another store");
                c.Equal(prefix,Original(g,point).Stock,"routing never repeats or rewinds delivered prefix");
                c.Equal(Fix.Zero,Facts(g,"restore.target",point,Alias).Stock,"unrelated current store inherits no cargo");
                c.Equal(Fix.Zero,Facts(g,"restore.target",point,Alias).InboundPromised,"unrelated current store inherits no receipt");
                c.Equal(remaining,Original(g,point).InboundPromised,"original actual residual receipt retained");
                c.That(!inventory.TryGetReservations(target,Alias,out var unavailable,First)&&unavailable.Count==0&&
                    !inventory.TryGetCapacity(target,Alias,out var unavailableCapacity,First)&&unavailableCapacity.Usage.Count==0,
                    "unavailable explicit original refuses without substituting current selected endpoint");
                c.Equal(0,Records(g,target,Alias,Second).Count,"current unrelated endpoint inherits no original receipts");
                c.That(held.SequenceEqual(Records(g,Find(g,"recovery.far",point),Far,First)),"another authorized route sees the SAME retained native receipt values");
                c.That(held.SequenceEqual(Records(twin,Find(twin,"recovery.far",point),Far,First)),"saved original tokens owners resources directions and residual quantities are exact");
                c.That(inventory.TryGetCapacity(Find(g,"recovery.far",point),Far,out var originalCapacity,First)&&
                    originalCapacity.Status==heldCapacity.Status&&originalCapacity.Usage.SequenceEqual(heldCapacity.Usage),
                    "retained capacity facts match unchanged original provider after selection");
                var waiting=Job(g,job);twin=f.Load(g.Save());for(int i=0;i<3;i++)Step(g,twin);
                c.Equal(waiting.HandlingProgress,Job(g,job).HandlingProgress,"saved unavailable wait earns no handling");
                PermanentRemovalTests.Submit(c,g,Route(g,point,First));twin=f.Load(g.Save());
                c.Equal(job,Jobs(g).Single(j=>Live(j)&&j.Worker==Find(g,"dock.machine",point)).Id,"restoration reuses original physical job");
                Reach(g,twin,()=>Cargo(g,point).IsZero&&!Jobs(g).Any(Live),"actual resumed handling and departure");
                c.Equal(Fix.FromInt(8),Original(g,point).Stock,"eight units delivered physically exactly once");
                c.Equal(Fix.Zero,Original(g,point).InboundPromised,"no original promise leak");
                c.Equal(0,Records(g,target,Alias,First).Count,"physical completion releases every actual named claim");
                c.That(held.Count>0,"retained immutable receipt facts stay historical after native release");
                c.Equal(Fix.Zero,Facts(g,"recovery.far",point,Far,Second).Stock,"explicit retained alias confirms unrelated route remains empty");
                c.Equal(First,Choices(g,point).Single(x=>x.Selected).Endpoint,"same engine selection durable");
                c.Equal(bound.DestinationEndpoint,First,"retained job facts detached");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"final route and original books save/load");
                c.Equal(untouched,isolated.Hash(),"interleaved selection sessions isolated");
                c.That(new ReplayVerifier().Verify(f,record.Finish(),limits).Success,"normal engine command ownership and physical replay");
            }
        }
        private static void Negative(Check c,string mode)
        {
            var f=Factory(false,false,mode);
            if(mode=="duplicate"||mode=="scope"||mode=="initial-missing"||mode=="initial-undeclared")
            {c.Throws<ContentException>(()=>New(f,false),"all-loaded malformed choices or initial endpoint "+mode+" refuse composition");return;}
            var g=New(f,false);var command=Route(g,false,Second);command.Port=new Code("selection.fail");
            ulong before=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(command),"faulting select callback is not invoked by pure admission");c.Equal(before,g.Hash(),"negative preview pure");
            g.Seat(0).Submit(command);
            try{g.Update();}catch(Exception){}
            c.That(g.Fault!=null,"illegal "+mode+" callback faults instead of healthy selection");
            if(mode=="throw")c.That(g.Fault?.Message.Contains("authored selection callback failed after prefix")==true,"first callback failure preserved");
            c.Throws<InvalidOperationException>(()=>g.Save(),"faulted selection prefix cannot save healthy");
            IReadOnlyList<ResourceReservation> failed=System.Array.Empty<ResourceReservation>();
            c.Throws<InvalidOperationException>(()=>g.Seat(0).View.Feature<InventoryView>().TryGetReservations(Find(g,"restore.target",false),Alias,out failed),"faulted named receipt query refuses before resolution");
            c.Equal(0,failed.Count,"faulted named receipt query exposes no facts");
        }
        private static void NeutralBook(Check c)
        {
            var f=ScopedHaulGame.Factory(restoredDockDelivery:true,editPools:root=>{
                var pools=root.Get("resource_pools");for(int i=0;i<pools.Count;i++)
                    if(pools[i].Get("id").Text=="source.first")
                        pools[i].Set("owner",JsonValue.Number(PlayerId.None.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            });
            var g=New(f,false);var target=Find(g,"restore.target",false);var resource=Steel(g);
            var inventory=g.Seat(0).View.Feature<InventoryView>();
            c.Equal(Code.None,inventory.ReadResourcePort(target,Alias,resource,out var visible),"public neutral stock remains observable through actual owned port");
            c.That(visible.Ready&&visible.Endpoint==First,"neutral binding really resolves, not an unavailable substitute");
            PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand {Source=Find(g,"source.warehouse",false),Destination=target,
                SourcePort=new Code("stock"),DestinationPort=Alias,Resource=resource,Ceiling=Fix.FromInt(8),Worker=Find(g,"dock.machine",false),Priority=100});
            ulong before=g.Hash();long at=g.Clock.StepIndex;
            c.That(!inventory.TryGetCapacity(target,Alias,out var capacity,First)&&capacity.Usage.Count==0,
                "owned proxy does not grant private neutral capacity facts");
            c.That(!inventory.TryGetReservations(target,Alias,out var receipts,First)&&receipts.Count==0,
                "owned proxy does not grant private shared reservation identities");
            var observer=g.Observer.Feature<InventoryView>();
            c.That(observer.TryGetCapacity(target,Alias,out _,First)&&observer.TryGetReservations(target,Alias,out var actual,First)&&
                actual.Any(x=>x.Token.Endpoint==First&&x.Direction==ReservationDirection.Inbound&&x.Amount.IsPositive),
                "explicit observer proves actual public-store shipment receipt exists behind private query refusal");
            c.That(g.Hash()==before&&g.Clock.StepIndex==at,"public/private neutral queries do not create claims or drive time");
        }
        private static void SharedReadFault(Check c, ClaimFaultMode mode)
        {
            var factory=Factory(false,false); var game=New(factory,false);
            var target=Find(game,"restore.target",false);
            PermanentRemovalTests.Submit(c,game,new HaulResourcesCommand {
                Source=Find(game,"source.warehouse",false),Destination=target,
                SourcePort=new Code("stock"),DestinationPort=Alias,Resource=Steel(game),
                Ceiling=Fix.FromInt(8),Worker=Find(game,"dock.machine",false),Priority=100 });
            Reach(game,null,()=>!Jobs(game).Any(Live),"shared-read negative physical stock preparation");
            c.Equal(Fix.FromInt(8),Original(game,false).Stock,"negative probe uses real delivered sibling stock");
            PermanentRemovalTests.Submit(c,game,Route(game,false,Second));
            var view=game.Seat(0).View.Feature<InventoryView>();
            var history=Records(game,target,Alias,Second);
            c.That(view.TryGetCapacity(target,Alias,out var retained,Second),"healthy shared capacity before fault");
            var twin=factory.Load(game.Save()); ulong twinHash=twin.Hash();
            var pools=game.Seat(0).View.Feature<EndpointPoolDiagnostics>();
            pools.Arm(Second.Identity,First.Identity,mode);
            CapacityCheck capacity=default; IReadOnlyList<ResourceReservation> receipts=Array.Empty<ResourceReservation>();
            bool threw=false;
            try {
                if(mode==ClaimFaultMode.CapacityReadPeer)view.TryGetCapacity(target,Alias,out capacity,Second);
                else view.TryGetReservations(target,Alias,out receipts,Second);
            } catch(InvalidOperationException) { threw=true; }
            c.That(threw&&game.Fault!=null,"shared "+mode+" read must fault on actual sibling mutation");
            c.That(capacity.Usage.Count==0&&receipts.Count==0,"failed shared read publishes no capacity or receipt prefix");
            c.Equal(Fix.FromInt(7),pools.Stock(First.Identity).AmountOf(Steel(twin)),"negative callback really mutated the sibling, not selected metadata");
            c.Equal(0,history.Count,"retained receipt snapshot remains historical");
            c.That(retained.Fits,"retained healthy capacity remains usable after unrelated failure");
            c.Throws<InvalidOperationException>(()=>view.TryGetCapacity(target,Alias,out _,Second),"later reads refuse before re-invoking faulty callback");
            c.Throws<InvalidOperationException>(()=>game.Save(),"mutated shared read cannot save healthy");
            c.Equal(twinHash,twin.Hash(),"failed owner observation does not alter restored session");
            twin.Update(); c.That(twin.Fault==null,"restored independent session still advances normally");
        }
        private static void Run(Check c)
        {
            foreach(bool missing in new[]{false,true})
                c.Throws<ContentException>(()=>OutputPortGame.Factory(edit:root=>{
                    var port=root.Get("entities")[0].Get("components").Get("pool_port")[0];
                    if(missing)port.Remove("endpoint_scope");
                    else port.Set("endpoint_scope",JsonValue.Of("colony.pools"));
                }).NewGame(new NewGameRequest {Scenario="ports",Seed=1294,Seats={Controller.Human,Controller.Human}}),
                    "actual named port requires an authored selected provider scope/"+(missing?"missing":"unselected"));
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current engine selection identity");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current authored protocol, no duplicate selection or stock payload");
            c.That(typeof(InventoryView).GetMethod("TryGetCapacity",new[]{typeof(EntityId),typeof(CapacityCheck).MakeByRefType()})==null&&
                typeof(InventoryView).GetMethod("TryGetReservations",new[]{typeof(EntityId),typeof(IReadOnlyList<ResourceReservation>).MakeByRefType()})==null,
                "explicit port query replaces old signatures without compatibility overloads");
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,point,half);
            NeutralBook(c);
            SharedReadFault(c,ClaimFaultMode.CapacityReadPeer);
            SharedReadFault(c,ClaimFaultMode.ReservationReadPeer);
            foreach(string mode in new[]{"duplicate","scope","initial-missing","initial-undeclared","reserve","clock","throw"})Negative(c,mode);
        }
    }
}

