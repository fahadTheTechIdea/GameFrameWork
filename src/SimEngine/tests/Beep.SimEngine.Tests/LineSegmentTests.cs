using System.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
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
    // Tests author inputs, submit commands and observe existing engine owners, not construction rules.
    public static partial class LineSegmentTests
    {
        public static void Register()
        {
            TestRunner.Add("Infrastructure plans: authored corridors, templates and admission", c => { Contracts(c); PlanContracts(c); });
            TestRunner.Add("Infrastructure plans: physical segments, controls and saved continuation", c => { Execution(c); PlanExecution(c); });
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(Action<JsonValue> edit = null, bool fast = false)
        {
            JsonValue inputs = Json.Parse(FrameworkProofContent.Read("line-segments.json")); edit?.Invoke(inputs);
            JsonValue content = Json.Parse(FrameworkProofContent.Read("resource-recovery.json"));
            if (fast) for (int i = 0; i < content.Get("entities").Count; i++)
            {
                JsonValue entity = content.Get("entities")[i];
                if (entity.Get("id").Text == "crane") entity.Get("components").Get("worker").Set("work_rate", JsonValue.Number("100"));
            }
            return Colony.Factory(Text(content), FrameworkProofContent.Read("endpoints-flows.json"),
                additionalDocuments: new Dictionary<string, string> { ["line-segments.json"] = Text(inputs) });
        }
        private static SimGame New(GameFactory factory) => ResourceDispositionTests.New(factory);
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static TileMap Map(SimGame game) => (TileMap)game.World.Space;
        private static ResourceId Steel(SimGame game) => game.World.Content.ResourceId("steel");
        private static void Tick(SimGame game) { game.Update(); if (game.Fault != null) throw new InvalidOperationException("line tick " + game.Tick + ": " + game.Fault); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, int limit = 1000)
        {
            for (int i = 0; i < limit && !done(); i++) { Tick(game); if (twin != null) Tick(twin); if (!done() && Plans(game).Exists(p => p.State == PlanState.Failed)) break; }
            if (!done()) { var info = new StringBuilder(stage + " stalled at " + game.Tick);
                foreach (Order order in Board(game).Orders) info.Append(" order ").Append(order.Id).Append(':').Append(order.State).Append('/').Append(order.Blocker);
                foreach (PlanView plan in Plans(game)) info.Append(" plan ").Append(plan.Id).Append(':').Append(plan.Definition).Append('/').Append(plan.State).Append('/').Append(plan.Blocker);
                foreach (Entity entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory inventory && inventory.Reservations.Count > 0)
                {
                    info.Append(" endpoint ").Append(entity.Id).Append(':').Append(entity.Def.Id).Append(" stock=").Append(inventory.Amount(Steel(game)));
                    foreach (var promise in inventory.Reservations) info.Append(" promise ").Append(promise.Direction).Append('/').Append(promise.Amount).Append('/').Append(promise.Owner);
                }
                foreach (var job in Board(game).Jobs) info.Append(" job ").Append(job.Id).Append(':').Append(job.State).Append('/').Append(job.Blocker);
                throw new InvalidOperationException(info.ToString()); }
        }
        private static TileCoord[] Path(int count = 4)
        { var path = new TileCoord[count]; for (int i = 0; i < count; i++) path[i] = new TileCoord(20 + i, 20); return path; }
        private static Order Begin(Check c, SimGame game, string line = "segment.road", int count = 4)
        {
            var command = new LayLineCommand { Line = line }; command.Tiles.AddRange(Path(count));
            c.That(TestKit.Order(game, 0, command).Accepted, "normal public line command admitted");
            return Board(game).Orders[Board(game).Orders.Count - 1];
        }
        private static Entity Target(SimGame game, Order order) => game.World.Entities.Get(order.Target);
        private static BigInteger Consumed(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Totals(Steel(game)).ConsumedRaw;
        private static void Stock(SimGame game, Entity entity, int amount)
        {
            var request = new ResourceInventoryIntent(new ReservationOwner("proof.segment-fixture", entity.Id.Value), entity.Owner, entity.Id,
                new Bill(new[] { new ResourceAmount(Steel(game), Fix.FromInt(amount)) }), ResourceFlowKind.Produced, new Code("proof.authored-test-effect"));
            if (!game.World.Services.Get<IResourceTransaction>().CommitInventory(request).Committed) throw new InvalidOperationException("fixture stock refused");
        }
        private static void Workers(SimGame game)
        { ResourceDispositionTests.Spawn(game, "depot", 4, 20); ResourceDispositionTests.Spawn(game, "truck", 8, 24); ResourceDispositionTests.Spawn(game, "crane", 6, 16); Tick(game); }
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "explicit current build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "only current save identity");
            Order order = Begin(c, game, count: 20); Entity marker = Target(game, order);
            c.Equal(2, marker.Get<LineWork>().Def.StateCodec.SchemaVersion, "line work has one revised codec");
            c.Equal(20, order.Nodes.Count, "corridor larger than one-unit buffer admitted as twenty stages");
            for (int i = 0; i < order.Nodes.Count; i++)
            {
                var node = order.Plan.Graph.Nodes[i];
                c.Equal("segment." + i + ".install", node.Id, "stable segment identity"); c.Equal(Fix.One, node.RequirementMultiplier, "only current tile's bill");
                c.Equal(2L, game.SimulationTime.Ticks(node.Duration), "each ninety-second physical stage earns two minute ticks");
                c.Equal(i == 0 ? 0 : 1, node.Dependencies.Count, "one sequential dependency, not another scheduler");
            }
            IWorkEffectPreview segment = game.World.Content.Get<ILineEffectDefinition>("sim.line.segment"), finish = game.World.Content.Get<ILineEffectDefinition>("sim.line.finish");
            ulong hash = game.Hash();
            c.Equal(WorkEffectCodes.NotComplete, segment.Check(game.World, order, order.Plan.Graph.Nodes[0]), "unearned segment is not ready");
            c.Equal(WorkEffectStatus.Failed, segment.Apply(game.World, order, order.Plan.Graph.Nodes[0]).Status, "direct effect cannot bypass work");
            c.Equal(WorkEffectStatus.Failed, finish.Apply(game.World, order, null).Status, "finish cannot stamp the corridor");
            c.Equal(hash, game.Hash(), "all refused checks/effects leave hash unchanged"); c.Equal((byte)0, Map(game).Layer<ByteLayer>("road")[Path()[0]], "unearned geometry absent");
            SimGame twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "waiting DAG/empty cursor roundtrip");
            foreach (string key in new[] { "activity", "finish_effect", "segment_node" })
                c.Throws<ContentException>(() => New(Factory(root => root.Get("lines")[0].Remove(key))), "missing explicit " + key + " refuses");
            c.Throws<ContentException>(() => New(Factory(root => root.Get("activities")[0].Get("nodes")[0].Set("effect", JsonValue.Of("sim.line.finish")))), "wrong effect operation refuses");
            c.Throws<ContentException>(() => New(Factory(root => root.Get("lines")[0].Set("finish_effect", JsonValue.Of("missing")))), "missing effect identity refuses");
            c.Throws<ContentException>(() => New(Factory(root => root.Get("activities")[0].Get("nodes")[0].Get("duration").Set("numerator", JsonValue.Number("0")))), "zero-time corridor refuses");
            c.Throws<ContentException>(() => New(Factory(root => root.Get("lines")[0].Set("hours_per_tile", JsonValue.Number("1")))), "old scalar duration is not accepted");
            var work = marker.Get<LineWork>(); JsonValue original = work.Path[1]; LineCursorCorruption.Path(work,1,work.Path[0]);
            c.Throws<CorruptStateException>(() => game.Save(), "checkpoint rejects repeated path that differs from admitted intent"); LineCursorCorruption.Path(work,1,original);
            LineCursorCorruption.Path(work,0,TilePlacement.Arguments("recovery.tiles",new TileCoord(19,20)));
            c.Throws<CorruptStateException>(() => game.Save(), "checkpoint rejects cursor/path mismatch before writing"); LineCursorCorruption.Path(work,0,TilePlacement.Arguments("recovery.tiles",Path()[0]));
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "normal cancellation uses existing work owner");
            Reach(game, () => Target(game, order) == null, "cancelled promise cleanup");
            c.That(Target(game, order) == null, "empty cancelled cursor removed");
            SimGame wide = New(Factory(root => root.Get("activities")[0].Get("nodes")[0].Set("duration",
                Json.Parse("{\"numerator\":9223372036854775807,\"denominator\":1,\"unit\":\"minute\"}"))));
            ILineConstruction lines = wide.World.Services.Get<ILineConstruction>(); ILineDefinition definition = wide.World.Content.Get<ILineDefinition>("segment.road");
            hash = wide.Hash(); c.Equal(LineCodes.DurationRange, lines.Check(new PlayerId(0), definition, LineDeclaration.ReadPath(wide.World, definition.Layer.Space.Id, TilePlacement.PathArguments(definition.Layer.Space.Id, Path(2)))), "checked total segment ticks refuse actual overflow");
            c.Throws<ArgumentException>(() => lines.Begin(new PlayerId(0), definition, LineDeclaration.ReadPath(wide.World, definition.Layer.Space.Id, TilePlacement.PathArguments(definition.Layer.Space.Id, Path(2))), 0), "direct entry cannot allocate an invalid duration plan");
            c.Equal(hash, wide.Hash(), "overflow refusal allocates no cursor/order/receipts");
            var overflow = new LayLineCommand { Line = definition.Id }; overflow.Tiles.AddRange(Path(2));
            c.Equal(LineCodes.DurationRange, TestKit.Order(wide, 0, overflow).Refusal, "public command shares exact overflow admission");
        }
        private static void Execution(Check c)
        {
            GameFactory factory = Factory(fast: true); SimGame game = New(factory); Workers(game);
            Order order = Begin(c, game); SimGame twin = factory.Load(game.Save());
            int previous = 0; bool sawPartial = false;
            for (int i = 0; i < 1000 && order.IsActive; i++)
            {
                Tick(game); Tick(twin);
                c.That(order.NodesDone - previous <= 1, "fast worker cannot stamp unvisited future segments in one Update");
                if (order.NodesDone > previous && order.NodesDone < order.Nodes.Count)
                {
                    sawPartial = true; Entity cursor = Target(game, order);
                    c.Equal(Path()[order.NodesDone], cursor.Get<ITilePresence>().Tile, "empty cursor advances one physical stage");
                    c.Equal(0, cursor.Get<IInventory>().Stock.Count, "no actual cargo teleports with cursor");
                    c.That(cursor.Get<IInventory>().Reservations.Count <= 1, "only next stage's one-unit incoming supply can reserve the new cursor");
                    c.Equal((byte)0, Map(game).Layer<ByteLayer>("road")[Path()[order.NodesDone]], "next unearned tile remains absent");
                    c.Equal(new BigInteger(Fix.FromInt(order.NodesDone).Raw), Consumed(game), "only physically completed segment bills consumed");
                    c.Equal(Fix.FromInt(10000 - 5 * order.NodesDone), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "each reached stage pays exactly once");
                    twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial earned prefix saves identically");
                }
                previous = order.NodesDone;
            }
            c.That(sawPartial, "observed an earned partial corridor"); c.Equal(OrderState.Done, order.State, "physical automatic supply and machine work finish");
            c.Equal(new BigInteger(Fix.FromInt(4).Raw), Consumed(game), "exact whole corridor material consumed");
            c.Equal(Fix.FromInt(9980), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "four actual stage charges");
            c.That(Target(game, order) == null, "normal completion removes only fully earned marker"); c.Equal(game.Hash(), twin.Hash(), "interleaved sessions continue identically");
            foreach (TileCoord tile in Path()) c.Equal((byte)1, Map(game).Layer<ByteLayer>("road")[tile], "each completed tile physically exists");

            game = New(factory); Workers(game); order = Begin(c, game, "segment.owned");
            Reach(game, () => order.NodesDone == 1, "first owned segment");
            c.That(((Beep.SimEngine.Maps.TileLineGeometry)game.World.Services.Providers<NetworkService>().Single().World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("segment.owned"), Path()[1], new PlayerId(1)), "ordinary competing company owns next tile");
            Tick(game); twin = factory.Load(game.Save());
            Reach(game, () => !order.IsActive, "changed ownership refusal", twin);
            c.Equal(OrderState.Failed, order.State, "changed tile ownership refuses later stage"); c.Equal(LineCodes.ForeignLine, order.Blocker, "precise current ownership blocker");
            c.Equal((byte)1, Map(game).Layer<ByteLayer>("segment.line")[Path()[0]], "earned prefix retained after failure");
            c.Equal((byte)0, Map(game).Layer<ByteLayer>("segment.line")[Path()[2]], "failure never stamps remaining path");
            c.Equal(2, Map(game).Layer<IntLayer>("segment.owner")[Path()[1]], "foreign geometry is not overwritten"); c.Equal(game.Hash(), twin.Hash(), "failed physical prefix restores identically");

            game = New(factory); ResourceDispositionTests.Spawn(game, "crane", 6, 16); Tick(game); order = Begin(c, game, "road", 3);
            Entity marker = Target(game, order); Stock(game, marker, 5); Tick(game);
            Reach(game, () => order.NodesDone == 1, "stocked first segment");
            Entity pile = null; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "recovery.container") pile = entity;
            c.That(pile != null, "surplus goes through actual recovery owner"); c.Equal(Path()[0], pile.Get<ITilePresence>().Tile, "leftovers stay at old physical placement");
            c.Equal(Fix.FromInt(4), pile.Get<IInventory>().Amount(Steel(game)), "surplus recovered without loss or duplicated consumption");
            c.Equal(0, marker.Get<IInventory>().Stock.Count, "moving cursor is empty after recovery");
            twin = factory.Load(game.Save()); Reach(game, () => order.Node("segment.1.install").State == NodeState.Waiting, "future material wait", twin);
            c.Equal((byte)0, Map(game).Layer<ByteLayer>("road")[Path()[1]], "recovered old stock is not free future material");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "partial corridor uses ordinary cancellation");
            c.Equal((byte)1, Map(game).Layer<ByteLayer>("road")[Path()[0]], "cancel retains physically built prefix");
            c.That(Target(game, order) == null, "cancel removes empty cursor only"); c.Equal(Fix.FromInt(4), pile.Get<IInventory>().Amount(Steel(game)), "cancel preserves old recovered cargo");

            factory = Factory(); game = New(factory); ResourceDispositionTests.Spawn(game, "crane", 6, 16); Tick(game);
            order = Begin(c, game, "road", 3); marker = Target(game, order); Stock(game, marker, 2); Tick(game);
            Reach(game, () => order.Nodes[0].Progress.WholeTicks > 0, "real started segment");
            IInventory inventory = marker.Get<IInventory>(); var owner = new ReservationOwner("proof.segment-held-inbound", 1);
            ReservationToken incoming = inventory.ReserveIn(owner, Steel(game), Fix.One);
            Reach(game, () => !order.IsActive, "held old-location promise");
            c.Equal(ResourceTransactions.ActivePromises, order.Blocker, "outstanding old-location promise prevents effect and cursor movement");
            c.Equal(Path()[0], marker.Get<ITilePresence>().Tile, "promised endpoint stays at its old placement");
            c.Equal(1, inventory.Reservations.Count, "failed handoff preserves the actual foreign receipt");
            c.Equal((byte)0, Map(game).Layer<ByteLayer>("road")[Path()[0]], "refused handoff stamps no segment");
            c.Equal(Fix.One, inventory.Amount(Steel(game)), "spent startup bill and unspent surplus remain distinct");
            inventory.ReleaseIn(owner, incoming); Tick(game);
            c.That(Target(game, order) == null, "ordinary cleanup resumes only after actual promise release");
            pile = null; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "recovery.container") pile = entity;
            c.That(pile != null && pile.Get<ITilePresence>().Tile == Path()[0] && pile.Get<IInventory>().Amount(Steel(game)) == Fix.One,
                "ended segment recovers remaining real stock at the unchanged location");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "fully disposed failed stage roundtrips");
        }
    }
}
