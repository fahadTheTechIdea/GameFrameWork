using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Public driver only. Malicious stock callbacks are explicitly authored engine-side probes.
    internal static class CargoDispositionObservationTests
    {
        internal const string Case = "Cargo disposition observation: actual stock partition durable private facts and first-fault cutoff";
        internal const string DuplicateCase = "Cargo disposition observation: duplicate provider partition stops the original faulted world";
        internal const string UnitCase = "Cargo disposition observation: actual resource units govern stock and reserved partitions";
        internal static void Register()
        { TestRunner.Add(Case, Run); TestRunner.Add(DuplicateCase, c => Negative(c, "duplicate", false)); TestRunner.Add(UnitCase, Units); }
        private static readonly Code Process = new Code("process");
        internal static GameFactory Factory(string fault, bool half, bool? discrete = null, bool fractionalStock = false)
            => ConnectedResourcePortTests.Factory(half, false, true, new ConnectedResourcePortModule(),
                definitions: FrameworkProofContent.Read("atomic-inventory.json"),
                edit: data => {
                    var parts = data.Get("entities")[1].Get("components");
                    parts.Get("atomic_inventory").Set("cargo_read_fault", JsonValue.Of(fault));
                    if (fractionalStock) parts.Get("atomic_inventory").Get("initial_stock").Set("supply", JsonValue.Number("11.5"));
                    parts.Set("transporter", Json.Parse("{\"handling\":\"points.handling\"}"));
                },
                editWorld: root => {
                    if (discrete.HasValue)
                    {
                        var units = root.Get("units"); int found = 0;
                        for (int i = 0; i < units.Count; i++)
                            if (units[i].Get("id").Text == "unit.supply" || units[i].Get("id").Text == "unit.capacity.count")
                            { units[i].Set("discrete", JsonValue.Of(discrete.Value)); found++; }
                        if (found != 2) throw new InvalidOperationException("authored cargo resource/capacity units missing");
                    }
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
                    root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("atomic.factory-start"));
                });
        private static SimGame New(GameFactory f) => ConstructionPlanTests.NewPoints(f);
        private static ResourceId Supply(SimGame g)
        { if (!g.Seat(0).View.TryFindResource("supply", out var r)) throw new InvalidOperationException("authored supply missing"); return r; }
        private static CargoDispositionSnapshot Read(SimGame g, EntityId worker)
        {
            Code refusal = g.Seat(0).View.Feature<LogisticsView>().ReadCargoDisposition(worker, out var row);
            if (!refusal.IsNone) throw new InvalidOperationException("cargo observation refused " + refusal);
            return row;
        }
        private static void Partition(Check c, CargoDispositionSnapshot row, ResourceId r, int retained, int committed, int unassigned)
        {
            c.Equal(Fix.FromInt(retained), row.Retained.AmountOf(r), "actual policy retained stock");
            c.Equal(Fix.FromInt(committed), row.Committed.AmountOf(r), "actual outgoing claim stock");
            c.Equal(Fix.FromInt(unassigned), row.Unassigned.AmountOf(r), "actual unassigned stock");
            c.Equal(Fix.FromInt(12), row.Retained.AmountOf(r) + row.Committed.AmountOf(r) + row.Unassigned.AmountOf(r), "partition conserves original actual stock");
        }
        private static TransformationSnapshot Current(SimGame g, EntityId site)
        { if (!g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(site, Process, out var row)) throw new InvalidOperationException("actual process missing"); return row; }
        private static void Healthy(Check c, bool half)
        {
            var f = Factory("none", half); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            var site = Entity(g, "atomic.factory"); var r = Supply(g); var limits = new ReplayLimits(4 * 1024 * 1024, 6000);
            using var recording = g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, g, new ConfigureStockPolicyCommand { Store = site, Resource = r,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(2), Fix.FromInt(2), Fix.FromInt(12), 30, LogisticsReserveAccess.Protected)) });
            var initial = Read(g, site); Partition(c, initial, r, 2, 0, 10);
            ulong pure = g.Hash(); Read(g, site); c.Equal(pure, g.Hash(), "cargo read is pure");
            c.Equal(Codes.NotFound, g.Seat(1).View.Feature<LogisticsView>().ReadCargoDisposition(site, out var privateRow), "foreign company cargo withheld");
            c.That(privateRow == null, "foreign cargo payload not published");
            PermanentRemovalTests.Submit(c, g, new EnqueueTransformationCommand { Transformer = site, Capability = Process, Transformation = "atomic.equal" });
            PermanentRemovalTests.Submit(c, g, new ConfigureOperationCommand { Entity = site, Operation = Process, Enabled = true, Target = Fix.One });
            var twin = f.Load(g.Save());
            for (int i = 0; i < 80 && Current(g, site).Flow.Produced.AmountOf(r) < Fix.FromInt(4); i++) Step(g, twin);
            c.That(Current(g, site).Flow.Produced.AmountOf(r) >= Fix.FromInt(4), "bounded actual timed production earns output");
            var active = Read(g, site); Partition(c, active, r, 2, 1, 9);
            Partition(c, Read(twin, site), r, 2, 1, 9); c.Equal(g.Hash(), twin.Hash(), "saved stock and original claims continue identically");
            g.Clock.Pause(); twin.Clock.Pause(); long time = g.Clock.StepIndex; Step(g, twin);
            c.Equal(time, g.Clock.StepIndex, "sole paused clock earns no cargo progress"); Partition(c, Read(g, site), r, 2, 1, 9);
            g.Clock.Resume(); twin.Clock.Resume();
            PermanentRemovalTests.Submit(c, g, new CancelTransformationCommand { Transformer = site, Capability = Process, Index = 0 });
            twin = f.Load(g.Save());
            for (int i = 0; i < 80 && Current(g, site).QueueCount != 0; i++) Step(g, twin);
            c.Equal(0, Current(g, site).QueueCount, "bounded physical stop releases actual tool claim");
            Partition(c, Read(g, site), r, 2, 0, 10); Partition(c, active, r, 2, 1, 9); Partition(c, initial, r, 2, 0, 10);
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled cargo partition durable"); c.Equal(isolated, idle.Hash(), "interleaved cargo sessions isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "public production and cargo policy replay");
        }
        private static void Negative(Check c, string mode, bool half, bool? discrete = null)
        {
            var f = Factory(mode, half, discrete); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            var site = Entity(g, "atomic.factory"); var r = Supply(g); var prior = Read(g, site); Partition(c, prior, r, 0, 0, 12);
            var before = g.Seat(0).Snapshot; var foreign = g.Seat(1).Snapshot; long time = g.Clock.StepIndex; g.Clock.Pause();
            CargoDispositionSnapshot row = null; Exception thrown = null;
            try { g.Seat(0).View.Feature<LogisticsView>().ReadCargoDisposition(site, out row); } catch (Exception error) { thrown = error; }
            c.That(thrown != null && row == null, "invalid cargo callback publishes no partial partition " + mode);
            var fault = g.Fault; c.That(fault != null, "cargo observation latches terminal first fault " + mode);
            if (mode.StartsWith("fractional-", StringComparison.Ordinal))
                c.That(fault != null && fault.Message.Contains("resource unit"), "invalid discrete partition retains its actual unit refusal");
            if (mode == "throw") c.That(fault.Message.Contains("proof.original-cargo-read-error"), "original cargo exception retained");
            g.Update(); c.Equal(time, g.Clock.StepIndex, "next public update publishes fault without earning time");
            var after = g.Seat(0).Snapshot; c.Equal(GameState.Faulted, after.State, "faulted cargo publication explicit");
            c.Equal(before.Tick, after.Tick, "last healthy publication time retained");
            c.Equal(0, after.Changed.Count + after.Spawned.Count + after.Removed.Count, "invalid observation emits no partial deltas");
            c.That(ReferenceEquals(before.Space, after.Space) && ReferenceEquals(foreign.Space, g.Seat(1).Snapshot.Space), "each viewer keeps own last healthy spatial snapshot");
            foreach (var old in before.Entities) c.That(ReferenceEquals(old.Spatial, after.Entities.Single(x => x.Id == old.Id).Spatial), "last healthy payload retained");
            g.Update(); c.Equal(time, g.Clock.StepIndex, "fault cutoff advances no clock"); Partition(c, prior, r, 0, 0, 12);
            c.Throws<InvalidOperationException>(() => g.Hash(), "fault cannot hash healthy"); c.Throws<InvalidOperationException>(() => g.Save(), "fault cannot save healthy");
            c.Throws<InvalidOperationException>(() => Read(g, site), "later live cargo view refuses"); c.That(ReferenceEquals(fault, g.Fault), "original cargo fault retained");
            c.Equal(isolated, idle.Hash(), "faulted cargo session isolated");
        }
        private static void Units(Check c)
        {
            foreach (bool half in new[] { false, true })
            {
                foreach (bool discrete in new[] { false, true })
                {
                    var f = Factory("none", half, discrete, fractionalStock: !discrete); var g = New(f);
                    var site = Entity(g, "atomic.factory"); var r = Supply(g);
                    Fix expected = discrete ? Fix.FromInt(12) : Fix.One * Fix.FromInt(23) / Fix.FromInt(2);
                    var row = Read(g, site); c.Equal(expected, row.Unassigned.AmountOf(r), "actual authored whole or divisible cargo remains legal");
                    c.That(row.Retained.IsEmpty && row.Committed.IsEmpty, "no invented reservation partition");
                    ulong before = g.Hash(); g.Clock.Pause(); var twin = f.Load(g.Save());
                    c.Equal(expected, Read(g, site).Unassigned.AmountOf(r), "paused legal quantity remains observable");
                    c.Equal(expected, Read(twin, site).Unassigned.AmountOf(r), "saved legal quantity remains observable");
                    c.Equal(g.Hash(), twin.Hash(), "unit-aware cargo preserves original saved owner");
                    g.Clock.Resume(); c.Equal(before, g.Hash(), "cargo queries and clock pause/resume preserve original state");
                    c.Equal(expected, row.Unassigned.AmountOf(r), "retained legal quantity facts stay detached");
                }
                Negative(c, "fractional-total", half, true);
                Negative(c, "fractional-partition", half, true);
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current cargo observation engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "original authored current protocol, no duplicate cargo persistence");
            foreach (bool half in new[] { false, true }) Healthy(c, half);
            foreach (string mode in new[] { "null", "duplicate", "unknown", "amount", "throw", "mutate", "clock" })
                foreach (bool half in new[] { false, true }) Negative(c, mode, half);
        }
    }
}
