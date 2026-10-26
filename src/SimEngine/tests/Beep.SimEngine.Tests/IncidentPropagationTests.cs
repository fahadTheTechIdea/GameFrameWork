using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    public static class IncidentPropagationTests
    {
        private static readonly Code Source = new Code("leak"), Spread = new Code("spread"), Kind = new Code("machine.leak");
        public static void Register()
        {
            TestRunner.Add("Incident propagation: authored rules and bounded admission", Contracts);
            TestRunner.Add("Incident propagation: delayed exposure, physical isolation and saved waves", Physical);
        }
        private static string Text(JsonValue json) { var text = new StringBuilder(); json.WriteCanonical(text); return text.ToString(); }
        private static JsonValue Rule(JsonValue json) => json.Get("tile_incident_propagation_rules")[0];
        private static JsonValue Parts(JsonValue json) => json.Get("entities")[0].Get("components");
        private static GameFactory Factory(Action<JsonValue> edit = null, int budget = 100, Action<JsonValue> settings = null)
        {
            JsonValue document = Json.Parse(FrameworkProofContent.Read("incident-propagation.json")); edit?.Invoke(document);
            JsonValue definitions = Json.Parse(Colony.Defs);
            JsonValue scenario = definitions.Get("scenarios")[0];
            scenario.Get("settings").Set(IncidentPropagationModule.ModuleId, Json.Parse("{\"budget\":" + budget + "}")); settings?.Invoke(scenario);
            return Colony.Factory(Text(definitions), additionalDocuments: new Dictionary<string, string> {
                { "incident-phases.json", FrameworkProofContent.Read("incident-phases.json") }, { "incident-propagation.json", Text(document) } });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "colony", Seed = 313 }; request.Mods.Add(IncidentPropagationModule.ModuleId);
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        // Fixture placement only. Player actions and subsequent timing use normal commands/Update.
        private static Entity Spawn(SimGame game, int x, string definition = "spreading-machine", int seat = 0)
        {
            var def = game.World.Content.Get<EntityDef>(definition);
            Entity entity = def.Component<ITilePresenceDefinition>() == null ? game.World.Spawn(def, new PlayerId(seat))
                : ((TileMap)game.World.Space).SpawnAt(game.World, def, new PlayerId(seat), new TileCoord(x, 28));
            game.World.Entities.Flush(); return entity;
        }
        private static CommandResult Raise(SimGame game, EntityId id, int seat = 0)
            => TestKit.Order(game, seat, new RaiseIncidentCommand { Target = id, Source = Source, Kind = Kind });
        private static CommandResult Permit(SimGame game, EntityId id)
            => TestKit.Order(game, 0, new SetWorkPermitCommand { Target = id, Enabled = true });
        private static bool Active(Entity entity) => entity.Get<IPhasedIncidentResponse>(Source).ReadLifecycle().Active;
        private static void Tick(SimGame game)
        { if (game.RunTicks(1) != 1) throw new InvalidOperationException("propagation tick=" + game.Tick + "; " + game.Fault); }
        private static void Advance(SimGame game, int ticks, SimGame twin = null)
        { for (int i = 0; i < ticks; i++) { Tick(game); if (twin != null) Tick(twin); } }
        private static IncidentPropagationSnapshot Read(SimGame game, EntityId id, bool observer = false)
        {
            var view = (observer ? game.World.ViewFor(PlayerId.None) : game.Seat(0).View).Feature<IncidentPropagationView>();
            if (!view.TryRead(id, Spread, out var facts)) throw new InvalidOperationException("missing actual propagation record " + id);
            return facts;
        }
        private static void Reach(SimGame game, EntityId source, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 500 && !ready(); i++) Advance(game, 1, twin);
            if (!ready())
            {
                var facts = Read(game, source, true); var order = game.World.Services.Get<WorkBoard>().ActiveResponseOn(source, Source);
                throw new InvalidOperationException(stage + " timeout; tick=" + game.Tick + "; scan=" + facts.ScanAfter + "/" + facts.ScanThrough +
                    "; pending=" + facts.Pending.Count + "; blocker=" + facts.Blocker + "; response=" + order?.State + "/" + order?.Blocker);
            }
        }
        private static void Invalid(Check c, Action<JsonValue> edit, string reason)
            => c.Throws<ContentException>(() => New(Factory(edit)), reason);
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(budget: 1); SimGame game = New(factory);
            Entity a = Spawn(game, 16), b = Spawn(game, 26), foreign = Spawn(game, 45, seat: 1);
            Tick(game);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current contract identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one new declared exposure system codec");
            c.Equal(IncidentPropagationCodes.Inactive, Read(game, a.Id).Blocker, "unraised binding has no fabricated exposure");
            c.Equal(-1L, Read(game, a.Id).NextCycleTick, "inactive has no fake schedule");
            c.That(!game.Seat(0).View.Feature<IncidentPropagationView>().TryRead(foreign.Id, Spread, out _), "foreign source facts remain private");
            c.That(!game.Seat(0).View.Feature<IncidentPropagationView>().TryRead(a.Id, new Code("absent"), out _), "named capability is exact");
            c.That(Raise(game, a.Id).Accepted && Raise(game, b.Id).Accepted, "ordinary commands raise two real source episodes");
            var beforeA = Read(game, a.Id, true); var beforeB = Read(game, b.Id, true);
            Advance(game, 1);
            var afterA = Read(game, a.Id, true); var afterB = Read(game, b.Id, true);
            int progressed = (afterA.ScanAfter != beforeA.ScanAfter ? 1 : 0) + (afterB.ScanAfter != beforeB.ScanAfter ? 1 : 0);
            c.Equal(1, progressed, "one authored budget unit advances exactly one candidate/source");
            Advance(game, 1);
            c.That(afterA.ScanAfter == beforeA.ScanAfter ? Read(game, a.Id, true).ScanAfter != afterA.ScanAfter
                : Read(game, b.Id, true).ScanAfter != afterB.ScanAfter,
                "saved stable round-robin advances the other due source");
            c.That(Read(game, a.Id).ScanAfter == EntityId.None && Read(game, a.Id).ScanThrough == EntityId.None,
                "private diagnostics never expose global scan identity high-water");
            c.That(Read(game, a.Id, true).ScanThrough.IsValid, "explicit observer can diagnose captured scan membership");
            var captured = Read(game, a.Id); ulong hash = game.Hash(); Read(game, a.Id);
            long capturedSchedule = captured.NextCycleTick;
            c.Equal(hash, game.Hash(), "reads cannot advance scans or mutate eligibility");
            c.Throws<NotSupportedException>(() => ((IList<PendingExposureSnapshot>)captured.Pending).Clear(), "pending view is detached immutable data");
            SimGame twin = factory.Load(game.Save()); Advance(game, 20, twin);
            c.Equal(game.Hash(), twin.Hash(), "mid-scan cursor/budget/source history restores identically");
            c.Equal(capturedSchedule, captured.NextCycleTick, "captured schedule remains a value, not a provider handle");
            Invalid(c, j => Rule(j).Remove("interval"), "interval is mandatory");
            Invalid(c, j => Rule(j).Get("interval").Set("numerator", JsonValue.Number("0")), "zero interval cannot create a same-update loop");
            Invalid(c, j => Rule(j).Get("delay").Set("numerator", JsonValue.Number("0")), "zero delivery delay is refused");
            Invalid(c, j => Rule(j).Get("delay").Set("denominator", JsonValue.Number("0")), "invalid rational denominator refuses");
            Invalid(c, j => Rule(j).Get("delay").Set("denominator", JsonValue.Number("2")), "nonaligned duration cannot round into instant/default timing");
            Invalid(c, j => Rule(j).Set("radius", JsonValue.Number("-1")), "negative geometry does not substitute a default");
            Invalid(c, j => Rule(j).Set("space", JsonValue.Of("absent")), "missing selected space refuses");
            Invalid(c, j => Rule(j).Set("companies", JsonValue.Of("friendly")), "scope is explicit, not inferred");
            Invalid(c, j => Rule(j).Set("target_source", JsonValue.Of("absent")), "missing target capability refuses before ticking");
            Invalid(c, j => Rule(j).Set("target_source", JsonValue.Of("")), "empty target identity is a typed content refusal");
            Invalid(c, j => Rule(j).Set("target_kind", JsonValue.Of("absent")), "unsupported target kind refuses before ticking");
            Invalid(c, j => Rule(j).Remove("source_conditions"), "conditions must explicitly select even empty intent");
            Invalid(c, j => Rule(j).Set("target_conditions", Json.Parse("[{\"id\":\"bad\",\"source\":\"absent\",\"signal\":\"absent\",\"expected\":false,\"blocker\":\"blocked\"}]")), "missing signal binding refuses");
            Invalid(c, j => Parts(j).Get("incident_propagation").Remove("id"), "emitter identity is mandatory");
            Invalid(c, j => Parts(j).Get("incident_propagation").Set("id", JsonValue.Of("")), "empty emitter identity is a typed content refusal");
            Invalid(c, j => Parts(j).Get("incident_propagation").Set("source", JsonValue.Of("absent")), "exact source metadata/runtime are required");
            Invalid(c, j => Parts(j).Get("incident_propagation").Set("rule", JsonValue.Of("absent")), "no fallback rule");
            Invalid(c, j => Parts(j).Remove("tile_presence"), "native rule requires actual source geometry");
            Invalid(c, j => Rule(j).Set("legacy", JsonValue.Of(true)), "unknown authoring keys are rejected");
            c.Throws<ContentException>(() => New(Factory(budget: 0)), "budget must be explicitly positive");
            c.Throws<ContentException>(() => New(Factory(settings: j => j.Get("settings").Remove(IncidentPropagationModule.ModuleId))), "selected module requires explicit settings");
            c.Throws<ArgumentException>(() => new IncidentExposureObservation(false, Code.None), "ineligible cannot conceal its blocker");
            c.Throws<ArgumentException>(() => new IncidentExposureObservation(true, new Code("blocked")), "eligible cannot carry contradictory facts");
            Codec(c, game, a.Id); Provider(c); CapturedMembership(c);
        }
        private static void Codec(Check c, SimGame game, EntityId id)
        {
            GameSystem system = null; foreach (var item in game.World.Systems) if (item.Id == IncidentPropagationModule.ModuleId) system = item;
            ulong before = game.Hash();
            foreach (int mode in new[] { 0, 1, 2 }) using (var stream = new MemoryStream())
            {
                var facts = Read(game, id, true); var writer = new StateWriter(stream);
                EntityId cursor = EntityId.None; string cap = null; writer.Entity(ref cursor); writer.String(ref cap); writer.Count(1);
                writer.Entity(ref id); string capability = Spread.Id, rule = mode == 1 ? "absent" : facts.Rule;
                writer.String(ref capability); writer.String(ref rule); int episode = mode == 0 ? -1 : facts.ObservedEpisode; writer.Int(ref episode);
                long next = facts.NextCycleTick; writer.Long(ref next); bool scanning = false; writer.Bool(ref scanning);
                EntityId zero = EntityId.None; writer.Entity(ref zero); writer.Entity(ref zero); string blocker = null; writer.String(ref blocker);
                writer.Count(mode == 2 ? 2 : 0);
                if (mode == 2) for (int i = 0; i < 2; i++)
                { EntityId target = new EntityId(id.Value + 1); int expected = 0; long scheduled = game.Tick, due = scheduled + 3;
                    writer.Entity(ref target); writer.Int(ref expected); writer.Long(ref scheduled); writer.Long(ref due); }
                byte[] bytes = stream.ToArray();
                c.Throws<CorruptStateException>(() => system.Sync(new StateReader(bytes, 0, bytes.Length)), "staged exposure decode rejects mode " + mode);
                c.Equal(before, game.Hash(), "rejected record never replaces prior live exposure state " + mode);
            }
        }
        private static void Provider(Check c)
        {
            Action<JsonValue> external = j => {
                j.Remove("tile_incident_propagation_rules"); Parts(j).Remove("tile_presence");
                Parts(j).Remove("incident_propagation"); Parts(j).Set("proof_incident_propagation", Json.Parse("{\"id\":\"spread\",\"source\":\"leak\",\"rule\":\"leak.permitted-spread\"}"));
            };
            GameFactory factory = Factory(external); SimGame game = New(factory); Entity a = Spawn(game, 0), b = Spawn(game, 0);
            c.That(a.Get<IIncidentPropagationEmitter>(Spread) is ProofPropagationEmitter && a.Get<ITilePresence>() == null,
                "independent emitter/rule works without source geometry or scheduler changes");
            c.That(Permit(game, b.Id).Accepted && Raise(game, a.Id).Accepted, "ordinary readiness/input selects nonspatial eligibility");
            Reach(game, a.Id, () => Active(b), "external provider spread");
            c.Equal(1, b.Get<IPhasedIncidentResponse>(Source).ReadLifecycle().Episode, "external rule delivers through existing incident owner exactly once");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "provider and native exposure owner share normal codec manifest");
            for (int mode = 1; mode <= 2; mode++)
            {
                int fault = mode; SimGame broken = New(Factory(j => { external(j); j.Get("permit_incident_propagation_rules")[0].Set("fault_mode", JsonValue.Number(fault.ToString())); }));
                Entity source = Spawn(broken, 0), target = Spawn(broken, 0);
                c.That(Raise(broken, source.Id).Accepted, "fault probe begins ordinary actual source " + mode);
                for (int i = 0; i < 20 && broken.Fault == null; i++) broken.Update();
                c.That(broken.Fault?.Phase == "observation.provider", "invalid/mutating observation faults actual provider boundary " + mode);
                c.That(!Active(target), "dishonest eligibility cannot raise a target " + mode);
                c.Throws<InvalidOperationException>(() => broken.Save(), "faulted exposure cannot save as healthy " + mode);
            }
        }
        private static void Physical(Check c)
        { Delayed(c); Isolation(c); Removed(c); Foreign(c); StaleEpisode(c); }
        private static void CapturedMembership(Check c)
        {
            GameFactory factory = Factory(budget: 1); SimGame game = New(factory); Entity a = Spawn(game, 16), receiver = Spawn(game, 18, "leaking-machine");
            c.That(Raise(game, a.Id).Accepted, "bounded membership begins with an ordinary episode");
            Reach(game, a.Id, () => Read(game, a.Id, true).Scanning, "captured scan start");
            var first = Read(game, a.Id, true); Entity late = Spawn(game, 19, "leaking-machine");
            c.That(late.Id.Value > first.ScanThrough.Value, "new live entity has identity beyond captured cycle membership");
            Reach(game, a.Id, () => !Read(game, a.Id, true).Scanning, "bounded cycle completion");
            c.That(!Active(late), "late entity cannot sneak into the in-progress cycle");
            SimGame twin = factory.Load(game.Save()); Reach(game, a.Id, () => Active(late), "late target next cycle", twin);
            c.That(Active(receiver), "budget exhaustion does not lose previously admitted deliveries");
            c.Equal(game.Hash(), twin.Hash(), "cycle rollover and newly born targets preserve saved stable membership");
        }
        private static void StaleEpisode(Check c)
        {
            GameFactory factory = Factory(j => Rule(j).Get("delay").Set("numerator", JsonValue.Number("300")));
            SimGame game = New(factory); Entity a = Spawn(game, 12), b = Spawn(game, 15, "leaking-machine");
            c.That(Raise(game, a.Id).Accepted, "stale-epoch proof starts existing source work");
            Reach(game, a.Id, () => Read(game, a.Id).Pending.Count > 0, "preincident target reservation");
            long due = Read(game, a.Id).Pending[0].DueTick;
            c.Equal(0, Read(game, a.Id).Pending[0].ExpectedEpisode, "exposure retains the exact preexisting target epoch");
            c.That(Permit(game, b.Id).Accepted && Raise(game, b.Id).Accepted, "target independently starts and physically recovers through ordinary commands");
            SimGame twin = factory.Load(game.Save()); Reach(game, a.Id, () => !Active(b), "independent target completion", twin);
            c.That(game.Tick < due, "target's real recovery occurs before old pending delivery");
            c.Equal(1, b.Get<IPhasedIncidentResponse>(Source).ReadLifecycle().Episode, "actual target owner retains completed episode one");
            Advance(game, checked((int)(due - game.Tick + 1)), twin);
            c.That(!Active(b) && b.Get<IPhasedIncidentResponse>(Source).ReadLifecycle().Episode == 1,
                "old target epoch never raises new damage after intervening completed recovery");
            c.Equal(game.Hash(), twin.Hash(), "stale epoch refusal shares normal saved histories, without a second incident state");
        }
        private static void Delayed(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            Entity a = Spawn(game, 16), b = Spawn(game, 19), d = Spawn(game, 22), far = Spawn(game, 30, "leaking-machine"),
                foreign = Spawn(game, 18, "leaking-machine", 1);
            WideTimeTests.SetTick(game, 1L << 40);
            c.That(Raise(game, a.Id).Accepted, "wide-time episode begins normal engine work");
            c.That(!Active(b) && !Active(d), "trigger is not instant area damage or target enrollment");
            Reach(game, a.Id, () => Read(game, a.Id).Pending.Count > 0, "first delayed exposure");
            var pending = Read(game, a.Id).Pending[0]; var captured = Read(game, a.Id);
            c.Equal(b.Id, pending.Target, "actual inclusive full-footprint radius selects nearby target");
            c.Equal(3L, pending.DueTick - pending.ScheduledTick, "authored exact game minutes retain wide signed timestamps");
            c.That(pending.ScheduledTick > (1L << 40) && !Active(b), "positive interval then delay precede actual spread");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending target/source epochs restore before delivery");
            Reach(game, a.Id, () => Active(b), "first real delayed Raise", twin);
            c.Equal(pending.DueTick, b.Get<IPhasedIncidentResponse>(Source).ReadLifecycle().RaisedTick, "due delivery occurs at declared update boundary");
            c.That(!Active(d), "newly raised emitter cannot propagate during the same Update even with spare budget");
            c.Equal(0, Read(game, b.Id).ObservedEpisode, "captured inactive source admission stays unchanged until next Update");
            c.Equal(1, captured.Pending.Count, "previous pending snapshot is detached after delivery");
            c.That(game.World.Services.Get<WorkBoard>().ActiveResponseOn(b.Id, Source) != null, "new target enrolls in existing physical response work");
            Advance(game, 1, twin); c.Equal(1, Read(game, b.Id).ObservedEpisode, "next Update admits the real new source epoch");
            Reach(game, a.Id, () => Active(d), "second authored wave", twin);
            c.That(!Active(far), "distant target never receives fabricated exposure");
            c.That(!Active(foreign), "same-company policy refuses nearby foreign endpoints without a hidden override");
            c.Equal(game.Hash(), twin.Hash(), "multiple delayed waves and physical work continue hash-identically");
            c.Equal(1, b.Get<IPhasedIncidentResponse>(Source).ReadLifecycle().Episode, "active targets do not receive duplicate episode Raises");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "postwave histories/backlog restore with normal engine save");
        }
        private static void Isolation(Check c)
        {
            GameFactory factory = Factory(j => {
                Rule(j).Get("delay").Set("numerator", JsonValue.Number("100"));
                j.Set("signals", Json.Parse("[{\"id\":\"leak.phase-hold\",\"kind\":\"boolean\",\"metric\":\"incident.phase-hold\",\"unit\":\"unit.boolean\",\"providers\":[{\"component\":\"incident_signal\",\"schema\":1}]}]"));
                Parts(j).Set("incident_signal", Json.Parse("{\"id\":\"sensor\",\"source\":\"leak\",\"workflow\":\"machine.leak\",\"phase\":\"isolation\"}"));
                Rule(j).Set("source_conditions", Json.Parse("[{\"id\":\"isolation\",\"source\":\"sensor\",\"signal\":\"leak.phase-hold\",\"expected\":false,\"blocker\":\"leak.isolated\"}]"));
            });
            SimGame game = New(factory); Entity a = Spawn(game, 12), b = Spawn(game, 15, "leaking-machine");
            Entity crane = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id); var position = crane.Get<ITilePresence>().Position;
            c.That(Raise(game, a.Id).Accepted, "isolation policy begins physical response");
            Reach(game, a.Id, () => Read(game, a.Id).Pending.Count > 0, "isolation pending exposure");
            long due = Read(game, a.Id).Pending[0].DueTick;
            Reach(game, a.Id, () => game.World.Services.Get<WorkBoard>().ActiveResponseOn(a.Id, Source)?.Node("isolate").State == NodeState.Done, "physical isolation");
            c.That(game.Tick < due, "real responder isolates before the authored exposure delay expires");
            c.That(crane.Get<ITilePresence>().Position != position, "machine physically travels before closing propagation");
            c.That(a.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")) < Fix.FromInt(30), "actual isolation consumes native stock");
            Order order = game.World.Services.Get<WorkBoard>().ActiveResponseOn(a.Id, Source);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "ordinary cancellation is handled by existing response work");
            SimGame twin = factory.Load(game.Save()); Advance(game, checked((int)(due - game.Tick + 3)), twin);
            c.That(Active(a) && !Active(b), "cancelled/resumed but isolated source never delivers its stale pending exposure");
            c.Equal(new Code("leak.isolated"), Read(game, a.Id).Blocker, "current shared phase-hold signal gives explicit safety blocker");
            c.Equal(game.Hash(), twin.Hash(), "saved physical hold rechecks delivery deterministically");
            c.That(Permit(game, a.Id).Accepted && Permit(twin, a.Id).Accepted, "ordinary readiness allows true cleanup/restart");
            Reach(game, a.Id, () => !Active(a), "completed response", twin);
            Advance(game, 1, twin);
            c.Equal(IncidentPropagationCodes.SourceEnded, Read(game, a.Id).Blocker, "real completed source cancels its exposure cause");
            c.Equal(0, Read(game, a.Id).Pending.Count, "completion erases only pending exposure, not incident/work history");
            c.Equal(-1L, Read(game, a.Id).NextCycleTick, "resolved source has no running propagation schedule");
            c.Equal(game.Hash(), twin.Hash(), "whole physical recovery retains one incident/work/exposure ownership path");
        }
        private static void Removed(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity a = Spawn(game, 16), b = Spawn(game, 18, "leaking-machine");
            c.That(Raise(game, a.Id).Accepted, "removal proof uses ordinary source trigger");
            Reach(game, a.Id, () => Read(game, a.Id).Pending.Count > 0, "removed target exposure");
            long due = Read(game, a.Id).Pending[0].DueTick;
            // Explicit fixture lifecycle fault: removal still passes engine resource disposition first.
            c.That(game.World.Services.Get<IResourceTransaction>().CommitInventory(new ResourceInventoryIntent(
                new ReservationOwner("proof.removal", b.Id.Value), b.Owner, b.Id,
                new Bill(new[] { new ResourceAmount(game.World.Content.ResourceId("steel"), Fix.FromInt(30)) }),
                ResourceFlowKind.Consumed, new Code("proof.removal"))).Committed,
                "fixture disposal uses real authoritative resource protocol");
            game.World.Events.Dispatch(); game.World.Despawn(b); game.World.Entities.Flush();
            Entity replacement = Spawn(game, 18, "leaking-machine");
            c.That(replacement.Id != b.Id, "engine never reuses removed live identity for pending delivery");
            SimGame twin = factory.Load(game.Save()); Advance(game, checked((int)(due - game.Tick + 1)), twin);
            c.That(!Active(replacement), "removed target is not silently substituted by a later occupant");
            c.Equal(game.Hash(), twin.Hash(), "removed identity and newly authored next-cycle exposure retain saved ordering");
        }
        private static void Foreign(Check c)
        {
            GameFactory factory = Factory(j => Rule(j).Set("companies", JsonValue.Of("any")));
            SimGame game = New(factory); Entity a = Spawn(game, 16), b = Spawn(game, 18, "leaking-machine", 1);
            c.That(Raise(game, a.Id).Accepted, "authored cross-company pressure begins ordinarily");
            Reach(game, a.Id, () => Read(game, a.Id, true).Pending.Count > 0, "private foreign exposure");
            c.Equal(0, Read(game, a.Id).Pending.Count, "own-company diagnostics never leak hidden foreign target identity");
            c.Equal(1, Read(game, a.Id, true).Pending.Count, "explicit observer sees actual backlog, not a fabricated empty book");
            SimGame twin = factory.Load(game.Save()); Reach(game, a.Id, () => Active(b), "authored company-any delivery", twin);
            c.Equal(game.Hash(), twin.Hash(), "cross-company timing is physical deterministic pressure, not private view authority");
        }
    }
}
