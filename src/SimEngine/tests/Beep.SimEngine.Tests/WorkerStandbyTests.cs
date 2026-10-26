using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    internal static class WorkerStandbyTests
    {
        internal static void Register()
        {
            TestRunner.Add("Worker standby: authored settings, private controls and shared admission", Contracts);
            TestRunner.Add("Worker standby: physical response, emergency supply and retained work", Physical);
        }
        internal static GameFactory Fixture(bool publicPolicy = false, bool publicWorker = false, bool supplied = false,
            int fault = 0, Action<JsonValue> author = null, bool haulWorker = false, Action<JsonValue> edit = null, Action<JsonValue> workAuthor = null)
        {
            var authored = Json.Parse(FrameworkProofContent.Read("worker-standby.json")); author?.Invoke(authored);
            var work = Json.Parse(FrameworkProofContent.Read("emergency-preemption.json"));
            work.Set("work_policies", authored.Get("work_policies"));
            if (supplied) work.Get("activities")[3].Set("automatic_supply", JsonValue.Of(true));
            workAuthor?.Invoke(work);
            var text = new StringBuilder(); work.WriteCanonical(text);
            return Factory("dispatch.supply", edit: root =>
            {
                root.Get("entity_states").Add(authored.Get("entity_states")[0]);
                root.Set("point_interval_regions", authored.Get("point_interval_regions"));
                var parts = root.Get("entities")[2].Get("components");
                parts.Remove("work_policy");
                parts.Set(publicPolicy ? "proof_work_policy" : "work_policy", publicPolicy
                    ? Json.Parse("{\"profile\":\"reserve.machine\",\"fault\":" + fault + "}") : Json.Parse("{\"profile\":\"reserve.machine\"}"));
                if (publicWorker)
                {
                    parts.Remove("worker"); parts.Set("contract_worker", Json.Parse("{\"role\":\"" + (haulWorker ? "haul" : "inspect") +
                        "\",\"rate\":1,\"states\":{\"working\":\"processing\",\"stopped\":\"blocked\"}}"));
                }
                if (supplied) root.Get("entities")[1].Get("components").Get("contract_incident").Set("response", JsonValue.Of("preempt.supplied-response"));
                edit?.Invoke(root);
            }, work: text.ToString());
        }
        private static WorkPolicySettings Settings(SimGame game, WorkAssignmentMode mode, bool enabled = true, bool emergency = true,
            string area = null, IReadOnlyList<WorkerRoleId> roles = null)
        {
            var current = WorkPolicyRules.Read(Find(game, "machine"));
            return new WorkPolicySettings(enabled, current.Priority, roles ?? current.AllowedRoles, area, current.Interruptible, emergency, mode);
        }
        private static void Mode(Check c, SimGame game, WorkAssignmentMode mode, bool enabled = true, bool emergency = true,
            string area = null, IReadOnlyList<WorkerRoleId> roles = null)
        {
            var settings = Settings(game, mode, enabled, emergency, area, roles); var actor = Find(game, "machine"); var view = game.Seat(0).View.Feature<WorkView>();
            c.Equal(Code.None, view.PreviewPolicy(actor.Id, settings), "private policy preview uses shared fresh admission");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = actor.Id, Settings = settings }).Accepted, "ordinary player command commits exact standby mode");
            c.Equal(mode, WorkPolicyRules.Read(actor).AssignmentMode, "actual policy owner acknowledges requested mode");
        }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("standby game update fault: " + game.Fault + "/" + twin?.Fault); }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Blocker + "/" + job.ToilIndex + "/" + job.Loaded);
                throw new InvalidOperationException(stage + " timeout at game time " + game.Clock.TotalGameTime + ": " + string.Join(";", facts));
            }
        }
        private static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInletPort>().Amount(Supply(game));
        private static byte[] PolicyBytes(IWorkPolicy policy)
        { using var stream = new MemoryStream(); ((Component)policy).Sync(new StateWriter(stream)); return stream.ToArray(); }
        private static byte[] BadMode()
        {
            using var stream = new MemoryStream(); var writer = new StateWriter(stream); bool enabled = true, interruptible = true, emergency = true;
            int priority = 100, mode = 9; string area = null;
            writer.Bool(ref enabled); writer.Int(ref priority); writer.String(ref area); writer.Bool(ref interruptible); writer.Bool(ref emergency); writer.Int(ref mode); return stream.ToArray();
        }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "standby dispatch/control identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "clean shared settings protocol replacement");
            foreach (bool publicPolicy in new[] { false, true }) foreach (bool publicWorker in new[] { false, true })
            {
                var factory = Fixture(publicPolicy, publicWorker); var game = Start(factory); var actor = Find(game, "machine"); var view = game.Seat(0).View.Feature<WorkView>();
                c.Equal(WorkAssignmentMode.EmergencyOnly, WorkPolicyRules.Read(actor).AssignmentMode, "explicit JSON seeds sole native/public policy owner");
                c.Equal(Code.None, view.TryReadPolicy(actor.Id, out var retained), "owned immutable settings are visible");
                c.Equal(WorkPolicyCodes.NotPolicy, view.PreviewPolicy(Find(game, "source").Id, Settings(game, WorkAssignmentMode.All)), "absent capability never fabricates standby state");
                c.Equal(Codes.NotFound, PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().PreviewPolicy(actor.Id, Settings(game, WorkAssignmentMode.All)), "foreign control has no authority");
                c.Equal(Codes.NotFound, PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().TryReadPolicy(actor.Id, out _), "preference facts are company-private");
                ulong hash = game.Hash(); var dispatch = game.World.Services.Get<IWorkerDispatch>();
                c.Equal(WorkPolicyCodes.Standby, dispatch.CheckAssignment(game.World, actor, actor.Owner, new WorkerRoleId("inspect"), new[] { Find(game, "destination") }, false), "ordinary new work cannot borrow responder reserve");
                c.Equal(Code.None, dispatch.CheckAssignment(game.World, actor, actor.Owner, new WorkerRoleId("inspect"), new[] { Find(game, "destination") }, true), "actual emergency candidate still uses normal safety checks");
                c.Equal(WorkPolicyCodes.Standby, dispatch.CheckAssignment(game.World, actor, actor.Owner, new WorkerRoleId("inspect"), new[] { Find(game, "destination") }, false, int.MaxValue), "invented job identity cannot claim continuation");
                c.Equal(hash, game.Hash(), "admission facts acquire no job, stock or motion state");
                var order = Post(c, game); c.Equal(0, actor.Get<IWorker>().Job, "maximum player priority never classifies ordinary work as emergency");
                c.Equal(Fix.FromInt(20), Stock(game, "source"), "queued ordinary work does not consume its materials");
                c.Equal(WorkPolicyCodes.Standby, Board(game).Job(order.Node("work").JobId).Blocker, "existing job queue exposes explicit standby wait");
                c.That(TestKit.Order(game, 0, new ConfigureCompanyAlertCommand { Company = actor.Owner,
                    Settings = new CompanyAlertSettings("alerts.standard", CompanyAlertLevel.Emergency, Array.Empty<WorkerReserveSetting>()) }).Accepted, "player uses existing emergency alert policy");
                c.Equal(0, actor.Get<IWorker>().Job, "company alert priority does not fabricate a bound response identity");
                c.Equal(new Code("state.standby"), actor.State, "Game.Update settles authored standby state on actual idle entity");
                var corrupt = BadMode(); hash = game.Hash();
                c.Throws<CorruptStateException>(() => ((Component)actor.Get<IWorkPolicy>()).Sync(new StateReader(corrupt, 0, corrupt.Length)), "unknown saved assignment mode rejects before commit");
                c.Equal(hash, game.Hash(), "invalid mode decode preserves sole settings/claims");
                var truncated = PolicyBytes(actor.Get<IWorkPolicy>()); Array.Resize(ref truncated, truncated.Length - 1);
                c.Throws<CorruptStateException>(() => ((Component)actor.Get<IWorkPolicy>()).Sync(new StateReader(truncated, 0, truncated.Length)), "truncated shared settings reject before commit");
                c.Equal(hash, game.Hash(), "truncated settings preserve actual policy and queued claims");
                game.Seat(0).Submit(new ConfigureWorkPolicyCommand { Worker = actor.Id, Settings = Settings(game, WorkAssignmentMode.All, enabled: false) });
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending shared policy command saves without a second mode codec");
                Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "saved pending standby command replays identically");
                c.Equal(WorkAssignmentMode.EmergencyOnly, retained.Settings.AssignmentMode, "retained private settings cannot follow later control");
                c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "claims/jobs payload stays authoritative and unchanged");
                c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "same system declares admission semantics");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "edited native/public policy restores exactly");
            }
            foreach (Action<JsonValue> invalid in new Action<JsonValue>[] {
                root => root.Get("work_policies")[0].Remove("assignment_mode"), root => root.Get("work_policies")[0].Set("assignment_mode", JsonValue.Of("unknown")),
                root => root.Get("work_policies")[0].Set("assignment_mode", JsonValue.Number("1")), root => root.Get("work_policies")[0].Get("states").Remove("standby"),
                root => root.Get("work_policies")[0].Get("states").Set("standby", JsonValue.Of("missing")) })
                c.Throws<ContentException>(() => Start(Fixture(author: invalid)), "mode and standby state require explicit valid authoring");
            c.Throws<ArgumentException>(() => new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect") }, null, true, true, (WorkAssignmentMode)8), "invalid assignment enum cannot enter settings");
            foreach (int fault in new[] { 1, 2 })
            {
                var bad = Start(Fixture(publicPolicy: true, fault: fault)); bad.Seat(0).Submit(new ConfigureWorkPolicyCommand {
                    Worker = Find(bad, "machine").Id, Settings = Settings(bad, WorkAssignmentMode.EmergencyOnly) }); bad.Update();
                c.That(bad.Fault != null, "ignored or impure public policy control faults normal update mode=" + fault);
                c.Throws<InvalidOperationException>(() => bad.Save(), "dishonest partial policy cannot checkpoint");
            }
            c.Throws<InvalidOperationException>(() => { var impure = Start(Fixture(publicPolicy: true, fault: 3));
                impure.Seat(0).View.Feature<WorkView>().TryReadPolicy(Find(impure, "machine").Id, out _); }, "impure public policy read is rejected");
            var unlinked = Start(Fixture(publicPolicy: true, fault: 4)); Mode(c, unlinked, WorkAssignmentMode.All); Post(c, unlinked);
            c.That(Assigned(unlinked) != null, "dishonest link edit is exercised on an actual incumbent");
            unlinked.Seat(0).Submit(new ConfigureWorkPolicyCommand { Worker = Find(unlinked, "machine").Id,
                Settings = Settings(unlinked, WorkAssignmentMode.EmergencyOnly) }); unlinked.Update();
            c.That(unlinked.Fault != null, "public policy cannot silently sever an actual worker/job relation");
            c.Throws<InvalidOperationException>(() => unlinked.Save(), "dishonest link edit cannot checkpoint partial state");
            Guards(c); LoadedHaulContracts(c);
        }
        private static void Guards(Check c)
        {
            foreach (string reason in new[] { "disabled", "emergency", "role", "area", "route" })
            {
                var game = Start(Fixture()); Mode(c, game, WorkAssignmentMode.EmergencyOnly, enabled: reason != "disabled", emergency: reason != "emergency",
                    area: reason == "area" ? "reserve.home" : null, roles: reason == "role" ? Array.Empty<WorkerRoleId>() : null);
                if (reason == "route") c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "actual route can close");
                var response = Raise(c, game); c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "standby cannot bypass current emergency guard: " + reason);
                c.That(response.State != OrderState.Done, "blocked emergency is never instantly repaired: " + reason);
            }
        }
        private static void LoadedHaulContracts(Check c)
        {
            foreach (bool publicPolicy in new[] { false, true })
            {
                var game = Start(Fixture(publicPolicy)); var hauls = game.World.Services.Get<IHaulExecution>(); var source = Find(game, "source"); var destination = Find(game, "destination");
                ulong hash = game.Hash(); var preview = hauls.PreviewHaul(source.Owner, source.Id, destination.Id, Supply(game), Fix.FromInt(4));
                c.Equal(HaulStatus.PolicyBlocked, preview.Status, "ordinary haul refuses responder reserve"); c.Equal(WorkPolicyCodes.Standby, preview.Blocker, "haul shares exact standby blocker");
                c.Equal(0, hauls.AcquireHaul(source.Owner, source.Id, destination.Id, Supply(game), Fix.FromInt(4), int.MaxValue), "no capacity/endpoints/stock claims granted to ordinary high-priority haul");
                c.Equal(hash, game.Hash(), "standby haul refusal does not allocate job or receipt identity");
            }
        }
        private static void Physical(Check c)
        {
            foreach (bool publicPolicy in new[] { false, true }) foreach (bool publicWorker in new[] { false, true }) Response(c, publicPolicy, publicWorker);
            foreach (bool publicPolicy in new[] { false, true }) { Supplied(c, publicPolicy);
                foreach (bool publicWorker in new[] { false, true }) { ContinueOrdinary(c, publicPolicy, publicWorker);
                    ContinueHaul(c, publicPolicy, publicWorker); CancelAndPreempt(c, publicPolicy, publicWorker); } }
        }
        private static void Response(Check c, bool publicPolicy, bool publicWorker)
        {
            var factory = Fixture(publicPolicy, publicWorker); var game = Start(factory); var isolated = Start(factory); ulong untouched = isolated.Hash();
            var ordinary = Post(c, game); var response = Raise(c, game);
            c.Equal(response.Id, Assigned(game).OrderId, "actual incident uses real standby machine before queued ordinary work");
            c.Equal(Fix.FromInt(20), Stock(game, "source"), "ordinary work has no resource spending during emergency response");
            var twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); long update = game.Clock.StepIndex;
            Step(game, twin); c.Equal(paused, game.Hash(), "pause freezes sole clock and response progress"); c.Equal(update, game.Clock.StepIndex, "no independent preparedness clock");
            game.Clock.Resume(); twin.Clock.Resume(); Until(game, () => response.State == OrderState.Done, "physical native/public emergency repair", twin);
            c.That(!Find(game, "destination").Get<ContractIncident>().ReadIncident().Active, "only actual timed response effect resolves current incident");
            c.Equal(OrderState.Active, ordinary.State, "ordinary retained job continues waiting instead of disappearing");
            for (int i = 0; i < 4; i++) Step(game, twin);
            c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "released worker returns to standby rather than taking ordinary backlog");
            c.Equal(new Code("state.standby"), Find(game, "machine").State, "entity Update settles standby after actual response completion");
            c.Equal(game.Hash(), twin.Hash(), "saved physical response/standby continuation is exact"); c.Equal(untouched, isolated.Hash(), "reserve is session-local actual settings");
        }
        private static void Supplied(Check c, bool publicPolicy)
        {
            var factory = Fixture(publicPolicy, supplied: true); var game = Start(factory); var ordinary = Post(c, game); var response = Raise(c, game);
            Until(game, () => Assigned(game)?.IsHaul == true, "actual emergency material demand assigns standby carrier"); var haul = Assigned(game);
            c.Equal(Fix.Zero, Stock(game, "destination"), "response materials are not teleported");
            Until(game, () => haul.Loaded.IsPositive, "timed emergency loading earns cargo"); var twin = factory.Load(game.Save());
            Until(game, () => response.State == OrderState.Done, "physical supply delivery then emergency repair", twin);
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "eight actual emergency inputs depart source exactly once");
            c.Equal(Fix.Zero, Stock(game, "destination"), "delivered emergency materials consumed by real response work");
            c.Equal(0U, ordinary.Node("work").Progress.Fraction, "queued ordinary work gains no fabricated progress");
            c.Equal(0L, ordinary.Node("work").Progress.WholeTicks, "ordinary remains unstarted despite priority");
            c.Equal(game.Hash(), twin.Hash(), "partial loading/delivery/repair restore is deterministic");
        }
        private static void ContinueOrdinary(Check c, bool publicPolicy, bool publicWorker)
        {
            var factory = Fixture(publicPolicy, publicWorker); var game = Start(factory); Mode(c, game, WorkAssignmentMode.All); var ordinary = Post(c, game);
            Until(game, () => ordinary.Node("work").Progress.WholeTicks >= 3, "ordinary paid work earns progress before standby edit"); var old = Assigned(game);
            var twin = factory.Load(game.Save()); Mode(c, game, WorkAssignmentMode.EmergencyOnly); Mode(c, twin, WorkAssignmentMode.EmergencyOnly);
            c.Equal(old.Id, Assigned(game).Id, "standby edit preserves actual incumbent worker/job relation");
            c.Equal(Code.None, game.World.Services.Get<IWorkerDispatch>().CheckJob(game.World, Find(game, "machine"), old), "actual live ordinary commitment is allowed to finish");
            c.Equal(WorkPolicyCodes.Standby, game.World.Services.Get<IWorkerDispatch>().CheckAssignment(game.World, Find(game, "machine"), new PlayerId(0),
                new WorkerRoleId("inspect"), new[] { Find(game, "destination") }, false, old.Id), "existing job ID cannot authorize unrelated new destination");
            Entity actor = Find(game, "machine"); var dispatch = game.World.Services.Get<IWorkerDispatch>();
            EntityId linked = old.Worker; old.Worker = EntityId.None;
            try { c.Equal(WorkPolicyCodes.Standby, dispatch.CheckAssignment(game.World, actor, actor.Owner, old.Role,
                new[] { Find(game, "source") }, false, old.Id), "one-sided worker link is not incumbent continuation"); }
            finally { old.Worker = linked; }
            actor.Get<IWorker>().Job = 0;
            try { c.Equal(WorkPolicyCodes.Standby, dispatch.CheckAssignment(game.World, actor, actor.Owner, old.Role,
                new[] { Find(game, "source") }, false, old.Id), "one-sided job link is not incumbent continuation"); }
            finally { actor.Get<IWorker>().Job = old.Id; }
            Until(game, () => ordinary.State == OrderState.Done, "retained ordinary work completes physically", twin);
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "same ordinary materials consumed once; held tools released");
            c.Equal(game.Hash(), twin.Hash(), "saved ordinary work finishes identically across standby edit");
            Post(c, game); c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "next ordinary assignment must respect standby");
        }
        private static void ContinueHaul(Check c, bool publicPolicy, bool publicWorker)
        {
            var factory = Fixture(publicPolicy, publicWorker, haulWorker: true); var game = Start(factory); Mode(c, game, WorkAssignmentMode.All);
            int id = game.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(4), 1);
            c.That(id > 0, "player's ordinary supply request creates actual physical haul"); var haul = Board(game).Job(id);
            Until(game, () => haul.Loaded.IsPositive, "ordinary loading earns actual cargo before standby edit"); var twin = factory.Load(game.Save());
            Mode(c, game, WorkAssignmentMode.EmergencyOnly); Mode(c, twin, WorkAssignmentMode.EmergencyOnly);
            c.Equal(id, Assigned(game).Id, "standby edit never strands or releases loaded carrier");
            Until(game, () => Stock(game, "destination") == Fix.FromInt(4) && Find(game, "machine").Get<IWorker>().Job == 0, "actual loaded ordinary commitment delivers then returns to standby", twin);
            c.Equal(Fix.FromInt(16), Stock(game, "source"), "cargo leaves actual source only once");
            c.Equal(game.Hash(), twin.Hash(), "saved loaded ordinary haul remains exact across standby edit");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "ended loaded haul/standby source checkpoint exactly");
        }
        private static void CancelAndPreempt(Check c, bool publicPolicy, bool publicWorker)
        {
            foreach (bool cancel in new[] { false, true })
            {
                var factory = Fixture(publicPolicy, publicWorker); var game = Start(factory); Mode(c, game, WorkAssignmentMode.All); var ordinary = Post(c, game);
                Until(game, () => ordinary.Node("work").Progress.WholeTicks >= 3, "ordinary incumbent earns physical work before interruption");
                var incumbent = Assigned(game); Mode(c, game, WorkAssignmentMode.EmergencyOnly); var twin = factory.Load(game.Save());
                if (cancel)
                {
                    foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0,
                        new CancelOrderCommand { Order = ordinary.Id }).Accepted, "player cancels actual incumbent through ordinary command");
                    c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "cancel releases actual worker relation, not a reserve roster");
                    c.Equal(Fix.FromInt(12), Stock(game, "source"), "cancel never refunds physically consumed materials");
                    c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Available(Supply(game)), "cancel releases actual held tool claim");
                    c.Equal(game.Hash(), twin.Hash(), "saved cancellation under standby is exact");
                    Post(c, game); c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "cancelled job cannot exempt a fresh ordinary assignment");
                }
                else
                {
                    var response = Raise(c, game); Raise(c, twin);
                    c.Equal(response.Id, Assigned(game).OrderId, "actual emergency preempts interruptible standby incumbent physically");
                    c.Equal(JobState.Open, incumbent.State, "interrupted ordinary work retains existing job and progress");
                    Until(game, () => response.State == OrderState.Done, "response completes before retained ordinary admission", twin);
                    for (int i = 0; i < 3; i++) Step(game, twin);
                    c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "unassigned preempted work must respect standby when reacquiring");
                    c.Equal(WorkPolicyCodes.Standby, incumbent.Blocker, "retained queue exposes standby refusal after emergency release");
                    c.Equal(game.Hash(), twin.Hash(), "saved preemption and ordinary wait are exact");
                }
            }
        }
    }
}
