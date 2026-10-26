using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
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
    public static class NetworkIsolationTests
    {
        private static readonly Code Source = new Code("leak"), Blocked = new Code("network.isolated");
        private static JsonValue Parts(JsonValue document, int index = 0) => document.Get("entities")[index].Get("components");
        private static JsonValue Gate(JsonValue document) => Parts(document).Get("signal_network_gate");
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        public static void Register()
        {
            TestRunner.Add("Network isolation: shared gate contracts and strict admission", Contracts);
            TestRunner.Add("Network isolation: physical phase closure and saved flow recovery", Physical);
        }
        private static GameFactory Factory(Action<JsonValue> edit = null)
        {
            JsonValue document = Json.Parse(FrameworkProofContent.Read("network-isolation.json")); edit?.Invoke(document);
            return Colony.Factory(FrameworkProofContent.Read("resource-recovery.json"), FrameworkProofContent.Read("endpoints-flows.json"),
                FrameworkProofContent.Read("network-transfers.json"), additionalDocuments: new Dictionary<string, string> { { "network-isolation.json", Text(document) } });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "colony", Seed = 109 }; request.Mods.Add(ResourceFlowsModule.ModuleId);
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Spawn(SimGame game, string definition, int x, int seat = 0)
        {
            Entity entity = ((TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>(definition), new PlayerId(seat), new TileCoord(x, 28));
            game.World.Entities.Flush(); return entity;
        }
        private static void Tick(SimGame game)
        { if (game.RunTicks(1) != 1) throw new InvalidOperationException("network isolation tick=" + game.Tick + ": " + game.Fault); }
        private static void Advance(SimGame game, int ticks, SimGame twin = null)
        { for (int i = 0; i < ticks; i++) { Tick(game); if (twin != null) Tick(twin); } }
        private static void Reach(SimGame game, Entity target, Func<bool> complete, string phase, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !complete(); i++) Advance(game, 1, twin);
            if (!complete())
            {
                Order order = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, Source);
                throw new InvalidOperationException(phase + " timeout; tick=" + game.Tick + "; gate=" + Gates(game, target.Id).Blocker +
                    "; response=" + order?.State + "/" + order?.Blocker);
            }
        }
        private static NetworkService Setup(SimGame game, bool neutral = false, bool advance = true)
        {
            NetworkService lines = game.World.Services.Providers<NetworkService>().Single();
            foreach (string id in neutral ? new[] { "network.pipe", "network.public-pipe" } : new[] { "network.pipe" })
                for (int x = 10; x <= 14; x++)
                    if (!((Beep.SimEngine.Maps.TileLineGeometry)lines.World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>(id), new TileCoord(x, 28), new PlayerId(0)))
                        throw new InvalidOperationException("network fixture line refused");
            if (advance) Tick(game); return lines;
        }
        private static ExecutionGateEvaluation Gates(SimGame game, EntityId entity, string network = "network.main")
        {
            Code refusal = game.Seat(0).View.Feature<InfrastructureView>().ReadGates(entity, network, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("network gate refused=" + refusal); return facts;
        }
        private static CommandResult Raise(SimGame game, EntityId target)
            => TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target, Source = Source, Kind = new Code("network.leak") });
        private static CommandResult Permit(SimGame game, EntityId target)
            => TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target, Enabled = true });
        private static Fix Total(NetworkService lines, string network, ResourceId resource)
        { Fix total = Fix.Zero; for (int i = 0; i < lines.ComponentCount(network); i++) total += lines.Component(network, i).Amount(resource); return total; }
        private static void Invalid(Check c, Action<JsonValue> edit, string message)
            => c.Throws<ContentException>(() => New(Factory(edit)), message);

        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Spawn(game, "network.isolated-source", 9);
            Entity receiver = Spawn(game, "network.isolated-receiver", 15), foreign = Spawn(game, "network.isolated-source", 30, 1);
            Setup(game);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "shared gate contract has current explicit build identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "existing stock/work/incident codecs stay unchanged");
            c.That(source.Def.Component<INetworkPortsDefinition>() is PortsDef && receiver.Def.Component<INetworkPortsDefinition>() is ProofNetworkPortsDef,
                "gate authoring accepts native and independent port metadata through one contract");
            c.That(receiver.Get<INetworkPorts>() is ProofNetworkPorts && receiver.Get<Ports>() == null, "runtime flow does not require concrete Ports");
            var definition = source.Def.Component<SignalNetworkGateDef>();
            c.That(ReferenceEquals(definition.Networks[0], game.World.Content.Get<INetworkDefinition>("network.main")), "gate selects exact existing catalog network");
            c.That(!typeof(SignalNetworkGate).GetMethod("Sync").DeclaringType.Equals(typeof(SignalNetworkGate)), "gate stores no second connection or hold state");
            c.That(typeof(INamedEntityCapability).IsAssignableFrom(typeof(INetworkFlowGate)), "connection gates are named public extension contracts");
            c.That(typeof(SignalTransformationGateDef).GetProperty("Conditions").PropertyType == typeof(SignalNetworkGateDef).GetProperty("Conditions").PropertyType,
                "process/network gates share one immutable condition definition type");
            bool operationRequired = false;
            foreach (var requirement in new InfrastructureModule().Requires) if (requirement.Id == OperationsModule.ModuleId) operationRequired = true;
            c.That(!operationRequired, "native infrastructure does not make signals/operations compulsory for nongated networks");
            ulong hash = game.Hash(); ExecutionGateEvaluation before = Gates(game, source.Id);
            c.Equal(Code.None, before.Blocker, "unraised connection has actual open readiness");
            c.Equal(hash, game.Hash(), "gate diagnostics cannot mutate progress, stock or topology");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateSnapshot>)before.Gates).Clear(), "gate collection is immutable");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateConditionSnapshot>)before.Gates[0].Conditions).Clear(), "signal condition facts are detached and immutable");
            c.Equal(Codes.NotFound, game.Seat(0).View.Feature<InfrastructureView>().ReadGates(foreign.Id, "network.main", out _), "foreign connection readiness stays private");
            c.Equal(Code.None, game.World.ViewFor(PlayerId.None).Feature<InfrastructureView>().ReadGates(foreign.Id, "network.main", out _), "explicit observer can inspect actual connection");
            c.Equal(Codes.UnknownDef, game.Seat(0).View.Feature<InfrastructureView>().ReadGates(source.Id, "absent", out _), "unknown network refuses explicitly");
            c.Equal(Codes.InvalidArgument, game.Seat(0).View.Feature<InfrastructureView>().ReadGates(source.Id, "network.secondary", out _), "unsupported connection is not fabricated");
            c.That(!Gates(game, source.Id, "network.public").Gates[0].Applies && Gates(game, source.Id, "network.public").Gates[0].Conditions.Count == 0,
                "unselected supported network returns no substitute signal facts");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "stateless gate shares native checkpoint identity");
            Invalid(c, j => Gate(j).Remove("id"), "capability identity is mandatory");
            Invalid(c, j => Gate(j).Set("networks", JsonValue.NewArray()), "network selection is explicit nonempty");
            Invalid(c, j => Gate(j).Get("networks").Add(JsonValue.Of("network.main")), "duplicate target network fails");
            Invalid(c, j => Gate(j).Set("networks", Json.Parse("[\"absent\"]")), "missing target network fails");
            Invalid(c, j => Gate(j).Set("networks", Json.Parse("[\"network.secondary\"]")), "unsupported target metadata fails");
            Invalid(c, j => Gate(j).Set("conditions", JsonValue.NewArray()), "missing readiness cannot silently become open");
            Invalid(c, j => Gate(j).Get("conditions").Add(Gate(j).Get("conditions")[0].Clone()), "duplicate condition identity fails");
            Invalid(c, j => Gate(j).Get("conditions")[0].Set("source", JsonValue.Of("leak")), "incident is not a substitute signal provider");
            Invalid(c, j => Gate(j).Get("conditions")[0].Set("signal", JsonValue.Of("absent")), "missing signal refuses");
            Invalid(c, j => Gate(j).Get("conditions")[0].Set("expected", JsonValue.Number("0")), "boolean expectation is not inferred from numbers");
            Invalid(c, j => Gate(j).Get("conditions")[0].Set("minimum", JsonValue.Number("0")), "boolean and numeric conditions cannot overlap");
            Invalid(c, j => Gate(j).Set("enabled", JsonValue.Of(true)), "no hidden persistent enabled state is admitted");
            Invalid(c, j => Parts(j).Remove("ports"), "gates require actual network metadata/runtime capability");
            Invalid(c, j => Parts(j).Get("ports").Get("list")[0].Set("pull", Json.Parse("[\"network.spare\"]")), "independent authoring still checks carried resources");
            c.That(Raise(game, source.Id).Accepted, "ordinary command raises exact native episode");
            Reach(game, source, () => Gates(game, source.Id).Blocker == Blocked, "isolation contract");
            c.Equal(Code.None, before.Blocker, "captured preincident aggregate remains detached after closure");
            c.That(before.Gates[0].Conditions[0].Ready, "captured condition never turns into a live provider handle");
            Numeric(c);
        }

        private static void Numeric(Check c)
        {
            GameFactory factory = Factory(j =>
            {
                Parts(j).Set("dial_operation", Json.Parse("{\"operation\":{\"id\":\"dial\",\"enabled\":false,\"target\":1,\"minimum\":0,\"maximum\":2,\"unit\":\"unit.units\"}}"));
                j.Get("signals").Add(Json.Parse("{\"id\":\"network.dial\",\"kind\":\"number\",\"metric\":\"dial.accumulated\",\"unit\":\"unit.units\",\"providers\":[{\"component\":\"dial_operation\",\"schema\":1}]}"));
                Gate(j).Get("conditions").Add(Json.Parse("{\"id\":\"capacity\",\"source\":\"dial\",\"signal\":\"network.dial\",\"minimum\":0,\"maximum\":1,\"unit\":\"unit.units\",\"blocker\":\"network.range\"}"));
            });
            SimGame game = New(factory); Entity source = Spawn(game, "network.isolated-source", 9); Setup(game);
            ExecutionGateEvaluation facts = Gates(game, source.Id);
            c.Equal("capacity", facts.Gates[0].Conditions[0].Id, "shared owner sorts conditions by stable identity, not JSON order");
            c.Equal(Code.None, facts.Blocker, "inclusive numeric range uses actual provider units");
            c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = source.Id, Operation = new Code("dial"), Enabled = true, Target = Fix.One }).Accepted,
                "ordinary control changes the existing signal provider");
            Advance(game, 2);
            c.Equal(new Code("network.range"), Gates(game, source.Id).Blocker, "out-of-range signal blocks through the same shared evaluator");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "range gate saves only its actual operation state");
        }

        private static void Physical(Check c)
        { Direction(c, false); Direction(c, true); ImmediateAndUnrelated(c); Failure(c); Unavailable(c); ProcessReuse(c); Faults(c); }

        private static void Unavailable(Check c)
        {
            GameFactory factory = Factory(j => {
                Parts(j).Set("network_sensor", Json.Parse("{\"operation\":{\"id\":\"gauge\",\"enabled\":true,\"target\":1,\"minimum\":0,\"maximum\":2,\"unit\":\"unit.level\"},\"level\":0}"));
                j.Get("signals").Add(Json.Parse("{\"id\":\"network.level\",\"kind\":\"number\",\"metric\":\"probe.level\",\"unit\":\"unit.level\",\"providers\":[{\"component\":\"network_sensor\",\"schema\":1}]}"));
                Gate(j).Get("conditions").Add(Json.Parse("{\"id\":\"sensor-ready\",\"source\":\"gauge\",\"signal\":\"network.level\",\"minimum\":0,\"maximum\":1,\"unit\":\"unit.level\",\"blocker\":\"network.level-range\"}"));
            });
            SimGame game = New(factory); Entity source = Spawn(game, "network.isolated-source", 9), receiver = Spawn(game, "network.isolated-receiver", 15);
            Setup(game); ResourceId steel = game.World.Content.ResourceId("steel");
            c.That(TestKit.Order(game, 0, new SetAutomationProbeCommand { Entity = source.Id, Level = Fix.Zero, Available = false }).Accepted,
                "ordinary environment input makes actual provider unavailable");
            c.Equal(new Code("probe.sensor-offline"), Gates(game, source.Id).Blocker, "unavailable signal preserves its provider blocker, not authored range/default readiness");
            Fix stock = source.Get<IInventory>().Amount(steel); SimGame twin = factory.Load(game.Save()); Advance(game, 20, twin);
            c.Equal(stock, source.Get<IInventory>().Amount(steel), "unavailable sensor cannot spend or transfer physical stock");
            c.Equal(game.Hash(), twin.Hash(), "unavailable state and gated physical flow continue identically");
            c.That(TestKit.Order(game, 0, new SetAutomationProbeCommand { Entity = source.Id, Level = Fix.Zero, Available = true }).Accepted &&
                TestKit.Order(twin, 0, new SetAutomationProbeCommand { Entity = source.Id, Level = Fix.Zero, Available = true }).Accepted,
                "real provider input restores availability without a gate fallback");
            c.Equal(Code.None, Gates(game, source.Id).Blocker, "compatible restored sample opens native readiness");
            Advance(game, 20, twin);
            c.That(receiver.Get<IInventory>().Amount(steel).IsPositive, "normal timed stock transfer follows actual sensor recovery");
            c.Equal(game.Hash(), twin.Hash(), "saved sensor recovery does not create another gate-state owner");
        }

        private static void Direction(Check c, bool pull)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            Entity source = Spawn(game, "network.isolated-source", 9), receiver = Spawn(game, "network.isolated-receiver", 15);
            Entity target = pull ? receiver : source; NetworkService lines = Setup(game); ResourceId steel = game.World.Content.ResourceId("steel");
            c.That(Raise(game, target.Id).Accepted, "ordinary episode begins actual " + (pull ? "pull" : "push") + " isolation");
            c.Equal(Code.None, Gates(game, target.Id).Blocker, "phase hold cannot close before the responder completes physical work");
            Reach(game, target, () => Gates(game, target.Id).Blocker == Blocked, "timed connection isolation");
            c.That(receiver.Get<IInventory>().Amount(steel).IsPositive, "real timed flow occurs before physical isolation, no retroactive stock undo");
            Order order = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, Source);
            c.That(order.Node("isolate").State == NodeState.Done && order.Node("cleanup").State != NodeState.Done, "native DAG closes connection before parallel cleanup finishes");
            var pool = lines.Component("network.main", 0); ResourceEndpointId endpoint = pool.Endpoint;
            var owner = new ReservationOwner("proof.isolation", 1); ReservationToken held = pool.Inventory.ReserveIn(owner, steel, Fix.One);
            c.That(held.IsValid, "existing endpoint owns a real retained capacity promise");
            Fix sourceStock = source.Get<IInventory>().Amount(steel), receiverStock = receiver.Get<IInventory>().Amount(steel);
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "isolated endpoint and live pool promise restore together");
            if (!pull)
            {
                if (!TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted)
                    throw new InvalidOperationException("ordinary response cancellation refused");
                twin = factory.Load(game.Save());
                c.Equal(Blocked, Gates(game, target.Id).Blocker, "response cancellation cannot release completed physical isolation");
                Advance(game, 1, twin);
                c.Equal(order.Id, game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, Source).Id, "response resumes the same real work order");
            }
            Advance(game, 30, twin);
            c.Equal(pull ? receiverStock : sourceStock, (pull ? receiver : source).Get<IInventory>().Amount(steel), "closed direction consumes/transfers no stock");
            c.Equal(Blocked, Gates(game, target.Id).Blocker, "actual blocked cleanup keeps the valve closed");
            c.Equal(endpoint, lines.Component("network.main", 0).Endpoint, "valve does not split or replace actual network topology");
            c.Equal(5, lines.Component("network.main", 0).MemberCount, "all real line tiles remain connected");
            c.That(pool.Inventory.Reservations.Count == 1 && pool.Inventory.Reservations[0].Token.Equals(held), "unrelated live promise is not cleared by shutdown");
            c.Equal(Fix.FromInt(100), source.Get<IInventory>().Amount(steel) + receiver.Get<IInventory>().Amount(steel) + Total(lines, "network.main", steel), "shutdown conserves actual native stock");
            c.Equal(game.Hash(), twin.Hash(), "paused direction continues hash-identically with native responders and receipts");
            pool.Inventory.ReleaseIn(owner, held); twin.World.Services.Providers<NetworkService>().Single().Component("network.main", 0).Inventory.ReleaseIn(owner, held);
            c.That(Permit(game, target.Id).Accepted && Permit(twin, target.Id).Accepted, "normal player readiness permits timed cleanup");
            Reach(game, target, () => !target.Get<Incident>(Source).ReadLifecycle().Active, "actual cleanup/restart", twin);
            c.Equal(Code.None, Gates(game, target.Id).Blocker, "whole physical response opens selected connection");
            Fix before = receiver.Get<IInventory>().Amount(steel); Advance(game, 20, twin);
            c.That(receiver.Get<IInventory>().Amount(steel) > before, "normal positive-time transfer resumes, not an instant stock grant");
            c.Equal(Fix.FromInt(6), target.Get<IInventory>().Amount(game.World.Content.ResourceId("network.spare")), "one isolation and three repair units are physically spent exactly once");
            c.Equal(Fix.FromInt(100), source.Get<IInventory>().Amount(steel) + receiver.Get<IInventory>().Amount(steel) + Total(lines, "network.main", steel), "restart preserves total stock after real verified transfers");
            c.Equal(game.Hash(), twin.Hash(), "saved isolated response and actual resumed transfers are deterministic");
        }

        private static void ImmediateAndUnrelated(Check c)
        {
            GameFactory factory = Factory(j => Gate(j).Get("conditions")[0].Set("signal", JsonValue.Of("network.incident.active")));
            SimGame game = New(factory); Entity source = Spawn(game, "network.isolated-source", 9), receiver = Spawn(game, "network.isolated-receiver", 15);
            NetworkService lines = Setup(game, true); ResourceId steel = game.World.Content.ResourceId("steel");
            c.That(Raise(game, source.Id).Accepted, "authored active policy raises its real episode");
            c.Equal(Blocked, Gates(game, source.Id).Blocker, "active policy closes before physical isolation nodes complete");
            c.Equal(Code.None, Gates(game, source.Id, "network.public").Blocker, "unselected actual connection remains open");
            SimGame twin = factory.Load(game.Save()); Advance(game, 20, twin);
            c.Equal(Fix.Zero, Total(lines, "network.main", steel), "blocked main push has no transfer receipts or invented pool stock");
            c.That(receiver.Get<IInventory>().Amount(steel).IsPositive, "unrelated neutral connection transfers real stock normally");
            c.Equal(Fix.FromInt(100), source.Get<IInventory>().Amount(steel) + receiver.Get<IInventory>().Amount(steel) + Total(lines, "network.main", steel) + Total(lines, "network.public", steel), "selective shutdown conserves stock across independent pools");
            c.Equal(game.Hash(), twin.Hash(), "active shutdown and unrelated timed flow save identically");
        }

        private static void Failure(Check c)
        {
            GameFactory factory = Factory(j => {
                j.Get("activities")[0].Get("nodes")[1].Set("requirement", JsonValue.Of("network.cleanup-permit"));
                Parts(j).Remove("work_permit");
            });
            SimGame game = New(factory); Entity source = Spawn(game, "network.isolated-source", 9), receiver = Spawn(game, "network.isolated-receiver", 15);
            Setup(game);
            c.That(Raise(game, source.Id).Accepted, "episode can start before later repair reference fails");
            Reach(game, source, () => Gates(game, source.Id).Blocker == Blocked, "failed response isolation");
            Reach(game, source, () => game.World.Services.Get<WorkBoard>().ResponseOn(source.Id, Source, 1).State == OrderState.Failed, "permanent repair failure");
            Fix stock = source.Get<IInventory>().Amount(game.World.Content.ResourceId("steel"));
            SimGame twin = factory.Load(game.Save()); Advance(game, 20, twin);
            c.Equal(Blocked, Gates(game, source.Id).Blocker, "permanent failure never auto-opens the network");
            c.Equal(stock, source.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "failed-response hold cannot silently export stock");
            c.Equal(game.Hash(), twin.Hash(), "permanent failure keeps actual episode/history and closed valve through save");
        }

        private static void ProcessReuse(Check c)
        {
            var factory = Colony.Factory(additionalDocuments: new Dictionary<string, string> {
                { "incident-phases.json", FrameworkProofContent.Read("incident-phases.json") },
                { "incident-isolation.json", FrameworkProofContent.Read("incident-isolation.json") } });
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 109 });
            Entity machine = ((TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>("isolated-machine"), new PlayerId(0), new TileCoord(16, 18));
            game.World.Entities.Flush(); Code process = new Code("process"); var view = game.Seat(0).View.Feature<TransformationManagementView>();
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = machine.Id, Capability = process, Transformation = "leak.batch", Count = 1 }).Accepted,
                "process contract still starts actual native batch after shared refactor");
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = machine.Id, Source = Source, Kind = new Code("machine.leak") }).Accepted, "ordinary process incident starts");
            for (int i = 0; i < 300; i++)
            {
                view.ReadGates(machine.Id, process, "leak.batch", out var facts);
                if (!facts.Blocker.IsNone) break; Tick(game);
            }
            c.That(view.ReadGates(machine.Id, process, "leak.batch", out var closed).IsNone && closed.Blocker == new Code("incident.isolated"), "same generic facts represent typed process safety readiness");
            view.TryGetTransformation(machine.Id, process, out var before); SimGame twin = factory.Load(game.Save()); Advance(game, 20, twin);
            view.TryGetTransformation(machine.Id, process, out var after);
            c.Equal(before.Progress, after.Progress, "shared validator keeps ongoing physical batch paused");
            c.Equal(Fix.FromInt(2), after.Wip.Stock.AmountOf(game.World.Content.ResourceId("steel")), "process WIP remains with its sole native owner");
            c.Equal(game.Hash(), twin.Hash(), "process gate migration preserves saved twins");
        }

        private static void Faults(Check c)
        {
            for (int mode = 1; mode <= 6; mode++)
            {
                int selected = mode; GameFactory factory = Factory(j => Parts(j).Set("network_gate_probe",
                    Json.Parse("{\"id\":\"probe\",\"network\":\"network.main\",\"mode\":" + selected + "}")));
                SimGame game = New(factory); Entity source = Spawn(game, "network.isolated-source", 9); Setup(game, advance: false);
                for (int i = 0; i < 20 && game.Fault == null; i++) game.Update();
                c.That(game.Fault?.Phase == "sim.lines", "dishonest network gate mode" + mode + " faults its actual execution phase");
                c.Equal(Fix.FromInt(100), source.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "gate violation refuses before physical stock acquisition");
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted gate cannot serialize as healthy mode" + mode);
                long tick = game.Tick; game.Update(); c.Equal(tick, game.Tick, "faulted flow has no compensating resume mode" + mode);
            }
        }
    }
}
