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
    // File/master authoring and ordinary player commands/public facts only.
    internal static class NavigationAccessParityTests
    {
        internal const string Case = "Navigation access parity: private grouped controls preserve physical cargo and saved travel";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half)
            => FleetNavigationTests.Factory(additionalModules: new GameModule[] { new InventorySightModule() },
                additionalDeclarations: Json.Parse("[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]"),
                worldEdit: root => {
                    var scenario = root.Get("scenarios")[0]; var selected = JsonValue.NewArray();
                    var existing = scenario.Get("modules");
                    for (int i = 0; i < existing.Count; i++) if (existing[i].Text != KnowledgeVisibilityModule.ModuleId) selected.Add(existing[i].Clone());
                    selected.Add(JsonValue.Of(InventorySightModule.ModuleId)); scenario.Set("modules", selected);
                    scenario.Get("settings").Remove(KnowledgeVisibilityModule.ModuleId);
                    scenario.Get("settings").Set(InventorySightModule.ModuleId, Json.Parse("{\"target_kind\":\"entity\",\"target\":\"survey.machine\",\"initial_hidden\":false}"));
                    // This selected fixture has one actual visibility service. Unselected sensor components are not instantiated.
                    root.Get("entities")[1].Get("components").Remove("tile_sensor");
                    root.Get("entities")[2].Get("components").Remove("tile_sensor");
                    if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                });
        private static SimGame New(GameFactory f) => LiveSensorTests.New(f);
        private static MotionView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<MotionView>();
        private static ConfigureMotionNavigationCommand Single(EntityId id, string profile) => new ConfigureMotionNavigationCommand { Entity = id, Profile = profile };
        private static ConfigureMotionNavigationGroupCommand Group(EntityId[] ids, string profile) => new ConfigureMotionNavigationGroupCommand { Entities = ids, Profile = profile };
        private static Code Parity(Check c, SimGame g, int seat, Command input)
        {
            Code typed = input is ConfigureMotionNavigationCommand one ? View(g, seat).PreviewNavigation(one.Entity, one.Profile)
                : View(g, seat).PreviewNavigationGroup(((ConfigureMotionNavigationGroupCommand)input).Entities, ((ConfigureMotionNavigationGroupCommand)input).Profile).Blocker;
            c.Equal(typed, g.Seat(seat).Preview(input), "typed and ordinary navigation admission share one evaluator");
            return typed;
        }
        private static MotionNavigationSnapshot Selection(SimGame g, EntityId id)
        {
            var refusal = g.Observer.Feature<MotionView>().ReadNavigation(id, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual observer selection absent/" + refusal);
            return value;
        }
        private static IReadOnlyList<WorkJobView> ObserverJobs(SimGame g)
        { var rows = new List<WorkJobView>(); g.Observer.Feature<WorkView>().CopyJobs(rows); return rows; }
        private static Fix Stock(SimGame g, EntityId id, ResourceId resource)
        {
            var refusal = g.Observer.Feature<LogisticsView>().ReadDiagnostics(id, resource, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual observer stock absent/" + refusal);
            return value.Stock;
        }
        private static Fix Total(SimGame g, EntityId[] inventories, ResourceId resource)
        { Fix sum = Fix.Zero; foreach (var id in inventories) sum += Stock(g, id, resource); return sum; }
        private static void Reach(Check c, SimGame g, SimGame twin, Func<bool> done, EntityId[] inventories, ResourceId resource, string label)
        {
            Console.WriteLine("NAV_STAGE enter=" + label + " clock=" + g.Clock.StepIndex); Console.Out.Flush();
            var tail = new Queue<string>();
            for (int i = 0; i < 512 && !done(); i++)
            {
                TransformationContentContractTests.Step(g, twin);
                if (i % 8 == 0)
                {
                    c.Equal(Fix.FromInt(20), Total(g, inventories, resource), "sampled physical native cargo conservation/" + label);
                    var diagnostic = g.Clock.StepIndex + "/jobs=" + string.Join(";", PermanentRemovalTests.Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.Blocker));
                    tail.Enqueue(diagnostic); if (tail.Count > 8) tail.Dequeue();
                    Console.WriteLine("NAV_STAGE waiting=" + label + " " + diagnostic); Console.Out.Flush();
                }
            }
            if (!done()) throw new InvalidOperationException(label + " exhausted512Updates/" + string.Join("|", tail));
            c.Equal(Fix.FromInt(20), Total(g, inventories, resource), "actual physical final conservation/" + label);
            Console.WriteLine("NAV_STAGE done=" + label + " clock=" + g.Clock.StepIndex); Console.Out.Flush();
        }
        private static void Hidden(Check c, SimGame g, EntityId[] machines, EntityId restricted)
        {
            ulong before = g.Hash();
            foreach (var id in machines)
            {
                c.That(!g.Seat(0).View.TryGetEntity(id, out _), "selected sight hides current machine");
                foreach (int seat in new[] { 0, 1 }) foreach (string profile in new[] { "terrain.route", "missing", null })
                    c.Equal(Codes.NotFound, Parity(c, g, seat, Single(id, profile)), "hidden/foreign access precedes profile/provider disclosure");
                c.Equal(Codes.NotFound, View(g).ReadNavigation(id, out var absent), "hidden current policy is unavailable");
                c.That(absent == null, "hidden selection never fabricates empty choices");
                c.Equal(Codes.NotFound, g.Observer.Feature<MotionView>().PreviewNavigation(id, "terrain.route"), "observer diagnosis retains actual owner refusal");
            }
            foreach (int seat in new[] { 0, 1 }) c.Equal(Codes.NotFound, Parity(c, g, seat, Group(machines.Reverse().ToArray(), "terrain.route")), "whole private group refusal");
            var mixed = View(g).PreviewNavigationGroup(new[] { restricted, machines[0] }, "survey.route");
            c.Equal(Codes.NotFound, mixed.Blocker, "mixed group cannot grant a valid prefix");
            c.That(mixed.Entries.Single(x => x.Entity == machines[0]).Selection == null, "hidden entry discloses no profile or allowed metadata");
            c.That(mixed.Entries.Single(x => x.Entity == restricted).Selection != null, "visible owned entry remains an actual detached selection");
            c.Equal(Codes.NotFound, Parity(c, g, 0, Group(new[] { restricted, machines[0] }, "survey.route")), "mixed typed/ordinary evaluator agrees");
            c.Equal(before, g.Hash(), "all private previews and observations are pure");
        }
        private static void Healthy(Check c, bool half)
        {
            Console.WriteLine("NAV_STAGE setup half=" + half); Console.Out.Flush();
            var f = Factory(half); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            Console.WriteLine("NAV_STAGE factory-ready half=" + half); Console.Out.Flush();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current private navigation identity");
            c.That(!g.Modules.Contains("sim.tycoon"), "physical route/work needs no Tycoon");
            var machines = g.Seat(0).Snapshot.Entities.Where(x => x.Def == "survey.machine").Select(x => x.Id).OrderBy(x => x).ToArray();
            var source = g.Seat(0).Snapshot.Entities.Single(x => x.Def == "survey.site").Id;
            var destination = g.Seat(0).Snapshot.Entities.Single(x => x.Def == "fleet.destination").Id;
            var restricted = g.Seat(0).Snapshot.Entities.Single(x => x.Def == "fleet.restricted").Id;
            c.That(machines.Length == 2 && g.Seat(0).View.TryFindResource("steel", out _), "actual native authored fleet and resource");
            g.Seat(0).View.TryFindResource("steel", out var resource); var inventories = machines.Concat(new[] { source, destination, restricted }).ToArray();
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            foreach (var id in machines.Concat(new[] { restricted })) PermanentRemovalTests.Submit(c, g, new SetWorkerHeldCommand { Worker = id, Held = true });
            Console.WriteLine("NAV_STAGE held half=" + half); Console.Out.Flush();
            foreach (var id in machines)
            {
                c.Equal(Code.None, Parity(c, g, 0, Single(id, "terrain.route")), "visible actual route admission");
                c.Equal(MotionNavigationCodes.NotAllowed, Parity(c, g, 0, Single(id, "missing")), "visible invalid profile has typed refusal");
                c.Equal(Codes.NotFound, Parity(c, g, 1, Single(id, "terrain.route")), "foreign command hides navigation metadata");
            }
            c.Equal(Codes.NotFound, Parity(c, g, 0, Single(EntityId.None, "missing")), "missing target refuses before profile");
            c.Equal(MotionNavigationCodes.Unsupported, Parity(c, g, 0, Single(source, "terrain.route")), "visible non-motion target is typed unsupported");
            foreach (EntityId[] invalid in new[] { Array.Empty<EntityId>(), new[] { machines[0], machines[0] }, new[] { EntityId.None } })
                c.Equal(Codes.InvalidArgument, Parity(c, g, 0, Group(invalid, "terrain.route")), "malformed group shape refuses");
            c.Equal(MotionNavigationCodes.NotAllowed, Parity(c, g, 0, Group(machines.Concat(new[] { restricted }).ToArray(), "terrain.route")), "limited actual member refuses whole change");
            c.Equal(Code.None, Parity(c, g, 0, Group(machines.Reverse().ToArray(), "terrain.route")), "unordered valid group uses stable canonical admission");
            Console.WriteLine("NAV_STAGE visible-parity half=" + half); Console.Out.Flush();
            PermanentRemovalTests.Submit(c, g, Group(machines.Reverse().ToArray(), "terrain.route"));
            foreach (var id in machines) c.Equal("terrain.route", Selection(g, id).Profile, "ordinary group commits existing motion owner");
            var retained = Selection(g, machines[0]);
            PermanentRemovalTests.Submit(c, g, new SetWorkerHeldCommand { Worker = machines[0], Held = false });
            PermanentRemovalTests.Submit(c, g, new HaulResourcesCommand { Source = source, Destination = destination, Worker = machines[0], Resource = resource, Ceiling = Fix.FromInt(8), Priority = 100 });
            Reach(c, g, null, () => Stock(g, machines[0], resource).IsPositive &&
                g.Observer.Feature<MotionView>().ReadExecution(machines[0], out var travel).IsNone && travel.Status == MoveStatus.Moving, inventories, resource, "loaded physical departure");
            var job = PermanentRemovalTests.Jobs(g).Single(x => x.Worker == machines[0] && x.State != JobState.Done).Id;
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true }); Hidden(c, g, machines, restricted);
            Console.WriteLine("NAV_STAGE hidden-parity half=" + half); Console.Out.Flush();
            var pending = new List<(int Seat, int Sequence)>();
            foreach (int seat in new[] { 0, 1 }) foreach (Command command in new Command[] { Single(machines[0], "survey.route"), Group(machines, "survey.route") })
                pending.Add((seat, g.Seat(seat).Submit(command)));
            Console.WriteLine("NAV_STAGE pending-save half=" + half); Console.Out.Flush();
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "hidden loaded travel and real pending command inbox save together");
            Console.WriteLine("NAV_STAGE loaded-pending half=" + half); Console.Out.Flush();
            Hidden(c, twin, machines, restricted); g.Clock.Pause(); twin.Clock.Pause(); long age = g.Clock.StepIndex;
            TransformationContentContractTests.Step(g, twin); c.Equal(age, g.Clock.StepIndex, "paused sole clock consumes no travel");
            g.Clock.Resume(); twin.Clock.Resume(); TransformationContentContractTests.Step(g, twin);
            foreach (var issue in pending)
            {
                long cursor = 0; var answers = new List<CommandResult>(); g.Seat(issue.Seat).ReadResults(ref cursor, answers);
                var answer = answers.Single(x => x.Sequence == issue.Sequence);
                c.That(!answer.Accepted && answer.Refusal == Codes.NotFound, "real hidden/foreign single/group input refuses without prefix commit");
            }
            foreach (var id in machines) c.Equal("terrain.route", Selection(g, id).Profile, "refused input preserves actual selected route");
            c.Equal(Fix.FromInt(20), Total(g, inventories, resource), "refused navigation creates or loses no physical cargo");
            c.That(ObserverJobs(g).Any(x => x.Id == job && x.Worker == machines[0]), "authorized observer verifies the same physical hauling job while worker is hidden");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false }); twin = f.Load(g.Save());
            c.Equal(Code.None, Parity(c, g, 0, Group(machines, "survey.route")), "reveal restores ordinary group control");
            PermanentRemovalTests.Submit(c, g, Group(machines, "survey.route")); twin = f.Load(g.Save());
            c.Equal("terrain.route", retained.Profile, "retained immutable selection does not become current authority");
            c.Throws<NotSupportedException>(() => ((IList<string>)retained.Allowed).Clear(), "retained profile metadata is readonly");
            Reach(c, g, twin, () => Stock(g, destination, resource) == Fix.FromInt(8) && Stock(g, machines[0], resource).IsZero, inventories, resource, "delivered native cargo");
            c.Equal(Fix.FromInt(12), Stock(g, source, resource), "actual finite source charged once");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled route/cargo state durable");
            c.Equal(isolated, idle.Hash(), "interleaved session remains independent");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "ordinary grouped route/private refusals/physical delivery replay");
            Console.WriteLine("NAV_STAGE complete half=" + half); Console.Out.Flush();
        }
        private static void Run(Check c) { Healthy(c, false); Healthy(c, true); }
    }
}

