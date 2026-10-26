using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class WorkerRoleTests
    {
        public static void Register()
        {
            TestRunner.Add("Worker roles: stable typed identities and one declared authority", Identity);
            TestRunner.Add("Worker roles: external authored role executes physical work and restores", ExternalRole);
            TestRunner.Add("Worker roles: missing, malformed and duplicate references refuse content", Invalid);
            TestRunner.Add("Worker roles: unstaffed timed steps require no invented role", Timed);
        }

        private static void Identity(Check c)
        {
            var crane = new WorkerRoleId("crane");
            c.That(crane.IsValid && !WorkerRoleId.None.IsValid, "None is distinct from a worker requirement");
            c.Equal(crane, new WorkerRoleId("crane"), "identity does not depend on content-table position");
            c.Equal(0, crane.CompareTo(new WorkerRoleId("crane")), "stable equality and ordering agree");
            c.That(new WorkerRoleId("module.fabricate") != crane, "module-authored identities are not built-in enum values");
            c.Throws<ArgumentException>(() => new WorkerRoleId(""), "empty identities are invalid, not timed work");
            c.Throws<ArgumentException>(() => new WorkerRoleId("Crane"), "case aliases cannot create implicit roles");
            c.Throws<ArgumentException>(() => new WorkerRoleId("crane worker"), "whitespace aliases are invalid");
            SimGame g = Colony.New();
            Entity worker = g.World.Entities.Get(Colony.Mine(g, 0, "crane")[0].Id);
            c.Equal(crane, g.World.Content.Get<WorkerRoleDef>("crane").Role, "the existing content registry resolves the typed identity");
            c.Equal(crane, worker.Get<IWorkerRoles>().Roles[0], "worker exposes resolved identities");
            c.That(worker.Get<IWorkerRoles>().Can(crane) && !worker.Get<IWorkerRoles>().Can(WorkerRoleId.None), "capability matching is typed");
            var roles = (IList<WorkerRoleId>)worker.Def.Component<WorkerDef>().Roles;
            c.Throws<NotSupportedException>(() => roles[0] = LogisticsService.HaulRole, "authored role lists cannot be mutated through collection casts");
            WorkBoard board = g.World.Services.Get<WorkBoard>();
            c.Throws<ArgumentException>(() => board.AddJob(new PlayerId(0), WorkerRoleId.None, 1, worker.Id), "machine jobs cannot invent an unstaffed role");
            c.Throws<ContentException>(() => board.AddJob(new PlayerId(0), new WorkerRoleId("missing"), 1, worker.Id), "programmatic jobs validate the same content authority");
            Job job = board.AddJob(new PlayerId(0), crane, 1, worker.Id);
            c.Equal(1, job.Id, "refused jobs consume neither identity nor board entries");
            job.Role = new WorkerRoleId("missing");
            c.Throws<CorruptStateException>(() => Colony.Factory().Load(g.Save()), "restoration refuses an undeclared job role instead of silently substituting one");
        }

        private static string RoleContent()
            => Colony.Defs.Replace("{ \"id\": \"haul\" }", "{ \"id\": \"haul\" }, { \"id\": \"module.fabricate\" }")
                .Replace("\"roles\": [\"crane\"]", "\"roles\": [\"module.fabricate\"]")
                .Replace("\"role\": \"crane\"", "\"role\": \"module.fabricate\"");

        private static void ExternalRole(Check c)
        {
            GameFactory factory = Colony.Factory(RoleContent());
            SimGame g = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            var role = new WorkerRoleId("module.fabricate");
            World w = g.World;
            Entity worker = ((global::Beep.SimEngine.Maps.TileMap)w.Space).SpawnMobile(w, w.Content.Get<EntityDef>("module_worker"), new PlayerId(0), FixVec2.Centre(new TileCoord(16, 5)));
            w.Entities.Flush();
            c.That(worker.Get<Worker>() == null && worker.Get<IWorkerRoles>().Can(role), "non-friend worker supports a newly authored role without a built-in Worker");
            c.That(!w.Entities.Get(Colony.Mine(g, 0, "crane")[0].Id).Get<IWorkerRoles>().Can(role), "an unrelated machine cannot satisfy the new requirement");
            c.Equal(1, g.Seat(0).View.Feature<WorkView>().IdleWorkers(role), "management queries use the same role contract");
            CommandResult result = TestKit.Order(g, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(g.World.Space.Id, new TileCoord(10, 16)), Priority = 50 });
            c.That(result.Accepted, "public construction command accepts authored role requirements");
            c.Equal(0, Colony.Mine(g, 0, "tower").Count, "role matching cannot bypass travel or construction time");
            var orders = new List<OrderView>();
            g.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            c.Equal(role, orders[0].CurrentRole, "public facts report the authoritative typed work requirement");
            Job active = w.Services.Get<WorkBoard>().Job(worker.Get<IWorker>().Job);
            c.That(active != null && active.Role == role && active.Worker == worker.Id, "the existing job board reserves the compatible external worker");
            g.RunTicks(20);
            SimGame restored = factory.Load(g.Save());
            c.Equal(g.Hash(), restored.Hash(), "typed job requirements restore through the existing save owner");
            int elapsed = TestKit.RunUntil(g, () => Colony.Mine(g, 0, "tower").Count == 1, checked((int)TestGame.Time.Days(1)), 1);
            restored.RunTicks(elapsed);
            c.Equal(1, Colony.Mine(g, 0, "tower").Count, "new role completes construction through unchanged standard scheduling");
            c.That(elapsed + 21 >= checked((int)TestGame.Time.Hours(2)), "external work-rate still consumes physical game time");
            c.Equal(g.Hash(), restored.Hash(), "saved physical work continues deterministically with stable role identities");
            c.Equal(1, g.Seat(0).View.Feature<WorkView>().IdleWorkers(role), "completed work releases the external role provider");
        }

        private static void Invalid(Check c)
        {
            void Refuse(string definitions, string what)
                => c.Throws<ContentException>(() => Colony.Factory(definitions).NewGame(new NewGameRequest { Scenario = "colony" }), what);
            Refuse(Colony.Defs.Replace("{ \"id\": \"crane\" }, ", ""), "missing role declarations fail before play");
            Refuse(Colony.Defs.Replace("\"roles\": [\"crane\"]", "\"roles\": [\"missing\"]"), "built-in workers refuse unknown roles");
            Refuse(Colony.Defs.Replace("\"contract_worker\": { \"role\": \"crane\"", "\"contract_worker\": { \"role\": \"missing\""), "external workers resolve through the same declared-role authority");
            Refuse(Colony.Defs.Replace("\"id\": \"prep\", \"roles\": [\"crane\"]", "\"id\": \"prep\", \"roles\": [\"missing\"]"), "construction requirements refuse unknown roles");
            Refuse(Colony.Defs.Replace("\"id\": \"look\", \"roles\": [\"crane\"]", "\"id\": \"look\", \"roles\": [\"missing\"]"), "order requirements refuse unknown roles");
            Refuse(Colony.Defs.Replace("\"id\": \"demolish\", \"roles\": [\"crane\"]", "\"id\": \"demolish\", \"roles\": [\"missing\"]"), "generated demolition requirements refuse unknown roles");
            Refuse(Colony.Defs.Replace("\"owned\": false, \"role\": \"crane\"", "\"owned\": false, \"role\": \"missing\""), "line requirements refuse unknown roles");
            Refuse(Colony.Defs.Replace("\"roles\": [\"crane\"]", "\"roles\": [\"crane\", \"crane\"]"), "duplicate worker memberships are refused");
            Refuse(Colony.Defs.Replace("{ \"id\": \"haul\" }", "{ \"id\": \"haul\" }, { \"id\": \"crane\" }"), "duplicate declared role ownership is refused");
            Refuse(Colony.Defs.Replace("{ \"id\": \"haul\" }", "{ \"id\": \"haul\" }, { \"id\": \"Bad Role\" }"), "malformed declarations are explicit content errors");
            Refuse(Colony.Defs.Replace("\"roles\": [\"crane\"]", "\"roles\": [\"crane\"], \"work_rate\": 0"), "nonpositive worker rates cannot create never-finishing jobs");
        }

        private static void Timed(Check c)
        {
            string definitions = Colony.Defs.Replace("\"id\": \"prep\", \"roles\": [\"crane\"], \"hours\": 2", "\"id\": \"prep\", \"hours\": 2");
            SimGame g = Colony.Factory(definitions).NewGame(new NewGameRequest { Scenario = "colony" });
            c.Equal(0, g.World.Content.Get<EntityDef>("tower").Component<ConstructibleDef>().Graph.Nodes[0].Roles.Count, "authored timed work explicitly has no machine requirement");
            c.That(TestKit.Order(g, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(g.World.Space.Id, new TileCoord(10, 16)), Priority = 50 }).Accepted, "mixed timed/machine plan is accepted");
            var orders = new List<OrderView>();
            g.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            c.Equal(WorkerRoleId.None, orders[0].CurrentRole, "timed work view does not fabricate a worker role");
            c.Equal(1, g.Seat(0).View.Feature<WorkView>().IdleWorkers(new WorkerRoleId("crane")), "timed step reserves no machine");
            g.RunTicks(checked((int)TestGame.Time.Hours(1)));
            c.Equal(0, Colony.Mine(g, 0, "tower").Count, "timed work does not finish immediately");
            TestKit.RunUntil(g, () => Colony.Mine(g, 0, "tower").Count == 1, checked((int)TestGame.Time.Days(1)));
            c.Equal(1, Colony.Mine(g, 0, "tower").Count, "timed stage releases its dependent typed machine stage");
        }
    }
}
