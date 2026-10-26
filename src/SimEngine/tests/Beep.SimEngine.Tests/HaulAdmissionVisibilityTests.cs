using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // JSON intent and normal public players only. No stock/assignment/receipt/progress writer.
    internal static class HaulAdmissionVisibilityTests
    {
        internal const string Case = "Haul admission visibility: explicit and selected private carriers refuse without replacing physical delivery";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, string masked)
            => ResourceContentContractTests.Factory(half, new DirectResourceContentModule(), worldEdit: root => {
                var other = root.Get("entities")[2].Clone(); other.Set("id", JsonValue.Of("secondary.machine")); root.Get("entities").Add(other);
                StartupContentAuthoring.Add(root, "aa.secondary", "secondary.machine", 0, Json.Parse("{\"space\":\"field.points\",\"position\":20}"));
                var entries = root.Get("entity_setups"); var ordered = JsonValue.NewArray(); ordered.Add(entries[entries.Count - 1].Clone());
                for (int i = 0; i < entries.Count - 1; i++) ordered.Add(entries[i].Clone()); StartupContentAuthoring.UseEntries(root, ordered);
                var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                var settings = Json.Parse("{\"target_kind\":\"entity\",\"target\":\"machine\",\"initial_hidden\":false}"); settings.Set("target", JsonValue.Of(masked));
                scenario.Get("settings").Set(InventorySightModule.ModuleId, settings);
            }, extraModules: new GameModule[] { new InventorySightModule() },
            extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static LogisticsView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<LogisticsView>();
        private static HaulResourcesCommand Request(EntityId source, EntityId target, ResourceId resource, EntityId worker = default)
            => new HaulResourcesCommand { Source = source, Destination = target, Resource = resource, Ceiling = Fix.FromInt(8), Worker = worker, Priority = 100 };
        private static HaulPreview Expect(Check c, SimGame g, int seat, HaulResourcesCommand command, HaulStatus status, Code code)
        {
            ulong pure = g.Hash();
            var typed = View(g, seat).PreviewHaul(command.Source, command.Destination, command.Resource, command.Ceiling,
                sourcePort: command.SourcePort, destinationPort: command.DestinationPort, worker: command.Worker, priority: command.Priority);
            c.Equal(status, typed.Status, "typed manual haul status");
            c.Equal(code, g.Seat(seat).Preview(command), "normal manual haul shares original authority/request decision");
            c.Equal(pure, g.Hash(), "manual preview pure");
            if (status == HaulStatus.NotFound)
                c.That(!typed.Source.IsValid && !typed.Destination.IsValid && !typed.Carrier.IsValid && !typed.Resource.IsValid && typed.Amount.IsZero &&
                    typed.Travel == null && typed.TripBudget == null && typed.TravelCost == null, "private refusal has no selected physical facts");
            return typed;
        }
        private static Fix Stock(SimGame g, EntityId id, ResourceId r)
        { var refusal = g.Observer.Feature<LogisticsView>().ReadDiagnostics(id, r, out var facts); if (!refusal.IsNone) throw new InvalidOperationException("actual haul stock/" + refusal); return facts.Stock; }
        private static void Reach(Check c, SimGame g, SimGame twin, Func<bool> done, EntityId[] stores, ResourceId r, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++)
            {
                TransformationContentContractTests.Step(g, twin); Fix total = Fix.Zero; foreach (var id in stores) total += Stock(g, id, r);
                c.Equal(Fix.FromInt(20), total, "finite physical haul conservation/" + stage);
            }
            if (!done()) throw new InvalidOperationException(stage + "/clock=" + g.Clock.StepIndex + "/stocks=" + string.Join(";", stores.Select(x => x + "/" + Stock(g, x, r))));
        }
        private static void Healthy(Check c, bool half, string maskedName)
        {
            var f = Factory(half, maskedName); var request = new NewGameRequest { Scenario = "points", Seed = 34003, Seats = { Controller.Human, Controller.Human } };
            var g = f.NewGame(request); var idle = f.NewGame(request); ulong isolated = idle.Hash();
            var source = TransformationContentContractTests.Entity(g, "source"); var target = TransformationContentContractTests.Entity(g, "destination");
            var primary = TransformationContentContractTests.Entity(g, "machine"); var other = TransformationContentContractTests.Entity(g, "secondary.machine");
            var stores = new[] { source, target, primary, other };
            c.That(g.Seat(0).View.TryFindResource("supply", out var r), "actual shared resource identity");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current manual haul authority identity");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            var retained = Expect(c, g, 0, Request(source, target, r), HaulStatus.Ready, Code.None);
            c.Equal(primary, retained.Carrier, "original least-cost winner is primary");
            c.Equal(other, Expect(c, g, 0, Request(source, target, r, other), HaulStatus.Ready, Code.None).Carrier, "visible alternative actually eligible by explicit request");
            c.Equal(primary, Expect(c, g, 0, Request(source, target, r, primary), HaulStatus.Ready, Code.None).Carrier, "explicit native carrier forwarded");
            var negative = Request(source, target, r, primary); negative.Priority = -1;
            Expect(c, g, 0, negative, HaulStatus.InvalidRequest, Codes.InvalidArgument);
            Expect(c, g, 1, Request(source, target, r, primary), HaulStatus.NotFound, Codes.NotFound);
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true });
            foreach (int seat in new[] { 0, 1 })
            {
                Expect(c, g, seat, Request(source, target, r), HaulStatus.NotFound, Codes.NotFound);
                Expect(c, g, seat, Request(source, target, r, primary), HaulStatus.NotFound, Codes.NotFound);
                var malformed = Request(source, target, default, primary); malformed.Ceiling = Fix.Zero; malformed.Priority = -1; malformed.SourcePort = new Code("missing");
                Expect(c, g, seat, malformed, HaulStatus.NotFound, Codes.NotFound);
            }
            if (maskedName == "machine")
                c.Equal(other, Expect(c, g, 0, Request(source, target, r, other), HaulStatus.Ready, Code.None).Carrier, "explicit visible alternative ready while implicit private original winner is unavailable, not substituted");
            var observer = g.Observer.Feature<LogisticsView>().PreviewHaul(source, target, r, Fix.FromInt(8));
            c.Equal(HaulStatus.NotFound, observer.Status, "observer manual preview retains actual player admission");
            c.Equal(Fix.FromInt(8), retained.Amount, "retained selected fact remains original");
            var pending = new List<(int Seat, int Sequence)>();
            foreach (int seat in new[] { 0, 1 }) foreach (var worker in new[] { EntityId.None, primary })
                pending.Add((seat, g.Seat(seat).Submit(Request(source, target, r, worker))));
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "private participants and actual pending explicit/implicit inbox saved");
            TransformationContentContractTests.Step(g, twin);
            foreach (var input in pending)
            {
                long cursor = 0; var results = new List<CommandResult>(); g.Seat(input.Seat).ReadResults(ref cursor, results);
                var result = results.Single(x => x.Sequence == input.Sequence);
                c.That(!result.Accepted && result.Refusal == Codes.NotFound, "executed private manual input refuses before acquisition");
            }
            var jobs = new List<WorkJobView>(); g.Observer.Feature<WorkView>().CopyJobs(jobs);
            c.That(!jobs.Any(x => x.IsHaul), "private refusals allocate no haul jobs");
            c.That(Stock(g, source, r) == Fix.FromInt(20) && stores.Skip(1).All(x => Stock(g, x, r).IsZero), "private requests charge or deliver nothing");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false });
            Expect(c, g, 0, Request(source, target, r, primary), HaulStatus.Ready, Code.None);
            PermanentRemovalTests.Submit(c, g, Request(source, target, r, primary));
            c.That(Stock(g, target, r).IsZero, "admission creates promises, never instant delivery");
            jobs.Clear(); g.Observer.Feature<WorkView>().CopyJobs(jobs);
            int job = jobs.Single(x => x.Worker == primary && x.State != JobState.Done).Id;
            c.Equal(1, job, "earlier private requests consumed no native job identity");
            Reach(c, g, null, () => Stock(g, primary, r).IsPositive, stores, r, "actual timed pickup");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true });
            Expect(c, g, 0, Request(source, target, r, primary), HaulStatus.NotFound, Codes.NotFound);
            twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "actual unseen acquired cargo and assignment durable");
            jobs.Clear(); g.Observer.Feature<WorkView>().CopyJobs(jobs); c.That(jobs.Any(x => x.Id == job && x.Worker == primary && x.State != JobState.Done), "manual authority does not revoke acquired assignment");
            Reach(c, g, twin, () => Stock(g, target, r) == Fix.FromInt(8) && Stock(g, primary, r).IsZero, stores, r, "hidden original physical delivery");
            c.Equal(Fix.Zero, Stock(g, other, r), "visible alternative never acquires denied implicit request");
            c.Equal(Fix.FromInt(12), Stock(g, source, r), "finite source charged eight once");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled native state durable"); c.Equal(isolated, idle.Hash(), "independent sessions isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "manual private refusals actual timed hauling and sight changes replay");
        }
        private static void Run(Check c) { foreach (bool half in new[] { false, true }) foreach (string masked in new[] { "source", "destination", "machine" }) Healthy(c, half, masked); }
    }
}

