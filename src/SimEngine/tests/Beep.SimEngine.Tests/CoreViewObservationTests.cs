using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class CoreViewObservationTests
    {
        internal const string Case = "Core view observations: guarded callbacks, private facts and atomic fault publication";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        // Engine-side failure instrumentation alone may access actual providers. Healthy players use public views/commands.
        private static ViewObservationVisibility Visibility(SimGame game) => (ViewObservationVisibility)game.World.Services.Get<IVisibility>();
        private static Entity Own(SimGame game, int seat = 0) => game.World.Entities.All.Single(e => e.Owner == new PlayerId(seat));
        private static EntityId Visible(SimGame game, int seat = 0) => game.Seat(seat).Snapshot.Entities.Single().Id;
        private static Fix Read(SimGame game, EntityId id)
        {
            if (!game.Seat(0).View.Feature<ViewObservationReadModel>().Read(id, out Fix value))
                throw new InvalidOperationException("missing private observation fact");
            return value;
        }
        private static void Reject<T>(Check c) where T : class
        {
            var spy = new InitializeProbe();
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(new ProbeModule("probe", new ModuleVersion(1, 0, 0), compose: composer =>
            { composer.AddSystem(spy, 10); composer.AddViewFeature<T>("probe.mutable", 1, (world, viewer) => null); })), "direct mutable contract refuses: " + typeof(T).Name);
            c.Equal(0, spy.Initializations, "contract refusal precedes Initialize: " + typeof(T).Name);
        }
        private static void Registrations(Check c)
        {
            Reject<object>(c); Reject<World>(c); Reject<IEntity>(c); Reject<IEntityCapability>(c);
            Reject<GameSystem>(c); Reject<IPersistentService>(c); Reject<IGameClock>(c);
            Reject<EntityStore>(c); Reject<ServiceContainer>(c); Reject<PlayerTable>(c); Reject<CommandRegistry>(c); Reject<EventBus>(c);
            var alias = CompositionProbe.New(new ProbeModule("probe", new ModuleVersion(1, 0, 0), compose: composer =>
            {
                var state = new ProbeState("probe.state"); composer.Services.Bind<IProbeFirst>(state);
                composer.AddViewFeature<IProbeFirst>("probe.alias", 1, (world, viewer) => state);
            }));
            c.Throws<InvalidOperationException>(() => alias.Seat(0).View.Feature<IProbeFirst>(), "unknown interface cannot hide a returned persistent owner");
            c.That(alias.Fault != null && alias.Fault.Phase == "observation.provider", "unsafe returned owner is an observation fault");
            var nullView = CompositionProbe.New(new ProbeModule("probe", new ModuleVersion(1, 0, 0), compose: composer =>
                composer.AddViewFeature<ProbeView>("probe.null", 1, (world, viewer) => null)));
            c.Throws<InvalidOperationException>(() => nullView.Seat(0).View.Feature<ProbeView>(), "declared null factory is not an absent feature");
            c.That(nullView.Fault != null && nullView.Seat(0).View.Feature<OtherProbeView>() == null, "null callback faults but unregistered optional metadata remains absent");
        }
        private static void Healthy(Check c)
        {
            var authored = Json.Parse(FrameworkProofContent.Read("view-observation.json"));
            var hiddenPresence = authored.Get("entities")[0].Get("components").Get("tile_presence").Clone();
            hiddenPresence.Set("width", JsonValue.Number("4")); hiddenPresence.Set("height", JsonValue.Number("2"));
            authored.Get("entities")[1].Get("components").Set("tile_presence", hiddenPresence);
            var factory = ViewObservationProof.Factory(authored: authored.ToString()); var game = ViewObservationProof.New(factory);
            var other = ViewObservationProof.New(factory); ulong untouched = other.Hash();
            var view = game.Seat(0).View; var retained = game.Seat(0).Snapshot;
            EntityId own = Visible(game), foreign = Visible(game, 1); ulong before = game.Hash();
            var entities = new List<EntityView>(); view.CopyEntities(entities);
            c.That(entities.Count == 1 && entities[0].Id == own && entities[0].Spatial is TileSpatialSnapshot, "engine returns only own detached spatial entity facts");
            view.CopyEntities(new PlayerId(1), entities); c.Equal(0, entities.Count, "owner filter cannot reveal another player's entity");
            c.That(!view.TryGetEntity(foreign, out _) && !view.Inspect(foreign, new List<Facet>()), "private target refuses before inspector runs");
            c.Equal(0, Own(game, 1).Get<ViewObservationPart>().Reads, "hidden inspectors are never invoked");
            var catalogue = new List<CatalogueEntry>(); view.CopyCatalogue(catalogue);
            c.That(catalogue.Count == 1 && catalogue[0].Id == "actor", "catalogue hides unlearned prototype");
            var resources = new List<ResourceInfo>(); view.CopyResources(resources);
            c.That(resources.Count == 1 && resources[0].Id == "visible" && !view.TryFindResource("secret", out _), "typed resource visibility is engine-owned");
            var units = new List<UnitInfo>(); view.CopyUnits(units);
            c.Equal(1, units.Count, "pure unit list uses selected visibility");
            var capacities = new List<CapacityPolicyInfo>(); view.CopyCapacityPolicies(capacities); c.Equal(0, capacities.Count, "absent authored policies are not fabricated");
            var map = view.Feature<TileMapView>(); map.CopyEntities(new TileRect(0, 0, 16, 16), entities);
            c.That(entities.Count == 1 && map.TryGetOccupantAt(new TileCoord(2, 2), out var occupant) && occupant.Id == own &&
                !map.TryGetOccupantAt(new TileCoord(5, 2), out _), "spatial query and occupant use the same private projection");
            c.That(map.TryGetFootprint("actor", out var footprint) && footprint.Equals(new TileRect(0, 0, 1, 1)), "visible prototype footprint uses authored geometry");
            c.That(!map.TryGetFootprint("secret", out footprint) && footprint.Equals(default(TileRect)), "hidden authored footprint returns no geometry");
            c.That(game.Observer.Feature<TileMapView>().TryGetFootprint("secret", out footprint)
                && footprint.Equals(new TileRect(0, 0, 4, 2)), "explicit observer proves hidden geometry actually exists");
            var copied = entities[0]; copied.Owner = new PlayerId(1); copied.Def = "client.changed"; copied.Spatial = null;
            c.That(retained.Entities[0].Id == own && retained.Entities[0].Owner == new PlayerId(0)
                && retained.Entities[0].Def == "actor" && retained.Entities[0].Spatial != null, "editing copied entity facts cannot change retained authoritative projection");
            c.Throws<NotSupportedException>(() => ((IList<EntityView>)retained.Entities).Clear(), "retained entity collection is immutable");
            c.Throws<NotSupportedException>(() => ((IList<EntityId>)retained.Removed).Clear(), "retained removal collection is immutable");
            c.Equal(Codes.OutOfBounds, map.CopyEntities(new TileRect(int.MaxValue, 0, 1, 1), entities), "wide invalid geometry is explicitly refused");
            c.Equal(0, entities.Count, "invalid geometry clears caller's prior facts");
            entities.Add(copied);
            c.Equal(Code.None, map.CopyEntities(new TileRect(0, 0, 0, 1), entities), "empty in-bounds region is a successful empty query");
            c.Equal(0, entities.Count, "empty geometry clears caller's prior facts");
            var space = view.Space; c.That(space is TileWorldSnapshot && space.Space == "views.tiles", "selected space returns detached engine facts");
            for (int i = 0; i < 8; i++) c.Equal(Fix.FromInt(2), Read(game, own), "repeated lazy feature/inspection read earns no work");
            c.Equal(before, game.Hash(), "all observations preserve state hash"); c.Equal(0L, game.Tick, "observations advance no game time");
            using (var recorder = game.RecordReplay(Limits))
            {
                game.Update(); c.Equal(Fix.FromInt(3), Read(game, own), "authored module update alone earns the next value");
                c.Equal(retained.Entities[0].Version + 1, game.Seat(0).Snapshot.Entities[0].Version, "new snapshot follows actual entity Update");
                c.Equal(0L, retained.Tick, "retained snapshot epoch remains immutable");
                c.Equal(retained.Revision, game.Seat(0).Snapshot.PreviousRevision, "delta baseline uses original retained owner");
                var twin = factory.Load(game.Save());
                game.Clock.Pause(); c.Equal(0, game.Update(1000000), "paused sole clock earns no update"); game.Clock.Resume();
                for (int i = 0; i < 12; i++)
                {
                    game.Update(); twin.Update();
                    c.That(game.Fault == null && twin.Fault == null && game.Hash() == twin.Hash(), "saved interleaved continuation preserves one state/clock");
                    c.Equal(Read(game, own), Read(twin, own), "saved private facts match after actual update");
                }
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "read guards preserve canonical replay: " + replay.Failure + "/" + replay.Detail);
            }
            c.Equal(untouched, other.Hash(), "independent session is not affected by observations or continuation");
        }
        private static void InspectFaults(Check c)
        {
            Action<World, PlayerId, Entity>[] mutations = {
                (world, viewer, entity) => entity.Get<ViewObservationPart>().Value += Fix.One,
                (world, viewer, entity) => entity.Touch(),
                (world, viewer, entity) => ((GameClock)world.Clock).Pause(),
                (world, viewer, entity) => world.Spawn(world.Content.Get<EntityDef>("secret"), viewer),
                (world, viewer, entity) => throw new InvalidOperationException("proof.inspect.failure")
            };
            foreach (var mutation in mutations)
            {
                var game = ViewObservationProof.New(); var part = Own(game).Get<ViewObservationPart>(); part.OnInspect = mutation;
                var rows = new List<Facet> { new Facet(new Code("sentinel"), Fix.One) };
                c.Throws<InvalidOperationException>(() => game.Seat(0).View.Inspect(Visible(game), rows), "inspect mutation/exception never returns facts");
                c.Equal(0, rows.Count, "inspector's partial facet never reaches destination");
                c.That(game.Fault != null && game.Fault.Phase == "observation.provider", "inspector latches first provider fault");
                int reads = part.Reads; var fault = game.Fault;
                c.Equal(0, game.Update(), "faulted inspection cannot continue simulation");
                c.That(ReferenceEquals(fault, game.Fault) && part.Reads == reads && game.Snapshot.State == GameState.Faulted, "fault publication retains cause and never retries inspector");
                c.Throws<InvalidOperationException>(() => game.Save(), "partial inspection state is not resumable");
                c.Throws<InvalidOperationException>(() => game.Hash(), "partial inspection state has no settled hash");
            }
        }
        private static void VisibilityFaults(Check c)
        {
            for (int kind = 0; kind < 8; kind++)
            {
                var game = ViewObservationProof.New(); var visibility = Visibility(game); var view = game.Seat(0).View;
                var entities = new List<EntityView>(); var catalogue = new List<CatalogueEntry>(); var resources = new List<ResourceInfo>();
                if (kind <= 1)
                {
                    visibility.OnEntity = (world, viewer, entity) =>
                    { if (kind == 1) entity.Touch(); else if (entity.Owner != viewer) visibility.Value++; };
                    c.Throws<InvalidOperationException>(() => { if (kind == 1) view.CopyEntities(new PlayerId(0), entities); else view.CopyEntities(entities); }, "entity visibility detects provider/target mutation");
                    c.Equal(0, entities.Count, "later entity predicate cannot publish an earlier partial list");
                }
                else if (kind <= 4)
                {
                    visibility.OnContent = (world, viewer, definition) =>
                    { if (definition.Id == "secret") { if (kind == 4) throw new InvalidOperationException("proof.content.failure"); visibility.Value++; } };
                    c.Throws<InvalidOperationException>(() => { if (kind == 2) view.CopyCatalogue(catalogue); else view.CopyResources(resources); }, "typed content predicate mutation/exception refuses");
                    c.That(catalogue.Count == 0 && resources.Count == 0, "content copies publish no partial facts");
                }
                else
                {
                    var map = view.Feature<TileMapView>();
                    if (kind == 5)
                    {
                        int visited = 0;
                        visibility.OnEntity = (world, viewer, entity) =>
                        { visited++; if (entity.Owner != viewer) throw new InvalidOperationException("proof.spatial-area.failure"); };
                        entities.Add(default);
                        c.Throws<InvalidOperationException>(() => map.CopyEntities(new TileRect(0, 0, 16, 16), entities), "late spatial visibility callback refuses area publication");
                        c.That(visited == 2 && entities.Count == 0, "later visibility failure publishes no earlier matching entity prefix");
                    }
                    else if (kind == 6)
                    {
                        visibility.OnEntity = (world, viewer, entity) => throw new InvalidOperationException("proof.spatial-occupant.failure");
                        EntityView occupant = game.Seat(0).Snapshot.Entities.Single();
                        c.Throws<InvalidOperationException>(() => map.TryGetOccupantAt(new TileCoord(2, 2), out occupant), "occupant visibility callback cannot publish facts");
                        c.That(occupant.Id.Equals(default(EntityId)) && occupant.Spatial == null, "failed occupant clears prior entity output");
                    }
                    else
                    {
                        visibility.OnContent = (world, viewer, definition) => throw new InvalidOperationException("proof.spatial-footprint.failure");
                        var footprint = new TileRect(1, 1, 3, 3);
                        c.Throws<InvalidOperationException>(() => map.TryGetFootprint("actor", out footprint), "footprint content callback cannot publish geometry");
                        c.That(footprint.Equals(default(TileRect)), "failed footprint clears prior geometry output");
                    }
                }
                c.That(game.Fault != null && game.Fault.Phase == "observation.provider", "visibility own persisted codec is guarded");
                int calls = visibility.EntityReads + visibility.ContentReads; var fault = game.Fault;
                game.Update(); c.That(ReferenceEquals(fault, game.Fault) && calls == visibility.EntityReads + visibility.ContentReads, "faulted publication invokes no visibility callback again");
            }
        }
        private static void FactoryFaults(Check c)
        {
            for (int kind = 0; kind < 3; kind++)
            {
                int calls = 0;
                var module = new ViewObservationModule { OnFactory = (world, viewer) =>
                {
                    calls++;
                    if (kind == 0) ((GameClock)world.Clock).Pause();
                    else if (kind == 1) world.Spawn(world.Content.Get<EntityDef>("secret"), viewer);
                    else throw new InvalidOperationException("proof.factory.failure");
                } };
                var game = ViewObservationProof.New(ViewObservationProof.Factory(module));
                c.Equal(0, calls, "snapshot initialization does not eagerly create features");
                c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<ViewObservationReadModel>(), "lazy factory mutation/exception refuses feature");
                c.That(game.Fault != null && game.Fault.Phase == "observation.provider", "factory uses original observation fault owner");
                c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<ViewObservationReadModel>(), "failed lazy feature was not cached");
                c.Equal(1, calls, "faulted lazy read cannot retry poisoned factory");
            }
        }
        private static void AtomicFaultPublication(Check c)
        {
            var game = ViewObservationProof.New(); var other = ViewObservationProof.New(); ulong otherHash = other.Hash();
            var observer = game.Snapshot; var first = game.Seat(0).Snapshot; var second = game.Seat(1).Snapshot;
            var visibility = Visibility(game); int failingReads = 0, events = 0;
            visibility.OnEntity = (world, viewer, entity) => { if (viewer == new PlayerId(1)) { failingReads++; throw new InvalidOperationException("proof.last-viewer.failure"); } };
            game.Updated += update =>
            {
                events++;
                c.That(game.Snapshot.State == GameState.Faulted && game.Seat(0).Snapshot.State == GameState.Faulted && game.Seat(1).Snapshot.State == GameState.Faulted,
                    "Updated sees all viewers faulted, not a partial healthy publication");
                c.Equal(Fix.Zero, update.Alpha, "Updated time reflects cleared fault pacing");
            };
            c.Equal(1, game.Update(40000), "game step completes before last-viewer projection fault");
            c.That(game.Fault != null && game.Fault.Phase == "observation.provider" && game.Fault.Message.Contains("proof.last-viewer.failure"), "snapshot wrapper preserves original callback cause");
            c.Equal(1L, game.Tick, "projection fault does not roll back the actual game clock");
            foreach (var pair in new[] { (observer, game.Snapshot), (first, game.Seat(0).Snapshot), (second, game.Seat(1).Snapshot) })
            {
                c.That(pair.Item2.Revision == pair.Item1.Revision + 1 && pair.Item2.PreviousRevision == pair.Item1.Revision && pair.Item2.Viewer == pair.Item1.Viewer,
                    "fault publication keeps exact viewer baseline and one common revision");
                c.That(pair.Item2.Tick == pair.Item1.Tick && pair.Item2.Entities.Select(e => e.Version).SequenceEqual(pair.Item1.Entities.Select(e => e.Version)) &&
                    pair.Item2.Entities.Select(e => e.Id).SequenceEqual(pair.Item1.Entities.Select(e => e.Id)), "faulted facts retain previous safe epoch, not partially captured current state");
                c.That(pair.Item2.Spawned.Count == 0 && pair.Item2.Changed.Count == 0 && pair.Item2.Removed.Count == 0, "unsafe facts create no success deltas");
            }
            c.That(first.Entities.Count == 1 && second.Entities.Count == 1 && observer.Entities.Count == 2, "retained private projections cannot borrow observer facts");
            c.Equal(0L, game.Clock.PendingTicks, "projection fault discards unapplied pacing budget");
            var fault = game.Fault; c.Equal(0, game.Update(), "terminal projection never advances again");
            c.That(ReferenceEquals(fault, game.Fault) && failingReads == 1 && events == 2, "fault metadata republishes without retrying failed provider");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted snapshot cannot hide partial state in a save");
            c.Equal(otherHash, other.Hash(), "late projection fault leaves another session untouched");
        }
        private static List<WorkJobView> Jobs(SimGame game)
        { var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 2000 && watch.ElapsedMilliseconds < 20000 && !done(); i++)
            {
                game.Update(); twin?.Update();
                if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                    throw new InvalidOperationException(stage + "/time=" + game.Tick + "/fault=" + game.Fault + "/twin=" + twin?.Fault);
            }
            if (!done()) throw new InvalidOperationException(stage + "/bound=2000Updates/20s/time=" + game.Tick + "/jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
        }
        private static void Physical(Check c)
        {
            var factory = Colony.Factory(); var game = Colony.New(288);
            using (var recorder = game.RecordReplay(Limits))
            {
                var command = new ConstructCommand { Def = "tower", Priority = 50, Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) };
                ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "engine previews real physical construction");
                c.Equal(before, game.Hash(), "physical preview creates no work or observation mutation");
                var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "player construction enters existing engine queue");
                Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "real construction pickup");
                c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "loaded moving materials are not completed construction");
                var twin = factory.Load(game.Save());
                Until(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "delivered machine construction", twin);
                Fix steel = Fix.Zero;
                foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0)))
                {
                    var rows = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, rows);
                    steel += rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel").Aggregate(Fix.Zero, (sum, row) => sum + row.Value);
                }
                c.Equal(Fix.FromInt(20), steel, "guarded real inventory facts conserve fifty minus thirty steel");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "actual physical construction/read boundary replays: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "core view observation identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no second view-state codec");
            Registrations(c); Healthy(c); InspectFaults(c); VisibilityFaults(c); FactoryFaults(c); AtomicFaultPublication(c); Physical(c);
        }
    }
}
