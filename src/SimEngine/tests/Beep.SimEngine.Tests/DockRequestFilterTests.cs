using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static class DockRequestFilterTests
    {
        internal const string Case = "Dock request filters: single authored predicate and early invalid binding refusal";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "haul.competition", Seed = 19, Seats = { Controller.Human, Controller.Human } });
        private static void Run(Check c)
        {
            var game = New(AtomicHaulTests.CompetitionFactory()); var content = game.World.Content;
            var dock = content.Get<EntityDef>("competition.source").Component<IWorkDockDefinition>();
            var role = content.Get<IWorkerRoleDefinition>("haul").Role; var resource = content.ResourceId("supply");
            var work = WorkDockRequest.Work(role); var load = WorkDockRequest.Haul(resource, ResourceHandlingPhase.Loading);
            var unload = WorkDockRequest.Haul(resource, ResourceHandlingPhase.Unloading);
            var workBay = dock.Slots.Single(slot => slot.Id == new Code("a.work-only"));
            var resourceBay = dock.Slots.Single(slot => slot.Id == new Code("service"));
            c.That(workBay.Matches(work) && !workBay.Matches(load) && !workBay.Matches(unload), "authored role bay admits work, not resource handling by the same worker role");
            c.That(!resourceBay.Matches(work) && resourceBay.Matches(load) && resourceBay.Matches(unload), "resource bay admits actual loading/unloading requests only");
            c.That(!default(WorkDockSlot).Matches(load) && !workBay.Matches(default), "unbound slot/request never matches by fabricated default");
            c.That(!workBay.Matches(WorkDockRequest.Work(new WorkerRoleId("absent.role"))) &&
                !resourceBay.Matches(WorkDockRequest.Haul(new ResourceId(int.MaxValue), ResourceHandlingPhase.Loading)), "filters compare exact resolved identities");
            var exit = dock.Slots.Single(slot => slot.Kind == WorkDockSlotKind.Departure);
            c.That(exit.Matches(work) && exit.Matches(load), "unrestricted slot inherits endpoint admission, not a new alternate policy");
            c.Throws<ArgumentException>(() => new WorkDockSlot(new Code("invalid"), WorkDockSlotKind.Service, role, resource), "role and resource cannot be combined implicitly");
            foreach (string invalid in new[] { "blank", "both", "unknown-role", "unknown-resource", "foreign-role", "no-resource-service", "no-resource-exit" })
                c.Throws<ContentException>(() => New(AtomicHaulTests.CompetitionFactory(root => {
                    var parts = root.Get("entities")[1].Get("components").Get("point_dock");
                    var rows = parts.Get("slots"); var workRow = rows[0]; var resourceRow = rows[1];
                    if (invalid == "blank") workRow.Set("work_role", JsonValue.Of(""));
                    if (invalid == "both") workRow.Set("resource", JsonValue.Of("supply"));
                    if (invalid == "unknown-role") workRow.Set("work_role", JsonValue.Of("absent.role"));
                    if (invalid == "unknown-resource") resourceRow.Set("resource", JsonValue.Of("absent.resource"));
                    if (invalid == "foreign-role") parts.Set("work_roles", JsonValue.NewArray());
                    if (invalid == "no-resource-service") { resourceRow.Remove("resource"); resourceRow.Set("work_role", JsonValue.Of("haul")); }
                    if (invalid == "no-resource-exit") rows[3].Set("work_role", JsonValue.Of("haul"));
                })), "malformed/unsupported authored request refuses before play: " + invalid);
            CorruptCommitments(c);
        }

        // Explicit negative fixture only: tamper with existing job fields to exercise native
        // checkpoint refusal. This is not a player operation or external module capability.
        private static void CorruptCommitments(Check c)
        {
            var factory = AtomicHaulTests.CompetitionFactory(); var game = New(factory);
            var world = game.World; var board = world.Services.Get<WorkBoard>();
            var source = world.Entities.All.Single(e => e.Alive && e.Def.Id == "competition.source");
            var destination = world.Entities.All.Single(e => e.Alive && e.Def.Id == "competition.destination");
            var resource = world.Content.ResourceId("supply");
            var id = world.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), source.Id, destination.Id,
                resource, Fix.FromInt(30), 100);
            var job = board.Job(id); c.That(job != null, "negative probe begins with an actual acquired haul");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "untampered filtered commitment checkpoints");
            job.SourceDockSlot = new Code("a.work-only");
            c.Throws<CorruptStateException>(() => board.ValidateDocks(job, true), "restore refuses resource commitment to work-only service");
            c.Throws<InvalidOperationException>(() => board.ValidateDocks(job, false), "live validation uses the same request predicate");
            c.Throws<InvalidOperationException>(() => game.Save(), "checkpoint cannot publish an incompatible resource bay");

            game = New(factory); world = game.World; board = world.Services.Get<WorkBoard>();
            source = world.Entities.All.Single(e => e.Alive && e.Def.Id == "competition.source");
            var posted = TestKit.Order(game, 0, new PostOrderCommand { Plan = "dock.inspect", Target = source.Id });
            c.That(posted.Accepted, "second negative probe begins with ordinary staffed assignment");
            job = board.Jobs.Single(j => j.IsLive && !j.IsHaul);
            c.Equal(new Code("a.work-only"), job.DestinationDockSlot, "ordinary work first owns the correct authored bay");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "untampered staffed commitment checkpoints");
            job.DestinationDockSlot = new Code("service");
            c.Throws<CorruptStateException>(() => board.ValidateDocks(job, true), "restore refuses work commitment to resource-only service");
            c.Throws<InvalidOperationException>(() => game.Save(), "checkpoint cannot publish an incompatible work bay");
        }
    }
}
