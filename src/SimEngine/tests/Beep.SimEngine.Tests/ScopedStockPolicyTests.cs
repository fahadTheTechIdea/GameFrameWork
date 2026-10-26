using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Thin player simulation: public commands, immutable facts and the sole native Update.
    internal static class ScopedStockPolicyTests
    {
        internal const string Case = "Scoped stock policies: actual bands, port exports, aliases and normal control actions";
        private static readonly Code A=new Code("a-stock"), Z=new Code("z-stock"), Other=new Code("other"),
            Tank=new Code("tank"), Alias=new Code("alias");
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",9000000001),
            Second=new ResourceEndpointId("proof.pools",9000000002), Third=new ResourceEndpointId("proof.pools",9000000003);
        internal static void Register() => TestRunner.Add(Case,Run);
        private static EntityId Find(SimGame game,string name) => game.Seat(0).Snapshot.Entities.Single(e=>e.Def==name).Id;
        private static ResourceId Resource(SimGame game,string name="steel")
        { if(!game.Seat(0).View.TryFindResource(name,out var id)) throw new InvalidOperationException("missing "+name); return id; }
        private static LogisticsView View(SimGame game,int seat=0) => game.Seat(seat).View.Feature<LogisticsView>();
        private static List<LogisticsDemandView> Demands(SimGame game,int seat=0)
        { var rows=new List<LogisticsDemandView>(); View(game,seat).CopyDemands(rows); return rows; }
        private static List<WorkJobView> Jobs(SimGame game)
        { var rows=new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
        private static List<LogisticsDispatchOffer> Offers(SimGame game)
        { var rows=new List<LogisticsDispatchOffer>(); View(game).CopyQueue(rows); return rows; }
        private static bool Live(WorkJobView job) => job.State==JobState.Open || job.State==JobState.Assigned || job.State==JobState.Paused;
        private static Fix Amount(SimGame game,string site,Code port)
        { var code=game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Find(game,site),port,Resource(game),out var snapshot); if(!code.IsNone) throw new InvalidOperationException("port "+code); return snapshot.Stock.AmountOf(Resource(game)); }
        private static Fix Local(SimGame game,string site)
        { var facets=new List<Facet>(); game.Seat(0).View.Inspect(Find(game,site),facets); return facets.Where(f=>f.Key==Storage.FacetStock && f.Subject=="steel").Aggregate(Fix.Zero,(sum,f)=>sum+f.Value); }
        private static void Step(SimGame game,SimGame twin=null)
        { game.Update(); twin?.Update(); if(game.Fault!=null || twin?.Fault!=null || twin!=null && game.Hash()!=twin.Hash()) throw new InvalidOperationException("policy update "+game.Tick+"/"+game.Fault+"/"+twin?.Fault); }
        private static void Until(SimGame game,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<1000 && !done();i++) Step(game,twin);
            if(!done()) throw new InvalidOperationException(stage+" at "+game.Tick+" z="+Outlet(game,Z).Enabled+"/"+View(game).PreviewHaul(Find(game,"source.store"),Find(game,"source.sink"),Resource(game),Fix.FromInt(6),sourcePort:Z,destinationPort:Alias).Status+"/"+View(game).PreviewHaul(Find(game,"source.store"),Find(game,"source.sink"),Resource(game),Fix.FromInt(6),sourcePort:Z,destinationPort:Alias).Blocker+" demands="+string.Join(";",Demands(game).Select(d=>d.Port+"/"+d.Endpoint.Identity+"/"+d.Replenishing))+
                " offers="+string.Join(";",Offers(game).Select(o=>o.Haul.Status+"/"+o.Haul.Blocker))+" jobs="+string.Join(";",Jobs(game).Select(j=>j.Id+"/"+j.State+"/"+j.ToilIndex+"/"+j.Loaded+"/"+j.Blocker)));
        }
        private static long Order(Check c,SimGame game,Command command)
        {
            ulong hash=game.Hash(); c.Equal(Code.None,game.Seat(0).Preview(command),"native preview"); c.Equal(hash,game.Hash(),"preview is pure");
            var result=TestKit.Order(game,0,command); c.That(result.Accepted,"native execution "+result.Refusal);
            if(!result.Accepted || game.Fault!=null) throw new InvalidOperationException("command "+result.Refusal+"/"+game.Fault); return result.Arg0;
        }
        private static LogisticsDemandSettings Bands(bool enabled,int desired=6,int maximum=10,int priority=70)
            => new LogisticsDemandSettings(enabled,Fix.FromInt(3),Fix.FromInt(desired),Fix.FromInt(maximum),priority,LogisticsReserveAccess.Protected);
        private static void Stock(Check c,SimGame game,Code port,LogisticsDemandChange change,ResourceEndpointId? endpoint=null)
            => Order(c,game,new ConfigureStockPolicyCommand {Store=Find(game,"source.sink"),Port=port,Endpoint=endpoint,Resource=Resource(game),Change=change});
        private static void Select(Check c,SimGame game,string site,Code port,ResourceEndpointId endpoint)
            => Order(c,game,new SelectResourcePortCommand {Entity=Find(game,site),Port=port,Endpoint = new ResourceEndpointId("proof.pools", endpoint.Identity)});
        private static void Fund(Check c,SimGame game,Code port,int amount)
        {
            int job=checked((int)Order(c,game,new HaulResourcesCommand {Source=Find(game,"source.warehouse"),Destination=Find(game,"source.store"),DestinationPort=port,
                Worker=Find(game,"haul.machine"),Resource=Resource(game),Ceiling=Fix.FromInt(amount),Priority=100}));
            Until(game,()=>!Jobs(game).Any(j=>j.Id==job && Live(j)),"physical funding");
        }
        private static OutletPolicySettings Outlet(SimGame game,Code port)
        { var refusal=View(game).ReadOutletPolicy(Find(game,"source.store"),out var policy,port); if(!refusal.IsNone) throw new InvalidOperationException("outlet "+refusal); return policy; }
        private static void Export(Check c,SimGame game,Code port,bool enabled,int reserve=4,int priority=3,string[] destinations=null)
            => Order(c,game,new ConfigureOutletPolicyCommand {Source=Find(game,"source.store"),Port=port,
                Change=OutletPolicyChange.Replace(new OutletPolicySettings(enabled,new Bill(new[]{new ResourceAmount(Resource(game),Fix.FromInt(reserve))}),priority,destinations,LogisticsReserveAccess.Protected))});
        private static SimGame Save(Check c,GameFactory factory,SimGame game)
        { var twin=factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"original policy and receipt checkpoint"); return twin; }
        private static LogisticsDemandSettings Read(SimGame game,Code port,ResourceEndpointId? endpoint=null)
        { var refusal=View(game).ReadStockPolicy(Find(game,"source.sink"),Resource(game),out var settings,port,endpoint); if(!refusal.IsNone) throw new InvalidOperationException("stock "+refusal); return settings; }
        private static AutomationSettings Rule(SimGame game,string profile,EntityId target,Code capability)
        {
            var ops=game.Seat(0).View.Feature<OperationsView>();
            if(!ops.TryGetAutomationProfile(profile,out var metadata)) throw new InvalidOperationException("missing authored rule");
            var bands=new Dictionary<string,ControlThresholdBand>(); foreach(var band in metadata.Thresholds) bands.Add(band.Key,band.Value.Initial);
            return new AutomationSettings(profile,true,new ControlBinding(target,capability),
                new Dictionary<string,ControlBinding>{{"stock",new ControlBinding(Find(game,"source.warehouse"),new Code("warehouse-stock"))}},bands,metadata.WhenTrue,metadata.WhenFalse);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"policy protocol"); c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"native migrated command/component format");
            var factory=ScopedHaulGame.Factory(scopedPolicies:true);
            var game=factory.NewGame(new NewGameRequest {Scenario="scoped-policies",Seed=961,Seats={Controller.Human,Controller.Human}});
            var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,4000)); var sink=Find(game,"source.sink"); var steel=Resource(game);
            c.That(!game.Modules.Contains("sim.tycoon"),"policies/manufacture need no business module");
            c.That(!game.Seat(0).View.Feature<InventoryView>().TryGetCapacity(sink, Code.None,out _),"named sink has no local storage");
            c.Equal(1,Demands(game).Count,"two authored aliases initialize one actual stock row");
            c.Equal(Third,Demands(game).Single().Endpoint,"authored band owns actual scoped destination");
            c.That(!Read(game,Tank).Enabled && Read(game,Alias).Desired==Fix.FromInt(6),"JSON owns explicit native bands");
            c.Equal(LogisticsPolicyCodes.NoPolicy,View(game).ReadOutletPolicy(Find(game,"source.store"),out _),"unrelated local export policy is not fabricated");
            c.Equal(Fix.FromInt(4),Outlet(game,A).Reserve.AmountOf(steel),"named reserve comes from JSON");
            c.Equal(Fix.FromInt(2),Outlet(game,Other).Reserve.AmountOf(steel),"second port has independent authored policy");
            c.Equal(0,Demands(game,1).Count,"policy rows are company-private");
            c.Equal(Codes.NotFound,View(game,1).ReadStockPolicy(sink,steel,out _,Tank),"foreign stock policy remains private");
            c.Equal(Codes.NotFound,View(game,1).ReadOutletPolicy(Find(game,"source.store"),out _,A),"foreign export policy remains private");
            ulong hash=game.Hash();
            c.Equal(StandardCodes.NoStorage,View(game).PreviewStockPolicy(sink,steel,LogisticsDemandChange.Replace(Bands(true))),"no local stock fallback");
            c.Equal(StandardCodes.NoStorage,View(game).PreviewStockPolicy(sink,steel,LogisticsDemandChange.Replace(Bands(true)),new Code("missing")),"unknown named store refuses");
            c.Equal(StandardCodes.NoStorage,View(game).PreviewStockPolicy(sink,steel,LogisticsDemandChange.Replace(Bands(true)),Tank,new ResourceEndpointId("proof.pools",99)),"unknown explicit original refuses without fault");
            c.Equal(Codes.InvalidArgument,View(game).PreviewStockPolicy(sink,steel,LogisticsDemandChange.Level(LogisticsStockLevel.Desired,Fix.Parse("4.5"),true),Tank),"fractional native band refuses");
            c.Equal(Codes.InvalidArgument,View(game).PreviewStockPolicy(sink,steel,LogisticsDemandChange.Replace(Bands(true,6,101)),Tank),"native capacity maximum refuses");
            c.Equal(Codes.InvalidArgument,View(game).PreviewStockPolicy(sink,Resource(game,"spare"),LogisticsDemandChange.Replace(new LogisticsDemandSettings(false,Fix.Zero,Fix.Zero,Fix.FromInt(95),1,LogisticsReserveAccess.Protected)),Tank),"complete actual multi-resource maxima refuse");
            c.Equal(StandardCodes.NoStorage,game.Seat(0).Preview(new ConfigureStockPolicyCommand {Store=sink,Port=Tank,Endpoint=new ResourceEndpointId("proof.pools",99),Resource=steel,Change=LogisticsDemandChange.Replace(Bands(true))}),"unknown original command agrees with management refusal");
            c.Equal(hash,game.Hash(),"refusals change neither policy nor stock");
            Fund(c,game,Z,8); Fund(c,game,Z,4); Fund(c,game,Other,8);
            c.Equal(Fix.FromInt(12),Amount(game,"source.store",Z),"source funding respects actual eight-unit cargo capacity per trip");
            c.Equal(LogisticsPolicyCodes.Destination,View(game).PreviewHaul(Find(game,"source.store"),Find(game,"source.warehouse"),steel,Fix.One,sourcePort:Z).Blocker,"JSON named destination filter blocks unrelated receiving site");
            c.Equal(Codes.InvalidArgument,View(game).PreviewOutletPolicy(Find(game,"source.store"),OutletPolicyChange.Reserve(steel,Fix.Parse("4.5"),true),Z),"named reserve requires exact native units");
            c.Equal(Fix.FromInt(30),Local(game,"source.warehouse")+Amount(game,"source.store",Z)+Amount(game,"source.store",Other),"stock was physically funded, never seeded in pools");
            Order(c,game,new SetWorkerHeldCommand {Worker=Find(game,"haul.machine"),Held=true});
            var original=Demands(game).Single();
            Stock(c,game,Tank,LogisticsDemandChange.Replace(Bands(true)));
            Stock(c,game,Alias,LogisticsDemandChange.Replace(Bands(true,6,10,72)));
            c.Equal(1,Demands(game).Count,"alias policy edits do not duplicate the demand");
            c.Equal(original.CreatedTick,Demands(game).Single().CreatedTick,"alias edit preserves original age");
            c.Equal(72,Read(game,Tank).Priority,"both aliases observe same actual band");
            Export(c,game,A,false); Export(c,game,Z,false); Export(c,game,Other,false,2,1);
            Until(game,()=>Offers(game).Count==1,"blocked stock offer");
            c.Equal(HaulStatus.PolicyBlocked,Offers(game).Single().Haul.Status,"actual source exports gate automatic stock supply");
            c.Equal(LogisticsPolicyCodes.Disabled,Offers(game).Single().Haul.Blocker,"source reports typed export refusal");
            Export(c,game,Z,true,10); // Only two spare units; enabled policy lives on z, not the earlier a alias.
            Order(c,game,new SetWorkerHeldCommand {Worker=Find(game,"haul.machine"),Held=false});
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&j.HaulDemand.IsValid),"port-specific admitted stock trip");
            var trip=Jobs(game).Single(j=>Live(j)&&j.HaulDemand.IsValid);
            c.Equal(Z,trip.SourcePort,"disabled earlier alias cannot hide an allowed later alias");
            c.Equal(Fix.FromInt(2),trip.Amount,"actual source reserve bounds physical assignment");
            c.Equal(Third,trip.DestinationEndpoint,"trip uses actual stock demand store");
            Stock(c,game,Tank,LogisticsDemandChange.Replace(Bands(false)));
            Export(c,game,Z,false,10);
            Select(c,game,"source.sink",Tank,Second);
            var twin=Save(c,factory,game);
            Until(game,()=>!Jobs(game).Any(j=>j.Id==trip.Id&&Live(j)),"edited disabled policy retains original acquired delivery",twin);
            c.Equal(Fix.FromInt(2),Amount(game,"source.sink",Alias),"old delivery reaches original store despite selection");
            c.Equal(Fix.FromInt(8),Amount(game,"source.sink",Tank),"new store received no old cargo");
            c.Equal(Fix.FromInt(6),Read(game,Tank,Third).Desired,"explicit original management survives selection");
            c.Equal(LogisticsPolicyCodes.NoStockPolicy,View(game).ReadStockPolicy(sink,steel,out _,Tank),"new selection does not silently move old band");
            Export(c,game,Z,true,4);
            Stock(c,game,Tank,LogisticsDemandChange.Replace(Bands(true)),Third);
            twin=Save(c,factory,game);
            Until(game,()=>Amount(game,"source.sink",Alias)==Fix.FromInt(6) && !Jobs(game).Any(Live),"hysteresis refills original desired level",twin);
            c.That(!Demands(game).Single().Replenishing,"reaching desired ends refill, not merely minimum");
            c.Equal(Fix.FromInt(6),Amount(game,"source.store",Z),"source retained actual exports/reserves");
            Select(c,game,"source.sink",Tank,Third);
            var haul=new HaulResourcesCommand {Source=Find(game,"source.warehouse"),Destination=sink,DestinationPort=Tank,Resource=steel,Worker=Find(game,"haul.machine"),Ceiling=Fix.FromInt(8),Priority=100};
            int direct=checked((int)Order(c,game,haul));
            c.Equal(Fix.FromInt(4),Jobs(game).Single(j=>j.Id==direct).Amount,"named receiving maximum subtracts actual stock");
            hash=game.Hash();
            c.Equal(HaulStatus.PolicyBlocked,View(game).PreviewHaul(haul.Source,haul.Destination,steel,Fix.One,destinationPort:Tank).Status,"inbound promises consume named maximum");
            c.Equal(hash,game.Hash(),"maximum preview acquires no duplicate capacity");
            twin=Save(c,factory,game); Until(game,()=>!Jobs(game).Any(j=>j.Id==direct&&Live(j)),"physical delivery to maximum",twin);
            c.Equal(Fix.FromInt(10),Amount(game,"source.sink",Tank),"exact scoped maximum reached");
            var retained=Read(game,Tank); Select(c,game,"source.sink",Tank,Second);
            Stock(c,game,Tank,LogisticsDemandChange.Replace(Bands(false)));
            c.Equal(2,Demands(game).Count,"explicit new store configuration retains original independent intent");
            c.Equal(Fix.FromInt(10),retained.Maximum,"retained immutable setting remains detached");
            var ops=game.Seat(0).View.Feature<OperationsView>(); var targets=new List<Code>();
            ops.CopyControlTargetIds(sink,"stock.desired-rule",targets);
            c.Equal(2,targets.Count,"native control actions expose both original actual stock references");
            var target=targets.Single(id=>id.Id.EndsWith(".9000000003",StringComparison.Ordinal));
            c.Equal(LogisticsPolicyCodes.NoStockPolicy,ops.PreviewAutomation("alias-forgery",Rule(game,"stock.desired-rule",sink,new Code(target.Id.Replace(".5.alias.",".4.tank.")))),"undeclared alias control identity cannot bypass canonical policy arbitration");
            c.Equal(LogisticsPolicyCodes.NoStockPolicy,ops.PreviewAutomation("length-forgery",Rule(game,"stock.desired-rule",sink,new Code(target.Id.Replace(".5.alias.",".05.alias.")))),"noncanonical encoded policy target refuses");
            Order(c,game,new ConfigureAutomationCommand {Rule="desired",Settings=Rule(game,"stock.desired-rule",sink,target)});
            twin=Save(c,factory,game); Until(game,()=>Read(game,Alias,Third).Desired==Fix.FromInt(8),"stock control applies normal exact original command next-step",twin);
            c.Equal(Fix.FromInt(6),Read(game,Tank,Second).Desired,"stock action never edits another current selection");
            Order(c,game,new RemoveAutomationCommand {Rule="desired"});
            ops.CopyControlTargetIds(Find(game,"source.store"),"stock.reserve-rule",targets);
            c.Equal(3,targets.Count,"single outlet owner exposes distinct authored ports");
            var reserveTarget=targets.Single(id=>id==LogisticsPolicyRules.OutletPolicyTarget(Z));
            Order(c,game,new ConfigureAutomationCommand {Rule="reserve",Settings=Rule(game,"stock.reserve-rule",Find(game,"source.store"),reserveTarget)});
            twin=Save(c,factory,game); Until(game,()=>Outlet(game,Z).Reserve.AmountOf(steel)==Fix.FromInt(3),"reserve action targets native named port owner",twin);
            c.Equal(Fix.FromInt(4),Outlet(game,A).Reserve.AmountOf(steel),"reserve action leaves another named intent untouched");
            Order(c,game,new RemoveAutomationCommand {Rule="reserve"});
            Stock(c,game,Alias,LogisticsDemandChange.Replace(Bands(false)),Third);
            c.Equal(Fix.FromInt(30),Local(game,"source.warehouse")+Local(game,"haul.machine")+Amount(game,"source.store",Z)+Amount(game,"source.store",Other)+Amount(game,"source.sink",Alias),"all policies and control actions preserve physical material");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,4000)).Success,"normal port-policy command/automation replay");
        }
    }
}
