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
    internal static class NativeDispatchPrefixTests
    {
        private const string Case = "Native admission prefixes: alternatives, timed scarcity and physical saved work";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register()
        { TestRunner.Add(Case, Run); TestRunner.Add(PriorityCase, PriorityLocal); TestRunner.Add(RoleCase, RoleCombinations); }
        private static GameFactory Factory(int stock = 8, string document = null)
            => DispatchAgingTests.Factory(profile: "dispatch.priority",
                shipmentDefinitions: document ?? FrameworkProofContent.Read("native-prefix.json"), shipmentDocument: "native-prefix.json",
                edit: root => root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number(stock.ToString())), startupProfiles: new[] { "native-prefix.start" });
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 299,
            Mods = { "proof.shipments" }, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Entity(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Id;
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static ActivityNodeView Node(SimGame game, int order)
        {
            var rows = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(order, rows)) throw new InvalidOperationException("missing native prefix " + order);
            return rows.Single();
        }
        private static Fix Stock(SimGame game, string prototype)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, prototype), rows);
            return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static Fix Total(SimGame game) => Stock(game, "source") + Stock(game, "prefix.first");
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("prefix clock=" + game.Tick + "/fault=" + game.Fault + "/twin=" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string phase, SimGame twin = null)
        {
            for (int i = 0; i < 1200 && !done(); i++) Update(game, twin);
            if (!done())
            {
                var jobs = new List<WorkJobView>(); Work(game).CopyJobs(jobs);
                throw new InvalidOperationException(phase + "/clock=" + game.Tick + "/jobs=" + string.Join(";",
                    jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.Worker)));
            }
        }
        private static CommandResult Answer(SimGame game, int sequence)
        {
            long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            return answers.Single(a => a.Sequence == sequence);
        }
        private static int Submit(Check c, SimGame game, Command command, SimGame twin = null)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary authored/player preview");
            c.Equal(before, game.Hash(), "preview changes no source choice/claims/clock");
            int sequence = game.Seat(0).Submit(command); twin?.Seat(0).Submit(command); return sequence;
        }
        private static CommandResult Command(Check c, SimGame game, Command command, SimGame twin = null)
        {
            int sequence = Submit(c, game, command, twin); Update(game, twin); var answer = Answer(game, sequence);
            c.That(answer.Accepted, "actual ordinary command accepted: " + answer.Refusal);
            if (!answer.Accepted) throw new InvalidOperationException("prefix refusal " + answer.Refusal);
            return answer;
        }
        private static PostOrderCommand Post(SimGame game, string plan, int priority)
            => new PostOrderCommand { Plan = plan, Target = Entity(game, "source"), Priority = priority };
        private static bool Done(SimGame game, string plan, int order)
            => Work(game).TryReadActivityCompletion("activity:" + plan, Entity(game, "source"), out var fact) && fact.Order == order;
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "feasible prefix deterministic identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "prefix is not a new saved model");
            Physical(c, 8, false, false);
            Physical(c, 8, false, true);
            Physical(c, 8, true, false);
            Physical(c, 24, false, false);
            foreach (string fault in new[] { "missing", "null", "negative" })
            {
                var document = Json.Parse(FrameworkProofContent.Read("native-prefix.json"));
                var requirement = document.Get("proof_dual_input_requirements")[0];
                if (fault == "missing") requirement.Remove("worker_cost");
                if (fault == "null") requirement.Set("worker_cost", JsonValue.Null());
                if (fault == "negative") requirement.Set("worker_cost", JsonValue.Number("-1"));
                c.Throws<ContentException>(() => New(Factory(document: document.ToString())), "public requirement costs explicitly validated: " + fault);
            }
        }
