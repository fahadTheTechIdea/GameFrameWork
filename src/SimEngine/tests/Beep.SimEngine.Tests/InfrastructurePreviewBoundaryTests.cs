using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Public commands/views and authored provider faults; no map, stock or work writers.
    internal static class InfrastructurePreviewBoundaryTests
    {
        internal const string Case = "Infrastructure previews: complete route publication removal authority and first-fault cutoff";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static List<TileCoord> Seed() => new List<TileCoord> { new TileCoord(0, 0) };
        private static void Physical(Check c, bool point, bool half)
        {
            var f = TopologyErasePostconditionTests.Factory(point, half);
            var g = TopologyErasePostconditionTests.Healthy(c, point, half, f);
            var view = g.Seat(0).View.Feature<InfrastructureView>();
            ulong hash = g.Hash(); var path = Seed();
            string line = point ? "corridor.point-line" : "network.pipe";
            EntityId a = Entity(g, point ? "source" : "binding.source"), b = Entity(g, point ? "machine" : "binding.watcher");
            c.Equal(point ? PathSearchResult.InvalidRequest : PathSearchResult.Found,
                view.SuggestLinkPath(line, a, b, path), "native complete link or explicit unsupported point routing");
            c.That(point ? path.Count == 0 : path.Count > 0, "only complete healthy link facts published");
            path = Seed();
            c.Equal(point ? PathSearchResult.InvalidRequest : PathSearchResult.Found,
                view.SuggestLinePath(line, new TileCoord(11, 28), new TileCoord(11, 29), path), "actual line route contract");
            if (!point) c.That(path.First() == new TileCoord(11, 28) && path.Last() == new TileCoord(11, 29), "whole native route endpoints");
            else c.Equal(0, path.Count, "no fabricated tile route for point space");
            path = Seed(); c.Equal(PathSearchResult.InvalidRequest, view.SuggestLinkPath(line, new EntityId(99999), b, path), "missing target refuses");
            c.Equal(0, path.Count, "missing target clears prior route");
            path = Seed(); c.Equal(PathSearchResult.InvalidRequest, g.Seat(1).View.Feature<InfrastructureView>().SuggestLinkPath(line, a, b, path), "foreign targets refuse");
            c.Equal(0, path.Count, "foreign link leaves no private route");
            c.Equal(Codes.UnknownDef, view.PreviewRemoval("missing.line", null), "unknown removal definition");
            IReadOnlyList<SpatialPlacement> cut = point
                ? new SpatialPlacement[] { new PointPlacement("field.points", Fix.FromInt(5)) }
                : new SpatialPlacement[] { new TilePlacement("recovery.tiles", new TileCoord(11, 28), FixVec2.Centre(new TileCoord(11, 28))) };
            c.Equal(Code.None, view.PreviewRemoval(line, cut), "same original topology admission after physically earned reconnect");
            c.Equal(LineCodes.ForeignLine, g.Seat(1).View.Feature<InfrastructureView>().PreviewRemoval(line, cut), "foreign company cannot erase owned line");
            c.Equal(hash, g.Hash(), "all route and removal previews pure");
            c.Equal(hash, f.Load(g.Save()).Hash(), "observations introduce no state or serialization model");
        }
        private static void Failure(Check c, bool half, string mode)
        {
            var f = LinePlacementPolicyTests.Factory(true, half, edit: data =>
                data.Get("independent_line_placement_policies")[0].Set("fault", JsonValue.Of(mode)));
            var g = LineDefinitionContractTests.New(f);
            var idle = LineDefinitionContractTests.New(LinePlacementPolicyTests.Factory(false, half)); ulong isolated = idle.Hash();
            var own = g.Seat(0).View.Feature<InfrastructureView>(); var foreign = g.Seat(1).View.Feature<InfrastructureView>();
            while (g.Clock.StepIndex < (half ? 6 : 3)) Step(g);
            var before = g.Snapshot; var path = Seed(); Exception error = null;
            try { own.SuggestLinePath("segment.road-line", new TileCoord(16, 10), new TileCoord(18, 10), path); }
            catch (Exception actual) { error = actual; }
            c.That(error is InvalidOperationException && g.Fault != null && path.Count == 0, "real callback withholds full destination/" + mode);
            if (g.Fault == null) throw new InvalidOperationException("expected actual authored provider fault/" + mode);
            if (mode == "throw" || mode == "clock-throw") c.Equal("proof.line-placement-first-error", error.Message, "original callback error retained");
            var first = g.Fault;
            foreach (var view in new[] { own, foreign }) foreach (string line in new[] { "missing.line", "segment.road-line" })
            {
                path = Seed(); c.Throws<InvalidOperationException>(() => view.SuggestLinePath(line, default, default, path), "faulted valid/missing line route");
                c.Equal(0, path.Count, "no stale route after first fault");
                path = Seed(); c.Throws<InvalidOperationException>(() => view.SuggestLinkPath(line, new EntityId(99999), default, path), "fault wins over missing/private link early return");
                c.Equal(0, path.Count, "no stale link after first fault");
                c.Throws<InvalidOperationException>(() => view.PreviewRemoval(line, null), "fault wins over missing/private removal early return");
                var pools = new List<NetworkPoolView>(); c.Throws<InvalidOperationException>(() => view.CopyPools("missing.network", pools), "faulted pool refusal");
                var flow = new List<NetworkFlowSnapshot>(); c.Throws<InvalidOperationException>(() => view.CopyFlow("missing.network", flow), "faulted flow refusal");
                c.Throws<InvalidOperationException>(() => view.ReadConnection(default, "missing.network", out _), "faulted connection refusal");
                c.Throws<InvalidOperationException>(() => view.ReadDemand("missing.network", default, default, false, out _), "faulted demand refusal");
                c.Throws<InvalidOperationException>(() => view.ReadGates(default, "missing.network", out _), "faulted gate refusal");
            }
            c.Equal(before.Tick, g.Snapshot.Tick, "last healthy snapshot time retained");
            c.Throws<InvalidOperationException>(() => g.Save(), "no healthy checkpoint after preview failure");
            c.Throws<InvalidOperationException>(() => g.Hash(), "no healthy hash after preview failure");
            long stopped = g.Clock.StepIndex; c.Equal(0, g.Update(), "first fault stops Update"); c.Equal(stopped, g.Clock.StepIndex, "clock does not advance");
            c.That(ReferenceEquals(first, g.Fault), "original first fault stable"); c.Equal(isolated, idle.Hash(), "other actual session isolated");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current preview protocol identity");
            foreach (bool point in new[] { false, true }) foreach (bool half in new[] { false, true }) Physical(c, point, half);
            foreach (bool half in new[] { false, true }) foreach (string mode in new[] { "throw", "clock", "clock-throw", "map" }) Failure(c, half, mode);
        }
    }
}
