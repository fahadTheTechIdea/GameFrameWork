using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Player-only driver: no live World/entity/capability access or gameplay fixups.
    internal static class SnapshotDeltaTests
    {
        internal const string Case = "Snapshot deltas: visible facts, physical mod movement and saved publication baselines";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static EntityView Entity(SimGame game, string prototype, int seat = 0)
            => game.Seat(seat).Snapshot.Entities.Single(e => e.Def == prototype);
        private static EntityView Fact(GameSnapshot snapshot, EntityId id) => snapshot.Entities.Single(e => e.Id == id);
        private static bool Progress(SimGame game, int id)
        {
            var nodes = new List<ActivityNodeView>();
            return game.Seat(0).View.Feature<WorkView>().TryCopyNodes(id, nodes) && nodes.Any(n => !n.Progress.Equals(WorkProgress.Zero));
        }
        private static bool Completed(SimGame game, string plan, EntityId target)
            => game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:" + plan, target, out _);
        private static void Verify(GameSnapshot before, GameSnapshot after)
        {
            if (after.PreviousRevision != before.Revision || after.Viewer != before.Viewer || after.Revision <= before.Revision)
                throw new InvalidOperationException("incorrect snapshot stream baseline");
            var current = after.Entities.Select(e => e.Id).ToArray(); var previous = before.Entities.Select(e => e.Id).ToArray();
            if (!current.SequenceEqual(current.OrderBy(id => id)) || !after.Spawned.SequenceEqual(current.Except(previous)) ||
                !after.Removed.SequenceEqual(previous.Except(current))) throw new InvalidOperationException("noncanonical visible arrivals/departures");
            var changedIds = after.Changed.Select(e => e.Id).ToArray();
            if (!changedIds.SequenceEqual(changedIds.OrderBy(id => id)) || changedIds.Distinct().Count() != changedIds.Length ||
                changedIds.Intersect(after.Spawned).Any() || changedIds.Intersect(after.Removed).Any())
                throw new InvalidOperationException("overlapping/noncanonical snapshot changes");
            foreach (var id in current.Intersect(previous))
            {
                var old = Fact(before, id); var now = Fact(after, id);
                bool same = old.Id == now.Id && old.Def == now.Def && old.Visual == now.Visual && old.Kind == now.Kind &&
                    old.Owner == now.Owner && old.State == now.State && old.StateSince == now.StateSince && old.Version == now.Version &&
                    old.Category == now.Category && (old.Spatial == null ? now.Spatial == null : old.Spatial.Matches(now.Spatial));
                if (changedIds.Contains(id) == same) throw new InvalidOperationException("incorrect changed facts " + id);
            }
            foreach (var value in after.Changed)
            {
                var actual = Fact(after, value.Id);
                if (actual.Version != value.Version || actual.State != value.State || actual.Spatial != value.Spatial)
                    throw new InvalidOperationException("delta is not original current projection");
            }
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            var before = game.Snapshot; var scoped = Enumerable.Range(0, game.SeatCount).Select(i => game.Seat(i).Snapshot).ToArray();
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("snapshot clock=" + game.Clock.StepIndex + "/" + game.Fault);
            Verify(before, game.Snapshot); for (int i = 0; i < scoped.Length; i++) Verify(scoped[i], game.Seat(i).Snapshot);
            if (twin != null) { Update(twin); if (game.Hash() != twin.Hash()) throw new InvalidOperationException("saved snapshot sessions diverged"); }
        }
        private static void Reach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 1000 && watch.ElapsedMilliseconds < 20000 && !ready(); i++) Update(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " bound=1000Updates/20s clock=" + game.Clock.StepIndex + " snapshot=" + game.Snapshot.Revision +
                " entities=" + string.Join(";", game.Seat(0).Snapshot.Entities.Select(e => e.Id + "/" + e.Def + "/" + e.State + "/" + e.Version)) +
                " orders=" + Orders(game));
        }
        private static string Orders(SimGame game)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>()?.CopyOrders(orders);
            return string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker));
        }
        private static CommandResult Submit(Check c, SimGame game, Command command, int seat = 0)
        {
            var before = game.Snapshot; var scoped = Enumerable.Range(0, game.SeatCount).Select(i => game.Seat(i).Snapshot).ToArray();
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(seat).Preview(command), "normal public preview"); c.Equal(hash, game.Hash(), "preview pure");
            var result = TestKit.Order(game, seat, command); c.That(result.Accepted, "normal public command");
            Verify(before, game.Snapshot); for (int i = 0; i < scoped.Length; i++) Verify(scoped[i], game.Seat(i).Snapshot);
            return result;
        }
        private static void Control(Check c, SimGame game, EntityId id, bool enabled, int distance, int seat = 0)
            => Submit(c, game, new ConfigureOperationCommand { Entity = id, Operation = new Code("survey.sensor"), Enabled = enabled, Target = Fix.FromInt(distance) }, seat);
        private static void Initial(Check c, SimGame game)
        {
            foreach (var snapshot in new[] { game.Snapshot }.Concat(Enumerable.Range(0, game.SeatCount).Select(i => game.Seat(i).Snapshot)))
                c.That(snapshot.PreviousRevision == 0 && snapshot.Changed.Count == 0 && snapshot.Removed.Count == 0 &&
                    snapshot.Spawned.SequenceEqual(snapshot.Entities.Select(e => e.Id)), "initial/load full visible baseline, not saved dirty cache");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "snapshot semantic identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no persisted snapshot cache");
            var factory = EntitySensorTests.Factory(); var game = LiveSensorTests.New(factory); Initial(c, game);
            var machine = Entity(game, "survey.machine"); var site = Entity(game, "survey.site");
            var rivals = game.Seat(1).Snapshot.Entities.Where(e => e.Def == "survey.foreign-sensor").OrderBy(e => ((TileSpatialSnapshot)e.Spatial).Tile.X).ToArray();
            var near = rivals[0].Id; var far = rivals[1].Id;
            c.That(game.Snapshot.Viewer == PlayerId.None && game.Seat(0).Snapshot.Viewer == new PlayerId(0) && game.Seat(1).Snapshot.Viewer == new PlayerId(1), "explicit observer/seat delta scope");
            var retained = game.Seat(0).Snapshot; var original = Fact(retained, machine.Id); var originalPosition = ((TileSpatialSnapshot)original.Spatial).Position;
            c.Throws<NotSupportedException>(() => ((IList<EntityView>)retained.Changed).Clear(), "readonly changed facts");
            c.Throws<NotSupportedException>(() => ((IList<EntityId>)retained.Spawned).Clear(), "readonly arrivals");
            Submit(c, game, new SetWorkerHeldCommand { Worker = machine.Id, Held = true });
            c.That(game.Seat(0).Snapshot.Changed.Any(e => e.Id == machine.Id), "ordinary worker policy/state changes reported");
            Control(c, game, far, false, 20, 1);
            c.That(game.Snapshot.Changed.Any(e => e.Id == far), "observer receives actual hidden source change");
            c.That(!game.Seat(0).Snapshot.Changed.Any(e => e.Id == far) && !game.Seat(0).Snapshot.Spawned.Contains(far) &&
                !game.Seat(0).Snapshot.Removed.Contains(far), "unseen source change leaks no identity delta");
            ulong hash = game.Hash(); var frozen = game.Seat(0).Snapshot;
            c.That(ReferenceEquals(frozen, game.Seat(0).Snapshot) && frozen.Entities.Count == 2, "reading snapshot retains same scoped projection");
            c.Equal(hash, game.Hash(), "snapshot reads pure");
            game.Clock.Pause(); long time = game.Clock.StepIndex; Update(game);
            c.That(game.Clock.StepIndex == time && game.Seat(0).Snapshot.Changed.Count == 0 && game.Seat(0).Snapshot.Spawned.Count == 0 &&
                game.Seat(0).Snapshot.Removed.Count == 0 && game.Seat(0).Snapshot.PreviousRevision == frozen.Revision, "paused publication has correct unchanged baseline");
            game.Clock.Resume();
            Submit(c, game, new SetWorkerHeldCommand { Worker = machine.Id, Held = false });
            var submitted = Submit(c, game, new PostOrderCommand { Plan = "survey.sample", Target = site.Id, Priority = 60 }); int orderId = checked((int)submitted.Arg0);
            Reach(game, () => Progress(game, orderId) && !Completed(game, "survey.sample", site.Id), "native positive physical work");
            var twin = factory.Load(game.Save()); Initial(c, twin); c.Equal(game.Hash(), twin.Hash(), "partial physical save has no snapshot state");
            var beforeInactive = twin.Seat(0).Snapshot; game.Clock.Pause(); long held = game.Clock.StepIndex; Update(game);
            c.That(game.Clock.StepIndex == held && ReferenceEquals(beforeInactive, twin.Seat(0).Snapshot), "interleaved inactive stream untouched"); game.Clock.Resume();
            Reach(game, () => Completed(game, "survey.sample", site.Id), "native completed physical survey", twin);
            c.That(Fact(retained, machine.Id).Version == original.Version && ((TileSpatialSnapshot)Fact(retained, machine.Id).Spatial).Position == originalPosition, "retained old facts never change");
            Submit(c, game, new MoveVehicleCommand { Vehicle = machine.Id, To = new TileCoord(28, 10) });
            twin = factory.Load(game.Save()); Initial(c, twin);
            Reach(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Id == near), "physical sight approach", twin);
            c.That(game.Seat(0).Snapshot.Spawned.Contains(near) && !game.Seat(0).Snapshot.Changed.Any(e => e.Id == near) &&
                !game.Seat(0).Snapshot.Spawned.Contains(far), "visible arrival exclusively spawned, never global prototype leak");
            Submit(c, game, new SetWorkerHeldCommand { Worker = machine.Id, Held = true });
            Control(c, game, near, false, 20, 1);
            c.That(game.Seat(0).Snapshot.Changed.Any(e => e.Id == near), "actually visible foreign changed facts reported");
            Control(c, game, machine.Id, false, 2);
            c.That(game.Seat(0).Snapshot.Removed.Contains(near) && !game.Seat(0).Snapshot.Removed.Contains(far) &&
                !game.Seat(0).Snapshot.Changed.Any(e => e.Id == near), "lost sight removal contains no hidden state or never-known identity");
            Control(c, game, machine.Id, true, 2);
            c.That(game.Seat(0).Snapshot.Spawned.Contains(near) && !game.Seat(0).Snapshot.Changed.Any(e => e.Id == near), "reacquired sight is fresh visible arrival");
            var same = new PointSnapshot("field.points", Fix.FromInt(3), Fix.FromInt(2));
            c.That(same.Matches(new PointSnapshot("field.points", Fix.FromInt(3), Fix.FromInt(2))) &&
                !same.Matches(new PointSnapshot("field.points", Fix.FromInt(3), Fix.FromInt(3))) &&
                !same.Matches(new PointSnapshot("other.space", Fix.FromInt(3), Fix.FromInt(2))) &&
                !same.Matches(original.Spatial) && !same.Matches(null), "required public geometry matches interpolation/space/type without host switches");
            Point(c); Birth(c);
        }
        private static void Point(Check c)
        {
            var factory = PointGame.Factory(); var request = new NewGameRequest { Scenario = "points", Seed = 27 }; request.Seats.Add(Controller.Human);
            var game = factory.NewGame(request); Initial(c, game); var machine = Entity(game, "machine"); var destination = Entity(game, "destination");
            var initial = game.Seat(0).Snapshot; var result = Submit(c, game, new PostOrderCommand { Plan = "inspect", Target = destination.Id, Priority = 30 }); int id = checked((int)result.Arg0);
            Reach(game, () => ((PointSnapshot)Entity(game, "machine").Spatial).Position > Fix.Zero && !Completed(game, "inspect", destination.Id), "actual public non-tile movement");
            c.That(game.Seat(0).Snapshot.Changed.Any(e => e.Id == machine.Id), "public mod movement uses same snapshot owner");
            var twin = factory.Load(game.Save()); Initial(c, twin);
            Reach(game, () => Completed(game, "inspect", destination.Id), "saved non-tile completion", twin);
            c.That(Completed(game, "inspect", destination.Id), "public physical work completes bounded");
            c.That(((PointSnapshot)Fact(initial, machine.Id).Spatial).Position == Fix.Zero, "retained independent point facts frozen");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "point state roundtrip unaffected by presentation deltas");
        }
        private static void Birth(Check c)
        {
            var factory = OptionalProcessTests.Factory(new ProcessProofModule()); var game = factory.NewGame(new NewGameRequest { Scenario = "process", Seed = 19 });
            var factoryEntity = Entity(game, "assembler"); var before = game.Seat(0).Snapshot;
            Submit(c, game, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = factoryEntity.Id, Transformation = "build", Count = 1 });
            int opening = before.Entities.Count(e => e.Def == "token");
            c.Equal(opening, game.Seat(0).Snapshot.Entities.Count(e => e.Def == "token"), "no instant manufactured entity");
            Reach(game, () => game.Seat(0).Snapshot.Entities.Count(e => e.Def == "token") > opening, "actual supplied timed manufacturing birth");
            var added = game.Seat(0).Snapshot.Spawned.Single(id => Fact(game.Seat(0).Snapshot, id).Def == "token");
            c.That(!game.Seat(0).Snapshot.Changed.Any(e => e.Id == added) && !before.Entities.Any(e => e.Id == added), "actual new machine exclusively spawned");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "manufactured state roundtrip with full rebase");
        }
    }
}
