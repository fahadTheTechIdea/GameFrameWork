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
    // Only authored intent, player input, Update and detached engine facts: no driver scheduler.
    internal static class DispatchAcceptanceTests
    {
        internal const string Case = "Dispatch acceptance: physical emergency priority aging cost identities and compatible providers";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(string profile = "dispatch.priority", bool pair = false,
            bool tie = false, bool shipment = false, bool mixed = false, bool stocked = false,
            IReadOnlyList<GameModule> extraModules = null, string extraDeclarations = null,
            IReadOnlyDictionary<string, string> extraDocuments = null, Action<JsonValue> editRoot = null, Action<JsonValue> editWork = null)
        {
            var work = Json.Parse(FrameworkProofContent.Read("shared-dispatch.json"));
            var extra = Json.Parse(FrameworkProofContent.Read("dispatch-acceptance.json"));
            if (!pair) extra.Get("world_setup_profiles")[0].Set("entries", Json.Parse("[\"acceptance.sink\"]"));
            for (int section = 0; section < extra.Count; section++)
            {
                string key = extra.KeyAt(section); JsonValue value = extra.ValueAt(section);
                if (work.Get(key) == null) work.Set(key, value);
                else for (int i = 0; i < value.Count; i++) work.Get(key).Add(value[i]);
            }
            editWork?.Invoke(work);
            var provider = Json.Parse(FrameworkProofContent.Read("dispatch-source.json"));
            if (mixed) provider.Get("proof_shipments")[0].Set("to", JsonValue.Of("acceptance.sink"));
            provider.Get("proof_shipments")[0].Set("enabled", JsonValue.Of(shipment));
            provider.Get("proof_shipments")[0].Set("priority", JsonValue.Number("100"));
            var independentProfiles = Json.Parse("[]");
            var authoredProfiles = Json.Parse(FrameworkProofContent.Read("dispatch-aging-provider.json")).Get("proof_dispatch_profiles");
            independentProfiles.Add(authoredProfiles[0]); independentProfiles.Add(authoredProfiles[5]);
            provider.Set("proof_dispatch_profiles", independentProfiles);
            var root = Json.Parse(PointGame.Definitions); var scenario = root.Get("scenarios")[0];
            scenario.Set("seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of(CompanyAlertModule.ModuleId));
            scenario.Get("modules").Add(JsonValue.Of(DispatchPolicyModule.ModuleId));
            scenario.Get("settings").Set(DispatchPolicyModule.ModuleId, Json.Parse("{\"profile\":\"" + profile + "\"}"));
            scenario.Get("settings").Set(CompanyAlertModule.ModuleId, Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"alerts.standard\",\"level\":\"normal\",\"worker_reserves\":[]},{\"company\":1,\"profile\":\"alerts.neutral\",\"level\":\"normal\",\"worker_reserves\":[]}]}"));
            root.Get("entities")[2].Get("components").Set("work_policy", Json.Parse("{\"profile\":\"shared.machine\"}"));
            root.Get("entity_states").Add(Json.Parse("{\"id\":\"stopped\",\"state\":\"point.incident\",\"priority\":70}"));
            root.Get("entities")[1].Get("components").Set("contract_incident", Json.Parse("{\"id\":\"damage\",\"kind\":\"proof.failure\",\"response\":\"shared.dry-response\"}"));
                    root.Get("entities")[0].Get("components").Get("point_presence").Set("position", JsonValue.Number("20"));
                    root.Get("entities")[1].Get("components").Get("point_presence").Set("position", JsonValue.Number("25"));
                    root.Get("entities")[2].Get("components").Get("point_presence").Set("position", JsonValue.Number(tie ? "10" : "5"));
                    root.Get("entity_setups")[0].Get("placement").Set("position", JsonValue.Number("20"));
                    root.Get("entity_setups")[1].Get("placement").Set("position", JsonValue.Number("25"));
                    root.Get("entity_setups")[2].Get("placement").Set("position", JsonValue.Number(tie ? "10" : "5"));
                    root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number(mixed ? "24" : "20"));
                    root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number(stocked ? "20" : "0"));
            var alerts = Json.Parse("{\"company_alert_profiles\":[]}");
            var authoredAlerts = Json.Parse(FrameworkProofContent.Read("company-alerts.json")).Get("company_alert_profiles");
            alerts.Get("company_alert_profiles").Add(authoredAlerts[0]); alerts.Get("company_alert_profiles").Add(authoredAlerts[1]);
            if (extraModules != null) foreach (var module in extraModules) scenario.Get("modules").Add(JsonValue.Of(module.Id));
            editRoot?.Invoke(root);
            return PointGame.Factory(root.ToString(), work.ToString(), new DispatchProofModule(),
                "{\"id\":\"proof.shipments\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"},{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[\"acceptance-provider.json\"]}",
                provider.ToString(), alerts.ToString(), dispatchProfiles: FrameworkProofContent.Read("dispatch-policy.json"), startupProfiles: new[] { "dispatch-acceptance.start" },
                extraModules: extraModules, extraDeclarations: extraDeclarations, extraDocuments: extraDocuments);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 712, Mods = { "proof.shipments" }, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Id(SimGame game, string name) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == name && e.Owner == new PlayerId(0)).Id;
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static List<WorkerDispatchOffer> Offers(SimGame game)
        { var facts = new List<WorkerDispatchOffer>(); Work(game).CopyAssignments(facts); return facts; }
        private static List<WorkJobView> Jobs(SimGame game)
        { var facts = new List<WorkJobView>(); Work(game).CopyJobs(facts); return facts; }
        private static ActivityNodeView Node(SimGame game, int order)
        { var facts = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(order, facts)) throw new InvalidOperationException("missing actual activity " + order); return facts.Single(); }
        private static bool Done(SimGame game, string plan, string target, int order)
            => Work(game).TryReadActivityCompletion("activity:" + plan, Id(game, target), out var fact) && fact.Order == order;
        private static Fix Stock(SimGame game, string name)
        { var facts = new List<Facet>(); game.Seat(0).View.Inspect(Id(game, name), facts); return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value); }
        private static Fix Position(SimGame game, string name) => ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Id(game, name)).Spatial).Position;
        private static void Step(Check c, SimGame game, SimGame twin)
        {
            game.Update(); twin.Update();
            if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("actual dispatch fault " + game.Fault + "/" + twin.Fault);
            c.Equal(game.Hash(), twin.Hash(), "same actual dispatch/material/motion/clock continuation");
        }
        private static void Until(Check c, SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 512 && !ready(); i++) Step(c, game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " exceeded512Updates at=" + game.Clock.StepIndex + "/jobs=" +
                string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/worker=" + j.Worker + "/loaded=" + j.Loaded)));
        }
        private static CommandResult[] Batch(Check c, GameFactory factory, SimGame game, out SimGame twin, params Command[] commands)
        {
            var sequences = new List<int>();
            foreach (var command in commands) {
                ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal dispatch command preview");
                c.Equal(before, game.Hash(), "preview creates no age/claims/resources"); sequences.Add(game.Seat(0).Submit(command));
            }
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending command inbox restores"); Step(c, game, twin);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            var answers = sequences.Select(s => results.Single(r => r.Sequence == s)).ToArray();
            foreach (var answer in answers) { c.That(answer.Accepted, "normal dispatch command result: " + answer.Refusal); if (!answer.Accepted) throw new InvalidOperationException("actual dispatch refusal " + answer.Refusal); }
            return answers;
        }
        private static ConfigureWorkPolicyCommand Enable(SimGame game, string worker = "machine", bool eligible = true, bool tieRoles = false)
            => new ConfigureWorkPolicyCommand { Worker = Id(game, worker), Settings = new WorkPolicySettings(true, 0,
                worker == "machine" || tieRoles ? new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") } :
                eligible ? new[] { new WorkerRoleId("alternate"), new WorkerRoleId("haul") } : new[] { new WorkerRoleId("alternate") },
                null, true, true, WorkAssignmentMode.All) };
        private static ConfigureStockPolicyCommand Demand(SimGame game, int priority)
        {
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("missing actual resource");
            return new ConfigureStockPolicyCommand { Store = Id(game, "destination"), Resource = resource,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(8), Fix.FromInt(8), priority, LogisticsReserveAccess.Protected)) };
        }
        private static PostOrderCommand Post(SimGame game, string plan, string target, int priority)
            => new PostOrderCommand { Plan = plan, Target = Id(game, target), Priority = priority };
        private static SimGame Partial(Check c, GameFactory factory, SimGame game)
        {
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial physical assignment/material save");
            game.Clock.Pause(); twin.Clock.Pause(); ulong before = game.Hash(); long time = game.Clock.StepIndex;
            c.Equal(0, game.Update(), "paused sole game clock produces no work"); c.Equal(0, twin.Update(), "saved paused sole clock");
            c.Equal(time, game.Clock.StepIndex, "pause preserves actual work age"); c.Equal(before, game.Hash(), "pause preserves resources and claims");
            game.Clock.Resume(); twin.Clock.Resume(); return twin;
        }
        private static void Finish(Check c, GameFactory factory, SimGame game, SimGame twin, SimGame isolated, ulong untouched, IReplayRecorder recorder)
        {
            c.Equal(game.Hash(), twin.Hash(), "terminal saved dispatch twins"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal actual dispatch state restores");
            c.Equal(untouched, isolated.Hash(), "interleaved session unchanged");
            ulong before = game.Hash(); var held = Offers(game); var foreign = new List<WorkerDispatchOffer>(); game.Seat(1).View.Feature<WorkView>().CopyAssignments(foreign);
            c.Equal(0, foreign.Count, "foreign company receives no private dispatch facts"); c.Equal(before, game.Hash(), "detached dispatch reads are pure");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "actual command/time/rank replay: " + replay.Failure + "/" + replay.Detail);
        }
        private static void Run(Check c)
        {
            foreach (string profile in new[] { "dispatch.priority", "dispatch.fair", "dispatch.mod-priority" }) Ranking(c, profile);
            Emergency(c); StableIntents(c); IntentCostAndAge(c, false); IntentCostAndAge(c, true); Mixed(c);
            foreach (string provider in new[] { "native", "logistics", "independent" }) {
                Selection(c, provider, false, false); Selection(c, provider, true, false);
                if (provider != "native") Selection(c, provider, false, true);
            }
        }
        private static void Ranking(Check c, string profile)
        {
            var factory = Factory(profile: profile, stocked: true); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); using var recorder = game.RecordReplay(Limits);
            int low = (int)Batch(c, factory, game, out var twin, Post(game, "ordering.low", "source", 1))[0].Arg0;
            for (int i = 0; i < 7; i++) Step(c, game, twin);
            int high = (int)Batch(c, factory, game, out twin, Post(game, "ordering.high", "destination", 1000))[0].Arg0;
            var held = Offers(game); bool priority = profile != "dispatch.fair";
            c.That(held.Single(o => o.Identity == Jobs(game).Single(j => j.Target == Id(game, "source")).Id).Starved, "old actual job meets authored starvation threshold");
            c.Equal(Id(game, priority ? "destination" : "source"), held[0].Target, "actual authored priority/aging order");
            Batch(c, factory, game, out twin, Enable(game));
            c.That(Node(game, priority ? high : low).Worker == Id(game, "machine") && !Node(game, priority ? low : high).Worker.IsValid, "rank chooses only first actual physical worker");
            c.Equal(Fix.FromInt(20), Stock(game, "source"), "assignment cannot consume remote materials instantly");
            c.Equal(Fix.FromInt(20), Stock(game, "destination"), "no remote completion fallback");
            Step(c, game, twin); twin = Partial(c, factory, game);
            Until(c, game, twin, () => Done(game, priority ? "ordering.high" : "ordering.low", priority ? "destination" : "source", priority ? high : low), "first ranked physical work");
            Until(c, game, twin, () => Done(game, priority ? "ordering.low" : "ordering.high", priority ? "source" : "destination", priority ? low : high), "second ranked physical work");
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "actual first source input consumed once"); c.Equal(Fix.FromInt(12), Stock(game, "destination"), "actual remote input consumed once");
            c.Equal(Id(game, priority ? "destination" : "source"), held[0].Target, "retained rank facts stay detached"); Finish(c, factory, game, twin, isolated, untouched, recorder);
        }
        private static void Emergency(Check c)
        {
            var factory = Factory(stocked: true); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); using var recorder = game.RecordReplay(Limits);
            int ordinary = (int)Batch(c, factory, game, out var twin, Post(game, "ordering.low", "source", int.MaxValue))[0].Arg0;
            for (int i = 0; i < 7; i++) Step(c, game, twin);
            Batch(c, factory, game, out twin, new RaiseIncidentCommand { Target = Id(game, "destination"), Source = new Code("damage"), Kind = new Code("proof.failure") });
            c.That(Offers(game)[0].Emergency && Offers(game)[0].Priority < int.MaxValue, "real incident outranks starving maximum-priority work");
            Batch(c, factory, game, out twin, Enable(game)); c.That(!Node(game, ordinary).Worker.IsValid && Jobs(game).Any(j => j.Target == Id(game, "destination") && j.Worker == Id(game, "machine")), "actual response owns worker first");
            twin = Partial(c, factory, game); Until(c, game, twin, () => Work(game).TryReadActivityCompletion("activity:shared.dry-response", Id(game, "destination"), out _), "physical emergency response");
            c.Equal(Fix.FromInt(20), Stock(game, "destination"), "explicit dry response invents no consumption");
            Until(c, game, twin, () => Done(game, "ordering.low", "source", ordinary), "ordinary aged work resumes after response"); Finish(c, factory, game, twin, isolated, untouched, recorder);
        }
        private static void StableIntents(Check c)
        {
            var factory = Factory(); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); using var recorder = game.RecordReplay(Limits);
            var answers = Batch(c, factory, game, out var twin, Post(game, "shared.work", "source", 100), Post(game, "shared.second", "source", 100));
            int first = (int)answers[0].Arg0, second = (int)answers[1].Arg0; var offers = Offers(game);
            c.Equal(offers[0].CreatedTick, offers[1].CreatedTick, "same command-update age tie"); c.Equal(offers[0].Target, offers[1].Target, "same actual target tie");
            c.That(offers[0].Identity < offers[1].Identity, "actual stable native job IDs break equal-ranked intent tie");
            Batch(c, factory, game, out twin, Enable(game)); c.That(Node(game, first).Worker.IsValid && !Node(game, second).Worker.IsValid, "actual first ID gets scarce worker");
            twin = Partial(c, factory, game); Until(c, game, twin, () => Done(game, "shared.work", "source", first), "first stable-ID job");
            Until(c, game, twin, () => Done(game, "shared.second", "source", second), "second stable-ID job");
            c.Equal(Fix.FromInt(20), Stock(game, "source"), "authored no-input activities consume no invented resource"); Finish(c, factory, game, twin, isolated, untouched, recorder);
        }
        private static void IntentCostAndAge(Check c, bool aging)
        {
            var factory = Factory(); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); using var recorder = game.RecordReplay(Limits);
            int far, near; SimGame twin;
            if (aging) {
                far = (int)Batch(c, factory, game, out twin, Post(game, "shared.work", "destination", 100))[0].Arg0;
                for (int i = 0; i < 7; i++) Step(c, game, twin);
                near = (int)Batch(c, factory, game, out twin, Post(game, "shared.second", "source", 100))[0].Arg0;
            } else {
                var results = Batch(c, factory, game, out twin, Post(game, "shared.work", "destination", 100), Post(game, "shared.second", "source", 100));
                far = (int)results[0].Arg0; near = (int)results[1].Arg0;
            }
            c.That(far < near, "far intent deliberately has earlier stable order ID");
            var offers = Offers(game);
            c.That(aging ? offers[0].Starved && !offers[1].Starved : offers[0].CreatedTick == offers[1].CreatedTick, "actual age/threshold facts match authored competition");
            Batch(c, factory, game, out twin, Enable(game));
            c.That(Node(game, aging ? far : near).Worker == Id(game, "machine") && !Node(game, aging ? near : far).Worker.IsValid,
                aging ? "older same-priority work beats nearer younger intent" : "same-age nearer intent beats earlier far ID");
            twin = Partial(c, factory, game);
            Until(c, game, twin, () => Done(game, aging ? "shared.work" : "shared.second", aging ? "destination" : "source", aging ? far : near), "first actual age/cost-ranked intent");
            Until(c, game, twin, () => Done(game, aging ? "shared.second" : "shared.work", aging ? "source" : "destination", aging ? near : far), "second actual age/cost-ranked intent");
            c.Equal(Fix.FromInt(20), Stock(game, "source"), "no-input authored competition invents no material consumption"); Finish(c, factory, game, twin, isolated, untouched, recorder);
        }
        private static void Mixed(Check c)
        {
            var factory = Factory(shipment: true, mixed: true); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); using var recorder = game.RecordReplay(Limits);
            int order = (int)Batch(c, factory, game, out var twin, Post(game, "ordering.low", "source", 300), Demand(game, 200))[0].Arg0;
            var offers = Offers(game); c.Equal(3, offers.Count, "one coordinator projects all three actual providers");
            c.Equal("sim.work", offers[0].Scope, "player native priority300 first"); c.Equal("sim.logistics", offers[1].Scope, "stock policy priority200 second"); c.Equal("proof.shipments", offers[2].Scope, "independent JSON priority100 third");
            c.That(offers[2].Identity > int.MaxValue, "independent intent keeps full stable64-bit identity");
            Batch(c, factory, game, out twin, Enable(game)); c.That(Node(game, order).Worker.IsValid && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "highest native work retains scarce compatible worker");
            twin = Partial(c, factory, game); Until(c, game, twin, () => Done(game, "ordering.low", "source", order), "mixed native consumption");
            Until(c, game, twin, () => Stock(game, "destination") == Fix.FromInt(8), "mixed ordinary stock-policy haul");
            c.Equal(Fix.Zero, Stock(game, "acceptance.sink"), "lower independent dispatch cannot deliver before higher logistics");
            twin = Partial(c, factory, game); Until(c, game, twin, () => Stock(game, "acceptance.sink") == Fix.FromInt(8) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "mixed independent actual haul");
            c.Equal(Fix.Zero, Stock(game, "source"), "twenty-four finite input split into eight consumption and two actual eight-unit hauls"); Finish(c, factory, game, twin, isolated, untouched, recorder);
        }
        private static void Selection(Check c, string provider, bool tie, bool incompatible)
        {
            var factory = Factory(pair: true, tie: tie, shipment: provider == "independent"); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); using var recorder = game.RecordReplay(Limits);
            var twin = factory.Load(game.Save()); int order = 0;
            if (provider == "native") order = (int)Batch(c, factory, game, out twin, Post(game, "acceptance.work", "source", 100))[0].Arg0;
            if (provider == "logistics") Batch(c, factory, game, out twin, Demand(game, 100));
            string selected = incompatible ? "machine" : "acceptance.alternate"; string unselected = selected == "machine" ? "acceptance.alternate" : "machine";
            Fix start = Position(game, selected), idle = Position(game, unselected);
            Batch(c, factory, game, out twin, Enable(game), Enable(game, "acceptance.alternate", !incompatible, tie));
            Until(c, game, twin, () => Jobs(game).Any(j => j.Worker.IsValid), "actual compatible-provider acquisition");
            var actual = Jobs(game).Single(j => j.Worker.IsValid); c.Equal(Id(game, selected), actual.Worker, provider + " actual compatible cost/ID selection");
            c.That(actual.IsHaul == (provider != "native"), "source uses original native work/haul execution owner");
            c.Equal(Fix.FromInt(20), Stock(game, "source"), "selection does not teleport or spend source"); c.Equal(Fix.Zero, Stock(game, "destination"), "selection cannot instantly deliver");
            if (tie) c.That(Id(game, selected).Value < Id(game, unselected).Value, "equal-cost native IDs are actual stable startup identities");
            Step(c, game, twin); twin = Partial(c, factory, game);
            Until(c, game, twin, () => Position(game, selected) != start, "selected actual movement");
            c.Equal(idle, Position(game, unselected), "unselected or incompatible worker remains unclaimed at physical location");
            Until(c, game, twin, () => provider == "native" ? Done(game, "acceptance.work", "source", order) : Stock(game, "destination") == Fix.FromInt(8) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "actual selected work/haul completion");
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "only actual eight consumed or physically exported");
            c.Equal(provider == "native" ? Fix.Zero : Fix.FromInt(8), Stock(game, "destination"), "actual delivery conservation without instant fallback");
            c.Equal(idle, Position(game, unselected), "other compatible alternative stays physically idle"); Finish(c, factory, game, twin, isolated, untouched, recorder);
        }
    }
}
