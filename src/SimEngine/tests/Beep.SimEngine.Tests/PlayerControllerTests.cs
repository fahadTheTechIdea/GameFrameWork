using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.App;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class PlayerControllerTests
    {
        public static void Register()
        {
            TestRunner.Add("Player controllers: authored bindings, scoped decisions and command parity", Contracts);
            TestRunner.Add("Player controllers: physical playbooks and saved cadence", Physical);
        }
        internal static GameFactory Factory(string profile = "field.playbook", Action<JsonValue> edit = null,
            Action<JsonValue> settingsEdit = null, bool profiles = true, Action<JsonValue> constructionEdit = null)
        {
            JsonValue document = Json.Parse(FrameworkProofContent.Read("player-controllers.json")); edit?.Invoke(document);
            JsonValue templates = Json.Parse(FrameworkProofContent.Read("plan-templates.json"));
            JsonValue entries = templates.Get("plan_templates")[0].Get("entries");
            for (int i = 0; i < entries.Count; i++) entries[i].Set("paused", JsonValue.Of(false));
            return ConstructionPlanTests.Points(edit: constructionEdit, additionalModule: new PlayerControllerProofModule(),
                moduleDeclaration: "{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[\"controllers.json\"]}",
                moduleDocument: document.ToString(), templateDefinitions: templates.ToString(), scenarioEdit: scenario =>
                {
                    scenario.Get("modules").Add(JsonValue.Of(PlayerControllersModule.ModuleId));
                    JsonValue settings = Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"" + profile + "\"}]}");
                    if (!profiles) settings.Set("companies", JsonValue.NewArray());
                    settingsEdit?.Invoke(settings); scenario.Get("settings").Set(PlayerControllersModule.ModuleId, settings);
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static PlayerControllerFacts Facts(SimGame game, int company = 0)
        {
            var facts = new List<PlayerControllerFacts>(); game.Seat(company).View.Feature<PlayerControllersView>().CopyControllers(facts);
            return facts.Count == 0 ? default : facts[0];
        }
        private static List<PlanView> Plans(SimGame game)
        { var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlanGroup("autopilot.field", plans); return plans; }
        private static void Step(SimGame game) { game.RunTicks(1); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
        private static void Switch(Check c, SimGame game, Controller selection)
            => c.That(TestKit.Order(game, 0, new SetControllerCommand { Controller = selection }).Accepted, "player changes ordinary controller selection");
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory("reply.proof"); SimGame game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current authored controller build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one current saved controller layout");
            CoreSessions(c);
            PlaybookCodec(c, game.World.Content.Get<PlanPlaybookDef>("field.playbook"));
            c.That(game.World.Content.Get<PlayerControllerDef>("reply.proof").AuthoringPath.Contains("controllers.json"), "master names separate public provider JSON");
            c.That(Facts(game).Profile == "reply.proof" && Facts(game).Decisions == 0, "human profile is dormant, not an implicit AI");
            c.Equal(0, Facts(game, 1).Profile == null ? 0 : 1, "other company cannot see profile or cadence");
            foreach (var property in typeof(PlayerDecisionContext).GetProperties())
                c.That(property.PropertyType != typeof(World) && property.PropertyType != typeof(SimGame)
                    && property.PropertyType != typeof(PlayerSeat), "decision property " + property.Name + " exposes no game/world/seat");
            c.That(typeof(PlayerDecisionContext).GetProperty("Game") == null && typeof(PlayerDecisionContext).GetProperty("Observer") == null,
                "no observer or lifecycle escape remains in decision context");
            var selection = new SetControllerCommand { Controller = Controller.Autopilot };
            c.Equal(Codes.NotFound, game.Seat(1).Preview(selection), "unbound company autopilot explicitly refuses");
            c.Equal(Codes.NotFound, TestKit.Order(game, 1, selection).Refusal, "same native refusal as management preview");
            c.Equal(0L, Facts(game).Decisions, "human updates do not advance provider memory");
            Switch(c, game, Controller.Autopilot); PlayerControllerFacts started = Facts(game);
            c.That(started.Decisions == 1 && started.AwaitingCommands == 1 && started.LastDecision == game.Tick - 1, "first engine decision queues a normal next-tick command");
            c.Equal(started.LastDecision + 3, started.NextDecision, "rational cadence resolves through the actual minute quantum");
            Step(game); c.That(Facts(game).PendingAnswers == 1 && Facts(game).AwaitingCommands == 0, "real command refusal is durable between cadence ticks");
            SimGame saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "pending real answer survives checkpoint without a presentation feed");
            while (game.Tick <= started.NextDecision) { Step(game); Step(saved); }
            c.That(Facts(game).Blocker == Codes.InvalidArgument && Facts(game).PendingAnswers == 0, "public provider receives exact ordinary refusal once");
            c.Equal(game.Hash(), saved.Hash(), "saved durable answer and provider state continue identically");
            Switch(c, game, Controller.Human); long decisions = Facts(game).Decisions;
            for (int i = 0; i < 5; i++) Step(game);
            c.Equal(decisions, Facts(game).Decisions, "human handover stops authored decisions without resetting state");
            Switch(c, game, Controller.Autopilot); c.Equal(decisions + 1, Facts(game).Decisions, "overdue return performs one decision, not catch-up bursts");
            SimGame independent = New(factory); c.Equal(0L, Facts(independent).Decisions, "shared content does not share live controller state");
            SimGame dormant = New(factory); dormant.Seat(0).Submit(new SetControllerCommand { Controller = Controller.Autopilot }); Step(dormant);
            dormant.Seat(0).Submit(new SetControllerCommand { Controller = Controller.Human }); Step(dormant);
            c.That(Facts(dormant).Decisions == 1 && Facts(dormant).PendingAnswers == 1, "handover retains an actual prior controller's answer, not unrelated human answers");
            SimGame dormantTwin = factory.Load(dormant.Save());
            for (int i = 0; i < 5; i++) { Step(dormant); Step(dormantTwin); }
            c.That(Facts(dormant).PendingAnswers == 1 && Facts(dormant).Decisions == 1, "dormant pending mail survives beyond cadence without synthetic decisions");
            c.Equal(dormant.Hash(), dormantTwin.Hash(), "saved dormant controller stays isolated and identical");
            Switch(c, dormant, Controller.Autopilot); Switch(c, dormantTwin, Controller.Autopilot);
            c.That(Facts(dormant).PendingAnswers == 0 && Facts(dormant).Blocker == Codes.InvalidArgument, "resumed controller consumes its preserved real refusal once");
            c.Equal(dormant.Hash(), dormantTwin.Hash(), "resumed saved provider receives identical command history");
            CorruptCadence(c, factory);
            foreach (string field in new[] { "cadence", "commands_per_decision", "steps" })
                c.Throws<ContentException>(() => New(Factory(edit: json => json.Get("plan_playbooks")[0].Remove(field))), "missing explicit " + field + " refuses before Initialize");
            c.Throws<ContentException>(() => New(Factory(edit: json => json.Get("plan_playbooks")[0].Set("commands_per_decision", JsonValue.Number("0")))), "zero command budget is not fabricated unlimited work");
            c.Throws<ContentException>(() => New(Factory(edit: json => json.Get("plan_playbooks")[0].Get("cadence").Set("numerator", JsonValue.Number("0")))), "zero cadence refuses");
            c.Throws<ContentException>(() => New(Factory("missing")), "unknown selected profile refuses");
            c.Throws<ContentException>(() => New(Factory(edit: json => json.Get("plan_playbooks")[0].Get("steps")[0].Set("template", JsonValue.Of("missing")))), "unknown template reference refuses");
            c.Throws<ContentException>(() => New(Factory(edit: json => json.Get("plan_site_sets")[0].Get("sites")[0].Get("anchor").Set("space", JsonValue.Of("other")))), "wrong-space anchor uses native admission before Initialize");
            c.Throws<ContentException>(() => New(Factory(edit: json => json.Get("plan_site_sets")[0].Get("sites")[0].Get("anchor").Set("position", JsonValue.Number("1000")))), "out-of-space anchor uses native geometry admission");
            c.Throws<ContentException>(() => New(Factory(settingsEdit: settings => settings.Get("companies").Add(settings.Get("companies")[0].Clone()))), "duplicate company bindings refuse");
            GameFactory empty = Factory(profiles: false);
            c.Equal(0L, Facts(New(empty)).Decisions, "explicit empty human bindings create no AI defaults");
            c.Throws<ContentException>(() => empty.NewGame(new NewGameRequest { Scenario = "points", Spectate = true }), "active autopilots cannot select missing default profiles");
            foreach (string mode in new[] { "budget", "expired" })
            {
                SimGame fault = New(Factory("reply.proof", json => json.Get("reply_controllers")[0].Set("mode", JsonValue.Of(mode))));
                fault.Seat(0).Submit(new SetControllerCommand { Controller = Controller.Autopilot });
                for (int i = 0; i < 5 && fault.Fault == null; i++) fault.RunTicks(1);
                c.That(fault.Fault != null && fault.State == GameState.Faulted, mode + " violation faults normal engine Update");
                c.Throws<InvalidOperationException>(() => fault.Save(), "partial controller tick cannot save");
                c.Throws<InvalidOperationException>(() => fault.Hash(), "partial controller tick cannot hash as valid state");
            }
            SimGame scriptGame = New(factory); bool answered = false;
            scriptGame.Seat(0).Attach(new ScriptedController(() => Script(() => answered = true)));
            scriptGame.RunTicks(5); c.That(answered, "thin scripted player uses revised context and real answer without Game access");
            var endingRequest = new NewGameRequest { Scenario = "points", Seed = 719, Horizon = GameDuration.Minutes(2) };
            endingRequest.Seats.Add(Controller.Autopilot); endingRequest.Seats.Add(Controller.Human);
            SimGame ending = factory.NewGame(endingRequest); ending.RunTicks(20);
            c.That(ending.State == GameState.Ended && ending.Tick == 2 && Facts(ending).Decisions == 1, "engine horizon ends autonomous play without hidden extra decisions");
        }
        private static GameFactory CoreFactory(bool controllers, string bindings = null)
        {
            var modules = new List<GameModule> { new ProbeModule("session", new ModuleVersion(1, 0, 0)) };
            if (controllers) modules.Add(new PlayerControllerProofModule());
            var definition = new GameDefinition("controller-core", modules);
            JsonValue profile = Json.Parse(FrameworkProofContent.Read("player-controllers.json")); profile.Remove("plan_playbooks");
            string selection = controllers ? "\"session\",\"sim.player-controllers\"" : "\"session\"";
            string settings = controllers ? "\"sim.player-controllers\":" + (bindings ?? "{\"companies\":[{\"company\":0,\"profile\":\"reply.proof\"}]}") : "";
            string declarations = controllers ? ",{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[\"controllers.json\"]}" : "";
            var files = new PreloadedFileSource().Add("manifest.json", "{\"schema\":1,\"modules\":[{\"id\":\"session\",\"version\":\"1.0.0\",\"documents\":[\"session.json\"]}" + declarations + "]}")
                .Add("session.json", "{" + TestGame.TimeDefinitions + "\"game_modes\":[{\"id\":\"player-controller.resource-only\",\"name\":\"Physical diagnostic game\",\"properties\":{}}],\"scenarios\":[{\"id\":\"session\",\"game_mode\":\"player-controller.resource-only\",\"mode_overrides\":{},\"time\":\"test.minute\",\"seats\":2,\"modules\":[" + selection + "],\"settings\":{" + settings + "}}]}")
                .Add("controllers.json", profile.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame CoreNew(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "session", Seed = 73 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static void CoreSessions(Check c)
        {
            var defaultFactory = CoreFactory(true, "{\"default_profile\":\"reply.proof\",\"companies\":[]}");
            var defaultGame = defaultFactory.NewGame(new NewGameRequest { Scenario = "session", Seats = { Controller.Human, Controller.Human } });
            c.That(Facts(defaultGame, 0).Profile == "reply.proof" && Facts(defaultGame, 1).Profile == "reply.proof",
                "explicit default profile binds both actual session companies");
            var defaultTwin = defaultFactory.Load(defaultGame.Save());
            c.Equal(defaultGame.Hash(), defaultTwin.Hash(), "default binding identities restore without another registry");
            c.Throws<ContentException>(() => CoreFactory(true, "{\"default_profile\":\"missing\",\"companies\":[]}").NewGame(new NewGameRequest { Scenario = "session" }),
                "missing default reference refuses before a session is returned");
            var absentDefault = CoreFactory(true, "{\"default_profile\":null,\"companies\":[{\"company\":0,\"profile\":\"reply.proof\"}]}").NewGame(new NewGameRequest { Scenario = "session", Seats = { Controller.Human, Controller.Human } });
            c.That(Facts(absentDefault, 1).Profile == null, "explicit null invents no company profile");
            GameFactory plainFactory = CoreFactory(false); SimGame plain = CoreNew(plainFactory);
            c.That(plain.World.Space == null && plain.World.Systems.Count == 0 && !plain.World.Services.TryGet<WorkBoard>(out _), "core selection needs no spatial/work/resource/gameplay systems");
            c.That(plain.World.Commands.Entries.Count == 1 && plain.World.Commands.Entries[0].Id == "core.set-controller"
                && plain.World.Commands.Entries[0].Owner == "engine", "core session owns exactly one ordinary selection command");
            c.That(!plain.World.Commands.TryGet("sim.set-controller", out _), "old work-owned command identity is deleted, not aliased");
            ulong before = plain.Hash(); c.Equal(Code.None, plain.Seat(0).Preview(new SetControllerCommand { Controller = Controller.Autopilot }), "core admission works without authored controller module");
            c.Equal(before, plain.Hash(), "core selection preview changes no state");
            plain.Seat(0).Submit(new SetControllerCommand { Controller = Controller.Autopilot });
            SimGame plainTwin = plainFactory.Load(plain.Save()); Step(plain); Step(plainTwin);
            c.That(plain.Seat(0).View.ControllerOf(new PlayerId(0)) == Controller.Autopilot && plain.World.Systems.Count == 0, "selection alone does not invent a default gameplay AI");
            c.Equal(plain.Hash(), plainTwin.Hash(), "pending core command survives sole inbox/checkpoint without modules");
            c.Equal(Codes.InvalidArgument, plain.Seat(0).Preview(new SetControllerCommand { Controller = (Controller)200 }), "core enum validation is ordinary preview");
            c.Equal(Codes.InvalidArgument, TestKit.Order(plain, 0, new SetControllerCommand { Controller = (Controller)200 }).Refusal, "core refusal matches preview without work module");
            c.Equal(Controller.Autopilot, plain.Seat(0).View.ControllerOf(new PlayerId(0)), "invalid command cannot change actual player selection");
            GameFactory factory = CoreFactory(true); SimGame game = CoreNew(factory);
            c.That(game.World.Space == null && game.World.Systems.Count == 1 && !game.World.Services.TryGet<IPlanBoard>(out _), "public authored provider composes independently of work and space");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(new SetControllerCommand { Controller = Controller.Autopilot }), "no-work missing profile refuses through selected actual service");
            c.Equal(Codes.NotFound, TestKit.Order(game, 1, new SetControllerCommand { Controller = Controller.Autopilot }).Refusal, "no-work refusal uses same preview/check owner");
            game.Seat(0).Submit(new SetControllerCommand { Controller = Controller.Autopilot });
            SimGame twin = factory.Load(game.Save());
            for (int i = 0; i < 5; i++) { Step(game); Step(twin); }
            c.That(Facts(game).Decisions == 2 && Facts(game).Blocker == Codes.InvalidArgument, "no-work provider receives its actual core command answer at authored cadence");
            c.Equal(game.Hash(), twin.Hash(), "no-work saved provider/inbox/mail continuation is deterministic");
            Switch(c, game, Controller.Human); long decisions = Facts(game).Decisions;
            twin = factory.Load(game.Save()); for (int i = 0; i < 5; i++) { Step(game); Step(twin); }
            c.Equal(decisions, Facts(game).Decisions, "no-work handover remains actual dormant selection");
            c.Equal(game.Hash(), twin.Hash(), "no-work dormant memory survives normal checkpoint");
            var other = new List<PlayerControllerFacts>(); game.Seat(1).View.Feature<PlayerControllersView>().CopyControllers(other);
            c.Equal(0, other.Count, "no-work provider facts remain company private");
        }
        private static void PlaybookCodec(Check c, PlanPlaybookDef definition)
        {
            var provider = (IPlayerControllerState)definition.Create();
            c.Equal(5, provider.StateSchemaVersion, "current codec owns stable per-node execution and recovery links");
            byte[] original;
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); provider.Sync(writer); writer.Flush(); original = stream.ToArray();
            }
            var reader = new StateReader(original, 0, original.Length);
            reader.Count(0); string id = null; reader.String(ref id);
            int pendingOffset = reader.Position, pending = 0; reader.Int(ref pending);
            int firstOffset = reader.Position; long first = 0; reader.Long(ref first);
            int sinceOffset = reader.Position; long since = 0; reader.Long(ref since);
            int attemptsOffset = reader.Position, attempts = 0; reader.Int(ref attempts);
            int recoveringOffset = reader.Position;
            foreach (var mutation in new[] {
                new KeyValuePair<int, byte[]>(0, BitConverter.GetBytes(-1)),
                new KeyValuePair<int, byte[]>(0, BitConverter.GetBytes(2)),
                new KeyValuePair<int, byte[]>(pendingOffset, BitConverter.GetBytes(-1)),
                new KeyValuePair<int, byte[]>(firstOffset, BitConverter.GetBytes(-1L)),
                new KeyValuePair<int, byte[]>(firstOffset, BitConverter.GetBytes(long.MaxValue)),
                new KeyValuePair<int, byte[]>(sinceOffset, BitConverter.GetBytes(-2L)),
                new KeyValuePair<int, byte[]>(attemptsOffset, BitConverter.GetBytes(-1)),
                new KeyValuePair<int, byte[]>(recoveringOffset, new byte[] { 1 }) })
            {
                byte[] corrupt = (byte[])original.Clone();
                Array.Copy(mutation.Value, 0, corrupt, mutation.Key, mutation.Value.Length);
                c.Throws<CorruptStateException>(() => provider.Sync(new StateReader(corrupt, 0, corrupt.Length)), "current captured codec refuses malformed execution/recovery fields");
                provider.Sync(new StateReader(original, 0, original.Length));
            }
            byte[] conflicting = (byte[])original.Clone();
            Array.Copy(BitConverter.GetBytes(1), 0, conflicting, pendingOffset, 4);
            Array.Copy(BitConverter.GetBytes(1L), 0, conflicting, firstOffset, 8);
            c.Throws<CorruptStateException>(() => provider.Sync(new StateReader(conflicting, 0, conflicting.Length)), "pending request cannot also claim an execution outside recovery");
        }
        private static void CorruptCadence(Check c, GameFactory factory)
        {
            SimGame game = New(factory); game.Seat(0).Submit(new SetControllerCommand { Controller = Controller.Autopilot }); Step(game); Step(game);
            GameSystem system = (GameSystem)game.World.Services.Get<IPlayerControllers>(); byte[] bytes;
            using (var stream = new MemoryStream()) { var writer = new StateWriter(stream); system.Sync(writer); writer.Flush(); bytes = stream.ToArray(); }
            var reader = new StateReader(bytes, 0, bytes.Length); reader.Count(0);
            int integer = 0; string text = null; long number = 0;
            reader.Int(ref integer); reader.String(ref text); int schemaOffset = reader.Position; reader.Int(ref integer);
            int decisionsOffset = reader.Position; reader.Long(ref number); int lastOffset = reader.Position; reader.Long(ref number);
            int nextOffset = reader.Position; reader.Long(ref number); reader.String(ref text);
            c.Equal(0, reader.Count(0), "processed controller submission has no orphan pending ticket");
            c.Equal(1, reader.Count(0), "the sole controller codec includes its undelivered answer");
            int answerCompanyOffset = reader.Position; ulong hash = game.Hash();
            foreach (var fault in new[] {
                new KeyValuePair<int, byte[]>(schemaOffset, BitConverter.GetBytes(2)),
                new KeyValuePair<int, byte[]>(decisionsOffset, BitConverter.GetBytes(-1L)),
                new KeyValuePair<int, byte[]>(lastOffset, BitConverter.GetBytes(game.Tick)),
                new KeyValuePair<int, byte[]>(nextOffset, BitConverter.GetBytes(0L)),
                new KeyValuePair<int, byte[]>(answerCompanyOffset, BitConverter.GetBytes(1)) })
            {
                byte[] bad = (byte[])bytes.Clone(); Array.Copy(fault.Value, 0, bad, fault.Key, fault.Value.Length);
                c.Throws<CorruptStateException>(() => system.Sync(new StateReader(bad, 0, bad.Length)), "controller codec refuses invalid saved schema/cadence/private answer");
                system.Sync(new StateReader(bytes, 0, bytes.Length));
                c.Equal(hash, game.Hash(), "valid current codec restores exact native provider memory after fault fixture");
            }
        }
        private static IEnumerable<Wait> Script(Action answered)
        {
            yield return Wait.Ticks(1);
            int sequence = 0;
            yield return Wait.Do(context => sequence = context.Submit(new SetControllerCommand { Controller = Controller.Human }));
            yield return Wait.Ticks(2);
            yield return Wait.ForAnswer(sequence, result => { if (result.Accepted) answered(); });
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            EmergencyPreemptionTests.Configure(c, game); Switch(c, game, Controller.Autopilot);
            c.That(Plans(game).Count == 0 && Facts(game).AwaitingCommands == 1, "decision records no instant site or completed construction");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending ordinary template command plus native controller cursor save together");
            Step(game); Step(twin); c.Equal(game.Hash(), twin.Hash(), "same normal command creates native physical plans after restore");
            c.That(Plans(game).Count == 2 && Plans(game).TrueForAll(plan => plan.State == PlanState.Active), "engine template command starts two ordinary native physical projects");
            bool savedProgress = false, savedAnswer = false;
            for (int i = 0; i < 1200 && !Plans(game).TrueForAll(plan => plan.State == PlanState.Done); i++)
            {
                if (!savedAnswer && Facts(game).PendingAnswers > 0)
                { twin = factory.Load(game.Save()); savedAnswer = true; c.Equal(game.Hash(), twin.Hash(), "real accepted command answer saves before next strategy decision"); }
                if (!savedProgress)
                    foreach (PlanView plan in Plans(game))
                        if (game.World.Services.Get<WorkBoard>().Order(plan.Order)?.Node("build").Progress.WholeTicks > 0)
                        { twin = factory.Load(game.Save()); savedProgress = true; c.Equal(game.Hash(), twin.Hash(), "controller and earned physical progress share the same resumable checkpoint"); break; }
                Step(game); Step(twin);
            }
            c.That(Plans(game).Count == 2 && Plans(game).TrueForAll(plan => plan.State == PlanState.Done), "authored engine playbook completes real machine construction over time");
            c.That(savedProgress && savedAnswer, "both slow-decision mail and physical progress checkpoint stages were reached");
            c.Equal(game.Hash(), twin.Hash(), "physical playbook twins remain identical through completion");
            int towers = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "point.tower") towers++;
            c.Equal(2, towers, "native work effects alone create the two finished structures");
            c.Equal(Fix.FromInt(4), EmergencyPreemptionTests.Find(game, "source").Get<IInletPort>().Amount(EmergencyPreemptionTests.Supply(game)), "actual hauling and construction spend16 of20 real inputs");
            long completedDecisions = Facts(game).Decisions;
            for (int i = 0; i < 8; i++) { Step(game); Step(twin); }
            c.Equal(2, Plans(game).Count, "completed playbook never resubmits or duplicates execution history");
            c.That(Facts(game).Decisions > completedDecisions && Facts(game).Blocker.IsNone && Facts(game).AwaitingCommands == 0,
                "finished strategy remains scheduled but issues no fabricated work");
            c.Equal(game.Hash(), twin.Hash(), "completed provider memory saves/continues exactly");
            SimGame isolated = New(factory); c.That(Plans(isolated).Count == 0 && Facts(isolated).Decisions == 0, "interleaved fresh session shares no plans/controller progress");
            c.Equal(0, Plans(New(ConstructionPlanTests.Points())).Count, "omitting controller module creates no hidden autonomous work");
            Sequential(c);
        }
        private static List<PlanView> AllPlans(SimGame game)
        { var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(plans); return plans; }
        private static void Reach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1200 && !ready(); i++) { Step(game); if (twin != null) Step(twin); }
            if (!ready()) throw new InvalidOperationException(stage + " tick=" + game.Tick + " controller=" + Facts(game).Blocker
                + " plans=" + string.Join(";", AllPlans(game).ConvertAll(plan => plan.Id + "/" + plan.State + "/" + plan.Group + "/" + plan.Blocker)));
        }
        private static GameFactory SequentialFactory() => Factory(edit: json =>
        {
            JsonValue next = json.Get("plan_playbooks")[0].Get("steps")[0].Clone();
            next.Set("id", JsonValue.Of("expand")); next.Set("group", JsonValue.Of("autopilot.expansion"));
            next.Set("after", Json.Parse("[\"establish\"]"));
            JsonValue sites = json.Get("plan_site_sets")[0].Clone(); sites.Set("id", JsonValue.Of("field.expansion.sites"));
            sites.Get("sites")[0].Get("anchor").Set("position", JsonValue.Number("24")); json.Get("plan_site_sets").Add(sites);
            next.Set("sites", JsonValue.Of("field.expansion.sites")); json.Get("plan_playbooks")[0].Get("steps").Add(next);
        }, constructionEdit: json => json.Get("target_resource_requirements")[0].Get("quantities").Set("supply", JsonValue.Number("4")));
        private static void Sequential(Check c)
        {
            GameFactory factory = SequentialFactory(); SimGame game = New(factory);
            EmergencyPreemptionTests.Configure(c, game); Switch(c, game, Controller.Autopilot);
            Reach(game, () => AllPlans(game).Count == 2 && Facts(game).AwaitingCommands == 0 && Facts(game).PendingAnswers == 0, "first accepted template execution link");
            c.That(AllPlans(game).Exists(plan => plan.State != PlanState.Done), "next strategic step cannot count travelling/working projects as finished");
            c.That(TestKit.Order(game, 0, new ConfigurePlanGroupCommand { Group = "autopilot.field", Priority = 70, Paused = true }).Accepted, "player pauses actual first-stage work through ordinary group command");
            foreach (PlanView plan in AllPlans(game))
                c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = plan.Id, Group = "player.renamed", Priority = plan.Priority, Paused = true }).Accepted, "player regroups actual plans without losing stable execution identity");
            SimGame twin = factory.Load(game.Save());
            for (int i = 0; i < 7; i++) { Step(game); Step(twin); }
            c.That(AllPlans(game).Count == 2 && AllPlans(game).TrueForAll(plan => plan.Paused), "saved strategy cannot bypass player pause or create the second stage early");
            c.That(Facts(game).Blocker != Codes.NotFound, "regrouped execution still resolves by actual plan IDs, not vanished old label");
            c.Equal(game.Hash(), twin.Hash(), "regrouped paused cursor resumes with identical physical/native state");
            c.That(TestKit.Order(game, 0, new ConfigurePlanGroupCommand { Group = "player.renamed", Priority = 70, Paused = false }).Accepted, "player resumes actual regrouped work");
            twin = factory.Load(game.Save());
            Reach(game, () => AllPlans(game).Count == 4, "sequential expansion submission", twin);
            c.That(AllPlans(game).FindAll(plan => plan.Id <= 2).TrueForAll(plan => plan.State == PlanState.Done), "second template is submitted only after real first-stage completion");
            c.Equal(game.Hash(), twin.Hash(), "next strategic stage plus new actual projects remain saved deterministic twins");
            twin = factory.Load(game.Save()); Reach(game, () => AllPlans(game).TrueForAll(plan => plan.State == PlanState.Done), "second physical expansion", twin);
            c.Equal(game.Hash(), twin.Hash(), "multi-step hauling/construction survives checkpoints at both stages");
            int towers = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "point.tower") towers++;
            c.Equal(4, towers, "two authored templates create four actual structures, not duplicate fake plans");
            c.Equal(Fix.FromInt(4), EmergencyPreemptionTests.Find(game, "source").Get<IInletPort>().Amount(EmergencyPreemptionTests.Supply(game)), "four authored physical input budgets consume exactly sixteen real supplies");
            int count = AllPlans(game).Count; for (int i = 0; i < 7; i++) Step(game); c.Equal(count, AllPlans(game).Count, "completed sequential cursor never repeats automatically");

            SimGame cancelled = New(factory); EmergencyPreemptionTests.Configure(c, cancelled); Switch(c, cancelled, Controller.Autopilot);
            Reach(cancelled, () => AllPlans(cancelled).Count == 2 && Facts(cancelled).PendingAnswers == 0 && Facts(cancelled).AwaitingCommands == 0, "cancellation accepted execution link");
            c.That(TestKit.Order(cancelled, 0, new CancelPlanGroupCommand { Group = "autopilot.field" }).Accepted, "player cancels native execution, not a parallel controller ledger");
            SimGame cancelledTwin = factory.Load(cancelled.Save());
            for (int i = 0; i < 12; i++) { Step(cancelled); Step(cancelledTwin); }
            c.That(AllPlans(cancelled).Count == 2 && AllPlans(cancelled).TrueForAll(plan => plan.State == PlanState.Cancelled)
                && !Facts(cancelled).Blocker.IsNone, "cancelled actual first step blocks expansion without hidden restart");
            c.Equal(cancelled.Hash(), cancelledTwin.Hash(), "cancelled strategy/native resource recovery remains deterministic after save");

            SimGame renamed = New(factory); EmergencyPreemptionTests.Configure(c, renamed); Switch(c, renamed, Controller.Autopilot);
            Step(renamed);
            c.That(Facts(renamed).PendingAnswers == 1 && AllPlans(renamed).Count == 2, "actual template commits before slower controller consumes its answer");
            foreach (PlanView plan in AllPlans(renamed))
                renamed.Seat(0).Submit(new ConfigurePlanCommand { Plan = plan.Id, Group = "player.early", Priority = plan.Priority, Paused = true });
            Step(renamed); c.That(Facts(renamed).PendingAnswers == 1 && AllPlans(renamed).TrueForAll(plan => plan.Group == "player.early"), "player regroups before the accepted reply reaches its strategy");
            SimGame renamedTwin = factory.Load(renamed.Save());
            for (int i = 0; i < 5; i++) { Step(renamed); Step(renamedTwin); }
            c.That(Facts(renamed).PendingAnswers == 0 && Facts(renamed).Blocker != Codes.NotFound && AllPlans(renamed).Count == 2,
                "accepted answer resolves actual plan IDs even when original group label disappeared before delivery");
            c.Equal(renamed.Hash(), renamedTwin.Hash(), "saved accepted mail binds the same regrouped physical execution");
            PlanTemplate unrelated = renamed.Seat(0).View.Feature<WorkView>().AuthoredPlanTemplate("point.pair");
            c.That(TestKit.Order(renamed, 0, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(unrelated),
                Anchor = Json.Parse("{\"space\":\"field.points\",\"position\":40}"), Group = "autopilot.field" }).Accepted,
                "player can use the retired label for another ordinary physically supplied plan group");
            c.That(TestKit.Order(renamed, 0, new CancelPlanGroupCommand { Group = "player.early" }).Accepted,
                "player cancellation releases original machine commitments before unrelated work can run");
            renamedTwin = factory.Load(renamed.Save());
            Reach(renamed, () => AllPlans(renamed).FindAll(plan => plan.Id >= 3).TrueForAll(plan => plan.State == PlanState.Done), "unrelated reused-label physical group", renamedTwin);
            for (int i = 0; i < 7; i++) { Step(renamed); Step(renamedTwin); }
            c.That(AllPlans(renamed).Count == 4 && AllPlans(renamed).FindAll(plan => plan.Id <= 2).TrueForAll(plan => plan.State == PlanState.Cancelled),
                "completed unrelated same-label projects cannot advance the cancelled original strategy or create expansion");
            c.That(!AllPlans(renamed).Exists(plan => plan.Group == "autopilot.expansion"), "stable execution links never substitute another group's completion");
            c.Equal(renamed.Hash(), renamedTwin.Hash(), "reused labels and authoritative unrelated physical work remain deterministic saved twins");
        }
    }
}
