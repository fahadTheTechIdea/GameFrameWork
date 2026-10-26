using System;
using System.IO;
using System.Linq;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class RandomObservationPurityTests
    {
        internal const string Case = "Observation random purity: scoped draws restored writes and saved physical previews";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Primitive(Check c)
        {
            using var stream = typeof(Program).Assembly.GetManifestResourceStream("tests.random-observation-profile.json") ?? throw new InvalidOperationException("missing random vector file");
            using var text = new StreamReader(stream); var reader = new JsonReader(Json.Parse(text.ReadToEnd()), "random.profile");
            c.Equal(1, reader.Int("schema"), "explicit random-vector schema");
            ulong seed = ulong.Parse(reader.Value("seed").Text, CultureInfo.InvariantCulture);
            ulong initial = ulong.Parse(reader.Value("initial_state").Text, CultureInfo.InvariantCulture);
            var draws = reader.Value("expected_draws"); reader.End();
            var random = new DetRandom(seed);
            c.Equal(initial, random.State, "existing SplitMix seed identity unchanged");
            c.Equal(0UL, random.ObservationVersion, "initial invalidation is not simulation age");
            for (int i = 0; i < draws.Count; i++)
            {
                c.Equal(ulong.Parse(draws[i].Text, CultureInfo.InvariantCulture), random.NextULong(), "original xorshift64 sequence unchanged");
                c.Equal((ulong)i + 1, random.ObservationVersion, "one original stream invalidation per draw");
            }
            ulong saved = random.State, version = random.ObservationVersion;
            random.State = saved; c.Equal(version + 1, random.ObservationVersion, "equal-state write is observable, never a silent restore bypass");
            version = random.ObservationVersion;
            c.Throws<ArgumentOutOfRangeException>(() => random.State = 0, "zero random state is rejected, never replaced with a seed");
            c.Equal(saved, random.State, "invalid zero write preserves actual stream");
            c.Equal(version, random.ObservationVersion, "refusal changes no invalidation state");
            c.Throws<ArgumentOutOfRangeException>(() => random.NextInt(0), "invalid random bound draws nothing");
            c.Equal(version, random.ObservationVersion, "invalid bound preserves stream invalidation");
            var restored = new DetRandom(seed); restored.State = saved;
            c.Equal(random.NextULong(), restored.NextULong(), "saved stream state, not transient invalidation count, determines continuation");
        }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest
        { Scenario = "process", Seed = 31, Seats = { Controller.Human, Controller.Human } });
        internal static void PreviewFault(Check c, string fault)
        {
            var document = Json.Parse(FrameworkProofContent.Read("transformer-proof.json"));
            document.Get("entities")[0].Get("components").Get("pulse_transformer").Set("snapshot_random_fault", JsonValue.Of(fault));
            var factory = TransformerTests.Factory(transformerDocument: document.ToString());
            var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash(); game.Update();
            var entity = game.Seat(0).Snapshot.Entities.Single(x => x.Def == "emitter").Id;
            var view = game.Seat(0).View.Feature<TransformationManagementView>();
            c.Equal(Code.None, view.ReadExecutionPreview(entity, new Code("pulse"), "pulse", out var retained), "actual independent preview is healthy before explicit fault activation");
            var own = game.Seat(0).Snapshot; var foreign = game.Seat(1).Snapshot; long time = game.Clock.StepIndex; game.Clock.Pause();
            TransformationExecutionPreview leaked = null; Exception error = null;
            try { view.ReadExecutionPreview(entity, new Code("pulse"), "pulse", out leaked); } catch (Exception failure) { error = failure; }
            c.That(error?.Message.Contains("random-stream") == true, "processor observation detects draw/restore/equal-state RNG mutation");
            c.That(leaked == null && game.Fault != null, "no detached preview escapes RNG-corrupted observation");
            var first = game.Fault; game.Update(); c.That(ReferenceEquals(first, game.Fault), "random purity violation preserves original fault and stops work");
            c.Equal(time, game.Clock.StepIndex, "random faults never advance sole game time");
            c.That(retained.Current.QueueCount == 0 && retained.Transformation == "pulse", "prior immutable preview is retained unchanged");
            c.That(ReferenceEquals(own.Space, game.Seat(0).Snapshot.Space) && ReferenceEquals(foreign.Space, game.Seat(1).Snapshot.Space), "both viewers retain last healthy spatial publication");
            c.Throws<InvalidOperationException>(() => game.Save(), "corrupted random state cannot become a resumable save");
            c.Equal(idleHash, idle.Hash(), "provider RNG violation never changes interleaved session");
        }
        // Corrupt protocol bytes only, not a second loader or simulation state writer.
        private static void ZeroSave(Check c)
        {
            var factory = TransformerTests.Factory(); var game = New(factory); game.Update();
            ulong hash = game.Hash(); var bytes = game.Save(); SaveGame.ReadHeader(bytes, out var body);
            byte[] tag = { 5, 0, 0, 0, (byte)'w', (byte)'o', (byte)'r', (byte)'l', (byte)'d' };
            int found = -1;
            for (int i = body.Position; i <= bytes.Length - 8 - tag.Length; i++)
                if (tag.Select((value, j) => bytes[i + j] == value).All(x => x))
                { if (found >= 0) throw new InvalidOperationException("ambiguous actual world section fixture"); found = i; }
            if (found < 0) throw new InvalidOperationException("actual world section unavailable");
            var state = new StateReader(bytes, found, bytes.Length - 8 - found); state.Section("world");
            long step = 0; state.Long(ref step); string outcome = null; state.String(ref outcome);
            int offset = state.Position; ulong rng = 0; state.ULong(ref rng); c.That(rng != 0, "actual save stores original nonzero stream");
            Array.Clear(bytes, offset, 8);
            ulong checksum = 14695981039346656037UL;
            unchecked { for (int i = 0; i < bytes.Length - 8; i++) { checksum ^= bytes[i]; checksum *= 1099511628211UL; } }
            for (int i = 0; i < 8; i++) bytes[bytes.Length - 8 + i] = (byte)(checksum >> (i * 8));
            string refusal = null; try { factory.Load(bytes); } catch (CorruptStateException error) { refusal = error.Message; }
            c.Equal("random stream has zero state", refusal, "checksum-valid zero stream is refused, not silently reseeded");
            c.Equal(hash, game.Hash(), "corrupt restore changes no existing session");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual changed engine identity");
            Primitive(c); ZeroSave(c);
            foreach (bool raw in new[] { false, true }) foreach (string mode in new[] { "rng", "rng-restore", "rng-same", "rng-zero", "rng-hash", "rng-save" })
                NavigationObservationAllocationTests.Guard(c, raw, mode);
            foreach (string mode in new[] { "rng", "rng-restore", "rng-same" }) PreviewFault(c, mode);
            foreach (bool direct in new[] { false, true }) foreach (bool half in new[] { false, true })
                RecipeDisclosureTests.Physical(c, direct, "resource", half);
        }
    }
}
