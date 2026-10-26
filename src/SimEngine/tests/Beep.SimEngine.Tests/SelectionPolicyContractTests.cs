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
using static Beep.SimEngine.Tests.ServiceSelectionContentContractTests;

namespace Beep.SimEngine.Tests
{
    // JSON inputs, ordinary players and detached facts only; shared engine strategies own normal behavior.
    internal static class SelectionPolicyContractTests
    {
        internal const string Case="Selection policies: direct contracts guard metadata and physical fleet facility work";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual current protocol");
            c.Equal(154,EngineInfo.SaveFormat,"no duplicated policy state codec");
            foreach(string type in new[]{"FleetPolicyDef","ActivityFacilityPolicyDef"})
                c.That(typeof(IFleetPolicy).Assembly.GetType("Beep.SimEngine.Standard."+type)==null,"obsolete base removed "+type);
            Console.WriteLine("selection stage: invalid callbacks");Faults(c);
            foreach(bool independent in new[]{false,true}) { ServiceWork(c,independent); FleetWork(c,independent); }
        }
        private static void ServiceWork(Check c,bool independent)
        {
            Console.WriteLine("selection stage: service "+independent+" initialization");
            var initialize=new InitializeProbe();var factory=Factory(false,initialize,out var proof,independent:independent);
            var game=New(factory,false);Console.WriteLine("selection stage: service initialized");var isolated=New(factory,false);ulong separate=isolated.Hash();
            c.Equal(independent,proof.IndependentPolicies,"native/direct actual aliases");c.Equal(2,initialize.Initializations,"one Initialize per session");
            var worker=Entity(game,"machine.a").Id;var near=Entity(game,"source").Id;var home=Entity(game,"destination").Id;
            ulong hash=game.Hash();var initial=Facilities(game);
            c.Equal(near,initial.Target,"original deterministic nearest selection");c.Equal(hash,game.Hash(),"observed query pure");
            c.Equal(Codes.NotFound,Work(game,1).ReadRecurringFacilities(worker,Service,out var hidden),"company-private facilities");c.That(hidden==null,"no private facts");
            var recorder=game.RecordReplay(Limits);Order(c,game,new SetHomeCommand{Worker=worker,Home=home});
            var preference=new Code("contract.facility.home");Order(c,game,Configure(game,preference,true));
            var twin=factory.Load(game.Save());
            Console.WriteLine("selection stage: accepted service");
            Until(game,twin,()=>Source(game).Schedule.LastPlan>0,"policy admitted physical service",128);long plan=Source(game).Schedule.LastPlan;
            Until(game,twin,()=>Plan(game,plan).Order>0&&Node(game,plan).Progress.WholeTicks>0,"policy travels and performs service",128);
            var partial=Node(game,plan);c.That(partial.Worker==worker&&Plan(game,plan).Target==home,"actual selected worker and target");
            c.That(Condition(game)==Fix.FromInt(4)&&Amount(game,home,"supply")==Fix.FromInt(29),"partial service consumes spares but cannot instantly repair");
            Order(c,game,Configure(game,preference,false));twin=factory.Load(game.Save());
            game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();long at=game.Clock.StepIndex;Step(game,twin);
            c.That(game.Hash()==hash&&at==game.Clock.StepIndex,"sole clock freezes accepted service");game.Clock.Resume();twin.Clock.Resume();
            Until(game,twin,()=>Plan(game,plan).State==PlanState.Done,"accepted physical service finishes",128);
            c.That(Condition(game)==Fix.FromInt(10)&&Amount(game,home,"supply")==Fix.FromInt(29)&&Amount(game,near,"supply")==Fix.FromInt(32),"exact repair/material accounting");
            c.That(initial.Target==near&&partial.Progress.WholeTicks>0,"retained facts detached");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"original completed checkpoint");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),Limits).Success,"same normal commands replay");
            c.Equal(separate,isolated.Hash(),"interleaved session unchanged");
        }
        private static void FleetWork(Check c,bool independent)
        {
            Console.WriteLine("selection stage: fleet "+independent);
            var factory=Factory(true,new InitializeProbe(),out var proof,independent:independent,
                worldEdit:world=>world.Get("entities")[1].Get("components").Get("mover").Set("speed",JsonValue.Number("120")));var game=New(factory,true);Console.WriteLine("selection stage: fleet initialized");
            c.Equal(independent,proof.IndependentPolicies,"tile native/direct policies");
            var machines=game.Seat(0).Snapshot.Entities.Where(e=>e.Def=="survey.machine"&&e.Owner==new PlayerId(0)).OrderBy(e=>e.Id).Select(e=>e.Id).ToArray();
            var policies=new List<FleetPolicySnapshot>();ulong hash=game.Hash();Fleets(game).CopyPolicies(policies);
            var policy=policies.Single(p=>p.Id=="contract.fleet");c.Equal(hash,game.Hash(),"fleet metadata observation pure");
            c.That(policy.ContentDependencies.SequenceEqual(new[]{"haul"})&&policy.NavigationClasses.SequenceEqual(new[]{"survey.route","terrain.route"}),"actual immutable role/navigation metadata");
            c.Throws<NotSupportedException>(()=>((IList<string>)policy.NavigationClasses).Clear(),"metadata detached immutable");
            Console.WriteLine("selection stage: fleet metadata read");
            var recorder=game.RecordReplay(Limits);Order(c,game,new ConfigureFleetCommand{Fleet="machines",Settings=new FleetSettings(policy.Id,machines)});
            c.Equal(FleetCodes.MemberConflict,Fleets(game).PreviewConfigure("overlap",new FleetSettings(policy.Id,machines),out _),"original exclusive membership owner");
            c.That(!Fleets(game,1).TryRead("machines",out _),"private fleet view");
            Console.WriteLine("selection stage: fleet member admitted");
            Order(c,game,new ConfigureFleetNavigationCommand{Fleet="machines",Profile="terrain.route"});
            var source=Entity(game,"survey.site").Id;var destination=Entity(game,"fleet.destination").Id;
            c.That(game.Seat(0).View.TryFindResource("steel",out var steel),"actual resource identity");
            Order(c,game,new HaulResourcesCommand{Source=source,Destination=destination,Worker=machines[0],Resource=steel,Ceiling=Fix.FromInt(8),Priority=60});
            Console.WriteLine("selection stage: fleet haul admitted");
            var twin=factory.Load(game.Save());Until(game,twin,()=>Amount(game,machines[0],"steel")==Fix.FromInt(8),"actual fleet member cargo",128);
            Console.WriteLine("selection stage: fleet physically loaded");
            c.Equal(Fix.Zero,Amount(game,destination,"steel"),"loading is not delivery");Console.WriteLine("selection stage: loaded checkpoint begin");twin=factory.Load(game.Save());Console.WriteLine("selection stage: loaded checkpoint end");
            Until(game,twin,()=>Amount(game,destination,"steel")==Fix.FromInt(8)&&Amount(game,machines[0],"steel")==Fix.Zero,"original physical delivery",96);
            c.Equal(Fix.FromInt(20),Amount(game,source,"steel")+Amount(game,destination,"steel")+machines.Aggregate(Fix.Zero,(n,id)=>n+Amount(game,id,"steel")),"conserved cargo");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"original membership/work checkpoint");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),Limits).Success,"policy commands replay");
        }
        private static GameFactory InvalidFactory(string fault,bool tile,out ServiceSelectionContentModule proof,InitializeProbe initialize=null)
            =>Factory(tile,initialize??new InitializeProbe(),out proof,edit:document=>{
                document.Get(tile?"independent_fleet_policies":"independent_facility_policies")[0].Set("metadata_fault",JsonValue.Of(fault));
            },worldEdit:world=>{
                var entities=world.Get("entities");
                for(int i=0;i<entities.Count;i++)entities[i].Get("components").Set("selection_private_probe",Json.Parse("{}"));
            });
        private static void Faults(Check c)
        {
            foreach(string fault in new[]{"node-private","node-pause","node-throw","rank-target-private","rank-worker-private","rank-pause","rank-throw"})
                foreach(bool preview in fault.StartsWith("rank",StringComparison.Ordinal)?new[]{false}:new[]{false,true})
                {
                    var factory=InvalidFactory(fault,false,out var proof);var game=New(factory,false);Step(game);
                    proof.ArmObservationFault();
                    Action observe=preview&&!fault.StartsWith("rank",StringComparison.Ordinal)
                        ?()=>game.Seat(0).Preview(Configure(game,new Code("contract.facility.nearest"),false)) : ()=>Facilities(game);
                    Failed(c,game,observe,fault,"facility");
                }
            foreach(string fault in new[]{"member-private","member-pause","member-throw","classes-pause","dependencies-throw","navigation-throw"})
            {
                var factory=InvalidFactory(fault,true,out var proof);var game=New(factory,true);Step(game);
                var machine=game.Seat(0).Snapshot.Entities.First(e=>e.Def=="survey.machine"&&e.Owner==new PlayerId(0)).Id;var settings=new FleetSettings("contract.fleet",new[]{machine});
                if(fault=="navigation-throw")Order(c,game,new ConfigureFleetCommand{Fleet="machines",Settings=settings});
                proof.ArmObservationFault();
                Action observe=fault=="navigation-throw"?()=>Fleets(game).PreviewNavigation("machines","terrain.route"):
                    fault=="classes-pause"?()=>Fleets(game).CopyPolicies(new List<FleetPolicySnapshot>()):
                    ()=>Fleets(game).PreviewConfigure("machines",settings,out _);
                Failed(c,game,observe,fault,"fleet");
            }
            var initialize=new InitializeProbe();
            c.Throws<ContentException>(()=>New(InvalidFactory("validate-throw",true,out _,initialize),true),"typed composition callback refusal");
            c.Equal(0,initialize.Initializations,"invalid unused fleet policy before Initialize");
        }
        private static void Failed(Check c,SimGame game,Action observe,string fault,string context)
        {
            if(fault.EndsWith("throw",StringComparison.Ordinal))c.Throws<ContentException>(observe,"first typed error "+context+"/"+fault);
            else c.Throws<InvalidOperationException>(observe,"actual selected observation "+context+"/"+fault);
            c.That(game.Fault!=null&&(!fault.EndsWith("throw",StringComparison.Ordinal)||game.Fault.Message.StartsWith("ContentException:",StringComparison.Ordinal)),"latched first fault "+fault);
            c.Throws<InvalidOperationException>(()=>game.Save(),"fault cannot checkpoint "+fault);
            c.Throws<InvalidOperationException>(()=>game.Hash(),"fault cannot publish hash "+fault);
        }
    }
}
