using System;
using System.Collections.Generic;
using Beep.SimEngine;
using Beep.SimEngine.App;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.ExtensionProof
{
    // ---------------------------------------------------------------- a tiny game used to prove the framework

    public sealed class CounterDef : ComponentDef<Counter>
    {
        public Fix PerTick { get; private set; }
        protected override void ReadCore(JsonReader r) { PerTick = r.Fix("per_tick"); }
    }

    public sealed class Counter : Component<CounterDef>, IInspectable
    {
        public Fix Value;
        public override void Sync(IStateSync s) => s.Fix(ref Value);
        public void Inspect(World world, PlayerId viewer, IList<Facet> into)
        {
            if (viewer == Entity.Owner || !viewer.IsValid) into.Add(new Facet(new Code("test.count"), Value));
        }
    }

    public sealed class CounterSystem : GameSystem
    {
        public override int StateSchemaVersion => 1;
        public override string Id => "test.counter";
        public override Schedule GetSchedule(ISimulationTime time) => Schedule.Spread(10);
        public int Visits;
        public override void Update(World world, TickInfo tick)
        {
            IReadOnlyList<Counter> due = world.Entities.Components<Counter>().Bucket(10, tick.Bucket);
            for (int i = 0; i < due.Count; i++)
            {
                if (!due[i].Entity.Alive) continue;
                due[i].Value += tick.Over(due[i].Def.PerTick);
                Visits++;
            }
        }
        public override void Sync(IStateSync s) => s.Int(ref Visits);
    }

    public struct BinPlaced { public EntityId Bin; }

    public sealed class PlaceBin : Command
    {
        public TileCoord At;
        public override void Sync(IStateSync s) => s.Tile(ref At);
    }

    public sealed class RemoveEntity : Command
    {
        public EntityId Target;
        public override void Sync(IStateSync s) => s.Entity(ref Target);
    }

    public sealed class PlaceBinHandler : CommandHandler<PlaceBin>
    {
        public static readonly Code Occupied = new Code("test.occupied");
        protected override Code Check(World world, PlaceBin c)
        {
            EntityDef bin = world.Content.Get<EntityDef>("bin");
            ITilePresenceDefinition presence = bin.Component<ITilePresenceDefinition>();
            if (presence == null) return Codes.InvalidArgument;
            var rect = new TileRect(c.At, presence.Width, presence.Height);
            TileMap map = (TileMap)world.Space;
            if (!map.InBounds(rect)) return Codes.OutOfBounds;
            if (!map.IsFree(rect)) return Occupied;
            return Code.None;
        }
        protected override void Apply(World world, PlaceBin c, CommandContext ctx)
        {
            Entity e = ((TileMap)world.Space).SpawnAt(world, world.Content.Get<EntityDef>("bin"), c.Player, c.At);
            ctx.Entity = e.Id;
            world.Events.Publish(new BinPlaced { Bin = e.Id });
            world.Notify(new Code("test.bin-placed"), c.Player, e.Id, "bin");
        }
    }

    public sealed class RemoveHandler : CommandHandler<RemoveEntity>
    {
        protected override Code Check(World world, RemoveEntity c)
        {
            Entity e = world.Entities.Get(c.Target);
            if (e == null) return Codes.NotFound;
            return e.Owner != c.Player ? Codes.NotOwned : world.CheckDespawn(e);
        }
        protected override void Apply(World world, RemoveEntity c, CommandContext ctx) => world.Despawn(world.Entities.Get(c.Target));
    }

    public sealed class EventLog : GameSystem
    {
        public override int StateSchemaVersion => 1;
        public override string Id => "test.eventlog";
        public readonly List<EntityId> Seen = new List<EntityId>();
        public void OnPlaced(BinPlaced e) => Seen.Add(e.Bin);
        public override void Update(World world, TickInfo tick) { }
    }

    public sealed class StartBins : WorldGenStage
    {
        public override string Id => "test.starts";
        public override void Generate(World world, ScenarioDef scenario)
        {
            JsonReader s = scenario.SettingsFor("test");
            int spacing = s == null ? 8 : s.Int("spacing");
            s?.End();
            TileMap map = (TileMap)world.Space;
            ByteLayer ground = map.Layer<ByteLayer>("ground");
            ground.Fill((x, y) => (byte)(world.Random.NextInt(4)));
            for (int p = 0; p < world.Players.Count; p++)
                map.SpawnAt(world, world.Content.Get<EntityDef>("bin"), new PlayerId(p), new TileCoord(2 + p * spacing, 2));
        }
    }

    public sealed class TestModule : GameModule
    {
        public override string Id => "test";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Component<CounterDef>("counter", 1);
        public override void Compose(WorldComposer c)
        {
            c.AddSystem(new CounterSystem(), 100);
            var log = new EventLog();
            c.Events.Register<BinPlaced>("test.bin-placed", 1);
            c.Events.Subscribe<BinPlaced>("test.eventlog", log.OnPlaced);
            c.AddSystem(log, 200);
            c.AddViewFeature<CounterView>("test.counter", 1, (world, viewer) => new CounterView(world, viewer));
            c.Commands.Register("test.place-bin", new PlaceBinHandler());
            c.Commands.Register("test.remove", new RemoveHandler());
            c.AddWorldGen(new StartBins(), 10);
        }
    }

    public static class TestGame
    {
        public const string TimeDefinitions = @"
  ""time_definitions"": [{ ""id"": ""test.minute"", ""quantum"": { ""numerator"": 60, ""denominator"": 1, ""unit"": ""second"" }, ""frame_ticks"": 1, ""ticks_per_second"": 30, ""calendar"": ""test.calendar"", ""horizon"": null }],
  ""calendars"": [{ ""id"": ""test.calendar"", ""fields"": [
    { ""id"": ""minute"", ""duration"": { ""numerator"": 1, ""denominator"": 1, ""unit"": ""minute"" }, ""radix"": 60, ""origin"": 0 },
    { ""id"": ""hour"", ""duration"": { ""numerator"": 1, ""denominator"": 1, ""unit"": ""hour"" }, ""radix"": 24, ""origin"": 0 },
    { ""id"": ""day"", ""duration"": { ""numerator"": 1, ""denominator"": 1, ""unit"": ""day"" }, ""radix"": 365, ""origin"": 1 },
    { ""id"": ""year"", ""duration"": { ""numerator"": 365, ""denominator"": 1, ""unit"": ""day"" }, ""radix"": 0, ""origin"": 1 }
  ] }],";
        public static string Manifest => FrameworkProofContent.Read("test-master.json");

        public static string Defs => FrameworkProofContent.Read("test/defs.json");

        public static string ExtraDefs => FrameworkProofContent.Read("extra/defs.json");

        public static GameDefinition Definition() => new GameDefinition("testgame", new GameModule[] { new TestModule(), new ExtraModule() });

        public static ContentSet Content(string defs = null)
        {
            var files = new PreloadedFileSource().Add("manifest.json", Manifest).Add("test/defs.json", defs ?? Defs).Add("extra/defs.json", ExtraDefs);
            return ContentSet.Load(files, Definition().ContentTypes);
        }

        public static GameFactory Factory() => new GameFactory(Definition(), Content());
        public static readonly ISimulationTime Time = Factory().ScenarioTime("flat");

        public static SimGame New(ulong seed = 7) => Factory().NewGame(new NewGameRequest { Scenario = "flat", Seed = seed });
    }

    /// <summary>An extension-owned immutable query surface using only public framework contracts.</summary>
    public sealed class CounterView
    {
        private readonly PlayerView _view;
        internal CounterView(World world, PlayerId viewer) { _view = world.ViewFor(viewer); }

        public bool TryRead(EntityId id, out Fix value)
        {
            var facets = new List<Facet>();
            _view.Inspect(id, facets);
            for (int i = 0; i < facets.Count; i++)
                if (facets[i].Key == new Code("test.count")) { value = facets[i].Value; return true; }
            value = default;
            return false;
        }
    }

    public sealed class ExtraModule : GameModule
    {
        public override string Id => "extra";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement("test", new ModuleVersion(1, 0, 0)) };
    }

}
