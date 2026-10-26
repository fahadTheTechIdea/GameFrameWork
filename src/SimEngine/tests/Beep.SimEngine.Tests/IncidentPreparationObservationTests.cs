using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class TileIncidentPropagationContractTests
    {
        private const string PreparationCase = "Incident preparation: complete target state guards delayed native terrain publication and physical saved response";
        private static GameFactory PreparationFactory(bool direct) => Factory(new InitializeProbe(), out _, direct,
            edit: root => {
                Rule(root, direct).Set("exposure_effect", JsonValue.Of("exposure.preparation"));
                root.Get("entities")[0].Get("components").Set("network_signal_private_probe", Json.Parse("{}"));
            }, editBindings: root => root.Get("entities")[1].Get("components").Set("network_signal_private_probe", Json.Parse("{}")),
            exposureDocument: FrameworkProofContent.Read("exposure-preparation.json"));
        private static void Preparation(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual preparation engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same original episode/work/terrain/backlog codecs");
            // These first variants reproduce both terminal-read and target-codec gaps on the old core.
            PreparationFault(c, false, "throw");
            PreparationFault(c, false, "target");
            foreach (bool direct in new[] { false, true })
            {
                PreparationPhysical(c, direct);
                foreach (string mode in new[] { "source", "clock", "target-throw", "source-throw", "target-refuse", "null" })
                    PreparationFault(c, direct, mode);
                if (direct) { PreparationFault(c, true, "target"); PreparationFault(c, true, "throw"); }
            }
        }
        private static void PendingPreparation(SimGame game)
        {
            Reach(game, null, () => Exposure(game).Pending.Count == 1, "actual delayed preparation receipt");
            long due = Exposure(game).Pending.Single().DueTick;
            Reach(game, null, () => game.Clock.StepIndex >= due, "actual delayed preparation coordinate");
            if (Incident(game, "binding.watcher").Snapshot.Episode != 0 || Exposure(game).Pending.Count != 1)
                throw new InvalidOperationException("preparation checkpoint must precede the actual target Raise");
        }
        private static void PreparationFault(Check c, bool direct, string mode)
        {
            var factory = PreparationFactory(direct); var game = New(factory); Raise(c, game); PendingPreparation(game);
            var source = game.World.Entities.Get(Entity(game, "terrain.source"));
            var target = game.World.Entities.Get(Entity(game, "binding.watcher"));
            var layer = ((ISpatialLayerProvider)game.World.Space).SpatialLayers.Single(l => l.Id == "terrain.receiver-mask") as ITileSampleLayer;
            if (layer == null) throw new InvalidOperationException("actual native terrain sample layer missing");
            var original = new[] { layer.GetSample(new TileCoord(13, 28)), layer.GetSample(new TileCoord(14, 28)) };
            int sourceRevision = source.Version, targetRevision = target.Version;
            long published = game.Seat(0).Snapshot.Tick;
            var ownView = View(game); var foreignView = View(game, 1);
            var probe = game.World.Content.Get<ExposurePreparationProbeDef>("exposure.preparation");
            probe.Arm(game.World, mode); game.Update();
            string label = (direct ? "independent/" : "native/") + mode;
            c.That(game.Fault != null, label + " pure preparation must fault before publication");
            if (game.Fault == null) throw new InvalidOperationException("preparation side effect escaped: " + label + ";episode=" + target.Get<IPhasedIncidentResponse>(Leak).ReadLifecycle().Episode);
            c.Equal(1, probe.PreparationCalls, label + " failed provider is called once");
            c.Equal(sourceRevision, source.Version, label + " source private probe bypasses revisions deliberately");
            c.Equal(targetRevision, target.Version, label + " target private probe bypasses revisions deliberately");
            c.Equal(mode.StartsWith("source", StringComparison.Ordinal) ? 1 : 0, source.Get<ExposurePrivateProbe>().Value, label + " exact source mutation diagnostic, no fake rollback");
            c.Equal(mode.StartsWith("target", StringComparison.Ordinal) ? 1 : 0, target.Get<ExposurePrivateProbe>().Value, label + " exact target mutation diagnostic, no fake rollback");
            c.Equal(0, target.Get<IPhasedIncidentResponse>(Leak).ReadLifecycle().Episode, label + " no successful target incident prefix");
            c.Equal(original[0], layer.GetSample(new TileCoord(13, 28)), label + " prepared first-cell write never publishes");
            c.Equal(original[1], layer.GetSample(new TileCoord(14, 28)), label + " prepared second-cell write never publishes");
            c.Equal(published, game.Seat(0).Snapshot.Tick, label + " retains last healthy snapshot");
            if (mode.Contains("throw")) c.That(game.Fault.Message.Contains("proof.exposure-preparation-first-error"), label + " original provider exception is the first cause");
            c.Throws<InvalidOperationException>(() => game.Save(), label + " failed preparation cannot save");
            c.Throws<InvalidOperationException>(() => game.Hash(), label + " failed preparation cannot certify a healthy hash");
            c.Throws<InvalidOperationException>(() => ownView.TryRead(source.Id, Spread, out _), label + " owned backlog refuses terminal state");
            c.Throws<InvalidOperationException>(() => foreignView.TryRead(source.Id, Spread, out _), label + " private early return refuses terminal state");
            c.Throws<InvalidOperationException>(() => ownView.TryRead(new EntityId(9999999), Spread, out _), label + " missing early return refuses terminal state");
            c.Throws<InvalidOperationException>(() => ownView.TryRead(source.Id, Code.None, out _), label + " unnamed early return refuses terminal state");
        }
        private static void PreparationPhysical(Check c, bool direct)
        {
            var factory = PreparationFactory(direct); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            var effect = game.World.Content.Get<ExposurePreparationProbeDef>("exposure.preparation");
            c.That(effect.AuthoringPath.Contains("exposure-preparation.json"), "effect comes from its explicit master-listed JSON document");
            c.That(effect.ContentDependencies.Single() is TileTerrainWorkEffectDef && game.World.Content.Owns(effect.ContentDependencies.Single()), "independent preparation reuses the actual native terrain owner");
            var limits = new ReplayLimits(8 * 1024 * 1024, 4000); using var recording = game.RecordReplay(limits);
            Raise(c, game); PendingPreparation(game); var pending = Exposure(game).Pending.Single();
            c.Equal(game.SimulationTime.ExactTicks(GameDuration.Minutes(60)), pending.DueTick - pending.ScheduledTick, "whole original sixty-minute exposure delay");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending receipt and original terrain save before preparation");
            Reach(game, twin, () => Incident(game, "binding.watcher").Snapshot.Active, "real delayed preparation and target episode");
            c.Equal(1, Incident(game, "binding.watcher").Snapshot.Episode, "one exact new target episode");
            var values = new List<int>();
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(0).View.Feature<TileMapView>().ReadLayer("terrain.receiver-mask", new TileRect(13, 28, 2, 1), values), "actual prepared native terrain is publicly observable");
            c.That(values.SequenceEqual(new[] { 2, 2 }), "whole real target footprint publishes through original atomic terrain owner");
            c.That(Incident(game, "terrain.source").Snapshot.Active && !Incident(game, "terrain.source").Blocker.IsNone, "source waits for physical response material, not instant repair");
            Supply(c, game, Entity(game, "terrain.source"), "water"); twin = factory.Load(game.Save());
            Reach(game, twin, () => !Incident(game, "terrain.source").Snapshot.Active, "actual supplied source response");
            Supply(c, game, Entity(game, "binding.watcher"), "water"); twin = factory.Load(game.Save());
            Reach(game, twin, () => !Incident(game, "binding.watcher").Snapshot.Active, "actual supplied target response");
            c.That(Amount(game, "depot", "water") == Fix.FromInt(8) && Amount(game, "terrain.source", "water") + Amount(game, "binding.watcher", "water") + Amount(game, "truck", "water") == Fix.Zero, "two real hauled water units consumed once");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original work/terrain/incident state roundtrips");
            c.Equal(untouched, isolated.Hash(), "prepared exposure mutates no unrelated session");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "delayed preparation and physical supplied responses replay through original owners");
        }
    }
}
