using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Thin player and explicit authoring input only; no live World, providers, scheduler or resource mutation.
    internal static class ServiceSelectionContentContractTests
    {
        internal const string Case="Service selection content contracts: independent JSON fleet and facility policies drive physical players";
        internal static readonly Code Service=new Code("fleet.service"),Nearest=new Code("contract.facility.nearest"),Home=new Code("contract.facility.home");
        internal static readonly ReplayLimits Limits=new ReplayLimits(8*1024*1024,5000);
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static JsonValue Declarations()=>Json.Parse("[{\"id\":\"sim.machine-fleets\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.motion\",\"version\":\"1.0.0\"},{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.service-selection\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"service-selection-contracts.json\"]}]");
        private static void Enroll(JsonValue world)
        {var ids=world.Get("scenarios")[0].Get("modules");ids.Add(JsonValue.Of(MachineFleetsModule.ModuleId));ids.Add(JsonValue.Of(ServiceSelectionContentModule.ModuleId));}
        internal static GameFactory Factory(bool tile,InitializeProbe initialize,out ServiceSelectionContentModule proof,Action<JsonValue> edit=null,Action<JsonValue> worldEdit=null,bool independent=true)
        {
            proof=new ServiceSelectionContentModule(initialize);var content=Json.Parse(FrameworkProofContent.Read("service-selection-contracts.json"));edit?.Invoke(content);
            if(!independent)
            {
                content.Set("fleet_policies",content.Get("independent_fleet_policies"));content.Set("independent_fleet_policies",Json.Parse("[]"));
                content.Set("activity_facility_policies",content.Get("independent_facility_policies"));content.Set("independent_facility_policies",Json.Parse("[]"));
            }
            var modules=new GameModule[]{new MachineFleetsModule(),proof};var documents=new Dictionary<string,string>{{"service-selection-contracts.json",content.ToString()}};
            if(tile)return FleetNavigationTests.Factory(additionalModules:modules,additionalDeclarations:Declarations(),additionalDocuments:documents,worldEdit:world=>{Enroll(world);worldEdit?.Invoke(world);});
            return FacilitySelectionTests.Factory(policy:Nearest.Id,additionalModules:modules,additionalDeclarations:Declarations(),additionalDocuments:documents,edit:world=>{Enroll(world);StartupContentAuthoring.Position(world,"machine.b",JsonValue.Number("30"));StartupContentAuthoring.Position(world,"machine.c",JsonValue.Number("40"));StartupContentAuthoring.Position(world,"machine.d",JsonValue.Number("50"));worldEdit?.Invoke(world);});
        }
        internal static SimGame New(GameFactory factory,bool tile)=>tile?LiveSensorTests.New(factory):FacilitySelectionTests.New(factory);
        internal static EntityView Entity(SimGame game,string id,int seat=0)=>game.Seat(seat).Snapshot.Entities.Single(value=>value.Def==id&&value.Owner==new PlayerId(seat));
        internal static WorkView Work(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<WorkView>();
        internal static FleetView Fleets(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<FleetView>();
        internal static Fix Amount(SimGame game,EntityId entity,string resource)
        {var values=new List<Facet>();game.Seat(0).View.Inspect(entity,values);return values.Where(value=>value.Key==Storage.FacetStock&&value.Subject==resource).Aggregate(Fix.Zero,(n,value)=>n+value.Value);}
        internal static void Step(SimGame game,SimGame twin=null)
        {game.Update();twin?.Update();if(game.Fault!=null||twin?.Fault!=null||twin!=null&&game.Hash()!=twin.Hash())throw new InvalidOperationException("policy step="+game.Clock.StepIndex+" fault="+game.Fault+"/"+twin?.Fault);}
        internal static void Until(SimGame game,SimGame twin,Func<bool> done,string stage,int maxUpdates=1500)
        {for(int i=0;i<maxUpdates&&!done();i++)Step(game,twin);if(!done()){var jobs=new List<WorkJobView>();Work(game).CopyJobs(jobs);throw new InvalidOperationException(stage+" step="+game.Clock.StepIndex+" jobs="+string.Join(";",jobs.Select(value=>value.Id+"/"+value.State+"/"+value.Blocker+"/worker="+value.Worker))+" points="+string.Join(";",game.Seat(0).Snapshot.Entities.Where(value=>value.Spatial is PointSnapshot).Select(value=>value.Def+"/"+((PointSnapshot)value.Spatial).Position)));}}
        internal static void Order(Check c,SimGame game,Command command)
        {ulong hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary public preview");c.Equal(hash,game.Hash(),"preview pure");c.That(TestKit.Order(game,0,command).Accepted,"ordinary player command");}
        internal static RecurringPlanView Source(SimGame game)
        {var code=Work(game).ReadRecurringPlan(Entity(game,"machine.a").Id,Service,out var value);if(!code.IsNone)throw new InvalidOperationException("source "+code);return value;}
        internal static ActivityFacilitySelectionView Facilities(SimGame game)
        {var code=Work(game).ReadRecurringFacilities(Entity(game,"machine.a").Id,Service,out var value);if(!code.IsNone)throw new InvalidOperationException("facilities "+code);return value;}
        internal static PlanView Plan(SimGame game,long id)
        {var values=new List<PlanView>();Work(game).CopyPlans(values);return values.Single(value=>value.Id==id);}
        internal static ActivityNodeView Node(SimGame game,long plan)
        {var values=new List<ActivityNodeView>();if(!Work(game).TryCopyNodes(Plan(game,plan).Order,values))throw new InvalidOperationException("missing owned nodes");return values.Single(value=>value.Id=="service");}
        internal static Fix Condition(SimGame game)
        {var code=game.Seat(0).View.Feature<EquipmentConditionView>().Read(Entity(game,"machine.a").Id,new Code("integrity"),out var value);if(!code.IsNone)throw new InvalidOperationException("condition "+code);return value.Value;}
        internal static ConfigureRecurringPlanCommand Configure(SimGame game,Code policy,bool enabled)
            =>new ConfigureRecurringPlanCommand{Entity=Entity(game,"machine.a").Id,Source=Service,Settings=new RecurringPlanSettings(enabled,60,GameDuration.Minutes(1),policy)};
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current open selection protocol");c.Equal(154,EngineInfo.SaveFormat,"same state codecs");
            Facility(c);Fleet(c);Invalid(c);
        }
        private static void Facility(Check c)
        {
            var initialize=new InitializeProbe();var factory=Factory(false,initialize,out var proof);var game=New(factory,false);
            c.That(proof.IndependentPolicies,"actual fleet/facility catalog contracts, not native bases");c.Equal(1,initialize.Initializations,"valid content admitted before one Initialize");
            var worker=Entity(game,"machine.a").Id;var near=Entity(game,"source").Id;var home=Entity(game,"destination").Id;
            ulong hash=game.Hash();var initial=Facilities(game);
            c.Equal(near,initial.Target,"independent nearest preference uses original candidate admission");
            c.That(initial.Candidates.Count==2&&initial.Candidates.All(value=>value.Ready),"both actual stocked compatible facilities");
            c.Throws<NotSupportedException>(()=>((IList<ActivityFacilityCandidate>)initial.Candidates).Clear(),"candidate facts immutable");
            c.Equal(Codes.NotFound,Work(game,1).ReadRecurringFacilities(worker,Service,out var hidden),"foreign selection private");c.That(hidden==null,"no leaked candidate facts");
            c.Equal(hash,game.Hash(),"facility preference query pure");
            c.Equal(ActivityFacilityCodes.Unknown,Work(game).PreviewRecurringPlan(worker,Service,new RecurringPlanSettings(false,60,GameDuration.Minutes(1),new Code("contract.fleet"))),"wrong contract kind cannot configure facility");
            var recorder=game.RecordReplay(Limits);
            Order(c,game,new SetHomeCommand{Worker=worker,Home=home});
            Order(c,game,Configure(game,Home,false));var selected=Facilities(game);
            c.Equal(home,selected.Target,"file-authored home preference ranks safe actual home over nearer dock");
            c.That(selected.Candidates.Single(value=>value.Target==near).Preference==1&&selected.Candidates.Single(value=>value.Target==home).Preference==0,"same declaration computes detached ranks");
            c.That(Amount(game,home,"supply")==Fix.FromInt(32)&&Condition(game)==Fix.FromInt(4),"preference grants no repair or material");
            Order(c,game,Configure(game,Home,true));var twin=factory.Load(game.Save());
            Until(game,twin,()=>Source(game).Schedule.LastPlan>0,"ordinary recurring intent admitted");long plan=Source(game).Schedule.LastPlan;
            Until(game,twin,()=>Plan(game,plan).Order>0&&Node(game,plan).Progress.WholeTicks>0,"physical service starts");
            var partial=Node(game,plan);
            c.That(Plan(game,plan).Target==home&&partial.Worker==worker&&((PointSnapshot)Entity(game,"machine.a").Spatial).Position==Fix.FromInt(11),"actual selected worker travels to home service bay");
            c.That(partial.Resources.Any(value=>value.Choice.Use==WorkResourceUse.Hold)&&partial.Resources.Any(value=>value.Choice.Use==WorkResourceUse.Consume),"original material/tool receipts, not preference-owned stock");
            c.Equal(Fix.FromInt(29),Amount(game,home,"supply"),"exact physical three spares consumed");c.Equal(Fix.FromInt(4),Condition(game),"travel and partial work cannot repair instantly");
            Order(c,game,Configure(game,Nearest,false));
            Order(c,game,new ConfigureWorkPolicyCommand{Worker=worker,Settings=new WorkPolicySettings(false,0,new[]{new WorkerRoleId("inspect"),new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            twin=factory.Load(game.Save());long progress=Node(game,plan).Progress.WholeTicks;
            for(int i=0;i<3;i++)Step(game,twin);
            c.That(Node(game,plan).Progress.WholeTicks==progress&&Plan(game,plan).Target==home,"future preference edit and pause retain accepted target/progress");
            game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();long step=game.Clock.StepIndex;Step(game,twin);
            c.That(game.Hash()==hash&&game.Clock.StepIndex==step,"sole clock pause advances no service");game.Clock.Resume();twin.Clock.Resume();
            Order(c,game,new ConfigureWorkPolicyCommand{Worker=worker,Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("inspect"),new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            twin=factory.Load(game.Save());Until(game,twin,()=>Plan(game,plan).State==PlanState.Done,"native service and departure complete");
            c.That(Condition(game)==Fix.FromInt(10)&&Amount(game,home,"supply")==Fix.FromInt(29)&&Amount(game,near,"supply")==Fix.FromInt(32),"physical repair once, no repeated consume or unrelated stock change");
            c.That(Source(game).Schedule.Settings.FacilityPolicy==Nearest&&!Source(game).Schedule.Settings.Enabled,"only future preference changes, no hidden repeated posting");
            c.That(initial.Target==near&&selected.Target==home&&partial.Progress.WholeTicks>0,"retained snapshots detached");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"facility/source/job original codecs continue");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),Limits).Success,"ordinary home/preference/pause commands replay");
            c.That(Condition(New(factory,false))==Fix.FromInt(4),"fresh point session isolated");
        }
        private static void Fleet(Check c)
        {
            var initialize=new InitializeProbe();var factory=Factory(true,initialize,out var proof);var game=New(factory,true);
            c.That(proof.IndependentPolicies,"direct policy contracts compose with native tile engine");c.Equal(1,initialize.Initializations,"tile initialization once");
            var machines=game.Seat(0).Snapshot.Entities.Where(value=>value.Def=="survey.machine"&&value.Owner==new PlayerId(0)).OrderBy(value=>value.Id).Select(value=>value.Id).ToArray();
            c.Equal(2,machines.Length,"actual authored worker set");
            var policies=new List<FleetPolicySnapshot>();ulong hash=game.Hash();Fleets(game).CopyPolicies(policies);
            var policy=policies.Single(value=>value.Id=="contract.fleet");
            c.That(policy.ContentDependencies.SequenceEqual(new[]{"haul"})&&policy.NavigationClasses.SequenceEqual(new[]{"survey.route","terrain.route"}),"direct role aliases and declared navigation facts");
            c.Throws<NotSupportedException>(()=>((IList<string>)policy.NavigationClasses).Clear(),"fleet profile facts immutable");c.Equal(hash,game.Hash(),"fleet catalog query pure");
            var recorder=game.RecordReplay(Limits);Order(c,game,new ConfigureFleetCommand{Fleet="machines",Settings=new FleetSettings(policy.Id,machines)});
            c.That(Fleets(game).TryRead("machines",out var retained)&&retained.Members.Count==2&&retained.Blocker.IsNone,"sole engine membership store admits actual independent policy");
            c.Equal(FleetCodes.MemberConflict,Fleets(game).PreviewConfigure("overlap",new FleetSettings(policy.Id,machines),out _),"same membership owner refuses overlap");
            c.That(!Fleets(game,1).TryRead("machines",out _),"membership private");
            Order(c,game,new ConfigureFleetNavigationCommand{Fleet="machines",Profile="terrain.route"});
            c.Equal(MotionNavigationCodes.NotAllowed,Fleets(game).PreviewNavigation("machines","missing").Blocker,"unknown navigation is explicit refusal");
            var source=Entity(game,"survey.site").Id;var destination=Entity(game,"fleet.destination").Id;
            c.That(game.Seat(0).View.TryFindResource("steel",out var steel),"public actual resource identity");
            var haul=new HaulResourcesCommand{Source=source,Destination=destination,Worker=machines[0],Resource=steel,Ceiling=Fix.FromInt(8),Priority=60};
            Order(c,game,haul);var twin=factory.Load(game.Save());
            Until(game,twin,()=>Amount(game,machines[0],"steel")==Fix.FromInt(8),"native fleet member loads physical cargo");
            var position=((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(value=>value.Id==machines[0]).Spatial).Position;
            Order(c,game,new ConfigureFleetWorkPolicyCommand{Fleet="machines",Settings=new WorkPolicySettings(false,100,new[]{new WorkerRoleId("haul")},null,true,false,WorkAssignmentMode.All)});
            twin=factory.Load(game.Save());for(int i=0;i<3;i++)Step(game,twin);
            c.Equal(Fix.FromInt(8),Amount(game,machines[0],"steel"),"whole-fleet disable preserves real cargo");
            c.That(((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(value=>value.Id==machines[0]).Spatial).Position==position,"paused machine is not teleported");
            Order(c,game,new ConfigureFleetWorkPolicyCommand{Fleet="machines",Settings=new WorkPolicySettings(true,100,new[]{new WorkerRoleId("haul")},null,true,false,WorkAssignmentMode.All)});
            twin=factory.Load(game.Save());Until(game,twin,()=>Amount(game,destination,"steel")==Fix.FromInt(8)&&Amount(game,machines[0],"steel")==Fix.Zero,"native physical delivery completes");
            c.Equal(Fix.FromInt(20),Amount(game,source,"steel")+Amount(game,destination,"steel")+machines.Aggregate(Fix.Zero,(n,id)=>n+Amount(game,id,"steel")),"conserved resources through membership/control/saves");
            c.That(retained.Settings.Members.SequenceEqual(machines),"retained membership facts detached");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"same membership and physical job codecs");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),Limits).Success,"ordinary fleet/haul/control commands replay");
            var fresh=New(factory,true);c.That(!Fleets(fresh).TryRead("machines",out _)&&Amount(fresh,Entity(fresh,"fleet.destination").Id,"steel")==Fix.Zero,"fresh tile session has no copied membership/cargo");
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"missing-roles","empty-roles","duplicate-roles","unknown-role","wrong-role-kind","missing-navigation","empty-navigation","duplicate-navigation","invalid-navigation","missing-node","bad-node","missing-preference","unknown-probe-node","timed-probe-node","wrong-facility-kind"})
            {
                var initialize=new InitializeProbe();bool tile=fault.Contains("roles")||fault.Contains("role")||fault.Contains("navigation");
                c.Throws<ContentException>(()=>New(Factory(tile,initialize,out _,edit:document=>{
                    var fleet=document.Get("independent_fleet_policies")[0];var facility=document.Get("independent_facility_policies")[0];
                    if(fault=="missing-roles")fleet.Remove("allowed_roles");if(fault=="empty-roles")fleet.Set("allowed_roles",Json.Parse("[]"));
                    if(fault=="duplicate-roles")fleet.Set("allowed_roles",Json.Parse("[\"haul\",\"haul\"]"));if(fault=="unknown-role")fleet.Set("allowed_roles",Json.Parse("[\"missing\"]"));
                    if(fault=="wrong-role-kind")fleet.Set("allowed_roles",Json.Parse("[\"contract.facility.home\"]"));
                    if(fault=="missing-navigation")fleet.Remove("navigation_classes");if(fault=="empty-navigation")fleet.Set("navigation_classes",Json.Parse("[]"));
                    if(fault=="duplicate-navigation")fleet.Set("navigation_classes",Json.Parse("[\"survey.route\",\"survey.route\"]"));if(fault=="invalid-navigation")fleet.Set("navigation_classes",Json.Parse("[\"Bad Navigation\"]"));
                    if(fault=="missing-node")facility.Remove("node");if(fault=="bad-node")facility.Set("node",JsonValue.Of("Bad Node"));if(fault=="missing-preference")facility.Remove("prefer_home");
                    if(fault=="unknown-probe-node")facility.Set("node",JsonValue.Of("missing"));if(fault=="timed-probe-node")facility.Set("node",JsonValue.Of("settle"));
                },worldEdit:world=>{if(fault=="wrong-facility-kind")FacilitySelectionTests.Parts(world).Get("recurring_activity").Set("facility_policy",JsonValue.Of("contract.fleet"));}),tile),"early selection-content refusal "+fault);
                c.Equal(0,initialize.Initializations,"before Initialize "+fault);
            }
        }
    }
}
