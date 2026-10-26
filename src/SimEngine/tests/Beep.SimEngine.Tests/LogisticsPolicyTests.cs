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
    public static class LogisticsPolicyTests
    {
        public static void Register()
        {
            TestRunner.Add("Logistics policies: strict native bands, shared admission and private settings", Admission);
            TestRunner.Add("Logistics policies: physical refill, export contention and saved commitments", Physical);
            TestRunner.Add("Haul candidate cost: complete selected trip, policy ties and saved acquisition", CandidateCost);
        }
        private static string Profiles => FrameworkProofContent.Read("logistics-policies.json");
        private static GameFactory Factory(bool preferred = false, string profiles = null, Action<JsonValue> edit = null)
        {
            JsonValue definitions = Content.Json.Parse(PointGame.Definitions);
            definitions.Get("entities")[0].Get("components").Set("logistics_policy", Content.Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"policy.source\"}]}"));
            definitions.Get("entities")[1].Get("components").Set("logistics_policy", Content.Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"policy.destination\"}]}"));
            if (preferred)
            {
                JsonValue extra = definitions.Get("entities")[0].Clone(); extra.Set("id", JsonValue.Of("preferred"));
                extra.Get("components").Get("point_presence").Set("position", JsonValue.Number("25"));
                extra.Get("components").Remove("logistics_policy"); extra.Get("components").Set("contract_outlet_policy", Content.Json.Parse("{\"profile\":\"policy.preferred\"}"));
                definitions.Get("entities").Add(extra);
                StartupContentAuthoring.Add(definitions, "preferred", "preferred", 0, extra.Get("components").Get("point_presence"));
            }
            edit?.Invoke(definitions);
            var text = new StringBuilder(); definitions.WriteCanonical(text);
            return PointGame.Factory(text.ToString(), profiles ?? Profiles);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 271 });
        private static Entity Find(SimGame game, string prototype)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype) return entity;
            throw new InvalidOperationException("missing policy entity " + prototype);
        }
        private static LogisticsDemandView Band(SimGame game)
        {
            var facts = new List<LogisticsDemandView>(); game.Seat(0).View.Feature<LogisticsView>().CopyDemands(facts);
            foreach (var fact in facts) if (fact.Source.Scope == LogisticsService.StockDemandScope && fact.Destination == Find(game, "destination").Id) return fact;
            throw new InvalidOperationException("missing stock band");
        }
        private static LogisticsDemandSettings Settings(bool enabled, int min, int desired, int max, int priority = 65)
            => new LogisticsDemandSettings(enabled, Fix.FromInt(min), Fix.FromInt(desired), Fix.FromInt(max), priority, LogisticsReserveAccess.Protected);
        private static OutletPolicySettings Outlet(ResourceId resource, bool enabled = true, int reserve = 4, int priority = 3, string[] destinations = null)
            => new OutletPolicySettings(enabled, reserve == 0 ? Bill.Empty : new Bill(new[] { new ResourceAmount(resource, Fix.FromInt(reserve)) }), priority, destinations, LogisticsReserveAccess.Protected);
        private static void Stock(Check c, SimGame game, LogisticsDemandSettings settings)
            => c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = game.World.Content.ResourceId("supply"), Change = LogisticsDemandChange.Replace(settings) }).Accepted, "player configures sole stock policy through engine command");
        private static void Export(Check c, SimGame game, Entity entity, OutletPolicySettings settings)
            => c.That(TestKit.Order(game, 0, new ConfigureOutletPolicyCommand { Source = entity.Id, Change = OutletPolicyChange.Replace(settings) }).Accepted, "player configures actual outlet provider through engine command");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, SimGame isolated = null)
        {
            for (int i = 0; i < 320 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); if (isolated != null) Tick(isolated); }
            if (!done())
            {
                var facts = new List<string>(); foreach (var job in game.World.Services.Get<WorkBoard>().Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timed out at tick " + game.Tick + "; " + string.Join(";", facts));
            }
        }
        private static void CandidateCost(Check c)
        {
            foreach (var fixture in new[] { (false, false, false, false), (true, false, false, false),
                (false, true, false, false), (false, false, true, false), (false, false, true, true) })
            {
                bool nativePreferred = fixture.Item1, priorityFirst = fixture.Item2, equalCost = fixture.Item3, reverse = fixture.Item4;
                var profiles = Content.Json.Parse(Profiles);
                profiles.Get("logistics_policies")[0].Get("outlet").Set("export_priority", JsonValue.Number(priorityFirst ? "9" : "3"));
                profiles.Get("logistics_policies")[2].Get("outlet").Set("export_priority", JsonValue.Number("3"));
                var band = profiles.Get("logistics_policies")[1].Get("stock").Get("supply");
                band.Set("desired", JsonValue.Number("8")); band.Set("maximum", JsonValue.Number("8"));
                var profileText = new StringBuilder(); profiles.WriteCanonical(profileText);
                var factory = Factory(true, profileText.ToString(), root =>
                {
                    StartupContentAuthoring.Position(root, "source", JsonValue.Number("9"));
                    StartupContentAuthoring.Position(root, "machine", JsonValue.Number("40"));
                    var preferred = root.Get("entities")[3].Get("components");
                    StartupContentAuthoring.Position(root, "preferred", JsonValue.Number(equalCost ? "9" : "25"));
                    if (nativePreferred)
                    {
                        preferred.Remove("contract_outlet_policy");
                        preferred.Set("logistics_policy", Content.Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"policy.source\"}]}"));
                    }
                    if (reverse)
                    {
                        var starts = root.Get("entity_setups"); var reordered = JsonValue.NewArray();
                        foreach (int index in new[] { 3, 1, 2, 0 }) reordered.Add(starts[index].Clone());
                        root.Set("entity_setups", reordered);
                    }
                });
                var game = New(factory); Entity source = Find(game, "source"), preferredSource = Find(game, "preferred"),
                    customer = Find(game, "destination"), worker = Find(game, "machine");
                ResourceId resource = game.World.Content.ResourceId("supply");
                var log = game.World.Services.Get<IHaulExecution>();
                var near = log.PreviewHaul(new PlayerId(0), source.Id, customer.Id, resource, Fix.FromInt(8));
                var far = log.PreviewHaul(new PlayerId(0), preferredSource.Id, customer.Id, resource, Fix.FromInt(8));
                c.That(near.Status == HaulStatus.Ready && far.Status == HaulStatus.Ready && near.Carrier == worker.Id && far.Carrier == worker.Id,
                    "native/public outlets supply real selected-carrier previews before any commitment");
                c.That(near.TravelCost == Fix.FromInt(32).Raw && far.TravelCost == Fix.FromInt(equalCost ? 32 : 30).Raw,
                    "selected haul cost includes actual pickup travel and delivery, not just requester proximity");
                Entity expected = priorityFirst ? source : equalCost
                    ? source.Id.Value < preferredSource.Id.Value ? source : preferredSource : preferredSource;
                ulong hash = game.Hash(); var offers = new List<LogisticsDispatchOffer>();
                game.Seat(0).View.Feature<LogisticsView>().CopyQueue(offers);
                c.That(offers.Count == 1 && offers[0].Haul.Status == HaulStatus.Ready && offers[0].Haul.Source == expected.Id &&
                    offers[0].Haul.TravelCost == (expected == source ? near.TravelCost : far.TravelCost),
                    priorityFirst ? "explicit export priority still precedes complete candidate cost" : equalCost
                        ? "equal selected costs use stable actual endpoint identity even with reversed authored starts"
                        : "farther-from-customer source wins because its actual complete selected trip is cheaper");
                c.Equal(hash, game.Hash(), "matching and diagnostics mutate no job, reservation, stock or clock");
                var retained = offers[0]; var twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "unclaimed selected-trip intent restores identically");
                Tick(game); Tick(twin);
                Job job = game.World.Services.Get<WorkBoard>().Jobs[0];
                c.That(job.IsLive && job.From == expected.Id && job.To == customer.Id && job.Worker == worker.Id &&
                    job.OutReservation.IsValid && job.CargoInReservation.IsValid && job.InReservation.IsValid && job.Loaded.IsZero,
                    "normal engine acquisition rebuilds the same candidate and reserves actual stock, room, cargo and worker");
                c.Equal(game.Hash(), twin.Hash(), "saved unclaimed intent chooses identical actual shipment");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "chosen real commitments save through existing codecs");
                long started = game.Clock.StepIndex;
                Reach(game, () => !job.IsLive && customer.Get<IInventory>().Amount(resource) == Fix.FromInt(8), "selected complete trip physical delivery", twin);
                c.That(game.Clock.StepIndex > started && worker.Get<ITransporter>().Cargo.Amount(resource).IsZero &&
                    source.Get<IInventory>().Amount(resource) + preferredSource.Get<IInventory>().Amount(resource) + customer.Get<IInventory>().Amount(resource) == Fix.FromInt(40),
                    "selected native/public supply moves and services over one clock, conserving authored stock without instant transfer");
                c.That(source.Get<IInventory>().Reservations.Count == 0 && preferredSource.Get<IInventory>().Reservations.Count == 0 &&
                    customer.Get<IInventory>().Reservations.Count == 0 && game.Hash() == twin.Hash() && retained.Haul.Source == expected.Id,
                    "completion clears only real owned claims; saved twins agree and retained preview stays immutable");
            }
            var changedProfiles = Content.Json.Parse(Profiles);
            changedProfiles.Get("logistics_policies")[0].Get("outlet").Set("export_priority", JsonValue.Number("3"));
            changedProfiles.Get("logistics_policies")[2].Get("outlet").Set("export_priority", JsonValue.Number("3"));
            var changedBand = changedProfiles.Get("logistics_policies")[1].Get("stock").Get("supply");
            changedBand.Set("desired", JsonValue.Number("8")); changedBand.Set("maximum", JsonValue.Number("8"));
            var changedText = new StringBuilder(); changedProfiles.WriteCanonical(changedText);
            var changedFactory = Factory(true, changedText.ToString(), root =>
            {
                StartupContentAuthoring.Position(root, "source", JsonValue.Number("9"));
                StartupContentAuthoring.Position(root, "machine", JsonValue.Number("40"));
            });
            var changed = New(changedFactory); var before = new List<LogisticsDispatchOffer>(); changed.Seat(0).View.Feature<LogisticsView>().CopyQueue(before);
            c.That(before.Count == 1 && before[0].Haul.Source == Find(changed, "preferred").Id, "changed-supply fixture initially chooses the cheaper complete candidate");
            // Explicit endpoint protocol instrumentation: another owner consumes existing, unpromised stock.
            Find(changed, "preferred").Get<IInventory>().Take(changed.World.Content.ResourceId("supply"), Fix.FromInt(20));
            var changedTwin = changedFactory.Load(changed.Save()); Tick(changed); Tick(changedTwin);
            Job fresh = changed.World.Services.Get<WorkBoard>().Jobs[0];
            c.That(fresh.From == Find(changed, "source").Id && Find(changed, "preferred").Get<IInventory>().Reservations.Count == 0 &&
                before[0].Haul.Source == Find(changed, "preferred").Id && changed.Hash() == changedTwin.Hash(),
                "actual acquisition discards a stale cost winner after real supply changes, without copied offers or invented stock");
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Find(game, "source"), destination = Find(game, "destination"), machine = Find(game, "machine");
            ResourceId supply = game.World.Content.ResourceId("supply"); var log = game.World.Services.Get<LogisticsService>(); var stock = game.World.Services.Get<ILogisticsStockPolicies>();
            var view = game.Seat(0).View.Feature<LogisticsView>(); var initial = Band(game);
            c.That(ReferenceEquals(log, stock), "policy service aliases the existing logistics authority");
            c.Equal(1, log.Demands.Count, "JSON initialization adds one actual stock record, not a second table");
            c.Equal(Fix.FromInt(2), initial.Settings.Minimum, "profile minimum is explicit native quantity");
            c.Equal(Fix.FromInt(12), initial.Settings.Desired, "profile desired is explicit native quantity");
            c.Equal(Fix.FromInt(12), initial.Settings.Maximum, "profile maximum is explicit native quantity");
            c.That(initial.Replenishing, "actual initial shortage starts the existing row's refill latch");
            c.Equal(initial.Settings, ((ILogisticsDemand)log.Demands[0]).ReadDemand(), "demand contract exposes the same immutable settings");
            c.Equal(Fix.Zero, destination.Get<IInventory>().Amount(supply), "profile does not create inventory");
            ulong before = game.Hash();
            c.Equal(Code.None, view.ReadOutletPolicy(source.Id, out var retained), "engine view reads actual outlet contract");
            c.Equal(Fix.FromInt(4), retained.Reserve.AmountOf(supply), "authored reserve is intent, not fabricated stock");
            c.Equal(Code.None, game.Seat(0).Preview(new ConfigureStockPolicyCommand { Store = destination.Id, Resource = supply, Change = LogisticsDemandChange.Replace(Settings(true, 2, 8, 10, int.MaxValue)) }), "stock preview shares pure native admission");
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new ConfigureStockPolicyCommand { Store = destination.Id, Resource = supply }), "missing stock settings refuses");
            c.Equal(Codes.InvalidArgument, stock.CheckStockPolicy(destination.Owner, destination.Id, supply, LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.Half, Fix.One, Fix.FromInt(2), 1, LogisticsReserveAccess.Protected))), "fractional count bands refuse rather than round");
            c.Equal(Codes.InvalidArgument, stock.CheckStockPolicy(destination.Owner, destination.Id, supply, LogisticsDemandChange.Replace(Settings(true, 1, 20, 41))), "maximum exceeds actual native destination capacity");
            c.Equal(Codes.NotOwned, stock.CheckStockPolicy(new PlayerId(1), destination.Id, supply, LogisticsDemandChange.Replace(Settings(true, 1, 2, 3))), "foreign stock owner refuses");
            c.Equal(Codes.InvalidArgument, stock.CheckStockPolicy(destination.Owner, destination.Id, new ResourceId(int.MaxValue), LogisticsDemandChange.Replace(Settings(true, 1, 2, 3))), "unknown resource has no substitute");
            c.Equal(LogisticsPolicyCodes.NoPolicy, game.Seat(0).Preview(new ConfigureOutletPolicyCommand { Source = machine.Id, Change = OutletPolicyChange.Replace(Outlet(supply)) }), "missing outlet policy is not fabricated");
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new ConfigureOutletPolicyCommand { Source = source.Id }), "missing outlet settings refuses");
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new ConfigureOutletPolicyCommand { Source = source.Id, Change = OutletPolicyChange.Replace(Outlet(supply, reserve: 41)) }), "native reserve capacity is validated before mutation");
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new ConfigureOutletPolicyCommand { Source = source.Id, Change = OutletPolicyChange.Replace(Outlet(supply, destinations: new[] { "unknown" })) }), "unknown destination filter refuses");
            c.Equal(Codes.NotFound, PlayerView.Create(game.World, new PlayerId(1)).Feature<LogisticsView>().ReadOutletPolicy(source.Id, out var hidden), "foreign policy projection refuses");
            c.That(hidden == null, "private refusal returns no settings handle");
            c.Equal(before, game.Hash(), "all pure settings checks and views preserve hash");
            c.Throws<ArgumentException>(() => Settings(true, 4, 2, 6), "unordered bands refuse construction");
            c.Throws<ArgumentException>(() => Settings(true, -1, 2, 6), "negative native band refuses construction");
            c.Throws<ArgumentException>(() => Outlet(supply, destinations: new[] { "destination", "destination" }), "duplicate destination prototypes refuse");
            Stock(c, game, Settings(true, 2, 8, 10, int.MaxValue));
            c.Equal(initial.CreatedTick, Band(game).CreatedTick, "stock edits retain original age");
            c.Equal(Fix.FromInt(12), initial.Settings.Desired, "retained stock facts do not mutate");
            c.Equal(Fix.FromInt(8), Band(game).Settings.Desired, "existing demand row contains current player intent");
            c.Equal(32, SaveGame.Describe(game.Save()).Format, "bands/latch use current save format");
            c.Equal(StateCodecTests.ExpectedLogisticsSchema, log.StateSchemaVersion, "one demand codec owns settings and latch");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "load restores edits, not original profile stock settings");
            c.Equal(Code.None, stock.ConfigureStockPolicy(source.Owner, source.Id, supply, LogisticsDemandChange.Replace(Settings(true, 0, 6, 6))), "source policy uses the same demand table");
            c.Equal(Fix.Max(Fix.Zero, source.Get<IOutletPort>().Available(supply) - Fix.FromInt(6)), log.Spare(source.Get<IOutletPort>(), supply), "desired and explicit reserve protect their maximum, not duplicate additive balances");
            Export(c, game, source, Outlet(supply, enabled: false));
            c.Equal(new Code("point.blocked"), source.State, "entity Update commits authored disabled outlet state");
            c.That(retained.Enabled, "retained outlet settings stay immutable");
            var preview = view.PreviewHaul(source.Id, destination.Id, supply, Fix.FromInt(20));
            c.Equal(HaulStatus.PolicyBlocked, preview.Status, "all haul preview uses disabled export gate");
            c.Equal(LogisticsPolicyCodes.Disabled, preview.Blocker, "disabled export has typed blocker");
            before = game.Hash(); c.Equal(0, log.AcquireHaul(source.Owner, source.Id, destination.Id, supply, Fix.FromInt(20), 1), "direct acquire cannot bypass disabled policy");
            c.Equal(before, game.Hash(), "policy refusal creates no claims or job IDs");
            Export(c, game, source, Outlet(supply, destinations: Array.Empty<string>()));
            c.Equal(LogisticsPolicyCodes.Destination, view.PreviewHaul(source.Id, destination.Id, supply, Fix.FromInt(8)).Blocker, "explicit empty filter authorizes no destination");
            Export(c, game, source, Outlet(supply, reserve: 20));
            c.Equal(LogisticsPolicyCodes.Reserve, view.PreviewHaul(source.Id, destination.Id, supply, Fix.FromInt(8)).Blocker, "reserve prevents taking protected source stock");
            c.Equal(Fix.Zero, log.Spare(source.Get<IOutletPort>(), supply), "reserve is enforced in the existing spare calculation");
            Stock(c, game, Settings(false, 2, 8, 10));
            c.Equal(Fix.Zero, log.DemandOf(destination.Id, supply), "disabled stock intent stops protection/request contribution");
            c.That(!Band(game).Replenishing, "disabled row retains settings but not refill authorization");
            StrictContent(c); NativeEnvelope(c); CorruptLatch(c);
        }
        private static void StrictContent(Check c)
        {
            foreach (var change in new[] {
                ("\"minimum\": 2", "\"minimum\": 13"), ("\"maximum\": 12", "\"maximum\": 41"),
                ("\"minimum\": 2", "\"minimum\": 0.5"), ("\"export_priority\": 3", "\"export_priority_typo\": 3"),
                ("\"allowed_destinations\": [\"destination\"]", "\"allowed_destinations\": [\"missing\"]"),
                ("\"reserve\": { \"supply\": 4 }", "\"reserve\": { \"supply\": 0.5 }"),
                ("\"reserve\": { \"supply\": 4 }", "\"reserve_typo\": { \"supply\": 4 }") })
                c.Throws<ContentException>(() => New(Factory(profiles: Profiles.Replace(change.Item1, change.Item2))), "strict profile refuses " + change.Item2);
        }
        private static void NativeEnvelope(Check c)
        {
            JsonValue content = Content.Json.Parse(FrameworkProofContent.Read("colony.json"));
            content.Get("resources").Add(Content.Json.Parse("{\"id\":\"ore\",\"unit\":\"unit.steel\",\"category\":\"material\",\"phase\":\"solid\",\"traits\":[],\"capacity\":{\"unit.steel\":1}}"));
            var text = new StringBuilder(); content.WriteCanonical(text);
            GameFactory factory = Colony.Factory(text.ToString()); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 19 });
            Entity factoryEntity = game.World.Entities.Get(Colony.Mine(game, 0, "factory")[0].Id); var policies = game.World.Services.Get<ILogisticsStockPolicies>();
            ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            c.Equal(Code.None, policies.ConfigureStockPolicy(factoryEntity.Owner, factoryEntity.Id, steel, LogisticsDemandChange.Replace(Settings(true, 1, 5, 10))), "engine module may configure its own private native store");
            c.Equal(Codes.InvalidArgument, policies.CheckStockPolicy(factoryEntity.Owner, factoryEntity.Id, water, LogisticsDemandChange.Replace(Settings(true, 1, 5, 10))), "capacity dimensions are not treated as mixed free space");
            c.Equal(Code.None, policies.ConfigureStockPolicy(factoryEntity.Owner, factoryEntity.Id, steel, LogisticsDemandChange.Replace(Settings(true, 1, 5, 60))), "first native maximum fits its whole store envelope");
            ulong before = game.Hash();
            c.Equal(Codes.InvalidArgument, policies.ConfigureStockPolicy(factoryEntity.Owner, factoryEntity.Id, game.World.Content.ResourceId("ore"), LogisticsDemandChange.Replace(Settings(true, 1, 5, 50))), "two separately fitting native maxima cannot overbook one authored dimension");
            c.Equal(before, game.Hash(), "whole envelope refusal preserves all previous policy and intent");
        }
        private static void CorruptLatch(Check c)
        {
            SimGame game = New(Factory()); using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); int route = 1; writer.Int(ref route); writer.Count(1);
                EntityId destination = Find(game, "destination").Id; ResourceId resource = game.World.Content.ResourceId("supply");
                writer.Entity(ref destination); writer.Resource(ref resource); LogisticsDemandSettings.Sync(writer, Settings(false, 2, 8, 10));
                bool latch = true; writer.Bool(ref latch); PlayerId owner = new PlayerId(0); writer.Player(ref owner);
                var source = new LogisticsDemandId(LogisticsService.StockDemandScope, destination.Value, Code.None); LogisticsDemandId.Sync(writer, ref source);
                long created = 0; writer.Long(ref created); writer.Count(0); writer.Flush(); byte[] data = stream.ToArray();
                c.Throws<CorruptStateException>(() => game.World.Services.Get<LogisticsService>().Sync(new StateReader(data, 0, data.Length)), "disabled saved refill latch cannot fabricate authorization");
            }
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Find(game, "source"), destination = Find(game, "destination"), machine = Find(game, "machine");
            ResourceId supply = game.World.Content.ResourceId("supply"); IInventory stock = source.Get<IInventory>(), dest = destination.Get<IInventory>(), cargo = machine.Get<ITransporter>().Cargo;
            SimGame start = factory.Load(game.Save()); c.Equal(game.Hash(), start.Hash(), "initial native stock/latch checkpoint restores");
            Reach(game, () => dest.Amount(supply) == Fix.FromInt(8), "first partial refill delivery", start);
            c.That(Band(game).Replenishing, "refill remains active above minimum until actual desired stock arrives");
            c.Equal(game.Hash(), start.Hash(), "initial saved twin performs actual partial refill");
            Stock(c, game, Settings(true, 2, 12, 12, 99)); Stock(c, start, Settings(true, 2, 12, 12, 99));
            c.That(Band(game).Replenishing, "priority-only policy edit preserves the active refill cycle above minimum");
            c.Equal(game.Hash(), start.Hash(), "saved partial cycle applies the same edit without resetting latch");
            SimGame partial = factory.Load(game.Save());
            Reach(game, () => dest.Amount(supply) == Fix.FromInt(12) && !Band(game).Replenishing, "continued refill to desired", partial);
            c.Equal(game.Hash(), partial.Hash(), "saved refill latch continues between minimum and desired");
            c.Equal(Fix.FromInt(8), stock.Amount(supply), "two native loads remove exactly delivered source stock");
            c.Equal(Fix.Zero, cargo.Amount(supply), "completed refill leaves no cargo");
            c.That(dest.Remove(supply, Fix.FromInt(8)) == Fix.FromInt(8), "controlled native consumption leaves stock between min and desired");
            Tick(game); SimGame quiet = factory.Load(game.Save());
            for (int i = 0; i < 25; i++) { Tick(game); Tick(quiet); }
            c.Equal(Fix.FromInt(4), dest.Amount(supply), "no automatic refill while quiet stock remains above minimum");
            c.Equal(game.Hash(), quiet.Hash(), "saved quiet stock does not reset refill latch from profile");
            dest.Remove(supply, Fix.FromInt(3)); Tick(game);
            Reach(game, () => cargo.Amount(supply).IsPositive, "new below-minimum refill pickup");
            c.Equal(Fix.FromInt(4), cargo.Amount(supply), "export reserve limits actual new native load");
            c.Equal(Fix.FromInt(4), stock.Amount(supply), "protected source reserve remains actual stock");
            Stock(c, game, Settings(false, 0, 0, 0));
            c.Equal(Fix.FromInt(4), cargo.Amount(supply), "stock policy disabling does not erase acquired cargo");
            c.Equal(Fix.FromInt(4), dest.ReservedIn(supply), "stock policy disabling keeps live receiving receipt");
            SimGame disabled = factory.Load(game.Save());
            Reach(game, () => cargo.Amount(supply).IsZero && dest.Amount(supply) == Fix.FromInt(5), "disabled requester committed unload", disabled);
            c.Equal(game.Hash(), disabled.Hash(), "disabled requester saved commitment completes identically");
            c.Equal(Fix.Zero, game.World.Services.Get<LogisticsService>().Spare(source.Get<IOutletPort>(), supply), "protected source is unavailable to routes and direct acquisition");
            Preferred(c); Maximum(c); Route(c); Recheck(c);
        }
        private static void Preferred(Check c)
        {
            GameFactory factory = Factory(preferred: true); SimGame game = New(factory), isolated = New(factory); Entity preferred = Find(game, "preferred"), destination = Find(game, "destination"); ResourceId supply = game.World.Content.ResourceId("supply");
            c.That(preferred.Get<IOutletPolicy>() is ContractOutletPolicy, "ordinary mod supplies public IOutletPolicy without selector edits");
            Stock(c, isolated, Settings(false, 0, 0, 0));
            Reach(game, () => Find(game, "machine").Get<ITransporter>().Cargo.Amount(supply).IsPositive, "preferred external source pickup", isolated: isolated);
            c.Equal(Fix.FromInt(20), Find(game, "source").Get<IInventory>().Amount(supply), "export priority selects farther compatible source before nearby lower priority");
            Job job = game.World.Services.Get<WorkBoard>().Job(Find(game, "machine").Get<IWorker>().Job);
            c.Equal(preferred.Id, job.From, "actual job uses the selected external source");
            Export(c, game, preferred, Outlet(supply, enabled: false, reserve: 2, priority: 9));
            SimGame saved = factory.Load(game.Save());
            Reach(game, () => destination.Get<IInventory>().Amount(supply) >= Fix.FromInt(8), "loaded disabled source delivery", saved, isolated);
            c.Equal(game.Hash(), saved.Hash(), "disabled external source retains in-flight receipt semantics on load");
            c.Equal(Fix.Zero, Find(isolated, "destination").Get<IInventory>().Amount(supply), "interleaved disabled stock session receives no borrowed hauls");
            c.Equal(Fix.FromInt(20), Find(isolated, "preferred").Get<IInventory>().Amount(supply), "external outlet state is session-local");
        }
        private static void Maximum(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Find(game, "source"), destination = Find(game, "destination"); ResourceId supply = game.World.Content.ResourceId("supply");
            Stock(c, game, Settings(true, 0, 4, 6)); var log = game.World.Services.Get<LogisticsService>();
            int first = log.AcquireHaul(source.Owner, source.Id, destination.Id, supply, Fix.FromInt(20), 1);
            c.That(first > 0, "direct native haul uses shared policy admission");
            c.Equal(Fix.FromInt(6), destination.Get<IInventory>().ReservedIn(supply), "maximum bounds real inbound promise, not only demand matching");
            Entity second = game.World.Spawn(game.World.Content.Get<EntityDef>("machine"), source.Owner); game.World.Settle(); ulong before = game.Hash();
            c.Equal(0, log.AcquireHaul(source.Owner, source.Id, destination.Id, supply, Fix.One, 1), "another idle carrier cannot overbook policy maximum");
            c.Equal(LogisticsPolicyCodes.Maximum, log.PreviewHaul(source.Owner, source.Id, destination.Id, supply, Fix.One).Blocker, "maximum wait reports typed current reason");
            c.Equal(before, game.Hash(), "ceiling refusal consumes no receipts, worker or job");
            Stock(c, game, Settings(true, 0, 1, 1)); c.Equal(Fix.FromInt(6), destination.Get<IInventory>().ReservedIn(supply), "lowered maximum does not revoke acquired inbound commitments");
            SimGame saved = factory.Load(game.Save()); Reach(game, () => destination.Get<IInventory>().Amount(supply) == Fix.FromInt(6), "grandfathered physical receiving commitment", saved);
            c.Equal(game.Hash(), saved.Hash(), "saved lowered maximum preserves exact committed delivery");
            c.Equal(0, destination.Get<IInventory>().Reservations.Count, "completed ceiling commitment leaves no orphan receiving claims");
            c.Equal(LogisticsPolicyCodes.Maximum, log.PreviewHaul(source.Owner, source.Id, destination.Id, supply, Fix.One).Blocker, "actual above-maximum stock blocks new inbound acquisition");
        }
        private static void Route(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Find(game, "source"), destination = Find(game, "destination"); ResourceId supply = game.World.Content.ResourceId("supply");
            Stock(c, game, Settings(true, 0, 8, 8)); Export(c, game, source, Outlet(supply, reserve: 18));
            c.That(TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = destination.Id, Resource = supply, Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 40) }).Accepted, "player creates route through ordinary engine command");
            Reach(game, () => destination.Get<IInventory>().Amount(supply) == Fix.FromInt(2), "route protected-stock transfer");
            c.Equal(Fix.FromInt(18), source.Get<IInventory>().Amount(supply), "route cannot bypass explicit source reserve");
            for (int i = 0; i < 30; i++) Tick(game);
            c.Equal(Fix.FromInt(2), destination.Get<IInventory>().Amount(supply), "route retries cannot duplicate protected supply");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "route and policy checkpoint use the same physical owners");
        }
        private static void Recheck(Check c)
        {
            SimGame game = New(Factory()); Entity source = Find(game, "source"), destination = Find(game, "destination"); ResourceId supply = game.World.Content.ResourceId("supply"); var log = game.World.Services.Get<LogisticsService>();
            HaulPreview retained = log.PreviewHaul(source.Owner, source.Id, destination.Id, supply, Fix.FromInt(8));
            c.Equal(HaulStatus.Ready, retained.Status, "pure current offer is ready before policy change");
            Export(c, game, source, Outlet(supply, enabled: false)); ulong before = game.Hash();
            c.Equal(0, log.AcquireHaul(source.Owner, source.Id, destination.Id, supply, retained.Amount, 1), "retained ready preview is not authorization after export edit");
            c.Equal(before, game.Hash(), "re-evaluation refuses without partial commitment");
        }
    }
}
