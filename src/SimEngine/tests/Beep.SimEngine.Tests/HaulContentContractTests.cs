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

namespace Beep.SimEngine.Tests
{
    internal static class HaulContentContractTests
    {
        internal const string Case="Haul content contracts: independent JSON batching and trip rules retain physical fuel and cargo";
        private static readonly ReplayLimits Limits=new ReplayLimits(4*1024*1024,3000);
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(InitializeProbe initialize,out HaulContractModule proof,Action<JsonValue> edit=null,Action<JsonValue> poweredEdit=null)
        {
            proof=new HaulContractModule(initialize);var root=Json.Parse(PointGame.Definitions);root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(HaulContractModule.ModuleId));
            root.Get("entity_setups")[2].Set("company",JsonValue.Number("1"));StartupContentAuthoring.Position(root,"machine",JsonValue.Number("60"));
            var powered=Json.Parse(FrameworkProofContent.Read("powered-hauling.json"));var parts=powered.Get("entities")[1].Get("components");
            parts.Get("storage").Set("capacity_policy",JsonValue.Of("fixture.count.40"));parts.Get("storage").Get("initial_stock").Set("fuel",JsonValue.Number("12"));
            parts.Set("haul_load_policy",Json.Parse("{\"profile\":\"contract.load\",\"states\":{\"disabled\":\"blocked\"}}"));parts.Set("haul_trip_policy",Json.Parse("{\"profile\":\"contract.trip\"}"));
            powered.Get("logistics_policies")[0].Get("stock").Get("fuel").Set("minimum",JsonValue.Number("0"));
            powered.Get("logistics_policies")[0].Get("stock").Get("fuel").Set("desired",JsonValue.Number("12"));powered.Get("logistics_policies")[0].Get("stock").Get("fuel").Set("maximum",JsonValue.Number("12"));
            powered.Get("logistics_policies")[0].Get("outlet").Get("reserve").Set("fuel",JsonValue.Number("12"));
            powered.Get("work_policies")[1].Set("enabled",JsonValue.Of(false));powered.Get("entity_setups")[2].Set("company",JsonValue.Number("1"));StartupContentAuthoring.Position(powered,"reserve.courier",JsonValue.Number("40"));poweredEdit?.Invoke(powered);
            var profiles=Json.Parse(FrameworkProofContent.Read("haul-contracts.json"));edit?.Invoke(profiles);
            return PointGame.Factory(root.ToString(),retryProfiles:FrameworkProofContent.Read("work-retry.json"),
                extraModules:new[]{proof},extraDeclarations:"[{\"id\":\"proof.haul-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"haul-contracts.json\"]}]",
                extraDocuments:new Dictionary<string,string>{{"haul-contracts.json",profiles.ToString()}},additionalDocuments:new Dictionary<string,string>{{"powered-input.json",powered.ToString()}},startupProfiles:new[]{"powered-hauling.start"});
        }
        private static SimGame New(GameFactory factory)
        {var request=new NewGameRequest{Scenario="points",Seed=809};request.Seats.Add(Controller.Human);request.Seats.Add(Controller.Human);return factory.NewGame(request);}
        private static EntityId Entity(SimGame game,string id)=>game.Seat(0).Snapshot.Entities.Single(value=>value.Def==id&&value.Owner==new PlayerId(0)).Id;
        private static LogisticsView View(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<LogisticsView>();
        private static Fix Amount(SimGame game,EntityId entity,string resource)
        {var values=new List<Facet>();game.Seat(0).View.Inspect(entity,values);return values.Where(value=>value.Key==Storage.FacetStock&&value.Subject==resource).Aggregate(Fix.Zero,(n,value)=>n+value.Value);}
        private static void Step(SimGame game,SimGame twin=null)
        {game.Update();twin?.Update();if(game.Fault!=null||twin?.Fault!=null||twin!=null&&game.Hash()!=twin.Hash())throw new InvalidOperationException("haul step="+game.Clock.StepIndex+" fault="+game.Fault+"/"+twin?.Fault);}
        private static void Until(SimGame game,SimGame twin,Func<bool> done,string stage)
        {for(int i=0;i<700&&!done();i++)Step(game,twin);if(!done()){var jobs=new List<WorkJobView>();game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);throw new InvalidOperationException(stage+" step="+game.Clock.StepIndex+" jobs="+string.Join(";",jobs.Select(value=>value.Id+"/"+value.State+"/"+value.Blocker)));}}
        private static void Order(Check c,SimGame game,Command command)
        {ulong hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary preview");c.Equal(hash,game.Hash(),"preview pure");c.That(TestKit.Order(game,0,command).Accepted,"normal player command");}
        private static void Run(Check c)
        {
            var initialize=new InitializeProbe();var factory=Factory(initialize,out var proof);var game=New(factory);var truck=Entity(game,"reserve.truck");var source=Entity(game,"source");var destination=Entity(game,"destination");
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current contract protocol");c.Equal(154,EngineInfo.SaveFormat,"unchanged saved owners");c.That(proof.IndependentProfiles,"actual owned profiles without native bases");c.Equal(1,initialize.Initializations,"valid Initialize once");
            c.That(game.Seat(0).View.TryFindResource("supply",out var supply),"actual resource identity");c.That(game.Seat(0).View.TryFindResource("fuel",out var fuel),"actual fuel identity");
            ulong hash=game.Hash();c.Equal(Code.None,View(game).ReadHaulLoadPolicy(truck,out var load),"direct load initial settings");c.Equal(Code.None,View(game).ReadHaulTripPolicy(truck,out var trip),"direct trip initial settings");
            c.That(load.Profile=="contract.load"&&load.MinimumLoad.AmountOf(supply)==Fix.FromInt(8)&&load.MaximumWaitTicks==5&&trip.Profile=="contract.trip"&&trip.PreserveOutletReserve,"native declared minimum/time/reserve values");
            c.Equal(Codes.NotFound,View(game,1).ReadHaulTripPolicy(truck,out _),"trip settings private");c.Equal(Code.None,View(game).ReadHaulTripServiceSources(truck,out var sources),"actual service metadata");c.Equal(0,sources.Count,"explicit empty service set has no fabricated service");
            c.Throws<NotSupportedException>(()=>((IList<Code>)sources).Add(new Code("fake")),"service facts immutable");
            c.Equal(HaulTripPolicyCodes.Invalid,View(game).PreviewHaulTripPolicy(truck,new HaulTripSettings("contract.load",true,false)),"wrong contract kind refuses");
            c.Equal(HaulLoadPolicyCodes.Invalid,View(game).PreviewHaulLoadPolicy(truck,new HaulLoadSettings("contract.trip",true,load.MinimumLoad,GameDuration.Minutes(5),false)),"wrong load kind refuses");
            var blocked=View(game).PreviewHaul(source,destination,supply,Fix.FromInt(8));c.Equal(HaulTripPolicyCodes.Reserve,blocked.Blocker,"real fuel plus outlet reserve blocks unfunded trip");c.Equal(hash,game.Hash(),"all policy/budget queries pure");
            var recorder=game.RecordReplay(Limits);Order(c,game,new ConfigureHaulTripPolicyCommand{Worker=truck,Settings=new HaulTripSettings("contract.trip-no-floor",true,false)});
            Order(c,game,new AddRouteCommand{From=source,To=destination,Resource=supply,Settings=new LogisticsRouteSettings(true,Fix.FromInt(4),1)});
            var queue=new List<LogisticsDispatchOffer>();View(game).CopyQueue(queue);c.That(queue.Any(value=>value.Haul.Blocker==HaulLoadPolicyCodes.Minimum),"actual automatic route waits below declared minimum");
            hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(new HaulResourcesCommand{Source=source,Destination=destination,Worker=truck,Resource=supply,Ceiling=Fix.FromInt(4),Priority=1}),"explicit manual player haul bypasses automatic batching only");c.Equal(hash,game.Hash(),"selected minimum preview pure");
            Step(game);Step(game);c.Equal(Fix.Zero,Amount(game,truck,"supply"),"actual young request does not load before deadline");
            c.Equal(Fix.Zero,Amount(game,truck,"supply"),"waiting policy cannot load instantly");var twin=factory.Load(game.Save());Until(game,twin,()=>Amount(game,truck,"supply")==Fix.FromInt(4),"deadline admits physical loading");
            // Stop future automatic cycles through ordinary route settings, not test-owned scheduling.
            var routes=new List<RouteView>();View(game).CopyRoutes(routes);
            Order(c,game,new ConfigureRouteCommand{Route=routes.Single().Id,Settings=new LogisticsRouteSettings(false,Fix.FromInt(4),1)});
            Order(c,game,new ConfigureHaulTripPolicyCommand{Worker=truck,Settings=new HaulTripSettings("contract.trip-no-floor",false,false)});
            twin=factory.Load(game.Save());var snapshot=game.Seat(0).Snapshot.Entities.Single(value=>value.Id==truck);Fix position=((PointSnapshot)snapshot.Spatial).Position;Fix retainedFuel=Amount(game,truck,"fuel");
            for(int i=0;i<4;i++)Step(game,twin);c.That(Amount(game,truck,"supply")==Fix.FromInt(4)&&Amount(game,truck,"fuel")==retainedFuel&&((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(value=>value.Id==truck).Spatial).Position==position,"disabled trip retains cargo/fuel/physical position");
            c.Equal(Code.None,View(game).ReadHaulTripBudget(truck,out var held),"original remaining budget view");c.Equal(HaulTripPolicyCodes.Disabled,held.Blocker,"same held job reports typed policy blocker");
            game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();long step=game.Clock.StepIndex;Step(game,twin);c.That(game.Hash()==hash&&game.Clock.StepIndex==step,"sole-clock pause advances nothing");game.Clock.Resume();twin.Clock.Resume();
            Order(c,game,new ConfigureHaulTripPolicyCommand{Worker=truck,Settings=new HaulTripSettings("contract.trip-no-floor",true,false)});twin=factory.Load(game.Save());Until(game,twin,()=>Amount(game,destination,"supply")==Fix.FromInt(4)&&Amount(game,truck,"supply")==Fix.Zero,"same physical delivery and unload complete");
            c.That(Amount(game,truck,"fuel")<retainedFuel&&Amount(game,truck,"fuel")>=Fix.Zero,"native elapsed movement consumes actual fuel");
            c.Equal(Fix.FromInt(20),Amount(game,source,"supply")+Amount(game,destination,"supply")+Amount(game,truck,"supply"),"cargo conservation without duplicates");c.That(load.MinimumLoad.AmountOf(supply)==Fix.FromInt(8)&&trip.PreserveOutletReserve,"retained facts unchanged");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"same job/component/stock codecs continue");c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),Limits).Success,"normal routes and policies replay");
            var fresh=New(factory);c.That(Amount(fresh,Entity(fresh,"reserve.truck"),"fuel")==Fix.FromInt(12)&&Amount(fresh,Entity(fresh,"destination"),"supply")==Fix.Zero,"fresh session isolated");Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"enabled","bypass","minimum","wait","resource","capacity","trip-enabled","preserve","sources","duplicate-service","bad-service","unknown-service","load-kind","trip-kind"})
            {
                var initialize=new InitializeProbe();c.Throws<ContentException>(()=>New(Factory(initialize,out _,edit:document=>{
                    var load=document.Get("independent_haul_load_profiles")[0];var trip=document.Get("independent_haul_trip_profiles")[0];
                    if(fault=="enabled")load.Remove("enabled");if(fault=="bypass")load.Remove("emergency_bypass");if(fault=="minimum")load.Remove("minimum_load");if(fault=="wait")load.Remove("maximum_wait");
                    if(fault=="resource")load.Set("minimum_load",Json.Parse("{\"missing\":1}"));if(fault=="capacity")load.Set("minimum_load",Json.Parse("{\"supply\":41}"));
                    if(fault=="trip-enabled")trip.Remove("enabled");if(fault=="preserve")trip.Remove("preserve_outlet_reserve");if(fault=="sources")trip.Remove("service_sources");
                    if(fault=="duplicate-service")trip.Set("service_sources",Json.Parse("[\"service\",\"service\"]"));if(fault=="bad-service")trip.Set("service_sources",Json.Parse("[\"Bad Service\"]"));if(fault=="unknown-service")trip.Set("service_sources",Json.Parse("[\"missing\"]"));
                },poweredEdit:document=>{var parts=document.Get("entities")[1].Get("components");if(fault=="load-kind")parts.Get("haul_load_policy").Set("profile",JsonValue.Of("contract.trip"));if(fault=="trip-kind")parts.Get("haul_trip_policy").Set("profile",JsonValue.Of("contract.load"));})),"early haul content refusal "+fault);c.Equal(0,initialize.Initializations,"before Initialize "+fault);
            }
        }
    }
}
