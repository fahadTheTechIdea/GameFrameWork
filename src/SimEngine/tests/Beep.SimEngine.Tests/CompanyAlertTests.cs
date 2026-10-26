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
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class CompanyAlertTests
    {
        public static void Register()
        {
            TestRunner.Add("Company alerts: strict profiles, private commands and derived dispatch priority", Admission);
            TestRunner.Add("Company alerts: physical policy changes, saved cargo and isolated sessions", Physical);
        }
        private const string Declaration = "{\"id\":\"proof.shipments\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"},{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[\"shipments.json\"]}";
        private static GameFactory Factory(string profiles = null, string profile = "alerts.standard", bool shipment = false, Action<JsonValue> edit = null)
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions), scenario = root.Get("scenarios")[0];
            scenario.Set("seats", JsonValue.Number("2")); scenario.Get("modules").Add(JsonValue.Of(CompanyAlertModule.ModuleId));
            scenario.Get("settings").Set("sim.company-alerts", Content.Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"" + profile + "\",\"level\":\"normal\",\"worker_reserves\":[]},{\"company\":1,\"profile\":\"alerts.neutral\",\"level\":\"normal\",\"worker_reserves\":[]}]}"));
            root.Get("entities")[2].Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"shared.machine\"}"));
            root.Get("entity_states").Add(Content.Json.Parse("{\"id\":\"stopped\",\"state\":\"point.incident\",\"priority\":70}"));
            root.Get("entities")[1].Get("components").Set("contract_incident", Content.Json.Parse("{\"id\":\"damage\",\"kind\":\"proof.failure\",\"response\":\"shared.response\"}"));
            edit?.Invoke(root); var text = new StringBuilder(); root.WriteCanonical(text);
            JsonValue document = Content.Json.Parse(FrameworkProofContent.Read("dispatch-source.json"));
            if (shipment) document.Get("proof_shipments")[0].Set("enabled", JsonValue.Of(true));
            document.Set("proof_alert_profiles", Content.Json.Parse("[{\"id\":\"alerts.shipment\",\"normal\":0,\"urgent\":300,\"emergency\":-200}]"));
            var source = new StringBuilder(); document.WriteCanonical(source);
            return PointGame.Factory(text.ToString(), FrameworkProofContent.Read("shared-dispatch.json"), new DispatchProofModule(), Declaration,
                source.ToString(), profiles ?? FrameworkProofContent.Read("company-alerts.json"));
        }
        private static SimGame New(GameFactory factory, int seats = 1)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 409 }; request.Mods.Add("proof.shipments");
            for (int i = 0; i < seats; i++) request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing alert entity " + id); }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static ICompanyAlertPolicy Policy(SimGame game) => game.World.Services.Get<ICompanyAlertPolicy>();
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static Order Post(SimGame game, int priority = 100)
            => Board(game).Post(game.World.Content.Get<ActivityDef>("shared.work"), new PlayerId(0), Find(game, "source").Id, priority);
        private static void Enable(SimGame game, bool emergency = true)
            => Find(game, "machine").Get<IWorkPolicy>().Configure(game.World, new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, emergency, WorkAssignmentMode.All));
        private static List<WorkerDispatchOffer> Queue(SimGame game)
        { var offers = new List<WorkerDispatchOffer>(); game.Seat(0).View.Feature<WorkView>().CopyAssignments(offers); return offers; }
        private static void Stock(Check c, SimGame game, int priority = 50)
            => c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(8), Fix.FromInt(40), priority, LogisticsReserveAccess.Protected)) }).Accepted, "stock intent is a normal engine command");
        private static void Alert(Check c, SimGame game, CompanyAlertLevel level, string profile = "alerts.standard")
        {
            var settings = new CompanyAlertSettings(profile, level, Array.Empty<WorkerReserveSetting>()); var cmd = new ConfigureCompanyAlertCommand { Company = new PlayerId(0), Settings = settings };
            c.Equal(Code.None, game.Seat(0).View.Feature<CompanyAlertView>().PreviewAlert(cmd.Company, settings), "private management preview agrees");
            c.Equal(Code.None, game.Seat(0).Preview(cmd), "command preview agrees");
            c.That(TestKit.Order(game, 0, cmd).Accepted, "alert edit executes through authoritative command boundary");
        }
        private static Job Assigned(SimGame game) => Board(game).Job(Find(game, "machine").Get<IWorker>().Job);
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, SimGame isolated = null)
        {
            for (int i = 0; i < 360 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); if (isolated != null) Tick(isolated); }
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Blocker + "/loaded=" + job.Loaded);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join(";", facts));
            }
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory, 2); Post(game); Stock(c, game);
            CompanyAlertView view = game.Seat(0).View.Feature<CompanyAlertView>();
            c.Equal(Code.None, view.TryReadAlert(new PlayerId(0), out CompanyAlertSnapshot retained), "own detached alert available");
            c.Equal(CompanyAlertLevel.Normal, retained.Settings.Level, "initial level comes from scenario JSON");
            c.Equal(0, retained.Version, "initial policy has no fabricated edit");
            var snapshots = new List<CompanyAlertSnapshot>(); view.CopyAlerts(snapshots); c.Equal(1, snapshots.Count, "own view hides other company's settings");
            c.Equal(Codes.NotOwned, view.TryReadAlert(new PlayerId(1), out var hidden), "foreign read refuses");
            c.That(hidden.Settings == null, "private refusal leaks no settings handle");
            PlayerView.Create(game.World, PlayerId.None).Feature<CompanyAlertView>().CopyAlerts(snapshots); c.Equal(2, snapshots.Count, "observer reads both detached policies");
            ulong before = game.Hash();
            var invalid = new ConfigureCompanyAlertCommand { Company = new PlayerId(0), Settings = new CompanyAlertSettings("missing", CompanyAlertLevel.Normal, Array.Empty<WorkerReserveSetting>()) };
            c.Equal(CompanyAlertCodes.InvalidSettings, game.Seat(0).Preview(invalid), "missing profile has typed refusal");
            c.Equal(CompanyAlertCodes.InvalidSettings, view.PreviewAlert(invalid.Company, invalid.Settings), "management shares missing-profile refusal");
            c.Equal(CompanyAlertCodes.InvalidSettings, view.PreviewAlert(new PlayerId(0), new CompanyAlertSettings("alerts.missing-source", CompanyAlertLevel.Urgent, Array.Empty<WorkerReserveSetting>())), "declared unavailable source refuses rather than matching another");
            c.Equal(CompanyAlertCodes.InvalidSettings, view.PreviewAlert(new PlayerId(0), null), "missing settings are not defaulted");
            c.Equal(CompanyAlertCodes.NotCompany, Policy(game).CheckAlert(new PlayerId(8), retained.Settings), "unknown company refuses");
            c.Equal(before, game.Hash(), "reads and previews are pure");
            c.Equal(CompanyAlertCodes.InvalidSettings, TestKit.Order(game, 0, invalid).Refusal, "actual refusal matches preview");
            var foreign = new ConfigureCompanyAlertCommand { Company = new PlayerId(1), Settings = retained.Settings };
            c.Equal(Codes.NotOwned, game.Seat(0).Preview(foreign), "foreign command preview refuses");
            c.Equal(Codes.NotOwned, TestKit.Order(game, 0, foreign).Refusal, "foreign command execution refuses identically");
            Alert(c, game, CompanyAlertLevel.Urgent); Enable(game); var offers = Queue(game);
            c.Equal(LogisticsModule.ModuleId, offers[0].Scope, "urgent authored adjustment changes actual common ordering");
            c.Equal(50, offers[0].Priority, "raw intent priority stays unchanged");
            c.Equal(250L, offers[0].EffectivePriority, "derived priority includes explicit adjustment");
            c.That(!offers[0].Emergency, "company urgent does not fabricate an incident");
            c.Equal(CompanyAlertLevel.Normal, retained.Settings.Level, "retained snapshot is immutable");
            Tick(game); c.That(Assigned(game).IsHaul, "actual acquisition follows same ranked projection");
            c.Equal(50, Assigned(game).Priority, "acquired native job retains raw player policy");
            int version = Policy(game).ReadAlert(new PlayerId(0)).Version;
            Alert(c, game, CompanyAlertLevel.Urgent); c.Equal(version, Policy(game).ReadAlert(new PlayerId(0)).Version, "identical edit is idempotent");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "current policy and physical job hash restore together");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current native work format includes progressive handling and dock bindings");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current build identifies engine semantics");
            Codec(c); Ranking(c); InvalidContent(c); Extension(c); SavedAdmission(c);
        }
        private static void Codec(Check c)
        {
            var command = new ConfigureCompanyAlertCommand { Company = new PlayerId(1), Settings = new CompanyAlertSettings("alerts.standard", CompanyAlertLevel.Emergency, Array.Empty<WorkerReserveSetting>()) };
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); command.Sync(writer); writer.Flush(); byte[] bytes = stream.ToArray();
                var copy = new ConfigureCompanyAlertCommand(); copy.Sync(new StateReader(bytes, 0, bytes.Length));
                c.Equal(command.Company, copy.Company, "command stream preserves company"); c.Equal(command.Settings.Profile, copy.Settings.Profile, "command stream preserves stable content identity");
                c.Equal(command.Settings.Level, copy.Settings.Level, "command stream preserves explicit level");
            }
            c.Throws<ArgumentException>(() => new CompanyAlertSettings("", CompanyAlertLevel.Normal, Array.Empty<WorkerReserveSetting>()), "empty profile refuses");
            c.Throws<ArgumentException>(() => new CompanyAlertSettings("alerts.standard", (CompanyAlertLevel)4, Array.Empty<WorkerReserveSetting>()), "undefined level refuses");
        }
        private static void Ranking(Check c)
        {
            SimGame game = New(Factory()); Post(game, int.MaxValue); Stock(c, game); Alert(c, game, CompanyAlertLevel.Urgent, "alerts.wide"); Enable(game);
            var offers = Queue(game); c.Equal(6442450941L, offers[0].EffectivePriority, "all-scope and specific adjustments sum in checked long");
            c.Equal(int.MaxValue, offers[0].Priority, "wide ranking never wraps raw maximum priority");
            Policy(game).ConfigureAlert(new PlayerId(0), new CompanyAlertSettings("alerts.wide", CompanyAlertLevel.Emergency, Array.Empty<WorkerReserveSetting>()));
            offers = Queue(game); c.Equal(-1L, offers[0].EffectivePriority, "negative adjustment remains exact, no clamp");
            Policy(game).ConfigureAlert(new PlayerId(0), new CompanyAlertSettings("alerts.standard", CompanyAlertLevel.Emergency, Array.Empty<WorkerReserveSetting>()));
            offers = Queue(game); c.Equal(WorkModule.ModuleId, offers[0].Scope, "emergency company profile can prioritize ordinary work without changing its type");
            c.That(!offers[0].Emergency, "company level does not bypass actual emergency classification");
            long created = offers[0].CreatedTick; ulong before = game.Hash(); Queue(game);
            c.Equal(before, game.Hash(), "derived queue read has no state writes"); c.Equal(created, Queue(game)[0].CreatedTick, "alert never resets job age");
            SimGame optional = PointGame.Factory().NewGame(new NewGameRequest { Scenario = "points", Seed = 19 });
            c.That(!optional.World.Services.TryGet<ICompanyAlertPolicy>(out _), "non-alert scenario needs no fabricated policy service");
        }
        private static void InvalidContent(Check c)
        {
            string profiles = FrameworkProofContent.Read("company-alerts.json");
            c.Throws<ContentException>(() => New(Factory(profiles.Replace("\"normal\":[],", ""))), "all three levels must be explicitly authored");
            c.Throws<ContentException>(() => New(Factory(profile: "missing")), "unknown initial profile refuses before play");
            var invalidSource = Content.Json.Parse(profiles);
            invalidSource.Get("company_alert_profiles").Add(Content.Json.Parse("{\"id\":\"alerts.missing-source\",\"normal\":[],\"urgent\":[{\"scope\":\"proof.missing\",\"emergency\":false,\"priority_adjustment\":100}],\"emergency\":[]}"));
            c.Throws<ContentException>(() => New(Factory(invalidSource.ToString())), "even unused profile cannot refer to undeclared dispatch source");
            c.Throws<ContentException>(() => New(Factory(invalidSource.ToString(), profile: "alerts.missing-source")), "initial profile cannot refer to undeclared dispatch source");
            c.Throws<ContentException>(() => New(Factory(edit: r => r.Get("scenarios")[0].Remove("settings"))), "selected module requires scenario bindings");
            c.Throws<ContentException>(() => New(Factory(edit: r => r.Get("scenarios")[0].Get("settings").Get("sim.company-alerts").Set("companies", Content.Json.Parse("[]")))), "missing active company binding refuses");
            c.Throws<ContentException>(() => New(Factory(edit: r => { var rows = r.Get("scenarios")[0].Get("settings").Get("sim.company-alerts").Get("companies"); rows.Add(rows[0].Clone()); })), "duplicate company binding refuses");
            c.Throws<ContentException>(() => New(Factory(edit: r => r.Get("scenarios")[0].Get("settings").Get("sim.company-alerts").Get("companies")[0].Set("level", JsonValue.Of("panic")))), "undefined authored level refuses");
            JsonValue root = Content.Json.Parse(profiles); var urgent = root.Get("company_alert_profiles")[0].Get("urgent"); urgent.Add(urgent[0].Clone());
            var text = new StringBuilder(); root.WriteCanonical(text); c.Throws<ContentException>(() => New(Factory(text.ToString())), "duplicate rule identity refuses");
        }
        private static void Extension(Check c)
        {
            SimGame game = New(Factory(profile: "alerts.shipment", shipment: true)); Post(game); Tick(game);
            Alert(c, game, CompanyAlertLevel.Urgent, "alerts.shipment"); Enable(game); var offers = Queue(game);
            c.That(game.World.Content.Get<CompanyAlertProfileDef>("alerts.shipment") is ShipmentAlertProfileDef, "open base index resolves independent JSON profile contract");
            c.Equal("proof.shipments", offers[0].Scope, "independent profile selects independent declared source");
            c.Equal(300L, offers[0].EffectivePriority, "extension contributes pure priority without scheduler edits");
            Tick(game); c.That(Assigned(game).IsHaul, "independent policy still acquires native physical hauling");
        }
        private static void SavedAdmission(Check c)
        {
            SimGame game = New(Factory()); Tick(game); var service = (IPersistentService)Policy(game);
            c.Equal(3, service.StateSchemaVersion, "one optional policy owner declares one current codec");
            void Refuse(int count = 1, int company = 0, string profile = "alerts.standard", int level = 1, int version = 0, long changed = 0)
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new StateWriter(stream); writer.Count(count); PlayerId seat = new PlayerId(company); writer.Player(ref seat);
                    writer.String(ref profile); writer.Int(ref level); writer.Count(0); writer.Int(ref version); writer.Long(ref changed); writer.Flush();
                    byte[] bytes = stream.ToArray(); service.Sync(new StateReader(bytes, 0, bytes.Length));
                }
            }
            ulong before = game.Hash();
            c.Throws<CorruptStateException>(() => Refuse(count: 2), "saved seat count must match selected companies");
            c.Throws<CorruptStateException>(() => Refuse(company: 1), "saved company identity must match canonical seat");
            c.Throws<CorruptStateException>(() => Refuse(profile: "missing"), "saved profile must resolve selected content");
            c.Throws<CorruptStateException>(() => Refuse(profile: "alerts.missing-source"), "saved profile retains source declaration admission");
            c.Throws<CorruptStateException>(() => Refuse(level: 4), "saved undefined level refuses");
            c.Throws<CorruptStateException>(() => Refuse(version: -1), "saved negative edit version refuses");
            c.Throws<CorruptStateException>(() => Refuse(changed: 2), "saved future change tick refuses");
            c.Throws<CorruptStateException>(() => Refuse(changed: 1), "unedited policy cannot claim a later change tick");
            c.Equal(before, game.Hash(), "single-company corrupt reads never replace accepted policy");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Order work = Post(game); Stock(c, game); Alert(c, game, CompanyAlertLevel.Urgent); Enable(game); Tick(game);
            Job haul = Assigned(game); c.That(haul.IsHaul && haul.State == JobState.Assigned, "urgent policy acquires normal physical haul");
            Reach(game, () => haul.Loaded.IsPositive, "pickup"); c.Equal(Fix.FromInt(8), haul.Loaded, "real cargo is loaded over update time");
            ReservationToken receipt = haul.InReservation; int id = haul.Id;
            Alert(c, game, CompanyAlertLevel.Emergency); c.Equal(id, Assigned(game).Id, "alert edit never preempts acquired cargo");
            c.Equal(receipt, haul.InReservation, "existing destination receipt survives edit");
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().ReservedIn(Supply(game)), "physical room remains promised exactly once");
            c.Equal(JobState.Open, Board(game).Job(work.Node("work").JobId).State, "reprioritized work cannot steal busy carrier");
            SimGame twin = factory.Load(game.Save()), isolated = New(factory); Order separate = Post(isolated); Stock(c, isolated); Enable(isolated); Tick(isolated);
            c.Equal(separate.Id, Assigned(isolated).OrderId, "normal isolated company chooses work rather than urgent haul");
            c.Equal(CompanyAlertLevel.Normal, Policy(isolated).ReadAlert(new PlayerId(0)).Settings.Level, "different session retains its own authored policy");
            c.Equal(game.Hash(), twin.Hash(), "loaded cargo and changed policy restore together");
            Reach(game, () => haul.State == JobState.Done, "delivery", twin, isolated);
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "arrival physically delivers once");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IOutletPort>().Amount(Supply(game)), "real source loses only delivered amount");
            c.Equal(game.Hash(), twin.Hash(), "interleaved saved continuation remains identical");
            Reach(game, () => Board(game).Job(work.Node("work").JobId).State == JobState.Assigned, "pending work dispatch", twin, isolated);
            Job active = Assigned(game); int workId = active.Id; long created = active.CreatedTick;
            WorkProgress progress = work.Node("work").Progress;
            Alert(c, game, CompanyAlertLevel.Normal); Alert(c, twin, CompanyAlertLevel.Normal);
            c.Equal(workId, Assigned(game).Id, "ordinary acquired work survives lower company alert");
            c.Equal(created, active.CreatedTick, "active work age is not recreated");
            c.That(work.Node("work").Progress.WholeTicks >= progress.WholeTicks, "alert does not reset active elapsed work");
            c.Equal(game.Hash(), twin.Hash(), "same command stream yields same policy and physical state");
            Reach(game, () => active.State == JobState.Done, "work completion", twin, isolated); c.Equal(game.Hash(), twin.Hash(), "work completion is deterministic after alert edits");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled state restores without saved queue");
            game.Seat(0).Submit(new ConfigureCompanyAlertCommand { Company = new PlayerId(0), Settings = new CompanyAlertSettings("alerts.neutral", CompanyAlertLevel.Emergency, Array.Empty<WorkerReserveSetting>()) });
            twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(CompanyAlertLevel.Emergency, Policy(twin).ReadAlert(new PlayerId(0)).Settings.Level, "saved pending command executes its actual level");
            c.Equal("alerts.neutral", Policy(twin).ReadAlert(new PlayerId(0)).Settings.Profile, "saved pending command executes its actual profile");
            c.Equal(game.Hash(), twin.Hash(), "saved command arrival and execution reproduce same authoritative state");
            Emergency(c);
        }
        private static void Emergency(Check c)
        {
            SimGame game = New(Factory()); Post(game, int.MaxValue); Tick(game);
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = Find(game, "destination").Id, Source = new Code("damage"), Kind = new Code("proof.failure") }).Accepted, "actual incident uses registered engine command");
            Alert(c, game, CompanyAlertLevel.Emergency); Enable(game, emergency: false); var offers = Queue(game);
            c.Equal(WorkModule.ModuleId, offers[0].Scope, "actual emergency with no eligible carrier cannot take ready work");
            c.Equal(WorkPolicyCodes.Emergency, offers[1].Blocker, "company alert does not bypass emergency eligibility");
            Enable(game); offers = Queue(game); c.Equal(LogisticsModule.ModuleId, offers[0].Scope, "actual emergency precedes boosted ordinary work");
            c.That(offers[0].Emergency && !offers[1].Emergency, "incident identity stays at original response owner");
            Tick(game); c.That(Assigned(game).IsHaul, "actual incident supply wins same shared engine pass");
        }
    }
}
