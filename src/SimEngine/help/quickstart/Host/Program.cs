using System;
using System.IO;
using Beep.SimEngine.Hosting;
using QuickGame.Engine;

var definition = QuickModule.CreateDefinition();
var files = new DiskFileSource(Path.Combine(AppContext.BaseDirectory, "Content"));
var factory = new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
var game = factory.NewGame(new NewGameRequest { Scenario = "quick", Seed = 42 });
game.Initialize(); // Factory already initialized; this call is harmless/idempotent.
for (int frame = 0; frame < 3; frame++)
{
    game.Update(); // Normal deterministic engine-time update.
}
var hash = game.Hash();
var snapshot = game.Seat(0).Snapshot;
if (snapshot.Tick != game.Tick) throw new InvalidOperationException("Simulation snapshot did not settle with Update.");
var restored = factory.Load(game.Save());
if (restored.Hash() != hash) throw new InvalidOperationException("Saved continuation differs.");
Console.WriteLine($"Quick setup OK: tick={game.Time.Tick}; entities={game.Seat(0).View.EntityCount}; hash={hash:X16}");
