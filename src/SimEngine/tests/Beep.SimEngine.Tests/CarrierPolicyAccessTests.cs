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
    // Authored JSON plus normal public player inputs; no private physical state writers.
    internal static class CarrierPolicyAccessTests
    {
        internal const string Case = "Carrier policy access: private grouped load trip and cargo views preserve saved physical delivery";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half)
            => ResourceContentContractTests.Factory(half, new DirectResourceContentModule(), worldEdit: root => {
                root.Set("haul_load_profiles", Json.Parse(FrameworkProofContent.Read("haul-load-policies.json")).Get("haul_load_profiles").Clone());
                root.Set("haul_trip_profiles", Json.Parse(FrameworkProofContent.Read("haul-trip-policy.json")).Get("haul_trip_profiles").Clone());
                var machine = root.Get("entities")[2].Get("components");
                machine.Set("haul_load_policy", Json.Parse("{\"profile\":\"load.batch\",\"states\":{\"disabled\":\"blocked\"}}"));
                machine.Set("haul_trip_policy", Json.Parse("{\"profile\":\"trip.no-floor\"}"));
                var prototype = root.Get("entities")[2].Clone(); prototype.Set("id", JsonValue.Of("secondary.machine")); root.Get("entities").Add(prototype);
                StartupContentAuthoring.Add(root, "aa.secondary", "secondary.machine", 0, Json.Parse("{\"space\":\"field.points\",\"position\":20}"));
                var entries = root.Get("entity_setups"); var ordered = JsonValue.NewArray(); ordered.Add(entries[entries.Count - 1].Clone());
                for (int i = 0; i < entries.Count - 1; i++) ordered.Add(entries[i].Clone());
                StartupContentAuthoring.UseEntries(root, ordered);
                var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                scenario.Get("settings").Set(InventorySightModule.ModuleId, Json.Parse("{\"target_kind\":\"entity\",\"target\":\"machine\",\"initial_hidden\":false}"));
            }, extraModules: new GameModule[] { new InventorySightModule() },
            extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static LogisticsView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<LogisticsView>();
        private static HaulLoadSettings Load(SimGame g, EntityId id)
        { var code = g.Observer.Feature<LogisticsView>().ReadHaulLoadPolicy(id, out var value); if (!code.IsNone) throw new InvalidOperationException("actual load/" + code); return value.Settings; }
        private static HaulTripSettings Trip(SimGame g, EntityId id)
        { var code = g.Observer.Feature<LogisticsView>().ReadHaulTripPolicy(id, out var value); if (!code.IsNone) throw new InvalidOperationException("actual trip/" + code); return value; }
        private static Fix Stock(SimGame g, EntityId id, ResourceId resource)
        { var code = g.Observer.Feature<LogisticsView>().ReadDiagnostics(id, resource, out var value); if (!code.IsNone) throw new InvalidOperationException("actual stock/" + code); return value.Stock; }
        private static Code Parity(Check c, SimGame g, int seat, Command command)
        {
            var v = View(g, seat);
            Code typed = command is ConfigureHaulLoadPolicyCommand l ? v.PreviewHaulLoadPolicy(l.Worker, l.Settings) :
                command is ConfigureHaulTripPolicyCommand t ? v.PreviewHaulTripPolicy(t.Worker, t.Settings) :
                command is ConfigureHaulLoadPolicyGroupCommand ls ? v.PreviewHaulLoadPolicyGroup(ls.Workers, ls.Settings).Blocker :
                v.PreviewHaulTripPolicyGroup(((ConfigureHaulTripPolicyGroupCommand)command).Workers, ((ConfigureHaulTripPolicyGroupCommand)command).Settings).Blocker;
            c.Equal(typed, g.Seat(seat).Preview(command), "typed and normal carrier policy admission agree"); return typed;
        }
        private static void Reach(Check c, SimGame g, SimGame twin, Func<bool> done, EntityId[] stores, ResourceId r, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++)
            {
                TransformationContentContractTests.Step(g, twin); Fix total = Fix.Zero;
                foreach (var id in stores) total += Stock(g, id, r);
                c.Equal(Fix.FromInt(20), total, "finite cargo conservation/" + stage);
            }
            if (!done()) throw new InvalidOperationException(stage + "/clock=" + g.Clock.StepIndex + "/stock=" + string.Join(";", stores.Select(x => x + "/" + Stock(g, x, r))));
        }
        private static void PrivateFacts(Check c, SimGame g, EntityId carrier, int seat)
        {
            var v = View(g, seat); ulong pure = g.Hash();
            c.Equal(Codes.NotFound, v.ReadHaulLoadPolicy(carrier, out var l), "private load omitted"); c.That(l == null, "no private load settings");
            c.Equal(Codes.NotFound, v.ReadHaulTripPolicy(carrier, out var t), "private trip omitted"); c.That(t == null, "no private trip settings");
            c.Equal(Codes.NotFound, v.ReadHaulTripServiceSources(carrier, out var sources), "private service identities omitted"); c.That(sources == null, "no substituted service set");
            c.Equal(Codes.NotFound, v.ReadHaulTripServiceWindow(carrier, out var window), "private service window omitted"); c.That(window == null, "no substituted service window");
            c.Equal(Codes.NotFound, v.ReadHaulTripBudget(carrier, out var budget), "private acquired trip budget omitted"); c.That(budget == null, "no substituted budget");
            c.Equal(Codes.NotFound, v.ReadCargoDisposition(carrier, out var cargo), "private disposition omitted"); c.That(cargo == null, "no substituted disposition");
            var returns = new List<CargoReturnView>(); g.Observer.Feature<LogisticsView>().CopyCargoReturns(carrier, returns);
            c.That(returns.Count > 0, "observer sees actual loaded return candidates");
            c.Equal(Codes.NotFound, v.CopyCargoReturns(carrier, returns), "private return facts omitted"); c.Equal(0, returns.Count, "old caller list cleared");
            c.Equal(pure, g.Hash(), "current private carrier reads pure");
        }
        private static void Healthy(Check c, bool half)
        {
            var f = Factory(half); var request = new NewGameRequest { Scenario = "points", Seed = 33881, Seats = { Controller.Human, Controller.Human } };
            var g = f.NewGame(request); var idle = f.NewGame(request); ulong isolated = idle.Hash();
            var source = TransformationContentContractTests.Entity(g, "source"); var target = TransformationContentContractTests.Entity(g, "destination");
            var carrier = TransformationContentContractTests.Entity(g, "machine"); var secondary = TransformationContentContractTests.Entity(g, "secondary.machine");
            c.That(secondary.CompareTo(carrier) < 0, "visible group prefix ordered before hidden carrier");
            c.That(g.Seat(0).View.TryFindResource("supply", out var r), "actual resource identity");
            var ids = new[] { carrier, secondary }; var stores = new[] { source, target, carrier, secondary };
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current carrier policy identity");
            PermanentRemovalTests.Submit(c, g, new SetWorkerHeldCommand { Worker = secondary, Held = true });
            var initialLoad = Load(g, carrier); var initialTrip = Trip(g, carrier);
            var changedLoad = new HaulLoadSettings(initialLoad.Profile, true, new Bill(new[] { new ResourceAmount(r, Fix.FromInt(5)) }), initialLoad.MaximumWait, true);
            var changedTrip = new HaulTripSettings(initialTrip.Profile, false, false);
            foreach (var invalid in new[] { Array.Empty<EntityId>(), new[] { carrier, carrier }, new[] { EntityId.None } })
            {
                c.Equal(Codes.InvalidArgument, Parity(c, g, 0, new ConfigureHaulLoadPolicyGroupCommand { Workers = invalid, Settings = changedLoad }), "load group shape refuses");
                c.Equal(Codes.InvalidArgument, Parity(c, g, 0, new ConfigureHaulTripPolicyGroupCommand { Workers = invalid, Settings = changedTrip }), "trip group shape refuses");
            }
            c.Equal(HaulLoadPolicyCodes.Invalid, Parity(c, g, 0, new ConfigureHaulLoadPolicyCommand { Worker = carrier, Settings = null }), "visible load null remains typed");
            c.Equal(HaulTripPolicyCodes.Invalid, Parity(c, g, 0, new ConfigureHaulTripPolicyCommand { Worker = carrier, Settings = null }), "visible trip null remains typed");
            PermanentRemovalTests.Submit(c, g, new HaulResourcesCommand { Source = source, Destination = target, Worker = carrier, Resource = r, Ceiling = Fix.FromInt(8), Priority = 100 });
            Reach(c, g, null, () => Stock(g, carrier, r).IsPositive, stores, r, "physical pickup");
            var jobs = new List<WorkJobView>(); g.Observer.Feature<WorkView>().CopyJobs(jobs);
            int job = jobs.Single(x => x.Worker == carrier && x.State != JobState.Done).Id;
            c.Equal(Code.None, View(g).ReadHaulTripBudget(carrier, out var acquired), "visible actual acquired trip budget");
            c.That(acquired != null, "native trip budget exists without optional money");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true });
            PrivateFacts(c, g, carrier, 0); PrivateFacts(c, g, carrier, 1);
            var commands = new Func<Command>[] {
                () => new ConfigureHaulLoadPolicyCommand { Worker = carrier, Settings = changedLoad },
                () => new ConfigureHaulTripPolicyCommand { Worker = carrier, Settings = changedTrip },
                () => new ConfigureHaulLoadPolicyGroupCommand { Workers = ids, Settings = changedLoad },
                () => new ConfigureHaulTripPolicyGroupCommand { Workers = ids, Settings = changedTrip }
            };
            foreach (int seat in new[] { 0, 1 })
            {
                c.Equal(Codes.NotFound, Parity(c, g, seat, new ConfigureHaulLoadPolicyCommand { Worker = carrier, Settings = null }), "private access before invalid load");
                c.Equal(Codes.NotFound, Parity(c, g, seat, new ConfigureHaulTripPolicyCommand { Worker = carrier, Settings = null }), "private access before invalid trip");
                foreach (var create in commands) c.Equal(Codes.NotFound, Parity(c, g, seat, create()), "hidden/foreign single and whole group refuse");
            }
            var lp = View(g).PreviewHaulLoadPolicyGroup(ids, changedLoad); var tp = View(g).PreviewHaulTripPolicyGroup(ids, changedTrip);
            c.That(lp.Entries.Single(x => x.Entity == carrier).Settings == null && tp.Entries.Single(x => x.Entity == carrier).Settings == null, "hidden group settings absent");
            c.That(lp.Entries.Single(x => x.Entity == secondary).Settings != null && tp.Entries.Single(x => x.Entity == secondary).Settings != null, "visible prefix retains actual immutable settings");
            c.Equal(Codes.NotFound, g.Observer.Feature<LogisticsView>().PreviewHaulLoadPolicy(carrier, changedLoad), "observer preview retains owner access");
            c.Equal(Codes.NotFound, g.Observer.Feature<LogisticsView>().PreviewHaulTripPolicy(carrier, changedTrip), "observer trip preview retains owner access");
            var pending = new List<(int Seat, int Sequence)>();
            foreach (int seat in new[] { 0, 1 }) foreach (var create in commands) pending.Add((seat, g.Seat(seat).Submit(create())));
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "saved loaded policies and actual pending private inputs durable");
            TransformationContentContractTests.Step(g, twin);
            foreach (var input in pending)
            {
                long cursor = 0; var results = new List<CommandResult>(); g.Seat(input.Seat).ReadResults(ref cursor, results);
                var result = results.Single(x => x.Sequence == input.Sequence);
                c.That(!result.Accepted && result.Refusal == Codes.NotFound, "executed private carrier controls refuse");
            }
            foreach (var id in ids) c.That(Load(g, id).MinimumLoad.AmountOf(r) == Fix.FromInt(4) && !Load(g, id).EmergencyBypass && Trip(g, id).Enabled, "refused groups change no actual policy or visible prefix");
            jobs.Clear(); g.Observer.Feature<WorkView>().CopyJobs(jobs); c.That(jobs.Any(x => x.Id == job && x.Worker == carrier && x.State != JobState.Done), "acquired actual assignment survives refusal");
            Reach(c, g, twin, () => Stock(g, target, r) == Fix.FromInt(8) && Stock(g, carrier, r).IsZero, stores, r, "hidden physical delivery");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false });
            PermanentRemovalTests.Submit(c, g, new ConfigureHaulLoadPolicyGroupCommand { Workers = ids, Settings = changedLoad });
            PermanentRemovalTests.Submit(c, g, new ConfigureHaulTripPolicyGroupCommand { Workers = ids, Settings = changedTrip });
            foreach (var id in ids) c.That(Load(g, id).MinimumLoad.AmountOf(r) == Fix.FromInt(5) && Load(g, id).EmergencyBypass && !Trip(g, id).Enabled, "revealed public groups commit original policy owners");
            c.That(initialLoad.MinimumLoad.AmountOf(r) == Fix.FromInt(4) && !initialLoad.EmergencyBypass && initialTrip.Enabled, "retained immutable settings remain original");
            c.Equal(Fix.FromInt(12), Stock(g, source, r), "finite source charged once");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled grouped policies durable"); c.Equal(isolated, idle.Hash(), "sessions isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "public group controls private refusal and physical delivery replay");
        }
        private static void Run(Check c) { Healthy(c, false); Healthy(c, true); }
    }
}

