using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent content provider. Fault injection is explicitly authored and active only
    // after a physical update plus host pause; it is never production gameplay logic.
    public sealed class NavigationObservationDef : Def, ITileNavigationRule
    {
        private string _space; private ITileSpaceDefinition _definition;
        private string _nestedNavigation;
        private TileCoord _nestedFrom;
        private TileRect _nestedGoal;
        private int _nestedBudget;
        public int NestedQueries { get; private set; } // Diagnostic callback witness, not gameplay state.
        public string Space => _definition.Id;
        public int MinCost { get; private set; }
        public string Fault { get; private set; }
        public bool Raw { get; private set; }
        public string Layer { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _space = reader.String("space"); MinCost = reader.Int("minimum"); Fault = reader.String("fault");
            Raw = reader.Bool("raw"); Layer = reader.String("layer");
            if (MinCost < 2 || (Fault != "none" && Fault != "throw" && Fault != "clock" && Fault != "space" && Fault != "negative" && Fault != "below-minimum" && Fault != "provider" && Fault != "rng" && Fault != "rng-restore" && Fault != "rng-same" && Fault != "rng-zero" && Fault != "rng-hash" && Fault != "rng-save" && Fault != "nested-route") || Fault == "provider" && !Raw)
                throw reader.Error("explicit valid navigation observation probe required");
            if(Fault=="nested-route")
            {
                var nested=reader.Child("nested_query")??throw reader.Error("explicit nested query required");
                _nestedNavigation=nested.String("navigation");
                _nestedFrom=new TileCoord(nested.Int("from_x"),nested.Int("from_y"));
                _nestedGoal=new TileRect(nested.Int("goal_x"),nested.Int("goal_y"),1,1);
                _nestedBudget=nested.Int("maximum_expanded");nested.End();
                if(string.IsNullOrWhiteSpace(_nestedNavigation)||_nestedNavigation==Id||_nestedBudget<1)
                    throw reader.Error("distinct authored inner navigation and positive expansion budget required");
            }
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _definition = content.Def<ITileSpaceDefinition>(_space);
            var layer = content.Def<ISpatialLayerDefinition>(Layer);
            if (layer.SpaceId != _space || layer.ValueType != SpatialLayerValueType.Int32)
                throw content.Fail("probe needs its actual int layer in selected tile space");
            if(_nestedNavigation!=null&&content.Def<ITileNavigationRule>(_nestedNavigation).Space!=_space)
                throw content.Fail("nested query requires its actual selected space");
        }
        public int Cost(World world, TileCoord tile)
        {
            TileNavigationContracts.RequireContext(this, world);
            var map = (TileMap)world.Space;
            if (!map.InBounds(tile)) return 0;
            if (world.Clock.Paused && world.Clock.StepIndex > 0)
            {
                if (Fault == "throw") throw new InvalidOperationException("proof.navigation-cost-original");
                if (Fault == "clock") ((GameClock)world.Clock).Resume();
                if (Fault == "space") map.Layer<IntLayer>(Layer).Set(tile, 1);
                if (Fault == "rng") world.Random.NextULong();
                if (Fault == "rng-restore") { ulong saved = world.Random.State; world.Random.NextULong(); world.Random.State = saved; }
                if (Fault == "rng-same") world.Random.State = world.Random.State;
                if (Fault == "rng-zero") world.Random.State = 0;
                if (Fault == "rng-hash") world.Hash();
                if (Fault == "rng-save") SaveGame.Write(world, "observation-probe");
                if (Fault == "negative") return -1;
                if (Fault == "below-minimum") return MinCost - 1;
                if (Fault == "nested-route")
                {
                    var path=new List<TileCoord>();
                    var result=world.Services.Get<NavService>().Grid(_nestedNavigation).FindPath(_nestedFrom,_nestedGoal,path,_nestedBudget);
                    if(result!=PathSearchResult.Found||path.Count==0||!_nestedGoal.Contains(path[path.Count-1]))
                        throw new InvalidOperationException("proof nested physical query failed: "+result);
                    NestedQueries++;
                }
            }
            return MinCost;
        }
    }
    public sealed class NavigationObservationView
    {
        private readonly World _world; private readonly string _grid;
        public NavigationObservationView(World world, string grid) { _world = world; _grid = grid; }
        public int EntryCost(TileCoord tile) => _world.Services.Get<NavService>().Grid(_grid).Cost(tile);
    }
    // Actual per-session persistent callback target; exercises the raw registration's
    // original service codec, rather than a forwarding closure with no state owner.
    public sealed class NavigationRawCostOwner : IPersistentService
    {
        private readonly World _world; private readonly NavigationObservationDef _rule; private int _value;
        public NavigationRawCostOwner(World world, NavigationObservationDef rule) { _world = world; _rule = rule; }
        public string Id => "proof.navigation-observation.raw-owner";
        public int StateSchemaVersion => 1;
        public int Cost(TileCoord tile)
        {
            if (_world.Clock.Paused && _world.Clock.StepIndex > 0 && _rule.Fault == "provider") _value++;
            return _rule.Cost(_world, tile);
        }
        public void Sync(IStateSync state)
        {
            int value = _value; state.Int(ref value);
            if (state.Reading) { if (value < 0) throw new CorruptStateException("invalid navigation probe counter"); _value = value; }
        }
    }
    public sealed class NavigationObservationModule : GameModule
    {
        public override string Id => "proof.navigation-observation";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TileNavigationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<NavigationObservationDef>("navigation_observation_probes");
        public override void Compose(WorldComposer composer)
        {
            var rule = composer.Content.Get<NavigationObservationDef>("probe.navigation.cost");
            string grid = rule.Id;
            if (rule.Raw)
            {
                grid = "probe.navigation.raw";
                var owner = new NavigationRawCostOwner(composer.World, rule); composer.Services.Bind(owner);
                composer.Services.Get<NavService>().Define(grid, rule.MinCost, owner.Cost);
            }
            composer.AddViewFeature("proof.navigation-cost", 1, (world, viewer) => new NavigationObservationView(world, grid));
        }
    }
}
