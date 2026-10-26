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
    // File selection, ordinary player commands and immutable engine facts only.
    internal static class ExplorationBindingTests
    {
        internal const string Case = "Exploration bindings: physical point discovery and saved interruption";
        private static readonly Code Source = new Code("binding.contact"), Sight = new Code("sensor.sight");
        private const string Provider = "sim.knowledge.facts";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(ExplorationBindingsModule module, bool independent = true,
            Action<JsonValue> edit = null, Action<JsonValue> sensorEdit = null,
            IReadOnlyList<GameModule> extraModules = null, IReadOnlyList<JsonValue> extraDeclarations = null,
            IReadOnlyDictionary<string, string> extraFiles = null, Action<JsonValue> definitionEdit = null)
        {
            var document = Json.Parse(FrameworkProofContent.Read("exploration-bindings.json"));
            var sensor = Json.Parse(FrameworkProofContent.Read("point-sensor.json"));
            var components = sensor.Get("entities")[0].Get("components");
            components.Set(independent ? "independent_contact" : "authored_exploration",
                Json.Parse("{\"id\":\"binding.contact\",\"sensor\":\"sensor.sight\",\"profile\":\"binding.profile\"}"));
            components.Set("point_dock", Json.Parse("{\"work_roles\":[],\"resources\":[\"supply\"],\"slots\":[{\"id\":\"binding.service\",\"kind\":\"service\",\"offset\":-1},{\"id\":\"binding.wait\",\"kind\":\"waiting\",\"offset\":-2},{\"id\":\"binding.depart\",\"kind\":\"departure\",\"offset\":-3}]}"));
            sensor.Get("entity_setups")[1].Set("company", JsonValue.Number("0"));
            sensor.Get("knowledge_visibility_policies")[0].Set("unlisted_entities", JsonValue.Of(true));
            if (!independent)
            { document.Set("exploration_profiles", document.Get("independent_contact_profiles")); document.Remove("independent_contact_profiles"); }
            edit?.Invoke(document); sensorEdit?.Invoke(sensor);
            var selected = new List<GameModule> { new ExplorationModule(), module };
            if (extraModules != null) selected.AddRange(extraModules);
            var declarations = new List<JsonValue> {
                    Json.Parse("{\"id\":\"sim.exploration\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[]}"),
                    Json.Parse("{\"id\":\"proof.exploration-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.exploration\",\"version\":\"1.0.0\"},{\"id\":\"proof.point-sensors\",\"version\":\"1.0.0\"}],\"documents\":[\"exploration-bindings.json\"]}") };
            if (extraDeclarations != null) declarations.AddRange(extraDeclarations);
            var files = new Dictionary<string,string> { ["exploration-bindings.json"] = document.ToString() };
            if (extraFiles != null) foreach (var file in extraFiles) files.Add(file.Key, file.Value);
            return PointSensorTests.Factory(sensor.ToString(), additionalModules: selected, additionalDeclarations: declarations,
                extraDocuments: files,
                definitionEdit: definitions => {
                    definitions.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
                    definitions.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ExplorationModule.ModuleId));
                    if (extraModules != null) foreach (var extra in extraModules) definitions.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(extra.Id));
                    definitionEdit?.Invoke(definitions);
                });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 42734 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static EntityId Entity(SimGame game, string definition)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static Fix Position(SimGame game)
            => ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Def == "sensor.machine").Spatial).Position;
        private static ExplorationSnapshot Read(SimGame game)
        {
            Code result = game.Seat(0).View.Feature<ExplorationView>().Read(Entity(game, "sensor.machine"), Source, out var snapshot);
            if (!result.IsNone) throw new InvalidOperationException("exploration " + result); return snapshot;
        }
        private static ExplorationEntrySnapshot Entry(SimGame game, string id) => Read(game).Entries.Single(e => e.Id == id);
        private static bool Known(SimGame game, string branch, int company = 0)
            => game.Seat(company).View.Feature<KnowledgeView>().TryRead(new PlayerId(company), Provider, "binding." + branch + "-known", out _);
        private static Fix Stock(SimGame game, string definition)
            => TransformationContentContractTests.Stock(game, definition);
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("exploration step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 480 && !done(); i++) Step(game, twin);
            if (!done()) {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " position=" + Position(game) +
                    " stock=" + Stock(game, "sensor.machine") + "/" + Stock(game, "source") +
                    " jobs=" + string.Join(";", jobs.Select(j => j.Id + "/" + j.Worker + "/" + j.State + "/" + j.Blocker + "/" + j.ToilIndex)) +
                    " entries=" + string.Join(";", Read(game).Entries.Select(e => e.Id + "/" + e.Progress + "/" + e.Blocker)));
            }
        }
        private static CommandResult Order(Check c, SimGame game, Command command, SimGame twin = null)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player preview");
            c.Equal(before, game.Hash(), "preview has no discovery side effects");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary player command");
            if (twin != null) { c.That(TestKit.Order(twin, 0, command).Accepted, "same normal command in saved twin"); c.Equal(game.Hash(), twin.Hash(), "command twin identity"); }
            return result;
        }
        private static void Control(Check c, SimGame game, bool enabled, SimGame twin = null)
            => Order(c, game, new ConfigureOperationCommand { Entity = Entity(game, "sensor.machine"), Operation = Sight, Enabled = enabled, Target = Fix.FromInt(3) }, twin);
        private static void Travel(Check c, SimGame game, string target)
            => Order(c, game, new PostOrderCommand { Plan = "binding.travel", Target = Entity(game, target), Priority = 70 });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current binding build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "original contact payload retained");
            c.Equal(typeof(Component), typeof(AuthoredExploration).BaseType, "reusable component does not require native binding definition");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Invalid(c); ValidationFaults(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new ExplorationBindingsModule(); var factory = Factory(module, independent); var game = New(factory);
            var idle = New(factory); ulong isolated = idle.Hash();
            c.That(module.ActualAliases && module.SharedComponent && module.Independent == independent, "actual profile alias configures sole original component");
            c.That(module.Initialize.Initializations > 0 && !game.Modules.Contains(TycoonModule.ModuleId), "resource-only startup with no Tycoon requirement");
            c.Equal("a.reset", Read(game).Entries[0].Id, "shared declaration canonicalizes authored unordered entries");
            c.Equal(ExplorationCodes.Outside, Entry(game, "a.reset").Blocker, "source initially outside authored non-tile region");
            c.That(!Known(game, "reset") && !Known(game, "retain"), "no discovery on placement");
            var retained = Read(game); ulong hash = game.Hash(); var snapshots = new List<ExplorationSnapshot>();
            game.Seat(0).View.Feature<ExplorationView>().CopySources(snapshots);
            c.Equal(1, snapshots.Count, "sole actual source in owned diagnostics");
            c.Equal(hash, game.Hash(), "diagnostics do not advance dwell or knowledge");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<ExplorationView>().Read(Entity(game, "sensor.machine"), Source, out var hidden), "foreign source diagnostics refused");
            c.That(hidden == null, "foreign source yields no profile/progress");
            c.Throws<NotSupportedException>(() => ((IList<ExplorationEntrySnapshot>)retained.Entries).Clear(), "detached facts immutable");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Order(c, game, new SetWorkerHeldCommand { Worker = Entity(game, "sensor.machine"), Held = true });
                game.Seat(0).View.TryFindResource("supply", out var resource);
                int route = checked((int)Order(c, game, new AddRouteCommand { From = Entity(game, "source"), To = Entity(game, "sensor.machine"), Resource = resource,
                    Settings = new LogisticsRouteSettings(true, Fix.One, 70) }).Arg0);
                c.Equal(Fix.Zero, Stock(game, "sensor.machine"), "no instant supply");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "in-flight physical supply save");
                Until(game, twin, () => Stock(game, "sensor.machine") == Fix.One, "actual sensor supply");
                Order(c, game, new RemoveRouteCommand { Route = route });
                Until(game, null, () => {
                    var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                    return jobs.All(j => j.State == JobState.Done || j.State == JobState.Cancelled);
                }, "physical carrier dock departure");
                Order(c, game, new SetWorkerHeldCommand { Worker = Entity(game, "machine"), Held = true });
                Control(c, game, false); Order(c, game, new SetWorkerHeldCommand { Worker = Entity(game, "sensor.machine"), Held = false });
                Travel(c, game, "destination"); twin = factory.Load(game.Save());
                Until(game, twin, () => Position(game) == Fix.FromInt(10), "ordinary work moves sensor into region");
                c.Equal(ExplorationCodes.NoSensor, Entry(game, "a.reset").Blocker, "physical arrival still requires active actual sensor");
                c.That(!Known(game, "reset") && Entry(game, "b.retain").Progress.Equals(WorkProgress.Zero), "disabled arrival is not a survey or discovery");
                Control(c, game, true); Until(game, null, () => Entry(game, "a.reset").Progress.WholeTicks >= 3, "earned partial dwell");
                var partial = Entry(game, "b.retain");
                c.That(partial.Progress.WholeTicks > 0 && partial.Progress.WholeTicks < 15 && !partial.Known, "unfinished observation only");
                c.Equal(new Code("binding.exploring"), game.Seat(0).Snapshot.Entities.Single(e => e.Def == "sensor.machine").State, "Game.Update settles authored sampling state");
                Control(c, game, false); long savedRetain = Entry(game, "b.retain").Progress.WholeTicks;
                c.Equal(0L, Entry(game, "a.reset").Progress.WholeTicks, "reset interruption clears unfinished dwell");
                c.Equal(partial.Progress.WholeTicks, savedRetain, "retain interruption keeps only unfinished dwell");
                twin = factory.Load(game.Save()); long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "sole paused clock freezes contact"); game.Clock.Resume(); twin.Clock.Resume();
                Step(game, twin); c.Equal(savedRetain, Entry(game, "b.retain").Progress.WholeTicks, "disabled saved retain makes no further progress");
                Travel(c, game, "sensor.contact"); twin = factory.Load(game.Save());
                Until(game, twin, () => Position(game) == Fix.Parse("12.5"), "physical departure");
                c.Equal(ExplorationCodes.Outside, Entry(game, "b.retain").Blocker, "actual outside-region interruption");
                c.Equal(savedRetain, Entry(game, "b.retain").Progress.WholeTicks, "outside preserves authored retain progress");
                Control(c, game, true); Travel(c, game, "destination"); twin = factory.Load(game.Save());
                Until(game, twin, () => Position(game) == Fix.FromInt(10), "physical return");
                Until(game, twin, () => Entry(game, "a.reset").Progress.WholeTicks == 11, "last reset discovery interval");
                c.That(!Known(game, "reset"), "no early reset discovery");
                twin = factory.Load(game.Save()); Until(game, twin, () => Known(game, "reset") && Known(game, "retain"), "both physical discoveries");
                c.That(Entry(game, "a.reset").Progress.Equals(WorkProgress.Zero) && Entry(game, "b.retain").Progress.Equals(WorkProgress.Zero), "knowledge ledger owns completed membership");
                c.That(!Known(game, "reset", 1), "earned company knowledge stays private");
                c.That(!game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), Provider, "sensor.reading", out _), "regional contact does not fabricate geological readings");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "complete discovery save");
                Order(c, game, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "binding.share" });
                c.That(Known(game, "reset", 1) && Known(game, "retain", 1), "ordinary sharing uses same earned ledger");
                Order(c, game, new ForgetKnowledgeCommand { Policy = "binding.forget" });
                c.That(!Known(game, "reset") && Known(game, "reset", 1), "ordinary forgetting affects own company only");
                twin = factory.Load(game.Save()); Until(game, twin, () => Known(game, "reset") && Known(game, "retain"), "physical rediscovery after forgetting");
                c.Equal(Fix.One, Stock(game, "sensor.machine"), "dwell does not invent or duplicate sensor supplies");
                c.Equal(Fix.FromInt(19), Stock(game, "source"), "real single delivered unit balance");
                c.Equal(0L, retained.Entries[0].Progress.WholeTicks, "old facts remain detached");
                c.Equal(isolated, idle.Hash(), "interleaved session discovery and supply isolated");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary contact/control/share/forget stream replays");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-entries", "empty-entries", "duplicate-entry", "unknown-region", "unknown-subject", "unknown-state", "missing-state", "unknown-key", "zero-duration", "interruption", "missing-profile", "missing-sensor", "refreshable-subject", "unused-null-entries", "selected-null-entries" })
            {
                var module = new ExplorationBindingsModule();
                c.Throws<ContentException>(() => New(Factory(module, edit: doc => {
                    var profile = doc.Get("independent_contact_profiles")[0]; var entry = profile.Get("entries")[0];
                    if (mode == "missing-entries") profile.Remove("entries");
                    if (mode == "empty-entries") profile.Set("entries", JsonValue.NewArray());
                    if (mode == "duplicate-entry") profile.Get("entries").Add(entry.Clone());
                    if (mode == "unknown-region") entry.Set("region", JsonValue.Of("missing"));
                    if (mode == "unknown-subject") entry.Set("subject", JsonValue.Of("missing"));
                    if (mode == "unknown-state") profile.Get("states").Set("sampling", JsonValue.Of("missing"));
                    if (mode == "missing-state") profile.Remove("states");
                    if (mode == "unknown-key") profile.Set("fallback", JsonValue.Of(true));
                    if (mode == "zero-duration") entry.Get("duration").Set("numerator", JsonValue.Number("0"));
                    if (mode == "interruption") entry.Set("interruption", JsonValue.Of("default"));
                    if (mode == "unused-null-entries") doc.Set("invalid_contact_profiles", Json.Parse("[{\"id\":\"unused.bad-profile\",\"states\":{\"sampling\":\"binding.sampling\"}}]"));
                    if (mode == "selected-null-entries") {
                        doc.Remove("independent_contact_profiles");
                        doc.Set("invalid_contact_profiles", Json.Parse("[{\"id\":\"binding.profile\",\"states\":{\"sampling\":\"binding.sampling\"}}]")); }
                    if (mode == "refreshable-subject") {
                        doc.Set("point_readings", Json.Parse("[{\"id\":\"binding.refreshable\",\"layer\":\"point.terrain\",\"position\":10,\"retention\":\"latest\",\"fresh_for\":null}]"));
                        entry.Set("subject", JsonValue.Of("binding.refreshable")); }
                }, sensorEdit: doc => {
                    var binding = doc.Get("entities")[0].Get("components").Get("independent_contact");
                    if (mode == "missing-profile") binding.Set("profile", JsonValue.Of("missing"));
                    if (mode == "missing-sensor") binding.Set("sensor", JsonValue.Of("missing"));
                })), "invalid registered contact authoring " + mode);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "refusal before Initialize " + mode);
            }
        }
        private static void ValidationFaults(Check c)
        {
            foreach (string fault in new[] { "pause", "throw" })
            {
                var module = new ExplorationBindingsModule();
                c.Throws<InvalidOperationException>(() => New(Factory(module, sensorEdit: doc => {
                    var parts = doc.Get("entities")[0].Get("components"); var binding = parts.Get("independent_contact");
                    binding.Set("fault", JsonValue.Of(fault)); parts.Remove("independent_contact"); parts.Set("invalid_contact", binding);
                })), "actual binding validation callback refuses " + fault);
                c.Equal(1, module.InvalidCalls, "real registered callback reached " + fault);
                c.That(module.MutationReached == (fault == "pause"), "actual clock mutation reached only in pause negative provider");
                c.Equal(0, module.Initialize.Initializations, "faulting validation cannot initialize world");
            }
        }
    }
}
