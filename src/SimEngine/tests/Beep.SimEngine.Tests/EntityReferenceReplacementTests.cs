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
    internal static partial class EntityReferenceReplacementTests
    {
        internal const string Case = "Entity replacement references: physical homes independent owners refusals and saved continuation";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add("Reference commit: clock geometry stock and claims stay invariant across saved replacement", CommitFaults);
            TestRunner.Add(ResourceIsolationCase, ResourceIsolation);
            TestRunner.Add(ResourceCodecCase, ResourceCodec);
            TestRunner.Add(CombinedResourceCase, CombinedResources);
        }
        internal static GameFactory Factory(bool external = false, string mode = "normal", Action<JsonValue> edit = null, string moduleDocument = "{}",
            IReadOnlyList<GameModule> extraModules = null, string extraDeclarations = null, IReadOnlyDictionary<string, string> extraDocuments = null)
        {
            var root = Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.references"));
            var next = root.Get("entities")[0].Clone(); next.Set("id", JsonValue.Of("source.next"));
            root.Get("entities").Add(next);
            var crew = root.Get("entities")[2].Clone(); crew.Set("id", JsonValue.Of("crew"));
            crew.Get("components").Get("worker").Set("roles", Json.Parse("[\"inspect\"]"));
            root.Get("entities").Add(crew);
            StartupContentAuthoring.Add(root, "crew", "crew", 0, Json.Parse("{\"space\":\"field.points\",\"position\":23}"));
            StartupContentAuthoring.Position(root, "source", JsonValue.Number("20"));
            StartupContentAuthoring.Position(root, "destination", JsonValue.Number("40"));
            StartupContentAuthoring.Position(root, "machine", JsonValue.Number("5"));
            if (external)
            {
                var parts = root.Get("entities")[2].Get("components"); parts.Remove("worker");
                parts.Set("contract_worker", Json.Parse("""{"role":"haul","rate":1,"states":{"working":"processing","stopped":"blocked"}}"""));
            }
            var reference = Json.Parse("""{"id":"reference","kind":"entity","components":{"replacement_reference":{"mode":"normal"}}}""");
            reference.Get("components").Get("replacement_reference").Set("mode", JsonValue.Of(mode));
            root.Get("entities").Add(reference); StartupContentAuthoring.Add(root, "reference", "reference", 0, null);
            root.Set("entity_replacement_effects", Json.Parse("""[{"id":"reference.upgrade","source":"source","target":"source.next"}]"""));
            root.Get("activities").Add(Json.Parse("""
                {"id":"upgrade","player_orderable":true,"automatic_supply":false,"priority":1,"target_tags":[],
                 "on_complete":"reference.upgrade","nodes":[{"id":"upgrade","executor":"work.staffed","roles":["inspect"],"duration":{"numerator":3,"denominator":1,"unit":"minute"},"after":[],"requirement":null,"effect":null}]}
                """));
            edit?.Invoke(root);
            return PointGame.Factory(root.ToString(), additionalModule: new ReplacementReferencesModule(),
                moduleDeclaration: """{"id":"proof.references","version":"1.0.0","requires":[{"id":"sim.work","version":"1.0.0"}],"documents":["references.json"]}""",
                moduleDocument: moduleDocument, extraModules: extraModules, extraDeclarations: extraDeclarations, extraDocuments: extraDocuments);
        }
        private static SimGame New(GameFactory factory)
            => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 31,
                Seats = new List<Controller> { Controller.Human, Controller.Human }, Mods = { ResourceFlowsModule.ModuleId } });
        private static EntityView Find(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition);
        private static WorkerHomeView Home(SimGame game, EntityId machine)
        {
            Code result = game.Seat(0).View.Feature<WorkView>().ReadHome(machine, out var view);
            if (!result.IsNone) throw new InvalidOperationException("native home: " + result);
            return view;
        }
        private static ReplacementReferenceFacts Reference(SimGame game, EntityId owner)
        {
            if (!game.Seat(0).View.Feature<ReplacementReferenceView>().Read(owner, out var facts)) throw new InvalidOperationException("reference unavailable");
            return facts;
        }
        private static void Send(Check c, SimGame game, Command command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "actual public command admission");
            int sequence = game.Seat(0).Submit(command); game.Update();
            long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Exists(answer => answer.Sequence == sequence && answer.Accepted), "actual command commits through the engine inbox");
        }
        private static void CommitFaults(Check c)
        {
            foreach(string mode in new[]{"clock-commit","speed-commit","space-commit","random-commit","random-restore-commit",
                "move-source-commit","move-target-commit","stock-commit","claim-commit"})
            foreach(bool loaded in new[]{false,true})
            {
                var factory=Factory(mode:mode,edit:root=>{
                    var rows=root.Get("entities");JsonValue later=null;
                    for(int i=0;i<rows.Count;i++)if(rows[i].Get("id").Text=="reference")later=rows[i].Clone();
                    later.Set("id",JsonValue.Of("reference.later"));later.Get("components").Get("replacement_reference").Set("mode",JsonValue.Of("normal"));
                    rows.Add(later);StartupContentAuthoring.Add(root,"reference.later","reference.later",0,null);
                });
                var game=New(factory);EntityId sourceId=Find(game,"source").Id,firstId=Find(game,"reference").Id,laterId=Find(game,"reference.later").Id;
                Send(c,game,new SetReplacementReferenceCommand {Owner=firstId,Target=sourceId});
                Send(c,game,new SetReplacementReferenceCommand {Owner=laterId,Target=sourceId});
                c.That(firstId.Value<laterId.Value,"authored reference-owner order is explicit and stable");
                if(loaded)game=factory.Load(game.Save());var world=game.World;var source=world.Entities.Get(sourceId);
                var published=game.Seat(0).Snapshot;var prior=game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
                int priorCount=prior.Entries.Count;long priorLatest=prior.Latest;
                var twin=factory.Load(game.Save());ulong untouched=twin.Hash();EntityId next=world.Entities.NextIdentity;
                Entity made=null;bool threw=false;
                try{Upgrades.TryReplace(world,source,world.Content.Get<EntityDef>("source.next"),out made);}catch(InvalidOperationException){threw=true;}
                string label=mode+"/loaded="+loaded;
                c.That(threw&&game.Fault!=null,"reference-only commit rejects unrelated mutation: "+label);
                c.Equal(0,world.Entities.Get(laterId).Get<ReplacementReference>().Commits,"later owner is not called after first violation: "+label);
                c.That(made==null&&source.Alive&&world.Entities.Get(next)!=null,"interrupted identities remain actual, no invented rollback");
                c.Equal(Fix.Zero,source.Get<IInventory>().Stock.Count==0?Fix.Zero:source.Get<IInventory>().Stock[0].Amount,
                    "physical relocation really precedes reference-only commit");
                c.Equal(published.Tick,game.Seat(0).Snapshot.Tick,"failed reference commit retains last healthy publication");
                c.Throws<InvalidOperationException>(()=>game.Save(),"reference fault has no healthy save");
                c.Throws<InvalidOperationException>(()=>game.Hash(),"reference fault has no healthy hash");
                c.Throws<InvalidOperationException>(()=>game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0),"reference fault exposes no live success history");
                c.That(prior.Entries.Count==priorCount&&prior.Latest==priorLatest,"retained prior flow facts remain historical");
                var first=game.Fault;c.Equal(Codes.NotAllowed,Upgrades.Check(world,source,source.Def),"later replacement refuses before callbacks");
                c.That(ReferenceEquals(first,game.Fault),"first fault remains unchanged");
                c.Equal(untouched,twin.Hash(),"independent loaded session unchanged");twin.Update();c.That(twin.Fault==null,"independent session still advances");
            }
        }
        private static void Run(Check c)
        {
            foreach (bool external in new[] { false, true })
            {
                var factory = Factory(external); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                var old = Find(game, "source"); var machine = Find(game, "machine"); var owner = Find(game, "reference");
                Send(c, game, new SetWorkerHeldCommand { Worker = machine.Id, Held = true });
                Send(c, game, new SetHomeCommand { Worker = machine.Id, Home = old.Id });
                Send(c, game, new SetReplacementReferenceCommand { Owner = owner.Id, Target = old.Id });
                var captured = Home(game, machine.Id); var beforeReference = Reference(game, owner.Id);
                var position = ((PointSnapshot)Find(game, "machine").Spatial).Position;
                c.That(captured.Home == old.Id && captured.State == WorkerHomeState.Held, "held native or independent worker records actual home");
                var command = new PostOrderCommand { Plan = "upgrade", Target = old.Id };
                ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "pure upgrade intent preview");
                c.Equal(before, game.Hash(), "reference admission changes no state");
                Send(c, game, command);
                c.That(game.Seat(0).Snapshot.Entities.Any(e => e.Id == old.Id), "posting does not instantly replace the home");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "unfinished upgrade saves original references");
                int steps = 0;
                while (!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next") && steps++ < 20)
                { game.Update(); twin.Update(); if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("reference fault: " + game.Fault + "/" + twin.Fault); }
                var made = Find(game, "source.next");
                c.That(made.Id != old.Id && !game.Seat(0).Snapshot.Entities.Any(e => e.Id == old.Id), "native replacement retires original identity once");
                c.That(Home(game, machine.Id).Home == made.Id && Home(game, machine.Id).State == WorkerHomeState.Held, "existing home owner rebinds without releasing held work");
                c.Equal(position, ((PointSnapshot)Find(game, "machine").Spatial).Position, "replacement never teleports or drives the held machine");
                var rebound = Reference(game, owner.Id);
                c.That(rebound.Reference == made.Id && rebound.Commits == 1, "independent reference owner joins the same replacement exactly once");
                c.That(rebound.CheckpointRejected, "normal checkpoint hashing still rejects the in-flight completion state");
                c.Equal(game.Hash(), twin.Hash(), "saved pending reference replacement continues identically");
                c.That(captured.Home == old.Id && beforeReference.Reference == old.Id, "earlier detached facts retain original identity");
                c.That(!game.Seat(1).View.Feature<ReplacementReferenceView>().Read(owner.Id, out _), "extension references remain private");
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                c.Equal(game.Hash(), twin.Hash(), "rebound references use existing codecs");
                // The real upgrade crew occupies the home; send it away normally before returning another machine.
                Send(c, game, new SetHomeCommand { Worker = Find(game, "crew").Id, Home = Find(game, "destination").Id });
                Send(c, twin, new SetHomeCommand { Worker = Find(twin, "crew").Id, Home = Find(twin, "destination").Id });
                Send(c, game, new SetWorkerHeldCommand { Worker = machine.Id, Held = false });
                Send(c, twin, new SetWorkerHeldCommand { Worker = machine.Id, Held = false });
                steps = 0;
                while (Home(game, machine.Id).State != WorkerHomeState.AtHome && steps++ < 30) { game.Update(); twin.Update(); }
                c.Equal(WorkerHomeState.AtHome, Home(game, machine.Id).State, "ordinary Update physically returns to the replacement home; position=" + ((PointSnapshot)Find(game, "machine").Spatial).Position + ";home=" + ((PointSnapshot)Find(game, "source.next").Spatial).Position + ";blocker=" + Home(game, machine.Id).Blocker);
                c.Equal(game.Hash(), twin.Hash(), "physical rebound-home return remains deterministic");
                c.Equal(isolatedHash, isolated.Hash(), "another session has no replacement/reference changes");
                var flow = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
                c.That(flow.Entries.Count(e => e.Item.IsTransfer && e.Item.Source.Endpoint == ResourceEndpointId.ForEntity(old.Id) &&
                    e.Item.Destination.Endpoint == ResourceEndpointId.ForEntity(made.Id) && e.Item.Amount == Fix.FromInt(20)) == 1,
                    "reference work preserves actual once-only resource relocation");
            }
            foreach (string mode in new[] { "deny", "mutate-check", "throw-commit", "despawn-commit" })
            {
                var factory = Factory(mode: mode); var game = New(factory); var source = Find(game, "source"); var owner = Find(game, "reference");
                Send(c, game, new SetReplacementReferenceCommand { Owner = owner.Id, Target = source.Id });
                var upgrade = new PostOrderCommand { Target = source.Id, Plan = "upgrade" };
                if (mode == "deny")
                {
                    ulong before = game.Hash();
                    c.Equal(new Code("proof.reference-denied"), game.Seat(0).Preview(upgrade), "typed reference refusal precedes work admission");
                    c.Equal(before, game.Hash(), "refused reference admission is pure");
                    int sequence = game.Seat(0).Submit(upgrade); game.Update();
                    long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
                    c.That(answers.Any(answer => answer.Sequence == sequence && answer.Refusal == new Code("proof.reference-denied")), "ordinary inbox reports the same typed reference refusal");
                    c.That(game.Fault == null && game.Seat(0).Snapshot.Entities.Any(e => e.Id == source.Id) &&
                        !game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next"), "owner refusal preserves source/no replacement grant");
                    c.That(Reference(game, owner.Id).Reference == source.Id && Reference(game, owner.Id).Commits == 0, "denied owner commits no reference transition");
                    c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused reference state remains a coherent saved checkpoint");
                }
                else
                {
                    if (mode == "mutate-check") c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(upgrade), "mutation during pure admission is rejected");
                    else
                    {
                        Send(c, game, upgrade);
                        for (int i = 0; i < 16 && game.Fault == null; i++) game.Update();
                    }
                    c.That(game.Fault != null, "bad reference callback faults normal Update: " + mode);
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted reference callback has no healthy save");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "faulted reference callback has no healthy hash");
                }
            }
        }
    }
}

