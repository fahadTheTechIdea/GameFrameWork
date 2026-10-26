using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>A selected game's flat terrain cost, authored rather than supplied by a host.</summary>
    public sealed class NavigationCostDef : Def
    {
        public string MovementClass { get; private set; }
        public int EntryCost { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            MovementClass = reader.String("movement"); EntryCost = reader.Int("entry_cost");
            if (string.IsNullOrWhiteSpace(MovementClass) || EntryCost < 1)
                throw reader.Error("explicit movement identity and positive entry cost are required");
        }
    }
    public sealed class NavigationStartDef : Def
    {
        private string _entity;
        public EntityDef Entity { get; private set; }
        public TileCoord Tile { get; private set; }
        public PlayerId Owner { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _entity = reader.String("entity"); Tile = new TileCoord(reader.Int("x"), reader.Int("y"));
            Owner = new PlayerId(reader.Int("owner"));
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Entity = content.Def<EntityDef>(_entity);
            if (Entity.Component<ITilePresenceDefinition>() == null) throw content.Fail("tile start needs tile presence");
            if (!Owner.IsValid) throw content.Fail("tile start needs an owned entity");
        }
    }
    public sealed class NavigationProofModule : GameModule
    {
        public int Initializations;
        public override string Id => "proof.navigation";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TileNavigationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<NavigationCostDef>("navigation_costs");
            types.Section<NavigationStartDef>("navigation_starts");
        }
        public override void Compose(WorldComposer composer)
        {
            NavService navigation = composer.Services.Get<NavService>();
            foreach (NavigationCostDef definition in composer.Content.All<NavigationCostDef>())
                navigation.Define(definition.MovementClass, definition.EntryCost, _ => definition.EntryCost);
            composer.AddWorldGen(new Starts(), 10);
            composer.AddSystem(new InitializationCounter(this), 300);
        }
        private sealed class Starts : WorldGenStage
        {
            public override string Id => "proof.navigation-starts";
            public override void Generate(World world, ScenarioDef scenario)
            {
                TileMap map = world.Services.Get<TileMap>();
                foreach (NavigationStartDef start in world.Content.All<NavigationStartDef>())
                {
                    if (!world.Players.IsSeat(start.Owner)) throw new ContentException("navigation start owner is outside the scenario seats");
                    map.SpawnAt(world, start.Entity, start.Owner, start.Tile);
                }
            }
        }
        private sealed class InitializationCounter : GameSystem
        {
            private readonly NavigationProofModule _module;
            public InitializationCounter(NavigationProofModule module) { _module = module; }
            public override string Id => "proof.navigation-initialization";
            public override int StateSchemaVersion => 1;
            public override void Initialize(World world) { _module.Initializations++; }
            public override void Update(World world, TickInfo tick) { }
        }
    }
}
