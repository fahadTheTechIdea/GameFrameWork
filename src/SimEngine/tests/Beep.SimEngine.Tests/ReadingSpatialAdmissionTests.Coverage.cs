using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class ReadingSpatialAdmissionTests
    {
        private const string CoverageCase = "Spatial coverage callbacks: direct strategies and native views preserve original faults and selected space";
        private static void CoverageCallbacks(Check c)
        {
            // Physical setup is normal public play; hooks and direct strategy calls below are
            // explicitly negative engine-contract instrumentation, not player discovery grants.
            foreach (bool regional in new[] { false, true })
            {
                var factory = PhysicalFactory(false, false, finite: true, regional: regional);
                var earned = New(factory, "points", 2);
                int order = checked((int)PermanentRemovalTests.Submit(c, earned,
                    new PostOrderCommand { Plan = "reading.point-sample", Target = Entity(earned, "source"), Priority = 60 }).Arg0);
                Until(c, earned, null, order, () => Known(earned, PointSubject), "coverage protocol physically earned checkpoint");
                c.Equal(Fix.FromInt(12), Stock(earned, Entity(earned, "source"), "supply"), "coverage setup retains full actual survey cost");
                byte[] checkpoint = earned.Save();
                foreach (bool direct in new[] { false, true })
                foreach (string mode in new[] { "clock", "throw", "clock-throw", "space", "space-throw" })
                {
                    var game = factory.Load(checkpoint); var independent = factory.Load(checkpoint); ulong isolated = independent.Hash();
                    var world = game.World; var point = (PointSpace)world.Space;
                    var own = game.Seat(0).View.Feature<PointLayerView>(); var foreign = game.Seat(1).View.Feature<PointLayerView>();
                    var rule = world.Content.Get<IVisibilitySampleTarget>(regional ? "reading.point-region" : "reading.point-fresh");
                    var reading = world.Content.Get<PointReadingDef>(PointSubject);
                    var region = world.Content.Get<PointIntervalRegionDef>("reading.point-exact");
                    var context = "regional=" + regional + "/direct=" + direct + "/" + mode;
                    var original = new InvalidOperationException("proof.original-coverage-callback");
                    var before = game.Seat(0).Snapshot; long at = game.Clock.StepIndex;
                    int calls = 0; bool changedSpace = false;
                    Action<SpatialSnapshot> attack = sample => {
                        calls++; if (calls != 1) return;
                        if (mode == "clock" || mode == "clock-throw") game.Clock.Pause();
                        if (mode == "space" || mode == "space-throw") { point.SetAccess(false); changedSpace = !point.AccessOpen; }
                        if (mode != "clock" && mode != "space") throw original;
                    };
                    if (regional) region.ContainmentProbe = attack; else reading.CoverageProbe = attack;
                    var values = new List<Fix> { Fix.One }; Exception caught = null; bool? answer = null; SpatialLayerPageResult? page = null;
                    try
                    {
                        c.Equal(SpatialLayerReadStatus.Unavailable, foreign.ReadLayer(PointLayerId, PointSample, values), "unearned foreign sample refuses before coverage callback: " + context);
                        c.That(calls == 0 && values.Count == 0, "foreign refusal exposes neither callback nor sample prefix: " + context);
                        values.Add(Fix.One);
                        if (direct) answer = rule.AllowsSample(world, new PlayerId(0), new PointSnapshot(world.Space.Id, Fix.Zero, Fix.Zero));
                        else page = own.ReadLayerPage(PointLayerId, PointSample, new SpatialLayerPageRequest(0, 1), values);
                    }
                    catch (Exception error) { caught = error; }
                    finally { region.ContainmentProbe = null; reading.CoverageProbe = null; }
                    c.Equal(1, calls, "actual selected coverage callback runs once: " + context);
                    c.That(caught is InvalidOperationException && game.Fault != null, "coverage failure cannot escape a healthy strategy/query: " + context);
                    c.That(answer == null && page == null, "failed coverage publishes neither bool success nor page facts: " + context);
                    if (!direct) c.Equal(0, values.Count, "failed public coverage query clears retained sample values: " + context);
                    if (mode != "clock" && mode != "space") c.That(ReferenceEquals(original, caught), "first coverage exception retained without finally masking: " + context);
                    if (mode == "space" || mode == "space-throw") c.That(changedSpace && !point.AccessOpen, "actual selected-space mutation happened and is not fabricated rollback: " + context);
                    var fault = game.Fault;
                    c.Throws<InvalidOperationException>(() => game.Save(), "failed coverage cannot save a healthy checkpoint: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "failed coverage cannot publish a healthy hash: " + context);
                    c.Equal(0, game.Update(), "failed coverage advances no game step: " + context);
                    c.Equal(at, game.Clock.StepIndex, "failed coverage retains sole-clock coordinate: " + context);
                    if (fault != null)
                    {
                        foreach (var view in new[] { own, foreign })
                        {
                            values.Clear(); values.Add(Fix.One);
                            c.Throws<InvalidOperationException>(() => view.ReadLayer("missing", PointSample, values), "faulted cached view rejects before missing/private shortcut: " + context);
                            c.Equal(0, values.Count, "faulted cached view clears output: " + context);
                        }
                        c.Throws<InvalidOperationException>(() => rule.AllowsSample(world, new PlayerId(0), new PointSnapshot(world.Space.Id, Fix.One, Fix.One)),
                            "faulted direct strategy rejects before ordinary outside result: " + context);
                        c.That(ReferenceEquals(fault, game.Fault), "original first coverage fault remains authoritative: " + context);
                        var published = game.Seat(0).Snapshot;
                        c.That(published.State == GameState.Faulted && published.Tick == before.Tick && ReferenceEquals(published.Space, before.Space),
                            "fault publication retains actual last healthy private space and time: " + context);
                    }
                    c.Equal(isolated, independent.Hash(), "unarmed independent saved session retains original space and knowledge: " + context);
                    Step(c, independent); c.That(independent.Fault == null, "independent session still advances normally: " + context);
                }
            }
        }
    }
}