private const string RoleCase = "Authored role combinations: joint inputs, readiness and saved physical response";
        private static GameFactory RoleFactory(int stock = 8, bool enabled = false, string document = null)
        {
            var authored = Json.Parse(document ?? FrameworkProofContent.Read("role-combinations.json"));
            authored.Get("work_policies")[0].Set("enabled", JsonValue.Of(enabled));
            return DispatchAgingTests.Factory(profile: "dispatch.priority", shipmentDefinitions: authored.ToString(),
                shipmentDocument: "role-combinations.json", edit: root => root.Get("entities")[0].Get("components")
                    .Get("point_stock").Set("initial", JsonValue.Number(stock.ToString(System.Globalization.CultureInfo.InvariantCulture))), startupProfiles: new[] { "role-combinations.start" });
        }
        private static Fix RoleTotal(SimGame game) => Stock(game, "source") + Stock(game, "prefix.first") + Stock(game, "prefix.incident");
        private static ActivityNodeView RoleNode(SimGame game, int order, string node)
        {
            var rows = new List<ActivityNodeView>();
            if (!Work(game).TryCopyNodes(order, rows)) throw new InvalidOperationException("missing role order " + order);
            return rows.Single(r => r.Id == node);
        }
        private static void RoleCombinations(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "role-combination deterministic identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "role alternatives are not a new saved model");
            foreach (int stock in new[] { 0, 8 })
            {
                var game = New(RoleFactory(stock, true)); var offers = new List<WorkWorkerCandidate>(); ulong hash = game.Hash();
                c.Equal(Code.None, Work(game).PreviewWorkerCandidates("roles.flexible", "work", Entity(game, "source"), offers),
                    "role-aware private preview admitted");
                c.Equal(2, offers.Count, "one actual machine exposes both authored role alternatives");
                c.Equal(1, offers.Select(o => o.Worker).Distinct().Count(), "two roles are not two workers");
                c.That(offers.Single(o => o.Role.Value == "roles.beta").Result.Status == WorkRequirementStatus.Satisfied,
                    "worker-source role is ready through actual local input");
                c.That(stock == 8 ? offers.All(o => o.Result.Status == WorkRequirementStatus.Satisfied) :
                    offers.Single(o => o.Role.Value == "roles.alpha").Result.Status != WorkRequirementStatus.Satisfied,
                    "target-source role has actual independent typed readiness");
                c.Equal(stock == 0 ? "roles.beta" : "roles.alpha", offers[0].Role.Value,
                    "readiness then original ordinal role rank, not first declared supported role");
                c.Equal(hash, game.Hash(), "role-aware previews acquire nothing and mutate no state");
                if (stock == 0)
                {
                    var factory = RoleFactory(0, true); game = New(factory);
                    using (var recorder = game.RecordReplay(Limits))
                    {
                        int order = checked((int)Command(c, game, Post(game, "roles.flexible", 1000)).Arg0);
                        c.Equal("roles.beta", Node(game, order).SelectedRole.Value, "unavailable first role does not hide feasible alternative");
                        var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual alternate role/native claim restores");
                        Until(game, () => Done(game, "roles.flexible", order), "single real alternate-role machine finishes", twin);
                        c.Equal(Fix.FromInt(8), RoleTotal(game), "single actual work consumes only its worker eight");
                        var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                        c.That(replay.Success && replay.Game.Hash() == game.Hash(), "single alternative role replays canonically");
                    }
                }
            }
            RolePhysical(c, 8, false);
            RolePhysical(c, 16, false);
            RolePhysical(c, 8, true);
            RoleResponse(c);
            foreach (string fault in new[] { "missing", "duplicate", "unknown", "endpoint" })
            {
                var document = Json.Parse(FrameworkProofContent.Read("role-combinations.json"));
                var requirement = document.Get("proof_role_input_requirements")[0];
                var rows = requirement.Get("role_sources");
                if (fault == "missing") requirement.Remove("role_sources");
                if (fault == "duplicate") rows[1].Set("role", JsonValue.Of("roles.alpha"));
                if (fault == "unknown") rows[1].Set("role", JsonValue.Of("roles.unknown"));
                if (fault == "endpoint") rows[1].Set("source", JsonValue.Of("imaginary"));
                c.Throws<ContentException>(() => New(RoleFactory(document: document.ToString())),
                    "role-aware authored source composition refuses " + fault);
            }
        }
        private static void RolePhysical(Check c, int stock, bool restrict)
        {
            var factory = RoleFactory(stock); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            using (var recorder = game.RecordReplay(Limits))
            {
                int first = checked((int)Command(c, game, Post(game, "roles.flexible", 1000)).Arg0);
                int second = checked((int)Command(c, game, Post(game, "roles.fixed", 100)).Arg0);
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending role-aware native orders restore");
                int a = Submit(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "prefix.first"),
                    Settings = new WorkPolicySettings(true, 0, restrict ? new[] { new WorkerRoleId("roles.alpha") } :
                        new[] { new WorkerRoleId("roles.alpha"), new WorkerRoleId("roles.beta") }, null, true, true, WorkAssignmentMode.All) }, twin);
                int b = Submit(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "prefix.second"),
                    Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("roles.fixed") }, null, true, true, WorkAssignmentMode.All) }, twin);
                Update(game, twin); c.That(Answer(game, a).Accepted && Answer(game, b).Accepted, "player enables exact authored role policies");
                bool preferred = stock == 16 || restrict;
                c.Equal(Entity(game, "prefix.first"), Node(game, first).Worker, "actual multi-role actor acquired once");
                c.Equal(preferred ? "roles.alpha" : "roles.beta", Node(game, first).SelectedRole.Value,
                    "same whole admission selects role/source combination rather than arbitrary first role");
                c.Equal(ResourceEndpointId.ForEntity(Entity(game, preferred ? "source" : "prefix.first")),
                    Node(game, first).Resources.Single().Choice.Endpoint, "actual native role has exact authored source claim");
                c.Equal(Fix.FromInt(stock), Stock(game, "source"), "role admission does not remotely consume target");
                c.Equal(Fix.FromInt(8), Stock(game, "prefix.first"), "role admission does not remotely consume worker stock");
                c.Equal(0L, Node(game, first).Progress.WholeTicks, "physical role work cannot precede actual arrival");
                if (restrict) c.That(!Node(game, second).Worker.IsValid && Node(game, second).Resources.Count == 0,
                    "disallowed alternative role cannot bypass actual player policy");
                else c.Equal(Entity(game, "prefix.second"), Node(game, second).Worker,
                    "joint resource-feasible role allows second distinct physical machine in same update");
                var options = new List<WorkWorkerCandidate>(); ulong hash = game.Hash();
                c.That(Work(game).TryCopyWorkerCandidates(first, "work", options), "actual live role candidates visible");
                c.Equal(1, options.Count(o => o.Worker == Node(game, first).Worker), "assigned live job keeps one actual role");
                c.Equal(Node(game, first).SelectedRole, options.Single(o => o.Worker == Node(game, first).Worker).Role,
                    "live role is never silently relabeled during diagnostics");
                c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyWorkerCandidates(first, "work", options) && options.Count == 0,
                    "foreign seat receives no private role/source alternatives");
                c.Equal(hash, game.Hash(), "live/private role observations are pure");
                var captured = Node(game, first); twin = factory.Load(game.Save());
                c.Equal(hash, twin.Hash(), "selected native role/input team restores without speculative role state");
                if (restrict)
                {
                    Command(c, game, new CancelOrderCommand { Order = first }, twin);
                    c.Equal(Fix.FromInt(8), Stock(game, "prefix.first"), "cancellation preserves unrelated worker input");
                    c.Equal(Entity(game, "prefix.second"), Node(game, second).Worker, "fresh matching uses released target claim for fixed worker");
                    Command(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "prefix.first"),
                        Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("roles.beta") }, null, true, true, WorkAssignmentMode.All) }, twin);
                    first = checked((int)Command(c, game, Post(game, "roles.flexible", 1000), twin).Arg0);
                    c.Equal("roles.beta", Node(game, first).SelectedRole.Value, "new policy permits actual remaining alternative");
                }
                Until(game, () => Node(game, first).Progress.WholeTicks > 0, "actual role-selected machine travels and works", twin);
                c.Equal(0L, captured.Progress.WholeTicks, "retained role/progress snapshot is detached");
                twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long clock = game.Tick;
                Update(game, twin); c.Equal(clock, game.Tick, "single GameClock pause stops role work and travel");
                c.Equal(hash, game.Hash(), "paused native roles/inputs/progress unchanged");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => Done(game, "roles.flexible", first) && Done(game, "roles.fixed", second),
                    "both actual role-selected material activities finish", twin);
                c.Equal(Fix.FromInt(stock == 16 ? 16 : 8), RoleTotal(game), "two real consumers spend exactly sixteen; no role double-booking or duplicated inputs");
                c.Equal(game.Hash(), twin.Hash(), "role choice and physical work continue identically after saves");
                c.Equal(isolatedHash, isolated.Hash(), "role-combination search is isolated from another session");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(),
                    "canonical player replay preserves actual roles and source combinations: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void RoleResponse(Check c)
        {
            var factory = RoleFactory(enabled: true); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            var target = Entity(game, "prefix.incident"); var source = new Code("damage");
            var incidents = game.Seat(0).View.Feature<IncidentsView>(); ulong hash = game.Hash();
            c.Equal(Code.None, incidents.ReadPreparedness(target, source, out var readiness), "prospective ordinary response readiness admitted");
            c.That(readiness.ReadyForFrontier && readiness.FrontierWorkersRequired == 2 && readiness.FrontierWorkersCovered == 2,
                "two role alternatives on one actor do not fabricate extra emergency machines");
            c.Equal("roles.beta", readiness.Nodes.Single(n => n.Id == "flexible").CoveredRole.Value,
                "preparedness uses same jointly feasible role/input choice as native assignment");
            c.Equal("roles.fixed", readiness.Nodes.Single(n => n.Id == "fixed").CoveredRole.Value, "fixed emergency node has actual illustrative role");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<IncidentsView>().ReadPreparedness(target, source, out _),
                "foreign company cannot inspect emergency role/material readiness");
            c.Equal(hash, game.Hash(), "preparedness acquires no roles, workers or inputs");
            using (var recorder = game.RecordReplay(Limits))
            {
                var twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "prospective response saves without hypothetical role state");
                Command(c, game, new RaiseIncidentCommand { Target = target, Source = source, Kind = new Code("proof.failure") }, twin);
                if (!incidents.TryGetResponseOrder(target, source, out int order)) throw new InvalidOperationException("missing role-aware response order");
                c.Equal(Entity(game, "prefix.first"), RoleNode(game, order, "flexible").Worker, "ordinary emergency physically claims actual flexible actor");
                c.Equal("roles.beta", RoleNode(game, order, "flexible").SelectedRole.Value, "actual response matches prospective feasible role");
                c.Equal(Entity(game, "prefix.second"), RoleNode(game, order, "fixed").Worker, "ordinary emergency claims second distinct actor jointly");
                c.Equal(Fix.FromInt(24), RoleTotal(game), "response acquisition is not remote material consumption");
                c.Equal("roles.beta", readiness.Nodes.Single(n => n.Id == "flexible").CoveredRole.Value, "retained prospective role view remains detached");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual emergency role/resource commitments restore");
                Until(game, () => RoleNode(game, order, "flexible").Progress.WholeTicks > 0, "emergency machine physically approaches and works", twin);
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partially earned emergency roles restore");
                Until(game, () => incidents.TryGetIncident(target, source, out var episode) && !episode.Active,
                    "actual material-consuming role-aware emergency completes through normal response effect", twin);
                c.Equal(Fix.FromInt(8), RoleTotal(game), "actual response consumes worker8 and target8, preserves unrelated source8");
                c.Equal(game.Hash(), twin.Hash(), "physical emergency saved continuation remains deterministic");
                c.Equal(isolatedHash, isolated.Hash(), "prospective and actual role choices remain session-isolated");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(),
                    "ordinary emergency command replay preserves roles/inputs/physical completion: " + replay.Failure + "/" + replay.Detail);
            }
        }

        private const string PriorityCase = "Priority-local workers: scarce source, lower-role contention and saved physical execution";
        private static void PriorityLocal(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "priority-local deterministic identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "speculative coverage is not saved");
            PriorityPhysical(c, 8, false, false);
            PriorityPhysical(c, 8, false, true);
            PriorityPhysical(c, 8, true, false);
            PriorityPhysical(c, 16, false, false);
        }
        private static void PriorityPhysical(Check c, int sourceStock, bool lowerFirst, bool cancel)
        {
            var factory = DispatchAgingTests.Factory(profile: "dispatch.priority",
                shipmentDefinitions: FrameworkProofContent.Read("native-priority-workers.json"),
                shipmentDocument: "native-priority-workers.json",
                edit: root => root.Get("entities")[0].Get("components").Get("point_stock")
                    .Set("initial", JsonValue.Number(sourceStock.ToString(System.Globalization.CultureInfo.InvariantCulture))), startupProfiles: new[] { "native-priority-workers.start" });
            var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            using (var recorder = game.RecordReplay(Limits))
            {
                int first = checked((int)Command(c, game, Post(game, "priority.flexible", 1000)).Arg0);
                int second = checked((int)Command(c, game, Post(game, "priority.fixed", 100)).Arg0);
                var twin = factory.Load(game.Save());
                c.Equal(game.Hash(), twin.Hash(), "pending ranked alternatives restore before actual matching");
                int lowerSequence = Submit(c, game, Post(game, "priority.lower", lowerFirst ? 2000 : 1), twin);
                var controls = new List<int>();
                foreach (string name in new[] { "prefix.first", "prefix.second", "prefix.third" })
                {
                    var roles = name == "prefix.first" ? new[] { new WorkerRoleId("priority.flex") } :
                        name == "prefix.second" ? new[] { new WorkerRoleId("priority.fixed") } :
                        new[] { new WorkerRoleId("priority.flex"), new WorkerRoleId("priority.lower") };
                    controls.Add(Submit(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, name),
                        Settings = new WorkPolicySettings(true, 0, roles, null, true, true, WorkAssignmentMode.All) }, twin));
                }
                Update(game, twin);
                int lower = checked((int)Answer(game, lowerSequence).Arg0);
                c.That(Answer(game, lowerSequence).Accepted && controls.All(s => Answer(game, s).Accepted),
                    "authored three-worker contention enabled through ordinary player commands");
                bool abundant = sourceStock == 16;
                string firstWorker = abundant || lowerFirst ? "prefix.first" : "prefix.third";
                c.Equal(Entity(game, firstWorker), Node(game, first).Worker,
                    "higher frontier selects an actual feasible worker, not a hypothetical lower reservation");
                c.Equal(ResourceEndpointId.ForEntity(Entity(game, abundant || lowerFirst ? "source" : "prefix.third")),
                    Node(game, first).Resources.Single().Choice.Endpoint, "actual selected source follows feasible physical team");
                c.Equal(Fix.FromInt(sourceStock), Stock(game, "source"), "native acquisition is not remote consumption");
                c.Equal(Fix.FromInt(8), Stock(game, "prefix.third"), "worker input remains actual stock through approach");
                c.Equal(0L, Node(game, first).Progress.WholeTicks, "selected real machine must arrive before work");
                if (lowerFirst)
                {
                    c.Equal(Entity(game, "prefix.third"), Node(game, lower).Worker, "explicitly promoted dry activity owns its real actor");
                    c.That(!Node(game, second).Worker.IsValid && Node(game, second).Resources.Count == 0,
                        "lower fixed activity cannot steal the earlier actual input");
                }
                else
                {
                    c.Equal(Entity(game, "prefix.second"), Node(game, second).Worker,
                        "two high activities acquire jointly despite lower distinct-role contention");
                    c.Equal(ResourceEndpointId.ForEntity(Entity(game, "source")),
                        Node(game, second).Resources.Single().Choice.Endpoint, "fixed machine retains its real target claim");
                    if (abundant) c.Equal(Entity(game, "prefix.third"), Node(game, lower).Worker,
                        "full feasible frontier still covers all three distinct machines");
                    else c.That(!Node(game, lower).Worker.IsValid && Node(game, lower).Resources.Count == 0 &&
                        Node(game, lower).Progress.WholeTicks == 0, "discarded lower work owns no speculative worker or progress");
                }
                var captured = Node(game, first); ulong hash = game.Hash();
                var offers = new List<WorkerDispatchOffer>(); Work(game).CopyAssignments(offers);
                var privateOffers = new List<WorkerDispatchOffer>();
                game.Seat(1).View.Feature<WorkView>().CopyAssignments(privateOffers);
                c.Equal(0, privateOffers.Count, "native worker/source alternatives remain company-private");
                c.Equal(hash, game.Hash(), "role contention diagnostics do not acquire speculative claims");
                twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "actual selected team and native inputs restore");
                if (cancel)
                {
                    Command(c, game, new CancelOrderCommand { Order = first }, twin);
                    c.Equal(Fix.FromInt(8), Stock(game, "prefix.third"), "cancel unspent alternative releases only own native claim");
                    c.Equal(Entity(game, "prefix.second"), Node(game, second).Worker, "cancel does not interrupt acquired sibling");
                    c.Equal(Entity(game, "prefix.third"), Node(game, lower).Worker,
                        "fresh normal matching gives released actor to genuinely runnable lower work");
                    Until(game, () => Done(game, "priority.fixed", second) && Done(game, "priority.lower", lower),
                        "uncancelled physical machine and released lower actor finish", twin);
                    c.Equal(Fix.Zero, Stock(game, "source"), "uncancelled fixed machine consumes target eight once");
                    c.Equal(Fix.FromInt(8), Stock(game, "prefix.third"), "dry lower work cannot consume unrelated worker stock");
                    first = checked((int)Command(c, game, Post(game, "priority.flexible", 1000), twin).Arg0);
                    c.Equal(Entity(game, "prefix.third"), Node(game, first).Worker, "fresh activity selects genuinely remaining ready actor");
                    c.Equal(ResourceEndpointId.ForEntity(Entity(game, "prefix.third")),
                        Node(game, first).Resources.Single().Choice.Endpoint, "fresh activity reserves exact remaining worker input");
                }
                Until(game, () => Node(game, first).Progress.WholeTicks > 0,
                    "priority-local real machine travels and earns productive work", twin);
                c.Equal(0L, captured.Progress.WholeTicks, "captured acquired facts remain detached from execution");
                if (!abundant && !lowerFirst) c.Equal(Fix.Zero, Stock(game, "prefix.third"),
                    "arrived higher machine consumes its actual alternative eight once");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partially earned physical team restores");
                game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long clock = game.Tick;
                Update(game, twin);
                c.Equal(clock, game.Tick, "one paused GameClock stops physical workers and progress");
                c.Equal(hash, game.Hash(), "paused actual team and resource bindings remain unchanged");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => Done(game, "priority.flexible", first), "higher physical activity completes", twin);
                if (lowerFirst)
                {
                    Until(game, () => Done(game, "priority.lower", lower), "promoted lower-origin dry activity completes", twin);
                    c.That(!Node(game, second).Worker.IsValid && !Node(game, second).Blocker.IsNone &&
                        Node(game, second).Resources.Count == 0, "unfunded remaining activity is an actual typed wait");
                    Command(c, game, new CancelOrderCommand { Order = second }, twin);
                }
                else
                {
                    Until(game, () => Done(game, "priority.fixed", second), "second physically supplied high activity completes", twin);
                    Until(game, () => Done(game, "priority.lower", lower), "original lower work gets actual released actor", twin);
                }
                c.Equal(Fix.FromInt(abundant || lowerFirst ? 8 : 0), Stock(game, "source") + Stock(game, "prefix.third"),
                    "exact physical material conservation across actual matching, consumption and release");
                c.Equal(game.Hash(), twin.Hash(), "saved pending/acquired/working/cancelled role contention remains deterministic");
                c.Equal(isolatedHash, isolated.Hash(), "priority-local transient matching never mutates another session");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(),
                    "canonical player replay preserves actual worker/source priority: " + replay.Failure + "/" + replay.Detail);
            }
        }

        private static void Physical(Check c, int sourceStock, bool timerFirst, bool cancel)
        {
            var factory = Factory(sourceStock); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            using (var recorder = game.RecordReplay(Limits))
            {
                int first = (int)Command(c, game, Post(game, "prefix.flexible", 1000)).Arg0;
                int second = (int)Command(c, game, Post(game, "prefix.fixed", 100)).Arg0;
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "real pending authored orders restore");
                int timerSequence = Submit(c, game, Post(game, "prefix.timer", timerFirst ? 2000 : 1), twin);
                var controls = new List<int>();
                foreach (string name in new[] { "prefix.first", "prefix.second" })
                    controls.Add(Submit(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, name),
                        Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId(name) }, null, true, true, WorkAssignmentMode.All) }, twin));
                Update(game, twin); int timer = checked((int)Answer(game, timerSequence).Arg0);
                c.That(Answer(game, timerSequence).Accepted && controls.All(s => Answer(game, s).Accepted), "timer/real actors admitted in same normal Update");
                bool all = sourceStock == 24;
                c.Equal(Entity(game, "prefix.first"), Node(game, first).Worker, "first native activity receives actual authored-role actor");
                c.Equal(ResourceEndpointId.ForEntity(Entity(game, all ? "source" : "prefix.first")),
                    Node(game, first).Resources.Single().Choice.Endpoint, "original joint source search selects exact feasible input alternative");
                c.Equal(Fix.FromInt(sourceStock), Stock(game, "source"), "assignment is not remote productive consumption");
                c.Equal(Fix.FromInt(8), Stock(game, "prefix.first"), "alternative reservation is not consumption");
                c.Equal(0L, Node(game, first).Progress.WholeTicks, "assigned machine must travel before earning work");
                if (timerFirst || all)
                {
                    c.Equal(NodeState.Working, Node(game, timer).State, "ranked feasible timer is genuinely acquired");
                    c.Equal(0L, Node(game, timer).Progress.WholeTicks, "timer earns no same-Update progress");
                    c.Equal(1, Node(game, timer).Resources.Count, "real timer holds its native input once");
                }
                else
                {
                    c.That(Node(game, timer).State != NodeState.Working && Node(game, timer).Resources.Count == 0 &&
                        Node(game, timer).Progress.WholeTicks == 0, "lowest infeasible timer owns no claims or progress");
                }
                if (timerFirst)
                    c.That(!Node(game, second).Worker.IsValid && Node(game, second).Resources.Count == 0, "higher timer takes scarce source before lower fixed machine");
                else
                {
                    c.Equal(Entity(game, "prefix.second"), Node(game, second).Worker, "second high-ranked machine starts alongside first despite infeasible lower timer");
                    c.Equal(ResourceEndpointId.ForEntity(Entity(game, "source")), Node(game, second).Resources.Single().Choice.Endpoint,
                        "second real native activity owns its actual target input");
                }
                var held = Node(game, first); ulong hash = game.Hash();
                var offers = new List<WorkerDispatchOffer>(); Work(game).CopyAssignments(offers);
                var foreign = new List<WorkerDispatchOffer>(); game.Seat(1).View.Feature<WorkView>().CopyAssignments(foreign);
                c.Equal(0, foreign.Count, "native prefix source facts stay private"); c.Equal(hash, game.Hash(), "native choice diagnostics are pure");
                twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "whole native prefix actual claims restore");
                if (cancel)
                {
                    Command(c, game, new CancelOrderCommand { Order = first }, twin);
                    c.Equal(Fix.FromInt(8), Stock(game, "prefix.first"), "cancel unspent alternative releases, never destroys inputs");
                    c.That(Node(game, timer).Resources.Count == 0 && Node(game, timer).Progress.WholeTicks == 0,
                        "cancel first does not steal second target claim for lower timer");
                    Until(game, () => Done(game, "prefix.fixed", second), "remaining actual native machine completes", twin);
                    c.Equal(Fix.FromInt(8), Total(game), "only uncancelled actual work consumed eight");
                    first = (int)Command(c, game, Post(game, "prefix.flexible", 1000), twin).Arg0;
                    c.Equal(ResourceEndpointId.ForEntity(Entity(game, "prefix.first")), Node(game, first).Resources.Single().Choice.Endpoint,
                        "fresh work chooses actual remaining worker input, no borrowed stale claim");
                }
                Until(game, () => Node(game, first).Progress.WholeTicks > 0, "real movement and first productive input", twin);
                if (!all) c.Equal(Fix.Zero, Stock(game, "prefix.first"), "arrived native work consumes actual alternative eight");
                c.Equal(0L, held.Progress.WholeTicks, "retained acquired facts detached from actual progress");
                twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long clock = game.Tick;
                Update(game, twin); c.Equal(clock, game.Tick, "sole-clock pause earns no native or timed progress");
                c.Equal(hash, game.Hash(), "paused native bindings/work hash stable"); game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => Done(game, "prefix.flexible", first), "actual flexible work completes", twin);
                if (timerFirst)
                {
                    Until(game, () => Done(game, "prefix.timer", timer), "actual highest timer completes", twin);
                    c.That(!Node(game, second).Worker.IsValid && !Node(game, second).Blocker.IsNone, "unfunded lower machine remains typed-blocked");
                    Command(c, game, new CancelOrderCommand { Order = second }, twin);
                }
                else Until(game, () => Done(game, "prefix.fixed", second), "actual second native work completes", twin);
                if (all) Until(game, () => Done(game, "prefix.timer", timer), "whole feasible mixed native frontier completes", twin);
                else if (!timerFirst) Command(c, game, new CancelOrderCommand { Order = timer }, twin);
                c.Equal(Fix.FromInt(all ? 8 : 0), Total(game), "exact physical totals, no duplicated inputs/progress");
                c.Equal(game.Hash(), twin.Hash(), "pending/held/working/cancelled native prefixes continue identically");
                c.Equal(isolatedHash, isolated.Hash(), "transient native prefix allocation stays session-isolated");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(),
                    "canonical commands replay native priority/source choices: " + replay.Failure + "/" + replay.Detail);
            }
        }
    }
}
