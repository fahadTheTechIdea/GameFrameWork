using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class NativeTeamAcquisitionTests
    {
        internal const string Case = "Native team acquisition: complete commitments, refusal purity and physical saved continuation";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Incident = new Code("damage");
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id);
        private static EntityView Visible(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == id && e.Owner == Company);
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static List<WorkJobView> Jobs(SimGame game) { var jobs = new List<WorkJobView>(); Work(game).CopyJobs(jobs); return jobs; }
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        { var nodes = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(order, nodes)) throw new InvalidOperationException("missing visible nodes"); return nodes; }
        private static OrderView ReadOrder(SimGame game, int id)
        { var rows = new List<OrderView>(); Work(game).CopyOrders(rows); return rows.Single(o => o.Id == id); }
        private static bool Completed(SimGame game, OrderView order)
            => Work(game).TryReadActivityCompletion(order.Plan, order.Target, out var fact) && fact.Order == order.Id;
        private static Fix Stock(SimGame game, EntityId id, string resource)
        {
            var facts = new List<Facet>(); if (!game.Seat(0).View.Inspect(id, facts)) throw new InvalidOperationException("stock private or missing");
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == resource).Aggregate(Fix.Zero, (a, f) => a + f.Value);
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("team fault at " + game.Tick + "/" + game.Fault);
            if (twin == null) return;
            twin.Update(); if (twin.Fault != null || twin.Hash() != game.Hash()) throw new InvalidOperationException("team twin divergence at " + game.Tick + "/" + twin.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 2000 && watch.ElapsedMilliseconds < 20000 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + "/step=" + game.Tick + "/jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/worker=" + j.Worker + "/toil=" + j.ToilIndex + "/" + j.Blocker + "/" + j.DestinationDockSlot)) +
                "/positions=" + string.Join(";", game.World.Entities.All.Where(e => e.Alive && e.Get<PointPresence>() != null)
                    .Select(e => e.Def.Id + "#" + e.Id + "/" + e.Get<PointPresence>().Position + "/" + e.Get<IMotion>()?.Status)));
        }
        private static SimGame New(GameFactory factory, string scenario = "points")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 284 };
            if (scenario == "points") request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static CommandResult Order(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "engine previews ordinary player intent");
            c.Equal(hash, game.Hash(), "preview mutates no native commitments");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary command accepted: " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("player refused " + result.Refusal); return result;
        }
        private static int Raise(Check c, SimGame game, string target = "destination", string kind = "proof.failure")
        {
            var id = Visible(game, target).Id;
            Order(c, game, new RaiseIncidentCommand { Target = id, Source = Incident, Kind = new Code(kind) });
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetResponseOrder(id, Incident, out int order))
                throw new InvalidOperationException("missing actual incident response order");
            return order;
        }
        private static void Hold(Check c, SimGame game, string worker, bool held)
            => Order(c, game, new SetWorkerHeldCommand { Worker = Visible(game, worker).Id, Held = held });
        private static WorkWorkerCandidate Candidate(SimGame game, Job job, Entity worker)
        {
            var order = Board(game).Order(job.OrderId); var execution = order.Node(job.NodeId); var definition = order.Plan.Graph.Find(job.NodeId);
            var candidates = new List<WorkWorkerCandidate>();
            game.World.Services.Get<IWorkerDispatch>().CopyCandidates(game.World, job.Owner, definition.Roles,
                Find(game, "destination"), definition, job.Id, execution.Choices, execution, WorkCandidateMode.IdleOnly, candidates, null);
            return candidates.Single(c => c.Worker == worker.Id && c.Result.Status == WorkRequirementStatus.Satisfied);
        }
        private static (SimGame Game, Order Order, Job[] Jobs, WorkWorkerCandidate[] Candidates, Code?[] Bays) Prepared(Check c, GameFactory factory)
        {
            // Diagnostic acquisition protocol only: ordinary commands create real jobs, then existing
            // engine worker-control service releases held actors without advancing time or inventing jobs.
            var game = New(factory); Hold(c, game, "machine", true); Hold(c, game, "prepared.worker.1", true);
            int id = Raise(c, game); var order = Board(game).Order(id);
            foreach (string name in new[] { "machine", "prepared.worker.1" })
                game.World.Services.Get<IWorkerControl>().SetHeld(game.World, Company, Find(game, name).Id, false);
            var jobs = new[] { Board(game).Job(order.Node("a.flex").JobId), Board(game).Job(order.Node("b.inspect").JobId) };
            var candidates = new[] { Candidate(game, jobs[0], Find(game, "machine")), Candidate(game, jobs[1], Find(game, "prepared.worker.1")) };
            c.That(jobs.All(j => j.State == JobState.Open) && order.Nodes.All(n => n.Resources.Count == 0), "diagnostic starts from actual open uncommitted authored jobs");
            c.That(game.Seat(0).View.Feature<IncidentsView>().ReadPreparedness(Find(game, "destination").Id, Incident, out var facts).IsNone && facts.ReadyForFrontier, "original joint admission returns feasible actual service intent");
            var bays = candidates.Select(c => (Code?)facts.FrontierDocks.Single(d => d.Worker == c.Worker).Slot).ToArray();
            return (game, order, jobs, candidates, bays);
        }
        private static void Refuse(Check c, SimGame game, int[] jobs, WorkWorkerCandidate[] candidates, Code?[] bays, string reason)
        {
            ulong hash = game.Hash();
            c.That(!Board(game).Assign(jobs, candidates, bays), reason + " whole native acquisition refuses");
            c.Equal(hash, game.Hash(), reason + " no prefix worker/dock/material/budget/sequence mutation");
        }
        private static void Protocol(Check c)
        {
            var factory = FrontierDockFeasibilityTests.Fixture(two: true, constrained: true); var data = Prepared(c, factory);
            var game = data.Game; var ids = data.Jobs.Select(j => j.Id).ToArray(); var target = Find(game, "destination");
            var port = target.Get<IOutletPort>(); var supply = game.World.Content.ResourceId("supply");
            Refuse(c, game, new[] { ids[0], ids[0] }, data.Candidates, data.Bays, "duplicate job identity");
            Refuse(c, game, ids, new[] { data.Candidates[0], data.Candidates[0] }, data.Bays, "duplicate physical actor");
            Refuse(c, game, new[] { ids[0], int.MaxValue }, data.Candidates, data.Bays, "missing later job");
            Refuse(c, game, ids, data.Candidates, new[] { data.Bays[0], data.Bays[0] }, "overlapping or policy-invalid later bay");
            Refuse(c, game, ids, data.Candidates, new Code?[] { data.Bays[0], new Code("missing") }, "missing later exact bay");
            var owner = new ReservationOwner("proof.team-competitor", 1); var held = port.ReserveOut(owner, supply, Fix.FromInt(3));
            Refuse(c, game, ids, data.Candidates, data.Bays, "late aggregate shortage despite individually fitting four-unit demands");
            c.That(data.Jobs.All(j => j.State == JobState.Open) && data.Order.Nodes.All(n => n.Resources.Count == 0), "resource refusal leaves both real jobs/nodes uncommitted");
            c.That(port.Reservations.Count == 1 && port.Reservations[0].Token.Equals(held), "no substitute claims or competitor cancellation");
            port.ReleaseOut(owner, held);
            var twin = factory.Load(game.Save()); ulong before = game.Hash();
            c.That(Board(game).Assign(ids, data.Candidates, data.Bays), "complete native machine/dock/resource team acquisition commits");
            c.That(Board(twin).Assign(ids.Reverse().ToArray(), data.Candidates.Reverse().ToArray(), data.Bays.Reverse().ToArray()), "reversed actual native set also commits through same owner");
            c.Equal(game.Hash(), twin.Hash(), "canonical native receipts and original actor/job/dock state independent of set arrival order");
            c.That(game.Hash() != before && data.Jobs.All(j => j.State == JobState.Assigned), "all real jobs committed, not retained ready previews");
            c.That(data.Jobs.Select(j => j.Worker).Distinct().Count() == 2 && data.Jobs.All(j => !j.DestinationDockSlot.IsNone), "two distinct native machines own real disjoint productive bays");
            c.That(data.Order.Nodes.Take(2).All(n => n.Resources.Count == 1 && n.Resources[0].Receipts.Count == 1), "original two node bindings own every actual resource token once");
            c.Equal(Fix.FromInt(8), port.ReservedOut(supply), "whole eight-unit materials committed together");
            c.Equal(Fix.FromInt(8), port.Amount(supply), "acquisition cannot perform productive work instantly");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "no team store or extra codec required for live commitments");
            Refuse(c, game, ids, data.Candidates, data.Bays, "already assigned retained team");
        }
        private static void TeamHome(Beep.SimEngine.Content.JsonValue root)
        {
            var home = Beep.SimEngine.Content.Json.Parse(FrameworkProofContent.Read("prepared-team-home.json"));
            root.Get("entities").Add(home);
            StartupContentAuthoring.Add(root, "prepared.east-home", "prepared.east-home", 0, home.Get("components").Get("point_presence"));
        }
        private static void AuthorPosition(Beep.SimEngine.Content.JsonValue root, string actor, string position)
        {
            var actors=root.Get("entities"); for(int i=0;i<actors.Count;i++)
                if(actors[i].Get("id").Text==actor)
                    actors[i].Get("components").Get("point_presence").Set("position",Content.JsonValue.Number(position));
            var setups=root.Get("entity_setups"); for(int i=0;i<setups.Count;i++)
                if(setups[i].Get("entity").Text==actor)
                    setups[i].Get("placement").Set("position",Content.JsonValue.Number(position));
        }
        private static void Physical(Check c)
        {
            // Normal acceptance below uses only public player inputs, views and loop/save/replay APIs.
            var factory = FrontierDockFeasibilityTests.Fixture(two: true, constrained: true, edit: TeamHome); var game = New(factory);
            foreach (string worker in new[] { "machine", "prepared.worker.1" })
                Order(c, game, new SetHomeCommand { Worker = Visible(game, worker).Id,
                    Home = Visible(game, worker == "machine" ? "prepared.east-home" : "source").Id });
            using (var recorder = game.RecordReplay(Limits))
            {
                int order = Raise(c, game); var nodes = Nodes(game, order); var first = nodes.Where(n => n.Id != "restart").ToArray();
                var jobs = Jobs(game).Where(j => first.Any(n => n.JobId == j.Id)).ToArray();
                c.That(jobs.Length == 2 && jobs.All(j => j.State == JobState.Assigned), "ordinary same Update dispatch commits complete authored native team");
                c.That(first.All(n => n.Progress.Equals(WorkProgress.Zero)) && Stock(game, Visible(game, "destination").Id, "supply") == Fix.FromInt(8), "travel follows real acquisition before productive consumption");
                c.That(jobs.Select(j => j.Worker).Distinct().Count() == 2 && jobs.Select(j => j.DestinationDockSlot).Distinct().Count() == 2, "no duplicate physical actor or overlapping bay assignment");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved fully acquired travelling team matches");
                long step = game.Tick; ulong hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(game.Tick == step && game.Hash() == hash, "one paused clock freezes all team physical work");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => Nodes(game, order).Where(n => n.Id != "restart").All(n => n.Progress.WholeTicks > 0), "both native machines productive", twin);
                c.That(Stock(game, Visible(game, "destination").Id, "supply") < Fix.FromInt(8), "real resources consumed only after physical productive arrival");
                twin = factory.Load(game.Save()); var completion = ReadOrder(game, order);
                Until(game, () => Completed(game, completion), "parallel work/physical departure/dependent restart", twin);
                c.Equal(Fix.Zero, Stock(game, Visible(game, "destination").Id, "supply"), "native team consumes eight actual units exactly once");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "public native team replay reproduces actual saved physical work: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void OptionalBudgets(Check c)
        {
            var factory = WorkBudgetTests.Factory(160, incidents: true, physicalDocuments: new[] { "prepared-frontier-costs.json" }, startupProfiles: new[] { "prepared-frontier-costs.start" },
                physicalEdit: root => root.Get("scenarios")[0].Set("seats", Content.JsonValue.Number("2")),
                workEdit: root => AuthorPosition(root,"budget.worker-a","22"));
            var game = New(factory, "purchase-haul");
            Order(c, game, new SetHomeCommand { Worker = Visible(game, "budget.worker-a").Id, Home = Visible(game, "budget.site-b").Id });
            Order(c, game, new SetHomeCommand { Worker = Visible(game, "budget.worker-b").Id, Home = Visible(game, "source").Id });
            using (var recorder = game.RecordReplay(Limits))
            {
                int order = Raise(c, game, "frontier.site", "frontier.failure");
                c.That(Nodes(game, order).Take(2).All(n => Jobs(game).Any(j => j.Id == n.JobId && j.State == JobState.Assigned)), "authored optional funded native team admitted together");
                c.Equal(Code.None, game.Seat(0).View.Feature<BusinessView>().ReadAccount(Company, out var account), "original optional account publishes actual commitments");
                c.That(account.Balance == Fix.FromInt(160) && account.Reserved == Fix.FromInt(160), "whole account holds four authored forty-unit occurrences without early payment");
                var twin = factory.Load(game.Save()); var completion = ReadOrder(game, order);
                Until(game, () => Completed(game, completion), "optional funded team physical completion", twin);
                c.Equal(Code.None, game.Seat(0).View.Feature<BusinessView>().ReadAccount(Company, out account), "original optional account after productive work");
                c.That(account.Balance == Fix.Zero && account.Reserved == Fix.Zero, "physical nodes spend exact original holds once, never purchase or fabricate work");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "native work budget acquisition/payment uses original account save/replay: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void Fault(Check c)
        {
            var factory = FrontierDockFeasibilityTests.Fixture(two: true); var game = New(factory);
            var other = New(factory); ulong otherHash = other.Hash(); int acquired = 0;
            Find(game, "destination").Get<SingleResourcePort>().AfterReserveOut = () => { if (++acquired == 2) throw new InvalidOperationException("proof.team.second-acquisition"); };
            game.Seat(0).Submit(new RaiseIncidentCommand { Target = Visible(game, "destination").Id, Source = Incident, Kind = new Code("proof.failure") });
            game.Update();
            c.That(game.Fault != null && acquired == 2, "real second acquisition provider fault crosses original Update boundary");
            c.That(Find(game, "machine").Get<IWorker>().Idle && Find(game, "prepared.worker.1").Get<IWorker>().Idle, "no acquired worker prefix before complete resource verification");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted partial provider book cannot save");
            c.Throws<InvalidOperationException>(() => game.Hash(), "faulted acquisition cannot masquerade as complete checkpoint");
            c.Equal(otherHash, other.Hash(), "faulted engine-side provider does not affect another session");
        }
        private static void EmergencyTeam(Check c)
        {
            var factory = FrontierDockFeasibilityTests.Fixture(two:true,constrained:true,fleet:new[]{3,2},edit:root=>{
                TeamHome(root); AuthorPosition(root,"machine","14");
            });
            var game = New(factory);
            foreach (string worker in new[] { "machine", "prepared.worker.1" })
                Order(c, game, new ConfigureWorkPolicyCommand { Worker = Visible(game, worker).Id,
                    Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect") },
                        worker=="prepared.worker.1"?"frontier.inspector":null, true, true, WorkAssignmentMode.All) });
            foreach (string worker in new[] { "machine", "prepared.worker.1" })
                Order(c, game, new SetHomeCommand { Worker=Visible(game,worker).Id,
                    Home=Visible(game,worker=="machine"?"prepared.east-home":"source").Id });
            Order(c, game, new ConfigureCompanyAlertCommand { Company = Company,
                Settings = new CompanyAlertSettings("alerts.neutral", CompanyAlertLevel.Normal, Array.Empty<WorkerReserveSetting>()) });
            var a = Order(c, game, new PostOrderCommand { Plan = "preempt.travel", Target = Visible(game, "source").Id, Priority = 100 });
            var b = Order(c, game, new PostOrderCommand { Plan = "preempt.travel", Target = Visible(game, "destination").Id, Priority = 100 });
            var before = Jobs(game).Where(j => j.State == JobState.Assigned).ToArray();
            c.That(before.Length == 2 && before.Select(j => j.Worker).Distinct().Count() == 2, "two ordinary physical incumbents exist before emergency team");
            using (var recorder = game.RecordReplay(Limits))
            {
                int response = Raise(c, game); var members = Nodes(game, response).Where(n => n.Id != "restart").ToArray();
                c.That(members.Length == 2 && members.All(n => n.Worker.IsValid), "native emergency set reacquires both actual interrupted actors");
                c.That(members.Select(n => n.Worker).OrderBy(x => x.Value).SequenceEqual(before.Select(j => j.Worker).OrderBy(x => x.Value)), "emergency team never invents or substitutes workers");
                c.That(Jobs(game).Where(j => before.Any(old => old.Id == j.Id)).All(j => j.State != JobState.Assigned && !j.Worker.IsValid), "original interruption relinquishes old worker commitments");
                // Player emergency policy keeps completed responders from resuming
                // opposing ordinary traffic through the still-active response bay.
                foreach (string worker in new[] { "machine", "prepared.worker.1" })
                    Order(c, game, new ConfigureWorkPolicyCommand { Worker=Visible(game,worker).Id,
                        Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("inspect")},
                            worker=="prepared.worker.1"?"frontier.inspector":null,true,true,WorkAssignmentMode.EmergencyOnly) });
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "interrupted native team persists through original job/node codecs");
                var completion = ReadOrder(game, response);
                Until(game, () => Completed(game, completion), "interrupted team physical response", twin);
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "native emergency whole-set interruption/acquisition replay: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void Hauling(Check c)
        {
            var factory = Colony.Factory(); var game = Colony.New(284);
            Order(c, game, new ConstructCommand { Def = "tower", Priority = 50, Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
            Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "actual loaded construction hauling");
            var twin = factory.Load(game.Save());
            Until(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "ordinary delivered staged construction", twin);
            Fix steel = Fix.Zero; foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == Company)) steel += Stock(game, entity.Id, "steel");
            c.Equal(Fix.FromInt(20), steel, "unchanged native carrier/work execution conserves50 minus30 steel");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "native team acquisition source identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no new native state owner or saved layout");
            Protocol(c); Physical(c); OptionalBudgets(c); Fault(c); EmergencyTeam(c); Hauling(c);
        }
    }
}
