using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    /// <summary>Declared predicate dependencies; the existing DAG compiler owns cycle validation.</summary>
    public interface ITileConditionGroupDefinition : ITileConditionDefinition, IDependencyNode
    { new string Id { get; } }
    public sealed class TileConditionGroupDef : Def, ITileConditionGroupDefinition
    {
        private string _space;
        private bool _all;
        private IReadOnlyList<ITileConditionDefinition> _conditions;
        public ITileSpaceDefinition Space { get; private set; }
        public IReadOnlyList<string> Dependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _space = reader.String("space"); string mode = reader.String("mode");
            if (mode != "all" && mode != "any") throw reader.Error("tile condition group requires all or any");
            _all = mode == "all"; string[] ids = reader.Strings("conditions");
            if (ids.Length == 0) throw reader.Error("tile condition group needs explicit nonempty conditions");
            var distinct = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
            {
                try { ContractIdentity.ValidateId(id, "tile condition"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                if (!distinct.Add(id)) throw reader.Error("duplicate tile condition dependency");
            }
            Dependencies = Array.AsReadOnly(ids);
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Space = content.Def<ITileSpaceDefinition>(_space);
            var conditions = new List<ITileConditionDefinition>();
            foreach (string id in Dependencies) conditions.Add(content.Def<ITileConditionDefinition>(id));
            _conditions = conditions.AsReadOnly();
        }
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            foreach (var condition in _conditions)
            {
                TileConditionContracts.Validate(condition, content);
                if (!ReferenceEquals(condition.Space, Space)) throw content.Fail("tile condition group dependencies require the same actual space");
            }
            var nodes = new List<ConditionNode>();
            foreach (var condition in content.Content.All<ITileConditionDefinition>()) nodes.Add(new ConditionNode(condition));
            _ = new DependencyGraph<ConditionNode>(nodes, "tile conditions");
        }
        public bool Matches(World world, ITileSpatialSpace map, TileCoord tile)
        {
            TileConditionContracts.RequireContext(this, world, map);
            foreach (var condition in _conditions)
            {
                bool matches = TileConditionContracts.Matches(condition, world, map, tile);
                if (_all != matches) return !_all;
            }
            return _all;
        }
        private sealed class ConditionNode : IDependencyNode
        {
            public string Id { get; }
            public IReadOnlyList<string> Dependencies { get; }
            internal ConditionNode(ITileConditionDefinition condition)
            {
                Id = condition.Id; Dependencies = condition is ITileConditionGroupDefinition group
                    ? group.Dependencies : Array.Empty<string>();
            }
        }
    }
}
