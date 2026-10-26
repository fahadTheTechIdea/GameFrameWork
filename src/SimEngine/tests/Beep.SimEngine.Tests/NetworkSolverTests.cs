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
    public static class NetworkSolverTests
    {
        private const string Network = "arbitration.main";
        public static void Register()
        {
            TestRunner.Add("Network solvers: explicit contracts, lifetime and guarded execution", Contracts);
            TestRunner.Add("Network solvers: physical selection, refusal and saved continuation", Physical);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(string solver = null, Action<JsonValue> edit = null, Action<JsonValue> providersEdit = null,
            bool utilities = false, Action<JsonValue> utilityEdit = null, bool groups = false, Action<JsonValue> groupEdit = null)
        {
            JsonValue root = Json.Parse(FrameworkProofContent.Read("network-arbitration.json"));
            root.Get("networks")[0].Set("capacity_policy", JsonValue.Of("network.shared"));
            root.Get("networks")[0].Get("rates")[0].Set("quantity", JsonValue.Number("4"));
            root.Get("networks")[0].Get("rates")[0].Get("period").Set("numerator", JsonValue.Number("1"));
            if (solver != null) root.Get("networks")[0].Set("solver", JsonValue.Of(solver)); edit?.Invoke(root);
            JsonValue providers = Json.Parse(FrameworkProofContent.Read("network-solvers.json")); providersEdit?.Invoke(providers);
            var documents = new Dictionary<string, string>
                { ["network-arbitration.json"] = Text(root), ["network-solvers.json"] = Text(providers) };
            if (utilities)
            {
                JsonValue utility = Json.Parse(FrameworkProofContent.Read("network-utilities.json")); utilityEdit?.Invoke(utility);
                documents.Add("network-utilities.json", Text(utility));
            }
            if (groups)
            {
                JsonValue group = Json.Parse(FrameworkProofContent.Read("network-utility-groups.json")); groupEdit?.Invoke(group);
                documents.Add("network-utility-groups.json", Text(group));
            }
            return Colony.Factory(FrameworkProofContent.Read("resource-recovery.json"), FrameworkProofContent.Read("endpoints-flows.json"),
                FrameworkProofContent.Read("network-transfers.json"), additionalDocuments: documents);
        }
        private static NetworkService Lines(SimGame game) => game.World.Services.Providers<NetworkService>().Single();
        private static ResourceId Steel(SimGame game) => game.World.Content.ResourceId("steel");
        private static NetworkService.NetComponent Pool(SimGame game) => Lines(game).Component(Network, 0);
        private static void Tick(SimGame game) { game.Update(); if (game.Fault != null) throw new InvalidOperationException("solver game step " + game.Tick + ": " + game.Fault); }
        private static void Advance(SimGame game, int updates, SimGame twin = null)
        { for (int i = 0; i < updates; i++) { Tick(game); if (twin != null) Tick(twin); } }
        private static SimGame Setup(GameFactory factory, out Entity source, out Entity first, out Entity second)
        {
            SimGame game = ResourceDispositionTests.New(factory);
            source = ResourceDispositionTests.Spawn(game, "arbitration.source", 9, 28);
            first = ResourceDispositionTests.Spawn(game, "arbitration.receiver", 11, 28);
            second = ResourceDispositionTests.Spawn(game, "arbitration.proof-receiver", 10, 27);
            if (!((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("network.pipe"), new TileCoord(10, 28), new PlayerId(0))) throw new InvalidOperationException("fixture line refused");
            return game;
        }
        private static Fix Amount(SimGame game, EntityId entity) => game.World.Entities.Get(entity).Get<IInventory>().Amount(Steel(game));
        private static SolverProbeDef Probe(SimGame game) => game.World.Content.Get<SolverProbeDef>("solver.probe");
        private static void Conserved(Check c, SimGame game, EntityId source, EntityId first, EntityId second)
            => c.Equal(Fix.FromInt(30), Amount(game, source) + Amount(game, first) + Amount(game, second) + Pool(game).Amount(Steel(game)), "actual native stock conserved across solver choices");
        private static void Faulted(Check c, SimGame game, string reason)
        { c.That(game.Fault != null && game.Fault.Phase == "sim.lines", reason); c.Throws<InvalidOperationException>(() => game.Save(), "provider contract violation cannot checkpoint healthy"); }
        private static void Valve(Check c, SimGame game, EntityId source, bool enabled)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = source, Operation = new Code("valve"), Enabled = enabled, Target = Fix.One }).Accepted, "ordinary engine valve command accepted");
        private static GameFactory UtilityFactory(Action<JsonValue> edit = null, Action<JsonValue> utilityEdit = null)
            => Factory(utilities: true, edit: root =>
            { root.Get("networks")[0].Set("demand_policy", JsonValue.Of("utility.next")); edit?.Invoke(root); }, utilityEdit: utilityEdit);
        private static SimGame UtilitySetup(GameFactory factory, out Entity source, out Entity native, out Entity independent)
        {
            SimGame game = ResourceDispositionTests.New(factory);
            source = ResourceDispositionTests.Spawn(game, "arbitration.source", 9, 28);
            native = ResourceDispositionTests.Spawn(game, "utility.native", 11, 28);
            independent = ResourceDispositionTests.Spawn(game, "utility.independent", 10, 27);
            if (!((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("network.pipe"), new TileCoord(10, 28), new PlayerId(0)))
                throw new InvalidOperationException("utility fixture line refused");
            return game;
        }
        private static UtilityDemandSnapshot Demand(SimGame game, EntityId entity)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(entity, new Code("meter"), out var facts))
                throw new InvalidOperationException("utility owner missing"); return facts;
        }
        private static void StartUtility(Check c, SimGame game, EntityId entity)
            => c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand
                { Transformer = entity, Capability = new Code("meter"), Transformation = "utility.consume", Repeat = false }).Accepted,
                "normal player enqueue starts native timed utility consumption");
        private static GameFactory GroupFactory(bool motion = false, Action<JsonValue> edit = null, Action<JsonValue> groupEdit = null)
            => Factory(utilities: true, groups: true, edit: root =>
            { root.Get("networks")[0].Set("demand_policy", JsonValue.Of(motion ? "utility.group.motion" : "utility.group.process")); edit?.Invoke(root); }, groupEdit: groupEdit);
        private static SimGame GroupSetup(GameFactory factory, string prototype, out Entity source, out Entity consumer)
        {
            SimGame game = ResourceDispositionTests.New(factory);
            source = ResourceDispositionTests.Spawn(game, "arbitration.source", 9, 28);
            consumer = ResourceDispositionTests.Spawn(game, prototype, 11, 28);
            if (!((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("network.pipe"), new TileCoord(10, 28), new PlayerId(0)))
                throw new InvalidOperationException("group fixture line refused");
            return game;
        }
        private static NetworkDemandSnapshot GroupDemand(SimGame game, EntityId entity)
        {
            Code refusal = game.Seat(0).View.Feature<InfrastructureView>().ReadDemand(Network, entity, Steel(game), true, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("group demand diagnostics refused " + refusal);
            return facts;
        }
        private static UtilityDemandSnapshot Member(SimGame game, EntityId entity, string capability)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(entity, new Code(capability), out var facts))
                throw new InvalidOperationException("group member missing " + capability);
            return facts;
        }
        private static void GroupContracts(Check c)
        {
            foreach (string invalid in new[] { "missing", "empty", "scalar", "duplicate", "empty-id", "invalid-id", "old", "mixed-old" })
                c.Throws<ContentException>(() => ResourceDispositionTests.New(GroupFactory(groupEdit: root =>
                {
                    JsonValue policy = root.Get("utility_network_demands")[1];
                    switch (invalid)
                    {
                        case "missing": policy.Remove("consumers"); break;
                        case "empty": policy.Set("consumers", JsonValue.NewArray()); break;
                        case "scalar": policy.Set("consumers", JsonValue.Of("meter")); break;
                        case "duplicate": policy.Set("consumers", Json.Parse("[\"meter\",\"meter\"]")); break;
                        case "empty-id": policy.Set("consumers", Json.Parse("[\"\"]")); break;
                        case "invalid-id": policy.Set("consumers", Json.Parse("[\"not valid\"]")); break;
                        case "old": policy.Remove("consumers"); policy.Set("consumer", JsonValue.Of("meter")); break;
                        case "mixed-old": policy.Set("consumer", JsonValue.Of("meter")); break;
                    }
                })), "strict explicit utility group refuses " + invalid);
            GameFactory factory = GroupFactory(); SimGame game = GroupSetup(factory, "utility.group.independent", out Entity source, out Entity consumer); Tick(game);
            var definition = (UtilityNetworkDemandPolicyDef)game.World.Content.Get<INetworkDefinition>(Network).DemandPolicy;
            c.Equal(new Code("auxiliary"), definition.Consumers[0], "group declaration uses stable canonical named ordering");
            c.Throws<NotSupportedException>(() => ((IList<Code>)definition.Consumers).Clear(), "authored group bindings are immutable");
            c.Equal(Fix.FromInt(3), GroupDemand(game, consumer.Id).Maximum, "four-unit group obligation subtracts shared one-unit stock exactly once");
            c.Equal(Fix.One, Member(game, consumer.Id, "meter").Available.AmountOf(Steel(game)), "first consumer observes the same real stock");
            c.Equal(Fix.One, Member(game, consumer.Id, "auxiliary").Available.AmountOf(Steel(game)), "second capped contribution is not a second stock balance");
            ulong before = game.Hash(); GroupDemand(game, consumer.Id); c.Equal(before, game.Hash(), "group preview does not reserve stock or advance time");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<InfrastructureView>().ReadDemand(Network, consumer.Id, Steel(game), true, out _), "group readiness remains company-private");
            var holder = new ReservationOwner("proof.utility-group", consumer.Id.Value); var promise = consumer.Get<IInventory>().ReserveOut(holder, Steel(game), Fix.One);
            c.Equal(Fix.FromInt(4), GroupDemand(game, consumer.Id).Maximum, "shared outgoing promise protects stock once for the whole group");
            consumer.Get<IInventory>().ReleaseOut(holder, promise);

            factory = GroupFactory(groupEdit: root => root.Get("entities")[1].Get("components").Get("proof_utility_consumer")[1].Set("inputs", Json.Parse("{\"water\":2}")));
            game = GroupSetup(factory, "utility.group.independent", out source, out consumer); Tick(game);
            c.Equal(Fix.One, GroupDemand(game, consumer.Id).Maximum, "heterogeneous member obligations retain their native resource dimensions");
            factory = GroupFactory(groupEdit: root =>
            {
                foreach (JsonValue row in new[] { root.Get("entities")[1].Get("components").Get("proof_utility_consumer")[0], root.Get("entities")[1].Get("components").Get("proof_utility_consumer")[1] })
                    row.Set("inputs", Json.Parse("{\"water\":2}"));
            });
            game = GroupSetup(factory, "utility.group.independent", out source, out consumer); Tick(game);
            c.Equal(ResourceSignalCodes.Incompatible, GroupDemand(game, consumer.Id).Blocker, "active group with no requested resource is incompatible, not invented demand");
            factory = GroupFactory(groupEdit: root =>
            {
                foreach (JsonValue row in new[] { root.Get("entities")[1].Get("components").Get("proof_utility_consumer")[0], root.Get("entities")[1].Get("components").Get("proof_utility_consumer")[1] })
                    row.Set("inputs", Json.Parse("{\"steel\":2000000000}"));
            });
            game = GroupSetup(factory, "utility.group.independent", out source, out consumer); Tick(game);
            c.Equal(UtilityNetworkDemandPolicyDef.Range, GroupDemand(game, consumer.Id).Blocker, "unrepresentable aggregate refuses with a typed range blocker");
            c.Equal(Fix.Zero, GroupDemand(game, consumer.Id).Maximum, "overflow never saturates or partially allocates group intent");

            factory = GroupFactory(); game = GroupSetup(factory, "utility.group.processors", out source, out consumer); Tick(game);
            c.Equal(UtilityCodes.Inactive, GroupDemand(game, consumer.Id).Blocker, "all idle native members preserve typed inactive demand");
            foreach (string capability in new[] { "meter", "auxiliary" })
                c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = consumer.Id, Capability = new Code(capability), Transformation = "utility.consume", Repeat = false }).Accepted,
                    "normal command starts native group member " + capability);
            foreach (string capability in new[] { "meter", "auxiliary" })
            {
                c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = consumer.Id, Operation = new Code(capability), Enabled = false, Target = Fix.One }).Accepted,
                    "normal command disables actual group member " + capability);
                if (capability == "meter") c.Equal(Code.None, GroupDemand(game, consumer.Id).Blocker,
                    "one disabled member does not hide another active native obligation");
            }
            c.Equal(UtilityCodes.Inactive, GroupDemand(game, consumer.Id).Blocker, "disabled members do not create network obligation");
            Fix disabledStock = Amount(game, consumer.Id); Advance(game, 3); c.Equal(disabledStock, Amount(game, consumer.Id), "all-disabled physical consumers receive no extra stock");
        }
        private static void GroupPhysical(Check c)
        {
            GameFactory factory = GroupFactory(); SimGame game = GroupSetup(factory, "utility.group.independent", out Entity source, out Entity consumer); Tick(game);
            var holder = new ReservationOwner("proof.utility-group", consumer.Id.Value); var promise = consumer.Get<IInventory>().ReserveIn(holder, Steel(game), Fix.FromInt(2));
            c.That(promise.IsValid, "actual shared inventory owns a two-unit incoming group promise");
            c.Equal(Fix.One, GroupDemand(game, consumer.Id).Maximum, "aggregate demand subtracts the shared incoming promise once");
            SimGame twin = factory.Load(game.Save()); Advance(game, 3, twin);
            c.Equal(Fix.FromInt(2), Amount(game, consumer.Id), "physical network fills only the deficit not covered by inbound commitments");
            c.Equal(Fix.FromInt(2), consumer.Get<IInventory>().ReservedIn(Steel(game)), "network does not consume another owner's incoming promise");
            c.Equal(game.Hash(), twin.Hash(), "saved group promises and actual flow continue identically");
            consumer.Get<IInventory>().ReleaseIn(holder, promise); twin.World.Entities.Get(consumer.Id).Get<IInventory>().ReleaseIn(holder, promise); Advance(game, 2, twin);
            c.Equal(Fix.FromInt(4), Amount(game, consumer.Id), "promise release admits exactly the remaining aggregate deficit");
            c.Equal(Fix.FromInt(31), Amount(game, source.Id) + Amount(game, consumer.Id) + Pool(game).Amount(Steel(game)), "group filling conserves finite source and actual initial stock");
            c.Equal(game.Hash(), twin.Hash(), "group fulfillment and saved continuation agree");
            long time = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); c.Equal(0, game.Update(), "paused sole clock runs no utility allocation steps");
            c.Equal(time, game.Clock.StepIndex, "group demand has no independently advancing clock"); c.Equal(hash, game.Hash(), "pause changes no physical group stock/history"); game.Clock.Resume();

            factory = GroupFactory(motion: true, groupEdit: root => root.Get("entities")[0].Get("components").Get("operating_inputs")[1].Get("inputs")[0].Get("period").Set("numerator", JsonValue.Number("2")));
            game = GroupSetup(factory, "utility.group.motion", out source, out consumer); Tick(game);
            c.That(Member(game, consumer.Id, "propulsion").Satisfied && Member(game, consumer.Id, "auxiliary").Satisfied,
                "one shared unit appears individually ready before aggregate physical admission");
            c.Equal(OperatingInputCodes.Inputs, OperatingInputsContracts.Read(game.World, consumer, OperatingPhase.Movement).Blocker, "actual combined motion needs two units, not two uses of one unit");
            c.Equal(Fix.One, GroupDemand(game, consumer.Id).Maximum, "authored motion group requests the missing combined unit");
            twin = factory.Load(game.Save());
            foreach (SimGame session in new[] { game, twin })
                c.That(TestKit.Order(session, 0, new MoveVehicleCommand { Vehicle = consumer.Id, To = new TileCoord(12, 28) }).Accepted,
                    "ordinary engine command moves a network-supplied native machine");
            c.Equal(0L, Member(game, consumer.Id, "auxiliary").ElapsedTicks, "later network delivery cannot retroactively fund earlier movement in the same game update");
            c.Equal(Fix.FromInt(2), Amount(game, consumer.Id), "network receipt supplies real stock for the next physical movement phase");
            for (int i = 0; i < 40 && Member(game, consumer.Id, "auxiliary").ElapsedTicks == 0; i++) Advance(game, 1, twin);
            if (Member(game, consumer.Id, "auxiliary").ElapsedTicks == 0) throw new InvalidOperationException("group first funded movement timeout at game step " + game.Clock.StepIndex);
            c.That(Member(game, consumer.Id, "auxiliary").NextInputs.IsEmpty, "group preserves the already-paid slower provider's interval");
            c.Equal(Fix.One, Member(game, consumer.Id, "propulsion").NextInputs.AmountOf(Steel(game)), "faster group member retains its own real next draw");
            for (int i = 0; i < 40 && consumer.Get<IMotion>().Status != MoveStatus.Arrived; i++) Advance(game, 1, twin);
            if (consumer.Get<IMotion>().Status != MoveStatus.Arrived) throw new InvalidOperationException("group motion timeout: game step=" + game.Clock.StepIndex + " tile=" + consumer.Get<ITilePresence>().Tile + " blocker=" + OperatingInputsContracts.Read(game.World, consumer, OperatingPhase.Movement).Blocker);
            c.Equal(new TileCoord(12, 28), consumer.Get<ITilePresence>().Tile, "group-supplied motion reaches its real commanded destination");
            var propulsion = Member(game, consumer.Id, "propulsion"); var auxiliary = Member(game, consumer.Id, "auxiliary");
            c.That(propulsion.ElapsedTicks > 0 && propulsion.ElapsedTicks == auxiliary.ElapsedTicks, "both native input providers earn only actual shared motion attempts");
            c.Equal(Fix.FromInt(31), Amount(game, source.Id) + Amount(game, consumer.Id) + Pool(game).Amount(Steel(game)) +
                propulsion.Consumed.AmountOf(Steel(game)) + auxiliary.Consumed.AmountOf(Steel(game)), "native grouped physical burn conserves actual delivered stock");
            c.Equal(game.Hash(), twin.Hash(), "group-fed native motion/save continuation has identical state");

            factory = GroupFactory(edit: root =>
            {
                JsonValue sibling = root.Get("networks")[0].Clone(); sibling.Set("id", JsonValue.Of("arbitration.sibling")); root.Get("networks").Add(sibling);
                root.Get("entities")[0].Get("components").Get("ports").Get("list").Add(Json.Parse("{\"network\":\"arbitration.sibling\",\"push\":[\"steel\"]}"));
            }, groupEdit: root => root.Get("entities")[1].Get("components").Get("proof_network_ports").Get("list").Add(Json.Parse("{\"network\":\"arbitration.sibling\",\"pull\":[\"steel\"]}")));
            game = GroupSetup(factory, "utility.group.independent", out source, out consumer); Tick(game); twin = factory.Load(game.Save()); Advance(game, 4, twin);
            c.Equal(Fix.FromInt(4), Amount(game, consumer.Id), "two networks late-recheck the same aggregate and cannot count shared stock twice");
            c.Equal(Fix.FromInt(31), Amount(game, source.Id) + Amount(game, consumer.Id) + Pool(game).Amount(Steel(game)) + Lines(game).Component("arbitration.sibling", 0).Amount(Steel(game)), "multi-network aggregate conserves both real pools and source");
            c.Equal(game.Hash(), twin.Hash(), "shared group multi-network saved flow is deterministic");

            factory = GroupFactory(groupEdit: root => root.Get("utility_network_demands")[1].Set("consumers", Json.Parse("[\"meter\",\"missing\"]")));
            game = GroupSetup(factory, "utility.group.independent", out source, out consumer); Advance(game, 3);
            c.Equal(Codes.NotFound, GroupDemand(game, consumer.Id).Blocker, "missing selected group member disables the complete obligation");
            c.Equal(Fix.One, Amount(game, consumer.Id), "missing member never falls back to single-consumer or capacity filling");
            factory = GroupFactory(); game = GroupSetup(factory, "utility.group.independent", out source, out consumer); Tick(game);
            consumer.Get<ProofUtilityConsumer>(new Code("auxiliary")).Fault = 3; game.Update(); Faulted(c, game, "malformed group member faults before physical aggregate transfer");
            c.Equal(Fix.One, Amount(game, consumer.Id), "invalid grouped provider cannot authorize partial receiving stock");
        }
        private static void DemandContracts(Check c)
        {
            GroupContracts(c);
            c.Throws<ArgumentException>(() => new NetworkDemandSnapshot(-Fix.One, Code.None), "negative demand is invalid");
            c.Throws<ArgumentException>(() => new NetworkDemandSnapshot(Fix.One, Codes.NotFound), "blocked demand cannot admit stock");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                root => root.Get("networks")[0].Remove("demand_policy"),
                root => root.Get("networks")[0].Set("demand_policy", JsonValue.Of("missing")),
                root => root.Get("networks")[0].Set("demand_policy", JsonValue.Of("arbitration.fair")) })
                c.Throws<ContentException>(() => ResourceDispositionTests.New(Factory(edit: edit)), "explicit demand policy is mandatory and type checked");
            c.Throws<ContentException>(() => ResourceDispositionTests.New(UtilityFactory(utilityEdit: root =>
                root.Get("utility_network_demands")[0].Remove("consumers"))), "utility demand requires explicit named consumers group");
            GameFactory factory = Factory(utilities: true, edit: root => root.Get("networks")[0].Set("demand_policy", JsonValue.Of("utility.probe")));
            SimGame game = Setup(factory, out Entity source, out Entity first, out Entity second);
            NetworkDemandContext retained = null;
            game.World.Content.Get<NetworkDemandProbeDef>("utility.probe").ObservationProbe = context =>
            {
                retained = context;
                if (!context.Pull) c.Equal(Codes.InvalidArgument, context.ReadUtility(new Code("meter"), out _, out _), "push observation cannot pretend to be a receiving consumer");
                return new NetworkDemandSnapshot(context.Readiness, Code.None);
            };
            Tick(game);
            c.Equal(Network, retained.Network, "public-only demand policy receives actual network identity");
            c.Equal(source.Id, retained.Entity, "demand context exposes detached actual source ID");
            c.That(!retained.Pull, "first available opportunity is physical source push");
            c.Throws<InvalidOperationException>(() => { var ignored = retained.Readiness; }, "demand readiness expires after read");
            c.Throws<InvalidOperationException>(() => { var ignored = retained.Quantum; }, "demand quantum expires after read");
            c.Throws<InvalidOperationException>(() => retained.ReadUtility(new Code("meter"), out _, out _), "retained demand observation cannot read later state");
            foreach (int fault in new[] { 1, 2, 3, 4, 5 })
            {
                game = Setup(factory, out source, out first, out second); Entity changed = fault == 4 ? first : source;
                NetworkDemandContext failed = null;
                game.World.Content.Get<NetworkDemandProbeDef>("utility.probe").ObservationProbe = context =>
                {
                    failed = context;
                    if (fault == 1) return new NetworkDemandSnapshot(context.Readiness + Fix.One, Code.None);
                    if (fault == 2) return new NetworkDemandSnapshot(Fix.FromRaw(1), Code.None);
                    if (fault == 5) throw new InvalidOperationException("injected demand fault");
                    changed.Touch(); return new NetworkDemandSnapshot(context.Readiness, Code.None);
                };
                game.Update(); Faulted(c, game, "invalid or mutating demand provider faults existing flow owner");
                c.Throws<InvalidOperationException>(() => { var ignored = failed.Readiness; }, "faulted demand context closes in finally");
            }
        }
        private static void UtilityPhysical(Check c)
        {
            GameFactory factory = UtilityFactory(); SimGame game = UtilitySetup(factory, out Entity source, out Entity native, out Entity independent);
            EntityId s = source.Id, n = native.Id, p = independent.Id;
            c.That(game.World.Content.Get<INetworkDefinition>(Network).DemandPolicy is UtilityNetworkDemandPolicyDef, "authored utility policy binds through open admission contract");
            StartUtility(c, game, n); Advance(game, 6);
            c.Equal(Fix.FromInt(2), Amount(game, p), "independent public-contract demand receives exactly requested actual stock");
            var infrastructure = game.Seat(0).View.Feature<InfrastructureView>(); ulong observed = game.Hash();
            c.Equal(Code.None, infrastructure.ReadDemand(Network, p, Steel(game), true, out var admitted), "private engine view reads same actual admission policy");
            c.Equal(Fix.Zero, admitted.Maximum, "satisfied utility view reports zero additional allocation");
            c.Equal(Code.None, admitted.Blocker, "satisfied zero allocation is not an unknown blocker");
            c.Equal(observed, game.Hash(), "demand diagnostics earn no progress and mutate no stock/state");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<InfrastructureView>().ReadDemand(Network, p, Steel(game), true, out _), "foreign company cannot inspect private allocation");
            c.Equal(Fix.One, Amount(game, n), "native network consumer retains next actual draw, not storage capacity");
            c.That(Demand(game, n).Consumed.AmountOf(Steel(game)).IsPositive, "native engine burns delivered utility over game time");
            c.Equal(Fix.FromInt(30), Amount(game, s) + Amount(game, n) + Amount(game, p) + Pool(game).Amount(Steel(game)) +
                Demand(game, n).Consumed.AmountOf(Steel(game)), "source, pools, endpoints and actual consumption conserve finite native stock");
            SimGame twin = factory.Load(game.Save()); Advance(game, 4, twin);
            c.Equal(game.Hash(), twin.Hash(), "saved utility allocation and native consumption continue identically");
            long now = game.Tick; ulong hash = game.Hash(); game.Clock.Pause(); game.Update(); game.Update();
            c.Equal(now, game.Tick, "sole paused game clock stops consumption and transfer"); c.Equal(hash, game.Hash(), "demand has no independent time store"); game.Clock.Resume();
            c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = n, Operation = new Code("meter"), Enabled = false, Target = Fix.One }).Accepted,
                "player command disables actual utility operation");
            Fix before = Amount(game, n), consumed = Demand(game, n).Consumed.AmountOf(Steel(game)); Advance(game, 4);
            c.Equal(before, Amount(game, n), "disabled native utility refuses further delivery");
            c.Equal(consumed, Demand(game, n).Consumed.AmountOf(Steel(game)), "disabled native utility earns no consumption");
            c.Equal(Code.None, infrastructure.ReadDemand(Network, n, Steel(game), true, out admitted), "disabled allocation remains observable");
            c.Equal(UtilityCodes.Inactive, admitted.Blocker, "view returns the same typed inactive refusal as allocation");

            game = UtilitySetup(factory, out source, out native, out independent); p = independent.Id;
            var holder = new ReservationOwner("proof.utility", p.Value);
            ReservationToken promise = independent.Get<IInventory>().ReserveIn(holder, Steel(game), Fix.FromInt(2));
            c.That(promise.IsValid, "existing inventory owner reserves actual incoming utility"); Advance(game, 4);
            c.Equal(Fix.Zero, Amount(game, p), "network does not duplicate already promised utility supply");
            independent.Get<IInventory>().ReleaseIn(holder, promise);
            c.Equal(Fix.Zero, independent.Get<IInventory>().ReservedIn(Steel(game)), "same reservation owner releases utility promise");
            Advance(game, 1); c.Equal(Fix.FromInt(2), Amount(game, p), "released promise admits actual next delivery without banked credit");
            game = UtilitySetup(factory, out source, out native, out independent); p = independent.Id;
            holder = new ReservationOwner("proof.utility", p.Value);
            promise = independent.Get<IInventory>().ReserveIn(holder, Steel(game), Fix.One); Advance(game, 4);
            c.Equal(Fix.One, Amount(game, p), "partial inbound promise admits only the remaining unpromised utility quantity");
            c.Equal(Fix.One, independent.Get<IInventory>().ReservedIn(Steel(game)), "network transfer preserves another actual owner's inbound commitment");
            independent.Get<IInventory>().ReleaseIn(holder, promise); Advance(game, 1);
            c.Equal(Fix.FromInt(2), Amount(game, p), "released partial promise admits only the new one-unit deficit");

            factory = UtilityFactory(utilityEdit: root => root.Get("utility_network_demands")[0].Set("consumers", Json.Parse("[\"missing\"]")));
            game = UtilitySetup(factory, out source, out native, out independent); Advance(game, 4);
            c.Equal(Fix.Zero, Amount(game, independent.Id), "missing named consumer never falls back to storage filling");
            c.That(game.Fault == null, "known missing consumer is typed refusal, not malformed provider");
            c.Equal(Code.None, game.Seat(0).View.Feature<InfrastructureView>().ReadDemand(Network, independent.Id, Steel(game), true, out admitted), "known refusal remains public engine diagnostic");
            c.Equal(Codes.NotFound, admitted.Blocker, "missing named provider has explicit typed blocker, not silent storage fill");

            factory = UtilityFactory(); game = UtilitySetup(factory, out source, out native, out independent);
            StartUtility(c, game, native.Id); Advance(game, 20); Fix completedStock = Amount(game, native.Id);
            c.That(!Demand(game, native.Id).Drawing, "finished native work withdraws utility demand through its existing execution owner");
            twin = factory.Load(game.Save()); Advance(game, 5, twin);
            c.Equal(completedStock, Amount(game, native.Id), "completed process cannot keep filling storage from network");
            c.Equal(game.Hash(), twin.Hash(), "completed utility demand and resource output retain saved identity");
            game = UtilitySetup(factory, out source, out native, out independent); Tick(game);
            independent.Get<ProofUtilityConsumer>().Fault = 3; game.Update();
            Faulted(c, game, "allocation validates actual named public consumer identity, never trusts forged demand");

            factory = UtilityFactory(edit: root => {
                JsonValue sibling = Json.Parse(Text(root.Get("networks")[0])); sibling.Set("id", JsonValue.Of("arbitration.sibling")); root.Get("networks").Add(sibling);
                root.Get("entities")[0].Get("components").Get("ports").Get("list").Add(Json.Parse("{\"network\":\"arbitration.sibling\",\"push\":[\"steel\"]}"));
            }, utilityEdit: root => root.Get("entities")[1].Get("components").Get("proof_network_ports").Get("list").Add(
                Json.Parse("{\"network\":\"arbitration.sibling\",\"pull\":[\"steel\"]}")));
            game = UtilitySetup(factory, out source, out native, out independent); Tick(game); twin = factory.Load(game.Save()); Advance(game, 1, twin);
            c.Equal(Fix.FromInt(2), Amount(game, independent.Id), "two networks recheck shared demand before receipts and cannot double deliver");
            c.Equal(Fix.FromInt(8), Lines(game).Component("arbitration.sibling", 0).Amount(Steel(game)), "second network retains two actual source pushes refused by already satisfied consumer");
            Advance(game, 3, twin); c.Equal(game.Hash(), twin.Hash(), "shared-consumer multi-network demand and saved history remain deterministic");
        }
        private static void Contracts(Check c)
        {
            DemandContracts(c);
            GameFactory factory = Factory(); SimGame game = Setup(factory, out Entity source, out Entity first, out Entity second);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current exact build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "unchanged current payload format");
            c.Equal(4, Lines(game).StateSchemaVersion, "solver creates no additional persistent state");
            c.That(game.World.Content.Get<INetworkDefinition>(Network).Solver is ResourceTransferNetworkSolverDef, "explicit native JSON solver resolves through the contract");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                root => root.Get("networks")[0].Remove("solver"),
                root => root.Get("networks")[0].Set("solver", JsonValue.Of("missing")),
                root => root.Get("networks")[0].Set("solver", JsonValue.Of("arbitration.fair")),
                root => root.Get("resource_transfer_network_solvers")[0].Set("strategy", JsonValue.Of("old")) })
                c.Throws<ContentException>(() => ResourceDispositionTests.New(Factory(edit: edit)), "missing/wrong/obsolete solver refuses before execution");
            c.Throws<ContentException>(() => ResourceDispositionTests.New(Factory("solver.limited", providersEdit: root =>
                root.Get("limited_network_solvers")[0].Get("interval").Set("unit", JsonValue.Of("second")))), "provider interval validates against the actual session quantum before Update");

            factory = Factory("solver.probe"); game = Setup(factory, out source, out first, out second);
            NetworkExecutionContext retained = null; Probe(game).ExecutionProbe = context => retained = context;
            Tick(game); c.Equal(Network, retained.Network, "context names its actual selected network");
            c.Equal(game.Tick, retained.CompletedAt, "context timestamp is the sole game clock's completed step");
            c.That(ReferenceEquals(game.SimulationTime, retained.Time), "solver reads the one immutable time converter, not another clock");
            c.Equal(1, retained.Candidates.Count, "only actual due source leg is exposed");
            var request = retained.Candidates[0]; c.Equal(source.Id, request.Candidate.Entity, "detached request names actual source");
            c.Equal(ResourceEndpointId.ForEntity(source.Id), request.Source, "request identifies real entity stock owner");
            c.Equal(Pool(game).Endpoint, request.Destination, "request identifies real existing pool owner");
            c.Equal(Fix.FromInt(4), request.Candidate.Requested, "request cannot claim more than actually earned allowance");
            c.Throws<NotSupportedException>(() => ((IList<NetworkFlowRequest>)retained.Candidates).Clear(), "provider requests are immutable snapshots");
            ulong hash = game.Hash(); c.Throws<InvalidOperationException>(() => retained.Transfer(request, Fix.One), "expired execution capability cannot move stock");
            c.Equal(hash, game.Hash(), "expired attempt changes no live game state");
            c.Equal(hash, factory.Load(game.Save()).Hash(), "contexts are not persistent queues or saved provider state");

            foreach (Fix invalid in new[] { Fix.Zero, -Fix.One, Fix.FromInt(5), Fix.FromRatio(1, 2) })
            {
                game = Setup(Factory("solver.probe"), out source, out first, out second);
                Probe(game).ExecutionProbe = context => context.Transfer(context.Candidates[0], invalid);
                game.Update(); Faulted(c, game, "invalid/over-budget/fractional request faults engine execution");
                c.Equal(Fix.FromInt(30), Amount(game, source.Id), "invalid request consumes no source stock");
                c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "invalid request creates no destination stock");
            }
            game = Setup(Factory("solver.probe"), out source, out first, out second);
            Probe(game).ExecutionProbe = context => { var due = context.Candidates[0]; context.Transfer(due, Fix.One); context.Transfer(due, Fix.One); };
            game.Update(); Faulted(c, game, "duplicate attempt cannot execute the same captured leg twice");
            c.Equal(Fix.FromInt(29), Amount(game, source.Id), "only first verified transfer spent actual stock");
            c.Equal(Fix.One, Pool(game).Amount(Steel(game)), "no compensation or duplicate delivery after provider fault");

            game = Setup(Factory("solver.probe"), out source, out first, out second);
            Probe(game).ExecutionProbe = context => { source.Touch(); context.Transfer(context.Candidates[0], Fix.One); };
            game.Update(); Faulted(c, game, "provider mutation before receipt execution is caught");
            c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "before-transfer mutation permits no partial receipt");
            game = Setup(Factory("solver.probe"), out source, out first, out second);
            Probe(game).ExecutionProbe = context => { context.Transfer(context.Candidates[0], Fix.One); source.Touch(); };
            game.Update(); Faulted(c, game, "provider mutation after legitimate transfer is caught on return");
            c.Equal(Fix.One, Pool(game).Amount(Steel(game)), "actual committed stock is not fabricated rollback");

            factory = Factory("solver.probe", root =>
            {
                JsonValue sibling = Json.Parse(Text(root.Get("networks")[0])); sibling.Set("id", JsonValue.Of("arbitration.sibling")); root.Get("networks").Add(sibling);
                root.Get("entities")[0].Get("components").Get("ports").Get("list").Add(Json.Parse("{\"network\":\"arbitration.sibling\",\"push\":[\"steel\"]}"));
            });
            game = Setup(factory, out source, out first, out second); NetworkFlowRequest foreign = null;
            Probe(game).ExecutionProbe = context =>
            { if (context.Network == Network) foreign = context.Candidates[0]; else context.Transfer(foreign, Fix.One); };
            game.Update(); Faulted(c, game, "cross-network request cannot execute through another live context");
            c.Equal(Fix.FromInt(30), Amount(game, source.Id), "foreign request does not debit source");
            c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "foreign context cannot credit another pool");

            factory = Factory("solver.probe"); game = Setup(factory, out source, out first, out second);
            Probe(game).ExecutionProbe = context => foreign = context.Candidates[0]; Tick(game); hash = game.Hash();
            SimGame other = Setup(factory, out Entity otherSource, out Entity otherFirst, out Entity otherSecond);
            NetworkExecutionContext failedContext = null;
            Probe(other).ExecutionProbe = context => { failedContext = context; context.Transfer(foreign, Fix.One); };
            other.Update(); Faulted(c, other, "same named network in another session cannot accept foreign request identity");
            c.Equal(hash, game.Hash(), "foreign-session refusal cannot mutate original session");
            c.Throws<InvalidOperationException>(() => failedContext.Transfer(failedContext.Candidates[0], Fix.One), "finally expires context even when provider faults");

            factory = Factory("solver.probe", root =>
            {
                var components = root.Get("entities")[2].Get("components"); components.Remove("storage");
                components.Set("single_resource_port", Json.Parse("{\"resource\":\"steel\",\"capacity_policy\":\"recovery.mixed\",\"initial\":0}"));
            });
            game = Setup(factory, out source, out first, out second);
            Probe(game).ExecutionProbe = context =>
            {
                foreach (var due in context.Candidates)
                {
                    if (due.Candidate.Entity == first.Id)
                        first.Get<SingleResourcePort>().InboundCheckProbe = () => context.Transfer(context.Candidates[context.Candidates.Count - 1], Fix.One);
                    context.Transfer(due, due.Candidate.Requested);
                }
            };
            Tick(game); game.Update(); Faulted(c, game, "provider cannot recursively execute another request during owned acquisition");
            c.That(game.Fault?.Message.Contains("recursively") == true, "recursive guard triggers before another request's stock mutation");
            c.Equal(Fix.Zero, Amount(game, first.Id) + Amount(game, second.Id), "recursive attempt delivers to neither receiver");
            c.Equal(0, Pool(game).Inventory.Reservations.Count, "recursive pure check creates no inbound/outbound receipt debris");

            factory = Factory("solver.probe", root =>
            {
                var components = root.Get("entities")[2].Get("components"); components.Remove("storage");
                components.Set("single_resource_port", Json.Parse("{\"resource\":\"steel\",\"capacity_policy\":\"recovery.mixed\",\"initial\":0,\"receipt_fault\":\"refuse_in\"}"));
            });
            game = Setup(factory, out source, out first, out second);
            Probe(game).ExecutionProbe = context => { foreach (var due in context.Candidates) context.Transfer(due, due.Candidate.Requested); };
            Tick(game); first.Get<SingleResourcePort>().InboundCheckProbe = () => second.Touch();
            game.Update(); Faulted(c, game, "refused attempt cannot silently refresh an unrelated mutated participant baseline");
            c.Equal(Fix.Zero, Amount(game, first.Id) + Amount(game, second.Id), "mutating refused callback authorizes no receiving transfer");
        }
        private static void Physical(Check c)
        {
            GroupPhysical(c);
            UtilityPhysical(c);
            GameFactory factory = Factory(); SimGame game = Setup(factory, out Entity source, out Entity first, out Entity second);
            EntityId s = source.Id, a = first.Id, b = second.Id;
            Tick(game); c.Equal(Fix.FromInt(26), Amount(game, s), "native selected solver executes actual four-unit due push");
            c.Equal(Fix.FromInt(4), Pool(game).Amount(Steel(game)), "actual pool receives only physical push");
            c.Equal(Fix.Zero, Amount(game, a), "new pool stock does not authorize a previously unready receiving leg");
            SimGame twin = factory.Load(game.Save()); Advance(game, 1, twin);
            c.Equal(Fix.FromInt(4), Amount(game, a), "native solver uses captured ranked due receiving request");
            c.Equal(Fix.FromInt(4), Amount(game, b), "independent port receives through same solver contract");
            c.Equal(game.Hash(), twin.Hash(), "native solver continuation matches saved twin");
            Advance(game, 4, twin); c.Equal(Fix.FromInt(6), Amount(game, s), "six real updates spend only twenty-four source units");
            Conserved(c, game, s, a, b); c.Equal(game.Hash(), twin.Hash(), "continued native sessions remain isolated and identical");
            long now = game.Tick; ulong pausedHash = game.Hash(); game.Clock.Pause(); game.Update(); game.Update();
            c.Equal(now, game.Tick, "one paused game clock stops solver execution"); c.Equal(pausedHash, game.Hash(), "paused solver has no independent time/progress"); game.Clock.Resume();

            factory = Factory("solver.limited"); game = Setup(factory, out source, out first, out second); s = source.Id; a = first.Id; b = second.Id;
            c.That(game.World.Content.Get<INetworkDefinition>(Network).Solver is LimitedNetworkSolverDef, "public-only independent JSON solver needs no coordinator edit");
            Tick(game); twin = factory.Load(game.Save()); Advance(game, 1, twin);
            c.Equal(Fix.FromInt(28), Amount(game, s), "independent solver can bound an actual earned allowance");
            c.Equal(Fix.One, Amount(game, a), "limited native consumer gets actual one-unit request");
            c.Equal(Fix.One, Amount(game, b), "limited independent consumer gets actual one-unit request");
            Advance(game, 4, twin); c.Equal(Fix.FromInt(24), Amount(game, s), "unused whole allowance is not banked into future source debit");
            c.Equal(game.Hash(), twin.Hash(), "independent solver uses existing saved time/history, not its own state"); Conserved(c, game, s, a, b);

            factory = Factory("solver.pulsed"); game = Setup(factory, out source, out first, out second); s = source.Id; a = first.Id; b = second.Id;
            Tick(game); c.Equal(Fix.FromInt(30), Amount(game, s), "authored service interval can skip an Update without moving stock");
            twin = factory.Load(game.Save()); Advance(game, 7, twin);
            c.Equal(Fix.FromInt(26), Amount(game, s), "four allowed opportunities debit four units, never skipped-time catch-up");
            c.Equal(Fix.FromInt(2), Amount(game, a), "pulsed native receiving service is physical");
            c.Equal(Fix.FromInt(2), Amount(game, b), "pulsed independent receiving service is physical");
            c.Equal(game.Hash(), twin.Hash(), "authored interval reads the sole clock consistently after restore"); Conserved(c, game, s, a, b);

            factory = Factory("solver.probe", root =>
            {
                var components = root.Get("entities")[2].Get("components"); components.Remove("storage");
                components.Set("single_resource_port", Json.Parse("{\"resource\":\"steel\",\"capacity_policy\":\"recovery.mixed\",\"initial\":0,\"receipt_fault\":\"refuse_in\"}"));
            });
            game = Setup(factory, out source, out first, out second); s = source.Id; a = first.Id; b = second.Id;
            var results = new List<NetworkFlowAttempt>(); Probe(game).ExecutionProbe = context =>
            { foreach (var due in context.Candidates) results.Add(context.Transfer(due, due.Candidate.Requested)); };
            Advance(game, 2);
            c.That(results.Exists(result => !result.Transferred && result.Amount == Fix.Zero && result.Blocker == NetworkExecutionContext.Unavailable), "known current receipt refusal is a typed zero-transfer result");
            c.Equal(Fix.Zero, Amount(game, a), "refused receiver gets no fabricated stock");
            c.Equal(Fix.FromInt(4), Amount(game, b), "another compatible request still executes through actual receipts");
            Conserved(c, game, s, a, b); c.Equal(0, first.Get<IInventory>().Reservations.Count, "refused attempt leaves no second promise ledger");

            factory = Factory(); game = Setup(factory, out source, out first, out second); s = source.Id; a = first.Id; b = second.Id; Tick(game);
            var pool = Pool(game); var holder = new ReservationOwner("proof.solver-capacity", 1);
            ReservationToken held = pool.Inventory.ReserveIn(holder, Steel(game), Fix.FromInt(6));
            Tick(game); c.Equal(Fix.FromInt(26), Amount(game, s), "real reserved pool capacity blocks native source selection");
            c.Equal(Fix.FromInt(4), Amount(game, a), "receiving can still drain only real unpromised stock");
            c.Equal(Fix.Zero, Amount(game, b), "later ranked request reports empty stock instead of duplicating it");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual blocking receipt and service state save together");
            pool.Inventory.ReleaseIn(holder, held); Pool(twin).Inventory.ReleaseIn(holder, held); Advance(game, 2, twin);
            c.Equal(game.Hash(), twin.Hash(), "released capacity resumes actual saved solver execution"); Conserved(c, game, s, a, b);

            factory = Factory(); game = Setup(factory, out source, out first, out second); s = source.Id; a = first.Id; b = second.Id; Advance(game, 2);
            Valve(c, game, s, false); Advance(game, 8); c.Equal(Fix.FromInt(22), Amount(game, s), "gated network time spends no source resources");
            twin = factory.Load(game.Save()); Valve(c, game, s, true); Valve(c, twin, s, true); Advance(game, 1, twin);
            c.Equal(Fix.FromInt(14), Amount(game, s), "resumed native solver spends only two current earned four-unit allowances");
            c.Equal(game.Hash(), twin.Hash(), "gate-resumed solver twins use the same single clock"); Conserved(c, game, s, a, b);

            factory = Factory(edit: root =>
            {
                root.Get("networks")[0].Get("rates").Add(Json.Parse("{\"resource\":\"water\",\"quantity\":1,\"period\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}}"));
                root.Get("entities")[0].Get("components").Get("storage").Get("initial_stock").Set("water", JsonValue.Number("6"));
                root.Get("entities")[0].Get("components").Get("ports").Get("list")[0].Get("push").Add(JsonValue.Of("water"));
                root.Get("entities")[2].Get("components").Get("ports").Get("list")[0].Get("pull").Add(JsonValue.Of("water"));
                root.Get("entities")[3].Get("components").Get("proof_network_ports").Get("list")[0].Get("pull").Add(JsonValue.Of("water"));
            });
            game = Setup(factory, out source, out first, out second); s = source.Id; a = first.Id; b = second.Id;
            Tick(game); twin = factory.Load(game.Save()); Advance(game, 1, twin);
            ResourceId water = game.World.Content.ResourceId("water");
            c.Equal(Fix.One, first.Get<IInventory>().Amount(water), "same solver executes separately typed fluid leg");
            c.Equal(Fix.One, second.Get<IInventory>().Amount(water), "independent endpoint gets real fluid receipt");
            c.Equal(Fix.FromInt(6), source.Get<IInventory>().Amount(water) + first.Get<IInventory>().Amount(water) + second.Get<IInventory>().Amount(water) + Pool(game).Amount(water), "fluid native quantities remain conserved separately from count units");
            Conserved(c, game, s, a, b); c.Equal(game.Hash(), twin.Hash(), "multi-resource solver/history state roundtrips through the sole codec");

            factory = Factory(edit: root =>
            {
                root.Get("networks")[0].Get("rates")[0].Set("quantity", JsonValue.Number("1"));
                JsonValue sibling = Json.Parse(Text(root.Get("networks")[0])); sibling.Set("id", JsonValue.Of("arbitration.sibling")); root.Get("networks").Add(sibling);
                root.Get("entities")[0].Get("components").Get("ports").Get("list").Add(Json.Parse("{\"network\":\"arbitration.sibling\",\"push\":[\"steel\"]}"));
            });
            game = Setup(factory, out source, out first, out second); Tick(game);
            c.Equal(Fix.FromInt(28), Amount(game, source.Id), "two selected networks debit a shared actual source only through verified legs");
            c.Equal(Fix.One, Pool(game).Amount(Steel(game)), "first context credits its actual graph pool");
            c.Equal(Fix.One, Lines(game).Component("arbitration.sibling", 0).Amount(Steel(game)), "second context credits its separate actual graph pool");
            twin = factory.Load(game.Save()); Advance(game, 2, twin);
            c.Equal(game.Hash(), twin.Hash(), "shared-source multi-network execution and ownership stay deterministic");
        }
    }
}
