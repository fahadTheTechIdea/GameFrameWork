using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Beep.SimEngine;
using Beep.SimEngine.Hosting;
using QuickGame.Engine;

namespace Beep.SimEngine.Tests
{
    public static class QuickstartAcceptanceTests
    {
        public const string Case = "P6 quickstart: authored initialization and current public lifecycle";
        public static void Register() => TestRunner.Add(Case, Run);
        private static void Run(Check c)
        {
            var definition = QuickModule.CreateDefinition();
            var files = new DiskFileSource(Path.Combine(AppContext.BaseDirectory, "QuickstartContent"));
            var content = ContentSet.Load(files, definition.ContentTypes);
            var factory = new GameFactory(definition, content);
            var game = factory.NewGame(new NewGameRequest { Scenario = "quick", Seed = 42 });
            var initial = game.Seat(0).Snapshot;
            c.Equal(0L, initial.Tick, "authored initialization consumes no game time");
            c.Equal(1, initial.Entities.Count, "sole engine setup creates authored entity once");
            c.Equal("quick.marker", initial.Entities[0].Def, "startup refers to explicit prototype");
            c.Equal(new Code("state.quick.idle"), initial.Entities[0].State, "entity has authored state");
            c.That(initial.Space == null && initial.Entities[0].Spatial == null, "nonspatial game fabricates no map or placement");
            c.Equal("quick.resource-only", game.GameMode.ModeId, "explicit resource-only game mode");
            c.That(!game.Modules.Contains("sim.tycoon"), "no business module selected");
            var resources = new List<ResourceInfo>(); game.Seat(0).View.CopyResources(resources);
            c.Equal(1, resources.Count, "master-listed resource document reaches public catalogue");
            c.Equal("space.planetx.ore", resources[0].Id, "stable authored resource identity");
            c.Equal("mass", resources[0].QuantityUnit.Dimension, "quantity unit is dimensional content, not display metadata");
            c.Equal(new Code("solid"), resources[0].Phase, "resource phase is explicitly authored");
            var before = game.Hash(); game.Initialize();
            c.Equal(before, game.Hash(), "Initialize is idempotent");
            for (int frame = 1; frame <= 3; frame++)
            {
                game.Update();
                c.Equal((long)frame, game.Tick, "sole game clock advances authored frame quantum");
                c.Equal(game.Tick, game.Seat(0).Snapshot.Tick, "player snapshot settles after Update");
                c.Equal(initial.Entities[0].Id, game.Seat(0).Snapshot.Entities[0].Id, "Update never repeats startup");
            }
            c.Equal(0L, initial.Tick, "retained initial publication remains detached");
            c.Throws<NotSupportedException>(() => ((IList<EntityView>)initial.Entities).Clear(), "snapshot collection immutable");
            var hash = game.Hash(); var restored = factory.Load(game.Save());
            c.Equal(hash, restored.Hash(), "save restores current public state");
            game.Update(); restored.Update();
            c.Equal(game.Hash(), restored.Hash(), "saved continuation uses same engine Update");
            var other = factory.NewGame(new NewGameRequest { Scenario = "quick", Seed = 42 });
            c.Equal(0L, other.Tick, "another session has independent clock");
            c.Equal(1, other.Seat(0).Snapshot.Entities.Count, "another session executes its own authored setup");
            c.That(game.Fault == null && restored.Fault == null && other.Fault == null, "no engine faults");
        }
    }
}
