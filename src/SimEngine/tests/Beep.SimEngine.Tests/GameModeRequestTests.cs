using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    public static class GameModeRequestTests
    {
        public static void Register() => TestRunner.Add("Game modes: captured request intent, saved identity and peer refusal", Requests);
        private static GameModeAssignment Number(int value) => new GameModeAssignment(new Code("proof.pressure"), GameModeValue.FromNumber(Fix.FromInt(value)));
        private static GameModeAssignment Text(string value) => new GameModeAssignment(new Code("proof.strategy"), GameModeValue.FromText(value));
        private static NewGameRequest Request(int pressure = 4, bool reverse = false)
        {
            var result = new NewGameRequest { Scenario = "custom", Seed = 31 };
            if (reverse) { result.ModeOverrides.Add(Text("rapid")); result.ModeOverrides.Add(Number(pressure)); }
            else { result.ModeOverrides.Add(Number(pressure)); result.ModeOverrides.Add(Text("rapid")); }
            return result;
        }
        private static GameModeValue Value(IGameModeConfiguration configuration, string id)
        {
            if (!configuration.TryGet(new Code(id), out var value)) throw new InvalidOperationException("missing proof setting " + id);
            return value;
        }
        private static GameModeValueSource Source(IGameModeConfiguration configuration, string id)
        {
            foreach (var property in configuration.Properties) if (property.Property.Id == id) return property.Source;
            throw new InvalidOperationException("missing proof source " + id);
        }
        private static ulong ModeHash(SimGame game)
        {
            foreach (var part in game.HashBreakdown()) if (part.Key == "game-mode") return part.Value;
            throw new InvalidOperationException("missing game-mode hash diagnostics");
        }
        private static void Requests(Check c)
        {
            int compositions = 0;
            IGameModeConfiguration composed = null;
            var factory = GameModeConfigurationTests.Factory(compose: composer => { compositions++; composed = composer.World.GameMode; });
            var request = Request();
            var preview = factory.PreviewGameMode(request);
            c.Equal(0, compositions, "setup preview invokes no composition or gameplay initialization");
            c.Equal(Fix.FromInt(4), Value(preview, "proof.pressure").Number, "request replaces scenario/profile/default precedence");
            c.Equal("rapid", Value(preview, "proof.strategy").Text, "request text is validated by authored domain");
            c.Equal(GameModeValueSource.Request, Source(preview, "proof.pressure"), "request provenance is explicit");
            var game = factory.NewGame(request);
            c.That(ReferenceEquals(game.GameMode, composed), "providers see the one effective configuration before composition");
            c.Equal(Value(preview, "proof.pressure"), Value(game.GameMode, "proof.pressure"), "preview and creation use the same resolver");
            c.Equal("expedition", game.World.Settings.GameMode, "creation settings capture explicit authored profile identity");
            c.Equal("proof.pressure", game.World.Settings.ModeOverrides[0].Property.Id, "request intent is canonical ordinal identity");
            c.Equal(2, game.World.Settings.ModeOverrides.Count, "only explicit player intent is captured, not defaults/property database");
            c.Throws<NotSupportedException>(() => ((IList<GameModeAssignment>)game.World.Settings.ModeOverrides).Clear(), "captured intent is immutable");
            ulong hash = game.Hash();
            request.ModeOverrides.Clear(); request.ModeOverrides.Add(Number(9)); request.Scenario = "survival";
            c.Equal(Fix.FromInt(4), Value(game.GameMode, "proof.pressure").Number, "retained request mutation cannot change session rules");
            c.Equal(Fix.FromInt(4), game.World.Settings.ModeOverrides[0].Value.Number, "saved intent was defensively copied");
            c.Equal(hash, game.Hash(), "caller mutation cannot change authoritative hash");
            c.Equal(Fix.FromInt(4), Value(preview, "proof.pressure").Number, "retained setup preview stays immutable");
            c.Equal(Fix.FromInt(2), Value(factory.ScenarioGameMode("custom"), "proof.pressure").Number, "request resolution never poisons shared content cache");
            var reordered = factory.NewGame(Request(reverse: true));
            c.Equal(game.Hash(), reordered.Hash(), "reversed input order yields identical effective hash");
            c.That(EqualBytes(game.Save(), reordered.Save()), "canonical request order produces byte-identical saves");
            var changed = factory.NewGame(Request(5));
            c.That(game.Hash() != changed.Hash(), "same content/scenario but different overrides changes authoritative identity");
            c.That(ModeHash(game) != ModeHash(changed), "diagnostics name the actual configuration mismatch");
            var implicitRequest = new NewGameRequest { Scenario = "custom", Seed = 31 };
            var empty = factory.NewGame(implicitRequest);
            c.That(ReferenceEquals(empty.GameMode, factory.ScenarioGameMode("custom")), "no request overrides reuses immutable scenario projection");
            implicitRequest.ModeOverrides.Add(Number(2));
            var sameValue = factory.NewGame(implicitRequest);
            c.Equal(Value(empty.GameMode, "proof.pressure"), Value(sameValue.GameMode, "proof.pressure"), "explicit equal override preserves authored value");
            c.That(empty.Hash() != sameValue.Hash(), "equal value with explicit request provenance is distinct session intent");

            byte[] save = game.Save(); var header = SaveGame.Describe(save);
            c.Equal("expedition", header.Settings.GameMode, "save header contains selected mode");
            c.Equal(2, header.Settings.ModeOverrides.Count, "header contains only canonical typed override intent");
            c.Equal(Fix.FromInt(4), header.Settings.ModeOverrides[0].Value.Number, "numeric request value roundtrips through existing settings Sync");
            c.Equal("rapid", header.Settings.ModeOverrides[1].Value.Text, "text request value roundtrips through same tagged codec");
            c.That(factory.Describe(save).Compatible, "valid captured intent is compatible in engine load menu");
            var restored = factory.Load(save);
            c.Equal(hash, restored.Hash(), "load recreates effective configuration from header intent and content");
            c.Equal(GameModeValueSource.Request, Source(restored.GameMode, "proof.pressure"), "saved request provenance survives");
            game.RunTicks(2); restored.RunTicks(1); changed.RunTicks(1); restored.RunTicks(1);
            c.Equal(game.Hash(), restored.Hash(), "interleaved saved sessions continue identically");
            c.Equal(Fix.FromInt(5), Value(changed.GameMode, "proof.pressure").Number, "interleaved other session keeps its own override");
            c.That(ReferenceEquals(game.Clock, game.World.Clock), "request configuration adds no time owner");
            var booleanRequest = new NewGameRequest { Scenario = "survival", Seed = 31,
                ModeOverrides = { new GameModeAssignment(new Code("business.tycoon.enabled"), GameModeValue.FromBoolean(false)) } };
            var booleanGame = factory.NewGame(booleanRequest); var booleanTwin = factory.Load(booleanGame.Save());
            c.Equal(false, booleanTwin.World.Settings.ModeOverrides[0].Value.Boolean, "explicit Boolean false roundtrips as actual player intent, not absence");
            c.Equal(GameModeValueSource.Request, Source(booleanTwin.GameMode, "business.tycoon.enabled"), "saved disabled request retains request provenance");
            c.Equal(booleanGame.Hash(), booleanTwin.Hash(), "all three supported tagged value kinds use the same save/hash owner");

            var duringRequest = Request();
            var mutatingFactory = GameModeConfigurationTests.Factory(compose: composer => duringRequest.ModeOverrides.Clear());
            var during = mutatingFactory.NewGame(duringRequest);
            c.Equal(Fix.FromInt(4), Value(during.GameMode, "proof.pressure").Number, "even composition callbacks cannot mutate captured caller input");
            c.Equal(2, during.World.Settings.ModeOverrides.Count, "saved intent remains fixed through composition callbacks");

            PeerChecks(c, factory);
            Refusals(c, factory, () => compositions);
            HeaderFaults(c);
            TaggedFaults(c);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "request identity directly changes engine protocol build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "settings and tagged mode protocol directly migrate save layout");
        }

        private static void PeerChecks(Check c, GameFactory factory)
        {
            var a = factory.NewGame(Request()); var b = factory.NewGame(Request(reverse: true));
            a.StartLockstep(new LockstepOptions { InputDelay = 0, ChecksumEvery = 1, LocalSeats = { 0 } });
            b.StartLockstep(new LockstepOptions { InputDelay = 0, ChecksumEvery = 1 });
            c.Equal(Code.None, b.Net.CheckHello(a.Net.Hello()), "reordered equivalent player intent joins same-content peers");
            var different = factory.NewGame(Request(5));
            different.StartLockstep(new LockstepOptions { InputDelay = 0, ChecksumEvery = 1 });
            ulong before = different.Hash();
            c.Equal(new Code("net.settings-differ"), different.Net.CheckHello(a.Net.Hello()), "different override refuses at settings, before gameplay state comparison");
            c.Equal(before, different.Hash(), "mismatched hello mutates no authoritative state");
            var plain = factory.NewGame(new NewGameRequest { Scenario = "custom", Seed = 31 });
            var explicitSame = factory.NewGame(new NewGameRequest { Scenario = "custom", Seed = 31, ModeOverrides = { Number(2) } });
            plain.StartLockstep(new LockstepOptions { InputDelay = 0, ChecksumEvery = 1 });
            explicitSame.StartLockstep(new LockstepOptions { InputDelay = 0, ChecksumEvery = 1 });
            c.Equal(new Code("net.settings-differ"), plain.Net.CheckHello(explicitSame.Net.Hello()), "equal effective value but different override intent refuses peers");
            for (int i = 0; i < 4; i++)
            {
                a.RunTicks(1); Exchange(a, b); b.RunTicks(1); Exchange(b, a);
            }
            c.Equal(4L, a.Tick, "peer workflow is bounded by four ordinary engine updates");
            c.Equal(a.Tick, b.Tick, "captured peers advance the same sole game clock");
            c.Equal(a.Hash(), b.Hash(), "captured settings preserve actual peer continuation");
            c.That(a.Net.State != NetState.Broken && a.Net.State != NetState.Desynced && b.Net.State != NetState.Broken && b.Net.State != NetState.Desynced, "valid override peers remain healthy");
        }
        private static void Exchange(SimGame from, SimGame to)
        {
            var messages = new List<byte[]>(); from.Net.DrainOutgoing(messages); messages.Reverse();
            foreach (byte[] message in messages) to.Net.Receive(message);
        }
        private static void Refusals(Check c, GameFactory factory, Func<int> compositions)
        {
            int before = compositions();
            foreach (GameModeAssignment assignment in new[] {
                Number(11), Text("missing"), new GameModeAssignment(new Code("proof.pressure"), GameModeValue.FromBoolean(false)),
                new GameModeAssignment(new Code("proof.locked"), GameModeValue.FromBoolean(false)),
                new GameModeAssignment(new Code("unknown"), GameModeValue.FromBoolean(false)),
                new GameModeAssignment(new Code("business.production_income.enabled"), GameModeValue.FromBoolean(true)) })
            {
                var invalid = new NewGameRequest { Scenario = "custom", ModeOverrides = { assignment } };
                c.Throws<ContentException>(() => factory.PreviewGameMode(invalid), "setup preview refuses invalid authored rule override: " + assignment.Property);
                c.Throws<ContentException>(() => factory.NewGame(invalid), "creation refuses same override before composing: " + assignment.Property);
            }
            var duplicate = Request(); duplicate.ModeOverrides.Add(Number(4));
            c.Throws<ArgumentException>(() => factory.NewGame(duplicate), "duplicate typed inputs refuse even equal values; no last writer wins");
            c.Throws<ArgumentException>(() => factory.PreviewGameMode(duplicate), "preview uses same canonical input capture refusal");
            var missingValue = Request(); missingValue.ModeOverrides.Add(default);
            c.Throws<ArgumentException>(() => factory.NewGame(missingValue), "default assignment is not fabricated disabled intent");
            c.Throws<ArgumentNullException>(() => factory.NewGame(new NewGameRequest { Scenario = "custom", ModeOverrides = null }), "null override collection is not an implicit default list");
            c.Throws<ContentException>(() => factory.NewGame(new NewGameRequest { Scenario = "missing", ModeOverrides = { Number(4) } }), "request cannot invent a scenario or profile");
            c.Equal(before, compositions(), "every invalid request refuses before provider composition");
            c.Throws<ArgumentException>(() => new GameModeAssignment(new Code("INVALID ID"), GameModeValue.FromBoolean(false)), "public contract validates stable assignment identity");
            c.Throws<ArgumentException>(() => new GameModeAssignment(new Code("proof.pressure"), default), "unavailable typed values refuse at public contract construction");
        }
        private static void HeaderFaults(Check c)
        {
            int compositions = 0; var initializations = new List<InitializeProbe>();
            var factory = GameModeConfigurationTests.Factory(compose: composer => {
                compositions++; var spy = new InitializeProbe(); initializations.Add(spy); composer.AddSystem(spy, 10);
            });
            var wrongMode = factory.NewGame(Request()); wrongMode.World.Settings.GameMode = "missing";
            byte[] modeSave = wrongMode.Save(); int before = compositions;
            c.That(!factory.Describe(modeSave).Compatible, "load-menu compatibility refuses changed mode identity");
            c.Throws<CorruptStateException>(() => factory.Load(modeSave), "saved mode mismatch refuses, not profile fallback");
            c.Equal(before, compositions, "invalid header mode cannot enter composition");
            var wrongRange = factory.NewGame(Request()); wrongRange.World.Settings.CaptureModeOverrides(new[] { Number(11) });
            byte[] rangeSave = wrongRange.Save(); before = compositions;
            c.That(!factory.Describe(rangeSave).Compatible, "load-menu compatibility validates captured override domain");
            c.Throws<CorruptStateException>(() => factory.Load(rangeSave), "semantically invalid saved override refuses before composition");
            c.Equal(before, compositions, "invalid saved override cannot enter composition");
            var changedIntent = factory.NewGame(Request()); changedIntent.World.Settings.CaptureModeOverrides(new[] { Number(5), Text("rapid") });
            byte[] changedSave = changedIntent.Save();
            c.Throws<CorruptStateException>(() => factory.Load(changedSave), "valid forged header intent cannot overwrite conflicting body identity");
            c.Equal(0, initializations[initializations.Count - 1].Initializations, "derived identity mismatch refuses before restored systems initialize");
        }
        private static void TaggedFaults(Check c)
        {
            // Target only the shared assignment protocol using its normal StateWriter. No second save serializer.
            foreach (int invalidTag in new[] { 0, 99 })
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); writer.Count(1); string id = "proof.pressure"; writer.String(ref id); int tag = invalidTag; writer.Int(ref tag);
                    byte[] bytes = stream.ToArray(); IReadOnlyList<GameModeAssignment> result = Array.Empty<GameModeAssignment>();
                    c.Throws<CorruptStateException>(() => GameModeConfiguration.SyncAssignments(new StateReader(bytes, 0, bytes.Length), ref result), "unknown/unavailable saved value tag refuses");
                }
            }
            foreach (string[] ids in new[] { new[] { "proof.pressure", "proof.pressure" }, new[] { "proof.strategy", "proof.pressure" }, new[] { "INVALID ID" } })
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); writer.Count(ids.Length);
                    foreach (string authoredId in ids) { string id = authoredId; writer.String(ref id); var value = GameModeValue.FromBoolean(false); GameModeValue.Sync(writer, ref value); }
                    byte[] bytes = stream.ToArray(); IReadOnlyList<GameModeAssignment> result = Array.Empty<GameModeAssignment>();
                    c.Throws<CorruptStateException>(() => GameModeConfiguration.SyncAssignments(new StateReader(bytes, 0, bytes.Length), ref result), "duplicate/noncanonical/invalid saved property identity refuses");
                    c.Equal(0, result.Count, "corrupt intent never partially installs a setting collection");
                }
            }
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); writer.Count(1); string id = "proof.strategy"; writer.String(ref id);
                int tag = (int)GameModeValueKind.Text; writer.Int(ref tag); string text = null; writer.String(ref text);
                byte[] bytes = stream.ToArray(); IReadOnlyList<GameModeAssignment> result = Array.Empty<GameModeAssignment>();
                c.Throws<CorruptStateException>(() => GameModeConfiguration.SyncAssignments(new StateReader(bytes, 0, bytes.Length), ref result), "null saved text is not a valid constrained identity");
            }
        }
        private static bool EqualBytes(byte[] first, byte[] second)
        {
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++) if (first[i] != second[i]) return false;
            return true;
        }
    }
}
