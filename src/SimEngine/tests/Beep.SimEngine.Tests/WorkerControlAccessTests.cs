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
    // JSON intent and public normal player lifecycle only; no world/stock/progress writers.
    internal static class WorkerControlAccessTests
    {
        internal const string Case = "Worker control access: private hold home and assignments preserve autonomous loaded travel";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, string masked)
            => ResourceContentContractTests.Factory(half, new DirectResourceContentModule(), worldEdit: root => {
                var prototype = root.Get("entities")[2].Clone(); prototype.Set("id", JsonValue.Of("secondary.machine")); root.Get("entities").Add(prototype);
                StartupContentAuthoring.Add(root, "aa.secondary", "secondary.machine", 0, Json.Parse("{\"space\":\"field.points\",\"position\":20}"));
                var entries = root.Get("entity_setups"); var ordered = JsonValue.NewArray(); ordered.Add(entries[entries.Count - 1].Clone());
                for (int i = 0; i < entries.Count - 1; i++) ordered.Add(entries[i].Clone());
                StartupContentAuthoring.UseEntries(root, ordered);
                var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                var settings = Json.Parse("{\"target_kind\":\"entity\",\"target\":\"machine\",\"initial_hidden\":false}"); settings.Set("target", JsonValue.Of(masked));
                scenario.Get("settings").Set(InventorySightModule.ModuleId, settings);
            }, extraModules: new GameModule[] { new InventorySightModule() },
            extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static WorkView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<WorkView>();
        private static SetHomeCommand Home(EntityId id, EntityId home) => new SetHomeCommand { Worker = id, Home = home };
        private static SetWorkerHomeGroupCommand Group(EntityId[] ids, EntityId home) => new SetWorkerHomeGroupCommand { Workers = ids, Home = home };
        private static SetWorkerHeldCommand Held(EntityId id) => new SetWorkerHeldCommand { Worker = id, Held = true };
        private static Code Parity(Check c, SimGame g, int seat, Command command)
        {
            var view = View(g, seat);
            Code typed = command is SetHomeCommand home ? view.PreviewHome(home.Worker, home.Home) :
                command is SetWorkerHeldCommand held ? view.PreviewWorkerHeld(held.Worker, held.Held) :
                view.PreviewHomeGroup(((SetWorkerHomeGroupCommand)command).Workers, ((SetWorkerHomeGroupCommand)command).Home).Blocker;
            c.Equal(typed, g.Seat(seat).Preview(command), "typed/ordinary worker control admission agrees"); return typed;
        }
        private static WorkerHomeView ActualHome(SimGame g, EntityId id)
        { var code = g.Observer.Feature<WorkView>().ReadHome(id, out var value); if (!code.IsNone) throw new InvalidOperationException("actual home/" + code); return value; }
        private static Fix At(SimGame g, EntityId id) => ((PointSnapshot)g.Snapshot.Entities.Single(x => x.Id == id).Spatial).Position;
        private static Fix Stock(SimGame g, EntityId id, ResourceId resource)
        { var code = g.Observer.Feature<LogisticsView>().ReadDiagnostics(id, resource, out var value); if (!code.IsNone) throw new InvalidOperationException("actual stock/" + code); return value.Stock; }
        private static void Reach(Check c, SimGame g, SimGame twin, Func<bool> done, EntityId[] stores, ResourceId resource, string label)
        {
            for (int i = 0; i < 512 && !done(); i++)
            {
                TransformationContentContractTests.Step(g, twin); Fix total = Fix.Zero; foreach (var id in stores) total += Stock(g, id, resource);
                c.Equal(Fix.FromInt(20), total, "actual finite cargo conservation/" + label);
            }
            if (!done())
            {
                var jobs = new List<WorkJobView>(); g.Observer.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException(label + "/clock=" + g.Clock.StepIndex + "/stores=" + string.Join(";", stores.Select(x => x + "/" + At(g, x) + "/" + Stock(g, x, resource))) +
                    "/jobs=" + string.Join(";", jobs.Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
            }
        }
        private static void HiddenWorker(Check c, SimGame g, EntityId machine)
        {
            ulong pure = g.Hash(); var view = View(g);
            c.Equal(Codes.NotFound, view.ReadWorkerAssignment(machine, out var assignment), "hidden assignment withheld before service read");
            c.That(!assignment.HasValue, "hidden assignment returns no facts");
            c.Equal(Codes.NotFound, view.ReadWorkerExecution(machine, out var execution), "hidden execution withheld"); c.That(execution == null, "no hidden execution object");
            c.Equal(Codes.NotFound, view.ReadHome(machine, out var home), "hidden home withheld"); c.That(home == null, "no hidden home object");
            c.Equal(Codes.NotFound, view.ReadOperatingInputs(machine, OperatingPhase.Idle, out var inputs, -1), "private access precedes invalid interval");
            c.That(inputs == null, "no hidden operating inputs");
            c.Equal(Codes.NotFound, g.Observer.Feature<WorkView>().PreviewWorkerHeld(machine, true), "observer diagnosis retains owner refusal");
            c.Equal(pure, g.Hash(), "private current-worker reads pure");
        }
        private static void Healthy(Check c, bool half, bool hiddenHome)
        {
            var f = Factory(half, hiddenHome ? "source" : "machine"); var request = new NewGameRequest { Scenario = "points", Seed = 33701, Seats = { Controller.Human, Controller.Human } };
            var g = f.NewGame(request); var idle = f.NewGame(request); ulong isolated = idle.Hash();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current worker authority identity");
            EntityId source = TransformationContentContractTests.Entity(g, "source"), target = TransformationContentContractTests.Entity(g, "destination"),
                machine = TransformationContentContractTests.Entity(g, "machine"), visible = TransformationContentContractTests.Entity(g, "secondary.machine");
            c.That(visible.Value < machine.Value, "authored visible group member sorts before hidden member");
            c.That(g.Seat(0).View.TryFindResource("supply", out var resource), "actual resource identity");
            var stores = new[] { source, target, machine, visible }; var mixed = new[] { machine, visible };
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, g, Held(visible));
            c.Equal(Code.None, Parity(c, g, 0, Held(machine)), "visible ordinary hold admission");
            PermanentRemovalTests.Submit(c, g, Held(machine));
            PermanentRemovalTests.Submit(c, g, Group(mixed, source));
            PermanentRemovalTests.Submit(c, g, new SetWorkerHeldCommand { Worker = machine, Held = false });
            foreach (var ids in new[] { Array.Empty<EntityId>(), new[] { machine, machine }, new[] { EntityId.None } })
                c.Equal(Codes.InvalidArgument, Parity(c, g, 0, Group(ids, source)), "malformed grouped home input refuses");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Home(machine, source)), "foreign home admission private");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Held(machine)), "foreign hold admission private");
            PermanentRemovalTests.Submit(c, g, new HaulResourcesCommand { Source = source, Destination = target, Worker = machine, Resource = resource, Ceiling = Fix.FromInt(8), Priority = 100 });
            Reach(c, g, null, () => Stock(g, machine, resource).IsPositive && At(g, machine) > Fix.One, stores, resource, "loaded travel");
            var before = ActualHome(g, machine); c.That(before.Job > 0, "real loaded hauling job assigned");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true });
            var inputs = new List<(int Seat, int Sequence)>();
            if (!hiddenHome)
            {
                HiddenWorker(c, g, machine);
                foreach (int seat in new[] { 0, 1 }) foreach (Command command in new Command[] { Held(machine), Home(machine, target), Group(mixed, target) })
                { c.Equal(Codes.NotFound, Parity(c, g, seat, command), "hidden worker command/group refuses"); inputs.Add((seat, g.Seat(seat).Submit(command))); }
                var projection = View(g).PreviewHomeGroup(mixed, target);
                c.That(projection.Entries.Single(x => x.Entity == machine).Current == null && projection.Entries.Single(x => x.Entity == visible).Current != null, "only visible prefix has current home facts");
            }
            else
            {
                c.Equal(Codes.NotFound, View(g).ReadHome(machine, out var unseen), "hidden home target blocks derived live home facts"); c.That(unseen == null, "no guessed hidden home state");
                c.Equal(Codes.NotFound, Parity(c, g, 0, Home(machine, source)), "unseen destination admission private");
                c.Equal(Codes.NotFound, Parity(c, g, 0, Group(mixed, source)), "whole unseen destination admission private");
                c.Equal(Code.None, Parity(c, g, 0, Group(mixed, EntityId.None)), "known own home intent can clear without disclosing hidden old target");
                c.That(View(g).PreviewHomeGroup(mixed, EntityId.None).Entries.All(x => x.Current == null), "allowed clearing discloses no hidden old-home facts");
                inputs.Add((0, g.Seat(0).Submit(Home(machine, source))));
            }
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "hidden original travel and real pending inputs save");
            TransformationContentContractTests.Step(g, twin);
            foreach (var issue in inputs)
            {
                long cursor = 0; var rows = new List<CommandResult>(); g.Seat(issue.Seat).ReadResults(ref cursor, rows);
                var answer = rows.Single(x => x.Sequence == issue.Sequence); c.That(!answer.Accepted && answer.Refusal == Codes.NotFound, "executed private worker control refuses");
            }
            c.That(ActualHome(g, machine).Home == source && ActualHome(g, machine).Job == before.Job && ActualHome(g, visible).Home == source, "refused hold/home changes no job, home or visible prefix");
            if (hiddenHome)
            {
                Reach(c, g, twin, () => Stock(g, target, resource) == Fix.FromInt(8) && ActualHome(g, machine).Job == 0, stores, resource, "hidden home delivery");
                Reach(c, g, twin, () => ActualHome(g, machine).State == WorkerHomeState.AtHome, stores, resource, "autonomous unseen-home return");
                c.Equal(Codes.NotFound, View(g).ReadHome(machine, out _), "autonomous return does not expose unseen home readiness");
                PermanentRemovalTests.Submit(c, g, Group(mixed, EntityId.None));
                c.That(ActualHome(g, machine).Home == EntityId.None && ActualHome(g, visible).Home == EntityId.None, "admitted clearing commits through private original intent without nullable preview dependency");
                PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false });
            }
            else
            {
                PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false });
                c.Equal(Code.None, Parity(c, g, 0, Home(machine, target)), "reveal restores ordinary live home control");
                PermanentRemovalTests.Submit(c, g, Home(machine, target)); twin = f.Load(g.Save());
                c.Equal(before.Job, ActualHome(g, machine).Job, "changing home never commandeers acquired haul");
                Reach(c, g, twin, () => Stock(g, target, resource) == Fix.FromInt(8) && ActualHome(g, machine).Job == 0, stores, resource, "revealed physical delivery");
            }
            c.Equal(Fix.FromInt(12), Stock(g, source, resource), "real finite source supplies eight once");
            c.Equal(Fix.Zero, Stock(g, machine, resource), "actual cargo unloads normally");
            c.That(before.Home == source && before.Job > 0, "retained immutable worker facts remain old");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "worker control lifecycle durable"); c.Equal(isolated, idle.Hash(), "interleaved session isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "normal worker/group/home privacy and physical continuation replay");
        }
        private static void Run(Check c) { foreach (bool half in new[] { false, true }) foreach (bool hiddenHome in new[] { false, true }) Healthy(c, half, hiddenHome); }
    }
}

