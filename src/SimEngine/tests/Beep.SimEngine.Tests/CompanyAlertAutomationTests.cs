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
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class CompanyAlertAutomationTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Leak = new Code("pressure.leak");
        private const string RepeatedCase = "Company alert continuation: repeated emergencies physically resupply and replay native policy decisions";
        public static void Register()
        {
            TestRunner.Add("Automation: typed company alerts, physical incident response and private saved decisions", Run);
            TestRunner.Add(RepeatedCase, c => Physical(c, true));
        }
        internal static GameFactory Factory(string json = null, string profiles = null, bool alerts = true, string response = null, string recovery = null,
            IReadOnlyDictionary<string, string> additionalWork = null)
        {
            var modules = new List<GameModule> { new GameConditionProofModule(), new InventorySignalsModule(), new AutomationModule(), new CompanyAlertModule(), new CompanyAlertAutomationProofModule() };
            var declarations = Content.Json.Parse("[{\"id\":\"sim.conditions\",\"version\":\"1.0.0\",\"documents\":[\"objective-conditions.json\"]},{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"company-alert-automation.json\"]},{\"id\":\"sim.company-alerts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"company-alert-automation-profiles.json\"]},{\"id\":\"proof.company-alert-actions\",\"version\":\"1.0.0\",\"documents\":[\"company-alert-automation-proof.json\"]}]");
            var documents = new Dictionary<string, string> {
                    ["objective-conditions.json"] = FrameworkProofContent.Read("objective-conditions.json"),
                    ["company-alert-automation.json"] = json ?? FrameworkProofContent.Read("company-alert-automation.json"),
                    ["company-alert-automation-profiles.json"] = profiles ?? FrameworkProofContent.Read("company-alert-automation-profiles.json"),
                    ["company-alert-automation-proof.json"] = FrameworkProofContent.Read("company-alert-automation-proof.json") };
            if (recovery != null)
            {
                modules.Add(new LogisticsRecoveryPolicyModule());
                declarations.Add(Content.Json.Parse("{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}"));
                documents.Add("logistics-recovery.json", recovery);
            }
            var workDocuments = new Dictionary<string, string> { ["objective-response.json"] = response ?? FrameworkProofContent.Read("objective-response.json") };
            if (additionalWork != null) foreach (var document in additionalWork) workDocuments.Add(document.Key, document.Value);
            return ConstructionPlanTests.Points(extraModules: modules, extraDeclarations: declarations.ToString(), extraDocuments: documents,
                additionalDocuments: workDocuments,
                scenarioEdit: scenario => {
                    foreach (string module in new[] { GameConditionsModule.ModuleId, InventorySignalsModule.ModuleId, AutomationModule.ModuleId, IncidentResponseModule.ModuleId, "proof.company-alert-actions" })
                        scenario.Get("modules").Add(JsonValue.Of(module));
                    if (alerts) scenario.Get("modules").Add(JsonValue.Of(CompanyAlertModule.ModuleId));
                    if (recovery != null)
                    {
                        scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                        scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Content.Json.Parse("{\"profile\":\"recovery.high\"}"));
                    }
                    scenario.Get("settings").Set(CompanyAlertModule.ModuleId, Content.Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"alerts.response\",\"level\":\"normal\",\"worker_reserves\":[]},{\"company\":1,\"profile\":\"alerts.other\",\"level\":\"normal\",\"worker_reserves\":[]}]}"));
                }, startupProfiles: new[] { "objective-conditions.start" });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static OperationsView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<OperationsView>();
        private static AutomationSettings Settings(SimGame game, string target = "field.plant")
        {
            if (!View(game).TryGetAutomationProfile("automation.incident-alert", out var profile))
                throw new InvalidOperationException("missing disclosed authored alert profile");
            return new AutomationSettings(profile.Id, true, ControlBinding.ForCompany(CompanyAlertControlActionDeclaration.Capability),
                new Dictionary<string, ControlBinding> { ["incident"] = new ControlBinding(Find(game, target).Id, new Code("incident-state")) },
                new Dictionary<string, ControlThresholdBand>(), profile.WhenTrue, profile.WhenFalse);
        }
        private static AutomationRuleSnapshot Rule(SimGame game)
        { if (!View(game).TryGetAutomationRule(Company, "plant.alert", out var rule)) throw new InvalidOperationException("missing private rule"); return rule; }
        private static CompanyAlertSnapshot Alert(SimGame game, int seat = 0)
        { var refusal = game.Seat(seat).View.Feature<CompanyAlertView>().TryReadAlert(new PlayerId(seat), out var snapshot); if (!refusal.IsNone) throw new InvalidOperationException("missing alert: " + refusal); return snapshot; }
        private static bool Active(SimGame game)
        { game.Seat(0).View.Feature<IncidentsView>().TryGetIncident(Find(game, "field.plant").Id, Leak, out var snapshot); return snapshot.Active; }
        private static Order Response(SimGame game) => Board(game).ActiveResponseOn(Find(game, "field.plant").Id, Leak);
        private static Fix Stock(SimGame game) => Find(game, "field.plant").Get<IInventory>().Amount(Supply(game));
        private static RaiseIncidentCommand Raise(SimGame game) => new RaiseIncidentCommand { Target = Find(game, "field.plant").Id, Source = Leak, Kind = Leak };
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 400 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " fault=" + game.Fault + " alert=" + Alert(game).Settings.Level +
                " rule=" + Rule(game).Blocker + "/" + Rule(game).PendingDecision + "/" + Rule(game).PendingAt +
                " orders=" + string.Join(";", Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static void Run(Check c)
        { c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "typed target semantics"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "kind explicitly serialized without old reader"); Physical(c); Refusals(c); InvalidContent(c); }
        private static void Physical(Check c, bool repeated = false)
        {
            var response = Content.Json.Parse(FrameworkProofContent.Read("objective-response.json"));
            if (repeated) response.Get("activities")[0].Set("automatic_supply", JsonValue.Of(true));
            GameFactory factory = Factory(response: response.ToString()); SimGame game = New(factory); var settings = Settings(game);
            SimGame idle = New(factory); ulong untouched = idle.Hash();
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000); using var recording = game.RecordReplay(limits);
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IPlayerControllers>(), "alerts/automation/physical response need no business or AI");
            c.That(View(game).TryGetAutomationProfile(settings.Profile, out var profile) && profile.TargetKind == ControlTargetKind.Company && profile.ActionCommandOwner == CompanyAlertModule.ModuleId, "detached metadata declares actual company command owner");
            c.That(settings.Target.Entity == EntityId.None && settings.Target.Kind == ControlTargetKind.Company, "company target has no fabricated entity");
            c.Equal(StateCodecTests.ExpectedAutomationSchema, ((GameSystem)game.World.Services.Get<IAutomationRules>()).StateSchemaVersion, "same sole rule owner saves typed kind");
            var ids = new List<Code>(); ulong hash = game.Hash();
            c.That(View(game).CopyCompanyControlTargetIds(Company, settings.Profile, ids) && ids.SequenceEqual(new[] { CompanyAlertControlActionDeclaration.Capability }), "company target discovered through provider contract");
            c.That(!View(game).CopyControlTargetIds(Find(game, "field.plant").Id, settings.Profile, ids) && ids.Count == 0, "company policy cannot pretend to be entity control");
            c.That(!View(game, 1).CopyCompanyControlTargetIds(Company, settings.Profile, ids) && ids.Count == 0, "foreign company target discovery refuses");
            c.That(game.Observer.Feature<OperationsView>().CopyCompanyControlTargetIds(Company, settings.Profile, ids), "exact observer company discovery explicit");
            foreach (int invalid in new[] { -2, 8 }) {
                var view = new OperationsView(game.World, new PlayerId(invalid));
                c.That(!view.CopyCompanyControlTargetIds(Company, settings.Profile, ids) && ids.Count == 0, "invalid viewer not company observer");
            }
            c.Equal(Code.None, View(game).PreviewAutomation("plant.alert", settings), "both native alert branches preflight through ordinary handler");
            c.Equal(hash, game.Hash(), "target/profile/preview changes no rule, alert, clock or resources");
            c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "plant.alert", Settings = settings }).Accepted, "player configures same native rule owner");
            Reach(game, () => Rule(game).HasApplied && !Rule(game).Latch, "initial actual normal decision");
            CompanyAlertSnapshot initial = Alert(game); c.Equal(0, initial.Version, "matching normal action does not manufacture policy revision");
            int rounds = repeated ? 3 : 1;
            for (int round = 1; round <= rounds; round++)
            {
                Fix opening = Stock(game), expected = Fix.FromInt(round == 3 ? 0 : 20 - 8 * round);
                c.That(TestKit.Order(game, 0, Raise(game)).Accepted, "player raises actual native incident");
                Reach(game, () => Rule(game).PendingDecision == true, "incident earns declared dwell before next-update emergency");
                AutomationRuleSnapshot pending = Rule(game);
                c.That(Alert(game).Settings.Level == CompanyAlertLevel.Normal && Active(game) && Stock(game) == opening, "pending alert cannot teleport repair or consume materials");
                SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending company action saves exactly");
                long pendingAt = game.Tick; ulong paused = game.Hash(); game.Clock.Pause(); twin.Clock.Pause();
                Tick(game); Tick(twin); c.That(game.Tick == pendingAt && game.Hash() == paused && twin.Hash() == paused,
                    "paused sole clock preserves actual queued company decision without applying it");
                game.Clock.Resume(); twin.Clock.Resume();
                Tick(game); Tick(twin);
                c.That(Alert(game).Settings.Level == CompanyAlertLevel.Emergency && Alert(game).Version == round * 2 - 1 && Rule(game).Latch, "next Update alone applies normal company command");
                c.That(pending.PendingDecision == true && pending.Settings.Target.Kind == ControlTargetKind.Company, "retained facts immutable");
                c.That(Alert(game).Settings.WorkerReserves.Count == 1 && Alert(game).Settings.WorkerReserves[0].MinimumReady == 1, "explicit emergency preparedness policy applied by sole owner");
                var offers = Queue(game);
                if (round == 1) c.That(offers.Any(o => o.Emergency && o.EffectivePriority == o.Priority + 500L), "real dispatch offers use authored emergency priority adjustment");
                else
                {
                    // The first round enables the worker. Later emergency work may already be
                    // acquired before phase290 applies the alert; acquired work is not a queue offer.
                    var pendingWork = offers.Where(offer => offer.Scope == WorkModule.ModuleId && offer.Emergency).ToArray();
                    var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                    var target = Find(game, "field.plant").Id;
                    c.That(pendingWork.All(offer => offer.EffectivePriority == offer.Priority + 500L)
                        && (pendingWork.Length > 0 || jobs.Any(job => job.State == JobState.Assigned && job.Emergency
                            && job.Worker == Find(game, "machine").Id && (job.Target == target || job.Destination == target))),
                        "later emergency is either correctly boosted queued work or an actual already-acquired emergency job");
                }
                c.That(Alert(game, 1).Version == 0 && Alert(game, 1).Settings.Level == CompanyAlertLevel.Normal, "other company's policy untouched");
                c.Equal(game.Hash(), twin.Hash(), "applied company decision twin agrees");
                Configure(c, game); twin = factory.Load(game.Save());
                if (repeated && round == 3)
                {
                    c.Equal(Fix.FromInt(4), opening, "third actual response starts with an insufficient whole eight-material bill");
                    bool LoadedSupply() {
                        var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                        return jobs.Any(job => job.IsHaul && job.Loaded == Fix.FromInt(4) && job.Source == Find(game, "source").Id
                            && job.Destination == Find(game, "field.plant").Id);
                    }
                    Reach(game, LoadedSupply, "engine emergency logistics physically picks up the missing four units", twin);
                    c.That(Alert(game).Settings.Level == CompanyAlertLevel.Emergency && Active(game) && Stock(game) == opening,
                        "preparedness reserve does not fabricate repair material while real supply travels");
                    c.Equal(Fix.FromInt(16), Find(game, "source").Get<IInventory>().Amount(Supply(game)),
                        "the original finite source actually loses only the hauled deficit");
                    twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual loaded emergency supply saves through original owners");
                }
                Reach(game, () => Response(game)?.Node("repair").Progress.WholeTicks > 0, "reserved machine performs emergency physical repair", twin);
                c.That(Active(game) && Stock(game) == expected && Find(game, "machine").Get<PointPresence>().Position == Fix.FromInt(20), "real travel and timed inspect work consume eight materials despite ordinary reserve");
                twin = factory.Load(game.Save()); Reach(game, () => !Active(game), "actual response resolves incident", twin);
                c.Equal(CompanyAlertLevel.Emergency, Alert(game).Settings.Level, "rule does not reset policy in incident-completion pass");
                Reach(game, () => Rule(game).PendingDecision == false, "healthy signal earns restoration dwell", twin);
                c.Equal(CompanyAlertLevel.Emergency, Alert(game).Settings.Level, "queued restoration remains deferred");
                twin = factory.Load(game.Save()); Tick(game); Tick(twin);
                c.That(Alert(game).Settings.Level == CompanyAlertLevel.Normal && Alert(game).Version == round * 2 && Alert(game).Settings.WorkerReserves.Count == 0, "native false branch restores explicit normal policy");
                for (int i = 0; i < 8; i++) { Tick(game); Tick(twin); }
                c.That(Alert(game).Version == round * 2 && Rule(game).PendingDecision == null && Stock(game) == expected, "unchanged condition cannot repeat alert commits or material effects");
                c.Equal(game.Hash(), twin.Hash(), "partial/pending restoration and settled twins agree");
                c.Equal(untouched, idle.Hash(), "each emergency leaves the independent session's actual policy, stock and clock untouched");
            }
            c.Equal(1 + 2 * rounds, recording.CommandCount, "only actual configure, incident and worker player intents are recorded; automatic company commands are recomputed");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount,
                "recording spans every advancing physical response and deferred alert update");
            Fix sourceStock = Find(game, "source").Get<IInventory>().Amount(Supply(game));
            Fix cargoStock = Find(game, "machine").Get<IInventory>().Amount(Supply(game));
            c.Equal(Fix.FromInt(40 - 8 * rounds), sourceStock + Stock(game) + cargoStock,
                "actual source, target and cargo retain only the finite opening forty minus authored repair consumption");
            c.Equal(Fix.Zero, cargoStock, "complete emergency repair leaves no fabricated or stranded material cargo");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "complete initial-session emergency policy, physical work and resupply replay to final live hash");
            Console.WriteLine("company alert replay: rounds=" + rounds + ";initial=" + recording.InitialStep + ";final=" + game.Tick
                + ";commands=" + recording.CommandCount + ";checkpoints=" + recording.CheckpointCount
                + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
            SimGame isolated = New(factory); c.That(Alert(isolated).Version == 0 && Stock(isolated) == Fix.FromInt(20) && !Active(isolated), "new interleaved session stays isolated");
        }
        private static void Refusals(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); var settings = Settings(game);
            c.Equal(Codes.NotOwned, View(game).PreviewAutomation("foreign", Settings(game, "field.foreign-plant")), "company action cannot borrow foreign entity signal");
            var wrongKind = new AutomationSettings(settings.Profile, true, new ControlBinding(Find(game, "field.plant").Id, settings.Target.Capability), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
            c.Equal(AutomationCodes.InvalidSettings, View(game).PreviewAutomation("entity", wrongKind), "wrong target kind refuses before provider interpretation");
            var wrongCapability = new AutomationSettings(settings.Profile, true, ControlBinding.ForCompany(new Code("company.missing")), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
            c.Equal(AutomationCodes.InvalidSettings, View(game).PreviewAutomation("capability", wrongCapability), "company target cannot silently substitute policy");
            c.Throws<ArgumentException>(() => new AutomationSettings(settings.Profile, true, settings.Target, new Dictionary<string, ControlBinding> { ["incident"] = settings.Target }, settings.Thresholds, settings.WhenTrue, settings.WhenFalse), "signal input cannot be a company proxy");
            var invalidAction = new AutomationSettings(settings.Profile, true, settings.Target, settings.Inputs, settings.Thresholds, new ControlAction(true, Fix.One, new Code("unit.boolean")), settings.WhenFalse);
            c.Equal(Codes.InvalidArgument, View(game).PreviewAutomation("scalar", invalidAction), "company alert action does not invent scalar level interpretation");
            c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "plant.alert", Settings = settings }).Accepted, "actual company target owner installed");
            c.Equal(AutomationCodes.Conflict, View(game).PreviewAutomation("competing", settings), "same company capability has one existing rule owner");
            var privateRows = new List<AutomationRuleSnapshot>(); View(game, 1).CopyAutomationRules(privateRows); c.Equal(0, privateRows.Count, "rule state company private");
            foreach (int invalid in new[] { -2, 8 }) {
                var view = new OperationsView(game.World, new PlayerId(invalid)); view.CopyAutomationRules(privateRows); c.Equal(0, privateRows.Count, "invalid rule observer denied");
                c.That(!view.TryGetAutomationRule(Company, "plant.alert", out _), "invalid viewer cannot read exact private rule");
                c.Equal(Codes.NotSeated, new CompanyAlertView(game.World, new PlayerId(invalid)).TryReadAlert(Company, out _), "invalid company policy observer denied");
            }
            c.That(TestKit.Order(game, 0, Raise(game)).Accepted, "cancellation proof raises real incident");
            Reach(game, () => Rule(game).PendingDecision == true, "cancel pending native action");
            c.That(TestKit.Order(game, 0, new RemoveAutomationCommand { Rule = "plant.alert" }).Accepted, "ordinary removal cancels actual deferred policy decision");
            for (int i = 0; i < 5; i++) Tick(game);
            c.That(Alert(game).Settings.Level == CompanyAlertLevel.Normal && Active(game) && Stock(game) == Fix.FromInt(20), "removed rule never applies queued emergency or free repair");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "removed company rule/native incident checkpoint");
            JsonValue bad = Content.Json.Parse(FrameworkProofContent.Read("company-alert-automation.json")); bad.Get("control_rules")[0].Set("action_policy", JsonValue.Of("proof.redirected-alert"));
            SimGame redirected = New(Factory(bad.ToString())); var operations = View(redirected); var invalidSettings = Settings(redirected);
            var cached = new[] { redirected.Seat(0).View.Feature<CompanyAlertView>(), redirected.Seat(1).View.Feature<CompanyAlertView>(),
                redirected.Observer.Feature<CompanyAlertView>(), new CompanyAlertView(redirected.World, new PlayerId(-2)) };
            var alerts = redirected.World.Services.Get<ICompanyAlertPolicy>();
            var prior = alerts.ReadAlert(Company); long at = redirected.Tick;
            c.Equal(Code.None, cached[0].TryReadReserve(Company, new WorkerRoleId("inspect"), out var priorReserve), "actual reserve facts exist before negative provider probe");
            c.Equal(Code.None, cached[0].TryReadReserveCoverage(Company, out var priorCoverage), "actual coverage exists before negative provider probe");
            c.Throws<InvalidOperationException>(() => operations.PreviewAutomation("redirected", invalidSettings), "public command provider cannot redirect company beneficiary");
            var first = redirected.Fault;
            c.That(first != null && first.Phase == "observation.provider", "malformed action provider retains the original terminal contract fault");
            // Explicit read-only framework diagnostics after fault, never healthy host views or rollback.
            c.That(alerts.ReadAlert(Company).Version == 0 && alerts.ReadAlert(new PlayerId(1)).Version == 0,
                "rejected command applied no actual company policy mutation");
            foreach (var view in cached)
            foreach (var company in new[] { Company, new PlayerId(99) })
            {
                CompanyAlertSnapshot alert = prior; CompanyWorkerReserveSnapshot reserve = priorReserve;
                CompanyWorkerReserveCoverage coverage = priorCoverage; var list = new List<CompanyAlertSnapshot> { prior };
                c.Throws<InvalidOperationException>(() => view.TryReadAlert(company, out alert), "cached alert read rejects before private, invalid or observer shortcuts");
                c.That(alert.Settings == null, "faulted alert read clears retained facts");
                c.Throws<InvalidOperationException>(() => view.PreviewAlert(company, prior.Settings), "cached alert preview rejects before refusal shortcuts");
                c.Throws<InvalidOperationException>(() => view.TryReadReserve(company, new WorkerRoleId("inspect"), out reserve), "cached reserve read rejects before ordinary refusal");
                c.That(reserve.Equals(default(CompanyWorkerReserveSnapshot)), "faulted reserve read clears retained facts");
                c.Throws<InvalidOperationException>(() => view.TryReadReserveCoverage(company, out coverage), "cached coverage read rejects before ordinary refusal");
                c.That(coverage == null, "faulted coverage read clears retained facts");
                c.Throws<InvalidOperationException>(() => view.CopyAlerts(list), "cached alert list rejects before empty or private shortcut");
                c.Equal(0, list.Count, "faulted alert list clears retained facts");
            }
            c.Throws<InvalidOperationException>(() => redirected.Save(), "invalid command provider cannot checkpoint as healthy");
            c.Throws<InvalidOperationException>(() => redirected.Hash(), "invalid command provider cannot publish healthy hash");
            c.Equal(0, redirected.Update(), "invalid command provider cannot advance another step");
            c.That(redirected.Tick == at && ReferenceEquals(first, redirected.Fault), "cached reads preserve the original fault and sole clock coordinate");
        }
        private static void InvalidContent(Check c)
        {
            c.Throws<ContentException>(() => New(Factory(alerts: false)), "optional alert module required only when declared action selected");
            foreach (string fault in new[] { "owner", "command", "profile", "settings", "level", "reserves", "role" })
                c.Throws<ContentException>(() => {
                    JsonValue json = Content.Json.Parse(FrameworkProofContent.Read("company-alert-automation.json")); JsonValue action = json.Get("company_alert_control_actions")[0];
                    if (fault == "owner") action.Set("owner", JsonValue.Of(OperationsModule.ModuleId));
                    if (fault == "command") action.Set("command", JsonValue.Of("sim.configure-operation"));
                    if (fault == "profile") action.Get("enabled").Set("profile", JsonValue.Of("missing"));
                    if (fault == "settings") action.Remove("disabled");
                    if (fault == "level") action.Get("enabled").Set("level", JsonValue.Of("default"));
                    if (fault == "reserves") action.Get("enabled").Remove("worker_reserves");
                    if (fault == "role") action.Get("enabled").Get("worker_reserves")[0].Set("role", JsonValue.Of("missing"));
                    New(Factory(json.ToString()));
                }, "bad authored company action refuses before Update: " + fault);
        }
    }
}
