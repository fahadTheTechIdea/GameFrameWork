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
    // Authored policy metadata, player inputs and public facts; no scheduler/stock/progress writes.
    internal static class WorkPolicyAccessTests
    {
        internal const string Case = "Work policy access: grouped private admission preserves physical cargo pause and saved continuation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, DirectResourceContentModule resources)
            => ResourceContentContractTests.Factory(half, resources, worldEdit: root => {
                var authored = Json.Parse(FrameworkProofContent.Read("point-work-policies.json"));
                root.Set("work_policies", authored.Get("work_policies").Clone());
                var machine = root.Get("entities")[2].Get("components");
                machine.Set("work_policy", Json.Parse("{\"profile\":\"policy.initial\"}"));
                // A separate authored stationary worker exercises a visible valid group prefix.
                var source = root.Get("entities")[0].Get("components");
                source.Set("worker", machine.Get("worker").Clone()); source.Set("work_policy", machine.Get("work_policy").Clone());
                var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                scenario.Get("settings").Set(InventorySightModule.ModuleId, Json.Parse("{\"target_kind\":\"entity\",\"target\":\"machine\",\"initial_hidden\":false}"));
            }, extraModules: new GameModule[] { new InventorySightModule() },
            extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static WorkPolicySettings Settings(bool enabled, int priority)
            => new WorkPolicySettings(enabled, priority, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All);
        private static ConfigureWorkPolicyCommand Single(EntityId id, WorkPolicySettings settings) => new ConfigureWorkPolicyCommand { Worker = id, Settings = settings };
        private static ConfigureWorkPolicyGroupCommand Group(EntityId[] ids, WorkPolicySettings settings) => new ConfigureWorkPolicyGroupCommand { Workers = ids, Settings = settings };
        private static WorkView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<WorkView>();
        private static Code Parity(Check c, SimGame g, int seat, Command command)
        {
            Code typed = command is ConfigureWorkPolicyCommand one ? View(g, seat).PreviewPolicy(one.Worker, one.Settings)
                : View(g, seat).PreviewPolicyGroup(((ConfigureWorkPolicyGroupCommand)command).Workers, ((ConfigureWorkPolicyGroupCommand)command).Settings).Blocker;
            c.Equal(typed, g.Seat(seat).Preview(command), "single/group typed and ordinary policy admission agree"); return typed;
        }
        private static WorkerPolicyView Policy(SimGame g, EntityId id)
        {
            var refusal = g.Observer.Feature<WorkView>().TryReadPolicy(id, out var policy);
            if (!refusal.IsNone) throw new InvalidOperationException("actual observed policy/" + refusal); return policy;
        }
        private static Fix Stock(SimGame g, EntityId id, ResourceId resource)
        {
            var refusal = g.Observer.Feature<LogisticsView>().ReadDiagnostics(id, resource, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual policy stock/" + refusal); return value.Stock;
        }
        private static Fix Position(SimGame g, EntityId id) => ((PointSnapshot)g.Snapshot.Entities.Single(x => x.Id == id).Spatial).Position;
        private static void Reach(Check c, SimGame g, SimGame twin, Func<bool> done, EntityId source, EntityId target, EntityId machine, ResourceId r, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++)
            {
                TransformationContentContractTests.Step(g, twin);
                c.Equal(Fix.FromInt(20), Stock(g, source, r) + Stock(g, target, r) + Stock(g, machine, r), "physical policy cargo conservation/" + stage);
            }
            if (!done()) throw new InvalidOperationException(stage + "/clock=" + g.Clock.StepIndex + "/position=" + Position(g, machine) + "/policy=" + Policy(g, machine).Settings.Enabled);
        }
        private static void Healthy(Check c, bool half)
        {
            var resources = new DirectResourceContentModule(); var f = Factory(half, resources);
            var g = f.NewGame(new NewGameRequest { Scenario = "points", Seed = 33619, Seats = { Controller.Human, Controller.Human } });
            var idle = f.NewGame(new NewGameRequest { Scenario = "points", Seed = 33619, Seats = { Controller.Human, Controller.Human } }); ulong isolated = idle.Hash();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current work policy authority identity");
            EntityId source = TransformationContentContractTests.Entity(g, "source"), target = TransformationContentContractTests.Entity(g, "destination"), machine = TransformationContentContractTests.Entity(g, "machine");
            c.That(g.Seat(0).View.TryFindResource("supply", out var r), "actual shared resource identity");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, g, new SetWorkerHeldCommand { Worker = source, Held = true });
            var requested = Settings(true, 9); var mixed = new[] { machine, source };
            c.Equal(Code.None, Parity(c, g, 0, Group(mixed, requested)), "public group control needs no named-fleet module");
            c.Equal(WorkPolicyCodes.InvalidSettings, Parity(c, g, 0, Single(machine, null)), "visible invalid settings remain typed");
            c.Equal(WorkPolicyCodes.NotPolicy, Parity(c, g, 0, Single(target, requested)), "visible non-policy target refuses");
            foreach (var ids in new[] { Array.Empty<EntityId>(), new[] { machine, machine }, new[] { EntityId.None } })
                c.Equal(Codes.InvalidArgument, Parity(c, g, 0, Group(ids, requested)), "malformed group selection refuses");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Single(machine, requested)), "foreign single policy control preserves privacy");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Group(mixed, requested)), "foreign group policy control preserves privacy");
            PermanentRemovalTests.Submit(c, g, Group(mixed, requested)); var retained = Policy(g, source);
            c.Equal(9, retained.Settings.Priority, "ordinary sorted group commits actual policy owner");
            PermanentRemovalTests.Submit(c, g, new HaulResourcesCommand { Source = source, Destination = target, Worker = machine, Resource = r, Ceiling = Fix.FromInt(8), Priority = 100 });
            Reach(c, g, null, () => Stock(g, machine, r).IsPositive && Position(g, machine) > Fix.One, source, target, machine, r, "loaded timed travel");
            var assigned = new List<WorkJobView>(); g.Observer.Feature<WorkView>().CopyJobs(assigned);
            int job = assigned.Single(x => x.Worker == machine && x.State != JobState.Done).Id;
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true });
            ulong pure = g.Hash(); c.Equal(Codes.NotFound, View(g).TryReadPolicy(machine, out _), "hidden current policy settings withheld");
            foreach (int seat in new[] { 0, 1 })
            {
                c.Equal(Codes.NotFound, Parity(c, g, seat, Single(machine, null)), "hidden access precedes null settings");
                c.Equal(Codes.NotFound, Parity(c, g, seat, Group(mixed, Settings(false, 19))), "hidden/mixed group refuses before commit");
            }
            var hidden = View(g).PreviewPolicyGroup(mixed, Settings(false, 19));
            c.That(hidden.Entries.Single(x => x.Entity == machine).Settings == null && hidden.Entries.Single(x => x.Entity == source).Settings != null, "only actual visible group entry contains settings");
            c.Equal(Codes.NotFound, g.Observer.Feature<WorkView>().PreviewPolicy(machine, requested), "observer diagnosis uses actual hidden owner admission");
            c.Equal(pure, g.Hash(), "private reads/admission are pure");
            var inputs = new List<(int Seat, int Sequence)>();
            foreach (int seat in new[] { 0, 1 }) foreach (Command command in new Command[] { Single(machine, Settings(false, 19)), Group(mixed, Settings(false, 19)) })
                inputs.Add((seat, g.Seat(seat).Submit(command)));
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "actual hidden loaded cargo and pending policy inputs durable");
            TransformationContentContractTests.Step(g, twin);
            foreach (var input in inputs)
            {
                long cursor = 0; var results = new List<CommandResult>(); g.Seat(input.Seat).ReadResults(ref cursor, results);
                var result = results.Single(x => x.Sequence == input.Sequence);
                c.That(!result.Accepted && result.Refusal == Codes.NotFound, "actual private single/group inputs refuse");
            }
            c.That(Policy(g, source).Settings.Matches(retained.Settings) && Policy(g, machine).Settings.Priority == 9, "hidden member refusal changes no visible prefix or hidden policy");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false });
            PermanentRemovalTests.Submit(c, g, Group(mixed, Settings(false, 11))); twin = f.Load(g.Save());
            Fix position = Position(g, machine), cargo = Stock(g, machine, r); long age = g.Clock.StepIndex;
            for (int i = 0; i < 3; i++) TransformationContentContractTests.Step(g, twin);
            assigned.Clear(); g.Observer.Feature<WorkView>().CopyJobs(assigned);
            c.That(assigned.Any(x => x.Id == job && x.Worker == machine && x.State != JobState.Done), "disabled physical policy preserves the actual assigned hauling job");
            c.That(g.Clock.StepIndex > age && Position(g, machine) == position && Stock(g, machine, r) == cargo, "disabled physical policy pauses motion without pausing game time or losing cargo");
            c.Equal(11, Policy(g, source).Settings.Priority, "same actual group policy controls stationary worker");
            c.Equal(9, retained.Settings.Priority, "retained immutable policy remains old fact");
            c.Throws<NotSupportedException>(() => ((IList<WorkerRoleId>)retained.Settings.AllowedRoles).Clear(), "retained roles readonly");
            PermanentRemovalTests.Submit(c, g, Group(mixed, Settings(true, 12))); twin = f.Load(g.Save());
            Reach(c, g, twin, () => Stock(g, target, r) == Fix.FromInt(8) && Stock(g, machine, r).IsZero, source, target, machine, r, "resumed actual delivery");
            c.Equal(Fix.FromInt(12), Stock(g, source, r), "finite source charged once");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "completed policy/cargo lifecycle durable"); c.Equal(isolated, idle.Hash(), "separate session isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "normal policy group/private refusal/pause/delivery replay");
            ResourceContentContractTests.Healthy(c, half, false, f, resources);
        }
        private static void Run(Check c) { Healthy(c, false); Healthy(c, true); }
    }
}

