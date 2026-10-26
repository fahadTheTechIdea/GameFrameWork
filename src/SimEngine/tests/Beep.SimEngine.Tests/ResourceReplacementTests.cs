using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    public static class ResourceReplacementTests
    {
        internal const string ProviderPreparationCase = "Replacement preparation: existing entity and service resources stay read-only across local callbacks";
        public static void Register()
        {
            TestRunner.Add("Resource replacement: native stock, real receipts, private history and saved identity", Accounting);
            TestRunner.Add("Resource replacement: timed geometry, auxiliary state, refusals and fault checkpoints", Lifecycle);
            TestRunner.Add("Replacement preflight: hidden payload clock space and callback failure stop before identity commit", Observation);
            TestRunner.Add("Replacement preparation: original detached codecs and whole-vector resource relocation", Preparation);
            TestRunner.Add("Replacement retention: source state clock space and local preparation failures preserve ownership", Retention);
            TestRunner.Add(ProviderPreparationCase, ProviderPreparation);
        }
        private static string Json => FrameworkProofContent.Read("resource-replacement.json");
        private static void ProviderPreparation(Check c)
        {
            // Explicit negative lifecycle/protocol lens. Resource setup uses ordinary physical player commands.
            var factory = LogisticsReferenceReplacementTests.Factory(false, true, "stable", edit: root =>
            {
                foreach (int index in new[] { 1, 3 })
                {
                    var parts = root.Get("entities")[index].Get("components");
                    parts.Set("creation_probe", Content.Json.Parse("{\"mode\":\"observe\",\"participant\":\"destination\"}"));
                    parts.Set("creation_tail", Content.Json.Parse("{}"));
                }
                StartupContentAuthoring.Position(root, "parking", JsonValue.Number("0"));
            });
            var setup = factory.NewGame(Request());
            EntityId Id(SimGame session, string name) => session.Seat(0).Snapshot.Entities.Single(entity => entity.Def == name).Id;
            var sourceId = Id(setup, "source"); var oldId = Id(setup, "destination"); var machine = Id(setup, "machine");
            if (!setup.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("preparation probe supply missing");
            void Submit(Command command)
            { var answer = TestKit.Order(setup, 0, command); if (!answer.Accepted) throw new InvalidOperationException("physical preparation setup refused " + answer.Refusal); }
            var stock = new Code("stock");
            Submit(new ConfigureStockPolicyCommand { Store = oldId, Port = stock, Resource = resource,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.FromInt(2), Fix.FromInt(4), Fix.FromInt(8), 47, LogisticsReserveAccess.Protected)) });
            Submit(new AddRouteCommand { From = sourceId, To = oldId, SourcePort = stock, DestinationPort = stock, Resource = resource,
                Settings = new LogisticsRouteSettings(false, Fix.FromInt(5), 19) });
            Submit(new HaulResourcesCommand { Source = sourceId, Destination = oldId, SourcePort = stock, DestinationPort = stock, Resource = resource,
                Ceiling = Fix.FromInt(5), Worker = machine, Priority = 90 });
            bool Ready()
            {
                var rows = new List<WorkJobView>(); setup.Seat(0).View.Feature<WorkView>().CopyJobs(rows);
                Code check = setup.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(oldId, resource, out var facts, stock);
                return check.IsNone && facts.Stock == Fix.FromInt(5) && !rows.Any(job => job.State == JobState.Open || job.State == JobState.Assigned || job.State == JobState.Paused);
            }
            for (int i = 0; i < 160 && !Ready(); i++) Tick(setup);
            if (!Ready()) throw new InvalidOperationException("physical preparation pool fill exceeded160Updates");
            Submit(new SetHomeCommand { Worker = machine, Home = sourceId });
            bool Home() => setup.Seat(0).View.Feature<WorkView>().ReadHome(machine, out var home).IsNone && home.State == WorkerHomeState.AtHome;
            for (int i = 0; i < 160 && !Home(); i++) Tick(setup);
            if (!Home()) throw new InvalidOperationException("physical preparation carrier return exceeded160Updates");
            var saved = setup.Save();
            var modes = new[] { "healthy", "throw", "pool-take", "pool-claim", "pool-take-throw", "pool-claim-throw", "peer-take", "peer-claim", "peer-take-throw", "peer-claim-throw", "pending-claim", "pending-claim-throw" };
            foreach (string stage in new[] { "create", "retain" })
            foreach (string mode in modes)
            {
                var game = factory.Load(saved); var world = game.World; var old = world.Entities.Get(oldId);
                var peer = world.Entities.Get(sourceId).Get<IInventory>(); var pool = world.Services.Get<EndpointPools>().Get(1);
                c.That(peer.Amount(resource) == Fix.FromInt(15) && pool.Amount(resource) == Fix.FromInt(5), "probe starts with genuinely hauled5 from finite20");
                var isolated = factory.Load(saved); ulong untouched = isolated.Hash(), before = game.Hash();
                Entity pending = null;
                if (mode.StartsWith("pending-", StringComparison.Ordinal))
                {
                    pending = world.Spawn(world.Content.Get<EntityDef>("parking"), new PlayerId(0),
                        entity => ((ISpatialPlacement)world.Space).ConfigurePlacement(entity, new PointPlacement(world.Space.Id, Fix.FromInt(80))));
                    c.That(ReferenceEquals(world.Entities.Get(pending.Id), pending) && !world.Entities.All.Contains(pending),
                        "pending resource owner is really registered but absent from deferred execution list");
                }
                EntityId next = world.Entities.NextIdentity; var published = game.Seat(0).Snapshot;
                bool pendingEvents = world.Events.HasPending;
                Entity candidate = null, made = null; Exception caught = null; int calls = 0;
                var primary = new InvalidOperationException("proof.replacement-preparation-primary");
                Action attack = () =>
                {
                    calls++;
                    IResourceOutlet participant = mode.StartsWith("pool-", StringComparison.Ordinal) ? pool : peer;
                    if (mode.Contains("-take", StringComparison.Ordinal))
                    { if (participant.Take(resource, Fix.One) != Fix.One) throw new InvalidOperationException("actual prepared probe stock missing"); }
                    if (mode.Contains("-claim", StringComparison.Ordinal))
                    {
                        var owner = new ReservationOwner("proof.preparation", 1);
                        if (pending != null) pending.Get<IInventory>().ReserveIn(owner, resource, Fix.One);
                        else participant.ReserveOut(owner, resource, Fix.One);
                    }
                    if (mode == "throw" || mode.EndsWith("-throw", StringComparison.Ordinal)) throw primary;
                };
                ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) =>
                {
                    if (phase != "configure" || entity?.Def.Id != "source.next") return;
                    candidate = entity;
                    if (stage == "create") entity.Get<CreationProbe>().BeforeCreate = attack;
                    else entity.Get<CreationProbe>().BeforeRetain = attack;
                };
                try { Upgrades.TryReplace(world, old, world.Content.Get<EntityDef>("source.next"), out made); }
                catch (Exception error) { caught = error; }
                string context = stage + "/" + mode;
                c.Equal(1, calls, "one actual private callback, not a retry " + context);
                if (mode == "healthy")
                {
                    c.That(caught == null && game.Fault == null && made != null, "healthy candidate-local preparation is still permitted " + context);
                    c.That(candidate.Get<CreationTailProbe>().Creates == 1 && candidate.Get<CreationTailProbe>().Retentions == 1, "healthy later local phases execute exactly once " + context);
                    world.Settle(); Tick(game);
                    c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "healthy prepared replacement saves normally " + context);
                }
                else if (mode == "throw")
                {
                    c.That(ReferenceEquals(primary, caught) && game.Fault == null && made == null, "pure local failure preserves original exception and healthy world " + context);
                    c.Equal(before, game.Hash(), "pure local failure consumes no world state or identity " + context);
                }
                else
                {
                    c.That(caught != null && game.Fault != null, "external prepared mutation faults instead of publishing replacement " + context);
                    c.Equal("entity.replace", game.Fault?.Phase, "sole replacement boundary owns external preparation violation " + context);
                    if (mode.EndsWith("-throw", StringComparison.Ordinal))
                        c.That(ReferenceEquals(primary, caught) && game.Fault?.Message.Contains(primary.Message, StringComparison.Ordinal) == true, "original callback cause survives verification " + context);
                    c.That(made == null && old.Alive && ReferenceEquals(world.Entities.Get(old.Id), old) && world.Entities.NextIdentity == next && world.Entities.Get(next) == null,
                        "rejected private candidate consumes no identity or source retirement " + context);
                    c.That(candidate != null && !candidate.Alive && candidate.Get<CreationTailProbe>().Creates == (stage == "create" ? 0 : 1) && candidate.Get<CreationTailProbe>().Retentions == 0,
                        "no later creation or retention callback runs after external prefix " + context);
                    c.That(world.Events.HasPending == pendingEvents && game.Seat(0).Snapshot.Tick == published.Tick, "preparation publishes no replacement success " + context);
                    c.Equal(mode.StartsWith("pool-take", StringComparison.Ordinal) ? Fix.FromInt(4) : Fix.FromInt(5), pool.Amount(resource), "actual pool prefix is diagnosed, not fabricated rollback " + context);
                    c.Equal(mode.StartsWith("peer-take", StringComparison.Ordinal) ? Fix.FromInt(14) : Fix.FromInt(15), peer.Amount(resource), "actual peer prefix is diagnosed, not fabricated rollback " + context);
                    c.That(pending != null ? pending.Get<IInventory>().Reservations.Count == 1 && pending.Get<IInventory>().Reservations[0].Direction == ReservationDirection.Inbound :
                        mode.Contains("-claim", StringComparison.Ordinal) ? (mode.StartsWith("pool-", StringComparison.Ordinal) ? pool : (IResourceOutlet)peer).Reservations.Count == 1 : pool.Reservations.Count == 0 && peer.Reservations.Count == 0,
                        "actual claim prefix remains visible only as fault diagnostics " + context);
                    if (game.Fault == null) world.Settle(); // Counterexample may really publish; never make a pending-event failure look like fault protection.
                    c.Throws<InvalidOperationException>(() => game.Save(), "mutated preparation cannot save healthy " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "mutated preparation cannot hash healthy " + context);
                    if (game.Fault != null)
                    {
                        var first = game.Fault; c.Equal(Codes.NotAllowed, Upgrades.Check(world, old, old.Def), "terminal preparation refuses later replacement " + context);
                        c.That(ReferenceEquals(first, game.Fault), "first fault remains immutable " + context);
                    }
                }
                c.Equal(untouched, isolated.Hash(), "separate restored session has no preparation prefix " + context);
            }
            // Positive public-player continuation: another access site owns the live haul, not the replacing structure.
            var live = factory.Load(saved); SimGame twin; var alias = Id(live, "parking");
            void SendLive(SimGame session, Command command)
            { var answer = TestKit.Order(session, 0, command); if (!answer.Accepted) throw new InvalidOperationException("live replacement claim command refused " + answer.Refusal); }
            var shipment = new HaulResourcesCommand { Source = alias, Destination = sourceId, SourcePort = stock, Resource = resource,
                Ceiling = Fix.FromInt(2), Worker = machine, Priority = 90 };
            SendLive(live, shipment);
            IReadOnlyList<ResourceReservation> records;
            c.That(live.Seat(0).View.Feature<InventoryView>().TryGetReservations(oldId, stock, out records), "actual shared pool exposes owned pre-existing haul claim");
            var held = records.Single(record => record.Direction == ReservationDirection.Outbound);
            c.That(held.Amount == Fix.FromInt(2) && held.Token.Endpoint == new ResourceEndpointId("proof.pools", 1), "ordinary alias shipment reserves two real shared units");
            SendLive(live, new PostOrderCommand { Target = oldId, Plan = "upgrade" }); twin = factory.Load(live.Save());
            void StepLive()
            {
                Tick(live); Tick(twin); c.Equal(live.Hash(), twin.Hash(), "live-claim replacement sessions remain identical each physical update");
                var sourceRead = live.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(sourceId, resource, out var sourceFacts);
                var poolSite = live.Seat(0).Snapshot.Entities.Any(entity => entity.Id == oldId) ? oldId : Id(live, "source.next");
                var poolRead = live.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(poolSite, resource, out var poolFacts, stock);
                var cargoRead = live.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(machine, resource, out var cargoFacts);
                c.That(sourceRead.IsNone && poolRead.IsNone && cargoRead.IsNone && sourceFacts.Stock + poolFacts.Stock + cargoFacts.Stock == Fix.FromInt(20),
                    "live claim, private preparation and physical delivery conserve original20");
            }
            for (int i = 0; i < 25 && !live.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "source.next"); i++) StepLive();
            var replacement = Id(live, "source.next");
            c.That(live.Seat(0).View.Feature<InventoryView>().TryGetReservations(replacement, stock, out records) && records.Any(record => record.Equals(held)),
                "valid replacement preserves another site's exact live receipt token owner direction and amount");
            live = factory.Load(live.Save()); twin = factory.Load(twin.Save());
            bool Loaded()
            {
                var jobs = new List<WorkJobView>(); live.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                return jobs.Any(job => job.IsHaul && job.Source == alias && job.Destination == sourceId && job.ToilIndex == 2 && job.Loaded == Fix.FromInt(2) &&
                    live.Seat(0).Snapshot.Entities.Single(entity => entity.Id == machine).Spatial is PointSnapshot at && at.Position > Fix.One && at.Position < Fix.FromInt(19));
            }
            for (int i = 0; i < 160 && !Loaded(); i++) StepLive();
            c.That(Loaded(), "same pre-existing claim physically loads after replacement and travels inside original route");
            live = factory.Load(live.Save()); twin = factory.Load(twin.Save());
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            using (var recorder = live.RecordReplay(limits))
            {
                bool Delivered() => live.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(sourceId, resource, out var facts).IsNone && facts.Stock == Fix.FromInt(17);
                for (int i = 0; i < 160 && !Delivered(); i++) StepLive();
                c.That(Delivered(), "original alias haul delivers both units without recreating or redirecting its claim");
                c.Equal(0L, recorder.CommandCount, "saved loaded continuation resubmits no shipment or replacement");
                var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
                c.That(verified.Success && verified.ActualHash == live.Hash(), "actual live-claim replacement continuation replays identically");
            }
            PreparedBindingChanges(c);
        }
        private static void PreparedBindingChanges(Check c)
        {
            // Explicit negative publication lens: use the real port capability to violate/lose a prepared binding.
            foreach (bool malformed in new[] { false, true })
            {
                var factory = LogisticsReferenceReplacementTests.Factory(false, true, malformed ? "stable" : "current");
                var game = factory.NewGame(Request()); var world = game.World;
                var old = Find(game, "destination"); var source = Find(game, "source"); var stock = new Code("stock");
                if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("prepared binding resource missing");
                var policy = new LogisticsDemandSettings(false, Fix.FromInt(2), Fix.FromInt(4), Fix.FromInt(8), 47, LogisticsReserveAccess.Protected);
                var configured = TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = old.Id, Port = stock, Resource = resource, Change = LogisticsDemandChange.Replace(policy) });
                var routed = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = old.Id, SourcePort = stock, DestinationPort = stock, Resource = resource,
                    Settings = new LogisticsRouteSettings(false, Fix.FromInt(5), 19) });
                c.That(configured.Accepted && routed.Accepted, "prepared binding fixture uses normal policy/route commands");
                var logistics = world.Services.Get<LogisticsService>(); var route = logistics.Routes.Single(); var demand = logistics.Demands.Single();
                c.Equal(Code.None, Upgrades.Check(world, old, world.Content.Get<EntityDef>("source.next")), "original source and authored candidate really admit before late binding change");
                EntityId next = world.Entities.NextIdentity; Entity candidate = null, made = null; Exception caught = null;
                ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) =>
                {
                    if (phase != "spawn" || entity?.Def.Id != "source.next") return;
                    candidate = entity; entity.Get<IResourcePortSelection>(stock).Select(world, new ResourceEndpointId("proof.pools", 2));
                };
                try { Upgrades.TryReplace(world, old, world.Content.Get<EntityDef>("source.next"), out made); }
                catch (Exception error) { caught = error; }
                c.That(caught != null && game.Fault != null && made == null, "malformed or changed prepared port cannot become healthy replacement");
                c.Equal(malformed ? "observation.provider" : "entity.replace", game.Fault?.Phase, "first actual binding/codec owner reports late failure");
                c.That(game.Fault?.Message.Contains(malformed ? "unknown selected authored port pool" : ResourcePortCodes.OriginalUnavailable.Id, StringComparison.Ordinal) == true,
                    "late selected/original binding cause is preserved");
                c.That(candidate != null && candidate.Alive && ReferenceEquals(world.Entities.Get(next), candidate) && old.Alive && world.Entities.NextIdentity.Value == next.Value + 1,
                    "late publication fault retains actual interrupted registration, not fabricated rollback");
                c.That(logistics.Routes.Single().From == route.From && logistics.Routes.Single().To == route.To && logistics.Routes.Single().CreatedTick == route.CreatedTick &&
                    logistics.Demands.Single().Store == demand.Store && logistics.Demands.Single().Source.Equals(demand.Source) && logistics.Demands.Single().Endpoint == demand.Endpoint && logistics.Demands.Single().Settings.Matches(policy),
                    "all actual binding checks precede this owner's persistent route and policy writes");
                c.That(!world.Events.HasPending && source.Get<IInventory>().Amount(resource) == Fix.FromInt(20) && world.Services.Get<EndpointPools>().Get(1).Amount(resource) == Fix.Zero,
                    "failed prepared binding emits no replacement success or stock copy");
                c.Throws<InvalidOperationException>(() => game.Save(), "late binding fault cannot save healthy");
                c.Throws<InvalidOperationException>(() => game.Hash(), "late binding fault cannot hash healthy");
            }
        }
        private static GameFactory PointFactory(string definitions)
            => PointGame.Factory(definitions, handlingProfiles: FrameworkProofContent.Read("point-handling.json").Replace("\"supply\"", "\"alpha\""));
        private static NewGameRequest Request(bool flows = true, string scenario = "points")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 127 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            if (flows) request.Mods.Add(ResourceFlowsModule.ModuleId);
            return request;
        }
        private static Entity Find(SimGame game, string id, int owner = 0)
        {
            foreach (Entity entity in game.World.Entities.All)
                if (entity.Alive && entity.Def.Id == id && entity.Owner == new PlayerId(owner)) return entity;
            throw new InvalidOperationException("missing replacement fixture " + id);
        }
        private static ResourceFlowPage Read(SimGame game, long cursor = 0)
            => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor);
        private static void Tick(SimGame game)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("replacement tick " + game.Tick + ": " + game.Fault);
        }
        private static Entity Replace(SimGame game, Entity source, string target)
        {
            Code result = Upgrades.TryReplace(game.World, source, game.World.Content.Get<EntityDef>(target), out Entity made);
            if (!result.IsNone) throw new InvalidOperationException("replacement refused " + result.Id);
            return made;
        }
        private static void Observation(Check c)
        {
            foreach (bool point in new[] { false, true })
            foreach (string fault in point ? new[] { "payload", "payload-deny", "clock", "space", "throw" }
                : new[] { "payload", "payload-deny", "clock", "throw" })
            foreach (bool replace in new[] { false, true })
            {
                var root=Beep.SimEngine.Content.Json.Parse(point ? PointGame.Definitions : Json);
                root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
                string sourceName=point ? "source" : "external", targetName=point ? "source" : "external.next";
                var entities=root.Get("entities");
                for(int i=0;i<entities.Count;i++) if(entities[i].Get("id").Text==sourceName)
                    entities[i].Get("components").Set("replacement_probe",Beep.SimEngine.Content.Json.Parse(
                        "{\"allowed_targets\":[\""+targetName+"\"],\"fail_initialization\":false,\"check_fault\":\""+fault+"\"}"));
                var factory=point ? PointGame.Factory(root.ToString()) : PointFactory(root.ToString());
                var game=factory.NewGame(Request()); var world=game.World;
                var source=point ? Find(game,sourceName) : world.Spawn(world.Content.Get<EntityDef>(sourceName),new PlayerId(0));
                world.Settle(); Tick(game);
                var twin=factory.Load(game.Save()); ulong independent=twin.Hash();
                var published=game.Seat(0).Snapshot; int version=source.Version;
                EntityId next=world.Entities.NextIdentity; var port=source.Get<SingleResourcePort>(); int payload=port.ServiceCount;
                Entity made=null; bool threw=false;
                try {
                    if(replace) Upgrades.TryReplace(world,source,world.Content.Get<EntityDef>(targetName),out made);
                    else Upgrades.Check(world,source,world.Content.Get<EntityDef>(targetName));
                } catch(InvalidOperationException) { threw=true; }
                string label=(point?"point":"mapless")+"/"+fault+"/"+(replace?"replace":"check");
                c.That(threw&&game.Fault!=null,"preflight observes actual owner and faults: "+label);
                c.Equal("observation.provider",game.Fault?.Phase,"original observation fault owner: "+label);
                if(fault=="throw")c.That(game.Fault?.Message.Contains("authored replacement preflight exception")==true,"first authored failure preserved");
                c.That(made==null && source.Alive && ReferenceEquals(world.Entities.Get(source.Id),source) && world.Entities.NextIdentity==next,
                    "pure preflight commits no target or source retirement: "+label);
                c.Equal(version,source.Version,"probe does not rely on source version changes: "+label);
                c.Equal(payload+(fault=="payload"||fault=="payload-deny"?1:0),port.ServiceCount,"real companion payload mutation is not fabricated: "+label);
                c.Equal(published.Tick,game.Seat(0).Snapshot.Tick,"failed preflight retains last published snapshot");
                c.Throws<InvalidOperationException>(()=>game.Save(),"failed preflight has no healthy save: "+label);
                c.Throws<InvalidOperationException>(()=>game.Hash(),"failed preflight has no healthy hash: "+label);
                c.Equal(Codes.NotAllowed,Upgrades.Check(world,source,source.Def),"faulted replacement check refuses before callbacks: "+label);
                c.Equal(independent,twin.Hash(),"loaded separate session unchanged: "+label);
                twin.Update(); c.That(twin.Fault==null,"loaded session still updates: "+label);
            }
        }
        private static void Preparation(Check c)
        {
            foreach(bool point in new[]{false,true})
            foreach(string fault in point ? new[]{"prepared-target","prepared-target-deny","prepared-source","prepared-clock","prepared-space","prepared-throw"}
                : new[]{"prepared-target","prepared-target-deny","prepared-source","prepared-clock","prepared-throw"})
            foreach(bool loaded in new[]{false,true})
            {
                var root=Beep.SimEngine.Content.Json.Parse(point?PointGame.Definitions:Json);
                root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
                string sourceName=point?"source":"external", targetName=point?"prepared.next":"external.next";
                var rows=root.Get("entities"); JsonValue target=null;
                for(int i=0;i<rows.Count;i++)
                    if(rows[i].Get("id").Text==(point?sourceName:targetName))target=rows[i];
                if(point){target=target.Clone();target.Set("id",JsonValue.Of(targetName));rows.Add(target);}
                var part=target.Get("components").Get("point_stock"); part.Set("replacement_fault",JsonValue.Of(fault));
                if(fault=="prepared-target-deny")part.Set("receipt_fault",JsonValue.Of("refuse_in"));
                var factory=point?PointGame.Factory(root.ToString()):PointFactory(root.ToString());
                var game=factory.NewGame(Request());
                if(!point){game.World.Spawn(game.World.Content.Get<EntityDef>(sourceName),new PlayerId(0));game.World.Settle();}
                Tick(game); if(loaded)game=factory.Load(game.Save());
                var world=game.World;var source=Find(game,sourceName);var stock=source.Get<SingleResourcePort>();
                int version=source.Version,payload=stock.ServiceCount;EntityId next=world.Entities.NextIdentity;
                var twin=factory.Load(game.Save());ulong untouched=twin.Hash();var published=game.Seat(0).Snapshot;
                Entity made=null;bool threw=false;
                try{Upgrades.TryReplace(world,source,world.Content.Get<EntityDef>(targetName),out made);}catch(InvalidOperationException){threw=true;}
                string label=(point?"point":"mapless")+"/"+fault+"/loaded="+loaded;
                c.That(threw&&game.Fault!=null,"detached admission faults before healthy result: "+label);
                if(fault=="prepared-throw")c.That(game.Fault?.Message.Contains("authored detached admission failure")==true,"first detached callback error preserved");
                c.That(made==null&&source.Alive&&ReferenceEquals(world.Entities.Get(source.Id),source)&&
                    world.Entities.Get(next)==null&&world.Entities.NextIdentity==next,"no registered target or identity consumption: "+label);
                c.Equal(version,source.Version,"hidden mutation does not rely on source version: "+label);
                c.Equal(payload+(fault=="prepared-source"?1:0),stock.ServiceCount,"only authored source payload probe changes source auxiliary state: "+label);
                c.That(!world.Events.HasPending,"no relocation or replacement success event before admission: "+label);
                c.Equal(published.Tick,game.Seat(0).Snapshot.Tick,"last healthy publication retained");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted preparation cannot save: "+label);
                c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted preparation cannot hash healthy: "+label);
                var first=game.Fault;c.Equal(Codes.NotAllowed,Upgrades.Check(world,source,source.Def),"later admission refuses before callback");
                c.That(ReferenceEquals(first,game.Fault),"first fault never replaced");
                c.Equal(untouched,twin.Hash(),"separate saved session untouched");twin.Update();c.That(twin.Fault==null,"separate saved session advances");
            }
            foreach(string fault in new[]{"none","second-short","second-throw"})WholeRelocation(c,fault);
        }
        private static void WholeRelocation(Check c,string fault)
        {
            var root=Beep.SimEngine.Content.Json.Parse(Json);
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.replacement-stock"));
            var data=Beep.SimEngine.Content.Json.Parse(FrameworkProofContent.Read("replacement-prepared-stock.json"));
            data.Get("entities")[0].Get("components").Get("atomic_inventory").Set("transfer_receive_fault",JsonValue.Of(fault));
            var factory=PointGame.Factory(root.ToString(),additionalModule:new ProbeModule("proof.replacement-stock",new ModuleVersion(1,0,0),
                    content:types=>types.Component<AtomicInventoryDef>("atomic_inventory",2)),
                moduleDeclaration:"{\"id\":\"proof.replacement-stock\",\"version\":\"1.0.0\",\"documents\":[\"replacement-prepared-stock.json\"]}",
                moduleDocument:data.ToString(),handlingProfiles:FrameworkProofContent.Read("point-handling.json").Replace("\"supply\"","\"alpha\""));
            var game=factory.NewGame(Request());Tick(game);var world=game.World;var source=Find(game,"bin");
            var before=Read(game);int entries=before.Entries.Count;EntityId next=world.Entities.NextIdentity;Entity made=null;
            bool threw=false;try{Upgrades.TryReplace(world,source,world.Content.Get<EntityDef>("replacement.atomic"),out made);}catch(InvalidOperationException){threw=true;}
            if(fault=="none")
            {
                c.That(!threw&&made!=null&&game.Fault==null,"whole mixed native-to-independent relocation commits");
                world.Settle();Tick(game);c.Equal(2,Read(game,before.Latest).Entries.Count,"exactly two verified resource facts, no initial grant");
                c.Equal(Fix.FromInt(10),made.Get<IInventory>().Amount(world.Content.ResourceId("alpha")),"whole count stock preserved");
                c.Equal(Fix.FromRatio(81,4),made.Get<IInventory>().Amount(world.Content.ResourceId("beta")),"whole fractional stock preserved");
                c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"independent mixed target persists through original codec");return;
            }
            c.That(threw&&game.Fault!=null,"later resource leg faults actual replacement: "+fault);
            c.That(!world.Events.HasPending,"whole relocation queues no earlier-leg success prefix: "+fault);
            c.That(made==null&&source.Alive&&world.Entities.Get(next)!=null,"fault retains actual interrupted registered endpoints, no fabricated rollback");
            c.Equal(Fix.Zero,source.Get<IInventory>().Amount(world.Content.ResourceId("alpha")),"first physical leg really executed before second failure");
            c.That(before.Entries.Count==entries,"retained history remains detached");
            c.Throws<InvalidOperationException>(()=>Read(game),"faulted batch exposes no live success history");
            c.Throws<InvalidOperationException>(()=>game.Save(),"interrupted mixed replacement cannot save");
            if(fault=="second-throw")c.That(game.Fault.Message.Contains("authored second receive failure"),"first actual later-leg error preserved");
        }
        private static void Retention(Check c)
        {
            foreach(bool point in new[]{false,true})
            foreach(string fault in point ? new[]{"retain-source","retain-clock","retain-space","retain-source-throw","retain-clock-throw","retain-space-throw","retain-throw","retain-target"}
                : new[]{"retain-source","retain-clock","retain-source-throw","retain-clock-throw","retain-throw","retain-target"})
            foreach(bool loaded in new[]{false,true})
            {
                var root=Beep.SimEngine.Content.Json.Parse(point?PointGame.Definitions:Json);
                root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
                string sourceName=point?"source":"external",targetName=point?"retained.next":"external.next";
                var rows=root.Get("entities");JsonValue target=null;
                for(int i=0;i<rows.Count;i++)if(rows[i].Get("id").Text==(point?sourceName:targetName))target=rows[i];
                if(point){target=target.Clone();target.Set("id",JsonValue.Of(targetName));rows.Add(target);}
                target.Get("components").Get("point_stock").Set("replacement_fault",JsonValue.Of(fault));
                var factory=point?PointGame.Factory(root.ToString()):PointFactory(root.ToString());
                var game=factory.NewGame(Request());
                if(!point){game.World.Spawn(game.World.Content.Get<EntityDef>(sourceName),new PlayerId(0));game.World.Settle();}
                var source=Find(game,sourceName);source.Get<SingleResourcePort>().RecordService();source.Get<SingleResourcePort>().RecordService();
                Tick(game);if(loaded){game=factory.Load(game.Save());source=Find(game,sourceName);}
                var world=game.World;var port=source.Get<SingleResourcePort>();int version=source.Version,payload=port.ServiceCount;
                var resource=port.Def.Resource;Fix amount=port.Amount(resource);EntityId next=world.Entities.NextIdentity;
                ulong before=game.Hash();var twin=factory.Load(game.Save());ulong untouched=twin.Hash();Entity made=null;Exception thrown=null;
                try{Upgrades.TryReplace(world,source,world.Content.Get<EntityDef>(targetName),out made);}catch(InvalidOperationException error){thrown=error;}
                string label=(point?"point":"mapless")+"/"+fault+"/loaded="+loaded;
                if(fault=="retain-target")
                {
                    c.That(thrown==null&&made!=null&&game.Fault==null,"candidate-local auxiliary changes remain valid: "+label);
                    c.Equal(payload,port.ServiceCount,"candidate callback does not alter source history");
                    c.Equal(payload+1,made.Get<SingleResourcePort>().ServiceCount,"provider-owned candidate auxiliary change retained");
                    c.Equal(amount,made.Get<IInventory>().Amount(resource),"candidate changes grant no extra resources");
                    world.Settle();Tick(game);c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"candidate-only change saves normally");
                }
                else if(fault=="retain-throw")
                {
                    c.That(thrown?.Message.Contains("authored local retention failure")==true&&game.Fault==null,"unmutated local failure remains healthy and original: "+label);
                    c.That(made==null&&source.Alive&&world.Entities.NextIdentity==next,"pure local failure registers no target");
                    c.Equal(before,game.Hash(),"pure failed candidate consumes no state or IDs");
                    c.Equal(before,factory.Load(game.Save()).Hash(),"pure failed candidate permits normal save/load");
                }
                else
                {
                    c.That(thrown!=null&&game.Fault!=null,"preparation interval detects authoritative mutation: "+label);
                    if(fault.EndsWith("-throw",StringComparison.Ordinal))
                        c.That(thrown?.Message.Contains("authored local retention failure")==true,"original callback exception survives mutation verification: "+label);
                    c.That(made==null&&source.Alive&&ReferenceEquals(world.Entities.Get(source.Id),source)&&
                        world.Entities.NextIdentity==next&&world.Entities.Get(next)==null,"no identity or registration prefix: "+label);
                    c.Equal(version,source.Version,"probe uses no source version change: "+label);
                    c.Equal(payload+(fault.StartsWith("retain-source",StringComparison.Ordinal)?1:0),port.ServiceCount,"actual auxiliary source mutation matches probe");
                    c.Equal(amount,port.Amount(resource),"rejected retention starts no stock movement");
                    c.That(!world.Events.HasPending,"rejected retention queues no resource/replacement success");
                    c.Throws<InvalidOperationException>(()=>game.Save(),"mutated preparation has no healthy checkpoint");
                    c.Throws<InvalidOperationException>(()=>game.Hash(),"mutated preparation has no healthy hash");
                    var first=game.Fault;c.Equal(Codes.NotAllowed,Upgrades.Check(world,source,source.Def),"later replacement refuses without callbacks");
                    c.That(ReferenceEquals(first,game.Fault),"first fault retained");
                }
                c.Equal(untouched,twin.Hash(),"separate loaded session unchanged: "+label);twin.Update();c.That(twin.Fault==null,"separate loaded session still advances");
            }
        }
        private static void Accounting(Check c)
        {
            GameFactory factory = PointFactory(Json); SimGame game = factory.NewGame(Request());
            SimGame twin = factory.Load(game.Save()), isolated = factory.NewGame(Request());
            World world = game.World; Entity old = Find(game, "bin");
            ResourceId alpha = world.Content.ResourceId("alpha"), beta = world.Content.ResourceId("beta");
            ResourceFlowPage retained = Read(game); long cursor = retained.Latest; ulong before = game.Hash();
            c.That(world.Space == null && !world.Services.Has<IEconomy>(), "resource relocation needs no map or money policy");
            c.Equal(Code.None, Upgrades.Check(world, old, world.Content.Get<EntityDef>("replacement")), "pure full-vector replacement preview");
            c.Equal(before, game.Hash(), "preview changes no stock, receipts, IDs or history");
            Entity made = Replace(game, old, "replacement"), twinMade = Replace(twin, Find(twin, "bin"), "replacement");
            c.That(made.Id != old.Id && made.Owner == old.Owner && made.Alive, "new registered identity retains the company");
            c.That(!old.Alive && world.Entities.Get(old.Id) == null, "source retires only after relocation");
            c.Equal(Fix.Zero, old.Get<IInventory>().Amount(alpha), "retired actual source contains no copied count stock");
            c.Equal(Fix.Zero, old.Get<IInventory>().Amount(beta), "retired actual source contains no copied fractional stock");
            c.Equal(Fix.FromInt(10), made.Get<IInventory>().Amount(alpha), "target receives actual stock, not authored one-unit grant");
            c.Equal(Fix.FromRatio(81, 4), made.Get<IInventory>().Amount(beta), "continuous native amount remains exact");
            c.Equal(0, made.Get<IInventory>().Reservations.Count, "every physical receipt was consumed");
            c.Equal(0, old.Get<IInventory>().Reservations.Count, "retired owner has no promises");
            world.Settle(); twin.World.Settle();
            c.Throws<InvalidOperationException>(() => game.Save(), "successful unjournaled transfers require existing Update boundary");
            c.Throws<InvalidOperationException>(() => game.Hash(), "unrecorded event facts cannot become a hash checkpoint");
            c.Equal(cursor, Read(game).Latest, "no inline journal or second event phase");
            Tick(game); Tick(twin);
            c.Equal(game.Hash(), twin.Hash(), "saved twin executes identical relocation and entity Update");
            ResourceFlowPage page = Read(game, cursor);
            c.Equal(2, page.Entries.Count, "whole stock has exactly two real transfer facts");
            foreach (var entry in page.Entries)
            {
                ResourceFlowFact fact = entry.Item;
                c.Equal(ResourceFlowKind.StockTransfer, fact.Kind, "replacement is not loss plus fake production");
                c.Equal(new ReservationOwner("sim.replacement", old.Id.Value), fact.Owner, "single engine replacement requester");
                c.Equal(ResourceEndpointId.ForEntity(old.Id), fact.Source.Endpoint, "real retired source token");
                c.Equal(ResourceEndpointId.ForEntity(made.Id), fact.Destination.Endpoint, "real registered destination token");
                c.That(fact.Source.IsValid && fact.Destination.IsValid && fact.IsTransfer, "owned physical receipts, not invented references");
                c.Equal(fact.Amount, fact.SourceBefore.Value, "source checkpoint contains actual native stock");
                c.Equal(Fix.Zero, fact.SourceAfter.Value, "exact source delta empties the retired owner");
                c.Equal(Fix.Zero, fact.DestinationBefore, "recipient begins empty");
                c.Equal(fact.Amount, fact.DestinationAfter, "exact target delta verifies native stock");
                c.That(fact.SourceVersionAfter > fact.SourceVersionBefore && fact.DestinationVersionAfter > fact.DestinationVersionBefore,
                    "actual owners advance versions");
            }
            c.Equal(BigInteger.Zero, page.Totals(alpha).InitializedRaw, "replacement never reapplies initial stock");
            c.Equal(BigInteger.Zero, page.Totals(alpha).CreatedRaw, "replacement fabricates no produced stock");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor).Status,
                "foreign company cannot read relocation history");
            c.Equal(2L, game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(1), 0).Latest, "other company journal is unchanged");
            c.Equal(Fix.FromInt(10), Find(game, "bin", 1).Get<IInventory>().Amount(alpha), "other company stock untouched");
            c.Equal(2, retained.Entries.Count, "previous immutable history stays detached");
            c.Equal(ResourceFlowKind.Initialized, retained.Entries[0].Item.Kind, "retained opening fact is not rewritten");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "existing stock and journal Sync restore exact replacement state");
            c.Equal(made.Id, Find(restored, "replacement").Id, "saved new identity remains stable");
            c.Equal(twinMade.Id, made.Id, "ID allocation is deterministic");
            for (int i = 0; i < 5; i++) { Tick(game); Tick(restored); Tick(twin); }
            c.Equal(game.Hash(), restored.Hash(), "continued saved replacement has identical state");
            c.Equal(game.Hash(), twin.Hash(), "interleaved twin keeps identical state");
            c.Equal(0L, isolated.Tick, "unrelated session clock untouched");
            c.Equal(before, isolated.Hash(), "unrelated stock and IDs untouched");
            Refusals(c, factory);
        }
        private static void Refusals(Check c, GameFactory factory)
        {
            SimGame game = factory.NewGame(Request()); World world = game.World; Entity old = Find(game, "bin");
            foreach (string target in new[] { "tiny", "filtered", "plain" })
            {
                ulong before = game.Hash();
                c.Equal(Upgrades.StockWouldBeLost, Upgrades.Check(world, old, world.Content.Get<EntityDef>(target)), "whole capacity/filter/removal preflight refuses " + target);
                c.Equal(Upgrades.StockWouldBeLost, Upgrades.TryReplace(world, old, world.Content.Get<EntityDef>(target), out Entity made), "same typed execution refusal " + target);
                c.That(made == null && old.Alive, "refusal retains original identity " + target);
                c.Equal(before, game.Hash(), "refusal consumes no ID, stock, receipt or history " + target);
            }
            var owner = new ReservationOwner("proof.replacement-claim", 1); ResourceId alpha = world.Content.ResourceId("alpha");
            IInventory stock = old.Get<IInventory>();
            foreach (bool inbound in new[] { false, true })
            {
                ReservationToken claim = inbound ? stock.ReserveIn(owner, alpha, Fix.One) : stock.ReserveOut(owner, alpha, Fix.One);
                ulong before = game.Hash();
                c.Equal(Upgrades.InventoryReserved, Upgrades.TryReplace(world, old, old.Def, out _), "every existing commitment blocks replacement");
                c.Equal(before, game.Hash(), "blocked replacement never clears another owner's promise");
                if (inbound) stock.ReleaseIn(owner, claim); else stock.ReleaseOut(owner, claim);
            }
            ulong checkpoint = game.Hash();
            c.Throws<InvalidOperationException>(() => Replace(game, old, "faulty"), "local preparation failure propagates");
            c.Equal(checkpoint, game.Hash(), "preparation failure preserves source and consumes no identity");
            c.That(game.Fault == null && old.Alive, "uncommitted local failure leaves a healthy original session");
            Entity external = world.Spawn(world.Content.Get<EntityDef>("external"), old.Owner); world.Settle(); Tick(game);
            checkpoint = game.Hash();
            c.Equal(Upgrades.StockWouldBeLost, Upgrades.TryReplace(world, external, world.Content.Get<EntityDef>("cases"), out _), "odd native stock refuses destination case quantum");
            c.Equal(checkpoint, game.Hash(), "quantum refusal is mutation-free");
            var intent = new ResourceInventoryIntent(owner, external.Owner, external.Id,
                new Bill(new[] { new ResourceAmount(alpha, Fix.One) }), ResourceFlowKind.Consumed, new Code("proof.preparation-material"));
            c.That(world.Services.Get<IResourceTransaction>().CommitInventory(intent).Committed, "explicit material consumption makes exact six-unit stock");
            Tick(game); Entity cases = Replace(game, external, "cases"); world.Settle(); Tick(game);
            c.Equal(Fix.FromInt(6), cases.Get<IInventory>().Amount(alpha), "different provider/key and exact native quantum work by contract");
            c.Equal(Fix.Zero, external.Get<IInventory>().Amount(alpha), "external provider is actually emptied, not byte-copied");
            Entity plain = world.Spawn(world.Content.Get<EntityDef>("plain"), old.Owner); world.Settle();
            long cursor = Read(game).Latest; Entity empty = Replace(game, plain, "bin"); world.Settle(); Tick(game);
            c.Equal(0, empty.Get<IInventory>().Stock.Count, "adding inventory during replacement does not grant initial stock");
            c.Equal(cursor, Read(game).Latest, "empty replacement fabricates no resource facts");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "case and empty replacement checkpoints remain durable");
            c.That(game.Fault == null, "normal refusals and valid relocations leave the engine healthy");
        }
        private static void Lifecycle(Check c)
        {
            // Reuse the directly affected bounded timed-order proof, not a broad suite/family filter.
            ProviderPlacementTests.Timed(c);
            Auxiliary(c); Geometry(c); Faults(c);
        }
        private static void Auxiliary(Check c)
        {
            GameFactory factory = PointFactory(Json); SimGame game = factory.NewGame(Request()); World world = game.World;
            Entity old = world.Spawn(world.Content.Get<EntityDef>("external"), new PlayerId(0));
            SingleResourcePort port = old.Get<SingleResourcePort>(); port.RecordService(); port.RecordService(); port.RecordService();
            world.Settle(); Tick(game); long cursor = Read(game).Latest;
            Entity made = Replace(game, old, "external.next"); world.Settle(); Tick(game);
            c.Equal(3, made.Get<SingleResourcePort>().ServiceCount, "public provider contract retains only auxiliary service state");
            c.Equal(3, port.ServiceCount, "retention does not mutate source auxiliary fields");
            c.Equal(Fix.Zero, port.Amount(world.Content.ResourceId("alpha")), "auxiliary retention never clones source stock");
            c.Equal(Fix.FromInt(7), made.Get<IInventory>().Amount(world.Content.ResourceId("alpha")), "actual resource relocation remains distinct from auxiliary retention");
            c.Equal(1, Read(game, cursor).Entries.Count, "external owner produces one exact transfer fact");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "one normal provider codec persists retained auxiliary and resource state");
            c.Equal(3, Find(restored, "external.next").Get<SingleResourcePort>().ServiceCount, "saved service history remains intact");

            string blocked = Json.Replace("\"id\":\"external.next\",\"kind\":\"entity\",\"components\":{\"point_stock\":{", "\"id\":\"external.next\",\"kind\":\"entity\",\"components\":{\"point_stock\":{\"receipt_fault\":\"refuse_in\",");
            c.That(blocked != Json, "authored refusal probe targets exact recipient");
            factory = PointFactory(blocked); game = factory.NewGame(Request()); world = game.World;
            old = world.Spawn(world.Content.Get<EntityDef>("external"), new PlayerId(0)); world.Settle(); Tick(game); ulong before = game.Hash();
            c.Equal(Upgrades.StockWouldBeLost, Upgrades.TryReplace(world, old, world.Content.Get<EntityDef>("external.next"), out made), "actual pure provider admission can refuse before commit");
            c.That(made == null && old.Alive && game.Fault == null, "runtime admission refusal leaves original healthy");
            c.Equal(before, game.Hash(), "private candidate refusal changes no live identity, stock, receipts or history");
        }
        private static void Geometry(Check c)
        {
            const string json = "{\"entities\":[{\"id\":\"expanded\",\"kind\":\"structure\",\"components\":{\"tile_presence\":{\"space\":\"recovery.tiles\",\"width\":4,\"height\":3,\"blocks\":true,\"movable\":false},\"storage\":{\"capacity_policy\":\"recovery.mixed\",\"initial_stock\":{\"steel\":1}},\"replacement_probe\":{\"allowed_targets\":[],\"fail_initialization\":false}}}]}";
            GameFactory factory = PhysicalRecoveryTestContent.Factory(json, supplier: true);
            SimGame game = factory.NewGame(Request(true, "colony")); World world = game.World; TileMap map = (TileMap)world.Space;
            Entity old = Find(game, "depot"); TileCoord origin = old.Get<ITilePresence>().Tile;
            EntityId oldId = old.Id; long cursor = Read(game).Latest;
            Entity made = Replace(game, old, "expanded"); world.Settle(); Tick(game);
            c.That(ReferenceEquals(map.OccupantAt(origin), made), "new identity owns old footprint only after physical transfer");
            c.That(ReferenceEquals(map.OccupantAt(new TileCoord(origin.X + 3, origin.Y)), made), "expanded footprint claimed by same spatial provider");
            c.That(world.Entities.Get(oldId) == null && !old.Alive, "retired identity cannot resolve as a transfer endpoint");
            c.Equal(Fix.FromInt(50), made.Get<IInventory>().Amount(world.Content.ResourceId("steel")), "resize preserves count resources");
            c.Equal(Fix.FromInt(10), made.Get<IInventory>().Amount(world.Content.ResourceId("water")), "resize preserves native fluids");
            c.Equal(2, Read(game, cursor).Entries.Count, "geometry is not an unreported resource copy");
            SimGame restored = factory.Load(game.Save()); c.Equal(game.Hash(), restored.Hash(), "saved resized provider reclaims exact geometry");
            c.Equal(made.Id, ((TileMap)restored.World.Space).OccupantAt(origin).Id, "restored occupancy belongs to new stable ID");
            c.That(game.Modules.Contains(TycoonModule.ModuleId) && !world.Services.Has<IBusinessAccounts>(),
                "supplier-only protocol proof explicitly selects optional provider without money or account defaults");
            // Explicit protocol fixture setup, not a player creation or startup fallback.
            var supplierTile = new TileCoord(4, 20);
            Entity market = world.Spawn(world.Content.Get<EntityDef>("market"), PlayerId.None,
                entity => ((ISpatialPlacement)world.Space).ConfigurePlacement(entity,
                    new TilePlacement(world.Space.Id, supplierTile, FixVec2.Centre(supplierTile))));
            world.Settle(); ISupplier supplier = market.Get<ISupplier>();
            var owner = new ReservationOwner("proof.replacement-supply", 1);
            ReservationToken claim = supplier.ReserveSupply(owner, world.Content.ResourceId("steel"), Fix.One);
            ulong before = game.Hash();
            c.Equal(Upgrades.InventoryReserved, Upgrades.TryReplace(world, market, market.Def, out _), "supplier-only owned claims cannot be silently retired");
            c.Equal(before, game.Hash(), "supplier commitment refusal preserves its actual book");
            supplier.ReleaseSupply(owner, claim);

            string failing = json.Replace("\"fail_initialization\":false", "\"fail_initialization\":true");
            factory = PhysicalRecoveryTestContent.Factory(failing, supplier: true); game = factory.NewGame(Request(true, "colony"));
            world = game.World; map = (TileMap)world.Space; old = Find(game, "depot"); origin = old.Get<ITilePresence>().Tile; before = game.Hash();
            c.Throws<InvalidOperationException>(() => Replace(game, old, "expanded"), "target local initialization can fail without unclaiming source geometry");
            c.That(ReferenceEquals(map.OccupantAt(origin), old) && map.OccupantAt(new TileCoord(origin.X + 3, origin.Y)) == null, "uncommitted preparation never occupies expanded geometry");
            c.Equal(before, game.Hash(), "preparation failure consumes no map state, stock or IDs");
            c.That(game.Fault == null, "uncommitted local failure is not a partial tick");
        }
        private static void Faults(Check c)
        {
            foreach (string fault in new[] { "retain_take", "partial_take", "partial_receive", "mutating_check", "silent_version", "clone_stock", "mutate_source" })
            {
                bool retention = fault == "clone_stock" || fault == "mutate_source";
                bool targetFault = fault == "partial_receive" || retention;
                string entity = targetFault ? "external.next" : "external";
                string anchor = "\"id\":\"" + entity + "\",\"kind\":\"entity\",\"components\":{\"point_stock\":{";
                string json = Json.Replace(anchor, anchor + "\"" + (retention ? "replacement_fault" : "receipt_fault") + "\":\"" + fault + "\",");
                c.That(json != Json, "fault probe targets actual provider: " + fault);
                GameFactory factory = PointFactory(json); SimGame game = factory.NewGame(Request()); World world = game.World;
                Entity old = world.Spawn(world.Content.Get<EntityDef>("external"), new PlayerId(0)); world.Settle(); Tick(game);
                // Protocol evidence from the original history codec; faulted public reads must refuse.
                var history = (IPersistentService)world.Services.Get<IResourceFlowJournal>();
                var beforeHistory = new StateHasher(); history.Sync(beforeHistory);
                c.Throws<InvalidOperationException>(() => Replace(game, old, "external.next"), "exact provider violation propagates: " + fault);
                string phase = fault == "clone_stock" ? "observation.provider" : fault == "mutate_source" ? "entity.replace" : "resource.transaction";
                c.Equal(phase, game.Fault?.Phase, "actual observation or resource transaction owns first fault: " + fault);
                var afterHistory = new StateHasher(); history.Sync(afterHistory);
                c.Equal(beforeHistory.Value, afterHistory.Value, "original history codec contains no success prefix: " + fault);
                c.Throws<InvalidOperationException>(() => Read(game), "faulted replacement exposes no live successful history: " + fault);
                c.That(!world.Events.HasPending, "failed single leg creates no deferred fact: " + fault);
                c.Throws<InvalidOperationException>(() => game.Save(), "interrupted replacement cannot resume: " + fault);
                c.Throws<InvalidOperationException>(() => game.Hash(), "interrupted replacement cannot checkpoint hash: " + fault);
                c.Equal(0, game.RunTicks(1), "faulted replacement cannot keep updating: " + fault);
                c.Equal(Codes.NotAllowed, Upgrades.Check(world, old, old.Def), "faulted session cannot replace again: " + fault);
            }
        }
    }
}
