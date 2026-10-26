using System;
using System.Collections.Generic;
using System.IO;
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
    internal static class AuthoredLockstepPolicyTests
    {
        internal const string Case = "Authored lockstep: physical work and shuffled large frames obey explicit JSON policies";
        internal const string AdmissionCase = "Authored lockstep declaration: explicit limits references and pre-initialization refusal";
        internal const string RefusalCase = "Authored lockstep refusal: invalid windows frames and policy hello remain terminal";
        internal static void Register()
        { TestRunner.Add(Case, Run); TestRunner.Add(AdmissionCase, Admission); TestRunner.Add(RefusalCase, Refusal); }
        internal static GameFactory Factory(bool half, string profile, LockstepPolicyProofModule proof, Action<JsonValue> edit = null, bool missingSettings = false, bool recordFlows = false)
        {
            var json = Json.Parse(PointGame.Definitions); var scenario = json.Get("scenarios")[0];
            scenario.Set("seats", JsonValue.Number("2")); scenario.Get("modules").Add(JsonValue.Of(proof.Id));
            if (recordFlows) scenario.Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
            if (half) json.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            if (profile != null)
            {
                scenario.Get("modules").Add(JsonValue.Of(LockstepPolicyModule.ModuleId));
                if (!missingSettings) scenario.Get("settings").Set(LockstepPolicyModule.ModuleId, Json.Parse("{\"profile\":\"" + profile + "\"}"));
            }
            var policies = Json.Parse(FrameworkProofContent.Read("lockstep-policies.json")); edit?.Invoke(policies);
            return PointGame.Factory(json.ToString(), extraModules: new GameModule[] { proof, new LockstepPolicyModule() },
                extraDeclarations: "[{\"id\":\"proof.lockstep-policy\",\"version\":\"1.0.0\",\"documents\":[\"independent-lockstep-policies.json\"]},{\"id\":\"sim.lockstep-policy\",\"version\":\"1.0.0\",\"documents\":[\"lockstep-policies.json\"]}]",
                extraDocuments: new Dictionary<string,string> {
                    ["lockstep-policies.json"] = policies.ToString(),
                    ["independent-lockstep-policies.json"] = FrameworkProofContent.Read("independent-lockstep-policies.json") });
        }
        internal static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 966, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Entity(SimGame g, string name) => g.Seat(0).Snapshot.Entities.Single(x => x.Def == name).Id;
        private static WorkView Work(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<WorkView>();
        private static PlanView Plan(SimGame g, long id)
        { var rows = new List<PlanView>(); Work(g).CopyPlans(rows); return rows.Single(x => x.Id == id); }
        // Transport observations only: no interpreted gameplay, command decision or authoritative mutation.
        private sealed class Packets { internal int Largest, Commands, Nonempty; }
        private static void Exchange(SimGame from, SimGame to, Packets facts)
        {
            var packets = new List<byte[]>(); from.Net.DrainOutgoing(packets); packets.Reverse();
            foreach (byte[] packet in packets)
            {
                if (facts != null && packet[0] == 1)
                {
                    var reader = new StateReader(packet, 1, packet.Length - 1);
                    long step = 0; int seat = 0, speed = 0, count = 0; byte control = 0;
                    reader.Long(ref step); reader.Int(ref seat); reader.Byte(ref control); reader.Int(ref speed); reader.Int(ref count);
                    facts.Largest = Math.Max(facts.Largest, count); facts.Commands += count; if (count > 0) facts.Nonempty++;
                }
                to.Net.Receive(packet);
            }
        }
        private static void Round(SimGame a, SimGame b, Packets facts)
        {
            a.Update(); b.Update(); Exchange(a, b, facts); Exchange(b, a, null);
            if (a.Fault != null || b.Fault != null || a.Net.State == NetState.Broken || b.Net.State == NetState.Broken ||
                a.Net.State == NetState.Desynced || b.Net.State == NetState.Desynced)
                throw new InvalidOperationException("physical network stopped: " + a.Clock.StepIndex + "/" + b.Clock.StepIndex + " " + a.Fault + "/" + b.Fault + " " + a.Net.State + "/" + b.Net.State);
        }
        private static void Reach(SimGame a, SimGame b, Packets facts, Func<bool> done, int bound, string phase)
        {
            for (int i = 0; i < bound && !done(); i++) Round(a, b, facts);
            if (!done()) throw new InvalidOperationException(phase + " exceeds " + bound + " updates at " + a.Clock.StepIndex + "/" + b.Clock.StepIndex);
        }
        private static void Admission(Check c)
        {
            foreach (string field in new[] { "maximum_ahead", "maximum_commands_per_frame" })
                foreach (string bad in new[] { "missing", "0", "-1", "1.5", "\"4\"", "9223372036854775808" })
                {
                    var proof = new LockstepPolicyProofModule();
                    c.Throws<ContentException>(() => New(Factory(false, "net.large", proof, json => {
                        var unused = json.Get("lockstep_policies")[0];
                        if (bad == "missing") unused.Remove(field); else unused.Set(field, Json.Parse(bad));
                    })), "unused policy rejects " + field + "=" + bad);
                    c.Equal(0, proof.Initialize.Initializations, "invalid declaration refuses before Initialize");
                }
            foreach (string profile in new[] { "missing", "net.large" })
            {
                var proof = new LockstepPolicyProofModule();
                c.Throws<ContentException>(() => New(Factory(false, profile, proof, missingSettings: profile == "net.large")), "selected profile/settings must exist");
                c.Equal(0, proof.Initialize.Initializations, "missing selection refuses before Initialize");
            }
            var unknown = new LockstepPolicyProofModule();
            c.Throws<ContentException>(() => New(Factory(false, "net.large", unknown, json => json.Get("lockstep_policies")[0].Set("legacy_window", JsonValue.Number("1024")))), "unknown legacy fields refuse");
            c.Equal(0, unknown.Initialize.Initializations, "unknown fields refuse before Initialize");
            // Both optional policies use the original JsonReader's one strict limit parser.
            var eventReader = new JsonReader(Json.Parse("{\"maximum_events_per_dispatch\":null}"), "event shared reader");
            c.That(!EventDispatchPolicyDeclaration.Read(eventReader).MaximumEventsPerDispatch.HasValue, "event explicit null uses same strict authored reader");
            eventReader.End();
        }
        private static void Physical(Check c, bool half, string profile, int delay)
        {
            var f = Factory(half, profile, new LockstepPolicyProofModule()); var a = New(f); var idle = New(f); ulong isolation = idle.Hash();
            c.That(!a.Modules.Contains(TycoonModule.ModuleId), "lockstep policy does not require business/platform");
            c.That(a.Seat(0).View.TryFindResource("supply", out var supply), "actual file-authored supply");
            PermanentRemovalTests.Submit(c, a, new HaulResourcesCommand { Source = Entity(a, "source"), Destination = Entity(a, "destination"),
                Worker = Entity(a, "machine"), Resource = supply, Ceiling = Fix.FromInt(4), Priority = 100 });
            var haulingTwin = f.Load(a.Save());
            for (int i = 0; i < 160 && PermanentRemovalTests.Facts(a, Entity(a, "destination")).Stock != Fix.FromInt(4); i++) PermanentRemovalTests.Step(a, haulingTwin);
            c.Equal(Fix.FromInt(4), PermanentRemovalTests.Facts(a, Entity(a, "destination")).Stock, "ordinary hauling earns destination material over time");
            long id = PermanentRemovalTests.Submit(c, a, new SubmitPlanCommand { Intent = new PlanIntent("activity:inspect",
                Json.Parse("{\"target\":" + Entity(a, "source").Value + "}"), "net", 1, true) }).Arg0;
            var b = f.Load(a.Save());
            var selected = a.Seat(0).View.Feature<LockstepPolicyView>();
            c.That(profile == null ? selected == null : selected.Policy == profile, "selected actual immutable policy metadata, never a fallback");
            var oa = new LockstepOptions { InputDelay = delay, ChecksumEvery = 1, LocalSeats = { 0 } };
            a.StartLockstep(oa); b.StartLockstep(new LockstepOptions { InputDelay = delay, ChecksumEvery = 1, LocalSeats = { 1 } });
            c.Equal(Code.None, a.Net.CheckHello(b.Net.Hello()), "actual selected policy participates in peer identity");
            oa.InputDelay = -1; oa.LocalSeats.Clear();
            c.That(a.Net.InputDelay == delay && a.Net.IsLocal(new PlayerId(0)), "session captures options and actual policy once");
            c.That(profile == null || profile == "net.unlimited"
                ? !a.Net.MaximumAhead.HasValue && !a.Net.MaximumCommandsPerFrame.HasValue
                : a.Net.MaximumAhead == (profile == "net.small" ? 4L : 2048L) && a.Net.MaximumCommandsPerFrame == (profile == "net.small" ? 4L : 400L),
                "absent/null/native/independent budgets apply through one coordinator");
            var packets = new Packets(); var limits = new ReplayLimits(4 * 1024 * 1024, 10000);
            using var left = a.RecordReplay(limits); using var right = b.RecordReplay(limits);
            for (int i = 1; i <= 300; i++)
            {
                var command = new ConfigurePlanCommand { Plan = id, Group = "net", Priority = i, Paused = i != 300 };
                ulong before = a.Hash(); c.That(a.Seat(0).Preview(command).IsNone, "public plan preview " + i);
                c.Equal(before, a.Hash(), "command preview is read-pure " + i);
                c.Equal(i + 2, a.Seat(0).Submit(command), "network proposals continue two actual earlier command identities " + i);
            }
            c.That(left.CommandCount == 0 && right.CommandCount == 0, "local pending proposals are not admitted/replayed commands");
            var nodes = new List<ActivityNodeView>();
            Reach(a, b, packets, () => Plan(a, id).Order > 0 && Work(a).TryCopyNodes(Plan(a, id).Order, nodes) &&
                nodes.Count == 1 && nodes[0].Progress.WholeTicks > 0, delay + 220, "physical post-input travel/work prefix");
            c.Equal(300, packets.Commands, "all pending commands travel once");
            c.Equal(profile == "net.small" ? 4 : 300, packets.Largest, "only authored frame budget splits input, never old256");
            c.Equal(profile == "net.small" ? 75 : 1, packets.Nonempty, "declared batching retains original queue/order");
            c.Equal(300, Plan(a, id).Priority, "last canonical input controls actual plan");
            c.Equal(a.Hash(), b.Hash(), "shuffled inputs reach same active physical state");
            var foreign = new List<PlanView>(); Work(b, 1).CopyPlans(foreign);
            c.Equal(0, foreign.Count, "network exposes no foreign plan facts");
            var restoredA = f.Load(a.Save()); var restoredB = f.Load(b.Save());
            c.Equal(a.Hash(), restoredA.Hash(), "partial physical save retains original engine state");
            c.Equal(b.Hash(), restoredB.Hash(), "other peer partial save agrees");
            Reach(a, b, packets, () => Plan(a, id).State == PlanState.Done && Plan(b, id).State == PlanState.Done, 160, "actual inspection completion");
            for (int i = 0; i < 160 && Plan(restoredA, id).State != PlanState.Done; i++) PermanentRemovalTests.Step(restoredA, restoredB);
            c.That(Plan(restoredA, id).State == PlanState.Done && restoredA.Hash() == restoredB.Hash(), "saved twin continuation earns same physical result");
            c.Equal(Fix.FromInt(20), PermanentRemovalTests.Total(a), "network input volume changes no conserved stock");
            c.Equal(a.Hash(), b.Hash(), "physical terminal peers agree");
            c.That(left.CommandCount == 300 && right.CommandCount == 300, "both peers record every actual logical admission once");
            byte[] l = left.Finish(), r = right.Finish();
            c.That(l.SequenceEqual(r), "reversed transport produces one canonical replay");
            var verification = new ReplayVerifier().Verify(f, l, limits);
            c.That(verification.Success, "physical lockstep stream replays through engine: " + verification.Failure + "/" + verification.Detail + " step=" + verification.Step);
            c.Equal(isolation, idle.Hash(), "interleaved idle session remains independent");
            c.That(selected == null || selected.Policy == profile, "retained immutable policy does not change during Update");
            var loaded = f.Load(a.Save()); loaded.StartLockstep(new LockstepOptions { InputDelay = delay, LocalSeats = { 0 } });
            c.That(loaded.Net.MaximumCommandsPerFrame == a.Net.MaximumCommandsPerFrame && loaded.Net.MaximumAhead == a.Net.MaximumAhead,
                "reattached saved session resolves identical authored policy, no serialized duplicate budget");
        }
        private static byte[] EmptyFrame(long step, int count = 0, bool trailing = false)
        {
            using var stream = new MemoryStream(); stream.WriteByte(1); var writer = new StateWriter(stream);
            writer.Long(ref step); int seat = 1, speed = 0; byte control = 0;
            // Negative protocol probe: encode the actual signed wire field so a
            // malformed count reaches NetSession, not the save collection guard.
            writer.Int(ref seat); writer.Byte(ref control); writer.Int(ref speed); writer.Int(ref count); writer.Flush();
            if (trailing) stream.WriteByte(7); return stream.ToArray();
        }
        private static void Refusal(Check c)
        {
            var f = Factory(false, "net.small", new LockstepPolicyProofModule()); var g = New(f); ulong before = g.Hash();
            c.Throws<ArgumentOutOfRangeException>(() => g.StartLockstep(new LockstepOptions { InputDelay = 5, LocalSeats = { 0 } }), "selected window refuses invalid session before attachment");
            c.That(g.Net == null && g.Hash() == before, "refused options have no authoritative prefix");
            foreach (byte[] packet in new[] { EmptyFrame(5), EmptyFrame(0, 5), EmptyFrame(-1), EmptyFrame(0, -1), EmptyFrame(0, trailing: true), new byte[] { 1 } })
            {
                var bad = New(f); bad.StartLockstep(new LockstepOptions { InputDelay = 0, LocalSeats = { 0 } }); ulong initial = bad.Hash();
                bad.Net.Receive(packet); c.Equal(NetState.Broken, bad.Net.State, "authored/structural packet refusal is terminal");
                c.Equal(initial, bad.Hash(), "refused packet commits no state or command prefix");
                bad.Net.Receive(EmptyFrame(0)); bad.Net.Receive(new byte[] { 4 });
                c.Equal(NetState.Broken, bad.Net.State, "later frames/resume never revive broken session");
                long stopped = bad.Clock.StepIndex; c.Equal(0, bad.Update(), "broken transport cannot drive gameplay Update");
                c.Equal(stopped, bad.Clock.StepIndex, "single game clock does not advance after refusal");
            }
            var a = New(f); var other = New(Factory(false, "net.large", new LockstepPolicyProofModule()));
            a.StartLockstep(new LockstepOptions { InputDelay = 0 }); other.StartLockstep(new LockstepOptions { InputDelay = 0 });
            c.That(!a.Net.CheckHello(other.Net.Hello()).IsNone, "different selected policy cannot silently join");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current policy identity"); Admission(c);
            foreach (bool half in new[] { false, true }) foreach (string profile in new[] { null, "net.unlimited", "net.independent" }) Physical(c, half, profile, 0);
            Physical(c, false, "net.large", 1025); Physical(c, false, "net.small", 0); Refusal(c);
        }
    }
}
