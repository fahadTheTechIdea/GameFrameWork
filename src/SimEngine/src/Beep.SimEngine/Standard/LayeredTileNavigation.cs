using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    public sealed class LayeredTileNavigationEntry
    {
        public string Id { get; }
        public int Priority { get; }
        public int Cost { get; }
        private readonly TileConditionReferences _conditions;
        public IReadOnlyList<ITileConditionDefinition> Conditions => _conditions.Definitions;
        internal void Resolve(ContentResolver content) => _conditions.Resolve(content);
        internal void Validate(ContentResolver content, ITileSpaceDefinition space) => _conditions.Validate(content, space);
        internal LayeredTileNavigationEntry(JsonReader reader)
        {
            Id = reader.String("id"); Priority = reader.Int("priority"); Cost = reader.Int("cost");
            try { ContractIdentity.ValidateId(Id, "navigation rule"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            if (Cost < 0) throw reader.Error("navigation cost is nonnegative; zero explicitly blocks entry");
            _conditions = new TileConditionReferences(reader); reader.End();
        }
    }
    /// <summary>Native authored cost provider; NavService/PathSearch/Mover remain the sole routing/travel owners.</summary>
    public sealed class LayeredTileNavigationDef : Def, ITileNavigationRule
    {
        private readonly LayeredTileNavigationDeclaration _declaration = new LayeredTileNavigationDeclaration();
        public int BaseCost => _declaration.BaseCost;
        public IReadOnlyList<LayeredTileNavigationEntry> Rules => _declaration.Rules;
        public string Space => _declaration.Space.Id;
        public int MinCost => _declaration.MinCost;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.Validate(content);
        public int Cost(World world, TileCoord tile)
        {
            TileNavigationContracts.RequireContext(this, world);
            return _declaration.Cost(world, tile);
        }
    }
    /// <summary>Sole native layered navigation authoring and rule interpretation, not route state.</summary>
    public sealed class LayeredTileNavigationDeclaration
    {
        private string _space;
        private ITileSpaceDefinition _resolved;
        private int _minimum;
        public int BaseCost { get; private set; }
        public IReadOnlyList<LayeredTileNavigationEntry> Rules { get; private set; }
        public ITileSpaceDefinition Space => _resolved;
        public int MinCost => _minimum;
        public void Read(JsonReader reader)
        {
            _space = reader.String("space"); BaseCost = reader.Int("base_cost");
            if (BaseCost < 0) throw reader.Error("base_cost must be nonnegative");
            var entries = reader.Array("rules") ?? throw reader.Error("explicit navigation rules required, including [] for base only");
            var rules = new LayeredTileNavigationEntry[entries.Count]; var ids = new HashSet<string>(StringComparer.Ordinal);
            int? minimum = BaseCost > 0 ? BaseCost : (int?)null;
            for (int i = 0; i < entries.Count; i++)
            {
                var rule = rules[i] = new LayeredTileNavigationEntry(new JsonReader(entries[i], reader.Where + ".rules[" + i + "]"));
                if (!ids.Add(rule.Id)) throw reader.Error("duplicate navigation rule identity: " + rule.Id);
                if (rule.Cost > 0 && (!minimum.HasValue || rule.Cost < minimum.Value)) minimum = rule.Cost;
            }
            if (!minimum.HasValue) throw reader.Error("navigation needs at least one positive authored cost outcome");
            _minimum = minimum.Value;
            Array.Sort(rules, (a, b) => { int priority = b.Priority.CompareTo(a.Priority); return priority != 0 ? priority : StringComparer.Ordinal.Compare(a.Id, b.Id); });
            Rules = Array.AsReadOnly(rules);
        }
        public void Resolve(ContentResolver content)
        {
            _resolved = content.Def<ITileSpaceDefinition>(_space);
            foreach (var rule in Rules) rule.Resolve(content);
        }
        public void Validate(ContentResolver content)
        {
            foreach (var rule in Rules) rule.Validate(content, _resolved);
        }
        public int Cost(World world, TileCoord tile)
        {
            if (world == null || !(world.Space is ITileSpatialSpace map) || Space == null || !world.Content.Owns(Space) || map.Id != Space.Id)
                throw new ArgumentException("layered navigation requires its actual session content and tile space");
            if (!map.InBounds(tile)) return 0;
            foreach (var rule in Rules)
            {
                bool matches = true;
                foreach (var condition in rule.Conditions)
                {
                    if (!TileConditionContracts.Matches(condition, world, map, tile)) { matches = false; break; }
                }
                if (matches) return rule.Cost;
            }
            return BaseCost;
        }
    }
}
