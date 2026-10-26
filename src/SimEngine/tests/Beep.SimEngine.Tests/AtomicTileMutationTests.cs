using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Engine storage/receipt protocol probes, not ordinary player commands or host gameplay.
    internal static class AtomicTileMutationTests
    {
        internal const string Case = "Atomic tile mutation: complete native and independent layer sets refuse without prefixes";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "independent-sensor", Seed = 723, Seats = { Controller.Human, Controller.Human } });
        private static ISpatialLayerDefinition Definition(SimGame game, string id) => game.World.Content.Get<ISpatialLayerDefinition>(id);
        private static ITileWritableSampleLayer Layer(SimGame game, string id)
            => TileSpatialGeometry.RequireLayer<ITileWritableSampleLayer>(game.World, (ITileSpatialSpace)game.World.Space, Definition(game,id), false);
        private static void Version(ITileSampleLayer layer, int value)
        {
            if (layer is MapLayer native) native.Version = value;
            else typeof(SensorSampleLayer).GetField("_version", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(layer, value);
        }
        private static void Run(Check c)
        {
            foreach (bool native in new[] { false, true }) {
                var factory = IndependentTileWorkTests.Factory(nativeTiles: native, lines: true); var game = New(factory);
                var space = (ITileSampleMutationSpace)game.World.Space; var at = new TileCoord(14,16);
                var raw = Definition(game,"sensor.pipeline"); var owner = Definition(game,"sensor.pipeline-owners");
                var layer = Layer(game,raw.Id); var owners = Layer(game,owner.Id); ulong before = game.Hash();
                foreach (var invalid in new IReadOnlyList<TileSampleMutation>[] {
                    new[] { new TileSampleMutation(raw,at,Fix.One), new TileSampleMutation(owner,at,Fix.FromRatio(1,2)) },
                    new[] { new TileSampleMutation(raw,at,Fix.One), new TileSampleMutation(owner,at,Fix.One), new TileSampleMutation(owner,at,Fix.FromInt(2)) },
                    new[] { new TileSampleMutation(raw,at,Fix.One), new TileSampleMutation(owner,new TileCoord(-1,16),Fix.One) }
                }) {
                    c.Throws<ArgumentException>(() => space.ApplySamples(invalid), "invalid later layer/cell refuses whole-set publication");
                    c.Equal(before, game.Hash(), "rejected batch retains original sparse values and codec");
                    c.Equal(Fix.Zero, layer.GetSample(at), "no topology prefix from rejected ownership write");
                    c.Equal(Fix.Zero, owners.GetSample(at), "no ownership prefix from rejected set");
                }
                var foreign = New(IndependentTileWorkTests.Factory(nativeTiles: native, lines: true));
                c.Throws<InvalidOperationException>(() => space.ApplySamples(new[] { new TileSampleMutation(Definition(foreign,raw.Id),at,Fix.One) }), "foreign catalog cannot alias actual selected storage");
                c.Equal(before, game.Hash(), "foreign mutation changes nothing");
                int old = owners.Version;
                int others = ((ISpatialLayerProvider)game.World.Space).SpatialLayers.OfType<ITileSampleLayer>().Where(l => !ReferenceEquals(l,owners)).Sum(l => l.Version);
                Version(owners, native ? int.MaxValue : int.MaxValue - others); before = game.Hash();
                c.Throws<OverflowException>(() => space.ApplySamples(new[] { new TileSampleMutation(raw,at,Fix.One), new TileSampleMutation(owner,at,Fix.One) }),
                    "later individual or aggregate revision exhaustion refuses before any write");
                c.Equal(before, game.Hash(), "exhausted whole-set mutation retains actual codec");
                c.Equal(Fix.Zero, layer.GetSample(at), "exhausted later layer leaves topology unchanged");
                c.Equal(Fix.Zero, owners.GetSample(at), "exhausted batch leaves ownership unchanged");
                Version(owners,old);
                space.ApplySamples(new[] { new TileSampleMutation(owner,at,Fix.One), new TileSampleMutation(raw,at,Fix.One) });
                c.Equal(Fix.One, layer.GetSample(at), "valid whole-set writes actual topology");
                c.Equal(Fix.One, owners.GetSample(at), "same whole-set writes actual ownership");
                game.Update(); c.That(game.Fault == null, "original network rebuild observes complete batch only");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "whole-set topology/ownership checkpoint remains resumable");
                before = game.Hash(); int version = layer.Version;
                space.ApplySamples(new[] { new TileSampleMutation(raw,at,Fix.One), new TileSampleMutation(owner,at,Fix.One) });
                c.Equal(before, game.Hash(), "unchanged whole-set changes no state"); c.Equal(version, layer.Version, "unchanged whole-set creates no revision");

                var materials = New(IndependentTileWorkTests.Factory(nativeTiles: native, extraction: true));
                var cell = materials.World.Content.Get<ISpatialResourceCellDefinition>("sensor.cell");
                var source = materials.World.Services.Get<ISpatialResourceEndpoints>().ResolveCell(cell);
                var requester = new ReservationOwner("proof.atomic",1); var steel = cell.Layer.Resource.Resource;
                source.ReserveOut(requester,steel,Fix.FromInt(2)); before = materials.Hash();
                c.Throws<InvalidOperationException>(() => ((ITileSampleMutationSpace)materials.World.Space).ApplySamples(new[] {
                    new TileSampleMutation(Definition(materials,"sensor.screen"),at,Fix.One),
                    new TileSampleMutation(cell.Layer,new TileCoord(14,20),Fix.Zero)
                }), "whole-set layer writes cannot erase receipt-protected finite material");
                c.Equal(before, materials.Hash(), "material guard refusal retains all values, receipts and cursors");
                c.Equal(Fix.FromInt(2), source.ReservedOut(steel), "whole-set refuses without spending another owner's receipt");
                c.Equal(Fix.FromInt(6), source.Amount(steel), "whole-set guard preserves exact native resource quantity");
            }
        }
    }
}
