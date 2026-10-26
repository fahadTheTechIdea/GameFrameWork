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
    internal static class MotionNavigationSelectionTests
    {
        internal static void Register() => TestRunner.Add("Navigation selection: private controls, retained travel and saved physical work", Run);
        private static GameFactory Factory(Action<JsonValue> edit = null)
            => LayeredNavigationTests.Factory(configureWorld: world =>
            {
                var mover = world.Get("entities")[1].Get("components").Get("mover");
                mover.Set("class", JsonValue.Of("survey.route"));
                mover.Set("classes", Content.Json.Parse("[\"terrain.route\",\"survey.route\"]")); edit?.Invoke(mover);
            });
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static Entity Machine(SimGame game) => Find(game, "survey.machine");
        private static MotionView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<MotionView>();
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("navigation selection step=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, Action observe = null)
        {
            for (int i = 0; i < 500 && !done(); i++) { Update(game, twin); observe?.Invoke(); }
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Tick + " position=" + Machine(game).Get<ITilePresence>().Position +
                " profile=" + Machine(game).Get<IMotionNavigation>().ReadNavigation() + " motion=" + Machine(game).Get<IMotion>().Status +
                " orders=" + string.Join(";", game.World.Services.Get<WorkBoard>().Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static ConfigureMotionNavigationCommand Select(Entity machine, string profile)
            => new ConfigureMotionNavigationCommand { Entity = machine.Id, Profile = profile };
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current navigation-control identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one directly migrated native selection payload");
            var factory = Factory(); var game = LiveSensorTests.New(factory); var machine = Machine(game); var site = Find(game, "survey.site");
            var mover = machine.Get<Mover>(); var view = View(game); var road = new TileCoord(8, 8); var steel = game.World.Content.ResourceId("steel");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = true }).Accepted, "player hold stabilizes initial idle policy checks");
            c.That(ReferenceEquals(mover, machine.Get<IMotionNavigation>()) && ReferenceEquals(mover, machine.Get<IMotion>()), "one actual native owner for selection, path and physical motion");
            c.That(!game.World.Services.Has<IEconomy>(), "physical route control needs no Tycoon");
            c.Equal(Code.None, view.ReadNavigation(machine.Id, out var initial), "private engine management view");
            c.Equal("survey.route", initial.Profile, "explicit native initial profile");
            c.Equal("survey.route,terrain.route", string.Join(",", initial.Allowed), "detached canonical allowed provider identities");
            c.Throws<NotSupportedException>(() => ((IList<string>)initial.Allowed)[0] = "missing", "host cannot mutate allowed metadata");
            ulong hash = game.Hash();
            c.Equal(Codes.NotFound, View(game, 1).ReadNavigation(machine.Id, out var hidden), "foreign read reveals no policy");
            c.That(hidden == null, "foreign view returns no snapshot");
            c.Equal(Codes.NotFound, View(game, 1).PreviewNavigation(machine.Id, "terrain.route"), "foreign management preview hides existence");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(Select(machine, "terrain.route")), "normal foreign command preserves private admission");
            c.Equal(MotionNavigationCodes.Unsupported, view.ReadNavigation(site.Id, out _), "non-motion target has typed unsupported result");
            c.Equal(MotionNavigationCodes.Unsupported, game.Seat(0).Preview(Select(site, "terrain.route")), "unsupported command agrees with management owner");
            c.Equal(Codes.NotFound, view.ReadNavigation(default, out _), "missing entity refuses");
            foreach (string profile in new[] { null, "", "missing", "terrain.vessel" })
            {
                c.Equal(MotionNavigationCodes.NotAllowed, view.PreviewNavigation(machine.Id, profile), "unlisted profile has no management fallback " + profile);
                c.Equal(MotionNavigationCodes.NotAllowed, game.Seat(0).Preview(Select(machine, profile)), "normal command has same refusal " + profile);
            }
            c.Equal(Code.None, view.PreviewNavigation(machine.Id, "terrain.route"), "explicit allowed registered profile preview");
            c.Equal(Code.None, game.Seat(0).Preview(Select(machine, "terrain.route")), "matching normal command preview");
            c.Equal(hash, game.Hash(), "reads and all management/command previews are pure");
            c.That(TestKit.Order(game, 0, Select(machine, "terrain.route")).Accepted, "normal idle policy commit");
            c.Equal(MoveStatus.Idle, mover.Status, "idle policy does not manufacture a destination");
            c.Equal("terrain.route", mover.ReadNavigation(), "same actual mover owns selected preference");
            int version = machine.Version;
            c.That(TestKit.Order(game, 0, Select(machine, "terrain.route")).Accepted, "same-value command accepted idempotently");
            c.Equal(version, machine.Version, "same-value idle selection does not mutate entity");
            c.That(TestKit.Order(game, 0, Select(machine, "survey.route")).Accepted, "normal player selects uniform route for departure");
            var posted = TestKit.Order(game, 0, new PostOrderCommand { Plan = "survey.sample", Target = site.Id, Priority = 60 });
            c.That(posted.Accepted, "normal player posts actual physical survey work");
            var order = game.World.Services.Get<WorkBoard>().Order((int)posted.Arg0);
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = false }).Accepted, "normal release enables original work coordinator");
            Reach(game, () => mover.Path.Count > 0 && mover.TicksMoving >= 2 && machine.Get<IWorker>().Job != 0, "uniform physical departure");
            c.That(!mover.Path.Contains(road), "actual uniform path does not borrow preferred road");
            var position = machine.Get<ITilePresence>().Position; var goal = mover.Goal; long age = mover.TicksMoving; int job = machine.Get<IWorker>().Job;
            c.Equal(0L, order.Node("sample").Progress.WholeTicks, "moving is not productive work");
            c.Equal(Fix.FromInt(20), site.Get<IInventory>().Amount(steel), "travel does not consume productive input bill");
            c.That(TestKit.Order(game, 0, Select(machine, "terrain.route")).Accepted, "normal player changes routing while job is travelling");
            c.Equal("terrain.route", mover.ReadNavigation(), "runtime selection changes existing motion preference");
            c.Equal(goal, mover.Goal, "same physical destination retained");
            c.Equal(job, machine.Get<IWorker>().Job, "same work assignment retained");
            c.Equal(age + 1, mover.TicksMoving, "selection preserves prior travel age plus normal Update");
            c.That(mover.Path.Contains(road), "same native pathfinder replans using selected layered costs");
            c.That((machine.Get<ITilePresence>().Position - position).Length < Fix.One, "switch advances only actual per-clock budget, never teleports");
            c.Equal(0L, order.Node("sample").Progress.WholeTicks, "switch grants no free productive progress");
            c.Equal("survey.route", initial.Profile, "retained immutable snapshot does not become current authority");
            c.Equal(Code.None, view.ReadNavigation(machine.Id, out var selected), "fresh private view reads actual selection");
            c.Equal("terrain.route", selected.Profile, "fresh selection snapshot");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "selected class and actual fractional route checkpoint together");
            c.Equal("terrain.route", Machine(twin).Get<IMotionNavigation>().ReadNavigation(), "load does not substitute initial profile");
            long step = game.Clock.StepIndex; position = machine.Get<ITilePresence>().Position;
            game.Clock.Pause(); twin.Clock.Pause(); Update(game, twin);
            c.Equal(step, game.Clock.StepIndex, "sole clock pause stops movement/work");
            c.Equal(position, machine.Get<ITilePresence>().Position, "pause preserves actual position");
            c.Equal(game.Hash(), twin.Hash(), "paused selection and route twins identical");
            game.Clock.Resume(); twin.Clock.Resume(); bool usedRoad = false;
            Reach(game, () => order.Node("sample").Progress.WholeTicks > 0, "preferred-road arrival and productive work", twin,
                () => { var tile = machine.Get<ITilePresence>().Tile; usedRoad |= tile.Y == 8 && tile.X >= 2 && tile.X <= 16; });
            c.That(usedRoad, "machine physically follows selected road before working");
            c.Equal(Fix.FromInt(12), site.Get<IInventory>().Amount(steel), "real productive work consumes exactly eight materials");
            c.Equal(game.Hash(), twin.Hash(), "saved native route arrives and starts work identically");
            c.That(!game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out _), "partial work grants no completion knowledge");
            twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "completed physical work", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial-work checkpoint finishes identically with selected profile");
            c.That(game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out _), "actual completion earns original knowledge");
            var fresh = LiveSensorTests.New(factory);
            c.Equal("survey.route", Machine(fresh).Get<IMotionNavigation>().ReadNavigation(), "fresh interleaved session uses independent initial profile");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing", "empty", "null", "duplicate", "unlisted-initial", "invalid-id", "unregistered" })
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(mover => {
                    if (mode == "missing") mover.Remove("classes");
                    if (mode == "empty") mover.Set("classes", Content.Json.Parse("[]"));
                    if (mode == "null") mover.Set("classes", Content.Json.Parse("null"));
                    if (mode == "duplicate") mover.Set("classes", Content.Json.Parse("[\"survey.route\",\"survey.route\"]"));
                    if (mode == "unlisted-initial") mover.Set("classes", Content.Json.Parse("[\"terrain.route\"]"));
                    if (mode == "invalid-id") mover.Set("classes", Content.Json.Parse("[\"survey.route\",\" bad\"]"));
                    if (mode == "unregistered") mover.Set("classes", Content.Json.Parse("[\"survey.route\",\"missing.route\"]"));
                })), "invalid explicit navigation classes fail initialization " + mode);
        }
    }
}
