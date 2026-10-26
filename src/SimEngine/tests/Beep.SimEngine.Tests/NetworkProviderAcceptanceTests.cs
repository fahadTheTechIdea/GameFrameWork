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
    // Authored master/files, ordinary inputs/views/clock/save/replay only; no network or utility execution here.
    internal static class NetworkProviderAcceptanceTests
    {
        internal const string Case = "Network provider acceptance: independent pulsed solver and shared-stock utility drive earned work and deferred manufacturing";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Reserve = new Code("reserve"), Meter = new Code("meter"), Utility = new Code("utility");
        private static GameFactory Factory(bool direct, bool half, InitializeProbe initialize, out NetworkDefinitionContractsModule network,
            Action<JsonValue> edit = null, Action<JsonValue> setup = null)
        {
            network = new NetworkDefinitionContractsModule(); var document = Json.Parse(FrameworkProofContent.Read("network-provider-acceptance.json"));
            if (!direct) document.Set("independent_networks", JsonValue.NewArray()); edit?.Invoke(document);
            return NetworkSignalBindingTests.Create(initialize, out _, extraModules: new GameModule[] {
                network, new UtilitySignalsModule(), new UtilityConsumerProofModule(), new AutomationModule(), new TransformationModule() },
                extraDeclarations: "[{\"id\":\"proof.network-definition-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"network-provider-acceptance.json\"]},{\"id\":\"sim.utility-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.utility-consumer\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.utility-signals\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.transformations\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}]",
                extraDocuments: new Dictionary<string,string> { { "network-provider-acceptance.json", document.ToString() } },
                edit: root => {
                    if (direct) root.Remove("networks");
                    else { var row = root.Get("networks")[0]; row.Set("solver", JsonValue.Of("provider.pulsed")); row.Set("demand_policy", JsonValue.Of("provider.reserve-demand")); }
                    var parts = root.Get("entities")[1].Get("components"); parts.Get("ports").Get("list")[0].Set("pull", Json.Parse("[\"steel\"]"));
                    parts.Set("proof_utility_consumer", Json.Parse("{\"id\":\"reserve\",\"inputs\":{\"steel\":2}}"));
                    parts.Set("utility_signal", Json.Parse("{\"id\":\"utility\",\"consumer\":\"reserve\"}"));
                    parts.Set("inventory_port", Json.Parse("[{\"id\":\"operating.input\"},{\"id\":\"material.output\"}]"));
                    parts.Set("transformer", Json.Parse("{\"automatic_supply\":false,\"wip_capacity\":\"recovery.mixed\",\"transformations\":[\"provider.manufacture\"],\"states\":{\"working\":\"working\",\"stopped\":\"stopped\"},\"operation\":{\"id\":\"meter\",\"enabled\":false,\"target\":1,\"minimum\":1,\"maximum\":1,\"unit\":\"unit.work-rate\"}}"));
                    setup?.Invoke(root);
                }, editDefinitions: root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 10119, Seats = { Controller.Human, Controller.Human } });
        private static OperationsView Operations(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<OperationsView>();
        private static UtilityDemandSnapshot Demand(SimGame game, Code consumer)
        { if (!Operations(game).TryGetUtilityDemand(Entity(game,"binding.watcher"), consumer, out var value)) throw new InvalidOperationException("missing actual consumer " + consumer); return value; }
        private static TransformationSnapshot Process(SimGame game)
        { if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Entity(game,"binding.watcher"), Meter, out var value)) throw new InvalidOperationException("missing actual processor"); return value; }
        private static Fix Steel(SimGame game, string owner)
        { var values = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game,owner), values); return values.Where(x => x.Key == Storage.FacetStock && x.Subject == "steel").Aggregate(Fix.Zero, (sum,x) => sum+x.Value); }
        private static Fix Total(SimGame game)
        {
            Fix sum = Fix.Zero; var steel = Resource(game);
            foreach (var owner in game.Seat(0).Snapshot.Entities) { var rows = new List<Facet>(); game.Seat(0).View.Inspect(owner.Id, rows); sum += rows.Where(x => x.Key == Storage.FacetStock && x.Subject == "steel").Aggregate(Fix.Zero,(amount,x)=>amount+x.Value); }
            var pools = new List<NetworkPoolView>(); if (!game.Seat(0).View.Feature<InfrastructureView>().CopyPools("binding.main", pools).IsNone) throw new InvalidOperationException("missing actual pool");
            foreach (var pool in pools) foreach (var row in pool.Stock) if (row.Resource == steel) sum += row.Amount;
            return sum;
        }
        private static ResourceId Resource(SimGame game)
        { if (!game.Seat(0).View.TryFindResource("steel", out var value)) throw new InvalidOperationException("missing steel"); return value; }
        private static AutomationSignalSnapshot Ready(SimGame game)
        { var refusal = Operations(game).ReadSignal(Entity(game,"binding.watcher"), Utility, "provider.ready", out var value); if (!refusal.IsNone) throw new InvalidOperationException("readiness " + refusal); return value; }
        private static AutomationSettings Settings(SimGame game)
        {
            if (!Operations(game).TryGetAutomationProfile("provider.control",out var profile)) throw new InvalidOperationException("missing authored control profile");
            var thresholds = new Dictionary<string,ControlThresholdBand>(); foreach (var row in profile.Thresholds) thresholds.Add(row.Key,row.Value.Initial);
            var actor = Entity(game,"binding.watcher"); return new AutomationSettings(profile.Id,true,new ControlBinding(actor,Meter),
                new Dictionary<string,ControlBinding>{{"ready",new ControlBinding(actor,Utility)}}, thresholds, profile.WhenTrue,profile.WhenFalse);
        }
        private static CommandResult Input(Check c,SimGame game,Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary engine preview"); c.Equal(hash,game.Hash(),"pure preview");
            int sequence=game.Seat(0).Submit(command); Step(game); long cursor=0; var results=new List<CommandResult>();game.Seat(0).ReadResults(ref cursor,results);
            var result=results.Single(x=>x.Sequence==sequence);c.Equal(Code.None,result.Refusal,"actual command agrees with preview");return result;
        }
        private static void Until(SimGame game,SimGame twin,Func<bool> done,string stage,int order=0)
        {
            for(int i=0;i<512&&!done();i++)Step(game,twin);
            if(!done())
            {
                var nodes=new List<ActivityNodeView>();var work=game.Seat(0).View.Feature<WorkView>();work.TryCopyNodes(order,nodes);
                string activity=work.TryReadProgress(order,out var progress)?" order="+progress.State+" blocker="+progress.Blocker:" no retained order";
                foreach(var node in nodes)activity+=" node="+node.Id+":"+node.State+":"+node.Blocker+":"+node.Progress.WholeTicks+"/"+node.RequiredTicks;
                long cursor=0;var notes=new List<Notification>();game.Seat(0).ReadNotifications(ref cursor,notes);
                foreach(var note in notes)activity+=" note="+note;
                throw new InvalidOperationException(stage+" exceeded512Updates at="+game.Clock.StepIndex+" phase="+Process(game).Phase+" blocker="+Process(game).Blocker+" progress="+Process(game).Progress.WholeTicks+" supply="+Steel(game,"binding.source")+" reserve="+Steel(game,"binding.watcher")+activity);
            }
        }
        private static ActivityNodeView Node(SimGame game,int order)
        { var rows=new List<ActivityNodeView>();game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order,rows);return rows.Single(x=>x.Id=="inspect"); }
        private static void Facts(Check c,SimGame game)
        {
            ulong hash=game.Hash();var demand=Demand(game,Reserve);var steel=Resource(game);var stock=Steel(game,"binding.watcher");
            c.That(demand.Drawing&&demand.NextInputs.AmountOf(steel)==Fix.FromInt(2)&&demand.Consumed.IsEmpty,"custom reserve obligation is not another consumption ledger");
            c.That(demand.Ports.Count==1&&demand.Ports[0].Endpoint==ResourceEndpointId.ForEntity(Entity(game,"binding.watcher"))
                &&demand.Ports[0].Stock.AmountOf(steel)==stock,"custom utility binds the actual original single inventory");
            var ready=Ready(game);c.That(ready.Available&&ready.Value==(demand.Satisfied?Fix.One:Fix.Zero),"original signal runtime reads actual custom consumer readiness");
            c.Equal(hash,game.Hash(),"utility/signal reads change no stock, claim, exposure or clock");
        }
        private static void Physical(Check c,bool direct,bool half)
        {
            var initialize=new InitializeProbe();var factory=Factory(direct,half,initialize,out var network);var game=New(factory);var idle=New(factory);ulong idleHash=idle.Hash();
            c.Equal(direct,network.ActualContract,"actual native/direct network metadata alias");c.That(initialize.Initializations==2&&!game.Modules.Contains("points")&&!game.Modules.Contains(TycoonModule.ModuleId),"generic runtime module has no point-game or business dependency");
            c.That(typeof(LimitedNetworkSolverDef).BaseType==typeof(Def)&&typeof(ProofUtilityConsumer).BaseType==typeof(Component<ProofUtilityConsumerDef>),"independent execution provider types, not native network/processor inheritance");
            var opening=Total(game);var limits=new ReplayLimits(4*1024*1024,4000);var recording=game.RecordReplay(limits);var twin=factory.Load(game.Save());
            Step(game,twin);c.Equal(Fix.FromInt(30),Steel(game,"binding.source"),"pulsed runtime transfers nothing before authored two-minute boundary");
            Until(game,twin,()=>Steel(game,"binding.watcher")==Fix.One,"first exact bounded one-unit transfer");Facts(c,game);
            c.That(Total(game)==opening&&Steel(game,"binding.watcher")==Fix.One&&Demand(game,Reserve).Blocker==UtilityCodes.Shortage,"custom one-unit limit conserves both transfer legs without fabricating two-unit reserve");
            var retained=Ready(game);var work=game.Seat(0).View.Feature<WorkView>();var requirements=new List<WorkRequirementView>();ulong before=game.Hash();
            c.Equal(Code.None,work.PreviewOrderRequirements("provider.inspect",Entity(game,"binding.watcher"),requirements),"engine requirement preview");
            c.That(requirements.Any(x=>x.Result.Blocker==UtilityCodes.Shortage)&&game.Hash()==before,"custom utility shortage blocks real activity without consumption");
            int order=(int)Input(c,game,new PostOrderCommand{Plan="provider.inspect",Target=Entity(game,"binding.watcher")}).Arg0;
            Until(game,null,()=>Node(game,order).Progress.WholeTicks>0,"actual reserve supplies timed inspection");var partial=Node(game,order);Facts(c,game);
            c.That(partial.Progress.WholeTicks<partial.RequiredTicks&&Ready(game).Value==Fix.One&&retained.Value==Fix.Zero,"earned partial work and retained detached shortage");
            twin=factory.Load(game.Save());game.Clock.Pause();twin.Clock.Pause();long at=game.Clock.StepIndex;ulong paused=game.Hash();Step(game,twin);
            c.That(game.Clock.StepIndex==at&&game.Hash()==paused,"sole clock pauses custom transfers and normal work");game.Clock.Resume();twin.Clock.Resume();
            Until(game,twin,()=>work.TryReadActivityCompletion("activity:provider.inspect",Entity(game,"binding.watcher"),out _),"normal four-minute utility-gated completion",order);
            c.Equal(opening,Total(game),"reserve readiness and work never duplicate or burn stock");
            c.That(!Operations(game,1).TryGetUtilityDemand(Entity(game,"binding.watcher"),Reserve,out _)&&Operations(game,1).ReadSignal(Entity(game,"binding.watcher"),Utility,"provider.ready",out var foreign)==Codes.NotOwned&&!foreign.Available,"visible foreign equipment discloses no private utility/automation detail");
            Input(c,game,new ConfigureAutomationCommand{Rule="provider.safety",Settings=Settings(game)});
            Input(c,game,new EnqueueTransformationCommand{Transformer=Entity(game,"binding.watcher"),Capability=Meter,Transformation="provider.manufacture"});
            Until(game,null,()=>Process(game).Progress.WholeTicks>0&&Process(game).QueueCount==1,"deferred readiness controls native productive consumption");var manufacture=Process(game);var burn=Demand(game,Meter).Consumed.AmountOf(Resource(game));
            c.That(burn.IsPositive&&manufacture.Progress.WholeTicks<manufacture.Required,"only native processor burns real supply over authored time");
            twin=factory.Load(game.Save());var ruleRows=new List<AutomationRuleSnapshot>();Operations(game).CopyAutomationRules(ruleRows);
            c.That(ruleRows.Count==1&&ruleRows[0].Id=="provider.safety","one original automation owner drives actual declared control");
            Until(game,twin,()=>Process(game).QueueCount==0,"pulsed physical refuel and deferred controls earn one manufacture");Facts(c,game);
            c.Equal(Fix.FromInt(6),opening+Fix.One-Total(game),"six actual consumed units and one earned output conserve shared stock");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed providers/control/flow state restore");c.Equal(idleHash,idle.Hash(),"interleaved idle session remains isolated");
            c.That(new ReplayVerifier().Verify(factory,recording.Finish(),limits).Success,"ordinary utility work/automation/manufacture replay");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current unchanged engine identity");
            foreach(bool direct in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,direct,half);
            foreach(string invalid in new[]{"missing-solver","interval","quantity","consumer","utility-owner"})
            {
                var initialize=new InitializeProbe();c.Throws<ContentException>(()=>New(Factory(true,false,initialize,out _,edit:root=>{
                    if(invalid=="missing-solver")root.Get("independent_networks")[0].Set("solver",JsonValue.Of("missing"));
                    if(invalid=="interval")root.Get("limited_network_solvers")[0].Get("interval").Set("numerator",JsonValue.Number("0"));
                    if(invalid=="quantity")root.Get("limited_network_solvers")[0].Set("limits",Json.Parse("{\"steel\":0.5}"));
                    if(invalid=="consumer")root.Get("utility_network_demands")[0].Set("consumers",JsonValue.NewArray());
                },setup:root=>{if(invalid=="utility-owner")root.Get("entities")[1].Get("components").Get("utility_signal").Set("consumer",JsonValue.Of("missing"));})),"invalid runtime declaration refuses "+invalid);
                c.Equal(0,initialize.Initializations,"invalid actual provider is rejected before Initialize");
            }
        }
    }
}
