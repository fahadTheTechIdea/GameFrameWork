using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Player fixtures, commands, detached views and bounded Update only; no facility/service rules.
    internal static class RoleFacilityTests
    {
        private const string Case = "Role-aware facilities: selected intent, physical repair and saved policy changes";
        private static readonly Code Service = new Code("fleet.service");
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(string policy, int near = 32, Action<JsonValue> edit = null, string document = null)
            => FacilitySelectionTests.Factory(policy: policy, facilityDocument: "role-facilities.json", facilityDefinitions: document, edit: root =>
            {
                for (int i = 0; i < 2; i++) root.Get("entities")[i].Get("components").Get("point_dock")
                    .Set("work_roles", Json.Parse("[\"haul\",\"inspect\"]"));
                root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number(near.ToString()));
                edit?.Invoke(root);
            });
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points",
            Seed = 302, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Entity(SimGame game, string prototype)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Id;
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static ActivityFacilitySelectionView Read(SimGame game)
        {
            var refusal = Work(game).ReadRecurringFacilities(Entity(game, "machine.a"), Service, out var view);
            if (!refusal.IsNone) throw new InvalidOperationException("facility read " + refusal); return view;
        }
        private static RecurringPlanView Source(SimGame game)
        {
            var refusal = Work(game).ReadRecurringPlan(Entity(game, "machine.a"), Service, out var view);
            if (!refusal.IsNone) throw new InvalidOperationException("source read " + refusal); return view;
        }
        private static PlanView Plan(SimGame game, long id)
        {
            var rows = new List<PlanView>(); Work(game).CopyPlans(rows); return rows.Single(p => p.Id == id);
        }
        private static ActivityNodeView Node(SimGame game, long id)
        {
            var rows = new List<ActivityNodeView>();
            if (!Work(game).TryCopyNodes(Plan(game, id).Order, rows)) throw new InvalidOperationException("missing selected-role order");
            return rows.Single(n => n.Id == "service");
        }
        private static Fix Stock(SimGame game, string prototype)
        {
            var facets = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, prototype), facets);
            return facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (a,b) => a + b.Value);
        }
        private static Fix Condition(SimGame game)
        {
            var refusal = game.Seat(0).View.Feature<EquipmentConditionView>().Read(Entity(game, "machine.a"), new Code("integrity"), out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("condition read " + refusal); return value.Value;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("role facility clock=" + game.Tick + "/fault=" + game.Fault + "/twin=" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1200 && !done(); i++) Update(game, twin);
            if (!done())
            {
                var jobs = new List<WorkJobView>(); Work(game).CopyJobs(jobs);
                throw new InvalidOperationException(stage + "/clock=" + game.Tick + "/jobs=" +
                    string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.Worker)));
            }
        }
        private static int Submit(Check c, SimGame game, Command command, SimGame twin = null)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal command preview");
            c.Equal(hash, game.Hash(), "preview claims nothing"); int sequence = game.Seat(0).Submit(command); twin?.Seat(0).Submit(command); return sequence;
        }
        private static void Command(Check c, SimGame game, Command command, SimGame twin = null)
        {
            int sequence = Submit(c, game, command, twin); Update(game, twin);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            var result = results.Single(r => r.Sequence == sequence);
            c.That(result.Accepted, "player command admitted " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("player command " + result.Refusal);
        }
        private static ConfigureRecurringPlanCommand Configure(SimGame game, string policy, bool enabled, long interval = 1000)
            => new ConfigureRecurringPlanCommand { Entity = Entity(game, "machine.a"), Source = Service,
                Settings = new RecurringPlanSettings(enabled, 60, GameDuration.Minutes(interval), new Code(policy)) };
        private static ConfigureWorkPolicyCommand Roles(SimGame game, params string[] roles)
        {
            if (!Work(game).TryReadPolicy(Entity(game, "machine.a"), out var view).IsNone) throw new InvalidOperationException("policy read");
            var p = view.Settings;
            return new ConfigureWorkPolicyCommand { Worker = view.Worker, Settings = new WorkPolicySettings(p.Enabled, p.Priority,
                roles.Select(r => new WorkerRoleId(r)).ToArray(), p.Area, p.Interruptible, p.EmergencyEligible, p.AssignmentMode) };
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current exact identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one current role-selection codec");
            foreach (var row in new[] { ("service.inspection-only","inspect"), ("service.inspection-first","inspect"),
                ("service.haul-first","haul"), ("service.equal","haul") })
            {
                var factory = Factory(row.Item1); var game = New(factory); ulong hash = game.Hash(); var read = Read(game);
                c.Equal(Entity(game, "source"), read.Target, "policy rank then route selects actual nearer dock");
                c.Equal(2, read.Candidates.Count, "roles do not fabricate another facility");
                c.That(read.Candidates.All(o => o.Ready && o.Role.Value == row.Item2), "all safe offers evaluate authored alternative roles");
                c.Equal(row.Item2, read.Request.Intent.Arguments.Get("workers")[0].Get("role").Text, "selected role enters sole immutable intent");
                c.Equal(hash, game.Hash(), "role ranking remains pure");
                c.Equal(hash, factory.Load(game.Save()).Hash(), "pure observations own no persistent shadow selection");
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadRecurringFacilities(Entity(game,"machine.a"), Service, out var hidden), "private role candidates");
                c.That(hidden == null, "foreign query returns no worker-role details");
            }
            var strict = New(Factory("service.inspection-only")); int actor = Entity(strict,"machine.a").Value, target = Entity(strict,"source").Value;
            foreach (string role in new[] { "", ",\"role\":\"missing\"", ",\"role\":\"bad role\"", ",\"role\":1" })
            {
                var intent = new PlanIntent("activity:facility.service", Json.Parse("{\"target\":" + target +
                    ",\"workers\":[{\"node\":\"service\",\"worker\":" + actor + role + "}]}"), null, 60, true);
                c.Equal(Codes.InvalidArgument, Work(strict).PreviewPlan(intent), "missing, unknown or malformed selected role cannot silently become unbound");
            }
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                root => root.Get("proof_role_facility_policies")[0].Remove("roles"),
                root => root.Get("proof_role_facility_policies")[0].Get("roles")[0].Set("role", JsonValue.Of("missing")),
                root => root.Get("proof_role_facility_policies")[0].Get("roles")[0].Set("rank", JsonValue.Number("-1")),
                root => root.Get("proof_role_facility_policies")[0].Get("roles").Add(root.Get("proof_role_facility_policies")[0].Get("roles")[0].Clone())
            })
            {
                var body = Json.Parse(Beep.SimEngine.ExtensionProof.FrameworkProofContent.Read("role-facilities.json"));
                edit(body);
                var text = new System.Text.StringBuilder(); body.WriteCanonical(text);
                c.Throws<ContentException>(() => New(Factory("service.inspection-only", document: text.ToString())),
                    "invalid authored role ranks are rejected before Initialize");
            }
            Physical(c, false); Physical(c, true);
        }
        private static void Physical(Check c, bool cancel)
        {
            var factory = Factory(cancel ? "service.inspection-first" : "service.inspection-only", 2);
            var isolated = New(factory); ulong untouched = isolated.Hash(); var game = New(factory);
            using (var recorder = game.RecordReplay(Limits))
            {
                var read = Read(game); c.Equal(Entity(game,"destination"), read.Target, "actual missing tools exclude nearer dock");
                c.That(!read.Candidates[0].Ready && !read.Candidates[0].Readiness.Blocker.IsNone, "policy cannot override physical shortage");
                int sequence = Submit(c, game, Configure(game, cancel ? "service.inspection-first" : "service.inspection-only", true));
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending policy command saves");
                Update(game,twin);
                long cursor=0; var answers=new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor,answers);
                c.That(answers.Single(a => a.Sequence == sequence).Accepted, "restored pending command posts ordinary physical intent");
                Until(game, () => Source(game).Schedule.LastPlan > 0, "post selected role", twin);
                long id = Source(game).Schedule.LastPlan;
                Until(game, () => Node(game,id).Worker.IsValid, "acquire selected role", twin);
                c.Equal("inspect", Node(game,id).RequestedRole.Value, "view carries authoritative authored role");
                c.Equal("inspect", Node(game,id).SelectedRole.Value, "actual job cannot revert to earlier compatible haul role");
                c.Equal(Entity(game,"machine.a"), Node(game,id).Worker, "one real selected machine acquires");
                c.Equal(Fix.FromInt(32), Stock(game,"destination"), "acquisition does not consume or teleport stock");
                var acquired = Node(game,id); twin=factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"claimed role saves exactly");
                Command(c,game,Configure(game,"service.haul-first",true),twin);
                c.Equal(id,Source(game).Schedule.LastPlan,"policy edit never duplicates active plan");
                c.Equal("inspect",Node(game,id).RequestedRole.Value,"future policy cannot rewrite active intent");
                Until(game,()=>Node(game,id).Progress.WholeTicks >= 3,"physical selected dock work",twin);
                c.Equal(Fix.FromInt(29),Stock(game,"destination"),"real dock supplied three spares exactly once");
                c.Equal(Fix.FromInt(4),Condition(game),"partial work does not repair early");
                c.Equal(0L,acquired.Progress.WholeTicks,"retained node facts stay detached");
                twin=factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"working role/progress saves");
                game.Clock.Pause();twin.Clock.Pause();ulong paused=game.Hash();long clock=game.Clock.StepIndex;
                Update(game,twin); c.Equal(paused,game.Hash(),"sole game clock pause freezes physical work");
                c.Equal(clock,game.Clock.StepIndex,"no independent facility clock");game.Clock.Resume();twin.Clock.Resume();
                Command(c,game,Roles(game,"haul"),twin);
                c.Equal("inspect",Node(game,id).RequestedRole.Value,"worker policy cannot substitute another role");
                c.Equal("inspect",Node(game,id).SelectedRole.Value,"actual incumbent keeps exact role while paused");
                c.Equal(NodeState.Paused,Node(game,id).State,"incompatible current worker policy physically pauses selected role");
                c.Equal(Fix.FromInt(4),Condition(game),"policy pause cannot finish repair");
                twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"paused selected role saves");
                Command(c,game,Roles(game,"haul","inspect"),twin);
                if(cancel)
                {
                    Command(c,game,new CancelPlanCommand { Selection=new PlanCancellationSelection(false,new[]{id}) },twin);
                    c.Equal(PlanState.Cancelled,Plan(game,id).State,"normal cancellation ends old role intent");
                    c.Equal(Fix.FromInt(4),Condition(game),"cancel cannot grant repair");
                    c.Equal(Fix.FromInt(29),Stock(game,"destination"),"cancel cannot refund spent spares");
                    Command(c,game,Configure(game,"service.haul-first",true,1),twin);
                    Until(game,()=>Source(game).Schedule.LastPlan > id,"future posting after physical cancellation",twin);
                    long next=Source(game).Schedule.LastPlan;
                    Until(game,()=>Node(game,next).Worker.IsValid,"new authored-role acquisition",twin);
                    c.Equal("haul",Node(game,next).RequestedRole.Value,"future policy owns only new role intent");
                    c.Equal("haul",Node(game,next).SelectedRole.Value,"new ordinary acquisition uses newly chosen role");
                    Command(c,game,Configure(game,"service.inspection-only",false),twin);
                    twin=factory.Load(game.Save());
                    Until(game,()=>Plan(game,next).State==PlanState.Done,"new role physically repairs",twin);
                    c.Equal(Fix.FromInt(26),Stock(game,"destination"),"two actually begun repairs consume six physical spares");
                }
                else Until(game,()=>Plan(game,id).State==PlanState.Done,"original selected role physically repairs",twin);
                c.Equal(Fix.FromInt(10),Condition(game),"full physical work/departure earns normal repair");
                c.Equal(Fix.FromInt(2),Stock(game,"source"),"short nearer dock never supplies invented tools");
                c.Equal(game.Hash(),twin.Hash(),"saved selected-role twins continue identically");
                c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed selected intent restores");
                c.Equal(untouched,isolated.Hash(),"facility roles are session isolated");
                var replay=new ReplayVerifier().Verify(factory,recorder.Finish(),Limits);
                c.That(replay.Success && replay.Game!=null && replay.Game.Hash()==game.Hash(),"canonical replay retains role: "+replay.Failure+"/"+replay.Detail);
            }
        }
    }
}

