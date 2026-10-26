using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class EntityConversionTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReservationOwner Owner = new ReservationOwner("proof.entity-conversion", 1);
        private static readonly Code Capability = new Code("transformation");
        private static readonly ResourceReservation[] Empty = Array.Empty<ResourceReservation>();
        public static void Register()
        {
            TestRunner.Add("Entity conversion: prepared contracts, pure result admission and scoped ownership", Contracts);
            TestRunner.Add("Entity conversion: timed results, saved waits and provider fault boundaries", Execution);
            TestRunner.Add("Creation admission: original source clock and space observation before ready or refused results", AdmissionObservation);
            TestRunner.Add("Creation lifecycle: direct failures and swallowed faults stop later callbacks", LifecycleCutoff);
            TestRunner.Add(CreationCleanupTests.Case, CreationCleanupTests.Run);
            TestRunner.Add(CreationCleanupTests.AllocationCase, CreationCleanupTests.Allocation);
            TestRunner.Add(CreationCleanupTests.InitializationCase, CreationCleanupTests.InitializationCheckpoints);
            TestRunner.Add(CreationCleanupTests.ClockAdmissionCase, CreationCleanupTests.ClockAdmission);
            TestRunner.Add(CreationCleanupTests.RemovalFaultCase, CreationCleanupTests.RemovalFaults);
            TestRunner.Add(CreationCleanupTests.RemovalAdmissionCase, CreationCleanupTests.RemovalAdmission);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static IResourceTransaction Transactions(SimGame game) => game.World.Services.Get<IResourceTransaction>();
        private static Entity Find(SimGame game, string id) => TransformerTests.Find(game, id);
        private static int Count(SimGame game, string id)
        { int count = 0; foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) count++; return count; }
        private static ResourceConversionIntent Convert(ResourceReservation[] consumed = null, ResourceProductionIntent[] outputs = null)
            => new ResourceConversionIntent(Owner, Company, (consumed ?? Empty).Select(record => new ResourceConversionInput(ResourceFlowKind.Consumed, record, record.Amount, false)),
                outputs ?? Array.Empty<ResourceProductionIntent>(), new Code("proof.result"));
        private static IEntityCreation Prepare(SimGame game, string result, EntityId source = default)
        {
            Code refusal = game.World.PrepareCreation(game.World.Content.Get<EntityDef>(result), Company, source, null, out var creation);
            if (!refusal.IsNone) throw new InvalidOperationException("proof creation admission: " + refusal);
            return creation;
        }
        private static void Tick(SimGame game, int count = 1)
        { for (int i = 0; i < count; i++) { game.Update(); if (game.Fault != null) throw new InvalidOperationException("entity conversion tick=" + game.Tick + ": " + game.Fault); } }
        private static ResourceFlowPage History(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0);
        private static GameFactory Endpoints(string fault = null)
        {
            var json = Json.Parse(FrameworkProofContent.Read("endpoints.json"));
            json.Get("entities").Add(Json.Parse("{\"id\":\"result\",\"kind\":\"entity\",\"components\":{}}"));
            if (fault != null) json.Get("entities")[2].Get("components").Set("creation_probe",
                Json.Parse("{\"mode\":\"" + fault + "\",\"participant\":\"source\"}"));
            return EndpointGame.Factory(content: Text(json));
        }
        private static SimGame NewEndpoints(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "endpoints", Seed = 91 });
        private static void Refuse(Check c, SimGame game, IEntityCreation creation, Code blocker, string reason)
        {
            ulong hash = game.Hash(); long history = History(game).Latest;
            c.Equal(blocker, Transactions(game).EvaluateConversion(Convert(), creation).Blocker, reason + " admission blocker");
            var result = Transactions(game).CommitConversion(Convert(), creation);
            c.That(!result.Committed && result.CreatedEntity == EntityId.None && result.ChangedResources == 0, reason + " no completion prefix");
            c.Equal(blocker, result.Blocker, reason + " fresh commit blocker");
            c.Equal(hash, game.Hash(), reason + " preserves identity and world state");
            c.Equal(history, History(game).Latest, reason + " no history");
        }
        private sealed class Forged : IEntityCreation
        {
            public EntityDef Definition => null; public PlayerId Owner => Company; public EntityId Source => default;
            public SpatialPlacement Placement => null; public EntityId Identity => new EntityId(3); public long Tick => 0;
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = Endpoints(); SimGame game = NewEndpoints(factory), other = NewEndpoints(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "completion semantics identify actual current source");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "only current save identity is accepted");
            World world = game.World; Entity source = Find(game, "source"); ulong before = game.Hash();
            IEntityCreation creation = Prepare(game, "result", source.Id);
            c.Equal(before, game.Hash(), "preparation allocates no identity/components/stock/events");
            c.Equal("result", creation.Definition.Id, "selected catalog prototype is retained");
            c.Equal(source.Id, creation.Source, "explicit source identity is retained");
            c.Equal(Company, creation.Owner, "explicit company is retained");
            c.Equal(game.Tick, creation.Tick, "intent belongs to current tick");
            c.Equal(3, creation.Identity.Value, "prospective identity uses the existing sole allocator");
            c.That(creation.Placement == null && world.Entities.Get(creation.Identity) == null, "preparation creates no ghost entity");
            c.That(Transactions(game).EvaluateConversion(Convert(), creation).Ready, "explicit entity-only result admits without invented materials");
            c.Equal(before, game.Hash(), "whole entity-only admission is pure");
            Refuse(c, game, Prepare(other, "result"), Codes.InvalidArgument, "cross-session intent");
            Refuse(c, game, new Forged(), Codes.InvalidArgument, "forged contract implementation");
            c.Equal(ResourceTransferStatus.InvalidRequest, Transactions(game).CommitConversion(Convert(), null).Status, "all-empty request without result is not conversion");
            c.Equal(Codes.UnknownDef, world.PrepareCreation(NewEndpoints(Endpoints()).World.Content.Get<EntityDef>("result"), Company, default, null, out _), "foreign catalog alias refuses");
            c.Equal(Codes.UnknownDef, world.PrepareCreation(null, Company, default, null, out _), "missing prototype refuses");
            c.Equal(Codes.NotSeated, world.PrepareCreation(creation.Definition, new PlayerId(99), default, null, out _), "unknown company refuses");
            c.Equal(Codes.NotFound, world.PrepareCreation(creation.Definition, Company, new EntityId(999), null, out _), "missing source refuses");
            c.Equal(Codes.InvalidArgument, world.PrepareCreation(creation.Definition, Company, new EntityId(-1), null, out _), "invalid optional source is not inferred");
            c.Equal(Codes.NotOwned, world.PrepareCreation(creation.Definition, new PlayerId(1), source.Id, null, out _), "foreign source refuses");
            c.Equal(Codes.InvalidArgument, world.PrepareCreation(creation.Definition, Company, default, new PointPlacement("foreign", Fix.One), out _), "nonspatial result rejects invented placement");
            world.PrepareCreation(creation.Definition, new PlayerId(1), default, null, out var foreignCompany);
            Refuse(c, game, foreignCompany, Codes.NotOwned, "conversion company must agree");
            Refuse(c, game, Prepare(game, "source"), ResourceTransactions.StockedCreation, "result opening stock must not grant uncounted resources");
            var done = Transactions(game).CommitConversion(Convert(), creation);
            c.That(done.Committed && done.CreatedEntity == creation.Identity && done.ChangedResources == 0, "entity-only completion returns the actual entity");
            c.That(ReferenceEquals(world.Entities.Get(done.CreatedEntity).Def, creation.Definition), "result is registered through native lifecycle");
            Tick(game);
            c.Equal(1, Count(game, "result"), "single result participates in normal entity Update");
            Refuse(c, game, creation, World.StaleCreation, "spent/tick-old intent");
            IEntityCreation stale = Prepare(game, "result"); Tick(game);
            Refuse(c, game, stale, World.StaleCreation, "uncommitted prior-tick intent");
            IEntityCreation sequence = Prepare(game, "result"); world.Spawn(creation.Definition, Company); world.Settle();
            Refuse(c, game, sequence, World.StaleCreation, "intervening real allocation");
            IEntityCreation lostSource = Prepare(game, "result", Find(game, "destination").Id);
            world.Despawn(Find(game, "destination")); world.Settle();
            Refuse(c, game, lostSource, Codes.NotFound, "removed explicit source");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "only real entities save, not ephemeral intent handles");
            Mixed(c, factory);
            c.Throws<ContentException>(() => TransformerTests.New(TransformerWipTests.Factory(json =>
                json.Get("entities")[1].Get("components").Set("process_stock", Json.Parse("{\"resource\":\"supply\",\"capacity_policy\":\"fixture.count.40\",\"initial\":1}")))),
                "authored conversion result opening stock is rejected before Initialize");
        }
        private static void Mixed(Check c, GameFactory factory)
        {
            SimGame game = NewEndpoints(factory); var source = Find(game, "source").Get<IInventory>();
            var pools = game.World.Services.Get<EndpointPools>(); var steel = game.World.Content.ResourceId("steel");
            var bill = new Bill(new[] { new ResourceAmount(steel, Fix.FromInt(3)) });
            var held = Transactions(game).AcquireClaims(Company, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, Owner, source.Endpoint, bill) }, System.Array.Empty<ResourceReservation>());
            c.That(held.Committed, "mixed result holds actual existing input receipts");
            var intent = Convert(new List<ResourceReservation>(held.Receipts).ToArray(), new[] {
                new ResourceProductionIntent(pools.Get(1).Endpoint, new Bill(new[] { new ResourceAmount(steel, Fix.One) })) });
            ulong hash = game.Hash(); long history = History(game).Latest;
            c.That(Transactions(game).EvaluateConversion(intent, Prepare(game, "result", Find(game, "source").Id)).Ready, "scoped output and actual result admit together");
            c.Equal(hash, game.Hash(), "mixed whole admission changes no stock/promise/ID");
            game.Seat(0).Submit(new ConversionProbeCommand { Intent = intent, Result = "result", Source = Find(game, "source").Id });
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "pending command saves stable prototype intent, not session handle");
            Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "fresh engine preparation after load produces identical result");
            c.Equal(1, Count(game, "result"), "mixed conversion creates exactly one real entity");
            c.Equal(Fix.FromInt(27), source.Amount(steel), "only actual input bill was consumed");
            c.Equal(Fix.One, pools.Get(1).Amount(steel), "scoped provider owns actual produced bill");
            c.Equal(history + 2, History(game).Latest, "success reports only fully verified consumed/produced facts");
            c.Equal(0, source.Reservations.Count, "completion retires the actual consumed input receipts");
            c.Equal(0, game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(1), 0).Entries.Count, "private completion history stays company-owned");
            Tick(game, 2); Tick(twin, 2); c.Equal(game.Hash(), twin.Hash(), "interleaved sessions retain separate creation state");
        }
        private static GameFactory Native(string fault = null, string resultFault = null, bool entityOnly = false)
            => TransformerWipTests.Factory(json =>
            {
                var transformation = json.Get("transformations")[0];
                if (entityOnly) {
                    transformation.Set("inputs", JsonValue.NewObject()); transformation.Set("outputs", JsonValue.NewObject());
                    transformation.Set("input_ports", JsonValue.NewObject()); transformation.Set("output_ports", JsonValue.NewObject());
                }
                else {
                    transformation.Get("tools").Set("supply", JsonValue.Number("2"));
                    transformation.Get("tool_ports").Set("supply", JsonValue.Of("tool.input"));
                }
                if (fault != null) json.Get("entities")[0].Get("components").Get("process_stock").Set("receipt_fault", JsonValue.Of(fault));
                if (resultFault != null) json.Get("entities")[1].Get("components").Set("creation_probe",
                    Json.Parse("{\"mode\":\"" + resultFault + "\",\"participant\":\"assembler\"}"));
            });
        private static void AdmissionObservation(Check c)
        {
            var root = Json.Parse(PointGame.Definitions);
            root.Get("entities").Add(Json.Parse("{\"id\":\"creation.result\",\"kind\":\"structure\",\"components\":{\"point_presence\":{\"space\":\"field.points\",\"position\":77}}}"));
            StartupContentAuthoring.Add(root, "creation.blocker", "creation.result", 0,
                Json.Parse("{\"space\":\"field.points\",\"position\":77}"));
            var factory = PointGame.Factory(Text(root));
            foreach (bool loaded in new[] { false, true })
            foreach (bool checking in new[] { false, true })
            foreach (bool blocked in new[] { false, true })
            foreach (string mode in new[] { "payload", "clock", "speed", "space", "random", "random-restore", "payload-throw", "throw" })
            {
                var game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 719 }); Tick(game);
                if (loaded) game = factory.Load(game.Save());
                var world = game.World; var space = (PointSpace)world.Space; Entity source = Find(game, "source");
                var definition = world.Content.Get<EntityDef>("creation.result"); var placement = new PointPlacement(space.Id, Fix.FromInt(78));
                ulong before = game.Hash();
                c.Equal(Code.None, world.PrepareCreation(definition, Company, source.Id, placement, out var intent), "ordinary actual placement prepares without allocation");
                c.Equal(before, game.Hash(), "healthy placement admission uses no stock, identity or clock prefix");
                if (blocked) Find(game, "creation.result").Get<PointPresence>().Move(world, placement.Position); // Explicit provider fixture setup.
                c.Equal(blocked ? Codes.Blocked : Code.None, world.CheckCreation(intent), "unarmed provider returns its actual current ready/blocked result");
                var independent = factory.Load(game.Save()); long independentAt = independent.Tick;
                var published = game.Seat(0).Snapshot; EntityId next = world.Entities.NextIdentity;
                var port = source.Get<SingleResourcePort>(); int payload = port.ServiceCount, version = source.Version;
                int calls = 0; IEntityCreation leaked = null; Exception caught = null;
                space.GeometryObservationFault = (phase, participant) => {
                    if (phase != "placement") return;
                    calls++;
                    if (mode.StartsWith("payload", StringComparison.Ordinal)) port.FaultServicePayload();
                    if (mode == "clock") game.Clock.Pause();
                    if (mode == "speed") game.Clock.Speed = 2;
                    if (mode == "space") space.SetAccess(!space.AccessOpen);
                    if (mode == "random") world.Random.NextULong();
                    if (mode == "random-restore") { ulong state = world.Random.State; world.Random.NextULong(); world.Random.State = state; }
                    if (mode.EndsWith("throw", StringComparison.Ordinal)) throw new InvalidOperationException("proof.creation-original-error");
                };
                try {
                    if (checking) world.CheckCreation(intent);
                    else world.PrepareCreation(definition, Company, source.Id, placement, out leaked);
                } catch (Exception error) { caught = error; }
                string context = mode + "/" + (checking ? "check" : "prepare") + "/" + (blocked ? "blocked" : "ready") + "/" + (loaded ? "loaded" : "new");
                c.That(caught is InvalidOperationException, "original placement callback refuses corrupted admission: " + context);
                if (mode.EndsWith("throw", StringComparison.Ordinal)) c.Equal("proof.creation-original-error", caught?.Message, "original callback exception is preserved: " + context);
                c.That(game.Fault != null, "direct creation admission latches the original terminal fault: " + context);
                c.That(leaked == null, "no successful prospective creation handle escapes: " + context);
                c.Equal(1, calls, "one actual provider callback, no retry or fallback: " + context);
                c.Equal(next, world.Entities.NextIdentity, "failed pure admission never allocates a result identity: " + context);
                c.Equal(1, Count(game, "creation.result"), "only the authored blocker exists, no registration prefix: " + context);
                c.That(!world.Events.HasPending, "failed admission queues no success prefix: " + context);
                c.Equal(published.Tick, game.Seat(0).Snapshot.Tick, "last healthy published snapshot remains available: " + context);
                if (mode.StartsWith("payload", StringComparison.Ordinal)) {
                    c.Equal(payload + 1, port.ServiceCount, "probe changes the original auxiliary payload, with no fake rollback: " + context);
                    c.Equal(version, source.Version, "payload mutation deliberately bypasses version-only detection: " + context);
                }
                c.Throws<InvalidOperationException>(() => game.Save(), "corrupt admission cannot save a healthy continuation: " + context);
                c.Throws<InvalidOperationException>(() => game.Hash(), "corrupt admission cannot advertise a healthy hash: " + context);
                if (game.Fault != null) {
                    c.Throws<InvalidOperationException>(() => world.PrepareCreation(null, Company, EntityId.None, null, out _), "later invalid creation stops at the first fault: " + context);
                    c.Equal(1, calls, "terminal admission never re-enters the provider: " + context);
                }
                Tick(independent);
                c.That(independent.Fault == null && independent.Tick > independentAt, "a separately restored session actually advances after the other session fails: " + context);
            }
        }
        private static void LifecycleCutoff(Check c)
        {
            var root = Json.Parse(FrameworkProofContent.Read("endpoints.json"));
            root.Get("entities").Add(Json.Parse("{\"id\":\"lifecycle.result\",\"kind\":\"entity\",\"components\":{\"creation_probe\":{\"mode\":\"observe\",\"participant\":\"source\"},\"creation_tail\":{}}}"));
            root.Get("entities").Add(Json.Parse("{\"id\":\"lifecycle.failure\",\"kind\":\"entity\",\"components\":{\"creation_probe\":{\"mode\":\"throw-initialize\",\"participant\":\"source\"}}}"));
            var factory = EndpointGame.Factory(content: Text(root));
            foreach (bool loaded in new[] { false, true })
            foreach (bool committing in new[] { false, true })
            {
                var healthy = NewEndpoints(factory); Tick(healthy); if (loaded) healthy = factory.Load(healthy.Save());
                EntityDef definition = healthy.World.Content.Get<EntityDef>("lifecycle.result");
                Entity positive = committing ? healthy.World.CommitCreation(Prepare(healthy, "lifecycle.result")) : healthy.World.Spawn(definition, Company);
                var tail = positive.Get<CreationTailProbe>();
                c.That(tail.Creates == 1 && tail.Initializations == 1, "healthy creation still invokes each original callback exactly once");
                healthy.World.Settle(); var saved = factory.Load(healthy.Save());
                c.Equal(healthy.Hash(), saved.Hash(), "ordinary healthy lifecycle keeps original saved identity/state");
                c.That(saved.World.Entities.Get(positive.Id).Get<CreationTailProbe>().Creates == 0 && saved.World.Entities.Get(positive.Id).Get<CreationTailProbe>().Initializations == 0,
                    "restore decodes original state and never reruns creation or grant callbacks");
                if (committing)
                {
                    foreach (string mode in new[] { "throw-create", "throw-initialize" })
                    {
                        var failureRoot = root.Clone(); var declarations = failureRoot.Get("entities");
                        for (int i = 0; i < declarations.Count; i++) if (declarations[i].Get("id").Text == "lifecycle.result")
                            declarations[i].Get("components").Get("creation_probe").Set("mode", JsonValue.Of(mode));
                        var failureFactory = EndpointGame.Factory(content: Text(failureRoot)); var failed = NewEndpoints(failureFactory); Tick(failed);
                        if (loaded) failed = failureFactory.Load(failed.Save());
                        var independent = failureFactory.Load(failed.Save()); long independentAt = independent.Tick;
                        var intent = Prepare(failed, "lifecycle.result"); Entity made = null; Exception caught = null;
                        try { made = failed.World.CommitCreation(intent); } catch (Exception error) { caught = error; }
                        string label = mode + "/commit/" + (loaded ? "loaded" : "new");
                        c.That(caught is InvalidOperationException && made == null, "direct commit withholds its result on actual lifecycle failure: " + label);
                        c.That(failed.Fault != null, "direct creation commit owns unexpected lifecycle failure: " + label);
                        c.Equal(mode == "throw-create" ? "entity.create" : "entity.initialize", failed.Fault?.Phase, "direct commit retains original lifecycle phase: " + label);
                        c.Equal(mode == "throw-create" ? "proof result OnCreate failure" : "proof result initialization failure", caught?.Message, "direct commit preserves the actual original exception: " + label);
                        Entity registered = failed.World.Entities.Get(intent.Identity);
                        if (mode == "throw-create") c.That(registered == null && failed.World.Entities.NextIdentity == intent.Identity,
                            "failed local creation allocates no registered result or consumed identity: " + label);
                        else c.That(registered != null && registered.Get<CreationTailProbe>().Creates == 1 && registered.Get<CreationTailProbe>().Initializations == 0,
                            "actual registered prefix remains terminal and later initialization never runs: " + label);
                        c.Throws<InvalidOperationException>(() => failed.Save(), "direct failed commit cannot checkpoint: " + label);
                        c.Throws<InvalidOperationException>(() => failed.Hash(), "direct failed commit cannot hash a healthy continuation: " + label);
                        if (failed.Fault != null) c.Throws<InvalidOperationException>(() => failed.World.Spawn(null, Company), "commit fault stops later invalid creation: " + label);
                        Tick(independent); c.That(independent.Fault == null && independent.Tick > independentAt, "separate saved commit session advances normally: " + label);
                    }
                    continue;
                }
                foreach (string phase in new[] { "configure", "create", "initialize" })
                foreach (bool swallowed in new[] { false, true })
                {
                    var game = NewEndpoints(factory); Tick(game); if (loaded) game = factory.Load(game.Save());
                    var world = game.World; definition = world.Content.Get<EntityDef>("lifecycle.result");
                    var independent = factory.Load(game.Save()); long independentAt = independent.Tick;
                    var published = game.Seat(0).Snapshot; Entity candidate = null, returned = null; Exception caught = null;
                    WorldFault original = null; int callbacks = 0;
                    Action fail = () => {
                        callbacks++;
                        if (!swallowed) throw new InvalidOperationException("proof.lifecycle-original-error");
                        try { world.Spawn(world.Content.Get<EntityDef>("lifecycle.failure"), Company); }
                        catch (InvalidOperationException) { original = world.Fault; }
                    };
                    Action<Entity> configure = entity => {
                        candidate = entity;
                        if (phase == "configure") fail();
                        else if (phase == "create") entity.Get<CreationProbe>().BeforeCreate = fail;
                        else entity.Get<CreationProbe>().BeforeInitialize = fail;
                    };
                    try {
                        returned = world.Spawn(definition, Company, configure);
                    } catch (Exception error) { caught = error; }
                    string context = phase + "/" + (swallowed ? "swallowed" : "throw") + "/spawn/" + (loaded ? "loaded" : "new");
                    c.That(caught is InvalidOperationException && returned == null, "no successful creation result after lifecycle failure: " + context);
                    c.That(game.Fault != null, "direct lifecycle owner latches terminal failure: " + context);
                    c.Equal(1, callbacks, "original failure callback runs once: " + context);
                    if (swallowed) c.That(original != null && ReferenceEquals(original, game.Fault), "swallowed inner failure retains the actual first fault: " + context);
                    else {
                        c.Equal("proof.lifecycle-original-error", caught?.Message, "unexpected direct callback keeps its original exception: " + context);
                        c.Equal(phase == "initialize" ? "entity.initialize" : "entity.create", game.Fault?.Phase, "original lifecycle phase owns failure: " + context);
                    }
                    c.That(candidate != null, "test observes the actual lifecycle candidate, not a fabricated model: " + context);
                    var later = candidate?.Get<CreationTailProbe>();
                    c.Equal(phase == "initialize" ? 1 : 0, later?.Creates ?? -1, "fault stops before later local creation callbacks: " + context);
                    c.Equal(0, later?.Initializations ?? -1, "fault stops before later capability initialization: " + context);
                    if (phase != "initialize") c.That(!ReferenceEquals(world.Entities.Get(candidate.Id), candidate), "failed pre-registration candidate never becomes the registered entity: " + context);
                    c.That(!world.Events.HasPending, "no success facts queued by failed lifecycle: " + context);
                    c.Equal(published.Tick, game.Seat(0).Snapshot.Tick, "last healthy publication retained: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "failed lifecycle cannot checkpoint a healthy continuation: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "failed lifecycle cannot present a healthy hash: " + context);
                    if (game.Fault != null) c.Throws<InvalidOperationException>(() => world.Spawn(null, Company, _ => callbacks++), "terminal fault precedes invalid arguments and later callbacks: " + context);
                    Tick(independent); c.That(independent.Fault == null && independent.Tick > independentAt, "separate restored session advances normally: " + context);
                }
            }
        }
        private static void Faulted(Check c, SimGame game, ResourceFlowPage healthy, string reason)
        {
            c.Equal(GameState.Faulted, game.State, reason + " faults authoritative Update");
            c.Throws<InvalidOperationException>(() => History(game), reason + " cannot expose live success history after a terminal fault");
            c.That(!healthy.Entries.Any(entry => entry.Item.Kind == ResourceFlowKind.Consumed || entry.Item.Kind == ResourceFlowKind.Produced),
                reason + " retained detached last-healthy facts acquire no conversion prefix");
            c.That(!game.World.Events.HasPending, reason + " queues no success prefix");
            c.Throws<InvalidOperationException>(() => game.Save(), reason + " cannot save partial mutation");
            c.Throws<InvalidOperationException>(() => game.Hash(), reason + " cannot checkpoint hash partial mutation");
        }
        private static void Execution(Check c)
        {
            GameFactory factory = Native(); SimGame game = TransformerTests.New(factory); Entity machine = Find(game, "assembler");
            IInventory stock = machine.Get<IInventory>(); var supply = game.World.Content.ResourceId("supply");
            c.That(TransformerTests.Queue(game, "assembler", "build", repeat: true).Accepted, "player queues timed mixed result and repeat");
            c.That(Count(game, "token") == 0 && TransformerTests.State(game).Progress.WholeTicks == 1, "no eager entity while work advances");
            c.Equal(Fix.FromInt(2), stock.ReservedOut(supply), "tools are actual held stock throughout processing");
            stock.Add(supply, Fix.FromInt(23)); // Capacity test setup, not completion logic.
            Tick(game, 5); var state = TransformerTests.State(game);
            c.That(state.Phase == TransformationPhase.Blocked && state.Blocker == TransformerManager.BlockerOutput, "resource output blocks the whole result");
            c.Equal(0, Count(game, "token"), "blocked capacity cannot allocate result entity");
            c.Equal(6L, state.Progress.WholeTicks, "complete physical work is retained during refusal");
            c.Equal(Fix.FromInt(3), state.Wip.Stock.AmountOf(supply), "blocked conversion keeps actual WIP");
            c.Equal(Fix.FromInt(2), stock.ReservedOut(supply), "blocked conversion keeps actual tools");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved wait restores existing queue/receipts/progress");
            Tick(game, 3); Tick(twin, 3); c.Equal(game.Hash(), twin.Hash(), "saved wait is deterministic and creates no prefix");
            stock.Take(supply, Fix.One); Find(twin, "assembler").Get<IInventory>().Take(supply, Fix.One);
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "released capacity commits entity and resources identically");
            c.Equal(1, Count(game, "token"), "one mixed completion creates one actual result");
            Tick(game); Tick(twin);
            c.That(TransformerTests.State(game).Wip.Execution == 2 && Count(game, "token") == 1, "repeat starts a fresh timed execution, not reused creation intent");
            c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = machine.Id, Capability = Capability, Index = 0 }).Accepted, "ordinary cancel returns repeat WIP and tools");
            Tick(game); c.Equal(1, Count(game, "token"), "cancellation creates no extra result");
            c.Equal(0, stock.Reservations.Count, "cancel releases actual repeat tools");
            SimGame only = TransformerTests.New(Native(entityOnly: true));
            c.That(TransformerTests.Queue(only, "assembler", "build", count: 2).Accepted, "entity-only authored queue uses same engine protocol");
            Tick(only, 5); c.Equal(1, Count(only, "token"), "entity-only result still takes full game time");
            Tick(only, 6); c.Equal(2, Count(only, "token"), "sequential result obtains fresh identity and full next work duration");
            SimGame independent = TransformerTests.New(Native(entityOnly: true));
            c.That(TransformerTests.Queue(independent, "emitter", "pulse").Accepted, "independent contract provider uses same result protocol");
            c.Equal(0, Count(independent, "token"), "independent result is not an instant host completion");
            Tick(independent); c.Equal(1, Count(independent, "token"), "independent provider returns actual committed entity after authored work");
            foreach (string fault in new[] { "partial_receive", "ignore_release" })
            {
                SimGame broken = TransformerTests.New(Native(fault: fault));
                c.That(TransformerTests.Queue(broken, "assembler", "build").Accepted, "fault setup starts actual timed work: " + fault);
                ResourceFlowPage history = History(broken); broken.RunTicks(5); Faulted(c, broken, history, fault);
                c.Equal(0, Count(broken, "token"), "failed resource completion never spawns result: " + fault);
            }
            foreach (string fault in new[] { "throw-create", "throw-initialize", "change-participant" })
            {
                SimGame broken = TransformerTests.New(Native(resultFault: fault));
                c.That(TransformerTests.Queue(broken, "assembler", "build").Accepted, "result initialization fault setup has actual timed startup: " + fault);
                ResourceFlowPage history = History(broken); broken.RunTicks(5); Faulted(c, broken, history, fault);
            }
            Spatial(c);
            SpatialWait(c);
        }
        private static void SpatialWait(Check c)
        {
            var json = Json.Parse(PointGame.Definitions);
            JsonValue processor = Json.Parse(FrameworkProofContent.Read("process.json")).Get("entities")[0].Get("components").Get("transformer");
            processor.Get("states").Set("working", JsonValue.Of("processing"));
            json.Get("entities").Add(Json.Parse("{\"id\":\"assembler\",\"kind\":\"entity\",\"components\":{\"point_presence\":{\"space\":\"field.points\",\"position\":10},\"point_stock\":{\"resource\":\"supply\",\"capacity_policy\":\"fixture.count.40\",\"initial\":20}}}"));
            json.Get("entities")[3].Get("components").Set("transformer", processor);
            json.Get("entities")[3].Get("components").Set("inventory_port", Json.Parse("[{\"id\":\"material\"},{\"id\":\"tools\"}]"));
            json.Get("entities").Add(Json.Parse("{\"id\":\"token\",\"kind\":\"entity\",\"components\":{\"point_presence\":{\"space\":\"field.points\",\"position\":77}}}"));
            json.Set("transformations", Json.Parse("[{\"id\":\"build\",\"mode\":\"batch\",\"cancel_policy\":\"return_inputs\",\"operating_inputs\":[],\"operating_ports\":{},\"input_ports\":{\"supply\":\"material\"},\"output_ports\":{\"supply\":\"material\"},\"tool_ports\":{\"supply\":\"tools\"},\"tool_consumption\":{},\"inputs\":{\"supply\":3},\"outputs\":{\"supply\":1},\"tools\":{\"supply\":2},\"entity_output\":\"token\",\"output_home\":\"unassigned\",\"duration\":{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"}}]"));
            StartupContentAuthoring.Add(json, "assembler", "assembler", 0, json.Get("entities")[3].Get("components").Get("point_presence"));
            GameFactory factory = PointGame.Factory(Text(json)); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 11 });
            c.That(TransformerTests.Queue(game, "assembler", "build", count: 2).Accepted, "ordinary point queue starts physical result work");
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "ordinary command closes actual rollout access");
            Tick(game, 4); var state = TransformerTests.State(game);
            c.That(state.Phase == TransformationPhase.Blocked && state.Blocker == Codes.Blocked, "completed work retains typed placement refusal");
            c.Equal(0, Count(game, "token"), "refused rollout has no eager result");
            c.Equal(Fix.FromInt(3), state.Wip.Stock.AmountOf(game.World.Content.ResourceId("supply")), "spatial wait retains actual WIP");
            c.Equal(Fix.FromInt(2), state.Tools.Held.AmountOf(game.World.Content.ResourceId("supply")), "spatial wait retains actual tool claims");
            ulong hash = game.Hash();
            c.Equal(Codes.Blocked, game.Seat(0).View.Feature<TransformationManagementView>().PreviewEntityOutput(Find(game, "assembler").Id, Capability, "build").Refusal, "management preview uses current provider blocker");
            c.Equal(hash, game.Hash(), "blocked preview mutates no stock/identity/geometry");
            SimGame twin = factory.Load(game.Save()); Tick(game, 2); Tick(twin, 2);
            c.Equal(game.Hash(), twin.Hash(), "saved placement wait continues identically");
            TestKit.Order(game, 0, new SetPointAccessCommand { Open = true }); TestKit.Order(twin, 0, new SetPointAccessCommand { Open = true });
            c.Equal(game.Hash(), twin.Hash(), "opening access commits the same resource/entity result");
            c.Equal(1, Count(game, "token"), "completed head releases one result only");
            c.Equal(Fix.FromInt(11), Find(game, "token").Get<PointPresence>().Position, "timed result uses actual provider-selected position");
            Tick(game, 6); Tick(twin, 6);
            c.Equal(2, Count(game, "token"), "second result requires a separate full physical batch");
            c.Equal(game.Hash(), twin.Hash(), "sequential spatial results keep deterministic saved identity");
        }
        private static void Spatial(Check c)
        {
            var json = Json.Parse(PointGame.Definitions);
            json.Get("entities").Add(Json.Parse("{\"id\":\"result\",\"kind\":\"entity\",\"components\":{\"point_presence\":{\"space\":\"field.points\",\"position\":77}}}"));
            GameFactory factory = PointGame.Factory(Text(json)); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 11 });
            Entity source = Find(game, "source"); EntityDef result = game.World.Content.Get<EntityDef>("result");
            ulong hash = game.Hash();
            c.Equal(Codes.InvalidArgument, game.World.PrepareCreation(result, Company, source.Id, null, out _), "spatial result requires explicit provider placement");
            c.Equal(Codes.InvalidArgument, game.World.PrepareCreation(result, Company, source.Id, new PointPlacement("wrong", Fix.One), out _), "wrong geometry identity refuses without fallback");
            c.Equal(Code.None, EntityRollout.Find(game.World, source, result, out var placement), "selected point provider chooses actual rollout");
            c.Equal(Code.None, game.World.PrepareCreation(result, Company, source.Id, placement, out var creation), "point result prepares through provider contract");
            c.Equal(hash, game.Hash(), "point preparation is pure and does not occupy authored prototype position");
            c.That(Transactions(game).CommitConversion(Convert(), creation).Committed, "non-tile creation-only result shares complete conversion");
            Tick(game); c.Equal(((PointPlacement)placement).Position, Find(game, "result").Get<PointPresence>().Position, "actual result uses selected rollout, not prototype position 77");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "point result saves only actual spatial state");
            SimGame hostile = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 11 });
            var pointSpace = (PointSpace)hostile.World.Space; Entity hostSource = Find(hostile, "source");
            pointSpace.GeometryObservationFault = (phase, participant) => { if (phase == "placement") hostSource.Touch(); };
            c.Throws<InvalidOperationException>(() => hostile.World.PrepareCreation(hostile.World.Content.Get<EntityDef>("result"),
                Company, hostSource.Id, new PointPlacement(pointSpace.Id, Fix.One), out _), "pure creation placement rejects actual source mutation");
            SimGame rolloutHostile = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 11 });
            ((PointSpace)rolloutHostile.World.Space).MutateRolloutSource = true;
            c.Throws<InvalidOperationException>(() => EntityRollout.Find(rolloutHostile.World, Find(rolloutHostile, "source"), rolloutHostile.World.Content.Get<EntityDef>("result"), out _),
                "pure rollout rejects actual producer mutation");
            var colony = Colony.Factory(); SimGame tiles = colony.NewGame(new NewGameRequest { Scenario = "colony", Seed = 1 });
            Entity producer = Find(tiles, "factory"); var truck = tiles.World.Content.Get<EntityDef>("truck"); hash = tiles.Hash();
            c.Equal(Code.None, EntityRollout.Find(tiles.World, producer, truck, out placement), "existing tile provider chooses actual result footprint");
            c.Equal(Code.None, tiles.World.PrepareCreation(truck, Company, producer.Id, placement, out creation), "tile result uses same kernel intent");
            c.Equal(hash, tiles.Hash(), "tile preparation creates no footprint or identity prefix");
            c.That(Transactions(tiles).CommitConversion(Convert(), creation).Committed, "tile result commits through same conversion owner");
            Tick(tiles); c.Equal(tiles.Hash(), colony.Load(tiles.Save()).Hash(), "tile result retains normal entity/space codec");
        }
    }
}
