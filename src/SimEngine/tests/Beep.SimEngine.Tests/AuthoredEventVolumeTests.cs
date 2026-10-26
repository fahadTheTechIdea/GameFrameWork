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
    // Actual master/files and public player input only; all physical work/events execute in engine modules.
    internal static class AuthoredEventVolumeTests
    {
        internal const string Case = "Authored event volume: physical completion exceeds old cap and explicit policies bound dispatch";
        internal const int Burst = 1048584;
        internal const string NativeCase = "Authored event volume native: physical completion exceeds old cap with explicit finite policy";
        internal const string BulkFaultCase = "Authored event volume fault: physical bulk budget refuses before delivery";
        internal const string ChainFaultCase = "Authored event volume fault: physical chained budget stops before next listener";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(NativeCase, c => Physical(c, false, "events.large"));
            TestRunner.Add(BulkFaultCase, c => Refusal(c, "bulk"));
            TestRunner.Add(ChainFaultCase, c => Refusal(c, "chain"));
        }
        internal static GameFactory Factory(bool half, string profile, EventVolumeModule module,
            Action<JsonValue> policyEdit = null, bool missingSettings = false, string mode = "bulk", int count = Burst)
        {
            var json = Json.Parse(PointGame.Definitions); var scenario = json.Get("scenarios")[0];
            scenario.Set("seats", JsonValue.Number("2")); scenario.Get("modules").Add(JsonValue.Of(module.Id));
            if (profile != null)
            {
                scenario.Get("modules").Add(JsonValue.Of(EventDispatchPolicyModule.ModuleId));
                if (!missingSettings) scenario.Get("settings").Set(EventDispatchPolicyModule.ModuleId, Json.Parse("{\"profile\":\"" + profile + "\"}"));
            }
            if (half) json.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            var policies = Json.Parse(FrameworkProofContent.Read("event-dispatch-policies.json")); policyEdit?.Invoke(policies);
            var work = Json.Parse(FrameworkProofContent.Read("event-volume.json"));
            work.Get("event_burst_effects")[0].Set("count", JsonValue.Number(count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            work.Get("event_burst_effects")[0].Set("mode", JsonValue.Of(mode));
            return PointGame.Factory(json.ToString(), extraModules: new GameModule[] { module, new EventDispatchPolicyModule() },
                extraDeclarations: "[{\"id\":\"proof.event-volume\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"event-volume.json\"]},{\"id\":\"sim.event-dispatch-policy\",\"version\":\"1.0.0\",\"documents\":[\"event-dispatch-policies.json\"]}]",
                extraDocuments: new Dictionary<string,string> { ["event-volume.json"] = work.ToString(), ["event-dispatch-policies.json"] = policies.ToString() });
        }
        internal static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 955, Seats = { Controller.Human, Controller.Human } });
        internal static EntityId Entity(SimGame g, string name) => g.Seat(0).Snapshot.Entities.Single(x => x.Def == name).Id;
        internal static WorkView Work(SimGame g) => g.Seat(0).View.Feature<WorkView>();
        internal static EventVolumeView Events(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<EventVolumeView>();
        internal static PlanView Plan(SimGame g, long id)
        { var rows = new List<PlanView>(); Work(g).CopyPlans(rows); return rows.Single(x => x.Id == id); }
        private static long Start(Check c, SimGame g, bool paused)
            => PermanentRemovalTests.Submit(c, g, PlanCommand(g, paused)).Arg0;
        internal static SubmitPlanCommand PlanCommand(SimGame g, bool paused)
            => new SubmitPlanCommand { Intent = new PlanIntent("activity:events.inspect",
                Json.Parse("{\"target\":" + Entity(g, "destination").Value + "}"), "events", 50, paused) };
        internal static HaulResourcesCommand SupplyCommand(SimGame g)
            => new HaulResourcesCommand { Source = Entity(g, "source"), Destination = Entity(g, "destination"),
                Worker = Entity(g, "machine"), Resource = PermanentRemovalTests.Supply(g), Ceiling = Fix.FromInt(4), Priority = 100 };
        private static void Reach(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 160 && !done(); i++) PermanentRemovalTests.Step(g, twin);
            if (!done()) throw new InvalidOperationException(stage + " exceeded160Updates clock=" + g.Clock.StepIndex + " fault=" + g.Fault);
        }
        private static void Admission(Check c)
        {
            Action<JsonValue>[] invalid = {
                x => x.Get("event_dispatch_policies")[0].Remove("maximum_events_per_dispatch"),
                x => x.Get("event_dispatch_policies")[0].Set("maximum_events_per_dispatch", JsonValue.Number("0")),
                x => x.Get("event_dispatch_policies")[0].Set("maximum_events_per_dispatch", JsonValue.Number("-1")),
                x => x.Get("event_dispatch_policies")[0].Set("maximum_events_per_dispatch", JsonValue.Number("1.5")),
                x => x.Get("event_dispatch_policies")[0].Set("maximum_events_per_dispatch", JsonValue.Of("unlimited")),
                x => x.Get("event_dispatch_policies")[0].Set("maximum_events_per_dispatch", JsonValue.Number("9223372036854775808")),
                x => x.Get("event_dispatch_policies")[0].Set("legacy_limit", JsonValue.Number("1024"))
            };
            foreach (var edit in invalid)
            {
                var module = new EventVolumeModule();
                c.Throws<ContentException>(() => New(Factory(false, "events.large", module, edit)), "unused malformed policy refuses before Initialize");
                c.Equal(0, module.Initialize.Initializations, "no system initialized after malformed policy");
            }
            foreach (bool missing in new[] { false, true })
            {
                var module = new EventVolumeModule();
                c.Throws<ContentException>(() => New(Factory(false, missing ? "events.large" : "missing", module, missingSettings: missing)), "selected policy needs actual profile/settings");
                c.Equal(0, module.Initialize.Initializations, "missing policy selection never initializes");
            }
        }
        private static void Physical(Check c, bool half, string profile)
        {
            var module = new EventVolumeModule(); var f = Factory(half, profile, module); var g = New(f); var idle = New(f); ulong isolation = idle.Hash();
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "event policy requires no business or platform");
            var policy = g.Seat(0).View.Feature<EventDispatchPolicyView>();
            if (profile == null) c.That(policy == null && !g.Modules.Contains(EventDispatchPolicyModule.ModuleId), "absent policy imposes no fabricated cap or view");
            else c.That(policy.Policy == profile && policy.MaximumEventsPerDispatch == (profile == "events.unlimited" ? (long?)null : 2000000L), "actual native/independent policy projects immutable metadata");
            var limits = new ReplayLimits(2 * 1024 * 1024, 2000); using var recorder = g.RecordReplay(limits);
            c.That(g.Seat(0).View.TryFindResource("supply", out var resource), "file-authored supply");
            PermanentRemovalTests.Submit(c, g, SupplyCommand(g));
            var twin = f.Load(g.Save()); Reach(g, twin, () => PermanentRemovalTests.Facts(g, Entity(g, "destination")).Stock == Fix.FromInt(4), "physical supply arrival");
            long plan = Start(c, g, true); twin = f.Load(g.Save());
            c.Equal(0L, Events(g).ReadCount(new PlayerId(0)), "intent grants no completion events");
            g.Clock.Pause(); twin.Clock.Pause(); ulong paused = g.Hash();
            c.Equal(0, g.Update(100000), "single paused game clock blocks authored completion"); c.Equal(0, twin.Update(100000), "saved sole clock also blocks");
            c.That(g.Hash() == paused && g.Hash() == twin.Hash(), "paused intent retains exact state"); g.Clock.Resume(); twin.Clock.Resume();
            PermanentRemovalTests.Submit(c, g, new ConfigurePlanCommand { Plan = plan, Group = "events", Priority = 50, Paused = false });
            twin = f.Load(g.Save()); Reach(g, twin, () => Plan(g, plan).Order > 0, "physical plan activation"); int order = Plan(g, plan).Order;
            var nodes = new List<ActivityNodeView>(); Reach(g, twin, () => Work(g).TryCopyNodes(order, nodes) && nodes.Count == 1 && nodes[0].Progress.WholeTicks > 0,
                "earned inspection prefix before event effect");
            c.Equal(0L, Events(g).ReadCount(new PlayerId(0)), "partial physical work grants no event result");
            twin = f.Load(g.Save()); Reach(g, twin, () => Plan(g, plan).State == PlanState.Done, "earned large event dispatch");
            ulong pure = g.Hash(); c.Equal((long)Burst, Events(g).ReadCount(new PlayerId(0)), "more than old global cap delivers every event exactly once");
            c.Equal(Burst, Events(g).ReadLate(new PlayerId(0)), "global sequence reaches final listener in exact order");
            c.Equal(0L, Events(g, 1).ReadCount(new PlayerId(0)), "private downstream event facts remain company-scoped");
            c.Equal(pure, g.Hash(), "detached event/policy views mutate no RNG/engine state");
            var retained = policy;
            for (int i = 0; i < 3; i++) PermanentRemovalTests.Step(g, twin);
            c.Equal((long)Burst, Events(g).ReadCount(new PlayerId(0)), "terminal updates neither replay nor duplicate effect");
            c.Equal(Fix.FromInt(20), PermanentRemovalTests.Total(g), "physical stock is conserved independently of event volume");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "large event receipts save through original service codec");
            c.That(new ReplayVerifier().Verify(f, recorder.Finish(), limits).Success, "physical public input and large events replay deterministically");
            c.Equal(isolation, idle.Hash(), "large event session does not mutate interleaved session");
            c.That(retained == null || retained.Policy == profile, "retained selected policy metadata remains detached");
        }
        private static void Refusal(Check c, string mode)
        {
            var module = new EventVolumeModule(); var f = Factory(false, "events.small", module, mode: mode, count: 257); var g = New(f);
            Start(c, g, false); g = f.Load(g.Save());
            long before = g.Clock.StepIndex;
            for (int i = 0; i < 160 && g.Fault == null; i++) g.Update();
            c.That(g.Fault != null && g.Fault.Phase == "events.budget" && g.Clock.StepIndex > before, "authored budget faults only after actual timed work");
            if (mode == "bulk") c.Equal(0L, module.RefusedCount, "budget refuses before queued prefix is dispatched");
            else c.That(module.RefusedCount > 0 && module.RefusedCount <= 128 && module.RefusedLate == module.RefusedCount - 1,
                "handler-produced budget failure stops before the next listener, without rollback fiction");
            c.That(!module.HasPendingAfterFailure && !module.PendingAfterFailure, "original event bus discards undeliverable events even inside a handler");
            var fault = g.Fault; long stopped = g.Clock.StepIndex;
            c.Equal(0, g.Update(50000), "exceeded authored budget stops future updates");
            c.That(ReferenceEquals(fault, g.Fault) && g.Clock.StepIndex == stopped, "original first fault and single clock remain stable");
            c.Throws<InvalidOperationException>(() => g.Save(), "partial event failure cannot produce save fiction");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current authored event policy build identity"); Admission(c);
            foreach (bool half in new[] { false, true }) foreach (string profile in new[] { null, "events.unlimited", "events.independent" }) Physical(c, half, profile);
            Physical(c, false, "events.large"); Refusal(c, "bulk"); Refusal(c, "chain");
        }
    }
}
