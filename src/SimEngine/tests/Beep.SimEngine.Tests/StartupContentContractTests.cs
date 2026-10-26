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
    // JSON input and public player actions only. Catalog instrumentation belongs to the module proof.
    internal static class StartupContentContractTests
    {
        internal const string Case = "Startup content contracts: independent nodes and profiles share aliases, physical work and saved replay";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 839, Seats = { Controller.Human, Controller.Human } });
        private static EntityView Entity(SimGame game, string def) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == def);
        private static Fix Position(SimGame game) => ((PointSnapshot)Entity(game, "machine").Spatial).Position;
        private static Fix Stock(SimGame game, string def)
        {
            var facts = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, def).Id, facts);
            return facts.Single(f => f.Key == Storage.FacetStock && f.Subject == "supply").Value;
        }
        private static void Step(SimGame game, SimGame twin)
        {
            game.Update(); twin.Update();
            if (game.Fault != null || twin.Fault != null || game.Hash() != twin.Hash())
                throw new InvalidOperationException("contract startup Update " + game.Clock.StepIndex + ": " + game.Fault + "/" + twin.Fault);
        }
        private static void Run(Check c)
        {
            var init = new InitializeProbe(); var stage = new StartupStageProbe(init) { InspectContracts = true };
            var factory = StartupCompositionTests.Factory(init, stage, contractRoot: true); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "contract content identity updated");
            c.Equal(154, EngineInfo.SaveFormat, "metadata-only migration has no saved layout change");
            var facts = stage.ContentFacts;
            c.Equal(5, facts.Entries, "contract index aliases all actual native/external entries once");
            c.Equal(4, facts.Profiles, "contract profile index aliases native and independent profiles once");
            c.That(facts.NativeAlias && facts.ExternalNodeAlias && facts.ExternalRootAlias, "contracts alias exact actual definitions, no native inheritance requirement");
            c.That(facts.OwnedAndFrozen, "actual contract aliases remain catalog-owned and frozen");
            c.That(facts.MissingFind && facts.MissingGetRefused && facts.WrongContractRefused, "missing or wrong selected contract never fabricates a default");
            c.That(facts.AmbiguousFindRefused && facts.AmbiguousGetRefused, "broad shared identity refuses instead of selecting first provider");
            c.That(facts.TableReadDetached && facts.ProfileListsImmutable, "table reads are detached and retained profile references are immutable");
            c.Equal(1, stage.EntryValidated, "independent selected node validates before Initialize");
            c.Equal(1, stage.EntryGenerated, "native stage executes independent node once");
            c.Equal(1, init.Initializations, "valid contract composition initializes normally");
            c.Equal(3, game.Seat(0).Snapshot.Entities.Count, "unselected company99 contract entry generates nothing");
            c.Equal(Fix.FromInt(20), Stock(game, "source"), "native startup inventory exists once");
            ulong opening = game.Hash();
            var twin = factory.Load(game.Save());
            c.Equal(opening, twin.Hash(), "contract metadata restores same actual state");
            c.Equal(1, stage.EntryGenerated, "restore does not execute independent startup node again");
            c.Equal(2, stage.EntryValidated, "restore validates actual selected contracts");
            var recording = game.RecordReplay(new ReplayLimits(1024 * 1024, 600));
            var command = new PostOrderCommand { Plan = "inspect", Target = Entity(game, "destination").Id };
            ulong previewHash = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "real work preview admits");
            c.Equal(previewHash, game.Hash(), "contract-backed startup does not alter preview purity");
            c.That(TestKit.Order(game, 0, command).Accepted, "player posts physical work");
            c.Equal(1, game.Seat(0).View.Feature<WorkView>().ActiveOrders(Entity(game, "destination").Id), "work is not instant");
            twin = factory.Load(game.Save()); int updates = 0; bool traveled = false;
            while (game.Seat(0).View.Feature<WorkView>().ActiveOrders(Entity(game, "destination").Id) != 0 && updates++ < 160)
            { Step(game, twin); traveled |= Position(game) != Fix.FromInt(4); }
            c.That(updates < 160 && traveled, "contract-authored world performs timed physical travel/work");
            c.That(game.Seat(0).View.TryFindResource("supply", out var supply), "stable resource resolves");
            c.That(TestKit.Order(game, 0, new HaulResourcesCommand { Source = Entity(game, "source").Id, Destination = Entity(game, "destination").Id,
                Worker = Entity(game, "machine").Id, Resource = supply, Ceiling = Fix.FromInt(4), Priority = 100 }).Accepted,
                "native hauling uses actual contract-backed startup entities");
            c.Equal(Fix.Zero, Stock(game, "destination"), "acceptance teleports no cargo");
            twin = factory.Load(game.Save()); updates = 0;
            while (Stock(game, "destination") != Fix.FromInt(4) && updates++ < 160) Step(game, twin);
            c.That(updates < 160, "physical delivery finishes within authored bound");
            c.Equal(Fix.FromInt(16), Stock(game, "source"), "actual stock debited exactly delivered amount");
            c.Equal(Fix.FromInt(4), Stock(game, "destination"), "actual stock credited once");
            c.Equal(1, stage.EntryGenerated, "saved work/hauling never reruns contract startup");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), new ReplayLimits(1024 * 1024, 600)).Success, "independent startup and physical player stream replay identically");
            c.Equal(opening, New(factory).Hash(), "independent interleaved session has identical opening state");

            foreach (string invalid in new[] { "missing-entry", "wrong-entry", "missing-profile", "wrong-profile", "contract-cycle",
                "ambiguous-entry", "ambiguous-profile", "missing-after", "foreign-company" })
            {
                var refused = new InitializeProbe(); var probe = new StartupStageProbe(refused);
                c.Throws<ContentException>(() => New(StartupCompositionTests.Factory(refused, probe, contractRoot: true, edit: root => {
                    if (invalid == "missing-entry") root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("not-listed"));
                    if (invalid == "wrong-entry") root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("inspect"));
                    if (invalid == "contract-cycle") root.Get("world_setup_profiles")[0].Get("includes").Add(JsonValue.Of("admission.contract-root"));
                    if (invalid == "foreign-company") root.Get("entity_setups")[0].Set("company", JsonValue.Number("99"));
                }, editExtra: extra => {
                    if (invalid == "missing-profile") extra.Get("contract_world_setup_profiles")[0].Set("include", JsonValue.Of("not-listed"));
                    if (invalid == "wrong-profile") extra.Get("contract_world_setup_profiles")[0].Set("include", JsonValue.Of("source"));
                    if (invalid == "ambiguous-entry") extra.Get("startup_admission_probes")[0].Set("id", JsonValue.Of("source"));
                    if (invalid == "ambiguous-profile") extra.Get("contract_world_setup_profiles")[0].Set("id", JsonValue.Of("points.start"));
                    if (invalid == "missing-after") extra.Get("startup_admission_probes")[0].Remove("after");
                })), "selected contract refuses " + invalid);
                c.Equal(0, refused.Initializations, invalid + " rejects before Initialize");
                c.Equal(0, probe.EntryGenerated, invalid + " generates no startup node");
                c.Equal(0, probe.Generated, invalid + " generates no later stage");
            }
        }
    }
}

