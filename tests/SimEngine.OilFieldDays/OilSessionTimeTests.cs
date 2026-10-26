using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    // Actual live-file profile authoring over the existing explicit fixture, not six full Oil worlds.
    internal static class OilSessionTimeTests
    {
        internal const string Case = "Oil session time: all live scenario profiles retain one minute clock and saved frame identity";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Run(Check c)
        {
            var manifest = Json.Parse(OilFieldDaysProofContent.Read("oil-manifest.json"));
            foreach (string document in new[] { "oil/game-modes.json", "oil/scenarios.json" })
            {
                int owners = 0;
                var modules = manifest.Get("modules");
                for (int i = 0; i < modules.Count; i++)
                    for (int j = 0; j < modules[i].Get("documents").Count; j++)
                        if (modules[i].Get("documents")[j].Text == document)
                        { owners++; c.Equal("oil", modules[i].Get("id").Text, "actual Oil module owns explicit time/scenario input"); }
                c.Equal(1, owners, "live input is master-listed exactly once, never a duplicate default");
            }
            var rows = Json.Parse(OilFieldDaysProofContent.Read("oil-scenarios.json")).Get("scenarios");
            var times = Json.Parse(OilFieldDaysProofContent.Read("oil-game-modes.json")).Get("time_definitions");
            c.Equal(6, rows.Count, "all current live Oil scenarios are checked, not one favorable profile");
            c.Equal(rows.Count, times.Count, "actual time profile coverage is complete");
            for (int i = 0; i < rows.Count; i++)
            {
                string scenario = rows[i].Get("id").Text, timeId = rows[i].Get("time").Text;
                JsonValue authored = null;
                for (int j = 0; j < times.Count; j++) if (times[j].Get("id").Text == timeId)
                { c.That(authored == null, "scenario time identity is unique"); authored = times[j]; }
                if (authored == null) throw new InvalidOperationException("actual scenario lacks authored time: " + scenario);
                c.Equal("minute", authored.Get("quantum").Get("unit").Text, "live profile explicitly authors minutes: " + scenario);
                var factory = OilAuthoredInfrastructureTests.Factory(authoredScenario: scenario,
                    editBindings: OilAuthoredInfrastructureTests.SelectLineCosts);
                var game = OilAuthoredInfrastructureTests.New(factory); var time = game.SimulationTime;
                c.Equal(timeId, time.Id, "actual scenario time binding reaches original runtime: " + scenario);
                c.Equal(60L, time.QuantumNumerator, "minute is an exact authored sixty-second quantum: " + scenario);
                c.Equal(1L, time.QuantumDenominator, "minute quantum has no lossy conversion: " + scenario);
                c.That(ReferenceEquals(time, game.Clock.Time), "game and sole clock share one immutable converter: " + scenario);
                c.Equal(10, game.Clock.TicksPerFrame, "actual ten-update frame stride: " + scenario);
                c.Equal(30, game.Clock.TicksPerSecond, "actual platform pacing rate, not game-time conversion: " + scenario);
                c.Equal(60L, time.ExactTicks(GameDuration.Hours(1)), "hour uses original selected minute clock: " + scenario);
                c.Equal(1440L, time.ExactTicks(GameDuration.Days(1)), "day uses original selected minute clock: " + scenario);
                c.That(game.Date == null && time.Calendar(0) == null, "absent calendar fabricates no Oil date: " + scenario);
                c.Equal(10, game.Update(), "ordinary Update runs authored frame size: " + scenario);
                c.Equal(10L, game.Clock.StepIndex, "one clock advances ten actual minutes: " + scenario);
                game.Clock.Pause(); ulong paused = game.Hash(); long before = game.Tick;
                c.Equal(0, game.Update(), "pause earns no simulation work: " + scenario);
                c.That(game.Tick == before && game.Hash() == paused, "paused frame changes no authoritative state: " + scenario);
                game.Clock.Resume(); game.Clock.Speed = 2;
                c.Equal(20, game.Update(), "speed changes step count, not minute quantum: " + scenario);
                c.Equal(30L, game.Clock.StepIndex, "speed keeps single actual time coordinate: " + scenario);
                game.Clock.Speed = 1;
                byte[] save = game.Save(); var header = SaveGame.Describe(save);
                c.Equal(timeId, header.Settings.Time, "saved header names actual selected time: " + scenario);
                var twin = factory.Load(save); c.Equal(game.Hash(), twin.Hash(), "saved native timing identity: " + scenario);
                game.RunTicks(30); twin.Update(); twin.Update(); twin.Update();
                c.Equal(game.Hash(), twin.Hash(), "batched and framed updates share deterministic minute decisions: " + scenario);
                c.Equal(game.Clock.StepIndex, game.Seat(0).Snapshot.Tick, "published state uses the sole clock: " + scenario);
                c.That(game.Fault == null && twin.Fault == null, "healthy current live-profile continuation: " + scenario);
            }
        }
    }
}
