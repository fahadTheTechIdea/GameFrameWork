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
    internal static class IncidentReferenceReplacementTests
    {
        internal const string Case = "Incident replacement references: delayed exposures retain episodes timing and due-time eligibility";
        private static readonly Code Leak = new Code("leak"), Spread = new Code("spread"), Kind = new Code("machine.leak");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool direct, string refusal = null)
        {
            var root = Json.Parse(Colony.Defs);
            var scenario = root.Get("scenarios")[0];
            scenario.Get("settings").Get(WorldSetupModule.ModuleId).Set("profile", JsonValue.Of("propagation.start"));
            scenario.Get("settings").Set(IncidentPropagationModule.ModuleId, Json.Parse("{\"budget\":100}"));
            var propagation = Json.Parse(FrameworkProofContent.Read("incident-propagation.json"));
            propagation.Get("tile_incident_propagation_rules")[0].Get("interval").Set("numerator", JsonValue.Number("60"));
            propagation.Get("tile_incident_propagation_rules")[0].Get("delay").Set("numerator", JsonValue.Number("40"));
            var source = propagation.Get("entities")[0];
            if (direct)
            {
                var parts = source.Get("components"); var binding = parts.Get("incident_propagation").Clone();
                parts.Remove("incident_propagation"); parts.Set("proof_incident_propagation", binding);
            }
            var target = source.Clone(); target.Set("id", JsonValue.Of("propagation.target")); propagation.Get("entities").Add(target);
            var next = target.Clone(); next.Set("id", JsonValue.Of("propagation.next"));
            next.Get("components").Get("single_resource_port").Set("initial", JsonValue.Number("0"));
            if (refusal == "missing") next.Get("components").Remove("incident");
            if (refusal == "rule") next.Get("components").Get(direct ? "proof_incident_propagation" : "incident_propagation").Set("rule", JsonValue.Of("leak.permitted-spread"));
            // A target without an emitter can still own an incident, but deleting a live emitter record must refuse.
            if (refusal == "emitter") next.Get("components").Remove(direct ? "proof_incident_propagation" : "incident_propagation");
            if (refusal == "missing") next.Get("components").Remove(direct ? "proof_incident_propagation" : "incident_propagation");
            propagation.Get("entities").Add(next);
            JsonValue crane = null;
            for (int i = 0; i < root.Get("entities").Count; i++)
                if (root.Get("entities")[i].Get("id").Text == "crane") crane = root.Get("entities")[i].Clone();
            crane.Set("id", JsonValue.Of("propagation.crew"));
            crane.Get("components").Get("worker").Set("roles", Json.Parse("[\"inspect\"]"));
            crane.Get("components").Remove("work_policy"); root.Get("entities").Add(crane);
            var phases = Json.Parse(FrameworkProofContent.Read("incident-phases.json"));
            phases.Remove("work_cost_requirements");
            var requirements = phases.Get("work_requirement_groups")[0].Get("entries"); var physical = JsonValue.NewArray();
            for (int i = 0; i < requirements.Count; i++)
                if (requirements[i].Get("id").Text != "cost") physical.Add(requirements[i].Clone());
            phases.Get("work_requirement_groups")[0].Set("entries", physical);
            if (refusal == "workflow")
            {
                var workflow = phases.Get("incident_workflows")[0].Clone();
                workflow.Set("id", JsonValue.Of("replacement.other")); workflow.Set("kind", JsonValue.Of("replacement.other"));
                phases.Get("incident_workflows").Add(workflow);
                next.Get("components").Get("incident").Set("workflow", JsonValue.Of("replacement.other"));
            }
            return Colony.Factory(root.ToString(), additionalDocuments: new Dictionary<string, string> {
                { "incident-phases.json", phases.ToString() },
                { "incident-propagation.json", propagation.ToString() },
                { "incident-replacement.json", FrameworkProofContent.Read("incident-replacement.json") } });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "colony", Seed = 313, Seats = new List<Controller> { Controller.Human, Controller.Human }, Mods = { IncidentPropagationModule.ModuleId, ResourceFlowsModule.ModuleId } });
        private static EntityId Find(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static IncidentPropagationSnapshot Facts(SimGame game, EntityId source)
        {
            if (!game.Seat(0).View.Feature<IncidentPropagationView>().TryRead(source, Spread, out var facts))
                throw new InvalidOperationException("missing propagation binding " + source);
            return facts;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("incident replacement fault: " + game.Fault + "/" + twin?.Fault);
        }
        private static void Send(Check c, SimGame game, Command command, SimGame twin = null)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary public admission");
            int sequence = game.Seat(0).Submit(command); twin?.Seat(0).Submit(command); Step(game, twin);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Any(r => r.Sequence == sequence && r.Accepted), "normal command inbox commits intent");
        }
        private static void Run(Check c)
        {
            foreach (bool direct in new[] { false, true })
            foreach (bool alreadyActive in new[] { false, true })
            {
                var factory = Factory(direct); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                var source = Find(game, "spreading-machine"); var old = Find(game, "propagation.target");
                Send(c, game, new RaiseIncidentCommand { Target = source, Source = Leak, Kind = Kind });
                for (int i = 0; i < 70 && !Facts(game, source).Pending.Any(p => p.Target == old); i++) Step(game);
                var captured = Facts(game, source); var pending = captured.Pending.Single(p => p.Target == old);
                c.That(pending.ExpectedEpisode == 0 && pending.DueTick - pending.ScheduledTick == 40, "actual delayed exposure records original target episode and authored duration");
                var command = new PostOrderCommand { Plan = "propagation.upgrade", Target = old }; ulong before = game.Hash();
                c.Equal(Code.None, game.Seat(0).Preview(command), "compatible incident target can be physically replaced"); c.Equal(before, game.Hash(), "reference check cannot alter scan/exposures");
                Send(c, game, command); var twin = factory.Load(game.Save());
                for (int i = 0; i < 20 && !game.Seat(0).Snapshot.Entities.Any(e => e.Def == "propagation.next"); i++) Step(game, twin);
                var made = Find(game, "propagation.next"); var retained = Facts(game, source).Pending.Single(p => p.Target == made);
                c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Id == old), "old live identity retires only after physical work");
                c.Equal(pending.ExpectedEpisode, retained.ExpectedEpisode, "expected episode is retained, not reset");
                c.Equal(pending.ScheduledTick, retained.ScheduledTick, "no exposure rescheduling"); c.Equal(pending.DueTick, retained.DueTick, "original delivery deadline is retained");
                c.That(captured.Pending.Any(p => p.Target == old), "captured facts remain detached historical values");
                c.Equal(-1L, Facts(game, made).NextCycleTick, "inactive target emitter record rebinds without fabricated timing");
                c.Equal(game.Hash(), twin.Hash(), "unfinished replacement save continues identically");
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                if (alreadyActive) Send(c, game, new RaiseIncidentCommand { Target = made, Source = Leak, Kind = Kind }, twin);
                while (game.Tick <= pending.DueTick) Step(game, twin);
                c.That(Facts(game, source).Pending.Count == 0, "due exposure is removed exactly once");
                c.Equal(alreadyActive ? IncidentPropagationCodes.TargetActive : Code.None, Facts(game, source).Blocker, "delivery rechecks actual target eligibility");
                c.That(game.Seat(0).View.Feature<IncidentsView>().TryGetIncident(made, Leak, out var incident) && incident.Active,
                    "actual native incident target becomes or remains active");
                if (!alreadyActive) c.Equal(pending.DueTick, incident.RaisedTick, "replacement never accelerates or restarts exposure timing");
                c.Equal(game.Hash(), twin.Hash(), "saved delayed exposure delivers identically");
                c.Equal(isolatedHash, isolated.Hash(), "interleaved independent session stays unchanged");
                c.That(!game.Seat(1).View.Feature<IncidentPropagationView>().TryRead(source, Spread, out _), "another company cannot inspect exposures");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "delivered incident and reference state load coherently");
            }
            foreach (string mode in new[] { "missing", "emitter", "rule", "workflow" })
            {
                var factory = Factory(false, mode); var game = New(factory); var source = Find(game, "spreading-machine"); var old = Find(game, "propagation.target");
                Send(c, game, new RaiseIncidentCommand { Target = source, Source = Leak, Kind = Kind });
                for (int i = 0; i < 70 && !Facts(game, source).Pending.Any(p => p.Target == old); i++) Step(game);
                c.That(Facts(game, source).Pending.Any(p => p.Target == old), "refusal starts with actual pending work");
                ulong before = game.Hash(); var command = new PostOrderCommand { Plan = "propagation.upgrade", Target = old };
                c.Equal(IncidentPropagationCodes.ReplacementBinding, game.Seat(0).Preview(command), "incompatible live incident/emitter binding refuses");
                c.Equal(before, game.Hash(), "refusal leaves original backlog intact");
                int sequence = game.Seat(0).Submit(command); Step(game); long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.That(results.Any(r => r.Sequence == sequence && r.Refusal == IncidentPropagationCodes.ReplacementBinding), "inbox and preview share exact refusal");
                c.That(game.Seat(0).Snapshot.Entities.Any(e => e.Id == old), "refused replacement does not retire target");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused backlog remains saveable");
            }
        }
    }
}
