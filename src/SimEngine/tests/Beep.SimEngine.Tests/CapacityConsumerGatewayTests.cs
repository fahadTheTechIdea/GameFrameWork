using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Same independently authored physical workflow plus public malformed-policy admission.
    internal static class CapacityConsumerGatewayTests
    {
        internal const string Case = "Capacity consumer gateway: shared physical workflow and malformed native hauling admission";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Malformed(Check c, bool half, string mode)
        {
            var f = InventoryCapacityAdmissionTests.Factory(half, out _, mode, queryOnly: false);
            var g = ConstructionPlanTests.NewPoints(f);
            var idle = ConstructionPlanTests.NewPoints(InventoryCapacityAdmissionTests.Factory(half, out _));
            ulong isolated = idle.Hash();
            if (!g.Seat(0).View.TryFindResource("supply", out var r)) throw new InvalidOperationException("explicit native resource missing");
            EntityId source = Entity(g, "source"), destination = Entity(g, "destination"), worker = Entity(g, "machine");
            var command = new HaulResourcesCommand { Source = source, Destination = destination, Worker = worker, Resource = r, Ceiling = Fix.FromInt(8), Priority = 10 };
            g.Clock.Pause(); ulong before = g.Hash(); long at = g.Clock.StepIndex;
            Exception commandError = null, viewError = null;
            try { g.Seat(0).Preview(command); } catch (Exception error) { commandError = error; }
            try { g.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source, destination, r, Fix.FromInt(8)); } catch (Exception error) { viewError = error; }
            c.That(commandError is InvalidOperationException && commandError.Message.Contains("native quantum"), "command admission rejects malformed quantum instead of normal capacity refusal " + mode);
            c.That(viewError is InvalidOperationException && viewError.Message.Contains("native quantum"), "management preview uses same canonical quantum refusal " + mode);
            c.Equal(before, g.Hash(), "refused malformed result changes no stock/receipts/queue/clock state");
            c.Equal(at, g.Clock.StepIndex, "pure refusal earns no work time");
            var jobs = new List<WorkJobView>(); g.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            c.Equal(0, jobs.Count, "preview acquires no worker or endpoint reservation");
            c.Equal(isolated, idle.Hash(), "malformed single-session fixture leaves healthy game untouched");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current consumer gateway protocol");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same actual state ownership and codecs");
            foreach (bool half in new[] { false, true }) foreach (bool cancel in new[] { false, true }) {
                var f = InventoryCapacityAdmissionTests.Factory(half, out var resources);
                ResourceContentContractTests.Healthy(c, half, cancel, f, resources);
            }
            foreach (string mode in new[] { "zero-quantum", "unknown-quantum", "failure-raw-quantum" })
                foreach (bool half in new[] { false, true }) Malformed(c, half, mode);
        }
    }
}
